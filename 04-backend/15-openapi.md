# 15 — API Versioning, OpenAPI y Documentación

> Fuente: *Web API Development with ASP.NET Core 8* (Xiaodi Yan) — Ch.3 Working with OpenAPI and Swagger

---

## API Versioning

| Estrategia | Pros | Contras |
|------------|------|---------|
| **URL** `/api/v1/customers` | Visible, cacheable, fácil de testear | Cambia las URLs — breaking change para clientes |
| **Header** `api-version: 1.0` | URLs limpias | Menos visible, complica testing con curl/browser |
| **Query string** `?api-version=1.0` | Fácil de testear desde browser | "Mancha" las URLs |
| **Media type** `Accept: application/vnd.v1+json` | RESTful puro | Complejo en práctica, poco usado |

```csharp
// dotnet add package Asp.Versioning.Mvc
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("api-version"));
    options.ReportApiVersions = true;   // añade header "api-supported-versions" en la respuesta
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat        = "'v'V";   // v1, v2
    options.SubstituteApiVersionInUrl = true;
});

// Controller con múltiples versiones
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/customers")]
public class CustomersController : ControllerBase
{
    [HttpGet, MapToApiVersion("1.0")]
    public Task<IActionResult> GetV1() { /* formato legacy */ }

    [HttpGet, MapToApiVersion("2.0")]
    public Task<IActionResult> GetV2() { /* formato nuevo */ }
}
```

---

## OpenAPI en .NET 9 (nativo)

.NET 9 incluye soporte OpenAPI sin Swashbuckle:

```csharp
// Program.cs
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title   = "Mi API";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

app.MapOpenApi();   // expone /openapi/v1.json

// UI moderna con Scalar (alternativa a Swagger UI)
// dotnet add package Scalar.AspNetCore
app.MapScalarApiReference();   // UI en /scalar/v1
```

---

## Swashbuckle — para proyectos en .NET 8 y anteriores

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Mi API",
        Version     = "v1",
        Description = "API SaaS multi-tenant",
    });

    // Autenticación JWT en Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        Description = "Ingresar token JWT (sin el prefijo 'Bearer')",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;   // UI en la raíz
    });
}
```

---

## Problem Details (RFC 7807)

Formato estándar para respuestas de error — todos los clientes pueden parsearlo de forma uniforme:

```csharp
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"]  = ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["tenantId"] = ctx.HttpContext.User.FindFirst("tenant_id")?.Value;
    };
});

app.UseExceptionHandler();
app.UseStatusCodePages();
```

```json
// Respuesta de error estándar
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "El usuario con ID abc123 no existe.",
  "traceId": "0HN5N7MVIGQG0:00000001",
  "tenantId": "tenant-xyz"
}
```

Ver `04-backend/08-problem-details.md` para la implementación completa con manejo de excepciones.

---

*Rogelio Arriaga Gonzalez*
