<!-- _class: lead -->

# Módulo 4 - Concurrencia y async

### .NET de alto rendimiento

**Rogelio Arriaga - Raptor Dev Services**

---

## Hoy

> `async` no hace más rápido nada: libera el hilo mientras espera.

- **Parte 1:** qué hace `await` y qué hace el pool de hilos
- **Parte 2:** bloquear sobre `async` (`.Wait()`, `.Result`) y el hambre de hilos
- **Parte 3:** concurrencia, paralelismo y cuándo no paralelizar
- **Parte 4:** `ValueTask`, bloqueos y condiciones de carrera
- **Parte 5:** cancelar lo que ya no hace falta
- **Práctica:** una API con una espera bloqueante, medida bajo carga hasta que se rompe, y corregida

Lecturas: [08 - async/await](../../../02-programacion/csharp/08-async.md)

Esta clase continúa el módulo 1 (medir), el 2 (memoria) y el 3 (Span y colecciones). Todo lo que se afirma de rendimiento sale de una medición del [código de referencia](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/README.md) o de la documentación oficial de Microsoft, y lleva su fuente.

---

## Cómo se leen las cifras de esta clase

- Entorno de **todas** las mediciones: .NET 10 (SDK 10.0.403, runtime 10.0.14), BenchmarkDotNet 0.15.8, trabajo `ShortRun`, CPU i5-11500, contenedor con 4 CPU, en un servidor compartido y con carga.
- Las pruebas de carga usan el [cliente `Carga`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Carga/Program.cs) contra la [API de ejemplo](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/ApiEjemplo/Program.cs), **ambos en el mismo contenedor**, con `DOTNET_PROCESSOR_COUNT=2` para la API (así se reproduce un servidor chico).
- **Los tiempos son ruidosos.** Dos corridas idénticas dieron 394 y 1 211 peticiones por segundo. Por eso aquí **no hay "X veces más rápido"** entre esas corridas: hay conclusiones cualitativas que se repitieron en todas las corridas.
- Las asignaciones (`Allocated`) no dependen de la carga.

> Nada de lo que viene se prueba contra un servicio real. La carga se corre **solo contra la API de ejemplo, en tu máquina**.

---

<!-- _class: lead -->

# Parte 1
## Qué hace await y qué hace el pool

---

## El problema

Una API recibe 100 peticiones a la vez. Cada una tiene que esperar 50 ms a algo externo (una base de datos, otra API). El código "obvio" de un endpoint:

```csharp
app.MapGet("/espera-bloqueante", (int ms) => { Task.Delay(ms).Wait(); return Results.Ok(); });
```

Funciona en tu máquina con una petición. Con 100 simultáneas, **se cae**. Hoy se mide cómo, y por qué.

---

## Qué NO hace `async`

La documentación oficial del patrón TAP dice:

> "The `async` keyword doesn't force a method to run asynchronously on another thread. It enables `await`, and the method runs synchronously until it reaches an incomplete awaitable."

- `async` **no crea hilos.**
- `await` sobre una operación que aún no termina **libera el hilo** que la ejecutaba; la continuación corre después, cuando la operación termina.
- Si la operación ya terminó cuando se llega al `await`, el método **sigue de forma síncrona**.

Fuente: [TAP, introducción](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).

---

## Esperar entrada y salida no necesita un hilo

El mismo documento lo dice así: un método asíncrono puede implementarse "by using asynchronous I/O (without being bound to a thread for the majority of the operation's execution)".

Cuando esperas la respuesta de una base de datos, de una API o de un disco, **no hay un hilo tuyo parado esperando**. Ese es todo el beneficio:

| | Espera bloqueante | Espera con `await` |
|---|---|---|
| Hilos ocupados durante los 50 ms | 1 por petición | 0 |
| Qué hace el hilo mientras espera | Nada, pero no puede atender a otro | Atiende otra petición |

ASP.NET Core lo dice en sus buenas prácticas: "Asynchronous APIs allow a small pool of threads to handle thousands of concurrent requests by not waiting on blocking calls."

Fuente: [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices), "Avoid blocking calls".

---

## El pool de hilos

Tu código no crea los hilos de las peticiones: los presta el **pool de hilos** (`ThreadPool`), uno por proceso. Lo usan, según la documentación, las operaciones de TPL, la finalización de E/S asíncrona, los callbacks de temporizadores y las conexiones de socket.

Dos datos de la documentación que importan hoy:

