# 18 — Atributos en C#

Los atributos agregan metadatos a clases, métodos, propiedades o parámetros. En tiempo de ejecución, el framework (ASP.NET Core, xUnit, Swagger, etc.) lee esos metadatos con reflexión y actúa en consecuencia.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.5 Building Your Own Types: Attributes and Reflection

---

## ¿Qué es un atributo?

Un atributo es una clase que hereda de `System.Attribute`. Se aplica con la sintaxis `[NombreAtributo]` encima del elemento decorado:

```csharp
// [Authorize] ES una clase: public class AuthorizeAttribute : Attribute { ... }
// Al escribir [Authorize], estás instanciando AuthorizeAttribute implícitamente

[Route("api/users")]          // RouteAttribute
[Authorize]                   // AuthorizeAttribute
public sealed class UsersController : BaseApiController
{
    [HttpGet("{id:guid}")]    // HttpGetAttribute
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct = default)
    {
        // ...
    }

    [HttpPost]
    public async Task<IActionResult> Insert(
        [FromBody] InsertUserBody body,   // FromBodyAttribute
        CancellationToken ct = default)
    {
        // ...
    }
}
```

---

## Atributos de ASP.NET Core — los más usados

### Routing y HTTP

```csharp
[Route("api/[controller]")]    // ruta base con token [controller] → "api/users"
[Route("api/v1/users")]        // ruta base literal

// Métodos HTTP
[HttpGet]                      // GET /ruta-base
[HttpGet("{id:guid}")]         // GET /ruta-base/{id} con constraint de tipo Guid
[HttpGet("{id:int:min(1)}")]   // GET con constraint de tipo y valor mínimo
[HttpPost]
[HttpPut("{id:guid}")]
[HttpPatch("{id:guid}")]
[HttpDelete("{id:guid}")]

// Route constraints comunes:
// {id:guid}       → solo acepta GUIDs válidos
// {id:int}        → solo acepta enteros
// {id:int:min(1)} → entero mayor que 0
// {slug:alpha}    → solo letras
// {page:int:range(1,100)} → entero entre 1 y 100
```

### Binding de parámetros

```csharp
// ASP.NET Core decide de dónde leer cada parámetro según el tipo.
// Con atributos puedes ser explícito:

[HttpPost]
public IActionResult Create(
    [FromBody]   CreateUserRequest body,       // del cuerpo JSON
    [FromRoute]  Guid id,                      // del segmento de ruta /usuarios/{id}
    [FromQuery]  int page = 1,                 // de ?page=2
    [FromQuery]  string? search = null,        // de ?search=texto
    [FromHeader] string? authorization = null, // del header Authorization
    [FromForm]   IFormFile? file = null)        // de multipart/form-data
{
    // ...
}
```

### Autorización

```csharp
[Authorize]                          // requiere cualquier usuario autenticado
[Authorize(Roles = "admin")]         // requiere rol "admin"
[Authorize(Roles = "admin,manager")] // requiere rol "admin" O "manager"
[Authorize(Policy = "MinimumAge18")] // requiere una policy personalizada
[AllowAnonymous]                     // permite acceso sin autenticación (sobreescribe [Authorize] del controller)

// En este proyecto — a nivel de controller:
[Authorize]
public sealed class ExampleUsersController : BaseApiController
{
    [AllowAnonymous]  // este endpoint es público aunque el controller requiera auth
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginBody body, CancellationToken ct)
    { /* ... */ }

    [HttpGet("{id:guid}")]  // heredará [Authorize] del controller
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    { /* ... */ }
}
```

### Documentación en Swagger

```csharp
[ProducesResponseType(typeof(ResultViewModel<ExampleUsersController>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{ /* ... */ }
```

### Validación con DataAnnotations

```csharp
public class InsertUserBody
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "El email no tiene formato válido.")]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; init; } = string.Empty;

    [Range(0, 150, ErrorMessage = "La edad debe estar entre 0 y 150.")]
    public int? Age { get; init; }

    [RegularExpression(@"^\+?[1-9]\d{7,14}$", ErrorMessage = "Teléfono inválido.")]
    public string? Phone { get; init; }
}
// ASP.NET Core valida automáticamente si el modelo implementa [ApiController]
```

---

## Atributos de xUnit (Tests)

```csharp
public class ExampleUserHandlerTests
{
    [Fact]           // test simple — sin parámetros
    public async Task Handle_UserExists_ReturnsSuccess()
    {
        // Arrange, Act, Assert
    }

    [Theory]         // test parametrizado — con múltiples casos
    [InlineData("", "email@test.com")]     // caso 1: nombre vacío
    [InlineData("Juan", "")]               // caso 2: email vacío
    [InlineData("Juan", "not-an-email")]   // caso 3: email inválido
    public async Task Handle_InvalidInput_ReturnsValidationFailure(
        string fullName, string email)
    {
        // se ejecuta una vez por cada [InlineData]
    }

    [Fact]
    [Trait("Category", "Integration")]  // categorizar tests para filtrar en CI
    public async Task FullFlow_InsertAndGet_WorksCorrectly() { }
}
```

---

## Atributos de serialización JSON

