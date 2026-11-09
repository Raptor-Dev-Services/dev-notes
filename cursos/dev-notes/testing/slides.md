<!-- _class: lead -->

# Testing en .NET 10
## xUnit  -  NSubstitute  -  NetArchTest  -  TDD

**Dev Notes**
Material de auto-estudio  -  Rogelio Arriaga  -  Mayo 2026

---

<!-- _class: lead -->

# Parte 1
## Por qué testear

---

## El costo real de un bug

| Momento en que se detecta | Costo relativo |
|---------------------------|---------------|
| Durante el desarrollo (TDD) | ×1 |
| En code review | ×6 |
| En QA / staging | ×15 |
| En producción (cliente lo reporta) | ×30-100 |

> Fuente: IBM Systems Sciences Institute. Un bug en producción cuesta hasta 100× más que detectarlo al escribir el código.

**Tests automatizados = detectar bugs en el momento más barato.**

---

## La pirámide de tests

```
         /\
        /  \         E2E tests         <- pocos, lentos, frágiles
       /----\
      /      \       Integration tests <- verifican el stack completo
     /--------\
    /          \     Unit tests        <- muchos, rápidos, precisos
   /____________\
```

**Distribución recomendada para el sistema de mensajería:**

| Nivel | % | Velocidad | Qué verifica |
|-------|----|-----------|--------------|
| Unit tests | 70% | < 1 ms/test | Handlers, Validators, lógica de dominio |
| Integration tests | 20% | 100-500 ms | DI, routing, DB real, auth |
| E2E (Playwright) | 10% | segundos | Flujos completos de usuario |

---

## Qué gana el equipo

- **Confianza al refactorizar**  -  si los tests pasan, el comportamiento no cambió
- **Documentación viva**  -  un test bien nombrado describe exactamente qué hace el código
- **Feedback inmediato**  -  `dotnet test` en 3 segundos vs correr la app y probar manualmente
- **Regresiones detectadas**  -  un bug arreglado no vuelve si tiene su test

### Sin tests en el sistema de mensajería:
- Cambiar la lógica de asignación de conversaciones -> ¿rompió el login? Nadie sabe.
- Agregar un canal nuevo -> ¿afectó los contactos? Hay que probarlo a mano.
- Merge al main -> ¿qué rompió? Descúbrelo en staging, frente al cliente.

---

<!-- _class: lead -->

# Parte 2
## xUnit  -  El framework

---

## Estructura del proyecto de tests

El back-template tiene un proyecto de tests por módulo, más uno transversal:

```
Modules/Users/Users.Tests/
|-- Architecture/
|   `-- UsersArchitectureTests.cs    <- NetArchTest
`-- UseCases/
    `-- GetUserProfileHandlerTests.cs <- NSubstitute + xUnit

Shared/Authentication/Authentication.Tests/
|-- Architecture/
|   `-- AuthenticationArchitectureTests.cs
`-- UseCases/
    `-- LoginHandlerTests.cs

Tests/
`-- Domain/
    `-- SampleDomainTests.cs         <- placeholder de dominio
```

---

## Packages del proyecto de tests

```xml
<!-- Users.Tests.csproj -->
<PackageReference Include="xunit"                Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.5.1" />
<PackageReference Include="NSubstitute"          Version="5.3.0" />
<PackageReference Include="NetArchTest.Rules"    Version="1.3.2" />
<PackageReference Include="coverlet.collector"   Version="10.0.0" />
```

```xml
<!-- Referencias al módulo completo -->
<ProjectReference Include="../Users.Domain/Users.Domain.csproj" />
<ProjectReference Include="../Users.Application/Users.Application.csproj" />
<ProjectReference Include="../Users.Infrastructure/Users.Infrastructure.csproj" />
<ProjectReference Include="../Users.Presentation/Users.Presentation.csproj" />
```

> El template **no usa FluentAssertions**  -  usa `Assert` nativo de xUnit.

---

## [Fact]  -  un caso concreto

```csharp
// Users.Tests/UseCases/GetUserProfileHandlerTests.cs
using NSubstitute;
using Users.Application.UseCases.GetUserProfile;
using Users.Application.UseCases.GetUserProfile.Responses;
using Users.Domain.Entities;
using Users.Domain.Repositories;
using Xunit;

namespace Users.Tests.UseCases;

public sealed class GetUserProfileHandlerTests
{
    private readonly IUserProfileRepository _repo =
        Substitute.For<IUserProfileRepository>();

    [Fact]
    public async Task Handle_WhenProfileExists_ReturnsSuccess()
    {
        var profile = new UserProfile
        {
            PublicId = Guid.NewGuid(), TenantId = 1,
            FullName = "John Doe", IsActive = true,
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        _repo.GetByPublicIdAsync(profile.PublicId, 1, Arg.Any<CancellationToken>())
             .Returns(profile);

        var result = await new GetUserProfileHandler(_repo)
            .Handle(new GetUserProfileRequest(profile.PublicId, 1), default);

        var success = Assert.IsType<GetUserProfileSuccess>(result);
        Assert.NotNull(success.Data);
        Assert.Equal("John Doe", success.Data.FullName);
    }
}
```

