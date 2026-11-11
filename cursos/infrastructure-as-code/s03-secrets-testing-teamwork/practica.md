# Práctica - S03 - Secretos, pruebas y trabajo en equipo

**Infraestructura como código**
**Duración:** 65 minutos, por equipo, en su VM.

---

## Objetivo

Ver con los ojos dónde queda un secreto aunque la variable sea `sensitive`, cifrar el estado y el plan, comprobar la diferencia entre un valor normal y uno efímero, escribir pruebas con `tofu test` (y ver una fallar) y simular en local el trabajo que haría un pull request: formato, validación, pruebas, plan y aviso de destrucciones.

Todos los valores son de laboratorio. **Nada** de lo que escriben hoy debe ser una contraseña real.

---

## Antes de empezar

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
tofu version
# OpenTofu v1.13.9
jq --version
python3 --version
mkdir -p ~/iac/s03 && cd ~/iac/s03
```

Si falta `jq`: `sudo apt-get install -y jq`. En todos los comandos de hoy usen `-input=false`: si falta una variable, quieren un error, no una pregunta que deje la terminal esperando.

---

## Paso 1 - Un secreto `sensitive`

```bash
mkdir -p ~/iac/s03/secretos && cd ~/iac/s03/secretos
nano main.tf
```

```hcl
terraform {
  required_providers {
    local = {
      source  = "hashicorp/local"
      version = "~> 2.5"
    }
  }
}

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

```bash
tofu init
export TF_VAR_clave_db='Lab-NoUsar-2026'
tofu plan -out=p.plan -input=false
```

Salida esperada (recortada):

```
  # local_sensitive_file.conexion will be created
  + resource "local_sensitive_file" "conexion" {
      + content              = (sensitive value)
      ...
Changes to Outputs:
  + cadena = (sensitive value)
```

```bash
tofu apply p.plan
```

```
Apply complete! Resources: 1 added, 0 changed, 0 destroyed.

Outputs:

cadena = <sensitive>
```

La pantalla **no** muestra la contraseña. Hasta aquí, `sensitive` hizo su trabajo.

---

## Paso 2 - Verlo fallar: el secreto aparece completo

### En el estado

```bash
grep -n "Lab-NoUsar" terraform.tfstate
```

Salida esperada (los números de línea pueden variar):

```
8:      "value": "Host=db.example.test;Password=Lab-NoUsar-2026",
23:            "content": "DB_PASSWORD=Lab-NoUsar-2026\n",
```

### En la salida

```bash
tofu output
# cadena = <sensitive>
tofu output -raw cadena; echo
# Host=db.example.test;Password=Lab-NoUsar-2026
```

### En el plan guardado

```bash
python3 - <<'PY'
import zipfile
z = zipfile.ZipFile('p.plan')
print([(n, b'Lab-NoUsar' in z.read(n)) for n in z.namelist()][:2])
PY
```

```
[('tfplan', True), ('tfstate', True)]
```

### En el JSON del plan

```bash
tofu show -json p.plan | head -c 200; echo
```

Salida esperada: un JSON que empieza con `{"format_version":"1.2","terraform_version":"1.13.9","variables":{"clave_db":{"value":"Lab-NoUsar-2026"}`.

**Registren** en `secretos.md`: los cuatro lugares donde apareció el valor y la forma en que, en su proyecto real, quedaría expuesto si ese archivo se sube a un repositorio o a un artefacto de CI.

### Un `output` sensible que no se marcó

Quiten `sensitive = true` del `output "cadena"` y corran `tofu plan -input=false`:

```
Error: Output refers to sensitive values

  on main.tf line 44:
  44: output "cadena" {

To reduce the risk of accidentally exporting sensitive data that was intended
to be only internal, OpenTofu requires that any root module output containing
sensitive data be explicitly marked as sensitive, to confirm your intent.
```

Devuelvan `sensitive = true`.

---

## Paso 3 - Cifrar el estado y el plan

Agreguen el bloque `encryption` dentro de `terraform` y la variable de la frase. Primero con **migración** (`fallback` con método sin cifrar), porque el estado actual está en claro:

