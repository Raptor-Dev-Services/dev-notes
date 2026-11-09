namespace Asistente.Core.Rag;

/// <summary>
/// Vectores léxicos por hashing de palabras: sin modelo, sin red y deterministas.
/// Sirven para probar la tubería (fragmentar, guardar, buscar). No entienden sinónimos.
/// </summary>
public sealed class EmbeddingsPorHash(int dimensiones = 1024) : IGeneradorEmbeddings
{
    public string Modelo => "hash-lexico";
    public int Dimensiones => dimensiones;

    public Task<IReadOnlyList<float[]>> GenerarAsync(
        IReadOnlyList<string> textos, TipoTexto tipo, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<float[]>>(textos.Select(Vectorizar).ToList());

    private float[] Vectorizar(string texto)
    {
        var v = new float[dimensiones];
        var palabras = Texto.Normalizar(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => new string(p.Where(char.IsLetterOrDigit).ToArray()))
            .Where(p => p.Length >= 3);
        foreach (var palabra in palabras)
            v[Fnv1a(palabra) % (uint)dimensiones] += 1f;

        double norma = Math.Sqrt(v.Sum(x => (double)x * x));
        if (norma == 0) { v[0] = 1f; return v; }   // pgvector no define el coseno de un vector en cero
        for (int i = 0; i < v.Length; i++) v[i] = (float)(v[i] / norma);
        return v;
    }

    private static uint Fnv1a(string s)
    {
        uint h = 2166136261;
        foreach (char c in s) { h ^= c; h *= 16777619; }
        return h;
    }
}
