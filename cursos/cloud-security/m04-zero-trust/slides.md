<!-- _class: lead -->

# M04 - Zero Trust

### Seguridad en la nube y Zero Trust

**Rogelio Arriaga - Raptor Dev Services**

<!--
Módulo 4 de 6 (2 h). Hoy cambiamos la pregunta: ya no "¿desde dónde llegas?" sino "¿quién eres, con qué equipo y qué necesitas?".
El laboratorio es una simulación en Python puro (sin red, sin nube, sin costo) de los componentes de la norma NIST SP 800-207. Está rotulada como simplificada en cada lámina donde aparece.
-->

---

## La idea de hoy

> Estar dentro de la red no es una credencial.

- Un acceso decidido por **dónde vienes** hereda todos los errores de esa red
- Zero Trust decide **cada acceso** con identidad, estado del equipo y contexto
- No es un producto: es una forma de tomar decisiones, y **se demuestra** con registros

<!-- Pregunta de arranque: ¿qué pasa en tu organización hoy si un portátil con VPN se infecta? Que respondan antes de ver el ejemplo. -->

---

## Agenda (2 h)

| Bloque | Tema | Min |
|---|---|---|
| 1 | El problema del perímetro | 10 |
| 2 | Qué dice NIST SP 800-207 | 20 |
| 3 | Un motor de decisiones que puedes ejecutar | 30 |
| 4 | Autorización en la nube: AWS y Azure | 25 |
| 5 | De la VPN al acceso por identidad | 15 |
| 6 | Errores típicos y qué no es Zero Trust | 10 |
| 7 | Práctica y cierre | (práctica aparte) |

---

## De dónde venimos

- **M2:** identidades, roles y credenciales de corta vida: quién eres
- **M3:** red y segmentación: a qué puedes llegar
- **Hoy:** unir las dos y **decidir cada acceso**, con registro

Seguimos con el **Caso hipotético: Taller Demo** (`api-lab`, `db-lab`, `lab-datos`; cuenta ficticia `111122223333`). Los nombres de personas del ejemplo (ana, luis) son **ficticios**.

Si ya lees `10-cloud/08-zero-trust.md`, esta clase **no lo repite**: lo ejecuta y lo pone junto a la norma.

---

<!-- _class: lead -->

# Parte 1
## El problema del perímetro

---

## El perímetro como credencial

```
Internet ---[ VPN ]--- red interna: todo se habla con todo
                          |-- api-lab/catalogo
                          |-- api-lab/facturas
                          |-- db-lab:5432
                          |-- panel-admin
```

La regla implícita: **"si llegaste por la VPN, eres de confianza"**.

Lo que esa regla no sabe:

- si la persona es quien dice
- si el equipo está parchado o ya comprometido
- si pedir `panel-admin` tiene sentido para ese rol

Fuente: *Zero Trust Networks* (Rais et al., 2024), cap. 1 (fundamentos).

---

## Demostración: un portátil comprometido ya dentro

**Caso hipotético:** un portátil de soporte, infectado, con sesión de VPN activa desde `10.8.0.23`.

```python
VPN = ipaddress.ip_network("10.8.0.0/24")

def decide_perimetro(ip_origen, servicio):
    return ipaddress.ip_address(ip_origen) in VPN
```

Salida real (Python 3, noviembre de 2026):

```
api-lab/catalogo   -> PERMITIDO
api-lab/facturas   -> PERMITIDO
db-lab:5432        -> PERMITIDO
panel-admin        -> PERMITIDO
```

La decisión **solo** miró la IP de origen. No hay forma de saber qué más hizo ese equipo.

---

## Por qué sigue pasando

| Razón | Efecto |
|---|---|
| Las aplicaciones migraron fuera de la red | La red interna ya no contiene lo que hay que proteger |
| Trabajo remoto y equipos personales | "Dentro" ya no significa "controlado" |
| Servicios que se hablan entre sí | La confianza lateral se hereda |
| Confundir autenticar con autorizar | Entrar por la VPN autentica; no dice **a qué** |

La respuesta de la norma NIST es quitar la confianza implícita por ubicación.

---

<!-- _class: lead -->

# Parte 2
## NIST SP 800-207

---

## Qué es

**NIST SP 800-207, "Zero Trust Architecture"**, publicada en **agosto de 2020** (Rose, Borchert, Mitchell y Connelly).

Su idea central, parafraseada de su resumen:

- Zero trust es un conjunto de enfoques que **mueve las defensas de perímetros de red fijos hacia usuarios, activos y recursos**
- **La ubicación en la red o ser dueño del activo no otorga confianza** por sí solos
- La autenticación y la autorización del sujeto y del dispositivo son **pasos separados, antes** de abrir la sesión con un recurso

Fuente: csrc.nist.gov/pubs/sp/800/207/final y el documento (doi.org/10.6028/NIST.SP.800-207).

---

## Los siete principios (tenets)

Sección 2.1 de la norma, en palabras propias:

| # | Principio |
|---|---|
| 1 | Todas las fuentes de datos y servicios de cómputo son **recursos** |
| 2 | Toda comunicación se protege **sin importar la ubicación** en la red |
| 3 | El acceso a cada recurso se concede **por sesión**, con el mínimo privilegio, y no hereda a otros recursos |
| 4 | El acceso lo decide una **política dinámica**: identidad, estado del equipo y la aplicación, y otros atributos de comportamiento y entorno |
| 5 | La organización **mide la integridad y la postura de seguridad** de todos los activos; ninguno es confiable de entrada |
| 6 | La autenticación y la autorización son **dinámicas y se aplican estrictamente antes** de permitir el acceso, con reevaluación continua |
| 7 | La organización **recopila la mayor información posible** sobre el estado de activos, red y comunicaciones, y la usa para mejorar |

La norma aclara que son un ideal: no todos se cumplen al máximo en cada estrategia.

---

## Principios, pilares y modelos: no los mezcles

| Nombre | Quién lo define | Qué es |
|---|---|---|
| Siete **tenets** | NIST SP 800-207 (2020) | Principios de la arquitectura |
| Cinco **pilares** | CISA Zero Trust Maturity Model v2.0 (abril de 2023) | Identidad, Dispositivos, Redes, Aplicaciones y cargas, Datos; con tres capacidades transversales y cuatro etapas de madurez |
| Tres principios guía | Microsoft | Verificar explícitamente, usar mínimo privilegio, asumir brecha |

**Aviso sobre la nota de Dev Notes:** `10-cloud/08-zero-trust.md` presenta cinco "pilares" de **su propia síntesis** (identidad, dispositivo, mínimo privilegio, inspección de tráfico, asumir brecha). **No son** los pilares de CISA ni los siete principios de NIST. Cuando cites, di de quién es cada lista.

Fuentes: NIST SP 800-207; cisa.gov (Zero Trust Maturity Model, versión 2.0); learn.microsoft.com/entra/identity/conditional-access/overview

---

## Arquitectura lógica: quién decide y quién obedece

```
          plano de control (separado del tráfico)
   +-------------------+       +----------------------+
   | Policy Engine     | <---> | Policy Administrator |
   | (PE) decide       |       | (PA) abre o cierra   |
   +-------------------+       +----------+-----------+
        \_________  PDP  ___________/     | ordena
                                          v
 Sujeto ---> [ PEP ] ================> Recurso   (plano de datos)
             (hace cumplir)
```

| Componente | Función según la norma |
|---|---|
| **PE** | La decisión final de conceder, denegar o revocar, con un algoritmo de confianza; registra la decisión |
| **PA** | Establece o corta la ruta de comunicación; genera credenciales de sesión |
| **PEP** | Habilita, vigila y termina la conexión entre sujeto y recurso |
| **PDP** | PE y PA juntos |

---

## Lo que alimenta al motor

Además del sujeto y el recurso, el PE recibe datos de fuentes como:

- Un sistema de **diagnóstico y mitigación continuos** (CDM): estado y parches del equipo
- Sistemas de **cumplimiento** de la industria
- **Inteligencia de amenazas**, internas y externas
- Registros de actividad y de acceso

Estas fuentes alimentan el **principio 7**: más contexto, mejores decisiones.

El libro de la unidad desarrolla cada confianza por separado: dispositivos (cap. 5), identidades (cap. 6), aplicaciones (cap. 7), tráfico (cap. 8). Fuente: *Zero Trust Networks* (Rais et al., 2024), caps. 3 a 8.

<!-- Subrayar la separación de planos: el que decide no está en el camino del tráfico, el que hace cumplir sí. Eso permite revocar sin tocar el recurso. -->

---

<!-- _class: lead -->

# Parte 3
## Un motor de decisiones que puedes ejecutar

---

## Simulación simplificada de la arquitectura

