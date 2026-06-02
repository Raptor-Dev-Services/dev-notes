# 10 — Cloud-Native: 12 Factores y Principios de Arquitectura

Una aplicación cloud-native está diseñada desde el principio para aprovechar las características del cloud: elasticidad, resiliencia automática, despliegue independiente y observabilidad integrada.

> Fuente: *ASP.NET Core 9 Essentials* (Packt) — Ch.11 Cloud-Native Development with ASP.NET Core 9

---

## Los 12 Factores (Twelve-Factor App)

Metodología creada por desarrolladores de Heroku para construir aplicaciones modernas, escalables y mantenibles en el cloud. Cada factor aborda una dimensión específica del ciclo de vida de la aplicación.

### Factor 1 — Codebase: un repositorio, múltiples deploys

```
✓ Un repositorio por contexto de aplicación (un repo para la API, otro para infra)
✓ El mismo código se despliega en staging y producción — solo cambia la config
✗ Un repo para staging y otro para producción = divergencia garantizada
```

Buenas prácticas: gestión de ramas (Git Flow o trunk-based), revisiones de código, documentación en el mismo repo.

### Factor 2 — Dependencies: declarar y aislar dependencias

```
✓ Todas las dependencias en el archivo de proyecto (.csproj)
✓ dotnet restore descarga exactamente lo declarado — sin "instalar a mano"
✗ Asumir que la librería X ya está instalada en el servidor
```

```bash
# Restaurar todas las dependencias declaradas
dotnet restore

# Verificar que el build no depende del entorno del host
dotnet build --no-restore
```

### Factor 3 — Config: configuración en el entorno, no en el código

```
✓ Connection strings, API keys, feature flags → variables de entorno o Azure App Configuration
✗ appsettings.json con credenciales de producción commiteadas al repo

# La misma imagen Docker se ejecuta en staging y producción
# La diferencia es solo en las variables de entorno inyectadas
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__MainDb=<valor por ambiente>
```

Ver: `10-cloud/05-secretos-produccion.md` para implementación con Azure Key Vault y Doppler.

### Factor 4 — Backing Services: tratar servicios externos como recursos adjuntos

```
✓ La base de datos, Redis, el servicio de email → referenciados por URL/credencial
✓ Se puede cambiar la DB de staging a producción cambiando una variable
✗ Conexión hardcodeada a un servidor físico específico

Ejemplos de backing services:
  Base de datos      → referenciada por connection string
  Cache              → referenciada por URL de Redis
  Message broker     → referenciado por AMQP URL
  Storage            → referenciado por URL de bucket S3 / Azure Blob
```

La arquitectura hexagonal (ports-and-adapters) implementa este factor a nivel de código: la app no conoce la implementación concreta, solo la interfaz.

### Factor 5 — Build, Release, Run: separar etapas de forma estricta

```
Build   → compilar el código, descargar dependencias, generar artefacto
Release → combinar artefacto + config del ambiente (staging, prod)
Run     → ejecutar el artefacto en el ambiente con su config

✓ CI genera el artefacto (build)
✓ CD lo despliega con config por ambiente (release + run)
✓ Rollback = regresar a la release anterior sin recompilar
✗ Modificar código directamente en el servidor de producción
```

### Factor 6 — Processes: procesos stateless

```
✓ Cada request es independiente — no hay estado en memoria entre requests
✓ El estado persiste en backing services (DB, Redis), no en la app
✗ Guardar sesiones en memoria del proceso (InMemory Session sin Redis)

// ✓ Stateless: el contexto del usuario viene en el JWT de cada request
// No hay estado de sesión en el servidor
app.UseAuthentication();   // extrae claims del token en cada request
app.UseAuthorization();

// ✓ Si necesitas sesión distribuida: usar Redis como backing service
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration["Redis:Connection"]);
builder.Services.AddSession(options =>
    options.IdleTimeout = TimeSpan.FromMinutes(30));
```

### Factor 7 — Port Binding: exportar servicios por binding de puerto

```
# La app escucha en un puerto configurado desde fuera
dotnet run --urls "http://0.0.0.0:8080"

# En Docker: el host mapea su puerto 80 al puerto 8080 del contenedor
docker run -p 80:8080 mi-api

# Cada servicio es identificable por su URL + puerto
# Service A: http://api:8080
# Service B: http://worker:8081
```

