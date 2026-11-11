<!-- _class: lead -->

# S06 - Políticas como código

### Infraestructura como código

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Cada equipo trae OpenTofu y Docker en su VM de las sesiones anteriores. Hoy instalamos Conftest. Es la última sesión: al final se presenta el proyecto final que une todo el curso.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, instalar Conftest | 20 min |
| Parte 1 | Qué es una política como código y por qué rechaza antes | 35 min |
| Parte 2 | Open Policy Agent y Rego | 55 min |
| Parte 3 | El plan de OpenTofu como JSON y Conftest | 45 min |
| Parte 4 | Pruebas, excepciones y huecos de una política | 35 min |
| Parte 5 | CI y controles de acceso automatizados | 25 min |
| Parte 6 | Práctica: políticas sobre el plan, vistas fallar | 55 min |
| Cierre | Proyecto final, tarea y quiz | 30 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. Ya están dentro de los tiempos. -->

---

## El lema de la sesión

> La política correcta rechaza el bucket público antes de que exista, no después del incidente.

- Un checklist depende de que alguien lo recuerde y lo lea.
- Una política en el pipeline **corre siempre**, en cada cambio, a cualquier hora.
- Hoy escribimos políticas, las probamos y las vemos **rechazar** un plan inseguro.

<!-- Pregunta al grupo: ¿cuántos pasos de su checklist de endurecimiento dependen de que alguien se acuerde? -->

---

## Al terminar puedes

- Explicar la diferencia entre **prevenir** (guardrail) y **reaccionar** (detectar después).
- Leer el plan de OpenTofu como JSON y encontrar lo que una política necesita.
- Escribir reglas en Rego (sintaxis de OPA 1.x) y probarlas con `opa test` y `conftest verify`.
- Correr Conftest sobre un plan real y sobre uno escrito a mano, y leer su salida y su código de salida.
- Escribir una **excepción** documentada y reconocer cuándo una política tiene un hueco.
- Poner la política en un workflow de CI que rechaza el pull request.

---

## Versiones de esta sesión (a noviembre de 2026)

| Herramienta | Versión | Dónde se comprobó |
|---|---|---|
| OpenTofu | 1.13.9 | Releases de GitHub (2026-11-05) |
| Conftest | 0.71.1 (trae OPA 1.21.1 embebido) | Releases de GitHub (2026-10-06) y `conftest --version` |
| OPA (CLI `opa`) | 1.22.3 | Releases de GitHub (2026-11-10) |
| Proveedor `kreuzwerker/docker` | 3.9.0 | `.terraform.lock.hcl` de la práctica |
| setup-opentofu | v2.0.2 | Releases de GitHub |

- Todas las salidas de la sesión son de corridas reales con estas versiones.
- Los ejemplos de AWS son **Ejemplo ilustrativo**: no hay nube real en el curso. Se prueban con un plan JSON reducido a mano.

---

## Instalar Conftest con su suma de verificación

```bash
curl -fsSLO https://github.com/open-policy-agent/conftest/releases/download/v0.71.1/conftest_0.71.1_Linux_x86_64.tar.gz
echo "c3f6b2a753bd56e377a1e51a95ae3e057f926f2e9139f7d5b5dfbbaf810f7985  conftest_0.71.1_Linux_x86_64.tar.gz" | sha256sum -c -
sudo tar -xzf conftest_0.71.1_Linux_x86_64.tar.gz -C /usr/local/bin conftest
conftest --version
```

```text
conftest_0.71.1_Linux_x86_64.tar.gz: OK
Conftest: 0.71.1
OPA: 1.21.1
```

- La suma sale del archivo `checksums.txt` de la misma release. Un binario que descargas y ejecutas sin verificar es una puerta abierta.

---

<!-- _class: lead -->

# Parte 1
## Qué es una política como código

---

## El problema: el incidente ya ocurrió

Tres historias que se parecen:

- Alguien abre el puerto 22 a todo internet "solo para probar" y nadie lo cierra.
- Un bucket queda público porque el formulario de la consola traía otra opción por omisión.
- Un rol de CI con permisos de administrador total, porque "así funciona de una vez".