---

## [Fact]  -  el caso negativo

```csharp
// Users.Tests/UseCases/GetUserProfileHandlerTests.cs (continuación)

[Fact]
public async Task Handle_WhenProfileNotFound_ReturnsNotFoundFailure()
{
    _repo.GetByPublicIdAsync(
            Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
         .Returns((UserProfile?)null);

    var result = await new GetUserProfileHandler(_repo)
        .Handle(new GetUserProfileRequest(Guid.NewGuid(), 1), default);

    Assert.IsType<GetUserProfileNotFoundFailure>(result);
}
```

**Observar:**
- `Assert.IsType<T>()`  -  xUnit nativo, sin FluentAssertions
- El tipo concreto `GetUserProfileNotFoundFailure`  -  no `INotFoundFailure`
- `(UserProfile?)null`  -  el cast explícito es necesario con NSubstitute

---

## Por qué usar el tipo concreto en Assert

```csharp
//  Usar la interfaz  -  pierdes acceso a las propiedades concretas
Assert.IsAssignableFrom<INotFoundFailure>(result);

//  Usar el tipo concreto  -  compilador verifica el tipo real
Assert.IsType<GetUserProfileNotFoundFailure>(result);

//  Además, si necesitas acceder a datos del Success:
var success = Assert.IsType<GetUserProfileSuccess>(result);
Assert.Equal("John Doe", success.Data.FullName);
//                        ↑ success.Data es UserProfileDto  -  con intellisense completo
```

`Assert.IsType<T>()` devuelve el valor casteado, listo para inspeccionar.

---

## Convención de nombres

```
{Método}_{Escenario}_{ResultadoEsperado}
```

| Ejemplo del back-template |  /  |
|---------------------------|--------|
| `Handle_WhenProfileExists_ReturnsSuccess` |  |
| `Handle_WhenProfileNotFound_ReturnsNotFoundFailure` |  |
| `Handle_WhenCredentialNotFound_ReturnsInvalidCredentialsFailure` |  |
| `Handle_WhenPasswordInvalid_ReturnsInvalidCredentialsFailure` |  |
| `Handle_WhenValidCredentials_ReturnsLoginSuccess` |  |
| `TestLogin()` |   -  no describe qué ni resultado |
| `Test1()` |   -  no dice nada |

**El nombre es la documentación.** Un test que falla debe explicarse solo con su nombre.

---

## [Theory] + [InlineData]  -  múltiples casos

```csharp
// Mismo test, datos distintos  -  xUnit corre una vez por cada [InlineData]
[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData(null)]
public void Validator_WhenFullNameEmpty_HasValidationError(string? fullName)
{
    var validator = new CreateContactRequestValidator();
    var request   = new CreateContactRequest(fullName!, "ana@example.com", "+52555000");

    var result = validator.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == "FullName");
}
```

`[Theory]` es ideal para validators: un solo bloque cubre todos los inputs inválidos.

---

## [MemberData]  -  datos más complejos

```csharp
public static IEnumerable<object[]> InvalidPhones()
{
    yield return new object[] { "123",        "Mínimo 10 dígitos." };
    yield return new object[] { "abcdefghij", "Solo dígitos permitidos." };
    yield return new object[] { "",           "Teléfono requerido." };
}

[Theory]
[MemberData(nameof(InvalidPhones))]
public void Validator_InvalidPhone_HasExpectedError(
    string phone, string expectedMsg)
{
    var validator = new CreateContactRequestValidator();
    var request   = new CreateContactRequest("Ana", "ana@example.com", phone);

    var result = validator.Validate(request);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.ErrorMessage == expectedMsg);
}
```

---

<!-- _class: lead -->

# Parte 3
## Unit Tests  -  Handlers con NSubstitute

---

## Por qué mockear las dependencias

```csharp
//  Sin mock  -  el Handler golpea la DB real
public async Task Handle_WhenProfileExists_ReturnsSuccess()
{
    var repo    = new UserProfileRepository(/* conexión real */);
    var handler = new GetUserProfileHandler(repo);
    // Depende de datos en DB, de la red, del estado previo.
    // Falla si no hay datos, si la DB está caída, si otro test limpió la tabla.
}

//  Con NSubstitute  -  controlamos exactamente qué devuelve el repositorio
var repo = Substitute.For<IUserProfileRepository>();
repo.GetByPublicIdAsync(publicId, 1L, Arg.Any<CancellationToken>())
    .Returns(profile);
```

**Regla:** en un unit test, solo se prueba **una clase**. Todo lo demás es un sustituto.

---

