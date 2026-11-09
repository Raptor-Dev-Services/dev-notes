# Glosario - S03 - Bash que falla en voz alta

> Los términos de la sesión 3, en orden alfabético.

---

## A

### Archivo parcial
**Categoría:** Scripting / Patrón
**Definición:** Archivo que un script escribe con un nombre provisional (por ejemplo, terminado en `.parcial`) mientras todavía no termina ni lo verifica. Solo al final se renombra con `mv` a su nombre definitivo. Dentro del mismo sistema de archivos el renombre ocurre en un solo paso, así que nadie ve nunca un resultado a medias con el nombre de uno bueno.
**En el curso:** `respaldo.sh` escribe `respaldo-FECHA-HORA.tar.gz.parcial`, lo verifica y lo mueve; si algo falla, `trap EXIT` lo borra.

---

## B

### bash -n
**Categoría:** Bash / Depuración
**Definición:** Opción que hace que Bash lea el script completo y reporte errores de sintaxis sin ejecutar ningún comando. Sale con 0 si la sintaxis es válida y con 2 si no. No detecta errores de lógica ni comandos que no existen.
**En el curso:** se corre sobre `respaldo.sh` antes de dejarlo en `/usr/local/bin`.

---

### bash -x
**Categoría:** Bash / Depuración
**Definición:** Opción (equivalente a `set -x`) que imprime en stderr cada comando ya expandido antes de ejecutarlo, con el prefijo que define `PS4`. Muestra el camino real que siguió el script: qué condición falló y qué valores tenían las variables.
**En el curso:** con `sudo bash -x respaldo.sh ...` se encuentra la condición `(( esperados > 0 ))` que detiene el respaldo de un origen vacío.

---

## C

### Código de salida
**Categoría:** Shell / Procesos
**Definición:** Número entre 0 y 255 con el que termina todo comando. 0 significa éxito y cualquier otro valor alguna falla. El shell lo guarda en `$?` y lo usan `if`, `&&` y `||` para decidir. Algunos valores tienen significado propio: 126 (no se puede ejecutar), 127 (comando no encontrado) y 128 + n (lo mató la señal n).
**En el curso:** el script viejo de respaldo sale con 0 aunque `tar` falló; el nuevo sale con 1 o 2 y dice por qué.

---

### Comillas dobles
**Categoría:** Bash / Sintaxis
**Definición:** Delimitan un texto en el que sí se expanden variables y sustituciones de comandos, pero cuyo resultado **no** se parte por espacios ni se expande como comodín. Escribir `"$VAR"` en lugar de `$VAR` evita que una ruta con espacios se convierta en varios argumentos.
**En el curso:** ShellCheck marcó con SC2086 y SC2046 las expansiones sin comillas del script viejo.

---

## D

### División en palabras
**Categoría:** Bash / Expansión
**Definición:** Paso en el que el shell, después de expandir una variable o una sustitución sin comillas, parte el resultado en varias palabras usando los caracteres de `IFS` (espacio, tabulador y salto de línea). Es la causa de que `ls $D` con `D="/tmp/mis datos"` busque dos rutas. En inglés se conoce como *word splitting*.

---

## E

### errexit (set -e)
**Categoría:** Bash / Opciones
**Definición:** Opción que termina el script en cuanto un comando sale con código distinto de 0. Tiene huecos conocidos: no aplica dentro de condiciones (`if`, `while`, `&&`, `||`, `!`) ni en lo que corre desde ahí, no ve el error de una sustitución asignada con `local`, `export` o `readonly`, y sin `inherit_errexit` no aplica dentro de `$( )`.
**En el curso:** se combina con `-E`, `-u` y `pipefail`, y cada hueco se demostró en clase.

---

### Expansión de parámetros
**Categoría:** Bash / Expansión
**Definición:** Forma de leer una variable aplicándole una regla. `${VAR:-valor}` usa un valor por defecto si falta o está vacía; `${VAR:=valor}` además lo guarda; `${VAR:?mensaje}` termina el script con ese mensaje si falta o está vacía. A diferencia de `set -u`, la forma `:?` también detecta la variable que existe pero está vacía.
**En el curso:** `respaldo.sh` exige sus dos argumentos con `${1:?uso: respaldo.sh ORIGEN DESTINO}`, y antes de un `rm -rf` se usa `${DIR:?}`.

---

## F

### Función
**Categoría:** Bash / Estructura
**Definición:** Bloque de comandos con nombre que se invoca como un comando más. Recibe argumentos en `$1`, `$2`, ..., termina con `return N` (que solo sale de la función) y entrega datos imprimiéndolos en stdout, para que quien la llama los capture con `$( )`. `exit` dentro de una función termina todo el script.
**En el curso:** `log`, `falla` y `al_salir` son las funciones de `respaldo.sh`.

---

## I

### inherit_errexit
**Categoría:** Bash / Opciones
**Definición:** Opción de Bash 4.4 en adelante (`shopt -s inherit_errexit`) que hace que lo que corre dentro de una sustitución de comandos herede `set -e`. Sin ella, `x=$(false; echo sigue)` deja a `x` con "sigue" y el script continúa.

---

## L

