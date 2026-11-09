<!-- _class: lead -->

# S01 - La terminal eficiente

### Linux para desarrolladores

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Antes de empezar, cada equipo entra a su VM: ssh <USUARIO>@<IP_DE_SU_VM>.
Si alguien no entra, se resuelve en la apertura, no a la mitad de la práctica.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, entrar a la VM | 28 min |
| Parte 1 | Lo que hace el shell antes de correr un comando | 50 min |
| Parte 2 | Entrada, salida y tuberías | 66 min |
| Parte 3 | Historial y moverse por el sistema de archivos | 39 min |
| Parte 4 | Práctica: preguntas a un log de Nginx | 78 min |
| Cierre | Errores que dan un número falso, tarea y quiz | 39 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. -->

---

## El lema de la sesión

> Quien domina las tuberías contesta en una línea lo que otros resuelven con una hoja de cálculo.

- Un servidor en problemas no te deja abrir Excel: te deja una terminal.
- La pregunta "¿quién nos está pidiendo tanto?" se contesta en segundos o en una hora.
- Hoy aprendemos a contestar en segundos, **y a desconfiar del número** hasta verlo fallar.

<!-- Pregunta al grupo: ¿quién ha copiado un comando de internet sin entender qué hace cada parte? Casi todos levantan la mano. Esa es la razón del curso. -->

---

## Al terminar puedes

- Explicar qué le hace el shell a tu línea **antes** de correrla.
- Separar la salida normal de los errores y mandar cada una a donde va.
- Encadenar `grep`, `cut`, `sort`, `uniq`, `awk` y compañía para contestar una pregunta.
- Recuperar comandos del historial sin volver a escribirlos.
- Moverte por el sistema de archivos sin perderte.
- Detectar cuándo una tubería te da un número **falso**.

---

## Cómo trabajamos en este curso

- **Una VM por equipo** con Ubuntu Server 24.04 LTS. Es la misma durante las seis sesiones.
- Todo lo que se ve en clase se corre en la VM, no se mira en la lámina.
- **La regla del curso:** si no lo vieron fallar, no lo probaron.
- Cada práctica tiene un paso "Verlo fallar" y una evidencia que se entrega.

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
lsb_release -d        # debe decir Ubuntu 24.04
echo $SHELL           # debe decir /bin/bash
```

<!-- Revisa que todos tengan bash. Si alguien trae zsh en su laptop, la VM sigue siendo bash: ahí trabajamos. -->

---

<!-- _class: lead -->

# Parte 1
## Lo que hace el shell antes de correr un comando

---

## El shell reescribe tu línea

Cuando presionas Enter, el programa todavía no corre. Primero el shell:

1. Parte la línea en palabras y reconoce operadores (`|`, `>`, `&&`).
2. **Expande** variables, comodines, llaves y sustituciones.
3. Prepara las **redirecciones**.
4. Busca **qué** es el comando: alias, función, builtin o programa del `PATH`.
5. Recién entonces lo ejecuta, con la lista de argumentos ya transformada.

El programa nunca ve tu `*.log`: ve la lista de archivos que el shell armó.

Fuente: Barrett, cap. 2.

<!-- Idea central de la sesión: el que interpreta los comodines es bash, no ls. Por eso `ls *.log` y `grep x *.log` se comportan igual con los comodines. -->

---

## El orden de las expansiones

| Orden | Expansión | Ejemplo | Queda |
|---|---|---|---|
| 1 | Llaves | `{a,b}.log` | `a.log b.log` |
| 2 | Tilde | `~` | `/home/ana` |
| 3 | Variables y parámetros | `$HOME` | `/home/ana` |
| 3 | Aritmética | `$((2 + 3))` | `5` |
| 3 | Sustitución de comando | `$(date +%F)` | `2026-11-07` |
| 4 | Separación de palabras | `$archivo` sin comillas | se parte en los espacios |
| 5 | Comodines | `*.log` | los nombres que coinciden |
| 6 | Quitar comillas | `"hola"` | `hola` |

Fuente: manual de Bash (GNU), sección *Shell Expansions*.

<!-- No hace falta memorizar la tabla. Sí hace falta saber que la separación de palabras y los comodines ocurren DESPUÉS de expandir variables: de ahí salen la mitad de los errores. -->

---

## Demostración: ver la expansión antes de correr

```bash
echo respaldo-{lunes,martes}.tar.gz
# respaldo-lunes.tar.gz respaldo-martes.tar.gz

