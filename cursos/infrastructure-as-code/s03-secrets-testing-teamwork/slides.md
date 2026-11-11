<!-- _class: lead -->

# S03 - Secretos, pruebas y trabajo en equipo

### Infraestructura como código

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Pide el módulo de la tarea de la sesión 2: hoy le agregamos una prueba y un plan automático.
Recuerda a los equipos que la contraseña de la práctica es de laboratorio y que hoy se trata de ver DÓNDE termina un secreto, no de esconderlo mejor.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos y repaso | 20 min |
| Parte 1 | Secretos: dónde viven y dónde se filtran | 50 min |
| Parte 2 | Qué protege `sensitive`, y qué sí: estado cifrado y valores efímeros | 55 min |
| Parte 3 | Probar infraestructura: `validate`, `fmt`, `tofu test` | 45 min |
| Parte 4 | El plan en cada pull request y cómo revisarlo | 40 min |
| Parte 5 | Práctica: secretos, pruebas y CI | 65 min |
| Cierre | Errores comunes, tarea y quiz | 25 min |

<!-- Descansos de 10 minutos entre la Parte 2 y la 3, y a la mitad de la práctica. Los 300 minutos ya los incluyen. -->

---

## El lema de la sesión

> El archivo de estado puede contener secretos en texto plano.

- Marcar una variable como `sensitive` **oculta** el valor en pantalla. No lo saca del estado.
- Un plan guardado, el estado y la salida JSON de un plan son lugares donde un secreto aparece **completo**.
- Hoy los vemos con los ojos, medimos qué protege qué y armamos la revisión que un equipo necesita antes de aplicar.

---

## Al terminar puedes

- Decir en qué archivos y comandos puede aparecer un secreto aunque la variable sea `sensitive`.
- Cifrar el estado y los planes con el cifrado de OpenTofu, y explicar qué **no** cubre.
- Usar un valor efímero para que un secreto no se guarde.
- Escribir pruebas con `tofu test`, incluida una que espera que una validación falle.
- Armar el plan de cada pull request en CI, con un resumen legible.
- Escribir la regla de revisión de cambios de infraestructura de tu equipo.

---

## Repaso en un minuto

- Sesión 1: el estado es la memoria, se bloquea y puede contener secretos en claro.
- Sesión 2: módulos, `for_each`, `moved`, versiones fijas.
- Hoy cerramos el círculo: ¿qué hace **seguro** compartir el estado y el plan con un equipo? ¿Y qué hace **confiable** un cambio antes de llegar a producción?

---

<!-- _class: lead -->

# Parte 1
## Secretos: dónde viven y dónde se filtran

---

## El secreto más común es una contraseña de base de datos

Caso hipotético. Un equipo crea una base de datos con IaC. Necesita una contraseña de administrador.

Formas que se ven en la práctica, de peor a mejor:

| Forma | Problema |
|---|---|
| Escrita en el `.tf` o en un `.tfvars` versionado | Queda en Git para siempre, con todo su historial |
| Pasada en la línea de comandos (`-var 'clave=...'`) | Queda en el historial de la shell y, en CI, en los registros |
| Generada por la propia herramienta | Queda en el **estado** (lo veremos) |
| Leída de un gestor de secretos al aplicar | El valor igual llega al estado, salvo valores efímeros |
| Que el servicio la lea de un gestor de secretos **cuando corre**, sin pasarla por IaC | La mejor: nunca pasa por el estado |

La nota `10-cloud/05-secretos-produccion.md` de dev-notes lista reglas de base: nunca commitear secretos ni pasarlos como argumentos de línea de comandos, y usar el servicio de secretos del proveedor.

---

## Lo que el libro ordena

El cap. 6 de *Terraform: Up and Running* (Brikman, 2022) tiene tres bloques que conviene leer con calma:

1. **Secret Management Basics:** qué tipos de secretos hay, cómo se guardan y qué interfaz se usa para leerlos.
2. **Secret Management Tools with Terraform:** secretos para los **proveedores** (usuarios humanos, usuarios máquina, CI con roles o con OIDC) y para **recursos y data sources** (variables de entorno, archivos cifrados, gestores de secretos).
3. **State Files and Plan Files:** el capítulo termina con dos archivos que guardan los secretos que tú no pusiste ahí a propósito.

Ese último bloque es el de hoy.

---

## Variables de entorno: `TF_VAR_nombre`

Según la documentación de OpenTofu (opentofu.org/docs/language/values/variables/), OpenTofu busca en su entorno variables `TF_VAR_` más el nombre de cada variable declarada, como respaldo de las otras formas de dar valores.

