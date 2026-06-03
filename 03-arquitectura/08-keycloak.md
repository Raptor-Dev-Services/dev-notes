# 08 — Keycloak — IAM, OIDC y SSO

Keycloak es un servidor de identidad y acceso (IAM) open-source. Centraliza autenticación, autorización, SSO y gestión de usuarios fuera de la aplicación. El back-template valida tokens JWT emitidos por Keycloak — no gestiona contraseñas ni sesiones directamente.

> Fuente: Documentación oficial Keycloak 25 — https://www.keycloak.org/documentation

---

## El problema que resuelve

```csharp
// ❌ Sin Keycloak — la API gestiona identidad internamente
public class AuthController : ControllerBase
{
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _repo.GetByEmailAsync(req.Email);
        if (!BCrypt.Verify(req.Password, user.PasswordHash)) return Unauthorized();
        var token = _jwtService.CreateToken(user);
        return Ok(new { token });
    }
    // La API tiene que gestionar: hash de contraseñas, refresh tokens, revocación,
    // 2FA, SSO entre apps, federación con Google/AD... código que crece sin control.
}
```

```csharp
// ✓ Con Keycloak — la API solo valida el token
// Program.cs
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority   = "https://auth.midominio.com/realms/gtm-suite";
        options.Audience    = "gtm-api";
        options.TokenValidationParameters.ValidateIssuer = true;
    });
// La API no sabe de contraseñas. Keycloak los gestiona; la API solo valida la firma JWT.
```

---

## Conceptos clave

| Concepto | Descripción |
|---------|------------|
| **Realm** | Espacio de configuración aislado — usuarios, clientes, roles. Un realm por entorno o por tenant |
| **Client** | Aplicación registrada en Keycloak (`gtm-api`, `gtm-frontend`) |
| **User** | Identidad en Keycloak — puede federarse desde LDAP, AD o Google |
| **Role** | Permiso asignable a usuarios — realm roles (globales) y client roles (por aplicación) |
| **Scope** | Claim opcional que el cliente puede solicitar (`openid`, `email`, `profile`, `roles`) |
| **Access Token** | JWT de corta vida (5-15 min) para acceder a la API |
| **Refresh Token** | Token de larga vida para obtener nuevos access tokens sin re-login |
| **ID Token** | JWT con datos del usuario para el cliente frontend |

---

## Flujos OIDC soportados

### Authorization Code + PKCE (frontend SPA)

```
Frontend (React)                     Keycloak                   Back-template API
     │                                    │                            │
     │── GET /auth?response_type=code ───►│                            │
     │   &client_id=gtm-frontend          │                            │
     │   &code_challenge=S256             │                            │
     │                                    │ [Login form]               │
     │◄── redirect + code ───────────────│                            │
     │                                    │                            │
     │── POST /token (code + verifier) ──►│                            │
     │◄── { access_token, refresh_token }─│                            │
     │                                    │                            │
     │── GET /api/users Authorization: Bearer {access_token} ─────────►│
     │                                    │        [valida JWT firma] ◄─│
     │◄──────────────────────────── 200 OK ──────────────────────────── │
```

### Client Credentials (machine-to-machine)

```bash
# Servicio A llama a servicio B sin usuario — usa client_id + client_secret
curl -X POST https://auth.midominio.com/realms/gtm-suite/protocol/openid-connect/token \
  -d "grant_type=client_credentials" \
  -d "client_id=billing-service" \
  -d "client_secret=secret"
```

---

## Configuración en ASP.NET Core

```csharp
// Program.cs — validación de JWT emitido por Keycloak
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        // Authority = "https://auth.midominio.com/realms/gtm-suite"
        // Keycloak expone JWKS en {Authority}/.well-known/openid-configuration

        options.Audience = builder.Configuration["Keycloak:Audience"];
        // Audience = "gtm-api" — el client_id del recurso API en Keycloak

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                // Loguear token inválido sin exponer detalles al cliente
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Token inválido: {Error}", ctx.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// appsettings.json
// {
//   "Keycloak": {
//     "Authority": "https://auth.midominio.com/realms/gtm-suite",
//     "Audience": "gtm-api"
//   }
// }
```

---

## Claims del token Keycloak

