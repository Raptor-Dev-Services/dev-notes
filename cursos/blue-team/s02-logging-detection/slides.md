<!-- _class: lead -->

# S02 - Bitácoras y detección

### Defensa y respuesta a incidentes

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Antes de empezar, cada equipo verifica su laboratorio: `kali` (192.168.56.10), `victima` (192.168.56.20) y la VM del SIEM (192.168.56.30). Esta sesión instala el agente en victima y recibe sus bitácoras en el SIEM. Si fail2ban quedó activo desde la práctica del módulo 6 de Ciberseguridad, se detiene al empezar la práctica: si no, bloquea el ataque y la regla nunca suena. Confirmar la carta de autorización del curso de Ciberseguridad antes de lanzar nada.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, repaso de la sesión 1 | 20 min |
| Parte 1 | Qué registrar y cómo se ve en Linux | 45 min |
| Parte 2 | Centralizar: del archivo local al SIEM | 40 min |
| Parte 3 | Reglas de detección: anatomía, umbrales y errores | 60 min |
| Parte 4 | Alertas que alguien puede atender y cómo probarlas | 30 min |
| Parte 5 | Práctica: el ataque de Hydra tiene que hacer sonar la alerta | 90 min |
| Cierre | Errores típicos, tarea y quiz | 15 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. -->

---

## El lema de la sesión

> Una alerta que nunca sonó en una prueba no se sabe si funciona.

- Las bitácoras existen casi siempre. Lo que falta es que alguien las mire a tiempo.
- Una regla sin probar puede estar mal por tres razones distintas y en las tres el síntoma es el mismo: **silencio**.
- Hoy cada regla se escribe, se dispara a propósito y se prueba también **sin** el ataque.

<!-- Pregunta al grupo: ¿alguna vez una alarma suya dejó de sonar sin que se enteraran? Alarmas de humo, de monitoreo, de pipelines. Es la misma falla. -->

---

## Al terminar puedes

- Decir qué eventos de un servidor merecen registro y cuáles datos **no** deben registrarse.
- Leer a ojo una línea de `sshd` y una de nginx y extraer quién, cuándo y desde dónde.
- Explicar por qué se centralizan las bitácoras y qué cuidar al hacerlo.
- Escribir una regla con fuente, condición, agrupación, umbral y ventana.
- Medir falsos positivos y falsos negativos de una regla.
- Probar una regla disparándola y comprobar que calla sin el ataque.

---

## Repaso de la sesión 1

| Pieza | Para qué la usamos hoy |
|---|---|
| El inventario | Decide **qué fuentes** hay que enviar al SIEM |
| La exposición | Decide qué fuentes se vigilan primero |
| El dueño | Es a quien llega la alerta |
| Los plazos | Un activo expuesto con una vulnerabilidad en KEV sube a primera fila **de vigilancia** |

Lo que no está en el inventario no envía bitácoras: **es un punto ciego**.

---

## Dónde estamos en NIST CSF 2.0

| Función | Categoría | Idea |
|---|---|---|
| **Detect** | `DE.CM` Continuous Monitoring | Los activos se monitorean para encontrar anomalías, indicadores de compromiso y otros eventos adversos |
| **Detect** | `DE.AE` Adverse Event Analysis | Esas anomalías se analizan para caracterizar los eventos y detectar incidentes |

Hoy es la función **Detect**. Y en los CIS Controls v8.1, el control 8: gestión de registros de auditoría.

Fuentes: NIST CSWP 29 (2024); CIS Controls v8.1 (24 de junio de 2024).

---

<!-- _class: lead -->

# Parte 1
## Qué registrar y cómo se ve

---

## No todo se registra

Se registra lo que alguien va a necesitar para contestar: **¿quién hizo qué, cuándo y desde dónde?**

| Pregunta | Dato |
|---|---|
| **Cuándo** | Fecha y hora con zona horaria, en formato internacional |
| **Dónde** | Aplicación, host, puerto, servicio |
| **Quién** | Dirección de origen e identidad del usuario, si se conoce |
| **Qué** | Tipo de evento, severidad, descripción |

Fuente: OWASP, *Logging Cheat Sheet*.

---

<!-- _pasos: lista -->

## Eventos que conviene registrar

