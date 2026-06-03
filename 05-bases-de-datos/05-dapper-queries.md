# 05 — Dapper — Queries Avanzadas

Dapper es un micro-ORM que ejecuta SQL directo y mapea los resultados a objetos C#. El back-template usa Dapper para todas las lecturas — ofrece control total sobre el SQL y rendimiento cercano al ADO.NET puro, con una API mucho más simple.

> Fuente: Documentación oficial Dapper — https://github.com/DapperLib/Dapper; *Using Dapper* — Marc Gravell

---

## Tipos de datos PostgreSQL ↔ C#

| PostgreSQL | C# | Notas |
|------------|-----|-------|
| `UUID` | `Guid` | Siempre para IDs públicos — evita enumeration attacks |
| `SERIAL` / `BIGSERIAL` | `int` / `long` | IDs internos — más eficiente en B-tree que UUID |
| `INTEGER` | `int` | |
| `DECIMAL(p,s)` | `decimal` | Dinero y cantidades exactas — nunca `double` |
| `VARCHAR(n)` / `TEXT` | `string` | |
| `BOOLEAN` | `bool` | |
| `TIMESTAMP(0)` | `DateTime` | Sin milisegundos, siempre UTC |
| `TIMESTAMPTZ` | `DateTimeOffset` | Con zona horaria |
| `DATE` | `DateOnly` | Solo fecha, sin hora |
| `JSONB` | `string` / clase custom | Serializar/deserializar manualmente |

Dapper mapea columnas a propiedades por nombre (case-insensitive). El nombre SQL debe coincidir con el de C#:

```sql
-- ✓ Columna: PublicId → Propiedad: PublicId
-- ✗ Columna: public_id → Propiedad: PublicId (snake_case no mapea automáticamente)
-- Solución: usar alias  SELECT public_id AS PublicId FROM users
```

---

## Convenciones del proyecto (back-template)

```sql
-- Esquema dbo — separa tablas de la app del resto del sistema
CREATE TABLE IF NOT EXISTS dbo.ExampleUsers (
    Id           SERIAL       NOT NULL,          -- PK interna, nunca exponer al cliente
    PublicId     UUID         NOT NULL DEFAULT gen_random_uuid(),  -- ID pública en la API
    FullName     VARCHAR(200) NOT NULL,
    Email        VARCHAR(320) NOT NULL,
    TenantId     UUID         NOT NULL,
    IsActive     BOOLEAN      NOT NULL DEFAULT true,
    CreatedAtUtc TIMESTAMP(0) NOT NULL DEFAULT (timezone('utc', now())),
    UpdatedAtUtc TIMESTAMP(0) NOT NULL DEFAULT (timezone('utc', now())),
    DeletedAt    TIMESTAMP(0) NULL     -- soft delete
);
```

Reglas de todas las queries de lectura:
- `WHERE deleted_at IS NULL` siempre presente
- `AND tenant_id = @TenantId` siempre presente
- Nunca concatenar valores del usuario en el SQL — siempre `@parametro`

---

## Setup en el back-template

```csharp
// Infrastructure/DependencyInjection.cs
builder.Services.AddSingleton<MainDbConnectionFactory>();
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var factory = sp.GetRequiredService<MainDbConnectionFactory>();
    return factory.Create();   // NpgsqlConnection abierta y lista
});

// MainDbConnectionFactory.cs
public class MainDbConnectionFactory(IConfiguration config)
{
    private readonly string _connectionString =
        config.GetConnectionString("Default")!;

    public NpgsqlConnection Create()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
```

---

## SQL Injection — regla absoluta

```csharp
// ❌ SQL Injection — NUNCA concatenar o interpolar valores del usuario
_db.QueryAsync<ExampleUser>($"SELECT * FROM users WHERE email = '{email}'");

// ✓ Siempre parámetros nombrados con @
_db.QueryAsync<ExampleUser>(
    "SELECT * FROM users WHERE email = @Email AND tenant_id = @TenantId",
    new { Email = email, TenantId = tenantId });

// ✓ Colecciones — Dapper expande automáticamente con ANY (PostgreSQL)
var ids = new[] { id1, id2, id3 };
_db.QueryAsync<ExampleUser>(
    "SELECT * FROM users WHERE public_id = ANY(@Ids)",
    new { Ids = ids });
```

---

## Queries básicas

