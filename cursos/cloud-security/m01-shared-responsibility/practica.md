# Práctica - M01 - Inventario de una cuenta de laboratorio

**Seguridad en la nube y Zero Trust**
**Duración estimada:** 60 a 75 minutos

---

## Objetivo

Producir el inventario de una cuenta de nube **simulada**, con dueño y clasificación por recurso; encontrar lo que no tiene dueño ni protección contra acceso público; corregirlo; y demostrar con una segunda medición que quedó corregido.

> **Qué es y qué no es este laboratorio.** Usa **moto**, un servidor que imita la API de AWS en tu máquina. No toca ninguna cuenta real ni cuesta nada. Moto es un simulador: implementa un subconjunto del comportamiento de AWS y **no** modela todo (más abajo verás un caso donde difiere). Lo que aprendes aquí es el **método**: inventariar, detectar, corregir, verificar. Las salidas reales de AWS pueden diferir en detalles.

Versiones con las que se escribió (a noviembre de 2026): Python 3.14, moto 5.2.5, boto3 1.44.0.

---

## Antes de empezar

```bash
python3 -m venv ~/lab-nube && source ~/lab-nube/bin/activate
pip install "moto[server]==5.2.5" "boto3==1.44.0"
mkdir -p ~/lab-nube/m01 && cd ~/lab-nube/m01
```

En una **segunda terminal** (con el mismo `source`) levanta la nube simulada y déjala corriendo:

```bash
moto_server -p 5011
```

Las credenciales que usan los scripts (`test` / `test`) son falsas y solo las acepta el simulador.

---

## Paso 1 - Sembrar la cuenta de laboratorio

Guarda esto como `sembrar.py`. Crea la cuenta del Caso hipotético **Taller Demo**: un bucket bien etiquetado y protegido, dos buckets descuidados, una instancia y dos usuarios.

```python
import boto3
EP = "http://127.0.0.1:5011"
def c(svc): return boto3.client(svc, endpoint_url=EP, region_name="us-east-1",
                                 aws_access_key_id="test", aws_secret_access_key="test")
s3, ec2, iam, kms = c("s3"), c("ec2"), c("iam"), c("kms")

s3.create_bucket(Bucket="lab-datos")
s3.put_bucket_tagging(Bucket="lab-datos", Tagging={"TagSet": [
    {"Key": "taller:owner", "Value": "equipo-api"},
    {"Key": "taller:clasificacion", "Value": "confidencial"}]})
s3.put_public_access_block(Bucket="lab-datos", PublicAccessBlockConfiguration={
    "BlockPublicAcls": True, "IgnorePublicAcls": True,
    "BlockPublicPolicy": True, "RestrictPublicBuckets": True})

s3.create_bucket(Bucket="lab-logs")          # sin etiquetas y sin bloqueo
s3.create_bucket(Bucket="lab-prueba-juan")   # sin etiquetas, olvidado

ec2.run_instances(ImageId="ami-12c6146b", MinCount=1, MaxCount=1, InstanceType="t3.micro",
    TagSpecifications=[{"ResourceType": "instance", "Tags": [
        {"Key": "Name", "Value": "api-lab"},
        {"Key": "taller:owner", "Value": "equipo-api"}]}])

iam.create_user(UserName="app")
iam.create_user(UserName="juan")
kms.create_key(Description="llave del bucket lab-datos")
print("cuenta de laboratorio sembrada")
```

```bash
python sembrar.py
```

Salida real (moto 5.2.5, boto3 1.44.0, noviembre de 2026):

```
cuenta de laboratorio sembrada
```

---

## Paso 2 - El inventario

Guarda como `inventario.py`. Lista buckets, instancias y usuarios; para cada uno saca dueño y clasificación de las etiquetas y anota qué etiquetas faltan y, en buckets, el estado del bloqueo de acceso público.

```python
import csv, sys, boto3
from botocore.exceptions import ClientError
EP = "http://127.0.0.1:5011"
def c(svc): return boto3.client(svc, endpoint_url=EP, region_name="us-east-1",
                                 aws_access_key_id="test", aws_secret_access_key="test")
OWNER, CLASE = "taller:owner", "taller:clasificacion"
filas = []

def fila(tipo, nombre, tags, extra=""):
    falta = [k for k in (OWNER, CLASE) if k not in tags]
    filas.append([tipo, nombre, tags.get(OWNER, ""), tags.get(CLASE, ""), ",".join(falta), extra])

s3 = c("s3")
for b in s3.list_buckets()["Buckets"]:
    n = b["Name"]
    try:
        tags = {x["Key"]: x["Value"] for x in s3.get_bucket_tagging(Bucket=n)["TagSet"]}
    except ClientError:
        tags = {}
    try:
        bpa = s3.get_public_access_block(Bucket=n)["PublicAccessBlockConfiguration"]
        extra = "bloqueo-publico=" + ("completo" if all(bpa.values()) else "parcial")
    except ClientError:
        extra = "bloqueo-publico=NINGUNO"
    fila("s3:bucket", n, tags, extra)

for r in c("ec2").describe_instances()["Reservations"]:
    for i in r["Instances"]:
        tags = {x["Key"]: x["Value"] for x in i.get("Tags", [])}
        fila("ec2:instance", i["InstanceId"], tags, tags.get("Name", ""))

for u in c("iam").list_users()["Users"]:
    fila("iam:user", u["UserName"], {})

w = csv.writer(sys.stdout, lineterminator="\n")
w.writerow(["tipo", "recurso", "dueno", "clasificacion", "etiquetas_faltantes", "nota"])
w.writerows(filas)
```

```bash
python inventario.py | tee inventario-antes.csv
```

