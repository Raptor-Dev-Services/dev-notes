# 19 — State

**Categoría:** Conductual

**Intención:** Permite a un objeto alterar su comportamiento cuando su estado interno cambia. Parece como si el objeto cambiara de clase.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.5 Behavioral Patterns: State

---

## El problema

Tienes una clase `Document` que puede estar en estados: Borrador, Moderación, Publicado. El comportamiento de `publish()` varía: desde Borrador puede pasar a Moderación; desde Moderación puede publicarse o rechazarse; desde Publicado no puede publicarse de nuevo. Sin el patrón, el código está lleno de `if/switch` que crecen con cada estado nuevo.

```csharp
// ❌ Sin State — if/switch monolítico que crece con cada estado nuevo
public void Publish()
{
    if (_state == "Draft")
    {
        _state = "Moderation";
    }
    else if (_state == "Moderation")
    {
        if (IsAdmin) _state = "Published";
    }
    else if (_state == "Published")
    {
        // no hacer nada
    }
    // Cada nuevo estado → agregar más if/else en todos los métodos
}
```

---

## Analogía

Un semáforo. El semáforo tiene estados: Rojo, Amarillo, Verde. Cada estado define el comportamiento (parar, prepararse, avanzar) y cuál es el siguiente estado. El semáforo como objeto solo llama "siguiente estado". Cada estado sabe qué hacer y a quién delegar.

---

## Estructura

```
Context
├── _state: State              ← estado actual
├── TransitionTo(State)        ← cambia el estado
├── Request1() → _state.Handle1()  ← delega al estado actual
└── Request2() → _state.Handle2()

State (abstract)
├── _context: Context          ← referencia al contexto (para cambiar estado)
├── SetContext(Context)
├── Handle1() (abstract)
└── Handle2() (abstract)

ConcreteStateA : State
├── Handle1() → hace algo; _context.TransitionTo(new ConcreteStateB())
└── Handle2() → hace otra cosa

ConcreteStateB : State
├── Handle1() → hace algo
└── Handle2() → hace algo; _context.TransitionTo(new ConcreteStateA())
```

---

## Código del ejemplo conceptual

```csharp
class Context
{
    private State _state;

    public Context(State state)
    {
        TransitionTo(state);
    }

    // Cambia el estado en runtime — el Context no sabe qué estados existen
    public void TransitionTo(State state)
    {
        Console.WriteLine($"Context: Transition to {state.GetType().Name}.");
        _state = state;
        _state.SetContext(this);  // el estado necesita referencia al contexto para cambiar estado
    }

    // Delega las requests al estado actual
    public void Request1() { _state.Handle1(); }
    public void Request2() { _state.Handle2(); }
}

abstract class State
{
    protected Context _context;
    public void SetContext(Context context) { _context = context; }

    public abstract void Handle1();
    public abstract void Handle2();
}

class ConcreteStateA : State
{
    public override void Handle1()
    {
        Console.WriteLine("ConcreteStateA handles request1.");
        Console.WriteLine("ConcreteStateA wants to change the state of the context.");
        _context.TransitionTo(new ConcreteStateB());  // cambia al estado B
    }

    public override void Handle2()
    {
        Console.WriteLine("ConcreteStateA handles request2.");
    }
}

class ConcreteStateB : State
{
    public override void Handle1()
    {
        Console.Write("ConcreteStateB handles request1.");
    }

    public override void Handle2()
    {
        Console.WriteLine("ConcreteStateB handles request2.");
        Console.WriteLine("ConcreteStateB wants to change the state of the context.");
        _context.TransitionTo(new ConcreteStateA());  // cambia de vuelta al estado A
    }
}

// Uso:
var context = new Context(new ConcreteStateA());
context.Request1();  // ConcreteStateA.Handle1() → transición a StateB
context.Request2();  // ConcreteStateB.Handle2() → transición a StateA
```

---

## Ejemplo real: sistema de órdenes de compra