## NSubstitute  -  sintaxis esencial

```csharp
// Crear sustituto de una interfaz
var repo = Substitute.For<IUserProfileRepository>();

// Retorno simple  -  cuando se llame con estos argumentos
repo.GetByPublicIdAsync(publicId, 1L, Arg.Any<CancellationToken>())
    .Returns(profile);

// Retorno null (entidad no existe)
repo.GetByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
    .Returns((UserProfile?)null);

// Verificar que se llamó exactamente N veces
await repo.Received(1).InsertAsync(
    Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<string>(),
    Arg.Any<CancellationToken>());

// Verificar que NO se llamó
await repo.DidNotReceive().InsertAsync(
    Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<string>(),
    Arg.Any<CancellationToken>());
```

---

## Handler de lectura  -  ejemplo real del back-template

```csharp
// Users.Tests/UseCases/GetUserProfileHandlerTests.cs
using NSubstitute;
using Users.Application.UseCases.GetUserProfile;
using Users.Application.UseCases.GetUserProfile.Responses;
using Users.Domain.Entities;
using Users.Domain.Repositories;
using Xunit;

namespace Users.Tests.UseCases;

public sealed class GetUserProfileHandlerTests
{
    private readonly IUserProfileRepository _repo =
        Substitute.For<IUserProfileRepository>();

    [Fact]
    public async Task Handle_WhenProfileExists_ReturnsSuccess()
    {
        var profile = new UserProfile
        {
            PublicId = Guid.NewGuid(), TenantId = 1,
            FullName = "John Doe",     IsActive  = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _repo.GetByPublicIdAsync(profile.PublicId, 1, Arg.Any<CancellationToken>())
             .Returns(profile);

        var result = await new GetUserProfileHandler(_repo)
            .Handle(new GetUserProfileRequest(profile.PublicId, 1), default);

        var success = Assert.IsType<GetUserProfileSuccess>(result);
        Assert.Equal("John Doe", success.Data.FullName);
    }
}
```

---

## El Handler que estamos probando

```csharp
// Users.Application/UseCases/GetUserProfile/GetUserProfileHandler.cs
using Common.Messaging;
using Users.Application.UseCases.GetUserProfile.Responses;
using Users.Contracts.Dtos;
using Users.Domain.Repositories;

namespace Users.Application.UseCases.GetUserProfile;

public sealed class GetUserProfileHandler
    : IRequestHandler<GetUserProfileRequest, GetUserProfileResponse>
{
    private readonly IUserProfileRepository _profiles;

    public GetUserProfileHandler(IUserProfileRepository profiles)
        => _profiles = profiles;

    public async Task<GetUserProfileResponse> Handle(
        GetUserProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _profiles
            .GetByPublicIdAsync(request.PublicId, request.TenantId, cancellationToken);
        if (profile is null)
            return new GetUserProfileNotFoundFailure("Perfil de usuario no encontrado.");

        return new GetUserProfileSuccess(new UserProfileDto(
            profile.PublicId, profile.FullName, profile.IsActive,
            profile.CreatedAtUtc, profile.UpdatedAtUtc));
    }
}
```

---

## Las Response records del Handler

```csharp
// GetUserProfileResponse.cs
public abstract record GetUserProfileResponse : IResponse;

// GetUserProfileSuccess.cs
public sealed record GetUserProfileSuccess(UserProfileDto Data)
    : GetUserProfileResponse, ISuccess<UserProfileDto>;

// GetUserProfileNotFoundFailure.cs
public sealed record GetUserProfileNotFoundFailure(string Message)
    : GetUserProfileResponse, INotFoundFailure;
```

```csharp
// Common/Results/ISuccess.cs
public interface ISuccess { }
public interface ISuccess<T> : ISuccess { T Data { get; } }

// Common/Results/IFailure.cs
public interface INotFoundFailure : IFailure { }
public interface IConflictFailure : IFailure { }
public interface IValidationFailure : IFailure { }
```

---

## Handler con múltiples dependencias  -  LoginHandlerTests

```csharp
// Authentication.Tests/UseCases/LoginHandlerTests.cs
using Authentication.Application.Services;
using Authentication.Application.UseCases.Login;
using Authentication.Application.UseCases.Login.Responses;
using Authentication.Domain.Entities;
using Authentication.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace Authentication.Tests.UseCases;

public sealed class LoginHandlerTests
{
    private readonly IUserCredentialRepository _credentials =
        Substitute.For<IUserCredentialRepository>();
    private readonly IRefreshTokenRepository _refreshTokens =
        Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher  _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt    = Substitute.For<IJwtTokenService>();
```

---

## LoginHandlerTests  -  los tres escenarios

