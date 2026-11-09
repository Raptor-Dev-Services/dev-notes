<!-- _class: lead -->

# S06 - Observar y diagnosticar

### Linux para desarrolladores

**Rogelio Arriaga - Raptor Dev Services**

<!--
Última sesión. Hoy no se instala nada nuevo: se rompe lo que ya tienen y se arregla con método.
Antes de empezar: armen parejas de equipos. Cada equipo siembra incidentes en la VM del otro con sembrar.sh. Nadie toca la VM ajena hasta el bloque de práctica.
-->

---

## La idea de hoy

> Diagnosticar es un método, no una corazonada.

- La corazonada funciona con lo que ya viste; el método funciona con lo que no
- Un método se escribe, se repite y lo puede seguir otra persona
- Reiniciar no es diagnosticar: borra la evidencia y a veces "arregla" por accidente

<!-- Pregunten: ¿cuántos han resuelto algo reiniciando sin saber qué era? La siguiente vez que pase, no van a saber qué hacer. -->

---

## Agenda del día

| Bloque | Tema |
|---|---|
| 1 | Diagnosticar es un método |
| 2 | La lista de cinco minutos |
| 3 | CPU, memoria y procesos |
| 4 | Disco lleno |
| 5 | Almacenamiento y fstab |
| 6 | Servicios y puertos |
| 7 | Leer logs con método |
| 8 | Práctica: tres incidentes sembrados |
| 9 | Cierre y proyecto final |

---

## De dónde venimos

Su VM ya tiene todo lo del curso:

- `api-lab` con usuario propio, límites y reinicio automático (S2 y S4)
- Respaldo que avisa y un timer que lo corre (S3 y S4)
- Nginx delante, UFW cerrando lo demás, SSH con llave (S5)

Hoy: cuando algo de eso falle, encontrar **por qué** en minutos y con un procedimiento que otro pueda repetir.

---

<!-- _class: lead -->

# Parte 1
## Diagnosticar es un método

---

## El ciclo

1. **Síntoma:** qué se observa, con hora y con quién lo vio
2. **Hipótesis:** qué podría causarlo (varias, no una)
3. **Medición:** el comando que confirma o descarta cada hipótesis
4. **Un cambio a la vez:** si cambias tres cosas, no sabes cuál arregló
5. **Verificar:** el síntoma original ya no ocurre, medido igual que al principio
6. **Anotar:** qué pasó, cómo se vio, qué se hizo

Fuente: *Practical Linux System Administration* (Hess), cap. 12.

---

## Corazonada frente a método

| Corazonada | Método |
|---|---|
| "Seguro es la base" | ¿Qué dice el log de la API a esa hora? |
| Reiniciar primero | Medir primero, reiniciar al final si hace falta |
| Cambiar tres cosas a la vez | Un cambio, verificar, el siguiente |
| "Ya quedó" | El `curl` que fallaba ahora da 200 |
| Se queda en tu cabeza | Se queda en el runbook |

---

## El método USE

Para **cada recurso** (CPU, memoria, disco, red), tres preguntas:

- **Utilización:** ¿qué tan ocupado está?
- **Saturación:** ¿hay trabajo esperando su turno?
- **Errores:** ¿está fallando?

Recorrer la tabla completa evita quedarse en el primer recurso que "se ve raro".

Fuente: Brendan Gregg, *The USE Method* (brendangregg.com).

---

## USE con comandos de Linux

| Recurso | Utilización | Saturación | Errores |
|---|---|---|---|
| CPU | `top`, `vmstat` (us, sy) | carga contra `nproc`, `vmstat` (r) | `journalctl -k` |
| Memoria | `free -h` (available) | `vmstat` (si, so) | `journalctl -k` (out of memory) |
| Disco | `df -h`, `df -i` | `vmstat` (b, wa) | `journalctl -k` (I/O error) |
| Red | `ss -s` | colas en `ss -ltn` (Recv-Q) | `ip -s link` |

No hace falta memorizarla: hace falta tenerla escrita.

---

## Anotar mientras diagnosticas

