using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Asistente.Core.Rag;

/// <summary>Embeddings con un modelo local servido por Ollama. Por omisión bge-m3: multilingüe, 1024 dimensiones.</summary>
public sealed class EmbeddingsOllama(HttpClient http, string modelo = "bge-m3", int dimensiones = 1024) : IGeneradorEmbeddings
{
    public string Modelo => modelo;
    public int Dimensiones => dimensiones;

    public async Task<IReadOnlyList<float[]>> GenerarAsync(
        IReadOnlyList<string> textos, TipoTexto tipo, CancellationToken ct = default)
    {
        // bge-m3 no distingue documento de consulta; otros modelos piden un prefijo o un campo distinto.
        using var respuesta = await http.PostAsJsonAsync("/api/embed", new { model = modelo, input = textos }, ct);
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaEmbed>(ct)
            ?? throw new InvalidOperationException("Ollama devolvió un cuerpo vacío.");

        if (cuerpo.Embeddings.Count != textos.Count)
            throw new InvalidOperationException($"Pedí {textos.Count} vectores y llegaron {cuerpo.Embeddings.Count}.");
        if (cuerpo.Embeddings.Any(v => v.Length != dimensiones))
            throw new InvalidOperationException($"El modelo {modelo} no devuelve {dimensiones} dimensiones.");
        return cuerpo.Embeddings;
    }

    private sealed record RespuestaEmbed([property: JsonPropertyName("embeddings")] List<float[]> Embeddings);
}
