# Práctica - S04 - Evidencia y análisis forense

**Defensa y respuesta a incidentes**
**Duración:** 70 minutos, por equipo, en su laboratorio.

---

## Objetivo

Reunir y analizar la evidencia de disco del incidente de la S03: adquirir una imagen con su suma de verificación, **verla fallar** cuando se repara la copia, leerla sin montarla, construir una línea de tiempo en UTC y llenar la cadena de custodia.

> **Marco legal y ético.** Todo ocurre sobre **tu** laboratorio y datos **ficticios**, con la carta de autorización del curso de Ciberseguridad. Nada de esto se hace sobre un equipo que no sea tuyo.

**Caso hipotético:** el incidente, las personas y los datos son inventados para enseñar. Las horas son las de la **referencia del curso** (10 de noviembre de 2026, UTC); si hiciste el simulacro de la S03 en vivo, tus horas serán otras.

Hay dos caminos y los dos llegan al mismo análisis:

| Camino | Cuándo | Qué obtienes |
|---|---|---|
| **A. La VM de la S03** | Terminaste el simulacro y conservas `victima` tras contener, antes de erradicar | Tu propia imagen, con tus horas |
| **B. El generador** | No tienes la VM o quieres reproducir la referencia | Una imagen ext4 de 16 MiB con el incidente de referencia |

---

## Antes de empezar

Necesitas en cualquier Linux (la VM de Kali sirve): `bash`, `python3`, `e2fsprogs` (`mke2fs`, `debugfs`, `e2fsck`, `dumpe2fs`) y `coreutils`. No se necesita `root` para nada del camino B.

```bash
python3 --version
debugfs -V 2>&1 | head -1
mke2fs -V 2>&1 | head -1
mkdir -p ~/s04 && cd ~/s04
```

La salida de este material se obtuvo con `e2fsprogs` 1.47.2 y Python 3.14. Con otras versiones el formato de `debugfs` puede variar un poco: si el script de la línea de tiempo no encuentra las fechas, revisa el texto de `debugfs -R "stat /etc" imagen.dd`.

---

## Paso 1 - Preparar el escenario

### Camino B: el generador

Guarda este script como `generar-escenario.sh`. Arma, sin root, una imagen ext4 desde una carpeta con los archivos del incidente (el `auth.log` de la S03, las dos persistencias, el `.tgz` y las marcas de la tarea):

