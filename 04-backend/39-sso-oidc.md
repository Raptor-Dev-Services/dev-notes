# 39 — SSO por Tenant: OAuth2 / OIDC

SSO (Single Sign-On) permite que los usuarios de un tenant inicien sesión con las credenciales de su empresa (Google Workspace, Microsoft Entra ID, Okta) en lugar de tener un password separado en el SaaS. Es una feature Enterprise que elimina la fricción de gestionar otra contraseña y centraliza el control de acceso en el IDP de la empresa.

---

## El flujo OAuth2 / OIDC

```
Usuario accede a alfacorp.misaas.com/login
    ↓
Frontend detecta que alfacorp tiene SSO configurado con Google
    ↓
Redirige a Google OAuth: accounts.google.com/oauth/authorize?client_id=...&state=...
    ↓
Usuario se autentica en Google (ya tiene sesión → automático)
    ↓
Google redirige de vuelta: alfacorp.misaas.com/auth/callback?code=abc123&state=xyz
    ↓
API intercambia el code por tokens de Google (POST a token endpoint)
    ↓
API obtiene el ID Token (JWT de Google con email, nombre, etc.)
    ↓
API busca o crea la credencial del usuario en la DB
    ↓
API emite el JWT propio del SaaS con tenant_id + branch_id
    ↓
Usuario queda autenticado
```

---

## Entidad SsoConfiguration

```csharp
// Authentication.Domain/Entities/SsoConfiguration.cs
public sealed class SsoConfiguration
{
    public long     Id            { get; init; }
    public Guid     PublicId      { get; init; }
    public long     TenantId      { get; init; }
    public SsoProvider Provider   { get; init; }
    public string   ClientId      { get; init; } = string.Empty;
    public string   ClientSecretEncrypted { get; init; } = string.Empty;  // AES cifrado
    public string   TenantIdOrDomain { get; init; } = string.Empty;  // tenant de Azure AD, dominio de Google
    public string?  AuthorizationEndpoint { get; init; }  // para OIDC genérico
    public string?  TokenEndpoint         { get; init; }
    public string?  UserInfoEndpoint      { get; init; }
    public bool     IsActive      { get; init; }
    public bool     EnforceForDomain { get; init; }  // todos los emails de este dominio → SSO obligatorio
    public string?  AllowedDomain  { get; init; }    // "@alfacorp.com" → solo acepta estos emails
    public DateTime CreatedAtUtc   { get; init; }
}

public enum SsoProvider
{
    Google        = 1,
    MicrosoftEntraId = 2,
    Okta          = 3,
    GenericOidc   = 4
}
```

---

## Flujo de implementación: Iniciar SSO

```csharp
// Authentication.Application/UseCases/InitiateSso/InitiateSsoHandler.cs
public async Task<InitiateSsoResponse> Handle(
    InitiateSsoRequest request, CancellationToken ct)
{
    // El tenant se resuelve del subdominio (ver 03-arquitectura/10-tenant-subdomain-routing.md)
    var ssoConfig = await _ssoConfigs.GetActiveByTenantAsync(request.TenantId, ct);

    if (ssoConfig is null)
        return new InitiateSsoNotConfiguredFailure(
            "Este tenant no tiene SSO configurado.");

    // State CSRF — previene ataques CSRF en el callback
    var state = Guid.NewGuid().ToString("N");
    await _ssoStateCache.SetAsync(state, request.TenantId, TimeSpan.FromMinutes(10), ct);

    var authUrl = BuildAuthorizationUrl(ssoConfig, state);
    return new InitiateSsoSuccess(authUrl);
}

private string BuildAuthorizationUrl(SsoConfiguration config, string state)
{
    var baseUrl = config.Provider switch
    {
        SsoProvider.Google =>
            "https://accounts.google.com/o/oauth2/v2/auth",
        SsoProvider.MicrosoftEntraId =>
            $"https://login.microsoftonline.com/{config.TenantIdOrDomain}/oauth2/v2.0/authorize",
        SsoProvider.Okta =>
            $"https://{config.TenantIdOrDomain}/oauth2/v1/authorize",
        _ => config.AuthorizationEndpoint!
    };

    var redirectUri = $"{_appBaseUrl}/api/auth/sso/callback";

    var queryParams = new Dictionary<string, string>
    {
        ["client_id"]     = config.ClientId,
        ["response_type"] = "code",
        ["scope"]         = "openid email profile",
        ["redirect_uri"]  = redirectUri,
        ["state"]         = state,
        ["prompt"]        = "select_account"  // forzar selección de cuenta
    };

    if (config.Provider == SsoProvider.Google && config.AllowedDomain is not null)
        queryParams["hd"] = config.AllowedDomain.TrimStart('@');  // hint de dominio

    return $"{baseUrl}?{string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"))}";
}
```

