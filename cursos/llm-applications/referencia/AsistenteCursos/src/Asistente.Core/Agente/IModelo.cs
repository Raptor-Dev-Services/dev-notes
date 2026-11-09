using Anthropic;
using Anthropic.Models.Messages;

namespace Asistente.Core.Agente;

/// <summary>La costura que permite probar el bucle con respuestas guionadas, sin red ni costo.</summary>
public interface IModelo
{
    Task<Message> EnviarAsync(MessageCreateParams parametros, CancellationToken ct);
}

public sealed class ModeloAnthropic(AnthropicClient cliente) : IModelo
{
    public Task<Message> EnviarAsync(MessageCreateParams parametros, CancellationToken ct) =>
        cliente.Messages.Create(parametros, ct);
}
