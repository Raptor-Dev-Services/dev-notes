# Glosario - S02 - Deployments, Services e Ingress

> Los términos de la sesión 2, en orden alfabético.

---

## A

### Actualización gradual (rolling update)
**Categoría:** Kubernetes / Despliegue
**Definición:** Estrategia por omisión de un Deployment: reemplaza los pods en tandas, subiendo los nuevos y bajando los viejos conforme los nuevos quedan listos, para no cortar el servicio.
**En el curso:** La API pasó de `v1.10.2` a `v1.10.3` con `maxSurge: 1` y `maxUnavailable: 0`.

---

## C

### ClusterIP
**Categoría:** Kubernetes / Service
**Definición:** Tipo de Service por omisión: una IP virtual accesible solo dentro del clúster.
**En el curso:** El Service `api` es de este tipo.

---

## D

### Deployment
**Categoría:** Kubernetes / Carga de trabajo
**Definición:** Objeto que administra ReplicaSets para mantener N pods y actualizarlos de forma declarativa, con historial para regresar.
**En el curso:** `api` y `front`.

---

### diff (kubectl diff)
**Categoría:** kubectl / Comando
**Definición:** Muestra qué cambiaría en el clúster si se aplicara un archivo, sin aplicarlo. Sale con código 1 si hay diferencias y 0 si no.
**En el curso:** Se corre antes de cada `apply`.

---

### dry-run (servidor)
**Categoría:** kubectl / Comando
**Definición:** `--dry-run=server` envía el objeto al API para que lo valide y lo procese como si lo guardara, pero sin persistirlo. Atrapa errores que el cliente no ve.
**En el curso:** `kubectl apply --dry-run=server -f k8s/api.yaml`.

---

## E

### EndpointSlice
**Categoría:** Kubernetes / Red
**Definición:** Objeto con la lista de direcciones de los pods que respaldan un Service. Lo mantiene el controlador del Service. Sustituye a `Endpoints`, marcado como obsoleto desde v1.33.
**En el curso:** `ENDPOINTS` vacío significa que ningún pod Ready empata con el selector.

---

### ExternalName
**Categoría:** Kubernetes / Service
**Definición:** Tipo de Service que devuelve un alias DNS hacia un nombre fuera del clúster.
**En el curso:** Solo se menciona; no se usa.

---

## G

### Gateway API
**Categoría:** Kubernetes / Red
**Definición:** Familia de objetos (`GatewayClass`, `Gateway`, `HTTPRoute`) que la documentación de Kubernetes recomienda en lugar de Ingress. Separa los roles de quien administra la infraestructura y quien publica rutas.
**En el curso:** Se menciona como siguiente paso.

---

## I

### Ingress
**Categoría:** Kubernetes / Red
**Definición:** Objeto que describe cómo entra el tráfico HTTP desde fuera hacia los Services, por host y ruta. Sin un ingress controller no tiene efecto.
**En el curso:** `titulacion` envía `/api` a la API y `/` al front.

---

### Ingress controller
**Categoría:** Kubernetes / Red
**Definición:** Componente que cumple lo que dicen los objetos Ingress. En K3s es Traefik.
**En el curso:** `kubectl get ingressclass` lo muestra.

---

### IngressClass
**Categoría:** Kubernetes / Red
**Definición:** Objeto que nombra a un tipo de ingress controller. El campo `ingressClassName` del Ingress elige cuál lo atiende.
**En el curso:** `traefik`.

---

## L

### Liveness probe
**Categoría:** Kubernetes / Probes
**Definición:** Comprobación periódica de si el contenedor sigue vivo. Si falla, el kubelet lo reinicia.
**En el curso:** Debe comprobar al proceso, no a sus dependencias.

---

### LoadBalancer
**Categoría:** Kubernetes / Service
**Definición:** Tipo de Service que pide un balanceador de carga al proveedor de nube; en el laboratorio lo atiende ServiceLB de K3s.
**En el curso:** Traefik usa uno para recibir el tráfico.

---

## M

### Manifiesto
**Categoría:** Kubernetes / Concepto
**Definición:** Archivo YAML que describe un objeto del clúster.
**En el curso:** Los de la carpeta `k8s/`.

