# 03 — Abstract Factory

**Categoría:** Creacional

**Intención:** Proporciona una interfaz para crear familias de objetos relacionados o dependientes sin especificar sus clases concretas.

---

## El problema

Tienes una app de UI que soporta múltiples temas visuales: Windows y macOS. Cada tema tiene sus propios botones, checkboxes y menús. Si el código crea los widgets directamente (`new WindowsButton()`), cambiar de tema significa cambiar cientos de lugares.

```csharp
// ❌ Sin Abstract Factory — acoplado a un estilo concreto
var button   = new WindowsButton();    // ← ¿y si es macOS?
var checkbox = new WindowsCheckbox();  // ← cambiar tema = modificar todo
```

---

## Analogía

Una tienda de muebles con catálogos de estilos: "Victoriano" y "Moderno". Cada catálogo tiene sillas, sofás y mesas en ese estilo. Si pides muebles del catálogo "Moderno", todos los muebles que recibes son del mismo estilo y son compatibles entre sí. No mezclas una silla victoriana con una mesa moderna.

---

## Estructura

```
IAbstractFactory
├── CreateProductA(): IAbstractProductA
└── CreateProductB(): IAbstractProductB

ConcreteFactory1 : IAbstractFactory  (variante 1 — "familia 1")
└── CreateProductA() → ConcreteProductA1
└── CreateProductB() → ConcreteProductB1

ConcreteFactory2 : IAbstractFactory  (variante 2 — "familia 2")
└── CreateProductA() → ConcreteProductA2
└── CreateProductB() → ConcreteProductB2

IAbstractProductA → ConcreteProductA1, ConcreteProductA2
IAbstractProductB → ConcreteProductB1, ConcreteProductB2
```

**Clave:** Los productos de la misma fábrica son compatibles entre sí. Los de fábricas distintas no.

---

## Código del ejemplo conceptual

```csharp
// La fábrica abstracta — define los métodos de creación
public interface IAbstractFactory
{
    IAbstractProductA CreateProductA();
    IAbstractProductB CreateProductB();
}

// Fábrica concreta 1 — crea la familia 1 (productos compatibles entre sí)
class ConcreteFactory1 : IAbstractFactory
{
    public IAbstractProductA CreateProductA() => new ConcreteProductA1();
    public IAbstractProductB CreateProductB() => new ConcreteProductB1();
}

// Fábrica concreta 2 — crea la familia 2
class ConcreteFactory2 : IAbstractFactory
{
    public IAbstractProductA CreateProductA() => new ConcreteProductA2();
    public IAbstractProductB CreateProductB() => new ConcreteProductB2();
}

// Productos abstractos
public interface IAbstractProductA
{
    string UsefulFunctionA();
}

public interface IAbstractProductB
{
    string UsefulFunctionB();
    string AnotherUsefulFunctionB(IAbstractProductA collaborator);  // colaboración entre productos
}

// Productos concretos de la familia 1
class ConcreteProductA1 : IAbstractProductA
{
    public string UsefulFunctionA() => "Result of product A1.";
}

class ConcreteProductB1 : IAbstractProductB
{
    public string UsefulFunctionB() => "Result of product B1.";

    // B1 solo colabora correctamente con A1 — misma familia
    public string AnotherUsefulFunctionB(IAbstractProductA collaborator)
    {
        var result = collaborator.UsefulFunctionA();
        return $"B1 collaborating with ({result})";
    }
}

// El cliente trabaja SOLO con interfaces — no sabe qué familia usa
class Client
{
    public void ClientMethod(IAbstractFactory factory)
    {
        var productA = factory.CreateProductA();
        var productB = factory.CreateProductB();

        Console.WriteLine(productB.UsefulFunctionB());
        Console.WriteLine(productB.AnotherUsefulFunctionB(productA));
    }
}

// Uso — cambiar de familia es cambiar UNA línea
var client = new Client();
client.ClientMethod(new ConcreteFactory1());  // familia 1
client.ClientMethod(new ConcreteFactory2());  // familia 2 — mismo código del cliente
```

---

## Ejemplo real: UI multiplataforma

```csharp
// Familia Windows
public class WindowsButton : IButton { public void Render() => Console.WriteLine("[Windows Button]"); }
public class WindowsCheckbox : ICheckbox { public void Check() => Console.WriteLine("[Windows Checkbox]"); }

// Familia macOS
public class MacButton : IButton { public void Render() => Console.WriteLine("[Mac Button]"); }
public class MacCheckbox : ICheckbox { public void Check() => Console.WriteLine("[Mac Checkbox]"); }

// Fábricas
public class WindowsFactory : IUIFactory
{
    public IButton CreateButton() => new WindowsButton();
    public ICheckbox CreateCheckbox() => new WindowsCheckbox();
}

public class MacFactory : IUIFactory
{
    public IButton CreateButton() => new MacButton();
    public ICheckbox CreateCheckbox() => new MacCheckbox();
}

// La app — solo conoce IUIFactory
public class Application
{
    private readonly IButton _button;
    private readonly ICheckbox _checkbox;

    public Application(IUIFactory factory)
    {
        _button   = factory.CreateButton();
        _checkbox = factory.CreateCheckbox();
    }

    public void Render()
    {
        _button.Render();
        _checkbox.Check();
    }
}

// Configuración en inicio — UN solo lugar decide la familia
IUIFactory factory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? new WindowsFactory()
    : new MacFactory();

var app = new Application(factory);
app.Render();
```

---

## Diferencia con Factory Method

| Aspecto | Factory Method | Abstract Factory |
|---------|---------------|-----------------|
| Nivel | Un producto | Una familia de productos |
| Extensión | Subclase con 1 método override | Nueva clase que implementa N métodos |
| Relación | La subclase crea el producto | La fábrica crea una familia entera |
| Uso típico | Un solo tipo de objeto varía | Múltiples tipos relacionados varían juntos |

---

## En este proyecto

```
Conexión conceptual:
appsettings.Local.json    → "familia desarrollo" de configuración
appsettings.Production.json → "familia producción" de configuración

IConfiguration actúa como la Abstract Factory abstracta.
El ambiente (Development/Production/Staging) determina qué familia
de valores de configuración se usa — misma interfaz, distintos valores.

No hay una clase IAbstractFactory explícita, pero el concepto aplica:
- Familia "dev": SQL text logging, localhost URLs, Seq local
- Familia "prod": solo SQL logging esencial, URLs reales, Seq externo
```

---

## Cuándo usar

- Cuando el sistema debe ser independiente de cómo se crean sus productos.
- Cuando el sistema debe trabajar con múltiples familias de productos.
- Cuando quieres garantizar que los productos de una familia se usen juntos.
- Cuando quieres proveer una biblioteca de productos sin revelar implementaciones.

## Cuándo NO usar

- Cuando solo tienes un tipo de producto que varía — usa Factory Method.
- Cuando agregar nuevos tipos de productos es frecuente — requiere cambiar la interfaz de la fábrica y todas sus implementaciones.
- Cuando la complejidad no está justificada por la variabilidad real.


---

*Rogelio Arriaga Gonzalez*
