using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiSeguridad;

public sealed class OpcionesJwt
{
    public string Emisor { get; set; } = "";
    public string Audiencia { get; set; } = "";
    public string Clave { get; set; } = "";
    public int MinutosDeVida { get; set; } = 15;
}

public static class ClavesJwt
{
    /// <summary>RFC 7518, 3.2: para HS256 la clave mide 256 bits o mas. Se exige al arrancar, no cuando alguien la adivina.</summary>
    public const int BytesMinimos = 32;

    public static bool EsValida(string? clave) =>
        !string.IsNullOrWhiteSpace(clave) && Encoding.UTF8.GetByteCount(clave) >= BytesMinimos;
}

/// <summary>Emite el token de acceso. Un solo lugar: lo que se firma aqui es lo que valida <see cref="ConfigurarJwtBearer"/>.</summary>
public sealed class EmisorDeTokens(IOptions<OpcionesJwt> opciones)
{
    public string Crear(int usuarioId, string rol)
    {
        var o = opciones.Value;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = o.Emisor,
            Audience = o.Audiencia,
            Subject = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("sub", usuarioId.ToString()),
                new System.Security.Claims.Claim("role", rol),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(o.MinutosDeVida),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Clave)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

/// <summary>La validacion estricta: emisor, audiencia, vida, firma Y algoritmo permitido.</summary>
public sealed class ConfigurarJwtBearer(IOptions<OpcionesJwt> opciones) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;
        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        var o = opciones.Value;
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = o.Emisor,
            ValidateAudience = true,
            ValidAudience = o.Audiencia,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Clave)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
    }
}
