using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Asistente.Core.Extraccion;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;

namespace Asistente.Cli.Comandos;

public static class Extraer
{
    public static async Task<int> EjecutarAsync(AnthropicClient cliente, string[] args, CancellationToken ct)
    {
        // El mensaje llega como argumento o por la entrada estándar: echo "..." | dotnet run -- extraer
        string mensaje = args.Length > 0 ? string.Join(' ', args) : await Console.In.ReadToEndAsync(ct);
        var prompt = RepositorioPrompts.DelRepositorio().Cargar("extraccion-soporte", 1);
        var extractor = new ExtractorSoporte(cliente, prompt, Modelos.PorOmision);

        var r = await extractor.ExtraerAsync(mensaje, ct);
        Console.WriteLine($"Prompt {r.PromptId} ({prompt.Huella}) | intentos: {r.Intentos} | costo: {r.CostoUsd:0.000000} USD | tokens leídos de caché: {r.TokensCacheLeidos}");
        if (r.Aceptada)
        {
            var opciones = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },   // "pago", no 1
            };
            Console.WriteLine(JsonSerializer.Serialize(r.Solicitud, opciones));
            return 0;
        }
        foreach (var f in r.Fallas) Console.WriteLine($"[{f.Tipo}] {f.Mensaje}");
        return 4;
    }
}
