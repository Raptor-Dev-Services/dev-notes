<!-- _class: lead -->

# S01 - Por qué IaC, y el estado

### Infraestructura como código

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Antes de empezar, cada equipo entra a su VM: ssh <USUARIO>@<IP_DE_SU_VM>.
Verifica que Docker responda (docker version) y que puedan abrir dos terminales a la vez: la práctica de hoy necesita las dos.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, entrar a la VM, instalar OpenTofu | 25 min |
| Parte 1 | El problema de la infraestructura a mano y el primer recurso | 50 min |
| Parte 2 | El estado: la memoria del sistema | 45 min |
| Parte 3 | Bloqueo del estado y backends remotos | 50 min |
| Parte 4 | Importar lo que ya existe y detectar drift | 40 min |
| Parte 5 | Práctica: estado remoto y verlo fallar | 60 min |
| Cierre | Errores comunes, tarea y quiz | 30 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. Los 300 minutos ya los incluyen. -->

---

## El lema de la sesión

> El estado de Terraform es la memoria del sistema: si dos lo escriben a la vez, se corrompe.

- Una herramienta de IaC **no mira tu infraestructura y decide**: compara tu código contra lo que **recuerda** haber creado.
- Si esa memoria se pierde, se desfasa o se escribe a medias, la herramienta actúa sobre un mundo que ya no existe.
- Hoy aprendemos a cuidar esa memoria **y a verla fallar** antes de que falle en un día malo.

<!-- Pregunta al grupo: ¿quién ha configurado un servidor siguiendo una lista de pasos de un documento? ¿Cuántos de esos pasos seguían vigentes seis meses después? -->

---

## Al terminar puedes

- Explicar por qué una infraestructura declarada en archivos se puede revisar, repetir y reconstruir, y una hecha a mano no.
- Escribir un primer recurso con OpenTofu y recorrer el ciclo `init`, `plan`, `apply` y `destroy`.
- Decir qué guarda el archivo de estado y por qué no se edita a mano.
- Guardar el estado en un backend remoto y **ver** el bloqueo que impide que dos personas lo escriban a la vez.
- Importar a código un recurso que hoy solo existe a mano.
- Detectar drift: lo que cambió fuera de tu código.

---

## Cómo trabajamos en este curso

- **La VM del curso de Linux**, con Ubuntu Server 24.04 LTS y Docker. Todo corre ahí.
- **Nada contra una nube real.** Los recursos son archivos, volúmenes de Docker y una base de datos en un contenedor. La nube real aparece solo como **fragmento** marcado, nunca como algo que se ejecuta en clase.
- La herramienta es **OpenTofu**. El libro de la unidad usa `terraform`; el lenguaje es el mismo, lo que cambia lo diremos expresamente.
- **La regla del curso:** si no lo vieron fallar, no lo probaron. Cada práctica trae un paso "Verlo fallar".

> Versiones de la clase, a noviembre de 2026: OpenTofu 1.13.9, proveedor `hashicorp/local` 2.9.1, proveedor `kreuzwerker/docker` 3.9.0, PostgreSQL 17.

---

<!-- _class: lead -->

# Apertura
## Instalar OpenTofu

---

## Instalar OpenTofu en la VM

Según la documentación oficial para Debian y Ubuntu (opentofu.org/docs/intro/install/deb/), hay un instalador y un método manual. El manual configura el repositorio de `apt` con dos llaves GPG:

```bash
# Fragmento: instalación por apt, tal como la documenta OpenTofu
sudo apt-get update
sudo apt-get install -y apt-transport-https ca-certificates curl gnupg
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://get.opentofu.org/opentofu.gpg | sudo tee /etc/apt/keyrings/opentofu.gpg >/dev/null
curl -fsSL https://packages.opentofu.org/opentofu/tofu/gpgkey | sudo gpg --no-tty --batch --dearmor -o /etc/apt/keyrings/opentofu-repo.gpg >/dev/null
sudo chmod a+r /etc/apt/keyrings/opentofu.gpg /etc/apt/keyrings/opentofu-repo.gpg
```

Después se crea `/etc/apt/sources.list.d/opentofu.list` con las líneas `deb` y `deb-src` de la documentación y se instala:

```bash
sudo apt-get update
sudo apt-get install -y tofu
tofu version
```

<!-- Pídeles que copien las líneas del sources.list de la página oficial, no de la lámina: así practican ir a la fuente. La documentación dice que las llaves permiten comprobar que los paquetes vienen del pipeline oficial y no fueron alterados. -->

---

## Lo que ya debes ver

```bash
tofu version
# OpenTofu v1.13.9
# on linux_amd64
```

- Si tu versión es otra (más nueva), todo lo de hoy sigue valiendo; los mensajes pueden variar en palabras.
- Para fijar la versión que exige **tu proyecto** se usa `required_version` en el bloque `terraform`, no la instalación de cada quien. Lo veremos en un minuto.
- Alternativa documentada: la imagen oficial de contenedor en GitHub Container Registry, útil para correr `tofu` sin instalar nada.

---

<!-- _class: lead -->

# Parte 1
## El problema de la infraestructura a mano y el primer recurso

---

