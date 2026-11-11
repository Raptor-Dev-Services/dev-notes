# Práctica - S01 - Inventario priorizado de un sistema de ejemplo

**Defensa y respuesta a incidentes**
**Duración estimada:** 1.5 horas

---

## Objetivo

Armar el inventario de un sistema pequeño, ordenarlo por criterio escrito, **verlo fallar** cuando un dato viene mal, y decidir qué hacer con una vulnerabilidad real combinando CVSS, EPSS, KEV y el contexto del sistema.

La regla del curso: una lista que parece completa y no lo es, es peor que no tener lista. Hoy se comprueba que la propia herramienta se queja cuando algo no cuadra.

**Alcance:** todo ocurre en su laboratorio (`victima` 192.168.56.20 y la terminal local). No se escanea ni se consulta nada que no sea suyo. El sistema de la tienda es un **ejemplo ilustrativo**: no existe.

---

## Antes de empezar

- Una carpeta de trabajo, por ejemplo `~/blue-team/s01`, dentro de un repositorio Git del equipo.
- Herramientas: `awk`, `column`, `jq` y `curl`.

```bash
mkdir -p ~/blue-team/s01 && cd ~/blue-team/s01
command -v awk column jq curl
```

Si falta alguna: `sudo apt install -y jq curl bsdextrautils` (`column` viene en `bsdextrautils` en Ubuntu reciente; en otras versiones, en `util-linux`).

---

## Paso 1 - El inventario de la tienda

El equipo de la tienda (**Ejemplo ilustrativo**: Ana y Luis) tiene este sistema. Guarden el inventario como `inventario.csv`:

```csv
id,nombre,tipo,dueno,datos,clase_datos,criticidad,exposicion,notas
A01,tienda-web,aplicacion,ana,pedidos y correos de clientes,confidencial,3,3,nginx en victima 192.168.56.20 puerto 443
A02,api-pagos,aplicacion,ana,tokens de la pasarela de pago,restringido,3,2,solo la llama tienda-web
A03,postgres-tienda,base de datos,luis,clientes y pedidos,confidencial,3,1,escucha solo en localhost
A04,respaldos-nocturnos,datos,luis,copia completa de postgres-tienda,restringido,3,1,disco externo y copia cifrada
A05,victima,servidor,luis,ninguno propio,interno,2,3,Ubuntu 24.04 con ssh y nginx
A06,siem,servidor,luis,bitacoras de todo el laboratorio,confidencial,2,1,solo red host-only
A07,repo-git,servicio,ana,codigo fuente y secretos olvidados,confidencial,3,2,alojado con un proveedor
A08,laptop-ana,equipo,ana,sesiones y llaves ssh,restringido,2,2,sale de la oficina
A09,cuenta-dns,cuenta,luis,control del dominio,restringido,3,3,proveedor de dominio sin segundo factor
A10,demo-2025,servidor,sin dueno,datos de prueba que parecen reales,confidencial,1,3,levantado para una demo hace un ano
A11,sitio-de-precios,aplicacion,ana,ninguno,publico,1,3,pagina estatica
A12,kali,equipo,luis,herramientas de laboratorio,interno,1,1,solo cuando hay practica
```

Dos reglas del formato: **ningún campo lleva comas** (el script lo lee con `awk -F,`) y **una fila por activo**.

```bash
wc -l inventario.csv
# 13 inventario.csv
```

Trece líneas: el encabezado y 12 activos.

---

## Paso 2 - El script que ordena

Guárdenlo como `priorizar.sh`:

