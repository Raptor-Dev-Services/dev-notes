# 27 — RBAC: Control de Acceso Basado en Roles

RBAC (Role-Based Access Control) es el modelo de autorización donde los permisos se asignan a roles, y los roles se asignan a usuarios. En un SaaS multi-tenant, cada usuario tiene un rol dentro de su tenant. El rol determina qué endpoints puede llamar y qué datos puede ver o modificar.

---

## Modelo conceptual

```
Tenant A
├── Usuario Ana     → rol: Admin      → puede todo dentro del tenant
├── Usuario Luis    → rol: Manager    → puede leer + modificar, no puede borrar ni gestionar usuarios
└── Usuario Carmen  → rol: Viewer     → solo lectura

Tenant B
├── Usuario Jorge   → rol: Admin
└── Usuario Marta   → rol: Operator
```

Los roles son **por tenant** — Ana es Admin de Tenant A, no de todo el sistema. Un rol en el JWT solo tiene significado dentro del `tenant_id` que lo emitió.

---

## Modelo de roles — flat vs jerárquico

### Flat (roles independientes, sin herencia)

```
Admin    — gestión total del tenant
Manager  — operaciones de negocio, no puede gestionar usuarios ni configuración
Operator — operaciones básicas (crear, leer registros operativos)
Viewer   — solo lectura en todo
```

Cada endpoint tiene un conjunto de roles permitidos. No hay herencia — si un endpoint requiere `Manager`, un `Viewer` no puede aunque sea "menor".

### Jerárquico (cada rol incluye permisos del rol inferior)

```
Admin > Manager > Operator > Viewer
```

Más intuitivo para el usuario ("si puedes hacer X, también puedes hacer Y") pero más difícil de implementar en JWT. El enfoque flat es el recomendado para empezar.

---

## Roles en el JWT

El rol se incluye como claim en el token:

```csharp
// Authentication.Infrastructure/Jwt/JwtTokenService.cs
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, credential.PublicId.ToString()),
    new Claim("email",     credential.Email),
    new Claim(ClaimTypes.Role, credential.Role),   // "Admin" | "Manager" | "Operator" | "Viewer"
    new Claim("tenant_id", credential.TenantId.ToString()),
};
```

Usar `ClaimTypes.Role` (que es `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`) en lugar del string `"role"` — ASP.NET Core lo reconoce automáticamente para `[Authorize(Roles = "Admin")]`.

---

## Autorización en controllers

### Por endpoint

```csharp
[Route("api/users")]
[Authorize]   // cualquier usuario autenticado
public sealed class UsersController : BaseApiController
{
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]   // solo Admin o Manager
    public async Task<IActionResult> GetAll(CancellationToken ct) { ... }

    [HttpGet("{id:guid}")]
    // Sin rol específico → cualquier usuario autenticado del tenant puede ver su perfil
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) { ... }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBody body, CancellationToken ct) { ... }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]   // solo Admin puede eliminar
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct) { ... }
}
```

### Por módulo completo

```csharp
[Route("api/tenant-settings")]
[Authorize(Roles = "Admin")]   // todo el módulo requiere Admin
public sealed class TenantSettingsController : BaseApiController
{
    // todos los endpoints aquí requieren Admin
}
```

---

## Roles en el dominio — enum o string

### String (recomendado para empezar)

```csharp
public static class Roles
{
    public const string Admin    = "Admin";
    public const string Manager  = "Manager";
    public const string Operator = "Operator";
    public const string Viewer   = "Viewer";
}
```

Ventajas: fácil de extender, compatible con JWT sin transformaciones, legible en logs.

### Enum (para sistemas que necesitan comparaciones en código)

```csharp
public enum UserRole
{
    Viewer   = 0,
    Operator = 1,
    Manager  = 2,
    Admin    = 3
}
```

Con jerarquía flat el enum tiene sentido para comparar `if (role >= UserRole.Manager)`.

---

## Leer el rol en el controller

```csharp
private string CurrentRole =>
    User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

// Útil para lógica condicional dentro del mismo endpoint
[HttpGet]
public async Task<IActionResult> GetAll(CancellationToken ct)
{
    // Admin ve todos, Manager ve solo los activos
    var isAdmin = CurrentRole == Roles.Admin;
    _ = await Mediator.Send(new GetUsersRequest(CurrentTenantId, isAdmin), ct);
    // ...
}
```

---

## Roles en el handler — evitar lógica de rol en el handler

El handler no debe saber sobre roles — eso es responsabilidad del controller/presentación. El handler recibe parámetros que ya reflejan la decisión:

```csharp
// ✓ El controller toma la decisión de rol antes de llamar al handler
_ = await Mediator.Send(new GetUsersRequest(tenantId, includeInactive: isAdmin), ct);

// ❌ El handler no debe verificar el rol directamente
// public async Task Handle(request, ct) {
//     if (request.RequesterRole != "Admin") return Forbidden;  ← no hagas esto
// }
```

