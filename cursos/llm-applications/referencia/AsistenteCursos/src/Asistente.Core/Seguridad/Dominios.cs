namespace Asistente.Core.Seguridad;

public static class Dominios
{
    /// <summary>true si el host es uno de los permitidos o un subdominio suyo.</summary>
    public static bool Permitido(string host, IReadOnlyCollection<string> permitidos) =>
        permitidos.Any(p => host.Equals(p, StringComparison.OrdinalIgnoreCase)
                         || host.EndsWith("." + p, StringComparison.OrdinalIgnoreCase));
}
