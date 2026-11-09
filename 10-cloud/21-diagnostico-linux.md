# 21 - Diagnóstico en Linux

> Fuente: *Learning Modern Linux* (Hausenblas, O'Reilly, 2022): caps. 5 Filesystems y 8 Observability; *Practical Linux System Administration* (Hess, O'Reilly, 2023): caps. 7 Managing Storage, 8 Maintaining System Health, 9 Monitoring Your System y 12 Troubleshooting Linux; *systemd for Linux SysAdmins* (Both, Apress): cap. 15 Analyzing systemd Startup and Configuration; Brendan Gregg, *The USE Method* y *Linux Performance Analysis in 60,000 Milliseconds* (Netflix Tech Blog, 2015)

## Problema que resuelve

Cuando algo se cae, la tentación es probar cosas: reiniciar el servicio, reiniciar el servidor, borrar logs, subir la memoria. A veces funciona y nadie aprende por qué; a veces destruye la evidencia. Esta nota da un método y una lista corta de comandos que **cualquiera del equipo puede repetir en el mismo orden**, con lo que hay que mirar en cada salida, y los tres incidentes que más se repiten en un servidor de aplicación: disco lleno, servicio que no arranca y puerto ocupado.

La parte de logs, métricas y alertas de la aplicación vive en [04-observabilidad.md](04-observabilidad.md). Aquí se trata del sistema operativo debajo de ella. Los ejemplos usan `api-lab`, la API de laboratorio del curso Linux para desarrolladores: sustitúyela por tu servicio.

---

## Diagnosticar es un método

1. **Síntoma, con hora.** "La API da 502 desde las 14:05", no "está lenta".
2. **Mirar antes de tocar.** La lista de cinco minutos va antes de cualquier reinicio: un reinicio borra procesos, conexiones y a veces logs.
3. **Hipótesis que se puede refutar.** "Se llenó el disco" se comprueba con `df` en un segundo.
4. **Un cambio a la vez**, y anotado. Dos cambios juntos no dicen cuál arregló.
5. **Verificar con el síntoma original.** Si el síntoma era "502 desde fuera", se verifica con `curl` desde fuera, no con `systemctl is-active`.
6. **Escribir lo que pasó**: causa, evidencia, arreglo y cómo se detecta la próxima vez.

El **método USE** de Brendan Gregg ordena la búsqueda: para cada recurso (CPU, memoria, disco, red) se pregunta por su **U**tilización (qué tan ocupado está), su **S**aturación (cuánto trabajo espera en cola) y sus **E**rrores. Un recurso al 100% sin cola puede estar bien; uno con cola creciendo es el cuello de botella.

---

## La lista de cinco minutos

Adaptada de la lista de 60 segundos de Gregg para un servidor con systemd. Se corre **en este orden** y se copia la salida al reporte.

| # | Comando | Qué buscar |
|---|---------|------------|
| 1 | `uptime` | ¿Reinició hace poco? Carga promedio (1, 5 y 15 min) contra `nproc` |
| 2 | `journalctl -k -b --no-pager \| tail -20` | El kernel: OOM killer, errores de disco (`I/O error`), red |
| 3 | `systemctl --failed` | Lo que está caído según systemd |
| 4 | `journalctl -p err -b --no-pager \| tail -30` | Errores de todo el sistema desde el arranque |
| 5 | `df -h` y `df -i` | Algún sistema de archivos arriba de 90%, en bloques o en inodos |
| 6 | `free -h` | La columna `available`, no `free`; swap en uso |
| 7 | `vmstat 1 5` | `r` (cola de CPU) mayor que `nproc`, `wa` alto (espera de disco), `si`/`so` distintos de 0 (swap activo) |
| 8 | `ps -eo pid,user,%cpu,%mem,rss,comm --sort=-%cpu \| head` | Quién consume CPU; cambia a `--sort=-rss` para memoria |
| 9 | `ss -ltnp` | Qué escucha, en qué dirección y con qué proceso |
| 10 | `systemctl status <servicio>` y `journalctl -u <servicio> --since "-30 min"` | El servicio del síntoma, en la ventana del síntoma |

