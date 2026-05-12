# 05 — PostgreSQL y Dapper: Convenciones del Proyecto

Cómo se integra PostgreSQL con Dapper en la arquitectura de back-template.

> Fuente: *Empezando con PostgreSQL* — Ch.3 Consultas avanzadas y drivers .NET

---

## Tipos de datos PostgreSQL ↔ C#

| PostgreSQL | C# | Notas |
|------------|-----|-------|
| `UUID` | `Guid` | Siempre usar para IDs públicos |
| `SERIAL` / `BIGSERIAL` | `int` / `long` | IDs internos autoincrement |
| `INTEGER` | `int` | |
| `BIGINT` | `long` | |
| `DECIMAL(p,s)` | `decimal` | Dinero, cantidades exactas |
| `DOUBLE PRECISION` | `double` | Flotante — evitar para dinero |
| `VARCHAR(n)` | `string` | Longitud máxima definida |
| `TEXT` | `string` | Sin límite de longitud |
| `BOOLEAN` | `bool` | |
| `TIMESTAMP(0)` | `DateTime` | Sin millisegundos, siempre UTC |
| `TIMESTAMPTZ` | `DateTimeOffset` | Con zona horaria |
| `DATE` | `DateOnly` (.NET 6+) | Solo fecha |
| `JSONB` | `string` / clase custom | |
| `INTEGER[]` | `int[]` | Array de enteros |

---

## Convenciones del proyecto

### Esquema

Todas las tablas en el esquema `dbo`:

```sql
CREATE TABLE IF NOT EXISTS dbo.ExampleUsers (
    Id           SERIAL       NOT NULL,
    PublicId     UUID         NOT NULL DEFAULT gen_random_uuid(),
    -- ...
);
```

No usar el esquema `public` por defecto — `dbo` separa las tablas de la app de otras tablas del sistema.

### IDs

```sql
Id       SERIAL NOT NULL,          -- PK interna — nunca exponer al cliente
PublicId UUID   NOT NULL           -- ID pública — exponer en la API
    DEFAULT gen_random_uuid(),
```

`Id` (serial) es la PK para joins internos — más eficiente que UUID en B-tree.  
`PublicId` (UUID) es lo que se devuelve en la API — evita enumeration attacks.

### Fechas

```sql
CreatedAtUtc  TIMESTAMP(0) NOT NULL DEFAULT (timezone('utc', now())),
UpdatedAtUtc  TIMESTAMP(0) NOT NULL DEFAULT (timezone('utc', now()))
```

Siempre UTC. `TIMESTAMP(0)` trunca a segundos — suficiente precisión para audit trail.

---

## Dapper — mapeo automático

Dapper mapea columnas a propiedades por nombre (case-insensitive). El nombre de la columna SQL debe coincidir con el nombre de la propiedad C#:

```sql
-- Columna: PublicId  → Propiedad: PublicId  ✓
-- Columna: FullName  → Propiedad: FullName  ✓
-- Columna: full_name → Propiedad: FullName  ✗ (snake_case no mapea a PascalCase automáticamente)
```

Para columnas con nombres distintos a las propiedades, usar alias:

```sql
SELECT full_name AS FullName, email AS Email FROM dbo.users;
```

O configurar Dapper globalmente para mapear snake_case → PascalCase (con `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true`).

### Tipos personalizados — handler de Dapper

Para tipos que Dapper no conoce (como `Guid` en algunas versiones):

```csharp
// Si Npgsql + Dapper no mapea UUID automáticamente
SqlMapper.AddTypeHandler(new GuidTypeHandler());

public sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value;

    public override Guid Parse(object value)
        => Guid.Parse(value.ToString()!);
}
```

---

## Parámetros en SQL

**Regla absoluta:** nunca concatenar o interpolar valores del usuario en SQL.

```csharp
// ❌ SQL Injection
_db.QueryAsync<ExampleUser>($"SELECT * FROM dbo.ExampleUsers WHERE Email = '{email}'");

// ✓ Siempre parámetros nombrados con @
_db.QueryAsync<ExampleUser>(
    "SELECT * FROM dbo.ExampleUsers WHERE Email = @email;",
    new { email });
```

### Parámetros con objetos anónimos

```csharp
// Objeto anónimo — propiedades = nombres de parámetros
_db.ExecuteAsync(
    "UPDATE dbo.ExampleUsers SET FullName = @FullName WHERE PublicId = @PublicId;",
    new { user.FullName, user.PublicId });
```

### Parámetros IN con colecciones

