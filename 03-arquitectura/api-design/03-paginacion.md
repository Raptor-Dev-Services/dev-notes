# 03 — Paginación: Offset, Cursor y Keyset

Devolver grandes colecciones de datos en partes manejables.

> Fuente: *Programming APIs with C# and .NET* (David Barkol) — Ch.4 Pagination, Filtering, and Sorting

---

## El problema que resuelve

Sin paginación, un GET que devuelve 1,000,000 de registros:
- Consume memoria desproporcionada en el servidor
- Hace esperar al usuario segundos antes de ver algo
- Puede crashear el proceso por OOM

---

## Offset pagination — la más común

Basada en `LIMIT` y `OFFSET` en SQL.

### Query string

```
GET /api/example/users?page=1&pageSize=10
GET /api/example/users?page=2&pageSize=10
GET /api/example/users?page=3&pageSize=50
```

### Request y Response

```csharp
// Application/UseCases/ExampleUsers/GetAll/GetExampleUsersRequest.cs
public sealed record GetExampleUsersRequest(int Page, int PageSize)
    : IRequest<GetExampleUsersResponse>;

// Application/UseCases/ExampleUsers/GetAll/Responses/GetExampleUsersSuccess.cs
public sealed record GetExampleUsersSuccess(
    IReadOnlyCollection<ExampleUserDto> Items,
    int                                 Total,
    int                                 Page,
    int                                 PageSize,
    int                                 TotalPages)
    : GetExampleUsersResponse, ISuccess;
```

```json
// Respuesta
{
  "isSuccess": true,
  "data": {
    "items": [ {...}, {...}, {...} ],
    "total": 1523,
    "page": 2,
    "pageSize": 10,
    "totalPages": 153
  }
}
```

### SQL

```sql
SELECT Id, PublicId, FullName, Email, IsActive
FROM   dbo.ExampleUsers
WHERE  DeletedAt IS NULL
ORDER BY CreatedAtUtc DESC
LIMIT  @pageSize
OFFSET @offset;     -- offset = (page - 1) * pageSize

-- Segundo query para el total
SELECT COUNT(*) FROM dbo.ExampleUsers WHERE DeletedAt IS NULL;
```

### Handler

```csharp
public sealed class GetExampleUsersHandler
    : IRequestHandler<GetExampleUsersRequest, GetExampleUsersResponse>
{
    private readonly IExampleUserRepository _repo;

    public async Task<GetExampleUsersResponse> Handle(
        GetExampleUsersRequest request, CancellationToken ct)
    {
        // Validación básica de parámetros
        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);  // máximo 100 por página

        var (users, total) = await _repo.GetPagedAsync(page, pageSize, ct);

        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        var dtos       = users.Select(u => new ExampleUserDto(u)).ToList();

        return new GetExampleUsersSuccess(dtos, total, page, pageSize, totalPages);
    }
}
```

### Controller

```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] int page     = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken ct     = default)
{
    _ = await Mediator.Send(new GetExampleUsersRequest(page, pageSize), ct);
    return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
}
```

### Problemas de offset pagination

```
Problema 1: Registros duplicados en páginas consecutivas
   Si se inserta un registro entre GET page=1 y GET page=2,
   el registro de la posición 11 aparece como primero de la página 2
   (el que era el primero de p2 se "empujó" a la posición 12).

Problema 2: Rendimiento en OFFSET grande
   OFFSET 100000 → PostgreSQL lee y descarta las primeras 100,000 filas
   En tablas de millones de filas: segundos de latencia.
```

---

## Cursor pagination — basada en un punto de anclaje

Usa un valor de la última fila vista como cursor en lugar de un número de página.

```
GET /api/example/users?pageSize=10
→ Response: items + cursor: "2025-05-11T10:00:00Z_abc123"

GET /api/example/users?pageSize=10&cursor=2025-05-11T10:00:00Z_abc123
→ Response: siguiente página + nuevo cursor
```

