# Hands-On S02 - DRY - KISS - YAGNI

## Dev Notes - Proyecto de consola individual

**Tiempo total:** 2 horas en clase

| Kata | Principio | Tiempo |
|------|-----------|--------|
| 1 | DRY - Value Object `Price` | 45 min |
| 2 | KISS - Simplificar descuento sobre-engineered | 25 min |
| 3 | YAGNI - Auditar código especulativo | 15 min |
| 4 | ADRs - Documentar las 3 decisiones de diseño de hoy | 20 min |

> Trabajo individual. Proyecto separado del back-template.

---

## Setup

```bash
dotnet new console -n S02-Kata
cd S02-Kata
```

Estructura que construirás durante las katas:

```
S02-Kata/
├── Program.cs      ← punto de entrada
├── Price.cs        ← Kata 1: Value Object
├── Email.cs        ← base para el homework
└── docs/
    └── adr/
        ├── ADR-001-sealed-record.md     ← Kata 4
        ├── ADR-002-result-pattern.md    ← Kata 4
        └── ADR-003-yagni-product.md     ← Kata 4
```

> No hay NuGet packages adicionales. El tipo `Result<T>` se define inline en `Program.cs` (siguiente slide).

---

## Kata 1 - DRY: El problema

La misma regla de validación copiada en 3 handlers:

```csharp
// Mal: DRY violation — misma regla en 3 lugares distintos
public class CreateProductHandler {
    public void Handle(decimal price) {
        if (price <= 0 || price > 1_000_000)        // regla aquí
            throw new ArgumentException("Precio inválido");
    }
}
public class UpdateProductHandler {
    public void Handle(decimal price) {
        if (price <= 0 || price > 1_000_000)        // copia exacta
            throw new ArgumentException("Precio inválido");
    }
}
// ImportProductsHandler: misma validación copiada (3er lugar)
```

> Si el máximo cambia de `1_000_000` a `500_000`, hay que editar 3 archivos. Uno se olvidará.

---

## Kata 1 - Tipo `Result<T>`

Agrega este tipo **al inicio de `Program.cs`** antes de cualquier otro código:

```csharp
public sealed class Result<T>
{
    public bool   IsSuccess { get; }
    public T?     Value     { get; }
    public string Error     { get; }

    private Result(bool ok, T? value, string error)
        => (IsSuccess, Value, Error) = (ok, value, error);

    public static Result<T> OK(T value)      => new(true, value, string.Empty);
    public static Result<T> Fail(string msg) => new(false, default, msg);
}
```

> En el back-template este tipo vive en `Common/Results/Result.cs`.
> Por ahora lo definimos inline para que el proyecto sea autónomo.

---

## Kata 1 - Skeleton `Price.cs`

Crea el archivo `Price.cs` y completa los `TODO`:

```csharp
// Price.cs
public sealed record Price
{
    public decimal Value { get; }
    private Price(decimal value) => Value = value;

    public static Result<Price> Create(decimal value)
    {
        // TODO: validar value > 0
        //       → Result<Price>.Fail("El precio debe ser mayor a 0.")
        // TODO: validar value <= 1_000_000
        //       → Result<Price>.Fail("El precio excede el máximo permitido.")
        // Si válido:
        //       → Result<Price>.OK(new Price(value))
        throw new NotImplementedException();
    }

    public override string ToString() => $"${Value:N2}";
}
```

---

## Kata 1 - `Program.cs` (no modificar)

```csharp
// Program.cs — Kata 1
Console.WriteLine("=== Kata 1: DRY — Value Object Price ===");

var casos   = new[] { 1299.99m, -10m, 1_500_000m, 29.99m };
var nombres = new[] { "Laptop", "", "", "Mouse" };

for (int i = 0; i < casos.Length; i++)
{
    var r = Price.Create(casos[i]);
    if (r.IsSuccess)
        Console.WriteLine($"Sí: {nombres[i]} creado — {r.Value}");
    else
        Console.WriteLine($"No: Precio inválido: {r.Error}");
}

Console.WriteLine("--- Cambiando límite máximo a $500 ---");
var prueba = Price.Create(1299.99m);
if (!prueba.IsSuccess)
    Console.WriteLine($"No: Precio inválido: {prueba.Error}");
```

