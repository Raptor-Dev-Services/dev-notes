# 05 - Terraform y OpenTofu: módulos, entornos y estado

> Fuente: *Terraform: Up and Running* (Brikman, 2022): caps. 3 How to Manage Terraform State, 4 How to Create Reusable Infrastructure with Terraform Modules, 5 Terraform Tips and Tricks y 8 Production-Grade Terraform Code. Comandos y mensajes verificados con OpenTofu 1.13.9 (a noviembre de 2026); el detalle de cada afirmación está en Referencias.

## Problema que resuelve

[04-terraform.md](04-terraform.md) enseña el primer recurso. Esta nota resuelve lo que viene después: el mismo código tiene que servir para dev, staging y producción sin copiarse, y el archivo de estado, que es lo único que sabe qué recursos son "suyos", tiene que sobrevivir a un equipo.

Copiar la carpeta de dev a la de staging funciona un mes. Después alguien arregla un bug en una copia y no en la otra, y los entornos dejan de parecerse justo en lo que se quería probar. Un módulo es la forma de escribir una vez y declarar solo lo que cambia.

> **Terraform u OpenTofu.** En esta ruta se usa OpenTofu (`tofu`). El lenguaje (HCL), los bloques y los conceptos son los de los libros de Terraform; cambia el nombre del comando. Los comandos de la nota se escriben con `tofu`: con Terraform, sustituye por `terraform`.

---

## Módulo raíz y módulo hijo

Un **módulo** es una carpeta con archivos `.tf` (o `.tofu`). El que está en la carpeta donde corres `tofu plan` es el **módulo raíz**. Un **módulo hijo** es el que otro llama con un bloque `module`.

```text
infra/
├── main.tf              módulo raíz: llama al hijo
├── modules/
│   └── archivo/
│       └── main.tf      módulo hijo: variables, recursos y outputs
└── .terraform.lock.hcl  versiones de proveedores (se versiona)
```

Un módulo hijo recibe **variables** (entradas), crea **recursos** y devuelve **outputs** (salidas). Ejemplo ilustrativo que corre tal cual con proveedores locales, sin nube:

```hcl
# modules/archivo/main.tf
terraform {
  required_providers {
    local = {
      source  = "hashicorp/local"
      version = "~> 2.5"
    }
  }
}

variable "nombre" {
  type = string
  validation {
    condition     = can(regex("^[a-z0-9-]+$", var.nombre))
    error_message = "El nombre solo admite minusculas, numeros y guiones."
  }
}

variable "contenido" {
  type = string
}

resource "local_file" "este" {
  filename = "${path.root}/salida/${var.nombre}.txt"
  content  = var.contenido
}

output "ruta" {
  value = local_file.este.filename
}
```

```hcl
# main.tf
variable "entornos" {
  type    = set(string)
  default = ["dev", "staging"]
}

module "archivo" {
  source   = "./modules/archivo"
  for_each = var.entornos

  nombre    = each.key
  contenido = "entorno ${each.key}\n"
}

output "rutas" {
  value = { for k, m in module.archivo : k => m.ruta }
}
```

```bash
tofu init
tofu apply -auto-approve
```

Salida real (resumida):

```
Apply complete! Resources: 2 added, 0 changed, 0 destroyed.

Outputs:

rutas = {
  "dev" = "./salida/dev.txt"
  "staging" = "./salida/staging.txt"
}
```

### Buenas formas de un módulo

La documentación de OpenTofu da estas pautas de composición:

- Mantén el árbol de módulos **plano**: compón módulos en el raíz en lugar de anidarlos en profundidad.
- Un módulo debe representar **un concepto de arquitectura** (un servicio con su base y su red), no envolver un solo recurso. Si cuesta darle un nombre distinto al del recurso que contiene, usa el recurso directo.
- No abuses: demasiados módulos dificultan entender la configuración.
- Para publicarlo: `README.md`, licencia, submódulos en `modules/NOMBRE`, ejemplos en `examples/NOMBRE` y pruebas con `tofu test`.

### Versionar un módulo

Un módulo local (`./modules/...`) siempre es la versión de tu rama. Uno remoto se **fija** a una versión, para que un cambio ajeno no entre sin que lo decidas:

```hcl
# Fragmento: apunta a un repositorio que no existe en este curso.
module "red" {
  source = "git::https://example.com/equipo/modulo-red.git?ref=v1.4.0"
}
```

Con un módulo del Registry se usa el argumento `version` (por ejemplo `version = "~> 1.4"`); con `git::` la versión va en `?ref=` porque `version` solo aplica a módulos del Registry. Las versiones de **proveedores** se fijan en `required_providers` y quedan registradas con sus hashes en `.terraform.lock.hcl`, que `tofu init` crea y mantiene: se commitea.

---

## Variables, ciclos y condicionales

### Validar entradas

El bloque `validation` rechaza una entrada mala antes de tocar nada. Con `-var 'entornos=["Prod"]'` el plan se detiene así (salida real):

```
Error: Invalid value for variable

  on main.tf line 10, in module "archivo":
  10:   nombre    = each.key
    │ var.nombre is "Prod"

El nombre solo admite minusculas, numeros y guiones.

This was checked by the validation rule at modules/archivo/main.tf:12,3-13.
```

### `count` contra `for_each`

`count` identifica cada recurso por su **posición** en una lista; `for_each`, por su **clave**. Con una lista `["ana","beto","carla"]` y `count`, quitar `"ana"` desplaza todos los índices. Salida real del plan:

```
  # local_file.usuario[0] must be replaced
  # local_file.usuario[1] must be replaced
  # local_file.usuario[2] will be destroyed
  # (because index [2] is out of range for count)
Plan: 2 to add, 0 to change, 3 to destroy.
```

Se quería destruir uno y el plan destruye tres. Con `for_each = toset(var.nombres)` cada recurso es `usuario["beto"]`, y quitar otro no mueve a los demás. Regla práctica: `count` para "uno o ninguno" (`count = var.activo ? 1 : 0`) y `for_each` para colecciones con nombre.

### Diferencias entre entornos

Lo que cambia se declara en un archivo de variables por entorno, no en el código:

```bash
tofu plan -var-file=dev.tfvars
tofu plan -var-file=staging.tfvars
```

Dos formas de separar entornos, con su costo:

| Forma | Cómo | Ventaja | Problema |
|---|---|---|---|
| Carpetas por entorno | `envs/dev/`, `envs/staging/`, cada una con su estado y su llamada al mismo módulo | Cada entorno tiene su propio estado y su propio permiso; es explícito | Algo de repetición en las llamadas |
| Workspaces | `tofu workspace new staging` dentro de la misma carpeta | Un solo código | Comparten un solo backend y sus credenciales, y el entorno activo no se ve en el código: es fácil aplicar en el equivocado |

La documentación de OpenTofu advierte que los workspaces "no son adecuados" cuando los despliegues necesitan credenciales y controles de acceso separados, porque todos comparten un solo backend. Para entornos que importan, un backend y un estado por carpeta; los workspaces sirven para varias instancias desechables de una misma configuración.

---

## El estado

El estado (`terraform.tfstate`) es el mapa entre cada bloque `resource` del código y el objeto real: sin él, `tofu plan` no sabe qué existe. Guarda **todos los atributos** de cada recurso, incluidos los sensibles (ver [10-cloud/05-secretos-produccion.md](../10-cloud/05-secretos-produccion.md)).

```bash
tofu state list                          # los recursos que conoce
tofu state show 'local_file.usuario[0]'  # sus atributos
```

### Bloqueo

OpenTofu bloquea el estado **automáticamente** en toda operación que pueda escribirlo, si el backend lo soporta. Con dos `apply` a la vez, el segundo falla (salida real con `-lock-timeout=3s`, estado local):

```
Acquiring state lock. This may take a few moments...
Error: Error acquiring the state lock

Error message: resource temporarily unavailable
Lock Info:
  ID:        be713480-1ac7-6faa-28f4-ed69340cdc76
  Path:      terraform.tfstate
  Operation: OperationTypeApply
  Who:       usuario@maquina
  Version:   1.13.9
```

`-lock-timeout` hace que espere ese tiempo antes de rendirse. `-lock=false` apaga el bloqueo y la documentación no lo recomienda. `tofu force-unlock <ID>` libera un bloqueo que quedó huérfano (un proceso que murió): úsalo solo después de confirmar que **nadie** está aplicando, porque soltarlo con otro `apply` en curso permite escrituras simultáneas.

