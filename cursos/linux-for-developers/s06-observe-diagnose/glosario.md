# Glosario - S06 - Observar y diagnosticar

> Definiciones de los términos técnicos de la sesión 6.
> Ordenado alfabéticamente.

---

## A

### Archivo borrado pero abierto
**Categoría:** Linux / Sistemas de archivos
**Definición:** Archivo al que ya se le quitó el nombre con `rm` pero que un proceso sigue teniendo abierto. Sus bloques siguen ocupados hasta que el último proceso lo cierre. `lsof +L1` los lista, con cero enlaces y la marca `(deleted)`.
**En el curso:** El incidente del disco: "ya borramos el log y sigue lleno". Se libera cerrando el proceso o vaciando `/proc/<PID>/fd/<N>`.

---

### available (memoria)
**Categoría:** Linux / Memoria
**Definición:** Columna de `free` que estima cuánta memoria pueden pedir los programas sin recurrir a swap, contando la caché que el kernel puede soltar. Es más útil que `free`, que en un sistema sano siempre es bajo.
**En el curso:** La alarma es available bajando junto con `si` y `so` de vmstat subiendo.

---

## B

### Bind mount
**Categoría:** Linux / Almacenamiento
**Definición:** Montaje que hace aparecer una carpeta existente en otra ruta, sin copiarla. En fstab se escribe con tipo `none` y la opción `bind`.
**En el curso:** El caso del server de staging: carpetas de CI que viven en un segundo disco y aparecen en sus rutas de siempre. Si el disco falta, la ruta queda vacía en el disco del sistema.

---

### Bitácora de incidente
**Categoría:** Operación / Método
**Definición:** Registro con hora de cada observación, comando, resultado y decisión durante un diagnóstico. Se escribe mientras se trabaja, no después, porque después ya no se recuerda el orden.
**En el curso:** Es parte de la evidencia de cada incidente sembrado.

---

## C

### Carga promedio (load average)
**Categoría:** Linux / Rendimiento
**Definición:** Promedio de procesos que están corriendo, esperando CPU o esperando E/S, en el último minuto, 5 y 15 minutos. Se compara contra el número de núcleos (`nproc`).
**En el curso:** La carga sola no dice si falta CPU o si el disco está saturado: hace falta `vmstat`.

---

### Código de salida de systemd
**Categoría:** systemd / Diagnóstico
**Definición:** Códigos del 200 en adelante que systemd reporta cuando falla al preparar un proceso antes de ejecutarlo. La tabla completa está en `man systemd.exec`.
**En el curso:** Medimos 217/USER (el usuario no existe), 200/CHDIR (el directorio de trabajo no existe) y 203/EXEC (el ejecutable no existe).

---

### copytruncate
**Categoría:** logrotate
**Definición:** Opción de logrotate que copia el log y después vacía el original, para programas que no saben reabrir su archivo. Necesita espacio libre para la copia.
**En el curso:** Con el disco ya lleno falló con "No space left on device": rotar previene, no rescata.

---

## D

### df y du
**Categoría:** Linux / Almacenamiento
**Definición:** `df` pregunta al sistema de archivos cuántos bloques están ocupados; `du` recorre los archivos visibles y suma sus tamaños. Cuando no cuadran, hay espacio ocupado por algo sin nombre visible.
**En el curso:** 100% en df y 20K en du fue la primera pista del incidente del disco.

---

### Drop-in
**Categoría:** systemd / Configuración
**Definición:** Archivo `.conf` dentro de `<unit>.d/` que agrega o sobrescribe directivas de una unit sin modificar el archivo original. `systemctl cat` muestra la unit junto con todos sus drop-ins.
**En el curso:** El incidente del servicio se sembró con un drop-in `50-ajuste.conf`.

---

### DynamicUser
**Categoría:** systemd / Seguridad
**Definición:** Directiva que hace que systemd cree un usuario temporal, con un UID alto, solo mientras el servicio corre. No aparece en `/etc/passwd`, así que `ps` lo muestra como número.
**En el curso:** El impostor del puerto 8080 corría como `64695`.

---

## F

### fstab
**Categoría:** Linux / Almacenamiento
**Definición:** Archivo `/etc/fstab` con los sistemas de archivos que se montan al arrancar. Cada línea tiene seis campos: qué, dónde, tipo, opciones, dump y orden de fsck. systemd convierte cada línea en una unit `.mount`.
**En el curso:** Se comprueba antes de reiniciar con `findmnt --verify`, `daemon-reload`, `mount -a` y `systemctl show -p Requires local-fs.target`.

---

## I

### Inodo
**Categoría:** Linux / Sistemas de archivos
**Definición:** Estructura que describe un archivo (dueño, permisos, bloques). Cada sistema de archivos tiene un número fijo de inodos: si se acaban, no se pueden crear archivos aunque haya espacio. `df -i` los muestra.
**En el curso:** Por eso la lista de cinco minutos revisa `df -h` y también `df -i`.

---

## L

