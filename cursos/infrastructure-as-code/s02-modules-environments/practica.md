# Práctica - S02 - Módulos y entornos

**Infraestructura como código**
**Duración:** 60 minutos, por equipo, en su VM.

---

## Objetivo

Escribir un módulo reutilizable, llamarlo desde dos entornos (dev y staging) con diferencias declaradas en archivos `tfvars`, **ver fallar** un valor inválido y una lista con `count` que propone destruir tres recursos al quitar uno, y arreglarlo con `for_each` y `moved` hasta que el plan diga cero destruidos para el refactor.

No hay contenedores ni nube: los recursos son archivos YAML (`local_file`). El mecanismo es el mismo que con una VM.

---

## Antes de empezar

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
tofu version
# OpenTofu v1.13.9
mkdir -p ~/iac/s02/modulos/servicios ~/iac/s02/entornos/dev ~/iac/s02/entornos/staging
cd ~/iac/s02
```

---

## Paso 1 - El módulo (versión 1, con `count`)

`modulos/servicios/variables.tf`:

```hcl
variable "entorno" {
  type        = string
  description = "Nombre del entorno: dev, staging o prod."

  validation {
    condition     = contains(["dev", "staging", "prod"], var.entorno)
    error_message = "El entorno debe ser dev, staging o prod."
  }
}

variable "servicios" {
  type        = list(string)
  description = "Nombres de los servicios que lleva el entorno."
}

variable "replicas" {
  type        = number
  default     = 1
  description = "Réplicas por servicio."

  validation {
    condition     = var.replicas >= 1 && var.replicas <= 5
    error_message = "Las réplicas van de 1 a 5."
  }
}

variable "directorio_salida" {
  type        = string
  description = "Carpeta donde se escriben los archivos de configuración."
}

variable "con_monitoreo" {
  type    = bool
  default = false
}
```

`modulos/servicios/main.tf`:

```hcl
terraform {
  required_providers {
    local = {
      source  = "hashicorp/local"
      version = "~> 2.5"
    }
  }
}

locals {
  etiquetas = {
    entorno = var.entorno
    gestion = "opentofu"
  }
}

resource "local_file" "servicio" {
  count = length(var.servicios)

  filename        = "${var.directorio_salida}/${var.servicios[count.index]}.yaml"
  file_permission = "0644"
  content = yamlencode({
    servicio  = var.servicios[count.index]
    entorno   = var.entorno
    replicas  = var.replicas
    etiquetas = local.etiquetas
  })
}

resource "local_file" "monitoreo" {
  count = var.con_monitoreo ? 1 : 0

  filename        = "${var.directorio_salida}/monitoreo.yaml"
  file_permission = "0644"
  content         = yamlencode({ entorno = var.entorno, vigilar = var.servicios })
}
```

`modulos/servicios/outputs.tf`:

```hcl
output "archivos" {
  description = "Rutas de los archivos de servicio."
  value       = local_file.servicio[*].filename
}
```

---

## Paso 2 - Los dos entornos

`entornos/dev/main.tf` (el de `staging` es **idéntico**; cópienlo):

```hcl
terraform {
  required_version = ">= 1.6.0"
}

module "servicios" {
  source = "../../modulos/servicios"

  entorno           = var.entorno
  servicios         = var.servicios
  replicas          = var.replicas
  con_monitoreo     = var.con_monitoreo
  directorio_salida = "${path.module}/salida"
}

variable "entorno" { type = string }
variable "servicios" { type = list(string) }
variable "replicas" { type = number }
variable "con_monitoreo" { type = bool }

output "archivos" {
  value = module.servicios.archivos
}
```

`entornos/dev/terraform.tfvars`:

```hcl
entorno       = "dev"
servicios     = ["api", "web", "worker"]
replicas      = 1
con_monitoreo = false
```

`entornos/staging/terraform.tfvars`:

```hcl
entorno       = "staging"
servicios     = ["api", "web", "worker"]
replicas      = 2
con_monitoreo = true
```

Comprueben formato y que los dos `main.tf` son idénticos:

```bash
tofu fmt -check -recursive . ; echo "fmt: $?"
# fmt: 0
diff entornos/dev/main.tf entornos/staging/main.tf && echo "iguales"
# iguales
diff entornos/dev/terraform.tfvars entornos/staging/terraform.tfvars
```

La última línea muestra **todas** las diferencias entre los entornos:

```
1c1
< entorno       = "dev"
---
> entorno       = "staging"
3,4c3,4
< replicas      = 1
< con_monitoreo = false
---
> replicas      = 2
> con_monitoreo = true
```

---

## Paso 3 - Inicializar y aplicar

Primero un error clásico: validar **antes** de inicializar.

```bash
cd ~/iac/s02/entornos/dev
tofu validate
```

Salida esperada (el mensaje puede traer más texto):

```

