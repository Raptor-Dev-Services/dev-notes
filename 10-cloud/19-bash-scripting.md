# 19 - Bash que falla en voz alta

> Fuente: *Pro Bash*, 3.ª ed. (Varma, Apress): caps. 3 Looping and Branching, 4 Command-Line Parsing and Expansion, 5 Parameters and Variables, 6 Shell Functions, 10 Writing Bug-Free Scripts and Debugging the Rest y 13 Data Processing; *Efficient Linux at the Command Line* (Barrett, O'Reilly, 2022): caps. 8 y 9

## Problema que resuelve

Bash, por omisión, **sigue adelante** cuando un comando falla. Un respaldo cuyo `tar` no encontró el origen imprime "Respaldo listo", sale con 0, y el cron o el timer lo registran como éxito. Nadie se entera hasta el día que hace falta restaurar. Esta nota es para escribir scripts que se detienen en el primer error, dicen dónde fue, no dejan basura y comprueban su propio resultado.

La sección "Scripting Bash esencial" de [03-linux-nginx.md](03-linux-nginx.md) da la sintaxis básica; aquí se trata de que el script no mienta.

---

## El código de salida es el contrato

Todo comando termina con un número de 0 a 255 que queda en `$?`. **0 es éxito; cualquier otro es fallo.** `if`, `&&`, `||`, `while` y `set -e` solo miran ese número: no leen lo que el comando imprimió.

| Código | Significado habitual |
|--------|----------------------|
| 0 | Éxito |
| 1 | Error general |
| 2 | Uso incorrecto (argumentos), o error de sintaxis de `bash -n` |
| 126 | El archivo existe pero no tiene permiso de ejecución |
| 127 | Comando no encontrado |
| 128 + n | Lo mató la señal n: 137 = 128 + 9 (KILL), 143 = 128 + 15 (TERM) |

```bash
grep -q 'ERROR' app.log && echo "hay errores"    # && solo corre si el anterior salió con 0
cd /srv/app || exit 1                             # || solo corre si el anterior falló

# Ojo: algunos programas usan el código para algo más que "falló"
grep -c zzz /etc/hosts      # imprime 0 Y sale con 1: "no hubo coincidencias" es un 1
diff a.txt b.txt            # 1 significa "son distintos", 2 significa "hubo un error"
```

Tu script también es un comando: termina con `exit 0` cuando hizo su trabajo y con otro número cuando no. Si no pones `exit`, sale con el código del último comando, y ese suele ser un `echo` que siempre sale bien.

---

## set -Eeuo pipefail, y dónde no te protege

```bash
#!/usr/bin/env bash
set -Eeuo pipefail
shopt -s inherit_errexit
```

| Opción | Qué hace | Sin ella |
|--------|----------|----------|
| `-e` | Sale en cuanto un comando falla | El script sigue con datos rotos |
| `-u` | Una variable sin definir es error (el script sale con 1) | `rm -rf "$DIR/"*` con `DIR` vacío borra desde `/` |
| `-o pipefail` | Una tubería falla si falla cualquiera de sus partes | Solo cuenta el último comando de la tubería |
| `-E` | El `trap ... ERR` también se dispara dentro de funciones | El trap ERR nunca avisa de un fallo dentro de una función |
| `inherit_errexit` | `$(...)` también hereda `-e` | Un fallo dentro de la sustitución se ignora |

El caso clásico de `pipefail`: el volcado falla y el compresor, que sí terminó bien, decide el resultado.

```bash
false | gzip > respaldo.sql.gz ; echo $?   # 0, y respaldo.sql.gz pesa 20 bytes: un gzip de nada
set -o pipefail
false | gzip > respaldo.sql.gz ; echo $?   # 1
# En la vida real "false" es pg_dump sin conexión o sin permisos.
```

`set -e` tiene huecos conocidos. Conócelos en lugar de confiar a ciegas:

```bash
# 1. Dentro de una condición, -e se apaga: aquí h falla a la mitad y nadie se entera
h() { false; echo "sigo dentro de h"; }
if h; then echo "h salió bien"; fi        # imprime las dos líneas

# 2. local enmascara el código de la sustitución
f() { local x=$(false); echo $?; }        # imprime 0: es el código de "local", no de false
g() { local x; x=$(false); echo $?; }     # con set -e, aquí el script sale; sin él, imprime 1

# 3. || true y ! apagan el error a propósito: úsalos solo cuando el fallo de verdad no importa
docker stop viejo 2>/dev/null || true
```

---

## Comillas y expansión

Regla: **toda expansión va entre comillas dobles**, salvo que quieras explícitamente que se parta o que expanda comodines.

```bash
archivo="reporte final.csv"
wc -l $archivo          # wc recibe "reporte" y "final.csv": dos errores
wc -l "$archivo"        # bien

# "$@" pasa cada argumento tal cual; $* y $@ sin comillas los vuelven a partir
f() { for a in "$@"; do echo "[$a]"; done; }
f "uno dos" tres        # [uno dos] [tres]

# Listas: arreglos, no cadenas con espacios
dirs=(/var/lib/api "/srv/datos compartidos")
tar -czf respaldo.tar.gz "${dirs[@]}"

# Antes de borrar con una variable, exige que tenga valor
rm -rf -- "${DIR:?DIR está vacío}"/*     # si DIR está vacío o sin definir, el script sale con error
```

`--` le dice al comando que lo que sigue ya no son opciones: un archivo llamado `-rf` no se convierte en bandera.

---

## Parámetros y valores por defecto

| Expansión | Qué da |
|-----------|--------|
| `$1`, `$2`... | Argumentos posicionales |
| `$#` | Cuántos argumentos llegaron |
| `"$@"` | Todos, cada uno como palabra aparte |
| `$0` | Cómo se invocó el script; `${0##*/}` es solo el nombre |
| `${VAR:-valor}` | `valor` si `VAR` está vacía o sin definir (no la cambia) |
| `${VAR:=valor}` | Igual, y además la asigna |
| `${VAR:?mensaje}` | Si está vacía o sin definir, imprime el mensaje y el script sale con error |
| `${VAR%.gz}`, `${VAR#*/}` | Quita un sufijo o un prefijo |

```bash
readonly ORIGEN="${1:?uso: respaldo.sh ORIGEN DESTINO}"   # obligatorio, con mensaje de uso
RETENER_DIAS="${RETENER_DIAS:-7}"                           # opcional, configurable desde el entorno

# Opciones cortas con getopts
SECO=false
while getopts ':n' opcion; do
  case "$opcion" in
    n) SECO=true ;;
    *) echo "uso: $0 [-n] ARCHIVO" >&2; exit 2 ;;
  esac
done
shift $((OPTIND - 1))      # lo que queda en "$@" son los argumentos que no son opciones
```

---

## Funciones

```bash
# Los mensajes van a stderr: stdout queda libre para los datos que otro comando quiera leer
log()   { printf '%s %s\n' "$(date '+%F %T')" "$*" >&2; }
falla() { log "ERROR: $*"; exit 1; }

# return termina la función con un código; exit termina TODO el script
existe_usuario() {
  local usuario=$1            # local: la variable no se escapa al resto del script
  id "$usuario" >/dev/null 2>&1
}

if existe_usuario api; then log "el usuario ya existe"; fi

# Una función "devuelve" texto imprimiéndolo, y el código diciendo si salió bien
espacio_libre_mb() { df -Pm -- "$1" | awk 'NR == 2 {print $4}'; }
libre=$(espacio_libre_mb /var/backups)
(( libre > 500 )) || falla "quedan $libre MB en /var/backups"
```

---

## trap: limpiar al salir y avisar dónde falló

`trap` registra lo que se corre cuando el script recibe una señal o llega a un evento. Los dos que importan:

```bash
TMP=$(mktemp -d)                      # carpeta temporal con nombre único y permisos 700
trap 'rm -rf -- "$TMP"' EXIT          # EXIT: siempre, salga bien, salga mal o con Ctrl-C

# ERR: cada vez que un comando falla (con -E, también dentro de funciones)
trap 'echo "falló la línea $LINENO: $BASH_COMMAND" >&2' ERR
```

Patrón para no dejar un resultado a medias que parezca bueno: escribir con otro nombre y renombrar al final.

```bash
al_salir() {
  local codigo=$?                     # el código con el que el script está saliendo
  if (( codigo != 0 )); then
    rm -f -- "$ARCHIVO.parcial"
    log "terminó con código $codigo; no quedó ningún respaldo nuevo"
  fi
}
trap al_salir EXIT

tar -czf "$ARCHIVO.parcial" -C "$ORIGEN" .
# ... verificar ...
mv -- "$ARCHIVO.parcial" "$ARCHIVO"   # mv dentro del mismo sistema de archivos es atómico
```

---

## Verificar el resultado, no el código

Que `tar` salga con 0 no prueba que el respaldo sirva. Un script serio comprueba **lo que produjo**:

| Comprobación | Atrapa |
|--------------|--------|
| El archivo existe y no está vacío (`[[ -s f ]]`) | Que no se haya escrito nada |
| `gzip -t f.gz` | Un comprimido truncado o corrupto |
| Contenido real (`tar -tzf`, `zcat \| head`, contar filas) | Un gzip válido **de nada**: `printf '' \| gzip` pesa 20 bytes y `gzip -t` lo da por bueno |
| Comparar con lo esperado (archivos, filas, tamaño mínimo) | Un respaldo parcial |
| Restaurar en un lugar de prueba | Todo lo anterior y lo que no se te ocurrió |

```bash
esperados=$(find "$ORIGEN" ! -type d | wc -l)
(( esperados > 0 )) || falla "el origen $ORIGEN está vacío: no hay nada que respaldar"

tar -czf "$ARCHIVO.parcial" -C "$ORIGEN" .

gzip -t "$ARCHIVO.parcial" || falla "el archivo comprimido está corrupto"
encontrados=$(tar -tzf "$ARCHIVO.parcial" | grep -vc '/$') || falla "el respaldo salió sin archivos"
(( encontrados == esperados )) || falla "se esperaban $esperados archivos y el respaldo trae $encontrados"
```

Fíjate en `grep -vc ... || falla`: aprovecha que `grep -c` sale con 1 cuando cuenta 0. El mismo detalle, usado al revés, produce errores difíciles de ver:

```bash
# Mal: grep -c imprime 0 Y sale con 1, así que también corre el echo
n=$(grep -c 'Runner.Worker' procesos.txt || echo 0)
printf '[%s]\n' "$n"      # [0
                          # 0]   dos líneas: n nunca es igual a "0"
# Bien: decide por el código de salida, no por el conteo
if grep -q 'Runner.Worker' procesos.txt; then echo ocupado; fi
```

> **Caso real.** Ese `|| echo 0` estaba en la limpieza nocturna de los runners de CI del dev-stack del autor: con un runner libre, `n` valía "0\n0", nunca era igual a "0", y **todos los runners libres parecían ocupados**. El script los dejaba "para el siguiente intento" y terminaba sin haber limpiado ninguno, sin un solo error. El arreglo fue decidir con `grep -q` y su código de salida, como arriba.

---

## Depurar: bash -n, bash -x y ShellCheck

```bash
bash -n script.sh          # solo revisa la sintaxis, no ejecuta nada
# script.sh: line 3: syntax error: unexpected end of file    (sale con 2)

bash -x script.sh          # imprime cada comando ya expandido antes de correrlo
set -x ... set +x          # lo mismo, solo en un tramo del script

# Con archivo y línea en cada traza. Ponlo dentro del script: como root, Bash ignora el PS4 del entorno
PS4='+ ${BASH_SOURCE}:${LINENO}: '
set -x
# + /usr/local/bin/respaldo.sh:4: origen=/var/lib/api-lab
```

**ShellCheck** (`sudo apt install shellcheck`) encuentra antes de correr la mayoría de los errores de esta nota: variables sin comillas (SC2086), `$(...)` sin comillas (SC2046), `local x=$(...)` (SC2155), `cd` sin `|| exit` (SC2164). Sale con 1 si encontró algo, así que sirve como puerta en CI.

```bash
shellcheck respaldo-viejo.sh
# tar -czf $DESTINO/respaldo-$(date +%Y%m%d).tar.gz -C $ORIGEN .
#          ^------^ SC2086 (info): Double quote to prevent globbing and word splitting.
```

---

## Plantilla de script

Probada con ShellCheck sin avisos. Sustituye el trabajo del final.

```bash
#!/usr/bin/env bash
# contar-lineas.sh - Cuenta las líneas de un archivo y lo deja en el log.
# Uso: contar-lineas.sh [-n] ARCHIVO
#   -n   en seco: dice qué haría, sin hacerlo
set -Eeuo pipefail
shopt -s inherit_errexit

NOMBRE="${0##*/}"
readonly NOMBRE
SECO=false

log()   { printf '%s %s: %s\n' "$(date '+%F %T')" "$NOMBRE" "$*" >&2; }
falla() { log "ERROR: $*"; exit 1; }
uso()   { sed -n '3,4s/^# //p' "$0" >&2; exit 2; }     # imprime las líneas de uso de arriba

TMP=$(mktemp -d)
trap 'rm -rf -- "$TMP"' EXIT
trap 'log "falló la línea $LINENO: $BASH_COMMAND"' ERR

while getopts ':n' opcion; do
  case "$opcion" in
    n) SECO=true ;;
    *) uso ;;
  esac
done
shift $((OPTIND - 1))
(( $# == 1 )) || uso
readonly ARCHIVO="$1"

[[ -r "$ARCHIVO" ]] || falla "no puedo leer $ARCHIVO"

if $SECO; then
  log "en seco: contaría las líneas de $ARCHIVO"
  exit 0
fi

lineas=$(wc -l < "$ARCHIVO")
log "listo: $ARCHIVO tiene $lineas líneas"
```

| Invocación | Salida | Código |
|------------|--------|--------|
| `contar-lineas.sh` | Las líneas de uso | 2 |
| `contar-lineas.sh /noexiste` | `ERROR: no puedo leer /noexiste` | 1 |
| `contar-lineas.sh -n /etc/passwd` | `en seco: contaría las líneas de /etc/passwd` | 0 |
| `contar-lineas.sh /etc/passwd` | `listo: /etc/passwd tiene 24 líneas` | 0 |

Si el script va a correr desde un timer de systemd (ver [20-systemd.md](20-systemd.md)), todo lo que escriba en stderr llega al journal, y el código de salida decide si la corrida aparece como fallida.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Código de salida | Número de 0 a 255 con el que termina un proceso; 0 es éxito y lo demás es fallo |
| `$?` | Variable con el código de salida del último comando |
| `set -e` (errexit) | Opción que termina el script en cuanto un comando falla, salvo dentro de condiciones |
| `set -u` (nounset) | Opción que convierte en error usar una variable sin definir |
| pipefail | Opción que hace fallar una tubería si falla cualquiera de sus comandos |
| `set -E` (errtrace) | Hace que el `trap ... ERR` se herede a funciones y subshells |
| inherit_errexit | Opción de `shopt` para que `$(...)` herede `set -e` |
| Sustitución de comandos | `$(...)`: corre un comando y pega su salida en la línea |
| Separación de palabras | Paso en el que el shell parte en espacios una expansión sin comillas |
| `trap` | Comando que registra qué correr ante una señal o un evento (`EXIT`, `ERR`) |
| `mktemp` | Crea un archivo o carpeta temporal con nombre único y permisos solo para el dueño |
| getopts | Builtin para leer opciones cortas como `-n` |
| ShellCheck | Analizador estático de scripts de shell que señala errores comunes antes de ejecutarlos |
| PS4 | Prefijo que Bash imprime en cada línea de la traza de `set -x` |
| Ejecución en seco | Modo en que un script dice qué haría sin hacer cambios |

---

*Rogelio Arriaga Gonzalez*
