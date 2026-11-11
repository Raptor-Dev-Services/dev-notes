# Práctica - S04 - Ver rechazar

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 2 horas, en equipo.

---

## Objetivo

Poner cuatro barreras a un clúster y ver **rechazar** a cada una: un RBAC que dice que no, un namespace que impide pods como root, una política propia que prohíbe `latest` y una NetworkPolicy que aísla la base. Cada barrera termina en una **prueba de rechazo** escrita, con su salida.

Todo en su laboratorio, con su propio clúster. No se prueba nada contra un sistema que no sea suyo.

---

## Antes de empezar

- El clúster de la sesión 1. En la ruta B:

```bash
mkdir -p ~/k8s-lab/s04 && cd ~/k8s-lab/s04
docker run -d --name k3s-lab --privileged -v "$PWD:$PWD" rancher/k3s:v1.37.2-k3s1 server
kubectl() { docker exec -i -w "$PWD" k3s-lab kubectl "$@"; }
sleep 40 && kubectl get nodes
```

- **Ejemplo ilustrativo:** namespaces `app`, `prod`, `base` y `pol`, y las identidades `lector-pods` y `intruso` son de laboratorio.
- Las versiones de las salidas de esta guía son de K3s `v1.37.2+k3s1`. Los mensajes exactos pueden variar un poco en otra versión.

---

## Paso 1 - RBAC: una cuenta de solo lectura

`rbac.yaml`:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: lector-pods
  namespace: app
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: lector-pods
  namespace: app