```bash
export TF_VAR_clave_db='Lab-NoUsar-2026'     # de laboratorio
tofu plan -input=false
```

Precedencia, de menor a mayor (la documentación): variables de entorno, `terraform.tfvars`, `terraform.tfvars.json`, `*.auto.tfvars` y, al final, `-var` y `-var-file` en la línea de comandos.

- Evita escribir el secreto en la línea de comandos: queda en el historial. En CI se entrega desde el almacén de secretos del propio CI.
- Un secreto **no** va en `terraform.tfvars`: ese archivo se versiona.
- `-input=false` hace que falte una variable sea un error, no una pregunta interactiva. En CI es lo que quieres: un pipeline que se queda esperando una contraseña no avisa.

---

## `sensitive`: lo que hace

```hcl
variable "clave_db" {
  type        = string
  sensitive   = true
  description = "Contraseña de la base de datos (solo de laboratorio)."
}

resource "local_sensitive_file" "conexion" {
  filename = "${path.module}/salida/conexion.env"
  content  = "DB_PASSWORD=${var.clave_db}\n"
}

output "cadena" {
  value     = "Host=db.example.test;Password=${var.clave_db}"
  sensitive = true
}
```

Salida real del `plan`:

```
  # local_sensitive_file.conexion will be created
  + resource "local_sensitive_file" "conexion" {
      + content              = (sensitive value)
      ...
Changes to Outputs:
  + cadena = (sensitive value)
```

Y `tofu apply` termina con `cadena = <sensitive>`. La documentación lo describe: `sensitive = true` oculta el valor en la salida de `plan` y `apply`, y también las expresiones que dependen de él.

---

## `sensitive`: lo que **no** hace

La misma página de la documentación lo dice sin rodeos: OpenTofu **sigue guardando** los valores sensibles en el estado, y quien lee el estado los ve en texto plano. Tampoco impide que un proveedor o un error los exponga.

Lo comprobamos en la VM, con el recurso de arriba ya aplicado:

```bash
grep -n "Lab-NoUsar" terraform.tfstate
```

```
8:      "value": "Host=db.example.test;Password=Lab-NoUsar-2026",
23:            "content": "DB_PASSWORD=Lab-NoUsar-2026\n",
```

**Dos** apariciones en claro: el recurso y la salida. Y más:

```bash
tofu output -raw cadena
# Host=db.example.test;Password=Lab-NoUsar-2026
```

`tofu output` sin `-raw` ni `-json` muestra `<sensitive>`; con `-raw` o `-json`, el valor. El que puede leer el estado, o ejecutar `tofu output`, ve el secreto.

---

## Verlo fallar: el plan guardado también lo trae

```bash
tofu plan -out=p.plan
python3 - <<'PY'
import zipfile
z = zipfile.ZipFile('p.plan')
print([(n, b'Lab-NoUsar' in z.read(n)) for n in z.namelist()][:2])
PY
# [('tfplan', True), ('tfstate', True)]
```

Un archivo `-out` abierto con un lector de zip tiene el valor en claro. La documentación de `plan` lo advierte: el archivo contiene la configuración completa, los valores planeados y las opciones del plan, incluidas las variables de entrada, y los valores sensibles pueden quedar en texto plano a menos que se active el cifrado de planes.

Y la forma legible por máquinas:

```bash
tofu show -json p.plan | head -c 150
# {"format_version":"1.2","terraform_version":"1.13.9","variables":{"clave_db":{"value":"Lab-NoUsar-2026"},...
```

**El JSON del plan trae las variables con su valor.** Quien suba `plan.json` como artefacto de CI para analizarlo, publica el secreto.

---

## Resumen: dónde puede aparecer un secreto

| Lugar | Aparece en claro | Cómo se reduce |
|---|---|---|
| `.tf` o `.tfvars` versionados | Sí | No escribirlo ahí |
| Línea de comandos e historial | Sí | `TF_VAR_...` desde el almacén de secretos |
| `terraform.tfstate` (local o remoto) | Sí | Cifrado del estado, acceso mínimo, valores efímeros |
| Archivo de plan (`-out`) | Sí | Cifrado de planes; no conservarlo ni subirlo |
| `tofu show -json` de un plan | Sí | No guardarlo; analizarlo en el mismo paso |
| `tofu output -raw` / `-json` | Sí | Quien lo ejecuta ya tiene acceso al estado |
| Pantalla del `plan` y `apply` | No | `sensitive = true` |

Lo que oculta `sensitive` es **la pantalla**. Todo lo demás son archivos.

---

## Descanso de 10 minutos

Al regresar: cifrar el estado y valores que nunca se guardan.

