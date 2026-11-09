# Práctica - S04 - La API como servicio

**Linux para desarrolladores**
**Tiempo estimado:** 2 horas, en equipo, en su VM.

---

## Objetivo

Convertir la API de laboratorio en un servicio de systemd con usuario propio, configuración fuera del código, reinicio automático, límite de memoria y CPU, endurecimiento medido y un timer de limpieza. Después, **verlo fallar** tres veces: matarla con `kill -9`, ahogarla de memoria y reiniciar la VM. Si en los tres casos vuelve sola, está desplegada.

---

## Antes de empezar

- Su VM Ubuntu Server 24.04 y un usuario con `sudo`: `ssh <USUARIO>@<IP_DE_SU_VM>`.
- El usuario de servicio `api-lab` de la S02:

```bash
getent passwd api-lab || sudo useradd --system --no-create-home --shell /usr/sbin/nologin api-lab
```

- `respaldo.sh` de la S03 en `/usr/local/bin`: no se usa en la práctica, pero sí en la tarea.
- **El journal con sudo.** Si su usuario no está en los grupos `adm` o `systemd-journal`, `journalctl` sin `sudo` responde `No entries`. En esta guía todos los `journalctl` llevan `sudo`.

---

## Paso 1 - Copiar la API

La API de laboratorio solo usa la biblioteca estándar de Python, que ya trae Ubuntu.

```bash
sudo install -d -m 755 /opt/api-lab
sudo tee /opt/api-lab/app.py > /dev/null <<'EOF'
#!/usr/bin/env python3
"""API de laboratorio del curso Linux para desarrolladores. Solo biblioteca estandar."""
import json, os, time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

HOST = os.environ.get("API_HOST", "127.0.0.1")
PORT = int(os.environ.get("API_PORT", "8080"))
retenida = []                                     # lo que pide /memoria se queda aqui

class Api(BaseHTTPRequestHandler):
    def responder(self, codigo, cuerpo):
        datos = json.dumps(cuerpo).encode()
        self.send_response(codigo)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(datos)))
        self.end_headers()
        self.wfile.write(datos)

    def do_GET(self):
        url = urlparse(self.path)
        if url.path == "/health":
            self.responder(200, {"estado": "ok", "pid": os.getpid()})
        elif url.path == "/lento":
            time.sleep(2)
            self.responder(200, {"estado": "ok", "tardo_s": 2})
        elif url.path == "/memoria":
            mb = int(parse_qs(url.query).get("mb", ["50"])[0])
            retenida.append(b"x" * (mb * 1024 * 1024))   # se escribe: ocupa RAM de verdad
            self.responder(200, {"retenidos_mb": sum(len(b) for b in retenida) // 1048576})
        elif url.path == "/error":
            self.responder(500, {"error": "falla a proposito"})
        else:
            self.responder(404, {"error": "no existe"})

    def log_message(self, formato, *args):     # a stdout: systemd lo manda al journal
        print(f"{self.address_string()} {formato % args}", flush=True)

if __name__ == "__main__":
    print(f"api-lab escuchando en {HOST}:{PORT}", flush=True)
    ThreadingHTTPServer((HOST, PORT), Api).serve_forever()
EOF
sudo chmod 644 /opt/api-lab/app.py
```

| Ruta | Qué hace |
|---|---|
| `/health` | Responde `{"estado": "ok", "pid": ...}`: el PID les dice si es el mismo proceso o uno nuevo |
| `/lento` | Tarda 2 segundos (lo usa la S05) |
| `/memoria?mb=N` | Retiene N MB más y no los suelta: sirve para provocar el OOM |
| `/error` | Responde 500 a propósito |

---

## Paso 2 - Probarla a mano, y ver por qué no basta

En una terminal, córranla como el usuario de servicio:

```bash
sudo -u api-lab python3 /opt/api-lab/app.py
```

```text
api-lab escuchando en 127.0.0.1:8080
```

En una **segunda** terminal SSH:

```bash
curl -s http://127.0.0.1:8080/health; echo
```

```text
{"estado": "ok", "pid": 505}
```

En la primera terminal aparece `127.0.0.1 "GET /health HTTP/1.1" 200 -`. Ahora deténganla con Ctrl+C (Python imprime un `KeyboardInterrupt`) o cierren esa terminal, y repitan el `curl`: falla con código 7, no hay nadie escuchando. Así vive hoy la API: atada a una terminal.

---

## Paso 3 - Configuración fuera del código

```bash
sudo install -d -m 755 /etc/api-lab
printf 'API_HOST=127.0.0.1\nAPI_PORT=8080\n' | sudo tee /etc/api-lab/env > /dev/null
sudo chmod 600 /etc/api-lab/env
sudo ls -l /etc/api-lab/env
```

```text
-rw------- 1 root root 33 Nov  9 04:07 /etc/api-lab/env
```

systemd lee este archivo como root antes de arrancar la API, así que puede ser `600`: ningún otro usuario de la VM ve lo que tenga. Aquí irían, en un proyecto real, la cadena de conexión o una llave de API.

---

## Paso 4 - La unit

```bash
sudo tee /etc/systemd/system/api-lab.service > /dev/null <<'EOF'
[Unit]
Description=API de laboratorio del curso Linux para desarrolladores
After=network-online.target
Wants=network-online.target
# Si truena 5 veces en 2 minutos, systemd deja de insistir y la marca como fallida.
StartLimitIntervalSec=120
StartLimitBurst=5

[Service]
Type=simple
User=api-lab
Group=api-lab
WorkingDirectory=/opt/api-lab
# El guion inicial hace opcional el archivo: sin él, la API usa sus valores por defecto.
EnvironmentFile=-/etc/api-lab/env
ExecStart=/usr/bin/python3 /opt/api-lab/app.py
Restart=on-failure
RestartSec=3

# Límites: los aplica el kernel por medio del cgroup de la unit.
MemoryMax=200M
# Sin esto, al llegar al límite el kernel manda a swap en vez de matar: el límite se estira.
MemorySwapMax=0
CPUQuota=50%
TasksMax=64

# Endurecimiento: el proceso ve el sistema en solo lectura salvo su carpeta de estado.
NoNewPrivileges=yes
ProtectSystem=strict
ProtectHome=yes
PrivateTmp=yes
StateDirectory=api-lab

[Install]
WantedBy=multi-user.target
EOF
sudo systemd-analyze verify /etc/systemd/system/api-lab.service && echo "verify: sin errores"
sudo systemctl daemon-reload
sudo systemctl enable --now api-lab
systemctl status api-lab --no-pager
curl -s http://127.0.0.1:8080/health; echo
systemctl is-enabled api-lab; systemctl is-active api-lab
```

Salida esperada (los PID, fechas y cifras de memoria serán los suyos):

```text
verify: sin errores
api-lab.service - API de laboratorio del curso Linux para desarrolladores
     Loaded: loaded (/etc/systemd/system/api-lab.service; enabled; preset: enabled)
     Active: active (running) since Mon 2026-11-09 04:07:58 UTC; 2s ago
   Main PID: 563 (python3)
      Tasks: 1 (limit: 64)
     Memory: 8.8M (max: 200.0M swap max: 0B available: 191.1M peak: 8.9M)
        CPU: 27ms
     CGroup: /system.slice/api-lab.service
             └─563 /usr/bin/python3 /opt/api-lab/app.py
{"estado": "ok", "pid": 563}
enabled
active
```

Revisen tres cosas en la salida: `enabled` (volverá al reiniciar), `limit: 64` y `max: 200.0M swap max: 0B` (los límites están puestos), y el PID de `/health` igual al `Main PID`.

Si `verify` reporta algo, **no sigan**: systemd ignora las líneas que no entiende y la unit arranca sin lo que ustedes creen que tiene.

---

## Paso 5 - Verlo fallar: kill -9

