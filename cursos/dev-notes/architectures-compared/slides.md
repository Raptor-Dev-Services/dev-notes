<!-- _class: lead -->

# Arquitecturas Comparadas

## Layered  -  Clean  -  Onion  -  Hexagonal  -  Vertical Slice  -  Modular Monolith  -  Microservicios

Material extra  -  Dev Notes
Instructor: Rogelio Arriaga  -  Raptor Dev Services

---

<!-- _class: lead -->

# Parte 1

## El problema que resuelven las arquitecturas

---

## ¿Por qué necesitamos arquitectura?

Sin arquitectura, el código crece así:

```
Semana 1: ContactsController llama a ConversationService
Semana 4: ConversationService llama a ContactsController  (¡ciclo!)
Semana 8: No puedes testear Contact sin levantar la DB completa
Semana 12: Cambiar EF Core por Dapper requiere tocar 40 archivos
```

La arquitectura resuelve tres problemas concretos:

| Problema | Síntoma | Solución |
|----------|---------|----------|
| **Acoplamiento alto** | Cambiar A rompe B, C y D | Dirección de dependencias clara |
| **Baja testabilidad** | No puedes testear sin la DB | Abstracciones (interfaces) en el centro |
| **Difícil evolución** | Agregar feature toca todo | Límites bien definidos entre capas |

---

## La regla de dependencias  -  el hilo conductor

**Todas las arquitecturas de este módulo comparten un principio:**

> Las dependencias apuntan hacia adentro (hacia el dominio), nunca hacia afuera.

```
            +-----------------------------+
            |   Infrastructure / UI       |  <- conoce todo lo de adentro
            |   +---------------------+   |
            |   |   Application       |   |  <- conoce solo el dominio
            |   |   +-------------+   |   |
            |   |   |   Domain    |   |   |  <- no conoce nada de afuera
            |   |   `-------------+   |   |
            |   `---------------------+   |
            `-----------------------------+

  Flecha de dependencia:  Infrastructure -> Application -> Domain
  Domain no importa nada de Application ni de Infrastructure.
```

Esto es lo que hace posible testear el dominio sin base de datos.

---

<!-- _class: lead -->

# Parte 2

## Layered Architecture (N-Tier)

### El punto de partida histórico

---

## Layered Architecture  -  estructura

La arquitectura más antigua y conocida. Cuatro capas horizontales:

```
+--------------------------------------+
|         Presentation Layer           |  Controllers, Views, API endpoints
|--------------------------------------|
|         Business Logic Layer         |  Services, Validators, Rules
|--------------------------------------|
|         Data Access Layer            |  Repositories, ORM, SQL
|--------------------------------------|
|         Database                     |  PostgreSQL, SQL Server, etc.
`--------------------------------------+
```

Cada capa solo llama a la inmediatamente inferior. Simple de entender, fácil de enseñar.

**Lo que NO hace el back-template del CRM:** no tiene una capa BLL global  -  cada módulo tiene su propia Application con casos de uso verticales.

---

## Layered  -  el problema del acoplamiento en cascada

```csharp
//   Layered clásico  -  clases concretas, sin inversión de dependencias
public class ConversationService
{
    private readonly ConversationRepository _repo;  // clase concreta
}

public class ConversationsController
{
    private readonly ConversationService _svc;      // clase concreta
}
```

**Problemas reales:**

- Cambiar EF Core por Dapper obliga a reescribir el DAL **y** revisar el BLL
- Para testear `ConversationsController` necesitas una DB real
- La lógica de negocio termina en el controlador ("fat controllers")
- El dominio (entidades) no tiene lugar propio  -  vive en el DAL

La solución: invertir las dependencias. De ahí nacen Clean, Onion y Hexagonal.

---

<!-- _class: lead -->

# Parte 3

## Clean Architecture

### El back-template como ejemplo real

---

## Clean Architecture  -  las capas concéntricas

Robert C. Martin (Uncle Bob), 2012. Dependencias **solo hacia adentro**:

```
+----------------------------------------------+
|  Infrastructure  /  Presentation  (afuera)   |
|  +----------------------------------------+  |
|  |         Application                    |  |
|  |  +----------------------------------+  |  |
|  |  |           Domain                 |  |  |
|  |  |  Entities  -  Value Objects        |  |  |
|  |  |  Repository interfaces           |  |  |
|  |  `----------------------------------+  |  |
|  |  Use Cases  -  Handlers  -  DTOs           |  |
|  `----------------------------------------+  |
|  EF Core  -  PostgreSQL  -  HTTP  -  WhatsApp API  |
`----------------------------------------------+
```

**Regla de oro:** `Domain` no importa nada de `Application`. `Application` no importa nada de `Infrastructure`.

---

## La arquitectura real del back-template

El back-template **no es solo Clean Architecture**  -  es la combinación de tres patrones:

```
Modular Monolith  +  Clean Architecture  +  Vertical Slice
```

**Estructura real del repositorio:**

```
back-template/
|-- Modules/
|   |-- Users/       -> Users.Domain  -  Users.Application  -  Users.Contracts
|   |                  Users.Infrastructure  -  Users.Presentation  -  Users.Tests
|   `-- Tenancy/     -> Tenancy.Domain  -  Tenancy.Application  -  Tenancy.Presentation
|-- Shared/
|   `-- Authentication/ -> Auth.Domain  -  Auth.Application
|                         Auth.Infrastructure  -  Auth.Presentation
|-- Common/Common/   -> Messaging  -  Results  -  ViewModels  -  MultiTenancy
`-- Host.Api/        -> Program.cs  (DI root  -  wires todo junto)
```

Un solo proceso. Un deploy. Módulos con capas propias.

---

## Clean Architecture  -  flujo de dependencias en el back-template

Traducido al módulo Users (espejo exacto para Contacts, Conversations, Agents, Channels):

```
Users.Domain          <- no depende de nada externo
   ↑
Users.Application     <- depende de Users.Domain (interfaces), Common
   ↑
Users.Infrastructure  <- depende de Users.Domain (implementa repos), EF Core, Dapper
   ↑
Users.Presentation    <- depende de Users.Application (envía Requests), Common.ViewModels
   ↑
Host.Api              <- wires todo junto (DI root)
```

`Users.Domain` **nunca importa** `Microsoft.EntityFrameworkCore`. `Users.Application` **nunca importa** `Users.Infrastructure`. La dirección de la flecha es siempre hacia adentro.

---

## Domain  -  entidad real del back-template

```csharp
// Users.Domain/Entities/UserProfile.cs
namespace Users.Domain.Entities;

public sealed class UserProfile
{
    public long     Id           { get; init; }
    public Guid     PublicId     { get; init; }
    public long     TenantId     { get; init; }
    public string   FullName     { get; init; } = string.Empty;
    public bool     IsActive     { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
```

Equivalente CRM: `Contact`, `Conversation`, `Agent`, `Channel` tendrán la misma estructura: `long Id`, `Guid PublicId`, `long TenantId`, `bool IsActive`, timestamps `init`.

Zero dependencias de EF Core. Zero dependencias de Application.

---

## Domain  -  interfaz del repositorio

```csharp
// Users.Domain/Repositories/IUserProfileRepository.cs
using Users.Domain.Entities;
namespace Users.Domain.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByPublicIdAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserProfile>> GetPagedAsync(
        long tenantId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        long tenantId, CancellationToken cancellationToken = default);

    Task InsertAsync(
        Guid publicId, long tenantId, string fullName, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid publicId, long tenantId, string fullName, CancellationToken cancellationToken = default);

    Task<bool> DisableAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default);
}
```

---

## Application  -  el patrón Request / Response

Cada Use Case tiene exactamente tres piezas: Request, Handler, Responses.

```csharp
// Users.Application/UseCases/GetUserProfile/GetUserProfileRequest.cs
using Common.Messaging;
using Users.Application.UseCases.GetUserProfile.Responses;
namespace Users.Application.UseCases.GetUserProfile;

public sealed record GetUserProfileRequest(Guid PublicId, long TenantId)
    : IRequest<GetUserProfileResponse>;
```

```csharp
// Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileResponse.cs
using Common.Messaging;
namespace Users.Application.UseCases.GetUserProfile.Responses;

public abstract record GetUserProfileResponse : IResponse;
```

El `TenantId` viaja dentro del Request  -  el Handler no accede al HttpContext.

---

## Application  -  los Responses: Success y Failure

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

Interfaces de Common.Results:
- `IFailure`  -  cualquier error (tiene `string Message`)
- `INotFoundFailure : IFailure`  -  recurso no encontrado
- `ISuccess<T>`  -  éxito con datos tipados