```bash
#!/usr/bin/env bash
# generar-escenario.sh - arma la imagen de practica del incidente de la sesion 3.
# Caso hipotetico: todo es ficticio. No necesita root: mke2fs -d llena la imagen desde una carpeta.
set -euo pipefail
D="${1:-$HOME/s04/escenario}"
mkdir -p "$D" && cd "$D"
rm -rf tree victima-disco.dd
mkdir -p tree/home/soporte/.ssh tree/home/admin/.ssh tree/etc/cron.d tree/etc/ufw \
         tree/var/tmp/.cache tree/var/log tree/srv/datos

cat > tree/var/log/auth.log <<'LOG'
2026-11-09T18:02:11.482311+00:00 victima sshd[1180]: Accepted publickey for admin from 192.168.56.50 port 40122 ssh2: ED25519 SHA256:Hq3u0b0dYpM0n3m4mP7kLwq3F1xZrC2eV9bT5sN8aGE
2026-11-09T18:02:11.490877+00:00 victima sshd[1180]: pam_unix(sshd:session): session opened for user admin(uid=1000) by (uid=0)
2026-11-09T18:41:30.115002+00:00 victima sshd[1180]: pam_unix(sshd:session): session closed for user admin
2026-11-10T02:30:41.201334+00:00 victima sshd[2203]: Invalid user ubuntu from 192.168.56.10 port 52140
2026-11-10T02:30:43.987120+00:00 victima sshd[2203]: Failed password for invalid user ubuntu from 192.168.56.10 port 52140 ssh2
2026-11-10T02:31:02.554310+00:00 victima sshd[2207]: Failed password for soporte from 192.168.56.10 port 52142 ssh2
2026-11-10T02:31:17.318456+00:00 victima sshd[2211]: Accepted password for soporte from 192.168.56.10 port 52144 ssh2
2026-11-10T02:31:17.325019+00:00 victima sshd[2211]: pam_unix(sshd:session): session opened for user soporte(uid=1001) by (uid=0)
2026-11-10T02:31:17.340871+00:00 victima systemd-logind[702]: New session 14 of user soporte.
2026-11-10T02:33:02.774120+00:00 victima sudo:  soporte : TTY=pts/0 ; PWD=/home/soporte ; USER=root ; COMMAND=/usr/bin/ls -la /srv/datos
2026-11-10T02:33:02.776201+00:00 victima sudo: pam_unix(sudo:session): session opened for user root(uid=0) by soporte(uid=1001)
2026-11-10T02:33:02.801944+00:00 victima sudo: pam_unix(sudo:session): session closed for user root
2026-11-10T02:41:50.330671+00:00 victima sudo:  soporte : TTY=pts/0 ; PWD=/home/soporte ; USER=root ; COMMAND=/usr/bin/tee /etc/cron.d/sync-cache
2026-11-10T02:41:50.332115+00:00 victima sudo: pam_unix(sudo:session): session opened for user root(uid=0) by soporte(uid=1001)
2026-11-10T02:41:50.360012+00:00 victima sudo: pam_unix(sudo:session): session closed for user root
2026-11-10T02:43:12.912480+00:00 victima sudo:  soporte : TTY=pts/0 ; PWD=/home/soporte ; USER=root ; COMMAND=/usr/bin/tar czf /var/tmp/.cache/datos.tgz /srv/datos
2026-11-10T02:43:12.914003+00:00 victima sudo: pam_unix(sudo:session): session opened for user root(uid=0) by soporte(uid=1001)
2026-11-10T02:43:12.980377+00:00 victima sudo: pam_unix(sudo:session): session closed for user root
2026-11-10T03:02:05.611204+00:00 victima sshd[2402]: Accepted publickey for admin from 192.168.56.50 port 40310 ssh2: ED25519 SHA256:Hq3u0b0dYpM0n3m4mP7kLwq3F1xZrC2eV9bT5sN8aGE
2026-11-10T03:02:05.620118+00:00 victima sshd[2402]: pam_unix(sshd:session): session opened for user admin(uid=1000) by (uid=0)
2026-11-10T03:02:05.640331+00:00 victima systemd-logind[702]: New session 15 of user admin.
2026-11-10T03:04:30.100000+00:00 victima sudo:  admin : TTY=pts/1 ; PWD=/home/admin ; USER=root ; COMMAND=/usr/bin/ss -tanp
2026-11-10T03:04:30.102000+00:00 victima sudo: pam_unix(sudo:session): session opened for user root(uid=0) by admin(uid=1000)
2026-11-10T03:04:30.140000+00:00 victima sudo: pam_unix(sudo:session): session closed for user root
2026-11-10T03:06:10.100000+00:00 victima sudo:  admin : TTY=pts/1 ; PWD=/home/admin ; USER=root ; COMMAND=/usr/local/bin/avml /mnt/evidencia/victima-mem.lime
2026-11-10T03:06:10.102000+00:00 victima sudo: pam_unix(sudo:session): session opened for user root(uid=0) by admin(uid=1000)
2026-11-10T03:06:10.140000+00:00 victima sudo: pam_unix(sudo:session): session closed for user root
2026-11-10T03:15:10.100000+00:00 victima sudo:  admin : TTY=pts/1 ; PWD=/home/admin ; USER=root ; COMMAND=/usr/sbin/ufw deny from 192.168.56.10
2026-11-10T03:15:10.102000+00:00 victima sudo: pam_unix(sudo:session): session opened for user root(uid=0) by admin(uid=1000)
2026-11-10T03:15:10.140000+00:00 victima sudo: pam_unix(sudo:session): session closed for user root
2026-11-10T03:15:40.100000+00:00 victima sudo:  admin : TTY=pts/1 ; PWD=/home/admin ; USER=root ; COMMAND=/usr/bin/loginctl terminate-session 14
2026-11-10T03:15:40.102000+00:00 victima sudo: pam_unix(sudo:session): session opened for user root(uid=0) by admin(uid=1000)
2026-11-10T03:15:40.140000+00:00 victima sudo: pam_unix(sudo:session): session closed for user root
2026-11-10T03:15:40.402118+00:00 victima systemd-logind[702]: Session 14 logged out. Waiting for processes to exit.
2026-11-10T03:15:41.215903+00:00 victima sshd[2211]: pam_unix(sshd:session): session closed for user soporte
2026-11-10T03:15:41.230777+00:00 victima systemd-logind[702]: Removed session 14.
LOG

echo "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIEjemploDeLaboratorioAdminNoEsUnaLlaveReal admin@estacion-admin" > tree/home/admin/.ssh/authorized_keys
echo "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIEjemploDeLaboratorioAtacanteNoEsUnaLlaveReal soporte@kali" > tree/home/soporte/.ssh/authorized_keys
echo '*/5 * * * * root /var/tmp/.cache/sync.sh >/dev/null 2>&1' > tree/etc/cron.d/sync-cache
printf '#!/bin/sh\n# Ejemplo ilustrativo e inofensivo: solo deja una marca de hora\ndate -u +%%FT%%TZ >> /var/tmp/.cache/.alive\n' > tree/var/tmp/.cache/sync.sh
chmod 755 tree/var/tmp/.cache/sync.sh
printf 'id,nombre,saldo\n1,Cliente Ficticio A,100\n2,Cliente Ficticio B,250\n' > tree/srv/datos/clientes.csv
tar czf tree/var/tmp/.cache/datos.tgz -C tree srv/datos
printf '# Muestra ilustrativa (no es el archivo completo de ufw)\n### RULES ###\n-A ufw-user-input -s 192.168.56.10 -j DROP\n### END RULES ###\n' > tree/etc/ufw/user.rules
python3 - <<'PY'
import datetime
t = datetime.datetime(2026, 11, 10, 2, 45)
filas = []
while t <= datetime.datetime(2026, 11, 10, 3, 15):
    filas.append(t.strftime("%Y-%m-%dT%H:%M:%SZ")); t += datetime.timedelta(minutes=5)
open("tree/var/tmp/.cache/.alive", "w").write("\n".join(filas) + "\n")
PY

# Fechas del incidente (UTC). TZ=UTC para que touch -d no use la zona del equipo.
export TZ=UTC
touch -d "2026-11-08 10:00:00" tree/srv/datos/clientes.csv tree/home/admin/.ssh/authorized_keys
touch -d "2026-11-10 02:36:10" tree/home/soporte/.ssh/authorized_keys
touch -d "2026-11-10 02:41:50" tree/etc/cron.d/sync-cache
touch -d "2026-11-10 02:42:20" tree/var/tmp/.cache/sync.sh
touch -d "2026-11-10 02:43:12" tree/var/tmp/.cache/datos.tgz
touch -d "2026-11-10 03:15:00" tree/var/tmp/.cache/.alive
touch -d "2026-11-10 03:15:10" tree/etc/ufw/user.rules
touch -d "2026-11-10 03:15:41" tree/var/log/auth.log
find tree -type d -exec touch -d "2026-11-10 03:15:41" {} +

mke2fs -q -t ext4 -L VICTIMA -d tree -F victima-disco.dd 16M
echo "Imagen lista: $D/victima-disco.dd"
ls -l victima-disco.dd
```

