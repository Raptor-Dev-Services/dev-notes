using System.Diagnostics;

// Cliente de carga minimo para la API de ejemplo del curso (solo local).
// Uso: dotnet run -c Release --project Carga -- <url> <concurrencia> <segundos> [pausa-ms]
// En la url, {n} se reemplaza por un entero distinto en cada peticion.
if (args.Length < 3) { Console.WriteLine("Uso: Carga <url> <concurrencia> <segundos>"); return 1; }
string plantilla = args[0];
int concurrencia = int.Parse(args[1]);
int segundos = int.Parse(args[2]);
int pausaMs = args.Length > 3 ? int.Parse(args[3]) : 0;   // espera entre peticiones de cada cliente, para no saturar

using var http = new HttpClient(new SocketsHttpHandler { MaxConnectionsPerServer = concurrencia }) { Timeout = TimeSpan.FromSeconds(30) };
var fin = Stopwatch.GetTimestamp() + segundos * Stopwatch.Frequency;
int contador = 0, errores = 0;
var latencias = new List<double>[concurrencia];

var tareas = Enumerable.Range(0, concurrencia).Select(async k =>
{
    var mias = latencias[k] = new List<double>();
    while (Stopwatch.GetTimestamp() < fin)
    {
        string url = plantilla.Replace("{n}", Interlocked.Increment(ref contador).ToString());
        long t0 = Stopwatch.GetTimestamp();
        try
        {
            using var r = await http.GetAsync(url);
            if (!r.IsSuccessStatusCode) Interlocked.Increment(ref errores);
        }
        catch { Interlocked.Increment(ref errores); }
        mias.Add(Stopwatch.GetElapsedTime(t0).TotalMilliseconds);
        if (pausaMs > 0) await Task.Delay(pausaMs);
    }
}).ToArray();
await Task.WhenAll(tareas);

var todas = latencias.SelectMany(l => l).OrderBy(x => x).ToArray();
double P(double q) => todas.Length == 0 ? 0 : todas[Math.Min(todas.Length - 1, (int)(q * todas.Length))];
Console.WriteLine($"peticiones: {todas.Length}  errores: {errores}  concurrencia: {concurrencia}  duracion: {segundos} s");
Console.WriteLine($"peticiones/s: {todas.Length / (double)segundos:F0}");
Console.WriteLine($"latencia ms  p50: {P(0.50):F1}  p95: {P(0.95):F1}  p99: {P(0.99):F1}  max: {(todas.Length == 0 ? 0 : todas[^1]):F1}");
return 0;
