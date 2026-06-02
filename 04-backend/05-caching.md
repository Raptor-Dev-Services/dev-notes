# 11 — Caching: IMemoryCache, IDistributedCache y Redis

Guardar temporalmente resultados costosos para no recalcularlos en cada request. El problema central es cuándo invalidar esa copia.

---

## El problema que resuelve

Cada request que lee datos repite el mismo trabajo: viaje de red a PostgreSQL, parse de filas, hidratación de objetos.

```csharp
// ❌ Sin caché — cada GET /api/example/users/{id} golpea la DB
public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
    // 5ms de latencia de DB × 1000 req/s = 5000ms de presión acumulada
    if (user is null) return new GetExampleUserNotFoundFailure("No encontrado.");
    return new GetExampleUserSuccess(new ExampleUserDto(user));
}
```

El caché resuelve esto cuando los datos cambian con poca frecuencia (perfil de usuario, catálogos, configuración).

---

## IMemoryCache — caché en proceso

Vive en la memoria del proceso. Rápido (nanosegundos), pero no se comparte entre instancias.

```csharp
// Host/Program.cs
builder.Services.AddMemoryCache();

// Application/Abstractions/ICacheService.cs  (interfaz en Application)
public interface ICacheService
{
    bool TryGet<T>(string key, out T? value);
    void Set<T>(string key, T value, TimeSpan? expiry = null);
    void Remove(string key);
}

// Infrastructure/Services/MemoryCacheService.cs
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

### Patrón Cache-Aside en un Handler

```csharp
public sealed class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;
    private readonly ICacheService          _cache;

    public GetExampleUserHandler(IExampleUserRepository repo, ICacheService cache)
    {
        _repo  = repo;
        _cache = cache;
    }

    public async Task<GetExampleUserResponse> Handle(
        GetExampleUserRequest request, CancellationToken ct)
    {
        var cacheKey = $"user:{request.PublicId}";

        // 1. Buscar en caché
        if (_cache.TryGet<ExampleUserDto>(cacheKey, out var cached) && cached is not null)
            return new GetExampleUserSuccess(cached);

        // 2. Si no hay, ir a DB
        var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
        if (user is null)
            return new GetExampleUserNotFoundFailure("Usuario no encontrado.");

        var dto = new ExampleUserDto(user);

        // 3. Guardar en caché para la próxima request
        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));

        return new GetExampleUserSuccess(dto);
    }
}
```

### Invalidar el caché al actualizar

```csharp
// Handler de Update — siempre invalida la entrada al modificar
public async Task<UpdateExampleUserResponse> Handle(
    UpdateExampleUserRequest request, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
    if (user is null) return new UpdateExampleUserNotFoundFailure("No encontrado.");

    // ... actualizar campos ...
    await _repo.UpdateAsync(user, ct);

    // Invalidar para que la siguiente GET vaya a DB y refleje los cambios
    _cache.Remove($"user:{request.PublicId}");

    return new UpdateExampleUserSuccess(new ExampleUserDto(user));
}
```

---

## IDistributedCache — caché compartido entre instancias

Redis es la implementación estándar. Necesario en entornos con múltiples instancias de la API.

```csharp
// Host/Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"]
        ?? throw new InvalidOperationException("Redis:ConnectionString no configurado.");
    options.InstanceName = "back-template:";
});

// appsettings.json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

```xml
<!-- Infrastructure/Infrastructure.csproj -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.*" />
```

### Implementación con IDistributedCache

```csharp
// Infrastructure/Services/DistributedCacheService.cs
public sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache  _cache;
    private static readonly JsonSerializerOptions _json =
        new() { PropertyNameCaseInsensitive = true };

    public DistributedCacheService(IDistributedCache cache) => _cache = cache;

    public bool TryGet<T>(string key, out T? value)
    {
        var bytes = _cache.Get(key);
        if (bytes is null) { value = default; return false; }
        value = JsonSerializer.Deserialize<T>(bytes, _json);
        return value is not null;
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5)
        };
        _cache.Set(key, JsonSerializer.SerializeToUtf8Bytes(value, _json), options);
    }

    public void Remove(string key) => _cache.Remove(key);
}
```

