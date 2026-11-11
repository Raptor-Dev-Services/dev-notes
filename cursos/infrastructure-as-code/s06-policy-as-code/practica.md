# Práctica - S06 - Políticas como código

**Infraestructura como código**
**Duración:** 55 minutos, por equipo, en su VM.

---

## Objetivo

Escribir políticas en Rego, probarlas y usarlas con **Conftest** para **rechazar un plan de OpenTofu inseguro antes de aplicarlo**. Ver fallar, a propósito:

1. El plan de un contenedor que publica el puerto 22 en todas las interfaces: Conftest lo rechaza (código de salida 1).
2. Una política rota: sus pruebas caen.
3. Un hueco de una política (la regla de "todo el tráfico") que deja pasar un recurso inseguro, y su arreglo.

Versiones (a noviembre de 2026): OpenTofu 1.13.9, Conftest 0.71.1 (OPA 1.21.1), proveedor `kreuzwerker/docker` 3.9.0. Las salidas son de corridas reales con esas versiones. Los ejemplos de AWS son **Ejemplo ilustrativo**: se prueban con un plan JSON reducido a mano; no se crea nada en una nube.

---

## Antes de empezar

- OpenTofu instalado (sesión 1) y Docker (no hace falta que levante nada: **solo se hace `plan`, nunca `apply`**).
- Conftest con su suma de verificación:

```bash
cd /tmp
curl -fsSLO https://github.com/open-policy-agent/conftest/releases/download/v0.71.1/conftest_0.71.1_Linux_x86_64.tar.gz
echo "c3f6b2a753bd56e377a1e51a95ae3e057f926f2e9139f7d5b5dfbbaf810f7985  conftest_0.71.1_Linux_x86_64.tar.gz" | sha256sum -c -
sudo tar -xzf conftest_0.71.1_Linux_x86_64.tar.gz -C /usr/local/bin conftest
conftest --version
```

Salida esperada:

```
conftest_0.71.1_Linux_x86_64.tar.gz: OK
Conftest: 0.71.1
OPA: 1.21.1
```

- `jq` (`sudo apt-get install -y jq`).

```bash
mkdir -p ~/iac-lab/politicas/policy ~/iac-lab/politicas/fixtures && cd ~/iac-lab/politicas
```

---

## Paso 1 - Un plan con un recurso inseguro

`main.tf`:

```hcl
terraform {
  required_version = ">= 1.10"
  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 3.0"
    }
  }
}

provider "docker" {}

resource "docker_container" "servidor" {
  name  = "lab-servidor"
  image = "ubuntu:24.04"

  ports {
    internal = 8080
    external = 8080
    ip       = "127.0.0.1"
  }
}
```

```bash
tofu init -input=false
tofu plan -input=false -out=plan.tfplan
tofu show -json plan.tfplan > plan.json
```

`tofu init` instala el proveedor (`Installed kreuzwerker/docker v3.9.0`). El `plan` **no crea nada**. Mira lo que el JSON dice del recurso:

```bash
jq '.format_version, (.resource_changes[] | {address, type, actions: .change.actions}), (.resource_changes[0].change.after.ports)' plan.json
```

Salida real:

```
"1.2"
{
  "address": "docker_container.servidor",
  "type": "docker_container",
  "actions": [
    "create"
  ]
}
[
  {
    "external": 2222,
    "internal": 22,
    "ip": "0.0.0.0",
    "protocol": "tcp"
  }
]
```

Las llaves de primer nivel de este plan:

```bash
jq -r 'keys | join(",")' plan.json
# configuration,errored,format_version,planned_values,resource_changes,terraform_version,timestamp
```

> **Cuidado:** `plan.json` puede traer valores sensibles en texto plano (la documentación de `tofu show` lo advierte). No lo subas a un repositorio ni lo imprimas en un log de CI. Agrégalo al `.gitignore`: `echo "plan.json plan.tfplan" | tr ' ' '\n' >> .gitignore`.

