# Práctica - S05 - Emular tres técnicas y cazarlas

**Defensa y respuesta a incidentes**
**Duración estimada:** 2 horas 30 minutos

---

## Objetivo

Emular tres técnicas de ATT&CK de persistencia en Linux dentro de tu propio laboratorio, cazarlas en las bitácoras con una hipótesis por técnica, y comprobar **dónde falla** cada consulta. Al final tienes cuatro fichas de cacería: tres con hallazgo y una con resultado vacío bien leído.

La regla del curso: si no lo viste fallar, no lo probaste. Aquí lo que falla es la suposición de que "si la consulta no regresa nada, está limpio".

---

## Antes de empezar: alcance y autorización

Esta práctica **ejecuta comportamiento de atacante**. Condiciones, todas obligatorias:

- Es **tu** laboratorio: `victima` (192.168.56.20, Ubuntu Server 24.04 LTS con sshd y nginx), `kali` (192.168.56.10) y, si lo tienes, `siem` (192.168.56.30), en la red host-only 192.168.56.0/24, **sin salida a otras redes**.
- Tienes la **carta de autorización** del módulo 1 del curso de Ciberseguridad (Autorización, ética y marco legal) firmada para esta práctica: alcance (tus tres máquinas), técnicas (las tres de esta hoja) y fechas.
- Las cargas son inocuas (`date`, `touch`, `echo`). No se descarga nada de internet, no se abren shells inversas, no se toca nada que no sea tu laboratorio.
- Cada acción se anota con su hora. Cada técnica se limpia al final.

Si algo de lo anterior falta, **no ejecutes la parte 2**. Puedes hacer todas las cacerías con el conjunto de datos de ejemplo (parte 1).

---

## Antes de empezar: lo que necesitas

- `victima` en UTC (`timedatectl` debe decir `Time zone: Etc/UTC` o similar; si no, `sudo timedatectl set-timezone UTC`). Las consultas de esta hoja suponen marcas de tiempo en UTC.
- Un usuario tuyo con `sudo` en `victima`.
- `rsyslog` escribiendo `/var/log/auth.log` y `/var/log/syslog`:

```bash
ls -l /var/log/auth.log /var/log/syslog
head -1 /var/log/auth.log
```

Si no existen: `sudo apt install -y rsyslog`. La primera línea debe empezar con una marca ISO 8601 (`2026-11-09T22:42:10.123456+00:00 victima ...`). Si empieza con `Nov  9 22:42:10`, tu rsyslog usa el formato tradicional y tienes que adaptar las expresiones regulares de la hoja; anótalo como diferencia en tu ficha.

- Una carpeta de trabajo con la función de ayuda:

```bash
mkdir -p ~/caza/datos && cd ~/caza
cat >> ~/caza/ayuda.sh <<'EOF'
limpia() { sed -E 's/\.[0-9]{6}[+-][0-9:]{4,5} [a-z]+ / /'; }
EOF
source ~/caza/ayuda.sh
```

---

## Parte 1 - Las cacerías sobre datos de ejemplo (50 minutos)

Esta parte no toca ninguna máquina: genera un registro ilustrativo con el formato real y caza en él. Es también tu **plan B** si tu laboratorio no arrancó.

### Paso 1 - Generar los datos

Guarda este script como `~/caza/generar-datos.py`. Necesita `python3` y `ssh-keygen` (los dos vienen con Ubuntu Server):