- Éxitos y fallos de **autenticación**.
- Fallos de **autorización** (control de acceso).
- Administración de usuarios y **cambios de privilegios**.
- Uso de cuentas por defecto, compartidas o de emergencia.
- Errores del sistema, cambios de configuración y **arranques, paradas o pausas del propio registro**.
- Acceso a datos sensibles; importaciones y exportaciones.

Fuente: OWASP, *Logging Cheat Sheet*. El último punto de la tercera viñeta importa: **un atacante que apaga el registro es un evento**.

---

## Lo que NO se registra

| No registrar (o enmascarar, hashear o cifrar) | Por qué |
|---|---|
| Contraseñas | La bitácora se vuelve una segunda base de credenciales |
| Tokens de acceso e identificadores de sesión | Quien lee la bitácora puede suplantar |
| Llaves y secretos; cadenas de conexión | Ídem |
| Datos personales sensibles y datos de tarjetas | Obligaciones legales y daño a personas |

Un caso típico: un formulario de login que guarda la petición completa "para depurar".

**Regla práctica:** la bitácora tiene casi siempre **menos** protección que la base de datos. No pongas ahí nada que no protegerías igual.

Fuente: OWASP, *Logging Cheat Sheet*.

---

## Los registros de `victima`

| Fuente | Qué contiene | Dónde |
|---|---|---|
| Autenticación | `sshd`, `sudo`, PAM, cron | `/var/log/auth.log` y el journal |
| Servicio web | Cada petición a nginx | `/var/log/nginx/access.log` |
| Errores web | Fallos de nginx | `/var/log/nginx/error.log` |
| Sistema | Arranque, servicios, kernel | `journalctl` |

El journal se consulta con `journalctl`; con `-o short-iso` las fechas salen en ISO 8601.

---

## Qué dice sshd cuando falla una contraseña

`sshd` arma la línea en **una sola función** de su código fuente (`auth_log`, en `auth.c` de OpenSSH):

```text
<Accepted|Failed|Partial|Postponed> <método> for [invalid user ]<usuario> from <IP> port <puerto> ssh2
```

```text
2026-11-12T10:10:52.003108+00:00 victima sshd[2108]: Failed password for practica from 192.168.56.10 port 41011 ssh2
2026-11-12T10:11:01.003116+00:00 victima sshd[2116]: Failed password for invalid user invitado from 192.168.56.10 port 41944 ssh2
```

**Ejemplo ilustrativo**: líneas generadas con un script a partir del formato real; no son de un incidente.

---

## Usuario existente o no existente

| Lo que adivinan | Qué se ve |
|---|---|
| Contraseña de un usuario **que existe** | `Failed password for practica` |
| Un usuario **que no existe** | Antes, `Invalid user invitado from ... port ...`; después, `Failed password for invalid user invitado` |

- La línea `Invalid user` es una **señal gratis**: distingue a quien adivina usuarios de quien adivina contraseñas de uno real.
- Una regla que solo busca `Failed password` ve las dos cosas mezcladas. A veces es lo que quieres; a veces no.

---

## Dos datos de configuración de sshd

| Opción (`sshd_config`) | Por omisión | Qué hace |
|---|---|---|
| `MaxAuthTries` | 6 | Intentos de autenticación por conexión; a la mitad de ese valor, los fallos adicionales se registran |
| `LogLevel` | INFO | Cuánto detalle escribe; DEBUG viola la privacidad de los usuarios y no se recomienda |

Fuente: `sshd_config(5)`, manual de OpenBSD.

Consecuencia: un ataque que prueba una contraseña por conexión y uno que prueba varias en la misma conexión (limitado por `MaxAuthTries`) dejan rastros distintos. Una regla debe saber cuál busca.

---

## El nombre del proceso cambió

OpenSSH **9.8** (1 de julio de 2024) dividió el servidor en dos binarios: un `sshd` que escucha y un `sshd-session` por cada sesión.

| Instalación | Cómo se ve la línea |
|---|---|
| Ubuntu 24.04 (OpenSSH 9.6p1, anterior a la 9.8) | `sshd[2108]: Failed password ...` |
| Una instalación de Ubuntu 26.04 revisada para este curso | `sshd-session[3397201]: Connection closed ...` |

Una regla que busca solo `sshd[` **deja de ver el ataque** después de actualizar el sistema, sin avisar. Lo comprobamos en la práctica.

