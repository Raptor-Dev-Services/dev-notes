using System.Net;
using System.Reflection;

namespace ApiSeguridad.Tests;

/// <summary>Modulo 6: configuracion y las comprobaciones que se quedan para siempre.</summary>
[Trait("Categoria", "Seguridad")]
public class Modulo6Tests
{
    [Fact]
    public void Todos_los_interruptores_del_laboratorio_arrancan_apagados()
    {
        var encendidos = typeof(LabOpciones).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(bool) && (bool)p.GetValue(new LabOpciones())!)
            .Select(p => p.Name).ToList();
        Assert.Empty(encendidos);
    }

    [Fact]
    public async Task Las_respuestas_llevan_las_cabeceras_de_una_API()
    {
        using var f = new Fabrica();
        using var c = f.ComoUsuario(1);
        var r = await c.GetAsync("/api/pedidos");
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-store", r.Headers.GetValues("Cache-Control").Single());
        Assert.Equal("frame-ancestors 'none'", r.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task CORS_solo_acepta_el_origen_de_la_aplicacion()
    {
        using var f = new Fabrica();
        using var c = f.CreateClient();
        HttpRequestMessage Previa(string origen) => new(HttpMethod.Options, "/api/perfil")
        {
            Headers = { { "Origin", origen }, { "Access-Control-Request-Method", "GET" } },
        };
        var propio = await c.SendAsync(Previa("https://app.ejemplo.test"));
        var ajeno = await c.SendAsync(Previa("https://otro.ejemplo.test"));
        Assert.Equal("https://app.ejemplo.test", propio.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(ajeno.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Una_ruta_sin_sesion_responde_401_con_formato_de_problema()
    {
        using var f = new Fabrica();
        using var c = f.CreateClient();
        var r = await c.GetAsync("/api/pedidos");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
