# 04 — TestContainers: PostgreSQL Real en Tests

Levanta contenedores Docker reales en los tests — PostgreSQL, Redis, RabbitMQ — de forma programática y sin configuración manual.

> Fuente: *Real-World Web Development with .NET 9* (Mark J. Price) — Ch.10 Integration Testing with Testcontainers

---

## El problema que resuelve

Los tests de repositorios necesitan una base de datos real. Las alternativas insatisfactorias:

```
❌ In-memory provider (SQLite) — semántica diferente, índices inexistentes, funciones de PostgreSQL no funcionan
❌ DB de desarrollo compartida — tests se interfieren entre sí, datos sucios, no reproducible en CI
❌ Configuración manual en CI — complejo, frágil, depende del ambiente

✓ TestContainers — levanta un PostgreSQL limpio, idéntico al de producción, en el propio test
```

---

## Setup

```xml
<!-- Tests/Tests.csproj -->
<PackageReference Include="Testcontainers.PostgreSql" Version="3.*" />
```

---

## DatabaseFixture — el contenedor de PostgreSQL

```csharp
// Tests/Fixtures/DatabaseFixture.cs
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        // 1. Arrancar el contenedor
        await _container.StartAsync();

        // 2. Aplicar las migraciones del proyecto (mismo SQL que en producción)
        await ApplyMigrationsAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private async Task ApplyMigrationsAsync()
    {
        // Leer los archivos .sql de migración del proyecto y ejecutarlos en orden
        var migrationsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Host", "Services", "Schema Migration", "Tables");

        var sqlFiles = Directory.GetFiles(migrationsPath, "*.sql")
            .OrderBy(f => f)
            .ToList();

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        foreach (var file in sqlFiles)
        {
            var sql = await File.ReadAllTextAsync(file);
            await conn.ExecuteAsync(sql);
        }
    }
}
```

---

## Fixture compartida entre múltiples clases de test

Para que el contenedor se levante una sola vez por sesión de test (y no por clase), usar `ICollectionFixture`:

```csharp
// Tests/Fixtures/DatabaseCollection.cs
[CollectionDefinition(nameof(DatabaseCollection))]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

// Tests/Repositories/ExampleUserRepositoryTests.cs
[Collection(nameof(DatabaseCollection))]
public sealed class ExampleUserRepositoryTests
{
    private readonly DatabaseFixture _db;

    public ExampleUserRepositoryTests(DatabaseFixture db) => _db = db;

    [Fact]
    public async Task GetByPublicIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange — insertar un usuario con SQL directo
        await using var conn = new NpgsqlConnection(_db.ConnectionString);
        await conn.OpenAsync();

        var userId = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO dbo.ExampleUsers (PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@PublicId, 'Test User', 'test@test.com', true, now(), now());
            """,
            new { PublicId = userId });

        // Construir el repositorio apuntando a la DB de test
        var factory = new TestDbConnectionFactory(_db.ConnectionString);
        var db      = new MainDapperDbConnection(factory, NullLogger<MainDapperDbConnection>.Instance);
        var sql     = new ExampleUsersSql(db);
        var repo    = new ExampleUserRepository(sql);

        // Act
        var user = await repo.GetByPublicIdAsync(userId, CancellationToken.None);

        // Assert
        user.Should().NotBeNull();
        user!.FullName.Should().Be("Test User");
        user.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetByPublicIdAsync_NonExistentUser_ReturnsNull()
    {
        var factory = new TestDbConnectionFactory(_db.ConnectionString);
        var db      = new MainDapperDbConnection(factory, NullLogger<MainDapperDbConnection>.Instance);
        var sql     = new ExampleUsersSql(db);
        var repo    = new ExampleUserRepository(sql);

        var user = await repo.GetByPublicIdAsync(Guid.NewGuid(), CancellationToken.None);

        user.Should().BeNull();
    }
}
```

---

## TestDbConnectionFactory — fábrica de conexiones para test