### Línea base
**Categoría:** Operación / Método
**Definición:** Registro de cómo se ve un sistema sano: carga, memoria, disco, servicios y puertos. Sin ella, cualquier número parece sospechoso.
**En el curso:** `linea-base.txt`, tomada antes de sembrar los incidentes.

---

### logrotate
**Categoría:** Linux / Logs
**Definición:** Herramienta que rota, comprime y borra logs viejos según reglas en `/etc/logrotate.d/`. En Ubuntu 24.04 la dispara `logrotate.timer` una vez al día. `logrotate -d` muestra lo que haría sin hacerlo.
**En el curso:** La prevención del incidente del disco: `size 10M`, `rotate 3`, `compress`.

---

### lsof
**Categoría:** Linux / Herramientas
**Definición:** Lista los archivos abiertos por los procesos, incluidos sockets. `lsof +L1` muestra los archivos con menos de un enlace, es decir, borrados que siguen abiertos.
**En el curso:** Encontró al proceso que retenía el log borrado.

---

## M

### Método USE
**Categoría:** Rendimiento / Método
**Definición:** Método de Brendan Gregg: para cada recurso (CPU, memoria, disco, red) revisar utilización, saturación y errores. Recorrer la tabla completa evita quedarse con el primer recurso que se ve raro.
**En el curso:** La tabla USE con comandos de Linux de la clase.

---

### Modo de emergencia
**Categoría:** systemd / Arranque
**Definición:** Estado al que llega el arranque cuando falta algo requerido, como un disco del fstab sin `nofail`: solo la raíz, una consola y nada de red. Desde ahí no entra SSH.
**En el curso:** La razón de que todo disco que no es del sistema lleve `nofail`.

---

## N

### nofail
**Categoría:** fstab / systemd
**Definición:** Opción de fstab que vuelve un montaje "deseado" en lugar de "requerido": systemd lo pone en `local-fs.target.wants` y el arranque sigue aunque el disco no aparezca. Se acompaña de `x-systemd.device-timeout` para no esperar los 90 s por omisión.
**En el curso:** Lo medimos con el generador del fstab: sin `nofail` la unit cayó en `local-fs.target.requires`.

---

## O

### OOM killer
**Categoría:** Linux / Memoria
**Definición:** Mecanismo del kernel que mata un proceso cuando no queda memoria. Con un límite de cgroup (como `MemoryMax`), actúa solo dentro de ese grupo. Deja una línea "out of memory" en el log del kernel.
**En el curso:** `Result=oom-kill` en la API de la sesión 4 y su línea en `journalctl -k`.

---

## R

### reset-failed
**Categoría:** systemd / Operación
**Definición:** `systemctl reset-failed <unit>` borra el estado de falla y el contador de arranques de una unit. Hace falta cuando la unit alcanzó su `StartLimitBurst` y systemd rechaza arrancarla dentro de la ventana.
**En el curso:** Sin él, la API siguió rechazada aunque el puerto ya estaba libre.

---

### Runbook
**Categoría:** Operación / Documentación
**Definición:** Procedimiento escrito para atender un síntoma: cómo se ve, qué medir primero, hipótesis con su comando, arreglo, verificación y prevención. Está bien escrito cuando lo puede seguir alguien que no lo escribió.
**En el curso:** La tarea de la sesión y la pieza que se califica en el proyecto final.

---

## S

### Start request repeated too quickly
**Categoría:** systemd / Diagnóstico
**Definición:** Mensaje del journal cuando una unit falló más veces que `StartLimitBurst` dentro de `StartLimitIntervalSec`. systemd deja de arrancarla hasta que pasa la ventana o hasta un `reset-failed`.
**En el curso:** Apareció en el incidente del puerto, aun después de quitar al impostor.

---

## U

### UUID (sistema de archivos)
**Categoría:** Linux / Almacenamiento
**Definición:** Identificador único que se asigna a un sistema de archivos al crearlo. Se usa en fstab en lugar de `/dev/sdb1` porque los nombres de dispositivo pueden cambiar entre arranques. `blkid` y `lsblk -f` lo muestran.
**En el curso:** La primera columna de cada línea de fstab de la clase.

---

## V

### vmstat
**Categoría:** Linux / Rendimiento
**Definición:** Muestra cada intervalo los procesos listos (`r`) y bloqueados (`b`), memoria, swap (`si`, `so`), E/S y reparto de CPU (`us`, `sy`, `id`, `wa`). La primera línea es el promedio desde el arranque.
**En el curso:** El paso 3 de la lista de cinco minutos: distingue falta de CPU de espera de disco.

---

## X

### x-systemd.before
**Categoría:** fstab / systemd
**Definición:** Opción de fstab que ordena el montaje antes de una unit, sin hacerla depender de él. Es orden, no requisito: si el disco falta, la otra unit arranca igual tras el tiempo de espera.
**En el curso:** El caso real: un disco de datos ordenado antes de Docker sin `RequiresMountsFor`, para que su falta no tumbe todo Docker.