## Una historia que reconoces

Caso hipotético. Un equipo levanta su servidor de pruebas un viernes siguiendo una lista de 23 pasos en un documento.

- Dos meses después alguien abre un puerto "temporalmente" para una demostración. No queda escrito.
- Seis meses después el servidor se descompone. Nadie recuerda cuáles de los 23 pasos eran los de verdad y cuáles se corrigieron en el camino.
- Para reconstruirlo, se pide a la única persona que lo armó, que está de vacaciones.

El problema no es la persona: es que **la infraestructura vive en la cabeza y en la consola**, no en un archivo que se revise.

<!-- Es un caso hipotético; no es de ningún cliente. Que cada equipo cuente una versión real que haya vivido (sin nombres). -->

---

## Qué ofrece la infraestructura como código

El cap. 1 de *Terraform: Up and Running* (Brikman, 2022) abre con "Why Terraform": qué es DevOps, qué es infraestructura como código y qué categorías de herramientas existen. Con tus palabras, lo que gana un equipo es:

| Antes (a mano) | Con IaC |
|---|---|
| Se ejecuta una lista de pasos | Se declara el resultado y la herramienta calcula los pasos |
| Nadie revisa un clic | Un cambio es un archivo que se revisa en un pull request |
| Reconstruir es arqueología | Reconstruir es volver a ejecutar |
| Dev y producción se parecen "más o menos" | Se parecen porque salen del mismo código |
| El historial está en la memoria de alguien | El historial está en Git |

Lo que **no** resuelve: no te dice si tu diseño es bueno, ni evita que alguien cambie algo por fuera. De eso último hablaremos con el nombre **drift**.

Fuente: Brikman, cap. 1.

---

## Declarativo frente a imperativo

El mismo objetivo, dos formas:

```bash
# Imperativo: tú dices los pasos. Segunda corrida: ¿qué pasa?
mkdir /srv/datos
echo "hola" > /srv/datos/nota.txt
```

```hcl
# Declarativo: tú dices el resultado. Segunda corrida: no cambia nada.
resource "local_file" "nota" {
  filename = "/srv/datos/nota.txt"
  content  = "hola\n"
}
```

- El script imperativo falla la segunda vez (`mkdir` dice que ya existe) o lo hace dos veces.
- La herramienta declarativa **compara** lo deseado contra lo que existe y hace solo la diferencia.
- Ese es el sentido de `plan`: mostrarte la diferencia **antes** de aplicarla.

El cap. 1 del libro compara también herramientas de configuración y de aprovisionamiento, lenguaje procedural y declarativo, entre otras dimensiones. Cada una merece su lectura.

---

## Qué es OpenTofu y por qué lo usamos

- **Terraform** es la herramienta de HashiCorp que popularizó este enfoque. El **10 de agosto de 2023** HashiCorp anunció que cambiaba la licencia de sus productos, de MPL 2.0 a la Business Source License (BSL) v1.1, para las versiones futuras (hashicorp.com/en/blog/hashicorp-adopts-business-source-license).
- **OpenTofu** es una bifurcación (fork) creada, según su propia FAQ, por Gruntwork, Spacelift, Harness, Env0, Scalr y otras empresas como respuesta a ese cambio. Está bajo la Linux Foundation y su repositorio usa la licencia **MPL-2.0** (consultado en la API de GitHub el 11 de noviembre de 2026).
- Compatibilidad, según la FAQ: funciona con archivos de estado hasta los creados con Terraform 1.5.x y con los proveedores actuales, pero usa un **registro distinto**.

Para el curso importa por una razón práctica: licencia abierta y el mismo lenguaje (HCL) que enseña el libro.

<!-- No hace falta tomar partido en la discusión comercial. El criterio del curso: el código HCL de hoy te sirve con cualquiera de las dos herramientas, y los ejemplos se verificaron con OpenTofu. -->

---

## Lo que cambia al usar `tofu` en lugar de `terraform`

Lo que **observamos** al correr OpenTofu 1.13.9:

| Cosa | Con OpenTofu |
|---|---|
| El comando | `tofu` |
| El bloque de configuración | sigue llamándose `terraform { ... }` |
| Archivos | `.tf`, `terraform.tfstate` y `.terraform.lock.hcl` conservan su nombre |
| Dónde busca proveedores | `registry.opentofu.org` (se ve en el estado: `registry.opentofu.org/hashicorp/local`) |
| Directorio de trabajo | `.terraform/` |

Cualquier ejemplo del libro con `terraform plan` se traduce a `tofu plan`. Si migras un proyecto existente, la página oficial de migración (opentofu.org/docs/intro/migration/) resume cuatro pasos: respaldar estado y código, instalar OpenTofu, inicializar y verificar, y probar con un cambio pequeño.

---

## El vocabulario mínimo

| Concepto | Qué es | Ejemplo |
|---|---|---|
| **Provider** | Plugin que sabe hablar con una plataforma | `hashicorp/local`, `kreuzwerker/docker` |
| **Resource** | Algo que la herramienta crea y administra | un archivo, un volumen, una VM |
| **Data source** | Algo que solo se **lee** | datos de un recurso que ya existe |
| **Variable** | Una entrada de tu configuración | `var.saludo` |
| **Output** | Un valor que tu configuración publica | la ruta del archivo |
| **State** | Lo que la herramienta recuerda haber creado | `terraform.tfstate` |

