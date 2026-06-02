# 20 — Memoria y Garbage Collector en .NET

Entender cómo .NET gestiona la memoria permite escribir código que no presiona innecesariamente al GC y evitar memory leaks en aplicaciones de larga duración.

---

## Stack vs Heap — dónde viven los datos
> Fuente: *Effective .NET Memory Management* — Ch.1 The Basics of .NET Memory

```
Stack                                  Heap
─────                                  ────
Tipos de valor (int, bool, struct)     Tipos de referencia (class, string, array)
Variables locales                      Objetos creados con new
Parámetros de métodos                  Strings
Automáticamente liberado al salir      Liberado por el Garbage Collector
del scope del método

ExampleUser user = new ExampleUser();
│              │                 │
│              └─ referencia      └─ objeto en el heap
└─ variable en el stack
```

---

## Generaciones del GC

El GC de .NET organiza los objetos en generaciones para optimizar la recolección:

```
Gen 0: objetos recién creados (recolección frecuente, rápida)
Gen 1: objetos que sobrevivieron una recolección de Gen 0 (buffer)
Gen 2: objetos de larga duración (recolección rara, costosa)
LOH (Large Object Heap): objetos > 85 KB (recolección muy rara)

Regla práctica: objetos de corta duración son buenos (Gen 0 es rápido).
Objetos que "promueven" a Gen 2 por error son malos (presionan el GC).
```

```csharp
// Ver información del GC en runtime
var gcInfo = GC.GetGCMemoryInfo();
Console.WriteLine($"Gen 0 collections: {GC.CollectionCount(0)}");
Console.WriteLine($"Gen 1 collections: {GC.CollectionCount(1)}");
Console.WriteLine($"Gen 2 collections: {GC.CollectionCount(2)}");
Console.WriteLine($"Total memory: {GC.GetTotalMemory(forceFullCollection: false) / 1024 / 1024} MB");
```

---

## Memory Leaks — causas comunes en .NET

A diferencia de C/C++, en .NET los leaks ocurren cuando el GC no puede recolectar objetos que ya no se necesitan.

### 1. Event handlers no desuscriptos

```csharp
// ❌ Memory leak — el publicador mantiene referencia al suscriptor
public sealed class ExampleUserService
{
    public event EventHandler<ExampleUser>? UserCreated;
}

public sealed class NotificationHandler
{
    public NotificationHandler(ExampleUserService service)
    {
        service.UserCreated += OnUserCreated;   // ← el servicio tiene referencia a este objeto
        // Si NotificationHandler se "destruye" pero service sigue vivo,
        // NotificationHandler no puede ser recolectado
    }

    private void OnUserCreated(object? sender, ExampleUser user) { ... }
}

// ✓ Desuscribirse cuando el objeto ya no se necesita
public sealed class NotificationHandler : IDisposable
{
    private readonly ExampleUserService _service;

    public NotificationHandler(ExampleUserService service)
    {
        _service = service;
        _service.UserCreated += OnUserCreated;
    }

    public void Dispose()
    {
        _service.UserCreated -= OnUserCreated;   // ← liberar la referencia
    }
}
```

### 2. Captura de variables en closures

```csharp
// ❌ El closure captura toda la instancia del Handler
public sealed class ProcessBatchHandler
{
    private readonly IExampleUserRepository _repo;
    private readonly List<Task> _pendingTasks = new();

    public void QueueProcessing(IEnumerable<Guid> ids)
    {
        foreach (var id in ids)
        {
            // El lambda captura 'this' — la instancia del Handler no puede ser recolectada
            // mientras haya tasks pendientes en _pendingTasks
            _pendingTasks.Add(Task.Run(() => ProcessUserAsync(id)));
        }
    }

    // ✓ Capturar solo lo necesario
    public void QueueProcessing_Safe(IEnumerable<Guid> ids)
    {
        var repo = _repo;   // captura local — no captura 'this'
        foreach (var id in ids)
        {
            var userId = id;   // captura local para evitar closure sobre variable del loop
            Task.Run(() => repo.GetByPublicIdAsync(userId, CancellationToken.None));
        }
    }
}
```

### 3. Servicios Scoped capturados por Singleton

```csharp
// ❌ Captive dependency — el Singleton captura un Scoped
// El Scoped debería vivir por request, pero el Singleton lo mantiene vivo siempre
public sealed class MyBackgroundService : BackgroundService
{
    private readonly IExampleUserRepository _repo;   // Scoped inyectado en Singleton → leak

    public MyBackgroundService(IExampleUserRepository repo) => _repo = repo;
}

// ✓ Crear scope por operación
public sealed class MyBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExampleUserRepository>();
            // repo vive solo dentro del using — liberado al salir del scope
            await ProcessBatchAsync(repo, ct);
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }
}
```

---

## IDisposable — liberar recursos no gestionados

Los recursos no gestionados (conexiones de BD, streams, sockets) deben liberarse explícitamente.