---

<!-- _class: lead -->

# Parte 2
## Qué protege `sensitive`, y qué sí

---

## Tres capas, cada una con su alcance

| Capa | Qué protege | Qué no |
|---|---|---|
| `sensitive = true` | La **pantalla** (plan, apply, `output` sin `-raw`) | El estado, el plan guardado, el JSON |
| Cifrado de estado y plan | Los archivos **en reposo** | A quien ejecuta `tofu`, y la salida de `show -json` |
| Valores efímeros | El secreto **no se guarda**: ni en estado ni en plan | Los proveedores o recursos que no soportan valores efímeros |

Y una cuarta que está fuera de OpenTofu: **control de acceso** al backend. Nada de lo anterior sustituye a decidir quién puede leer el estado.

---

## Cifrado de estado y plan de OpenTofu

La documentación (opentofu.org/docs/language/state/encryption/) describe un bloque `encryption` dentro de `terraform`:

```hcl
terraform {
  encryption {
    key_provider "pbkdf2" "clave" {
      passphrase = var.passphrase
    }
    method "aes_gcm" "cifrado" {
      keys = key_provider.pbkdf2.clave
    }
    state {
      method = method.aes_gcm.cifrado
    }
    plan {
      method = method.aes_gcm.cifrado
    }
  }
}
```

- Un **key provider** produce la clave (aquí, a partir de una frase con PBKDF2). Un **method** cifra con ella (AES-GCM). `state` y `plan` dicen qué método usar para cada archivo.
- La frase debe tener al menos **16 caracteres**.
- La clave viene de una **variable sensible**, entregada por `TF_VAR_passphrase` o desde un gestor de claves. **Nunca** va escrita en el archivo.

<!-- La clase usa PBKDF2 con una frase porque corre sin servicios externos. En un equipo, la documentación también describe proveedores de clave de servicios en la nube; no los usamos por costo. -->

---

## Migrar un estado en claro al cifrado

Con un estado ya existente, el primer paso permite **leer en claro y escribir cifrado**, con un método `unencrypted` como respaldo (`fallback`):

```hcl
method "unencrypted" "migrar" {}

state {
  method = method.aes_gcm.cifrado
  fallback {
    method = method.unencrypted.migrar
  }
}
```

La documentación: si OpenTofu no puede **leer** el estado con el método nuevo, prueba automáticamente el `fallback`; al **guardar**, usa siempre el método nuevo.

Mientras el método `unencrypted` esté en la configuración, cada comando avisa:

```
Warning: Unencrypted method configured

Method unencrypted is present in configuration. This is a security risk and
should only be enabled during migrations.
```

Cuando el estado ya está cifrado, se quita el `fallback` y el método `unencrypted`.

---

## El estado, ya cifrado

```bash
export TF_VAR_passphrase='frase-de-laboratorio-no-usar'
tofu apply -auto-approve -input=false
head -c 250 terraform.tfstate
```

```
{"serial":1,"lineage":"511a4f14-...","meta":{"key_provider.pbkdf2.clave":"eyJzYWx0Ijoi...
```

```bash
grep -c "Lab-NoUsar" terraform.tfstate
# 0
tofu output -raw cadena     # sigue funcionando para quien tiene la frase
# Host=db.example.test;Password=Lab-NoUsar-2026
```

- El archivo es ahora un JSON con `meta` (parámetros del derivado de clave) y `encrypted_data`: el secreto ya no aparece.
- El plan guardado con `-out` también quedó cifrado: **ya no abre** como un zip.
- `tofu output -raw` **sigue mostrando** el secreto a quien ejecuta con la frase correcta. El cifrado protege el archivo, no al usuario autorizado.

---

## Verlo fallar: frase equivocada y frase corta

Con otra frase:

```
Error: Error acquiring the state lock

Error message: failed to write backup file: decryption failed for all
provided methods
attempted decryption failed for state: decryption failed: cipher: message
authentication failed
```

Con una frase de menos de 16 caracteres:

```
Error: Unable to build encryption key data

key_provider.pbkdf2.clave failed with error: passphrase is too short (minimum
16 characters)
```

La documentación lo advierte al principio: **perder la clave es perder el estado.** Una vez cifrados, el estado y los planes no se pueden leer sin la clave correcta. Por eso: respaldar la frase en un gestor de secretos del equipo, y probar el plan de recuperación **antes** de activar el cifrado.

---

## `enforced`: que nada se escriba en claro

```hcl
state {
  method   = method.aes_gcm.cifrado
  enforced = true
}
plan {
  method   = method.aes_gcm.cifrado
  enforced = true
}
```

