using System.Text.RegularExpressions;

namespace Asistente.Core.Seguridad;

public sealed record SalidaFiltrada(string Texto, IReadOnlyList<string> Retirados);

/// <summary>
/// Defensa determinista sobre lo que sale: quita imágenes Markdown y retira enlaces y correos
/// fuera de la lista de dominios permitidos. No depende de que el modelo se porte bien.
/// </summary>
public sealed partial class FiltroDeSalida(IReadOnlyCollection<string> dominiosPermitidos)
{
    public const string Retirado = "[enlace retirado]";

    public SalidaFiltrada Filtrar(string texto)
    {
        var retirados = new List<string>();

        // Una imagen se descarga sola al pintarse: es el canal clásico para sacar datos.
        texto = Imagen().Replace(texto, m => { retirados.Add(m.Value); return ""; });
        texto = Url().Replace(texto, m =>
        {
            if (Uri.TryCreate(m.Value, UriKind.Absolute, out var uri) && Dominios.Permitido(uri.Host, dominiosPermitidos))
                return m.Value;
            retirados.Add(m.Value);
            return Retirado;
        });
        texto = Correo().Replace(texto, m =>
        {
            if (Dominios.Permitido(m.Groups["dominio"].Value, dominiosPermitidos)) return m.Value;
            retirados.Add(m.Value);
            return Retirado;
        });
        return new SalidaFiltrada(texto.Trim(), retirados);
    }

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex Imagen();

    [GeneratedRegex(@"https?://[^\s<>()\[\]""']+", RegexOptions.IgnoreCase)]
    private static partial Regex Url();

    [GeneratedRegex(@"[\w.+-]+@(?<dominio>[\w-]+(\.[\w-]+)+)")]
    private static partial Regex Correo();
}
