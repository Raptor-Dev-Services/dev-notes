# Hands-On S04  -  Patrones Creacionales  -  Estructurales

## Dev Notes  -  Proyecto de consola individual

**Tiempo total:** 2 horas en clase

| Kata | Patrón | Tiempo |
|------|--------|--------|
| 1 | Singleton | 10 min |
| 2 | Factory Method | 15 min |
| 3 | Builder | 20 min |
| 4 | Adapter | 15 min |
| 5 | Decorator | 25 min |
| 6 | Facade | 15 min |
| 7 | Proxy | 15 min |

> Trabajo individual. Cada kata se implementa en el mismo proyecto `S04-Kata`.

---

## Setup  -  Crea tu proyecto de consola

```bash
dotnet new console -n S04-Kata
cd S04-Kata
```

Estructura sugerida:

```
S04-Kata/
|-- Creacionales/
|   |-- ConfiguracionApp.cs
|   |-- NotificacionFactory.cs
|   `-- PedidoBuilder.cs
|-- Estructurales/
|   |-- LegacyAdapter.cs
|   |-- LoggingDecorator.cs
|   |-- CheckoutFacade.cs
|   `-- LazyProxy.cs
`-- Program.cs
```

> No se requieren paquetes NuGet adicionales. Todo con la BCL de .NET 10.

---

## Kata 1  -  Singleton (10 min)

**Archivo:** `Creacionales/ConfiguracionApp.cs`

```csharp
public sealed class ConfiguracionApp
{
    // TODO: implementar Singleton thread-safe con Lazy<T>
    private static readonly Lazy<ConfiguracionApp> _instance =
        new(() => new ConfiguracionApp());

    public static ConfiguracionApp Instance => _instance.Value;

    // TODO: hacer el constructor private
    public ConfiguracionApp() { }  // <- corregir a private

    public string Ambiente   { get; } = "development";
    public int    TimeoutMs  { get; } = 5000;
}
```

---

## Kata 1  -  Singleton  -  Program.cs

**`Program.cs`**

```csharp
var cfg1 = ConfiguracionApp.Instance;
var cfg2 = ConfiguracionApp.Instance;
// TODO: imprimir si cfg1 == cfg2 (misma instancia)
// TODO: imprimir cfg1.Ambiente y cfg1.TimeoutMs
```

---

## Kata 1  -  Singleton  -  Salida esperada y criterio

**Salida esperada en consola:**

```
--- Singleton ---
¿Misma instancia? True
Ambiente: development | Timeout: 5000ms
```

**Criterio de aceptación:**

- [ ] `new ConfiguracionApp()` no compila desde `Program.cs` (constructor `private`)
- [ ] `Instance` siempre retorna la misma referencia (`cfg1 == cfg2` es `True`)
- [ ] `Lazy<T>` garantiza inicialización thread-safe sin `lock`

---

## Kata 2  -  Factory Method (15 min)

**Archivo:** `Creacionales/NotificacionFactory.cs`

```csharp
public interface INotificacion
{
    Task EnviarAsync(string destinatario, string mensaje);
}

public sealed class EmailNotificacion : INotificacion
{
    public async Task EnviarAsync(string destinatario, string mensaje)
    {
        await Task.Delay(50);
        Console.WriteLine($"[EMAIL] -> {destinatario}: {mensaje}");
    }
}
```

---

## Kata 2  -  Factory Method  -  Implementaciones

```csharp
public sealed class SmsNotificacion : INotificacion
{
    public async Task EnviarAsync(string destinatario, string mensaje)
    {
        await Task.Delay(30);
        Console.WriteLine($"[SMS] -> {destinatario}: {mensaje}");
    }
}

