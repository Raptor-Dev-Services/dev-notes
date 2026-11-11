# Glosario - M02 - Identidad y mínimo privilegio

> Definiciones de los términos técnicos del módulo 2. Ordenado alfabéticamente.

---

## A

### Access Analyzer (IAM)
**Categoría:** AWS / IAM
**Definición:** Servicio de AWS que revisa acceso público y entre cuentas, valida políticas (más de 100 revisiones) y puede generar una política de mínimo privilegio a partir de la actividad registrada en CloudTrail.
**En el curso:** Se conoce en clase; no corre en el simulador.

---

### AssumeRole
**Categoría:** AWS / STS
**Definición:** Operación de STS que devuelve credenciales temporales de un rol. La sesión dura de 15 minutos hasta el máximo del rol (1 a 12 horas); por omisión, 1 hora.
**En el curso:** Es la alternativa a las llaves largas.

---

## C

### Credential report
**Categoría:** AWS / IAM
**Definición:** Reporte en CSV que lista a los usuarios IAM con el estado de su contraseña, MFA y llaves de acceso.
**En el curso:** Se genera en el simulador y se lee como defensor.

---

## D

### Deny explícito
**Categoría:** Autorización
**Definición:** Declaración con `Effect: Deny`. En la evaluación de AWS anula cualquier `Allow` de políticas de identidad, de recurso, límites, SCP o RCP.
**En el curso:** `app-con-deny` lista el bucket pero no puede borrar.

---

## F

### Federación
**Categoría:** Identidad
**Definición:** Usar un proveedor de identidad externo para que los usuarios entren a la nube con credenciales temporales, sin una cuenta IAM por persona.
**En el curso:** La recomendación de AWS para identidades humanas.

---

## I

### IAM Identity Center
**Categoría:** AWS / Identidad
**Definición:** Servicio de AWS para administrar de forma centralizada el acceso de usuarios de la fuerza laboral a varias cuentas, con identidades propias o de un proveedor externo.
**En el curso:** Es el camino recomendado para humanos.

---

### Identidad administrada (managed identity)
**Categoría:** Azure / Identidad
**Definición:** Identidad que Microsoft Entra asigna a un recurso de cómputo para obtener tokens sin administrar credenciales. Puede ser asignada por el sistema (vive con el recurso) o por el usuario (recurso independiente, reutilizable).
**En el curso:** Equivale, en espíritu, al rol de una instancia en AWS.

---

### IMDSv2
**Categoría:** AWS / EC2
**Definición:** Segunda versión del servicio de metadatos de instancia, orientada a sesión: un `PUT` entrega un token que se envía en cada `GET`. Se configura con `HttpTokens=required` para rechazar v1.
**En el curso:** Protege las credenciales del rol de la instancia.

---

## L

### Límite de permisos (permissions boundary)
**Categoría:** AWS / IAM
**Definición:** Política administrada que fija el máximo de permisos que una política de identidad puede conceder a un rol o usuario. No concede permisos por sí misma.
**En el curso:** Sirve para delegar la creación de roles sin perder el techo.

---

## M

### Mínimo privilegio
**Categoría:** Principios
**Definición:** Conceder solo las acciones, sobre los recursos y bajo las condiciones necesarios para una tarea.
**En el curso:** Se mide con una prueba que niega lo que no se necesita.

---

## O

### OIDC (OpenID Connect)
**Categoría:** Identidad
**Definición:** Capa de identidad sobre OAuth 2.0 que permite a un proveedor emitir tokens firmados que otro sistema verifica.
**En el curso:** Con GitHub Actions, evita guardar llaves de AWS en el repositorio.

---

## P

### PIM (Privileged Identity Management)
**Categoría:** Azure / Acceso privilegiado
**Definición:** Servicio de Microsoft Entra que da acceso privilegiado justo a tiempo, con vigencia, aprobación y MFA al activar.
**En el curso:** Es la respuesta a «el administrador lo es siempre». Requiere licencias.

---

## R

### RBAC de Azure
**Categoría:** Azure / Autorización
**Definición:** Sistema de autorización de Azure basado en asignaciones de rol: entidad de seguridad, definición de rol y ámbito. Es aditivo.
**En el curso:** El equivalente de las políticas de identidad de AWS.

---

### Role chaining
**Categoría:** AWS / STS
**Definición:** Usar las credenciales de un rol para asumir otro. La sesión resultante queda limitada a 1 hora.
**En el curso:** Se cita para entender los límites de duración.

---

## S

### SCP (Service Control Policy)
**Categoría:** AWS / Organizations
**Definición:** Política de organización que fija un techo de permisos para las cuentas. No concede permisos.
**En el curso:** Se combina por intersección con las políticas de identidad.

---

### STS
**Categoría:** AWS / Identidad
**Definición:** Security Token Service. Emite credenciales temporales.
**En el curso:** La pieza detrás de roles y federación.

---

## U

### Usuario raíz
**Categoría:** AWS / Cuenta
**Definición:** Identidad con la que se crea la cuenta, con acceso completo. AWS exige MFA, desaconseja crear llaves de acceso y recomienda monitorear su uso.
**En el curso:** Se protege y casi no se usa.
