using BenchmarkDotNet.Attributes;

namespace Bench;

[MemoryDiagnoser]
public class TaskVsValueTask
{
    private readonly Dictionary<int, int> _cache = Enumerable.Range(0, 10).ToDictionary(i => i, i => i * 1000 + 17);

    private async Task<int> ObtenerTask(int id)
    {
        if (_cache.TryGetValue(id, out int v)) return v;      // casi siempre termina sincrono
        await Task.Yield();
        return -1;
    }

    private async ValueTask<int> ObtenerValueTask(int id)
    {
        if (_cache.TryGetValue(id, out int v)) return v;
        await Task.Yield();
        return -1;
    }

    [Benchmark(Baseline = true)]
    public async Task<int> ConTask()
    {
        int suma = 0;
        for (int i = 0; i < 1_000; i++) suma += await ObtenerTask(i % 10);
        return suma;
    }

    [Benchmark]
    public async Task<int> ConValueTask()
    {
        int suma = 0;
        for (int i = 0; i < 1_000; i++) suma += await ObtenerValueTask(i % 10);
        return suma;
    }
}

public class ParaleloVsSecuencial
{
    [Params(1_000, 200_000)]
    public int Limite;

    private static bool EsPrimo(int n)
    {
        if (n < 2) return false;
        for (int d = 2; (long)d * d <= n; d++)
            if (n % d == 0) return false;
        return true;
    }

    [Benchmark(Baseline = true)]
    public int Secuencial() => Enumerable.Range(2, Limite).Count(EsPrimo);

    [Benchmark]
    public int Plinq() => Enumerable.Range(2, Limite).AsParallel().Count(EsPrimo);

    [Benchmark]
    public int ParallelFor()
    {
        int total = 0;
        Parallel.For(2, Limite + 2,
            () => 0,
            (n, _, local) => EsPrimo(n) ? local + 1 : local,
            local => Interlocked.Add(ref total, local));
        return total;
    }
}

public class Sincronizacion
{
    private int _n;
    private readonly object _candado = new();
    private const int Hilos = 4;
    private const int Vueltas = 250_000;

    [Benchmark(Baseline = true)]
    public int ConLock()
    {
        _n = 0;
        Parallel.For(0, Hilos, _ => { for (int i = 0; i < Vueltas; i++) lock (_candado) _n++; });
        return _n;
    }

    [Benchmark]
    public int ConInterlocked()
    {
        _n = 0;
        Parallel.For(0, Hilos, _ => { for (int i = 0; i < Vueltas; i++) Interlocked.Increment(ref _n); });
        return _n;
    }
}
