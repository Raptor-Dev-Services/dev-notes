# 07 — Seguridad Web: OWASP Top 10 en .NET

Las vulnerabilidades más comunes en aplicaciones web y cómo prevenirlas en ASP.NET Core.

---

## OWASP Top 10 — resumen rápido
> Fuente: *Web Application Security* 2nd Ed.: Ch.1-5 OWASP vulnerabilities

| # | Vulnerabilidad | Ejemplo |
|---|---------------|---------|
| A01 | Broken Access Control | Usuario accede a datos de otro usuario |
| A02 | Cryptographic Failures | Passwords en texto plano, HTTP sin TLS |
| A03 | Injection | SQL injection, command injection |
| A04 | Insecure Design | Sin rate limiting en login |
| A05 | Security Misconfiguration | Headers de seguridad faltantes |
| A06 | Vulnerable Components | Dependencias con CVEs conocidos |
| A07 | Auth Failures | Tokens de larga vida sin rotación |
| A08 | Software Integrity | Sin validación de dependencias (supply chain) |
| A09 | Logging Failures | No loguear intentos de acceso fallidos |
| A10 | Server-Side Request Forgery | La app hace requests a URLs del usuario |

---

## A01 — Broken Access Control (Control de acceso roto)

```csharp
// ❌ Confiar en el ID del recurso sin verificar ownership
[HttpGet("{publicId}")]
public async Task<IActionResult> GetOrder(Guid publicId)
{
    var order = await _mediator.Send(new GetOrderRequest(publicId));
    return Ok(order);   // ← cualquier usuario autenticado puede ver cualquier orden
}

// ✓ Verificar que el recurso pertenece al usuario actual
[HttpGet("{publicId}")]
[Authorize]
public async Task<IActionResult> GetOrder(Guid publicId)
{
    var currentUserId = User.GetUserId();
    var order = await _mediator.Send(new GetOrderRequest(publicId, currentUserId));
    //                                                             ↑ el handler filtra por usuario
    return Ok(order);
}

// En el Handler — verificar ownership
public async Task<GetOrderResponse> Handle(GetOrderRequest request, CancellationToken ct)
{
    var order = await _repo.GetByPublicIdAsync(request.PublicId, ct);

    if (order is null)
        return new GetOrderNotFoundFailure("Pedido no encontrado.");

    // Verificar que el pedido pertenece al usuario que hace la request
    if (order.CustomerId != request.RequestingUserId)
        return new GetOrderForbiddenFailure("Acceso denegado.");

    return new GetOrderSuccess(new OrderDto(order));
}
```

```csharp
// ✓ Usar [Authorize] con policies en lugar de checks manuales
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrderOwner", policy =>
        policy.Requirements.Add(new ResourceOwnerRequirement()));
});

public sealed class ResourceOwnerRequirement : IAuthorizationRequirement { }

public sealed class OrderOwnerHandler
    : AuthorizationHandler<ResourceOwnerRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement,
        Order resource)
    {
        if (resource.CustomerId == context.User.GetUserId())
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

---

## A03 — Injection (SQL y Command Injection)

```csharp
// ❌ SQL Injection — concatenar input del usuario en SQL
public async Task<ExampleUser?> GetByEmailAsync(string email)
{
    var sql = $"SELECT * FROM ExampleUsers WHERE Email = '{email}'";
    // Si email = "' OR '1'='1" → devuelve todos los usuarios
    // Si email = "'; DROP TABLE ExampleUsers; --" → borra la tabla
    return await _db.QuerySingleOrDefaultAsync<ExampleUser>(sql);
}

// ✓ Parámetros — nunca concatenar valores del usuario en SQL
public async Task<ExampleUser?> GetByEmailAsync(string email, CancellationToken ct)
    => await _db.QuerySingleOrDefaultAsync<ExampleUser>(
        "SELECT * FROM ExampleUsers WHERE Email = @email",
        new { email },   // ← Dapper/EF Core escapan automáticamente
        cancellationToken: ct);
```

```csharp
// ❌ Command Injection — ejecutar comandos del sistema con input del usuario
public async Task<string> ConvertFileAsync(string fileName)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo("convert", $"{fileName} output.pdf")
        // Si fileName = "input.jpg && rm -rf /", ejecuta ambos comandos
    };
    process.Start();
    // ...
}

// ✓ Validar y sanitizar el nombre del archivo antes de usarlo
public async Task<string> ConvertFileAsync(string fileName)
{
    // Validar que solo contiene caracteres permitidos
    if (!Regex.IsMatch(fileName, @"^[a-zA-Z0-9_\-\.]+$"))
        throw new ArgumentException("Nombre de archivo inválido.");

    // Usar un directorio seguro y validar que el path no escapa del sandbox
    var safePath = Path.GetFullPath(Path.Combine(_uploadDir, fileName));
    if (!safePath.StartsWith(_uploadDir))
        throw new ArgumentException("Path inválido.");

    var process = new Process
    {
        StartInfo = new ProcessStartInfo("convert")
        {
            ArgumentList = { safePath, "output.pdf" }   // ← array de argumentos (no concatenación)
        }
    };
    // ...
}
```

---

## A02 — Fallos criptográficos

```csharp
// ❌ MD5 o SHA1 para passwords — crackeables con rainbow tables
var hash = MD5.HashData(Encoding.UTF8.GetBytes(password));

// ❌ Cifrado débil
var aes = Aes.Create();
aes.KeySize = 128;   // ← demasiado corto para 2024

