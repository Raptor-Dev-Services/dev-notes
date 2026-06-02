# 09 — Decorator

**Categoría:** Estructural

**Intención:** Permite agregar nuevos comportamientos a objetos colocándolos dentro de objetos envolventes especiales que contienen los comportamientos. También conocido como "Wrapper".

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.4 Structural Patterns: Decorator

---

## El problema

Tienes una clase `EmailNotifier`. Quieres agregarle capacidades de envío por SMS Y Slack. Con herencia:
- `SMSEmailNotifier : EmailNotifier`
- `SlackEmailNotifier : EmailNotifier`
- `SMSSlackEmailNotifier : EmailNotifier`

La jerarquía explota. Además, la herencia es estática — no puedes cambiar el comportamiento en runtime.

```csharp
// ❌ Con herencia — explosión combinatoria, estático en compile-time
class SMSSlackEmailNotifier : EmailNotifier { /* combina 3 comportamientos */ }
// No puedes activar/desactivar canales en runtime
```

---

## Analogía

Vestirse con ropa. Empiezas con la ropa base. Luego la "decoras" con capas: una chaqueta, una bufanda, un impermeable. Cada capa añade comportamiento (calor, protección) sin modificar las capas inferiores. Puedes combinar capas en cualquier orden y en runtime.

---

## Estructura

```
Component (abstract o interface)
└── Operation(): string

ConcreteComponent : Component
└── Operation() → "ConcreteComponent"  ← hace el trabajo real

Decorator : Component
├── _component: Component              ← referencia al envuelto
└── Operation() → _component.Operation()  ← delega al envuelto

ConcreteDecoratorA : Decorator
└── Operation() → "A(" + base.Operation() + ")"  ← antes/después del wrapped

ConcreteDecoratorB : Decorator
└── Operation() → "B(" + base.Operation() + ")"
```

---

## Código del ejemplo conceptual

```csharp
// Componente base
public abstract class Component
{
    public abstract string Operation();
}

// Componente concreto — la funcionalidad base
class ConcreteComponent : Component
{
    public override string Operation() => "ConcreteComponent";
}

// Decorador base — wrappea un componente
abstract class Decorator : Component
{
    protected Component _component;

    public Decorator(Component component) { _component = component; }

    public override string Operation()
    {
        return _component != null ? _component.Operation() : string.Empty;
    }
}

// Decorador concreto A — añade comportamiento alrededor del wrapped
class ConcreteDecoratorA : Decorator
{
    public ConcreteDecoratorA(Component comp) : base(comp) { }

    public override string Operation()
    {
        return $"ConcreteDecoratorA({base.Operation()})";
        //                           ↑ llama al wrapped (base o inner component)
    }
}

// Decorador concreto B
class ConcreteDecoratorB : Decorator
{
    public ConcreteDecoratorB(Component comp) : base(comp) { }

    public override string Operation()
    {
        return $"ConcreteDecoratorB({base.Operation()})";
    }
}

// Uso — se pueden anidar en cualquier orden:
var simple     = new ConcreteComponent();
var decorator1 = new ConcreteDecoratorA(simple);        // wrappea simple
var decorator2 = new ConcreteDecoratorB(decorator1);    // wrappea decorator1

Console.WriteLine(simple.Operation());
// "ConcreteComponent"

Console.WriteLine(decorator2.Operation());
// "ConcreteDecoratorB(ConcreteDecoratorA(ConcreteComponent))"
// Se ejecuta de afuera hacia adentro, o de adentro hacia afuera según la implementación
```

---

## Ejemplo real: sistema de notificaciones

```csharp
// Componente base
public interface INotifier
{
    void Send(string message);
}

// Componente concreto
public class BaseEmailNotifier : INotifier
{
    private readonly string _email;

    public BaseEmailNotifier(string email) { _email = email; }

    public void Send(string message)
    {
        Console.WriteLine($"Sending email to {_email}: {message}");
    }
}

// Decorador base
public abstract class NotifierDecorator : INotifier
{
    private readonly INotifier _wrappee;

    protected NotifierDecorator(INotifier notifier) { _wrappee = notifier; }

    public virtual void Send(string message)
    {
        _wrappee.Send(message);  // delega al envuelto
    }
}

// Decorador SMS — añade envío por SMS
public class SMSDecorator : NotifierDecorator
{
    private readonly string _phone;

    public SMSDecorator(INotifier notifier, string phone) : base(notifier)
    {
        _phone = phone;
    }

    public override void Send(string message)
    {
        base.Send(message);  // primero el email (el envuelto)
        Console.WriteLine($"Sending SMS to {_phone}: {message}");  // luego el SMS
    }
}

// Decorador Slack
public class SlackDecorator : NotifierDecorator
{
    private readonly string _channel;

    public SlackDecorator(INotifier notifier, string channel) : base(notifier)
    {
        _channel = channel;
    }

    public override void Send(string message)
    {
        base.Send(message);
        Console.WriteLine($"Sending Slack to #{_channel}: {message}");
    }
}

// Uso — componer en runtime según configuración:
INotifier notifier = new BaseEmailNotifier("admin@company.com");

// Agregar SMS
notifier = new SMSDecorator(notifier, "+52-555-0000");

// Agregar Slack también
notifier = new SlackDecorator(notifier, "alerts");

// Una llamada → envía por los 3 canales
notifier.Send("Servidor caído!");
// "Sending email to admin@company.com: Servidor caído!"
// "Sending SMS to +52-555-0000: Servidor caído!"
// "Sending Slack to #alerts: Servidor caído!"
```

