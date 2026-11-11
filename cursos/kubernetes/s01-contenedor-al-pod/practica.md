# Práctica - S01 - Del contenedor al pod

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 2 horas, en equipo, en su VM o en su equipo con Docker.

---

## Objetivo

Levantar un clúster local, traducir un `compose.yaml` a dos pods, comparar línea por línea qué se tradujo y qué no, y **verlo fallar** tres veces: borrar un pod y comprobar que nadie lo repone, escribir mal una imagen y leer el evento, y buscar la base por nombre y ver que no existe. Al final cada equipo sabe decir qué le falta a un pod suelto para parecerse a un servicio de Compose.

---

## Antes de empezar

- Docker funcionando (`docker version`) y memoria libre: un clúster local pide unos 2 GB.
- Una carpeta de trabajo vacía; todos los archivos de la práctica viven ahí y se versionan con Git desde hoy:

```bash
mkdir -p ~/k8s-lab/s01 && cd ~/k8s-lab/s01 && git init
```

- **Ejemplo ilustrativo:** el `compose.yaml` de esta práctica usa `traefik/whoami` como API de juguete. Responde con el nombre del contenedor, su IP y las cabeceras de la petición; sirve para ver qué réplica contestó. En el proyecto final usarán su propia API.

---

## Paso 1 - Un clúster local

Elijan **una** de las dos rutas.

### Ruta A: K3s en una VM (la que da la documentación de K3s)

```bash
curl -sfL https://get.k3s.io | sudo sh -
sudo k3s kubectl get nodes
```

Lean el script antes de correrlo, como en la sesión 2 de Linux. Para usar `kubectl` sin `sudo`:

```bash
mkdir -p ~/.kube && sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
sudo chown "$USER":"$USER" ~/.kube/config
kubectl get nodes
```

### Ruta B: K3s dentro de un contenedor

Para quien no tenga una VM libre. El clúster completo corre en **un contenedor privilegiado**: se borra con un comando y no deja nada en el sistema.

```bash
docker run -d --name k3s-lab --privileged \
  -v "$PWD:$PWD" -p 127.0.0.1:18081:80 \
  rancher/k3s:v1.37.2-k3s1 server
```

Las opciones, una por una: `--privileged` porque K3s necesita administrar redes y cgroups; `-v "$PWD:$PWD"` monta la carpeta de trabajo en la **misma ruta** dentro del contenedor, para que los archivos se vean igual; `-p 127.0.0.1:18081:80` deja el puerto 80 del clúster (donde escucha Traefik) en el 18081 de su equipo, solo local.

Como `kubectl` viene dentro de la imagen, se usa con una función que lo ejecuta ahí, en su carpeta actual:

```bash
kubectl() { docker exec -i -w "$PWD" k3s-lab kubectl "$@"; }
sleep 40 && kubectl get nodes
```

La función vale solo en esa terminal; si abren otra, la definen de nuevo.

### Comprueben

```console
$ kubectl get nodes
NAME           STATUS   ROLES           AGE   VERSION
913899f8ce8a   Ready    control-plane   50s   v1.37.2+k3s1
$ kubectl get pods -A
```

- **STATUS `Ready`.** Si dice `NotReady`, esperen un minuto y repitan.
- En `kube-system` deben ver `coredns`, `traefik`, `local-path-provisioner` y `metrics-server`. Los trae K3s; en otro tipo de clúster tendrían que instalarlos.
- También verán pods `helm-install-*` en `Completed` (uno se llama `helm-install-gateway-api-crd`): son trabajos de instalación de K3s que terminaron, no un problema.
- Anoten la versión que les salió: es la que citarán en su ADR.

---

## Paso 2 - Reconocer el terreno

```bash
kubectl config get-contexts
kubectl config current-context
kubectl get ns
kubectl api-resources | head -10
kubectl explain pod.spec.containers.imagePullPolicy
```

Contesten por escrito, con lo que ven:

