using System.Text.Json;
using Anthropic;
using Asistente.Core;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;
using Asistente.Core.Rag;

namespace Asistente.Pruebas;

public sealed record CasoRag(string Id, string Pregunta, bool Sabe, string? Fuente);

/// <summary>Tasa de aciertos sobre casos/rag.jsonl. Necesita llave, base indexada y Ollama.</summary>
[Trait("Categoria", "Eval")]
[Collection("BaseDeDatos")]
public class EvalRag(ITestOutputHelper salida)
{
    private const double Umbral = 0.85;

    [Fact]
    public async Task El_asistente_acierta_y_sabe_decir_no_lo_se()
    {
        Requiere.LlaveDeApi();
        var ct = TestContext.Current.CancellationToken;
        await using var fuente = AlmacenVectorial.CrearFuente(Requiere.BaseDeDatos());
        var embeddings = new EmbeddingsOllama(new HttpClient
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434"),
        });
        var prompt = RepositorioPrompts.DelRepositorio().Cargar("asistente-documentos", 2);
        var asistente = new AsistenteDocumentos(new AnthropicClient(), embeddings, new AlmacenVectorial(fuente),
            prompt, Modelos.PorOmision, new OpcionesRag(DominiosPermitidos: ["academia.example"]));

        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var casos = File.ReadLines(Path.Combine(Rutas.Buscar("casos"), "rag.jsonl"))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => JsonSerializer.Deserialize<CasoRag>(l, opciones)!)
            .ToList();

        int aciertos = 0;
        foreach (var caso in casos)
        {
            var r = await asistente.PreguntarAsync(caso.Pregunta, ct);
            bool ok = r.Sabe == caso.Sabe
                && (!caso.Sabe || r.Citadas.Any(f => f.Fuente.EndsWith(caso.Fuente!, StringComparison.Ordinal)));
            if (ok) aciertos++;
            else salida.WriteLine($"FALLA {caso.Id}: sabe={r.Sabe}, citó {string.Join(",", r.Citadas.Select(f => f.Fuente))}, motivo {r.Motivo}");
        }
        double tasa = (double)aciertos / casos.Count;
        salida.WriteLine($"{prompt.Id} ({prompt.Huella}): {aciertos}/{casos.Count} = {tasa:P0}");
        Assert.True(tasa >= Umbral, $"La tasa bajó a {tasa:P0}; el umbral es {Umbral:P0}.");
    }
}
