# 13 — Background Services: IHostedService y BackgroundService

Ejecutar trabajo en segundo plano dentro del proceso de la API: limpieza periódica, procesamiento de colas, tareas de mantenimiento.

---

## El problema que resuelve

Algunas tareas no encajan en el ciclo request/response:

```
❌ Problemas que no resuelve una request HTTP:
   - Enviar emails de recordatorio cada hora
   - Purgar sesiones expiradas cada noche
   - Procesar una cola de trabajos de forma continua
   - Calentar el caché al arrancar la aplicación
   - Sincronizar datos con un servicio externo cada 5 minutos
```

`IHostedService` y `BackgroundService` son el mecanismo de .NET para esto: se registran en el contenedor de DI, arrancan con la aplicación y se detienen cuando el host se apaga.

---

## IHostedService — interfaz base

```csharp
public interface IHostedService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

`StartAsync` se llama cuando la aplicación arranca. `StopAsync` cuando el host recibe la señal de apagado.

**No bloquear `StartAsync`:** si tiene trabajo continuo, lanzar una tarea en background.

---

## BackgroundService — clase base para trabajo continuo

`BackgroundService` implementa `IHostedService` y abstrae el loop de trabajo:

```csharp
// Application/BackgroundServices/SessionCleanupService.cs
public sealed class SessionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionCleanupService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Apagado limpio — no loguear como error
                break;
            }
            catch (Exception ex)
            {
                // Loguear pero no dejar caer el servicio
                _logger.LogError(ex, "Error en SessionCleanupService. Reintentando en 1 min.");
            }

            await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
        }

        _logger.LogInformation("SessionCleanupService detenido.");
    }

    private async Task DoWorkAsync(CancellationToken ct)
    {
        // ⚠️ IHostedService es Singleton — los Scoped services requieren un scope propio
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        var deleted = await repo.DeleteExpiredAsync(ct);
        _logger.LogInformation("Sesiones expiradas purgadas: {Count}", deleted);
    }
}
```

### Por qué IServiceScopeFactory

Los `BackgroundService` son **Singleton**. Los repositorios y handlers son **Scoped**. No se pueden inyectar directamente — la dependencia capturada (captive dependency) causa bugs.

```csharp
// ❌ Captive dependency — el Singleton captura un Scoped
public sealed class MyService : BackgroundService
{
    private readonly IExampleUserRepository _repo;  // ← Scoped inyectado en Singleton: BUG
    public MyService(IExampleUserRepository repo) => _repo = repo;
    // El repo vive para siempre aunque debería descartarse por request
}

// ✓ Correcto — crear un scope por ciclo de trabajo
await using var scope = _scopeFactory.CreateAsyncScope();
var repo = scope.ServiceProvider.GetRequiredService<IExampleUserRepository>();
```

---

## Tarea única al arrancar

Para trabajo que solo se hace una vez al inicio (calentar caché, verificar infraestructura):

```csharp
// Infrastructure/Services/CacheWarmupService.cs
public sealed class CacheWarmupService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheWarmupService> _logger;

    public CacheWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<CacheWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calentando caché...");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var repo  = scope.ServiceProvider.GetRequiredService<IExampleUserRepository>();

        // Precargar datos frecuentes
        var activeUsers = await repo.GetAllActiveAsync(cancellationToken);
        foreach (var user in activeUsers)
            cache.Set($"user:{user.PublicId}", new ExampleUserDto(user), TimeSpan.FromMinutes(30));

        _logger.LogInformation("Caché calentado: {Count} usuarios.", activeUsers.Count());
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

---

## Procesador de cola (Worker pattern)

Para consumir mensajes de una cola (RabbitMQ, Azure Service Bus, o una tabla DB):

```csharp
// Infrastructure/Services/OutboxProcessorService.cs
public sealed class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingMessagesAsync(stoppingToken);
            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        await using var scope   = _scopeFactory.CreateAsyncScope();
        var outboxRepo          = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var mediator            = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await outboxRepo.GetPendingAsync(batchSize: 10, ct);

        foreach (var message in messages)
        {
            try
            {
                // Procesar el mensaje según su tipo
                await mediator.Send(message.ToRequest(), ct);
                await outboxRepo.MarkProcessedAsync(message.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje {MessageId}", message.Id);
                await outboxRepo.MarkFailedAsync(message.Id, ex.Message, ct);
            }
        }
    }
}
```

