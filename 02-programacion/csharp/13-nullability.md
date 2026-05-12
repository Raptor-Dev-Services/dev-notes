# 13 — Nulabilidad en C#

El valor `null` representa "sin valor". Manejarlo incorrectamente es la causa #1 de crashes en aplicaciones .NET: `NullReferenceException`.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.2 Speaking C#: Null and Nullable Reference Types

---

## El problema

```csharp
ExampleUser user = null;
Console.WriteLine(user.FullName); // ¡CRASH! NullReferenceException
```

C# 8+ introdujo **Nullable Reference Types** — el compilador te avisa antes de que el crash ocurra.

---

## Tipos nullable vs non-nullable

### Tipos de referencia (clases, records, strings, interfaces)

```csharp
// Non-nullable — el compilador asume que NUNCA es null
string nombre = "Ana";     // siempre tiene valor
ExampleUser user = new();  // siempre tiene valor

// Nullable — puede ser null, debes verificar antes de usar
string? nombreOpcional = null;     // puede ser null
ExampleUser? userONull = null;     // puede ser null
```

### Tipos de valor (int, bool, DateTime, struct, Guid)

Por defecto NO pueden ser null (son tipos de valor — viven en el stack):

```csharp
int numero = 0;    // nunca es null — 0 es un valor válido de int
bool activo = false; // nunca es null

// Para hacerlos nullable:
int?      numeroOpcional = null;
bool?     activoOpcional = null;
DateTime? fechaOpcional  = null;
Guid?     idOpcional     = null;
```

---

## Operadores de nulabilidad

### `?` en el tipo — permite null

```csharp
ExampleUser? user = await repo.GetByPublicIdAsync(id, ct);
// user PUEDE ser null — el método retorna null si no existe
```

### `?.` — acceso seguro (null-conditional)

```csharp
// Sin operador — puede explotar
string nombre = user.FullName;  // NullReferenceException si user es null

// Con ?. — retorna null si user es null, en vez de explotar
string? nombre = user?.FullName;

// Encadenar
string? ciudad = persona?.Direccion?.Ciudad?.ToUpper();
// Si persona o Direccion o Ciudad es null → retorna null sin excepción
```

### `??` — operador null-coalescing (valor por defecto)

```csharp
// Si la izquierda es null, usa la derecha
string nombre = user?.FullName ?? "Anónimo";
// user?.FullName puede ser null si user es null
// en ese caso usa "Anónimo"

// Encadenar ?? para múltiples fallbacks
string config = 
    Environment.GetEnvironmentVariable("CONN_STR") 
    ?? configuration["ConnectionStrings:MainDbConnection"]
    ?? throw new InvalidOperationException("No hay connection string configurada.");
```

### `??=` — asignar si es null (C# 8+)

```csharp
// Si la variable es null, asignarle el valor
_cache ??= new Dictionary<Guid, ExampleUser>();
// equivale a: if (_cache == null) _cache = new Dictionary<Guid, ExampleUser>();

string nombre = null!;
nombre ??= "Valor default";  // nombre = "Valor default"
```

### `!` — null-forgiving operator (suprimir advertencia)

Le dices al compilador "confía en mí, esto no es null":

```csharp
// User.FindFirstValue puede retornar null, pero en este contexto sabemos que no
var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
//                                                                         ↑ "no es null"

// Configuración que sabemos que existe
var jwtKey = configuration["Jwt:Key"]!;
// Sin ! el compilador avisa: "Possible null reference"
```

**Úsalo solo cuando REALMENTE sabes que no es null.** Si lo usas en algo que SÍ puede ser null, el compilador no te avisará y tendrás un crash en runtime.

---

## Flow analysis — el compilador rastrea nullabilidad

```csharp
ExampleUser? user = await repo.GetByPublicIdAsync(id, ct);

// Aquí: user puede ser null — el compilador lo sabe

if (user is null)
    return new GetExampleUserNotFoundFailure("Usuario no encontrado.");

// Aquí: el compilador sabe que user NO es null (porque si fuera null ya retornamos)
// No necesitas user! ni verificar de nuevo
return new GetExampleUserSuccess(new ExampleUserDto(
    user.PublicId,      // ✓ seguro — el compilador sabe que user no es null
    user.FullName,      // ✓ seguro
    user.Email,         // ✓ seguro
    user.IsActive,
    user.CreatedAtUtc,
    user.UpdatedAtUtc));
```

### Guardias de null

