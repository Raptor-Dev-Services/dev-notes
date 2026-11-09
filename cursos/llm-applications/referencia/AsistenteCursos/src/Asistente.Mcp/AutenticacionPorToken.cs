using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Asistente.Mcp;

public sealed class OpcionesToken : AuthenticationSchemeOptions
{
    /// <summary>SHA-256 del token, en hexadecimal. El servidor guarda la huella, nunca el secreto.</summary>
    public string HuellaDelToken { get; set; } = "";
}

/// <summary>Valida "Authorization: Bearer &lt;token&gt;" comparando huellas en tiempo constante.</summary>
public sealed class AutenticacionPorToken(IOptionsMonitor<OpcionesToken> opciones, ILoggerFactory registro, UrlEncoder codificador)
    : AuthenticationHandler<OpcionesToken>(opciones, registro, codificador)
{
    public const string Esquema = "TokenDeAcceso";
    private const string Prefijo = "Bearer ";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? cabecera = Request.Headers.Authorization;
        if (cabecera is null || !cabecera.StartsWith(Prefijo, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.NoResult());

        byte[] presentada = SHA256.HashData(Encoding.UTF8.GetBytes(cabecera[Prefijo.Length..].Trim()));
        byte[] esperada = Convert.FromHexString(Options.HuellaDelToken);
        if (!CryptographicOperations.FixedTimeEquals(presentada, esperada))
            return Task.FromResult(AuthenticateResult.Fail("Token inválido."));

        var identidad = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "cliente-mcp"), new Claim("scope", "cursos:leer")], Esquema);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema)));
    }
}
