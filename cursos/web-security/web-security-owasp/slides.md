<!-- _class: lead -->

# Seguridad Web
## OWASP Top 10  -  Zero Trust  -  Secretos en Producción
### Dev Notes  -  Material extra

---

<!-- _class: lead -->

# Parte 1
## OWASP Top 10

---

## OWASP Top 10 (2021)  -  Las 10 vulnerabilidades más críticas

| # | Vulnerabilidad | Ejemplo en el sistema de mensajería |
|---|---------------|--------------------------|
| A01 | Broken Access Control | Agente accede a conversaciones de otro workspace |
| A02 | Cryptographic Failures | Passwords en texto plano, HTTP sin TLS |
| A03 | Injection | SQL injection en búsqueda de contactos |
| A04 | Insecure Design | Sin rate limiting en endpoint de login |
| A05 | Security Misconfiguration | Headers de seguridad faltantes en la API |
| A06 | Vulnerable Components | Dependencias NuGet/npm con CVEs conocidos |
| A07 | Auth Failures | Tokens JWT sin expiración, sin rotación |
| A08 | Software Integrity | Sin validación de webhooks de WhatsApp/Instagram |
| A09 | Logging Failures | No loguear intentos de acceso fallidos |
| A10 | SSRF | La app hace requests a URLs enviadas por contactos |

---

## A01  -  Broken Access Control

 Sin verificar ownership del recurso

```csharp
// Cualquier agente autenticado puede ver conversaciones de otros workspaces
[HttpGet("{conversationId}")]
[Authorize]
public async Task<IActionResult> GetConversation(Guid conversationId)
{
    var conversation = await _mediator.Send(new GetConversationRequest(conversationId));
    return Ok(conversation);   // <- no verifica a qué workspace pertenece
}
```

 Verificar que el recurso pertenece al workspace del agente actual

```csharp
[HttpGet("{conversationId}")]
[Authorize]
public async Task<IActionResult> GetConversation(Guid conversationId)
{
    var workspaceId = User.GetWorkspaceId();
    var request     = new GetConversationRequest(conversationId, workspaceId);
    var result      = await _mediator.Send(request);

    return result switch
    {
        GetConversationSuccess s      => Ok(s.Data),
        GetConversationForbidden f    => Forbid(),
        GetConversationNotFound _     => NotFound(),
        _                            => StatusCode(500)
    };
}
```

---

## A01  -  Broken Access Control (Handler)

 El handler verifica ownership en cada query

```csharp
public sealed class GetConversationHandler
    : IRequestHandler<GetConversationRequest, GetConversationResponse>
{
    public async Task<GetConversationResponse> Handle(
        GetConversationRequest request, CancellationToken ct)
    {
        var conversation = await _repo.GetByIdAsync(request.ConversationId, ct);

        if (conversation is null)
            return new GetConversationNotFound("Conversación no encontrada.");

        // Verificar que pertenece al workspace del agente que solicita
        if (conversation.WorkspaceId != request.WorkspaceId)
            return new GetConversationForbidden("Acceso denegado.");

        return new GetConversationSuccess(ConversationDto.From(conversation));
    }
}
```

> Regla: cada query/command valida ownership. Nunca confiar solo en el token.

---

<!-- _class: lead -->

# Parte 2
## Inyección  -  SQL Injection  -  Command Injection

---

## A03  -  SQL Injection

 Concatenar input del usuario en SQL

```csharp
// Si email = "' OR '1'='1" -> devuelve todos los usuarios
// Si email = "'; DROP TABLE Contacts; --" -> borra la tabla de contactos
public async Task<Contact?> GetByEmailAsync(string email)
{
    var sql = $"SELECT * FROM Contacts WHERE Email = '{email}'";
    return await _db.QuerySingleOrDefaultAsync<Contact>(sql);
}
```

 Parámetros nombrados  -  Dapper/EF Core escapan automáticamente

```csharp
public async Task<Contact?> GetByEmailAsync(string email, CancellationToken ct)
    => await _db.QuerySingleOrDefaultAsync<Contact>(
        "SELECT * FROM Contacts WHERE Email = @email",
        new { email },
        cancellationToken: ct);
```

 Con EF Core  -  LINQ nunca concatena SQL

```csharp
// EF Core traduce esto a un query parametrizado
var contact = await _ctx.Contacts
    .Where(c => c.Email == email)
    .FirstOrDefaultAsync(ct);
```

