using System.Text.Json;

namespace Asistente.Mcp;

public sealed record Unidad(string Id, int Numero, string Titulo, int Minutos);

public sealed record Curso(string Id, string Titulo, string Subtitulo, string Estado, IReadOnlyList<Unidad> Unidades);

/// <summary>El catálogo de cursos, en modo lectura. Se carga una vez al arrancar.</summary>
public sealed class Catalogo(IReadOnlyList<Curso> cursos)
{
    public IReadOnlyList<Curso> Cursos => cursos;

    public Curso? Buscar(string id) => cursos.FirstOrDefault(c => c.Id == id);

    public static Catalogo Cargar(string ruta)
    {
        var archivo = JsonSerializer.Deserialize<Archivo>(File.ReadAllText(ruta), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"El catálogo {ruta} está vacío.");
        return new Catalogo(archivo.Cursos);
    }

    private sealed record Archivo(IReadOnlyList<Curso> Cursos);
}