```
## Incidente: la API no contesta
- 10:42  síntoma: curl /health da 502 desde fuera (lo reporta el otro equipo)
- 10:43  systemctl status api-lab: failed, 217/USER
- 10:45  systemctl cat api-lab: drop-in 50-ajuste.conf con User=api-labo
- 10:46  cambio: borrar el drop-in, daemon-reload, restart
- 10:47  verificación: curl /health da 200 desde fuera
```

La bitácora se escribe **durante**, no después. Después ya no recuerdas el orden.

---

<!-- _class: lead -->

# Parte 2
## La lista de cinco minutos

---

## La lista

| # | Comando | Busca |
|---|---|---|
| 1 | `uptime` | Carga contra `nproc`; ¿se reinició? |
| 2 | `journalctl -k -b -p warning` | OOM, errores de disco |
| 3 | `vmstat 1 5` | r, b, si/so, wa |
| 4 | `free -h` | La columna available |
| 5 | `df -h` y `df -i` | Espacio e inodos |
| 6 | `systemctl --failed` | Units caídas |
| 7 | `journalctl -p err -b` | Errores desde el arranque |
| 8 | `sudo ss -ltnp` | ¿Escucha lo que debe? |
| 9 | `ps --sort` o `top` | Quién consume |

Inspirada en "Linux Performance Analysis in 60,000 Milliseconds" (Netflix Tech Blog), adaptada a systemd.

---

## 1. `uptime` y la carga

```
 10:42:13 up 8:00,  1 user,  load average: 0.07, 0.12, 0.07
```

- Tres promedios: último minuto, 5 y 15. ¿Sube o baja?
- La carga cuenta procesos **corriendo o esperando** CPU, y también los atorados en E/S
- Compárenla con `nproc`: carga 4 en 4 núcleos es lleno; en 1 núcleo es una fila de 3
- `up 3 min` cuando nadie reinició: alguien o algo reinició

---

## 2. Lo que dijo el kernel

```bash
sudo journalctl -k -b -p warning     # solo el kernel, solo este arranque
sudo dmesg -T | tail -20             # lo mismo, con hora legible
```

La línea que vieron en la sesión 4 cuando la API pasó su `MemoryMax`:

```
kernel: Memory cgroup out of memory: Killed process 96344 (python3) ... UID:999
```

Errores de disco (`I/O error`), OOM y segfaults aparecen aquí antes que en ningún otro lado.

---

## 3. `vmstat 1 5`

```
procs -----------memory---------- ---swap-- -----io---- -system-- -------cpu-------
 r  b   swpd   free   buff  cache   si   so    bi    bo   in   cs us sy id wa st gu
 1  0   6796 684380 510004 5801820    0    0     0     0 1950 2458  0  0 99  0  0  0
```

| Columna | Si está alta |
|---|---|
| `r` | Más procesos listos que núcleos: falta CPU |
| `b` | Procesos esperando disco |
| `si` / `so` | Está usando swap de verdad: falta memoria |
| `wa` | La CPU espera al disco |

La primera línea es el promedio desde el arranque: lean las siguientes.

---

## 4. `free -h`: mira available

```
               total        used        free      shared  buff/cache   available
Mem:           7.7Gi       1.3Gi       698Mi        54Mi       6.0Gi       6.4Gi
Swap:          1.0Gi       6.6Mi       1.0Gi
```

- `free` bajo es normal: Linux usa la RAM libre como caché de disco
- `available` es lo que de verdad pueden pedir los programas sin swap
- Se preocupan cuando `available` baja **y** `si`/`so` de vmstat suben

---

## 5. Espacio e inodos

```bash
df -h -x tmpfs -x squashfs       # espacio, sin los sistemas en memoria
df -i                            # inodos: cuántos archivos caben
```

Un disco puede tener gigas libres y no aceptar un archivo más: se acabaron los inodos. Pasa con millones de archivos pequeños (cachés, sesiones, colas).

`No space left on device` sale en los dos casos. `df -h` solo ve uno.

---

## 6 y 7. systemd y el journal

```bash
systemctl --failed
sudo journalctl -p err -b --no-pager | tail -20
```

