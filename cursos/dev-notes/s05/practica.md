# Hands-On S05  -  Patrones de Comportamiento

## Dev Notes - Proyecto de consola individual

**Proyecto:** `dotnet new console -n S05-Kata`

```
S05-Kata/
|-- Comportamiento/
|   |-- ShippingStrategies.cs
|   |-- OrderObserver.cs
|   |-- OrderState.cs
|   `-- OrderCommands.cs
`-- Program.cs
```

| Kata | Patrón | Tiempo |
|------|--------|--------|
| 1 | Strategy | 20 min |
| 2 | Observer | 20 min |
| 3 | State | 25 min |
| 4 | Command + Undo | 20 min |

---

## Kata 1  -  Strategy - Skeleton (parte 1)

**Archivo:** `Comportamiento/ShippingStrategies.cs`

```csharp
public interface IShippingCostStrategy
{
    decimal CalcularCosto(decimal subtotal, decimal pesoKg);
}

public sealed class StandardShipping : IShippingCostStrategy
{
    // TODO: $5.00 fijo + $1.50 por kg
    public decimal CalcularCosto(decimal subtotal, decimal pesoKg) =>
        throw new NotImplementedException();
}

public sealed class ExpressShipping : IShippingCostStrategy
{
    // TODO: $15.00 fijo + $3.00 por kg
    public decimal CalcularCosto(decimal subtotal, decimal pesoKg) =>
        throw new NotImplementedException();
}
```

---

## Kata 1  -  Strategy - Skeleton (parte 2)

```csharp
public sealed class FreeShipping : IShippingCostStrategy
{
    private readonly decimal _umbralMinimo;

    public FreeShipping(decimal umbralMinimo) =>
        _umbralMinimo = umbralMinimo;

    // TODO: $0.00 si subtotal >= _umbralMinimo; sino $8.00 fijo
    public decimal CalcularCosto(decimal subtotal, decimal pesoKg) =>
        throw new NotImplementedException();
}
```

---

## Kata 1  -  Strategy - Program.cs

**Program.cs (no modificar):**

```csharp
Console.WriteLine("--- Strategy ---");
var pedidos = new[] { (sub: 120m, peso: 2.5m), (sub: 40m, peso: 1.0m) };
IShippingCostStrategy[] estrategias =
[
    new StandardShipping(),
    new ExpressShipping(),
    new FreeShipping(umbralMinimo: 100m)
];

foreach (var (sub, peso) in pedidos)
{
    Console.WriteLine($"Subtotal: ${sub} | Peso: {peso}kg");
    foreach (var e in estrategias)
        Console.WriteLine($"  {e.GetType().Name,-20}: ${e.CalcularCosto(sub, peso):F2}");
}
```

---

## Kata 1  -  Strategy - Salida esperada y criterios

**Salida esperada:**

```
--- Strategy ---
Subtotal: $120 | Peso: 2.5kg
  StandardShipping    : $8.75
  ExpressShipping     : $22.50
  FreeShipping        : $0.00
Subtotal: $40 | Peso: 1.0kg
  StandardShipping    : $6.50
  ExpressShipping     : $18.00
  FreeShipping        : $8.00
```

**Criterios de aceptación:**
- [ ] StandardShipping: 5 + (1.5 × peso)
- [ ] ExpressShipping: 15 + (3 × peso)
- [ ] FreeShipping: 0 si subtotal >= umbral, sino 8
- [ ] Agregar una 4ª estrategia no requiere modificar Program.cs

---

## Kata 2  -  Observer - Skeleton (parte 1)

**Archivo:** `Comportamiento/OrderObserver.cs`

```csharp
public sealed record OrdenColocada(Guid OrdenId, string Cliente, decimal Total);

public interface IOrdenObserver
{
    Task HandleAsync(OrdenColocada evento);
}

public sealed class EmailNotificador : IOrdenObserver
{
    // TODO: await Task.Delay(10)
    // TODO: imprimir "[EMAIL] Orden {evento.OrdenId.ToString()[..8]}
    //       confirmada para {evento.Cliente}"
    public async Task HandleAsync(OrdenColocada evento)
    {
        await Task.Delay(10);
        throw new NotImplementedException();
    }
}
```

---

## Kata 2  -  Observer - Skeleton (parte 2)