---

## OutputCache — caché en la capa HTTP

Caché a nivel de respuesta HTTP completa. No requiere lógica en los handlers.

```csharp
// Host/Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromSeconds(60)));

    options.AddPolicy("users-list", policy => policy
        .Expire(TimeSpan.FromMinutes(5))
        .Tag("users"));
});

app.UseOutputCache();  // antes de MapControllers

// Controller
[HttpGet]
[OutputCache(PolicyName = "users-list")]
public async Task<IActionResult> GetAll(CancellationToken ct) { ... }

// Invalidar el tag "users" cuando se modifica un usuario
// (desde un Handler o Handler + IOutputCacheStore)
await _cacheStore.EvictByTagAsync("users", ct);
```

---

## Tipos de expiración

```csharp
// Absoluta — expira X tiempo después de ser guardado
options.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
// Ejemplo: token de autenticación, código OTP

// Sliding — expira X tiempo después del ÚLTIMO ACCESO
options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
// Ejemplo: sesión activa, perfil de usuario activo

// Combinada — la más restrictiva gana
options.SetAbsoluteExpiration(DateTime.UtcNow.AddHours(1));
options.SetSlidingExpiration(TimeSpan.FromMinutes(15));
// Expira a la hora si nadie accede, o a los 15 min del último acceso
```

---

## Relación con back-template

El proyecto no tiene caché configurado por defecto. Para agregarlo:

1. Registrar en `Infrastructure/ServiceCollectionEx.cs`:
   ```csharp
   services.AddMemoryCache();
   services.AddScoped<ICacheService, MemoryCacheService>();
   ```
2. Definir interfaz `ICacheService` en `Application/Abstractions/`.
3. Implementar en `Infrastructure/Services/`.
4. Inyectar en los Handlers que leen datos frecuentemente (`GetExampleUserHandler`).
5. Invalidar en los Handlers que modifican (`UpdateExampleUserHandler`, `DeleteExampleUserHandler`).

---

## Cuándo usar / Cuándo no usar

| Escenario | Decisión |
|-----------|----------|
| Datos de catálogo (países, categorías, configuración) | ✓ Caché largo (horas) |
| Perfil de usuario — se lee mucho, cambia poco | ✓ Caché corto (5-10 min) |
| Resultado de cómputo costoso (reportes) | ✓ Caché con invalidación explícita |
| Múltiples instancias de la API | ✓ Redis (IDistributedCache) |
| Una sola instancia | ✓ IMemoryCache (más simple) |
| Datos que cambian en cada request | ✗ No cachear |
| Datos de inventario en tiempo real | ✗ No cachear |
| Listado que incluye datos del usuario actual | ✗ No cachear sin key por usuario |

**Regla de oro:** la clave del caché debe incluir todos los parámetros que hacen única la respuesta. Para datos por usuario: `$"user:{userId}:{resourceId}"`.

---

## Caché por pipeline behavior (MediatR)
> Fuente: *Apps and Services with .NET 8* (Price) — Ch.8 Caching Strategies

En lugar de poner lógica de caché en cada Handler, se centraliza en un Pipeline Behavior de MediatR. Así los Handlers no saben nada del caché.

```csharp
// Interfaz que marcan los Queries que quieren caché
public interface ICacheableQuery
{
    string   CacheKey   { get; }
    TimeSpan Expiration { get; }
}

// El Query declara su propia clave y TTL
public sealed record GetExampleUserRequest(Guid PublicId)
    : IRequest<GetExampleUserResponse>, ICacheableQuery
{
    public string   CacheKey   => $"user:{PublicId}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}

// Behavior de caché — se ejecuta ANTES de llegar al Handler
public sealed class QueryCachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableQuery
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<QueryCachingBehavior<TRequest, TResponse>> _logger;

    public QueryCachingBehavior(
        IDistributedCache cache,
        ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var key    = request.CacheKey;
        var cached = await _cache.GetStringAsync(key, ct);

        if (cached is not null)
        {
            _logger.LogDebug("Cache HIT: {Key}", key);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        _logger.LogDebug("Cache MISS: {Key}", key);
        var response = await next();

        await _cache.SetStringAsync(key,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = request.Expiration
            }, ct);

        return response;
    }
}
```

