using System.Text;
using Asistente.Core;
using Asistente.Core.Rag;
using Asistente.Core.Seguridad;

namespace Asistente.Cli.Comandos;

/// <summary>Indexa los .md de una carpeta. Lo sospechoso va a cuarentena hasta que una persona lo apruebe.</summary>
public static class Ingestar
{
    public static async Task<int> EjecutarAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0) { Console.Error.WriteLine("Uso: ingestar <carpeta>"); return 1; }
        var carpeta = Path.GetFullPath(args[0]);
        var embeddings = Servicios.Embeddings();
        await using var fuente = AlmacenVectorial.CrearFuente(Servicios.CadenaDeConexion());
        var almacen = new AlmacenVectorial(fuente);
        await almacen.CrearEsquemaAsync(embeddings.Dimensiones, ct);
        var aprobadas = Aprobadas();
        var cuarentena = new StringBuilder();

        int total = 0;
        foreach (var archivo in Directory.EnumerateFiles(carpeta, "*.md", SearchOption.AllDirectories).Order())
        {
            var relativa = Path.GetRelativePath(carpeta, archivo).Replace('\\', '/');
            var texto = await File.ReadAllTextAsync(archivo, ct);
            var hallazgos = FiltroDeIngesta.Revisar(texto);
            if (hallazgos.Count > 0 && !aprobadas.Contains(FiltroDeIngesta.Huella(texto)))
            {
                Console.WriteLine($"{relativa}: EN CUARENTENA ({hallazgos.Count} hallazgos)");
                cuarentena.AppendLine($"## {relativa}\n\nHuella: `{FiltroDeIngesta.Huella(texto)}`\n");
                foreach (var h in hallazgos) cuarentena.AppendLine($"- {h}");
                cuarentena.AppendLine();
                // Si ya estaba indexado, sale del índice: en cuarentena no responde preguntas.
                await almacen.ReemplazarFuenteAsync(relativa, [], [], ct);
                continue;
            }

            var fragmentos = Fragmentador.Partir(relativa, texto);
            if (fragmentos.Count == 0) continue;
            var vectores = new List<float[]>();
            foreach (var lote in fragmentos.Chunk(16))
                vectores.AddRange(await embeddings.GenerarAsync(lote.Select(f => f.Contenido).ToList(), TipoTexto.Documento, ct));
            await almacen.ReemplazarFuenteAsync(relativa, fragmentos, vectores, ct);
            total += fragmentos.Count;
            Console.WriteLine($"{relativa}: {fragmentos.Count} fragmentos");
        }

        if (cuarentena.Length > 0)
        {
            await File.WriteAllTextAsync("cuarentena.md", "# Documentos en cuarentena\n\n" + cuarentena, ct);
            Console.WriteLine("Revisa cuarentena.md. Para aprobar un documento, agrega su huella a seguridad/ingesta-aprobada.txt.");
        }
        Console.WriteLine($"Listo: {total} fragmentos con {embeddings.Modelo} ({embeddings.Dimensiones} dimensiones).");
        return 0;
    }

    private static HashSet<string> Aprobadas() =>
        File.ReadLines(Path.Combine(Rutas.Buscar("seguridad"), "ingesta-aprobada.txt"))
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l => l.Split(' ', 2)[0].ToLowerInvariant())
            .ToHashSet();
}
