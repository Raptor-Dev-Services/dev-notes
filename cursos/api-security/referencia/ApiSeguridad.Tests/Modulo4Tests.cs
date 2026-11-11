using System.Net;

namespace ApiSeguridad.Tests;

/// <summary>Modulo 4: entradas, errores y limites.</summary>
[Trait("Categoria", "Seguridad")]
public class Modulo4Tests
{
    

    [Fact]
    public async Task Un_apostrofe_en_la_busqueda_es_texto_y_no_rompe_la_consulta()
    {
        using var f = new Fabrica();
        using var c = f.ComoUsuario(1);
        var r = await c.GetAsync("/api/productos?nombre=O'Brien");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Cuaderno O'Brien", await r.TextAsync());
    }

    [Fact]
    public async Task Verlo_fallar_con_la_consulta_concatenada_el_apostrofe_da_error_500()
    {
        using var f = new Fabrica("SqlConcatenado");
        using var c = f.ComoUsuario(1);
        var r = await c.GetAsync("/api/productos?nombre=O'Brien");
        Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
    }

    [Fact]
    public async Task El_cliente_no_decide_cuantos_registros_recibe()
    {
        using var f = new Fabrica();
        using var c = f.ComoUsuario(1);
        var cuerpo = await (await c.GetAsync("/api/productos?limite=100000")).TextAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(cuerpo);
        Assert.Equal(50, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Verlo_fallar_sin_tope_la_respuesta_trae_todo_el_catalogo()
    {
        using var f = new Fabrica("SinTopeDePagina");
        using var c = f.ComoUsuario(1);
        var cuerpo = await (await c.GetAsync("/api/productos?limite=100000")).TextAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(cuerpo);
        Assert.Equal(120, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Una_busqueda_demasiado_larga_se_rechaza_con_400()
    {
        using var f = new Fabrica();
        using var c = f.ComoUsuario(1);
        var r = await c.GetAsync("/api/productos?nombre=" + new string('a', 51));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Un_fallo_interno_no_cuenta_como_esta_hecha_la_API()
    {
        using var f = new Fabrica("ProvocarFalla");
        using var admin = f.ComoUsuario(3);
        var r = await admin.GetAsync("/api/reportes/resumen");
        var cuerpo = await r.TextAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Password", cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.DoesNotContain("   at ", cuerpo);
        Assert.Contains("traceId", cuerpo);
    }

    [Fact]
    public async Task Verlo_fallar_devolver_la_excepcion_filtra_la_cadena_de_conexion()
    {
        using var f = new Fabrica("ProvocarFalla", "ErroresDetallados");
        using var admin = f.ComoUsuario(3);
        var cuerpo = await (await admin.GetAsync("/api/reportes/resumen")).TextAsync();
        Assert.Contains("Password", cuerpo);
    }

    [Fact]
    public async Task Un_cuerpo_enorme_se_rechaza_antes_de_leerlo()
    {
        using var f = new Fabrica();
        using var c = f.CreateClient();
        var r = await c.PostAsync("/api/auth/login", Ayuda.Json("{\"usuario\":\"" + new string('a', 70_000) + "\"}"));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, r.StatusCode);
    }
}
