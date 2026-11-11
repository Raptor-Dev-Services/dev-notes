# Práctica - S02 - Que el ataque haga sonar la alerta

**Defensa y respuesta a incidentes**
**Duración estimada:** 2.5 horas

---

## Objetivo

Escribir detectores para la fuerza bruta de SSH y para el sondeo de rutas de nginx, **dispararlos a propósito**, comprobar que callan con tráfico normal y que su prueba cae si se rompe la regla. Después, hacer lo mismo en el SIEM del laboratorio con el ataque de Hydra del módulo 6 del curso de Ciberseguridad.

La regla del curso: una regla que nunca sonó en una prueba no se sabe si funciona. Hoy cada regla se ve sonar, y se ve fallar.

---

## Marco legal y ético, al frente

> **Autorización.** Todo ataque de esta práctica va contra **su propia** máquina `victima` (192.168.56.20), desde **su propia** `kali` (192.168.56.10), en la red host-only del laboratorio, con la misma carta de autorización del curso de Ciberseguridad firmada por el equipo. Nunca contra una dirección que no sea de ese laboratorio. Los ejemplos de registros de esta práctica se generan con scripts: no son de ningún incidente real.

---

## Antes de empezar

- Las tres máquinas del laboratorio encendidas: `kali` (.10), `victima` (.20, con `ssh` y `nginx`) y la VM del SIEM (.30).
- La VM del SIEM con **8 GiB de RAM y 4 vCPU**: es lo que pide la guía rápida de Wazuh 4.14 para hasta 25 agentes a noviembre de 2026.
- Una captura (*snapshot*) de cada VM antes de empezar: la práctica instala cosas.
- Una carpeta de trabajo en `victima`: `mkdir -p ~/blue-team/s02 && cd ~/blue-team/s02`.
- **Detengan fail2ban en `victima` si quedó de la práctica del módulo 6**: si está activo, bloquea la IP de `kali` y la regla nunca suena.

```bash
sudo systemctl is-active fail2ban || true
sudo systemctl stop fail2ban 2>/dev/null || true
```

Al terminar la práctica, vuelvan a activarlo con `sudo systemctl start fail2ban`.

---

## Parte A - Reglas locales con registros de ejemplo

En esta parte no se ataca nada: se generan registros de texto con el mismo formato que dejan `sshd` y nginx y se prueba la lógica de detección sobre ellos. Todo corre en `victima` o en su laptop.

### Paso 1 - Generar los registros de ejemplo

El primero reproduce lo que deja una práctica pequeña de Hydra: dos usuarios y tres contraseñas, con tres segundos entre intentos, más ruido legítimo (un administrador que se equivoca dos veces y luego entra, y la tarea de cron). Guárdenlo como `generar-auth.py`:

```python
#!/usr/bin/env python3
"""Escribe un auth.log de EJEMPLO con los mensajes de sshd (formato de auth_log en OpenSSH).
No ataca nada: solo genera texto. Uso: python3 generar-auth.py > auth.log"""
from datetime import datetime, timedelta

host = "victima"
t0 = datetime(2026, 11, 12, 10, 0, 0)
lineas, pid = [], 2100

def ts(t, k):
    return t.strftime("%Y-%m-%dT%H:%M:%S") + f".{k:06d}+00:00"

def sshd(t, mensaje):
    global pid
    pid += 2
    lineas.append((t, f"{ts(t, 1000 + pid)} {host} sshd[{pid}]: {mensaje}"))

def cron(t):
    lineas.append((t, f"{ts(t, 500)} {host} CRON[3300]: pam_unix(cron:session): session opened for user root(uid=0) by root(uid=0)"))

# Ruido legitimo: el admin entra desde el host (192.168.56.1) tras equivocarse dos veces
t = t0 + timedelta(minutes=2, seconds=5)
for puerto in (58342, 57146):
    sshd(t, f"Failed password for admin from 192.168.56.1 port {puerto} ssh2")
    t += timedelta(seconds=6)
sshd(t, "Accepted password for admin from 192.168.56.1 port 57784 ssh2")
for minuto in range(0, 30, 5):
    cron(t0 + timedelta(minutes=minuto, seconds=1))

# Ataque de lista pequena, como la practica de Hydra del modulo 6: 2 usuarios x 3 claves, 3 s entre intentos
t = t0 + timedelta(minutes=10, seconds=52)
puerto = 40700
for usuario, existe in (("practica", True), ("invitado", False)):
    for _ in range(3):
        puerto += 311
        if not existe:
            sshd(t, f"Invalid user {usuario} from 192.168.56.10 port {puerto}")
        sshd(t + timedelta(milliseconds=2), f"Failed password for {'' if existe else 'invalid user '}{usuario} from 192.168.56.10 port {puerto} ssh2")
        t += timedelta(seconds=3)

print("\n".join(l for _, l in sorted(lineas, key=lambda e: e[1])))
```

