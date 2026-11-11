using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache(o => o.SizeLimit = 100);
var app = builder.Build();

var meter = new Meter("ApiEjemplo");
var peticiones = meter.CreateCounter<long>("ejemplo.peticiones", "peticion", "Peticiones atendidas por /pedido/{id}");
var fuga = new ConcurrentDictionary<int, byte[]>();                      // cache sin limite (el defecto sembrado)
meter.CreateObservableGauge("ejemplo.cache.entradas", () => fuga.Count, "entrada", "Entradas en la cache sin limite");
bool modoFuga = (Environment.GetEnvironmentVariable("CACHE") ?? "fuga") == "fuga";

app.MapGet("/ping", () => "ok");

// Modulo 4: el mismo trabajo (esperar ms milisegundos), bloqueando un hilo o liberandolo.
app.MapGet("/espera-bloqueante", (int ms) => { Task.Delay(ms).Wait(); return Results.Ok(); });
app.MapGet("/espera-async", async (int ms) => { await Task.Delay(ms); return Results.Ok(); });

// Modulo 4 y 6: estado del proceso, para mirarlo sin herramientas.
app.MapGet("/diag", () =>
{
    ThreadPool.GetMinThreads(out int minTrabajo, out _);
    return Results.Json(new
    {
        procesadores = Environment.ProcessorCount,
        hilosDelPool = ThreadPool.ThreadCount,
        minimoDeHilos = minTrabajo,
        trabajosPendientes = ThreadPool.PendingWorkItemCount,
        heapMB = GC.GetTotalMemory(false) / 1_000_000,
        gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2),
        entradasEnCache = fuga.Count,
    });
});

// Modulo 2: el costo en memoria de un endpoint, medido dentro del propio endpoint.
app.MapGet("/reporte", (int pedidos, int version, HttpContext http) =>
{
    var datos = Enumerable.Range(1, pedidos).Select(i => (Id: i, Cliente: "Cliente " + (i % 500), Total: i * 3.25m)).ToArray();
    long antes = GC.GetAllocatedBytesForCurrentThread();
    string csv;
    if (version == 1)
    {
        csv = "";
        foreach (var p in datos) csv += string.Create(CultureInfo.InvariantCulture, $"{p.Id},{p.Cliente},{p.Total}\n");
    }
    else
    {
        var sb = new StringBuilder(pedidos * 24);
        foreach (var p in datos) sb.Append(CultureInfo.InvariantCulture, $"{p.Id},{p.Cliente},{p.Total}\n");
        csv = sb.ToString();
    }
    long usados = GC.GetAllocatedBytesForCurrentThread() - antes;
    http.Response.Headers["X-Bytes-Asignados"] = usados.ToString(CultureInfo.InvariantCulture);
    return Results.Text(csv, "text/csv");
});

// Modulo 6: un "documento" de ~20 KB por id. Con CACHE=fuga se guarda para siempre; con CACHE=acotada, en IMemoryCache con tope.
app.MapGet("/pedido/{id:int}", (int id, IMemoryCache cache) =>
{
    peticiones.Add(1);
    byte[] doc;
    if (modoFuga)
        doc = fuga.GetOrAdd(id, _ => new byte[20_000]);
    else
        doc = cache.GetOrCreate(id, e => { e.Size = 1; e.SlidingExpiration = TimeSpan.FromMinutes(5); return new byte[20_000]; })!;
    return Results.Ok(new { id, bytes = doc.Length });
});

app.Run();
