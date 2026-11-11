using System.Net;

namespace ApiSeguridad.Tests;

/// <summary>Modulo 5: logica de negocio, destinos y datos expuestos.</summary>
[Trait("Categoria", "Seguridad")]
public class Modulo5Tests
{
    private static async Task<HttpStatusCode> Cupon(HttpClient c, int pedido) =>
        (await c.PostAsync($"/api/pedidos/{pedido}/cupon", Ayuda.Json("""{"codigo":"PROMO10"}"""))).StatusCode;

    [Fact]
    public async Task Un_cupon_se_aplica_una_sola_vez_por_pedido()
    {
        using var f = new Fabrica();
        using var ana = f.ComoUsuario(1);
        Assert.Equal(HttpStatusCode.OK, await Cupon(ana, 100));
        Assert.Equal(HttpStatusCode.Conflict, await Cupon(ana, 100));
        var pedido = await (await ana.GetAsync("/api/pedidos/100")).LeerAsync<PedidoDto>();
        Assert.Equal(450m, pedido!.Total); // 500 menos 10 %, una vez
    }

    [Fact]
    public async Task Veinte_peticiones_simultaneas_aplican_el_cupon_una_vez()
    {
        using var f = new Fabrica();
        using var ana = f.ComoUsuario(1);
        var estados = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Cupon(ana, 101)));
        Assert.Equal(1, estados.Count(e => e == HttpStatusCode.OK));
        Assert.Equal(19, estados.Count(e => e == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Verlo_fallar_con_el_cupon_repetible_el_total_baja_cada_vez()
    {
        using var f = new Fabrica("CuponRepetible");
        using var ana = f.ComoUsuario(1);
        await Cupon(ana, 100);
        await Cupon(ana, 100);
        var pedido = await (await ana.GetAsync("/api/pedidos/100")).LeerAsync<PedidoDto>();
        Assert.Equal(405m, pedido!.Total); // 500 -> 450 -> 405: el descuento se acumulo
    }

    [Fact]
    public async Task El_cupon_de_un_pedido_ajeno_no_existe_para_mi()
    {
        using var f = new Fabrica();
        using var ana = f.ComoUsuario(1);
        Assert.Equal(HttpStatusCode.NotFound, await Cupon(ana, 200));
    }

    public static IEnumerable<object[]> Destinos_que_se_rechazan() =>
    [
        ["http://imagenes.ejemplo.test/a.png"],                // sin https
        ["https://imagenes.ejemplo.test:8443/a.png"],          // otro puerto
        ["https://otro.ejemplo.test/a.png"],                   // host fuera de la lista
        ["https://imagenes.ejemplo.test@otro.ejemplo.test/"],  // el host real es el de la derecha del @
        ["https://127.0.0.1/a.png"],                           // direccion numerica
        ["https://[::1]/a.png"],                               // direccion IPv6
        ["file:///etc/hostname"],                              // otro esquema
        ["/a.png"],                                            // relativa
        [""],
    ];

    [Theory]
    [MemberData(nameof(Destinos_que_se_rechazan))]
    public void La_lista_de_destinos_rechaza(string url) =>
        Assert.False(Destinos.EsPermitido(url, ["imagenes.ejemplo.test"]));

    [Fact]
    public void La_lista_de_destinos_acepta_lo_que_si_esta_permitido()
    {
        Assert.True(Destinos.EsPermitido("https://imagenes.ejemplo.test/a.png", ["imagenes.ejemplo.test"]));
        Assert.True(Destinos.EsPermitido("https://IMAGENES.ejemplo.test/a.png", ["imagenes.ejemplo.test"]));
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.1.2.3", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.32.0.1", false)]
    [InlineData("192.168.1.10", true)]
    [InlineData("169.254.169.254", true)]
    [InlineData("::1", true)]
    [InlineData("::ffff:10.0.0.1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("93.184.216.34", false)]
    public void Las_direcciones_internas_se_reconocen(string ip, bool interna) =>
        Assert.Equal(interna, Destinos.EsDireccionInterna(System.Net.IPAddress.Parse(ip)));

    [Fact]
    public async Task La_API_solo_visita_destinos_de_la_lista()
    {
        using var f = new Fabrica();
        using var c = f.ComoUsuario(1);
        var bien = await c.PostAsync("/api/vistas-previas", Ayuda.Json("""{"url":"https://imagenes.ejemplo.test/a.png"}"""));
        var mal = await c.PostAsync("/api/vistas-previas", Ayuda.Json("""{"url":"https://otro.ejemplo.test/a.png"}"""));
        Assert.Equal(HttpStatusCode.Accepted, bien.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, mal.StatusCode);
    }

    [Fact]
    public async Task Verlo_fallar_sin_lista_la_API_acepta_cualquier_destino()
    {
        using var f = new Fabrica("SinListaDeDestinos");
        using var c = f.ComoUsuario(1);
        var r = await c.PostAsync("/api/vistas-previas", Ayuda.Json("""{"url":"https://otro.ejemplo.test/a.png"}"""));
        Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
    }
}