public sealed class PushNotificacion : INotificacion
{
    public async Task EnviarAsync(string destinatario, string mensaje)
    {
        await Task.Delay(20);
        Console.WriteLine($"[PUSH] -> {destinatario}: {mensaje}");
    }
}
```

---

## Kata 2  -  Factory Method  -  Skeleton

```csharp
public static class NotificacionFactory
{
    // TODO: implementar Create(string tipo) retornando INotificacion
    // tipo: "email", "sms", "push"
    // tipo desconocido -> throw ArgumentException
    public static INotificacion Create(string tipo) =>
        throw new NotImplementedException();
}
```

---

## Kata 2  -  Factory Method  -  Program.cs

**`Program.cs`**

```csharp
var canales = new[] { "email", "sms", "push" };
foreach (var canal in canales)
{
    var notif = NotificacionFactory.Create(canal);
    await notif.EnviarAsync("usuario@example.com", "Tu pedido esta listo");
}
```

---

## Kata 2  -  Factory Method  -  Salida esperada

**Salida esperada:**

```
--- Factory Method ---
[EMAIL] -> usuario@example.com: Tu pedido esta listo
[SMS]   -> usuario@example.com: Tu pedido esta listo
[PUSH]  -> usuario@example.com: Tu pedido esta listo
```

---

## Kata 3  -  Builder (20 min)

**Archivo:** `Creacionales/PedidoBuilder.cs`

```csharp
public sealed class Pedido
{
    public Guid   Id         { get; init; }
    public string Cliente    { get; init; } = string.Empty;
    public string Direccion  { get; init; } = string.Empty;
    public string MetodoPago { get; init; } = "efectivo";
    public bool   Urgente    { get; init; }
    public IReadOnlyList<string> Items { get; init; } = [];

    public override string ToString() =>
        $"Pedido {Id.ToString()[..8]} | {Cliente} | " +
        $"{Items.Count} items | {MetodoPago} | Urgente: {Urgente}";
}
```

---

## Kata 3  -  Builder  -  Skeleton del Builder

```csharp
public sealed class PedidoBuilder
{
    private string _cliente   = string.Empty;
    private string _direccion = string.Empty;
    private string _pago      = "efectivo";
    private bool   _urgente;
    private readonly List<string> _items = [];

    // TODO: cada metodo retorna this (fluent interface)
    public PedidoBuilder ConCliente(string cliente)    => throw new NotImplementedException();
    public PedidoBuilder EnDireccion(string direccion) => throw new NotImplementedException();
    public PedidoBuilder ConPago(string metodo)        => throw new NotImplementedException();
    public PedidoBuilder EsUrgente()                   => throw new NotImplementedException();
    public PedidoBuilder AgregarItem(string item)      => throw new NotImplementedException();
    public Pedido Build()                              => throw new NotImplementedException();
}
```

---

## Kata 3  -  Builder  -  Program.cs (dado  -  no modificar)

```csharp
var pedido = new PedidoBuilder()
    .ConCliente("Juan Garcia")
    .EnDireccion("Av. Reforma 123, CDMX")
    .ConPago("tarjeta")
    .EsUrgente()
    .AgregarItem("Laptop")
    .AgregarItem("Mouse")
    .Build();
Console.WriteLine(pedido);
```

---

## Kata 3  -  Builder  -  Salida esperada y criterio

**Salida esperada:**

```
Pedido a1b2c3d4 | Juan Garcia | 2 items | tarjeta | Urgente: True
```

**Criterio:**

- [ ] `Build()` retorna un `Pedido` inmutable con `init`
- [ ] El Builder es fluent (cada método retorna `this`)

---

## Kata 4  -  Adapter (15 min)

**Contexto:** el equipo tiene codigo legacy de pagos. Hay que adaptarlo a la nueva interfaz sin modificar la clase legacy.

**Archivo:** `Estructurales/LegacyAdapter.cs`

```csharp
// Sistema legacy  -  NO modificar
public sealed class SistemaPagoLegacy
{
    public string ProcesarPago(string tarjeta, double monto, string moneda)
    {
        Thread.Sleep(50);
        return $"OK-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }
}

// Nueva interfaz que el sistema moderno espera
public interface IPasarelaPago
{
    Task<Result<string>> CobrarAsync(
        decimal monto, string moneda, CancellationToken ct = default);
}
```

---

## Kata 4  -  Adapter  -  Skeleton

```csharp
// TODO: implementar el Adapter
public sealed class LegacyPagoAdapter : IPasarelaPago
{
    private readonly SistemaPagoLegacy _legacy = new();

