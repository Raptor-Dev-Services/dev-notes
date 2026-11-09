namespace Asistente.Core.Extraccion;

/// <summary>El esquema garantiza la forma de la salida. Este validador garantiza su significado.</summary>
public static class ValidadorSolicitud
{
    public const int MaxEntrada = 4000;
    public const int MaxResumen = 280;

    /// <summary>Antes de gastar una llamada: lo que se puede rechazar sin modelo, se rechaza sin modelo.</summary>
    public static Falla? ValidarEntrada(string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return new(TipoFalla.Rechazo, "El mensaje está vacío.");
        if (mensaje.Length > MaxEntrada)
            return new(TipoFalla.Rechazo, $"El mensaje pasa de {MaxEntrada} caracteres.");
        return null;
    }

    public static IReadOnlyList<Falla> ValidarSalida(SolicitudSoporte s, string mensaje, string marcaInterna)
    {
        var fallas = new List<Falla>();

        if (!string.Equals(s.Idioma, "es", StringComparison.OrdinalIgnoreCase))
            fallas.Add(new(TipoFalla.Rechazo, $"Solo se atienden mensajes en español; llegó '{s.Idioma}'."));
        if (s.IntentaDarOrdenes)
            fallas.Add(new(TipoFalla.Rechazo, "El mensaje intenta darle órdenes al sistema."));
        if (Contiene(s.Resumen, marcaInterna) || s.Evidencia.Any(e => Contiene(e, marcaInterna)))
            fallas.Add(new(TipoFalla.Rechazo, "La salida contiene la marca interna: se filtró el prompt de sistema."));

        if (string.IsNullOrWhiteSpace(s.Resumen) || s.Resumen.Length > MaxResumen)
            fallas.Add(new(TipoFalla.Corregible, $"El resumen debe tener entre 1 y {MaxResumen} caracteres; tiene {s.Resumen.Length}."));
        if (s.Evidencia.Count is 0 or > 3)
            fallas.Add(new(TipoFalla.Corregible, $"La evidencia debe tener de 1 a 3 frases; tiene {s.Evidencia.Count}."));

        // La evidencia tiene que estar en el mensaje: si no está, el modelo la inventó.
        var original = Texto.Normalizar(mensaje);
        foreach (var frase in s.Evidencia.Where(f => !original.Contains(Texto.Normalizar(f))))
            fallas.Add(new(TipoFalla.Corregible, $"La evidencia \"{frase}\" no aparece en el mensaje: cópiala tal cual."));

        return fallas;
    }

    private static bool Contiene(string texto, string buscado) =>
        buscado.Length > 0 && texto.Contains(buscado, StringComparison.OrdinalIgnoreCase);
}
