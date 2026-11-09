# Práctica - S06 - Tres incidentes sembrados

**Linux para desarrolladores**
**Duración estimada:** 2.5 horas

---

## Objetivo

Resolver tres incidentes que otro equipo siembra a escondidas en su VM (disco lleno por logs, servicio que no arranca y puerto ocupado) **siguiendo la lista de cinco minutos**, con bitácora, y comprobar al final que su procedimiento lo puede seguir alguien que no lo escribió.

La regla del curso: si no lo vieron fallar, no lo probaron. Hoy lo que se prueba es el método, y se ve fallar cuando otro equipo intenta usar su runbook.

---

## Antes de empezar

- Su VM con todo lo de las sesiones 4 y 5: `api-lab.service`, Nginx en el 80 y UFW.

```bash
systemctl is-active api-lab nginx
curl -s http://127.0.0.1/health; echo
# active
# active
# {"estado": "ok", "pid": 183}
```

- `lsof` instalado (`sudo apt install -y lsof` si falta).
- Parejas de equipos: A siembra en la VM de B y B en la de A. El equipo que siembra entra a la VM ajena con una cuenta que le presten para la práctica, y se sale al terminar de sembrar.
- Una bitácora abierta (un `.md` en el repo del equipo) donde anotan **hora, comando, lo que vieron y lo que decidieron**, mientras trabajan.

---

## Paso 1 - La línea base de su VM sana

Antes de que nadie siembre nada:

```bash
{
  echo "== uptime";  uptime; nproc
  echo "== memoria"; free -h
  echo "== disco";   df -h -x tmpfs -x squashfs; df -i /
  echo "== units";   systemctl --failed
  echo "== puertos"; sudo ss -ltnp
} > ~/linea-base.txt
cat ~/linea-base.txt
```

Guarden este archivo en el repo. Es lo "normal" contra lo que van a comparar.

---

## Paso 2 - El script para sembrar

Lo corre **el otro equipo** en su VM. Guárdenlo como `sembrar.sh` (lo pueden leer antes: no hay trampa en el código, la trampa es el diagnóstico).

