# 18 — Observer

**Categoría:** Conductual

**Intención:** Define un mecanismo de suscripción para notificar a múltiples objetos sobre cualquier evento que le ocurra al objeto que están observando. También conocido como "Publish-Subscribe" (Pub/Sub) o "Event-Driven".

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.5 Behavioral Patterns: Observer

---

## El problema

Tienes un objeto `Store` que tiene stock de productos. Otros objetos (clientes, notificadores, analytics) quieren saber cuando llega nuevo stock. Sin Observer, el Store necesita conocer directamente a todos los objetos interesados, lo que genera un acoplamiento fuerte.

```csharp
// ❌ Sin Observer — Store está acoplado a todos los observadores
class Store
{
    private CustomerNotifier _notifier;
    private Analytics _analytics;
    private EmailService _email;

    public void AddStock(Product product)
    {
        _stock.Add(product);
        _notifier.Notify(product);  // Store conoce directamente a todos
        _analytics.Track(product);
        _email.SendUpdates(product);
        // Agregar un nuevo observador = modificar Store
    }
}
```

---

## Analogía

Una suscripción a un periódico. Los suscriptores (observers) se registran para recibir el periódico. El periódico (subject/publisher) no sabe ni le importa quiénes son sus suscriptores. Cuando sale una nueva edición (evento), todos los suscriptores la reciben. Los suscriptores pueden cancelar su suscripción en cualquier momento.

---

## Estructura

```
ISubject (Publisher)
├── Attach(IObserver)
├── Detach(IObserver)
└── Notify()  → llama Update() en todos los observers registrados

Subject : ISubject
├── _observers: List<IObserver>
├── State: int
└── SomeBusinessLogic() → modifica State → llama Notify()

IObserver (Subscriber)
└── Update(ISubject)

ConcreteObserverA : IObserver
└── Update(subject) → reacciona si subject.State < 3

ConcreteObserverB : IObserver
└── Update(subject) → reacciona si subject.State == 0 || >= 2
```

---

## Código del ejemplo conceptual

```csharp
public interface IObserver
{
    void Update(ISubject subject);  // recibe el sujeto para consultar su estado
}

public interface ISubject
{
    void Attach(IObserver observer);
    void Detach(IObserver observer);
    void Notify();
}

public class Subject : ISubject
{
    public int State { get; set; } = 0;
    private List<IObserver> _observers = new();

    public void Attach(IObserver observer)
    {
        Console.WriteLine("Subject: Attached an observer.");
        _observers.Add(observer);
    }

    public void Detach(IObserver observer)
    {
        _observers.Remove(observer);
        Console.WriteLine("Subject: Detached an observer.");
    }

    public void Notify()
    {
        Console.WriteLine("Subject: Notifying observers...");
        foreach (var observer in _observers)
            observer.Update(this);  // notifica a cada suscriptor
    }

    public void SomeBusinessLogic()
    {
        Console.WriteLine("Subject: I'm doing something important.");
        State = new Random().Next(0, 10);  // cambia estado
        Console.WriteLine("Subject: My state has changed to: " + State);
        Notify();  // notifica automáticamente
    }
}

// Observadores concretos — reaccionan de forma diferente al mismo evento
class ConcreteObserverA : IObserver
{
    public void Update(ISubject subject)
    {
        if ((subject as Subject).State < 3)
            Console.WriteLine("ConcreteObserverA: Reacted to the event.");
    }
}

class ConcreteObserverB : IObserver
{
    public void Update(ISubject subject)
    {
        var state = (subject as Subject).State;
        if (state == 0 || state >= 2)
            Console.WriteLine("ConcreteObserverB: Reacted to the event.");
    }
}

// Uso:
var subject   = new Subject();
var observerA = new ConcreteObserverA();
var observerB = new ConcreteObserverB();

subject.Attach(observerA);
subject.Attach(observerB);

subject.SomeBusinessLogic();  // ambos observers notificados
subject.SomeBusinessLogic();

subject.Detach(observerB);    // desuscribir B

subject.SomeBusinessLogic();  // solo A notificado
```

---

## Observer en C# — Eventos y Delegates

C# tiene Observer integrado en el lenguaje con `event` y `delegate`:

```csharp
// Usando eventos de C# — Observer nativo
public class StockMarket
{
    // El evento ES la lista de observers — el compilador lo gestiona
    public event EventHandler<StockChangedEventArgs>? StockChanged;

    private Dictionary<string, decimal> _prices = new();

    public void UpdatePrice(string symbol, decimal newPrice)
    {
        _prices[symbol] = newPrice;

        // Notifica a todos los suscriptores
        StockChanged?.Invoke(this, new StockChangedEventArgs(symbol, newPrice));
    }
}

public class StockChangedEventArgs : EventArgs
{
    public string Symbol   { get; }
    public decimal NewPrice { get; }
    public StockChangedEventArgs(string symbol, decimal price) { Symbol = symbol; NewPrice = price; }
}

// Observadores se suscriben con +=
var market = new StockMarket();

market.StockChanged += (sender, e) =>
    Console.WriteLine($"Alert: {e.Symbol} is now ${e.NewPrice}");

market.StockChanged += (sender, e) =>
{
    if (e.NewPrice > 100)
        Console.WriteLine($"Risk alert: {e.Symbol} exceeds $100!");
};

market.UpdatePrice("AAPL", 150.00m);
// "Alert: AAPL is now $150"
// "Risk alert: AAPL exceeds $100!"

// Desuscribir con -=:
void MyHandler(object? s, StockChangedEventArgs e) { /* ... */ }
market.StockChanged += MyHandler;
market.StockChanged -= MyHandler;
```