Un archivo local solo protege a quienes comparten esa carpeta. Para un equipo, el estado va en un **backend remoto** con bloqueo; Dev Notes muestra S3 en [04-terraform.md](04-terraform.md), y en la práctica del curso se usa el backend `pg` (Postgres) para no depender de una nube.

### Refactorizar sin destruir: `moved`

Cambiar el nombre de un módulo o recurso en el código sin avisarlo hace que el plan lo trate como "borrar uno y crear otro" (salida real al renombrar `module.archivo` a `module.documento`):

```
  # module.archivo["dev"].local_file.este will be destroyed
  # module.archivo["staging"].local_file.este will be destroyed
  # module.documento["dev"].local_file.este will be created
  # module.documento["staging"].local_file.este will be created
Plan: 2 to add, 0 to change, 3 to destroy.
```

Un bloque `moved` le dice que es el mismo objeto:

```hcl
moved {
  from = module.archivo
  to   = module.documento
}
```

```
  # module.archivo["dev"].local_file.este has moved to module.documento["dev"].local_file.este
  # module.archivo["staging"].local_file.este has moved to module.documento["staging"].local_file.este
```

Ya no hay destrucción de esos recursos. Tras renombrar un módulo hay que volver a correr `tofu init` antes del plan; si no, el plan falla con `Module not installed`.

### Importar lo que ya existe

Para llevar a código un recurso creado a mano se usa el bloque `import` y se pide a OpenTofu que genere la configuración con `tofu plan -generate-config-out=generado.tf`. La práctica de la sesión 1 lo hace paso a paso.

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Un módulo por concepto de arquitectura, con versión fijada | Un módulo que envuelve un solo recurso sin agregar nada |
| `for_each` con claves estables para colecciones | `count` sobre una lista cuyo orden puede cambiar |
| `validation` en las variables que se prestan a error | Descubrir el error en el `apply` |
| `moved` al renombrar, y plan limpio antes de aplicar | Renombrar y confiar en que "se arregla solo" |
| Un estado por entorno, con backend remoto y bloqueo | El mismo estado y las mismas credenciales para dev y producción |
| `force-unlock` solo con la certeza de que nadie aplica | `-lock=false` para "salir del paso" |

---

## Referencias

Consultadas a noviembre de 2026; OpenTofu 1.13.9 (lanzada el 2026-11-05, según su página de releases en GitHub).

- OpenTofu, "State Locking": https://opentofu.org/docs/language/state/locking/
- OpenTofu, "Module Development": https://opentofu.org/docs/language/modules/develop/
- OpenTofu, "Refactoring" (bloque `moved`): https://opentofu.org/docs/language/modules/develop/refactoring/
- OpenTofu, "Workspaces": https://opentofu.org/docs/language/state/workspaces/
- OpenTofu, "Module Sources" (`version` y `?ref=`): https://opentofu.org/docs/language/modules/sources/
- OpenTofu, "Generating configuration" (import): https://opentofu.org/docs/language/import/generating-configuration/
- OpenTofu, "Dependency Lock File": https://opentofu.org/docs/language/files/dependency-lock/
- Brikman, Y. *Terraform: Up and Running*, 3.ª ed. (O'Reilly, 2022), caps. 3, 4, 5 y 8.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Módulo | Carpeta de archivos `.tf` que se puede llamar con un bloque `module` |
| Módulo raíz | El de la carpeta donde se ejecuta `tofu plan` o `tofu apply` |
| Módulo hijo | Módulo llamado por otro |
| Estado | Archivo que relaciona cada recurso del código con el objeto real y guarda sus atributos |
| Backend | Lugar donde se guarda el estado y que decide si hay bloqueo |
| Bloqueo del estado | Mecanismo que impide que dos operaciones escriban el estado a la vez |
| `force-unlock` | Orden que libera un bloqueo huérfano |
| `count` | Repite un recurso identificándolo por su posición |
| `for_each` | Repite un recurso identificándolo por una clave |
| `moved` | Bloque que declara que un objeto cambió de dirección sin destruirlo |
| Workspace | Estado alterno dentro de la misma configuración |
| `.terraform.lock.hcl` | Archivo con las versiones y los hashes de los proveedores elegidos |

---

*Rogelio Arriaga Gonzalez*
