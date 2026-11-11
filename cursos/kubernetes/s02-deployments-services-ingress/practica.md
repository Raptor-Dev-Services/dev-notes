# Práctica - S02 - Publicar, actualizar y regresar

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 2 horas, en equipo.

---

## Objetivo

Desplegar una API y un front con Deployments, Services y un Ingress; publicar una versión nueva sin cortar el servicio; **verlo fallar** con una readiness probe mal escrita que detiene el despliegue, y regresar a la versión buena. Todo desde archivos en una carpeta `k8s/` versionada, sin `kubectl edit` ni `kubectl scale` a mano.

---

## Antes de empezar

- El clúster de la sesión 1. Si lo borraron, levántenlo de nuevo (paso 1 de la práctica 1). Ruta B (K3s en contenedor), con el puerto 80 del clúster expuesto en el 18081 de su equipo:

```bash
mkdir -p ~/k8s-lab/s02/k8s && cd ~/k8s-lab/s02
docker run -d --name k3s-lab --privileged \
  -v "$PWD:$PWD" -p 127.0.0.1:18081:80 \
  rancher/k3s:v1.37.2-k3s1 server
kubectl() { docker exec -i -w "$PWD" k3s-lab kubectl "$@"; }
sleep 40 && kubectl get nodes
```

  Ruta A (K3s en VM): `kubectl` normal, y para el Ingress usen la IP de la VM en lugar de `127.0.0.1:18081` (el puerto es el 80).
- Git inicializado en `~/k8s-lab/s02`: `git init`.
- **Ejemplo ilustrativo:** la API es `traefik/whoami`, que responde con el nombre del pod que la atendió; el front es `nginx`. Los nombres de dominio (`titulacion.lab.local`) son inventados y no existen en ningún DNS: se prueba con la cabecera `Host`.

---

## Paso 1 - La API

Guarden como `k8s/api.yaml`:

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
          image: traefik/whoami:v1.10.2
          env:
            - name: WHOAMI_NAME
              value: api-lab
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
          resources:
            requests:
              cpu: 50m
              memory: 16Mi
            limits:
              memory: 64Mi
---
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

Primero preguntan, luego aplican:

```bash
kubectl apply --dry-run=server -f k8s/api.yaml
kubectl diff -f k8s/api.yaml | head -5
kubectl apply -f k8s/api.yaml
kubectl rollout status deployment/api --timeout=120s
kubectl get deploy,rs,pods,svc -l app=api
kubectl get endpointslices -l kubernetes.io/service-name=api
```

Contesten:

1. ¿Cuántos ReplicaSets hay y cuántos pods tiene?
2. En el nombre de un pod, ¿qué parte es el hash de la plantilla?
3. ¿Cuántas direcciones salen en `ENDPOINTS`?

(`resources` aparece ya: las sesiones 3 y 5 explican `requests` y `limits`; por ahora déjenlo.)

---

## Paso 2 - Quién contesta por nombre

```bash
kubectl run cliente --image=busybox:1.37 --command -- sleep 3600
kubectl wait --for=condition=Ready pod/cliente --timeout=60s
for i in 1 2 3 4 5 6; do kubectl exec cliente -- wget -qO- http://api/ | grep Hostname; done
kubectl exec cliente -- wget -qO- http://api.default.svc.cluster.local/ | head -2
kubectl exec cliente -- cat /etc/resolv.conf
```

- ¿Cuántos nombres distintos de `Hostname` salieron? ¿Son rotación exacta?
- Qué línea de `resolv.conf` explica por qué `api` basta, sin el sufijo.

**Ahora sí se repone.** Borren un pod y miren:

```bash
kubectl get pods -l app=api
kubectl delete pod <un-pod-de-api>
kubectl get pods -l app=api
```

Comparen con el paso 6 de la sesión 1: antes desaparecía, ahora hay otro.

---

## Paso 3 - El front y el Ingress

`k8s/front.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: front
  labels:
    app: front
spec:
  replicas: 2
  selector:
    matchLabels:
      app: front
  template:
    metadata:
      labels:
        app: front
    spec:
      containers:
        - name: front
          image: nginx:1.27-alpine
          ports:
            - name: http
              containerPort: 80
          readinessProbe:
            httpGet:
              path: /
              port: http
          resources:
            requests:
              cpu: 20m
              memory: 16Mi
            limits:
              memory: 64Mi
---
apiVersion: v1
kind: Service
metadata:
  name: front
spec:
  selector:
    app: front
  ports:
    - name: http
      port: 80
      targetPort: http
```

`k8s/ingress.yaml`:

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

```bash
kubectl apply -f k8s/front.yaml -f k8s/ingress.yaml
kubectl rollout status deployment/front --timeout=120s
kubectl get ingressclass
kubectl get ingress
```

Probar la entrada:

```bash
curl -s -o /dev/null -w "%{http_code}\n" -H 'Host: titulacion.lab.local' http://127.0.0.1:18081/
curl -s -H 'Host: titulacion.lab.local' http://127.0.0.1:18081/api/hola | head -3
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:18081/
```

Esperado: `200`, la respuesta de la API (`Name: api-lab`, `Hostname: ...`) y `404` en la tercera, sin el `Host`. Expliquen por escrito por qué el 404 **no** es una falla.

---

## Paso 4 - Publicar una versión nueva