---

## Application  -  el Handler

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

    public GetUserProfileHandler(IUserProfileRepository profiles)
        => _profiles = profiles;

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

## Application  -  el Contracts DTO

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

`Users.Contracts` es el proyecto que otros módulos pueden referenciar.
Contiene DTOs y eventos de integración  -  sin dependencias de EF Core ni de Domain.

Equivalente CRM: `ContactDto`, `ConversationDto`, `AgentDto` vivirán en sus respectivos `*.Contracts`.

---

## Infrastructure  -  la implementación del repositorio

```csharp
// Users.Infrastructure/Repositories/UserProfileRepository.cs
using Microsoft.EntityFrameworkCore;
using Shared.Database;
using Users.Domain.Entities;
using Users.Domain.Repositories;
namespace Users.Infrastructure.Repositories;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _db;

    public UserProfileRepository(AppDbContext db) => _db = db;

    public Task<UserProfile?> GetByPublicIdAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default) =>
        _db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PublicId == publicId && e.IsActive, cancellationToken);
}
```

`UserProfileRepository` sabe de EF Core. `IUserProfileRepository` (en Domain) no sabe nada.
En tests se inyecta un fake de `IUserProfileRepository`  -  EF Core nunca entra.

---

## Presentation  -  el Presenter con pattern matching

```csharp
// Users.Presentation/Presenters/GetUserProfilePresenter.cs
using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Users.Application.UseCases.GetUserProfile.Responses;
using Users.Contracts.Dtos;
using Users.Presentation;
namespace Users.Presentation.Presenters;

public sealed class GetUserProfilePresenter
    : INotificationHandler<GetUserProfileResponse>
{
    private readonly ResultViewModel<UsersController> _viewModel;

    public GetUserProfilePresenter(ResultViewModel<UsersController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(GetUserProfileResponse notification, CancellationToken cancellationToken)
    {
        if (notification is IFailure failure)        // <- IFailure PRIMERO
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<UserProfileDto> success)
            _viewModel.Set(success);

        return Task.CompletedTask;
    }
}
```

---

## Presentation  -  el Controller

```csharp
// Users.Presentation/Controllers/UsersController.cs
using Common.Messaging;
using Common.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Users.Application.UseCases.GetUserProfile;
using Users.Application.UseCases.GetUserProfile.Responses;
namespace Users.Presentation;

[Route("api/users")]
[Authorize]
public sealed class UsersController : BaseApiController
{
    private readonly ResultViewModel<UsersController> _viewModel;

    private long CurrentTenantId =>
        long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetUserProfileRequest(id, CurrentTenantId), ct);
        if (_viewModel.IsSuccess) return Ok(_viewModel);
        return result is GetUserProfileNotFoundFailure ? NotFound(_viewModel) : StatusCode(500, _viewModel);
    }
}
```

---

## Host.Api  -  el DI root (Program.cs real)

```csharp
// Host.Api/Program.cs   -  fragmento real
using Tenancy.Application;
using Tenancy.Infrastructure;
using Tenancy.Presentation;
using Users.Application;
using Users.Infrastructure;
using Users.Presentation;

// Mediator  -  un solo registro con todos los assemblies de Application
builder.Services.AddMediator(
    typeof(Tenancy.Application.ServiceCollectionEx).Assembly,
    typeof(Users.Application.ServiceCollectionEx).Assembly,
    typeof(Authentication.Application.ServiceCollectionEx).Assembly
);

// Cada módulo registra sus propias dependencias
builder.Services.AddUsersApplicationServices();
builder.Services.AddUsersInfrastructureServices();
builder.Services.AddUsersWebApiServices();

app.UseMiddleware<TenantClaimsMiddleware>();
app.MapControllers();
```

`Host.Api` es el único proyecto que conoce a todos los módulos. Los módulos no se conocen entre sí.

---

<!-- _class: lead -->

# Parte 4

## Onion Architecture

### Anillos en lugar de capas

---

## Onion Architecture  -  el modelo de anillos

Jeffrey Palermo, 2008. Misma idea que Clean, diferente metáfora y énfasis:

```
          +----------------------------------+
          |          Infrastructure          |
          |   +--------------------------+   |
          |   |       Application        |   |
          |   |   +------------------+   |   |
          |   |   | Domain Services  |   |   |
          |   |   | +------------+   |   |   |
          |   |   | |   Domain   |   |   |   |
          |   |   | |   Model    |   |   |   |
          |   |   | `------------+   |   |   |
          |   |   `------------------+   |   |
          |   `--------------------------+   |
          `----------------------------------+
```

**La diferencia clave con Clean:** Onion define "Domain Services" como anillo explícito entre el Domain Model y Application. El Domain Model (entidades + VOs) está en el núcleo absoluto.

---

## Onion vs Clean  -  diferencias prácticas

| Aspecto | Clean Architecture | Onion Architecture |
|---------|-------------------|-------------------|
| Metáfora | Capas concéntricas | Anillos de cebolla |
| Domain Services | Viven en Application | Anillo propio |
| Interfaces de repo | En Domain | En Domain |
| Énfasis | Dirección de dependencias | El modelo de dominio como núcleo |
| En la práctica | Separación Domain / App / Infra | Domain Services más prominentes |

**En código, son casi idénticos.** La diferencia es filosófica:

- **Clean:** énfasis en la regla de dependencias (la flecha)
- **Onion:** énfasis en el modelo de dominio como núcleo irremplazable

El back-template sigue Clean Architecture. Onion y Clean se confunden frecuentemente porque comparten el mismo principio fundamental.

---

<!-- _class: lead -->

# Parte 5

## Hexagonal Architecture

### Ports & Adapters

---

## Hexagonal  -  la idea central

Alistair Cockburn, 2005. También conocida como **Ports & Adapters**.

```
           +-----------------------------------------+
 HTTP ----|  Primary Adapter                        |
           |  (Controller)     Application           |
 CLI  ----|  Primary Adapter  (Hexágono)            |<-- IContactRepository  (Port)
           |  (Console)                              |-- PostgreSQL Adapter
 Test ----|  Primary Adapter                        |-- WhatsApp API Adapter
           |  (Test)                                 |-- Email Adapter
           `-----------------------------------------+
```

- **Ports (puertos):** interfaces que define el hexágono  -  `IContactRepository`, `IWhatsAppGateway`
- **Primary Adapters:** quienes invocan a la app  -  HTTP, CLI, tests de integración
- **Secondary Adapters:** implementaciones que la app usa  -  EF Core, APIs externas, Redis

---

## Hexagonal  -  Ports & Adapters en el sistema de mensajería

```
Primary Adapters (invocan a la app):      Secondary Adapters (la app los usa):
---------------------------------         ------------------------------------
ContactsController (HTTP)                 PostgreSQL via EF Core
  -> GetContactRequest                       <- IContactRepository
ConversationsController (HTTP)            WhatsApp API Client
  -> AssignConversationRequest               <- IWhatsAppGateway
WebhookController (HTTP)                  SendGrid (Email)
  -> ProcessInboundMessageRequest            <- IEmailSender
IntegrationTest (Test)                    Redis (Cache)
  -> CreateContactRequest                    <- ICacheService
```

**Equivalencia exacta con el back-template:**
- Primary Adapters = `*.Presentation` (UsersController, etc.)
- Secondary Adapters = `*.Infrastructure` (UserProfileRepository, etc.)
- Ports = interfaces en `*.Domain` (`IUserProfileRepository`, etc.)

Hexagonal y Clean Architecture son el mismo patrón con diferente vocabulario.

---

<!-- _class: lead -->

# Parte 6

## Vertical Slice Architecture

### Organización por feature, no por capa

---

## Vertical Slice  -  el cambio de perspectiva

En lugar de organizar por capa técnica, se organiza por **feature completa**:

```
Clean (horizontal):              Vertical Slice:
--------------------             -----------------------------
Domain/Entities/                 Features/
  Contact.cs                       Contacts/
  Conversation.cs                    CreateContact/
Application/UseCases/                  CreateContactCommand.cs
  CreateContact/                       CreateContactHandler.cs
  GetContacts/                         CreateContactValidator.cs
Infrastructure/Repos/              Conversations/
  ContactRepo.cs                     AssignConversation/
Presentation/                        CloseConversation/
  ContactsController.cs
