# 13 — REPR Pattern (Request-EndPoint-Response)

El patrón REPR (se pronuncia "reaper") organiza cada endpoint de una API como una unidad autónoma de tres piezas: el objeto de entrada, la lógica del endpoint, y el objeto de salida. Es una alternativa al modelo MVC que alinea el diseño del backend directamente con la semántica HTTP de request-response.

> Fuente: *Architecting ASP.NET Core Applications, 3a ed.* (Carl-Hugo Marcotte) — Ch.18 Request-EndPoint-Response (REPR)

---

## El problema que resuelve

```csharp
// ❌ Con MVC — el endpoint vive en un controller genérico que agrupa
// operaciones no relacionadas; el model binding es indirecto y verbose

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _svc;
    // El controller mezcla listas, detalles, creación, eliminación...
    // Cambiar un endpoint puede afectar los otros sin querer
    [HttpGet] public async Task<IActionResult> GetAll() { ... }
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) { ... }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateProductDto dto) { ... }
    // N métodos en un solo archivo — crecimiento no controlado
}
```

Con REPR, cada operación es una clase independiente con todo su código en un solo lugar:

```csharp
// ✓ REPR — la feature ShuffleText es autocontenida
public class ShuffleText
{
    public record class Request(string Text);   // entrada
    public record class Response(string Text);  // salida

    public class Endpoint                        // lógica
    {
        public Response Handle(Request request)
        {
            var chars = request.Text.ToArray();
            Random.Shared.Shuffle(chars);
            return new Response(new string(chars));
        }
    }
}
// Encontrar, entender o modificar esta feature no toca nada más.
```

---

## El patrón REPR

**REPR tiene tres componentes:**

1. **Request:** DTO de entrada; contiene toda la información que el endpoint necesita para ejecutar. Puede modelarse como `Query` (lectura) o `Command` (escritura) siguiendo CQS.
2. **EndPoint:** la lógica de negocio; es la pieza central que justifica la existencia del endpoint.
3. **Response:** DTO de salida; lo que el endpoint devuelve al cliente.

```
Cliente → [Request] → EndPoint → [Response] → Cliente
                          ↑
                     Lógica de negocio
                     (Validación, acceso a datos, reglas)
```

REPR es especialmente adecuado para **Minimal APIs** de ASP.NET Core porque el modelo de Minimal APIs ya es orientado a endpoints en lugar de a controladores.

---

## Variantes de organización (Ferreira)

### Variante 1 — Handler separado del registro

```csharp
// Feature/ShuffleText.cs — solo la lógica
public class ShuffleText
{
    public record class Request(string Text);
    public record class Response(string Text);

    public class Endpoint
    {
        public Response Handle(Request request)
        {
            var chars = request.Text.ToArray();
            Random.Shared.Shuffle(chars);
            return new Response(new string(chars));
        }
    }
}

// Program.cs — el registro del endpoint está fuera de la clase
builder.Services.AddSingleton<ShuffleText.Endpoint>();
app.MapGet(
    "/shuffle-text/{text}",
    ([AsParameters] ShuffleText.Request query, ShuffleText.Endpoint endpoint)
        => endpoint.Handle(query)
);
```

### Variante 2 — Delegate del endpoint dentro de la feature

```csharp
public class RandomNumber
{
    public record class Request(int Amount, int Min, int Max);
    public record class Response(IEnumerable<int> Numbers);

    public class Handler
    {
        public Response Handle(Request request)
        {
            var result = new int[request.Amount];
            for (var i = 0; i < request.Amount; i++)
                result[i] = Random.Shared.Next(request.Min, request.Max);
            return new Response(result);
        }
    }

    // El delegate está DENTRO de la feature — reduce lógica en Program.cs
    public static Response Endpoint([AsParameters] Request query, Handler handler)
        => handler.Handle(query);
}

// Program.cs — más limpio
builder.Services.AddSingleton<RandomNumber.Handler>();
app.MapGet("/random-number/{Amount}/{Min}/{Max}", RandomNumber.Endpoint);
```

### Variante 3 — Feature completamente autocontenida con extension methods (recomendada)

```csharp
public static class UpperCase
{
    public record class Request(string Text);
    public record class Response(string Text);

    public class Handler
    {
        public Response Handle(Request request)
            => new Response(request.Text.ToUpper());
    }

    // La feature registra sus propias dependencias
    public static IServiceCollection AddUpperCase(this IServiceCollection services)
        => services.AddSingleton<Handler>();

    // La feature mapea su propio endpoint
    public static IEndpointRouteBuilder MapUpperCase(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/upper-case/{Text}",
            ([AsParameters] Request query, Handler handler)
                => handler.Handle(query)
        );
        return endpoints;
    }
}

// Program.cs — completamente limpio
builder.Services.AddUpperCase();
app.MapUpperCase();
```

