# 02 — Factory Method

**Categoría:** Creacional

**Intención:** Define una interfaz para crear un objeto, pero deja que las subclases decidan qué clase instanciar. El Factory Method delega la instanciación a las subclases.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.3 Creational Patterns: Factory Method

---

## El problema

Tienes un sistema de logística que inicialmente solo usa camiones. Toda la lógica de planificación está en una clase `Logistics`. Más tarde necesitas soporte para barcos. El código está acoplado a `Truck` — agregar `Ship` requiere cambiar toda la clase.

```csharp
// ❌ Sin Factory Method — acoplado a la clase concreta
public class Logistics
{
    public void PlanDelivery()
    {
        var truck = new Truck();  // ← acoplado a Truck
        truck.Deliver();
    }
}
// Agregar Ship requiere modificar Logistics — violación del Open/Closed Principle
```

---

## Analogía

Una franquicia de comida rápida. La receta general (el algoritmo) está definida por la empresa central. Cada sucursal (subclase) decide qué proveedor local de ingredientes usar (qué producto crear). El proceso de preparación es el mismo — los ingredientes son específicos a cada sucursal.

---

## Estructura

```
Creator (abstract)
├── FactoryMethod(): IProduct    ← abstract — la subclase lo implementa
└── SomeOperation()              ← usa FactoryMethod() — no sabe qué producto se crea

ConcreteCreator1 : Creator
└── FactoryMethod() → new ConcreteProduct1()

ConcreteCreator2 : Creator
└── FactoryMethod() → new ConcreteProduct2()

IProduct (interface)
ConcreteProduct1 : IProduct
ConcreteProduct2 : IProduct
```

---

## Código del ejemplo conceptual

```csharp
// El Creator declara el factory method que retorna IProduct
abstract class Creator
{
    // Las subclases deben implementar esto
    public abstract IProduct FactoryMethod();

    // Este método usa el producto sin saber cuál es
    public string SomeOperation()
    {
        var product = FactoryMethod();  // ← delega la creación
        return "Creator: working with " + product.Operation();
    }
}

// Subclase 1 — crea ConcreteProduct1
class ConcreteCreator1 : Creator
{
    public override IProduct FactoryMethod()
    {
        return new ConcreteProduct1();
    }
}

// Subclase 2 — crea ConcreteProduct2
class ConcreteCreator2 : Creator
{
    public override IProduct FactoryMethod()
    {
        return new ConcreteProduct2();
    }
}

public interface IProduct
{
    string Operation();
}

class ConcreteProduct1 : IProduct
{
    public string Operation() => "{Result of ConcreteProduct1}";
}

class ConcreteProduct2 : IProduct
{
    public string Operation() => "{Result of ConcreteProduct2}";
}

// El cliente trabaja con Creator — no sabe qué producto se crea
void ClientCode(Creator creator)
{
    Console.WriteLine(creator.SomeOperation());
}

// Uso:
ClientCode(new ConcreteCreator1());  // trabaja con Product1
ClientCode(new ConcreteCreator2());  // trabaja con Product2
```

---

## Ejemplo real: Logística

```csharp
// Problema resuelto con Factory Method:
public interface ITransport
{
    void Deliver();
}

public class Truck : ITransport
{
    public void Deliver() => Console.WriteLine("Entrega por carretera en caja");
}

public class Ship : ITransport
{
    public void Deliver() => Console.WriteLine("Entrega marítima en contenedor");
}

// Creator — sin cambiar este código se soportan nuevos transportes
public abstract class Logistics
{
    // Factory Method — cada subclase decide el transporte
    protected abstract ITransport CreateTransport();

    public void PlanDelivery()
    {
        var transport = CreateTransport();  // no sabe qué transporte
        transport.Deliver();
    }
}

public class RoadLogistics : Logistics
{
    protected override ITransport CreateTransport() => new Truck();
}

public class SeaLogistics : Logistics
{
    protected override ITransport CreateTransport() => new Ship();
}

// Agregar AirLogistics no modifica la clase base — solo agrega una subclase nueva
```

