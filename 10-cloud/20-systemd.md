# 20 - Servicios con systemd

> Fuente: *systemd for Linux SysAdmins* (Both, Apress): caps. 2 Linux Boot and Startup, 3 Understanding Linux Startup with systemd, 4 How to Manage Startup Using systemd, 5 Manage systemd Units with systemctl, 8 Using systemd Timers, 9 Using systemd Journals, 11 Resource Management with cgroups y 14 Getting More Out of the Journal

## Problema que resuelve

Una API que se arrancó con `nohup dotnet Host.dll &` desde una sesión SSH no está desplegada: muere si el proceso truena, no vuelve después de reiniciar el servidor, corre con el usuario de quien la lanzó, puede comerse toda la memoria y sus logs se pierden. systemd resuelve las cinco cosas con un archivo de texto: arranca el servicio al encender, lo reinicia si se cae, lo corre con su propio usuario, le pone límites con cgroups y guarda lo que imprime en el journal.

[03-linux-nginx.md](03-linux-nginx.md) trae los comandos básicos de `systemctl`. Aquí está el porqué de cada línea de una unit.

---

## Del encendido al login

| Etapa | Quién | Qué hace |
|-------|-------|----------|
| 1 | Firmware (UEFI o BIOS) | Revisa el hardware y busca un cargador en el disco |
| 2 | Cargador (GRUB) | Carga el kernel y el initramfs en memoria, con sus parámetros |
| 3 | Kernel + initramfs | Detecta dispositivos, monta el sistema de archivos raíz real |
| 4 | systemd (PID 1) | Lee las units y arranca todo lo que pide el target por omisión, en paralelo |
| 5 | `default.target` | En Ubuntu Server suele apuntar a `graphical.target`, que jala `multi-user.target`: ahí cuelgan los servicios |

```bash
systemctl get-default                      # el target al que llega el arranque
systemd-analyze                            # cuánto tardó el arranque, por etapa
systemd-analyze blame | head               # las units que más tardaron
systemd-analyze critical-chain             # la cadena que de verdad retrasó el arranque
systemctl list-dependencies multi-user.target | head
```

`blame` engaña un poco: una unit lenta que corre en paralelo no retrasa a nadie. La que retrasa es la que está en `critical-chain`.

---

## Units: qué son y dónde viven

Una **unit** es un archivo de texto que describe algo que systemd administra. El tipo va en la extensión:

| Tipo | Para qué |
|------|----------|
| `.service` | Un proceso: su API, Nginx, Postgres |
| `.timer` | Cuándo arrancar un `.service` (reemplaza a cron) |
| `.socket` | Escuchar un puerto y arrancar el servicio al primer cliente (`ssh.socket` en Ubuntu 24.04) |
| `.mount` | Un punto de montaje (systemd genera uno por cada línea de `/etc/fstab`) |
| `.target` | Un grupo de units, como un "nivel de arranque" |
| `.path` | Arrancar algo cuando cambia un archivo |

| Carpeta | Quién la escribe | Prioridad |
|---------|------------------|-----------|
| `/usr/lib/systemd/system/` | Los paquetes (apt) | La más baja: no la edites, una actualización la pisa |
| `/etc/systemd/system/` | Tú | Gana sobre la del paquete |
| `/etc/systemd/system/<unit>.d/*.conf` | Tú, con `systemctl edit` | Drop-in: cambia solo las líneas que pones |
| `/run/systemd/system/` | Generadas en el arranque | Temporales |

Para cambiar una unit de un paquete **no se copia ni se edita**: se le pone un drop-in.

```bash
sudo systemctl edit nginx            # abre el editor y guarda en /etc/systemd/system/nginx.service.d/override.conf
systemctl cat nginx                  # la unit completa, con sus drop-ins y de qué archivo sale cada parte
systemctl status nginx               # la línea "Drop-In:" dice que hay cambios locales
sudo systemctl revert nginx          # quita tus drop-ins y regresa a la versión del paquete
```

Dependencias: dos ejes que se confunden.

| Directiva | Qué dice | Qué NO dice |
|-----------|----------|-------------|
| `Wants=b` | Cuando arranques, arranca también b; si b falla, sigue | Nada sobre el orden |
| `Requires=b` | Igual, pero si b no logra arrancar (con `After=b`) o alguien la detiene, tú también te detienes | Nada sobre el orden |
| `After=b` | Si los dos van a arrancar, tú vas después de b | No arranca a b |
| `Before=b` | Tú vas antes que b | No arranca a b |

Casi siempre se usan en pareja: `Wants=network-online.target` + `After=network-online.target` significa "arráncala y espérala".