### Factor 8 — Concurrency: escalar horizontalmente, no verticalmente

```
Vertical   = agregar más CPU/RAM a un servidor (límite físico)
Horizontal = agregar más instancias del mismo proceso (elástico)

✓ Kubernetes HPA (Horizontal Pod Autoscaler) — escala réplicas de pods
✓ ECS Service desired_count → aumentar réplicas
✓ Azure App Service scale-out → más instancias del mismo contenedor
```

Para trabajo de larga duración → procesos worker separados de los HTTP handlers.

### Factor 9 — Disposability: startup rápido y shutdown limpio

```csharp
// ASP.NET Core 9 — shutdown graceful con CancellationToken
// El host espera a que los requests en vuelo terminen antes de cerrar

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<HostOptions>(options =>
{
    // Tiempo máximo para que los requests en vuelo terminen al hacer shutdown
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// En Background Services: respetar el CancellationToken
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await ProcessBatchAsync(ct);
        await Task.Delay(TimeSpan.FromSeconds(10), ct);
    }
    // Al cancelar el token: finalizar limpiamente, liberar recursos
}
```

### Factor 10 — Dev/Prod Parity: ambientes lo más similares posible

```
✓ Usar Docker en desarrollo → mismo runtime que producción
✓ docker-compose con las mismas versiones de PostgreSQL, Redis, etc. que prod
✗ SQL Server en producción y SQLite en desarrollo
✗ Windows en desarrollo y Linux en producción (diferencias de paths, encodings)
```

### Factor 11 — Logs: tratar logs como streams de eventos

```
✓ Escribir logs a stdout/stderr — el entorno los captura y enruta
✓ No abrir/cerrar archivos de log en la app: el orchestrator los gestiona
✓ Structured logging (JSON) para que el sistema de agregación indexe los campos

// ✓ Serilog escribiendo a stdout con structured JSON
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// En producción: Loki, CloudWatch, Azure Monitor capturan stdout
// La app no sabe ni le importa dónde van los logs
```

### Factor 12 — Admin Processes: tareas administrativas como procesos one-off

```
✓ Migraciones de DB → comando separado en el pipeline de CI/CD, no en startup
✓ Scripts de seed → ejecutar una vez antes del deploy
✗ Correr migraciones en el constructor del DbContext (corre en cada instancia)

# Pipeline de CI/CD:
1. dotnet ef database update     ← admin process one-off
2. kubectl apply -f deploy.yaml  ← deploy de la app
```

---

## The Reactive Manifesto — sistemas resilientes por diseño
> Fuente: *Docker: Up and Running* (Kane, Matthias) — Ch.13 Container Platform Design

Complementario a los 12 factores, el Reactive Manifesto (Jonas Bonér, 2013) define cómo los sistemas deben comportarse ante fallas, carga variable y eventos inesperados.

Un sistema reactivo tiene 4 propiedades:

### Responsive (responsivo)
El sistema responde en tiempo razonable bajo cualquier condición. Si una operación toma tiempo (ej: generar un PDF), responder inmediatamente con "trabajo enviado" y notificar cuando termine — nunca hacer esperar al usuario en un request bloqueante.

```csharp
// ✓ Responsivo: respuesta inmediata con ID de trabajo
[HttpPost("reports/generate")]
public async Task<IActionResult> GenerateReport(ReportRequest request, CancellationToken ct)
{
    var jobId = await _queue.EnqueueAsync(new GenerateReportJob(request), ct);
    return Accepted(new { JobId = jobId, StatusUrl = $"/reports/{jobId}/status" });
    // El cliente consulta el status async — nunca espera bloqueado
}
```

### Resilient (resiliente)
El sistema permanece responsivo ante fallas. Degradar funcionalidad gracefully en lugar de fallar completamente.

```csharp
// ✓ Resiliente: circuit breaker + fallback cuando el servicio externo falla
builder.Services.AddHttpClient<IInventoryClient, InventoryClient>()
    .AddResilienceHandler("inventory", pipeline =>
    {
        pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            BreakDuration = TimeSpan.FromSeconds(30),
            SamplingDuration = TimeSpan.FromSeconds(60),
            FailureRatio = 0.5,    // 50% de errores abre el circuito
        });
        pipeline.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(300),
        });
    });
```

