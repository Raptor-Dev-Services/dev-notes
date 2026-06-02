# 26 — Tenant Onboarding: Registro de Empresa Nueva

El onboarding de un tenant es el flujo que convierte a una empresa desconocida en un tenant activo con su primer usuario Admin. Es el "día cero" del tenant — el momento en que el SaaS adquiere un nuevo cliente.

---

## El flujo completo

```
1. Empresa llena el formulario de registro (nombre empresa, email admin, password)
2. API crea el Tenant + Credencial Admin en una transacción
3. Se publica UserShouldBeCreatedIntegrationEvent → crea UserProfile
4. Login automático: retorna accessToken + refreshToken
5. (Opcional) Email de bienvenida con instrucciones

Variante con verificación de email:
2b. Se crea el tenant en estado Pending
3b. Se envía email con token de verificación
4b. Admin hace clic → API activa el tenant + hace login automático
```

---

## Entidades involucradas

### Tenant

```csharp
// Tenancy.Domain/Entities/Tenant.cs
public sealed class Tenant
{
    public long      Id           { get; init; }
    public Guid      PublicId     { get; init; }
    public string    Name         { get; init; } = string.Empty;
    public string    Slug         { get; init; } = string.Empty;  // alfacorp → alfacorp.misaas.com
    public TenantStatus Status    { get; init; }
    public string    Plan         { get; init; } = "trial";       // trial, free, pro, enterprise
    public DateTime  CreatedAtUtc { get; init; }
    public DateTime? TrialEndsAt  { get; init; }
}

public enum TenantStatus { Pending, Active, Suspended, Deleted }
```

### UserCredential (existente, extender)

```csharp
// Authentication.Domain/Entities/UserCredential.cs
public sealed class UserCredential
{
    public long   Id           { get; init; }
    public Guid   PublicId     { get; init; }
    public long   TenantId     { get; init; }
    public string Email        { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string Role         { get; init; } = string.Empty;   // "Admin" para el primer usuario
    public bool   IsActive     { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

---

## Caso de uso: RegisterTenant

### Request y Responses

```csharp
// Authentication.Contracts/Dtos/RegisterTenantBody.cs
public sealed record RegisterTenantBody(
    string TenantName,
    string TenantSlug,
    string AdminEmail,
    string AdminPassword,
    string AdminFullName);

// Authentication.Application/UseCases/RegisterTenant/RegisterTenantRequest.cs
public sealed record RegisterTenantRequest(
    string TenantName,
    string TenantSlug,
    string AdminEmail,
    string AdminPassword,
    string AdminFullName)
    : IRequest<RegisterTenantResponse>;

// Authentication.Application/UseCases/RegisterTenant/Responses/
public abstract record RegisterTenantResponse : IResponse;

public sealed record RegisterTenantSuccess(TokenDto Tokens)
    : RegisterTenantResponse, ISuccess<TokenDto>;

public sealed record RegisterTenantSlugConflictFailure(string Message)
    : RegisterTenantResponse, IConflictFailure;

public sealed record RegisterTenantEmailConflictFailure(string Message)
    : RegisterTenantResponse, IConflictFailure;

public sealed record RegisterTenantValidationFailure(string Message)
    : RegisterTenantResponse, IValidationFailure;