```csharp
// ✓ Implementar IDisposable correctamente
public sealed class ExampleUserFileExporter : IDisposable
{
    private readonly StreamWriter _writer;
    private bool _disposed;

    public ExampleUserFileExporter(string filePath)
        => _writer = new StreamWriter(filePath, append: false);

    public void WriteUser(ExampleUser user)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _writer.WriteLine($"{user.PublicId},{user.FullName},{user.Email}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _writer.Dispose();
        _disposed = true;
    }
}

// Uso correcto — using garantiza Dispose aunque haya excepción
using var exporter = new ExampleUserFileExporter("output.csv");
foreach (var user in users)
    exporter.WriteUser(user);
// Dispose se llama automáticamente aquí
```

```csharp
// ✓ IAsyncDisposable para recursos async (conexiones, streams async)
public sealed class ExampleUserRepository : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private bool _disposed;

    public ExampleUserRepository(string connectionString)
        => _connection = new NpgsqlConnection(connectionString);

    public async Task<ExampleUser?> GetByPublicIdAsync(Guid id, CancellationToken ct)
    {
        if (_connection.State == System.Data.ConnectionState.Closed)
            await _connection.OpenAsync(ct);
        return await _connection.QuerySingleOrDefaultAsync<ExampleUser>(
            "SELECT * FROM ExampleUsers WHERE PublicId = @id", new { id });
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _connection.DisposeAsync();
        _disposed = true;
    }
}

// Uso
await using var repo = new ExampleUserRepository(connectionString);
var user = await repo.GetByPublicIdAsync(id, ct);
```

---

## Value Types vs Reference Types — elegir bien

```csharp
// ✓ Usar struct para Value Objects pequeños e inmutables
// — no heap allocation cuando se usa como campo o variable local
public readonly struct Money
{
    public decimal Amount   { get; }
    public string  Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("El monto no puede ser negativo.");
        Amount   = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("No se pueden sumar monedas distintas.");
        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}

// ❌ struct para objetos grandes — se copia en cada asignación
// struct con 10+ campos puede ser más lento que una class por el costo de copia

// ✓ record struct para Value Objects (C# 10+)
public readonly record struct UserId(Guid Value)
{
    public static UserId New()      => new(Guid.NewGuid());
    public static UserId From(Guid v) => new(v);
    public override string ToString() => Value.ToString();
}
```

---

## ObjectPool — reutilizar objetos costosos
> Fuente: *Effective .NET Memory Management* — Ch.2 Object Allocation and Deallocation

Crear y destruir objetos repetidamente presiona al GC. Un pool mantiene instancias listas para reutilizar, eliminando el costo de allocación y recolección en hot paths.

```csharp
// Microsoft.Extensions.ObjectPool — registrar en DI
builder.Services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
builder.Services.AddSingleton(sp =>
{
    var provider = sp.GetRequiredService<ObjectPoolProvider>();
    return provider.CreateStringBuilderPool();
});

// Uso en un servicio
public sealed class CsvExportService
{
    private readonly ObjectPool<StringBuilder> _sbPool;

    public CsvExportService(ObjectPool<StringBuilder> sbPool) => _sbPool = sbPool;

    public string ExportExampleUsers(IEnumerable<ExampleUser> users)
    {
        var sb = _sbPool.Get();
        try
        {
            sb.AppendLine("Id,FullName,Email");
            foreach (var u in users)
                sb.AppendLine($"{u.PublicId},{u.FullName},{u.Email}");
            return sb.ToString();
        }
        finally
        {
            _sbPool.Return(sb);  // devolver al pool — se limpia automáticamente
        }
    }
}
```

Usar `ObjectPool<T>` para objetos costosos de crear (regex compilados, conexiones, buffers) que se usan frecuentemente en endpoints de alta carga.

---

## ArrayPool — reutilizar arrays grandes
> Fuente: *Effective .NET Memory Management* — Ch.2 Object Allocation and Deallocation

Arrays de más de 85 KB van al LOH (Large Object Heap), que el GC no compacta. `ArrayPool<T>` permite reutilizarlos sin generar presión al LOH.

```csharp
// System.Buffers.ArrayPool<T>
public async Task ProcessExampleUserBatchAsync(int batchSize, CancellationToken ct)
{
    // Alquilar un array del pool — el tamaño real puede ser mayor que el solicitado
    var buffer = ArrayPool<ExampleUser>.Shared.Rent(minimumLength: batchSize);
    try
    {
        var count = await _repo.FillBatchAsync(buffer.AsMemory(0, batchSize), ct);
        await ProcessUsersAsync(buffer.AsSpan(0, count), ct);
    }
    finally
    {
        ArrayPool<ExampleUser>.Shared.Return(buffer, clearArray: true);
    }
}
```

Regla práctica: cualquier array temporal mayor a ~80 KB debe venir de `ArrayPool<T>.Shared`.

---

## WeakReference — referenciar sin prevenir la recolección
> Fuente: *Effective .NET Memory Management* — Ch.2 Object Allocation and Deallocation

Una referencia débil permite mantener un puntero a un objeto sin impedir que el GC lo recolecte cuando no hay otras referencias fuertes.

