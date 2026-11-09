using Anthropic;
using Anthropic.Helpers;
using Anthropic.Models.Messages;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;
using Asistente.Core.Seguridad;

namespace Asistente.Core.Rag;

/// <summary>Responde con los documentos indexados y cita la fuente; si no está, dice que no sabe.</summary>
public sealed class AsistenteDocumentos(
    AnthropicClient cliente,
    IGeneradorEmbeddings embeddings,
    AlmacenVectorial almacen,
    PromptVersionado prompt,
    string modelo,
    OpcionesRag opciones)
{
    public const string NoLoSe = "No lo sé: eso no está en los documentos del curso.";

    public async Task<RespuestaRag> PreguntarAsync(string pregunta, CancellationToken ct = default)
    {
        var consulta = (await embeddings.GenerarAsync([pregunta], TipoTexto.Consulta, ct))[0];
        var recuperadas = await almacen.BuscarAsync(consulta, opciones.K, ct);
        var relevantes = recuperadas.Where(f => f.Distancia <= opciones.DistanciaMaxima).ToList();

        // Primera defensa contra inventar: si nada se parece, no se le pregunta al modelo.
        if (relevantes.Count == 0)
            return SinRespuesta("Ningún fragmento pasó el umbral de distancia.", recuperadas, 0m);

        var respuesta = await cliente.Messages.Create(new MessageCreateParams
        {
            Model = modelo,
            MaxTokens = 4096,
            System = new List<TextBlockParam> { new() { Text = prompt.Texto, CacheControl = new CacheControlEphemeral() } },
            OutputConfig = new OutputConfig
            {
                Effort = Effort.Low,
                Format = StructuredOutput.CreateJsonFormat<SalidaRag>(),
            },
            Messages = [new UserMessageParam { Content = ConstruirContexto(relevantes) + $"\n\n<pregunta>\n{pregunta}\n</pregunta>" }],
        }, ct);
        decimal costo = TablaDePrecios.Costo(modelo, respuesta.Usage);

        if (respuesta.StopReason != StopReason.EndTurn)
            return SinRespuesta($"La llamada terminó con stop_reason {respuesta.StopReason}.", recuperadas, costo);

        var salida = StructuredOutput.Parse<SalidaRag>(
            string.Concat(respuesta.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text)));
        if (!salida.Sabe)
            return SinRespuesta("El modelo indicó que los documentos no lo contienen.", recuperadas, costo);

        // Segunda defensa: toda cita tiene que ser un fragmento que de verdad se le entregó.
        var citadas = relevantes.Where(f => salida.Fuentes.Contains(f.Clave)).ToList();
        if (citadas.Count == 0 || citadas.Count != salida.Fuentes.Distinct().Count())
            return SinRespuesta("La respuesta no citó fuentes o citó claves que no se recuperaron.", recuperadas, costo);

        // Tercera defensa, determinista: lo que sale no lleva imágenes ni enlaces a dominios ajenos.
        var filtrada = new FiltroDeSalida(opciones.DominiosPermitidos ?? []).Filtrar(salida.Respuesta);
        return new RespuestaRag(true, filtrada.Texto, citadas, recuperadas, costo, null, filtrada.Retirados);
    }

    /// <summary>
    /// Cada fragmento va dentro de su etiqueta y con los signos &lt; y &gt; escapados:
    /// un documento no puede cerrar su etiqueta y escribir fuera de ella.
    /// </summary>
    public static string ConstruirContexto(IReadOnlyList<FragmentoRecuperado> fragmentos) =>
        "<documentos>\n" + string.Join("\n", fragmentos.Select(f =>
            $"<documento clave=\"{f.Clave}\" fuente=\"{Escapar(f.Fuente)}\">\n{Escapar(f.Contenido)}\n</documento>"))
        + "\n</documentos>";

    private static string Escapar(string texto) =>
        texto.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static RespuestaRag SinRespuesta(string motivo, IReadOnlyList<FragmentoRecuperado> recuperadas, decimal costo) =>
        new(false, NoLoSe, [], recuperadas, costo, motivo);
}
