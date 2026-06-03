# 05 — LLMOps: Operar sistemas basados en LLMs en producción

LLMOps es el conjunto de prácticas para diseñar, deployar, monitorear y mejorar aplicaciones que usan Modelos de Lenguaje Grande (LLMs) en producción.

---

## LLMOps vs MLOps vs DevOps
> Fuente: *Essential Guide to LLMOps*: Ch.1 Introduction; Ch.2 The LLM Development Lifecycle

```
DevOps:  código determinista → tests unitarios → CI/CD tradicional
MLOps:   modelo estadístico → métricas de modelo (accuracy, F1) → reentrenamiento
LLMOps:  LLM no determinista → evaluación semántica → fine-tuning / prompt engineering

Diferencias clave con DevOps tradicional:
- No hay "respuesta correcta" exacta — las respuestas son evaluadas semánticamente
- Los prompts son código — se versionan y se testean
- El modelo puede cambiar sin aviso (el proveedor actualiza el modelo base)
- La latencia y el costo varían por request según el tamaño de la respuesta
```

---

## Arquitectura de una aplicación con LLM

```csharp
// Capa de abstracción — no acoplar al proveedor específico
// Application/Abstractions/IAICompletionService.cs
public interface IAICompletionService
{
    Task<string> CompleteAsync(string prompt, AICompletionOptions options, CancellationToken ct);
    IAsyncEnumerable<string> StreamAsync(string prompt, AICompletionOptions options, CancellationToken ct);
}

public sealed record AICompletionOptions(
    string  Model         = "claude-sonnet-4-6",
    int     MaxTokens     = 1024,
    float   Temperature   = 0.7f,
    string? SystemPrompt  = null);
```

```csharp
// Infrastructure/AI/AnthropicCompletionService.cs
// Implementación concreta — puede reemplazarse sin cambiar Application
public sealed class AnthropicCompletionService : IAICompletionService
{
    private readonly HttpClient _http;
    private readonly string     _apiKey;

    public async Task<string> CompleteAsync(
        string prompt, AICompletionOptions options, CancellationToken ct)
    {
        var request = new
        {
            model      = options.Model,
            max_tokens = options.MaxTokens,
            system     = options.SystemPrompt,
            messages   = new[] { new { role = "user", content = prompt } }
        };

        var response = await _http.PostAsJsonAsync("/v1/messages", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct);
        return result?.Content.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt, AICompletionOptions options,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Streaming de tokens usando SSE
        var request = new { model = options.Model, stream = true, /* ... */ };
        using var response = await _http.PostAsJsonAsync("/v1/messages", request, ct);
        using var stream   = await response.Content.ReadAsStreamAsync(ct);
        using var reader   = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") == true)
            {
                var data = line[6..];
                if (data == "[DONE]") break;
                var chunk = JsonSerializer.Deserialize<StreamChunk>(data);
                if (chunk?.Delta?.Text is string text)
                    yield return text;
            }
        }
    }
}
```

---

## Gestión y versionado de prompts

Los prompts son código. Deben versionarse, testearse y desplegarse con el mismo rigor.

```csharp
// Application/AI/Prompts/UserSummaryPrompt.cs
public static class UserSummaryPrompt
{
    // v1.0 — prompt base
    public const string SystemV1 = """
        Eres un asistente especializado en análisis de usuarios.
        Responde siempre en español.
        Sé conciso — máximo 3 párrafos.
        """;

    public static string BuildUserPrompt(ExampleUser user, IEnumerable<Order> orders) =>
        $"""
        Resume el perfil del siguiente usuario:
        Nombre: {user.FullName}
        Email: {user.Email}
        Miembro desde: {user.CreatedAtUtc:dd/MM/yyyy}
        Total de pedidos: {orders.Count()}
        Gasto total: ${orders.Sum(o => o.Total):F2}
        Último pedido: {orders.MaxBy(o => o.CreatedAtUtc)?.CreatedAtUtc:dd/MM/yyyy ?? "Sin pedidos"}
        """;
}

// Registro de versiones — para comparar A/B
public enum PromptVersion { V1, V2 }

public sealed class UserSummaryService
{
    private readonly IAICompletionService _ai;
    private readonly PromptVersion        _version;

    public async Task<string> GetUserSummaryAsync(
        ExampleUser user, IEnumerable<Order> orders, CancellationToken ct)
    {
        var (system, userPrompt) = _version switch
        {
            PromptVersion.V1 => (UserSummaryPrompt.SystemV1, UserSummaryPrompt.BuildUserPrompt(user, orders)),
            PromptVersion.V2 => (UserSummaryPromptV2.System, UserSummaryPromptV2.BuildUserPrompt(user, orders)),
            _                => throw new ArgumentException("Versión de prompt desconocida")
        };

        return await _ai.CompleteAsync(
            userPrompt,
            new AICompletionOptions(SystemPrompt: system),
            ct);
    }
}
```

---

## Evaluación de respuestas — cómo testear LLMs

A diferencia del código determinista, los LLMs requieren evaluación semántica.

