# 07 — Bridge

**Categoría:** Estructural

**Intención:** Separa una clase grande o un conjunto de clases relacionadas en dos jerarquías separadas — abstracción e implementación — que pueden desarrollarse independientemente.

---

## El problema

Tienes una clase `Shape` con subclases `Circle` y `Square`. Quieres añadir colores `Red` y `Blue`. Con herencia simple necesitarías `RedCircle`, `BlueCircle`, `RedSquare`, `BlueSquare` — la jerarquía explota exponencialmente.

```
Sin Bridge — explosión combinatoria:
Shape
├── Circle
│   ├── RedCircle
│   └── BlueCircle
└── Square
    ├── RedSquare
    └── BlueSquare
    
Agregar un color nuevo = N nuevas clases
Agregar una forma nueva = M nuevas clases
```

```
Con Bridge — dos jerarquías independientes:
Shape (abstracción)    Color (implementación)
├── Circle      ╔══════╗ ├── Red
└── Square      ║bridge║ └── Blue
                ╚══════╝
N shapes × M colors = N + M clases, no N × M
```

---

## Analogía

Un control remoto (abstracción) y un TV (implementación). El control remoto define los botones: encender, subir volumen, cambiar canal. El TV define cómo ejecutar esas acciones. Puedes tener un control remoto universal (abstracción extendida) que funciona con cualquier marca de TV (Sony, Samsung, LG — implementaciones distintas). El control y el TV se pueden extender independientemente.

---

## Estructura

```
Abstraction
├── _implementation: IImplementation   ← referencia al "otro lado"
└── Operation() → delega a _implementation.OperationImplementation()

ExtendedAbstraction : Abstraction
└── Operation() → extiende la abstracción sin cambiar la implementación

IImplementation
└── OperationImplementation(): string

ConcreteImplementationA : IImplementation
ConcreteImplementationB : IImplementation
```

---

## Código del ejemplo conceptual

```csharp
// La Abstracción define la interfaz de "alto nivel"
class Abstraction
{
    protected IImplementation _implementation;

    public Abstraction(IImplementation implementation)
    {
        _implementation = implementation;
    }

    // Delega el trabajo a la implementación
    public virtual string Operation()
    {
        return "Abstract: Base operation with:\n" +
               _implementation.OperationImplementation();
    }
}

// Extiende la abstracción sin cambiar la implementación
class ExtendedAbstraction : Abstraction
{
    public ExtendedAbstraction(IImplementation implementation) : base(implementation) { }

    public override string Operation()
    {
        return "ExtendedAbstraction: Extended operation with:\n" +
               _implementation.OperationImplementation();  // mismo _implementation
    }
}

// La implementación define operaciones primitivas
public interface IImplementation
{
    string OperationImplementation();
}

// Implementaciones concretas — pueden cambiar sin afectar la abstracción
class ConcreteImplementationA : IImplementation
{
    public string OperationImplementation() =>
        "ConcreteImplementationA: The result in platform A.\n";
}

class ConcreteImplementationB : IImplementation
{
    public string OperationImplementation() =>
        "ConcreteImplementationB: The result in platform B.\n";
}

// El cliente combina abstracción e implementación libremente
Client client = new Client();

// Abstracción base con implementación A
var abstraction = new Abstraction(new ConcreteImplementationA());
client.ClientCode(abstraction);
// "Abstract: Base operation with: ConcreteImplementationA: The result in platform A."

// Abstracción extendida con implementación B — combinación diferente
abstraction = new ExtendedAbstraction(new ConcreteImplementationB());
client.ClientCode(abstraction);
// "ExtendedAbstraction: Extended operation with: ConcreteImplementationB: The result in platform B."
```

---

## Ejemplo real: notificaciones multiplataforma

```csharp
// Implementación: el "cómo" se envía
public interface INotificationSender
{
    Task SendAsync(string to, string subject, string body);
}

public class EmailSender : INotificationSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        // enviar via SMTP
    }
}

public class SmsSender : INotificationSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        // enviar via Twilio
    }
}

public class SlackSender : INotificationSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        // enviar via Slack API
    }
}

// Abstracción: el "qué" se envía
public abstract class Notification
{
    protected readonly INotificationSender _sender;

    protected Notification(INotificationSender sender) { _sender = sender; }

    public abstract Task NotifyAsync(string recipient);
}

// Tipos de notificación — independientes del canal
public class WelcomeNotification : Notification
{
    private readonly string _appName;

    public WelcomeNotification(INotificationSender sender, string appName) : base(sender)
    {
        _appName = appName;
    }

    public override Task NotifyAsync(string recipient) =>
        _sender.SendAsync(
            recipient,
            $"Bienvenido a {_appName}",
            $"Gracias por registrarte en {_appName}.");
}

public class PasswordResetNotification : Notification
{
    private readonly string _token;

    public PasswordResetNotification(INotificationSender sender, string token) : base(sender)
    {
        _token = token;
    }

    public override Task NotifyAsync(string recipient) =>
        _sender.SendAsync(
            recipient,
            "Restablecer contraseña",
            $"Usa este código: {_token}");
}

// Uso — combina libremente cualquier notificación con cualquier canal:
var welcomeEmail = new WelcomeNotification(new EmailSender(), "MiApp");
var resetSms     = new PasswordResetNotification(new SmsSender(), "123456");

await welcomeEmail.NotifyAsync("usuario@ejemplo.com");
await resetSms.NotifyAsync("+52-555-1234567");
```

Agregar un nuevo canal (PushNotification) solo requiere implementar `INotificationSender`. Agregar un nuevo tipo de notificación solo requiere heredar de `Notification`. **Cero cambios en el código existente.**

---

## En este proyecto

```csharp
// El patrón Bridge es la base del sistema de repositorios:

// Abstracción: qué operaciones de datos existen
public interface IExampleUserRepository
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);
    Task InsertAsync(ExampleUser user, CancellationToken ct = default);
    // ...
}

// Implementación: cómo se ejecutan esas operaciones (con Dapper + PostgreSQL)
public sealed class ExampleUserRepository : IExampleUserRepository
{
    private readonly ExampleUsersSql _sql;

    public ExampleUserRepository(ExampleUsersSql sql) { _sql = sql; }

    public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct) =>
        _sql.GetByPublicIdAsync(publicId, ct);

    // ...
}

// El Handler habla con IExampleUserRepository (abstracción)
// No sabe nada de PostgreSQL (implementación)
// Bridge: la abstracción (interfaz del repositorio) está "puenteada" a la implementación concreta
```

Este es exactamente el Bridge: `IExampleUserRepository` (abstracción) ↔ `ExampleUserRepository` (implementación). Podrías cambiar a MongoDB creando `MongoUserRepository : IExampleUserRepository` sin tocar ningún handler.

---

## Cuándo usar

- Cuando quieres dividir una clase monolítica que tiene varias variantes de funcionalidad.
- Cuando quieres extender abstracción e implementación de forma independiente.
- Cuando quieres cambiar la implementación en tiempo de ejecución.
- Cuando tienes una jerarquía que explota combinatoriamente (NxM subclases → N+M).

## Cuándo NO usar

- Cuando solo tienes una implementación — el bridge es innecesario.
- Para código simple donde la abstracción y la implementación nunca van a cambiar independientemente.
- Cuando la indirección extra añade más confusión que claridad.


---

*Rogelio Arriaga Gonzalez*
