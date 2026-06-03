# 40 — Gestión de Sesiones

La gestión de sesiones permite controlar las sesiones activas de los usuarios: ver desde qué dispositivos están conectados, revocar sesiones individuales o todas a la vez, forzar logout al cambiar el password o detectar actividad sospechosa.

---

## El problema con JWT

Un JWT es stateless. Una vez emitido, es válido hasta su expiración aunque el usuario haya cambiado su password. Sin gestión de sesiones activa, un JWT robado es válido hasta que expira (minutos u horas).

```
Sin gestión de sesiones:
  Usuario cambia su password → el JWT anterior sigue siendo válido por 15 minutos
  Atacante con el JWT robado → sigue accediendo durante esos 15 minutos

Con gestión de sesiones + token rotation:
  Usuario cambia su password → se invalidan todos los refresh tokens
  El access token expira en 15 minutos (corto plazo, tolerable)
  Al intentar refresh → falla porque el refresh token fue revocado
  → Atacante queda bloqueado al expirar el access token
```

---

## Estrategia recomendada

```
Access Token:  corta duración (15 min), stateless (no se revoca individualmente)
Refresh Token: larga duración (30 días), stateful (se almacena y revoca en DB)
Session:       registro de cada par accessToken/refreshToken con metadata del dispositivo
```

---

## Entidad Session

```csharp
// Authentication.Domain/Entities/UserSession.cs
public sealed class UserSession
{
    public long     Id                  { get; init; }
    public Guid     PublicId            { get; init; }
    public long     TenantId            { get; init; }
    public long?    BranchId            { get; init; }
    public Guid     CredentialPublicId  { get; init; }
    public string   RefreshTokenHash    { get; init; } = string.Empty;  // SHA-256 del refresh token
    public string?  DeviceName          { get; init; }   // "Chrome en Windows", "iPhone 15"
    public string?  IpAddress           { get; init; }
    public string?  UserAgent           { get; init; }
    public bool     IsActive            { get; init; }
    public DateTime LastActivityAt      { get; init; }
    public DateTime CreatedAtUtc        { get; init; }
    public DateTime ExpiresAtUtc        { get; init; }
    public DateTime? RevokedAtUtc       { get; init; }
    public string?  RevokedReason       { get; init; }  // "password_changed", "user_revoked", "admin_revoked"
}
```

---

## Registrar la sesión al hacer login

```csharp
// Authentication.Infrastructure/Jwt/JwtTokenService.cs
public TokenDto Generate(
    Guid publicId, string email, string role,
    long tenantId, long? branchId,
    string? ipAddress = null, string? userAgent = null)
{
    // ... generar access token ...

    var refreshToken = Guid.NewGuid().ToString("N");

    // Guardar el hash del refresh token como sesión
    var tokenHash = Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))).ToLower();

    _sessions.InsertAsync(new UserSession
    {
        PublicId            = Guid.NewGuid(),
        TenantId            = tenantId,
        BranchId            = branchId,
        CredentialPublicId  = publicId,
        RefreshTokenHash    = tokenHash,
        DeviceName          = ParseDeviceName(userAgent),
        IpAddress           = ipAddress,
        UserAgent           = userAgent,
        IsActive            = true,
        LastActivityAt      = DateTime.UtcNow,
        ExpiresAtUtc        = DateTime.UtcNow.AddDays(30)
    }, CancellationToken.None).FireAndForget();

    return new TokenDto(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15));
}

private static string ParseDeviceName(string? userAgent)
{
    if (userAgent is null) return "Dispositivo desconocido";

    // Parsing básico del User-Agent
    if (userAgent.Contains("iPhone"))   return "iPhone";
    if (userAgent.Contains("Android"))  return "Android";
    if (userAgent.Contains("Chrome"))   return "Chrome";
    if (userAgent.Contains("Firefox"))  return "Firefox";
    if (userAgent.Contains("Safari"))   return "Safari";
    return "Navegador";
}
```

---

## Refresh Token: validar contra la sesión activa

