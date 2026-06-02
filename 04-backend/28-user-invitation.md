# 28 — Invitación de Usuarios al Tenant/Branch

La invitación es el flujo mediante el cual un Admin agrega a otras personas a su tenant (o a un branch específico) sin que esas personas se registren por su cuenta. El Admin genera un link de invitación con un rol y branch pre-asignados; el invitado hace clic, crea su password, y queda habilitado.

---

## El flujo completo

```
Admin envía invitación
    ↓
API crea InvitationToken (email + role + tenantId + branchId + token + expiry)
    ↓
Email con link: https://misaas.com/accept-invite?token=abc123
    ↓
Invitado hace clic → frontend carga formulario con email pre-rellenado (readonly)
    ↓
Invitado escribe su nombre y password
    ↓
API valida token → crea UserCredential + emite Integration Event para UserProfile
    ↓
Login automático con tenant_id + branch_id ya asignados
```

---

## Entidad InvitationToken

```csharp
// Authentication.Domain/Entities/InvitationToken.cs
public sealed class InvitationToken
{
    public long     Id            { get; init; }
    public Guid     PublicId      { get; init; }
    public long     TenantId      { get; init; }
    public long?    BranchId      { get; init; }   // null → sin branch (usuario a nivel tenant)
    public string   Email         { get; init; } = string.Empty;
    public string   Role          { get; init; } = string.Empty;
    public string   Token         { get; init; } = string.Empty;   // GUID criptográfico
    public InvitationStatus Status { get; init; }
    public Guid     InvitedByPublicId { get; init; }   // quién invitó
    public DateTime ExpiresAtUtc  { get; init; }
    public DateTime CreatedAtUtc  { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
}

public enum InvitationStatus
{
    Pending  = 0,   // enviada, no aceptada
    Accepted = 1,   // aceptada exitosamente
    Expired  = 2,   // expiró sin aceptar
    Revoked  = 3    // el Admin la canceló antes de que se aceptara
}
```

---

## Caso de uso: SendInvitation

```csharp
// Authentication.Application/UseCases/SendInvitation/SendInvitationRequest.cs
public sealed record SendInvitationRequest(
    string Email,
    string Role,
    long?  BranchId,          // null = usuario a nivel tenant
    long   TenantId,
    Guid   InvitedByPublicId)
    : IRequest<SendInvitationResponse>;

public abstract record SendInvitationResponse : IResponse;
public sealed record SendInvitationSuccess()         : SendInvitationResponse, ISuccess;
public sealed record SendInvitationConflictFailure(string Message)
    : SendInvitationResponse, IConflictFailure;
public sealed record SendInvitationValidationFailure(string Message)
    : SendInvitationResponse, IValidationFailure;
```

```csharp
// Authentication.Application/UseCases/SendInvitation/SendInvitationHandler.cs
public sealed class SendInvitationHandler
    : IRequestHandler<SendInvitationRequest, SendInvitationResponse>
{
    private readonly IUserCredentialRepository _credentials;
    private readonly IInvitationTokenRepository _invitations;
    private readonly IEmailService             _email;

    public async Task<SendInvitationResponse> Handle(
        SendInvitationRequest request, CancellationToken ct)
    {
        // 1. El email ya tiene cuenta en este tenant → no tiene sentido invitar
        var existing = await _credentials.GetByEmailAndTenantAsync(
            request.Email, request.TenantId, ct);
        if (existing is not null)
            return new SendInvitationConflictFailure(
                $"'{request.Email}' ya tiene cuenta en este tenant.");

        // 2. Ya hay una invitación pendiente para este email en el mismo tenant
        var pendingInvitation = await _invitations.GetPendingByEmailAndTenantAsync(
            request.Email, request.TenantId, ct);
        if (pendingInvitation is not null)
        {
            // Revocar la anterior y emitir una nueva (reenvío)
            await _invitations.RevokeAsync(pendingInvitation.Id, ct);
        }

        // 3. Crear el token de invitación
        var token = Guid.NewGuid().ToString("N");   // 32 chars hex, criptográficamente seguro
        await _invitations.InsertAsync(
            tenantId:          request.TenantId,
            branchId:          request.BranchId,
            email:             request.Email,
            role:              request.Role,
            token:             token,
            invitedByPublicId: request.InvitedByPublicId,
            expiresAt:         DateTime.UtcNow.AddDays(7),
            ct:                ct);

        // 4. Enviar email
        await _email.SendInvitationAsync(request.Email, token, ct);

        return new SendInvitationSuccess();
    }
}
```

