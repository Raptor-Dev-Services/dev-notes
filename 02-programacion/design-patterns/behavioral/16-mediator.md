# 16 — Mediator

**Categoría:** Conductual

**Intención:** Reduce las dependencias caóticas entre objetos. El patrón restringe las comunicaciones directas entre objetos y los obliga a colaborar solo a través de un objeto mediador.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.5 Behavioral Patterns: Mediator

---

## El problema

Tienes un formulario de UI con: TextField, CheckBox, Button, DatePicker. Cuando el usuario escribe en el TextField, el Button debe habilitarse. Cuando la CheckBox se activa, el DatePicker debe mostrarse. Sin Mediator, cada componente referencia directamente a los demás. Esto crea un grafo de dependencias imposible de mantener.

```
Sin Mediator — N componentes, O(N²) dependencias:
TextField  →  Button, DatePicker, CheckBox
Button     →  TextField, DatePicker
DatePicker →  CheckBox, TextField
CheckBox   →  Button, DatePicker

Con Mediator — N componentes, N dependencias:
TextField  →  Mediator
Button     →  Mediator
DatePicker →  Mediator
CheckBox   →  Mediator
(cada componente solo conoce al mediador)
```

---

## Analogía

Una torre de control de aeropuerto. Los pilotos no hablan directamente entre sí para coordinar aterrizajes y despegues; todos reportan a la torre de control (el Mediador). La torre decide el orden, asigna pistas y comunica las instrucciones. Los pilotos solo hablan con la torre y no saben nada de los demás aviones.

---

## Estructura

```
IMediator
└── Notify(object sender, string event)

ConcreteMediator : IMediator
├── _component1: Component1
├── _component2: Component2
└── Notify(sender, event)
    ├── if event == "A" → _component2.DoC()
    └── if event == "D" → _component1.DoB(); _component2.DoC()

BaseComponent
├── _mediator: IMediator
└── SetMediator(mediator)

Component1, Component2 : BaseComponent
└── DoAction() → _mediator.Notify(this, "event")
```

---

## Código del ejemplo conceptual

```csharp
public interface IMediator
{
    void Notify(object sender, string ev);
}

class ConcreteMediator : IMediator
{
    private Component1 _component1;
    private Component2 _component2;

    public ConcreteMediator(Component1 component1, Component2 component2)
    {
        _component1 = component1;
        _component1.SetMediator(this);  // el mediador se registra en los componentes
        _component2 = component2;
        _component2.SetMediator(this);
    }

    public void Notify(object sender, string ev)
    {
        // El mediador decide qué hacer según el evento y quién lo envió
        if (ev == "A")
        {
            Console.WriteLine("Mediator reacts on A and triggers following operations:");
            _component2.DoC();
        }
        if (ev == "D")
        {
            Console.WriteLine("Mediator reacts on D and triggers following operations:");
            _component1.DoB();
            _component2.DoC();
        }
    }
}

// Componente base — solo conoce al mediador
class BaseComponent
{
    protected IMediator _mediator;

    public BaseComponent(IMediator mediator = null) { _mediator = mediator; }
    public void SetMediator(IMediator mediator) { _mediator = mediator; }
}

class Component1 : BaseComponent
{
    public void DoA()
    {
        Console.WriteLine("Component 1 does A.");
        _mediator.Notify(this, "A");  // notifica al mediador, no a Component2 directamente
    }

    public void DoB()
    {
        Console.WriteLine("Component 1 does B.");
        _mediator.Notify(this, "B");
    }
}

class Component2 : BaseComponent
{
    public void DoC()
    {
        Console.WriteLine("Component 2 does C.");
        _mediator.Notify(this, "C");
    }

    public void DoD()
    {
        Console.WriteLine("Component 2 does D.");
        _mediator.Notify(this, "D");
    }
}

// Uso:
Component1 c1 = new Component1();
Component2 c2 = new Component2();
new ConcreteMediator(c1, c2);  // el mediador conecta los componentes

Console.WriteLine("Client triggers operation A.");
c1.DoA();
// "Component 1 does A."
// "Mediator reacts on A and triggers following operations:"
// "Component 2 does C."
```

---

## Mediator en .NET — `MediatR` y el `IMediator` del proyecto

```csharp
// El patrón Mediator está en el núcleo del proyecto via Common.Messaging.IMediator

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
    Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;
}
```

### Flujo en el proyecto