Detectar **después** significa: ya estuvo expuesto, hay que investigar desde cuándo y quién entró.

> Una política como código mueve la pregunta antes: "¿este cambio puede existir?"

Fuente: Ray, *Policy as Code* (2024), cap. 1; Das y Chu, *Security as Code* (2023), cap. 3.

---

## Guardrail frente a reacción

| | Guardrail (prevenir) | Plan de reacción (detectar) |
|---|---|---|
| Cuándo actúa | Antes de crear el recurso | Después de que existe |
| Ejemplo | El pull request se rechaza si abre el 22 al mundo | Una alerta avisa que el 22 está abierto |
| Costo de un error | Un pull request rojo | Una investigación |
| Qué necesita | Reglas legibles por máquina | Monitoreo, alertas y personas |

- Ray (2024), cap. 1, distingue ambas ideas con los títulos de sus secciones: "Guardrails: Preventing the Unwanted" y "Plans: Reacting to the Unplanned".
- Un equipo maduro tiene **las dos**: la política reduce el ruido y el monitoreo atrapa lo que se escapó.

---

## Checklist frente a política

El checklist de la nota `09-cicd/01-checklists.md` pide, por ejemplo, "ningún secreto en el diff" y "sin vulnerabilidades críticas":

| | Checklist | Política como código |
|---|---|---|
| Quién lo aplica | Una persona, cuando se acuerda | El pipeline, cada vez |
| Evidencia | "Yo lo revisé" | Una salida con código de salida |
| Se prueba | No | Con pruebas, como cualquier código |
| Se versiona | A veces | Siempre, en el repositorio |
| Cambia con revisión | No necesariamente | Con pull request |

- La política **no reemplaza** a las personas: elimina de su lista lo que una máquina puede decidir, y les deja lo que requiere criterio.

---

## ¿Qué es una política, entonces?

Una regla que se puede evaluar con datos estructurados y responde "permitido" o "rechazado, porque...".

- **Entrada:** datos (JSON o YAML): un plan de OpenTofu, un manifiesto, una configuración.
- **Regla:** código declarativo.
- **Salida:** una lista de **mensajes de rechazo**. Si la lista está vacía, pasa.

> Un buen mensaje de rechazo dice **qué recurso**, **qué regla** y **cómo se arregla**.

Fuente: Ray (2024), cap. 1 ("What Is a Policy?" y "PaC Policy Characteristics").

---

## El motor: Open Policy Agent

- **OPA** es un motor de políticas de propósito general, con su lenguaje, **Rego**.
- Es un proyecto **graduado** de la Cloud Native Computing Foundation desde el 29 de enero de 2021 (ficha del proyecto en cncf.io, a noviembre de 2026).
- Se usa como CLI (`opa eval`, `opa test`), como servidor y embebido en otras herramientas.
- **Conftest** es una herramienta que usa OPA para probar archivos de configuración (Terraform, Kubernetes, Dockerfile, YAML, JSON...) en el pipeline.

Fuentes: cncf.io/projects/open-policy-agent-opa; conftest.dev; Ray (2024), cap. 2.

---

<!-- _class: lead -->

# Parte 2
## Open Policy Agent y Rego

---

## Rego en una lámina

```rego
package main

deny contains msg if {
	input.token != "secret"
	msg := "token invalido"
}
```

- `package main`: el espacio de nombres donde Conftest busca reglas por omisión.
- `deny contains msg if { ... }`: un **conjunto parcial**: cada `msg` que el cuerpo logre producir es un rechazo.
- Las líneas del cuerpo son **condiciones**: todas deben ser verdaderas a la vez (un "y").
- `:=` asigna una variable; `==` compara.
- `input` es el documento que se evalúa (aquí, el plan).

Fuente: documentación de OPA, "Policy Language" (openpolicyagent.org/docs/policy-language).

---

## Sintaxis v1: `if` y `contains` son obligatorios

OPA 1.x trae la **sintaxis v1** por omisión. Una regla escrita con la sintaxis vieja:

```rego
deny[msg] {
	input.resource_changes[_].type == "docker_container"
	msg := "ejemplo"
}
```

Falla al cargar. Salida real de Conftest 0.71.1:

```text
viejo.rego:3: rego_parse_error: `if` keyword is required before rule body
viejo.rego:3: rego_parse_error: `contains` keyword is required for partial set rules
```

- La guía de actualización de OPA dice: "OPA v1.0 requires the usage of `if` and `contains` keywords when declaring rules."
- Si encuentras un tutorial con `deny[msg] {`, es sintaxis v0: escríbelo con `contains ... if`.
- En v1 las palabras `in`, `every`, `if` y `contains` ya son parte del lenguaje: no hace falta importar nada.

Fuente: openpolicyagent.org/docs/v0-upgrade.

---

## Iterar: `some ... in`

```rego
deny_puerto_publico contains msg if {
	some rc in cambios
	rc.type == "docker_container"
	some p in rc.change.after.ports
	p.ip in {"0.0.0.0", ""}
	msg := sprintf("%s publica el puerto %d en todas las interfaces (ip=%q); usa 127.0.0.1", [rc.address, p.external, p.ip])
}
```

- `some rc in cambios`: prueba **cada** recurso. Si una condición falla para ese recurso, se pasa al siguiente.
- `some p in rc.change.after.ports`: lo mismo con cada puerto.
- `p.ip in {"0.0.0.0", ""}`: pertenencia a un conjunto.
- `sprintf` arma el mensaje con valores reales.

Fuente: "Policy Language" (documentación de OPA): `some ... in`, membresía y `sprintf`.

---

## Indefinido no es falso

En Rego, una expresión que se refiere a un valor **indefinido** es indefinida, y el cuerpo de la regla simplemente **no produce nada**:

- Un recurso sin `ports` (por ejemplo, `local_file`) **no** dispara `deny_puerto_publico`. Es lo que queremos aquí.
- Pero cuidado: un typo en una ruta (`rc.change.after.port`) también es indefinido... y la regla **pasa en silencio**.
- `not` niega: `not p["foo"]` es verdadero si no existe.
- `default x := false` da un valor cuando la regla no se cumple.

> Una política con una ruta mal escrita nunca falla: por eso hay que **probarla viéndola rechazar**.

Fuente: "Policy Language" (documentación de OPA): "Expressions that refer to undefined values are also undefined."

---

## Comprensiones y funciones

```rego
# Conjunto de puertos publicados (comprension de conjunto)
puertos_publicados := {p.external | some rc in cambios; some p in rc.change.after.ports}

# Funcion auxiliar
acciones(s) := s.Action if is_array(s.Action)
acciones(s) := [s.Action] if not is_array(s.Action)
```

- Comprensiones: `[ x | ... ]` (arreglo), `{ x | ... }` (conjunto), `{ k: v | ... }` (objeto).
- `acciones` normaliza un campo que puede ser **texto o lista**: en una política IAM de AWS, `"Action": "*"` y `"Action": ["*"]` significan lo mismo. Sin eso la política tiene un hueco.
- `every` verifica que algo valga para **todos** los elementos (con un dominio vacío, es verdadero).

Fuente: "Policy Language" (documentación de OPA).

---

## Verlo fallar: variable insegura

Una versión ingenua de la función:

```rego
acciones(s) := a if is_array(s.Action)
```

```text
Error: running test: load: loading policies: get compiler: 2 errors occurred:
policy/aws.rego:44: rego_unsafe_var_error: var a is unsafe
```

- Rego exige que **toda variable quede ligada**. Aquí `a` nunca recibe un valor.
- El arreglo es devolver `s.Action` directamente: `acciones(s) := s.Action if is_array(s.Action)`.
- Compilar la política (`opa check --strict`) encuentra este tipo de errores **sin necesidad de datos**.

---

## `opa eval`: preguntarle a una política

```bash
opa eval -f pretty -d policy -i fixtures/aws-inseguro.json 'data.main.deny_ssh_al_mundo'
```

