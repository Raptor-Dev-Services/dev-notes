# 19 — Performance en .NET: Identificar y resolver cuellos de botella

La optimización prematura es el origen de todos los males (Knuth). El proceso correcto es: medir → identificar el cuello de botella → optimizar ese punto específico → medir de nuevo.

---

## El proceso de optimización
> Fuente: *High-Performance Programming in C# and .NET* — Ch.1 Performance Methodology

```
1. Establecer una baseline (¿qué tan lento está ahora?)
2. Definir el objetivo (¿qué tan rápido necesita ser?)
3. Perfilar (¿dónde está el tiempo? ¿qué genera más allocations?)
4. Optimizar el punto más lento
5. Medir de nuevo — verificar que mejoró SIN regresar otros indicadores
6. Repetir hasta alcanzar el objetivo
```

```csharp
// BenchmarkDotNet — medir el rendimiento real de un método
// dotnet add package BenchmarkDotNet

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ExampleUserSerializationBenchmarks
{
    private ExampleUser _user = null!;

    [GlobalSetup]
    public void Setup()
    {
        _user = new ExampleUser
        {
            PublicId = Guid.NewGuid(),
            FullName = "María García López",
            Email    = "maria@test.com",
            IsActive = true
        };
    }

    [Benchmark(Baseline = true)]
    public string SystemTextJson()
        => System.Text.Json.JsonSerializer.Serialize(_user);

    [Benchmark]
    public string SystemTextJsonWithOptions()
        => System.Text.Json.JsonSerializer.Serialize(_user, _cachedOptions);

    private static readonly System.Text.Json.JsonSerializerOptions _cachedOptions =
        new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
}

// Ejecutar: dotnet run -c Release --project Benchmarks
```

---

## Strings y allocations

Las strings en .NET son inmutables — cada concatenación crea un nuevo objeto en el heap.

```csharp
// ❌ Concatenación en loop — O(n²) allocations
public string BuildCsv(IEnumerable<ExampleUser> users)
{
    string result = "";
    foreach (var user in users)
        result += $"{user.FullName},{user.Email}\n";  // nueva string en cada iteración
    return result;
}

// ✓ StringBuilder — O(n) allocations
public string BuildCsv(IEnumerable<ExampleUser> users)
{
    var sb = new StringBuilder();
    foreach (var user in users)
        sb.AppendLine($"{user.FullName},{user.Email}");
    return sb.ToString();  // una sola allocation final
}

// ✓ Para CSV grande — escribir directo a stream (0 allocations de string)
public async Task WriteCsvAsync(IEnumerable<ExampleUser> users, Stream output)
{
    await using var writer = new StreamWriter(output, leaveOpen: true);
    foreach (var user in users)
        await writer.WriteLineAsync($"{user.FullName},{user.Email}");
}
```

### Span<T> — trabajar sin allocations

```csharp
// Procesar un substring sin allocar una nueva string
public static bool IsValidEmail(ReadOnlySpan<char> email)
{
    var atIndex = email.IndexOf('@');
    if (atIndex <= 0 || atIndex == email.Length - 1) return false;

    var domain = email[(atIndex + 1)..];
    return domain.Contains('.');
}

// Uso — sin allocar strings
var email = "usuario@ejemplo.com".AsSpan();
bool valid = IsValidEmail(email);   // ← sin new string

// MemoryPool para buffers reutilizables
public async Task ProcessBatchAsync(IDbConnection db, CancellationToken ct)
{
    using var owner  = MemoryPool<ExampleUser>.Shared.Rent(1000);
    var buffer        = owner.Memory.Span;

    var users = await db.QueryAsync<ExampleUser>("SELECT ... FROM ExampleUsers LIMIT 1000");
    int idx   = 0;
    foreach (var user in users)
        buffer[idx++] = user;

    // Procesar buffer[0..idx] sin nuevas allocations
    ProcessBatch(buffer[..idx]);
}
```

---

## Colecciones eficientes

