# Práctica - S01 - La terminal eficiente

**Linux para desarrolladores**
**Duración:** 80 minutos, por equipo, en su VM.

---

## Objetivo

Contestar en **una sola línea de comandos** tres preguntas sobre el log de Nginx de un servidor de laboratorio, comprobar cada respuesta por otro camino y ver fallar dos tuberías que dan números falsos sin marcar ningún error.

Las tres preguntas:

1. ¿Cuáles son las 10 IP que más piden?
2. ¿Cuántas respuestas 5xx hubo por hora?
3. ¿Cuál es la ruta más lenta: la petición más lenta y la ruta con peor promedio?

---

## Antes de empezar

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
lsb_release -d
# Description:    Ubuntu 24.04.x LTS
python3 --version
# Python 3.12.3
```

Ubuntu Server 24.04 ya trae Python 3. El generador es determinista: con la misma versión de Python, **todos los equipos obtienen el mismo log y deben llegar a los mismos números**. Si tu número es otro, la tubería tiene un error.

---

## Paso 1 - Generar el log

```bash
mkdir -p ~/s01 && cd ~/s01
nano generar-log.py
```

Pega este script, guarda con Ctrl-O y sal con Ctrl-X:

```python
#!/usr/bin/env python3
# Genera un access.log de Nginx de laboratorio: un día, IP de documentación y tiempo de respuesta al final.
import random
from datetime import datetime, timedelta

random.seed(2026)
ips = [f"192.0.2.{n}" for n in range(1, 40)] + [f"198.51.100.{n}" for n in range(1, 30)] + [f"203.0.113.{n}" for n in range(1, 20)]
pesos = [random.paretovariate(2.0) for _ in ips]           # unas pocas IP piden mucho
rutas = {"/": 0.02, "/api/health": 0.003, "/api/productos": 0.08, "/api/productos/42": 0.05,
         "/api/pedidos": 0.15, "/api/reportes/mensual": 1.8, "/login": 0.12, "/static/app.js": 0.01}
agentes = ['Mozilla/5.0 (X11; Linux x86_64) Firefox/131.0', 'curl/8.5.0', 'Uptime-Kuma/1.23.13']
inicio = datetime(2026, 11, 7, 0, 0, 0)

with open("access.log", "w") as log:
    for i in range(6000):
        t = inicio + timedelta(seconds=i * 14 + random.randint(0, 13))
        ruta = random.choice(list(rutas))
        if random.random() < 0.1:
            ruta += f"?pagina={random.randint(1, 9)}"
        estado = random.choices([200, 304, 404, 500, 502], [86, 6, 5, 2, 1])[0]
        if t.hour == 14 and ruta.startswith("/api/pedidos") and random.random() < 0.6:
            estado = 502                                    # el incidente de las 14 h
        tiempo = rutas[ruta.split("?")[0]] * random.uniform(0.5, 2.0)
        ip = random.choices(ips, pesos)[0]
        fecha = t.strftime("%d/%b/%Y:%H:%M:%S +0000")
        log.write(f'{ip} - - [{fecha}] "GET {ruta} HTTP/1.1" {estado} {random.randint(200, 9000)} '
                  f'"-" "{random.choice(agentes)}" {tiempo:.3f}\n')
        if i % 1500 == 0:                                   # un cliente que abre y cierra sin pedir nada
            log.write(f'198.51.100.250 - - [{fecha}] "-" 400 0 "-" "-" 0.000\n')
