# 05 — Herencia y Polimorfismo

La herencia permite que una clase tome el comportamiento de otra y lo extienda. El polimorfismo permite tratar objetos distintos de forma uniforme.

---

## Herencia básica

Una clase puede heredar de otra usando `:`. Obtiene todos los miembros públicos y protegidos del padre.

```csharp
// Clase padre (base)
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;
    
    protected BaseApiController(IMediator mediator)
    {
        Mediator = mediator;
    }
    
    // Método común para todos los controllers
    protected IActionResult HandleError(Exception ex, ILogger logger, ResultViewModel<object> viewModel)
    {
        logger.LogError(ex, "Error no controlado");
        var inner = ex;
        while (inner.InnerException != null) inner = inner.InnerException!;
        return StatusCode(500, viewModel.Fail(inner.Message));
    }
}

// Clase hija — hereda todo de BaseApiController
public sealed class ExampleUsersController : BaseApiController
{
    private readonly ILogger<ExampleUsersController> _logger;
    private readonly ResultViewModel<ExampleUsersController> _viewModel;

    public ExampleUsersController(
        IMediator mediator,
        ILogger<ExampleUsersController> logger,
        ResultViewModel<ExampleUsersController> viewModel)
        : base(mediator)   // ← llama constructor del padre
    {
        _logger    = logger;
        _viewModel = viewModel;
    }
    
    // Tiene acceso a "Mediator" — heredado del padre
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        try
        {
            _ = await Mediator.Send(new GetExampleUserRequest(id), ct);
            return _viewModel.IsSuccess ? Ok(_viewModel) : StatusCode(500, _viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetById id={UserId}", id);
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException!;
            return StatusCode(500, _viewModel.Fail(inner.Message));
        }
    }
}
```

---

## `virtual` — método que se puede sobreescribir

```csharp
public class Animal
{
    public string Nombre { get; set; } = string.Empty;
    
    // virtual = la subclase PUEDE sobreescribir este método
    public virtual string HacerSonido()
    {
        return "...";  // comportamiento por defecto
    }
    
    // No virtual = no se puede sobreescribir (sealed por defecto)
    public string ObtenerNombre() => Nombre;
}

public class Perro : Animal
{
    // override = sobreescribo el virtual del padre
    public override string HacerSonido()
    {
        return "Guau";  // reemplazo completo
    }
}

public class Gato : Animal
{
    public override string HacerSonido()
    {
        // base.HacerSonido() llama la implementación del padre
        var sonidoBase = base.HacerSonido();  // "..."
        return $"Miau (base era: {sonidoBase})";
    }
}

Animal a = new Perro { Nombre = "Rex" };
Console.WriteLine(a.HacerSonido()); // "Guau" — se ejecuta el Perro, no el Animal
```

---

## `abstract` — método que DEBE sobreescribirse

```csharp
public abstract class FiguraGeometrica
{
    public string Color { get; set; } = "negro";
    
    // abstract = sin implementación, la subclase DEBE implementarlo
    public abstract double CalcularArea();
    public abstract double CalcularPerimetro();
    
    // Método concreto — usa los abstractos
    public void Describir()
    {
        Console.WriteLine($"Figura {Color}: área={CalcularArea():F2}, perímetro={CalcularPerimetro():F2}");
    }
}

public sealed class Circulo : FiguraGeometrica
{
    public double Radio { get; init; }
    
    // OBLIGATORIO implementar los dos abstractos
    public override double CalcularArea() => Math.PI * Radio * Radio;
    public override double CalcularPerimetro() => 2 * Math.PI * Radio;
}

public sealed class Rectangulo : FiguraGeometrica
{
    public double Ancho { get; init; }
    public double Alto { get; init; }
    
    public override double CalcularArea() => Ancho * Alto;
    public override double CalcularPerimetro() => 2 * (Ancho + Alto);
}

// Uso polimórfico
var figuras = new FiguraGeometrica[]
{
    new Circulo  { Radio = 5, Color = "rojo" },
    new Rectangulo { Ancho = 4, Alto = 3, Color = "azul" }
};

foreach (var figura in figuras)
{
    figura.Describir();  // llama CalcularArea/Perimetro correcto para cada tipo
}
// "Figura rojo: área=78.54, perímetro=31.42"
// "Figura azul: área=12.00, perímetro=14.00"
```

---

## `sealed` sobre un `override` — sellar la cadena

Una vez sobreescrito, puedes sellarlo para que nadie más lo sobreescriba:

```csharp
public abstract class Base
{
    public abstract void Metodo();
}

public class Medio : Base
{
    public override void Metodo()
    {
        Console.WriteLine("Implementación en Medio");
    }
}

public sealed class Final : Medio
{
    public sealed override void Metodo()
    {
        base.Metodo();
        Console.WriteLine("Y algo más en Final");
    }
    // Nadie puede heredar de Final ni sobreescribir Metodo() más allá
}
```

---

## Polimorfismo

Un mismo tipo (la clase base o interfaz) puede contener objetos de distintos tipos concretos:

```csharp
// Polimorfismo con clase abstracta
FiguraGeometrica f;
f = new Circulo { Radio = 5 };     // un Circulo ES una FiguraGeometrica
f = new Rectangulo { Ancho = 3 };  // un Rectangulo ES una FiguraGeometrica

// Polimorfismo con interfaz
IExampleUserRepository repo;
repo = new ExampleUserRepository(sql);   // la implementación real
repo = new FakeExampleUserRepository();  // un fake para tests

// El código que usa "repo" no sabe ni le importa cuál es
var user = await repo.GetByPublicIdAsync(id, ct);
```

### En el Presenter — polimorfismo con pattern matching

