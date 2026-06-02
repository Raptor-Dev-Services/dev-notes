# 37 — Emails Transaccionales

Los emails transaccionales son los emails que el sistema envía automáticamente en respuesta a acciones del usuario: bienvenida al registrarse, invitación a colaboradores, verificación de cuenta, restablecimiento de contraseña, notificación de pago fallido. Son distintos a los emails de marketing (campañas, boletines).

---

## Tipos de emails transaccionales en un SaaS

```
Onboarding:
├── Bienvenida al registrar el tenant
├── Verificación de email
└── Primeros pasos / guía de inicio

Gestión de usuarios:
├── Invitación a un colaborador
├── Confirmación de aceptación de invitación
└── Cambio de rol notificado al usuario

Seguridad:
├── Cambio de password
├── Intento de login desde nuevo dispositivo
└── Código de verificación 2FA

Facturación:
├── Confirmación de suscripción
├── Pago exitoso (recibo)
├── Pago fallido (con link de actualización)
├── Trial próximo a vencer
└── Suscripción cancelada

Operacionales:
├── Límite de uso al 80%
├── Límite alcanzado
└── Exportación lista para descargar
```

---

## IEmailService — abstracción del proveedor

```csharp
// Common/Email/IEmailService.cs
public interface IEmailService
{
    Task SendWelcomeAsync(
        string toEmail, string tenantName, string adminName,
        CancellationToken ct = default);

    Task SendVerificationEmailAsync(
        string toEmail, string verificationToken,
        CancellationToken ct = default);

    Task SendInvitationAsync(
        string toEmail, string inviterName, string tenantName,
        string invitationToken, string role,
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string toEmail, string resetToken,
        CancellationToken ct = default);

    Task SendPaymentFailedAsync(
        string toEmail, string tenantName, long amountCents, string currency,
        CancellationToken ct = default);

    Task SendTrialEndingAsync(
        string toEmail, string tenantName, int daysLeft,
        CancellationToken ct = default);

    Task SendLimitWarningAsync(
        string toEmail, string tenantName,
        string resourceName, int current, int limit,
        CancellationToken ct = default);
}
```

---

## Proveedores recomendados

| Proveedor | Free tier | Puntos fuertes |
|-----------|-----------|----------------|
| Resend | 3,000/mes | API moderna, SDK .NET oficial |
| SendGrid | 100/día | El más usado, templates visuales |
| Postmark | 100/mes | Excelente entregabilidad, logs detallados |
| AWS SES | 62,000/mes (desde EC2) | Más barato en volumen |
| Mailgun | 5,000/mes (3 meses) | API sencilla |

**Recomendación para empezar:** Resend o Postmark — mejor DX y entregabilidad.

---

## Implementación con Resend

```csharp
// Infrastructure/Email/ResendEmailService.cs
public sealed class ResendEmailService : IEmailService
{
    private readonly ResendClient _client;
    private readonly string       _fromAddress;
    private readonly string       _appBaseUrl;

    public ResendEmailService(IConfiguration config)
    {
        _client      = new ResendClient(config["Resend:ApiKey"]!);
        _fromAddress = config["Email:FromAddress"] ?? "noreply@misaas.com";
        _appBaseUrl  = config["App:BaseUrl"] ?? "https://app.misaas.com";
    }

    public async Task SendWelcomeAsync(
        string toEmail, string tenantName, string adminName, CancellationToken ct)
    {
        var email = new EmailMessage
        {
            From    = _fromAddress,
            To      = [toEmail],
            Subject = $"Bienvenido a MiSaaS, {adminName}",
            HtmlBody = BuildWelcomeHtml(adminName, tenantName)
        };
        await _client.Emails.SendAsync(email, ct);
    }

    public async Task SendInvitationAsync(
        string toEmail, string inviterName, string tenantName,
        string invitationToken, string role, CancellationToken ct)
    {
        var acceptUrl = $"{_appBaseUrl}/accept-invite?token={invitationToken}";
        var email = new EmailMessage
        {
            From    = _fromAddress,
            To      = [toEmail],
            Subject = $"{inviterName} te invitó a {tenantName}",
            HtmlBody = BuildInvitationHtml(inviterName, tenantName, role, acceptUrl)
        };
        await _client.Emails.SendAsync(email, ct);
    }

    public async Task SendPasswordResetAsync(
        string toEmail, string resetToken, CancellationToken ct)
    {
        var resetUrl = $"{_appBaseUrl}/reset-password?token={resetToken}";
        var email = new EmailMessage
        {
            From    = _fromAddress,
            To      = [toEmail],
            Subject = "Restablece tu contraseña",
            HtmlBody = BuildPasswordResetHtml(resetUrl)
        };
        await _client.Emails.SendAsync(email, ct);
    }
}
```

