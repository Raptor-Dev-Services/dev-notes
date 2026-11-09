# Práctica - S01 - Qué es un LLM y cómo elegirlo

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 1 hora 40 minutos

---

## Objetivo

Hacer la primera llamada a la API de Claude desde C# con el SDK oficial, **contar los tokens antes de gastar**, medir el **costo** y la **latencia** de cada respuesta, y comparar la misma pregunta en dos modelos con una rúbrica calificada a ciegas.

Al final tienes el repositorio `AsistenteCursos` compilando sin warnings, con un comando `comparar` que deja un `comparacion.md` listo para calificar.

---

## El proyecto que crece

Este repositorio es el mismo durante todo el curso. Cada sesión le agrega una pieza:

| Sesión | Lo que agrega |
|--------|---------------|
| S01 | `Asistente.Core/Llm`: modelos, precios y llamadas medidas. Comando `comparar` |
| S02 | Extracción a JSON validado con prompts versionados y pruebas |
| S03 | RAG con PostgreSQL y pgvector |
| S04 | Un agente con dos herramientas y confirmación humana |
| S05 | Un servidor MCP con el catálogo de cursos |
| S06 | Defensas contra prompt injection y evaluación en CI |

---

## Antes de empezar

- [ ] .NET SDK 10.0.300 o posterior: `dotnet --version`
- [ ] La llave de la API del curso, que te dio el instructor por un canal privado. Tiene tope de gasto
- [ ] Un editor con soporte de C# (Rider, VS Code con C# Dev Kit o Visual Studio)

Pon la llave en una variable de entorno de tu terminal. **Nunca** en un archivo del repositorio:

```bash
# macOS o Linux (bash o zsh)
export ANTHROPIC_API_KEY='<SU_LLAVE>'
```

```powershell
# Windows (PowerShell)
$env:ANTHROPIC_API_KEY = '<SU_LLAVE>'
```

La variable vive solo en esa terminal. Si abres otra, la vuelves a definir.

---

## Paso 1 - El esqueleto del repositorio (10 min)

**Objetivo:** las piezas que comparten todos los proyectos: versión del SDK de .NET, reglas de compilación y versiones de paquetes en un solo lugar.

```bash
mkdir AsistenteCursos && cd AsistenteCursos
git init
mkdir -p src/Asistente.Core/Llm src/Asistente.Cli/Comandos
```

`.gitignore`: lo que nunca se sube. `.env` está desde el primer commit.

```
bin/
obj/
.env
bitacoras/
cuarentena.md
```

`global.json` fija el SDK de .NET y activa la plataforma de pruebas que usamos desde la sesión 2:

```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

`Directory.Build.props` aplica a todos los proyectos. `TreatWarningsAsErrors` convierte cualquier warning en error: el repositorio compila limpio o no compila.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

`Directory.Packages.props` es el único lugar donde vive una versión de paquete. El SDK de Anthropic queda fijado en 13.6.0:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Una sola versión fijada por paquete, para todo el repositorio. -->
    <PackageVersion Include="Anthropic" Version="13.6.0" />
  </ItemGroup>
</Project>
```

La solución, en el formato `.slnx` de .NET 10:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Asistente.Cli/Asistente.Cli.csproj" />
    <Project Path="src/Asistente.Core/Asistente.Core.csproj" />
  </Folder>
</Solution>
```

**Lo que debe pasar:** cinco archivos en la raíz y dos carpetas vacías en `src/`.

---

## Paso 2 - Los dos proyectos (5 min)

`Asistente.Core` es la librería que crece cada sesión. `Asistente.Cli` es la consola con la que la ejercitamos.

`src/Asistente.Core/Asistente.Core.csproj`. La versión del paquete no va aquí: sale de `Directory.Packages.props`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Anthropic" />
  </ItemGroup>
</Project>
```

`src/Asistente.Cli/Asistente.Cli.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Asistente.Core/Asistente.Core.csproj" />
  </ItemGroup>
</Project>
```

---

## Paso 3 - Modelos y precios (15 min)

**Objetivo:** los ids de los modelos y sus precios en un solo lugar, para que cambiarlos sea editar un archivo.

