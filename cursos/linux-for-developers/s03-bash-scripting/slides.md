<!-- _class: lead -->

# S03 - Bash que falla en voz alta

### Linux para desarrolladores

**Rogelio Arriaga - Raptor Dev Services**

<!--
Sesión de 5 horas. Hilo conductor: el respaldo de la API de laboratorio. Empieza con un script que todos
reconocen (el que "siempre ha funcionado") y termina con uno que se verifica solo.
Antes de empezar: cada equipo con su VM Ubuntu 24.04, el usuario api-lab de la S02 y ShellCheck instalado
(sudo apt install -y shellcheck).
-->

---

## El lema de hoy

> Un script que sale con 0 después de fallar es peor que no tener script.

Sin script, alguien revisa a mano. Con un script que miente, nadie revisa: todos creen que hay respaldo.

<!--
Pregunta al grupo antes de seguir: ¿cómo saben que el respaldo de anoche funcionó? Casi siempre la respuesta
es "porque no llegó ningún correo de error". Esa es exactamente la trampa de hoy.
-->

---

## Agenda

| Bloque | Qué hacemos |
|---|---|
| 1 | El código de salida es el contrato |
| 2 | `set -Eeuo pipefail` y sus huecos |
| 3 | Comillas, parámetros y funciones |
| 4 | `trap`, temporales y verificar el resultado |
| 5 | Depurar: `bash -n`, `bash -x` y ShellCheck |
| 6 | Práctica guiada: el respaldo que se verifica |
| 7 | Cierre, tarea y lo que sigue |

---

## De dónde venimos

- **S01:** tuberías y herramientas de texto en una línea.
- **S02:** el usuario de servicio `api-lab`, permisos y mínimo privilegio.
- **Hoy:** el respaldo de `/var/lib/api-lab`, escrito para que avise cuando falla.
- **S04:** ese mismo respaldo lo correrá un timer de systemd, sin nadie mirando.

Lo que escriban hoy se ejecuta mañana a las 02:00, cuando nadie lo está viendo.

---

<!-- _class: lead -->

# Parte 1
## El código de salida es el contrato

---

## Cada comando contesta con un número

```bash
true;  echo "$?"     # 0
false; echo "$?"     # 1
ls /no-existe 2>/dev/null; echo "$?"   # 2
```

- `$?` guarda el código de salida del **último** comando.
- **0** es éxito. Cualquier otro valor (1 a 255) es algún tipo de falla.
- El texto que imprime un comando es para personas; el código es para los scripts.

Fuente: Varma, *Pro Bash*, cap. 3.

---

## Códigos que conviene reconocer

| Código | Significa |
|---|---|
| 0 | Terminó bien |
| 1 | Falla general |
| 2 | Uso incorrecto (en muchas herramientas) |
| 126 | El archivo existe pero no se puede ejecutar |
| 127 | Comando no encontrado |
| 128 + n | Lo mató la señal n: 130 es Ctrl+C, 137 es `kill -9`, 143 es `kill` (TERM) |

<!--
Los medimos en el contenedor del curso: 127 con un comando inexistente, 126 con un archivo sin permiso de
ejecución, 143 al mandar TERM a un sleep. El 137 lo van a ver otra vez en la S04 cuando el kernel mate a la
API por memoria.
-->

---

## `grep` tiene tres respuestas, no dos

```bash
grep -q root /etc/passwd;       echo "$?"   # 0: encontró
grep -q zzz  /etc/passwd;       echo "$?"   # 1: no encontró
grep -q x /no-existe 2>/dev/null; echo "$?"   # 2: no pudo buscar
```

"No encontré nada" y "no pude leer el archivo" son respuestas distintas. Un script que las trata igual oculta un error.

---

## `if`, `&&` y `||` leen el código, no la salida

```bash
if grep -q "ERROR" /var/log/app.log; then
  echo "hay errores"
fi

mkdir -p /var/backups/api-lab && echo "listo"
cd /ruta/que/no/existe || exit 1
```

