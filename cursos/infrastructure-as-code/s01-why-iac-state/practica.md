# Práctica - S01 - Por qué IaC, y el estado

**Infraestructura como código**
**Duración:** 60 minutos, por equipo, en su VM. Hacen falta dos terminales abiertas a la vez.

---

## Objetivo

Crear el primer recurso con OpenTofu guardando su estado en un PostgreSQL, **ver fallar** dos `apply` simultáneos por el bloqueo del estado, importar a código un volumen de Docker creado a mano y leer en un `plan` un drift provocado a propósito.

---

## Antes de empezar

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
tofu version
# OpenTofu v1.13.9
docker version --format '{{.Server.Version}}'
```

Si `tofu` no existe, instálenlo con el método de apt de la documentación oficial (https://opentofu.org/docs/intro/install/deb/), como se vio en la apertura.

Un PostgreSQL 17 de laboratorio, solo accesible desde la VM:

```bash
mkdir -p ~/iac/s01 && cd ~/iac/s01
docker run -d --name a07-pg -e POSTGRES_PASSWORD=lab-no-usar -e POSTGRES_DB=estado \
  -p 127.0.0.1:15432:5432 postgres:17
docker ps --filter name=a07-pg --format '{{.Names}} {{.Status}}'
```

Debe decir `a07-pg Up ...`. Las dos terminales necesitan la variable del backend:

```bash
export PG_CONN_STR='postgres://postgres:lab-no-usar@127.0.0.1:15432/estado?sslmode=disable'
```

> La contraseña es de laboratorio. En un equipo real no iría en un comando ni en el código: la guardarías en un gestor de secretos y la entregarías por entorno (sesión 3).

---

## Paso 1 - El primer recurso

```bash
cd ~/iac/s01
nano main.tf
```

```hcl
terraform {
  backend "pg" {}

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

# Un recurso lento a propósito: abre una ventana de 25 segundos
# para que dos aplicaciones se encuentren.
resource "terraform_data" "lento" {
  provisioner "local-exec" {
    command = "sleep 25"
  }
}
```

```bash
tofu fmt
tofu init
```

Salida esperada (recortada):

```
Initializing the backend...

Successfully configured the backend "pg"! OpenTofu will automatically
use this backend unless the backend configuration changes.
Initializing provider plugins...
- Finding hashicorp/local versions matching "~> 2.5"...
- Installing hashicorp/local v2.9.1...
...
OpenTofu has been successfully initialized!
```

Si el proveedor que se instala es una versión más nueva dentro de `~> 2.5`, es normal.

> `backend "pg" {}` va vacío a propósito: la cadena de conexión viene de `PG_CONN_STR`. Así la contraseña no queda en un archivo que se pueda subir a Git.

---

## Paso 2 - Aplicar

```bash
tofu plan
```

Debe terminar con:

```
Plan: 2 to add, 0 to change, 0 to destroy.
```

```bash
time tofu apply -auto-approve
```

Salida esperada (recortada):

```
terraform_data.lento: Creating...
terraform_data.lento: Provisioning with 'local-exec'...
terraform_data.lento (local-exec): Executing: ["/bin/sh" "-c" "sleep 25"]
local_file.nota: Creating...
local_file.nota: Creation complete after 0s [id=2b76913696ee7bb2f6808afb08cd874424898cc1]
terraform_data.lento: Still creating... [10s elapsed]
terraform_data.lento: Still creating... [20s elapsed]
terraform_data.lento: Creation complete after 25s [id=...]

Apply complete! Resources: 2 added, 0 changed, 0 destroyed.

Outputs:

ruta = "./salida/nota.txt"
```

```bash
cat salida/nota.txt
tofu state list
ls
```

Observen que **no hay** `terraform.tfstate` en la carpeta: el estado no está aquí. Lo que sí hay es `.terraform/` (los plugins) y `.terraform.lock.hcl`.

---

## Paso 3 - El estado, visto en la base

```bash
docker exec a07-pg psql -U postgres -d estado \
  -c 'select id, name, length(data) from terraform_remote_state.states;'
```

Salida esperada (el largo puede variar unos bytes):

```
 id |  name   | length
----+---------+--------
  1 | default |   1485
(1 row)
```

Es una fila: el nombre del workspace (`default`) y, en la columna `data`, el JSON del estado.

**Pregunta para el equipo:** ¿quién, en su organización, tendría que poder ejecutar ese `select`? ¿Qué podrían leer si el recurso fuera una base de datos con contraseña?

---

## Paso 4 - Verlo fallar: dos `apply` a la vez

Necesitan dos terminales con `PG_CONN_STR` exportada, ambas en `~/iac/s01`.

**Terminal 1:** fuerza el recurso lento, que tarda 25 segundos:

```bash
tofu apply -auto-approve -replace=terraform_data.lento
```

**Terminal 2:** a los 5 segundos, sin esperar a que la 1 termine:

```bash
tofu apply -auto-approve
```

Salida esperada en la terminal 2:

```
Error: Error acquiring the state lock

Error message: Workspace is already locked: default
Lock Info:
  ID:        5b2781e4-b50b-f81c-b516-9f871cc53b33
  Path:
  Operation: OperationTypeApply
  Who:       <USUARIO>@<NOMBRE_DE_SU_VM>
  Version:   1.13.9
  Created:   2026-11-11 13:18:30.311726664 +0000 UTC
  Info:


OpenTofu acquires a state lock to protect the state from being written
by multiple users at the same time. Please resolve the issue above and try
again. For most commands, you can disable locking with the "-lock=false"
flag, but this is not recommended.
```

El ID, el usuario y la hora serán los suyos. Lo que importa:

- La terminal 2 **no hizo nada**: no tocó el estado.
- `Who` y `Created` dicen quién bloquea y desde cuándo.
- La terminal 1 terminó normal.

**Registren:** el mensaje completo de su terminal 2.

### Esperar en lugar de fallar

Repitan el escenario, pero ahora la terminal 2 con paciencia:

```bash
time tofu apply -auto-approve -lock-timeout=5s
```

Salida esperada:

```
Acquiring state lock. This may take a few moments...

Error: Error acquiring the state lock
...
real	0m5.4s
```

Esperó 5 segundos y falló igual, porque la terminal 1 necesita 25. Prueben ahora con `-lock-timeout=60s`: la terminal 2 debe esperar a que la 1 termine y luego aplicar sola, sin error.

> **Pregunta:** ¿qué pasaría en un pipeline de CI con `-lock-timeout=60s` si el apply del pipeline anterior tarda 10 minutos?

### Lo que no se debe hacer

Con la terminal 1 aplicando otra vez (`-replace=terraform_data.lento`), corran en la terminal 2 un **plan** que se salta el bloqueo (solo `plan`, nunca `apply`):

```bash
tofu plan -lock=false
```

Salida esperada: el plan termina sin error, aunque otra terminal esté aplicando. **No es una buena noticia:** leyó un estado que está a punto de cambiar. El bloqueo no se salta para escribir.

### `force-unlock` en el backend pg

La documentación del backend `pg` dice que sus bloqueos son advisory locks de PostgreSQL (se liberan solos al terminar la sesión) y que por eso `force-unlock` no está soportado en ese backend. Comprueben en la documentación (https://opentofu.org/docs/language/settings/backends/pg/) la frase exacta y anótenla en `bloqueo.txt`. Si la terminal 1 se cierra a media operación (Ctrl-C), ¿queda el bloqueo? Pruébenlo: lancen el `apply` lento, interrúmpanlo con Ctrl-C y vuelvan a aplicar.

---

## Paso 5 - Drift: alguien cambió algo por fuera

Con todo aplicado y sin nada en marcha:

```bash
tofu plan
# No changes. Your infrastructure matches the configuration.

echo "editado a mano" > salida/nota.txt
tofu plan -detailed-exitcode; echo "código de salida: $?"
```

Salida esperada (recortada):

```
  # local_file.nota will be created
  + resource "local_file" "nota" {
      + content              = <<-EOT
            hola desde OpenTofu
        EOT
      ...
Plan: 1 to add, 0 to change, 0 to destroy.
...
código de salida: 2
```

El proveedor `local` considera que un archivo con otro contenido **ya no es** el que creó, y propone crearlo otra vez. Decidan: **revertir** el cambio.

```bash
tofu apply -auto-approve
cat salida/nota.txt
# hola desde OpenTofu
tofu plan -detailed-exitcode; echo "código de salida: $?"
# código de salida: 0
```

**Registren:** los dos códigos de salida (2 y 0) y qué significan según la documentación de `plan`.

---

## Paso 6 - Importar un volumen que existe solo a mano

Creen un volumen **sin** OpenTofu, como lo haría alguien con prisa:

```bash
mkdir -p ~/iac/s01/importar && cd ~/iac/s01/importar
docker volume create a07-datos
```

Escriban `main.tf` con un bloque `import` y el proveedor de Docker (versión fijada a la serie 3):

```hcl
terraform {
  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 3.0"
    }
  }
}

