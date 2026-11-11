# Glosario - S04 - Seguridad del clúster

> Los términos de la sesión 4, en orden alfabético.

---

## A

### Admisión (controlador de admisión)
**Categoría:** Kubernetes / Seguridad
**Definición:** Componente que revisa, y puede rechazar o modificar, un objeto después de autenticar y autorizar la petición y antes de guardarlo.
**En el curso:** Pod Security y ValidatingAdmissionPolicy son controladores de admisión.

---

### allowPrivilegeEscalation
**Categoría:** Kubernetes / Seguridad
**Definición:** Campo de `securityContext` que controla si un proceso puede ganar más privilegios que su proceso padre. El perfil `restricted` exige ponerlo en `false`.
**En el curso:** `seguro.yaml`.

---

### auth can-i
**Categoría:** kubectl / Comando
**Definición:** `kubectl auth can-i <verbo> <recurso> [--as=<identidad>]` pregunta al API si una identidad puede hacer algo. `--list` muestra todo lo que puede.
**En el curso:** La prueba de rechazo de RBAC.

---

## B

### Baseline
**Categoría:** Kubernetes / Pod Security
**Definición:** Perfil mínimamente restrictivo de los Pod Security Standards: evita escaladas de privilegio conocidas. Permite correr como root.
**En el curso:** Rechazó `privileged: true`; dejó pasar un pod como root.

---

## C

### capabilities
**Categoría:** Linux / Seguridad
**Definición:** Privilegios del kernel que se pueden dar o quitar a un proceso por separado. `drop: ["ALL"]` las quita todas.
**En el curso:** El perfil `restricted` pide quitarlas.

---

### CEL (Common Expression Language)
**Categoría:** Kubernetes / Políticas
**Definición:** Lenguaje de expresiones con el que se escriben las validaciones de una ValidatingAdmissionPolicy.
**En el curso:** `object.spec.containers.all(...)`.

---

### ClusterRole / ClusterRoleBinding
**Categoría:** Kubernetes / RBAC
**Definición:** Permisos y asignación de permisos a nivel de clúster, no limitados a un namespace.
**En el curso:** Se mencionan; la práctica usa Role y RoleBinding.

---

## E

### enforce, warn, audit
**Categoría:** Kubernetes / Pod Security
**Definición:** Los tres modos de Pod Security Admission: rechazar, admitir y avisar, o admitir y anotar en el registro de auditoría.
**En el curso:** Etiquetas `pod-security.kubernetes.io/<modo>` en un namespace.

---

## G

### Gatekeeper
**Categoría:** Kubernetes / Políticas
**Definición:** Controlador de admisión que aplica políticas escritas con el marco de restricciones de OPA (Open Policy Agent), en Rego.
**En el curso:** Se explica; no se instala.

---

## K

### Kyverno
**Categoría:** Kubernetes / Políticas
**Definición:** Motor de políticas de Kubernetes que valida, muta, genera recursos, limpia y verifica imágenes, con políticas en YAML.
**En el curso:** Se explica; no se instala.

---

## N

### NetworkPolicy
**Categoría:** Kubernetes / Red
**Definición:** Objeto que dice qué tráfico puede entrar o salir de ciertos pods. Lo implementa el complemento de red; sin uno que la soporte no tiene efecto. Las políticas se suman.
**En el curso:** `db-solo-api`.

---

### No aislado (non-isolated)
**Categoría:** Kubernetes / Red
**Definición:** Estado por omisión de un pod: acepta y abre conexiones de cualquier lado hasta que una NetworkPolicy lo selecciona.
**En el curso:** Antes de la política, `intruso` entraba.

---

## P

### Pod Security Admission
**Categoría:** Kubernetes / Seguridad
**Definición:** Controlador de admisión integrado que aplica los Pod Security Standards por namespace, según etiquetas.
**En el curso:** Namespaces `prod`, `lab2` y `base`.

---

### Pod Security Standards
**Categoría:** Kubernetes / Seguridad
**Definición:** Tres perfiles de lo que un pod puede pedir: Privileged, Baseline y Restricted.
**En el curso:** Los tres modos de la práctica.

---

### privileged
**Categoría:** Kubernetes / Seguridad
**Definición:** Campo de `securityContext` que da al contenedor casi los privilegios del nodo.
**En el curso:** Rechazado por `baseline`.

---

### Prueba de rechazo
**Categoría:** Método del curso
**Definición:** Acción que una barrera debe impedir, con la salida que demuestra que la impidió. Una política que nunca rechazó nada no se ha probado.
**En el curso:** Las cuatro de la práctica.

---

## R

### RBAC
**Categoría:** Kubernetes / Seguridad
**Definición:** Control de acceso basado en roles: decide qué identidad puede hacer qué verbo sobre qué recurso. Solo permite; los permisos se suman.
**En el curso:** `lector-pods`.

---

### Restricted
**Categoría:** Kubernetes / Pod Security
**Definición:** Perfil muy restrictivo que sigue las mejores prácticas actuales de endurecimiento de pods: exige `runAsNonRoot`, quitar capacidades, `seccomp` y sin escalada.
**En el curso:** El namespace `prod`.

---

### Role / RoleBinding
**Categoría:** Kubernetes / RBAC
**Definición:** Un conjunto de permisos dentro de un namespace, y la asignación de esos permisos a identidades.
**En el curso:** `lector-pods` en `app`.

---

### runAsNonRoot
**Categoría:** Kubernetes / Seguridad
**Definición:** Campo de `securityContext` que exige que el contenedor corra con un usuario distinto de root.
**En el curso:** Pedido por `restricted`.

---

## S

### seccompProfile
**Categoría:** Linux / Seguridad
**Definición:** Perfil que limita las llamadas al sistema que un contenedor puede hacer. `RuntimeDefault` usa el del runtime.
**En el curso:** Pedido por `restricted`.

---

### securityContext
**Categoría:** Kubernetes / Seguridad
**Definición:** Sección del pod o del contenedor con sus ajustes de seguridad: usuario, capacidades, escalada, seccomp.
**En el curso:** `seguro.yaml`.

---

### ServiceAccount
**Categoría:** Kubernetes / RBAC
**Definición:** Identidad de un programa que corre en un pod. Cada namespace trae una `default`.
**En el curso:** `lector-pods`.

---

## V

### ValidatingAdmissionPolicy
**Categoría:** Kubernetes / Políticas
**Definición:** Política de admisión que viene en Kubernetes: reglas en CEL, aplicadas por un binding que dice dónde y con qué acción (`Deny`, `Warn`, `Audit`).
**En el curso:** `sin-latest`.

---

*Rogelio Arriaga Gonzalez*