```

Un PR para "Crear Contacto" toca **una carpeta**. En Clean puro toca **cuatro proyectos**.

---

## Vertical Slice  -  en el back-template (estructura real)

El back-template aplica Vertical Slice **dentro de Application**, no en todo el proyecto:

```
Users.Application/
`-- UseCases/
    |-- GetUserProfile/          <- slice autónomo
    |   |-- GetUserProfileRequest.cs
    |   |-- GetUserProfileHandler.cs
    |   `-- Responses/
    |       |-- GetUserProfileResponse.cs    <- abstract record
    |       |-- GetUserProfileSuccess.cs     <- ISuccess<UserProfileDto>
    |       `-- GetUserProfileNotFoundFailure.cs  <- INotFoundFailure
    |-- GetUserProfiles/         <- slice independiente
    |-- UpdateUserProfile/
    `-- DisableUserProfile/
```

Cada slice tiene su propia jerarquía de Responses. No hay `Response<T>` genérica compartida que mezcle todos los casos.

---

## Vertical Slice  -  ventajas y trade-offs

### Ventajas
- Cada slice es autónomo: Request + Handler + Responses en un directorio
- PRs pequeños y enfocados  -  sin conflictos entre features paralelas
- Borrar una feature: borrar una carpeta
- Equipos paralelos trabajan sin pisar código de otros

### Trade-offs

```
 Duplicación: dos handlers pueden tener lógica similar
 Consistencia difícil: cada slice puede evolucionar con su propio estilo
 Más libertad = más disciplina requerida
 Tendencia a integration tests por feature, no unit tests por capa
```

**El back-template mitiga esto:** Domain e Infrastructure son horizontales (compartidos por todos los slices del módulo). Solo Application es vertical.

---

<!-- _class: lead -->

# Parte 7

## Modular Monolith

### Módulos con contratos bien definidos en un solo proceso

---

## Modular Monolith  -  qué es

Un **Modular Monolith** corre en **un solo proceso** con **módulos bien aislados**:

```
+-------------------------------------------------------------+
|                   Sistema de mensajería (un proceso)                |
|                                                             |
|  +--------------+  +--------------+  +------------------+  |
|  |  Contacts    |  |Conversations |  |    Agents        |  |
|  |  Module      |  |  Module      |  |    Module        |  |
|  |--------------|  |--------------|  |------------------|  |
|  |.Domain       |  |.Domain       |  |.Domain           |  |
|  |.Application  |  |.Application  |  |.Application      |  |
|  |.Infrastructure| |.Infrastructure| |.Infrastructure   |  |
|  |.Presentation |  |.Presentation |  |.Presentation     |  |
|  `--------------+  `--------------+  `------------------+  |
|                                                             |
|  +--------------+  +--------------+  Host.Api (DI root)    |
|  |  Channels    |  |Tenancy       |                         |
|  |  Module      |  |Module        |                         |
|  `--------------+  `--------------+                         |
`-------------------------------------------------------------+
              Un solo deploy  -  Una sola base de datos
```

---

## Modular Monolith  -  Host.Api como DI root real

Esto es exactamente lo que hace `Host.Api/Program.cs` en el back-template:

```csharp
// Host.Api/Program.cs  -  los módulos se ensamblan aquí

builder.Services.AddMediator(
    typeof(Tenancy.Application.ServiceCollectionEx).Assembly,
    typeof(Users.Application.ServiceCollectionEx).Assembly,
    typeof(Authentication.Application.ServiceCollectionEx).Assembly
);

// Cada módulo registra sus propias dependencias
builder.Services.AddTenancyApplicationServices();
builder.Services.AddTenancyInfrastructureServices();
builder.Services.AddTenancyWebApiServices();

builder.Services.AddUsersApplicationServices();
builder.Services.AddUsersInfrastructureServices();
builder.Services.AddUsersWebApiServices();
```

`Host.Api` conoce a todos. Los módulos **no se conocen entre sí**.

---

## Modular Monolith  -  Tenant entity como ejemplo de dominio compartido

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

El módulo `Tenancy` provee el concepto de tenant. Los demás módulos lo consumen via `Tenancy.Contracts`  -  nunca referenciando directamente `Tenancy.Domain`.

---

## Modular Monolith  -  el camino a microservicios

El Modular Monolith es el **paso intermedio natural**:

```
Etapa 1: Monolito sin estructura
  -> Código mezclado, difícil de mantener