import {
  to = docker_volume.datos
  id = "a07-datos"
}
```

Este directorio no declara ningún backend, así que usa el estado local (`terraform.tfstate` en la carpeta); `PG_CONN_STR` no interviene aquí.

```bash
tofu init
tofu plan -generate-config-out=generado.tf
```

Salida esperada (recortada):

```
Plan: 1 to import, 0 to add, 0 to change, 0 to destroy.

Warning: Config generation is experimental
...
OpenTofu has generated configuration and written it to generado.tf. Please
review the configuration and edit it as necessary before adding it to version
control.
```

```bash
cat generado.tf
```

```hcl
# __generated__ by OpenTofu from "a07-datos"
resource "docker_volume" "datos" {
  driver      = "local"
  driver_opts = {}
  name        = "a07-datos"
}
```

Revisen el archivo (**es un punto de partida, no la verdad**), muévanlo a `main.tf` y apliquen:

```bash
cat generado.tf >> main.tf && rm generado.tf
tofu fmt
tofu apply -auto-approve
```

Salida esperada:

```
docker_volume.datos: Importing... [id=a07-datos]
docker_volume.datos: Import complete [id=a07-datos]

Apply complete! Resources: 1 imported, 0 added, 0 changed, 0 destroyed.
```

Ya pueden borrar el bloque `import` de `main.tf` y comprobar:

```bash
tofu plan
# docker_volume.datos: Refreshing state... [id=a07-datos]
# No changes. Your infrastructure matches the configuration.
```

### Verlo fallar: el volumen se borra a mano

```bash
tofu state list
docker volume rm a07-datos
tofu plan
```

Salida esperada:

```
docker_volume.datos: Refreshing state... [id=a07-datos]

