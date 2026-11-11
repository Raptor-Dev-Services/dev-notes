# Glosario - M06 - Cumplimiento como código y arquitectura

> Definiciones de los términos técnicos del módulo 6.
> Ordenado alfabéticamente.

---

## A

### ADR (Architecture Decision Record)
**Categoría:** Arquitectura
**Definición:** Registro corto de una decisión de arquitectura: contexto, decisión, alternativas descartadas, consecuencias, dueño y fecha de revisión.
**En el curso:** Evita rediscutir cada tres meses por qué el bucket de registros está en otra cuenta.

---

### Azure Policy
**Categoría:** Azure / Gobierno
**Definición:** Servicio que evalúa recursos contra definiciones de política y aplica un efecto (por ejemplo `audit`, `deny`, `deployIfNotExists`, `modify`).
**En el curso:** El equivalente de las reglas de AWS Config con capacidad de negar la creación.

---

## C

### checkov
**Categoría:** Herramienta
**Definición:** Herramienta de código abierto que revisa plantillas de infraestructura como código contra reglas incluidas y propias, y devuelve un código de salida que un pipeline puede usar.
**En el curso:** Se usó la versión 3.2.459 (noviembre de 2026) sobre plantillas de CloudFormation.

---

### Cumplimiento como código
**Categoría:** Cumplimiento
**Definición:** Práctica de expresar los requisitos de cumplimiento como reglas ejecutables que se evalúan de forma automática y dejan evidencia.
**En el curso:** El tema del módulo.

---

## D

### Deriva (drift)
**Categoría:** Cumplimiento
**Definición:** Diferencia entre lo que la plantilla o la política dicen que debe existir y lo que realmente hay en la cuenta, causada por cambios manuales o posteriores.
**En el curso:** `deriva.py` la detecta en buckets de una cuenta simulada.

---

### deny
**Categoría:** Azure Policy
**Definición:** Efecto que impide crear o actualizar un recurso que no cumple la regla. Se evalúa antes que `audit`.
**En el curso:** Se recomienda empezar en `Audit` y pasar a `Deny` cuando se sabe cuántos recursos marca.

---

## E

### Evidencia
**Categoría:** Cumplimiento
**Definición:** Resultado fechado y verificable de que un control se evaluó: qué recurso, con qué regla y con qué resultado.
**En el curso:** Una captura de pantalla de la víspera no sustituye el resultado de la regla.

---

### Excepción
**Categoría:** Cumplimiento
**Definición:** Aceptación documentada de que un recurso no cumple una regla, con dueño, motivo y fecha de caducidad.
**En el curso:** `Skipped checks: 1` en la salida de `checkov` con el comentario en la plantilla.

---

## G

### Gate (puerta)
**Categoría:** Pipeline
**Definición:** Conjunto de reglas cuyo fallo bloquea un cambio antes de desplegarlo.
**En el curso:** Las nueve reglas del taller.

---

### Guardrail
**Categoría:** Arquitectura
**Definición:** Límite preventivo impuesto desde arriba (por ejemplo una SCP o una política que niega) que evita que un equipo cree algo fuera de lo permitido.
**En el curso:** Se ubican en la organización, por encima de las cuentas de carga.

---

## N

### No conforme (NON_COMPLIANT)
**Categoría:** AWS Config
**Definición:** Resultado de una regla de Config cuando el recurso no cumple lo que evalúa.
**En el curso:** Un bucket cuya política permite lectura pública es no conforme para `S3_BUCKET_PUBLIC_READ_PROHIBITED`.

---

## P

### Prevenir / detectar / remediar
**Categoría:** Cumplimiento
**Definición:** Los tres momentos de una regla: impedir que se cree mal, encontrar lo que ya existe mal y corregirlo.
**En el curso:** Cada uno tiene una receta propia en *Cloud Native Security Cookbook*, cap. 7.

---

### Política (en cumplimiento)
**Categoría:** Cumplimiento
**Definición:** Declaración en lenguaje humano de lo que debe cumplirse. Se separa del control, de la regla y de la evidencia.
**En el curso:** "Los datos de clientes no son públicos".

---

## R

### Regla administrada (AWS Config)
**Categoría:** AWS Config
**Definición:** Regla predefinida por AWS, personalizable por alcance y parámetros, que evalúa si los recursos cumplen una práctica común.
**En el curso:** `S3_BUCKET_PUBLIC_READ_PROHIBITED`.

---

### Remediación
**Categoría:** Cumplimiento
**Definición:** Acción que lleva un recurso no conforme a conforme, manual o automática.
**En el curso:** Solo automática sobre lo seguro y reversible.

---

## S

### SCP
**Categoría:** AWS Organizations
**Definición:** Política de control de servicio: limita los permisos máximos de las cuentas miembro; no concede permisos ni afecta a la cuenta de administración.
**En el curso:** Se usa como guardrail.

---

## T

### Etiqueta (tag)
**Categoría:** Inventario
**Definición:** Par clave-valor asociado a un recurso. Sirve para dueño, entorno y clasificación.
**En el curso:** Las reglas exigen `dueno`.