1. ¿Cuántos namespaces hay y cuáles son?
2. ¿Qué valor por omisión tiene `imagePullPolicy` y de qué depende?
3. ¿`pods` es un recurso "namespaced"? ¿Y `nodes`? (la columna `NAMESPACED` de `api-resources` lo dice).

---

## Paso 3 - El `compose.yaml` de partida

Guárdenlo como `compose.yaml` en `~/k8s-lab/s01`:

```yaml
services:
  api:
    image: traefik/whoami:v1.10.3
    ports:
      - "8080:80"
    environment:
      WHOAMI_NAME: api-lab
    depends_on:
      - db
    restart: unless-stopped
  db:
    image: postgres:17
    environment:
      POSTGRES_PASSWORD: clave-de-laboratorio
      POSTGRES_DB: titulacion
    volumes:
      - datos:/var/lib/postgresql/data
volumes:
  datos:
```

No lo van a correr con Compose; es el documento que van a traducir. Con la tabla de traducción de la clase, anoten **antes de escribir YAML** qué línea va a pasar tal cual, cuál cambia y cuál desaparece.

---

## Paso 4 - Traducir la base de datos

`db-pod.yaml`:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: db
  labels:
    app: db
spec:
  containers:
    - name: postgres
      image: postgres:17
      env:
        - name: POSTGRES_PASSWORD
          value: clave-de-laboratorio
        - name: POSTGRES_DB
          value: titulacion
      ports:
        - containerPort: 5432
```

```bash
kubectl apply -f db-pod.yaml
kubectl get pods -w          # Ctrl+C cuando pase a Running
kubectl describe pod db
kubectl logs db | tail -5
```

- ¿Cuántos eventos tiene `describe`? ¿En qué orden pasan: `Scheduled`, `Pulling`, `Pulled`, `Created`, `Started`?
- En `logs` busquen la línea `database system is ready to accept connections`.
- Comprueben que la base `titulacion` existe, **dentro** del contenedor:

```bash
kubectl exec db -- psql -U postgres -d titulacion -c 'select 1 as ok;'
```

---

## Paso 5 - Traducir la API

`api-pod.yaml`:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: api
  labels:
    app: api
spec:
  containers:
    - name: api
      image: traefik/whoami:v1.10.3
      env:
        - name: WHOAMI_NAME
          value: api-lab
      ports:
        - containerPort: 80
```

```bash
kubectl apply -f api-pod.yaml
kubectl wait --for=condition=Ready pod/api --timeout=60s
kubectl port-forward pod/api 8080:80 &
sleep 2 && curl -s http://127.0.0.1:8080/ | head -5
kill %1
```

`port-forward` abre un túnel entre su equipo y el pod: sirve para probar, no para publicar. Con el `&` queda en segundo plano en su terminal; `kill %1` lo cierra.

La primera línea de la respuesta debe decir `Name: api-lab`: la variable de entorno llegó.

**Pregunta para el equipo:** en Compose, `ports: "8080:80"` hacía este mismo trabajo, ¿qué diferencia hay en **quién** puede entrar al puerto 8080?

---

## Paso 6 - Verlo fallar (1): un pod suelto no se repone

```bash
kubectl get pods
kubectl delete pod api
kubectl get pods
```

- `api` ya no aparece. Nadie lo recrea: usted declaró un pod, no "quiero una API siempre arriba".
- Vuelvan a crearlo: `kubectl apply -f api-pod.yaml`.
- Anoten qué hace `restart: unless-stopped` de Compose y qué **no** hace este pod cuando se le borra.

---

## Paso 7 - Verlo fallar (2): una imagen mal escrita

```bash
kubectl run roto --image=traefik/whoami:v9.9.9
sleep 25
kubectl get pod roto
kubectl describe pod roto | tail -8
```

Salida esperada del `get`: `ImagePullBackOff`. La causa está en los eventos, no en el estado:

```console
Warning  Failed  ...  Failed to pull image "traefik/whoami:v9.9.9": ... not found
```

