# 34 — API Keys: Autenticación Machine-to-Machine

Las API Keys permiten que aplicaciones externas (scripts, integraciones, ERPs) accedan al SaaS sin un usuario humano detrás. A diferencia del JWT que expira en minutos/horas, una API Key es de larga vida y está ligada a un tenant (y opcionalmente a un branch).

---

## Cuándo usar API Keys vs JWT

| Escenario | Usar |
|-----------|------|
| Usuario hace login en la app | JWT |
| Script de integración nocturna | API Key |
| ERP que llama al SaaS cada hora | API Key |
| Webhook que el SaaS recibe de terceros | API Key o Shared Secret |
| Integración CI/CD | API Key |
| App móvil | JWT con refresh token |

---

## Estructura de una API Key

```
sk_live_c8f2a1d0e9b3f4a5b6c7d8e9f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5
│    │    └─────────────────────────────────────────────────────────
│    │          parte aleatoria (32 bytes = 64 chars hex)
│    └── ambiente (live/test)
└── prefijo del tipo (sk = secret key)
```

```csharp
public static class ApiKeyGenerator
{
    public static string Generate(string environment = "live")
    {
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var hex = Convert.ToHexString(randomBytes).ToLower();
        return $"sk_{environment}_{hex}";
    }
}
```

---

## Entidad ApiKey

```csharp
// Tenancy.Domain/Entities/ApiKey.cs
public sealed class ApiKey
{
    public long     Id             { get; init; }
    public Guid     PublicId       { get; init; }
    public long     TenantId       { get; init; }
    public long?    BranchId       { get; init; }   // null = key a nivel tenant, valor = key a nivel branch
    public string   Name           { get; init; } = string.Empty;   // "Integración ERP"
    public string   KeyHash        { get; init; } = string.Empty;   // SHA-256 del key — nunca el key en claro
    public string   KeyPrefix      { get; init; } = string.Empty;   // "sk_live_c8f2" para mostrar al usuario
    public string   Scopes         { get; init; } = string.Empty;   // "orders:read,orders:write"
    public bool     IsActive       { get; init; }
    public DateTime? LastUsedAt    { get; init; }
    public DateTime? ExpiresAt     { get; init; }   // null = no expira
    public DateTime CreatedAtUtc   { get; init; }
    public Guid     CreatedByPublicId { get; init; }
}
```

La API Key **nunca se almacena en texto claro** — solo el hash. El hash permite verificar sin recuperar la key original.

---

## Crear una API Key

```csharp
// Tenancy.Application/UseCases/CreateApiKey/CreateApiKeyHandler.cs
public async Task<CreateApiKeyResponse> Handle(
    CreateApiKeyRequest request, CancellationToken ct)
{
    // 1. Verificar límite de keys por tenant
    var count = await _apiKeys.CountActiveAsync(request.TenantId, ct);
    if (count >= MaxApiKeysPerTenant)
        return new CreateApiKeyLimitReachedFailure(
            $"Máximo {MaxApiKeysPerTenant} API keys por tenant.");

    // 2. Generar la key en claro (solo se muestra UNA VEZ)
    var plainKey = ApiKeyGenerator.Generate();

    // 3. Hash SHA-256 para almacenar
    var keyHash = ComputeHash(plainKey);

    // 4. Prefix para identificar (sin revelar la key completa)
    var keyPrefix = plainKey[..16];   // "sk_live_c8f2a1d0"

    await _apiKeys.InsertAsync(
        tenantId:          request.TenantId,
        branchId:          request.BranchId,
        name:              request.Name,
        keyHash:           keyHash,
        keyPrefix:         keyPrefix,
        scopes:            string.Join(",", request.Scopes),
        expiresAt:         request.ExpiresAt,
        createdByPublicId: request.CreatorPublicId,
        ct:                ct);

    // Retornar la key en claro SOLO en este momento — nunca se puede recuperar después
    return new CreateApiKeySuccess(plainKey, keyPrefix);
}

private static string ComputeHash(string key)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    return Convert.ToHexString(bytes).ToLower();
}

private const int MaxApiKeysPerTenant = 20;
```

---