Todo esto vive en archivos `.tf` escritos en **HCL**. La convención es separarlos (`main.tf`, `variables.tf`, `outputs.tf`), pero a la herramienta le da igual: lee todos los `.tf` de la carpeta.

---

## El primer recurso

```hcl
terraform {
  required_version = ">= 1.6.0"
  required_providers {
    local = {
      source  = "hashicorp/local"
      version = "~> 2.5"
    }
  }
}

variable "saludo" {
  type    = string
  default = "hola desde OpenTofu"
}

resource "local_file" "nota" {
  filename        = "${path.module}/salida/nota.txt"
  content         = "${var.saludo}\n"
  file_permission = "0644"
}

output "ruta" {
  value = local_file.nota.filename
}
```

<!-- Este bloque corrió completo con OpenTofu 1.13.9 y local 2.9.1. En la práctica lo escriben ustedes. -->

---

## Lo que dice cada parte

```hcl {1-8|10-13|15-19|21-23}
terraform {
  required_version = ">= 1.6.0"
  required_providers {
    local = {
      source  = "hashicorp/local"
      version = "~> 2.5"
    }
  }
}

variable "saludo" {
  type    = string
  default = "hola desde OpenTofu"
}

resource "local_file" "nota" {
  filename        = "${path.module}/salida/nota.txt"
  content         = "${var.saludo}\n"
  file_permission = "0644"
}

output "ruta" {
  value = local_file.nota.filename
}
```

- **Primer paso:** `required_version` pone un piso para la herramienta; `required_providers` dice de dónde viene el plugin y qué versiones acepta (`~> 2.5` significa 2.5 o superior dentro de la 2.x).
- **Segundo:** una variable con tipo y valor por omisión.
- **Tercero:** un recurso. `local_file` es el tipo, `nota` es su nombre local. `${var.saludo}` interpola la variable.
- **Cuarto:** un output que lee un atributo del recurso: así un recurso se **refiere** a otro, y la herramienta deduce el orden.

---

## `init`: descargar lo necesario

```bash
tofu init
```

Salida real (recortada, OpenTofu 1.13.9):

```
Initializing provider plugins...
- Finding hashicorp/local versions matching "~> 2.5"...
- Installing hashicorp/local v2.9.1 ...
- Installed hashicorp/local v2.9.1 (signed, key ID 0C0AF313E5FD9F80)

OpenTofu has created a lock file .terraform.lock.hcl to record the provider
selections it made above. Include this file in your version control repository
so that OpenTofu can guarantee to make the same selections by default when you
run "tofu init" in the future.
```

- `init` crea `.terraform/` (plugins; **no se versiona**) y `.terraform.lock.hcl` (**sí se versiona**).
- El archivo de bloqueo fija la versión exacta del proveedor (`2.9.1`) y sus huellas, para que todo el equipo use lo mismo.
- Hay que correrlo de nuevo al cambiar de backend o de proveedores.

<!-- El mensaje real de OpenTofu 1.13.9 cambia de redacción entre versiones; lo importante es que el archivo se versiona. -->

---

## `plan`: la diferencia, antes de aplicar

```bash
tofu plan
```

```
  # local_file.nota will be created
  + resource "local_file" "nota" {
      + content              = <<-EOT
            hola desde OpenTofu
        EOT
      + content_sha256       = (known after apply)
      + file_permission      = "0644"
      + filename             = "./salida/nota.txt"
      + id                   = (known after apply)
      ...
    }

Plan: 1 to add, 0 to change, 0 to destroy.
```

| Símbolo | Significado |
|---|---|
| `+` | se crea |
| `-` | se destruye |
| `~` | se modifica en sitio |
| `-/+` | se destruye y se vuelve a crear (**reemplazo**) |

Un `plan` **no cambia nada**. Pero un plan sin guardar no es una promesa: entre el `plan` y el `apply` el mundo puede moverse. Para eso existe `-out`.

---

## `apply` y `destroy`

```bash
tofu apply            # muestra el plan y pide aprobación
tofu apply -auto-approve   # sin pregunta: solo en automatizaciones revisadas
tofu destroy          # el plan inverso: borra lo que el estado dice que creaste
```

- `apply` crea el plan y pide aprobación antes de actuar. `-auto-approve` omite esa aprobación interactiva y se queda para canalizaciones **donde el plan ya se revisó**. Con un plan guardado, pasar el archivo cuenta como aprobación.
- `destroy` solo borra lo que **el estado** conoce. Si el estado se perdió, `destroy` cree que no hay nada que borrar.
- Las tres operaciones leen y escriben el estado. Eso las hace sensibles al tema de la sesión.

Fuente: documentación de OpenTofu (opentofu.org/docs/cli/commands/apply/ y plan/), a noviembre de 2026.

---

## Un plan guardado es una promesa

