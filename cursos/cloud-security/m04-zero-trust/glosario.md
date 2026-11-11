# Glosario - M04 - Zero Trust

> Definiciones de los términos técnicos del módulo 4.
> Ordenado alfabéticamente.

---

## A

### Algoritmo de confianza
**Categoría:** Zero Trust
**Definición:** Lógica que usa el Policy Engine para combinar sus entradas (política, identidad, estado del equipo, otras fuentes) y decidir conceder, denegar o revocar. La norma lo trata en su sección 3.3.
**En el curso:** En la simulación es la función `decide`.

---

### Asumir brecha
**Categoría:** Principio
**Definición:** Diseñar como si ya hubiera algo comprometido dentro: reevaluar, limitar la confianza y registrar cada decisión.
**En el curso:** Fichas cortas, por recurso y revocables.

---

### AWS Verified Access
**Categoría:** Acceso / AWS
**Definición:** Servicio de AWS que da acceso a aplicaciones sin VPN, evaluando cada petición con datos de identidad y de dispositivo, y registrando los intentos. Tiene cargo por aplicación y por datos.
**En el curso:** Ejemplo de PEP administrado.

---

## B

### Bitácora de decisiones
**Categoría:** Registro
**Definición:** Registro de cada decisión de acceso con sujeto, recurso, resultado y motivo.
**En el curso:** Contesta "¿quién entró y por qué?".

---

## C

### CDM (diagnóstico y mitigación continuos)
**Categoría:** Zero Trust
**Definición:** Sistema que reúne el estado actual de los activos y aplica actualizaciones, y que informa al Policy Engine sobre el equipo que pide acceso.
**En el curso:** Fuente de la postura del dispositivo.

---

### Conditional Access
**Categoría:** Acceso / Azure
**Definición:** Motor de políticas de Microsoft Entra: toma señales (usuario, ubicación, dispositivo, aplicación, riesgo) y decide bloquear o exigir controles. Se aplica tras la autenticación de primer factor. Requiere licencias P1.
**En el curso:** Equivalente de Azure a un PDP.

---

## D

### Denegar por omisión
**Categoría:** Diseño
**Definición:** Todo lo que la política no declara se deniega. Es lo contrario de permitir por omisión.
**En el curso:** El recurso sin política devuelve `DENEGADO`.

---

### Deny explícito
**Categoría:** IAM
**Definición:** Instrucción `Effect: Deny` en una política de IAM. Anula cualquier Allow, en la misma cuenta o por otras políticas aplicables.
**En el curso:** `DeleteObject` se negó con `s3:*` permitido.

---

## F

### Ficha (token) de sesión
**Categoría:** Zero Trust
**Definición:** Credencial corta que emite el administrador para una sesión sobre un recurso. En la simulación va firmada con HMAC-SHA256 e incluye sujeto, recurso y expiración.
**En el curso:** Reusarla en otro recurso falla.

---

### Frontera de permisos (permissions boundary)
**Categoría:** IAM
**Definición:** Política que fija el máximo que una identidad puede recibir. El resultado es la intersección con sus políticas de identidad.
**En el curso:** Mencionada en la evaluación de IAM.

---

## M

### Micro-segmentación
**Categoría:** Redes / Zero Trust
**Definición:** Dividir en zonas pequeñas con control entre ellas. La norma la cita como uno de tres enfoques de arquitectura Zero Trust.
**En el curso:** Los grupos de M03 que referencian grupos.

---

### Modo informe (report-only)
**Categoría:** Operación
**Definición:** Modo en que una política se evalúa y se registra sin bloquear, para ver su impacto antes de activarla.
**En el curso:** Paso 3 de la migración desde VPN.

---

## N

### NIST SP 800-207
**Categoría:** Estándar
**Definición:** "Zero Trust Architecture", publicado en agosto de 2020. Define siete principios y los componentes lógicos de la arquitectura.
**En el curso:** Marco de referencia del módulo.

---

## P

### Pilares de CISA
**Categoría:** Estándar
**Definición:** Los cinco pilares del Zero Trust Maturity Model v2.0 (abril de 2023): identidad, dispositivos, redes, aplicaciones y cargas de trabajo, y datos.
**En el curso:** No equivalen a los principios de NIST.

---

### Plano de control y plano de datos
**Categoría:** Arquitectura
**Definición:** El de control es donde PE, PA y PEP se coordinan; el de datos, por donde viaja el tráfico de la aplicación. La norma los separa de forma lógica.
**En el curso:** Permite revocar sin tocar el recurso.

---

### Policy Administrator (PA)
**Categoría:** Zero Trust
**Definición:** Componente que abre o corta la ruta entre sujeto y recurso, y genera las credenciales de sesión, según la decisión del PE.
**En el curso:** `PolicyAdministrator` en el laboratorio.

---

### Policy Decision Point (PDP)
**Categoría:** Zero Trust
**Definición:** El conjunto de Policy Engine y Policy Administrator.
**En el curso:** Quien decide en la arquitectura.

---

### Policy Enforcement Point (PEP)
**Categoría:** Zero Trust
**Definición:** Componente que habilita, vigila y termina la conexión entre sujeto y recurso.
**En el curso:** `PEP` que reevalúa en cada uso.

---

### Policy Engine (PE)
**Categoría:** Zero Trust
**Definición:** Componente que toma la decisión final de conceder, denegar o revocar, y la registra.
**En el curso:** `PolicyEngine` en el laboratorio.

---

### Postura del dispositivo
**Categoría:** Zero Trust
**Definición:** Estado de seguridad de un equipo (versión del sistema, parches, cifrado, software aprobado) que influye en la decisión de acceso.
**En el curso:** El escenario 4 negó a ana por equipo sin parches.

---

## R

### RCP y SCP
**Categoría:** AWS Organizations
**Definición:** Políticas de control de recursos y de servicio. Para actuar en una cuenta de una organización, una acción debe estar permitida también por ellas.
**En el curso:** Parte de la evaluación de IAM.

---

### Reevaluación continua
**Categoría:** Principio
**Definición:** Volver a decidir durante la sesión, no solo al abrirla.
**En el curso:** Revocar corta aunque la ficha esté vigente.

---

## S

### Session Manager
**Categoría:** Acceso / AWS
**Definición:** Capacidad de Systems Manager para administrar nodos sin puertos de entrada, bastiones ni llaves SSH; control por IAM y registro de sesiones.
**En el curso:** Equivalente Zero Trust del SSH abierto.

---

### Sujeto
**Categoría:** Zero Trust
**Definición:** Quien pide acceso: una persona o un servicio, con sus atributos.
**En el curso:** `ana`, `luis` y `api-lab`.

---

## Z

### Zero Trust
**Categoría:** Principio
**Definición:** Enfoque que no otorga confianza por ubicación en la red ni por ser dueño del activo, y decide cada acceso con identidad, estado y contexto.
**En el curso:** Tema del módulo.

---