---

## systemctl: el día a día

| Comando | Qué hace |
|---------|----------|
| `systemctl start / stop / restart api` | Ahora mismo |
| `systemctl reload api` | Releer configuración sin cortar conexiones (si el servicio lo soporta) |
| `systemctl enable api` | Que arranque en cada encendido (crea el enlace en `multi-user.target.wants/`) |
| `systemctl enable --now api` | Las dos cosas a la vez |
| `systemctl status api` | Estado, PID, memoria, límites y las últimas líneas del journal |
| `systemctl is-active api` / `is-enabled api` | Para scripts: contestan con una palabra y un código de salida |
| `systemctl --failed` | Lo que está roto ahora |
| `systemctl daemon-reload` | Obligatorio después de crear o editar una unit a mano |
| `systemctl mask apt-daily-upgrade.timer` | Que nadie pueda arrancarla (ni otra unit). Sirve para units de paquetes, no para las tuyas de `/etc` |
| `systemctl show api -p MainPID -p NRestarts` | Cualquier propiedad, para scripts |

`enable` y `start` son cosas distintas: `start` sin `enable` funciona hasta el próximo reinicio; `enable` sin `start` funciona a partir del próximo reinicio. La prueba de que algo está desplegado es reiniciar el servidor y ver que volvió solo.

---

## Una unit para su API

Ejemplo para una API ASP.NET Core publicada en `/opt/{{PROJECT_SLUG}}`. Se probó tal cual (con una app `dotnet new web`): arranca, responde, y con el endurecimiento de abajo `systemd-analyze security` la califica 2.2 OK.

```ini
# /etc/systemd/system/{{PROJECT_SLUG}}.service
[Unit]
Description={{PROJECT_NAME}} API
After=network-online.target
Wants=network-online.target
# Si truena 5 veces en 2 minutos, systemd deja de insistir y la marca como fallida
StartLimitIntervalSec=120
StartLimitBurst=5

[Service]
Type=simple
User={{PROJECT_SLUG}}
Group={{PROJECT_SLUG}}
WorkingDirectory=/opt/{{PROJECT_SLUG}}
ExecStart=/usr/bin/dotnet /opt/{{PROJECT_SLUG}}/Host.dll
Environment=ASPNETCORE_ENVIRONMENT=Production
# Solo en localhost: el que da la cara a internet es Nginx
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
# Secretos: archivo 600 de root. Lo lee systemd ANTES de bajar privilegios; la app solo ve las variables
EnvironmentFile=/etc/{{PROJECT_SLUG}}/env
SyslogIdentifier={{PROJECT_SLUG}}
Restart=on-failure
RestartSec=5
# ASP.NET Core cierra ordenado con SIGTERM; si en 30 s no terminó, systemd manda SIGKILL
TimeoutStopSec=30

# Límites (cgroups)
MemoryMax=512M
MemorySwapMax=0
CPUQuota=100%
TasksMax=256

# Endurecimiento
NoNewPrivileges=yes
ProtectSystem=strict
ProtectHome=yes
PrivateTmp=yes
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
StateDirectory={{PROJECT_SLUG}}

[Install]
WantedBy=multi-user.target
```

```bash
# El usuario de servicio: sin shell, sin home
sudo useradd --system --no-create-home --shell /usr/sbin/nologin {{PROJECT_SLUG}}

# Los secretos, legibles solo por root
sudo install -d -m 755 /etc/{{PROJECT_SLUG}}
sudo install -m 600 /dev/null /etc/{{PROJECT_SLUG}}/env
sudoedit /etc/{{PROJECT_SLUG}}/env          # ConnectionStrings__Default=...

sudo systemd-analyze verify /etc/systemd/system/{{PROJECT_SLUG}}.service   # sin salida = sin errores
sudo systemctl daemon-reload
sudo systemctl enable --now {{PROJECT_SLUG}}
curl -s http://127.0.0.1:5000/health
```

Dos detalles que se descubren tarde: si una variable aparece en `Environment=` y también en el `EnvironmentFile=`, **gana la del archivo**, sin importar el orden de las líneas; y una directiva mal escrita (`Restart=onfailure`, `MemoryMax=200MB`) se ignora en silencio al arrancar: solo `systemd-analyze verify` la reporta.

`Restart=` decide cuándo vuelve:

| Valor | Reinicia si... |
|-------|----------------|
| `no` | Nunca (por omisión) |
| `on-failure` | Sale con código distinto de 0, lo mata una señal, se pasa de tiempo o lo mata el OOM. Lo normal para una API |
| `always` | Siempre, incluso si salió con 0. Para procesos que nunca deberían terminar |
| `on-abnormal` | Solo señales, tiempos y watchdog, no códigos de salida |

