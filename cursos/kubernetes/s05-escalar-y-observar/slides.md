<!-- _class: lead -->

# S05 - Escalar y observar

### Kubernetes: del Compose al clúster

**Rogelio Arriaga - Raptor Dev Services**

<!--
Sesión de 5 horas. Hilo conductor: la API de las sesiones anteriores recibe una prueba de carga, y el clúster le agrega
réplicas solo. La clase es sobre medir: cuánto pide cada pod, cuánto usa, cuándo se agregan réplicas y si sirvió.
Versión de referencia: K3s v1.37.2+k3s1 (probado el 11 de noviembre de 2026). Las cifras de las salidas son de UNA
corrida de laboratorio con un solo nodo y poca carga: las de cada equipo serán distintas, y eso es lo que se mide.
-->

---

## El lema de hoy

> Escalar sin medir es gastar más sin saber si sirvió.

Hoy cada decisión de escala lleva dos mediciones: **antes** y **después**. Y una pregunta que se contesta con esos números: ¿cuántas réplicas necesita mi aplicación?

---

## Agenda

| Bloque | Qué hacemos |
|---|---|
| 1 | Peticiones y límites: lo que un pod pide y lo que puede usar |
| 2 | Medir: metrics-server y `kubectl top` |
| 3 | Autoescalado horizontal (HPA) |
| 4 | DaemonSets, Jobs y CronJobs |
| 5 | Logs y eventos: leer lo que pasó |
| 6 | Cómo se hace una prueba de carga que sirve |
| 7 | Práctica guiada: carga, escala y gráfica |
| 8 | Cierre, tarea y lo que sigue |

---

## Dónde estamos

En las sesiones 2 a 4 pusimos `replicas: 3` a mano, "porque sí".

| Pregunta | Respuesta hoy |
|---|---|
| ¿Por qué 3 réplicas? | Porque sonaba bien |
| ¿Cuánta CPU y memoria necesita cada una? | No se sabe |
| ¿Qué pasa si llega el triple de tráfico? | No se sabe |
| ¿Qué pasa si una aplicación se come la memoria del nodo? | Se llevaría a las demás |

Todo cambia con **números**.

---

<!-- _class: lead -->

# Parte 1
## Peticiones y límites

---

## Dos números por recurso

```yaml
resources:
  requests:
    cpu: 200m
    memory: 64Mi
  limits:
    cpu: 500m
    memory: 128Mi
```

| Campo | Para quién | Qué significa |
|---|---|---|
| `requests` | El **scheduler** | Cuánto se reserva; con ese dato elige el nodo |
| `limits` | El **kubelet** y el kernel | Hasta cuánto puede usar el contenedor |

- `200m` son 200 milicores: 0.2 de un núcleo. `64Mi` son 64 mebibytes.
- Un contenedor **puede** usar más que su `request` si el nodo tiene recursos libres.
- Si pones un `limit` y no un `request`, Kubernetes usa el límite como petición.

Fuente: kubernetes.io, "Resource Management for Pods and Containers"; Burns et al., *Kubernetes: Up and Running* (2022), cap. 5; Stencel, *Kubernetes Recipes*, cap. 6.

---

## Qué pasa al pasarse

| Recurso | Cuando el contenedor se pasa del límite |
|---|---|
| CPU | **Throttling**: el kernel le recorta CPU; no lo mata |
| Memoria | Puede ser terminado por el kernel (**OOM kill**) |

La documentación aclara que el límite de memoria se hace valer **de forma reactiva**: un contenedor que reserva de más no muere necesariamente al instante, sino cuando el kernel detecta presión de memoria.

- CPU y memoria **no son iguales**: pasarse de CPU hace lenta a la aplicación; pasarse de memoria la mata.
- Por eso se suele fijar el límite de memoria con cuidado y el de CPU con más holgura (o sin él), según el caso. **Es una decisión que se mide**, no una regla.

Fuente: kubernetes.io, "Resource Management for Pods and Containers".

---

## Verlo fallar: OOMKilled

Un pod con límite de 32 MiB que intenta reservar unos 100 MB:

```console
$ kubectl get pod oom
NAME   READY   STATUS      RESTARTS      AGE
oom    0/1     OOMKilled   2 (24s ago)   26s
$ kubectl describe pod oom | grep -E 'Reason|Exit Code'
      Reason:       OOMKilled
      Exit Code:    137
```

- `OOMKilled` y el código **137**: 128 más la señal 9 (SIGKILL).
- El kubelet lo **reinicia**, lo mata de nuevo, y los reinicios suben (`RESTARTS 2`): termina en `CrashLoopBackOff`.
- Los logs del contenedor muerto no siempre se pueden leer (`unable to retrieve container logs`): por eso el motivo se busca primero en `describe`.

