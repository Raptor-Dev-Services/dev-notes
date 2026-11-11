# 47 - Benchmarking: medir antes de optimizar

Una optimización sin medición antes y después es una opinión. Esta nota explica cómo medir una función con BenchmarkDotNet, cómo leer su tabla y qué errores hacen que un benchmark mienta. Continúa la nota [19 - Performance](19-performance.md), que da el proceso general; aquí está el instrumento.

> Las cifras de esta nota se midieron con BenchmarkDotNet 0.15.8 en .NET 10 (SDK 10.0.403, runtime 10.0.14), trabajo `ShortRun`, en un Intel Core i5-11500 dentro de un contenedor de Linux, en un servidor compartido y con carga. Los **tiempos son ruidosos** y cambian de una máquina a otra; las **asignaciones** no dependen de la carga. El código está en [`cursos/dotnet-performance/referencia/Bench/`](../cursos/dotnet-performance/referencia/Bench/).

---

## Por qué no basta un `Stopwatch`

Cronometrar una sola llamada mide, sobre todo, el compilador JIT. En el demo del curso, la misma función de 100 000 vueltas tardó unos 1 004 µs la primera vez y entre 78 y 95 µs las siete siguientes (`dotnet run -c Release --project Demos -- jit`, en [`referencia/Demos`](../cursos/dotnet-performance/referencia/Demos/Program.cs)). Un `Stopwatch` suelto no separa el calentamiento del costo real, no repite, no da dispersión y no cuenta asignaciones. BenchmarkDotNet hace el calentamiento, repite hasta que las mediciones se estabilizan y reporta el error y la desviación.

---

## Un benchmark mínimo

```csharp
[MemoryDiagnoser]
public class ContarPalabras
{
    private string _texto = "";

    [Params(100, 10_000)]
    public int Palabras;

    [GlobalSetup]
    public void Preparar()
    {
        _texto = GenerarTexto(Palabras);
        // Un benchmark que compara dos métodos que dan resultados distintos no mide nada útil.
        if (ConSplit() != ConCiclo())
            throw new InvalidOperationException("Las dos implementaciones no coinciden.");
    }

    [Benchmark(Baseline = true)]
    public int ConSplit() => _texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    [Benchmark]
    public int ConCiclo() { /* recorre los caracteres y cuenta palabras */ }
}
```

Fragmento: el archivo completo está en `referencia/Bench/M1_Medir.cs`. Se corre en Release:

```bash
dotnet run -c Release --project Bench -- --filter "*ContarPalabras*"
```

- `[Benchmark(Baseline = true)]` marca la implementación contra la que se compara: la columna `Ratio` sale de ahí.
- `[Params]` repite el benchmark con cada valor: un resultado a un solo tamaño no dice cómo escala.
- `[GlobalSetup]` prepara los datos una vez, fuera de lo que se mide.
- `[MemoryDiagnoser]` agrega las columnas `Gen0`, `Gen1`, `Gen2` y `Allocated`.
- Devolver el resultado evita que el JIT elimine el cálculo por no usarse (*dead code elimination*).

---

## Cómo leer la tabla

Corrida real (`--job short`, 4 CPU del contenedor, servidor con carga):

| Method   | Palabras | Mean         | Ratio | Gen0    | Allocated |
|----------|----------|-------------:|------:|--------:|----------:|
| ConSplit | 100      |   2 893.4 ns |  1.00 |  0.8430 |    5304 B |
| ConCiclo | 100      |     694.0 ns |  0.24 |       - |         - |
| ConSplit | 10000    | 1 156 494 ns |  1.00 | 89.8438 |  526039 B |
| ConCiclo | 10000    |   179 277 ns |  0.16 |       - |         - |

