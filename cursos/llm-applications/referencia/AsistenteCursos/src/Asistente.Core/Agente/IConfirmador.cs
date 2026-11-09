namespace Asistente.Core.Agente;

/// <summary>Lo que ve la persona antes de decidir: la herramienta y los datos exactos que se van a escribir.</summary>
public sealed record PropuestaDeAccion(string Herramienta, string Descripcion);

public interface IConfirmador
{
    Task<bool> ConfirmarAsync(PropuestaDeAccion propuesta, CancellationToken ct);
}