---

## A03  -  Command Injection

 Concatenar input en argumentos de proceso del sistema

```csharp
// Si fileName = "attachment.jpg && rm -rf /"  -> ejecuta ambos comandos
public async Task ConvertFileAsync(string fileName)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo("convert", $"{fileName} output.pdf")
    };
    process.Start();
}
```

 Validar y usar ArgumentList (array), no concatenación

```csharp
public async Task ConvertFileAsync(string fileName)
{
    if (!Regex.IsMatch(fileName, @"^[a-zA-Z0-9_\-\.]+$"))
        throw new ArgumentException("Nombre de archivo inválido.");

    var safePath = Path.GetFullPath(Path.Combine(_uploadDir, fileName));
    if (!safePath.StartsWith(_uploadDir))
        throw new ArgumentException("Path inválido.");

    var process = new Process
    {
        StartInfo = new ProcessStartInfo("convert")
        {
            ArgumentList = { safePath, "output.pdf" }   // <- array, no concatenación
        }
    };
    process.Start();
}
```

---

<!-- _class: lead -->

# Parte 3
## XSS  -  Cross-Site Scripting

---

## XSS  -  Tipos y vectores de ataque

| Tipo | Descripción | Ejemplo en el sistema de mensajería |
|------|-------------|--------------------------|
| **Reflected** | El script viene en la URL y el servidor lo refleja en la respuesta | `?search=<script>alert(1)</script>` |
| **Stored** | El script se guarda en DB y se ejecuta al renderizar | Mensaje de WhatsApp con `<script>` guardado en la bandeja |
| **DOM-based** | El script manipula el DOM sin pasar por el servidor | `location.hash` insertado en `innerHTML` sin sanitizar |

### Impacto en el sistema de mensajería

- Robar el token JWT del `localStorage` de un agente
- Exfiltrar el historial de conversaciones de un contacto
- Inyectar código que envíe mensajes en nombre del agente

---

## XSS  -  Prevención en React y CSP

 React escapa por defecto  -  pero `dangerouslySetInnerHTML` rompe esa protección

```tsx
//  Nunca renderizar HTML de mensajes sin sanitizar
const MessageBubble = ({ content }: { content: string }) => (
  <div dangerouslySetInnerHTML={{ __html: content }} />
);

//  Dejar que React escape  -  solo texto plano
const MessageBubble = ({ content }: { content: string }) => (
  <div>{content}</div>
);

//  Si necesitas HTML: sanitizar primero con DOMPurify
import DOMPurify from 'dompurify';
const MessageBubble = ({ content }: { content: string }) => (
  <div dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(content) }} />
);
```

 Content-Security-Policy en el servidor .NET

```csharp
context.Response.Headers.Append("Content-Security-Policy",
    "default-src 'self'; " +
    "script-src 'self'; " +           // bloquea scripts inline y de otros dominios
    "style-src 'self' 'unsafe-inline'; " +
    "img-src 'self' data: https:; " +
    "connect-src 'self' wss://crm.example.com");   // SignalR WebSocket
```

---

<!-- _class: lead -->

# Parte 4
## Autenticación Rota  -  Bcrypt  -  Rate Limiting

---

## A02 + A04  -  Contraseñas y Fuerza Bruta

 MD5 / SHA-256 para passwords  -  crackeables con GPU en segundos

```csharp
//  SHA-256 diseñado para ser RÁPIDO -> ataque de fuerza bruta viable
var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));

//  MD5  -  roto criptográficamente desde hace décadas
var hash = MD5.HashData(Encoding.UTF8.GetBytes(password));
```

 BCrypt  -  diseñado para ser lento (work factor configurable)

```csharp
//  BCrypt agrega salt automáticamente + work factor = iteraciones
var hash  = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
var valid = BCrypt.Net.BCrypt.Verify(password, hash);

// workFactor = 12 -> ~250ms por hash en hardware moderno
// Si un atacante roba la DB: 250ms × millones de intentos = impracticable
```

> A mayor `workFactor`, más tiempo tarda el hash. Escalar según capacidad del servidor.
> BCrypt incluye salt y versión en el propio hash  -  no hay que almacenarlos por separado.

---

## A04  -  Rate Limiting en Login

 Limitar intentos de login por IP para bloquear fuerza bruta

