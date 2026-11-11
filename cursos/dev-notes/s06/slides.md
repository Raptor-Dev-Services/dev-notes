<!-- _class: lead -->

# S06  -  Arquitectura SaaS
## DDD - CQRS - Clean Architecture - Multi-Tenant - Billing - Roles

**Dev Notes**
Stack: .NET 10 - back-template - EF Core 10 - Dapper

---

## Agenda

- Revision homework S05 + dudas
- **Bloque I**  -  DDD - CQRS - Specification - Outbox
- **Bloque II**  -  Clean Architecture - Vertical Slice - Monolito Modular - back-template
- **Bloque III**  -  SaaS Multi-Tenant - Modelos de aislamiento - EF Core global filter
- **Bloque IV**  -  Billing - Planes - JWT - Metricas SaaS
- Quiz + retrospectiva + homework

> S06 es sesion de teoria pura. El primer codigo del producto arranca más adelante.

---

<!-- _class: lead -->

## El problema que resuelven estas arquitecturas
### Crecimiento sin caos

---

## El arco del caos

```
Mes 1:
  ContactsController.cs  -  60 líneas, todo claro

Mes 6:
  ContactsController.cs  -  900 líneas
  Llama directo a EF Core, valida inputs,
  envía emails, importa CSV, calcula segmentos
  Nadie entiende qué puede tocar sin romper algo

Mes 12:
  "Necesitamos reescribirlo desde cero"
```

**Causa raíz:** mezcla de responsabilidades.
El Controller sabe demasiado y nadie puso límites claros.

**La solución:** separar capas por responsabilidad,
establecer reglas de dependencia que el compilador pueda verificar.

---

<!-- _class: lead -->

## Domain-Driven Design (DDD)
### Modelar el software según el dominio del negocio

---

## DDD  -  Conceptos clave

| Concepto | Definición | Ejemplo en el sistema de mensajería |
|----------|-----------|--------------------------|
| **Entity** | Objeto con identidad propia  -  se distingue por su Id | `Contact`, `Conversation`, `Agent` |
| **Value Object** | Objeto sin identidad  -  se distingue por su valor | `Email`, `ConversationStatus` |
| **Aggregate** | Cluster de entidades con una raíz que garantiza invariantes | `Conversation` + `Message[]` |
| **Aggregate Root** | Única entrada para modificar el cluster | `Conversation`  -  no se agrega `Message` directo |
| **Repository** | Abstracción de persistencia para un Aggregate Root | `IConversationRepository` |
| **Domain Service** | Lógica de negocio que no pertenece a una sola entidad | `IConversationAssignmentStrategy` |
| **Domain Event** | Hecho relevante que ocurrió en el dominio | `ConversationAssigned`, `ContactImported` |
| **Bounded Context** | Límite explícito donde un modelo tiene significado | Módulo `Conversations`, módulo `Contacts` |

---

## DDD  -  Por que nacio

```
1990s: el software empresarial se volvio tan complejo que los
desarrolladores perdian la capacidad de entender el negocio que modelaban.

El problema de la epoca:
  Los expertos del negocio hablaban de "conversaciones", "agentes" y "cierres".
  Los desarrolladores hablaban de "records", "rows" y "objects".
  Nadie entendia al otro.
  El codigo resultante no reflejaba el negocio real.
  Cada cambio en las reglas de negocio requeria entender el codigo de cero.

Eric Evans, 2003  -  "Domain-Driven Design: Tackling Complexity in the Heart of Software":
  El modelo del software debe ser un reflejo fiel del dominio del negocio.
  El codigo debe hablar el mismo idioma que el negocio.
  Los expertos del dominio y los desarrolladores deben colaborar constantemente.
```

DDD no es una arquitectura tecnica  -  es una filosofia de colaboracion.
El objetivo es que el codigo sea tan legible para el negocio
como lo es para el equipo tecnico.

---

## DDD  -  Lenguaje ubicuo en el sistema de mensajería

El mismo termino puede significar cosas distintas segun quien lo usa.
DDD llama a esto "Ubiquitous Language": un vocabulario compartido
entre developers y stakeholders, usado igual en codigo, documentos y conversaciones.

| Termino | Lo que entiende Ventas | Lo que entiende el Dev | El modelo correcto |
|---------|----------------------|----------------------|-------------------|
| "Contacto" | Prospecto que puede comprar | Una tabla en la DB | `Contact` con `Tags[]`, `Channel`, `Segments` |
| "Conversacion" | Negociacion larga | Hilo de mensajes | `Conversation` con `Status`, `AssignedAgentId`, `Channel` |
| "Agente" | Vendedor | Usuario del sistema | `Agent` con `Role`, `WorkspaceId`, `IsOnline` |
| "Canal" | Medio de comunicacion | Integracion con API | `Channel` con `Type`, `Config`, `IsActive` |
| "Cierre" | Venta concretada | No existe en el dominio | `ConversationResolved` (Domain Event) |

**La regla de oro:** si el termino existe en el lenguaje del negocio,
debe existir exactamente igual en el codigo.
Si el Dev llama `ConvRecord` a lo que el negocio llama "conversacion", hay un problema.

---

## DDD  -  Entity vs Value Object

```csharp
// Entity  -  tiene Id, puede mutar, se rastrea en DB
public sealed class Contact
{
    public long   Id        { get; init; }
    public Guid   PublicId  { get; init; }
    public string FullName  { get; init; } = string.Empty;
    public bool   IsActive  { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
```

```csharp
// Value Object  -  sin Id, inmutable, se compara por valor
public sealed record Email
{
    public string Value { get; }
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))   throw new ArgumentException("Email vacío.");
        if (!value.Contains('@'))               throw new ArgumentException("Email inválido.");
        if (!value.Split('@')[1].Contains('.')) throw new ArgumentException("Dominio inválido.");
        Value = value.Trim().ToLowerInvariant();
    }
    public static implicit operator string(Email e) => e.Value;
}
```

---

## DDD  -  Aggregate Root

**Regla:** solo se modifica el estado de un Aggregate a través de su raíz.
Nunca se accede directamente a las entidades hijas  -  la raíz garantiza los invariantes.

```csharp
// Aggregate Root: Conversation
public sealed class Conversation
{
    private readonly List<Message> _messages = [];
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();
    public ConversationStatus Status { get; private set; }

    // Solo la raíz puede agregar mensajes  -  garantiza invariantes
    public Result AddMessage(string content, Guid senderId)
    {
        if (Status == ConversationStatus.Archived)
            return Result.Fail("No se puede enviar mensajes a una conversación archivada.");

        _messages.Add(new Message(Guid.NewGuid(), Id, content, senderId, DateTime.UtcNow));
        return Result.OK();
    }
}
```

```csharp
//  Incorrecto  -  bypassa la lógica de la raíz
var msg = new Message(...);
_db.Messages.Add(msg);  // no valida si la conversación está archivada
```

---

## DDD  -  Bounded Context

**Concepto:** diferentes partes del sistema tienen modelos distintos para el mismo concepto.

```
Contexto "Conversations"          Contexto "Contacts"
  Contact = {                       Contact = {
    ConversationId,                   FullName, Email,
    AssignedAgentId,                  Tags, Segments,
    Channel, Status                   ImportedAtUtc
  }                                 }
```

La misma entidad "Contact" tiene atributos completamente diferentes en cada contexto.
En el back-template, cada módulo es un Bounded Context.

**Regla:** los módulos no comparten entidades de dominio.
Si `Conversations` necesita un dato de `Contacts`, usa el Id  -  no importa la clase `Contact` del otro módulo.

---

## DDD  -  Cuando aplicar (y cuando no)

```
Aplica DDD cuando:
  El dominio es COMPLEJO  -  reglas de negocio no triviales
    Ejemplo CRM: asignacion automatica de conversaciones por
    carga del agente + turno + skill + canal
  Tienes acceso a expertos del dominio que saben explicar bien las reglas
  El sistema va a evolucionar durante años
  El equipo supera las 3-4 personas en el mismo modulo

No apliques DDD cuando:
  Es CRUD puro sin reglas de negocio
    Ejemplo: catalogo de etiquetas con nombre y color
  El dominio es trivial (poco codigo de logica real)
  Es un proyecto de 2-4 semanas
  El equipo no tiene experiencia o el tiempo no da para aprender el dominio

El anti-patron mas comun:
  Aplicar Aggregate Roots, Bounded Contexts y Domain Events
  a un simple INSERT de datos de configuracion.
  El back-template tiene DDD donde agrega valor  -  no en todas partes.
```

**Senales de que si necesitas DDD:**
- Un cambio de regla de negocio requiere tocar mas de 5 archivos
- Los developers no pueden explicar una regla sin leer el codigo
- Los bugs aparecen en los bordes entre modulos

---

## Domain Events vs Integration Events

**La distinción más importante de DDD para el sistema de mensajería:**

| | Domain Event | Integration Event |
|--|-------------|-----------------|
| **Scope** | Dentro del mismo bounded context | Cruza módulos o sistemas |
| **Timing** | Síncrono, dentro de la misma transacción | Asíncrono, después de commitar |
| **Transporte** | MediatR `Publish` (en memoria) | Message bus (Redis, RabbitMQ, SQS) |
| **Acoplamiento** | Módulo conoce el evento | Módulo publica contrato versionado |
| **Ejemplo** | `ConversationAssigned` -> audit log interno | `MessageReceived` -> notificación SignalR |

---

## Domain Events  -  ocurren dentro del mismo módulo

```csharp
// Un Domain Event es un hecho que ocurrió en el dominio
// Se publica y se consume en la MISMA transacción de DB

// Domain/Events/ConversationAssignedEvent.cs
public sealed record ConversationAssignedEvent(
    long TenantId,
    long ConvId,
    long AgentId,
    DateTimeOffset AssignedAt) : INotification;

// Application/Handlers/AssignConversationHandler.cs
public async Task<AssignConversationResponse> Handle(
    AssignConversationRequest request, CancellationToken ct)
{
    await _repo.AssignAsync(request.TenantId, request.ConvId, request.AgentId, ct);

    // Publicar Domain Event  -  síncrono, mismo hilo, antes de retornar
    await _mediator.Publish(
        new ConversationAssignedEvent(request.TenantId, request.ConvId,
            request.AgentId, DateTimeOffset.UtcNow), ct);

    return new AssignConversationSuccess(/* dto */);
}

// Application/Handlers/ConversationAssignedAuditHandler.cs
// Otro Handler en el MISMO módulo escucha el Domain Event
public sealed class ConversationAssignedAuditHandler
    : INotificationHandler<ConversationAssignedEvent>
{
    public Task Handle(ConversationAssignedEvent notification, CancellationToken ct)
    {
        _auditLog.Record(notification.TenantId, "ConversationAssigned",
            new { notification.ConvId, notification.AgentId });
        return Task.CompletedTask;
    }
}
```

---

## Integration Events  -  cruzan módulos o sistemas

```csharp
// Un Integration Event cruza el boundary del bounded context
// Se publica DESPUÉS de que la transacción de DB commita (Outbox Pattern)

// Contracts/Events/MessageReceivedIntegrationEvent.cs
// Este contrato vive en la capa Contracts  -  visible para otros módulos
public sealed record MessageReceivedIntegrationEvent(
    long TenantId,
    long ConvId,
    long MessageId,
    string Content,
    string FromChannel,   // "whatsapp" | "instagram" | "email"
    DateTimeOffset ReceivedAt);

// El módulo Messages publica el evento al bus cuando llega un mensaje
// El módulo Notifications lo consume para enviar push notifications
// El módulo Analytics lo consume para métricas de tiempo de respuesta

// Regla: el módulo que publica NO conoce quién consume.
//        El consumidor NO conoce los internos del publicador.
```

---

## Cuándo usar cada uno  -  regla de decisión

```
¿El comportamiento adicional debe ocurrir en la MISMA transacción?
    SÍ -> Domain Event (MediatR.Publish en memoria)
         Ejemplo: audit log que debe ser atómico con la acción

    NO -> Integration Event (Outbox -> message bus)
         Ejemplo: notificación push al agente (puede llegar 1s tarde)

¿El consumidor está en el MISMO módulo?
    SÍ -> Domain Event
    NO -> Integration Event (siempre, aunque el bus sea en la misma app)

¿El consumidor necesita garantía de entrega ("at-least-once")?
    SÍ -> Integration Event + Outbox (el Outbox garantiza que se publica)
    NO -> Domain Event (si falla, falla toda la transacción)
```