Con `enforced = true`, OpenTofu **rechaza** estados sin cifrar. Salida real al inicializar con un estado en claro:

```
Error: Error refreshing state

encountered unencrypted payload without unencrypted method configured
```

La documentación sugiere activarlo después de la configuración inicial: evita que un entorno sin la variable escriba datos sin cifrar por descuido.

---

## Lo que el cifrado **no** hace

Según la propia documentación:

- No protege contra la **pérdida** de datos ni contra la **repetición** (usar un estado o plan viejo).
- No protege de **quien ejecuta** `tofu`: esa persona sigue pudiendo ver los valores sensibles.
- No renombres proveedores de clave ni métodos después de cifrar: los metadatos guardados dependen de esos nombres. Para cambiar de clave o de método, el `fallback` apunta al método anterior.
- Si usas un gestor de claves en la nube, una clave **por cada** estado.

Y lo que vimos: `tofu show -json` de un plan cifrado **sigue** imprimiendo los valores de las variables. El cifrado cubre el archivo en reposo; la salida del comando es del que lo ejecuta.

---

## Valores efímeros: que el secreto no se guarde

La documentación (opentofu.org/docs/language/ephemerality/) define un valor efímero como el que **no se guarda en el estado ni en el plan** y existe solo durante una ejecución de `tofu`. Los constructos que lo soportan son: recursos efímeros, variables efímeras, salidas efímeras, locals, proveedores, provisioners, bloques `connection` y atributos de solo escritura (`write-only`) de recursos. Este último requiere OpenTofu 1.11 o posterior, según su página.

Regla de la documentación: usa valores efímeros cuando puedas; si no, marca lo sensible como `sensitive` y activa el cifrado del estado y del plan.

```hcl
variable "clave" {
  type      = string
  ephemeral = true
}
```

Una variable efímera solo puede usarse en contextos efímeros; no se guarda su valor en el estado (solo el nombre en el plan).

---

## Un recurso efímero, comprobado

El proveedor `hashicorp/random` (3.9.1, en esta clase) trae un recurso **efímero** `random_password`. Lo comparamos con el normal:

```hcl
# Contraseña normal: queda en el estado.
resource "random_password" "normal" {
  length  = 20
  special = false
}

# Contraseña efímera: no se guarda ni en el estado ni en el plan.
ephemeral "random_password" "efimera" {
  length  = 20
  special = false
}

resource "terraform_data" "usa_efimera" {
  provisioner "local-exec" {
    command = "printf 'la clave mide %s caracteres\\n' \"$(printf %s \"$CLAVE\" | wc -c)\""
    environment = {
      CLAVE = ephemeral.random_password.efimera.result
    }
  }
}
```

En el apply se ve el ciclo de vida: `ephemeral.random_password.efimera: Opening...`, `Open complete`, y después `Closing...` y `Close complete`. El resultado en el estado: la contraseña **normal** aparece completa (`result`); de la efímera, el estado solo guarda la **dirección** de la que depende el recurso, no su valor.

---

## Dónde sí y dónde no se puede usar lo efímero

La documentación enumera los contextos efímeros. Un valor efímero **no** puede ir a un argumento normal de un recurso, porque ese argumento se guardaría. Sí puede ir a:

- un **provisioner** o un bloque `connection` (como en el ejemplo);
- la configuración de un **proveedor**;
- un **local** o una **salida** marcada como efímera;
- un **atributo de solo escritura** de un recurso, si el proveedor lo ofrece.

Los atributos de solo escritura se muestran siempre como `(write-only attribute)` en el plan y se guardan como `null` en el estado. Como OpenTofu no ve su valor remoto, no puede detectar cambios en ellos: por eso los proveedores suelen incluir un atributo compañero (de versión) cuyo cambio dispara la actualización.

**Adopción:** depende de cada proveedor. En esta clase, `hashicorp/local` 2.9.1 y `hashicorp/random` 3.9.1 traen recursos efímeros, pero ninguno de sus recursos administrados tiene un atributo de solo escritura (revisado con `tofu providers schema -json`). Ejemplos con recursos de nube serían **fragmentos**.

---

## Un secreto que no pasa por IaC

La mejor protección sigue siendo que el secreto **no pase** por tu código de infraestructura:

```
IaC crea:  el gestor de secretos y un permiso de lectura para el servicio
Servicio:  lee la contraseña del gestor cuando arranca
Humano:    rota el secreto en el gestor, sin tocar el estado
```

- El estado guarda una referencia (el nombre del secreto), no su valor.
- La rotación no requiere un `apply`.
- El cap. 6 de Brikman llama a este bloque "Secret stores"; la nota `10-cloud/05-secretos-produccion.md` muestra el patrón con servicios de nube.

