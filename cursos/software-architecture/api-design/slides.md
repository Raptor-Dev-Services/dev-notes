<!-- _class: lead -->

# API Design
## REST  -  Paginación  -  Errores  -  Buenas Prácticas

**Dev Notes**
Rogelio Arriaga  -  Raptor Dev Services  -  2026

---

<!-- _class: lead -->

# Parte 1
## REST  -  Semántica HTTP y Recursos

---

## ¿Qué es REST?

REST no es un protocolo ni un estándar  -  es un **estilo arquitectónico** con 6 constraints.

| Constraint | Qué significa |
|---|---|
| **Client-Server** | Frontend y backend son independientes |
| **Stateless** | Cada request contiene toda la info  -  sin sesiones en el servidor |
| **Cacheable** | Las responses indican si pueden cachearse |
| **Uniform Interface** | URL = recurso  -  Verbo HTTP = acción  -  JSON = representación |
| **Layered System** | El cliente no sabe si hay proxy o load balancer en el medio |
| **Code on Demand** | Opcional  -  raramente usado |

> **Stateless es el más importante:** JWT en el header de cada request, no sesiones en el servidor.

---

## Verbos HTTP  -  semántica correcta

| Verbo | Semántica | Idempotente | Body |
|---|---|---|---|
| `GET` | Leer  -  nunca modifica estado | [OK] | No |
| `POST` | Crear recurso nuevo | [Err] | Sí |
| `PUT` | Reemplazar recurso completo | [OK] | Sí |
| `PATCH` | Actualización parcial | [Err] | Sí |
| `DELETE` | Eliminar recurso | [OK] | No |

**Idempotente:** repetir la misma request N veces produce el mismo resultado que ejecutarla una sola vez.
`DELETE /api/contacts/abc-123`  -  ejecutarlo dos veces: el resultado es el mismo (contacto eliminado).

---

## Recursos  -  la unidad de diseño

En REST, todo se modela como un **recurso** (sustantivo, nunca verbo):

```
Colección     -> Plural en la URI
/api/conversations               -  todas las conversaciones
/api/conversations/abc-123       -  una conversación específica
/api/conversations/abc-123/messages    -  mensajes de esa conversación
/api/contacts                    -  todos los contactos
/api/contacts/def-456            -  un contacto específico
```

**Reglas de URIs:**

```
[OK] Sustantivos, plural, minúsculas con guiones
   /api/contacts          [OK]
   /api/getContacts       [Err]  <- verbo en la URI
   /api/Contact           [Err]  <- singular y mayúscula
   /api/contact_list      [Err]  <- guion bajo
```

---

## Acciones que no son CRUD

Algunas operaciones no encajan en el modelo CRUD  -  se modelan como **sub-recursos de acción**:

```
POST /api/conversations/abc-123/assign      <- asignar conversación a agente
POST /api/conversations/abc-123/close       <- cerrar conversación
POST /api/contacts/def-456/archive          <- archivar contacto
POST /api/auth/logout                       <- acción de dominio
POST /api/messages/ghi-789/resend           <- reenviar mensaje fallido
```

La regla: si la operación tiene un side-effect que no es crear/leer/actualizar/borrar un recurso, se modela como un sub-recurso de acción con `POST`.

---

## PUT vs PATCH  -  diferencia crítica

```http
PUT /api/contacts/def-456
{
  "firstName": "María",
  "lastName": "González",
  "email": "maria@example.com",
  "phone": "+52 55 1234 5678",
  "isActive": true
}
```

El cliente envía el **recurso completo**  -  los campos no enviados se borran o resetean.

```http
PATCH /api/contacts/def-456
{
  "phone": "+52 55 9999 0000"
}
```

El cliente envía **solo los cambios**  -  los otros 4 campos quedan igual.

> El back-template usa **PUT** para simplificar: el frontend siempre envía el objeto completo.

---

<!-- _class: lead -->

# Parte 2
## Contratos  -  Versionado y Naming

---

## Versionado desde el primer día

```
[OK] /api/v1/contacts         -  desde el día 1, aunque no se prevean cambios
[Err] /api/contacts            -  sin versión -> cuando haya un breaking change, no hay cómo migrar
```

**Cuándo crear v2:**
1. Crear `/api/v2/contacts` con el nuevo contrato
2. Mantener `/api/v1/contacts` funcionando (deprecar, no apagar)
3. Comunicar fecha de fin de vida de v1
4. Dar mínimo 6 meses para migrar (APIs públicas)

**Opciones de versionado:**

| Estrategia | Ejemplo | Ventaja |
|---|---|---|
| URI (más común) | `/api/v1/contacts` | Visible, fácil de testear con curl |
| Header | `Api-Version: 1` | URI limpia |
| Query param | `?api-version=1` | Fácil de probar en browser |

