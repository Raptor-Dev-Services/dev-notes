# Práctica - M03 - La red del Taller Demo: construirla mal, detectarla, corregirla

**Seguridad en la nube y Zero Trust**
**Duración estimada:** 2 horas

---

## Objetivo

Armar en tu máquina la red del **Caso hipotético: Taller Demo** con dos errores de red a propósito (SSH y PostgreSQL abiertos a `0.0.0.0/0`), **detectarlos** con un auditor propio, **corregirlos** y **verificar** que el auditor ya no encuentra nada. Después revisar plantillas con checkov y ver cómo una NACL sin regla de retorno se come las respuestas.

Regla del curso: si no lo viste fallar, no lo probaste. Aquí lo que falla es la suposición de que "la regla de abajo ya cierra la puerta".

> **Alcance.** Todo corre en tu máquina. No hay cuenta de nube, no hay costo y no se prueba nada contra redes de nadie. Lo que se analiza son **configuraciones tuyas**.

---

## Antes de empezar

- Linux o macOS con Python 3 reciente (los comandos se probaron con Python 3.14, moto 5.2.5, boto3 1.44.0, cfn-lint 1.60.0 y checkov 3.2.459, en noviembre de 2026)
- Dos terminales: una para el servidor moto y otra para tus comandos
- **No uses credenciales reales de AWS** en ninguna parte de esta práctica: las claves `test` que ves son las que acepta moto

---

## Paso 0 - El laboratorio

```bash
mkdir -p ~/lab-cloud/m03 && cd ~/lab-cloud
python3 -m venv v
v/bin/pip install "moto[server]==5.2.5" boto3==1.44.0 checkov==3.2.459 cfn-lint==1.60.0
```

Arranca moto en la **terminal 1** (queda en primer plano; lo detienes con `Ctrl+C` al final):

```bash
cd ~/lab-cloud
v/bin/moto_server -p 5031
```

Comprueba desde la terminal 2 que contesta:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:5031/moto-api/
```

Debe imprimir un código HTTP (200). Si el puerto está ocupado, usa otro y cámbialo en `EP` de los scripts.

---

## Paso 1 - Construir la red mala

Guarda como `~/lab-cloud/m03/red_lab.py`:

```python
"""Laboratorio local de red: construye en moto (no en AWS) la red del Taller Demo."""
import boto3, json, sys

EP = "http://127.0.0.1:5031"
ec2 = boto3.client("ec2", endpoint_url=EP, region_name="us-east-1",
                   aws_access_key_id="test", aws_secret_access_key="test")

vpc = ec2.create_vpc(CidrBlock="10.20.0.0/16")["Vpc"]["VpcId"]
sub = {}
for nombre, cidr in [("publica", "10.20.1.0/24"), ("app", "10.20.11.0/24"), ("datos", "10.20.21.0/24")]:
    sub[nombre] = ec2.create_subnet(VpcId=vpc, CidrBlock=cidr)["Subnet"]["SubnetId"]

def sg(nombre, desc):
    return ec2.create_security_group(GroupName=nombre, Description=desc, VpcId=vpc)["GroupId"]

sg_alb, sg_app, sg_db = sg("sg-alb", "entrada publica"), sg("sg-app", "api-lab"), sg("sg-db", "db-lab")

# Configuracion MALA a proposito: la base acepta a cualquiera en 5432
ec2.authorize_security_group_ingress(GroupId=sg_alb, IpPermissions=[
    {"IpProtocol": "tcp", "FromPort": 443, "ToPort": 443, "IpRanges": [{"CidrIp": "0.0.0.0/0"}]}])
ec2.authorize_security_group_ingress(GroupId=sg_app, IpPermissions=[
    {"IpProtocol": "tcp", "FromPort": 8080, "ToPort": 8080, "UserIdGroupPairs": [{"GroupId": sg_alb}]},
    {"IpProtocol": "tcp", "FromPort": 22, "ToPort": 22, "IpRanges": [{"CidrIp": "0.0.0.0/0"}]}])
ec2.authorize_security_group_ingress(GroupId=sg_db, IpPermissions=[
    {"IpProtocol": "tcp", "FromPort": 5432, "ToPort": 5432, "IpRanges": [{"CidrIp": "0.0.0.0/0"}]}])
