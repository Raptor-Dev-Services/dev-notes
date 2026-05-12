# 15 — API Versioning: Versionar sin Romper Clientes

Estrategia para evolucionar una API pública sin romper los clientes existentes.

> Fuente: *Web API Development with ASP.NET Core 8* (Xiaodi Yan) — Ch.12 API Versioning Strategies

---

## El problema que resuelve

Una API en producción tiene clientes (apps móviles, frontends, servicios terceros). Cambiar el contrato rompe esos clientes:

```
❌ Sin versionado:
   v1: GET /api/users → { "name": "John" }
   Cambio: renombrar "name" a "fullName"
   Resultado: todos los clientes que esperan "name" se rompen

✓ Con versionado:
   /api/v1/users → { "name": "John" }         ← clientes existentes siguen funcionando
   /api/v2/users → { "fullName": "John" }      ← nuevos clientes usan el nuevo contrato
```

---

## Las 4 estrategias de versionado

### 1. URL path (más común y más claro)

```
GET /api/v1/users
GET /api/v2/users
```

**Pros:** visible en la URL, fácil de cachear, fácil de probar en el browser.  
**Contras:** la URL cambia — "URLs no deben cambiar" según REST puro.

### 2. Query string

```
GET /api/users?api-version=1.0
GET /api/users?api-version=2.0
```

**Pros:** URL base no cambia.  
**Contras:** más difícil de cachear, menos visible.

### 3. Header HTTP

```
GET /api/users
Api-Version: 1.0
```

**Pros:** URL limpia.  
**Contras:** no visible en browser, requiere configuración en clientes.

### 4. Media type (Content Negotiation)

```
GET /api/users
Accept: application/vnd.myapi.v1+json
```

**Pros:** REST "puro".  
**Contras:** complejo, poco adoptado en APIs REST típicas.

---

## Implementación con Asp.Versioning

```xml
<!-- Host/Host.csproj -->
<PackageReference Include="Asp.Versioning.Mvc"         Version="8.*" />
<PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.*" />
```

```csharp
// Host/Extensions/ApiVersioningExtensions.cs
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion             = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions              = true;  // agrega headers api-supported-versions

            // Aceptar versión por URL, header o query string
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat           = "'v'VVV";   // v1, v2, v1.1
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}

// Host/Program.cs
builder.Services.AddApiVersioning();
```

---

## Controllers versionados

### Estrategia A — controllers separados por versión (recomendada)

```csharp
// WebApi/EndPoints/ExampleUsers/V1/ExampleUsersController.cs
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/example/users")]
[Authorize]
public sealed class ExampleUsersV1Controller : BaseApiController
{
    private readonly ResultViewModel<ExampleUsersV1Controller> _viewModel;

    public ExampleUsersV1Controller(
        IMediator mediator,
        ResultViewModel<ExampleUsersV1Controller> viewModel) : base(mediator)
    {
        _viewModel = viewModel;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        _ = await Mediator.Send(new GetExampleUserV1Request(id), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }
}

// WebApi/EndPoints/ExampleUsers/V2/ExampleUsersController.cs
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/example/users")]
[Authorize]
public sealed class ExampleUsersV2Controller : BaseApiController
{
    // Puede reusar handlers v1 o tener handlers nuevos
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        _ = await Mediator.Send(new GetExampleUserV2Request(id), ct);  // ← nuevo contrato
        // ...
    }
}
```

### Estrategia B — un controller con múltiples versiones

```csharp
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/example/users")]
public sealed class ExampleUsersController : BaseApiController
{
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetByIdV1(Guid id, CancellationToken ct = default)
    {
        // contrato v1
    }

    [HttpGet("{id:guid}")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> GetByIdV2(Guid id, CancellationToken ct = default)
    {
        // contrato v2
    }
}
```

La Estrategia A escala mejor — controllers separados son más fáciles de eliminar cuando se depreca una versión.