```csharp
// Guard clause — retornar early si es null
public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
    
    // Guard: si user es null, retornar error early
    if (user is null)
        return new GetExampleUserNotFoundFailure("Usuario no encontrado.");
    
    // De aquí en adelante, user no es null — sin necesidad de ?. o checks adicionales
    return new GetExampleUserSuccess(new ExampleUserDto(
        user.PublicId, user.FullName, user.Email,
        user.IsActive, user.CreatedAtUtc, user.UpdatedAtUtc));
}
```

---

## Patrones comunes en el proyecto

### TryGet — retornar null si no existe

```csharp
// Convención: métodos que pueden no encontrar resultado retornan T?
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);
    // ↑ T? = puede retornar null si no se encuentra
}
```

### Validar argumentos

```csharp
// Validar en el constructor (fail fast)
public JwtTokenService(IConfiguration configuration)
{
    _key = configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key no configurado.");
    //   ↑ ?? throw = si es null, lanzar excepción inmediatamente con mensaje claro
}
```

### Configuración obligatoria

```csharp
// En lugar de verificar null cada vez, hacer throw en la construcción
private readonly string _key;
private readonly string _issuer;

public JwtTokenService(IConfiguration config)
{
    _key    = config["Jwt:Key"]    ?? throw new InvalidOperationException("Jwt:Key no configurado.");
    _issuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer no configurado.");
}

// Ahora _key y _issuer NUNCA son null — sin necesidad de verificar después
```

---

## Nullable en colecciones

```csharp
// Lista que puede ser null
List<ExampleUser>? listaONull = null;

// Lista que no puede ser null, pero puede tener elementos nulos
List<ExampleUser?> listaConNulos = new() { user1, null, user3 };

// Lista que no puede ser null y sus elementos tampoco
List<ExampleUser> listaSolida = new() { user1, user2, user3 };

// Iteración segura con ?. y ??
foreach (var user in listaConNulos)
{
    string nombre = user?.FullName ?? "Desconocido";
    Console.WriteLine(nombre);
}
```

---

## Activar nullable en el proyecto

En el `.csproj`:

```xml
<PropertyGroup>
    <Nullable>enable</Nullable>
    <!-- Convierte advertencias de null en errores: -->
    <WarningsAsErrors>nullable</WarningsAsErrors>
</PropertyGroup>
```

Con `enable`, el compilador:
- Asume que todos los tipos de referencia son non-nullable por defecto
- Avisa si asignas null a uno non-nullable
- Avisa si usas un nullable sin verificar

---

## Errores comunes

### Error 1 — Ignorar la advertencia de nullable

```csharp
// ❌ Ignorar el warning — puede crashear en producción
ExampleUser? user = await repo.GetByPublicIdAsync(id, ct);
Console.WriteLine(user.FullName);  // Warning: Dereference of a possibly null reference
// Si user es null → NullReferenceException en producción

// ✓ Verificar primero
if (user is null) return NotFound();
Console.WriteLine(user.FullName);  // ✓ seguro aquí
```

### Error 2 — Abusar del `!` para silenciar warnings

```csharp
// ❌ Silenciar todos los warnings con !
ExampleUser? user = await repo.GetByPublicIdAsync(id, ct);
Console.WriteLine(user!.FullName);  // "confío en que no es null"
// Si user SÍ es null → NullReferenceException sin warning

// ✓ Manejar correctamente
if (user is null) return new GetExampleUserNotFoundFailure("...");
Console.WriteLine(user.FullName);  // ✓
```

### Error 3 — string vacío vs null

```csharp
// Ambos son "sin valor" pero son distintos
string? conNull = null;
string vacia = "";

// string.IsNullOrEmpty verifica ambos
if (string.IsNullOrEmpty(email))
    return new RegistroValidationFailure("Email es requerido.");

// string.IsNullOrWhiteSpace verifica null, vacío y solo espacios
if (string.IsNullOrWhiteSpace(nombre))
    return new RegistroValidationFailure("Nombre es requerido.");
```

### Error 4 — Nullable value types

```csharp
// int? tiene un valor bool HasValue y acceso por .Value
int? numero = null;

// ❌ Acceder a Value sin verificar
int resultado = numero.Value;  // InvalidOperationException si es null

// ✓ Verificar primero
if (numero.HasValue)
    int resultado = numero.Value;

// ✓ O usar ?? para default
int resultado = numero ?? 0;

// ✓ O con pattern matching
if (numero is int n)
    Console.WriteLine(n);
```


---

*Rogelio Arriaga Gonzalez*
