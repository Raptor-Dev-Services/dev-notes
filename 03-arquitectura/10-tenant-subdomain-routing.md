# 10 — Tenant Subdomain Routing

El subdomain routing resuelve el tenant desde el subdominio de la URL en lugar del JWT claim. Cuando un usuario accede a `alfacorp.misaas.com`, el sistema infiere que pertenece al tenant "alfacorp" antes de validar el JWT. Es el patrón estándar en SaaS B2B (Slack, Notion, Vercel, etc.).

---

## Por qué subdomain routing

```
Sin subdomain routing:
   misaas.com/app  → cualquier tenant inicia sesión aquí
   El tenant se conoce solo DESPUÉS del login (del JWT)

Con subdomain routing:
   alfacorp.misaas.com → el tenant se conoce ANTES del login
   Ventajas:
   ✓ UI personalizada (logo, color) antes de autenticar
   ✓ Validación temprana ("este subdominio no existe")
   ✓ Segregación visual por empresa
   ✓ Permite SSO por tenant (cada empresa tiene su propio IDP)
```

---

## Resolución del tenant: dos momentos

```
Momento A — Request sin JWT (login, registro, verificación email):
   alfacorp.misaas.com/login
   → Extraer "alfacorp" del Host header
   → Buscar tenant por slug en la DB
   → Establecer TenantContext en el accessor

Momento B — Request con JWT:
   alfacorp.misaas.com/api/users
   → Extraer "alfacorp" del Host header
   → Validar que el tenant del subdominio coincide con el tenant_id del JWT
   → Si no coincide → 403 (el usuario tiene token de otro tenant)
```

---

## SubdomainTenantMiddleware

```csharp
// Host.Api/Middleware/SubdomainTenantMiddleware.cs
public sealed class SubdomainTenantMiddleware
{
    private readonly RequestDelegate          _next;
    private readonly ITenantContextAccessor   _tenantAccessor;
    private readonly ITenantRepository        _tenants;
    private readonly ILogger<SubdomainTenantMiddleware> _logger;

    private readonly string _baseDomain;   // "misaas.com" de la configuración

    public SubdomainTenantMiddleware(
        RequestDelegate next,
        ITenantContextAccessor tenantAccessor,
        ITenantRepository tenants,
        ILogger<SubdomainTenantMiddleware> logger,
        IConfiguration config)
    {
        _next           = next;
        _tenantAccessor = tenantAccessor;
        _tenants        = tenants;
        _logger         = logger;
        _baseDomain     = config["App:BaseDomain"] ?? "misaas.com";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host   = context.Request.Host.Host;   // "alfacorp.misaas.com"
        var slug   = ExtractSlug(host);

        if (slug is not null)
        {
            var tenant = await _tenants.GetBySlugAsync(slug);
            if (tenant is null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new { message = $"Tenant '{slug}' no encontrado." });
                return;
            }

            if (tenant.Status == TenantStatus.Suspended)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { message = "Tu cuenta está suspendida." });
                return;
            }

            // Establecer el tenant en el accessor
            _tenantAccessor.Current = new TenantContext(tenant.Id.ToString(), branchId: null);
        }

        await _next(context);
    }

    private string? ExtractSlug(string host)
    {
        // "alfacorp.misaas.com" → "alfacorp"
        // "misaas.com" → null (dominio raíz, sin tenant)
        // "localhost" → null (dev local sin subdominio)
        if (host == _baseDomain || host == "localhost") return null;

        var withoutBase = host.Replace($".{_baseDomain}", "");
        return withoutBase != host ? withoutBase : null;
    }
}
```

---

## Orden del middleware con subdominio

```csharp
// Host.Api/Program.cs
app.UseMiddleware<SubdomainTenantMiddleware>();   // 1. Resolver tenant del subdominio
app.UseAuthentication();                          // 2. Validar JWT
app.UseAuthorization();                           // 3. Verificar [Authorize]
app.UseMiddleware<TenantClaimsMiddleware>();       // 4. Verificar coherencia subdominio vs JWT
```

El `SubdomainTenantMiddleware` va **antes** de la autenticación para que el tenant esté disponible durante el login (que no tiene JWT todavía).

---

## TenantClaimsMiddleware con validación de coherencia

Cuando hay JWT, verificar que el tenant del subdominio coincide con el `tenant_id` del token:

```csharp
// Host.Api/Middleware/TenantClaimsMiddleware.cs (versión con subdomain)
public async Task InvokeAsync(HttpContext context)
{
    var jwtTenantId = context.User.FindFirstValue("tenant_id");

    if (!string.IsNullOrEmpty(jwtTenantId))
    {
        // Si el subdominio ya estableció un tenant, verificar coherencia
        var subdomainTenantId = _accessor.Current?.TenantId;

        if (subdomainTenantId is not null && subdomainTenantId != jwtTenantId)
        {
            // El usuario tiene token de tenant B pero accede desde subdominio de tenant A
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Token no válido para este tenant."
            });
            return;
        }

        // El tenant del JWT toma precedencia (más específico)
        _accessor.Current = new TenantContext(
            jwtTenantId,
            context.User.FindFirstValue("branch_id"));
    }

    await _next(context);
}
```

---

## Configuración de DNS y certificados

Para que los subdominios funcionen en producción:

### DNS — wildcard record

```
# En tu proveedor DNS (ej. Cloudflare, Route53)
*.misaas.com  CNAME  misaas.com   (o A record a la IP del load balancer)
```

### Nginx — wildcard SSL

