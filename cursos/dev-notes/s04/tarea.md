# Homework S04  -  Patrones Creacionales  -  Estructurales

## Dev Notes

**Tiempo estimado:** 3-4 horas

| Parte | Actividad | Tiempo |
|-------|-----------|--------|
| 1 | Lecturas en dev-notes | 1 hora |
| 2 | Proyecto de consola: LoggingProductoRepository | 2 horas |
| 3 | Analisis de patrones en paquetes NuGet | 30 min |
| Bonus | CachingProductoRepository (segunda capa Decorator) | 30 min |

---

## Parte 1  -  Lecturas (1 hora)

Lee los siguientes archivos de dev-notes antes de escribir codigo:

**Patrones creacionales:**

```
dev-notes/02-programacion/design-patterns/creacional/01-05
```

- `01-singleton.md`
- `02-factory-method.md`
- `03-abstract-factory.md`
- `04-builder.md`
- `05-prototype.md`

**Patrones estructurales:**

```
dev-notes/02-programacion/design-patterns/structural/06-12
```

- `06-adapter.md`
- `07-bridge.md`
- `08-composite.md`
- `09-decorator.md`
- `10-facade.md`
- `11-flyweight.md`
- `12-proxy.md`

> Pon especial atencion en Decorator y Proxy  -  son los mas usados en el back-template.

---

## Parte 2  -  Proyecto de consola (2 horas)

### Setup

```bash
dotnet new console -n S04-Homework
cd S04-Homework
dotnet add package Microsoft.Extensions.Logging.Abstractions
```

### Entidad `Producto`

```csharp
public sealed class Producto
{
    public Guid     Id       { get; init; } = Guid.NewGuid();
    public string   Nombre   { get; init; } = string.Empty;
    public decimal  Precio   { get; init; }
    public bool     IsActive { get; init; } = true;
    public DateTime CreadoEn { get; init; } = DateTime.UtcNow;
}
```

---

## Parte 2  -  Interfaz del repositorio

```csharp
public interface IProductoRepository
{
    Task<Producto?>          GetByIdAsync(Guid id);
    Task<IReadOnlyList<Producto>> ListActiveAsync();
    Task                     InsertAsync(Producto producto);
    Task                     DeactivateAsync(Guid id);
}
```

---

## Parte 2  -  InMemoryProductoRepository (parte 1)

```csharp
public sealed class InMemoryProductoRepository : IProductoRepository
{
    private readonly Dictionary<Guid, Producto> _store = [];

    public Task<Producto?> GetByIdAsync(Guid id) =>
        Task.FromResult(_store.TryGetValue(id, out var p) ? p : null);

    public Task<IReadOnlyList<Producto>> ListActiveAsync() =>
        Task.FromResult<IReadOnlyList<Producto>>(
            _store.Values.Where(p => p.IsActive).ToList());
```

---

## Parte 2  -  InMemoryProductoRepository (parte 2)

```csharp
    public Task InsertAsync(Producto producto)
    {
        _store[producto.Id] = producto;
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(Guid id)
    {
        if (_store.TryGetValue(id, out var p))
            _store[id] = p with { IsActive = false };
        return Task.CompletedTask;
    }
}
```

---

## Parte 2  -  Decorator con Microsoft.Extensions.Logging

Implementa `LoggingProductoRepository` con las siguientes reglas:

1. Recibe `IProductoRepository inner` e `ILogger<LoggingProductoRepository> logger` en el constructor
2. Cada metodo loguea ANTES de llamar al inner y DESPUES con el resultado
3. Las plantillas de log deben ser **estructuradas**  -  NO string interpolation en el mensaje

```csharp
// CORRECTO  -  plantilla estructurada
_logger.LogInformation("Insertando producto {Nombre} con precio {Precio}",
    producto.Nombre, producto.Precio);

// INCORRECTO  -  string interpolation en el mensaje
_logger.LogInformation($"Insertando producto {producto.Nombre}");
```

---

## Parte 2  -  Metodos a implementar

| Metodo | Log antes | Log despues |
|--------|-----------|-------------|
| `GetByIdAsync` | `"Buscando producto {Id}"` | `"Producto {Nombre} encontrado"` o `"Producto {Id} no encontrado"` |
| `ListActiveAsync` | `"Listando productos activos"` | `"Se encontraron {Count} productos activos"` |
| `InsertAsync` | `"Insertando producto {Nombre} con precio {Precio}"` | `"Producto {Nombre} insertado correctamente"` |
| `DeactivateAsync` | `"Desactivando producto {Id}"` | `"Producto {Id} desactivado"` |

---

## Parte 2  -  Program.cs (dado  -  no modificar)  -  parte 1

```csharp
using Microsoft.Extensions.Logging.Abstractions;

IProductoRepository inner = new InMemoryProductoRepository();
IProductoRepository repo  = new LoggingProductoRepository(
    inner,
    NullLogger<LoggingProductoRepository>.Instance);

await repo.InsertAsync(new Producto { Nombre = "Laptop",  Precio = 1299.99m });
await repo.InsertAsync(new Producto { Nombre = "Monitor", Precio = 349.00m  });
```

---

## Parte 2  -  Program.cs (dado  -  no modificar)  -  parte 2