```csharp
using System.Text.Json.Serialization;

public class UserDto
{
    [JsonPropertyName("user_id")]        // serializa como "user_id" en lugar de "UserId"
    public Guid UserId { get; init; }

    [JsonIgnore]                         // excluir del JSON
    public string PasswordHash { get; init; } = string.Empty;

    [JsonPropertyOrder(1)]               // controla el orden en el JSON
    public string FullName { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; init; }  // serializa como "Admin" no como 10

    [JsonInclude]                        // incluir un campo (field, no property)
    public string InternalCode;
}
```

---

## Múltiples atributos

```csharp
// Pueden apilarse múltiples atributos — se ejecutan independientemente
[HttpGet("{id:guid}")]
[Authorize(Roles = "admin,user")]
[ResponseCache(Duration = 60)]
[ProducesResponseType(200)]
[ProducesResponseType(401)]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct) { }

// Forma compacta (mismo atributo):
[Authorize(Roles = "admin"), Authorize(Policy = "MFA")]
// ↑ equivale a dos [Authorize] separados
```

---

## Crear atributos personalizados

```csharp
// Un atributo es una clase que hereda de Attribute
// El sufijo "Attribute" es convencional — al usarlo se omite: [ApiKey]
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Class,  // dónde puede aplicarse
    AllowMultiple = false,                              // no repetir en el mismo target
    Inherited = true)]                                  // se hereda por subclases
public sealed class ApiKeyAttribute : Attribute
{
    public string KeyName { get; }

    public ApiKeyAttribute(string keyName = "X-Api-Key")
    {
        KeyName = keyName;
    }
}

// Uso:
[ApiKey("X-Custom-Key")]
[HttpGet("internal-data")]
public IActionResult GetInternalData() { }
```

### Atributo de validación personalizado

```csharp
public sealed class NoSqlInjectionAttribute : ValidationAttribute
{
    private static readonly string[] _forbidden = { "--", ";", "DROP", "SELECT", "INSERT" };

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is string s)
        {
            foreach (var token in _forbidden)
            {
                if (s.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return new ValidationResult($"El campo '{ctx.DisplayName}' contiene caracteres no permitidos.");
            }
        }
        return ValidationResult.Success;
    }
}

// Uso:
public class SearchRequest
{
    [NoSqlInjection]
    [MaxLength(200)]
    public string Query { get; init; } = string.Empty;
}
```

---

## Leer atributos con reflexión

```csharp
// Leer atributos en runtime:
var method    = typeof(UsersController).GetMethod("GetById");
var httpGet   = method?.GetCustomAttribute<HttpGetAttribute>();
var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();

if (authorize != null)
    Console.WriteLine($"Requiere rol: {authorize.Roles}");

// En una clase:
var type       = typeof(UsersController);
var routeAttr  = type.GetCustomAttribute<RouteAttribute>();
Console.WriteLine($"Ruta base: {routeAttr?.Template}");

// Verificar si un tipo tiene un atributo:
bool hasAuth = typeof(UsersController).IsDefined(typeof(AuthorizeAttribute), inherit: true);
```

---

## Atributos en este proyecto

```csharp
// Controller — nivel de clase
[Route("api/example/users")]      // ruta base fija
[Authorize]                        // toda la clase requiere auth
public sealed class ExampleUsersController : BaseApiController
{
    // Endpoint GET — nivel de método
    [HttpGet("{id:guid}")]         // GET api/example/users/{guid}
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    { /* ... */ }

    // Endpoint POST
    [HttpPost]                     // POST api/example/users
    public async Task<IActionResult> Insert(
        [FromBody] InsertExampleUserBody body,   // JSON del cuerpo
        CancellationToken ct = default)
    { /* ... */ }

    // Endpoint GET con query params
    [HttpGet]                      // GET api/example/users?page=1&pageSize=10
    public async Task<IActionResult> GetAll(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct     = default)
    { /* ... */ }
}
```

---

## Tabla de atributos frecuentes

| Atributo | Namespace | Para qué |
|---------|-----------|---------|
| `[Authorize]` | Microsoft.AspNetCore.Authorization | Requiere autenticación |
| `[AllowAnonymous]` | Microsoft.AspNetCore.Authorization | Permite acceso sin auth |
| `[HttpGet/Post/Put/Delete/Patch]` | Microsoft.AspNetCore.Mvc | Método HTTP del endpoint |
| `[Route]` | Microsoft.AspNetCore.Mvc | Define la ruta |
| `[FromBody]` | Microsoft.AspNetCore.Mvc | Leer del cuerpo JSON |
| `[FromQuery]` | Microsoft.AspNetCore.Mvc | Leer de query string |
| `[FromRoute]` | Microsoft.AspNetCore.Mvc | Leer del segmento de ruta |
| `[ApiController]` | Microsoft.AspNetCore.Mvc | Activa validación automática, binding inferido |
| `[Required]` | System.ComponentModel.DataAnnotations | Campo obligatorio |
| `[MaxLength]` | System.ComponentModel.DataAnnotations | Longitud máxima |
| `[EmailAddress]` | System.ComponentModel.DataAnnotations | Validar formato email |
| `[JsonIgnore]` | System.Text.Json.Serialization | Excluir del JSON |
| `[JsonPropertyName]` | System.Text.Json.Serialization | Nombre en el JSON |
| `[Fact]` / `[Theory]` | Xunit | Marcar tests |
| `[InlineData]` | Xunit | Datos para Theory |


---

*Rogelio Arriaga Gonzalez*