Salida real (el ID de la instancia cambia en cada corrida):

```
tipo,recurso,dueno,clasificacion,etiquetas_faltantes,nota
s3:bucket,lab-datos,equipo-api,confidencial,,bloqueo-publico=completo
s3:bucket,lab-logs,,,"taller:owner,taller:clasificacion",bloqueo-publico=NINGUNO
s3:bucket,lab-prueba-juan,,,"taller:owner,taller:clasificacion",bloqueo-publico=NINGUNO
ec2:instance,i-2927a7114d0cbabb6,equipo-api,,taller:clasificacion,api-lab
iam:user,app,,,"taller:owner,taller:clasificacion",
iam:user,juan,,,"taller:owner,taller:clasificacion",
```

**Qué leer aquí:** cuatro de seis filas incumplen la convención. Dos buckets no tienen dueño **ni** bloqueo de acceso público. Esa combinación es la que se convierte en incidente.

> Nota sobre los usuarios IAM: este script no los etiqueta porque IAM etiqueta distinto; en el módulo 2 los revisas con el reporte de credenciales. Aquí solo quedan marcados como "sin dueño".

---

## Paso 3 - Verlo fallar

Vas a hacer que un bucket quede público por una política, y a medir cómo lo ve la API. Guarda como `politica_publica.py`:

```python
import boto3, json
EP = "http://127.0.0.1:5011"
s3 = boto3.client("s3", endpoint_url=EP, region_name="us-east-1",
                  aws_access_key_id="test", aws_secret_access_key="test")
pol = {"Version": "2012-10-17", "Statement": [{"Sid": "TodosLeen", "Effect": "Allow",
       "Principal": "*", "Action": "s3:GetObject", "Resource": "arn:aws:s3:::lab-logs/*"}]}
s3.put_bucket_policy(Bucket="lab-logs", Policy=json.dumps(pol))
print("estado:", s3.get_bucket_policy_status(Bucket="lab-logs")["PolicyStatus"])
```

```bash
python politica_publica.py
```

Salida real:

```
estado: {'IsPublic': True}
```

`"Principal": "*"` sin condiciones significa "cualquiera". Es la forma más común del hallazgo, y la API lo marca `IsPublic: True`.

> **Dónde el simulador difiere de AWS.** Con `BlockPublicPolicy` activo, **AWS rechazaría** este `put_bucket_policy` (documentación de S3, "Block public access settings"). Moto no modela ese rechazo: acepta la política. Por eso en la corrección se **borra** la política además de activar el bloqueo. En AWS real el orden ideal es el contrario: bloqueo primero, y el intento de política pública falla antes de existir.

---

## Paso 4 - Corregir

Guarda como `corregir.py`:

```python
import boto3
from botocore.exceptions import ClientError
EP = "http://127.0.0.1:5011"
s3 = boto3.client("s3", endpoint_url=EP, region_name="us-east-1",
                  aws_access_key_id="test", aws_secret_access_key="test")
s3.delete_bucket_policy(Bucket="lab-logs")          # quita la declaración pública
for b in ("lab-logs", "lab-prueba-juan"):
    s3.put_public_access_block(Bucket=b, PublicAccessBlockConfiguration={
        "BlockPublicAcls": True, "IgnorePublicAcls": True,
        "BlockPublicPolicy": True, "RestrictPublicBuckets": True})
    s3.put_bucket_tagging(Bucket=b, Tagging={"TagSet": [
        {"Key": "taller:owner", "Value": "equipo-api"},
        {"Key": "taller:clasificacion", "Value": "interna"}]})
try:
    s3.get_bucket_policy_status(Bucket="lab-logs")
except ClientError as e:
    print("lab-logs:", e.response["Error"]["Code"])
```

```bash
python corregir.py
```

Salida real:

```
lab-logs: NoSuchBucketPolicy
```

Sin política no hay política pública que evaluar. Decidir que `lab-logs` es "interna" es **tu criterio**, no del script: en un caso real, pregúntale al dueño.

---

## Paso 5 - Verificar con una segunda medición

```bash
python inventario.py | tee inventario-despues.csv
diff inventario-antes.csv inventario-despues.csv
```

Los dos buckets deben aparecer con dueño, clasificación y `bloqueo-publico=completo`. Si el `diff` no muestra esos cambios, la corrección no se aplicó: no te quedes con "el script dijo corregido".

Quedan dos filas pendientes que el script **no** corrigió: la instancia sin clasificación y los usuarios sin dueño. Son parte de la tarea.

---

## Paso 6 - La pregunta de responsabilidad

Para cada recurso del inventario, completa en tu bitácora:

| Recurso | Servicio | ¿Qué parte es del proveedor? | ¿Qué parte es nuestra? |
|---|---|---|---|
| `lab-datos` | Almacenamiento de objetos | Disponibilidad y seguridad física del servicio | Permisos, cifrado, datos, bloqueo público |
| la instancia | Máquina virtual | Hosts físicos y virtualización | Sistema operativo, parches, firewall, aplicación |
| ... | | | |

Usa la tabla de la clase y la fuente oficial de cada nube. Si un servicio no está en la tabla, **búscalo** en la documentación.

---

## Limpieza

Detén el simulador con `Ctrl+C` en su terminal. No hay nada más que borrar: todo vivía en la memoria del proceso. Si no lo vas a usar de nuevo, `rm -rf ~/lab-nube` borra el entorno.

---

## Lo que debiste ver

- [ ] Un inventario con 6 filas y 4 incumplimientos de etiquetas
- [ ] `IsPublic: True` antes de la corrección
- [ ] El segundo inventario con los dos buckets corregidos y un `diff` que lo muestra
- [ ] Tu tabla de responsabilidades por recurso