> Para la última sección: cambia el límite a `500m` en `Price.Create()`, corre de nuevo y verifica. Luego devuélvelo a `1_000_000m`.

---

## Kata 1 - Salida esperada y criterios

**Salida esperada:**

```
=== Kata 1: DRY — Value Object Price ===
Sí: Laptop creado — $1,299.99
No: Precio inválido: El precio debe ser mayor a 0.
No: Precio inválido: El precio excede el máximo permitido.
Sí: Mouse creado — $29.99
--- Cambiando límite máximo a $500 ---
No: Precio inválido: El precio excede el máximo permitido.
```

**Criterios de aceptación:**
- [ ] `Price.cs` compila; `Create()` retorna `Result<Price>`
- [ ] `new Price(999)` no compila desde `Program.cs` (constructor `private`)
- [ ] Cambiar el límite en `Create()` afecta todos los puntos sin tocar `Program.cs`
- [ ] Ninguna `throw` en `Create()` - usa `Result<Price>.OK()` y `Result<Price>.Fail()`

---

## Kata 2 - KISS: El problema

6 tipos para calcular un descuento simple:

```csharp
// Mal: KISS violation — sobre-ingeniería (fragmento)
public interface IDiscountContext { decimal Subtotal { get; } }

public interface IDiscountStrategy {
    decimal Calculate(IDiscountContext ctx);
}
public class DiscountContextBuilder {
    public IDiscountContext Build(decimal subtotal) =>
        new DiscountContext(subtotal);
    private record DiscountContext(decimal Subtotal) : IDiscountContext;
}
public class StandardDiscountStrategy : IDiscountStrategy {
    public decimal Calculate(IDiscountContext ctx) =>
        ctx.Subtotal >= 100 ? ctx.Subtotal * 0.10m : 0m;
}
// + DiscountStrategyFactory + DiscountCalculatorDirector (2 clases más)
```

> ~50 líneas - 6 tipos - un solo `if` escondido adentro. La regla cabe en 1 expresión.

---

## Kata 2 - Tu tarea y `Program.cs` (no modificar)

Reemplaza todo lo anterior por este método. Completa el `TODO`:

```csharp
// TODO: implementar en ~5 líneas o menos
// Regla: 10% de descuento si subtotal >= 100; 0% si subtotal < 100
static decimal CalculateDiscount(decimal subtotal)
{
    throw new NotImplementedException();
}
```

**`Program.cs` (no modificar):**

```csharp
Console.WriteLine("\n=== Kata 2: KISS — Descuento simple ===");
decimal[] subtotales = [50m, 100m, 200m];
foreach (var s in subtotales)
{
    var d = CalculateDiscount(s);
    Console.WriteLine($"Subtotal ${s:N2} → Descuento: ${d:N2}");
}
```

---

## Kata 2 - Salida esperada y criterios

**Salida esperada:**

```
=== Kata 2: KISS — Descuento simple ===
Subtotal $50.00 → Descuento: $0.00
Subtotal $100.00 → Descuento: $10.00
Subtotal $200.00 → Descuento: $20.00
```

**Criterios de aceptación:**
- [ ] Implementado en **5 líneas o menos** (sin contar el cierre `}`)
- [ ] Sin interfaces, factories ni clases adicionales
- [ ] La salida coincide exactamente con la esperada

---

## Kata 3 - YAGNI: Skeleton a auditar

Agrega esta clase en `Program.cs` (o en un archivo `ProductService.cs`):

```csharp
public sealed class ProductService
{
    private readonly List<string> _productos = [];

    // Funcionalidad requerida hoy
    public Result<Guid> CreateProduct(string name, decimal price)
    {
        _productos.Add($"{name} — ${price:N2}");
        return Result<Guid>.OK(Guid.NewGuid());
    }
    public IReadOnlyList<string> ListProducts() => _productos.AsReadOnly();

    // "por si acaso lo necesitamos después"
    public Task   ExportToExcelAsync()          => Task.CompletedTask;
    public Task   ImportFromSapAsync(string ep) => Task.CompletedTask;
    public void   SendToAnalyticsPlatform()     { }
    public string GenerateReportPdf()           => string.Empty;
}
```

---

## Kata 3 - Instrucciones y criterios

**Instrucciones:**

1. Comenta o elimina cada método que viole YAGNI
2. Agrega `// YAGNI: [tu razón]` junto a cada uno explicando por qué lo eliminas
3. Verifica que `CreateProduct` y `ListProducts` siguen compilando