---

## Paso 2 - La primera política

`policy/docker.rego`:

```rego
package main

# Cada recurso que el plan va a crear o cambiar (no los que se borran).
cambios contains rc if {
	some rc in input.resource_changes
	some accion in rc.change.actions
	accion in {"create", "update"}
}

# Un contenedor que publica un puerto en todas las interfaces.
deny_puerto_publico contains msg if {
	some rc in cambios
	rc.type == "docker_container"
	some p in rc.change.after.ports
	p.ip in {"0.0.0.0", ""}
	msg := sprintf("%s publica el puerto %d en todas las interfaces (ip=%q); usa 127.0.0.1", [rc.address, p.external, p.ip])
}

# SSH nunca se publica desde un contenedor.
deny_ssh_en_contenedor contains msg if {
	some rc in cambios
	rc.type == "docker_container"
	some p in rc.change.after.ports
	p.internal == 22
	msg := sprintf("%s expone el puerto 22 del contenedor", [rc.address])
}

# Contenedores privilegiados.
deny_privilegiado contains msg if {
	some rc in cambios
	rc.type == "docker_container"
	rc.change.after.privileged == true
	msg := sprintf("%s corre con privileged = true", [rc.address])
}
```

```bash
conftest test --policy policy plan.json
echo "salida: $?"
```

Salida real:

```
FAIL - plan.json - main - docker_container.servidor publica el puerto 2222 en todas las interfaces (ip="0.0.0.0"); usa 127.0.0.1
FAIL - plan.json - main - docker_container.servidor expone el puerto 22 del contenedor
3 tests, 1 passed, 0 warnings, 2 failures, 0 exceptions
salida: 1
```

**Lo que debes ver:** dos rechazos, cada uno con **recurso, problema y arreglo**, y la salida `1`. En un CI, esta salida pone rojo el pull request. El contenedor **nunca existió**.

> Conftest también acepta `-o json`, que es útil para otros programas. Y dentro del resumen, "3 tests" son las tres reglas `deny` evaluadas contra el documento.

---

## Paso 3 - Corregir el recurso y ver pasar el plan

En `main.tf`, cambia el puerto:

```hcl
  ports {
    internal = 8080
    external = 8080
    ip       = "127.0.0.1"
  }
```

```bash
tofu plan -input=false -out=plan.tfplan > /dev/null
tofu show -json plan.tfplan > plan.json
conftest test --policy policy plan.json; echo "salida: $?"
```

```
3 tests, 3 passed, 0 warnings, 0 failures, 0 exceptions
salida: 0
```

Mismas reglas, mismo pipeline, resultado distinto porque cambió **el recurso**.

---

## Paso 4 - Pruebas de la política y verla caer

`policy/docker_test.rego`:

```rego
package main

plan_con_puerto(ip, interno) := {"resource_changes": [{
	"address": "docker_container.x",
	"type": "docker_container",
	"change": {"actions": ["create"], "after": {"ports": [{"ip": ip, "internal": interno, "external": 8080}], "privileged": false}},
}]}

test_rechaza_puerto_en_todas_las_interfaces if {
	count(deny_puerto_publico) == 1 with input as plan_con_puerto("0.0.0.0", 8080)
}

test_acepta_puerto_en_loopback if {
	count(deny_puerto_publico) == 0 with input as plan_con_puerto("127.0.0.1", 8080)
}

test_rechaza_ssh_en_contenedor if {
	count(deny_ssh_en_contenedor) == 1 with input as plan_con_puerto("127.0.0.1", 22)
}

test_ignora_lo_que_se_borra if {
	plan := {"resource_changes": [{
		"address": "docker_container.x",
		"type": "docker_container",
		"change": {"actions": ["delete"], "after": null},
	}]}
	count(deny_puerto_publico) == 0 with input as plan
}
```

```bash
conftest verify --policy policy
```

```
4 tests, 4 passed, 0 warnings, 0 failures, 0 exceptions, 0 skipped
```

