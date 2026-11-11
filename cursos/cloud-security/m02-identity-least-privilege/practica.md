# Práctica - M02 - Tres identidades, un bucket

**Seguridad en la nube y Zero Trust**
**Duración estimada:** 75 a 90 minutos

---

## Objetivo

Demostrar con código que una identidad con permisos amplios puede hacer más de lo que necesita, que una de mínimo privilegio no, y que un `Deny` explícito anula un `Allow`. Después, detectar una política demasiado amplia **antes** de desplegarla, y leer un reporte de credenciales como defensor.

> **Laboratorio simulado.** Usa **moto**, que imita la API de AWS en tu máquina; no toca ninguna cuenta real ni cuesta. Moto documenta su control de acceso como "muy básico": sirve para ver negar una acción por política, **no** reemplaza la evaluación completa de AWS (SCP, límites de permisos, condiciones, políticas de recurso). Lo que entrenas es el método: escribir, probar que niega, medir.

Versiones (noviembre de 2026): Python 3.14, moto 5.2.5, boto3 1.44.0, checkov 3.2.459, cfn-lint 1.60.0.

---

## Antes de empezar

```bash
python3 -m venv ~/lab-nube && source ~/lab-nube/bin/activate     # si ya lo hiciste en M01, solo el source
pip install "moto[server]==5.2.5" "boto3==1.44.0" "checkov==3.2.459" "cfn-lint==1.60.0"
mkdir -p ~/lab-nube/m02 && cd ~/lab-nube/m02
```

---

## Paso 1 - Levantar la nube con control de acceso

Moto no autentica nada a menos que se lo pidas. La variable `INITIAL_NO_AUTH_ACTION_COUNT` dice **cuántas llamadas deja pasar sin autenticar**; a partir de la siguiente, evalúa las políticas de la identidad que firma. Sirve para que el preparativo (crear usuarios y políticas) corra libre y las pruebas ya estén bajo control.

En una segunda terminal (mismo `source`):

```bash
INITIAL_NO_AUTH_ACTION_COUNT=15 moto_server -p 5021
```

El 15 sale de contar las llamadas del preparativo del paso 2 (3 de datos y 4 por cada una de las 3 identidades). Si cambias el script, cambia el número.

---

## Paso 2 - Preparar tres identidades

Guarda como `preparar.py`. Las tres políticas son del **Caso hipotético Taller Demo**:

```python
import json, boto3
EP = "http://127.0.0.1:5021"
def cl(svc, ak="test", sk="test"):
    return boto3.client(svc, endpoint_url=EP, region_name="us-east-1",
                        aws_access_key_id=ak, aws_secret_access_key=sk)
iam, s3 = cl("iam"), cl("s3")
n = 0
def paso(f, **kw):
    global n; n += 1; return f(**kw)

paso(s3.create_bucket, Bucket="lab-datos")
paso(s3.put_object, Bucket="lab-datos", Key="clientes/ana.txt", Body=b"datos de ana")
paso(s3.put_object, Bucket="lab-datos", Key="clientes/luis.txt", Body=b"datos de luis")

AMPLIA = {"Version": "2012-10-17", "Statement": [
    {"Effect": "Allow", "Action": "s3:*", "Resource": "*"}]}
MINIMA = {"Version": "2012-10-17", "Statement": [
    {"Effect": "Allow", "Action": ["s3:GetObject", "s3:PutObject"],
     "Resource": "arn:aws:s3:::lab-datos/clientes/*"}]}
CON_DENY = {"Version": "2012-10-17", "Statement": [
    {"Effect": "Allow", "Action": "s3:*", "Resource": "*"},
    {"Effect": "Deny", "Action": "s3:DeleteObject", "Resource": "*"}]}

llaves = {}
for usuario, pol in (("app-amplia", AMPLIA), ("app-minima", MINIMA), ("app-con-deny", CON_DENY)):
    paso(iam.create_user, UserName=usuario)
    p = paso(iam.create_policy, PolicyName=f"pol-{usuario}", PolicyDocument=json.dumps(pol))
    paso(iam.attach_user_policy, UserName=usuario, PolicyArn=p["Policy"]["Arn"])
    k = paso(iam.create_access_key, UserName=usuario)["AccessKey"]
    llaves[usuario] = (k["AccessKeyId"], k["SecretAccessKey"])
json.dump(llaves, open("llaves.json", "w"))
print("llamadas sin autenticar:", n)
```