## Autenticar con API Key — Handler de autenticación

```csharp
// Host.Api/Authentication/ApiKeyAuthenticationHandler.cs
public sealed class ApiKeyAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyRepository _apiKeys;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Leer la key del header Authorization: Bearer sk_live_...
        // o del header X-Api-Key: sk_live_...
        string? plainKey = null;

        if (Request.Headers.TryGetValue("X-Api-Key", out var headerValue))
        {
            plainKey = headerValue.ToString();
        }
        else if (Request.Headers.Authorization.ToString()
            .StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
        {
            plainKey = Request.Headers.Authorization.ToString()["ApiKey ".Length..];
        }

        if (string.IsNullOrEmpty(plainKey))
            return AuthenticateResult.NoResult();   // no es autenticación por API key — intentar JWT

        // Calcular hash y buscar en la DB
        var keyHash = ComputeHash(plainKey);
        var apiKey  = await _apiKeys.GetByHashAsync(keyHash);

        if (apiKey is null || !apiKey.IsActive)
            return AuthenticateResult.Fail("API Key inválida.");

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt < DateTime.UtcNow)
            return AuthenticateResult.Fail("API Key expirada.");

        // Actualizar LastUsedAt (sin bloquear la request — fire and forget)
        _ = _apiKeys.UpdateLastUsedAtAsync(apiKey.Id, DateTime.UtcNow);

        // Construir ClaimsPrincipal con el tenant (y branch si aplica)
        var claims = new List<Claim>
        {
            new Claim("tenant_id", apiKey.TenantId.ToString()),
            new Claim("api_key_id", apiKey.PublicId.ToString()),
            new Claim("api_key_name", apiKey.Name),
            new Claim(ClaimTypes.Role, "ApiClient"),   // rol especial para API keys
        };

        if (apiKey.BranchId.HasValue)
            claims.Add(new Claim("branch_id", apiKey.BranchId.Value.ToString()));

        // Agregar scopes como claims
        foreach (var scope in apiKey.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries))
            claims.Add(new Claim("scope", scope.Trim()));

        var identity  = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private static string ComputeHash(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLower();
    }
}
```

### Registrar el esquema de autenticación

```csharp
// Host.Api/Extensions/AuthExtensions.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* configuración JWT existente */ })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        "ApiKey", null);

// Política que acepta JWT o API Key
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme, "ApiKey")
        .Build();
});
```

---

## Scopes — permisos granulares de la API Key

```csharp
public static class ApiScopes
{
    public const string OrdersRead   = "orders:read";
    public const string OrdersWrite  = "orders:write";
    public const string ReportsRead  = "reports:read";
    public const string UsersRead    = "users:read";
    public const string WebhooksRead = "webhooks:read";
}
```

```csharp
// Verificar scope en un endpoint
[HttpPost("orders")]
[Authorize]
public async Task<IActionResult> Create([FromBody] CreateOrderBody body, CancellationToken ct)
{
    // Si es API Key, verificar que tiene el scope correcto
    if (User.HasClaim("api_key_id", _) &&
        !User.HasClaim("scope", ApiScopes.OrdersWrite))
    {
        return Forbid();
    }
    // ...
}
```

O usando una policy:

```csharp
options.AddPolicy("OrdersWrite", policy =>
    policy.RequireAssertion(ctx =>
        ctx.User.HasClaim("scope", ApiScopes.OrdersWrite) ||
        ctx.User.HasClaim(ClaimTypes.Role, "Admin")));   // Admins siempre pueden
```

---

## Endpoints de gestión de API Keys

```csharp
[Route("api/api-keys")]
[Authorize(Roles = "Admin")]   // solo Admin puede gestionar API Keys
public sealed class ApiKeysController : BaseApiController
{
    // Listar keys del tenant (muestra solo prefix + metadata, nunca la key completa)
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        _ = await Mediator.Send(new GetApiKeysRequest(CurrentTenantId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }

    // Crear nueva key — retorna la key en claro UNA sola vez
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyBody body, CancellationToken ct)
    {
        var creatorPublicId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        _ = await Mediator.Send(new CreateApiKeyRequest(
            body.Name, body.BranchId, body.Scopes,
            body.ExpiresAt, CurrentTenantId, creatorPublicId), ct);
        if (_viewModel.IsSuccess) return Ok(_viewModel);   // incluye plainKey
        return StatusCode(500, _viewModel);
    }

    // Revocar (desactivar) una key
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        _ = await Mediator.Send(new RevokeApiKeyRequest(id, CurrentTenantId), ct);
        return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
    }
}
```

