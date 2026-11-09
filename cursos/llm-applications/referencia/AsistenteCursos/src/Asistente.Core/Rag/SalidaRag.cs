using Anthropic.Helpers;

namespace Asistente.Core.Rag;

/// <summary>El contrato de salida del asistente: responder, o decir que no sabe.</summary>
[SchemaClass("Respuesta a una pregunta usando solo los documentos entregados.")]
public sealed class SalidaRag
{
    [SchemaProperty("true solo si los documentos contienen la respuesta.")]
    public bool Sabe { get; set; }

    [SchemaProperty("La respuesta en español. Si sabe es false, una cadena vacía.")]
    public string Respuesta { get; set; } = "";

    [SchemaProperty("Las claves de los documentos usados, por ejemplo F12. Vacía si sabe es false.")]
    public List<string> Fuentes { get; set; } = [];
}

public sealed record RespuestaRag(
    bool Sabe,
    string Texto,
    IReadOnlyList<FragmentoRecuperado> Citadas,
    IReadOnlyList<FragmentoRecuperado> Recuperadas,
    decimal CostoUsd,
    string? Motivo,
    IReadOnlyList<string>? Retirados = null);

/// <summary>K y el umbral de distancia se calibran con el juego de preguntas, no a ojo.</summary>
public sealed record OpcionesRag(
    int K = 5,
    double DistanciaMaxima = 0.55,
    IReadOnlyCollection<string>? DominiosPermitidos = null);
