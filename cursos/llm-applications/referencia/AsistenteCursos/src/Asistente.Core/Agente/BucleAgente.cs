using System.Text.Json;
using Anthropic.Models.Messages;
using Asistente.Core.Llm;

namespace Asistente.Core.Agente;

/// <summary>Los límites no son una sugerencia al modelo: el bucle los hace cumplir.</summary>
public sealed record LimitesAgente(int MaxPasos = 8, decimal TopeUsd = 0.05m);

public sealed record ResultadoAgente(bool Terminado, string Texto, int Pasos, decimal CostoUsd, string? Motivo);

public sealed class BucleAgente(
    IModelo modelo,
    string nombreModelo,
    string sistema,
    IReadOnlyList<IHerramienta> herramientas,
    IConfirmador confirmador,
    Bitacora bitacora,
    LimitesAgente limites)
{
    public async Task<ResultadoAgente> EjecutarAsync(string peticion, CancellationToken ct = default)
    {
        List<MessageParam> mensajes = [new UserMessageParam { Content = peticion }];
        List<ToolUnion> definiciones = [.. herramientas.Select(h => (ToolUnion)h.Definicion)];
        decimal costo = 0m;
        bitacora.Registrar(0, "peticion", peticion, 0m);

        for (int paso = 1; paso <= limites.MaxPasos; paso++)
        {
            var respuesta = await modelo.EnviarAsync(new MessageCreateParams
            {
                Model = nombreModelo,
                MaxTokens = 8000,
                System = sistema,
                OutputConfig = new OutputConfig { Effort = Effort.Medium },
                Tools = definiciones,
                Messages = mensajes,
            }, ct);
            costo += TablaDePrecios.Costo(nombreModelo, respuesta.Usage);

            if (respuesta.StopReason == StopReason.Refusal)
                return Detener(paso, costo, "El modelo se negó a continuar.");
            if (costo > limites.TopeUsd)
                return Detener(paso, costo, $"Se alcanzó el tope de gasto de {limites.TopeUsd} USD.");

            // Se devuelve el contenido completo, pensamiento incluido y sin tocar la firma.
            var eco = new List<ContentBlockParam>();
            var usos = new List<ToolUseBlock>();
            foreach (var bloque in respuesta.Content)
            {
                if (bloque.TryPickText(out var texto))
                {
                    eco.Add(new TextBlockParam { Text = texto.Text });
                    bitacora.Registrar(paso, "texto", texto.Text, costo);
                }
                else if (bloque.TryPickThinking(out var pensamiento))
                    eco.Add(new ThinkingBlockParam { Thinking = pensamiento.Thinking, Signature = pensamiento.Signature });
                else if (bloque.TryPickRedactedThinking(out var oculto))
                    eco.Add(new RedactedThinkingBlockParam { Data = oculto.Data });
                else if (bloque.TryPickToolUse(out var uso))
                {
                    eco.Add(new ToolUseBlockParam { ID = uso.ID, Name = uso.Name, Input = uso.Input });
                    usos.Add(uso);
                }
            }
            mensajes.Add(new AssistantMessageParam { Content = eco });

            if (respuesta.StopReason != StopReason.ToolUse)
            {
                var final = string.Concat(respuesta.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
                bitacora.Registrar(paso, "fin", $"stop_reason={respuesta.StopReason}", costo);
                return new ResultadoAgente(true, final, paso, costo, null);
            }

            // Todos los resultados de este turno van juntos, en un solo mensaje de usuario.
            var resultados = new List<ContentBlockParam>();
            foreach (var uso in usos)
                resultados.Add(await EjecutarHerramientaAsync(paso, uso, costo, ct));
            mensajes.Add(new UserMessageParam { Content = resultados });
        }
        return Detener(limites.MaxPasos, costo, $"Se alcanzó el límite de {limites.MaxPasos} pasos.");
    }

    private async Task<ToolResultBlockParam> EjecutarHerramientaAsync(int paso, ToolUseBlock uso, decimal costo, CancellationToken ct)
    {
        string entrada = JsonSerializer.Serialize(uso.Input);
        bitacora.Registrar(paso, "herramienta_pedida", $"{uso.Name} {entrada}", costo);

        var herramienta = herramientas.FirstOrDefault(h => h.Nombre == uso.Name);
        if (herramienta is null)
            return Resultado(paso, uso, $"No existe la herramienta {uso.Name}.", error: true, costo);

        if (herramienta.Escribe)
        {
            var propuesta = new PropuestaDeAccion(herramienta.Nombre, herramienta.Describir(uso.Input));
            bool autorizada = await confirmador.ConfirmarAsync(propuesta, ct);
            bitacora.Registrar(paso, "confirmacion", autorizada ? "autorizada" : "rechazada", costo);
            if (!autorizada)
                return Resultado(paso, uso,
                    "La persona rechazó la acción y no se ejecutó. No la repitas; pregúntale qué quiere cambiar.",
                    error: true, costo);
        }

        try
        {
            return Resultado(paso, uso, await herramienta.EjecutarAsync(uso.Input, ct), error: false, costo);
        }
        catch (ArgumentException e)
        {
            // Una regla de negocio incumplida vuelve al modelo para que corrija la entrada.
            return Resultado(paso, uso, e.Message, error: true, costo);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Otro error se registra completo, pero al modelo solo le llega un aviso genérico.
            bitacora.Registrar(paso, "error_interno", e.ToString(), costo);
            return Resultado(paso, uso, "La herramienta falló por un error interno.", error: true, costo);
        }
    }

    private ToolResultBlockParam Resultado(int paso, ToolUseBlock uso, string contenido, bool error, decimal costo)
    {
        bitacora.Registrar(paso, error ? "herramienta_error" : "herramienta_resultado", $"{uso.Name}: {contenido}", costo);
        return new ToolResultBlockParam { ToolUseID = uso.ID, Content = contenido, IsError = error };
    }

    private ResultadoAgente Detener(int paso, decimal costo, string motivo)
    {
        bitacora.Registrar(paso, "detenido", motivo, costo);
        return new ResultadoAgente(false, "", paso, costo, motivo);
    }
}
