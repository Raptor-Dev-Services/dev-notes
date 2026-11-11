using System.Diagnostics;
using System.Net.Sockets;

// Demos de consola del curso. Uso: dotnet run -c Release --project Demos -- <demo> [argumentos]
string demo = args.Length > 0 ? args[0] : "ayuda";
switch (demo)
{
    case "jit": Jit(); break;
    case "gc": Gc(); break;
    case "loh": Loh(); break;
    case "race": Race(); break;
    case "http-nuevo": await Http(args[1], int.Parse(args[2]), compartido: false); break;
    case "http-compartido": await Http(args[1], int.Parse(args[2]), compartido: true); break;
    default: Console.WriteLine("Demos: jit | gc | loh | race | http-nuevo <url> <n> | http-compartido <url> <n>"); break;
}

// Modulo 1: medir una sola vez con Stopwatch da el costo del JIT, no del metodo.
static void Jit()
{
#if DEBUG
    const string config = "Debug";
#else
    const string config = "Release";
#endif
    Console.WriteLine($"Configuracion: {config}  |  Runtime: {Environment.Version}");
    for (int i = 1; i <= 8; i++)
    {
        var t = Stopwatch.StartNew();
        long r = SumarCuadrados(100_000);
        t.Stop();
        Console.WriteLine($"llamada {i}: {t.Elapsed.TotalMicroseconds,9:F1} us  (resultado {r})");
    }
}

static long SumarCuadrados(int n)
{
    long s = 0;
    for (int i = 0; i < n; i++) s += (long)i * i;
    return s;
}

// Modulo 2: generaciones del recolector.
static void Gc()
{
    var viejo = new byte[1_000];
    Console.WriteLine($"recien creado:         generacion {GC.GetGeneration(viejo)}");
    GC.Collect(0);
    Console.WriteLine($"tras un GC de gen0:    generacion {GC.GetGeneration(viejo)}");
    GC.Collect(1);
    Console.WriteLine($"tras un GC de gen1:    generacion {GC.GetGeneration(viejo)}");

    int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
    long antes = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 1_000_000; i++) Estado.Destino = new byte[100];      // basura de vida corta; el campo evita que el JIT elimine la asignacion
    long despues = GC.GetAllocatedBytesForCurrentThread();
    Console.WriteLine($"1 000 000 de byte[100]: {(despues - antes) / 1_000_000.0:F1} MB asignados (medido con GetAllocatedBytesForCurrentThread)");
    Console.WriteLine($"colecciones durante el ciclo: gen0 +{GC.CollectionCount(0) - g0}, gen1 +{GC.CollectionCount(1) - g1}, gen2 +{GC.CollectionCount(2) - g2}");
    GC.KeepAlive(viejo);
}

// Modulo 2: donde empieza el Large Object Heap.
static void Loh()
{
    Console.WriteLine($"GC.GetGCMemoryInfo, modo de servidor: {System.Runtime.GCSettings.IsServerGC}, latencia: {System.Runtime.GCSettings.LatencyMode}");
    int ultimoPequeno = 0, primeroGrande = 0;
    for (int bytes = 84_000; bytes <= 86_000; bytes++)
    {
        var a = new byte[bytes];
        if (GC.GetGeneration(a) == 2) { primeroGrande = bytes; break; }
        ultimoPequeno = bytes;
    }
    Console.WriteLine($"byte[{ultimoPequeno}] nace en gen0; byte[{primeroGrande}] nace directo en gen2 (LOH)");
    Console.WriteLine($"byte[85000]: generacion {GC.GetGeneration(new byte[85_000])}");
    Console.WriteLine($"double[10600]: generacion {GC.GetGeneration(new double[10_600])}  |  double[10700]: generacion {GC.GetGeneration(new double[10_700])}");
}

// Modulo 4: la carrera de datos, para verla fallar.
static void Race()
{
    int sinProteger = 0, conInterlocked = 0;
    const int hilos = 4, vueltas = 1_000_000;
    // n++ son tres pasos: leer, sumar, escribir. Aqui se escriben a mano para que el JIT no los junte en un registro.
    Parallel.For(0, hilos, _ => { for (int i = 0; i < vueltas; i++) { int v = Volatile.Read(ref sinProteger); Volatile.Write(ref sinProteger, v + 1); } });
    Parallel.For(0, hilos, _ => { for (int i = 0; i < vueltas; i++) Interlocked.Increment(ref conInterlocked); });
    Console.WriteLine($"esperado: {hilos * vueltas:N0}");
    Console.WriteLine($"leer-sumar-escribir sin proteger:  {sinProteger:N0}");
    Console.WriteLine($"Interlocked:   {conInterlocked:N0}");
}

// Modulo 5: un HttpClient por peticion contra un HttpClient compartido (solo contra la API de ejemplo, local).
static async Task Http(string url, int n, bool compartido)
{
    var sw = Stopwatch.StartNew();
    HttpClient? unico = compartido ? new HttpClient() : null;
    for (int i = 0; i < n; i++)
    {
        if (compartido) { using var r = await unico!.GetAsync(url); r.EnsureSuccessStatusCode(); }
        else
        {
            using var c = new HttpClient();
            using var r = await c.GetAsync(url);
            r.EnsureSuccessStatusCode();
        }
    }
    sw.Stop();
    Console.WriteLine($"{(compartido ? "compartido" : "uno por peticion")}: {n} peticiones en {sw.Elapsed.TotalMilliseconds:F0} ms; sockets en TIME_WAIT: {ContarTimeWait()}");
}

// Cuenta los sockets TCP en estado 06 (TIME_WAIT) leyendo /proc/net/tcp (solo Linux).
static int ContarTimeWait() =>
    new[] { "/proc/net/tcp", "/proc/net/tcp6" }.Where(File.Exists)
        .Sum(f => File.ReadLines(f).Skip(1).Count(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)[3] == "06"));

// Campo estatico usado solo para que el JIT no pueda eliminar las asignaciones del demo.
static class Estado { public static byte[]? Destino; }