Fuente: notas de la versión OpenSSH 9.8; paquete `openssh-server` de Ubuntu noble (1:9.6p1).

---

## Verlo fallar: el nombre de proceso

```bash
grep -c 'sshd\[.*Failed password' auth.log
sed 's/ sshd\[/ sshd-session[/' auth.log > auth-session.log
grep -c 'sshd\[.*Failed password' auth-session.log
grep -cE 'sshd(-session)?\[.*Failed password' auth-session.log
```

```text
8
0
8
```

El mismo registro, la misma cantidad de ataques: la primera regla pasó de ver 8 a ver **0**. Es un falso negativo silencioso. Arreglo: aceptar las dos formas y, mejor aún, **avisar cuando una línea parezca de ataque y no se pueda leer**.

<!-- Salida real de los comandos sobre el registro de ejemplo de la práctica. -->

---

## El formato de fecha también cambia

| Formato | Ejemplo |
|---|---|
| ISO 8601 con zona | `2026-11-12T10:10:52.003108+00:00` |
| Clásico de syslog | `Nov 12 10:10:52` (sin año ni zona) |

Depende de la plantilla de rsyslog de cada instalación: **míralo con `head -3`**, no lo supongas.

Una regla que lee la fecha de la forma ISO y recibe la clásica puede descartar todas las líneas sin un solo error. En la práctica la regla de la ventana **debe quejarse** cuando eso ocurra.

---

## Nginx: el formato `combined`

```nginx
log_format combined '$remote_addr - $remote_user [$time_local] '
                    '"$request" $status $body_bytes_sent '
                    '"$http_referer" "$http_user_agent"';
```

```text
192.168.56.10 - - [12/Nov/2026:10:21:06 +0000] "GET /test/ HTTP/1.1" 404 153 "-" "lab-scanner/1.0"
```

| Campo awk | Dato |
|---|---|
| `$1` | IP del cliente |
| `$4` | Fecha (con `[` al inicio) |
| `$9` | Código de estado |

El formato `combined` es el predefinido del módulo de registro de nginx; la ruta por omisión de `access_log` es relativa al directorio de instalación. Confirma la tuya: `nginx -T | grep access_log`.

Fuente: `ngx_http_log_module`, nginx.org.

---

## Un solo evento no es una alerta

| Evento | ¿Alerta? |
|---|---|
| Un `Failed password` | No: alguien se equivocó |
| Cinco en un minuto desde una IP | Quizá: depende de quién |
| Un 404 en nginx | No: un enlace roto |
| Doce 404 con rutas como `/.env` y `/.git/config` | Casi seguro: alguien está buscando |

La detección es **patrón sobre muchos eventos**: cuántos, desde dónde, en cuánto tiempo y contra qué.

---

<!-- _class: lead -->

# Parte 2
## Centralizar

---

## Por qué sacar las bitácoras de la máquina

| Problema si se quedan locales | Qué pasa |
|---|---|
| Quien entra las puede borrar o alterar | La evidencia desaparece justo cuando hace falta |
| No se pueden correlacionar | Un fallo en `victima` y un escaneo en el proxy son un ataque que nadie ve junto |
| Se rotan y se llenan | La retención es corta |

**Una copia ya enviada es evidencia; la local, no tanto.**

---

## Dos formas de centralizar

| Forma | Cómo | Pro | Contra |
|---|---|---|---|
| **Agente** | Un programa lee archivos y journal y los envía | Entiende formatos y cambios de configuración | Un agente más que parchar |
| **Reenvío de syslog** | El sistema manda sus mensajes por la red | Nada que instalar | Sin garantías si falla la red |

En el curso: **agente**. Son dos productos abiertos que sirven al laboratorio.

---

## Wazuh y Elastic en el laboratorio

| | Wazuh | Elastic Stack |
|---|---|---|
| Piezas | manager, indexer y dashboard en el servidor; agente en cada equipo | Elasticsearch, Kibana y Elastic Agent |
| Reglas | XML, con reglas listas para `sshd` y servidores web | Reglas de detección en Kibana |
| Tamaño del laboratorio | Guía rápida (4.14 a noviembre de 2026): 4 vCPU y 8 GiB de RAM para hasta 25 agentes | Según el libro de Pease, caps. 4 a 7 |
| En el curso | **Ruta principal** | Alternativa: el libro arma todo el laboratorio |