```bash
python3 generar-auth.py > auth.log
wc -l auth.log
grep -c 'Failed password' auth.log
```

```text
18 auth.log
8
```

De los 8 fallos, 2 son del administrador legítimo (`192.168.56.1`) y 6 son del ataque (`192.168.56.10`). Miren el archivo completo y comprueben que reconocen cada línea:

```bash
cut -c1-135 auth.log
```

El segundo, para nginx, en formato `combined`. Guárdenlo como `generar-nginx.py`:

```python
#!/usr/bin/env python3
"""Genera un access.log de EJEMPLO en el formato 'combined' de nginx (ngx_http_log_module). Uso: generar-nginx.py > access.log"""
import datetime, random
random.seed(7)
t0 = datetime.datetime(2026, 11, 12, 10, 20, 0)
def linea(t, ip, req, st, size, ua, ref="-"):
    return f'{ip} - - [{t.strftime("%d/%b/%Y:%H:%M:%S")} +0000] "{req}" {st} {size} "{ref}" "{ua}"'
UA = "Mozilla/5.0 (X11; Linux x86_64) Firefox/140.0"
out = []
for i in range(12):   # visitante normal desde el host: una pagina y sus recursos
    t = t0 + datetime.timedelta(seconds=7 * i)
    out.append((t, linea(t, "192.168.56.1", random.choice(["GET / HTTP/1.1", "GET /app.css HTTP/1.1", "GET /app.js HTTP/1.1"]), 200, random.randint(800, 9000), UA)))
t = t0 + datetime.timedelta(seconds=30)   # un enlace roto: un 404 aislado
out.append((t, linea(t, "192.168.56.1", "GET /logo-viejo.png HTTP/1.1", 404, 153, UA)))
rutas = ["/admin", "/login", "/phpmyadmin/", "/.env", "/backup.zip", "/wp-login.php", "/config.php", "/.git/config", "/server-status", "/api/v1/users", "/old/", "/test/"]
t = t0 + datetime.timedelta(seconds=44)   # sondeo de rutas desde kali: 404 casi todos, 2 s entre pedidos
for r in rutas:
    out.append((t, linea(t, "192.168.56.10", f"GET {r} HTTP/1.1", 404, 153, "lab-scanner/1.0")))
    t += datetime.timedelta(seconds=2)
out.sort(key=lambda e: e[0])
print("\n".join(l for _, l in out))
```

```bash
python3 generar-nginx.py > access.log
wc -l access.log
awk '$9 == 404' access.log | wc -l
```

```text
25 access.log
13
```

Doce de los 13 `404` son de `kali` (sondeo de rutas con un cliente de laboratorio) y uno es un enlace roto del visitante normal.

Preparen también los registros "limpios" (sin el ataque), que se usan en las pruebas:

```bash
grep -v '192.168.56.10' auth.log > auth-limpio.log
grep -v '192.168.56.10' access.log > access-limpio.log
```

