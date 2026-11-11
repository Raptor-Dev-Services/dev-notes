<!-- _class: lead -->

# S02 - Módulos y entornos

### Infraestructura como código

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Pide que cada equipo traiga a la mano el main.tf de la tarea de la sesión 1: lo usaremos como punto de partida.
Antes de empezar, que verifiquen tofu version y que su carpeta ~/iac/ exista.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos y repaso del estado | 20 min |
| Parte 1 | Por qué módulos: el problema de copiar y pegar | 35 min |
| Parte 2 | Variables, validación, locals y outputs | 40 min |
| Parte 3 | Ciclos y condicionales, y por qué `count` te puede destruir cosas | 55 min |
| Parte 4 | Entornos: carpetas o workspaces | 35 min |
| Parte 5 | Versionar módulos, fijar proveedores y refactorizar con `moved` | 35 min |
| Parte 6 | Práctica: un módulo para dev y staging, y verlo fallar | 60 min |
| Cierre | Errores comunes, tarea y quiz | 20 min |

<!-- Descansos de 10 minutos entre la Parte 2 y la 3, y entre la 5 y la práctica. Los 300 minutos ya los incluyen. -->

---

## El lema de la sesión

> Copiar y pegar un entorno es la forma más rápida de que dev y producción dejen de parecerse.

- El día que alguien arregla algo en una copia y no en la otra, tienes dos entornos que **parecen** iguales.
- Un módulo es **un solo lugar** donde se decide cómo se hace algo. Las diferencias entre entornos se declaran como parámetros, a la vista.
- Hoy aprendemos a escribir ese lugar único y a **no destruir infraestructura** al mover una lista.

---

## Al terminar puedes

- Escribir un módulo con entradas, locals y salidas, y llamarlo desde dos entornos con valores distintos.
- Rechazar valores inválidos antes de que lleguen a la infraestructura, con `validation`.
- Elegir entre `count` y `for_each`, y explicar qué hace cada uno cuando quitas un elemento del medio de una lista.
- Separar entornos con carpetas o con workspaces, y decir cuándo conviene cada uno.
- Fijar las versiones de proveedores y de módulos.
- Refactorizar un recurso a otro nombre o dirección sin destruirlo, con `moved`.

---

## Repaso de la sesión 1 en un minuto

- El **estado** es la memoria: mapea tu código con los recursos reales.
- El **bloqueo** impide que dos operaciones lo escriban a la vez.
- `import` pasa a código lo que existió a mano; `plan` te dice si lo entendiste (`No changes`).
- **Drift:** lo que cambió por fuera.

Hoy el estado aparece de otra forma: **cada cambio de dirección de un recurso** (de `a[0]` a `a["api"]`) es, para el estado, "un recurso nuevo y otro que sobra". El resto de la sesión es aprender a que no lo parezca.

---

<!-- _class: lead -->

# Parte 1
## Por qué módulos: el problema de copiar y pegar

---

## El entorno copiado

Caso hipotético. Un equipo tiene `dev/main.tf` y quiere un staging. Copia la carpeta, cambia dos nombres y listo.

```
infra/
├── dev/
│   └── main.tf        # 180 líneas
└── staging/
    └── main.tf        # 180 líneas, casi las mismas
```

Tres meses después:

- Alguien agrega un campo de etiquetas en `dev/` y olvida `staging/`.
- Staging usa dos réplicas "porque así lo dejó Ana", pero no está escrito por qué.
- Producción es una tercera copia que **ya no se parece a ninguna**.

El problema no es la copia: es que **nadie puede ver las diferencias**, porque están mezcladas con lo que es igual.

---

## Qué es un módulo

Un módulo es una carpeta con archivos `.tf` que otras configuraciones **llaman** con valores. La documentación de OpenTofu (opentofu.org/docs/language/modules/develop/) pide que un buen módulo "eleve el nivel de abstracción describiendo un concepto nuevo de tu arquitectura": no una envoltura delgada alrededor de un solo recurso.

```
modulos/servicios/
├── main.tf        # los recursos
├── variables.tf   # las entradas
└── outputs.tf     # las salidas
entornos/
├── dev/main.tf       # llama al módulo con sus valores
└── staging/main.tf   # llama al mismo módulo con otros valores
```