---

## Proyecto e-commerce con REPR + CQS + FluentValidation + Mapperly

El ejemplo completo de Ferreira usa un stack con dependencias adicionales para un proyecto realista:

```
ASP.NET Core Minimal API        ← backbone
FluentValidation                ← validación
FluentValidation.AspNetCore.Http ← integración con Minimal API
Mapperly                        ← object mapping con source generators
ExceptionMapper                 ← manejo de excepciones → HTTP status codes
EF Core InMemory                ← persistencia
```

### Estructura del proyecto (Vertical Slice)

```
Web/
├── Program.cs                  ← solo bootstrap
└── Features/
    ├── Features.cs             ← AddFeatures(), MapFeatures(), SeedFeaturesAsync()
    ├── Baskets/
    │   ├── Baskets.cs          ← contexto compartido, AddBasketsFeature(), MapBasketsFeature()
    │   ├── Baskets.AddItem.cs  ← Command, Response, Mapper, Validator, Handler, MapAddItem
    │   ├── Baskets.FetchItems.cs
    │   ├── Baskets.RemoveItem.cs
    │   └── Baskets.UpdateQuantity.cs
    └── Products/
        ├── Products.cs
        ├── Products.FetchAll.cs
        └── Products.FetchOne.cs
```

### Anatomía completa de una feature REPR (AddItem)

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Web.Features;

public partial class Baskets
{
    public partial class AddItem
    {
        // 1. REQUEST (Command)
        public record class Command(
            int CustomerId,
            int ProductId,
            int Quantity
        );

        // 2. RESPONSE
        public record class Response(
            int ProductId,
            int Quantity
        );

        // 3. MAPPER (Mapperly — generado en compilación)
        [Mapper]
        public partial class Mapper
        {
            public partial BasketItem Map(Command item);
            public partial Response   Map(BasketItem item);
        }

        // 4. VALIDADOR (FluentValidation)
        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.CustomerId).GreaterThan(0);
                RuleFor(x => x.ProductId).GreaterThan(0);
                RuleFor(x => x.Quantity).GreaterThan(0);
            }
        }

        // 5. HANDLER (lógica de negocio)
        public class Handler
        {
            private readonly BasketContext _db;
            private readonly Mapper _mapper;

            public Handler(BasketContext db, Mapper mapper)
            {
                _db     = db     ?? throw new ArgumentNullException(nameof(db));
                _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            }

            public async Task<Response> HandleAsync(
                Command command, CancellationToken cancellationToken)
            {
                var itemExists = await _db.Items.AnyAsync(
                    x => x.CustomerId == command.CustomerId
                      && x.ProductId  == command.ProductId,
                    cancellationToken: cancellationToken
                );

                if (itemExists)
                    throw new DuplicateBasketItemException(command.ProductId);

                var item   = _mapper.Map(command);
                _db.Add(item);
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(item);
            }
        }
    }

    // 6. REGISTRO DE DEPENDENCIAS
    public static IServiceCollection AddAddItem(this IServiceCollection services)
        => services
            .AddScoped<AddItem.Handler>()
            .AddSingleton<AddItem.Mapper>();

    // 7. MAPEO DEL ENDPOINT
    public static IEndpointRouteBuilder MapAddItem(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/",
            async (AddItem.Command command, AddItem.Handler handler,
                   CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                // 201 Created + Location header hacia el recurso creado
                return TypedResults.Created($"/products/{result.ProductId}", result);
            }
        );
        return endpoints;
    }
}
```

---

## Manejo de excepciones con ExceptionMapper

En REPR + Minimal APIs, el enfoque recomendado es propagar errores mediante excepciones tipadas y capturarlos en un middleware centralizado:

```csharp
// 1. Excepción tipada — hereda del tipo correcto de ExceptionMapper
using ForEvolve.ExceptionMapper;

public class DuplicateBasketItemException : ConflictException
{
    public DuplicateBasketItemException(int productId)
        : base($"El producto '{productId}' ya está en el carrito.")
    { }
}

// 2. Registrar el middleware en Program.cs
builder.AddExceptionMapper();   // registra dependencias
app.UseExceptionMapper();       // agrega el middleware