Cuando es inevitable que el valor pase por IaC (por ejemplo, crear la base con su contraseña inicial), combina: valor efímero si el proveedor lo permite, cifrado del estado y del plan, acceso mínimo al backend y **rotación inmediata** del secreto inicial.

---

<!-- _class: lead -->

# Parte 3
## Probar infraestructura

---

## Qué se puede probar sin crear nada

Del cap. 9 de Brikman, "How to Test Terraform Code", se puede tomar su organización: pruebas manuales, automáticas (unitarias, de integración, de punta a punta) y otros enfoques (análisis estático, pruebas de plan, pruebas del servidor). Una escalera práctica para el curso:

| Nivel | Comando | Qué detecta | Cuesta |
|---|---|---|---|
| Formato | `tofu fmt -check -recursive` | Estilo inconsistente | Nada |
| Sintaxis y referencias | `tofu validate` | Argumentos que no existen, tipos mal | Nada |
| Plan | `tofu plan` | Lo que se crearía, destruiría o fallaría | Lee el estado |
| Pruebas | `tofu test` | Que el módulo **se comporta** como se espera | Puede crear y destruir recursos |

Cada escalón es más lento y más costoso. Se corren en ese orden: lo barato primero.

---

## `validate` y `fmt`, con su error

```bash
tofu validate
```

Con un argumento mal escrito (`contenido` en lugar de `content`), salida real:

```
Error: Unsupported argument

  on main.tf line 18, in resource "local_file" "nota":
  18:   contenido       = "${var.saludo}\n"

An argument named "contenido" is not expected here.
```

```bash
tofu fmt -check -recursive -diff   # sin cambios: sale con 0
tofu fmt -recursive                # reescribe al formato canónico
```

- `validate` no necesita credenciales ni toca la plataforma: se puede correr en cada commit.
- `fmt -check` en CI evita discusiones de estilo en los pull requests.

---

## `tofu test`

La documentación (opentofu.org/docs/cli/commands/test/): ejecuta cada archivo `*.tftest.hcl` (también `*.tftest.json`, `*.tofutest.hcl` y `*.tofutest.json`) en el directorio actual o en un directorio llamado `tests`. Con `command = apply` (el valor por omisión) crea infraestructura de verdad, comprueba las aserciones y la destruye al terminar; con `command = plan` solo planea.

```hcl
# tests/servicios.tftest.hcl
run "tres_servicios_por_nombre" {
  command = plan

  variables {
    entorno           = "dev"
    servicios         = ["api", "web", "worker"]
    directorio_salida = "salida-prueba"
  }

  assert {
    condition     = length(local_file.servicio) == 3
    error_message = "Debe haber un archivo por servicio."
  }
}
```

- Un `run` es un paso. Las `assert` deben referirse a recursos, data sources, variables, salidas o módulos del código principal.
- Opciones útiles: `-filter=archivo`, `-verbose` (muestra el plan o el estado de cada `run`), `-json`, `-no-color`.

---

## Esperar un fallo: `expect_failures`

Una buena prueba también verifica que lo inválido **se rechaza**:

```hcl
run "entorno_invalido_se_rechaza" {
  command = plan

  variables {
    entorno           = "produccion"
    servicios         = ["api"]
    directorio_salida = "salida-prueba"
  }

  expect_failures = [var.entorno]
}
```

- `expect_failures` lista las condiciones personalizadas (validaciones de variables, precondiciones, poscondiciones, checks) que **deben** fallar.
- Solo cubre condiciones escritas en la configuración, no la lógica de validación dentro de los proveedores.
- Si la validación **no** falla, la prueba falla: la regla dejó de proteger.

Salida real de la suite completa del módulo de la sesión 2:

```
tests/servicios.tftest.hcl... pass
  run "tres_servicios_por_nombre"... pass
  run "monitoreo_en_staging"... pass
  run "entorno_invalido_se_rechaza"... pass
  run "replicas_fuera_de_rango_se_rechazan"... pass
  run "aplica_y_el_contenido_es_el_esperado"... pass

Success! 5 passed, 0 failed.
```

---

## Verlo fallar: la prueba que atrapa un cambio

Alguien "simplifica" el módulo y deja el monitoreo **siempre** encendido (`count = 1` en lugar de `var.con_monitoreo ? 1 : 0`):

