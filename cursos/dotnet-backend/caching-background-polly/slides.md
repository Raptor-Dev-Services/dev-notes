<!-- _class: lead -->

# Caching  -  Background Services  -  Polly

## Material extra  -  Sistema de mensajería  -  .NET 10

Rogelio Arriaga  -  Raptor Dev Services  -  Mayo 2026

---

<!-- _class: lead -->

# Parte 1

## Caching: IMemoryCache vs IDistributedCache

---

## ¿Por qué cachear?

Cada request sin caché repite el mismo trabajo costoso:

```
GET /api/channels  (10 veces por segundo)
  -> viaje de red a PostgreSQL
  -> parse de filas
  -> hidratación de objetos
  -> serialización JSON
```

El caché guarda el resultado la primera vez y lo sirve directamente en requests posteriores.

> **Regla de aplicación:** Cachear cuando los datos cambian con poca frecuencia y se leen frecuentemente. En el sistema de mensajería: lista de canales configurados, configuración del workspace, contactos frecuentes.

---

## IMemoryCache  -  caché en proceso

Vive en la memoria RAM del proceso. Rápido (nanosegundos). **No se comparte entre instancias.**

```csharp
// Channels.Infrastructure/Services/MemoryCacheService.cs
using Microsoft.Extensions.Caching.Memory;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public bool TryGet<T>(string key, out T? value) =>
        _cache.TryGetValue(key, out value);

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
            options.SetAbsoluteExpiration(expiry.Value);
        else
            options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
        _cache.Set(key, value, options);
    }

    public void Remove(string key) => _cache.Remove(key);
}
```

---

## IDistributedCache con Redis  -  registro

Redis es la implementación estándar para múltiples instancias.

```csharp
// Host.Api/Extensions/CacheExtensions.cs
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace Host.Api.Extensions;

public static class CacheExtensions
{
    public static IServiceCollection AddCacheServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:Connection"]
                ?? throw new InvalidOperationException(
                    "Redis:Connection no está configurado.");
            options.InstanceName = "mensajeria:";
        });

        services.AddScoped<ICacheService, DistributedCacheService>();
        return services;
    }
}
```

---

## Registro en Program.cs  -  patrón del back-template

El `Program.cs` del back-template **delega a extension methods**, igual que `AddMainDatabase` o `AddHealthServices`.

```csharp
// Host.Api/Program.cs  (extracto  -  sigue el mismo patrón del template)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoggingServices(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddMainDatabase(builder.Configuration);

// Nuevo  -  Caching y Background Services
builder.Services.AddCacheServices(builder.Configuration);
builder.Services.AddHostedService<WebhookProcessorService>();
builder.Services.AddHostedService<AgentSessionCleanupService>();
builder.Services.AddHostedService<CampaignDispatcherService>();
builder.Services.AddSingleton<WebhookChannel>();

// ... resto igual que el template
builder.Services.AddJwtAuthentication(builder.Configuration);
```

> Los servicios se agregan en **extension methods por módulo** (`AddCacheServices`, `AddChannelsInfrastructureServices`, etc.)  -  nunca inline en `Program.cs`.

---

## IDistributedCache  -  implementación con JSON

```csharp
// Channels.Infrastructure/Services/DistributedCacheService.cs
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions _json =
        new() { PropertyNameCaseInsensitive = true };

    public DistributedCacheService(IDistributedCache cache) =>
        _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null) return default;
        return JsonSerializer.Deserialize<T>(bytes, _json);
    }

    public async Task SetAsync<T>(
        string key, T value,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var opts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);
        await _cache.SetAsync(key, bytes, opts, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default) =>
        await _cache.RemoveAsync(key, ct);
}
```

---

## IMemoryCache vs IDistributedCache

| Criterio | IMemoryCache | IDistributedCache (Redis) |
|----------|-------------|--------------------------|
| Velocidad | Nanosegundos (RAM local) | Microsegundos (red local) |
| Compartido entre instancias |  No |  Sí |
| Requiere infraestructura extra |  No |  Sí (Redis) |
| Survives app restart |  No |  Sí |
| Serialización necesaria |  No |  Sí (JSON/bytes) |
| **Usar cuando** | Una instancia de la API | Múltiples instancias / escalado horizontal |

> En el sistema de mensajería con una sola instancia inicial: **IMemoryCache**. Al escalar horizontalmente: migrar a **Redis** cambiando solo el registro en `CacheExtensions`.

---

## Patrón Cache-Aside en un Handler