// 3. Resultado automático al lanzar DuplicateBasketItemException:
// HTTP 409 Conflict
// {
//   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
//   "title": "El producto '3' ya está en el carrito.",
//   "status": 409,
//   "traceId": "..."
// }
```

**Excepciones built-in de ExceptionMapper:**

| Tipo | HTTP Status |
|------|-------------|
| `BadRequestException` | 400 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `InternalServerErrorException` | 500 |
| `ServiceUnavailableException` | 503 |

---

## Flujo del request en REPR (paso a paso)

```
1. Cliente envía POST /baskets  { customerId, productId, quantity }
2. ASP.NET Core deserializa el body → Command
3. FluentValidationEndpointFilter ejecuta Validator<Command>
   → si inválido: 400 Bad Request con detalles de validación
4. El delegate del MapAddItem extrae Handler del IoC y llama HandleAsync(command)
5. Handler consulta la base de datos
   → si duplicado: lanza DuplicateBasketItemException
6. ExceptionMapper captura la excepción → 409 Conflict
7. Si exitoso: Handler retorna Response
8. El delegate retorna TypedResults.Created(location, result) → 201 Created
```

---

## Relación con el back-template

El back-template usa MVC (Controllers + Presenters + Mediator). REPR es una alternativa más directa, especialmente útil cuando se migra hacia Minimal APIs o se adopta Vertical Slice Architecture. Las diferencias clave son:

| Aspecto | Back-template (MVC + Mediator) | REPR (Minimal APIs) |
|---------|-------------------------------|---------------------|
| Organización | Por capa (Controllers, Handlers, Repos) | Por feature (todo junto) |
| Entrada | Request record en el Handler | Command/Query record en el endpoint |
| Salida | IResponse → Presenter → ViewModel → Controller | Response record retornado directamente |
| Error handling | Result Pattern (IFailure) | Excepciones tipadas + ExceptionMapper |
| Testing | Unit tests del Handler aislado | Gray-box integration tests con WebApplicationFactory |
| Extensibilidad | Pipeline behaviors (MediatR) | FluentValidationEndpointFilter + middleware |

**Cuándo migrar a REPR:** cuando el overhead de Mediator + Presenter es injustificado (features simples, prototipos, proyectos pequeños) o cuando se adopta Vertical Slice Architecture.

---

## Cuándo usar

- Al construir REST APIs con Minimal APIs de .NET 6+.
- En proyectos con Vertical Slice Architecture donde cada feature debe ser independiente.
- Cuando la navegabilidad del código importa: buscar la feature "AddItem" lleva directo al código, no a un controller genérico.
- Cuando se quiere separación de lectura/escritura (CQS) sin la complejidad de un Mediator completo.

## Cuándo NO usar

- Cuando el proyecto ya tiene MVC con controllers establecidos y el cambio no aporta valor.
- Cuando las features comparten muchos comportamientos transversales que el MVC pipeline ya maneja (auth, caching, etc. via filters y middleware).
- Cuando el equipo no conoce Minimal APIs y la curva de aprendizaje no está justificada.

---

## Glosario

| Término | Definición |
|---------|-----------|
| REPR | Request-EndPoint-Response — patrón que organiza cada operación de una API como una unidad de tres piezas autocontenida |
| Request | DTO de entrada que contiene los datos que el endpoint necesita para ejecutar; equivale a un Command o Query en CQS |
| EndPoint | La clase o método que contiene la lógica de negocio de la operación; el centro del patrón |
| Response | DTO de salida que el endpoint retorna al cliente; puede ser `null` en operaciones sin datos de vuelta |
| Command | Request que modifica estado (escritura) — convención de CQS |
| Query | Request que lee estado sin modificarlo — convención de CQS |
| Handler | Clase que contiene la lógica de negocio; separada del delegate del endpoint para permitir unit testing |
| FluentValidation | Librería de validación que integra con Minimal APIs vía `FluentValidationEndpointFilter` |
| ExceptionMapper | Middleware que mapea excepciones a HTTP status codes con cuerpo ProblemDetails (RFC 9457) |
| Mapperly | Librería de source generation que genera código de object mapping en tiempo de compilación — muy rápida |
| Vertical Slice | Organización por feature completa (UI → lógica → datos) en lugar de por capa técnica |
| `TypedResults` | API de Minimal APIs que retorna resultados tipados en compilación (`TypedResults.Ok<T>`, `TypedResults.Created<T>`) |
| `[AsParameters]` | Atributo que permite a ASP.NET Core bindear parámetros de ruta/query a una clase record como si fuera un objeto |
| Gray-box testing | Estrategia de testing que combina conocimiento interno (DbContext, IoC) con pruebas HTTP end-to-end usando `WebApplicationFactory` |
| `WebApplicationFactory<TProgram>` | Clase de xUnit que levanta la aplicación real en memoria para integration tests |
| `partial class` | Modificador de C# que permite dividir una clase en múltiples archivos — usado en REPR para separar features del contexto compartido |

---

*Rogelio Arriaga Gonzalez*
