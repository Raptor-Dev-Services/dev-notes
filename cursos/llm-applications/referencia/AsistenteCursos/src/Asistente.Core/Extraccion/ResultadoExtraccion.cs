namespace Asistente.Core.Extraccion;

public sealed record ResultadoExtraccion(
    SolicitudSoporte? Solicitud,
    IReadOnlyList<Falla> Fallas,
    int Intentos,
    decimal CostoUsd,
    long TokensCacheLeidos,
    string PromptId)
{
    public bool Aceptada => Solicitud is not null && Fallas.Count == 0;
}
