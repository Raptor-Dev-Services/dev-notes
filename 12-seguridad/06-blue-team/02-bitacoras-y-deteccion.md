# 02 - Bitácoras y detección

> Lectura del curso [Defensa y respuesta a incidentes](../../cursos/blue-team/README.md), sesión 2. Antecedente: [`10-cloud/04-observabilidad.md`](../../10-cloud/04-observabilidad.md), que explica logs estructurados, métricas y trazas desde el lado de la aplicación.

## Por qué importa

Un incidente que nadie ve no se responde. La detección es lo que convierte a las bitácoras (que casi todos los sistemas ya producen) en una alarma que suena a tiempo. Tiene dos mitades que se prueban por separado: **hay datos** (se registra lo que hace falta y llega a un lugar donde alguien lo puede consultar) y **hay reglas** (alguien definió qué patrón de esos datos merece atención y se comprobó que la regla suena cuando debe y calla cuando no).

La segunda mitad es la que más se salta. Una regla que nunca sonó en una prueba no se sabe si funciona: puede tener un umbral imposible, leer un campo que no existe o dejar de reconocer el formato de fecha después de una actualización, y en los tres casos el síntoma es el mismo, silencio. Esta nota cubre qué registrar, cómo centralizar, cómo se escribe una regla, cómo se mide un falso positivo y un falso negativo y cómo se prueba una regla disparándola.

Todos los sistemas de esta nota son del laboratorio del curso (red host-only 192.168.56.0/24: `kali` .10, `victima` .20, `siem` .30). Los registros de ejemplo se generaron con scripts a partir de los formatos reales de los mensajes: no son capturas de un incidente.

## Dónde encaja en NIST CSF 2.0

Detectar es una función completa del CSF 2.0 (publicado el 26 de febrero de 2024). Sus categorías de esta sesión:

| Categoría | Identificador | Idea |
|---|---|---|
| Continuous Monitoring | `DE.CM` | Los activos se monitorean para encontrar anomalías, indicadores de compromiso y otros eventos adversos |
| Adverse Event Analysis | `DE.AE` | Esas anomalías se analizan para caracterizar los eventos y detectar incidentes |

En los CIS Controls v8.1 el equivalente práctico es el control 8, gestión de registros de auditoría.

## Qué registrar

No todo se registra: lo que se registra es lo que alguien va a necesitar para contestar **quién hizo qué, cuándo y desde dónde**. La hoja de referencia de OWASP sobre registro (*Logging Cheat Sheet*) enumera los eventos que conviene guardar. Los que más pesan para quien defiende un servidor:

- Éxitos y fallos de **autenticación**.
- Fallos de **autorización** (control de acceso).
- Administración de usuarios y **cambios de privilegios**; acciones de administradores.
- Uso de cuentas por defecto, compartidas o de emergencia.
- Errores de aplicación y eventos del sistema: arranques, paradas, cambios de configuración, y **arranques, paradas o pausas del propio registro**.
- Acceso a datos sensibles, importaciones y exportaciones.

Cada evento debería traer, según la misma hoja, **cuándo** (fecha y hora en formato internacional), **dónde** (aplicación, host, puerto, servicio), **quién** (dirección de origen e identidad del usuario, si se conoce) y **qué** (tipo de evento, severidad, descripción).

### Lo que no se registra

La hoja de OWASP también lista lo que **no** debe aparecer en una bitácora, o debe enmascararse, hashearse o cifrarse: contraseñas, tokens de acceso e identificadores de sesión, llaves y secretos, cadenas de conexión, datos personales sensibles, datos de tarjetas. Una bitácora que guarda contraseñas es una segunda base de credenciales, y casi siempre con menos protección que la primera. Un caso típico: un formulario de login que registra la petición completa "para depurar".

## Los registros de un servidor Linux del laboratorio

### Autenticación: `auth.log` o el journal

En Ubuntu, los mensajes de autenticación (`sshd`, `sudo`, PAM) llegan a `/var/log/auth.log` mediante rsyslog y también al journal de systemd, que se consulta con `journalctl`. El formato de fecha del archivo depende de la plantilla de rsyslog: una instalación de Ubuntu 26.04 revisada para esta nota escribe fechas ISO 8601 (`2026-11-08T00:59:49.673874+00:00`); otras instalaciones usan la forma clásica `Nov 12 10:10:52`. **No lo asumas: míralo con `head -3`** y, si trabajas con el journal, `journalctl -o short-iso` da fechas ISO.