---

## En este proyecto

```csharp
// Infrastructure/PostgreSql/MainDbConnectionFactory.cs
// El factory method es OpenConnection() — los detalles de creación de NpgsqlConnection
// están encapsulados aquí, no dispersos por el código.

public sealed class MainDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public MainDbConnectionFactory(IConfiguration config)
    {
        var connStr = config.GetConnectionString("MainDbConnection")
            ?? throw new InvalidOperationException("MainDbConnection no configurado.");
        _dataSource = NpgsqlDataSource.Create(connStr);
    }

    // Factory method: crea y retorna una conexión lista para usar
    public NpgsqlConnection OpenConnection()
    {
        return _dataSource.OpenConnection();
    }
}

// MainDapperDbConnection usa la factory:
public sealed class MainDapperDbConnection
{
    private readonly MainDbConnectionFactory _factory;

    public Task<T?> QuerySingleAsync<T>(string sql, object? param, ...) =>
        ExecuteWithConnection(conn => conn.QuerySingleOrDefaultAsync<T>(sql, param, ...));

    private async Task<T> ExecuteWithConnection<T>(Func<IDbConnection, Task<T>> action)
    {
        using var conn = _factory.OpenConnection();  // ← factory method
        return await action(conn);
    }
}
```

---

## Factory Method vs `new` directo

| Aspecto | `new` directo | Factory Method |
|---------|--------------|----------------|
| Acoplamiento | Acoplado a la clase concreta | Acoplado a la interfaz |
| Extensibilidad | Requiere modificar el código existente | Solo agregar subclase |
| Testing | Difícil de mockear | Fácil — reemplazar factory |
| Complejidad | Mínima | Agrega una clase |

---

## Cuándo usar

- Cuando no sabes de antemano qué tipo exacto de objeto necesitas crear.
- Cuando quieres que las subclases especifiquen los objetos que crean.
- Cuando quieres reutilizar objetos existentes en lugar de crear nuevos (pooling).
- Cuando el código de creación es complejo y quieres encapsularlo.

## Cuándo NO usar

- Para creaciones simples con `new` — no agregar complejidad innecesaria.
- Cuando solo existe un tipo de producto — no hay variabilidad que justifique el patrón.
- Cuando la creación no cambia nunca — no hay extensibilidad que ganar.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Factory Method | Patrón creacional que define una interfaz para crear objetos pero deja a las subclases decidir qué clase instanciar |
| Creator | Clase base abstracta que declara el factory method y contiene el algoritmo que usa el producto |
| ConcreteCreator | Subclase que sobreescribe el factory method para retornar un tipo concreto de producto |
| IProduct | Interfaz común que todos los productos creados por el factory method deben implementar |
| Acoplamiento | Grado de dependencia entre clases; un acoplamiento alto dificulta el mantenimiento y el testing |
| Open/Closed Principle | Principio que establece que el código debe estar abierto para extensión pero cerrado para modificación |
| Delegación | Técnica en la que una clase cede parte de su responsabilidad a otra clase u objeto |
| Instanciación | Proceso de crear un objeto concreto a partir de una clase en tiempo de ejecución |
| `MainDbConnectionFactory` | Clase del proyecto que encapsula la creación de conexiones Npgsql mediante un factory method (`OpenConnection()`) |
| Encapsulamiento | Principio de ocultar los detalles de implementación dentro de una clase y exponer solo una interfaz pública |
| Polimorfismo | Capacidad de un mismo método o interfaz de comportarse de manera diferente según el tipo concreto en tiempo de ejecución |
| Object pool | Patrón que reutiliza objetos costosos de crear en lugar de instanciar nuevos cada vez |

---

*Rogelio Arriaga Gonzalez*
