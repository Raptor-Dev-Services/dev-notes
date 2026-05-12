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
