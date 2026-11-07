# 07 - Seguridad Web: OWASP Top 10:2025 en .NET

Las vulnerabilidades más comunes en aplicaciones web y cómo prevenirlas en ASP.NET Core.

---

## OWASP Top 10:2025 - resumen rápido
> Fuente: [OWASP Top 10:2025](https://owasp.org/Top10/2025/). Ataques y defensas: *Web Application Security* 2nd Ed. (Andrew Hoffman): Part II Offense y Part III Defense

| # | Categoría | En la lista de 2021 | Ejemplo |
|---|-----------|---------------------|---------|
| A01 | Broken Access Control | A01, ahora incluye SSRF | Usuario accede a datos de otro usuario |
| A02 | Security Misconfiguration | A05 | Headers de seguridad faltantes |
| A03 | Software Supply Chain Failures | A06, ampliada | Dependencia comprometida o pipeline sin controles |
| A04 | Cryptographic Failures | A02 | Passwords en texto plano, HTTP sin TLS |
| A05 | Injection | A03 | SQL injection, command injection |
| A06 | Insecure Design | A04 | Sin rate limiting en login |
| A07 | Authentication Failures | A07, con otro nombre | Tokens de larga vida sin rotación |
| A08 | Software or Data Integrity Failures | A08 | Deserializar sin restringir el tipo |
| A09 | Security Logging and Alerting Failures | A09, con otro nombre | Nadie se entera de los intentos de acceso fallidos |
| A10 | Mishandling of Exceptional Conditions | Nueva | Si el servicio de permisos cae, la app deja pasar |

Tres cambios de fondo respecto a 2021:

- **SSRF dejó de ser categoría propia.** Era la A10 y ahora vive dentro de A01.
- **A03 amplía a la antigua A06.** Ya no habla solo de componentes con vulnerabilidades conocidas, sino de todo lo que interviene al construir, distribuir y actualizar el software.
- **A10 es nueva.** Reúne lo que pasa cuando la app no sabe reaccionar ante un error: falla abierta, transacciones a medias, recursos sin liberar y mensajes que cuentan de más.

---

## A01 - Broken Access Control (Control de acceso roto)

```csharp
// Mal: Confiar en el ID del recurso sin verificar ownership
[HttpGet("{publicId}")]
public async Task<IActionResult> GetOrder(Guid publicId)
{
    var order = await _mediator.Send(new GetOrderRequest(publicId));
    return Ok(order);   // ← cualquier usuario autenticado puede ver cualquier orden
}

// Bien: Verificar que el recurso pertenece al usuario actual
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
// Bien: Usar [Authorize] con policies en lugar de checks manuales
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

### SSRF: el servidor como intermediario

Server-Side Request Forgery ocurre cuando la app hace una petición a una URL que decide el usuario. Desde dentro de tu red, el servidor llega a lo que el atacante no alcanza: otros servicios internos o el endpoint de metadatos del proveedor de nube.

```csharp
// Mal: El usuario decide a dónde llama el servidor
var contenido = await _http.GetStringAsync(request.Url);
// Si Url = "http://169.254.169.254/latest/meta-data/", el servidor consulta los metadatos de la nube

// Bien: Lista de hosts permitidos y solo HTTPS
private static readonly HashSet<string> HostsPermitidos =
    new(StringComparer.OrdinalIgnoreCase) { "api.proveedor.example" };

public static bool EsUrlPermitida(string url) =>
    Uri.TryCreate(url, UriKind.Absolute, out var uri)
    && uri.Scheme == Uri.UriSchemeHttps
    && HostsPermitidos.Contains(uri.Host);

// Bien: Sin seguir redirecciones: una URL permitida no debe poder mandarte a otro host
builder.Services.AddHttpClient("proveedor")
    .ConfigurePrimaryHttpMessageHandler(() =>
        new SocketsHttpHandler { AllowAutoRedirect = false });
```

La última línea importa porque `HttpClient` sigue redirecciones por omisión: un host permitido que responde con un 302 hacia un servicio interno lo alcanza igual.

---

## A02 - Security Misconfiguration (Headers de seguridad)

```csharp
// En producción — deshabilitar el header de la versión del servidor
builder.WebHost.ConfigureKestrel(opts =>
    opts.AddServerHeader = false);   // elimina "Server: Kestrel" del response

// Nunca exponer detalles de excepción en producción
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();   // responde con ProblemDetails, sin pila ni mensaje interno

// Program.cs — headers de seguridad
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;

        // Prevenir clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Prevenir MIME sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Referrer policy
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy — controlar qué recursos puede cargar el browser
        headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'";

        // HSTS — solo HTTPS (también configurar con UseHsts)
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

        // Permissions Policy — desactivar APIs del browser no necesarias
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        return Task.CompletedTask;
    });

    await next();
});
```

Los headers van dentro de `OnStarting` por una razón medida: si se agregan antes de `next()`, la respuesta 200 los lleva, pero `UseExceptionHandler` limpia la respuesta antes de escribir el error y el 500 sale sin ninguno. Con `OnStarting` salen en el 200, en el 404 y en el 500.

---

## A03 - Software Supply Chain Failures (Cadena de suministro)

Cubre cualquier compromiso al construir, distribuir o actualizar el software: dependencias directas y transitivas, herramientas de CI/CD, imágenes y registros. OWASP cita casos como SolarWinds (2019), el robo a Bybit (2025) y el gusano de npm Shai-Hulud (2025). Es la categoría que más votos recibió en su encuesta a la comunidad.

```xml
<!-- Directory.Build.props — auditar en cada restore y fijar lo que se instala -->
<Project>
  <PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>     <!-- también las dependencias transitivas -->
    <NuGetAuditLevel>low</NuGetAuditLevel>
    <WarningsAsErrors>$(WarningsAsErrors);NU1900;NU1901;NU1902;NU1903;NU1904</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