```csharp
// Channels.Application/UseCases/GetChannels/GetChannelsHandler.cs
using Common.Messaging;
using Channels.Application.UseCases.GetChannels.Responses;
using Channels.Domain.Repositories;
using Channels.Contracts.Dtos;

namespace Channels.Application.UseCases.GetChannels;

public sealed class GetChannelsHandler
    : IRequestHandler<GetChannelsRequest, GetChannelsResponse>
{
    private readonly IChannelRepository _channels;
    private readonly ICacheService      _cache;

    public GetChannelsHandler(
        IChannelRepository channels, ICacheService cache)
    {
        _channels = channels;
        _cache    = cache;
    }

    public async Task<GetChannelsResponse> Handle(
        GetChannelsRequest request, CancellationToken ct)
    {
        var key = $"channels:{request.TenantId}";

        var cached = await _cache.GetAsync<IReadOnlyList<ChannelDto>>(key, ct);
        if (cached is not null)
            return new GetChannelsSuccess(cached);

        var channels = await _channels.GetByTenantAsync(request.TenantId, ct);
        var dtos     = channels.Select(c => new ChannelDto(c)).ToList();

        await _cache.SetAsync(key, dtos, TimeSpan.FromMinutes(10), ct);
        return new GetChannelsSuccess(dtos);
    }
}
```

---

## Invalidar caché al modificar

```csharp
// Channels.Application/UseCases/CreateChannel/CreateChannelHandler.cs
using Common.Messaging;
using Channels.Application.UseCases.CreateChannel.Responses;
using Channels.Domain.Entities;
using Channels.Domain.Repositories;

namespace Channels.Application.UseCases.CreateChannel;

public sealed class CreateChannelHandler
    : IRequestHandler<CreateChannelRequest, CreateChannelResponse>
{
    private readonly IChannelRepository _channels;
    private readonly ICacheService      _cache;

    public CreateChannelHandler(
        IChannelRepository channels, ICacheService cache)
    {
        _channels = channels;
        _cache    = cache;
    }

    public async Task<CreateChannelResponse> Handle(
        CreateChannelRequest request, CancellationToken ct)
    {
        var channel = new Channel(request.TenantId, request.Name, request.Type);
        await _channels.AddAsync(channel, ct);

        // Invalidar  -  la lista ya no es válida
        await _cache.RemoveAsync($"channels:{request.TenantId}", ct);

        return new CreateChannelSuccess(channel.PublicId);
    }
}
```

---

## Tipos de expiración (TTL)

```csharp
// IMemoryCache  -  opciones de expiración
var options = new MemoryCacheEntryOptions();

// Absoluta  -  expira X tiempo después de ser guardado
options.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
// Usar para: configuración de workspace, catálogos, lista de canales

// Sliding  -  expira X tiempo después del ÚLTIMO ACCESO
options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
// Usar para: perfil de agente activo, sesión activa

// Combinada  -  la más restrictiva gana
options.SetAbsoluteExpiration(DateTime.UtcNow.AddHours(1));
options.SetSlidingExpiration(TimeSpan.FromMinutes(15));
// Expira máximo en 1h aunque haya accesos frecuentes
```

> **Regla de oro:** la clave del caché debe incluir todos los parámetros que hacen única la respuesta. Para datos multi-tenant: `$"channels:{tenantId}"`.

---

## Cache de un Contact por PublicId + TenantId

```csharp
// Contacts.Application/UseCases/GetContact/GetContactHandler.cs
using Common.Messaging;
using Common.Results;
using Contacts.Application.UseCases.GetContact.Responses;
using Contacts.Contracts.Dtos;
using Contacts.Domain.Repositories;

namespace Contacts.Application.UseCases.GetContact;

public sealed class GetContactHandler
    : IRequestHandler<GetContactRequest, GetContactResponse>
{
    private readonly IContactRepository _contacts;
    private readonly ICacheService      _cache;

    public GetContactHandler(
        IContactRepository contacts, ICacheService cache)
    {
        _contacts = contacts;
        _cache    = cache;
    }

    public async Task<GetContactResponse> Handle(
        GetContactRequest request, CancellationToken ct)
    {
        var key    = $"contact:{request.TenantId}:{request.PublicId}";
        var cached = await _cache.GetAsync<ContactDto>(key, ct);
        if (cached is not null)
            return new GetContactSuccess(cached);

        var contact = await _contacts.GetByPublicIdAsync(
            request.PublicId, request.TenantId, ct);
        if (contact is null)
            return new GetContactNotFoundFailure("Contacto no encontrado.");

        var dto = new ContactDto(contact);
        await _cache.SetAsync(key, dto, TimeSpan.FromMinutes(5), ct);
        return new GetContactSuccess(dto);
    }
}
```

---

<!-- _class: lead -->

# Parte 2

## Output Caching en .NET 10

---

## Output Caching  -  caché a nivel HTTP

En lugar de caché dentro de los handlers, **Output Cache almacena la respuesta HTTP completa** antes de salir al cliente. No requiere ningún cambio en los handlers.

