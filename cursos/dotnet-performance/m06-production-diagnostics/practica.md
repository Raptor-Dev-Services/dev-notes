# Práctica - M6 - Diagnóstico en producción

**.NET de alto rendimiento**
**Duración:** 60 a 75 minutos, individual, en tu máquina y dentro de **un** contenedor `mcr.microsoft.com/dotnet/sdk:10.0`.

---

## Objetivo

Encontrar una **fuga de memoria sembrada** en la API de ejemplo con `dotnet-counters` y `dotnet-dump`, sin parar la API: verla crecer, tomar un volcado, descubrir qué tipo ocupa la memoria y **quién lo retiene**, corregirla y volver a medir.

> **Solo la API de ejemplo, en tu máquina.** Nunca apuntes estas herramientas ni la carga a un servicio de producción o ajeno. El volcado contiene la memoria del proceso: se **borra** al terminar.

El código es [`referencia/ApiEjemplo/Program.cs`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/ApiEjemplo/Program.cs) (endpoint `/pedido/{id}`, variable `CACHE`) y [`referencia/Carga/Program.cs`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Carga/Program.cs). Tus cifras serán otras; lo que debe repetirse es la **forma**: el heap sube con la carga y no baja al pararla.

Entorno de las salidas de esta guía: .NET 10 (SDK 10.0.403, runtime 10.0.14), `dotnet-counters`, `dotnet-dump` y `dotnet-trace` 10.0.750501, i5-11500, contenedor con 4 CPU, servidor compartido y con carga. Salidas completas en [`referencia/mediciones/`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/mediciones/README.md).

---

## Antes de empezar

Un contenedor, y bórralo al terminar (`--rm`). Se monta la carpeta del curso para compilar ahí; las herramientas y la API **corren en el mismo contenedor y con el mismo usuario**, que es lo que la documentación pide en Linux.

```bash
cd cursos/dotnet-performance
docker run --rm -d --name perf-m6 --cpus 4 -u "$(id -u):$(id -g)" \
  -v "$PWD":/work -w /work/referencia \
  -e HOME=/work -e DOTNET_CLI_HOME=/work/.dotnet -e NUGET_PACKAGES=/work/.nuget \
  -e DOTNET_NOLOGO=1 -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  mcr.microsoft.com/dotnet/sdk:10.0 sleep infinity
docker exec perf-m6 bash -c 'for p in dotnet-counters dotnet-dump; do dotnet tool install --global $p; done'
docker exec perf-m6 dotnet build -c Release ApiEjemplo
docker exec perf-m6 dotnet build -c Release Carga
```

Fragmento (comandos de `docker`, no compilados como script). En la medición, las herramientas quedaron en `/work/.dotnet/.dotnet/tools`; si el shell no encuentra `dotnet-counters`, agrega esa carpeta al `PATH`:

```bash
docker exec -it perf-m6 bash
export PATH=$PATH:/work/.dotnet/.dotnet/tools
dotnet-counters --version      # 10.0.750501 en esta guía
```

Las instrucciones siguientes usan **dos terminales** dentro del contenedor (`docker exec -it perf-m6 bash` en cada una).

---

## Paso 1 - Levantar la API con la fuga

Terminal A:

```bash
ASPNETCORE_URLS=http://127.0.0.1:5081 CACHE=fuga \
  dotnet ApiEjemplo/bin/Release/net10.0/ApiEjemplo.dll
```

Terminal B:

```bash
export PATH=$PATH:/work/.dotnet/.dotnet/tools
curl -s http://127.0.0.1:5081/ping          # ok
dotnet-counters ps                           # anota el PID de ApiEjemplo.dll
curl -s http://127.0.0.1:5081/diag
```

**Salida esperada** (de la medición):

```
 121  dotnet  /usr/share/dotnet/dotnet  dotnet ApiEjemplo/bin/Release/net10.0/ApiEjemplo.dll
{"procesadores":4,...,"heapMB":6,"gc0":2,"gc1":1,"gc2":0,"entradasEnCache":237}
```

