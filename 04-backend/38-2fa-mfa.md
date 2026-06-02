# 38 — 2FA / MFA: Autenticación de Dos Factores

La autenticación de dos factores (2FA) agrega una segunda capa de verificación después del password. Incluso si un atacante obtiene la contraseña de un usuario, necesita también el segundo factor para acceder. Es el estándar mínimo de seguridad en cualquier SaaS con datos sensibles.

---

## Factores de autenticación

```
Factor 1 — Algo que sabes    → contraseña
Factor 2 — Algo que tienes   → TOTP app (Google Authenticator, Authy)
                               o código de email (OTP de 6 dígitos)
                               o llave de hardware (YubiKey)
Factor 3 — Algo que eres     → biométrico (fuera del scope de un SaaS típico)
```

**Estrategia recomendada para un SaaS:**
- TOTP (app autenticadora) como método principal — sin costo, sin dependencia de SMS
- Email OTP como fallback — cuando el usuario perdió acceso a su app

---

## TOTP — Time-based One-Time Password

TOTP genera un código de 6 dígitos que cambia cada 30 segundos. El algoritmo (RFC 6238) usa un secreto compartido entre el servidor y la app del usuario. No requiere conectividad.

```
Secreto compartido (base32): JBSWY3DPEHPK3PXP
Tiempo actual (epoch / 30): 1234567

HOTP(secreto, contador) → 6 dígitos
```

### Librería: OtpNet

```xml
<!-- Authentication.Infrastructure.csproj -->
<PackageReference Include="OtpNet" Version="1.4.0" />
```

---

## Entidades

```csharp
// Authentication.Domain/Entities/UserMfaConfig.cs
public sealed class UserMfaConfig
{
    public long     Id                  { get; init; }
    public long     TenantId            { get; init; }
    public Guid     CredentialPublicId  { get; init; }
    public string   TotpSecretEncrypted { get; init; } = string.Empty;  // AES cifrado
    public MfaMethod Method             { get; init; }
    public bool     IsEnabled           { get; init; }
    public DateTime? EnabledAtUtc       { get; init; }
    public DateTime CreatedAtUtc        { get; init; }
}

public enum MfaMethod
{
    Totp  = 1,  // app autenticadora
    Email = 2,  // código por email
}

// Recovery codes — para cuando el usuario pierde acceso al 2FA
public sealed class MfaRecoveryCode
{
    public long     Id                 { get; init; }
    public long     TenantId           { get; init; }
    public Guid     CredentialPublicId { get; init; }
    public string   CodeHash           { get; init; } = string.Empty;  // SHA-256
    public bool     IsUsed             { get; init; }
    public DateTime? UsedAtUtc         { get; init; }
    public DateTime CreatedAtUtc       { get; init; }
}
```

---

## Flujo de setup de TOTP

```
1. Usuario solicita setup de 2FA
2. API genera secreto TOTP → retorna URI para QR code
3. Usuario escanea QR con Google Authenticator / Authy
4. Usuario envía el primer código de 6 dígitos para confirmar
5. API verifica el código → activa 2FA y genera recovery codes
6. Usuario guarda los recovery codes en lugar seguro
```

### Generar el secreto y la URI del QR

```csharp
// Authentication.Application/UseCases/SetupMfa/SetupMfaHandler.cs
public sealed class SetupMfaHandler : IRequestHandler<SetupMfaRequest, SetupMfaResponse>
{
    private readonly IUserMfaConfigRepository _mfaConfigs;
    private readonly IEncryptionService       _encryption;

    public async Task<SetupMfaResponse> Handle(
        SetupMfaRequest request, CancellationToken ct)
    {
        // 1. Generar secreto aleatorio de 20 bytes (160 bits)
        var secretBytes = new byte[20];
        RandomNumberGenerator.Fill(secretBytes);
        var secretBase32 = Base32Encoding.ToString(secretBytes);

        // 2. Guardar secreto cifrado (nunca en texto claro en la DB)
        var secretEncrypted = _encryption.Encrypt(secretBase32);
        await _mfaConfigs.UpsertPendingAsync(
            request.TenantId,
            request.CredentialPublicId,
            secretEncrypted,
            MfaMethod.Totp,
            ct);

        // 3. Generar URI para el QR code
        // formato: otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}
        var qrUri = $"otpauth://totp/MiSaaS:{Uri.EscapeDataString(request.UserEmail)}" +
                    $"?secret={secretBase32}&issuer=MiSaaS&algorithm=SHA1&digits=6&period=30";

        return new SetupMfaSuccess(qrUri, secretBase32);
        // El frontend genera el QR code a partir de la URI con una librería como qrcode.js
    }
}
```