```nginx
# /etc/nginx/sites-available/misaas
server {
    listen 443 ssl;
    server_name *.misaas.com;

    ssl_certificate     /etc/letsencrypt/live/misaas.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/misaas.com/privkey.pem;

    location / {
        proxy_pass         http://localhost:8080;
        proxy_set_header   Host $host;              # ← preservar el subdominio
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

### Let's Encrypt — wildcard certificate

```bash
certbot certonly --dns-cloudflare \
  --dns-cloudflare-credentials ~/.secrets/cloudflare.ini \
  -d misaas.com -d *.misaas.com
```

---

## Leer el Host header detrás de un proxy

ASP.NET Core detrás de Nginx/ALB/Cloudflare necesita leer el host del header `X-Forwarded-Host`:

```csharp
// Host.Api/Program.cs
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
});
```

```csharp
// En el middleware: usar Request.Host que ya refleja el header forwarded
var host = context.Request.Host.Host;   // ya es "alfacorp.misaas.com" aunque llegue de Nginx
```

---

## Desarrollo local con subdominios

Para probar subdominios en local sin DNS real:

### Opción A — /etc/hosts (Windows: C:\Windows\System32\drivers\etc\hosts)

```
127.0.0.1   alfacorp.localhost
127.0.0.1   betacorp.localhost
```

```csharp
// appsettings.Local.json
{
  "App": { "BaseDomain": "localhost" }
}
```

### Opción B — LocalTunnel / ngrok

```bash
ngrok http 5080 --subdomain misaas
# Expone: https://misaas.ngrok.io
# Para subdominos: necesita plan ngrok pro con wildcard
```

### Opción C — Header X-Tenant-Slug para dev (solo en ambiente Local)

Para evitar la complejidad de subdominios en dev, aceptar el slug en un header:

```csharp
// Solo en Development/Local
if (_environment.IsDevelopment())
{
    var devSlug = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
    if (devSlug is not null)
    {
        var tenant = await _tenants.GetBySlugAsync(devSlug);
        if (tenant is not null)
            _tenantAccessor.Current = new TenantContext(tenant.Id.ToString());
    }
}
```

---

## Tenant resolution desde dominio custom (Enterprise)

Algunos tenants Enterprise quieren su propio dominio: `crm.alfacorp.com` en lugar de `alfacorp.misaas.com`.

```csharp
// Tenancy.Domain/Entities/Tenant.cs
public sealed class Tenant
{
    // ...
    public string?  CustomDomain   { get; init; }   // "crm.alfacorp.com"
    public bool     CustomDomainVerified { get; init; }
}
```

```csharp
// SubdomainTenantMiddleware — también buscar por dominio custom
var tenant = ExtractSlug(host) is string slug
    ? await _tenants.GetBySlugAsync(slug)
    : await _tenants.GetByCustomDomainAsync(host);   // fallback a dominio custom
```

El dominio custom requiere que el cliente apunte `crm.alfacorp.com` CNAME a `misaas.com` y que se verifique la propiedad del dominio (con un TXT record).

---

## Slug en el login — vincular subdominio con credenciales

El endpoint de login recibe el slug del subdominio para validar que el usuario pertenece a ese tenant:

```csharp
// LoginHandler.cs
public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken ct)
{
    // El TenantId ya está en el accessor (puesto por SubdomainTenantMiddleware)
    var tenantId = long.TryParse(_tenantAccessor.Current?.TenantId, out var id) ? id : 0L;

    var credential = await _credentials.GetByEmailAndTenantAsync(
        request.Email, tenantId, ct);

    if (credential is null || !BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash))
        return new LoginInvalidCredentialsFailure("Credenciales inválidas.");

    // ...
}
```

Si el usuario existe pero en otro tenant, el resultado es "Credenciales inválidas" (sin revelar que el email existe en otro tenant).

---

## Checklist de implementación

- [ ] DNS wildcard `*.misaas.com` apuntando al load balancer
- [ ] Certificado SSL wildcard con Let's Encrypt o ACM (AWS)
- [ ] `UseForwardedHeaders` configurado correctamente detrás de proxy
- [ ] `SubdomainTenantMiddleware` va antes de `UseAuthentication`
- [ ] Validación de coherencia: tenant del subdominio == tenant del JWT
- [ ] Tenant `Suspended` retorna 403 antes de procesar el request
- [ ] Tenant inexistente retorna 404 con mensaje claro
- [ ] Slug normalizado a minúsculas — `AlfaCorp` == `alfacorp`
- [ ] Header `X-Tenant-Slug` como fallback solo en Development
- [ ] Tests: acceso con JWT de tenant B desde subdominio de tenant A → 403

---

## Relación con back-template (tenant + branch)

El subdominio identifica al **tenant**. El **branch** sigue viniendo del JWT (o de otra lógica como el perfil del usuario). El subdomain routing no resuelve el nivel de branch — eso es responsabilidad del claim.

Múltiples tenants, cada uno con su subdominio y sus branches:
```
alfacorp.misaas.com → TenantId=1
    JWT tiene branch_id=10 → BranchId=10 (Sucursal Norte de Alfa)

betacorp.misaas.com → TenantId=2
    JWT tiene branch_id=20 → BranchId=20 (Planta Monterrey de Beta)
```

Ver `04-backend/24-multi-tenancy-logic.md` para el contexto de tenant.
Ver `04-backend/25-tenant-branch-logic.md` para la jerarquía de dos niveles.

---

*Rogelio Arriaga Gonzalez*
