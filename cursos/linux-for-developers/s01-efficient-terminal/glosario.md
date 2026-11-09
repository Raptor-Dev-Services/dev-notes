# Glosario - S01 - La terminal eficiente

> Definiciones de los términos técnicos de la sesión 1.
> Ordenado alfabéticamente.

---

## A

### awk
**Categoría:** Herramientas de texto
**Definición:** Lenguaje pequeño para procesar texto por líneas y campos. Parte cada línea en campos (`$1`, `$2`... `$NF` es el último), permite filtrar con condiciones y agrupar con arreglos asociativos. El bloque `END` corre al terminar de leer la entrada.
**En el curso:** Saca la IP (`$1`), el código (`$9`) y el tiempo de respuesta (`$NF`) del access.log, y calcula el promedio por ruta con `suma[ruta] += $NF`.

---

## B

### Builtin
**Categoría:** Shell
**Definición:** Comando que vive dentro del propio shell en lugar de ser un programa aparte en el disco. `cd`, `echo`, `type` y `history` son builtins de bash. `cd` tiene que serlo: un programa aparte no podría cambiar la carpeta del shell que lo llamó.
**En el curso:** `type -a echo` muestra que existe el builtin y también `/usr/bin/echo`; bash usa el builtin primero.

---

## C

### CDPATH
**Categoría:** Shell / Navegación
**Definición:** Variable con una lista de carpetas, separadas por dos puntos, donde `cd` busca cuando el nombre que le das no existe en la carpeta actual. Se define en `~/.bashrc` sin exportarla, para que no altere el `cd` de los scripts.
**En el curso:** Con `CDPATH=.:/var/log:/etc`, `cd nginx` desde cualquier lugar lleva a `/var/log/nginx`.

---

### Comillas
**Categoría:** Shell
**Definición:** Mecanismo para controlar qué expande el shell. Las comillas dobles permiten expandir variables y sustituciones pero protegen espacios y comodines; las sencillas dejan todo literal; la diagonal invertida protege un solo carácter.
**En el curso:** Regla práctica: toda variable va entre comillas dobles; los programas de `awk` y las expresiones de `grep` van entre sencillas.

---

### Comodín
**Categoría:** Shell
**Definición:** Patrón como `*`, `?` o `[abc]` que el shell reemplaza por los nombres de archivo que coinciden, antes de correr el comando. El programa nunca ve el patrón, ve la lista. Si nada coincide, bash deja el patrón literal. También se le llama *glob*.
**En el curso:** `echo nada*` imprime `nada*`; antes de un `rm` con comodines se cambia `rm` por `echo` para ver la lista.

---

### Comprobación cruzada
**Categoría:** Método
**Definición:** Llegar al mismo número por un camino distinto para confirmar que una tubería está bien. Si dos herramientas independientes coinciden, el resultado es confiable; si no, una de las dos tiene un error.
**En el curso:** Las 1613 peticiones de 192.0.2.28 se confirman con `grep -c`, y la suma de los 5xx por hora debe dar el total de 179.

---

## D

### Descriptor de archivo
**Categoría:** Sistema operativo
**Definición:** Número que un proceso usa para referirse a un archivo abierto o a un flujo. Todo proceso empieza con tres: 0 (stdin), 1 (stdout) y 2 (stderr). Las redirecciones cambian a dónde apunta cada uno.
**En el curso:** `2>&1` copia el descriptor 1 en el 2: "los errores van a donde vaya la salida en este momento".

---

## E

### Expansión de llaves
**Categoría:** Shell
**Definición:** Primera de las expansiones de bash: `{a,b,c}` genera una palabra por elemento y `{1..5}` genera una secuencia. No depende de que existan archivos, a diferencia de los comodines.
**En el curso:** `echo respaldo-{lunes,martes}.tar.gz` y `mkdir -p demo/{logs,datos,tmp}`.

---

### Expansión del historial
**Categoría:** Shell / Historial
**Definición:** Atajos que bash reemplaza por partes de comandos anteriores antes de correr la línea: `!!` es la línea anterior completa, `!$` su último argumento, `!123` la línea 123 y `^viejo^nuevo` corrige la anterior. Con `:p` se imprime sin correr.
**En el curso:** `sudo !!` repite con sudo el comando que falló por permisos.

---

## G

### Globstar
**Categoría:** Shell
**Definición:** Opción de bash (`shopt -s globstar`) que hace que `**` coincida con cualquier número de subcarpetas. Sin ella, `**` se comporta como `*` y solo baja un nivel.
**En el curso:** `ls /etc/nginx/**/*.conf` encuentra también los `.conf` de las subcarpetas de Nginx.

---

## H

### HISTCONTROL
**Categoría:** Shell / Historial
**Definición:** Variable que decide qué líneas no se guardan en el historial. Con `ignoreboth`, que Ubuntu trae en `~/.bashrc`, no se guardan duplicados seguidos ni líneas que empiezan con espacio.
**En el curso:** Empezar con un espacio una línea que lleva un dato sensible evita que quede en `~/.bash_history`. `HISTTIMEFORMAT` agrega fecha y hora a cada entrada.

---

## P