```python
#!/usr/bin/env python3
"""Genera eventos.log de ejemplo (auth.log y syslog de victima, reunidos). Datos ilustrativos."""
import random, subprocess, tempfile, os, sys
from datetime import datetime, timedelta, timezone

random.seed(56)                      # misma salida cada vez
salida = sys.argv[1] if len(sys.argv) > 1 else "datos"
os.makedirs(salida, exist_ok=True)

def huella():
    """Huella SHA256 real de una llave ed25519 recien creada."""
    with tempfile.TemporaryDirectory() as d:
        k = os.path.join(d, "k")
        subprocess.run(["ssh-keygen", "-q", "-t", "ed25519", "-N", "", "-f", k],
                       check=True, stdout=subprocess.DEVNULL)
        out = subprocess.run(["ssh-keygen", "-lf", k + ".pub"], check=True,
                             capture_output=True, text=True).stdout
        return out.split()[1]        # SHA256:...

FP_DEPLOY, FP_ADMIN, FP_ATAQUE = huella(), huella(), huella()
HOST = "victima"
lineas = []                          # (datetime, texto)

def emite(t, programa, pid, msg):
    marca = t.strftime("%Y-%m-%dT%H:%M:%S.") + f"{random.randint(0, 999999):06d}+00:00"
    lineas.append((t, f"{marca} {HOST} {programa}[{pid}]: {msg}"))

def pid():
    return random.randint(1200, 48000)

# Linea base: 2 al 8 de noviembre de 2026
for dia in range(2, 9):
    d0 = datetime(2026, 11, dia, tzinfo=timezone.utc)
    emite(d0 + timedelta(hours=2), "CRON", pid(), "(root) CMD (/usr/local/bin/respaldo.sh)")
    for h in (9, 13, 17):
        t = d0 + timedelta(hours=h, minutes=random.randint(0, 50))
        quien, fp, ip = random.choice([("deploy", FP_DEPLOY, "192.168.56.1"),
                                       ("admin1", FP_ADMIN, "192.168.56.1")])
        emite(t, "sshd", pid(),
              f"Accepted publickey for {quien} from {ip} port {random.randint(40000, 60000)} ssh2: ED25519 {fp}")
    if dia in (3, 6):                # ruido: alguien se equivoca de clave
        t = d0 + timedelta(hours=11, minutes=5)
        emite(t, "sshd", pid(),
              f"Failed password for admin1 from 192.168.56.1 port {random.randint(40000, 60000)} ssh2")

# Alta legitima de 'monitor' (cambio aprobado el 3 de noviembre, 10:15)
t = datetime(2026, 11, 3, 10, 15, tzinfo=timezone.utc)
emite(t, "sudo", pid(), "admin1 : TTY=pts/0 ; PWD=/home/admin1 ; USER=root ; COMMAND=/usr/sbin/useradd -m -s /bin/bash monitor")
emite(t, "useradd", pid(), "new group: name=monitor, GID=1001")
emite(t, "useradd", pid(), "new user: name=monitor, UID=1001, GID=1001, home=/home/monitor, shell=/bin/bash, from=/dev/pts/0")

# El incidente: 9 de noviembre, noche
base = datetime(2026, 11, 9, 22, 40, tzinfo=timezone.utc)
for i in range(30):                  # fuerza bruta desde kali (T1110.001)
    t = base + timedelta(seconds=i)
    if i % 5 == 4:
        emite(t, "sshd", pid(), f"Invalid user admin from 192.168.56.10 port {50000 + i}")
    emite(t, "sshd", pid(), f"Failed password for soporte from 192.168.56.10 port {50000 + i} ssh2")
t = base + timedelta(seconds=62)
emite(t, "sshd", pid(), "Accepted password for soporte from 192.168.56.10 port 50031 ssh2")
t = base + timedelta(minutes=2, seconds=10)
emite(t, "sudo", pid(), "soporte : TTY=pts/0 ; PWD=/home/soporte ; USER=root ; COMMAND=/usr/sbin/useradd -m -s /bin/bash sysupdate")
emite(t, "useradd", pid(), "new group: name=sysupdate, GID=1002")
emite(t, "useradd", pid(), "new user: name=sysupdate, UID=1002, GID=1002, home=/home/sysupdate, shell=/bin/bash, from=/dev/pts/0")
t = base + timedelta(minutes=3, seconds=20)
p = pid()
emite(t, "crontab", p, "(sysupdate) BEGIN EDIT (sysupdate)")
emite(t + timedelta(seconds=40), "crontab", p, "(sysupdate) REPLACE (sysupdate)")
emite(t + timedelta(seconds=41), "crontab", p, "(sysupdate) END EDIT (sysupdate)")
# Segunda persistencia: archivo soltado en /etc/cron.d. No deja linea de crontab[...]
for m in range(5, 50, 5):
    emite(base + timedelta(minutes=10 + m), "CRON", pid(), "(root) CMD (/opt/.cache/healthd)")
for m in range(4, 15):               # la tarea de sysupdate, cada minuto
    emite(base + timedelta(minutes=m), "CRON", pid(), "(sysupdate) CMD (/tmp/.u/sync.sh >/dev/null 2>&1)")
# Regreso con la llave nueva (T1098.004): no se registra el cambio en authorized_keys
emite(base + timedelta(minutes=30), "sshd", pid(),
      f"Accepted publickey for sysupdate from 192.168.56.10 port 51890 ssh2: ED25519 {FP_ATAQUE}")

lineas.sort(key=lambda x: x[0])
with open(os.path.join(salida, "eventos.log"), "w") as f:
    f.write("\n".join(l for _, l in lineas) + "\n")
with open(os.path.join(salida, "huellas-base.txt"), "w") as f:
    f.write(f"{FP_DEPLOY}\n{FP_ADMIN}\n")
with open(os.path.join(salida, "cron-base.txt"), "w") as f:
    f.write("root /usr/local/bin/respaldo.sh\n")
print(f"{len(lineas)} lineas en {salida}/eventos.log")
```

