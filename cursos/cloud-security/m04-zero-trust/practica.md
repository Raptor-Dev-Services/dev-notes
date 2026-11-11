# Práctica - M04 - Un motor de decisiones Zero Trust, de la VPN a la identidad

**Seguridad en la nube y Zero Trust**
**Duración estimada:** 2 horas

---

## Objetivo

Ver con tus propios archivos la diferencia entre decidir por **ubicación** y decidir por **identidad, equipo y contexto**: correr un motor de decisiones (PE, PA y PEP de la norma NIST SP 800-207), verlo negar diez casos, **quitarle un control y comprobar que su prueba cae**, repetir en moto el caso del `Deny` explícito de IAM y consultar la bitácora para contestar "¿quién entró y por qué?".

> **Alcance.** Todo es local y sin costo. El motor es una **simulación simplificada**: no hay red, TLS ni identidad real, y la clave de firma es ficticia. Los nombres (ana, luis) y los servicios son **ficticios** (Caso hipotético: Taller Demo).

---

## Antes de empezar

- Python 3 reciente (se probó con 3.14, noviembre de 2026)
- El entorno de M03 (`~/lab-cloud/v`, con moto 5.2.5 y boto3 1.44.0). Si no lo tienes:

```bash
mkdir -p ~/lab-cloud/m04 && cd ~/lab-cloud
python3 -m venv v && v/bin/pip install "moto[server]==5.2.5" boto3==1.44.0
```

- Dos terminales: una para moto (pasos 4 y 5) y otra para tus comandos

---

## Paso 1 - Ver la confianza implícita

Guarda como `~/lab-cloud/m04/vpn_lab.py` (**Caso hipotético**: un portátil de soporte infectado, con sesión de VPN activa):

```python
"""El modelo del perimetro: quien llega desde la VPN (10.8.0.0/24) entra a todo."""
import ipaddress
VPN = ipaddress.ip_network("10.8.0.0/24")
SERVICIOS = ["api-lab/catalogo", "api-lab/facturas", "db-lab:5432", "panel-admin"]

def decide_perimetro(ip_origen, servicio):
    return ipaddress.ip_address(ip_origen) in VPN

# Un portatil comprometido que ya esta dentro de la VPN (Caso hipotetico)
for s in SERVICIOS:
    print(f"{s:18} -> {'PERMITIDO' if decide_perimetro('10.8.0.23', s) else 'denegado'}")
```

```bash
cd ~/lab-cloud/m04 && python3 vpn_lab.py
```

Salida real:

```
api-lab/catalogo   -> PERMITIDO
api-lab/facturas   -> PERMITIDO
db-lab:5432        -> PERMITIDO
panel-admin        -> PERMITIDO
```

Escribe en tu bitácora **tres cosas que la decisión no supo** (por ejemplo, quién usa el equipo, si está parchado, si necesitaba `panel-admin`).

---

## Paso 2 - El motor de decisiones

Guarda como `zt_lab.py`:

```python
"""Simulacion simplificada de una arquitectura Zero Trust (NIST SP 800-207, Fig. 2).
PE decide, PA abre o cierra la sesion, PEP la hace cumplir. Sin red real: son objetos Python."""
import hmac, hashlib, json, time, base64

CLAVE = b"clave-ficticia-de-laboratorio"      # Ejemplo ilustrativo; nunca una clave real

POLITICA = {                                  # politica como dato, no como if sueltos
    "api-lab/facturas": {"sensibilidad": "alta", "mfa": True, "dispositivo_sano": True, "horario": (7, 22)},
    "api-lab/catalogo": {"sensibilidad": "baja", "mfa": False, "dispositivo_sano": True, "horario": (0, 24)},
}
DIRECTORIO = {"ana": {"grupos": {"facturacion"}}, "luis": {"grupos": {"soporte"}}}
ACCESOS = {"facturacion": {"api-lab/facturas", "api-lab/catalogo"}, "soporte": {"api-lab/catalogo"}}
REVOCADOS = set()
BITACORA = []

def registra(**campos):
    BITACORA.append(json.dumps(campos, sort_keys=True))

class PolicyEngine:
    def decide(self, sujeto, recurso, contexto):
        regla = POLITICA.get(recurso)
        if regla is None:
            return False, "recurso sin politica (denegar por omision)"
        if sujeto in REVOCADOS:
            return False, "identidad revocada"
        grupos = DIRECTORIO.get(sujeto, {}).get("grupos", set())
        if not any(recurso in ACCESOS.get(g, set()) for g in grupos):
            return False, "la identidad no tiene este recurso"
        if regla["mfa"] and not contexto["mfa"]:
            return False, "falta MFA"
        if regla["dispositivo_sano"] and not contexto["dispositivo_sano"]:
            return False, "dispositivo no cumple la postura"
        h = contexto["hora"]
        if not (regla["horario"][0] <= h < regla["horario"][1]):
            return False, "fuera de horario"
        return True, "cumple la politica"            # NOTA: la red de origen no cuenta

class PolicyAdministrator:
    def __init__(self, pe, ttl=300):
        self.pe, self.ttl = pe, ttl
    def abre_sesion(self, sujeto, recurso, contexto, ahora):
        ok, motivo = self.pe.decide(sujeto, recurso, contexto)
        registra(evento="decision", sujeto=sujeto, recurso=recurso, permitido=ok, motivo=motivo, t=ahora)
        if not ok:
            return None
        cuerpo = base64.urlsafe_b64encode(json.dumps({"s": sujeto, "r": recurso, "exp": ahora + self.ttl}).encode()).decode()
        firma = hmac.new(CLAVE, cuerpo.encode(), hashlib.sha256).hexdigest()
        return f"{cuerpo}.{firma}"

class PEP:
    def __init__(self, pe):
        self.pe = pe
    def acceso(self, ficha, recurso, contexto, ahora):
        try:
            cuerpo, firma = ficha.rsplit(".", 1)
            if not hmac.compare_digest(firma, hmac.new(CLAVE, cuerpo.encode(), hashlib.sha256).hexdigest()):
                raise ValueError("firma")
            datos = json.loads(base64.urlsafe_b64decode(cuerpo))
        except Exception:
            registra(evento="pep", permitido=False, motivo="ficha invalida", recurso=recurso, t=ahora)
            return False
        if datos["r"] != recurso or ahora >= datos["exp"]:
            registra(evento="pep", sujeto=datos["s"], permitido=False, motivo="ficha de otro recurso o vencida", recurso=recurso, t=ahora)
            return False
        ok, motivo = self.pe.decide(datos["s"], recurso, contexto)   # se reevalua en cada uso
        registra(evento="pep", sujeto=datos["s"], permitido=ok, motivo=motivo, recurso=recurso, t=ahora)
        return ok

pe = PolicyEngine(); pa = PolicyAdministrator(pe); pep = PEP(pe)
sano = {"mfa": True, "dispositivo_sano": True, "hora": 10}

print("1 ana + MFA + equipo sano      :", bool(pa.abre_sesion("ana", "api-lab/facturas", sano, 0)))
print("2 ana sin MFA                  :", bool(pa.abre_sesion("ana", "api-lab/facturas", {**sano, "mfa": False}, 0)))
print("3 luis (soporte) a facturas    :", bool(pa.abre_sesion("luis", "api-lab/facturas", sano, 0)))
print("4 ana, equipo sin parches      :", bool(pa.abre_sesion("ana", "api-lab/facturas", {**sano, "dispositivo_sano": False}, 0)))
ficha = pa.abre_sesion("ana", "api-lab/facturas", sano, 0)
print("5 uso de la ficha a t=60       :", pep.acceso(ficha, "api-lab/facturas", sano, 60))
print("6 la misma ficha en otro recurso:", pep.acceso(ficha, "api-lab/catalogo", sano, 61))
print("7 el equipo pierde postura a t=90:", pep.acceso(ficha, "api-lab/facturas", {**sano, "dispositivo_sano": False}, 90))
REVOCADOS.add("ana")
print("8 ana revocada, ficha vigente  :", pep.acceso(ficha, "api-lab/facturas", sano, 100))
print("9 ficha vencida (t=400)        :", pep.acceso(ficha, "api-lab/facturas", sano, 400))
print("10 ficha alterada              :", pep.acceso(ficha[:-1] + ("0" if ficha[-1] != "0" else "1"), "api-lab/facturas", sano, 101))
print("\n--- bitacora (una linea JSON por decision) ---")
print("\n".join(BITACORA[:4]))
print("... total de lineas:", len(BITACORA))

# Consulta: quien logro entrar a facturas y quien fue rechazado, y por que
print("\n--- quien entro a facturas ---")
for l in BITACORA:
    r = json.loads(l)
    if r["recurso"] == "api-lab/facturas":
        print(r["t"], r.get("sujeto", "?"), "PERMITIDO" if r["permitido"] else "DENEGADO", "-", r["motivo"])
```