### Paso 2 - Regla 1: fallos por IP y minuto

Guárdenla como `detecta-ssh.sh`:

```bash
#!/usr/bin/env bash
# detecta-ssh.sh - Regla 1: >= N fallos de contrasena de sshd desde la misma IP en el mismo MINUTO.
# Uso: detecta-ssh.sh [umbral] [archivo]      Ejemplo: ./detecta-ssh.sh 5 auth.log
set -euo pipefail
umbral=${1:-5}
archivo=${2:-/var/log/auth.log}
grep -E 'sshd(-session)?\[[0-9]+\]: Failed password' "$archivo" |
awk -v u="$umbral" '
  { t = ($1 ~ /^[0-9]{4}-/) ? substr($1, 1, 16) : $1 " " $2 " " substr($3, 1, 5)   # minuto
    ip = ""
    for (i = 1; i <= NF; i++) if ($i == "from") { ip = $(i + 1); break }
    n[ip " " t]++ }
  END { for (k in n) if (n[k] >= u) print "ALERTA", k, n[k] }' | sort
```

```bash
chmod +x detecta-ssh.sh
echo "alertas con umbral 5: $(./detecta-ssh.sh 5 auth.log | wc -l)"
./detecta-ssh.sh 3 auth.log
```

```text
alertas con umbral 5: 0
ALERTA 192.168.56.10 2026-11-12T10:10 3
ALERTA 192.168.56.10 2026-11-12T10:11 3
```

**Verlo fallar.** Seis fallos desde la misma IP en 15 segundos y **cero alertas** con umbral 5. Revisen `auth.log`: tres fallos caen a las 10:10 y tres a las 10:11. La ventana es el minuto de reloj; el ataque cruzó el borde y quedó repartido.

Anoten: ¿cuál es el **peor caso** de esta ventana fija? (Pista: un ataque de N intentos, ¿cuántos puede dejar sin contar si el borde cae justo a la mitad?)

### Paso 3 - Regla 1b: ventana deslizante

Guárdenla como `detecta-ssh-ventana.py`:

```python
#!/usr/bin/env python3
"""Regla 1b: >= N fallos de contrasena de sshd desde la misma IP en una ventana DESLIZANTE de T segundos.
Uso: detecta-ssh-ventana.py [N] [T] [archivo]    (fechas ISO 8601; si tu auth.log usa 'Nov 12 10:10:52', ver la practica)"""
import re, sys
from collections import defaultdict, deque
from datetime import datetime
n = int(sys.argv[1]) if len(sys.argv) > 1 else 5
t = int(sys.argv[2]) if len(sys.argv) > 2 else 60
ruta = sys.argv[3] if len(sys.argv) > 3 else "/var/log/auth.log"
fallo = re.compile(r"^(\S+) \S+ sshd(?:-session)?\[\d+\]: Failed password for (?:invalid user )?(\S+) from (\S+) port \d+")
ventana, ya, ilegibles = defaultdict(deque), set(), 0
for linea in open(ruta, errors="replace"):
    m = fallo.match(linea)
    if not m:
        if "Failed password" in linea:      # parece un fallo de sshd pero no lo pudimos leer: que se note
            ilegibles += 1
        continue
    ts, usuario, ip = datetime.fromisoformat(m.group(1)), m.group(2), m.group(3)
    q = ventana[ip]
    q.append((ts, usuario))
    while (ts - q[0][0]).total_seconds() > t:
        q.popleft()
    if len(q) >= n and ip not in ya:
        ya.add(ip)
        usuarios = sorted({u for _, u in q})
        print(f"ALERTA {ip}: {len(q)} fallos en {t} s, usuarios {','.join(usuarios)}, desde {q[0][0].isoformat(timespec='seconds')}")
if ilegibles:
    print(f"AVISO: {ilegibles} lineas con 'Failed password' no se pudieron leer (formato de fecha o de proceso distinto); la regla esta ciega para ellas", file=sys.stderr)
    sys.exit(2)
```