`-p err` incluye err, crit, alert y emerg. `-b` es desde el último arranque.

En el laboratorio, un `WorkingDirectory` mal escrito apareció aquí al primer intento:

```
vm-lab (python3)[4881]: api-lab.service: Changing to the requested working directory failed: No such file or directory
```

---

## 8 y 9. Puertos y procesos

```bash
sudo ss -ltnp                                      # compárenlo con su inventario de la S5
ps -eo pid,user,%cpu,%mem,cmd --sort=-%cpu | head  # quién come CPU
ps -eo pid,user,rss,cmd --sort=-rss | head         # quién come memoria (RSS en KB)
systemctl status <PID>                             # a qué unit pertenece ese proceso
```

`systemctl status` con un PID les dice qué servicio lo levantó. Es el puente entre "un proceso raro" y "un servicio que nadie recuerda".

---

## Demostración: la línea base

Corran la lista completa en su VM **sana** y guarden la salida:

```bash
{ uptime; nproc; free -h; df -h -x tmpfs; df -i /; systemctl --failed; sudo ss -ltnp; } > ~/linea-base.txt
```

Diagnosticar es comparar contra lo normal. Si no saben cómo se ve su VM sana, cualquier número parece sospechoso.

<!-- 10 minutos. La línea base es parte del runbook de la tarea. -->

---

<!-- _class: lead -->

# Parte 3
## CPU, memoria y procesos

---

## Carga alta: ¿CPU o disco?

| Señal | CPU | Disco |
|---|---|---|
| `vmstat` | `r` alto, `us`+`sy` cerca de 100 | `b` alto, `wa` alto |
| `top` | procesos en estado `R` | procesos en estado `D` |
| Qué hacer | `ps --sort=-%cpu`, ¿qué lo dispara? | ¿quién escribe? `iostat -xz 1` (paquete sysstat) |

La carga sube igual en los dos casos. Por eso la carga sola no diagnostica nada.

Fuente: *Practical Linux System Administration* (Hess), cap. 9.

---

## Memoria: el OOM killer

- Cuando no hay memoria, el kernel **mata** un proceso para salvar al resto
- Con `MemoryMax` (S4), el límite es el cgroup de la unit: muere solo esa unit

```bash
sudo journalctl -k -b | grep -i "out of memory"
systemctl show api-lab -p Result -p NRestarts
# Result=oom-kill
```

```
api-lab.service: A process of this unit has been killed by the OOM killer.
api-lab.service: Failed with result 'oom-kill'.
```

El síntoma que ve el usuario es "la API se reinició sola". La causa está en el kernel.

---

## Quién come memoria

```bash
ps -eo pid,user,rss,cmd --sort=-rss | head -5
systemd-cgtop -m                  # por unit de systemd, ordenado por memoria
systemctl show api-lab -p MemoryCurrent -p MemoryMax
```

`systemd-cgtop` agrupa por servicio: si un servicio tiene diez procesos, los ven sumados.

Fuente: *Learning Modern Linux* (Hausenblas), cap. 8.

---

<!-- _class: lead -->

# Parte 4
## Disco lleno

---

## `df` y `du` no cuadran

```bash
df -h /srv/registros
# /dev/loop0   56M   52M     0 100% /srv/registros
du -sh /srv/registros
# 20K   /srv/registros
```

`df` pregunta al sistema de archivos cuántos bloques están ocupados. `du` suma los archivos que **puede ver**.

Si `df` dice lleno y `du` dice casi vacío, hay espacio ocupado por algo que ya no tiene nombre.

---

## Borrado, pero abierto

```bash
sudo lsof +L1 /srv/registros
```

```
COMMAND  PID    USER   FD   TYPE DEVICE SIZE/OFF NLINK NODE NAME
bash    2806 api-lab    3w   REG    7,0 53956608     0   12 /srv/registros/api-lab.log (deleted)
```

- `rm` quita el nombre; el espacio se libera cuando el **último** proceso cierra el archivo
- `NLINK 0`: ya no tiene nombre. `3w`: el proceso lo tiene abierto para escribir
- "Ya borramos el log y sigue lleno" casi siempre es esto

---