- Los archivos del módulo son **los mismos** para todos los entornos.
- Cada entorno es **corto**: solo lo que lo hace distinto.
- El cap. 4 de *Terraform: Up and Running* (Brikman, 2022) cubre los fundamentos (Module Basics, Module Inputs, Module Locals, Module Outputs) y las trampas del tema.

---

## Llamar a un módulo

```hcl
module "servicios" {
  source = "../../modulos/servicios"

  entorno           = "dev"
  servicios         = ["api", "web", "worker"]
  replicas          = 1
  directorio_salida = "${path.module}/salida"
}
```

- `module "servicios"` es un nombre **local**: cómo lo llamas tú.
- `source` es obligatorio. Una ruta que empieza con `./` o `../` es un módulo **local**: se lee directo del disco.
- Los demás argumentos son las **variables de entrada** del módulo.
- Tras agregar, quitar o cambiar un bloque `module`, hay que correr de nuevo `tofu init` (documentación de `module`).

Salida real del `init` con un módulo local:

```
Initializing modules...
- servicios in ../../modulos/servicios
```

---

## Cuándo **no** hacer un módulo

La documentación avisa contra dos excesos:

| Exceso | Señal |
|---|---|
| Una envoltura delgada de **un** recurso | El módulo se llama igual que el recurso que contiene |
| Un módulo cuyas entradas **repiten** los argumentos del recurso | No esconde nada: solo agrega una capa |

Y recomienda mantener el árbol de módulos **relativamente plano**: componer módulos pequeños en lugar de anidarlos muy profundo.

Regla práctica del curso: haz un módulo cuando ya tienes **dos usos reales** (dev y staging), no antes. El primer uso te enseña qué se repite.

---

<!-- _class: lead -->

# Parte 2
## Variables, validación, locals y outputs

---

## Las entradas: `variable`

```hcl
variable "entorno" {
  type        = string
  description = "Nombre del entorno: dev, staging o prod."
}

variable "servicios" {
  type        = list(string)
  description = "Nombres de los servicios que lleva el entorno."
}

variable "replicas" {
  type        = number
  default     = 1
  description = "Réplicas por servicio."
}
```

- `type` evita que `"dos"` pase por un número.
- `default` vuelve opcional la variable; sin él, es **obligatoria**.
- `description` es documentación que `tofu` muestra cuando falta un valor. Escríbela para quien no conoce el módulo.

---

## Validar antes de crear

```hcl {1-9|11-19}
variable "entorno" {
  type = string

  validation {
    condition     = contains(["dev", "staging", "prod"], var.entorno)
    error_message = "El entorno debe ser dev, staging o prod."
  }
}

variable "replicas" {
  type    = number
  default = 1

  validation {
    condition     = var.replicas >= 1 && var.replicas <= 5
    error_message = "Las réplicas van de 1 a 5."
  }
}
```

La documentación de `validation` pide dos cosas: `condition` (verdadera si el valor es válido) y `error_message`, y recomienda escribir el mensaje como oraciones completas. Puedes poner varias validaciones; se reportan todas las que fallen.

---

## Verlo fallar: el valor inválido

```bash
tofu plan -var 'entorno=produccion'
```

Salida real (OpenTofu 1.13.9, recortada):

```
Planning failed. OpenTofu encountered an error while generating this plan.

Error: Invalid value for variable

  on main.tf line 8, in module "servicios":
   8:   entorno           = var.entorno
    ├────────────────
    │ var.entorno is "produccion"

El entorno debe ser dev, staging o prod.

This was checked by the validation rule at
../../modulos/servicios/variables.tf:5,3-13.
```

- El error cita **tu** mensaje y **dónde** está la regla.
- Falla en el `plan`, antes de tocar nada. Esa es la gracia: el error más barato es el que no llega a la infraestructura.
- El mismo error con `replicas=9` dice `Las réplicas van de 1 a 5.`

---

## Locals: nombres para lo que se repite

```hcl
locals {
  etiquetas = {
    entorno = var.entorno
    gestion = "opentofu"
  }
}

resource "local_file" "servicio" {
  # ...
  content = yamlencode({
    servicio  = each.key
    entorno   = var.entorno
    etiquetas = local.etiquetas
  })
}
```

- Un `local` es un valor **interno** del módulo: no se pasa desde fuera ni sale.
- Úsalos para calcular una vez algo que usas muchas veces (etiquetas, prefijos de nombre).
- No los uses para esconder variables: si el que llama debe poder cambiarlo, es una `variable`.

