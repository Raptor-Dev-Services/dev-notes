# Práctica - S03 - Configuración, secretos y datos

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 2 horas, en equipo.

---

## Objetivo

Separar la configuración (ConfigMap), el secreto (Secret) y los datos (volumen) de la aplicación; correr PostgreSQL como StatefulSet con un volumen persistente; y **verlo fallar**: borrar el pod y comprobar que los datos siguen, decodificar un Secret con un solo comando, encontrar el valor en la anotación `last-applied-configuration` y apuntar a un Secret que no existe.

---

## Antes de empezar

- El clúster de la sesión 1 (ruta A o B). En la ruta B, las mismas dos líneas de siempre:

```bash
mkdir -p ~/k8s-lab/s03 && cd ~/k8s-lab/s03
docker run -d --name k3s-lab --privileged -v "$PWD:$PWD" rancher/k3s:v1.34.1-k3s1 server
kubectl() { docker exec -i -w "$PWD" k3s-lab kubectl "$@"; }
sleep 40 && kubectl get nodes
```

- **Ejemplo ilustrativo:** la base `titulacion`, el usuario `titulacion`, la contraseña `clave-de-laboratorio` y la tabla de alumnos son de laboratorio. Nada de esto se usa fuera de ella.
- Anoten la StorageClass del clúster antes de empezar: `kubectl get storageclass`. Esperado en K3s: `local-path (default)`, `RECLAIMPOLICY Delete`, `VOLUMEBINDINGMODE WaitForFirstConsumer`.

---

## Paso 1 - Un ConfigMap y un Secret (el camino malo)

`config.yaml`:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: api-config
data:
  NIVEL_DE_LOG: informacion
  appsettings.json: |
    {
      "Titulacion": { "MaxIntentos": 3 }
    }
---
apiVersion: v1
kind: Secret
metadata:
  name: db-credenciales
type: Opaque
stringData:
  POSTGRES_USER: titulacion
  POSTGRES_PASSWORD: clave-de-laboratorio
```

```bash
kubectl apply -f config.yaml
kubectl get secret db-credenciales -o yaml
```

**Verlo fallar (1): el Secret no está cifrado.**

```bash
kubectl get secret db-credenciales -o jsonpath='{.data.POSTGRES_PASSWORD}'; echo
kubectl get secret db-credenciales -o jsonpath='{.data.POSTGRES_PASSWORD}' | base64 -d; echo
```

La primera línea imprime algo como `Y2xhdmUtZGUtbGFib3JhdG9yaW8=`; la segunda, `clave-de-laboratorio`. Anoten **cuántos comandos** hicieron falta para leer la contraseña y qué permiso tuvieron que tener para hacerlo.

**Verlo fallar (2): la anotación.** En la salida de `get secret -o yaml` busquen `kubectl.kubernetes.io/last-applied-configuration`. Esa anotación lleva el manifiesto original, con `"POSTGRES_PASSWORD":"clave-de-laboratorio"` en texto claro.

Cuéntenlo en dos líneas: por qué un `apply` de un YAML con valores secretos deja una segunda copia, y por qué eso importa si alguien puede leer el objeto.

---

## Paso 2 - El Secret bien creado

Borren el Secret y créenlo sin que su valor pase por ningún archivo:

```bash
kubectl delete secret db-credenciales
kubectl create secret generic db-credenciales \
  --from-literal=POSTGRES_USER=titulacion \
  --from-literal=POSTGRES_PASSWORD='clave-de-laboratorio'
