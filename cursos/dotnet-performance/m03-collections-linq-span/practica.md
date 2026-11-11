# Práctica - M3 - Colecciones, LINQ y Span

**.NET de alto rendimiento**
**Duración:** 40 a 50 minutos, individual, en tu máquina (o en un contenedor `mcr.microsoft.com/dotnet/sdk:10.0`).

---

## Objetivo

Reescribir un procesamiento de líneas de texto con `Span<T>`, medirlo contra la versión con `Split` y **decir qué parte de la diferencia es firme y cuál es ruido**. En el camino verás fallar una comparación mal hecha.

El código de partida es [`referencia/Bench/M3_Colecciones.cs`](../referencia/Bench/M3_Colecciones.cs), clase `ParsearLineas`. Si lo corres en una máquina distinta, **los tiempos cambiarán**; los bytes asignados deben coincidir con los de esta guía.

---

## Antes de empezar

```bash
cd cursos/dotnet-performance/referencia
dotnet --version        # esta guía se midió con 10.0.403
```

Si usas contenedor (recomendado en un servidor compartido: uno a la vez, y bórralo al terminar):

```bash
docker run --rm -it --cpus 4 -v "$PWD":/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 bash
```

Los resultados de esta guía se midieron con: .NET 10 (SDK 10.0.403, runtime 10.0.14), BenchmarkDotNet 0.15.8, trabajo `ShortRun`, i5-11500, contenedor con 4 CPU, en un servidor compartido y con carga.

---

## Paso 1 - La línea base

```bash
dotnet run -c Release --project Bench -- --filter "*ParsearLineas*" --job short
```

Tarda unos minutos. **Salida esperada** (los tiempos varían; la columna `Allocated` no):

```
| Method   | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|--------- |---------:|---------:|---------:|------:|--------:|--------:|----------:|------------:|
| ConSplit | 406.7 us | 890.2 us | 48.79 us |  1.01 |    0.14 | 38.0859 |  238968 B |        1.00 |
| ConSpan  | 325.4 us | 178.2 us |  9.77 us |  0.81 |    0.08 |       - |         - |        0.00 |
```

Anota en tu hoja:

- Bytes asignados por operación de cada versión.
- El `Error` de cada una **comparado con la diferencia de las medias**.

---

## Paso 2 - Leer el código de `ConSpan`

```csharp
ReadOnlySpan<char> resto = linea;
int coma = resto.IndexOf(',');
int id = int.Parse(resto[..coma], CultureInfo.InvariantCulture);
resto = resto[(coma + 1)..];
coma = resto.IndexOf(',');
resto = resto[(coma + 1)..];          // el nombre del cliente se salta
coma = resto.IndexOf(',');
total += id + decimal.Parse(resto[..coma], CultureInfo.InvariantCulture);
```

Contesta por escrito, sin correr nada:

1. ¿Qué se copia de la línea en esta versión?
2. ¿Por qué `resto = resto[(coma + 1)..]` no asigna?
3. ¿Qué pasaría con este código si una línea tuviera solo dos campos?

(La respuesta a la 1 está en la [documentación de span](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/); la 3, la verás en el paso 5.)

---

## Paso 3 - Verlo fallar: un benchmark que "gana" sin merecerlo

Este es un **fragmento** (no está en el repositorio) de lo que hace quien olvida comprobar que sus dos versiones hacen lo mismo. Agrégalo a una copia de `ParsearLineas`:

```csharp
// Fragmento: una "optimización" tramposa
[Benchmark]
public decimal Tramposo()
{
    decimal total = 0;
    foreach (string linea in _lineas)
    {
        ReadOnlySpan<char> resto = linea;
        int coma = resto.IndexOf(',');
        total += int.Parse(resto[..coma], CultureInfo.InvariantCulture);   // olvidó el monto
    }
    return total;
}
```

Córrelo junto con los otros y anota lo que marca `Tramposo` (tiempo y `Allocated`): al hacer menos trabajo, es de esperar que **parezca ganar**; compruébalo tú. Pero devuelve otra cosa: no suma los montos.