    public Task<Result<string>> CobrarAsync(
        decimal monto, string moneda, CancellationToken ct = default)
    {
        // TODO: llamar _legacy.ProcesarPago con los tipos correctos
        //       tarjeta fija: "4111111111111111"
        //       monto como double: (double)monto
        // TODO: si resultado empieza con "OK-" -> Result.OK<string>(codigo)
        // TODO: si no                          -> Result.Fail<string>("Pago rechazado")
        throw new NotImplementedException();
    }
}
```

---

## Kata 4  -  Adapter  -  Salida esperada y criterio

**Salida esperada:**

```
--- Adapter ---
Pago procesado. Confirmacion: OK-A3F2D1
```

**Criterio:**

- [ ] `SistemaPagoLegacy` no se modifica
- [ ] El `Adapter` convierte tipos y resultado

---

## Kata 5  -  Decorator (25 min)

**La kata mas importante del dia.** Agrega capas de comportamiento sin modificar la clase base.

**Archivo:** `Estructurales/LoggingDecorator.cs`

```csharp
public interface IProductoRepository
{
    Task<string?> GetNombreAsync(Guid id);
    Task InsertAsync(Guid id, string nombre);
}

public sealed class InMemoryProductoRepository : IProductoRepository
{
    private readonly Dictionary<Guid, string> _store = [];

    public Task<string?> GetNombreAsync(Guid id) =>
        Task.FromResult(_store.TryGetValue(id, out var n) ? n : null);

    public Task InsertAsync(Guid id, string nombre)
    {
        _store[id] = nombre;
        return Task.CompletedTask;
    }
}
```

---

## Kata 5  -  Decorator  -  Skeleton del Decorator

```csharp
// TODO: Decorator que agrega logging
public sealed class LoggingProductoRepository : IProductoRepository
{
    private readonly IProductoRepository _inner;
    // TODO: inyectar _inner en constructor

    public async Task<string?> GetNombreAsync(Guid id)
    {
        // TODO: imprimir "[LOG] GetNombreAsync: {id}"
        // TODO: llamar _inner.GetNombreAsync(id)
        // TODO: imprimir "[LOG] resultado: {nombre ?? "null"}"
        // TODO: retornar el resultado
        throw new NotImplementedException();
    }

    public async Task InsertAsync(Guid id, string nombre)
    {
        // TODO: imprimir "[LOG] InsertAsync: {nombre} ({id})"
        // TODO: llamar _inner.InsertAsync(id, nombre)
        // TODO: imprimir "[LOG] insertado OK"
        throw new NotImplementedException();
    }
}
```

---

## Kata 5  -  Decorator  -  Program.cs (dado  -  no modificar)

```csharp
IProductoRepository repo =
    new LoggingProductoRepository(new InMemoryProductoRepository());
var id = Guid.NewGuid();
await repo.InsertAsync(id, "Teclado Mecanico");
var nombre = await repo.GetNombreAsync(id);
Console.WriteLine($"Encontrado: {nombre}");
```

---

## Kata 5  -  Decorator  -  Salida esperada y criterio

**Salida esperada:**

```
--- Decorator ---
[LOG] InsertAsync: Teclado Mecanico (a1b2c3d4-...)
[LOG] insertado OK
[LOG] GetNombreAsync: a1b2c3d4-...
[LOG] resultado: Teclado Mecanico
Encontrado: Teclado Mecanico
```

**Criterio:**

- [ ] `InMemoryProductoRepository` no tiene ningun `Console.WriteLine`
- [ ] El logging vive 100% en `LoggingProductoRepository`

---

## Kata 6  -  Facade (15 min)

**Archivo:** `Estructurales/CheckoutFacade.cs`

```csharp
// Subsistemas independientes (ya implementados)
public sealed class ServicioInventario
{
    public bool HayStock(Guid productId, int cantidad) => true;
}
public sealed class ServicioPago
{
    public async Task<bool> CobrarAsync(decimal total)
    { await Task.Delay(30); return true; }
}
public sealed class ServicioNotificacion
{
    public async Task NotificarAsync(string email, string msg)
    { await Task.Delay(20); Console.WriteLine($"[NOTIF] {email}: {msg}"); }
}
```

---

## Kata 6  -  Facade  -  Skeleton

```csharp
// TODO: Facade que orquesta los 3 subsistemas
public sealed class CheckoutFacade
{
    private readonly ServicioInventario   _inventario   = new();
    private readonly ServicioPago         _pago         = new();
    private readonly ServicioNotificacion _notificacion = new();