```json
{
  "sub": "d4e5f6a7-...",
  "iss": "https://auth.midominio.com/realms/gtm-suite",
  "aud": ["gtm-api", "account"],
  "exp": 1735000000,
  "iat": 1734999100,
  "email": "rogelio@alfacorp.com",
  "email_verified": true,
  "preferred_username": "rogelio.arriaga",
  "realm_access": {
    "roles": ["admin", "user"]
  },
  "resource_access": {
    "gtm-api": {
      "roles": ["reports:read", "users:write"]
    }
  }
}
```

```csharp
// Leer claims en un handler
public class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    private readonly IHttpContextAccessor _ctx;

    public async Task<GetExampleUserResponse> Handle(
        GetExampleUserRequest request, CancellationToken ct)
    {
        var userId  = _ctx.HttpContext!.User.FindFirst("sub")?.Value;
        var email   = _ctx.HttpContext!.User.FindFirst(ClaimTypes.Email)?.Value;
        var isAdmin = _ctx.HttpContext!.User.IsInRole("admin");
        // ...
    }
}
```

---

## Multi-tenancy con Keycloak

Hay dos estrategias:

### Opción A — Un realm por tenant (aislamiento máximo)

```
Realm: alfacorp  → usuarios de Alfa Corp
Realm: betacorp  → usuarios de Beta Corp
```

El backend detecta el realm desde el `iss` del token y deriva el `tenant_id`.

```csharp
// Middleware — extrae tenant desde el issuer del token
var issuer  = context.User.FindFirst("iss")?.Value;
// issuer = "https://auth.midominio.com/realms/alfacorp"
var tenantSlug = issuer?.Split('/').LastOrDefault();
// tenantSlug = "alfacorp"
```

**Ventaja:** aislamiento total — usuarios de un tenant nunca ven a otros.  
**Desventaja:** gestionar N realms — complejidad operativa alta.

### Opción B — Un realm + claim tenant_id (recomendado para empezar)

```
Realm: gtm-suite  → todos los tenants
  Usuario: rogelio@alfacorp.com
    Custom claim: tenant_id = "alfacorp"
    Custom claim: branch_id = "sucursal-norte"
```

```csharp
// Program.cs — añade mapper de claims custom de Keycloak
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
{
    opts.TokenValidationParameters.RoleClaimType = "realm_access.roles";
});

// Middleware que lee tenant_id del JWT y lo pone en ITenantContextAccessor
public class TenantClaimsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContextAccessor accessor)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;
        var branchId = context.User.FindFirst("branch_id")?.Value;
        if (tenantId is not null) accessor.SetContext(tenantId, branchId);
        await next(context);
    }
}
```

---

## Docker Compose para desarrollo local

```yaml
services:
  keycloak:
    image: quay.io/keycloak/keycloak:25.0
    command: start-dev --import-realm
    environment:
      KC_DB: dev-mem          # en memoria para dev; usar postgres en prod
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
    volumes:
      - ./keycloak/realm-export.json:/opt/keycloak/data/import/realm.json
    ports:
      - "8080:8080"
```

```bash
# Exportar realm para versionarlo en el repo
docker exec -it keycloak /opt/keycloak/bin/kc.sh export \
  --realm gtm-suite \
  --dir /opt/keycloak/data/export
```

---

## Relación con el back-template

El back-template está diseñado para que el `token_id` y `tenant_id` vengan del JWT. Con Keycloak:

- `SubdomainTenantMiddleware` puede reemplazarse por `TenantClaimsMiddleware` que lee los claims
- Los Global Query Filters en EF Core siguen usando `ITenantContextAccessor` — no cambia nada en Infrastructure
- Las políticas de autorización usan los roles del JWT: `[Authorize(Roles = "admin")]`

---

## Mapear roles de Keycloak al sistema de roles de .NET

Keycloak emite los roles en `realm_access.roles` — estructura diferente a lo que .NET espera en `ClaimTypes.Role`:

```csharp
options.Events = new JwtBearerEvents
{
    OnTokenValidated = ctx =>
    {
        var identity = ctx.Principal?.Identity as ClaimsIdentity;
        if (identity is null) return Task.CompletedTask;

        var realmAccess = ctx.Principal?.FindFirst("realm_access")?.Value;
        if (realmAccess is not null)
        {
            var roles = JsonSerializer.Deserialize<KeycloakRealmAccess>(realmAccess);
            foreach (var role in roles?.Roles ?? [])
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
        return Task.CompletedTask;
    },
    // SignalR — el token llega en el query string
    OnMessageReceived = ctx =>
    {
        var token = ctx.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            ctx.Token = token;
        return Task.CompletedTask;
    }
};

public sealed class KeycloakRealmAccess
{
    [JsonPropertyName("roles")]
    public List<string> Roles { get; init; } = [];
}
```

---

## ClaimsPrincipalExtensions — leer claims del token

```csharp
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
        => Guid.Parse(principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Claim 'sub' no encontrado."));

    public static string GetTenantId(this ClaimsPrincipal principal)
        => principal.FindFirst("tenant_id")?.Value
            ?? throw new InvalidOperationException("Claim 'tenant_id' no encontrado.");

    public static bool HasRole(this ClaimsPrincipal principal, string role)
        => principal.IsInRole(role);
}

// CurrentUserService — encapsula la lectura del contexto del usuario en el request
public sealed class CurrentUserService(IHttpContextAccessor http) : ICurrentUserService
{
    public Guid   UserId   => http.HttpContext!.User.GetUserId();
    public string TenantId => http.HttpContext!.User.GetTenantId();
    public bool   IsAdmin  => http.HttpContext!.User.HasRole("admin");
}
```

---

## Client Credentials — M2M con token cacheado

```csharp
public sealed class KeycloakTokenService(HttpClient http, IConfiguration config)
{
    private string?  _cachedToken;
    private DateTime _tokenExpiry;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var response = await http.PostAsync("/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = config["Keycloak:ClientId"]!,
                ["client_secret"] = config["Keycloak:ClientSecret"]!
            }), ct);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        _cachedToken = token!.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 30);
        return _cachedToken;
    }
}
```

---

## Cuándo usar / no usar

| Usar Keycloak | No usar Keycloak |
|---------------|-----------------|
| SSO entre múltiples aplicaciones del mismo tenant | App con un solo cliente y auth simple |
| Federación con Google, Microsoft AD, LDAP | Proyecto sin presupuesto de infraestructura adicional |
| Requisitos de 2FA / MFA gestionados externamente | MVP o PoC donde auth custom es más rápido |
| Múltiples tenants con políticas de acceso distintas | Cuando ya existe otro IdP (Auth0, Okta, Entra ID) |

---

## Glosario

| Término | Definición |
|---------|-----------|
| IAM | Identity and Access Management — gestión centralizada de identidades y permisos |
| Keycloak | Servidor de identidad open-source de Red Hat — gestiona usuarios, SSO y tokens OAuth2/OIDC |
| Realm | Unidad de configuración aislada en Keycloak — agrupa usuarios, clientes y roles |
| Client | Aplicación registrada en Keycloak que solicita tokens (`gtm-api`, `gtm-frontend`) |
| OIDC | OpenID Connect — capa de identidad sobre OAuth2 que añade el ID Token con datos del usuario |
| OAuth2 | Protocolo de autorización — define cómo obtener tokens de acceso a recursos protegidos |
| Authorization Code + PKCE | Flujo seguro para SPAs/móvil — usa code_verifier para evitar interceptación del código |
| Client Credentials | Flujo OAuth2 para machine-to-machine — no hay usuario, el servicio usa client_id + secret |
| Access Token | JWT de corta vida (~5-15 min) que la API valida en cada request |
| Refresh Token | Token de larga vida para obtener nuevos access tokens sin re-login del usuario |
| JWKS | JSON Web Key Set — endpoint de Keycloak con las claves públicas para verificar JWT |
| SSO | Single Sign-On — un login en Keycloak da acceso a todas las apps del realm sin re-autenticación |
| `sub` | Subject claim en JWT — identificador único del usuario en Keycloak |
| Realm Roles | Roles globales del realm — disponibles en todos los clientes |
| Client Roles | Roles específicos de un client — solo aplican a ese recurso |
| `tenant_id` claim | Claim personalizado que el back-template usa para resolver el tenant del request |

---

*Rogelio Arriaga Gonzalez*
