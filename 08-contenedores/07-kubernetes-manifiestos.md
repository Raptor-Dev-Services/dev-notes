# 07 - Kubernetes: manifiestos para desplegar

> Fuente: *Kubernetes: Up and Running* (Burns, Beda, Hightower y Evenson, 2022): caps. 7 Service Discovery, 8 HTTP Load Balancing with Ingress, 9 ReplicaSets y 10 Deployments. *Kubernetes Recipes* (Stencel): cap. 2 Configuring Stateless Applications. *Certified Kubernetes Administrator Study Companion* (Sachdeva, 2025): caps. 5 y 6. Documentación oficial citada en Referencias.

## Problema que resuelve

[06-kubernetes-conceptos.md](06-kubernetes-conceptos.md) deja un pod suelto: sin controlador no se repone, sin Service no tiene nombre estable y sin Ingress nadie entra. Esta nota escribe los manifiestos para publicar una API y un front, actualizarlos sin cortar el servicio y regresar si la versión nueva falla.

Los ejemplos se probaron con K3s `v1.34.1+k3s1` el 11 de noviembre de 2026. La API de ejemplo es `traefik/whoami`, que responde con el nombre del pod que la atendió; **ejemplo ilustrativo**, no una API real.

---

## Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api
  labels:
    app: api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: api
    spec:
      containers:
        - name: api
          image: traefik/whoami:v1.10.3
          ports:
            - name: http
              containerPort: 80
          readinessProbe:
            httpGet:
              path: /health
              port: http
            periodSeconds: 3
          livenessProbe:
            httpGet:
              path: /health
              port: http
            initialDelaySeconds: 5
            periodSeconds: 10
```

| Campo | Qué hace |
|-------|----------|
| `replicas` | Cuántos pods debe haber |
| `selector.matchLabels` | Qué pods son suyos; **debe coincidir** con `template.metadata.labels` y no cambia después |
| `strategy.rollingUpdate` | Cómo se reemplazan los pods |
| `template` | La plantilla del pod. Solo un cambio aquí provoca una actualización |

El Deployment crea un **ReplicaSet** por cada versión de la plantilla. El nombre de un pod es `<deployment>-<hash de la plantilla>-<sufijo>`.

---

## Actualizar y regresar

```bash
kubectl diff -f k8s/api.yaml
kubectl apply -f k8s/api.yaml
kubectl rollout status deployment/api --timeout=120s
kubectl rollout history deployment/api
kubectl rollout undo deployment/api
```

| Campo | Por omisión | Significa |
|-------|-------------|-----------|
| `maxSurge` | 25 % (hacia arriba) | Pods de más permitidos durante el cambio |
| `maxUnavailable` | 25 % (hacia abajo) | Pods que pueden quedar sin servicio durante el cambio |
| `progressDeadlineSeconds` | 600 | Segundos sin progreso antes de marcar el despliegue como detenido |
| `revisionHistoryLimit` | 10 | ReplicaSets viejos que se conservan |

Los valores se comprobaron en el clúster con `kubectl get deploy -o jsonpath='{.spec.strategy}'` y con `kubectl get deploy -o yaml`. `rollout undo` regresa con **otra** actualización gradual, no con un salto. Cambiar `replicas` escala pero no actualiza.

Una actualización durante la que hubo tráfico de prueba mostró un `502` entre respuestas `200` en una corrida de laboratorio: la actualización gradual reduce el corte, no lo garantiza en cero. Cómo se apaga la aplicación (cierre ordenado de conexiones) importa.

---

## Probes

| Probe | Pregunta | Si falla |
|-------|----------|----------|
| `startupProbe` | ¿Terminó de arrancar? | Se reinicia el contenedor; las otras dos esperan |
| `livenessProbe` | ¿Sigue vivo? | El kubelet lo reinicia |
| `readinessProbe` | ¿Puede atender tráfico? | Sale de los destinos del Service; no se reinicia |

Una probe HTTP acepta cualquier código de 200 a 399. Con una readiness mal escrita (ruta inexistente, 404), el pod nuevo queda `0/1`, los viejos siguen atendiendo y `rollout status --timeout` termina en error: la probe evitó que una versión mala recibiera tráfico.

```console
$ kubectl describe pod front-5fcc468f4b-nxs97 | grep Unhealthy
  Warning  Unhealthy  ...  Readiness probe failed: HTTP probe failed with statuscode: 404
```

Una probe que siempre pasa no protege a nadie: `whoami` contesta 200 a cualquier ruta, y una readiness con una ruta inventada la deja pasar. Para saber si una probe sirve, apúntala a algo que deba fallar y comprueba que falla.

La liveness debe comprobar al **proceso**, no a sus dependencias: si depende de la base y la base se cae, Kubernetes reinicia toda la API sin remedio.

---

## Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: api
spec:
  selector:
    app: api
  ports:
    - name: http
      port: 80
      targetPort: http
```