```bash
# Las 10 en un archivo con fecha, para adjuntar al reporte
{
  date; uptime; nproc
  journalctl -k -b --no-pager | tail -20
  systemctl --failed --no-pager
  journalctl -p err -b --no-pager | tail -30
  df -h; df -i
  free -h
  vmstat 1 5
  ps -eo pid,user,%cpu,%mem,rss,comm --sort=-%cpu | head
  ss -ltnp
} > "diagnostico-$(date +%F-%H%M).txt" 2>&1
```

Para la vista en vivo: `top` (o `htop`), y dentro de `top` la tecla `M` ordena por memoria y `P` por CPU.

---

## CPU y procesos

```bash
uptime
#  14:11:15 up 8 days,  2:01,  1 user,  load average: 3.92, 2.10, 1.05
nproc
# 2
```

La **carga** cuenta los procesos que están corriendo o esperando CPU, más los que esperan disco sin poder interrumpirse (estado `D`). Carga 3.9 en 2 núcleos: hay cola. Que el número de 1 minuto sea mayor que el de 15 dice que el problema **empezó hace poco**.

| Señal | Comando | Lectura |
|-------|---------|---------|
| Cola de CPU | `vmstat 1` columna `r` | Más procesos listos que núcleos |
| Espera de disco | `vmstat 1` columna `wa`; procesos en estado `D` en `ps` | La CPU está libre pero esperando I/O |
| Un proceso desbocado | `ps --sort=-%cpu` | Su PID, su usuario y su unit: `systemctl status <PID>` dice a qué unit pertenece |
| Límite de la unit | `systemctl show api -p CPUQuotaPerSecUSec` | Una API con `CPUQuota=50%` no pasa de medio núcleo aunque la máquina esté libre |

---

## Memoria

```bash
free -h
#                total        used        free      shared  buff/cache   available
# Mem:           7.7Gi       1.3Gi       681Mi        54Mi       6.0Gi       6.4Gi
```

`free` bajo es normal: Linux usa la memoria libre como caché de disco y la suelta cuando alguien la pide. La que importa es **`available`**. Si `available` es poca y `vmstat` muestra `si`/`so` distintos de 0, el sistema está viviendo en swap y todo va lento.

Cuando se acaba, el **OOM killer** del kernel mata un proceso. Siempre deja firma:

```bash
journalctl -k | grep -i 'out of memory'
# kernel: Memory cgroup out of memory: Killed process 96344 (python3) total-vm:355700kB, anon-rss:204228kB ...
journalctl -u api | grep -i oom
# api.service: A process of this unit has been killed by the OOM killer.
```

"Memory cgroup out of memory" significa que el proceso pasó el `MemoryMax` de su unit (ver [20-systemd.md](20-systemd.md)), no que el servidor se quedó sin RAM. Son dos arreglos distintos: subir el límite o encontrar la fuga.

---

## Disco lleno

Síntomas: escrituras que fallan con `No space left on device`, una base que no arranca, un servicio que se cae sin razón aparente.

```bash
df -h                                   # qué sistema de archivos está lleno
df -i                                   # ¿inodos? miles de archivos chicos llenan los inodos con espacio libre
sudo du -xh --max-depth=1 /var 2>/dev/null | sort -h | tail   # -x: sin cruzar a otros discos
sudo find /var/log -type f -size +100M -exec ls -lh {} +
journalctl --disk-usage
```

**El archivo borrado que sigue ocupando.** Si alguien borra un log que un proceso tiene abierto, el nombre desaparece pero el espacio no se libera hasta que el proceso lo cierre. `du` ya no lo ve; `df` sigue lleno.

```bash
df -h /var/log          # 78% usado
sudo rm /var/log/app/app.log
df -h /var/log          # sigue en 78%: alguien lo tiene abierto
sudo lsof -a +L1 /var/log            # archivos con 0 enlaces (borrados) que siguen abiertos en ese sistema de archivos
# COMMAND  PID USER   FD   TYPE DEVICE SIZE/OFF NLINK NODE NAME
# python3 1928 root    3w   REG    7,0 41943040     0   12 /var/log/app/app.log (deleted)

# Arreglo 1: reiniciar el proceso (cierra el archivo)
# Arreglo 2, sin reiniciar: vaciarlo por su descriptor
sudo sh -c ': > /proc/1928/fd/3'
```