`src/Asistente.Core/Llm/Modelos.cs`. `PorOmision` lee la variable `ASISTENTE_MODELO`: puedes cambiar de modelo sin recompilar.

```csharp
namespace Asistente.Core.Llm;

/// <summary>Los modelos vigentes del curso, en un solo lugar.</summary>
public static class Modelos
{
    public const string Fable = "claude-fable-5-1";
    public const string Opus = "claude-opus-5-5";
    public const string Sonnet = "claude-sonnet-5-5";
    public const string Haiku = "claude-haiku-5-5";

    /// <summary>El de las prácticas: el más barato que cumple. Se cambia sin recompilar.</summary>
    public static string PorOmision =>
        Environment.GetEnvironmentVariable("ASISTENTE_MODELO") ?? Haiku;
}
```

`src/Asistente.Core/Llm/TablaDePrecios.cs`. Precios de lista en dólares por millón de tokens, con la tarifa de prompt largo de Haiku 5.5:

```csharp
using Anthropic.Models.Messages;

namespace Asistente.Core.Llm;

/// <summary>Dólares por millón de tokens.</summary>
public sealed record Precio(decimal Entrada, decimal Salida, decimal LecturaCache, decimal EscrituraCache);

public static class TablaDePrecios
{
    // Precios de lista de la API revisados el 2026-11-08. Compáralos con la página
    // oficial de precios antes de presupuestar: cambian y este archivo no se entera.
    private static readonly Dictionary<string, Precio> Precios = new()
    {
        [Modelos.Fable] = new(10.00m, 50.00m, 0.25m, 12.50m),
        [Modelos.Opus] = new(4.00m, 20.00m, 0.20m, 5.00m),
        [Modelos.Sonnet] = new(2.00m, 10.00m, 0.20m, 2.50m),
        [Modelos.Haiku] = new(0.10m, 0.50m, 0.01m, 0.125m),
    };

    // Haiku 5.5 cobra otra tarifa cuando el prompt pasa de 100 mil tokens.
    private static readonly Precio HaikuPromptLargo = new(0.50m, 2.50m, 0.05m, 0.625m);
    private const long LimitePromptCortoHaiku = 100_000;

    public static Precio Para(string modelo, long tokensDelPrompt)
    {
        if (modelo == Modelos.Haiku && tokensDelPrompt > LimitePromptCortoHaiku)
            return HaikuPromptLargo;
        return Precios.TryGetValue(modelo, out var precio)
            ? precio
            : throw new ArgumentException($"No hay precio registrado para {modelo}.", nameof(modelo));
    }

    public static decimal Costo(string modelo, long entrada, long salida, long cacheLeidos, long cacheEscritos)
    {
        var p = Para(modelo, entrada + cacheLeidos + cacheEscritos);
        return (entrada * p.Entrada
              + salida * p.Salida
              + cacheLeidos * p.LecturaCache
              + cacheEscritos * p.EscrituraCache) / 1_000_000m;
    }

    public static decimal Costo(string modelo, Usage uso) =>
        Costo(modelo, uso.InputTokens, uso.OutputTokens,
              uso.CacheReadInputTokens ?? 0, uso.CacheCreationInputTokens ?? 0);
}
```

**Haz la cuenta a mano** antes de seguir: con 2,000 tokens de entrada y 400 de salida en Haiku 5.5, ¿cuánto cuesta una llamada? Debe darte 0.0004 USD. Si no te da, revisa que estés dividiendo entre un millón.

---

## Paso 4 - Medir una llamada (20 min)

**Objetivo:** una clase que cuenta los tokens antes, llama en streaming y mide tokens, costo, tiempo al primer token y tiempo total.

`src/Asistente.Core/Llm/Medicion.cs`:

```csharp
namespace Asistente.Core.Llm;

/// <summary>Lo que costó y tardó una llamada. Se registra en cada una, no solo cuando falla.</summary>
public sealed record Medicion(
    string Modelo,
    long TokensEntrada,
    long TokensSalida,
    long TokensCacheLeidos,
    long TokensCacheEscritos,
    decimal CostoUsd,
    TimeSpan PrimerToken,
    TimeSpan Total,
    string StopReason);
```

`src/Asistente.Core/Llm/LlamadaMedida.cs`:

```csharp
using System.Diagnostics;
using System.Text;
using Anthropic;
using Anthropic.Models.Messages;

namespace Asistente.Core.Llm;

public sealed class LlamadaMedida(AnthropicClient cliente)
{
    /// <summary>Cuenta los tokens de entrada antes de gastar: el conteo no se cobra como inferencia.</summary>
    public async Task<long> ContarTokensAsync(string modelo, string sistema, string pregunta, CancellationToken ct = default)
    {
        var conteo = await cliente.Messages.CountTokens(new MessageCountTokensParams
        {
            Model = modelo,
            System = sistema,
            Messages = [new UserMessageParam { Content = pregunta }],
        }, ct);
        return conteo.InputTokens;
    }

    /// <summary>Pregunta en streaming para medir el tiempo al primer token y el total.</summary>
    public async Task<(string Texto, Medicion Medicion)> PreguntarAsync(
        string modelo, string sistema, string pregunta, CancellationToken ct = default)
    {
        var parametros = new MessageCreateParams
        {
            Model = modelo,
            MaxTokens = 4096,
            System = sistema,
            OutputConfig = new OutputConfig { Effort = Effort.Low },
            Messages = [new UserMessageParam { Content = pregunta }],
        };

        var reloj = Stopwatch.StartNew();
        TimeSpan? primerToken = null;
        var texto = new StringBuilder();
        long entrada = 0, salida = 0, cacheLeidos = 0, cacheEscritos = 0;
        string stopReason = "desconocido";

        // El uso llega repartido: la entrada en message_start y la salida acumulada en message_delta.
        await foreach (var evento in cliente.Messages.CreateStreaming(parametros, ct))
        {
            if (evento.TryPickStart(out var inicio))
            {
                entrada = inicio.Message.Usage.InputTokens;
                cacheLeidos = inicio.Message.Usage.CacheReadInputTokens ?? 0;
                cacheEscritos = inicio.Message.Usage.CacheCreationInputTokens ?? 0;
            }
            else if (evento.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var fragmento))
            {
                primerToken ??= reloj.Elapsed;
                texto.Append(fragmento.Text);
            }
            else if (evento.TryPickDelta(out var cierre))
            {
                salida = cierre.Usage.OutputTokens;
                stopReason = cierre.Delta.StopReason?.ToString() ?? stopReason;
            }
        }
        reloj.Stop();

        var medicion = new Medicion(
            modelo, entrada, salida, cacheLeidos, cacheEscritos,
            TablaDePrecios.Costo(modelo, entrada, salida, cacheLeidos, cacheEscritos),
            primerToken ?? reloj.Elapsed, reloj.Elapsed, stopReason);
        return (texto.ToString(), medicion);
    }
}
```

Fíjate en tres cosas:

1. `ContarTokensAsync` manda lo mismo que la llamada real (modelo, sistema y mensaje) a `count_tokens`. Así el conteo usa el tokenizador del modelo correcto.
2. No hay `temperature`: en la API vigente regresa 400. El que controla cuánto piensa el modelo es `Effort`.
3. El uso llega repartido: la entrada en el evento de inicio y la salida acumulada en el evento de cierre. Si solo lees uno, el costo sale mal.

---

## Paso 5 - El comando comparar (15 min)

**Objetivo:** la misma pregunta a Haiku 5.5 y a Sonnet 5.5, con la medición de cada uno, y un `comparacion.md` con la rúbrica.

`src/Asistente.Cli/Comandos/Comparar.cs`:

```csharp
using System.Text;
using Anthropic;
using Asistente.Core.Llm;

namespace Asistente.Cli.Comandos;

/// <summary>La misma pregunta a dos modelos, con tokens, costo y latencia de cada uno.</summary>
public static class Comparar
{
    private const string Sistema =
        "Eres instructor de bases de datos. Responde en español de México, en un máximo de 120 palabras.";

    public static async Task<int> EjecutarAsync(AnthropicClient cliente, string[] args, CancellationToken ct)
    {
        string pregunta = args.Length > 0
            ? string.Join(' ', args)
            : "¿Cuándo conviene un índice GIN en lugar de un B-tree en PostgreSQL?";
        string[] modelos = [Modelos.Haiku, Modelos.Sonnet];
        var llamada = new LlamadaMedida(cliente);
        var informe = new StringBuilder();
        informe.AppendLine($"# Comparación\n\nPregunta: {pregunta}\n");
        informe.AppendLine("| Modelo | Tokens entrada | Tokens salida | Costo USD | Primer token ms | Total ms |");
        informe.AppendLine("|---|---|---|---|---|---|");

        foreach (var modelo in modelos)
        {
            long estimados = await llamada.ContarTokensAsync(modelo, Sistema, pregunta, ct);
            var (texto, m) = await llamada.PreguntarAsync(modelo, Sistema, pregunta, ct);

            Console.WriteLine($"\n## {modelo}\n{texto}\n");
            Console.WriteLine($"Contados antes: {estimados} | entrada real: {m.TokensEntrada} | salida: {m.TokensSalida}");
            Console.WriteLine($"Costo: {m.CostoUsd:0.000000} USD | primer token: {m.PrimerToken.TotalMilliseconds:0} ms | total: {m.Total.TotalMilliseconds:0} ms | stop_reason: {m.StopReason}");

            informe.AppendLine($"| {modelo} | {m.TokensEntrada} | {m.TokensSalida} | {m.CostoUsd:0.000000} | {m.PrimerToken.TotalMilliseconds:0} | {m.Total.TotalMilliseconds:0} |");
            await File.WriteAllTextAsync($"respuesta-{modelo}.md", texto, ct);
        }

        informe.AppendLine("""

            ## Rúbrica (0 = no cumple, 1 = a medias, 2 = cumple)

            | Criterio | claude-haiku-5-5 | claude-sonnet-5-5 |
            |---|---|---|
            | Exactitud técnica |  |  |
            | Responde lo que se preguntó |  |  |
            | Respeta las 120 palabras |  |  |
            | Da un ejemplo concreto |  |  |
            | **Total (de 8)** |  |  |
            """);
        await File.WriteAllTextAsync("comparacion.md", informe.ToString(), ct);
        Console.WriteLine("Escribí comparacion.md: califica la rúbrica a mano, sin ver qué modelo es cuál.");
        return 0;
    }
}
```

`src/Asistente.Cli/Program.cs`. Crea el cliente sin llave en el código y convierte los errores de credencial y de límite en mensajes legibles:

```csharp
using Anthropic;
using Anthropic.Exceptions;
using Asistente.Cli.Comandos;

if (args.Length == 0)
{
    Console.Error.WriteLine("Uso: dotnet run --project src/Asistente.Cli -- <comando> [argumentos]");
    return 1;
}

// La llave sale del entorno (ANTHROPIC_API_KEY): nunca del código ni del repositorio.
var cliente = new AnthropicClient();
using var cancelar = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelar.Cancel(); };

try
{
    return args[0] switch
    {
        "comparar" => await Comparar.EjecutarAsync(cliente, args[1..], cancelar.Token),
        _ => Desconocido(args[0]),
    };
}
catch (AnthropicUnauthorizedException)
{
    Console.Error.WriteLine("La API rechazó la credencial (401). Revisa la variable ANTHROPIC_API_KEY.");
    return 2;
}
catch (AnthropicRateLimitException)
{
    Console.Error.WriteLine("Límite de peticiones (429). El SDK ya reintentó; espera y vuelve a correrlo.");
    return 3;
}

static int Desconocido(string comando)
{
    Console.Error.WriteLine($"Comando desconocido: {comando}");
    return 1;
}
```

---

## Paso 6 - Compilar sin warnings (5 min)

```bash
dotnet build AsistenteCursos.slnx
```

**Lo que debe pasar:**

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Si aparece un warning, es un error: `TreatWarningsAsErrors` está activo. Corrígelo, no lo silencies.

---

## Paso 7 - La primera llamada medida (15 min)

```bash
dotnet run --project src/Asistente.Cli -- comparar
```

**Lo que debe pasar**, para cada modelo:

```
## claude-haiku-5-5
<la respuesta, de no más de 120 palabras>

Contados antes: <n> | entrada real: <n> | salida: <n>
Costo: <0.000000> USD | primer token: <ms> ms | total: <ms> ms
```

