# Hands-On S06  -  DDD - CQRS - Clean Architecture

## Dev Notes - Proyecto de consola individual

```
dotnet new console -n S06-Kata
```

| Kata | Concepto | Tiempo |
|------|----------|--------|
| 1 | Entity `Producto` + Value Object `Dinero` | 30 min |
| 2 | `IProductoRepository` in-memory | 20 min |
| 3 | `CrearProductoHandler` (Command  -  CQRS) | 25 min |
| 4 | `ObtenerProductoHandler` (Query  -  CQRS) | 25 min |
| 5 | Demostrar separación CQRS | 15 min |

---

## Estructura del proyecto

```
S06-Kata/
|-- Dominio/
|   |-- Producto.cs       <- Entity + tipos Result compartidos
|   `-- Dinero.cs         <- Value Object
|-- Aplicacion/
|   |-- IProductoRepository.cs
|   |-- ProductoDto.cs
|   |-- CrearProductoHandler.cs
|   `-- ObtenerProductoHandler.cs
|-- Infraestructura/
|   `-- InMemoryProductoRepository.cs
`-- Program.cs            <- NO modificar
```

> Todos los archivos son clases de nivel superior (`global using` no es necesario).
> No se usa MediatR  -  los Handlers son clases simples con `HandleAsync()`.

---

## Tipos compartidos  -  definir en `Dominio/Producto.cs`

Estos tipos simulan la librería `Common` del back-template. Colocarlos **al inicio** del archivo, antes de la clase `Producto`.

```csharp
// Tipos compartidos  -  simulan Common del back-template
public sealed record Result(bool IsSuccess, string? Error = null)
{
    public static Result OK()              => new(true);
    public static Result Fail(string msg)  => new(false, msg);
}

public sealed record Result<T>(bool IsSuccess, T? Value, string? Error = null)
{
    public static Result<T> OK(T value)      => new(true, value);
    public static Result<T> Fail(string msg) => new(false, default, msg);
}
```

> En el back-template real estos tipos viven en `Common/Results/Result.cs`.
> Aquí los duplicamos para que el proyecto sea autónomo.

---

## Kata 1  -  Entity: `Producto` (skeleton)

**Archivo:** `Dominio/Producto.cs`

En DDD, la **Entity** tiene identidad (`Id`), puede tener cambios de estado y valida sus propias reglas de negocio. El constructor es `private`  -  el único punto de entrada es el factory method `Create()`.

```csharp
public sealed class Producto
{
    public Guid    Id         { get; init; }
    public string  Nombre     { get; init; } = string.Empty;
    public decimal Precio     { get; init; }
    public bool    EstaActivo { get; private set; } = true;

    private Producto() { }

    // TODO: validar Nombre no vacío y Precio > 0
    // Retornar Result<Producto>.Fail("...") si inválido
    // Retornar Result<Producto>.OK(new Producto { ... }) si válido
    public static Result<Producto> Create(string nombre, decimal precio) =>
        throw new NotImplementedException();
```

---

## Kata 1  -  Entity: `Producto` (skeleton  -  parte 2)

```csharp
    // TODO: si EstaActivo == false -> return Result.Fail("El producto ya está desactivado.")
    //       si EstaActivo == true  -> EstaActivo = false; return Result.OK()
    public Result Desactivar() =>
        throw new NotImplementedException();
}
```

---

## Kata 1  -  Value Object: `Dinero` (skeleton)

**Archivo:** `Dominio/Dinero.cs`

El **Value Object** no tiene identidad  -  su igualdad se basa en los valores. Es inmutable. Se usa `record` para obtener igualdad estructural gratis.