echo ~ $((2 + 3)) "hoy es $(date +%A)"
# /home/ana 5 hoy es Saturday

cd /etc && echo host*
# host.conf hostname hosts hosts.allow hosts.deny

echo nada*
# nada*        (sin coincidencias, bash deja el patrón tal cual)
```

**Truco:** antes de un `rm` con comodines, cambia `rm` por `echo` y mira la lista.

<!-- Demo en vivo en la VM. Pídeles que corran `echo nada*` y expliquen por qué sale literal. Ese comportamiento sorprende a quien viene de otros shells. -->

---

## Comillas: tres niveles de protección

```bash
NOMBRE=mundo
echo Hola $NOMBRE        # Hola mundo      (expande y parte en palabras)
echo "Hola $NOMBRE"      # Hola mundo      (expande, pero no parte)
echo 'Hola $NOMBRE'      # Hola $NOMBRE    (no expande nada)
echo "Hola \$NOMBRE"     # Hola $NOMBRE    (la diagonal protege un carácter)
```

- **Dobles:** expanden `$`, `$( )` y `` ` ``; protegen espacios y comodines.
- **Sencillas:** todo literal. Ideales para programas de `awk` y expresiones de `grep`.
- Regla práctica: **toda variable va entre comillas dobles** salvo que quieras que se parta.

Fuente: Barrett, cap. 2.

---

## Separación de palabras: el archivo con espacio

```bash
archivo="mi reporte.txt"
touch "$archivo"

ls -l $archivo
# ls: cannot access 'mi': No such file or directory
# ls: cannot access 'reporte.txt': No such file or directory

ls -l "$archivo"
# -rw-r--r-- 1 ana ana 0 Nov  7 10:15 mi reporte.txt
```

Con `printf` se ve cuántos argumentos recibe el programa:

```bash
printf '[%s]\n' $archivo      # [mi]  [reporte.txt]   dos argumentos
printf '[%s]\n' "$archivo"    # [mi reporte.txt]      uno
```

<!-- printf '[%s]\n' es la herramienta para "ver" la separación de palabras. Que lo anoten en su hoja de atajos. -->

---

## ¿Qué corre de verdad? `type -a`

```bash
type -a ls
# ls is aliased to `ls --color=auto'
# ls is /usr/bin/ls
# ls is /bin/ls

type -a echo
# echo is a shell builtin
# echo is /usr/bin/echo