El texto de los eventos de `sshd` sale de una sola función del código fuente de OpenSSH (`auth_log`, en `auth.c`), que construye la línea con la forma:

```text
<Accepted|Failed|Partial|Postponed> <método> for [invalid user ]<usuario> from <IP> port <puerto> ssh2
```

Por eso una contraseña incorrecta se ve así, para un usuario que existe y para uno que no:

```text
2026-11-12T10:10:52.003108+00:00 victima sshd[2108]: Failed password for practica from 192.168.56.10 port 41011 ssh2
2026-11-12T10:11:01.003116+00:00 victima sshd[2116]: Failed password for invalid user invitado from 192.168.56.10 port 41944 ssh2
```

Además, cuando el usuario no existe, `sshd` escribe antes una línea `Invalid user invitado from 192.168.56.10 port 41944`. Es una **señal gratis**: distingue a quien adivina usuarios de quien adivina contraseñas de un usuario real.

### El nombre del proceso cambió, y las reglas se enteran tarde

OpenSSH 9.8 (1 de julio de 2024) dividió el servidor en dos binarios: un `sshd` que escucha y un `sshd-session` por cada sesión. Una consecuencia directa para quien escribe reglas: las líneas pasan de `sshd[1234]:` a `sshd-session[1234]:`. Ubuntu 24.04 trae OpenSSH 9.6p1 (anterior a la 9.8), así que su proceso se llama `sshd`; una instalación de Ubuntu 26.04 revisada para esta nota registra `sshd-session`. Una regla que busca solo `sshd[` deja de ver el ataque después de una actualización de sistema, sin ninguna advertencia.

### Nginx: `access.log` y `error.log`

El módulo de registro de nginx trae siempre un formato predefinido llamado `combined`:

```nginx
log_format combined '$remote_addr - $remote_user [$time_local] '
                    '"$request" $status $body_bytes_sent '
                    '"$http_referer" "$http_user_agent"';
```

Una línea típica (generada para el laboratorio):

```text
192.168.56.10 - - [12/Nov/2026:10:21:06 +0000] "GET /test/ HTTP/1.1" 404 153 "-" "lab-scanner/1.0"
```

La directiva por omisión escribe en `access.log` bajo el directorio de instalación de nginx, con el formato `combined`; en los paquetes de Ubuntu la ruta habitual es `/var/log/nginx/access.log`, así que confírmala en tu configuración con `nginx -T | grep access_log`.

## Centralizar

Dejar los registros solo en la máquina que los genera tiene tres problemas:

1. **Quien entra los puede borrar o alterar.** Una copia ya enviada a otro lado es evidencia; la local, no tanto.
2. **No se pueden correlacionar.** Un fallo en `victima` y un escaneo en el proxy son un solo ataque que nadie ve junto.
3. **Se rotan y se pierden.** La retención local es corta y se llena junto con el disco.

La centralización envía cada evento, casi en tiempo real, a un sistema que lo almacena, lo indexa y evalúa reglas sobre él. Hay dos formas comunes:

| Forma | Cómo | Pro | Contra |
|---|---|---|---|
| **Agente** en cada equipo | Un programa lee los archivos y los manda (Wazuh agent, Elastic Agent) | Entiende archivos, journal y cambios de configuración | Un agente más que parchar y vigilar |
| **Reenvío de syslog** | El propio sistema manda sus mensajes por la red | Nada que instalar en equipos simples | Sin garantías si la red falla; hay que cuidar el transporte |

### Dos SIEM que cubre el curso

- **Wazuh.** Plataforma de código abierto con tres componentes en el servidor (manager, indexer, dashboard) y un agente en cada equipo. Su guía rápida de instalación asistida, para la versión 4.14 a noviembre de 2026, pide para un solo host con hasta 25 agentes **4 vCPU y 8 GiB de RAM**. Trae decodificadores y reglas para `sshd` y para servidores web; es la ruta principal del laboratorio.
- **Elastic (Elastic Stack).** Elasticsearch, Kibana y Elastic Agent, con integraciones para *System* (eventos de autenticación y syslog en Linux) y *Nginx* (datasets `nginx.access` y `nginx.error`). El libro *Threat Hunting with Elastic Stack* (Pease, 2021) arma el laboratorio completo (caps. 4 a 7). Los tipos de regla de detección de Kibana a noviembre de 2026 son: consulta personalizada, correlación de eventos (EQL), umbral (*Threshold*), coincidencia de indicadores, términos nuevos, ES|QL y aprendizaje automático.

