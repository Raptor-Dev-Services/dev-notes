<!-- _class: lead -->

# M03 - Red y segmentación

### Seguridad en la nube y Zero Trust

**Rogelio Arriaga - Raptor Dev Services**

<!--
Módulo 3 de 6 (2 h). Hoy la base de datos del Taller Demo deja de ser alcanzable por quien no debe.
Antes de empezar: cada quien necesita el venv del curso con moto, boto3 y checkov (la práctica lo arma en el paso 0). Nada de hoy toca una cuenta de nube real ni cuesta dinero.
-->

---

## La idea de hoy

> Una base de datos con IP pública es un incidente que todavía no pasa.

- La red no autentica a nadie, pero decide **quién puede llegar a tocar la puerta**
- Cada regla de más es superficie de ataque; cada regla de menos es una caída
- Una mala configuración de red se explica, se detecta, se corrige y **se verifica desde fuera**

<!-- Pregunta de arranque: ¿cuántos puertos de su carga están abiertos a "0.0.0.0/0" ahora mismo? Que apuesten un número antes de medir. -->

---

## Agenda (2 h)

| Bloque | Tema | Min |
|---|---|---|
| 1 | La red como una capa más (defensa en profundidad) | 10 |
| 2 | VPC, subredes públicas y privadas | 10 |
| 3 | Grupos de seguridad: qué son y cómo se rompen | 25 |
| 4 | NACL: sin estado y con orden | 15 |
| 5 | Tráfico de salida y endpoints privados | 15 |
| 6 | Acceso administrativo sin puertos abiertos | 10 |
| 7 | Registro del tráfico y detección automática | 15 |
| 8 | Azure: los mismos conceptos con otros nombres | 10 |
| 9 | Práctica y cierre | (práctica aparte) |

---

## De dónde venimos

- **M1:** qué le toca proteger a tu equipo y qué al proveedor; el inventario de activos
- **M2:** identidades y mínimo privilegio; credenciales de corta vida
- **Hoy:** que lo que quedó inventariado solo sea **alcanzable** por lo necesario

Hoy trabajamos sobre el **Caso hipotético: Taller Demo**, una carga ficticia: `api-lab` (API en .NET), `db-lab` (PostgreSQL) y el bucket `lab-datos`, en la cuenta de laboratorio `111122223333`, región `us-east-1`.

Lo de infraestructura como código (Terraform, CloudFormation) se trata a fondo en `/infrastructure-as-code/`; aquí lo usamos solo para **revisar** la red.

---

## Defensa en profundidad

Ningún control es suficiente solo. La red es **una** capa:

| Capa | Pregunta | Dónde se vio |
|---|---|---|
| Identidad | ¿Quién eres y qué puedes hacer? | M2 |
| **Red** | **¿Puedes siquiera llegar?** | **M3 (hoy)** |
| Datos | ¿Está cifrado lo que llega? | M1 y nota 22 |
| Detección | ¿Lo vimos pasar? | M5 |

Fuente: *Practical Cloud Security* (Dotson, 2023), cap. 1 (defensa en profundidad) y cap. 6 (seguridad de red).

<!-- Idea para el grupo: si el atacante pasa la red, ¿qué lo detiene? Que ese "qué" sea otra capa y no la misma regla repetida. -->

---

<!-- _class: lead -->

# Parte 1
## VPC y subredes

---

## La red del Taller Demo

```
Internet
   |
[ Balanceador (ALB) ]        subred pública     10.20.1.0/24
   |  443 desde el mundo
[ api-lab :8080 ]            subred de app      10.20.11.0/24
   |  5432 solo desde api-lab
[ db-lab :5432 ]             subred de datos    10.20.21.0/24
```

- Un **solo** punto de entrada desde Internet
- Cada salto **nombra** quién puede hablarle
- La base **no tiene ruta** hacia Internet en ninguna dirección