```
tests/servicios.tftest.hcl... fail
  run "tres_servicios_por_nombre"... fail

Error: Test assertion failed

  on tests/servicios.tftest.hcl line 22, in run "tres_servicios_por_nombre":
  22:     condition     = length(local_file.monitoreo) == 0
    ├────────────────
    │ local_file.monitoreo is tuple with 1 element
    ├────────────────
    │ Diff: 
    │     1 -> 0

Sin con_monitoreo no debe haber monitoreo.yaml.
...
Failure! 4 passed, 1 failed.
```

- El mensaje es **el que escribiste**: por eso `error_message` merece una oración clara.
- `tofu test` sale con **código 1** (comprobado) cuando una prueba falla: es lo que hace fallar el CI.
- Esa prueba cuesta menos que descubrirlo en staging.

---

## Pruebas que crean cosas de verdad

Con `command = apply` se crean recursos reales y luego se destruyen: la documentación indica que cada bloque `run` termina con un `destroy` de lo temporal.

| Tipo de prueba | Cuándo | Riesgo |
|---|---|---|
| `command = plan` | Toda lógica pura: validaciones, `for_each`, condicionales | Ninguno |
| `command = apply` con recursos locales | Contenido de archivos, valores calculados | Archivos temporales |
| `command = apply` en nube real | Integración con la plataforma | **Costo y residuos** si falla la limpieza |

La documentación también describe `mock_provider` y `override_resource` para probar sin crear nada real. El curso **no** las usa porque los proveedores locales ya son baratos; para recursos de nube, serían la primera opción (fragmentos en la nota de la sesión).

El cap. 9 de Brikman dedica secciones a las pruebas de integración y de punta a punta; se leen en la tarea, no se practican hoy.

---

<!-- _class: lead -->

# Parte 4
## El plan en cada pull request

---

## Por qué el plan va en el pull request

El cap. 10 de Brikman, "How to Use Terraform as a Team", describe el flujo para infraestructura: control de versiones, ejecutar el código local, hacer cambios, enviarlos a revisión, pruebas automáticas, fusionar y liberar, y desplegar. Incluye una sección titulada "The Golden Rule of Terraform", que conviene leer completa.

Lo que cambia respecto a código de aplicación: **revisar el diff de código no basta.** Un cambio de tres líneas puede destruir una base de datos. Lo que importa es el **plan**.

```
Código:  - count = 1
         + count = 0          <- parece inocente
Plan:    Plan: 0 to add, 0 to change, 1 to destroy.
```

Por eso el plan se calcula **automáticamente** en cada pull request y queda a la vista de quien revisa.

---

## Un workflow de plan por pull request

**Fragmento:** no corre en el curso (no hay un CI conectado al laboratorio). Es el esquema que se ejecutaría en GitHub Actions. Las acciones van fijadas por SHA con su etiqueta en el comentario; los SHA se tomaron de la API de GitHub el 11 de noviembre de 2026.

```yaml {1-6|8-13|15-23|24-32}
name: Plan de infraestructura

on:
  pull_request:
    paths:
      - "infra/**"

permissions:
  contents: read

concurrency:
  group: plan-${{ github.ref }}
  cancel-in-progress: true

jobs:
  plan:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: infra/entornos/staging
    env:
      TF_IN_AUTOMATION: "1"
      TF_VAR_passphrase: ${{ secrets.TOFU_PASSPHRASE }}
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          persist-credentials: false

      - uses: opentofu/setup-opentofu@a1320f892987e89d278cc92dc5adc984fb93aca4 # v2.0.2
        with:
          tofu_version: 1.13.9
          tofu_wrapper: false
```

---

## Los pasos del workflow

```yaml {1-2|4-8|10-14|16-17|19-26|28-33}
      - name: Formato
        run: tofu fmt -check -recursive -diff

      - name: Init
        run: tofu init -input=false

      - name: Validate
        run: tofu validate

      - name: Pruebas del módulo
        working-directory: infra/modulos/servicios
        run: |
          tofu init -input=false
          tofu test

      - name: Plan
        run: tofu plan -input=false -lock-timeout=120s -out=tfplan

      - name: Resumen para quien revisa
        run: |
          {
            echo "### Plan de staging"
            echo '```'
            tofu show -no-color tfplan
            echo '```'
          } >> "$GITHUB_STEP_SUMMARY"

      - name: Avisar de destrucciones
        run: |
          n=$(tofu show -json tfplan | jq '[.resource_changes[] | select(.change.actions | index("delete"))] | length')
          if [ "$n" -gt 0 ]; then
            echo "::warning title=Destrucciones::El plan destruye o reemplaza $n recurso(s). Revísalo antes de aprobar."
          fi
