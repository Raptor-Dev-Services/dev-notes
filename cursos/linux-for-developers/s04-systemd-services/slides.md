<!-- _class: lead -->

# S04 - Servicios con systemd

### Linux para desarrolladores

**Rogelio Arriaga - Raptor Dev Services**

<!--
Sesión de 5 horas. Hilo conductor: la API de laboratorio pasa de "corre en mi terminal" a servicio que
arranca solo, se reinicia, tiene límites y deja rastro. Cierra con el reinicio de la VM.
Antes de empezar: cada equipo con el usuario api-lab (S02) y respaldo.sh en /usr/local/bin (S03).
-->

---

## El lema de hoy

> Si su aplicación no vuelve sola después de reiniciar el servidor, no está desplegada.

Hoy la prueba final no es un `curl`: es `sudo reboot` y ver que todo regresa sin que nadie entre a levantarlo.

---

## Agenda

| Bloque | Qué hacemos |
|---|---|
| 1 | Del encendido al login |
| 2 | Units y `systemctl` |
| 3 | Una unit para la API |
| 4 | Límites con cgroups y endurecimiento |
| 5 | Timers en lugar de cron, y el caso real |
| 6 | El journal |
| 7 | Práctica guiada: verlo fallar, hasta el reinicio |
| 8 | Cierre, tarea y lo que sigue |

---

## Dónde estamos

```bash
python3 /opt/api-lab/app.py      # la API corre... mientras esta terminal siga abierta
```

- Si cierran la sesión SSH, la API muere.
- Si truena a las 3 de la mañana, nadie la levanta.
- Si se come la memoria, se lleva a la VM entera.
- Si reinician el servidor, no vuelve.

**Desplegada** significa: arranca sola, se levanta sola, tiene límites y deja rastro auditable.

---

<!-- _class: lead -->

# Parte 1
## Del encendido al login

---

## La cadena de arranque

1. **Firmware** (UEFI o BIOS): revisa el hardware y busca un cargador.
2. **Cargador de arranque** (GRUB): elige y carga el kernel y el initramfs.
3. **Kernel**: detecta dispositivos y monta un sistema de archivos inicial en memoria.
4. **initramfs**: lo mínimo para encontrar y montar el disco raíz de verdad.
5. **systemd**, el proceso 1: arranca todo lo demás, en paralelo, según dependencias.

Fuente: Both, *systemd for Linux SysAdmins*, caps. 2 y 3.

<!--
No hace falta memorizar GRUB. Lo importante: a partir del paso 5, todo lo que arranca en su servidor lo
arranca systemd, y lo hace porque una unit se lo pide. Si su API no tiene unit, no existe para el arranque.
-->

---

## systemd es el proceso 1

```console
$ ps -p 1 -o pid,comm
    PID COMMAND
      1 systemd
```

- Es el primer proceso del espacio de usuario y el padre adoptivo de todos los demás.
- Arranca servicios, monta discos, abre sockets y corre tareas programadas.
- Si el proceso 1 muere, el kernel entra en pánico: por eso systemd casi nunca truena.

---

## Targets: a dónde quiere llegar el arranque

```console
$ systemctl get-default
graphical.target
```

| Target | Para qué |
|---|---|
| `multi-user.target` | Sistema completo con red y servicios, sin escritorio |
| `graphical.target` | `multi-user.target` más el entorno gráfico, si lo hay |
| `rescue.target` | Modo de mantenimiento, un solo usuario |

Un target agrupa units. Una API se cuelga de `multi-user.target` con `WantedBy=multi-user.target`. Aunque diga `graphical.target`, ese target incluye a `multi-user.target`.

Fuente: Both, *systemd for Linux SysAdmins*, cap. 4.

---

## Demostración: ¿cuánto tardó y por qué?

```bash
systemd-analyze                              # tiempo total del arranque
systemd-analyze blame | head                 # qué unit tardó más
systemd-analyze critical-chain api-lab.service
```

```console
$ systemd-analyze critical-chain api-lab.service
api-lab.service @1min 4.050s
└─basic.target @111ms
  └─sockets.target @111ms
    └─ssh.socket @111ms
```