```bash
#!/usr/bin/env bash
# sembrar.sh - Siembra UNO de los tres incidentes de la sesión 6 en la VM de laboratorio de OTRO equipo.
# Uso: sudo bash sembrar.sh disco|servicio|puerto|deshacer
# Requiere la API de la sesión 4 (usuario api-lab y api-lab.service). Solo para VM de laboratorio.
set -euo pipefail

[[ $EUID -eq 0 ]] || { echo "Córrelo con sudo." >&2; exit 1; }
id api-lab >/dev/null 2>&1 || { echo "No existe el usuario api-lab: falta la sesión 4." >&2; exit 1; }
systemctl cat api-lab.service >/dev/null 2>&1 || { echo "No existe api-lab.service: falta la sesión 4." >&2; exit 1; }

LAB=/var/lib/lab-s6
IMG=$LAB/registros.img

ticket() { printf '\n=== Ticket para el otro equipo (no le digas nada más) ===\n%s\n\n' "$1"; }

sembrar_disco() {
  install -d "$LAB" /srv/registros /usr/local/lib/lab-s6
  truncate -s 64M "$IMG"                          # un disco de juguete: llenarlo no arriesga la VM
  mkfs.ext4 -q -F -L registros "$IMG"
  mount -o loop "$IMG" /srv/registros
  chown api-lab:api-lab /srv/registros
  cat > /usr/local/lib/lab-s6/bitacora.sh <<'GUION'
#!/usr/bin/env bash
# Escribe un log sin control y sin rotación.
exec 3>>/srv/registros/api-lab.log
while :; do
  if ! { date '+%F %T'; head -c 400000 /dev/urandom | base64 -w 0; echo; } >&3 2>/dev/null; then
    echo "no pude escribir en el log" >&2
    sleep 10
  fi
  sleep 0.1
done
GUION
  chmod 755 /usr/local/lib/lab-s6/bitacora.sh
  cat > /etc/systemd/system/bitacora-lab.service <<'UNIT'
[Unit]
Description=Bitácora detallada de la API de laboratorio

[Service]
User=api-lab
ExecStart=/usr/local/lib/lab-s6/bitacora.sh
UNIT
  systemctl daemon-reload
  systemctl start bitacora-lab
  echo "Llenando el disco de registros..."
  local i
  for (( i = 0; i < 120; i++ )); do
    (( $(df --output=pcent /srv/registros | tail -1 | tr -dc '0-9') >= 99 )) && break
    sleep 1
  done
  (( i < 120 )) || { echo "El disco no se llenó en 2 minutos: revisa bitacora-lab." >&2; exit 1; }
  rm -f /srv/registros/api-lab.log                # alguien ya "liberó espacio" borrando el log
  ticket "El disco de /srv/registros está al 100%. Ya borramos el log y sigue lleno. ¿Qué pasa?"
}

sembrar_servicio() {
  local fallas=("User=api-labo"
                "WorkingDirectory=/opt/api-lab-v2"
                $'ExecStart=\nExecStart=/usr/bin/python /opt/api-lab/app.py')
  install -d /etc/systemd/system/api-lab.service.d
  printf '[Service]\n%s\n' "${fallas[RANDOM % 3]}" > /etc/systemd/system/api-lab.service.d/50-ajuste.conf
  systemctl daemon-reload
  systemctl restart api-lab || true
  ticket "Después del último cambio, la API ya no levanta. Nadie recuerda qué se cambió."
}

sembrar_puerto() {
  cat > /etc/systemd/system/cache-lab.service <<'UNIT'
[Unit]
Description=Cache local de laboratorio
Before=api-lab.service

[Service]
DynamicUser=yes
ExecStart=/usr/bin/python3 -m http.server 8080 --bind 127.0.0.1 --directory /usr/share/doc
Restart=always

[Install]
WantedBy=multi-user.target
UNIT
  systemctl daemon-reload
  systemctl stop api-lab
  systemctl enable --now cache-lab 2>/dev/null
  systemctl start api-lab || true
  ticket "La API se cayó y no vuelve, ni siquiera con systemctl start. La página da respuestas raras."
}

deshacer() {
  local u
  for u in bitacora-lab cache-lab; do             # uno por uno: si falta uno, el otro igual se detiene
    systemctl stop "$u" 2>/dev/null || true
    systemctl disable "$u" 2>/dev/null || true
  done
  rm -f /etc/systemd/system/bitacora-lab.service /etc/systemd/system/cache-lab.service
  rm -rf /etc/systemd/system/api-lab.service.d /usr/local/lib/lab-s6
  if mountpoint -q /srv/registros; then umount /srv/registros; fi
  rm -rf "$LAB"
  systemctl daemon-reload
  systemctl reset-failed api-lab 2>/dev/null || true
  systemctl restart api-lab
  sleep 2
  if curl -fsS http://127.0.0.1:8080/health; then echo; echo "Todo deshecho."; else echo "La API sigue sin responder." >&2; exit 1; fi
}

case "${1:-}" in
  disco)    sembrar_disco ;;
  servicio) sembrar_servicio ;;
  puerto)   sembrar_puerto ;;
  deshacer) deshacer ;;
  *) echo "Uso: sudo bash sembrar.sh disco|servicio|puerto|deshacer" >&2; exit 2 ;;
esac
```

Reglas para quien siembra:

- **Un incidente a la vez.** Siembren, entreguen el ticket, y esperen a que la víctima lo resuelva o pida terminar.
- No den pistas. Si la víctima se atora 15 minutos, el instructor da una: el número de la lista que deberían mirar.
- Al terminar cada incidente: `sudo bash sembrar.sh deshacer`. Debe terminar con `Todo deshecho.`

---

## Paso 3 - Incidente 1: el disco

El otro equipo corre `sudo bash sembrar.sh disco` y les entrega el ticket:

> El disco de /srv/registros está al 100%. Ya borramos el log y sigue lleno. ¿Qué pasa?

Diagnostiquen con la lista. Lo que van a encontrar en el camino (las cifras varían un poco):