```bash
tofu plan -var 'saludo=hola de nuevo' -out=cambio.plan
tofu show cambio.plan           # legible
tofu show -json cambio.plan     # para máquinas (lo usaremos en la sesión 6)
tofu apply cambio.plan          # aplica EXACTAMENTE ese plan
```

Salida real del plan (recortada):

```
  # local_file.nota must be replaced
-/+ resource "local_file" "nota" {
      ~ content              = <<-EOT # forces replacement
          - hola desde OpenTofu
          + hola de nuevo
        EOT
      ...
Plan: 1 to add, 0 to change, 1 to destroy.
```

- La etiqueta `# forces replacement` dice **qué atributo** obliga a destruir y crear. Aprende a buscarla.
- En `local_file`, cambiar `content` reemplaza el recurso. En otros recursos el mismo cambio sería una modificación en sitio.
- La documentación advierte que el archivo de plan guardado contiene la configuración completa y las variables, y que los valores sensibles pueden quedar en texto plano si no se cifra el plan. En la sesión 3 veremos qué hacer con él.

---

## Error típico 1: leer solo la última línea

`Plan: 1 to add, 0 to change, 1 to destroy.` no dice **qué** se destruye. Tres hábitos que evitan sustos:

1. Buscar `forces replacement` y `must be replaced`.
2. Contar los destroy: un `1 to destroy` en un cambio "inocente" merece una pregunta.
3. Preguntar "¿esto perdería datos?" por cada recurso con estado propio (una base de datos, un volumen).

En un pull request, quien revisa debería poder contestar esas tres sin ejecutar nada. Es el tema de la sesión 3.

---

<!-- _class: lead -->

# Parte 2
## El estado: la memoria del sistema

---

## Qué es el estado

Según la documentación de OpenTofu (opentofu.org/docs/language/state/), el estado sirve para **mapear los recursos del mundo real con tu configuración**, además de guardar metadatos y mejorar el rendimiento en infraestructuras grandes. Por omisión se guarda en un archivo local llamado `terraform.tfstate`.

```bash
tofu state list
# local_file.nota
# terraform_data.lento

tofu state show local_file.nota
```

```
# local_file.nota:
resource "local_file" "nota" {
    content              = <<-EOT
        hola desde OpenTofu
    EOT
    content_sha1         = "2b76913696ee7bb2f6808afb08cd874424898cc1"
    file_permission      = "0644"
    filename             = "./salida/nota.txt"
    id                   = "2b76913696ee7bb2f6808afb08cd874424898cc1"
    ...
}
```

---

## Cómo se ve por dentro

El archivo es JSON. Abre el `terraform.tfstate` de tu práctica:

```json
{
  "version": 4,
  "terraform_version": "1.13.9",
  "serial": 2,
  "lineage": "5767e049-1148-675d-54e3-2b2f88cfc8ab",
  "outputs": { "ruta": { "value": "./salida/nota.txt", "type": "string" } },
  "resources": [
    {
      "mode": "managed",
      "type": "local_file",
      "name": "nota",
      "provider": "provider[\"registry.opentofu.org/hashicorp/local\"]",
      "instances": [ { "attributes": { "content": "hola desde OpenTofu\n", ... } } ]
    }
  ]
}
```

- `serial` sube con cada cambio del estado. `lineage` identifica **esa** historia de estado: dos estados con distinto `lineage` no son el mismo.
- Guarda **todos los atributos** de cada recurso, incluido `content`. Si el recurso tuviera una contraseña, estaría aquí.
- La documentación advierte: la edición directa del archivo está desaconsejada; para modificaciones básicas se usa `tofu state`.

<!-- Los valores de serial y lineage son de una corrida de laboratorio; en tu archivo serán otros. -->

---

## El estado guarda secretos en texto plano

La página "Sensitive Data in State" de la documentación dice que, con estado local, el estado se guarda en **archivos JSON en texto plano** y contiene el ID y **todos los atributos** de cada recurso, lo que puede incluir contraseñas iniciales de bases de datos.

Consecuencias:

- El archivo de estado se trata con el mismo cuidado que el secreto más delicado que contenga.
- **Nunca** va a Git: el `.gitignore` de un proyecto de IaC empieza con `*.tfstate*` y `.terraform/`.
- Marcar una variable `sensitive` oculta el valor en la pantalla; **no lo quita del estado**. Lo comprobaremos en la sesión 3.

---

## Para qué sirve recordar

Sin estado, `plan` solo podría comparar tu código con la plataforma entera, y no sabría cuáles de los objetos que existen **son suyos**.

| Pregunta de la herramienta | La contesta |
|---|---|
| ¿Este recurso ya lo creé yo? | el estado |
| ¿Qué ID tiene en la plataforma? | el estado |
| ¿Qué depende de qué, para borrar en orden? | el estado y el código |
| ¿Cambió algo desde la última vez? | el estado frente a la plataforma (`refresh`) |

Por eso un estado perdido es grave: la herramienta ya no reconoce **sus** recursos. Si vuelves a aplicar, intenta crear de nuevo lo que ya existe, y el peor caso es que choque o que duplique.

---

## Error típico 2: el estado en tu laptop

Con el backend local, el estado vive en la carpeta del proyecto de **quien corrió `apply`**.

