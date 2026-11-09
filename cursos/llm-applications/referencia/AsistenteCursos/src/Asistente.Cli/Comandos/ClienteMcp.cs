using System.Net;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Asistente.Cli.Comandos;

/// <summary>Un cliente MCP mínimo: lista las herramientas del servidor y llama una.</summary>
public static class ClienteMcp
{
    public static async Task<int> EjecutarAsync(string[] args, CancellationToken ct)
    {
        var url = Environment.GetEnvironmentVariable("MCP_URL") ?? "http://localhost:5100/mcp";
        var token = args.Contains("--sin-token") ? null : Environment.GetEnvironmentVariable("MCP_TOKEN");

        var opciones = new HttpClientTransportOptions { Endpoint = new Uri(url), Name = "asistente-cli" };
        if (token is not null)
            opciones.AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" };

        try
        {
            await using var cliente = await McpClient.CreateAsync(new HttpClientTransport(opciones), cancellationToken: ct);
            foreach (var h in await cliente.ListToolsAsync(cancellationToken: ct))
                Console.WriteLine($"- {h.Name}: {h.Description}");

            var resultado = await cliente.CallToolAsync("listar_cursos", new Dictionary<string, object?>(), cancellationToken: ct);
            foreach (var bloque in resultado.Content.OfType<TextContentBlock>())
                Console.WriteLine(bloque.Text);
            return resultado.IsError == true ? 7 : 0;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            Console.Error.WriteLine("El servidor rechazó la conexión: 401, sin credencial válida.");
            return 6;
        }
    }
}