```bash
#!/usr/bin/env bash
# priorizar.sh - Ordena un inventario CSV por puntaje = criticidad x exposicion (de mayor a menor).
# Uso: priorizar.sh inventario.csv        Los campos no pueden llevar comas.
set -euo pipefail
awk -F, 'NR > 1 {
  if ($7 !~ /^[1-3]$/ || $8 !~ /^[1-3]$/) { printf "ERROR linea %d: criticidad/exposicion invalida (%s,%s)\n", NR, $7, $8 > "/dev/stderr"; bad = 1; next }
  printf "%d\t%s\t%s\t%s\t%s\n", $7 * $8, $1, $2, $4, $6 }
  END { exit bad }' "$1" | sort -t$'\t' -k1,1nr -k2,2 | column -t -s$'\t'
```

```bash
chmod +x priorizar.sh
./priorizar.sh inventario.csv
```

Salida esperada (la ejecución es determinista):

```text
9  A01  tienda-web           ana        confidencial
9  A09  cuenta-dns           luis       restringido
6  A02  api-pagos            ana        restringido
6  A05  victima              luis       interno
6  A07  repo-git             ana        confidencial
4  A08  laptop-ana           ana        restringido
3  A03  postgres-tienda      luis       confidencial
3  A04  respaldos-nocturnos  luis       restringido
3  A10  demo-2025            sin dueno  confidencial
3  A11  sitio-de-precios     ana        publico
2  A06  siem                 luis       confidencial
1  A12  kali                 luis       interno
```

**Preguntas para el equipo, antes de seguir:**

1. ¿Por qué `cuenta-dns` empata con `tienda-web`, si no es un servidor?
2. ¿Por qué `demo-2025` empata con los respaldos?
3. ¿Qué activo pondrían ustedes más arriba que el script, y con qué argumento escrito?

---

## Paso 3 - Verlo fallar

Dañen un dato a propósito. Escriban `alta` en lugar de `2` en la exposición de `api-pagos`:

```bash
sed 's/^A02,\(.*\),3,2,/A02,\1,3,alta,/' inventario.csv > roto.csv
grep '^A02' roto.csv
./priorizar.sh roto.csv
echo "salida: $?"
```

Lo que debe verse:

```text
A02,api-pagos,aplicacion,ana,tokens de la pasarela de pago,restringido,3,alta,solo la llama tienda-web
ERROR linea 3: criticidad/exposicion invalida (3,alta)
9  A01  tienda-web           ana        confidencial
9  A09  cuenta-dns           luis       restringido
6  A05  victima              luis       interno
...
salida: 1
```

Observen tres cosas:

1. El aviso sale por `stderr`: si redirigen la salida a un archivo (`./priorizar.sh roto.csv > lista.txt`), **el aviso no queda en el archivo** y `api-pagos`, un activo de criticidad 3 que maneja tokens de pago, **desaparece de la lista**.
2. El código de salida es `1`: quien automatice el script debe revisarlo.
3. La lista restante **parece completa**. Nadie que solo lea el archivo se da cuenta.

Comprueben el punto 1:

```bash
./priorizar.sh roto.csv > lista.txt 2> errores.txt || echo "falló con código $?"
grep -c api-pagos lista.txt; cat errores.txt
```

```text
falló con código 1
0
ERROR linea 3: criticidad/exposicion invalida (3,alta)
```

Anoten en su bitácora qué cambiarían para que un dato inválido **detenga** el reporte en lugar de recortarlo. (Pista: validar todo el archivo en una primera pasada y no imprimir nada hasta que pase.)

---

## Paso 4 - Preguntas al inventario

Un inventario sirve cuando contesta preguntas. Estas dos son las primeras:

```bash
# Activos sin dueño
awk -F, '$4=="sin dueno" {print $1, $2}' inventario.csv
# A10 demo-2025

# Datos restringidos con exposición de 2 o más
awk -F, 'NR>1 && $6=="restringido" && $8>=2 {print $1, $2, "exposicion="$8}' inventario.csv
# A02 api-pagos exposicion=2
# A08 laptop-ana exposicion=2
# A09 cuenta-dns exposicion=3
```

Y esta cuenta cuántos activos hay de cada clase:

```bash
awk -F, 'NR>1 {c[$6]++} END{for(k in c) print k, c[k]}' inventario.csv | sort
```