```csharp
// Host.Api/Extensions/OutputCacheExtensions.cs
using Microsoft.AspNetCore.OutputCaching;

namespace Host.Api.Extensions;

public static class OutputCacheExtensions
{
    public static IServiceCollection AddOutputCacheServices(
        this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(policy =>
                policy.Expire(TimeSpan.FromSeconds(60)));

            options.AddPolicy("channels-list", policy => policy
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByQuery("*")
                .SetVaryByHeader("X-Tenant-Id")
                .Tag("channels"));

            options.AddPolicy("per-tenant", policy => policy
                .Expire(TimeSpan.FromMinutes(10))
                .SetVaryByHeader("X-Tenant-Id")
                .Tag("tenant-data"));
        });
        return services;
    }
}
```

---

## Output Caching  -  uso en controladores

```csharp
// Channels.Presentation/Controllers/ChannelsController.cs
using Common.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Channels.Application.UseCases.GetChannels;
using Channels.Application.UseCases.GetChannels.Responses;

namespace Channels.Presentation.Controllers;

[Route("api/channels")]
[Authorize]
public sealed class ChannelsController : BaseApiController
{
    public ChannelsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    [OutputCache(PolicyName = "channels-list")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetChannelsRequest(), ct);
        return result switch
        {
            GetChannelsSuccess s => Ok(s.Data),
            _                   => StatusCode(500)
        };
    }
}
```

---

## Output Caching  -  invalidar por tag

```csharp
// Channels.Application/UseCases/CreateChannel/CreateChannelHandler.cs
using Microsoft.AspNetCore.OutputCaching;

public sealed class CreateChannelHandler
    : IRequestHandler<CreateChannelRequest, CreateChannelResponse>
{
    private readonly IChannelRepository _channels;
    private readonly IOutputCacheStore  _outputCache;

    public CreateChannelHandler(
        IChannelRepository channels, IOutputCacheStore outputCache)
    {
        _channels    = channels;
        _outputCache = outputCache;
    }

    public async Task<CreateChannelResponse> Handle(
        CreateChannelRequest request, CancellationToken ct)
    {
        var channel = new Channel(request.TenantId, request.Name, request.Type);
        await _channels.AddAsync(channel, ct);

        // Invalida TODAS las respuestas cacheadas con el tag "channels"
        await _outputCache.EvictByTagAsync("channels", ct);

        return new CreateChannelSuccess(channel.PublicId);
    }
}
```

---

## Output Cache vs IMemoryCache  -  ¿cuándo usar cada uno?

