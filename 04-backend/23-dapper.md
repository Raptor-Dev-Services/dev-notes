# 23 — Dapper: Micro-ORM para .NET

Dapper es un micro-ORM de .NET que mapea resultados SQL directamente a objetos C#. Es más rápido que EF Core en lecturas complejas porque no tiene change tracking ni generación de queries — escribe el SQL tú mismo.

---

## Por qué Dapper existe

EF Core genera SQL automáticamente desde LINQ. En queries complejos (JOINs múltiples, CTEs, funciones de ventana) el SQL generado puede ser ineficiente o difícil de controlar. Dapper deja el SQL exactamente como lo escribes, con el mínimo overhead posible.

```
EF Core LINQ  → EF Core genera SQL → ADO.NET → PostgreSQL
Dapper        → Tu SQL directo      → ADO.NET → PostgreSQL (menos capas = más rápido)
```

---

## Instalación

```xml
<!-- Shared/Database.csproj o {Modulo}.Infrastructure.csproj -->
<PackageReference Include="Dapper"  Version="2.1.72" />
<PackageReference Include="Npgsql"  Version="10.*" />
```

---

## Uso básico con `IDbConnection`

```csharp
using Dapper;
using Npgsql;

await using var connection = new NpgsqlConnection(connectionString);
// Dapper abre la conexión automáticamente si está cerrada

// Query — mapea filas a objetos
var users = await connection.QueryAsync<ExampleUser>(
    "SELECT id, public_id, full_name, email FROM dbo.example_users WHERE is_active = TRUE");

// QuerySingle — lanza si no hay exactamente 1 resultado
var user = await connection.QuerySingleOrDefaultAsync<ExampleUser>(
    "SELECT id, public_id, full_name FROM dbo.example_users WHERE public_id = @publicId",
    new { publicId });

// Execute — INSERT, UPDATE, DELETE (retorna filas afectadas)
var rows = await connection.ExecuteAsync(
    "UPDATE dbo.example_users SET full_name = @fullName WHERE public_id = @publicId",
    new { fullName, publicId });

// ExecuteScalar — retorna un valor escalar
var count = await connection.ExecuteScalarAsync<int>(
    "SELECT COUNT(*) FROM dbo.example_users WHERE tenant_id = @tenantId",
    new { tenantId });
```

---

## Parámetros — siempre con `new { }`, nunca interpolación

```csharp
// ✓ Parámetros — Dapper genera SQL parametrizado, seguro contra inyección
var user = await conn.QuerySingleOrDefaultAsync<ExampleUser>(
    "SELECT * FROM dbo.example_users WHERE email = @email",
    new { email });

// ❌ NUNCA interpolación — SQL injection
var user = await conn.QuerySingleOrDefaultAsync<ExampleUser>(
    $"SELECT * FROM dbo.example_users WHERE email = '{email}'");
```

---

## Múltiples queries en un solo round-trip (`QueryMultiple`)

```csharp
var sql = """
    SELECT * FROM dbo.example_users WHERE tenant_id = @tenantId
    ORDER BY created_at_utc DESC LIMIT @pageSize OFFSET @offset;

    SELECT COUNT(*) FROM dbo.example_users WHERE tenant_id = @tenantId;
    """;

using var multi = await conn.QueryMultipleAsync(sql, new { tenantId, pageSize, offset });

var items = await multi.ReadAsync<ExampleUser>();
var total = await multi.ReadSingleAsync<int>();
```

Un solo viaje a la DB en lugar de dos.

---

## Mapeo a tipos personalizados

### Mapeo automático (convención snake_case → PascalCase)

Npgsql + Dapper mapea `public_id` a `PublicId` automáticamente al activar `DefaultTypeMap.MatchNamesWithUnderscores`:

```csharp
// Una sola vez al iniciar la app (Program.cs o DbFactory)
DefaultTypeMap.MatchNamesWithUnderscores = true;
```

### Mapeo con alias en SQL

```csharp
public sealed class ExampleUserSummary
{
    public Guid   PublicId { get; init; }
    public string FullName { get; init; } = string.Empty;
}

var summaries = await conn.QueryAsync<ExampleUserSummary>(
    "SELECT public_id AS PublicId, full_name AS FullName FROM dbo.example_users");
```

### Multi-mapping — JOIN a múltiples objetos

```csharp
var sql = """
    SELECT u.id, u.public_id, u.full_name, t.id, t.name
    FROM dbo.example_users u
    INNER JOIN dbo.tenants t ON t.id = u.tenant_id
    WHERE u.is_active = TRUE
    """;

var users = await conn.QueryAsync<ExampleUser, ExampleTenant, ExampleUser>(
    sql,
    (user, tenant) =>
    {
        user.Tenant = tenant;
        return user;
    },
    splitOn: "id");   // ← columna donde empieza el segundo objeto
```

---

## DapperSqlDbConnectionBase — patrón del back-template

La librería `Common` incluye `DapperSqlDbConnectionBase` — una clase base que envuelve `IDbConnection` con logging automático (nombre de query, duración, hash del SQL):

