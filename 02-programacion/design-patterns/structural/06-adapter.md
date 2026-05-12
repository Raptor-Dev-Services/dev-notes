# 06 — Adapter

**Categoría:** Estructural

**Intención:** Permite que objetos con interfaces incompatibles colaboren. Convierte la interfaz de una clase en otra interfaz que el cliente espera.

---

## El problema

Tienes una clase que espera datos en formato XML, pero la librería que quieres usar devuelve JSON. No puedes modificar ninguna de las dos. Necesitas un "adaptador" entre ellas.

```csharp
// El cliente espera ITarget.GetRequest()
// La librería tiene Adaptee.GetSpecificRequest() — distinto nombre, distinto formato

// ❌ Sin Adapter — incompatible
Adaptee adaptee = new Adaptee();
ITarget target = adaptee;  // ← ERROR: no implementa ITarget
```

---

## Analogía

Un adaptador de enchufes de viaje. Tu laptop tiene un enchufe tipo A (EE.UU.) pero el tomacorriente del hotel en Europa es tipo C. El adaptador de viaje no cambia la funcionalidad de tu laptop ni la del tomacorriente — solo conecta interfaces incompatibles.

---

## Estructura

```
ITarget (interfaz que espera el cliente)
└── GetRequest(): string

Adaptee (clase existente con interfaz incompatible)
└── GetSpecificRequest(): string

Adapter : ITarget
├── _adaptee: Adaptee
└── GetRequest() → traduce → _adaptee.GetSpecificRequest()

Client → ITarget (nunca sabe que hay un Adaptee)
```

---

## Código del ejemplo conceptual

```csharp
// La interfaz que el cliente usa
public interface ITarget
{
    string GetRequest();
}

// La clase existente que no podemos modificar (librería de terceros)
class Adaptee
{
    public string GetSpecificRequest()
    {
        return "Specific request.";  // formato incompatible
    }
}

// El adaptador: implementa ITarget y delega a Adaptee
class Adapter : ITarget
{
    private readonly Adaptee _adaptee;

    public Adapter(Adaptee adaptee)
    {
        _adaptee = adaptee;
    }

    public string GetRequest()
    {
        // Traduce la llamada del cliente al método del Adaptee
        return $"This is '{_adaptee.GetSpecificRequest()}'";
    }
}

// Uso:
Adaptee adaptee = new Adaptee();
ITarget target  = new Adapter(adaptee);  // el cliente solo ve ITarget

Console.WriteLine("Adaptee interface is incompatible with the client.");
Console.WriteLine("But with adapter client can call it's method.");
Console.WriteLine(target.GetRequest());
// "This is 'Specific request.'"
```

---

## Dos variantes: Object Adapter vs Class Adapter

### Object Adapter (composición — recomendado)

```csharp
// Usa composición — contiene una instancia del Adaptee
class ObjectAdapter : ITarget
{
    private readonly Adaptee _adaptee;  // composición

    public ObjectAdapter(Adaptee adaptee) { _adaptee = adaptee; }

    public string GetRequest() => _adaptee.GetSpecificRequest();
}
// Ventaja: puede adaptar subclases de Adaptee también
// Desventaja: no puede sobreescribir métodos del Adaptee
```

### Class Adapter (herencia — solo cuando tiene sentido)

```csharp
// Usa herencia múltiple (solo disponible en lenguajes que la soportan)
// En C#: hereda del Adaptee E implementa ITarget
class ClassAdapter : Adaptee, ITarget
{
    public string GetRequest()
    {
        return GetSpecificRequest();  // llama al método heredado
    }
}
// Ventaja: puede sobreescribir comportamiento del Adaptee
// Desventaja: acoplado a la clase concreta Adaptee (no funciona con subclases)
```

**En C#: usar Object Adapter (composición) es la norma** — no existe herencia múltiple de clases.

---

## Ejemplo real: integrar una librería de pago

```csharp
// Tu sistema espera IPaymentProcessor
public interface IPaymentProcessor
{
    Task<PaymentResult> ProcessAsync(decimal amount, string currency, string cardToken);
}

// La librería de Stripe tiene su propia API — incompatible
public class StripeClient
{
    public Task<StripeCharge> CreateChargeAsync(StripeChargeOptions options)
    { /* ... */ }
}

// Adapter: adapta StripeClient a IPaymentProcessor
public sealed class StripePaymentAdapter : IPaymentProcessor
{
    private readonly StripeClient _stripe;

    public StripePaymentAdapter(StripeClient stripe) { _stripe = stripe; }

    public async Task<PaymentResult> ProcessAsync(decimal amount, string currency, string cardToken)
    {
        var options = new StripeChargeOptions
        {
            Amount   = (long)(amount * 100),  // Stripe usa centavos
            Currency = currency.ToLower(),
            Source   = cardToken,
        };

        var charge = await _stripe.CreateChargeAsync(options);

        return new PaymentResult
        {
            Success       = charge.Status == "succeeded",
            TransactionId = charge.Id,
        };
    }
}

// Ahora el sistema usa IPaymentProcessor sin saber que es Stripe:
services.AddScoped<IPaymentProcessor>(sp =>
    new StripePaymentAdapter(new StripeClient(apiKey)));
```

---

## En este proyecto

```csharp
// MainDapperDbConnection adapta Dapper/Npgsql al sistema de logging del proyecto.
// La "interfaz" que el proyecto espera es: métodos con logging de performance automático.
// La "interfaz" que Dapper ofrece es: métodos de extensión en IDbConnection.

public sealed class MainDapperDbConnection
{
    private readonly MainDbConnectionFactory _factory;
    private readonly ILogger<MainDapperDbConnection> _logger;

    // Adapter: "GetRequest()" equivalente — traduce QueryAsync con logging
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql, object? param = null,
        CancellationToken cancellationToken = default)
    {
        using var conn = _factory.OpenConnection();   // abre conexión (Adaptee)
        var sw = Stopwatch.StartNew();

        var result = await conn.QueryAsync<T>(sql, param);  // Dapper (Adaptee)

        sw.Stop();
        _logger.LogDebug("SQL QueryAsync {Elapsed}ms", sw.ElapsedMilliseconds);  // logging añadido

        return result;
    }
}
// El código que usa MainDapperDbConnection nunca habla con Dapper directamente.
// MainDapperDbConnection ES el Adapter.
```

---

## Cuándo usar

- Cuando quieres usar una clase existente pero su interfaz no es compatible con el resto del código.
- Cuando quieres crear una clase reutilizable que coopere con clases que no tienen interfaces compatibles.
- Cuando necesitas integrar librerías de terceros sin modificarlas.
- Cuando migras un sistema antiguo — el Adapter permite que el código nuevo y viejo coexistan.

## Cuándo NO usar

- Cuando puedes modificar directamente la interfaz del Adaptee.
- Cuando la diferencia entre interfaces es tan grande que el Adapter se convierte en una traducción compleja — puede ser mejor refactorizar.
- Cuando agregas tanta lógica en el Adapter que se vuelve un God Object — mantenerlo enfocado en la traducción.


---

*Rogelio Arriaga Gonzalez*