```csharp
// "notification" es del tipo base GetExampleUserResponse
// Puede ser GetExampleUserSuccess o GetExampleUserNotFoundFailure
public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
{
    if (notification is ISuccess<ExampleUserDto> success)
        _viewModel.Set(success);           // es un éxito con DTO
    else if (notification is INotFoundFailure notFound)
        _viewModel.Fail(notFound.Message); // es un 404
    else if (notification is IFailure failure)
        _viewModel.Fail(failure.Message);  // es otro tipo de error
    
    return Task.CompletedTask;
}
```

---

## `new` en métodos — ocultar (shadowing)

Diferente de `override` — oculta el método del padre en vez de sobreescribirlo:

```csharp
public class Padre
{
    public virtual void Metodo() => Console.WriteLine("Padre");
}

public class Hijo : Padre
{
    // override — sobreescribe correctamente
    public override void Metodo() => Console.WriteLine("Hijo override");
}

public class HijoShadow : Padre
{
    // new — oculta el del padre (no polimórfico)
    public new void Metodo() => Console.WriteLine("Hijo shadow");
}

Padre p1 = new Hijo();
p1.Metodo();  // "Hijo override" — polimorfismo funciona

Padre p2 = new HijoShadow();
p2.Metodo();  // "Padre" — NO polimórfico, se ejecuta el del padre
```

**Nunca uses `new` para ocultar métodos** a menos que sea absolutamente necesario. Úsalo solo cuando estás trabajando con código legacy que no puedes cambiar.

---

## Herencia en records

Los records siguen las mismas reglas de herencia, pero con restricciones:
- Un record solo puede heredar de otro record (no de una clase)
- Una clase no puede heredar de un record

```csharp
// Jerarquía de records del proyecto
public abstract record GetExampleUserResponse : IResponse;  // base

public sealed record GetExampleUserSuccess(ExampleUserDto Data)
    : GetExampleUserResponse, ISuccess<ExampleUserDto>;       // subclase sellada

public sealed record GetExampleUserNotFoundFailure(string Message)
    : GetExampleUserResponse, INotFoundFailure;               // subclase sellada

// Comprobación de tipo con is:
GetExampleUserResponse response = GetUserFromService();

if (response is GetExampleUserSuccess s)
    Console.WriteLine($"OK: {s.Data.FullName}");
else if (response is GetExampleUserNotFoundFailure f)
    Console.WriteLine($"Error: {f.Message}");
```

---

## `is` y casting — verificar y convertir el tipo

```csharp
// is — verificar sin lanzar excepción
if (response is GetExampleUserSuccess)
    Console.WriteLine("Es un éxito");

// is con asignación — verificar y asignar (C# 7+)
if (response is GetExampleUserSuccess success)
    Console.WriteLine(success.Data.FullName);  // "success" está casteado

// as — castear sin lanzar excepción (retorna null si falla)
var success = response as GetExampleUserSuccess;
if (success != null)
    Console.WriteLine(success.Data.FullName);

// (T)obj — casteo directo — lanza InvalidCastException si falla
var success = (GetExampleUserSuccess)response;  // ¡cuidado!
```

**Prefiere `is` con asignación** sobre `as` + null check — es más limpio y seguro.

---

## Herencia y DI container

El container no tiene problemas con herencia, pero debes registrar el tipo correcto:

```csharp
// Si ExampleUsersController hereda de BaseApiController:
// No necesitas registrar BaseApiController — solo el concreto:
services.AddScoped<ExampleUsersController>();
// (Los controllers se registran automáticamente con AddControllers())

// Para handlers — AddMediator escanea y registra todos los IRequestHandler<,>:
services.AddMediator(typeof(GetExampleUserHandler).Assembly);
// Registra: IRequestHandler<GetExampleUserRequest, GetExampleUserResponse> → GetExampleUserHandler
```

---

## Anti-patrones de herencia

### Anti-patrón 1 — Herencia para reutilizar código (favorece composición)

```csharp
// ❌ Herencia incorrecta — Cuadrado hereda de Rectángulo solo para reutilizar código
// pero viola el Liskov Substitution Principle: si tienes un Rectangulo, 
// puedes cambiar Ancho y Alto independientemente — con Cuadrado no puedes
public class Cuadrado : Rectangulo
{
    public new double Ancho
    {
        get => base.Ancho;
        set { base.Ancho = value; base.Alto = value; }  // rompe el contrato de Rectangulo
    }
}

// ✓ Composición — Cuadrado TIENE un lado, no ES un Rectangulo
public sealed class Cuadrado
{
    public double Lado { get; init; }
    public double Area => Lado * Lado;
    public double Perimetro => 4 * Lado;
}
```

### Anti-patrón 2 — Herencia profunda

```csharp
// ❌ Cadena larga de herencia — difícil de seguir y mantener
Animal → Mamifero → Vertebrado → Perro → Labrador → LabradorNegro

// ✓ Plana con interfaces — más mantenible
public interface IAnimal { string HacerSonido(); }
public interface IDomestico { string ObtenerDuenio(); }
public sealed class Labrador : IAnimal, IDomestico
{
    public string HacerSonido() => "Guau";
    public string ObtenerDuenio() => "Juan";
}
```

### Anti-patrón 3 — Sobreescribir para cambiar el comportamiento esperado (viola LSP)

```csharp
// ❌ Viola Liskov Substitution Principle
public class RepoBase
{
    public virtual Task<Usuario?> GetByIdAsync(Guid id) { /* DB query */ }
}

public class RepoCacheado : RepoBase
{
    public override Task<Usuario?> GetByIdAsync(Guid id)
    {
        // ❌ Si el cache falla, lanza excepción en vez de retornar null
        // El código que usaba RepoBase espera null, no excepción
        throw new CacheException("Cache caído");
    }
}
```


---

*Rogelio Arriaga Gonzalez*