```bash
python preparar.py
```

Salida real:

```
llamadas sin autenticar: 15
```

> `llaves.json` guarda llaves **del simulador**. En la vida real una llave en un archivo plano es justo el problema de la clase; aquí es aceptable porque no abre nada.

---

## Paso 3 - Verlo fallar (y verlo no fallar)

Guarda como `probar.py`. El código es el **mismo** para las tres identidades:

```python
import json, boto3
from botocore.exceptions import ClientError
EP = "http://127.0.0.1:5021"
llaves = json.load(open("llaves.json"))
def intento(s3, accion):
    try:
        accion(s3); return "permitido"
    except ClientError as e:
        return "NEGADO (" + e.response["Error"]["Code"] + ")"
acciones = {
  "leer":   lambda s3: s3.get_object(Bucket="lab-datos", Key="clientes/ana.txt"),
  "escribir": lambda s3: s3.put_object(Bucket="lab-datos", Key="clientes/nuevo.txt", Body=b"x"),
  "borrar": lambda s3: s3.delete_object(Bucket="lab-datos", Key="clientes/luis.txt"),
  "listar el bucket": lambda s3: s3.list_objects_v2(Bucket="lab-datos"),
}
for usuario, (ak, sk) in llaves.items():
    s3 = boto3.client("s3", endpoint_url=EP, region_name="us-east-1",
                      aws_access_key_id=ak, aws_secret_access_key=sk)
    print(f"{usuario}:")
    for nombre, f in acciones.items():
        print(f"  {nombre:<18}", intento(s3, f))
```

```bash
python probar.py
```

Salida real (moto 5.2.5, noviembre de 2026):

```
app-amplia:
  leer               permitido
  escribir           permitido
  borrar             permitido
  listar el bucket   permitido
app-minima:
  leer               permitido
  escribir           permitido
  borrar             NEGADO (AccessDenied)
  listar el bucket   NEGADO (AccessDenied)
app-con-deny:
  leer               permitido
  escribir           permitido
  borrar             NEGADO (AccessDenied)
  listar el bucket   permitido
```

**Léelo como defensor:**

1. `app-amplia` puede **borrar**. Si su llave se filtra, quien la tenga borra los datos de clientes. Es la política del «ya funciona, déjala».
2. `app-minima` no puede borrar ni listar: la aplicación solo necesita leer y escribir en `clientes/`. Si en producción necesitara listar, lo **sabrías** porque fallaría, y agregarías `s3:ListBucket` sobre el bucket con una condición de prefijo; no `s3:*`.
3. `app-con-deny` muestra que un `Deny` explícito gana a un `Allow`: puede listar pero no borrar.

**Pregunta de la práctica:** ¿cuál de las tres elegirías para `api-lab`? Escribe en una línea por qué.

---

## Paso 4 - Detectarlo antes de desplegar

Mismo problema, pero en una plantilla de CloudFormation. Guarda como `rol.yaml` (ejemplo ilustrativo, de la carga api-lab):

```yaml
AWSTemplateFormatVersion: "2010-09-09"
Description: Rol de la API del Taller Demo (ejemplo ilustrativo)
Resources:
  RolApi:
    Type: AWS::IAM::Role
    Properties:
      RoleName: api-lab
      AssumeRolePolicyDocument:
        Version: "2012-10-17"
        Statement:
          - Effect: Allow
            Principal: { Service: ec2.amazonaws.com }
            Action: sts:AssumeRole
      Policies:
        - PolicyName: acceso-datos
          PolicyDocument:
            Version: "2012-10-17"
            Statement:
              - Effect: Allow
                Action: "*"
                Resource: "*"
```

```bash
cfn-lint rol.yaml; echo "cfn-lint salió con $?"
checkov -f rol.yaml --framework cloudformation --compact --quiet
```

Salida real:

```
cfn-lint salió con 0
cloudformation scan results:

Passed checks: 2, Failed checks: 7
```

Y entre los fallos, entre otros, `CKV_AWS_62` (privilegios administrativos completos `*-*`) y `CKV_AWS_63` (`"*"` como acción).

