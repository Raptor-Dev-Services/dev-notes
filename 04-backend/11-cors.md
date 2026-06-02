# 11 — CORS (Cross-Origin Resource Sharing)

CORS es el mecanismo del navegador que restringe peticiones JavaScript a un origen distinto al del documento HTML. En SaaS con frontend y backend en dominios diferentes, CORS es obligatorio.

> Fuente: *Web Application Security 2nd Ed* (Andrew Hoffman) — Ch.15 Same-Origin Policy and CORS  
> Fuente: *ASP.NET Core 9 Essentials* (Packt) — Ch.6 Enhancing Security and Quality

---

## Cómo funciona

CORS no es un mecanismo de seguridad del servidor — es una restricción del **navegador**. El servidor indica qué orígenes puede confiar. Herramientas como Postman o curl no están afectadas por CORS.

```
Preflight request (navegador → servidor):
  OPTIONS /api/users
  Origin: https://app.midominio.com
  Access-Control-Request-Method: POST
  Access-Control-Request-Headers: Authorization

Respuesta del servidor:
  Access-Control-Allow-Origin: https://app.midominio.com
  Access-Control-Allow-Methods: GET, POST, PUT, DELETE
  Access-Control-Allow-Headers: Authorization, Content-Type
  Access-Control-Max-Age: 600   ← cachea el preflight por 10 minutos
```

---

## Configuración en ASP.NET Core

```csharp
// Program.cs

// Leer orígenes permitidos desde configuración
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    // Política para SPA — la más común en SaaS
    options.AddPolicy("SpaPolicy", policy =>
        policy
            .WithOrigins(corsOrigins)          // orígenes específicos (nunca * con credentials)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()                // necesario para cookies y Authorization header
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));

    // Política pública — para endpoints sin autenticación
    options.AddPolicy("PublicPolicy", policy =>
        policy
            .AllowAnyOrigin()                  // cualquier origen
            .AllowAnyHeader()
            .AllowAnyMethod());                // NO se puede usar con AllowCredentials()
});

// CRÍTICO: el orden del middleware importa
app.UseCors("SpaPolicy");      // ANTES de Authentication y Authorization
app.UseAuthentication();
app.UseAuthorization();
```

---

## Wildcard subdomains para multi-tenant

En SaaS con subdominio por tenant (`tenant1.app.com`, `tenant2.app.com`) se necesita lógica dinámica:

```csharp
options.AddPolicy("TenantPolicy", policy =>
    policy.SetIsOriginAllowed(origin =>
    {
        var uri = new Uri(origin);
        // Permitir el dominio raíz y todos sus subdominios
        return uri.Host == "miapp.com"
            || uri.Host.EndsWith(".miapp.com")
            || uri.Host == "localhost";           // para desarrollo local
    })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());
```

---

## CORS por endpoint

```csharp
// Política diferente por endpoint — para APIs públicas dentro de la misma app
app.MapGet("/api/public/plans", GetPublicPlans)
   .RequireCors("PublicPolicy");    // endpoint público: cualquier origen

app.MapControllers()
   .RequireCors("SpaPolicy");       // controllers privados: solo orígenes permitidos
```

---

## Configuración en appsettings.json

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.midominio.com",
      "https://admin.midominio.com"
    ]
  }
}
```

---

## Errores comunes

| Error | Causa | Fix |
|-------|-------|-----|
| `AllowAnyOrigin()` + `AllowCredentials()` | Combinación inválida | Usar `WithOrigins(...)` cuando se necesitan credentials |
| `UseCors` después de `UseRouting` pero antes de `MapControllers` | ✓ correcto | Poner `UseCors` después de `UseRouting` y antes de `UseAuthentication` |
| El preflight falla con 401 | El middleware de auth rechaza OPTIONS antes de CORS | `UseCors` debe ir ANTES de `UseAuthentication` |
| Headers personalizados bloqueados | No están en `AllowAnyHeader()` o `WithHeaders(...)` | Agregar el header a la política |

---

## Glosario

| Término | Definición |
|---------|-----------|
| CORS | Cross-Origin Resource Sharing — mecanismo HTTP que controla qué dominios pueden hacer requests a la API |
| Preflight | Request OPTIONS automático del navegador para verificar si el CORS policy permite la solicitud real |
| Access-Control-Allow-Origin | Header de respuesta que indica qué origen tiene permiso para acceder al recurso |
| AllowCredentials | Configuración CORS que permite enviar cookies y headers de autorización cross-origin |
| SetIsOriginAllowed | Método para validar orígenes dinámicamente con una función — útil para subdomains de tenant |
| WithOrigins | Método de CORS policy que especifica orígenes permitidos de forma explícita |
| SpaPolicy | Nombre de convención para la política CORS destinada al frontend SPA (React/Vite) |
| SetPreflightMaxAge | Configura cuánto tiempo puede el navegador cachear la respuesta del preflight OPTIONS |
| UseCors | Middleware de ASP.NET que aplica la política CORS — debe ir antes de UseAuthentication |
| AllowAnyHeader | Permite cualquier header en requests cross-origin — combinable con WithOrigins específicos |
| CorsPolicyBuilder | Clase builder para construir configuraciones CORS de forma fluida en Program.cs |

---

*Rogelio Arriaga Gonzalez*
