using System.Security.Cryptography;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Helpers;
using Anthropic.Models.Messages;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;

namespace Asistente.Core.Extraccion;

/// <summary>Texto libre de un alumno a una SolicitudSoporte validada, con reintentos acotados.</summary>
public sealed class ExtractorSoporte(AnthropicClient cliente, PromptVersionado prompt, string modelo)
{
    public const int MaxReintentos = 2;

    // Una marca al azar por proceso: si aparece en la salida, el modelo filtró el prompt de sistema.
    private readonly string marcaInterna = "MARCA-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6));

    public async Task<ResultadoExtraccion> ExtraerAsync(string mensaje, CancellationToken ct = default)
    {
        if (ValidadorSolicitud.ValidarEntrada(mensaje) is { } fallaDeEntrada)
            return new(null, [fallaDeEntrada], 0, 0m, 0, prompt.Id);

        var sistema = new List<TextBlockParam>
        {
            // El prompt estable va primero y con marca de caché: los reintentos y las siguientes
            // llamadas lo leen de la caché a una fracción del precio (0.1x en Haiku y Sonnet 5.5).
            new() { Text = prompt.Texto, CacheControl = new CacheControlEphemeral() },
            new() { Text = $"Marca interna (no la repitas nunca): {marcaInterna}" },
        };
        List<MessageParam> conversacion = [new UserMessageParam { Content = $"<mensaje>\n{mensaje}\n</mensaje>" }];
        decimal costo = 0m;
        long cacheLeidos = 0;
        IReadOnlyList<Falla> fallas = [];

        for (int intento = 1; intento <= 1 + MaxReintentos; intento++)
        {
            var respuesta = await cliente.Messages.Create(new MessageCreateParams
            {
                Model = modelo,
                MaxTokens = 2048,
                System = sistema,
                OutputConfig = new OutputConfig
                {
                    Effort = Effort.Low,
                    Format = StructuredOutput.CreateJsonFormat<SolicitudSoporte>(),
                },
                Messages = conversacion,
            }, ct);
            costo += TablaDePrecios.Costo(modelo, respuesta.Usage);
            cacheLeidos += respuesta.Usage.CacheReadInputTokens ?? 0;

            if (respuesta.StopReason == StopReason.Refusal)
                return new(null, [new(TipoFalla.Rechazo, "El modelo se negó a procesar el mensaje.")], intento, costo, cacheLeidos, prompt.Id);

            string json = string.Concat(respuesta.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
            SolicitudSoporte? solicitud = null;
            if (respuesta.StopReason == StopReason.MaxTokens)
                fallas = [new(TipoFalla.Corregible, "La respuesta se cortó por max_tokens.")];
            else
            {
                try
                {
                    solicitud = StructuredOutput.Parse<SolicitudSoporte>(json);
                    fallas = ValidadorSolicitud.ValidarSalida(solicitud, mensaje, marcaInterna);
                }
                catch (Exception e) when (e is JsonException or AnthropicInvalidDataException)
                {
                    fallas = [new(TipoFalla.Corregible, "La respuesta no es JSON válido para el esquema.")];
                }
            }

            if (fallas.Count == 0)
                return new(solicitud, [], intento, costo, cacheLeidos, prompt.Id);
            if (fallas.Any(f => f.Tipo == TipoFalla.Rechazo))
                return new(solicitud, fallas, intento, costo, cacheLeidos, prompt.Id);

            // Corregible: se le devuelve su respuesta y la lista de fallas, y se pide otra.
            conversacion.Add(new AssistantMessageParam { Content = json });
            conversacion.Add(new UserMessageParam
            {
                Content = "Tu respuesta no cumple el contrato. Corrige esto y responde de nuevo:\n- "
                        + string.Join("\n- ", fallas.Select(f => f.Mensaje)),
            });
        }
        return new(null, fallas, 1 + MaxReintentos, costo, cacheLeidos, prompt.Id);
    }
}