<!--
Cifras de una corrida de laboratorio. El código 137 = 128 + 9 es la convención de Linux para "terminado por la señal 9".
-->

---

## Clases de calidad de servicio

Según los `requests` y `limits`, cada pod recibe una clase de **QoS**:

| Clase | Cuándo | Bajo presión del nodo |
|---|---|---|
| **Guaranteed** | Todos los contenedores con CPU y memoria, `requests` = `limits` | Los últimos en ser desalojados |
| **Burstable** | Algún contenedor con petición o límite, pero sin cumplir lo de Guaranteed | En medio |
| **BestEffort** | Ningún contenedor con petición ni límite | Los primeros en ser desalojados |

```console
$ kubectl get pod -l app=php-apache -o jsonpath='{.items[0].status.qosClass}'
Burstable
```

Con peticiones y límites **distintos**, la clase es Burstable. Con ambos iguales, Guaranteed. **Sin nada**, BestEffort: el primero en irse cuando el nodo se queda sin recursos.

Fuente: kubernetes.io, "Pod Quality of Service Classes".

---

<!-- _class: lead -->

# Parte 2
## Medir

---

## metrics-server y `kubectl top`

`kubectl top` muestra el uso **actual** de CPU y memoria de nodos y pods. Lo alimenta el **Metrics Server**, que expone la API `metrics.k8s.io`.

```console
$ kubectl top nodes
NAME           CPU(cores)   CPU(%)   MEMORY(bytes)   MEMORY(%)
c5dcc1bd39df   581m         4%       762Mi           2%
$ kubectl top pods
NAME                         CPU(cores)   MEMORY(bytes)
php-apache-8cdd76588-mqg2x   1m           9Mi
```

- K3s lo trae instalado; en otros clústeres hay que instalarlo.
- Justo después de crear un pod, `kubectl top pods` dice `error: metrics not available yet`: las métricas tardan en juntarse. Espera unos segundos.
- Es una **foto del momento**, no un historial. Para historial, Prometheus (nota `10-cloud/04-observabilidad.md`).

Fuente: kubernetes.io, "Horizontal Pod Autoscaling" (el Metrics Server provee `metrics.k8s.io`); Sachdeva, *CKA Study Companion* (2025), cap. 10.

---

<!-- _class: lead -->

# Parte 3
## Autoescalado horizontal

---

## El HPA

El **HorizontalPodAutoscaler** cambia el número de réplicas de un Deployment según una métrica. La fórmula de la documentación:

```text
réplicasDeseadas = ceil[ réplicasActuales × valorActual / valorDeseado ]
```

- El controlador revisa cada 15 segundos (valor por omisión del `kube-controller-manager`).
- Si la razón está muy cerca de 1.0, no hace nada: la tolerancia por omisión es 0.1.
- Con CPU, el "valor actual" es un **porcentaje de la petición** (`requests`) de cada contenedor.

Fuente: kubernetes.io, "Horizontal Pod Autoscaling"; Stencel, *Kubernetes Recipes*, cap. 6; Sachdeva (2025), cap. 10.

---

## Sin `requests`, no hay autoescalado

La documentación es explícita: si algún contenedor del pod no tiene la petición del recurso, la **utilización de CPU del pod no está definida** y el autoescalador **no actúa** sobre esa métrica.

```console
$ kubectl get hpa php-apache
NAME         REFERENCE               TARGETS              MINPODS   MAXPODS   REPLICAS
php-apache   Deployment/php-apache   cpu: <unknown>/50%   1         4         1
```

`<unknown>` en `TARGETS` es la señal. Dos causas habituales:

- Faltan los `requests` de CPU en el Deployment.
- Es el primer minuto: el Metrics Server todavía no tiene datos del pod (en la corrida, el HPA registró `FailedGetResourceMetric` al inicio y después se estabilizó).

---

## El manifiesto

```yaml {1-7|8-9|10-17}
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

- `minReplicas` y `maxReplicas`: el piso y el techo. **Sin techo** un error de carga o un bucle se llevan el presupuesto.
- `averageUtilization: 50`: mantener, en promedio, 50 % de la CPU **pedida**.
- Con un HPA, **ya no** se fija `replicas` en el Deployment: se pelearían.

---

## Una corrida real

Un Deployment `php-apache` (la imagen de ejemplo de la documentación de Kubernetes, `registry.k8s.io/hpa-example`), con `requests.cpu: 200m`, y un pod `carga` que le hace peticiones sin parar:

```console
13:58:25  cpu: 0%/50%    REPLICAS 1
13:58:45  cpu: 74%/50%   REPLICAS 1
13:59:05  cpu: 250%/50%  REPLICAS 2
13:59:25  cpu: 94%/50%   REPLICAS 4
```

```console
$ kubectl describe hpa php-apache | grep SuccessfulRescale
  Normal  SuccessfulRescale  69s  ...  New size: 2; reason: cpu resource utilization (percentage of request) above target
  Normal  SuccessfulRescale  54s  ...  New size: 4; reason: cpu resource utilization (percentage of request) above target