```csharp
var todos = await repo.ListActiveAsync();
Console.WriteLine($"Activos: {todos.Count}");

var primero = todos[0];
await repo.DeactivateAsync(primero.Id);

var restantes = await repo.ListActiveAsync();
Console.WriteLine($"Activos despues: {restantes.Count}");
```

---

## Parte 2  -  Salida esperada

**Salida esperada** (`NullLogger` no imprime  -  los `Console.WriteLine` son del `Program.cs`):

```
Activos: 2
Activos despues: 1
```

> El punto clave: el Decorator funciona sin importar que logger se inyecte.
> En produccion se reemplaza `NullLogger` con `ConsoleLogger` o `SerilogLogger`.

---

## Parte 2  -  DI registration (en comentarios de Program.cs)

Agrega al final de `Program.cs` un bloque de comentarios mostrando como se registraria en un proyecto real con `WebApplication`:

```csharp
// --- DI Registration (comentario para code review) ---

// Opcion A: repositorio en memoria (tests / desarrollo sin BD)
// builder.Services.AddScoped<IProductoRepository, InMemoryProductoRepository>();
// builder.Services.Decorate<IProductoRepository, LoggingProductoRepository>();
//   ^ requiere Scrutor: dotnet add package Scrutor

// Opcion B: repositorio sobre EF Core (produccion)
// builder.Services.AddScoped<IProductoRepository, EfProductoRepository>();
// builder.Services.Decorate<IProductoRepository, LoggingProductoRepository>();
//   ^ mismo Decorator, diferente inner  -  el Decorator no sabe cual es
```

---

## Parte 3  -  Analisis de patrones NuGet (30 min)

Revisa el codigo fuente de los paquetes usados en el back-template e identifica que patron GoF aplican. Completa la tabla en el PR como comentario.

| Paquete | Clase / Interface | Patron | Evidencia (metodo o concepto clave) |
|---------|-------------------|--------|--------------------------------------|
| `MediatR` | `INotificationHandler<T>` | ??? | ??? |
| `Serilog` | `LoggerConfiguration` | ??? | ??? |
| `FluentValidation` | `AbstractValidator<T>` | ??? | ??? |
| `EF Core` | `ModelBuilder` en `OnModelCreating` | ??? | ??? |
| `Microsoft.Extensions.Options` | `IOptions<T>` | ??? | ??? |

> Tip: busca en GitHub el codigo fuente de cada paquete. No necesitas instalarlos.

---

## Bonus  -  CachingProductoRepository (30 min)

Agrega una segunda capa de Decorator sobre `LoggingProductoRepository` que cachee el resultado de `GetByIdAsync` en la segunda llamada.

```csharp
public sealed class CachingProductoRepository : IProductoRepository
{
    private readonly IProductoRepository          _inner;
    private readonly Dictionary<Guid, Producto?> _cache = [];

    public CachingProductoRepository(IProductoRepository inner)
        => _inner = inner;

    public async Task<Producto?> GetByIdAsync(Guid id)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            Console.WriteLine($"[CACHE] HIT para {id}");
            return cached;
        }
```

---

## Bonus  -  CachingProductoRepository (parte 2)

```csharp
        var result = await _inner.GetByIdAsync(id);
        _cache[id] = result;
        Console.WriteLine($"[CACHE] MISS para {id}  -  guardado en cache");
        return result;
    }

    // Los demas metodos delegan directamente a _inner
    public Task<IReadOnlyList<Producto>> ListActiveAsync() => _inner.ListActiveAsync();
    public Task InsertAsync(Producto producto)             => _inner.InsertAsync(producto);
    public Task DeactivateAsync(Guid id)                  => _inner.DeactivateAsync(id);
}
```

**Cadena resultante:** `CachingProductoRepository` -> `LoggingProductoRepository` -> `InMemoryProductoRepository`

---

## Criterio de aceptacion global


- [ ] PR creado con el titulo exacto indicado
- [ ] `LoggingProductoRepository` compila e implementa `IProductoRepository`
- [ ] Las plantillas de log son estructuradas (sin string interpolation en el mensaje)
- [ ] `NullLogger<T>.Instance` usado  -  proyecto compila sin `Microsoft.Extensions.Logging.Console`
- [ ] DI registration comentada en `Program.cs` (sobre EF Core y sobre InMemory)
- [ ] Tabla de analisis NuGet incluida en el PR como comentario

---

## Criterio de aceptacion global (cont.)

- [ ] Proyecto compila sin warnings (`dotnet build` limpio)
- [ ] (Bonus) `CachingProductoRepository` implementado y encadenado

---

## Preparacion para S05

S05 cubre los **patrones de comportamiento**  -  los mas complejos de GoF.

Vienen 10 patrones mas. Llega habiendo leido:

```
dev-notes/02-programacion/design-patterns/behavioral/13-22
```

| Patron | Archivo |
|--------|---------|
| Chain of Responsibility | `13-chain-of-responsibility.md` |
| Command | `14-command.md` |
| Iterator | `15-iterator.md` |
| Mediator | `16-mediator.md` |
| Memento | `17-memento.md` |
| Observer | `18-observer.md` |
| State | `19-state.md` |
| Strategy | `20-strategy.md` |
| Template Method | `21-template-method.md` |
| Visitor | `22-visitor.md` |

> En S05 se implementa **Strategy** directamente en el modulo de asignacion de conversaciones del sistema de mensajería.
