# 10 — Resiliencia: Circuit Breaker y Retry (Polly)

Polly es la librería estándar de .NET para políticas de resiliencia. Resuelve el problema de qué hacer cuando una operación falla: reintentar, esperar, o cortar el circuito para proteger el sistema.

> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.4 Building Resilient Microservices

---

## ¿Por qué resiliencia?

Las llamadas a recursos externos (HTTP, base de datos, servicios de terceros) fallan. La pregunta no es si fallará, sino qué hace el sistema cuando falla:

```csharp
// ❌ Sin resiliencia — un timeout de red tumba toda la request
var result = await _httpClient.GetAsync("/api/payment/validate");
// si el servicio de pagos está lento, la request del usuario espera indefinidamente

// ❌ Sin retry — un error transitorio de red falla permanentemente
var conn = await _factory.OpenConnectionAsync();
// si PostgreSQL estaba bajo carga por 100ms, la operación falla aunque sería exitosa si se reintentara
```

---

## Instalación

```xml
<!-- Host.csproj -->
<PackageReference Include="Polly"                       Version="8.*" />
<PackageReference Include="Polly.Extensions.Http"       Version="3.*" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.*" />
```

---

## Retry — reintentar en fallos transitorios

```csharp
// Retry básico — 3 intentos con espera exponencial
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        // attempt 1 → espera 2s, attempt 2 → 4s, attempt 3 → 8s (exponential backoff)
        onRetry: (exception, timeSpan, attempt, context) =>
        {
            _logger.LogWarning(
                "Retry {Attempt}/3 after {Delay}s due to {Exception}",
                attempt, timeSpan.TotalSeconds, exception.GetType().Name);
        });

// Uso:
var response = await retryPolicy.ExecuteAsync(
    () => _httpClient.GetAsync("/api/payment/validate"));
```

### Retry con jitter (recomendado en producción)

El exponential backoff puro puede causar una "tormenta de reintentos" si muchos clientes fallan al mismo tiempo — todos esperan los mismos intervalos y golpean el servidor a la vez:

```csharp
// Exponential backoff + jitter aleatorio — distribuye los reintentos en el tiempo
var jitterer = new Random();
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))
            + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)));  // jitter: 0-1s aleatorio
```

---

## Circuit Breaker — cortar el circuito

El Circuit Breaker tiene 3 estados:

```
Closed (normal)  →  abre cuando hay N fallos seguidos
    ↓
Open (circuito abierto)  →  rechaza todas las llamadas inmediatamente
    ↓ (después de un tiempo)
Half-Open  →  permite una llamada de prueba
    ↓ éxito          ↓ fallo
Closed             Open (vuelve a abrir)
```

```csharp
// Circuit Breaker — abre después de 5 fallos consecutivos, espera 30s
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (exception, timespan) =>
        {
            _logger.LogError(
                "Circuit breaker OPEN for {Duration}s. Cause: {Exception}",
                timespan.TotalSeconds, exception.Message);
        },
        onReset: () => _logger.LogInformation("Circuit breaker CLOSED — service recovered."),
        onHalfOpen: () => _logger.LogInformation("Circuit breaker HALF-OPEN — testing..."));

// Uso:
try
{
    var response = await circuitBreakerPolicy.ExecuteAsync(
        () => _httpClient.GetAsync("/api/payment/validate"));
}
catch (BrokenCircuitException)
{
    // El circuito está abierto — responder con un fallback inmediato
    return new PaymentServiceUnavailableFailure("Servicio de pagos temporalmente no disponible.");
}
```

---

## Combinar políticas con PolicyWrap

En producción se combinan Retry + Circuit Breaker (y opcionalmente Timeout):

```csharp
// El orden importa: Timeout envuelve Retry, Retry envuelve CircuitBreaker
//
// Timeout  →  si la operación total supera el límite, cancela todo
//   Retry  →  reintenta si la operación falla
//     CircuitBreaker  →  si ya está roto, rechaza inmediatamente
//       HttpCall

var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    seconds: 10,   // timeout total de toda la operación (incluyendo reintentos)
    timeoutStrategy: TimeoutStrategy.Optimistic);  // coopera con CancellationToken

var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutRejectedException>()
    .WaitAndRetryAsync(3, attempt =>
        TimeSpan.FromSeconds(Math.Pow(2, attempt))
        + TimeSpan.FromMilliseconds(new Random().Next(0, 500)));

var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutRejectedException>()
    .AdvancedCircuitBreakerAsync(
        failureThreshold: 0.5,               // 50% de fallos en la ventana
        samplingDuration: TimeSpan.FromSeconds(60),
        minimumThroughput: 10,               // mínimo 10 llamadas antes de evaluar
        durationOfBreak: TimeSpan.FromSeconds(30));

// Combinar: Timeout(Retry(CircuitBreaker(call)))
var resiliencePolicy = Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);

// Uso:
var response = await resiliencePolicy.ExecuteAsync(
    () => _httpClient.GetAsync("/api/payment/validate"));
```

---

## Integración con HttpClient (recomendado)