```bash
chmod +x detecta-ssh-ventana.py
./detecta-ssh-ventana.py 5 60 auth.log; echo "salida: $?"
./detecta-ssh-ventana.py 7 60 auth.log; echo "salida: $?"
./detecta-ssh-ventana.py 2 60 auth.log; echo "salida: $?"
```

```text
ALERTA 192.168.56.10: 5 fallos en 60 s, usuarios invitado,practica, desde 2026-11-12T10:10:52+00:00
salida: 0
salida: 0
ALERTA 192.168.56.1: 2 fallos en 60 s, usuarios admin, desde 2026-11-12T10:02:05+00:00
ALERTA 192.168.56.10: 2 fallos en 60 s, usuarios practica, desde 2026-11-12T10:10:52+00:00
salida: 0
```

Tres cosas que leer:

1. Con umbral 5 **sí suena**: la ventana deslizante junta los seis fallos.
2. Con umbral 7 **no suena, y es lo correcto**: el ataque de ejemplo solo hizo seis.
3. Con umbral 2 suena **dos veces**; la primera es el administrador legítimo que se equivocó. Eso es un falso positivo medido.

### Paso 4 - Verlo fallar: la regla ciega

Simulen lo que pasa cuando cambia el formato de la fecha, como en algunas instalaciones de rsyslog. Conviertan las fechas ISO a la forma clásica:

```bash
python3 - <<'PY'
import re, datetime
salida = []
for l in open("auth.log"):
    m = re.match(r"(\d{4})-(\d\d)-(\d\d)T(\d\d:\d\d:\d\d)\.\d+\+00:00 (.*)", l)
    d = datetime.date(int(m[1]), int(m[2]), int(m[3]))
    salida.append(f"{d.strftime('%b')} {d.day:2d} {m[4]} {m[5]}\n")
open("auth-tradicional.log", "w").writelines(salida)
PY
head -2 auth-tradicional.log | cut -c1-100
./detecta-ssh-ventana.py 5 60 auth-tradicional.log; echo "salida: $?"
```

```text
Nov 12 10:00:01 victima CRON[3300]: pam_unix(cron:session): session opened for user root(uid=0) by r
Nov 12 10:02:05 victima sshd[2102]: Failed password for admin from 192.168.56.1 port 58342 ssh2
AVISO: 8 lineas con 'Failed password' no se pudieron leer (formato de fecha o de proceso distinto); la regla esta ciega para ellas
salida: 2
```

El detector **se queja**: ocho líneas parecen de ataque y no pudo leerlas, y termina con código 2. Ahora quiten la queja para ver el problema que evita. Este comando borra del script, desde `if ilegibles:` hasta el final, el aviso y el código 2, y guarda el resultado en otro archivo:

```bash
sed '/^if ilegibles:/,$d' detecta-ssh-ventana.py > detecta-ciega.py
chmod +x detecta-ciega.py
./detecta-ciega.py 5 60 auth-tradicional.log; echo "salida: $?"
```

```text
salida: 0
```

**Cero alertas, código 0, sin ningún aviso**, con un ataque en el archivo. Lo que se aprende: una regla debe avisar cuando recibe datos que no puede leer; el silencio de una regla ciega y el de una regla sana son idénticos.

Ahora el nombre del proceso:

```bash
grep -c 'sshd\[.*Failed password' auth.log
sed 's/ sshd\[/ sshd-session[/' auth.log > auth-session.log
grep -c 'sshd\[.*Failed password' auth-session.log
grep -cE 'sshd(-session)?\[.*Failed password' auth-session.log
./detecta-ssh-ventana.py 5 60 auth-session.log
```

```text
8
0
8
ALERTA 192.168.56.10: 5 fallos en 60 s, usuarios invitado,practica, desde 2026-11-12T10:10:52+00:00
```