type ll
# ll is aliased to `ls -alF'
```

- Orden de búsqueda: **alias, palabra reservada, función, builtin, `PATH`**.
- El alias `ll` viene del `.bashrc` de Ubuntu: en un script no existe.
- Si un comando "se porta raro" en tu máquina y no en la de otro, empieza por `type -a`.

<!-- Pregunta: ¿por qué `ls` aparece dos veces en el PATH? Porque en Ubuntu /bin es un enlace a /usr/bin. No es un error. -->

---

## Ver lo que el shell hizo: `set -x`

```bash
set -x
cd /etc
ls host* > /dev/null
# + cd /etc
# + ls host.conf hostname hosts hosts.allow hosts.deny
set +x
```

- Cada línea con `+` es el comando **ya expandido**, tal como lo recibe el programa.
- Sirve igual en la terminal que en un script (`bash -x script.sh`, sesión 3).

---

## Momento de práctica: predice antes de correr

En parejas, **escriban** qué imprime cada línea y después córranla:

```bash
cd /etc
echo "*.conf"
echo *.conf | wc -w
echo '$HOME' "$HOME"
echo {1..3}-{a,b}
X="a   b"; echo $X; echo "$X"
```

- Cada acierto es un punto; cada error se explica con la tabla de expansiones.
- 10 minutos.

<!-- Respuestas: literal; el número de .conf en /etc; $HOME y /home/<usuario>; 1-a 1-b 2-a 2-b 3-a 3-b; "a b" y "a   b". El último es el que más fallan. -->

---

<!-- _class: lead -->

# Parte 2
## Entrada, salida y tuberías

---

## Tres flujos por proceso

| Descriptor | Nombre | Por defecto | Para qué |
|---|---|---|---|
| 0 | stdin | el teclado | lo que el programa lee |
| 1 | stdout | la pantalla | el resultado |
| 2 | stderr | la pantalla | los errores y avisos |

- stdout y stderr se ven igual en la pantalla, pero **son dos caminos distintos**.
- Una tubería `|` solo conecta **stdout** con el stdin del siguiente.
- Por eso un error "atraviesa" la tubería y sale en tu pantalla.

Fuente: Barrett, cap. 1; Hausenblas, cap. 3.

---

## Redirecciones

| Escribes | Hace |
|---|---|
| `cmd > archivo` | stdout al archivo, lo reemplaza |
| `cmd >> archivo` | stdout al final del archivo |
| `cmd 2> errores.txt` | stderr a un archivo |
| `cmd 2> /dev/null` | stderr a la basura |
| `cmd > todo.txt 2>&1` | stdout al archivo y stderr a donde va stdout |
| `cmd &> todo.txt` | lo mismo, forma corta de bash |
| `cmd < entrada.txt` | stdin desde un archivo |

**Cuidado:** `>` vacía el archivo **antes** de correr el comando. `sort f > f` te deja `f` vacío.

<!-- Demuéstralo: crea un archivo con tres líneas y corre `sort f > f`. Queda vacío. Que lo vean fallar. -->

---

## El orden de `2>&1` importa

```bash
ls /noexiste /etc/hostname > a.txt 2>&1
cat a.txt
# ls: cannot access '/noexiste': No such file or directory
# /etc/hostname

