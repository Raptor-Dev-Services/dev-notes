# Práctica - S03 - El respaldo que se verifica

**Linux para desarrolladores**
**Tiempo estimado:** 90 minutos, en equipo, en su VM.

---

## Objetivo

Escribir un respaldo de `/var/lib/api-lab` que **verifica su propio resultado**: si el origen no existe, si está vacío, si no puede escribir o si el archivo sale sin contenido, termina con un código distinto de 0, dice por qué y no deja nada que parezca un respaldo bueno.

Antes de escribirlo, van a ver fallar en silencio al respaldo "de siempre". La regla del curso: si no lo vieron fallar, no lo probaron.

---

## Antes de empezar

- Su VM Ubuntu Server 24.04 y un usuario con `sudo`: `ssh <USUARIO>@<IP_DE_SU_VM>`.
- El usuario de servicio `api-lab` de la S02. Compruébenlo:

```bash
getent passwd api-lab
```

```text
api-lab:x:999:997::/home/api-lab:/usr/sbin/nologin
```

Los números pueden ser otros. Si el comando no imprime nada, créenlo como en la S02:

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin api-lab
```

- ShellCheck instalado:

```bash
sudo apt update && sudo apt install -y shellcheck
shellcheck --version | head -2
```

```text
ShellCheck - shell script analysis tool
version: 0.9.0
```

---

## Paso 1 - Datos de prueba

La API de laboratorio guardará su estado en `/var/lib/api-lab`. Por ahora le ponemos datos a mano:

```bash
sudo install -d -o api-lab -g api-lab /var/lib/api-lab /var/lib/api-lab/tmp
printf '{"pedido": 1, "total": 350}\n' | sudo -u api-lab tee /var/lib/api-lab/pedidos.json > /dev/null
printf 'id,nombre\n1,Ana\n2,Beto\n'     | sudo -u api-lab tee /var/lib/api-lab/clientes.csv > /dev/null
printf '{"sesion": "abc"}\n'           | sudo -u api-lab tee /var/lib/api-lab/tmp/sesion-1.json > /dev/null
mkdir -p ~/lab-s03/vacio ~/lab-s03/respaldos
find /var/lib/api-lab
```

Salida esperada:

```text
/var/lib/api-lab
/var/lib/api-lab/pedidos.json
/var/lib/api-lab/clientes.csv
/var/lib/api-lab/tmp
/var/lib/api-lab/tmp/sesion-1.json
```

---

## Paso 2 - El respaldo de siempre

Este es el script que casi todos tenemos en algún servidor. Guárdenlo como `~/lab-s03/respaldo-viejo.sh`:

```bash
cd ~/lab-s03
cat > respaldo-viejo.sh <<'EOF'
#!/bin/bash
# El respaldo de siempre: nadie lo ha visto fallar.
ORIGEN=$1
DESTINO=$2
tar -czf $DESTINO/respaldo-$(date +%Y%m%d).tar.gz -C $ORIGEN .
echo "Respaldo listo"
EOF
chmod +x respaldo-viejo.sh
./respaldo-viejo.sh /var/lib/api-lab ~/lab-s03/respaldos; echo "salida: $?"
ls -l respaldos
```

Salida esperada (la fecha, la hora y el usuario serán los suyos):

```text
Respaldo listo
salida: 0
-rw-r--r-- 1 alumno alumno 280 Nov  9 04:07 respaldo-20261109.tar.gz
```

Con datos buenos funciona. Por eso nadie lo ha revisado.

---

## Paso 3 - Verlo fallar: el script viejo

### 3a. Origen que no existe

```bash
rm -f respaldos/*
./respaldo-viejo.sh /var/lib/api-lab-que-no-existe ~/lab-s03/respaldos; echo "salida: $?"
ls -l respaldos
```

```text
tar: /var/lib/api-lab-que-no-existe: Cannot open: No such file or directory
tar: Error is not recoverable: exiting now
Respaldo listo
salida: 0
-rw-r--r-- 1 alumno alumno 20 Nov  9 04:07 respaldo-20261109.tar.gz
```

`tar` se quejó, pero el script dice "Respaldo listo", sale con **0** y deja un archivo de **20 bytes**. Un cron o un timer lo registra como éxito.

### 3b. Origen vacío

```bash
rm -f respaldos/*
./respaldo-viejo.sh ~/lab-s03/vacio ~/lab-s03/respaldos; echo "salida: $?"
tar -tzf respaldos/*.tar.gz
```

```text
Respaldo listo
salida: 0
./
```

Un respaldo "correcto" de nada.

### 3c. El bueno se sobrescribe

```bash
rm -f respaldos/*
./respaldo-viejo.sh /var/lib/api-lab ~/lab-s03/respaldos; ls -l respaldos | tail -1
./respaldo-viejo.sh /var/lib/api-lab-typo ~/lab-s03/respaldos 2>/dev/null; echo "salida: $?"; ls -l respaldos | tail -1
```

```text
Respaldo listo
-rw-r--r-- 1 alumno alumno 280 Nov  9 04:07 respaldo-20261109.tar.gz
Respaldo listo
salida: 0
-rw-r--r-- 1 alumno alumno 20 Nov  9 04:07 respaldo-20261109.tar.gz
```

El nombre solo lleva la fecha: una segunda corrida con un error en el mismo día **destruye el respaldo bueno** y lo cambia por uno de 20 bytes.

---

## Paso 4 - ShellCheck sobre el script viejo

```bash
shellcheck respaldo-viejo.sh; echo "shellcheck: $?"
```

```text
In respaldo-viejo.sh line 5:
tar -czf $DESTINO/respaldo-$(date +%Y%m%d).tar.gz -C $ORIGEN .
         ^------^ SC2086 (info): Double quote to prevent globbing and word splitting.
                           ^-------------^ SC2046 (warning): Quote this to prevent word splitting.
                                                     ^-----^ SC2086 (info): Double quote to prevent globbing and word splitting.
...
shellcheck: 1
```

Corrijan las comillas como sugiere y vuelvan a correr el paso 3a. **Sigue diciendo "Respaldo listo".** ShellCheck revisa cómo está escrito el script, no si revisa lo que hace.

---

## Paso 5 - Escribir respaldo.sh

Escríbanlo en `/usr/local/bin/respaldo.sh`. Intenten primero su propia versión con los ocho bloques de la clase; esta es la de referencia, la que usa la S04:

```bash
sudo tee /usr/local/bin/respaldo.sh > /dev/null <<'EOF'
#!/usr/bin/env bash
# respaldo.sh - Respalda un directorio en un .tar.gz y verifica el resultado antes de darlo por bueno.
# Uso: respaldo.sh ORIGEN DESTINO
set -Eeuo pipefail
shopt -s inherit_errexit

readonly ORIGEN="${1:?uso: respaldo.sh ORIGEN DESTINO}"
readonly DESTINO="${2:?uso: respaldo.sh ORIGEN DESTINO}"
ARCHIVO="$DESTINO/respaldo-$(date +%Y%m%d-%H%M%S).tar.gz"
readonly ARCHIVO

log()   { printf '%s respaldo: %s\n' "$(date '+%F %T')" "$*" >&2; }
falla() { log "ERROR: $*"; exit 1; }

# Si algo truena a la mitad, no dejar un archivo a medias que parezca bueno.
al_salir() {
  local codigo=$?
  if (( codigo != 0 )); then
    rm -f -- "$ARCHIVO.parcial"
    log "terminó con código $codigo; no quedó ningún respaldo nuevo"
  fi
}
trap al_salir EXIT
trap 'log "falló la línea $LINENO: $BASH_COMMAND"' ERR

[[ -d "$ORIGEN" ]] || falla "el origen $ORIGEN no existe o no es un directorio"
mkdir -p -- "$DESTINO"

esperados=$(find "$ORIGEN" ! -type d | wc -l)
(( esperados > 0 )) || falla "el origen $ORIGEN está vacío: no hay nada que respaldar"

log "respaldando $esperados archivos de $ORIGEN"
tar -czf "$ARCHIVO.parcial" -C "$ORIGEN" .

# Verificar el resultado, no solo el código de salida de tar.
gzip -t "$ARCHIVO.parcial" || falla "el archivo comprimido está corrupto"
encontrados=$(tar -tzf "$ARCHIVO.parcial" | grep -vc '/$') || falla "el respaldo salió sin archivos"
(( encontrados == esperados )) || falla "se esperaban $esperados archivos y el respaldo trae $encontrados"

mv -- "$ARCHIVO.parcial" "$ARCHIVO"
log "listo: $ARCHIVO ($(du -h "$ARCHIVO" | cut -f1), $encontrados archivos)"
EOF
sudo chmod 755 /usr/local/bin/respaldo.sh
bash -n /usr/local/bin/respaldo.sh && echo "sintaxis: ok"
shellcheck /usr/local/bin/respaldo.sh && echo "shellcheck: sin avisos"
```

Salida esperada:

```text
sintaxis: ok
shellcheck: sin avisos
```

Fíjense en tres decisiones:

- `ARCHIVO` se asigna en una línea y se marca `readonly` en otra: así el código de `date` no se pierde.
- El nombre lleva fecha **y hora**: dos corridas el mismo día no se pisan.
- `grep -vc '/$'` cuenta las entradas que no son directorios. Si no hay ninguna, `grep -c` sale con 1 y el `|| falla` lo atrapa: el mismo comportamiento que engañó a la limpieza de runners, usado a favor.

---

## Paso 6 - El caso bueno

```bash
sudo respaldo.sh /var/lib/api-lab /var/backups/api-lab; echo "salida: $?"
sudo ls -l /var/backups/api-lab
sudo tar -tzf /var/backups/api-lab/respaldo-*.tar.gz
```

```text
2026-11-09 04:07:35 respaldo: respaldando 3 archivos de /var/lib/api-lab
2026-11-09 04:07:35 respaldo: listo: /var/backups/api-lab/respaldo-20261109-040735.tar.gz (4.0K, 3 archivos)
salida: 0
-rw-r--r-- 1 root root 280 Nov  9 04:07 respaldo-20261109-040735.tar.gz
./
./pedidos.json
./clientes.csv
./tmp/
./tmp/sesion-1.json
```

Cuenta 3 archivos en el origen, encuentra 3 en el respaldo y solo entonces lo publica con su nombre final.

---

## Paso 7 - Verlo fallar: el script nuevo

Provoquen los cuatro casos. Anoten en cada uno la salida, el código y si quedó algún archivo nuevo en `/var/backups/api-lab`.

```bash
sudo respaldo.sh; echo "salida: $?"
sudo respaldo.sh /var/lib/api-lab-que-no-existe /var/backups/api-lab; echo "salida: $?"
sudo respaldo.sh ~/lab-s03/vacio /var/backups/api-lab; echo "salida: $?"
respaldo.sh /var/lib/api-lab /var/backups/api-lab; echo "salida: $?"     # sin sudo: no puede escribir
sudo ls -l /var/backups/api-lab
```

```text
/usr/local/bin/respaldo.sh: line 7: 1: uso: respaldo.sh ORIGEN DESTINO
salida: 1
2026-11-09 04:07:35 respaldo: ERROR: el origen /var/lib/api-lab-que-no-existe no existe o no es un directorio
2026-11-09 04:07:35 respaldo: terminó con código 1; no quedó ningún respaldo nuevo
salida: 1
2026-11-09 04:07:35 respaldo: ERROR: el origen /home/alumno/lab-s03/vacio está vacío: no hay nada que respaldar
2026-11-09 04:07:35 respaldo: terminó con código 1; no quedó ningún respaldo nuevo
salida: 1
2026-11-09 04:07:35 respaldo: respaldando 3 archivos de /var/lib/api-lab
tar (child): /var/backups/api-lab/respaldo-20261109-040735.tar.gz.parcial: Cannot open: Permission denied
tar (child): Error is not recoverable: exiting now
tar: Child returned status 2
tar: Error is not recoverable: exiting now
2026-11-09 04:07:35 respaldo: falló la línea 33: tar -czf "$ARCHIVO.parcial" -C "$ORIGEN" .
2026-11-09 04:07:35 respaldo: terminó con código 2; no quedó ningún respaldo nuevo
salida: 2
-rw-r--r-- 1 root root 280 Nov  9 04:07 respaldo-20261109-040735.tar.gz
```

| Caso | Script viejo | Script nuevo |
|---|---|---|
| Origen inexistente | "Respaldo listo", 0, archivo de 20 bytes | Error, 1, sin archivo |
| Origen vacío | "Respaldo listo", 0, respaldo de nada | Error, 1, sin archivo |
| Sin permiso en el destino | (no lo probaron: pruébenlo) | Error en la línea 33, 2, sin archivo |
| Segunda corrida con error | Sobrescribe el bueno | El bueno sigue intacto |

Si alguno de sus casos **no** falla como dice la tabla, deténganse: encuentren por qué antes de seguir.

---

## Paso 8 - Rastrear una falla con bash -x

```bash
sudo bash -x /usr/local/bin/respaldo.sh ~/lab-s03/vacio /var/backups/api-lab 2>&1 | tail -8
```

```text
+ al_salir
+ local codigo=1
+ ((  codigo != 0  ))
+ rm -f -- /var/backups/api-lab/respaldo-20261109-040735.tar.gz.parcial
+ log 'terminó con código 1; no quedó ningún respaldo nuevo'
++ date '+%F %T'
+ printf '%s respaldo: %s\n' '2026-11-09 04:07:35' 'terminó con código 1; no quedó ningún respaldo nuevo'
2026-11-09 04:07:35 respaldo: terminó con código 1; no quedó ningún respaldo nuevo
```

Quiten el `| tail -8` y busquen en la traza la línea exacta donde `(( esperados > 0 ))` resultó falsa.

---

## Paso 9 - Reto (opcional): respaldos que solo lee root

Los respaldos quedaron con permisos `644`: cualquier usuario de la VM puede leer los datos de la API. Agreguen `umask 077` justo después de `shopt -s inherit_errexit`, corran el caso bueno con un destino nuevo y comprueben:

```bash
sudo respaldo.sh /var/lib/api-lab /var/backups/api-lab-privado
sudo ls -ld /var/backups/api-lab-privado; sudo ls -l /var/backups/api-lab-privado
```

```text
drwx------ 2 root root 4096 Nov  9 04:12 /var/backups/api-lab-privado
-rw------- 1 root root 305 Nov  9 04:12 respaldo-20261109-041235.tar.gz
```

Con una línea más, los números de línea del script cambian: el `tar` ahora es la línea 34.

---

## Evidencia que entregan

Un archivo `evidencia-s03.md` en el repositorio del equipo con:

1. El `respaldo.sh` final y la salida de `bash -n` y `shellcheck` sin avisos.
2. La salida del caso bueno (paso 6) con el listado de `tar -tzf`.
3. La salida y el código de los tres casos del script viejo (paso 3) y de los cuatro del nuevo (paso 7).
4. La tabla del paso 7 llenada con lo que **ustedes** midieron.
5. Una línea por caso: qué habría pasado en producción con el script viejo y quién se habría enterado.

---

## Checklist

- [ ] El usuario `api-lab` existe y `/var/lib/api-lab` tiene datos de prueba.
- [ ] Vieron al script viejo decir "Respaldo listo" con un origen que no existe.
- [ ] Vieron al script viejo sobrescribir un respaldo bueno con uno de 20 bytes.
- [ ] `respaldo.sh` pasa `bash -n` y `shellcheck` sin avisos.
- [ ] El caso bueno publica el respaldo y el conteo coincide.
- [ ] Los cuatro casos de falla terminan con código distinto de 0 y no dejan archivo nuevo.
- [ ] Encontraron en la traza de `bash -x` la condición que falló.
- [ ] `respaldo.sh` queda en `/usr/local/bin`: la S04 lo va a correr con un timer.