`blame` ordena por tiempo; `critical-chain` muestra la cadena de esperas que de verdad retrasó a una unit.

<!--
Hazlo en tu VM, no en un contenedor: ahí los tiempos son reales. Señala que blame suma tiempos de units que
corren en paralelo: la que más tarda no siempre es la que más retrasa.
-->

---

<!-- _class: lead -->

# Parte 2
## Units y systemctl

---

## Qué es una unit

Un archivo de texto que describe **algo que systemd administra**.

| Tipo | Ejemplo | Describe |
|---|---|---|
| `.service` | `api-lab.service` | Un proceso que corre |
| `.timer` | `limpieza-api-lab.timer` | Cuándo activar otra unit |
| `.socket` | `ssh.socket` | Un puerto que systemd escucha por el servicio |
| `.mount` | `mnt-datos.mount` | Un sistema de archivos montado |
| `.target` | `multi-user.target` | Un grupo de units, un punto del arranque |

Fuente: Both, *systemd for Linux SysAdmins*, cap. 5.

---

## Dónde viven

| Ruta | De quién es | ¿Se edita? |
|---|---|---|
| `/usr/lib/systemd/system/` | De los paquetes (`apt`) | No: una actualización la reemplaza |
| `/etc/systemd/system/` | Del administrador | Sí: aquí van las suyas |
| `/run/systemd/system/` | Temporales, se pierden al reiniciar | No |

Si existe el mismo nombre en `/etc` y en `/usr/lib`, gana `/etc`.

```bash
systemctl cat nginx.service     # imprime la unit que de verdad se usa, con sus drop-ins
```

---

## Drop-ins: cambiar sin tocar el archivo del paquete

```bash
sudo systemctl edit api-lab          # abre un editor y guarda en api-lab.service.d/override.conf
systemctl cat api-lab                # la unit y, debajo, cada drop-in que la modifica
sudo systemctl revert api-lab        # borra los drop-ins y vuelve a la unit original
```

```console
$ systemctl cat api-lab | grep '^# /'
# /etc/systemd/system/api-lab.service
# /etc/systemd/system/api-lab.service.d/override.conf
```

- Lo medimos: un drop-in con `Environment=API_PORT=8090` movió la API al 8090... hasta que existió `/etc/api-lab/env`. Lo que dice `EnvironmentFile=` **gana** sobre `Environment=`.
- `revert` borra **todos** los drop-ins de la unit, también el de endurecimiento que harán más tarde.

---

## Requerir no es ordenar

| Directiva | Qué dice |
|---|---|
| `Wants=b` | Cuando arranques, arranca también `b`; si `b` falla, sigo |
| `Requires=b` | Si `b` se detiene, yo también; con `After=b`, si `b` falla yo no arranco |
| `After=b` | Si los dos arrancan, yo voy **después** de `b` |
| `Before=b` | Si los dos arrancan, yo voy **antes** de `b` |

`Wants` y `Requires` dicen **qué** arranca. `After` y `Before` dicen **en qué orden**. Casi siempre se usan en pareja:

```ini
After=network-online.target
Wants=network-online.target
```

---

## systemctl: el día a día

| Comando | Qué hace |
|---|---|
| `systemctl status api-lab` | Estado, PID, memoria y las últimas líneas del journal |
| `sudo systemctl start` / `stop` / `restart` | Arrancar, detener, reiniciar ahora |
| `sudo systemctl reload nginx` | Releer la configuración sin cortar conexiones (si el servicio lo soporta) |
| `sudo systemctl enable api-lab` | Arrancar en cada arranque del sistema |
| `systemctl is-active` / `is-enabled` / `is-failed` | Respuestas de una palabra, útiles en scripts |
| `systemctl --failed` | Todo lo que está en estado fallido |
| `sudo systemctl daemon-reload` | Releer los archivos de units después de editarlos |

Fuente: Both, *systemd for Linux SysAdmins*, cap. 5.

---

## `enable` no es `start`

- `start` arranca **ahora**. Al reiniciar, se olvida.
- `enable` crea un enlace en `multi-user.target.wants/`: arranca **en el próximo arranque**. Ahora no hace nada.
- `enable --now` hace las dos cosas.

