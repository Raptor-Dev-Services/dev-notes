# Práctica - S06 - Cinco fallas sembradas

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 2 horas, en equipo.

---

## Objetivo

Aplicar el **método de diagnóstico** a cinco fallas sembradas: una imagen mal escrita, un selector que no empata, un volumen pendiente, un CrashLoopBackOff y una NetworkPolicy que corta. Para cada una: ver el síntoma, encontrar la **causa** con el método (no adivinando), corregirla y comprobar que quedó. Al final, con lo aprendido, escribir el borrador del runbook de su aplicación.

Cada falla vive en su propio namespace (`falla1` a `falla5`) para no estorbarse.

---

## Antes de empezar

- El clúster de la sesión 1 (K3s). En la ruta B:

```bash
mkdir -p ~/k8s-lab/s06 && cd ~/k8s-lab/s06
docker run -d --name k3s-lab --privileged -v "$PWD:$PWD" rancher/k3s:v1.37.2-k3s1 server
kubectl() { docker exec -i -w "$PWD" k3s-lab kubectl "$@"; }
sleep 40 && kubectl get nodes
for n in 1 2 3 4 5; do kubectl create namespace falla$n; done
```

- **Ejemplo ilustrativo:** la aplicación es `nginx`; las fallas son inventadas para la práctica. Las salidas son de K3s `v1.37.2+k3s1`.
- **Regla de la práctica:** antes de abrir la sección "Soluciones" del final, cada equipo escribe, **por falla**, (1) el síntoma, (2) los comandos que corrió **en orden**, (3) la causa y (4) el arreglo. Si adivinaron, no cuenta: tienen que poder decir qué línea de la salida los llevó a la causa.
- Un solo cambio a la vez, y siempre **desde el archivo**: corrijan `fallaN.yaml` y vuelvan a aplicar.

---

## El método (tarjeta)

1. `kubectl get pods -o wide`: ¿qué STATUS?
2. `kubectl describe pod <pod>`: sección **Events**.
3. `kubectl logs <pod>` y `kubectl logs <pod> --previous`.
4. `kubectl get endpointslices -l kubernetes.io/service-name=<svc>`: ¿hay destinos?
5. Un pod de pruebas: `kubectl run cli --image=busybox:1.37 --command -- sleep 3600`, y `wget` desde él.
6. `kubectl get nodes`, `kubectl get networkpolicy`, `kubectl describe pvc`.

---

## Falla 1

`falla1.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: web
spec:
  replicas: 2
  selector:
    matchLabels:
      app: web
  template:
    metadata:
      labels:
        app: web
    spec:
      containers:
        - name: web
          image: nginx:1.27-alpne
          ports:
            - name: http
              containerPort: 80

---
apiVersion: v1
kind: Service
metadata:
  name: web
spec:
  selector:
    app: web
  ports:
    - name: http
      port: 80
      targetPort: http
```

```bash
kubectl -n falla1 apply -f falla1.yaml
sleep 30
kubectl -n falla1 get pods
```

Diagnostiquen, corrijan y comprueben con `kubectl -n falla1 rollout status deployment/web`.

---

## Falla 2

`falla2.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: web
spec:
  replicas: 2
  selector:
    matchLabels:
      app: web
  template:
    metadata:
      labels:
        app: web
    spec:
      containers:
        - name: web
          image: nginx:1.27-alpine
          ports:
            - name: http
              containerPort: 80

---
apiVersion: v1
kind: Service
metadata:
  name: web
spec:
  selector:
    app: webb
  ports:
    - name: http
      port: 80
      targetPort: http
```

```bash
kubectl -n falla2 apply -f falla2.yaml
kubectl -n falla2 rollout status deployment/web --timeout=90s
kubectl -n falla2 run cli --image=busybox:1.37 --command -- sleep 3600
kubectl -n falla2 wait --for=condition=Ready pod/cli --timeout=60s
kubectl -n falla2 exec cli -- wget -qO- -T 3 http://web/
```