```csharp
    [Fact]
    public async Task Handle_WhenCredentialNotFound_ReturnsInvalidCredentialsFailure()
    {
        _credentials.GetForLoginAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserCredential?)null);

        var result = await new LoginHandler(_credentials, _refreshTokens, _hasher, _jwt)
            .Handle(new LoginRequest("user@example.com", "password"), default);

        Assert.IsType<LoginInvalidCredentialsFailure>(result);
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ReturnsInvalidCredentialsFailure()
    {
        var credential = new UserCredential
        {
            Id = 1, PublicId = Guid.NewGuid(), TenantId = 1,
            Email = "user@example.com", PasswordHash = "hash",
            Role = "User", IsActive = true
        };
        _credentials.GetForLoginAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(credential);
        _hasher.Verify("wrong-password", "hash").Returns(false);

        var result = await new LoginHandler(_credentials, _refreshTokens, _hasher, _jwt)
            .Handle(new LoginRequest("user@example.com", "wrong-password"), default);

        Assert.IsType<LoginInvalidCredentialsFailure>(result);
    }
```

---

## LoginHandlerTests  -  el caso de éxito

```csharp
    [Fact]
    public async Task Handle_WhenValidCredentials_ReturnsLoginSuccess()
    {
        var credential = new UserCredential
        {
            Id = 1, PublicId = Guid.NewGuid(), TenantId = 1,
            Email = "user@example.com", PasswordHash = "hash",
            Role = "User", IsActive = true
        };
        _credentials.GetForLoginAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(credential);
        _hasher.Verify("password", "hash").Returns(true);
        _jwt.GenerateAccessToken(
                Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<long>())
            .Returns("access-token");
        _jwt.GenerateRefreshToken().Returns("refresh-token");
        _jwt.GetRefreshTokenExpiry().Returns(DateTime.UtcNow.AddDays(7));

        var result = await new LoginHandler(_credentials, _refreshTokens, _hasher, _jwt)
            .Handle(new LoginRequest("user@example.com", "password"), default);

        var success = Assert.IsType<LoginSuccess>(result);
        Assert.Equal("access-token",  success.Data.AccessToken);
        Assert.Equal("refresh-token", success.Data.RefreshToken);
    }
```

---

<!-- _class: lead -->

# Parte 4
## Tests de arquitectura  -  NetArchTest

---

## Qué son los tests de arquitectura

Los tests de arquitectura verifican que las dependencias entre capas se respeten **a nivel de ensamblado**. Si alguien en Infrastructure importa algo de Application, el test falla.

```
Domain        <- no depende de nadie
Application   <- depende solo de Domain
Infrastructure<- depende de Domain (implementa contratos)
Presentation  <- depende de Application (llama Handlers vía MediatR)
```

**NetArchTest** inspecciona los metadatos del ensamblado en tiempo de ejecución. No analiza código fuente  -  analiza el IL compilado.

---

## Tests de arquitectura  -  Users module (real del back-template)

```csharp
// Users.Tests/Architecture/UsersArchitectureTests.cs
using NetArchTest.Rules;
using System.Reflection;
using Users.Application.UseCases.GetUserProfile;
using Users.Domain.Entities;
using Users.Infrastructure.Repositories;
using Xunit;

namespace Users.Tests.Architecture;

public sealed class UsersArchitectureTests
{
    private const string ApplicationNs    = "Users.Application";
    private const string InfrastructureNs = "Users.Infrastructure";
    private const string PresentationNs   = "Users.Presentation";

    private static readonly Assembly DomainAssembly =
        typeof(UserProfile).Assembly;
    private static readonly Assembly ApplicationAssembly =
        typeof(GetUserProfileHandler).Assembly;
    private static readonly Assembly InfrastructureAssembly =
        typeof(UserProfileRepository).Assembly;
```

---

## Reglas que verifica UsersArchitectureTests

```csharp
    [Fact]
    public void Domain_MustNot_DependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn(ApplicationNs)
            .GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_MustNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn(InfrastructureNs)
            .GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_MustNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot().HaveDependencyOn(InfrastructureNs)
            .GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Infrastructure_MustNot_DependOn_Application()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot().HaveDependencyOn(ApplicationNs)
            .GetResult();
        Assert.True(result.IsSuccessful);
    }
```

---

## El mismo patrón en Authentication y Tenancy

```csharp
// Authentication.Tests/Architecture/AuthenticationArchitectureTests.cs
private const string ApplicationNs    = "Authentication.Application";
private const string InfrastructureNs = "Authentication.Infrastructure";
private const string PresentationNs   = "Authentication.Presentation";

private static readonly Assembly DomainAssembly =
    typeof(UserCredential).Assembly;
private static readonly Assembly ApplicationAssembly =
    typeof(LoginHandler).Assembly;
private static readonly Assembly InfrastructureAssembly =
    typeof(UserCredentialRepository).Assembly;
```