```text
confidencial 5
interno 2
publico 1
restringido 4
```

**Para escribir en la bitácora:**

- ¿Qué harían con `demo-2025`? Las opciones válidas son apagarlo (baja), asignarle dueño y fecha de revisión, o documentar una excepción.
- De los tres activos restringidos con exposición de 2 o más, ¿cuál tiene el control faltante más barato de corregir?

---

## Paso 5 - Lo que realmente hay en `victima`

El inventario dice lo que **creemos** que hay. En `victima`, pidan a la máquina lo que **realmente** hay:

```bash
ssh <USUARIO>@192.168.56.20
sudo ss -tulpn
dpkg-query -W -f='${Package},${Version}\n' | wc -l
dpkg-query -W -f='${Package},${Version}\n' | grep -E '^(openssh-server|nginx),'
```

La salida real depende de su VM (versiones, puertos). Lo que busquen:

| Comparación | Qué significa |
|---|---|
| Un puerto que escucha y no está en el inventario | Un activo (o una puerta) que nadie anotó |
| `0.0.0.0` donde el inventario dice "solo localhost" | La exposición está mal anotada |
| Un proceso que no reconocen | Primera pregunta para el dueño |

Añadan al inventario cualquier cosa nueva con su dueño provisional, y corrijan la `exposicion` de lo que estaba mal. **Anoten cuántas filas cambiaron**: ese es el tamaño de la brecha entre lo que creían y lo que hay.

---

## Paso 6 - Una CVE real, con las tres señales

El sistema de ejemplo corre OpenSSH en `victima`. Vamos a decidir sobre **CVE-2024-6387** (una condición de carrera en `sshd`, versiones 8.5p1 a 9.7p1 de OpenSSH, corregida en la 9.8). Es una CVE real; las cifras cambian a diario, así que **cada quien las consulta hoy**.

**CVSS (NVD):**

```bash
curl -fsS 'https://services.nvd.nist.gov/rest/json/cves/2.0?cveId=CVE-2024-6387' |
  jq -r '.vulnerabilities[0].cve.metrics.cvssMetricV31[] | "\(.source) \(.cvssData.baseScore) \(.cvssData.baseSeverity) \(.cvssData.vectorString)"'
```

Al preparar este material (11 de noviembre de 2026) devolvió dos líneas con **8.1 HIGH** y el vector `CVSS:3.1/AV:N/AC:H/PR:N/UI:N/S:U/C:H/I:H/A:H`. La NVD limita las peticiones sin clave: si falla, esperen unos segundos.

**EPSS (FIRST):**

```bash
curl -fsS 'https://api.first.org/data/v1/epss?cve=CVE-2024-6387' |
  jq -r '.data[] | "\(.cve) epss=\(.epss) percentil=\(.percentile) fecha=\(.date)"'
```

```text
CVE-2024-6387 epss=0.995060000 percentil=0.999450000 fecha=2026-11-10
```

**KEV (CISA):**

```bash
curl -fsS -O https://www.cisa.gov/sites/default/files/feeds/known_exploited_vulnerabilities.json
jq -r '"catalogVersion=\(.catalogVersion) total=\(.count)"' known_exploited_vulnerabilities.json
jq -r '.vulnerabilities[] | select(.cveID=="CVE-2024-6387") | .cveID' known_exploited_vulnerabilities.json | wc -l
```

```text
catalogVersion=2026.11.10 total=1779
0
```

**Qué versión corre su `victima`:**

```bash
dpkg-query -W -f='${Version}\n' openssh-server
ssh -V
```