```console
$ sudo systemctl disable api-lab      # y luego reiniciar
tras reinicio sin enable: inactive / disabled
$ sudo systemctl enable api-lab       # y luego reiniciar
tras reinicio con enable: active
```

El error más común de este curso: probar con `start`, ver que funciona y olvidar el `enable`.

---

## `mask`: que nadie lo pueda arrancar

```console
$ sudo systemctl mask --now nginx
$ sudo systemctl start nginx
Failed to start nginx.service: Unit nginx.service is masked.
$ sudo systemctl unmask nginx
```

- `disable` solo quita el arranque automático: otra unit o una persona todavía lo pueden arrancar.
- `mask` enlaza la unit a `/dev/null`: no arranca por ningún camino.
- Es para units **de paquetes**. Con una unit propia en `/etc/systemd/system`, `mask` falla: el archivo ya ocupa ese lugar.

---

## `daemon-reload`: systemd no relee solo

```bash
sudo nano /etc/systemd/system/api-lab.service   # cambian algo
sudo systemctl restart api-lab                    # systemd sigue usando la versión vieja
```

```text
Warning: The unit file, source configuration file or drop-ins of api-lab.service changed on disk.
Run 'systemctl daemon-reload' to reload units.
```

Después de **editar cualquier unit**: `sudo systemctl daemon-reload` y después `restart`. `systemctl edit` hace el `daemon-reload` por ustedes.

---

<!-- _class: lead -->

# Parte 3
## Una unit para la API

---

## La unit completa

```ini
[Unit]
Description=API de laboratorio del curso Linux para desarrolladores
After=network-online.target
Wants=network-online.target
StartLimitIntervalSec=120
StartLimitBurst=5

[Service]
Type=simple
User=api-lab
Group=api-lab
WorkingDirectory=/opt/api-lab
EnvironmentFile=-/etc/api-lab/env
ExecStart=/usr/bin/python3 /opt/api-lab/app.py
Restart=on-failure
RestartSec=3
MemoryMax=200M
MemorySwapMax=0
CPUQuota=50%
TasksMax=64
NoNewPrivileges=yes
ProtectSystem=strict
ProtectHome=yes
PrivateTmp=yes
StateDirectory=api-lab

[Install]
WantedBy=multi-user.target
```

<!--
La versión de la práctica trae comentarios en cada bloque. Aquí va limpia para recorrerla línea por línea en
las siguientes láminas.
-->

---

## `[Service]`: quién, dónde y qué

| Directiva | Por qué |
|---|---|
| `User=api-lab` | El usuario sin shell de la S02: si alguien explota la API, entra como `api-lab`, no como root |
| `WorkingDirectory=/opt/api-lab` | Desde dónde corre |
| `EnvironmentFile=-/etc/api-lab/env` | Configuración fuera del código; el `-` lo hace opcional |
| `ExecStart=/usr/bin/python3 ...` | Ruta completa al ejecutable |
| `StateDirectory=api-lab` | systemd crea `/var/lib/api-lab` con dueño `api-lab` antes de arrancar |
| `Type=simple` | El proceso que arranca es el servicio; no se va a segundo plano |

---

## `Restart=`: cuándo levantarla otra vez

| Valor | Reinicia si... |
|---|---|
| `no` | Nunca (es el valor por defecto) |
| `on-failure` | Sale con código distinto de 0, la mata una señal, o vence un tiempo |
| `always` | Termina por cualquier motivo, aunque salga bien |

- `RestartSec=3`: espera 3 segundos entre intentos.
- `systemctl stop` nunca dispara un reinicio: detenerla a mano es una decisión, no una falla.

Fuente: Both, *systemd for Linux SysAdmins*, cap. 5.

---

## `StartLimit`: cuándo deja de insistir

```ini
StartLimitIntervalSec=120
StartLimitBurst=5
```

```text
api-lab.service: Scheduled restart job, restart counter is at 5.
api-lab.service: Start request repeated too quickly.
api-lab.service: Failed with result 'exit-code'.
Failed to start api-lab.service - API de laboratorio del curso Linux para desarrolladores.
```

