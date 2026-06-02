# 06 — Propiedades y Campos

Los campos almacenan datos directamente en memoria. Las propiedades son una abstracción sobre los campos que permite controlar el acceso.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price) — Ch.5 Properties and Fields

---

## Campos (Fields)

Un campo es una variable que pertenece a la clase.

```csharp
public sealed class ExampleUsersSql
{
    // Campo privado — convención: guión bajo + camelCase
    private readonly MainDapperDbConnection _db;
    
    // Campo privado mutable
    private int _intentos;
    
    // Campo privado con valor inicial
    private bool _inicializado = false;
}
```

### `readonly` — solo escritura en el constructor

```csharp
public sealed class GetExampleUserHandler
{
    private readonly IExampleUserRepository _repo;
    //       ↑ readonly: se asigna en el constructor, nunca más cambia
    
    public GetExampleUserHandler(IExampleUserRepository repo)
    {
        _repo = repo;  // OK — estamos en el constructor
    }
    
    public void OtroMetodo()
    {
        // _repo = null;  // ERROR de compilación — readonly no se puede reasignar
    }
}
```

**Usa `readonly` siempre que el campo no deba cambiar después del constructor.** Es la práctica estándar para dependencias inyectadas.

### `static` — pertenece a la clase, no a la instancia

```csharp
public static class ProductMetrics
{
    // Campos estáticos — uno por toda la aplicación, no por objeto
    private static readonly Meter _meter = new Meter("back-template.products");
    public static readonly Counter<long> ProductsCreated =
        _meter.CreateCounter<long>("products_created_total");
    
    // No necesitas instanciar ProductMetrics para usar ProductsCreated:
    // ProductMetrics.ProductsCreated.Add(1);
}
```

### `const` — valor en tiempo de compilación

```csharp
public static class CorsPolicy
{
    // const — el valor se sustituye en compilación, no existe como campo en runtime
    public const string LocalhostPolicy = "LocalhostCorsPolicy";
}

// Uso:
app.UseCors(CorsPolicy.LocalhostPolicy);
```

Diferencia con `static readonly`:
- `const`: valor fijo en compilación, solo tipos primitivos y string
- `static readonly`: valor en runtime, cualquier tipo, puede inicializarse con lógica

```csharp
public static class Config
{
    public const string Version = "1.0.0";                          // OK — string literal
    public static readonly DateTime Inicio = DateTime.UtcNow;       // OK — evaluado en runtime
    // public const DateTime Inicio = DateTime.UtcNow;              // ERROR — no es constante
}
```

---

## Propiedades (Properties)

Una propiedad es acceso controlado a un valor. Puede tener lógica en lectura (`get`) y escritura (`set`).

### Auto-property — el compilador crea el campo privado

```csharp
public class ExampleUser
{
    // Auto-property — el compilador genera un campo privado oculto
    public Guid     PublicId     { get; set; }
    public string   FullName     { get; set; } = string.Empty;  // con valor inicial
    public bool     IsActive     { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}

var user = new ExampleUser();
user.FullName = "Ana García";  // set
Console.WriteLine(user.FullName);  // get → "Ana García"
```

### Propiedad con backing field — control total

```csharp
public class Producto
{
    private decimal _precio;
    
    public decimal Precio
    {
        get => _precio;
        set
        {
            if (value < 0)
                throw new ArgumentException("El precio no puede ser negativo.");
            _precio = value;
        }
    }
}

var p = new Producto();
p.Precio = 9.99m;   // OK
p.Precio = -1m;     // ArgumentException
```

### `get` solo — propiedad de solo lectura

```csharp
public class Rectangulo
{
    public double Ancho { get; set; }
    public double Alto { get; set; }
    
    // Solo get — calculado, no almacenado
    public double Area => Ancho * Alto;
    
    // Equivalente con cuerpo completo:
    public double Perimetro
    {
        get { return 2 * (Ancho + Alto); }
    }
}
```

### `set` privado — escritura solo desde la clase

```csharp
public class Pedido
{
    // Solo la clase puede asignar Estado — el cliente solo puede leer
    public string Estado { get; private set; } = "Pendiente";
    
    public void Confirmar()
    {
        Estado = "Confirmado";  // OK — estamos dentro de la clase
    }
    
    public void Cancelar()
    {
        Estado = "Cancelado";
    }
}

var pedido = new Pedido();
Console.WriteLine(pedido.Estado);  // "Pendiente"
pedido.Confirmar();
Console.WriteLine(pedido.Estado);  // "Confirmado"
// pedido.Estado = "Otro";  // ERROR — set es private
```

### `init` — solo en la inicialización del objeto

C# 9+. Permite asignar en el constructor o en el object initializer, pero no después.

```csharp
public class ConfiguracionApi
{
    public string Url   { get; init; } = string.Empty;
    public int    Puerto { get; init; } = 443;
    public bool   UsarSsl { get; init; } = true;
}

// OK — en object initializer
var config = new ConfiguracionApi
{
    Url    = "api.ejemplo.com",
    Puerto = 8080,
    UsarSsl = false
};

// ERROR — después de la inicialización
// config.Url = "otro.com";  // init no permite esto

// OK en constructor
public sealed class ServicioExterno(IConfiguration configuration)
{
    private readonly ConfiguracionApi _config = new()
    {
        Url    = configuration["Api:Url"]!,
        Puerto = configuration.GetValue<int>("Api:Puerto", 443)
    };
}
```

