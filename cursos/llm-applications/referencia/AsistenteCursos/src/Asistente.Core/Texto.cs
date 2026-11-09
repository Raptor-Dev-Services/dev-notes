using System.Globalization;
using System.Text;

namespace Asistente.Core;

public static class Texto
{
    /// <summary>Minúsculas, sin acentos y con los espacios colapsados, para comparar sin falsos negativos.</summary>
    public static string Normalizar(string texto)
    {
        var sb = new StringBuilder(texto.Length);
        bool espacio = false;
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(c)) { espacio = true; continue; }
            if (espacio && sb.Length > 0) sb.Append(' ');
            espacio = false;
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