- Más de 5 arranques en 120 segundos: systemd la deja en `failed` y deja de intentar.
- Cuentan también los arranques a mano. `sudo systemctl reset-failed api-lab` borra la cuenta.
- Un servicio que truena en ciclo no debe comerse la VM reintentando para siempre.

---

## systemd ignora lo que no entiende

```console
$ sudo systemd-analyze verify /tmp/api-lab-mal.service; echo "verify: $?"
/tmp/api-lab-mal.service:17: Failed to parse service restart specifier, ignoring: onfailure
/tmp/api-lab-mal.service:21: Invalid memory limit '200MB', ignoring: Invalid argument
api-lab-mal.service: Command /usr/bin/python is not executable: No such file or directory
verify: 1
```

- `Restart=onfailure` (sin guion) se **ignora**: la API queda sin reinicio y nadie se entera.
- `200MB` no es válido (es `200M`): la API queda **sin límite**.
- `systemd-analyze verify` antes de cada `daemon-reload`. Es el ShellCheck de las units.

<!--
Conecta con la S03: igual que un script que sale con 0 después de fallar, una unit con un error de dedo
arranca "bien" y no hace lo que dice.
-->

---

## Demostración: instalarla

```bash
sudo systemd-analyze verify /etc/systemd/system/api-lab.service && echo "verify: sin errores"
sudo systemctl daemon-reload
sudo systemctl enable --now api-lab
systemctl status api-lab --no-pager
```

```text
api-lab.service - API de laboratorio del curso Linux para desarrolladores
     Loaded: loaded (/etc/systemd/system/api-lab.service; enabled; preset: enabled)
     Active: active (running) since Mon 2026-11-09 04:07:58 UTC; 2s ago
   Main PID: 563 (python3)
      Tasks: 1 (limit: 64)
     Memory: 8.8M (max: 200.0M swap max: 0B available: 191.1M peak: 8.9M)
     CGroup: /system.slice/api-lab.service
             └─563 /usr/bin/python3 /opt/api-lab/app.py
```

---

## Momento de práctica: su API como servicio (40 min)

1. Copien `app.py` a `/opt/api-lab` y pruébenla a mano como `api-lab`.
2. Creen `/etc/api-lab/env` con permisos `600`.
3. Escriban `/etc/systemd/system/api-lab.service`.
4. `systemd-analyze verify` sin errores.
5. `daemon-reload`, `enable --now` y `status`.
6. `curl -s http://127.0.0.1:8080/health` responde con su PID.

Guía paso a paso en la práctica de la sesión. Lleguen al paso 6 antes del siguiente bloque.

---

<!-- _class: lead -->

# Parte 4
## Límites con cgroups

---

## Cada unit es un cgroup

```console
$ systemd-cgtop -n 1 -b --depth=2
system.slice                                5      -    23.6M        -        -
system.slice/api-lab.service                1      -     8.9M        -        -
system.slice/dbus.service                   1      -     1.5M        -        -
```

- systemd mete cada servicio en su propio **cgroup**: un grupo de procesos que el kernel mide y limita junto.
- Los hijos que lance la API quedan en el mismo grupo: no se escapan del límite.
- `systemctl status` ya muestra memoria, tareas y CPU del grupo.

Fuente: Both, *systemd for Linux SysAdmins*, cap. 11.

---

## Memoria: `MemoryMax` y `MemorySwapMax`

```ini
MemoryMax=200M
MemorySwapMax=0
```

- `MemoryMax`: el tope duro. Si el grupo lo pasa, el kernel mata un proceso del grupo, no de la VM.
- Lo medimos **sin** `MemorySwapMax=0`, en una máquina con swap: la API retuvo 250 MB con un límite de 200 MB. El kernel la mandó a swap y el límite se estiró.
- Con `MemorySwapMax=0`, al pasar los 200 MB llega el OOM killer: `Failed with result 'oom-kill'`, y `Restart=on-failure` la levanta.

---

## CPU y número de procesos

```ini
CPUQuota=50%
TasksMax=64
```

```console
$ sudo systemd-run -q --unit=quema -p CPUQuota=20% python3 -c "while True: pass"
$ sleep 5; systemctl show quema -p CPUUsageNSec
CPUUsageNSec=1012190000
$ sudo systemctl stop quema
```