```csharp
// QueryAsync<T> — lista de resultados
public async Task<IEnumerable<ExampleUser>> GetAllActiveAsync(
    Guid tenantId, CancellationToken ct)
{
    const string sql = ExampleUsersSql.GetAllActive;
    return await _db.QueryAsync<ExampleUser>(
        sql,
        new { TenantId = tenantId },
        commandTimeout: 30);
}

// QueryFirstOrDefaultAsync<T> — un resultado o null
public async Task<ExampleUser?> GetByPublicIdAsync(
    Guid publicId, Guid tenantId, CancellationToken ct)
{
    const string sql = ExampleUsersSql.GetByPublicId;
    return await _db.QueryFirstOrDefaultAsync<ExampleUser>(
        sql, new { PublicId = publicId, TenantId = tenantId });
}

// ExecuteAsync — INSERT, UPDATE, DELETE — retorna filas afectadas
public async Task<int> InsertAsync(ExampleUser user, CancellationToken ct)
{
    const string sql = ExampleUsersSql.Insert;
    return await _db.ExecuteAsync(sql, user);
}

// ExecuteScalarAsync<T> — retorna un solo valor
public async Task<int> CountActiveAsync(Guid tenantId)
{
    const string sql = "SELECT COUNT(*) FROM users WHERE tenant_id = @TenantId AND is_active = true";
    return await _db.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId });
}
```

---

## Multi-mapping — JOINs con objetos anidados

Cuando la query tiene JOIN y se quiere mapear a un objeto con propiedad anidada:

```csharp
// SQL con JOIN
public const string GetWithTenant = """
    SELECT  u.id, u.public_id, u.full_name, u.email, u.is_active,
            t.id, t.name AS tenant_name, t.plan_id
    FROM    users u
    JOIN    tenants t ON t.id = u.tenant_id
    WHERE   u.public_id = @PublicId
    """;

// Multi-mapping con splitOn
public async Task<ExampleUserWithTenant?> GetWithTenantAsync(Guid publicId)
{
    var result = await _db.QueryAsync<ExampleUser, Tenant, ExampleUserWithTenant>(
        ExampleUsersSql.GetWithTenant,
        (user, tenant) =>                    // función de ensamblado
        {
            user.Tenant = tenant;
            return user;                     // o return new ExampleUserWithTenant(user, tenant);
        },
        new { PublicId = publicId },
        splitOn: "id");                      // ← columna donde empieza el segundo objeto
                                             //   Dapper parte el ResultSet ahí

    return result.FirstOrDefault();
}
```

> `splitOn` es el nombre de la columna que marca el inicio del siguiente objeto. Si el JOIN trae columnas ambiguas, alias con AS:  
> `u.id AS user_id, t.id AS tenant_id` → `splitOn: "tenant_id"`

---

## QueryMultiple — múltiples result sets en una sola roundtrip

```csharp
// SQL con punto y coma — dos SELECT en una sola llamada
public const string GetUserWithOrders = """
    SELECT  id, public_id, full_name, email
    FROM    users
    WHERE   public_id = @PublicId;

    SELECT  o.id, o.order_number, o.total, o.status
    FROM    orders o
    JOIN    users u ON u.id = o.user_id
    WHERE   u.public_id = @PublicId
    ORDER   BY o.created_at DESC;
    """;

public async Task<(ExampleUser? User, IEnumerable<Order> Orders)> GetUserWithOrdersAsync(
    Guid publicId)
{
    using var multi = await _db.QueryMultipleAsync(
        ExampleUsersSql.GetUserWithOrders,
        new { PublicId = publicId });

    var user   = await multi.ReadFirstOrDefaultAsync<ExampleUser>();
    var orders = await multi.ReadAsync<Order>();

    return (user, orders);
}
```

**Ventaja:** una sola roundtrip a la BD en lugar de dos queries separadas — importante en redes con latencia.

---

## DynamicParameters — parámetros dinámicos y OUTPUT

```csharp
// Parámetros construidos en runtime
public async Task<IEnumerable<ExampleUser>> SearchAsync(
    Guid tenantId, string? email, bool? isActive)
{
    var sql = new StringBuilder("""
        SELECT id, public_id, full_name, email, is_active
        FROM   users
        WHERE  tenant_id = @TenantId
        """);

    var p = new DynamicParameters();
    p.Add("TenantId", tenantId);

    if (email is not null)
    {
        sql.Append(" AND email ILIKE @Email");
        p.Add("Email", $"%{email}%");
    }
    if (isActive.HasValue)
    {
        sql.Append(" AND is_active = @IsActive");
        p.Add("IsActive", isActive.Value);
    }

    return await _db.QueryAsync<ExampleUser>(sql.ToString(), p);
}

// DynamicParameters con OUTPUT (SQL Server) / RETURNING (PostgreSQL)
// En PostgreSQL se usa RETURNING directamente en el SQL:
public const string InsertReturning = """
    INSERT INTO users (public_id, full_name, email, tenant_id, created_at)
    VALUES (@PublicId, @FullName, @Email, @TenantId, NOW())
    RETURNING id, created_at
    """;

public async Task<(long Id, DateTime CreatedAt)> InsertAsync(ExampleUser user)
{
    var result = await _db.QuerySingleAsync<(long Id, DateTime CreatedAt)>(
        ExampleUsersSql.InsertReturning, user);
    return result;
}
```