Si tienes `opa` instalado, `opa test policy -v` da el detalle de cada prueba. **Verlo fallar:** rompe la regla a propósito.

```bash
cp policy/docker.rego /tmp/docker.rego.bak
sed -i 's/p.ip in {"0.0.0.0", ""}/p.ip == "1.1.1.1"/' policy/docker.rego
conftest verify --policy policy; echo "salida: $?"
```

```
FAIL - policy/docker_test.rego -  - data.main.test_rechaza_puerto_en_todas_las_interfaces
4 tests, 3 passed, 0 warnings, 1 failure, 0 exceptions, 0 skipped
salida: 1
```

La prueba de rechazo cayó: la regla ya no ve el puerto abierto. Restaura y confirma:

```bash
cp /tmp/docker.rego.bak policy/docker.rego
conftest verify --policy policy
# 4 tests, 4 passed, 0 warnings, 0 failures, 0 exceptions, 0 skipped
```

---

## Paso 5 - Una advertencia que no bloquea

Agrega al final de `policy/docker.rego`:

```rego
# Advertencia (no bloquea): imagen sin etiqueta fija.
warn_imagen_sin_version contains msg if {
	some rc in cambios
	rc.type == "docker_container"
	endswith(rc.change.after.image, ":latest")
	msg := sprintf("%s usa una imagen con :latest", [rc.address])
}
```

```bash
jq '.resource_changes[0].change.after.image = "ubuntu:latest"' plan.json > fixtures/latest.json
conftest test --policy policy fixtures/latest.json; echo "salida: $?"
conftest test --policy policy --fail-on-warn fixtures/latest.json > /dev/null; echo "con --fail-on-warn: $?"
```

```
WARN - fixtures/latest.json - main - docker_container.servidor usa una imagen con :latest
4 tests, 3 passed, 1 warning, 0 failures, 0 exceptions
salida: 0
con --fail-on-warn: 1
```

Una advertencia sale con 0; con `--fail-on-warn` sale con 1. Así se **introduce** una regla nueva sin romper a nadie: primero `warn`, después `deny`.

---

## Paso 6 - Reglas de AWS sobre un plan reducido a mano (Ejemplo ilustrativo)

No hay nube real. Escribe a mano un plan **reducido** con la forma que documenta OpenTofu (`resource_changes`, `change.actions`, `change.after`), con campos del proveedor AWS (`cidr_ipv4`, `from_port`, `to_port`, `ip_protocol` del recurso `aws_vpc_security_group_ingress_rule`; los cuatro ajustes de `aws_s3_bucket_public_access_block`; `acl` de `aws_s3_bucket_acl`).

`fixtures/aws-inseguro.json`:

```json
{
  "format_version": "1.2",
  "resource_changes": [
    {
      "address": "aws_vpc_security_group_ingress_rule.ssh",
      "type": "aws_vpc_security_group_ingress_rule",
      "change": {
        "actions": ["create"],
        "after": {"cidr_ipv4": "0.0.0.0/0", "from_port": 22, "to_port": 22, "ip_protocol": "tcp"}
      }
    },
    {
      "address": "aws_s3_bucket_public_access_block.datos",
      "type": "aws_s3_bucket_public_access_block",
      "change": {
        "actions": ["create"],
        "after": {"block_public_acls": false, "block_public_policy": true, "ignore_public_acls": true, "restrict_public_buckets": true}
      }
    },
    {
      "address": "aws_s3_bucket_acl.datos",
      "type": "aws_s3_bucket_acl",
      "change": {"actions": ["create"], "after": {"acl": "public-read"}}
    }
  ]
}
```

Su versión segura, derivada con `sed`:

```bash
sed 's#"0.0.0.0/0"#"203.0.113.0/24"#; s#"block_public_acls": false#"block_public_acls": true#; s#public-read#private#' fixtures/aws-inseguro.json > fixtures/aws-seguro.json
```

`policy/aws.rego` (primera versión):

