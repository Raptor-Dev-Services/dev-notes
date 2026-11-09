using Asistente.Core.Rag;

namespace Asistente.Pruebas;

/// <summary>Contra un PostgreSQL con pgvector de verdad, con vectores léxicos: sin modelo y sin costo.</summary>
[Trait("Categoria", "Integracion")]
[Collection("BaseDeDatos")]
public class AlmacenVectorialIntegracion
{
    [Fact]
    public async Task Guarda_y_recupera_primero_el_fragmento_mas_parecido()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fuente = AlmacenVectorial.CrearFuente(Requiere.BaseDePruebas());
        var almacen = new AlmacenVectorial(fuente);
        var embeddings = new EmbeddingsPorHash();
        await almacen.CrearEsquemaAsync(embeddings.Dimensiones, ct);

        var fuenteDoc = $"prueba-{Guid.NewGuid():N}.md";
        var fragmentos = Fragmentador.Partir(fuenteDoc,
            "## Índices\n\nUn índice GIN acelera búsquedas de texto completo y arreglos.\n\n" +
            "## Respaldos\n\nEl respaldo diario se guarda siete días en el almacenamiento de objetos.");
        var vectores = await embeddings.GenerarAsync(fragmentos.Select(f => f.Contenido).ToList(), TipoTexto.Documento, ct);
        await almacen.ReemplazarFuenteAsync(fuenteDoc, fragmentos, vectores, ct);

        var consulta = (await embeddings.GenerarAsync(["¿Cuántos días se guarda el respaldo diario?"], TipoTexto.Consulta, ct))[0];
        var encontrados = await almacen.BuscarAsync(consulta, 5, ct);

        var primero = encontrados.First(f => f.Fuente == fuenteDoc);
        Assert.Equal("Respaldos", primero.Seccion);
        await almacen.ReemplazarFuenteAsync(fuenteDoc, [], [], ct);   // limpia lo que sembró
    }
}