Una regla que busque solo `sshd[` pasa de ver 8 fallos a **0** cuando el proceso se llama `sshd-session` (OpenSSH 9.8 en adelante). Los detectores del curso aceptan las dos formas.

### Paso 5 - Regla 2: sondeo de rutas en nginx

Guárdenla como `detecta-web.sh`:

```bash
#!/usr/bin/env bash
# detecta-web.sh - Regla 2: >= N respuestas 404 de nginx desde la misma IP en el mismo minuto (formato combined).
# Uso: detecta-web.sh [umbral] [archivo]
set -euo pipefail
umbral=${1:-10}
archivo=${2:-/var/log/nginx/access.log}
awk -v u="$umbral" '$9 == 404 { n[$1 " " substr($4, 2, 17)]++ }
                    END { for (k in n) if (n[k] >= u) print "ALERTA", k, n[k] }' "$archivo" | sort
```

```bash
chmod +x detecta-web.sh
./detecta-web.sh 10 access.log; echo "alertas con umbral 10: $(./detecta-web.sh 10 access.log | wc -l)"
./detecta-web.sh 5 access.log
```

```text
alertas con umbral 10: 0
ALERTA 192.168.56.10 12/Nov/2026:10:20 8
```

Con umbral 10 **no suena**, por el mismo borde de minuto: ocho `404` a las 10:20 y cuatro a las 10:21. Con 5 suena. Anoten qué rutas pidió el sondeo (`awk '$9==404 {print $7}' access.log`) y cuáles de ellas **ningún usuario legítimo pediría**.

### Paso 6 - La prueba de las reglas, y mutarlas

Guárdenla como `probar-reglas.sh`:

```bash
#!/usr/bin/env bash
# probar-reglas.sh - Una regla no esta lista hasta que (1) suena con el ataque y (2) calla sin el.
set -u
fallos=0
espera() {   # espera <descripcion> <alertas_esperadas> <comando...>
  local desc=$1 esperadas=$2; shift 2
  local n; n=$("$@" | grep -c '^ALERTA' || true)
  if [ "$n" -eq "$esperadas" ]; then echo "ok    $desc ($n alertas)"; else echo "FALLA $desc: esperaba $esperadas y hubo $n"; fallos=$((fallos + 1)); fi
}
espera "ssh ventana: con ataque suena"  1 ./detecta-ssh-ventana.py 5 60 auth.log
espera "ssh ventana: sin ataque calla"  0 ./detecta-ssh-ventana.py 5 60 auth-limpio.log
espera "web 404: con sondeo suena"      1 ./detecta-web.sh 5 access.log
espera "web 404: trafico normal calla"  0 ./detecta-web.sh 5 access-limpio.log
exit "$fallos"
```

```bash
chmod +x probar-reglas.sh
./probar-reglas.sh; echo "salida: $?"
```

```text
ok    ssh ventana: con ataque suena (1 alertas)
ok    ssh ventana: sin ataque calla (0 alertas)
ok    web 404: con sondeo suena (1 alertas)
ok    web 404: trafico normal calla (0 alertas)
salida: 0
```

**Ahora mutarla.** Cambien el umbral de la regla web en la tercera prueba de 5 a 9 y vuelvan a correr:

```bash
sed -i 's|detecta-web.sh 5 access.log|detecta-web.sh 9 access.log|' probar-reglas.sh
./probar-reglas.sh; echo "salida: $?"
```

```text
ok    ssh ventana: con ataque suena (1 alertas)
ok    ssh ventana: sin ataque calla (0 alertas)
FALLA web 404: con sondeo suena: esperaba 1 y hubo 0
ok    web 404: trafico normal calla (0 alertas)
salida: 1
```

La prueba **cayó**. Restauren (`sed -i 's|detecta-web.sh 9 access.log|detecta-web.sh 5 access.log|' probar-reglas.sh`) y comprueben que vuelve a salir 0. Si una prueba no cae cuando rompen la regla, **no prueba nada**.

---

## Parte B - El SIEM y el ataque real del laboratorio

