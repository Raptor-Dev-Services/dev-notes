# 11 — Pattern Matching: is, switch, when

Pattern matching es una forma expresiva y segura de examinar el tipo y valor de un objeto. Es la base del sistema de Presenters en este proyecto.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.3 Controlling Flow: Pattern Matching

---

## ¿Por qué pattern matching?

Sin pattern matching, para distinguir tipos en tiempo de ejecución necesitabas casteos manuales:

```csharp
// ❌ Forma antigua — verbose y frágil
GetExampleUserResponse response = GetResponse();

if (response is GetExampleUserSuccess)
{
    var success = (GetExampleUserSuccess)response;  // cast explícito
    _viewModel.Set(success);
}
else if (response is GetExampleUserNotFoundFailure)
{
    var failure = (GetExampleUserNotFoundFailure)response;  // otro cast
    _viewModel.Fail(failure.Message);
}

// ✓ Con pattern matching — más limpio, el cast es implícito
if (response is ISuccess<ExampleUserDto> success)
    _viewModel.Set(success);
else if (response is INotFoundFailure notFound)
    _viewModel.Fail(notFound.Message);
else if (response is IFailure failure)
    _viewModel.Fail(failure.Message);
```

---

## `is` — verificar tipo y asignar

### Verificación simple

```csharp
object obj = "Hola mundo";

if (obj is string)
    Console.WriteLine("Es un string");

if (obj is not string)
    Console.WriteLine("No es un string");
```

### Con asignación de variable (C# 7+)

```csharp
// Verifica el tipo Y asigna la variable casteada en una sola línea
if (notification is ISuccess<ExampleUserDto> success)
{
    // "success" está disponible aquí, ya casteado a ISuccess<ExampleUserDto>
    _viewModel.Set(success);
}
```

### Con condición adicional

```csharp
if (user is ExampleUser u && u.IsActive)
{
    Console.WriteLine($"Usuario activo: {u.FullName}");
}
```

### `is null` y `is not null`

```csharp
ExampleUser? user = await repo.GetByPublicIdAsync(id, ct);

if (user is null)
    return new GetExampleUserNotFoundFailure("Usuario no encontrado.");

// Aquí el compilador sabe que user no es null (flow analysis)
return new GetExampleUserSuccess(new ExampleUserDto(user.PublicId, user.FullName, /* ... */));
```

---

## `switch` expression — múltiples casos que retornan un valor

### Forma básica

```csharp
// switch expression — retorna un valor
string nivel = tiempoMs switch
{
    < 300  => "Debug",    // menos de 300ms
    < 1000 => "Warning",  // 300–999ms
    < 2000 => "Error",    // 1000–1999ms
    _      => "Critical"  // cualquier otro (default)
};
// "_" es el patrón catch-all (equivalente al default en switch statement)
```

### Con pattern matching de tipos

```csharp
// Determinar el código HTTP según el tipo de respuesta
int statusCode = notification switch
{
    ISuccess          => 200,
    INotFoundFailure  => 404,
    IConflictFailure  => 409,
    IValidationFailure => 400,
    IUnauthorizedFailure => 401,
    IFailure          => 500,
    _                 => 500
};
```

### Con `when` — condición adicional

```csharp
string describir = user switch
{
    null                          => "No existe",
    { IsActive: false }           => "Inactivo",
    { IsActive: true, Email: "" } => "Sin email",
    { IsActive: true } u when u.FullName.Length > 50 => "Nombre muy largo",
    { IsActive: true }            => $"Activo: {user.FullName}",
    _                             => "Desconocido"
};
```

---

## Patrones disponibles

### Patrón de tipo (type pattern)

```csharp
if (obj is ExampleUser user)     // verifica tipo, asigna variable
if (obj is not ExampleUser)       // negación de tipo
```

### Patrón constante

```csharp
string diaSemana = numero switch
{
    1 => "Lunes",
    2 => "Martes",
    3 => "Miércoles",
    // ...
    7 => "Domingo",
    _ => "Inválido"
};
```

### Patrón relacional

```csharp
string categoria = precio switch
{
    <= 0           => "Inválido",
    > 0 and < 100  => "Económico",
    >= 100 and < 500 => "Medio",
    >= 500         => "Premium"
};
```

### Patrón de propiedad

```csharp
// Verifica propiedades del objeto sin asignar variable completa
string estado = user switch
{
    { IsActive: true, Email: not null and not "" } => "Completo",
    { IsActive: true }  => "Sin email",
    { IsActive: false } => "Inactivo",
    null                => "No existe"
};
```

### Patrón posicional (deconstruct)

```csharp
public sealed record Punto(double X, double Y);

string cuadrante = punto switch
{
    (> 0, > 0) => "Primer cuadrante",
    (< 0, > 0) => "Segundo cuadrante",
    (< 0, < 0) => "Tercer cuadrante",
    (> 0, < 0) => "Cuarto cuadrante",
    (0, _)     => "Eje Y",
    (_, 0)     => "Eje X",
    _          => "Origen"
};
```

### Patrón `and`, `or`, `not`

```csharp
// Combinar patrones lógicamente
string resultado = valor switch
{
    int n when n is > 0 and < 100       => "En rango",
    int n when n is < 0 or > 1000       => "Fuera de rango",
    int n when n is not 0               => "Otro número",
    0                                   => "Cero",
    _                                   => "No es int"
};

// Verificar múltiples tipos
if (obj is string or int or Guid)
    Console.WriteLine("Es un tipo primitivo soportado");
```

---

## Pattern matching en el proyecto — Presenters

El caso más importante es el Presenter, que recibe el tipo base y distingue el subtipo:

```csharp
// GetExampleUserPresenter — Variante A (con ISuccess<T>)
public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
{
    if (notification is IFailure failure)
        _viewModel.Fail(failure.Message);
    else if (notification is ISuccess<ExampleUserDto> success)
        _viewModel.Set(success);
    
    return Task.CompletedTask;
}

// GetExampleUsersPresenter — Variante B (ISuccess sin genérico)
public Task Handle(GetExampleUsersResponse notification, CancellationToken ct)
{
    if (notification is IFailure failure)
        _viewModel.Fail(failure.Message);
    else if (notification is GetExampleUsersSuccess success)
        _viewModel.OK(success);  // pasa el record completo
    
    return Task.CompletedTask;
}

// UpdateExampleUserPresenter — Variante C (éxito sin datos)
public Task Handle(UpdateExampleUserResponse notification, CancellationToken ct)
{
    if (notification is IFailure failure)
        _viewModel.Fail(failure.Message);
    else if (notification is ISuccess)
        _viewModel.OK(new { });  // objeto vacío — el éxito no tiene datos
    
    return Task.CompletedTask;
}
```

### Por qué `if/else if` y no `switch` en los presenters

Ambas formas funcionan. Se usa `if/else if` por legibilidad cuando la lógica es simple.

Con `switch` expression sería:

```csharp
public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
{
    _ = notification switch
    {
        ISuccess<ExampleUserDto> success => _viewModel.Set(success),
        INotFoundFailure notFound        => _viewModel.Fail(notFound.Message),
        IFailure failure                 => _viewModel.Fail(failure.Message),
        _                                => _viewModel.Fail("Error desconocido")
    };
    
    return Task.CompletedTask;
}
```

---

## `switch statement` vs `switch expression`

### Statement — ejecuta código, no retorna valor

```csharp
// switch statement (el clásico)
switch (environment)
{
    case "Local":
        seqUri = "http://localhost:5341";
        break;
    case "Production":
        seqUri = Environment.GetEnvironmentVariable("SEQ_URI")!;
        break;
    default:
        seqUri = "http://localhost:5341";
        break;
}
```

### Expression — evalúa y retorna un valor

```csharp
// switch expression (C# 8+) — más conciso cuando retornas un valor
string seqUri = environment switch
{
    "Local"      => "http://localhost:5341",
    "Production" => Environment.GetEnvironmentVariable("SEQ_URI")!,
    _            => "http://localhost:5341"
};
```

**Cuándo usar expression:** cuando el resultado es un valor simple (asignación, return).
**Cuándo usar statement:** cuando necesitas ejecutar múltiples líneas de código por caso.

---

## Exhaustiveness — el compilador detecta casos faltantes

```csharp
// Si todos los posibles valores son conocidos (enum, sealed hierarchy),
// el compilador avisa si falta un caso

public enum Direccion { Norte, Sur, Este, Oeste }

string flecha = direccion switch
{
    Direccion.Norte => "↑",
    Direccion.Sur   => "↓",
    Direccion.Este  => "→",
    // ❌ El compilador avisa: "Oeste" no está cubierto
    _               => "?"  // o agrega el _ para silenciar
};
```

---

## Errores comunes

### Error 1 — Orden incorrecto de casos (más específico primero)

```csharp
// ❌ IFailure es más general que INotFoundFailure
// El primer match "gana" — nunca llega a INotFoundFailure
if (notification is IFailure)
    _viewModel.Fail("Error");  // captura TODO, incluyendo NotFound
else if (notification is INotFoundFailure notFound)  // nunca llega aquí
    _viewModel.Fail(notFound.Message);

// ✓ Más específico primero
if (notification is INotFoundFailure notFound)
    _viewModel.Fail(notFound.Message);
else if (notification is IFailure failure)
    _viewModel.Fail(failure.Message);
```

### Error 2 — Olvidar el caso `null`

```csharp
// ❌ Si "notification" puede ser null, ningún patrón de tipo lo captura
// El switch no tiene rama para null → puede lanzar excepción implícita
string mensaje = notification switch
{
    ISuccess s => "Éxito",
    IFailure f => f.Message,
    // ❌ Si notification es null, lanza SwitchExpressionException
};

// ✓ Manejar null explícitamente
string mensaje = notification switch
{
    null     => "Sin respuesta",
    ISuccess => "Éxito",
    IFailure f => f.Message,
    _ => "Desconocido"
};
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Pattern matching | Conjunto de expresiones de C# para examinar el tipo, forma o valor de un objeto de forma concisa y segura |
| Type pattern | Verificación de tipo con `is TipoConcreto variable` que asigna el objeto casteado si la verificación es verdadera |
| Switch expression | Forma de switch que retorna un valor directamente; cada rama es `patrón => expresión` |
| Switch statement | Forma clásica de switch con bloques `case`; útil cuando hay efectos secundarios en las ramas |
| `when` clause | Condición adicional en un `case` o rama de switch: `case NpgsqlException ex when ex.SqlState == "23505"` |
| Property pattern | Verificación de tipo y propiedades juntas: `is ExampleUser { IsActive: true }` |
| Positional pattern | Verificación que usa el deconstructor de un record: `is (Guid id, string name)` |
| `is not null` | Patrón de nulabilidad para verificar que un valor no es null de forma explícita |
| Exhaustividad | Propiedad de un switch expression que lanza error de compilación si no se cubre todos los casos posibles |
| Presenters del proyecto | Usan pattern matching (`is ISuccess<T>`, `is INotFoundFailure`) para distinguir tipos de respuesta del mediador |
| `_` (discard) | Patrón que coincide con cualquier valor y lo descarta; usado como caso por defecto en switch expressions |

---

*Rogelio Arriaga Gonzalez*
