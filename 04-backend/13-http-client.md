# 13 — HttpClient Factory y Resiliencia

Crear `HttpClient` con `new HttpClient()` es uno de los bugs más comunes en .NET: agota puertos TCP del sistema (socket exhaustion) porque `HttpClient` no libera conexiones correctamente cuando se descarta. La solución es `IHttpClientFactory`.

> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.5 Building Microservices Using Web API

---

## El problema con `new HttpClient()`

```csharp
// ❌ Socket exhaustion — cada instancia abre nuevas conexiones TCP
public class PaymentService
{
    public async Task<string> ChargeAsync(decimal amount)
    {
        using var client = new HttpClient();   // ← crea y descarta un HttpClient en cada llamada
        // ... aunque se use "using", el socket no se libera inmediatamente
    }
}

// ❌ También incorrecto — singleton de HttpClient no respeta cambios de DNS
public class PaymentService
{
    private static readonly HttpClient _client = new();  // ← no rota, ignora DNS TTL
}
```

---

## Patrón correcto — Typed HTTP Client

```csharp
// Registro en Program.cs
builder.Services.AddHttpClient<StripeClient>(client =>
{
    client.BaseAddress = new Uri("https://api.stripe.com/");
    client.Timeout     = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {stripeKey}");
});

// El cliente tipado recibe HttpClient por DI — factory gestiona el pool de conexiones
public sealed class StripeClient
{
    private readonly HttpClient _http;

    public StripeClient(HttpClient http) => _http = http;

    public async Task<Customer> CreateCustomerAsync(string email, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("v1/customers", new { email }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Customer>(cancellationToken: ct)!;
    }
}
```

---

## Resiliencia con Polly — `AddStandardResilienceHandler`

.NET 8+ integra Polly v8 con `Microsoft.Extensions.Http.Resilience`. Un solo método configura retry + circuit breaker + timeouts:

```csharp
builder.Services
    .AddHttpClient<InventoryClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:Inventory:Url"]!);
    })
    .AddStandardResilienceHandler(options =>
    {
        // Reintentos con backoff exponencial + jitter (evita thundering herd)
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType      = DelayBackoffType.Exponential;
        options.Retry.UseJitter        = true;

        // Circuit breaker — abre si el 50% de los requests en 30s fallan
        options.CircuitBreaker.FailureRatio      = 0.5;
        options.CircuitBreaker.MinimumThroughput = 10;   // mínimo de requests para evaluar
        options.CircuitBreaker.SamplingDuration  = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration     = TimeSpan.FromSeconds(30);

        // Timeouts
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);  // total incluidos reintentos
        options.AttemptTimeout.Timeout      = TimeSpan.FromSeconds(15);  // por intento individual
    });
```

---

## Named HTTP Client (cuando no se usa typed)

```csharp
// Registro
builder.Services.AddHttpClient("github", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "mi-app");
});

// Uso
public class GitHubService(IHttpClientFactory factory)
{
    public async Task<string> GetReposAsync()
    {
        var client = factory.CreateClient("github");
        return await client.GetStringAsync("repos");
    }
}
```

---

## Resumen de tipos de registro

| Tipo | Cuándo usar |
|------|------------|
| **Typed Client** (`AddHttpClient<T>`) | Un cliente por servicio externo (Stripe, SendGrid, etc.) |
| **Named Client** | Cuando el mismo servicio se usa en múltiples clases con distinta config |
| **Basic** (sin tipo ni nombre) | Caso general cuando no hay config específica por servicio |

Ver `04-backend/04-resiliencia-polly.md` para patrones avanzados de resiliencia (retry policies personalizadas, fallbacks, hedging).

---

## Glosario

| Término | Definición |
|---------|-----------|
| IHttpClientFactory | Factory de .NET que gestiona el ciclo de vida de HttpClient evitando socket exhaustion |
| Socket Exhaustion | Agotamiento de sockets TCP causado por crear y destruir HttpClient sin reutilizar conexiones |
| Typed HTTP Client | Clase que encapsula la lógica de comunicación con un servicio externo e inyecta HttpClient vía DI |
| Named Client | HttpClient registrado con nombre en DI para ser recuperado por ese nombre desde IHttpClientFactory |
| AddStandardResilienceHandler | Método de .NET 8 que aplica retry, circuit breaker y timeout predeterminados a un HttpClient |
| HttpMessageHandler | Handler de bajo nivel que procesa requests HTTP — IHttpClientFactory reutiliza su pool de handlers |
| DelegatingHandler | Handler encadenable que envuelve otro handler — usado para agregar headers, logging o retry |
| BackoffType | Enum de Polly que define la estrategia de backoff: Linear, Exponential o Constant |
| CircuitBreakerStrategy | Estrategia de resiliencia que abre el circuito tras un porcentaje de fallos en una ventana de tiempo |
| BaseAddress | Dirección base del HttpClient configurada en registro — los requests pueden usar paths relativos |
| AddHttpClient\<T\> | Método de DI que registra un Typed Client con su HttpClient asociado y configuración |

---

*Rogelio Arriaga Gonzalez*
