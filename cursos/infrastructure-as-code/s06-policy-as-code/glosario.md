# Glosario - S06 - Políticas como código

> Definiciones de los términos técnicos de la sesión 6.
> Ordenado alfabéticamente.

---

## A

### Advertencia (warn)
**Categoría:** Conftest
**Definición:** Regla `warn_*` que avisa sin rechazar: Conftest sale con 0, o con 1 si se usa `--fail-on-warn`. Sirve para introducir una regla antes de volverla `deny`.
**En el curso:** `warn_imagen_sin_version` avisa de una imagen con `:latest`.

---

## C

### Cambios (conjunto)
**Categoría:** Rego
**Definición:** Conjunto auxiliar de la política que reúne los recursos del plan con acción `create` o `update`, para ignorar lo que se borra.
**En el curso:** `some rc in cambios` al inicio de cada regla.

---

### Comprensión
**Categoría:** Rego
**Definición:** Expresión que construye un arreglo, conjunto u objeto a partir de una condición: `[x | ...]`, `{x | ...}`, `{k: v | ...}`.
**En el curso:** `{p.external | some rc in cambios; some p in rc.change.after.ports}`.

---

### Conftest
**Categoría:** Herramienta
**Definición:** Utilidad que prueba archivos de configuración estructurados (JSON, YAML, HCL, Dockerfile...) contra políticas escritas en Rego.
**En el curso:** Versión 0.71.1, con OPA 1.21.1 embebido.

---

### conftest verify
**Categoría:** Conftest
**Definición:** Comando que ejecuta las pruebas (`*_test.rego`) de las políticas.
**En el curso:** Detecta una política rota o con huecos antes de usarla.

---

### Cumplimiento como código
**Categoría:** Principio
**Definición:** Expresar cada control de un estándar como una política que se evalúa en cada cambio, de modo que la evidencia es el historial del repositorio y del CI.
**En el curso:** Armitage (2022), cap. 7.

---

## D

### default
**Categoría:** Rego
**Definición:** Palabra clave que da un valor a una regla cuando ninguna de sus definiciones se cumple: `default x := false`.
**En el curso:** Se explica con el concepto de indefinido.

---

### deny
**Categoría:** Conftest
**Definición:** Regla (nombre que empieza con `deny`) que, si produce un mensaje, rechaza la entrada y hace que Conftest salga con 1.
**En el curso:** `deny_puerto_publico`.

---

## E

### Excepción
**Categoría:** Conftest
**Definición:** Regla `exception contains rules` que exime a un recurso concreto de reglas concretas. Se cuenta aparte en el resumen (`1 exception`).
**En el curso:** `rules := ["puerto_publico"]` para `docker_container.demo_publica`.

---

## F

### format_version
**Categoría:** OpenTofu
**Definición:** Llave del plan en JSON que declara la versión del formato. Los consumidores deben ignorar propiedades nuevas y rechazar una versión mayor no soportada.
**En el curso:** El plan de la práctica trae `1.2`.

---

## G

### Guardrail
**Categoría:** Principio
**Definición:** Control que previene: impide que el cambio inseguro exista, en lugar de avisar cuando ya existe.
**En el curso:** Un pull request rechazado por una política.

---

## H

### Hueco (de una política)
**Categoría:** Calidad
**Definición:** Caso inseguro que una política deja pasar por no contemplarlo. Una política que pasa no demuestra que el recurso sea seguro.
**En el curso:** `ip_protocol = "-1"` con puertos `null` pasó la primera versión de la regla del puerto 22.

---

## I

### Indefinido
**Categoría:** Rego
**Definición:** Resultado de una expresión que se refiere a un valor que no existe. El cuerpo de la regla no produce nada; una ruta mal escrita falla en silencio.
**En el curso:** Por eso toda política se prueba con un caso que debe rechazar.

---

## J

### json.unmarshal
**Categoría:** Rego
**Definición:** Función integrada que convierte un texto JSON en un objeto.
**En el curso:** Abre la política de IAM que viaja como texto dentro del plan.

---

## O

### OPA (Open Policy Agent)
**Categoría:** Herramienta
**Definición:** Motor de políticas de propósito general con el lenguaje Rego. Proyecto graduado de la CNCF desde el 2021-01-29.
**En el curso:** Evalúa las políticas de Conftest.

---

### opa eval / opa test / opa check
**Categoría:** Herramienta
**Definición:** Comandos de OPA: consultar una política, ejecutar sus pruebas (`test_*`) y revisar errores de compilación (`--strict`).
**En el curso:** `opa eval -f pretty -d policy -i entrada.json 'data.main.deny_ssh_al_mundo'`.

---

## P

### Plan (de OpenTofu)
**Categoría:** OpenTofu
**Definición:** Lista de acciones que OpenTofu realizaría. Con `tofu show -json` se convierte en un documento que las políticas pueden evaluar.
**En el curso:** `tofu plan -out=plan.tfplan` y `tofu show -json plan.tfplan > plan.json`.

---

### Política como código
**Categoría:** Principio
**Definición:** Regla evaluable por máquina, escrita como código, versionada y probada, que responde permitido o rechazado con un motivo.
**En el curso:** Ray (2024), cap. 1.

---

## R

### Rego
**Categoría:** Lenguaje
**Definición:** Lenguaje declarativo de OPA. Desde OPA 1.0 exige `if` y `contains` al declarar reglas.
**En el curso:** `deny contains msg if { ... }`.

---

### resource_changes
**Categoría:** OpenTofu
**Definición:** Llave del plan en JSON con la acción planeada para cada instancia de recurso (`address`, `type`, `change.actions`, `change.before`, `change.after`).
**En el curso:** Lo que recorren las reglas con `some rc in cambios`.

---

## S

### Sensibles (en el plan JSON)
**Categoría:** Seguridad
**Definición:** Valores marcados como sensibles que, con `tofu show -json`, salen en texto plano. El `plan.json` debe tratarse como un secreto.
**En el curso:** No se sube como artefacto del CI.

---

### some ... in
**Categoría:** Rego
**Definición:** Forma de iterar sobre una colección o de probar pertenencia en Rego.
**En el curso:** `some p in rc.change.after.ports`.

---

## T

### tofu show -json
**Categoría:** OpenTofu
**Definición:** Comando que imprime un plan guardado (o el estado) como JSON legible por máquina.
**En el curso:** Entrada de Conftest.

---

## V

### violation
**Categoría:** Conftest
**Definición:** Como `deny`, pero puede devolver datos estructurados además del mensaje.
**En el curso:** Se menciona; la práctica usa `deny`.

---

## W

### with (en una prueba)
**Categoría:** Rego
**Definición:** Palabra clave que sustituye el `input` (o datos y funciones) solo dentro de una prueba.
**En el curso:** `with input as plan_con_puerto("0.0.0.0", 8080)`.

---

