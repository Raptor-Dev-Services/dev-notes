# 04 — Records en C#

Los `record` son tipos diseñados para datos inmutables. Son la base de todos los DTOs, Requests y Responses de este proyecto.

---

## ¿Por qué records?

### El problema con las clases para datos

```csharp
// Con clase normal — comparación por referencia (identidad)
public class PuntoClass { public int X { get; set; } public int Y { get; set; } }

var a = new PuntoClass { X = 1, Y = 2 };
var b = new PuntoClass { X = 1, Y = 2 };

Console.WriteLine(a == b);    // false — son objetos distintos en memoria
Console.WriteLine(a.Equals(b)); // false — misma historia
```

Esto es problemático para DTOs y mensajes: dos respuestas con los mismos datos deberían ser iguales.

### Records — comparación por valor

```csharp
public record PuntoRecord(int X, int Y);

var a = new PuntoRecord(1, 2);
var b = new PuntoRecord(1, 2);

Console.WriteLine(a == b);     // true — mismos valores = iguales
Console.WriteLine(a.Equals(b)); // true
Console.WriteLine(a);          // "PuntoRecord { X = 1, Y = 2 }" — ToString descriptivo
```

El compilador genera automáticamente:
- `Equals()`
- `GetHashCode()`
- `==` y `!=`
- `ToString()` descriptivo
- Constructor posicional
- `Deconstruct()`

---

## Formas de declarar un record

### Record posicional (el más usado en este proyecto)

Los parámetros van en la declaración. El compilador genera todo.

```csharp
// Declaración
public sealed record ExampleUserDto(
    Guid     UserId,
    string   FullName,
    string   Email,
    bool     IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

// Uso
var dto = new ExampleUserDto(
    UserId:      user.PublicId,
    FullName:    user.FullName,
    Email:       user.Email,
    IsActive:    user.IsActive,
    CreatedAtUtc: user.CreatedAtUtc,
    UpdatedAtUtc: user.UpdatedAtUtc);

Console.WriteLine(dto.FullName);    // acceso por propiedad
Console.WriteLine(dto.CreatedAtUtc); // auto-generada como {get; init;}
```

**Lo que genera el compilador para el record posicional:**

```csharp
// Equivalente expandido (lo que realmente compila):
public sealed record ExampleUserDto
{
    public Guid     UserId      { get; init; }
    public string   FullName    { get; init; }
    public string   Email       { get; init; }
    public bool     IsActive    { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    
    public ExampleUserDto(Guid userId, string fullName, string email,
        bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        UserId = userId; FullName = fullName; Email = email;
        IsActive = isActive; CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc;
    }
    
    // Deconstructor generado:
    public void Deconstruct(out Guid userId, out string fullName, /* ... */) { }
    
    // Equals/GetHashCode/ToString generados automáticamente
}
```

### Record con propiedades explícitas

Cuando necesitas más control sobre las propiedades:

```csharp
public sealed record ProductConfig
{
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    
    // Propiedad calculada — no forma parte de Equals/GetHashCode
    public bool IsAvailable => Stock > 0 && Price > 0;
}

var config = new ProductConfig
{
    Name  = "Widget",
    Price = 9.99m,
    Stock = 100
};
```

### Mezcla — posicional + propiedades adicionales

```csharp
public sealed record GetExampleUsersSuccess(
    IReadOnlyCollection<ExampleUserDto> Users,
    int Total,
    int Page,
    int PageSize)
    : GetExampleUsersResponse, ISuccess
{
    // Propiedad calculada — no en el constructor posicional
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

---

## `abstract record` — base sin instanciar

```csharp
// La "familia" de respuestas para GetExampleUser
public abstract record GetExampleUserResponse : IResponse;
// No se puede instanciar directamente
// Permite tratar todos los subtipos polimórficamente

public sealed record GetExampleUserSuccess(ExampleUserDto Data)
    : GetExampleUserResponse, ISuccess<ExampleUserDto>;