Elige una. Los conceptos de la sesión son los mismos en las dos.

Fuentes: documentación de Wazuh (guía rápida); *Threat Hunting with Elastic Stack* (Pease, 2021).

---

## Los tipos de regla de Elastic

A noviembre de 2026, Kibana ofrece estos tipos de regla de detección:

| Tipo | Para qué sirve |
|---|---|
| Custom query | Un evento que cumple una consulta |
| Event correlation (EQL) | Secuencias de eventos |
| **Threshold** | **N coincidencias, agrupadas por un campo** |
| Indicator match | Comparar con indicadores de inteligencia |
| New terms | Un valor que nunca se había visto |
| ES\|QL | Consultas en ES\|QL |
| Machine learning | Anomalías por modelo |

Para fuerza bruta, la que corresponde a lo que haremos es **Threshold**.

Fuente: documentación de Elastic, "Create a detection rule".

---

## Qué recoge un agente de Wazuh

La plantilla de configuración del agente en el repositorio de Wazuh incluye, entre otros:

```xml
<localfile>
  <log_format>syslog</log_format>
  <location>/var/log/auth.log</location>
</localfile>
<localfile>
  <log_format>apache</log_format>
  <location>/var/log/nginx/access.log</location>
</localfile>
```

- Para nginx usa el formato `apache`: el `combined` de nginx tiene la misma forma que el de Apache.
- **Verifícalo** en tu agente: `grep -A2 localfile /var/ossec/etc/ossec.conf`. Si falta, se agrega.

<!-- Aclarar que el XML de arriba es la forma de la plantilla (formato + ubicación), no una copia literal del archivo. -->

---

## Instalar el agente en `victima`

Procedimiento según la documentación de Wazuh 4.14 (no ejecutado al preparar este material), en Ubuntu con `apt`:

```bash {1|2|3|4-5|6}
sudo apt-get install -y gnupg apt-transport-https curl
curl -s https://packages.wazuh.com/key/GPG-KEY-WAZUH | sudo gpg --no-default-keyring --keyring gnupg-ring:/usr/share/keyrings/wazuh.gpg --import && sudo chmod 644 /usr/share/keyrings/wazuh.gpg
echo "deb [signed-by=/usr/share/keyrings/wazuh.gpg] https://packages.wazuh.com/4.x/apt/ stable main" | sudo tee /etc/apt/sources.list.d/wazuh.list
sudo apt-get update
sudo WAZUH_MANAGER="192.168.56.30" apt-get install -y wazuh-agent
sudo systemctl daemon-reload && sudo systemctl enable --now wazuh-agent
```

La clave y el repositorio los publica Wazuh: **verifica los comandos contra su documentación del día** antes de ejecutarlos.

---

## Lo que hay que cuidar al centralizar

| Cuidado | Por qué |
|---|---|
| **Hora sincronizada** y con zona | Una línea de tiempo con relojes distintos no se reconstruye |
| **Retención y capacidad** | Cuánto y por cuánto tiempo es una decisión con costo |
| **Cobertura** | Lo que no envía bitácoras es un punto ciego; mide el % de activos del inventario con su fuente activa |
| **Integridad** | El registro es un activo: acceso limitado y cambios registrados |
| **Alertas de silencio** | Una fuente que dejó de enviar es un hallazgo |

---

<!-- _class: lead -->

# Parte 3
## Reglas de detección

---

<!-- _pasos: filas -->

## La anatomía de una regla

| Pieza | Pregunta | Ejemplo: fuerza bruta SSH |
|---|---|---|
| Fuente | ¿Qué datos mira? | `auth.log` de `victima` |
| Condición | ¿Qué evento cuenta? | Línea `Failed password` de sshd |
| Agrupación | ¿Cuenta por qué? | Por IP de origen |
| Umbral | ¿Cuántos? | 5 |
| Ventana | ¿En cuánto tiempo? | 60 segundos |
| Severidad | ¿Qué tan urgente? | Media; sube si luego hay un `Accepted` |
| Acción | ¿Qué sigue? | Avisar y enlazar al runbook |

---

## Primera regla en cinco líneas