- `if` ejecuta el comando y decide con su código.
- `a && b`: corre `b` solo si `a` salió con 0.
- `a || b`: corre `b` solo si `a` falló.

Fuente: Varma, *Pro Bash*, cap. 3.

---

## Demostración: medir antes de suponer

```console
$ bash -c "comandoquenoexiste"; echo "salida: $?"
bash: line 1: comandoquenoexiste: command not found
salida: 127
$ touch /tmp/noexec; bash -c /tmp/noexec; echo "salida: $?"
bash: line 1: /tmp/noexec: Permission denied
salida: 126
$ bash -c 'sleep 30 & p=$!; kill -TERM $p; wait $p; echo "salida: $?"'
salida: 143
```

<!--
Demostración en vivo en la VM del instructor. Pide que cada equipo prediga el número antes de presionar
Enter. Quien falla la predicción de 143 entiende de golpe el "128 + n".
-->

---

## En una tubería manda el último

```console
$ false | true; echo "$?"
0
$ false | true | gzip > /dev/null; echo "${PIPESTATUS[@]}"
1 0 0
```

- Sin más opciones, el código de `a | b` es el de `b`.
- `PIPESTATUS` guarda el código de cada pieza de la última tubería.
- Si la primera pieza falla, nadie se entera... salvo que se lo pidan al shell.

Fuente: Barrett, *Efficient Linux at the Command Line*, cap. 1 (repaso de la S01).

---

## Caso real: el runner que siempre estaba ocupado

En la limpieza nocturna de los runners de CI del dev-stack del instructor, el script preguntaba si cada runner tenía un job corriendo con una línea de esta forma:

```bash
n=$(docker exec "$c" ps -eo args | grep -c "[R]unner.Worker" || echo 0)
```

- Sin coincidencias, `grep -c` **imprime 0 y sale con 1**.
- El `|| echo 0` agrega **otro** 0: la variable queda con `0` y `0` en dos líneas.
- `"0\n0"` nunca es igual a `"0"`: **todo runner libre parecía ocupado** y la limpieza no limpiaba nada.

<!--
La corrección fue preguntar por el código de salida de grep -q, no por su conteo. Muéstrales que nadie vio
un error: el script terminaba bien todas las noches. Solo el disco que seguía creciendo delató el problema.
-->

---

## Lo medimos

```console
$ printf "a\nb\n" > f
$ n=$(grep -c zzz f || echo 0); printf "[%s]\n" "$n"
[0
0]
$ grep -c zzz f; echo "salida de grep -c: $?"
0
salida de grep -c: 1
```

La forma que no miente: preguntar por el código, no por el texto.

```bash
if docker exec "$c" ps -eo args | grep -q "[R]unner.Worker"; then echo "ocupado"; fi
```

---

<!-- _class: lead -->

# Parte 2
## set -Eeuo pipefail y sus huecos

---

## Las cuatro banderas

| Bandera | Qué hace |
|---|---|
| `-e` | Termina el script en cuanto un comando falla |
| `-E` | Hace que `trap ... ERR` también funcione dentro de funciones |
| `-u` | Usar una variable que no existe es un error, no un texto vacío |
| `-o pipefail` | Una tubería falla si falla **cualquiera** de sus piezas |

```bash
#!/usr/bin/env bash
set -Eeuo pipefail
shopt -s inherit_errexit   # bash 4.4 o más nuevo: -e también dentro de $( )
```

Fuente: Varma, *Pro Bash*, cap. 10.

---

## pipefail: el respaldo de 20 bytes

```console
$ false | gzip > x.gz; echo "sin pipefail: $?"; wc -c < x.gz
sin pipefail: 0
20
$ set -o pipefail; false | gzip > x.gz; echo "con pipefail: $?"
con pipefail: 1
```

Cambien `false` por `pg_dump` con la contraseña equivocada: el volcado falla, `gzip` comprime **nada**, el archivo pesa 20 bytes y el script sale con 0.

<!--
Este es el caso más común en la vida real. El archivo "existe" y "no está vacío": pasa cualquier
revisión ingenua. Lo retomamos en la Parte 4.
-->

