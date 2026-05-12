# 01 — xUnit: Fundamentos y Convenciones

El framework de testing estándar en .NET moderno. Todo test es un método público en una clase pública.

> Fuente: *Clean Code with C# 2nd Ed* (Jason Alls) — Ch.10 Unit Testing with xUnit

---

## El problema que resuelve

Sin tests automatizados, verificar que el código funciona requiere ejecutar la app manualmente y probar cada caso. Los tests hacen esa verificación reproducible, rápida y sin intervención humana.

---

## Estructura básica

```csharp
// Tests/ExampleUsers/GetExampleUserHandlerTests.cs
public sealed class GetExampleUserHandlerTests
{
    [Fact]
    public async Task Handle_UserExists_ReturnsSuccess()
    {
        // Arrange — preparar el escenario
        var repo    = Substitute.For<IExampleUserRepository>();
        var handler = new GetExampleUserHandler(repo);
        var userId  = Guid.NewGuid();

        repo.GetByPublicIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new ExampleUser { PublicId = userId, FullName = "John Doe", Email = "j@test.com" });

        // Act — ejecutar la acción bajo prueba
        var result = await handler.Handle(new GetExampleUserRequest(userId), CancellationToken.None);

        // Assert — verificar el resultado
        result.Should().BeOfType<GetExampleUserSuccess>();
        var success = (GetExampleUserSuccess)result;
        success.Data.FullName.Should().Be("John Doe");
    }
}
```

El patrón **Arrange / Act / Assert** es la estructura universal de un test.

---

## [Fact] vs [Theory]

### [Fact] — un caso concreto

```csharp
[Fact]
public async Task Handle_UserNotFound_ReturnsNotFoundFailure()
{
    var repo    = Substitute.For<IExampleUserRepository>();
    var handler = new GetExampleUserHandler(repo);

    // repo retorna null → usuario no existe
    repo.GetByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
        .Returns((ExampleUser?)null);

    var result = await handler.Handle(
        new GetExampleUserRequest(Guid.NewGuid()), CancellationToken.None);

    result.Should().BeOfType<GetExampleUserNotFoundFailure>();
}
```

### [Theory] — múltiples casos con los mismos pasos

```csharp
[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData(null)]
public void Validator_EmptyFullName_HasValidationError(string? fullName)
{
    var validator = new InsertExampleUserRequestValidator();
    var request   = new InsertExampleUserRequest(fullName!, "test@test.com", "Password1");

    var result = validator.Validate(request);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "FullName");
}

// MemberData — datos más complejos desde un método
public static IEnumerable<object[]> InvalidPasswords()
{
    yield return new object[] { "short",     "Mínimo 8 caracteres." };
    yield return new object[] { "nouppercase1", "Debe contener al menos una mayúscula." };
    yield return new object[] { "NoNumber",  "Debe contener al menos un número." };
}

[Theory]
[MemberData(nameof(InvalidPasswords))]
public void Validator_InvalidPassword_HasExpectedError(string password, string expectedMessage)
{
    var validator = new InsertExampleUserRequestValidator();
    var request   = new InsertExampleUserRequest("John", "j@test.com", password);

    var result = validator.Validate(request);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.ErrorMessage == expectedMessage);
}
```

---

## Assertions con FluentAssertions

Más legibles que `Assert.Equal(expected, actual)`:

```xml
<!-- Tests/Tests.csproj -->
<PackageReference Include="FluentAssertions" Version="6.*" />
```

```csharp
// Tipos
result.Should().BeOfType<GetExampleUserSuccess>();
result.Should().BeAssignableTo<ISuccess>();
result.Should().NotBeNull();
result.Should().BeNull();

// Strings
name.Should().Be("John Doe");
name.Should().StartWith("John");
name.Should().Contain("Doe");
name.Should().NotBeNullOrWhiteSpace();

// Números
count.Should().Be(5);
count.Should().BeGreaterThan(0);
count.Should().BeInRange(1, 10);

// Colecciones
users.Should().HaveCount(3);
users.Should().Contain(u => u.Email == "test@test.com");
users.Should().BeEmpty();
users.Should().NotBeEmpty();
users.Should().AllSatisfy(u => u.IsActive.Should().BeTrue());

// Excepciones
var act = () => handler.Handle(null!, CancellationToken.None);
await act.Should().ThrowAsync<ArgumentNullException>();

// Async
var result = await handler.Handle(request, CancellationToken.None);
result.Should().BeOfType<GetExampleUserSuccess>()
      .Which.Data.Email.Should().Be("test@test.com");  // encadenar con Which
```

---

## Fixtures — setup compartido

### IClassFixture — compartido entre todos los tests de una clase

```csharp
// Tests/Fixtures/DatabaseFixture.cs  (ver 04-testcontainers.md)
public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Arrancar contenedor de PostgreSQL
        // ConnectionString = ...
    }

    public async Task DisposeAsync()
    {
        // Detener contenedor
    }
}

// Test class que usa la fixture
public sealed class ExampleUserRepositoryTests
    : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _db;

    public ExampleUserRepositoryTests(DatabaseFixture db) => _db = db;

    [Fact]
    public async Task GetByPublicIdAsync_ExistingUser_ReturnsUser()
    {
        var connString = _db.ConnectionString;
        // ...
    }
}
```

La fixture se crea una vez para toda la clase — ideal para recursos costosos (contenedores Docker).

### IAsyncLifetime — setup/teardown async

```csharp
public sealed class GetExampleUserHandlerTests : IAsyncLifetime
{
    private IExampleUserRepository _repo = null!;
    private GetExampleUserHandler  _handler = null!;

    public Task InitializeAsync()
    {
        _repo    = Substitute.For<IExampleUserRepository>();
        _handler = new GetExampleUserHandler(_repo);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Handle_UserExists_ReturnsSuccess()
    {
        // _handler ya está listo
    }
}
```

---

## Organización de tests

```
Tests/
├── Unit/
│   ├── ExampleUsers/
│   │   ├── GetExampleUserHandlerTests.cs
│   │   ├── InsertExampleUserHandlerTests.cs
│   │   └── InsertExampleUserValidatorTests.cs
│   └── Presenters/
│       └── GetExampleUserPresenterTests.cs
├── Integration/
│   ├── ExampleUsers/
│   │   └── ExampleUsersApiTests.cs
│   └── Repositories/
│       └── ExampleUserRepositoryTests.cs
└── Fixtures/
    ├── DatabaseFixture.cs
    └── WebAppFixture.cs
```

### Nombres de tests — convención

```
{Metodo}_{Escenario}_{ResultadoEsperado}

Handle_UserNotFound_ReturnsNotFoundFailure      ✓
Handle_ValidRequest_CallsRepositoryOnce         ✓
Validate_EmptyEmail_ReturnsValidationError      ✓

TestLogin()                                     ✗ — no describe qué ni resultado
```

---

## Ejecutar tests

```bash
# Todos los tests
dotnet test

# Con detalle
dotnet test --verbosity normal

# Solo una clase
dotnet test --filter "FullyQualifiedName~GetExampleUserHandlerTests"

# Solo los que contienen una palabra
dotnet test --filter "DisplayName~Handle_User"

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

---

## Relación con back-template

`back-template/Tests/Tests.csproj` ya tiene xUnit configurado. Los tests de Handlers, Presenters y repositorios siguen el patrón de este documento. `back-template/docs/Testing.md` cubre la configuración específica del proyecto.


---

*Rogelio Arriaga Gonzalez*