```csharp
public sealed record Dinero
{
    public decimal Monto  { get; init; }
    public string  Moneda { get; init; } = string.Empty;

    private Dinero() { }

    // TODO: validar Monto >= 0 y Moneda con exactamente 3 caracteres
    // Retornar Result<Dinero>.Fail("...") si inválido
    // Retornar Result<Dinero>.OK(new Dinero { ... }) si válido
    public static Result<Dinero> Create(decimal monto, string moneda) =>
        throw new NotImplementedException();

    public override string ToString() => $"{Moneda} {Monto:F2}";
}
```

---

## Kata 1  -  Criterios de aceptación

- [ ] `new Producto()` no compila desde fuera de la clase (constructor `private`)
- [ ] `Create("", 10)` retorna `IsSuccess = false`
- [ ] `Create("Laptop", -1)` retorna `IsSuccess = false`
- [ ] `Create("Laptop", 89.99m)` retorna `IsSuccess = true`
- [ ] `Desactivar()` en producto ya inactivo retorna `IsSuccess = false`

---

## Kata 1  -  `Program.cs` (sección 1, no modificar)

```csharp
Console.WriteLine("--- Kata 1: Entity + Value Object ---");

var r1 = Producto.Create("Teclado Mecánico", 899.99m);
Console.WriteLine(r1.IsSuccess
    ? $"  [OK] Producto creado: {r1.Value!.Nombre} (${r1.Value.Precio})"
    : $"  [Err] Error: {r1.Error}");

var r2 = Producto.Create("", 100m);
Console.WriteLine(r2.IsSuccess ? $"  [OK] {r2.Value}" : $"  [Err] Error: {r2.Error}");

if (r1.IsSuccess)
{
    var des1 = r1.Value!.Desactivar();
    Console.WriteLine(des1.IsSuccess ? "  [OK] Producto desactivado" : $"  [Err] {des1.Error}");
    var des2 = r1.Value!.Desactivar();
    Console.WriteLine(des2.IsSuccess ? "  [OK] Desactivado" : $"  [Err] {des2.Error}");
}

var d1 = Dinero.Create(450.00m, "MXN");
Console.WriteLine(d1.IsSuccess ? $"  [OK] Dinero: {d1.Value}" : $"  [Err] {d1.Error}");
var d2 = Dinero.Create(-10m, "USD");
Console.WriteLine(d2.IsSuccess ? $"  [OK] {d2.Value}" : $"  [Err] Error: {d2.Error}");
```

---

## Kata 1  -  Salida esperada

```
--- Kata 1: Entity + Value Object ---
  [OK] Producto creado: Teclado Mecánico ($899.99)
  [Err] Error: El nombre no puede estar vacío.
  [OK] Producto desactivado
  [Err] El producto ya está desactivado.
  [OK] Dinero: MXN 450.00
  [Err] Error: El monto no puede ser negativo.
```

---

## Kata 2  -  Interfaz del repositorio

**Archivo:** `Aplicacion/IProductoRepository.cs`

En Clean Architecture la **interfaz** del repositorio vive en `Aplicacion/` (o `Dominio/`). La implementación vive en `Infraestructura/`.

```csharp
public interface IProductoRepository
{
    Task GuardarAsync(Producto producto, CancellationToken ct = default);
    Task<Producto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Producto>> ListarActivosAsync(CancellationToken ct = default);
}
```

---

## Kata 2  -  Implementación in-memory (skeleton)

**Archivo:** `Infraestructura/InMemoryProductoRepository.cs`

```csharp
public sealed class InMemoryProductoRepository : IProductoRepository
{
    private readonly Dictionary<Guid, Producto> _store = [];

    // TODO: guardar producto en _store usando su Id como clave
    public Task GuardarAsync(Producto producto, CancellationToken ct = default) =>
        throw new NotImplementedException();

    // TODO: buscar por Id; retornar null si no existe
    public Task<Producto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        throw new NotImplementedException();

    // TODO: retornar todos donde EstaActivo == true, como IReadOnlyList<Producto>
    public Task<IReadOnlyList<Producto>> ListarActivosAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();
}
```

---

## Kata 2  -  Criterios de aceptación