---

## `-u`: la variable que no existe

```bash
#!/usr/bin/env bash
set -u
echo "Respaldando ${ORIGEN}"     # ORIGEN no existe
```

```console
$ bash u.sh; echo "salida: $?"
u.sh: line 3: ORIGEN: unbound variable
salida: 1
```

- Sin `-u`, `${ORIGEN}` vale vacío y el script sigue con una ruta incompleta.
- Para lo que **sí** es opcional, digan el valor por defecto: `"${NIVEL:-info}"`.

---

## Hueco 1: `-e` no aplica dentro de condiciones

```console
$ bash -c 'set -e; h() { false; echo sigo-dentro-de-h; }; if h; then echo h-ok; fi; echo fin'
sigo-dentro-de-h
h-ok
fin
$ bash -c 'set -e; h() { false; echo sigo-dentro-de-h; }; h; echo fin'; echo "salida: $?"
salida: 1
```

La misma función se comporta distinto según **dónde** la llamen. Dentro de `if`, `while`, `&&`, `||` o `!`, `-e` se apaga para todo lo que corre ahí.

---

## Hueco 2: `local x=$(...)` se traga el error

```console
$ f() { local x=$(false); echo "código tras local: $?"; }; f
código tras local: 0
$ g() { local x; x=$(false); echo "código separado: $?"; }; g
código separado: 1
```

- `local` es un comando, y **su** código es 0: el de `false` se pierde.
- Lo mismo pasa con `export x=$(...)` y `readonly x=$(...)`.
- Declaren en una línea y asignen en otra. ShellCheck lo marca como SC2155.

---

## Hueco 3: sustitución de comandos

```console
$ bash -c 'set -e; x=$(false; echo sigue); echo "x=$x"'
x=sigue
$ bash -c 'set -e; shopt -s inherit_errexit; x=$(false; echo sigue); echo "x=$x"'; echo "salida: $?"
salida: 1
```

Sin `inherit_errexit`, lo que corre dentro de `$( )` no hereda `-e`: el `false` pasa y el script sigue con un valor a medias.

---

## Hueco 4: `|| true` es una decisión

```bash
rm -f /tmp/api-lab.lock                 # -f ya no falla si no existe: mejor que || true
docker stop api-lab 2>/dev/null || true # aceptable: "si no estaba corriendo, da igual"
pg_dump "$BASE" > volcado.sql || true   # NO: aquí se esconde exactamente lo que importa
```

- `|| true` le dice al lector: "si esto falla, **a propósito** no me importa".
- Si no pueden escribir en un comentario por qué no importa, no lo pongan.

---

## Momento de práctica: predecir y medir (15 min)

En su VM, **antes** de presionar Enter, escriban en papel el código de salida que esperan:

```bash
false | true; echo $?
set -o pipefail; false | true; echo $?; set +o pipefail
bash -c 'set -e; f() { false; echo dentro; }; f || echo rescate'
bash -c 'set -eu; echo "${NOEXISTE:-ok}"'
bash -c 'set -e; v() { local r=$(ls /no-existe); }; v; echo "pasó"'
```

Comparen con su compañero. La que más fallen es la que van a recordar.

<!--
Respuestas medidas en el contenedor del curso, en orden: 0; 1; imprime "dentro" y NO imprime "rescate",
porque -e se apaga en todo lo que corre a la izquierda de || y la función termina bien con su echo;
imprime "ok"; y la última imprime el error de ls y después "pasó", por el hueco de local.
Deja que discutan antes de dar la respuesta.
-->

---

<!-- _class: lead -->

# Parte 3
## Comillas, parámetros y funciones

---

## Sin comillas, el shell parte por espacios

```console
$ D="/tmp/mis datos"
$ ls $D
ls: cannot access '/tmp/mis': No such file or directory
ls: cannot access 'datos': No such file or directory
$ ls "$D"
a.txt
```

- Después de expandir `$D`, el shell **parte** el resultado por espacios y expande comodines.
- Regla de bolsillo: toda expansión va entre comillas dobles, salvo que quieran partirla a propósito.

