namespace Asistente.Core.Llm;

/// <summary>Los modelos vigentes del curso, en un solo lugar.</summary>
public static class Modelos
{
    public const string Fable = "claude-fable-5-1";
    public const string Opus = "claude-opus-5-5";
    public const string Sonnet = "claude-sonnet-5-5";
    public const string Haiku = "claude-haiku-5-5";

    /// <summary>El de las prácticas: el más barato que cumple. Se cambia sin recompilar.</summary>
    public static string PorOmision =>
        Environment.GetEnvironmentVariable("ASISTENTE_MODELO") ?? Haiku;
}