> En el sistema de mensajería:
> `ConversationAssigned` -> Domain Event (audit log atómico)
> `MessageReceived` -> Integration Event (SignalR push puede llegar tarde, no crítico que sea atómico)

---

## DDD  -  Aggregate Root en el back-template

```csharp
// Tenancy.Domain/Entities/Tenant.cs
namespace Tenancy.Domain.Entities;

public sealed class Tenant
{
    public long     Id           { get; init; }
    public Guid     PublicId     { get; init; }
    public string   Name         { get; init; } = string.Empty;
    public string   Slug         { get; init; } = string.Empty;
    public bool     IsActive     { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
```

- `Tenant` es un Aggregate Root  -  representa un workspace completo
- Propiedades con `init`  -  inmutabilidad por defecto
- Sin referencias a EF Core, ASP.NET o Dapper  -  Domain puro

---

## DDD  -  Repository en el back-template

```csharp
// Tenancy.Domain/Repositories/ITenantRepository.cs
using Tenancy.Domain.Entities;

namespace Tenancy.Domain.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
```

```csharp
// Users.Domain/Repositories/IUserProfileRepository.cs
using Users.Domain.Entities;

namespace Users.Domain.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByPublicIdAsync(Guid publicId, long tenantId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserProfile>> GetPagedAsync(long tenantId, int page,
        int pageSize, CancellationToken cancellationToken = default);
    Task InsertAsync(Guid publicId, long tenantId, string fullName,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid publicId, long tenantId, string fullName,
        CancellationToken cancellationToken = default);
    Task<bool> DisableAsync(Guid publicId, long tenantId,
        CancellationToken cancellationToken = default);
}
```

La interfaz vive en **Domain**. La implementación concreta vive en **Infrastructure**.

---

<!-- _class: lead -->

## CQRS
### Command Query Responsibility Segregation

---

## CQRS  -  El problema que resuelve

**Sin CQRS:** el mismo repositorio, la misma entidad, el mismo modelo para leer y escribir.

```
Escritura: INSERT + validaciones + reglas de negocio complejas
Lectura:   SELECT con JOINs, proyecciones, paginación, filtros

¿Qué pasa cuando el modelo de escritura es un Aggregate con
reglas estrictas, pero la pantalla de listado necesita datos
de 5 tablas con paginación cursor-based?
```

**Con CQRS:** modelos separados para leer y escribir.

```
Command side -> Request -> Handler -> Repository (escribe, valida invariantes)
Query side   -> Request -> Handler -> DB directa -> DTO (lee, sin Aggregate)
```

**Beneficio real:** los Query Handlers pueden usar `AsNoTracking()` o Dapper
directamente  -  sin cargar el grafo completo del Aggregate.

---

## CQRS  -  Query en el back-template (real)

```csharp
// Users.Application/UseCases/GetUserProfile/GetUserProfileRequest.cs
using Common.Messaging;
using Users.Application.UseCases.GetUserProfile.Responses;

namespace Users.Application.UseCases.GetUserProfile;

public sealed record GetUserProfileRequest(Guid PublicId, long TenantId)
    : IRequest<GetUserProfileResponse>;
```

```csharp
// Users.Application/UseCases/GetUserProfile/GetUserProfileHandler.cs
using Common.Messaging;
using Users.Application.UseCases.GetUserProfile.Responses;
using Users.Contracts.Dtos;
using Users.Domain.Repositories;

namespace Users.Application.UseCases.GetUserProfile;

public sealed class GetUserProfileHandler
    : IRequestHandler<GetUserProfileRequest, GetUserProfileResponse>
{
    private readonly IUserProfileRepository _profiles;

    public GetUserProfileHandler(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task<GetUserProfileResponse> Handle(
        GetUserProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByPublicIdAsync(
            request.PublicId, request.TenantId, cancellationToken);

        if (profile is null)
            return new GetUserProfileNotFoundFailure("Perfil de usuario no encontrado.");

        return new GetUserProfileSuccess(new UserProfileDto(
            profile.PublicId, profile.FullName, profile.IsActive,
            profile.CreatedAtUtc, profile.UpdatedAtUtc));
    }
}
```

---

## CQRS  -  Response pattern en el back-template

```csharp
// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileResponse.cs
using Common.Messaging;

namespace Users.Application.UseCases.GetUserProfile.Responses;

public abstract record GetUserProfileResponse : IResponse;
```

```csharp
// Responses/GetUserProfileSuccess.cs
using Common.Results;
using Users.Contracts.Dtos;

namespace Users.Application.UseCases.GetUserProfile.Responses;

public sealed record GetUserProfileSuccess(UserProfileDto Data)
    : GetUserProfileResponse, ISuccess<UserProfileDto>;
```

```csharp
// Responses/GetUserProfileNotFoundFailure.cs
using Common.Results;

namespace Users.Application.UseCases.GetUserProfile.Responses;

public sealed record GetUserProfileNotFoundFailure(string Message)
    : GetUserProfileResponse, INotFoundFailure;
```

El Handler devuelve siempre `GetUserProfileResponse`  -  nunca lanza excepciones de negocio.
El Controller hace pattern matching sobre el tipo concreto para elegir el HTTP status code.

---

## CQRS  -  DTO en Users.Contracts

```csharp
// Users.Contracts/Dtos/UserProfileDto.cs
namespace Users.Contracts.Dtos;

public sealed record UserProfileDto(
    Guid     PublicId,
    string   FullName,
    bool     IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
```

**Por qué existe `Users.Contracts`?**
- Los DTOs son contratos públicos que otros módulos pueden referenciar
- Separa el "qué se expone" (Contracts) del "cómo se obtiene" (Application)
- `Users.Domain` nunca se expone fuera del módulo  -  solo los DTOs de Contracts

---

## CQRS  -  Command en el back-template (real)

```csharp
// Users.Application/UseCases/UpdateUserProfile/UpdateUserProfileRequest.cs
using Common.Messaging;
using Users.Application.UseCases.UpdateUserProfile.Responses;

namespace Users.Application.UseCases.UpdateUserProfile;

public sealed record UpdateUserProfileRequest(Guid PublicId, long TenantId, string FullName)
    : IRequest<UpdateUserProfileResponse>;
```

```csharp
// Users.Application/UseCases/UpdateUserProfile/UpdateUserProfileHandler.cs
using Common.Messaging;
using Users.Application.UseCases.UpdateUserProfile.Responses;
using Users.Domain.Repositories;

namespace Users.Application.UseCases.UpdateUserProfile;

public sealed class UpdateUserProfileHandler
    : IRequestHandler<UpdateUserProfileRequest, UpdateUserProfileResponse>
{
    private readonly IUserProfileRepository _profiles;

    public UpdateUserProfileHandler(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task<UpdateUserProfileResponse> Handle(
        UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var updated = await _profiles.UpdateAsync(
            request.PublicId, request.TenantId, request.FullName, cancellationToken);

        return updated
            ? new UpdateUserProfileSuccess()
            : new UpdateUserProfileNotFoundFailure("Perfil de usuario no encontrado.");
    }
}
```

---

## CQRS  -  Query vs Command  -  diferencias clave

| Aspecto | Query (GetUserProfile) | Command (UpdateUserProfile) |
|---------|------------------------|----------------------------|
| **Efecto** | Solo lee  -  no modifica estado | Modifica estado |
| **Repositorio** | `GetByPublicIdAsync`  -  puede ser `AsNoTracking` | `UpdateAsync`  -  escribe en DB |
| **Retorno** | DTO con datos | Éxito/Fallo sin datos |
| **Idempotencia** | Siempre idempotente | Depende de la regla de negocio |
| **Request name** | `Get...Request`, `List...Request` | `Create...`, `Update...`, `Disable...` |
| **Response** | `...Success(dto)` con datos | `...Success()` sin datos |

**El mismo `IRequestHandler` sirve para ambos**  -  la diferencia es semántica y de convención,
no de tipo base distinto.

---

## CQRS  -  Cuándo separar repositorios

| Escenario | Recomendación |
|-----------|--------------|
| CRUD simple, pocas reglas | Repositorio unificado es suficiente (YAGNI) |
| Queries con muchos JOINs + commands con reglas complejas | `IContactWriteRepository` + `IContactReadRepository` |
| Alta escala de lectura (10× más reads que writes) | Read replica + CQRS puro |
| MVP / inicio del proyecto | Unificado  -  separar cuando el dolor sea real |

**Dapper en Query Handlers**  -  alternativa válida para queries de lectura complejas:

```csharp
// Handler con Dapper  -  SQL directo, sin change tracking, sin Aggregate
var dto = await _db.QueryFirstOrDefaultAsync<ConversationDto>(
    "SELECT c.id, c.status, a.full_name AS agent_name " +
    "FROM conversations c LEFT JOIN agents a ON a.id = c.agent_id " +
    "WHERE c.public_id = @PublicId AND c.tenant_id = @TenantId",
    new { request.PublicId, request.TenantId });
```

---

## CQRS  -  Por que divergen los modelos con el tiempo

```
Mes 1  -  El modelo de escritura y de lectura son identicos:
  Contact { Id, Name, Email, CreatedAt }
  POST /contacts recibe lo mismo que GET /contacts devuelve.
  Un solo repositorio sirve para todo.

Mes 4  -  La pantalla de listado necesita:
  Contact {
    Id, Name, Email,
    LastConversationDate,    <- JOIN a conversations
    TotalConversations,      <- COUNT en conversations
    AssignedAgentName,       <- JOIN a agents
    TagNames[],              <- JOIN a contact_tags + tags
    UnreadMessages           <- subquery en messages
  }
  El modelo de escritura NO tiene esos campos.
  Arrastrar el Aggregate completo solo para proyectar un listado es ineficiente.

Mes 8  -  Las reglas de escritura se complican:
  Crear un contacto requiere:
    - Email unico en el tenant
    - Telefono en formato E.164
    - No duplicado por fuzzy match de nombre
    - Al menos un canal de contacto definido
  El modelo de lectura no necesita ninguna de esas validaciones.
```

CQRS emerge naturalmente: las necesidades de lectura y escritura
divergen tan rapido que mantenerlas en el mismo repositorio crea friccion constante.

---

## CQRS  -  Cuando NO usarlo

| Escenario | Por que no aplicar CQRS |
|-----------|------------------------|
| CRUD sin reglas de negocio | El mismo modelo sirve para leer y escribir (YAGNI) |
| MVP primeras 4 semanas | La complejidad no se amortiza todavia |
| Equipo sin experiencia previa | La curva ralentiza el delivery en momento critico |
| Dominio estable que no cambia | Sin divergencia de modelos, no hay problema que resolver |

**El patron de adopcion correcto en el CRM:**

```
Sprint 1-2:  Un repositorio, un modelo por entidad
             Aprende el dominio real antes de abstraer

Sprint 3-5:  Los queries de listado se complican con JOINs
             Extraes esos Query Handlers con Dapper  -  solo donde duele

Sprint 6+:   Separas Write Repositories de Read Repositories
             Solo donde el dolor es real y medible, no por default
```

CQRS es la solucion a un problema que aparece con el tiempo.
Aplicarlo desde el dia 1 en todo es over-engineering.

---

<!-- _class: lead -->

## Specification Pattern

### Consultas componibles sin contaminar el Repositorio

---

## El problema: repositorios que crecen sin control

```csharp
//  Sin Specification  -  el repositorio acumula métodos infinitos
public interface IConversationRepository
{
    Task<IReadOnlyList<Conversation>> GetUnassignedAsync(long tenantId, CancellationToken ct);
    Task<IReadOnlyList<Conversation>> GetByAgentAsync(long tenantId, long agentId, CancellationToken ct);
    Task<IReadOnlyList<Conversation>> GetOpenByChannelAsync(long tenantId, long channelId, CancellationToken ct);
    Task<IReadOnlyList<Conversation>> GetUnassignedOpenByChannelAsync(long tenantId, long channelId, CancellationToken ct);
    Task<IReadOnlyList<Conversation>> GetByAgentAndStatusAsync(long tenantId, long agentId, ConversationStatus status, CancellationToken ct);
    // ... 20 métodos más, uno por combinación de filtros
}
```

**El Specification Pattern soluciona esto:**
- Una sola firma `GetAsync(ISpecification<Conversation>)`
- Las reglas de negocio viven en objetos, no en el nombre del método
- Las especificaciones se componen: `IsUnassigned AND IsOpen AND InChannel(3)`

