using System.Buffers;
using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace Bench;

[MemoryDiagnoser]
public class Busqueda
{
    private List<int> _lista = [];
    private HashSet<int> _conjunto = [];
    private int[] _ordenado = [];
    private int[] _consultas = [];

    [Params(8, 100, 10_000)]
    public int N;

    [GlobalSetup]
    public void Preparar()
    {
        _ordenado = Enumerable.Range(0, N).Select(i => i * 2).ToArray();   // pares: 0, 2, 4...
        _lista = [.. _ordenado];
        _conjunto = [.. _ordenado];
        var azar = new Random(1);
        _consultas = Enumerable.Range(0, 1_000).Select(_ => azar.Next(0, N * 2)).ToArray();   // la mitad existe
    }

    [Benchmark(Baseline = true)]
    public int ListContains()
    {
        int hallados = 0;
        foreach (int q in _consultas) if (_lista.Contains(q)) hallados++;
        return hallados;
    }

    [Benchmark]
    public int HashSetContains()
    {
        int hallados = 0;
        foreach (int q in _consultas) if (_conjunto.Contains(q)) hallados++;
        return hallados;
    }

    [Benchmark]
    public int BinarySearch()
    {
        int hallados = 0;
        foreach (int q in _consultas) if (Array.BinarySearch(_ordenado, q) >= 0) hallados++;
        return hallados;
    }
}

[MemoryDiagnoser]
public class LinqVsCiclo
{
    private int[] _datos = [];

    [Params(100, 10_000)]
    public int N;

    [GlobalSetup]
    public void Preparar() => _datos = Enumerable.Range(0, N).ToArray();

    [Benchmark(Baseline = true)]
    public int Linq() => _datos.Where(x => x % 2 == 0).Select(x => x * 2).Sum();

    [Benchmark]
    public int LinqConCaptura()
    {
        int divisor = 2;   // la lambda captura una variable local: el compilador crea un objeto de cierre
        return _datos.Where(x => x % divisor == 0).Select(x => x * divisor).Sum();
    }

    [Benchmark]
    public int Ciclo()
    {
        int suma = 0;
        foreach (int x in _datos)
            if (x % 2 == 0) suma += x * 2;
        return suma;
    }
}

[MemoryDiagnoser]
public class ParsearLineas
{
    private string[] _lineas = [];

    [GlobalSetup]
    public void Preparar()
    {
        _lineas = Enumerable.Range(1, 1_000)
            .Select(i => string.Create(CultureInfo.InvariantCulture, $"{1000 + i},Cliente numero {i},{i * 3.25m},2026-11-03"))
            .ToArray();
        if (ConSplit() != ConSpan()) throw new InvalidOperationException("No coinciden.");
    }

    [Benchmark(Baseline = true)]
    public decimal ConSplit()
    {
        decimal total = 0;
        foreach (string linea in _lineas)
        {
            string[] campos = linea.Split(',');
            total += int.Parse(campos[0], CultureInfo.InvariantCulture) + decimal.Parse(campos[2], CultureInfo.InvariantCulture);
        }
        return total;
    }

    [Benchmark]
    public decimal ConSpan()
    {
        decimal total = 0;
        foreach (string linea in _lineas)
        {
            ReadOnlySpan<char> resto = linea;
            int coma = resto.IndexOf(',');
            int id = int.Parse(resto[..coma], CultureInfo.InvariantCulture);
            resto = resto[(coma + 1)..];
            coma = resto.IndexOf(',');
            resto = resto[(coma + 1)..];
            coma = resto.IndexOf(',');
            total += id + decimal.Parse(resto[..coma], CultureInfo.InvariantCulture);
        }
        return total;
    }
}

[MemoryDiagnoser]
public class Buffers
{
    [Params(4_096, 100_000)]
    public int Tamano;

    [Benchmark(Baseline = true)]
    public int ArregloNuevo()
    {
        var b = new byte[Tamano];
        b[0] = 1;
        b[Tamano - 1] = 2;
        return b[0] + b[Tamano - 1];
    }

    [Benchmark]
    public int ArrayPoolRentar()
    {
        byte[] b = ArrayPool<byte>.Shared.Rent(Tamano);
        try
        {
            b[0] = 1;
            b[Tamano - 1] = 2;
            return b[0] + b[Tamano - 1];
        }
        finally { ArrayPool<byte>.Shared.Return(b); }
    }
}