`systemctl stop` nunca dispara el reinicio: una parada pedida no es una falla.

Con `Type=simple` systemd da el servicio por arrancado en cuanto lanza el proceso. Si la app llama a `UseSystemd()` (paquete `Microsoft.Extensions.Hosting.Systemd`) se puede usar `Type=notify`: systemd espera a que la app avise que ya está lista, y lo que dependa de ella arranca después.

---

## Límites con cgroups

Cada unit vive en su propio **cgroup**: un grupo de procesos al que el kernel le cuenta y le limita memoria, CPU y número de tareas. Los hijos que lance la API heredan el grupo, así que no se escapan del límite.

| Directiva | Qué limita | Al llegar al límite |
|-----------|-----------|---------------------|
| `MemoryMax=512M` | RAM del grupo | El OOM killer del kernel mata un proceso del grupo; la unit queda con resultado `oom-kill` y `Restart=on-failure` la levanta |
| `MemoryHigh=400M` | Umbral suave | El kernel frena al grupo y le quita memoria, sin matarlo |
| `MemorySwapMax=0` | Swap del grupo | Sin esto, si hay swap, el kernel manda a swap en lugar de matar y el límite "se estira" (comprobado: con swap y sin esta línea no hubo OOM) |
| `CPUQuota=50%` | Tiempo de CPU (100% = un núcleo completo) | Se le da menos CPU: va más lento, no muere |
| `TasksMax=256` | Procesos e hilos | Falla la creación de hilos nuevos |

```bash
systemctl status api                                      # Memory: 9.1M (max: 200.0M available: 190.8M peak: 9.1M)
systemctl show api -p MemoryCurrent -p MemoryPeak -p NRestarts
systemd-cgtop                                             # como top, pero por cgroup
journalctl -u api | grep -i oom
# api.service: A process of this unit has been killed by the OOM killer.
# api.service: Failed with result 'oom-kill'.

# Probar un límite sin escribir una unit: systemd-run crea una transitoria
sudo systemd-run --wait -p MemoryMax=50M -p MemorySwapMax=0 python3 -c 'b = b"x" * (80 * 1024 * 1024)'
echo $?      # 1: lo mató el límite
```

---

## Endurecer la unit y medirlo

Las directivas de endurecimiento le quitan al proceso lo que no necesita. Si alguien explota la API, se encuentra un sistema en solo lectura, sin `/home`, sin dispositivos, sin poder cargar módulos ni ganar privilegios.

| Directiva | Qué quita |
|-----------|-----------|
| `NoNewPrivileges=yes` | Subir privilegios con setuid (`sudo`, `su`) |
| `ProtectSystem=strict` | Escribir en cualquier parte del sistema; solo quedan las carpetas que declares (`StateDirectory=`, `ReadWritePaths=`) |
| `ProtectHome=yes` | Ver `/home`, `/root` y `/run/user` |
| `PrivateTmp=yes` | El `/tmp` compartido: tiene uno propio |
| `PrivateDevices=yes` | Los dispositivos de `/dev` salvo los básicos |
| `ProtectKernelTunables`, `ProtectKernelModules`, `ProtectControlGroups` | Cambiar `/proc/sys`, cargar módulos o tocar los cgroups |
| `RestrictAddressFamilies=AF_INET AF_INET6 AF_UNIX` | Sockets raros (raw, netlink, bluetooth) |
| `CapabilityBoundingSet=` | Todas las capabilities de root |
| `SystemCallFilter=@system-service` | Llamadas al sistema fuera del conjunto típico de un servicio |
| `StateDirectory={{PROJECT_SLUG}}` | Crea `/var/lib/{{PROJECT_SLUG}}` con el dueño correcto y la deja escribible |

```bash
systemd-analyze security {{PROJECT_SLUG}}      # tabla de lo que falta y una calificación de 0 (bien) a 10 (mal)
```

Medido con la API de laboratorio del curso: sin endurecer, **9.2 UNSAFE**; con `NoNewPrivileges`, `ProtectSystem`, `ProtectHome` y `PrivateTmp`, **8.3 EXPOSED**; con la lista completa de arriba, **2.2 OK**, y la API siguió respondiendo. No uses `MemoryDenyWriteExecute=yes` con .NET ni con Node: rompe su compilador JIT (comprobado con .NET 8: "Failed to create CoreCLR" y la unit no arranca).

---

## Timers en lugar de cron

Un timer es una unit que arranca un `.service` cuando toca. El trabajo va en un `.service` de `Type=oneshot` con el mismo nombre.

