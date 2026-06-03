# 17: Enums en C#

Un `enum` (enumeración) es un tipo de valor que define un conjunto de constantes nombradas. Hace el código legible, evita "números mágicos" y permite al compilador verificar los valores válidos.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price). Ch.5 Building Your Own Types: Enumerations

---

## Declaración básica

```csharp
// Enum simple — los valores son int por defecto: 0, 1, 2, 3...
public enum OrderStatus
{
    Pending,    // 0
    Paid,       // 1
    Shipped,    // 2
    Delivered,  // 3
    Cancelled   // 4
}

// Uso
OrderStatus status = OrderStatus.Paid;

if (status == OrderStatus.Paid)
    Console.WriteLine("La orden está pagada.");

// Comparación exhaustiva con switch
string message = status switch
{
    OrderStatus.Pending   => "Esperando pago",
    OrderStatus.Paid      => "Pagado, listo para enviar",
    OrderStatus.Shipped   => "En camino",
    OrderStatus.Delivered => "Entregado",
    OrderStatus.Cancelled => "Cancelado",
    _                     => "Estado desconocido"
};
```

---

## Tipo subyacente explícito

```csharp
// Por defecto el tipo subyacente es int
// Se puede cambiar a byte, short, long, etc.
public enum UserRole : byte   // byte: 0-255 — suficiente para roles
{
    Guest    = 0,
    User     = 1,
    Moderator = 2,
    Admin    = 10,   // los valores pueden ser discontinuos
    SuperAdmin = 99
}

// Acceso al valor numérico:
int roleValue = (int)UserRole.Admin;   // 10
UserRole role = (UserRole)10;           // UserRole.Admin

// Verificar si un valor numérico es válido:
if (Enum.IsDefined(typeof(UserRole), 10))
    Console.WriteLine("10 es un UserRole válido.");

// Convertir desde string:
bool ok = Enum.TryParse<UserRole>("Admin", ignoreCase: true, out UserRole parsed);
// ok = true, parsed = UserRole.Admin

// Nombre desde valor:
string name = Enum.GetName(typeof(UserRole), 10);  // "Admin"
string name2 = UserRole.Admin.ToString();           // "Admin"
```

---

## Valores asignados explícitamente

```csharp
// Asignar valores específicos — útil para HTTP status codes, errores, etc.
public enum HttpStatusCode
{
    OK                  = 200,
    Created             = 201,
    NoContent           = 204,
    BadRequest          = 400,
    Unauthorized        = 401,
    Forbidden           = 403,
    NotFound            = 404,
    Conflict            = 409,
    UnprocessableEntity = 422,
    InternalServerError = 500,
}

// Útil para mapear a valores de base de datos:
public enum ProductCategory
{
    Electronics = 1,  // ID en la tabla Categories de la DB
    Clothing    = 2,
    Food        = 3,
    Sports      = 4
}
```

---

## Flags: combinación de valores con OR de bits

`[Flags]` permite combinar valores de enum usando OR (`|`). Cada valor debe ser una potencia de 2.

```csharp
[Flags]
public enum Permissions
{
    None    = 0,
    Read    = 1,   // 0001
    Write   = 2,   // 0010
    Delete  = 4,   // 0100
    Admin   = 8,   // 1000

    // Combinaciones predefinidas
    ReadWrite = Read | Write,          // 0011 = 3
    FullAccess = Read | Write | Delete  // 0111 = 7
}

// Asignar múltiples flags:
Permissions userPerm = Permissions.Read | Permissions.Write;
// userPerm = 3 (0001 | 0010 = 0011)

// Verificar si tiene un flag específico:
bool canRead  = userPerm.HasFlag(Permissions.Read);   // true
bool canDelete = userPerm.HasFlag(Permissions.Delete); // false

// Agregar un flag:
userPerm |= Permissions.Delete;  // ahora tiene Read | Write | Delete

// Quitar un flag:
userPerm &= ~Permissions.Write;  // quita Write — tiene Read | Delete

// Verificar flags combinados:
bool hasReadWrite = (userPerm & Permissions.ReadWrite) == Permissions.ReadWrite;

// ToString() con [Flags]:
Console.WriteLine(Permissions.ReadWrite.ToString());  // "Read, Write"
Console.WriteLine(userPerm.ToString());               // "Read, Delete"
```

---

## Enum en base de datos

```csharp
// Opción 1: guardar como int (eficiente, difícil de leer en la DB)
// La columna es INTEGER/SMALLINT en PostgreSQL
public enum OrderStatus { Pending = 0, Paid = 1, Shipped = 2 }

// Opción 2: guardar como string (legible, más espacio)
// La columna es VARCHAR en PostgreSQL
// En Dapper: configurar la conversión

// Opción 3: usar una tabla de referencia en la DB y un enum en C#
// La DB tiene una tabla Statuses(id, name); el enum mapea a esos IDs
public enum OrderStatus
{
    Pending = 1,  // ID en la tabla Statuses
    Paid    = 2,
    Shipped = 3
}
```

---

## Enum con métodos de extensión

Los enums no pueden tener métodos, pero las extension methods los enriquecen:

```csharp
public enum OrderStatus { Pending, Paid, Shipped, Delivered, Cancelled }

public static class OrderStatusExtensions
{
    public static string ToSpanish(this OrderStatus status) =>
        status switch
        {
            OrderStatus.Pending   => "Pendiente",
            OrderStatus.Paid      => "Pagado",
            OrderStatus.Shipped   => "Enviado",
            OrderStatus.Delivered => "Entregado",
            OrderStatus.Cancelled => "Cancelado",
            _                     => status.ToString()
        };

    public static bool IsTerminal(this OrderStatus status) =>
        status is OrderStatus.Delivered or OrderStatus.Cancelled;

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next) =>
        (current, next) switch
        {
            (OrderStatus.Pending,  OrderStatus.Paid)      => true,
            (OrderStatus.Pending,  OrderStatus.Cancelled) => true,
            (OrderStatus.Paid,     OrderStatus.Shipped)   => true,
            (OrderStatus.Paid,     OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped,  OrderStatus.Delivered) => true,
            _ => false
        };
}

// Uso:
OrderStatus status = OrderStatus.Paid;
Console.WriteLine(status.ToSpanish());  // "Pagado"
Console.WriteLine(status.IsTerminal()); // false

bool ok = status.CanTransitionTo(OrderStatus.Shipped);  // true
bool ko = status.CanTransitionTo(OrderStatus.Pending);  // false
```

---

## Iterar sobre todos los valores

```csharp
// Obtener todos los valores de un enum:
foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
    Console.WriteLine($"{(int)status}: {status}");

// Obtener todos los nombres:
string[] names = Enum.GetNames<OrderStatus>();
// ["Pending", "Paid", "Shipped", "Delivered", "Cancelled"]

// Obtener todos los valores como int:
int[] values = (int[])Enum.GetValues(typeof(OrderStatus));
```

---

## Enum en JSON (System.Text.Json)

```csharp
// Por defecto: serializa como número
// OrderStatus.Paid → 1

// Para serializar como string:
using System.Text.Json.Serialization;

public class Order
{
    public Guid Id { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; init; }
}
// JSON: { "id": "...", "status": "Paid" }

// Global en Program.cs:
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

---

## Errores comunes

### Error 1: olvidar que los enums no se validan automáticamente

```csharp
// ❌ Un int puede castearse a cualquier valor aunque no esté definido
OrderStatus status = (OrderStatus)999;  // no lanza excepción
Console.WriteLine(status);  // "999" — no es un valor definido

// ✓ Validar al recibir de fuentes externas (API, base de datos)
if (!Enum.IsDefined(typeof(OrderStatus), valueFromDb))
    throw new ArgumentOutOfRangeException($"Status inválido: {valueFromDb}");
```

### Error 2: [Flags] sin potencias de 2

```csharp
// ❌ Los valores no son potencias de 2 — los OR de bits no funcionan correctamente
[Flags]
public enum BadFlags
{
    A = 1,
    B = 2,
    C = 3,   // ← mal: 3 = A | B — colisión de bits
}

// ✓ Cada valor debe ser una potencia de 2 independiente
[Flags]
public enum GoodFlags
{
    A = 1,   // 0001
    B = 2,   // 0010
    C = 4,   // 0100
    D = 8,   // 1000
}
```

### Error 3: comparar con el número en lugar del nombre

```csharp
// ❌ Número mágico — no es claro qué significa
if (order.Status == (OrderStatus)2)  // ¿qué es 2?
    Ship(order);

// ✓ Usar el nombre siempre
if (order.Status == OrderStatus.Paid)
    Ship(order);
```

---

## Enum en este proyecto

```csharp
// Los enums son útiles para roles, estados y tipos en cualquier módulo:

// Ejemplo: estado de aprobación en un proceso de QC
public enum ApprovalStatus
{
    Pending  = 0,
    Approved = 1,
    Rejected = 2
}

// En el dominio:
public class QcInspection
{
    public ApprovalStatus Status { get; private set; } = ApprovalStatus.Pending;

    public void Approve() => Status = ApprovalStatus.Approved;
    public void Reject()  => Status = ApprovalStatus.Rejected;
}

// En la migración SQL:
// Status SMALLINT NOT NULL DEFAULT 0  ← int subyacente del enum
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Enum | Tipo de valor que define un conjunto de constantes nombradas, evitando números mágicos y mejorando la legibilidad |
| Tipo subyacente | El tipo de datos que almacena internamente el enum (`int` por defecto; también `byte`, `short`, `long`) |
| `[Flags]` | Atributo que permite combinar valores de un enum como bitflags; habilita operaciones de bit (`|`, `&`, `~`) |
| Número mágico | Anti-patrón de usar valores numéricos sin nombre en el código; los enums lo resuelven nombrando cada valor |
| `Enum.Parse<T>()` | Método para convertir un string al valor de enum correspondiente; lanza excepción si el valor no existe |
| `Enum.TryParse<T>()` | Versión segura de `Parse`: retorna `false` sin lanzar excepción si el valor no es válido |
| `ToString()` en enum | Retorna el nombre del valor: `OrderStatus.Paid.ToString()` → `"Paid"` |
| Casting de enum | Conversión entre el enum y su tipo subyacente: `(int)OrderStatus.Paid` → `1`; `(OrderStatus)1` → `Paid` |
| `switch` exhaustivo | El compilador avisa si no se cubren todos los valores posibles del enum en una switch expression |
| Serialización de enums | En APIs REST, los enums se serializan como strings con `[JsonConverter(typeof(JsonStringEnumConverter))]` |
| Discriminated union | Alternativa a los enums para jerarquías de tipos complejas; en C# se implementa con `abstract record` + subtipos |

---

*Rogelio Arriaga Gonzalez*