---

## Registro en DI

```csharp
// Host/Program.cs  o  Infrastructure/ServiceCollectionEx.cs
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddHostedService<CacheWarmupService>();
builder.Services.AddHostedService<OutboxProcessorService>();
```

`AddHostedService<T>` registra el servicio como Singleton y lo inicia con el host.

---

## Graceful Shutdown

.NET da 5 segundos (por defecto) para que los hosted services se detengan limpiamente. Para trabajo de larga duración, respetar el `CancellationToken`:

```csharp
// appsettings.json — extender el tiempo de apagado si los jobs son largos
{
  "ShutdownTimeout": "00:00:30"
}

// Host/Program.cs
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

```csharp
// En ExecuteAsync — siempre pasar stoppingToken a operaciones async
var result = await _repo.GetPendingAsync(ct: stoppingToken);
await _http.PostAsync(url, content, stoppingToken);
await Task.Delay(interval, stoppingToken);  // ← Delay se cancela limpiamente al apagar
```

---

## Relación con back-template

`back-template/docs/Pagination.md` menciona `BackgroundService` en el contexto de refresh tokens. El patrón concreto de implementación es este documento.

Para agregar un background service al proyecto:
1. Crear la clase en `Infrastructure/Services/` o `Application/BackgroundServices/` según la dependencia.
2. Registrar con `AddHostedService<T>` en `Host/Program.cs`.
3. Usar `IServiceScopeFactory` para acceder a servicios Scoped.
4. Respetar `CancellationToken` en todas las operaciones async.

---

## Cuándo usar / Cuándo no usar

| Escenario | Decisión |
|-----------|----------|
| Tarea periódica simple (purga, limpieza) | ✓ BackgroundService con Task.Delay |
| Precarga al arrancar | ✓ IHostedService.StartAsync |
| Cola de mensajes simple (outbox pattern) | ✓ BackgroundService con polling |
| Cron jobs complejos | ✓ Considerar Quartz.NET o Hangfire |
| Tareas de larga duración distribuidas | ✓ Considerar workers separados o Azure Functions |
| Lógica que depende de un usuario autenticado | ✗ No tiene contexto de request — usar ICurrentUserService solo para audit trail en handlers regulares |
| Trabajo que puede perderse si la app se cae | ✗ No — usar una cola externa persistente (RabbitMQ, Azure Service Bus) |

---

## PeriodicTimer — alternativa moderna a Task.Delay
> Fuente: *Apps and Services with .NET 8* (Price) — Ch.12 Background Services

`PeriodicTimer` (introducido en .NET 6) es más preciso que `Task.Delay` en loops de polling porque no acumula drift de tiempo.

```csharp
// ❌ Task.Delay — el intervalo incluye el tiempo de ejecución + el delay
// Si el trabajo toma 2s y el delay es 5s → el loop real es cada 7s, se va desviando
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await DoWorkAsync(stoppingToken);
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);  // ← inicia DESPUÉS del trabajo
    }
}

// ✓ PeriodicTimer — el tick ocurre en el intervalo exacto, independientemente del tiempo de trabajo
public sealed class SessionCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        // Ejecutar una vez al iniciar, luego esperar el timer
        do
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en limpieza de sesiones.");
                // Continúa — el loop no se rompe por errores individuales
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
```

---

## BackgroundService con canal (Channel<T>)

Para procesar trabajo enviado desde requests HTTP — el Handler publica en el canal, el Background Service consume.

```csharp
// Shared/Channels/EmailChannel.cs
// Un canal tipado para encolar emails sin bloquear el request
public sealed class EmailChannel
{
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(500)
        {
            FullMode          = BoundedChannelFullMode.Wait,
            SingleReader      = true,
            SingleWriter      = false
        });

    public ChannelWriter<EmailMessage>  Writer => _channel.Writer;
    public ChannelReader<EmailMessage>  Reader => _channel.Reader;
}