| Aspecto | Output Cache | IMemoryCache / IDistributedCache |
|---------|-------------|----------------------------------|
| Nivel | HTTP (respuesta completa) | Aplicación (objetos C#) |
| Configuración | Atributo en el controlador | Lógica en el handler |
| Vary by headers/query |  Nativo |  Hay que incluirlo en la clave |
| Invalidación por tags |  `EvictByTagAsync` |  Manual por clave |
| Lógica condicional |  Todo o nada |  Cachear solo partes |
| **Usar cuando** | Endpoints GET públicos o semi-públicos | Datos internos reutilizados entre handlers |

---

<!-- _class: lead -->

# Parte 3

## Redis  -  caché distribuido

---

## Redis: configuración completa

```xml
<!-- Channels.Infrastructure/Channels.Infrastructure.csproj -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis"
                  Version="10.*" />
```

```json
// appsettings.json
{
  "Redis": {
    "Connection": "localhost:6379"
  }
}
```

```csharp
// Host.Api/Extensions/CacheExtensions.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["Redis:Connection"]
        ?? throw new InvalidOperationException(
            "Redis:Connection no está configurado.");
    options.InstanceName = "mensajeria:";
});

builder.Services.AddScoped<ICacheService, DistributedCacheService>();
```

---

## Redis: operaciones básicas con IDistributedCache

```csharp
// GetStringAsync / SetStringAsync  -  el par más común
public async Task<ChannelDto?> GetChannelAsync(
    Guid channelId, long tenantId, CancellationToken ct)
{
    var key    = $"channel:{tenantId}:{channelId}";
    var cached = await _cache.GetStringAsync(key, ct);

    if (cached is not null)
        return JsonSerializer.Deserialize<ChannelDto>(cached);

    var channel = await _channels.GetByPublicIdAsync(channelId, tenantId, ct);
    if (channel is null) return null;

    var dto = new ChannelDto(channel);

    await _cache.SetStringAsync(
        key,
        JsonSerializer.Serialize(dto),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        },
        ct);

    return dto;
}
```

> Redis **serializa a bytes**  -  los objetos C# requieren JSON. Usar `System.Text.Json`.

---

## Redis en distribución horizontal

```
Sin Redis (IMemoryCache):
  Instancia A  --- caché propio  +
  Instancia B  --- caché propio  |-- datos inconsistentes entre instancias
  Instancia C  --- caché propio  +

Con Redis (IDistributedCache):
  Instancia A -+
  Instancia B -+-- Redis <-- caché único y compartido
  Instancia C -+
```

**Beneficios adicionales:**
- El caché sobrevive a reinicios de la API
- Un solo punto de invalidación: `await _cache.RemoveAsync(key, ct)`
- Métricas y monitoreo centralizados (Redis INFO, RedisInsight)

---

<!-- _class: lead -->

# Parte 4

## Background Services: IHostedService y BackgroundService

---

## El problema que resuelve

Algunas tareas no encajan en el ciclo request/response:

```
No se pueden resolver con un endpoint HTTP:
  - Procesar webhooks de WhatsApp en cola (no bloquear el endpoint)
  - Limpiar sesiones de agentes expiradas cada hora
  - Enviar campañas programadas de mensajes masivos
  - Calentar el caché al arrancar la aplicación
  - Sincronizar estado de conversaciones con Meta API cada 5 min
```

`IHostedService` y `BackgroundService` son el mecanismo de .NET: se registran en el contenedor DI, **arrancan con la aplicación** y se detienen cuando el host se apaga.

---

## IHostedService  -  el back-template ya lo usa

El back-template tiene **dos** hosted services propios que sirven de referencia:

```csharp
// Shared/Database/ServiceCollectionEx.cs  (extracto del template real)
internal sealed class DatabaseInitializationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseInitializationService(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var accessor = scope.ServiceProvider
            .GetRequiredService<ITenantContextAccessor>();
        accessor.Current = new TenantContext("0");

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

> Patrón idéntico al que usaremos para `CacheWarmupService`.

---

## IHostedService vs BackgroundService

```csharp
// IHostedService  -  interfaz base (para trabajo único al arrancar)
public interface IHostedService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

```csharp
// BackgroundService  -  clase base para trabajo continuo (loop)
// Implementa IHostedService internamente
public abstract class BackgroundService : IHostedService
{
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    // StartAsync lanza ExecuteAsync en background
    // StopAsync cancela el stoppingToken y espera que ExecuteAsync termine
}
```

| | IHostedService | BackgroundService |
|--|--|--|
| Caso de uso | Tarea única al iniciar | Loop continuo |
| Implementar | StartAsync + StopAsync | Solo ExecuteAsync |
| Ejemplo CRM | DatabaseInitializationService | WebhookProcessorService |

---

## BackgroundService: patrón base con PeriodicTimer

```csharp
// Conversations.Infrastructure/BackgroundServices/ConversationCleanupService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Conversations.Domain.Repositories;

namespace Conversations.Infrastructure.BackgroundServices;

public sealed class ConversationCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory              _scopeFactory;
    private readonly ILogger<ConversationCleanupService> _logger;

    public ConversationCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ConversationCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConversationCleanupService iniciado.");
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try   { await DoWorkAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogError(ex, "Error en limpieza de conversaciones."); }
        }
    }
}
```

---

## BackgroundService: el método de trabajo

```csharp
// Conversations.Infrastructure/BackgroundServices/ConversationCleanupService.cs
// (continuación  -  método DoWorkAsync)

    private async Task DoWorkAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<IConversationRepository>();

        var archived = await repo.ArchiveInactiveAsync(
            olderThan: DateTime.UtcNow.AddDays(-30), ct);

        _logger.LogInformation(
            "Conversaciones archivadas: {Count}", archived);
    }
```

> El método `DoWorkAsync` **crea su propio scope** para acceder a repositorios Scoped desde un servicio Singleton.

---

## El problema de captive dependency

Los `BackgroundService` son **Singleton**. Los repositorios son **Scoped**. No se pueden inyectar directamente.

```csharp
//  Captive dependency  -  BUG silencioso
public sealed class MyService : BackgroundService
{
    private readonly IConversationRepository _repo;
    // <- Scoped capturado dentro de un Singleton: el DbContext
    //   nunca se libera y los datos quedan stale
    public MyService(IConversationRepository repo) => _repo = repo;
}
```

```csharp
//  Correcto  -  crear un scope por ciclo de trabajo
private async Task DoWorkAsync(CancellationToken ct)
{
    await using var scope = _scopeFactory.CreateAsyncScope();
    var repo = scope.ServiceProvider
        .GetRequiredService<IConversationRepository>();

    var archived = await repo.ArchiveInactiveAsync(
        olderThan: DateTime.UtcNow.AddDays(-30), ct);

    _logger.LogInformation("Archivadas: {Count}", archived);
}
```

> Siempre inyectar `IServiceScopeFactory`, nunca servicios Scoped directamente en un BackgroundService.

---

## Registrar en el módulo  -  patrón del template

El back-template registra sus hosted services dentro del extension method del módulo correspondiente:

```csharp
// Shared/Database/ServiceCollectionEx.cs  -  patrón del template
public static IServiceCollection AddMainDatabase(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(
            configuration.GetConnectionString("MainDbConnection")));

    services.AddHostedService<DatabaseInitializationService>();
    return services;
}
```

```csharp
// Conversations.Infrastructure/ServiceCollectionEx.cs  -  mismo patrón para CRM
public static IServiceCollection AddConversationsInfrastructureServices(
    this IServiceCollection services)
{
    services.AddScoped<IConversationRepository, ConversationRepository>();
    services.AddHostedService<ConversationCleanupService>();
    return services;
}
```

---

## Graceful Shutdown  -  apagado limpio

.NET da **5 segundos** por defecto para que los hosted services terminen. Si los jobs son largos, ampliar el timeout:

```csharp
// Host.Api/Program.cs
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

```csharp
// En ExecuteAsync  -  SIEMPRE pasar stoppingToken a todas las ops async
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<ICampaignRepository>();
        var sender = scope.ServiceProvider
            .GetRequiredService<IMessageSender>();

        var due = await repo.GetDueCampaignsAsync(stoppingToken);
        foreach (var campaign in due)
            await sender.DispatchAsync(campaign, stoppingToken);
    }
}
```

---

<!-- _class: lead -->

# Parte 5

## BackgroundService en el sistema de mensajería

---

## Caso 1: Procesar cola de webhooks de WhatsApp

WhatsApp envía webhooks en tiempo real. El endpoint debe responder `200 OK` a cada notificación; si no, Meta reintenta durante 36 horas con frecuencia decreciente (por eso conviene responder rápido y procesar en segundo plano, y deduplicar). La documentación no fija un límite en milisegundos.

Fuente: Meta, *Webhooks for Graph API: Getting started*, developers.facebook.com/docs/graph-api/webhooks/getting-started (consultado en noviembre de 2026).

```csharp
// Channels.Presentation/Controllers/WebhookController.cs
[HttpPost("webhook/whatsapp")]
public async Task<IActionResult> WhatsAppWebhook(
    [FromBody] WhatsAppPayload payload, CancellationToken ct)
{
    await _webhookChannel.Writer.WriteAsync(
        new IncomingWebhook(Source: "whatsapp", Payload: payload), ct);
    return Ok();  // responde en microsegundos
}
```

```csharp
// Channels.Infrastructure/BackgroundServices/WebhookProcessorService.cs
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var webhook in
        _webhookChannel.Reader.ReadAllAsync(stoppingToken))
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider
            .GetRequiredService<IMediator>();
        await mediator.Send(
            new ProcessWebhookCommand(webhook), stoppingToken);
    }
}
```

---

## Caso 2: Limpiar sesiones de agentes expiradas

```csharp
// Agents.Infrastructure/BackgroundServices/AgentSessionCleanupService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Agents.Domain.Repositories;