```

### Handler

```csharp
// Authentication.Application/UseCases/RegisterTenant/RegisterTenantHandler.cs
public sealed class RegisterTenantHandler
    : IRequestHandler<RegisterTenantRequest, RegisterTenantResponse>
{
    private readonly ITenantRepository      _tenants;
    private readonly IUserCredentialRepository _credentials;
    private readonly IJwtTokenService       _jwt;
    private readonly IMediator              _mediator;

    public RegisterTenantHandler(
        ITenantRepository tenants,
        IUserCredentialRepository credentials,
        IJwtTokenService jwt,
        IMediator mediator)
    {
        _tenants     = tenants;
        _credentials = credentials;
        _jwt         = jwt;
        _mediator    = mediator;
    }

    public async Task<RegisterTenantResponse> Handle(
        RegisterTenantRequest request, CancellationToken ct)
    {
        // 1. Validaciones de negocio
        if (await _tenants.SlugExistsAsync(request.TenantSlug, ct))
            return new RegisterTenantSlugConflictFailure(
                $"El slug '{request.TenantSlug}' ya está en uso.");

        if (await _credentials.EmailExistsAsync(request.AdminEmail, ct))
            return new RegisterTenantEmailConflictFailure(
                $"El email '{request.AdminEmail}' ya está registrado.");

        // 2. Crear Tenant
        var tenantId = await _tenants.InsertAsync(
            name:    request.TenantName,
            slug:    request.TenantSlug,
            plan:    "trial",
            trialEndsAt: DateTime.UtcNow.AddDays(14),
            ct:      ct);

        // 3. Crear credencial Admin
        var adminPublicId = Guid.NewGuid();
        var passwordHash  = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword, workFactor: 12);

        await _credentials.InsertAsync(
            publicId:     adminPublicId,
            tenantId:     tenantId,
            email:        request.AdminEmail,
            passwordHash: passwordHash,
            role:         "Admin",
            ct:           ct);

        // 4. Crear perfil de usuario vía Integration Event
        await _mediator.Publish(new UserShouldBeCreatedIntegrationEvent(
            adminPublicId, tenantId, request.AdminFullName, request.AdminEmail), ct);

        // 5. Emitir tokens (login automático)
        var tokens = _jwt.Generate(adminPublicId, request.AdminEmail, "Admin", tenantId);

        return new RegisterTenantSuccess(tokens);
    }
}
```

---

## Transacción atómica — Tenant + Credential

El Tenant y la Credential deben crearse en una sola transacción. Si la credencial falla, el tenant no debe quedar huérfano:

```csharp
// Alternativa con transacción explícita en el handler
public async Task<RegisterTenantResponse> Handle(
    RegisterTenantRequest request, CancellationToken ct)
{
    // ... validaciones ...

    await using var transaction = await _db.Database.BeginTransactionAsync(ct);
    try
    {
        var tenantId = await _tenants.InsertAsync(..., ct);
        await _credentials.InsertAsync(..., ct);
        await transaction.CommitAsync(ct);
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;
    }

    // Integration event DESPUÉS del commit — si el evento falla, el tenant ya existe
    // y se puede reenviar. Si se publica antes del commit y el commit falla → inconsistencia.
    await _mediator.Publish(new UserShouldBeCreatedIntegrationEvent(...), ct);

    var tokens = _jwt.Generate(...);
    return new RegisterTenantSuccess(tokens);
}
```

---

## Slug del tenant

El slug es el identificador amigable del tenant. Se usa en:
- Subdominio: `alfacorp.misaas.com`
- URLs de recursos: `/api/tenants/alfacorp/settings`
- Identificación en logs

### Reglas de slug

```csharp
// Authentication.Application/UseCases/RegisterTenant/SlugValidator.cs
public static class SlugValidator
{
    private static readonly Regex _valid = new(@"^[a-z0-9][a-z0-9\-]{2,30}[a-z0-9]$",
        RegexOptions.Compiled);

    public static bool IsValid(string slug) => _valid.IsMatch(slug);