```
Controller (Component)
    │
    ↓ Mediator.Send(request)  ← solo conoce al mediador
    │
Mediator (ConcreteMediator)
    │
    ↓  determina quién maneja
    │
Handler (Component1)
    │
    ↓ return response
    │
InteractorPipeline (parte del Mediator)
    │
    ↓ Mediator.Publish(response)
    │
Presenter (Component2)
    │
    ↓ _viewModel.Set(success) / .Fail(msg)
    │
Controller ← lee _viewModel
```

**Ningún componente conoce directamente a los demás:**
- El Controller no sabe de los Handlers.
- Los Handlers no saben de los Presenters.
- Los Presenters no saben del Controller.
- El Mediator (IMediator) es el único punto de conexión.

---

## Ejemplo expandido: Mediator con INotification (Observer + Mediator)

```csharp
// Request — el mensaje que inicia una operación
public sealed record InsertExampleUserRequest(string FullName, string Email, string Password)
    : IRequest<InsertExampleUserResponse>;

// Handler — el receptor del mensaje
public sealed class InsertExampleUserHandler
    : IRequestHandler<InsertExampleUserRequest, InsertExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;

    public InsertExampleUserHandler(IExampleUserRepository repo) { _repo = repo; }

    public async Task<InsertExampleUserResponse> Handle(
        InsertExampleUserRequest request, CancellationToken ct)
    {
        var user = new ExampleUser
        {
            PublicId  = Guid.NewGuid(),
            FullName  = request.FullName,
            Email     = request.Email,
        };

        await _repo.InsertAsync(user, ct);

        // El Handler retorna la respuesta — el Mediador la publica a los suscriptores
        return new InsertExampleUserSuccess(user.PublicId);
    }
}

// El InteractorPipeline del mediador toma la respuesta y la publica:
// Mediator.Publish(response) → notifica a todos los INotificationHandler<TResponse>

// Presenter — suscriptor de la respuesta
public sealed class InsertExampleUserPresenter
    : INotificationHandler<InsertExampleUserResponse>
{
    private readonly ResultViewModel<ExampleUsersController> _viewModel;

    public InsertExampleUserPresenter(ResultViewModel<ExampleUsersController> vm)
        => _viewModel = vm;

    public Task Handle(InsertExampleUserResponse notification, CancellationToken ct)
    {
        if (notification is INotFoundFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is InsertExampleUserSuccess success)
            _viewModel.OK(success.PublicId);

        return Task.CompletedTask;
    }
}
```

---

## Cuándo usar

- Cuando muchos objetos se comunican de formas complejas, resultando en dependencias difíciles de entender.
- Cuando no puedes reutilizar un componente porque depende de otros componentes.
- Cuando creates muchas subclases de componentes solo para reutilizar comportamiento básico en varios contextos.
- Sistemas de mensajería, event bus, formularios complejos de UI, coordinación de microservicios.

## Cuándo NO usar

- Cuando el mediador se convierte en un God Object que hace demasiado. Dividirlo en mediadores más pequeños.
- Para comunicación simple entre 2 objetos. El patrón agrega complejidad innecesaria.
- Cuando los componentes tienen relaciones fijas y bien definidas. La herencia puede ser suficiente.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Mediator | Patrón conductual que centraliza la comunicación entre objetos en un objeto intermediario, eliminando las dependencias directas entre ellos |
| `IMediator` | Interfaz del proyecto en `Common.Messaging` que define `Send()` y `Publish()` — punto central de comunicación |
| `Mediator.Send()` | Método que envía un Request al Handler correspondiente y devuelve la respuesta |
| `Mediator.Publish()` | Método que notifica a todos los `INotificationHandler<T>` suscritos a un tipo de respuesta |
| InteractorPipeline | Componente del proyecto que orquesta el flujo: Handler → Publish → Presenter usando el mediador |
| `INotificationHandler<T>` | Interfaz implementada por los Presenters para suscribirse a un tipo específico de respuesta |
| Acoplamiento O(N²) | Problema que el Mediator resuelve: sin él, N componentes tendrían N×(N-1) dependencias entre sí |
| `ConcreteMediator` | Implementación del mediador que conoce a todos los componentes y orquesta su interacción |
| Event bus | Variante del Mediator para comunicación asíncrona entre componentes o microservicios mediante eventos |
| God Object | Anti-patrón al que puede degenerar el Mediator si acumula demasiada lógica de coordinación |
| Desacoplamiento | Propiedad lograda con el Mediator: ningún componente (Controller, Handler, Presenter) conoce directamente a los demás |

---

*Rogelio Arriaga Gonzalez*