namespace Agents.Infrastructure.BackgroundServices;

public sealed class AgentSessionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory               _scopeFactory;
    private readonly ILogger<AgentSessionCleanupService> _logger;

    public AgentSessionCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<AgentSessionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider
                    .GetRequiredService<IAgentSessionRepository>();

                var deleted = await repo.DeleteExpiredAsync(stoppingToken);
                _logger.LogInformation(
                    "Sesiones expiradas eliminadas: {Count}", deleted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en limpieza de sesiones.");
            }
        }
    }
}
```

---

## Caso 3: Enviar campañas programadas

```csharp
// Campaigns.Infrastructure/BackgroundServices/CampaignDispatcherService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Campaigns.Domain.Repositories;

namespace Campaigns.Infrastructure.BackgroundServices;

public sealed class CampaignDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory              _scopeFactory;
    private readonly ILogger<CampaignDispatcherService> _logger;

    public CampaignDispatcherService(
        IServiceScopeFactory scopeFactory,
        ILogger<CampaignDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope  = _scopeFactory.CreateAsyncScope();
            var repo   = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
            var sender = scope.ServiceProvider.GetRequiredService<IMessageSender>();

            var due = await repo.GetDueCampaignsAsync(stoppingToken);
            foreach (var campaign in due)
            {
                await sender.DispatchAsync(campaign, stoppingToken);
                await repo.MarkDispatchedAsync(campaign.Id, stoppingToken);
                _logger.LogInformation(
                    "Campaña {Id} enviada a {Count} contactos.",
                    campaign.Id, campaign.ContactCount);
            }
        }
    }
}
```

---

## Tabla de Background Services del CRM

| Servicio | Módulo | Intervalo | Patrón |
|---------|--------|-----------|--------|
| `DatabaseInitializationService` | Shared.Database | Al arrancar | `IHostedService` |
| `SchemaMigrationHostedService` | Common.PostgreSql | Al arrancar | `IHostedService` |
| `WebhookProcessorService` | Channels | Continuo | `Channel<T>.ReadAllAsync` |
| `ConversationCleanupService` | Conversations | Cada hora | `PeriodicTimer` |
| `AgentSessionCleanupService` | Agents | Cada hora | `PeriodicTimer` |
| `CampaignDispatcherService` | Campaigns | Cada minuto | `PeriodicTimer` |

> Los dos primeros son del template real. Los siguientes cuatro se agregan para el sistema de mensajería siguiendo el **mismo patrón**.

---

<!-- _class: lead -->

# Parte 6

## Polly: políticas de resiliencia

---

## ¿Por qué resiliencia?

Las llamadas a APIs externas fallan. La pregunta no es *si* fallará, sino *qué hace el sistema cuando falla*:

```csharp
//  Sin resiliencia  -  un timeout de Meta API tumba toda la request
var result = await _httpClient.PostAsync("/api/messages/send", content, ct);
// Si la API de WhatsApp está lenta -> el agente espera indefinidamente