```bash {1-3|4-8|9}
#!/usr/bin/env bash
# Regla 1: >= N fallos de contrasena de sshd desde la misma IP en el mismo MINUTO.
set -euo pipefail
grep -E 'sshd(-session)?\[[0-9]+\]: Failed password' "${2:-/var/log/auth.log}" |
awk -v u="${1:-5}" '
  { t = ($1 ~ /^[0-9]{4}-/) ? substr($1, 1, 16) : $1 " " $2 " " substr($3, 1, 5)
    for (i = 1; i <= NF; i++) if ($i == "from") { ip = $(i + 1); break }
    n[ip " " t]++ }
  END { for (k in n) if (n[k] >= u) print "ALERTA", k, n[k] }' | sort
```

- Cuenta por **IP y minuto**: una ventana fija.
- Acepta las dos formas de fecha y las dos del proceso.
- `detecta-ssh.sh` en la práctica.

---

## Verlo fallar: el borde del minuto

El registro de ejemplo trae seis fallos desde `192.168.56.10` en 15 segundos: tres a las 10:10 y tres a las 10:11.

```bash
./detecta-ssh.sh 5 auth.log; echo "alertas con umbral 5: $(./detecta-ssh.sh 5 auth.log | wc -l)"
./detecta-ssh.sh 3 auth.log
```

```text
alertas con umbral 5: 0
ALERTA 192.168.56.10 2026-11-12T10:10 3
ALERTA 192.168.56.10 2026-11-12T10:11 3
```

Seis fallos seguidos desde una IP y **cero alertas**: el ataque cruzó el cambio de minuto y quedó repartido en dos cubetas. Con umbral 3 suena, pero dos veces.

---

## La ventana deslizante

Cuenta los fallos de **los últimos 60 segundos desde cada evento**, no por minuto de reloj.

```python {1-4|5-8|9-10|*}
ventana = defaultdict(deque)
for linea in open(ruta):
    m = fallo.match(linea)
    ts, usuario, ip = datetime.fromisoformat(m[1]), m[2], m[3]
    q = ventana[ip]
    q.append((ts, usuario))
    while (ts - q[0][0]).total_seconds() > t:
        q.popleft()
    if len(q) >= n:
        print(f"ALERTA {ip}: {len(q)} fallos en {t} s")
```

Fragmento de `detecta-ssh-ventana.py` (el script completo, con `import` y avisos, está en la práctica).

---

## Con ventana deslizante sí suena

```bash
./detecta-ssh-ventana.py 5 60 auth.log
```

```text
ALERTA 192.168.56.10: 5 fallos en 60 s, usuarios invitado,practica, desde 2026-11-12T10:10:52+00:00
```

- Salida real sobre el registro de ejemplo.
- La alerta incluye los **usuarios distintos** que se probaron: dos. Esa es la diferencia entre adivinar una contraseña y adivinar nombres.
- Con umbral 7 no suena, y **es lo correcto**: el ataque de ejemplo solo hizo seis intentos.

---

## Falsos positivos y falsos negativos

| | Qué es | Qué cuesta |
|---|---|---|
| **Falso positivo** | Suena y no había ataque | Fatiga de alertas: la gente deja de mirar |
| **Falso negativo** | Había ataque y calló | El ataque avanza sin que nadie lo sepa |

Bajar el umbral reduce los falsos negativos y sube los falsos positivos.

```bash
./detecta-ssh-ventana.py 2 60 auth.log
```

```text
ALERTA 192.168.56.1: 2 fallos en 60 s, usuarios admin, desde 2026-11-12T10:02:05+00:00
ALERTA 192.168.56.10: 2 fallos en 60 s, usuarios practica, desde 2026-11-12T10:10:52+00:00
```

La primera alerta es el administrador legítimo que se equivocó dos veces antes de entrar. **No hay umbral correcto universal**: hay uno defendible para **tu** tráfico.

---

## Cómo se elige un umbral

1. Reúne **datos normales** de tu sistema (una semana si se puede).
2. Reúne **datos de ataque** de tu laboratorio.
3. Prueba umbrales y escribe, para cada uno: cuántos falsos positivos y cuántos falsos negativos.
4. Elige el que puedas defender y **anota por qué**.
5. Revísalo cuando cambie el tráfico.

Copiar el umbral de otro sistema es copiar sus datos normales.

---

## La fuerza bruta según MITRE ATT&CK

La técnica **T1110, Brute Force**, y sus subtécnicas:

