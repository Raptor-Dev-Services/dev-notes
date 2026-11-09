using System.Text.Json;
using Anthropic;
using Asistente.Core;
using Asistente.Core.Extraccion;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;

namespace Asistente.Pruebas;

public sealed record CasoExtraccion(string Id, string Mensaje, bool Aceptada, string? Categoria, string? Urgencia);

[Trait("Categoria", "Eval")]
public class EvalExtraccion(ITestOutputHelper salida)
{
    private const double Umbral = 0.9;

    [Fact]
    public async Task Casos_de_extraccion_pasan_el_umbral()
    {
        Requiere.LlaveDeApi();
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var casos = File.ReadLines(Path.Combine(Rutas.Buscar("casos"), "extraccion-soporte.jsonl"))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => JsonSerializer.Deserialize<CasoExtraccion>(l, opciones)!)
            .ToList();
        var prompt = RepositorioPrompts.DelRepositorio().Cargar("extraccion-soporte", 1);
        var extractor = new ExtractorSoporte(new AnthropicClient(), prompt, Modelos.PorOmision);

        int aciertos = 0;
        decimal costo = 0m;
        foreach (var caso in casos)
        {
            var r = await extractor.ExtraerAsync(caso.Mensaje, TestContext.Current.CancellationToken);
            costo += r.CostoUsd;
            bool ok = r.Aceptada == caso.Aceptada
                && (!caso.Aceptada
                    || (string.Equals(r.Solicitud!.Categoria.ToString(), caso.Categoria, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(r.Solicitud.Urgencia.ToString(), caso.Urgencia, StringComparison.OrdinalIgnoreCase)));
            if (ok) aciertos++;
            else salida.WriteLine($"FALLA {caso.Id}: aceptada={r.Aceptada} {string.Join("; ", r.Fallas.Select(f => f.Mensaje))}");
        }

        double tasa = (double)aciertos / casos.Count;
        salida.WriteLine($"{prompt.Id} ({prompt.Huella}): {aciertos}/{casos.Count} = {tasa:P0}, costo {costo:0.0000} USD");
        Assert.True(tasa >= Umbral, $"La tasa bajó a {tasa:P0}; el umbral es {Umbral:P0}.");
    }
}
