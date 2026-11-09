using System.Text;

namespace Asistente.Core.Rag;

public sealed record FragmentoNuevo(string Fuente, string Seccion, string Contenido);

/// <summary>Parte un Markdown por secciones y, si una sección es larga, por párrafos con solape.</summary>
public static class Fragmentador
{
    public static IReadOnlyList<FragmentoNuevo> Partir(string fuente, string markdown, int maxCaracteres = 1500, int solape = 200)
    {
        var resultado = new List<FragmentoNuevo>();
        foreach (var (seccion, cuerpo) in Secciones(markdown))
        {
            var actual = new StringBuilder();
            foreach (var pedazo in Parrafos(cuerpo).SelectMany(p => Trocear(p, maxCaracteres)))
            {
                if (actual.Length > 0 && actual.Length + pedazo.Length + 2 > maxCaracteres)
                {
                    resultado.Add(Nuevo(fuente, seccion, actual.ToString()));
                    string cola = Cola(actual.ToString(), solape);
                    actual.Clear().Append(cola);
                }
                if (actual.Length > 0) actual.Append("\n\n");
                actual.Append(pedazo);
            }
            if (actual.ToString().Trim().Length > 0) resultado.Add(Nuevo(fuente, seccion, actual.ToString()));
        }
        return resultado;
    }

    // Cada fragmento lleva el título de su sección: sin él, "la tabla de abajo" no significa nada.
    private static FragmentoNuevo Nuevo(string fuente, string seccion, string texto) =>
        new(fuente, seccion, $"{seccion}\n\n{texto.Trim()}");

    private static IEnumerable<(string Seccion, string Cuerpo)> Secciones(string markdown)
    {
        string titulo = "Introducción";
        var cuerpo = new StringBuilder();
        bool enCodigo = false;
        foreach (var linea in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (linea.TrimStart().StartsWith("```")) enCodigo = !enCodigo;
            bool esEncabezado = !enCodigo && linea.StartsWith('#');
            if (esEncabezado)
            {
                if (cuerpo.ToString().Trim().Length > 0) yield return (titulo, cuerpo.ToString());
                titulo = linea.TrimStart('#').Trim();
                cuerpo.Clear();
            }
            else cuerpo.AppendLine(linea);
        }
        if (cuerpo.ToString().Trim().Length > 0) yield return (titulo, cuerpo.ToString());
    }

    // Párrafos separados por línea en blanco; un bloque de código cuenta como un solo párrafo.
    private static IEnumerable<string> Parrafos(string cuerpo)
    {
        var actual = new StringBuilder();
        bool enCodigo = false;
        foreach (var linea in cuerpo.Split('\n'))
        {
            if (linea.TrimStart().StartsWith("```")) enCodigo = !enCodigo;
            if (!enCodigo && linea.Trim().Length == 0)
            {
                if (actual.Length > 0) { yield return actual.ToString().TrimEnd(); actual.Clear(); }
                continue;
            }
            actual.AppendLine(linea);
        }
        if (actual.Length > 0) yield return actual.ToString().TrimEnd();
    }

    private static IEnumerable<string> Trocear(string parrafo, int max)
    {
        for (int i = 0; i < parrafo.Length; i += max)
            yield return parrafo.Substring(i, Math.Min(max, parrafo.Length - i));
    }

    // Los últimos caracteres del fragmento anterior, cortados en un espacio para no partir palabras.
    private static string Cola(string texto, int solape)
    {
        if (texto.Length <= solape) return texto;
        var cola = texto[^solape..];
        int espacio = cola.IndexOf(' ');
        return espacio >= 0 ? cola[(espacio + 1)..] : cola;
    }
}
