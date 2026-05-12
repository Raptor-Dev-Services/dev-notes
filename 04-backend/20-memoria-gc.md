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

*Rogelio Arriaga Gonzalez*
