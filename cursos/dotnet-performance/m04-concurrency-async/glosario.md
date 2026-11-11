# Glosario - M4 - Concurrencia y async

> Definiciones de los términos técnicos del módulo 4.
> Ordenado alfabéticamente.

---

## A

### async / await
**Categoría:** Lenguaje
**Definición:** Palabras clave de C#. `async` habilita `await` en un método; no obliga a que corra en otro hilo. `await` sobre una operación incompleta libera el hilo y registra la continuación; si la operación ya terminó, el método sigue de forma síncrona (documentación de TAP).
**En el curso:** Los endpoints `/espera-async` y `/espera-corregida` usan `await Task.Delay(ms)`.

---

### async void
**Categoría:** Lenguaje
**Definición:** Método asíncrono que no devuelve `Task`: quien lo llama no puede esperarlo ni ver su excepción. La guía de ASP.NET Core dice que usarlo es siempre una mala práctica en esas aplicaciones.
**En el curso:** Se usa `async Task`.

---

## C

### CancellationToken
**Categoría:** Concurrencia
**Definición:** Objeto que comunica una solicitud de cancelación a una operación. Por convención, el parámetro se llama `cancellationToken`. Una operación cancelada termina en el estado `Canceled`.
**En el curso:** Se propaga desde el endpoint hasta cada llamada asíncrona.

---

### Carrera de datos (race condition)
**Categoría:** Concurrencia
**Definición:** Resultado incorrecto que ocurre cuando varios hilos leen y escriben el mismo dato sin sincronización y el resultado depende del orden en que se intercalan.
**En el curso:** `Demos race`: 4 hilos × 1 000 000 de leer-sumar-escribir dieron 2 548 716 en vez de 4 000 000.

---

### Concurrencia
**Categoría:** Concurrencia
**Definición:** Varias operaciones en progreso al mismo tiempo, aunque haya pocos hilos. No es lo mismo que paralelismo.
**En el curso:** 100 peticiones esperando a la vez con 5 o 6 hilos.

---

### CPU-bound
**Categoría:** Concurrencia
**Definición:** Trabajo limitado por los ciclos de procesador, no por esperar algo externo.
**En el curso:** Contar primos; `async` no lo acelera.

---

## H

### Hambre de hilos (thread pool starvation)
**Categoría:** Concurrencia
**Definición:** Situación en que los hilos del pool están bloqueados y el trabajo nuevo espera en cola mientras el pool agrega hilos poco a poco. La guía de ASP.NET Core la asocia con muchas llamadas bloqueantes.
**En el curso:** `/espera-bloqueante` con 100 clientes: 100 de 100 peticiones con error por tiempo límite.

---

## I

### I/O-bound
**Categoría:** Concurrencia
**Definición:** Trabajo limitado por esperar entrada y salida (red, disco, base de datos). Con E/S asíncrona no hay un hilo atado durante la espera.
**En el curso:** Los 50 ms de espera de la API de ejemplo.

---

### Interlocked
**Categoría:** Sincronización
**Definición:** Clase con operaciones atómicas sobre una variable (`Increment`, `Add`...). Para un contador simple evita perder actualizaciones.
**En el curso:** 4 000 000 exactos en el demo; 12.20 ms contra 53.78 ms de `lock` en esa corrida.

---

## L

### lock
**Categoría:** Sincronización
**Definición:** Instrucción de C# que permite que solo un hilo a la vez ejecute una sección. Dentro de un `lock` no se puede hacer `await`.
**En el curso:** `ConLock` en `Sincronizacion`.

---

## P

### Paralelismo
**Categoría:** Concurrencia
**Definición:** Varias operaciones ejecutándose literalmente al mismo tiempo en varios núcleos. Tiene un costo de reparto.
**En el curso:** Con límite 1 000, PLINQ y `Parallel.For` fueron más lentos que el ciclo secuencial en esa corrida.

---

### Parallel.For / PLINQ
**Categoría:** Concurrencia
**Definición:** `Parallel.For` reparte las iteraciones de un ciclo entre hilos; PLINQ (`AsParallel`) hace lo mismo con consultas LINQ.
**En el curso:** Contar primos hasta 200 000: PLINQ quedó a la par del secuencial; `Parallel.For`, más lento, en esa corrida.

---

### Pool de hilos (ThreadPool)
**Categoría:** Concurrencia
**Definición:** Conjunto de hilos que el runtime presta, uno por proceso. Crea hilos a demanda hasta un mínimo; pasado el mínimo puede crear más o esperar a que termine trabajo.
**En el curso:** `/diag` muestra `hilosDelPool` y `minimoDeHilos`.

---

## S

### SemaphoreSlim
**Categoría:** Sincronización
**Definición:** Primitiva que limita cuántos hilos o tareas entran a la vez a una sección; tiene versión que se puede esperar con `await`.
**En el curso:** Alternativa a `lock` en código asíncrono.

---

### Sync over async
**Categoría:** Concurrencia
**Definición:** Esperar de forma síncrona (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) algo asíncrono. Retiene un hilo del pool durante toda la espera.
**En el curso:** `Task.Delay(ms).Wait()` en `/espera-bloqueante`.

---

## T

### Task.Run
**Categoría:** Concurrencia
**Definición:** Pone trabajo en el pool de hilos. En ASP.NET Core no vuelve asíncrona una API síncrona ni evita el bloqueo de un hilo (guía oficial).
**En el curso:** Paso 6 de la práctica.

---

### TAP (Task-based Asynchronous Pattern)
**Categoría:** Concurrencia
**Definición:** Patrón recomendado en .NET para operaciones asíncronas, basado en `Task` y `Task<TResult>`; sus métodos llevan el sufijo `Async`.
**En el curso:** Fuente de varias citas de la clase.

---

## V

### ValueTask<TResult>
**Categoría:** Concurrencia
**Definición:** Struct que envuelve un `Task<TResult>` o un resultado ya disponible. Evita la asignación cuando el resultado es síncrono, pero solo se puede esperar una vez y su uso es restringido.
**En el curso:** 72 B contra 72 072 B asignados por 1 000 llamadas síncronas.

---

## W

### Warm-up (calentamiento)
**Categoría:** Medición
**Definición:** Carga inicial que se descarta para sacar el arranque en frío (JIT, primeras conexiones) antes de medir.
**En el curso:** 2 clientes durante 3 s antes de los 100 clientes.

---
