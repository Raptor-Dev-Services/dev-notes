# 02 — Unit Tests: Handlers y Presenters con NSubstitute

Tests de unidad aislados: se prueba una sola clase, con todos sus colaboradores sustituidos por fakes.

---

## El problema que resuelve

Los Handlers dependen de repositorios, servicios de token, servicios de correo. Para testear el Handler en aislamiento, esas dependencias deben ser reemplazadas por objetos que podemos controlar:

```csharp
// ❌ Sin mock — el Handler golpea la DB real en el test
public sealed class GetExampleUserHandlerTests
{
    [Fact]
    public async Task Handle_UserExists_ReturnsSuccess()
    {
        var handler = new GetExampleUserHandler(new ExampleUserRepository(/* conexión real */));
        // El test depende de que haya datos en la DB, de la red, del estado previo
    }
}

// ✓ Con mock — el Handler recibe un repositorio falso que devuelve lo que el test necesita
var repo    = Substitute.For<IExampleUserRepository>();
var handler = new GetExampleUserHandler(repo);
repo.GetByPublicIdAsync(userId, Arg.Any<CancellationToken>()).Returns(fakeUser);
```

---

## NSubstitute — sintaxis esencial

```xml
<!-- Tests/Tests.csproj -->
<PackageReference Include="NSubstitute" Version="5.*" />
```

### Crear un sustituto

```csharp
var repo         = Substitute.For<IExampleUserRepository>();
var jwtService   = Substitute.For<IJwtTokenService>();
var currentUser  = Substitute.For<ICurrentUserService>();
var logger       = Substitute.For<ILogger<GetExampleUserHandler>>();
```

### Configurar retornos

```csharp
// Retorno simple
repo.GetByPublicIdAsync(userId, Arg.Any<CancellationToken>())
    .Returns(new ExampleUser { PublicId = userId, FullName = "John" });

// Retorno null (usuario no existe)
repo.GetByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
    .Returns((ExampleUser?)null);

// Retorno async con Task
repo.InsertAsync(Arg.Any<ExampleUser>(), Arg.Any<CancellationToken>())
    .Returns(Task.CompletedTask);

// Lanzar excepción
repo.GetByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
    .Throws(new InvalidOperationException("DB error"));

// Retorno condicional según argumento
repo.GetByPublicIdAsync(Arg.Is<Guid>(id => id == knownId), Arg.Any<CancellationToken>())
    .Returns(knownUser);
repo.GetByPublicIdAsync(Arg.Is<Guid>(id => id != knownId), Arg.Any<CancellationToken>())
    .Returns((ExampleUser?)null);
```

### Verificar llamadas

```csharp
// Verificar que se llamó exactamente una vez
await repo.Received(1).InsertAsync(
    Arg.Is<ExampleUser>(u => u.Email == "test@test.com"),
    Arg.Any<CancellationToken>());

// Verificar que NO se llamó
await repo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

// Verificar que se llamó al menos una vez
await repo.Received().GetByPublicIdAsync(userId, Arg.Any<CancellationToken>());
```

---

## Tests de Handlers

### Handler de lectura (Query)

```csharp
public sealed class GetExampleUserHandlerTests
{
    private readonly IExampleUserRepository _repo;
    private readonly GetExampleUserHandler  _handler;

    public GetExampleUserHandlerTests()
    {
        _repo    = Substitute.For<IExampleUserRepository>();
        _handler = new GetExampleUserHandler(_repo);
    }

    [Fact]
    public async Task Handle_UserExists_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user   = new ExampleUser
        {
            PublicId = userId,
            FullName = "John Doe",
            Email    = "john@test.com",
            IsActive = true
        };
        _repo.GetByPublicIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(new GetExampleUserRequest(userId), CancellationToken.None);

        // Assert
        result.Should().BeOfType<GetExampleUserSuccess>();
        var success = (GetExampleUserSuccess)result;
        success.Data.FullName.Should().Be("John Doe");
        success.Data.Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        _repo.GetByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((ExampleUser?)null);

        // Act
        var result = await _handler.Handle(
            new GetExampleUserRequest(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().BeOfType<GetExampleUserNotFoundFailure>();
        var failure = (GetExampleUserNotFoundFailure)result;
        failure.Message.Should().NotBeNullOrWhiteSpace();
    }
}
```