- Nombre de DNS dentro del clúster: `api` (mismo namespace) o `api.default.svc.cluster.local`.
- Reparte entre los pods **Ready** que empatan con `selector`.
- `kubectl get endpointslices -l kubernetes.io/service-name=api` lista los destinos. Si sale vacío, o la etiqueta no empata o la readiness falla: es lo primero que se mira cuando el Service "no responde". `kubectl get endpoints` ya avisa que `Endpoints` está obsoleto desde v1.33.

| Tipo | Para qué |
|------|----------|
| `ClusterIP` | Solo dentro del clúster (por omisión) |
| `NodePort` | Además, un puerto en cada nodo |
| `LoadBalancer` | Un balanceador del proveedor de nube |
| `ExternalName` | Un alias DNS a un nombre de fuera |

---

## Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: titulacion
spec:
  ingressClassName: traefik
  rules:
    - host: titulacion.lab.local
      http:
        paths:
          - path: /api
            pathType: Prefix
            backend:
              service:
                name: api
                port:
                  name: http
          - path: /
            pathType: Prefix
            backend:
              service:
                name: front
                port:
                  name: http
```

- Un Ingress no hace nada sin un **ingress controller**. K3s trae Traefik, con la clase `traefik`.
- Sin dominio, se prueba con la cabecera `Host`: `curl -H 'Host: titulacion.lab.local' http://127.0.0.1:<puerto>/`. Sin la cabecera, Traefik devuelve 404: ninguna regla empata.
- La documentación de Kubernetes recomienda **Gateway API** en lugar de Ingress y dice que el API de Ingress está congelado, sin planes de quitarlo (a noviembre de 2026).

---

## Los manifiestos viven en el repositorio

```text
k8s/
  api.yaml          # Deployment + Service
  front.yaml
  ingress.yaml
```

```bash
kubectl apply --dry-run=server -f k8s/
kubectl diff -f k8s/
kubectl apply -f k8s/
kubectl rollout status deployment/api --timeout=120s
```

Un `kubectl edit` o un `kubectl scale` a mano deja el clúster distinto del repositorio. Después de un `rollout undo`, corrige también el archivo.

Imagen con versión (`v1.10.3`), nunca `latest`: con `latest` un cambio de imagen no cambia la plantilla y no dispara la actualización.

---

## Errores comunes

| Error | Qué pasa | Qué hacer |
|-------|----------|-----------|
| `selector` ≠ etiquetas del `template` | El API rechaza el Deployment | Alinearlos |
| Readiness con ruta o puerto equivocado | `0/1 Running`, despliegue detenido | Leer el evento `Readiness probe failed` |
| Liveness que depende de la base | Reinicios en cadena | Liveness solo del proceso |
| Service con `selector` que no empata | `ENDPOINTS` vacío | Comparar con `--show-labels` |
| Ingress sin controlador | Nada ocurre | `kubectl get ingressclass` |
| `latest` | La actualización no se dispara | Versión explícita |
| Corregir con `undo` y no el archivo | El repo contradice al clúster | Corregir el archivo y `kubectl diff` |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Deployment | Administra ReplicaSets para mantener N pods y actualizar de forma declarativa |
| ReplicaSet | Mantiene un número estable de pods idénticos |
| RollingUpdate | Reemplazo de pods en tandas controlado por `maxSurge` y `maxUnavailable` |
| Probe | Comprobación del kubelet: startup, liveness o readiness |
| Service | Nombre e IP estables delante de pods elegidos por etiqueta |
| EndpointSlice | Lista de direcciones de los pods de un Service |
| Ingress | Entrada HTTP por host y ruta hacia Services |
| Ingress controller | Componente que cumple los Ingress (Traefik en K3s) |
| Gateway API | Sucesor recomendado de Ingress |

---

## Referencias

- Kubernetes, "Deployments": https://kubernetes.io/docs/concepts/workloads/controllers/deployment/
- Kubernetes, "Configure Liveness, Readiness and Startup Probes": https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/
- Kubernetes, "Service": https://kubernetes.io/docs/concepts/services-networking/service/
- Kubernetes, "Ingress": https://kubernetes.io/docs/concepts/services-networking/ingress/ (nota de Gateway API y Ingress congelado, consultada el 11 de noviembre de 2026).
- Burns, B., Beda, J., Hightower, K. y Evenson, L. (2022). *Kubernetes: Up and Running* (3.ª ed.). O'Reilly.
- Stencel, G. *Kubernetes Recipes*. Apress.
- Sachdeva, P. (2025). *Certified Kubernetes Administrator Study Companion*. Apress.

---

*Rogelio Arriaga Gonzalez*