Fuente: Varma, *Pro Bash*, cap. 4.

---

## `"$@"` conserva los argumentos tal cual

```console
$ f() { for a in "$@"; do echo "[$a]"; done; }
$ f "uno dos" tres
[uno dos]
[tres]
$ g() { for a in $*; do echo "[$a]"; done; }
$ g "uno dos" tres
[uno]
[dos]
[tres]
```

Para pasar los argumentos a otro comando: siempre `"$@"`.

---

## `${DIR:?}` antes de cualquier `rm -rf`

```bash
rm -rf "$DIR/"*            # si DIR está vacío, esto es rm -rf /*
rm -rf "${DIR:?DIR vacío}/"*   # si DIR está vacío o no existe, el script se detiene aquí
```

- En 2015, el instalador de un cliente de juegos para Linux borró el home de algunos usuarios con una línea así: la variable llegó vacía.
- `${VAR:?mensaje}` termina el script con ese mensaje si la variable está vacía o no existe.

<!--
No hace falta nombrar el producto: la lección es que una variable vacía convierte una ruta en la raíz.
-->

---

## Parámetros y valores por defecto

| Expresión | Qué hace |
|---|---|
| `$1`, `$2`, ... | Argumentos posicionales |
| `$#` | Cuántos argumentos llegaron |
| `"$@"` | Todos, cada uno como palabra aparte |
| `${1:?uso: respaldo.sh ORIGEN DESTINO}` | Obligatorio: si falta, termina con ese mensaje |
| `${NIVEL:-info}` | Si falta o está vacío, usa `info` (no lo guarda) |
| `${NIVEL:=info}` | Igual, y además lo guarda en la variable |
| `shift` | Descarta `$1` y recorre los demás |

Fuente: Varma, *Pro Bash*, cap. 5.

---

## Funciones: `local`, `return` y `exit`

```console
$ bash -c 'h() { return 3; }; h; echo "h devolvió $?"; echo sigue'
h devolvió 3
sigue
$ bash -c 'h() { exit 3; }; h; echo nunca'; echo "salida: $?"
salida: 3
```

- `return` sale de la **función** con un código; `exit` termina **todo el script**.
- `local` evita que una variable de la función pise otra del script.
- Una función "devuelve" datos imprimiéndolos en stdout: `total=$(contar "$dir")`.

Fuente: Varma, *Pro Bash*, cap. 6.

---

## Mensajes a stderr, datos a stdout

```bash
log()   { printf '%s respaldo: %s\n' "$(date '+%F %T')" "$*" >&2; }
falla() { log "ERROR: $*"; exit 1; }

log "respaldando $ORIGEN"
[[ -d "$ORIGEN" ]] || falla "el origen $ORIGEN no existe"
```

- Si los mensajes salen por stdout, se mezclan con los datos que otro comando lee por la tubería.
- Bajo systemd (S04), stderr llega al journal con el nombre de la unit: no hace falta un archivo de log propio.

---

## Momento de práctica: ShellCheck contra el script viejo (15 min)

```bash
sudo apt install -y shellcheck
shellcheck respaldo-viejo.sh
```

1. Lean cada aviso y su código (SC2086, SC2046...).
2. Corrijan solo las comillas y vuelvan a correrlo hasta que no diga nada.
3. Pregunta para el grupo: ahora que ShellCheck está contento, ¿el script ya avisa si falla?

<!--
La respuesta es no. ShellCheck revisa cómo está escrito, no si el script revisa lo que hace. Ese es el
puente a la Parte 4. En el contenedor del curso, el script viejo dio SC2086 dos veces y SC2046 una vez.
-->

---

<!-- _class: lead -->

# Parte 4
## trap, temporales y verificar el resultado

---

## `trap EXIT`: limpiar pase lo que pase

```bash
al_salir() {
  local codigo=$?
  if (( codigo != 0 )); then
    rm -f -- "$ARCHIVO.parcial"
    log "terminó con código $codigo; no quedó ningún respaldo nuevo"
  fi
}
trap al_salir EXIT
```