Error: Module not installed
...
```

Un `module` nuevo exige `tofu init`:

```bash
tofu init
```

```
Initializing modules...
- servicios in ../../modulos/servicios
...
OpenTofu has been successfully initialized!
```

```bash
tofu validate
# Success! The configuration is valid.
tofu apply -auto-approve
```

Salida esperada (el orden puede variar):

```
Apply complete! Resources: 3 added, 0 changed, 0 destroyed.

Outputs:

archivos = [
  "./salida/api.yaml",
  "./salida/web.yaml",
  "./salida/worker.yaml",
]
```

```bash
cat salida/api.yaml
```

```
"entorno": "dev"
"etiquetas":
  "entorno": "dev"
  "gestion": "opentofu"
"replicas": 1
"servicio": "api"
```

Ahora staging (**otra carpeta, otro estado**):

```bash
cd ~/iac/s02/entornos/staging
tofu init > /dev/null
tofu apply -auto-approve
# Apply complete! Resources: 4 added, 0 changed, 0 destroyed.
ls salida
# api.yaml  monitoreo.yaml  web.yaml  worker.yaml
```

Staging creó **uno más** (`monitoreo.yaml`) por una sola línea de `tfvars`. Cada carpeta tiene su propio `terraform.tfstate`.

---

## Paso 4 - Verlo fallar (1): el valor inválido

En `entornos/dev`:

```bash
cd ~/iac/s02/entornos/dev
tofu plan -var 'entorno=produccion'
```

Salida esperada (recortada):

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

Prueben también `-var 'replicas=9'`: debe decir `Las réplicas van de 1 a 5.`

**Registren:** ¿en qué archivo y línea está la regla que rechazó el valor? ¿Se creó o se cambió algo? (No: falló en el `plan`.)

---

## Paso 5 - Verlo fallar (2): quitar un elemento con `count`

Con dev aplicado y la lista `["api", "web", "worker"]`, quiten `"api"` en `entornos/dev/terraform.tfvars`:

```hcl
servicios     = ["web", "worker"]
```

```bash
tofu plan
```

Salida esperada (recortada):

```
  # module.servicios.local_file.servicio[0] must be replaced
  # module.servicios.local_file.servicio[1] must be replaced
  # module.servicios.local_file.servicio[2] will be destroyed
  # (because index [2] is out of range for count)

Plan: 2 to add, 0 to change, 3 to destroy.
```

**No apliquen.** Piensen en voz alta:

- Querían quitar **un** servicio. ¿Cuántos destruidos propone el plan?
- Busquen la línea `# forces replacement` en el detalle del plan: ¿qué atributo la causa?
- Si cada elemento fuera un volumen con datos, ¿qué se perdería?

Devuelvan el `tfvars` a `["api", "web", "worker"]` y comprueben que el plan vuelve a `No changes`:

```bash
tofu plan | grep -E "No changes"
# No changes. Your infrastructure matches the configuration.
```

---

## Paso 6 - El arreglo: `for_each` y `moved`

Reemplacen el recurso `local_file.servicio` en `modulos/servicios/main.tf` por esta versión y agreguen los `moved`:

```hcl
resource "local_file" "servicio" {
  for_each = toset(var.servicios)

  filename        = "${var.directorio_salida}/${each.key}.yaml"
  file_permission = "0644"
  content = yamlencode({
    servicio  = each.key
    entorno   = var.entorno
    replicas  = var.replicas
    etiquetas = local.etiquetas
  })
}

# De count a for_each sin destruir: cada índice pasa a su clave.
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

Y la salida del módulo ahora es un mapa (`modulos/servicios/outputs.tf`):

```hcl
output "archivos" {
  description = "Rutas de los archivos de servicio, por nombre."
  value       = { for nombre, f in local_file.servicio : nombre => f.filename }
}
```

```bash
cd ~/iac/s02/entornos/dev
tofu fmt -recursive ../..
tofu plan
```

Salida esperada (recortada):

```
  # module.servicios.local_file.servicio[0] has moved to module.servicios.local_file.servicio["api"]
    resource "local_file" "servicio" {
        id                   = "<ID_DEL_ARCHIVO>"
        # (10 unchanged attributes hidden)
    }
  ...
Plan: 0 to add, 0 to change, 0 to destroy.

Changes to Outputs:
  ~ archivos = [
      - "./salida/api.yaml",
      ...
    ] -> {
      + api    = "./salida/api.yaml"
      ...
    }
```

Esto es lo que se busca: **cero** cambios de infraestructura. Solo la salida cambió de forma.

```bash
tofu apply -auto-approve
# Apply complete! Resources: 0 added, 0 changed, 0 destroyed.
```

Ahora sí, quiten `"api"` de `entornos/dev/terraform.tfvars` (`servicios = ["web", "worker"]`):

```bash
tofu plan
```

```
  # module.servicios.local_file.servicio["api"] will be destroyed
  # (because key ["api"] is not in for_each map)

Plan: 0 to add, 0 to change, 1 to destroy.
```

Uno solo, el que pidieron. Apliquen y comprueben:

```bash
tofu apply -auto-approve
# Apply complete! Resources: 0 added, 0 changed, 1 destroyed.
ls salida
# web.yaml  worker.yaml
```

> **El staging usa el mismo módulo:** entren a `entornos/staging` y corran `tofu plan`. Debe mostrar los tres `has moved` y `0 to destroy`. El refactor llegó a staging sin destruir nada, porque el `moved` viaja con el módulo.

---

## Paso 7 - Fijar proveedores para varias plataformas

```bash
cd ~/iac/s02/entornos/dev
tofu providers lock -platform=linux_amd64 -platform=darwin_arm64 -platform=windows_amd64
```

Salida esperada (recortada):

```
- Fetching hashicorp/local 2.9.1 for darwin_arm64...
- Retrieved hashicorp/local 2.9.1 for darwin_arm64 (signed, key ID 0C0AF313E5FD9F80)
...
Success! OpenTofu has validated the lock file and found no need for changes.
```

Si en su corrida hay una versión más nueva dentro de `~> 2.5`, el número cambia: es normal. Revisen el archivo:

```bash
head -8 .terraform.lock.hcl
```

Comprueben que dice `version` y `constraints = "~> 2.5"` y que tiene líneas `h1:`. Este archivo **sí** se versiona; `.terraform/` y `terraform.tfstate*` no.

---

## Paso 8 - Limpieza

```bash
cd ~/iac/s02/entornos/dev && tofu destroy -auto-approve
cd ~/iac/s02/entornos/staging && tofu destroy -auto-approve
```

---

## Evidencia a entregar

Una carpeta `entrega-s02/` con:

1. El módulo `modulos/servicios/` (versión final con `for_each` y `moved`).
2. Los dos `terraform.tfvars` y la salida de `diff` entre ellos.
3. `validacion.txt`: el error del paso 4.
4. `count.txt`: el plan del paso 5 con `Plan: 2 to add, 0 to change, 3 to destroy.` y tu explicación de por qué.
5. `moved.txt`: el plan del paso 6 con `0 to add, 0 to change, 0 to destroy` y el plan con 1 destruido.
6. `.terraform.lock.hcl` del entorno dev.

---

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `Module not installed` | falta `tofu init` tras agregar el `module` | correr `tofu init` |
| `for_each` se queja de valores que no se conocen todavía | el conjunto depende de algo que la plataforma genera al aplicar (la documentación de `for_each` lo prohíbe) | construir el conjunto solo con variables o locals conocidos |
| `has moved` no aparece | el `moved` apunta a una dirección que no existe en el estado | revisar índices y claves con `tofu state list` |
| El plan de `staging` no dice `has moved` | staging se aplicó ya con la versión nueva del módulo, no con la de `count` | normal: no hay nada que mover |