### PATH
**Categoría:** Shell
**Definición:** Variable con la lista de carpetas donde el shell busca un programa cuando el comando no es alias, palabra reservada, función ni builtin. Se recorre en orden y gana la primera coincidencia.
**En el curso:** `type -a ls` muestra el alias y dos rutas del `PATH`, porque en Ubuntu `/bin` es un enlace a `/usr/bin`.

---

### Pila de directorios
**Categoría:** Shell / Navegación
**Definición:** Lista de carpetas que bash mantiene con `pushd` (entra y guarda), `popd` (regresa a la anterior) y `dirs -v` (la muestra numerada). Complementa a `cd -`, que solo alterna entre dos carpetas.
**En el curso:** Sirve para saltar entre `/etc/nginx`, `/var/log` y la carpeta del proyecto sin escribir rutas largas.

---

## Q

### Query string
**Categoría:** HTTP
**Definición:** Parte de una URL que va después del `?` y lleva parámetros, como `?pagina=3`. Cambia de una petición a otra aunque la ruta sea la misma.
**En el curso:** Se quita con `sub(/\?.*/, "", ruta)` antes de agrupar, para que `/api/productos?pagina=3` cuente como `/api/productos`.

---

## R

### Redirección
**Categoría:** Shell
**Definición:** Cambio de a dónde lee o escribe un comando: `>` reemplaza un archivo, `>>` agrega al final, `2>` manda los errores, `<` lee de un archivo. El shell las prepara de izquierda a derecha **antes** de correr el comando, con los permisos de tu usuario.
**En el curso:** Por eso `sort f > f` deja el archivo vacío y `sudo echo x > /etc/archivo` falla: la redirección no la hace root.

---

## S

### Separación de palabras
**Categoría:** Shell
**Definición:** Paso de la expansión en el que bash parte en los espacios, tabuladores y saltos de línea el resultado de una variable o sustitución que no está entre comillas. Ocurre después de expandir variables y antes de los comodines.
**En el curso:** `cat $archivo` con `archivo="reporte final.txt"` recibe dos argumentos; `printf '[%s]\n'` muestra cuántos llegaron.

---

### stderr
**Categoría:** Sistema operativo
**Definición:** Salida de errores estándar, descriptor 2. Por defecto va a la pantalla, igual que stdout, pero es un camino separado: una tubería no lo conecta y una redirección `>` no lo captura.
**En el curso:** `2>/dev/null` calla los "Permission denied" de `find` y `du` al recorrer carpetas ajenas.

---

### stdin
**Categoría:** Sistema operativo
**Definición:** Entrada estándar, descriptor 0. Es lo que un programa lee cuando no se le da un archivo: el teclado, la salida del comando anterior en una tubería o un archivo con `<`.
**En el curso:** `wc -l < access.log` cuenta líneas leyendo de stdin, sin el `cat` de más.

---

### stdout
**Categoría:** Sistema operativo
**Definición:** Salida estándar, descriptor 1. Es donde un programa escribe su resultado. Es lo único que una tubería `|` pasa al siguiente comando.
**En el curso:** Cada tramo de las tuberías de la práctica toma el stdout del anterior como su stdin.

---

### Sustitución de comando
**Categoría:** Shell
**Definición:** Forma `$(comando)` que el shell reemplaza por la salida del comando, sin el salto de línea final. Se expande junto con las variables, así que sin comillas también se parte en palabras.
**En el curso:** `"hoy es $(date +%A)"` y, en la sesión 3, el nombre del respaldo con `$(date +%Y%m%d-%H%M%S)`.

---

## T

### tee
**Categoría:** Herramientas de texto
**Definición:** Programa que copia su entrada a uno o más archivos y también a stdout, para guardar un paso intermedio sin cortar la tubería. Con `-a` agrega en lugar de reemplazar.
**En el curso:** `awk '$9 ~ /^5/' access.log | tee errores-5xx.log | wc -l`, y `echo x | sudo tee /etc/archivo` para escribir como root.

---

### Tubería
**Categoría:** Shell
**Definición:** Conexión, con `|`, del stdout de un comando con el stdin del siguiente. Todos los comandos de la tubería corren al mismo tiempo y los datos fluyen por líneas. En inglés, *pipe* o *pipeline*.
**En el curso:** El patrón de la sesión es ordenar, contar y ordenar: `sort | uniq -c | sort -rn`.

---

## U

### uniq
**Categoría:** Herramientas de texto
**Definición:** Programa que junta en una sola las líneas repetidas **adyacentes**; con `-c` antepone cuántas había. No ve repeticiones separadas por otras líneas, por eso casi siempre va después de `sort`.
**En el curso:** Sin `sort`, la IP de 1613 peticiones aparece con 6: el paso "Verlo fallar" de la práctica.

---

## X

### xargs
**Categoría:** Herramientas de texto
**Definición:** Programa que convierte líneas de su entrada en argumentos de otro comando. Por defecto separa en espacios y saltos de línea, lo que rompe nombres de archivo con espacios; con `-0` separa por el carácter nulo que produce `find -print0`.
**En el curso:** `find /tmp/x -name '*.log' -print0 | xargs -0 ls -1` lista bien el archivo `con espacio.log`.