---

## Breaking changes vs backwards compatible

```
[OK] NO es breaking change (backwards compatible):
   Agregar campo OPCIONAL en la response
   Agregar campo OPCIONAL en el request body
   Agregar un nuevo endpoint
   Agregar un nuevo valor a un enum

[Err] SÍ es breaking change  -  requiere nueva versión:
   Renombrar un campo (fullName -> name)
   Cambiar el tipo de un campo (string -> int)
   Eliminar un campo
   Cambiar el status code de una respuesta
   Hacer REQUERIDO un campo que era opcional
```

---

## Naming conventions  -  consistencia ante todo

```json
// camelCase en JSON  -  el estándar para APIs (convención JavaScript)
{
  "publicId": "550e8400-e29b-41d4-a716-446655440000",
  "firstName": "María",
  "lastName": "González",
  "createdAt": "2026-05-29T10:30:00Z",
  "isActive": true,
  "conversationsCount": 12
}
```

**kebab-case en URIs:**

```
/api/v1/bot-flows            [OK]
/api/v1/botFlows             [Err]
/api/v1/bot_flows            [Err]
```

**Fechas  -  siempre ISO 8601 UTC:**

```json
"createdAt": "2026-05-29T10:30:00Z"   [OK]
"createdAt": "05/29/2026"             [Err]   -  ambiguo
"createdAt": 1748512200               [Err]   -  difícil de debuggear
```

---

## IDs públicos  -  nunca exponer IDs internos

```json
//  Expone ID secuencial  -  enumeration attack
GET /api/contacts/1
GET /api/contacts/2
GET /api/contacts/999   <- fácil iterar todos los contactos

// [OK] UUID público  -  imposible enumerar
GET /api/contacts/550e8400-e29b-41d4-a716-446655440000
```

El back-template usa:
- `Id` (serial `long`)  -  **solo para joins internos en DB**, nunca sale al exterior
- `PublicId` (UUID `Guid`)  -  **siempre en las responses de la API**

> **Nunca devolver `Id` en las responses.** Solo `PublicId`.

---

<!-- _class: lead -->

# Parte 3
## El Controller Real  -  back-template

---

## Anatomía del Controller

El back-template sigue un patrón preciso: **Controller -> Mediator -> Handler -> Presenter -> ViewModel**.

```csharp
// Modules/Agents/Agents.Presentation/Controllers/AgentsController.cs
using Common.Messaging;
using Common.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Web;
using System.Security.Claims;

namespace Agents.Presentation;

[Route("api/agents")]
[Authorize]
public sealed class AgentsController : BaseApiController
{
    private readonly ILogger<AgentsController>         _logger;
    private readonly ResultViewModel<AgentsController> _viewModel;
```

- `BaseApiController` expone `Mediator` (inyectado vía constructor base)
- `ResultViewModel<T>` se inyecta por DI  -  **scoped por request**
- `[Authorize]` a nivel de clase  -  todos los endpoints requieren JWT

---

## Constructor y CurrentTenantId

```csharp
    public AgentsController(
        IMediator mediator,
        ILogger<AgentsController> logger,
        ResultViewModel<AgentsController> viewModel) : base(mediator)
    {
        _logger    = logger;
        _viewModel = viewModel;
    }

    private long CurrentTenantId =>
        long.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : 0;
```

`CurrentTenantId` lee el claim `tenant_id` del JWT firmado.
El handler recibe el `TenantId` y lo usa para filtrar datos en DB.
Sin `TenantId` válido -> `0` -> ningún registro de otra empresa es visible.

---

## GetAll  -  paginación con page + pageSize

```csharp
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            _ = await Mediator.Send(
                    new GetAgentProfilesRequest(CurrentTenantId, page, pageSize), ct);
            return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetAll Agents");
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException!;
            return StatusCode(500, _viewModel.Fail(inner.Message));
        }
    }
```

El handler aplica `Math.Clamp(page, 1, int.MaxValue)` y `Math.Clamp(pageSize, 1, 100)`.

---

## GetById  -  discriminated union en el resultado

```csharp
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await Mediator.Send(
                             new GetAgentProfileRequest(id, CurrentTenantId), ct);
            if (_viewModel.IsSuccess) return Ok(_viewModel);
            return result is GetAgentProfileNotFoundFailure
                ? NotFound(_viewModel)
                : StatusCode(500, _viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetById Agent");
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException!;
            return StatusCode(500, _viewModel.Fail(inner.Message));
        }
    }
```

`result is GetAgentProfileNotFoundFailure`  -  pattern matching sobre el tipo del resultado.
El Presenter ya pobló `_viewModel` antes de que el Controller lo evalúe.

---

## Update  -  solo Admin puede actualizar

