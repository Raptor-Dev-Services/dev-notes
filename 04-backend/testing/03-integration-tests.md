# 03 — Integration Tests: WebApplicationFactory y TestServer

Tests que levantan toda la API en memoria y hacen requests HTTP reales contra ella, sin necesidad de un servidor externo.

---

## El problema que resuelve

Los unit tests verifican cada pieza en aislamiento. Los integration tests verifican que todas las piezas funcionan juntas:

```
Unit test:     Handler + mock de repo → verifica lógica del handler
Integration:   HTTP GET /api/example/users/{id} → verifica todo el stack
                  Controller → Mediator → Handler → Repo → DB (real) → Presenter → Response
```

Si el Controller tiene mal el route, si el DI no registró algo, si la serialización JSON falla — los unit tests no lo detectan. Los integration tests sí.

---

## WebApplicationFactory

ASP.NET Core provee `WebApplicationFactory<T>` que levanta el programa de la app en memoria para tests:

```xml
<!-- Tests/Tests.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
```

```csharp
// Tests/Fixtures/WebAppFixture.cs
public sealed class WebAppFixture
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // La DB se inyecta desde una fixture de TestContainers (ver 04-testcontainers.md)
    // O se puede sobreescribir con InMemory para tests sin DB

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Sobreescribir servicios externos con fakes
            // Por ejemplo, reemplazar el servicio de email
            services.AddScoped<IEmailService, FakeEmailService>();

            // Sobreescribir la conexión a DB con TestContainers (ver 04)
            // services.RemoveAll<DbConnection>();
            // services.AddScoped<...>(...);
        });

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            // Inyectar configuración de test
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]     = "test-jwt-key-min-32-characters-long!!",
                ["Jwt:Issuer"]  = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();
}
```

**Nota:** `Program.cs` necesita ser accesible como punto de entrada. En .NET 10 top-level statements, agregar al final:

```csharp
// Host/Program.cs — al final del archivo
public partial class Program { }  // expone Program para WebApplicationFactory
```

---

## Escribir tests de integración

```csharp
// Tests/Integration/ExampleUsers/ExampleUsersApiTests.cs
public sealed class ExampleUsersApiTests
    : IClassFixture<WebAppFixture>
{
    private readonly HttpClient _client;

    public ExampleUsersApiTests(WebAppFixture factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_ExistingUser_Returns200WithUser()
    {
        // Arrange — obtener un ID que existe (semilla en DB de test)
        var existingId = SeedData.ExampleUserId;

        // Act
        var response = await _client.GetAsync($"/api/example/users/{existingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ResultViewModel<ExampleUserDto>>();
        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeTrue();
        body.Data!.FullName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_NonExistentUser_Returns500WithFailure()
    {
        var response = await _client.GetAsync($"/api/example/users/{Guid.NewGuid()}");

        // En el flujo actual del proyecto: 500 con IsSuccess = false
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var body = await response.Content.ReadFromJsonAsync<ResultViewModel<object>>();
        body!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_NoAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/example/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

## Autenticación en integration tests

Los endpoints con `[Authorize]` requieren un token JWT válido:

```csharp
// Tests/Helpers/JwtTestHelper.cs
public static class JwtTestHelper
{
    private const string TestKey     = "test-jwt-key-min-32-characters-long!!";
    private const string TestIssuer  = "test-issuer";
    private const string TestAudience = "test-audience";

