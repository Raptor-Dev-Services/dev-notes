namespace Asistente.Core.Rag;

/// <summary>Algunos modelos de embeddings tratan distinto un documento y una consulta.</summary>
public enum TipoTexto { Documento, Consulta }

/// <summary>
/// La interfaz propia: el resto del sistema no sabe qué proveedor genera los vectores.
/// Cambiar de proveedor obliga a reindexar todo: vectores de modelos distintos no se comparan.
/// </summary>
public interface IGeneradorEmbeddings
{
    string Modelo { get; }
    int Dimensiones { get; }
    Task<IReadOnlyList<float[]>> GenerarAsync(IReadOnlyList<string> textos, TipoTexto tipo, CancellationToken ct = default);
}