Sin `-a`, `lsof +L1 /var/log` combina las condiciones con "o" y lista todos los borrados del sistema (incluidos los `memfd` de .NET, que no son archivos en disco).

Lo que se borra con seguridad, y lo que no:

| Seguro | Con cuidado | Nunca a mano |
|--------|-------------|--------------|
| `sudo journalctl --vacuum-size=500M` | Logs rotados viejos (`*.gz`, `*.1`) de `/var/log` | Archivos de `/var/lib/postgresql`, `/var/lib/docker` |
| `sudo apt clean` | `docker system prune` (borra imágenes y contenedores detenidos) | Un log abierto: vacíalo, no lo borres |

El arreglo de fondo es que no vuelva a pasar: `logrotate` para los logs en texto (`/etc/logrotate.d/`), `SystemMaxUse=` en `/etc/systemd/journald.conf` para el journal, y una alerta de disco al 80%.

---

## Almacenamiento y fstab

```bash
lsblk -f                       # discos, particiones, sistema de archivos, etiqueta, UUID y dónde está montado
sudo blkid                     # UUID y tipo de cada partición
findmnt                        # árbol de montajes actual; findmnt /mnt/datos para uno solo
findmnt --verify               # revisa /etc/fstab: sintaxis, que exista el origen y el punto de montaje
```

Cada línea de `/etc/fstab` tiene seis campos:

```
# <origen>                                  <punto>       <tipo> <opciones>                                            <dump> <pass>
UUID=0a1b2c3d-aaaa-4bbb-8ccc-ddddeeeeffff   /mnt/datos    ext4   defaults,noatime,nofail,x-systemd.device-timeout=15s  0      2
/mnt/datos/cache                            /srv/cache    none   bind,nofail,x-systemd.requires-mounts-for=/mnt/datos  0      0
```

| Campo u opción | Por qué |
|----------------|---------|
| `UUID=` en lugar de `/dev/sdb1` | Los nombres `/dev/sdX` cambian según el orden en que el kernel detecta los discos; el UUID no |
| `nofail` | El disco pasa de **requisito** del arranque a **deseado**: si falta, el sistema arranca igual |
| `x-systemd.device-timeout=15s` | Cuánto espera al disco antes de seguir sin él (por omisión, 90 s) |
| `bind` | Monta una carpeta en otra ruta: la carpeta vive en el disco grande y el programa la ve donde la espera |
| `x-systemd.requires-mounts-for=` | El bind mount espera al disco del que sale |
| `pass` 2 | `fsck` lo revisa al arrancar, después de la raíz (1); 0 para bind mounts |

systemd convierte cada línea en una unit `.mount` en el arranque. La diferencia entre tener o no `nofail` se ve en lo que genera (comprobado con `systemd-fstab-generator`):

```
local-fs.target.requires/-.mount            # la raíz: sin ella no hay arranque
local-fs.target.requires/mnt-fotos.mount    # una línea SIN nofail: si el disco falta, el arranque se detiene
local-fs.target.wants/mnt-datos.mount       # con nofail: se intenta, y si falla se sigue
local-fs.target.wants/srv-cache.mount
```

**Antes de reiniciar después de tocar el fstab**, siempre:

```bash
sudo findmnt --verify
sudo systemctl daemon-reload
sudo mount -a                                  # monta todo lo que falte; un error aquí es un error en el arranque
systemctl show -p Requires local-fs.target     # solo deben aparecer los montajes del sistema
# Requires=-.mount boot.mount boot-efi.mount   (cualquier disco de datos aquí es un nofail que falta)
```

Un fstab roto sin `nofail` deja la máquina en el modo de emergencia esperando un disco, y en una VM en la nube eso significa entrar por la consola serial del proveedor.

---

## Caso real: el segundo disco con nofail

En el server de staging del autor, un segundo disco guarda "la basura" de CI: las carpetas de trabajo de los runners y las cachés de paquetes. Si muere, se pierde tiempo de build, no datos. Está montado con `nofail,x-systemd.device-timeout=15s`, y las carpetas que usan los runners llegan a él por bind mounts con `nofail,x-systemd.requires-mounts-for=`.

Se probó de verdad: en un arranque el disco llegó tarde, systemd esperó el tiempo configurado, siguió sin él, y todo lo demás levantó; los runners se quedaron sin su disco hasta que se montó a mano. Lo que enseña:

- **`nofail` cambia una caída por un síntoma silencioso.** Sin el disco, los puntos de montaje son **carpetas vacías en el disco principal**. Nada truena: un programa que arranque en ese estado encuentra su carpeta vacía y escribe ahí, en el disco del sistema, hasta llenarlo.
- Por eso cada aplicación que vive en un disco opcional necesita **comprobar que su carpeta es la buena** antes de escribir (un archivo marcador que solo existe en el disco real), o una dependencia explícita.
- **Orden no es requisito.** En otro disco del mismo server se usó `x-systemd.before=docker.service`: Docker espera a que ese disco monte (o a que pasen los 15 s) antes de arrancar. No se usó `RequiresMountsFor=` en `docker.service`, porque haría que la falta de un disco tumbara **todo Docker**, staging incluido.
- Después de cada cambio se comprueba `systemctl show -p Requires local-fs.target`: si aparece un disco de datos, le falta `nofail`.

---

## Puertos y red

```bash
ss -ltnp                                  # TCP en escucha, con proceso (sudo para ver los de otros usuarios)
ss -ltnp 'sport = :8080'                  # solo un puerto
ss -tn state established '( sport = :443 )' | wc -l   # conexiones activas al 443
curl -sv http://127.0.0.1:8080/health     # probar desde dentro
```

**Puerto ocupado.** El síntoma es un servicio que no arranca con un error que menciona la dirección:

```bash
journalctl -u api-lab -n 20 -o cat
# OSError: [Errno 98] Address already in use          (Python; en .NET: "address already in use")
# api-lab.service: Main process exited, code=exited, status=1/FAILURE
sudo ss -ltnp 'sport = :8080'
# LISTEN 0  5  127.0.0.1:8080  0.0.0.0:*  users:(("python3",pid=2082,fd=3))
systemctl status 2082                     # ¿a qué unit pertenece ese PID? (si es de una)
```

Se arregla deteniendo al intruso **por su unit o por su PID** (`kill 2082`), nunca con `pkill -f` o `killall`, que matan por patrón y se llevan procesos ajenos.

Escuchar, estar permitido y ser alcanzable son tres preguntas distintas: `ss` contesta la primera, `ufw status` la segunda y solo un `curl` desde otra máquina la tercera.

---

## Un servicio que no arranca

```bash
systemctl status api-lab                   # estado, código y las últimas líneas
journalctl -u api-lab -b -n 50 -o cat      # todo lo de este arranque
journalctl -xeu api-lab                    # el final, con explicaciones de systemd
sudo systemd-analyze verify /etc/systemd/system/api-lab.service
```

El código `status=` dice en qué paso falló. Los que salen con más frecuencia (medidos con units rotas a propósito):

| Código | Causa | Línea del journal |
|--------|-------|-------------------|
| `217/USER` | El `User=` no existe | `status=217/USER` |
| `203/EXEC` | El `ExecStart=` no existe o no tiene permiso de ejecución | `Failed to execute /ruta: Permission denied` |
| `200/CHDIR` | El `WorkingDirectory=` no existe | `Changing to the requested working directory failed` |
| resultado `resources` | Falta el `EnvironmentFile=` (sin `-` delante) | `Failed to load environment files: No such file or directory` |
| `1/FAILURE` | El programa arrancó y salió con error: el porqué está en sus propias líneas | `Address already in use`, cadena de conexión mala, etc. |
| resultado `oom-kill` | Pasó su `MemoryMax` | `killed by the OOM killer` |

Si truena muchas veces seguidas, systemd deja de reintentar (`StartLimitBurst=`) y lo marca así:

```
api-lab.service: Start request repeated too quickly.
```

Después de arreglar la causa: `sudo systemctl reset-failed api-lab && sudo systemctl start api-lab`. El bloqueo dura lo que la ventana `StartLimitIntervalSec=` (pasada la ventana, `start` vuelve a funcionar solo), pero `reset-failed` no obliga a esperar y limpia el estado de falla. Para un arranque lento del servidor completo: `systemd-analyze blame` y `systemd-analyze critical-chain`.

---

## Leer logs con método

