# 08 — Composite

**Categoría:** Estructural

**Intención:** Permite componer objetos en estructuras de árbol para representar jerarquías parte-todo. El Composite permite a los clientes tratar objetos individuales y composiciones de objetos de manera uniforme.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.4 Structural Patterns: Composite

---

## El problema

Tienes una app de gestión de tareas. Una tarea puede ser simple o puede ser un grupo de tareas (proyecto). Quieres calcular el tiempo total de todas las tareas de un proyecto, incluyendo sub-proyectos. Con clases separadas para "tarea simple" y "grupo de tareas", el cliente necesita verificar el tipo en cada operación:

```csharp
// ❌ Sin Composite — el cliente tiene que distinguir los tipos
public void CalculateTime(object item)
{
    if (item is SimpleTask task)
        total += task.Hours;
    else if (item is TaskGroup group)
        foreach (var child in group.Children)
            CalculateTime(child);  // recursión manual
}
```

---

## Analogía

El sistema de archivos. Un archivo es hoja, una carpeta es un contenedor. Ambos soportan la operación "obtener tamaño". Para un archivo devuelve su tamaño directo. Para una carpeta devuelve la suma de los tamaños de todos sus contenidos recursivamente. El cliente usa la misma operación en ambos.

---

## Estructura

```
Component (abstract o interface)
├── Operation(): string    ← operación común para hojas y composites
├── Add(Component)         ← solo tiene sentido en Composite
└── Remove(Component)      ← solo tiene sentido en Composite

Leaf : Component           ← objeto final — sin hijos, hace el trabajo real
└── Operation() → "Leaf"

Composite : Component      ← contenedor con hijos
├── _children: List<Component>
├── Add(Component) → _children.Add(component)
├── Remove(Component) → _children.Remove(component)
└── Operation() → llama Operation() en todos los hijos y combina resultados
```

---

## Código del ejemplo conceptual

```csharp
// Componente base — interfaz común para Leaf y Composite
abstract class Component
{
    public abstract string Operation();

    public virtual void Add(Component component) { throw new NotImplementedException(); }
    public virtual void Remove(Component component) { throw new NotImplementedException(); }
    public virtual bool IsComposite() => true;
}

// Leaf — objeto terminal, hace el trabajo real
class Leaf : Component
{
    public override string Operation() => "Leaf";
    public override bool IsComposite() => false;
}

// Composite — contenedor, delega a sus hijos
class Composite : Component
{
    protected List<Component> _children = new List<Component>();

    public override void Add(Component component) => _children.Add(component);
    public override void Remove(Component component) => _children.Remove(component);

    // Recorre recursivamente todos los hijos
    public override string Operation()
    {
        var results = _children.Select(child => child.Operation());
        return "Branch(" + string.Join("+", results) + ")";
    }
}

// El cliente trabaja igual con Leaf y Composite:
Client client = new Client();

Leaf leaf = new Leaf();
Console.WriteLine("Simple component:");
client.ClientCode(leaf);
// "RESULT: Leaf"

Composite tree    = new Composite();
Composite branch1 = new Composite();
branch1.Add(new Leaf());
branch1.Add(new Leaf());
Composite branch2 = new Composite();
branch2.Add(new Leaf());
tree.Add(branch1);
tree.Add(branch2);

Console.WriteLine("Composite tree:");
client.ClientCode(tree);
// "RESULT: Branch(Branch(Leaf+Leaf)+Branch(Leaf))"
```

---

## Ejemplo real: sistema de descuentos