```bash
systemctl show api-lab -p MainPID -p NRestarts
sudo kill -9 "$(systemctl show -p MainPID --value api-lab)"
sleep 5
systemctl show api-lab -p MainPID -p NRestarts
sudo journalctl -u api-lab -n 5 --no-pager
```

```text
MainPID=563
NRestarts=0
MainPID=584
NRestarts=1
Nov 09 04:08:27 vm-lab systemd[1]: api-lab.service: Main process exited, code=killed, status=9/KILL
Nov 09 04:08:27 vm-lab systemd[1]: api-lab.service: Failed with result 'signal'.
Nov 09 04:08:30 vm-lab systemd[1]: api-lab.service: Scheduled restart job, restart counter is at 1.
Nov 09 04:08:30 vm-lab systemd[1]: Started api-lab.service - API de laboratorio del curso Linux para desarrolladores.
Nov 09 04:08:30 vm-lab python3[584]: api-lab escuchando en 127.0.0.1:8080
```

Un PID nuevo, el contador en 1, y entre la muerte y el arranque los segundos de `RestartSec`. Ahora prueben `sudo systemctl stop api-lab`: **no** se reinicia, porque detenerla a mano no es una falla. Vuelvan a arrancarla con `sudo systemctl start api-lab`.

---

## Paso 6 - Verlo fallar: el OOM

```bash
curl -s "http://127.0.0.1:8080/memoria?mb=100"; echo
systemctl show api-lab -p MemoryCurrent -p MemoryMax
curl -s "http://127.0.0.1:8080/memoria?mb=150"; echo "curl salió con $?"
sleep 5
sudo journalctl -u api-lab -n 7 --no-pager
systemctl show api-lab -p NRestarts
sudo journalctl -k -n 50 --no-pager | grep -i "out of memory"
```

```text
{"retenidos_mb": 100}
MemoryCurrent=114618368
MemoryMax=209715200
curl salió con 52
Nov 09 04:08:33 vm-lab python3[584]: 127.0.0.1 "GET /memoria?mb=100 HTTP/1.1" 200 -
Nov 09 04:08:33 vm-lab systemd[1]: api-lab.service: A process of this unit has been killed by the OOM killer.
Nov 09 04:08:33 vm-lab systemd[1]: api-lab.service: Main process exited, code=killed, status=9/KILL
Nov 09 04:08:33 vm-lab systemd[1]: api-lab.service: Failed with result 'oom-kill'.
Nov 09 04:08:36 vm-lab systemd[1]: api-lab.service: Scheduled restart job, restart counter is at 2.
Nov 09 04:08:36 vm-lab systemd[1]: Started api-lab.service - API de laboratorio del curso Linux para desarrolladores.
Nov 09 04:08:37 vm-lab python3[596]: api-lab escuchando en 127.0.0.1:8080
NRestarts=2
Nov 09 04:08:33 vm-lab kernel: Memory cgroup out of memory: Killed process ... (python3) ...
```

- 100 MB caben (unos 115 MB con el propio Python). 100 + 150 ya no: el kernel mata a la API **dentro de su cgroup**, y el resto de la VM ni se entera.
- `curl` sale con 52: el servidor cerró la conexión sin responder.
- La línea del kernel dice `Memory cgroup out of memory`: fue el límite de la unit, no la VM sin memoria.

**Experimento para la evidencia:** si su VM tiene swap (`swapon --show` imprime algo), quiten `MemorySwapMax=0`, hagan `daemon-reload` y `restart`, y repitan. Anoten cuánto llegó a retener la API sin que la mataran. Al terminar, vuelvan a poner la línea.

---

## Paso 7 - El timer de limpieza

Dos units: el trabajo y el horario.

