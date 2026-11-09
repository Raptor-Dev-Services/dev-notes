# Tour del Back-Template - S03

**Dev Notes - **

> Modalidad: navegación guiada - no hay código que escribir
> Ruta del template: `D:\Personal\Raptor Dev Services\Templates\back-template\back-template\`
> Abre el proyecto en VS Code o Rider ahora.

Cada parada tiene: **qué abrir -> qué buscar -> pregunta de comprensión.**
Sigue el orden. No saltes paradas.

---

## Parada 1 - Estructura de carpetas - 10 min

Abre el proyecto raíz en el explorador de archivos o en el árbol de VS Code/Rider.

**Identifica las cinco carpetas de primer nivel:**

| Carpeta | Responsabilidad |
|---------|----------------|
| `Modules/` | Módulos de negocio del sistema |
| `Shared/` | Funcionalidad transversal compartida entre módulos |
| `Common/` | Librería base (resultados, mensajería, multi-tenancy) |
| `Host.Api/` | Punto de entrada - DI root - pipeline HTTP |
| `Tests/` | Pruebas de arquitectura (NetArchTest) |

**Checklist Parada 1:**
- [ ] ¿Cuántos módulos de negocio hay dentro de `Modules/`?
- [ ] ¿Qué módulo hay en `Shared/` y por qué NO está dentro de `Modules/`?
- [ ] ¿Qué diferencia hay entre `Shared/` y `Common/`?

> `Shared/` tiene funcionalidad que CRUZA módulos (Authentication).
> `Common/` es la librería base que podría distribuirse como NuGet.

---

## Parada 2 - Un módulo completo: Users - 15 min

Navega a `Modules/Users/` y lista los sub-proyectos que encuentres.

**Los 6 sub-proyectos y su responsabilidad:**

| Sub-proyecto | Responsabilidad | Depende de |
|---|---|---|
| `Users.Domain` | Entidades + interfaces de repositorio | Nada |
| `Users.Application` | Casos de uso (Handlers) | Domain, Common |
| `Users.Contracts` | DTOs + eventos de integración (API pública del módulo) | Nada |
| `Users.Infrastructure` | Implementaciones EF Core / Dapper | Domain |
| `Users.Presentation` | Controllers + Presenters + RequestBodies | Application, Contracts, Common |
| `Users.Tests` | Pruebas unitarias del módulo | Application, Domain |

**Checklist Parada 2:**
- [ ] ¿Dónde vive la entidad `UserProfile`?
- [ ] ¿Dónde vive la interfaz `IUserProfileRepository`?
- [ ] ¿Dónde vive la implementación del repositorio?
- [ ] ¿Dónde vive `UsersController`?
- [ ] ¿Por qué `Users.Application` NO referencia `Users.Infrastructure`?

---

## Parada 3 - Flujo de una request: GetUserProfile - 20 min

Sigue el flujo de punta a punta. Abre cada archivo en el orden indicado.

**Paso 1 - El Controller recibe la petición HTTP**

Abre:
```
Modules/Users/Users.Presentation/Controllers/UsersController.cs
```

Busca el método `GetById`. Observa:
- ¿Qué tipo de objeto envía al Mediator?
- ¿Qué parámetros lleva ese objeto?
- ¿De dónde obtiene `CurrentTenantId`?

> El Controller nunca llama al repositorio directamente.
> Solo construye la Request y delega al Mediator.

---

**Paso 2 - La Request: qué datos viajan al Handler**

Busca en `Users.Application/UseCases/GetUserProfile/`:
```
GetUserProfileRequest.cs
```

Observa:
- ¿Es `class` o `record`? ¿Por qué?
- ¿Qué interfaz implementa?
- ¿Por qué lleva `TenantId` además de `PublicId`?

---

**Paso 3 - El Handler: lógica de aplicación**

Abre:
```
Modules/Users/Users.Application/UseCases/GetUserProfile/GetUserProfileHandler.cs
```

```csharp
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

**Pregunta:** ¿El Handler importa `Users.Infrastructure`? Revisa los `using` al principio del archivo.

---

**Paso 4 - El contrato del repositorio: vive en Domain**

Abre:
```
Modules/Users/Users.Domain/Repositories/IUserProfileRepository.cs
```

```csharp
public interface IUserProfileRepository
{
    Task<UserProfile?> GetByPublicIdAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserProfile>> GetPagedAsync(
        long tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task InsertAsync(
        Guid publicId, long tenantId, string fullName, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(
        Guid publicId, long tenantId, string fullName, CancellationToken cancellationToken = default);
    Task<bool> DisableAsync(
        Guid publicId, long tenantId, CancellationToken cancellationToken = default);
}
```

**Pregunta:** Todos los métodos reciben `tenantId`. ¿Por qué no hay un método `GetAllAsync()` sin filtro de tenant?

---

**Paso 5 - Las Responses: jerarquía de tipos**

Abre los tres archivos de `Responses/`:

```
Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileResponse.cs
Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileSuccess.cs
Users.Application/UseCases/GetUserProfile/Responses/GetUserProfileNotFoundFailure.cs
```

**GetUserProfileResponse.cs** - el tipo base:
```csharp
public abstract record GetUserProfileResponse : IResponse;
```

**GetUserProfileSuccess.cs** - éxito con dato:
```csharp
public sealed record GetUserProfileSuccess(UserProfileDto Data)
    : GetUserProfileResponse, ISuccess<UserProfileDto>;
```

