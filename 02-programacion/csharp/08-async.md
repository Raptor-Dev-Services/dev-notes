# 08 — async, await, Task y CancellationToken

La programación asíncrona es fundamental en cualquier API web. Este documento explica desde cero qué significa, por qué existe y cómo funciona en este proyecto.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.12 Improving Performance and Scalability Using Multitasking

---

## El problema — operaciones lentas bloquean el hilo

ASP.NET Core tiene un pool de hilos. Cada hilo puede atender una petición simultáneamente.

```
Pool de hilos: [Hilo 1] [Hilo 2] [Hilo 3] [Hilo 4] ... [Hilo N]
```

Sin async, cuando un hilo consulta la base de datos:

```
[Hilo 1] → consulta SQL → BLOQUEADO esperando respuesta...
              ↑ 150ms sin hacer nada
              ↑ No puede atender otras peticiones
              ↑ Si llegan 100 peticiones simultáneas que esperan BD, necesitas 100 hilos
```

Con async:

```
[Hilo 1] → envía consulta SQL → libre para atender otra petición
           (la red/BD trabaja sola)
→ [Otra petición llega, Hilo 1 la atiende]
→ BD responde → continuación se agenda en cualquier hilo disponible
```

**Resultado:** con async, el mismo número de hilos puede atender muchas más peticiones simultáneas.

---

## `Task<T>` — la promesa de un valor futuro

`Task<T>` representa una operación que **eventualmente** devolverá un `T`. Es como un ticket de reclamación.

```csharp
// Esto es una promesa de que eventualmente tendremos un ExampleUser? (o null)
Task<ExampleUser?> promesa = repo.GetByPublicIdAsync(id, ct);

// Para obtener el valor, debemos esperar:
ExampleUser? usuario = await promesa;

// En una línea:
ExampleUser? usuario = await repo.GetByPublicIdAsync(id, ct);
```

Tipos de Task:

```csharp
Task<ExampleUser>     // promesa de un ExampleUser
Task<ExampleUser?>    // promesa de un ExampleUser o null
Task<IEnumerable<T>>  // promesa de una colección
Task<int>             // promesa de un int
Task                  // promesa de que algo termina (sin valor — equivalente async a void)
```

---

## `async` y `await`

- `async`: marca el método como asíncrono. Habilita el uso de `await` dentro.
- `await`: "espera este Task sin bloquear el hilo".

```csharp
// Sin async — síncrono, bloquea el hilo
public ExampleUser? GetById(Guid id)
{
    // El hilo espera bloqueado durante toda la consulta
    return _db.QuerySingle<ExampleUser>("SELECT ...", new { id });
}

// Con async/await — no bloquea
public async Task<ExampleUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
{
    // await libera el hilo mientras espera la BD
    return await _db.QuerySingleAsync<ExampleUser>("SELECT ...", new { id }, cancellationToken: ct);
}
```

---

## La cadena async

La regla de oro: **si `awaitas` algo, tu método debe ser `async`**. Esto se propaga hacia arriba:

```
Npgsql ejecuta SQL asíncrono
    ↑ await
MainDapperDbConnection.QuerySingleAsync() → async Task<T>
    ↑ await
ExampleUsersSql.GetByPublicIdAsync() → async Task<T?>
    ↑ await
ExampleUserRepository.GetByPublicIdAsync() → async Task<T?>
    ↑ await
GetExampleUserHandler.Handle() → async Task<Response>
    ↑ await
Mediator.Send() → async Task<Response>
    ↑ await
ExampleUsersController.GetById() → async Task<IActionResult>
    ↑ ASP.NET Core maneja esto automáticamente
```

Toda la cadena debe ser async. "Cortar" la cadena (llamar a un método async sin await) elimina los beneficios.

---

## Cómo se ve en el proyecto — ejemplo completo

```csharp
// Controller — punto de entrada
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
{
    try
    {
        // await aquí — controller espera sin bloquear
        _ = await Mediator.Send(new GetExampleUserRequest(id), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en GetById id={UserId}", id);
        var inner = ex;
        while (inner.InnerException != null) inner = inner.InnerException!;
        return StatusCode(500, _viewModel.Fail(inner.Message));
    }
}

// Handler — lógica de negocio
public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken cancellationToken)
{
    // await aquí — handler espera sin bloquear
    var user = await _repo.GetByPublicIdAsync(request.PublicId, cancellationToken);
    
    if (user is null)
        return new GetExampleUserNotFoundFailure("Usuario no encontrado.");
    
    return new GetExampleUserSuccess(new ExampleUserDto(
        user.PublicId, user.FullName, user.Email,
        user.IsActive, user.CreatedAtUtc, user.UpdatedAtUtc));
}

// Repositorio — delegación al Sql
public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
    => _sql.GetByPublicIdAsync(publicId, ct);
// Nota: NO es async — no necesita await, solo delega directamente

// Sql — ejecución real
public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
    => db.QuerySingleAsync<ExampleUser>(
        """
        SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc
        FROM dbo.ExampleUsers
        WHERE PublicId = @publicId
        """,
        new { publicId },
        cancellationToken: ct);
// También NO es async — delega directamente a Dapper
```