public sealed record GetExampleUserNotFoundFailure(string Message)
    : GetExampleUserResponse, INotFoundFailure;

// En el presenter:
public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
{
    // Recibe el tipo base — distingue con pattern matching
    if (notification is ISuccess<ExampleUserDto> success)
        _viewModel.Set(success);
    else if (notification is INotFoundFailure notFound)
        _viewModel.Fail(notFound.Message);
    else if (notification is IFailure failure)
        _viewModel.Fail(failure.Message);
    
    return Task.CompletedTask;
}
```

---

## La expresión `with` — copia con modificaciones

Records son inmutables. Para "modificar" uno, creas una copia con los cambios:

```csharp
var original = new ExampleUserDto(
    UserId:      Guid.NewGuid(),
    FullName:    "Ana García",
    Email:       "ana@ejemplo.com",
    IsActive:    true,
    CreatedAtUtc: DateTime.UtcNow.AddDays(-30),
    UpdatedAtUtc: DateTime.UtcNow.AddDays(-30));

// Crear copia con solo Email diferente — el resto igual
var actualizado = original with { Email = "ana.nueva@ejemplo.com", UpdatedAtUtc = DateTime.UtcNow };

Console.WriteLine(original.Email);    // "ana@ejemplo.com"   — original sin cambios
Console.WriteLine(actualizado.Email); // "ana.nueva@ejemplo.com" — copia modificada

// Son objetos distintos
Console.WriteLine(object.ReferenceEquals(original, actualizado)); // false
```

**Cuándo usar `with`:**
- En tests — crear variantes de un objeto base
- En mappers — transformar datos cambiando pocos campos
- En handlers — actualizar estado sin mutar el original

---

## Deconstrucción

Los records posicionales tienen `Deconstruct` automático:

```csharp
public sealed record Coordenada(double Lat, double Lon);

var punto = new Coordenada(19.4326, -99.1332);

// Deconstrucción
var (lat, lon) = punto;
Console.WriteLine($"Latitud: {lat}, Longitud: {lon}");

// Útil en foreach:
var puntos = new[] { new Coordenada(1, 2), new Coordenada(3, 4) };
foreach (var (lat, lon) in puntos)
{
    Console.WriteLine($"{lat}, {lon}");
}
```

---

## `record struct` — record como tipo de valor

Los records normales son tipos de referencia (como clases). `record struct` es un tipo de valor (como `int`, `DateTime`).

```csharp
// record struct — vive en el stack, no en el heap
public readonly record struct Dinero(decimal Cantidad, string Moneda);

var precio = new Dinero(9.99m, "MXN");
var precio2 = precio;  // COPIA del valor, no la misma referencia

precio2 = precio2 with { Cantidad = 15.00m };

Console.WriteLine(precio.Cantidad);  // 9.99 — el original no cambió
Console.WriteLine(precio2.Cantidad); // 15.00
```

**Cuándo usar `record struct`:**
- Datos pequeños (2-4 campos)
- Usados en colecciones grandes (evita presión en el GC)
- Representan valores, no identidades (Dinero, Coordenada, Rango de fechas)

**No usar para:**
- Objetos con muchos campos
- Datos que se pasan por referencia frecuentemente
- DTOs en el API (usa record class normal)

---

## Herencia en records

```csharp
// Cadena de herencia típica del proyecto:
//
// IResponse (interface)
//     ↑
// GetExampleUserResponse (abstract record)
//     ↑               ↑
// GetExampleUserSuccess  GetExampleUserNotFoundFailure
// (sealed record)        (sealed record)

// La herencia de records sigue las mismas reglas que las clases:
// - Un record puede heredar de un record (no de una clase)
// - sealed = no se puede heredar de él
// - abstract = no se puede instanciar