```csharp
// Tests de evaluación — verifican que la respuesta cumple criterios específicos
public sealed class UserSummaryEvaluationTests
{
    private readonly IAICompletionService _ai;
    private readonly UserSummaryService   _service;

    [Fact]
    public async Task GetUserSummary_ActiveUser_ContainsNameAndRecentActivity()
    {
        // Arrange
        var user   = new ExampleUser { FullName = "María García", Email = "maria@test.com", IsActive = true };
        var orders = new[] { new Order { Total = 500m, CreatedAtUtc = DateTime.UtcNow.AddDays(-5) } };

        // Act
        var summary = await _service.GetUserSummaryAsync(user, orders, CancellationToken.None);

        // Assert — evaluación semántica, no igualdad exacta
        summary.Should().NotBeNullOrWhiteSpace();
        summary.Should().Contain("María", StringComparison.OrdinalIgnoreCase);
        summary.Length.Should().BeLessThan(1000);   // verificar que es conciso
        // No verificar el texto exacto — el LLM puede variar la redacción
    }

    [Fact]
    public async Task GetUserSummary_ResponseIsInSpanish()
    {
        var user   = new ExampleUser { FullName = "Test User" };
        var orders = Enumerable.Empty<Order>();

        var summary = await _service.GetUserSummaryAsync(user, orders, CancellationToken.None);

        // Verificar que el idioma es español mediante palabras clave
        var spanishIndicators = new[] { "usuario", "perfil", "pedido", "sin", "desde", "cuenta" };
        summary.ToLowerInvariant()
            .Should().ContainAny(spanishIndicators, "la respuesta debe estar en español");
    }
}
```

---

## Observabilidad de LLMs — qué monitorear

```csharp
// Logging estructurado de llamadas a LLMs
public sealed class ObservableAIService : IAICompletionService
{
    private readonly IAICompletionService _inner;
    private readonly ILogger<ObservableAIService> _logger;

    public async Task<string> CompleteAsync(
        string prompt, AICompletionOptions options, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _inner.CompleteAsync(prompt, options, ct);
            sw.Stop();

            _logger.LogInformation(
                "LLM call: {Model} | Prompt: {PromptTokens} tokens est. | " +
                "Response: {ResponseLength} chars | Latency: {LatencyMs}ms",
                options.Model,
                EstimateTokens(prompt),
                response.Length,
                sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM call failed: {Model} | Latency: {LatencyMs}ms",
                options.Model, sw.ElapsedMilliseconds);
            throw;
        }
    }

    private static int EstimateTokens(string text) => text.Length / 4;  // estimación aproximada
}

// Decorar el servicio real con el observable
builder.Services.AddScoped<AnthropicCompletionService>();
builder.Services.AddScoped<IAICompletionService>(sp =>
    new ObservableAIService(
        sp.GetRequiredService<AnthropicCompletionService>(),
        sp.GetRequiredService<ILogger<ObservableAIService>>()));
```

---

## Guardrails — controlar las salidas del LLM

```csharp
// Validar que la respuesta cumple restricciones antes de devolverla al usuario
public sealed class GuardrailedAIService : IAICompletionService
{
    private readonly IAICompletionService _inner;

    // Patrones que no deben aparecer en las respuestas
    private static readonly string[] _blockedPatterns =
    [
        "contraseña", "password", "token", "secret", "api_key"
    ];

    public async Task<string> CompleteAsync(
        string prompt, AICompletionOptions options, CancellationToken ct)
    {
        var response = await _inner.CompleteAsync(prompt, options, ct);

        // Verificar que no hay información sensible en la respuesta
        foreach (var pattern in _blockedPatterns)
        {
            if (response.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                // Loguear para revisión y devolver respuesta genérica
                throw new AIGuardrailException(
                    $"La respuesta contiene contenido bloqueado: '{pattern}'");
            }
        }

        return response;
    }
}
```

---

## Gestión de costos

```csharp
// Middleware de control de costos — evitar gastos inesperados
public sealed class CostControlMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        if (userId is not null)
        {
            var key   = $"ai_tokens:{userId}:{DateTime.UtcNow:yyyy-MM-dd}";
            var used  = int.Parse(await _cache.GetStringAsync(key) ?? "0");
            var limit = 50_000;   // 50k tokens por día por usuario

            if (used >= limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Límite diario de tokens alcanzado. Intenta mañana."
                });
                return;
            }
        }

        await _next(context);
    }
}
```

---

## Cuándo construir vs usar un servicio existente

| Construir propio | Usar servicio gestionado |
|-----------------|-------------------------|
| Datos muy sensibles que no pueden salir de la infraestructura propia | Apps que no tienen restricciones de privacidad |
| Control total sobre el modelo y el fine-tuning | Prototipo o MVP — velocidad sobre control |
| Volumen muy alto donde el costo de API es prohibitivo | Equipo sin expertise en infraestructura de LLMs |
| Requisitos de latencia que solo se cumpren con modelo local | Modelo necesita actualizarse frecuentemente |

---

## Glosario

| Término | Definición |
|---------|-----------|
| LLMOps | prácticas de ingeniería para desplegar, monitorear y mantener sistemas con modelos de lenguaje en producción |
| LLM | Large Language Model — modelo de lenguaje de gran escala entrenado con texto masivo para generar y comprender lenguaje natural |
| RAG | Retrieval-Augmented Generation — técnica que recupera documentos relevantes antes de generar la respuesta, reduciendo hallucinations |
| Fine-tuning | proceso de ajustar los pesos de un LLM preentrenado con datos específicos del dominio para mejorar su rendimiento |
| Temperatura | parámetro que controla la aleatoriedad de las respuestas del LLM; valores altos son más creativos, bajos más deterministas |
| Guardrail | validación o filtro aplicado a las entradas o salidas del LLM para evitar respuestas inseguras o incorrectas |
| Prompt injection | ataque donde un usuario inserta instrucciones en su input para sobreescribir el system prompt del modelo |
| Semantic cache | caché que almacena respuestas a prompts similares semánticamente, no solo idénticos textualmente |
| Fallback | respuesta alternativa que se devuelve cuando el LLM falla o excede el tiempo de respuesta permitido |
| Decorator pattern en AI | patrón que envuelve el servicio de AI con capas de observabilidad, guardrails y control de costos sin modificar el servicio base |

---

*Rogelio Arriaga Gonzalez*