---

## Decorator en ASP.NET Core y C#

### Atributos como decoradores

```csharp
// [Authorize] "decora" el método — añade verificación de auth sin modificar el código
[HttpGet("{id:guid}")]
[Authorize]                     // ← Decorator: añade autenticación
[ResponseCache(Duration = 60)]  // ← Decorator: añade caché
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    // código sin cambios — los decoradores se aplican desde afuera
}
```

### Pipeline behaviors en Mediator (concepto)

```csharp
// Un behavior de pipeline envuelve el handler — es un Decorator
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger _logger;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();  // ← llama al handler envuelto (el "inner component")
        _logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
// El handler no sabe que está decorado. El Decorator es transparente.
```

---

## En este proyecto

```csharp
// 1. [Authorize] en controllers — decorador declarativo de ASP.NET Core
[Authorize]
public sealed class ExampleUsersController : BaseApiController { }

// 2. MainDapperDbConnection wrappea Dapper — es un Decorator + Adapter
// Añade logging y timing alrededor de cada query de Dapper
// sin que el código que llama sepa del overhead de logging

// 3. El InteractorPipeline del mediador envuelve el Handler:
// Controller → InteractorPipeline → Handler → InteractorPipeline → Presenter
//              ↑ Decorator que añade el flujo Publish
```

---

## Comparación: Decorator vs Herencia

| Aspecto | Herencia | Decorator |
|---------|---------|-----------|
| Composición | Estática (compile-time) | Dinámica (runtime) |
| Combinaciones | N×M clases | N+M clases |
| Modificar base | Afecta todas las subclases | No afecta el original |
| Desactivar comportamiento | Imposible sin refactorizar | Basta con no envolver |
| Orden de ejecución | Fijo por jerarquía | Configurable por orden de wrapping |

---

## Cuándo usar

- Cuando quieres añadir comportamiento a objetos individuales sin afectar a otros.
- Cuando la extensión por herencia es poco práctica (muchas combinaciones).
- Cuando quieres combinar comportamientos de formas no previstas al diseñar.
- Para cross-cutting concerns: logging, caché, autorización, timing, retry, circuit-breaker.

## Cuándo NO usar

- Para comportamiento tan esencial que todas las instancias siempre lo necesitan — use herencia o el componente base.
- Cuando el orden de los decoradores importa y es difícil de controlar — puede ser confuso.
- Cuando solo tienes un comportamiento adicional — una subclase simple puede ser suficiente.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Decorator | Patrón estructural que permite añadir comportamiento a objetos individuales envolviéndolos en objetos especiales que implementan la misma interfaz |
| Wrapper | Sinónimo de Decorator; objeto que envuelve al componente original y añade funcionalidad antes o después de delegarle la llamada |
| Componente base | El objeto original que realiza la funcionalidad central; todos los decoradores delegan a él en última instancia |
| Encadenamiento de decoradores | Técnica de anidar múltiples decoradores donde cada uno envuelve al anterior, acumulando comportamiento |
| Cross-cutting concern | Aspecto transversal del sistema (logging, caché, autorización, retry) que aplica a múltiples partes sin pertenecer a ninguna en particular |
| `[Authorize]` | Atributo de ASP.NET Core que actúa como Decorator declarativo sobre acciones del controller |
| Pipeline behavior | Componente que envuelve al Handler en el mediador — Decorator que aplica lógica antes y después de la ejecución |
| `IPipelineBehavior<TRequest, TResponse>` | Interfaz del Decorator del pipeline del mediador en el proyecto |
| Composición dinámica | Capacidad del Decorator de combinar comportamientos en runtime, a diferencia de la herencia que es estática |
| Decorador vs Herencia | El Decorator combina comportamientos con N+M clases; la herencia requiere N×M para las mismas combinaciones |

---

*Rogelio Arriaga Gonzalez*
