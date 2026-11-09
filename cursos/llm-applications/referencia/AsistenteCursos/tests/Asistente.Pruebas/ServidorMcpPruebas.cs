using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using ModelContextProtocol.Client;

namespace Asistente.Pruebas;

/// <summary>El servidor MCP en memoria: sin puertos y sin red.</summary>
public class ServidorMcpPruebas
{
    private const string Token = "token-de-prueba-que-solo-existe-en-esta-prueba";

    private static WebApplicationFactory<Program> Servidor() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.UseSetting("MCP_TOKEN_SHA256", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Token)))));

    private static StringContent Inicializar() => new("""
        {"jsonrpc":"2.0","id":1,"method":"initialize",
         "params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"prueba","version":"1.0"}}}
        """, Encoding.UTF8, "application/json");

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer token-equivocado")]
    [InlineData("Basic no-es-un-token")]
    public async Task Sin_credencial_valida_responde_401(string? autorizacion)
    {
        await using var servidor = Servidor();
        var http = servidor.CreateClient();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = Inicializar() };
        peticion.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (autorizacion is not null) peticion.Headers.TryAddWithoutValidation("Authorization", autorizacion);

        using var respuesta = await http.SendAsync(peticion, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_token_un_cliente_MCP_lista_y_llama_herramientas()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var servidor = Servidor();
        var http = servidor.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        var transporte = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp") }, http, ownsHttpClient: false);

        await using var cliente = await McpClient.CreateAsync(transporte, cancellationToken: ct);
        var herramientas = await cliente.ListToolsAsync(cancellationToken: ct);
        var inexistente = await cliente.CallToolAsync("obtener_curso",
            new Dictionary<string, object?> { ["id"] = "no-existe" }, cancellationToken: ct);

        Assert.Contains(herramientas, h => h.Name == "listar_cursos");
        Assert.True(inexistente.IsError);
    }
}