La ruta de Seq con Grafana (como en el dev-stack del instructor) sirve para bitácoras de aplicación; para eventos de seguridad del sistema operativo las dos anteriores ya traen el trabajo de decodificación hecho.

### Lo que hay que cuidar al centralizar

- **Hora.** Todos los equipos deben tener la hora sincronizada y las fechas con zona horaria: una línea de tiempo con relojes distintos no se puede reconstruir.
- **Capacidad y retención.** Cuánto se guarda y por cuánto tiempo es una decisión con costo; defínela antes de que se llene.
- **Cobertura.** El inventario de la sesión 1 manda: lo que no envía registros es un punto ciego. Una métrica útil: porcentaje de activos con su fuente de registros activa.
- **Integridad del registro.** El propio sistema de registro es un activo. Su acceso se limita y sus cambios se registran.

## La anatomía de una regla

Una regla de detección tiene siempre las mismas piezas:

| Pieza | Pregunta | Ejemplo (fuerza bruta SSH) |
|---|---|---|
| Fuente | ¿Qué datos mira? | `auth.log` de `victima` |
| Condición | ¿Qué evento cuenta? | Línea `Failed password` de `sshd` |
| Agrupación | ¿Cuenta por qué? | Por IP de origen |
| Umbral | ¿Cuántos? | 5 |
| Ventana | ¿En cuánto tiempo? | 60 segundos |
| Severidad | ¿Qué tan urgente? | Media; sube si hay un `Accepted` después |
| Acción | ¿Qué sigue? | Avisar y enlazar al runbook |

### Ventana fija y ventana deslizante

La ventana más simple corta el tiempo en cubetas (por ejemplo, por minuto) y cuenta por cubeta. Tiene un defecto que se descubre midiendo: un ataque que cruza el cambio de minuto se reparte entre dos cubetas y puede quedar bajo el umbral en las dos. Con el registro de ejemplo del laboratorio, seis fallos en 15 segundos desde la misma IP se reparten 3 y 3 entre las 10:10 y las 10:11: con umbral de 5 por minuto **no suena nada**. Una ventana **deslizante** (los últimos 60 segundos desde cada evento) sí los junta.

### Falsos positivos y falsos negativos

| Término | Qué es | Costo |
|---|---|---|
| Falso positivo | La regla suena y no había ataque | Fatiga de alertas: la gente deja de mirar |
| Falso negativo | Había ataque y la regla calló | El ataque avanza sin que nadie lo sepa |

Bajar el umbral reduce los falsos negativos y sube los falsos positivos: con el mismo registro de ejemplo, un umbral de 2 fallos en 60 segundos marca al administrador legítimo que se equivocó dos veces antes de entrar. No hay un número correcto universal: hay uno defendible para **tu** tráfico, y se encuentra probando con datos de ataque y datos normales.

### La fuerza bruta en ATT&CK

El marco MITRE ATT&CK describe la técnica **T1110 Brute Force**, con las subtécnicas T1110.001 (*Password Guessing*), T1110.002 (*Password Cracking*), T1110.003 (*Password Spraying*) y T1110.004 (*Credential Stuffing*). Entre las pistas de detección de la técnica en ATT&CK están el volumen alto de fallos seguido de un éxito desde el mismo origen, y los fallos contra muchas cuentas en intervalos cortos (*spraying*). Una regla que solo cuenta fallos por IP no distingue una de otra: agrega el conteo de **usuarios distintos** para ver la diferencia.

## Reglas en Wazuh

Wazuh clasifica cada evento con reglas XML. El conjunto por omisión para `sshd` (archivo `0095-sshd_rules.xml`, rama `master` del repositorio de Wazuh a noviembre de 2026) incluye entre otras:

| Regla | Nivel | Qué detecta |
|---|---|---|
| 5710 | 5 | Intento de inicio de sesión con un usuario que no existe |
| 5716 y 5760 | 5 | Falla de autenticación |
| 5715 | 3 | Autenticación exitosa |
| 5712 | 10 | Fuerza bruta contra usuarios inexistentes: 8 coincidencias de la 5710 en 120 s desde la misma IP |
| 5763 | 10 | Fuerza bruta con autenticación fallida: 8 coincidencias de la 5760 en 120 s desde la misma IP |

Observa el umbral: **8 en 120 segundos**. La práctica de Hydra del módulo 6 del curso de Ciberseguridad usa dos usuarios y tres contraseñas, es decir, **6 combinaciones como máximo**. Esa regla por omisión no puede dispararse con ese ataque, y por eso existen las reglas propias.