```bash
python3 zt_lab.py
```

Salida real (Python 3, noviembre de 2026):

```
1 ana + MFA + equipo sano      : True
2 ana sin MFA                  : False
3 luis (soporte) a facturas    : False
4 ana, equipo sin parches      : False
5 uso de la ficha a t=60       : True
6 la misma ficha en otro recurso: False
7 el equipo pierde postura a t=90: False
8 ana revocada, ficha vigente  : False
9 ficha vencida (t=400)        : False
10 ficha alterada              : False
```

Para cada línea, responde por escrito: **¿qué componente decidió** (PE, PA o PEP) **y con qué razón**? La razón la tienes en la bitácora que imprime el script.

---

## Paso 3 - Verlo fallar: quita un control

Una prueba que no cae al quitar el control no verifica nada. Copia el script y desactiva la comprobación de MFA:

```bash
sed 's/if regla\["mfa"\] and not contexto\["mfa"\]:/if False and regla["mfa"] and not contexto["mfa"]:/' zt_lab.py > zt_mut.py
python3 zt_mut.py | head -3
```

Salida real:

```
1 ana + MFA + equipo sano      : True
2 ana sin MFA                  : True
3 luis (soporte) a facturas    : False
```

El escenario 2 pasó de `False` a `True`: **tu prueba detectó que el control faltaba**. Ahora repite el ejercicio con otros dos controles a tu elección (el horario y la postura del equipo) y anota qué escenario cambia en cada caso. Si con alguno **no cambia ninguno**, tu conjunto de escenarios tiene un hueco: escribe el escenario que falta.

Borra `zt_mut.py` al terminar.

---

## Paso 4 - Un Deny explícito en moto

En la terminal 1 (queda en primer plano; `Ctrl+C` al final):

```bash
cd ~/lab-cloud
INITIAL_NO_AUTH_ACTION_COUNT=5 v/bin/moto_server -p 5041
```

`INITIAL_NO_AUTH_ACTION_COUNT` deja pasar sin autenticar las primeras 5 llamadas (las que preparan el laboratorio) y desde la sexta aplica el control de acceso tipo IAM de moto. Guarda como `moto_deny.py`:

```python
import boto3, json
from botocore.exceptions import ClientError
EP="http://127.0.0.1:5041"
def cl(svc, ak="test", sk="test"):
    return boto3.client(svc, endpoint_url=EP, region_name="us-east-1", aws_access_key_id=ak, aws_secret_access_key=sk)
iam=cl("iam"); iam.create_user(UserName="api-lab")
pol={"Version":"2012-10-17","Statement":[
 {"Effect":"Allow","Action":"s3:*","Resource":["arn:aws:s3:::lab-datos","arn:aws:s3:::lab-datos/*"]},
 {"Effect":"Deny","Action":"s3:DeleteObject","Resource":"arn:aws:s3:::lab-datos/*"}]}
p=iam.create_policy(PolicyName="api-lab-s3",PolicyDocument=json.dumps(pol))["Policy"]["Arn"]
iam.attach_user_policy(UserName="api-lab",PolicyArn=p)
k=iam.create_access_key(UserName="api-lab")["AccessKey"]
cl("s3").create_bucket(Bucket="lab-datos")
app=cl("s3",k["AccessKeyId"],k["SecretAccessKey"])
app.put_object(Bucket="lab-datos",Key="f1.txt",Body=b"x"); print("PutObject: permitido")
for accion,fn in [("DeleteObject",lambda: app.delete_object(Bucket="lab-datos",Key="f1.txt")),("ListBuckets",lambda: app.list_buckets())]:
    try: fn(); print(accion+": permitido")
    except ClientError as e: print(accion+":", e.response["Error"]["Code"])
```