- Un ciclo infinito con `CPUQuota=20%`: en 5 segundos usó 1.01 s de CPU. El 20 % exacto.
- `TasksMax=64`: ni una fuga de hilos ni un error en un ciclo de `fork` pasan de 64 tareas.

---

## `systemd-run`: probar un límite sin escribir una unit

```console
$ sudo systemd-run --scope -q -p MemoryMax=50M -p MemorySwapMax=0 \
    python3 -c 'b = b"x" * (100 * 1024 * 1024)'
Killed
$ echo $?
137
```

- `systemd-run` lanza un comando en un cgroup temporal con las propiedades que le pidan.
- 137 = 128 + 9: lo mató `SIGKILL`, el mismo código de la S03.
- Sirve para medir cuánta memoria necesita algo antes de fijar `MemoryMax`.

---

## Demostración: el OOM, en vivo

```bash
curl -s "http://127.0.0.1:8080/memoria?mb=100"     # retiene 100 MB
curl -s "http://127.0.0.1:8080/memoria?mb=150"; echo "curl salió con $?"
sudo journalctl -u api-lab -n 6 --no-pager
```

```text
curl salió con 52
vm-lab systemd[1]: api-lab.service: A process of this unit has been killed by the OOM killer.
vm-lab systemd[1]: api-lab.service: Main process exited, code=killed, status=9/KILL
vm-lab systemd[1]: api-lab.service: Failed with result 'oom-kill'.
vm-lab systemd[1]: api-lab.service: Scheduled restart job, restart counter is at 2.
vm-lab systemd[1]: Started api-lab.service - API de laboratorio del curso Linux para desarrolladores.
```

La API murió sola, la VM siguió bien, y 3 segundos después la API estaba otra vez arriba.

---

## Endurecer, y medirlo

```bash
systemd-analyze security api-lab --no-pager | tail -1
```

| Unit | Calificación (menos es mejor) |
|---|---|
| Sin ninguna directiva de endurecimiento | 9.2 UNSAFE |
| La unit de la práctica (`ProtectSystem=strict`, `PrivateTmp`, ...) | 8.3 EXPOSED |
| Más el drop-in `endurecer.conf` | 2.2 OK |

La API siguió respondiendo en los tres casos. Se endurece por pasos y se vuelve a probar después de cada uno.

---

## El drop-in de endurecimiento

```ini
# /etc/systemd/system/api-lab.service.d/endurecer.conf
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
```

Ojo: si su API necesita algo de esto (un dispositivo, otra familia de sockets), va a fallar al arrancar. El journal dirá qué le negaron: léanlo antes de quitar la línea.

---

<!-- _class: lead -->

# Parte 5
## Timers en lugar de cron

---

## Por qué un timer y no cron

| | cron | timer de systemd |
|---|---|---|
| Dónde queda la salida | Correo local que nadie lee, o se pierde | En el journal, con el nombre de la unit |
| ¿Se encima si tarda? | Sí: arranca otra copia | No: si sigue corriendo, no lo vuelve a arrancar |
| Si la VM estaba apagada | Se pierde | `Persistent=true` lo corre al arrancar |
| Límites y endurecimiento | No | Los mismos de cualquier servicio |
| ¿Cuándo toca? | Hay que leer la línea | `systemctl list-timers` |

Ubuntu ya funciona así: `apt-daily.timer`, `fstrim.timer` y `dpkg-db-backup.timer` vienen activos.

Fuente: Both, *systemd for Linux SysAdmins*, cap. 8.

---

## Un timer son dos units

```ini
# /etc/systemd/system/limpieza-api-lab.service
[Unit]
Description=Borra los temporales de la API de laboratorio con más de 7 días

[Service]
Type=oneshot
User=api-lab
ExecStart=/usr/bin/find /var/lib/api-lab/tmp -type f -mtime +7 -print -delete
```

```ini
# /etc/systemd/system/limpieza-api-lab.timer
[Timer]
OnCalendar=*-*-* 03:30:00
Persistent=true
RandomizedDelaySec=10min

[Install]
WantedBy=timers.target
```

El `.timer` activa al `.service` del mismo nombre. Se habilita **el timer**, no el servicio.

---