OpenTofu used the selected providers to generate the following execution
plan. Resource actions are indicated with the following symbols:
  + create

  # docker_volume.datos will be created
  + resource "docker_volume" "datos" {
      + driver     = "local"
      + id         = (known after apply)
      + mountpoint = (known after apply)
      + name       = "a07-datos"
    }

Plan: 1 to add, 0 to change, 0 to destroy.
```

El estado todavía **recordaba** el volumen; la plataforma ya no lo tiene. Ahora el plan propone crearlo de nuevo. Respondan: ¿qué cambia si en lugar de un volumen vacío fuera uno con datos de producción? ¿Qué decisión tomarías: crear uno nuevo, restaurar un respaldo o aceptar que no existe (`tofu apply -refresh-only`)?

Prueben la tercera: `tofu apply -refresh-only -auto-approve`. Debe terminar con `Apply complete! Resources: 0 added, 0 changed, 0 destroyed.` y el estado ya no lista el volumen.

---

## Paso 7 - Limpieza

```bash
cd ~/iac/s01
tofu destroy -auto-approve
docker rm -f a07-pg
docker volume rm a07-datos 2>/dev/null || true
cd ~ && rm -rf ~/iac/s01/importar
```

Comprueben que no quedó nada: `docker ps -a --filter name=a07-` no debe listar contenedores.

---

## Evidencia a entregar

Una carpeta `entrega-s01/` con:

1. `main.tf` del paso 1 (sin contraseñas).
2. `bloqueo.txt`: el error completo de la terminal 2 del paso 4, y su explicación en tres líneas: qué protege, de qué y qué habría pasado sin él.
3. `drift.txt`: los dos códigos de salida del paso 5.
4. `importar/main.tf` y la salida del `tofu plan` final con `No changes`.
5. Una frase: ¿dónde guardaría **su** equipo el estado y por qué?

---

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `connection refused` al hacer `init` | el contenedor no está arriba o el puerto es otro | `docker ps`, revisen el `-p 127.0.0.1:15432:5432` |
| `PG_CONN_STR` vacío | la variable no se exportó en esa terminal | exportarla de nuevo |
| El `apply` de la terminal 2 no falla | la 1 ya terminó | repetir el escenario; el recurso lento dura 25 s |
| `Cannot import non-existent remote object` | el `id` del bloque `import` no es el que el proveedor espera | revisar el formato de `id` en la documentación del proveedor |
| `docker: permission denied` | el usuario no pertenece al grupo `docker` | pedir a quien administra la VM |
