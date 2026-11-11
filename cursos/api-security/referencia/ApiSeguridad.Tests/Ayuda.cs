using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ApiSeguridad;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiSeguridad.Tests;

/// <summary>La aplicacion de practica, arrancada en memoria. Los nombres de <c>lab</c> encienden una falla del laboratorio.</summary>
public sealed class Fabrica : WebApplicationFactory<Program>
{
    public const string Emisor = "https://emisor.ejemplo.test";
    public const string Audiencia = "api-ejemplo";
    public const string ClaveDePrueba = "clave-de-pruebas-que-mide-mas-de-32-bytes";

    private readonly Dictionary<string, string?> _ajustes;

    /// <param name="ajustes">Un nombre ("Bola") enciende ese interruptor del laboratorio; "Seccion:Clave=valor" fija un ajuste cualquiera.</param>
    public Fabrica(params string[] ajustes)
    {
        _ajustes = new()
        {
            ["Jwt:Emisor"] = Emisor,
            ["Jwt:Audiencia"] = Audiencia,
            ["Jwt:Clave"] = ClaveDePrueba,
        };
        foreach (var a in ajustes)
        {
            var i = a.IndexOf('=');
            if (i < 0) _ajustes[$"Lab:{a}"] = "true";
            else _ajustes[a[..i]] = a[(i + 1)..];
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        foreach (var (clave, valor) in _ajustes) builder.UseSetting(clave, valor);
    }

    /// <summary>Un cliente con la sesion de un usuario de ejemplo: 1 Ana, 2 Beto (clientes), 3 Admin.</summary>
    public HttpClient ComoUsuario(int id)
    {
        var rol = id == 3 ? "admin" : "cliente";
        var cliente = CreateClient();
        var token = Services.GetRequiredService<EmisorDeTokens>().Crear(id, rol);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

public static class Tokens
{
    /// <summary>Un token con lo que el emisor real haria, salvo lo que cada prueba cambie a proposito.</summary>
    public static string Crear(
        string? emisor = Fabrica.Emisor, string? audiencia = Fabrica.Audiencia, string clave = Fabrica.ClaveDePrueba,
        DateTime? expira = null, DateTime? desde = null, string algoritmo = SecurityAlgorithms.HmacSha256, bool sinExpiracion = false)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = emisor,
            Audience = audiencia,
            Subject = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("sub", "1"),
                new System.Security.Claims.Claim("role", "cliente"),
            ]),
            NotBefore = desde,
            Expires = sinExpiracion ? null : expira ?? DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)), algoritmo),
        };
        // Por omision el manejador agrega una expiracion de 60 minutos: se apaga para poder probar "sin exp".
        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = !sinExpiracion }.CreateToken(descriptor);
    }

    /// <summary>Un token SIN firma (alg none), construido solo para comprobar que la API propia lo rechaza.</summary>
    public static string SinFirma()
    {
        var cabecera = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
        var cuerpo = Base64UrlEncoder.Encode(
            $$"""{"iss":"{{Fabrica.Emisor}}","aud":"{{Fabrica.Audiencia}}","sub":"1","role":"cliente","exp":{{DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()}}}""");
        return $"{cabecera}.{cuerpo}.";
    }

    public static HttpClient ConToken(this Fabrica fabrica, string token)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

public static class Ayuda
{
    public static async Task<string> TextAsync(this HttpResponseMessage r) => await r.Content.ReadAsStringAsync();
    public static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
    public static async Task<HttpResponseMessage> PatchAsync(this HttpClient c, string ruta, string json) =>
        await c.PatchAsync(ruta, Json(json));
    public static async Task<T?> LeerAsync<T>(this HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<T>();
}