```csharp
// Estados de una orden
public abstract class OrderState
{
    protected OrderContext _order;
    public void SetOrder(OrderContext order) { _order = order; }

    public abstract void Pay();
    public abstract void Ship();
    public abstract void Cancel();
    public abstract string GetStatusName();
}

public class PendingState : OrderState
{
    public override void Pay()
    {
        Console.WriteLine("Order: Payment received.");
        _order.TransitionTo(new PaidState());
    }

    public override void Ship()
    {
        Console.WriteLine("Order: Cannot ship — not paid yet.");
    }

    public override void Cancel()
    {
        Console.WriteLine("Order: Cancelled from pending.");
        _order.TransitionTo(new CancelledState());
    }

    public override string GetStatusName() => "Pending";
}

public class PaidState : OrderState
{
    public override void Pay() => Console.WriteLine("Order: Already paid.");

    public override void Ship()
    {
        Console.WriteLine("Order: Shipping the order.");
        _order.TransitionTo(new ShippedState());
    }

    public override void Cancel()
    {
        Console.WriteLine("Order: Refunding payment...");
        _order.TransitionTo(new CancelledState());
    }

    public override string GetStatusName() => "Paid";
}

public class ShippedState : OrderState
{
    public override void Pay()    => Console.WriteLine("Order: Already paid.");
    public override void Ship()   => Console.WriteLine("Order: Already shipped.");
    public override void Cancel() => Console.WriteLine("Order: Cannot cancel — already shipped.");
    public override string GetStatusName() => "Shipped";
}

public class CancelledState : OrderState
{
    public override void Pay()    => Console.WriteLine("Order: Cannot pay — cancelled.");
    public override void Ship()   => Console.WriteLine("Order: Cannot ship — cancelled.");
    public override void Cancel() => Console.WriteLine("Order: Already cancelled.");
    public override string GetStatusName() => "Cancelled";
}

// El Context — la Orden
public class OrderContext
{
    private OrderState _state;
    public Guid OrderId { get; } = Guid.NewGuid();

    public OrderContext() { TransitionTo(new PendingState()); }

    public void TransitionTo(OrderState state)
    {
        _state = state;
        _state.SetOrder(this);
        Console.WriteLine($"Order {OrderId}: → {state.GetStatusName()}");
    }

    public void Pay()    => _state.Pay();
    public void Ship()   => _state.Ship();
    public void Cancel() => _state.Cancel();
    public string Status => _state.GetStatusName();
}

// Uso:
var order = new OrderContext();  // Pending
order.Ship();   // "Cannot ship — not paid yet."
order.Pay();    // Paid
order.Ship();   // Shipped
order.Cancel(); // "Cannot cancel — already shipped."
```

---

## En este proyecto

```csharp
// ResultViewModel<T> es una máquina de estados de dos estados:
// Estado inicial: undefined (IsSuccess = false, Data = null)
// Estado Success: IsSuccess = true, Data = resultado
// Estado Failure: IsSuccess = false, Message = error

public class ResultViewModel<T>
{
    public bool IsSuccess { get; private set; }
    public object? Data   { get; private set; }
    public string Message { get; private set; } = string.Empty;

    // Transición a Estado Success con datos ISuccess<TData>
    public ResultViewModel<T> Set<TData>(ISuccess<TData> success)
    {
        IsSuccess = true;
        Data      = success.Data;
        return this;
    }

    // Transición a Estado Success con datos arbitrarios
    public ResultViewModel<T> OK(object data)
    {
        IsSuccess = true;
        Data      = data;
        return this;
    }

    // Transición a Estado Failure
    public ResultViewModel<T> Fail(string message)
    {
        IsSuccess = false;
        Message   = message;
        return this;
    }
}

// El Controller actúa según el estado:
_ = await Mediator.Send(request, ct);
return _viewModel.IsSuccess        // consulta el estado
    ? Ok(_viewModel)               // respuesta si Success
    : StatusCode(500, _viewModel); // respuesta si Failure
```

---

## State vs Strategy

| Aspecto | State | Strategy |
|---------|-------|---------|
| Quién cambia el comportamiento | El estado puede cambiarse a sí mismo | El cliente elige la estrategia |
| Conocimiento entre estados | Los estados pueden saber de otros estados | Las estrategias son independientes |
| Cuándo | Ciclo de vida del objeto (tiene estados naturales) | Algoritmo intercambiable por el cliente |
| Runtime | El estado cambia automáticamente | El cliente cambia la estrategia manualmente |

---

## Cuándo usar

- Cuando el comportamiento de un objeto depende de su estado y debe cambiar en runtime.
- Cuando tienes switch/if con muchas ramas dependiendo del estado del objeto.
- Cuando hay transiciones de estado complejas con reglas de validación por estado.
- Máquinas de estado: flujos de trabajo, ciclos de vida de entidades, protocolos de red, UI.

## Cuándo NO usar

- Cuando solo hay 2-3 estados simples. Un `bool` o `enum` es más directo.
- Cuando las transiciones son pocas y estables. Un switch simple puede ser suficiente.
- Cuando los estados no tienen comportamientos realmente distintos.


---

## Glosario

| Término | Definición |
|---------|-----------|
| State | Patrón conductual que permite a un objeto cambiar su comportamiento cuando su estado interno cambia, como si cambiara de clase |
| Context | El objeto que tiene el estado; delega el comportamiento al objeto State actual y expone `TransitionTo()` |
| State (clase abstracta) | Clase base para todos los estados concretos; define las operaciones posibles y mantiene referencia al Context |
| Transición | Cambio de un estado a otro; los estados concretos la disparan llamando `_context.TransitionTo(new OtroState())` |
| Máquina de estados (FSM) | Modelo formal donde un objeto puede estar en uno de N estados y transita según eventos definidos |
| `ResultViewModel<T>` | Máquina de estados del proyecto con tres estados: undefined → Success / undefined → Failure |
| `IsSuccess` | Propiedad del `ResultViewModel` que representa el estado actual para que el controller tome decisiones |
| Ciclo de vida de entidad | Escenario natural para el State: órdenes (Pending → Paid → Shipped → Cancelled), documentos (Draft → Review → Published) |
| State vs Strategy | En State el objeto cambia de comportamiento automáticamente; en Strategy el cliente elige el algoritmo explícitamente |
| Guard condition | Validación en una transición de estado que solo permite el cambio si se cumple una condición (ej: solo admin puede publicar) |

---

*Rogelio Arriaga Gonzalez*
