# Glosario - S04 - Servicios con systemd

> Los términos de la sesión 4, en orden alfabético.

---

## A

### After y Before
**Categoría:** systemd / Dependencias
**Definición:** Directivas de orden. `After=b` dice que, si las dos units arrancan, esta va después de `b`; `Before=b`, que va antes. No arrancan nada por sí mismas: solo ordenan. Por eso se usan en pareja con `Wants=` o `Requires=`.
**En el curso:** `api-lab.service` lleva `After=network-online.target` junto con `Wants=network-online.target`.

---

## C

### cgroup
**Categoría:** Linux / Kernel
**Definición:** Grupo de control: un conjunto de procesos que el kernel mide y limita junto (memoria, CPU, número de tareas, disco). systemd pone cada unit en su propio cgroup, así que los procesos hijos de un servicio quedan dentro del mismo límite y no se le escapan.
**En el curso:** `systemd-cgtop` muestra `system.slice/api-lab.service` con su memoria; el OOM de la práctica mató a la API dentro de su cgroup sin tocar al resto de la VM.

---

### CPUQuota
**Categoría:** systemd / Recursos
**Definición:** Directiva que limita cuánta CPU puede usar una unit, como porcentaje de un núcleo por unidad de tiempo. `CPUQuota=50%` deja medio núcleo; `200%` dejaría dos.
**En el curso:** un ciclo infinito con `CPUQuota=20%` usó 1.01 s de CPU en 5 s.

---

## D

### daemon-reload
**Categoría:** systemd / Comandos
**Definición:** `systemctl daemon-reload` hace que systemd vuelva a leer todos los archivos de units. Sin él, systemd sigue usando la versión que cargó antes, aunque el archivo en disco haya cambiado, y lo avisa con `changed on disk`. `systemctl edit` lo hace por su cuenta al guardar.

---

### Drop-in
**Categoría:** systemd / Configuración
**Definición:** Archivo `.conf` dentro de `nombre.service.d/` que agrega o reemplaza directivas de una unit sin editar el archivo original. Es la forma de cambiar units de paquetes sin que una actualización borre el cambio. `systemctl cat` muestra la unit con todos sus drop-ins y `systemctl revert` los borra todos.
**En el curso:** `endurecer.conf` bajó la calificación de seguridad de la API de 8.3 a 2.2.

---

## E

### enable
**Categoría:** systemd / Comandos
**Definición:** `systemctl enable` crea los enlaces que indica la sección `[Install]` (por ejemplo, en `multi-user.target.wants/`) para que la unit arranque en cada arranque del sistema. No la arranca ahora: eso es `start`. `enable --now` hace las dos cosas; `disable` quita los enlaces.
**En el curso:** con `disable`, la API no volvió tras reiniciar; con `enable`, sí.

---

### EnvironmentFile
**Categoría:** systemd / Configuración
**Definición:** Directiva que carga variables de entorno desde un archivo de líneas `CLAVE=valor`. Con un guion delante (`EnvironmentFile=-/ruta`), el archivo es opcional. systemd lo lee como root, así que puede tener permisos `600`. Lo que define gana sobre lo que define `Environment=`.
**En el curso:** `/etc/api-lab/env` fija `API_HOST` y `API_PORT`; un drop-in con `Environment=API_PORT=8090` no movió el puerto mientras ese archivo existía.

---

## J

### journalctl
**Categoría:** systemd / Logs
**Definición:** Comando para consultar el journal, el registro central donde systemd guarda lo que escriben los servicios, el kernel y el propio systemd. Se filtra por unit (`-u`), por arranque (`-b`, `-b -1`), por prioridad (`-p err`), por tiempo (`--since`, `--until`) o en vivo (`-f`). Lo que un servicio escribe en stdout o stderr llega con prioridad info.
**En el curso:** `sudo journalctl -u api-lab` mostró `Failed with result 'oom-kill'` y el reinicio que siguió.

---

## M

### mask
**Categoría:** systemd / Comandos
**Definición:** `systemctl mask` enlaza una unit a `/dev/null` para que no pueda arrancar por ningún camino, ni a mano ni como dependencia de otra. Es más fuerte que `disable`. Sirve para units de paquetes: si la unit vive en `/etc/systemd/system`, el archivo ya ocupa el lugar del enlace y `mask` falla.

---

### MemoryMax
**Categoría:** systemd / Recursos
**Definición:** Tope duro de memoria del cgroup de una unit. Si los procesos del grupo lo rebasan y el kernel no puede recuperar memoria, el OOM killer mata a uno de ellos. Se escribe con sufijos `K`, `M`, `G` (`200M`); `200MB` no es válido y systemd lo ignora con un aviso.

---

### MemorySwapMax
**Categoría:** systemd / Recursos
**Definición:** Tope de swap que puede usar el cgroup de una unit. Con `MemorySwapMax=0`, el servicio no puede mandar memoria a swap y `MemoryMax` se vuelve un límite real. Sin esa línea, en una máquina con swap, el servicio puede seguir creciendo por encima de `MemoryMax`.
**En el curso:** sin esta directiva, la API retuvo 250 MB con un tope de 200 MB.

---

## O

