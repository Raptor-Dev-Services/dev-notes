# Práctica - S05 - Carga, escala y gráfica

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 2 horas, en equipo.

---

## Objetivo

Medir una aplicación **antes y después** de que el clúster la escale. Desplegar una aplicación con peticiones y límites, crear un autoescalador, generarle carga, registrar réplicas y CPU en el tiempo y sacar una conclusión con datos. Además, **verlo fallar**: un contenedor que se pasa de su límite de memoria y termina `OOMKilled`.

La prueba de carga se hace contra **su propio clúster**, nunca contra un sistema ajeno.

---

## Antes de empezar

- El clúster de la sesión 1 (K3s). En la ruta B, con memoria suficiente (unos 3 GB libres):

```bash
mkdir -p ~/k8s-lab/s05 && cd ~/k8s-lab/s05
docker run -d --name k3s-lab --privileged -v "$PWD:$PWD" rancher/k3s:v1.37.2-k3s1 server
kubectl() { docker exec -i -w "$PWD" k3s-lab kubectl "$@"; }
sleep 40 && kubectl get nodes
```

- **Ejemplo ilustrativo:** la aplicación es `registry.k8s.io/hpa-example`, la imagen de ejemplo que usa la documentación de Kubernetes para el autoescalado: un servidor web que gasta CPU en cada petición. No es una API real.
- Las cifras de las salidas de esta guía son de **una** corrida de laboratorio en un nodo con poca carga. Las suyas serán distintas, y esa es la gracia: **ustedes miden**.

---

## Paso 1 - La aplicación, con peticiones y límites

`apache.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: php-apache
spec:
  replicas: 1
  selector:
    matchLabels:
      app: php-apache
  template:
    metadata:
      labels:
        app: php-apache
    spec:
      containers:
        - name: php-apache
          image: registry.k8s.io/hpa-example
          ports:
            - containerPort: 80
          resources:
            requests:
              cpu: 200m
              memory: 64Mi
            limits:
              cpu: 500m
              memory: 128Mi
---
apiVersion: v1
kind: Service
metadata:
  name: php-apache
spec:
  selector:
    app: php-apache
  ports:
    - port: 80
```

```bash
kubectl apply -f apache.yaml
kubectl rollout status deployment/php-apache --timeout=180s
kubectl get pod -l app=php-apache -o jsonpath='{.items[0].status.qosClass}'; echo
sleep 30
kubectl top nodes
kubectl top pods
```

Si `kubectl top pods` dice `error: metrics not available yet`, esperen 20 segundos y repitan. Anoten **la línea base**: CPU y memoria por pod y del nodo, sin carga.

Contesten:

1. ¿Qué clase de QoS salió y por qué (comparen `requests` y `limits`)?
2. ¿Qué porcentaje de **su petición** de CPU usa el pod sin carga?
3. ¿Cuántos pods caben, de esta forma, en un nodo? (Hagan la cuenta con la CPU del nodo de `kubectl describe node`, sección `Allocatable`.)

---

## Paso 2 - El autoescalador

`hpa.yaml`:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: php-apache
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: php-apache
  minReplicas: 1
  maxReplicas: 4
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 50
```

```bash
kubectl apply -f hpa.yaml
sleep 20; kubectl get hpa php-apache
sleep 30; kubectl get hpa php-apache
```

La primera lectura puede mostrar `cpu: <unknown>/50%`; la segunda, `cpu: 0%/50%` (o un valor pequeño). Expliquen por escrito **por qué** el HPA dijo `<unknown>` al inicio. Consulten con:

```bash
kubectl describe hpa php-apache | sed -n '/Events/,$p'
```

---

## Paso 3 - La carga y el registro

Abran **dos terminales**. En la primera, el **registro** cada 20 segundos, en un archivo CSV (en la ruta A, quiten la función de `kubectl`; es la misma):

```bash
echo "hora,replicas,cpu_pct" > medicion.csv
while true; do
  R=$(kubectl get hpa php-apache -o jsonpath='{.status.currentReplicas}')
  C=$(kubectl get hpa php-apache -o jsonpath='{.status.currentMetrics[0].resource.current.averageUtilization}')
  echo "$(date +%T),$R,$C" | tee -a medicion.csv
  sleep 20
done
```

En la segunda, la carga: un pod que le hace peticiones sin parar.

```bash
kubectl run carga --image=busybox:1.37 --restart=Never --command -- \
  sh -c 'while true; do wget -q -O- http://php-apache >/dev/null; done'