    public async Task<Result<string>> ProcesarPedidoAsync(
        Guid productId, int cantidad, decimal precio, string email)
    {
        // TODO: 1. Verificar stock  -> si no hay: Result.Fail<string>("Sin stock")
        // TODO: 2. Cobrar           -> si falla:  Result.Fail<string>("Pago rechazado")
        // TODO: 3. Notificar al cliente
        // TODO: 4. Retornar Result.OK<string>("Pedido procesado")
        throw new NotImplementedException();
    }
}
```

---

## Kata 6  -  Facade  -  Salida esperada

**Salida esperada:**

```
[NOTIF] cliente@example.com: Tu pedido ha sido procesado
Resultado: Pedido procesado
```

---

## Kata 7  -  Proxy (15 min)

**Archivo:** `Estructurales/LazyProxy.cs`

```csharp
public interface IReportePesado { Task<string> GenerarAsync(); }

public sealed class ReportePesado : IReportePesado
{
    public async Task<string> GenerarAsync()
    {
        Console.WriteLine("[REPORTE] Generando reporte pesado...");
        await Task.Delay(500);
        return "REPORTE_DATOS_2026_Q1";
    }
}
```

---

## Kata 7  -  Proxy  -  Skeleton

```csharp
// TODO: Proxy con lazy loading + cache en memoria
public sealed class ReportePesadoProxy : IReportePesado
{
    private IReportePesado? _real;
    private string?         _cache;

    public async Task<string> GenerarAsync()
    {
        // TODO: si _cache no es null -> retornar _cache (sin llamar al real)
        // TODO: si _cache es null    -> crear _real, llamarlo, guardar en _cache
        throw new NotImplementedException();
    }
}
```

---

## Kata 7  -  Proxy  -  Salida esperada

**Salida esperada:**

```
--- Proxy ---
Primera llamada:
[REPORTE] Generando reporte pesado...
Resultado: REPORTE_DATOS_2026_Q1
Segunda llamada (debe ser instantanea, sin log):
Resultado: REPORTE_DATOS_2026_Q1
Mismo resultado? True
```

---

## Resumen  -  Patrones del dia

| Patron | Kata | Donde aparece en el back-template |
|--------|------|-----------------------------------|
| Singleton | 1 | `ConfiguracionApp`, `ITenantContextAccessor` |
| Factory Method | 2 | `NotificacionFactory`, `StrategyFactory` |
| Builder | 3 | `PedidoBuilder` (a implementar en el producto) |
| Adapter | 4 | `LegacyPagoAdapter`  -  integracion con APIs externas |
| Decorator | 5 | `LoggingProductoRepository`, `InteractorPipeline` |
| Facade | 6 | `CheckoutFacade`, `BaseApiController` |
| Proxy | 7 | Lazy loading, `ResultViewModel<T>` |

---

## Criterio global

- [ ] Proyecto compila sin errores ni warnings
- [ ] `new ConfiguracionApp()` falla en compilacion (Singleton)
- [ ] Factory lanza `ArgumentException` para tipo desconocido
- [ ] `Build()` retorna `Pedido` con propiedades `init` (inmutable)
- [ ] `InMemoryProductoRepository` no tiene `Console.WriteLine` (Decorator)
- [ ] Proxy no llama a `ReportePesado` en la segunda invocacion