---

## Caso de uso: AcceptInvitation

```csharp
// Authentication.Application/UseCases/AcceptInvitation/AcceptInvitationRequest.cs
public sealed record AcceptInvitationRequest(
    string Token,
    string FullName,
    string Password)
    : IRequest<AcceptInvitationResponse>;

public abstract record AcceptInvitationResponse : IResponse;
public sealed record AcceptInvitationSuccess(TokenDto Tokens)
    : AcceptInvitationResponse, ISuccess<TokenDto>;
public sealed record AcceptInvitationInvalidTokenFailure(string Message)
    : AcceptInvitationResponse, IValidationFailure;
public sealed record AcceptInvitationExpiredFailure(string Message)
    : AcceptInvitationResponse, IValidationFailure;
```

```csharp
// Authentication.Application/UseCases/AcceptInvitation/AcceptInvitationHandler.cs
public sealed class AcceptInvitationHandler
    : IRequestHandler<AcceptInvitationRequest, AcceptInvitationResponse>
{
    private readonly IInvitationTokenRepository _invitations;
    private readonly IUserCredentialRepository  _credentials;
    private readonly IJwtTokenService           _jwt;
    private readonly IMediator                  _mediator;

    public async Task<AcceptInvitationResponse> Handle(
        AcceptInvitationRequest request, CancellationToken ct)
    {
        // 1. Validar token
        var invitation = await _invitations.GetPendingByTokenAsync(request.Token, ct);

        if (invitation is null || invitation.Status != InvitationStatus.Pending)
            return new AcceptInvitationInvalidTokenFailure("Token inválido o ya utilizado.");

        if (invitation.ExpiresAtUtc < DateTime.UtcNow)
        {
            await _invitations.MarkAsExpiredAsync(invitation.Id, ct);
            return new AcceptInvitationExpiredFailure("La invitación ha expirado.");
        }

        // 2. Crear credencial con el rol y branch pre-asignados
        var publicId     = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        await _credentials.InsertAsync(
            publicId:     publicId,
            tenantId:     invitation.TenantId,
            branchId:     invitation.BranchId,   // puede ser null
            email:        invitation.Email,
            passwordHash: passwordHash,
            role:         invitation.Role,
            ct:           ct);

        // 3. Crear perfil de usuario
        await _mediator.Publish(new UserShouldBeCreatedIntegrationEvent(
            publicId, invitation.TenantId, request.FullName, invitation.Email), ct);

        // 4. Marcar invitación como aceptada
        await _invitations.MarkAsAcceptedAsync(invitation.Id, ct);

        // 5. Login automático
        var tokens = _jwt.Generate(
            publicId,
            invitation.Email,
            invitation.Role,
            invitation.TenantId,
            invitation.BranchId);

        return new AcceptInvitationSuccess(tokens);
    }
}
```

---

## JWT con branch_id al aceptar la invitación

El token generado al aceptar la invitación incluye el `branch_id` si la invitación lo tenía:

```csharp
// Authentication.Infrastructure/Jwt/JwtTokenService.cs
public TokenDto Generate(Guid publicId, string email, string role,
    long tenantId, long? branchId = null)
{
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, publicId.ToString()),
        new Claim("email",          email),
        new Claim(ClaimTypes.Role,  role),
        new Claim("tenant_id",      tenantId.ToString()),
    };

    if (branchId.HasValue)
        claims.Add(new Claim("branch_id", branchId.Value.ToString()));

    // ... generar JWT ...
}
```

---

## Endpoints

```csharp
// Authentication.Presentation/Controllers/AuthController.cs

// Admin envía invitación
[HttpPost("invitations")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> SendInvitation(
    [FromBody] SendInvitationBody body, CancellationToken ct)
{
    var inviterPublicId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    _ = await Mediator.Send(new SendInvitationRequest(
        body.Email, body.Role, body.BranchId,
        CurrentTenantId, inviterPublicId), ct);
    // ...
}

// Invitado acepta (sin JWT)
[HttpPost("invitations/accept")]
[AllowAnonymous]
public async Task<IActionResult> AcceptInvitation(
    [FromBody] AcceptInvitationBody body, CancellationToken ct)
{
    _ = await Mediator.Send(new AcceptInvitationRequest(
        body.Token, body.FullName, body.Password), ct);
    // ...
}

// Admin consulta invitaciones pendientes del tenant
[HttpGet("invitations")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetInvitations(CancellationToken ct)
{
    _ = await Mediator.Send(new GetInvitationsRequest(CurrentTenantId), ct);
    // ...
}

// Admin revoca una invitación
[HttpDelete("invitations/{id:guid}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> RevokeInvitation(Guid id, CancellationToken ct)
{
    _ = await Mediator.Send(new RevokeInvitationRequest(id, CurrentTenantId), ct);
    // ...
}
```

---

## Invitación con branch_id (tenant + branch)

En un sistema con jerarquía Tenant + Branch, el Admin puede invitar usuarios a un branch específico o al tenant en general (usuarios corporativos sin branch):

```csharp
// SendInvitationBody — body del request
public sealed record SendInvitationBody(
    string Email,
    string Role,
    long?  BranchId);    // null = usuario corporativo, valor = usuario de branch

// Ejemplo: invitar operador a la Sucursal Norte (branch_id = 10)
POST /api/auth/invitations
{
    "email":    "nuevo@empresa.com",
    "role":     "Operator",
    "branchId": 10
}

// Ejemplo: invitar manager corporativo (sin branch)
POST /api/auth/invitations
{
    "email":  "manager@empresa.com",
    "role":   "Manager",
    "branchId": null
}
```

El repositorio de invitaciones filtra por tenant — un Admin no puede ver ni revocar invitaciones de otro tenant:

```csharp
// InvitationTokenRepository — con tenant + branch opcional
public async Task<List<InvitationToken>> GetPendingByTenantAsync(
    long tenantId, long? branchId, CancellationToken ct) =>
    await _db.InvitationTokens
        .AsNoTracking()
        .Where(e => e.TenantId == tenantId
            && (branchId == null || e.BranchId == branchId)
            && e.Status == InvitationStatus.Pending
            && e.ExpiresAtUtc > DateTime.UtcNow)
        .OrderByDescending(e => e.CreatedAtUtc)
        .ToListAsync(ct);
```

---

## Seguridad del token

El token de invitación es un GUID v4 generado con `Guid.NewGuid()`. Consideraciones:

```
Entropía: 122 bits → prácticamente imposible de adivinar por fuerza bruta
Formato:  "N" → 32 chars hexadecimales sin guiones
Expiración: 7 días (configurable)
Un solo uso: se marca como Accepted al primer uso exitoso
Solo Pending: solo funciona si está en estado Pending
```

No enviar el token en la URL como query param — embebido en el link es suficiente:
```
https://app.misaas.com/accept-invite?token=a1b2c3d4e5f6...
```

El frontend pre-rellena el email desde la invitación (readonly) para que el usuario no pueda cambiar a qué email se vincula la cuenta.

---