## `OnCalendar` y cómo comprobarlo

```console
$ systemd-analyze calendar "*:0/15"
Normalized form: *-*-* *:00/15:00
$ systemd-analyze calendar "Mon..Fri 08:00"
Normalized form: Mon..Fri *-*-* 08:00:00
$ systemd-analyze calendar "*-*-* 05:00:00 America/Mexico_City"
Normalized form: *-*-* 05:00:00 America/Mexico_City
    Next elapse: Mon 2026-11-09 11:00:00 UTC
```

- `daily`, `weekly` y `hourly` también valen.
- Con la zona horaria al final, la hora no depende de en qué zona esté el servidor.
- `systemd-analyze calendar` antes de confiar: dice la forma normalizada y la próxima vez que toca.

---

## `Persistent` y `RandomizedDelaySec`

- `Persistent=true`: si la máquina estaba apagada a la hora del timer, corre en cuanto arranque.
- `RandomizedDelaySec=10min`: agrega un retraso al azar de hasta 10 minutos. Si 50 servidores tienen la misma tarea a las 03:30, no golpean juntos a la misma base.
- `OnBootSec=5min` y `OnUnitActiveSec=1h`: en lugar de una hora del reloj, "5 minutos después de arrancar" y "una hora después de la última vez".

---

## `list-timers`: cuándo toca y cuándo tocó

```console
$ systemctl list-timers limpieza-api-lab.timer --no-pager
NEXT                        LEFT LAST PASSED UNIT                   ACTIVATES
Tue 2026-11-10 03:34:51 UTC  23h -         - limpieza-api-lab.timer limpieza-api-lab.service
```

- `NEXT` ya trae el retraso aleatorio: 03:34, no 03:30.
- `LAST` dice cuándo corrió por última vez, **no si salió bien**.
- Para eso: `systemctl status limpieza-api-lab.service` o `systemctl --failed`.
- Para probarlo sin esperar a las 03:30: `sudo systemctl start limpieza-api-lab.service`.

---

## Caso real: la limpieza nocturna de los runners

En el dev-stack del instructor, los jobs de CI corren en runners de GitHub Actions dentro de contenedores. Cada runner acumula checkouts, herramientas y artefactos en su carpeta de trabajo: decenas de GB en pocas semanas.

¿Por qué la limpieza no es un workflow programado de GitHub Actions?

- **Un job no puede detener al runner que lo corre**: al detenerlo, se mata a sí mismo a media limpieza.
- No se puede vaciar la carpeta de un runner que tiene un job corriendo.

La limpieza es un **timer de systemd en el host**, fuera de los runners: los ve a todos, espera a que cada uno esté libre, lo detiene, lo vacía y lo vuelve a arrancar.

<!--
Mensaje: elegir dónde corre una tarea es parte del diseño. Lo que administra a algo no puede vivir dentro
de eso que administra.
-->

---

## Caso real: cada línea de la unit es una decisión

| Directiva | Por qué así |
|---|---|
| `Type=oneshot` | Corre, termina y deja su resultado en el journal |
| `OnCalendar=... 05:00:00 America/Mexico_City` | El servidor está en UTC: con la zona explícita la hora no se mueve |
| `Persistent=false`, **a propósito** | Si el servidor estaba apagado, no limpiar al arrancar: es justo cuando se relanzan los jobs cortados |
| `TimeoutStartSec` largo | Si hay runners ocupados, reintenta cada tantos minutos antes de rendirse |
| `Nice=10` e `IOSchedulingClass=idle` | Que la limpieza no le quite CPU ni disco a los builds |
| `trap ... EXIT` en el script | Si algo falla a media limpieza, el runner que se detuvo vuelve a arrancar |

---

<!-- _class: lead -->

# Parte 6
## El journal

---

## `journalctl`: las preguntas de siempre

| Pregunta | Comando |
|---|---|
| ¿Qué dijo la API? | `sudo journalctl -u api-lab` |
| ¿Qué está diciendo ahora? | `sudo journalctl -u api-lab -f` |
| ¿Qué pasó desde este arranque? | `sudo journalctl -u api-lab -b` |
| ¿Y en el arranque anterior? | `sudo journalctl -u api-lab -b -1` |
| ¿Qué pasó entre las 2 y las 3? | `sudo journalctl --since "02:00" --until "03:00"` |
| Solo errores | `sudo journalctl -p err -b` |
| ¿Qué dijo el kernel? (OOM, discos) | `sudo journalctl -k -b` |