- `EXIT` corre al terminar el script: por `exit`, por `-e` o al llegar al final.
- Lo primero que hace es guardar `$?`: es el código con el que el script iba a salir.

---

## `trap ERR`: decir dónde falló

```bash
#!/usr/bin/env bash
set -Eeuo pipefail
trap 'echo "falló la línea $LINENO: $BASH_COMMAND (código $?)" >&2' ERR
echo "paso 1"
cp /etc/no-existe /tmp/
echo "paso 2"
```

```console
$ bash err.sh; echo "salida: $?"
paso 1
cp: cannot stat '/etc/no-existe': No such file or directory
falló la línea 5: cp /etc/no-existe /tmp/ (código 1)
salida: 1
```

---

## `mktemp`: temporales que nadie más pisa

```bash
tmp=$(mktemp)                 # /tmp/tmp.XXXXXXXXXX, permisos 600
dir=$(mktemp -d)              # un directorio, permisos 700
trap 'rm -rf -- "$dir"' EXIT  # se borra al salir, falle o no
```

- `/tmp/respaldo.tmp` con nombre fijo choca si dos copias corren a la vez, y otro usuario puede crearlo antes que ustedes.
- `mktemp` crea un nombre único con permisos que solo ustedes pueden leer.

---

## Escribir a medias, publicar entero

```bash
tar -czf "$ARCHIVO.parcial" -C "$ORIGEN" .
# ... verificaciones ...
mv -- "$ARCHIVO.parcial" "$ARCHIVO"
```

- Mientras el respaldo se escribe, se llama `.parcial`: nadie lo confunde con uno bueno.
- `mv` dentro del mismo sistema de archivos es un solo paso: el archivo final aparece completo o no aparece.
- Si algo falla antes, `trap EXIT` borra el `.parcial`.

---

## Verificar el resultado, no el código

```bash
gzip -t "$ARCHIVO.parcial" || falla "el archivo comprimido está corrupto"
encontrados=$(tar -tzf "$ARCHIVO.parcial" | grep -vc '/$') || falla "el respaldo salió sin archivos"
(( encontrados == esperados )) || falla "se esperaban $esperados archivos y trae $encontrados"
```

Que `tar` saliera con 0 dice que `tar` cree que terminó. Lo que importa es si **el archivo** sirve para restaurar: se abre, trae archivos y trae los que esperaban.

Fuente: Varma, *Pro Bash*, cap. 10.

---

## Por qué "no está vacío" no basta

```console
$ printf "" | gzip > v.gz; wc -c < v.gz
20
$ gzip -t v.gz && echo "gzip -t dice que está bien"
gzip -t dice que está bien
$ zcat v.gz | wc -c
0
```

Un gzip de nada pesa 20 bytes y es un gzip válido. `[[ -s archivo ]]` lo da por bueno. Hay que mirar **adentro**.

---

<!-- _class: lead -->

# Parte 5
## Depurar: bash -n, bash -x y ShellCheck

---

## `bash -n`: revisar sin ejecutar

```console
$ printf 'if true; then\n echo hola\n' > roto.sh
$ bash -n roto.sh; echo "bash -n: $?"
roto.sh: line 3: syntax error: unexpected end of file
bash -n: 2
```

Lee el script completo y reporta errores de sintaxis sin correr nada. Útil antes de dejarlo en un servidor donde se va a ejecutar de madrugada.

---

## `bash -x` y `PS4`: ver lo que de verdad corre

```bash
#!/usr/bin/env bash
set -Eeuo pipefail
PS4='+ ${BASH_SOURCE##*/}:${LINENO}: '
[[ "${DEPURAR:-0}" == 1 ]] && set -x
```

```console
$ DEPURAR=1 bash traza.sh /tmp/o
+ traza.sh:5: origen=/tmp/o
++ traza.sh:6: find /tmp/o -type f
++ traza.sh:6: wc -l
archivos: 1
```

Bash ignora un `PS4` que llega del entorno cuando corre como root: defínanlo dentro del script.