```csharp
// Program.cs
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit          = 5;                       // máx 5 intentos
        limiter.Window               = TimeSpan.FromMinutes(15);// por ventana de 15 min
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit           = 0;                       // no encolar  -  rechazar
    });

    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

app.UseRateLimiter();

// AuthController
[HttpPost("login")]
[EnableRateLimiting("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request) { ... }
```

> 5 intentos en 15 minutos bloquea fuerza bruta automatizada.
> En producción: usar Redis como almacén para escalar entre múltiples instancias.

---

## A07  -  Tokens seguros con HttpOnly Cookie

 Access token en HttpOnly Cookie  -  no accesible por JavaScript

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
    if (result is LoginFailure failure)
        return Unauthorized(new { error = failure.Message });

    var success = (LoginSuccess)result;

    Response.Cookies.Append("access_token", success.AccessToken, new CookieOptions
    {
        HttpOnly = true,                        // no accesible por JavaScript (protege vs XSS)
        Secure   = true,                        // solo HTTPS
        SameSite = SameSiteMode.Strict,         // protege vs CSRF
        Expires  = DateTimeOffset.UtcNow.AddMinutes(15)
    });

    return Ok(new { message = "Login exitoso." });
}

[HttpPost("logout")]
[Authorize]
public IActionResult Logout()
{
    Response.Cookies.Delete("access_token");
    return Ok();
}
```

---

<!-- _class: lead -->

# Parte 5
## Zero Trust  -  "Never Trust, Always Verify"

---

## Modelo Perimetral vs Zero Trust

 Modelo perimetral (viejo)  -  "castillo y foso"

```
Internet ---- Firewall ---- Red interna (confianza total)
              "Si estás dentro del firewall, eres de confianza"
              Problema: si un atacante entra -> acceso a TODO
```

 Zero Trust  -  cada request se verifica, sin importar el origen

```
Cada request ->  ¿Quién eres?       (identidad: JWT / mTLS)
             ->  ¿Qué puedes hacer? (autorización: least privilege)
             ->  ¿Desde dónde?      (dispositivo, red, contexto)

"Nunca confiar, siempre verificar  -  incluso dentro de la red interna"
Si un atacante entra a la red -> no tiene acceso automático a nada
```

### Los 5 pilares de Zero Trust

| Pilar | Implementación en el sistema de mensajería |
|-------|---------------------------------|
| Verificar identidad | JWT con MFA para agentes; mTLS entre servicios |
| Verificar el dispositivo | Solo dispositivos corporativos conocidos |
| Least privilege | Claims granulares: `conversations:read`, `contacts:write` |
| Inspeccionar tráfico | TLS en todo el tráfico interno + logs de acceso |
| Asumir breach | Validar en cada capa, aunque venga de un servicio interno |

---

## Zero Trust  -  Least Privilege con Claims

 Rol amplio  -  "admin puede todo"

```csharp
//  Un solo rol amplio no diferencia operaciones
options.AddPolicy("Admin", policy => policy.RequireRole("admin"));
```

 Claims granulares por operación

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Conversations:Read",
        p => p.RequireClaim("permission", "conversations:read"));
    options.AddPolicy("Conversations:Assign",
        p => p.RequireClaim("permission", "conversations:assign"));
    options.AddPolicy("Contacts:Export",
        p => p.RequireClaim("permission", "contacts:export"));
});

[HttpGet]
[Authorize(Policy = "Conversations:Read")]
public async Task<IActionResult> GetAll() { ... }

[HttpPatch("{id}/assign")]
[Authorize(Policy = "Conversations:Assign")]
public async Task<IActionResult> Assign(Guid id, ...) { ... }
```

---

## Zero Trust  -  mTLS entre Servicios

 Autenticación mutua: servidor y cliente se validan entre sí

```csharp
// Servidor: validar el certificado del servicio que llama
builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = ctx =>
            {
                var expectedThumb = "expected-thumbprint-of-internal-service";
                if (ctx.ClientCertificate.Thumbprint != expectedThumb)
                {
                    ctx.Fail("Certificado de cliente no reconocido.");
                    return Task.CompletedTask;
                }
                ctx.Success();
                return Task.CompletedTask;
            }
        };
    });

// Cliente: adjuntar su propio certificado en las llamadas
builder.Services.AddHttpClient("crm-internal")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        var cert    = new X509Certificate2(certPath, certPassword);
        handler.ClientCertificates.Add(cert);
        return handler;
    });
```

---

## Zero Trust  -  Asumir Breach

 Validar en cada capa aunque el request venga de un servicio interno