Direcciones de documentación y RFC 1918: **Ejemplo ilustrativo**, no una red real.

---

## ¿Qué hace "pública" a una subred?

No es una casilla: es **una ruta** en su tabla de rutas.

| Subred | Ruta `0.0.0.0/0` hacia... | Efecto |
|---|---|---|
| Pública | Internet Gateway | Lo que tenga IP pública puede recibir y enviar |
| Privada con salida | NAT Gateway | Puede **iniciar** conexiones hacia afuera, no recibirlas |
| De datos | (ninguna) | No sale ni entra por Internet |

Detalle de rutas, IGW y NAT: nota `10-cloud/11-aws-vpc-networking.md`. No lo repetimos.

Error típico: poner la base en una subred pública "mientras pruebo". Esa prueba se queda meses.

---

## Cuatro controles, cuatro preguntas

| Control | Se aplica a | Pregunta que contesta |
|---|---|---|
| Tabla de rutas | Subred | ¿Existe camino? |
| **Grupo de seguridad (SG)** | Interfaz de red | ¿Esta interfaz acepta este tráfico? |
| **NACL** | Subred | ¿Pasa este paquete por la frontera de la subred? |
| Endpoint privado | VPC | ¿Llego al servicio sin salir a Internet? |

Los dos de en medio son los que se configuran mal con más frecuencia. Los vemos a fondo.

Fuente: *AWS Certified Advanced Networking - Specialty Exam Guide* (Sluga, 2019), cap. 3 (VPC Network Security).

---

<!-- _class: lead -->

# Parte 2
## Grupos de seguridad

---

## Qué dice la documentación de AWS

Verificado en la documentación oficial a noviembre de 2026:

- Un grupo de seguridad controla el tráfico que **llega y sale** de los recursos asociados
- Es **con estado** (*stateful*): la respuesta a una petición permitida se permite sola
- Solo hay reglas que **permiten**; lo no permitido está denegado
- Un recurso puede tener **varios** grupos de seguridad
- No filtra el tráfico a DNS, DHCP, metadatos de EC2 ni el servicio de hora de Amazon
- No tiene costo adicional

Fuente: docs.aws.amazon.com/vpc/latest/userguide/vpc-security-groups.html

<!-- Insistir en la línea de los metadatos: el SG NO protege el servicio de metadatos de la instancia. Eso se resuelve con la configuración de IMDS (M2), no con la red. -->

---

## El grupo "default" no es inocente

Toda VPC trae un grupo llamado `default`:

| Dirección | Regla |
|---|---|
| Entrada | Todo el tráfico desde **el mismo grupo** |
| Salida | Todo el tráfico a `0.0.0.0/0` (y `::/0` si la VPC tiene IPv6) |

- No se puede borrar (`Client.CannotDelete`), pero sí **cambiar sus reglas**
- Si lanzas un recurso sin indicar grupo, **se le asigna el `default`**

Consecuencia: todo lo que nadie configuró **se habla entre sí sin restricción** y sale a Internet sin restricción.

Fuente: docs.aws.amazon.com/vpc/latest/userguide/default-security-group.html

---

## Referenciar grupos, no rangos

```
sg-alb  : 443  desde 0.0.0.0/0
sg-app  : 8080 desde sg-alb          <- origen: un GRUPO
sg-db   : 5432 desde sg-app          <- origen: un GRUPO
```

| Origen en la regla | Quién entra |
|---|---|
| `10.20.11.0/24` (rango) | Cualquier cosa que viva en esa subred |
| `sg-app` (grupo) | Solo interfaces **con ese grupo** |

La regla sobrevive a cambios de IP, y si una interfaz pierde el grupo, pierde el acceso.

La nota `10-cloud/11-aws-vpc-networking.md` tiene los comandos de `aws ec2`; hoy los reproducimos **sin cuenta de nube**.

---

## Nuestro laboratorio: moto