---

## Observer con `IObservable<T>` / `IObserver<T>` — Reactive Extensions (Rx)

```csharp
// La interfaz estándar de .NET para Observer:
public interface IObservable<T>
{
    IDisposable Subscribe(IObserver<T> observer);
}

public interface IObserver<T>
{
    void OnNext(T value);      // nuevo valor
    void OnError(Exception e); // error
    void OnCompleted();        // terminó
}

// Con Reactive Extensions (Rx.NET):
IObservable<int> numbers = Observable.Range(1, 10);  // publisher
numbers
    .Where(n => n % 2 == 0)   // filtrar
    .Select(n => n * n)        // transformar
    .Subscribe(
        onNext:      n => Console.WriteLine(n),     // observer
        onError:     e => Console.WriteLine(e),
        onCompleted: () => Console.WriteLine("Done")
    );
```

---

## En este proyecto — Observer en el sistema Mediator/Presenter

El sistema de Presenter es exactamente el patrón Observer:

```csharp
// Subject (Publisher): el Mediator publica respuestas
// Observer (Subscriber): los Presenters reciben y reaccionan

// Registro: suscribir el Presenter al tipo de respuesta (en WebApi/ServiceCollectionEx.cs)
services.AddScoped<INotificationHandler<GetExampleUserResponse>, GetExampleUserPresenter>();
//       ↑ Observer                      ↑ Evento (Subject notifies with this type)
//                                                     ↑ Observer concreto

// Publisher: el InteractorPipeline publica la respuesta después del Handler
// Mediator.Publish(response)  →  INotificationHandler<GetExampleUserResponse>.Handle(response)

// El Presenter (Observer) reacciona:
public sealed class GetExampleUserPresenter
    : INotificationHandler<GetExampleUserResponse>  // ← suscrito a GetExampleUserResponse
{
    private readonly ResultViewModel<ExampleUsersController> _viewModel;

    public GetExampleUserPresenter(ResultViewModel<ExampleUsersController> vm)
        => _viewModel = vm;

    // Update() equivalente — se llama cuando el Subject (Mediator) publica
    public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
    {
        if (notification is IFailure failure)
            _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<ExampleUserDto> success)
            _viewModel.Set(success);

        return Task.CompletedTask;
    }
}
```

**La diferencia con Observer clásico:** en lugar de `Attach/Detach` manuales, el DI Container gestiona las suscripciones. Cuando el Mediator publica, el container resuelve todos los `INotificationHandler<T>` registrados y los notifica.

---

## Observer vs Mediator

| Aspecto | Observer | Mediator |
|---------|---------|---------|
| Dirección | Publisher → Subscribers | Bidireccional entre componentes |
| Conocimiento | Publisher conoce la interfaz Observer | Componentes solo conocen al Mediador |
| Uso típico | Notificaciones de eventos | Coordinación compleja entre múltiples objetos |
| En .NET | `event`, `IObservable<T>` | `IMediator`, MediatR |

---

## Cuándo usar

- Cuando un cambio en un objeto requiere cambiar otros objetos, y no sabes cuántos.
- Cuando los objetos deben notificar a otros sin hacer suposiciones sobre quiénes son esos objetos.
- Cuando tienes eventos que múltiples partes del sistema necesitan manejar.
- Sistemas de eventos, UI reactiva, sincronización de datos, notificaciones.

## Cuándo NO usar

- Para relaciones donde solo hay un suscriptor fijo. La referencia directa es más clara.
- Cuando los observadores tienen efectos secundarios difíciles de predecir o depurar.
- Cuando el orden de notificación importa y debe ser controlado. Usar Chain of Responsibility.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Observer | Patrón conductual que define un mecanismo de suscripción para notificar a múltiples objetos sobre eventos del objeto observado |
| Subject (Publisher) | El objeto observado que mantiene la lista de suscriptores y los notifica cuando su estado cambia |
| Observer (Subscriber) | Objeto que recibe notificaciones del Subject cuando este cambia |
| `Attach/Detach` | Métodos del Subject para registrar y eliminar observadores de la lista de suscriptores |
| `event` | Palabra clave de C# que implementa Observer de forma nativa mediante delegates |
| `EventHandler<T>` | Delegate estándar de .NET para eventos con datos tipados (T : EventArgs) |
| `IObservable<T>` / `IObserver<T>` | Interfaces estándar de .NET para el patrón Observer reactivo (Reactive Extensions) |
| `INotificationHandler<T>` | Interfaz del proyecto que los Presenters implementan para suscribirse a tipos de respuesta del mediador |
| Pub/Sub | Publish-Subscribe: variante del Observer donde publisher y subscriber están desacoplados mediante un canal |
| `+=` / `-=` | Operadores de C# para suscribir y desuscribir handlers de un `event` |
| DI como gestor de suscripciones | En el proyecto, el contenedor registra y resuelve los `INotificationHandler<T>` en lugar de `Attach/Detach` manuales |

---

*Rogelio Arriaga Gonzalez*
