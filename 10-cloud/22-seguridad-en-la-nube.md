# 22 - Seguridad en la nube

> Resumen de consulta del curso *Seguridad en la nube y Zero Trust* (`cursos/cloud-security/`). Las fuentes son la documentación oficial de AWS, Microsoft y GitHub, a noviembre de 2026.

## Problema que resuelve

Un equipo despliega en AWS o Azure y asume que "la nube ya es segura". El proveedor protege la infraestructura; la identidad, la configuración y los datos siguen siendo del equipo. Esta nota junta, en una página, qué le toca a cada quien y qué se revisa primero. Las notas `01-aws-basics`, `11-aws-vpc-networking`, `15-aws-iam-seguridad` y `16-aws-cloudwatch` explican **cómo se crea** cada pieza; esta explica **cómo se defiende**.

El método es siempre el mismo para cada mala configuración: **explicar** qué es y por qué ocurre, **detectarla**, **corregirla** y **verificar** la corrección.

---

## Responsabilidad compartida

- AWS: es responsable de proteger la infraestructura que corre los servicios; la responsabilidad del cliente la determinan los servicios que elige. Con EC2 el cliente administra el sistema operativo invitado; con S3 administra sus datos (incluidas las opciones de cifrado) y los permisos.
- Azure: datos, configuraciones e identidades son del cliente en local, IaaS, PaaS y SaaS. Sistema operativo, hosts, red física y centro de datos pasan a Microsoft según el modelo.
- Un control **compartido** se parte en dos; si cada parte asume que la otra lo hace, nadie lo hace.

> Fuentes: AWS, "Shared Responsibility Model"; Microsoft Learn, "Shared responsibility in the cloud".

## Inventario y etiquetas

Lo que no está en el inventario no se protege. Cada fila debe decir qué es, de quién es, qué tan delicado es lo que guarda y cómo está expuesto. Una fila sin dueño es un hallazgo.

| | AWS | Azure |
|---|---|---|
| Etiquetas por recurso | 50 de usuario | 50 pares |
| Herencia desde el contenedor | No aplica | Los recursos no heredan las del grupo de recursos |
| Inventario consultable | AWS Config, API de etiquetado | Azure Resource Graph |

No se ponen datos personales ni secretos en etiquetas.

## Datos: acceso público y cifrado

- S3 Block Public Access tiene cuatro ajustes (`BlockPublicAcls`, `IgnorePublicAcls`, `BlockPublicPolicy`, `RestrictPublicBuckets`); AWS recomienda activarlos los cuatro a nivel de cuenta. Quitar el bloqueo no borra una política pública que ya exista.
- Desde el 5 de enero de 2023, todo objeto nuevo en S3 se cifra con SSE-S3 (AES-256) sin costo. No sustituye decidir quién usa las llaves.

## Identidad

- Humanos: federación con un proveedor de identidad y credenciales temporales; MFA resistente al phishing donde se pueda.
- Cargas de trabajo: roles con credenciales temporales, no llaves de un usuario IAM.
- `AssumeRole`: sesión de 15 minutos a 12 horas según el rol; 1 hora por omisión y con role chaining.
- Evaluación en AWS: un `Deny` explícito anula cualquier `Allow`; las SCP y RCP no conceden permisos, solo limitan (intersección).
- Azure RBAC es **aditivo**: los permisos efectivos son la suma de las asignaciones.
- Usuario raíz: MFA obligatoria, sin llaves de acceso, uso monitoreado.

> Fuentes: AWS IAM User Guide ("Security best practices in IAM", "Policy evaluation logic", "Methods to assume a role", "Root user best practices"); Microsoft Learn ("What is Azure RBAC?", "Managed identities", "Privileged Identity Management").

## Lo que sigue en el curso

Red y segmentación, Zero Trust, detección y respuesta, y cumplimiento como código tienen su módulo en `cursos/cloud-security/`: cada uno con su práctica en un laboratorio local, sin cuenta de nube y sin costo.

## Cuándo usar / no usar

- Úsala como lista de revisión rápida antes de una auditoría interna de una cuenta.
- No sustituye la documentación oficial: los límites y valores cambian con el tiempo.

---

## Glosario

| Término | Definición |
|---|---|
| Responsabilidad compartida | Reparto entre proveedor y cliente de lo que protege cada quien |
| Control heredado | Control que el cliente hereda por completo del proveedor |
| Block Public Access | Cuatro ajustes de S3 que limitan el acceso público |
| Credenciales temporales | Credenciales que caducan, emitidas por STS |
| Deny explícito | Declaración que anula cualquier permiso concedido |
| SCP | Política de organización que fija un techo de permisos; no concede |

---

*Rogelio Arriaga Gonzalez*
