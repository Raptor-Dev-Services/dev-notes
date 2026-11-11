# Práctica - M1 - Medir antes de tocar

**.NET de alto rendimiento**
**Duración:** 45 minutos, individual.

---

## Objetivo

1. Correr un benchmark de **dos implementaciones de la misma función** y leer su tabla.
2. **Verlo fallar, parte a:** compilarlo en Debug y leer el rechazo de BenchmarkDotNet.
3. **Verlo fallar, parte b:** medir con un `Stopwatch` de una sola llamada y ver el costo del JIT.
4. Escribir una conclusión que lleve su entorno y su ruido.

Todo corre en un contenedor desechable con el SDK de .NET 10; no se instala nada en tu máquina.

---

## Antes de empezar

Necesitas Docker y el código de [`referencia/`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/README.md) en una carpeta tuya:

```bash
cp -r cursos/dotnet-performance/referencia ~/perf-m1
cd ~/perf-m1
```

Abre un contenedor con el SDK. Se borra al salir (`--rm`) y los paquetes de NuGet se quedan en tu carpeta:

```bash
docker run --rm -it -v "$PWD":/work -w /work \
  -e HOME=/work -e NUGET_PACKAGES=/work/.nuget -e DOTNET_NOLOGO=1 \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  mcr.microsoft.com/dotnet/sdk:10.0 bash
```

Dentro del contenedor:

```bash
dotnet --version
```

Las cifras de esta guía se midieron con la versión `10.0.403` del SDK. Si ves otra, tus números serán distintos y tu **conclusión** debe ser la misma.

> **Si tu máquina está ocupada**, el tiempo se mueve mucho. Cierra lo que puedas. La columna `Allocated` no depende de eso.

---

## Paso 1 - Leer el benchmark antes de correrlo

Abre `Bench/M1_Medir.cs` y localiza:

- la clase `ContarPalabras` y su `[MemoryDiagnoser]`;
- `[Params(100, 10_000)]`: se medirá con dos tamaños;
- el `[GlobalSetup]`, que arma el texto **y comprueba** que `ConSplit()` y `ConCiclo()` dan el mismo número;
- el `Baseline = true` en `ConSplit`.

Contesta por escrito: **¿qué implementación es la línea base?** y **¿qué pasaría con el benchmark si `ConCiclo` contara distinto?**

---

## Paso 2 - Correrlo en Release

```bash
cd /work
dotnet run -c Release --project Bench -- --filter "*ContarPalabras*" --job short
```

- `--filter` elige la clase; sin él, BenchmarkDotNet te pregunta qué correr.
- `--job short` hace pocas iteraciones: tarda menos y es **menos preciso**. Para publicar una cifra se usa el trabajo por omisión.

La primera vez descarga paquetes y compila; después tarda unos minutos.

**Salida esperada** (medida con BenchmarkDotNet 0.15.8, .NET 10.0.14, i5-11500, contenedor con 4 CPU, servidor con carga; se omitieron columnas):

```text
| Method   | Palabras | Mean           | Error        | Ratio | Gen0    | Gen1    | Gen2    | Allocated |
|--------- |--------- |---------------:|-------------:|------:|--------:|--------:|--------:|----------:|
| ConSplit | 100      |     2,893.4 ns |   1,623.5 ns |  1.00 |  0.8430 |  0.0076 |       - |    5304 B |
| ConCiclo | 100      |       694.0 ns |     338.4 ns |  0.24 |       - |       - |       - |         - |
| ConSplit | 10000    | 1,156,494.0 ns | 842,878.4 ns |  1.00 | 89.8438 | 50.7813 | 25.3906 |  526039 B |
| ConCiclo | 10000    |   179,276.8 ns |  42,735.7 ns |  0.16 |       - |       - |       - |         - |
```

Tus **tiempos** serán otros. Tu columna `Allocated` debe ser prácticamente la misma.

**Anota en tu hoja:**

| Dato | Tu valor |
|---|---|
| Procesador y hilos (la línea `... CPU, N logical and M physical cores` de la salida) | |
| Versión de .NET (línea `.NET SDK`) | |
| `Allocated` de `ConSplit` y de `ConCiclo`, con 100 palabras | |
| `Error` de `ConSplit` con 100 palabras, como porcentaje de su `Mean` | |

---

## Paso 3 - Verlo fallar (a): el benchmark en Debug

Corre exactamente lo mismo, pero en Debug:

```bash
dotnet run -c Debug --project Bench -- --filter "*ContarPalabras*"
```

**Salida esperada** (las últimas líneas, medida con la misma configuración):

```text
// Validating benchmarks:
//    * Assembly Bench which defines benchmarks is non-optimized
Benchmark was built without optimization enabled (most probably a DEBUG configuration). Please, build it in RELEASE.
```

Lee el mensaje: BenchmarkDotNet **no mide** y dice por qué y cómo arreglarlo. La guía oficial lo justifica: "Never use the Debug build for benchmarking. _Never_." (benchmarkdotnet.org, *Good practices*).