| ID | Nombre | Idea |
|---|---|---|
| T1110.001 | Password Guessing | Muchas contraseñas contra una cuenta |
| T1110.002 | Password Cracking | Romper contraseñas obtenidas fuera de línea |
| T1110.003 | Password Spraying | Una contraseña contra muchas cuentas |
| T1110.004 | Credential Stuffing | Credenciales filtradas de otro sitio |

Entre las pistas de detección que ATT&CK da para la técnica: **volumen alto de fallos seguido de un éxito** desde el mismo origen, y fallos contra **muchas cuentas** en intervalos cortos.

Fuente: MITRE ATT&CK, técnica T1110.

---

## Dos reglas distintas, no una

| Regla | Cuenta | Detecta |
|---|---|---|
| Fallos por IP | `Failed password` por IP | Fuerza bruta (T1110.001) |
| **Usuarios distintos** por IP | Nombres diferentes por IP | Rociado de contraseñas (T1110.003) o enumeración de usuarios |
| Fallos **seguidos de éxito** | Un `Accepted` tras varios fallos | Que el ataque funcionó |

La tercera es la más importante: **un ataque que funcionó es un incidente**. Esa regla sube la severidad y es la que dispara la sesión 3.

---

## Las reglas por omisión de Wazuh para sshd

Del archivo `0095-sshd_rules.xml` (rama `master` del repositorio de Wazuh, noviembre de 2026):

| Regla | Nivel | Qué detecta |
|---|---|---|
| 5710 | 5 | Intento de login con un usuario inexistente |
| 5716 y 5760 | 5 | Falla de autenticación |
| 5715 | 3 | Autenticación exitosa |
| 5712 | 10 | Fuerza bruta: **8** coincidencias de la 5710 en **120 s** desde la misma IP |
| 5763 | 10 | Fuerza bruta: **8** coincidencias de la 5760 en **120 s** desde la misma IP |

Verifica las de **tu** manager: `grep -B1 -A8 'id="5763"' /var/ossec/ruleset/rules/0095-sshd_rules.xml`.

---

## El ataque del módulo 6 no alcanza el umbral

La práctica de Hydra del curso de Ciberseguridad prueba **2 usuarios × 3 contraseñas = 6 combinaciones como máximo**.

- La regla por omisión pide **8** en 120 segundos.
- Seis nunca llegan a ocho: con la configuración de fábrica **esa alerta no suena**.

Esto no es un defecto de Wazuh: sus umbrales piensan en ataques con listas largas. Es la razón por la que un equipo de defensa **escribe y prueba sus propias reglas**.

<!-- Dejar que descubran por sí mismos el 6 contra 8 antes de decirlo. Preguntar: ¿qué cambiarían? -->

---

## Una regla propia en Wazuh

Las reglas propias van en `/var/ossec/etc/rules/local_rules.xml`, con identificadores de **100000 a 120000**.

```xml {1-2|3-4|5|6-8|*}
<group name="local,sshd,">
  <rule id="100100" level="10" frequency="5" timeframe="60">
    <if_matched_sid>5760</if_matched_sid>
    <same_source_ip />
    <description>Laboratorio: fallos repetidos de contrasena en sshd desde la misma IP</description>
    <mitre>
      <id>T1110.001</id>
    </mitre>
  </rule>
</group>
```

Misma forma que la 5763: otra frecuencia (5) y otro `timeframe` (60). Se reinicia el manager (`sudo systemctl restart wazuh-manager`).

---

## Probar la regla sin atacar: wazuh-logtest

```bash
sudo /var/ossec/bin/wazuh-logtest
```

Se pega una línea de registro y muestra tres fases:

| Fase | Qué muestra |
|---|---|
| Pre-decodificación | Fecha, host y nombre del programa |
| Decodificación | Campos extraídos (`srcip`, `srcport`, `srcuser`) |
| Filtrado | La regla que coincide, su nivel y su descripción |

Pegando varias veces la misma línea se ve **cuántas repeticiones hacen falta** para que cambie la regla que coincide. Se mide: no se supone.

Procedimiento según la documentación de Wazuh 4.14, no ejecutado al preparar este material.

---

## Sigma: reglas que no dependen del SIEM

**Sigma** es un formato abierto, en YAML, para describir detecciones sin atarlas a un producto.

