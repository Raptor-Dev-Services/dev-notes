# 14 — Command

**Categoría:** Conductual

**Intención:** Convierte una petición en un objeto independiente que contiene toda la información sobre la petición. Esta transformación te permite parametrizar métodos con diferentes peticiones, retrasar o poner en cola la ejecución de una petición y soportar operaciones que se puedan deshacer.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.5 Behavioral Patterns: Command

---

## El problema

Tienes una barra de herramientas con botones. Cada botón necesita hacer algo diferente (guardar, abrir, imprimir). Con herencia directa: `SaveButton`, `OpenButton`, `PrintButton`. Pero luego quieres los mismos comandos en el menú, en atajos de teclado, y en una cola de deshacer. El mismo "Guardar" necesita ejecutarse desde 3 lugares. La lógica se duplica.

---

## Analogía

Un restaurante. El mesero no cocina; toma tu pedido y lo escribe en una nota (el comando). La nota viaja a la cocina (el receptor). El chef puede ejecutar el pedido inmediatamente o más tarde. El gerente puede cancelar el pedido antes de que llegue a la cocina. La nota es el objeto que encapsula toda la información del pedido.

---

## Estructura

```
ICommand
└── Execute()

SimpleCommand : ICommand
└── Execute() → hace algo simple directamente

ComplexCommand : ICommand
├── _receiver: Receiver
├── _a, _b: string  ← datos del comando
└── Execute() → _receiver.DoSomething(_a); _receiver.DoSomethingElse(_b)

Receiver
├── DoSomething(a)
└── DoSomethingElse(b)

Invoker
├── _onStart: ICommand
├── _onFinish: ICommand
├── SetOnStart(command)
├── SetOnFinish(command)
└── DoSomethingImportant()
    ├── _onStart.Execute()
    ├── [hace su trabajo]
    └── _onFinish.Execute()
```

---

## Código del ejemplo conceptual

```csharp
public interface ICommand
{
    void Execute();
}

// Comando simple — hace el trabajo directamente
class SimpleCommand : ICommand
{
    private string _payload;

    public SimpleCommand(string payload) { _payload = payload; }

    public void Execute()
    {
        Console.WriteLine($"SimpleCommand: printing ({_payload})");
    }
}

// Comando complejo — delega al Receiver
class ComplexCommand : ICommand
{
    private Receiver _receiver;
    private string _a;
    private string _b;

    public ComplexCommand(Receiver receiver, string a, string b)
    {
        _receiver = receiver;
        _a = a;
        _b = b;
    }

    public void Execute()
    {
        Console.WriteLine("ComplexCommand: Complex stuff done by receiver.");
        _receiver.DoSomething(_a);
        _receiver.DoSomethingElse(_b);
    }
}

// El Receiver contiene la lógica real del negocio
class Receiver
{
    public void DoSomething(string a)    => Console.WriteLine($"Receiver: Working on ({a}).");
    public void DoSomethingElse(string b) => Console.WriteLine($"Receiver: Also working on ({b}).");
}

// El Invoker ejecuta comandos — no sabe qué hace cada comando
class Invoker
{
    private ICommand _onStart;
    private ICommand _onFinish;

    public void SetOnStart(ICommand command)  { _onStart  = command; }
    public void SetOnFinish(ICommand command) { _onFinish = command; }

    public void DoSomethingImportant()
    {
        Console.WriteLine("Invoker: Before I begin...");
        _onStart?.Execute();

        Console.WriteLine("Invoker: ...doing important work...");

        Console.WriteLine("Invoker: After I finish...");
        _onFinish?.Execute();
    }
}

// Uso — parametrizar el Invoker con diferentes comandos:
var invoker  = new Invoker();
var receiver = new Receiver();

invoker.SetOnStart(new SimpleCommand("Say Hi!"));
invoker.SetOnFinish(new ComplexCommand(receiver, "Send email", "Save report"));

invoker.DoSomethingImportant();
```

---

## Implementar Undo/Redo

```csharp
public interface ICommand
{
    void Execute();
    void Undo();  // ← para soportar deshacer
}

public class MoveCommand : ICommand
{
    private readonly IMovable _target;
    private readonly Point _delta;
    private Point _previousPosition;

    public MoveCommand(IMovable target, Point delta)
    {
        _target = target;
        _delta  = delta;
    }

    public void Execute()
    {
        _previousPosition = _target.Position;  // guardar estado anterior
        _target.Move(_delta);
    }

    public void Undo()
    {
        _target.Position = _previousPosition;  // restaurar estado anterior
    }
}

// Historial de comandos — el "Caretaker" del Memento
public class CommandHistory
{
    private readonly Stack<ICommand> _history = new();

    public void Execute(ICommand command)
    {
        command.Execute();
        _history.Push(command);  // guardar para posible undo
    }

    public void Undo()
    {
        if (_history.Count > 0)
            _history.Pop().Undo();
    }
}
```

