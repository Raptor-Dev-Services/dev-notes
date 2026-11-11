using System.Net;

namespace ApiSeguridad;

/// <summary>
/// Decide si la API puede visitar una URL que le dio un cliente (SSRF, API7:2023).
/// Caso 1 del cheat sheet de OWASP: la API solo habla con destinos conocidos, asi que se usa una lista de permitidos.
/// </summary>
public static class Destinos
{
    public static bool EsPermitido(string? texto, IReadOnlyCollection<string> hostsPermitidos)
    {
        if (!Uri.TryCreate(texto, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;      // solo https
        if (!uri.IsDefaultPort) return false;                    // y solo el puerto 443
        if (!string.IsNullOrEmpty(uri.UserInfo)) return false;   // nada de usuario@host
        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out _)) return false; // por nombre, no por direccion
        return hostsPermitidos.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Direcciones que una API nunca debe visitar por orden de un cliente. Es el control complementario (capa de red
    /// y verificacion de la direccion resuelta): sirve para comprobar el resultado de la resolucion DNS antes de conectar.
    /// </summary>
    public static bool EsDireccionInterna(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) return true;
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal;
        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] is >= 16 and <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254)   // enlace local, donde viven los servicios de metadatos de la nube
            || b[0] == 127
            || b[0] == 0;
    }
}
