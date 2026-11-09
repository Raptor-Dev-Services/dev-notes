using Anthropic;
using Asistente.Core.Agente;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;
using Asistente.Core.Rag;

namespace Asistente.Cli.Comandos;

public static class Agente
{
    public static async Task<int> EjecutarAsync(AnthropicClient cliente, string[] args, CancellationToken ct)
    {
        if (args.Length == 0) { Console.Error.WriteLine("Uso: agente <petición>"); return 1; }
        await using var fuente = AlmacenVectorial.CrearFuente(Servicios.CadenaDeConexion());
        var tickets = new RepositorioTickets(fuente);
        await tickets.CrearEsquemaAsync(ct);

        Directory.CreateDirectory("bitacoras");
        var bitacora = new Bitacora(Path.Combine("bitacoras", $"agente-{DateTime.UtcNow:yyyyMMdd-HHmmss}.jsonl"));
        var prompt = RepositorioPrompts.DelRepositorio().Cargar("agente-soporte", 1);
        var agente = new BucleAgente(
            new ModeloAnthropic(cliente),
            Modelos.PorOmision,
            prompt.Texto,
            [new HerramientaBuscarTickets(tickets), new HerramientaCrearTicket(tickets)],
            new ConfirmadorDeConsola(),
            bitacora,
            new LimitesAgente(MaxPasos: 8, TopeUsd: 0.05m));

        var r = await agente.EjecutarAsync(string.Join(' ', args), ct);
        Console.WriteLine(r.Terminado ? $"\n{r.Texto}" : $"\nDetenido: {r.Motivo}");
        Console.WriteLine($"Pasos: {r.Pasos} | costo: {r.CostoUsd:0.000000} USD | bitácora: {bitacora.Eventos.Count} eventos");
        return r.Terminado ? 0 : 5;
    }
}