```csharp
// Tenancy.Tests/Architecture/TenancyArchitectureTests.cs
private const string ApplicationNs    = "Tenancy.Application";
private const string InfrastructureNs = "Tenancy.Infrastructure";
private const string PresentationNs   = "Tenancy.Presentation";

private static readonly Assembly DomainAssembly =
    typeof(Tenant).Assembly;          // Tenancy.Domain
private static readonly Assembly ApplicationAssembly =
    typeof(TenancyApi).Assembly;      // Tenancy.Application
```

Cada módulo del sistema de mensajería tendrá su `*ArchitectureTests.cs` idéntico en estructura.

---

## Por qué los tests de arquitectura son valiosos

```
Sin tests de arquitectura, estos errores pasan desapercibidos:

 Un Handler en Application importa EF Core directamente
   -> viola Clean Architecture: Application no debe conocer la DB

 Un Repository en Infrastructure llama a un Handler de Application
   -> ciclo de dependencias

 Un Controller en Presentation crea un DbContext manualmente
   -> bypasea el pipeline de DI

 Con NetArchTest, cualquiera de los tres hace fallar el test
   en el primer `dotnet test`  -  antes de llegar a code review.
```


---

<!-- _class: lead -->

# Parte 5
## Unit Tests en el sistema de mensajería

---

## Qué testear con unit tests (y qué no)

| Escenario | Decisión |
|-----------|----------|
| Lógica de Handler (flujos, condiciones de negocio) |  Unit test con NSubstitute |
| Validators de FluentValidation |  Unit test  -  rápidos, sin dependencias |
| Métodos de dominio en entidades |  Unit test  -  sin mocks necesarios |
| Dependencias entre capas (NetArchTest) |  Architecture test |
| SQL / persistencia real |  No  -  ver Integration Tests |
| Controllers / Presenters |  Mínimo  -  cubiertos por integration tests |
| Lógica de routing |  No  -  cubierta por integration tests |

---

## Test de Handler de escritura  -  CreateContact

```csharp
// Contacts.Tests/UseCases/CreateContactHandlerTests.cs
using Contacts.Application.UseCases.CreateContact;
using Contacts.Application.UseCases.CreateContact.Responses;
using Contacts.Domain.Entities;
using Contacts.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace Contacts.Tests.UseCases;

public sealed class CreateContactHandlerTests
{
    private readonly IContactRepository _repo =
        Substitute.For<IContactRepository>();

    [Fact]
    public async Task Handle_WhenEmailNew_InsertsContactReturnsSuccess()
    {
        _repo.EmailExistsAsync(
                "nueva@example.com", 1L, Arg.Any<CancellationToken>())
             .Returns(false);

        var result = await new CreateContactHandler(_repo)
            .Handle(new CreateContactRequest(
                "Ana García", "nueva@example.com", "+525551234567", 1L), default);

        Assert.IsType<CreateContactSuccess>(result);
        await _repo.Received(1).InsertAsync(
            Arg.Any<Guid>(), 1L,
            Arg.Is<string>(n => n == "Ana García"),
            Arg.Any<CancellationToken>());
    }
```

---

## Handler de escritura  -  el caso de conflicto

```csharp
    [Fact]
    public async Task Handle_WhenEmailDuplicate_ReturnsConflictFailure()
    {
        _repo.EmailExistsAsync(
                "duplicado@example.com", 1L, Arg.Any<CancellationToken>())
             .Returns(true);

        var result = await new CreateContactHandler(_repo)
            .Handle(new CreateContactRequest(
                "Ana", "duplicado@example.com", "+525551234567", 1L), default);

        Assert.IsType<CreateContactConflictFailure>(result);
        await _repo.DidNotReceive().InsertAsync(
            Arg.Any<Guid>(), Arg.Any<long>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
```

`DidNotReceive()` verifica que si hay conflicto, **no se inserta nada**  -  prueba el efecto secundario esperado.

---

## Test de Handler  -  AssignConversation

```csharp
// Conversations.Tests/UseCases/AssignConversationHandlerTests.cs
using Conversations.Application.UseCases.AssignConversation;
using Conversations.Application.UseCases.AssignConversation.Responses;
using Conversations.Domain.Entities;
using Conversations.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace Conversations.Tests.UseCases;

public sealed class AssignConversationHandlerTests
{
    private readonly IConversationRepository _repo =
        Substitute.For<IConversationRepository>();

    [Fact]
    public async Task Handle_WhenConversationExists_AssignsAgentReturnsSuccess()
    {
        var convId  = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conv    = new Conversation
            { PublicId = convId, TenantId = 1L, AssignedAgentId = null };

        _repo.GetByPublicIdAsync(convId, 1L, Arg.Any<CancellationToken>())
             .Returns(conv);

        var result = await new AssignConversationHandler(_repo)
            .Handle(new AssignConversationRequest(convId, agentId, 1L), default);

        Assert.IsType<AssignConversationSuccess>(result);
        Assert.Equal(agentId, conv.AssignedAgentId);
    }
```

---