```csharp
// Authentication.Application/UseCases/RefreshToken/RefreshTokenHandler.cs
public async Task<RefreshTokenResponse> Handle(
    RefreshTokenRequest request, CancellationToken ct)
{
    // 1. Calcular hash del token recibido
    var tokenHash = Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken))).ToLower();

    // 2. Buscar la sesión activa con ese hash
    var session = await _sessions.GetActiveByHashAsync(tokenHash, ct);

    if (session is null)
        return new RefreshTokenInvalidFailure("Refresh token inválido.");

    if (!session.IsActive || session.ExpiresAtUtc < DateTime.UtcNow)
        return new RefreshTokenExpiredFailure("Sesión expirada. Inicia sesión de nuevo.");

    // 3. Token rotation: invalidar el refresh token actual, emitir uno nuevo
    await _sessions.RevokeAsync(session.Id, "rotated", ct);

    var credential = await _credentials.GetByPublicIdAsync(session.CredentialPublicId, ct);
    if (credential is null || !credential.IsActive)
        return new RefreshTokenInvalidFailure("Usuario inactivo.");

    // 4. Emitir nuevos tokens y registrar nueva sesión
    var tokens = _jwt.Generate(
        credential.PublicId, credential.Email,
        credential.Role, credential.TenantId, credential.BranchId,
        ipAddress:  request.IpAddress,
        userAgent:  request.UserAgent);

    // 5. Actualizar LastActivityAt de la sesión anterior
    await _sessions.UpdateLastActivityAsync(session.Id, ct);

    return new RefreshTokenSuccess(tokens);
}
```

---

## Revocar sesiones

### Revocar una sesión específica (usuario)

```csharp
// Authentication.Application/UseCases/RevokeSession/RevokeSessionHandler.cs
public async Task<RevokeSessionResponse> Handle(
    RevokeSessionRequest request, CancellationToken ct)
{
    var session = await _sessions.GetByPublicIdAsync(request.SessionPublicId, ct);

    if (session is null || session.TenantId != request.TenantId)
        return new RevokeSessionNotFoundFailure("Sesión no encontrada.");

    // Solo el dueño de la sesión (o un Admin) puede revocarla
    if (session.CredentialPublicId != request.RequesterPublicId &&
        request.RequesterRole != Roles.Admin)
        return new RevokeSessionForbiddenFailure("No tienes permiso para revocar esta sesión.");

    await _sessions.RevokeAsync(session.Id, "user_revoked", ct);
    return new RevokeSessionSuccess();
}
```

### Revocar todas las sesiones (cambio de password, sospecha de compromiso)

```csharp
// Authentication.Application/UseCases/RevokeAllSessions/RevokeAllSessionsHandler.cs
public async Task<RevokeAllSessionsResponse> Handle(
    RevokeAllSessionsRequest request, CancellationToken ct)
{
    // Revocar todas las sesiones activas del usuario excepto la actual
    await _sessions.RevokeAllExceptAsync(
        credentialPublicId: request.CredentialPublicId,
        exceptSessionPublicId: request.CurrentSessionPublicId,
        reason: request.Reason,
        ct: ct);

    return new RevokeAllSessionsSuccess();
}
```

```csharp
// ChangePasswordHandler.cs — después de cambiar el password, revocar todas las sesiones
await _mediator.Publish(new RevokeAllSessionsForCredentialIntegrationEvent(
    credential.PublicId,
    currentSessionPublicId: request.CurrentSessionPublicId,
    reason: "password_changed"), ct);
```

---

## Listar sesiones activas

```csharp
// SessionDto — lo que el usuario puede ver
public sealed record SessionDto(
    Guid     PublicId,
    string?  DeviceName,
    string?  IpAddress,
    bool     IsCurrentSession,
    DateTime LastActivityAt,
    DateTime CreatedAtUtc);

// Endpoint
[HttpGet("sessions")]
[Authorize]
public async Task<IActionResult> GetSessions(CancellationToken ct)
{
    var currentSessionId = GetCurrentSessionId();  // del claim o header
    _ = await Mediator.Send(new GetSessionsRequest(
        CurrentTenantId,
        CurrentUserPublicId,
        currentSessionId), ct);
    return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
}

[HttpDelete("sessions/{id:guid}")]
[Authorize]
public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
{
    _ = await Mediator.Send(new RevokeSessionRequest(
        id, CurrentTenantId, CurrentUserPublicId, CurrentRole), ct);
    return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
}

[HttpDelete("sessions")]
[Authorize]
public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
{
    _ = await Mediator.Send(new RevokeAllSessionsRequest(
        CurrentUserPublicId, GetCurrentSessionId(), "user_requested"), ct);
    return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
}
```

---

## Session ID en el JWT

Para identificar la sesión actual sin DB lookup en cada request, incluir el session `PublicId` en el JWT:

```csharp
var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, publicId.ToString()),
    new Claim("email",     email),
    new Claim(ClaimTypes.Role, role),
    new Claim("tenant_id", tenantId.ToString()),
    new Claim("session_id", sessionPublicId.ToString()),  // ← ID de la sesión
};
```

El controller extrae el session_id para distinguir la sesión actual al listar:

```csharp
private Guid GetCurrentSessionId() =>
    Guid.TryParse(User.FindFirstValue("session_id"), out var id) ? id : Guid.Empty;
```

---

## Revocación inmediata del access token

El access token de 15 minutos no se puede revocar inmediatamente (es stateless). Para casos de urgencia (cuenta comprometida), se puede invalidar chequeando una denylist en Redis:

```csharp
// JWT validation middleware — verificar denylist
options.Events = new JwtBearerEvents
{
    OnTokenValidated = async context =>
    {
        var jti      = context.Principal!.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var isRevoked = await redis.KeyExistsAsync($"revoked_token:{jti}");

        if (isRevoked)
        {
            context.Fail("Token revocado.");
            return;
        }
    }
};

// Al revocar una sesión con urgencia — agregar el JTI a la denylist
await redis.StringSetAsync(
    $"revoked_token:{jti}",
    "1",
    expiry: TimeSpan.FromMinutes(15));  // mismo TTL que el access token
```

Solo necesario en escenarios de urgencia. La mayoría de los SaaS toleran los 15 minutos de gracia.

---

## Admin del tenant: revocar sesiones de otro usuario

```csharp
[HttpDelete("users/{userId:guid}/sessions")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> RevokeUserSessions(Guid userId, CancellationToken ct)
{
    _ = await Mediator.Send(new AdminRevokeUserSessionsRequest(
        userId, CurrentTenantId, reason: "admin_action"), ct);
    // ...
}
```

Útil cuando un usuario es desvinculado de la empresa. El Admin puede cerrar todas sus sesiones inmediatamente.

---

## Cleanup de sesiones expiradas

```csharp
// Background job semanal
public async Task CleanupExpiredSessionsAsync(CancellationToken ct)
{
    var cutoff = DateTime.UtcNow.AddDays(-30);
    await _db.UserSessions
        .Where(s => !s.IsActive || s.ExpiresAtUtc < cutoff)
        .ExecuteDeleteAsync(ct);
}
```

---

## Checklist

- [ ] Refresh token almacenado como hash SHA-256: nunca en texto claro
- [ ] Token rotation: revocar el refresh token al usarlo, emitir uno nuevo
- [ ] `session_id` incluido en el JWT para identificar la sesión actual
- [ ] Listar sesiones activas con device name e IP: visible para el usuario
- [ ] Revocar sesión individual (usuario) y todas las sesiones (logout global)
- [ ] Al cambiar password: revocar todas las sesiones excepto la actual
- [ ] Admin puede revocar sesiones de usuarios de su tenant
- [ ] Cleanup periódico de sesiones expiradas
- [ ] Denylist en Redis para revocación urgente del access token (casos extremos)

---

## Glosario

| Término | Definición |
|---------|-----------|
| UserSession | Entidad que registra cada sesión activa con DeviceName, IpAddress, RefreshToken y LastActivityAt |
| Refresh Token Rotation | Estrategia donde cada uso del refresh token genera uno nuevo e invalida el anterior para detectar robo |
| Token Rotation | Sinónimo de Refresh Token Rotation — previene el uso de tokens robados que ya fueron utilizados |
| RevokeAllSessions | Operación que invalida todos los refresh tokens del usuario — equivale a logout global |
| session_id | Claim en el JWT que identifica la sesión actual — permite revocar tokens individuales sin invalidar el usuario |
| Redis Denylist | Lista negra de access tokens revocados almacenada en Redis para invalidación urgente antes de su expiración |
| DeviceName | Descripción del dispositivo desde el que se inició la sesión — extraída del User-Agent |
| IpAddress | Dirección IP registrada al crear la sesión — útil para detección de acceso sospechoso |
| LastActivityAt | Timestamp del último uso de la sesión — permite limpiar sesiones inactivas automáticamente |
| Logout Global | Revocación de todas las sesiones del usuario excepto opcionalmente la actual |
| Cleanup Job | Job periódico que elimina sesiones con RefreshToken expirado de la base de datos |

---

*Rogelio Arriaga Gonzalez*
