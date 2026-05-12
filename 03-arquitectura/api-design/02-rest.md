# 02 — REST: Recursos, URLs y Constraints

REST no es un protocolo ni un estándar — es un estilo arquitectónico. Las APIs "RESTful" siguen sus constraints para ser predecibles e interoperables.

> Fuente: *Programming APIs with C# and .NET* (David Barkol) — Ch.2 RESTful Patterns and Conventions

---

## Los 6 constraints de REST

| Constraint | Qué significa |
|------------|--------------|
| **Client-Server** | Frontend y backend son independientes — se comunican por interfaz uniforme |
| **Stateless** | Cada request contiene toda la información necesaria — el servidor no guarda estado de sesión |
| **Cacheable** | Las responses indican si pueden cachearse — mejora el rendimiento |
| **Uniform Interface** | URL identifica el recurso; verbos HTTP definen la acción; representación estándar (JSON) |
| **Layered System** | El cliente no sabe si habla directamente con el servidor o con un intermediario (proxy, LB) |
| **Code on Demand** (opcional) | El servidor puede enviar código ejecutable (JavaScript) — raramente usado |

**Stateless es el más importante:** cada request es independiente. JWT en el header de cada request, no sesiones en el servidor.

---

## Recursos — la unidad de diseño REST

En REST, todo se modela como un **recurso**. Un recurso es una entidad que se puede identificar, manipular y representar:

```
Recurso       → Substantivo (nunca verbo)
Colección     → Plural
Instancia     → Colección + ID

/api/users              — colección de usuarios
/api/users/123          — un usuario específico
/api/users/123/orders   — órdenes de un usuario específico
/api/orders/456/items   — items de una orden
```

---

## Diseño de URLs

### Reglas

```
✓ Sustantivos, nunca verbos
   /api/users          ✓
   /api/getUsers       ✗
   /api/createUser     ✗

✓ Plural para colecciones
   /api/users          ✓
   /api/user           ✗

✓ Minúsculas con guiones
   /api/example-users  ✓
   /api/ExampleUsers   ✗
   /api/example_users  ✗ (guion bajo es menos legible en URLs subrayadas)

✓ Jerarquía solo cuando hay relación real de posesión
   /api/users/123/orders          ✓ (órdenes de un usuario)
   /api/users/123/recent-activity ✓

✗ No anidar más de 2 niveles
   /api/users/123/orders/456/items/789    ✗ (demasiado anidado)
   → Preferir: /api/order-items/789
```

### Acciones que no son CRUD

Algunas operaciones no encajan en el modelo de recursos. Opciones:

```
POST /api/users/123/activate        — acción como sub-recurso
POST /api/users/123/password/reset  — acción anidada
POST /api/auth/logout               — acción de dominio

POST /api/payments/123/capture      — acción de negocio
POST /api/invoices/123/send         — enviar una factura (side-effect, no CRUD puro)
```

---

## URLs en el proyecto back-template

```csharp
// ✓ Convención del proyecto
[Route("api/example/users")]
public sealed class ExampleUsersController : BaseApiController
{
    [HttpGet]                    // GET  /api/example/users
    [HttpGet("{id:guid}")]       // GET  /api/example/users/{id}
    [HttpPost]                   // POST /api/example/users
    [HttpPut("{id:guid}")]       // PUT  /api/example/users/{id}
    [HttpDelete("{id:guid}")]    // DELETE /api/example/users/{id}
}
```

---

## Request bodies

```csharp
// POST — body con todos los campos requeridos para crear
POST /api/example/users
{
  "fullName": "John Doe",
  "email": "john@test.com",
  "password": "Password1"
}

// PUT — body completo del recurso (reemplaza)
PUT /api/example/users/123
{
  "fullName": "John Updated",
  "email": "john@test.com",
  "isActive": true
}

// PATCH — solo los campos que cambian (actualización parcial)
PATCH /api/example/users/123
{
  "fullName": "John Updated"
}
```

### PUT vs PATCH

```
PUT:   el cliente envía el recurso COMPLETO — los campos no enviados se borran o resetean
PATCH: el cliente envía SOLO los cambios — los campos no enviados no se modifican

Ejemplo práctico con un perfil de usuario (fullName, email, bio, avatar):
- PUT requiere enviar los 4 campos aunque solo cambie el nombre
- PATCH envía solo { "fullName": "nuevo nombre" } — los otros 3 quedan igual

El proyecto usa PUT para simplificar: el frontend siempre envía el objeto completo
```

---

## Naming conventions en JSON

```json
// camelCase — el estándar para APIs JSON (JavaScript convention)
{
  "fullName": "John Doe",
  "createdAt": "2025-05-11T10:00:00Z",
  "isActive": true,
  "ordersCount": 5
}

// snake_case — común en APIs de Python/Ruby
{
  "full_name": "John Doe",
  "created_at": "2025-05-11T10:00:00Z"
}
```

**Consistencia es más importante que el estilo.** El proyecto usa camelCase (configurado en `JsonSerializerOptions`).

---

## Dates — formato ISO 8601

```json
// ✓ ISO 8601 con UTC explícito — el estándar
"createdAt": "2025-05-11T10:30:00Z"
"updatedAt": "2025-05-11T10:30:00.000Z"

// ✗ Formatos ambiguos — nunca en APIs
"createdAt": "05/11/2025"       — mes/día/año o día/mes/año?
"createdAt": "11-05-2025"       — ambiguo
"createdAt": 1747037400         — Unix timestamp en segundos — difícil de debuggear
```

---

## HATEOAS — nivel máximo de REST

Hypermedia As The Engine Of Application State: la response incluye links a las acciones disponibles.

```json
// HATEOAS completo (nivel 3 de Richardson Maturity Model)
{
  "id": "123",
  "fullName": "John Doe",
  "_links": {
    "self":   { "href": "/api/users/123",         "method": "GET" },
    "update": { "href": "/api/users/123",         "method": "PUT" },
    "delete": { "href": "/api/users/123",         "method": "DELETE" },
    "orders": { "href": "/api/users/123/orders",  "method": "GET" }
  }
}
```

En la práctica: pocas APIs implementan HATEOAS completo. El tradeoff de complejidad raramente vale la pena para APIs internas o con un solo cliente. El proyecto back-template no lo implementa.

---

## Richardson Maturity Model

```
Nivel 0: Túnel HTTP   — un endpoint, todo es POST  (SOAP)
Nivel 1: Recursos     — múltiples endpoints por recurso, todo es POST
Nivel 2: Verbos HTTP  — GET/POST/PUT/DELETE correctamente usados ← back-template está aquí
Nivel 3: HATEOAS      — responses con links a acciones disponibles
```

La mayoría de APIs "REST" están en nivel 2 — es suficiente para la mayoría de casos.


---

*Rogelio Arriaga Gonzalez*