---

## Las salidas: `output`

```hcl
output "archivos" {
  description = "Rutas de los archivos de servicio, por nombre."
  value       = { for nombre, f in local_file.servicio : nombre => f.filename }
}
```

Y quien llama al módulo la lee como `module.servicios.archivos`:

```hcl
output "archivos" {
  value = module.servicios.archivos
}
```

- Un módulo **no** deja ver sus recursos internos: solo sus salidas. Esa es su frontera.
- Diseña las salidas como diseñarías una API: lo mínimo que alguien más necesita.
- Una salida sensible se marca con `sensitive = true` (sesión 3).

---

## Dos entornos, un módulo

```hcl
# entornos/dev/terraform.tfvars
entorno       = "dev"
servicios     = ["api", "web", "worker"]
replicas      = 1
con_monitoreo = false
```

```hcl
# entornos/staging/terraform.tfvars
entorno       = "staging"
servicios     = ["api", "web", "worker"]
replicas      = 2
con_monitoreo = true
```

- `terraform.tfvars` se carga solo en esa carpeta. Aquí están **todas** las diferencias entre entornos, en 4 líneas que cualquiera revisa en un pull request.
- Con `diff entornos/dev/terraform.tfvars entornos/staging/terraform.tfvars` ves exactamente en qué se parecen y en qué no. Eso es "diferencias declaradas".
- Lo que **no** debe haber en un `tfvars`: secretos. Se versiona. (Sesión 3.)

---

## Descanso de 10 minutos

Al regresar: por qué una lista puede destruir tu infraestructura.

---

<!-- _class: lead -->

# Parte 3
## Ciclos y condicionales, y por qué `count` te puede destruir cosas

---

## `count`: n copias por posición

```hcl
resource "local_file" "servicio" {
  count = length(var.servicios)

  filename = "${var.directorio_salida}/${var.servicios[count.index]}.yaml"
  content  = yamlencode({ servicio = var.servicios[count.index] })
}
```

- `count = 3` crea tres instancias: `servicio[0]`, `servicio[1]`, `servicio[2]`.
- `count.index` es el número de la instancia, empezando en 0.
- En el estado, cada instancia se identifica **por su posición**.

Funciona. Hasta que la lista cambia.

---

## Verlo fallar: quitar un elemento del medio

La lista era `["api", "web", "worker"]`. Quitas `"api"`:

```bash
tofu plan
```

Salida real:

```
  # module.servicios.local_file.servicio[0] must be replaced
  # module.servicios.local_file.servicio[1] must be replaced
  # module.servicios.local_file.servicio[2] will be destroyed
  # (because index [2] is out of range for count)

Plan: 2 to add, 0 to change, 3 to destroy.
```

Querías **un** cambio (adiós, `api`) y el plan propone **tres** destrucciones. Lo que ocurrió:

| Posición | Antes | Ahora | Resultado |
|---|---|---|---|
| `[0]` | api | web | filename cambia: reemplazo |
| `[1]` | web | worker | filename cambia: reemplazo |
| `[2]` | worker | (no existe) | se destruye |

Con archivos es un susto. Con una base de datos o un volumen con datos, es un incidente.

La documentación de `count` lo dice así: derivar `count` de la longitud de una lista y buscar valores con `count.index` es frágil, porque las instancias se identifican por posición.

---

## `for_each`: n copias por nombre

```hcl
resource "local_file" "servicio" {
  for_each = toset(var.servicios)

  filename = "${var.directorio_salida}/${each.key}.yaml"
  content  = yamlencode({ servicio = each.key })
}
```

- Cada instancia se identifica **por su clave**: `servicio["api"]`, `servicio["web"]`.
- `for_each` acepta un **mapa** o un **conjunto de cadenas**; una lista se convierte con `toset()`.
- Quitar `"api"` de la lista afecta **solo** a `servicio["api"]`:

```
  # module.servicios.local_file.servicio["api"] will be destroyed
  # (because key ["api"] is not in for_each map)

Plan: 0 to add, 0 to change, 1 to destroy.
```

Fuente: documentación de `for_each` y `count` de OpenTofu, a noviembre de 2026.

---

## Reglas de `for_each` que muerden

Según la documentación:

| Regla | Consecuencia |
|---|---|
| Acepta un mapa o un conjunto de cadenas | Una lista directa falla: usa `toset()` |
| El valor debe ser **conocido antes** de cualquier acción remota | No puedes usar un ID que la plataforma genera al aplicar |
| No admite funciones impuras (`uuid`, `timestamp`...) | Las claves no pueden cambiar en cada plan |
| No admite valores sensibles | Las claves aparecen en pantalla |
| No se combina con `count` en el mismo bloque | Elige uno |

Si necesitas claves a partir de algo sensible, la propia documentación sugiere construir un valor no sensible con una expresión `for`.

El cap. 5 de Brikman, "Terraform Gotchas", abre con "count and for_each Have Limitations" y trata también el refactor como trampa.

---

## Expresiones `for`

Transforman una colección en otra. Ejemplos reales, ejecutados con `tofu console`:

```hcl
{ for k, v in { api = 1, web = 2 } : upper(k) => v * 10 }
# { "API" = 10, "WEB" = 20 }

[for s in ["api", "web", "worker"] : s if s != "web"]
# ["api", "worker"]

toset(["b", "a", "b"])
# toset(["a", "b"])      (un conjunto no repite elementos)
```

- Con llaves `{ }` el resultado es un **mapa**; con corchetes `[ ]`, una **lista**.
- El `if` filtra.
- Úsalas para dar forma a las salidas de un módulo o construir el mapa que alimenta a `for_each`.

---

## Condicionales

```hcl
resource "local_file" "monitoreo" {
  count = var.con_monitoreo ? 1 : 0

  filename = "${var.directorio_salida}/monitoreo.yaml"
  content  = yamlencode({ entorno = var.entorno, vigilar = var.servicios })
}
```

- La expresión `condición ? si_verdadero : si_falso` es el único condicional del lenguaje.
- Para crear un recurso **opcional**, el patrón es `count = condición ? 1 : 0`. Aquí `count` es la herramienta correcta: no hay lista que se reordene, solo 0 o 1.
- Referirse a un recurso condicional requiere cuidado: `local_file.monitoreo[0]` solo existe si el `count` es 1.

Ejemplo real: dev (`con_monitoreo = false`) crea 3 archivos; staging (`true`) crea 4, uno de ellos `monitoreo.yaml`.

---

## Bloques `dynamic`

Cuando un recurso lleva un bloque **repetible** (por ejemplo, varios `ports` en un contenedor) y la cantidad viene de una variable:

```hcl
# Fragmento: solo se planea, no crea ningún contenedor.
resource "docker_container" "web" {
  name  = "a07-dinamico"
  image = "nginx:1.29-alpine"

  dynamic "ports" {
    for_each = var.puertos
    content {
      internal = 80
      external = ports.value
      ip       = "127.0.0.1"
    }
  }
}
```

Con `puertos = { http = 8080, admin = 9090 }`, el plan mostró **dos** bloques `ports` (`external = 8080` y `external = 9090`).

La documentación advierte: el abuso de `dynamic` hace la configuración difícil de leer. Úsalo para esconder detalles detrás de una interfaz limpia en un módulo reutilizable, y escribe los bloques literales cuando puedas.

---

## `count` o `for_each`: cómo decidir

| Pregunta | Respuesta |
|---|---|
| ¿Es un recurso opcional (0 o 1)? | `count` con condición |
| ¿Cada instancia tiene un **nombre** propio? | `for_each` |
| ¿Puede la lista perder un elemento del medio? | `for_each` |
| ¿Las instancias son **idénticas** e intercambiables? | `count` está bien |
| ¿El orden de la lista **no** significa nada? | `for_each` con `toset()` |

Si dudas, `for_each`. Empezar con `count` y cambiar después es posible (lo veremos con `moved`), pero cuesta más que empezar bien.

---

<!-- _class: lead -->

# Parte 4
## Entornos: carpetas o workspaces

---

## Dos formas de separar entornos

El cap. 3 de Brikman titula así su sección de aislamiento del estado: "State File Isolation", con dos caminos: "Isolation via Workspaces" y "Isolation via File Layout".

| | Carpetas (una por entorno) | Workspaces |
|---|---|---|
| Qué es | Cada entorno es una carpeta con su `main.tf`, su `tfvars` y su estado | Un mismo código con varios estados con nombre |
| Qué se ve | Qué entornos existen, **en el árbol de archivos** | Hay que ejecutar `tofu workspace list` |
| Diferencias | Declaradas en `tfvars` | Dentro del código, con `terraform.workspace` |
| Permisos separados | Posible: backend y credenciales distintas | **No** a menos que se haga algo más |

