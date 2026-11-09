using System.Text.Json;
using Anthropic.Helpers;
using Anthropic.Models.Messages;

namespace Asistente.Core.Agente;

public interface IHerramienta
{
    string Nombre { get; }
    Tool Definicion { get; }

    /// <summary>Si escribe, el bucle pide la confirmación de una persona antes de ejecutarla.</summary>
    bool Escribe { get; }

    string Describir(IReadOnlyDictionary<string, JsonElement> entrada);
    Task<string> EjecutarAsync(IReadOnlyDictionary<string, JsonElement> entrada, CancellationToken ct);
}

/// <summary>Una herramienta con entrada tipada: el esquema JSON sale de la clase TEntrada.</summary>
public abstract class Herramienta<TEntrada> : IHerramienta where TEntrada : class, new()
{
    public abstract string Nombre { get; }
    public abstract bool Escribe { get; }

    /// <summary>Lo que lee el modelo para decidir cuándo usarla: di cuándo, no solo qué hace.</summary>
    protected abstract string Descripcion { get; }

    public Tool Definicion => new()
    {
        Name = Nombre,
        Description = Descripcion,
        InputSchema = InputSchema.FromRawUnchecked(
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(StructuredOutput.ToJsonSchema<TEntrada>().ToJsonString())!),
        Strict = true,   // la entrada que manda el modelo cumple el esquema, siempre
    };

    public string Describir(IReadOnlyDictionary<string, JsonElement> entrada) => Describir(Leer(entrada));

    public Task<string> EjecutarAsync(IReadOnlyDictionary<string, JsonElement> entrada, CancellationToken ct) =>
        HacerAsync(Leer(entrada), ct);

    protected abstract Task<string> HacerAsync(TEntrada entrada, CancellationToken ct);

    protected virtual string Describir(TEntrada entrada) =>
        JsonSerializer.Serialize(entrada, new JsonSerializerOptions { WriteIndented = true });

    private static TEntrada Leer(IReadOnlyDictionary<string, JsonElement> entrada) =>
        StructuredOutput.Parse<TEntrada>(JsonSerializer.SerializeToElement(entrada));
}