```text
[
  "aws_vpc_security_group_ingress_rule.ssh abre el puerto 22 a 0.0.0.0/0"
]
```

- `-d`: dónde están las políticas. `-i`: el documento de entrada. La última cadena es la **consulta**.
- Útil para explorar: `opa eval ... 'count(data.main.cambios)'` dice cuántos recursos entran a revisión.
- `opa fmt` da formato estándar y `opa check --strict` revisa errores: ambos sin salida significan "limpio".

---

<!-- _class: lead -->

# Parte 3
## El plan de OpenTofu como JSON y Conftest

---

## El problema: ¿qué le doy a la política?

- Las políticas necesitan **datos estructurados**. El código HCL tiene variables, módulos y valores calculados.
- El **plan** de OpenTofu ya resolvió todo eso: dice **qué va a crear, cambiar o borrar**, con los valores finales.
- Evaluar el plan es evaluar **lo que realmente va a pasar**, no lo que parece decir el código.

```bash
tofu plan -out=plan.tfplan
tofu show -json plan.tfplan > plan.json
```

Fuente: documentación de OpenTofu, "tofu show" y "JSON Output Format".

---

## La forma del plan en JSON

Llaves de primer nivel (documentación de OpenTofu, "JSON Output Format"):

| Llave | Qué contiene |
|---|---|
| `format_version` | Versión del formato (en la práctica salió `1.2`) |
| `resource_changes` | La acción planeada para cada instancia de recurso |
| `planned_values` | Cómo quedará el estado final |
| `configuration` | La configuración leída, con expresiones |
| `prior_state` | El estado al que se aplica el plan |
| `output_changes`, `variables`, `checks`, `errored`, `timestamp` | Salidas, variables, comprobaciones, si falló y cuándo |

En el plan de la práctica (infraestructura nueva) salieron: `configuration`, `errored`, `format_version`, `planned_values`, `resource_changes`, `terraform_version` y `timestamp`; no hay `prior_state` porque no existía estado.

La documentación pide **ignorar propiedades desconocidas** (compatibilidad hacia adelante) y **rechazar una versión mayor no soportada**.

> Tu política debe leer `format_version` solo si le importa: ante una versión mayor nueva, fallar es mejor que aprobar a ciegas.

---

## Un elemento de `resource_changes`

```json
{
  "address": "docker_container.servidor",
  "mode": "managed",
  "type": "docker_container",
  "name": "servidor",
  "provider_name": "registry.opentofu.org/kreuzwerker/docker",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": {
      "image": "ubuntu:24.04",
      "name": "a07-servidor",
      "ports": [{"external": 2222, "internal": 22, "ip": "0.0.0.0", "protocol": "tcp"}]
    }
  }
}
```

(Salida real, recortada.) En `change`:

- `actions`: `["create"]`, `["update"]`, `["delete"]`, `["no-op"]`, `["read"]`, o un reemplazo como `["delete", "create"]`.
- `before` / `after`: el valor antes y después. `after` no existe en un `delete`.
- `after_unknown`: marca con `true` lo que solo se sabrá al aplicar.
- `before_sensitive` / `after_sensitive`: marca lo sensible.

---

## Dos advertencias sobre el JSON del plan

1. **Los valores sensibles salen en texto plano.** La documentación de `tofu show` advierte: con `-json`, "any sensitive values in OpenTofu state will be returned in plain text". El `plan.json` es tan delicado como el estado (sesión 3): **no lo subas como artefacto del CI ni lo imprimas** en el log.
2. **Los valores desconocidos no están en `after`.** Un atributo que se calcula al aplicar (por ejemplo, un `id`) aparece en `after_unknown`. Una política que compara ese atributo no lo ve: decídelo con cuidado (rechazar o ignorar).

Fuente: documentación de OpenTofu, "tofu show" y "JSON Output Format".

---

## Conftest: cómo encuentra y corre las reglas