En una **segunda terminal** dejen un ciclo que pida la API cada segundo. Es el "usuario" durante la actualización:

```bash
while true; do curl -s -o /dev/null -w "%{http_code} " -H 'Host: titulacion.lab.local' http://127.0.0.1:18081/api/; sleep 1; done
```

En la primera, cambien la imagen **en el archivo** (no con `kubectl set image`): `traefik/whoami:v1.10.2` pasa a `traefik/whoami:v1.10.3`, y

```bash
kubectl diff -f k8s/api.yaml
git add k8s && git commit -m "api: v1.10.3"
kubectl apply -f k8s/api.yaml
kubectl rollout status deployment/api --timeout=120s
kubectl get rs -l app=api
kubectl rollout history deployment/api
```

- ¿Cuántos ReplicaSets hay ahora y cuántos pods tiene cada uno?
- Miren el ciclo del curl. En una corrida de laboratorio con esta misma estrategia, durante una actualización apareció un `502` entre las demás respuestas `200`. **Anoten lo que ven ustedes**: no hay promesa de cero errores en una actualización; el grado de corte depende de cómo se apague la aplicación (sesión 6 retoma esto).
- Con Ctrl+C paren el ciclo.

---

## Paso 5 - Verlo fallar: una readiness probe mal escrita

Cambien **solo** la readiness probe del front en `k8s/front.yaml`: `path: /` pasa a `path: /health`. Esa ruta no existe en nginx. **No** toquen el Ingress ni nada más: un cambio dentro de `spec.template` basta para que el Deployment lance una actualización.

```bash
kubectl diff -f k8s/front.yaml
kubectl apply -f k8s/front.yaml
kubectl rollout status deployment/front --timeout=30s; echo "salida: $?"
kubectl get pods -l app=front
kubectl get deploy front
kubectl describe pod -l app=front | grep -E 'Unhealthy|Readiness'
```

Lo que debe ver:

```console
error: timed out waiting for the condition
salida: 1
NAME                     READY   STATUS    RESTARTS   AGE
front-5fcc468f4b-nxs97   0/1     Running   0          41s
front-7744f6974f-cbxj9   1/1     Running   0          109s
front-7744f6974f-hmkf2   1/1     Running   0          109s
...
Warning  Unhealthy  ...  Readiness probe failed: HTTP probe failed with statuscode: 404
```

Los hashes serán otros. Contesten:

1. ¿Por qué el pod nuevo está `Running` y aun así `0/1`?
2. ¿Por qué los dos pods viejos siguen vivos? ¿Qué dos números de la estrategia lo permiten?
3. ¿Cuál es la **causa** y cuál el **síntoma** en lo que muestra el terminal?
4. Con el sitio: `curl -s -o /dev/null -w "%{http_code}\n" -H 'Host: titulacion.lab.local' http://127.0.0.1:18081/` sigue en `200`. ¿Por qué?

---

## Paso 6 - Regresar

```bash
kubectl rollout history deployment/front
kubectl rollout undo deployment/front
kubectl rollout status deployment/front --timeout=60s
kubectl get pods -l app=front
```

Debe terminar con `successfully rolled out` y dos pods `1/1`. `kubectl` mostrará antes un aviso (`resource deployments/front was previously managed with 'kubectl apply'. Rolling back will not update the kubectl.kubernetes.io/last-applied-configuration annotation...`): es exactamente por qué hay que corregir el archivo. Después, y esto es lo importante del paso: **corrijan el archivo** `k8s/front.yaml` (vuelvan a `path: /`) para que el repositorio diga lo mismo que el clúster. Un `undo` repara el clúster, no el repositorio.

```bash
git diff
kubectl diff -f k8s/front.yaml          # debe salir sin diferencias
```

---

## Paso 7 - La probe que no comprueba nada (opcional, para pensar)

Con la API: cambien su readiness probe a `path: /saludo-que-no-existe` y apliquen.

- `traefik/whoami` contesta 200 **a cualquier ruta**, así que el despliegue termina bien. Una probe que siempre pasa no protege a nadie.
- Pregunta: si su aplicación real tuviera ese comportamiento (por ejemplo, un `app.UseSpa` que devuelve el `index.html` para toda ruta), ¿cómo se daría cuenta de que su readiness no sirve? Una prueba: apuntarla a una ruta que **sí** debe fallar y comprobar que falla.

Vuelvan al archivo original antes de seguir.

---

## Limpieza

```bash
kubectl delete -f k8s/ --ignore-not-found
kubectl delete pod cliente --ignore-not-found
docker rm -f k3s-lab        # solo si usaron la ruta B y ya terminaron
```

---

## Si algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `Error from server (Invalid): ... selector does not match template labels` | `selector.matchLabels` distinto de `template.metadata.labels` | Alinearlos |
| `ENDPOINTS` vacío | La etiqueta del Service no empata, o los pods no están Ready | `kubectl get pods --show-labels`; `kubectl describe pod` |
| 404 en la entrada con el `Host` correcto | El Ingress no se aplicó o la clase es otra | `kubectl get ingress`; `kubectl get ingressclass` |
| `connection refused` en `127.0.0.1:18081` | El puerto no se mapeó | Revisar el `-p` del `docker run` |
| El `kubectl diff` no muestra nada y creen que no se aplicó | Ya se aplicó | `kubectl get deploy -o yaml` |
