# 21 — JWT: JSON Web Tokens

JWT es el estándar para transmitir información de forma segura entre partes como un objeto JSON firmado. En APIs REST es la forma más común de implementar autenticación stateless.

---

## Estructura de un JWT
> Fuente: *JWT Handbook* — Ch.2 Practical Applications of JWT

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9     ← Header (base64url)
.eyJzdWIiOiJ1c3ItMTIzIiwibmFtZSI6Ik1hcsOtYSIsImlhdCI6MTcxNjAwMDAwMH0   ← Payload (base64url)
.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c   ← Signature

Header:  { "alg": "HS256", "typ": "JWT" }
Payload: { "sub": "usr-123", "name": "María", "iat": 1716000000 }
Signature: HMACSHA256(base64(header) + "." + base64(payload), secret)
```

**El Payload es solo base64, no está cifrado.** No poner información sensible (passwords, datos de tarjeta).

---

## Claims estándar

```csharp
// Claims del estándar JWT (RFC 7519)
// sub  (Subject)          — identificador único del usuario
// iss  (Issuer)           — quién emitió el token
// aud  (Audience)         — para quién es el token
// exp  (Expiration)       — cuándo expira (Unix timestamp)
// iat  (Issued At)        — cuándo se emitió
// nbf  (Not Before)       — no válido antes de este tiempo
// jti  (JWT ID)           — ID único del token (para revocación)

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public string GenerateToken(Guid userId, string email, string[] roles)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now   = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),      // sub
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // jti único por token
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer:             _options.Issuer,
            audience:           _options.Audience,
            claims:             claims,
            notBefore:          now,
            expires:            now.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## Refresh Tokens — renovar sin pedir credenciales

Un JWT de acceso tiene vida corta (15-60 min). El Refresh Token tiene vida larga y se usa para obtener un nuevo Access Token.

```csharp
// Domain/Entities/RefreshToken.cs
public sealed class RefreshToken
{
    public int      Id          { get; init; }
    public Guid     UserId      { get; init; }
    public string   Token       { get; init; } = string.Empty;
    public DateTime ExpiresAt   { get; init; }
    public DateTime CreatedAt   { get; init; } = DateTime.UtcNow;
    public bool     IsRevoked   { get; private set; }
    public string?  ReplacedBy  { get; private set; }

    public static RefreshToken Create(Guid userId, int ttlDays = 30)
        => new()
        {
            UserId    = userId,
            Token     = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(ttlDays)
        };

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public RefreshToken Rotate()
    {
        var newToken = Create(UserId);
        IsRevoked   = true;
        ReplacedBy  = newToken.Token;
        return newToken;
    }
}
```

```csharp
// Application/UseCases/Auth/RefreshToken/RefreshTokenHandler.cs
public sealed class RefreshTokenHandler
    : IRequestHandler<RefreshTokenRequest, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly IExampleUserRepository  _users;
    private readonly IJwtTokenService        _jwt;

    public async Task<RefreshTokenResponse> Handle(
        RefreshTokenRequest request, CancellationToken ct)
    {
        // 1. Buscar el refresh token
        var existing = await _tokens.GetByTokenAsync(request.RefreshToken, ct);
        if (existing is null || !existing.IsValid())
            return new RefreshTokenInvalidFailure("Refresh token inválido o expirado.");

        // 2. Cargar el usuario
        var user = await _users.GetByIdAsync(existing.UserId, ct);
        if (user is null || !user.IsActive)
            return new RefreshTokenInvalidFailure("Usuario no encontrado o inactivo.");

        // 3. Rotar el refresh token (invalidar el anterior, crear uno nuevo)
        var newRefreshToken = existing.Rotate();
        await _tokens.UpdateAsync(existing, ct);
        await _tokens.InsertAsync(newRefreshToken, ct);

        // 4. Generar nuevo access token
        var accessToken = _jwt.GenerateToken(user.PublicId, user.Email, user.Roles);

        return new RefreshTokenSuccess(accessToken, newRefreshToken.Token);
    }
}
```

---

## Validación del JWT en ASP.NET Core

```csharp
// Program.cs — configuración completa de validación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validar la firma del token
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            // Validar issuer y audience
            ValidateIssuer   = true,
            ValidIssuer      = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience    = jwtOptions.Audience,

            // Validar expiración
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.FromSeconds(30),  // tolerancia de 30s por diferencias de reloj

            // Algoritmo permitido
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };

        options.Events = new JwtBearerEvents
        {
            // Para SignalR — el token llega en el query string
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            },

            // Log de errores de autenticación
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT authentication failed: {Error}", ctx.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });
```

---

## Revocación de tokens

JWT es stateless por diseño — una vez emitido no se puede invalidar sin un store adicional.