ls /noexiste /etc/hostname 2>&1 > b.txt
# ls: cannot access '/noexiste': No such file or directory   (sale en pantalla)
cat b.txt
# /etc/hostname
```

- Las redirecciones se aplican **de izquierda a derecha**.
- `2>&1` quiere decir "stderr a donde apunta stdout **en este momento**".
- En el segundo caso, cuando se copió, stdout todavía era la pantalla.

---

## Tuberías: cada programa hace una cosa

```bash
cat access.log | wc -l          # funciona, pero cat sobra
wc -l < access.log              # igual, sin un proceso extra
grep ' 502 ' access.log | wc -l # cuántos 502
```

- Cada programa de la tubería corre **al mismo tiempo**; los datos fluyen por líneas.
- Se construye de izquierda a derecha, **viendo la salida en cada paso** con `head`.
- Una tubería larga no es mérito; una tubería que se puede leer, sí.

Fuente: Barrett, cap. 1.

---

## Las seis herramientas para empezar

| Comando | Qué hace | Ejemplo |
|---|---|---|
| `wc` | cuenta líneas, palabras, bytes | `wc -l access.log` |
| `head` / `tail` | primeras o últimas líneas | `tail -n 20 access.log` |
| `cut` | saca columnas por delimitador | `cut -d' ' -f1 access.log` |
| `grep` | filtra líneas que coinciden | `grep -v '/api/health' access.log` |
| `sort` | ordena | `sort -rn` (numérico, al revés) |
| `uniq` | junta repetidos **adyacentes** | `uniq -c` (cuenta) |

Fuente: Barrett, cap. 1, presenta estas seis como el punto de partida.

<!-- Subraya "adyacentes" en uniq. En la práctica lo van a ver fallar. -->

---

## `sort` y `uniq`: la pareja que cuenta

```bash
cut -d' ' -f1 access.log | sort | uniq -c | sort -rn | head -3
#    1613 192.0.2.28
#     246 203.0.113.4
#     203 192.0.2.25
```

Léela de izquierda a derecha:

1. `cut` deja solo la IP.
2. `sort` pone juntas las IP iguales.
3. `uniq -c` cuenta cada grupo.
4. `sort -rn` ordena por la cuenta, mayor primero.
5. `head -3` se queda con el podio.

Este patrón (**ordenar, contar, ordenar**) contesta la mitad de las preguntas de un log.

---

## `awk`: columnas y cuentas

```bash
awk '{print $1}' access.log                 # primera columna
awk '$9 == 502' access.log | wc -l          # filtra por un campo
awk '{print $NF}' access.log | head -3      # último campo
awk '{s += $NF} END {print s / NR}' access.log   # promedio del último campo
```

- Parte cada línea en campos por espacios: `$1`, `$2`... `$NF` es el último.
- `NR` es el número de línea; `END` corre al terminar de leer.
- Los arreglos de `awk` agrupan sin `sort`: `cuenta[$1]++`.

Fuente: Barrett, cap. 5, lo trata a fondo; hoy usamos lo mínimo.

---

## `sed` y `tr`: transformar en el camino

```bash
sed 's/192\.0\.2\.[0-9]*/IP_OCULTA/' access.log | head -1
# IP_OCULTA - - [07/Nov/2026:00:00:08 +0000] "GET /static/app.js HTTP/1.1" ...

echo "GET /API/Pedidos" | tr '[:upper:]' '[:lower:]'
# get /api/pedidos

sed -n '100,105p' access.log        # solo las líneas 100 a 105
```

- `sed 's/viejo/nuevo/'` cambia la primera coincidencia de cada línea; con `g`, todas.
- `tr` cambia o borra **caracteres**, no palabras.
- Para compartir un log con alguien de fuera: ocultar IP con `sed` antes de mandarlo.

---

## `tee`: guardar y seguir

```bash
awk '$9 ~ /^5/' access.log | tee errores-5xx.log | wc -l
# 179

wc -l errores-5xx.log
# 179 errores-5xx.log
```

- `tee` copia su entrada a un archivo **y** a stdout.
- Útil para dejar evidencia de un paso intermedio sin cortar la tubería.
- `tee -a` agrega al final en lugar de reemplazar.
- Con `sudo`: `echo texto | sudo tee /etc/archivo` (el `>` lo haría tu usuario, no root).

---

## `find` y `xargs`, con nombres raros

```bash
find /var/log -name '*.log' -mtime -1        # logs modificados en el último día
find /var/log -size +100M                    # archivos de más de 100 MB

find /tmp/x -name '*.log' | xargs ls -1
# ls: cannot access '/tmp/x/a/con': No such file or directory
# ls: cannot access 'espacio.log': No such file or directory

