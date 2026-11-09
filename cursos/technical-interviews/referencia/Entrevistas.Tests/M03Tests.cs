using Entrevistas.M03;

namespace Entrevistas.Tests;

public class BusquedaTests
{
    [Theory]
    [InlineData(new int[0], 5, -1)]
    [InlineData(new[] { 5 }, 5, 0)]
    [InlineData(new[] { 5 }, 4, -1)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 1, 0)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 9, 4)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 7, 3)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 4, -1)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 10, -1)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 0, -1)]
    public void Binaria(int[] datos, int objetivo, int esperado)
        => Assert.Equal(esperado, Busqueda.Binaria(datos, objetivo));

    [Fact]
    public void Binaria_coincide_con_la_del_framework_en_arreglos_grandes()
    {
        int[] datos = Enumerable.Range(0, 10_000).Select(i => i * 3).ToArray();
        for (int x = -2; x < 30_005; x++)
        {
            int nuestra = Busqueda.Binaria(datos, x);
            int framework = Array.BinarySearch(datos, x);
            Assert.Equal(framework >= 0 ? framework : -1, nuestra);
        }
    }

    [Fact]
    public void Binaria_hace_a_lo_mas_log2_mas_uno_comparaciones()
    {
        int[] datos = Enumerable.Range(0, 1 << 20).ToArray();
        int evaluaciones = 0;
        int encontrado = Busqueda.PrimerVerdadero(0, datos.Length, i => { evaluaciones++; return datos[i] >= 777_777; });
        Assert.Equal(777_777, encontrado);
        Assert.InRange(evaluaciones, 1, 21);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(0, 10, 7)]
    [InlineData(0, 10, 10)]
    [InlineData(5, 5, 5)]
    public void Primer_verdadero_con_umbral(int lo, int hi, int umbral)
        => Assert.Equal(Math.Max(lo, Math.Min(hi, umbral)), Busqueda.PrimerVerdadero(lo, hi, x => x >= umbral));

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 5, 15)]
    [InlineData(new[] { 3, 2, 2, 4, 1, 4 }, 3, 6)]
    [InlineData(new[] { 1, 2, 3, 1, 1 }, 4, 3)]
    [InlineData(new[] { 7 }, 1, 7)]
    [InlineData(new[] { 4, 4, 4 }, 1, 12)]
    [InlineData(new[] { 4, 4, 4 }, 3, 4)]
    [InlineData(new[] { 4, 4, 4 }, 10, 4)]
    public void Capacidad_minima(int[] pesos, int dias, int esperado)
        => Assert.Equal(esperado, Busqueda.CapacidadMinima(pesos, dias));

    [Fact]
    public void Capacidad_minima_coincide_con_fuerza_bruta()
    {
        var azar = new Random(11);
        for (int prueba = 0; prueba < 200; prueba++)
        {
            int[] pesos = Enumerable.Range(0, azar.Next(1, 12)).Select(_ => azar.Next(1, 20)).ToArray();
            int dias = azar.Next(1, pesos.Length + 2);
            int bruta = Enumerable.Range(pesos.Max(), pesos.Sum()).First(c => Viajes(pesos, c) <= dias);
            Assert.Equal(bruta, Busqueda.CapacidadMinima(pesos, dias));
        }

        static int Viajes(int[] pesos, int capacidad)
        {
            int viajes = 1, carga = 0;
            foreach (int p in pesos)
            {
                if (carga + p > capacidad) { viajes++; carga = 0; }
                carga += p;
            }
            return viajes;
        }
    }

    [Fact]
    public void Traslapes()
    {
        Assert.False(Reuniones.HayTraslape([]));
        Assert.False(Reuniones.HayTraslape([new(9, 10)]));
        Assert.False(Reuniones.HayTraslape([new(10, 11), new(9, 10)])); // se tocan, no chocan
        Assert.True(Reuniones.HayTraslape([new(13, 15), new(9, 10), new(14, 16)]));
        Assert.True(Reuniones.HayTraslape([new(9, 12), new(10, 11)])); // una dentro de otra
    }
}

public class ArbolesTests
{
    //        8
    //      /   \
    //     3     10
    //    / \      \
    //   1   6      14
    //      / \    /
    //     4   7  13
    private static Nodo Ejemplo() =>
        new(8,
            new(3, new(1), new(6, new(4), new(7))),
            new(10, null, new(14, new(13))));