public abstract record AnimalRecord(string Nombre);  // base
public sealed record PerroRecord(string Nombre, string Raza) : AnimalRecord(Nombre);
public sealed record GatoRecord(string Nombre, bool EsIndoor) : AnimalRecord(Nombre);
```

**Regla del proyecto:** todas las respuestas (`*Response`) son `abstract record`, todas las variantes (`*Success`, `*Failure`) son `sealed record`.

---

## Records vs Clases — tabla de decisión completa

| Característica | `class` | `record` |
|----------------|---------|----------|
| Comparación por defecto | Referencia (identidad) | Valor (contenido) |
| Inmutabilidad | Manual (`readonly`, `init`) | Por diseño (`init` en posicionales) |
| `ToString()` | `NombreClase` | `NombreClase { Prop1 = val1, ... }` |
| `Equals()` | Referencia | Todos los campos/props |
| `with` expression | No | ✓ |
| `Deconstruct` | Manual | Automático (posicionales) |
| Herencia | Clases y abstract | Solo records |
| Mutabilidad | Configurable | Posible pero desaconsejado |
| Uso típico | Servicios, repositorios, handlers | DTOs, Requests, Responses |

---

## Cuándo NO usar record

### 1. Cuando el objeto tiene estado mutable

```csharp
// ❌ Un repositorio NO es un record — tiene estado (la conexión) que cambia
public sealed record ExampleUserRepository(ExampleUsersSql sql) : IExampleUserRepository
{
    // El record se compara por valor — dos repos con la misma sql serían "iguales"
    // Eso no tiene sentido para un repositorio
}

// ✓ Clase normal
public sealed class ExampleUserRepository : IExampleUserRepository { }
```

### 2. Cuando necesitas la misma instancia (identidad)

```csharp
// ❌ Si dos peticiones crean el mismo GetExampleUserRequest, son "iguales"
// como records — pero podrían ser peticiones distintas del mismo usuario
// (en algunos casos quieres distinguirlas)
```

### 3. Respuestas de colección con `ISuccess` genérico

**IMPORTANTE — trampa del proyecto:**

```csharp
// ❌ NO implementar ISuccess<TSelf> en un record con Data => this
// Crea una referencia circular en la serialización JSON
public sealed record GetExampleUsersSuccess(...)
    : GetExampleUsersResponse, ISuccess<GetExampleUsersSuccess>
{
    public GetExampleUsersSuccess Data => this;  // ← CIRCULAR — Data.Data.Data...
}

// ✓ Implementar ISuccess (sin genérico) cuando el éxito contiene múltiples campos
public sealed record GetExampleUsersSuccess(
    IReadOnlyCollection<ExampleUserDto> Users, int Total, int Page, int PageSize)
    : GetExampleUsersResponse, ISuccess;  // ISuccess sin <T>
// En el presenter: _viewModel.OK(success)  (no _viewModel.Set(success))
```

---

## Records en cada capa del proyecto

### Domain — Entidades (clase, no record)

```csharp
// Las entidades de dominio son clases porque Dapper las mapea por propiedades mutables
public class ExampleUser
{
    public int      Id           { get; set; }
    public Guid     PublicId     { get; set; }
    public string   FullName     { get; set; } = string.Empty;
    public string   Email        { get; set; } = string.Empty;
    public bool     IsActive     { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
```

### Application — Request, Response, DTO (todos records)

```csharp
// Request — immutable message
public sealed record GetExampleUserRequest(Guid PublicId) : IRequest<GetExampleUserResponse>;

// Response base — abstract
public abstract record GetExampleUserResponse : IResponse;

// Éxito — sealed con datos
public sealed record GetExampleUserSuccess(ExampleUserDto Data)
    : GetExampleUserResponse, ISuccess<ExampleUserDto>;

// Fallo — sealed con mensaje
public sealed record GetExampleUserNotFoundFailure(string Message)
    : GetExampleUserResponse, INotFoundFailure;

// DTO — transferencia de datos
public sealed record ExampleUserDto(
    Guid UserId, string FullName, string Email,
    bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
```


---

*Rogelio Arriaga Gonzalez*