```csharp
// Dapper expande la colección automáticamente con IN
var ids = new[] { id1, id2, id3 };
_db.QueryAsync<ExampleUser>(
    "SELECT * FROM dbo.ExampleUsers WHERE PublicId = ANY(@ids);",
    new { ids });

// Alternativa con IN (también funciona con Dapper)
_db.QueryAsync<ExampleUser>(
    "SELECT * FROM dbo.ExampleUsers WHERE PublicId IN @ids;",
    new { ids });
```

---

## Métodos de MainDapperDbConnection

Referencia rápida de la API disponible en el proyecto:

```csharp
// Múltiples filas
IEnumerable<T> results = await _db.QueryAsync<T>(sql, params, ct);

// 0 o 1 fila — retorna null si no existe
T? result = await _db.QuerySingleAsync<T>(sql, params, ct);

// Primera fila — retorna null si no hay filas
T? result = await _db.QueryFirstAsync<T>(sql, params, ct);

// INSERT / UPDATE / DELETE — retorna filas afectadas
int affected = await _db.ExecuteAsync(sql, params, ct);

// COUNT / EXISTS / escalar
int count = await _db.ExecuteScalarAsync<int>(sql, ct: ct);
bool exists = await _db.ExecuteScalarAsync<bool>(sql, params, ct);
```

### Lectura multi-resultado

Cuando se necesitan dos queries en una sola ida a la DB (por ejemplo, datos + total para paginación):

```csharp
// SQL con múltiples SELECTs
public async Task<(IEnumerable<ExampleUser> Users, int Total)> GetPagedAsync(
    int page, int pageSize, CancellationToken ct)
{
    await using var conn = await _factory.OpenConnectionAsync(ct);

    const string sql = """
        SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc
        FROM   dbo.ExampleUsers
        WHERE  DeletedAt IS NULL
        ORDER BY CreatedAtUtc DESC
        LIMIT  @pageSize OFFSET @offset;

        SELECT COUNT(*) FROM dbo.ExampleUsers WHERE DeletedAt IS NULL;
        """;

    using var multi = await conn.QueryMultipleAsync(sql,
        new { pageSize, offset = (page - 1) * pageSize });

    var users = await multi.ReadAsync<ExampleUser>();
    var total = await multi.ReadSingleAsync<int>();

    return (users, total);
}
```

---

## Patterns de INSERT / UPDATE / DELETE

```csharp
// INSERT retornando el Id generado
public Task<int> InsertAsync(ExampleUser user, CancellationToken ct) =>
    _db.ExecuteScalarAsync<int>(
        """
        INSERT INTO dbo.ExampleUsers (PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@PublicId, @FullName, @Email, @IsActive, timezone('utc', now()), timezone('utc', now()))
        RETURNING Id;
        """,
        new { user.PublicId, user.FullName, user.Email, user.IsActive },
        ct);

// UPDATE — verificar que se modificó exactamente 1 fila
public async Task UpdateAsync(ExampleUser user, CancellationToken ct)
{
    var affected = await _db.ExecuteAsync(
        """
        UPDATE dbo.ExampleUsers
        SET FullName     = @FullName,
            UpdatedAtUtc = timezone('utc', now())
        WHERE PublicId   = @PublicId
          AND DeletedAt  IS NULL;
        """,
        new { user.FullName, user.PublicId },
        ct);

    if (affected == 0)
        throw new KeyNotFoundException($"Usuario {user.PublicId} no encontrado.");
}

// Soft DELETE
public Task SoftDeleteAsync(Guid publicId, CancellationToken ct) =>
    _db.ExecuteAsync(
        """
        UPDATE dbo.ExampleUsers
        SET DeletedAt    = timezone('utc', now()),
            UpdatedAtUtc = timezone('utc', now())
        WHERE PublicId   = @publicId
          AND DeletedAt  IS NULL;
        """,
        new { publicId },
        ct);
```

---

## Relación con back-template

Todo SQL del proyecto vive en `Infrastructure/Persistence/SQLDB/Main/{Modulo}/{Entidad}Sql.cs`.

Checklist de una clase `...Sql` bien hecha:
- Recibe `MainDapperDbConnection` en el constructor
- Todos los SQL como raw strings `"""..."""`
- Parámetros siempre como objeto anónimo `new { param }`
- `WHERE DeletedAt IS NULL` en todas las queries de lectura
- `AND DeletedAt IS NULL` en UPDATE/DELETE para no tocar registros eliminados
- `timezone('utc', now())` para todas las fechas
- No retorna Row classes intermedias — mapea directamente a entidades de dominio


---

*Rogelio Arriaga Gonzalez*