Etapa 2: Modular Monolith  (<- aquí estamos en el CRM)
  -> Módulos aislados con contratos, un solo deploy
  -> Contacts / Conversations / Agents / Channels como módulos

Etapa 3: Microservicios (si un módulo necesita escalar solo)
  -> Ese módulo se separa en servicio independiente
  -> IContactRepository -> ContactHttpClient (mismo contrato, distinta impl.)
  -> El resto del sistema no cambia
```

```csharp
// La transición en Program.cs:
// Antes (en proceso, Modular Monolith):
services.AddScoped<IContactRepository, ContactRepository>();

// Después (HTTP, mismo contrato):
services.AddHttpClient<IContactRepository, ContactHttpClient>(
    c => c.BaseAddress = new Uri(config["Services:Contacts:Url"]!));
```

---

<!-- _class: lead -->

# Parte 8

## Multi-Tenancy baked in

### Cómo el `tenantId` fluye de extremo a extremo

---

## Multi-tenancy  -  el flujo completo

```
HTTP Request
  Header: Authorization: Bearer <jwt con claim "tenant_id">
       |

TenantClaimsMiddleware  (Host.Api)
  -> extrae tenant_id del JWT
  -> lo escribe en TenantContextAccessor
       |

UsersController  (Users.Presentation)
  private long CurrentTenantId =>
      long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0;
       |

GetUserProfileRequest(id, CurrentTenantId)
       |

GetUserProfileHandler
  await _profiles.GetByPublicIdAsync(request.PublicId, request.TenantId, ct)
       |

UserProfileRepository (EF Core)
  WHERE PublicId = @publicId  AND  TenantId = @tenantId  AND  IsActive = true
```

El `tenantId` viaja explícitamente dentro del Request. El Handler nunca toca el HttpContext.

---

## Multi-tenancy  -  el Request lleva el tenantId

```csharp
// El TenantId es parte del contrato del caso de uso
public sealed record GetUserProfileRequest(Guid PublicId, long TenantId)
    : IRequest<GetUserProfileResponse>;
```

```csharp
// El Controller extrae el tenantId del JWT y lo inyecta en el Request
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
{
    var result = await Mediator.Send(
        new GetUserProfileRequest(id, CurrentTenantId), ct);

    if (_viewModel.IsSuccess) return Ok(_viewModel);

    return result is GetUserProfileNotFoundFailure
        ? NotFound(_viewModel)
        : StatusCode(500, _viewModel);
}
```

**Por qué así:** el Handler es testeable sin HttpContext  -  se le pasa el tenantId directamente en el test.

---

## Multi-tenancy  -  el repositorio filtra por tenantId

```csharp
// Users.Infrastructure/Repositories/UserProfileRepository.cs
public Task<UserProfile?> GetByPublicIdAsync(
    Guid publicId, long tenantId, CancellationToken cancellationToken = default) =>
    _db.UserProfiles
        .AsNoTracking()
        .FirstOrDefaultAsync(
            e => e.PublicId == publicId && e.IsActive,
            cancellationToken);
```

La entidad `UserProfile` tiene `long TenantId { get; init; }`. El repositorio pasa el `tenantId` para que el query filter de EF Core (global filter por tenant) lo aplique automáticamente.

**Para el CRM:** `Contact`, `Conversation`, `Agent`, `Channel` tendrán el mismo `long TenantId` y el mismo patrón de filtrado.

---

<!-- _class: lead -->

# Parte 9

## Microservicios

### Cuándo tiene sentido  -  y cuándo no

---

## Microservicios  -  qué son

Cada servicio es un proceso independiente con su propia DB, deploy y ciclo de release:

```
+------------+  +--------------+  +---------------+  +--------------+
|  Contacts  |  |Conversations |  |   Messages    |  |   Channels   |
|  Service   |  |  Service     |  |   Service     |  |   Service    |
|------------|  |--------------|  |---------------|  |--------------|
| PostgreSQL |  |  PostgreSQL  |  |   PostgreSQL  |  |  PostgreSQL  |
|  (propia)  |  |  (propia)    |  |   (propia)    |  |  (propia)    |
`------------+  `--------------+  `---------------+  `--------------+
      ^                ^                  ^                  ^
      `----------------+--- API Gateway --+------------------+
                             (YARP / Nginx)
                                  ^
                             React 19 Frontend
```

