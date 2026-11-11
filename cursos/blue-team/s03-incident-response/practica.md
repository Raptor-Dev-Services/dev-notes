# Práctica - S03 - Responder a un incidente

**Defensa y respuesta a incidentes**
**Duración:** 80 minutos, por equipo de tres, en su laboratorio.

---

## Objetivo

Conducir la respuesta a un incidente simulado en `victima`: detectar, preservar, contener, erradicar y recuperar, con una bitácora con hora de cada paso. Antes, ver fallar a propósito dos maneras de contener mal.

> **Marco legal y ético.** Todo ocurre en **tu** laboratorio, entre máquinas **tuyas** en la red aislada `192.168.56.0/24`, con la carta de autorización del curso de Ciberseguridad firmada. El "atacante" es un compañero de equipo que ejecuta un guion benigno. Las contraseñas, usuarios, IP y datos son **ficticios**. Nada de esto se prueba contra un sistema que no sea tuyo.

**Caso hipotético:** las personas, equipos y datos de este ejercicio son inventados para enseñar.

---

## Roles del equipo

| Rol | Quién | Dónde trabaja |
|---|---|---|
| **A** Atacante | Un integrante | `kali` (192.168.56.10) |
| **R** Responsable técnico | Un integrante | La estación del admin (192.168.56.50), por SSH con llave a `victima` |
| **E** Escriba y líder | Un integrante | Una hoja o archivo de bitácora, con reloj UTC visible |

R y E **no saben** qué hará A. A no dice nada hasta que R o E lo descubran.

---

## Antes de empezar (lo hace el instructor o el equipo, 10 minutos)

En `victima` (Ubuntu Server 24.04):

```bash
# 1) el servicio de bitacoras y SSH
ls -l /var/log/auth.log || sudo apt install -y rsyslog
systemctl status ssh --no-pager | head -3

# 2) un usuario de soporte mal configurado a proposito (Caso hipotetico)
sudo useradd -m -s /bin/bash soporte
echo 'soporte:<CONTRASENA_DE_LABORATORIO>' | sudo chpasswd
sudo usermod -aG sudo soporte

# 3) datos ficticios
sudo mkdir -p /srv/datos
printf 'id,nombre,saldo\n1,Cliente Ficticio A,100\n2,Cliente Ficticio B,250\n' | sudo tee /srv/datos/clientes.csv >/dev/null

# 4) un disco aparte para guardar evidencia (disco virtual de 2 GB, ya montado en /mnt/evidencia)
findmnt /mnt/evidencia || echo "FALTA: agrega un segundo disco virtual y montalo en /mnt/evidencia"

# 5) apuntar la hora del sistema
timedatectl | grep -E "Time zone|System clock"
```

- Sustituye `<CONTRASENA_DE_LABORATORIO>` por una de laboratorio que **no uses en ningún otro sitio**.
- El usuario `soporte` en el grupo `sudo` es el error de diseño que explota el ejercicio: se corrige en la tarea.
- Toma una **instantánea** de `victima` con nombre `limpia`.

En Ubuntu 24.04 la unidad de SSH se llama `ssh` y, además, usa activación por socket (`ssh.socket`). Para este laboratorio no hace falta cambiar nada.

---

## Ronda 1 - Hacerlo mal a propósito

### Paso 1 - El guion del atacante (solo A)

A prepara una llave solo para este ejercicio, abre una sesión desde `kali` con la contraseña de laboratorio de `soporte` y ejecuta, **sin prisa**, estos pasos. Los comandos son inofensivos: dejan marcas, no dañan nada.