json.dump({"vpc": vpc, "sg_alb": sg_alb, "sg_app": sg_app, "sg_db": sg_db, "subnets": sub}, open("ids.json", "w"))
print("VPC", vpc, "con 3 subredes y 3 grupos de seguridad")
```

Ejecútalo:

```bash
cd ~/lab-cloud/m03 && ../v/bin/python red_lab.py
```

Salida real (moto 5.2.5, boto3 1.44.0; el identificador cambia en cada corrida):

```
VPC vpc-30260a1715f010755 con 3 subredes y 3 grupos de seguridad
```

Antes de seguir, responde por escrito: ¿qué dos reglas están mal y quién llega a cada puerto? ¿Qué regla **sí** está bien y por qué?

---

## Paso 2 - El auditor

Guarda como `auditar_sg.py`:

```python
"""Detecta reglas de entrada abiertas al mundo y las reporta. Sale 1 si hay hallazgos."""
import boto3, sys
EP = "http://127.0.0.1:5031"
SENSIBLES = {22: "SSH", 3389: "RDP", 5432: "PostgreSQL", 3306: "MySQL", 1433: "SQL Server"}
ec2 = boto3.client("ec2", endpoint_url=EP, region_name="us-east-1",
                   aws_access_key_id="test", aws_secret_access_key="test")
hallazgos = 0
for g in ec2.describe_security_groups()["SecurityGroups"]:
    if g["GroupName"] == "default":
        continue
    for p in g["IpPermissions"]:
        for r in p.get("IpRanges", []):
            if r["CidrIp"] == "0.0.0.0/0":
                desde, hasta = p.get("FromPort", 0), p.get("ToPort", 65535)
                sens = [n for pt, n in SENSIBLES.items() if desde <= pt <= hasta]
                if sens:
                    hallazgos += 1
                    print(f"HALLAZGO {g['GroupName']} ({g['GroupId']}): {','.join(sens)} abierto a 0.0.0.0/0 (puertos {desde}-{hasta})")
print("hallazgos:", hallazgos)
sys.exit(1 if hallazgos else 0)
```

```bash
../v/bin/python auditar_sg.py; echo "salida=$?"
```

Salida real:

```
HALLAZGO sg-app (sg-326b53c0a9e045f7f): SSH abierto a 0.0.0.0/0 (puertos 22-22)
HALLAZGO sg-db (sg-63b564ca386b92c9a): PostgreSQL abierto a 0.0.0.0/0 (puertos 5432-5432)
hallazgos: 2
salida=1
```

Dos cosas que debes entender del script:

1. Ignora el grupo `default` a propósito: otro paso de la práctica lo revisa aparte.
2. Sale con **1** si encuentra algo. Eso lo vuelve una puerta de pipeline.

---

## Paso 3 - Corregir

Guarda como `corregir_sg.py`:

```python
import boto3, json
EP = "http://127.0.0.1:5031"
ids = json.load(open("ids.json"))
ec2 = boto3.client("ec2", endpoint_url=EP, region_name="us-east-1",
                   aws_access_key_id="test", aws_secret_access_key="test")
# 1) quitar lo abierto al mundo
ec2.revoke_security_group_ingress(GroupId=ids["sg_db"], IpPermissions=[
    {"IpProtocol": "tcp", "FromPort": 5432, "ToPort": 5432, "IpRanges": [{"CidrIp": "0.0.0.0/0"}]}])
ec2.revoke_security_group_ingress(GroupId=ids["sg_app"], IpPermissions=[
    {"IpProtocol": "tcp", "FromPort": 22, "ToPort": 22, "IpRanges": [{"CidrIp": "0.0.0.0/0"}]}])
# 2) la base solo acepta al grupo de la app
ec2.authorize_security_group_ingress(GroupId=ids["sg_db"], IpPermissions=[
    {"IpProtocol": "tcp", "FromPort": 5432, "ToPort": 5432, "UserIdGroupPairs": [{"GroupId": ids["sg_app"]}]}])
print("corregido")
```

```bash
../v/bin/python corregir_sg.py && ../v/bin/python auditar_sg.py; echo "salida=$?"
```

Salida real:

```
corregido
hallazgos: 0
salida=0
```

Anota en tu bitácora: qué quitaste, qué pusiste y **por qué el origen de la regla nueva es un grupo y no un rango**.

---

## Paso 4 - Verlo fallar (primera parte): lo que el auditor no ve

Alguien "ayuda" y agrega una regla nueva, esta vez al **3306** (MySQL), que no es la del Taller Demo pero está en la lista de `SENSIBLES`:

```bash
../v/bin/python - <<'EOF'
import boto3, json
ids = json.load(open("ids.json"))
ec2 = boto3.client("ec2", endpoint_url="http://127.0.0.1:5031", region_name="us-east-1",
                   aws_access_key_id="test", aws_secret_access_key="test")