## AssignConversation  -  caso no encontrado

```csharp
    [Fact]
    public async Task Handle_WhenConversationNotFound_ReturnsNotFoundFailure()
    {
        _repo.GetByPublicIdAsync(
                Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
             .Returns((Conversation?)null);

        var result = await new AssignConversationHandler(_repo)
            .Handle(new AssignConversationRequest(
                Guid.NewGuid(), Guid.NewGuid(), 1L), default);

        Assert.IsType<AssignConversationNotFoundFailure>(result);
        await _repo.DidNotReceive()
            .UpdateAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
    }
}
```

Dos tests, dos escenarios, cero base de datos.
`DidNotReceive()` confirma que el happy path no se ejecutó.

---

## Ejecutar los tests

```bash
# Todos los tests del solution
dotnet test

# Con detalle (ver cada test individualmente)
dotnet test --verbosity normal

# Solo una clase
dotnet test --filter "FullyQualifiedName~GetUserProfileHandlerTests"

# Solo tests de arquitectura
dotnet test --filter "FullyQualifiedName~ArchitectureTests"

# Solo un módulo
dotnet test Modules/Users/Users.Tests/Users.Tests.csproj

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

---

<!-- _class: lead -->

# Parte 6
## Integration Tests  -  El stack completo

---

## Qué verifican los integration tests

```
Unit test:    Handler + mock de repo -> verifica lógica del handler
Integration:  HTTP GET /api/users/{id} -> verifica TODO el stack

  HTTP Client
      ↓
  Minimal API Endpoint   <- ¿está el route bien registrado?
      ↓
  MediatR                <- ¿el Handler está registrado en DI?
      ↓
  GetUserProfileHandler  <- lógica
      ↓
  UserProfileRepository  <- ¿EF Core configurado bien?
      ↓
  PostgreSQL             <- DB REAL (no mock)
      ↓
  Presenter              <- serialización JSON correcta
```

Si el DI no registró algo, si la migración falló, si el route está mal  -  **los unit tests no lo detectan. Los integration tests sí.**

---

## WebApplicationFactory  -  levantar la API en memoria

```csharp
// Tests/Integration/Fixtures/WebAppFixture.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class WebAppFixture
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]      = "test-jwt-key-min-32-characters-long!!",
                ["Jwt:Issuer"]   = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();
}
```

---

## Por qué NO mockear la DB en integration tests

```csharp
//  Reemplazar EF Core con InMemory
services.RemoveAll<AppDbContext>();
services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("test"));
```

**Problemas con InMemoryDatabase:**
- No enforcea FK constraints  -  un `Contact` sin `TenantId` válido pasa sin error
- No soporta transacciones reales
- No ejecuta índices  -  las queries lentas no se detectan
- No valida tipos de columna  -  `decimal(18,2)` guarda cualquier cosa
- Un bug que solo ocurre en PostgreSQL pasará desapercibido

```csharp
//  Usar PostgreSQL real con Testcontainers
// Levanta un contenedor Docker de PostgreSQL 17 solo para tests.
// La DB es idéntica a producción  -  mismas migraciones, constraints y SQL.
```

**Regla del equipo:** si el test toca la base de datos, usa PostgreSQL real. Siempre.

---

## Escribir un integration test básico

```csharp
// Tests/Integration/Users/UserProfileApiTests.cs
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Integration.Users;

public sealed class UserProfileApiTests : IClassFixture<WebAppFixture>
{
    private readonly HttpClient _client;

