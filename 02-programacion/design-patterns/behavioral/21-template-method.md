# 21 — Template Method

**Categoría:** Conductual

**Intención:** Define el esqueleto de un algoritmo en la clase base, pero deja que las subclases sobrescriban pasos específicos del algoritmo sin cambiar su estructura general.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.5 Behavioral Patterns: Template Method

---

## El problema

Tienes código para analizar datos de diferentes formatos (CSV, XML, JSON). El proceso siempre es: leer datos → parsear → analizar → generar reporte. Los pasos "leer" y "generar reporte" son iguales en todos. "Parsear" y "analizar" varían por formato. Sin el patrón, duplicas el esqueleto del algoritmo en cada subclase.

```csharp
// ❌ Sin Template Method — el esqueleto se duplica en cada clase
class CsvParser
{
    public void Process(string file)
    {
        var raw = ReadFile(file);   // igual en todas
        var data = ParseCsv(raw);   // específico
        Analyze(data);               // específico
        GenerateReport(data);        // igual en todas
    }
}

class XmlParser
{
    public void Process(string file)
    {
        var raw = ReadFile(file);   // igual — duplicado
        var data = ParseXml(raw);   // específico
        Analyze(data);               // específico
        GenerateReport(data);        // igual — duplicado
    }
}
```

---

## Analogía

Una receta de cocina con pasos opcionales. La receta de "preparar pasta" tiene pasos fijos: hervir agua, cocinar pasta, escurrir. El paso "preparar la salsa" es el que varía por receta (carbonara, arrabiata, al pesto). El Template Method define el esqueleto (hervir, cocinar, escurrir, agregar salsa) y deja que cada receta concreta implemente "preparar la salsa".

---

## Estructura

```
AbstractClass
├── TemplateMethod()          ← el esqueleto — FINAL, no se sobreescribe
│   ├── BaseOperation1()      ← implementado en la base
│   ├── RequiredOperations1() ← abstract — DEBE implementar la subclase
│   ├── BaseOperation2()      ← implementado en la base
│   ├── Hook1()               ← virtual vacío — PUEDE sobreescribir la subclase
│   ├── RequiredOperation2()  ← abstract — DEBE implementar la subclase
│   ├── BaseOperation3()      ← implementado en la base
│   └── Hook2()               ← virtual vacío — PUEDE sobreescribir

ConcreteClass1 : AbstractClass
├── RequiredOperations1() → implementación específica
├── RequiredOperation2()  → implementación específica
└── (no sobreescribe hooks)

ConcreteClass2 : AbstractClass
├── RequiredOperations1() → implementación específica
├── RequiredOperation2()  → implementación específica
└── Hook1() → implementación específica (sobreescribe el hook)
```

---

## Código del ejemplo conceptual

```csharp
abstract class AbstractClass
{
    // El Template Method — define el esqueleto del algoritmo
    // No marcar virtual — las subclases NO deben sobreescribir el esqueleto
    public void TemplateMethod()
    {
        BaseOperation1();
        RequiredOperations1();  // abstract — subclase lo implementa
        BaseOperation2();
        Hook1();                // hook — subclase puede sobreescribirlo o no
        RequiredOperation2();   // abstract — subclase lo implementa
        BaseOperation3();
        Hook2();
    }

    // Operaciones con implementación en la base
    protected void BaseOperation1() =>
        Console.WriteLine("AbstractClass says: I am doing the bulk of the work");

    protected void BaseOperation2() =>
        Console.WriteLine("AbstractClass says: But I let subclasses override some operations");

    protected void BaseOperation3() =>
        Console.WriteLine("AbstractClass says: But I am doing the bulk of the work anyway");

    // Abstract — OBLIGATORIO implementar en subclase
    protected abstract void RequiredOperations1();
    protected abstract void RequiredOperation2();

    // Hooks — OPCIONAL sobreescribir; tienen implementación vacía por defecto
    protected virtual void Hook1() { }
    protected virtual void Hook2() { }
}

// Subclase 1 — implementa los abstractos, no toca los hooks
class ConcreteClass1 : AbstractClass
{
    protected override void RequiredOperations1() =>
        Console.WriteLine("ConcreteClass1 says: Implemented Operation1");

    protected override void RequiredOperation2() =>
        Console.WriteLine("ConcreteClass1 says: Implemented Operation2");
}

// Subclase 2 — implementa los abstractos Y sobreescribe un hook
class ConcreteClass2 : AbstractClass
{
    protected override void RequiredOperations1() =>
        Console.WriteLine("ConcreteClass2 says: Implemented Operation1");

    protected override void RequiredOperation2() =>
        Console.WriteLine("ConcreteClass2 says: Implemented Operation2");

    // Sobreescribe el hook — punto de extensión opcional
    protected override void Hook1() =>
        Console.WriteLine("ConcreteClass2 says: Overridden Hook1");
}

// El cliente llama el TemplateMethod — el algoritmo se ejecuta completo
static void ClientCode(AbstractClass abstractClass)
{
    abstractClass.TemplateMethod();
}

ClientCode(new ConcreteClass1());
// AbstractClass says: I am doing the bulk of the work
// ConcreteClass1 says: Implemented Operation1
// AbstractClass says: But I let subclasses override some operations
// (Hook1 vacío — no imprime nada)
// ConcreteClass1 says: Implemented Operation2
// AbstractClass says: But I am doing the bulk of the work anyway

ClientCode(new ConcreteClass2());
// AbstractClass says: I am doing the bulk of the work
// ConcreteClass2 says: Implemented Operation1
// AbstractClass says: But I let subclasses override some operations
// ConcreteClass2 says: Overridden Hook1  ← hook sobreescrito
// ConcreteClass2 says: Implemented Operation2
// AbstractClass says: But I am doing the bulk of the work anyway
```