**En records posicionales**, todas las propiedades generadas son `{ get; init; }` automáticamente.

---

## Propiedades en records

Los records posicionales generan propiedades `{ get; init; }`:

```csharp
public sealed record ExampleUserDto(
    Guid     UserId,
    string   FullName,
    string   Email,
    bool     IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

// Equivale a:
public sealed record ExampleUserDto
{
    public Guid     UserId      { get; init; }
    public string   FullName    { get; init; }
    public string   Email       { get; init; }
    public bool     IsActive    { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    
    public ExampleUserDto(Guid userId, ...) { UserId = userId; ... }
}
```

---

## Propiedades estáticas

```csharp
public static class AppInfo
{
    public static string Version { get; } = "1.0.0";
    public static DateTime StartTime { get; } = DateTime.UtcNow;
    
    // Propiedad estática calculada
    public static TimeSpan Uptime => DateTime.UtcNow - StartTime;
}

Console.WriteLine(AppInfo.Version);  // "1.0.0"
Console.WriteLine(AppInfo.Uptime);   // tiempo desde que arrancó la app
```

---

## Propiedades requeridas — C# 11+

```csharp
public class ConfiguracionObligatoria
{
    // required — el object initializer DEBE asignar estas propiedades
    public required string Host { get; init; }
    public required string BaseDeDatos { get; init; }
    
    public int Puerto { get; init; } = 5432;  // opcional — tiene default
}

// ERROR si no asignas Host o BaseDeDatos:
// var config = new ConfiguracionObligatoria();  // falta Host y BaseDeDatos

// OK:
var config = new ConfiguracionObligatoria
{
    Host        = "localhost",
    BaseDeDatos = "back_template_dev"
};
```

---

## Propiedades calculadas vs almacenadas

```csharp
public sealed record PaginacionDto(int Page, int PageSize, int Total)
{
    // Almacenada — viene del constructor/initializer
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    
    // Calculadas — no se almacenan, se recalculan cada vez
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    public int Skip => (Page - 1) * PageSize;
}
```

**Importante:** las propiedades calculadas NO forman parte de `Equals()` ni `GetHashCode()` en los records. Solo las posicionales/almacenadas.

---

## Tabla de decisión

| Situación | Usar |
|-----------|------|
| Dependencia inyectada por constructor | `private readonly T _campo;` |
| Dato de entidad (mutable por ORM) | `public T Propiedad { get; set; }` |
| Dato de DTO/record | `public T Propiedad { get; init; }` (auto) |
| Valor calculado | `public T Calculado => expresion;` |
| Constante en tiempo de compilación | `public const string Nombre = "...";` |
| Valor inicializado una vez al arrancar | `public static readonly T Valor = new();` |
| Propiedad de config requerida | `public required T Config { get; init; }` |

---

## Errores comunes

### Error 1 — Campo público en vez de propiedad

```csharp
// ❌ Campo público — no encapsulado, no se puede agregar lógica después
public class Usuario
{
    public string Email;  // campo, no propiedad
}

// ✓ Propiedad — encapsulado, extensible
public class Usuario
{
    public string Email { get; set; } = string.Empty;
}
```

### Error 2 — Olvidar `readonly` en dependencias

```csharp
// ❌ Sin readonly — permite reasignación accidental
public sealed class GetExampleUserHandler
{
    private IExampleUserRepository _repo;  // mutable
    
    public void ReemplazarRepo(IExampleUserRepository otro)
    {
        _repo = otro;  // ¡Bug potencial! — dependencia cambiada en medio de una operación
    }
}

// ✓ Con readonly — inmutable después del constructor
public sealed class GetExampleUserHandler
{
    private readonly IExampleUserRepository _repo;  // no se puede reasignar
}
```

### Error 3 — Propiedad mutable en un record

```csharp
// ❌ Record con set mutable — rompe la promesa de inmutabilidad
public sealed record ExampleUserDto(Guid UserId)
{
    // override de la propiedad posicional — ahora es mutable
    public Guid UserId { get; set; }  // ya no es init
}

// ✓ Mantén init en records
public sealed record ExampleUserDto(Guid UserId);
// UserId es { get; init; } — solo se asigna al crear
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Campo (field) | Variable declarada directamente en la clase que almacena datos en memoria; por convención privada con `_camelCase` |
| Propiedad (property) | Abstracción controlada sobre un campo: expone getters/setters con posibilidad de añadir lógica de validación |
| `readonly` | Modificador de campo que permite asignación solo en el constructor; garantiza inmutabilidad post-construcción |
| Auto-property | Propiedad donde el compilador genera automáticamente el campo privado de respaldo: `public string Nombre { get; set; }` |
| Backing field | Campo privado explícito que una propiedad usa internamente para almacenar el valor |
| `init` | Modificador de propiedad de C# 9+ que permite asignación solo durante la construcción o en object initializers |
| `const` | Modificador para valores fijos en tiempo de compilación; solo acepta tipos primitivos y `string` |
| `static readonly` | Campo evaluado una vez al arrancar la aplicación; puede ser de cualquier tipo, a diferencia de `const` |
| Propiedad calculada | Propiedad con solo getter que computa su valor en lugar de almacenarlo: `public int Total => Items.Count` |
| `required` | Modificador de C# 11+ que obliga a asignar una propiedad en el object initializer |
| `{ get; init; }` | Tipo de propiedad generada por los records posicionales: legible desde fuera, asignable solo al crear |

---

*Rogelio Arriaga Gonzalez*