```hcl
terraform {
  encryption {
    key_provider "pbkdf2" "clave" {
      passphrase = var.passphrase
    }
    method "aes_gcm" "cifrado" {
      keys = key_provider.pbkdf2.clave
    }
    method "unencrypted" "migrar" {}

    state {
      method = method.aes_gcm.cifrado
      fallback {
        method = method.unencrypted.migrar
      }
    }
    plan {
      method = method.aes_gcm.cifrado
    }
  }

  required_providers {
    local = {
      source  = "hashicorp/local"
      version = "~> 2.5"
    }
  }
}

variable "passphrase" {
  type        = string
  sensitive   = true
  description = "Frase para cifrar el estado y los planes (mínimo 16 caracteres)."
}
```

(El resto del archivo no cambia.) Guarden una copia del estado en claro **fuera del repositorio**, solo para comparar, y apliquen:

```bash
cp terraform.tfstate ~/iac/s03/estado-en-claro.bak
export TF_VAR_passphrase='frase-de-laboratorio-no-usar'
tofu plan -input=false
```

El `plan` muestra, antes de todo:

```
Warning: Unencrypted method configured

Method unencrypted is present in configuration. This is a security risk and
should only be enabled during migrations.
```

```bash
tofu apply -auto-approve -input=false
head -c 250 terraform.tfstate; echo
grep -c "Lab-NoUsar" terraform.tfstate
```

Salida esperada:

```
{"serial":1,"lineage":"...","meta":{"key_provider.pbkdf2.clave":"eyJzYWx0Ijoi...
0
```

El estado ya no tiene el secreto. Compruébenlo también en el plan:

```bash
tofu plan -out=c.plan -input=false > /dev/null
python3 -c "import zipfile; zipfile.ZipFile('c.plan')"
```

Debe terminar con `zipfile.BadZipFile: File is not a zip file`: el plan cifrado **ya no abre** como un zip.

### Verlo fallar: frase equivocada

```bash
TF_VAR_passphrase='otra-frase-distinta-123' tofu plan -input=false
```

Salida esperada (recortada):

```
Error: Error acquiring the state lock

Error message: failed to write backup file: decryption failed for all
provided methods
attempted decryption failed for state: decryption failed: cipher: message
authentication failed
```

### Verlo fallar: frase corta

```bash
TF_VAR_passphrase='corta' tofu plan -input=false
```

```
Error: Unable to build encryption key data

key_provider.pbkdf2.clave failed with error: passphrase is too short (minimum
16 characters)
```

### Lo que el cifrado **no** hace

```bash
tofu output -raw cadena; echo
# Host=db.example.test;Password=Lab-NoUsar-2026
tofu show -json c.plan | head -c 160; echo
# {"format_version":"1.2",...,"variables":{"clave_db":{"value":"Lab-NoUsar-2026"},...
```

Con la frase correcta, el **comando** sigue mostrando el valor. El cifrado protege el archivo en reposo; no protege lo que imprime `tofu` a quien lo ejecuta.

### Cerrar la migración

Ahora que el estado ya está cifrado, quiten el método `unencrypted` y el `fallback`, y exijan el cifrado:

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

```bash
tofu plan -input=false
# No changes. Your infrastructure matches the configuration.
```

Y comprueben que `enforced` rechaza un estado en claro. En otra carpeta, con la copia que guardaron:

```bash
mkdir -p ~/iac/s03/enforzado && cd ~/iac/s03/enforzado
cp ~/iac/s03/secretos/main.tf ~/iac/s03/secretos/.terraform.lock.hcl .
cp ~/iac/s03/estado-en-claro.bak terraform.tfstate
tofu init -input=false
```

```
Error: Error refreshing state

encountered unencrypted payload without unencrypted method configured
```

Después borren la copia en claro: `rm ~/iac/s03/estado-en-claro.bak` y `rm -rf ~/iac/s03/enforzado`. Si no, siguen teniendo el secreto en un archivo.

> **Respaldo de la frase:** perder la frase es perder el estado. En su equipo real, la frase vive en un gestor de secretos, no en el historial de una shell ni en un archivo.

---

## Paso 4 - Un valor efímero

```bash
mkdir -p ~/iac/s03/efimero && cd ~/iac/s03/efimero
nano main.tf
```