---

## ISpecification\<T\>  -  la interfaz base

```csharp
// Domain/Specifications/ISpecification.cs
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }

    // Opcionales  -  para eager loading y ordenamiento
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
}
```

```csharp
// Domain/Specifications/BaseSpecification.cs
public abstract class BaseSpecification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; private set; } = _ => true;
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Take { get; private set; }
    public int? Skip { get; private set; }

    protected void AddCriteria(Expression<Func<T, bool>> criteria) => Criteria = criteria;
    protected void AddInclude(Expression<Func<T, object>> include) => Includes.Add(include);
    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;
    protected void ApplyPaging(int skip, int take) { Skip = skip; Take = take; }
}
```

---

## Especificaciones concretas  -  reglas de negocio como objetos

```csharp
// Domain/Specifications/Conversations/UnassignedOpenConversationsSpec.cs
public class UnassignedOpenConversationsSpec : BaseSpecification<Conversation>
{
    public UnassignedOpenConversationsSpec(long tenantId, long? channelId = null)
    {
        if (channelId.HasValue)
            AddCriteria(c => c.TenantId == tenantId
                          && c.AssignedAgentId == null
                          && c.Status == ConversationStatus.Open
                          && c.ChannelId == channelId.Value);
        else
            AddCriteria(c => c.TenantId == tenantId
                          && c.AssignedAgentId == null
                          && c.Status == ConversationStatus.Open);

        AddInclude(c => c.Channel);
        ApplyOrderBy(c => c.LastMessageAt);
    }
}
```

```csharp
// Uso en el Handler
var spec = new UnassignedOpenConversationsSpec(request.TenantId, request.ChannelId);
var conversations = await _repo.GetAsync(spec, ct);
```

---

## Repositorio con Specification  -  una sola firma

```csharp
// Domain/Repositories/IConversationRepository.cs
public interface IConversationRepository
{
    Task<IReadOnlyList<Conversation>> GetAsync(
        ISpecification<Conversation> spec, CancellationToken ct = default);
    Task<Conversation?> GetOneAsync(
        ISpecification<Conversation> spec, CancellationToken ct = default);
    Task<int> CountAsync(
        ISpecification<Conversation> spec, CancellationToken ct = default);
    Task InsertAsync(Conversation conversation, CancellationToken ct = default);
    Task<bool> UpdateAsync(Conversation conversation, CancellationToken ct = default);
}
```

```csharp
// Infrastructure/Repositories/ConversationRepository.cs
public async Task<IReadOnlyList<Conversation>> GetAsync(
    ISpecification<Conversation> spec, CancellationToken ct)
{
    var query = _db.Conversations.AsQueryable();

    if (spec.Criteria is not null)
        query = query.Where(spec.Criteria);

    foreach (var include in spec.Includes)
        query = query.Include(include);

    if (spec.OrderBy is not null)
        query = query.OrderBy(spec.OrderBy);

    if (spec.Skip.HasValue)
        query = query.Skip(spec.Skip.Value);

    if (spec.Take.HasValue)
        query = query.Take(spec.Take.Value);

    return await query.AsNoTracking().ToListAsync(ct);
}
```

---

## Composición de especificaciones

```csharp
// Clase utilitaria para AND / OR / NOT
public class AndSpecification<T> : BaseSpecification<T>
{
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        var leftExpr  = left.Criteria;
        var rightExpr = right.Criteria;
        var param     = Expression.Parameter(typeof(T));
        var body      = Expression.AndAlso(
            Expression.Invoke(leftExpr, param),
            Expression.Invoke(rightExpr, param));
        AddCriteria(Expression.Lambda<Func<T, bool>>(body, param));
    }
}

// Uso  -  composición en el Handler sin tocar el repositorio
var unassigned = new UnassignedConversationsSpec(tenantId);
var highPriority = new HighPriorityConversationsSpec(tenantId);
var combined = new AndSpecification<Conversation>(unassigned, highPriority);

var conversations = await _repo.GetAsync(combined, ct);
```

---

## Specification en el sistema de mensajería  -  casos reales

| Specification | Qué filtra |
|--------------|-----------|
| `UnassignedOpenSpec(tenantId)` | Bandeja de entrada principal |
| `AssignedToAgentSpec(tenantId, agentId)` | Mis conversaciones |
| `ByChannelSpec(tenantId, channelId)` | Conversaciones por canal |
| `SearchByContactSpec(tenantId, query)` | Búsqueda FTS de contactos |
| `StaleConversationsSpec(tenantId, hours)` | Sin respuesta hace X horas (SLA) |
| `PendingArchivingSpec(tenantId)` | Con >100 mensajes listos para archivar |

> Cada especificación es testeable independientemente:
> `new UnassignedOpenSpec(1).Criteria` devuelve una expresión verificable con LINQ to Objects.

---

## Specification  -  Composicion visual de filtros

```
Caso real: bandeja del agente Maria  -  WhatsApp  -  urgentes primero

Sin Specification (el repositorio acumula metodos por combinacion):
  GetByAgentAsync(agentId)
  GetByAgentAndChannelAsync(agentId, channelId)
  GetByAgentAndChannelAndStatusAsync(agentId, channelId, status)
  GetByAgentAndChannelAndStatusOrderedByPriorityAsync(...)   <- 10 parametros

Con Specification (el Handler compone los bloques):

  IsAssigned(agentId: Maria.Id)
      AND
  InChannel(channelId: WhatsApp.Id)
      AND
  HasStatus("open")
      AND
  OrderByDescending(LastMessageAt)

  var spec = new AndSpecification<Conversation>(
      new IsAssignedSpec(agentId),
      new AndSpecification<Conversation>(
          new InChannelSpec(channelId),
          new HasStatusSpec("open")));
  spec.ApplyOrderByDescending(c => c.LastMessageAt);

  var results = await _repo.GetAsync(spec, ct);
```

El Handler construye el filtro como piezas LEGO.
El Repository no sabe que combinacion se uso  -  solo aplica la expresion.
Añadir un nuevo filtro no requiere tocar el Repository.

---

<!-- _class: lead -->

## Clean Architecture
### Las reglas de dependencia

---

## Clean Architecture  -  Las capas

```
+-------------------------------------------------+
|  Presentation / Host                             |  Controllers, Program.cs
|  +-------------------------------------------+  |
|  |  Infrastructure                            |  |  EF Core, Dapper, Email, S3
|  |  +-------------------------------------+  |  |
|  |  |  Application                         |  |  |  Handlers, Pipeline, Responses
|  |  |  +-------------------------------+  |  |  |
|  |  |  |  Domain                        |  |  |  |  Entities, Value Objects, Interfaces
|  |  |  `-------------------------------+  |  |  |
|  |  `-------------------------------------+  |  |
|  `-------------------------------------------+  |
`-------------------------------------------------+
```

**Regla fundamental:** las flechas de dependencia apuntan hacia adentro.
`Domain` no depende de nadie. `Infrastructure` depende de `Domain`.

---

## Clean Architecture  -  Reglas de dependencia

| Capa | Puede importar | No puede importar |
|------|---------------|-------------------|
| `Domain` | Solo BCL (.NET base class library) | Application, Infrastructure, EF Core, Dapper |
| `Application` | Domain | Infrastructure, EF Core, Dapper, ASP.NET |
| `Infrastructure` | Domain, Application | Host |
| `Presentation` | Application, Domain (Contracts) | Infrastructure directamente |
| `Host.Api` | Todo (composition root) |  -  |

**Herramienta:** architecture tests (NetArchTest)  -  verifican en CI que Domain no tiene
referencia a Infrastructure.

```csharp
[Fact]
public void Domain_ShouldNotDependOn_Infrastructure()
{
    var result = Types.InAssembly(DomainAssembly)
        .ShouldNot().HaveDependencyOn("Infrastructure")
        .GetResult();
    Assert.True(result.IsSuccessful);
}
```

---

## Clean Architecture  -  Por qué Domain no debe importar EF Core

```csharp
//  Entity con EF Core en Domain  -  viola Clean Architecture
using Microsoft.EntityFrameworkCore;  // importa EF Core en Domain

[Index(nameof(Email), IsUnique = true)]
public sealed class Contact
{
    [Key]
    public long Id { get; set; }
    // ...
}
```

```csharp
//  Entity pura en Domain  -  sin atributos de EF Core
public sealed class Contact
{
    public long   Id       { get; init; }
    public Guid   PublicId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public bool   IsActive { get; init; }
}
```

```csharp
//  Configuración en Infrastructure  -  EntityTypeConfiguration separada
public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.PublicId).IsUnique();
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
    }
}
```

---

## Clean Architecture  -  La historia de la idea

```
El problema historico: el codigo de negocio dependia del framework.
Si cambias de ASP.NET a gRPC, o de EF Core a Dapper,
el "nucleo" de tu sistema tiene que reescribirse.

Alistair Cockburn, 2005  -  Hexagonal Architecture (Ports & Adapters):
  El nucleo del negocio no sabe si lo llama HTTP, CLI o un test.
  Puerto (Port): la interfaz que el nucleo expone o necesita.
  Adaptador (Adapter): la implementacion concreta del puerto.
  Ejemplo: IConversationRepository es el Puerto.
           ConversationRepository (EF Core) es el Adaptador.

Jeffrey Palermo, 2008  -  Onion Architecture:
  Igual que Hexagonal con mas capas explicitas.
  Domain en el centro, rodeado de Application, luego Infrastructure.

Robert C. Martin, 2012  -  Clean Architecture:
  Sintetiza Hexagonal y Onion en una sola regla:
  "Las dependencias apuntan hacia adentro."
  Domain en el centro. Infrastructure en el borde.
  Nunca al reves  -  nunca.
```

El back-template implementa Clean Architecture.
`Domain` no importa nada de `Infrastructure`.
`Infrastructure` implementa las interfaces del `Domain`.

---

## Clean Architecture  -  El compilador como guardian

```
El problema de las convenciones verbales:
  "Acordamos que Domain no debe importar EF Core."
  6 meses despues: alguien bajo presion de un deadline agrega
  [using Microsoft.EntityFrameworkCore] en una entidad de Domain.
  Pasa el code review. El CI no lo detecta. Ya contamino la capa.
  6 meses mas: nadie puede hacer unit tests de Domain sin levantar una DB.

La solucion: hacer que el compilador rechace la violacion.
```

```csharp
// Architecture tests con NetArchTest  -  corren en CI, no dependen de la disciplina
[Fact]
public void Domain_ShouldNotDependOn_Infrastructure()
{
    var result = Types.InAssembly(DomainAssembly)
        .ShouldNot().HaveDependencyOn("Infrastructure")
        .GetResult();
    Assert.True(result.IsSuccessful);
}

[Fact]
public void Application_ShouldNotDependOn_EFCore()
{
    var result = Types.InAssembly(ApplicationAssembly)
        .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
        .GetResult();
    Assert.True(result.IsSuccessful);
}
```

Si alguien viola la regla, el pipeline falla y el PR no puede mergearse.
No dependes de que el reviewer lo detecte  -  la maquina lo detecta siempre.

---

<!-- _class: lead -->

## Vertical Slice Architecture
### Features como unidades autónomas

---

## Layered vs Vertical Slice

**Layered Architecture**  -  organizas por tecnología:

```
Controllers/    -  todos los controllers juntos
Application/    -  todos los handlers juntos
Domain/         -  todas las entidades juntas
Infrastructure/  -  todos los repos juntos
```

Cambiar un feature toca N carpetas distribuidas por todo el proyecto.

**Vertical Slice Architecture**  -  organizas por feature:

```
UseCases/
|-- GetUserProfile/     -  todo lo de "obtener perfil" en una carpeta
|   |-- GetUserProfileRequest.cs
|   |-- GetUserProfileHandler.cs
|   `-- Responses/
|-- UpdateUserProfile/  -  slice independiente, no comparte nada
`-- DisableUserProfile/
```

Un feature = una carpeta = un "slice" vertical que atraviesa las capas.

---

## Vertical Slice en el back-template (real)

