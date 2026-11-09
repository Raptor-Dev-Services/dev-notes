using Asistente.Core.Agente;

namespace Asistente.Cli;

/// <summary>Solo "si" confirma. Enter, "s", "ok" o cualquier otra cosa es un no.</summary>
public sealed class ConfirmadorDeConsola : IConfirmador
{
    public Task<bool> ConfirmarAsync(PropuestaDeAccion propuesta, CancellationToken ct)
    {
        Console.WriteLine($"\nEl agente quiere ejecutar {propuesta.Herramienta}:\n{propuesta.Descripcion}");
        Console.Write("¿Lo autorizas? Escribe si para confirmar: ");
        var respuesta = Console.ReadLine()?.Trim().ToLowerInvariant();
        return Task.FromResult(respuesta is "si" or "sí");
    }
}