```

---

## Qué dice cada decisión del workflow

| Decisión | Por qué |
|---|---|
| `permissions: contents: read` | Con cualquier permiso declarado, los no declarados quedan en `none` (documentación de GitHub): el plan solo necesita leer el repositorio |
| `paths: "infra/**"` | Solo corre si el pull request toca infraestructura |
| `concurrency` con `cancel-in-progress` | Un push nuevo cancela el plan anterior de la misma rama |
| `persist-credentials: false` | El token del checkout no queda disponible a los pasos siguientes |
| `tofu_wrapper: false` | El README de la acción lo recomienda si hay problemas con códigos de salida o formato |
| `tofu_version: 1.13.9` | Versión **fijada**: el CI y las laptops usan la misma |
| `-lock-timeout=120s` | Si hay otro plan o apply, espera dos minutos antes de fallar |
| `tofu show -no-color tfplan` al resumen | Quien revisa ve el plan sin abrir los registros |
| `jq` sobre `show -json` **en la misma línea** | No se escribe `plan.json` a disco ni se sube como artefacto: tiene los valores de las variables |

El runner `ubuntu-latest` (imagen Ubuntu 24.04 versión 20261101.360.1) trae `jq` 1.7, según el README de `actions/runner-images`.

---

## Qué **no** hace este workflow

- **No aplica.** Un `apply` desde un pull request, sin revisión ni aprobación, es la receta del incidente. El `apply` va en otro workflow, después del merge y con un entorno protegido.
- **No sube el archivo de plan** como artefacto. Ya vimos por qué: puede traer secretos.
- **No configura credenciales de nube.** Con un backend remoto real, haría falta autenticarse; el cap. 6 de Brikman describe la opción de GitHub Actions con OIDC, que evita credenciales de larga vida. En el curso no hay nube, así que no se configura.
- **No sustituye a la revisión humana.** Genera la evidencia; la decisión es de una persona.

---

## Qué se mira en un plan

Una lista corta para quien revisa. Cada punto se puede ver en la salida de `tofu plan` o con el `jq` de la lámina anterior:

1. **Destroy y replace:** líneas con `destroyed`, `must be replaced` y `forces replacement`. ¿Se pierden datos?
2. **El conteo:** `Plan: X to add, Y to change, Z to destroy.` ¿Coincide con lo que dice la descripción del pull request?
3. **Cambios que nadie pidió:** recursos en el plan que el diff de código no toca. Es drift (sesión 1) o un cambio de versión.
4. **Valores sensibles que cambian:** `(sensitive value)` en un recurso que no debería cambiar.
5. **Seguridad:** puertos, permisos, acceso público, cifrado desactivado. (La sesión 6 lo vuelve automático con políticas.)
6. **Versiones:** cambios en `.terraform.lock.hcl` o en la versión de un módulo.

Detectar un reemplazo con `jq`, salida real:

```bash
tofu show -json cambio.plan | jq -c '.resource_changes[] | {address, actions: .change.actions}'
# {"address":"local_file.nota","actions":["delete","create"]}
# {"address":"terraform_data.lento","actions":["no-op"]}
```

`["delete","create"]` es el reemplazo. `["no-op"]` es "sin cambios".

---

## Error típico: aprobar un diff de código sin mirar el plan

| Lo que ve el revisor | Lo que no ve |
|---|---|
| Un `for_each` nuevo | Que cambió la clave de un recurso con datos y se reemplaza |
| Un valor de `count` | Que baja de 1 a 0 un recurso de producción |
| Una versión de módulo | Qué recursos cambian en esa versión |
| Una variable nueva con valor por omisión | Que el valor por omisión no es el de producción |

La regla del equipo que escribirás en la tarea cierra esa brecha: **ningún cambio de infraestructura se aprueba sin su plan a la vista.**

---

## Plantilla de regla de revisión

Para la tarea. Una regla de equipo cabe en una página:

1. **Quién revisa:** al menos una persona que **no** escribió el cambio, y que sepa leer un plan.
2. **Qué debe ver:** el plan del pull request, generado por CI con la versión de OpenTofu fijada.
3. **Qué bloquea:** destrucciones o reemplazos de recursos con datos sin una nota que explique el respaldo y la recuperación; cambios en permisos o acceso público sin segunda aprobación; cambios en el lock file sin explicación.
4. **Quién aplica:** el pipeline, después del merge, no una persona desde su laptop.
5. **Qué se archiva:** el resumen del plan; **no** el archivo de plan ni su JSON.
6. **Qué pasa en una emergencia:** quién puede aplicar fuera del flujo y cómo se documenta después.

---

<!-- _class: lead -->

# Parte 5
## Práctica: secretos, pruebas y CI

---

## Qué vas a hacer

La guía completa está en `practica.md`. En 65 minutos, por equipo:

1. **Verlo fallar:** un secreto `sensitive` que aparece completo en el estado, en la salida y en el plan guardado.
2. Cifrar el estado y el plan; ver el error de una frase equivocada y de una frase corta.
3. Comprobar la diferencia entre un valor normal y uno **efímero**.
4. Escribir y correr `tofu test` sobre el módulo de la sesión 2; ver una prueba fallar.
5. Simular localmente el trabajo del pull request: `fmt`, `validate`, `test`, `plan -out` y el aviso de destrucciones.
6. Validar con un analizador de YAML el workflow de CI (que **no** se ejecuta).

Evidencia: el estado con el secreto antes y después del cifrado, la salida de `tofu test`, el resumen del plan y el workflow validado.

---

<!-- _class: lead -->

# Cierre
## Errores comunes y qué sigue

---

## Resumen de errores de la sesión

| Error | Se ve así | Se evita así |
|---|---|---|
| Creer que `sensitive` protege el estado | `grep` encuentra la contraseña en `terraform.tfstate` | Cifrar estado y plan; valores efímeros; acceso mínimo |
| Subir `plan.json` como artefacto | Las variables con su valor, publicadas | Analizar en el mismo paso, sin guardar |
| Perder la frase de cifrado | `decryption failed for all provided methods` | Respaldarla en un gestor de secretos del equipo |
| Dejar el método `unencrypted` | Advertencia en cada comando | Quitar `fallback` tras migrar y usar `enforced = true` |
| Pruebas que no fallan nunca | Una regla borrada y el CI sigue verde | `expect_failures` y probar que la prueba cae al romper el código |
| Aprobar solo el diff | Un `1 to destroy` que nadie vio | El plan a la vista y una regla de revisión escrita |
| `apply` desde la laptop | El código y lo desplegado ya no coinciden | Aplicar desde el pipeline, tras el merge |

---

## Referencias

Todas consultadas el 11 de noviembre de 2026.

- OpenTofu, variables (`sensitive`, `ephemeral`, `TF_VAR_`, precedencia): https://opentofu.org/docs/language/values/variables/
- OpenTofu, datos sensibles en el estado: https://opentofu.org/docs/language/state/sensitive-data/
- OpenTofu, cifrado de estado y plan: https://opentofu.org/docs/language/state/encryption/
- OpenTofu, efimeralidad: https://opentofu.org/docs/language/ephemerality/ y atributos de solo escritura: https://opentofu.org/docs/language/ephemerality/write-only-attributes/
- OpenTofu, `plan` (archivo de plan, `-detailed-exitcode`, `-lock-timeout`): https://opentofu.org/docs/cli/commands/plan/
- OpenTofu, `tofu test`: https://opentofu.org/docs/cli/commands/test/
- OpenTofu, condiciones personalizadas: https://opentofu.org/docs/language/expressions/custom-conditions/
- `opentofu/setup-opentofu` v2.0.2 (README): https://github.com/opentofu/setup-opentofu
- GitHub Docs, sintaxis de workflows y comandos de workflow: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax y https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands
- GitHub, imagen del runner Ubuntu 24.04: https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2404-Readme.md
- Versiones de la clase: OpenTofu 1.13.9, `hashicorp/local` 2.9.1, `hashicorp/random` 3.9.1 (a noviembre de 2026).

**Libros:** *Terraform: Up and Running* (Brikman, 2022), caps. 6, 9 y 10.

---

## Tarea

**La regla de revisión de cambios de infraestructura de tu equipo.**

- Escribes la regla en una página (con la plantilla de la sesión), la pruebas con **tres cambios de infraestructura** que hayas simulado y decides cuáles aprueba o bloquea.
- Entregas el workflow de plan por pull request, validado con un analizador de YAML, y una prueba `tofu test` para tu módulo.
- Incluyes la lista de **dónde puede aparecer cada secreto** de tu proyecto.

El detalle y la rúbrica están en `tarea.md`.

---

## Siguiente sesión

### S04 - Ansible: configurar lo que se creó

- Terraform y OpenTofu **crean**; Ansible **configura**. ¿Dónde está la frontera?
- Inventario, playbooks, variables, hechos y roles.
- Un rol que endurece la VM: SSH sin contraseña, firewall y fail2ban.
- Verlo fallar: la segunda corrida debe reportar cero cambios.

> Un playbook que cambia algo en la segunda corrida no es idempotente.

---

## Quiz: secretos, pruebas y trabajo en equipo

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin la VM abierta.
- Lo que falles, búscalo en la documentación de variables, cifrado y `tofu test` antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
