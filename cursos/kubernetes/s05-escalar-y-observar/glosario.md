# Glosario - S05 - Escalar y observar

> Los términos de la sesión 5, en orden alfabético.

---

## B

### BestEffort
**Categoría:** Kubernetes / QoS
**Definición:** Clase de calidad de servicio de un pod sin peticiones ni límites en ningún contenedor. Es la primera en ser desalojada cuando el nodo se queda sin recursos.
**En el curso:** Un pod sin `resources`.

---

### Burstable
**Categoría:** Kubernetes / QoS
**Definición:** Clase de QoS de un pod con alguna petición o límite que no cumple lo de Guaranteed.
**En el curso:** `php-apache` (peticiones distintas de límites).

---

## C

### concurrencyPolicy
**Categoría:** Kubernetes / CronJob
**Definición:** Qué hacer si la corrida anterior sigue activa cuando toca otra: `Allow`, `Forbid` o `Replace`.
**En el curso:** `Forbid`.

---

### CPU throttling
**Categoría:** Linux / Recursos
**Definición:** Cuando un contenedor se acerca a su límite de CPU, el kernel le recorta el acceso; no lo mata.
**En el curso:** Se contrasta con el OOM kill.

---

### CronJob
**Categoría:** Kubernetes / Carga de trabajo
**Definición:** Crea Jobs según un horario con el formato de cinco campos de cron.
**En el curso:** `limpieza`.

---

## D

### DaemonSet
**Categoría:** Kubernetes / Carga de trabajo
**Definición:** Asegura un pod en cada nodo (o en los que se elijan). Un nodo nuevo recibe su pod.
**En el curso:** `agente`.

---

## G

### Guaranteed
**Categoría:** Kubernetes / QoS
**Definición:** Clase de QoS cuando cada contenedor tiene CPU y memoria con petición igual al límite. Es la última en ser desalojada.
**En el curso:** No se usa en la práctica.

---

## H

### HPA (HorizontalPodAutoscaler)
**Categoría:** Kubernetes / Escalado
**Definición:** Cambia el número de réplicas de un Deployment según una métrica, con la fórmula `ceil[réplicas × actual / deseado]`.
**En el curso:** `php-apache`.

---

## J

### Job
**Categoría:** Kubernetes / Carga de trabajo
**Definición:** Corre pods hasta que terminan bien; reintenta si fallan (`backoffLimit`).
**En el curso:** `uno`.

---

## L

### limits (límites)
**Categoría:** Kubernetes / Recursos
**Definición:** Lo máximo que un contenedor puede usar. Pasarse de CPU provoca throttling; pasarse de memoria puede terminar en OOM kill.
**En el curso:** `memory: 128Mi`.

---

## M

### Metrics Server
**Categoría:** Kubernetes / Observabilidad
**Definición:** Complemento que expone la API `metrics.k8s.io` con el uso de CPU y memoria. Alimenta a `kubectl top` y al HPA. K3s lo trae instalado.
**En el curso:** `kubectl top pods`.

---

### Milicore (m)
**Categoría:** Kubernetes / Recursos
**Definición:** Unidad de CPU: 1000m es un núcleo; `200m` es 0.2.
**En el curso:** `cpu: 200m`.

---

## O

### OOMKilled
**Categoría:** Kubernetes / Estado de pod
**Definición:** Contenedor terminado por el kernel por pasarse de su límite de memoria. Sale con código 137.
**En el curso:** Falla de la práctica.

---

## P

### Prueba de carga
**Categoría:** Método / Rendimiento
**Definición:** Generar tráfico controlado contra una aplicación propia para medir cómo responde y cómo escala. Se mide antes, durante y después.
**En el curso:** Sesión completa.

---

## Q

### QoS (calidad de servicio)
**Categoría:** Kubernetes / Recursos
**Definición:** Clase que Kubernetes asigna a cada pod según sus peticiones y límites; decide el orden de desalojo bajo presión.
**En el curso:** `status.qosClass`.

---

## R

### requests (peticiones)
**Categoría:** Kubernetes / Recursos
**Definición:** Cantidad de un recurso que se reserva para un contenedor; el scheduler la usa para elegir nodo y el HPA la usa como base del porcentaje de uso.
**En el curso:** `cpu: 200m`.

---

## T

### Tolerancia del HPA
**Categoría:** Kubernetes / Escalado
**Definición:** Margen alrededor de 1.0 dentro del cual el HPA no escala. Por omisión, 0.1.
**En el curso:** Evita oscilaciones.

---

## V

### Ventana de estabilización
**Categoría:** Kubernetes / Escalado
**Definición:** Tiempo que el HPA espera antes de bajar réplicas, para no reaccionar a una caída pasajera. La documentación da 5 minutos por omisión para la bajada.
**En el curso:** Se mide en el paso 4 de la práctica.

---

## Números

### 137 (código de salida)
**Categoría:** Linux / Procesos
**Definición:** Convención: 128 más el número de la señal que terminó al proceso. 137 es la señal 9 (SIGKILL).
**En el curso:** `OOMKilled`.

---

*Rogelio Arriaga Gonzalez*