```
Users.Application/UseCases/
|-- GetUserProfile/
|   |-- GetUserProfileRequest.cs
|   |-- GetUserProfileHandler.cs
|   `-- Responses/
|       |-- GetUserProfileResponse.cs   <- abstract record base
|       |-- GetUserProfileSuccess.cs
|       `-- GetUserProfileNotFoundFailure.cs
|-- GetUserProfiles/
|   |-- GetUserProfilesRequest.cs
|   |-- GetUserProfilesHandler.cs
|   `-- Responses/
|       |-- GetUserProfilesResponse.cs
|       `-- GetUserProfilesSuccess.cs
|-- UpdateUserProfile/
|   |-- UpdateUserProfileRequest.cs
|   |-- UpdateUserProfileHandler.cs
|   `-- Responses/
`-- DisableUserProfile/
    |-- DisableUserProfileRequest.cs
    |-- DisableUserProfileHandler.cs
    `-- Responses/
```

Cada slice tiene su propio Response hierarchy  -  no comparten tipos con otros slices.

---

## Vertical Slice  -  Trade-offs

| Aspecto | Layered | Vertical Slice |
|---------|---------|----------------|
| Localización de código | Por tecnología | Por feature |
| Cambiar un feature | Tocar N carpetas | Tocar 1 carpeta |
| Reutilización de código | Alta (capas compartidas) | Baja por diseño (cada slice es autónomo) |
| Fricción al onboarding | Navegar muchas carpetas | Un slice es legible solo |
| Duplicación permitida | No  -  extraes a capas | Sí  -  "share nothing" es una virtud |
| Escala de equipo | Equipos con convenciones estrictas | Equipos pequeños / features independientes |

**En el back-template:** Layered en la estructura de proyectos
(`Users.Domain`, `Users.Application`, `Users.Infrastructure`) con
Vertical Slices internos en `Application/UseCases/{Feature}/`.

No son excluyentes  -  se combinan.

---

## Vertical Slice  -  La Ley de Conway

```
Melvin Conway, 1967:
  "Las organizaciones que disenan sistemas estan limitadas a producir
   disenos que son copias de las estructuras de comunicacion de esas
   organizaciones."

Equipo organizado por capas -> arquitectura por capas:
  Equipo DB       -> controla la capa de datos para todos los features
  Equipo Backend  -> controla la capa de negocio para todos los features
  Equipo Frontend -> controla la capa de presentacion
  Resultado: cada feature nuevo requiere coordinar con los 3 equipos
             tickets de dependencia, esperas, handoffs

Equipo organizado por features -> arquitectura de features:
  Equipo Conversaciones -> controla todo lo de conversaciones (DB + logica + API)
  Equipo Contactos      -> controla todo lo de contactos
  Resultado: cada equipo entrega features completos sin depender de los demas
```

El equipo tiene 4 personas.
Cada quien puede ser dueno de un feature completo.
Vertical Slice los hace autonomos  -  no tienen que esperarse entre si.

---

## Vertical Slice  -  La arquitectura que grita

Robert C. Martin: "La arquitectura debe gritar el proposito del sistema."
Un arquitecto de casas no te entrega planos que digan "madera", "concreto", "vidrio".
Te entrega planos que dicen "cocina", "sala", "dormitorio".

```
Arquitectura por capas  -  grita la tecnologia, no el negocio:
  Controllers/     todos los controllers juntos
  Models/          todos los modelos juntos
  Repositories/    todos los repos juntos
  Services/        todos los servicios juntos
  -> Un desarrollador nuevo no sabe de que trata el sistema.

Vertical Slice  -  grita el proposito:
  UseCases/
  |-- GetConversations/    grita "gestiono bandeja de conversaciones"
  |-- AssignConversation/  grita "asigno conversaciones a agentes"
  |-- GetContacts/         grita "gestiono contactos del workspace"
  |-- ImportContacts/      grita "importo contactos desde CSV"
  |-- SendReply/           grita "envio respuestas en tiempo real"
  `-- CreateBotFlow/       grita "gestiono flujos de bot automatizado"
```

Un desarrollador nuevo puede entender que hace el sistema
mirando solo los nombres de las carpetas  -  sin leer una sola linea de codigo.

---

<!-- _class: lead -->

## Monolito Modular
### El punto de partida correcto para el CRM

---

## ¿Por qué no microservicios desde el día 1?

```
Microservicios requieren:
  [Err] Service mesh (Istio / Linkerd)
  [Err] Distributed tracing (Jaeger / Zipkin)
  [Err] Event bus (RabbitMQ / Kafka)
  [Err] API Gateway con routing por servicio
  [Err] Manejo de eventual consistency
  [Err] Distributed transactions (Saga pattern)
  [Err] Infraestructura multi-deployment
  [Err] Equipo de DevOps dedicado

Monolito Modular requiere:
   Un repositorio, un deployment
   Transacciones ACID locales
   El mismo nivel de separación de módulos
   Debug con un solo proceso
   Migratable a microservicios cuando el dominio esté probado
```

Eso generaría semanas de infraestructura antes de escribir una sola línea de negocio.

---

## Monolito Modular  -  Estructura real del back-template

```
back-template/
|-- Modules/
|   |-- Users/
|   |   |-- Users.Domain          <- Entities, Repositories (interfaces)
|   |   |-- Users.Application     <- Handlers, Responses, UseCases/
|   |   |-- Users.Contracts       <- DTOs públicos del módulo
|   |   |-- Users.Infrastructure  <- Repos (EF Core), EntityTypeConfigurations
|   |   |-- Users.Presentation    <- UsersController
|   |   `-- Users.Tests
|   `-- Tenancy/
|       |-- Tenancy.Domain        <- Tenant (Aggregate Root), ITenantRepository
|       |-- Tenancy.Application   <- Use cases de tenancy
|       |-- Tenancy.Contracts
|       |-- Tenancy.Infrastructure
|       |-- Tenancy.Presentation
|       `-- Tenancy.Tests
|-- Shared/
|   `-- Authentication/
|       |-- Authentication.Domain
|       |-- Authentication.Application
|       |-- Authentication.Infrastructure
|       `-- Authentication.Presentation
|-- Common/Common/
|   |-- Messaging/        <- IMediator, IRequest, IRequestHandler, IResponse
|   |-- Results/          <- IFailure, ISuccess, INotFoundFailure, IConflictFailure
|   |-- ViewModels/       <- ResultViewModel<T>
|   |-- MultiTenancy/     <- ITenantContextAccessor, TenantContextAccessor
|   `-- Abstractions/     <- IInteractor<TRequest, TResponse>
`-- Host.Api/
    |-- Program.cs
    |-- Extensions/       <- AddSwaggerWithJwt, AddJwtAuthentication, etc.
    `-- Middleware/       <- TenantClaimsMiddleware
```

---

## Monolito Modular  -  Reglas de comunicación entre módulos

**Regla:** los módulos no se llaman directamente entre sí.
Si `Conversations` necesita datos de `Contacts`, hay dos opciones:

**Opción A  -  Via interfaz de repositorio** (acoplamiento mínimo, transaccional):

```csharp
// En AssignConversationHandler (módulo Conversations):
// Necesita verificar que el contacto existe antes de asignar
var exists = await _contactRepository.ExistsAsync(request.ContactId, ct);
if (!exists)
    return new AssignConversationContactNotFoundFailure("Contacto no encontrado.");
```

**Opción B  -  Via Domain Events** (desacoplamiento total, asíncrono):

```csharp
// Conversation publica un Domain Event
// Un handler en Contacts se suscribe y actualiza su estado
public sealed record ConversationAssigned(
    Guid ConversationId, Guid AgentId, Guid ContactId) : INotification;
```

**Regla práctica:** para operaciones transaccionales síncronas, usa interfaz.
Para side effects asíncronos (notificaciones, auditoría, estadísticas), usa Domain Events.

---

## Monolito Modular  -  Cuando extraer un microservicio

```
El patron de extraccion (no el dia 1):

Fase 1  -  MVP (mes 1-12):
  Monolito Modular con Modules/Conversations, Modules/Contacts, etc.
  Un deployment. Una DB. Un equipo.

Fase 2  -  Senales de que un modulo necesita escalar diferente:
  - Supera 500 req/seg sostenidos mientras el resto esta en 50 req/seg
  - Tiene un SLA diferente al resto del sistema (99.99% vs 99.9%)
  - El equipo del modulo crecio y tiene ownership separado del resto
  - Necesita tecnologia diferente (Redis, Cassandra) que no comparte con otros

Fase 3  -  Extraccion (porque el modulo ya esta bien separado):
  1. El modulo ya tiene interfaces bien definidas en su capa Application
  2. Cambias la implementacion: en vez de llamada en memoria,
     llamas via HTTP o gRPC
  3. El modulo se convierte en su propio deployment con su propia DB
  4. El resto del monolito no cambia  -  solo cambia el Adaptador
```

Sin modulos bien separados desde el inicio,
la extraccion a microservicio es una reescritura total.
El Monolito Modular no es el problema  -  es la base que hace posible la extraccion.

---

<!-- _class: lead -->

## El back-template diseccionado
### De Program.cs al Handler

---

## Program.cs  -  La raíz de composición (real)

```csharp
// Host.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoggingServices(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);

// Multi-tenancy
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();

// Database  -  EF Core + schema initialization
builder.Services.AddMainDatabase(builder.Configuration);

// Mediator  -  un solo AddMediator con todos los assemblies de Application
builder.Services.AddMediator(
    typeof(Tenancy.Application.ServiceCollectionEx).Assembly,
    typeof(Users.Application.ServiceCollectionEx).Assembly,
    typeof(Authentication.Application.ServiceCollectionEx).Assembly
);
```

`AddMediator` escanea los tres assemblies de Application para registrar
todos los `IRequestHandler` automáticamente por convención.

---

## Program.cs  -  Registro por módulo (real)

```csharp
// Tenancy module
builder.Services.AddTenancyApplicationServices();
builder.Services.AddTenancyInfrastructureServices();
builder.Services.AddTenancyWebApiServices();

// Users module
builder.Services.AddUsersApplicationServices();
builder.Services.AddUsersInfrastructureServices();
builder.Services.AddUsersWebApiServices();

// Authentication shared
builder.Services.AddAuthenticationApplicationServices();
builder.Services.AddAuthenticationInfrastructureServices(builder.Configuration);
builder.Services.AddAuthenticationWebApiServices();
```

Cada módulo expone sus propios métodos de extensión `ServiceCollectionEx`.
`Host.Api` solo orquesta  -  no contiene lógica de ningún módulo.

---