```bash
# En CI: restaurar exactamente lo que dice packages.lock.json
dotnet restore --locked-mode

# Inventario de vulnerabilidades, incluidas las transitivas
dotnet list package --vulnerable --include-transitive

# SBOM: la lista de lo que realmente entregas
syft scan dir:. -o cyclonedx-json=sbom.cdx.json
```

Lo que cada control detiene, medido con el SDK 10.0.300:

| Situación | Resultado |
|-----------|-----------|
| Paquete con vulnerabilidad conocida | `NU1903`, el restore falla |
| Alguien cambió una versión sin regenerar el lockfile | `NU1004`, `--locked-mode` falla |
| El paquete descargado no coincide con el hash del lockfile | `NU1403`, el restore falla |

La cadena no termina en NuGet: las acciones de GitHub se fijan por SHA, como explica [Checklists de verificación](../09-cicd/01-checklists.md), y las imágenes base entran en el mismo inventario.

---

## A04 - Fallos criptográficos

```csharp
// Mal: MD5 o SHA1 para passwords — crackeables con rainbow tables
var hash = MD5.HashData(Encoding.UTF8.GetBytes(password));

// Mal: Modo ECB — no autentica, y dos bloques iguales cifran igual
var aes = Aes.Create();
aes.Mode = CipherMode.ECB;

// Bien: AES-GCM — modo autenticado; si alguien altera el dato, Decrypt lanza una excepción
var nonce      = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
var ciphertext = new byte[plaintext.Length];
var tag        = new byte[AesGcm.TagByteSizes.MaxSize];

using var gcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);   // key de 256 bits de preferencia
gcm.Encrypt(nonce, plaintext, ciphertext, tag);

// Bien: BCrypt para passwords — diseñado para ser lento (protege contra brute force)
var hash  = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
var valid = BCrypt.Net.BCrypt.Verify(password, hash);

// Bien: HTTPS obligatorio en producción
builder.Services.AddHsts(options =>
{
    options.Preload           = true;
    options.IncludeSubDomains = true;
    options.MaxAge            = TimeSpan.FromDays(365);
});

// Redirigir HTTP a HTTPS
app.UseHttpsRedirection();
app.UseHsts();

// No loguear datos sensibles
logger.LogInformation("Login attempt for {Email}", email);   // Bien: email (identificador)
logger.LogInformation("Password: {Password}", password);     // nunca loguear passwords
```

Dos precisiones de OWASP. Para contraseñas recomienda Argon2id, luego scrypt, y deja bcrypt para sistemas heredados, con un factor de trabajo mínimo de 10 y un límite de 72 bytes de entrada: el ejemplo usa bcrypt porque es lo que trae back-template. Para AES pide una clave de al menos 128 bits, idealmente 256, y modos autenticados como GCM.

---

## A05 - Injection (SQL y Command Injection)