### OnCalendar
**Categoría:** systemd / Timers
**Definición:** Directiva de un `.timer` que fija cuándo dispararse según el reloj: `*-*-* 03:30:00`, `Mon..Fri 08:00`, `*:0/15` o abreviaturas como `daily`. Acepta una zona horaria al final. `systemd-analyze calendar "expresión"` muestra la forma normalizada y la próxima vez que toca.

---

### oneshot
**Categoría:** systemd / Tipos de servicio
**Definición:** Tipo de servicio (`Type=oneshot`) para tareas que corren y terminan: systemd espera a que el proceso salga y usa su código de salida como resultado. Es el tipo de las tareas que activa un timer: limpiezas, respaldos, reportes.
**En el curso:** `limpieza-api-lab.service` y `respaldo-api-lab.service`.

---

### OOM killer
**Categoría:** Linux / Kernel
**Definición:** Mecanismo del kernel que, cuando falta memoria, elige un proceso y lo mata con `SIGKILL` para liberarla. Dentro de un cgroup con `MemoryMax`, actúa solo sobre los procesos de ese grupo. El kernel lo registra como `Memory cgroup out of memory` y systemd marca la unit con el resultado `oom-kill`.

---

## P

### Persistent
**Categoría:** systemd / Timers
**Definición:** Directiva de un `.timer`. Con `Persistent=true`, systemd guarda cuándo corrió por última vez y, si la máquina estaba apagada a la hora programada, lo corre en cuanto arranca. Con `false`, la corrida perdida se pierde. La decisión depende de la tarea.
**En el curso:** la limpieza de los runners del dev-stack usa `false` a propósito: el arranque es justo cuando se relanzan los jobs cortados.

---

### PID 1
**Categoría:** Linux / Procesos
**Definición:** El primer proceso que arranca el kernel en el espacio de usuario; en Ubuntu es systemd. Arranca todo lo demás y adopta a los procesos que quedan huérfanos. Si termina, el kernel entra en pánico.

---

## R

### Restart
**Categoría:** systemd / Servicios
**Definición:** Directiva que decide si systemd vuelve a arrancar un servicio que terminó. `no` (por defecto) nunca; `on-failure` si salió con código distinto de 0, lo mató una señal o venció un tiempo; `always` por cualquier motivo. `RestartSec=` fija la espera entre intentos. Detenerlo con `systemctl stop` nunca dispara un reinicio.

---

## S

### StartLimitBurst
**Categoría:** systemd / Servicios
**Definición:** Junto con `StartLimitIntervalSec=` (en la sección `[Unit]`), fija cuántos arranques se permiten en una ventana de tiempo. Si se rebasan, systemd deja la unit en `failed` con `Start request repeated too quickly` y deja de intentar. Cuentan también los arranques a mano; `systemctl reset-failed` borra la cuenta.

---

### StateDirectory
**Categoría:** systemd / Configuración
**Definición:** Directiva que pide a systemd crear `/var/lib/NOMBRE` antes de arrancar el servicio, con dueño el `User=` de la unit, y dejarla escribible aunque el resto del sistema esté en solo lectura por `ProtectSystem=strict`.
**En el curso:** `StateDirectory=api-lab` crea y mantiene `/var/lib/api-lab`.

---

### systemd-analyze
**Categoría:** systemd / Herramientas
**Definición:** Herramienta para examinar systemd: `verify` revisa archivos de units y reporta lo que se ignoraría; `security` califica el endurecimiento de un servicio; `calendar` interpreta expresiones de `OnCalendar`; `blame` y `critical-chain` muestran qué retrasó el arranque.
**En el curso:** `verify` detectó `Restart=onfailure` y `MemoryMax=200MB`, que systemd habría ignorado en silencio.

---

## T

### Target
**Categoría:** systemd / Units
**Definición:** Unit que agrupa a otras y marca un punto del arranque, como `multi-user.target` (sistema completo sin escritorio) o `timers.target`. Las units se cuelgan de un target con `WantedBy=` en su sección `[Install]`; `systemctl get-default` dice a qué target quiere llegar el arranque.

---

### Timer
**Categoría:** systemd / Units
**Definición:** Unit `.timer` que activa a otra (por defecto, el `.service` del mismo nombre) a una hora (`OnCalendar=`) o después de un intervalo (`OnBootSec=`, `OnUnitActiveSec=`). Frente a cron, su salida queda en el journal, no arranca otra copia si la anterior sigue corriendo y puede recuperar corridas perdidas con `Persistent=`. `systemctl list-timers` dice cuándo toca y cuándo corrió, pero no si salió bien.

---

## U

### Unit
**Categoría:** systemd / Conceptos
**Definición:** Archivo de texto que describe algo que systemd administra: un servicio (`.service`), un horario (`.timer`), un socket, un montaje, un target. Las de los paquetes viven en `/usr/lib/systemd/system`; las del administrador, en `/etc/systemd/system`, que gana si hay dos con el mismo nombre.

---

## W

### Wants y Requires
**Categoría:** systemd / Dependencias
**Definición:** Directivas de requerimiento. `Wants=b` arranca también `b`, pero si `b` falla esta unit sigue; `Requires=b` es más estricto: si `b` se detiene, esta también, y si además lleva `After=b`, no arranca cuando `b` falla. No ordenan: para eso están `After=` y `Before=`.