---

## Lo que dice la documentación de workspaces

De opentofu.org/docs/language/state/workspaces/:

- Cada configuración tiene **un** backend. Un backend nuevo empieza con un workspace, `default`, que no se puede borrar. Algunos backends admiten varios workspaces con nombre (la lista incluye Local, Postgres, S3, GCS, AzureRM y otros).
- `${terraform.workspace}` inserta el nombre del workspace en cualquier lugar donde se permita interpolación.
- La advertencia: los workspaces **no son apropiados** para descomponer un sistema ni para despliegues que necesiten **credenciales y controles de acceso separados**.

Ejemplo real, con el backend local:

```bash
tofu workspace new staging
# Created and switched to workspace "staging"!
# You're now on a new, empty workspace. Workspaces isolate their state,
# so if you run "tofu plan" OpenTofu will not see any existing state
# for this configuration.
```

El estado de `staging` quedó en `terraform.tfstate.d/staging/terraform.tfstate`, y el del workspace `default` siguió en `terraform.tfstate`.

---

## El peligro silencioso de los workspaces

```bash
tofu workspace show
# staging
tofu apply     # ¿en cuál estoy aplicando?
```

- El **mismo código** y la **misma terminal** sirven para dev y para producción. Lo único que los distingue es el nombre del workspace activo, que no sale en el prompt.
- Un `apply` en el workspace equivocado no avisa nada.
- Con carpetas, **estar en `entornos/prod/`** es parte de la ruta del comando y del historial de Git.

Regla del curso: **carpetas por entorno** para dev, staging y producción. Los workspaces, para variantes del mismo entorno que **no** necesitan permisos distintos (por ejemplo, una rama de prueba efímera de un desarrollador).

---

## Estructura de carpetas con módulos

```
infra/
├── modulos/
│   └── servicios/          # el código compartido
└── entornos/
    ├── dev/
    │   ├── main.tf         # llama al módulo
    │   └── terraform.tfvars
    └── staging/
        ├── main.tf
        └── terraform.tfvars
```

- Cada carpeta de `entornos/` se inicializa y aplica **por separado**: tiene su propio estado.
- En la práctica de hoy el backend es el local (un `terraform.tfstate` por carpeta). En un equipo, cada entorno apuntaría a su **propio** estado remoto (sesión 1).
- El cap. 10 de Brikman ("How to Use Terraform as a Team") trata la separación de un repositorio "live" y uno de módulos; lo retomamos en la sesión 3.

---

## Error típico: el `tfvars` equivocado

```bash
cd entornos/dev
tofu apply -var-file=../staging/terraform.tfvars   # dev con valores de staging
```

- Mezclar valores de otro entorno es fácil si las variables se pasan "a mano".
- Con la estructura de carpetas, **`terraform.tfvars` del directorio actual** se carga solo y no hay nada que recordar.
- Si necesitas variantes, usa archivos con nombre explícito (`-var-file=carga-alta.tfvars`) y que su nombre diga qué son.

---

<!-- _class: lead -->

# Parte 5
## Versionar módulos, fijar proveedores y refactorizar con `moved`

---

## Versionar el módulo

Un módulo compartido cambia. Si todos lo llaman "como esté hoy", un cambio rompe a todos a la vez.

La documentación de OpenTofu (opentofu.org/docs/language/modules/sources/) dice qué admite cada tipo de fuente:

| Fuente | Cómo se fija la versión |
|---|---|
| Ruta local (`./`, `../`) | No admite `version`: va con el código que la llama |
| Registro | `version = "~> 1.2"` o una versión exacta |
| Git (`git::`) | `?ref=` con rama, etiqueta o commit |
| HTTP, S3, GCS, OCI | sus propios selectores (`archive`, `tag`, `digest`...) |

```hcl
# Fragmento: direcciones de ejemplo, no resuelven a nada real.
module "servicios" {
  source = "git::https://example.com/org/modulos.git//servicios?ref=v1.2.0"
}

module "otro" {
  source  = "example.com/org/servicios/local"
  version = "~> 1.2"
}
```

La doble diagonal (`//`) elige un subdirectorio del repositorio, antes de los argumentos. Una **etiqueta** o un commit identifican una versión concreta; una rama sigue avanzando y cada `init` podría traer algo distinto.