```bash
ssh-keygen -t ed25519 -f ~/.ssh/lab_soporte -N ""     # en kali; <LLAVE_PUBLICA_DE_KALI> es el contenido de lab_soporte.pub
ssh soporte@192.168.56.20
id
sudo ls -la /srv/datos

# persistencia 1: una llave propia
mkdir -p ~/.ssh && chmod 700 ~/.ssh
echo "<LLAVE_PUBLICA_DE_KALI>" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

# persistencia 2: una tarea programada inofensiva
sudo mkdir -p /var/tmp/.cache
printf '#!/bin/sh\ndate -u +%%FT%%TZ >> /var/tmp/.cache/.alive\n' | sudo tee /var/tmp/.cache/sync.sh >/dev/null
sudo chmod 755 /var/tmp/.cache/sync.sh
echo '*/5 * * * * root /var/tmp/.cache/sync.sh >/dev/null 2>&1' | sudo tee /etc/cron.d/sync-cache >/dev/null

# datos preparados para "sacar"
sudo tar czf /var/tmp/.cache/datos.tgz /srv/datos

# un servicio que escucha, solo en memoria
python3 -m http.server 8081 --directory /var/tmp/.cache &
```

A **deja la sesión abierta** y avisa a E: "ya empecé". Hora de inicio anotada por E.

### Paso 2 - Detectar (R y E)

Desde la estación del admin, R se conecta a `victima` y busca. E anota la hora de cada comando.

```bash
ssh admin@192.168.56.20

# ¿quién entro y desde donde?
sudo journalctl -u ssh --since "20 min ago" --no-pager | grep -E "Accepted|Failed|Invalid"
who -a
sudo ss -tanp | grep -v 127.0.0.1

# ¿esa IP la conociamos?
sudo grep -h "Accepted" /var/log/auth.log | awk '{print $9}' | sort | uniq -c
```

Debe aparecer una conexión **establecida** desde `192.168.56.10` y un puerto 8081 escuchando. E aplica los criterios de la clase y **declara el incidente** con la hora. Primera fila de la bitácora.

### Paso 3 - Verlo fallar 1: contener a la mala

R **reinicia** `victima`, como haría quien quiere "limpiar":

```bash
sudo reboot
```

Cuando `victima` vuelve (1 o 2 minutos), R comprueba qué se perdió:

```bash
sudo ss -tanp | grep -E "192.168.56.10|8081"   # ¿sigue la sesion? ¿sigue el puerto 8081?
sudo ps -eo pid,user,lstart,cmd | grep -E "http.server"
who -a
```

Anota en la bitácora, con tus palabras, **qué desapareció**: la sesión, el servicio en memoria, el rastro de procesos. Y comprueba qué **no** desapareció:

```bash
ls -l /home/soporte/.ssh/authorized_keys /etc/cron.d/sync-cache /var/tmp/.cache/
```

### Paso 4 - Verlo fallar 2: bloquear solo la contraseña

R "bloquea" a `soporte`:

```bash
sudo passwd -l soporte
sudo passwd -S soporte            # L = bloqueada
```

A intenta volver a entrar **con la llave**:

```bash
ssh -i ~/.ssh/lab_soporte soporte@192.168.56.20
```

**Anota lo que pasa.** Según el manual de `passwd`, `-l` invalida la contraseña; la llave es otro camino. Documentación de la comunidad dice que un bloqueo de contraseña no detiene la autenticación por llave pública: aquí lo compruebas en tu propia máquina. Si te deja entrar, esa es la lección; si no te deja, anota tu versión de OpenSSH y tu configuración de PAM, porque ahí está la explicación.

Y comprueba que la tarea programada sigue viva:

```bash
sleep 330; sudo tail -3 /var/tmp/.cache/.alive     # la marca de hora sigue avanzando cada 5 minutos
```

### Paso 5 - Restaurar la instantánea

Con la instantánea `limpia` se vuelve al punto inicial. E escribe en una frase por qué la ronda 1 **no** fue una respuesta: qué se perdió, qué se dejó adentro.

---

## Ronda 2 - Hacerlo bien

A repite **exactamente** el guion del Paso 1 (otra vez desde la instantánea).

### Paso 6 - Detectar y declarar (ronda 2)

R y E repiten el Paso 2. Esta vez, E empieza la **bitácora** con esta plantilla:

| Hora UTC | Quién | Acción | Resultado | Evidencia |
|---|---|---|---|---|
| | | | | |

Y escribe quién es líder, quién técnico, quién comunica. La hora de cada comando sale de `date -u +%FT%TZ`.

