using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Bench;

public record Pedido(int Id, string Cliente, decimal Total, DateOnly Fecha);

// Modulo 2: el costo en memoria de generar un reporte CSV, en tres versiones.
[MemoryDiagnoser]
public class ReporteCsv
{
    private Pedido[] _pedidos = [];

    [Params(100, 1_000)]
    public int Pedidos;

    [GlobalSetup]
    public void Preparar()
    {
        var azar = new Random(7);
        _pedidos = Enumerable.Range(1, Pedidos)
            .Select(i => new Pedido(i, "Cliente " + azar.Next(1, 500), azar.Next(1_000, 900_000) / 100m, new DateOnly(2026, 1, 1).AddDays(azar.Next(0, 300))))
            .ToArray();
        var a = Concatenando();
        if (a != ConStringBuilder() || a != ConCapacidad())
            throw new InvalidOperationException("Las versiones no coinciden.");
    }

    [Benchmark(Baseline = true)]
    public string Concatenando()
    {
        string resultado = "";
        foreach (var p in _pedidos)
            resultado += string.Create(CultureInfo.InvariantCulture, $"{p.Id},{p.Cliente},{p.Total},{p.Fecha:yyyy-MM-dd}\n");
        return resultado;
    }

    [Benchmark]
    public string ConStringBuilder()
    {
        var sb = new StringBuilder();
        foreach (var p in _pedidos)
            sb.Append(CultureInfo.InvariantCulture, $"{p.Id},{p.Cliente},{p.Total},{p.Fecha:yyyy-MM-dd}\n");
        return sb.ToString();
    }

    [Benchmark]
    public string ConCapacidad()
    {
        var sb = new StringBuilder(_pedidos.Length * 40);
        foreach (var p in _pedidos)
            sb.Append(CultureInfo.InvariantCulture, $"{p.Id},{p.Cliente},{p.Total},{p.Fecha:yyyy-MM-dd}\n");
        return sb.ToString();
    }
}

public class PuntoClase { public int X, Y; public PuntoClase(int x, int y) { X = x; Y = y; } }
public struct PuntoStruct { public int X, Y; public PuntoStruct(int x, int y) { X = x; Y = y; } }

[MemoryDiagnoser]
public class BoxingYValores
{
    private const int N = 10_000;

    [Benchmark(Baseline = true)]
    public long ListaDeObject()
    {
        var lista = new List<object>(N);
        for (int i = 0; i < N; i++) lista.Add(i);       // boxing: cada int se envuelve en un objeto
        long suma = 0;
        foreach (object o in lista) suma += (int)o;     // unboxing
        return suma;
    }

    [Benchmark]
    public long ListaDeInt()
    {
        var lista = new List<int>(N);
        for (int i = 0; i < N; i++) lista.Add(i);
        long suma = 0;
        foreach (int v in lista) suma += v;
        return suma;
    }

    [Benchmark]
    public long ArregloDeClases()
    {
        var a = new PuntoClase[N];
        for (int i = 0; i < N; i++) a[i] = new PuntoClase(i, i);
        long suma = 0;
        foreach (var p in a) suma += p.X + p.Y;
        return suma;
    }

    [Benchmark]
    public long ArregloDeStructs()
    {
        var a = new PuntoStruct[N];
        for (int i = 0; i < N; i++) a[i] = new PuntoStruct(i, i);
        long suma = 0;
        foreach (var p in a) suma += p.X + p.Y;
        return suma;
    }
}