Qué es: objetos Python que **imitan** a PE, PA y PEP de la figura de la norma.
Qué simplifica: **no hay red, ni TLS, ni identidad real**; la clave de firma es ficticia, y la política es un diccionario.
Qué sí es real: la **lógica** de decidir por atributos, la ficha firmada con HMAC-SHA256 y la reevaluación en cada uso.

El mismo código lo ejecutas en la práctica. Todo es **Ejemplo ilustrativo**.

---

## La política como dato

```python {1-3|4-6|7-9|*}
POLITICA = {
  "api-lab/facturas": {"sensibilidad": "alta", "mfa": True,
                       "dispositivo_sano": True, "horario": (7, 22)},
  "api-lab/catalogo": {"sensibilidad": "baja", "mfa": False,
                       "dispositivo_sano": True, "horario": (0, 24)},
}
DIRECTORIO = {"ana": {"grupos": {"facturacion"}}, "luis": {"grupos": {"soporte"}}}
ACCESOS = {"facturacion": {"api-lab/facturas", "api-lab/catalogo"},
           "soporte": {"api-lab/catalogo"}}
```

Cada recurso declara **qué exige**. Lo que no está en la política **se deniega por omisión**.

---

## El Policy Engine

```python {1-3|4|5-7|8-10|11-13|14|*}
def decide(self, sujeto, recurso, contexto):
    regla = POLITICA.get(recurso)
    if regla is None: return False, "recurso sin politica (denegar por omision)"
    if sujeto in REVOCADOS: return False, "identidad revocada"
    grupos = DIRECTORIO.get(sujeto, {}).get("grupos", set())
    if not any(recurso in ACCESOS.get(g, set()) for g in grupos):
        return False, "la identidad no tiene este recurso"
    if regla["mfa"] and not contexto["mfa"]: return False, "falta MFA"
    if regla["dispositivo_sano"] and not contexto["dispositivo_sano"]:
        return False, "dispositivo no cumple la postura"
    h = contexto["hora"]
    if not (regla["horario"][0] <= h < regla["horario"][1]):
        return False, "fuera de horario"
    return True, "cumple la politica"   # la red de origen no cuenta
```

Observa lo que **no** hay: ninguna línea mira la IP de origen.

---

## Administrador y punto de cumplimiento

- **PA:** pregunta al PE; si permite, emite una **ficha** firmada con sujeto, recurso y expiración (300 s en el ejemplo)
- **PEP:** en **cada uso**, valida la firma, el recurso, la expiración **y vuelve a preguntar al PE**

```python
ok, motivo = self.pe.decide(datos["s"], recurso, contexto)   # se reevalua en cada uso
```

Por eso revocar una identidad, o que un equipo pierda su postura, **corta el acceso aunque la ficha siga vigente**: es el principio 6 de la norma (reevaluación continua).

La ficha es de **un solo recurso**: reusarla en otro falla (principio 3: no hereda).

---

## Diez escenarios

Salida real (Python 3, noviembre de 2026):

```
1 ana + MFA + equipo sano       : True
2 ana sin MFA                   : False
3 luis (soporte) a facturas     : False
4 ana, equipo sin parches       : False
5 uso de la ficha a t=60        : True
6 la misma ficha en otro recurso: False
7 el equipo pierde postura a t=90: False
8 ana revocada, ficha vigente   : False
9 ficha vencida (t=400)         : False
10 ficha alterada               : False
```

Solo los casos 1 y 5 permiten (el 5 usa una ficha emitida a t=0). Todo lo demás se niega **por una razón distinta y legible**.

---

## La bitácora: cada decisión con su porqué

```
{"evento": "decision", "motivo": "cumple la politica", "permitido": true, "recurso": "api-lab/facturas", "sujeto": "ana", "t": 0}
{"evento": "decision", "motivo": "falta MFA", "permitido": false, "recurso": "api-lab/facturas", "sujeto": "ana", "t": 0}
{"evento": "decision", "motivo": "la identidad no tiene este recurso", "permitido": false, "recurso": "api-lab/facturas", "sujeto": "luis", "t": 0}
```

Consulta "¿quién entró a facturas?" sobre ese registro:

```
0 ana PERMITIDO - cumple la politica
0 ana DENEGADO - falta MFA
0 luis DENEGADO - la identidad no tiene este recurso
90 ana DENEGADO - dispositivo no cumple la postura
100 ana DENEGADO - identidad revocada
101 ? DENEGADO - ficha invalida
```