La excepción: si la misma operación tiene comportamientos muy distintos por rol, considera casos de uso separados.

---

## RBAC a nivel de recurso (Resource-Based Authorization)

A veces no basta con el rol — también importa si el recurso pertenece al usuario que lo solicita. Ejemplo: un `Operator` puede editar sus propios registros, pero no los de otro operador.

```csharp
[HttpPut("orders/{id:guid}")]
[Authorize(Roles = "Admin,Manager,Operator")]
public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] UpdateOrderBody body, CancellationToken ct)
{
    var currentUserPublicId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    var currentRole = User.FindFirstValue(ClaimTypes.Role)!;

    _ = await Mediator.Send(new UpdateOrderRequest(
        id, body.Data,
        CurrentTenantId,
        RequesterPublicId: currentUserPublicId,
        RequesterRole:     currentRole), ct);

    if (_viewModel.IsSuccess) return Ok(_viewModel);
    return result is UpdateOrderForbiddenFailure ? Forbid() : StatusCode(500, _viewModel);
}

// Handler verifica si el operador puede editar este registro específico
public async Task<UpdateOrderResponse> Handle(UpdateOrderRequest request, CancellationToken ct)
{
    var order = await _orders.GetByPublicIdAsync(request.OrderId, request.TenantId, ct);
    if (order is null) return new UpdateOrderNotFoundFailure("Orden no encontrada.");

    // Admin y Manager pueden editar cualquier orden del tenant
    // Operator solo puede editar sus propias órdenes
    if (request.RequesterRole == Roles.Operator &&
        order.CreatedByPublicId != request.RequesterPublicId)
        return new UpdateOrderForbiddenFailure("No tienes permiso para editar esta orden.");

    await _orders.UpdateAsync(request.OrderId, request.Data, request.TenantId, ct);
    return new UpdateOrderSuccess();
}
```

---

## Gestión de roles — cambiar el rol de un usuario

Solo un Admin puede cambiar el rol de otro usuario dentro del mismo tenant:

```csharp
// Users.Application/UseCases/ChangeUserRole/ChangeUserRoleHandler.cs
public async Task<ChangeUserRoleResponse> Handle(ChangeUserRoleRequest request, CancellationToken ct)
{
    // Validar que el rol destino es válido
    if (!IsValidRole(request.NewRole))
        return new ChangeUserRoleValidationFailure($"Rol '{request.NewRole}' no existe.");

    // Un Admin no puede degradar su propio rol (quedaría sin admin)
    if (request.TargetPublicId == request.RequesterPublicId)
        return new ChangeUserRoleValidationFailure("No puedes cambiar tu propio rol.");

    // Verificar que sigue habiendo al menos un Admin en el tenant
    if (request.NewRole != Roles.Admin)
    {
        var adminCount = await _credentials.CountAdminsAsync(request.TenantId, ct);
        var targetIsAdmin = await _credentials.IsAdminAsync(request.TargetPublicId, ct);
        if (targetIsAdmin && adminCount <= 1)
            return new ChangeUserRoleValidationFailure(
                "El tenant debe tener al menos un Admin.");
    }

    await _credentials.UpdateRoleAsync(request.TargetPublicId, request.TenantId, request.NewRole, ct);
    return new ChangeUserRoleSuccess();
}

private static bool IsValidRole(string role) =>
    role is Roles.Admin or Roles.Manager or Roles.Operator or Roles.Viewer;
```

---

## SuperAdmin — rol del operador del SaaS

El SuperAdmin es el rol del equipo que opera el SaaS. NO es un rol del tenant — es un rol del sistema:

```csharp
public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";   // operadores del SaaS
    public const string Support    = "Support";      // soporte técnico con acceso de lectura
}
```

```csharp
[Route("api/admin")]
[Authorize(Roles = SystemRoles.SuperAdmin)]
public sealed class SuperAdminController : BaseApiController
{
    // Endpoints que cruzan todos los tenants — listar tenants, suspender, impersonar
}
```

El JWT de un SuperAdmin **no lleva** `tenant_id` — o lleva `tenant_id = 0`. Los repositorios que accede el SuperAdmin usan `IgnoreQueryFilters()`.

---

## Tabla de permisos por rol (documentar en el proyecto)

| Endpoint | Admin | Manager | Operator | Viewer |
|----------|-------|---------|----------|--------|
| GET /users | ✓ | ✓ | ✗ | ✗ |
| GET /users/{id} | ✓ | ✓ | propio | propio |
| PUT /users/{id} | ✓ | ✓ | propio | ✗ |
| DELETE /users/{id} | ✓ | ✗ | ✗ | ✗ |
| GET /orders | ✓ | ✓ | ✓ | ✓ |
| POST /orders | ✓ | ✓ | ✓ | ✗ |
| DELETE /orders/{id} | ✓ | ✓ | ✗ | ✗ |
| GET /settings | ✓ | ✗ | ✗ | ✗ |
| PUT /settings | ✓ | ✗ | ✗ | ✗ |

