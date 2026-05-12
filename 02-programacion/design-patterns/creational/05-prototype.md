# 05 — Prototype

**Categoría:** Creacional

**Intención:** Permite copiar objetos existentes sin que el código dependa de sus clases.

---

## El problema

Necesitas una copia exacta de un objeto. El problema: no siempre conoces la clase concreta del objeto (solo tienes una referencia a la interfaz), y los campos del objeto pueden ser privados.

```csharp
// ❌ Problema: campos privados, no sabes la clase concreta
IAnimal animal = GetAnimalFromSomewhere();  // puede ser Dog, Cat, Wolf...
// No puedes hacer: var copy = new ??? (animal.GetType())
// No puedes acceder a los campos privados para copiarlos manualmente
```

---

## Analogía

La división celular. Una célula se copia a sí misma — no necesita un "constructor externo" que sepa cómo crear una célula. La célula sabe cómo duplicarse. El resultado es una copia con el mismo material genético.

---

## Estructura

```
IPrototype (o ICloneable)
└── Clone(): IPrototype

ConcretePrototype : IPrototype
├── ShallowCopy(): ConcretePrototype   ← copia de campo en campo (referencias compartidas)
└── DeepCopy(): ConcretePrototype      ← copia recursiva (referencias nuevas)
```

---

## Código del ejemplo conceptual

```csharp
public class Person
{
    public int Age;
    public DateTime BirthDate;
    public string Name;
    public IdInfo IdInfo;  // objeto de referencia — clave para entender shallow vs deep

    // Copia superficial — MemberwiseClone copia campo a campo
    // Los campos de tipo valor (int, DateTime) se copian por valor
    // Los campos de referencia (string*, objetos) comparten la misma referencia
    public Person ShallowCopy()
    {
        return (Person) this.MemberwiseClone();
    }

    // Copia profunda — todos los objetos de referencia se crean nuevos
    public Person DeepCopy()
    {
        Person clone = (Person) this.MemberwiseClone();
        clone.IdInfo = new IdInfo(IdInfo.IdNumber);  // nuevo objeto IdInfo — no compartido
        clone.Name   = string.Copy(Name);            // nueva instancia de string
        return clone;
    }
}

public class IdInfo
{
    public int IdNumber;
    public IdInfo(int idNumber) { IdNumber = idNumber; }
}

// Demostración de la diferencia:
Person p1 = new Person { Age = 42, Name = "Jack", IdInfo = new IdInfo(666) };

Person p2 = p1.ShallowCopy();  // copia superficial
Person p3 = p1.DeepCopy();     // copia profunda

// Modificar p1:
p1.Age = 32;
p1.IdInfo.IdNumber = 7878;

Console.WriteLine(p2.Age);           // 42 → ✓ int se copió por valor
Console.WriteLine(p2.IdInfo.IdNumber); // 7878 → ❌ comparten el mismo IdInfo
Console.WriteLine(p3.IdInfo.IdNumber); // 666  → ✓ IdInfo independiente
```

**Conclusión:**
- **Shallow copy:** campos de valor copiados, campos de referencia **compartidos**.
- **Deep copy:** todo independiente — modificar el original no afecta la copia.

---

## Shallow vs Deep — tabla comparativa

| | Shallow Copy | Deep Copy |
|--|-------------|-----------|
| `int`, `double`, `bool`, `struct` | Copia el valor | Copia el valor |
| `string` | Comparte referencia (pero string es inmutable → seguro) | Nueva instancia |
| Objetos de referencia (clases) | **Comparte referencia** | **Nuevo objeto** |
| Arrays | Comparte el array | Nuevo array con nuevos elementos |
| Velocidad | Más rápido | Más lento (recursivo) |
| Riesgo | Modificar sub-objetos afecta ambas copias | Completamente independiente |

---

## Prototype en C# moderno — `ICloneable` y records

### `ICloneable` — interfaz estándar

```csharp
public class Config : ICloneable
{
    public string Host { get; set; }
    public int Port { get; set; }
    public DatabaseConfig Database { get; set; }

    public object Clone()
    {
        return new Config
        {
            Host = this.Host,
            Port = this.Port,
            Database = (DatabaseConfig) this.Database.Clone()  // copia profunda
        };
    }
}
```

### Records — Prototype con `with`

En C# moderno con records, el Prototype se expresa naturalmente con la expresión `with`:

```csharp
// Record — inmutable por defecto
public sealed record ExampleUserDto(
    Guid PublicId,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

// Prototype: crear copia modificada sin constructor explícito
var original = new ExampleUserDto(
    Guid.NewGuid(), "Ana García", "ana@test.com", true,
    DateTime.UtcNow, DateTime.UtcNow);

// Prototype: copia con FullName diferente — resto idéntico
var updated = original with { FullName = "Ana García-López" };
// original sigue sin cambios
// updated es una NUEVA instancia con todos los campos del original excepto FullName
```

**Los records en este proyecto SON el Prototype pattern** — el `with` expression genera una copia con los campos modificados.

---

## Prototype Registry — catálogo de prototipos

```csharp
// Registry: almacena prototipos nombrados para clonar bajo demanda
public class PrototypeRegistry
{
    private readonly Dictionary<string, IPrototype> _items = new();

    public void AddPrototype(string key, IPrototype prototype)
    {
        _items[key] = prototype;
    }

    public IPrototype Create(string key)
    {
        return _items[key].Clone();  // siempre devuelve una copia, nunca el original
    }
}

// Uso:
var registry = new PrototypeRegistry();
registry.AddPrototype("basic-user", new UserProfile { Role = "user", IsActive = true });
registry.AddPrototype("admin-user", new UserProfile { Role = "admin", IsActive = true });

// Clonar un prototipo registrado
var newUser  = registry.Create("basic-user");
var newAdmin = registry.Create("admin-user");
```

---

## En este proyecto

```csharp
// El `with` de records es Prototype en su forma más idiomática:
public sealed record GetExampleUsersSuccess(
    IReadOnlyCollection<ExampleUserDto> Users, int Total, int Page, int PageSize)
    : GetExampleUsersResponse, ISuccess;

// Modificar una respuesta preservando el resto:
var original = new GetExampleUsersSuccess(users, 100, 1, 20);
var nextPage  = original with { Page = 2 };  // copia del Prototype con Page diferente
```

---

## Cuándo usar

- Cuando crear un objeto desde cero es costoso (requiere operaciones de I/O, cálculos, etc.) y tienes un objeto existente que es una "plantilla".
- Cuando quieres reducir el número de subclases — copiar y modificar en lugar de heredar.
- Cuando el código no debería depender de las clases concretas de los objetos a copiar.
- En C# moderno: cuando usas records con `with` — es Prototype implícito.

## Cuándo NO usar

- Para objetos simples — `new MyObject()` es más claro.
- Cuando los objetos tienen recursos no copiables (conexiones, hilos).
- Cuando la semántica de "copia" no está clara (¿qué significa copiar una transacción activa?).


---

*Rogelio Arriaga Gonzalez*
