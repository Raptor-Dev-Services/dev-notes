# 22 — Visitor

**Categoría:** Conductual

**Intención:** Permite separar algoritmos de los objetos sobre los que operan. Con Visitor puedes agregar nuevas operaciones a una jerarquía de clases sin modificar esas clases.

---

## El problema

Tienes una jerarquía de formas geométricas: `Circle`, `Rectangle`, `Triangle`. Quieres agregar operaciones: exportar a XML, calcular área, serializar a JSON. Sin Visitor, cada operación nueva requiere modificar todas las clases de la jerarquía — viola el Open/Closed Principle.

```csharp
// ❌ Sin Visitor — cada nueva operación modifica todas las clases
class Circle
{
    public string ExportXml() { /* ... */ }   // operación 1
    public double CalculateArea() { /* ... */ } // operación 2
    public string ToJson() { /* ... */ }       // operación 3 — ¿cuántas más?
}
class Rectangle { /* mismo problema */ }
class Triangle  { /* mismo problema */ }
```

---

## Analogía

Un inspector de un edificio. El edificio tiene diferentes tipos de habitaciones (cocina, baño, dormitorio). Para inspeccionar el edificio, el inspector "visita" cada habitación. Según el tipo de habitación, el inspector ejecuta diferentes verificaciones. Si quieres una nueva inspección (eléctrica, de plomería), creas un nuevo inspector (Visitor) sin modificar las habitaciones.

---

## Estructura

```
IComponent
└── Accept(IVisitor visitor)    ← el "double dispatch"

ConcreteComponentA : IComponent
├── Accept(visitor) → visitor.VisitConcreteComponentA(this)
└── ExclusiveMethodOfA(): string  ← método específico de A

ConcreteComponentB : IComponent
├── Accept(visitor) → visitor.VisitConcreteComponentB(this)
└── SpecialMethodOfB(): string

IVisitor
├── VisitConcreteComponentA(ConcreteComponentA)
└── VisitConcreteComponentB(ConcreteComponentB)

ConcreteVisitor1 : IVisitor
├── VisitConcreteComponentA(a) → usa a.ExclusiveMethodOfA()
└── VisitConcreteComponentB(b) → usa b.SpecialMethodOfB()

ConcreteVisitor2 : IVisitor
└── (mismas visitas, diferente comportamiento)
```

---

## Código del ejemplo conceptual

```csharp
// La interfaz de componente declara Accept — el "puerta de entrada" al Visitor
public interface IComponent
{
    void Accept(IVisitor visitor);
}

// Componentes concretos — implementan Accept llamando al método del visitor correspondiente
public class ConcreteComponentA : IComponent
{
    public void Accept(IVisitor visitor)
    {
        // Double dispatch: se llama al método específico para ConcreteComponentA
        visitor.VisitConcreteComponentA(this);
    }

    // Método especial de A — el Visitor puede llamarlo porque sabe el tipo concreto
    public string ExclusiveMethodOfConcreteComponentA() => "A";
}

public class ConcreteComponentB : IComponent
{
    public void Accept(IVisitor visitor)
    {
        visitor.VisitConcreteComponentB(this);
    }

    public string SpecialMethodOfConcreteComponentB() => "B";
}

// La interfaz del Visitor — un método por cada tipo de componente
public interface IVisitor
{
    void VisitConcreteComponentA(ConcreteComponentA element);
    void VisitConcreteComponentB(ConcreteComponentB element);
}

// Visitor concreto 1 — una operación específica sobre todos los componentes
class ConcreteVisitor1 : IVisitor
{
    public void VisitConcreteComponentA(ConcreteComponentA element)
    {
        Console.WriteLine(element.ExclusiveMethodOfConcreteComponentA() + " + ConcreteVisitor1");
    }

    public void VisitConcreteComponentB(ConcreteComponentB element)
    {
        Console.WriteLine(element.SpecialMethodOfConcreteComponentB() + " + ConcreteVisitor1");
    }
}

// Visitor concreto 2 — otra operación
class ConcreteVisitor2 : IVisitor
{
    public void VisitConcreteComponentA(ConcreteComponentA element)
    {
        Console.WriteLine(element.ExclusiveMethodOfConcreteComponentA() + " + ConcreteVisitor2");
    }

    public void VisitConcreteComponentB(ConcreteComponentB element)
    {
        Console.WriteLine(element.SpecialMethodOfConcreteComponentB() + " + ConcreteVisitor2");
    }
}

// Uso — aplicar visitors a una colección de componentes:
List<IComponent> components = new()
{
    new ConcreteComponentA(),
    new ConcreteComponentB()
};

var visitor1 = new ConcreteVisitor1();
foreach (var component in components)
    component.Accept(visitor1);
// "A + ConcreteVisitor1"
// "B + ConcreteVisitor1"

var visitor2 = new ConcreteVisitor2();
foreach (var component in components)
    component.Accept(visitor2);
// "A + ConcreteVisitor2"
// "B + ConcreteVisitor2"
```

---

## Double Dispatch — el truco clave

El problema de despacho simple: cuando llamas `component.DoSomething()` donde `component` es `IComponent`, en runtime se resuelve al método de la clase concreta. Pero si `DoSomething` es el mismo para todos, no puedes ejecutar código diferente según el tipo del componente.

```csharp
// Single dispatch — solo resuelve el tipo del receptor
void ProcessComponent(IComponent component)
{
    // No sabes si es ComponentA o ComponentB aquí
    // El compilador no puede elegir el método correcto
}

// Double dispatch — resuelve tipo del receptor Y tipo del argumento
void ProcessComponent(IComponent component, IVisitor visitor)
{
    component.Accept(visitor);
    //           ↑ dispatch 1: resuelve tipo concreto del component
    // Dentro de Accept:
    // visitor.VisitConcreteComponentA(this)
    //          ↑ dispatch 2: resuelve tipo del visitor Y pasa el tipo concreto del component
}
```

