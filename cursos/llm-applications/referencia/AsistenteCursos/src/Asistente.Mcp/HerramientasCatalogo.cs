using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Asistente.Mcp;

/// <summary>Herramientas de solo lectura. ReadOnly e Idempotent son pistas para el cliente, no un permiso.</summary>
[McpServerToolType]
public sealed class HerramientasCatalogo(Catalogo catalogo)
{
    [McpServerTool(Name = "listar_cursos", ReadOnly = true, Idempotent = true)]
    [Description("Lista los cursos del catálogo con su id, título, estado y número de unidades. Úsala para saber qué cursos existen.")]
    public IReadOnlyList<ResumenCurso> ListarCursos() =>
        catalogo.Cursos.Select(c => new ResumenCurso(c.Id, c.Titulo, c.Estado, c.Unidades.Count)).ToList();

    [McpServerTool(Name = "obtener_curso", ReadOnly = true, Idempotent = true)]
    [Description("Devuelve un curso con todas sus unidades. Úsala cuando ya conoces el id del curso.")]
    public Curso ObtenerCurso([Description("Id del curso, por ejemplo llm-applications.")] string id) =>
        catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'. Usa listar_cursos para ver los ids.");

    [McpServerTool(Name = "buscar_unidades", ReadOnly = true, Idempotent = true)]
    [Description("Busca, en todos los cursos, las unidades cuyo título contiene el texto dado.")]
    public IReadOnlyList<UnidadEncontrada> BuscarUnidades([Description("Texto a buscar, por ejemplo RAG.")] string texto) =>
        catalogo.Cursos
            .SelectMany(c => c.Unidades.Select(u => new UnidadEncontrada(c.Id, u.Id, u.Titulo, u.Minutos)))
            .Where(u => u.Titulo.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();
}

public sealed record ResumenCurso(string Id, string Titulo, string Estado, int Unidades);

public sealed record UnidadEncontrada(string Curso, string Unidad, string Titulo, int Minutos);