### Paso 7 - Preservar lo volátil, antes de tocar nada

```bash
sudo mkdir -p /mnt/evidencia/s03
date -u +%FT%TZ | sudo tee /mnt/evidencia/s03/hora-inicio.txt

sudo ss -tanp | sudo tee /mnt/evidencia/s03/ss-tanp.txt >/dev/null
sudo ss -tlnp | sudo tee /mnt/evidencia/s03/ss-tlnp.txt >/dev/null
sudo ps -eo pid,ppid,user,lstart,cmd --sort=lstart | sudo tee /mnt/evidencia/s03/ps.txt >/dev/null
who -a | sudo tee /mnt/evidencia/s03/who.txt >/dev/null
sudo journalctl -u ssh --since "2 hours ago" --no-pager | sudo tee /mnt/evidencia/s03/ssh-journal.txt >/dev/null
sudo cp /var/log/auth.log /mnt/evidencia/s03/auth.log
( cd /mnt/evidencia/s03 && sudo sha256sum * | sudo tee SHA256SUMS >/dev/null )
```

La suma de verificación de la última línea sella lo reunido; en la sesión 4 se explica por qué. Cada comando va a la bitácora con su hora.

Del puerto 8081 que escucha, lee el PID en `ss-tlnp.txt` (el campo `pid=`) y anota qué proceso es:

```bash
sudo ls -l /proc/<PID>/exe
sudo cat /proc/<PID>/cmdline | tr '\0' ' '; echo
```

En este guion el servicio es `python3`, un binario del sistema, así que `exe` no termina en `(deleted)`. La técnica de recuperar un binario borrado se practica por separado en el ejercicio opcional.

### Paso 8 - Contener sin apagar

```bash
sudo ufw status                           # inactive = sin reglas activas
sudo ufw allow from 192.168.56.50 to any port 22 proto tcp
sudo ufw deny from 192.168.56.10
sudo ufw --force enable
sudo ufw status numbered
sudo ss -tanp | grep 192.168.56.10       # ¿la conexion ya establecida sigue viva despues de la regla?
loginctl list-sessions
sudo loginctl terminate-session <ID_DE_LA_SESION_DE_SOPORTE>
sudo ss -tanp | grep 192.168.56.10       # debe salir vacio
```

- El orden importa: **primero** permitir tu estación, **luego** bloquear y **al final** activar el firewall.
- Anota lo que viste con la conexión ya establecida: esa observación es la razón por la que la sesión se termina aparte, con `loginctl`.
- Anota en la bitácora la hora de cada línea.
- Pregúntate: ¿el servicio de 8081 sigue vivo? ¿Quién lo mata, y cuándo? Una decisión de E, no de R.

Cuando E lo autorice, se mata **por PID** después de haber copiado lo que se necesitaba:

```bash
sudo kill <PID_DEL_SERVICIO_8081>
```

### Paso 9 - Erradicar con el listado de persistencias

```bash
# llaves
sudo find /home /root -name authorized_keys -exec ls -l {} \;

# tareas programadas
ls -l /etc/cron.d /etc/cron.daily
sudo crontab -l -u soporte 2>/dev/null
systemctl list-timers --all --no-pager | head -20

# cuentas y grupos
getent passwd | awk -F: '$3>=1000 {print $1,$3,$6}'
getent group sudo

# archivos de la ventana del incidente (ajusta las horas a las de tu guion)
sudo find / -xdev -type f -newermt "<INICIO_DEL_GUION>" ! -newermt "<FIN_DEL_GUION>" \
  -printf '%TY-%Tm-%Td %TH:%TM:%.2TS %u %p\n' 2>/dev/null | sort

# servicios y puertos
systemctl list-unit-files --state=enabled --no-pager | head -30
sudo ss -tlnp
```

Por cada hallazgo, la bitácora lleva: qué es, dónde, a qué hora se modificó, y qué hiciste con él. **Antes de borrar** una persistencia, cópiala a `/mnt/evidencia/s03/` con su hash.