```bash
cd ~/caza && python3 generar-datos.py datos
# 97 lineas en datos/eventos.log
wc -l datos/*
```

El archivo `eventos.log` junta lo que en una `victima` real está en dos archivos: `auth.log` (sshd, sudo, useradd) y `syslog` (cron y crontab). **Ejemplo ilustrativo:** el formato de cada línea es el real; los eventos, los usuarios y las horas están inventados. Las huellas `SHA256:...` las crea `ssh-keygen` en cada corrida, así que las tuyas serán distintas a las de la clase.

### Paso 2 - Una mirada antes de cazar

```bash
cd ~/caza && source ayuda.sh
head -3 datos/eventos.log | limpia
cut -d' ' -f3 datos/eventos.log | sed -E 's/\[[0-9]+\]:?//' | sort | uniq -c | sort -rn
```

Debes ver `CRON`, `sshd`, `sudo`, `crontab` y `useradd`. Esa lista es el **inventario de fuentes**: lo que puedes cazar con este archivo, y nada más.

### Paso 3 - Cacería 0: la entrada (T1110.001)

Escribe primero la hipótesis y qué resultado la refutaría, y luego corre:

```bash
grep 'Failed password' datos/eventos.log | grep -oE 'from [0-9.]+' | sort | uniq -c
```

Resultado esperado: 30 fallos desde `192.168.56.10` y 2 desde `192.168.56.1`. Contesta en tu ficha: ¿cuál es ruido y por qué? ¿Cuándo entró `192.168.56.10`?

```bash
grep -E 'Accepted (password|publickey)' datos/eventos.log | limpia | grep 192.168.56.10
```

### Paso 4 - Cacería 1: cuenta local nueva (T1136.001)

```bash
grep -E 'useradd\[[0-9]+\]: new user' datos/eventos.log | limpia | cut -c1-165
grep -E 'sudo\[[0-9]+\]: .*COMMAND=/usr/sbin/(useradd|adduser)' datos/eventos.log | limpia
```

Debes encontrar dos cuentas nuevas (`monitor` y `sysupdate`). **El calendario de cambios del equipo dice que solo `monitor` fue aprobada** (el 3 de noviembre, por `admin1`). Llena la ficha: ¿qué evidencia tienes de que `sysupdate` no era legítima? Subraya cada dato: quién, cuándo, desde dónde, con qué nombre.

### Paso 5 - Cacería 2: cron (T1053.003)

Consulta A, las **ediciones** de crontab:

```bash
grep -E 'crontab\[[0-9]+\]: \(.*\) (BEGIN EDIT|REPLACE|DELETE)' datos/eventos.log | limpia
```

Consulta B, lo que **se ejecuta**, contra la línea base:

```bash
grep -E 'CRON\[[0-9]+\]: \(.*\) CMD' datos/eventos.log \
  | sed -E 's/.*\]: \(([^)]*)\) CMD \((.*)\)$/\1 \2/' | sort -u \
  | grep -vxFf datos/cron-base.txt
```

**Verlo fallar.** Compara los dos resultados: la consulta A encuentra **una** persistencia (la de `sysupdate`); la B, **dos**. La segunda, `root /opt/.cache/healthd`, no pasó por el programa `crontab` (en esta práctica la emularás soltando un archivo en `/etc/cron.d/`). Anota en la ficha: ¿qué cobertura tiene la consulta A y cuál no?

