# Práctica - M06 - Una regla que marca lo mal configurado

**Seguridad en la nube y Zero Trust**
**Duración estimada:** 45 minutos en clase (el resto en la tarea)

---

## Objetivo

Convertir políticas de seguridad en reglas que se ejecutan solas: verlas **fallar** sobre una plantilla mala, escribir dos reglas propias, corregir la plantilla hasta ver todo en verde, registrar una excepción con dueño y caducidad, y detectar la **deriva** en una cuenta simulada.

> Todo corre en tu máquina: **ninguna cuenta de nube real**. Las plantillas son un **ejemplo ilustrativo** (Caso hipotético: Taller Demo, cuenta ficticia `111122223333`, red `10.20.0.0/16`). `moto` imita la API de AWS en `127.0.0.1`; `checkov` y `cfn-lint` solo leen archivos.

---

## Antes de empezar

```bash
mkdir -p ~/lab-cloud/m06/politicas && cd ~/lab-cloud/m06
python3 -m venv v
v/bin/pip install -q "moto[server,iam,s3,kms,cloudtrail]" boto3 cfn-lint checkov
v/bin/pip list 2>/dev/null | grep -iE "^(moto|boto3|cfn-lint|checkov) "
```

Salida real (noviembre de 2026): `boto3 1.44.0`, `cfn-lint 1.60.0`, `checkov 3.2.459`, `moto 5.2.5`. Con otras versiones, los identificadores y el número de reglas pueden cambiar: **anota las tuyas**.

---

## Paso 1 - Verlo fallar

Guarda `taller-malo.yaml`:

```yaml
AWSTemplateFormatVersion: "2010-09-09"
Description: Taller Demo - version con malas configuraciones a proposito (ejemplo ilustrativo)
Resources:
  DatosLab:
    Type: AWS::S3::Bucket
    Properties:
      BucketName: lab-datos
  SgBase:
    Type: AWS::EC2::SecurityGroup
    Properties:
      GroupDescription: base de datos del laboratorio
      VpcId: vpc-0123456789abcdef0
      SecurityGroupIngress:
        - IpProtocol: tcp
          FromPort: 5432
          ToPort: 5432
          CidrIp: 0.0.0.0/0
  DbLab:
    Type: AWS::RDS::DBInstance
    Properties:
      DBInstanceClass: db.t3.micro
      Engine: postgres
      AllocatedStorage: "20"
      MasterUsername: lab
      ManageMasterUserPassword: true
      PubliclyAccessible: true
      StorageEncrypted: false
      VPCSecurityGroups:
        - !Ref SgBase
```

Corre `checkov` sobre la plantilla:

```bash
v/bin/checkov -f taller-malo.yaml --framework cloudformation --compact --quiet
echo "salio $?"
```

Salida real (resumen): `Passed checks: 7, Failed checks: 12, Skipped checks: 0`. Entre los fallos están `CKV_AWS_54`, `CKV_AWS_56` (bucket público), `CKV_AWS_21` (sin versionado), `CKV_AWS_16` (base sin cifrar) y `CKV_AWS_17` (base pública).

**Pregunta que debes contestar antes de seguir:** el grupo de seguridad abre PostgreSQL (`5432`) a `0.0.0.0/0`. ¿Aparece como fallo algún identificador de **ese** problema en tu salida? (En la salida real de arriba, el grupo solo falla `CKV_AWS_23`, que pide descripción en la regla.)

Anota qué regla hay que escribir para cubrirlo.

---

## Paso 2 - Dos reglas propias

Crea `politicas/__init__.py` vacío y `politicas/reglas_taller.py`:

