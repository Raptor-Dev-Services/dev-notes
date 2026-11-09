using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Asistente.Core.Seguridad;

/// <summary>
/// Revisa un documento antes de indexarlo. Lo que parece una instrucción para el modelo o contenido
/// oculto manda el documento a cuarentena: lo revisa una persona antes de que entre al índice.
/// Tiene falsos positivos a propósito: una nota que explica prompt injection con ejemplos también cae.
/// Cada patrón nuevo se mide contra los documentos reales: si casi todo cae, la gente aprueba sin leer.
/// </summary>
public static partial class FiltroDeIngesta
{
    public static IReadOnlyList<string> Revisar(string documento)
    {
        var hallazgos = new List<string>();
        foreach (Match m in ComentarioHtml().Matches(documento))
            hallazgos.Add($"Contenido oculto en un comentario HTML: \"{Recortar(m.Value)}\"");
        if (InvisibleUnicode().IsMatch(documento))
            hallazgos.Add("Caracteres invisibles de ancho cero.");
        foreach (var patron in Instrucciones())
            foreach (Match m in patron.Matches(documento))
                hallazgos.Add($"Parece una instrucción para el modelo: \"{Recortar(m.Value)}\"");
        return hallazgos;
    }

    /// <summary>La aprobación se ata al contenido: si el documento cambia, cambia la huella y se revisa otra vez.</summary>
    public static string Huella(string documento) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(documento))).ToLowerInvariant();

    private static Regex[] Instrucciones() =>
    [
        Ignora(), Ignore(), NotaParaElAsistente(), APartirDeAhora(),
    ];

    private static string Recortar(string s) => s.Length <= 80 ? s : s[..80] + "...";

    [GeneratedRegex(@"<!--[\s\S]*?-->")]
    private static partial Regex ComentarioHtml();

    [GeneratedRegex(@"[\u200B-\u200F\u2060\uFEFF]")]
    private static partial Regex InvisibleUnicode();

    [GeneratedRegex(@"\b(ignora|olvida|omite)\b[^.\n]{0,40}\b(instrucciones|reglas|anterior)", RegexOptions.IgnoreCase)]
    private static partial Regex Ignora();

    [GeneratedRegex(@"\b(ignore|disregard)\b[^.\n]{0,40}\b(instructions|previous|above)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Ignore();

    [GeneratedRegex(@"\b(nota|mensaje|instrucci[oó]n(es)?)\s+para\s+(el|la)\s+(asistente|modelo|ia|agente)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NotaParaElAsistente();

    // "A partir de ahora" solo cuenta si le sigue una orden: "a partir de ahora los plazos son..." es un dato.
    [GeneratedRegex(@"\ba partir de ahora\b[^.\n]{0,30}\b(responde|di|actúa|eres|debes|ignora|agrega)\b", RegexOptions.IgnoreCase)]
    private static partial Regex APartirDeAhora();
}