## Program.cs  -  Pipeline HTTP (real)

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment() ||
    app.Environment.IsEnvironment("Local") ||
    app.Environment.IsEnvironment("Staging"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCoreProblemDetails();
app.UseCorrelationId();
app.UseCors(CorsExtensions.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantClaimsMiddleware>();  // extrae TenantId del JWT

app.MapControllers();
app.MapHealth();
app.MapPrometheusScrapingEndpoint();

app.Run();
```

`TenantClaimsMiddleware` extrae el `tenant_id` del JWT y lo pone en `ITenantContextAccessor`.
Todo handler downstream puede leer el tenant sin depender de `HttpContext`.

---

## El flujo completo  -  Request a Response

```
HTTP GET /api/users/{id}
        |

UsersController.GetById(id)
        |  new GetUserProfileRequest(id, CurrentTenantId)

IMediator.Send(request)
        |

InteractorPipeline (logging, error handling)
        |

GetUserProfileHandler.Handle(request, ct)
        |  _profiles.GetByPublicIdAsync(id, tenantId, ct)

IUserProfileRepository  <---- implementado en Users.Infrastructure
        |

GetUserProfileHandler devuelve:
  GetUserProfileSuccess(dto)    -  si encontró
  GetUserProfileNotFoundFailure("...")  -  si no encontró
        |

Controller: pattern matching sobre el tipo concreto
  -> 200 OK   si IsSuccess
  -> 404 Not Found si NotFoundFailure
```

---

## El Controller  -  Pattern matching sobre Response (real)

```csharp
// Users.Presentation/Controllers/UsersController.cs
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
{
    try
    {
        var result = await Mediator.Send(new GetUserProfileRequest(id, CurrentTenantId), ct);

        if (_viewModel.IsSuccess) return Ok(_viewModel);

        return result is GetUserProfileNotFoundFailure
            ? NotFound(_viewModel)
            : StatusCode(500, _viewModel);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en GetById User");
        var inner = ex;
        while (inner.InnerException != null) inner = inner.InnerException!;
        return StatusCode(500, _viewModel.Fail(inner.Message));
    }
}
```

El Controller comprueba `IFailure` primero (via `_viewModel.IsSuccess`).
Luego distingue el tipo concreto de fallo para elegir el status code correcto.

---

## ResultViewModel  -  El objeto de respuesta HTTP

```csharp
// Common/Common/ViewModels/ResultViewModel.cs
public class ResultViewModel<T>
{
    public object?   Data         { get; private set; }
    public bool      IsSuccess    { get; private set; }
    public string?   Message      { get; private set; }
    public DateTime  UtcTimeStamp { get; private set; }

    public void Set(IFailure failure)   // llamado por el pipeline cuando hay fallo
    {
        Data      = null;
        Message   = failure.Message;
        IsSuccess = false;
    }

    public void Set<TData>(ISuccess<TData> success, Func<TData, object>? callback = null)
    {
        Data      = callback?.Invoke(success.Data) ?? success.Data;
        IsSuccess = true;
        Message   = null;
    }
}
```

`ResultViewModel<T>` es el sobre JSON uniforme que todos los endpoints devuelven.
El pipeline lo rellena automáticamente  -  el Controller solo pregunta `IsSuccess`.

---

## Common.Messaging  -  Las abstracciones base

```csharp
// Common/Common/Messaging/MediatorAbstractions.cs
public interface IResponse : INotification { }

public interface IRequest<out TResponse> { }

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    Task Publish<TNotification>(TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

El back-template tiene su propio mini-mediator  -  no depende de MediatR.
`IInteractor<TRequest, TResponse>` en `Common.Abstractions` extiende `IRequestHandler`
y agrega la constraint `where TResponse : IResponse`.

---

## Common.Results  -  La jerarquía de resultados

```csharp
// Common/Common/Results/IFailure.cs
public interface IFailure
{
    string Message { get; }
}

public interface IValidationFailure : IFailure { }
public interface INotFoundFailure   : IFailure { }
public interface IConflictFailure   : IFailure { }
```

```csharp
// Common/Common/Results/Result.cs
public abstract class Result : IResponse
{
    public static Result    OK()               => new SuccessResult();
    public static Result    Fail(string msg)   => new FailureResult(msg);
    public static Result    Fail(Exception ex) => new FailureResult(ex);
    public static Result<T> OK<T>(T data)      => new SuccessResult<T>(data);
    public static Result<T> Fail<T>(string m)  => new FailureResult<T>(m);
}
```

`Result` es el tipo base para respuestas simples (sin DTO).
Los Response records de cada slice heredan de `GetUserProfileResponse : IResponse`
y marcan `ISuccess<T>` o `INotFoundFailure`  -  no de `Result` directamente.

---

## Resumen  -  Arquitectura de un feature completo

```
Módulo Users  -  feature GetUserProfile

Domain          IUserProfileRepository  (interfaz  -  no sabe de EF Core)
                UserProfile             (entity  -  sin atributos de EF Core)

Contracts       UserProfileDto          (record  -  contrato público)

Application     GetUserProfileRequest   (IRequest<GetUserProfileResponse>)
                GetUserProfileHandler   (IRequestHandler  -  usa IUserProfileRepository)
                Responses/
                  GetUserProfileResponse         (abstract record base)
                  GetUserProfileSuccess          (ISuccess<UserProfileDto>)
                  GetUserProfileNotFoundFailure  (INotFoundFailure)

Infrastructure  UserProfileRepository   (implementa IUserProfileRepository con EF Core)

Presentation    UsersController         (HttpGet -> IMediator.Send -> pattern match)

Host.Api        Program.cs              (registra todo  -  composition root)
```

Esta es la anatomía que repetirás para cada feature del sistema de mensajería.

---

## Los 5 conceptos  -  Cómo se relacionan entre sí

| Concepto | Rol | En el back-template |
|----------|-----|---------------------|
| **DDD** | Modelar el negocio correctamente | `Tenant`, `Contact` en `*.Domain`  -  sin EF Core |
| **CQRS** | Separar lectura de escritura | `Get...Request` (query) vs `Update...Request` (command) |
| **Clean Architecture** | Dependencias apuntan al Domain | `Domain` no importa `Infrastructure`  -  verificable en CI |
| **Vertical Slice** | Features autónomos | `UseCases/GetUserProfile/` completo en una carpeta |
| **Monolito Modular** | Un deployment, módulos bien aislados | `Modules/Users/`, `Modules/Tenancy/`  -  un solo `Host.Api` |

No son alternativas entre sí  -  son capas complementarias del mismo diseño.
El back-template las aplica todas simultáneamente.

---

## Errores comunes  -  Qué no hacer

| Error | Consecuencia | Corrección |
|-------|-------------|-----------|
| Entidad de Domain importa EF Core | Domain acoplado a Infrastructure | Mover configuración a `EntityTypeConfiguration` en Infrastructure |
| Handler llama a otro Handler directamente | Acoplamiento entre features | Usar `IMediator.Send()` o Domain Event |
| Repository devuelve `IQueryable<T>` | La query se arma fuera del repo  -  filtros pueden escapar | Retornar `IReadOnlyList<T>` o DTOs |
| Mismo modelo para reads y writes | El modelo de escritura arrastra complejidad a las queries | Query Handler directo a DB con DTO plano |
| Módulo A importa namespace de Módulo B | Bounded Contexts mezclados | Solo compartir por Id o via Domain Event |
| `GetUserProfileNotFoundFailure` usado fuera de su slice | Acoplamiento entre slices | Cada slice define sus propios Response types |

---

<!-- _class: lead -->

## Outbox Pattern

### Garantizar que los eventos se publiquen aunque falle la red

---

## El problema del dual-write

```
// Flujo ROTO  -  sin Outbox
1. Llega webhook de WhatsApp: nuevo mensaje de Ana García
2. INSERT en tabla messages (PostgreSQL)
3. PUBLISH "MessageReceived" al bus de mensajes <- puede fallar
4. Si el PUBLISH falla:
   - El mensaje está en DB (visible en la bandeja)
   - Pero el agente nunca recibe la notificación push (SignalR no se disparó)
   - El SLA timer nunca empieza
   - La campaña de respuesta automática nunca se activa
```

**La atomicidad del problema:**
- No puedes poner el `PUBLISH` dentro de la transacción de DB (el bus es externo)
- No puedes confirmar el `INSERT` después del `PUBLISH` (el bus puede confirmar antes de que el DB commite)
- Las dos operaciones son en sistemas distintos -> no hay transacción distribuida

---

## La solución: Outbox Table

```
// Flujo CORRECTO  -  con Outbox
1. Llega webhook de WhatsApp
2. BEGIN TRANSACTION
3.   INSERT en tabla messages
4.   INSERT en tabla outbox_events (el evento serializado como JSON)
5. COMMIT TRANSACTION <- atómico  -  ambos o ninguno

// Worker separado (BackgroundService):
6. SELECT * FROM outbox_events WHERE processed_at IS NULL ORDER BY created_at LIMIT 10
7. PUBLISH cada evento al bus
8. UPDATE outbox_events SET processed_at = NOW() WHERE id = @id
```

**Garantía:** si la app cae entre el paso 5 y el 7, al reiniciar el worker reintenta los eventos no procesados.

---

## Outbox Table en PostgreSQL

```sql
-- Migration: CreateOutboxEventsTable
CREATE TABLE outbox_events (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       BIGINT      NOT NULL,
    event_type      VARCHAR(200) NOT NULL,   -- nombre del tipo de evento
    payload         JSONB       NOT NULL,    -- evento serializado
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at    TIMESTAMPTZ NULL,        -- NULL = pendiente
    retry_count     INT         NOT NULL DEFAULT 0,
    last_error      TEXT        NULL
);

CREATE INDEX idx_outbox_events_unprocessed
    ON outbox_events (created_at)
    WHERE processed_at IS NULL;
```

---

## Outbox en EF Core  -  guardar evento en la misma transacción

```csharp
// Application/Handlers/ProcessWhatsAppWebhookHandler.cs
public async Task<ProcessWebhookResponse> Handle(
    ProcessWhatsAppWebhookRequest request, CancellationToken ct)
{
    // Todo en una sola transacción de EF Core
    var message = new Message
    {
        TenantId  = request.TenantId,
        ConvId    = request.ConvId,
        Content   = request.Content,
        FromNumber = request.FromNumber,
        ReceivedAt = DateTimeOffset.UtcNow
    };
    _db.Messages.Add(message);

    // Guardar el Integration Event en la tabla Outbox (mismo DbContext -> misma transacción)
    _db.OutboxEvents.Add(new OutboxEvent
    {
        TenantId  = request.TenantId,
        EventType = nameof(MessageReceivedIntegrationEvent),
        Payload   = JsonSerializer.Serialize(new MessageReceivedIntegrationEvent(
            request.TenantId, request.ConvId, message.Id,
            request.Content, "whatsapp", message.ReceivedAt))
    });

    await _db.SaveChangesAsync(ct); // <- INSERT message + INSERT outbox_event en 1 transacción
    return new ProcessWebhookSuccess(message.Id);
}
```

---

## Outbox Worker  -  el publicador confiable

```csharp
// Infrastructure/BackgroundServices/OutboxPublisherWorker.cs
public class OutboxPublisherWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await PublishPendingEventsAsync(ct);
            await Task.Delay(TimeSpan.FromSeconds(5), ct); // poll cada 5 segundos
        }
    }

    private async Task PublishPendingEventsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var pending = await db.OutboxEvents
            .Where(e => e.ProcessedAt == null && e.RetryCount < 5)
            .OrderBy(e => e.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var outboxEvent in pending)
        {
            try
            {
                await bus.PublishAsync(outboxEvent.EventType, outboxEvent.Payload, ct);
                outboxEvent.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                outboxEvent.RetryCount++;
                outboxEvent.LastError = ex.Message;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

---

## Idempotencia en el consumidor  -  at-least-once delivery

El Outbox garantiza **at-least-once**: el evento puede publicarse más de una vez si el worker falla entre el PUBLISH y el UPDATE.

**El consumidor debe ser idempotente:**

```csharp
// Consumer de MessageReceivedIntegrationEvent
public async Task ConsumeAsync(MessageReceivedIntegrationEvent evt, CancellationToken ct)
{
    // Verificar si ya procesamos este evento (idempotency key = evt.MessageId)
    var alreadyProcessed = await _db.ProcessedEvents
        .AnyAsync(e => e.EventId == evt.MessageId && e.EventType == "MessageReceived", ct);

    if (alreadyProcessed)
    {
        _logger.LogInformation(
            "Evento {EventType} para mensaje {MessageId} ya fue procesado  -  ignorando",
            "MessageReceived", evt.MessageId);
        return;
    }

    // Procesar el evento (enviar SignalR push al agente asignado)
    await _signalR.SendToTenantAsync(evt.TenantId,
        "new-message", new { evt.ConvId, evt.MessageId, evt.Content });

    // Marcar como procesado
    _db.ProcessedEvents.Add(new ProcessedEvent
        { EventId = evt.MessageId, EventType = "MessageReceived" });
    await _db.SaveChangesAsync(ct);
}
```

---

## Outbox en el sistema de mensajería  -  qué eventos van al Outbox

| Evento | Consumidor | Efecto |
|--------|-----------|--------|
| `MessageReceivedIntegrationEvent` | Notifications | Push SignalR al agente |
| `ConversationCreatedIntegrationEvent` | Analytics | Métricas de volumen |
| `CampaignMessageQueuedIntegrationEvent` | CampaignWorker | Encola para envío |
| `AgentInvitedIntegrationEvent` | Email | Envía email de invitación |
| `ContactImportedIntegrationEvent` | Segmentation | Recalcula segmentos |

> Todos estos eventos requieren que primero se persista la acción en DB.
> El Outbox garantiza que el evento se publica exactamente cuando la DB commita.
> Sin Outbox, cualquier fallo de red entre DB y bus pierde el evento silenciosamente.

---

## Consistencia eventual  -  la realidad del software distribuido

```
Consistencia ACIDA (un solo sistema, una sola transaccion):
  BEGIN TRANSACTION
    INSERT messages            <- atomico
    INSERT outbox_events       <- atomico con el anterior
  COMMIT
  O todo o nada. Inmediato. Sin estado intermedio.

Consistencia EVENTUAL (dos sistemas diferentes):
  1. El mensaje se guarda en PostgreSQL          (inmediato, dentro de la transaccion)
  2. El evento se publica al bus de mensajes     (puede tardar 1-5 segundos)
  3. El agente recibe la notificacion push       (puede tardar 2-10 segundos desde el paso 1)

  Entre el paso 1 y el paso 3, el sistema es "eventualmente consistente":
  el mensaje existe en DB pero la notificacion aun no llego al agente.
  Esto es normal y aceptable en un CRM.
```

**La pregunta clave para cada evento:**
Cuanto retraso es aceptable?

```
Retraso 0 aceptable (debe ser atomico con la accion):
  -> Domain Event (sincrono, MediatR.Publish en memoria, misma transaccion)
  Ejemplo: audit log del mensaje enviado

Retraso de segundos aceptable:
  -> Integration Event + Outbox (asincrono, publicado despues del commit)
  Ejemplo: notificacion push al agente (puede llegar 2s tarde sin problema)
```

---

## Outbox  -  Que pasa sin el patron (casos reales)

```
Caso 1: El bus de mensajes esta caido 30 minutos

  Sin Outbox:
    INSERT mensaje en DB                    [OK]
    PUBLISH evento al bus                   [FALLA]
    El agente nunca recibe la notificacion push. Jamas.
    Los mensajes de esa media hora no generan ninguna alerta.
    El cliente espera respuesta. Nadie sabe que llego un mensaje.

  Con Outbox:
    INSERT mensaje + INSERT outbox_event    [OK, atomico]
    PUBLISH falla -> evento queda en outbox_events con ProcessedAt = NULL
    El worker reintenta cada 5 segundos
    Cuando el bus vuelve, todos los eventos se publican en orden
    El agente recibe las notificaciones con retraso pero ninguna se pierde

Caso 2: La app se reinicia entre el PUBLISH y el UPDATE outbox_events

  Sin Outbox: el evento se perdio definitivamente

  Con Outbox:
    El evento sigue en outbox_events con ProcessedAt = NULL
    El worker lo reintenta al reiniciar -> el consumidor recibe el evento dos veces
    Solucion: el consumidor verifica si ya proceso ese MessageId (idempotencia)
    Resultado: at-least-once delivery con idempotencia = efectivamente exactly-once
```

---

## Deuda técnica  -  El cuadrante de Fowler

```
                  DELIBERADA              ACCIDENTAL
            +------------------+----------------------+
PRUDENTE    | "Lanzamos ahora  | "Ahora entendemos    |
            |  y lo arreglamos |  cómo deberíamos     |
            |  después."       |  haberlo hecho."     |
            |------------------+----------------------|
IMPRUDENTE  | "No hay tiempo   | "¿Qué son capas de   |
            |  para diseñar."  |  arquitectura?"      |
            `------------------+----------------------+
```

**Solo la Deliberada/Prudente es aceptable**  -  y debe documentarse en un ADR.

Las violaciones de Clean Architecture caen siempre en el cuadrante Imprudente.
Son las que bloquean sprints 6 meses después.

---

## Deuda técnica  -  Hacerla visible en el backlog

| Campo | Ejemplo real |
|-------|-------------|
| **Título** | Refactorizar ConversationService  -  God Class |
| **Impacto** | Cada feature de conversaciones tarda 3× más de lo esperado |
| **Criterio de Done** | Clase < 200 líneas, cobertura > 70%, sin warnings |
| **Estimación** | 5 story points |
| **Etiqueta** | `tech-debt`, `hotspot` |

**Regla del 20%:** reservar 20% de la capacidad del sprint para tech debt.
No pedir permiso para hygiene básica  -  forma parte de la Definition of Done.

---

## ADRs como historial de deuda deliberada

```markdown
# ADR-003  -  Monolito Modular sobre Microservicios

**Status:** Accepted   **Date:** 2026-05-01

## Decisión
Monolito Modular. Un deployment, módulos separados en el código.

## Contexto
MVP con 6 personas, dominio no probado en producción todavía.
Microservicios añadirían 4-6 semanas de infraestructura antes de
escribir una línea de lógica de negocio.

## Trade-off aceptado
Escalar un módulo individual requiere extraerlo a microservicio propio.

## Condición de revisión
Cuando un módulo supere 500 req/seg sostenidos por 4 semanas consecutivas.
```

La condición de revisión convierte la deuda en un item medible  -  no en "algún día".

---

## Comunicar deuda técnica al PO

```
 "Necesitamos refactorizar ConversationService porque tiene
    alta complejidad ciclomática y deuda técnica acumulada."

 "Agregar filtros de conversación tarda 3 días cuando debería
    tardar 4 horas, porque toda la lógica está en un archivo
    de 800 líneas. Con 5 puntos de tech debt este sprint,
    las próximas 10 features de conversaciones serán 60% más rápidas."
```

**Estructura que funciona:**
contexto -> impacto medible -> inversión requerida -> retorno esperado.

El PO no entiende "complejidad ciclomática 45".
Sí entiende "3 días vs 4 horas" y "10 features más rápido".

---

## Fin del Bloque I y II  -  Arquitectura de codigo

**Conceptos cubiertos:**
DDD - CQRS - Specification Pattern - Clean Architecture
Vertical Slice - Monolito Modular - Outbox Pattern - Deuda Tecnica

**A continuacion:** Bloque III  -  SaaS Multi-Tenant

---

<!-- _class: lead -->

## SaaS Multi-Tenant
### Aislar datos entre clientes sin duplicar el sistema

---

## El problema sin multi-tenancy

```
Sin multi-tenancy:
  Empresa A -> Deployment A (servidor, DB, config, ops)
  Empresa B -> Deployment B (servidor, DB, config, ops)
  Empresa C -> Deployment C (servidor, DB, config, ops)

N clientes = N deployments = N DBs = N mantenimientos

Con multi-tenancy Pool:
  Un solo deployment
  Una sola base de datos
  Los datos de cada empresa estan aislados por tenant_id
  Empresa A jamas ve datos de Empresa B
```

**El riesgo central:** tenant leakage.
Un query sin filtro de tenant devuelve datos de todos los clientes mezclados.
En produccion con 50 tenants, el primer GET sin filtro es un data breach.

---

## Los 3 modelos de aislamiento

| Modelo | Estructura | Aislamiento | Escalabilidad | Costo |
|--------|-----------|-------------|--------------|-------|
| **Silo** | Una DB por tenant | Maximo (fisico) | Bajo (N DBs, N migraciones) | Alto |
| **Bridge** | Un schema por tenant, misma DB | Alto | Medio | Medio |
| **Pool** | Una DB, columna `tenant_id` | Logico con global filter | Alto | Bajo |

**El back-template usa Pool.**
La seguridad del aislamiento depende del global filter en EF Core.
Sin ese filtro, un solo query expone datos de todos los tenants.

---

## Los 3 modelos  -  Quienes los usan en la realidad

| Modelo | Quien lo usa | Por que lo eligieron |
|--------|-------------|---------------------|
| **Silo** | Salesforce enterprise tier, sistemas bancarios, ERP de gobierno | Contratos que exigen aislamiento fisico, auditorias separadas por cliente |
| **Bridge** | GitLab (schemas por organizacion), algunos SaaS con compliance medio | Balance entre aislamiento y costo operativo |
| **Pool** | Shopify, Slack, Linear, Notion, GitHub, la mayoria de SaaS modernos | Escala masiva, una sola infraestructura, bajo costo operativo |

**Por que el 90% de los SaaS modernos usa Pool:**

```
Un solo schema  ->  un solo migrate en cada deploy
Backups unificados  ->  un snapshot cubre todos los tenants
Un RDS  ->  no N RDS con N facturas y N mantenimientos
Agregar un tenant nuevo  ->  INSERT en la tabla tenants, sin infraestructura nueva
Monitoreo centralizado  ->  una sola DB que observar
```

**El sistema de mensajería usa Pool:**
No hay requerimiento de compliance que exija aislamiento fisico.
El global filter de EF Core da el aislamiento logico necesario.
Cuando la empresa tenga un cliente enterprise que lo exija,
pueden ofrecer un Silo dedicado como tier premium.

---

## Multi-Tenant  -  Flujo de onboarding de un tenant nuevo

```
Empresa "Distribuidora ABC" se registra en el CRM

Paso 1: Registro del workspace
  POST /api/auth/register-workspace
  { companyName: "Distribuidora ABC", adminEmail: "carlos@cliente.example.com" }
        |
        v
  BEGIN TRANSACTION
    INSERT tenants (name, slug="distribuidora-abc", is_active=true)
    INSERT users (tenant_id=nuevo, role="admin", email="carlos@...")
    INSERT tenant_plans (tenant_id=nuevo, plan_id=trial, status="active")
  COMMIT

Paso 2: Primer login del admin
  POST /api/auth/login -> JWT { tenant_id: 42, role: "admin" }
  Todos los requests futuros de Carlos tienen tenant_id=42 en el JWT
  El global filter de AppDbContext filtra automaticamente

Paso 3: Invitar agentes (admin only)
  POST /api/agents/invite { email: "ana@cliente.example.com" }
  INSERT users (tenant_id: 42, role: "agent")
  INSERT outbox_events -> email de invitacion enviado por el worker

Paso 4: Configurar canal WhatsApp (admin only)
  POST /api/channels { type: "whatsapp", config: { token, phoneNumberId } }
  INSERT channels (tenant_id: 42, type: "whatsapp")
  Ya puede recibir conversaciones por WhatsApp
```

---

## Silo  -  cuando tiene sentido

```
Tenant A -> PostgreSQL Workspace_A
Tenant B -> PostgreSQL Workspace_B
Tenant C -> PostgreSQL Workspace_C

Al hacer deploy de una version nueva:
  foreach (var conn in GetAllTenantConnections())
      await MigrateAsync(conn);
```

**Usar Silo cuando:**
- El contrato exige aislamiento fisico (p. ej. clientes de salud o de gobierno; HIPAA no lo exige por si misma, lo pide el contrato)
- Un tenant tiene volumenes que no deben competir con otros
- El cliente tiene derecho contractual a un backup separado

**No usar en el sistema de mensajería:**
MVP con pocos tenants, sin requerimiento de compliance estricto.
El costo operativo del Silo no se justifica en esta etapa.

---

## Pool  -  columna tenant_id en cada tabla

```sql
-- Cada tabla de negocio tiene tenant_id
CREATE TABLE conversations (
    id          BIGSERIAL   PRIMARY KEY,
    tenant_id   BIGINT      NOT NULL,    -- la clave del Pool
    status      VARCHAR(20) NOT NULL,
    agent_id    BIGINT      NULL,
    channel_id  BIGINT      NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT timezone('utc', now())
);

-- Indice critico: la mayoria de queries filtran por tenant primero
CREATE INDEX idx_conversations_tenant ON conversations (tenant_id);

-- Sin el indice, cada query escanea toda la tabla de todos los tenants
```

Un solo schema, un solo `dotnet ef database update`.
El aislamiento lo garantiza la aplicacion, no la DB.

---

## ITenantOwned  -  la interfaz que activa el global filter

```csharp
// Shared.Kernel/ITenantOwned.cs
public interface ITenantOwned
{
    long TenantId { get; }
}
```

```csharp
// Conversations.Domain/Entities/Conversation.cs
public sealed class Conversation : ITenantOwned
{
    public long   Id        { get; init; }
    public long   TenantId  { get; init; }   // implementa ITenantOwned
    public string Status    { get; init; } = string.Empty;
    public long?  AgentId   { get; init; }
    public long   ChannelId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
```

`AppDbContext` detecta automaticamente todas las entidades que implementan `ITenantOwned`
y les inyecta el global filter en tiempo de modelado.
No hay que recordar agregar `WHERE tenant_id = @id` en cada query.

---

## AppDbContext  -  global filter automatico

```csharp
// Shared.Database/AppDbContext.cs
private void ApplyKernelConventions(ModelBuilder builder)
{
    var tenantId = _tenantCtx.CurrentTenantId;   // lee del AsyncLocal

    foreach (var entityType in builder.Model.GetEntityTypes())
    {
        var clrType = entityType.ClrType;

        if (typeof(ITenantOwned).IsAssignableFrom(clrType))
        {
            // Equivalente a: e => e.TenantId == tenantId
            var param  = Expression.Parameter(clrType, "e");
            var prop   = Expression.Property(param, nameof(ITenantOwned.TenantId));
            var filter = Expression.Lambda(
                Expression.Equal(prop, Expression.Constant(tenantId)), param);

            entityType.SetQueryFilter(filter);
        }
    }
}
```

Todo `DbSet<T>` donde `T : ITenantOwned` tiene el filtro inyectado.
EF Core añade `WHERE e.tenant_id = @tenantId` al SQL generado automaticamente.

---

## TenantContextAccessor  -  AsyncLocal

```csharp
// Common.MultiTenancy/TenantContextAccessor.cs
public class TenantContextAccessor : ITenantContextAccessor
{
    // AsyncLocal: cada request tiene su propio valor, aislado del thread pool
    private static readonly AsyncLocal<TenantContextHolder> _holder = new();

    public TenantContext? Current
    {
        get => _holder.Value?.Context;
        set
        {
            var holder = _holder.Value;
            if (holder != null) holder.Context = null;
            if (value != null)
                _holder.Value = new TenantContextHolder { Context = value };
        }
    }

    // Fail-closed: sin contexto retorna 0
    // AppDbContext con tenant_id = 0 no devuelve ninguna fila
    public long CurrentTenantId => Current?.TenantId ?? 0;
}
```

`AsyncLocal<T>` garantiza que el tenant_id de la Request A no contamina la Request B
aunque ambas compartan el mismo thread del pool.

---

## TenantClaimsMiddleware  -  de JWT al contexto

```csharp
// Host.Api/Middleware/TenantClaimsMiddleware.cs
public class TenantClaimsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor tenantCtx)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst("tenant_id")?.Value;
            if (long.TryParse(claim, out var tenantId))
                tenantCtx.Current = new TenantContext(tenantId);
        }
        await next(context);
    }
}
```

```csharp
// Program.cs  -  orden critico del pipeline HTTP
app.UseAuthentication();                          // 1. valida la firma del JWT
app.UseAuthorization();                           // 2. evalua los permisos
app.UseMiddleware<TenantClaimsMiddleware>();       // 3. extrae tenant_id del JWT validado
app.MapControllers();                             // 4. handlers ya tienen contexto correcto
```

---

## El flujo completo  -  de JWT a query filtrada

```
HTTP GET /api/conversations
  Authorization: Bearer { tenant_id: "42", role: "agent" }
        |
        v
UseAuthentication()
  valida firma del JWT
        |
        v
TenantClaimsMiddleware
  tenantCtx.Current = new TenantContext(42)
        |
        v
ConversationsController.GetAll()
  IMediator.Send(new GetConversationsRequest())
        |
        v
GetConversationsHandler
  _db.Conversations.ToListAsync()
        |
        v
AppDbContext global filter inyectado:
  WHERE tenant_id = 42    <- el handler no escribio ningun WHERE
```

El filtro es invisible para el handler pero siempre esta presente.

---

## Tenant leakage  -  el bug mas peligroso en SaaS

```csharp
//  Sin global filter  -  devuelve datos de TODOS los tenants
public async Task<GetConversationsResponse> Handle(
    GetConversationsRequest req, CancellationToken ct)
{
    // En prod con 50 tenants: mezcla conversaciones de todas las empresas
    return await _db.Conversations
        .Select(c => new ConversationDto(c.Id, c.Status))
        .ToListAsync(ct);
}
```

```csharp
//  IgnoreQueryFilters()  -  solo para SysAdmin, nunca en handlers normales
return await _db.Conversations
    .IgnoreQueryFilters()          // bypasea el global filter
    .Where(c => c.TenantId == targetTenantId)   // SysAdmin especifica el tenant explicito
    .ToListAsync(ct);
```

**La trampa real:** en desarrollo con un solo tenant el leakage no se detecta.
En produccion con 50 tenants, el primer usuario que hace un GET ve datos de todos.

---

## Testear aislamiento de tenant

```csharp
[Fact]
public async Task GetConversations_NeverReturns_OtherTenantData()
{
    // Arrange: datos de dos tenants distintos en la misma DB
    await InsertConversationAsync(tenantId: 1, status: "open");
    await InsertConversationAsync(tenantId: 2, status: "open");

    // Act: request autenticado como tenant 1
    SetTenantContext(tenantId: 1);
    var result = await _mediator.Send(new GetConversationsRequest(TenantId: 1));

    // Assert: solo sus propios datos
    Assert.All(result.Items, c => Assert.Equal(1, c.TenantId));
    Assert.DoesNotContain(result.Items, c => c.TenantId == 2);
}
```

Este test debe existir para cada endpoint que devuelve listas.
Es el test de regresion mas critico de un sistema SaaS Pool.

---

## Multi-Tenant en el sistema de mensajería  -  que implementa ITenantOwned

| Entidad | ITenantOwned | Razon |
|---------|-------------|-------|
| `Conversation` | Si | Cada empresa tiene sus propias conversaciones |
| `Contact` | Si | Los contactos pertenecen al workspace |
| `Message` | Si | Los mensajes pertenecen a conversaciones del tenant |
| `Agent` | Si | Los agentes son usuarios del workspace |
| `Channel` | Si | WhatsApp/Instagram configurados por empresa |
| `BotFlow` | Si | Los flujos de bot son por empresa |
| `Tenant` | No | Es la raiz  -  no se filtra por si mismo |
| `PricingPlan` | No | Los planes son globales  -  los define el SysAdmin |

**Regla:** si la entidad contiene datos de negocio de un cliente, implementa `ITenantOwned`.

---

## Multi-Tenant  -  Ciclo de vida del tenant

```
Estado: Trial (los primeros 14 dias)
  tenant_plans.status = "active", plan = "trial"
  Limites reducidos: 3 agentes, 200 conversaciones/mes
  El tenant puede usar el sistema pero con restricciones
        |
        v (el admin elige un plan y paga)
Estado: Activo (plan pagado)
  Stripe webhook: checkout.session.completed
  tenant_plans.status = "active", plan = "pro" (o "basico")
  Limites completos segun el plan contratado
        |
        +-----------> (pago mensual falla)
        |                      |
        v                      v
Estado: Pausado         Estado: Pausado
  (cancelacion         invoice.payment_failed
   voluntaria)         El tenant puede leer pero no puede
        |              crear nuevas conversaciones ni canales
        v
  tenant_plans.status = "paused"
  El tenant tiene 30 dias para exportar sus datos (plazo de la politica de este ejemplo, no de la ley)
        |
        v (si no regulariza o no reanuda)
Estado: Cancelado
  tenant_plans.status = "canceled"
  tenant.is_active = false (soft delete, datos conservados)
  Solo el SysAdmin puede ver sus datos con IgnoreQueryFilters()
        |
        v (si quiere volver)
Estado: Reactivado
  El admin paga de nuevo -> tenant.is_active = true
  Los datos no se borraron  -  retoman donde se quedaron
```

---

<!-- _class: lead -->

## Billing, Planes y Metricas SaaS
### El modelo de negocio detras del CRM

---

## Metricas SaaS que el equipo debe conocer

| Metrica | Que mide | Formula | Ejemplo CRM |
|---------|---------|---------|------------|
| **MRR** | Ingresos recurrentes mensuales | Suma suscripciones activas | 50 x $200 = $10,000 |
| **ARR** | Ingresos anualizados | MRR x 12 | $120,000 |
| **Churn** | % clientes que cancelan por mes | Cancelados / Total x 100 | 2/50 = 4% |
| **LTV** | Valor total del cliente en su vida | ARPU / Churn% | $200 / 0.04 = $5,000 |
| **ARPU** | Ingreso promedio por cliente | MRR / Tenants activos | $200 |
| **CAC** | Costo de adquirir un cliente | Gasto ventas / Clientes nuevos | Variable |

**Por que importan en el codigo:**
El sistema necesita conocer el plan activo del tenant para decidir
que puede hacer y hasta que limite puede llegar.

---

## Modelos de pricing

| Modelo | Como cobra | Ejemplo | Ventaja | Riesgo |
|--------|-----------|---------|---------|-------|
| **Flat** | Precio fijo por mes | $200/mes | Predecible | No refleja el valor real |
| **Per seat** | Por usuario activo | $20/agente/mes | Escala con el equipo | Frena el crecimiento |
| **Usage-based** | Por unidad consumida | $0.005/mensaje | Justa para el cliente | MRR impredecible |
| **Freemium** | Gratis hasta un limite | 500 convs/mes gratis | Adquisicion facil | Conversion compleja |

**El sistema de mensajería usa:** flat + limites de uso.
Precio base por workspace mas quotas de agentes y conversaciones por mes.

---

## Billing  -  Como decidir el modelo de pricing

```
Pregunta 1: El valor que el cliente recibe escala con cuanto usa el sistema?
  Si (mas mensajes = mas valor) -> Usage-based o Freemium
  No (el valor es fijo independiente del uso) -> Flat o Per seat

Pregunta 2: El valor escala con el tamano del equipo del cliente?
  Si (mas agentes = mas ROI para la empresa) -> Per seat (cobrar por agente activo)
  No -> Flat (cobrar precio fijo por workspace)

Pregunta 3: Como quieres que los clientes lleguen al producto?
  Quiero que lo prueben solos, sin ventas -> Freemium (gratis hasta un limite)
  Quiero hacer demos y ventas directas -> Trial con tiempo limitado (14 dias)

Pregunta 4: Puedes predecir y controlar los costos de infraestructura?
  Si -> Usage-based (cobras proporcional a lo que usas)
  No -> Flat o Per seat (ingresos predecibles aunque el uso varie)

Decision para el sistema de mensajería:
  - El valor escala con el equipo (mas agentes = mas conversaciones atendidas)
    -> indica Per seat
  - Queremos simplicidad en el MVP (menos friction al vender)
    -> indica Flat
  - Conclusion: Flat por workspace con quotas por plan
    (incentivar upgrade sin cobrar por cada agente individualmente)
```

---

## Plan vs Feature Flag vs Quota

```
Plan (PricingPlan):
  Lo que el tenant compro este mes.
  "Plan Pro" a $200/mes.
  Define a que features tiene acceso y hasta que limite.

Feature Flag:
  Funcionalidad que se activa o desactiva segun el plan.
  Plan Basico: sin Bot Builder.
  Plan Pro: Bot Builder habilitado.
  Implementacion: booleano en PricingPlan.HasBotBuilder.

Quota (Resource Quota):
  Limite numerico sobre un recurso.
  Plan Basico: max 5 agentes, 1,000 conversaciones/mes.
  Plan Pro: max 20 agentes, 10,000 conversaciones/mes.
  Implementacion: verificar antes de cada INSERT con IResourceQuotaService.
```

---

## PricingPlan  -  la entidad del plan

```csharp
// Identity.Domain/Entities/PricingPlan.cs
public sealed class PricingPlan
{
    public long   Id              { get; init; }
    public Guid   PublicId        { get; init; }
    public string Name            { get; init; } = string.Empty;
    public string PlanType        { get; init; } = "base";    // "base" | "addon"
    public string? StripePriceId  { get; init; }
    public int    AmountCents     { get; init; }   // 20000 = $200.00
    public string Currency        { get; init; } = "usd";
    public string BillingInterval { get; init; } = "monthly";  // "monthly" | "annual"
    public bool   IsActive        { get; init; }

    // Feature flags  -  que puede hacer con este plan
    public bool HasBotBuilder           { get; init; }
    public bool HasCampaigns            { get; init; }
    public bool HasAnalyticsDashboard   { get; init; }

    // Quotas  -  hasta cuanto puede hacer
    public int MaxAgents                 { get; init; }
    public int MaxConversationsPerMonth  { get; init; }
    public int MaxChannels               { get; init; }
}
```

---

## TenantPlan  -  la suscripcion activa del tenant

```csharp
// Identity.Domain/Entities/TenantPlan.cs
public sealed class TenantPlan
{
    public long   Id            { get; init; }
    public long   TenantId      { get; init; }
    public long   PricingPlanId { get; init; }
    public string Status        { get; init; } = "active";  // "active" | "paused" | "canceled"
    public DateTimeOffset  ActivatedAtUtc { get; init; }
    public DateTimeOffset? CanceledAtUtc  { get; init; }

    public PricingPlan Plan { get; init; } = null!;
}
```

```csharp
// Consultar el plan activo del tenant en cualquier handler
var plan = await _db.TenantPlans
    .IgnoreQueryFilters()            // TenantPlan no implementa ITenantOwned
    .Include(tp => tp.Plan)
    .Where(tp => tp.TenantId == tenantId && tp.Status == "active")
    .Select(tp => tp.Plan)
    .FirstOrDefaultAsync(ct);

if (plan is null)
    return new NoActivePlanFailure("El workspace no tiene un plan activo.");
```

---

## IResourceQuotaService  -  verificar limites antes de operar

```csharp
// BillingSaas.Contracts/IResourceQuotaService.cs
public interface IResourceQuotaService
{
    Task<QuotaResult> TryConsumeAsync(
        long   tenantId,
        string resourceCode,
        int    amount = 1,
        CancellationToken ct = default);

    Task ReleaseAsync(
        long   tenantId,
        string resourceCode,
        int    amount = 1,
        CancellationToken ct = default);
}

public sealed record QuotaResult(bool Allowed, int? Limit, int? CurrentUsage);

public static class ResourceCodes
{
    public const string Agents        = "agents";
    public const string Conversations = "conversations";
    public const string Channels      = "channels";
    public const string BotFlows      = "bot-flows";
    public const string Campaigns     = "campaigns";
}
```

---

## Verificar quota en el Handler  -  patron obligatorio

```csharp
// Conversations.Application/UseCases/CreateConversation/CreateConversationHandler.cs
public async Task<CreateConversationResponse> Handle(
    CreateConversationRequest request, CancellationToken ct)
{
    // 1. Verificar quota ANTES de operar
    var quota = await _quotaService.TryConsumeAsync(
        request.TenantId,
        ResourceCodes.Conversations,
        amount: 1, ct);

    if (!quota.Allowed)
        return new CreateConversationQuotaExceededFailure(
            $"Limite mensual alcanzado ({quota.CurrentUsage}/{quota.Limit} conversaciones). " +
            "Actualiza tu plan para continuar.");

    // 2. Operar normalmente
    var conv = new Conversation
    {
        TenantId     = request.TenantId,
        Status       = "open",
        ChannelId    = request.ChannelId,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };
    await _repo.InsertAsync(conv, ct);

    return new CreateConversationSuccess(conv.PublicId);
}
```

---

## BillingMode  -  quien procesa el pago

```csharp
// Identity.Application/Security/BillingModes.cs
public static class BillingModes
{
    public const string Stripe   = "stripe";    // pago via Stripe
    public const string Partner  = "partner";   // revendedor factura por su cuenta
    public const string Internal = "internal";  // tenant interno  -  sin cobro

    public static bool RequiresBilling(string mode) => mode == Stripe;
}
```

| Modo | Quien cobra | Cuando usar en CRM |
|------|------------|-------------------|
| `stripe` | Stripe cobra la tarjeta automaticamente | Clientes SaaS normales |
| `partner` | Un revendedor factura al cliente final | Integradores, white-label |
| `internal` | No se cobra nada | Staging, demos, staff de la empresa |

El tenant de la empresa usa `internal` durante desarrollo y demos:
sin quota de conversaciones, sin facturacion.

---

## Ciclo de vida de una suscripcion  -  Stripe webhooks

```
Cliente elige Plan Pro ($200/mes)
        |
        v
POST /api/billing/checkout
  backend crea Stripe Checkout Session
        |
        v
Cliente paga en la pagina hosteada por Stripe
        |
        v
Stripe envia webhook: checkout.session.completed
        |
        v
WebhookHandler:
  INSERT TenantPlan(status="active")
  UPDATE Tenant.StripeSubscriptionId
        |
        v
Cada mes: invoice.payment_succeeded
  -> WebhookHandler confirma suscripcion activa

Si el pago falla: invoice.payment_failed
  -> WebhookHandler: UPDATE TenantPlan(status="paused")
     El tenant pierde acceso hasta que regularice el pago
```

**Regla:** no confiar en el redirect del frontend para confirmar el pago.
El webhook de Stripe es la unica fuente de verdad.

---

## Billing  -  Upgrade y downgrade de plan

```
Upgrade (Plan Basico -> Plan Pro durante el mes):

  Admin hace clic en "Upgrade a Pro"
        |
        v
  POST /api/billing/upgrade { newPlanId: "pro" }
  Backend crea Stripe Subscription Update con prorrateo (prorates)
        |
        v
  Stripe calcula el costo proporcional:
    Si quedan 15 de 30 dias, cobra 50% del precio de Plan Pro
    Cobra inmediatamente la diferencia
        |
        v
  Webhook: customer.subscription.updated
  UPDATE tenant_plans SET pricing_plan_id = pro_plan_id
  Las nuevas quotas (20 agentes, Bot Builder) aplican INMEDIATAMENTE

Downgrade (Plan Pro -> Plan Basico):

  Admin hace clic en "Bajar a Basico"
        |
        v
  El downgrade entra en vigor al SIGUIENTE ciclo de facturacion
  (el admin ya pago el mes completo de Pro, sigue usandolo hasta el fin del mes)
        |
        v
  Dia 1 del siguiente mes: Stripe cancela Pro, activa Basico
  Si el tenant tiene 12 agentes y baja a Basico (max 5):
    Los 7 agentes extra quedan en estado "suspended" (no pueden loguearse)
    No se borran  -  el admin puede reactivarlos si vuelve a Pro
```

---

## Billing  -  Comunicar quota exceeded correctamente

```
El error comun: mensaje tecnico que no ayuda al usuario

HTTP 429
{ "error": "quota_exceeded", "resource": "conversations", "limit": 1000 }

El usuario ve esto y no sabe que hacer.
```

```csharp
// La respuesta correcta: accionable, con contexto, con camino de salida
if (!quota.Allowed)
    return new CreateConversationQuotaExceededFailure(
        $"Alcanzaste el limite de tu plan: " +
        $"{quota.CurrentUsage} de {quota.Limit} conversaciones este mes. " +
        "Actualiza a Plan Pro para continuar sin interrupciones.");
```

```json
{
  "isSuccess": false,
  "message": "Alcanzaste el limite: 1000 de 1000 conversaciones este mes. Actualiza a Plan Pro para continuar.",
  "data": {
    "limitReached": true,
    "currentUsage": 1000,
    "limit": 1000,
    "resourceCode": "conversations",
    "upgradeUrl": "/settings/billing/upgrade"
  }
}
```

El frontend usa `upgradeUrl` para abrir el modal de upgrade directamente desde el error.
El usuario no necesita navegar: el error le da el camino.
El status HTTP correcto para quota exceeded es **429**, no 400 ni 403.

---

## JWT con claims multi-tenant  -  los campos criticos

```csharp
// Identity.Infrastructure/Security/JwtTokenService.cs
private List<Claim> BuildClaims(User user, string role)
{
    return [
        new Claim(JwtRegisteredClaimNames.Sub, user.PublicId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("tenant_id", user.TenantId.ToString()),   // para TenantClaimsMiddleware
        new Claim("email",     user.Email),
        new Claim("role",      role),                       // para [Authorize(Roles=...)]
        new Claim("name",      $"{user.FirstName} {user.LastName}"),
    ];
}
```

```json
{
  "sub":       "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenant_id": "42",
  "email":     "maria@example.com",
  "role":      "agent",
  "exp":       1718924400
}
```

El `tenant_id` del JWT cierra el ciclo:
JWT -> TenantClaimsMiddleware -> AsyncLocal -> AppDbContext -> global filter.

---

## Roles en el sistema de mensajería

| Rol | Que puede hacer | Que no puede hacer |
|-----|----------------|------------------|
| `admin` | Configurar canales, invitar agentes, ver reportes, gestionar bots, responder | - |
| `agent` | Ver y responder sus conversaciones asignadas, ver contactos | Configurar canales, invitar, ver reportes globales |
| `viewer` | Solo leer: conversaciones, contactos, reportes | Responder, configurar, invitar |

```csharp
[HttpPost("channels")]
[Authorize(Roles = "admin")]                      // solo admin configura canales
public async Task<IActionResult> AddChannel(...) { ... }

[HttpGet("conversations")]
[Authorize(Roles = "admin,agent,viewer")]          // todos pueden leer
public async Task<IActionResult> GetConversations(...) { ... }

[HttpPost("{id}/reply")]
[Authorize(Roles = "admin,agent")]                // viewer no puede responder
public async Task<IActionResult> SendReply(...) { ... }
```

---

## Flujo completo  -  de login a request autorizada

```
1. Login inicial
   POST /api/auth/login { email, password }
        |
        v
   Verificar password: BCrypt.Verify(password, hashedPassword)
   Consultar rol del usuario en el workspace (de la tabla users)
        |
        v
   JwtTokenService.Issue(user, role):
     AccessToken  (15 minutos, firmado con HS256)
     RefreshToken (7 dias, guardado en tabla refresh_tokens)
   Respuesta: { accessToken, refreshToken }

2. Request autenticado
   GET /api/conversations
   Authorization: Bearer eyJ... (AccessToken)
        |
        v
   UseAuthentication()     -> valida firma HS256 y expiracion del JWT
   TenantClaimsMiddleware  -> extrae tenant_id del claim, puebla AsyncLocal
   [Authorize(Roles="admin,agent,viewer")] -> verifica el claim "role"
        |
        v
   Handler ejecuta con tenant_id correcto en AppDbContext (global filter activo)

3. AccessToken expirado (despues de 15 minutos)
   401 Unauthorized
        |
        v
   POST /api/auth/refresh { refreshToken }
   Verifica RefreshToken en DB (no expirado, no usado)
   Emite nuevo AccessToken (15 minutos) sin pedir password
   El usuario no nota la renovacion (el frontend lo maneja transparente)
```

---

## Matriz de permisos  -  quien puede hacer que

| Accion | admin | agent | viewer |
|--------|:-----:|:-----:|:------:|
| Ver todas las conversaciones | Si | Solo asignadas | Si |
| Responder conversaciones | Si | Si | No |
| Asignar conversaciones a agentes | Si | Si (si tiene permiso) | No |
| Ver contactos | Si | Si | Si |
| Importar contactos CSV | Si | No | No |
| Editar o eliminar contactos | Si | No | No |
| Configurar canales (WhatsApp, etc.) | Si | No | No |
| Invitar agentes al workspace | Si | No | No |
| Ver reportes globales | Si | No | Si |
| Crear y editar bot flows | Si | No | No |
| Configurar planes y billing | Si | No | No |
| Exportar datos del workspace | Si | No | No |

Esta tabla es el contrato entre Producto y Desarrollo.
Cada fila es un endpoint (o grupo de endpoints).
Cada columna es un `[Authorize(Roles="...")]` o `[HasPermission("...")]`.
Antes de implementar cualquier endpoint, se define aqui quien puede usarlo.

---

## RBAC con permisos granulares

```csharp
// Permisos por accion  -  mas fino que roles genericos
public static class Permissions
{
    public const string ConversationsRead    = "conversations.read";
    public const string ConversationsReply   = "conversations.reply";
    public const string ConversationsAssign  = "conversations.assign";
    public const string ChannelsManage       = "channels.manage";
    public const string AgentsInvite         = "agents.invite";
    public const string ReportsView          = "reports.view";
    public const string BotFlowsManage       = "bot-flows.manage";
}
```

```csharp
[HttpPost("{id}/assign")]
[HasPermission(Permissions.ConversationsAssign)]   // permiso especifico
public async Task<IActionResult> AssignConversation(Guid id, AssignRequest req, CancellationToken ct)
```

**Ventaja sobre roles puros:**
Un `agent` puede recibir `conversations.assign` sin convertirse en `admin`.
El permiso se asigna por rol en la tabla de roles, no en el codigo.

---

## Los 3 pilares  -  Arquitectura SaaS completa

```
Pilar I  -  Arquitectura de codigo
  DDD          -> modelar entidades, Aggregates, Bounded Contexts
  CQRS         -> separar lectura de escritura, Vertical Slice
  Clean Arch.  -> dependencias apuntan al Domain
  Outbox       -> eventos persistidos antes de publicar al bus

Pilar II  -  Aislamiento de datos
  Pool model   -> tenant_id en cada tabla
  ITenantOwned -> marca las entidades con scope de tenant
  AsyncLocal   -> TenantContextAccessor aislado por request
  Middleware   -> extrae tenant_id del JWT validado
  Leakage test -> cada endpoint de lista tiene su test de aislamiento

Pilar III  -  Modelo de negocio
  PricingPlan  -> define features y quotas por plan
  TenantPlan   -> suscripcion activa del tenant
  Quota check  -> verificar IResourceQuotaService antes de cada INSERT
  BillingMode  -> stripe / partner / internal
  JWT + roles  -> tenant_id + role en cada request
```

Estos tres pilares juntos forman el sistema de mensajería como sistema SaaS produccion-ready.