kubectl get secret db-credenciales -o yaml | grep -c last-applied
```

El conteo debe dar `0`: `create` no guarda esa anotación. (El historial de su shell sí guarda el comando: en producción la contraseña saldría de un gestor y no se escribiría a mano.)

Quiten del `config.yaml` el Secret (dejen solo el ConfigMap) y escriban en un comentario por qué.

---

## Paso 3 - Consumirlos desde un pod

`consumidor.yaml` (usa `busybox` porque la imagen de `whoami` no trae shell):

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: consumidor
spec:
  replicas: 1
  selector:
    matchLabels:
      app: consumidor
  template:
    metadata:
      labels:
        app: consumidor
    spec:
      containers:
        - name: consumidor
          image: busybox:1.37
          command: ["sleep", "3600"]
          envFrom:
            - configMapRef:
                name: api-config
          env:
            - name: DB_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: db-credenciales
                  key: POSTGRES_PASSWORD
          volumeMounts:
            - name: config
              mountPath: /etc/api
              readOnly: true
      volumes:
        - name: config
          configMap:
            name: api-config
            items:
              - key: appsettings.json
                path: appsettings.json
```

```bash
kubectl apply -f consumidor.yaml
kubectl rollout status deployment/consumidor --timeout=60s
P=$(kubectl get pod -l app=consumidor -o name | head -1)
kubectl exec $P -- sh -c 'echo nivel=$NIVEL_DE_LOG; echo pw=$DB_PASSWORD; ls /etc/api; cat /etc/api/appsettings.json'
```

Debe verse `nivel=informacion`, la contraseña, el archivo `appsettings.json` y su contenido.

**¿Qué se entera de un cambio?** Cambien el ConfigMap y miren las dos formas de consumo:

```bash
kubectl patch configmap api-config --type merge \
  -p '{"data":{"NIVEL_DE_LOG":"depuracion","appsettings.json":"{ \"Titulacion\": { \"MaxIntentos\": 5 } }\n"}}'
for i in 1 2 3 4 5 6; do
  kubectl exec $P -- sh -c 'echo "variable=$NIVEL_DE_LOG"; grep -o "MaxIntentos[^}]*" /etc/api/appsettings.json'
  sleep 20
done
```

Anoten, **con lo que vean**: ¿cambió la variable? ¿cambió el archivo? ¿cuántos segundos tardó? La documentación afirma que los ConfigMaps montados se actualizan solos y que las variables se fijan al crear el contenedor; ustedes lo comprueban. Después:

```bash
kubectl rollout restart deployment/consumidor
kubectl rollout status deployment/consumidor --timeout=60s
P=$(kubectl get pod -l app=consumidor -o name | head -1)
kubectl exec $P -- sh -c 'echo variable=$NIVEL_DE_LOG'
```

Con el reinicio, la variable ya debe decir `depuracion`.

---

## Paso 4 - Verlo fallar (3): un Secret que no existe

```bash
sed 's/name: db-credenciales/name: db-credencial/; s/name: consumidor/name: roto/; s/app: consumidor/app: roto/' consumidor.yaml > roto.yaml
kubectl apply -f roto.yaml
sleep 12
kubectl get pods -l app=roto
kubectl describe pod -l app=roto | tail -5
```

Esperado: `CreateContainerConfigError`, y un evento como `Error: secret "db-credencial" not found`. ¿Qué letra falta en el nombre?

```bash
kubectl delete -f roto.yaml
```

---

## Paso 5 - PostgreSQL con volumen

`db.yaml`:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: db
spec:
  clusterIP: None
  selector:
    app: db
  ports:
    - name: pg
      port: 5432
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: db
spec:
  serviceName: db
  replicas: 1
  selector:
    matchLabels:
      app: db
  template:
    metadata:
      labels:
        app: db
    spec:
      containers:
        - name: postgres
          image: postgres:17
          env:
            - name: POSTGRES_DB
              value: titulacion
            - name: PGDATA
              value: /var/lib/postgresql/data/pgdata
          envFrom:
            - secretRef:
                name: db-credenciales
          ports:
            - name: pg
              containerPort: 5432
          readinessProbe:
            exec:
              command: ["pg_isready", "-U", "titulacion", "-d", "titulacion"]
            periodSeconds: 5
          volumeMounts:
            - name: datos
              mountPath: /var/lib/postgresql/data
  volumeClaimTemplates:
    - metadata:
        name: datos
      spec:
        accessModes: ["ReadWriteOnce"]
        resources:
          requests:
            storage: 1Gi
