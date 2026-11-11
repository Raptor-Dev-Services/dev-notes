# Práctica - M05 - Leer, detectar y apagar el registro

**Seguridad en la nube y Zero Trust**
**Duración estimada:** 45 minutos en clase (el resto en la tarea)

---

## Objetivo

Con el registro de una cuenta de laboratorio ficticia, contestar cinco preguntas (quién, qué, cuándo, desde dónde, si funcionó), detectar cuatro conductas con reglas propias, auditar la configuración de un trail y **ver cómo la auditoría falla** cuando alguien apaga el registro.

> Todo corre en tu máquina. **No uses una cuenta de nube real**, con costo o sin él. El simulador `moto` imita la API de AWS en `127.0.0.1`; los registros de la práctica son un **ejemplo ilustrativo** con el formato documentado de CloudTrail, de la cuenta ficticia `111122223333` (Caso hipotético: Taller Demo).

---

## Antes de empezar

Necesitas Python 3, `jq` y unos 300 MB libres. Entorno aislado:

```bash
mkdir -p ~/lab-cloud/m05 && cd ~/lab-cloud/m05
python3 -m venv v
v/bin/pip install -q "moto[server,iam,s3,kms,cloudtrail]" boto3
v/bin/pip list 2>/dev/null | grep -iE "^(moto|boto3) "
```

Salida real (noviembre de 2026): `boto3 1.44.0` y `moto 5.2.5`. Con otras versiones, el comportamiento puede variar un poco.

---

## Paso 1 - El registro de ejemplo

Guarda esto como `eventos-lab.json`. Son 7 eventos del 10 de noviembre de 2026 (UTC), con IP de documentación (`203.0.113.50`, `198.51.100.7`):