- Lee reglas del directorio `policy` (o el que pases con `--policy`), en el espacio de nombres `main` por omisión.
- Busca reglas cuyo nombre empieza con **`deny`**, **`violation`** o **`warn`**; admite un sufijo con identificador: `deny_puerto_publico`.
- `deny` y `violation` **rechazan**; `warn` **avisa**. `violation` puede devolver datos estructurados además del mensaje.
- Formatos de entrada: JSON, YAML, HCL/HCL2, Dockerfile, TOML, INI y otros.
- La salida termina con un resumen: `N tests, N passed, N warnings, N failures, N exceptions`.

Fuente: conftest.dev.

---

## La política sobre un plan real

```bash
tofu show -json plan.tfplan > plan.json
conftest test --policy policy plan.json
echo "salida: $?"
```

Plan de un contenedor que publica el 22 en todas las interfaces (salida real):

```text
FAIL - plan-inseguro.json - main - docker_container.servidor publica el puerto 2222 en todas las interfaces (ip="0.0.0.0"); usa 127.0.0.1
FAIL - plan-inseguro.json - main - docker_container.servidor expone el puerto 22 del contenedor

8 tests, 6 passed, 0 warnings, 2 failures, 0 exceptions
salida: 1
```

- **Código de salida 1 = el CI se pone rojo.** Cada mensaje dice recurso, problema y arreglo.
- El plan **no se aplicó**: el recurso inseguro nunca existió.

---

## El mismo plan, corregido

Cambias el puerto a `127.0.0.1` y el contenedor a un puerto de aplicación:

```hcl
  ports {
    internal = 8080
    external = 8080
    ip       = "127.0.0.1"
  }
```

```text
8 tests, 8 passed, 0 warnings, 0 failures, 0 exceptions
salida: 0
```

- Mismo pipeline, mismas reglas, resultado distinto porque cambió **el recurso**, no la política.
- Los "8 tests" son cada regla evaluada contra el documento (4 de contenedor, 4 de AWS en esta práctica; las de AWS no encuentran nada que revisar).

---

## `warn`, `--fail-on-warn` y `--no-fail`

```text
WARN - latest.json - main - docker_container.servidor usa una imagen con :latest

8 tests, 7 passed, 1 warning, 0 failures, 0 exceptions
```

- Código de salida **0** con solo advertencias (medido). Con `--fail-on-warn` sale **1**.
- `--no-fail` hace que Conftest salga con 0 aunque una política falle: sirve para **introducir** una política sin romper a nadie. Úsalo con fecha de caducidad.
- Estrategia de adopción: regla nueva como `warn` unas semanas, luego como `deny`.

Fuente: `conftest test --help` (v0.71.1) y salida real.

---

<!-- _class: lead -->

# Parte 4
## Pruebas, excepciones y huecos

---

## Una política sin prueba es un deseo

Tres pruebas mínimas por regla:

1. Un caso que **debe rechazarse** (la regla ve el problema).
2. Un caso que **debe aceptarse** (no rechaza lo legítimo).
3. Un caso **borde** (por ejemplo, un recurso que se está borrando).

```rego
test_rechaza_puerto_en_todas_las_interfaces if {
	count(deny_puerto_publico) == 1 with input as plan_con_puerto("0.0.0.0", 8080)
}

test_acepta_puerto_en_loopback if {
	count(deny_puerto_publico) == 0 with input as plan_con_puerto("127.0.0.1", 8080)
}
```

- `with input as ...` sustituye el `input` solo dentro de esa prueba.
- Se corren con `opa test policy -v` o con `conftest verify --policy policy` (archivos `*_test.rego`).

Fuente: documentación de OPA, "Policy Testing"; conftest.dev ("Testing policies with conftest verify").

---

## Verlo fallar: la prueba que atrapa una política rota

Rompe la regla a propósito (`p.ip == "1.1.1.1"` en lugar de `p.ip in {"0.0.0.0", ""}`):

```text
FAIL - policy/docker_test.rego -  - data.main.test_rechaza_puerto_en_todas_las_interfaces

4 tests, 3 passed, 0 warnings, 1 failure, 0 exceptions, 0 skipped
salida: 1
```