    [Fact]
    public void Recorridos_en_profundidad()
    {
        var pre = new List<int>(); Recorridos.Preorden(Ejemplo(), pre);
        var ino = new List<int>(); Recorridos.Inorden(Ejemplo(), ino);
        var post = new List<int>(); Recorridos.Postorden(Ejemplo(), post);
        Assert.Equal([8, 3, 1, 6, 4, 7, 10, 14, 13], pre);
        Assert.Equal([1, 3, 4, 6, 7, 8, 10, 13, 14], ino);
        Assert.Equal([1, 4, 7, 6, 3, 13, 14, 10, 8], post);
    }

    [Fact]
    public void Recorridos_de_arbol_vacio()
    {
        var salida = new List<int>();
        Recorridos.Inorden(null, salida);
        Assert.Empty(salida);
        Assert.Empty(Recorridos.PorNiveles(null));
    }

    [Fact]
    public void Por_niveles()
    {
        var niveles = Recorridos.PorNiveles(Ejemplo());
        Assert.Equal(4, niveles.Count);
        Assert.Equal([8], niveles[0]);
        Assert.Equal([3, 10], niveles[1]);
        Assert.Equal([1, 6, 14], niveles[2]);
        Assert.Equal([4, 7, 13], niveles[3]);
        Assert.Single(Recorridos.PorNiveles(new Nodo(1)));
    }

    [Fact]
    public void Altura()
    {
        Assert.Equal(0, Arboles.Altura(null));
        Assert.Equal(1, Arboles.Altura(new Nodo(1)));
        Assert.Equal(4, Arboles.Altura(Ejemplo()));
    }

    [Fact]
    public void Es_bst()
    {
        Assert.True(Arboles.EsBst(null));
        Assert.True(Arboles.EsBst(new Nodo(int.MinValue)));
        Assert.True(Arboles.EsBst(new Nodo(int.MaxValue, new Nodo(int.MinValue))));
        Assert.True(Arboles.EsBst(Ejemplo()));
        // Cada padre con su hijo está bien, pero el 6 está a la derecha del 8 y debería ser mayor que 8.
        Assert.False(Arboles.EsBst(new Nodo(8, new Nodo(3), new Nodo(10, new Nodo(6)))));
        Assert.False(Arboles.EsBst(new Nodo(5, new Nodo(5))));   // repetidos no
        Assert.False(Arboles.EsBst(new Nodo(5, null, new Nodo(5))));
    }

    [Fact]
    public void Contiene()
    {
        foreach (int x in new[] { 1, 3, 4, 6, 7, 8, 10, 13, 14 })
            Assert.True(Arboles.Contiene(Ejemplo(), x));
        foreach (int x in new[] { 0, 2, 5, 9, 11, 12, 15 })
            Assert.False(Arboles.Contiene(Ejemplo(), x));
        Assert.False(Arboles.Contiene(null, 1));
    }
}

public class GrafosTests
{
    private static Dictionary<string, List<string>> Red() => Grafos.NoDirigido(
    [
        ("Centro", "Norte"), ("Centro", "Sur"), ("Norte", "Aeropuerto"),
        ("Sur", "Puerto"), ("Puerto", "Aeropuerto"), ("Isla", "Faro"),
    ]);

    [Theory]
    [InlineData("Centro", "Centro", 0)]
    [InlineData("Centro", "Norte", 1)]
    [InlineData("Centro", "Aeropuerto", 2)]
    [InlineData("Sur", "Norte", 2)]
    [InlineData("Puerto", "Norte", 2)]
    [InlineData("Centro", "Faro", -1)]
    [InlineData("Centro", "NoExiste", -1)]
    [InlineData("NoExiste", "Centro", -1)]
    public void Camino_mas_corto(string origen, string destino, int esperado)
        => Assert.Equal(esperado, Grafos.CaminoMasCorto(Red(), origen, destino));

    [Fact]
    public void Alcanzables()
    {
        Assert.Equal(new HashSet<string> { "Centro", "Norte", "Sur", "Aeropuerto", "Puerto" }, Grafos.Alcanzables(Red(), "Centro"));
        Assert.Equal(new HashSet<string> { "Isla", "Faro" }, Grafos.Alcanzables(Red(), "Faro"));
        Assert.Equal(new HashSet<string> { "Solo" }, Grafos.Alcanzables(Red(), "Solo"));
    }