```json
{"Records":[
{"eventVersion":"1.11","userIdentity":{"type":"Root","principalId":"111122223333","arn":"arn:aws:iam::111122223333:root","accountId":"111122223333"},"eventTime":"2026-11-10T14:02:11Z","eventSource":"signin.amazonaws.com","eventName":"ConsoleLogin","awsRegion":"us-east-1","sourceIPAddress":"203.0.113.50","userAgent":"Mozilla/5.0","requestParameters":null,"responseElements":{"ConsoleLogin":"Success"},"additionalEventData":{"MFAUsed":"No"},"eventID":"11111111-0000-0000-0000-000000000001","eventType":"AwsConsoleSignIn","recipientAccountId":"111122223333"},
{"eventVersion":"1.11","userIdentity":{"type":"IAMUser","principalId":"AIDAEXAMPLEDEPLOY","arn":"arn:aws:iam::111122223333:user/app-deploy","accountId":"111122223333","userName":"app-deploy"},"eventTime":"2026-11-10T14:05:40Z","eventSource":"cloudtrail.amazonaws.com","eventName":"StopLogging","awsRegion":"us-east-1","sourceIPAddress":"203.0.113.50","userAgent":"aws-cli/2.x","requestParameters":{"name":"trail-lab"},"responseElements":null,"eventID":"11111111-0000-0000-0000-000000000002","readOnly":false,"eventType":"AwsApiCall","managementEvent":true,"recipientAccountId":"111122223333","eventCategory":"Management"},
{"eventVersion":"1.11","userIdentity":{"type":"AssumedRole","principalId":"AROAEXAMPLEAPI:sesion-api","arn":"arn:aws:sts::111122223333:assumed-role/api-lab-rol/sesion-api","accountId":"111122223333"},"eventTime":"2026-11-10T14:06:01Z","eventSource":"s3.amazonaws.com","eventName":"GetObject","awsRegion":"us-east-1","sourceIPAddress":"198.51.100.7","userAgent":"aws-sdk-dotnet","errorCode":"AccessDenied","errorMessage":"User is not authorized to perform: s3:GetObject","requestParameters":{"bucketName":"lab-datos","key":"nominas/2026-10.csv"},"responseElements":null,"eventID":"11111111-0000-0000-0000-000000000003","readOnly":true,"eventType":"AwsApiCall","recipientAccountId":"111122223333","eventCategory":"Data"},
{"eventVersion":"1.11","userIdentity":{"type":"AssumedRole","principalId":"AROAEXAMPLEAPI:sesion-api","arn":"arn:aws:sts::111122223333:assumed-role/api-lab-rol/sesion-api","accountId":"111122223333"},"eventTime":"2026-11-10T14:06:03Z","eventSource":"s3.amazonaws.com","eventName":"GetObject","awsRegion":"us-east-1","sourceIPAddress":"198.51.100.7","userAgent":"aws-sdk-dotnet","errorCode":"AccessDenied","errorMessage":"User is not authorized to perform: s3:GetObject","requestParameters":{"bucketName":"lab-datos","key":"nominas/2026-09.csv"},"responseElements":null,"eventID":"11111111-0000-0000-0000-000000000004","readOnly":true,"eventType":"AwsApiCall","recipientAccountId":"111122223333","eventCategory":"Data"},
{"eventVersion":"1.11","userIdentity":{"type":"AssumedRole","principalId":"AROAEXAMPLEAPI:sesion-api","arn":"arn:aws:sts::111122223333:assumed-role/api-lab-rol/sesion-api","accountId":"111122223333"},"eventTime":"2026-11-10T14:06:05Z","eventSource":"s3.amazonaws.com","eventName":"GetObject","awsRegion":"us-east-1","sourceIPAddress":"198.51.100.7","userAgent":"aws-sdk-dotnet","errorCode":"AccessDenied","errorMessage":"User is not authorized to perform: s3:GetObject","requestParameters":{"bucketName":"lab-datos","key":"nominas/2026-08.csv"},"responseElements":null,"eventID":"11111111-0000-0000-0000-000000000005","readOnly":true,"eventType":"AwsApiCall","recipientAccountId":"111122223333","eventCategory":"Data"},
{"eventVersion":"1.11","userIdentity":{"type":"IAMUser","principalId":"AIDAEXAMPLEDEPLOY","arn":"arn:aws:iam::111122223333:user/app-deploy","accountId":"111122223333","userName":"app-deploy"},"eventTime":"2026-11-10T14:09:12Z","eventSource":"s3.amazonaws.com","eventName":"PutBucketPolicy","awsRegion":"us-east-1","sourceIPAddress":"203.0.113.50","userAgent":"aws-cli/2.x","requestParameters":{"bucketName":"lab-datos"},"responseElements":null,"eventID":"11111111-0000-0000-0000-000000000006","readOnly":false,"eventType":"AwsApiCall","managementEvent":true,"recipientAccountId":"111122223333","eventCategory":"Management"},
{"eventVersion":"1.11","userIdentity":{"type":"AssumedRole","principalId":"AROAEXAMPLEAPI:sesion-api","arn":"arn:aws:sts::111122223333:assumed-role/api-lab-rol/sesion-api","accountId":"111122223333"},"eventTime":"2026-11-10T14:10:00Z","eventSource":"s3.amazonaws.com","eventName":"GetObject","awsRegion":"us-east-1","sourceIPAddress":"198.51.100.7","userAgent":"aws-sdk-dotnet","requestParameters":{"bucketName":"lab-datos","key":"facturas/2026-11.pdf"},"responseElements":null,"eventID":"11111111-0000-0000-0000-000000000007","readOnly":true,"eventType":"AwsApiCall","recipientAccountId":"111122223333","eventCategory":"Data"}
]}
```

Ejemplo ilustrativo: los campos siguen la estructura documentada de CloudTrail (`eventVersion` 1.11). No es una captura de una cuenta real.

---

## Paso 2 - Leer con jq

Cuántos eventos de cada tipo:

```bash
jq -r '.Records | map(.eventName) | group_by(.) | map({e:.[0],n:length}) | .[] | "\(.n) \(.e)"' eventos-lab.json
```

Salida real (jq, noviembre de 2026):

```
1 ConsoleLogin
4 GetObject
1 PutBucketPolicy
1 StopLogging
```

Solo los que fallaron:

```bash
jq -r '.Records[] | select(.errorCode) | [.eventTime,.userIdentity.arn,.eventName,.errorCode] | @tsv' eventos-lab.json
```

```
2026-11-10T14:06:01Z	arn:aws:sts::111122223333:assumed-role/api-lab-rol/sesion-api	GetObject	AccessDenied
2026-11-10T14:06:03Z	arn:aws:sts::111122223333:assumed-role/api-lab-rol/sesion-api	GetObject	AccessDenied
2026-11-10T14:06:05Z	arn:aws:sts::111122223333:assumed-role/api-lab-rol/sesion-api	GetObject	AccessDenied
```