```csharp
public sealed class InventarioActualizador : IOrdenObserver
{
    // TODO: await Task.Delay(5)
    // TODO: imprimir "[INVENTARIO] Reservando stock para orden
    //       {evento.OrdenId.ToString()[..8]}"
    public async Task HandleAsync(OrdenColocada evento)
    {
        await Task.Delay(5);
        throw new NotImplementedException();
    }
}

public sealed class AuditLogger : IOrdenObserver
{
    // TODO: imprimir "[AUDIT] {DateTime.UtcNow:HH:mm:ss} | OrdenColocada
    //       | Id:{evento.OrdenId.ToString()[..8]} | ${evento.Total}"
    public Task HandleAsync(OrdenColocada evento) =>
        throw new NotImplementedException();
}
```

---

## Kata 2  -  Observer - Skeleton (parte 3)

```csharp
public sealed class OrdenPublisher
{
    private readonly List<IOrdenObserver> _observers = [];

    public void Suscribir(IOrdenObserver observer) =>
        _observers.Add(observer);

    // TODO: llamar HandleAsync en cada observer
    public async Task PublicarAsync(OrdenColocada evento)
    {
        foreach (var obs in _observers)
            throw new NotImplementedException();
    }
}
```

---

## Kata 2  -  Observer - Program.cs

**Program.cs (no modificar):**

```csharp
var publisher = new OrdenPublisher();
publisher.Suscribir(new EmailNotificador());
publisher.Suscribir(new InventarioActualizador());
publisher.Suscribir(new AuditLogger());

Console.WriteLine("--- Observer ---");
await publisher.PublicarAsync(
    new OrdenColocada(Guid.NewGuid(), "Ana López", 349.99m));
```

---

## Kata 2  -  Observer - Salida esperada y criterios

**Salida esperada** (OrdenId varía  -  primeros 8 caracteres):

```
--- Observer ---
[EMAIL] Orden a1b2c3d4 confirmada para Ana López
[INVENTARIO] Reservando stock para orden a1b2c3d4
[AUDIT] 14:05:33 | OrdenColocada | Id:a1b2c3d4 | $349.99
```

**Criterios de aceptación:**
- [ ] Los 3 observers se ejecutan en orden
- [ ] `OrdenPublisher` no tiene `Console.WriteLine`
- [ ] Agregar un 4º observer no toca `OrdenPublisher` ni los demás

---

## Kata 3  -  State - Skeleton (parte 1)

**Archivo:** `Comportamiento/OrderState.cs`

```csharp
// Result mínimo para este kata
public sealed record Result(bool IsSuccess, string? Error = null)
{
    public static Result OK()             => new(true);
    public static Result Fail(string msg) => new(false, msg);
}

public enum EstadoOrden { Pendiente, Confirmada, Enviada, Entregada, Cancelada }

public sealed class Orden
{
    public Guid        Id     { get; } = Guid.NewGuid();
    public EstadoOrden Estado { get; private set; } = EstadoOrden.Pendiente;

    // TODO: Confirmar()  -  solo desde Pendiente -> Confirmada
    //       Error: "Solo se confirman órdenes pendientes."
    public Result Confirmar() => throw new NotImplementedException();
```

---

## Kata 3  -  State - Skeleton (parte 2)

```csharp
    // TODO: Enviar()  -  solo desde Confirmada -> Enviada
    //       Error: "Solo se envían órdenes confirmadas."
    public Result Enviar() => throw new NotImplementedException();

    // TODO: Entregar()  -  solo desde Enviada -> Entregada
    //       Error: "Solo se entregan órdenes enviadas."
    public Result Entregar() => throw new NotImplementedException();

    // TODO: Cancelar()  -  desde Pendiente o Confirmada
    //       NO desde Enviada/Entregada/Cancelada
    //       Error: "No se puede cancelar una orden enviada o entregada."
    //       Error si ya cancelada: "La orden ya está cancelada."
    public Result Cancelar() => throw new NotImplementedException();
}
```

---

## Kata 3  -  State - Program.cs

**Program.cs (no modificar):**