El principio 7 y el libro *Identity-Native Infrastructure Access Management* (Kontsevoy et al., 2023), cap. 6 (auditoría), apuntan a lo mismo: **sin registro, no hay Zero Trust demostrable**.

---

## Verlo fallar: ¿tu prueba detecta que falta el control?

Quita la comprobación de MFA del motor (una línea) y vuelve a correr:

```
1 ana + MFA + equipo sano : True
2 ana sin MFA             : True     <- debía ser False
3 luis (soporte) a facturas: False
```

El escenario 2 **cambió**: tu prueba cae si el control desaparece. Así sabes que **el escenario 2 de verdad prueba MFA**, y no solo que "algo" negó el acceso.

Regla: si quitas un control y **ninguna prueba cae**, esa prueba no verificaba nada.

<!-- Es la misma idea de verificación del M3 (el auditor): la mutación demuestra que la prueba sirve. -->

---

## Error típico: la política en forma de `if` sueltos

| Síntoma | Causa | Arreglo |
|---|---|---|
| Nadie sabe qué exige un recurso | Reglas repartidas en código | Política como dato, en un solo lugar |
| Un recurso nuevo queda abierto | Se asumió "permitir" por omisión | **Denegar por omisión** |
| Se revocó a alguien y siguió entrando | La ficha duraba días y nadie la reevaluaba | Fichas cortas y reevaluación en el PEP |
| "No sé por qué lo negó" | El motor devuelve solo true/false | Devolver **el motivo** y registrarlo |

Quien ya trabajó con políticas como código en un pipeline: el curso `/infrastructure-as-code/` lo desarrolla; aquí interesa el **diseño de la decisión**.

---

<!-- _class: lead -->

# Parte 4
## Autorización en la nube

---

## AWS: así decide IAM

Según la documentación de AWS (a noviembre de 2026):

- Dentro de una misma cuenta, los permisos de las políticas **basadas en identidad y en recursos se suman**; un **Deny explícito** en cualquiera **anula** un Allow
- Con una **frontera de permisos**, el resultado es la **intersección**
- En una organización, una acción debe estar permitida por la política de identidad, las **SCP** y las **RCP**; un Deny explícito en cualquiera las anula

En términos de la norma: IAM es un **PDP** que ya tienes; tu trabajo es **alimentarlo con políticas mínimas**.

Fuente: docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_evaluation-logic.html

---

## Verlo en moto: el Deny que gana

Política de la identidad `api-lab`: `s3:*` sobre `lab-datos`, y un `Deny` explícito a `s3:DeleteObject`.

Salida real (moto 5.2.5 con control de acceso tipo IAM, boto3 1.44.0, noviembre de 2026):

```
PutObject: permitido
DeleteObject: AccessDenied
ListBuckets: AccessDenied
```

- `PutObject`: permitido por el `Allow`
- `DeleteObject`: el `Deny` gana aunque `s3:*` lo cubra
- `ListBuckets`: **nunca se permitió**; denegado por omisión

Moto imita la evaluación para este caso; **no** reproduce todas las condiciones de IAM. Los matices de IAM se tratan en M2.

---

## AWS Verified Access: aplicaciones sin VPN

Según la documentación de AWS:

- Da acceso seguro a aplicaciones **sin requerir una VPN**
- Evalúa **cada petición** a la aplicación; la idea tradicional, dice la documentación, es evaluar una sola vez y dar acceso a todo
- Se integra con servicios de identidad y de gestión de dispositivos (de AWS y de terceros)
- **Registra todos los intentos de acceso**
- Tiene cargo por hora por aplicación y por datos procesados

Fuente: docs.aws.amazon.com/verified-access/latest/ug/what-is-verified-access.html

En la norma, es un **PEP** administrado, con un motor que consulta identidad y estado del dispositivo.

---

## Servidores: sin puertos de entrada

Session Manager de AWS Systems Manager (M3) es el equivalente para **administrar nodos**:

| Zero Trust | Cómo lo hace Session Manager |
|---|---|
| Autenticar y autorizar por sesión | Permisos IAM por persona o grupo y por nodo |
| No confiar en la red | Sin puertos de entrada, sin bastión, sin llaves SSH |
| Acceso temporal | Se puede dar acceso solo mientras dura un turno |
| Registrar | CloudTrail, S3, CloudWatch Logs y EventBridge |