find /tmp/x -name '*.log' -print0 | xargs -0 ls -1
# /tmp/x/a/con espacio.log
# /tmp/x/b/normal.log
```

- `xargs` convierte líneas en argumentos y **parte en los espacios**.
- `-print0` y `-0` separan con un carácter nulo: aguanta cualquier nombre.

Fuente: Barrett, cap. 5.

---

## Demostración en vivo: una tubería desde cero

Pregunta: **¿qué rutas piden más?**

```bash
head -3 access.log
grep -oE '"GET [^ ]+' access.log | head -3
grep -oE '"GET [^ ]+' access.log | cut -d' ' -f2 | head -3
grep -oE '"GET [^ ]+' access.log | cut -d' ' -f2 | sed 's/?.*//' | sort | uniq -c | sort -rn | head -3
#     803 /api/reportes/mensual
#     785 /login
#     780 /static/app.js
```

- Cada paso se prueba con `head` antes de agregar el siguiente.
- Con flecha arriba se recupera la línea y se le agrega un tramo.

<!-- Hazlo en vivo, sin copiar. Equivócate a propósito una vez (por ejemplo, olvida el sed de la query string) y muestra cómo aparece /api/productos?pagina=3 como ruta aparte. -->

---

<!-- _class: lead -->

# Parte 3
## Historial y moverse por el sistema de archivos

---

## Historial: buscar en lugar de escribir

- **Ctrl-R** y escribe un pedazo: busca hacia atrás. Otra vez Ctrl-R, la siguiente coincidencia.
- **Enter** la corre; **flecha derecha** la deja lista para editar.
- `history | grep nginx` cuando no recuerdas ni el pedazo.
- `!123` corre la línea 123 del historial (mírala antes con `history`).

Fuente: Barrett, cap. 3.

<!-- Ejercicio rápido: que cada quien recupere con Ctrl-R la tubería de la demostración anterior sin tocar las flechas. -->

---

## Expansión del historial

```bash
ls /etc/hostname
echo !$             # !$ es el último argumento de la línea anterior
# echo /etc/hostname

systemctl status nginx
sudo !!             # !! es la línea anterior completa
# sudo systemctl status nginx

grep 502 acces.log
^acces^access       # corrige la línea anterior y la vuelve a correr
```

- `!$`, `!!` y `^viejo^nuevo` se expanden **antes** de correr: bash te muestra la línea final.
- Si no estás seguro, agrega `:p` (por ejemplo `!!:p`) para ver sin correr.

---

## Configurar el historial

Ubuntu ya trae en `~/.bashrc`:

```bash
HISTCONTROL=ignoreboth    # no guarda duplicados seguidos ni líneas que empiezan con espacio
HISTSIZE=1000             # líneas en memoria
HISTFILESIZE=2000         # líneas en ~/.bash_history
```

Vale la pena agregar:

```bash
HISTTIMEFORMAT='%F %T '   # fecha y hora en `history`
HISTSIZE=10000
HISTFILESIZE=20000
```

- Con `ignoreboth`, un comando que empieza con espacio **no se guarda**: útil para no dejar un token en el historial.
- En un servidor, el historial con fecha es evidencia de qué se hizo y cuándo.

---

## Moverse: `cd -`, `pushd` y `popd`

```bash
cd /etc/nginx
cd /var/log
cd -              # regresa a /etc/nginx y lo imprime
cd -              # otra vez a /var/log

pushd /var/log    # entra y guarda la carpeta en una pila
pushd /etc        # otra
dirs -v           # 0 /etc   1 /var/log   2 la de antes
popd              # regresa a /var/log
```

- `cd` sin nada te lleva a tu `$HOME`; `~-` es la carpeta anterior.
- `pushd`/`popd` sirven cuando saltas entre tres o más carpetas.

Fuente: Barrett, cap. 4.

---

## `CDPATH` y `globstar`

```bash
CDPATH=.:/var/log:/etc
cd nginx           # no existe aquí: bash la busca en CDPATH
# /var/log/nginx