```csharp
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAgentProfileBody body,
        CancellationToken ct = default)
    {
        try
        {
            var result = await Mediator.Send(
                             new UpdateAgentProfileRequest(id, CurrentTenantId, body.FullName), ct);
            if (_viewModel.IsSuccess) return Ok(_viewModel);
            return result is UpdateAgentProfileNotFoundFailure
                ? NotFound(_viewModel)
                : StatusCode(500, _viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Update Agent");
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException!;
            return StatusCode(500, _viewModel.Fail(inner.Message));
        }
    }
```

`[Authorize(Roles = "Admin")]` a nivel de acción  -  sobreescribe el `[Authorize]` de la clase.

---

## Disable (soft delete)  -  solo Admin

```csharp
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await Mediator.Send(
                             new DisableAgentProfileRequest(id, CurrentTenantId), ct);
            if (_viewModel.IsSuccess) return Ok(_viewModel);
            return result is DisableAgentProfileNotFoundFailure
                ? NotFound(_viewModel)
                : StatusCode(500, _viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Disable Agent");
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException!;
            return StatusCode(500, _viewModel.Fail(inner.Message));
        }
    }
}
```

`DELETE` retorna `Ok(_viewModel)` con payload  -  no `204 No Content`.
El back-template nunca borra físicamente: marca `IsActive = false` (soft delete).

---

## RequestBody  -  record inmutable

```csharp
// Agents.Presentation/RequestBodies/UpdateAgentProfileBody.cs
namespace Agents.Presentation.RequestBodies;

public sealed record UpdateAgentProfileBody(string FullName);
```

Un `sealed record` por cada body de request  -  simple e inmutable.
Solo los campos que el cliente puede enviar (nunca `Id` ni `TenantId`).

---

## El flujo completo  -  de request a response

```
HTTP Request
    |

AgentsController.Update(id, body)         <- lee CurrentTenantId del JWT
    |  Mediator.Send(UpdateAgentProfileRequest)

UpdateAgentProfileHandler.Handle()        <- lógica de dominio + DB
    |  publica UpdateAgentProfileResponse (notification)

UpdateAgentProfilePresenter.Handle()      <- patrón Observer
    |  _viewModel.Fail() o _viewModel.OK()

AgentsController <- _viewModel.IsSuccess   <- decide status code
    |

HTTP Response  Ok(_viewModel) / NotFound(_viewModel) / StatusCode(500, _viewModel)
```

El Controller **nunca** accede al dominio directamente  -  solo habla con el Mediator.

---

<!-- _class: lead -->

# Parte 4
## Handler y Responses  -  el patrón completo

---

## Request  -  record sellado

```csharp
// Agents.Application/UseCases/GetAgentProfiles/GetAgentProfilesRequest.cs
using Common.Messaging;
using Agents.Application.UseCases.GetAgentProfiles.Responses;

namespace Agents.Application.UseCases.GetAgentProfiles;

public sealed record GetAgentProfilesRequest(
    long TenantId,
    int  Page,
    int  PageSize) : IRequest<GetAgentProfilesResponse>;
```

`IRequest<TResponse>` del mediador propio del back-template.
El record es inmutable  -  no hay setters, no hay constructor extra.

---

## Response  -  abstract record base

```csharp
// Agents.Application/UseCases/GetAgentProfiles/Responses/GetAgentProfilesResponse.cs
using Common.Messaging;

namespace Agents.Application.UseCases.GetAgentProfiles.Responses;

public abstract record GetAgentProfilesResponse : IResponse;
```

```csharp
// Responses/GetAgentProfilesSuccess.cs
using Common.Results;
using Agents.Contracts.Dtos;

namespace Agents.Application.UseCases.GetAgentProfiles.Responses;

public sealed record GetAgentProfilesSuccess(
    IReadOnlyCollection<AgentProfileDto> Profiles,
    int Total,
    int Page,
    int PageSize) : GetAgentProfilesResponse, ISuccess;
```

`ISuccess` (sin `<T>`)  -  el Presenter accede a los datos por los campos del record.
Para `GetById` usa `ISuccess<AgentProfileDto>` con propiedad `.Data`.

---

## NotFoundFailure  -  tipo específico por use case

```csharp
// Responses/GetAgentProfileNotFoundFailure.cs
using Common.Results;

namespace Agents.Application.UseCases.GetAgentProfile.Responses;

public sealed record GetAgentProfileNotFoundFailure(string Message)
    : GetAgentProfileResponse, INotFoundFailure;
```

`INotFoundFailure` viene de `Common.Results`  -  el Controller hace pattern matching sobre él.

```csharp
// En el Controller:
return result is GetAgentProfileNotFoundFailure
    ? NotFound(_viewModel)
    : StatusCode(500, _viewModel);
```