```

Dejen correr **unos 3 minutos**. En una corrida de laboratorio, los números fueron:

| hora | réplicas | CPU (% de la petición) |
|---|---|---|
| 13:58:25 | 1 | 0 |
| 13:58:45 | 1 | 74 |
| 13:59:05 | 2 | 250 |
| 13:59:25 | 4 | 94 |

Comparen con lo que ustedes registran. Después:

```bash
kubectl get hpa php-apache
kubectl top pods
kubectl describe hpa php-apache | grep SuccessfulRescale
```

Contesten con sus datos:

1. ¿En qué momento pasó de 1 réplica a más, y cuántos segundos después de empezar la carga?
2. Con la fórmula `ceil[réplicas × actual / deseado]`, ¿cuántas réplicas pidió el HPA la primera vez? ¿Coincide con lo que se ve?
3. ¿Llegó a `maxReplicas`? ¿Qué habría pasado con `maxReplicas: 10`?
4. ¿Cuántos pods quedaron `Pending`, si alguno? ¿Por qué?

---

## Paso 4 - Parar la carga y esperar la bajada

```bash
kubectl delete pod carga
```

**No** detengan el registro. Dejen que corra al menos **6 minutos más**: el HPA espera antes de bajar réplicas (la documentación habla de una ventana de estabilización de 5 minutos por omisión). Anoten el momento en que paró la carga y el momento en que bajó la primera réplica: **cuánto tardó**.

Mientras esperan, hagan los pasos 5 y 6.

---

## Paso 5 - Verlo fallar: OOMKilled

`oom.yaml`:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: oom
spec:
  containers:
    - name: c
      image: busybox:1.37
      command: ["sh", "-c", "echo reservando memoria; x=$(head -c 100000000 /dev/zero | tr '\\0' a); echo no-llego-aqui; sleep 3600"]
      resources:
        requests:
          memory: 16Mi
        limits:
          memory: 32Mi
```

```bash
kubectl apply -f oom.yaml
sleep 25
kubectl get pod oom
kubectl describe pod oom | grep -E 'Reason|Exit Code'
kubectl logs oom --previous
kubectl get pod oom -o jsonpath='{.status.qosClass}'; echo
```

Esperado: `OOMKilled`, código `137`, y los reinicios subiendo (`RESTARTS`). El `logs --previous` puede fallar con `unable to retrieve container logs`: es normal en este caso.

Contesten:

1. ¿Qué significa el código 137?
2. ¿Qué clase de QoS tiene este pod? (Tiene petición y límite, pero distintos.)
3. ¿Qué cambiarían **en el manifiesto** para que no muera: el límite o la aplicación? ¿Con qué medida lo decidirían?

```bash
kubectl delete pod oom
```

---

## Paso 6 - Job, CronJob y DaemonSet

```bash
kubectl create job uno --image=busybox:1.37 -- sh -c 'echo hola desde el job; date +%Y'
sleep 15
kubectl get job uno
kubectl logs job/uno
```

`cron.yaml`:

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: limpieza
spec:
  schedule: "*/1 * * * *"
  concurrencyPolicy: Forbid
  successfulJobsHistoryLimit: 2
  jobTemplate:
    spec:
      template:
        spec:
          restartPolicy: OnFailure
          containers:
            - name: c
              image: busybox:1.37
              command: ["sh", "-c", "echo limpiando"]
```

```bash
kubectl apply -f cron.yaml
kubectl get cronjob limpieza
sleep 70
kubectl get jobs
kubectl get events --sort-by=.lastTimestamp | tail -4
```

`ds.yaml`:

```yaml
apiVersion: apps/v1
kind: DaemonSet
metadata:
  name: agente
spec:
  selector:
    matchLabels:
      app: agente
  template:
    metadata:
      labels:
        app: agente
    spec:
      containers:
        - name: c
          image: busybox:1.37
          command: ["sh", "-c", "while true; do echo latido; sleep 30; done"]
```

```bash
kubectl apply -f ds.yaml
sleep 12
kubectl get ds agente
kubectl logs ds/agente | head -2
```

Contesten: ¿por qué un Job es mejor que un `sleep` en un pod para un respaldo? ¿Qué mostraría `DESIRED` con tres nodos?

---

## Paso 7 - La gráfica y la conclusión

Con `medicion.csv` (hora, réplicas, CPU), hagan **una** gráfica con el tiempo en el eje horizontal y dos series: réplicas y CPU. Pueden usar una hoja de cálculo o un script. Marquen:

- el inicio y el fin de la carga;
- la línea del objetivo (50 %);
- `maxReplicas`.

Escriban **una conclusión** en una frase que alguien pueda desmentir con los datos, por ejemplo: "con esta carga la aplicación necesitó N réplicas y tardó S segundos en alcanzarlas". Y una segunda frase con **lo que la prueba no demuestra**.

---

## Limpieza

```bash
kubectl delete -f hpa.yaml -f apache.yaml -f cron.yaml -f ds.yaml --ignore-not-found
kubectl delete job uno --ignore-not-found
kubectl delete pod carga oom --ignore-not-found
docker rm -f k3s-lab        # solo ruta B, si ya terminaron
```

---

## Si algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `TARGETS` en `<unknown>` más de 2 minutos | Faltan `requests` de CPU, o el Metrics Server no está | `kubectl describe hpa`; `kubectl top pods` |
| El HPA no sube aunque hay carga | La carga no gasta CPU suficiente | Mirar `kubectl top pods` y aumentar la carga |
| Pods `Pending` | No hay CPU libre en el nodo para sus `requests` | `kubectl describe pod`: el evento `Insufficient cpu` lo dice |
| `ImagePullBackOff` con `registry.k8s.io/hpa-example` | Sin salida a internet | Revisar la red; probar otra imagen |
| La columna de CPU del CSV sale vacía | El HPA aún no tiene métrica | Esperar y repetir |