ec2.authorize_security_group_ingress(GroupId=ids["sg_app"], IpPermissions=[
    {"IpProtocol": "tcp", "FromPort": 3306, "ToPort": 3306, "IpRanges": [{"CidrIp": "0.0.0.0/0"}]}])
EOF
../v/bin/python auditar_sg.py; echo "salida=$?"
```

Resultado esperado: el auditor **lo detecta** (`MySQL abierto a 0.0.0.0/0`) y sale con 1. Revoca esa regla (`revoke_security_group_ingress`, con los mismos parámetros) y prueba un puerto que **no** está en la lista, por ejemplo el 6379 (Redis), con el mismo comando cambiando `3306` por `6379`.

Pregunta de cierre del paso: ¿el auditor lo detecta? Si la respuesta es no, **ahí está la falla del auditor**, no de la red. Anota qué cambiarías (una lista de puertos sensibles que crece, o marcar **cualquier** `0.0.0.0/0` salvo 443 en el balanceador).

Revoca también la del 6379 antes de seguir.

---

## Paso 5 - Revisar la plantilla antes de que exista

Crea dos carpetas con Terraform (es el mismo diseño, mal y bien). `malo/red.tf`:

```hcl
resource "aws_vpc" "lab" {
  cidr_block = "10.20.0.0/16"
}

resource "aws_security_group" "db" {
  name        = "sg-db"
  description = "db-lab"
  vpc_id      = aws_vpc.lab.id

  ingress {
    description = "PostgreSQL desde cualquier lugar"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_db_instance" "lab" {
  identifier          = "db-lab"
  engine              = "postgres"
  instance_class      = "db.t3.micro"
  allocated_storage   = 20
  publicly_accessible = true
}
```

`bueno/red.tf`:

```hcl
resource "aws_vpc" "lab" {
  cidr_block = "10.20.0.0/16"
}

resource "aws_security_group" "app" {
  name        = "sg-app"
  description = "api-lab"
  vpc_id      = aws_vpc.lab.id
}

resource "aws_security_group" "db" {
  name        = "sg-db"
  description = "db-lab"
  vpc_id      = aws_vpc.lab.id

  ingress {
    description     = "PostgreSQL solo desde la API"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.app.id]
  }
}

resource "aws_db_instance" "lab" {
  identifier          = "db-lab"
  engine              = "postgres"
  instance_class      = "db.t3.micro"
  allocated_storage   = 20
  publicly_accessible = false
}
```

```bash
mkdir -p malo bueno   # y guarda cada archivo en su carpeta
../v/bin/checkov -d malo  --quiet --compact --framework terraform -c CKV_AWS_17,CKV_AWS_24,CKV_AWS_25,CKV_AWS_260,CKV_AWS_23
../v/bin/checkov -d bueno --quiet --compact --framework terraform -c CKV_AWS_17,CKV_AWS_24,CKV_AWS_25,CKV_AWS_260,CKV_AWS_23
```

Salida real de la plantilla mala:

```
Passed checks: 4, Failed checks: 1, Skipped checks: 0
Check: CKV_AWS_17: "Ensure all data stored in RDS is not publicly accessible"
	FAILED for resource: aws_db_instance.lab
```

Y de la buena: `Passed checks: 9, Failed checks: 0`.

**Verlo fallar (segunda parte).** La plantilla mala tiene el `0.0.0.0/0` al 5432 y **checkov no lo marcó**. Escribe la política propia `politicas/db_sin_mundo.yaml`:

```yaml
metadata:
  id: "CKV2_LAB_001"
  name: "Ninguna regla de entrada de un grupo de seguridad usa 0.0.0.0/0"
  category: "NETWORKING"
definition:
  and:
    - cond_type: attribute
      resource_types:
        - aws_security_group
      attribute: ingress
      operator: not_contains
      value: "0.0.0.0/0"