Esta parte instala Wazuh y usa el ataque de Hydra del módulo 6. Los comandos de instalación **siguen la documentación de Wazuh 4.14 a noviembre de 2026 y no se ejecutaron al preparar este material**: léanlos contra la documentación del día antes de correrlos (los paquetes y la versión cambian).

### Paso 7 - El SIEM

En la VM del SIEM (192.168.56.30), la instalación asistida de la guía rápida:

```bash
curl -sO https://packages.wazuh.com/4.14/wazuh-install.sh && sudo bash ./wazuh-install.sh -a
```

Al terminar muestra la dirección del dashboard y las credenciales del usuario `admin`. Si las pierden:

```bash
sudo tar -O -xvf wazuh-install-files.tar wazuh-install-files/wazuh-passwords.txt
```

El dashboard queda en `https://192.168.56.30`. Es un certificado propio del laboratorio: el navegador avisará, y es esperado **solo aquí**.

### Paso 8 - El agente en `victima`

En `victima`, los pasos de la guía del agente (con la IP del SIEM como `WAZUH_MANAGER`):

```bash
sudo apt-get install -y gnupg apt-transport-https curl
curl -s https://packages.wazuh.com/key/GPG-KEY-WAZUH | sudo gpg --no-default-keyring --keyring gnupg-ring:/usr/share/keyrings/wazuh.gpg --import && sudo chmod 644 /usr/share/keyrings/wazuh.gpg
echo "deb [signed-by=/usr/share/keyrings/wazuh.gpg] https://packages.wazuh.com/4.x/apt/ stable main" | sudo tee /etc/apt/sources.list.d/wazuh.list
sudo apt-get update
sudo WAZUH_MANAGER="192.168.56.30" apt-get install -y wazuh-agent
sudo systemctl daemon-reload && sudo systemctl enable --now wazuh-agent
```

Comprueben en el SIEM que el agente está conectado:

```bash
sudo /var/ossec/bin/agent_control -lc
```

Debe listar el agente de `victima` como activo. Y en `victima`, que el agente **lee** sus dos fuentes:

```bash
sudo grep -B1 -A2 -E 'auth.log|nginx' /var/ossec/etc/ossec.conf
```

Si `/var/log/auth.log` o `/var/log/nginx/access.log` no aparecen, agréguenlos dentro de `<ossec_config>` con la forma de la documentación (`<localfile><log_format>syslog</log_format><location>/var/log/auth.log</location></localfile>`, y `log_format` `apache` para el acceso de nginx) y reinicien el agente.

### Paso 9 - Verlo fallar: el ataque no hace sonar la alerta de fábrica

Con autorización, en su laboratorio. Desde `kali`, el ataque de la práctica del módulo 6 contra **su** `victima`:

```bash
hydra -L usuarios_practica.txt -P passwords_practica.txt -t 1 -W 3 -V ssh://192.168.56.20
```

Ahora, en `victima`, cuenten lo que quedó:

```bash
sudo grep -c 'Failed password' /var/log/auth.log
sudo grep -E 'Failed password|Invalid user|Accepted' /var/log/auth.log | tail -n 12 | cut -c1-140
```

Y en el SIEM, busquen las alertas del agente. Archivos de alertas del manager (rutas por omisión según la documentación de Wazuh):

```bash
sudo tail -n 20 /var/ossec/logs/alerts/alerts.log
sudo grep -c 'sshd: brute force' /var/ossec/logs/alerts/alerts.log || true
```

**Respondan en la bitácora:**

1. ¿Cuántos intentos fallidos dejó Hydra? (Con las listas del módulo 6 son, como máximo, 6.)
2. ¿Sonó alguna alerta de nivel 10 de fuerza bruta (reglas 5712 o 5763)? Léanlas en el manager: `sudo grep -B1 -A8 'id="5763"' /var/ossec/ruleset/rules/0095-sshd_rules.xml`.
3. Si no sonó, ¿por qué? (Comparen su cuenta con el `frequency` de esa regla.)

