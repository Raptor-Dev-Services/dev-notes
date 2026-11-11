# 08 - Kubernetes: seguridad del clúster

> Fuente: *Kubernetes: Up and Running* (Burns, Beda, Hightower y Evenson, 2022): caps. 14 Role-Based Access Control for Kubernetes, 19 Securing Applications in Kubernetes y 20 Policy and Governance for Kubernetes Clusters. *Kubernetes Recipes* (Stencel): cap. 11 Security in Kubernetes. *Policy as Code* (Ray, 2024): caps. 4, 7 y 8. *Certified Kubernetes Administrator Study Companion* (Sachdeva, 2025): caps. 12 y 13. Documentación oficial citada en Referencias.

## Problema que resuelve

Un clúster recién instalado acepta casi todo: pods como root, imágenes `latest`, cualquier pod hablando con la base y una cuenta de despliegue con más poder del que necesita. Esta nota pone cuatro barreras, y de cada una explica la **prueba de rechazo**: la acción que debe fallar y la salida que lo demuestra. Una política que nunca rechazó nada no se ha probado.

Complementa a [07-kubernetes-manifiestos.md](07-kubernetes-manifiestos.md) (lo que se despliega) y a [05-secretos-produccion.md](../10-cloud/05-secretos-produccion.md) (dónde viven los secretos). Los ejemplos se probaron con K3s `v1.34.1+k3s1` el 11 de noviembre de 2026, en un laboratorio propio.

---

## Cuatro puertas

| Puerta | Pregunta | Herramienta |
|--------|----------|-------------|
| Acceso al API | ¿Quién eres y qué puedes hacer? | RBAC |
| Admisión | ¿Este objeto cumple lo que exigimos? | Pod Security, ValidatingAdmissionPolicy, Gatekeeper, Kyverno |
| Ejecución | ¿Con qué privilegios corre? | `securityContext` |
| Red | ¿Quién habla con quién? | NetworkPolicy |

---

## RBAC

| Objeto | Qué es |
|--------|--------|
| Role / ClusterRole | Permisos dentro de un namespace / a nivel de clúster |
| RoleBinding / ClusterRoleBinding | Quién recibe esos permisos |
| ServiceAccount | Identidad de un programa que corre en un pod |

RBAC **solo permite**: no hay reglas de denegar. Los permisos **se suman**.

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: lector-pods
  namespace: app
rules:
  - apiGroups: [""]
    resources: ["pods", "pods/log"]
    verbs: ["get", "list", "watch"]
```

### Prueba de rechazo

```console
$ SA=system:serviceaccount:app:lector-pods
$ kubectl auth can-i list pods -n app --as=$SA
yes
$ kubectl auth can-i delete pods -n app --as=$SA
no
$ kubectl auth can-i list secrets -n app --as=$SA
no
$ kubectl auth can-i list pods -n default --as=$SA
no
```

`kubectl auth can-i --list --as=$SA -n app` muestra todo, incluidos los permisos básicos que todo cliente recibe (por ejemplo, revisar sus propios permisos).

### Lo que pesa

La documentación de Kubernetes advierte que quien puede **crear un pod** en un namespace puede leer **cualquier Secret** de ese namespace, también de forma indirecta (con un Deployment). Por eso pesan:

- `get` o `list` sobre `secrets`;
- `create` sobre `pods` o `deployments`;
- `*` en recursos o verbos;
- un `ClusterRoleBinding` a `cluster-admin`;
- `exec` en pods.

Cuentas de servicio: cada namespace trae una `default`; úsala lo menos posible. Una cuenta por aplicación, con su Role mínimo. Si la aplicación no habla con el API, `automountServiceAccountToken: false`. `kubectl create token <cuenta>` pide un token de corta vida.

---

## Pod Security

Los Pod Security Standards son tres perfiles: **Privileged** (sin restricciones), **Baseline** (evita escaladas de privilegio conocidas) y **Restricted** (sigue las mejores prácticas actuales de endurecimiento). Pod Security Admission los aplica por namespace con etiquetas, en tres modos: `enforce` (rechaza), `warn` (admite y avisa) y `audit` (admite y anota).

```bash
kubectl label namespace prod pod-security.kubernetes.io/enforce=restricted
```

### Prueba de rechazo

Un pod mínimo como root en un namespace `restricted`:

```console
$ kubectl -n prod apply -f root.yaml
Error from server (Forbidden): ... violates PodSecurity "restricted:latest":
  allowPrivilegeEscalation != false (...), unrestricted capabilities (...),
  runAsNonRoot != true (...), seccompProfile (...)
```

El mensaje dice qué corregir. El pod que pasa:

```yaml
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

Su `id` dio `uid=10001 gid=0(root)`: `restricted` exige no ser root, no exige un grupo distinto de 0. `baseline` rechazó `privileged: true` y **dejó pasar** un pod como root.

Adopción: `warn` y `audit` en `restricted` primero, corregir lo que avisa, y luego `enforce`.

---

## Políticas como código

Los controladores de admisión revisan los objetos antes de guardarlos. Para reglas propias ("toda imagen con versión", "todo pod con `limits`"):