- Si quien lo corrió se va, se queda con la memoria de la infraestructura.
- Si dos personas aplican desde su laptop, cada una tiene **su** memoria. La primera crea el recurso; la segunda no sabe que existe y lo intenta de nuevo.
- Si el disco se daña, se pierde el mapa entre el código y la infraestructura.

El cap. 3 de Brikman, "How to Manage Terraform State", parte justo de aquí: el almacenamiento compartido del estado y el aislamiento entre entornos.

Fuente: Brikman, cap. 3.

---

## Descanso de 10 minutos

Al regresar: **dos personas, un estado**.

<!-- Descanso. Antes de salir, que dejen el apply del primer recurso corrido en su VM; al volver lo necesitan. -->

---

<!-- _class: lead -->

# Parte 3
## Bloqueo del estado y backends remotos

---

## Qué hace un backend

Un **backend** decide **dónde** se guarda el estado y **cómo** se protege. Se declara dentro del bloque `terraform`:

```hcl
# Fragmento: el backend local es el de siempre; aquí se escribe explícito.
terraform {
  backend "local" {
    path = "terraform.tfstate"
  }
}
```

Un backend remoto da tres cosas que el archivo local no puede:

1. **Un solo lugar** compartido por todo el equipo.
2. **Control de acceso** y cifrado en reposo (según el backend).
3. **Bloqueo**: mientras una operación escribe, ninguna otra puede.

La documentación aclara que no todos los backends soportan bloqueo; cada uno lo dice en su página.

---

## El bloqueo, según la documentación

De la página oficial de bloqueo de estado (opentofu.org/docs/language/state/locking/):

- El bloqueo ocurre **automáticamente** en todas las operaciones que podrían escribir el estado.
- Se puede desactivar en la mayoría de los comandos con `-lock=false`, pero **no se recomienda**.
- `force-unlock` solo debe usarse para quitar **tu propio** bloqueo cuando el desbloqueo automático falló. Requiere el ID del bloqueo, que OpenTofu imprime si el desbloqueo falla.

Es lo que dice el lema: dos escritores a la vez corrompen el estado. El bloqueo **no evita** que dos personas lo intenten; evita que lo logren las dos.

---

## Verlo con el backend local

Un recurso lento hace visible la ventana de la carrera:

```hcl
resource "terraform_data" "lento" {
  provisioner "local-exec" {
    command = "sleep 25"
  }
}
```

Terminal 1: `tofu apply -auto-approve`. En seguida, Terminal 2: lo mismo.

```
Error: Error acquiring the state lock

Error message: resource temporarily unavailable
Lock Info:
  ID:        7694c37d-469a-0b2a-4b7e-52d3fed4721c
  Path:      terraform.tfstate
  Operation: OperationTypeApply
  Who:       raptor@raptor-server
  Version:   1.13.9
  Created:   2026-11-11 13:15:19.775763437 +0000 UTC
```

- `Who` y `Created` dicen quién tiene el bloqueo y desde cuándo: un compañero que sí está aplicando, o un proceso que murió.
- Con el backend local, el bloqueo solo protege **en la misma máquina**: dos laptops distintas tienen dos archivos distintos y ningún bloqueo en común.

<!-- ID, usuario y hora son de una corrida de laboratorio. En su VM aparecerá su usuario. -->

---

## Esperar en lugar de fallar: `-lock-timeout`

```bash
tofu apply -auto-approve -lock-timeout=5s
```

```
Acquiring state lock. This may take a few moments...

Error: Error acquiring the state lock
```

- Según la documentación del comando, OpenTofu reintenta obtener el bloqueo durante ese tiempo antes de devolver un error (la duración es un número con unidad, como `3s`). Aquí esperó 5 segundos y falló igual.
- En una canalización de CI, un `-lock-timeout` de uno o dos minutos evita que dos pipelines seguidos se estorben, sin intervención humana.
- `-lock=false` desactiva el bloqueo; la documentación de `plan` advierte que es peligroso si otras personas pueden correr comandos sobre el mismo workspace. Es la opción que **rompe** la protección: nunca para escribir.

Salida real de `tofu plan -lock=false` mientras otro apply corría: la planificación terminó sin error. Eso no quiere decir que sea correcta: leyó un estado que otro está a punto de cambiar.

---

## Backend `pg`: el estado en PostgreSQL

La documentación (opentofu.org/docs/language/settings/backends/pg/) lo describe así:

```hcl
terraform {
  backend "pg" {}
}
```

```bash
export PG_CONN_STR='postgres://usuario:clave@127.0.0.1:5432/estado?sslmode=disable'
tofu init
```

- La cadena de conexión puede ir en el bloque (`conn_str`) o en la variable de entorno `PG_CONN_STR`. **La contraseña no va en el código**: por eso se deja el bloque vacío y se pasa por entorno.
- El backend crea el esquema `terraform_remote_state` y la tabla `states`, con una fila por workspace (`default` si no se usan workspaces).
- El bloqueo usa **advisory locks** de PostgreSQL.

Es el backend de la práctica porque corre en un contenedor, sin nube ni costo, y es un servidor real compartido.

---

## El mismo bloqueo, ahora entre dos clientes

