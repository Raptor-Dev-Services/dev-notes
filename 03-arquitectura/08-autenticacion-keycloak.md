# 08 — Autenticación y Keycloak

Keycloak es un Identity Provider (IdP) open-source que implementa OAuth2 y OpenID Connect. Centraliza la autenticación para múltiples servicios y aplicaciones.

---

## OAuth2 y OpenID Connect — conceptos base
> Fuente: *Keycloak - Identity and Access Management for Modern Applications* — Ch.2

```
OAuth2:         protocolo de autorización — "¿qué puede hacer este token?"
OpenID Connect: extensión de OAuth2 para autenticación — "¿quién eres?"

Flujos principales:
  Authorization Code + PKCE  → apps de usuario (SPA, móviles)
  Client Credentials         → comunicación servicio a servicio (M2M)
  Device Authorization       → dispositivos sin teclado (TV, IoT)
```

### Roles en OAuth2

```
Resource Owner  = el usuario final
Client          = la aplicación (SPA, API)
Authorization Server = Keycloak
Resource Server = la API que protege los recursos
```

---

## Configuración de Keycloak

```json
// appsettings.json — configuración para conectar al servidor Keycloak
{
  "Keycloak": {
    "Authority":   "https://auth.raptordev.io/realms/gtm-suite",
    "Audience":    "gtm-suite-api",
    "RequireHttps": true
  }
}
```

```csharp
// Program.cs — validar JWT emitidos por Keycloak
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keycloak publica las claves públicas en el endpoint /.well-known/openid-configuration
        options.Authority    = builder.Configuration["Keycloak:Authority"];
        options.Audience     = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttps");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromSeconds(30)
        };

        // Para SignalR — el token llega en el query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("RequireTenantAccess", policy =>
        policy.RequireClaim("tenant_id"));
});
```

---

## Claims y roles de Keycloak en .NET

Keycloak emite los roles en una estructura diferente a la que .NET espera por defecto. Se necesita un mapper de claims.

```csharp
// Los roles en Keycloak vienen en:
// realm_access.roles  → ["admin", "user"]
// resource_access.gtm-suite-api.roles → ["orders.write", "reports.read"]

// Mapear roles de Keycloak al claim estándar de .NET
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience  = builder.Configuration["Keycloak:Audience"];

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var identity = ctx.Principal?.Identity as ClaimsIdentity;
                if (identity is null) return Task.CompletedTask;

                // Extraer roles de realm_access
                var realmAccess = ctx.Principal?.FindFirst("realm_access")?.Value;
                if (realmAccess is not null)
                {
                    var roles = JsonSerializer.Deserialize<KeycloakRealmAccess>(realmAccess);
                    foreach (var role in roles?.Roles ?? [])
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }

                return Task.CompletedTask;
            }
        };
    });

public sealed class KeycloakRealmAccess
{
    [JsonPropertyName("roles")]
    public List<string> Roles { get; init; } = [];
}
```

```csharp
// Alternativa con ClaimsPrincipal extension
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Claim 'sub' no encontrado.");
        return Guid.Parse(sub);
    }

    public static string GetTenantId(this ClaimsPrincipal principal)
        => principal.FindFirst("tenant_id")?.Value
            ?? throw new InvalidOperationException("Claim 'tenant_id' no encontrado.");

    public static bool HasRole(this ClaimsPrincipal principal, string role)
        => principal.IsInRole(role);
}

// Uso en el CurrentUserService
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public Guid   UserId   => _http.HttpContext!.User.GetUserId();
    public string TenantId => _http.HttpContext!.User.GetTenantId();
    public bool   IsAdmin  => _http.HttpContext!.User.HasRole("admin");
}
```

---

## Multi-tenant con Keycloak

En un sistema SaaS multi-tenant, cada tenant puede tener su propio realm en Keycloak o todos pueden compartir un realm con un claim de `tenant_id`.

```csharp
// Opción 1: un realm por tenant (máximo aislamiento)
// Requiere descubrir el realm a partir del request (ej: por subdominio)

public sealed class TenantResolver
{
    public string ResolveRealm(HttpContext context)
    {
        // Por subdominio: mxgrogu.raptordev.io → realm "mxgrogu"
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.').FirstOrDefault() ?? "default";
        return subdomain;
    }
}

// Authority dinámica según el tenant
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var resolver = ctx.HttpContext.RequestServices.GetRequiredService<TenantResolver>();
                var realm = resolver.ResolveRealm(ctx.HttpContext);
                ctx.Options.Authority = $"https://auth.raptordev.io/realms/{realm}";
                return Task.CompletedTask;
            }
        };
    });
```

```csharp
// Opción 2: realm compartido con claim tenant_id (más simple)
// El token contiene: { "sub": "user-id", "tenant_id": "mxgrogu", "roles": [...] }

// Middleware que valida que el tenant del token coincide con el del recurso
public sealed class TenantAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantFromRoute = context.GetRouteValue("tenantId")?.ToString();
        var tenantFromToken = context.User.GetTenantId();

        if (tenantFromRoute is not null && tenantFromRoute != tenantFromToken)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Acceso denegado al tenant." });
            return;
        }

        await _next(context);
    }
}
```

---

## Client Credentials — comunicación M2M

Para comunicación entre servicios, el servicio que llama obtiene un token usando sus propias credenciales (no las del usuario).

```csharp
// Infrastructure/Auth/KeycloakTokenService.cs
// Servicio que obtiene tokens de client credentials para llamadas M2M
public sealed class KeycloakTokenService
{
    private readonly HttpClient _http;
    private string?             _cachedToken;
    private DateTime            _tokenExpiry;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var response = await _http.PostAsync("/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = "orders-service",
                ["client_secret"] = "the-client-secret"
            }), ct);

        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);

        _cachedToken = tokenResponse!.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30);

        return _cachedToken;
    }
}

public sealed class TokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
    [JsonPropertyName("expires_in")]   public int    ExpiresIn   { get; init; }
}
```

---

## Cuándo usar Keycloak vs JWT propio

| Criterio | Keycloak | JWT propio |
|----------|----------|-----------|
| Múltiples aplicaciones en el mismo tenant | ✓ SSO entre todas | ✗ Cada app gestiona sus tokens |
| Proveedores externos (Google, GitHub) | ✓ Identity Brokering integrado | ✗ Implementar por separado |
| MFA obligatorio | ✓ Configuración en la UI | ✗ Implementar por separado |
| App simple con un solo servicio | ✗ Overhead significativo | ✓ Más simple |
| Control total sobre el flujo de auth | ✗ Limitado por el servidor Keycloak | ✓ Implementación propia |


---

*Rogelio Arriaga Gonzalez*
