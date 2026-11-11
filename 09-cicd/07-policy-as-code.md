# 07 - Políticas como código: rechazar lo inseguro antes de que exista

> Fuente: *Policy as Code* (Ray, 2024): caps. 1 Policy as Code: A Gentle Introduction, 2 Open Policy Agent, 11 PaC and Infrastructure as Code y 12 PaC and Terraform IaC. *Security as Code* (Das y Chu, 2023): caps. 3 Securing Your Infrastructure y 5 Controlling Access Through Automation. *Cloud Native Security Cookbook* (Armitage, 2022): caps. 6 Infrastructure as Code y 7 Compliance as Code. Código verificado con OPA 1.22.3, Conftest 0.71.1 y OpenTofu 1.13.9 (a noviembre de 2026).

## Problema que resuelve

[01-checklists.md](01-checklists.md) lista lo que alguien debe revisar antes de cada merge: "el puerto 22 no está abierto al mundo", "el bucket no es público". Un checklist depende de que una persona se acuerde y de que lo lea con cuidado a las seis de la tarde de un viernes. Una **política como código** convierte cada línea del checklist en una regla que una máquina evalúa en cada cambio y que **detiene el pipeline** si no se cumple.

La diferencia con una alarma es el momento. Una alerta avisa cuando el bucket público ya existe; una política sobre el **plan** de OpenTofu lo rechaza antes de crearlo.

---

## Guardia contra reacción

| | Guardrail (preventivo) | Reactivo |
|---|---|---|
| Cuándo actúa | Antes de crear el recurso, sobre el plan | Después, sobre lo que ya existe |
| Ejemplo | El CI falla si el plan abre el 22 al mundo | Una alerta avisa que un grupo de seguridad quedó abierto |
| Costo del error | Un pull request rojo | Una ventana de exposición y un incidente |

Las dos hacen falta: la política previene lo previsto y el monitoreo detecta lo que se saltó el proceso (un cambio hecho a mano en la consola, por ejemplo). Esta nota cubre la primera.

---

## Open Policy Agent y Rego

**Open Policy Agent (OPA)** es un motor de políticas de propósito general, proyecto graduado de la CNCF desde el 29 de enero de 2021. Las políticas se escriben en **Rego**: reglas sobre un documento JSON de entrada (`input`) que producen un resultado. **Conftest** es una herramienta que corre políticas Rego sobre archivos de configuración (JSON, YAML, HCL...) y sale con código distinto de cero si alguna regla `deny` se cumple, que es lo que un CI necesita.

Reglas de Rego mínimas para esta nota (sintaxis de OPA 1.x, donde `if` y `contains` son obligatorios y no hace falta importar nada):

- `deny contains msg if { ... }` define un **conjunto** de mensajes. Cada vez que el cuerpo es verdadero, se agrega un mensaje; si el conjunto queda vacío, no hay violación.
- Dentro del cuerpo, cada línea es una condición y todas deben cumplirse (AND).
- `some rc in input.resource_changes` itera una lista.
- `"0.0.0.0/0" in rc.change.after.cidr_blocks` pregunta si un valor está en una lista.

---

## Qué evaluar: el plan en JSON

OpenTofu puede exportar el plan como JSON. La política evalúa lo que **va a existir** (`change.after`), no el código HCL:

```bash
tofu plan -out=plan.bin
tofu show -json plan.bin > plan.json
```

El JSON trae, entre otras cosas, `resource_changes`: un elemento por instancia de recurso, con su `address`, `type` y un objeto `change` con `actions` (`create`, `update`, `delete`...) y `after` (los valores resultantes, que pueden estar incompletos si dependen de algo que solo se sabe al aplicar). Un plan real de un recurso `terraform_data` da (salida real, recortada):

```json
{
  "address": "terraform_data.demo",
  "type": "terraform_data",
  "change": {
    "actions": ["create"],
    "after": { "input": "hola", "triggers_replace": null }
  }
}
```

La forma completa está en la documentación de OpenTofu, "JSON Output Format". Un consumidor debe ignorar propiedades que no conoce.

