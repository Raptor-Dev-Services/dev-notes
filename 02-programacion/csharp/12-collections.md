# 12 — Colecciones y LINQ

Las colecciones son estructuras para almacenar múltiples elementos. LINQ es la sintaxis para consultarlas y transformarlas.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.8 Working with Common .NET Types

---

## Tipos de colección más usados

### `List<T>` — lista mutable, orden garantizado

La colección más común para uso interno.

```csharp
var lista = new List<string>();
lista.Add("uno");
lista.Add("dos");
lista.Add("tres");

// Acceso por índice
Console.WriteLine(lista[0]);  // "uno"

// Modificar
lista[1] = "DOS";

// Eliminar
lista.Remove("tres");      // por valor
lista.RemoveAt(0);         // por índice

// Contar
Console.WriteLine(lista.Count);  // 1

// Iterar
foreach (var item in lista)
    Console.WriteLine(item);
```

### `IEnumerable<T>` — la interfaz más básica

Solo permite iterar. No sabe cuántos elementos tiene, no permite acceso por índice.

```csharp
// QueryAsync de Dapper devuelve IEnumerable<T>
IEnumerable<ExampleUser> users = await _db.QueryAsync<ExampleUser>(sql);

// Puedes iterar:
foreach (var user in users)
    Console.WriteLine(user.FullName);

// Pero NO puedes:
// users[0]     // no hay índice en IEnumerable
// users.Count  // no hay Count (solo .Count() de LINQ)
```

**Importante:** `IEnumerable<T>` es **lazy** (diferida). En Dapper, la consulta SQL puede ejecutarse al iterar, no al asignar.

### `IReadOnlyCollection<T>` — inmutable con Count

Permite iterar y saber cuántos elementos hay, pero no modificar.

```csharp
// Ideal para retornar colecciones en DTOs y Responses
public sealed record GetExampleUsersSuccess(
    IReadOnlyCollection<ExampleUserDto> Users,  // inmutable con Count
    int Total, int Page, int PageSize)
    : GetExampleUsersResponse, ISuccess;

// Quién recibe esto puede iterar y saber la cantidad, pero no modificar la colección
```

### `IReadOnlyList<T>` — inmutable con Count + índice

Como `IReadOnlyCollection<T>` pero también tiene acceso por índice.

```csharp
IReadOnlyList<string> nombres = new List<string> { "Ana", "Luis", "María" };
Console.WriteLine(nombres[1]);  // "Luis" — acceso por índice
Console.WriteLine(nombres.Count); // 3
// nombres.Add("Otra");  // ERROR — ReadOnly no permite modificar
```

### `Dictionary<TKey, TValue>` — clave → valor

```csharp
var cache = new Dictionary<Guid, ExampleUser>();

// Agregar
cache[userId] = user;
cache.Add(userId, user);  // lanza si la clave ya existe

// Leer (seguro con TryGetValue)
if (cache.TryGetValue(userId, out var cachedUser))
    return cachedUser;

// Leer (lanza KeyNotFoundException si no existe)
var u = cache[userId];

// Eliminar
cache.Remove(userId);

// Iterar
foreach (var (key, value) in cache)
    Console.WriteLine($"{key}: {value.FullName}");
```

### `HashSet<T>` — conjunto sin duplicados

```csharp
var roles = new HashSet<string>();
roles.Add("Admin");
roles.Add("User");
roles.Add("User");  // duplicado — no se agrega

Console.WriteLine(roles.Count);       // 2
Console.WriteLine(roles.Contains("Admin")); // true

// Útil para verificar pertenencia O(1)
```

### Arrays `T[]`

Tamaño fijo, alto rendimiento.

```csharp
// Tamaño fijo al crear
string[] dias = new string[7];
dias[0] = "Lunes";

// O con inicialización
string[] dias = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

Console.WriteLine(dias.Length);  // 7 (no .Count, sino .Length)
Console.WriteLine(dias[0]);      // "Lunes"
// dias[7];  // IndexOutOfRangeException
```

---

## Tabla de elección

| Tipo | Cuando usar |
|------|------------|
| `List<T>` | Construir una colección dinámicamente dentro de un método |
| `T[]` | Tamaño fijo conocido, operaciones de bajo nivel |
| `IEnumerable<T>` | Retornar desde métodos que el caller solo iterará |
| `IReadOnlyCollection<T>` | Retornar colecciones inmutables donde el caller necesita Count |
| `IReadOnlyList<T>` | Igual que arriba + acceso por índice |
| `Dictionary<K,V>` | Lookup rápido por clave |
| `HashSet<T>` | Verificar existencia, eliminar duplicados |

---

## LINQ — Language Integrated Query

LINQ permite consultar y transformar colecciones con una sintaxis fluida.

### Filtrar — `Where`

```csharp
var activos = users.Where(u => u.IsActive);
var adminActivos = users.Where(u => u.IsActive && u.Role == "Admin");
```

### Transformar — `Select`

```csharp
// Convertir ExampleUser a ExampleUserDto
var dtos = users.Select(u => new ExampleUserDto(
    u.PublicId, u.FullName, u.Email, u.IsActive, u.CreatedAtUtc, u.UpdatedAtUtc));

// Extraer solo un campo
var emails = users.Select(u => u.Email);
```

### Ordenar — `OrderBy`, `OrderByDescending`

```csharp
var ordenados = users.OrderBy(u => u.FullName);
var recientes = users.OrderByDescending(u => u.CreatedAtUtc);

// Multi-nivel
var multi = users
    .OrderBy(u => u.IsActive)
    .ThenBy(u => u.FullName);
```