//  Sin retry  -  un error transitorio falla permanentemente
var response = await _metaClient.GetAsync("/me/messages");
// Si Meta tuvo un flap de 200ms -> el webhook falla aunque sería exitoso
//   si se reintentara
```

**Polly** es la librería estándar de .NET para políticas de resiliencia.

```xml
<!-- Channels.Infrastructure/Channels.Infrastructure.csproj -->
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
```

> En .NET 9/10 se usa `Microsoft.Extensions.Http.Resilience`  -  ya incluye Polly v8 integrado con `IHttpClientBuilder`.

---

## HttpClientBuilderEx  -  el helper real del back-template

El template tiene en `Common/Common/Http/HttpClientBuilderEx.cs` dos extension methods:

```csharp
// Common/Common/Http/HttpClientBuilderEx.cs  (archivo real del template)
using Microsoft.Extensions.Http.Resilience;
using Common.MultiTenancy;

namespace Common.Http;

public static class HttpClientBuilderEx
{
    // Agrega retry + circuit breaker estándar desde config
    public static IHttpClientBuilder AddCoreResilience(
        this IHttpClientBuilder builder,
        IConfigurationSection? section = null)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            if (section is not null)
                section.Bind(options);
        });
        return builder;
    }

    // Propaga el X-Tenant-Id al servicio externo
    public static IHttpClientBuilder AddTenantPropagation(
        this IHttpClientBuilder builder)
    {
        builder.Services
            .AddTransient<TenantPropagationHttpMessageHandler>();
        builder.AddHttpMessageHandler<TenantPropagationHttpMessageHandler>();
        return builder;
    }
}
```

---

## TenantPropagationHttpMessageHandler  -  detalle

```csharp
// Common/Common/Http/HttpClientBuilderEx.cs  (clase interna del mismo archivo)
using Microsoft.Extensions.Options;

public sealed class TenantPropagationHttpMessageHandler : DelegatingHandler
{
    private readonly ITenantContextAccessor          _tenantContextAccessor;
    private readonly IOptions<MultiTenantOptions>    _options;

    public TenantPropagationHttpMessageHandler(
        ITenantContextAccessor tenantContextAccessor,
        IOptions<MultiTenantOptions> options)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _options               = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContextAccessor.GetTenantId();
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var headerName = _options.Value.TenantHeaderName;
            request.Headers.Remove(headerName);
            request.Headers.TryAddWithoutValidation(headerName, tenantId);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
```

---

## Registrar el HttpClient de Meta API con resiliencia

```csharp
// Channels.Infrastructure/ServiceCollectionEx.cs
using Common.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Channels.Infrastructure;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddChannelsInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddHostedService<WebhookProcessorService>();

        // HttpClient de Meta API con resiliencia del template
        services
            .AddHttpClient<IMetaApiClient, MetaApiClient>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["MetaApi:BaseUrl"]
                    ?? throw new InvalidOperationException(
                        "MetaApi:BaseUrl no está configurado."));
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddCoreResilience(
                configuration.GetSection("MetaApi:Resilience"))
            .AddTenantPropagation();

        return services;
    }
}
```

---

## AddStandardResilienceHandler  -  qué incluye

`AddStandardResilienceHandler` configura automáticamente una pipeline de:

```
Request
  ↓
[1] Rate Limiter    -  evita sobrecarga de salida
  ↓
[2] Total Timeout   -  timeout total de todo el intento (incluye reintentos)
  ↓
[3] Retry           -  reintentos con exponential backoff + jitter
  ↓
[4] Circuit Breaker  -  corta si hay demasiados fallos
  ↓
[5] Attempt Timeout  -  timeout de cada intento individual
  ↓