```ini
# /etc/systemd/system/respaldo-{{PROJECT_SLUG}}.service
[Unit]
Description=Respaldo de {{PROJECT_NAME}}

[Service]
Type=oneshot
ExecStart=/usr/local/bin/respaldo.sh /var/lib/{{PROJECT_SLUG}} /var/backups/{{PROJECT_SLUG}}
Nice=10
IOSchedulingClass=idle
```

```ini
# /etc/systemd/system/respaldo-{{PROJECT_SLUG}}.timer
[Unit]
Description=Respaldo diario de {{PROJECT_NAME}}

[Timer]
OnCalendar=*-*-* 03:30:00
# Si el servidor estaba apagado a esa hora, corre al arrancar
Persistent=true
# Hasta 10 min de retraso al azar: que no arranquen todos los servidores al mismo segundo
RandomizedDelaySec=10min

[Install]
WantedBy=timers.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now respaldo-{{PROJECT_SLUG}}.timer      # se habilita el TIMER, no el service
systemctl list-timers                                           # próxima y última corrida de cada timer
sudo systemctl start respaldo-{{PROJECT_SLUG}}.service          # correrlo ya, para probar
systemd-analyze calendar "Mon..Fri 08:00" "*-*-* 05:00:00 America/Mexico_City"   # qué significa una expresión
```

| Expresión `OnCalendar=` | Cuándo |
|-------------------------|--------|
| `daily` | Todos los días a las 00:00 |
| `*-*-* 03:30:00` | Todos los días a las 03:30 |
| `Mon..Fri 08:00` | De lunes a viernes a las 08:00 |
| `*:0/15` | Cada 15 minutos |
| `*-*-* 05:00:00 America/Mexico_City` | A las 05:00 de esa zona aunque el servidor esté en UTC (son las 11:00 UTC) |

También hay timers relativos: `OnBootSec=5min` (5 minutos después de arrancar) y `OnUnitActiveSec=1h` (una hora después de la última corrida).

| | cron | timer de systemd |
|---|------|------------------|
| Logs | Se pierden o llegan por correo | En el journal, con `journalctl -u` |
| ¿Falló la última corrida? | No se sabe | `systemctl status` y `--failed` |
| Corridas encimadas | Puede lanzar otra mientras la anterior sigue | No arranca el service si sigue activo (comprobado: un oneshot de 15 s con timer cada 3 s arrancó una sola vez) |
| Servidor apagado a la hora | Se la salta | `Persistent=true` la corre al arrancar |
| Límites y endurecimiento | No | Todas las directivas de cualquier service |
| Dependencias | No | `Requires=`, `After=` |

---

## Caso real: la limpieza nocturna de los runners

En el dev-stack del autor, los runners de GitHub Actions corren en contenedores y acumulan en su carpeta de trabajo (`_work`) checkouts, herramientas y artefactos: decenas de GB en pocas semanas. La limpieza es un **timer de systemd en el host y no un workflow**, por dos razones:

1. **Un job no puede detener al runner que lo corre.** Para vaciar `_work` con seguridad hay que detener el runner; si la limpieza fuera un job, al detener su runner se mataría a sí misma.
2. **No se puede vaciar el `_work` de un runner ocupado.** El script revisa cada runner, limpia los libres y reintenta los ocupados más tarde. Eso solo lo puede orquestar alguien que esté fuera de todos los runners.

```ini
# limpiar-runners.timer
[Unit]
Description=Limpieza diaria de los runners de CI, 05:00 de Ciudad de México

[Timer]
# El servidor está en UTC: con la zona explícita, la hora no se mueve
OnCalendar=*-*-* 05:00:00 America/Mexico_City
# A propósito: si el servidor estaba apagado a las 05:00, NO correr al arrancar,
# que es justo cuando se relanzan los jobs que se cortaron
Persistent=false

[Install]
WantedBy=timers.target
```

```ini
# limpiar-runners.service
[Unit]
Description=Vacía las carpetas de trabajo de los runners de CI que estén libres
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
User=ci
Group=docker
ExecStart=/opt/mantenimiento/limpiar-runners.sh
# Con runners ocupados reintenta cada 10 min, hasta 6 veces: en el peor caso, cerca de una hora
TimeoutStartSec=2h
Nice=10
IOSchedulingClass=idle
```

Lo que enseña cada línea:

- `Persistent=false` es una decisión, no un olvido: aquí correr "lo que se perdió" sería dañino.
- `TimeoutStartSec=2h`: un oneshot no tiene límite de tiempo por omisión (`TimeoutStartUSec=infinity`); este le pone un techo que alcanza para los reintentos, y si el script se cuelga, systemd lo mata y la corrida queda como fallida.
- `Nice=10` e `IOSchedulingClass=idle`: la limpieza cede CPU y disco a los builds que estén corriendo.
- El script lleva un `trap ... EXIT` que vuelve a arrancar el runner si algo falla a media limpieza (ver [19-bash-scripting.md](19-bash-scripting.md)): sin eso, un error dejaría un runner apagado hasta que alguien lo note.

---

## El journal

Todo lo que un service escribe en stdout y stderr llega al journal con su unit, su PID y la hora. En Ubuntu 24.04 existe `/var/log/journal`, así que el journal **es persistente**: sobrevive a los reinicios y permite ver el arranque anterior.

```bash
journalctl -u api                         # todo lo de una unit
journalctl -u api -f                      # en vivo
journalctl -u api --since "10 min ago"    # o --since "2026-11-07 14:00" --until "2026-11-07 14:30"
journalctl -u api -b                      # desde el arranque actual; -b -1 es el arranque anterior
journalctl -p err -b                      # solo errores (o peores) de todo el sistema
journalctl -k                             # mensajes del kernel (el OOM killer firma aquí)
journalctl -u api -o cat                  # solo el mensaje, sin fecha ni host
journalctl -u api -o json-pretty -n 1     # todos los campos: _PID, _SYSTEMD_UNIT, PRIORITY...
journalctl -t respaldo                    # por identificador (SyslogIdentifier=, logger -t, systemd-cat -t)
journalctl --list-boots
journalctl --disk-usage
sudo journalctl --vacuum-size=500M        # recortar los archivos viejos hasta ocupar 500 MB
```

Para que un script que no corre como service deje rastro:

```bash
logger -t respaldo "inicio del respaldo"            # una línea
./respaldo.sh 2>&1 | systemd-cat -t respaldo        # todo lo que imprima
```

Prioridades (`-p`): 0 emerg, 1 alert, 2 crit, 3 err, 4 warning, 5 notice, 6 info, 7 debug. `-p warning` muestra de la 4 hacia arriba en gravedad.

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Una unit con `Restart=on-failure` para cada API que corra directo en la VM | `nohup`, `screen` o `tmux` para dejar un servicio corriendo |
| Drop-ins (`systemctl edit`) para cambiar units de paquetes | Editar archivos de `/usr/lib/systemd/system`: la próxima actualización los pisa |
| Timers para tareas programadas en el servidor | Un workflow de CI para mantener el propio runner (no puede detenerse a sí mismo) |
| `MemoryMax` + `MemorySwapMax=0` en todo servicio | Límites sin probarlos: provoca el OOM una vez y míralo en el journal |
| `systemd-analyze verify` y `security` antes de habilitar | Dar por buena una unit porque `daemon-reload` no se quejó |
| `restart: unless-stopped` si el servicio vive en Docker Compose | Una unit de systemd que envuelve `docker run` cuando ya usas Compose |

---

## Glosario

| Término | Definición |
|---------|-----------|
| systemd | Sistema de arranque y administrador de servicios de Linux; corre como PID 1 |
| Unit | Archivo de texto que describe algo que systemd administra: servicio, timer, socket, montaje, target |
| Target | Unit que agrupa otras; marca etapas del arranque como `multi-user.target` |
| Drop-in | Archivo `.conf` en `<unit>.d/` que cambia solo algunas líneas de una unit |
| `daemon-reload` | Orden para que systemd vuelva a leer las units del disco |
| Oneshot | Tipo de service que corre una tarea y termina; el que usan los timers |
| Timer | Unit que arranca un service según el calendario o un tiempo relativo |
| `Persistent=` | Opción del timer para correr al arrancar lo que se perdió con el servidor apagado |
| cgroup | Grupo de procesos al que el kernel le cuenta y le limita recursos |
| `MemoryMax` | Límite duro de memoria del cgroup de una unit |
| OOM killer | Mecanismo del kernel que mata procesos cuando no queda memoria, o cuando un cgroup pasa su límite |
| Journal | Registro binario e indexado de systemd con los logs de todas las units y del kernel |
| journalctl | Herramienta para consultar el journal |
| `systemd-analyze` | Herramienta para medir el arranque, verificar units, calificar su seguridad y probar expresiones de calendario |
| Activación por socket | systemd escucha el puerto y arranca el servicio cuando llega el primer cliente |
| Endurecimiento | Directivas que le quitan al servicio permisos y vistas del sistema que no necesita |

---

*Rogelio Arriaga Gonzalez*