    public static string GenerateToken(
        Guid   userId = default,
        string email  = "test@test.com",
        string role   = "user")
    {
        if (userId == default) userId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             TestIssuer,
            audience:           TestAudience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Uso en tests
var token  = JwtTestHelper.GenerateToken(role: "admin");
_client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var response = await _client.GetAsync($"/api/example/users/{userId}");
```

---

## Limpiar estado entre tests

Cada test debe empezar desde un estado conocido. Opciones:

### Opción A — transacciones que se revierten

```csharp
// Cada test corre en una transacción que se revierte al finalizar
public sealed class ExampleUsersApiTests
    : IClassFixture<WebAppFixture>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly IDbConnection _conn;

    public ExampleUsersApiTests(WebAppFixture factory)
    {
        _scope  = factory.Services.CreateScope();
        _conn   = _scope.ServiceProvider.GetRequiredService<IDbConnection>();
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _conn.OpenAsync();
        // Comenzar transacción — los cambios del test no se persisten
    }

    public async Task DisposeAsync()
    {
        // Revertir transacción
        _scope.Dispose();
    }
}
```

### Opción B — DB limpia por test (TestContainers)

TestContainers levanta un contenedor PostgreSQL limpio para cada clase de test (ver `04-testcontainers.md`). Más aislado, más lento.

### Opción C — datos de semilla idempotentes

```csharp
// Tests/Helpers/SeedData.cs
public static class SeedData
{
    public static readonly Guid ExampleUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(IDbConnection conn)
    {
        await conn.ExecuteAsync("""
            INSERT INTO dbo.ExampleUsers (PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@PublicId, @FullName, @Email, true, now(), now())
            ON CONFLICT (PublicId) DO NOTHING;
            """,
            new { PublicId = ExampleUserId, FullName = "Seed User", Email = "seed@test.com" });
    }
}
```

---

## Test de creación completo (POST)

```csharp
[Fact]
public async Task Insert_ValidBody_Returns200AndCreatesUser()
{
    // Arrange
    var token = JwtTestHelper.GenerateToken();
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var body = new InsertExampleUserBody
    {
        FullName = "New User",
        Email    = $"new-{Guid.NewGuid()}@test.com",   // email único por test
        Password = "NewPassword1"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/example/users", body);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var result = await response.Content
        .ReadFromJsonAsync<ResultViewModel<ExampleUserDto>>();
    result!.IsSuccess.Should().BeTrue();
    result.Data!.FullName.Should().Be("New User");
}
```

---

## Relación con back-template

`back-template/docs/Testing.md` documenta la configuración específica del proyecto. `WebApplicationFactory<Program>` requiere que `Program` sea `public partial class` — la plantilla ya lo tiene configurado.

Los integration tests son más lentos que los unit tests. La convención habitual es separar los suites con categorías y correr solo los unit tests en el loop de desarrollo, los integration tests en CI.

---

## Cuándo usar / Cuándo no usar

| Escenario | Decisión |
|-----------|----------|
| Verificar que el DI está configurado correctamente | ✓ Integration test |
| Verificar routing y serialización JSON | ✓ Integration test |
| Verificar auth/authorize | ✓ Integration test |
| Verificar flujo completo Controller → DB | ✓ Integration test con TestContainers |
| Lógica de negocio del Handler | ✓ Unit test (más rápido) |
| Reglas de validación | ✓ Unit test del Validator |
| Cada combinación de input/output | ✓ Unit test (barato de multiplicar) |

---

## Organización de tests y convenciones de nombres
> Fuente: *Tools and Skills for .NET 8* — Ch.5 Integration Testing Strategies

```
Tests/
├── Unit/
│   ├── ExampleUsers/
│   │   ├── GetExampleUserHandlerTests.cs
│   │   ├── InsertExampleUserHandlerTests.cs
│   │   └── ExampleUserDomainTests.cs
│   └── Validators/
│       └── InsertExampleUserValidatorTests.cs
├── Integration/
│   ├── ExampleUsers/
│   │   ├── GetExampleUsersTests.cs
│   │   └── InsertExampleUserTests.cs
│   └── Auth/
│       └── LoginTests.cs
├── Fixtures/
│   ├── WebAppFixture.cs
│   └── PostgresFixture.cs
└── Helpers/
    ├── JwtTestHelper.cs
    ├── SeedData.cs
    └── ExampleUserBuilder.cs
```

### Convención de nombre para tests

```
[Método]_[Escenario]_[ResultadoEsperado]

GetById_UsuarioExiste_Retorna200
Insert_EmailDuplicado_Retorna409
Login_PasswordIncorrecta_Retorna401
Deactivate_UsuarioYaInactivo_LanzaInvalidOperationException
```

---

## Marcar tests por categoría (Trait)

```csharp
// Clasificar tests para ejecutar subsets en CI
[Trait("Category", "Unit")]
public sealed class GetExampleUserHandlerTests { ... }

[Trait("Category", "Integration")]
public sealed class ExampleUsersApiTests { ... }

[Trait("Category", "Slow")]
public sealed class ExampleUsersTestContainersTests { ... }
```

```bash
# Solo unit tests (rápidos, en el loop de desarrollo)
dotnet test --filter "Category=Unit"

# Solo integration (en CI antes de merge)
dotnet test --filter "Category=Integration"

# Todo excepto slow (TestContainers)
dotnet test --filter "Category!=Slow"
```

---

## Snapshot testing para respuestas JSON

Cuando la respuesta JSON es grande y quieres asegurarte de que no cambia inadvertidamente.

```csharp
// Instalar: dotnet add package Verify.Xunit
[Fact]
public async Task GetById_ExistingUser_MatchesSnapshot()
{
    var response = await _client.GetAsync($"/api/example/users/{SeedData.ExampleUserId}");
    var body     = await response.Content.ReadAsStringAsync();

    // Compara contra el snapshot guardado en Tests/Snapshots/
    // Primera ejecución: crea el snapshot. Siguientes: compara contra él.
    await Verify(body).UseDirectory("Snapshots");
}
```


---

*Rogelio Arriaga Gonzalez*