**moto** es un servidor de código abierto que imita la API de AWS en tu máquina. Con él:

- `boto3` (el SDK real) habla con `http://127.0.0.1:5031` y **no con AWS**
- Cero costo, cero riesgo para una cuenta real
- Los fragmentos de la clase se ejecutaron con **moto 5.2.5 y boto3 1.44.0 (noviembre de 2026)**

Qué **no** hace: no mueve paquetes. Valida **configuración**, no conectividad. Para "no responde desde fuera" usamos, más adelante, una simulación simplificada y el razonamiento de las reglas.

```bash
# Salida real (fragmento de la práctica, paso 1)
VPC vpc-30260a1715f010755 con 3 subredes y 3 grupos de seguridad
```

---

## Construir la red con la mala configuración a propósito

```python {1-4|6-9|11|*}
ec2 = boto3.client("ec2", endpoint_url="http://127.0.0.1:5031",
                   region_name="us-east-1",
                   aws_access_key_id="test", aws_secret_access_key="test")
vpc = ec2.create_vpc(CidrBlock="10.20.0.0/16")["Vpc"]["VpcId"]

sg_db = ec2.create_security_group(GroupName="sg-db", Description="db-lab", VpcId=vpc)["GroupId"]
ec2.authorize_security_group_ingress(GroupId=sg_db, IpPermissions=[
  {"IpProtocol": "tcp", "FromPort": 5432, "ToPort": 5432,
   "IpRanges": [{"CidrIp": "0.0.0.0/0"}]}])      # <- el error

# la API de la aplicacion tambien trae su SSH abierto al mundo
```

Fragmento: el script completo y ejecutable está en la práctica. Las claves `test/test` son de moto, no de AWS.

---

## Error típico: abierto al mundo

Por qué pasa:

1. "No conecta, abro `0.0.0.0/0` y luego lo cierro" (y no se cierra)
2. Copiar una regla de un tutorial
3. Nadie es **dueño** de la regla

Qué cuesta: la documentación de AWS recomienda, para 22 (SSH) y 3389 (RDP), **autorizar solo rangos específicos**, porque `0.0.0.0/0` permite el acceso a cualquiera desde cualquier IP.

Fuente: docs.aws.amazon.com/vpc/latest/userguide/vpc-security-groups.html (buenas prácticas).

Cómo se detecta: **con un inventario de reglas**, no mirando la consola de vez en cuando.

---

## Detectar: un auditor de reglas

```python
SENSIBLES = {22: "SSH", 3389: "RDP", 5432: "PostgreSQL", 3306: "MySQL"}
for g in ec2.describe_security_groups()["SecurityGroups"]:
    for p in g["IpPermissions"]:
        for r in p.get("IpRanges", []):
            if r["CidrIp"] == "0.0.0.0/0":
                ...  # cruza el rango de puertos con SENSIBLES
```

Salida real (moto 5.2.5, boto3 1.44.0, noviembre de 2026):

```
HALLAZGO sg-app (sg-326b53c0a9e045f7f): SSH abierto a 0.0.0.0/0 (puertos 22-22)
HALLAZGO sg-db (sg-63b564ca386b92c9a): PostgreSQL abierto a 0.0.0.0/0 (puertos 5432-5432)
hallazgos: 2
salida=1
```

El script **sale con 1** si hay hallazgos: sirve igual en un pipeline.

---

## Corregir y verificar

Corregir = quitar lo abierto **y** poner lo mínimo:

```python
ec2.revoke_security_group_ingress(GroupId=sg_db, IpPermissions=[...0.0.0.0/0...])
ec2.authorize_security_group_ingress(GroupId=sg_db, IpPermissions=[
  {"IpProtocol": "tcp", "FromPort": 5432, "ToPort": 5432,
   "UserIdGroupPairs": [{"GroupId": sg_app}]}])
```

Verificar = correr **el mismo auditor** y ver que ya no encuentra nada:

```
corregido
hallazgos: 0
salida=0
```

Regla del curso: **una corrección sin su verificación es una opinión**.

<!-- Cierre de bloque: el auditor es el "test" de la corrección. Si el arreglo no hace caer el hallazgo, no estaba arreglado. -->

---

## Lo que un SG no ve

| Qué | Por qué importa |
|---|---|
| DNS, DHCP, metadatos | No se filtran con SG ni con NACL |
| Tráfico dentro del mismo grupo si la regla lo permite | `default` se habla a sí mismo |
| Una regla por rango amplio | Aprueba a vecinos que no conoces |
| Reglas de salida abiertas | Lo que se instala puede **llamar a casa** |

Siguiente: la otra frontera, la de la subred.

---

<!-- _class: lead -->

# Parte 3
## NACL

---

## NACL: lo que cambia respecto al SG

| | Grupo de seguridad | NACL |
|---|---|---|
| Se aplica a | Interfaz | Subred |
| Estado | Con estado | **Sin estado** |
| Reglas | Solo permiten | Permiten y **deniegan** |
| Evaluación | Todas | **En orden, la de número menor primero** |
| Final | Denegado | Regla `*` que deniega |

- Número de regla: de 1 a 32766; se detiene en la primera coincidencia
- Se evalúa al **entrar y salir** de la subred, no dentro de ella
- Una subred tiene una sola NACL a la vez; sin asociación explícita usa la `default`

Fuente: docs.aws.amazon.com/vpc/latest/userguide/vpc-network-acls.html

---

## Sin estado: la respuesta es otra regla

Entra una petición al 443. Si solo permitiste la entrada:

```
entrada 443  ->  permitida   (regla 100)
respuesta    ->  ???         (nadie la permitió)
```

El **cliente** elige el puerto efímero de su lado. Los rangos documentados por AWS:

| Cliente | Rango |
|---|---|
| Muchos kernel de Linux (incluido Amazon Linux) | 32768-61000 |
| Windows Server 2008 y posteriores | 49152-65535 |
| NAT Gateway, Lambda, Elastic Load Balancing | 1024-65535 |

Fuente: docs.aws.amazon.com/vpc/latest/userguide/custom-network-acl.html (puertos efímeros).

---

## Verlo con una simulación

Simulación simplificada (Python puro, sin red): reglas numeradas, primera coincidencia gana, final `deny`, **sin estado**. No simula nada más de una NACL real.

```bash
python3 nacl_sim.py
```

Salida real (Python 3, noviembre de 2026):

```
entrada 443 : ('allow', 100)
respuesta sin regla de salida : ('deny', '*')
respuesta con 1024-65535      : ('allow', 100)
con deny 90 al /24 : ('deny', 90)
otro cliente       : ('allow', 100)
```

Lectura: la entrada pasa, **la respuesta muere**; un `deny` con número menor gana al `allow` de 100.

---

## Error típico de NACL

| Síntoma | Causa | Cómo se detecta |
|---|---|---|
| "Entra pero no responde" | Falta la regla de retorno (puertos efímeros) | Registro del tráfico: entrada `ACCEPT`, respuesta `REJECT` |
| "Dejó de funcionar el balanceador" | `deny` a `0.0.0.0/0` o al CIDR de la subred | AWS lo documenta: el balanceador no puede hacer sus comprobaciones |
| "La regla nueva no hace nada" | Número **mayor** que una regla previa que ya coincide | Revisar el orden |

Consejo de AWS: numera de 10 en 10 (o 100 en 100) para poder insertar reglas después.

Cuándo sí usar NACL: un bloqueo explícito de un rango, o una **segunda barrera** donde la norma lo exija. El día a día va en grupos de seguridad.

---

<!-- _class: lead -->

# Parte 4
## Salida y endpoints

---

## El tráfico de salida también es riesgo