Fuente: Varma, *Pro Bash*, cap. 10.

---

## ShellCheck: el revisor que no se cansa

```console
$ shellcheck respaldo-viejo.sh
tar -czf $DESTINO/respaldo-$(date +%Y%m%d).tar.gz -C $ORIGEN .
         ^------^ SC2086 (info): Double quote to prevent globbing and word splitting.
                           ^-------------^ SC2046 (warning): Quote this to prevent word splitting.
```

- Encuentra comillas que faltan, `local x=$(...)`, variables sin usar y decenas de trampas.
- **No** sabe si su script verifica lo que hace: el viejo, con comillas, sigue diciendo "Respaldo listo" sin respaldo.

---

## Plantilla para empezar cualquier script

```bash
#!/usr/bin/env bash
# nombre.sh - qué hace, en una línea. Uso: nombre.sh ARG1 ARG2
set -Eeuo pipefail
shopt -s inherit_errexit

readonly ARG1="${1:?uso: nombre.sh ARG1 ARG2}"

log()   { printf '%s nombre: %s\n' "$(date '+%F %T')" "$*" >&2; }
falla() { log "ERROR: $*"; exit 1; }
trap 'log "falló la línea $LINENO: $BASH_COMMAND"' ERR

# 1. revisar entradas   2. hacer el trabajo   3. verificar el resultado   4. avisar
```

---

<!-- _class: lead -->

# Parte 6
## Práctica guiada: el respaldo que se verifica

---

## Paso 1: datos de prueba y el script viejo

```bash
sudo install -d -o api-lab -g api-lab /var/lib/api-lab /var/lib/api-lab/tmp
printf '{"pedido": 1, "total": 350}\n' | sudo -u api-lab tee /var/lib/api-lab/pedidos.json > /dev/null
mkdir -p ~/lab-s03/vacio ~/lab-s03/respaldos
```

Copien `respaldo-viejo.sh` de la guía de práctica y córranlo con datos buenos:

```console
$ ./respaldo-viejo.sh /var/lib/api-lab ~/lab-s03/respaldos; echo "salida: $?"
Respaldo listo
salida: 0
```

<!--
Guía completa en practica.md de esta sesión. Aquí solo el recorrido. Den 10 minutos para preparar datos.
-->

---

## Paso 2: verlo fallar (el script viejo)

```console
$ ./respaldo-viejo.sh /var/lib/api-lab-que-no-existe ~/lab-s03/respaldos; echo "salida: $?"
tar: /var/lib/api-lab-que-no-existe: Cannot open: No such file or directory
tar: Error is not recoverable: exiting now
Respaldo listo
salida: 0
$ ls -l ~/lab-s03/respaldos
-rw-r--r-- 1 alumno alumno 20 Nov  9 04:07 respaldo-20261109.tar.gz
```

- Dice "Respaldo listo", sale con 0 y deja un archivo de 20 bytes.
- Peor: el nombre solo lleva la fecha. Si corrió bien a las 02:00 y alguien lo vuelve a correr con un error, **el bueno se sobrescribe** con el de 20 bytes.

---

## Paso 3: el script nuevo, por bloques

1. `set -Eeuo pipefail` e `inherit_errexit`.
2. `ORIGEN` y `DESTINO` obligatorios con `${1:?uso}`.
3. `log` y `falla` escriben a stderr.
4. `trap EXIT` borra el `.parcial` si algo falla; `trap ERR` dice la línea.
5. Revisar entradas: el origen existe y **tiene archivos**.
6. `tar` a un `.parcial` con fecha y hora en el nombre.
7. Verificar: `gzip -t`, contar con `tar -tzf`, comparar.
8. `mv` al nombre final y avisar qué quedó.

Escríbanlo en `/usr/local/bin/respaldo.sh`, con `bash -n` y ShellCheck limpios antes de correrlo.

---

## Paso 4: el caso bueno