    // "Alfa Corp S.A." → "alfa-corp-sa"
    public static string Normalize(string name) =>
        Regex.Replace(
            name.ToLowerInvariant()
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
                .ToString()
                .Replace(' ', '-'),
            @"[^a-z0-9\-]", "")
        .Trim('-');
}
```

---

## Verificación de email (variante)

Si el producto requiere verificar el email antes de activar el tenant:

```csharp
// Authentication.Domain/Entities/EmailVerificationToken.cs
public sealed class EmailVerificationToken
{
    public long     Id          { get; init; }
    public long     TenantId    { get; init; }
    public Guid     CredentialPublicId { get; init; }
    public string   Token       { get; init; } = string.Empty;  // GUID seguro
    public bool     IsUsed      { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

```csharp
// Handler — crea tenant en estado Pending, emite token
var verificationToken = Guid.NewGuid().ToString("N");
await _verificationTokens.InsertAsync(tenantId, adminPublicId, verificationToken,
    expiresAt: DateTime.UtcNow.AddHours(24), ct);

// Enviar email con link: https://misaas.com/verify?token=abc123
await _emailService.SendWelcomeEmailAsync(request.AdminEmail, verificationToken, ct);

return new RegisterTenantPendingVerification("Revisa tu email para activar tu cuenta.");
```

```csharp
// VerifyEmailHandler — activa el tenant
public async Task<VerifyEmailResponse> Handle(VerifyEmailRequest request, CancellationToken ct)
{
    var token = await _verificationTokens.GetByTokenAsync(request.Token, ct);

    if (token is null || token.IsUsed)
        return new VerifyEmailInvalidTokenFailure("Token inválido o ya utilizado.");

    if (token.ExpiresAtUtc < DateTime.UtcNow)
        return new VerifyEmailExpiredTokenFailure("El token ha expirado.");

    await _tenants.ActivateAsync(token.TenantId, ct);
    await _verificationTokens.MarkAsUsedAsync(token.Id, ct);

    // Login automático
    var credential = await _credentials.GetByPublicIdAsync(token.CredentialPublicId, ct);
    var tokens = _jwt.Generate(credential!.PublicId, credential.Email,
        credential.Role, credential.TenantId);

    return new VerifyEmailSuccess(tokens);
}
```

---

## Endpoint de registro

```csharp
// Authentication.Presentation/Controllers/AuthController.cs
[HttpPost("register-tenant")]
[AllowAnonymous]
public async Task<IActionResult> RegisterTenant(
    [FromBody] RegisterTenantBody body, CancellationToken ct)
{
    try
    {
        _ = await Mediator.Send(new RegisterTenantRequest(
            body.TenantName, body.TenantSlug,
            body.AdminEmail, body.AdminPassword, body.AdminFullName), ct);

        if (_viewModel.IsSuccess) return Ok(_viewModel);

        return _viewModel.StatusCode switch
        {
            409 => Conflict(_viewModel),
            400 => BadRequest(_viewModel),
            _   => StatusCode(500, _viewModel)
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en RegisterTenant");
        return StatusCode(500, _viewModel.Fail(ex.Message));
    }
}
```

---

## Validación del body

```csharp
// Authentication.Application/UseCases/RegisterTenant/RegisterTenantValidator.cs
public static class RegisterTenantValidator
{
    public static RegisterTenantValidationFailure? Validate(RegisterTenantRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.TenantName) || r.TenantName.Length > 100)
            return new RegisterTenantValidationFailure("Nombre de empresa requerido (máx 100 chars).");

        if (!SlugValidator.IsValid(r.TenantSlug))
            return new RegisterTenantValidationFailure(
                "Slug inválido. Solo minúsculas, números y guiones (4-32 chars).");

        if (!IsValidEmail(r.AdminEmail))
            return new RegisterTenantValidationFailure("Email inválido.");

        if (r.AdminPassword.Length < 8)
            return new RegisterTenantValidationFailure("Password mínimo 8 caracteres.");

        if (string.IsNullOrWhiteSpace(r.AdminFullName))
            return new RegisterTenantValidationFailure("Nombre completo requerido.");

        return null;
    }

    private static bool IsValidEmail(string email) =>
        email.Contains('@') && email.Contains('.') && email.Length <= 320;
}
```

---

## Estado del tenant — máquina de estados

```
Pending ──verificación email──→ Active
Active  ──pago fallido──→ PastDue ──grace period──→ Suspended
Active  ──admin suspende──→ Suspended
Suspended ──admin reactiva──→ Active
Suspended ──90 días──→ Deleted (soft delete + purge scheduled)
```

```csharp
public enum TenantStatus
{
    Pending    = 0,   // Esperando verificación de email
    Active     = 1,   // Operacional
    PastDue    = 2,   // Pago vencido, grace period activo
    Suspended  = 3,   // Sin acceso, datos retenidos
    Deleted    = 4    // Soft delete, datos en cola de purga
}
```

---

## Checklist de onboarding completo

- [ ] `RegisterTenant` crea Tenant + Credential en transacción atómica
- [ ] Integration Event crea UserProfile después del commit
- [ ] Tenant empieza en `Active` (sin verificación) o `Pending` (con verificación)
- [ ] Trial period configurado al crear (ej. 14 días)
- [ ] Slug validado y único — normalización automática desde el nombre
- [ ] Email verificado antes de hacer login automático (variante)
- [ ] Email de bienvenida enviado con primeros pasos
- [ ] Endpoint `[AllowAnonymous]` — no requiere JWT
- [ ] Primer usuario siempre recibe rol `Admin`

---

## Relación con el back-template

El back-template tiene `Register` (usuario en tenant existente) pero no `RegisterTenant` (crear empresa nueva). Para agregar onboarding:

1. `Tenancy.Domain/Entities/Tenant.cs` — agregar `Status`, `Slug`, `Plan`, `TrialEndsAt`
2. `Tenancy.Infrastructure/Repositories/TenantRepository.cs` — `InsertAsync`, `SlugExistsAsync`, `ActivateAsync`
3. `Authentication.Application/UseCases/RegisterTenant/` — handler completo
4. `Authentication.Presentation/Controllers/AuthController.cs` — endpoint `POST /api/auth/register-tenant`
5. `Authentication.Contracts/Events/TenantRegisteredIntegrationEvent.cs` — para notificar a otros módulos

Ver `04-backend/27-rbac.md` para los roles del primer usuario Admin.
Ver `04-backend/32-stripe-billing.md` para conectar el onboarding con el cobro.

---

*Rogelio Arriaga Gonzalez*