```python
from checkov.common.models.enums import CheckCategories, CheckResult
from checkov.cloudformation.checks.resource.base_resource_check import BaseResourceCheck

PUERTOS_DB = {5432, 3306, 1433, 6379, 27017}
ABIERTOS = {"0.0.0.0/0", "::/0"}


class SgSinDbAbierta(BaseResourceCheck):
    def __init__(self):
        super().__init__(
            name="TALLER_001: un grupo de seguridad no abre puertos de base de datos a Internet",
            id="CKV_TALLER_001",
            categories=[CheckCategories.NETWORKING],
            supported_resources=["AWS::EC2::SecurityGroup"],
        )

    def scan_resource_conf(self, conf):
        reglas = conf.get("Properties", {}).get("SecurityGroupIngress", [])
        if not isinstance(reglas, list):
            return CheckResult.UNKNOWN
        for r in reglas:
            if not isinstance(r, dict):
                continue
            desde, hasta = r.get("FromPort"), r.get("ToPort")
            origen = r.get("CidrIp") or r.get("CidrIpv6")
            try:
                rango = set(range(int(desde), int(hasta) + 1))
            except (TypeError, ValueError):
                continue
            if origen in ABIERTOS and rango & PUERTOS_DB:
                return CheckResult.FAILED
        return CheckResult.PASSED


class BucketConDueno(BaseResourceCheck):
    def __init__(self):
        super().__init__(
            name="TALLER_002: todo bucket lleva la etiqueta dueno",
            id="CKV_TALLER_002",
            categories=[CheckCategories.GENERAL_SECURITY],
            supported_resources=["AWS::S3::Bucket"],
        )

    def scan_resource_conf(self, conf):
        tags = conf.get("Properties", {}).get("Tags", [])
        if isinstance(tags, list) and any(isinstance(t, dict) and t.get("Key") == "dueno" and t.get("Value") for t in tags):
            return CheckResult.PASSED
        return CheckResult.FAILED


check1 = SgSinDbAbierta()
check2 = BucketConDueno()
```

Corre solo las dos reglas propias:

```bash
v/bin/checkov -f taller-malo.yaml --framework cloudformation \
  --external-checks-dir politicas --check CKV_TALLER_001,CKV_TALLER_002 --compact --quiet
```

Salida real:

```
cloudformation scan results:
Passed checks: 0, Failed checks: 2, Skipped checks: 0
Check: CKV_TALLER_002: "TALLER_002: todo bucket lleva la etiqueta dueno"
	FAILED for resource: AWS::S3::Bucket.DatosLab
	File: /taller-malo.yaml:4-7
Check: CKV_TALLER_001: "TALLER_001: un grupo de seguridad no abre puertos de base de datos a Internet"
	FAILED for resource: AWS::EC2::SecurityGroup.SgBase
	File: /taller-malo.yaml:8-17
```

---

## Paso 3 - Corregir y volver a medir

Guarda `taller-bueno.yaml`:

```yaml
AWSTemplateFormatVersion: "2010-09-09"
Description: Taller Demo - version corregida (ejemplo ilustrativo)
Resources:
  DatosLab:
    Type: AWS::S3::Bucket
    Properties:
      BucketName: lab-datos
      Tags:
        - Key: dueno
          Value: equipo-api
      VersioningConfiguration:
        Status: Enabled
      BucketEncryption:
        ServerSideEncryptionConfiguration:
          - ServerSideEncryptionByDefault:
              SSEAlgorithm: aws:kms
      PublicAccessBlockConfiguration:
        BlockPublicAcls: true
        BlockPublicPolicy: true
        IgnorePublicAcls: true
        RestrictPublicBuckets: true
  SgBase:
    Type: AWS::EC2::SecurityGroup
    Properties:
      GroupDescription: base de datos del laboratorio
      VpcId: vpc-0123456789abcdef0
      SecurityGroupIngress:
        - IpProtocol: tcp
          FromPort: 5432
          ToPort: 5432
          CidrIp: 10.20.1.0/24
          Description: solo la subred de la aplicacion
  DbLab:
    Type: AWS::RDS::DBInstance
    Properties:
      DBInstanceClass: db.t3.micro
      Engine: postgres
      AllocatedStorage: "20"
      MasterUsername: lab
      ManageMasterUserPassword: true
      PubliclyAccessible: false
      StorageEncrypted: true
      VPCSecurityGroups:
        - !Ref SgBase
```

Corre las dos plantillas con el mismo **gate** de nueve reglas:

```bash
K="CKV_AWS_53,CKV_AWS_54,CKV_AWS_55,CKV_AWS_56,CKV_AWS_21,CKV_AWS_16,CKV_AWS_17,CKV_TALLER_001,CKV_TALLER_002"
for f in taller-malo.yaml taller-bueno.yaml; do
  echo "== $f"
  v/bin/checkov -f $f --framework cloudformation --external-checks-dir politicas --check $K --compact --quiet | grep -E "^Passed"
done
```

Salida real:

```
== taller-malo.yaml
Passed checks: 0, Failed checks: 9, Skipped checks: 0
== taller-bueno.yaml
Passed checks: 9, Failed checks: 0, Skipped checks: 0
```

Para un pipeline, el código de salida de `checkov` es 1 con fallos y 0 sin ellos. Valida también la sintaxis:

```bash
v/bin/cfn-lint taller-bueno.yaml; echo "cfn-lint: $?"
```

Salida real: `cfn-lint: 0`.

---

## Paso 4 - Todo el catálogo y la excepción

Corre la plantilla corregida **sin** limitar las reglas:

```bash
v/bin/checkov -f taller-bueno.yaml --framework cloudformation --external-checks-dir politicas --compact --quiet | grep -E "^Passed|Check:"
```

Salida real:

```
Passed checks: 17, Failed checks: 4, Skipped checks: 0
Check: CKV_AWS_18: "Ensure the S3 bucket has access logging enabled"
Check: CKV_AWS_161: "Ensure RDS database has IAM authentication enabled"
Check: CKV_AWS_157: "Ensure that RDS instances have Multi-AZ enabled"
Check: CKV_AWS_118: "Ensure that enhanced monitoring is enabled for Amazon RDS instances"
```

Decide, por escrito, cuáles de las cuatro son obligatorias para un laboratorio y cuáles no, y por qué. Después, registra una **excepción** para `CKV_AWS_18` agregando bajo `DatosLab` (en una copia llamada `taller-excepcion.yaml`):

```yaml
    Metadata:
      checkov:
        skip:
          - id: CKV_AWS_18
            comment: "Excepcion EXC-004, dueno equipo-api, caduca 2027-02-01: el bucket de logs aun no existe"
```

```bash
v/bin/checkov -f taller-excepcion.yaml --framework cloudformation --external-checks-dir politicas \
  --check CKV_AWS_18,CKV_AWS_21 --compact --quiet | grep -E "^Passed"
```

Salida real: `Passed checks: 1, Failed checks: 0, Skipped checks: 1`.

**Qué debes ver:** la regla no desapareció; quedó **omitida con un motivo, un dueño y una fecha**. Pon la fecha en tu calendario.

---

## Paso 5 - Deriva en una cuenta simulada

Guarda `deriva.py`:

```python
"""Detecta deriva: buckets de una cuenta sin bloqueo de acceso publico ni cifrado."""
import sys, boto3
from botocore.exceptions import ClientError

s3 = boto3.client("s3", endpoint_url=sys.argv[1], region_name="us-east-1",
                  aws_access_key_id="test", aws_secret_access_key="test")
PAB = ("BlockPublicAcls", "BlockPublicPolicy", "IgnorePublicAcls", "RestrictPublicBuckets")
malos = 0
for b in s3.list_buckets()["Buckets"]:
    nombre = b["Name"]
    try:
        cfg = s3.get_public_access_block(Bucket=nombre)["PublicAccessBlockConfiguration"]
        pab_ok = all(cfg.get(k) for k in PAB)
    except ClientError:
        pab_ok = False
    try:
        s3.get_bucket_encryption(Bucket=nombre)
        cifrado = True
    except ClientError:
        cifrado = False
    estado = "CONFORME" if pab_ok and cifrado else "NO CONFORME"
    malos += estado != "CONFORME"
    print(f"{nombre:16} bloqueo-publico={pab_ok!s:5} cifrado={cifrado!s:5} {estado}")
sys.exit(1 if malos else 0)
```

Arranca el simulador, crea dos buckets "mal hechos" y mide:

```bash
v/bin/moto_server -p 5061 > moto.log 2>&1 &
echo $! > moto.pid
sleep 4
v/bin/python - <<'PY'
import boto3
s = boto3.client("s3", endpoint_url="http://127.0.0.1:5061", region_name="us-east-1",
                 aws_access_key_id="t", aws_secret_access_key="t")
s.create_bucket(Bucket="lab-datos"); s.create_bucket(Bucket="lab-reportes")
PY
v/bin/python deriva.py http://127.0.0.1:5061; echo "salio $?"
```

Salida real:

```
lab-datos        bloqueo-publico=False cifrado=False NO CONFORME
lab-reportes     bloqueo-publico=False cifrado=False NO CONFORME
salio 1
```

