# 09 — Specification Pattern

El Specification Pattern encapsula una regla de negocio (o criterio de filtrado) en un objeto reutilizable y composable. Resuelve el problema de queries complejos que se duplican y de lógica de filtrado dispersa en los repositorios.

---

## El problema que resuelve

```csharp
// ❌ Lógica de filtrado dispersa — repetida en varios handlers y repositorios
public class GetActiveUsersHandler
{
    public async Task<...> Handle(...)
    {
        // la regla "usuario activo" repetida aquí...
        var users = await _repo.GetAllAsync(ct);
        return users.Where(u => u.IsActive && u.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30));
    }
}

public class SendWelcomeEmailHandler
{
    public async Task<...> Handle(...)
    {
        // ...y repetida aquí con posible variación
        var users = await _repo.GetAllAsync(ct);
        return users.Where(u => u.IsActive && u.CreatedAtUtc > DateTime.UtcNow.AddDays(-30));
        //                                                                    ↑ > vs >= — bug sutil
    }
}

// ❌ Repositorio con métodos específicos por combinación de filtros
public interface IUserRepository
{
    Task<IEnumerable<User>> GetActiveAsync();
    Task<IEnumerable<User>> GetActiveByRoleAsync(UserRole role);
    Task<IEnumerable<User>> GetActiveByRoleAndCityAsync(UserRole role, string city);
    Task<IEnumerable<User>> GetActiveCreatedAfterAsync(DateTime date);
    // explosion de métodos con cada combinación posible
}
```

---

## Implementación básica

```csharp
// Interfaz base de una Specification
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);
}

// Specification concreta — encapsula UNA regla
public sealed class ActiveUserSpecification : ISpecification<ExampleUser>
{
    public bool IsSatisfiedBy(ExampleUser user) => user.IsActive;
}

public sealed class RecentUserSpecification : ISpecification<ExampleUser>
{
    private readonly int _daysBack;
    public RecentUserSpecification(int daysBack = 30) => _daysBack = daysBack;

    public bool IsSatisfiedBy(ExampleUser user)
        => user.CreatedAtUtc >= DateTime.UtcNow.AddDays(-_daysBack);
}

public sealed class AdminUserSpecification : ISpecification<ExampleUser>
{
    public bool IsSatisfiedBy(ExampleUser user) => user.Role == UserRole.Admin;
}
```

---

## Composición de Specifications

La potencia del patrón está en componer specifications con operadores lógicos:

```csharp
// Specification base con composición
public abstract class Specification<T> : ISpecification<T>
{
    public abstract bool IsSatisfiedBy(T entity);

    public Specification<T> And(Specification<T> other)
        => new AndSpecification<T>(this, other);

    public Specification<T> Or(Specification<T> other)
        => new OrSpecification<T>(this, other);

    public Specification<T> Not()
        => new NotSpecification<T>(this);
}

public sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    { _left = left; _right = right; }

    public override bool IsSatisfiedBy(T entity)
        => _left.IsSatisfiedBy(entity) && _right.IsSatisfiedBy(entity);
}

public sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    { _left = left; _right = right; }

    public override bool IsSatisfiedBy(T entity)
        => _left.IsSatisfiedBy(entity) || _right.IsSatisfiedBy(entity);
}

public sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _inner;
    public NotSpecification(Specification<T> inner) => _inner = inner;

    public override bool IsSatisfiedBy(T entity)
        => !_inner.IsSatisfiedBy(entity);
}
```

```csharp
// Uso con composición:
var activeSpec  = new ActiveUserSpecification();
var recentSpec  = new RecentUserSpecification(30);
var adminSpec   = new AdminUserSpecification();

// Usuarios activos Y recientes
var activeAndRecent = activeSpec.And(recentSpec);

// Usuarios activos Y (recientes O admin)
var complex = activeSpec.And(recentSpec.Or(adminSpec));

// Aplicar sobre colección en memoria:
var users = await _repo.GetAllAsync(ct);
var filtered = users.Where(u => complex.IsSatisfiedBy(u)).ToList();
```

---

## Specifications con Expression Trees (para SQL)

Las specifications en memoria no generan SQL eficiente. Para usar con Dapper o EF Core se usan `Expression<Func<T, bool>>`:

```csharp
// Specification con Expression — se traduce a SQL
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
        => ToExpression().Compile()(entity);  // en memoria (para tests)

    public Specification<T> And(Specification<T> other)
        => new AndSpecification<T>(this, other);
}

public sealed class ActiveUserSpecification : Specification<ExampleUser>
{
    public override Expression<Func<ExampleUser, bool>> ToExpression()
        => user => user.IsActive;
}

public sealed class RecentUserSpecification : Specification<ExampleUser>
{
    private readonly int _daysBack;
    public RecentUserSpecification(int daysBack = 30) => _daysBack = daysBack;

    public override Expression<Func<ExampleUser, bool>> ToExpression()
    {
        var cutoff = DateTime.UtcNow.AddDays(-_daysBack);
        return user => user.CreatedAtUtc >= cutoff;
    }
}

// AndSpecification con Expressions combinadas
public sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    { _left = left; _right = right; }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr  = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var param     = Expression.Parameter(typeof(T));
        var body      = Expression.AndAlso(
            Expression.Invoke(leftExpr,  param),
            Expression.Invoke(rightExpr, param));

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
```

---

## Specification con Dapper (este proyecto)

Con Dapper no hay LINQ-to-SQL automático. Se usa la Specification para construir la cláusula WHERE:

```csharp
// Specification que genera SQL + parámetros
public abstract class SqlSpecification<T>
{
    public abstract string ToWhereClause();
    public abstract object ToParameters();
}

public sealed class ActiveUserSqlSpec : SqlSpecification<ExampleUser>
{
    public override string ToWhereClause() => "IsActive = true";
    public override object ToParameters()  => new { };
}

public sealed class RecentUserSqlSpec : SqlSpecification<ExampleUser>
{
    private readonly int _daysBack;
    public RecentUserSqlSpec(int daysBack = 30) => _daysBack = daysBack;

    public override string ToWhereClause() => "CreatedAtUtc >= @cutoff";
    public override object ToParameters()  => new { cutoff = DateTime.UtcNow.AddDays(-_daysBack) };
}

// Uso en la clase Sql:
public Task<IEnumerable<ExampleUser>> GetBySpecAsync(
    SqlSpecification<ExampleUser> spec, CancellationToken ct) =>
    _db.QueryAsync<ExampleUser>(
        $"""
        SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc
        FROM dbo.ExampleUsers
        WHERE {spec.ToWhereClause()}
        ORDER BY CreatedAtUtc DESC
        """,
        spec.ToParameters(),
        cancellationToken: ct);
```

---

## Dónde viven las Specifications

```
Domain/Specifications/ExampleUsers/
├── ActiveUserSpecification.cs        ← regla de negocio pura (en memoria)
├── RecentUserSpecification.cs
└── AdminUserSpecification.cs

Infrastructure/Persistence/Specifications/ExampleUsers/
├── ActiveUserSqlSpec.cs              ← traducción a SQL (en Infrastructure)
└── RecentUserSqlSpec.cs
```

Las Specifications de negocio viven en **Domain** — son reglas de negocio puras.
Las Specifications SQL viven en **Infrastructure** — son detalles de persistencia.

---

## Cuándo usar Specification Pattern

**Sí usar cuando:**
- La misma regla de filtrado se repite en varios lugares
- Las queries se construyen dinámicamente según parámetros del usuario
- Los criterios de negocio son suficientemente complejos para merecer nombre y prueba propia
- Se necesita combinar criterios de forma flexible

**No usar cuando:**
- Las queries son simples y no se repiten (`WHERE PublicId = @id`)
- Solo hay una o dos formas de filtrar una entidad
- El overhead de las Specifications no se justifica con la complejidad actual

**En este proyecto:** la mayoría de los queries actuales son suficientemente simples. Las Specifications son útiles cuando aparece la necesidad de combinar filtros (búsquedas con múltiples criterios opcionales, reportes, exportaciones).

---

## Alternativa simple: método de extensión LINQ

Para casos sin tanta complejidad, un método de extensión puede ser suficiente:

```csharp
// Sin Specification Pattern — extensión simple
public static class ExampleUserExtensions
{
    public static IEnumerable<ExampleUser> WhereActive(this IEnumerable<ExampleUser> users)
        => users.Where(u => u.IsActive);

    public static IEnumerable<ExampleUser> WhereRecentlyCreated(
        this IEnumerable<ExampleUser> users, int daysBack = 30)
        => users.Where(u => u.CreatedAtUtc >= DateTime.UtcNow.AddDays(-daysBack));
}

// Uso:
var users = (await _repo.GetAllAsync(ct))
    .WhereActive()
    .WhereRecentlyCreated(30)
    .ToList();
```

Menos formal que Specification pero adecuado cuando la lógica es simple y en memoria.

---

## Specification para filtros dinámicos de UI
> Fuente: *Enterprise Architecture Patterns with .NET* — Ch.5 Query Object; *Patterns of Enterprise Application Architecture* (Fowler) — Query Object

