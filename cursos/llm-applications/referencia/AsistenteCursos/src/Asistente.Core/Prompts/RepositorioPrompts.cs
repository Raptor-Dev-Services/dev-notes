namespace Asistente.Core.Prompts;

/// <summary>Lee los prompts de la carpeta prompts/ del repositorio: nombre.vN.md.</summary>
public sealed class RepositorioPrompts(string carpeta)
{
    public static RepositorioPrompts DelRepositorio() => new(Rutas.Buscar("prompts"));

    public PromptVersionado Cargar(string nombre, int version)
    {
        var ruta = Path.Combine(carpeta, $"{nombre}.v{version}.md");
        if (!File.Exists(ruta))
            throw new FileNotFoundException($"No existe el prompt {nombre} v{version}.", ruta);
        return new PromptVersionado(nombre, version, File.ReadAllText(ruta).Trim());
    }
}