Servicio externo (Meta API)
```

```json
// appsettings.json  -  ajustar sin cambiar código
{
  "MetaApi": {
    "BaseUrl": "https://graph.facebook.com/v19.0",
    "Resilience": {
      "Retry": { "MaxRetryAttempts": 3 },
      "CircuitBreaker": { "SamplingDuration": "00:00:60" },
      "TotalRequestTimeout": { "Timeout": "00:00:30" }
    }
  }
}
```

---

## Polly v8  -  ResiliencePipeline manual (cuando necesitas más control)

```csharp
// Channels.Infrastructure/Services/MetaApiResiliencePipeline.cs
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;

var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddTimeout(TimeSpan.FromSeconds(10))
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType      = DelayBackoffType.Exponential,
        UseJitter        = true,
        ShouldHandle     = args => args.Outcome switch
        {
            { Exception: HttpRequestException }  => PredicateResult.True(),
            { Result.StatusCode: >= (HttpStatusCode)500 }
                                                 => PredicateResult.True(),
            _                                    => PredicateResult.False()
        }
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio      = 0.5,
        SamplingDuration  = TimeSpan.FromSeconds(60),
        MinimumThroughput = 10,
        BreakDuration     = TimeSpan.FromSeconds(30)
    })
    .Build();
```

---

## Retry  -  reintentar fallos transitorios

```csharp
// Ejemplo standalone con Polly v8 (sin HttpClient DI)
using Polly;
using Polly.Retry;

var retryPipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType      = DelayBackoffType.Exponential,
        UseJitter        = true,
        OnRetry          = args =>
        {
            _logger.LogWarning(
                "Retry {Attempt}/3 tras {Delay}s. Causa: {Exception}",
                args.AttemptNumber,
                args.RetryDelay.TotalSeconds,
                args.Outcome.Exception?.GetType().Name);
            return default;
        }
    })
    .Build();

// attempt 1 -> espera ~2s + jitter aleatorio
// attempt 2 -> espera ~4s + jitter
// attempt 3 -> espera ~8s + jitter
```

---

## ¿Por qué jitter en el backoff?

Sin jitter, todos los clientes que fallaron al mismo tiempo reintentarán juntos  -  **tormenta de reintentos**:

```
Sin jitter:
  t=0s:   1000 requests fallan simultáneamente
  t=2s:   1000 reintentos llegan al mismo tiempo -> servidor sobrecargado
  t=4s:   1000 reintentos llegan al mismo tiempo -> sigue saturado

Con jitter:
  t=0s:   1000 requests fallan
  t=2-3s: los reintentos llegan distribuidos (100/s) -> servidor se recupera
```

```csharp
// Con AddStandardResilienceHandler: UseJitter = true por defecto
// Con ResiliencePipelineBuilder manual:
.AddRetry(new RetryStrategyOptions
{
    BackoffType = DelayBackoffType.Exponential,
    UseJitter   = true  // <- agrega ruido aleatorio automáticamente
})
```

---

## Circuit Breaker  -  cortar el circuito

```
Closed (normal)     ->  N fallos consecutivos
      ↓
Open (abierto)      ->  rechaza TODAS las llamadas inmediatamente
      ↓ (tras BreakDuration)
Half-Open           ->  permite UNA llamada de prueba
      ↓ éxito              ↓ fallo
   Closed                 Open (reabre)
```

```csharp
.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
{
    FailureRatio      = 0.5,   // 50% de fallos en la ventana
    SamplingDuration  = TimeSpan.FromSeconds(60),
    MinimumThroughput = 10,    // mínimo 10 requests para evaluar
    BreakDuration     = TimeSpan.FromSeconds(30),
    OnOpened  = args =>
    {
        _logger.LogError(
            "Circuit breaker OPEN por {Duration}s.", args.BreakDuration);
        return default;
    },
    OnClosed = _ =>
    {
        _logger.LogInformation("Circuit breaker CLOSED  -  recuperado.");
        return default;
    }
})
```

---

<!-- _class: lead -->

# Parte 7

## Polly en HttpClient  -  APIs de WhatsApp e Instagram

---

## Cuándo aplica cada política en el CRM

| Situación en el sistema de mensajería | Política |
|---------------------------|---------|
| Meta API retorna 503 temporalmente | Retry con backoff exponencial + jitter |
| Meta API caída por varios minutos | Circuit Breaker (no gastar requests) |
| Webhook de WhatsApp tarda demasiado | Timeout (liberar el thread) |
| Todo lo anterior combinado | `AddStandardResilienceHandler` / `ResiliencePipeline` |

**Regla general:**
- **Retry** para fallos transitorios (milisegundos a segundos)
- **Circuit Breaker** para fallos sostenidos (segundos a minutos)
- Sin Circuit Breaker los reintentos agotan recursos hacia un servicio ya caído

---

## Qué NO reintentar

```csharp
//  Reintentar operaciones no idempotentes sin protección
await retryPipeline.ExecuteAsync(async ct =>
    await _conversations.AddAsync(conversation, ct));