Si `journalctl` sin `sudo` dice `No entries`, su usuario no está en los grupos `adm` ni `systemd-journal`.

Fuente: Both, *systemd for Linux SysAdmins*, caps. 9 y 14.

---

## `-p err` no ve el ERROR de su script

```console
$ sudo journalctl -u respaldo-api-lab -p err --no-pager
vm-lab systemd[1]: Failed to start respaldo-api-lab.service - Respaldo verificado de /var/lib/api-lab.
```

- Lo que un servicio escribe en stdout o stderr llega al journal con prioridad **info**, aunque diga "ERROR".
- `-p err` solo muestra la línea de systemd. Para ver el porqué: `journalctl -u` sin `-p`.
- Si quieren que una línea cuente como error, empiécenla con `<3>`: systemd la guarda con prioridad 3 (err).

```console
$ sudo systemd-run -q --wait -p SyslogIdentifier=prueba-prio bash -c 'echo "<3>esto es un error" >&2'
$ sudo journalctl -t prueba-prio -p err -o cat
esto es un error
```

---

## Persistente, y cuánto ocupa

```bash
ls -ld /var/log/journal          # en Ubuntu 24.04 existe: el journal sobrevive al reinicio
sudo journalctl --disk-usage
sudo journalctl --list-boots     # un renglón por arranque guardado
sudo journalctl --vacuum-time=30d   # borrar lo de más de 30 días
```

- Con `Storage=auto` (el valor por defecto), si `/var/log/journal` existe, el journal se guarda en disco.
- Sin eso, todo se pierde al reiniciar, y `journalctl -b -1` no tiene nada que mostrar.
- El tope de espacio se fija en `/etc/systemd/journald.conf` con `SystemMaxUse=`.

---

## Momento de práctica: leer el OOM (10 min)

Después de provocar el OOM con `/memoria`:

```bash
sudo journalctl -u api-lab --since "-10min" --no-pager
sudo journalctl -k --since "-10min" --no-pager | grep -i "out of memory"
systemctl show api-lab -p NRestarts -p Result
```

Respondan en su evidencia:

1. ¿Qué línea dice que fue por memoria y no por un error de la API?
2. ¿Cuántos segundos pasaron entre la muerte y el nuevo arranque? ¿De qué directiva sale ese número?
3. ¿Qué proceso nombra el kernel y con cuánta memoria residente?

---

<!-- _class: lead -->

# Parte 7
## Práctica guiada: verlo fallar

---

## Paso 1: `kill -9` y volver

```bash
systemctl show api-lab -p MainPID -p NRestarts
sudo kill -9 "$(systemctl show -p MainPID --value api-lab)"
sleep 5
systemctl show api-lab -p MainPID -p NRestarts
```

```text
MainPID=563
NRestarts=0
MainPID=584
NRestarts=1
```

En el journal: `Failed with result 'signal'` y, 3 segundos después, `Started api-lab.service`.

---

## Paso 2: el OOM con `/memoria`

```bash
curl -s "http://127.0.0.1:8080/memoria?mb=100"; echo
systemctl show api-lab -p MemoryCurrent -p MemoryMax
curl -s "http://127.0.0.1:8080/memoria?mb=150"; echo "curl salió con $?"
```

```text
{"retenidos_mb": 100}
MemoryCurrent=114618368
MemoryMax=209715200
curl salió con 52
```

`curl` sale con 52 (respuesta vacía): la API murió a media petición. Confirmen en el journal que fue `oom-kill` y que volvió.

---

## Paso 3: el timer de limpieza

```bash
sudo -u api-lab touch -d "10 days ago" /var/lib/api-lab/tmp/viejo-1.json /var/lib/api-lab/tmp/viejo-2.json
sudo -u api-lab touch /var/lib/api-lab/tmp/nuevo.json
sudo systemctl start limpieza-api-lab.service
ls /var/lib/api-lab/tmp
sudo journalctl -u limpieza-api-lab --no-pager
```