---

## Los proveedores también se fijan

Dos mecanismos, que se complementan:

```hcl
terraform {
  required_providers {
    local = {
      source  = "hashicorp/local"
      version = "~> 2.5"    # el rango que tu código acepta
    }
  }
}
```

- `.terraform.lock.hcl` guarda la versión **exacta** elegida (aquí 2.9.1) y las huellas. Se versiona, para que los cambios de dependencia se revisen como cualquier otro cambio.
- Con `init` normal, OpenTofu **reselecciona** la versión del archivo aunque exista una más nueva. Con `tofu init -upgrade` ignora lo registrado y elige la más nueva que cumpla las restricciones.

```bash
tofu providers lock -platform=linux_amd64 -platform=darwin_arm64 -platform=windows_amd64
```

registra las huellas de **varias plataformas** de antemano: si tu equipo usa Linux y Mac, el lock file sirve a ambos. Salida real: `Success! OpenTofu has validated the lock file and found no need for changes.`

---

## Refactorizar sin destruir: `moved`

El problema: cambiar el **nombre** o la **dirección** de un recurso en el código es, para el estado, "destruir el viejo y crear uno nuevo".

```hcl
moved {
  from = local_file.servicio[0]
  to   = local_file.servicio["api"]
}
```

La documentación (opentofu.org/docs/language/modules/develop/refactoring/):

- Un bloque `moved` solo lleva `from` y `to`. Le dice a OpenTofu que el objeto de la dirección vieja **ahora pertenece** a la nueva.
- Sirve para renombrar, para mover entre módulos y para pasar de `count` a `for_each`, usando las claves de instancia.
- Se pueden encadenar si un objeto se movió dos veces.
- En un módulo compartido, la recomendación es **conservar** los `moved` históricos para que quien usa una versión vieja pueda actualizar sin destruir.

---

## De `count` a `for_each`, con el plan que lo prueba

```hcl
moved {
  from = local_file.servicio[0]
  to   = local_file.servicio["api"]
}
moved {
  from = local_file.servicio[1]
  to   = local_file.servicio["web"]
}
moved {
  from = local_file.servicio[2]
  to   = local_file.servicio["worker"]
}
```

Salida real del plan:

```
  # module.servicios.local_file.servicio[0] has moved to module.servicios.local_file.servicio["api"]
    resource "local_file" "servicio" {
        id                   = "e09a989fe452fd70a0529a41de25a97a92183ec8"
        # (10 unchanged attributes hidden)
    }
  ...
Plan: 0 to add, 0 to change, 0 to destroy.
```

**Cero** cambios en la infraestructura: el estado solo cambió de dirección. Después de aplicar, quitar `"api"` de la lista da `Plan: 0 to add, 0 to change, 1 to destroy.`

<!-- Los ids son de una corrida de laboratorio. En la práctica, los de ustedes serán otros. -->

---

## Alternativa: `tofu state mv`

```bash
tofu state mv 'module.servicios.local_file.servicio[0]' 'module.servicios.local_file.servicio["api"]'
```

| | `moved` (en el código) | `state mv` (comando) |
|---|---|---|
| Queda registrado | Sí, en Git | No |
| Lo ven todos | Sí: se aplica al actualizar | Solo quien lo corre, en su estado |
| Revisable en un PR | Sí | No |

Para equipos, `moved`: el refactor es un cambio **revisado**, no un comando suelto. `state mv` queda para arreglos puntuales.

---

## Error típico: "refactoring can be tricky"

El cap. 5 de Brikman cierra sus trampas con "Refactoring Can Be Tricky". La lista corta de lo que **sí** debes tratar con cuidado:

1. **Renombrar** un recurso: destruye y crea (si no hay `moved`).
2. **Cambiar un atributo inmutable** (por ejemplo el `filename` de `local_file`): reemplazo.
3. **Mover un recurso a un módulo**: otra dirección.
4. **Cambiar `count` por `for_each`**: otra dirección.

Hábito: tras cualquier refactor, el `plan` debe decir **`0 to add, 0 to change, 0 to destroy`** (o solo cambios de salida). Si dice otra cosa, **no se aplica**: se agrega el `moved` que falta.

---

## Descanso de 10 minutos

Al regresar: la práctica.

---

<!-- _class: lead -->