Limitación documentada: **no** se registran los comandos en sesiones de reenvío de puertos o SSH.

Fuente: docs.aws.amazon.com/systems-manager/latest/userguide/session-manager.html. Las ideas de infraestructura con identidad: *Identity-Native Infrastructure Access Management* (Kontsevoy et al., 2023), caps. 2 a 6.

---

## Azure: Conditional Access

Microsoft Entra Conditional Access es el "**motor de políticas de Zero Trust**" de Microsoft. Funciona como **si-entonces**: si alguien quiere entrar a un recurso, debe cumplir algo.

| Señales | Decisiones |
|---|---|
| Usuario, grupo | Bloquear el acceso |
| Ubicación por IP | Exigir autenticación multifactor |
| Dispositivo | Exigir dispositivo marcado como conforme |
| Aplicación | Exigir otros controles (cambio de contraseña, términos de uso) |
| Riesgo detectado | |

Cosas que debes saber (documentación de Microsoft, abril de 2026):

- Las políticas se aplican **después** de la autenticación de primer factor
- No es una defensa de primera línea contra denegación de servicio
- Requiere licencias **Microsoft Entra ID P1**; el riesgo requiere **P2**
- Existe el modo **solo informe** (*report-only*): evalúa sin bloquear

Fuente: learn.microsoft.com/entra/identity/conditional-access/overview

---

## Comparación honesta

| Pregunta de la norma | AWS | Azure |
|---|---|---|
| ¿Quién decide? (PDP) | Evaluación de IAM; Verified Access para aplicaciones | Conditional Access |
| ¿Quién cumple? (PEP) | Servicios de AWS; Verified Access; Session Manager | Servicios protegidos por Entra |
| ¿Qué contexto usa? | Identidad, recurso, condiciones de la política; dispositivo en Verified Access | Usuario, dispositivo, ubicación, aplicación, riesgo |
| ¿Dónde queda el registro? | CloudTrail y registros de cada servicio | Registros de inicio de sesión de Entra |

No hay un "botón Zero Trust" en ninguna nube: cada una trae **piezas** que debes componer.

---

<!-- _class: lead -->

# Parte 5
## De la VPN a la identidad

---

## Migrar sin romper a nadie

| Paso | Qué haces | Cómo sabes que funcionó |
|---|---|---|
| 1 | **Inventariar** quién usa qué (por registros de la VPN y de las apps) | Una tabla persona x aplicación |
| 2 | Escribir la **política** de cada aplicación como dato | Un archivo versionado |
| 3 | Correr en **modo informe** o en sombra | Decisiones "habría denegado" revisadas |
| 4 | Cortar la **ruta** de VPN a esa aplicación | La aplicación solo responde por el PEP |
| 5 | Quitar lo sobrante (reglas, rutas, usuarios de VPN) | El auditor de M3 sale en 0 |
| 6 | Mantener el **registro** y revisarlo | Alertas de denegaciones inusuales (M5) |

Los pasos 3 y 4 no se invierten: **cortar antes de mirar** es la forma más rápida de tener una caída.

---

## Micro-segmentación y Zero Trust

La norma describe tres enfoques de arquitectura: **gobierno de identidad reforzado**, **micro-segmentación** y **infraestructura de red con perímetros definidos por software**.

Lo que ya hiciste en M3 (grupos de seguridad que referencian a otros grupos) es micro-segmentación **a nivel de red**; Zero Trust le agrega **decisión por identidad y contexto** encima.

| Capa | Segmentación de M3 | Zero Trust (M4) |
|---|---|---|
| Qué decide | La red, por regla | La política, por atributo |
| Cuándo | Al establecer la conexión | **Cada sesión**, con reevaluación |
| Registro | Flow Logs | Bitácora de decisiones |

Fuentes: NIST SP 800-207, sección 3.1 (variaciones del enfoque); *Zero Trust Networks* (Rais et al., 2024), cap. 9 (realizar una red Zero Trust).

---

## Qué NO es Zero Trust

- **No es** "una VPN con MFA": sigue decidiendo por ubicación una vez dentro
- **No es** un producto que se compra y termina
- **No es** desconfiar de las personas: es **verificar** el contexto de cada acceso
- **No es** sustituir la segmentación: la complementa
- **No es** "todo o nada": se adopta por aplicación, la de mayor riesgo primero