---

## `_ = await` — descarta el resultado

En los controllers, el resultado del handler va al Presenter via el pipeline de mediator. El controller no usa ese retorno:

```csharp
// _ = descarta el retorno
_ = await Mediator.Send(new GetExampleUserRequest(id), ct);
// El resultado llegó al Presenter vía Publish — ya populó _viewModel

return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
```

---

## Operaciones paralelas con `Task.WhenAll`

Cuando necesitas hacer múltiples operaciones independientes y esperar todas:

```csharp
public async Task<(ExampleUser? user, int totalUsers)> GetUserWithStatsAsync(
    Guid publicId, CancellationToken ct)
{
    // Sin paralelismo — secuencial: 200ms + 150ms = 350ms total
    var user  = await _repo.GetByPublicIdAsync(publicId, ct);
    var total = await _repo.CountAsync(ct);
    return (user, total);
    
    // Con paralelismo — paralelo: max(200ms, 150ms) = 200ms total
    var userTask  = _repo.GetByPublicIdAsync(publicId, ct);
    var totalTask = _repo.CountAsync(ct);
    
    await Task.WhenAll(userTask, totalTask);
    
    return (await userTask, await totalTask);
}
```

**Cuidado:** en el mismo scope de base de datos, dos queries paralelas pueden causar problemas si comparten la misma conexión. En este proyecto, `MainDapperDbConnection` es Scoped — úsalo de forma secuencial por defecto.

---

## `Task.WhenAny` — el primero que termine

```csharp
// Timeout manual: esperar a que termine la operación O pasen 5 segundos
var operacion = _repo.GetByPublicIdAsync(id, ct);
var timeout   = Task.Delay(TimeSpan.FromSeconds(5), ct);

var primero = await Task.WhenAny(operacion, timeout);

if (primero == timeout)
    throw new TimeoutException("La operación tardó demasiado.");

var user = await operacion;  // ya terminó — obtener el resultado
```

---

## `CancellationToken` — señal de cancelación

### ¿Qué es?

Un token que viaja con la petición desde ASP.NET hasta la base de datos. Si el cliente cierra el navegador o la petición es cancelada, el token se activa y cancela todo en cascada.

```csharp
// ASP.NET inyecta CancellationToken automáticamente en los métodos de controller
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
//                                                  ↑ ASP.NET lo llena automáticamente
{
    _ = await Mediator.Send(new GetExampleUserRequest(id), ct);
    // ct viaja: controller → handler → repositorio → sql → driver de BD
}
```

### Cómo funciona

```
Cliente cierra el navegador
    ↓ ASP.NET detecta la desconexión
    ↓ Activa (cancela) el CancellationToken de la petición
    ↓
Npgsql detecta que el token está cancelado
    ↓ Cancela la consulta SQL en PostgreSQL
    ↓ Lanza OperationCanceledException
    ↓
Se propaga hacia arriba por toda la cadena async
    ↓
La petición termina limpiamente — sin trabajo innecesario
```

### `CancellationToken.None` y `default`

Equivalentes — "no hay cancelación":

```csharp
await repo.GetByPublicIdAsync(id);                           // ct = default
await repo.GetByPublicIdAsync(id, default);                  // explícito
await repo.GetByPublicIdAsync(id, CancellationToken.None);   // también
```

Úsalos en tests y en código que no tiene contexto de request HTTP.

### `CancellationTokenSource` — crear tu propio token

```csharp
// Crear un token que se cancela después de 5 segundos
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var ct = cts.Token;

try
{
    var user = await repo.GetByPublicIdAsync(id, ct);
}
catch (OperationCanceledException)
{
    Console.WriteLine("La operación fue cancelada (timeout o cliente desconectado)");
}

// Cancelar manualmente:
cts.Cancel();  // activa el token inmediatamente
```

### Combinar tokens

```csharp
// Crear un token que se cancela SI el de la request O el de timeout se activa
using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
    requestCt,              // el de ASP.NET
    timeoutCts.Token);      // el de timeout propio

var ct = combinedCts.Token;
var user = await repo.GetByPublicIdAsync(id, ct);
```

### Verificar cancelación manualmente

```csharp
public async Task ProcessBulkAsync(IEnumerable<Guid> ids, CancellationToken ct)
{
    foreach (var id in ids)
    {
        // Verificar si fue cancelado antes de cada iteración
        ct.ThrowIfCancellationRequested();
        // Alternativa sin lanzar: if (ct.IsCancellationRequested) break;
        
        await ProcessOneAsync(id, ct);
    }
}
```