```csharp
// Mal: SQL Injection — concatenar input del usuario en SQL
public async Task<ExampleUser?> GetByEmailAsync(string email)
{
    var sql = $"SELECT * FROM ExampleUsers WHERE Email = '{email}'";
    // Si email = "' OR '1'='1" → devuelve todos los usuarios
    // Si email = "'; DROP TABLE ExampleUsers; --" → borra la tabla
    return await _db.QuerySingleOrDefaultAsync<ExampleUser>(sql);
}

// Bien: Parámetros — nunca concatenar valores del usuario en SQL
public async Task<ExampleUser?> GetByEmailAsync(string email, CancellationToken ct)
    => await _db.QuerySingleOrDefaultAsync<ExampleUser>(
        "SELECT * FROM ExampleUsers WHERE Email = @email",
        new { email },   // ← Dapper/EF Core escapan automáticamente
        cancellationToken: ct);
```

```csharp
// Mal: Command Injection — pasar input del usuario por un shell
public async Task<string> ConvertFileAsync(string fileName)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo("/bin/sh", $"-c \"convert {fileName} output.pdf\"")
        // Si fileName = "input.jpg && rm -rf /", el shell ejecuta ambos comandos
    };
    process.Start();
    // ...
}

// Bien: Validar y sanitizar el nombre del archivo antes de usarlo
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

El shell es lo que convierte `&&` o `;` en un segundo comando. `ProcessStartInfo` sin shell no los interpreta, y `UseShellExecute` vale `false` por omisión en .NET. Concatenar sigue siendo mala idea aun así: el texto se parte en argumentos, y uno que empiece con guion puede cambiar lo que hace el programa. Por eso la versión segura usa `ArgumentList`.

---

## A06 - Insecure Design: Rate Limiting

```csharp
// Rate limiting en el endpoint de login — proteger contra brute force
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;   // por omisión responde 503

    opts.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",    // un contador por IP
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window      = TimeSpan.FromMinutes(15),
            QueueLimit  = 0
        }));
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

La partición por IP es parte del diseño. Con `AddFixedWindowLimiter("login", ...)` hay un solo contador para todos: quien agota los 5 intentos deja sin login a los demás, y el límite contra fuerza bruta se vuelve una forma de negación de servicio. Detrás de un proxy, `RemoteIpAddress` es la del proxy hasta configurar `UseForwardedHeaders`. Más políticas en [Rate Limiting](../04-backend/12-rate-limiting.md).

---

## A07 - Fallos de autenticación: manejo seguro de tokens

```csharp
// Bien: Guardar tokens de forma segura en el cliente
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

// Bien: Invalidar la cookie en logout
[HttpPost("logout")]
[Authorize]
public IActionResult Logout()
{
    Response.Cookies.Delete("access_token");
    return Ok();
}
```

---

## A08 - Software or Data Integrity Failures

Código o datos que se aceptan como confiables sin verificarlo. Según OWASP, mira la integridad a un nivel más bajo que A03: cada artefacto, actualización o dato crítico. La deserialización insegura vive aquí.

```csharp
// Mal: El JSON decide qué tipo se instancia: "$type" puede crear cualquier clase cargada
var datos = JsonConvert.DeserializeObject<object>(json,
    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

// Bien: System.Text.Json con un DTO concreto: "$type" se ignora
var pedido = JsonSerializer.Deserialize<PedidoDto>(json);
```

`BinaryFormatter` ya no es una opción: en .NET 10 lanza `PlatformNotSupportedException`.

---

## A09 - Security Logging and Alerting Failures

OWASP cambió el nombre para subrayar la alerta: un registro que nadie mira no provoca ninguna reacción. Hay que registrar los fallos de login, de acceso y de validación, y además avisar cuando se disparan.

```csharp
// Bien: Registrar el evento sin datos sensibles, y contarlo para poder alertar
private static readonly Counter<long> LoginsFallidos =
    new Meter("Seguridad.Auth").CreateCounter<long>("auth.login.failed");

_logger.LogWarning("Login fallido para {Email} desde {Ip}", request.Email, ip);
LoginsFallidos.Add(1);
```

```yaml
# Regla de Prometheus: avisar cuando la tasa se sostiene
groups:
  - name: seguridad
    rules:
      - alert: LoginsFallidosAltos
        expr: rate(auth_login_failed_total[5m]) > 1
        for: 5m
        labels:
          severity: warning
```

