using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace ApiSeguridad;

/// <summary>
/// Cliente HTTP para visitar destinos que eligio otro: sin redirecciones, con tiempos, y que se niega a
/// conectar si el nombre se resuelve a una direccion interna. La verificacion y la conexion usan la MISMA
/// direccion resuelta (no se vuelve a resolver), y el nombre original sigue siendo el del Host y el de SNI.
/// </summary>
public static class ClienteSeguro
{
    public static HttpClient Crear()
    {
        var manejador = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(3),
            ConnectCallback = async (contexto, ct) =>
            {
                var direcciones = await Dns.GetHostAddressesAsync(contexto.DnsEndPoint.Host, ct);
                if (direcciones.Length == 0 || direcciones.Any(Destinos.EsDireccionInterna))
                    throw new HttpRequestException("Destino no permitido: se resuelve a una direccion interna.");
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(direcciones[0], contexto.DnsEndPoint.Port), ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
        return new HttpClient(manejador, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(5),
            MaxResponseContentBufferSize = 100_000,
        };
    }
}

public sealed record Cotizacion(string Moneda, decimal Valor);

public static class LectorDeTercero
{
    /// <summary>Lo que devuelve un tercero es entrada no confiable: tipo, tamano y forma se comprueban antes de usarlo.</summary>
    public static async Task<Cotizacion?> LeerAsync(HttpClient http, Uri url, CancellationToken ct)
    {
        using var r = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!r.IsSuccessStatusCode) return null;
        if (r.Content.Headers.ContentType?.MediaType != "application/json") return null;
        if (r.Content.Headers.ContentLength is > 10_000) return null;
        var texto = await r.Content.ReadAsStringAsync(ct);
        Cotizacion? c;
        try { c = JsonSerializer.Deserialize<Cotizacion>(texto, JsonSerializerOptions.Web); }
        catch (JsonException) { return null; }
        return c is { Moneda.Length: 3, Valor: > 0 and <= 1_000_000 } ? c : null;
    }
}