```

```bash
python3 generar-log.py
wc -l access.log
md5sum access.log
```

**Salida esperada:**

```
6004 access.log
39243ff4f0aac5d33b4b838298412277  access.log
```

Si el `md5sum` no coincide, revisa que copiaste el script completo antes de seguir.

---

## Paso 2 - Conocer el formato antes de preguntarle nada

```bash
head -3 access.log
```

```
192.0.2.28 - - [07/Nov/2026:00:00:08 +0000] "GET /static/app.js HTTP/1.1" 200 8345 "-" "curl/8.5.0" 0.015
198.51.100.250 - - [07/Nov/2026:00:00:08 +0000] "-" 400 0 "-" "-" 0.000
192.0.2.13 - - [07/Nov/2026:00:00:22 +0000] "GET / HTTP/1.1" 200 8640 "-" "curl/8.5.0" 0.038
```

Es el formato `combined` de Nginx con el tiempo de respuesta (`$request_time`) al final. En la sesión 5 configuran su propio Nginx para que escriba exactamente esto.

Con los espacios como separador, `awk` numera así una línea normal:

| Campo | Ejemplo | Qué es |
|---|---|---|
| `$1` | `192.0.2.28` | IP del cliente |
| `$4` | `[07/Nov/2026:00:00:08` | fecha y hora, con el corchete |
| `$7` | `/static/app.js` | ruta, con su query string si trae |
| `$9` | `200` | código de estado |
| `$10` | `8345` | bytes enviados |
| `$NF` | `0.015` | segundos que tardó (el último campo) |

Anota ya la línea 2: se ve distinta. Vuelve en el paso 6.

---

## Paso 3 - Pregunta 1: las 10 IP que más piden

Constrúyela por tramos, mirando la salida de cada uno con `head`:

```bash
awk '{print $1}' access.log | head -3
awk '{print $1}' access.log | sort | head -3
awk '{print $1}' access.log | sort | uniq -c | head -3
awk '{print $1}' access.log | sort | uniq -c | sort -rn | head -10
```

**Salida esperada de la última:**

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

**Comprobación cruzada** (otra herramienta, mismo número):

```bash
grep -c '^192\.0\.2\.28 ' access.log
# 1613
cut -d' ' -f1 access.log | sort | uniq -c | sort -rn | head -1
#    1613 192.0.2.28
```

El `^` y el espacio final del `grep` evitan contar `192.0.2.2` dentro de `192.0.2.28`, o al revés.

---

## Paso 4 - Pregunta 2: los 5xx por hora

```bash
awk '$9 ~ /^5/ {print substr($4, 2, 14)}' access.log | sort | uniq -c
```

**Salida esperada (fragmento):**

```
      6 07/Nov/2026:00
     15 07/Nov/2026:01
      6 07/Nov/2026:02
...
      7 07/Nov/2026:13
     26 07/Nov/2026:14
      8 07/Nov/2026:15
...
```

Son 24 líneas, una por hora. `substr($4, 2, 14)` empieza después del corchete y corta justo en la hora.

**Comprobación cruzada:** la suma de la columna debe dar el total de 5xx.

```bash
awk '$9 ~ /^5/' access.log | wc -l
# 179
awk '$9 ~ /^5/ {print substr($4, 2, 14)}' access.log | sort | uniq -c | awk '{s += $1} END {print s}'
# 179
```

**Bajar un nivel.** La hora 14 tiene casi el doble que la siguiente. ¿Qué falló?

```bash
awk '$9 ~ /^5/ && $4 ~ /:14:/ {print $9, $7}' access.log | sort | uniq -c | sort -rn | head -4
```

```
     18 502 /api/pedidos
      2 500 /login
      2 500 /api/reportes/mensual
      2 500 /api/health
```

Anota en tu respuesta qué significa un 502 detrás de un proxy inverso (lo vemos a fondo en la sesión 5).

---

## Paso 5 - Pregunta 3: la ruta más lenta

**La petición más lenta.** Se imprime el tiempo primero para que `sort -rn` ordene por él:

```bash
awk '{print $NF, $7}' access.log | sort -rn | head -3
```

```
3.591 /api/reportes/mensual
3.590 /api/reportes/mensual
3.582 /api/reportes/mensual
```

**La ruta con peor promedio**, sin la query string y con cuántas veces se pidió:

```bash
awk '{ruta = $7; sub(/\?.*/, "", ruta); suma[ruta] += $NF; n[ruta]++}
     END {for (r in suma) printf "%.3f %5d %s\n", suma[r] / n[r], n[r], r}' access.log | sort -rn | head -5
```

```
2.308   803 /api/reportes/mensual
0.184   748 /api/pedidos
0.147   785 /login
0.101   723 /api/productos
0.064   700 /api/productos/42
```

**Comprobación cruzada:** el promedio general de todas las peticiones.

```bash
awk '{s += $NF} END {printf "%.3f\n", s / NR}' access.log
# 0.375
```

Una sola ruta lenta jala el promedio general hacia arriba: casi todas las demás tardan menos de 0.2 s.

---

## Paso 6 - Verlo fallar

Dos tuberías que **no marcan ningún error** y dan un número falso.

### 6.1 `uniq` sin `sort`

```bash
awk '{print $1}' access.log | uniq -c | sort -rn | head -3
```

```
      6 192.0.2.28
      6 192.0.2.28
      5 192.0.2.28
```

```bash
awk '{print $1}' access.log | uniq | wc -l
# 5481
awk '{print $1}' access.log | sort -u | wc -l
# 88
```

`uniq` solo junta líneas iguales **que están una junto a otra**. Sin `sort` cuenta rachas: la IP de 1613 peticiones aparece con 6, repetida muchas veces, y en lugar de 88 IP distintas salen 5481 "grupos".

### 6.2 La línea que corre los campos

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

```bash
grep -c '"-" 400' access.log
# 4
grep '"-" 400' access.log | head -1
# 198.51.100.250 - - [07/Nov/2026:00:00:08 +0000] "-" 400 0 "-" "-" 0.000
```

Un cliente abrió la conexión y la cerró sin mandar petición. Nginx escribe `"-"` en lugar de `"GET /ruta HTTP/1.1"`: una palabra en vez de tres, y todos los campos siguientes se corren dos lugares. El 400 queda en `$7` y en `$9` aparece `"-"`.

Consecuencia: contar los 4xx por el campo 9 da un número equivocado.

```bash
awk '$9 ~ /^4/' access.log | wc -l
# 311
```

### 6.3 La versión que no se deja engañar

Partir la línea por las comillas en lugar de los espacios:

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

```bash
awk -F'"' '{split($3, c, " "); if (c[1] ~ /^4/) n++} END {print n}' access.log
# 315
```

Con `-F'"'`, el campo 3 es lo que hay entre la petición y el referer (` 400 0 `), sin importar cuántas palabras traiga la petición. Hay **315** respuestas 4xx, no 311.

Anota en tu entrega: ¿la ruta más lenta del paso 5 también se ve afectada por estas 4 líneas? (Pista: busca una "ruta" llamada `400` en la tabla de promedios completa, sin `head`).

---

## Paso 7 - Retos (si te sobra tiempo)

1. **¿Quién es 192.0.2.28?** ¿Qué agentes de usuario usa y qué rutas pide?

   ```bash
   grep '^192\.0\.2\.28 ' access.log | awk -F'"' '{print $6}' | sort | uniq -c | sort -rn
   ```

2. **Bytes servidos por ruta.** ¿La ruta que más datos mueve es la que más se pide?

   ```bash
   awk '{ruta = $7; sub(/\?.*/, "", ruta); b[ruta] += $10} END {for (r in b) print b[r], r}' access.log | sort -rn | head -3
   ```

3. Reescribe la pregunta 2 con el campo robusto del paso 6.3. ¿Cambia algún número? ¿Por qué no?

---

## Evidencia que entregas

Un archivo `respuestas-s01.txt` por equipo con:

1. Para cada una de las tres preguntas: **la línea** que la contesta, **la salida** que obtuviste y **la comprobación cruzada**.
2. El paso "Verlo fallar": los números de `uniq` sin `sort` (6, 5481) frente a los correctos (1613, 88), y la explicación con tus palabras de las líneas `"-"` y del 311 frente a 315.
3. Tu historial de la práctica:

   ```bash
   history | tail -60 > ~/s01/historial-s01.txt
   ```

---

## Checklist

- [ ] El `md5sum` del log coincide con el de la guía.
- [ ] Las 10 IP salen con la misma cuenta que la guía, y `grep -c` lo confirma.
- [ ] Los 5xx por hora suman 179 y encontraste qué ruta falló a las 14 h.
- [ ] Tienes la petición más lenta y la ruta con peor promedio, sin la query string.
- [ ] Viste fallar `uniq` sin `sort` y anotaste los dos números.
- [ ] Explicaste por qué `$9` da 311 cuando hay 315 respuestas 4xx.
- [ ] Cada respuesta es **una sola línea** de comandos, recuperable con Ctrl-R.
