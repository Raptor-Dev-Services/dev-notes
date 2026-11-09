using System.Text.Encodings.Web;
using System.Text.Json;

namespace Asistente.Core.Agente;

/// <summary>Un renglón por decisión del agente. Si algo sale mal, aquí se reconstruye qué pasó.</summary>
public sealed record EventoAgente(DateTimeOffset Utc, int Paso, string Tipo, string Detalle, decimal CostoAcumuladoUsd);

public sealed class Bitacora(string? ruta = null)
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // acentos legibles en el archivo
    };
    private readonly List<EventoAgente> eventos = [];

    public IReadOnlyList<EventoAgente> Eventos => eventos;

    public void Registrar(int paso, string tipo, string detalle, decimal costoAcumulado)
    {
        var evento = new EventoAgente(DateTimeOffset.UtcNow, paso, tipo, detalle, costoAcumulado);
        eventos.Add(evento);
        if (ruta is not null)
            File.AppendAllText(ruta, JsonSerializer.Serialize(evento, Opciones) + "\n");
    }
}