### Handler de escritura (Command)

```csharp
public sealed class InsertExampleUserHandlerTests
{
    private readonly IExampleUserRepository _repo;
    private readonly ICurrentUserService    _currentUser;
    private readonly InsertExampleUserHandler _handler;

    public InsertExampleUserHandlerTests()
    {
        _repo        = Substitute.For<IExampleUserRepository>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _handler     = new InsertExampleUserHandler(_repo, _currentUser);

        // Setup por defecto — usuario autenticado
        _currentUser.UserId.Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_NewEmail_InsertsUserAndReturnsSuccess()
    {
        // Arrange
        _repo.EmailExistsAsync("new@test.com", Arg.Any<CancellationToken>())
             .Returns(false);
        _repo.InsertAsync(Arg.Any<ExampleUser>(), Arg.Any<CancellationToken>())
             .Returns(Task.CompletedTask);

        var request = new InsertExampleUserRequest("Jane Doe", "new@test.com", "Password1");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<InsertExampleUserSuccess>();

        // Verificar que se llamó InsertAsync con los datos correctos
        await _repo.Received(1).InsertAsync(
            Arg.Is<ExampleUser>(u =>
                u.FullName == "Jane Doe" &&
                u.Email    == "new@test.com" &&
                u.IsActive == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictFailure()
    {
        // Arrange
        _repo.EmailExistsAsync("existing@test.com", Arg.Any<CancellationToken>())
             .Returns(true);

        var request = new InsertExampleUserRequest("John", "existing@test.com", "Password1");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<InsertExampleUserConflictFailure>();

        // NO debe haber llamado a InsertAsync
        await _repo.DidNotReceive().InsertAsync(
            Arg.Any<ExampleUser>(), Arg.Any<CancellationToken>());
    }
}
```

### Handler de autenticación (Login)

```csharp
public sealed class LoginHandlerTests
{
    private readonly IExampleUserRepository _repo;
    private readonly IJwtTokenService       _jwt;
    private readonly LoginHandler           _handler;

    public LoginHandlerTests()
    {
        _repo    = Substitute.For<IExampleUserRepository>();
        _jwt     = Substitute.For<IJwtTokenService>();
        _handler = new LoginHandler(_repo, _jwt);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        // Arrange — usuario con password hasheada real
        var password = "TestPassword1";
        var hash     = BCrypt.Net.BCrypt.HashPassword(password);
        var user     = new ExampleUser
        {
            PublicId      = Guid.NewGuid(),
            Email         = "user@test.com",
            PasswordHash  = hash,
            IsActive      = true
        };

        _repo.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _jwt.GenerateToken(user.PublicId, user.Email).Returns("fake.jwt.token");

        // Act
        var result = await _handler.Handle(
            new LoginRequest("user@test.com", password), CancellationToken.None);

        // Assert
        result.Should().BeOfType<LoginSuccess>();
        ((LoginSuccess)result).Token.Should().Be("fake.jwt.token");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsUnauthorizedFailure()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var user = new ExampleUser { Email = "u@test.com", PasswordHash = hash, IsActive = true };

        _repo.GetByEmailAsync("u@test.com", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(
            new LoginRequest("u@test.com", "wrongpassword"), CancellationToken.None);

        result.Should().BeOfType<LoginUnauthorizedFailure>();
        _jwt.DidNotReceive().GenerateToken(Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsForbiddenFailure()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Password1");
        var user = new ExampleUser
        {
            Email        = "u@test.com",
            PasswordHash = hash,
            IsActive     = false   // ← cuenta inactiva
        };

        _repo.GetByEmailAsync("u@test.com", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(
            new LoginRequest("u@test.com", "Password1"), CancellationToken.None);

        result.Should().BeOfType<LoginForbiddenFailure>();
    }
}
```

---

## Tests de Presenters