### Elastic (elástico)
El sistema escala horizontalmente para mantener la respuesta bajo carga variable.

```
Docker Swarm:   docker service scale api=8   ← 8 réplicas bajo alta carga
                docker service scale api=2   ← reducir al bajar la carga

Kubernetes HPA: autoscale según CPU/memoria/métricas custom
ECS:            desired_count ajustado por auto-scaling policies
```

### Message Driven (orientado a mensajes)
Los componentes se comunican por mensajes asíncronos para lograr desacoplamiento y absorber picos de carga.

```csharp
// ✓ Message-driven: el handler publica un evento en lugar de llamar directamente
// Si el servicio de email está caído, el mensaje queda en la cola — no se pierde
await _bus.PublishAsync(new UserRegisteredEvent(user.Id, user.Email), ct);
// EmailService consume el evento cuando esté disponible
```

---

## Escalabilidad vertical vs horizontal

| Aspecto | Vertical | Horizontal |
|---------|----------|------------|
| Cómo | Más CPU/RAM a una instancia | Más instancias iguales |
| Límite | Límite físico del hardware | Casi ilimitado |
| Downtime | Requiere reinicio | Sin downtime |
| Complejidad | Simple | Requiere stateless + load balancer |
| Costo | Caro en instancias grandes | Más flexible |
| Uso típico | Bases de datos con estado | APIs stateless |

---

## Checklist cloud-native para .NET

```
Stateless:
  ✓ No hay datos de usuario en memoria entre requests
  ✓ Sesiones en Redis si se necesitan
  ✓ No hay static mutable state compartido entre requests

Config:
  ✓ Sin secretos en el código ni en appsettings.json commiteados
  ✓ Variables de entorno o Key Vault para credenciales
  ✓ La misma imagen funciona en staging y producción

Observabilidad:
  ✓ Structured logging a stdout
  ✓ Health checks en /health/live y /health/ready
  ✓ Métricas exportadas (OpenTelemetry)
  ✓ Trazas distribuidas

Resiliencia:
  ✓ Polly para reintentos y circuit breakers en llamadas externas
  ✓ Graceful shutdown con CancellationToken
  ✓ Startup rápido (< 5 segundos)

Backing services:
  ✓ DB, Redis, S3 referenciados solo por connection string/URL
  ✓ La app puede cambiar de DB con solo cambiar la config
```

---

## Glosario

| Término | Definición |
|---------|-----------|
| Cloud-Native | Enfoque de diseño de aplicaciones que aprovecha la elasticidad, resiliencia y servicios gestionados del cloud |
| Twelve-Factor App | Metodología de 12 prácticas para construir aplicaciones modernas, escalables y portables en el cloud |
| Backing Service | Recurso externo consumido por la app (DB, Redis, S3, broker) referenciado solo por URL o credencial |
| Stateless | Propiedad de un proceso que no guarda estado entre requests; el estado persiste en backing services |
| Graceful Shutdown | Proceso de cierre limpio que espera a que los requests en vuelo terminen antes de detener la app |
| Escalado horizontal | Agregar más instancias del mismo proceso para manejar mayor carga, en contraposición a instancias más grandes |
| Feature Flag | Mecanismo de configuración que activa o desactiva funcionalidades en producción sin desplegar nuevo código |
| Reactive Manifesto | Documento que define los cuatro principios de los sistemas reactivos: responsivo, resiliente, elástico y orientado a mensajes |
| Circuit Breaker | Patrón de resiliencia que abre el circuito y aplica un fallback cuando un servicio externo falla repetidamente |
| HPA | Horizontal Pod Autoscaler; objeto de Kubernetes que escala réplicas según métricas de CPU, memoria u otras |
| Dev/Prod Parity | Principio de mantener los ambientes de desarrollo y producción lo más similares posible para evitar sorpresas |
| Port Binding | Factor que establece que la app exporta su servicio escuchando en un puerto configurado desde el entorno |
| Trunk-based Development | Estrategia donde todos los desarrolladores integran cambios frecuentemente en la rama principal del repositorio |

---

*Rogelio Arriaga Gonzalez*