```bash
df -h /srv/registros
# Filesystem      Size  Used Avail Use% Mounted on
# /dev/loop0       56M   52M     0 100% /srv/registros

du -sh /srv/registros
# 20K   /srv/registros
```

`df` y `du` no cuadran. Algo ocupa espacio sin tener nombre:

```bash
sudo lsof +L1 /srv/registros
# COMMAND  PID    USER   FD   TYPE DEVICE SIZE/OFF NLINK NODE NAME
# bash    2806 api-lab    3w   REG    7,0 53956608     0   12 /srv/registros/api-lab.log (deleted)
# sleep   3269 api-lab    3w   REG    7,0 53956608     0   12 /srv/registros/api-lab.log (deleted)

ps -o unit= -p 2806          # a qué unit pertenece (systemctl status 2806 dice lo mismo y su estado)
# bitacora-lab.service

sudo journalctl -u bitacora-lab -n 3 -o cat
# no pude escribir en el log
```

Primer auxilio, sin reiniciar nada (usen su PID y su número de descriptor):

```bash
sudo sh -c ': > /proc/2806/fd/3'
df -h /srv/registros
# /dev/loop0       56M   24K   52M   1% /srv/registros
```

Esperen 10 segundos y repitan el `df`: vuelve a subir. El proceso sigue escribiendo. La causa es el servicio, no el archivo:

```bash
sudo systemctl stop bitacora-lab
df -h /srv/registros
sudo lsof +L1 /srv/registros     # ya no sale nada
```

Para la bitácora: causa raíz en una línea, y qué harían en producción para que no vuelva (rotación con logrotate, límite de tamaño, bajar el nivel de log). Ojo: `copytruncate` necesita espacio para copiar el log, así que con el disco ya lleno no los rescata.

Al terminar, el otro equipo corre `sudo bash sembrar.sh deshacer`.

---

## Paso 4 - Incidente 2: el servicio

Ticket:

> Después del último cambio, la API ya no levanta. Nadie recuerda qué se cambió.

La falla es una de tres, al azar. Lo que van a ver depende de cuál les tocó:

```bash
systemctl --failed
systemctl status api-lab --no-pager
sudo journalctl -u api-lab -b -n 20 --no-pager -o cat
```

| Lo que dice `status` | Lo que dice el journal | Qué significa |
|---|---|---|
| `status=217/USER` | solo el código | El usuario de `User=` no existe |
| `status=200/CHDIR` | `Changing to the requested working directory failed: No such file or directory` | El `WorkingDirectory=` no existe |
| `status=203/EXEC` | solo el código | El programa de `ExecStart=` no existe o no es ejecutable |

¿Dónde está el cambio? La unit original no la tocaron:

```bash
systemctl cat api-lab
# # /etc/systemd/system/api-lab.service
# ...
# # /etc/systemd/system/api-lab.service.d/50-ajuste.conf
# [Service]
# User=api-labo
```

Arreglen **una cosa**, y verifiquen:

```bash
sudo rm /etc/systemd/system/api-lab.service.d/50-ajuste.conf
sudo systemctl daemon-reload
sudo systemctl reset-failed api-lab
sudo systemctl restart api-lab
curl -s http://127.0.0.1/health; echo
```

Y la verificación que cuenta: el mismo `curl` **desde fuera** que hicieron en la sesión 5.

Al terminar, el otro equipo corre `sudo bash sembrar.sh deshacer`.

---

## Paso 5 - Incidente 3: el puerto

Ticket:

> La API se cayó y no vuelve, ni siquiera con systemctl start. La página da respuestas raras.

```bash
sudo journalctl -u api-lab -b --no-pager -o cat | sort | uniq -c | sort -rn | head -5
#       3 OSError: [Errno 98] Address already in use
#       1 api-lab.service: Start request repeated too quickly.
#       ...

sudo ss -ltnp 'sport = :8080'
# LISTEN 0 5 127.0.0.1:8080 0.0.0.0:* users:(("python3",pid=2593,fd=3))

ps -o unit= -p 2593
# cache-lab.service

ps -o user,pid,cmd -p 2593
# USER         PID CMD
# 64695       2593 /usr/bin/python3 -m http.server 8080 --bind 127.0.0.1 --directory /usr/share/doc
```

