# 09 — Monolito Modular con Clean Architecture

Un Monolito Modular combina la simplicidad operacional de un monolito (un solo proceso, un solo deploy) con la separación estructural de un sistema de microservicios (módulos independientes con fronteras explícitas). Clean Architecture define la dirección de las dependencias dentro de cada módulo.

---

## El problema que resuelve

Los microservicios resuelven el problema de escalar equipos y despliegues de forma independiente. Pero tienen un costo alto: red, serialización, distributed tracing, eventual consistency, operaciones complejas.

La mayoría de sistemas SaaS con un solo equipo de desarrollo no necesitan microservicios — necesitan **modularidad sin la complejidad operacional**. El Monolito Modular es ese punto medio.

```
Microservicios          Monolito Modular         Monolito Big Ball of Mud
─────────────────       ──────────────────       ──────────────────────────
✓ fronteras explícitas  ✓ fronteras explícitas   ✗ sin fronteras
✓ deploy independiente  ✗ deploy conjunto         ✗ deploy conjunto
✗ complejidad red       ✓ sin overhead de red     ✓ sin overhead de red
✗ distributed tracing   ✓ tracing simple          ✓ tracing simple
✗ equipo grande needed  ✓ equipo pequeño          ✓ equipo pequeño
```

---

## Ventajas del Monolito Modular
> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.20 Modular Monolith

| Ventaja | Descripción |
|---------|-------------|
| **Gestión simplificada** | Un solo proceso, sin coordinación de múltiples servicios distribuidos |
| **Testing en aislamiento** | Cada módulo se prueba de forma independiente sin afectar a los otros |
| **Deploy único** | Un solo artefacto que desplegar — sin orquestar múltiples servicios |
| **Costo reducido** | Sin infraestructura de microservicios (service mesh, distributed tracing cross-service, múltiples clusters) |
| **Fronteras explícitas** | La modularidad previene el Big Ball of Mud sin el overhead operacional de microservicios |

> Regla práctica: empezar con el enfoque más simple. Un Monolito Modular bien diseñado puede escalar lejos antes de necesitar microservicios.

---

## Proceso de planificación

Antes de escribir código, definir:

```
1. Analizar y modelar el dominio
      → identificar entidades, agregados, relaciones de alto nivel

2. Identificar y diseñar los módulos
      → un módulo = un bounded context (capacidad de negocio cohesiva)
      → si dos módulos interactúan frecuentemente, probablemente son uno solo

3. Identificar las interacciones entre módulos y diseñar integration events
      → qué eventos necesita escuchar cada módulo de los demás
      → nombrar los eventos en pasado: ProductCreated, OrderConfirmed

4. Construir y testear los módulos en aislamiento
5. Integrar en el aggregator y testear la composición
6. Deploy, operar y monitorear el monolito
```

Las fronteras mal definidas son el error más costoso. Si dos módulos están continuamente chateando entre sí, probablemente deberían ser uno.

---

## Estructura de un módulo

Cada módulo es una unidad funcional independiente con **6 proyectos `.csproj`**. Las fronteras son reales en tiempo de compilación — si violas una dependencia, el build falla.

```
Modules/{Modulo}/
├── {Modulo}.Contracts/       → DTOs + interfaces públicas + integration events
│                                (sin dependencias externas de implementación)
├── {Modulo}.Domain/          → Entidades + interfaces de repositorio
│                                (sin dependencias a capas superiores)
├── {Modulo}.Application/     → Handlers + Use Cases
│                                (→ Domain + Contracts + Common)
├── {Modulo}.Infrastructure/  → Repositorios concretos (EF Core, APIs externas)
│                                (→ Domain + Common + Shared.Database)
├── {Modulo}.Presentation/    → Controllers + Presenters
│                                (→ Application + Common + Shared.Web)
└── {Modulo}.Tests/           → Tests de arquitectura + unitarios
```

### Dirección de dependencias

```
Contracts      ──────────── (sin deps de implementación)
                                    ↑
Domain         → Common             ↑
                                    ↑
Application    → Common + Domain + Contracts
                    (NO referencia Infrastructure ni Presentation)
                                    ↑
Infrastructure → Common + Domain + Shared.Database
                    (NO referencia Application ni Presentation)
                                    ↑
Presentation   → Common + Application + Shared.Web
                    (NO referencia Infrastructure)
```