- La prueba de rechazo cayó: ya no ve el puerto abierto.
- Con la regla restaurada: `4 tests, 4 passed`.
- Es la misma idea de las sesiones anteriores: **quita el arreglo y comprueba que la prueba cae**.

---

## El hueco: una política que pasa también puede estar mal

Primera versión de la regla de SSH abierto al mundo:

```rego
rc.change.after.from_port <= 22
rc.change.after.to_port >= 22
```

Una regla de seguridad que abre **todo el tráfico** (`ip_protocol = "-1"`) no trae puertos: `from_port` y `to_port` son `null`.

```text
8 tests, 8 passed, 0 warnings, 0 failures, 0 exceptions
```

- **Pasó.** Y esa regla abre el 22 (y todo lo demás) a internet.
- El arreglo: contemplar el caso en la regla.

```rego
abre_ssh(regla) if regla.ip_protocol == "-1"

abre_ssh(regla) if {
	regla.ip_protocol == "tcp"
	regla.from_port <= 22
	regla.to_port >= 22
}
```

- Con el arreglo: `FAIL ... abre el puerto 22 a 0.0.0.0/0` y una prueba nueva (`test_rechaza_todo_el_trafico`) que lo cubre.

---

## Excepciones

A veces algo **debe** violar la regla (una demo, un servicio público a propósito). Conftest tiene un mecanismo:

```rego
exception contains rules if {
	some rc in cambios
	rc.address == "docker_container.demo_publica"
	rules := ["puerto_publico"]
}
```

```text
EXCP - demo-publica.json - main - data.main.exception[_][_] == "puerto_publico"

8 tests, 7 passed, 0 warnings, 0 failures, 1 exception
```

- `rules := ["puerto_publico"]` exime a las reglas `deny_puerto_publico` o `violation_puerto_publico` **solo para ese recurso**.
- Una cadena vacía exime **todas** las reglas: no lo hagas.
- Por eso las reglas llevan **identificador** en su nombre: para poder exentar una sin apagar las demás.

Fuente: conftest.dev/exceptions.

---

## Gobernar las excepciones

Una excepción es una **decisión de riesgo**. Que lo parezca:

- Vive en un archivo propio (`excepciones.rego`), no dentro de la regla.
- Se pide en un pull request con **motivo, responsable y fecha de revisión** en el comentario.
- Es por **recurso concreto**, nunca por tipo.
- Se revisa periódicamente: una excepción sin revisar es una puerta que se quedó abierta.
- El conteo (`1 exception`) aparece en cada corrida: **se ve**.

> Si el equipo agrega excepciones más rápido de lo que arregla recursos, la política ya no gobierna: decora.

---

<!-- _class: lead -->

# Parte 5
## CI y controles de acceso automatizados

---

## El workflow que rechaza (FRAGMENTO)

> **Fragmento:** el YAML se validó con un parser y los SHA vienen de la API de GitHub; el workflow no se ejecutó en un runner real. Los comandos de cada paso sí se corrieron en local.

```yaml {1-11|12-22|23-34|*}
name: Politicas de infraestructura

on:
  pull_request:
    paths:
      - "infra/**"
      - "policy/**"
      - ".github/workflows/politicas.yml"

permissions:
  contents: read

jobs:
  politicas:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1
        with:
          persist-credentials: false
      - uses: opentofu/setup-opentofu@a1320f892987e89d278cc92dc5adc984fb93aca4  # v2.0.2
        with:
          tofu_version: 1.13.9
          tofu_wrapper: false
      - name: Instalar Conftest 0.71.1 con su suma de verificacion
        run: |
          curl -fsSLO https://github.com/open-policy-agent/conftest/releases/download/v0.71.1/conftest_0.71.1_Linux_x86_64.tar.gz
          echo "c3f6b2a753bd56e377a1e51a95ae3e057f926f2e9139f7d5b5dfbbaf810f7985  conftest_0.71.1_Linux_x86_64.tar.gz" | sha256sum -c -
          sudo tar -xzf conftest_0.71.1_Linux_x86_64.tar.gz -C /usr/local/bin conftest
      - name: Probar las politicas (con sus pruebas)
        run: conftest verify --policy policy
      - name: Plan de OpenTofu en JSON
        working-directory: infra
        run: |
          tofu init -input=false
          tofu plan -input=false -out=plan.tfplan
          tofu show -json plan.tfplan > plan.json
      - name: Rechazar el plan inseguro
        run: conftest test --policy policy infra/plan.json
```