```

```bash
../v/bin/checkov -d malo  --quiet --compact --framework terraform --external-checks-dir politicas -c CKV2_LAB_001
../v/bin/checkov -d bueno --quiet --compact --framework terraform --external-checks-dir politicas -c CKV2_LAB_001
```

Salida real: la mala falla (`FAILED for resource: aws_security_group.db`) y la buena da `Passed checks: 2, Failed checks: 0`.

---

## Paso 6 - La NACL sin estado

Guarda como `nacl_sim.py` (**simulación simplificada**: reglas numeradas, primera coincidencia gana, final `deny`, sin estado; no es una NACL real):

```python
"""Simulacion simplificada de una NACL: reglas numeradas, primera coincidencia gana,
final implicito DENY, y SIN estado (la respuesta se evalua aparte)."""
import ipaddress

def evalua(reglas, proto, puerto, ip):
    for num, accion, p, desde, hasta, cidr in sorted(reglas, key=lambda r: r[0]):
        if p in (proto, "all") and desde <= puerto <= hasta and ipaddress.ip_address(ip) in ipaddress.ip_network(cidr):
            return accion, num
    return "deny", "*"

# (numero, accion, proto, desde, hasta, cidr)
entrada = [(100, "allow", "tcp", 443, 443, "0.0.0.0/0")]
salida_sin_retorno = []                                   # nadie permitio la respuesta
salida_con_retorno = [(100, "allow", "tcp", 1024, 65535, "0.0.0.0/0")]

# Un cliente 198.51.100.7 pide HTTPS (puerto destino 443); la respuesta sale al puerto efimero 50000
print("entrada 443 :", evalua(entrada, "tcp", 443, "198.51.100.7"))
print("respuesta sin regla de salida :", evalua(salida_sin_retorno, "tcp", 50000, "198.51.100.7"))
print("respuesta con 1024-65535      :", evalua(salida_con_retorno, "tcp", 50000, "198.51.100.7"))
# El orden importa: deny 90 antes de allow 100
con_bloqueo = [(90, "deny", "tcp", 443, 443, "198.51.100.0/24")] + entrada
print("con deny 90 al /24 :", evalua(con_bloqueo, "tcp", 443, "198.51.100.7"))
print("otro cliente       :", evalua(con_bloqueo, "tcp", 443, "203.0.113.9"))
```

```bash
python3 nacl_sim.py
```

Salida real:

```
entrada 443 : ('allow', 100)
respuesta sin regla de salida : ('deny', '*')
respuesta con 1024-65535      : ('allow', 100)
con deny 90 al /24 : ('deny', 90)
otro cliente       : ('allow', 100)
```

**Verlo fallar.** Cambia la regla de `deny` para que tenga el número **110** en lugar de 90 y vuelve a correr. Escribe qué cambió y **por qué**: a partir de 110 ya ganó el `allow` de la 100.

---

## Paso 7 - Azure como fragmento, revisado

Este archivo **solo se analiza**; nunca se despliega. `bicep/nsg.bicep`:

```bicep
resource nsg 'Microsoft.Network/networkSecurityGroups@2023-11-01' = {
  name: 'nsg-db'
  location: 'eastus'
  properties: {
    securityRules: [
      {
        name: 'permitir-ssh-a-todos'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}
```

```bash
../v/bin/checkov -d bicep --quiet --compact --framework bicep
```

Salida real:

```
Passed checks: 2, Failed checks: 1, Skipped checks: 0
Check: CKV_AZURE_10: "Ensure that SSH access is restricted from the internet"
	FAILED for resource: Microsoft.Network/networkSecurityGroups.nsg
```

Corrige el archivo (limita `sourceAddressPrefix` a `203.0.113.0/24`, un rango de documentación) y repite hasta que pase.

---

## Paso 8 - Cierre y limpieza

1. Detén moto en la terminal 1 con `Ctrl+C`. Si lo arrancaste en segundo plano, mátalo **por su PID**.
2. Guarda en tu repositorio: `red_lab.py`, `auditar_sg.py`, `corregir_sg.py`, `malo/`, `bueno/`, `politicas/` y la bitácora de los pasos 1 a 7.
3. No dejes procesos vivos ni puertos abiertos.

---

## Qué debes llevarte

- Un auditor de reglas de red que **sale con 1** y que puedes poner en un pipeline
- La evidencia de que tu corrección se **verifica** con la misma herramienta que detectó el error
- Una regla propia donde las listas de herramientas no llegan
- La NACL sin estado, vista fallar con tus propias reglas