```csharp
// Tests/Helpers/TestDbConnectionFactory.cs
public sealed class TestDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public TestDbConnectionFactory(string connectionString)
        => _connectionString = connectionString;

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
```

---

## Limpieza entre tests

Para que los tests no se contaminen entre sí, limpiar las tablas antes de cada test o usar transacciones:

### Opción A — truncate antes de cada test

```csharp
public sealed class ExampleUserRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;

    public ExampleUserRepositoryTests(DatabaseFixture db) => _db = db;

    public async Task InitializeAsync()
    {
        // Limpiar tablas antes de cada test
        await using var conn = new NpgsqlConnection(_db.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync("TRUNCATE TABLE dbo.ExampleUsers RESTART IDENTITY CASCADE;");
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
```

### Opción B — transacción por test (más rápida)

```csharp
public sealed class ExampleUserRepositoryTests : IAsyncLifetime
{
    private NpgsqlConnection    _conn        = null!;
    private NpgsqlTransaction   _transaction = null!;

    public async Task InitializeAsync()
    {
        _conn        = new NpgsqlConnection(_db.ConnectionString);
        await _conn.OpenAsync();
        _transaction = await _conn.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();  // revertir todos los cambios del test
        await _conn.DisposeAsync();
    }
}
```

---

## WebApplicationFactory + TestContainers

Para integration tests que usan la app completa con DB real:

```csharp
// Tests/Fixtures/WebAppWithDbFixture.cs
public sealed class WebAppWithDbFixture
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ApplyMigrationsAsync(_container.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Apuntar la app al contenedor de test
                ["ConnectionStrings:MainDbConnection"] = _container.GetConnectionString(),
                ["Jwt:Key"] = "test-jwt-key-min-32-characters-long!!"
            });
        });
    }

    private static async Task ApplyMigrationsAsync(string connectionString)
    {
        // ... mismo código que DatabaseFixture.ApplyMigrationsAsync ...
    }
}
```

```csharp
// Tests/Integration/ExampleUsersApiTests.cs
[Collection(nameof(WebDbCollection))]
public sealed class ExampleUsersApiTests
{
    private readonly HttpClient _client;
    private readonly WebAppWithDbFixture _fixture;

    public ExampleUsersApiTests(WebAppWithDbFixture fixture)
    {
        _fixture = fixture;
        _client  = fixture.CreateClient();
    }

    [Fact]
    public async Task GetAll_AuthenticatedUser_Returns200WithList()
    {
        // Sembrar datos en la DB de test
        await _fixture.SeedAsync(new ExampleUser { /* ... */ });

        var token = JwtTestHelper.GenerateToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/example/users?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

---

## Relación con back-template

El proyecto ya tiene `Tests/Tests.csproj`. Para agregar TestContainers:

1. Agregar paquete `Testcontainers.PostgreSql` al proyecto de tests.
2. Crear `Tests/Fixtures/DatabaseFixture.cs` con `PostgreSqlBuilder`.
3. Crear `Tests/Helpers/TestDbConnectionFactory.cs`.
4. Los archivos `.sql` de migración de `Host/Services/Schema Migration/` se aplican en `InitializeAsync()`.
5. Los tests de repositorios usan la `DatabaseFixture` vía `ICollectionFixture`.

---

## Cuándo usar / Cuándo no usar

| Escenario | Decisión |
|-----------|----------|
| Tests de repositorios (`...Sql` classes, queries) | ✓ TestContainers — única forma real |
| Tests de migraciones SQL | ✓ TestContainers — verificar idempotencia |
| Tests de índices y rendimiento de queries | ✓ TestContainers |
| Integration tests de la API completa con DB | ✓ TestContainers + WebApplicationFactory |
| Tests de Handlers (lógica) | ✗ No necesario — unit test con mock del repo |
| CI/CD pipeline | ✓ TestContainers — Docker disponible en GitHub Actions |
| Máquina sin Docker | ✗ No funciona — requiere Docker |


---

*Rogelio Arriaga Gonzalez*
