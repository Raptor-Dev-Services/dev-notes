# 35 — Pruebas de Tenant Leakage

Los tests de tenant leakage verifican que el aislamiento multi-tenant funciona correctamente: que ningún tenant puede ver, modificar ni eliminar datos de otro tenant. Son tests de seguridad que deben ejecutarse en CI junto con los tests funcionales.

---

## Por qué son necesarios

Los Global Query Filters de EF Core protegen contra fugas accidentales, pero pueden silenciarse con `IgnoreQueryFilters()`, un bug en un repositorio, un query Dapper sin filtro, o un endpoint que no verifica el tenant del recurso. Los tests de leakage detectan estas regresiones.

```
Escenarios que deben FALLAR:
- Tenant B consulta un recurso de Tenant A con su propio JWT válido
- Usuario de Branch Norte accede a recursos de Branch Sur del mismo tenant
- Endpoint de login de Tenant A accede a credenciales de Tenant B
- Query sin filtro de tenant devuelve datos de todos los tenants
```

---

## Estructura del test: dos tenants, dos requests

```csharp
// Tests/TenantIsolation/UserProfileLeakageTests.cs
public sealed class UserProfileLeakageTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public UserProfileLeakageTests(IntegrationTestFactory factory)
        => _factory = factory;

    [Fact]
    public async Task GetUserProfile_WithOtherTenantJwt_Returns404()
    {
        // Arrange: crear datos para dos tenants distintos
        var tenantA = await _factory.CreateTenantAsync("Tenant A");
        var tenantB = await _factory.CreateTenantAsync("Tenant B");

        var userA = await _factory.CreateUserAsync(tenantA.Id);
        var userB = await _factory.CreateUserAsync(tenantB.Id);

        // Act: Tenant B intenta acceder al perfil de un usuario de Tenant A
        var clientB = _factory.CreateAuthenticatedClient(tenantB.Id, "Admin");

        var response = await clientB.GetAsync($"/api/users/{userA.PublicId}");

        // Assert: 404 (no existe desde la perspectiva de Tenant B)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOnlyOwnTenantUsers()
    {
        // Arrange
        var tenantA = await _factory.CreateTenantAsync("Tenant A");
        var tenantB = await _factory.CreateTenantAsync("Tenant B");

        await _factory.CreateUserAsync(tenantA.Id, "User A1");
        await _factory.CreateUserAsync(tenantA.Id, "User A2");
        await _factory.CreateUserAsync(tenantB.Id, "User B1");

        // Act: Tenant A lista usuarios
        var clientA = _factory.CreateAuthenticatedClient(tenantA.Id, "Admin");
        var response = await clientA.GetAsync("/api/users");
        var result = await response.Content.ReadFromJsonAsync<ResultViewModel<List<UserProfileDto>>>();

        // Assert: solo ve sus 2 usuarios
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, result!.Data!.Count);
        Assert.All(result.Data, u => Assert.DoesNotContain("B", u.FullName));
    }
}
```

---

## IntegrationTestFactory: factory para tests de leakage

```csharp
// Tests/Infrastructure/IntegrationTestFactory.cs
public sealed class IntegrationTestFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IServiceScope _scope;
    private AppDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _scope = Services.CreateScope();

        var accessor = _scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        accessor.Current = new TenantContext("0");   // tenant dummy para EnsureCreated

        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task<TenantRecord> CreateTenantAsync(string name)
    {
        var tenant = new Tenant
        {
            Name   = name,
            Slug   = name.ToLower().Replace(" ", "-"),
            Status = TenantStatus.Active,
            Plan   = "pro"
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        return new TenantRecord(tenant.Id, tenant.PublicId);
    }

    public async Task<UserRecord> CreateUserAsync(long tenantId, string? name = null)
    {
        var publicId = Guid.NewGuid();
        _db.UserProfiles.Add(new UserProfile
        {
            PublicId = publicId,
            TenantId = tenantId,
            FullName = name ?? $"User {publicId:N}",
            IsActive = true
        });
        await _db.SaveChangesAsync();
        return new UserRecord(publicId, tenantId);
    }

    // Crear cliente HTTP con JWT del tenant especificado
    public HttpClient CreateAuthenticatedClient(long tenantId, string role = "Admin")
    {
        var client = CreateClient();
        var token  = GenerateTestJwt(tenantId: tenantId, role: role);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string GenerateTestJwt(
        long tenantId, long? branchId = null, string role = "Admin")
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-min-32-chars-for-hmac-sha256"));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims  = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.Role, role),
        };

        if (branchId.HasValue)
            claims.Add(new Claim("branch_id", branchId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer:   "test",
            audience: "test",
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task DisposeAsync()
    {
        // Limpiar datos de test
        await _db.Database.ExecuteSqlRawAsync(
            "TRUNCATE dbo.user_profiles, dbo.credentials, dbo.tenants RESTART IDENTITY CASCADE");
        _scope.Dispose();
    }
}
```

