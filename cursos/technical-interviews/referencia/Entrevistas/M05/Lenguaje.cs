#pragma warning disable CA2200 // P18 muestra a propósito lo que pasa con "throw ex"
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Entrevistas.M05;

// P01. Tipos por valor y por referencia.
public struct PuntoValor { public int X; }
public sealed class PuntoReferencia { public int X; }

public static class P01
{
    public static (int Original, int Copia) ConStruct()
    {
        var a = new PuntoValor { X = 1 };
        var b = a;        // copia el valor completo
        b.X = 99;
        return (a.X, b.X); // (1, 99)
    }

    public static (int Original, int Copia) ConClase()
    {
        var a = new PuntoReferencia { X = 1 };
        var b = a;        // copia la referencia: los dos apuntan al mismo objeto
        b.X = 99;
        return (a.X, b.X); // (99, 99)
    }
}

// P02. Records: igualdad por valor y copia con "with".
public sealed record Dinero(string Moneda, decimal Monto);

public static class P02
{
    public static (bool Iguales, bool MismaInstancia, Dinero Copia) Comparar()
    {
        var a = new Dinero("MXN", 100m);
        var b = new Dinero("MXN", 100m);
        var c = a with { Monto = 250m }; // a no cambia: c es otro objeto
        return (a == b, ReferenceEquals(a, b), c); // (true, false, Dinero { MXN, 250 })
    }
}

// P03. == entre strings compara el contenido, no la referencia.
public static class P03
{
    public static (bool Igual, bool MismaReferencia) Comparar()
    {
        string a = "hola";
        string b = new string(['h', 'o', 'l', 'a']); // otra instancia con el mismo texto
        return (a == b, ReferenceEquals(a, b)); // (true, false)
    }
}

// P04. string es inmutable: concatenar en un ciclo copia todo cada vez. StringBuilder no.
public static class P04
{
    // O(n²) caracteres copiados: cada += crea un string nuevo con todo lo anterior.
    public static string UnirConcatenando(int n)
    {
        string s = "";
        for (int i = 0; i < n; i++)
            s += i % 10;
        return s;
    }

    // O(n): con la capacidad reservada desde el inicio, cada Append escribe al final sin copiar lo anterior.
    public static string UnirConBuilder(int n)
    {
        var sb = new StringBuilder(n);
        for (int i = 0; i < n; i++)
            sb.Append(i % 10);
        return sb.ToString();
    }
}

// P05. await secuencial frente a Task.WhenAll: cuántas operaciones corren al mismo tiempo.
public sealed class Medidor
{
    private int _enCurso;
    private int _maximo;
    public int Maximo => _maximo;

    public async Task ConsultarAsync()
    {
        int ahora = Interlocked.Increment(ref _enCurso);
        int visto;
        while ((visto = Volatile.Read(ref _maximo)) < ahora)
            Interlocked.CompareExchange(ref _maximo, ahora, visto);
        await Task.Delay(100);               // simula una consulta a la base o a otra API
        Interlocked.Decrement(ref _enCurso);
    }
}

public static class P05
{
    public static async Task<int> UnaTrasOtraAsync(Medidor m)
    {
        await m.ConsultarAsync();            // espera a que termine antes de arrancar la siguiente
        await m.ConsultarAsync();
        await m.ConsultarAsync();
        return m.Maximo;                     // 1: nunca hubo dos a la vez
    }

    public static async Task<int> JuntasAsync(Medidor m)
    {
        await Task.WhenAll(m.ConsultarAsync(), m.ConsultarAsync(), m.ConsultarAsync());
        return m.Maximo;                     // 3: las tres esperaron al mismo tiempo
    }
}

// P06 y P07. Excepciones en código asíncrono.
public static class P06
{
    public static async Task<int> CalcularAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("falló el cálculo");
    }

    // Con await la excepción llega tal cual y se atrapa con un catch normal.
    public static async Task<string> ConAwaitAsync()
    {
        try
        {
            await CalcularAsync();
            return "sin error";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }
}

public static class P07
{
    // .Result bloquea el hilo y envuelve la excepción en AggregateException.
    public static Exception? ConResult()
    {
        try { _ = P06.CalcularAsync().Result; return null; }
        catch (Exception ex) { return ex; }  // AggregateException con la original en InnerException
    }
}

// P08. Cancelar una operación con CancellationToken.
public static class P08
{
    public static async Task EsperarAsync(TimeSpan cuanto, CancellationToken ct)
        => await Task.Delay(cuanto, ct); // si ct se cancela, lanza TaskCanceledException
}

// P09 y P10. Tiempos de vida en el contenedor de dependencias.
public sealed class ServicioSingleton { public Guid Id { get; } = Guid.NewGuid(); }
public sealed class ServicioScoped { public Guid Id { get; } = Guid.NewGuid(); }
public sealed class ServicioTransient { public Guid Id { get; } = Guid.NewGuid(); }

public static class P09
{
    public static ServiceProvider Construir()
    {
        var servicios = new ServiceCollection();
        servicios.AddSingleton<ServicioSingleton>(); // uno para toda la aplicación
        servicios.AddScoped<ServicioScoped>();       // uno por alcance (en ASP.NET Core, por petición)
        servicios.AddTransient<ServicioTransient>(); // uno nuevo cada vez que se pide
        return servicios.BuildServiceProvider();
    }
}