Un caso muy común es construir queries con filtros opcionales que el usuario activa desde la UI. La Specification es ideal para esto: cada filtro activo se agrega como una spec al pipeline.

```csharp
// Request con filtros opcionales desde la UI
public sealed record SearchExampleUsersRequest(
    string?   NameContains,
    bool?     IsActive,
    DateTime? CreatedAfter,
    string?   Role,
    int       Page     = 1,
    int       PageSize = 20) : IRequest<SearchExampleUsersResponse>;

// Builder de filtros — agrega condiciones solo cuando el filtro tiene valor
public sealed class ExampleUserFilterBuilder
{
    private readonly List<string> _conditions = new();
    private readonly DynamicParameters _params = new();

    public ExampleUserFilterBuilder WithName(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            _conditions.Add("FullName ILIKE @name");
            _params.Add("name", $"%{name.Trim()}%");
        }
        return this;
    }

    public ExampleUserFilterBuilder WithActiveStatus(bool? isActive)
    {
        if (isActive.HasValue)
        {
            _conditions.Add("IsActive = @isActive");
            _params.Add("isActive", isActive.Value);
        }
        return this;
    }

    public ExampleUserFilterBuilder WithCreatedAfter(DateTime? date)
    {
        if (date.HasValue)
        {
            _conditions.Add("CreatedAtUtc >= @createdAfter");
            _params.Add("createdAfter", date.Value);
        }
        return this;
    }

    public ExampleUserFilterBuilder WithRole(string? role)
    {
        if (!string.IsNullOrWhiteSpace(role))
        {
            _conditions.Add("Role = @role");
            _params.Add("role", role);
        }
        return this;
    }

    public (string WhereClause, DynamicParameters Parameters) Build()
    {
        var whereClause = _conditions.Any()
            ? "WHERE " + string.Join(" AND ", _conditions)
            : string.Empty;
        return (whereClause, _params);
    }
}
```

```csharp
// Handler que usa el builder de filtros
public sealed class SearchExampleUsersHandler
    : IRequestHandler<SearchExampleUsersRequest, SearchExampleUsersResponse>
{
    private readonly IDbConnection _db;

    public async Task<SearchExampleUsersResponse> Handle(
        SearchExampleUsersRequest req, CancellationToken ct)
    {
        var (whereClause, parameters) = new ExampleUserFilterBuilder()
            .WithName(req.NameContains)
            .WithActiveStatus(req.IsActive)
            .WithCreatedAfter(req.CreatedAfter)
            .WithRole(req.Role)
            .Build();

        parameters.Add("offset", (req.Page - 1) * req.PageSize);
        parameters.Add("limit",  req.PageSize);

        var sql = $"""
            SELECT PublicId, FullName, Email, IsActive, CreatedAtUtc
            FROM ExampleUsers
            {whereClause}
            ORDER BY CreatedAtUtc DESC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;

            SELECT COUNT(*)
            FROM ExampleUsers
            {whereClause};
            """;

        using var multi   = await _db.QueryMultipleAsync(sql, parameters);
        var items          = (await multi.ReadAsync<ExampleUserDto>()).ToList();
        var totalCount     = await multi.ReadSingleAsync<int>();

        return new SearchExampleUsersSuccess(items, totalCount, req.Page, req.PageSize);
    }
}
```

---

## Testing de Specifications

Las Specifications son lógica de negocio pura — se testean sin base de datos.

```csharp
public sealed class ActiveUserSpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_ActiveUser_ReturnsTrue()
    {
        var spec = new ActiveUserSpecification();
        var user = new ExampleUser { IsActive = true };

        Assert.True(spec.IsSatisfiedBy(user));
    }

    [Fact]
    public void IsSatisfiedBy_InactiveUser_ReturnsFalse()
    {
        var spec = new ActiveUserSpecification();
        var user = new ExampleUser { IsActive = false };

        Assert.False(spec.IsSatisfiedBy(user));
    }

    [Fact]
    public void And_ActiveAndRecent_BothMustBeTrue()
    {
        var activeSpec = new ActiveUserSpecification();
        var recentSpec = new RecentUserSpecification(daysBack: 30);
        var combined   = activeSpec.And(recentSpec);

        var activeAndRecent = new ExampleUser
        {
            IsActive     = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };
        var activeButOld = new ExampleUser
        {
            IsActive     = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-60)
        };

        Assert.True(combined.IsSatisfiedBy(activeAndRecent));
        Assert.False(combined.IsSatisfiedBy(activeButOld));
    }
}
```


---

*Rogelio Arriaga Gonzalez*