| Campo | Obligatorio | Qué es |
|---|---|---|
| `title` | Sí | Título corto |
| `logsource` | Sí | Qué logs mirar: `product: linux`, `service: sshd` |
| `detection` | Sí | La lógica, con su `condition` |
| `status`, `level`, `falsepositives`, `id`... | No | Metadatos |

El **conteo** (N en un tiempo) se expresa con una **regla de correlación** de tipo `event_count`, con `timespan`, `group-by` y `condition`.

Fuente: SigmaHQ, documentación de reglas, fuentes de logs y correlaciones.

---

## Una regla Sigma de la práctica

```yaml {1-8|9-17}
title: Fallo de contrasena en sshd
name: fallo_ssh
logsource:
  product: linux
  service: sshd
detection:
  seleccion: ['Failed password']
  condition: seleccion
---
title: Fuerza bruta de contrasenas en sshd desde una IP
correlation:
  type: event_count
  rules: [fallo_ssh]
  group-by: [src_ip]
  timespan: 1m
  condition:
    gte: 5
```

Fragmento escrito según la especificación; no se convirtió ni se ejecutó al preparar este material. Un convertidor (sigma-cli con el backend de tu SIEM) la traduce; el nombre `src_ip` depende de la conversión.

---

## Una regla para nginx

Sondeo de rutas: muchos 404 desde una IP, con rutas que ningún usuario pide.

```bash
awk '$9 == 404 { n[$1 " " substr($4, 2, 17)]++ }
     END { for (k in n) if (n[k] >= 5) print "ALERTA", k, n[k] }' access.log
```

```text
ALERTA 192.168.56.10 12/Nov/2026:10:20 8
```

- Sale `8` en el minuto 10:20 y **4 más** a las 10:21: con umbral 10 **no suena**, por el mismo borde de minuto de antes.
- Wazuh trae reglas parecidas para servidores web: la 31151 pide **14** errores 4xx en **90 s** desde la misma IP; la 31152, 8 intentos de inyección SQL en 120 s.

Fuente: `0245-web_rules.xml` de Wazuh (rama `master`, noviembre de 2026).

---

<!-- _class: lead -->

# Parte 4
## Alertas y pruebas

---

## Una alerta que alguien puede atender

| Lleva | Para qué |
|---|---|
| La regla y **las líneas** que la dispararon | No solo "hubo 5": cuáles y de dónde |
| El **activo** y su dueño del inventario | A quién llamar |
| La **severidad** y por qué | Qué tan urgente |
| Un enlace al **runbook** | Los primeros tres pasos para confirmar |

**Alerta accionable:** quien la recibe sabe qué pasó, por qué importa y qué hacer ahora. Si no, es ruido.

---

## Los primeros pasos de un runbook de fuerza bruta

1. **¿Es nuestra?** La IP de origen: ¿es de un administrador, de un escáner autorizado o desconocida?
2. **¿Funcionó?** Buscar un `Accepted` de la misma IP o con los mismos usuarios.
3. **¿Quién y qué?** Usuarios probados: ¿existen? ¿Alguno tiene privilegios?
4. Si funcionó: **es un incidente.** Se pasa a la sesión 3 (contener, erradicar, recuperar).

```bash
grep -E '(Accepted|Failed) password .* from 192.168.56.10 ' /var/log/auth.log
```

---

## Dos límites de las reglas

- **Una regla sin dueño se apaga sola.** Alguien la silencia por ruido y nadie lo nota.
- **Las reglas se revisan.** Cada alerta cerrada como falso positivo debe terminar en un ajuste de la regla o en una excepción documentada.

Y un tercero, el que casi todos olvidan: **una fuente que dejó de enviar bitácoras es un hallazgo.** Alerta por silencio.

---

## Una regla está lista cuando pasó dos pruebas

| Prueba | Qué comprueba |
|---|---|
| **Suena con el ataque** | La regla ve lo que debe ver |
| **Calla sin el ataque** | La regla no hace ruido con tráfico normal |

```bash
./probar-reglas.sh
```

```text
ok    ssh ventana: con ataque suena (1 alertas)
ok    ssh ventana: sin ataque calla (0 alertas)
ok    web 404: con sondeo suena (1 alertas)
ok    web 404: trafico normal calla (0 alertas)
```

Salida real. Un verde no basta: **hay que romper la regla y ver que la prueba cae.**