> No hay salida de consola - se revisa durante el code review.

**Criterios de aceptación:**
- [ ] Los 4 métodos especulativos están comentados o eliminados
- [ ] Cada uno tiene su `// YAGNI: [razón]`
- [ ] `CreateProduct()` y `ListProducts()` siguen compilando y funcionando

---

## Kata 4 - ADRs: Las decisiones de hoy merecen registro

En las katas 1-3 tomaste 3 decisiones de diseño concretas. Documéntalas ahora:

| ADR | Decisión |
|-----|---------|
| ADR-001 | `sealed record Price` en lugar de `class` |
| ADR-002 | `Result<T>` en lugar de exceptions para errores de negocio |
| ADR-003 | Eliminar los 4 métodos especulativos de `ProductService` (YAGNI) |

> Crea la carpeta `docs/adr/` en tu proyecto y escribe los 3 archivos.

---

## Kata 4 - Formato MADR (copiar en cada ADR)

```markdown
# ADR-00X — [Título de la decisión]

**Status:** Accepted
**Date:** 2026-05-XX
**Deciders:** [tu rol — ej: "Desarrollador backend"]

## Contexto
[¿Qué problema o situación llevó a esta decisión?
Describe sin mencionar la solución elegida.]

## Decisión
[¿Qué decidiste? ¿Qué alternativa descartaste y por qué?]

## Consecuencias
**Positivas:**
- [Al menos 1]

**Negativas:**
- [Al menos 1]
```

---

## Kata 4 - ADR-001: `sealed record` vs `class`

Completa `docs/adr/ADR-001-sealed-record.md`:

**Contexto a desarrollar:** necesitabas representar el concepto de precio de dominio. Opciones consideradas: `decimal` puro, `class`, `record`, `sealed record`.

**Pistas para la sección Decisión:**
- ¿Por qué `record` y no `class`? (igualdad por valor vs por referencia)
- ¿Por qué `sealed`? (invariantes del tipo)
- ¿Por qué constructor `private`? (creación solo via `Create()`)

**Criterio:** el ADR debe explicar la alternativa descartada (`class`) con al menos 1 razón concreta.

---

## Kata 4 - ADR-002 y ADR-003

**`ADR-002-result-pattern.md`** - `Result<T>` vs exceptions:

Contexto: `Price.Create()` puede fallar con errores de validación esperados. Opciones: retornar `null`, lanzar `ArgumentException`, retornar `Result<T>`.

Pistas: ¿para qué son las exceptions? ¿qué fuerza al llamador a manejar el error? ¿cómo se ve el call site con cada opción?

---

**`ADR-003-yagni-product.md`** - YAGNI en `ProductService`:

Contexto: `ProductService` tenía 4 métodos que nadie pidió. Opciones: mantenerlos "por si acaso", comentarlos, eliminarlos.

Pistas: ¿qué costo tiene mantener código muerto? ¿qué costo tiene volver a escribirlo si se necesita?

---

## Kata 4 - Criterios de aceptación

- [ ] Existen los 3 archivos en `docs/adr/`
- [ ] Cada ADR tiene las 3 secciones: Contexto, Decisión, Consecuencias
- [ ] Cada Decisión menciona **la alternativa descartada** con razón concreta
- [ ] Las Consecuencias son reales - no copiadas del enunciado ni inventadas
- [ ] Los ADRs son sobre las decisiones de **esta kata** - no genéricos


---

## Discusión grupal - 15 min

Anota las respuestas en los comentarios de tu PR.

**1. DRY vs Unificación prematura**

`Price` y `Quantity` podrían tener reglas similares (ambas positivas). ¿Las combinas en un `PositiveNumber<T>` genérico o las mantienes separadas? ¿Cuándo cambia tu respuesta?

**2. KISS vs Extensibilidad real**

¿Cuándo sí justifican las 6 clases? Describe un escenario concreto donde el diseño de Kata 2 sería la decisión correcta.

**3. YAGNI en tu propio código**

Nombra un ejemplo de código especulativo que hayas escrito o visto. ¿Cómo lo identificaste? ¿Se eliminó o terminó siendo útil?

> No hay respuesta correcta única - el razonamiento debe ser concreto y basado en el contexto del negocio.