```hcl
terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "~> 3.7"
    }
    local = {
      source  = "hashicorp/local"
      version = "~> 2.5"
    }
  }
}

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

```bash
tofu init
tofu apply -auto-approve -input=false
```

Salida esperada (recortada):

```
Plan: 2 to add, 0 to change, 0 to destroy.
ephemeral.random_password.efimera: Opening...
random_password.normal: Creating...
ephemeral.random_password.efimera: Open complete after 0s
terraform_data.usa_efimera: Provisioning with 'local-exec'...
terraform_data.usa_efimera (local-exec): (output suppressed due to sensitive value in config)
ephemeral.random_password.efimera: Closing...
ephemeral.random_password.efimera: Close complete after 0s

Apply complete! Resources: 2 added, 0 changed, 0 destroyed.
```

Observen:

- La contraseña efímera se **abre**, se usa y se **cierra** dentro del mismo `apply`.
- La salida del provisioner se suprimió porque el valor viene de algo efímero.

Ahora busquen la contraseña **normal** en el estado y comprueben que de la efímera no hay nada:

```bash
python3 - <<'PY'
import json
estado = json.load(open('terraform.tfstate'))
for r in estado['resources']:
    if r['type'] == 'random_password':
        clave = r['instances'][0]['attributes']['result']
        print('clave normal en el estado:', clave)
        print('apariciones en el archivo:', open('terraform.tfstate').read().count(clave))
