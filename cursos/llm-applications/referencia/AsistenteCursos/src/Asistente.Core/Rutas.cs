namespace Asistente.Core;

public static class Rutas
{
    /// <summary>Busca hacia arriba, desde donde corre el programa, la carpeta indicada (prompts, casos...).</summary>
    public static string Buscar(string carpeta)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidata = Path.Combine(dir.FullName, carpeta);
            if (Directory.Exists(candidata)) return candidata;
        }
        throw new DirectoryNotFoundException($"No encontré la carpeta '{carpeta}' en la raíz del repositorio.");
    }
}