```csharp
// ❌ List<T> cuando se sabe el tamaño — causa resizes internos
var results = new List<ExampleUserDto>();
while (reader.Read())
    results.Add(MapRow(reader));

// ✓ Con capacidad inicial — evita resizes
var results = new List<ExampleUserDto>(expectedCount);

// ✓ Array si el tamaño es fijo — más eficiente en memoria
var results = new ExampleUserDto[rows.Length];
for (int i = 0; i < rows.Length; i++)
    results[i] = MapRow(rows[i]);

// ✓ ImmutableArray para datos que no cambian
var roles = ImmutableArray.Create("admin", "user", "viewer");

// ✓ Dictionary con capacidad inicial para lookups
var userIndex = new Dictionary<Guid, ExampleUser>(users.Count);
foreach (var user in users)
    userIndex[user.PublicId] = user;
```

---

## Async/await — evitar problemas comunes

```csharp
// ❌ .Result / .Wait() — deadlock en contextos síncronos (ASP.NET, UI)
var user = _repo.GetByPublicIdAsync(id).Result;   // puede causar deadlock

// ✓ async/await de punta a punta
public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest req, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(req.PublicId, ct);
    return user is null
        ? new GetExampleUserNotFoundFailure("No encontrado.")
        : new GetExampleUserSuccess(new ExampleUserDto(user));
}

// ❌ async void — excepciones no capturadas
public async void OnUserCreated(object sender, EventArgs e)   // ← async void = peligroso

// ✓ async Task siempre
public async Task OnUserCreatedAsync(object sender, EventArgs e)

// ✓ ConfigureAwait(false) en librerías (no en ASP.NET Core — no tiene SynchronizationContext)
// En ASP.NET Core apps: ConfigureAwait(false) es irrelevante pero tampoco hace daño
```

### Paralelismo con async

```csharp
// ❌ Esperar operaciones una por una cuando son independientes
var user    = await _userRepo.GetByPublicIdAsync(userId, ct);
var profile = await _profileRepo.GetByUserIdAsync(userId, ct);
var roles   = await _roleRepo.GetByUserIdAsync(userId, ct);
// Tiempo total = tiempo(user) + tiempo(profile) + tiempo(roles)

// ✓ Ejecutar en paralelo — tiempo total ≈ max(user, profile, roles)
var (user, profile, roles) = await (
    _userRepo.GetByPublicIdAsync(userId, ct),
    _profileRepo.GetByUserIdAsync(userId, ct),
    _roleRepo.GetByUserIdAsync(userId, ct)
).WhenAll();
// O con Task.WhenAll:
var userTask    = _userRepo.GetByPublicIdAsync(userId, ct);
var profileTask = _profileRepo.GetByUserIdAsync(userId, ct);
var rolesTask   = _roleRepo.GetByUserIdAsync(userId, ct);
await Task.WhenAll(userTask, profileTask, rolesTask);
var user    = await userTask;
var profile = await profileTask;
var roles   = await rolesTask;
```

---

## Response compression — comprimir respuestas HTTP

```csharp
// Program.cs — compresión automática de respuestas
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;   // habilitar en HTTPS (puede ser riesgo CRIME, evaluar)
    options.Providers.Add<BrotliCompressionProvider>();   // Brotli primero (mejor ratio)
    options.Providers.Add<GzipCompressionProvider>();     // fallback a Gzip
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json"]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(opts =>
    opts.Level = System.IO.Compression.CompressionLevel.Fastest);

app.UseResponseCompression();
```

---

## Object pooling — reutilizar objetos costosos

```csharp
// Reutilizar StringBuilder en lugar de crear uno nuevo por request
public sealed class ExampleUserCsvExporter
{
    private static readonly ObjectPool<StringBuilder> _pool =
        new DefaultObjectPoolProvider().CreateStringBuilderPool();

    public string ExportToCsv(IEnumerable<ExampleUser> users)
    {
        var sb = _pool.Get();
        try
        {
            sb.AppendLine("PublicId,FullName,Email,IsActive");
            foreach (var user in users)
                sb.AppendLine($"{user.PublicId},{user.FullName},{user.Email},{user.IsActive}");
            return sb.ToString();
        }
        finally
        {
            _pool.Return(sb);  // devolver al pool — listo para el próximo request
        }
    }
}
```

---

## LINQ — patrones de rendimiento
> Fuente: *High-Performance Programming in C# and .NET* — Ch.7 LINQ Performance

### Acceso al último elemento

```csharp
// ❌ Last() itera toda la colección internamente
var lastUser = users.Last();

// ✓ Índice directo — O(1) sin iteración
var lastUser = users[users.Count - 1];

// ✓ C# 8+ Index syntax
var lastUser = users[^1];
```