---

## Deprecar una versión

```csharp
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/example/users")]
public sealed class ExampleUsersController : BaseApiController { ... }
```

Con `ReportApiVersions = true`, la respuesta incluirá:
```
api-supported-versions: 2.0
api-deprecated-versions: 1.0
Sunset: Sat, 01 Jan 2027 00:00:00 GMT  // fecha de fin de vida
```

---

## Swagger con múltiples versiones

```csharp
// Host/Extensions/SwaggerExtensions.cs
public static IServiceCollection AddSwaggerWithVersioning(
    this IServiceCollection services)
{
    services.AddSwaggerGen(options =>
    {
        // Agregar un documento Swagger por versión
        var provider = services
            .BuildServiceProvider()
            .GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title   = $"Back Template API {description.GroupName}",
                Version = description.ApiVersion.ToString()
            });
        }

        // ... Bearer auth config ...
    });

    return services;
}

// Host/Program.cs
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            description.GroupName);
});
```

---

## Estrategia de requests/handlers por versión

### Opción A — requests separados por versión

```csharp
// Application/UseCases/ExampleUsers/Get/V1/GetExampleUserV1Request.cs
public sealed record GetExampleUserV1Request(Guid PublicId)
    : IRequest<GetExampleUserV1Response>;

// Application/UseCases/ExampleUsers/Get/V2/GetExampleUserV2Request.cs
public sealed record GetExampleUserV2Request(Guid PublicId)
    : IRequest<GetExampleUserV2Response>;
// V2 puede retornar campos adicionales o con distinta estructura
```

### Opción B — mismo handler, DTO diferente

```csharp
// Mismo handler, misma entidad, distinto DTO de salida
public sealed record GetExampleUserV1Dto(string Name, string Email);
public sealed record GetExampleUserV2Dto(string FullName, string Email, DateTime CreatedAt);
// El handler reutiliza la lógica, solo el contrato de respuesta cambia
```

---

## Versionado sin librería — simple y sin dependencias

Para proyectos pequeños o APIs internas donde el contrato es estable:

```csharp
// Simplemente prefijo en la ruta — sin librería
[Route("api/v1/example/users")]
public sealed class ExampleUsersV1Controller : BaseApiController { ... }

[Route("api/v2/example/users")]
public sealed class ExampleUsersV2Controller : BaseApiController { ... }
```

Funciona perfectamente para la mayoría de casos. La librería `Asp.Versioning` añade headers, deprecación automática, Swagger multi-versión — útil cuando la API es pública y tiene muchos consumidores.

---

## Relación con back-template

La plantilla actual usa `[Route("api/example/users")]` sin versionado explícito. Para agregar versionado:

1. Instalar `Asp.Versioning.Mvc` en `Host.csproj`.
2. Crear `Host/Extensions/ApiVersioningExtensions.cs`.
3. Cambiar rutas de controllers a `api/v{version:apiVersion}/...`.
4. Decorar controllers con `[ApiVersion("1.0")]`.
5. Actualizar `SwaggerExtensions.cs` para soporte multi-versión.

---

## Cuándo usar / Cuándo no usar

| Escenario | Decisión |
|-----------|----------|
| API pública con clientes externos | ✓ Versionar desde v1 |
| API interna con un solo equipo | ✓ Considerar versionado tardío — complicar cuando haya necesidad |
| Breaking change inevitable | ✓ Nueva versión major |
| Agregar campo opcional | ✗ No requiere nueva versión (backwards compatible) |
| Renombrar campo | ✓ Nueva versión |
| Cambiar tipo de dato | ✓ Nueva versión |
| Eliminar campo | ✓ Deprecar en versión actual, eliminar en la siguiente |

**Regla de compatibilidad:** agregar campos opcionales es backwards compatible. Renombrar, eliminar o cambiar la semántica de un campo requiere nueva versión.


---

*Rogelio Arriaga Gonzalez*