Un tipo de failure por use case  -  no un string genérico. Permite discriminar entre 404 y 500.

---

## Handler  -  lógica de aplicación

```csharp
// Agents.Application/UseCases/GetAgentProfiles/GetAgentProfilesHandler.cs
using Common.Messaging;
using Agents.Application.UseCases.GetAgentProfiles.Responses;
using Agents.Contracts.Dtos;
using Agents.Domain.Repositories;

namespace Agents.Application.UseCases.GetAgentProfiles;

public sealed class GetAgentProfilesHandler
    : IRequestHandler<GetAgentProfilesRequest, GetAgentProfilesResponse>
{
    private readonly IAgentProfileRepository _profiles;

    public GetAgentProfilesHandler(IAgentProfileRepository profiles)
        => _profiles = profiles;

    public async Task<GetAgentProfilesResponse> Handle(
        GetAgentProfilesRequest request, CancellationToken cancellationToken)
    {
        var page     = Math.Clamp(request.Page, 1, int.MaxValue);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var listTask  = _profiles.GetPagedAsync(request.TenantId, page, pageSize, cancellationToken);
        var totalTask = _profiles.GetCountAsync(request.TenantId, cancellationToken);
        await Task.WhenAll(listTask, totalTask);

        var dtos = listTask.Result.Select(p =>
            new AgentProfileDto(p.PublicId, p.FullName, p.IsActive,
                                p.CreatedAtUtc, p.UpdatedAtUtc)).ToArray();

        return new GetAgentProfilesSuccess(dtos, totalTask.Result, page, pageSize);
    }
}
```

---

## Presenter  -  pobla el ViewModel

```csharp
// Agents.Presentation/Presenters/GetAgentProfilesPresenter.cs
using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Agents.Application.UseCases.GetAgentProfiles.Responses;
using Agents.Presentation;

namespace Agents.Presentation.Presenters;

public sealed class GetAgentProfilesPresenter
    : INotificationHandler<GetAgentProfilesResponse>
{
    private readonly ResultViewModel<AgentsController> _viewModel;

    public GetAgentProfilesPresenter(ResultViewModel<AgentsController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(GetAgentProfilesResponse notification, CancellationToken cancellationToken)
    {
        if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is GetAgentProfilesSuccess success)
            _viewModel.OK(success);

        return Task.CompletedTask;
    }
}
```

El Presenter es un `INotificationHandler`  -  se ejecuta automáticamente después del Handler.

---

## ResultViewModel  -  la forma de la response

```csharp
// Common/Common/ViewModels/ResultViewModel.cs
namespace Common.ViewModels;

public class ResultViewModel<T>
{
    public object?   Data         { get; private set; }
    public bool      IsSuccess    { get; private set; }
    public string?   Message      { get; private set; }
    public DateTime  UtcTimeStamp { get; private set; }

    public ResultViewModel<T> OK(object data, int statusCode = 200)  { ... }
    public ResultViewModel<T> Fail(string message, int statusCode = 400) { ... }
    public void Set<TData>(ISuccess<TData> success, ...) { ... }
    public void Set(IFailure failure) { ... }
}
```

**Response JSON en success:**

```json
{
  "isSuccess": true,
  "data": { "publicId": "...", "fullName": "...", "isActive": true },
  "message": null,
  "utcTimeStamp": "2026-05-29T10:30:00Z"
}
```

---

<!-- _class: lead -->

# Parte 5
## Paginación  -  Offset vs Cursor

---

## ¿Por qué paginar?

Sin paginación, `GET /api/messages` con 1,000,000 de registros:

- Consume memoria desproporcionada en el servidor
- Hace esperar al usuario segundos antes de ver algo
- Puede crashear el proceso por OOM (Out of Memory)

---

## Offset pagination  -  la más común

```
GET /api/conversations?page=1&pageSize=20
GET /api/conversations?page=2&pageSize=20
```

```json
{
  "isSuccess": true,
  "data": {
    "profiles": [ { "publicId": "...", "status": "open" }, "..." ],
    "total": 1523,
    "page": 2,
    "pageSize": 20
  }
}
```

**SQL:**

```sql
SELECT PublicId, Status, CreatedAtUtc
FROM   Conversations
WHERE  TenantId = @tenantId
  AND  DeletedAt IS NULL
ORDER BY CreatedAtUtc DESC
LIMIT  20 OFFSET 20;   -- offset = (page - 1) * pageSize
```

---

## Problemas de offset pagination

```
Problema 1: Registros duplicados entre páginas
   Se inserta una conversación nueva entre GET page=1 y GET page=2.
   El registro que era el primero de p2 se "empuja" a la posición siguiente.
   -> El usuario ve el mismo elemento repetido.

Problema 2: Rendimiento en OFFSET grande
   OFFSET 100000 -> PostgreSQL lee y descarta las primeras 100,000 filas.
   En tablas de millones de mensajes: segundos de latencia.
```