Ahora arregla el benchmark, no el método. En el `[GlobalSetup]`, agrega:

```csharp
if (ConSplit() != Tramposo()) throw new InvalidOperationException("No coinciden.");
```

Vuelve a correr. **Lo esperado:** el `GlobalSetup` lanza `No coinciden.` y el benchmark no publica una tabla con esa comparación. Anota el mensaje que ves. Eso es lo correcto: la comparación era inválida.

Quita `Tramposo` al terminar el paso.

---

## Paso 4 - Tu versión con Span

Escribe `ConSpanTuyo` con una variante: en lugar de `IndexOf` repetido, usa `resto.Slice(...)` o, si tu versión de .NET la ofrece, `MemoryExtensions.Split` (consulta primero su [documentación](https://learn.microsoft.com/en-us/dotnet/api/system.memoryextensions.split): firma y comportamiento).

Condiciones:

- Debe dar **el mismo total** que `ConSplit` (agrega la comprobación al `GlobalSetup`).
- Corre el benchmark con las tres versiones.
- Anota `Allocated` de la tuya.

Si tu versión no baja a 0 B, busca qué línea asigna (módulo 2: `new`, `string`, `ToArray`, LINQ con captura).

---

## Paso 5 - Una línea mal formada

Agrega, en una copia del proyecto de consola, una llamada con la línea `"7,Ana"` (solo dos campos) a `ConSpan` y a `ConSplit`.

**Lo que debes observar, sin inventarlo:** una de las dos versiones lanza una excepción. Anota **cuál**, qué tipo de excepción es y en qué línea del código se produce. Esa excepción es la prueba de que optimizar también te hace responsable de validar la entrada que antes `Split` toleraba "por accidente".

Después escribe tu corrección (por ejemplo, verificar que `coma >= 0` antes de cortar) y su prueba.

---

## Paso 6 - ArrayPool: el LOH a la vista

```bash
dotnet run -c Release --project Bench -- --filter "*Buffers*" --job short
```

**Salida esperada** (columnas relevantes):

```
| Method          | Tamano | Mean         | Gen0    | Gen1    | Gen2    | Allocated |
| ArregloNuevo    | 4096   |    464.95 ns |  0.6561 |       - |       - |    4120 B |
| ArrayPoolRentar | 4096   |     37.63 ns |       - |       - |       - |         - |
| ArregloNuevo    | 100000 | 19,179.58 ns | 31.2195 | 31.2195 | 31.2195 |  100034 B |
| ArrayPoolRentar | 100000 |     32.05 ns |       - |       - |       - |         - |
```

Contesta:

1. ¿Por qué con 100 000 bytes aparece Gen2 y con 4 096 no? (Pista: módulo 2, el umbral es de 85 000 bytes.)
2. Cambia `Tamano` a 84 000 y a 90 000 en `[Params]`. ¿Dónde se mueve la columna Gen2? Anota tus cifras (no las de esta guía: no las medimos).

---

## Paso 7 - La conclusión por escrito

Completa en tu hoja una tabla como esta con **tus** cifras:

| Pregunta | Dato firme (Allocated) | Dato ruidoso (tiempo) | Decisión |
|---|---|---|---|
| ¿`Split` o `Span`? | | | |
| ¿`new byte[]` o `ArrayPool`? | | | |

Regla: una conclusión sobre tiempo solo se escribe como "mejor" si la diferencia de las medias es **mayor** que el `Error` de ambas filas. Si no, se escribe "no concluyente".

---

## Si te atoras

- BenchmarkDotNet dice que el ensamblado "is non-optimized": corriste en Debug; usa `-c Release`.
- `GlobalSetup` lanza `No coinciden.`: tu versión devuelve otra cosa. Compara el valor de las dos con `Console.WriteLine` en un proyecto aparte.
- Un contenedor tarda en restaurar paquetes la primera vez: es normal.

---

## Limpieza

```bash
exit            # sale del contenedor; --rm lo borra
```

No dejes procesos de BenchmarkDotNet vivos: `ps aux | grep Bench`, y mata por PID si queda alguno.
