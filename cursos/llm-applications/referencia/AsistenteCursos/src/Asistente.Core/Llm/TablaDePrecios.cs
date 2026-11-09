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