**Recuerda la advertencia de la clase:** moto no reproduce los valores por defecto de AWS (en AWS real, S3 cifra con SSE-S3 los objetos nuevos desde el 5 de enero de 2023 y los buckets nuevos nacen con el bloqueo de acceso público activado). Aquí sirve para practicar la **lógica** de la regla.

Corrige los dos buckets en el bloqueo y solo uno en el cifrado:

```bash
v/bin/python - <<'PY'
import boto3
s = boto3.client("s3", endpoint_url="http://127.0.0.1:5061", region_name="us-east-1",
                 aws_access_key_id="t", aws_secret_access_key="t")
for b in ("lab-datos", "lab-reportes"):
    s.put_public_access_block(Bucket=b, PublicAccessBlockConfiguration={
        k: True for k in ("BlockPublicAcls", "BlockPublicPolicy", "IgnorePublicAcls", "RestrictPublicBuckets")})
s.put_bucket_encryption(Bucket="lab-datos", ServerSideEncryptionConfiguration={
    "Rules": [{"ApplyServerSideEncryptionByDefault": {"SSEAlgorithm": "aws:kms"}}]})
PY
v/bin/python deriva.py http://127.0.0.1:5061; echo "salio $?"
```

Salida real:

```
lab-datos        bloqueo-publico=True  cifrado=True  CONFORME
lab-reportes     bloqueo-publico=True  cifrado=False NO CONFORME
salio 1
```

El bucket que corregiste cambió; el que olvidaste **sigue marcado**. Corrige el segundo y verifica que la salida sea 0. Al terminar: `kill $(cat moto.pid)`.

---

## Paso 6 - Las reglas de la nube, como fragmento

Estas dos definiciones **no se despliegan** en la práctica (requieren una cuenta con Config activado o una suscripción de Azure). Se validan por sintaxis. Guarda esta como `config-regla.yaml`:

```yaml
AWSTemplateFormatVersion: "2010-09-09"
Description: Regla administrada de AWS Config (ejemplo ilustrativo)
Resources:
  BucketSinLecturaPublica:
    Type: AWS::Config::ConfigRule
    Properties:
      ConfigRuleName: s3-bucket-public-read-prohibited
      Source:
        Owner: AWS
        SourceIdentifier: S3_BUCKET_PUBLIC_READ_PROHIBITED
      Scope:
        ComplianceResourceTypes:
          - AWS::S3::Bucket
```

Y esta como `azure-policy.json`:

```json
{
  "properties": {
    "displayName": "Taller Demo: las cuentas de almacenamiento no permiten acceso anonimo",
    "mode": "Indexed",
    "parameters": {
      "effect": {
        "type": "String",
        "allowedValues": ["Audit", "Deny", "Disabled"],
        "defaultValue": "Audit"
      }
    },
    "policyRule": {
      "if": {
        "allOf": [
          { "field": "type", "equals": "Microsoft.Storage/storageAccounts" },
          { "field": "Microsoft.Storage/storageAccounts/allowBlobPublicAccess", "notEquals": false }
        ]
      },
      "then": { "effect": "[parameters('effect')]" }
    }
  }
}
```

Fragmentos (no ejecutados contra ninguna nube). Valida:

```bash
v/bin/cfn-lint config-regla.yaml; echo "cfn-lint: $?"
```

Salida real: `cfn-lint: 0`. La definición de Azure se valida solo como JSON:

```bash
python3 -c "import json; json.load(open('azure-policy.json')); print('json valido')"
```

Salida real: `json valido`. Que el JSON sea válido **no** prueba que Azure la acepte: el alias de la propiedad debe comprobarse contra la documentación vigente.

Escribe, en una tabla, cuál de tus reglas locales equivale a cuál regla de la nube y qué diferencia hay entre revisar una plantilla y revisar la cuenta viva.

---

## Paso 7 - Un ADR de tu cuenta

Escribe `adr-0001.md` con los campos de la clase (contexto, decisión, alternativas, consecuencias, dueño y fecha de revisión) para **una** decisión de arquitectura de tu laboratorio, y menciona qué regla de esta práctica la vigila.

---

## Entregable

Una carpeta con `reglas_taller.py`, las dos plantillas, la salida de los pasos 1 a 5 y `adr-0001.md`.

## Limpieza

`kill $(cat moto.pid)` y, si no lo vas a usar, `rm -r ~/lab-cloud/m06/v`.