**Observa esto:** `cfn-lint` sale **0**. La plantilla es válida, está bien escrita y es un desastre de seguridad. Un validador de sintaxis y un revisor de seguridad responden preguntas distintas.

---

## Paso 5 - Corregir y verificar

Cambia la política a lo mínimo y vuelve a medir:

```bash
sed -e 's/Action: "\*"/Action: [s3:GetObject, s3:PutObject]/' \
    -e 's#Resource: "\*"#Resource: "arn:aws:s3:::lab-datos/clientes/*"#' rol.yaml > rol-minimo.yaml
cfn-lint rol-minimo.yaml; echo "cfn-lint salió con $?"
checkov -f rol-minimo.yaml --framework cloudformation --compact --quiet
```

Salida real:

```
cfn-lint salió con 0
cloudformation scan results:

Passed checks: 9, Failed checks: 0
```

De 7 fallos a 0, sin cambiar nada más. **Pero** checkov no sabe si `GetObject` y `PutObject` sobre `clientes/*` es lo que tu API necesita: solo sabe que ya no es `*`. Que sea suficiente lo comprueban las pruebas de la aplicación (si falta algo, falla con `AccessDenied`, que es lo que quieres ver en pruebas y no en producción).

---

## Paso 6 - Reporte de credenciales

Detén el simulador del paso 1 y levanta otro **sin** control de acceso (para poder crear usuarios libremente):

```bash
moto_server -p 5022
```

Guarda como `reporte.py`:

```python
import boto3, csv, io
EP = "http://127.0.0.1:5022"
iam = boto3.client("iam", endpoint_url=EP, region_name="us-east-1",
                   aws_access_key_id="test", aws_secret_access_key="test")
iam.create_user(UserName="juan")
iam.create_access_key(UserName="juan")
iam.create_user(UserName="ana")
iam.create_login_profile(UserName="ana", Password="Contrasena-de-Laboratorio-1!")
iam.generate_credential_report()
rep = iam.get_credential_report()["Content"].decode()
for f in csv.DictReader(io.StringIO(rep)):
    print(f["user"], "| mfa:", f["mfa_active"], "| password:", f["password_enabled"],
          "| llave1:", f["access_key_1_active"])
```

```bash
python reporte.py
```

Salida real:

```
juan | mfa: false | password: false | llave1: true
ana | mfa: false | password: true | llave1: false
```

Para cada fila, decide: ¿cuál es el riesgo? ¿Quién es el dueño (M1)? ¿Qué lo corrige? (`ana`: MFA o, mejor, federación; `juan`: ¿para qué sirve esa llave? ¿puede ser un rol?). En una cuenta real el reporte trae más columnas; aquí solo ves las que el simulador llena.

---

## Paso 7 - Para Azure (fragmento, no se ejecuta)

El equivalente de la política mínima es una asignación de rol con **ámbito** acotado. Fragmento de Azure CLI (no ejecutado; requiere suscripción de Azure; los valores son marcadores):

```bash
# Fragmento (no ejecutado): Storage Blob Data Contributor solo sobre UN contenedor
az role assignment create \
  --assignee-object-id <ID_DE_LA_IDENTIDAD_ADMINISTRADA> \
  --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Contributor" \
  --scope "/subscriptions/<SUSCRIPCION>/resourceGroups/rg-lab/providers/Microsoft.Storage/storageAccounts/<CUENTA>/blobServices/default/containers/clientes"
```

Pregunta: ¿qué pasaría con el alcance de esa identidad si el `--scope` fuera la suscripción? (Pista: azure RBAC es aditivo y los ámbitos se heredan hacia abajo.)

---

## Limpieza

`Ctrl+C` en los simuladores. Borra `llaves.json` (son llaves del simulador, pero la costumbre se entrena): `rm llaves.json`.

---

## Lo que debiste ver

- [ ] Las tres identidades dan resultados distintos con el mismo código
- [ ] `app-amplia` puede borrar; `app-minima` no
- [ ] `cfn-lint` en 0 y checkov con fallos sobre la misma plantilla; luego 9 chequeos pasados
- [ ] Un reporte de credenciales y tu lectura de cada fila