## Liberar el espacio

```bash
systemctl status 2806 | head -1         # ¿de qué servicio es?
sudo systemctl restart bitacora-lab     # al cerrar el archivo, el espacio vuelve

# o, sin reiniciar: vaciar el archivo por su descriptor
sudo sh -c ': > /proc/2806/fd/3'
df -h /srv/registros                    # 1%
```

Lo medimos: al vaciar el descriptor, `df` pasó de 100% a 1% al instante. Pero el proceso sigue escribiendo: en 5 segundos ya iba en 4 MB.

Liberar espacio es el primer auxilio. La causa sigue ahí.

---

## El journal también ocupa

```bash
journalctl --disk-usage
sudo journalctl --vacuum-size=200M      # borra lo más viejo hasta quedar en 200 MB
```

Para dejarlo fijo, en `/etc/systemd/journald.conf.d/tamano.conf`:

```ini
[Journal]
SystemMaxUse=500M
```

Y `sudo systemctl restart systemd-journald`.

---

## logrotate, y su trampa

```
/srv/registros/api-lab.log {
    size 10M
    rotate 3
    compress
    missingok
    notifempty
    copytruncate
    su api-lab api-lab
}
```

```bash
sudo logrotate -d /etc/logrotate.d/api-lab-registros   # en seco: dice qué haría
```

`copytruncate` **copia** el log antes de vaciarlo. En un disco ya lleno falla:

```
error: error copying /srv/registros/api-lab.log to /srv/registros/api-lab.log.1: No space left on device
```

Rotar es prevención, no rescate. En Ubuntu lo dispara `logrotate.timer` una vez al día.

---

<!-- _class: lead -->

# Parte 5
## Almacenamiento y fstab

---

## Qué discos hay y dónde están montados

```bash
lsblk -f                  # discos, particiones, sistema de archivos, etiqueta, UUID, punto de montaje
sudo blkid                # UUID y tipo de cada dispositivo
findmnt                   # árbol de montajes, con opciones
findmnt /srv/registros    # ¿qué hay montado aquí?
```

Un directorio vacío y un disco que no montó **se ven igual** con `ls`. `findmnt` los distingue.

Fuente: *Learning Modern Linux* (Hausenblas), cap. 5. *Practical Linux System Administration* (Hess), cap. 7.

---

## Anatomía de una línea de fstab

```
UUID=<UUID_DEL_DISCO>  /mnt/datos  ext4  defaults,noatime,nofail,x-systemd.device-timeout=15s  0  2
```

| Campo | Qué es |
|---|---|
| `UUID=...` | Qué montar. Por UUID, no por `/dev/sdb`: los nombres cambian entre arranques |
| `/mnt/datos` | Dónde |
| `ext4` | Tipo de sistema de archivos |
| `defaults,...` | Opciones |
| `0` | dump: ya nadie lo usa |
| `2` | Orden de `fsck`: 1 la raíz, 2 los demás, 0 nunca |

---

## `nofail`: deseado, no requerido

systemd convierte cada línea del fstab en una unit `.mount`. Lo medimos con su generador:

```
local-fs.target.requires/-.mount
local-fs.target.requires/mnt-fotos.mount       <- sin nofail
local-fs.target.wants/mnt-datos.mount          <- con nofail
```

- **Sin `nofail`:** si el disco no aparece, el arranque se detiene en modo de emergencia
- **Con `nofail`:** el sistema arranca sin él
- `x-systemd.device-timeout=15s`: cuánto esperarlo (por omisión, 90 s)

---

## Bind mounts

```
/mnt/datos/ci-cache  /srv/ci-cache  none  bind,nofail,x-systemd.requires-mounts-for=/mnt/datos  0  0
```

- Un bind mount hace que una carpeta aparezca en otra ruta
- `x-systemd.requires-mounts-for` lo hace depender del disco: si el disco falta, este tampoco monta
- Y **también** lleva `nofail`, o un disco opcional tumbaría el arranque por la puerta de atrás

---

## Comprobar antes de reiniciar