```

```bash
kubectl apply -f db.yaml
kubectl get pvc
kubectl rollout status statefulset/db --timeout=120s
kubectl get pods,pvc,pv
```

Notas del paso:

- Es normal ver un aviso `spec.SessionAffinity is ignored for headless services`: el Service sin IP lo ignora.
- La PVC `datos-db-0` puede mostrarse `Pending` un instante: `WaitForFirstConsumer`. Debe pasar a `Bound` cuando el pod arranca.
- El manifiesto usa `postgres:17`. Con PostgreSQL 18 o superior la ruta de datos de la imagen cambió: no reusen este archivo sin leer la página de la imagen.

Creen datos:

```bash
kubectl exec db-0 -- psql -U titulacion -d titulacion \
  -c "create table alumnos(id int primary key, nombre text); insert into alumnos values (1,'Ana');"
```

---

## Paso 6 - La prueba: borrar el pod

```bash
kubectl delete pod db-0
kubectl wait --for=condition=Ready pod/db-0 --timeout=120s
kubectl exec db-0 -- psql -U titulacion -d titulacion -c "select * from alumnos;"
```

Debe mostrar la fila de Ana. Contesten: ¿qué nombre tiene el pod nuevo? ¿Qué objeto "recordó" el volumen?

Ahora comparen con la sesión 1, donde la base era un pod suelto sin volumen: escriban una línea sobre qué habría pasado con los datos.

---

## Paso 7 - Verlo fallar (4): qué se conserva y qué no

```bash
kubectl delete statefulset db
kubectl get pods,pvc
```

La PVC `datos-db-0` **sigue ahí**: borrar el StatefulSet no borra los volúmenes. Recréenlo y miren si encuentra sus datos:

```bash
kubectl apply -f db.yaml
kubectl wait --for=condition=Ready pod/db-0 --timeout=120s
kubectl exec db-0 -- psql -U titulacion -d titulacion -c "select * from alumnos;"
```

Ahora el paso destructivo, **solo en el laboratorio**:

```bash
kubectl delete statefulset db
kubectl delete pvc datos-db-0
kubectl get pv
kubectl apply -f db.yaml
kubectl wait --for=condition=Ready pod/db-0 --timeout=120s
kubectl exec db-0 -- psql -U titulacion -d titulacion -c "select * from alumnos;"
```

Esperado: el `select` falla con `relation "alumnos" does not exist`. Con `RECLAIMPOLICY Delete`, borrar la PVC borró el volumen y los datos. Anoten dónde dice la StorageClass que eso pasaría.

---

## Paso 8 - Conclusión escrita

Para entregar al final, en cuatro líneas:

1. ¿Qué protege el volumen y qué **no** protege (compárenlo con un respaldo)?
2. ¿Dónde vive la contraseña que usó su práctica y dónde debería vivir en producción?
3. ¿Su base de datos real viviría en el clúster? ¿Quién la respalda y quién la restaura?

---

## Limpieza

```bash
kubectl delete statefulset db --ignore-not-found
kubectl delete pvc --all
kubectl delete -f config.yaml -f consumidor.yaml --ignore-not-found
kubectl delete secret db-credenciales --ignore-not-found
docker rm -f k3s-lab        # solo ruta B, si ya terminaron
```

---

## Si algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| El pod `db-0` en `CrashLoopBackOff` | Variables mal puestas o volumen con contenido previo | `kubectl logs db-0`; leer la causa en el log |
| PVC `Pending` con pod ya creado | No hay StorageClass por omisión | `kubectl get storageclass` |
| `psql: FATAL: password authentication failed` | Usuario o contraseña distintos de los del Secret | Revisar el Secret; el Secret **no** actualiza a una base ya inicializada |
| La variable no cambia | Se fija al crear el contenedor | `rollout restart` |
| Los datos no están tras recrear | Se borró la PVC | Ver el paso 7 |
