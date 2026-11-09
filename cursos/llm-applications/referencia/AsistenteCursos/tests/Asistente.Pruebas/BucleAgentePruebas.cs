using System.Text.Json;
using Anthropic.Models.Messages;
using Asistente.Core.Agente;
using Asistente.Core.Llm;

namespace Asistente.Pruebas;

/// <summary>El bucle se prueba con respuestas guionadas: sin red, sin costo y en milisegundos.</summary>
public class BucleAgentePruebas
{
    [Fact]
    public async Task Sin_confirmacion_la_escritura_no_se_ejecuta()
    {
        var espia = new HerramientaEspia("crear_ticket", escribe: true);
        var modelo = new ModeloGuionado(PideHerramienta("crear_ticket"), Termina("Entendido, no lo creo."));
        var bitacora = new Bitacora();

        var r = await Bucle(modelo, espia, new ConfirmadorFijo(false), bitacora).EjecutarAsync("Crea un ticket", TestContext.Current.CancellationToken);

        Assert.True(r.Terminado);
        Assert.Equal(0, espia.Ejecuciones);
        Assert.Contains(bitacora.Eventos, e => e.Tipo == "confirmacion" && e.Detalle == "rechazada");
        // Lo que vio el modelo en el segundo turno: un resultado con error que explica el rechazo.
        var segundo = JsonSerializer.Serialize(modelo.Recibidos[1].Messages[^1]);
        Assert.Contains("rechaz", segundo);
    }

    [Fact]
    public async Task Con_confirmacion_la_escritura_se_ejecuta_una_vez()
    {
        var espia = new HerramientaEspia("crear_ticket", escribe: true);
        var modelo = new ModeloGuionado(PideHerramienta("crear_ticket"), Termina("Listo."));

        await Bucle(modelo, espia, new ConfirmadorFijo(true), new Bitacora()).EjecutarAsync("Crea un ticket", TestContext.Current.CancellationToken);

        Assert.Equal(1, espia.Ejecuciones);
    }

    [Fact]
    public async Task Una_lectura_no_pide_confirmacion()
    {
        var espia = new HerramientaEspia("buscar_tickets", escribe: false);
        var confirmador = new ConfirmadorFijo(false);
        var modelo = new ModeloGuionado(PideHerramienta("buscar_tickets"), Termina("No hay tickets."));

        await Bucle(modelo, espia, confirmador, new Bitacora()).EjecutarAsync("¿Hay tickets?", TestContext.Current.CancellationToken);

        Assert.Equal(1, espia.Ejecuciones);
        Assert.Equal(0, confirmador.Preguntas);
    }

    [Fact]
    public async Task Se_detiene_en_el_limite_de_pasos()
    {
        var espia = new HerramientaEspia("buscar_tickets", escribe: false);
        var modelo = new ModeloGuionado(Enumerable.Range(0, 10).Select(_ => PideHerramienta("buscar_tickets")).ToArray());

        var r = await Bucle(modelo, espia, new ConfirmadorFijo(true), new Bitacora(), new LimitesAgente(MaxPasos: 3))
            .EjecutarAsync("Busca para siempre", TestContext.Current.CancellationToken);

        Assert.False(r.Terminado);
        Assert.Equal(3, modelo.Recibidos.Count);
        Assert.Contains("límite", r.Motivo);
    }

    [Fact]
    public async Task Se_detiene_al_pasar_el_tope_de_gasto_sin_ejecutar_lo_pedido()
    {
        var espia = new HerramientaEspia("crear_ticket", escribe: true);
        // 200 mil tokens de entrada en Haiku 5.5 cuestan 0.10 USD: más que el tope de 0.01.
        var modelo = new ModeloGuionado(PideHerramienta("crear_ticket", tokensEntrada: 200_000), Termina("x"));

        var r = await Bucle(modelo, espia, new ConfirmadorFijo(true), new Bitacora(), new LimitesAgente(TopeUsd: 0.01m))
            .EjecutarAsync("Crea un ticket", TestContext.Current.CancellationToken);

        Assert.False(r.Terminado);
        Assert.Equal(0, espia.Ejecuciones);
        Assert.Contains("tope", r.Motivo);
    }

    private static BucleAgente Bucle(IModelo modelo, IHerramienta h, IConfirmador c, Bitacora b, LimitesAgente? l = null) =>
        new(modelo, Modelos.Haiku, "sistema de prueba", [h], c, b, l ?? new LimitesAgente());

    private static string PideHerramienta(string nombre, long tokensEntrada = 100) => $$$"""
        {"id":"msg_prueba","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"tool_use","id":"toolu_{{{Guid.NewGuid():N}}}","name":"{{{nombre}}}","input":{"texto":"x"},"caller":{"type":"direct"}}],
         "stop_reason":"tool_use","stop_sequence":null,
         "usage":{"input_tokens":{{{tokensEntrada}}},"output_tokens":20,"cache_creation_input_tokens":0,"cache_read_input_tokens":0}}
        """;

    private static string Termina(string texto) => $$$"""
        {"id":"msg_prueba","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"{{{texto}}}","citations":null}],
         "stop_reason":"end_turn","stop_sequence":null,
         "usage":{"input_tokens":100,"output_tokens":20,"cache_creation_input_tokens":0,"cache_read_input_tokens":0}}
        """;
}

public sealed class ModeloGuionado(params string[] respuestas) : IModelo
{
    private int siguiente;
    public List<MessageCreateParams> Recibidos { get; } = [];

    public Task<Message> EnviarAsync(MessageCreateParams parametros, CancellationToken ct)
    {
        Recibidos.Add(parametros);
        return Task.FromResult(JsonSerializer.Deserialize<Message>(respuestas[siguiente++])!);
    }
}

public sealed class HerramientaEspia(string nombre, bool escribe) : IHerramienta
{
    public int Ejecuciones { get; private set; }
    public string Nombre => nombre;
    public bool Escribe => escribe;
    public Tool Definicion => new() { Name = nombre, Description = "espía", InputSchema = new() };
    public string Describir(IReadOnlyDictionary<string, JsonElement> entrada) => "propuesta de prueba";

    public Task<string> EjecutarAsync(IReadOnlyDictionary<string, JsonElement> entrada, CancellationToken ct)
    {
        Ejecuciones++;
        return Task.FromResult("hecho");
    }
}

public sealed class ConfirmadorFijo(bool respuesta) : IConfirmador
{
    public int Preguntas { get; private set; }

    public Task<bool> ConfirmarAsync(PropuestaDeAccion propuesta, CancellationToken ct)
    {
        Preguntas++;
        return Task.FromResult(respuesta);
    }
}