```csharp
public sealed class GetExampleUserPresenterTests
{
    private readonly ResultViewModel<ExampleUsersController> _viewModel;
    private readonly GetExampleUserPresenter                 _presenter;

    public GetExampleUserPresenterTests()
    {
        _viewModel = new ResultViewModel<ExampleUsersController>();
        _presenter = new GetExampleUserPresenter(_viewModel);
    }

    [Fact]
    public async Task Handle_SuccessResponse_SetsViewModelSuccess()
    {
        var dto      = new ExampleUserDto(Guid.NewGuid(), "John", "j@test.com", true);
        var response = new GetExampleUserSuccess(dto);

        await _presenter.Handle(response, CancellationToken.None);

        _viewModel.IsSuccess.Should().BeTrue();
        _viewModel.Data.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Handle_NotFoundFailure_SetsViewModelFailed()
    {
        var response = new GetExampleUserNotFoundFailure("Usuario no encontrado.");

        await _presenter.Handle(response, CancellationToken.None);

        _viewModel.IsSuccess.Should().BeFalse();
        _viewModel.Message.Should().Contain("no encontrado");
    }
}
```

---

## Tests de Validators

```csharp
public sealed class InsertExampleUserValidatorTests
{
    private readonly InsertExampleUserRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var request = new InsertExampleUserRequest("John Doe", "j@test.com", "Password1");
        var result  = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "FullName")]
    [InlineData("   ", "FullName")]
    public void Validate_EmptyName_HasNameError(string name, string field)
    {
        var request = new InsertExampleUserRequest(name, "j@test.com", "Password1");
        var result  = _validator.Validate(request);
        result.Errors.Should().Contain(e => e.PropertyName == field);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@domain.com")]
    public void Validate_InvalidEmail_HasEmailError(string email)
    {
        var request = new InsertExampleUserRequest("John", email, "Password1");
        var result  = _validator.Validate(request);
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
```

---

## Cuándo usar / Cuándo no usar

| Escenario | Decisión |
|-----------|----------|
| Lógica de Handler (flujos, condiciones) | ✓ Unit test con NSubstitute |
| Validadores | ✓ Unit test — rápidos, sin dependencias |
| Presenters | ✓ Unit test — deterministas |
| SQL / persistencia real | ✗ No unit test — ver 04-testcontainers.md |
| Controllers | ✗ Unit test mínimo — cubiertos por integration tests |
| Código puramente de mappeo/transformación | ✓ Unit test con datos concretos |

---

## Parametrización y teorías
> Fuente: *Tools and Skills for .NET 8* — Ch.4 Unit Testing Patterns

xUnit ofrece `[Theory]` + `[InlineData]` para evitar duplicar tests con variaciones de datos.

```csharp
public sealed class EmailValidationTests
{
    [Theory]
    [InlineData("usuario@ejemplo.com",   true)]
    [InlineData("admin@empresa.co.mx",   true)]
    [InlineData("user+tag@gmail.com",    true)]
    [InlineData("sindominio",            false)]
    [InlineData("@sinusuario.com",       false)]
    [InlineData("",                      false)]
    [InlineData(null,                    false)]
    public void EmailValido_ReturnsExpected(string? email, bool expected)
    {
        var result = EmailValidator.IsValid(email);
        result.Should().Be(expected);
    }
}

// MemberData para casos más complejos
public static IEnumerable<object[]> InvalidRequestCases() =>
[
    [new InsertExampleUserRequest("",     "a@b.com", "Pass1"), "FullName"],
    [new InsertExampleUserRequest("Juan", "",         "Pass1"), "Email"],
    [new InsertExampleUserRequest("Juan", "a@b.com", ""),       "Password"],
];

[Theory]
[MemberData(nameof(InvalidRequestCases))]
public void Validate_InvalidRequest_HasExpectedError(
    InsertExampleUserRequest request, string expectedField)
{
    var validator = new InsertExampleUserRequestValidator();
    var result    = validator.Validate(request);
    result.Errors.Should().Contain(e => e.PropertyName == expectedField);
}
```

---

## Builder pattern para objetos de prueba

En lugar de duplicar la construcción de entidades en cada test, se usa un builder.

