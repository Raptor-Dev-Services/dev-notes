# 18 - La terminal eficiente

> Fuente: *Efficient Linux at the Command Line* (Barrett, O'Reilly, 2022): caps. 1 a 4; *Learning Modern Linux* (Hausenblas, O'Reilly, 2022): cap. 3 Shells and Scripting

## Problema que resuelve

En un servidor no hay hoja de cálculo ni IDE: hay una terminal y archivos de texto. Quien copia comandos sin saber qué hace el shell con ellos se topa con resultados que parecen correctos y no lo son (un conteo que no suma, un archivo con espacios que "no existe", un error que no llegó al log). Esta nota cubre lo que pasa entre que presionas Enter y que el programa arranca, cómo se encadenan las herramientas de texto y cómo contestar en una línea las preguntas típicas sobre un log.

Complementa la sección de comandos esenciales de [03-linux-nginx.md](03-linux-nginx.md).

---

## Lo que hace el shell antes de correr un comando

El programa nunca ve lo que escribiste: ve lo que el shell dejó después de expandir. El orden en Bash es:

| Paso | Qué hace | Ejemplo |
|------|----------|---------|
| 1. Llaves | Genera palabras | `img{1,2}.png` da `img1.png img2.png` |
| 2. Tilde | `~` pasa a tu home | `~/notas` da `/home/ana/notas` |
| 3. Parámetros y variables | `$VAR`, `${VAR:-def}` | `$HOME` |
| 4. Sustitución de comandos | Corre y pega la salida | `$(date +%F)` |
| 5. Aritmética | Calcula enteros | `$((60 * 24))` |
| 6. Separación de palabras | Parte en espacios lo que NO iba entre comillas | `$f` con espacios da dos argumentos |
| 7. Comodines (globbing) | `*`, `?`, `[...]` contra nombres de archivo | `*.log` |
| 8. Quitar comillas | Las comillas ya cumplieron y se van | |

Después aplica las redirecciones y busca el programa: primero alias, luego función, luego comando interno (builtin) y al final las carpetas de `$PATH`, en orden.

```bash
# ¿Qué se va a ejecutar de verdad cuando escribo ls?
type -a ls
# ls is aliased to `ls --color=auto'
# ls is /usr/bin/ls

# Ver la expansión antes de que el programa la reciba
set -x                 # el shell imprime cada comando ya expandido, con un + delante
echo ~ $HOME/{uno,dos} $((2*3)) $(date +%Y)
# + echo /root /root/uno /root/dos 6 2026
set +x

# Un comodín que no encuentra nada se queda tal cual (Bash no lo borra)
echo *.txt             # imprime *.txt si no hay ningún .txt
```

La separación de palabras es la fuente número uno de sorpresas:

```bash
f="con espacio.log"
ls $f        # ls recibe DOS argumentos: "con" y "espacio.log"
# ls: cannot access 'con': No such file or directory
ls "$f"      # ls recibe uno solo
# con espacio.log
```

| Comillas | Qué expande | Úsalas para |
|----------|-------------|-------------|
| `"dobles"` | Variables y `$(...)`, pero no separa ni aplica comodines | Casi siempre: `"$archivo"`, `"$(pwd)"` |
| `'simples'` | Nada: el texto va literal | Programas de awk y sed, patrones con `$` |
| Sin comillas | Todo, incluida la separación y los comodines | Solo cuando quieres que el comodín se expanda |

---

## Entrada, salida y redirecciones

Todo proceso nace con tres canales: 0 entrada estándar (stdin), 1 salida estándar (stdout) y 2 salida de error (stderr). Las tuberías conectan el 1 de un proceso con el 0 del siguiente. **El 2 no viaja por la tubería**: por eso los errores aparecen en la pantalla aunque redirijas la salida.

```bash
comando > salida.txt          # stdout a un archivo (lo trunca)
comando >> salida.txt         # stdout al final del archivo
comando 2> errores.txt        # solo stderr
comando > todo.txt 2>&1       # stdout al archivo, y stderr a donde ya apunta stdout
comando &> todo.txt           # lo mismo, forma corta de Bash
comando 2>/dev/null           # tirar los errores (solo si sabes cuáles tiras)
comando < entrada.txt         # stdin desde un archivo
comando | tee copia.txt | wc -l   # tee duplica: escribe el archivo Y sigue la tubería
```

El orden de las redirecciones importa, porque se aplican de izquierda a derecha:

```bash
ls /noexiste > o1.txt 2>&1    # primero stdout al archivo, luego stderr a donde va stdout: el error queda en o1.txt
ls /noexiste 2>&1 > o2.txt    # primero stderr a la pantalla (donde iba stdout), luego stdout al archivo
# el error sale en la terminal y o2.txt queda vacío
```

---

## Tuberías: las herramientas que se encadenan

Cada herramienta hace una cosa con líneas de texto. El poder está en combinarlas.

| Herramienta | Qué hace | Ejemplo |
|-------------|----------|---------|
| `wc -l` | Cuenta líneas | `grep -c` hace lo mismo para coincidencias |
| `head -n 5`, `tail -n 5` | Primeras o últimas líneas | `tail -f` sigue un archivo que crece |
| `cut -d: -f1` | Corta columnas por delimitador | `cut -d: -f1 /etc/passwd` |
| `grep` | Filtra líneas | `-v` invierte, `-i` sin mayúsculas, `-E` regex extendida, `-o` solo lo que coincide |
| `sort` | Ordena | `-n` numérico, `-r` al revés, `-h` tamaños (`2K`, `1G`), `-k2` por columna, `-u` sin repetidos |
| `uniq -c` | Cuenta repetidos **consecutivos** | Siempre después de `sort` |
| `awk` | Columnas, filtros y cuentas | `awk '$9 >= 500 {print $1}'` |
| `sed` | Reemplaza o recorta | `sed 's/?.*//'` quita la query string |
| `tr` | Cambia o borra caracteres | `tr -s ' '` junta espacios repetidos |
| `xargs` | Convierte líneas en argumentos | `find ... -print0 \| xargs -0 rm` |
| `find` | Busca archivos por nombre, edad, tamaño | `find /var/log -name '*.gz' -mtime +30` |

El patrón que más se repite en la vida real es **contar y ordenar**:

```bash
# ¿qué valores aparecen y cuántas veces?, del más frecuente al menos
... | sort | uniq -c | sort -rn | head
```

```bash
# Los 5 directorios más pesados bajo /var, en tamaños legibles
sudo du -xh --max-depth=1 /var 2>/dev/null | sort -h | tail -5