| Herramienta | Qué es |
|-------------|--------|
| ValidatingAdmissionPolicy | Viene en Kubernetes; reglas en CEL, en objetos del API |
| OPA Gatekeeper | Controlador de admisión con el marco de restricciones de OPA (Rego); valida y muta |
| Kyverno | Motor de políticas nativo; valida, muta, genera recursos, limpia y verifica imágenes |

Ray trata cada uno en un capítulo propio (*Policy as Code*, 2024: cap. 7 Gatekeeper, cap. 8 Kyverno; cap. 4 los controladores de admisión y las políticas nativas). Esta nota solo trae el ejemplo que corrió en el laboratorio sin instalar nada:

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

### Prueba de rechazo

`kubectl -n pol run malo --image=nginx:latest` y `--image=nginx` (sin etiqueta) se rechazan con el mensaje de la política; `--image=nginx:1.27-alpine` pasa; en un namespace **sin** la etiqueta `politica=sin-latest` la política no aplica. Saber **dónde** aplica forma parte de probarla.

Adopción de una política: escribirla, ponerla en `Warn` o `Audit`, ver qué objetos existentes la violarían, corregir o excluir con razón escrita, pasar a `Deny` y guardar la prueba de rechazo en el repositorio.

---

## NetworkPolicy

Por omisión un pod **no está aislado**: acepta y abre conexiones de cualquier lado. Un pod se aísla en una dirección cuando una NetworkPolicy lo selecciona e incluye esa dirección en `policyTypes`. Las políticas se suman, y para que una conexión pase deben permitirla los dos lados. **Las implementa el complemento de red**: sin uno que las soporte, crearlas no tiene efecto. K3s trae kube-router.

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

### Prueba de rechazo

Antes de la política, un pod `api` y un pod `intruso` llegaban a `db`. Después, `api` sigue entrando e `intruso` recibe `wget: can't connect to remote host (...): Connection refused` (el mensaje, o un tiempo agotado, depende del complemento de red).

Negar toda la entrada de un namespace: una política con `podSelector: {}` y `policyTypes: [Ingress]`, sin reglas `ingress`. Aislar la **salida** también bloquea el DNS si no se permite explícitamente: pruébalo antes de producción.

---

## La matriz de permisos

Una tabla con una fila por identidad y una columna por recurso sensible (`pods`, `pods/exec`, `deployments`, `secrets`, `namespaces`), con el verbo más alto de cada celda y el `kubectl auth can-i` que lo comprueba. Una celda sin prueba es una suposición.

---

## Errores comunes

| Error | Qué pasa | Qué hacer |
|-------|----------|-----------|
| Role con `*` | Todo permitido | Verbos y recursos explícitos |
| `get secrets` "por si acaso" | Se leen todas las contraseñas | Quitarlo |
| Cuenta `default` para todo | No se sabe quién hizo qué | Una cuenta por aplicación |
| `enforce` sin `warn` previo | Fallan despliegues que ya andaban | `warn` primero |
| Creer que `baseline` es `restricted` | Corre como root | Leer los perfiles |
| NetworkPolicy sin complemento | Nada se aísla | Comprobar el complemento |
| Política sin prueba de rechazo | No se sabe si rechaza | Escribirla y guardarla |

---

## Glosario

| Término | Definición |
|---------|-----------|
| RBAC | Control de acceso por roles; solo permite y los permisos se suman |
| ServiceAccount | Identidad de un programa en un pod |
| Pod Security Standards | Tres perfiles: Privileged, Baseline y Restricted |
| Pod Security Admission | Aplica los perfiles por namespace con etiquetas |
| `securityContext` | Ajustes de seguridad del pod o del contenedor |
| ValidatingAdmissionPolicy | Política de admisión integrada, escrita en CEL |
| CEL | Lenguaje de expresiones de las políticas integradas |
| NetworkPolicy | Reglas de tráfico entre pods, aplicadas por el complemento de red |
| Prueba de rechazo | Acción que debe fallar, con la salida que lo demuestra |

---

## Referencias

- Kubernetes, "Using RBAC Authorization": https://kubernetes.io/docs/reference/access-authn-authz/rbac/
- Kubernetes, "Secrets": https://kubernetes.io/docs/concepts/configuration/secret/ (advertencia sobre crear pods y leer Secrets, consultada el 11 de noviembre de 2026).
- Kubernetes, "Pod Security Standards": https://kubernetes.io/docs/concepts/security/pod-security-standards/
- Kubernetes, "Pod Security Admission": https://kubernetes.io/docs/concepts/security/pod-security-admission/
- Kubernetes, "Validating Admission Policy": https://kubernetes.io/docs/reference/access-authn-authz/validating-admission-policy/
- Kubernetes, "Network Policies": https://kubernetes.io/docs/concepts/services-networking/network-policies/
- Ray, J. (2024). *Policy as Code*. O'Reilly.
- Burns, B., Beda, J., Hightower, K. y Evenson, L. (2022). *Kubernetes: Up and Running* (3.ª ed.). O'Reilly.
- Stencel, G. *Kubernetes Recipes*. Apress.
- Sachdeva, P. (2025). *Certified Kubernetes Administrator Study Companion*. Apress.

---

*Rogelio Arriaga Gonzalez*