Las "respuestas raras" de la página son la página de error de ese otro servidor, que Nginx sirve porque le pasa todo al 8080. El usuario `64695` no está en `/etc/passwd`: es un `DynamicUser` que systemd crea al vuelo. Y `systemctl cat cache-lab` les dice por qué gana la carrera al arrancar (`Before=api-lab.service`).

Quiten al impostor y arranquen la API. Si lo hacen dentro de los 2 minutos siguientes a los últimos fallos, el arreglo ingenuo falla:

```bash
sudo systemctl disable --now cache-lab
sudo systemctl start api-lab; echo "salida: $?"
# Job for api-lab.service failed because the control process exited with error code.
# salida: 1
sudo journalctl -u api-lab -n 3 -o cat
# api-lab.service: Start request repeated too quickly.
# api-lab.service: Failed with result 'exit-code'.
# Failed to start api-lab.service - API de laboratorio del curso Linux para desarrolladores.
```

El puerto ya está libre, pero la API alcanzó su límite de arranques (`StartLimitBurst=5` en `StartLimitIntervalSec=120`, sesión 4) y systemd no la deja arrancar hasta que pase esa ventana. Lo medimos: a los 2 minutos y medio, `start` ya funcionó solo. No esperen a que pase la ventana; el hábito es limpiar el contador:

```bash
sudo systemctl reset-failed api-lab
sudo systemctl start api-lab; echo "salida: $?"
# salida: 0
curl -s http://127.0.0.1:8080/health; echo
# {"estado": "ok", "pid": 2670}
```

Al terminar, el otro equipo corre `sudo bash sembrar.sh deshacer`.

---

## Paso 6 - Opcional: ¿qué discos detienen su arranque?

Solo lectura, no cambia nada:

```bash
lsblk -f
findmnt --verify
systemctl show -p Requires -p Wants local-fs.target
```

Anoten qué montajes aparecen en `Requires`: si alguno de ellos no está, la VM no arranca. Si su VM tiene un disco de datos sin `nofail`, ese es un hallazgo para el runbook (no lo cambien hoy sin el instructor).

---

## Paso 7 - Verlo fallar: el runbook en manos de otro

Con lo que anotaron en los tres incidentes, escriban la primera versión de su runbook (la plantilla está en la clase). Después:

1. Le entregan su runbook al otro equipo.
2. Ustedes siembran **otra vez** uno de los tres incidentes en la VM del otro equipo (el que elijan).
3. El otro equipo lo resuelve **solo con su runbook**, sin su ayuda y sin sus propias notas.
4. Anoten en qué paso el runbook no alcanzó: un comando que faltaba, una salida que no explicaron, un orden que confundía.

Si el runbook no alcanza, falló el runbook, no el otro equipo. Corríjanlo. Al terminar: `sudo bash sembrar.sh deshacer`.

---

## Evidencia que entregan

En el repo del equipo, `s06-evidencia.md` con:

1. `linea-base.txt`.
2. La bitácora de cada incidente: hora, comando, salida relevante y decisión.
3. La causa raíz de cada incidente en una línea, y el cambio que la corrigió.
4. La verificación de cada uno (el `curl` desde fuera en los incidentes 2 y 3; el `df` estable en el 1).
5. Lo que encontró el otro equipo al usar su runbook, y la corrección que hicieron.

---

## Checklist

- [ ] Tienen la línea base de la VM sana (paso 1).
- [ ] Explicaron por qué `df` y `du` no cuadraban y liberaron el espacio (paso 3).
- [ ] Encontraron el drop-in con `systemctl cat` y saben qué significa su código de salida (paso 4).
- [ ] Encontraron quién tenía el 8080, a qué unit pertenece y por qué hizo falta `reset-failed` (paso 5).
- [ ] Cada incidente tiene bitácora escrita **durante** el diagnóstico, no después.
- [ ] Otro equipo usó su runbook y anotaron dónde se quedó corto (paso 7).
- [ ] Después de cada incidente, `sembrar.sh deshacer` terminó con `Todo deshecho.`