```bash
bash generar-escenario.sh ~/s04/escenario
cd ~/s04/escenario
file victima-disco.dd
```

`file` debe decir `Linux rev 1.0 ext4 filesystem data` y el volumen `VICTIMA`.

### Camino A: tu VM

En tu laboratorio, con la memoria ya capturada en la S03 (si no la capturaste, anótalo en la cadena de custodia como "no se capturó"), **apaga** `victima` y clona su disco:

```bash
# Procedimiento segun el manual de VirtualBox (VBoxManage), no ejecutado al preparar este material.
VBoxManage clonemedium disk victima.vdi victima-disco.dd --format RAW
```

Trabaja con `victima-disco.dd`. Para el Paso 3 en adelante, la imagen debe ser un sistema de archivos ext4 que `debugfs` abra; si tu VM usa LVM o particiones, `debugfs` necesita el desplazamiento de la partición y conviene usar el camino B para esta práctica.

---

## Paso 2 - Adquirir y sellar

La **original** es `victima-disco.dd`. Primero el hash, antes de cualquier otra operación:

```bash
date -u +%FT%TZ                                  # hora UTC de la adquisicion: anotala
sha256sum victima-disco.dd | tee victima-disco.dd.sha256
```

```
1270825e591b9bb338a231b20353de25c566d73a9e807693b63530967798387e  victima-disco.dd
```