---

## Templates HTML — mantenerlos simples

El HTML de emails debe ser compatible con clientes de email viejos (Outlook, Gmail app). Usar tablas en lugar de CSS moderno:

```csharp
private static string BuildWelcomeHtml(string name, string tenantName) => $"""
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="utf-8">
      <style>
        body {{ font-family: -apple-system, Arial, sans-serif; background: #f4f4f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: #fff; border-radius: 8px; padding: 40px; }}
        .btn {{ display: inline-block; background: #6366f1; color: #fff; padding: 12px 24px; border-radius: 6px; text-decoration: none; }}
        .footer {{ color: #9ca3af; font-size: 12px; margin-top: 32px; }}
      </style>
    </head>
    <body>
      <div class="container">
        <h1 style="color:#1f2937">Bienvenido, {name}</h1>
        <p>Tu cuenta de <strong>{tenantName}</strong> en MiSaaS está lista.</p>
        <p>
          <a href="https://app.misaas.com/dashboard" class="btn">Ir al dashboard</a>
        </p>
        <div class="footer">
          MiSaaS · Unsubscribe de notificaciones operacionales no es posible.<br>
          Recibiste este email porque registraste una cuenta en misaas.com.
        </div>
      </div>
    </body>
    </html>
    """;
```

---

## Branded por tenant — personalización

Los emails pueden incluir el logo y el nombre del tenant en el from:

```csharp
// Emails de invitación y operacionales se envían "en nombre del tenant"
public async Task SendInvitationAsync(
    string toEmail, string inviterName, string tenantName,
    string invitationToken, string role,
    string? tenantLogoUrl,        // ← logo del tenant en el email
    string? tenantPrimaryColor,   // ← color del botón
    CancellationToken ct)
{
    var email = new EmailMessage
    {
        From    = $"{tenantName} via MiSaaS <invites@misaas.com>",
        ReplyTo = [inviterEmail],   // replies van al admin del tenant
        To      = [toEmail],
        // ...
    };
}
```

---

## Restablecimiento de contraseña — flujo seguro

```csharp
// Authentication.Domain/Entities/PasswordResetToken.cs
public sealed class PasswordResetToken
{
    public long     Id            { get; init; }
    public long     TenantId      { get; init; }
    public Guid     CredentialPublicId { get; init; }
    public string   Token         { get; init; } = string.Empty;   // GUID criptográfico
    public bool     IsUsed        { get; init; }
    public DateTime ExpiresAtUtc  { get; init; }
    public DateTime CreatedAtUtc  { get; init; }
}
```

```csharp
// ForgotPasswordHandler.cs
public async Task<ForgotPasswordResponse> Handle(
    ForgotPasswordRequest request, CancellationToken ct)
{
    // Siempre retornar éxito — no revelar si el email existe
    var credential = await _credentials.GetByEmailAsync(request.Email, ct);
    if (credential is null)
        return new ForgotPasswordSuccess();   // mismo response que si existe

    // Solo permitir un token activo por email
    await _passwordResetTokens.RevokeAllByCredentialAsync(credential.PublicId, ct);

    var token = Guid.NewGuid().ToString("N");
    await _passwordResetTokens.InsertAsync(
        credential.TenantId, credential.PublicId, token,
        expiresAt: DateTime.UtcNow.AddMinutes(30), ct);

    await _email.SendPasswordResetAsync(credential.Email, token, ct);

    return new ForgotPasswordSuccess();
}
```

