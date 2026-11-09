using Anthropic;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;
using Asistente.Core.Rag;

namespace Asistente.Cli.Comandos;

public static class Preguntar
{
    public static async Task<int> EjecutarAsync(AnthropicClient cliente, string[] args, CancellationToken ct)
    {
        if (args.Length == 0) { Console.Error.WriteLine("Uso: preguntar <pregunta>"); return 1; }
        await using var fuente = AlmacenVectorial.CrearFuente(Servicios.CadenaDeConexion());
        var asistente = new AsistenteDocumentos(
            cliente,
            Servicios.Embeddings(),
            new AlmacenVectorial(fuente),
            RepositorioPrompts.DelRepositorio().Cargar("asistente-documentos", 2),
            Modelos.PorOmision,
            new OpcionesRag(DominiosPermitidos: ["academia.example"]));

        var r = await asistente.PreguntarAsync(string.Join(' ', args), ct);
        Console.WriteLine(r.Texto);
        foreach (var f in r.Citadas) Console.WriteLine($"  [{f.Clave}] {f.Fuente} > {f.Seccion}");
        if (r.Motivo is not null) Console.WriteLine($"  (motivo: {r.Motivo})");
        foreach (var x in r.Retirados ?? []) Console.WriteLine($"  Retirado por el filtro de salida: {x}");
        Console.WriteLine("  Recuperados: " + string.Join(", ", r.Recuperadas.Select(f => $"{f.Clave} {f.Distancia:0.000}")));
        Console.WriteLine($"  Costo: {r.CostoUsd:0.000000} USD");
        return 0;
    }
}