**Regla de oro:** un módulo solo puede referenciar `.Contracts` de otro módulo. Nunca `.Domain`, `.Application`, `.Infrastructure` ni `.Presentation` de otro módulo.

---

## El test de fuego — ¿tu modularidad es real?

Si borras el folder de implementación de un módulo (dejando solo su `.Contracts` como stub), ¿el resto del sistema compila?

```
# Escenario: borrar Users.Application, Users.Infrastructure, Users.Presentation
# Mantener: Users.Contracts (con los DTOs e interfaces públicas)

# Si Authentication.Application todavía compila → modularidad real ✓
# Si Authentication.Application falla → tiene una dependencia oculta → monolito disfrazado ✗
```

`Host.Api` sí va a fallar (es el composition root — le falta registrar los servicios del módulo borrado). Eso es correcto y esperado. `Host.Api` no es un módulo — es el orquestador.

---

## Comunicación entre módulos

Los módulos NO se llaman entre sí directamente. Hay dos mecanismos:

### 1. Contratos síncronos (interfaz pública)

Exponer una interfaz en `.Contracts` que otro módulo puede llamar:

```csharp
// Tenancy.Contracts/Interfaces/ITenancyApi.cs
public interface ITenancyApi
{
    Task<TenantDto?> GetTenantByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsAsync(long id, CancellationToken ct = default);
}

// Tenancy.Application/Api/TenancyApi.cs — implementación
public sealed class TenancyApi : ITenancyApi { ... }

// Authentication.Application/UseCases/Register/RegisterHandler.cs
// referencia Tenancy.Contracts — NO Tenancy.Application
private readonly ITenancyApi _tenancyApi;

var exists = await _tenancyApi.ExistsAsync(request.TenantId, ct);
if (!exists) return new RegisterTenantNotFoundFailure("Tenant no existe.");
```

### 2. Integration Events en proceso

Para comunicación asíncrona dentro del mismo proceso (sin bus de mensajes externo):

```csharp
// Authentication.Contracts/Events/UserShouldBeCreatedIntegrationEvent.cs
public sealed record UserShouldBeCreatedIntegrationEvent(
    Guid PublicId, long TenantId, string FullName, string Email)
    : INotification;

// Authentication.Application/UseCases/Register/RegisterHandler.cs
await _mediator.Publish(new UserShouldBeCreatedIntegrationEvent(
    credential.PublicId, request.TenantId, request.FullName, request.Email));

// Users.Application/IntegrationEventHandlers/UserShouldBeCreatedHandler.cs
public sealed class UserShouldBeCreatedHandler : INotificationHandler<UserShouldBeCreatedIntegrationEvent>
{
    public async Task Handle(UserShouldBeCreatedIntegrationEvent notification, CancellationToken ct)
    {
        await _profiles.InsertAsync(notification.PublicId, notification.TenantId, notification.FullName, ct);
    }
}
```

El mediator (`Common.Messaging.IMediator`) ejecuta todos los handlers del evento en el mismo proceso, dentro del mismo request HTTP, de forma síncrona.

---

## Convenciones compartidas entre módulos
> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.20 Modular Monolith

Los módulos comparten cuatro elementos de infraestructura. Usar el nombre del módulo como discriminador evita conflictos:

### Espacio de URLs

```
/{nombre-módulo}/{recurso}

Ejemplos:
  /products
  /products/123
  /baskets
  /customers/profile
```

Cada módulo se auto-registra en su prefijo de URL:

```csharp
// Baskets/BasketModuleExtensions.cs
public static IEndpointRouteBuilder MapBasketModule(this IEndpointRouteBuilder endpoints)
{
    endpoints
        .MapGroup(Constants.ModuleName.ToLower())  // "baskets"
        .WithTags(Constants.ModuleName)
        .MapFetchItems()
        .MapAddItem();
    return endpoints;
}
```

### Configuración — clave por módulo

```json
// appsettings.json
{
  "Products": {
    "CacheTimeout": "00:05:00"
  },
  "Baskets": {
    "MaxItemsPerBasket": 50
  }
}
```

Patrón: `{NombreMódulo}:{Clave}` — evita colisiones entre módulos.

### Esquema de base de datos por módulo

Cada módulo tiene su propio schema SQL, aunque compartan la misma base de datos:

```csharp
// Products/Data/ProductContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasDefaultSchema(Constants.ModuleName.ToLower()); // "products"
}

// Baskets/Data/BasketContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasDefaultSchema(Constants.ModuleName.ToLower()); // "baskets"
}
```