```csharp
// Estrategia 1: Token Blacklist (blocklist) en Redis
// Cuando el usuario hace logout, el jti del token se agrega al blocklist hasta su expiración

public sealed class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;

    public async Task RevokeAsync(string jti, DateTime expiry, CancellationToken ct)
    {
        var ttl = expiry - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero) return;  // ya expiró — no hace falta bloquear

        await _cache.SetStringAsync(
            $"revoked:{jti}",
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken ct)
        => await _cache.GetStringAsync($"revoked:{jti}", ct) is not null;
}

// Middleware que verifica el blacklist
public sealed class TokenRevocationMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklist)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (jti is not null && await blacklist.IsRevokedAsync(jti, CancellationToken.None))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Token revocado." });
                return;
            }
        }

        await _next(context);
    }
}
```

---

## Algoritmos de firma

```csharp
// HS256 — HMAC-SHA256 con secreto compartido
// Usa la misma clave para firmar y verificar
// ✓ Simple, rápido
// ✗ El secreto debe estar en todos los servicios que validan el token

var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

// RS256 — RSA-SHA256 con par de claves pública/privada
// Firma con clave privada (solo el servidor de auth)
// Verifica con clave pública (cualquier servicio puede verificar sin conocer la clave privada)
// ✓ Más seguro para microservicios — los servicios solo necesitan la clave pública
// ✗ Más lento que HS256

using var rsa     = RSA.Create(2048);
var privateKey    = new RsaSecurityKey(rsa);  // para firmar — solo el auth server
var publicKey     = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false));
var rsaCreds      = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);

// ES256 — ECDSA-SHA256 con par de claves de curva elíptica (P-256)
// Firma con clave privada, verifica con clave pública — igual que RS256
// ✓ Claves más pequeñas que RSA con seguridad equivalente
// ✓ Más rápido que RS256 en generación y verificación
// Fuente: *JWT Handbook* — Ch.4 JSON Web Signatures

using var ecdsa  = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var ecPrivateKey = new ECDsaSecurityKey(ecdsa);
var ecCreds      = new SigningCredentials(ecPrivateKey, SecurityAlgorithms.EcdsaSha256);
```

| Algoritmo | Tipo de clave | Velocidad | Caso de uso |
|-----------|--------------|-----------|-------------|
| HS256 | Secreto compartido (simétrico) | Muy rápido | Monolito o servicios que comparten el secreto |
| RS256 | Par RSA 2048 bits (asimétrico) | Lento en firma | Microservicios — la clave pública se distribuye |
| ES256 | Par ECDSA P-256 (asimétrico) | Rápido | Preferido sobre RS256 por claves más pequeñas |

---

## JWE — JSON Web Encryption (tokens cifrados)
> Fuente: *JWT Handbook* — Ch.5 JSON Web Encryption

Por defecto el payload de un JWT está codificado en base64url — **no cifrado**. Cualquiera que intercepte el token puede leer su contenido. JWE cifra el payload para que sea ilegible sin la clave de descifrado.

```
JWT (JWS):  header.payload.signature
JWE:        header.encrypted_key.iv.ciphertext.tag
```

```csharp
// JWE con Microsoft.IdentityModel.Tokens
var encryptionKey = new SymmetricSecurityKey(
    Convert.FromBase64String(jwtOptions.EncryptionKey));  // clave de 256 bits (32 bytes)

var encryptingCreds = new EncryptingCredentials(
    encryptionKey,
    JwtConstants.DirectKeyUseAlg,           // alg: "dir" — clave directa sin envolver
    SecurityAlgorithms.Aes256CbcHmacSha512  // enc: A256CBC-HS512
);

var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject               = new ClaimsIdentity(claims),
    Expires               = DateTime.UtcNow.AddMinutes(60),
    SigningCredentials     = signingCreds,
    EncryptingCredentials  = encryptingCreds   // firma + cifrado
};

var handler        = new JsonWebTokenHandler();
var encryptedToken = handler.CreateToken(tokenDescriptor);
```

Usar JWE cuando el payload contiene datos sensibles (PII, datos médicos o financieros) o cuando el token se almacena fuera de memoria del servidor.

---

## Consideraciones de seguridad
> Fuente: *JWT Handbook* — Ch.2 Practical Applications of JWT

### Signature Stripping

Un atacante puede tomar un JWT firmado, cambiar el header a `"alg": "none"` y eliminar la firma. Si el servidor no valida el algoritmo explícitamente, el token manipulado pasa.

```csharp
// ✓ Definir lista blanca de algoritmos aceptados
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
    // ...
};
// ✗ Omitir ValidAlgorithms — el default acepta cualquier algoritmo, incluido "none"
```

### Almacenamiento del token y XSS / CSRF

```
localStorage / sessionStorage
  ✗ Vulnerable a XSS — cualquier script en la página puede leerlo

Cookie HttpOnly + SameSite=Strict   ← opción preferida para web apps
  ✓ Inaccesible desde JavaScript
  ✓ SameSite=Strict mitiga CSRF sin token adicional

Memoria (variable JavaScript)
  ✓ XSS no puede extraerlo del storage
  ✗ Se pierde al refrescar la página — requiere refresh token en cookie para renovar
```