## Email de invitación

```csharp
// Infrastructure/Email/EmailService.cs
public async Task SendInvitationAsync(string toEmail, string token, CancellationToken ct)
{
    var acceptUrl = $"https://app.misaas.com/accept-invite?token={token}";
    await _emailProvider.SendAsync(
        to:      toEmail,
        subject: "Te han invitado a MiSaaS",
        body:    $"""
            <h1>Te han invitado</h1>
            <p>Haz clic para aceptar tu invitación (válida por 7 días):</p>
            <a href="{acceptUrl}">Aceptar invitación</a>
            """,
        ct);
}
```

---

## Expiración automática

Un Background Service revisa y marca como expiradas las invitaciones vencidas:

```csharp
// Authentication.Infrastructure/BackgroundJobs/InvitationExpirationJob.cs
public sealed class InvitationExpirationJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IInvitationTokenRepository>();
            var expired = await repo.GetExpiredPendingAsync(ct);
            foreach (var inv in expired)
                await repo.MarkAsExpiredAsync(inv.Id, ct);

            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }
}
```

---

## Checklist

- [ ] El token es un GUID v4 de un solo uso
- [ ] La invitación expira (7 días por defecto — configurable)
- [ ] Solo el Admin del tenant puede enviar invitaciones
- [ ] El email pre-asignado no se puede cambiar al aceptar
- [ ] El rol y branch_id vienen de la invitación, no del body del request de aceptación
- [ ] Un reenvío revoca la invitación anterior y crea una nueva
- [ ] El Admin puede revocar invitaciones pendientes
- [ ] `AcceptInvitation` es `[AllowAnonymous]` — no requiere JWT
- [ ] El login automático incluye `branch_id` si la invitación lo tenía

---

## Relación con el back-template

Para agregar invitaciones al back-template:

1. `Authentication.Domain/Entities/InvitationToken.cs` — entidad
2. `Shared/Database/EntityTypeConfigurations/InvitationTokenConfiguration.cs` — config EF Core
3. `AppDbContext` — agregar `DbSet<InvitationToken>`
4. `Authentication.Application/UseCases/SendInvitation/` — caso de uso
5. `Authentication.Application/UseCases/AcceptInvitation/` — caso de uso
6. `Authentication.Presentation/Controllers/AuthController.cs` — endpoints
7. `IJwtTokenService.Generate` — extender para aceptar `branchId` opcional

Ver `04-backend/27-rbac.md` para el modelo de roles asignados en la invitación.
Ver `04-backend/37-emails-transaccionales.md` para el envío del email de invitación.

---

## Glosario

| Término | Definición |
|---------|-----------|
| InvitationToken | Token único criptográficamente seguro enviado al invitado por email para aceptar la invitación |
| InvitationStatus | Estado de la invitación: Pending, Accepted, Revoked, Expired — controla la validez del token |
| SendInvitation | Caso de uso que crea el registro de invitación, genera el token y envía el email al destinatario |
| AcceptInvitation | Caso de uso que valida el token, crea el usuario y le asigna el rol y branch definidos en la invitación |
| One-time Use | Propiedad del InvitationToken: una vez aceptado se invalida inmediatamente para evitar reutilización |
| JIT Provisioning via Invitation | Creación del usuario en el momento de aceptar la invitación, sin pre-registro previo |
| BranchId Pre-assignment | Opción de asociar al invitado a una branch específica antes de que cree su cuenta |
| Reenvío de invitación | Operación que genera un nuevo token e invalida el anterior — útil cuando el email expiró |
| Revocación | Cancelación de una invitación pendiente por el Admin — cambia estado a Revoked |
| ExpiresAt | Timestamp de expiración del InvitationToken — típicamente 7 días desde la creación |
| InvitedBy | FK del usuario Admin que creó la invitación — auditoría de quién invitó a quién |

---

*Rogelio Arriaga Gonzalez*