```text
vm-lab find[658]: /var/lib/api-lab/tmp/viejo-2.json
vm-lab find[658]: /var/lib/api-lab/tmp/viejo-1.json
vm-lab systemd[1]: limpieza-api-lab.service: Deactivated successfully.
```

Los viejos se borraron, el nuevo se quedó, y el journal dice exactamente qué borró.

---

## Paso 4: `sudo reboot`

```bash
sudo reboot
# esperar un minuto y volver a entrar
ssh <USUARIO>@<IP_DE_SU_VM>
systemctl is-active api-lab
curl -s http://127.0.0.1:8080/health; echo
systemctl list-timers --no-pager | grep api-lab
sudo journalctl --list-boots --no-pager | tail -2
sudo journalctl -u api-lab -b -1 -n 3 --no-pager
```

- Nadie entra a levantar nada: la API responde, con un PID nuevo.
- `--list-boots` muestra dos arranques, y `-b -1` lo que dijo la API **antes** de reiniciar.

<!--
Si a un equipo no le vuelve la API, casi siempre es el enable que faltó. Que lo encuentren con
systemctl is-enabled antes de que se lo digas.
-->

---

## Paso 4 bis: verlo fallar en el reinicio

Si les sobra tiempo, demuestren que el `enable` es lo que la trae de vuelta:

1. `sudo systemctl disable api-lab` y `sudo reboot`.
2. Al volver: `systemctl is-active api-lab` dice `inactive`, y `curl` falla con código 7.
3. `sudo systemctl enable --now api-lab` y otro `sudo reboot`.
4. Al volver: `active`.

Una prueba de reinicio que nunca vieron fallar no prueba que el `enable` sea la razón.

---

## Evidencia que entregan

- `api-lab.service`, `limpieza-api-lab.service`, `limpieza-api-lab.timer` y `endurecer.conf`, con `systemd-analyze verify` sin errores.
- `systemctl status api-lab` después del arranque.
- El journal del `kill -9` y del OOM, con las respuestas del momento de práctica.
- La calificación de `systemd-analyze security` antes y después de `endurecer.conf`.
- La salida de los comandos del paso 4 **después** del reinicio.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Al terminar puedes

Dejar un servicio que arranca solo, se limita y se puede auditar.

- Leer el arranque y saber qué lo retrasa.
- Escribir una unit con usuario propio, reinicio automático y límites, y validarla con `verify`.
- Distinguir `enable` de `start`, y `Wants` de `After`.
- Cambiar una unit de paquete con un drop-in, sin editarla.
- Reemplazar cron por un timer y saber cuándo corrió y si falló.
- Leer el journal por unit, por arranque y por prioridad.

---

## Tarea

**Un timer que corra su respaldo de la S03 y deje rastro en el journal.**

- `respaldo-api-lab.service` (`Type=oneshot`) que corre `/usr/local/bin/respaldo.sh /var/lib/api-lab /var/backups/api-lab`.
- `respaldo-api-lab.timer` con hora fija y la decisión de `Persistent` justificada por escrito.
- La corrida buena y una corrida **fallida** a propósito, vistas en el journal y en `systemctl status`.

Instrucciones completas en la tarea de la sesión.

---

## Lecturas y libros

- `dev-notes/10-cloud/20-systemd.md`: la nota de hoy, completa.
- Both, *systemd for Linux SysAdmins* (Apress): caps. 2 (arranque de Linux), 3 (el arranque con systemd), 4 (administrar el arranque), 5 (units con `systemctl`), 8 (timers), 9 (el journal), 11 (recursos con cgroups) y 14 (sacarle más al journal).

---

## Siguiente sesión: red, firewall y SSH

> Lo que escucha en un puerto es superficie de ataque, se haya planeado o no.

- La API escucha en `127.0.0.1:8080`: hoy nadie de fuera la alcanza.
- En la S05 la publicamos detrás de Nginx, cerramos todo lo demás y lo probamos desde otra máquina.
- Y vamos a ver cómo un contenedor de Docker se salta el firewall.

Traigan la API como servicio y sobreviviendo al reinicio: es el punto de partida.