```csharp
Console.WriteLine("--- State ---");
static void Intentar(string accion, Result r) =>
    Console.WriteLine(r.IsSuccess ? $"  [OK] {accion}" : $"  [Err] {accion}: {r.Error}");

var orden = new Orden();
Console.WriteLine($"Estado inicial: {orden.Estado}");
Intentar("Confirmar",  orden.Confirmar());
Intentar("Enviar",     orden.Enviar());
Intentar("Cancelar",   orden.Cancelar());
Intentar("Entregar",   orden.Entregar());
Intentar("Confirmar",  orden.Confirmar());
```

---

## Kata 3  -  State - Salida esperada y criterios

**Salida esperada:**

```
--- State ---
Estado inicial: Pendiente
  [OK] Confirmar
  [OK] Enviar
  [Err] Cancelar: No se puede cancelar una orden enviada o entregada.
  [OK] Entregar
  [Err] Confirmar: Solo se confirman órdenes pendientes.
```

**Criterios de aceptación:**
- [ ] La lógica de transición vive 100% en `Orden`
- [ ] Program.cs no tiene `switch` ni `if (estado == ...)`
- [ ] Confirmar después de Entregada falla correctamente

---

## Kata 4  -  Command + Undo - Skeleton (parte 1)

**Archivo:** `Comportamiento/OrderCommands.cs`

```csharp
public interface IOrdenCommand
{
    void Execute();
    void Undo();
}

public sealed class ColocarOrdenCommand : IOrdenCommand
{
    private readonly string _producto;
    private readonly int    _cantidad;

    public ColocarOrdenCommand(string producto, int cantidad)
    { _producto = producto; _cantidad = cantidad; }

    // TODO: imprimir "[CMD] Orden colocada: {_cantidad}x {_producto}"
    public void Execute() => throw new NotImplementedException();

    // TODO: imprimir "[UNDO] Orden cancelada: {_cantidad}x {_producto}"
    public void Undo()    => throw new NotImplementedException();
}
```

---

## Kata 4  -  Command + Undo - Skeleton (parte 2)

```csharp
public sealed class HistorialComandos
{
    private readonly Stack<IOrdenCommand> _historial = new();

    // TODO: ejecutar el comando y apilarlo en _historial
    public void Ejecutar(IOrdenCommand cmd) =>
        throw new NotImplementedException();

    // TODO: si _historial no está vacío -> sacar el último y llamar Undo()
    //       si vacío -> imprimir "[UNDO] Nada que deshacer"
    public void Deshacer() =>
        throw new NotImplementedException();
}
```

---

## Kata 4  -  Command + Undo - Program.cs

**Program.cs (no modificar):**

```csharp
Console.WriteLine("--- Command ---");
var historial = new HistorialComandos();
historial.Ejecutar(new ColocarOrdenCommand("Laptop",  1));
historial.Ejecutar(new ColocarOrdenCommand("Monitor", 2));
historial.Deshacer();
historial.Deshacer();
historial.Deshacer();
```

---

## Kata 4  -  Command + Undo - Salida esperada y criterios

**Salida esperada:**

```
--- Command ---
[CMD] Orden colocada: 1x Laptop
[CMD] Orden colocada: 2x Monitor
[UNDO] Orden cancelada: 2x Monitor
[UNDO] Orden cancelada: 1x Laptop
[UNDO] Nada que deshacer
```

**Criterios de aceptación:**
- [ ] Undo es LIFO (stack)
- [ ] `HistorialComandos` no imprime el contenido  -  delega a los comandos
- [ ] El tercer Deshacer() no lanza excepción

---

## Resumen  -  Patrones y el back-template

| Patrón | Kata | Dónde aparece en el back-template |
|--------|------|-----------------------------------|
| Strategy | 1 | `IConversationAssignmentStrategy`  -  algoritmo intercambiable por DI |
| Observer | 2 | `INotificationHandler<TResponse>`  -  Presenters suscritos a la respuesta base |
| State | 3 | Transiciones en entidades (`Conversation.Archive()` -> `Result`) |
| Command | 4 | `IRequest<TResponse>`  -  encapsula intención + parámetros |

**Criterios de aceptación globales:**
- [ ] Proyecto compila sin errores ni warnings
- [ ] Las 4 secciones de Program.cs producen las salidas esperadas
- [ ] Agregar una 4ª Strategy no toca Program.cs
- [ ] Agregar un 4º Observer no toca `OrdenPublisher` ni los demás
- [ ] Las transiciones de `Orden` están en la clase `Orden`  -  Program.cs no valida estados
