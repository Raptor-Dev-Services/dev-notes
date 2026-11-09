using System.Security.Cryptography;
using System.Text;

namespace Asistente.Core.Prompts;

/// <summary>Un prompt con nombre, versión y huella: lo que se registra en cada llamada.</summary>
public sealed record PromptVersionado(string Nombre, int Version, string Texto)
{
    public string Id => $"{Nombre}.v{Version}";

    /// <summary>Los primeros 12 caracteres del SHA-256: delata si alguien editó el archivo sin subir la versión.</summary>
    public string Huella { get; } = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Texto)))[..12];
}
