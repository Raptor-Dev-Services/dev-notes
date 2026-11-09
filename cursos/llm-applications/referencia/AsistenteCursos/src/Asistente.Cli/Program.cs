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
        "extraer" => await Extraer.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "ingestar" => await Ingestar.EjecutarAsync(args[1..], cancelar.Token),
        "preguntar" => await Preguntar.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "agente" => await Agente.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "mcp" => await ClienteMcp.EjecutarAsync(args[1..], cancelar.Token),
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