Después quita la línea base y repite la B:

```bash
grep -E 'CRON\[[0-9]+\]: \(.*\) CMD' datos/eventos.log \
  | sed -E 's/.*\]: \(([^)]*)\) CMD \((.*)\)$/\1 \2/' | sort -u
```

Salen tres pares, y ninguno se ve "nuevo". Sin línea base la consulta B no distingue lo normal de lo malicioso.

### Paso 6 - Cacería 3: llaves SSH (T1098.004)

Primero, la consulta que **parece** suficiente:

```bash
grep -E 'authorized_keys' datos/eventos.log | limpia
```

No regresa nada. **Verlo fallar:** esto no demuestra que nadie tocó `authorized_keys`; demuestra que `sshd` no registra ese cambio. Segunda consulta, por huellas contra la línea base:

```bash
grep -E 'Accepted publickey' datos/eventos.log \
  | sed -E 's/^(\S+)\.[0-9]{6}[+-][0-9:]{4,5} .*for (\S+) from (\S+) port .* (SHA256:\S+)$/\1 \2 \3 \4/' \
  | while read ts u ip fp; do
      grep -qxF "$fp" datos/huellas-base.txt || echo "$ts $u $ip $fp"
    done
```

Debes ver una sola línea: `sysupdate`, desde `192.168.56.10`, a las 23:10 del 9 de noviembre.

### Paso 7 - Cacería con resultado vacío (T1021.004)

Hipótesis: desde `victima` el adversario se movió lateralmente por SSH a otra máquina.

```bash
grep -cE 'Accepted .* from 192\.168\.56\.20' datos/eventos.log
```

Regresa `0`. Llena la ficha con la lectura honesta: ¿qué cubre `auth.log` de `victima` y qué no? ¿En qué máquina estaría el dato que sí contesta? ¿Qué mejora levantas?

---

## Parte 2 - La emulación en tu laboratorio (70 minutos)

> **Solo con la carta de autorización firmada y solo en tu laboratorio.** Si no la tienes, salta al paso 16 y trabaja con tus datos de ejemplo.

### Paso 8 - Línea base de `victima`

En `victima`, antes de tocar nada:

```bash
mkdir -p ~/caza/base && cd ~/caza && source ayuda.sh
# cuentas con UID de usuario normal
getent passwd | awk -F: '$3>=1000 && $3<65534 {print $1}' | sort > base/cuentas.txt
# crontabs de todos los usuarios y archivos de cron del sistema
for u in $(cut -d: -f1 /etc/passwd); do
  sudo crontab -l -u "$u" 2>/dev/null | grep -v '^#' | sed "s/^/$u /"
done > base/crontabs.txt
ls /etc/cron.d > base/cron-d.txt
# huellas de las llaves SSH que SI deben existir
for f in /home/*/.ssh/authorized_keys /root/.ssh/authorized_keys; do
  sudo test -f "$f" && sudo ssh-keygen -lf "$f"
done | awk '{print $2}' | sort -u > base/huellas.txt
wc -l base/*
```

Haz tres inicios de sesión normales por SSH con tu llave de siempre, para que existan en la bitácora:

```bash
# desde tu máquina anfitriona o desde kali, con tu llave habitual
ssh <TU_USUARIO>@192.168.56.20 true
```

Abre tu registro de emulación (una línea por acción, con hora UTC):

```bash
reg() { echo "$(date -u +%FT%TZ) $*" >> ~/caza/registro.txt; }
reg "inicio de la practica, linea base lista"
```

### Paso 9 - T1136.001: la cuenta

En `victima`:

```bash
reg "T1136.001 useradd sysupdate"
sudo useradd -m -s /bin/bash sysupdate
echo 'sysupdate:ClaveDeLaboratorio-2026!' | sudo chpasswd
id sysupdate
```

Esa contraseña es solo de laboratorio. No la reutilices en ningún lado.

### Paso 10 - T1053.003: dos persistencias con cron

Primera, con el programa `crontab`, como el usuario nuevo:

```bash
reg "T1053.003 crontab de sysupdate"
sudo -u sysupdate mkdir -p /tmp/.u
printf '#!/bin/sh\ndate -u >> /tmp/.u/sync.log\n' | sudo -u sysupdate tee /tmp/.u/sync.sh >/dev/null
sudo chmod 755 /tmp/.u/sync.sh
echo '* * * * * /tmp/.u/sync.sh >/dev/null 2>&1' | sudo -u sysupdate crontab -
sudo -u sysupdate crontab -l
```

Segunda, **sin** pasar por `crontab`, soltando un archivo en `/etc/cron.d/`:

```bash
reg "T1053.003 archivo en /etc/cron.d (healthd)"
sudo mkdir -p /opt/.cache
printf '#!/bin/sh\ndate -u >> /var/tmp/healthd.log\n' | sudo tee /opt/.cache/healthd >/dev/null
sudo chmod 755 /opt/.cache/healthd
echo '* * * * * root /opt/.cache/healthd' | sudo tee /etc/cron.d/healthd >/dev/null
```

Según `man cron`, los archivos de `/etc/cron.d` deben ser de `root` y sin permiso de escritura para grupo ni otros, se vigilan por cambios y llevan el campo de usuario (`root`) antes del comando. El nombre va sin punto, como los `local` o `local-foo` del ejemplo de esa página. Espera unos dos minutos y comprueba que corren:

```bash
tail -2 /tmp/.u/sync.log /var/tmp/healthd.log
```

### Paso 11 - T1098.004: la llave

En `kali`, crea una llave nueva **solo de laboratorio**:

```bash
ssh-keygen -t ed25519 -N '' -C 'lab-persistencia' -f ~/.ssh/lab_persist
cat ~/.ssh/lab_persist.pub
```

En `victima`, instálala para `sysupdate` (pega el contenido de la `.pub` entre las comillas):

```bash
reg "T1098.004 llave en authorized_keys de sysupdate"
sudo install -d -m 700 -o sysupdate -g sysupdate /home/sysupdate/.ssh
echo '<PEGA_AQUI_LA_LLAVE_PUBLICA>' | sudo tee /home/sysupdate/.ssh/authorized_keys >/dev/null
sudo chown sysupdate:sysupdate /home/sysupdate/.ssh/authorized_keys
sudo chmod 600 /home/sysupdate/.ssh/authorized_keys
```

De vuelta en `kali`, entra con ella:

```bash
ssh -i ~/.ssh/lab_persist sysupdate@192.168.56.20 'id; hostname'
```

En `victima`, anota el momento: `reg "T1098.004 login de sysupdate con la llave nueva"`.

### Paso 12 - Reunir las bitácoras y cazar

En `victima`:

```bash
cd ~/caza && source ayuda.sh
sudo cat /var/log/auth.log /var/log/syslog | sort > datos/eventos-vivo.log
wc -l datos/eventos-vivo.log
cat registro.txt
```

Corre sobre `datos/eventos-vivo.log` las **mismas consultas** de los pasos 4, 5 y 6 (cambia `eventos.log` por `eventos-vivo.log`). Las líneas base se arman con lo que ya existía **antes** de tu primera acción:

```bash
# pares usuario-comando que cron ya ejecutaba antes de empezar
T0=$(sed -n 1p registro.txt | cut -c1-19)
awk -v t0="$T0" 'substr($1,1,19) < t0' datos/eventos-vivo.log \
  | grep -E 'CRON\[[0-9]+\]: \(.*\) CMD' \
  | sed -E 's/.*\]: \(([^)]*)\) CMD \((.*)\)$/\1 \2/' | sort -u > datos/cron-base.txt
# huellas autorizadas
cp base/huellas.txt datos/huellas-base.txt
cat datos/cron-base.txt
```

Una `victima` recién instalada ejecuta pocas tareas; si `cron-base.txt` sale casi vacío, es normal: todo lo que aparezca después será nuevo y tendrás que juzgarlo.

Compara cada resultado con tu `registro.txt`: ¿sale todo lo que hiciste? ¿Con qué desfase de hora? Lo que hiciste y **no** salió es un hallazgo de cobertura.

---

## Parte 3 - Verlo fallar y cerrar (30 minutos)

### Paso 13 - Verlo fallar de verdad

En tus datos vivos, comprueba tres cosas y anótalas:

1. La consulta A de cron (ediciones de `crontab`) **no** muestra la persistencia de `/etc/cron.d/healthd`; la B sí.
2. `grep authorized_keys datos/eventos-vivo.log` no regresa el momento en que instalaste la llave; la consulta por huellas sí encuentra el login.
3. Repite la de huellas **sin** `huellas-base.txt` (`: > datos/huellas-base.txt` lo vacía): todas las huellas salen "nuevas". Sin línea base no hay señal.

### Paso 14 - Opcional: ver el cambio de `authorized_keys` con auditd

Fragmento: **no ejecutado al preparar este material.** Si quieres saber **cuándo** se agregó la llave y no solo cuándo se usó, hay que vigilar el archivo. Con auditd (paquete `auditd`), una regla de llamada al sistema sobre el archivo, que debe existir antes de crear la regla:

```bash
sudo apt install -y auditd
sudo auditctl -a always,exit -F arch=b64 -F path=/home/sysupdate/.ssh/authorized_keys -F perm=wa -F key=ssh_keys
sudo auditctl -l
# modifica el archivo (por ejemplo, agrega una línea en blanco) y luego:
sudo ausearch -k ssh_keys -i
```

`perm=wa` vigila escrituras (`w`) y cambios de atributos (`a`); `-F key=` etiqueta los registros para buscarlos después. La sintaxis de las reglas está en la página `auditctl(8)` (man7.org). Si el equipo la adopta, es una mejora que sale de esta cacería.

### Paso 15 - Limpieza

```bash
sudo crontab -r -u sysupdate
sudo rm -f /etc/cron.d/healthd /var/tmp/healthd.log
sudo rm -rf /opt/.cache /tmp/.u
sudo userdel -r sysupdate
sudo auditctl -D 2>/dev/null || true
reg "limpieza terminada"
```

Comprueba contra tu línea base:

```bash
getent passwd | awk -F: '$3>=1000 && $3<65534 {print $1}' | sort | diff - base/cuentas.txt && echo "cuentas: igual que la base"
ls /etc/cron.d | diff - base/cron-d.txt && echo "cron.d: igual que la base"
sudo test ! -e /home/sysupdate && echo "sin carpeta de sysupdate"
```

En `kali`: `rm -f ~/.ssh/lab_persist ~/.ssh/lab_persist.pub`.

### Paso 16 - Las fichas

Llena `ficha-cacerias.md` (formato en la tarea) con:

| Ficha | Técnica | Resultado |
|---|---|---|
| 0 | T1110.001 | La entrada: 30 fallos y un éxito desde la misma IP |
| 1 | T1136.001 | `sysupdate` sin cambio aprobado |
| 2 | T1053.003 | Dos persistencias; la consulta A solo ve una |
| 3 | T1098.004 | Una huella fuera de la línea base |
| 4 | T1021.004 | Cero resultados, con su lectura de cobertura |

---

## Cuando algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| Las consultas no regresan nada con `eventos-vivo.log` | La zona horaria no es UTC o el formato de rsyslog es el tradicional | `head -1 /var/log/auth.log`; adapta las expresiones regulares y anótalo |
| No hay `CRON ... CMD` en `auth.log` | En Ubuntu esas líneas van a `/var/log/syslog` | Por eso se juntan ambos archivos |
| `healthd` no se ejecuta | El archivo de `/etc/cron.d` no es de `root`, es escribible por otros o su formato no trae el campo de usuario | `ls -l /etc/cron.d/healthd` y `man cron` |
| El login con la llave falla | Permisos de `~/.ssh` o de `authorized_keys` | `sudo tail /var/log/auth.log`: sshd dice por qué (por ejemplo `bad ownership or modes`) |
| `crontab -u` pide permiso | Se corrió sin `sudo` | Antepón `sudo` |

---

## Checklist de salida

- [ ] Tienes tu `registro.txt` con horas UTC de cada acción.
- [ ] Las cinco fichas están llenas, con consulta y lectura.
- [ ] Viste fallar, con tus propios datos, la consulta A de cron y la búsqueda de `authorized_keys`.
- [ ] La limpieza dejó `victima` igual que tu línea base.
- [ ] Guardaste las consultas en un archivo para la tarea.