```csharp
// ResetPasswordHandler.cs
public async Task<ResetPasswordResponse> Handle(
    ResetPasswordRequest request, CancellationToken ct)
{
    var resetToken = await _passwordResetTokens.GetPendingByTokenAsync(request.Token, ct);

    if (resetToken is null || resetToken.IsUsed)
        return new ResetPasswordInvalidTokenFailure("Token inválido.");

    if (resetToken.ExpiresAtUtc < DateTime.UtcNow)
        return new ResetPasswordExpiredFailure("El link expiró. Solicita uno nuevo.");

    var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
    await _credentials.UpdatePasswordAsync(resetToken.CredentialPublicId, newHash, ct);
    await _passwordResetTokens.MarkAsUsedAsync(resetToken.Id, ct);

    return new ResetPasswordSuccess();
}
```

---

## Emails con retry y fallback

El email no es transaccional en el sentido de la DB — si falla, el tenant no se entera. Agregar reintentos:

```csharp
// Wrapper con reintento (sin usar Polly por simplicidad)
public sealed class RetryEmailService : IEmailService
{
    private readonly IEmailService _inner;
    private readonly ILogger<RetryEmailService> _logger;

    public async Task SendWelcomeAsync(string toEmail, string tenantName,
        string adminName, CancellationToken ct)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await _inner.SendWelcomeAsync(toEmail, tenantName, adminName, ct);
                return;
            }
            catch (Exception ex) when (attempt < 3)
            {
                _logger.LogWarning(ex,
                    "Email send failed, attempt {Attempt}/3. To={Email}", attempt, toEmail);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
            }
        }
    }
}
```

O enqueue en un background job para desacoplar el envío del request:

```csharp
// En el handler: encolar en lugar de enviar directamente
await _emailQueue.EnqueueAsync(new EmailMessage
{
    Type     = EmailType.Welcome,
    ToEmail  = adminEmail,
    TenantId = tenantId,
    Data     = new { adminName, tenantName }
}, ct);

// Background service lee la cola y envía con reintentos
```

---

## Registro de emails enviados (log)

```csharp
// EmailLog — para diagnóstico y evitar duplicados
public sealed class EmailLog
{
    public long     Id           { get; init; }
    public long?    TenantId     { get; init; }
    public string   Type         { get; init; } = string.Empty;   // "welcome", "invitation"
    public string   ToEmail      { get; init; } = string.Empty;
    public string?  ExternalId   { get; init; }   // ID del proveedor (Resend, SendGrid)
    public bool     Delivered    { get; init; }
    public string?  ErrorMessage { get; init; }
    public DateTime SentAtUtc    { get; init; }
}
```

---

## Configuración

```json
{
  "Email": {
    "FromAddress": "noreply@misaas.com",
    "FromName": "MiSaaS"
  },
  "Resend": {
    "ApiKey": ""
  },
  "App": {
    "BaseUrl": "https://app.misaas.com"
  }
}
```

**La API key nunca en git.** Usar `dotnet user-secrets` o variable de entorno:

```
Resend__ApiKey=re_live_xxxx
```

---

## Checklist

- [ ] Todos los tokens de email (invitación, verificación, reset) son GUIDs de un solo uso con expiración
- [ ] `ForgotPassword` siempre retorna éxito — no revelar si el email existe
- [ ] Emails branded con nombre del tenant en el From y logo en el HTML
- [ ] HTML compatible con Outlook (tablas, inline styles)
- [ ] Retry automático en fallo del proveedor (3 intentos)
- [ ] Log de emails enviados para diagnóstico
- [ ] API key del proveedor en variables de entorno — nunca en código/git
- [ ] Unsubscribe solo aplica a emails de marketing — los transaccionales no tienen unsubscribe
- [ ] Emails de seguridad (reset, cambio de password) se envían sin importar las preferencias de notificación

---

*Rogelio Arriaga Gonzalez*