// ✓ BCrypt para passwords — diseñado para ser lento (protege contra brute force)
var hash  = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
var valid = BCrypt.Net.BCrypt.Verify(password, hash);

// ✓ HTTPS obligatorio en producción
builder.Services.AddHsts(options =>
{
    options.Preload           = true;
    options.IncludeSubDomains = true;
    options.MaxAge            = TimeSpan.FromDays(365);
});

// Redirigir HTTP a HTTPS
app.UseHttpsRedirection();
app.UseHsts();

// ✓ No loguear datos sensibles
logger.LogInformation("Login attempt for {Email}", email);   // ✓ email (identificador)
logger.LogInformation("Password: {Password}", password);     // ❌ nunca loguear passwords
```

---

## A05 — Security Misconfiguration (Headers de seguridad)

```csharp
// Program.cs — headers de seguridad
app.Use(async (context, next) =>
{
    // Prevenir clickjacking
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // Prevenir MIME sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // Referrer policy
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy — controlar qué recursos puede cargar el browser
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");

    // HSTS — solo HTTPS (también configurar con UseHsts)
    context.Response.Headers.Append("Strict-Transport-Security",
        "max-age=31536000; includeSubDomains; preload");

    // Permissions Policy — desactivar APIs del browser no necesarias
    context.Response.Headers.Append("Permissions-Policy",
        "camera=(), microphone=(), geolocation=()");

    await next();
});

// En producción — deshabilitar el header de la versión del servidor
builder.WebHost.ConfigureKestrel(opts =>
    opts.AddServerHeader = false);   // elimina "Server: Kestrel" del response

// Nunca exponer detalles de excepción en producción
builder.Services.AddProblemDetails(opts =>
{
    opts.CustomizeProblemDetails = ctx =>
    {
        // En producción, no incluir el stack trace
        if (!ctx.HttpContext.RequestServices
                .GetRequiredService<IHostEnvironment>().IsDevelopment())
            ctx.ProblemDetails.Extensions.Remove("exception");
    };
});
```

---

## A04 — Insecure Design: Rate Limiting

```csharp
// Rate limiting en el endpoint de login — proteger contra brute force
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit         = 5;
        limiter.Window              = TimeSpan.FromMinutes(15);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit           = 0;
    });
});

app.UseRateLimiter();

// Aplicar al controller de auth
[HttpPost("login")]
[EnableRateLimiting("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // ...
}
```

---

## A07 — Fallos de autenticación: manejo seguro de tokens

```csharp
// ✓ Guardar tokens de forma segura en el cliente
// En SPA: HttpOnly Cookie (no accesible por JavaScript) > localStorage

// En el servidor — devolver el access token en cookie HttpOnly
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
    if (result is LoginFailure failure)
        return Unauthorized(new { error = failure.Message });

    var success = (LoginSuccess)result;

    // Access token en cookie HttpOnly + Secure
    Response.Cookies.Append("access_token", success.AccessToken, new CookieOptions
    {
        HttpOnly  = true,    // no accesible por JavaScript (protege contra XSS)
        Secure    = true,    // solo HTTPS
        SameSite  = SameSiteMode.Strict,
        Expires   = DateTimeOffset.UtcNow.AddMinutes(15)
    });

    return Ok(new { message = "Login exitoso." });
}

// ✓ Invalidar la cookie en logout
[HttpPost("logout")]
[Authorize]
public IActionResult Logout()
{
    Response.Cookies.Delete("access_token");
    return Ok();
}
```

---

## CORS — configuración segura

```csharp
// ❌ CORS permisivo — acepta cualquier origen
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ✓ CORS restrictivo — solo origenes conocidos
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowedOrigins", policy =>
        policy
            .WithOrigins(
                "https://app.raptordev.io",
                "https://www.raptordev.io"
            )
            .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
            .WithHeaders("Authorization", "Content-Type")
            .AllowCredentials());   // necesario si se usan cookies
});

app.UseCors("AllowedOrigins");
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| OWASP | Open Worldwide Application Security Project; organización que publica el Top 10 de vulnerabilidades web más críticas |
| SQL Injection | Ataque que inyecta código SQL en consultas al concatenar input del usuario sin parametrizar |
| Broken Access Control | Fallo que permite a un usuario acceder a recursos que pertenecen a otro usuario u operación |
| HSTS | HTTP Strict Transport Security; cabecera que fuerza al navegador a usar solo HTTPS durante el tiempo definido |
| CSP | Content Security Policy; cabecera que controla qué recursos (scripts, estilos, imágenes) puede cargar el navegador |
| Clickjacking | Ataque donde un iframe invisible superpone la UI legítima para engañar al usuario; mitigado con X-Frame-Options |
| BCrypt | Algoritmo de hashing diseñado para contraseñas, intencionalmente lento para resistir ataques de fuerza bruta |
| Rate Limiting | Mecanismo que limita el número de peticiones por tiempo a un endpoint para prevenir ataques de fuerza bruta |
| HttpOnly Cookie | Cookie no accesible por JavaScript; protege el token de sesión contra robo por ataques XSS |
| SSRF | Server-Side Request Forgery; vulnerabilidad donde la app realiza peticiones a URLs controladas por el atacante |
| CORS | Cross-Origin Resource Sharing; mecanismo que controla qué orígenes pueden hacer peticiones a la API |
| MIME Sniffing | Proceso del navegador de inferir el tipo de contenido; mitigado con la cabecera X-Content-Type-Options: nosniff |

---

*Rogelio Arriaga Gonzalez*