```rego
package main

# Ejemplo ilustrativo: se prueba con un plan reducido a mano, sin nube real.

# SSH abierto al mundo.
deny_ssh_al_mundo contains msg if {
	some rc in cambios
	rc.type == "aws_vpc_security_group_ingress_rule"
	rc.change.after.cidr_ipv4 == "0.0.0.0/0"
	rc.change.after.from_port <= 22
	rc.change.after.to_port >= 22
	rc.change.after.ip_protocol in {"tcp", "-1"}
	msg := sprintf("%s abre el puerto 22 a 0.0.0.0/0", [rc.address])
}

# Un bloqueo de acceso publico con alguna proteccion apagada.
deny_bucket_publico contains msg if {
	some rc in cambios
	rc.type == "aws_s3_bucket_public_access_block"
	some campo in ["block_public_acls", "block_public_policy", "ignore_public_acls", "restrict_public_buckets"]
	rc.change.after[campo] == false
	msg := sprintf("%s deja %s en false", [rc.address, campo])
}

# ACL publica de un bucket.
deny_bucket_acl_publica contains msg if {
	some rc in cambios
	rc.type == "aws_s3_bucket_acl"
	rc.change.after.acl in {"public-read", "public-read-write"}
	msg := sprintf("%s usa la ACL %s", [rc.address, rc.change.after.acl])
}
```

```bash
conftest test --policy policy fixtures/aws-inseguro.json; echo "salida: $?"
conftest test --policy policy fixtures/aws-seguro.json; echo "salida: $?"
```

```
FAIL - fixtures/aws-inseguro.json - main - aws_vpc_security_group_ingress_rule.ssh abre el puerto 22 a 0.0.0.0/0
FAIL - fixtures/aws-inseguro.json - main - aws_s3_bucket_public_access_block.datos deja block_public_acls en false
FAIL - fixtures/aws-inseguro.json - main - aws_s3_bucket_acl.datos usa la ACL public-read
7 tests, 4 passed, 0 warnings, 3 failures, 0 exceptions
salida: 1
7 tests, 7 passed, 0 warnings, 0 failures, 0 exceptions
salida: 0
```

(Son 7 reglas ahora: 4 de contenedor y 3 de AWS.)

---

## Paso 7 - Verlo fallar: el hueco de la política

Una regla de seguridad que abre **todo el tráfico** (`ip_protocol = "-1"`) no tiene puertos: `from_port` y `to_port` son `null`. `fixtures/aws-todo-el-trafico.json`:

```json
{
  "format_version": "1.2",
  "resource_changes": [
    {
      "address": "aws_vpc_security_group_ingress_rule.todo",
      "type": "aws_vpc_security_group_ingress_rule",
      "change": {
        "actions": ["create"],
        "after": {"cidr_ipv4": "0.0.0.0/0", "from_port": null, "to_port": null, "ip_protocol": "-1"}
      }
    }
  ]
}
```

```bash
conftest test --policy policy fixtures/aws-todo-el-trafico.json; echo "salida: $?"
```

```
7 tests, 7 passed, 0 warnings, 0 failures, 0 exceptions
salida: 0
```

**Lo que debes ver:** la política **pasa**. Y ese recurso abre el puerto 22 (y todo lo demás) a todo internet. Una política que pasa no demuestra que el recurso sea seguro: demuestra que **esa regla** no lo vio.

El arreglo: contemplar el caso en la regla. Sustituye la regla `deny_ssh_al_mundo` por:

```rego
# SSH abierto al mundo.
deny_ssh_al_mundo contains msg if {
	some rc in cambios
	rc.type == "aws_vpc_security_group_ingress_rule"
	rc.change.after.cidr_ipv4 == "0.0.0.0/0"
	abre_ssh(rc.change.after)
	msg := sprintf("%s abre el puerto 22 a 0.0.0.0/0", [rc.address])
}

# Todo el trafico (-1) no trae puertos: abre SSH aunque from_port y to_port sean null.
abre_ssh(regla) if regla.ip_protocol == "-1"

abre_ssh(regla) if {
	regla.ip_protocol == "tcp"
	regla.from_port <= 22
	regla.to_port >= 22
}
```