- [ ] `GuardarAsync` persiste el producto en memoria
- [ ] `ObtenerPorIdAsync` retorna `null` para un Id no registrado
- [ ] `ListarActivosAsync` excluye productos con `EstaActivo = false`

---

## Kata 3  -  DTO y Command

**Archivo:** `Aplicacion/ProductoDto.cs`  -  definir aquí (se reutiliza en Kata 4 y 5):

```csharp
public sealed record ProductoDto(Guid Id, string Nombre, decimal Precio, bool EstaActivo);
```

**Archivo:** `Aplicacion/CrearProductoHandler.cs`  -  Command:

```csharp
public sealed record CrearProductoCommand(string Nombre, decimal Precio);
```

Un **Command** escribe estado y retorna confirmación  -  nunca retorna la entidad directamente.

---

## Kata 3  -  `CrearProductoHandler` (skeleton)

```csharp
public sealed class CrearProductoHandler
{
    private readonly IProductoRepository _repo;
    public CrearProductoHandler(IProductoRepository repo) => _repo = repo;

    // TODO: 1. Llamar Producto.Create(command.Nombre, command.Precio)
    //       2. Si falla -> retornar Result<ProductoDto>.Fail(error)
    //       3. Si éxito -> guardar en repo, retornar Result<ProductoDto>.OK(dto)
    //          donde dto = new ProductoDto(producto.Id, producto.Nombre,
    //                                     producto.Precio, true)
    public async Task<Result<ProductoDto>> HandleAsync(
        CrearProductoCommand command, CancellationToken ct = default) =>
        throw new NotImplementedException();
}
```

---

## Kata 3  -  Criterios de aceptación

- [ ] Retorna `ProductoDto`  -  NO la entidad `Producto`
- [ ] No tiene lógica de validación  -  la delega a `Producto.Create()`
- [ ] No llama a `ListarActivosAsync` ni a `ObtenerPorIdAsync`  -  solo escribe

---

## Kata 3  -  `Program.cs` (sección 3, no modificar)

```csharp
Console.WriteLine("--- Kata 3: Command (CQRS) ---");
IProductoRepository repo = new InMemoryProductoRepository();
var handler = new CrearProductoHandler(repo);

var r1 = await handler.HandleAsync(new CrearProductoCommand("Monitor 4K", 4999.99m));
Console.WriteLine(r1.IsSuccess
    ? $"  [OK] Creado: {r1.Value!.Nombre} (Id: {r1.Value.Id.ToString()[..8]})"
    : $"  [Err] Error: {r1.Error}");

var r2 = await handler.HandleAsync(new CrearProductoCommand("", 100m));
Console.WriteLine(r2.IsSuccess ? $"  [OK] {r2.Value}" : $"  [Err] Error: {r2.Error}");
```

---

## Kata 3  -  Salida esperada

```
--- Kata 3: Command (CQRS) ---
  [OK] Creado: Monitor 4K (Id: a1b2c3d4)
  [Err] Error: El nombre no puede estar vacío.
```

> El Id es un `Guid` aleatorio  -  los primeros 8 caracteres varían en cada ejecución.

---

## Kata 4  -  `ObtenerProductoHandler` (Query)

**Archivo:** `Aplicacion/ObtenerProductoHandler.cs`

Un **Query** lee estado, proyecta a DTO y no modifica nada. No llama a `GuardarAsync()`. No invoca métodos de dominio (`Desactivar()`, `Create()`).

```csharp
public sealed record ObtenerProductoQuery(Guid Id);

public sealed class ObtenerProductoHandler
{
    private readonly IProductoRepository _repo;
    public ObtenerProductoHandler(IProductoRepository repo) => _repo = repo;

    // TODO: 1. Buscar en _repo por Id
    //       2. Si no existe -> Result<ProductoDto>.Fail("Producto no encontrado.")
    //       3. Si existe -> proyectar a ProductoDto y retornar Result<ProductoDto>.OK(dto)
    // IMPORTANTE: NO llamar Desactivar() ni ningún método de la entidad  -  solo leer
    public async Task<Result<ProductoDto>> HandleAsync(
        ObtenerProductoQuery query, CancellationToken ct = default) =>
        throw new NotImplementedException();
}
```