public sealed class Repositorio { }                    // scoped: vive lo que dura la petición
public sealed class CacheGlobal(Repositorio repo)       // singleton: vive toda la aplicación
{
    public Repositorio Repo { get; } = repo;
}

public static class P10
{
    // El singleton capturaría para siempre un servicio que debía morir con la petición.
    public static ServiceProvider ConValidacion()
    {
        var servicios = new ServiceCollection();
        servicios.AddScoped<Repositorio>();
        servicios.AddSingleton<CacheGlobal>();
        return servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}

// P11. Restricciones genéricas.
public static class P11
{
    public static T Mayor<T>(IEnumerable<T> valores) where T : IComparable<T>
    {
        using var e = valores.GetEnumerator();
        if (!e.MoveNext()) throw new InvalidOperationException("La secuencia está vacía.");
        T mayor = e.Current;
        while (e.MoveNext())
            if (e.Current.CompareTo(mayor) > 0)
                mayor = e.Current;
        return mayor;
    }
}

// P12. Covarianza y contravarianza.
public static class P12
{
    public static int Contar()
    {
        IEnumerable<object> objetos = new List<string> { "a", "b" }; // compila: IEnumerable<out T>
        // List<object> lista = new List<string>();                  // no compila: List<T> es invariante
        return objetos.Count();
    }

    public static string Contravarianza()
    {
        Action<object> imprimirCualquiera = o => _ = o.ToString();
        Action<string> imprimirTexto = imprimirCualquiera;           // compila: Action<in T>
        imprimirTexto("hola");
        return "ok";
    }
}

// P13. int? es otro tipo (Nullable<int>); string? es solo una anotación para el compilador.
public static class P13
{
    public static int Longitud(string? texto) => texto?.Length ?? 0;

    public static (bool IntEsNullable, Type TipoDelParametro) Revisar()
    {
        bool intEsNullable = typeof(int?) == typeof(Nullable<int>);
        Type parametro = typeof(P13).GetMethod(nameof(Longitud))!.GetParameters()[0].ParameterType;
        return (intEsNullable, parametro); // (true, typeof(string)): en ejecución string? es string
    }
}

// P14. El operador ! solo calla la advertencia: no evita el NullReferenceException.
public static class P14
{
    public static int LongitudForzada(string? texto) => texto!.Length;
}

// P15. Ejecución diferida en LINQ.
public static class P15
{
    public static int Contar()
    {
        var numeros = new List<int> { 1, 2, 3 };
        var mayoresQueUno = numeros.Where(n => n > 1); // todavía no se ejecuta nada
        numeros.Add(4);
        return mayoresQueUno.Count();                  // se ejecuta aquí: 2, 3 y 4
    }
}

// P16. IQueryable guarda un árbol de expresión que un proveedor (EF Core) traduce, por ejemplo, a SQL.
public static class P16
{
    public static (string Metodo, Type TipoDelFiltro) Inspeccionar()
    {
        IQueryable<int> consulta = new[] { 1, 2, 3 }.AsQueryable().Where(n => n > 1);
        var llamada = (MethodCallExpression)consulta.Expression;
        var filtro = (UnaryExpression)llamada.Arguments[1]; // la lambda, citada como datos
        return (llamada.Method.Name, filtro.Operand.Type);  // ("Where", Func<int, bool>)
    }
}

// P17. First, FirstOrDefault y Single.
public static class P17
{
    public static readonly int[] Datos = [5, 8, 8, 13];

    public static int Primero() => Datos.First(n => n > 6);                    // 8
    public static int PrimeroOCero() => Datos.FirstOrDefault(n => n > 100);    // 0: default(int)
    public static int Unico() => Datos.Single(n => n > 6);                     // lanza: hay tres
}

// P18. throw frente a throw ex.
public static class P18
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Fallar() => throw new InvalidOperationException("origen");

    public static string ConThrow()
    {
        try
        {
            try { Fallar(); }
            catch (InvalidOperationException) { throw; }      // conserva el rastro original
        }
        catch (InvalidOperationException ex) { return ex.StackTrace ?? ""; }
        return "";
    }

    public static string ConThrowEx()
    {
        try
        {
            try { Fallar(); }
            catch (InvalidOperationException ex) { throw ex; } // reinicia el rastro aquí
        }
        catch (InvalidOperationException ex) { return ex.StackTrace ?? ""; }
        return "";
    }
}

// P19. using garantiza Dispose aunque haya una excepción.
public sealed class Conexion : IDisposable
{
    public bool Cerrada { get; private set; }
    public void Dispose() => Cerrada = true;
}

public static class P19
{
    public static void UsarYFallar(Conexion conexion)
    {
        using (conexion)
        {
            throw new InvalidOperationException("algo salió mal");
        }
    }
}

// P20. Cierres (closures) dentro de ciclos.
public static class P20
{
    public static List<int> ConFor()
    {
        var acciones = new List<Func<int>>();
        for (int i = 0; i < 3; i++)
            acciones.Add(() => i);          // las tres capturan la MISMA variable i
        return acciones.Select(a => a()).ToList(); // [3, 3, 3]
    }

    public static List<int> ConForeach()
    {
        var acciones = new List<Func<int>>();
        foreach (int i in new[] { 0, 1, 2 })
            acciones.Add(() => i);          // desde C# 5 cada vuelta tiene su propia i
        return acciones.Select(a => a()).ToList(); // [0, 1, 2]
    }
}