// Si el INSERT se ejecutó pero falló la respuesta de red,
// reintentar puede insertar la conversación DOS VECES

//  Solo reintentar operaciones idempotentes
// GET                  -> siempre seguro de reintentar
// DELETE by ID         -> seguro (el segundo no encuentra nada)
// PUT/PATCH con ETag   -> seguro si hay control de concurrencia
// INSERT sin idempotency key -> NO reintentar directamente
```

```csharp
//  Con Idempotency Key  -  el servidor detecta el duplicado
await _metaClient.SendMessageAsync(new SendMessageRequest
{
    To             = contact.Phone,
    Text           = message.Body,
    IdempotencyKey = message.PublicId.ToString()  // <- UUID del mensaje
}, ct);
```

> En el sistema de mensajería: usar **IdempotencyKey** en el envío de mensajes a Meta API para que los reintentos no generen mensajes duplicados al contacto.

---

## TenantPropagation en llamadas a microservicios internos

Si el sistema de mensajería crece y extrae un servicio de notificaciones, `AddTenantPropagation` **propaga automáticamente el tenant**:

```csharp
// Notifications.Infrastructure/ServiceCollectionEx.cs
services
    .AddHttpClient<INotificationClient, NotificationClient>(client =>
    {
        client.BaseAddress = new Uri(
            configuration["NotificationService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "NotificationService:BaseUrl no configurado."));
    })
    .AddCoreResilience()           // retry + circuit breaker estándar
    .AddTenantPropagation();       // agrega X-Tenant-Id a cada request
```

```csharp
// Cada request saliente llevará automáticamente:
// X-Tenant-Id: <tenantId del contexto actual>
// Sin escribir ningún código adicional en el cliente
```

---

<!-- _class: lead -->

# Parte 8

## Cuándo NO cachear

---

## Anti-patrones de caché en el CRM

| Dato | ¿Cachear? | Razón |
|------|-----------|-------|
| Lista de canales configurados |  Sí (10 min) | Cambia pocas veces al día |
| Configuración del workspace |  Sí (30 min) | Muy estable |
| Contactos frecuentes de un agente |  Sí (5 min, key por agente) | Alta frecuencia de lectura |
| Mensajes de una conversación |  No | Tiempo real  -  llegan nuevos cada segundo |
| Estado de conversaciones activas |  No | Muta constantemente (asignación, estado) |
| Lista de agentes conectados |  No | Cambia en cada login/logout |
| Decisiones de autorización |  No | Cambios de rol deben aplicar inmediatamente |
| Datos de otro workspace |  NUNCA | Riesgo de filtración entre tenants |

---

## La clave del caché debe ser única por contexto

```csharp
//  Incorrecto  -  todos los workspaces comparten el mismo caché
var cacheKey = "channels";

//  Correcto  -  cada workspace tiene su propio caché
var cacheKey = $"channels:{tenantId}";

//  Correcto  -  datos por agente dentro del workspace
var cacheKey = $"agent-contacts:{tenantId}:{agentId}:{page}";

//  Peligroso  -  sin tenantId puede filtrar datos entre workspaces
var cacheKey = $"conversations:{agentId}";

//  Incluir tenantId siempre en datos multi-tenant
var cacheKey = $"conversations:{tenantId}:{agentId}:{status}";
```

> **Regla de seguridad:** en aplicaciones multi-tenant, **tenantId siempre debe ser parte de la clave** de caché. Un caché sin el tenant correcto puede filtrar datos entre workspaces.

---

## Resumen general

| Tecnología | Para qué | Cuándo usarla |
|-----------|---------|--------------|
| **IMemoryCache** | Datos frecuentes, una instancia | Inicio del proyecto, sin escala horizontal |
| **IDistributedCache + Redis** | Datos frecuentes, múltiples instancias | Al escalar horizontalmente |
| **Output Cache** | Respuestas HTTP GET completas | Endpoints de catálogos, configuración |
| **BackgroundService** | Trabajo continuo fuera del request | Cola de webhooks, limpieza, campañas |
| **IHostedService** | Trabajo único al arrancar | DB init, schema migrations, warmup |
| **AddCoreResilience** | Retry + CB estándar en HttpClient | Meta API, servicios internos |
| **ResiliencePipeline manual** | Control fino de políticas | Cuando `AddStandardResilienceHandler` no es suficiente |
| **AddTenantPropagation** | Propagar tenant en llamadas HTTP salientes | Microservicios internos |

---

<!-- _class: lead -->

# Fin del módulo

## Caching  -  Background Services  -  Polly

**Sistema de mensajería**  -  stack .NET 10  -  Redis  -  Polly v8  -  back-template

Rogelio Arriaga  -  Raptor Dev Services  -  Mayo 2026