---

## Dos políticas: SSH al mundo y bucket público

**Ejemplo ilustrativo:** el siguiente plan JSON está **reducido a mano** (no salió de un `tofu plan` contra una nube real) para mostrar políticas sobre recursos de AWS sin crear una cuenta ni pagar nada. Sigue la forma de `resource_changes` de arriba.

```json
{
  "format_version": "1.2",
  "resource_changes": [
    {"address": "aws_security_group_rule.ssh", "type": "aws_security_group_rule",
     "change": {"actions": ["create"], "after": {"type": "ingress", "from_port": 22, "to_port": 22, "protocol": "tcp", "cidr_blocks": ["0.0.0.0/0"]}}},
    {"address": "aws_s3_bucket_acl.logs", "type": "aws_s3_bucket_acl",
     "change": {"actions": ["create"], "after": {"acl": "public-read"}}}
  ]
}
```

La política de red, con una función auxiliar en su propio archivo:

```rego
# policy/lib/ssh.rego
package lib.ssh

abre_ssh(regla) if {
	regla.from_port <= 22
	regla.to_port >= 22
}
```

```rego
# policy/red.rego
package main

import data.lib.ssh

deny contains msg if {
	some rc in input.resource_changes
	rc.type == "aws_security_group_rule"
	rc.change.after.type == "ingress"
	ssh.abre_ssh(rc.change.after)
	"0.0.0.0/0" in rc.change.after.cidr_blocks
	msg := sprintf("%s abre el puerto 22 al mundo", [rc.address])
}
```

```rego
# policy/bucket.rego
package main

deny contains msg if {
	some rc in input.resource_changes
	rc.type == "aws_s3_bucket_acl"
	rc.change.after.acl in {"public-read", "public-read-write"}
	msg := sprintf("%s hace publico el bucket (acl %s)", [rc.address, rc.change.after.acl])
}
```

```bash
conftest test plan-malo.json -p policy
echo "salida=$?"
```

Salida real:

```
FAIL - plan-malo.json - main - aws_s3_bucket_acl.logs hace publico el bucket (acl public-read)
FAIL - plan-malo.json - main - aws_security_group_rule.ssh abre el puerto 22 al mundo

2 tests, 0 passed, 0 warnings, 2 failures, 0 exceptions
salida=1
```

Con el mismo plan pero `cidr_blocks` acotado a `203.0.113.0/24` y `acl` en `private`: `2 tests, 2 passed, 0 warnings, 0 failures, 0 exceptions` y `salida=0`. Una política que nunca has visto fallar no demuestra nada: **siempre** prueba primero el caso que debe rechazar.

> Los nombres `aws_security_group_rule` y `aws_s3_bucket_acl` y sus atributos son los del proveedor de AWS. Los proveedores cambian: para un proyecto real, revisa los atributos en la documentación de la versión que fijaste, y cubre también los recursos que el proveedor recomiende hoy para lo mismo.

### Probar las políticas

Las políticas son código y se prueban como código. OPA corre archivos `*_test.rego` con `opa test`:

```rego
package main

test_ssh_al_mundo_se_rechaza if {
	r := deny with input as {"resource_changes": [{"address": "aws_security_group_rule.ssh", "type": "aws_security_group_rule", "change": {"after": {"type": "ingress", "from_port": 22, "to_port": 22, "cidr_blocks": ["0.0.0.0/0"]}}}]}
	count(r) == 1
}

test_ssh_acotado_se_acepta if {
	r := deny with input as {"resource_changes": [{"address": "aws_security_group_rule.ssh", "type": "aws_security_group_rule", "change": {"after": {"type": "ingress", "from_port": 22, "to_port": 22, "cidr_blocks": ["203.0.113.0/24"]}}}]}
	count(r) == 0
}
```

```bash
opa test policy -v
```

```
data.main.test_ssh_acotado_se_acepta: PASS (3.754493ms)
data.main.test_ssh_al_mundo_se_rechaza: PASS (5.152594ms)
PASS: 2/2
```