### Una regla propia

Las reglas personalizadas viven en `/var/ossec/etc/rules/local_rules.xml` y sus identificadores van de 100000 a 120000. Siguiendo la forma de la 5763:

```xml
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

Después de editar, se reinicia el manager (`systemctl restart wazuh-manager`) y se prueba con la herramienta de prueba de reglas, `/var/ossec/bin/wazuh-logtest`, a la que se pega una línea de registro y muestra tres fases: pre-decodificación, decodificación y regla coincidente. Pegando varias veces la misma línea de un usuario inexistente se ve cuántas repeticiones hacen falta para que la regla pase de la 5710 (nivel 5) a la de fuerza bruta 5712 (nivel 10): mídelo en tu manager en lugar de suponerlo.

> Procedimiento según la documentación de Wazuh 4.14, no ejecutado al preparar este material.

### Qué recoge el agente

La plantilla de configuración del agente en el repositorio de Wazuh incluye, entre los archivos de registro a leer, `/var/log/auth.log` con formato `syslog` y `/var/log/nginx/access.log` y `error.log` con formato `apache` (el formato `combined` de nginx tiene la misma forma que el de Apache). Aun así, **verifica** en la máquina que tu agente los lee: `grep -A2 localfile /var/ossec/etc/ossec.conf`.

## Reglas portables con Sigma

**Sigma** es un formato abierto, en YAML, para describir detecciones sin atarlas a un SIEM. Una regla tiene tres campos obligatorios, `title`, `logsource` y `detection` (con su `condition`); el resto (`id`, `status`, `description`, `falsepositives`, `level`...) son metadatos. Para Linux, el `logsource` se escribe con `product: linux` y `service: auth` o `service: sshd`. El conteo (N en un tiempo) se expresa con una **regla de correlación** de tipo `event_count`, con `timespan`, `group-by` y `condition` (`gte: 5`) sobre una regla base.

Una regla de la práctica, en esa forma:

```yaml
title: Fallo de contrasena en sshd
name: fallo_ssh
logsource:
  product: linux
  service: sshd
detection:
  seleccion:
    - 'Failed password'
  condition: seleccion
level: low
---
title: Fuerza bruta de contrasenas en sshd desde una IP
correlation:
  type: event_count
  rules:
    - fallo_ssh
  group-by:
    - src_ip
  timespan: 1m
  condition:
    gte: 5