Y al final:

```
Escribí comparacion.md: califica la rúbrica a mano, sin ver qué modelo es cuál.
```

Revisa:

- [ ] "Contados antes" y "entrada real" coinciden o casi: los dos los calcula el tokenizador del mismo modelo
- [ ] El costo de Sonnet es mayor que el de Haiku por un factor cercano a la diferencia de precios
- [ ] El tiempo al primer token es menor que el total en los dos
- [ ] Hay tres archivos nuevos: `respuesta-claude-haiku-5-5.md`, `respuesta-claude-sonnet-5-5.md` y `comparacion.md`

Corre el comando **dos veces más** y anota cómo cambian la latencia y los tokens de salida entre corridas. Con una sola medición no se puede decir nada.

Prueba también con una pregunta tuya:

```bash
dotnet run --project src/Asistente.Cli -- comparar "¿Qué es una transacción en una base de datos?"
```

---

## Paso 8 - Calificar a ciegas (15 min)

**Objetivo:** que la reputación del modelo no decida la nota.

1. Pásale a un compañero los dos archivos `respuesta-*.md`
2. Él los renombra al azar como `A.md` y `B.md` y anota cuál es cuál en un papel que no te enseña
3. Tú calificas A y B con la rúbrica de `comparacion.md` (0, 1 o 2 por criterio)
4. Él revela cuál era cuál y tú pasas las notas a las columnas de cada modelo

Al final de `comparacion.md` escribe dos líneas: qué modelo ganó en calidad, y si la diferencia de calidad justifica la diferencia de costo **para esta pregunta**.

---

## Paso 9 - Verlo fallar (15 min)

**Si no lo viste fallar, no lo probaste.** Dos fallas a propósito.

### 9.1 Sin llave

Corre el comando en una terminal sin la variable:

```bash
# macOS o Linux
env -u ANTHROPIC_API_KEY dotnet run --project src/Asistente.Cli -- comparar
echo $?
```

```powershell
# Windows (PowerShell)
Remove-Item Env:ANTHROPIC_API_KEY
dotnet run --project src/Asistente.Cli -- comparar
$LASTEXITCODE
```

**Lo que debe pasar:** un mensaje de una línea, no una traza de excepción, y código de salida 2:

```
La API rechazó la credencial (401). Revisa la variable ANTHROPIC_API_KEY.
```

Si ves una traza larga, el `catch` de `Program.cs` no está atrapando `AnthropicUnauthorizedException`: revísalo. Vuelve a definir la variable antes de seguir.

### 9.2 Sin espacio para responder

En `LlamadaMedida.cs`, cambia por un momento `MaxTokens = 4096` por `MaxTokens = 16` y corre `comparar` otra vez.

**Lo que debe pasar:**

- La salida reporta 16 tokens o casi
- El texto sale cortado a media frase o vacío: el pensamiento adaptativo también cuenta contra `max_tokens` y a veces se come todo el presupuesto antes de escribir una palabra
- La línea de costo dice `stop_reason: "max_tokens"` en lugar de `stop_reason: "end_turn"`

Regresa `MaxTokens = 4096`, vuelve a compilar y confirma que todo regresa a la normalidad.

**Pregunta para tu bitácora:** si guardaras en una base de datos lo que regresó la llamada con 16 tokens sin revisar `stop_reason`, ¿qué habrías guardado?

---

## Paso 10 - Primer commit (5 min)

Revisa que no se vaya nada que no deba:

```bash
git status
```

No debe aparecer `bin/`, `obj/` ni ningún archivo con la llave. Entonces:

```bash
git add .
git commit -m "feat(llm): llamadas medidas y comando comparar"
```

---

## Evidencia que entregas

- [ ] El repositorio compila con `0 Warning(s)` y `0 Error(s)`
- [ ] `comparacion.md` con la tabla de medición de las dos corridas y la rúbrica calificada a ciegas
- [ ] Una captura o copia del mensaje del 401 del paso 9.1
- [ ] Una nota de tres líneas sobre lo que viste con `MaxTokens = 16`
- [ ] El commit, sin llaves ni carpetas `bin/` u `obj/`