Resultado: aunque ambos módulos tengan una tabla `Products`, son `products.products` y `baskets.products` — sin conflicto. Cada módulo puede aplicar permisos SQL distintos por schema.

---

## Shared vs Modules

No todos los módulos van en `Modules/`. Algunos son infraestructura transversal:

```
Shared/
├── Database/         → AppDbContext + EntityTypeConfigurations (no es un módulo — es infraestructura)
├── Web/              → BaseApiController (no es un módulo — es infraestructura)
└── Authentication/   → Identidad, JWT (módulo transversal — todos los módulos necesitan auth)
```

`Authentication` vive en `Shared/` porque si estuviera en `Modules/`, todos los demás módulos tendrían que referenciar `Modules/Authentication.Contracts` — haciendo de `Authentication` un "módulo dios". Al estar en `Shared/`, queda claro que es infraestructura compartida.

---

## AppDbContext compartido

A diferencia de los microservicios, los módulos **comparten una sola base de datos y un solo `DbContext`**. Las tablas son "propiedad" lógica de cada módulo, pero físicamente viven en la misma DB.

```csharp
// Shared/Database/AppDbContext.cs — un DbContext para todos los módulos
public sealed class AppDbContext : DbContext
{
    public DbSet<Tenant>         Tenants       { get; set; } = null!;  // Tenancy
    public DbSet<UserCredential> Credentials   { get; set; } = null!;  // Authentication
    public DbSet<RefreshToken>   RefreshTokens { get; set; } = null!;  // Authentication
    public DbSet<UserProfile>    UserProfiles  { get; set; } = null!;  // Users
}
```

Las `EntityTypeConfiguration<T>` viven en `Shared/Database/EntityTypeConfigurations/` — no en el módulo propietario, porque `AppDbContext` las descubre por assembly.

**Consecuencia:** si un módulo Users quiere leer datos de Authentication (por ejemplo, el email), debe hacerlo vía Integration Event o contrato, no accediendo directamente a `_db.Credentials`. Mantiene las fronteras lógicas aunque compartan DB física.

---

## El Composition Root — Host.Api

`Host.Api` es el único lugar donde todos los módulos se conocen entre sí. Registra los servicios de cada módulo y los conecta:

```csharp
// Host.Api/Program.cs — composition root
builder.Services.AddMediator(                          // UNA sola llamada
    typeof(Tenancy.Application.ServiceCollectionEx).Assembly,
    typeof(Users.Application.ServiceCollectionEx).Assembly,
    typeof(Authentication.Application.ServiceCollectionEx).Assembly
);

builder.Services.AddTenancyApplicationServices();
builder.Services.AddTenancyInfrastructureServices();
builder.Services.AddTenancyWebApiServices();

builder.Services.AddUsersApplicationServices();
builder.Services.AddUsersInfrastructureServices();
builder.Services.AddUsersWebApiServices();
// ...
```

`Host.Api` no tiene lógica de negocio — solo composición. Si `Host.Api` falla al borrar un módulo, el fix es quitar 3 líneas de `Program.cs`.

---

## Checklist para un módulo nuevo

- [ ] Crear los 6 proyectos `.csproj` con las dependencias correctas
- [ ] `.Contracts` solo referencia `Common` — sin dependencias de implementación
- [ ] `.Infrastructure` **no** referencia `.Application`
- [ ] `.Application` **no** referencia `.Infrastructure`
- [ ] Crear `{Entidad}Configuration.cs` en `Shared/Database/EntityTypeConfigurations/`
- [ ] Agregar `DbSet<T>` en `AppDbContext`
- [ ] Crear tests de arquitectura (7 reglas de dependencia) en `{Modulo}.Tests/Architecture/`
- [ ] Registrar en `Host.Api/Program.cs` y `Host.Api.csproj`
- [ ] `dotnet build Host.Api/Host.Api.csproj` — 0 errores

---

## Cuándo usar / no usar

**Usar Monolito Modular cuando:**
- Equipo de 1-10 personas
- Dominio bien entendido pero con subdominios distintos
- Quieres las fronteras de microservicios sin el overhead operacional
- El producto está en fase de crecimiento (no de escala masiva)

**Migrar a microservicios cuando:**
- Módulos individuales necesitan escalar de forma independiente
- Equipos distintos necesitan deployar de forma independiente
- El módulo tiene requisitos de disponibilidad diferentes (SLA distinto)