---

### maxSurge
**Categoría:** Kubernetes / Despliegue
**Definición:** Cuántos pods de más se permiten durante una actualización gradual. Por omisión, 25 %, redondeado hacia arriba.
**En el curso:** Valor 1 en la práctica.

---

### maxUnavailable
**Categoría:** Kubernetes / Despliegue
**Definición:** Cuántos pods pueden quedar sin servicio durante una actualización gradual. Por omisión, 25 %, redondeado hacia abajo.
**En el curso:** Valor 0 en la práctica.

---

## N

### NodePort
**Categoría:** Kubernetes / Service
**Definición:** Tipo de Service que además abre un puerto en cada nodo.
**En el curso:** Se menciona; en producción es más común un Ingress.

---

## P

### pathType
**Categoría:** Kubernetes / Ingress
**Definición:** Campo de cada ruta de un Ingress que dice cómo se compara: `Exact`, `Prefix` o `ImplementationSpecific`.
**En el curso:** `Prefix` en `/api` y en `/`.

---

### Probe
**Categoría:** Kubernetes / Probes
**Definición:** Comprobación que el kubelet hace sobre un contenedor: puede ser HTTP, TCP o un comando. Hay tres tipos: startup, liveness y readiness.
**En el curso:** `httpGet` a `/health`.

---

### progressDeadlineSeconds
**Categoría:** Kubernetes / Despliegue
**Definición:** Segundos que un Deployment espera sin progreso antes de marcar `Progressing` como falso. Vale 600 por omisión.
**En el curso:** `rollout status --timeout` es la espera que usamos nosotros.

---

## R

### Readiness probe
**Categoría:** Kubernetes / Probes
**Definición:** Comprobación de si el pod puede atender tráfico. Mientras falla, el pod sale de los destinos del Service; no se reinicia.
**En el curso:** La probe rota del front dejó el pod en `0/1`.

---

### ReplicaSet
**Categoría:** Kubernetes / Carga de trabajo
**Definición:** Controlador que mantiene un número estable de pods idénticos, elegidos por etiqueta. Normalmente lo crea un Deployment, no una persona.
**En el curso:** El hash del nombre del pod es el de la plantilla de su ReplicaSet.

---

### revisionHistoryLimit
**Categoría:** Kubernetes / Despliegue
**Definición:** Cuántos ReplicaSets viejos conserva un Deployment para poder regresar. Vale 10 por omisión.
**En el curso:** `kubectl rollout history`.

---

### rollout
**Categoría:** Kubernetes / Despliegue
**Definición:** El proceso de poner en marcha una versión nueva de un Deployment. Se vigila con `kubectl rollout status`.
**En el curso:** Con `--timeout` sirve de puerta en un pipeline.

---

### rollout undo
**Categoría:** kubectl / Comando
**Definición:** Regresa un Deployment a una revisión anterior, con otra actualización gradual.
**En el curso:** Se usó para volver de la probe rota.

---

## S

### Service
**Categoría:** Kubernetes / Red
**Definición:** Nombre y IP virtuales estables delante de un conjunto de pods elegidos por etiqueta; reparte entre los que están Ready.
**En el curso:** `api` y `front`.

---

### ServiceLB
**Categoría:** K3s / Red
**Definición:** Implementación de `LoadBalancer` que trae K3s para clústeres sin proveedor de nube.
**En el curso:** Por él Traefik recibe tráfico en el puerto 80.

---

### Startup probe
**Categoría:** Kubernetes / Probes
**Definición:** Comprobación de si la aplicación terminó de arrancar. Mientras no pase, las otras dos probes no corren.
**En el curso:** Útil para aplicaciones de arranque lento.

---

## T

### targetPort
**Categoría:** Kubernetes / Service
**Definición:** Puerto del contenedor al que el Service reenvía. Puede ser un número o el nombre de un puerto declarado.
**En el curso:** `targetPort: http`.

---

### Traefik
**Categoría:** K3s / Red
**Definición:** Ingress controller que K3s instala por omisión.
**En el curso:** Escucha en el puerto 80 del clúster.

---

*Rogelio Arriaga Gonzalez*