```csharp
// Componente
public interface IDiscount
{
    decimal Apply(decimal price);
    string Describe();
}

// Leaf: descuento simple
public class PercentDiscount : IDiscount
{
    private readonly decimal _percent;
    private readonly string _name;

    public PercentDiscount(string name, decimal percent) { _name = name; _percent = percent; }

    public decimal Apply(decimal price) => price * (1 - _percent / 100);
    public string Describe() => $"{_name} ({_percent}%)";
}

public class FixedDiscount : IDiscount
{
    private readonly decimal _amount;
    private readonly string _name;

    public FixedDiscount(string name, decimal amount) { _name = name; _amount = amount; }

    public decimal Apply(decimal price) => Math.Max(0, price - _amount);
    public string Describe() => $"{_name} (-${_amount})";
}

// Composite: combina múltiples descuentos
public class CompositeDiscount : IDiscount
{
    private readonly List<IDiscount> _discounts = new();
    private readonly string _name;

    public CompositeDiscount(string name) { _name = name; }

    public void Add(IDiscount discount) => _discounts.Add(discount);

    public decimal Apply(decimal price)
    {
        return _discounts.Aggregate(price, (current, d) => d.Apply(current));
    }

    public string Describe() =>
        $"{_name}: [{string.Join(", ", _discounts.Select(d => d.Describe()))}]";
}

// Uso — componer descuentos como árbol:
var blackFriday = new CompositeDiscount("Black Friday");
blackFriday.Add(new PercentDiscount("Base", 20));    // 20% off

var loyaltyBundle = new CompositeDiscount("Loyalty Bundle");
loyaltyBundle.Add(new PercentDiscount("Loyalty", 5));   // 5% off para leales
loyaltyBundle.Add(new FixedDiscount("Coupon", 10));     // -$10 con cupón

var total = new CompositeDiscount("Total Sale");
total.Add(blackFriday);
total.Add(loyaltyBundle);

decimal finalPrice = total.Apply(100);
// 100 → 80 (20%) → 76 (5%) → 66 ($10)
Console.WriteLine($"Precio final: {finalPrice}");
Console.WriteLine(total.Describe());
```

---

## Composite en ASP.NET Core: el pipeline de middleware

El pipeline de middleware de ASP.NET Core es un Composite implícito:

```csharp
// Cada middleware es un Component
// El pipeline es el Composite que los contiene

app.UseHttpsRedirection();    // Leaf middleware
app.UseCors(PolicyName);      // Leaf middleware
app.UseAuthentication();      // Leaf middleware
app.UseAuthorization();       // Leaf middleware
app.MapControllers();         // Leaf middleware

// El pipeline los ejecuta en orden — igual que Composite.Operation()
// recorre todos los hijos
```

---

## En este proyecto

```csharp
// Los servicios de DI se registran en una estructura Composite implícita:

// Cada ServiceCollectionEx agrega sus "hojas" al contenedor
builder.Services.AddApplicationServices();    // agrega hojas de Application
builder.Services.AddInfrastructureServices(); // agrega hojas de Infrastructure
builder.Services.AddWebApiServices();         // agrega hojas de WebApi

// El IServiceProvider actúa como el Composite — sabe cómo resolver
// la jerarquía completa de dependencias recursivamente
```

---

## Cuándo usar

- Cuando tienes una estructura de árbol (jerarquía parte-todo).
- Cuando el cliente debe tratar objetos simples y compuestos de forma uniforme.
- Cuando necesitas operaciones recursivas sobre estructuras jerárquicas.
- Ejemplos: sistemas de archivos, UI (widgets que contienen widgets), organizaciones (empresa → departamento → empleado), expresiones matemáticas, menús de navegación.

## Cuándo NO usar

- Cuando la estructura es siempre plana (sin jerarquía).
- Cuando los objetos hoja y compuesto tienen comportamientos muy diferentes — la interfaz común se vuelve incómoda.
- Cuando el tipo del componente importa al cliente — el Composite hace que sea difícil restringir los tipos de componentes permitidos.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Composite | Patrón estructural que permite componer objetos en estructuras de árbol y tratarlos uniformemente como objetos individuales |
| Componente (Component) | Interfaz o clase base común para hojas y composites que define las operaciones disponibles para todos |
| Hoja (Leaf) | Elemento terminal del árbol que no tiene hijos y realiza el trabajo real cuando se llama su operación |
| Composite (nodo) | Elemento del árbol que puede contener hijos (hojas u otros composites) y delega las operaciones a ellos |
| Jerarquía parte-todo | Estructura donde un objeto puede contener otros objetos del mismo tipo (árbol, sistema de archivos, menú) |
| Recursión | Mecanismo por el que el Composite ejecuta su operación llamando la misma operación en cada hijo sucesivamente |
| Tratamiento uniforme | Propiedad clave del Composite: el cliente usa la misma interfaz para hojas y composites sin distinguir tipos |
| `IDiscount` | Ejemplo del proyecto donde `CompositeDiscount` agrupa múltiples descuentos tratándolos uniformemente |
| Pipeline de middleware | Estructura Composite implícita de ASP.NET Core donde cada middleware es una hoja y el pipeline los contiene |
| `IServiceProvider` | Contenedor de DI que resuelve recursivamente la jerarquía de dependencias — Composite implícito |

---

*Rogelio Arriaga Gonzalez*