Cómo llegan esas métricas a Prometheus está en [Observabilidad](04-observabilidad.md).

---

## A10 - Mishandling of Exceptional Conditions

Categoría nueva. Trata de la app que no previene una condición anormal, no la detecta o reacciona mal. OWASP lo resume en una frase: cada vez que la aplicación no está segura de su siguiente instrucción, una condición excepcional se manejó mal.

### Fallar cerrado

```csharp
// Mal: Falla abierta: si el servicio de permisos cae, todos pasan
public async Task<bool> PuedeVerAsync(Guid userId, Guid recursoId, CancellationToken ct)
{
    try { return await _permisos.TieneAccesoAsync(userId, recursoId, ct); }
    catch { return true; }
}

// Bien: Falla cerrada: ante la duda se niega, y queda registrado
public async Task<bool> PuedeVerAsync(Guid userId, Guid recursoId, CancellationToken ct)
{
    try { return await _permisos.TieneAccesoAsync(userId, recursoId, ct); }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Servicio de permisos caído al evaluar {RecursoId}", recursoId);
        return false;
    }
}
```

### Deshacer lo que quedó a medias

```csharp
// Mal: Sin transacción: si cae entre los dos pasos, el dinero desaparece
await db.ExecuteAsync("UPDATE cuentas SET saldo = saldo - @monto WHERE id = @origen", datos);
await db.ExecuteAsync("UPDATE cuentas SET saldo = saldo + @monto WHERE id = @destino", datos);

// Bien: Todo o nada: sin CommitAsync, la transacción se revierte al salir del bloque
await using var tx = await db.BeginTransactionAsync(ct);
await db.ExecuteAsync("UPDATE cuentas SET saldo = saldo - @monto WHERE id = @origen", datos, tx);
await db.ExecuteAsync("UPDATE cuentas SET saldo = saldo + @monto WHERE id = @destino", datos, tx);
await tx.CommitAsync(ct);
```

Con saldos de 100 y 0, y una caída entre los dos pasos, sin transacción quedan 60 y 0. Con transacción siguen en 100 y 0.

### Liberar los recursos

```csharp
// Si ProcesarAsync lanza, Dispose nunca corre y el archivo queda abierto
var stream = File.OpenRead(ruta);
await ProcesarAsync(stream, ct);
stream.Dispose();

// Bien: await using libera también cuando hay excepción
await using var stream = File.OpenRead(ruta);
await ProcesarAsync(stream, ct);
```

### No contar de más y poner límites

Un error detallado es información para el atacante: el `detail` de la respuesta nunca lleva `exception.Message`. El manejador global de [Problem Details](../04-backend/08-problem-details.md) es la red de seguridad, no la estrategia: el error se atiende donde ocurre, y lo que llega hasta él se registra y responde sin datos internos.

Prevenir también cuenta. Los límites de tasa, las cuotas y los topes de tamaño evitan que la condición excepcional llegue a ocurrir. En palabras de OWASP, nada en tecnologías de la información debería ser ilimitado.

---

## CORS - configuración segura

OWASP cuenta una mala configuración de CORS dentro de A01.

```csharp
// Mal: CORS permisivo — acepta cualquier origen
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Bien: CORS restrictivo — solo origenes conocidos
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
| SSRF | Server-Side Request Forgery; vulnerabilidad donde la app realiza peticiones a URLs controladas por el atacante. En 2025 forma parte de A01 |
| CORS | Cross-Origin Resource Sharing; mecanismo que controla qué orígenes pueden hacer peticiones a la API |
| MIME Sniffing | Proceso del navegador de inferir el tipo de contenido; mitigado con la cabecera X-Content-Type-Options: nosniff |
| Cadena de suministro | Todo lo que interviene al construir, distribuir y actualizar el software: dependencias, herramientas, pipelines y registros |
| SBOM | Software Bill of Materials; inventario de los componentes que forman parte de lo que se entrega |
| Lockfile | Archivo con las versiones exactas y el hash de cada dependencia; en .NET, `packages.lock.json` |
| Falla abierta | Comportamiento inseguro donde, ante un error, el sistema concede lo que debía negar. Lo contrario es fallar cerrado |
| Deserialización insegura | Reconstruir objetos a partir de datos que el atacante puede modificar, dejándole elegir tipos o contenido |

---

*Rogelio Arriaga Gonzalez*