**Preguntas:**

1. ¿Qué parte del mensaje te dice cuál es la causa probable?
2. ¿Qué habría pasado si hubieras medido con un `Stopwatch` en Debug y nadie te avisara?

---

## Paso 4 - Verlo fallar (b): un cronómetro de una sola llamada

Corre el demo `jit`, que mide la misma función ocho veces seguidas con `Stopwatch`:

```bash
dotnet run -c Release --project Demos -- jit
```

La salida empieza con una línea que dice la configuración y el runtime, y sigue una línea por llamada. **Salida esperada** (medida con .NET 10.0.14, Release):

```text
llamada 1:    1003.9 us  (resultado 333328333350000)
llamada 2:      94.9 us  (resultado 333328333350000)
llamada 3:      85.7 us  (resultado 333328333350000)
llamada 4:      82.7 us  (resultado 333328333350000)
llamada 5:      85.7 us  (resultado 333328333350000)
llamada 6:      90.7 us  (resultado 333328333350000)
llamada 7:      83.3 us  (resultado 333328333350000)
llamada 8:      78.5 us  (resultado 333328333350000)
```

- El **resultado** no cambia: es la misma función con la misma entrada.
- El **tiempo** de la primera llamada fue ≈ 1,004 µs; el de las siguientes, entre 78 y 95 µs.

La documentación de Microsoft explica la causa general: .NET usa compilación por niveles, y el primer nivel "generates code more quickly (quick JIT) or loads pre-compiled code" mientras el segundo "generates optimized code in the background" (Microsoft Learn, *Compilation config settings*).

**Tarea de este paso:** corre el demo dos veces más y anota cuánto tarda **tu** primera llamada y el rango de las siguientes. ¿Se parece al de arriba? ¿Qué cifra habrías publicado si solo hubieras medido una llamada?

### Variante: el mismo demo en Debug

```bash
dotnet run -c Debug --project Demos -- jit
```

En la corrida del curso, las llamadas 2 a 8 en Debug salieron entre 118 y 150 µs, contra 78 a 95 µs en Release. La diferencia existe pero es **moderada** para una función así de simple; no la generalices. Y la primera llamada en Debug (280.1 µs) tampoco guarda la misma proporción: es un dato más de que una sola medición no dice nada.

---

## Paso 5 - Romper la comprobación de igualdad

Para ver por qué el `GlobalSetup` compara los resultados, **rompe a propósito** `ConCiclo`: en `Bench/M1_Medir.cs`, cambia `palabras++` por `palabras += 2`, y corre de nuevo el comando del paso 2.

BenchmarkDotNet falla durante el `GlobalSetup`. Salida medida (BenchmarkDotNet 0.15.8, .NET 10):

```
System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.InvalidOperationException: Las dos implementaciones no coinciden.
```

Sin esa comprobación, el benchmark habría medido, con toda seriedad, una función **incorrecta** más rápida. Revierte el cambio.

---

## Paso 6 - Tu propia comparación

Agrega un **tercer método** a `ContarPalabras` (sin cambiar los otros): por ejemplo, una versión con `string.Split(' ')` sin `RemoveEmptyEntries` más un `Count` de las entradas no vacías, o cualquiera que se te ocurra. Debe cumplir:

- da el **mismo resultado** que `ConSplit` (agrega su comparación al `GlobalSetup`);
- se mide con los **mismos** tamaños;
- lo corres en Release.

Anota su `Allocated` y su `Ratio`.

---

## Paso 7 - La conclusión

Escribe, en cinco líneas como máximo, una conclusión con este molde:

> En **<ENTORNO>**, con BenchmarkDotNet y `[MemoryDiagnoser]`, `ConCiclo` asigna **<BYTES>** por llamada y `ConSplit` **<BYTES>** con 100 palabras. El tiempo de `ConCiclo` fue menor en esa corrida (ratio <RATIO>), pero su `Error` fue <ERROR>, así que el tiempo es **<POCO PRECISO / PRECISO>**.

Sustituye cada `<...>` por tu dato. Si no puedes llenar un hueco, te falta una medición.

---

## Salir y limpiar

```bash
exit
```

El contenedor se borró solo. Tu carpeta `~/perf-m1` conserva el código y los paquetes; puedes borrarla cuando termines con la tarea.

---

## Si algo falla

| Síntoma | Causa probable |
|---|---|
| `Permission denied` al restaurar paquetes | Falta `-e HOME=/work -e NUGET_PACKAGES=/work/.nuget` en el `docker run` |
| BenchmarkDotNet pregunta qué benchmark correr | Falta `--filter "*ContarPalabras*"` |
| La corrida tarda más de 10 minutos | La máquina está muy cargada; usa `--job short` o vuelve a correr después |
| Tus tiempos varían mucho entre corridas | Es lo esperable en una máquina compartida; fíate de `Allocated` y repite |