Con el backend `pg`, dos terminales, un servidor PostgreSQL 17:

```
Error: Error acquiring the state lock

Error message: Workspace is already locked: default
Lock Info:
  ID:        5b2781e4-b50b-f81c-b516-9f871cc53b33
  Path:
  Operation: OperationTypeApply
  Who:       raptor@raptor-server
  Version:   1.13.9
  Created:   2026-11-11 13:18:30.311726664 +0000 UTC
```

Y el estado, visto directamente en la base:

```sql
select id, name, length(data) from terraform_remote_state.states;
--  id |  name   | length
-- ----+---------+--------
--   1 | default |   1485
```

- Ahora el bloqueo **sí** protege a todos los clientes del mismo servidor, estén donde estén.
- El estado es una fila de texto: quien tenga acceso a la tabla tiene acceso a todos los secretos que el estado contenga.

---

## `force-unlock` y los bloqueos del pg

La documentación del backend `pg` dice dos cosas que hay que juntar:

1. El bloqueo se hace con advisory locks de PostgreSQL, que se **liberan solos** al terminar la sesión o perderse la conexión.
2. Por eso `force-unlock` **no está soportado** en ese backend. Los bloqueos pendientes se pueden ver en la vista del sistema `pg_locks`.

Un proceso que muere a medias, en `pg`, no deja un bloqueo huérfano. En otros backends sí puede quedar, y ahí aplica la advertencia de `force-unlock`:

> Solo si estás seguro de que **nadie más** tiene el bloqueo, y con el ID exacto que OpenTofu te imprimió.

<!-- Ejercicio mental: ¿qué pasa si fuerzas un desbloqueo mientras otra persona aplica? Que ahora dos escritores corren a la vez. Es justo el daño que el bloqueo impedía. -->

---

## Otros backends: S3 con bloqueo propio

Fragmento (no se ejecuta en clase, sin nube): la documentación de OpenTofu para el backend `s3` describe el bloqueo nativo con `use_lockfile = true`, que crea un objeto de bloqueo **junto al objeto de estado** en el mismo bucket, y lo llama la opción preferida. También sigue soportado el bloqueo con una tabla de DynamoDB; la documentación dice que el equipo no tiene planes de retirar ninguno de los dos.

```hcl
# Fragmento ilustrativo: valores de ejemplo, no corre en el laboratorio.
terraform {
  backend "s3" {
    bucket       = "mybucket"
    key          = "path/to/my/key"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }
}
```

La nota `09-cicd/04-terraform.md` de dev-notes usa la variante con DynamoDB. Ambas funcionan; la lámina de arriba es la que la documentación presenta como preferida (a noviembre de 2026).

---

## Mover el estado local a un backend remoto

Fragmento: se agrega el bloque `backend` a un proyecto que ya tiene estado local y se vuelve a inicializar.

```bash
# Fragmento: proceso documentado; en la práctica empezamos ya con el backend pg.
tofu init -migrate-state
```

- Según la documentación, `-migrate-state` intenta **copiar** el estado existente al backend nuevo; para actualizar la configuración del backend hay que pasar `-reconfigure` (que descarta la configuración anterior y **no** migra nada) o `-migrate-state`.
- Antes de migrar: respaldo del `terraform.tfstate` (una copia, fuera de Git).
- Después de migrar: el archivo local ya no manda. **Bórralo** para que nadie lo use por error.

Fuente: opentofu.org/docs/cli/commands/init/, a noviembre de 2026.

---

## Qué elegir

| Necesidad | Opción |
|---|---|
| Aprender, un solo usuario | backend local |
| Equipo pequeño con un PostgreSQL ya operado | `pg` |
| Nube de AWS | `s3` con `use_lockfile` (o DynamoDB) |
| No quiero operar nada | un servicio administrado de la plataforma que uses |

Independiente del backend, hay reglas:

1. **Un estado por entorno** (lo vemos en la sesión 2).
2. **Acceso mínimo** al estado: guarda secretos.
3. **Versionado o respaldo** del estado, según lo que ofrezca el backend.
4. **Bloqueo activo**: si tu backend no lo tiene, no lo uses para un equipo.

---

## Error típico 3: forzar el bloqueo "para que ya avance"

Una escena frecuente: el pipeline de un compañero se colgó y dejó el bloqueo. Alguien corre `force-unlock` sin preguntar.

Lo que sale mal:

- El pipeline **no** se había colgado: terminaba un `apply` largo, y ahora dos escritores trabajan sobre el mismo estado.
- Resultado posible: recursos creados que el estado no registró, o registrados que ya no existen.
- Reparar eso requiere `tofu import` y `tofu state rm` a mano, con riesgo de borrar lo que no debía.

Regla del equipo: antes de forzar, **se localiza a quien tiene el bloqueo** (`Who`, `Created`) y se pregunta. Si es un proceso muerto, se confirma que ya no corre.

---

<!-- _class: lead -->

# Parte 4
## Importar lo que ya existe y detectar drift

---

## El día uno de casi todos

Nadie empieza con un terreno limpio. Hoy hay recursos creados a mano que quieres pasar a código **sin destruirlos**.

Dos pasos:

1. **Describir** el recurso en HCL, con sus atributos actuales.
2. **Importar**: asociar el recurso real con la dirección de tu código en el estado.

Si haces solo el paso 2, el próximo `plan` compara tu código vacío contra el recurso y lo propone destruir. Si haces solo el 1, el próximo `apply` intenta crear uno nuevo.

---

## El bloque `import`

La documentación (opentofu.org/docs/language/import/) lo describe como un bloque que se **procesa durante el plan**; al aprobar el plan, el apply importa el objeto al estado. Campos: `to` (la dirección en tu código), `id` (la identificación remota) y, opcionalmente, `provider` y `for_each`.

```hcl
import {
  to = docker_volume.datos
  id = "a07-datos"
}
```

- Un volumen creado a mano con `docker volume create a07-datos` se importa por su **nombre**.
- Cada tipo de recurso define su propio formato de `id`: está en la documentación del proveedor. En el proveedor `kreuzwerker/docker`, un `docker_container` se importa por el **ID largo** del contenedor, no por su nombre (con el nombre falla: lo vimos).
- Una vez importado, el bloque `import` se puede borrar: ya hizo su trabajo.

---

## Que la herramienta escriba el HCL: `-generate-config-out`

```bash
tofu plan -generate-config-out=generado.tf
```

Salida real con un volumen de Docker importado:

```hcl
# __generated__ by OpenTofu from "a07-datos"
resource "docker_volume" "datos" {
  driver      = "local"
  driver_opts = {}
  name        = "a07-datos"
}
```

- La documentación lo marca como **experimental**: el formato generado puede cambiar entre versiones.
- Lo generado es un **punto de partida**: se revisa, se limpia y se mueve a tu `main.tf`.
- No funciona con `for_each` dentro de `import`.

Resultado del apply: `Apply complete! Resources: 1 imported, 0 added, 0 changed, 0 destroyed.` Y el siguiente plan: `No changes. Your infrastructure matches the configuration.`

---

## Error típico 4: la configuración generada no es la verdad

Con un contenedor importado y `kreuzwerker/docker` 3.9.0, el proceso se comportó así:

1. `tofu plan -generate-config-out=...` generó el archivo, pero la validación posterior falló: `'' doesn't match the pattern '^\d+([,-]\d+)*$'`, por un `cpu_set = ""` generado por el propio proceso.
2. Corregido eso, el plan dijo `1 to import, 1 to add, 0 to change, 1 to destroy` y `# docker_container.web must be replaced`, con la advertencia: `this will destroy the imported resource`. La causa: la variable de entorno del contenedor (`env`) no se leyó al importar y el atributo `env` fuerza reemplazo.

Lecciones:

- **Lee el plan después de importar**: quieres ver `No changes`, no `must be replaced`.
- Cada proveedor importa distinto. Un recurso con estado (una base, un volumen con datos) **nunca** se aplica sin leer qué pasará.
- Si el plan propone reemplazar lo que importaste, **no apliques**: ajusta el código hasta que el plan sea limpio.

<!-- Es un hallazgo de la verificación del material con docker 3.9.0; con otra versión del proveedor puede variar. Lo importante es el hábito. -->

---

## Drift: cuando el mundo cambió por fuera

Drift es la diferencia entre lo que tu estado cree y lo que existe. Alguien edita, borra o crea algo a mano.

```bash
echo "editado a mano" > salida/nota.txt
tofu plan
```

Salida real:

```
  # local_file.nota will be created
  + resource "local_file" "nota" {
      + content = <<-EOT
            hola desde OpenTofu
        EOT
      ...
Plan: 1 to add, 0 to change, 0 to destroy.
```

- El proveedor `local` considera que un archivo con contenido distinto **ya no es** el que creó, y propone volver a crearlo. Cada proveedor decide cómo detecta el cambio.
- `tofu plan -detailed-exitcode` termina con **código 0** si no hay cambios, **1** si hay un error y **2** si hay cambios (documentación de `plan`): útil para vigilar drift en CI. Con el drift anterior, el código fue 2.

---

## Aceptar el cambio o deshacerlo

Dos decisiones distintas ante un drift:

| Decisión | Cómo |
|---|---|
| **Revertir**: la infraestructura debe volver a lo que dice el código | `tofu apply` |
| **Aceptar**: lo cambiado a mano es lo correcto | actualizar el código para que coincida y `tofu apply -refresh-only` para que el estado lo registre |

```bash
tofu apply -refresh-only
```

Un `-refresh-only` **solo actualiza el estado**; no cambia la infraestructura. La salida real, tras borrar a mano un volumen: mostró `- id = "a07-datos" -> null` y terminó con `Apply complete! Resources: 0 added, 0 changed, 0 destroyed.`

Lo importante: el drift **se decide**, no se descubre al azar un viernes. Un plan periódico en CI (con `-detailed-exitcode`) lo vuelve rutina.

---

## Lo que no se debe hacer con el estado