rules:
  - apiGroups: [""]
    resources: ["pods", "pods/log"]
    verbs: ["get", "list", "watch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: lector-pods
  namespace: app
subjects:
  - kind: ServiceAccount
    name: lector-pods
    namespace: app
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: Role
  name: lector-pods
```

```bash
kubectl create namespace app
kubectl apply -f rbac.yaml
```

### Prueba de rechazo 1

```bash
SA=system:serviceaccount:app:lector-pods
for v in "list pods" "get pods/log" "delete pods" "list secrets" "create deployments"; do
  printf "%-20s " "$v"; kubectl auth can-i $v -n app --as=$SA
done
printf "list pods en default: "; kubectl auth can-i list pods -n default --as=$SA
kubectl get secrets -n app --as=$SA
```

Esperado:

- `yes` para `list pods` y `get pods/log`.
- `no` para `delete pods`, `list secrets` y `create deployments`.
- `no` para `list pods` en `default`.
- El último comando: `Error from server (Forbidden): secrets is forbidden: User "system:serviceaccount:app:lector-pods" cannot list resource "secrets" in API group "" in the namespace "app"`.

Anoten: ¿qué dice el error de `Forbidden` que ayuda a saber **qué regla falta**? ¿Qué cambiarían para dar permiso, y por qué **no** lo darían?

Después pidan la lista completa y busquen qué permisos tiene la cuenta **además** de los pods:

```bash
kubectl auth can-i --list --as=$SA -n app
```

Verán permisos como `selfsubjectreviews`: son permisos básicos que todo cliente recibe, no los que ustedes escribieron.

---

## Paso 2 - RBAC: lo que nadie debería regalar

```bash
kubectl auth can-i --list --as=system:serviceaccount:app:default -n app
```

- ¿Qué puede hacer la cuenta `default` del namespace? (Debe ser casi nada, además de los permisos básicos.)
- Escriban en una frase por qué una cuenta con `create pods` sería, de hecho, una cuenta que **puede leer los Secrets** del namespace (según la documentación de Kubernetes, "Secrets").
- Pidan un token de corta vida y miren solo su comienzo:

```bash
kubectl create token lector-pods -n app | cut -c1-20
```

No copien el token completo en ningún documento.

---

## Paso 3 - Pod Security: ver rechazar un pod como root

`root.yaml`:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: root
spec:
  containers:
    - name: c
      image: busybox:1.37
      command: ["sleep","3600"]
```

```bash
kubectl create namespace prod
kubectl label namespace prod pod-security.kubernetes.io/enforce=restricted pod-security.kubernetes.io/enforce-version=latest
kubectl -n prod apply -f root.yaml
```

### Prueba de rechazo 2

Debe salir `Error from server (Forbidden)` con `violates PodSecurity "restricted:latest"` y cuatro causas: `allowPrivilegeEscalation`, `capabilities`, `runAsNonRoot` y `seccompProfile`. Copien la salida completa y marquen cuál de las cuatro causas corregirían primero y por qué.

### El pod que sí pasa

`seguro.yaml`:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: seguro
spec:
  securityContext:
    runAsNonRoot: true
    runAsUser: 10001
    seccompProfile:
      type: RuntimeDefault
  containers:
    - name: c
      image: busybox:1.37
      command: ["sleep","3600"]
      securityContext:
        allowPrivilegeEscalation: false
        capabilities:
          drop: ["ALL"]
```

```bash
kubectl -n prod apply -f seguro.yaml
kubectl -n prod wait --for=condition=Ready pod/seguro --timeout=60s
kubectl -n prod exec seguro -- id
```

Esperado: `uid=10001 gid=0(root) groups=0(root)`. Preguntas:

1. ¿Por qué el pod pasó aunque el grupo es 0?
2. ¿Qué le pasaría a **su** aplicación real con estas restricciones? Pruébenla: cambien `busybox` por su imagen y anoten lo que falla (escribir en una carpeta, abrir un puerto bajo, etc.).

---

## Paso 4 - Los tres modos y el perfil baseline

```bash
kubectl create namespace lab2
kubectl label namespace lab2 pod-security.kubernetes.io/warn=restricted
kubectl -n lab2 apply -f root.yaml

kubectl create namespace base
kubectl label namespace base pod-security.kubernetes.io/enforce=baseline
```

`priv.yaml`:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: priv
spec:
  containers:
    - name: c
      image: busybox:1.37
      command: ["sleep","3600"]
      securityContext:
        privileged: true
```

```bash
kubectl -n base apply -f priv.yaml
kubectl -n base apply -f root.yaml
kubectl -n base get pods
```

Anoten para cada uno de los tres comandos con pod: ¿se creó o se rechazó? ¿Con aviso o sin él? Expliquen **por qué `baseline` deja correr como root** y `restricted` no.

---

## Paso 5 - Una política propia: nada de `latest`

`vap.yaml`:

```yaml
apiVersion: admissionregistration.k8s.io/v1
kind: ValidatingAdmissionPolicy
metadata:
  name: sin-latest
spec:
  failurePolicy: Fail
  matchConstraints:
    resourceRules:
      - apiGroups: [""]
        apiVersions: ["v1"]
        operations: ["CREATE", "UPDATE"]
        resources: ["pods"]
  validations:
    - expression: "object.spec.containers.all(c, c.image.contains(':') && !c.image.endsWith(':latest'))"
      message: "Toda imagen debe llevar una etiqueta de versión distinta de latest."
---
apiVersion: admissionregistration.k8s.io/v1
kind: ValidatingAdmissionPolicyBinding
metadata:
  name: sin-latest
spec:
  policyName: sin-latest
  validationActions: ["Deny"]
  matchResources:
    namespaceSelector:
      matchLabels:
        politica: sin-latest
```

```bash
kubectl apply -f vap.yaml
kubectl create namespace pol
kubectl label namespace pol politica=sin-latest
sleep 5
```

### Prueba de rechazo 3

```bash
kubectl -n pol run malo --image=nginx:latest
kubectl -n pol run malo2 --image=nginx
kubectl -n pol run bueno --image=nginx:1.27-alpine
kubectl -n default run libre --image=nginx:latest
kubectl -n pol get pods
```

Esperado: `malo` y `malo2` rechazados con `denied request: Toda imagen debe llevar una etiqueta de versión distinta de latest.`; `bueno` y `libre` creados. Contesten:

1. ¿Por qué `nginx` **sin** etiqueta también se rechazó?
2. ¿Por qué `libre` pasó? ¿Qué tendrían que hacer para que la política también aplique en `default`?
3. Cambien `validationActions` a `["Warn"]` y repitan `malo`: ¿qué cambia? (Es el modo para **medir** antes de hacer cumplir.)

---

## Paso 6 - NetworkPolicy: aislar la base

Una "base" de laboratorio (nginx hace de base) y dos clientes con distinta etiqueta:

```bash
kubectl -n app create deployment db --image=nginx:1.27-alpine --port=80
kubectl -n app expose deployment db --port=80
kubectl -n app rollout status deployment/db --timeout=90s
kubectl -n app run api     --image=busybox:1.37 --labels=app=api     --command -- sleep 3600
kubectl -n app run intruso --image=busybox:1.37 --labels=app=intruso --command -- sleep 3600
kubectl -n app wait --for=condition=Ready pod/api pod/intruso --timeout=60s
```

`kubectl create deployment db` etiqueta sus pods con `app=db`. Compruébenlo con `kubectl -n app get pods --show-labels`.

Antes de la política:

```bash
for p in api intruso; do echo "$p:"; kubectl -n app exec $p -- wget -qO- -T 3 http://db/ | head -c 30; echo; done
```

Los dos llegan. Ahora `netpol.yaml`:

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: db-solo-api
  namespace: app
spec:
  podSelector:
    matchLabels:
      app: db
  policyTypes:
    - Ingress
  ingress:
    - from:
        - podSelector:
            matchLabels:
              app: api
      ports:
        - protocol: TCP
          port: 80
```

```bash
kubectl apply -f netpol.yaml
sleep 5
kubectl -n app exec api     -- wget -qO- -T 3 http://db/ | head -c 30; echo
kubectl -n app exec intruso -- wget -qO- -T 3 http://db/
kubectl -n app describe networkpolicy db-solo-api
```

### Prueba de rechazo 4

`api` sigue recibiendo la página; `intruso` recibe `wget: can't connect to remote host (...): Connection refused` (o un tiempo agotado, según el complemento de red). Anoten:

1. ¿Qué dice la sección `Allowing ingress traffic` del `describe`?
2. ¿Qué pasaría si el clúster no tuviera un complemento de red que soporte NetworkPolicy? (La documentación lo dice: la política **no tendría efecto**.)
3. Escriban una política de **negar toda la entrada** del namespace y comprueben con `api` que ahora tampoco entra. Luego quítenla.

---

## Paso 7 - La matriz

Armen la tabla de **quién puede hacer qué** de lo que acaban de crear, con una fila por identidad (`lector-pods`, `default`, ustedes como administrador) y una columna por recurso (pods, deployments, secrets, namespaces). Cada celda con su `kubectl auth can-i`. Esa tabla es el insumo de la tarea.

---

## Paso 8 - Entregar las cuatro pruebas de rechazo

Un archivo `pruebas-de-rechazo.md` con, para cada barrera:

- La acción que debe fallar y el comando.
- La salida real (texto, no captura).
- Una frase: "esto demuestra que ..., y **no** demuestra que ...".

Una prueba que **nunca falló** no vale: si su política dejó pasar algo que debía rechazar, corríjanla y repitan.

---

## Limpieza

```bash
kubectl delete namespace app prod lab2 base pol --ignore-not-found
kubectl delete validatingadmissionpolicybinding sin-latest
kubectl delete validatingadmissionpolicy sin-latest
docker rm -f k3s-lab        # solo ruta B, si ya terminaron
```

---

## Si algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| El pod `root` se crea en `prod` | La etiqueta no se aplicó | `kubectl get ns prod --show-labels` |
| La política `sin-latest` no rechaza | El namespace no tiene la etiqueta, o falta esperar unos segundos | `kubectl get ns pol --show-labels`; reintentar |
| `intruso` sigue entrando a `db` | La política no se aplicó o la etiqueta de `db` no es `app=db` | `kubectl -n app get pods --show-labels` |
| `can-i` dice `yes` a todo | Se ejecutó sin `--as` con su cuenta de administrador | Añadir `--as=<identidad>` |
| Un pod del paso 3 se queda sin arrancar por su imagen | La imagen necesita root o escribir en `/` | Leer los eventos y ajustar la imagen |