---

## Flujo de callback: Intercambiar code por tokens

```csharp
// Authentication.Application/UseCases/HandleSsoCallback/HandleSsoCallbackHandler.cs
public async Task<HandleSsoCallbackResponse> Handle(
    HandleSsoCallbackRequest request, CancellationToken ct)
{
    // 1. Validar state anti-CSRF
    var tenantId = await _ssoStateCache.GetAndRemoveAsync(request.State, ct);
    if (!tenantId.HasValue)
        return new HandleSsoCallbackInvalidStateFailure("State inválido o expirado.");

    var ssoConfig = await _ssoConfigs.GetActiveByTenantAsync(tenantId.Value, ct);
    if (ssoConfig is null)
        return new HandleSsoCallbackFailure("SSO no configurado.");

    // 2. Intercambiar code por tokens del proveedor
    var tokenResponse = await ExchangeCodeAsync(ssoConfig, request.Code, ct);
    if (tokenResponse is null)
        return new HandleSsoCallbackFailure("Error al obtener tokens del proveedor.");

    // 3. Extraer información del usuario del ID Token
    var idTokenClaims = ParseIdToken(tokenResponse.IdToken);
    var email = idTokenClaims.FindFirst("email")?.Value;
    var name  = idTokenClaims.FindFirst("name")?.Value;
    var sub   = idTokenClaims.FindFirst("sub")?.Value;  // ID único en el proveedor

    if (email is null) return new HandleSsoCallbackFailure("El proveedor no retornó un email.");

    // 4. Validar que el email pertenece al dominio permitido
    if (ssoConfig.AllowedDomain is not null &&
        !email.EndsWith(ssoConfig.AllowedDomain, StringComparison.OrdinalIgnoreCase))
        return new HandleSsoCallbackDomainMismatchFailure(
            $"Solo se permiten emails de {ssoConfig.AllowedDomain}.");

    // 5. Buscar o crear la credencial del usuario
    var credential = await _credentials.GetByEmailAndTenantAsync(email, tenantId.Value, ct);

    if (credential is null)
    {
        // JIT Provisioning: crear el usuario al primer login con SSO
        var publicId = Guid.NewGuid();
        await _credentials.InsertSsoUserAsync(
            publicId:         publicId,
            tenantId:         tenantId.Value,
            email:            email,
            ssoProviderId:    sub!,
            ssoProvider:      ssoConfig.Provider,
            role:             "Viewer",   // rol por defecto para nuevos usuarios SSO
            ct:               ct);

        await _mediator.Publish(new UserShouldBeCreatedIntegrationEvent(
            publicId, tenantId.Value, name ?? email, email), ct);

        credential = await _credentials.GetByEmailAndTenantAsync(email, tenantId.Value, ct);
    }

    if (!credential!.IsActive)
        return new HandleSsoCallbackFailure("Tu cuenta está desactivada.");

    // 6. Emitir JWT propio del SaaS
    var tokens = _jwt.Generate(
        credential.PublicId, credential.Email,
        credential.Role, credential.TenantId, credential.BranchId);

    return new HandleSsoCallbackSuccess(tokens);
}

private async Task<OidcTokenResponse?> ExchangeCodeAsync(
    SsoConfiguration config, string code, CancellationToken ct)
{
    var tokenEndpoint = config.Provider switch
    {
        SsoProvider.Google          => "https://oauth2.googleapis.com/token",
        SsoProvider.MicrosoftEntraId =>
            $"https://login.microsoftonline.com/{config.TenantIdOrDomain}/oauth2/v2.0/token",
        _ => config.TokenEndpoint!
    };

    var clientSecret = _encryption.Decrypt(config.ClientSecretEncrypted);
    var redirectUri  = $"{_appBaseUrl}/api/auth/sso/callback";

    var body = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"]    = "authorization_code",
        ["code"]          = code,
        ["client_id"]     = config.ClientId,
        ["client_secret"] = clientSecret,
        ["redirect_uri"]  = redirectUri
    });

    var response = await _httpClient.PostAsync(tokenEndpoint, body, ct);
    if (!response.IsSuccessStatusCode) return null;

    var json = await response.Content.ReadAsStringAsync(ct);
    return JsonSerializer.Deserialize<OidcTokenResponse>(json);
}

private ClaimsPrincipal ParseIdToken(string idToken)
{
    // El ID Token es un JWT — parsear sin validar firma (el proveedor lo emitió)
    // En producción validar la firma con las claves públicas del proveedor (JWKS endpoint)
    var handler = new JwtSecurityTokenHandler();
    var jwt     = handler.ReadJwtToken(idToken);
    return new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims));
}
```