---

## Por qué está escrito así

- Primero se prueban **las políticas** (`conftest verify`): una política rota no debe juzgar a nadie.
- Después se genera el plan y se evalúa. **Un código de salida distinto de cero detiene el pull request.**
- `tofu_wrapper: false`: la documentación de `setup-opentofu` sugiere desactivar el *wrapper* cuando hay problemas con los códigos de salida o el formato de la salida; aquí necesitamos el JSON limpio en un archivo y el código de salida real.
- `plan.json` **no** se sube como artefacto: puede traer valores sensibles en texto plano.
- Versiones y SHA fijados. El binario de Conftest se verifica con su suma.
- `permissions: contents: read`: el workflow solo lee.

Fuente: github.com/opentofu/setup-opentofu; docs.github.com (sintaxis de workflows).

---

## Cumplimiento como código

Un estándar de cumplimiento (por ejemplo, "los buckets no son públicos", "todo recurso lleva responsable") es un conjunto de **controles**. Si cada control es una política:

- El auditor pide evidencia y tú das **el historial del repositorio** y las corridas del CI.
- Cambiar un control es un pull request con revisión, no un correo.
- El estado de cumplimiento se mide en cada cambio, no una vez al año.

Armitage (2022), cap. 7 ("Compliance as Code"), y Das y Chu (2023), cap. 3, desarrollan esta idea para nubes públicas. Este curso la practica a escala de laboratorio.

> Una política no es un certificado: demuestra que ese **control** se cumplió en ese **cambio**, no que la organización "cumple".

---

## Controles de acceso automatizados

El principio de mínimo privilegio también se puede **comprobar**:

```rego
deny_iam_admin_total contains msg if {
	some rc in cambios
	rc.type == "aws_iam_policy"
	doc := json.unmarshal(rc.change.after.policy)
	some s in doc.Statement
	s.Effect == "Allow"
	"*" in acciones(s)
	"*" in recursos(s)
	msg := sprintf("%s concede Action * sobre Resource *", [rc.address])
}
```

- La política de IAM viaja en el plan como **texto JSON**: `json.unmarshal` la abre para revisarla.
- Real, sobre un plan reducido a mano (Ejemplo ilustrativo): `FAIL - fixtures/iam-inseguro.json - main - aws_iam_policy.ci concede Action * sobre Resource *`; con un permiso acotado (`s3:GetObject` sobre un bucket) pasa.
- Otros controles automatizables: etiquetas obligatorias (responsable, entorno), regiones permitidas, cifrado activado.

Fuente: Das y Chu (2023), cap. 5 ("Controlling Access Through Automation"); Armitage (2022), cap. 6 y 7.

---

## Lo que una política no ve

- Lo que **no está en el plan**: algo creado a mano en la consola, o por otra herramienta.
- Lo que **ya existía**: una política sobre un plan solo juzga los cambios (en nuestras reglas, `create` y `update`).
- Valores **desconocidos** hasta aplicar.
- La **intención**: una regla correcta puede aprobar una arquitectura mala.

Por eso las políticas de prevención se acompañan de **detección** (monitoreo, revisiones periódicas de lo que existe) y de revisión humana.

---

<!-- _class: lead -->

# Parte 6
## Práctica: políticas sobre el plan

---

## Lo que vamos a hacer

1. Un plan real de OpenTofu con un contenedor que publica el puerto 22 en todas las interfaces.
2. Políticas en Rego (contenedores y AWS) con sus pruebas.
3. **Verlo fallar:** Conftest rechaza el plan inseguro (código de salida 1).
4. Corregir el recurso y ver pasar el plan (código 0).
5. Un hueco de la política (todo el tráfico `-1`), su arreglo y su prueba.
6. Una excepción, y el CI que rechaza.