```csharp
// Caché que cede memoria bajo presión del GC
public sealed class ExampleUserCache
{
    private readonly Dictionary<Guid, WeakReference<ExampleUser>> _cache = new();

    public void Add(ExampleUser user)
        => _cache[user.PublicId] = new WeakReference<ExampleUser>(user);

    public ExampleUser? Get(Guid id)
    {
        if (_cache.TryGetValue(id, out var weakRef) &&
            weakRef.TryGetTarget(out var user))
            return user;   // el objeto todavía vive en memoria

        _cache.Remove(id); // el GC ya lo recolectó
        return null;
    }
}

// Referencia fuerte vs débil
ExampleUser user = new ExampleUser { ... };         // referencia fuerte — el GC NO recolecta
var weak = new WeakReference<ExampleUser>(user);     // referencia débil — el GC PUEDE recolectar
user = null!;                                        // sin referencias fuertes...
GC.Collect();                                        // ...el GC puede recolectar el objeto
weak.TryGetTarget(out var recovered);               // recovered == null si fue recolectado
```

Usar `WeakReference<T>` para cachés opcionales donde perder la entrada es aceptable.

---

## Optimización de strings
> Fuente: *Effective .NET Memory Management* — Ch.2 Object Allocation and Deallocation

Los strings son inmutables — cada concatenación crea un objeto nuevo en el heap.

```csharp
// ❌ Concatenación en loop — crea N strings intermedios en el heap
var result = string.Empty;
foreach (var user in users)
    result += $"{user.FullName}, ";

// ✓ StringBuilder — un solo buffer mutable, evita N allocations
var sb = new StringBuilder(capacity: users.Count * 30);  // pre-sizing evita resize interno
foreach (var user in users)
    sb.Append(user.FullName).Append(", ");
var result = sb.ToString();

// ✓ string.Create — crear sin StringBuilder cuando se conoce el tamaño exacto
var formatted = string.Create(36, userId, static (span, id) =>
    id.TryFormat(span, out _, "D"));

// ✓ Interpolated string handlers (C# 10+) — el compilador optimiza automáticamente
// $"..." en métodos como logger.LogInformation ya usa DefaultInterpolatedStringHandler
// que evita el string intermedio cuando el log level no está habilitado
```

---

## Diagnóstico de memoria

```csharp
// Herramientas disponibles:
// 1. dotnet-counters — métricas en tiempo real
// dotnet-counters monitor --process-id <PID> System.Runtime

// 2. dotnet-dump — capturar un dump del proceso
// dotnet-dump collect --process-id <PID>
// dotnet-dump analyze <dump-file>

// 3. dotnet-trace — tracing de eventos del GC
// dotnet-trace collect --process-id <PID> --providers System.Runtime:0xffffffff:5

// 4. Visual Studio Memory Profiler (Windows)
// 5. JetBrains dotMemory (multiplataforma)

// En código: registrar métricas del GC
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddRuntimeInstrumentation()   // incluye GC metrics
        .AddAspNetCoreInstrumentation());
```

---

## Cuándo preocuparse por la memoria

| Señal | Acción |
|-------|--------|
| Gen 2 collections frecuentes | Revisar objetos de larga duración mal gestionados |
| Crecimiento continuo de memoria sin release | Buscar event handlers no desuscriptos, servicios Scoped capturados |
| LOH fragmentada | Reutilizar arrays grandes con ArrayPool |
| Alta presión de GC en endpoints de alta frecuencia | Usar Span<T>, MemoryPool, ObjectPool |
| OutOfMemoryException | Revisar streams no cerrados, listas que crecen sin límite |


---

## Glosario

| Término | Definición |
|---------|-----------|
| Garbage Collector | Componente del runtime de .NET que gestiona la memoria automáticamente liberando objetos sin referencias |
| Generaciones | División del heap en Gen 0, Gen 1 y Gen 2 según la edad y supervivencia de los objetos |
| Gen 0 | Generación donde se alocan los objetos nuevos — la GC collection más frecuente y barata |
| Large Object Heap | Segmento del heap para objetos mayores a 85 KB — raramente compactado, propenso a fragmentación |
| IDisposable | Interfaz que implementan los objetos con recursos no administrados para liberarlos en Dispose() |
| IAsyncDisposable | Variante asíncrona de IDisposable para recursos que requieren liberación asíncrona (streams, conexiones) |
| WeakReference\<T\> | Referencia que no impide al GC recolectar el objeto — útil para cachés que ceden memoria bajo presión |
| ObjectPool\<T\> | Pool de objetos para reutilizar instancias costosas de crear y evitar presión en el GC |
| ArrayPool\<T\> | Pool de arrays para reutilizar buffers y evitar allocations en el LOH |
| Memory Leak | Situación donde la memoria crece indefinidamente porque objetos no se liberan correctamente |
| Captive Dependency | Singleton que retiene una dependencia Scoped, causando que el Scoped nunca sea recolectado |
| Finalizador | Método llamado por el GC antes de recolectar un objeto — usar solo como salvaguarda, preferir Dispose() |

---

*Rogelio Arriaga Gonzalez*