---

## Kata 4  -  Criterios de aceptación

- [ ] No llama a `Producto.Create()` ni a `Desactivar()`
- [ ] No llama a `repo.GuardarAsync()`  -  es pura lectura
- [ ] Retorna `ProductoDto`, no la entidad `Producto`

---

## Kata 4  -  `Program.cs` (sección 4, no modificar)

```csharp
Console.WriteLine("--- Kata 4: Query (CQRS) ---");
var queryHandler = new ObtenerProductoHandler(repo);  // mismo repo del Kata 3

var id = r1.Value!.Id;
var q1 = await queryHandler.HandleAsync(new ObtenerProductoQuery(id));
Console.WriteLine(q1.IsSuccess
    ? $"  [OK] Encontrado: {q1.Value!.Nombre} | Activo: {q1.Value.EstaActivo}"
    : $"  [Err] {q1.Error}");

var q2 = await queryHandler.HandleAsync(new ObtenerProductoQuery(Guid.NewGuid()));
Console.WriteLine(q2.IsSuccess ? $"  [OK] {q2.Value}" : $"  [Err] {q2.Error}");
```

---

## Kata 4  -  Salida esperada

```
--- Kata 4: Query (CQRS) ---
  [OK] Encontrado: Monitor 4K | Activo: True
  [Err] Producto no encontrado.
```

---

## Kata 5  -  `Program.cs` (sección 5, no modificar)

```csharp
Console.WriteLine("--- Kata 5: Separación CQRS ---");

var cmd = await handler.HandleAsync(new CrearProductoCommand("Webcam HD", 799m));
var wid = cmd.Value!.Id;
Console.WriteLine($"  Creado: {cmd.Value.Nombre}");

// Desactivar usando el repositorio directamente (fuera del flujo CQRS  -  solo demo)
var prod = await repo.ObtenerPorIdAsync(wid);
prod!.Desactivar();
await repo.GuardarAsync(prod);

var q = await queryHandler.HandleAsync(new ObtenerProductoQuery(wid));
Console.WriteLine($"  Después de desactivar -> Activo: {q.Value!.EstaActivo}");

var activos = await repo.ListarActivosAsync();
Console.WriteLine($"  Productos activos: {activos.Count}");
foreach (var p in activos) Console.WriteLine($"    - {p.Nombre}");
```

---

## Kata 5  -  Salida esperada

```
--- Kata 5: Separación CQRS ---
  Creado: Webcam HD
  Después de desactivar -> Activo: False
  Productos activos: 1
    - Monitor 4K
```

---

## Resumen  -  Conceptos del día

| Concepto | Kata | Regla clave |
|----------|------|-------------|
| Entity | 1 | Constructor `private` + `Create()` con `Result<T>` |
| Value Object | 1 | `record` + `Create()` + igualdad por valor |
| Repository | 2 | Interfaz en `Aplicacion/`, implementación en `Infraestructura/` |
| Command (CQRS) | 3 | Escribe, retorna DTO, NO retorna entidad |
| Query (CQRS) | 4 | Lee, proyecta a DTO, NO modifica estado |

**Criterios de aceptación globales:**
- [ ] El proyecto compila sin errores ni warnings
- [ ] Las 5 secciones del `Program.cs` producen las salidas esperadas
- [ ] `CrearProductoHandler` nunca llama `ObtenerPorIdAsync` ni `ListarActivosAsync`
- [ ] `ObtenerProductoHandler` nunca llama `GuardarAsync`
- [ ] La lógica de validación está en `Producto.Create()` y `Desactivar()`  -  no en los Handlers