PY
grep -n "ephemeral" terraform.tfstate
```

Salida esperada (la clave será otra):

```
clave normal en el estado: jTOQYONV2R9CL8e6o8zc
apariciones en el archivo: 1
```

y en `grep`, solo una línea con la **dirección** `"ephemeral.random_password.efimera"` en las dependencias de `usa_efimera`, no un valor.

**Pregunta para el equipo:** ¿qué recursos de su proyecto aceptarían un valor efímero hoy? (Pista: depende de cada proveedor. En esta práctica, ni `hashicorp/local` ni `hashicorp/random` tienen atributos de solo escritura en sus recursos administrados: lo comprobamos con `tofu providers schema -json`.)

```bash
tofu destroy -auto-approve -input=false
```

---

## Paso 5 - `tofu test` sobre el módulo de la sesión 2

```bash
mkdir -p ~/iac/s03/infra && cp -r ~/iac/s02/modulos ~/iac/s02/entornos ~/iac/s03/infra/
cd ~/iac/s03/infra
rm -rf entornos/*/.terraform* entornos/*/salida entornos/*/terraform.tfstate*
mkdir -p modulos/servicios/tests
nano modulos/servicios/tests/servicios.tftest.hcl
```

```hcl
# Prueba de plan: no crea nada.
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

  assert {
    condition     = contains(keys(local_file.servicio), "api")
    error_message = "Los archivos se identifican por nombre de servicio."
  }

  assert {
    condition     = length(local_file.monitoreo) == 0
    error_message = "Sin con_monitoreo no debe haber monitoreo.yaml."
  }
}

run "monitoreo_en_staging" {
  command = plan

  variables {
    entorno           = "staging"
    servicios         = ["api"]
    con_monitoreo     = true
    directorio_salida = "salida-prueba"
  }

  assert {
    condition     = length(local_file.monitoreo) == 1
    error_message = "Con con_monitoreo debe existir monitoreo.yaml."
  }
}

run "entorno_invalido_se_rechaza" {
  command = plan

  variables {
    entorno           = "produccion"
    servicios         = ["api"]
    directorio_salida = "salida-prueba"
  }

  expect_failures = [var.entorno]
}

run "replicas_fuera_de_rango_se_rechazan" {
  command = plan

  variables {
    entorno           = "dev"
    servicios         = ["api"]
    replicas          = 9
    directorio_salida = "salida-prueba"
  }

  expect_failures = [var.replicas]
}

# Prueba con apply: crea archivos de verdad y los destruye al terminar.
run "aplica_y_el_contenido_es_el_esperado" {
  command = apply

  variables {
    entorno           = "staging"
    servicios         = ["api"]
    replicas          = 2
    directorio_salida = "salida-prueba"
  }

  assert {
    condition     = yamldecode(local_file.servicio["api"].content).replicas == 2
    error_message = "El archivo debe llevar las réplicas pedidas."
  }
}
```

> Esta prueba supone la **versión final** del módulo de la sesión 2 (con `for_each`). Si el suyo todavía usa `count`, es el momento de pasarlo a `for_each` con `moved`, como en la práctica anterior.

```bash
cd modulos/servicios
tofu init -input=false
tofu test
```

Salida esperada:

```
tests/servicios.tftest.hcl... pass
  run "tres_servicios_por_nombre"... pass
  run "monitoreo_en_staging"... pass
  run "entorno_invalido_se_rechaza"... pass
  run "replicas_fuera_de_rango_se_rechazan"... pass
  run "aplica_y_el_contenido_es_el_esperado"... pass

Success! 5 passed, 0 failed.
```

```bash
echo "salida: $?"
# salida: 0
```

### Verlo fallar: la prueba que atrapa un cambio

"Simplifiquen" el módulo: en `main.tf`, cambien `count = var.con_monitoreo ? 1 : 0` por `count = 1`.

```bash
tofu test; echo "salida: $?"
```

Salida esperada (recortada):

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
salida: 1
```

El código de salida es **1**: es lo que detendría a un CI. Restauren el `count` original.

**Registren:** ¿qué prueba atrapó el cambio, con qué mensaje, y cuánto habría tardado en descubrirse en staging?

---

## Paso 6 - Simular el pull request en local

Un script que hace lo que haría el workflow de CI. Guárdenlo en `~/iac/s03/infra/ci-local.sh`:

```bash
#!/usr/bin/env bash
# Simula en local lo que haría el workflow del pull request.
set -euo pipefail
export TF_IN_AUTOMATION=1
entorno="${1:-dev}"

tofu fmt -check -recursive -diff .
(cd modulos/servicios && tofu init -input=false -no-color >/dev/null && tofu test -no-color)

cd "entornos/${entorno}"
tofu init -input=false -no-color >/dev/null
tofu validate -no-color
tofu plan -input=false -lock-timeout=120s -out=tfplan -no-color > /dev/null
tofu show -no-color tfplan > resumen-plan.txt
grep -E '^Plan:' resumen-plan.txt || echo 'Sin cambios de infraestructura'
n=$(tofu show -json tfplan | jq '[.resource_changes[] | select(.change.actions | index("delete"))] | length')
if [ "$n" -gt 0 ]; then
  echo "AVISO: el plan destruye o reemplaza $n recurso(s). Revísalo antes de aprobar."
fi
```

Con `servicios = ["api", "web", "worker"]` en `entornos/dev/terraform.tfvars`:

```bash
cd ~/iac/s03/infra
chmod +x ci-local.sh
./ci-local.sh dev
```

Salida esperada (recortada):

```
Success! 5 passed, 0 failed.
Success! The configuration is valid.
Plan: 3 to add, 0 to change, 0 to destroy.
```

Apliquen el plan **guardado** (lo que el pipeline aplicaría después del merge) y luego simulen un pull request que quita `"api"`:

```bash
(cd entornos/dev && tofu apply tfplan)
sed -i 's/servicios .*/servicios     = ["web", "worker"]/' entornos/dev/terraform.tfvars
./ci-local.sh dev
```

```
Success! 5 passed, 0 failed.
Success! The configuration is valid.
Plan: 0 to add, 0 to change, 1 to destroy.
AVISO: el plan destruye o reemplaza 1 recurso(s). Revísalo antes de aprobar.
```

Lean `entornos/dev/resumen-plan.txt`: ¿qué dice sobre `servicio["api"]`? Debe decir `will be destroyed` y `(because key ["api"] is not in for_each map)`.

### Verlo fallar: el formato

```bash
echo 'variable   "x" {   type=string }' >> modulos/servicios/variables.tf
./ci-local.sh dev; echo "salida: $?"
```

Salida esperada (recortada):

```
modulos/servicios/variables.tf
--- old/modulos/servicios/variables.tf
+++ new/modulos/servicios/variables.tf
...
-variable   "x" {   type=string }
+variable "x" { type = string }
salida: 3
```

`tofu fmt -check` sale con código 3 y el script se detiene en el primer paso. Quiten la línea que agregaron.

---

## Paso 7 - El workflow de CI (fragmento)

Este archivo **no se ejecuta** en el laboratorio: no hay un CI conectado. Lo que sí se puede hacer es comprobar que es YAML válido y que dice lo que ustedes creen.

Creen `~/iac/s03/plan.yml` uniendo los dos bloques de código de las láminas de la Parte 4 (la cabecera y los pasos forman **un solo** archivo; los pasos van con la sangría que traen, debajo de `steps:`) y valídenlo:

```bash
python3 - <<'PY'
import yaml
d = yaml.safe_load(open('plan.yml'))
print(list(d['jobs']['plan'].keys()))
print([s.get('name') or s['uses'].split('@')[0] for s in d['jobs']['plan']['steps']])
PY
```

Salida esperada:

```
['runs-on', 'defaults', 'env', 'steps']
['actions/checkout', 'opentofu/setup-opentofu', 'Formato', 'Init', 'Validate', 'Pruebas del módulo', 'Plan', 'Resumen para quien revisa', 'Avisar de destrucciones']
```

(Si no tienen PyYAML: `sudo apt-get install -y python3-yaml`. PyYAML interpreta la clave `on` como `True`; es una particularidad de YAML 1.1 de ese analizador, y GitHub la lee bien.)

Comprueben también que las dos acciones están **fijadas por SHA** y no por etiqueta:

```bash
grep -nE "uses:" plan.yml
```

```
uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
uses: opentofu/setup-opentofu@a1320f892987e89d278cc92dc5adc984fb93aca4 # v2.0.2
```

> Los SHA se tomaron de la API de GitHub el 11 de noviembre de 2026 (`gh api repos/<dueño>/<repo>/git/ref/tags/<etiqueta>`). Antes de usar un workflow de un tutorial, comprueben el SHA de la etiqueta que dicen que fijan.

---

## Paso 8 - Limpieza

```bash
cd ~/iac/s03/infra/entornos/dev && tofu destroy -auto-approve -input=false
cd ~/iac/s03/secretos && tofu destroy -auto-approve -input=false
```

Con el `destroy` del estado cifrado necesitan `TF_VAR_passphrase` y `TF_VAR_clave_db` exportadas. Después:

```bash
unset TF_VAR_clave_db TF_VAR_passphrase
history -c
```

---

## Evidencia a entregar

Una carpeta `entrega-s03/` con:

1. `secretos.md`: los cuatro lugares donde apareció el secreto (paso 2) y los dos comandos que lo demuestran.
2. `cifrado.md`: el `head -c 250` del estado cifrado, el error de la frase equivocada y la salida de `enforced`.
3. `efimero.md`: qué diferencia hay entre la contraseña normal y la efímera en el estado.
4. `servicios.tftest.hcl` y la salida de `tofu test` con 5 pruebas pasando y la salida con la prueba fallando.
5. `ci-local.sh` y la salida del script con el aviso de destrucción.
6. `plan.yml` y la salida del analizador de YAML.

**No** incluyan ningún archivo de estado, plan ni JSON de plan, aunque estén cifrados.

---

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| El comando se queda esperando sin imprimir | Falta una variable y OpenTofu la está pidiendo | Usar `-input=false` y exportar `TF_VAR_...` en esa terminal |
| `Error acquiring the state lock` con "decryption failed" | La frase de cifrado no es la que se usó | Exportar `TF_VAR_passphrase` correcta |
| `passphrase is too short` | Menos de 16 caracteres | Usar una frase más larga |
| `tofu test` no encuentra pruebas | El archivo no está en `tests/` ni termina en `.tftest.hcl` | Revisar el nombre y la carpeta |
| La prueba con `apply` deja una carpeta `salida-prueba` vacía | El proveedor crea el directorio y no lo borra | Es normal; bórrenla a mano |
| `jq: command not found` | No está instalado | `sudo apt-get install -y jq` |
| `fmt -check` falla en un archivo que no tocaron | Estaba mal formateado desde antes | `tofu fmt -recursive` y revisen el diff |