El grupo `default` permite **toda** la salida. Un grupo nuevo que creas tiene, por omisión, la misma regla de salida abierta.

Por qué importa: si algo corre dentro de tu red que no debía, **la salida abierta** le permite descargar más herramientas y mandar datos fuera.

| Estrategia | Qué hace |
|---|---|
| Quitar la salida abierta del SG | Solo sale lo que listas |
| Subred de datos sin ruta a Internet | Ni NAT ni IGW |
| Endpoints privados | El tráfico a servicios de AWS no sale a Internet |
| Registro de tráfico | Ves a dónde habla cada interfaz |

Fuente: *Practical Cloud Security* (Dotson, 2023), cap. 6 (filtrado de salida).

---

## VPC endpoints: dos tipos que debes distinguir

| | De puerta de enlace (*gateway*) | De interfaz (PrivateLink) |
|---|---|---|
| Servicios | **Solo S3 y DynamoDB** | Muchos servicios de AWS y de terceros |
| Cómo llega el tráfico | Una **ruta** (lista de prefijos) en la tabla | Una **interfaz de red** con IP privada en tu subred |
| Costo adicional | **Ninguno** | Sí; consulta la página de precios |
| Usa PrivateLink | No | Sí |

Fuente: docs.aws.amazon.com/vpc/latest/privatelink/gateway-endpoints.html y concepts.html

Detalle importante: con un endpoint de puerta de enlace, **las instancias usan el endpoint público** del servicio, por la ruta del endpoint. Tu SG de salida debe permitir la **lista de prefijos** del servicio, y la NACL, sus rangos de IP.

---

## La política del endpoint

La política por omisión de un endpoint **permite todas las acciones, a todos los principales, sobre todos los recursos** a través de él.

Eso significa: un endpoint a S3 sin política deja que tu red hable con **cualquier** bucket de **cualquier** cuenta.

Fragmento (no ejecutado: moto no simula endpoints con política):

```json
{
  "Statement": [{
    "Effect": "Allow",
    "Principal": "*",
    "Action": ["s3:GetObject", "s3:PutObject"],
    "Resource": "arn:aws:s3:::lab-datos/*"
  }]
}
```

Fuente: docs.aws.amazon.com/vpc/latest/privatelink/concepts.html (políticas de endpoint). La forma de la política es la de IAM de recursos; revísala con tu equipo antes de aplicarla.

<!-- Aclarar: la política del endpoint LIMITA lo que se puede hacer a través de él; no sustituye a IAM. Es otra capa. -->

---

## Mismo cuidado en Azure: Private Endpoint

Un *private endpoint* es una interfaz de red con una **IP privada de tu red virtual** que conecta con un servicio de Private Link (Storage, SQL Database, Key Vault, etc.).

Lo que hay que saber:

- Las conexiones solo las inicia **el cliente** que se conecta al endpoint
- Hace falta **DNS** que resuelva el nombre del servicio a esa IP privada
- **Ojo:** el endpoint da una IP privada, pero **no necesariamente restringe el acceso público** del servicio: eso hay que apagarlo aparte

Fuente: learn.microsoft.com/azure/private-link/private-endpoint-overview (consultada a noviembre de 2026).

Error típico: crear el endpoint privado, **dejar el acceso público abierto**, y creer que ya está cerrado.

---

<!-- _class: lead -->

# Parte 5
## Acceso administrativo y registro

---

## Del bastión a ningún puerto abierto

| Modelo | Puerto en el SG | Qué hay que cuidar |
|---|---|---|
| SSH directo | 22 abierto a un rango | Llaves, rangos, rotación |
| Bastión | 22 abierto al bastión | El bastión es un servidor más |
| **Session Manager** (AWS Systems Manager) | **Ninguno de entrada** | Permisos IAM y registro de sesiones |

Según la documentación de AWS, Session Manager da acceso **sin abrir puertos de entrada, sin bastiones y sin administrar llaves SSH**, con control por IAM.