**Ventaja real:** escalar `Conversations` de forma independiente sin tocar `Contacts`.

---

## Microservicios  -  los costos de distribución

Lo que se gana en escalabilidad se paga en complejidad:

| Problema en Monolito | Equivalente en Microservicios |
|---------------------|-------------------------------|
| Llamada a método (nanosegundos) | Llamada HTTP (milisegundos, puede fallar) |
| Transacción ACID entre tablas | Saga Pattern (puede quedar inconsistente) |
| Un solo log en un archivo | Distributed Tracing (Jaeger, OpenTelemetry) |
| Un deploy | Kubernetes + registry + health checks por servicio |
| Debug con breakpoint | Correlación de logs entre 5 servicios |
| Cambiar un contrato | Versionado de API (breaking changes cross-service) |

**Consistencia eventual:** si `Conversations` llama a `Contacts` por HTTP y `Contacts` está caído, necesitas circuit breakers, retries, timeouts, dead letter queues.

---

## Por qué NO empezar con microservicios


```
Equipo de 4-6 personas + dominio en exploración + plazo de 24 semanas:

Microservicios desde el día 1:
  [Err] 5 bases de datos para configurar
  [Err] API Gateway + service discovery
  [Err] Tracing distribuido para debuggear
  [Err] Sagas para cada operación multi-módulo
  [Err] Versionado de contratos entre servicios
  [Err] CI/CD pipeline por cada servicio
  -> Meses antes de tener una feature funcionando

Modular Monolith (back-template):
  [OK] Un solo deploy, una sola DB
  [OK] Módulos con contratos explícitos (ServiceCollectionEx por módulo)
  [OK] Se puede extraer un módulo a microservicio cuando sea necesario
  [OK] El equipo entiende el dominio antes de distribuirlo
  -> Features funcionando desde la semana 1
```

---

<!-- _class: lead -->

# Parte 10

## Tabla Comparativa

### Todas las arquitecturas frente a frente

---

## Comparativa  -  criterios técnicos

| Arquitectura | Testabilidad | Acoplamiento | Costo entrada | Curva |
|-------------|-------------|--------------|---------------|-------|
| **Layered (N-Tier)** | Baja (DB en cascada) | Alto | Bajo | Baja |
| **Clean Architecture** | Alta (Domain aislado) | Bajo | Medio | Media |
| **Onion Architecture** | Alta (igual que Clean) | Bajo | Medio | Media |
| **Hexagonal (P&A)** | Muy alta (adapters intercambiables) | Muy bajo | Medio-Alto | Media-Alta |
| **Vertical Slice** | Media (integration tests) | Medio (por feature) | Bajo | Baja-Media |
| **Modular Monolith** | Alta (módulos aislados) | Bajo entre módulos | Medio-Alto | Alta |
| **Microservicios** | Alta (por servicio) | Muy bajo | Muy alto | Muy alta |

---

## Comparativa  -  cuándo usar cada una

| Arquitectura | Cuándo usar |
|-------------|-------------|
| **Layered** | Prototipos, apps CRUD simples, aprendizaje inicial |
| **Clean Architecture** | La gran mayoría de proyectos de negocio |
| **Onion Architecture** | Dominio muy rico (DDD táctico intensivo) |
| **Hexagonal** | Múltiples canales de entrada (HTTP + CLI + eventos) |
| **Vertical Slice** | Dentro de Application para organizar Use Cases |
| **Modular Monolith** | SaaS, equipos medianos, paso previo a microservicios |
| **Microservicios** | Escala masiva, equipos independientes por servicio |

**El back-template:** Modular Monolith + Clean Architecture + Vertical Slice  -  las tres juntas.

---

## Comparativa  -  mitos comunes

| Mito | Realidad |
|------|---------|
| "Onion y Clean son diferentes" | Son casi idénticos en práctica  -  diferente vocabulario |
| "Hexagonal es más moderna que Clean" | Son de la misma era (2005-2008), enfoques complementarios |
| "Vertical Slice reemplaza a Clean Architecture" | Se usan juntas  -  VSA organiza los Use Cases dentro de Clean |
| "Microservicios = arquitectura moderna" | Son un estilo de despliegue; agregan complejidad real |
| "Modular Monolith = monolito viejo" | Es la elección correcta para la mayoría de proyectos nuevos |
| "El back-template es solo Clean Architecture" | Es Modular Monolith + Clean + Vertical Slice |