---

## Cursor pagination  -  basada en un punto de anclaje

```
GET /api/messages?pageSize=20
-> items + nextCursor: "2026-05-29T10:00:00Z_abc123"

GET /api/messages?pageSize=20&cursor=2026-05-29T10:00:00Z_abc123
-> siguiente página + nuevo cursor
```

```json
{
  "isSuccess": true,
  "data": {
    "items": [ { "publicId": "...", "body": "Hola" }, "..." ],
    "nextCursor": "MjAyNi0wNS0yOVQwOTowMDowMFp8NTUwZTg0MDA=",
    "hasNextPage": true
  }
}
```

**SQL  -  keyset pagination:**

```sql
SELECT PublicId, Body, CreatedAtUtc
FROM   Messages
WHERE  ConversationId = @convId
  AND  (CreatedAtUtc, PublicId) < (@lastCreatedAt, @lastPublicId)
ORDER BY CreatedAtUtc DESC, PublicId DESC
LIMIT  20;
```

---

## Comparación de estrategias

| | Offset | Cursor |
|---|---|---|
| Implementación | Simple | Más compleja |
| Rendimiento en páginas profundas | Degrada | Constante |
| Saltar a página N | [OK] (`?page=50`) | [Err] (no hay "página 50") |
| Datos consistentes al paginar | [Err] (duplicados con inserts) | [OK] |
| Ideal para | Dashboards, búsqueda admin | Feeds, scroll infinito, chat |

**Regla práctica para el sistema de mensajería:**
- Bandeja de conversaciones (con filtros, búsqueda) -> **offset**
- Historial de mensajes de una conversación -> **cursor** (scroll infinito)

---

## Filtros y ordenamiento en query params

```
GET /api/conversations?page=1&pageSize=20&status=open&assignedTo=agent-uuid
GET /api/contacts?page=1&pageSize=50&search=maría&isActive=true
GET /api/contacts?page=1&pageSize=20&sortBy=createdAt&sortDir=desc
```

**Reglas:**
- Filtros como query params opcionales  -  sin filtro devuelve todos
- `sortBy` acepta nombre de campo en camelCase
- `sortDir` = `asc` | `desc`
- `pageSize` máximo 100  -  el handler aplica `Math.Clamp(pageSize, 1, 100)`

---

<!-- _class: lead -->

# Parte 6
## Errores y Problem Details

---

## El principio: errores para máquinas, mensajes para humanos

```json
//  Error para humanos  -  la máquina no puede reaccionar de forma diferente
{ "error": "Something went wrong. Please try again." }

// [OK] RFC 7807 Problem Details  -  máquina + humano
{
  "type":     "https://httpstatuses.io/409",
  "title":    "Conflict",
  "status":   409,
  "detail":   "El email ya está registrado. Por favor usa otro.",
  "instance": "/api/v1/contacts"
}
```

`type`  -  URI que identifica el tipo de error de forma única (campo clave del RFC 7807)
`detail`  -  mensaje en lenguaje natural para mostrar al usuario final
`traceId`  -  extensión que permite correlacionar con los logs internos

---

## Status codes  -  4xx vs 5xx

**4xx  -  el cliente debe cambiar algo** (reintentar la misma request no ayuda):

| Code | Cuándo usarlo | Ejemplo en CRM |
|---|---|---|
| `400` | Body malformado, tipos incorrectos | JSON inválido en POST /messages |
| `401` | Token ausente o expirado | Sin `Authorization` header |
| `403` | Token válido, sin permiso | Agente intenta borrar workspace |
| `404` | El recurso no existe | Conversación con ese UUID no existe |
| `409` | Conflicto  -  email duplicado | Contacto con ese email ya existe |
| `422` | Reglas de negocio violadas | Canal sin webhook configurado |
| `429` | Rate limit excedido | 100 req/min superados |

**5xx  -  el servidor falló** (el cliente puede reintentar con backoff):

| Code | Cuándo |
|---|---|
| `500` | Error interno  -  nunca exponer stack trace |
| `503` | App reiniciando o mantenimiento |

---

## 401 vs 403  -  diferencia exacta

```
401 Unauthorized
   "No sé quién eres  -  demuestra tu identidad"
   -> Token ausente, expirado o con firma inválida
   -> Cliente: renovar el token y reintentar

403 Forbidden
   "Sé quién eres, pero no tienes permiso"
   -> Token válido pero rol insuficiente
   -> Cliente: no reintentar  -  cambiar el flujo de la app
```

**Ejemplo en CRM:**