# Los usuarios con shell de login (los que pueden entrar)
grep -v -E 'nologin|false$' /etc/passwd | cut -d: -f1,7

# Archivos con espacios en el nombre: -print0 y -0 usan el byte nulo como separador
find . -name '*.log' -print0 | xargs -0 ls -l
```

---

## Preguntas a un log de Nginx en una línea

Un log de acceso con el formato `combined` de Nginx más el tiempo de respuesta al final (`$request_time`) se ve así:

```
192.0.2.28 - - [07/Nov/2026:14:03:11 +0000] "GET /api/pedidos HTTP/1.1" 502 1834 "-" "curl/8.5.0" 0.212
```

Separado por espacios, awk ve: `$1` la IP, `$4` la fecha con su corchete, `$7` la ruta, `$9` el estado y `$NF` (el último campo) el tiempo. El agente de usuario trae espacios, así que el tiempo no tiene número fijo: por eso `$NF`.

```bash
LOG=/var/log/nginx/{{PROJECT_SLUG}}.access.log

# Las 10 IP que más piden
awk '{print $1}' "$LOG" | sort | uniq -c | sort -rn | head -10

# Los 5xx por hora: substr toma "07/Nov/2026:14" de "[07/Nov/2026:14:03:11"
awk '$9 ~ /^5/ {print substr($4, 2, 14)}' "$LOG" | sort | uniq -c

# La petición más lenta (una sola)
awk '{print $NF, $7}' "$LOG" | sort -rn | head -1

# La ruta más lenta EN PROMEDIO, sin la query string, con cuántas peticiones tuvo
awk '{ruta = $7; sub(/\?.*/, "", ruta); suma[ruta] += $NF; n[ruta]++}
     END {for (r in suma) printf "%.3f %5d %s\n", suma[r] / n[r], n[r], r}' "$LOG" | sort -rn | head -5

# Qué estados hay y cuántos de cada uno (aquí aparecen las sorpresas)
awk '{print $9}' "$LOG" | sort | uniq -c | sort -rn
```

Para tener el tiempo de respuesta en el log, se declara un formato propio en Nginx:

```nginx
# /etc/nginx/conf.d/tiempos.conf
log_format tiempos '$remote_addr - $remote_user [$time_local] "$request" '
                   '$status $body_bytes_sent "$http_referer" "$http_user_agent" $request_time';
```

y en el `server` del sitio: `access_log /var/log/nginx/{{PROJECT_SLUG}}.access.log tiempos;`.

---

## Historial: no volver a escribir

Bash guarda lo que escribes en `~/.bash_history` al cerrar la sesión. Ubuntu ya trae en `~/.bashrc` `HISTCONTROL=ignoreboth` (no guarda duplicados seguidos ni líneas que empiezan con espacio), `HISTSIZE=1000` y `shopt -s histappend`.

| Atajo | Qué hace |
|-------|----------|
| `Ctrl-R` | Busca hacia atrás en el historial mientras escribes; `Ctrl-R` otra vez da la anterior |
| `!!` | El comando anterior completo: `sudo !!` cuando olvidaste el sudo |
| `!$` | El último argumento del comando anterior: después de `mkdir -p /srv/x`, `cd !$` (en la línea siguiente, no en la misma) |
| `^viejo^nuevo` | Repite el anterior cambiando un texto |
| `history \| grep nginx` | Buscar con herramientas |
| `fc` | Abre el comando anterior en el editor para corregirlo |

```bash
# ~/.bashrc: historial con fecha, más grande, y escrito al momento (sirve para saber QUÉ se corrió y CUÁNDO)
export HISTTIMEFORMAT='%F %T '
export HISTSIZE=10000
export HISTFILESIZE=20000
PROMPT_COMMAND='history -a'

