using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Asistente.Mcp;

/// <summary>Un recurso lo elige la aplicación cliente para dárselo al modelo; una herramienta la elige el modelo.</summary>
[McpServerResourceType]
public sealed class RecursosCatalogo(Catalogo catalogo)
{
    [McpServerResource(UriTemplate = "catalogo://cursos/{id}", Name = "curso", MimeType = "application/json")]
    [Description("Un curso del catálogo, completo, en JSON.")]
    public string Curso(string id) =>
        JsonSerializer.Serialize(catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'."));
}