La forma más limpia de usar Polly con `HttpClient` es configurarlo en el DI:

```csharp
// Host/Extensions/HttpClientExtensions.cs
public static IServiceCollection AddPaymentHttpClient(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddHttpClient<IPaymentService, PaymentService>(client =>
        {
            client.BaseAddress = new Uri(configuration["PaymentService:BaseUrl"]
                ?? throw new InvalidOperationException("PaymentService:BaseUrl no configurado."));
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddTransientHttpErrorPolicy(builder =>
            builder.WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt))
                    + TimeSpan.FromMilliseconds(new Random().Next(0, 500))))
        .AddTransientHttpErrorPolicy(builder =>
            builder.CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30)));

    return services;
}
```

`AddTransientHttpErrorPolicy` aplica automáticamente a errores HTTP 5xx y errores de red.

---

## Polly v8 — Resilience Pipelines (API moderna)

Polly v8 introduce `ResiliencePipeline` como API unificada:

```csharp
// Polly v8 — API fluida
var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddTimeout(TimeSpan.FromSeconds(10))
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType      = DelayBackoffType.Exponential,
        UseJitter        = true,
        ShouldHandle     = args => args.Outcome switch
        {
            { Exception: HttpRequestException } => PredicateResult.True(),
            { Result.IsSuccessStatusCode: false } when
                args.Result?.StatusCode >= HttpStatusCode.InternalServerError => PredicateResult.True(),
            _ => PredicateResult.False()
        },
        OnRetry = args =>
        {
            _logger.LogWarning("Retry {Attempt}", args.AttemptNumber);
            return default;
        }
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio          = 0.5,
        SamplingDuration      = TimeSpan.FromSeconds(60),
        MinimumThroughput     = 10,
        BreakDuration         = TimeSpan.FromSeconds(30)
    })
    .Build();

// Uso:
var response = await pipeline.ExecuteAsync(
    async ct => await _httpClient.GetAsync("/api/payment/validate", ct),
    cancellationToken);
```

---

## Resiliencia en la base de datos

```csharp
// Retry para conexiones a PostgreSQL — útil en el arranque cuando la DB no está lista aún
var dbRetryPolicy = Policy
    .Handle<NpgsqlException>(ex => ex.IsTransient)
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 5,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(attempt * 2),
        onRetry: (ex, delay, attempt, _) =>
            _logger.LogWarning("DB retry {Attempt}/5 after {Delay}s: {Message}",
                attempt, delay.TotalSeconds, ex.Message));

// En el arranque de la app:
await dbRetryPolicy.ExecuteAsync(async () =>
{
    await using var conn = await _factory.OpenConnectionAsync();
    await conn.ExecuteAsync("SELECT 1");  // verifica conectividad
});
```

---

## Fallback — respuesta de respaldo

```csharp
// Fallback — retornar valor por defecto cuando el circuito está abierto
var fallbackPolicy = Policy<IEnumerable<ProductDto>>
    .Handle<BrokenCircuitException>()
    .Or<TimeoutRejectedException>()
    .FallbackAsync(
        fallbackValue: Enumerable.Empty<ProductDto>(),
        onFallbackAsync: (exception, context) =>
        {
            _logger.LogWarning("Usando fallback para productos. Causa: {Ex}", exception.Exception?.Message);
            return Task.CompletedTask;
        });

var combinedPolicy = Policy.WrapAsync(fallbackPolicy, retryPolicy, circuitBreakerPolicy);

// Si el servicio de productos falla → devuelve lista vacía en lugar de error
var products = await combinedPolicy.ExecuteAsync(() => _productService.GetAllAsync(ct));
```

---

## Cuándo aplicar cada política

| Situación | Política |
|-----------|---------|
| Error transitorio de red (timeout corto, flap) | Retry con backoff exponencial + jitter |
| Servicio externo caído por tiempo prolongado | Circuit Breaker |
| Operación que no puede durar más de N segundos | Timeout |
| Servicio crítico con degradación aceptable | Fallback |
| Todo lo anterior combinado | PolicyWrap / ResiliencePipeline |

**Regla general:** Retry para fallos transitorios (ms-segundos), Circuit Breaker para fallos sostenidos (segundos-minutos). Sin Circuit Breaker, los reintentos agotan recursos hacia un servicio que ya está caído.

---

## Qué NO hacer con reintentos

```csharp
// ❌ Reintentar operaciones no idempotentes sin protección
// Si InsertUser falla después de ejecutar el INSERT (en el network response),
// reintentar puede insertar el usuario dos veces
var retryPolicy = Policy.Handle<Exception>().RetryAsync(3);
await retryPolicy.ExecuteAsync(() => _repo.InsertAsync(user, ct));  // ← peligroso

// ✓ Solo reintentar operaciones idempotentes o con detección de duplicados
// GET, DELETE by ID, UPDATE con versión/timestamp → seguros de reintentar
// INSERT sin idempotency key → requiere manejo especial (UUID generado en el cliente)
```


---

*Rogelio Arriaga Gonzalez*