Entrega: las políticas, sus pruebas, el workflow y las salidas.

---

## Errores comunes de la sesión

| Síntoma | Causa | Qué hacer |
|---|---|---|
| `rego_parse_error: if keyword is required` | Sintaxis v0 de un tutorial | `deny contains msg if { ... }` |
| `rego_unsafe_var_error` | Variable sin ligar | Asignarla o devolver el valor directamente |
| La política "pasa" siempre | Ruta mal escrita: indefinido | Probar el caso que debe rechazar |
| `0 tests` o nada que revisar | Espacio de nombres distinto a `main` | `--namespace` o `package main` |
| El plan no trae lo que busco | Valor desconocido hasta aplicar | Mirar `after_unknown` |
| El CI no corre | `paths` no incluye la carpeta que cambió | Revisar `on.pull_request.paths` |
| Plan en el log con secretos | `plan.json` como artefacto o salida | No subirlo ni imprimirlo |

---

## Proyecto final

Crear y endurecer un servidor **desde cero con un solo comando**:

| Pieza | Qué hace | Se ve fallar cuando |
|---|---|---|
| OpenTofu | Crea el servidor (y red, y volúmenes) con estado remoto y bloqueo | Dos `apply` a la vez: el bloqueo lo impide |
| Ansible | Lo endurece con el rol `endurecer` | La segunda corrida reporta cambios |
| El CI | Corre lint, Molecule y las políticas en cada pull request | Un cambio malo lo deja en rojo |
| La política | Rechaza lo inseguro antes de que exista | El plan con el puerto 22 al mundo se rechaza |

- Un solo punto de entrada (un script o un `make`) que encadena las cuatro piezas.
- **Cada pieza, vista fallar**, con la salida real en la evidencia.
- La rúbrica está en `tarea.md`.

---

## Referencias (a noviembre de 2026)

- OPA, lenguaje de políticas: https://www.openpolicyagent.org/docs/policy-language
- OPA, pruebas: https://www.openpolicyagent.org/docs/policy-testing
- OPA, actualización a v1.0: https://www.openpolicyagent.org/docs/v0-upgrade
- OPA en la CNCF: https://www.cncf.io/projects/open-policy-agent-opa/
- Conftest: https://www.conftest.dev/ y excepciones: https://www.conftest.dev/exceptions/
- OpenTofu, formato JSON: https://opentofu.org/docs/internals/json-format/
- OpenTofu, `tofu show`: https://opentofu.org/docs/cli/commands/show/
- setup-opentofu: https://github.com/opentofu/setup-opentofu
- Sintaxis de workflows de GitHub Actions: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax
- AWS, S3 Block Public Access (los cuatro ajustes): https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html
- Esquema del recurso `aws_vpc_security_group_ingress_rule` y de `aws_s3_bucket_public_access_block`: documentación del proveedor AWS (repositorio hashicorp/terraform-provider-aws, carpeta website/docs/r).
- Libros: Ray, *Policy as Code* (2024), caps. 1, 2, 11 y 12; Das y Chu, *Security as Code* (2023), caps. 3 y 5; Armitage, *Cloud Native Security Cookbook* (2022), caps. 6 y 7.
- Versiones: OpenTofu 1.13.9, Conftest 0.71.1 (OPA 1.21.1), OPA 1.22.3, proveedor docker 3.9.0.

---

## Tarea y cierre del curso

**Tarea:** tres políticas de su organización escritas como código, con pruebas, y el **proyecto final** (ver `tarea.md`).

Lo que se llevan del curso:

- Infraestructura que se **crea** con código, con estado compartido y bloqueado.
- Servidores que se **configuran** con código idempotente y probado.
- Cambios que se **revisan** y **prueban** como cualquier código.
- Políticas que **rechazan** lo inseguro sin que nadie tenga que recordarlo.

> La política correcta rechaza el bucket público antes de que exista, no después del incidente.

---

## Quiz: Políticas como código

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin la VM abierta.
- Lo que falles, búscalo en esta clase y en la nota `09-cicd/07-policy-as-code` antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