### local
**Categoría:** Bash / Variables
**Definición:** Comando que declara una variable visible solo dentro de la función actual, para no pisar otra con el mismo nombre en el resto del script. Como es un comando, su propio código de salida (0) reemplaza al de lo que se le asigna: `local x=$(comando)` esconde la falla de `comando`. Se declara en una línea y se asigna en otra.

---

## M

### mktemp
**Categoría:** Linux / Comandos
**Definición:** Comando que crea un archivo (o, con `-d`, un directorio) temporal con un nombre único y permisos que solo su dueño puede leer (600 y 700), e imprime su ruta. Evita los choques y las suplantaciones de un nombre fijo como `/tmp/respaldo.tmp`.
**En el curso:** se combina con `trap ... EXIT` para borrar el temporal pase lo que pase.

---

## N

### nounset (set -u)
**Categoría:** Bash / Opciones
**Definición:** Opción que convierte en error el uso de una variable que no existe, en lugar de sustituirla por un texto vacío. En un script, termina con código 1 y el mensaje `unbound variable`. Lo que de verdad es opcional se escribe con valor por defecto: `${NIVEL:-info}`.

---

## P

### Parámetro posicional
**Categoría:** Bash / Variables
**Definición:** Cada argumento con el que se llamó al script o a la función: `$1`, `$2`, y así sucesivamente. `$#` dice cuántos llegaron, `"$@"` los entrega todos como palabras separadas y `shift` descarta el primero y recorre los demás.

---

### pipefail
**Categoría:** Bash / Opciones
**Definición:** Opción (`set -o pipefail`) con la que una tubería termina con el código de la última pieza que falló, en lugar del código del último comando. Sin ella, `pg_dump | gzip > respaldo.gz` sale con 0 aunque `pg_dump` falle, y deja un gzip válido de 20 bytes.

---

### PIPESTATUS
**Categoría:** Bash / Variables
**Definición:** Arreglo que guarda el código de salida de cada comando de la última tubería, en orden. Después de `false | true | gzip > /dev/null`, `echo "${PIPESTATUS[@]}"` imprime `1 0 0`. Sirve para saber cuál pieza falló.

---

### PS4
**Categoría:** Bash / Depuración
**Definición:** Variable con el prefijo que imprime `set -x` antes de cada comando. Con `PS4='+ ${BASH_SOURCE##*/}:${LINENO}: '` la traza muestra el archivo y la línea. Desde Bash 4.4, un shell que corre como root no hereda `PS4` del entorno, así que conviene definirla dentro del script.

---

## S

### Shebang
**Categoría:** Linux / Scripts
**Definición:** Primera línea de un script, que empieza con `#!` y le dice al sistema qué intérprete usar al ejecutarlo directamente. `#!/usr/bin/env bash` busca `bash` en el `PATH`. Sin ella, quien ejecute el archivo decide el intérprete, y el `sh` de Ubuntu (dash) no entiende `[[ ]]`, `(( ))` ni `pipefail`.

---

### ShellCheck
**Categoría:** Herramientas / Análisis estático
**Definición:** Analizador de scripts de shell que señala trampas de escritura conocidas, cada una con un código (por ejemplo SC2086: falta de comillas; SC2155: declarar y asignar en la misma línea). Revisa cómo está escrito el script, no si el script verifica el resultado de lo que hace.
**En el curso:** el script viejo con comillas corregidas pasa ShellCheck y sigue diciendo "Respaldo listo" sin respaldo.

---

### stderr
**Categoría:** Linux / Flujos
**Definición:** Salida de errores estándar (descriptor 2), separada de la salida normal (stdout, descriptor 1). Los mensajes para personas van a stderr con `>&2` para no mezclarse con los datos que otro comando lee por una tubería o que se capturan con `$( )`. Bajo systemd, stderr llega al journal con el nombre de la unit.

---

### Sustitución de comandos
**Categoría:** Bash / Expansión
**Definición:** La forma `$(comando)`, que ejecuta un comando y se reemplaza por lo que imprimió en stdout, sin el salto de línea final. Su código de salida es el del comando, salvo cuando se asigna con `local`, `export` o `readonly`, que lo reemplazan por el suyo.

---

## T

### trap
**Categoría:** Bash / Señales
**Definición:** Comando que registra lo que el shell debe ejecutar al recibir una señal o un evento. `trap funcion EXIT` corre al terminar el script por cualquier motivo; `trap '...' ERR` corre cada vez que un comando falla fuera de una condición y puede reportar `$LINENO` y `$BASH_COMMAND`. Con `set -E`, el `ERR` también se dispara dentro de funciones.
**En el curso:** `respaldo.sh` usa `EXIT` para borrar el `.parcial` y `ERR` para decir en qué línea falló.

---

## V

### Verificar el resultado
**Categoría:** Scripting / Criterio
**Definición:** Comprobar que lo que el script produjo sirve, en lugar de confiar en que los comandos salieron con 0. En un respaldo significa abrir el archivo (`gzip -t`), contar lo que trae (`tar -tzf`) y compararlo con lo que se esperaba. Un gzip de nada pesa 20 bytes y pasa `gzip -t`: solo mirar adentro lo delata.
**En el curso:** es la regla central de la sesión: un script que no verifica su resultado no avisa cuando falla.