public sealed record EmailMessage(string To, string Subject, string Body);
```

```csharp
// Infrastructure/BackgroundServices/EmailSenderService.cs
public sealed class EmailSenderService : BackgroundService
{
    private readonly EmailChannel _channel;
    private readonly IEmailClient _emailClient;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(
        EmailChannel channel,
        IEmailClient emailClient,
        ILogger<EmailSenderService> logger)
    {
        _channel     = channel;
        _emailClient = emailClient;
        _logger      = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _emailClient.SendAsync(message.To, message.Subject, message.Body, stoppingToken);
                _logger.LogInformation("Email enviado a {To}", message.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email a {To}", message.To);
            }
        }
    }
}
```

```csharp
// Handler que encola un email sin esperar el envío
public sealed class RegisterExampleUserHandler
    : IRequestHandler<RegisterExampleUserRequest, RegisterExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;
    private readonly EmailChannel           _emailChannel;

    public async Task<RegisterExampleUserResponse> Handle(
        RegisterExampleUserRequest request, CancellationToken ct)
    {
        var user = ExampleUser.Create(request.FullName, request.Email);
        await _repo.InsertAsync(user, ct);

        // Encolar el email — no bloquea el response al usuario
        await _emailChannel.Writer.WriteAsync(
            new EmailMessage(
                To:      user.Email,
                Subject: "Bienvenido a la plataforma",
                Body:    $"Hola {user.FullName}, tu cuenta fue creada exitosamente."),
            ct);

        return new RegisterExampleUserSuccess(new ExampleUserDto(user));
    }
}
```

```csharp
// Registro en Program.cs
builder.Services.AddSingleton<EmailChannel>();
builder.Services.AddHostedService<EmailSenderService>();
```

### Cuándo usar Channel vs Queue externa

| Criterio | Channel<T> (en proceso) | RabbitMQ / Azure Service Bus |
|----------|------------------------|------------------------------|
| Simplicidad | ✓ Sin dependencias externas | ✗ Requiere broker externo |
| Durabilidad | ✗ Mensajes se pierden si la app cae | ✓ Mensajes persisten |
| Escala horizontal | ✗ Solo una instancia consume | ✓ Múltiples consumidores |
| Volumen | ✓ Hasta ~1M mensajes/s en memoria | ✓ Cualquier volumen |
| **Usar cuando** | Emails de bienvenida, notificaciones | Pagos, órdenes, eventos críticos |


---

## Glosario

| Término | Definición |
|---------|-----------|
| BackgroundService | Clase base de .NET para servicios en segundo plano con método ExecuteAsync y soporte de cancellation |
| IHostedService | Interfaz de .NET para servicios del host con StartAsync y StopAsync — BackgroundService la implementa |
| IServiceScopeFactory | Factory que crea scopes DI dentro de un Singleton — necesaria para resolver servicios Scoped en background |
| PeriodicTimer | Timer de .NET 6+ para ejecutar tareas en intervalos fijos sin drift acumulado |
| Channel\<T\> | Cola en memoria de alta performance para producer/consumer patterns dentro del mismo proceso |
| Captive Dependency | Error de DI donde un Singleton retiene una dependencia Scoped causando comportamiento incorrecto |
| Graceful Shutdown | Proceso de cierre ordenado donde el servicio termina el trabajo en curso antes de apagarse |
| CancellationToken | Token de cancelación que se activa cuando el host solicita la detención del servicio |
| Outbox Pattern | Patrón donde los eventos de dominio se guardan en la misma transacción que el dato, luego se procesan |
| IServiceProvider | Contenedor DI raíz desde el que se crean scopes en servicios Singleton |
| ExecuteAsync | Método abstracto de BackgroundService donde se implementa el loop principal del servicio |
| StoppingToken | Token de cancelación específico de BackgroundService que se activa en shutdown del host |

---

*Rogelio Arriaga Gonzalez*
