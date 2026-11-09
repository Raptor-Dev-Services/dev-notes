using Asistente.Core.Rag;

namespace Asistente.Cli;

/// <summary>La configuración sale del entorno. Nada de cadenas de conexión ni llaves en el código.</summary>
public static class Servicios
{
    public static string CadenaDeConexion() =>
        Environment.GetEnvironmentVariable("ASISTENTE_DB")
        ?? throw new InvalidOperationException("Falta la variable ASISTENTE_DB con la cadena de conexión de PostgreSQL.");

    public static IGeneradorEmbeddings Embeddings() =>
        Environment.GetEnvironmentVariable("ASISTENTE_EMBEDDINGS") == "hash"
            ? new EmbeddingsPorHash()
            : new EmbeddingsOllama(new HttpClient
            {
                BaseAddress = new Uri(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434"),
                Timeout = TimeSpan.FromMinutes(2),
            });
}