```bash
conftest test --policy policy fixtures/aws-todo-el-trafico.json; echo "salida: $?"
```

```
FAIL - fixtures/aws-todo-el-trafico.json - main - aws_vpc_security_group_ingress_rule.todo abre el puerto 22 a 0.0.0.0/0
7 tests, 6 passed, 0 warnings, 1 failure, 0 exceptions
salida: 1
```

---

## Paso 8 - Un control de acceso: IAM sin administrador total

Agrega al final de `policy/aws.rego`:

```rego
# Permisos de administrador total en una politica IAM nueva (Action y Resource con "*").
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

# Action y Resource pueden venir como texto o como lista: se normalizan a lista.
acciones(s) := s.Action if is_array(s.Action)

acciones(s) := [s.Action] if not is_array(s.Action)

recursos(s) := s.Resource if is_array(s.Resource)

recursos(s) := [s.Resource] if not is_array(s.Resource)
```

`fixtures/iam-inseguro.json` (la política de IAM viaja en el plan como **texto JSON** dentro de `policy`):

```json
{
  "format_version": "1.2",
  "resource_changes": [
    {
      "address": "aws_iam_policy.ci",
      "type": "aws_iam_policy",
      "change": {
        "actions": ["create"],
        "after": {
          "name": "ci",
          "policy": "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Action\":\"*\",\"Resource\":\"*\"}]}"
        }
      }
    }
  ]
}
```

```bash
conftest test --policy policy fixtures/iam-inseguro.json; echo "salida: $?"
```

```
FAIL - fixtures/iam-inseguro.json - main - aws_iam_policy.ci concede Action * sobre Resource *
8 tests, 7 passed, 0 warnings, 1 failure, 0 exceptions
salida: 1
```

Si pones `Action` como lista (`["*"]`) también se rechaza: eso lo cubren las funciones `acciones` y `recursos`.

> **Verlo fallar (compilación):** una versión ingenua de la función (`acciones(s) := a if is_array(s.Action)`) no compila: `rego_unsafe_var_error: var a is unsafe`. Rego exige que toda variable quede ligada.

---

## Paso 9 - Pruebas de las reglas de AWS

`policy/aws_test.rego`:

```rego
package main

regla_ingress(proto, desde, hasta) := {"resource_changes": [{
	"address": "aws_vpc_security_group_ingress_rule.x",
	"type": "aws_vpc_security_group_ingress_rule",
	"change": {"actions": ["create"], "after": {"cidr_ipv4": "0.0.0.0/0", "ip_protocol": proto, "from_port": desde, "to_port": hasta}},
}]}

test_rechaza_ssh_tcp_22 if {
	count(deny_ssh_al_mundo) == 1 with input as regla_ingress("tcp", 22, 22)
}

test_rechaza_todo_el_trafico if {
	count(deny_ssh_al_mundo) == 1 with input as regla_ingress("-1", null, null)
}

test_acepta_https_al_mundo if {
	count(deny_ssh_al_mundo) == 0 with input as regla_ingress("tcp", 443, 443)
}

plan_iam(accion, recurso) := {"resource_changes": [{
	"address": "aws_iam_policy.x",
	"type": "aws_iam_policy",
	"change": {"actions": ["create"], "after": {"policy": json.marshal({"Statement": [{"Effect": "Allow", "Action": accion, "Resource": recurso}]})}},
}]}

test_rechaza_admin_total_en_texto if {
	count(deny_iam_admin_total) == 1 with input as plan_iam("*", "*")
}

test_rechaza_admin_total_en_lista if {
	count(deny_iam_admin_total) == 1 with input as plan_iam(["*"], ["*"])
}

test_acepta_permiso_acotado if {
	count(deny_iam_admin_total) == 0 with input as plan_iam(["s3:GetObject"], ["arn:aws:s3:::bucket-ejemplo/*"])
}
```