Una limitación que debes saber: **no** se registran los comandos de sesiones por reenvío de puertos o SSH, porque van cifrados dentro del túnel.

Fuente: docs.aws.amazon.com/systems-manager/latest/userguide/session-manager.html (consultada a noviembre de 2026). Avanzaremos esto en M4.

---

## VPC Flow Logs: qué ves y qué no

- Capturan **información del tráfico IP** de las interfaces de red de tu VPC
- Se publican en CloudWatch Logs, Amazon S3 o Amazon Data Firehose
- Se recolectan **fuera de la ruta del tráfico**: no afectan el rendimiento
- **No** capturan el contenido de los paquetes
- Tienen cargo por ingesta y almacenamiento de los registros

Ejemplo de la forma de un registro en formato por omisión (**Ejemplo ilustrativo**, valores ficticios):

```
2 111122223333 eni-0a1b2c3d 198.51.100.7 10.20.21.15 49152 5432 6 12 3040 1731340800 1731340860 REJECT OK
```

Campos: versión, cuenta, interfaz, origen, destino, puerto origen, puerto destino, protocolo (6 = TCP), paquetes, bytes, inicio, fin, acción, estado.

Fuente: docs.aws.amazon.com/AmazonVPC/latest/UserGuide/flow-logs.html

---

## Qué buscar en los Flow Logs

| Pregunta | Señal |
|---|---|
| ¿Alguien de fuera toca la base? | `dstport 5432` con origen fuera de `10.20.0.0/16` |
| ¿Mi regla "cerrada" de verdad cierra? | Intentos `REJECT` hacia ese puerto |
| ¿Algo sale a donde no debe? | Destinos nuevos desde la subred de datos |
| ¿Una NACL se comió la respuesta? | Entrada `ACCEPT` y respuesta `REJECT` |

Los Flow Logs **no sustituyen** a las reglas: demuestran si funcionan. Detección y respuesta a fondo: M5; el curso `/blue-team/` trabaja el análisis de bitácoras.

---

## Azure: los mismos conceptos, otros nombres

| Concepto | AWS | Azure |
|---|---|---|
| Red | VPC | Red virtual (VNet) |
| Filtro por recurso | Security Group | Network Security Group (NSG) |
| Agrupar por aplicación | SG que referencia a otro SG | Application Security Group (ASG) |
| Servicios por nombre | Lista de prefijos | **Service tags** |
| Acceso privado a PaaS | Endpoint de interfaz | Private Endpoint |
| Registro de tráfico | VPC Flow Logs | Flow logs de red virtual |

Cuidado con la correspondencia: un NSG es **con estado**, tiene reglas de **permitir y de denegar**. No es "igual que un SG".

---

## NSG de Azure: datos verificados

Según la documentación de Microsoft (a noviembre de 2026):

- Prioridad de reglas: un número de **100 a 4096**; menor número, se evalúa primero; al coincidir se detiene
- Reglas por defecto (no se pueden quitar, sí anular con prioridades menores):

| Entrada | Prioridad | Efecto |
|---|---|---|
| AllowVNetInBound | 65000 | Permite la red virtual |
| AllowAzureLoadBalancerInBound | 65001 | Permite el balanceador |
| DenyAllInbound | 65500 | Niega el resto |

- **Cambios en reglas aplican a conexiones nuevas**: una sesión SSH abierta sigue viva aunque quites la regla que la permitió
- Los **NSG flow logs se retiran el 30 de septiembre de 2027**; ya no se pueden crear nuevos: la ruta es el flow log de red virtual

Fuente: learn.microsoft.com/azure/virtual-network/network-security-groups-overview

---

## Azure como código, revisado por checkov

Fragmento (no ejecutado en Azure; es un archivo local que **solo se analiza**):

