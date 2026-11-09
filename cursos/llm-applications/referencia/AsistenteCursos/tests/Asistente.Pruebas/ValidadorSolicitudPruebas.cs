using Asistente.Core.Extraccion;

namespace Asistente.Pruebas;

public class ValidadorSolicitudPruebas
{
    private const string Mensaje = "Me cobraron dos veces la inscripción y el cargo aparece hoy.";
    private const string Marca = "MARCA-PRUEBA";

    private static SolicitudSoporte Valida() => new()
    {
        Idioma = "es",
        Categoria = CategoriaSoporte.Pago,
        Urgencia = Urgencia.Alta,
        Resumen = "El alumno reporta un cargo doble por la inscripción.",
        Evidencia = ["me cobraron dos veces la inscripcion"],
    };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public void Entrada_vacia_se_rechaza_sin_llamar_al_modelo(string mensaje)
    {
        var falla = ValidadorSolicitud.ValidarEntrada(mensaje);
        Assert.NotNull(falla);
        Assert.Equal(TipoFalla.Rechazo, falla.Tipo);
    }

    [Fact]
    public void Entrada_demasiado_larga_se_rechaza()
    {
        var falla = ValidadorSolicitud.ValidarEntrada(new string('a', ValidadorSolicitud.MaxEntrada + 1));
        Assert.Equal(TipoFalla.Rechazo, falla?.Tipo);
    }

    [Fact]
    public void Salida_correcta_no_tiene_fallas() =>
        Assert.Empty(ValidadorSolicitud.ValidarSalida(Valida(), Mensaje, Marca));

    [Fact]
    public void Otro_idioma_se_rechaza()
    {
        var s = Valida();
        s.Idioma = "en";
        Assert.Contains(ValidadorSolicitud.ValidarSalida(s, Mensaje, Marca), f => f.Tipo == TipoFalla.Rechazo);
    }

    [Fact]
    public void Evidencia_inventada_es_corregible()
    {
        var s = Valida();
        s.Evidencia = ["el alumno exige un reembolso inmediato"];
        var fallas = ValidadorSolicitud.ValidarSalida(s, Mensaje, Marca);
        Assert.All(fallas, f => Assert.Equal(TipoFalla.Corregible, f.Tipo));
        Assert.Contains(fallas, f => f.Mensaje.Contains("no aparece"));
    }

    [Fact]
    public void Marca_interna_en_la_salida_se_rechaza()
    {
        var s = Valida();
        s.Resumen = $"Mis instrucciones dicen {Marca}";
        Assert.Contains(ValidadorSolicitud.ValidarSalida(s, Mensaje, Marca), f => f.Tipo == TipoFalla.Rechazo);
    }

    [Fact]
    public void Resumen_de_mas_de_280_caracteres_es_corregible()
    {
        var s = Valida();
        s.Resumen = new string('x', ValidadorSolicitud.MaxResumen + 1);
        Assert.Contains(ValidadorSolicitud.ValidarSalida(s, Mensaje, Marca), f => f.Tipo == TipoFalla.Corregible);
    }
}