---

## Command en ASP.NET Core — MVC Actions como Commands

Cada acción HTTP es implícitamente un comando:

```csharp
// El request HTTP es el Command — contiene toda la información de la petición
// El Controller es el Invoker — recibe el command y lo envía
// El Handler es el Receiver — tiene la lógica real
[HttpPost]
public async Task<IActionResult> Insert(
    [FromBody] InsertExampleUserBody body,   // ← Command data
    CancellationToken ct = default)
{
    // Crear el Command object
    var request = new InsertExampleUserRequest(body.FullName, body.Email, body.Password);

    // Invoker envía el Command al Receiver (Handler)
    _ = await Mediator.Send(request, ct);

    return _viewModel.IsSuccess
        ? Created($"/api/example/users/{_viewModel.Data}", _viewModel)
        : StatusCode(500, _viewModel);
}
```

---

## En este proyecto — CQRS como Command Pattern

CQRS (Command Query Responsibility Segregation) es directamente el Command Pattern aplicado a la arquitectura:

```csharp
// Cada Request ES un Command (o Query) — un objeto que encapsula una petición
public sealed record GetExampleUserRequest(Guid PublicId)
    : IRequest<GetExampleUserResponse>;
//  ↑ Command — contiene todos los datos necesarios para ejecutar la operación

public sealed record InsertExampleUserRequest(
    string FullName, string Email, string Password)
    : IRequest<InsertExampleUserResponse>;
//  ↑ Command de escritura — todos los datos necesarios para insertar

// El Handler es el Receiver — tiene la lógica real
public sealed class InsertExampleUserHandler
    : IRequestHandler<InsertExampleUserRequest, InsertExampleUserResponse>
{
    public async Task<InsertExampleUserResponse> Handle(
        InsertExampleUserRequest request, CancellationToken ct)
    {
        // El Receiver ejecuta la lógica del comando
        var user = new ExampleUser
        {
            FullName = request.FullName,   // datos del Command
            Email    = request.Email,
        };
        await _repo.InsertAsync(user, ct);
        return new InsertExampleUserSuccess(user.PublicId);
    }
}

// El Controller es el Invoker
_ = await Mediator.Send(request, ct);  // Invoker → envía Command → Handler (Receiver)
```

**Ventajas en este proyecto:**
- Cada request es un objeto serializable. Fácil de loguear, auditar y poner en cola.
- Los handlers son puros. Fáciles de testear en aislamiento.
- El controller (Invoker) no sabe qué hace el handler (Receiver).

---

## Cuándo usar

- Cuando quieres parametrizar objetos con operaciones.
- Cuando quieres poner en cola operaciones, programarlas para ejecución diferida.
- Cuando necesitas implementar operaciones reversibles (Undo/Redo).
- Cuando quieres auditar o loguear operaciones. Cada comando es un registro.
- CQRS: separar escrituras (Commands) de lecturas (Queries).

## Cuándo NO usar

- Para operaciones simples que no requieren deshacer, diferir, o auditar.
- Cuando la indirección adicional no aporte claridad ni flexibilidad.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Command | Patrón conductual que convierte una petición en un objeto independiente que contiene toda la información necesaria para ejecutarla |
| Invoker | Objeto que ejecuta los comandos; no sabe qué hace cada comando internamente |
| Receiver | Objeto que contiene la lógica real del negocio; el comando complejo delega en él |
| ICommand | Interfaz que declara el método `Execute()` (y opcionalmente `Undo()`) que todos los comandos implementan |
| Undo/Redo | Funcionalidad que el Command pattern facilita: cada comando puede guardar el estado anterior para revertir la operación |
| `CommandHistory` | Pila (Stack) que almacena los comandos ejecutados para soportar deshacer operaciones |
| CQRS | Command Query Responsibility Segregation: patrón arquitectónico que separa las operaciones de escritura (Commands) de las de lectura (Queries) |
| `IRequest<TResponse>` | Interfaz del proyecto que representa un Command (o Query) — un objeto que encapsula una petición para el mediador |
| Controller como Invoker | En el proyecto, el controller envía el Request (Command) al mediador sin saber qué Handler lo ejecuta |
| Handler como Receiver | En el proyecto, el Handler contiene la lógica real y es el Receiver del patrón Command |
| Serialización de comandos | Ventaja del Command pattern: al ser objetos, los requests se pueden loguear, auditar y poner en cola |

---

*Rogelio Arriaga Gonzalez*