### Confirmar el setup (verificar el primer código)

```csharp
// Authentication.Application/UseCases/ConfirmMfaSetup/ConfirmMfaSetupHandler.cs
public async Task<ConfirmMfaSetupResponse> Handle(
    ConfirmMfaSetupRequest request, CancellationToken ct)
{
    var config = await _mfaConfigs.GetPendingByCredentialAsync(request.CredentialPublicId, ct);
    if (config is null)
        return new ConfirmMfaSetupNotFoundFailure("No hay setup de MFA pendiente.");

    var secretBase32 = _encryption.Decrypt(config.TotpSecretEncrypted);
    var secretBytes  = Base32Encoding.ToBytes(secretBase32);

    var totp = new Totp(secretBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);

    // Verificar con ventana de ±1 período (30 seg de tolerancia por desfase de reloj)
    var isValid = totp.VerifyTotp(
        request.Code,
        out _,
        new VerificationWindow(previous: 1, future: 1));

    if (!isValid)
        return new ConfirmMfaSetupInvalidCodeFailure("Código incorrecto.");

    // Activar 2FA
    await _mfaConfigs.ActivateAsync(config.Id, ct);

    // Generar 10 recovery codes
    var recoveryCodes = GenerateRecoveryCodes(10);
    await _recoveryCodes.InsertAsync(
        request.TenantId, request.CredentialPublicId, recoveryCodes, ct);

    return new ConfirmMfaSetupSuccess(recoveryCodes.Select(c => c.Plain).ToList());
}

private static List<(string Plain, string Hash)> GenerateRecoveryCodes(int count)
{
    return Enumerable.Range(0, count).Select(_ =>
    {
        var bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        var plain = $"{Convert.ToHexString(bytes[..4]).ToLower()}-{Convert.ToHexString(bytes[4..]).ToLower()}";
        var hash  = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plain))).ToLower();
        return (plain, hash);
    }).ToList();
}
```

---

## Flujo de login con 2FA activo

```
Paso 1: POST /api/auth/login
  { email, password }
  → Si 2FA no activo: retorna accessToken + refreshToken (flujo actual)
  → Si 2FA activo: retorna mfaToken (temporal, 5 min) y requires2FA: true

Paso 2: POST /api/auth/login/verify-2fa
  { mfaToken, code }    ← code puede ser TOTP o recovery code
  → Si correcto: retorna accessToken + refreshToken definitivos
```

### Token temporal de MFA (MfaPendingToken)

```csharp
// Authentication.Domain/Entities/MfaPendingToken.cs
public sealed class MfaPendingToken
{
    public long     Id                 { get; init; }
    public long     TenantId           { get; init; }
    public Guid     CredentialPublicId { get; init; }
    public string   Token              { get; init; } = string.Empty;  // GUID seguro
    public DateTime ExpiresAtUtc       { get; init; }
    public bool     IsUsed             { get; init; }
}
```

### LoginHandler — modificado para 2FA

```csharp
// Authentication.Application/UseCases/Login/LoginHandler.cs
public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken ct)
{
    var credential = await _credentials.GetForLoginAsync(request.Email, ct);
    if (credential is null || !BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash))
        return new LoginInvalidCredentialsFailure("Credenciales inválidas.");

    if (!credential.IsActive)
        return new LoginInvalidCredentialsFailure("Cuenta inactiva.");

    // Verificar si tiene 2FA activo
    var mfaConfig = await _mfaConfigs.GetActiveByCredentialAsync(credential.PublicId, ct);

    if (mfaConfig is not null)
    {
        // Emitir token temporal — el JWT definitivo se emite después del 2FA
        var mfaToken = Guid.NewGuid().ToString("N");
        await _mfaPendingTokens.InsertAsync(
            credential.TenantId, credential.PublicId, mfaToken,
            expiresAt: DateTime.UtcNow.AddMinutes(5), ct);

        return new LoginMfaRequiredSuccess(mfaToken);
    }

    // Sin 2FA — login directo
    var tokens = _jwt.Generate(credential.PublicId, credential.Email,
        credential.Role, credential.TenantId, credential.BranchId);
    return new LoginSuccess(tokens);
}
```

### VerifyMfaHandler

