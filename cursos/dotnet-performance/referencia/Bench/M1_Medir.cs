using System.Text;
using BenchmarkDotNet.Attributes;

namespace Bench;

// Modulo 1: dos implementaciones de la misma funcion, con una comprobacion de que dan lo mismo.
[MemoryDiagnoser]
public class ContarPalabras
{
    private string _texto = "";

    [Params(100, 10_000)]
    public int Palabras;

    [GlobalSetup]
    public void Preparar()
    {
        var azar = new Random(42);
        var sb = new StringBuilder();
        for (int i = 0; i < Palabras; i++)
        {
            sb.Append(new string((char)('a' + azar.Next(26)), azar.Next(2, 9)));
            sb.Append(i % 7 == 0 ? "  " : " ");
        }
        _texto = sb.ToString();

        // Un benchmark que compara dos metodos que dan resultados distintos no mide nada util.
        if (ConSplit() != ConCiclo())
            throw new InvalidOperationException("Las dos implementaciones no coinciden.");
    }

    [Benchmark(Baseline = true)]
    public int ConSplit() => _texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    [Benchmark]
    public int ConCiclo()
    {
        int palabras = 0;
        bool dentro = false;
        foreach (char c in _texto)
        {
            if (char.IsWhiteSpace(c)) dentro = false;
            else if (!dentro) { dentro = true; palabras++; }
        }
        return palabras;
    }
}

[MemoryDiagnoser]
public class UnirTextos
{
    private string[] _partes = [];

    [Params(10, 1_000)]
    public int N;

    [GlobalSetup]
    public void Preparar()
    {
        _partes = Enumerable.Range(0, N).Select(i => "parte" + i).ToArray();
        if (Concatenar() != ConStringBuilder() || Concatenar() != ConJoin())
            throw new InvalidOperationException("Las implementaciones no coinciden.");
    }

    [Benchmark(Baseline = true)]
    public string Concatenar()
    {
        string r = "";
        for (int i = 0; i < _partes.Length; i++)
        {
            if (i > 0) r += ",";
            r += _partes[i];
        }
        return r;
    }

    [Benchmark]
    public string ConStringBuilder()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < _partes.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(_partes[i]);
        }
        return sb.ToString();
    }

    [Benchmark]
    public string ConJoin() => string.Join(',', _partes);
}
