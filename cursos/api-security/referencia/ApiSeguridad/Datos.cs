using System.Security.Cryptography;

namespace ApiSeguridad;

public sealed class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "cliente";
    public string HashContrasena { get; set; } = "";
    public string NotasInternas { get; set; } = "";
}

public sealed class Pedido
{
    public int Id { get; set; }
    public int DuenoId { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "nuevo";
    public bool CuponAplicado { get; set; }
}

public static class Contrasenas
{
    // Valor del cheat sheet "Password Storage" de OWASP para PBKDF2-HMAC-SHA256 (consultado en noviembre de 2026).
    public const int Iteraciones = 600_000;

    public static string Crear(string clave)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string clave, string almacenado)
    {
        var partes = almacenado.Split('.');
        if (partes.Length != 2) return false;
        var sal = Convert.FromBase64String(partes[0]);
        var esperado = Convert.FromBase64String(partes[1]);
        var hash = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(hash, esperado);
    }
}

/// <summary>Datos de ejemplo en memoria. Todos ficticios.</summary>
public sealed class Almacen
{
    public object Candado { get; } = new();
    public List<Usuario> Usuarios { get; }
    public List<Pedido> Pedidos { get; }

    public Almacen()
    {
        // La clave de los tres usuarios de ejemplo es la misma y es ficticia.
        var hash = Contrasenas.Crear("clave-de-ejemplo");
        Usuarios =
        [
            new() { Id = 1, Nombre = "Ana", Rol = "cliente", HashContrasena = hash, NotasInternas = "cliente frecuente" },
            new() { Id = 2, Nombre = "Beto", Rol = "cliente", HashContrasena = hash, NotasInternas = "pidio factura" },
            new() { Id = 3, Nombre = "Admin", Rol = "admin", HashContrasena = hash, NotasInternas = "cuenta de soporte" },
        ];
        Pedidos =
        [
            new() { Id = 100, DuenoId = 1, Total = 500m },
            new() { Id = 101, DuenoId = 1, Total = 80m },
            new() { Id = 200, DuenoId = 2, Total = 1200m },
        ];
    }
}
