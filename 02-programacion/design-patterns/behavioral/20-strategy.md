# 20 — Strategy

**Categoría:** Conductual

**Intención:** Define una familia de algoritmos, los encapsula en clases separadas y hace sus objetos intercambiables. Strategy permite cambiar el algoritmo usado dentro de un objeto en runtime.

---

## El problema

Tienes un `Navigator` GPS que calcula rutas. Inicialmente solo tiene rutas en auto. Luego agregan: a pie, en bici, por bus, rutas turísticas. El código del Navigator crece con if/switch enormes y la clase se vuelve difícil de mantener.

```csharp
// ❌ Sin Strategy — Navigator contiene todos los algoritmos
class Navigator
{
    public void BuildRoute(Point from, Point to, string type)
    {
        if (type == "car")
            { /* lógica de rutas en auto */ }
        else if (type == "walking")
            { /* lógica de rutas a pie */ }
        else if (type == "cycling")
            { /* lógica de rutas en bici */ }
        // Agregar "bus" = modificar este método → viola Open/Closed Principle
    }
}
```

---

## Analogía

Diferentes modos de transporte hacia el aeropuerto. Todos resuelven el mismo problema (llegar al aeropuerto) pero con algoritmos diferentes (taxi, metro, bus, bici, caminar). Seleccionas la estrategia según tiempo, costo, o preferencia — sin cambiar el objetivo final.

---

## Estructura

```
Context
├── _strategy: IStrategy       ← referencia a la estrategia actual
├── SetStrategy(IStrategy)     ← cambiar estrategia en runtime
└── DoSomeBusinessLogic()      ← usa _strategy.DoAlgorithm()

IStrategy
└── DoAlgorithm(data): object

ConcreteStrategyA : IStrategy
└── DoAlgorithm(data) → ordena normal

ConcreteStrategyB : IStrategy
└── DoAlgorithm(data) → ordena inverso
```

---

## Código del ejemplo conceptual

```csharp
// El Context — usa la estrategia, no sabe cuál es
class Context
{
    private IStrategy _strategy;

    public Context() { }

    public Context(IStrategy strategy) { _strategy = strategy; }

    // Permite cambiar la estrategia en runtime
    public void SetStrategy(IStrategy strategy) { _strategy = strategy; }

    public void DoSomeBusinessLogic()
    {
        Console.WriteLine("Context: Sorting data using the strategy...");
        // Delega el algoritmo a la estrategia — no sabe cómo ordena
        var result = _strategy.DoAlgorithm(new List<string> { "a", "b", "c", "d", "e" });

        foreach (var element in result as List<string>)
            Console.Write(element + ",");
        Console.WriteLine();
    }
}

// Interfaz común para todas las estrategias
public interface IStrategy
{
    object DoAlgorithm(object data);
}

// Estrategia A — orden ascendente
class ConcreteStrategyA : IStrategy
{
    public object DoAlgorithm(object data)
    {
        var list = data as List<string>;
        list.Sort();        // orden normal
        return list;
    }
}

// Estrategia B — orden descendente
class ConcreteStrategyB : IStrategy
{
    public object DoAlgorithm(object data)
    {
        var list = data as List<string>;
        list.Sort();
        list.Reverse();     // orden inverso
        return list;
    }
}

// Uso — cambiar estrategia en runtime:
var context = new Context();

Console.WriteLine("Client: Strategy is set to normal sorting.");
context.SetStrategy(new ConcreteStrategyA());
context.DoSomeBusinessLogic();  // a,b,c,d,e

Console.WriteLine("Client: Strategy is set to reverse sorting.");
context.SetStrategy(new ConcreteStrategyB());
context.DoSomeBusinessLogic();  // e,d,c,b,a
```

---

## Strategy con lambdas — la forma moderna en C#

En C# moderno, las estrategias simples se expresan con `Func<T>`:

```csharp
// En lugar de clases separadas para cada estrategia simple:
class Sorter
{
    private Func<IEnumerable<int>, IEnumerable<int>> _sortStrategy;

    public Sorter(Func<IEnumerable<int>, IEnumerable<int>> strategy)
    {
        _sortStrategy = strategy;
    }

    public IEnumerable<int> Sort(IEnumerable<int> data) => _sortStrategy(data);
}

// Uso — estrategias como lambdas:
var ascendingSorter  = new Sorter(data => data.OrderBy(x => x));
var descendingSorter = new Sorter(data => data.OrderByDescending(x => x));
var randomSorter     = new Sorter(data => data.OrderBy(_ => Random.Shared.Next()));

var numbers = new[] { 5, 3, 1, 4, 2 };
Console.WriteLine(string.Join(",", ascendingSorter.Sort(numbers)));   // 1,2,3,4,5
Console.WriteLine(string.Join(",", descendingSorter.Sort(numbers)));  // 5,4,3,2,1
```

---

## Ejemplo real: procesamiento de pagos con Strategy