level: medium
```

> Fragmento escrito según la especificación de SigmaHQ a noviembre de 2026; no se convirtió ni se ejecutó al preparar este material. Un convertidor (sigma-cli con el backend de tu SIEM) la traduce a la consulta de ese SIEM; el nombre del campo `src_ip` depende del *pipeline* de conversión.

## Alertas que alguien puede atender

Una alerta es útil cuando quien la recibe sabe **qué pasó, por qué importa y qué hacer ahora**. Una alerta bien formada lleva:

- Qué regla sonó y qué la disparó (las líneas de registro que la causaron, no solo el conteo).
- Sobre qué activo, con su dueño del inventario.
- Su severidad y por qué.
- Un enlace a su *runbook*: los primeros tres pasos para confirmar si es real.

Dos límites: **una regla sin dueño se apaga sola** (alguien la silencia por ruido y nadie lo nota) y **las reglas se revisan**: cada alerta cerrada como falso positivo debería terminar en un ajuste de la regla o en una excepción documentada.

## Probar una regla disparándola

Una regla está lista cuando pasó dos pruebas:

1. **Suena con el ataque.** Se lanza el ataque en el laboratorio y se comprueba que la alerta aparece.
2. **Calla sin él.** Se corre la regla sobre tráfico normal y se comprueba que no suena.

Una prueba que nunca ha fallado tampoco se sabe si prueba algo. La comprobación final es **mutarla**: cambia el umbral a un valor imposible y confirma que la prueba **cae**. En la práctica de la sesión, la prueba automática de reglas lo hace.

## Errores típicos

- **Una regla que nunca se probó.** El umbral o el campo está mal y nadie lo sabe hasta el incidente.
- **Formato de fecha o nombre de proceso que cambian.** Una actualización cambia `sshd` por `sshd-session` o el formato de fecha, y la regla deja de ver sin avisar.
- **Ventana fija sin medir el borde.** El ataque que cruza el cambio de minuto pasa.
- **Umbral copiado.** Los 8 intentos de otro sistema no son los de tu tráfico.
- **Contar IP y olvidar usuarios.** No distingue adivinar una contraseña de rociar una contraseña sobre muchas cuentas.
- **Alerta sin dueño ni runbook.** Suena y nadie sabe qué hacer.
- **Registrar secretos.** La bitácora termina con contraseñas en claro.
- **Centralizar sin sincronizar la hora.** La línea de tiempo no cuadra.

## Actualización al 2026

- OpenSSH 9.8 (1 de julio de 2024) separó `sshd` en `sshd` y `sshd-session`: revisa tus reglas.
- Wazuh: versión 4.14 en la guía de instalación a noviembre de 2026; las reglas citadas son las de la rama `master` del repositorio en esa fecha y pueden cambiar entre versiones. Lee las de tu propio manager en `/var/ossec/ruleset/rules/`.
- Sigma incluye reglas de correlación (tipos `event_count`, `value_count`, `temporal` y otros) además de las reglas de un solo evento.

## Glosario

| Término | Definición |
|---|---|
| Bitácora (log) | Registro ordenado de eventos de un sistema |
| SIEM | Sistema que reúne bitácoras de muchas fuentes, las almacena, las correlaciona y genera alertas |
| Agente | Programa que lee bitácoras en un equipo y las envía al SIEM |
| Decodificador | Componente que extrae campos (IP, usuario, puerto) de una línea de bitácora |
| Regla de detección | Condición sobre eventos que, al cumplirse, genera una alerta |
| Umbral | Número de coincidencias necesarias para disparar una regla |
| Ventana de tiempo | Periodo en el que se cuentan las coincidencias |
| Ventana deslizante | Ventana que se mueve con cada evento, sin cortes fijos |
| Falso positivo | Alerta sin ataque real |
| Falso negativo | Ataque real que no genera alerta |
| Fatiga de alertas | Pérdida de atención de quien recibe demasiadas alertas |
| Fuerza bruta | Probar muchas contraseñas contra una cuenta (ATT&CK T1110) |
| Password spraying | Probar una contraseña contra muchas cuentas (ATT&CK T1110.003) |
| Sigma | Formato abierto de reglas de detección independientes del SIEM |
| Runbook | Guía corta de los primeros pasos para atender un tipo de alerta |
| Mutación (de una prueba) | Estropear a propósito la regla para comprobar que su prueba falla |

## Referencias

- Berlin, A., Brotherston, L. y Reyor, W. F., III. (2024). *Defensive security handbook: Best practices for securing infrastructure* (2.ª ed.). O'Reilly Media. Caps. 21 y 22.
- Center for Internet Security. (2024). *CIS Critical Security Controls Version 8.1*. https://www.cisecurity.org/controls/v8-1
- Elastic. (s. f.). *Create a detection rule*. https://www.elastic.co/docs/solutions/security/detect-and-alert/create-detection-rule
- MITRE. (s. f.). *Brute Force, Technique T1110*. MITRE ATT&CK. https://attack.mitre.org/techniques/T1110/
- National Institute of Standards and Technology. (2024). *The NIST Cybersecurity Framework (CSF) 2.0* (NIST CSWP 29). https://doi.org/10.6028/NIST.CSWP.29
- nginx. (s. f.). *Module ngx_http_log_module*. https://nginx.org/en/docs/http/ngx_http_log_module.html
- OpenSSH. (2024, 1 de julio). *OpenSSH 9.8 release notes*. https://www.openssh.org/txt/release-9.8
- OpenSSH. (s. f.). *auth.c* (función `auth_log`). openssh-portable. https://github.com/openssh/openssh-portable/blob/master/auth.c
- OWASP Foundation. (s. f.). *Logging Cheat Sheet*. OWASP Cheat Sheet Series. https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html
- Pease, A. (2021). *Threat hunting with Elastic Stack*. Packt Publishing. Caps. 3 a 7.
- SigmaHQ. (s. f.). *Sigma rules: Rules, log sources and correlations*. https://sigmahq.io/docs/basics/rules.html
- Wazuh. (s. f.). *Wazuh documentation (4.14): Quickstart, Custom rules, Testing the ruleset, Wazuh agent installation, localfile*. https://documentation.wazuh.com/current/
- Wazuh. (s. f.). *0095-sshd_rules.xml* y *0245-web_rules.xml*. wazuh/wazuh, ruleset. https://github.com/wazuh/wazuh/tree/master/ruleset/rules

---

*Rogelio Arriaga Gonzalez*