---

## Endpoint del callback

```csharp
[Route("api/auth/sso")]
[AllowAnonymous]
public sealed class SsoController : BaseApiController
{
    // Frontend llama este endpoint para obtener la URL de redirección al IDP
    [HttpGet("initiate")]
    public async Task<IActionResult> Initiate(CancellationToken ct)
    {
        // El TenantId ya está en el accessor (vía SubdomainTenantMiddleware)
        var tenantId = long.Parse(_tenantAccessor.Current?.TenantId ?? "0");
        _ = await Mediator.Send(new InitiateSsoRequest(tenantId), ct);
        if (!_viewModel.IsSuccess) return BadRequest(_viewModel);
        return Ok(_viewModel);   // { redirectUrl: "https://accounts.google.com/..." }
    }

    // Google/Microsoft redirige aquí después del login
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (error is not null)
            return Redirect($"{_appBaseUrl}/login?error=sso_cancelled");

        _ = await Mediator.Send(new HandleSsoCallbackRequest(code, state), ct);

        if (!_viewModel.IsSuccess)
            return Redirect($"{_appBaseUrl}/login?error=sso_failed");

        // Redirigir al frontend con los tokens en el fragment (nunca en query param)
        var tokens = (_viewModel.Data as TokenDto)!;
        return Redirect(
            $"{_appBaseUrl}/auth/sso-complete#access_token={tokens.AccessToken}" +
            $"&refresh_token={tokens.RefreshToken}");
    }
}
```

---

## Configurar SSO — endpoint Admin

```csharp
[Route("api/tenant/sso")]
[Authorize(Roles = "Admin")]
public sealed class TenantSsoController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetConfig(CancellationToken ct) { ... }

    [HttpPost]
    public async Task<IActionResult> Configure([FromBody] ConfigureSsoBody body, CancellationToken ct)
    {
        // Admin del tenant configura su IDP
        // ClientSecret se cifra antes de guardar
        _ = await Mediator.Send(new ConfigureSsoRequest(
            CurrentTenantId,
            body.Provider,
            body.ClientId,
            body.ClientSecret,         // se cifra en la infraestructura
            body.TenantIdOrDomain,
            body.AllowedDomain,
            body.EnforceForDomain), ct);
        // ...
    }

    [HttpDelete]
    public async Task<IActionResult> Remove(CancellationToken ct) { ... }
}
```