# Parte 6
## Práctica: un módulo para dev y staging

---

## Qué vas a hacer

La guía completa está en `practica.md`. En 60 minutos, por equipo:

1. Escribir el módulo `servicios` con validaciones, locals y salidas.
2. Llamarlo desde `entornos/dev` y `entornos/staging` con sus `tfvars`.
3. **Verlo fallar (1):** un valor inválido que el `plan` rechaza.
4. **Verlo fallar (2):** quitar un elemento de la lista con `count` y leer los destruidos que propone.
5. Arreglarlo con `for_each` y `moved`, y comprobar que el plan queda en cero.
6. Fijar proveedores para varias plataformas y ver la diferencia entre `dev` y `staging` en una sola línea.

Evidencia: el módulo, los dos `tfvars`, el plan con 3 destruidos y el plan con `0 to destroy` tras el `moved`.

<!-- Un equipo = una persona que escribe y otra que lee el plan en voz alta. Cambien de rol en cada paso. -->

---

<!-- _class: lead -->

# Cierre
## Errores comunes y qué sigue

---

## Resumen de errores de la sesión

| Error | Se ve así | Se evita así |
|---|---|---|
| Copiar y pegar un entorno | Dos carpetas que ya no se parecen | Un módulo y un `tfvars` por entorno |
| `count` sobre una lista que cambia | `3 to destroy` por quitar un elemento | `for_each` con `toset()` |
| Refactorizar sin `moved` | Destruir y crear lo que solo cambió de nombre | `moved` y plan en cero |
| Workspaces para producción | `apply` en el workspace equivocado | Carpetas por entorno |
| Módulo sin versión | Un cambio rompe a todos los entornos a la vez | `?ref=` con etiqueta o `version` |
| Lock file fuera de Git | Cada quien usa un proveedor distinto | Versionar `.terraform.lock.hcl` |
| Validar tarde | El error aparece en la plataforma | `validation` en la variable |

---

## Referencias

Todas consultadas el 11 de noviembre de 2026.

- OpenTofu, módulos: https://opentofu.org/docs/language/modules/syntax/ y https://opentofu.org/docs/language/modules/develop/
- OpenTofu, fuentes de módulos: https://opentofu.org/docs/language/modules/sources/
- OpenTofu, refactorizar con `moved`: https://opentofu.org/docs/language/modules/develop/refactoring/
- OpenTofu, `count`: https://opentofu.org/docs/language/meta-arguments/count/ y `for_each`: https://opentofu.org/docs/language/meta-arguments/for_each/
- OpenTofu, bloques `dynamic`: https://opentofu.org/docs/language/expressions/dynamic-blocks/
- OpenTofu, validación y condiciones: https://opentofu.org/docs/language/expressions/custom-conditions/
- OpenTofu, workspaces: https://opentofu.org/docs/language/state/workspaces/
- OpenTofu, archivo de bloqueo de dependencias: https://opentofu.org/docs/language/files/dependency-lock/
- Versiones de la clase: OpenTofu 1.13.9, `hashicorp/local` 2.9.1, `kreuzwerker/docker` 3.9.0 (a noviembre de 2026).

**Libros:** *Terraform: Up and Running* (Brikman, 2022), caps. 4, 5 y 8.

---

## Tarea

**Su módulo con versión fijada y un ejemplo de uso.**

- Conviertes tu configuración de la sesión 1 (o una propia) en un módulo, con variables validadas, locals y salidas.
- Lo publicas en un repositorio de Git con una **etiqueta** (`v1.0.0`) y lo llamas desde dos entornos usando esa etiqueta.
- Haces un cambio incompatible, lo publicas como `v2.0.0` y documentas cómo migrar sin destruir (`moved`).

El detalle y la rúbrica están en `tarea.md`.

---

## Siguiente sesión

### S03 - Secretos, pruebas y trabajo en equipo

- Por qué el archivo de estado puede contener secretos en texto plano, y qué sí protege a un secreto.
- Pruebas de infraestructura: `validate`, `fmt` y `tofu test`.
- El `plan` en cada pull request, por CI.
- La regla de revisión de cambios de infraestructura de tu equipo.

> El archivo de estado puede contener secretos en texto plano.

---

## Quiz: módulos y entornos

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin la VM abierta.
- Lo que falles, búscalo en la documentación de `count`, `for_each` y `moved` antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