### Obtener uno — `First`, `FirstOrDefault`, `Single`, `SingleOrDefault`

```csharp
// First — el primero, lanza si no hay ninguno
var primero = users.First();

// FirstOrDefault — el primero o null si la colección está vacía
var primeroONull = users.FirstOrDefault();

// FirstOrDefault con condición
var admin = users.FirstOrDefault(u => u.Role == "Admin");

// Single — exactamente uno, lanza si hay 0 o más de 1
var exacto = users.Single(u => u.PublicId == publicId);

// SingleOrDefault — uno o null, lanza si hay más de uno
var unicoONull = users.SingleOrDefault(u => u.Email == email);
```

### Agregación — `Count`, `Sum`, `Max`, `Min`, `Average`

```csharp
int total     = users.Count();
int activos   = users.Count(u => u.IsActive);
decimal suma  = products.Sum(p => p.Price);
decimal max   = products.Max(p => p.Price);
decimal min   = products.Min(p => p.Price);
double  avg   = products.Average(p => (double)p.Price);
```

### Existencia — `Any`, `All`

```csharp
bool hayAdmins   = users.Any(u => u.Role == "Admin");
bool todoActivos = users.All(u => u.IsActive);
bool sinUsuarios = !users.Any();  // equivalente a !users.Any()
```

### Agrupación — `GroupBy`

```csharp
var porRol = users.GroupBy(u => u.Role);

foreach (var grupo in porRol)
{
    Console.WriteLine($"Rol: {grupo.Key} → {grupo.Count()} usuarios");
    foreach (var user in grupo)
        Console.WriteLine($"  - {user.FullName}");
}
```

### Proyección plana — `SelectMany`

```csharp
// Cada usuario tiene múltiples permisos
// SelectMany "aplana" la colección de colecciones
var todosPermisos = users.SelectMany(u => u.Permisos);
```

### Paginación

```csharp
int pagina    = 2;
int tamanio   = 10;

var paginados = users
    .Skip((pagina - 1) * tamanio)  // saltar los de páginas anteriores
    .Take(tamanio);                // tomar los de esta página
```

### Convertir — `ToList`, `ToArray`, `ToDictionary`, `ToHashSet`

```csharp
// Materializar — ejecuta la query LINQ y crea la colección concreta
List<ExampleUserDto>    lista    = dtos.ToList();
ExampleUserDto[]        array    = dtos.ToArray();
HashSet<string>         emails   = users.Select(u => u.Email).ToHashSet();
Dictionary<Guid, ExampleUserDto> dict = dtos.ToDictionary(d => d.UserId, d => d);
```

### Concatenar — `Concat`, `Union`

```csharp
var todos = admins.Concat(usuarios);  // incluye duplicados
var union = admins.Union(usuarios);   // sin duplicados (usa Equals)
```

### Diferencia e intersección

```csharp
var soloEnA  = a.Except(b);    // elementos en A pero no en B
var enAmbos  = a.Intersect(b); // elementos en A y en B
```

---

## LINQ lazy vs materializado

LINQ es **lazy (diferido)** — la consulta no se ejecuta hasta que necesitas los resultados:

```csharp
// Esto NO ejecuta nada todavía:
var query = users.Where(u => u.IsActive).OrderBy(u => u.FullName);

// Esto SÍ ejecuta la consulta (materialización):
var lista  = query.ToList();         // ejecuta al hacer ToList
var primero = query.FirstOrDefault(); // ejecuta al hacer First
var count  = query.Count();           // ejecuta al hacer Count

// Cuidado — si iters dos veces, ejecuta dos veces:
foreach (var u in query) Console.WriteLine(u.FullName);  // ejecución 1
foreach (var u in query) Console.WriteLine(u.Email);     // ejecución 2

// Materializa si vas a iterar varias veces:
var lista = query.ToList();
foreach (var u in lista) Console.WriteLine(u.FullName);  // lectura de memoria
foreach (var u in lista) Console.WriteLine(u.Email);     // lectura de memoria
```

---

## IAsyncEnumerable — streaming asíncrono

Para secuencias que se obtienen elemento a elemento de forma asíncrona:

```csharp
// Definir en la interfaz
public interface IExampleUserRepository
{
    IAsyncEnumerable<ExampleUser> StreamAllAsync(CancellationToken ct = default);
}

// Implementar
public async IAsyncEnumerable<ExampleUser> StreamAllAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var user in _sql.StreamAllAsync(ct))
    {
        yield return user;
    }
}

// Consumir
await foreach (var user in repo.StreamAllAsync(ct))
{
    await ProcessUserAsync(user, ct);
}
```

---

## Colecciones en las respuestas del proyecto

```csharp
// Success con colección paginada — IReadOnlyCollection
public sealed record GetExampleUsersSuccess(
    IReadOnlyCollection<ExampleUserDto> Users,
    int Total,
    int Page,
    int PageSize)
    : GetExampleUsersResponse, ISuccess;

// En el handler — convertir IEnumerable a IReadOnlyCollection
var users = await _repo.GetPagedAsync(request.Page, request.PageSize, ct);
var dtos  = users.Select(u => new ExampleUserDto(/* ... */)).ToList();
var total = await _repo.CountAsync(ct);

return new GetExampleUsersSuccess(dtos, total, request.Page, request.PageSize);
```


---

*Rogelio Arriaga Gonzalez*