```bash
sudo mkdir -p /mnt/evidencia/s03/persistencia
sudo cp -p /home/soporte/.ssh/authorized_keys /etc/cron.d/sync-cache /var/tmp/.cache/sync.sh /mnt/evidencia/s03/persistencia/
( cd /mnt/evidencia/s03/persistencia && sudo sha256sum * | sudo tee SHA256SUMS >/dev/null )

sudo rm /etc/cron.d/sync-cache
sudo mv /home/soporte/.ssh/authorized_keys /mnt/evidencia/s03/persistencia/authorized_keys.retirada
sudo rm -r /var/tmp/.cache
sudo gpasswd -d soporte sudo
echo 'soporte:<OTRA_CONTRASENA_DE_LABORATORIO>' | sudo chpasswd
```

(`rm -r` sobre el directorio del atacante, una vez copiado lo que importaba.)

### Paso 10 - Recuperar y verificar

```bash
# seis minutos despues: la tarea ya no debe escribir
sleep 400; ls -l /var/tmp/.cache 2>&1 | head -2          # el directorio no existe

# ninguna conexion suya, ningun puerto extra, ninguna llave nueva
sudo ss -tanp | grep 192.168.56.10
sudo ss -tlnp
sudo find /home -name authorized_keys -exec ls -l {} \;

# los servicios legitimos siguen
systemctl is-active ssh
```

Y la prueba final: A intenta entrar otra vez, con la contraseña y con la llave. Las dos deben fallar. E declara el **fin de la recuperación** con su criterio escrito.

---

## Ejercicio opcional: el binario borrado

Para practicar la técnica de `/proc/<PID>/exe` sin riesgo, en cualquier Linux:

```bash
mkdir -p ~/s03 && cd ~/s03
cp /usr/bin/bash mal                      # un binario inofensivo con otro nombre
./mal -c 'read -t 120 < /dev/zero; sleep 120' &
PID=$!
ls -l /proc/$PID/exe                      # apunta a ~/s03/mal
rm mal
ls -l /proc/$PID/exe                      # ahora termina en  (deleted)
cp /proc/$PID/exe recuperado
sha256sum recuperado /usr/bin/bash        # los dos hashes coinciden
kill $PID
```

Probado al preparar el material: el enlace pasó a terminar en `(deleted)` y el hash del recuperado coincidió con el de `bash`.

---

## Reto 1: reglas para el SIEM

Con lo que aprendió el incidente, escribe tres criterios de detección que **habrían** sonado antes de las 02:43 (solo la lógica, en palabras; las reglas formales son de la sesión 2):

- "Acceso exitoso de una cuenta desde una IP que no está en la lista de conocidas."
- "Escritura en `/etc/cron.d` por una cuenta que no es de administración."
- "Un usuario del grupo `sudo` que nunca había usado `sudo` y lo usa tres veces en diez minutos."

Para cada una: ¿qué línea del `auth.log` la dispara?

## Reto 2: F3EAD de tu equipo

Llena la tabla con lo que **hizo** tu equipo, no con lo que debía:

| Fase | Qué hicimos | Qué faltó |
|---|---|---|
| Find | | |
| Fix | | |
| Finish | | |
| Exploit | | |
| Analyze | | |
| Disseminate | | |

---

## Entrega de la práctica

Por equipo, en una carpeta `s03/`:

1. `bitacora-ronda1.md` y `bitacora-ronda2.md`: las dos bitácoras con hora UTC.
2. `verlo-fallar.md`: qué desapareció y qué no en la ronda 1; qué pasó con `passwd -l`; tu versión de OpenSSH (`ssh -V`).
3. `persistencias.md`: cada persistencia hallada, con su comando, su hora de modificación y qué hiciste.
4. `evidencia/`: la carpeta de evidencia con `SHA256SUMS`.
5. `f3ead.md`: la tabla del Reto 2.

<!-- Se entrega por equipo. Revisa en clase la bitácora de la ronda 2 de al menos un equipo en voz alta: busca horas en blanco y acciones sin resultado. -->