```bash
opa fmt -w policy 2>/dev/null || true     # opcional, si tienes opa
conftest verify --policy policy
```

```
10 tests, 10 passed, 0 warnings, 0 failures, 0 exceptions, 0 skipped
```

La prueba `test_rechaza_todo_el_trafico` es la que **cubre el hueco del paso 7**: si alguien vuelve a la primera versión de la regla, esa prueba cae.

---

## Paso 10 - Una excepción documentada

`policy/excepciones.rego`:

```rego
package main

# Excepcion revisada y documentada: el contenedor de la demo del curso puede publicar en todas las interfaces.
exception contains rules if {
	some rc in cambios
	rc.address == "docker_container.demo_publica"
	rules := ["puerto_publico"]
}
```

```bash
jq '.resource_changes[0].address = "docker_container.demo_publica" | .resource_changes[0].change.after.ports[0].ip = "0.0.0.0" | .resource_changes[0].change.after.ports[0].internal = 8080' plan.json > fixtures/demo-publica.json
conftest test --policy policy fixtures/demo-publica.json; echo "salida: $?"
```

```
EXCP - fixtures/demo-publica.json - main - data.main.exception[_][_] == "puerto_publico"
8 tests, 7 passed, 0 warnings, 0 failures, 0 exceptions
salida: 0
```

(El resumen real dice `1 exception`.) El recurso `demo_publica` queda exento **solo** de `deny_puerto_publico`. Prueba que el mismo plan con otro nombre de recurso **sí** se rechaza (cambia `demo_publica` por `otro` y vuelve a correr).

---

## Paso 11 - El CI que rechaza

Crea `.github/workflows/politicas.yml` con el workflow de la clase (**fragmento**: lo valida un parser, no un runner):

```yaml
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

```bash
~/.venvs/ansible/bin/python -c "import yaml; d=yaml.safe_load(open('.github/workflows/politicas.yml')); print([s.get('name') or s.get('uses') for s in d['jobs']['politicas']['steps']])"
```

Muestra los seis pasos. Para el CI, el proyecto debe tener `infra/` (con el `main.tf`) y `policy/` en la raíz del repositorio. Verifica en GitHub: un pull request con el puerto abierto debe fallar en el paso "Rechazar el plan inseguro" y uno corregido debe pasar. Si no usas GitHub, corre los mismos comandos en tu VM y anota los códigos de salida.

---

## Evidencia a entregar

Carpeta `evidencia-s06/`:

1. `rechazo.txt`: la salida y el código `1` del paso 2, y la del paso 3 con código `0`.
2. `politica-rota.txt`: la prueba que cae en el paso 4.
3. `hueco.txt`: el paso 7 completo (pasa, y después falla con el arreglo).
4. `excepcion.txt`: el paso 10, con el recurso exento y el mismo recurso con otro nombre rechazado.
5. `workflow.yml` y la salida de validarlo con un parser.
6. Las políticas y sus pruebas en el repositorio.

---

## Si algo falla

| Síntoma | Qué revisar |
|---|---|
| `rego_parse_error: if keyword is required` | Una regla con sintaxis v0 (`deny[msg] {`): escríbela con `contains` e `if` |
| `rego_unsafe_var_error` | Una variable que nunca recibe valor |
| `conftest test` dice `0 tests` | El paquete no es `main` o la carpeta de políticas está mal (`--policy`) |
| La política siempre pasa | Ruta mal escrita (indefinido): escribe una prueba que **debe** rechazar |
| `tofu init` no baja el proveedor | Sin red desde la VM, o el registro no responde: reintenta |
| `jq: command not found` | `sudo apt-get install -y jq` |
| `plan.json` quedó en el repositorio | `git rm --cached plan.json` y al `.gitignore`: puede traer valores sensibles |