---

## En el pipeline

El paso es el mismo que corriste en tu terminal: generar el JSON del plan y pasárselo a Conftest. Si sale con 1, el pull request no se puede fusionar. La sesión 6 del curso arma el workflow completo. La política vive en el mismo repositorio que la infraestructura y cambia por pull request, con revisión: **quien puede cambiar la regla es quien decide qué se permite**.

### Excepciones

Habrá casos legítimos (un bastión que de verdad acepta SSH desde una oficina). La excepción se escribe **en la política**, acotada y con dueño, no se resuelve apagando el paso del CI:

```rego
# Fragmento: se agrega al archivo de la politica.
excepciones := {"aws_security_group_rule.bastion_oficina"}
```

y la regla agrega `not rc.address in excepciones`. Cada excepción queda en el historial de Git con quien la aprobó.

---

## Cumplimiento y accesos como código

La idea se extiende más allá del plan de OpenTofu, y los libros de la ruta tienen capítulos dedicados (se citan por su título; el contenido se lee en cada libro):

- **Cumplimiento como código:** *Cloud Native Security Cookbook*, cap. 7 (Compliance as Code). Los controles que hoy son una hoja de cálculo pasan a ser reglas que se evalúan solas.
- **Controles de acceso automatizados:** *Security as Code*, cap. 5 (Controlling Access Through Automation). Los permisos se declaran y se revisan como código en lugar de concederse a mano.
- **Políticas sobre cualquier cosa que sea código:** *Policy as Code*, caps. 1 y 11. El mismo enfoque aplica a manifiestos, Dockerfiles y pipelines, no solo a Terraform.

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Una política por cada línea del checklist que sea verificable por máquina | Dejar la seguridad solo en un checklist humano |
| Evaluar el **plan** (`tofu show -json`) | Evaluar solo el HCL: no ve valores calculados ni módulos |
| Mensajes que dicen qué recurso falló y por qué | `deny` con mensajes como "política violada" |
| Pruebas con `opa test` y un caso que debe fallar | Una política nunca vista fallar |
| Excepciones acotadas y con dueño en el repositorio | Apagar el paso del CI "por esta vez" |
| Complementar con monitoreo de lo que ya existe | Creer que el plan cubre los cambios hechos a mano en la consola |

---

## Referencias

Consultadas a noviembre de 2026.

- OPA, documentación y "Policy Language" (Rego): https://www.openpolicyagent.org/docs
- Conftest: https://www.conftest.dev/
- CNCF, Open Policy Agent (graduado el 2021-01-29): https://www.cncf.io/projects/open-policy-agent-opa/
- OpenTofu, "JSON Output Format": https://opentofu.org/docs/internals/json-format/
- Versiones: OPA 1.22.3 (2026-11-10) y Conftest 0.71.1 (2026-10-06), según las páginas de releases de sus repositorios en GitHub.
- Ray, J. *Policy as Code* (O'Reilly, 2024), caps. 1, 2, 11 y 12. Das, B. S. y Chu, V. *Security as Code* (O'Reilly, 2023), caps. 3 y 5. Armitage, J. *Cloud Native Security Cookbook* (O'Reilly, 2022), caps. 6 y 7.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Política como código | Regla de seguridad o cumplimiento escrita en un lenguaje que una máquina evalúa en cada cambio |
| Guardrail | Control preventivo que impide crear lo que viola la regla |
| OPA | Open Policy Agent, motor de políticas de propósito general |
| Rego | Lenguaje de políticas de OPA |
| Conftest | Herramienta que evalúa archivos de configuración contra políticas Rego |
| `deny` | Regla cuyo resultado no vacío significa que el cambio se rechaza |
| `input` | Documento JSON que la política evalúa |
| Plan en JSON | Salida de `tofu show -json` con lo que cambiaría una aplicación |
| `opa test` | Comando que corre las pruebas de las políticas |
| Excepción | Caso permitido, declarado en la política con alcance y dueño |

---

*Rogelio Arriaga Gonzalez*
