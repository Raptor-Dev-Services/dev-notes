namespace Asistente.Core.Extraccion;

/// <summary>Rechazo: no se reintenta, la entrada no es válida. Corregible: se reintenta con la falla como pista.</summary>
public enum TipoFalla { Rechazo, Corregible }

public sealed record Falla(TipoFalla Tipo, string Mensaje);