### SQL — keyset pagination

```sql
-- Primera página (sin cursor)
SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc
FROM   dbo.ExampleUsers
WHERE  DeletedAt IS NULL
ORDER BY CreatedAtUtc DESC, PublicId DESC   -- orden determinístico
LIMIT  @pageSize;

-- Páginas siguientes (con cursor = último CreatedAtUtc + PublicId vistos)
SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc
FROM   dbo.ExampleUsers
WHERE  DeletedAt IS NULL
  AND  (CreatedAtUtc, PublicId) < (@lastCreatedAt, @lastPublicId)   -- row comparison
ORDER BY CreatedAtUtc DESC, PublicId DESC
LIMIT  @pageSize;
```

La cláusula `(col1, col2) < (@val1, @val2)` es **row comparison** de PostgreSQL — evalúa como `(col1 < @val1) OR (col1 = @val1 AND col2 < @val2)`.

### Response con cursor

```json
{
  "isSuccess": true,
  "data": {
    "items": [ {...}, {...} ],
    "nextCursor": "2025-05-10T08:00:00Z_550e8400-e29b-41d4-a716-446655440000",
    "hasNextPage": true
  }
}
```

```csharp
// Codificar/decodificar el cursor de forma opaca para el cliente
public static string EncodeCursor(DateTime createdAt, Guid publicId) =>
    Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{createdAt:O}|{publicId}"));

public static (DateTime CreatedAt, Guid PublicId) DecodeCursor(string cursor)
{
    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
    var parts   = decoded.Split('|');
    return (DateTime.Parse(parts[0]), Guid.Parse(parts[1]));
}
```

---

## Comparación de estrategias

| | Offset | Cursor |
|---|--------|--------|
| Implementación | Simple | Más compleja |
| Rendimiento en páginas profundas | Degrada | Constante |
| Saltar a página N | ✓ (/api/users?page=50) | ✗ (no hay "página 50") |
| Datos consistentes al paginar | ✗ (duplicados si hay inserts) | ✓ |
| Ideal para | Dashboards, búsqueda admin | Feeds, scroll infinito |

---

## Parámetros de query adicionales — filtros y ordenamiento

```
GET /api/example/users?page=1&pageSize=10&search=john&sortBy=createdAt&sortDir=desc
GET /api/example/users?page=1&pageSize=10&isActive=true
```

```csharp
public sealed record GetExampleUsersRequest(
    int     Page      = 1,
    int     PageSize  = 10,
    string? Search    = null,
    bool?   IsActive  = null,
    string  SortBy    = "createdAt",
    string  SortDir   = "desc")
    : IRequest<GetExampleUsersResponse>;
```

```sql
-- Query dinámico con filtros opcionales
SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc
FROM   dbo.ExampleUsers
WHERE  DeletedAt IS NULL
  AND  (@Search   IS NULL OR FullName ILIKE '%' || @Search || '%' OR Email ILIKE '%' || @Search || '%')
  AND  (@IsActive IS NULL OR IsActive = @IsActive)
ORDER BY
    CASE WHEN @SortBy = 'fullName' AND @SortDir = 'asc'  THEN FullName     END ASC,
    CASE WHEN @SortBy = 'fullName' AND @SortDir = 'desc' THEN FullName     END DESC,
    CASE WHEN @SortBy = 'createdAt'                       THEN CreatedAtUtc END DESC
LIMIT  @pageSize
OFFSET @offset;
```

---

## Relación con back-template

`back-template/docs/Pagination.md` cubre `PagedResult<T>` específico del proyecto. La implementación concreta usa `GetExampleUsersSuccess` con la colección, total, page y pageSize — exactamente el patrón de este documento.

El Controller recibe los query params con `[FromQuery]` y los pasa al Request. El Handler aplica `Math.Clamp(pageSize, 1, 100)` para evitar que el cliente pida 1,000,000 registros.


---

*Rogelio Arriaga Gonzalez*