```bicep
resource nsg 'Microsoft.Network/networkSecurityGroups@2023-11-01' = {
  name: 'nsg-db'
  location: 'eastus'
  properties: {
    securityRules: [ {
      name: 'permitir-ssh-a-todos'
      properties: {
        priority: 100, direction: 'Inbound', access: 'Allow', protocol: 'Tcp'
        sourcePortRange: '*', destinationPortRange: '22'
        sourceAddressPrefix: '*', destinationAddressPrefix: '*'
      }
    } ]
  }
}
```

Salida real de `checkov --framework bicep` (checkov 3.2.459, noviembre de 2026):

```
Passed checks: 2, Failed checks: 1, Skipped checks: 0
Check: CKV_AZURE_10: "Ensure that SSH access is restricted from the internet"
	FAILED for resource: Microsoft.Network/networkSecurityGroups.nsg
```

---

<!-- _class: lead -->

# Parte 6
## Que la detección sea automática

---

## Dos capas de detección

| Cuándo | Herramienta | Qué ve |
|---|---|---|
| **Antes** de desplegar | Análisis de la plantilla (checkov, cfn-lint) | Lo que dice el código |
| **Después** de desplegar | AWS Config, Azure Policy | Lo que **existe de verdad** |

Reglas administradas de AWS Config relevantes hoy:

| Regla | Identificador | Marca incumplimiento si... |
|---|---|---|
| `restricted-ssh` | `INCOMING_SSH_DISABLED` | Un SG acepta SSH desde `0.0.0.0/0` o `::/0` |
| `rds-instance-public-access-check` | `RDS_INSTANCE_PUBLIC_ACCESS_CHECK` | `publiclyAccessible` es verdadero |

Nota de la documentación: de `restricted-ssh`, el **nombre** de la regla y su **identificador** son distintos.

Fuente: docs.aws.amazon.com/config/latest/developerguide/restricted-ssh.html y rds-instance-public-access-check.html

---

## Revisar la plantilla antes de que exista

Una base marcada `publicly_accessible = true` en un archivo de Terraform del Taller Demo:

```
Check: CKV_AWS_17: "Ensure all data stored in RDS is not publicly accessible"
	FAILED for resource: aws_db_instance.lab
	File: /red.tf:19-25
```

Corregida (`false` y el SG de la base con origen en el grupo de la API):

```
Passed checks: 9, Failed checks: 0, Skipped checks: 0
exit=0
```

Salida real de `checkov -c CKV_AWS_17,CKV_AWS_24,CKV_AWS_25,CKV_AWS_260,CKV_AWS_23` (checkov 3.2.459, noviembre de 2026).

---

## Lo que las reglas listas **no** cubren

Con el mismo archivo, checkov **no** marcó el `0.0.0.0/0` al puerto **5432** del grupo de seguridad con las cinco reglas predefinidas que ejecutamos (`CKV_AWS_17`, `23`, `24`, `25` y `260`): ninguna mira el 5432.

Solución: **escribe tu regla**. Checkov acepta políticas propias en YAML:

```yaml
metadata:
  id: "CKV2_LAB_001"
  name: "Ninguna regla de entrada de un grupo de seguridad usa 0.0.0.0/0"
definition:
  and:
    - cond_type: attribute
      resource_types:
        - aws_security_group
      attribute: ingress
      operator: not_contains
      value: "0.0.0.0/0"
```

Resultado: la plantilla mala falla (`aws_security_group.db`) y la corregida pasa (`Passed checks: 2`).

<!-- Mensaje: una herramienta no es tu política. La política es tuya; la herramienta la ejecuta. En el M06 lo convertimos en cumplimiento como código. -->

---

## Segmentación: para qué

> Si algo se compromete, ¿hasta dónde llega?