### Evitar el keyword `let` en queries LINQ

```csharp
// ❌ let genera IL extra — más lento y más allocations
var result = from u in users
             let name = u.FullName.ToLower()
             where name.Contains("garcía")
             select u;

// ✓ Sin let — el compilador genera menos IL
var result = from u in users
             where u.FullName.ToLower().Contains("garcía")
             select u;

// ✓ O mejor aún — método con lambda (el compilador optimiza más)
var result = users.Where(u => u.FullName.Contains("garcía", StringComparison.OrdinalIgnoreCase));
```

### Ordenar condiciones en filtros

```csharp
// Si hay varios filtros con distintos tamaños de conjuntos, poner el más selectivo primero
// — el short-circuit de && evita evaluar la segunda condición cuando la primera ya falla

// ❌ Verificar primero el conjunto más grande
var result = users.Where(u => largeRoleSet.Contains(u.Role) && smallTenantSet.Contains(u.TenantId));

// ✓ Verificar primero el conjunto más pequeño — elimina más candidatos rápido
var result = users.Where(u => smallTenantSet.Contains(u.TenantId) && largeRoleSet.Contains(u.Role));
```

### GroupBy con conversión a array

```csharp
// ✓ Convertir a array antes de GroupBy puede ser más rápido que operar sobre List<T>
// porque array tiene menor overhead de iteración interna
var grouped = users
    .ToArray()                                     // ← array tiene iteración más rápida
    .GroupBy(u => u.TenantId)
    .Where(g => g.Count() > 1)
    .SelectMany(g => g)
    .ToList();
```

### IQueryable vs materialización prematura

```csharp
// ❌ ToList() trae todos los registros a memoria antes de filtrar
var admins = _context.ExampleUsers
    .ToList()                                      // ← materializa TODOS los usuarios
    .Where(u => u.Role == "Admin");

// ✓ Filtrar en la DB antes de materializar
var admins = await _context.ExampleUsers
    .Where(u => u.Role == "Admin")
    .ToListAsync(ct);                              // ← solo los admins viajan por la red
```

---

## Cuándo optimizar / cuándo no

| Optimizar | No optimizar todavía |
|-----------|---------------------|
| Hay datos de profiling que muestran el problema | "Creo que esta función es lenta" |
| La optimización no compromete la legibilidad | Requiere código muy complejo para ganancias marginales |
| Queries a DB (N+1, full table scans) | Microoptimizaciones en código que se ejecuta poco |
| Serialización en endpoints de alta frecuencia | Serialización en endpoints raramente usados |
| Allocations en el hot path medidas con BenchmarkDotNet | Allocations en paths fríos |


---

## Glosario

| Término | Definición |
|---------|-----------|
| BenchmarkDotNet | Librería de .NET para medir el rendimiento de código de forma reproducible con estadísticas detalladas |
| Span\<T\> | Tipo de .NET que referencia una región contigua de memoria sin allocations adicionales |
| StringBuilder | Clase para construir strings de forma eficiente evitando la inmutabilidad y allocations de string |
| ObjectPool\<T\> | Pool de objetos reutilizables para evitar allocations repetidas de objetos costosos de crear |
| ArrayPool\<T\> | Pool de arrays reutilizables — evita allocations de arrays grandes y reduce presión sobre el GC |
| IQueryable | Interfaz de LINQ para queries diferidas que se traducen a SQL — evita cargar datos innecesarios |
| ConfigureAwait(false) | Llamada que evita la captura del contexto de sincronización en await — mejora rendimiento en librerías |
| Response Compression | Middleware que comprime respuestas HTTP con gzip/brotli — reduce el tamaño de transferencia |
| PerformanceBehavior | Pipeline behavior que mide el tiempo de cada request y alerta ante latencias elevadas |
| Hot Path | Ruta de código ejecutada con alta frecuencia donde las optimizaciones tienen mayor impacto |
| N+1 Query | Problema de rendimiento donde se ejecuta una query por cada elemento de una colección en lugar de un JOIN |
| Allocation | Reserva de memoria en el heap administrado — reduce allocations para disminuir la presión sobre el GC |

---

*Rogelio Arriaga Gonzalez*