```csharp
// Interfaz común
public interface IPaymentStrategy
{
    Task<PaymentResult> ProcessAsync(decimal amount, PaymentDetails details);
    string Name { get; }
}

// Estrategias concretas
public class CreditCardStrategy : IPaymentStrategy
{
    public string Name => "Credit Card";

    public async Task<PaymentResult> ProcessAsync(decimal amount, PaymentDetails details)
    {
        // Lógica específica de tarjeta de crédito
        Console.WriteLine($"Processing ${amount} via Credit Card ending in {details.CardLast4}");
        return new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() };
    }
}

public class PayPalStrategy : IPaymentStrategy
{
    public string Name => "PayPal";

    public async Task<PaymentResult> ProcessAsync(decimal amount, PaymentDetails details)
    {
        Console.WriteLine($"Processing ${amount} via PayPal account {details.PayPalEmail}");
        return new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() };
    }
}

public class CryptoStrategy : IPaymentStrategy
{
    public string Name => "Crypto";

    public async Task<PaymentResult> ProcessAsync(decimal amount, PaymentDetails details)
    {
        Console.WriteLine($"Processing ${amount} in BTC to wallet {details.WalletAddress}");
        return new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() };
    }
}

// El Contexto — usa cualquier estrategia sin saber cuál es
public class PaymentProcessor
{
    private IPaymentStrategy _strategy;

    public PaymentProcessor(IPaymentStrategy strategy) { _strategy = strategy; }

    public void SetStrategy(IPaymentStrategy strategy) { _strategy = strategy; }

    public async Task<PaymentResult> ProcessAsync(decimal amount, PaymentDetails details)
    {
        Console.WriteLine($"Using strategy: {_strategy.Name}");
        return await _strategy.ProcessAsync(amount, details);
    }
}

// Uso — cambiar estrategia según elección del usuario:
var processor = new PaymentProcessor(new CreditCardStrategy());
await processor.ProcessAsync(99.99m, new PaymentDetails { CardLast4 = "4242" });

// El usuario elige PayPal:
processor.SetStrategy(new PayPalStrategy());
await processor.ProcessAsync(99.99m, new PaymentDetails { PayPalEmail = "user@example.com" });
```

---

## Strategy con DI — registro de múltiples estrategias

```csharp
// Registrar todas las estrategias en DI
services.AddScoped<IPaymentStrategy, CreditCardStrategy>();
services.AddScoped<IPaymentStrategy, PayPalStrategy>();
services.AddScoped<IPaymentStrategy, CryptoStrategy>();

// O mejor, usar un factory para seleccionar:
services.AddScoped<Func<string, IPaymentStrategy>>(sp => name =>
    name switch
    {
        "credit_card" => sp.GetRequiredService<CreditCardStrategy>(),
        "paypal"      => sp.GetRequiredService<PayPalStrategy>(),
        "crypto"      => sp.GetRequiredService<CryptoStrategy>(),
        _             => throw new ArgumentException($"Unknown payment: {name}")
    });
```

---

## En este proyecto

```csharp
// Cada Presenter es una Estrategia de presentación para su caso de uso:
// Mismo interfaz (INotificationHandler<TResponse>), comportamiento diferente por acción

// Estrategia 1: GetExampleUserPresenter
public sealed class GetExampleUserPresenter : INotificationHandler<GetExampleUserResponse>
{
    public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
    {
        if (notification is INotFoundFailure failure) _viewModel.Fail(failure.Message);
        else if (notification is ISuccess<ExampleUserDto> success) _viewModel.Set(success);
        return Task.CompletedTask;
    }
}

// Estrategia 2: InsertExampleUserPresenter
public sealed class InsertExampleUserPresenter : INotificationHandler<InsertExampleUserResponse>
{
    public Task Handle(InsertExampleUserResponse notification, CancellationToken ct)
    {
        if (notification is IConflictFailure failure) _viewModel.Fail(failure.Message);
        else if (notification is InsertExampleUserSuccess success) _viewModel.OK(success.PublicId);
        return Task.CompletedTask;
    }
}

// El DI Container actúa como el Context — elige la estrategia correcta
// según el tipo de respuesta que se publique.
```

---

## Strategy vs State

| Aspecto | Strategy | State |
|---------|---------|-------|
| Quién elige el algoritmo | El cliente explícitamente | El objeto mismo (transición interna) |
| Conocimiento entre estrategias | No se conocen | Los estados pueden conocerse |
| Propósito | Hacer algoritmos intercambiables | Cambiar comportamiento según estado interno |
| Cuándo usar | El cliente necesita elegir el algoritmo | El objeto tiene ciclo de vida con estados |

---

## Cuándo usar

- Cuando quieres diferentes variantes de un algoritmo y poder cambiar entre ellas en runtime.
- Cuando tienes muchas clases similares que solo difieren en cómo ejecutan cierto comportamiento.
- Cuando quieres aislar la lógica de negocio de los detalles de implementación del algoritmo.
- Cuando una clase tiene un if/switch enorme sobre variantes de un algoritmo.

## Cuándo NO usar

- Si solo tienes un algoritmo que nunca cambia — no necesitas la abstracción.
- Si el cliente no necesita saber de las diferencias entre estrategias.
- Para comportamientos muy simples — lambdas o funciones locales son suficientes.


---

*Rogelio Arriaga Gonzalez*