- **`Allocated`** es la columna que sostiene la conclusión: `Split` asigna 5 304 B con 100 palabras y 526 039 B con 10 000; el ciclo, nada. Esa cifra no depende de qué tan cargado esté el servidor.
- **`Mean`** y **`Ratio`** sí dependen. En esta corrida el `Error` de `ConSplit` a 10 000 palabras (842 878 ns) es del tamaño de lo que se midió: con `ShortRun` hay solo tres iteraciones. El orden (el ciclo es más rápido) se sostiene; la razón de esa corrida (0.16 a 0.24) no es una constante: en otra máquina será otra.
- **`Gen0`, `Gen1`, `Gen2`:** número de recolecciones por cada 1000 operaciones de ese nivel (documentación de BenchmarkDotNet).

---

## Lo que hace mentir a un benchmark

1. **Correrlo en Debug.** BenchmarkDotNet se niega: `Benchmark was built without optimization enabled (most probably a DEBUG configuration). Please, build it in RELEASE.` Su guía de buenas prácticas dice "Never use the Debug build for benchmarking".
2. **No comprobar que las dos versiones dan lo mismo.** Un método más rápido que devuelve otra cosa no es una mejora.
3. **Dejar que el JIT elimine el trabajo.** Si el resultado no se usa, puede desaparecer: devuélvelo.
4. **Datos de un solo tamaño.** Usa `[Params]` con un caso chico y uno grande.
5. **Extrapolar.** La guía oficial dice "Please, don't extrapolate your results": el resultado vale para esa máquina, ese runtime y esos datos.
6. **Medir con la máquina ocupada.** Cierra lo demás. Si no puedes, apóyate en `Allocated`.

---

## Más allá del microbenchmark

Un benchmark responde "¿esta función es más barata?". No responde "¿por qué está lenta mi API?". Para eso se usan otras herramientas, y se eligen según la pregunta:

| Pregunta | Herramienta |
|---|---|
| ¿Cuál de dos implementaciones es mejor? | BenchmarkDotNet |
| ¿Cuánto tarda y cuántos errores da el servicio bajo carga? | Una prueba de carga contra una instancia de prueba |
| ¿Dónde se va el tiempo de CPU del proceso? | Un perfilador o `dotnet-trace` |
| ¿Cómo crece la memoria y cuántas recolecciones hay? | `dotnet-counters` |
| ¿Qué objetos ocupan la memoria? | `dotnet-dump` |

Las tres últimas se ven en la nota [48 - Diagnóstico en producción](48-diagnostico-en-produccion.md).

---

## Checklist

- [ ] ¿Está en Release y sin depurador?
- [ ] ¿Hay una línea base y una comprobación de que los resultados coinciden?
- [ ] ¿Se mide con más de un tamaño de entrada?
- [ ] ¿Se usó `[MemoryDiagnoser]` y la conclusión se apoya en `Allocated`?
- [ ] ¿Se dice en qué máquina, qué runtime y qué carga se midió?

---

## Referencias

- BenchmarkDotNet, *Good practices*: https://benchmarkdotnet.org/articles/guides/good-practices.html
- BenchmarkDotNet, *Diagnosers* (MemoryDiagnoser, columnas Gen): https://benchmarkdotnet.org/articles/configs/diagnosers.html
- *High-Performance Programming in C# and .NET* (Alls, 2022), cap. 5. *Tools and Skills for .NET 10* (Price, 2025), cap. 13.
- Mediciones propias: `cursos/dotnet-performance/referencia/` (.NET 10, a noviembre de 2026).

---

## Glosario

| Término | Definición |
|---------|-----------|
| Benchmark | medición repetible del costo de un fragmento de código, con calentamiento y estadística |
| Línea base (baseline) | la implementación contra la que se compara; su `Ratio` es 1.00 |
| JIT | compilador que convierte el IL a código de máquina al ejecutar; la primera llamada incluye su costo |
| Dead code elimination | el JIT descarta cálculos cuyo resultado no se usa |
| `[MemoryDiagnoser]` | atributo de BenchmarkDotNet que agrega las columnas de recolecciones y memoria asignada |
| `Allocated` | bytes asignados por operación; no depende de la carga de la máquina |
| ShortRun | trabajo de BenchmarkDotNet con pocas iteraciones: rápido, pero con más error |

---

*Rogelio Arriaga Gonzalez*