```
POST /api/v1/conversations/abc-123/assign
-> 401   si el header Authorization no está presente
-> 403   si el agente autenticado tiene rol "viewer" (solo lectura)
-> 200   si el agente tiene rol "agent" o "admin"
```

---

## Errores de validación  -  detalle por campo

```json
//  Poco útil
{ "status": 400, "detail": "Validation failed" }

// [OK] Útil  -  el cliente sabe exactamente qué campos y por qué
{
  "type":   "https://httpstatuses.io/400",
  "title":  "Validation Error",
  "status": 400,
  "errors": {
    "email":   ["El email no tiene un formato válido."],
    "phone":   ["El teléfono debe incluir el código de país."],
    "channel": ["El canal especificado no existe en este workspace."]
  },
  "traceId": "00-abc123def456..."
}
```

El `traceId` permite al equipo correlacionar el error con los logs internos sin exponer detalles técnicos al cliente.

---

## UseCoreProblemDetails  -  el middleware del back-template

```csharp
// Common/Common/Web/ApplicationBuilderEx.cs
public static class ApplicationBuilderEx
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();

    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
        => app.UseMiddleware<TenantResolutionMiddleware>();

    public static IApplicationBuilder UseCoreProblemDetails(this IApplicationBuilder app)
        => app.UseMiddleware<ProblemDetailsMiddleware>();
}
```

**Registro en Program.cs:**

```csharp
// Host.Api/Program.cs
app.UseCoreProblemDetails();   // captura BusinessRuleException -> 400, Exception -> 500
app.UseCorrelationId();        // añade X-Correlation-Id a cada request/response
app.UseTenantResolution();     // resuelve tenant_id del subdominio o header
```

---

## ProblemDetailsMiddleware  -  cómo funciona

```csharp
// Captura BusinessRuleException del dominio -> 400
catch (BusinessRuleException ex)
{
    _logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
    await WriteProblemDetails(context, 400, "Business rule violation", ex.Message);
}
// Captura cualquier Exception no controlada -> 500
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled exception");
    await WriteProblemDetails(context, 500, "Unhandled error", "An unexpected error occurred");
}
```

**Response 500 en producción  -  nunca expone detalles internos:**

```json
{
  "status":   500,
  "title":    "Unhandled error",
  "detail":   "An unexpected error occurred",
  "instance": "/api/v1/conversations",
  "traceId":  "00-abc123def456ghi789..."
}
```

---

## Error codes  -  distinguir errores del mismo status

```json
// Ambos son 409 Conflict, pero el cliente reacciona diferente:
{ "status": 409, "code": "EMAIL_ALREADY_EXISTS",         "detail": "..." }
{ "status": 409, "code": "CONVERSATION_ALREADY_ASSIGNED","detail": "..." }
```

```csharp
// Definir los códigos como constantes
public static class ErrorCodes
{
    public const string EmailAlreadyExists          = "EMAIL_ALREADY_EXISTS";
    public const string ConversationAlreadyAssigned = "CONVERSATION_ALREADY_ASSIGNED";
    public const string ContactNotFound             = "CONTACT_NOT_FOUND";
    public const string ChannelNotConfigured        = "CHANNEL_NOT_CONFIGURED";
}
```

---

<!-- _class: lead -->

# Parte 7
## OpenAPI / Swagger

---

## Documentar los contratos con OpenAPI

Una API sin documentación **no existe** para quienes no la escribieron.

```csharp
// Documentar qué puede retornar cada endpoint
[HttpPost]
[ProducesResponseType(typeof(ResultViewModel<ContactDto>),     StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails),        StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(typeof(ProblemDetails),                  StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails),                  StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> Create(
    [FromBody] CreateContactBody body,
    CancellationToken ct)
{ ... }
```

Con `[ProducesResponseType]` el cliente (y Swagger) sabe exactamente qué esperar de cada endpoint, incluyendo los errores posibles.

---

## SwaggerExtensions  -  configuración real del back-template

```csharp
// Host.Api/Extensions/SwaggerExtensions.cs
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Host.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(ConfigureSwagger);
        return services;
    }

    private static void ConfigureSwagger(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title   = "Back Template API",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Ingresa el token JWT sin el prefijo Bearer."
        });

        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer"), [] }
        });
    }
}
```

---

## XML Comments  -  descripción de endpoints

```csharp
/// <summary>Obtiene todos los mensajes de una conversación.</summary>
/// <param name="conversationId">UUID público de la conversación</param>
/// <param name="pageSize">Tamaño de página (1-100, default 20)</param>
/// <param name="cursor">Cursor opaco de la respuesta anterior (base64)</param>
/// <response code="200">Lista de mensajes con cursor para página siguiente</response>
/// <response code="401">Token ausente o expirado</response>
/// <response code="403">Sin permiso para ver esta conversación</response>
/// <response code="404">Conversación no encontrada</response>
[HttpGet("{conversationId:guid}/messages")]
[ProducesResponseType(typeof(ResultViewModel<MessagesPageDto>), 200)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
public async Task<IActionResult> GetMessages(
    Guid conversationId,
    [FromQuery] int pageSize   = 20,
    [FromQuery] string? cursor = null,
    CancellationToken ct       = default) { ... }
```