| Nivel | Ejemplo | Límite |
|---|---|---|
| Entre cuentas / suscripciones | Producción y laboratorio separados | Un error en una no toca la otra |
| Entre redes | VPC distinta por entorno | No hay ruta entre ellas por omisión |
| Entre subredes | App y datos separadas | SG y NACL en la frontera |
| Entre cargas | SG por función | Cada carga habla solo con sus pares |

Fuentes: *Defensive Security Handbook* (Berlin et al., 2024), cap. 17 (segmentación); *Cloud Native Security Cookbook* (Armitage, 2022), cap. 5 (redes seguras, recetas 5.2 y 5.3).

La micro-segmentación basada en identidad es el puente a **M4: Zero Trust**.

---

## Mapa de errores de red

| Mala configuración | Por qué ocurre | Detección | Corrección | Verificación |
|---|---|---|---|---|
| SSH/RDP/BD abiertos a `0.0.0.0/0` | Prueba que se queda | Auditor de SG, Config `restricted-ssh` | Rango acotado o SG origen | El auditor sale con 0 |
| Base con IP pública | Valor por omisión del servicio o copia | `CKV_AWS_17`, Config RDS | `publicly_accessible = false`, subred de datos | Plantilla pasa; sin ruta a IGW |
| SG `default` en uso | No se indicó grupo | Inventario de asociaciones | Grupo por función; vaciar `default` | Nadie usa `default` |
| NACL sin retorno | Sin estado | Flow Logs `ACCEPT/REJECT` | Regla de puertos efímeros | Respuesta vuelve a `ACCEPT` |
| Endpoint sin política | Valor por omisión permite todo | Revisión de políticas | Política acotada al recurso | Un recurso ajeno es denegado |
| PaaS con acceso público tras Private Endpoint | Se asume que el endpoint lo cierra | Revisión del servicio | Desactivar acceso público | Falla desde fuera |

---

## Lo que NO hacemos en este curso

- **No** probamos ataques contra redes de nadie: el alcance es defensa y verificación de **tu** configuración
- **No** usamos cuentas de nube reales para romper cosas
- Todo lo ejecutado corre en tu máquina contra moto o sobre archivos locales; lo que no corrimos (Azure, endpoints con política) va marcado como **fragmento**

Si tu organización te autoriza a probar una red real, eso es **otro marco**: autorización escrita, alcance y ventana. El curso `/pentesting-with-kali/` trata ese marco legal y ético.

---

## Resumen

1. Una subred es pública por **su ruta**, no por su nombre
2. SG: con estado, solo permite, se referencia **por grupo**
3. NACL: sin estado, ordenada, con `deny`; los puertos efímeros son la trampa
4. La salida abierta es riesgo: **cierra la salida**, usa endpoints
5. Acceso administrativo **sin puertos de entrada**
6. Se **detecta** con auditor propio, Flow Logs y reglas de Config; se **verifica** repitiendo la detección
7. En Azure: NSG (con estado, prioridades 100-4096), Private Endpoint y **desactivar el acceso público**

---

## Práctica y tarea

**Práctica (en clase):** la red del Taller Demo en moto: construirla mal, detectarla, corregirla y verificar; revisar plantillas con checkov; y **verlo fallar** con la NACL sin retorno.

**Tarea:** inventario de reglas de una red (la tuya o la del laboratorio) con dueño, justificación y plan de cierre.

**Libros del módulo**

- *Practical Cloud Security* (Dotson, 2023), cap. 6
- *Zero Trust Networks* (Rais et al., 2024), cap. 8
- *Cloud Native Security Cookbook* (Armitage, 2022), cap. 5
- *AWS Certified Advanced Networking - Specialty Exam Guide* (Sluga, 2019), caps. 2 y 3
- *Security and Microservice Architecture on AWS* (Raje, 2021), cap. 5
- *Defensive Security Handbook* (Berlin et al., 2024), caps. 16 y 17
- *Attack Surface Management* (Eddings y Kaufmann, 2025), caps. 2 y 9

**Siguiente:** M04, Zero Trust.