Ubuntu aplica correcciones de seguridad sin subir el número de versión de OpenSSH: el paquete se llama `1:9.6p1-3ubuntu13.x` en Ubuntu 24.04 y puede estar parchado aunque diga 9.6. **No decidan solo por el número de versión de OpenSSH**: consulten el aviso de seguridad de Ubuntu para esa CVE en `ubuntu.com/security/CVE-2024-6387` y comparen con la versión **del paquete**. Al preparar este material, esa página daba para Ubuntu 24.04 el estado "Fixed in 1:9.6p1-3ubuntu13.3": cualquier versión de paquete igual o mayor ya trae la corrección.

**Decidan y escriban (cinco líneas):**

1. ¿Está expuesto `sshd` de `victima` a internet? (En el laboratorio, no: red host-only. Escriban qué cambiaría su respuesta.)
2. ¿Qué dicen las tres señales y qué no dice cada una?
3. ¿Qué hay que verificar antes de actuar? (La versión del paquete, no solo la de OpenSSH.)
4. ¿Qué plazo les daría la tabla de plazos de la clase en el caso interno? ¿Y si estuviera en internet?
5. ¿Qué evidencia guardarían de que ya quedó? (Salida de `dpkg-query` y de `ps -o pid,lstart,cmd -C sshd` después de reiniciar.)

---

## Paso 7 - Parches pendientes

```bash
apt list --upgradable 2>/dev/null
sudo unattended-upgrades --dry-run --debug 2>&1 | tail -n 5
```

- La primera lista lo que hay pendiente según los orígenes configurados.
- La segunda simula la actualización automática, sin instalar.
- Un paquete en la lista no es una vulnerabilidad por sí mismo: decidan cuáles son de seguridad y cuáles no.

Revisen la configuración de actualización automática:

```bash
cat /etc/apt/apt.conf.d/20auto-upgrades
grep -n 'Automatic-Reboot' /etc/apt/apt.conf.d/50unattended-upgrades
```

Respondan: ¿se instalan las actualizaciones de seguridad solas? ¿La máquina se reinicia sola cuando hace falta? Si la segunda respuesta es no, **¿quién la reinicia y cuándo?** Esa respuesta va en la política de parches de su tarea.

---

## Paso 8 - Una excepción documentada

Supongan que el parche de `openssh-server` en `victima` **no se puede aplicar hasta el viernes**. Escriban la excepción (**Ejemplo ilustrativo**) con este formato:

```markdown
## Excepción E-001
- Qué se incumple: parche de openssh-server en victima (A05)
- Por qué: el servicio de pruebas no tolera reinicios hasta el viernes
- Control compensatorio: cerrar el puerto 22 al exterior; solo desde la red de administración
- Quién autoriza: <persona responsable técnica>
- Fecha de alta: <hoy>
- Vence: <hoy + 10 días>
- Cómo se verifica el control: sudo ss -tulpn | grep ':22'
```

El campo **"Vence"** es el que más se omite y el más importante: una excepción sin fecha es una decisión permanente que nadie tomó.

---

## Entregables de la práctica

Suban al repositorio del equipo, en `s01/`:

1. `inventario.csv` actualizado con lo que encontraron en el paso 5.
2. `priorizar.sh` y la lista ordenada (`lista.txt`).
3. `bitacora.md` con: las respuestas del paso 2, lo que cambiarían del paso 3, las cinco líneas del paso 6 y la excepción del paso 8.

## Limpieza

```bash
rm -f roto.csv lista.txt errores.txt known_exploited_vulnerabilities.json
```

El archivo del catálogo KEV pesa varios megabytes y cambia a diario: no se versiona.

---

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `curl: (22) ... 403` o `429` en la NVD | Límite de peticiones sin clave | Esperen unos segundos y repitan; no hagan ciclos |
| `jq: error: Cannot index ...` | La consulta no devolvió JSON (límite o error) | Corran el `curl` solo y lean el cuerpo |
| `priorizar.sh` muestra columnas torcidas | Un campo con coma | Quiten la coma del campo |
| `column: command not found` | Falta el paquete | `sudo apt install -y bsdextrautils` |
| `ss` no muestra procesos | Falta `sudo` | Repitan con `sudo` |