```bash
sudo tee /etc/systemd/system/limpieza-api-lab.service > /dev/null <<'EOF'
[Unit]
Description=Borra los temporales de la API de laboratorio con más de 7 días

[Service]
Type=oneshot
User=api-lab
ExecStart=/usr/bin/find /var/lib/api-lab/tmp -type f -mtime +7 -print -delete
EOF
sudo tee /etc/systemd/system/limpieza-api-lab.timer > /dev/null <<'EOF'
[Unit]
Description=Limpieza diaria de los temporales de la API de laboratorio

[Timer]
OnCalendar=*-*-* 03:30:00
Persistent=true
RandomizedDelaySec=10min

[Install]
WantedBy=timers.target
EOF
sudo systemd-analyze verify /etc/systemd/system/limpieza-api-lab.service /etc/systemd/system/limpieza-api-lab.timer && echo "verify: sin errores"
sudo systemctl daemon-reload
sudo systemctl enable --now limpieza-api-lab.timer
systemctl list-timers limpieza-api-lab.timer --no-pager
```

```text
verify: sin errores
NEXT                        LEFT LAST PASSED UNIT                   ACTIVATES
Tue 2026-11-10 03:34:51 UTC  23h -         - limpieza-api-lab.timer limpieza-api-lab.service
```

Se habilita **el timer**, no el servicio. `NEXT` ya incluye el retraso aleatorio. Ahora siembren archivos viejos y nuevos, y corran el servicio sin esperar a las 03:30:

```bash
sudo -u api-lab mkdir -p /var/lib/api-lab/tmp
sudo -u api-lab touch -d "10 days ago" /var/lib/api-lab/tmp/viejo-1.json /var/lib/api-lab/tmp/viejo-2.json
sudo -u api-lab touch /var/lib/api-lab/tmp/nuevo.json
ls -l --time-style=+%F /var/lib/api-lab/tmp
sudo systemctl start limpieza-api-lab.service
ls /var/lib/api-lab/tmp
sudo journalctl -u limpieza-api-lab --no-pager
```

```text
-rw-rw-r-- 1 api-lab api-lab  0 2026-11-09 nuevo.json
-rw-rw-r-- 1 api-lab api-lab  0 2026-10-30 viejo-1.json
-rw-rw-r-- 1 api-lab api-lab  0 2026-10-30 viejo-2.json
nuevo.json
Nov 09 04:08:38 vm-lab systemd[1]: Starting limpieza-api-lab.service - Borra los temporales de la API de laboratorio con más de 7 días...
Nov 09 04:08:38 vm-lab find[658]: /var/lib/api-lab/tmp/viejo-2.json
Nov 09 04:08:38 vm-lab find[658]: /var/lib/api-lab/tmp/viejo-1.json
Nov 09 04:08:38 vm-lab systemd[1]: limpieza-api-lab.service: Deactivated successfully.
Nov 09 04:08:38 vm-lab systemd[1]: Finished limpieza-api-lab.service - Borra los temporales de la API de laboratorio con más de 7 días.
```

Si les quedaron archivos de la S03 en `tmp` (por ejemplo `sesion-1.json`, de hoy), también se quedan: solo se borra lo de más de 7 días. Comprueben el horario con `systemd-analyze calendar "*-*-* 03:30:00"`.

---

## Paso 8 - Endurecer y medirlo

```bash
systemd-analyze security api-lab --no-pager | tail -1
sudo mkdir -p /etc/systemd/system/api-lab.service.d
sudo tee /etc/systemd/system/api-lab.service.d/endurecer.conf > /dev/null <<'EOF'
[Service]
PrivateDevices=yes
ProtectKernelTunables=yes
ProtectKernelModules=yes
ProtectControlGroups=yes
RestrictAddressFamilies=AF_INET AF_INET6 AF_UNIX
RestrictNamespaces=yes
LockPersonality=yes
CapabilityBoundingSet=
SystemCallFilter=@system-service
SystemCallArchitectures=native
EOF
sudo systemctl daemon-reload
sudo systemctl restart api-lab
sleep 2
systemd-analyze security api-lab --no-pager | tail -1
curl -s http://127.0.0.1:8080/health; echo
systemctl cat api-lab --no-pager | grep '^# /'
```