Mantener esta tabla actualizada es tan importante como el código — es el contrato de autorización del sistema.

---

## Policies — alternativa a Roles en [Authorize]

Para lógica más compleja (múltiples condiciones), usar policies:

```csharp
// Host.Api/Extensions/AuthorizationExtensions.cs
public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
{
    services.AddAuthorization(options =>
    {
        options.AddPolicy("CanManageUsers", policy =>
            policy.RequireRole(Roles.Admin));

        options.AddPolicy("CanWriteOrders", policy =>
            policy.RequireRole(Roles.Admin, Roles.Manager, Roles.Operator));

        options.AddPolicy("ActiveTenant", policy =>
            policy.RequireClaim("tenant_status", "Active"));
    });
    return services;
}
```

```csharp
[HttpPost("orders")]
[Authorize(Policy = "CanWriteOrders")]
public async Task<IActionResult> Create([FromBody] CreateOrderBody body, CancellationToken ct) { ... }
```

---

## Tests de autorización

```csharp
public sealed class RbacTests
{
    [Theory]
    [InlineData(Roles.Admin,    true)]
    [InlineData(Roles.Manager,  true)]
    [InlineData(Roles.Operator, false)]
    [InlineData(Roles.Viewer,   false)]
    public async Task GetAllUsers_RespectsRoleRestriction(string role, bool shouldSucceed)
    {
        // El test verifica que el endpoint retorna 403 para roles sin permiso
        // Se implementa con WebApplicationFactory + JWT de prueba con el rol correcto
    }
}
```

---

## Antipatrones a evitar

**Hardcodear roles en el handler:**
```csharp
// ❌
if (request.RequesterRole != "Admin") return Forbidden;
// ✓ — la restricción de rol va en [Authorize], no en el handler
```

**Crear demasiados roles:**
Empezar con 3-4 roles planos. Agregar más solo cuando hay un caso de uso real diferenciado, no por anticipar el futuro.

**Roles en el body del request:**
```csharp
// ❌ — el cliente no debe declarar su propio rol
public sealed record CreateOrderRequest(long TenantId, string RequesterRole, ...);
// ✓ — el rol viene del JWT siempre
```

**No tener al menos un Admin por tenant:**
Siempre validar que queda al menos un Admin cuando se cambia o elimina un usuario Admin.

---

## Relación con el back-template

El back-template usa el campo `Role` en `UserCredential` con el valor `"Admin"` para el primer usuario. Para extender el RBAC:

1. Agregar las constantes de rol en `Common/` o `Authentication.Contracts/`
2. Cambiar `[Authorize]` a `[Authorize(Roles = "Admin")]` en los endpoints que lo requieran
3. Agregar `ChangeUserRole` como caso de uso en `Authentication.Application`
4. Documentar la tabla de permisos en `docs/Authorization.md`

Ver `04-backend/26-tenant-onboarding.md` para el rol del primer usuario al registrar un tenant.
Ver `04-backend/28-user-invitation.md` para cómo se asigna el rol al invitar usuarios.

---

## Glosario

| Término | Definición |
|---------|-----------|
| RBAC | Role-Based Access Control — modelo de autorización donde los permisos se asignan a roles, no a usuarios |
| ClaimTypes.Role | Claim estándar de .NET que almacena el rol del usuario en el JWT — usado por [Authorize(Roles = "...")] |
| Authorize | Atributo de ASP.NET Core que protege endpoints requiriendo autenticación y/o roles específicos |
| Resource-Based Authorization | Verificación de autorización que considera el recurso específico además del rol — necesaria para datos de tenant |
| SystemRoles | Constantes que definen los roles disponibles en el sistema: SuperAdmin, Admin, Manager, Member |
| SuperAdmin | Rol especial del equipo del SaaS con acceso cross-tenant — nunca asignado a usuarios de un tenant |
| Flat Roles | Modelo de roles sin jerarquía donde cada rol es independiente — Admin no hereda permisos de Member |
| Hierarchical Roles | Modelo de roles con herencia donde un rol superior incluye los permisos de los roles inferiores |
| ChangeUserRole | Caso de uso que cambia el rol de un usuario dentro del mismo tenant |
| Policy | Configuración de autorización en ASP.NET Core que puede combinar roles, claims y requisitos personalizados |
| IAuthorizationService | Servicio de ASP.NET Core para verificar autorización de forma programática — usado en resource-based auth |
| Tenant-scoped role | Rol que aplica solo dentro del contexto de un tenant — un usuario puede ser Admin en un tenant y Member en otro |

---

*Rogelio Arriaga Gonzalez*