```bash
sudo findmnt --verify
# [W] your fstab has been modified, but systemd still uses the old version;
#     use 'systemctl daemon-reload' to reload
sudo systemctl daemon-reload
sudo mount -a                                   # monta todo lo del fstab; un error sale aquí y no en el arranque
systemctl show -p Requires local-fs.target      # solo deben aparecer los discos del sistema
```

Si un disco opcional aparece en `Requires`, le falta `nofail`. Lo medimos: al quitarle `nofail`, apareció `Requires=mnt-datos.mount`.

Fuente: *systemd for Linux SysAdmins* (Both), cap. 15.

---

## Caso real: el segundo disco del server de staging

- El server de staging del instructor tiene un disco aparte para "la basura" de CI: carpetas de los runners y cachés de paquetes
- Se monta con `nofail,x-systemd.device-timeout=15s`, y las carpetas llegan por bind mounts con `nofail`
- Si ese disco falla, el sistema **arranca igual**. Se probó de verdad: llegó tarde, systemd lo esperó 15 s y siguió

La pregunta incómoda: ¿y qué hay entonces en esas carpetas?

<!-- La regla del dueño del server: el arranque nunca depende de un disco extra. Le pasó en otra máquina que un disco desconectado dejó a Linux esperando en modo de emergencia. -->

---

## Lo que aparece cuando el disco no está

```bash
ls -la /srv/ci-cache
# total 8
# drwxr-xr-x 2 root root 4096 ...  .
# drwxr-xr-x 1 root root 4096 ...  ..
df --output=source,target /srv/ci-cache
# el disco del SISTEMA, no el de datos
```

- La carpeta existe, vacía, **en el disco principal**
- Lo que se escriba ahí llena el disco del sistema
- Por eso las aplicaciones serias revisan que su carpeta sea la buena (un archivo marcador) antes de escribir

---

## Orden no es requisito

En el mismo server, otro disco guarda los datos de una aplicación en Docker:

```
... /mnt/<DISCO>  ext4  defaults,noatime,nofail,x-systemd.device-timeout=15s,x-systemd.before=docker.service  0  2
```

- `x-systemd.before=docker.service` es **orden**: Docker espera a que el disco monte o a que pasen los 15 s
- No se usó `RequiresMountsFor` en Docker: sin ese disco se caería **todo** Docker, staging incluido
- La aplicación se niega sola a arrancar si su carpeta no es la del disco

---

<!-- _class: lead -->

# Parte 6
## Servicios y puertos

---

## Un servicio que no arranca

```bash
systemctl status api-lab
sudo journalctl -u api-lab -b -n 30 --no-pager
systemctl cat api-lab                  # la unit Y sus drop-ins, con la ruta de cada uno
```

`status` da el código de salida; el journal, el mensaje; `cat`, lo que de verdad está configurado.

Fuente: *systemd for Linux SysAdmins* (Both), cap. 15.

---

## Los códigos que medimos

| Lo que se rompió | `status` dice | El journal dice |
|---|---|---|
| `User=` que no existe | `status=217/USER` | solo el código |
| `WorkingDirectory=` que no existe | `status=200/CHDIR` | Changing to the requested working directory failed |
| `ExecStart=` con un binario que no existe | `status=203/EXEC` | solo el código |
| `EnvironmentFile=` que no existe (sin `-`) | `Result: resources` | Failed to load environment files |

Cuando el journal solo da el número, la tabla completa está en `man systemd.exec`, sección de códigos de salida.

---

## El drop-in que nadie recuerda

```bash
systemctl cat api-lab
```

```
# /etc/systemd/system/api-lab.service
...
# /etc/systemd/system/api-lab.service.d/50-ajuste.conf
[Service]
User=api-labo
```

Un drop-in sobrescribe la unit sin tocar el archivo original. `cat` es el único comando que los muestra juntos y en orden.

---

## Puerto ocupado

```
api-lab: OSError: [Errno 98] Address already in use
```

```bash
sudo ss -ltnp 'sport = :8080'
# LISTEN 0 5 127.0.0.1:8080 0.0.0.0:* users:(("python3",pid=2593,fd=3))
ps -o unit= -p 2593                  # o systemctl status 2593
# cache-lab.service
ps -o user,pid,cmd -p 2593
# 64695  2593 /usr/bin/python3 -m http.server 8080 ...
```