Los pods están `Running` y `1/1`, y aun así la última orden falla. Diagnostiquen y corrijan.

---

## Falla 3

`falla3.yaml`:

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: datos
spec:
  storageClassName: rapida
  accessModes: ["ReadWriteOnce"]
  resources:
    requests:
      storage: 1Gi
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: web
spec:
  replicas: 2
  selector:
    matchLabels:
      app: web
  template:
    metadata:
      labels:
        app: web
    spec:
      containers:
        - name: web
          image: nginx:1.27-alpine
          ports:
            - name: http
              containerPort: 80
          volumeMounts:
            - name: datos
              mountPath: /usr/share/nginx/html
      volumes:
        - name: datos
          persistentVolumeClaim:
            claimName: datos
---
apiVersion: v1
kind: Service
metadata:
  name: web
spec:
  selector:
    app: web
  ports:
    - name: http
      port: 80
      targetPort: http
```

```bash
kubectl -n falla3 apply -f falla3.yaml
sleep 20
kubectl -n falla3 get pods,pvc
```

Diagnostiquen: ¿qué objeto dice la causa, el pod o la PVC? Corrijan sabiendo que el campo `storageClassName` de una PVC **no se puede cambiar** una vez creada: tendrán que borrar y volver a crear. (Ojo: el Deployment que la usa también.)

---

## Falla 4

`falla4.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: web
spec:
  replicas: 2
  selector:
    matchLabels:
      app: web
  template:
    metadata:
      labels:
        app: web
    spec:
      containers:
        - name: web
          image: nginx:1.27-alpine
          ports:
            - name: http
              containerPort: 80
          command: ["sh", "-c", "test -n \"$DB_HOST\" || { echo \"falta la variable DB_HOST\"; exit 1; }; exec nginx -g \"daemon off;\""]
---
apiVersion: v1
kind: Service
metadata:
  name: web
spec:
  selector:
    app: web
  ports:
    - name: http
      port: 80
      targetPort: http
```

```bash
kubectl -n falla4 apply -f falla4.yaml
sleep 30
kubectl -n falla4 get pods
```

Diagnostiquen. Pista de método: ¿el STATUS dice la causa? ¿Qué comando sí? (Si ven `Error` en lugar de `CrashLoopBackOff`, esperen unos segundos: el kubelet aún no ha acumulado reinicios.)

---

## Falla 5

`falla5.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: web
spec:
  replicas: 2
  selector:
    matchLabels:
      app: web
  template:
    metadata:
      labels:
        app: web
    spec:
      containers:
        - name: web
          image: nginx:1.27-alpine
          ports:
            - name: http
              containerPort: 80

---
apiVersion: v1
kind: Service
metadata:
  name: web
spec:
  selector:
    app: web
  ports:
    - name: http
      port: 80
      targetPort: http
---
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: solo-lo-necesario
spec:
  podSelector:
    matchLabels:
      app: web
  policyTypes: [Ingress]
  ingress:
    - from:
        - podSelector:
            matchLabels:
              app: frontal
      ports:
        - protocol: TCP
          port: 8080
```

```bash
kubectl -n falla5 apply -f falla5.yaml
kubectl -n falla5 rollout status deployment/web --timeout=90s
kubectl -n falla5 run frontal --image=busybox:1.37 --labels=app=frontal --command -- sleep 3600
kubectl -n falla5 wait --for=condition=Ready pod/frontal --timeout=60s
kubectl -n falla5 exec frontal -- wget -qO- -T 3 http://web/
```

Los pods están `1/1`, el Service tiene destinos y aun así no entra. Diagnostiquen: ¿hay **una** causa o **dos**? (Una pista: el pod de pruebas lleva la etiqueta que la política espera.)

---

## Dos extras (Pending por recursos y nodo cordonado)

```bash
kubectl run grande --image=nginx:1.27-alpine \
  --overrides='{"spec":{"containers":[{"name":"grande","image":"nginx:1.27-alpine","resources":{"requests":{"cpu":"64"}}}]}}'