1. **Ventana de tiempo primero.** `journalctl --since "2026-11-07 14:00" --until "2026-11-07 14:15"`: el síntoma tiene hora, úsala.
2. **Por prioridad.** `-p warning` quita el ruido de info.
3. **Por fuente.** `-u nginx`, `-u api-lab`, `-k` (kernel), `-t respaldo`.
4. **Contar antes de leer.** ¿Cuántas veces sale el error? ¿Desde cuándo?
   ```bash
   journalctl -u api-lab --since today -o cat | grep -c 'Errno 98'
   awk '$9 ~ /^5/ {print substr($4, 2, 14)}' /var/log/nginx/api-lab.access.log | sort | uniq -c   # 5xx por hora
   ```
5. **La primera aparición, no la última.** El primer error suele ser la causa; los siguientes, consecuencias.
6. **Correlacionar por hora** entre capas: el 502 de Nginx a las 14:05:12, el OOM del kernel a las 14:05:11.

`grep -B5 -A5` muestra contexto alrededor de una coincidencia; en el journal, `journalctl ... | less` y `/patrón` para buscar.

---

## Plantilla de runbook

Un runbook es la lista que alguien sin contexto puede seguir a las 3 de la mañana. Se escribe en el repo, junto al código, y se prueba sembrando incidentes.

```markdown
# Runbook - {{PROJECT_NAME}} en la VM

## Datos
- Servicio: `{{PROJECT_SLUG}}.service`, usuario `{{PROJECT_SLUG}}`, escucha en 127.0.0.1:5000
- Nginx: sitio `/etc/nginx/sites-available/{{PROJECT_SLUG}}`, logs en `/var/log/nginx/`
- Respaldos: timer `respaldo-{{PROJECT_SLUG}}.timer`, destino `/var/backups/{{PROJECT_SLUG}}`
- Discos: `/` del sistema; `/mnt/datos` con nofail

## 1. Síntoma
Qué se ve, desde dónde y desde qué hora.

## 2. Lista de cinco minutos
(los 10 comandos, en orden, con qué buscar en cada uno)

## 3. Árbol de decisión
- `curl` desde fuera da 502: Nginx vive y la API no. Ir a 4
- `systemctl status` en failed: leer el `status=` en la tabla de códigos
- `df` arriba de 90%: sección disco lleno (incluye `lsof -a +L1`)
- `Address already in use`: `ss -ltnp 'sport = :5000'`

## 4. Arreglos conocidos
Cada uno con el comando exacto y cómo se verifica.

## 5. Escalar
A quién, con qué evidencia (el archivo de diagnóstico de la lista).
```

---

## Glosario

| Término | Definición |
|---------|-----------|
| Método USE | Revisar de cada recurso su utilización, su saturación y sus errores |
| Carga promedio | Promedio de procesos corriendo, esperando CPU o esperando disco sin interrupción, a 1, 5 y 15 minutos |
| Estado D | Proceso esperando I/O sin poder interrumpirse; cuenta en la carga aunque no use CPU |
| `available` | Memoria que el sistema puede dar sin recurrir a swap, contando la caché que puede soltar |
| Swap | Espacio en disco que el kernel usa como memoria cuando falta RAM; mucho más lento |
| OOM killer | Mecanismo del kernel que mata procesos cuando se acaba la memoria del sistema o la de un cgroup |
| Inodo | Estructura que describe un archivo; un sistema de archivos puede quedarse sin inodos con espacio libre |
| Archivo borrado abierto | Archivo sin nombre (0 enlaces) que un proceso sigue usando; su espacio no se libera hasta cerrarlo |
| `lsof` | Lista los archivos abiertos por los procesos; `+L1` muestra los borrados que siguen abiertos |
| fstab | Archivo `/etc/fstab` con los sistemas de archivos que se montan al arrancar |
| UUID | Identificador único de un sistema de archivos; no cambia aunque cambie el orden de los discos |
| `nofail` | Opción de fstab que hace opcional un montaje en el arranque |
| Bind mount | Montaje que hace visible una carpeta en otra ruta |
| Modo de emergencia | Estado al que cae systemd cuando falta un montaje requerido; solo hay consola de root |
| Runbook | Procedimiento escrito para diagnosticar y resolver incidentes conocidos, que otra persona puede repetir |
| `reset-failed` | Orden de systemctl que borra el estado de falla de una unit y su contador de reintentos |

---

*Rogelio Arriaga Gonzalez*