```

- Con 1 réplica y 74 %: ceil(1 × 74 / 50) = **2**. Coincide con la fórmula.
- Después, con 250 % y 2 réplicas, la fórmula pide más de 4: el HPA se **topó en `maxReplicas: 4`**.
- Estas cifras son de una corrida, en un nodo con poca carga. Las tuyas cambiarán.

---

## Escalar hacia abajo es más lento a propósito

- Al **subir**, el HPA actúa en cuanto la métrica lo pide.
- Al **bajar**, espera: la documentación habla de una ventana de estabilización de **5 minutos** por omisión (`--horizontal-pod-autoscaler-downscale-stabilization`), para no quitar réplicas por una caída pasajera.
- Con el campo `behavior` de `autoscaling/v2` se ajustan las dos velocidades por separado.
- En clase, esperar esos 5 minutos cuesta tiempo: se deja corriendo mientras se hace otra cosa y se mide **cuánto tardó**.

Fuente: kubernetes.io, "Horizontal Pod Autoscaling".

---

## Lo que el HPA no arregla

| Situación | Por qué |
|---|---|
| El cuello de botella es la base de datos | Más réplicas de la API = más conexiones contra la misma base |
| La aplicación guarda estado en memoria | Cada réplica tiene el suyo: se desincronizan |
| No hay nodos con capacidad | Los pods nuevos quedan `Pending`: hace falta más clúster |
| La aplicación tarda 90 s en arrancar | Cuando llega la réplica, el pico ya pasó |
| La métrica no representa la carga (CPU baja, pero la cola crece) | Se necesitan métricas propias |

Escalar nodos (el *cluster autoscaler*) y ajustar peticiones por pod (el VPA) son otras piezas; Sachdeva las presenta en el cap. 10 de su *CKA Study Companion* (2025). Aquí se mencionan, no se practican.

---

<!-- _class: lead -->

# Parte 4
## DaemonSets, Jobs y CronJobs

---

## Un pod por nodo, o hasta terminar

| Objeto | Para qué |
|---|---|
| **DaemonSet** | Un pod en **cada nodo** (o en los que elijas): agentes de logs, de métricas |
| **Job** | Correr algo **hasta que termine bien**, una o varias veces |
| **CronJob** | Un Job con horario |

Fuente: Burns et al., *Kubernetes: Up and Running* (2022), caps. 11 y 12; Sachdeva (2025), cap. 8.

```console
$ kubectl get ds agente
NAME     DESIRED   CURRENT   READY   UP-TO-DATE   AVAILABLE   NODE SELECTOR   AGE
agente   1         1         1       1            1           <none>          13s
```

Con un nodo, `DESIRED` es 1; con N nodos, N. Si entra un nodo nuevo, el DaemonSet le pone su pod.

---

## Job

```console
$ kubectl create job uno --image=busybox:1.37 -- sh -c 'echo hola desde el job; date +%Y'
job.batch/uno created
$ kubectl get job uno
NAME   STATUS     COMPLETIONS   DURATION   AGE
uno    Complete   1/1           2s         15s
$ kubectl logs job/uno
hola desde el job
2026
```

- Un Job se considera **terminado** cuando sus pods salieron con código 0.
- Si el pod falla, el Job lo reintenta (con `backoffLimit`).
- Es la forma de correr migraciones, respaldos o limpiezas: **fallan en voz alta** (como el script de la sesión 3 de Linux).

---

## CronJob

```yaml {1-5|6-8|9-17}
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

- `schedule`: el mismo formato de cinco campos de cron.
- `concurrencyPolicy: Forbid`: si la corrida anterior sigue, **no** lanza otra.
- `successfulJobsHistoryLimit`: cuántos Jobs terminados se conservan.
- En la corrida de laboratorio, a la primera hora marcada el CronJob creó su Job y el evento `SawCompletedJob` lo confirmó.

<!--
Comparar con los timers de systemd de la sesión 4 de Linux: misma necesidad, otro ejecutor. La pregunta es siempre dónde corre y quién lo vigila.
-->

---

<!-- _class: lead -->

# Parte 5
## Logs y eventos

---

## Leer lo que pasó

```bash
kubectl logs <pod>                       # lo que imprimió el contenedor
kubectl logs <pod> -f                    # seguirlo en vivo
kubectl logs <pod> --previous            # el contenedor anterior (tras un reinicio)
kubectl logs -l app=api --tail=20        # varios pods por etiqueta
kubectl logs job/uno                     # los de un Job
kubectl get events --sort-by=.lastTimestamp | tail
```