(El hash de tu imagen será distinto: `mke2fs` asigna un UUID y marcas de hora nuevos cada vez. Eso no es un error: es una razón más para anotar **tu** hash.)

Ahora una **copia de trabajo**; la original se sella:

```bash
cp victima-disco.dd copia-trabajo.dd
sha256sum victima-disco.dd copia-trabajo.dd
sha256sum -c victima-disco.dd.sha256
chmod a-w victima-disco.dd victima-disco.dd.sha256     # la original, solo lectura
ls -l victima-disco.dd victima-disco.dd.sha256
```

Los dos hashes deben ser **idénticos** y `sha256sum -c` debe decir `OK`. Anótalos en la cadena de custodia (Paso 6).

---

## Paso 3 - Verlo fallar: el hash que ya no coincide

Una "reparación" aparentemente inocente sobre la **copia**:

```bash
e2fsck -fn copia-trabajo.dd | tail -2        # -n: solo lectura
sha256sum copia-trabajo.dd | cut -c1-24      # el mismo hash

e2fsck -fy copia-trabajo.dd | tail -2        # -y: repara y ESCRIBE
sha256sum copia-trabajo.dd                   # otro hash
```

Ahora verifica contra la original, y contra la lista aplicada a la copia:

```bash
sha256sum -c victima-disco.dd.sha256
sed 's/victima-disco.dd/copia-trabajo.dd/' victima-disco.dd.sha256 | sha256sum -c
```

```
victima-disco.dd: OK
copia-trabajo.dd: FAILED
sha256sum: WARNING: 1 computed checksum did NOT match
```

### ¿Qué cambió?

```bash
cmp -l victima-disco.dd copia-trabajo.dd | wc -l
for f in victima-disco.dd copia-trabajo.dd; do echo "== $f"; dumpe2fs -h $f 2>/dev/null | grep -E "Last write time|Last checked"; done
```

En la referencia fueron **9 bytes** y las líneas `Last write time` y `Last checked` quedaron con la hora de la reparación. Ningún byte es "contenido" del atacante: son metadatos del propio sistema de archivos. Aun así, **la copia dejó de poder demostrarse idéntica**.

Contesta por escrito (3 a 5 renglones):

1. ¿Qué habría pasado si esta copia hubiera sido la **única** y la original se hubiera descartado?
2. ¿Qué habría pasado si el hash se hubiera calculado **después** de la reparación?
3. ¿Cómo se recupera la situación ahora? (pista: tienes la original sellada).

Recupera la copia de trabajo y comprueba:

```bash
cp victima-disco.dd copia-trabajo.dd
sha256sum -c victima-disco.dd.sha256 && echo "copia-trabajo vuelve a ser fiel"
sed 's/victima-disco.dd/copia-trabajo.dd/' victima-disco.dd.sha256 | sha256sum -c
```

---

## Paso 4 - Leer la imagen sin montarla

`debugfs` abre la imagen en **solo lectura** salvo que se le pase `-w`. Con él puedes leer archivos de la copia sin ser root y sin reproducir el journal.

```bash
debugfs -R "ls -l /etc/cron.d" copia-trabajo.dd
debugfs -R "cat /etc/cron.d/sync-cache" copia-trabajo.dd
debugfs -R "cat /home/soporte/.ssh/authorized_keys" copia-trabajo.dd
debugfs -R "ls -l /var/tmp/.cache" copia-trabajo.dd
debugfs -R "cat /var/tmp/.cache/.alive" copia-trabajo.dd | tail -3
```

```
     15  100664 (1)   1000   1000      57 10-Nov-2026 02:41 sync-cache
*/5 * * * * root /var/tmp/.cache/sync.sh >/dev/null 2>&1
ssh-ed25519 AAAA...  soporte@kali
```

Responde, con la salida de tu imagen:

- ¿Qué persistencias hay y cuándo se modificó cada una?
- La tarea programada corre cada cinco minutos: ¿cuál es el último valor de `.alive` y qué hora dice?
- ¿Hay `.bash_history` de `soporte`? ¿Por qué podría faltar? (pista: la sesión se terminó por la fuerza).

### Extraer el `auth.log` para analizarlo

```bash
debugfs -R "cat /var/log/auth.log" copia-trabajo.dd > auth.log
sha256sum auth.log                       # un hash para lo que extraes tambien
grep -E "Accepted|Failed|Invalid" auth.log | cut -c1-110
grep "COMMAND" auth.log | sed -E 's/^([^ ]+) .*(sudo:)  ?([a-z]+) : .*COMMAND=(.*)/\1 \3 \4/'
```

Contesta con tus datos: el primer acceso del atacante, sus comandos de `sudo`, y cuáles de los renglones de `sudo` son **del responsable** (sus propias huellas: recuérdalo para el reporte).

---

## Paso 5 - La línea de tiempo

Guarda este script como `linea-de-tiempo.py`. Recorre la imagen con `debugfs` (solo lectura), lee las marcas de cada archivo y las imprime ordenadas, en UTC:

```python
#!/usr/bin/env python3
"""Linea de tiempo de una imagen ext4 sin montarla: usa debugfs en solo lectura.
Uso: linea-de-tiempo.py imagen.dd > linea.csv
Salida: fecha_UTC,tipo(M/A/C/B),ruta,tamano
"""
import re, subprocess, sys, datetime

img = sys.argv[1]

def debugfs(orden):
    # debugfs abre la imagen en solo lectura salvo que se le pase -w
    r = subprocess.run(["debugfs", "-R", orden, img], capture_output=True, text=True)
    return r.stdout

def recorrer(ruta):
    for linea in debugfs(f"ls -p {ruta}").splitlines():
        partes = linea.split("/")
        if len(partes) < 7:
            continue
        modo, nombre, tam = partes[2], partes[5], partes[6]
        if nombre in (".", "..", "lost+found"):
            continue
        hijo = ruta.rstrip("/") + "/" + nombre
        yield hijo, modo, tam
        if modo.startswith("040"):
            yield from recorrer(hijo)

TIPOS = {"mtime": "M", "atime": "A", "ctime": "C", "crtime": "B"}
filas = []
for ruta, modo, tam in recorrer("/"):
    salida = debugfs(f"stat {ruta}")
    for campo, letra in TIPOS.items():
        m = re.search(rf"^\s*{campo}: 0x([0-9a-f]+):", salida, re.M)
        if m:
            t = datetime.datetime.fromtimestamp(int(m.group(1), 16), datetime.timezone.utc)
            filas.append((t.strftime("%Y-%m-%dT%H:%M:%SZ"), letra, ruta, tam))
for f in sorted(filas):
    print(",".join(f))
```

```bash
python3 linea-de-tiempo.py copia-trabajo.dd > linea.csv
wc -l linea.csv
awk -F, '$2=="M" && $1>="2026-11-10T02:00" && $1<"2026-11-11" && $4!=""' linea.csv
```

```
2026-11-10T02:36:10Z,M,/home/soporte/.ssh/authorized_keys,95
2026-11-10T02:41:50Z,M,/etc/cron.d/sync-cache,57
2026-11-10T02:42:20Z,M,/var/tmp/.cache/sync.sh,116
2026-11-10T02:43:12Z,M,/var/tmp/.cache/datos.tgz,208
2026-11-10T03:15:00Z,M,/var/tmp/.cache/.alive,147
2026-11-10T03:15:10Z,M,/etc/ufw/user.rules,132
2026-11-10T03:15:41Z,M,/var/log/auth.log,4411
```

(El tamaño de `datos.tgz` y de `user.rules` puede variar un poco con la versión de `gzip` o del generador.)

### Correlacionar con el `auth.log`

Arma una tabla con **diez filas o más**, en UTC, con las dos fuentes lado a lado:

| Hora UTC | Fuente 1 (bitácora) | Fuente 2 (fecha de archivo) | Qué concluyes |
|---|---|---|---|
| 02:41:50 | `sudo ... tee /etc/cron.d/sync-cache` | mtime de `/etc/cron.d/sync-cache` | `soporte` creó la tarea a esa hora |
| 02:43:12 | `sudo ... tar czf /var/tmp/.cache/datos.tgz /srv/datos` | mtime de `datos.tgz` | Prepararon los datos |
| 03:15:10 | `sudo admin ... ufw deny from 192.168.56.10` | mtime de `/etc/ufw/user.rules` | **El responsable** bloqueó la IP |
| ... | | | |

