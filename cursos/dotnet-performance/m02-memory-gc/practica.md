# Práctica - M2 - Memoria y recolector de basura

**.NET de alto rendimiento**
**Duración:** 45 a 60 minutos, individual, en tu máquina (o en un contenedor `mcr.microsoft.com/dotnet/sdk:10.0`).

---

## Objetivo

Medir las asignaciones de un endpoint, **bajarlas a la mitad o menos** con un cambio que puedas explicar, y demostrar con la medición (no con la intuición) que lo lograste. En el camino verás fallar dos cosas: la concatenación con `+=` y una demo de asignaciones que el JIT "arregla" por ti.

El código está en [`referencia/`](../referencia/README.md): la API de ejemplo ([`ApiEjemplo/Program.cs`](../referencia/ApiEjemplo/Program.cs)), las demos ([`Demos/Program.cs`](../referencia/Demos/Program.cs)) y el benchmark [`Bench/M2_Memoria.cs`](../referencia/Bench/M2_Memoria.cs). Los resultados de esta guía se midieron con .NET 10 (SDK 10.0.403, runtime 10.0.14), BenchmarkDotNet 0.15.8, trabajo `ShortRun`, i5-11500, contenedor con 4 CPU, en un servidor compartido y con carga. **Los tiempos cambian en tu máquina; los bytes asignados no deberían.**

> Corre la API **solo en tu máquina**, en local. Nunca apuntes una prueba a un servicio real.

---

## Antes de empezar

```bash
cd cursos/dotnet-performance/referencia
dotnet --version        # esta guía se midió con 10.0.403
```

Si usas contenedor (uno a la vez, y bórralo al terminar):

```bash
docker run --rm -it --cpus 4 -v "$PWD":/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 bash
```

---

## Paso 1 - Las generaciones, con tus ojos

```bash
dotnet run -c Release --project Demos -- gc
```

Las tres primeras líneas deben decir generación 0, 1 y 2 (el mismo objeto, promovido tras cada recolección). La salida medida:

```
recien creado:         generacion 0
tras un GC de gen0:    generacion 1
tras un GC de gen1:    generacion 2
```

Anota: ¿qué le pasa a un objeto que sobrevive a una recolección?

---

## Paso 2 - Verlo fallar (1): el JIT se come tu asignación

La última parte de `gc` asigna 1,000,000 de `byte[100]`. Ábrela y mira esta línea:

```csharp
for (int i = 0; i < 1_000_000; i++) Estado.Destino = new byte[100];
```

Corre el demo y anota los MB asignados y cuántas recolecciones de gen 0 hubo. **Salida medida:**

```
1 000 000 de byte[100]: 128.0 MB asignados (medido con GetAllocatedBytesForCurrentThread)
colecciones durante el ciclo: gen0 +20, gen1 +0, gen2 +0
```

Ahora rómpelo a propósito: cambia la línea por `_ = new byte[100];` (el arreglo ya no se usa ni se guarda) y corre otra vez. En la medición del curso salió así:

```
1 000 000 de byte[100]: 1.3 MB asignados
colecciones durante el ciclo: gen0 +0, gen1 +0, gen2 +0
```

La misma cantidad de `new`, 1.3 MB en lugar de 128.0 MB. Pregúntate **qué estabas midiendo**. La documentación de rendimiento de .NET 10 describe que el JIT amplió el *escape analysis* y puede asignar en el stack arreglos que "nunca salen del marco" ([Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/)). Regla: **usa el resultado de lo que asignas** o tu medición no vale.

Vuelve a dejar la línea como estaba.

---

## Paso 3 - El LOH en Linux

```bash
dotnet run -c Release --project Demos -- loh
```

**Salida medida:**

```
byte[84975] nace en gen0; byte[84976] nace directo en gen2 (LOH)
byte[85000]: generacion 2
double[10600]: generacion 0  |  double[10700]: generacion 2
```

Anota: ¿a cuántos **bytes** (no elementos) pasa un arreglo al LOH? ¿Por qué `double[10700]` está en gen 2 y `byte[10700]` no? La documentación dice 85,000 bytes y describe Windows; aquí lo viste en Linux.

---

## Paso 4 - Mide el endpoint (la línea base)

Terminal 1:

```bash
ASPNETCORE_URLS=http://127.0.0.1:5080 dotnet run -c Release --project ApiEjemplo
```

Terminal 2:

```bash
curl -s -D - -o /dev/null "http://127.0.0.1:5080/reporte?pedidos=100&version=1"  | grep -i x-bytes
curl -s -D - -o /dev/null "http://127.0.0.1:5080/reporte?pedidos=1000&version=1" | grep -i x-bytes
```

**Salida medida** (la cifra puede variar un poco con el tamaño de los nombres, pero debe ser del mismo orden):

```
X-Bytes-Asignados: 214968
X-Bytes-Asignados: 23055296
```

Esa cabecera es `GC.GetAllocatedBytesForCurrentThread` antes y después del trabajo. **Esta es tu línea base.** Anótala con el número de pedidos.

---

## Paso 5 - Encuentra de dónde salen los bytes

Abre el endpoint en [`ApiEjemplo/Program.cs`](../referencia/ApiEjemplo/Program.cs), rama `version == 1`. Sin mirar la rama 2, contesta por escrito:

1. ¿Cuántas cadenas nuevas se crean por cada pedido?
2. ¿Qué crece más rápido, el texto final o la basura intermedia? (Pista: compara los 23 MB con el tamaño de un CSV de 1000 líneas cortas.)
3. ¿Cuál cambio recortaría **más**, y cuál es un detalle menor?

---

## Paso 5b - Verlo fallar (2): el costo cuadrático

Mide el endpoint con 10 veces más pedidos:

```bash
curl -s -D - -o /dev/null "http://127.0.0.1:5080/reporte?pedidos=10000&version=1" | grep -i x-bytes
```

No hay cifra del curso para este caso: **anótala tú** y compárala con la de 1000 pedidos. Si el crecimiento fuera lineal, serían unas 10 veces más; con `+=` debe ser claramente más. Esa diferencia es el crecimiento cuadrático que viste en la clase (de 1816 B con 10 piezas a 17 628 136 B con 1000 piezas, en `UnirTextos`).

---

## Paso 6 - Llega a la mitad

Haz **tu propio cambio** en la rama `version == 1` (o en una copia como `version == 3`). La meta: asignar **la mitad o menos** que la línea base **con el mismo texto de salida**. Compruébalo con la cabecera y con el texto:

```bash
curl -s "http://127.0.0.1:5080/reporte?pedidos=1000&version=1" | md5sum
curl -s "http://127.0.0.1:5080/reporte?pedidos=1000&version=2" | md5sum
```

Los dos hash deben coincidir: un cambio que altera el resultado no es una mejora (módulo 1). En la corrida de referencia, con `pedidos=1000`, las dos versiones dieron `4904bce304e56e96ec9cfda5603575ae`.

Cuando llegues, compara con la versión 2 que ya trae el endpoint. **Salida medida de la versión 2:**

```
version=2 pedidos=100  -> X-Bytes-Asignados: 14600
version=2 pedidos=1000 -> X-Bytes-Asignados: 150760
```

Con 1000 pedidos bajó de 23,055,296 a 150,760 bytes: mucho menos de la mitad.

---

## Paso 7 - Lo mismo, con BenchmarkDotNet

```bash
dotnet run -c Release --project Bench -- --filter "*ReporteCsv*" --job short
```

**Salida medida** (los tiempos varían; la columna `Allocated` no):

```
| Method           | Pedidos | Allocated   | Gen0      | Gen1     |
| Concatenando     | 100     |   339.35 KB |   55.3589 |   0.9155 |
| ConStringBuilder | 100     |    15.18 KB |    2.4719 |   0.0610 |
| ConCapacidad     | 100     |    14.43 KB |    2.3499 |   0.0610 |
| Concatenando     | 1000    | 33665.39 KB | 5460.9375 | 859.3750 |
| ConStringBuilder | 1000    |   146.73 KB |   23.4375 |   4.3945 |
| ConCapacidad     | 1000    |   145.45 KB |   23.4375 |   2.9297 |
```

(El benchmark formatea con `CultureInfo.InvariantCulture` y la fecha; el endpoint, sin fecha: por eso los bytes no coinciden entre las dos tablas.)

Contesta: ¿cuál de los dos cambios (`StringBuilder`, capacidad inicial) explica casi todo el ahorro?

---

## Qué debes entregar

Una hoja con: línea base (bytes, con cuántos pedidos), tu cambio, bytes después, el `md5sum` igual, el entorno de la medición y una frase sobre qué cifras son firmes (bytes) y cuáles ruido (tiempos).

---

## Limpieza

Detén la API con `Ctrl+C` y sal del contenedor (`exit`; con `--rm` se borra solo).
