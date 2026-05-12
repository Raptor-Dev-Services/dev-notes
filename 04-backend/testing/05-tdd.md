# 05 — TDD: Test-Driven Development

Escribir el test antes que el código de producción. El ciclo Red/Green/Refactor guía el desarrollo de fuera hacia adentro.

> Fuente: *Clean Code with C# 2nd Ed* (Jason Alls) — Ch.11 Test-Driven Development

---

## El problema que resuelve

Sin TDD, el código se escribe pensando en "que funcione", y los tests se agregan después como verificación. El resultado habitual: código difícil de testear porque no fue diseñado para serlo, tests escritos a la defensiva para hacer pasar casos que ya funcionan.

TDD invierte el orden: primero se define el comportamiento esperado (el test), después se implementa lo mínimo para cumplirlo.

---

## El ciclo Red / Green / Refactor

```
1. RED    — Escribir un test que falla (ni compila está bien)
               El test describe el comportamiento esperado.
               No existe código de producción que lo haga pasar.

2. GREEN  — Escribir el código mínimo para que el test pase.
               No se busca elegancia, solo que el test sea verde.
               Resistir la tentación de hacer más de lo necesario.

3. REFACTOR — Mejorar el código sin cambiar el comportamiento.
               Los tests siguen en verde durante todo el refactor.
               Ahora sí se aplican buenas prácticas, nombres, estructura.

→ Repetir
```

---

## Ejemplo completo: Handler de login

### Paso 1 — RED: escribir el test primero

```csharp
// Tests/Unit/Auth/LoginHandlerTests.cs
public sealed class LoginHandlerTests
{
    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenInSuccess()
    {
        // Arrange
        var password = "Password1";
        var hash     = BCrypt.Net.BCrypt.HashPassword(password);

        var repo = Substitute.For<IExampleUserRepository>();
        var jwt  = Substitute.For<IJwtTokenService>();

        repo.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>())
            .Returns(new ExampleUser
            {
                PublicId     = Guid.NewGuid(),
                Email        = "user@test.com",
                PasswordHash = hash,
                IsActive     = true
            });

        jwt.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>())
           .Returns("token.jwt.fake");

        var handler = new LoginHandler(repo, jwt);  // ← NO EXISTE AÚN

        // Act
        var result = await handler.Handle(
            new LoginRequest("user@test.com", password), CancellationToken.None);  // ← NO EXISTE

        // Assert
        result.Should().BeOfType<LoginSuccess>();
        ((LoginSuccess)result).Token.Should().Be("token.jwt.fake");
    }
}
```

El test no compila — `LoginHandler`, `LoginRequest`, `LoginSuccess` no existen. Eso es correcto en este punto.

### Paso 2 — GREEN: implementar lo mínimo para compilar y pasar

```csharp
// Application/UseCases/Auth/Login/LoginRequest.cs
public sealed record LoginRequest(string Email, string Password)
    : IRequest<LoginResponse>;

// Application/UseCases/Auth/Login/Responses/LoginResponse.cs
public abstract record LoginResponse : IResponse;
public sealed record LoginSuccess(string Token) : LoginResponse, ISuccess;
public sealed record LoginUnauthorizedFailure(string Message)
    : LoginResponse, IUnauthorizedFailure;

// Application/UseCases/Auth/Login/LoginHandler.cs
public sealed class LoginHandler : IRequestHandler<LoginRequest, LoginResponse>
{
    private readonly IExampleUserRepository _repo;
    private readonly IJwtTokenService       _jwt;

    public LoginHandler(IExampleUserRepository repo, IJwtTokenService jwt)
    {
        _repo = repo;
        _jwt  = jwt;
    }

    public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken ct)
    {
        var user = await _repo.GetByEmailAsync(request.Email, ct);
        if (user is null)
            return new LoginUnauthorizedFailure("Credenciales incorrectas.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new LoginUnauthorizedFailure("Credenciales incorrectas.");

        var token = _jwt.GenerateToken(user.PublicId, user.Email);
        return new LoginSuccess(token);
    }
}
```

Test verde. El Handler existe con lo mínimo para hacer pasar el test.

### Paso 3 — Agregar más tests antes de más código