```csharp
// Authentication.Application/UseCases/VerifyMfa/VerifyMfaHandler.cs
public async Task<VerifyMfaResponse> Handle(
    VerifyMfaRequest request, CancellationToken ct)
{
    var pending = await _mfaPendingTokens.GetByTokenAsync(request.MfaToken, ct);

    if (pending is null || pending.IsUsed)
        return new VerifyMfaInvalidTokenFailure("Token MFA inválido.");

    if (pending.ExpiresAtUtc < DateTime.UtcNow)
        return new VerifyMfaExpiredFailure("Token MFA expirado. Inicia sesión de nuevo.");

    var mfaConfig = await _mfaConfigs.GetActiveByCredentialAsync(pending.CredentialPublicId, ct);
    if (mfaConfig is null)
        return new VerifyMfaInvalidTokenFailure("Configuración MFA no encontrada.");

    var isValid = false;

    if (mfaConfig.Method == MfaMethod.Totp)
    {
        var secretBytes = Base32Encoding.ToBytes(_encryption.Decrypt(mfaConfig.TotpSecretEncrypted));
        var totp        = new Totp(secretBytes, step: 30);
        isValid = totp.VerifyTotp(request.Code, out _, new VerificationWindow(1, 1));
    }

    // Intentar con recovery code si TOTP falló
    if (!isValid)
    {
        var codeHash     = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Code))).ToLower();
        var recoveryCode = await _recoveryCodes.GetUnusedByHashAsync(
            pending.CredentialPublicId, codeHash, ct);

        if (recoveryCode is not null)
        {
            await _recoveryCodes.MarkAsUsedAsync(recoveryCode.Id, ct);
            isValid = true;
        }
    }

    if (!isValid)
        return new VerifyMfaInvalidCodeFailure("Código incorrecto.");

    // Invalidar el token MFA temporal
    await _mfaPendingTokens.MarkAsUsedAsync(pending.Id, ct);

    // Emitir los tokens definitivos
    var credential = await _credentials.GetByPublicIdAsync(pending.CredentialPublicId, ct);
    var tokens     = _jwt.Generate(credential!.PublicId, credential.Email,
        credential.Role, credential.TenantId, credential.BranchId);

    return new VerifyMfaSuccess(tokens);
}
```

---

## Email OTP como alternativa / fallback

Cuando el usuario no tiene acceso a su app autenticadora, recibe un código de 6 dígitos por email:

```csharp
// EmailOtpHandler.cs — genera y envía código por email
public async Task<SendEmailOtpResponse> Handle(
    SendEmailOtpRequest request, CancellationToken ct)
{
    var pending = await _mfaPendingTokens.GetByTokenAsync(request.MfaToken, ct);
    if (pending is null) return new SendEmailOtpInvalidFailure("Token MFA inválido.");

    // Código de 6 dígitos (no TOTP — generado random, no basado en tiempo + secreto)
    var code = new Random().Next(100000, 999999).ToString();
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))).ToLower();

    await _emailOtpCodes.InsertAsync(
        pending.CredentialPublicId, hash,
        expiresAt: DateTime.UtcNow.AddMinutes(10), ct);

    var credential = await _credentials.GetByPublicIdAsync(pending.CredentialPublicId, ct);
    await _email.SendOtpAsync(credential!.Email, code, ct);

    return new SendEmailOtpSuccess();
}
```

---

## 2FA obligatorio por tenant

El Admin puede exigir 2FA a todos los usuarios del tenant:

```csharp
// TenantSettings: "require_2fa" = "true"
// En el login, si el tenant tiene require_2fa y el usuario no tiene 2FA configurado:
var requiresMfa = await _settings.GetBoolAsync(credential.TenantId, "require_2fa");
if (requiresMfa && mfaConfig is null)
    return new LoginMfaSetupRequiredFailure(
        "Tu empresa requiere autenticación de dos factores. Configúrala antes de continuar.");
```

---

## Endpoints