```bash
~/lab-cloud/v/bin/python moto_deny.py
```

Salida real (moto 5.2.5, boto3 1.44.0, noviembre de 2026):

```
PutObject: permitido
DeleteObject: AccessDenied
ListBuckets: AccessDenied
```

Responde: ¿por qué `DeleteObject` se niega si la política da `s3:*`? ¿Por qué `ListBuckets` se niega si nadie lo prohibió? (Pista: la evaluación de IAM, en la clase.)

**Verlo fallar.** Quita el `Deny` de la política en el script, vuelve a crear el laboratorio con una moto nueva y comprueba que `DeleteObject` ahora **se permite**. Anota qué te dice esto sobre cuántas veces conviene poner un `Deny` explícito.

Moto no reproduce todas las condiciones de IAM; esta práctica solo muestra el orden de evaluación.

---

## Paso 5 - La bitácora responde

La última parte del script imprime "quién entró a facturas". Salida real:

```
0 ana PERMITIDO - cumple la politica
0 ana DENEGADO - falta MFA
0 luis DENEGADO - la identidad no tiene este recurso
0 ana DENEGADO - dispositivo no cumple la postura
0 ana PERMITIDO - cumple la politica
60 ana PERMITIDO - cumple la politica
90 ana DENEGADO - dispositivo no cumple la postura
100 ana DENEGADO - identidad revocada
400 ana DENEGADO - ficha de otro recurso o vencida
101 ? DENEGADO - ficha invalida
```

Tareas:

1. Escribe una consulta que liste **solo las denegaciones** y las cuente **por motivo**
2. Agrega a cada registro un campo `origen` (una IP ficticia) y comprueba que la **decisión no cambia** con la IP: el motor no mira la red
3. ¿Qué campo agregarías para investigar un incidente? (por ejemplo, el identificador de la ficha)

---

## Paso 6 - Del VPN a la política (en papel)

Elige **una aplicación tuya** (o `api-lab/facturas`) y llena:

| Pregunta | Tu respuesta |
|---|---|
| ¿Quién necesita acceder? (roles, no personas) | |
| ¿Qué atributos exiges? (MFA, equipo sano, horario...) | |
| ¿Qué **no** se permite aunque el rol lo tenga? | |
| ¿Cuánto dura una sesión? | |
| ¿Qué registras en cada decisión? | |
| ¿Cómo la pruebas **en modo informe** antes de activarla? | |

Fragmento (no ejecutado): si tu organización usa Azure, el mismo diseño se expresa como una política de Conditional Access (señales: usuario, dispositivo, ubicación, aplicación, riesgo; decisión: bloquear o exigir MFA o dispositivo conforme). Requiere licencias Microsoft Entra ID P1; tampoco se ejecuta aquí.

---

## Paso 7 - Cierre y limpieza

1. Detén moto en la terminal 1 con `Ctrl+C` (o mata su PID)
2. Guarda `vpn_lab.py`, `zt_lab.py`, `moto_deny.py` y tu bitácora en tu repositorio de práctica
3. No dejes procesos vivos

---

## Qué debes llevarte

- La prueba de que decidir por IP deja pasar al portátil comprometido
- Un motor con **denegar por omisión**, fichas cortas por recurso y reevaluación
- La técnica de **quitar un control para ver caer su prueba**
- Una bitácora que contesta "¿quién entró y por qué?"
