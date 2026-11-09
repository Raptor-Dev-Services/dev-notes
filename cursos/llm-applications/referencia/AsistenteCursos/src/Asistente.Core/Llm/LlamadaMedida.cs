using System.Diagnostics;
using System.Text;
using Anthropic;
using Anthropic.Models.Messages;

namespace Asistente.Core.Llm;

public sealed class LlamadaMedida(AnthropicClient cliente)
{
    /// <summary>Cuenta los tokens de entrada antes de gastar: el conteo no se cobra como inferencia.</summary>
    public async Task<long> ContarTokensAsync(string modelo, string sistema, string pregunta, CancellationToken ct = default)
    {
        var conteo = await cliente.Messages.CountTokens(new MessageCountTokensParams
        {
            Model = modelo,
            System = sistema,
            Messages = [new UserMessageParam { Content = pregunta }],
        }, ct);
        return conteo.InputTokens;
    }

    /// <summary>Pregunta en streaming para medir el tiempo al primer token y el total.</summary>
    public async Task<(string Texto, Medicion Medicion)> PreguntarAsync(
        string modelo, string sistema, string pregunta, CancellationToken ct = default)
    {
        var parametros = new MessageCreateParams
        {
            Model = modelo,
            MaxTokens = 4096,
            System = sistema,
            OutputConfig = new OutputConfig { Effort = Effort.Low },
            Messages = [new UserMessageParam { Content = pregunta }],
        };

        var reloj = Stopwatch.StartNew();
        TimeSpan? primerToken = null;
        var texto = new StringBuilder();
        long entrada = 0, salida = 0, cacheLeidos = 0, cacheEscritos = 0;
        string stopReason = "desconocido";

        // El uso llega repartido: la entrada en message_start y la salida acumulada en message_delta.
        await foreach (var evento in cliente.Messages.CreateStreaming(parametros, ct))
        {
            if (evento.TryPickStart(out var inicio))
            {
                entrada = inicio.Message.Usage.InputTokens;
                cacheLeidos = inicio.Message.Usage.CacheReadInputTokens ?? 0;
                cacheEscritos = inicio.Message.Usage.CacheCreationInputTokens ?? 0;
            }
            else if (evento.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var fragmento))
            {
                primerToken ??= reloj.Elapsed;
                texto.Append(fragmento.Text);
            }
            else if (evento.TryPickDelta(out var cierre))
            {
                salida = cierre.Usage.OutputTokens;
                stopReason = cierre.Delta.StopReason?.ToString() ?? stopReason;
            }
        }
        reloj.Stop();

        var medicion = new Medicion(
            modelo, entrada, salida, cacheLeidos, cacheEscritos,
            TablaDePrecios.Costo(modelo, entrada, salida, cacheLeidos, cacheEscritos),
            primerToken ?? reloj.Elapsed, reloj.Elapsed, stopReason);
        return (texto.ToString(), medicion);
    }
}