    public UserProfileApiTests(WebAppFixture factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task GetProfile_WhenNoAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

`IClassFixture<WebAppFixture>`  -  la fixture se crea una vez por clase, no por test.

---

## JWT en integration tests

```csharp
// Tests/Helpers/JwtTestHelper.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class JwtTestHelper
{
    private const string TestKey      = "test-jwt-key-min-32-characters-long!!";
    private const string TestIssuer   = "test-issuer";
    private const string TestAudience = "test-audience";

    public static string GenerateToken(
        Guid   userId = default,
        long   tenantId = 1L,
        string email  = "test@example.com",
        string role   = "agent")
    {
        if (userId == default) userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));
        var token = new JwtSecurityToken(TestIssuer, TestAudience, claims,
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials:
                            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## Integration test con auth

```csharp
// Tests/Integration/Users/UserProfileApiTests.cs (continuación)

[Fact]
public async Task GetProfile_WhenAuthenticated_Returns200()
{
    var agentId = Guid.NewGuid();
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken(userId: agentId, role: "agent"));

    var response = await _client.GetAsync(
        $"/api/users/profile/{agentId}?tenantId=1");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task GetProfile_WhenAdminRole_Returns200()
{
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken(role: "admin"));

    var response = await _client.GetAsync("/api/users/profile");
    Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

<!-- _class: lead -->

# Parte 7
## TDD  -  Test-Driven Development

---

## El ciclo Red / Green / Refactor

```
1. RED     -  Escribir un test que falla (ni compila está bien)
               El test describe el comportamiento esperado.
               No existe código de producción que lo haga pasar.

2. GREEN   -  Escribir el MÍNIMO de código para que el test pase.
               No buscar elegancia, solo que el test sea verde.
               Resistir la tentación de implementar más de lo necesario.

3. REFACTOR  -  Mejorar el código sin cambiar el comportamiento.
               Los tests siguen en verde durante todo el refactor.
               Ahora sí aplicar buenas prácticas, nombres, estructura.

-> Repetir con el siguiente test
```

> TDD no es escribir todos los tests primero y luego todo el código.
> Es un **ciclo corto**: 1 test -> código mínimo -> refactor -> siguiente test.

---

## Paso 1  -  RED: el test antes del código

```csharp
// Conversations.Tests/UseCases/AssignConversationHandlerTests.cs
[Fact]
public async Task Handle_WhenConversationExists_AssignsAgentReturnsSuccess()
{
    // Arrange
    var repo    = Substitute.For<IConversationRepository>();
    var handler = new AssignConversationHandler(repo);  // <- NO EXISTE AÚN
    var convId  = Guid.NewGuid();
    var agentId = Guid.NewGuid();

    var conv = new Conversation                         // <- tampoco existe
        { PublicId = convId, TenantId = 1L, AssignedAgentId = null };
    repo.GetByPublicIdAsync(convId, 1L, Arg.Any<CancellationToken>())
        .Returns(conv);

    // Act
    var result = await handler.Handle(                  // <- no compila
        new AssignConversationRequest(convId, agentId, 1L), default);

    // Assert
    Assert.IsType<AssignConversationSuccess>(result);
    Assert.Equal(agentId, conv.AssignedAgentId);
}
```

El test **no compila**  -  `AssignConversationHandler`, `AssignConversationRequest` no existen. Eso es correcto.

---

## Paso 2  -  GREEN: código mínimo para compilar y pasar

```csharp
// Conversations.Application/UseCases/AssignConversation/
// AssignConversationRequest.cs
public sealed record AssignConversationRequest(
    Guid ConversationId, Guid AgentId, long TenantId)
    : IRequest<AssignConversationResponse>;

public abstract record AssignConversationResponse : IResponse;
public sealed record AssignConversationSuccess()
    : AssignConversationResponse, ISuccess;
public sealed record AssignConversationNotFoundFailure(string Message)
    : AssignConversationResponse, INotFoundFailure;
```

```csharp
// AssignConversationHandler.cs
public sealed class AssignConversationHandler
    : IRequestHandler<AssignConversationRequest, AssignConversationResponse>
{
    private readonly IConversationRepository _repo;
    public AssignConversationHandler(IConversationRepository repo) => _repo = repo;

    public async Task<AssignConversationResponse> Handle(
        AssignConversationRequest request, CancellationToken ct)
    {
        var conv = await _repo.GetByPublicIdAsync(request.ConversationId, request.TenantId, ct);
        if (conv is null)
            return new AssignConversationNotFoundFailure("Conversación no encontrada.");
        conv.AssignedAgentId = request.AgentId;
        await _repo.UpdateAsync(conv, ct);
        return new AssignConversationSuccess();
    }
}
```

---

## Paso 3  -  Agregar más tests (RED) antes de más código

```csharp
[Fact]
public async Task Handle_WhenConversationNotFound_ReturnsNotFoundFailure()
{
    _repo.GetByPublicIdAsync(
            Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
         .Returns((Conversation?)null);

    var result = await new AssignConversationHandler(_repo)
        .Handle(new AssignConversationRequest(
            Guid.NewGuid(), Guid.NewGuid(), 1L), default);

    Assert.IsType<AssignConversationNotFoundFailure>(result);
    await _repo.DidNotReceive()
        .UpdateAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
}
```

Este test **pasa inmediatamente**  -  el Handler ya maneja `null`. Eso es TDD funcionando: el código ya tiene el comportamiento porque el test anterior lo forzó.

---

## Paso 4  -  REFACTOR: mejorar sin romper

```csharp
// Con todos los tests en verde, refactorizar con confianza.
// ¿Hay magic strings? -> extraer constante o mover al dominio

public sealed class AssignConversationHandler
    : IRequestHandler<AssignConversationRequest, AssignConversationResponse>
{
    private readonly IConversationRepository _repo;

    public AssignConversationHandler(IConversationRepository repo)
        => _repo = repo;

    public async Task<AssignConversationResponse> Handle(
        AssignConversationRequest request, CancellationToken ct)
    {
        var conv = await _repo
            .GetByPublicIdAsync(request.ConversationId, request.TenantId, ct);

        if (conv is null)
            return NotFound();

        conv.AssignedAgentId = request.AgentId;
        await _repo.UpdateAsync(conv, ct);
        return new AssignConversationSuccess();
    }

    private static AssignConversationNotFoundFailure NotFound() =>
        new("Conversación no encontrada.");
}
```

---

## Cuándo TDD agrega más valor

| Escenario | Valor |
|-----------|-------|
| Lógica de Handlers y Validators | Alto  -  fuerza pensar los casos antes de codear |
| Bug reproducible en producción | Alto  -  escribir el test que lo reproduce, luego arreglarlo |
| Código de infraestructura (SQL, HTTP) | Bajo  -  difícil sin contenedor real |
| Código exploratorio ("¿cómo funciona X?") | Bajo  -  explorar primero, testear después |
| Refactor de Handler existente | Alto  -  los tests existentes son el cinturón de seguridad |

---

<!-- _class: lead -->

# Parte 8
## Coverage  -  Qué medir y qué ignorar

---

## Code coverage: qué mide

Coverage mide qué porcentaje del código de producción fue ejecutado por los tests.

```bash
dotnet test --collect:"XPlat Code Coverage"
# Genera coverage.cobertura.xml
# Ver en VS Code con Coverage Gutters, o con reportgenerator
```

```
Archivo                              Líneas  Branches  Cobertura
--------------------------------------------------------------
GetUserProfileHandler.cs             15/15   2/2       100%
LoginHandler.cs                      28/32   6/8        88%
UserProfileRepository.cs             12/60   2/10       20%  <- infra
AppDbContext.cs                       0/20   0/0         0%  <- ignorar
Program.cs                            0/15   0/0         0%  <- ignorar
```

**El número solo tiene sentido si los tests verifican comportamiento real.**

---

## El umbral correcto: 70-80%, no 100%

| Zona | Coverage objetivo | Razón |
|------|------------------|-------|
| Handlers (`Application/`) | 85-95% | Lógica crítica de negocio |
| Validators | 90-100% | Puro, sin dependencias, barato |
| Domain entities / Value Objects | 80-90% | Invariantes de negocio |
| Repositories / Infrastructure | 40-60% | Cubiertos por integration tests |
| Controllers / Presenters | 30-50% | Cubiertos por integration tests |
| Program.cs / bootstrapping | 0-10% | No testear configuración |

> **100% coverage no es la meta.** Un test que solo ejecuta código para subir el número, sin verificar comportamiento, es peor que no tener el test.

---

## Qué ignorar en el reporte de coverage

```csharp
// Excluir del reporte con atributo
[ExcludeFromCodeCoverage]
public sealed class AppDbContext : DbContext { ... }

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions { ... }
```

```xml
<!-- coverlet.runsettings  -  excluir namespaces enteros -->
<DataCollectionRunSettings>
  <DataCollectors>
    <DataCollector friendlyName="XPlat code coverage">
      <Configuration>
        <Exclude>[*.Infrastructure]*,[*.Host]*</Exclude>
      </Configuration>
    </DataCollector>
  </DataCollectors>
</DataCollectionRunSettings>
```

**El coverage de Infrastructure y Host no dice nada útil**  -  esos paths se validan con integration tests.

---

<!-- _class: lead -->

# Resumen

---

## Resumen final

| Concepto | Clave |
|----------|-------|
| Pirámide | 70% unit  -  20% integration  -  10% E2E |
| `[Fact]` | Un escenario con datos fijos |
| `[Theory]` + `[InlineData]` | Mismo test, múltiples datasets |
| Naming | `Método_WhenEscenario_ResultadoEsperado` |
| Unit test | Una clase, dependencias mockeadas con NSubstitute |
| Architecture test | NetArchTest  -  dependencias entre capas vía ensamblado |
| Assert style | `Assert.IsType<T>()` nativo xUnit  -  no FluentAssertions |
| Integration test | HTTP real -> DB real (PostgreSQL + Testcontainers) |
| TDD | Red -> Green -> Refactor  -  ciclo corto, 1 test a la vez |
| Coverage | 70-80% como umbral, 100% no es la meta |

**Regla de oro:** si escribir el test es difícil, el código está acoplado. Ese dolor es información útil.

---

## Estructura de tests por módulo del sistema de mensajería

```
Modules/Contacts/Contacts.Tests/
|-- Architecture/ContactsArchitectureTests.cs
`-- UseCases/
    |-- GetContactHandlerTests.cs
    |-- CreateContactHandlerTests.cs
    `-- CreateContactValidatorTests.cs

Modules/Conversations/Conversations.Tests/
|-- Architecture/ConversationsArchitectureTests.cs
`-- UseCases/
    |-- AssignConversationHandlerTests.cs
    `-- SendMessageHandlerTests.cs

Tests/
|-- Integration/
|   |-- Users/UserProfileApiTests.cs
|   `-- Auth/LoginApiTests.cs
`-- Helpers/
    `-- JwtTestHelper.cs
```