**Calienta antes de medir.** La primera petición de la API tardó más de 11 s en frío en la medición y **no se investigó la causa**. Una carga corta de calentamiento evita que ese arranque ensucie lo demás:

```bash
dotnet Carga/bin/Release/net10.0/Carga.dll "http://127.0.0.1:5081/pedido/{n}" 1 3 10
```

---

## Paso 2 - Observar con contadores

Terminal B: inicia la carga en segundo plano (4 clientes, 10 ms de pausa, 45 s) y los contadores en vivo.

```bash
dotnet Carga/bin/Release/net10.0/Carga.dll "http://127.0.0.1:5081/pedido/{n}" 4 45 10 > carga.txt &
dotnet-counters monitor -p <PID> --refresh-interval 5 \
  --counters "System.Runtime[dotnet.gc.last_collection.heap.size,dotnet.process.memory.working_set,dotnet.gc.collections],ApiEjemplo"
```

Observa el heap por generación, el working set y `ejemplo.cache.entradas`. Sal con `q`.

Para guardarlo y revisarlo después:

```bash
dotnet-counters collect -p <PID> --refresh-interval 5 --format csv -o contadores.csv \
  --counters "System.Runtime[dotnet.gc.last_collection.heap.size,dotnet.process.memory.working_set],ApiEjemplo" &
CNT=$!
```

Cuando termines (pasados 40 a 60 s): `kill -TERM $CNT; wait $CNT`. **Usa `SIGTERM`**: lanzado en segundo plano desde un script, `collect` ignoró `SIGINT` en la medición y el script se quedó esperando.

**Salida esperada** (resumen de la medición, suma de generaciones):

| Momento | Entradas | Heap | Working set |
|---|---|---|---|
| inicio de la carga | 2 270 | 51.8 MB | 147.2 MB |
| a los ~40 s | 10 269 | 221.6 MB | 330.0 MB |

---

## Paso 3 - Verlo fallar: la memoria no baja

Deja de cargar (la carga de 45 s termina sola) y **sigue mirando** los contadores otros 2 o 3 minutos.

```bash
curl -s http://127.0.0.1:5081/diag
```

**Salida esperada:** el heap se queda en **221.6 MB** y `entradasEnCache` en **10 269**; en `/diag`, `"heapMB":214` con `gc2` en 1. En la medición el heap no bajó en más de siete minutos de contadores.

Dos cuidados al interpretarlo:

- **Heap no es working set.** En esta corrida el working set bajó de 330 a unos 209 MB mientras el heap seguía en 221.6 MB. Un working set que baja **no** significa que la fuga se haya ido.
- `dotnet.gc.last_collection.heap.size` se actualiza **cuando el recolector corre**; si no hay recolecciones, el valor no cambia aunque haya asignaciones nuevas.

**Anota:** ¿qué generación creció más y qué dice eso de la vida de los objetos?

---

## Paso 4 - Tomar el volcado (sin parar la API)

```bash
dotnet-dump collect -p <PID> --type Full -o /work/fuga.dmp
ls -la /work/fuga.dmp
```

**Salida esperada:**

```
Writing full to /work/resultados/fuga.dmp
Complete
-rw------- 1 ... 553492480 ... fuga.dmp
```

(Ruta distinta en la medición; el tamaño de tu archivo será otro.) El archivo es del tamaño de la memoria del proceso: unos 553 MB en la medición.

> **Es un secreto.** Contiene todo lo que había en memoria. No lo copies fuera del contenedor, no lo subas a ningún lado y bórralo al terminar (Paso 7). Si en un caso real solo necesitas pilas y excepciones, la documentación ofrece `--type Triage` (sin datos personales).

---

## Paso 5 - ¿Qué ocupa la memoria? ¿Quién lo retiene?

```bash
dotnet-dump analyze /work/fuga.dmp -c "dumpheap -stat" -c "exit"
```