**GetUserProfileNotFoundFailure.cs** - fallo específico:
```csharp
public sealed record GetUserProfileNotFoundFailure(string Message)
    : GetUserProfileResponse, INotFoundFailure;
```

**Pregunta:** ¿Por qué el tipo base es `abstract record` y no `interface`?

---

**Paso 6 - El Presenter: pattern matching sobre la respuesta**

Abre:
```
Modules/Users/Users.Presentation/Presenters/GetUserProfilePresenter.cs
```

```csharp
public sealed class GetUserProfilePresenter
    : INotificationHandler<GetUserProfileResponse>
{
    private readonly ResultViewModel<UsersController> _viewModel;

    public GetUserProfilePresenter(ResultViewModel<UsersController> viewModel)
        => _viewModel = viewModel;

    public Task Handle(GetUserProfileResponse notification, CancellationToken cancellationToken)
    {
        if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<UserProfileDto> success)
            _viewModel.Set(success);

        return Task.CompletedTask;
    }
}
```

**Pregunta:** ¿Por qué `IFailure` va primero y `ISuccess<T>` va segundo en el `if/else`?

> Respuesta: `IFailure` captura toda la jerarquía de fallos
> (`INotFoundFailure`, `IValidationFailure`, `IConflictFailure`).
> Si pusiera `ISuccess<T>` primero, un `INotFoundFailure` podría escapar
> sin ser manejado si alguien cambiara la jerarquía.

---

## Parada 4 - Common/Common/Results - 10 min

Abre los dos archivos de resultados de la librería compartida.

**IFailure.cs** - jerarquía de fallos:
```
Common/Common/Results/IFailure.cs
```

```csharp
public interface IFailure          { string Message { get; } }
public interface IValidationFailure : IFailure { }
public interface INotFoundFailure   : IFailure { }
public interface IConflictFailure   : IFailure { }
```

**ISuccess.cs** - jerarquía de éxitos:
```
Common/Common/Results/ISuccess.cs
```

```csharp
public interface ISuccess { }
public interface ISuccess<T> : ISuccess { T Data { get; } }
```

**Checklist Parada 4:**
- [ ] ¿Cuántos tipos de fallo distintos hay?
- [ ] ¿Qué diferencia hay entre `ISuccess` e `ISuccess<T>`?
- [ ] ¿El Controller usa `IFailure` o el tipo concreto para decidir el status code HTTP?
  (pista: vuelve a `UsersController.GetById` y mira el `return`)

---

## Parada 5 - Host.Api/Program.cs - 10 min

Abre:
```
Host.Api/Program.cs
```

Observa el patrón de registro por módulo:

```csharp
// Mediator — recibe los assemblies de Application de cada módulo
builder.Services.AddMediator(
    typeof(Tenancy.Application.ServiceCollectionEx).Assembly,
    typeof(Users.Application.ServiceCollectionEx).Assembly,
    typeof(Authentication.Application.ServiceCollectionEx).Assembly
);

// Cada módulo se registra en 3 líneas
builder.Services.AddUsersApplicationServices();
builder.Services.AddUsersInfrastructureServices();
builder.Services.AddUsersWebApiServices();
```

**Checklist Parada 5:**
- [ ] ¿Por qué `AddMediator` recibe múltiples assemblies?
- [ ] ¿Qué ocurre si agregas un módulo `Contacts` nuevo?
  ¿Cuántas líneas cambian en `Program.cs`?
- [ ] Busca `TenantClaimsMiddleware` - ¿en qué posición del pipeline está?
  ¿Antes o después de `UseAuthentication`?

> El middleware de tenant va después de `UseAuthentication`
> porque necesita leer el JWT ya validado para extraer el `tenant_id`.

---

## Parada 6 - DIP en acción - 5 min

Esta parada es solo de observación - no abras archivos nuevos.

**El flujo de dependencias:**

```
Users.Application
    importa: IUserProfileRepository  ← está en Users.Domain
    NO importa: Users.Infrastructure

Users.Infrastructure
    implementa: IUserProfileRepository
    NO es conocida por: Users.Application

Host.Api/Program.cs
    conecta los dos:
    builder.Services.AddUsersInfrastructureServices()
    → registra IUserProfileRepository → UserProfileRepository (Scoped)
```

**Confirmación:**
- Abre `GetUserProfileHandler.cs` -> revisa los `using` al inicio
- Confirma que NO hay ningún `using` que mencione `Users.Infrastructure`

**Pregunta final:** Si mañana reemplazas EF Core por Dapper en `Users.Infrastructure`, ¿qué archivos fuera de `Users.Infrastructure/` tendrías que modificar?

> Respuesta: ninguno. Solo cambia la implementación. El contrato (`IUserProfileRepository`) no se toca.

---

## Resumen del tour - mapa mental

| Concepto | Dónde lo viste |
|---|---|
| `sealed class` en Handlers | `GetUserProfileHandler` |
| `abstract record` como base de Response | `GetUserProfileResponse` |
| `ISuccess<T>` / `IFailure` | `Common/Common/Results/` |
| DIP: interfaz en Domain, implementación en Infrastructure | `IUserProfileRepository` ↔ repositorio en Infrastructure |
| Pattern matching en Presenter | `GetUserProfilePresenter` - `is IFailure` / `is ISuccess<T>` |
| Multi-tenancy explícita | `tenantId` en cada método del repositorio |
| Registro por módulo | `Program.cs` - 3 líneas por módulo |

> El homework replica este patrón completo para el módulo `Contacts`.