Tres preguntas para tu tabla:

1. ¿Hay **algún** archivo con `mtime` entre 02:31 y 03:15 que **no** aparezca en el `auth.log`? ¿Qué significa?
2. Las marcas `ctime` y `crtime` de la imagen, ¿coinciden con el incidente? (Mira `debugfs -R "stat /etc/cron.d/sync-cache" copia-trabajo.dd`.) ¿Por qué no?
3. La tarea escribió por última vez a las 03:15:00 y el responsable actuó a las 03:15:10. ¿Qué dice eso sobre la contención?

### Lo que NO demuestra

Escribe una lista de cinco cosas que esta evidencia **no** permite concluir. Por ejemplo: "no demuestra que `clientes.csv` se leyó" (mira el `atime` en `debugfs -R "stat /srv/datos/clientes.csv"`: ¿qué dice y por qué no basta?).

---

## Paso 6 - La cadena de custodia

Llena esta ficha **al momento**, no al final:

| Campo | Valor |
|---|---|
| Identificador | `EV-2026-11-10-001` |
| Descripción | Imagen del disco de `victima` |
| Origen | |
| Recolectó (nombre) | |
| Fecha y hora UTC de la adquisición | |
| Método y herramienta (versiones) | |
| Algoritmo y hash | SHA-256 |
| Medio de almacenamiento | |

Y la tabla de traspasos, con **al menos dos** movimientos (por ejemplo: de quien adquirió al custodio, y del custodio al analista):

| Fecha y hora UTC | Entrega | Recibe | Motivo | Hash verificado (sí/no) |
|---|---|---|---|---|
| | | | | |

Reglas del ejercicio:

- Un solo **custodio** por equipo.
- Cada traspaso se hace con `sha256sum -c` en presencia de quien recibe, y se anota.
- El análisis siempre se hace sobre la **copia**.

---

## Reto 1: otra imagen, la misma historia

Con el generador, construye **dos** imágenes y compara sus hashes. ¿Por qué no coinciden si el contenido es el mismo? Contesta con `dumpe2fs -h` en las dos (pista: UUID y fechas de creación).

## Reto 2: un archivo borrado

En una **copia** (nunca en la original), con `debugfs -w`, borra `/var/tmp/.cache/datos.tgz` y comprueba:

```bash
cp victima-disco.dd borrado.dd
debugfs -w -R "rm /var/tmp/.cache/datos.tgz" borrado.dd
debugfs -R "ls -l -d /var/tmp/.cache" borrado.dd
```

En la referencia, el listado muestra una entrada sin inodo, `<     0>  ...  datos.tgz`, y esto:

```
 Inode  Owner  Mode    Size      Blocks   Time deleted
    34   1000 100664    208      1/     1 Wed Nov 11 13:19:35 2026
1 deleted inodes found.
```

¿Qué te dice `lsdel`, y por qué hay que reunir la imagen **antes** de que el atacante, el responsable o el propio sistema reutilicen el espacio? Cuéntalo en tus palabras. (Esto es lo que The Sleuth Kit hace a gran escala con los archivos borrados; aquí lo ves con una herramienta mínima.)

---

## Entrega de la práctica

En una carpeta `s04/` por equipo:

1. `adquisicion.md`: la hora UTC, los comandos y los dos hashes del Paso 2.
2. `hash-roto.md`: tus respuestas del Paso 3, con la salida de `cmp` y `dumpe2fs`.
3. `linea.csv` y `tabla-correlacion.md`: tu línea de tiempo y la tabla de diez filas o más.
4. `no-demuestra.md`: cinco cosas que la evidencia no permite concluir.
5. `cadena-de-custodia.md`: la ficha y los traspasos.

```bash
history | tail -60 > historial-s04.txt
```

<!-- Se entrega por equipo. Revisa en voz alta, en al menos un equipo, el hash roto: que digan sin ayuda por qué ya no se puede defender esa copia. -->