---

## SSO obligatorio por dominio de email

Si `EnforceForDomain = true` en la config del tenant, cualquier intento de login por password con un email del dominio configurado es rechazado:

```csharp
// LoginHandler.cs — verificar si el email debe usar SSO obligatorio
public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken ct)
{
    // ¿El tenant tiene SSO y enforce para este dominio?
    var emailDomain = "@" + request.Email.Split('@').Last();
    var ssoConfig   = await _ssoConfigs.GetActiveByTenantAndDomainAsync(
        request.TenantId, emailDomain, ct);

    if (ssoConfig?.EnforceForDomain == true)
        return new LoginSsoRequiredFailure(
            "Tu empresa requiere autenticación con SSO. " +
            "Usa el botón 'Iniciar sesión con tu empresa'.");

    // ... login normal con password ...
}
```

---

## JIT Provisioning vs Pre-provisioning

| Modo | Descripción | Cuándo usar |
|------|-------------|-------------|
| JIT Provisioning | El usuario se crea automáticamente al primer login SSO | Empresas que confían en el IDP para gestionar usuarios |
| Pre-provisioning | El Admin del SaaS debe invitar al usuario antes de que pueda hacer login | Empresas que quieren control explícito sobre quién accede |
| SCIM | El IDP sincroniza usuarios automáticamente (crear, modificar, desactivar) | Enterprise con muchos usuarios — fuera del scope inicial |

El back-template implementa JIT Provisioning como default — más simple y la mayoría de empresas lo prefiere.

---

## Checklist

- [ ] `ClientSecret` cifrado con AES-256 en la DB
- [ ] State anti-CSRF verificado en el callback — rechazar si no coincide
- [ ] Validar que el email pertenece al dominio permitido del tenant
- [ ] JIT Provisioning: crear usuario con rol `Viewer` por defecto
- [ ] SSO obligatorio por dominio: rechazar login por password si `EnforceForDomain = true`
- [ ] ID Token parseado correctamente — validar firma en producción (JWKS)
- [ ] Tokens del SaaS emitidos con el JWT propio — no usar los tokens del proveedor
- [ ] Redirect con tokens en el fragment (no query param) — no quedan en logs del servidor
- [ ] Feature de SSO solo en planes Pro/Enterprise (ver `04-backend/31-planes-limites.md`)

---

## Glosario

| Término | Definición |
|---------|-----------|
| SSO | Single Sign-On — permite que los usuarios inicien sesión con las credenciales de su proveedor de identidad corporativo |
| OIDC | OpenID Connect — capa de identidad sobre OAuth2 que añade el ID Token con claims del usuario |
| OAuth2 | Protocolo de autorización delegada — base de OIDC para el flujo de autorización |
| SsoConfiguration | Entidad que almacena por tenant la configuración del IdP: ClientId, ClientSecret, Authority, AllowedDomain |
| JIT Provisioning | Just-in-Time — creación automática del usuario en el primer login SSO si no existe previamente |
| State Anti-CSRF | Parámetro aleatorio generado en el inicio del flujo OAuth que se valida al recibir el callback para prevenir CSRF |
| Authorization Code Flow | Flujo OAuth2 donde el frontend recibe un code y el backend lo intercambia por tokens — más seguro que Implicit |
| ID Token | JWT emitido por el IdP que contiene claims del usuario: email, name, sub — diferente del Access Token |
| AllowedDomain | Dominio de email corporativo permitido para SSO en el tenant (ej. alfacorp.com) |
| EnforceForDomain | Configuración del tenant que obliga a usuarios de ese dominio a usar SSO en lugar de password |
| JWKS Endpoint | URL del IdP que expone las claves públicas para verificar la firma del ID Token |
| Redirect URI | URL del SaaS registrada en el IdP a la que se redirige tras la autenticación exitosa |

---

*Rogelio Arriaga Gonzalez*