Un usuario numérico (`64695`) es un `DynamicUser` de systemd: no busquen ese nombre en `/etc/passwd`.

---

## "Start request repeated too quickly"

```bash
sudo systemctl disable --now cache-lab    # se quitó al impostor
sudo systemctl start api-lab              # y TODAVÍA falla (dentro de la ventana)
sudo journalctl -u api-lab -n 3 -o cat
# api-lab.service: Start request repeated too quickly.
sudo systemctl reset-failed api-lab
sudo systemctl start api-lab              # ahora sí
```

Después de 5 fallos en 2 minutos (`StartLimitBurst` y `StartLimitIntervalSec`, S4), systemd rechaza los arranques **aunque la causa ya no esté**, hasta que pasa la ventana. Lo medimos: dentro de la ventana, rechazado; a los 2 minutos y medio, arrancó. `reset-failed` no espera.

---

<!-- _class: lead -->

# Parte 7
## Leer logs con método

---

## Primero la ventana de tiempo

```bash
sudo journalctl --since "2026-11-09 10:30" --until "2026-11-09 10:50"
sudo journalctl --since "20 min ago"
sudo journalctl -b -1                    # el arranque ANTERIOR: lo que pasó antes del reinicio
```

El síntoma tiene una hora. Leer el log entero es buscar la aguja quitando el pajar a mano.

`-b -1` solo funciona si el journal es persistente (en Ubuntu 24.04 lo es: `/var/log/journal`).

---

## Después prioridad y origen

```bash
sudo journalctl -p warning --since "1 hour ago"   # warning y más grave
sudo journalctl -u api-lab -u nginx --since "1 hour ago"
sudo journalctl -k --since "1 hour ago"           # solo el kernel
sudo tail -n 50 /var/log/nginx/api-lab.error.log  # lo que no va al journal
```

Nginx escribe sus logs en archivos, no en el journal. Sepan dónde escribe cada pieza.

---

## Contar antes de leer

```bash
sudo journalctl -u api-lab --since "1 hour ago" -o cat | sort | uniq -c | sort -rn | head
```

```
      3 OSError: [Errno 98] Address already in use
      1 api-lab.service: Start request repeated too quickly.
```

La técnica de la sesión 1: agrupar y contar convierte mil líneas en cinco.

---

## Correlacionar

Una misma hora, varias fuentes:

```
10:41:58  journal sudo    : <USUARIO> : ... COMMAND=/usr/bin/systemctl daemon-reload
10:42:01  journal api-lab : Main process exited, code=exited, status=217/USER
10:42:03  nginx error.log : connect() failed (111: Connection refused) while connecting to upstream
```

El 502 que vio el usuario, la API que no levantó y el cambio que lo provocó, alineados por la hora.

Fuente: *Practical Linux System Administration* (Hess), cap. 12.

<!-- Las líneas son ilustrativas del formato; en la práctica las van a ver de verdad. El journal registra cada uso de sudo con el comando completo: es la mejor bitácora de "qué se cambió". -->

---

## Plantilla de runbook

```
# Runbook: <síntoma>
## Cómo se ve           qué reporta el usuario y qué comando lo reproduce
## Primeros 5 minutos   la lista, en orden, con lo normal de esta VM (línea base)
## Hipótesis frecuentes cada una con el comando que la confirma o la descarta
## Arreglo              el cambio mínimo, uno a la vez
## Verificación         el mismo comando del síntoma, ahora bien
## Prevención           qué cambiar para que no vuelva (alerta, límite, rotación)
```

La prueba de un runbook: que lo siga alguien que no lo escribió.

---

<!-- _class: lead -->

# Parte 8
## Práctica: tres incidentes sembrados

---

## Reglas del juego

- Parejas de equipos: A siembra en la VM de B y B en la de A
- `sudo bash sembrar.sh disco|servicio|puerto`: uno a la vez
- El script imprime un **ticket**: es lo único que se le dice al otro equipo
- La víctima diagnostica con la lista y anota en su bitácora
- El que sembró observa y **no ayuda**; al final, `sembrar.sh deshacer`