shopt -s globstar
ls /etc/nginx/**/*.conf     # ** baja a todas las subcarpetas
# /etc/nginx/fastcgi.conf
# /etc/nginx/nginx.conf
# /etc/nginx/snippets/fastcgi-php.conf
# /etc/nginx/snippets/snakeoil.conf
```

- Sin `globstar`, `**` se comporta como `*` y solo baja un nivel.
- `CDPATH` va en tu `~/.bashrc`, **nunca** exportado: un script con otro `cd` se confundiría.

---

## Encontrar lo que creció o cambió

```bash
ls -ltr /var/log | tail -5                  # lo último que se modificó, al final
du -sh /var/log/* 2>/dev/null | sort -h | tail -5   # qué carpeta pesa más
find /var/log -newer /etc/nginx/nginx.conf  # lo que cambió después de ese archivo
find / -xdev -size +500M 2>/dev/null        # archivos grandes, sin cruzar a otros discos
```

- `sort -h` entiende `K`, `M` y `G`; `sort -n` no.
- `2>/dev/null` calla los "Permission denied" de carpetas que no puedes leer.
- Esto se vuelve a usar en la sesión 6, cuando el disco se llena.

---

<!-- _class: lead -->

# Parte 4
## Práctica: preguntas a un log de Nginx

---

## El laboratorio

```bash
mkdir -p ~/s01 && cd ~/s01
nano generar-log.py          # copia el script de la guía de práctica
python3 generar-log.py
wc -l access.log
# 6004 access.log
```

- El log es de **un día** de un servidor de laboratorio: 6004 peticiones.
- Las IP son de rangos de documentación; ninguna es real.
- El formato es el de Nginx de siempre, con una columna más al final: **el tiempo de respuesta**.

<!-- El script está en practica.md. Es determinista: a todos les sale el mismo log, así que todos deben llegar a los mismos números. Si un equipo llega a otro número, su tubería está mal. -->

---

## Anatomía de una línea

```
192.0.2.28 - - [07/Nov/2026:00:00:08 +0000] "GET /static/app.js HTTP/1.1" 200 8345 "-" "curl/8.5.0" 0.015
```

| Campo de `awk` | Valor | Qué es |
|---|---|---|
| `$1` | `192.0.2.28` | IP del cliente |
| `$4` | `[07/Nov/2026:00:00:08` | fecha y hora |
| `$6` | `"GET` | método |
| `$7` | `/static/app.js` | ruta |
| `$9` | `200` | código de estado |
| `$10` | `8345` | bytes enviados |
| `$NF` | `0.015` | segundos que tardó |

El agente de usuario trae espacios: por eso el tiempo se pide como `$NF` y no por número.

---

## Pregunta 1: las 10 IP que más piden

```bash
awk '{print $1}' access.log | sort | uniq -c | sort -rn | head -10
```

```
   1613 192.0.2.28
    246 203.0.113.4
    203 192.0.2.25
    127 192.0.2.26
    100 198.51.100.28
     92 198.51.100.14
     91 203.0.113.3
     90 198.51.100.11
     89 203.0.113.12
     83 192.0.2.4
```

**Compruébalo de otra forma:** `grep -c '^192\.0\.2\.28 ' access.log` debe dar 1613.

<!-- 15 minutos. Que primero lo intenten solos. La comprobación cruzada con grep -c es parte de la respuesta: un número sin comprobar no se entrega. -->

---

## Pregunta 2: los 5xx por hora

```bash
awk '$9 ~ /^5/ {print substr($4, 2, 14)}' access.log | sort | uniq -c
```

```
      6 07/Nov/2026:00
     15 07/Nov/2026:01
     ...
     26 07/Nov/2026:14
      8 07/Nov/2026:15
     ...
```

- `substr($4, 2, 14)` quita el corchete y corta en la hora: `07/Nov/2026:14`.
- El orden alfabético sirve porque el log es de un solo día.
- Total de 5xx: `awk '$9 ~ /^5/' access.log | wc -l` da **179**.

---

## ¿Qué pasó a las 14 h?

Un número alto es una pregunta, no una respuesta. Se baja un nivel:

```bash
awk '$9 ~ /^5/ && $4 ~ /:14:/ {print $9, $7}' access.log | sort | uniq -c | sort -rn | head -4
```

```
     18 502 /api/pedidos
      2 500 /login
      2 500 /api/reportes/mensual
      2 500 /api/health
```

- 18 de los 26 son **502 en `/api/pedidos`**: la API detrás de Nginx no contestó.
- Eso ya es algo que se puede investigar en los logs de la API (sesión 6).

<!-- Esta es la habilidad que importa: del total a la hora, de la hora a la ruta, de la ruta al servicio. Cada nivel es una tubería que se recupera con flecha arriba y se le cambia un pedazo. -->

---

## Pregunta 3: la petición más lenta

```bash
awk '{print $NF, $7}' access.log | sort -rn | head -3
```

```
3.591 /api/reportes/mensual
3.590 /api/reportes/mensual
3.582 /api/reportes/mensual
```

- Se imprime primero el tiempo para que `sort -rn` ordene por él.
- La petición más lenta es una sola; la pregunta útil suele ser la siguiente.

---

## Pregunta 3b: la ruta más lenta en promedio

```bash
awk '{ruta = $7; sub(/\?.*/, "", ruta); suma[ruta] += $NF; n[ruta]++}
     END {for (r in suma) printf "%.3f %5d %s\n", suma[r] / n[r], n[r], r}' access.log \
  | sort -rn | head -5