```csharp
[Route("api/auth/mfa")]
[Authorize]
public sealed class MfaController : BaseApiController
{
    // Iniciar setup
    [HttpPost("setup")]
    public async Task<IActionResult> Setup(CancellationToken ct) { ... }

    // Confirmar setup con primer código
    [HttpPost("setup/confirm")]
    public async Task<IActionResult> ConfirmSetup([FromBody] ConfirmMfaBody body, CancellationToken ct) { ... }

    // Desactivar 2FA (requiere verificar el código actual)
    [HttpDelete]
    public async Task<IActionResult> Disable([FromBody] DisableMfaBody body, CancellationToken ct) { ... }

    // Regenerar recovery codes (requiere verificar 2FA primero)
    [HttpPost("recovery-codes/regenerate")]
    public async Task<IActionResult> RegenerateRecoveryCodes([FromBody] VerifyCodeBody body, CancellationToken ct) { ... }
}

[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : BaseApiController
{
    // Verificar código 2FA después del login
    [HttpPost("login/verify-2fa")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaBody body, CancellationToken ct) { ... }

    // Solicitar código por email (fallback)
    [HttpPost("login/send-otp")]
    public async Task<IActionResult> SendEmailOtp([FromBody] SendOtpBody body, CancellationToken ct) { ... }
}
```

---

## Cifrado del secreto TOTP

El secreto TOTP se almacena cifrado con AES-256 — si la DB se compromete, el atacante no puede usar los secretos:

```csharp
// Common/Encryption/IEncryptionService.cs
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

// Infrastructure/Encryption/AesEncryptionService.cs
public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration config)
    {
        var keyBase64 = config["Encryption:Key"]!;
        _key = Convert.FromBase64String(keyBase64);   // 32 bytes para AES-256
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Guardar IV + ciphertext como base64
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        var data = Convert.FromBase64String(ciphertext);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV  = data[..16];   // primeros 16 bytes son el IV

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(data, 16, data.Length - 16);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

---

## Checklist

- [ ] Secreto TOTP cifrado con AES-256 en la DB — nunca en texto claro
- [ ] Ventana de tolerancia ±1 período (30 segundos) para desfase de reloj
- [ ] Recovery codes generados al activar 2FA — mostrados solo una vez
- [ ] Recovery codes almacenados como hash SHA-256 — nunca en texto claro
- [ ] Token MFA temporal de 5 minutos — no usar el accessToken definitivo hasta pasar el 2FA
- [ ] Email OTP como fallback con expiración de 10 minutos
- [ ] 2FA obligatorio por tenant: `require_2fa` en TenantSettings
- [ ] `Encryption:Key` en variables de entorno — nunca en appsettings
- [ ] Rate limiting estricto en los endpoints de verificación (evitar fuerza bruta del código de 6 dígitos)

---

## Relación con el back-template

Para agregar 2FA al back-template:
1. `Authentication.Domain/Entities/UserMfaConfig.cs`, `MfaRecoveryCode.cs`, `MfaPendingToken.cs`
2. `Shared/Database/EntityTypeConfigurations/` — 3 configuraciones EF Core
3. `Authentication.Application/UseCases/SetupMfa/`, `ConfirmMfaSetup/`, `VerifyMfa/`
4. `LoginHandler` — modificar para retornar `LoginMfaRequiredSuccess` cuando hay 2FA activo
5. `AesEncryptionService` + `IEncryptionService` en `Common/`

Ver `04-backend/21-jwt.md` para el flujo de tokens.
Ver `04-backend/40-session-management.md` para revocar sesiones cuando se activa 2FA.

---

## Glosario

| Término | Definición |
|---------|-----------|
| TOTP | Time-based One-Time Password — código de 6 dígitos que cambia cada 30 segundos según RFC 6238 |
| OtpNet | Librería de .NET para generar y verificar códigos TOTP y HOTP compatibles con Google Authenticator |
| MfaMethod | Enumeración de métodos de segundo factor disponibles: TOTP, RecoveryCode |
| MfaRecoveryCode | Código de respaldo de un solo uso que permite acceder si el dispositivo TOTP no está disponible |
| MfaPendingToken | Token temporal emitido tras el login exitoso con password que permite completar el segundo factor |
| AES-256 | Algoritmo de cifrado simétrico usado para proteger el secreto TOTP almacenado en base de datos |
| VerifyTotp | Método que valida el código de 6 dígitos del usuario contra el secreto TOTP del usuario |
| Base32Encoding | Codificación del secreto TOTP compatible con apps de autenticación como Google Authenticator |
| QR Code URI | URI en formato otpauth:// que se convierte a QR para escanearlo con la app de autenticación |
| Recovery Codes | Conjunto de códigos de un solo uso generados al activar 2FA — alternativa si se pierde el dispositivo |
| Ventana de validación | Tolerancia temporal en la verificación TOTP que acepta el código del intervalo anterior y siguiente |
| IEncryptionService | Interfaz del Common que abstrae el cifrado AES-256 del secreto TOTP para almacenamiento seguro |

---

*Rogelio Arriaga Gonzalez*
