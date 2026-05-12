# 10 — Facade

**Categoría:** Estructural

**Intención:** Proporciona una interfaz simplificada a un conjunto complejo de clases, una librería o un framework.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.4 Structural Patterns: Facade

---

## El problema

Tu código necesita trabajar con un subsistema complejo: un framework de video con decenas de clases (`VideoFile`, `OggCompressionCodec`, `MPEG4CompressionCodec`, `CodecFactory`, `BitrateReader`, `AudioMixer`). Para convertir un video necesitas orquestar 8 clases de forma específica. Tu código queda acoplado a todos esos detalles.

```csharp
// ❌ Sin Facade — el cliente conoce todos los subsistemas
var file    = new VideoFile("funny-cats.ogg");
var codec   = CodecFactory.extract(file);
var buffer1 = BitrateReader.read(file, codec);
var buffer2 = BitrateReader.convert(buffer1, new MPEG4CompressionCodec());
var audio   = (new AudioMixer()).fix(buffer2);
new MP4ContainerMuxer().produce(buffer1, buffer2, audio);
// 6 clases, orden específico, múltiples llamadas → el cliente está acoplado a todo esto
```

---

## Analogía

El mostrador de un hotel. Cuando te hospedas en un hotel, no coordinas directamente con la cocina, el personal de limpieza, el bar, el spa, y el departamento de reservas. El conserje (Facade) es tu único punto de contacto. Dices "quiero cenar a las 7" y el conserje coordina todo el backend complejo.

---

## Estructura

```
Client → Facade

Facade
├── _subsystem1: Subsystem1
├── _subsystem2: Subsystem2
└── Operation()
    ├── _subsystem1.operation1()
    ├── _subsystem2.operation1()
    ├── _subsystem1.operationN()
    └── _subsystem2.operationZ()

Subsystem1   ← puede existir independientemente; el cliente puede acceder directo si necesita
Subsystem2
```

---

## Código del ejemplo conceptual

```csharp
// Facade — interfaz simple sobre dos subsistemas complejos
public class Facade
{
    protected Subsystem1 _subsystem1;
    protected Subsystem2 _subsystem2;

    public Facade(Subsystem1 subsystem1, Subsystem2 subsystem2)
    {
        _subsystem1 = subsystem1;
        _subsystem2 = subsystem2;
    }

    // Una sola operación pública orquesta la complejidad interna
    public string Operation()
    {
        string result = "Facade initializes subsystems:\n";
        result += _subsystem1.operation1();
        result += _subsystem2.operation1();
        result += "Facade orders subsystems to perform the action:\n";
        result += _subsystem1.operationN();
        result += _subsystem2.operationZ();
        return result;
    }
}

// Subsistemas — pueden usarse directamente si el cliente necesita más control
public class Subsystem1
{
    public string operation1() => "Subsystem1: Ready!\n";
    public string operationN() => "Subsystem1: Go!\n";
}

public class Subsystem2
{
    public string operation1() => "Subsystem2: Get ready!\n";
    public string operationZ() => "Subsystem2: Fire!\n";
}

// El cliente solo habla con la Facade:
var facade = new Facade(new Subsystem1(), new Subsystem2());
Console.Write(facade.Operation());
/*
"Facade initializes subsystems:
Subsystem1: Ready!
Subsystem2: Get ready!
Facade orders subsystems to perform the action:
Subsystem1: Go!
Subsystem2: Fire!"
*/
```

---

## Ejemplo real: VideoConverter Facade

```csharp
// Subsistemas complejos
public class VideoFile { public VideoFile(string name) { } }
public class OggCompressionCodec { }
public class MPEG4CompressionCodec { }

public class CodecFactory
{
    public static object extract(VideoFile file) => new OggCompressionCodec();
}

public class BitrateReader
{
    public static byte[] read(VideoFile file, object codec) => new byte[0];
    public static byte[] convert(byte[] buffer, object codec) => new byte[0];
}

public class AudioMixer
{
    public byte[] fix(byte[] buffer) => new byte[0];
}

// La Facade — un método simple que oculta toda la complejidad
public class VideoConverter
{
    public byte[] Convert(string filename, string format)
    {
        var file  = new VideoFile(filename);
        var codec = CodecFactory.extract(file);

        object destinationCodec = format == "mp4"
            ? new MPEG4CompressionCodec()
            : (object)new OggCompressionCodec();

        var buffer1 = BitrateReader.read(file, codec);
        var buffer2 = BitrateReader.convert(buffer1, destinationCodec);
        var audio   = new AudioMixer().fix(buffer2);

        return buffer2;  // el resultado del video convertido
    }
}

// El cliente ahora tiene una interfaz simple:
var converter = new VideoConverter();
byte[] mp4Video = converter.Convert("funny-cats.ogg", "mp4");
// Una línea en lugar de 6
```

---

## En este proyecto

```csharp
// BaseApiController es una Facade sobre el mediador
public abstract class BaseApiController : ControllerBase
{
    private IMediator _mediator;

    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected BaseApiController(IMediator mediator)
    {
        _mediator = mediator;
    }
}

// ExampleUsersController hereda BaseApiController:
// El controller no sabe cómo funciona internamente el mediador,
// el pipeline, ni el presenter. Solo llama:
_ = await Mediator.Send(new GetExampleUserRequest(id), ct);
// ← Una línea que oculta: Handler → InteractorPipeline → Publish → Presenter
// BaseApiController + Mediator = Facade sobre toda la arquitectura CQRS
```

```csharp
// ServiceCollectionEx.cs en cada capa también es una Facade:
// "Registra todos los servicios de Application" — una llamada oculta N registros
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddMediator();
    services.AddScoped<GetExampleUserHandler>();
    services.AddScoped<InsertExampleUserHandler>();
    // ... N registros
    return services;
}

// El cliente (Program.cs) solo necesita:
builder.Services.AddApplicationServices();  // ← Facade: una línea
```

---

## Facade vs Adapter vs Proxy

| Patrón | Propósito | Cuándo |
|--------|-----------|--------|
| **Facade** | Simplificar una interfaz compleja | Hay muchas clases, quieres una sola interfaz simple |
| **Adapter** | Convertir una interfaz en otra | Interfaces incompatibles que necesitas conectar |
| **Proxy** | Controlar acceso a un objeto | Mismo interfaz, pero añades control de acceso/caché/logging |

---

## Cuándo usar

- Cuando necesitas una interfaz simple para un subsistema complejo.
- Cuando hay mucho acoplamiento entre clientes y clases de implementación.
- Cuando quieres estructurar un subsistema en capas — cada capa expone una Facade.
- Para integración con librerías externas: la Facade aísla el código de los detalles de la librería.

## Cuándo NO usar

- Cuando el "subsistema" es simple — una Facade para 2 clases es innecesaria.
- Cuando la Facade se convierte en un God Object que hace demasiado — dividirla.
- Cuando los clientes realmente necesitan acceso a la funcionalidad detallada del subsistema.


---

*Rogelio Arriaga Gonzalez*