```

```
2.308   803 /api/reportes/mensual
0.184   748 /api/pedidos
0.147   785 /login
0.101   723 /api/productos
0.064   700 /api/productos/42
```

- `sub(/\?.*/, "", ruta)` quita la query string: `/api/productos?pagina=3` cuenta como `/api/productos`.
- La columna del medio es cuántas veces se pidió: un promedio de 3 peticiones no vale lo mismo que uno de 800.

---

## Verlo fallar 1: `uniq` sin `sort`

```bash
awk '{print $1}' access.log | uniq -c | sort -rn | head -3
```

```
      6 192.0.2.28
      6 192.0.2.28
      5 192.0.2.28
```

- `uniq` solo junta líneas iguales **que están juntas**. Sin `sort`, cuenta rachas.
- La IP que más pide aparece con 6 en lugar de 1613, y repetida.
- `awk '{print $1}' access.log | uniq | wc -l` da **5481** grupos; las IP distintas son **88**.

**No da error. Da un número que parece razonable.** Ese es el peligro.

<!-- Que cada equipo lo corra y anote los dos números. Pregunta: ¿cuántas veces han visto un uniq sin sort en un script de la empresa? -->

---

## Verlo fallar 2: la línea que corre los campos

```bash
awk '{print $9}' access.log | sort | uniq -c | sort -rn
```

```
   5165 200
    345 304
    311 404
     99 500
     80 502
      4 "-"
```

¿Qué es `"-"`? Hay 4 líneas así:

```
198.51.100.250 - - [07/Nov/2026:00:00:08 +0000] "-" 400 0 "-" "-" 0.000
```

- Un cliente abrió la conexión y cerró sin pedir nada. Nginx escribe `"-"` en vez de `"GET /ruta HTTP/1.1"`.
- La petición tiene **una palabra** en lugar de tres: todos los campos se corren dos lugares.
- Resultado: contar 4xx con `$9` da **311**, y en realidad son **315**.

---

## La versión robusta: partir por comillas

```bash
awk -F'"' '{split($3, c, " "); print c[1]}' access.log | sort | uniq -c | sort -rn
```

```
   5165 200
    345 304
    311 404
     99 500
     80 502
      4 400
```

- Con `-F'"'` el campo 3 es lo que va entre la petición y el referer: ` 400 0 `.
- Ya no importa cuántas palabras traiga la petición.
- Regla: **antes de confiar en un campo, cuenta sus valores** y busca los que no esperabas.

<!-- Este es el cierre de la práctica: el número correcto sale de desconfiar del primero. -->

---

## Reto 1: ¿quién es 192.0.2.28?

Una IP con el 27 % del tráfico merece una mirada.

```bash
grep '^192\.0\.2\.28 ' access.log | awk -F'"' '{print $6}' | sort | uniq -c | sort -rn
grep '^192\.0\.2\.28 ' access.log | awk '{print $7}' | sed 's/?.*//' | sort | uniq -c | sort -rn | head -3
```

- ¿Qué agentes de usuario usa? ¿Qué rutas pide?
- ¿Es un monitor, un usuario real o un abuso? Justifica con números.

---

## Reto 2: bytes servidos por ruta

```bash
awk '{ruta = $7; sub(/\?.*/, "", ruta); b[ruta] += $10} END {for (r in b) print b[r], r}' access.log \
  | sort -rn | head -3