- Los logs **viven en el nodo**: si borras el pod (o el nodo se pierde), se van con él.
- Los **eventos** duran poco: son para lo reciente.
- Para historial hace falta enviar los logs a un sistema central (Seq, Loki, la nube): nota `10-cloud/04-observabilidad.md`.

Fuente: Burns et al. (2022), cap. 4; Stencel, *Kubernetes Recipes*, cap. 9.

---

## Qué observar de una aplicación en el clúster

| Señal | Pregunta | Dónde |
|---|---|---|
| Uso de CPU y memoria | ¿Cuánto consume cada pod? | `kubectl top` |
| Reinicios | ¿Algo se muere solo? | `kubectl get pods` (columna `RESTARTS`) |
| Eventos | ¿Qué hizo Kubernetes? | `kubectl describe`, `kubectl get events` |
| Logs | ¿Qué dijo la aplicación? | `kubectl logs` |
| Latencia y errores de la API | ¿Los usuarios están contentos? | Métricas de la aplicación (no las da Kubernetes) |

La última fila importa: **Kubernetes no sabe si su aplicación está sirviendo bien**; solo sabe si el proceso corre y si responde a las probes.

---

<!-- _class: lead -->

# Parte 6
## Una prueba de carga que sirve

---

## Receta

1. **Pregunta:** "¿cuántas réplicas necesita mi API para soportar X?".
2. **Línea base:** con carga mínima, anota CPU y memoria por pod y tiempo de respuesta.
3. **Carga controlada:** una herramienta que sube la carga en escalones y registra latencia y errores (en la práctica, un pod con un ciclo de `wget`; en serio, una herramienta de carga).
4. **Mide durante:** réplicas, CPU por pod, reinicios, errores.
5. **Para y mide después:** cuánto tardó en bajar.
6. **Conclusión con datos:** "con N réplicas la API aguantó X; a partir de Y, la latencia se disparó".

- Una sola corrida no demuestra nada: repite.
- La **carga del cliente** también tiene límites: un generador de carga mal hecho se vuelve el cuello de botella.
- La prueba se hace contra **tu** clúster, nunca contra un sistema ajeno.

---

## Qué se grafica

Una gráfica por pregunta, con el **tiempo** en el eje horizontal:

| Gráfica | Series |
|---|---|
| Escala | Número de réplicas, y la línea de `maxReplicas` |
| Uso | CPU promedio por pod frente a la línea del objetivo (50 %) |
| Experiencia | Latencia y errores de la API durante la prueba |

La tarea pide **una** gráfica con **una** conclusión: una frase que alguien pueda desmentir con números.

---

<!-- _class: lead -->

# Parte 7
## Práctica guiada

---

## Lo que haces en la práctica

1. Desplegar `php-apache` con peticiones y límites, ver su QoS y su uso con `kubectl top`.
2. Crear un HPA (objetivo 50 % de CPU, de 1 a 4 réplicas) y ver `<unknown>` antes de que haya datos.
3. Generar carga y **registrar cada 20 segundos** réplicas y CPU.
4. **Verlo fallar:** un pod que excede su límite de memoria y termina `OOMKilled`.
5. Un Job, un CronJob y un DaemonSet.
6. Con tus datos, la gráfica y su conclusión.

---

## Errores típicos de la sesión

| Error | Cómo se ve | Qué hacer |
|---|---|---|
| HPA sin `requests` de CPU | `<unknown>` para siempre | Poner `requests` |
| Sin `maxReplicas` razonable | Gasto desbordado | Techo según la capacidad |
| `limits` de memoria muy ajustados | `OOMKilled` en picos | Medir el pico real |
| Medir sin línea base | No se sabe si mejoró | Medir antes |
| Probar una sola vez | Resultado casual | Repetir |
| Escalar la API cuando el cuello es la base | Más réplicas, igual de lento | Medir dónde se atora |
| Fijar `replicas` y tener HPA | Se pelean | Quitar `replicas` del manifiesto |
| Creer que `kubectl top` es historial | Foto del momento | Prometheus para series |

---

## Cierre: lo que se llevan

- `requests` para el scheduler, `limits` para el kernel; pasarse de memoria mata, pasarse de CPU frena.
- El HPA calcula réplicas con una fórmula simple sobre **porcentaje de la petición**: sin petición, no actúa.
- Subir es rápido; bajar, lento a propósito.
- Un DaemonSet es un pod por nodo; un Job, hasta terminar; un CronJob, con horario.
- Una prueba de carga se mide **antes, durante y después**, y su conclusión puede desmentirse con los datos.

**Tarea:** la gráfica de su prueba, con su conclusión.

**Siguiente sesión:** operar y diagnosticar.

Libros de la sesión: Stencel, *Kubernetes Recipes*, caps. 6 y 9. Burns et al. (2022), caps. 11 y 12. Sachdeva (2025), caps. 8 a 10.
