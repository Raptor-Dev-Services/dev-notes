using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bench;

[MemoryDiagnoser]
public class LeerArchivo
{
    private string _ruta = "";

    [GlobalSetup]
    public void Preparar()
    {
        _ruta = Path.Combine(Path.GetTempPath(), "lineas-a11.txt");
        File.WriteAllLines(_ruta, Enumerable.Range(0, 200_000).Select(i => $"linea {i}: 2026-11-03 INFO pedido {i * 7} procesado"));
    }

    [GlobalCleanup]
    public void Limpiar() => File.Delete(_ruta);

    [Benchmark(Baseline = true)]
    public int ReadAllLines() => File.ReadAllLines(_ruta).Length;

    [Benchmark]
    public int ReadLines()
    {
        int n = 0;
        foreach (string _ in File.ReadLines(_ruta)) n++;
        return n;
    }

    [Benchmark]
    public int StreamReaderBuffer64K()
    {
        int n = 0;
        using var lector = new StreamReader(_ruta, System.Text.Encoding.UTF8, true, 64 * 1024);
        while (lector.ReadLine() is not null) n++;
        return n;
    }
}

public class PedidoFila
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public double Total { get; set; }
    public string Fecha { get; set; } = "";
}

public class TiendaContext(string ruta) : DbContext
{
    public DbSet<PedidoFila> Pedidos => Set<PedidoFila>();
    protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite($"Data Source={ruta}");
    protected override void OnModelCreating(ModelBuilder b) =>
        b.Entity<PedidoFila>(e => { e.ToTable("pedidos"); e.HasKey(p => p.Id); e.Property(p => p.Id).HasColumnName("id"); e.Property(p => p.ClienteId).HasColumnName("cliente_id"); e.Property(p => p.Total).HasColumnName("total"); e.Property(p => p.Fecha).HasColumnName("fecha"); });
}

// SQLite en archivo local: sin red de por medio. Sirve para comparar costos de acceso a datos; no mide latencia de red.
[MemoryDiagnoser]
public class ConsultaPorCliente
{
    private string _ruta = "";

    [Params(false, true)]
    public bool Indice;

    [GlobalSetup]
    public void Preparar()
    {
        _ruta = Path.Combine(Path.GetTempPath(), $"tienda-a11-{(Indice ? "idx" : "sin")}.db");
        File.Delete(_ruta);
        using var cn = new SqliteConnection($"Data Source={_ruta}");
        cn.Open();
        cn.Execute("CREATE TABLE pedidos (id INTEGER PRIMARY KEY, cliente_id INTEGER NOT NULL, total REAL NOT NULL, fecha TEXT NOT NULL)");
        using var tx = cn.BeginTransaction();
        var azar = new Random(3);
        for (int i = 1; i <= 100_000; i++)
            cn.Execute("INSERT INTO pedidos (id, cliente_id, total, fecha) VALUES (@i, @c, @t, @f)",
                new { i, c = i % 1_000, t = azar.Next(1_000, 900_000) / 100.0, f = "2026-11-03" }, tx);
        tx.Commit();
        if (Indice) cn.Execute("CREATE INDEX ix_pedidos_cliente ON pedidos (cliente_id)");
    }

    [GlobalCleanup]
    public void Limpiar() { SqliteConnection.ClearAllPools(); File.Delete(_ruta); }

    [Benchmark(Baseline = true)]
    public int Dapper()
    {
        using var cn = new SqliteConnection($"Data Source={_ruta}");
        return cn.Query<PedidoFila>("SELECT id AS Id, cliente_id AS ClienteId, total AS Total, fecha AS Fecha FROM pedidos WHERE cliente_id = @c", new { c = 123 }).AsList().Count;
    }

    [Benchmark]
    public int EfCoreAsNoTracking()
    {
        using var db = new TiendaContext(_ruta);
        return db.Pedidos.AsNoTracking().Where(p => p.ClienteId == 123).ToList().Count;
    }

    [Benchmark]
    public int EfCoreConSeguimiento()
    {
        using var db = new TiendaContext(_ruta);
        return db.Pedidos.Where(p => p.ClienteId == 123).ToList().Count;
    }
}

[MemoryDiagnoser]
public class NMasUno
{
    private string _ruta = "";
    private readonly long[] _clientes = Enumerable.Range(0, 100).Select(i => (long)i).ToArray();

    [GlobalSetup]
    public void Preparar()
    {
        _ruta = Path.Combine(Path.GetTempPath(), "nmasuno-a11.db");
        File.Delete(_ruta);
        using var cn = new SqliteConnection($"Data Source={_ruta}");
        cn.Open();
        cn.Execute("CREATE TABLE pedidos (id INTEGER PRIMARY KEY, cliente_id INTEGER NOT NULL, total REAL NOT NULL, fecha TEXT NOT NULL)");
        using var tx = cn.BeginTransaction();
        for (int i = 1; i <= 100_000; i++)
            cn.Execute("INSERT INTO pedidos (id, cliente_id, total, fecha) VALUES (@i, @c, 10.5, '2026-11-03')", new { i, c = i % 1_000 }, tx);
        tx.Commit();
        cn.Execute("CREATE INDEX ix_pedidos_cliente ON pedidos (cliente_id)");
    }

    [GlobalCleanup]
    public void Limpiar() { SqliteConnection.ClearAllPools(); File.Delete(_ruta); }

    [Benchmark(Baseline = true)]
    public int UnaConsultaPorCliente()
    {
        using var cn = new SqliteConnection($"Data Source={_ruta}");
        int total = 0;
        foreach (long c in _clientes)
            total += cn.Query<long>("SELECT id FROM pedidos WHERE cliente_id = @c", new { c }).AsList().Count;
        return total;
    }

    [Benchmark]
    public int UnaSolaConsulta()
    {
        using var cn = new SqliteConnection($"Data Source={_ruta}");
        return cn.Query<long>("SELECT id FROM pedidos WHERE cliente_id IN @cs", new { cs = _clientes }).AsList().Count;
    }
}