---

## Visitor en C# moderno — Pattern Matching como Visitor implícito

En C# moderno, el pattern matching en switch expressions es un Visitor implícito:

```csharp
// Visitor implícito con switch expression:
public static string ExportToXml(IComponent component)
{
    return component switch
    {
        ConcreteComponentA a => $"<ComponentA>{a.ExclusiveMethodOfConcreteComponentA()}</ComponentA>",
        ConcreteComponentB b => $"<ComponentB>{b.SpecialMethodOfConcreteComponentB()}</ComponentB>",
        _ => throw new ArgumentException($"Unknown component type: {component.GetType().Name}")
    };
}

public static double CalculateArea(IShape shape)
{
    return shape switch
    {
        Circle c    => Math.PI * c.Radius * c.Radius,
        Rectangle r => r.Width * r.Height,
        Triangle t  => 0.5 * t.Base * t.Height,
        _           => throw new ArgumentException($"Unknown shape: {shape.GetType().Name}")
    };
}
```

Este es exactamente el Visitor — el switch es el "VisitX" por tipo — pero sin las clases adicionales de la implementación clásica.

---

## Ejemplo real: serialización de árbol de expresiones

```csharp
public interface IExpression
{
    T Accept<T>(IExpressionVisitor<T> visitor);
}

public sealed class NumberExpression : IExpression
{
    public double Value { get; }
    public NumberExpression(double value) { Value = value; }
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitNumber(this);
}

public sealed class AddExpression : IExpression
{
    public IExpression Left  { get; }
    public IExpression Right { get; }
    public AddExpression(IExpression left, IExpression right) { Left = left; Right = right; }
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitAdd(this);
}

public sealed class MultiplyExpression : IExpression
{
    public IExpression Left  { get; }
    public IExpression Right { get; }
    public MultiplyExpression(IExpression left, IExpression right) { Left = left; Right = right; }
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitMultiply(this);
}

// Interfaz del visitor genérico
public interface IExpressionVisitor<T>
{
    T VisitNumber(NumberExpression number);
    T VisitAdd(AddExpression add);
    T VisitMultiply(MultiplyExpression multiply);
}

// Visitor 1: evalúa la expresión
public class EvaluatorVisitor : IExpressionVisitor<double>
{
    public double VisitNumber(NumberExpression n)   => n.Value;
    public double VisitAdd(AddExpression a)          => a.Left.Accept(this) + a.Right.Accept(this);
    public double VisitMultiply(MultiplyExpression m) => m.Left.Accept(this) * m.Right.Accept(this);
}

// Visitor 2: convierte a string infix
public class PrinterVisitor : IExpressionVisitor<string>
{
    public string VisitNumber(NumberExpression n)    => n.Value.ToString();
    public string VisitAdd(AddExpression a)           => $"({a.Left.Accept(this)} + {a.Right.Accept(this)})";
    public string VisitMultiply(MultiplyExpression m) => $"({m.Left.Accept(this)} * {m.Right.Accept(this)})";
}

// Uso — misma estructura de árbol, dos visitors diferentes:
IExpression expr = new AddExpression(
    new NumberExpression(3),
    new MultiplyExpression(new NumberExpression(4), new NumberExpression(5)));
//  3 + (4 * 5) = 23

var evaluator = new EvaluatorVisitor();
Console.WriteLine(expr.Accept(evaluator));  // 23

var printer = new PrinterVisitor();
Console.WriteLine(expr.Accept(printer));    // "(3 + (4 * 5))"

// Agregar un nuevo Visitor (ej: LaTeX printer) NO modifica NumberExpression, AddExpression...
```

---

## En este proyecto

```csharp
// El pattern matching en los Presenters ES el Visitor:
public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
{
    // "Visit" cada tipo concreto de respuesta:
    if (notification is INotFoundFailure failure)     // VisitNotFoundFailure
        _viewModel.Fail(failure.Message);
    else if (notification is ISuccess<ExampleUserDto> success)  // VisitSuccess
        _viewModel.Set(success);

    return Task.CompletedTask;
}

// El Presenter "visita" cada tipo concreto de respuesta y actúa diferente.
// Si se agrega un nuevo tipo de respuesta (ej: IConflictFailure),
// el Presenter (Visitor) se extiende — los types no se modifican.
```

---

## Cuándo usar

- Cuando necesitas realizar una operación sobre todos los elementos de una estructura de objetos compleja.
- Cuando quieres mantener juntas las operaciones relacionadas y separadas las no relacionadas.
- Cuando una operación sobre elementos de la jerarquía requiere saber el tipo concreto.
- Para separar algoritmos de los datos que operan: compiladores (AST + Visitor), motores de reglas, serialización.

## Cuándo NO usar

- Cuando la jerarquía de clases cambia frecuentemente — agregar un nuevo tipo requiere modificar todos los Visitors.
- Cuando las operaciones que quieres añadir son pocas — sobreingeniería.
- Cuando C# pattern matching resuelve el problema de forma más simple — úsalo en su lugar.

### Regla práctica

Si **los tipos** cambian frecuentemente → No usar Visitor (agregar tipo = modificar todos los Visitors).  
Si **las operaciones** cambian frecuentemente → Usar Visitor (agregar operación = nuevo Visitor, tipos no cambian).


---

*Rogelio Arriaga Gonzalez*