**No usar cuando:**
- El sistema es simple y tiene un solo subdominio — un monolito simple funciona mejor
- El equipo aún no entiende bien las fronteras del dominio — las fronteras incorrectas son peores que ninguna frontera

---

## Retos y problemas comunes
> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.20 Modular Monolith

| Problema | Señal de alerta | Mitigación |
|----------|----------------|------------|
| **Módulo demasiado complejo** | Un módulo hace demasiadas cosas, difícil de mantener | Aplicar SRP: un módulo = una capacidad de negocio. Refactorizar antes de que sea tarde |
| **Fronteras mal definidas** | Dos módulos interactúan constantemente entre sí ("chattiness") | Buena planificación y análisis de dominio. Si dos módulos se hablan mucho, probablemente deberían ser uno |
| **Escalabilidad limitada** | Necesitas escalar solo un módulo pero debes desplegar todo | Migrar ese módulo específico a microservicio. Extraer servicios in-memory a distribuidos (cache distribuido, broker en nube) |
| **Consistencia eventual** | Datos desincronizados entre módulos por comunicación asíncrona | Broker in-memory de baja latencia como primer paso. Para producción, usar un broker resiliente (RabbitMQ, Azure Service Bus) |
| **Transición a microservicios** | El módulo tiene requisitos de escala o deploy independiente | Una arquitectura event-driven desde el inicio facilita la extracción |

---

## Transición a microservicios
> Fuente: *Architecting ASP.NET Core Applications* (Ferreira) — Ch.20 Modular Monolith

No hay obligación de migrar. Un monolito bien construido puede ir muy lejos. Si llega el momento:

```
Estrategia:
1. Colocar un gateway o reverse proxy delante del aggregator (YARP, Azure API Management)
2. Extraer el módulo como microservicio independiente
3. Redirigir las rutas del módulo al nuevo servicio en el proxy
4. Gradualmente transferir el tráfico — el monolito sigue activo como fallback
5. Una vez estable, eliminar el módulo del monolito
```

**Patrón de código compartido:** si varios microservicios necesitan la misma configuración (registros de DI, serialización, logging), crear un assembly compartido. Cuidado: al actualizar ese assembly, se actualizan todos los microservicios — pierden independencia de deploy. En mono-repo esto es referencia directa a proyecto; en multi-repo requiere paquete NuGet versionado.

La arquitectura event-driven del Modular Monolith ya define los contratos de integración entre módulos. Al extraer un módulo, esos contratos se convierten en los contratos entre microservicios — el trabajo grueso ya está hecho.

---

## Relación con el back-template

El back-template implementa este patrón con:
- `Modules/Tenancy/`, `Modules/Users/`, `Shared/Authentication/`
- `Common.Messaging.IMediator` como mecanismo de integration events y mediador de use cases
- `AppDbContext` compartido con global query filters de multi-tenancy
- Tests de arquitectura con `NetArchTest.Rules` — verifican las fronteras en CI

Ver `CLAUDE.md` del back-template para las reglas de implementación específicas.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Monolito Modular | arquitectura donde el sistema se despliega como una sola unidad pero el código está organizado en módulos con fronteras bien definidas |
| Bounded Context | límite dentro del cual un modelo de dominio es válido; cada módulo del monolito representa un Bounded Context |
| Integration Event | mensaje que cruza la frontera entre módulos o servicios; no es un Domain Event — es un contrato de integración |
| Contracts project | proyecto `.Contracts` que expone los DTOs e interfaces públicas de un módulo; es el único punto de entrada para otros módulos |
| Composition Root | punto único donde se registran todas las dependencias del sistema; en el monolito modular es `Host.Api` |
| AppDbContext compartido | único DbContext que agrupa las entidades de todos los módulos, con schemas SQL separados por módulo |
| Schema SQL por módulo | agrupación de tablas bajo un schema de PostgreSQL/SQL Server (`products.items`, `baskets.items`) para evitar colisiones |
| Mediator in-memory | bus de mensajes que opera dentro del mismo proceso; permite que los módulos se comuniquen sin acoplamiento directo |
| Test de arquitectura | prueba automatizada que verifica las reglas de dependencia entre capas o módulos con herramientas como NetArchTest |
| Strangler Fig | patrón para extraer un módulo del monolito a microservicio de forma incremental, redirigiendo el tráfico gradualmente |

---

*Rogelio Arriaga Gonzalez*