```csharp
// Tests/Builders/ExampleUserBuilder.cs
public sealed class ExampleUserBuilder
{
    private Guid     _publicId   = Guid.NewGuid();
    private string   _fullName   = "Usuario de Prueba";
    private string   _email      = "prueba@test.com";
    private bool     _isActive   = true;
    private DateTime _createdAt  = DateTime.UtcNow;

    public ExampleUserBuilder WithPublicId(Guid id)    { _publicId  = id;      return this; }
    public ExampleUserBuilder WithFullName(string name){ _fullName   = name;    return this; }
    public ExampleUserBuilder WithEmail(string email)  { _email      = email;   return this; }
    public ExampleUserBuilder Inactive()               { _isActive   = false;   return this; }

    public ExampleUser Build() => new ExampleUser
    {
        PublicId     = _publicId,
        FullName     = _fullName,
        Email        = _email,
        IsActive     = _isActive,
        CreatedAtUtc = _createdAt
    };
}

// Uso en tests — legible y sin repetición
[Fact]
public async Task Handle_InactiveUser_ReturnsForbiddenFailure()
{
    var user = new ExampleUserBuilder()
        .WithEmail("bloqueado@test.com")
        .Inactive()
        .Build();

    _repo.GetByEmailAsync("bloqueado@test.com", Arg.Any<CancellationToken>()).Returns(user);

    var result = await _handler.Handle(
        new LoginRequest("bloqueado@test.com", "Password1"), CancellationToken.None);

    result.Should().BeOfType<LoginForbiddenFailure>();
}
```

---

## Tests de entidades de dominio (invariantes)

Los métodos de negocio de las entidades también se testean como unit tests. No necesitan mocks.

```csharp
public sealed class ExampleUserDomainTests
{
    [Fact]
    public void Create_ValidData_CreatesActiveUser()
    {
        var user = ExampleUser.Create("María García", "maria@test.com");

        user.FullName.Should().Be("María García");
        user.Email.Should().Be("maria@test.com");
        user.IsActive.Should().BeTrue();
        user.PublicId.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        var act = () => ExampleUser.Create("", "maria@test.com");
        act.Should().Throw<ArgumentException>().WithMessage("*nombre*");
    }

    [Fact]
    public void Deactivate_ActiveUser_SetsIsActiveFalse()
    {
        var user = ExampleUser.Create("María García", "maria@test.com");
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactiveUser_ThrowsInvalidOperationException()
    {
        var user = ExampleUser.Create("María García", "maria@test.com");
        user.Deactivate();

        var act = () => user.Deactivate();  // segunda desactivación
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateEmail_ValidEmail_UpdatesEmail()
    {
        var user = ExampleUser.Create("María García", "maria@test.com");
        user.UpdateEmail("nueva@test.com");
        user.Email.Should().Be("nueva@test.com");
    }
}
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Test unitario | Test que verifica una sola clase en aislamiento total, sin dependencias externas reales |
| Mock / Sustituto | Objeto falso que reemplaza una dependencia real y permite controlar su comportamiento en tests |
| NSubstitute | Librería de mocking para .NET basada en proxies que genera sustitutos de interfaces y clases abstractas |
| `Substitute.For<T>()` | Método de NSubstitute que crea un sustituto de la interfaz o clase abstracta `T` |
| `Arg.Any<T>()` | Matcher de NSubstitute que acepta cualquier valor del tipo `T` como argumento |
| `Arg.Is<T>(predicate)` | Matcher de NSubstitute que acepta solo argumentos que cumplen el predicado indicado |
| `Received(n)` | Verificación de NSubstitute que el método fue llamado exactamente `n` veces |
| `DidNotReceive()` | Verificación de NSubstitute que un método nunca fue invocado durante el test |
| Builder pattern de tests | Clase auxiliar con método `Build()` que construye objetos de prueba con valores por defecto configurables |
| Invariante de dominio | Regla de negocio que la entidad debe cumplir en todo momento; se testa sin mocks ni infraestructura |
| Aislamiento | Principio de tests unitarios: el test no debe depender de red, disco, DB ni estado global externo |
| SUT (System Under Test) | La clase o componente específico que está siendo probado en un test determinado |

---

*Rogelio Arriaga Gonzalez*