sleep 5
kubectl get pod grande
kubectl describe pod grande | tail -3
kubectl delete pod grande
```

Después, con cuidado (es su laboratorio de un solo nodo):

```bash
N=$(kubectl get nodes -o name | head -1)
kubectl cordon $N
kubectl run nuevo --image=nginx:1.27-alpine
sleep 4
kubectl get pod nuevo
kubectl describe pod nuevo | tail -2
kubectl uncordon $N
kubectl delete pod nuevo
```

Anoten qué dice cada evento. Los dos pods están `Pending`: ¿en qué se distinguen las causas?

---

## Un contenedor efímero de depuración

```bash
P=$(kubectl -n falla2 get pod -l app=web -o name | head -1 | cut -d/ -f2)
kubectl -n falla2 debug -it pod/$P --image=busybox:1.37 --target=web -- sh
```

Dentro: `wget -qO- http://127.0.0.1/ | head -c 25` y `ls /proc | head`. Salgan con `exit`. Contesten: ¿por qué `127.0.0.1` llega a nginx? ¿Qué cambió en el pod original?

---

## Soluciones (no abrir antes de tiempo)

| Falla | Síntoma | Pista que la distingue | Causa | Arreglo |
|---|---|---|---|---|
| 1 | `ErrImagePull` / `ImagePullBackOff` | `describe`: `Failed to pull image "nginx:1.27-alpne" ... not found` | La etiqueta tiene una letra de menos (`alpne`) | Corregir a `1.27-alpine` y aplicar |
| 2 | Pods `1/1`; `Connection refused` desde el Service | `get endpointslices`: `ENDPOINTS <unset>`; el `selector` del Service es `app: webb` y los pods llevan `app=web` | Selector mal escrito | Poner `app: web` en el Service |
| 3 | Pods `Pending` | `describe pvc`: `storageclass.storage.k8s.io "rapida" not found`; en el pod, `unbound immediate PersistentVolumeClaims` | La StorageClass pedida no existe | Borrar Deployment y PVC y recrear con una StorageClass que exista (`kubectl get storageclass`) o sin `storageClassName` para usar la de omisión |
| 4 | `CrashLoopBackOff` | `logs`: `falta la variable DB_HOST`; código de salida 1 | El contenedor sale con error al no tener la variable | Agregar `DB_HOST` (con `env` en el Deployment) |
| 5 | Pods `1/1`, endpoints presentes, `Connection refused` | `describe networkpolicy`: permite solo el puerto 8080 desde `app=frontal` | La política permite el puerto 8080 y nginx escucha en el 80 | Cambiar el puerto de la política a 80 |

Para cada una, comprueben el arreglo con el mismo comando que mostró el síntoma.

---

## Su propio runbook (borrador)

Con lo que hicieron, escriban una primera versión de `docs/runbook.md` para **su** aplicación, con las secciones de la clase: síntoma, primera lectura, árbol de decisión, causas conocidas, cuándo escalar, cómo regresar y última revisión. La tarea lo termina y lo prueba.

---

## Limpieza

```bash
kubectl delete namespace falla1 falla2 falla3 falla4 falla5 --ignore-not-found
docker rm -f k3s-lab        # solo ruta B, si ya terminaron
```

---

## Si algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| La falla 1 se ve bien | La imagen ya la tenía en caché con ese nombre | Cambiar la etiqueta por otra inexistente |
| `kubectl debug` pide una imagen y se queda esperando | Sin salida para bajar `busybox` | Usar una imagen que ya esté en el nodo |
| `can't connect` en la falla 2 incluso tras corregir | Falta esperar a que se actualicen los endpoints | `kubectl get endpointslices` y repetir |
| No pueden borrar la PVC de la falla 3 | La usa un pod | Borrar primero el Deployment |
