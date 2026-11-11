using System.Net;
using Microsoft.IdentityModel.Tokens;

namespace ApiSeguridad.Tests;

/// <summary>Modulo 2: autenticacion y JWT. Cada defecto se prueba con un token que la API debe rechazar.</summary>
[Trait("Categoria", "Seguridad")]
public class Modulo2Tests
{
    private static async Task<HttpStatusCode> Perfil(Fabrica f, string token)
    {
        using var c = f.ConToken(token);
        return (await c.GetAsync("/api/perfil")).StatusCode;
    }

    [Fact]
    public async Task Un_token_valido_entra()
    {
        using var f = new Fabrica();
        Assert.Equal(HttpStatusCode.OK, await Perfil(f, Tokens.Crear()));
    }

    [Fact]
    public async Task Sin_token_no_entra()
    {
        using var f = new Fabrica();
        using var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/perfil")).StatusCode);
    }

    [Fact]
    public async Task Un_token_vencido_no_entra()
    {
        using var f = new Fabrica();
        var token = Tokens.Crear(desde: DateTime.UtcNow.AddHours(-2), expira: DateTime.UtcNow.AddHours(-1));
        Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, token));
    }

    [Fact]
    public async Task Un_token_sin_expiracion_no_entra()
    {
        using var f = new Fabrica();
        Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, Tokens.Crear(sinExpiracion: true)));
    }

    [Fact]
    public async Task Un_token_de_otro_emisor_no_entra()
    {
        using var f = new Fabrica();
        Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, Tokens.Crear(emisor: "https://otro.ejemplo.test")));
    }

    [Fact]
    public async Task Un_token_para_otra_audiencia_no_entra()
    {
        using var f = new Fabrica();
        Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, Tokens.Crear(audiencia: "otra-api")));
    }

    [Fact]
    public async Task Un_token_firmado_con_otra_clave_no_entra()
    {
        using var f = new Fabrica();
        Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, Tokens.Crear(clave: "otra-clave-distinta-de-mas-de-32-bytes!")));
    }

    [Fact]
    public async Task Un_token_con_algoritmo_no_permitido_no_entra()
    {
        // Clave de 64 bytes (HS512 la exige de 512 bits). Misma clave, pero otro algoritmo: la API solo acepta el que ella usa.
        var clave = new string('k', 64);
        using var f = new Fabrica($"Jwt:Clave={clave}");
        Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, Tokens.Crear(clave: clave, algoritmo: SecurityAlgorithms.HmacSha512)));
        Assert.Equal(HttpStatusCode.OK, await Perfil(f, Tokens.Crear(clave: clave)));
    }

    [Fact]
    public async Task Un_token_sin_firma_no_entra()
    {
        using var f = new Fabrica();
        Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, Tokens.SinFirma()));
    }

    [Fact]
    public void Una_clave_corta_se_rechaza()
    {
        Assert.False(ClavesJwt.EsValida(null));
        Assert.False(ClavesJwt.EsValida("secreto"));
        Assert.False(ClavesJwt.EsValida(new string('a', 31)));
        Assert.True(ClavesJwt.EsValida(new string('a', 32)));
    }

    [Fact]
    public void Verlo_fallar_la_aplicacion_no_arranca_con_una_clave_corta()
    {
        using var f = new Fabrica("Jwt:Clave=secreto");
        var error = Assert.ThrowsAny<Exception>(() => f.CreateClient());
        Assert.Contains("256 bits", error.ToString());
    }

    [Fact]
    public async Task Credenciales_correctas_dan_un_token_que_funciona()
    {
        using var f = new Fabrica();
        using var c = f.CreateClient();
        var r = await c.PostAsync("/api/auth/login", Ayuda.Json("""{"usuario":"Ana","clave":"clave-de-ejemplo"}"""));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var token = (await r.LeerAsync<Dictionary<string, string>>())!["token"];
        Assert.Equal(HttpStatusCode.OK, await Perfil(f, token));
    }

    [Fact]
    public async Task Usuario_inexistente_y_clave_mala_responden_igual()
    {
        using var f = new Fabrica();
        using var c = f.CreateClient();
        var malaClave = await c.PostAsync("/api/auth/login", Ayuda.Json("""{"usuario":"Ana","clave":"incorrecta"}"""));
        var noExiste = await c.PostAsync("/api/auth/login", Ayuda.Json("""{"usuario":"Nadie","clave":"incorrecta"}"""));
        Assert.Equal(HttpStatusCode.Unauthorized, malaClave.StatusCode);
        Assert.Equal(malaClave.StatusCode, noExiste.StatusCode);
        // Se compara el titulo, no el traceId, que cambia en cada peticion.
        var a = await malaClave.LeerAsync<Dictionary<string, object>>();
        var b = await noExiste.LeerAsync<Dictionary<string, object>>();
        Assert.Equal(a!["title"].ToString(), b!["title"].ToString());
    }

    [Fact]
    public async Task El_sexto_intento_de_login_en_un_minuto_se_limita()
    {
        using var f = new Fabrica();
        using var c = f.CreateClient();
        var estados = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
            estados.Add((await c.PostAsync("/api/auth/login", Ayuda.Json("""{"usuario":"Ana","clave":"incorrecta"}"""))).StatusCode);
        Assert.All(estados.Take(5), e => Assert.Equal(HttpStatusCode.Unauthorized, e));
        Assert.Equal(HttpStatusCode.TooManyRequests, estados[5]);
    }

    [Fact]
    public void Un_token_se_lee_sin_la_clave_porque_esta_codificado_no_cifrado()
    {
        var token = Tokens.Crear();
        var partes = token.Split('.');
        Assert.Equal(3, partes.Length);
        var cabecera = Base64UrlEncoder.Decode(partes[0]);
        var cuerpo = Base64UrlEncoder.Decode(partes[1]);
        Assert.Contains("\"alg\":\"HS256\"", cabecera);
        Assert.Contains("\"sub\":\"1\"", cuerpo);
        Assert.Contains("\"role\":\"cliente\"", cuerpo);
    }

    [Fact]
    public void El_margen_de_reloj_por_omision_de_la_libreria_es_de_cinco_minutos()
    {
        // Por eso la API lo baja a 30 segundos: un token vencido no debe seguir sirviendo cinco minutos mas.
        Assert.Equal(TimeSpan.FromMinutes(5), new TokenValidationParameters().ClockSkew);
    }

    [Fact]
    public void El_manejador_agrega_una_expiracion_de_60_minutos_si_no_se_le_da_ninguna()
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("sub", "1")]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Fabrica.ClaveDePrueba)), SecurityAlgorithms.HmacSha256),
        };
        var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().CreateToken(descriptor);
        var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
        var vida = jwt.ValidTo - DateTime.UtcNow;
        Assert.InRange(vida.TotalMinutes, 59, 60.1);
    }
}