---

<!-- _class: lead -->

# Parte 8
## Buenas Prácticas

---

## Consistencia  -  la regla número uno

```
 Inconsistente  -  el cliente tiene que leer el código fuente para entender cada endpoint:
GET /api/conversations   -> { "data": [...], "total": 100 }
GET /api/contacts        -> { "contacts": [...], "count": 50 }
GET /api/messages        -> [ {...}, {...} ]       <- array directo
POST /api/contacts/create -> { "success": true }

[OK] Consistente  -  el cliente predice la forma de cada response:
GET /api/v1/conversations -> { "isSuccess": true, "data": { ... }, "utcTimeStamp": "..." }
GET /api/v1/contacts      -> { "isSuccess": true, "data": { ... }, "utcTimeStamp": "..." }
POST /api/v1/contacts     -> { "isSuccess": true, "data": { ... }, "utcTimeStamp": "..." }
```

Una API inconsistente es **más difícil de usar** que una API que sigue convenciones no ideales pero consistentes.

---

## HATEOAS  -  nivel máximo de REST

Hypermedia As The Engine Of Application State: la response incluye links a las acciones disponibles.

```json
{
  "publicId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "open",
  "_links": {
    "self":     { "href": "/api/v1/conversations/550e8400", "method": "GET" },
    "assign":   { "href": "/api/v1/conversations/550e8400/assign",  "method": "POST" },
    "close":    { "href": "/api/v1/conversations/550e8400/close",   "method": "POST" },
    "messages": { "href": "/api/v1/conversations/550e8400/messages","method": "GET" }
  }
}
```

En la práctica: pocas APIs implementan HATEOAS completo. El tradeoff de complejidad raramente vale para APIs internas o con un solo cliente frontend.

**El back-template está en Richardson Level 2**  -  suficiente para el CRM.

---

## Idempotency Keys  -  POST seguros para reintentar

Para operaciones con side-effects (envío de mensajes, webhooks salientes):

```http
POST /api/v1/messages
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

{ "conversationId": "abc-123", "body": "Hola, ¿en qué te puedo ayudar?" }
```

Si el cliente no recibió la respuesta y reintenta con la misma `Idempotency-Key`  -  el mensaje **no se envía dos veces**.

```
Si el servidor ya procesó esa key -> retorna el mismo resultado sin ejecutar de nuevo
Si la key es nueva              -> procesa normalmente y guarda el resultado por 24h
```

---

## Rate Limiting  -  comunicarlo claramente

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1748512200
```

```json
{
  "type":   "https://httpstatuses.io/429",
  "title":  "Too Many Requests",
  "status": 429,
  "detail": "Límite de 100 requests por minuto excedido. Reintenta en 60 segundos."
}
```

El cliente legítimo lee `Retry-After` y espera exactamente ese tiempo  -  sin backoff exponencial innecesario.

---

<!-- _class: lead -->

# Parte 9
## Errores Comunes en Diseño de API

---

## Anti-patrón 1: Verbos en las URIs

```
 Verbos en URIs  -  no es REST, es RPC sobre HTTP
GET  /api/getContacts
POST /api/createContact
POST /api/deleteContact/123
GET  /api/searchMessages?q=hola
GET  /api/getConversationsByAgent/abc-123

[OK] Sustantivos + verbo HTTP
GET    /api/v1/contacts
POST   /api/v1/contacts
DELETE /api/v1/contacts/abc-123
GET    /api/v1/messages?search=hola
GET    /api/v1/conversations?assignedTo=abc-123
```

---

## Anti-patrón 2: Status codes incorrectos

```
 Status codes incorrectos  -  el cliente no puede reaccionar bien
POST /api/v1/contacts  -> 200 OK   aunque hubo error de validación
GET  /api/v1/contacts/no-existe -> 200 OK  con body { "error": "not found" }
POST /api/v1/contacts  -> 200 OK   con body { "success": false }
POST /api/v1/contacts  -> 500      para un error de validación de negocio

[OK] Status codes correctos
POST /api/v1/contacts  -> 201 Created         al crear exitosamente
POST /api/v1/contacts  -> 422 Unprocessable   si hay error de validación
GET  /api/v1/contacts/no-existe -> 404 Not Found
DELETE /api/v1/contacts/abc-123 -> 204 No Content
```

El status code es la **primera señal** que el cliente recibe  -  si es incorrecto, todo lo demás falla.

---

## Anti-patrón 3: Anidamiento excesivo

```
 Anidamiento profundo  -  URIs ilegibles