```csharp
public sealed class SendMessageHandler
    : IRequestHandler<SendMessageCommand, SendMessageResponse>
{
    public async Task<SendMessageResponse> Handle(
        SendMessageCommand cmd, CancellationToken ct)
    {
        // Validar aunque venga de un servicio interno  -  principio "asumir breach"
        if (string.IsNullOrWhiteSpace(cmd.Content) && cmd.AttachmentId is null)
            return new SendMessageValidationFailure("El mensaje no puede estar vacío.");

        if (cmd.Content?.Length > 4096)
            return new SendMessageValidationFailure("Mensaje excede el límite de caracteres.");

        // Verificar que el agente tiene acceso a esta conversación
        var conversation = await _convRepo.GetByIdAsync(cmd.ConversationId, ct);
        if (conversation is null || conversation.WorkspaceId != cmd.WorkspaceId)
            return new SendMessageForbidden("Acceso denegado.");

        // ...
    }
}
```

---

<!-- _class: lead -->

# Parte 6
## Secretos en Producción

---

## Reglas fundamentales de secretos

 Lo que NUNCA se debe hacer

```
appsettings.Production.json  -> JWT secret, connection string hardcodeados
.env con valores reales       -> commiteado al repositorio
Variables de entorno sin cifrar -> visibles en logs de CI/CD
Argumentos de línea de comandos -> quedan en historial de shell
```

 Dónde guardar secretos

| Entorno | Herramienta | Cómo |
|---------|-------------|------|
| Desarrollo local | `dotnet user-secrets` | Fuera del repo, en `%APPDATA%` |
| Staging / Prod AWS | AWS Secrets Manager | IAM Role del contenedor ECS |
| Staging / Prod Azure | Azure Key Vault | Managed Identity del App Service |
| Pipeline CI/CD | GitHub Secrets | `${{ secrets.MY_SECRET }}` |

---

## Secretos en Desarrollo  -  User Secrets

 `dotnet user-secrets`  -  fuera del repositorio, automático en Development

```bash
# Inicializar user secrets (agrega <UserSecretsId> al .csproj)
dotnet user-secrets init

# Guardar secretos locales
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=crm_dev;..."
dotnet user-secrets set "Jwt:Secret" "dev-secret-key-256-bits-minimum"
dotnet user-secrets set "WhatsApp:ApiToken" "EAAxxxxx..."
```

```csharp
// Program.cs  -  NO se necesita código adicional
// CreateBuilder(args) carga user-secrets automáticamente en Development
var builder = WebApplication.CreateBuilder(args);

// Los secretos se inyectan en IConfiguration igual que appsettings.json
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
```

> Los secretos se almacenan en `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json`
> Nunca se incluyen en el repositorio Git.

---

## Secretos en AWS  -  Secrets Manager

 Vault gestionado con rotación automática

```bash
# Crear secreto con todos los valores del sistema de mensajería
aws secretsmanager create-secret \
  --name "mensajeria/production" \
  --secret-string '{
    "ConnectionStrings__Default": "Host=crm-db.rds.amazonaws.com;...",
    "Jwt__Secret": "super-secret-key-256-bits",
    "WhatsApp__ApiToken": "EAAxxxxx...",
    "Instagram__AppSecret": "ig-secret..."
  }'
```

```csharp
// Program.cs  -  cargar secretos al iniciar la app
builder.Configuration.AddSecretsManager(region: RegionEndpoint.USEast1, configurator: opts =>
{
    opts.SecretFilter  = entry => entry.Name.StartsWith("mensajeria/");
    opts.KeyGenerator  = (entry, key) => key
        .Replace("mensajeria/production:", "")
        .Replace("__", ":");   // Jwt__Secret -> Jwt:Secret
});
```

> La ECS task definition asume un IAM Role con permiso `secretsmanager:GetSecretValue`.
> La app nunca tiene credenciales propias  -  usa el rol del contenedor.

---

## Secretos en Azure  -  Key Vault + Managed Identity

 Sin credenciales en código  -  la identidad del App Service accede al vault

```bash
# Crear Key Vault y agregar secretos del sistema de mensajería
az keyvault create --name mensajeria-kv --resource-group crm-rg --location eastus

az keyvault secret set --vault-name mensajeria-kv \
  --name "ConnectionStrings--Default" \
  --value "Host=crm-db.postgres.database.azure.com;..."

az keyvault secret set --vault-name mensajeria-kv \
  --name "WhatsApp--ApiToken" --value "EAAxxxxx..."
```