| No | Por qué | En su lugar |
|---|---|---|
| Editar `terraform.tfstate` a mano | La documentación lo desaconseja; un JSON roto invalida todo | `tofu state mv`, `tofu state rm`, `tofu import` |
| Subir el estado a Git | Contiene secretos en claro | Un backend remoto con acceso controlado |
| Compartirlo por correo o chat | Mismo motivo | Lo mismo |
| Borrarlo "para empezar limpio" | Pierdes el mapa; lo creado sigue existiendo | `tofu destroy` primero, o importar |
| `-lock=false` para escribir | Rompe la protección | `-lock-timeout` |

---

<!-- _class: lead -->

# Parte 5
## Práctica: estado remoto y verlo fallar

---

## Qué vas a hacer

La guía completa está en `practica.md`. En 60 minutos, por equipo:

1. Levantar un PostgreSQL 17 en un contenedor y configurarlo como backend.
2. Crear el primer recurso con el estado en la base.
3. **Verlo fallar:** dos `apply` a la vez y el bloqueo que lo impide.
4. Ver el estado como fila de la tabla `states`.
5. Importar un volumen de Docker creado a mano.
6. Provocar drift y leerlo en el plan.

Evidencia: el `main.tf`, las salidas de los dos intentos simultáneos y el plan con `No changes` tras importar.

<!-- Reparte las tareas: una persona de cada equipo "es" la terminal 1 y otra la terminal 2. La práctica sale mejor con dos teclados. -->

---

## Antes de empezar

```bash
mkdir -p ~/iac/s01 && cd ~/iac/s01
docker run -d --name a07-pg -e POSTGRES_PASSWORD=lab-no-usar -e POSTGRES_DB=estado \
  -p 127.0.0.1:15432:5432 postgres:17
export PG_CONN_STR='postgres://postgres:lab-no-usar@127.0.0.1:15432/estado?sslmode=disable'
```

- La contraseña `lab-no-usar` es de laboratorio. El contenedor escucha solo en `127.0.0.1`.
- El `export` va en **cada terminal** que uses.
- Al terminar, el contenedor se borra: `docker rm -f a07-pg`.

---

<!-- _class: lead -->

# Cierre
## Errores comunes y qué sigue

---

## Resumen de errores de la sesión

| Error | Se ve así | Se evita así |
|---|---|---|
| Estado en la laptop de una persona | "En mi máquina funciona" | Backend remoto con bloqueo |
| Estado en Git | Un `.tfstate` en el repositorio | `.gitignore` y revisión del primer commit |
| Leer solo la última línea del plan | Un reemplazo inesperado | Buscar `forces replacement` |
| `force-unlock` sin preguntar | Dos escritores a la vez | Localizar quién tiene el bloqueo |
| Importar sin leer el plan | `must be replaced` | Plan limpio antes de aplicar |
| Dejar el drift sin decidir | Un `apply` que revierte un arreglo urgente | Plan periódico y decisión explícita |

---

## Referencias

Todas consultadas el 11 de noviembre de 2026.

- OpenTofu, estado: https://opentofu.org/docs/language/state/ y datos sensibles en el estado: https://opentofu.org/docs/language/state/sensitive-data/
- OpenTofu, bloqueo del estado: https://opentofu.org/docs/language/state/locking/
- OpenTofu, backend `pg`: https://opentofu.org/docs/language/settings/backends/pg/
- OpenTofu, backend `s3`: https://opentofu.org/docs/language/settings/backends/s3/
- OpenTofu, importar: https://opentofu.org/docs/language/import/
- OpenTofu, instalación en Debian y Ubuntu: https://opentofu.org/docs/intro/install/deb/
- OpenTofu, migración: https://opentofu.org/docs/intro/migration/
- OpenTofu, preguntas frecuentes: https://opentofu.org/faq/
- HashiCorp, "HashiCorp adopts Business Source License" (10 de agosto de 2023): https://www.hashicorp.com/en/blog/hashicorp-adopts-business-source-license
- Versiones: OpenTofu 1.13.9 (publicada el 5 de noviembre de 2026, repositorio github.com/opentofu/opentofu).

**Libros:** *Terraform: Up and Running* (Brikman, 2022), caps. 1 a 3.

---

## Tarea

**Importar a código un recurso que hoy existe solo a mano.**

- Creas a mano un volumen de Docker con datos, lo describes en HCL, lo importas y llegas a un plan que dice `No changes`.
- Entregas el código, el plan antes de limpiar y después, y una explicación de qué estado guardaría tu equipo y dónde.
- Lecturas: `09-cicd/04-terraform.md` y la documentación de `import`.

El detalle, la rúbrica y el plazo están en `tarea.md`.

---

## Siguiente sesión

### S02 - Módulos y entornos

- Qué es un módulo y por qué copiar y pegar un entorno es la forma más rápida de que dev y producción dejen de parecerse.
- Variables con validación, `for_each` y por qué `count` te puede destruir recursos al mover un elemento de una lista.
- `moved` y refactorizar sin destruir.
- Un módulo reutilizable con versión fijada.

> Copiar y pegar un entorno es la forma más rápida de que dev y producción dejen de parecerse.

---

## Quiz: por qué IaC, y el estado

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin la VM abierta: se contesta con lo que entendiste, no con lo que copias.
- Lo que falles, búscalo en la documentación del estado y del bloqueo antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