---

## Paginación eficiente con COUNT total

```csharp
public const string GetPaged = """
    SELECT  u.id, u.public_id, u.full_name, u.email, u.is_active,
            COUNT(*) OVER() AS TotalCount    -- window function: total sin segunda query
    FROM    users u
    WHERE   u.tenant_id = @TenantId
      AND   u.is_active = @IsActive
    ORDER BY u.full_name ASC
    LIMIT   @PageSize OFFSET @Offset
    """;

public sealed record PagedResult<T>(IEnumerable<T> Items, int TotalCount);

public async Task<PagedResult<ExampleUserDto>> GetPagedAsync(
    Guid tenantId, bool isActive, int page, int pageSize)
{
    var rows = await _db.QueryAsync<ExampleUserRow>(
        ExampleUsersSql.GetPaged,
        new { TenantId = tenantId, IsActive = isActive,
              PageSize = pageSize, Offset = (page - 1) * pageSize });

    var list = rows.ToList();
    int total = list.FirstOrDefault()?.TotalCount ?? 0;

    return new PagedResult<ExampleUserDto>(
        list.Select(r => new ExampleUserDto(r.PublicId, r.FullName, r.Email, r.IsActive)),
        total);
}

// Clase auxiliar para capturar TotalCount junto con los datos
private sealed class ExampleUserRow
{
    public Guid    PublicId  { get; init; }
    public string  FullName  { get; init; } = string.Empty;
    public string  Email     { get; init; } = string.Empty;
    public bool    IsActive  { get; init; }
    public int     TotalCount { get; init; }   // ← del COUNT(*) OVER()
}
```

---

## Transacciones con Dapper

```csharp
public async Task TransferAsync(Guid fromId, Guid toId, decimal amount)
{
    using var transaction = _db.BeginTransaction();
    try
    {
        await _db.ExecuteAsync(
            "UPDATE accounts SET balance = balance - @Amount WHERE id = @Id",
            new { Amount = amount, Id = fromId },
            transaction);

        await _db.ExecuteAsync(
            "UPDATE accounts SET balance = balance + @Amount WHERE id = @Id",
            new { Amount = amount, Id = toId },
            transaction);

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

---

## Tenant context — inyectar tenant_id automáticamente

Para no repetir `TenantId` en cada query del repositorio:

```csharp
public abstract class TenantRepository(IDbConnection db, ITenantContextAccessor tenant)
{
    protected readonly IDbConnection _db = db;

    protected DynamicParameters TenantParams(object? extra = null)
    {
        var p = new DynamicParameters(extra);
        p.Add("TenantId", tenant.TenantId);
        return p;
    }
}

public class ExampleUserRepository(IDbConnection db, ITenantContextAccessor tenant)
    : TenantRepository(db, tenant), IExampleUserRepository
{
    public async Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        return await _db.QueryFirstOrDefaultAsync<ExampleUser>(
            ExampleUsersSql.GetByPublicId,
            TenantParams(new { PublicId = publicId }));  // ← TenantId incluido automáticamente
    }
}
```

---

## Bulk insert con tvp-style en PostgreSQL

```csharp
// Para insertar muchos registros eficientemente en PostgreSQL
public async Task BulkInsertAsync(IEnumerable<ExampleUser> users)
{
    // Opción 1: INSERT con múltiples VALUES (hasta ~1000 registros)
    var values = string.Join(",",
        users.Select((_, i) => $"(@PublicId{i}, @FullName{i}, @Email{i}, @TenantId{i})"));

    var sql = $"INSERT INTO users (public_id, full_name, email, tenant_id) VALUES {values}";
    var p   = new DynamicParameters();
    var list = users.ToList();
    for (int i = 0; i < list.Count; i++)
    {
        p.Add($"PublicId{i}",  list[i].PublicId);
        p.Add($"FullName{i}",  list[i].FullName);
        p.Add($"Email{i}",     list[i].Email);
        p.Add($"TenantId{i}",  list[i].TenantId);
    }
    await _db.ExecuteAsync(sql, p);

    // Opción 2: Npgsql COPY — máximo rendimiento para miles de registros
    // Ver: NpgsqlConnection.BeginBinaryImportAsync
}
```

---

## INSERT, UPDATE y Soft Delete

```csharp
// INSERT con RETURNING — retorna el Id generado por la BD
public const string Insert = """
    INSERT INTO dbo.ExampleUsers (PublicId, FullName, Email, TenantId, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@PublicId, @FullName, @Email, @TenantId, timezone('utc', now()), timezone('utc', now()))
    RETURNING Id
    """;