---

## API Keys con tenant + branch

Una API Key puede estar limitada a un branch específico (para integraciones de sucursal):

```
Key "Integración ERP Corporativo": TenantId=1, BranchId=null → accede a todo el tenant
Key "Integración POS Sucursal Norte": TenantId=1, BranchId=10 → solo Sucursal Norte
Key "Script Reportes Sucursal Sur": TenantId=1, BranchId=11 → solo Sucursal Sur
```

El `TenantClaimsMiddleware` procesa el `branch_id` del ClaimsPrincipal (puesto por `ApiKeyAuthenticationHandler`) igual que si viniera del JWT — el Global Query Filter de EF Core filtra por ambos automáticamente.

---

## Seguridad adicional

**IP Whitelist (opcional para Enterprise):**

```csharp
// Verificar IP en el handler de autenticación
var allowedIps = apiKey.AllowedIps?.Split(',') ?? Array.Empty<string>();
if (allowedIps.Any() &&
    !allowedIps.Contains(Request.Connection.RemoteIpAddress?.ToString()))
{
    return AuthenticateResult.Fail("IP no autorizada para esta API Key.");
}
```

**Rotación de keys:**
El cliente crea una nueva key, actualiza su integración para usar la nueva, y luego revoca la vieja. No hay downtime porque ambas son válidas durante la transición.

---

## Checklist

- [ ] La key en texto claro solo se muestra al crear — nunca se recupera después
- [ ] Almacenar solo el hash SHA-256 — no la key en claro
- [ ] Prefijo en la key para identificar sin revelar (`sk_live_c8f2...`)
- [ ] Scopes granulares — no dar acceso total por defecto
- [ ] `LastUsedAt` actualizado en cada uso (fire and forget para no bloquear)
- [ ] `ExpiresAt` opcional — keys de integración suelen ser permanentes pero revocables
- [ ] Endpoint de revocación — el Admin puede revocar cualquier key de su tenant
- [ ] El rol `ApiClient` no tiene acceso a endpoints de gestión de usuarios ni de billing
- [ ] Rate limiting por `api_key_id` en lugar de por `tenant_id` (si la key tiene límites propios)

---

## Glosario

| Término | Definición |
|---------|-----------|
| API Key | Credencial de larga duración para autenticación máquina-a-máquina — alternativa al JWT para integraciones |
| SHA-256 Hash | Hash criptográfico almacenado en lugar de la API Key en texto plano — impide exposición ante breach de DB |
| KeyHash | Campo en la entidad ApiKey que almacena el hash SHA-256 de la clave — lo que se guarda en base de datos |
| KeyPrefix | Primeros caracteres de la API Key mostrados al usuario para identificar la clave sin revelarla completa |
| Scopes | Permisos granulares de la API Key: read:users, write:orders — principio de mínimo privilegio |
| ApiKeyAuthenticationHandler | Handler de autenticación de ASP.NET Core que valida el header X-Api-Key buscando el hash en base de datos |
| Revocación | Invalidación inmediata de una API Key — el Admin puede revocar cualquier key de su tenant |
| LastUsedAt | Timestamp de último uso de la API Key — actualizado en background para no bloquear el request |
| ApiKeyGenerator | Utilidad que genera la API Key como string aleatorio criptográficamente seguro (Base64URL o hex) |
| AuthenticateResult | Resultado del handler de autenticación: Success con ClaimsPrincipal o Fail con mensaje de error |
| One-time Show | La API Key en texto plano solo se muestra una vez al crearla — después solo se almacena el hash |
| X-Api-Key | Header HTTP estándar por convención para enviar la API Key en cada request |

---

*Rogelio Arriaga Gonzalez*