---

<!-- _class: lead -->

# Parte 11

## El sistema de mensajería en el back-template

### La arquitectura concreta del producto

---

## Sistema de mensajería  -  módulos reales

Contexto: 4-6 devs, dominio en exploración, entrega en 24 semanas.

```
Decisión: Modular Monolith + Clean Architecture + Vertical Slice
```

```
mensajeria/
|-- Modules/
|   |-- Contacts/       -> Contacts.Domain  -  .Application  -  .Contracts
|   |                     .Infrastructure  -  .Presentation  -  .Tests
|   |-- Conversations/  -> Conversations.Domain  -  .Application  -  .Contracts
|   |                     .Infrastructure  -  .Presentation  -  .Tests
|   |-- Agents/         -> Agents.Domain  -  .Application  -  .Contracts
|   |                     .Infrastructure  -  .Presentation  -  .Tests
|   `-- Channels/       -> Channels.Domain  -  .Application  -  .Contracts
|                         .Infrastructure  -  .Presentation  -  .Tests
|-- Shared/Authentication/
|-- Common/Common/      -> Messaging  -  Results  -  ViewModels  -  MultiTenancy
`-- Host.Api/           -> Program.cs (DI root)
```

---

## Sistema de mensajería  -  Vertical Slice de un módulo

```
Contacts.Application/
`-- UseCases/
    |-- GetContact/
    |   |-- GetContactRequest.cs         <- record con Guid PublicId, long TenantId
    |   |-- GetContactHandler.cs         <- IRequestHandler<GetContactRequest, GetContactResponse>
    |   `-- Responses/
    |       |-- GetContactResponse.cs    <- abstract record : IResponse
    |       |-- GetContactSuccess.cs     <- ISuccess<ContactDto>
    |       `-- GetContactNotFoundFailure.cs  <- INotFoundFailure
    |-- CreateContact/
    |   |-- CreateContactRequest.cs
    |   |-- CreateContactHandler.cs
    |   `-- Responses/
    |       |-- CreateContactResponse.cs
    |       |-- CreateContactSuccess.cs
    |       `-- CreateContactConflictFailure.cs  <- IConflictFailure
    |-- ImportContacts/
    `-- DisableContact/
```

---

## Sistema de mensajería  -  el stack completo

```
                 +--------------------------------------+
                 |         React 19 Frontend            |
                 |   Bandeja  -  Chat  -  Bot Builder       |
                 `------------------+-------------------+
                                    | HTTPS + JWT
                 +-------------------------------------+
                 |     .NET 10 WebApi (un proceso)       |
                 |                                       |
                 |  +----------+ +----------+           |
                 |  | Contacts | |Conversat.|  Agents   |
                 |  | Module   | | Module   |  Module   |
                 |  `----------+ `----------+           |
                 |  +----------+  Common + Tenancy      |
                 |  | Channels |  Authentication        |
                 |  | Module   |  Host.Api (DI root)    |
                 |  `----------+                        |
                 `------------------+-------------------+
                                    |
                 +-------------------------------------+
                 |   PostgreSQL (schemas por módulo)     |
                 `--------------------------------------+
```

---

## Resumen  -  la frase que hay que recordar

> La arquitectura no es sobre qué tecnología usar.
> Es sobre **dónde poner las dependencias** para que el sistema pueda cambiar sin dolor.

**En el sistema de mensajería:**
- **Domain** (`Contact`, `Conversation`, `Agent`, `Channel`)  -  sin dependencias externas
- **Application** (Use Cases con CQRS, Vertical Slice)  -  solo depende de Domain
- **Infrastructure** (EF Core, WhatsApp API, S3)  -  implementa las interfaces de Domain
- **Presentation** (Controllers + Presenters)  -  envía Requests a Application via Mediator
- **Host.Api**  -  DI root, wires todo, aplica middleware de multi-tenancy

Esta estructura permite:
- Testear el dominio sin base de datos (fake de `IContactRepository`)
- Cambiar EF Core sin tocar la lógica de negocio
- Agregar canal Telegram sin modificar `Conversations`
- Extraer un módulo a microservicio sin reescribir los demás

---

*Material extra  -  Arquitecturas Comparadas*
*Dev Notes  -  Raptor Dev Services*
