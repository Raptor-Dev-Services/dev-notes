using Asistente.Core.Seguridad;

namespace Asistente.Pruebas;

public class TopeDeGastoPruebas
{
    [Fact]
    public void Corta_al_llegar_al_tope_y_se_reinicia_al_dia_siguiente()
    {
        var reloj = new RelojFijo(new DateTimeOffset(2026, 11, 8, 23, 0, 0, TimeSpan.Zero));
        var tope = new TopeDeGasto(0.10m, reloj);

        tope.Cargar("ana", 0.06m);
        Assert.True(tope.PuedeGastar("ana"));
        tope.Cargar("ana", 0.05m);
        Assert.False(tope.PuedeGastar("ana"));
        Assert.True(tope.PuedeGastar("luis"));   // el tope es por usuario

        reloj.Ahora = reloj.Ahora.AddHours(2);   // cambia el día en UTC
        Assert.True(tope.PuedeGastar("ana"));
    }
}

public sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = ahora;
    public override DateTimeOffset GetUtcNow() => Ahora;
}
