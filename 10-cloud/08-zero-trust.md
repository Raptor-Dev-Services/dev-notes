# 08 — Zero Trust Networks

Zero Trust es el principio de seguridad: "nunca confiar, siempre verificar". Ningún usuario, dispositivo o servicio recibe confianza implícita, incluso dentro de la red interna.

---

## El modelo perimetral vs Zero Trust
> Fuente: *Zero Trust Networks* 2nd Ed. (Gilman, Barth) — Ch.1 Zero Trust Fundamentals

```
Modelo perimetral (viejo):
  Internet ──── Firewall ──── Red interna (confianza total)
  "Si estás dentro del firewall, eres de confianza"
  Problema: si un atacante entra a la red, tiene acceso a todo

Zero Trust:
  Cada request se autentica y autoriza, independientemente del origen
  "Nunca confiar, siempre verificar — incluso dentro de la red interna"
  Si un atacante entra a la red, no tiene acceso automático a nada
```

### Los cinco pilares de Zero Trust

```
1. Verificar identidad       — autenticación fuerte (MFA) para usuarios y servicios
2. Verificar el dispositivo  — solo dispositivos conocidos y sanos acceden
3. Limitar acceso            — least privilege: acceso mínimo necesario
4. Inspeccionar el tráfico   — cifrar y monitorear incluso tráfico interno
5. Asumir breach             — diseñar como si el atacante ya estuviera dentro
```

---

## Autenticación mutua entre servicios (mTLS)

En lugar de que solo el cliente valide el certificado del servidor (TLS normal), con mTLS ambos validan el certificado del otro.

```csharp
// Configurar mTLS en ASP.NET Core — validar el certificado del cliente
builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode          = X509RevocationMode.Online;

        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = ctx =>
            {
                // Verificar que el certificado fue emitido por nuestra CA interna
                var expectedThumbprint = "expected-thumbprint-of-service-cert";
                if (ctx.ClientCertificate.Thumbprint != expectedThumbprint)
                {
                    ctx.Fail("Certificado de cliente no reconocido.");
                    return Task.CompletedTask;
                }

                // Extraer el nombre del servicio del certificado
                var serviceId = ctx.ClientCertificate.GetNameInfo(X509NameType.SimpleName, false);
                var claims    = new[] { new Claim("service_id", serviceId) };
                ctx.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ctx.Scheme.Name));
                ctx.Success();
                return Task.CompletedTask;
            }
        };
    });
```

```csharp
// Cliente que presenta su certificado en las llamadas
public sealed class OrdersServiceClient
{
    private readonly HttpClient _http;

    public OrdersServiceClient(IHttpClientFactory factory)
    {
        // El HttpClient está configurado con el certificado del servicio
        _http = factory.CreateClient("orders-service-mtls");
    }
}

// Configurar el HttpClient con el certificado de cliente
builder.Services.AddHttpClient("orders-service-mtls")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        // Cargar el certificado del servicio (desde Key Vault o archivo)
        var cert = new X509Certificate2(certPath, certPassword);
        handler.ClientCertificates.Add(cert);
        return handler;
    });
```

---

## Least Privilege — acceso mínimo necesario

```csharp
// Policies granulares en lugar de roles amplios
builder.Services.AddAuthorization(options =>
{
    // ❌ Policy amplia — cualquier admin puede hacer cualquier cosa
    options.AddPolicy("Admin", policy => policy.RequireRole("admin"));

    // ✓ Policies por operación específica
    options.AddPolicy("Orders:Read",   policy => policy.RequireClaim("permission", "orders:read"));
    options.AddPolicy("Orders:Write",  policy => policy.RequireClaim("permission", "orders:write"));
    options.AddPolicy("Orders:Delete", policy => policy.RequireClaim("permission", "orders:delete"));
    options.AddPolicy("Reports:Read",  policy => policy.RequireClaim("permission", "reports:read"));
});

// Controller con granularidad por endpoint
[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Orders:Read")]   // solo lectura
    public async Task<IActionResult> GetAll() { ... }

    [HttpPost]
    [Authorize(Policy = "Orders:Write")]  // escritura
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req) { ... }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Orders:Delete")] // eliminación (más restrictivo)
    public async Task<IActionResult> Delete(Guid id) { ... }
}
```

```csharp
// Agregar permisos granulares al token JWT
public string GenerateToken(ExampleUser user, IEnumerable<string> permissions)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.PublicId.ToString()),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    // Permisos específicos — no roles amplios
    foreach (var permission in permissions)
        claims.Add(new Claim("permission", permission));

    // ...
}
```

---

## Segmentación de red en AWS