---

## Errores comunes con async

### Error 1 — `async void` (nunca usar excepto en event handlers)

```csharp
// ❌ async void — las excepciones no se pueden capturar externamente
public async void CargarDatos()
{
    var user = await _repo.GetByPublicIdAsync(id, ct);
    // Si lanza excepción, la app puede crashear silenciosamente
}

// ✓ async Task — las excepciones se propagan correctamente
public async Task CargarDatosAsync()
{
    var user = await _repo.GetByPublicIdAsync(id, ct);
}
```

### Error 2 — Bloquear async con `.Result` o `.Wait()`

```csharp
// ❌ Bloquear el hilo — puede causar deadlock en ASP.NET
var user = repo.GetByPublicIdAsync(id, ct).Result;  // BLOQUEA el hilo
var user = repo.GetByPublicIdAsync(id, ct).GetAwaiter().GetResult();  // ídem

// ✓ Siempre await
var user = await repo.GetByPublicIdAsync(id, ct);
```

**Deadlock:** el hilo espera que el Task termine; el Task espera que el hilo esté disponible → deadlock.

### Error 3 — Olvidar `await`

```csharp
// ❌ Sin await — el handler retorna inmediatamente sin esperar la BD
public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest request, CancellationToken ct)
{
    var task = _repo.GetByPublicIdAsync(request.PublicId, ct);
    // ↑ "task" es un Task<ExampleUser?> — NO es el usuario todavía
    
    if (task is null)  // ERROR LÓGICO — task nunca es null (es un Task)
        return new GetExampleUserNotFoundFailure("...");
    
    // ... el usuario nunca fue esperado
}

// ✓ Con await
public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest request, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
    // ↑ "user" es ExampleUser? — el valor real
    
    if (user is null)
        return new GetExampleUserNotFoundFailure("...");
    
    return new GetExampleUserSuccess(/* ... */);
}
```

### Error 4 — No propagar CancellationToken

```csharp
// ❌ Recibe ct pero no lo pasa — la BD no puede cancelar
public async Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
{
    return await _db.QuerySingleAsync<ExampleUser>(
        "SELECT * FROM ...",
        new { publicId });
        // ↑ falta: cancellationToken: ct
}

// ✓ Propaga el token
public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
    => _db.QuerySingleAsync<ExampleUser>(
        "SELECT * FROM ...",
        new { publicId },
        cancellationToken: ct);  // ← propagado
```

### Error 5 — `async` sin `await` (warning del compilador)

```csharp
// ❌ El compilador avisa: "This async method lacks 'await' operators"
// El método se ejecuta síncrono aunque diga async
public async Task<string> GetNameAsync()
{
    return "Ana";  // sin await — el async no sirve de nada aquí
}

// ✓ Si el método es síncrono, no pongas async
public string GetName() => "Ana";

// ✓ Si quieres retornar un Task sin async, usa Task.FromResult
public Task<string> GetNameAsync() => Task.FromResult("Ana");
```

---

## `ValueTask<T>` — optimización avanzada

`ValueTask<T>` es como `Task<T>` pero evita allocations cuando el resultado está disponible inmediatamente (ej. desde cache).

```csharp
// Task<T> — siempre aloca un objeto en el heap
public Task<ExampleUser?> GetFromCache(Guid id)
{
    if (_cache.TryGetValue(id, out var user))
        return Task.FromResult(user);  // aloca un Task aunque el resultado sea inmediato
    return _repo.GetByPublicIdAsync(id);
}

// ValueTask<T> — no aloca cuando el resultado está en cache
public ValueTask<ExampleUser?> GetFromCache(Guid id)
{
    if (_cache.TryGetValue(id, out var user))
        return new ValueTask<ExampleUser?>(user);  // sin allocation
    return new ValueTask<ExampleUser?>(_repo.GetByPublicIdAsync(id));
}
```

**En este proyecto:** usar `Task<T>` por defecto. `ValueTask<T>` solo cuando el profiling muestra que las allocations son un problema.

---

## Tabla de decisión

| Situación | Qué usar |
|-----------|---------|
| Método que llama a otros async | `async Task<T> MetodoAsync()` |
| Método que solo delega (no usa el resultado) | `Task<T> MetodoAsync() => otro.MetodoAsync()` |
| Método sin valor de retorno | `async Task MetodoAsync()` |
| Método síncrono sin I/O | Sin async — método síncrono normal |
| Retornar un valor inmediato como Task | `Task.FromResult(valor)` |
| Retornar un Task completado | `Task.CompletedTask` |
| Operaciones paralelas independientes | `await Task.WhenAll(t1, t2, t3)` |
| Timeout o cancelación | `CancellationTokenSource` con `TimeSpan` |


---

*Rogelio Arriaga Gonzalez*
