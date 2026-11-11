# Glosario - M1 - Medir antes de tocar

> Definiciones de los términos técnicos del módulo 1.
> Ordenado alfabéticamente.

---

## A

### Allocated
**Categoría:** BenchmarkDotNet
**Definición:** Columna que informa la memoria administrada asignada por una sola operación del benchmark. Se obtiene con `GC.GetAllocatedBytesForCurrentThread` (documentación de BenchmarkDotNet), por eso no depende de qué tan cargada esté la máquina.
**En el curso:** `ConSplit` con 100 palabras asignó 5304 B y `ConCiclo`, 0 B.

---

### Alloc Ratio
**Categoría:** BenchmarkDotNet
**Definición:** Igual que `Ratio`, pero sobre la memoria asignada: la de este método entre la de la línea base.
**En el curso:** `ConStringBuilder` con N = 1000 salió con 0.003 contra `Concatenar`.

---

## B

### Baseline
**Categoría:** BenchmarkDotNet
**Definición:** El método marcado con `Baseline = true`. Es la referencia contra la que se calcula la columna `Ratio`. Normalmente es la implementación que hoy tienes en producción.
**En el curso:** `ConSplit` era la línea base de `ContarPalabras`.

---

### Benchmark
**Categoría:** Medición
**Definición:** Medición repetida y controlada del costo de un fragmento de código. Un *microbenchmark* aísla una función; no sustituye al perfil de la aplicación completa.
**En el curso:** `ContarPalabras` compara dos implementaciones de la misma función.

---

### BenchmarkDotNet
**Categoría:** Herramienta
**Definición:** Librería de .NET para escribir microbenchmarks. Ejecuta la medición en un proceso aparte, con etapas de piloto, calentamiento y medición, y reporta media, error y desviación.
**En el curso:** versión 0.15.8, con .NET 10.

---

## C

### Calentamiento (warmup)
**Categoría:** Medición
**Definición:** Ejecuciones previas a la medición para que el código ya esté compilado por el JIT. BenchmarkDotNet lo hace por ti; un `Stopwatch` suelto no.
**En el curso:** la primera llamada del demo `jit` tardó ≈ 1,004 µs; las siguientes, entre 78 y 95 µs.

---

### Compilación por niveles (tiered compilation)
**Categoría:** Runtime
**Definición:** Mecanismo del runtime de .NET por el que un método pasa por dos niveles: uno que genera código rápidamente (o carga código precompilado) y otro que genera código optimizado en segundo plano. Está activa por omisión desde .NET Core 3.0 (Microsoft Learn).
**En el curso:** explica, en general, por qué las primeras llamadas de un método no representan su costo estable.

---

## D

### Dead code elimination
**Categoría:** Compilador
**Definición:** Optimización del JIT que elimina cálculos cuyo resultado no se usa. En un benchmark puede dejar un método vacío. Se evita devolviendo el resultado.
**En el curso:** los métodos `[Benchmark]` devuelven su `int`.

---

## E

### Error
**Categoría:** BenchmarkDotNet
**Definición:** Según la leyenda de BenchmarkDotNet, la mitad del intervalo de confianza de 99.9 %. Si es grande respecto de `Mean`, el tiempo es poco preciso.
**En el curso:** `Error` de 1,623.5 ns sobre un `Mean` de 2,893.4 ns: más de la mitad de la media.

---

## G

### GC (garbage collector)
**Categoría:** Memoria
**Definición:** El recolector de basura de .NET. Libera la memoria de los objetos que ya no se usan. Se estudia a fondo en el módulo 2.
**En el curso:** las columnas `Gen0`, `Gen1` y `Gen2` cuentan sus colecciones.

---

### GenN (Gen0, Gen1, Gen2)
**Categoría:** BenchmarkDotNet
**Definición:** Colecciones del recolector de esa generación por cada 1000 operaciones, con `[MemoryDiagnoser]`.
**En el curso:** `ConSplit` con 10,000 palabras: Gen0 89.8438, Gen1 50.7813, Gen2 25.3906.

---

### GlobalSetup
**Categoría:** BenchmarkDotNet
**Definición:** Atributo del método que prepara los datos una sola vez, fuera de lo que se mide. Buen lugar para comprobar que las implementaciones dan el mismo resultado.
**En el curso:** arma el texto y lanza `InvalidOperationException` si `ConSplit` y `ConCiclo` no coinciden.

---

## J

### JIT (just-in-time)
**Categoría:** Runtime
**Definición:** Compilador que traduce el código IL a código de máquina cuando un método se usa por primera vez.
**En el curso:** su costo contamina la primera medición de un `Stopwatch`.

---

## L

### Línea base
**Categoría:** Método
**Definición:** La medición del estado actual, hecha con la misma herramienta y condiciones que la medición posterior. Sin ella no hay "antes y después".
**En el curso:** es el primer paso del método y el primer expediente de la tarea.

---

## M

### Mean
**Categoría:** BenchmarkDotNet
**Definición:** Media aritmética de todas las mediciones.
**En el curso:** 694.0 ns para `ConCiclo` con 100 palabras, en esa corrida.

---

### MemoryDiagnoser
**Categoría:** BenchmarkDotNet
**Definición:** Atributo que agrega a la tabla las columnas `Gen0`, `Gen1`, `Gen2` y `Allocated`.
**En el curso:** va en todas las clases de benchmark del curso.

---

## O

### Objetivo de rendimiento
**Categoría:** Método
**Definición:** Una frase con un número que dice cuándo la mejora es suficiente. Permite parar de optimizar.
**En el curso:** el paso 2 de la tarea.

---

## P

### Params
**Categoría:** BenchmarkDotNet
**Definición:** Atributo que corre el mismo benchmark con varios valores de un campo, típicamente tamaños de datos.
**En el curso:** `[Params(100, 10_000)]` en `ContarPalabras`.

---

### Perfil (profiling)
**Categoría:** Diagnóstico
**Definición:** Observar un programa en marcha para ver dónde gasta tiempo o memoria. Se usa para encontrar el cuello de botella antes de hacer microbenchmarks.
**En el curso:** Parte 6 de la clase; las herramientas de .NET se ven en el módulo 6.

---

### Prueba de carga
**Categoría:** Diagnóstico
**Definición:** Generar tráfico controlado contra un servicio para ver cómo se comporta con varios usuarios. Solo se corre contra un entorno propio y de prueba.
**En el curso:** en este curso, solo contra la API de ejemplo local.

---

## R

### Ratio
**Categoría:** BenchmarkDotNet
**Definición:** `Mean` del método entre `Mean` de la línea base.
**En el curso:** `ConCiclo` salió con 0.24 con 100 palabras: en esa corrida, su tiempo fue el 24 % del de la línea base, con un `Error` grande.

---

## S

### ShortRun
**Categoría:** BenchmarkDotNet
**Definición:** Trabajo de BenchmarkDotNet con pocas iteraciones (`--job short`). Termina antes y es menos preciso.
**En el curso:** todas las cifras del curso se midieron así; por eso los tiempos se citan como "en esa corrida".

---

### Stopwatch
**Categoría:** .NET
**Definición:** Clase de `System.Diagnostics` que mide tiempo transcurrido. Útil para medir una operación real, engañoso para comparar el costo de un fragmento de código con una sola llamada.
**En el curso:** el demo `jit`.

---

### StdDev
**Categoría:** BenchmarkDotNet
**Definición:** Desviación estándar de todas las mediciones.
**En el curso:** 88.99 ns en `ConSplit` con 100 palabras.