```console
$ sudo respaldo.sh /var/lib/api-lab /var/backups/api-lab; echo "salida: $?"
2026-11-09 04:07:35 respaldo: respaldando 3 archivos de /var/lib/api-lab
2026-11-09 04:07:35 respaldo: listo: /var/backups/api-lab/respaldo-20261109-040735.tar.gz (4.0K, 3 archivos)
salida: 0
$ sudo tar -tzf /var/backups/api-lab/respaldo-*.tar.gz
./
./pedidos.json
./clientes.csv
./tmp/
./tmp/sesion-1.json
```

---

## Paso 5: verlo fallar (el script nuevo)

| Caso | Qué dice | Código | ¿Deja archivo? |
|---|---|---|---|
| Sin argumentos | `uso: respaldo.sh ORIGEN DESTINO` | 1 | No |
| Origen inexistente | `ERROR: el origen ... no existe` | 1 | No |
| Origen vacío | `ERROR: el origen ... está vacío` | 1 | No |
| Sin permiso en el destino (sin sudo) | `falló la línea 33: tar ...` | 2 | No |

Los cuatro casos que el viejo daba por buenos ahora terminan con error, dicen por qué y no dejan nada que parezca un respaldo.

<!--
Pide que cada equipo provoque los cuatro y los copie a su evidencia. Si un caso no falla como dice la
tabla, ese es el mejor momento de la sesión: hay que encontrar por qué antes de seguir.
-->

---

## Paso 6: rastrear una falla con `bash -x`

```console
$ sudo bash -x /usr/local/bin/respaldo.sh ~/lab-s03/vacio /var/backups/api-lab 2>&1 | tail -4
+ local codigo=1
+ ((  codigo != 0  ))
+ rm -f -- /var/backups/api-lab/respaldo-20261109-040735.tar.gz.parcial
+ log 'terminó con código 1; no quedó ningún respaldo nuevo'
```

Con `-x` ven el camino exacto: qué condición falló, qué limpió `trap` y con qué código salió.

---

## Evidencia que entregan

- `respaldo.sh` final, con `bash -n` y `shellcheck` sin avisos.
- La salida del caso bueno y el listado de `tar -tzf`.
- La salida y el código de los **cuatro** casos de falla, del viejo y del nuevo.
- Una línea por caso: qué habría pasado en producción con el script viejo.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Al terminar puedes

Escribir scripts que nadie tiene que vigilar, porque avisan cuando algo sale mal.

- Leer y usar códigos de salida en lugar de texto.
- Usar `set -Eeuo pipefail` sabiendo dónde no protege.
- Citar, validar parámetros y separar mensajes de datos.
- Limpiar con `trap` y verificar el resultado antes de darlo por bueno.
- Depurar con `bash -n`, `bash -x` y ShellCheck.

---

## Tarea

**Reescribir uno de sus scripts viejos** (de un proyecto, del trabajo o de la escuela) y mostrar el caso que antes pasaba en silencio.

- El script antes y después.
- El caso que fallaba sin avisar, ejecutado con los dos: la salida y el código de cada uno.
- ShellCheck sin avisos en la versión nueva.

Instrucciones completas en la tarea de la sesión.

---

## Lecturas y libros

- `dev-notes/10-cloud/19-bash-scripting.md`: la nota de hoy, completa.
- Varma, *Pro Bash* (3.ª ed.): caps. 3 (ciclos y condiciones), 4 (análisis y expansión de la línea de comandos), 5 (parámetros y variables), 6 (funciones), 10 (scripts sin errores y depuración) y 13 (procesar datos).
- Barrett, *Efficient Linux at the Command Line* (2022): caps. 8 (comandos de una línea) y 9 (aprovechar archivos de texto).

---

## Siguiente sesión: servicios con systemd

> Si su aplicación no vuelve sola después de reiniciar el servidor, no está desplegada.

- La API de laboratorio deja de correr en una terminal y pasa a ser un servicio.
- Reinicio automático, límite de memoria y endurecimiento medido.
- Su `respaldo.sh` de hoy lo correrá un **timer**, y el journal dirá si falló.

Traigan `respaldo.sh` funcionando: es la entrada de la tarea de la S04.