1. El pool crea hilos **a demanda hasta un mínimo**. Pasado el mínimo, "the thread pool can create additional threads or wait until some tasks complete".
2. Hay escenarios en que no conviene usar el pool: "You have tasks that cause the thread to block for long periods of time. The thread pool has a maximum number of threads, so a large number of blocked thread pool threads might prevent tasks from starting."

Fuente: [The managed thread pool](https://learn.microsoft.com/en-us/dotnet/standard/threading/the-managed-thread-pool).

La documentación no da aquí una cifra de cuánto tarda en agregar un hilo. **Eso lo vas a medir.**

---

## Dónde ver el pool de tu proceso

La API de ejemplo expone `/diag`, que usa APIs públicas del runtime:

```csharp
app.MapGet("/diag", () =>
{
    ThreadPool.GetMinThreads(out int minTrabajo, out _);
    return Results.Json(new
    {
        procesadores = Environment.ProcessorCount,
        hilosDelPool = ThreadPool.ThreadCount,
        minimoDeHilos = minTrabajo,
        trabajosPendientes = ThreadPool.PendingWorkItemCount,
        // ...
    });
});
```

Con `DOTNET_PROCESSOR_COUNT=2`, el proceso ve 2 procesadores y el mínimo de hilos del pool resultó 2 en las mediciones (`"minimoDeHilos":2`, [`api-E.log`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/mediciones/api-E.log)).

---

<!-- _class: lead -->

# Parte 2
## Bloquear sobre async

---

## Tres formas de lo mismo

```csharp {1|2|3|*}
var r1 = MiMetodoAsync().Result;                  // bloquea el hilo hasta tener el valor
MiMetodoAsync().Wait();                           // bloquea el hilo hasta que termine
var r3 = MiMetodoAsync().GetAwaiter().GetResult();// bloquea el hilo; cambia solo cómo sale la excepción
```

Las tres se llaman **sync over async**: se espera de forma síncrona algo que es asíncrono. Las tres **retienen un hilo del pool** durante toda la espera.

La guía oficial de ASP.NET Core es directa:

> "**Do not** block asynchronous execution by calling Task.Wait or Task<TResult>.Result."

Y explica la consecuencia: "Many synchronous blocking calls lead to Thread Pool starvation and degraded response times."

Fuente: [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices).

---

## El hambre de hilos (thread pool starvation)

Es lo que pasa cuando **todos los hilos del pool están bloqueados esperando** y las peticiones nuevas se forman en cola. El pool agrega hilos, pero poco a poco: mientras tanto, nadie avanza.

```
petición 1..100 llegan juntas
   cada una ocupa un hilo y lo bloquea 50 ms
   hay 2 hilos (mínimo) -> 98 peticiones esperan en cola
   el pool agrega hilos de a poco
```

La documentación de ASP.NET Core remite a "Thread Pool starvation" y la herramienta para diagnosticarla es del módulo 6. Hoy se **provoca y se mide**.

---

## Demostración: la misma espera, de dos formas

Los dos endpoints hacen exactamente lo mismo (esperar `ms` milisegundos). Solo cambia **cómo** esperan:

```csharp {1|2|*}
app.MapGet("/espera-bloqueante", (int ms) => { Task.Delay(ms).Wait(); return Results.Ok(); });
app.MapGet("/espera-async", async (int ms) => { await Task.Delay(ms); return Results.Ok(); });
```

La API corre con `DOTNET_PROCESSOR_COUNT=2`. El cliente `Carga` primero **calienta** con 2 clientes durante 3 s (para sacar el arranque en frío) y luego manda **100 clientes simultáneos durante 15 s**, con `ms=50`. El cliente tiene un tiempo límite de 30 s por petición.

---

## Lo que se midió: el endpoint bloqueante

Dos corridas, cada una con la API recién arrancada y con calentamiento:

| Corrida | Calentamiento (2 clientes) | Peticiones con error | Latencia p50 | Hilos del pool al final |
|---|---|---|---|---|
| 1 | 32 peticiones/s | **100 de 100** | 30 013.5 ms | 44 |
| 2 | 35 peticiones/s | **100 de 100** | 30 002.2 ms | 43 |

- Con 2 clientes, la API atendía bien (~32 a 35 peticiones/s: el calentamiento).
- Con 100 clientes, **las 100 peticiones agotaron el tiempo límite de 30 s del cliente**: ninguna terminó bien.
- El pool pasó de 2 hilos (el mínimo) a **43 o 44**. Fue creciendo, pero no a la velocidad que la carga necesitaba.

Una medición anterior, sin calentamiento ni tiempo límite estricto, mostró lo mismo: las 100 peticiones tardaron unos 22.7 s cada una y el pool pasó de 3 a 43 hilos ([`api-C.log`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/mediciones/api-C.log)).

---

## Lo que se midió: el endpoint async

Mismas condiciones (2 corridas, con calentamiento, 100 clientes, 15 s):

| Corrida | Peticiones | Errores | p50 | Peticiones/s | Hilos del pool |
|---|---|---|---|---|---|
| 1 | 5 909 | 0 | 59.2 ms | 394 | 5 |
| 2 | 18 169 | 0 | 56.9 ms | 1 211 | 6 |

- **Cero errores** y una mediana (p50) de unos 57 a 59 ms, cerca de los 50 ms de espera pedidos.
- El pool se quedó en **5 o 6 hilos**: no hizo falta crecer.
- Las peticiones por segundo **cambiaron de una corrida a otra** (394 y 1 211) en el mismo código y la misma máquina: es el ruido del servidor compartido. **No se compara con un "X veces".**
- La cola larga (p99 de 11 155.4 ms en la corrida 1 y 857.1 ms en la 2) **no se explica con esta medición**: no se investigó su causa.

---

## Qué sí se puede concluir

1. Con 100 peticiones simultáneas y 2 procesadores, el endpoint bloqueante **no completó ninguna dentro de 30 s**; el async **completó miles en 15 s**. Eso se repitió en las dos corridas de cada uno.
2. El pool de hilos con el endpoint bloqueante creció a más de 40 hilos; con el async se quedó en 5 o 6. Es la diferencia entre retener un hilo por petición y no retenerlo.
3. Las cifras exactas de throughput **no son transferibles**: dependen de la máquina y de la carga.

Lo que esta medición **no dice**: cuánto tarda el pool en agregar un hilo, ni cómo se comportaría con 10 peticiones o con 10 000.

---

## Error típico 1: "mejor lo envuelvo en Task.Run"

La guía oficial lo prohíbe expresamente en ASP.NET Core:

> "**Do not** call Task.Run and immediately await it. ASP.NET Core already runs app code on normal Thread Pool threads, so calling Task.Run only results in extra unnecessary Thread Pool scheduling. Even if the scheduled code would block a thread, Task.Run does not prevent that."

Y: "**Do not** use Task.Run to make a synchronous API asynchronous."

El bloqueo se **mueve** a otro hilo del mismo pool; no desaparece.

---

## Error típico 2: async solo hasta la mitad

```csharp
// Fragmento: el controlador es async, pero un servicio de abajo bloquea.
public async Task<IActionResult> Get()
{
    var datos = _servicio.ObtenerDatos();   // internamente: _repo.LeerAsync().Result
    return Ok(datos);
}
```

La guía pide que "The entire call stack is asynchronous in order to benefit from async/await patterns." Un solo `.Result` en la cadena retiene un hilo por petición.

Cómo se encuentra: busca `.Result`, `.Wait()` y `.GetAwaiter().GetResult()` en el código (es la tarea de este módulo).

---

## Error típico 3: `async void`

La documentación de ASP.NET Core: "Using `async void` is **ALWAYS** a bad practice in ASP.NET Core apps." Un `async void` devuelve el control a quien lo llamó en el primer `await` que no termina, así que el framework da por terminada la petición y el código que sigue corre sin que nadie lo espere.

Se usa `async Task`. La única excepción habitual (manejadores de eventos) no aplica a un endpoint.

---

<!-- _class: lead -->

# Parte 3
## Concurrencia y paralelismo

---

## Dos problemas distintos

| | I/O-bound (espera) | CPU-bound (calcula) |
|---|---|---|
| Qué limita | Esperar algo externo | Ciclos de procesador |
| Herramienta | `async` / `await` | Más hilos haciendo cálculo (paralelismo) |
| Qué mejora `async` | La **capacidad de atender** muchas esperas a la vez | **Nada**: el cálculo ocupa un hilo igual |

La documentación de TAP distingue las dos formas de implementar un método asíncrono: ejecutar la carga en el pool, o usar E/S asíncrona sin quedar atado a un hilo.

**Concurrencia** = muchas cosas en progreso a la vez (aunque haya pocos hilos). **Paralelismo** = varias cosas ejecutándose al mismo tiempo en varios núcleos.

---

## Paralelizar tiene costo

Se contó cuántos primos hay hasta un límite, de tres formas: secuencial, PLINQ (`AsParallel`) y `Parallel.For` con acumuladores locales ([`M4_Concurrencia.cs`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Bench/M4_Concurrencia.cs)). Entorno: 4 CPU del contenedor, servidor cargado.

| Límite | Secuencial | PLINQ | Parallel.For |
|---|---|---|---|
| 1 000 | 18.20 us | 250.68 us | 39.85 us |
| 200 000 | 17 454.42 us | 16 781.11 us | 30 754.35 us |

- Con un trabajo **chico** (1 000), las dos versiones paralelas fueron **más lentas** que la secuencial en esa corrida.
- Con 200 000, PLINQ quedó **a la par** de la secuencial y `Parallel.For` **más lenta**, en esa corrida.
- Los `Error` de esa corrida son enormes (en varias filas, mayores que la media): **no concluyas más que esto**. Paralelizar no es gratis y no siempre gana; hay que medir con el hardware real y con carga real.

No se midió con 4 núcleos libres ni con tamaños mayores: no se afirma qué pasaría.

---

## Antes de paralelizar, pregúntate

- ¿El cuello de botella es CPU o es espera? Si es espera, paralelizar con hilos no ayuda: ayuda `await`.
- ¿Cada unidad de trabajo es lo bastante grande para pagar el costo de repartirla?
- ¿Tu servidor ya está atendiendo muchas peticiones a la vez? En una API, cada petición ya es concurrente con las demás; repartir una petición entre varios hilos le quita CPU a las otras. (Es una consideración de diseño, no una medición de este curso.)
- ¿Compartes estado entre las tareas? Entonces necesitas sincronización (Parte 4).

---

<!-- _class: lead -->

# Parte 4
## ValueTask, bloqueos y carreras

---

## ValueTask: cuándo sí

Un método `async` que devuelve `Task<int>` devuelve un objeto en el heap. Se midieron 1 000 llamadas que terminan de forma síncrona (los valores devueltos se eligieron grandes a propósito, para que el resultado no sea un caso trivial) ([`TaskVsValueTask`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Bench/M4_Concurrencia.cs)):

| Método | Mean | Allocated |
|---|---|---|
| `ConTask` | 49.03 us | 72 072 B |
| `ConValueTask` | 25.57 us | 72 B |

Lo firme es `Allocated`: **72 072 B contra 72 B** por operación. Los tiempos son ruidosos (el `Error` de `ConTask` es de 149.46 us).

La documentación oficial pone la condición: un método puede devolver `ValueTask<TResult>` "when it's likely that the result of its operation will be available synchronously, and when it's expected to be invoked so frequently that the cost of allocating a new Task for each call will be prohibitive." Y la regla por omisión: "the default choice for any asynchronous method should be to return a Task or Task<TResult>. Only if performance analysis proves it worthwhile should a ValueTask<TResult> be used."

---

## ValueTask: las restricciones

Según la documentación de `ValueTask<TResult>`, **nunca** se debe:

- esperarlo (`await`) varias veces;
- llamar a `AsTask` varias veces;
- usar `.Result` o `.GetAwaiter().GetResult()` si la operación no ha terminado, ni usarlos varias veces;
- consumirlo de más de una forma a la vez.

"If you do any of the above, the results are undefined." Si esas restricciones no son aceptables, se convierte con `AsTask()`.

```csharp
// Fragmento
ValueTask<int> v = ObtenerAsync();
int a = await v;
int b = await v;   // MAL: el resultado es indefinido
```

Por eso el valor por omisión es `Task`: no cambies a `ValueTask` sin una medición como la de arriba.

---

## Una carrera de datos, vista fallar

Cuatro hilos suman 1 000 000 de veces cada uno a un contador. Esperado: 4 000 000. El incremento se escribe en sus tres pasos (leer, sumar, escribir) para que el compilador no los junte:

```csharp {1-2|3|*}
Parallel.For(0, hilos, _ => { for (int i = 0; i < vueltas; i++) {
    int v = Volatile.Read(ref sinProteger); Volatile.Write(ref sinProteger, v + 1); } });
Parallel.For(0, hilos, _ => { for (int i = 0; i < vueltas; i++) Interlocked.Increment(ref conInterlocked); });
```

Salida medida ([`Demos race`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Demos/Program.cs), [`demos2.txt`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/mediciones/demos2.txt)):

```
esperado: 4,000,000
leer-sumar-escribir sin proteger:  2,548,716
Interlocked:   4,000,000
```

Se **perdieron más de 1.4 millones de actualizaciones**. Una nota honesta: en la primera versión del demo se usó `sinProteger++` y dio 4 000 000; el JIT pudo haber juntado el ciclo en un registro. Eso **no significa que `n++` sea seguro entre hilos**: significa que el resultado dependió de cómo se compiló. Por eso el demo separa los tres pasos.

---

## Proteger el estado compartido: lock o Interlocked

Para un contador simple, 4 hilos y 250 000 vueltas cada uno ([`Sincronizacion`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Bench/M4_Concurrencia.cs)):

| Método | Mean |
|---|---|
| `ConLock` | 53.78 ms |
| `ConInterlocked` | 12.20 ms |

En esa corrida (`Error` de 85.01 ms para `lock` y 22.50 ms para `Interlocked`) `Interlocked` fue más rápido para esta operación simple. **No se generaliza:** sirve para operaciones atómicas de una sola variable. Cuando el estado es más complejo, `lock` (o `SemaphoreSlim` para código `async`, porque no se puede hacer `await` dentro de un `lock`) es la herramienta.

Y la guía de ASP.NET Core agrega: "**Do not** acquire locks in common code paths."

---

## Qué usar para qué

| Necesidad | Herramienta |
|---|---|
| Un contador o una bandera compartida | `Interlocked` |
| Proteger una sección con varios pasos (código síncrono) | `lock` |
| Limitar cuántas tareas `async` entran a la vez | `SemaphoreSlim` |
| Pasar trabajo de un productor a un consumidor | `Channel<T>` (en `System.Threading.Channels`) |
| Una colección que varios hilos modifican | Las de `System.Collections.Concurrent` |

La tabla describe para qué existe cada una; este módulo **mide solo `lock` contra `Interlocked`**. Para cualquier otra, mide antes de cambiar.

---

<!-- _class: lead -->

# Parte 5
## Cancelar lo que ya no hace falta

---

## CancellationToken

Si el cliente se desconecta, o la petición se pasó de tiempo, seguir trabajando es desperdicio. En TAP, la cancelación es opcional: un método que la permite "exposes an overload of the asynchronous method that accepts a cancellation token". Por convención, el parámetro se llama `cancellationToken`.

```csharp
// Fragmento: ASP.NET Core inyecta el token de la petición
app.MapGet("/reporte-lento", async (CancellationToken ct) =>
{
    await Task.Delay(TimeSpan.FromSeconds(5), ct);   // si se cancela, lanza OperationCanceledException
    return Results.Ok();
});
```

- Pásalo hacia abajo a **cada** llamada asíncrona (base de datos, `HttpClient`, `Task.Delay`).
- Una tarea cancelada termina en el estado `Canceled`; quien la espera con `await` recibe `OperationCanceledException`.

Fuente: [TAP, cancelación](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).

---

## Resumen de decisiones

| Pregunta | Respuesta |
|---|---|
| ¿Espero algo externo (BD, red, disco)? | `await` con la API asíncrona. Nunca `.Result` / `.Wait()` |
| ¿Calculo mucho? | Mide si paralelizar gana; con trabajos chicos, perdió |
| ¿Se llama miles de veces y casi siempre termina síncrono? | Considera `ValueTask`, **con medición** |
| ¿Varios hilos tocan el mismo dato? | `Interlocked` si es una variable; `lock` o `SemaphoreSlim` si es más |
| ¿La petición se puede abandonar? | Propaga `CancellationToken` |

---

## Práctica de hoy

Levantar la API de ejemplo localmente con `DOTNET_PROCESSOR_COUNT=2`, medir `/espera-async`, luego `/espera-bloqueante` y **verla fallar**, mirar `/diag`, convertir el endpoint bloqueante en asíncrono y volver a medir.

Reglas: **carga solo contra la API de ejemplo en tu máquina**. Con la medición de cada paso anotada (no solo "funcionó").

Material: [práctica](practica.md) y [tarea](tarea.md).

---

## Referencias

**Documentación oficial de Microsoft (consultada a noviembre de 2026):**

- Task-based asynchronous pattern (TAP): https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap
- The managed thread pool: https://learn.microsoft.com/en-us/dotnet/standard/threading/the-managed-thread-pool
- ASP.NET Core Best Practices: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices
- ValueTask<TResult>: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1
- BenchmarkDotNet, diagnosers: https://benchmarkdotnet.org/articles/configs/diagnosers.html

**Libros (solo por capítulo):**

- *High-Performance Programming in C# and .NET* (Alls, 2022), cap. 14 (Multi-Threaded Programming), cap. 15 (Parallel Programming), cap. 16 (Asynchronous Programming).
- *Systems Programming with C# and .NET* (Vroegop, 2024), cap. 4 (hilos).
- *Tools and Skills for .NET 10* (Price, 2025), cap. 9 (Multitasking and Concurrency).

**Mediciones propias:** [código de referencia](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/README.md), .NET 10 (SDK 10.0.403, runtime 10.0.14).
