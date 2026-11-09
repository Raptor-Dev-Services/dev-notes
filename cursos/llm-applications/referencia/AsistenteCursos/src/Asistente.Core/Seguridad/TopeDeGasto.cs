using System.Collections.Concurrent;

namespace Asistente.Core.Seguridad;

/// <summary>
/// Gasto diario por usuario. Se consulta antes de llamar al modelo y se carga con el costo real después.
/// En memoria sirve para una instancia; con varias réplicas, el contador vive en Redis o en la base.
/// </summary>
public sealed class TopeDeGasto(decimal topeDiarioUsd, TimeProvider reloj)
{
    private readonly ConcurrentDictionary<(string Usuario, DateOnly Dia), decimal> gastado = new();

    public bool PuedeGastar(string usuario) => Gastado(usuario) < topeDiarioUsd;

    public void Cargar(string usuario, decimal costoUsd) =>
        gastado.AddOrUpdate((usuario, Hoy()), costoUsd, (_, previo) => previo + costoUsd);

    public decimal Gastado(string usuario) => gastado.GetValueOrDefault((usuario, Hoy()));

    private DateOnly Hoy() => DateOnly.FromDateTime(reloj.GetUtcNow().UtcDateTime);
}