La última línea de `security` cambia de `8.3 EXPOSED` a `2.2 OK`, y la API sigue respondiendo:

```text
{"estado": "ok", "pid": 693}
# /etc/systemd/system/api-lab.service
# /etc/systemd/system/api-lab.service.d/endurecer.conf
```

Si en su VM la API ya no arranca después de este paso, lean `sudo journalctl -u api-lab -n 20` antes de quitar líneas: quiten **una** a la vez, la que el mensaje señale. Cuidado con `sudo systemctl revert api-lab`: borra **todos** los drop-ins, incluido este.

---

## Paso 9 - Verlo fallar: sudo reboot

Antes de reiniciar, anoten el `Main PID` actual. Luego:

```bash
sudo reboot
```

La sesión SSH se corta. Esperen un minuto y vuelvan a entrar:

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
systemctl is-active api-lab
curl -s http://127.0.0.1:8080/health; echo
systemctl list-timers --no-pager | grep api-lab
sudo journalctl --list-boots --no-pager | tail -2
sudo journalctl -u api-lab -b -1 -n 3 --no-pager
sudo journalctl -u api-lab -b -n 3 --no-pager
systemd-analyze critical-chain api-lab.service
```

Lo que deben ver:

- `active` y un `/health` que responde con un **PID distinto** al que anotaron.
- El timer de limpieza listado con su próximo `NEXT`.
- Dos arranques en `--list-boots`: el anterior y el actual.
- En `-b -1`, las últimas líneas de la API **antes** del reinicio (systemd la detuvo con orden); en `-b`, su arranque de hoy.
- En `critical-chain`, cuánto tardó en llegar la API y detrás de qué esperó.

### 9 bis - Si les sobra tiempo: ver fallar el reinicio

1. `sudo systemctl disable api-lab` y `sudo reboot`.
2. Al volver: `systemctl is-active api-lab` dice `inactive` y `curl -s http://127.0.0.1:8080/health; echo "curl: $?"` sale con 7.
3. `sudo systemctl enable --now api-lab` y otro `sudo reboot`.
4. Al volver: `active`.

Así prueban que lo que la trae de vuelta es el `enable`, y no la suerte.

---

## Evidencia que entregan

Un archivo `evidencia-s04.md` en el repositorio del equipo con:

1. `api-lab.service`, `endurecer.conf`, `limpieza-api-lab.service` y `limpieza-api-lab.timer`, y la salida de `systemd-analyze verify` de cada uno.
2. `systemctl status api-lab` después del paso 4.
3. Las líneas del journal del `kill -9` (paso 5) y del OOM (paso 6), y su respuesta: ¿cuántos segundos pasaron entre la muerte y el arranque, y de qué directiva sale ese número?
4. Si hicieron el experimento sin `MemorySwapMax=0`: cuánto retuvo la API antes de morir (o si no murió).
5. El journal de la limpieza: qué borró y qué dejó.
6. La calificación de `systemd-analyze security` antes y después de `endurecer.conf`.
7. La salida de los comandos del paso 9 **después** del reinicio, con el PID de antes y el de después.

---

## Checklist

- [ ] La API corre como `api-lab`, no como root (`ps -o user= -p "$(systemctl show -p MainPID --value api-lab)"`).
- [ ] `/etc/api-lab/env` tiene permisos `600`.
- [ ] `systemd-analyze verify` no reporta nada en ninguna de sus units.
- [ ] `systemctl is-enabled api-lab` dice `enabled`.
- [ ] Después de `kill -9`, la API volvió con otro PID.
- [ ] Vieron `Failed with result 'oom-kill'` en el journal y la API volvió.
- [ ] El timer de limpieza aparece en `list-timers` y su servicio borró solo lo viejo.
- [ ] `systemd-analyze security` bajó con el drop-in y la API sigue respondiendo.
- [ ] Después de `sudo reboot`, todo volvió sin que nadie lo levantara.
