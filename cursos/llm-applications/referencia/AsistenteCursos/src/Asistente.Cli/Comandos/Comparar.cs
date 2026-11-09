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
