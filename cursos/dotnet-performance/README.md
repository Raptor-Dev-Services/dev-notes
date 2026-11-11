# .NET de alto rendimiento

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/dotnet-performance/](https://academy.raptorcloud.dev/dotnet-performance/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** C# avanzado (la sesión 3 de [Dev Notes](../dev-notes/README.md)) y una API en .NET en producción o casi. Es la continuación de [Backend con .NET](../dotnet-backend/README.md): aquí no se repite lo de EF Core, caché o Polly.
**Objetivo:** medir antes de optimizar, entender la memoria y el recolector de basura, escribir código con menos asignaciones, usar bien la concurrencia y diagnosticar en producción.
**Formato:** 6 módulos de 2 horas (12 h). Cada módulo trae clase, quiz, práctica, tarea y glosario.
**Para quién:** desarrolladores de .NET a quienes ya les preguntaron "¿por qué está lenta?".

**Cómo se sostienen las cifras.** Cada afirmación de rendimiento del curso viene de una medición hecha con BenchmarkDotNet (con `[MemoryDiagnoser]`), con `dotnet-counters`, `dotnet-dump` o `dotnet-trace`, o de la documentación oficial de Microsoft. Las mediciones del curso se hicieron en .NET 10 (SDK 10.0.403, runtime 10.0.14) dentro de un contenedor, en un servidor compartido y con carga: **los tiempos son ruidosos y cambian de una máquina a otra**; las asignaciones de memoria no dependen de la carga, y son las que sostienen las conclusiones. El código de la carpeta [`referencia/`](referencia/) es el que se midió. Las versiones de este curso están fechadas a noviembre de 2026.

---

## Módulo 1. Medir antes de tocar (2 h)

> Una optimización sin medición antes y después es una opinión.

**Material:** [clase](m01-measure-first/slides.md), [práctica](m01-measure-first/practica.md), [tarea](m01-measure-first/tarea.md), [glosario](m01-measure-first/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** el método (línea base, objetivo, perfil, cambio, nueva medición), BenchmarkDotNet, por qué un `Stopwatch` suelto engaña, el JIT y el calentamiento, profiling, trazas y pruebas de carga.
- [ ] **Lectura:** [`04-backend/19-performance.md`](../../04-backend/19-performance.md). Nota nueva: [`04-backend/47-benchmarking.md`](../../04-backend/47-benchmarking.md).
- [ ] **Práctica:** un benchmark de dos implementaciones de la misma función. Verlo fallar: el benchmark en modo Debug se niega a correr, y un `Stopwatch` de una sola llamada mide el JIT.
- [ ] **Tarea:** medir una función propia y entregar el antes y después con la salida completa de BenchmarkDotNet.
- **Libros:** *High-Performance Programming in C# and .NET* (Alls, 2022), cap. 5. *Tools and Skills for .NET 10* (Price, 2025), caps. 4 y 13.

**Al terminar puedes:** demostrar con números que un cambio mejora algo.

---

## Módulo 2. Memoria y recolector de basura (2 h)

> El GC no es gratis: cada asignación se paga después.

**Material:** [clase](m02-memory-gc/slides.md), [práctica](m02-memory-gc/practica.md), [tarea](m02-memory-gc/tarea.md), [glosario](m02-memory-gc/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** stack y heap, generaciones, el Large Object Heap, el recolector y la vida de los objetos.
- [ ] **Lectura:** [`04-backend/20-memoria-gc.md`](../../04-backend/20-memoria-gc.md).
- [ ] **Práctica:** medir las asignaciones de un endpoint y bajarlas a la mitad. Verlo fallar: la concatenación con `+=` dentro de un ciclo.
- [ ] **Tarea:** medir las asignaciones de un endpoint propio y bajarlas con un cambio justificado.
- **Libros:** *Effective .NET Memory Management* (Williams, 2024), caps. 1 a 4.

**Al terminar puedes:** leer el costo en memoria de su código.

---

## Módulo 3. Colecciones, LINQ y Span (2 h)

> El LINQ más legible no siempre es el más barato, y a veces sí.

**Material:** [clase](m03-collections-linq-span/slides.md), [práctica](m03-collections-linq-span/practica.md), [tarea](m03-collections-linq-span/tarea.md), [glosario](m03-collections-linq-span/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** elegir colección, costo de LINQ, `Span<T>`, `ArrayPool` y tipos por valor.
- [ ] **Lectura:** [`04-backend/19-performance.md`](../../04-backend/19-performance.md) (colecciones, LINQ y Span), [`04-backend/20-memoria-gc.md`](../../04-backend/20-memoria-gc.md) (`ArrayPool`, tipos por valor).
- [ ] **Práctica:** reescribir un procesamiento de texto con `Span<T>` y medir la diferencia.
- [ ] **Tarea:** elegir entre legibilidad y rendimiento con datos: tres casos con su benchmark y su decisión.
- **Libros:** *High-Performance Programming in C# and .NET*, caps. 3, 6 y 7. *Effective .NET Memory Management*, caps. 5 y 7.

**Al terminar puedes:** escoger entre legibilidad y rendimiento con datos, no por costumbre.

---

## Módulo 4. Concurrencia y async (2 h)

> `async` no hace más rápido nada: libera el hilo mientras espera.

**Material:** [clase](m04-concurrency-async/slides.md), [práctica](m04-concurrency-async/practica.md), [tarea](m04-concurrency-async/tarea.md), [glosario](m04-concurrency-async/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** hilos, tareas, async y await, paralelismo, bloqueos y el pool de hilos.
- [ ] **Lectura:** [`02-programacion/csharp/08-async.md`](../../02-programacion/csharp/08-async.md).
- [ ] **Práctica:** encontrar un `.Wait()` o `.Result` que agota el pool de hilos bajo carga, y corregirlo.
- [ ] **Tarea:** auditar el código propio en busca de bloqueo sobre `async` y medir una corrección.
- **Libros:** *High-Performance Programming in C# and .NET*, caps. 14 a 16. *Systems Programming with C# and .NET* (Vroegop, 2024), cap. 4. *Tools and Skills for .NET 10*, cap. 9.

**Al terminar puedes:** saber cuándo paralelizar y cuándo solo esperar mejor.

---

## Módulo 5. I/O, red y base de datos (2 h)

> En una API, el cuello de botella casi siempre está fuera del proceso.

**Material:** [clase](m05-io-network-data/slides.md), [práctica](m05-io-network-data/practica.md), [tarea](m05-io-network-data/tarea.md), [glosario](m05-io-network-data/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** archivos y streams, `HttpClient`, y acceso a datos medido.
- [ ] **Lectura:** [`05-bases-de-datos/02-indices.md`](../../05-bases-de-datos/02-indices.md), [`05-bases-de-datos/05-dapper-queries.md`](../../05-bases-de-datos/05-dapper-queries.md), [`04-backend/13-http-client.md`](../../04-backend/13-http-client.md).
- [ ] **Práctica:** benchmark de la misma consulta con EF Core y con Dapper, y del efecto de un índice.
- [ ] **Tarea:** localizar con mediciones si la lentitud de un flujo está en su código, en la red o en la base.
- **Libros:** *High-Performance Programming in C# and .NET*, caps. 8 a 11. *Systems Programming with C# and .NET*, caps. 5 y 8.

**Al terminar puedes:** encontrar si la lentitud está en su código, en la red o en la base.

---

## Módulo 6. Diagnóstico en producción (2 h)

> En producción no hay depurador: hay contadores, volcados y trazas.

**Material:** [clase](m06-production-diagnostics/slides.md), [práctica](m06-production-diagnostics/practica.md), [tarea](m06-production-diagnostics/tarea.md), [glosario](m06-production-diagnostics/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** `dotnet-counters`, `dotnet-dump`, `dotnet-trace`, métricas y trazas distribuidas.
- [ ] **Lectura:** [`10-cloud/17-tracing-distribuido.md`](../../10-cloud/17-tracing-distribuido.md). Nota nueva: [`04-backend/48-diagnostico-en-produccion.md`](../../04-backend/48-diagnostico-en-produccion.md).
- [ ] **Práctica:** una fuga de memoria sembrada en un contenedor, encontrada con contadores y un volcado.
- [ ] **Tarea:** su runbook de diagnóstico de una API que crece en memoria.
- **Libros:** *Effective .NET Memory Management*, cap. 6. *Systems Programming with C# and .NET*, caps. 10 y 11. *Tools and Skills for .NET 10*, cap. 5.

**Al terminar puedes:** diagnosticar una API lenta o que crece en memoria sin pararla.

---

## Proyecto final

Tomar una API real, medirla bajo carga, encontrar los tres cuellos de botella principales, corregirlos y entregar el antes y después con números.

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| Effective .NET Memory Management | Trevoir Williams | 2024 | Memoria y GC |
| High-Performance Programming in C# and .NET | Jason Alls | 2022 | Columna del curso. Es de .NET 6: cada técnica se verificó en .NET 10 antes de enseñarla, porque algunas mejoras ya vienen de serie |
| Systems Programming with C# and .NET | Dennis Vroegop | 2024 | Sistema operativo, I/O y diagnóstico |
| Tools and Skills for .NET 10 | Mark J. Price | 2025 | Herramientas de .NET 10 |

---

*Rogelio Arriaga Gonzalez*