---

## Tests de modificación cross-tenant

```csharp
[Fact]
public async Task UpdateUserProfile_WithOtherTenantJwt_Returns404()
{
    var tenantA = await _factory.CreateTenantAsync("A");
    var tenantB = await _factory.CreateTenantAsync("B");
    var userA   = await _factory.CreateUserAsync(tenantA.Id, "Ana");

    var clientB = _factory.CreateAuthenticatedClient(tenantB.Id);

    // Tenant B intenta modificar el perfil de un usuario de Tenant A
    var response = await clientB.PutAsJsonAsync(
        $"/api/users/{userA.PublicId}",
        new { FullName = "HACKED" });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    // Verificar que el dato NO cambió en la DB
    var accessor = _factory.Services.CreateScope()
        .ServiceProvider.GetRequiredService<ITenantContextAccessor>();
    accessor.Current = new TenantContext(tenantA.Id.ToString());

    var db = _factory.Services.CreateScope()
        .ServiceProvider.GetRequiredService<AppDbContext>();

    var profile = await db.UserProfiles
        .FirstOrDefaultAsync(u => u.PublicId == userA.PublicId);

    Assert.Equal("Ana", profile?.FullName);   // no fue modificado
}

[Fact]
public async Task DeleteUserProfile_WithOtherTenantJwt_Returns404()
{
    var tenantA = await _factory.CreateTenantAsync("A");
    var tenantB = await _factory.CreateTenantAsync("B");
    var userA   = await _factory.CreateUserAsync(tenantA.Id);

    var clientB = _factory.CreateAuthenticatedClient(tenantB.Id);

    var response = await clientB.DeleteAsync($"/api/users/{userA.PublicId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

---

## Tests de branch leakage (tenant + branch)

```csharp
[Fact]
public async Task GetOrder_FromDifferentBranch_Returns404()
{
    var tenant   = await _factory.CreateTenantAsync("Empresa Alfa");
    var branchN  = await _factory.CreateBranchAsync(tenant.Id, "Sucursal Norte");
    var branchS  = await _factory.CreateBranchAsync(tenant.Id, "Sucursal Sur");

    var orderN = await _factory.CreateOrderAsync(tenant.Id, branchN.Id, "ORD-001");

    // Usuario de Sucursal Sur intenta ver la orden de Sucursal Norte
    var clientSur = _factory.CreateAuthenticatedClient(
        tenantId: tenant.Id,
        branchId: branchS.Id,
        role:     "Operator");

    var response = await clientSur.GetAsync($"/api/orders/{orderN.PublicId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public async Task GetAllOrders_ReturnsOnlyOwnBranchOrders()
{
    var tenant  = await _factory.CreateTenantAsync("Empresa");
    var branchN = await _factory.CreateBranchAsync(tenant.Id, "Norte");
    var branchS = await _factory.CreateBranchAsync(tenant.Id, "Sur");

    await _factory.CreateOrderAsync(tenant.Id, branchN.Id, "ORD-N1");
    await _factory.CreateOrderAsync(tenant.Id, branchN.Id, "ORD-N2");
    await _factory.CreateOrderAsync(tenant.Id, branchS.Id, "ORD-S1");

    var clientNorte = _factory.CreateAuthenticatedClient(
        tenantId: tenant.Id, branchId: branchN.Id);

    var response = await clientNorte.GetAsync("/api/orders");
    var result   = await response.Content.ReadFromJsonAsync<ResultViewModel<List<OrderDto>>>();

    Assert.Equal(2, result!.Data!.Count);
    Assert.All(result.Data, o => Assert.StartsWith("ORD-N", o.OrderNumber));
}

[Fact]
public async Task AdminCorporativo_CanSeeAllBranchOrders()
{
    var tenant  = await _factory.CreateTenantAsync("Empresa");
    var branchN = await _factory.CreateBranchAsync(tenant.Id, "Norte");
    var branchS = await _factory.CreateBranchAsync(tenant.Id, "Sur");

    await _factory.CreateOrderAsync(tenant.Id, branchN.Id, "ORD-N1");
    await _factory.CreateOrderAsync(tenant.Id, branchS.Id, "ORD-S1");

    // Admin sin branch (corporativo) debe ver ambas
    var clientAdmin = _factory.CreateAuthenticatedClient(
        tenantId: tenant.Id,
        branchId: null,         // sin branch = admin corporativo
        role:     "Admin");

    var response = await clientAdmin.GetAsync("/api/orders");
    var result   = await response.Content.ReadFromJsonAsync<ResultViewModel<List<OrderDto>>>();

    Assert.Equal(2, result!.Data!.Count);
}
```

---

## Tests de escalada de privilegios

```csharp
[Fact]
public async Task Viewer_CannotDeleteUser()
{
    var tenant = await _factory.CreateTenantAsync("Empresa");
    var user   = await _factory.CreateUserAsync(tenant.Id);

    var clientViewer = _factory.CreateAuthenticatedClient(
        tenantId: tenant.Id, role: "Viewer");

    var response = await clientViewer.DeleteAsync($"/api/users/{user.PublicId}");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task Operator_CannotAccessTenantSettings()
{
    var tenant = await _factory.CreateTenantAsync("Empresa");

    var clientOperator = _factory.CreateAuthenticatedClient(
        tenantId: tenant.Id, role: "Operator");

    var response = await clientOperator.GetAsync("/api/tenant/settings");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## Tests de autenticación (sin tenant)

```csharp
[Fact]
public async Task LoginWithCorrectCredentials_ReturnsTokenWithCorrectTenantId()
{
    var tenantA = await _factory.CreateTenantAsync("A");
    var tenantB = await _factory.CreateTenantAsync("B");

    // Credencial del tenant A
    await _factory.CreateCredentialAsync(tenantA.Id, "user@a.com", "password123");

    // Intentar login en el tenant B con credenciales del tenant A
    var response = await _factory.CreateClient().PostAsJsonAsync(
        "/api/auth/login",
        new { Email = "user@a.com", Password = "password123", TenantId = tenantB.Id });

    // Debe fallar (credencial pertenece a tenant A, no a tenant B)
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

---

## Integrar en CI

```yaml
# .github/workflows/tests.yml
- name: Run leakage tests
  run: |
    dotnet test back-template/Tests/TenantIsolation/ \
      --logger "trx;LogFileName=leakage-results.trx" \
      --verbosity normal

- name: Upload test results
  uses: actions/upload-artifact@v4
  with:
    name: leakage-test-results
    path: "**/*.trx"
```

---

## Checklist de tests de leakage

- [ ] GET de recurso de otro tenant: 404 (no 403, no revelar que existe)
- [ ] PUT/DELETE de recurso de otro tenant: 404
- [ ] GET all: solo retorna recursos del tenant propio
- [ ] GET de recurso de otro branch (mismo tenant): 404
- [ ] Admin corporativo (sin branch): ve todos los branches del tenant
- [ ] Viewer no puede DELETE ni PUT
- [ ] Operator no puede acceder a settings de tenant
- [ ] Login con credenciales de otro tenant: 401
- [ ] JWT de tenant A no funciona en subdominio de tenant B: 403
- [ ] Tests ejecutan contra una DB real (no mocks): RLS y filtros se prueban de verdad

---

## Glosario

| Término | Definición |
|---------|-----------|
| Tenant Leakage | Situación donde datos de un tenant son accesibles desde el contexto de otro — fallo crítico de seguridad |
| IntegrationTestFactory | Clase que extiende WebApplicationFactory para configurar la app con base de datos de tests |
| WebApplicationFactory | Clase de .NET para tests de integración que levanta la app en memoria con un servidor de tests |
| Test JWT | Token JWT generado con la clave de signing de tests para simular usuarios de diferentes tenants |
| IgnoreQueryFilters | Método usado en el test para verificar que los datos existen pero son inaccesibles desde otro tenant |
| 404 en cross-tenant | Respuesta esperada al acceder a un recurso de otro tenant — el filtro hace que "no exista" |
| Branch Isolation | Verificación de que un usuario de una branch no puede leer datos de otra branch del mismo tenant |
| Privilege Escalation | Test que verifica que un usuario no puede acceder a recursos que su rol no permite |
| Cross-tenant Access | Intento de acceso a datos de otro tenant — debe siempre resultar en 404 (filtrado) o 403 (forbid) |
| DB Real en Tests | Requisito de los tests de leakage: usar una base de datos real (no mocks) para que RLS y filtros sean efectivos |
| TestTenantIds | Constantes con IDs de tenants de test (TenantA, TenantB) para mantener consistencia en los asserts |

---

*Rogelio Arriaga Gonzalez*