    [Fact]
    public void Orden_topologico_respeta_cada_dependencia()
    {
        (int, int)[] deps = [(0, 2), (1, 2), (2, 3), (1, 4), (4, 3)];
        var orden = Grafos.OrdenTopologico(5, deps);
        Assert.NotNull(orden);
        Assert.Equal(5, orden.Length);
        var posicion = orden.Select((tarea, i) => (tarea, i)).ToDictionary(p => p.tarea, p => p.i);
        foreach (var (antes, despues) in deps)
            Assert.True(posicion[antes] < posicion[despues]);
    }

    [Fact]
    public void Orden_topologico_casos_borde()
    {
        Assert.Empty(Grafos.OrdenTopologico(0, [])!);
        Assert.Equal(new[] { 0, 1, 2 }, Grafos.OrdenTopologico(3, []));
        Assert.Null(Grafos.OrdenTopologico(3, [(0, 1), (1, 2), (2, 0)])); // ciclo
        Assert.Null(Grafos.OrdenTopologico(1, [(0, 0)]));                  // se requiere a sí misma
    }

    [Fact]
    public void Islas()
    {
        bool T = true, F = false;
        var mapa = new bool[,]
        {
            { T, T, F, F, F },
            { T, F, F, T, F },
            { F, F, T, T, F },
            { F, F, F, F, F },
            { T, F, T, F, T },
        };
        Assert.Equal(5, Cuadricula.ContarIslas(mapa));
        Assert.True(mapa[0, 0]); // no modifica la entrada
        Assert.Equal(0, Cuadricula.ContarIslas(new bool[0, 0]));
        Assert.Equal(0, Cuadricula.ContarIslas(new bool[3, 3]));
        Assert.Equal(1, Cuadricula.ContarIslas(new bool[,] { { T, T }, { T, T } }));
        Assert.Equal(2, Cuadricula.ContarIslas(new bool[,] { { T, F }, { F, T } })); // en diagonal no se unen
    }

    [Fact]
    public void Isla_enorme_no_desborda_la_pila()
    {
        var mapa = new bool[1000, 1000];
        for (int f = 0; f < 1000; f++)
            for (int c = 0; c < 1000; c++)
                mapa[f, c] = true;
        Assert.Equal(1, Cuadricula.ContarIslas(mapa));
    }

    [Fact]
    public void Dijkstra_toma_el_camino_barato_aunque_tenga_mas_aristas()
    {
        var grafo = new Dictionary<int, List<(int Destino, int Peso)>>
        {
            [0] = [(1, 10), (2, 1)],
            [2] = [(3, 1)],
            [3] = [(1, 1)],
            [1] = [(4, 2)],
        };
        var d = Dijkstra.Distancias(grafo, 0);
        Assert.Equal(0, d[0]);
        Assert.Equal(3, d[1]); // 0-2-3-1 cuesta 3; la arista directa cuesta 10
        Assert.Equal(1, d[2]);
        Assert.Equal(2, d[3]);
        Assert.Equal(5, d[4]);
        Assert.False(Dijkstra.Distancias(grafo, 4).ContainsKey(0)); // inalcanzable
    }

    [Fact]
    public void Dijkstra_coincide_con_bellman_ford_en_grafos_aleatorios()
    {
        var azar = new Random(5);
        for (int prueba = 0; prueba < 100; prueba++)
        {
            int n = azar.Next(1, 9);
            var aristas = Enumerable.Range(0, azar.Next(0, 20))
                .Select(_ => (De: azar.Next(n), A: azar.Next(n), Peso: azar.Next(0, 10))).ToList();
            var grafo = aristas.GroupBy(a => a.De)
                .ToDictionary(g => g.Key, g => g.Select(a => (a.A, a.Peso)).ToList());

            var esperado = new long?[n];
            esperado[0] = 0;
            for (int vuelta = 0; vuelta < n; vuelta++)
                foreach (var (de, a, peso) in aristas)
                    if (esperado[de] is long dd && (esperado[a] is null || dd + peso < esperado[a]))
                        esperado[a] = dd + peso;

            var real = Dijkstra.Distancias(grafo, 0);
            for (int v = 0; v < n; v++)
                Assert.Equal(esperado[v], real.TryGetValue(v, out long x) ? x : null);
        }
    }
}
