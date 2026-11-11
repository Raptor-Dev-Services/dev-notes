using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSeguridad.Tests;

/// <summary>Modulo 1: el inventario. Lo que la aplicacion expone se compara con lo que el equipo documento.</summary>
[Trait("Categoria", "Seguridad")]
public class Modulo1Tests
{
    public sealed record Diferencias(List<string> SinDocumentar, List<string> Fantasma, List<string> AnonimasInesperadas);

    private static string Llave(RouteEndpoint e, string verbo) => $"{verbo} {e.RoutePattern.RawText}";

    public static Diferencias Comparar(Fabrica fabrica)
    {
        var documentadas = File.ReadAllLines("rutas-documentadas.txt")
            .Where(l => l.Trim().Length > 0 && !l.StartsWith('#')).ToHashSet();
        var reales = new HashSet<string>();
        var anonimas = new HashSet<string>();
        foreach (var fuente in fabrica.Services.GetServices<EndpointDataSource>())
            foreach (var e in fuente.Endpoints.OfType<RouteEndpoint>())
            {
                var verbos = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
                foreach (var v in verbos.Where(v => v != "OPTIONS"))
                {
                    reales.Add(Llave(e, v));
                    if (e.Metadata.GetMetadata<IAllowAnonymous>() is not null) anonimas.Add(Llave(e, v));
                }
            }
        return new Diferencias(
            reales.Except(documentadas).Order().ToList(),
            documentadas.Except(reales).Order().ToList(),
            anonimas.Except(["POST /api/auth/login"]).Order().ToList());
    }

    [Fact]
    public void La_API_expone_exactamente_lo_que_documenta()
    {
        using var fabrica = new Fabrica();
        var d = Comparar(fabrica);
        Assert.Empty(d.SinDocumentar);
        Assert.Empty(d.Fantasma);
    }

    [Fact]
    public void Solo_el_login_es_publico()
    {
        using var fabrica = new Fabrica();
        Assert.Empty(Comparar(fabrica).AnonimasInesperadas);
    }

    [Fact]
    public void Verlo_fallar_una_ruta_olvidada_aparece_como_hallazgo()
    {
        using var fabrica = new Fabrica("RutaOlvidada");
        var d = Comparar(fabrica);
        Assert.Contains("GET /api/v0/debug/estado", d.SinDocumentar);
        Assert.Contains("GET /api/v0/debug/estado", d.AnonimasInesperadas);
    }

    [Fact]
    public async Task Una_ruta_que_nadie_documento_no_responde_sin_sesion()
    {
        using var fabrica = new Fabrica();
        using var cliente = fabrica.CreateClient();
        // La politica por omision exige sesion en toda ruta; esta ni siquiera existe.
        var r = await cliente.GetAsync("/api/v0/debug/estado");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