La propia norma advertía en 2020 que el ecosistema de componentes **no era aún lo bastante maduro** para una adopción amplia, y que ningún producto cubría todo. Hoy hay más piezas; **revisa el estado actual** antes de decidir.

Fuente: NIST SP 800-207, apéndice B (brechas identificadas). Más retos: *Zero Trust Networks*, cap. 12.

---

## Asumir brecha: consecuencias de diseño

> Diseña como si algo ya estuviera dentro.

| Si asumes brecha... | Entonces... |
|---|---|
| Un equipo puede estar comprometido | Su postura se reevalúa y se puede cortar |
| Una credencial puede filtrarse | Fichas cortas, por recurso, revocables |
| Un servicio puede ser abusado | Cada servicio solo habla con sus pares |
| No lo notarás a tiempo | Registro de **cada** decisión y alerta sobre lo raro |

Las dos primeras filas son lo que el laboratorio acaba de mostrar; las dos últimas conectan con M5 (detección y respuesta).

Fuente: *Practical Cloud Security* (Dotson, 2023), cap. 1 (Zero Trust y defensa en profundidad).

---

## Mapa de errores de Zero Trust

| Mala práctica | Por qué ocurre | Detección | Corrección | Verificación |
|---|---|---|---|---|
| Acceso por IP de origen / VPN | Herencia del perímetro | Registro: decisiones sin identidad ni estado del equipo | Política por atributos en un PEP | Un equipo comprometido recibe `DENEGADO` |
| Permitir por omisión | Comodidad | Recursos sin política | Denegar lo no declarado | Un recurso sin política devuelve `DENEGADO` |
| Fichas largas sin reevaluar | "Que no pida login seguido" | Revocación que no corta | Expiración corta y reevaluación en cada uso | Revocar corta con ficha vigente |
| Decisión sin motivo | Solo devuelve true/false | No se puede explicar un rechazo | Registrar el porqué | La bitácora responde "¿por qué?" |
| Política sin prueba | Se cree que funciona | Quitar un control y ver que nada cae | Prueba por control | Al quitar MFA el escenario 2 cae |
| Conditional Access sin modo informe | Prisa | Bloqueos masivos | Probar en solo informe primero | Revisar el impacto antes de activar |

---

## Alcance de este curso

- Diseñamos, ejecutamos y **verificamos defensas**; no explicamos cómo atacar un Zero Trust
- Los ejemplos corren en tu máquina; lo no ejecutado (Azure) se marca como fragmento
- Para la perspectiva adversaria, el libro de la unidad dedica el cap. 10 (*The Adversarial View*) y la práctica ofensiva autorizada es otro curso: `/pentesting-with-kali/`
- Para vigilar y responder: `/blue-team/`

---

## Resumen

1. Un acceso decidido por **ubicación** hereda los fallos de la red
2. **NIST SP 800-207 (agosto de 2020):** siete principios; PE decide, PA abre o cierra, PEP hace cumplir; PE + PA = PDP
3. No mezcles **principios (NIST)**, **pilares (CISA)** y **principios guía (Microsoft)**
4. Una decisión Zero Trust usa **identidad, estado del equipo y contexto**, es **por sesión**, se **reevalúa** y se **registra con su motivo**
5. En AWS: IAM como PDP (el Deny explícito gana), Verified Access y Session Manager; en Azure: Conditional Access
6. Se migra **por aplicación**, en modo informe primero
7. **Verifica** cada control quitándolo y viendo que su prueba cae

---

## Práctica y tarea

**Práctica (en clase):** correr el motor de decisiones, verlo negar diez casos, **quitar el control de MFA y ver que la prueba cae**, repetir el caso del Deny en moto y consultar la bitácora.

**Tarea:** diseñar la política Zero Trust de una aplicación tuya (atributos, decisiones, registro y plan de migración desde VPN).

**Libros del módulo**

- *Zero Trust Networks* (Rais et al., 2024), caps. 1 a 9
- *Practical Cloud Security* (Dotson, 2023), cap. 1
- *Identity-Native Infrastructure Access Management* (Kontsevoy et al., 2023), caps. 2 a 6
- *Cloud Native Security Cookbook* (Armitage, 2022), cap. 5 (recetas 5.14 y 5.15, acceso privado a aplicaciones)
- *Building Secure and Reliable Systems* (Adkins et al., 2020), cap. 5 (diseño para mínimo privilegio)

**Siguiente:** M05, detección y respuesta en la nube.