```csharp
// ✓ Si se usa cookie, configurar las propiedades de seguridad
options.Cookie = new CookieBuilder
{
    HttpOnly     = true,
    SameSite     = SameSiteMode.Strict,
    SecurePolicy = CookieSecurePolicy.Always
};
```

---

## JWK — JSON Web Keys y JWKS endpoint
> Fuente: *JWT Handbook* (Sebastian Peyrott, Auth0) — Ch.6 JSON Web Keys

Un JSON Web Key (JWK) es un formato estandarizado (RFC 7517) para representar claves criptográficas. Permite distribuir claves públicas de forma interoperable para que múltiples servicios puedan verificar tokens emitidos por un servidor de autenticación central.

### Estructura de un JWK

```json
{
  "kty": "RSA",
  "use": "sig",
  "alg": "RS256",
  "kid": "2024-key-1",
  "n":   "0vx7agoebGcQSuuPiLJXZptN9nn...",
  "e":   "AQAB"
}
```

| Campo | Descripción |
|-------|-------------|
| `kty` | Tipo de clave: `RSA`, `EC` (curva elíptica), `oct` (simétrica) |
| `use` | Uso: `sig` (firma) o `enc` (cifrado) |
| `alg` | Algoritmo: `RS256`, `ES256`, `HS256`, etc. |
| `kid` | Key ID — permite identificar qué clave usó el token (claim `kid` en el header) |
| `n`, `e` | Parámetros RSA (módulo y exponente) — solo la parte pública |

### JWK Set (JWKS)

Un JWKS es un JSON con un array `keys` que contiene múltiples JWKs. El endpoint `/.well-known/jwks.json` expone las claves públicas del servidor de autenticación:

```json
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "alg": "RS256",
      "kid": "2024-key-1",
      "n": "0vx7agoebGcQSuuPiLJXZptN9nn...",
      "e": "AQAB"
    },
    {
      "kty": "EC",
      "use": "sig",
      "alg": "ES256",
      "kid": "2024-key-2",
      "crv": "P-256",
      "x": "MKBCTNIcKUSDii11ySs3526iDZ8AiTo7Tu6KPAqv7D4",
      "y": "4Etl6SRW2YiLUrN5vfvVHuhp7x8PxltmWWlbbM4IFyM"
    }
  ]
}
```

### Exponer JWKS endpoint en ASP.NET Core

```csharp
// Program.cs — endpoint que expone las claves públicas
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

app.MapGet("/.well-known/jwks.json", (IOptions<JwtOptions> opts) =>
{
    using var rsa = RSA.Create();
    rsa.ImportRSAPublicKey(Convert.FromBase64String(opts.Value.PublicKeyBase64), out _);

    var rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false))
    {
        KeyId = opts.Value.KeyId
    };

    var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaSecurityKey);

    return Results.Json(new { keys = new[] { jwk } });
})
.AllowAnonymous()
.WithName("JWKS");
```

### Consumir JWKS desde un servicio cliente

```csharp
// Los microservicios validan tokens usando el JWKS del servidor de autenticación
// En lugar de compartir el secreto, solo necesitan la URL pública
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ConfigurationManager descarga el JWKS automáticamente y rota las claves
        options.Authority           = "https://auth.misaas.com";
        options.MetadataAddress     = "https://auth.misaas.com/.well-known/openid-configuration";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer           = true,
            ValidIssuer              = "https://auth.misaas.com",
            ValidateAudience         = true,
            ValidAudience            = "api",
            ValidateLifetime         = true,
        };
    });
```

### Rotación de claves sin downtime

Con JWKS, la rotación de claves es transparente:

```
1. Generar nueva clave (key-2024-12)
2. Agregar nueva clave al endpoint JWKS (ahora hay 2 claves publicadas)
3. Emitir nuevos tokens con kid = "key-2024-12" (la nueva clave)
4. Los tokens viejos (kid = "key-2024-01") siguen siendo válidos hasta que expiran
5. Cuando todos los tokens viejos expiran → eliminar la clave vieja del JWKS
```

```
❌ Con secreto compartido (HS256):
   - Cambiar el secreto invalida todos los tokens activos inmediatamente
   - Todos los servicios deben recibir el nuevo secreto de forma sincronizada

✓ Con JWKS (RS256 / ES256):
   - Múltiples claves coexisten durante el período de transición
   - Los servicios descargan el JWKS automáticamente → sin coordinación manual
   - Rotación gradual y sin downtime
```

---

## Cuándo usar / no usar JWT

| Usar JWT | No usar JWT |
|----------|------------|
| APIs stateless con múltiples clientes | Sesiones de usuario que necesitan revocación inmediata |
| Microservicios — el token viaja entre servicios | Aplicaciones donde el server puede mantener sesión en DB |
| Transferir claims entre sistemas de confianza | Cuando el payload crece mucho (tokens en cada request) |
| Mobile apps — el token persiste sin servidor | Long-lived sessions sin refresh token rotation |


---

*Rogelio Arriaga Gonzalez*