Lo esperado: las reglas de bajo nivel (5710 y 5760) **sí** generan alertas por cada intento, pero las de fuerza bruta piden 8 coincidencias en 120 segundos y el ataque pequeño no las alcanza. Si su ataque dejó 8 o más, la regla sonará: la medición manda.

### Paso 10 - La regla propia

En el manager, agreguen a `/var/ossec/etc/rules/local_rules.xml` (dentro del archivo, sin quitar lo que ya traiga):

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

```bash
sudo systemctl restart wazuh-manager
```

Prueben primero **sin atacar**, pegando una línea en la herramienta de prueba de reglas (usen una línea real de su `auth.log`; la de abajo es de ejemplo):

```bash
sudo /var/ossec/bin/wazuh-logtest
# pegar: Nov 12 10:10:52 victima sshd[2108]: Failed password for practica from 192.168.56.10 port 41011 ssh2
```

Comprueben qué regla coincide en la fase de filtrado. Peguen la línea varias veces seguidas y anoten **en qué repetición** aparece la regla 100100 (la herramienta de prueba mide la frecuencia, no se supone).

Después, repitan el ataque real desde `kali` y comprueben que la alerta **100100** aparece en `alerts.log` y en el dashboard.

### Paso 11 - Calla sin el ataque

Con la regla activa, hagan **entrar legítimamente** a un usuario de `victima` equivocándose dos veces (sin llegar a 5 en un minuto) y comprueben que **no** aparece la 100100:

```bash
ssh admin@192.168.56.20      # escriban mal la contraseña dos veces y luego bien
sudo grep -c '100100' /var/ossec/logs/alerts/alerts.log || true
```

Anoten el conteo antes y después: no debe cambiar. Ésa es la mitad de la prueba que casi todos omiten.

**Mutación en el SIEM.** Cambien `frequency="5"` por `frequency="50"`, reinicien el manager, repitan el ataque y verifiquen que la alerta **ya no** suena. Devuelvan el 5. Si no sonó con 50, la regla importa; si sonó con 50, hay otra regla haciendo el trabajo y hay que averiguar cuál.

---

## Entregables de la práctica

Suban al repositorio del equipo, en `blue-team/s02/`:

1. Los scripts y registros de ejemplo de la parte A y la salida de `probar-reglas.sh` (verde y mutada).
2. `bitacora.md` con: el peor caso de la ventana fija (paso 2), las tres lecturas del paso 3, lo que pasó al quitar la queja (paso 4) y las respuestas del paso 9.
3. La regla 100100, la repetición en la que apareció en `wazuh-logtest` y la captura del conteo antes y después del paso 11.

## Limpieza

Restauren fail2ban si lo detuvieron:

```bash
sudo systemctl start fail2ban 2>/dev/null || true
```

Borren `detecta-ciega.py`, `auth-tradicional.log` y `auth-session.log` si no los necesitan. Conserven los snapshots hasta la sesión 3: el laboratorio se reutiliza.

---

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `detecta-ssh.sh` no imprime nada con datos reales | Formato de fecha o proceso distinto | Corran `head -3 auth.log`; usen `detecta-ssh-ventana.py`, que avisa |
| `grep -c 'Failed password'` da 0 tras Hydra | fail2ban bloqueó la IP, o Hydra no llegó | `sudo fail2ban-client status sshd`; paren fail2ban; revisen conectividad |
| El agente no aparece como activo | Firewall, IP del manager o clave | `sudo systemctl status wazuh-agent` y los registros en `/var/ossec/logs/ossec.log` |
| La regla 100100 no coincide | Error de XML o falta reiniciar | Reinicien el manager y revisen `/var/ossec/logs/ossec.log` |
| El dashboard no carga | La VM no tiene RAM suficiente | Den 8 GiB al SIEM; esperen a que termine de iniciar |