```csharp
[Fact]
public async Task Handle_WrongPassword_ReturnsUnauthorizedFailure()
{
    var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1");
    var repo = Substitute.For<IExampleUserRepository>();
    var jwt  = Substitute.For<IJwtTokenService>();

    repo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(new ExampleUser { PasswordHash = hash, IsActive = true });

    var handler = new LoginHandler(repo, jwt);
    var result  = await handler.Handle(
        new LoginRequest("u@test.com", "WrongPassword1"), CancellationToken.None);

    result.Should().BeOfType<LoginUnauthorizedFailure>();
    jwt.DidNotReceive().GenerateToken(Arg.Any<Guid>(), Arg.Any<string>());
}

[Fact]
public async Task Handle_InactiveUser_ReturnsForbiddenFailure()
{
    var hash = BCrypt.Net.BCrypt.HashPassword("Password1");
    var repo = Substitute.For<IExampleUserRepository>();
    var jwt  = Substitute.For<IJwtTokenService>();

    repo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(new ExampleUser { PasswordHash = hash, IsActive = false });

    var handler = new LoginHandler(repo, jwt);
    var result  = await handler.Handle(
        new LoginRequest("u@test.com", "Password1"), CancellationToken.None);

    result.Should().BeOfType<LoginForbiddenFailure>();
}
```

El último test falla (RED) — no existe `LoginForbiddenFailure` ni el Handler maneja el caso `IsActive = false`.

### Paso 4 — GREEN: agregar el nuevo caso

```csharp
// Agregar al responses
public sealed record LoginForbiddenFailure(string Message)
    : LoginResponse, IForbiddenFailure;

// Actualizar el Handler
public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken ct)
{
    var user = await _repo.GetByEmailAsync(request.Email, ct);
    if (user is null)
        return new LoginUnauthorizedFailure("Credenciales incorrectas.");

    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return new LoginUnauthorizedFailure("Credenciales incorrectas.");

    if (!user.IsActive)
        return new LoginForbiddenFailure("Cuenta inactiva.");

    var token = _jwt.GenerateToken(user.PublicId, user.Email);
    return new LoginSuccess(token);
}
```

Todos los tests vuelven a verde.

### Paso 5 — REFACTOR

Con todos los tests en verde, refactorizar sin miedo:

```csharp
// ¿Hay duplicación? ¿Nombres que mejorar? ¿Extraer métodos?
// Ejemplo: el mensaje "Credenciales incorrectas." se repite — extraer a constante
private const string InvalidCredentialsMessage = "Credenciales incorrectas.";

// Los tests siguen en verde después de cualquier cambio de estructura
```

---

## Cuándo TDD agrega más valor

| Escenario | Valor de TDD |
|-----------|-------------|
| Lógica de negocio (handlers, validators) | ✓ Alto — fuerza pensar en los casos antes de implementar |
| Bugs reproducibles | ✓ Alto — escribir el test que reproduce el bug, luego arreglarlo |
| Refactoring | ✓ Alto — los tests existentes protegen el comportamiento |
| Código de infraestructura (SQL, HTTP) | Bajo — difícil sin un contenedor real |
| Código exploratorio ("¿cómo funciona X?") | Bajo — primero explorar, luego testear |
| Código UI / presentación | Bajo — difícil de automatizar |

---

## TDD con repositorios: Outside-In

TDD "outside-in" empieza por el test de integración (HTTP request) y va hacia adentro:

```
1. Test de integration: POST /api/example/users → 200 con usuario creado
2. Para que pase: necesita Controller → implementar
3. Controller necesita Handler → implementar (con mock del repo)
4. Handler necesita Repository → implementar (con TestContainers)
5. Todos los tests pasan → refactorizar
```

Este orden asegura que cada capa es necesaria — no se implementa lo que ningún test pide.

---

## Lo que TDD no es

```
✗ TDD no es escribir todos los tests primero y luego todo el código
   → es un ciclo corto: 1 test → código mínimo → refactor → siguiente test

✗ TDD no garantiza buen diseño
   → facilita buen diseño al hacer visible cuándo el código es difícil de testear

✗ TDD no es obligatorio para todo
   → elegir qué partes se benefician: lógica de negocio sí, infraestructura menos

✗ TDD no reemplaza el pensamiento
   → si no se entiende el dominio, los tests serán incorrectos aunque pasen
```

---

## Relación con back-template

El flujo de desarrollo recomendado para nuevos casos de uso:

1. Escribir `{Accion}HandlerTests.cs` con los casos (éxito, not found, conflicto, sin permisos).
2. Implementar `{Accion}Request.cs`, `{Accion}Response.cs`, `{Accion}Handler.cs`.
3. Tests en verde → agregar `{Accion}Presenter.cs` con test.
4. Agregar Controller endpoint.
5. Integration test final: el endpoint completo funciona.


---

*Rogelio Arriaga Gonzalez*