public async Task<int> InsertAsync(ExampleUser user, CancellationToken ct)
    => await _db.ExecuteScalarAsync<int>(ExampleUsersSql.Insert, user);

// UPDATE — verificar que se modificó exactamente 1 fila
public const string Update = """
    UPDATE dbo.ExampleUsers
    SET    FullName     = @FullName,
           UpdatedAtUtc = timezone('utc', now())
    WHERE  PublicId     = @PublicId
      AND  TenantId     = @TenantId
      AND  DeletedAt    IS NULL
    """;

public async Task UpdateAsync(ExampleUser user, CancellationToken ct)
{
    var affected = await _db.ExecuteAsync(ExampleUsersSql.Update, user);
    if (affected == 0)
        throw new KeyNotFoundException($"Usuario {user.PublicId} no encontrado.");
}

// Soft DELETE — nunca borrar físicamente; marcar con timestamp
public const string SoftDelete = """
    UPDATE dbo.ExampleUsers
    SET    DeletedAt    = timezone('utc', now()),
           UpdatedAtUtc = timezone('utc', now())
    WHERE  PublicId     = @PublicId
      AND  TenantId     = @TenantId
      AND  DeletedAt    IS NULL
    """;
```

---

## Relación con el back-template

El back-template sigue estas convenciones con Dapper:

- Todas las queries están en clases estáticas `...Sql` en `Infrastructure/Persistence/SQLDB/`
- Los repositorios inyectan `IDbConnection` (Scoped) — cada request tiene su propia conexión
- `MainDbConnectionFactory` es Singleton — solo guarda el connection string
- El `tenant_id` siempre se pasa como parámetro — nunca hardcodeado en el SQL
- Las queries de lectura usan `QueryAsync` / `QueryFirstOrDefaultAsync` — nunca `Query` síncrono

---

## Cuándo usar / no usar Dapper

| Usar Dapper | Usar EF Core |
|-------------|-------------|
| Queries complejas con JOINs, CTEs, window functions | Operaciones de escritura con change tracking |
| Reports y dashboards con agregaciones | Migrations automáticas |
| Multi-tenancy con Row Level Security explícito | Código rápido tipo scaffold |
| Control total sobre el SQL generado | Relaciones complejas de navegación |
| Rendimiento crítico (benchmarks: 2-3× más rápido que EF Core) | |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Dapper | Micro-ORM de Marc Gravell — extiende IDbConnection con métodos de mapeo de SQL a objetos |
| QueryAsync\<T\> | Ejecuta SELECT y mapea cada fila a un objeto T — retorna IEnumerable\<T\> |
| QueryFirstOrDefaultAsync\<T\> | Retorna el primer resultado o null — más eficiente que FirstOrDefault() en memoria |
| ExecuteAsync | Ejecuta INSERT/UPDATE/DELETE — retorna número de filas afectadas |
| ExecuteScalarAsync\<T\> | Retorna un solo valor escalar de la query — COUNT, SUM, etc. |
| QueryMultiple | Ejecuta múltiples SELECTs en una sola roundtrip — usa GridReader para leer cada result set |
| DynamicParameters | Colección de parámetros construida en runtime — permite queries dinámicas |
| Multi-mapping | Mapeo de un JOIN a múltiples objetos C# en una sola query — usa splitOn |
| splitOn | Nombre de columna que indica a Dapper dónde empieza el siguiente objeto en un multi-map |
| RETURNING | Cláusula PostgreSQL que retorna valores de filas insertadas/actualizadas — equivale a OUTPUT en SQL Server |
| Window Function | COUNT(*) OVER() — calcula sobre filas sin colapsarlas, útil para paginación con total |
| Roundtrip | Una llamada de red entre la aplicación y la base de datos — reducirlas mejora el rendimiento |
| TenantParams | Método auxiliar del repo base que añade TenantId a los parámetros Dapper automáticamente |
| Connection Scoped | IDbConnection con lifetime Scoped — una conexión por request HTTP, se cierra al final |
| MainDbConnectionFactory | Factory Singleton que crea y abre conexiones NpgsqlConnection a partir del connection string |

---

*Rogelio Arriaga Gonzalez*