```csharp
// Patrón del back-template (cuando se usa Dapper en lugar de EF Core)
// {Modulo}.Infrastructure/Persistence/SQLDB/{Entidad}Sql.cs

public sealed class ExampleUserSql
{
    private readonly IDapperSqlDbConnection _db;  // inyectado por DI

    public ExampleUserSql(IDapperSqlDbConnection db) => _db = db;

    public Task<ExampleUser?> GetByPublicIdAsync(
        Guid publicId, long tenantId, CancellationToken ct = default) =>
        _db.QuerySingleAsync<ExampleUser?>(
            """
            SELECT id, public_id, tenant_id, full_name, is_active, created_at_utc
            FROM dbo.example_users
            WHERE public_id = @publicId
              AND tenant_id  = @tenantId
              AND is_active  = TRUE;
            """,
            new { publicId, tenantId },
            cancellationToken: ct);
}
```

Los queries pasan por la clase base que:
1. Loguea el nombre del método como `QueryName` en Serilog
2. Mide el tiempo de ejecución como `ElapsedMs`
3. Calcula el SHA-256 del SQL como `SqlHash`
4. Si `IncludeSqlText: true` en config, loguea el SQL completo como `SqlText`

---

## Transacciones con Dapper

```csharp
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync(ct);
await using var transaction = await connection.BeginTransactionAsync(ct);

try
{
    await connection.ExecuteAsync(
        "INSERT INTO dbo.example_users (public_id, tenant_id, full_name) VALUES (@publicId, @tenantId, @fullName)",
        new { publicId, tenantId, fullName },
        transaction: transaction);

    await connection.ExecuteAsync(
        "INSERT INTO dbo.audit_log (entity_id, action) VALUES (@entityId, 'created')",
        new { entityId = publicId },
        transaction: transaction);

    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

---

## Paginación con COUNT en un solo round-trip

```csharp
var sql = """
    SELECT
        id, public_id, full_name, is_active, created_at_utc,
        COUNT(*) OVER() AS total_count   -- función de ventana — no hace segunda pasada
    FROM dbo.example_users
    WHERE tenant_id = @tenantId AND is_active = TRUE
    ORDER BY created_at_utc DESC
    LIMIT @pageSize OFFSET @offset;
    """;

var rows = await conn.QueryAsync<(ExampleUser User, int TotalCount)>(
    sql, new { tenantId, pageSize, offset = (page - 1) * pageSize });

var list  = rows.ToList();
var total = list.Count > 0 ? list[0].TotalCount : 0;
var items = list.Select(r => r.User).ToList();
```

---

## Dapper vs EF Core — cuándo usar cada uno

| Situación | Usa |
|-----------|-----|
| CRUD simple (Insert, Update, Delete, GetById) | EF Core |
| Queries con `init` properties y `ExecuteUpdateAsync` | EF Core |
| Global Query Filters (multi-tenancy, soft delete) | EF Core |
| Migrations automáticas | EF Core |
| Reportes con JOINs complejos y funciones de ventana | Dapper |
| Queries con CTEs, LATERAL JOINs, `RETURNING` | Dapper |
| Máximo rendimiento en lecturas masivas | Dapper |
| Optimización de un query específico que EF Core genera mal | Dapper |

**Estrategia mixta:** EF Core para escrituras y queries simples (Commands), Dapper para lecturas complejas y reportes (Queries).

---

## Relación con el back-template

El back-template usa **EF Core** como ORM principal. `Common` incluye `DapperSqlDbConnectionBase` para casos donde se necesita control total del SQL. En la arquitectura actual, todos los repositorios usan `AppDbContext` (EF Core).

Si agregas Dapper a un módulo, el patrón es:
- `{Modulo}.Infrastructure/Persistence/SQLDB/{Entidad}Sql.cs` — queries SQL con `IDapperSqlDbConnection`
- `{Modulo}.Infrastructure/Repositories/{Entidad}Repository.cs` — orquesta entre SQL y dominio

Ver `04-backend/17-ef-core.md` para los patrones de EF Core del proyecto.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Dapper | Micro-ORM de .NET que extiende IDbConnection con métodos de mapeo de SQL a objetos |
| IDbConnection | Interfaz ADO.NET que representa una conexión a base de datos — Dapper extiende sus métodos |
| QueryMultiple | Método de Dapper que ejecuta múltiples SELECT en una sola query y retorna un GridReader |
| Multi-mapping | Capacidad de Dapper para mapear una fila a múltiples objetos C# en una sola query con JOIN |
| DapperSqlDbConnectionBase | Clase base del Common que encapsula IDbConnection con soporte de tenant context |
| IDapperSqlDbConnection | Interfaz del Common que abstrae la conexión Dapper para inyección de dependencias |
| DefaultTypeMap | Configuración de Dapper para mapear columnas snake_case a propiedades PascalCase automáticamente |
| snake_case | Convención de nombres de PostgreSQL para columnas (`user_id`) — requiere mapeo en Dapper |
| CTE | Common Table Expression — cláusula SQL WITH que simplifica queries complejas y recursivas |
| Npgsql | Driver ADO.NET para PostgreSQL en .NET — usado como proveedor de conexión con Dapper |
| SplitOn | Parámetro de Dapper para multi-mapping que indica en qué columna dividir el resultado entre objetos |
| Parametrized Query | Query SQL con parámetros tipados que evitan inyección SQL y permiten plan caching en PostgreSQL |

---

*Rogelio Arriaga Gonzalez*
