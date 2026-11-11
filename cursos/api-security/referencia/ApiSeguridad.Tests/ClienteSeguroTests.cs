using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ApiSeguridad.Tests;

[Trait("Categoria", "Seguridad")]
public class ClienteSeguroTests
{
    [Fact]
    public async Task El_cliente_no_conecta_a_una_direccion_interna()
    {
        using var oyente = new TcpListener(IPAddress.Loopback, 0);
        oyente.Start();
        var puerto = ((IPEndPoint)oyente.LocalEndpoint).Port;
        using var http = ClienteSeguro.Crear();
        await Assert.ThrowsAsync<HttpRequestException>(() => http.GetAsync($"http://localhost:{puerto}/"));
        Assert.False(oyente.Pending()); // nadie llego a conectarse
    }

    private static async Task<(TcpListener, Uri)> Servidor(string cabeceras, string cuerpo)
    {
        var oyente = new TcpListener(IPAddress.Loopback, 0);
        oyente.Start();
        _ = Task.Run(async () =>
        {
            using var cliente = await oyente.AcceptTcpClientAsync();
            var flujo = cliente.GetStream();
            var buf = new byte[2048];
            _ = await flujo.ReadAsync(buf);
            var resp = $"HTTP/1.1 200 OK\r\n{cabeceras}Content-Length: {Encoding.UTF8.GetByteCount(cuerpo)}\r\nConnection: close\r\n\r\n{cuerpo}";
            await flujo.WriteAsync(Encoding.UTF8.GetBytes(resp));
        });
        return (oyente, new Uri($"http://127.0.0.1:{((IPEndPoint)oyente.LocalEndpoint).Port}/"));
    }

    [Theory]
    [InlineData("Content-Type: application/json\r\n", """{"moneda":"MXN","valor":1.5}""", true)]
    [InlineData("Content-Type: text/html\r\n", """{"moneda":"MXN","valor":1.5}""", false)]
    [InlineData("Content-Type: application/json\r\n", """{"moneda":"MXN","valor":-3}""", false)]
    [InlineData("Content-Type: application/json\r\n", """{"moneda":"MXNN","valor":1}""", false)]
    [InlineData("Content-Type: application/json\r\n", "no es json", false)]
    public async Task Lo_que_devuelve_un_tercero_se_valida(string cabeceras, string cuerpo, bool valido)
    {
        var (oyente, url) = await Servidor(cabeceras, cuerpo);
        using var _ = oyente.Server;
        using var http = new HttpClient();
        var c = await LectorDeTercero.LeerAsync(http, url, CancellationToken.None);
        Assert.Equal(valido, c is not null);
    }
}