---

## Ejemplo real: parsers de datos

```csharp
// El esqueleto del algoritmo de análisis
abstract class DataMiner
{
    // Template Method — no virtual
    public void Mine(string path)
    {
        var rawData  = ExtractData(path);    // paso 1: extraer
        var data     = ParseData(rawData);   // paso 2: parsear — varía
        var analysis = AnalyzeData(data);    // paso 3: analizar — puede variar
        SendReport(analysis);                // paso 4: reportar
    }

    // Implementación base compartida
    protected string ExtractData(string path)
    {
        Console.WriteLine($"Reading file: {path}");
        return File.ReadAllText(path);
    }

    protected void SendReport(string report)
    {
        Console.WriteLine($"Sending report:\n{report}");
    }

    // Abstract — cada formato implementa su parsing
    protected abstract string[] ParseData(string rawData);

    // Hook — análisis por defecto, subclase puede personalizar
    protected virtual string AnalyzeData(string[] data)
    {
        return $"Found {data.Length} records. Average length: {data.Average(s => s.Length):F1}";
    }
}

// CSV miner
class CsvDataMiner : DataMiner
{
    protected override string[] ParseData(string rawData)
    {
        Console.WriteLine("Parsing CSV data...");
        return rawData.Split('\n').Skip(1).ToArray(); // skip header
    }
}

// XML miner — sobreescribe también el análisis
class XmlDataMiner : DataMiner
{
    protected override string[] ParseData(string rawData)
    {
        Console.WriteLine("Parsing XML data...");
        // parsear XML...
        return new[] { "node1", "node2", "node3" };
    }

    // Sobreescribe el hook para análisis específico de XML
    protected override string AnalyzeData(string[] data)
    {
        return $"XML Analysis: {data.Length} nodes found.";
    }
}

// El cliente usa el Template Method sin saber el formato:
DataMiner miner = new CsvDataMiner();
miner.Mine("data.csv");

miner = new XmlDataMiner();
miner.Mine("data.xml");
```

---

## Abstract vs Hook — cuándo usar cada uno

| Tipo | Modificador | Semántica |
|------|------------|-----------|
| `abstract` | Obligatorio override | El paso ES diferente en cada subclase |
| `virtual` vacío (hook) | Opcional override | El paso PUEDE ser diferente, pero tiene default |
| `protected` base | No sobreescribir | El paso es IGUAL en todas las subclases |

---

## En este proyecto

```csharp
// BaseApiController ES un Template Method:
public abstract class BaseApiController : ControllerBase
{
    private IMediator _mediator;

    // Constructor del esqueleto — la "plantilla"
    protected BaseApiController(IMediator mediator) { _mediator = mediator; }

    // Operación base compartida — el Mediator resuelto del scope de la request
    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
}

// Cada Controller "concreto" extiende el esqueleto:
public sealed class ExampleUsersController : BaseApiController
{
    // Constructor que llama al base (base de Template Method)
    public ExampleUsersController(
        IMediator mediator, ILogger<ExampleUsersController> logger,
        ResultViewModel<ExampleUsersController> viewModel) : base(mediator)
    { _logger = logger; _viewModel = viewModel; }

    // Operación concreta — el endpoint específico que la subclase define
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            _ = await Mediator.Send(new GetExampleUserRequest(id), ct);  // ← paso del template
            return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetById");
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException!;
            return StatusCode(500, _viewModel.Fail(inner.Message));
        }
    }
}
// El esqueleto (mediador, gestión de errores) viene del BaseApiController.
// Los endpoints específicos los define cada subclase.
```

---

## Cuándo usar

- Cuando quieres que los clientes extiendan solo ciertos pasos de un algoritmo, no el algoritmo completo.
- Cuando tienes múltiples clases que contienen el mismo algoritmo con pequeñas diferencias.
- Para evitar duplicación de código: el esqueleto va en la clase base, solo los detalles se delegan.

## Cuándo NO usar

- Cuando el algoritmo raramente varía — sobreingeniería.
- Cuando el esqueleto cambia frecuentemente — cada cambio afecta todas las subclases.
- Cuando Strategy (composición) sería más flexible que Template Method (herencia).

### Template Method vs Strategy

| Aspecto | Template Method | Strategy |
|---------|----------------|---------|
| Mecanismo | Herencia | Composición |
| Granularidad | Pasos del algoritmo | El algoritmo completo |
| Cambio | En compilación (subclase) | En runtime (setStrategy) |
| Preferir | Cuando el esqueleto es estable | Cuando el algoritmo completo varía |


---

*Rogelio Arriaga Gonzalez*