```

- ¿Qué ruta mueve más datos? ¿Coincide con la que más se pide?
- Vuelve a correrlo con el campo robusto de bytes: ¿cambia algo por las 4 líneas raras?

---

## Entrega de la práctica

Un archivo `respuestas-s01.txt` con, para cada pregunta:

1. La **línea** que la contesta.
2. La **salida** que obtuviste.
3. La **comprobación cruzada** (otra tubería o un `grep -c` que da lo mismo).

Más el paso "Verlo fallar": los dos números de `uniq` sin `sort` y la explicación de las líneas `"-"`.

```bash
history | tail -40 > historial-s01.txt
```

<!-- Se entrega por equipo. Revisa en clase al menos un equipo en voz alta. -->

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Errores que dan un número falso

| Error | Qué ves | Cómo se evita |
|---|---|---|
| `uniq -c` sin `sort` | conteos chicos y repetidos | siempre `sort` antes de `uniq` |
| Contar por número de campo | categorías raras como `"-"` | contar los valores del campo antes de usarlo |
| Ruta con query string | la misma ruta en varias filas | `sub(/\?.*/, "", ruta)` |
| `sort -n` con `K` y `M` | `9K` después de `1G` | `sort -h` |
| `xargs` con espacios | archivos que "no existen" | `find -print0` con `xargs -0` |
| `sort f > f` | el archivo queda vacío | escribir a otro archivo y luego `mv` |

Ninguno da error. Todos dan un número que alguien va a creer.

---

## Lo que te llevas

- El shell expande, separa y busca **antes** de correr: `echo`, `printf '[%s]\n'` y `set -x` te lo muestran.
- stdout y stderr son dos caminos; el orden de las redirecciones importa.
- Ordenar, contar, ordenar: `sort | uniq -c | sort -rn`.
- Del total a la hora, de la hora a la ruta: cada nivel es la tubería anterior con un cambio.
- Ctrl-R antes que volver a escribir.
- Un número sin comprobación cruzada no se entrega.

---

## Tarea

**Tu hoja de atajos propia**, con un ejemplo real por comando.

- Mínimo 25 entradas: expansión, redirección, tuberías, historial y movimiento.
- Cada entrada con un ejemplo **que corriste** en tu VM y su salida.
- Al menos 3 entradas de "esto me dio un número falso y así lo detecté".

Detalle completo en la guía de tarea.

---

## Lecturas y libros

**Obligatorias**

- `dev-notes/10-cloud/03-linux-nginx.md`, sección "Comandos esenciales de navegación".
- `dev-notes/10-cloud/18-terminal-eficiente.md`, completa.

**Recomendadas**

- *Efficient Linux at the Command Line* (Barrett, 2022), caps. 1 a 4.
- *Learning Modern Linux* (Hausenblas, 2022), cap. 3.

---

## Siguiente sesión

### S02 - Usuarios, permisos y paquetes

- Quién es quién en `/etc/passwd` y por qué tu API no debe correr como tu usuario.
- `sudo` con criterio: quién puede, para qué y cómo se audita.
- Permisos, umask y una carpeta compartida por grupo que **sí** funciona.
- El gestor de paquetes sin miedo.

> El mínimo privilegio empieza en el sistema operativo, no en la aplicación.

---

## Quiz: la terminal eficiente

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin la VM abierta: se contesta con lo que entendiste, no con lo que copias.
- Lo que falles, búscalo en la nota `18-terminal-eficiente` antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