1. ¿Qué palabra de ese mensaje dice por qué? (En K3s 1.37 el evento empieza con `spec.containers{roto}:` antes de `Failed to pull image`.)
2. Corrijan la etiqueta **sin borrar el pod**. La imagen de un contenedor sí se puede cambiar en un pod vivo (es de los pocos campos que aceptan cambio):

```bash
kubectl set image pod/roto roto=traefik/whoami:v1.10.3
sleep 10 && kubectl get pod roto
```

   Debe pasar a `Running`. Casi todo lo demás de un pod no se puede cambiar después de creado: para eso se usa un Deployment (sesión 2), que reemplaza pods en vez de parcharlos.

---

## Paso 8 - Verlo fallar (3): buscar la base por nombre

En Compose, la API conectaba a `db:5432`. Un pod de pruebas que haga de cliente:

```bash
kubectl run cliente --image=busybox:1.37 --command -- sleep 3600
kubectl wait --for=condition=Ready pod/cliente --timeout=60s
kubectl exec cliente -- nslookup db
```

Resultado esperado: `server can't find db.svc.cluster.local: NXDOMAIN` (el nombre de la última línea cambia entre versiones y con el dominio de búsqueda de su red; lo que importa es `NXDOMAIN`). Ahora por IP:

```bash
IP=$(kubectl get pod db -o jsonpath='{.status.podIP}')
echo "$IP"
kubectl exec cliente -- sh -c "nc -zv -w2 $IP 5432"
```

Debe decir `open`. Borren el pod `db`, vuélvanlo a crear con `kubectl apply -f db-pod.yaml` y repitan `jsonpath`: **la IP cambió**.

Conclusión escrita, en dos líneas: por qué `depends_on` y el nombre `db` no existen en esta forma, y qué pieza (sesión 2) lo resuelve.

---

## Paso 9 - Etiquetas y selectores

```bash
kubectl get pods --show-labels
kubectl get pods -l app=db
kubectl get pods -l 'app in (api,db)'
kubectl label pod db entorno=lab
kubectl get pods -l entorno=lab
kubectl label pod db entorno-
kubectl annotate pod db repo="https://example.com/titulacion"
kubectl describe pod db | grep -A2 -i annotations
```

- `label pod db entorno-` (con el guion final) **quita** la etiqueta.
- Escriban un selector que devuelva **solo** `api` y otro que devuelva `api` y `cliente` pero no `db`.

---

## Paso 10 - La comparación final

Llenen esta tabla, con una línea por fila:

| Compose | Pod | ¿Equivalente? | Qué falta |
|---|---|---|---|
| `image` | `image` | Sí | Evitar `latest` |
| `environment` | `env` | Parcial | Un ConfigMap o Secret (sesión 3) |
| `ports` | `containerPort` | No | Service o port-forward |
| `volumes` | (nada) | No | PVC (sesión 3) |
| `depends_on` | (nada) | No | Probes y reintentos |
| `restart` | `restartPolicy` | Parcial | Un controlador (sesión 2) |

Revisen que su tabla coincida con lo que vieron en los pasos 6 a 8: **cada "No" de la tabla tiene su falla**.

---

## Limpieza

```bash
kubectl delete pod api db roto cliente --ignore-not-found
```

Si usaron la ruta B y ya terminaron por hoy, borren todo el clúster:

```bash
docker rm -f k3s-lab
```

Cierren también el `kubectl port-forward` que hayan dejado (`jobs` y `kill %N`). Un clúster de laboratorio encendido sin usarse come memoria.

---

## Si algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `kubectl get nodes` se queda esperando | El clúster aún arranca | Esperar 40 s más y repetir |
| `The connection to the server ... was refused` | Contexto equivocado o K3s detenido | `kubectl config current-context`; `sudo systemctl status k3s` |
| Ruta B: `docker: Error response ... port is already allocated` | El 18081 está ocupado | Cambien el número de la izquierda en `-p` |
| Un pod se queda en `ContainerCreating` | La imagen sigue descargándose | `kubectl describe pod`: la sección Events lo dice |
| `Error from server (AlreadyExists)` | El pod ya existe | `kubectl delete pod <nombre>` y repetir |