```hcl
# Terraform — VPC con subredes privadas para los servicios
# Solo el ALB está en subred pública. Los servicios y la DB solo en privadas.

resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
}

# Subredes públicas — solo ALB
resource "aws_subnet" "public" {
  count             = 2
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.${count.index}.0/24"
  availability_zone = data.aws_availability_zones.available.names[count.index]
}

# Subredes privadas — ECS y RDS
resource "aws_subnet" "private" {
  count      = 2
  vpc_id     = aws_vpc.main.id
  cidr_block = "10.0.${count.index + 10}.0/24"
}

# Security Group del API — solo acepta tráfico del ALB
resource "aws_security_group" "api" {
  vpc_id = aws_vpc.main.id

  ingress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]   # solo del ALB, no de internet
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# Security Group de RDS — solo acepta tráfico del ECS
resource "aws_security_group" "rds" {
  vpc_id = aws_vpc.main.id

  ingress {
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.api.id]   # solo del ECS, no de internet
  }
}
```

---

## Auditoría y monitoreo Zero Trust

```csharp
// Loguear TODOS los accesos, no solo los fallidos
public sealed class AccessAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AccessAuditMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Loguear cada request con identidad y resultado
        _logger.LogInformation(
            "ACCESS: {Method} {Path} → {StatusCode} | User: {UserId} | IP: {IP}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            context.User.FindFirst("sub")?.Value ?? "anonymous",
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }
}

// Detectar patrones sospechosos — múltiples 401/403 del mismo IP
builder.Services.AddHealthChecks()
    .AddCheck("suspicious-activity-monitor", () =>
    {
        // En una implementación real, verificar métricas de CloudWatch
        return HealthCheckResult.Healthy("Sin actividad sospechosa detectada.");
    });
```

---

## Principio "Asumir Breach" — diseño defensivo en código

```csharp
// ✓ Validar los datos en cada capa, no solo en la entrada
// Aunque el request venga de otro microservicio interno, validar de todas formas

public sealed class ProcessPaymentHandler
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    public async Task<ProcessPaymentResponse> Handle(
        ProcessPaymentCommand cmd, CancellationToken ct)
    {
        // Validar aunque venga de un servicio interno — principio "asumir breach"
        if (cmd.Amount <= 0)
            return new ProcessPaymentValidationFailure("El monto debe ser positivo.");

        if (cmd.Amount > 100_000)
            return new ProcessPaymentValidationFailure("Monto excede el límite permitido.");

        // Verificar que el customer tiene permiso para este monto
        var customer = await _customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer is null || customer.CreditLimit < cmd.Amount)
            return new ProcessPaymentForbiddenFailure("Límite de crédito insuficiente.");

        // ...
    }
}
```

---

## Cuándo aplicar Zero Trust

| Aplicar | Considerar más adelante |
|---------|------------------------|
| Siempre: least privilege para IAM y roles de DB | mTLS entre servicios (agrega complejidad operacional) |
| Siempre: MFA para cuentas con acceso a producción | Zero Trust Network Access (ZTNA) para acceso remoto |
| Siempre: segmentación de red (privada vs pública) | Micro-segmentación de red interna para equipos grandes |
| Siempre: logs de acceso completos | Análisis de comportamiento de usuarios (UEBA) |


---

## Glosario

| Término | Definición |
|---------|-----------|
| Zero Trust | Modelo de seguridad basado en "nunca confiar, siempre verificar"; ningún usuario o servicio recibe acceso implícito |
| Least Privilege | Principio de otorgar solo los permisos mínimos necesarios para realizar una tarea específica |
| mTLS | Mutual TLS; extensión de TLS donde tanto cliente como servidor presentan y validan sus certificados |
| Modelo perimetral | Arquitectura de seguridad tradicional que confía en todo el tráfico dentro del firewall interno |
| Asumir Breach | Principio de diseñar el sistema como si un atacante ya estuviera dentro de la red |
| Micro-segmentación | División de la red interna en segmentos pequeños con controles de acceso independientes entre ellos |
| Security Group | Firewall virtual en AWS que controla el tráfico entrante y saliente de recursos como EC2 o ECS |
| Policy granular | Política de autorización definida por operación específica (orders:read, orders:write) en lugar de rol amplio |
| CA interna | Autoridad certificadora propia de la organización que emite certificados para la autenticación entre servicios |
| Thumbprint | Huella digital única de un certificado X.509 utilizada para identificarlo sin exponer su contenido |
| Claim de permiso | Valor dentro de un JWT que indica una capacidad específica del portador (ej. `permission: orders:write`) |
| UEBA | User and Entity Behavior Analytics; análisis de comportamiento para detectar anomalías de acceso |

---

*Rogelio Arriaga Gonzalez*