**Salida esperada** (últimas filas):

```
      11,001  205,757,768 System.Byte[]
      ...
Total 138,845 objects, 243,393,926 bytes
```

Pregunta 1: ¿qué porcentaje del heap son los `Byte[]`? (en la medición, alrededor del 85 %).

Ahora la raíz. Primero la dirección de un arreglo de 20 000 bytes (20 024 con el encabezado):

```bash
dotnet-dump analyze /work/fuga.dmp -c "dumpheap -type System.Byte[] -min 20000" -c "exit" | head -20
# toma una dirección de la primera columna
dotnet-dump analyze /work/fuga.dmp -c "gcroot <direccion>" -c "exit"
```

**Salida esperada** (recortada):

```
-> System.Func<Int32, IMemoryCache, IResult>
-> Program+<>c__DisplayClass0_0
-> ConcurrentDictionary<Int32, Byte[]>
-> ...+Tables -> ...+VolatileNode[] -> ...+Node
-> System.Byte[]
```

Pregunta 2: con tus palabras, ¿por qué el recolector no libera esos arreglos? ¿Qué línea de `Program.cs` es el origen? (busca `fuga.GetOrAdd`).

---

## Paso 6 - Corregir y volver a medir

Detén la API de la terminal A (Ctrl+C) y levántala con la caché acotada, que usa `IMemoryCache` con límite de 100 entradas y expiración por inactividad:

```bash
ASPNETCORE_URLS=http://127.0.0.1:5081 CACHE=acotada \
  dotnet ApiEjemplo/bin/Release/net10.0/ApiEjemplo.dll
```

Repite el calentamiento, la misma carga (4 clientes, 45 s) y los contadores del Paso 2.

**Salida esperada** (de la medición):

| | Fuga | Acotada |
|---|---|---|
| Peticiones en 45 s | 10 269 | 15 275 |
| Heap (contadores) | 51.8 a 221.6 MB | ~3.0 a 3.1 MB |
| Working set | 147 a 330 MB | ~103 a 106 MB |
| `/diag` al final | heapMB 214, gc0 7, gc1 2, gc2 1 | heapMB 4, gc0 30, gc1 27, gc2 1 |

La versión acotada hace **más** recolecciones de gen 0 y gen 1 y casi ninguna de gen 2: sus objetos mueren jóvenes. Ese es el costo normal de no retener.

**Anota:** ¿cuál fue tu cambio de política (tamaño y expiración) y qué cifra lo demuestra?

---

## Paso 7 - Limpiar

```bash
rm -f /work/fuga.dmp /work/contadores.csv
exit                      # de cada terminal
docker stop perf-m6       # el contenedor se borra solo (--rm)
```

Comprueba que no quedan volcados: `ls /work/*.dmp` no debe encontrar nada. No dejes procesos ni contenedores corriendo.

---

## Extra - Una traza de tiempo

Con la API en marcha y carga sobre `/reporte?pedidos=3000&version=1&n={n}` (2 clientes):

```bash
dotnet tool install --global dotnet-trace
dotnet-trace collect -p <PID> --duration 00:00:00:12 -o cpu.nettrace
dotnet-trace report cpu.nettrace topN -n 12
```

Las primeras filas serán esperas de hilos ociosos (`Monitor.Wait`, semáforos). **Sáltalas** y busca el lambda del endpoint, `String.Concat` y `Buffer.MemmoveInternal`. En la medición, el lambda del endpoint tuvo 15.1 % de tiempo inclusivo. Una traza no es un diagnóstico: dice dónde mirar.

---

## Lo que debes entregar

1. Una captura de los contadores con la memoria creciendo y la misma captura **después** de parar la carga.
2. La salida de `dumpheap -stat` (últimas filas) y de `gcroot`.
3. Tu cambio de código o de configuración y la tabla antes/después.
4. Una línea: cómo borraste el volcado.