<!-- Tiempo sugerido: 25 minutos por incidente. Si un equipo se atora 15 minutos, el instructor da una sola pista: el número de la lista que deberían mirar. -->

---

## Incidente 1: el disco

Ticket: *"El disco de /srv/registros está al 100%. Ya borramos el log y sigue lleno."*

Ruta esperada:

1. `df -h` contra `du -sh`: no cuadran
2. `lsof +L1`: el archivo borrado sigue abierto
3. `systemctl status <PID>`: de qué servicio es
4. Liberar, y decidir qué hacer con el servicio que escribe sin control

---

## Incidente 2: el servicio

Ticket: *"Después del último cambio, la API ya no levanta. Nadie recuerda qué se cambió."*

Ruta esperada:

1. `systemctl --failed` y `systemctl status api-lab`: el código de salida
2. `journalctl -u api-lab -b`: el mensaje, si lo hay
3. `systemctl cat api-lab`: el drop-in
4. Arreglar, `daemon-reload`, `reset-failed`, `restart`, y `curl` desde fuera

La falla es una de tres al azar: no copien la solución del equipo de al lado.

---

## Incidente 3: el puerto

Ticket: *"La API se cayó y no vuelve, ni siquiera con systemctl start. La página da respuestas raras."*

Ruta esperada:

1. El journal de la API: `Address already in use`
2. `ss -ltnp 'sport = :8080'`: quién lo tiene
3. `systemctl status <PID>`: qué unit es, y por qué arranca antes que la API
4. Quitarlo, `reset-failed`, arrancar, verificar

---

## Verlo fallar: el runbook en manos de otro

Al terminar los tres:

- Cada equipo le da su runbook al otro
- El que sembró elige un incidente y lo siembra **otra vez**
- La víctima lo resuelve **solo con el runbook del otro equipo**

Si el runbook no alcanza, el runbook falló. Anoten en qué paso.

---

## Evidencia

- Bitácora de cada incidente: hora, comando, salida, decisión
- La causa raíz de cada uno, en una línea
- El cambio que hicieron y la verificación (`curl` desde fuera)
- En qué paso se quedó corto el runbook del otro equipo

---

<!-- _class: lead -->

# Parte 9
## Cierre

---

## Al terminar puedes

Encontrar la causa de una caída con un procedimiento que otra persona puede repetir.

- Correr la lista de cinco minutos y saber qué significa cada número
- Explicar por qué `df` y `du` no cuadran, y liberar el espacio
- Escribir un fstab que no detiene el arranque y comprobarlo antes de reiniciar
- Leer el código de salida de un servicio y encontrar el drop-in que lo rompió
- Encontrar quién tiene un puerto y por qué arranca primero

---

## Tarea

**Su runbook de diagnóstico, probado con un incidente que les siembre otro equipo.**

- Lecturas: `dev-notes/10-cloud/04-observabilidad.md` y `dev-notes/10-cloud/21-diagnostico-linux.md`
- Libros: *Learning Modern Linux*, caps. 5 y 8; *Practical Linux System Administration*, caps. 7 a 9 y 12; *systemd for Linux SysAdmins*, cap. 15

Detalle y criterio de aceptación en la tarea de la sesión.

---

## Proyecto final

Dejar un servidor listo para una API:

- Usuario de servicio y unit con límites (S2, S4)
- Timer de respaldo verificado (S3, S4)
- Nginx y firewall (S5)
- Runbook (S6)

**Se califica cuando otro equipo rompe algo a escondidas y ustedes lo encuentran con su runbook.**

---

## Lo que se llevan del curso

- S1: contestar en una línea lo que antes era una hoja de cálculo
- S2: cada cosa corre con el usuario que debe
- S3: scripts que avisan cuando fallan
- S4: servicios que vuelven solos y se pueden auditar
- S5: cada puerto abierto tiene un porqué
- S6: un método que otra persona puede repetir

---

<!-- _class: lead -->

# Gracias
## Quiz de la sesión 6 en la plataforma
