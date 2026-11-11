using System.Net;

namespace ApiSeguridad.Tests;

/// <summary>Modulo 3: autorizacion. Cada prueba usa DOS usuarios y mide lo que NO debe pasar.</summary>
[Trait("Categoria", "Seguridad")]
public class Modulo3Tests
{
    // Pedidos de ejemplo: 100 y 101 son de Ana (1); 200 es de Beto (2).
    private static readonly (int Usuario, int Pedido)[] Ajenos = [(1, 200), (2, 100), (2, 101)];

    /// <summary>Devuelve los cruces entre usuarios que SI dieron acceso. Una API correcta devuelve una lista vacia.</summary>
    public static async Task<List<string>> HallazgosBola(Fabrica f)
    {
        var hallazgos = new List<string>();
        foreach (var (usuario, pedido) in Ajenos)
        {
            using var c = f.ComoUsuario(usuario);
            var r = await c.GetAsync($"/api/pedidos/{pedido}");
            if (r.StatusCode != HttpStatusCode.NotFound) hallazgos.Add($"usuario {usuario} leyo el pedido {pedido} ({(int)r.StatusCode})");
        }
        return hallazgos;
    }

    [Fact]
    public async Task Cada_quien_ve_su_pedido()
    {
        using var f = new Fabrica();
        using var ana = f.ComoUsuario(1);
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/api/pedidos/100")).StatusCode);
    }

    [Fact]
    public async Task Nadie_ve_el_pedido_de_otro()
    {
        using var f = new Fabrica();
        Assert.Empty(await HallazgosBola(f));
    }

    [Fact]
    public async Task Un_pedido_ajeno_y_uno_inexistente_responden_igual()
    {
        using var f = new Fabrica();
        using var ana = f.ComoUsuario(1);
        var ajeno = await ana.GetAsync("/api/pedidos/200");
        var inexistente = await ana.GetAsync("/api/pedidos/999");
        Assert.Equal(inexistente.StatusCode, ajeno.StatusCode);
    }

    [Fact]
    public async Task El_listado_trae_solo_lo_propio()
    {
        using var f = new Fabrica();
        using var beto = f.ComoUsuario(2);
        var lista = await (await beto.GetAsync("/api/pedidos")).LeerAsync<List<PedidoDto>>();
        Assert.Equal([200], lista!.Select(p => p.Id));
    }

    [Fact]
    public async Task Verlo_fallar_sin_la_comprobacion_de_dueno_la_prueba_lo_detecta()
    {
        using var f = new Fabrica("Bola");
        var hallazgos = await HallazgosBola(f);
        Assert.Equal(3, hallazgos.Count);
    }

    [Fact]
    public async Task Un_cliente_no_usa_funciones_de_administracion()
    {
        using var f = new Fabrica();
        using var ana = f.ComoUsuario(1);
        using var admin = f.ComoUsuario(3);
        using var anonimo = f.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await ana.GetAsync("/api/admin/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/admin/usuarios")).StatusCode);
    }

    [Fact]
    public async Task Verlo_fallar_sin_la_politica_el_cliente_entra_a_administracion()
    {
        using var f = new Fabrica("Bfla");
        using var ana = f.ComoUsuario(1);
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/api/admin/usuarios")).StatusCode);
    }

    [Fact]
    public async Task El_cliente_no_puede_cambiar_su_propio_rol()
    {
        using var f = new Fabrica();
        using var ana = f.ComoUsuario(1);
        await ana.PatchAsync("/api/perfil", """{"nombre":"Ana M.","rol":"admin"}""");
        var perfil = await (await ana.GetAsync("/api/perfil")).LeerAsync<PerfilDto>();
        Assert.Equal("Ana M.", perfil!.Nombre);
        Assert.Equal("cliente", perfil.Rol);
    }

    [Fact]
    public async Task Verlo_fallar_con_la_entidad_enlazada_el_rol_cambia()
    {
        using var f = new Fabrica("Bopla");
        using var ana = f.ComoUsuario(1);
        await ana.PatchAsync("/api/perfil", """{"nombre":"Ana M.","rol":"admin"}""");
        var perfil = await (await ana.GetAsync("/api/perfil")).LeerAsync<PerfilDto>();
        Assert.Equal("admin", perfil!.Rol);
    }

    [Fact]
    public async Task El_perfil_no_devuelve_campos_internos()
    {
        using var f = new Fabrica();
        using var ana = f.ComoUsuario(1);
        var cuerpo = await (await ana.GetAsync("/api/perfil")).TextAsync();
        Assert.DoesNotContain("hashContrasena", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("notasInternas", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Verlo_fallar_devolver_la_entidad_filtra_campos_internos()
    {
        using var f = new Fabrica("EntidadCompleta");
        using var ana = f.ComoUsuario(1);
        var cuerpo = await (await ana.GetAsync("/api/perfil")).TextAsync();
        Assert.Contains("hashContrasena", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    // La matriz de autorizacion: quien (0 = sin sesion, 1 Ana, 2 Beto, 3 Admin), que llama y que debe recibir.
    // Pedidos 100 y 101 son de Ana; el 200, de Beto. El administrador NO ve pedidos ajenos: decision de negocio de la API de practica.
    public static IEnumerable<object[]> Matriz() =>
    [
        [0, "GET", "/api/perfil", 401],
        [0, "GET", "/api/pedidos", 401],
        [0, "GET", "/api/pedidos/100", 401],
        [0, "GET", "/api/admin/usuarios", 401],
        [0, "GET", "/api/reportes/resumen", 401],
        [1, "GET", "/api/perfil", 200],
        [1, "GET", "/api/pedidos", 200],
        [1, "GET", "/api/pedidos/100", 200],
        [1, "GET", "/api/pedidos/200", 404],
        [1, "POST", "/api/pedidos/200/cupon", 404],
        [1, "GET", "/api/admin/usuarios", 403],
        [1, "GET", "/api/reportes/resumen", 403],
        [2, "GET", "/api/pedidos/200", 200],
        [2, "GET", "/api/pedidos/100", 404],
        [2, "GET", "/api/pedidos/101", 404],
        [2, "GET", "/api/admin/usuarios", 403],
        [3, "GET", "/api/admin/usuarios", 200],
        [3, "GET", "/api/reportes/resumen", 200],
        [3, "GET", "/api/pedidos/100", 404],
    ];

    [Theory]
    [MemberData(nameof(Matriz))]
    public async Task Matriz_de_autorizacion(int quien, string verbo, string ruta, int esperado)
    {
        using var f = new Fabrica();
        using var cliente = quien == 0 ? f.CreateClient() : f.ComoUsuario(quien);
        var contenido = verbo == "POST" ? Ayuda.Json("""{"codigo":"PROMO10"}""") : null;
        var r = await cliente.SendAsync(new HttpRequestMessage(new HttpMethod(verbo), ruta) { Content = contenido });
        Assert.Equal(esperado, (int)r.StatusCode);
    }
}