---

## La mutación: romper la regla a propósito

Cambia el umbral de la regla web de 5 a 9 y vuelve a correr las pruebas:

```text
ok    ssh ventana: con ataque suena (1 alertas)
ok    ssh ventana: sin ataque calla (0 alertas)
FALLA web 404: con sondeo suena: esperaba 1 y hubo 0
ok    web 404: trafico normal calla (0 alertas)
```

Salida real. La prueba **cayó**: ahora sabes que la prueba mide algo. Si una prueba sigue en verde después de romper la regla, **no prueba nada**.

---

<!-- _class: lead -->

# Parte 5
## Práctica

---

## Qué vas a hacer

1. Instalar el agente de Wazuh en `victima` y ver su bitácora de `sshd` y de nginx en el SIEM.
2. Escribir los detectores locales y **probarlos disparándolos** con registros de ejemplo.
3. **Verlo fallar:** lanzar la fuerza bruta de Hydra del módulo 6 (con autorización, en laboratorio propio) y ver qué pasa con la regla por omisión.
4. Escribir la regla propia para que el ataque **sí** haga sonar la alerta.
5. Probar que calla sin el ataque y mutarla.

Todo ataque va contra **tu propia** `victima` en la red host-only. El detalle está en `practica.md`.

---

## Verlo fallar

El ataque del módulo 6 hace, como máximo, 6 intentos. La regla de fábrica pide 8.

- Lanza Hydra contra tu `victima`.
- Cuenta cuántas líneas `Failed password` dejó en el registro.
- ¿Sonó la alerta de nivel 10? ¿Por qué?
- Escribe tu regla con umbral 5 y repite el ataque.

**La lección:** una regla que viene instalada no es una regla probada **para tu ataque**.

<!-- Dejar que cada equipo mida cuántos intentos reales dejó. Hydra con -t 1 -W 3 puede tardar; si hay discrepancias con la cuenta, es parte del aprendizaje. -->

---

<!-- _class: lead -->

# Cierre

---

## Errores típicos de la sesión

| Error | Se ve así |
|---|---|
| Regla que nunca se disparó | Silencio, y nadie lo sabe hasta el incidente |
| Formato de fecha o de proceso distinto | La regla ve cero líneas y no avisa |
| Ventana fija sin medir el borde | El ataque que cruza el minuto pasa |
| Umbral copiado | Falsos negativos o ruido |
| Contar IP y olvidar usuarios | No distingue adivinar contraseñas de rociarlas |
| Alerta sin runbook | Suena y nadie sabe qué hacer |
| Registrar contraseñas | La bitácora es otra base de credenciales |
| Relojes sin sincronizar | La línea de tiempo no cuadra |

---

## Lo que se lleva esta sesión

- Una lista de eventos que merecen registro y de datos que no.
- Dos fuentes de registro leídas: `sshd` y nginx.
- Un SIEM que recibe bitácoras de `victima`.
- Reglas con fuente, condición, agrupación, umbral y ventana, **disparadas a propósito**.
- Una prueba que suena con el ataque, calla sin él y **cae** si se rompe la regla.

---

## Tarea

**Tres reglas más**, cada una disparada a propósito y probada también sin el ataque.

- Entregable: las tres reglas (en el formato de su SIEM o como scripts), sus pruebas y una tabla de falsos positivos y negativos medidos.
- Criterios y formato en `tarea.md`.
- Lectura previa: la nota `12-seguridad/06-blue-team/02-bitacoras-y-deteccion.md` y `10-cloud/04-observabilidad.md`.

---

## Libros de la sesión

| Libro | Capítulos |
|---|---|
| *Defensive Security Handbook* (Berlin, Brotherston y Reyor, 2024) | Cap. 21 Understanding IDSs and IPSs; cap. 22 Logging and Monitoring |
| *Threat Hunting with Elastic Stack* (Pease, 2021) | Cap. 3 Introduction to the Elastic Stack; cap. 4 y 5 Building Your Hunting Lab, partes 1 y 2; cap. 6 Data Collection with Beats and the Elastic Agent; cap. 7 Using Kibana to Explore and Visualize Data |

Se citan por capítulo. Cada quien consigue su copia.

---

<!-- _class: lead -->

# Gracias
## Quiz de la sesión 2 en la plataforma