# Un comando con secretos: empezar con espacio para que no quede en el historial (requiere ignorespace o ignoreboth)
 export TOKEN_TEMPORAL=...
```

`Alt-.` pega el último argumento del comando anterior y, presionado otra vez, el del anterior a ese.

---

## Moverse por el sistema de archivos

| Comando | Qué hace |
|---------|----------|
| `cd -` | Regresa al directorio anterior (alterna entre dos) |
| `cd` | A tu home |
| `pushd /etc/nginx` / `popd` | Pila de directorios: entra y luego regresa |
| `CDPATH=.:~:/srv` | `cd proyecto` busca también en `~` y `/srv` |
| `ls -ltr` | Lo más reciente al final: lo que acaba de cambiar |
| `ls -lhS` | Por tamaño, en unidades legibles |
| `find /etc -newer /tmp/marca` | Lo que cambió después de crear `/tmp/marca` (útil antes y después de instalar algo) |
| `realpath archivo` | La ruta absoluta, resolviendo enlaces |

Los lugares que un desarrollador visita en un servidor:

| Ruta | Qué hay |
|------|---------|
| `/etc` | Configuración (Nginx, systemd, SSH, fstab) |
| `/var/log` | Logs en texto; el journal vive en `/var/log/journal` |
| `/var/lib` | Estado de los servicios (bases, contenedores) |
| `/opt` o `/srv` | Aplicaciones y datos propios |
| `/tmp` | Temporales: se pueden borrar en cualquier reinicio |
| `/proc` y `/sys` | El kernel expuesto como archivos (`/proc/meminfo`, `/proc/<PID>/`) |

---

## Errores que dan un número falso

| Error | Por qué pasa | Cómo se hace |
|-------|--------------|--------------|
| `uniq -c` sin `sort` | `uniq` solo junta repetidos **consecutivos**: `b a b a` da cuatro unos | `sort \| uniq -c` |
| Contar con `$9` sin mirar | Una petición vacía (`"-"`) mueve los campos y en `$9` queda otra cosa | Revisa primero `awk '{print $9}' \| sort \| uniq -c` |
| `ls $f` sin comillas | La separación de palabras parte el nombre | `"$f"` |
| `wc -l` en un archivo sin salto final | Cuenta saltos de línea, no líneas: `printf x \| wc -l` da 0 | Saber de dónde viene el archivo |
| Ordenar números con `sort` | Orden alfabético: `10` antes que `9` | `sort -n` (o `-h` para `2K`, `1G`) |
| `cmd 2>&1 > f` | El error ya apuntaba a la pantalla | `cmd > f 2>&1` |
| Parsear la salida de `ls` | Nombres con espacios o saltos de línea | `find ... -print0 \| xargs -0` |

La regla: antes de confiar en un conteo, mira qué valores distintos hay. Lo que no esperabas es justo lo que tenías que ver.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Shell | Programa que lee lo que escribes, lo expande y lanza los procesos; en Ubuntu, Bash |
| Expansión | Transformaciones que hace el shell antes de ejecutar: llaves, tilde, variables, sustitución de comandos, aritmética, separación de palabras y comodines |
| Globbing | Expansión de comodines (`*`, `?`, `[...]`) contra nombres de archivo |
| Separación de palabras | Paso en el que el shell parte en espacios el resultado de una expansión sin comillas |
| stdin, stdout, stderr | Los tres canales estándar de un proceso: 0 entrada, 1 salida y 2 errores |
| Redirección | Cambiar a dónde apunta un canal (`>`, `>>`, `2>`, `<`, `2>&1`) |
| Tubería (pipe) | Conexión `\|` entre la salida de un proceso y la entrada del siguiente |
| Builtin | Comando implementado dentro del shell, como `cd` o `echo`; no está en `$PATH` |
| PATH | Variable con las carpetas donde el shell busca programas, en orden |
| awk | Lenguaje para procesar texto por columnas: filtra, suma y agrupa |
| Historial | Registro de comandos del shell; en Bash vive en `~/.bash_history` |
| HISTCONTROL | Variable que decide qué no se guarda en el historial (`ignoreboth`) |
| CDPATH | Variable con carpetas donde `cd` busca directorios relativos |
| Byte nulo | Separador (`\0`) que no puede aparecer en un nombre de archivo; lo usan `find -print0` y `xargs -0` |

---

*Rogelio Arriaga Gonzalez*
