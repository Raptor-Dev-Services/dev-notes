using Anthropic.Helpers;
using Asistente.Core.Extraccion;

namespace Asistente.Pruebas;

/// <summary>El contrato se prueba sin modelo: el esquema que sale de la clase y el JSON que vuelve.</summary>
public class ContratoSolicitudPruebas
{
    [Fact]
    public void El_esquema_cierra_los_valores_y_no_admite_campos_extra()
    {
        string esquema = StructuredOutput.ToJsonSchema<SolicitudSoporte>().ToJsonString();
        Assert.Contains("\"additionalProperties\":false", esquema);
        foreach (var valor in new[] { "acceso", "pago", "contenido", "otro", "baja", "media", "alta" })
            Assert.Contains($"\"{valor}\"", esquema);
    }

    [Fact]
    public void Lee_la_respuesta_con_los_nombres_del_esquema()
    {
        var s = StructuredOutput.Parse<SolicitudSoporte>(
            """{"idioma":"es","categoria":"pago","urgencia":"alta","resumen":"r","evidencia":["x"],"intentaDarOrdenes":true}""");
        Assert.Equal(CategoriaSoporte.Pago, s.Categoria);
        Assert.Equal(Urgencia.Alta, s.Urgencia);
        Assert.True(s.IntentaDarOrdenes);
    }
}