```csharp
// Program.cs  -  DefaultAzureCredential usa Managed Identity en producción
var keyVaultUri = new Uri("https://mensajeria-kv.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
// En local usa las credenciales de "az login"  -  sin secretos en código
```

> `DefaultAzureCredential` detecta el entorno automáticamente:
> producción -> Managed Identity, local -> `az login`, CI -> federated credential.

---

<!-- _class: lead -->

# Parte 7
## HTTPS y Headers de Seguridad

---

## A05  -  Headers de seguridad en .NET

 Configurar todos los headers en un solo middleware

```csharp
// Program.cs  -  middleware de headers de seguridad
app.Use(async (context, next) =>
{
    // Prevenir clickjacking (iframe embedding)
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // Prevenir MIME sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // Referrer controlado
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // CSP  -  controlar qué recursos puede cargar el browser
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");

    // Desactivar APIs del browser no necesarias
    context.Response.Headers.Append("Permissions-Policy",
        "camera=(), microphone=(), geolocation=()");

    await next();
});

// Ocultar versión del servidor
builder.WebHost.ConfigureKestrel(opts => opts.AddServerHeader = false);
```

---

## HSTS  -  HTTP Strict Transport Security

 HSTS instruye al browser a usar solo HTTPS por un año

```csharp
// Program.cs
builder.Services.AddHsts(options =>
{
    options.Preload           = true;               // incluir en lista de precarga HSTS
    options.IncludeSubDomains = true;               // aplica a *.example.com
    options.MaxAge            = TimeSpan.FromDays(365);
});

// El orden importa: UseHttpsRedirection antes de UseHsts
app.UseHttpsRedirection();   // redirigir HTTP -> HTTPS
app.UseHsts();               // enviar header HSTS en HTTPS

// Header resultante en el response:
// Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

> Primera visita: redirigido a HTTPS.
> Visitas siguientes: el browser va directo a HTTPS sin siquiera intentar HTTP.
> Esto previene ataques de "SSL stripping".

---

<!-- _class: lead -->

# Parte 8
## CORS Correcto

---

## CORS  -  Qué es y qué NO es

**CORS** (Cross-Origin Resource Sharing): política del browser que controla qué dominios pueden hacer requests a tu API.

| Concepto | Descripción |
|----------|-------------|
| **CORS** | Protege tu API de ser llamada desde un dominio no autorizado |
| **CSRF** | Ataque donde un sitio malicioso hace requests autenticadas en nombre del usuario |
| `*` en CORS | Permite cualquier dominio  -  nunca en APIs con autenticación |

---

## CORS  -  Configuración correcta en .NET

 CORS permisivo  -  abre la API a cualquier dominio

```csharp
//  Acepta requests de cualquier origen, método y header
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
// Además: AllowAnyOrigin() y AllowCredentials() son mutuamente excluyentes en .NET
```

 CORS restrictivo  -  solo orígenes conocidos del sistema de mensajería

```csharp
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowedOrigins", policy =>
        policy
            .WithOrigins(
                "https://app.example.com",
                "https://admin.example.com"
            )
            .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
            .WithHeaders("Authorization", "Content-Type")
            .AllowCredentials());   // necesario si se usan cookies HttpOnly
});

app.UseCors("AllowedOrigins");   // antes de UseAuthentication
```

---

## Resumen: Checklist de Seguridad para el sistema de mensajería

| Area | Check | Herramienta |
|------|-------|-------------|
| Inyección | Parámetros en todas las queries | Dapper `@param` / EF Core LINQ |
| Passwords | BCrypt workFactor ≥ 12 | `BCrypt.Net` |
| Rate limiting | Login ≤ 5 intentos / 15 min | `AddRateLimiter` |
| Tokens | HttpOnly Cookie, 15 min exp | `CookieOptions` |
| Headers | X-Frame, CSP, HSTS, nosniff | Middleware + `AddHsts` |
| CORS | Solo dominios propios | `WithOrigins(...)` |
| Secretos | Nunca en Git | User Secrets / Secrets Manager |
| Zero Trust | Verificar ownership en cada handler | Claims + Handler validation |
| Logs | Loguear accesos y fallos | Serilog + CloudWatch |
| Dependencias | Auditar CVEs regularmente | `dotnet audit` / Dependabot |