GET /api/workspaces/123/agents/456/conversations/789/messages/012/attachments

[OK] Máximo 2 niveles de anidamiento
GET /api/v1/conversations/789/messages      <- mensajes de una conversación
GET /api/v1/messages/012/attachments        <- attachments de un mensaje
```

Si necesitas más contexto en el filtro, usa **query params**:

```
GET /api/v1/messages?conversationId=789&agentId=456
```

---

## Anti-patrón 4: PUT cuando se necesita PATCH

```json
// Frontend tiene el perfil de un contacto:
{ "firstName": "María", "lastName": "González", "email": "m@example.com", "phone": "..." }

// El usuario solo cambia el teléfono.

//  Si el endpoint es PATCH pero el frontend envía solo el campo cambiado:
PUT /api/v1/contacts/abc-123
{ "phone": "+52 55 9999 0000" }
// -> firstName, lastName, email quedan en null o se borran en el servidor

// [OK] Con PUT el frontend envía el objeto completo:
PUT /api/v1/contacts/abc-123
{ "firstName": "María", "lastName": "González",
  "email": "m@example.com", "phone": "+52 55 9999 0000" }
// -> Solo cambia phone, el resto permanece igual
```

---

## Anti-patrón 5: Exponer el ViewModel sin el Presenter

```csharp
//  El Controller accede a la BD directamente  -  viola Clean Architecture
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id)
{
    var contact = await _dbContext.Contacts.FindAsync(id);
    return contact is null ? NotFound() : Ok(contact);  // expone la entidad de dominio
}

// [OK] El Controller solo habla con el Mediator
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
{
    var result = await Mediator.Send(new GetContactRequest(id, CurrentTenantId), ct);
    if (_viewModel.IsSuccess) return Ok(_viewModel);
    return result is GetContactNotFoundFailure
        ? NotFound(_viewModel) : StatusCode(500, _viewModel);
}
```

---

## Checklist  -  antes de implementar un endpoint

```
Diseño:
[ ] ¿El verbo HTTP es correcto? (GET leer, POST crear, PUT reemplazar, DELETE borrar)
[ ] ¿La URI sigue las convenciones? (kebab-case, plural, sin verbos, máx 2 niveles)
[ ] ¿Qué status codes puede retornar? (200, 201, 404, 409, 422...)
[ ] ¿Qué errores de negocio puede retornar? (failure types documentados)
[ ] ¿El endpoint requiere autenticación? ¿Qué roles? ([Authorize(Roles = "Admin")])
[ ] ¿Hay paginación si puede retornar muchos resultados?

Antes del PR:
[ ] ¿El response body usa ResultViewModel<T> (isSuccess + data + utcTimeStamp)?
[ ] ¿Se devuelve PublicId (UUID) en lugar de Id (serial)?
[ ] ¿Las fechas son ISO 8601 UTC (CreatedAtUtc)?
[ ] ¿Hay [ProducesResponseType] en el controller?
[ ] ¿dotnet build -> 0 errores, 0 warnings?
```

---

<!-- _class: lead -->

# Resumen

---

## Una API bien diseñada es...

| Principio | Regla concreta |
|---|---|
| **Predecible** | `ResultViewModel<T>` en todos los endpoints  -  misma forma siempre |
| **Semántica correcta** | Verbos HTTP que dicen lo que hacen |
| **Segura** | `PublicId` UUID nunca `Id` serial; `ProblemDetailsMiddleware` sin stack trace |
| **Robusta** | Status codes correctos; failure types específicos por use case |
| **Escalable** | Paginación siempre; cursor para historial de mensajes |
| **Evolucionable** | Versionado desde v1; breaking changes en v2 |
| **Documentada** | `AddSwaggerWithJwt` + `[ProducesResponseType]` = contrato claro |

---

## Recursos del proyecto

- `back-template`  -  `UsersController.cs`, `GetUserProfilesHandler.cs`, `SwaggerExtensions.cs`
- `back-template`  -  `ApplicationBuilderEx.UseCoreProblemDetails()`, `CorrelationIdMiddleware`
- `dev-notes/03-arquitectura/api-design/`  -  5 archivos de referencia detallada
- `dev-notes/04-backend/15-openapi.md`  -  configuración de Swagger/Scalar en .NET 10
- `dev-notes/04-backend/12-rate-limiting.md`  -  rate limiting con .NET built-in
- RFC 7807  -  https://datatracker.ietf.org/doc/html/rfc7807

> **Próximo paso:** aplicar estas convenciones al diseño de endpoints del sistema de mensajería  -
> `contacts`, `conversations`, `messages`, `channels`, `bot-flows`.