**Anota:** ¿qué identidad, sobre qué recursos y con qué ritmo? (Mira `requestParameters.key` de esos tres registros.)

---

## Paso 3 - Reglas de detección

Guarda como `buscar.py`:

```python
"""Reglas de deteccion sobre un archivo de registros con el formato de CloudTrail."""
import json, sys
from collections import Counter

registros = json.load(open(sys.argv[1]))["Records"]
hallazgos = []

for r in registros:
    ident = r["userIdentity"]
    # Regla 1: uso de la cuenta raiz
    if ident["type"] == "Root":
        sin_mfa = r.get("additionalEventData", {}).get("MFAUsed") == "No"
        hallazgos.append(("ALTA", "uso de root" + (" sin MFA" if sin_mfa else ""), r["eventName"], r["eventTime"], r["sourceIPAddress"]))
    # Regla 2: apagar o borrar el registro
    if r["eventSource"] == "cloudtrail.amazonaws.com" and r["eventName"] in ("StopLogging", "DeleteTrail", "UpdateTrail"):
        hallazgos.append(("ALTA", "se toco el registro", r["eventName"], r["eventTime"], ident["arn"]))
    # Regla 3: cambio de politica de un bucket
    if r["eventName"] in ("PutBucketPolicy", "DeleteBucketPolicy", "PutBucketAcl", "PutBucketPublicAccessBlock", "DeletePublicAccessBlock"):
        hallazgos.append(("MEDIA", "cambio de acceso a bucket", r["eventName"], r["eventTime"], ident["arn"]))

# Regla 4: rafaga de AccessDenied por la misma identidad (umbral 3 en este lote)
denegados = Counter(r["userIdentity"]["arn"] for r in registros if r.get("errorCode") == "AccessDenied")
for arn, n in denegados.items():
    if n >= 3:
        hallazgos.append(("MEDIA", f"{n} AccessDenied seguidos", "varios", "-", arn))

for h in sorted(hallazgos, key=lambda x: (x[0] != "ALTA", x[3])):
    print(" | ".join(h))
print(f"{len(registros)} registros leidos, {len(hallazgos)} hallazgos")
```

Corre:

```bash
v/bin/python buscar.py eventos-lab.json
```

Salida real:

```
ALTA | uso de root sin MFA | ConsoleLogin | 2026-11-10T14:02:11Z | 203.0.113.50
ALTA | se toco el registro | StopLogging | 2026-11-10T14:05:40Z | arn:aws:iam::111122223333:user/app-deploy
MEDIA | 3 AccessDenied seguidos | varios | - | arn:aws:sts::111122223333:assumed-role/api-lab-rol/sesion-api
MEDIA | cambio de acceso a bucket | PutBucketPolicy | 2026-11-10T14:09:12Z | arn:aws:iam::111122223333:user/app-deploy
7 registros leidos, 4 hallazgos
```

**Punto de reflexión.** El script detecta tres conductas con una regla cada una y la cuarta (ráfaga) con un conteo. Ninguna usa inteligencia: es lo que el equipo decidió vigilar. Eso es lo bueno (se entiende) y lo malo (solo ve lo que pensaste).

---

## Paso 4 - Auditar el trail

Arranca el simulador (en otra terminal, o en segundo plano) y crea un trail con buena configuración:

```bash
v/bin/moto_server -p 5051 > moto.log 2>&1 &
echo $! > moto.pid
sleep 4
```

```bash
v/bin/python - <<'PY'
import boto3
kw = dict(endpoint_url="http://127.0.0.1:5051", region_name="us-east-1",
          aws_access_key_id="test", aws_secret_access_key="test")
boto3.client("s3", **kw).create_bucket(Bucket="lab-trail-logs")
ct = boto3.client("cloudtrail", **kw)
ct.create_trail(Name="trail-lab", S3BucketName="lab-trail-logs",
                IsMultiRegionTrail=True, EnableLogFileValidation=True)
ct.start_logging(Name="trail-lab")
PY
```

Guarda como `auditar_trail.py`:

```python
"""Revisa la configuracion de los trails de una cuenta (contra moto en el laboratorio)."""
import sys, boto3
ep = sys.argv[1]
kw = dict(endpoint_url=ep, region_name="us-east-1", aws_access_key_id="test", aws_secret_access_key="test")
ct = boto3.client("cloudtrail", **kw)
trails = ct.describe_trails()["trailList"]
if not trails:
    print("FALLA: no hay ningun trail"); sys.exit(1)
fallas = 0
for t in trails:
    estado = ct.get_trail_status(Name=t["Name"])
    checks = {
        "esta registrando": estado["IsLogging"],
        "multi-region": t.get("IsMultiRegionTrail", False),
        "validacion de integridad": t.get("LogFileValidationEnabled", False),
    }
    for nombre, ok in checks.items():
        print(f'{t["Name"]:12} {nombre:26} {"OK" if ok else "FALLA"}')
        fallas += not ok
sys.exit(1 if fallas else 0)
```

```bash
v/bin/python auditar_trail.py http://127.0.0.1:5051; echo "salio $?"
```

Salida real:

```
trail-lab    esta registrando           OK
trail-lab    multi-region               OK
trail-lab    validacion de integridad   OK
salio 0
```

El código de salida 0 es lo que un pipeline o una tarea programada leería como "todo bien".

---

## Paso 5 - Verlo fallar

Simula la acción del registro de ejemplo: alguien apaga el trail.

```bash
v/bin/python -c "
import boto3
boto3.client('cloudtrail', endpoint_url='http://127.0.0.1:5051', region_name='us-east-1',
             aws_access_key_id='t', aws_secret_access_key='t').stop_logging(Name='trail-lab')"
v/bin/python auditar_trail.py http://127.0.0.1:5051; echo "salio $?"
```

Salida real:

```
trail-lab    esta registrando           FALLA
trail-lab    multi-region               OK
trail-lab    validacion de integridad   OK
salio 1
```

Esto es lo que debes entender: **la configuración de un trail "bien hecho" no te dice que siga registrando**. La auditoría tiene que preguntar el *estado*, no solo la configuración. Vuelve a encenderlo y comprueba:

```bash
v/bin/python -c "
import boto3
boto3.client('cloudtrail', endpoint_url='http://127.0.0.1:5051', region_name='us-east-1',
             aws_access_key_id='t', aws_secret_access_key='t').start_logging(Name='trail-lab')"
v/bin/python auditar_trail.py http://127.0.0.1:5051; echo "salio $?"
```

Debe volver a salir 0. Apaga el simulador al terminar:

```bash
kill $(cat moto.pid)
```

**Qué NO prueba este simulador:** no genera eventos de CloudTrail, no valida los archivos de resumen con firmas y su `lookup_events` no está implementado (devuelve error 500 al llamarlo). Para el historial real de 90 días necesitarías una cuenta de nube; este curso no te pide tocar una.

---

## Paso 6 - La línea de tiempo

Escribe, en `linea-de-tiempo.md`, una tabla con las 7 filas del registro y una columna "Qué significa". Ejemplo de las dos primeras:

| Hora (UTC) | Quién | Qué | Qué significa |
|---|---|---|---|
| 14:02:11 | root, `203.0.113.50` | `ConsoleLogin` sin MFA | La cuenta raíz entró solo con contraseña |
| 14:05:40 | `app-deploy`, misma IP | `StopLogging` | Se apagó el trail; desde aquí hay un hueco en el registro |

Responde al final, en tres líneas: ¿qué regla debió avisar primero? ¿qué contención recomiendas y en qué orden? ¿qué evidencia preservarías antes de tocar nada?

---

## Paso 7 - Contra la lista de errores típicos

Marca cuáles aplican a tu equipo hoy (lo que no sepas contestar, anótalo como pendiente):

- [ ] ¿Existe un trail multirregión y con validación de integridad?
- [ ] ¿El bucket de registros está en una cuenta distinta a la vigilada?
- [ ] ¿Hay al menos una alarma por `StopLogging` con una persona que la recibe?
- [ ] ¿Hay eventos de datos para el bucket que guarda lo sensible?
- [ ] ¿La retención cubre el tiempo que tardas en descubrir un problema?

---

## Entregable

`linea-de-tiempo.md` con la tabla completa y las tres respuestas, y la lista de verificación marcada. Súbelo con tu tarea.

## Limpieza

Cierra el simulador (`kill $(cat moto.pid)`) y borra el entorno si no lo vas a usar: `rm -r ~/lab-cloud/m05/v`.