```csharp
// Registrar el behavior en Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(QueryCachingBehavior<,>));
});
```

```csharp
// Invalidar el caché desde un Command Handler — sin conocer el behavior
public sealed class UpdateExampleUserHandler
    : IRequestHandler<UpdateExampleUserRequest, UpdateExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;
    private readonly IDistributedCache      _cache;

    public async Task<UpdateExampleUserResponse> Handle(
        UpdateExampleUserRequest request, CancellationToken ct)
    {
        var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
        if (user is null) return new UpdateExampleUserNotFoundFailure("No encontrado.");

        user.UpdateEmail(request.NewEmail);
        await _repo.UpdateAsync(user, ct);

        // Invalida la misma clave que usa el Query
        await _cache.RemoveAsync($"user:{request.PublicId}", ct);

        return new UpdateExampleUserSuccess(new ExampleUserDto(user));
    }
}
```

---

## Stampede de caché y cómo evitarlo

El **cache stampede** ocurre cuando muchos requests llegan al mismo tiempo mientras el caché expiró — todos van a la DB al mismo tiempo.

```
Problema:
    t=0:  1000 req/s llegan → caché expiró → 1000 queries a DB simultáneos → DB se sobrecarga

Solución 1 — Probabilistic Early Expiration (PER):
    Recalcular antes de que expire con probabilidad proporcional a la cercanía del vencimiento.

Solución 2 — Lock/Mutex:
    Solo un request recalcula; los demás esperan o sirven stale data.

Solución 3 — Stale-While-Revalidate:
    Servir el valor vencido mientras se recalcula en background.
```

```csharp
// Solución práctica con SemaphoreSlim — evita stampede para keys individuales
public sealed class StampedeSafeCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        // Intento rápido sin lock
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached is not null) return JsonSerializer.Deserialize<T>(cached);

        // Adquirir lock específico para esta key
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Segunda verificación dentro del lock (otro thread pudo haber poblado ya)
            cached = await _cache.GetStringAsync(key, ct);
            if (cached is not null) return JsonSerializer.Deserialize<T>(cached);

            var value = await factory();
            await _cache.SetStringAsync(key,
                JsonSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry },
                ct);
            return value;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
```

```csharp
// Uso en Handler
public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken ct)
{
    var dto = await _cacheService.GetOrSetAsync(
        key:     $"user:{request.PublicId}",
        factory: async () =>
        {
            var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
            return user is null ? null : new ExampleUserDto(user);
        },
        expiry: TimeSpan.FromMinutes(10),
        ct: ct);

    return dto is null
        ? new GetExampleUserNotFoundFailure("No encontrado.")
        : new GetExampleUserSuccess(dto);
}
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| IMemoryCache | Interfaz de .NET para caché en memoria del proceso — no se comparte entre instancias |
| IDistributedCache | Interfaz de .NET para caché distribuida (Redis, SQL Server) — compartida entre instancias |
| Cache-Aside | Patrón donde la aplicación gestiona la caché: leer caché primero, si miss consultar DB y guardar |
| TTL | Time To Live — tiempo de expiración de una entrada de caché |
| Cache Stampede | Fenómeno donde múltiples requests simultáneos llegan a una caché vacía y saturan la base de datos |
| SemaphoreSlim | Mecanismo de sincronización usado para proteger el recálculo de caché ante stampede |
| ICacheService | Interfaz del back-template que abstrae IMemoryCache e IDistributedCache con GetOrCreateAsync |
| QueryCachingBehavior | Pipeline behavior que cachea respuestas de queries antes de llegar al handler |
| AbsoluteExpiration | Expiración absoluta de caché — la entrada expira en una fecha/hora fija |
| SlidingExpiration | Expiración deslizante — la entrada expira si no se accede en X tiempo |
| Redis | Base de datos en memoria de alto rendimiento usada como caché distribuida en producción |
| Output Cache | Caché a nivel de middleware que almacena la respuesta HTTP completa — independiente de la lógica |

---

*Rogelio Arriaga Gonzalez*
