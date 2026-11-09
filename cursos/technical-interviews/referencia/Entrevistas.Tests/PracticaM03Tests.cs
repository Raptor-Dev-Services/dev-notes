using Entrevistas.M03;
using Entrevistas.M03.Practica;

namespace Entrevistas.Tests;

public class PracticaM03Tests
{
    [Theory]
    [InlineData(new int[0], 3, -1)]
    [InlineData(new[] { 3 }, 3, 0)]
    [InlineData(new[] { 3 }, 4, -1)]
    [InlineData(new[] { 1, 2, 2, 2, 5 }, 2, 1)]
    [InlineData(new[] { 2, 2, 2 }, 2, 0)]
    [InlineData(new[] { 1, 2, 2, 2, 5 }, 5, 4)]
    [InlineData(new[] { 1, 2, 2, 2, 5 }, 3, -1)]
    [InlineData(new[] { 1, 2, 2, 2, 5 }, 0, -1)]
    [InlineData(new[] { 1, 2, 2, 2, 5 }, 9, -1)]
    public void P1_primera_aparicion(int[] datos, int objetivo, int esperado)
        => Assert.Equal(esperado, Problema1.PrimeraAparicion(datos, objetivo));

    [Fact]
    public void P1_coincide_con_IndexOf_al_azar()
    {
        var azar = new Random(12);
        for (int prueba = 0; prueba < 300; prueba++)
        {
            int[] datos = Enumerable.Range(0, azar.Next(0, 30)).Select(_ => azar.Next(0, 10)).Order().ToArray();
            int objetivo = azar.Next(-1, 11);
            Assert.Equal(Array.IndexOf(datos, objetivo), Problema1.PrimeraAparicion(datos, objetivo));
        }
    }

    [Fact]
    public void P2_profundidad_minima()
    {
        Assert.Equal(0, Problema2.ProfundidadMinima(null));
        Assert.Equal(1, Problema2.ProfundidadMinima(new Nodo(1)));
        // 3 con hijos 9 (hoja) y 20 (con dos hijos): la hoja más cercana es el 9.
        Assert.Equal(2, Problema2.ProfundidadMinima(new Nodo(3, new Nodo(9), new Nodo(20, new Nodo(15), new Nodo(7)))));
        // Una sola rama: un nodo con un hijo NO es hoja, hay que bajar hasta el final.
        Assert.Equal(4, Problema2.ProfundidadMinima(new Nodo(1, null, new Nodo(2, null, new Nodo(3, null, new Nodo(4))))));
    }

    //        6
    //      /   \
    //     2     8
    //    / \   / \
    //   0   4 7   9
    //      / \
    //     3   5
    private static Nodo Bst() => new(6,
        new(2, new(0), new(4, new(3), new(5))),
        new(8, new(7), new(9)));

    [Theory]
    [InlineData(2, 8, 6)]
    [InlineData(2, 4, 2)]
    [InlineData(3, 5, 4)]
    [InlineData(0, 5, 2)]
    [InlineData(7, 9, 8)]
    [InlineData(5, 5, 5)]
    [InlineData(9, 0, 6)]
    public void P3_ancestro_comun(int a, int b, int esperado)
        => Assert.Equal(esperado, Problema3.AncestroComun(Bst(), a, b));

    [Fact]
    public void P3_arbol_vacio() => Assert.Null(Problema3.AncestroComun(null, 1, 2));

    [Fact]
    public void P4_grupos()
    {
        Assert.Equal(0, Problema4.ContarGrupos(0, []));
        Assert.Equal(3, Problema4.ContarGrupos(3, []));
        Assert.Equal(2, Problema4.ContarGrupos(5, [(0, 1), (1, 2), (3, 4)]));
        Assert.Equal(1, Problema4.ContarGrupos(4, [(0, 1), (2, 3), (1, 2)]));
        Assert.Equal(2, Problema4.ContarGrupos(3, [(0, 0), (1, 2), (2, 1)])); // lazo y arista repetida
    }

    [Fact]
    public void P4_coincide_con_union_find_al_azar()
    {
        var azar = new Random(13);
        for (int prueba = 0; prueba < 200; prueba++)
        {
            int n = azar.Next(0, 15);
            var aristas = n == 0 ? [] : Enumerable.Range(0, azar.Next(0, 15)).Select(_ => (azar.Next(n), azar.Next(n))).ToList();
            var padre = Enumerable.Range(0, n).ToArray();
            int Raiz(int x) => padre[x] == x ? x : padre[x] = Raiz(padre[x]);
            foreach (var (a, b) in aristas) padre[Raiz(a)] = Raiz(b);
            int esperado = Enumerable.Range(0, n).Count(x => Raiz(x) == x);
            Assert.Equal(esperado, Problema4.ContarGrupos(n, aristas));
        }
    }

    [Fact]
    public void P5_pasos_minimos()
    {
        bool L = true, X = false;
        Assert.Equal(0, Problema5.PasosMinimos(new bool[,] { { L } }));
        Assert.Equal(-1, Problema5.PasosMinimos(new bool[,] { { X } }));
        Assert.Equal(-1, Problema5.PasosMinimos(new bool[0, 0]));
        Assert.Equal(2, Problema5.PasosMinimos(new bool[,] { { L, L }, { L, L } }));
        Assert.Equal(-1, Problema5.PasosMinimos(new bool[,] { { L, X }, { X, L } }));   // en diagonal no se pasa
        Assert.Equal(-1, Problema5.PasosMinimos(new bool[,] { { L, L }, { L, X } }));   // destino bloqueado
        var almacen = new bool[,]
        {
            { L, L, L, L },
            { X, X, X, L },
            { L, L, L, L },
            { L, X, X, X },
            { L, L, L, L },
        };
        Assert.Equal(13, Problema5.PasosMinimos(almacen)); // el pasillo obliga a zigzaguear
    }

    [Fact]
    public void P6_ruta_critica()
    {
        Assert.Equal(0, Problema6.DuracionMinima([], []));
        Assert.Equal(5, Problema6.DuracionMinima([5], []));
        Assert.Equal(7, Problema6.DuracionMinima([3, 7, 2], []));                     // todas en paralelo
        Assert.Equal(12, Problema6.DuracionMinima([3, 7, 2], [(0, 1), (1, 2)]));      // en cadena
        // 0 (2 días) y 1 (4 días) en paralelo; 2 (3 días) espera a las dos; 3 (1 día) espera a 0.
        Assert.Equal(7, Problema6.DuracionMinima([2, 4, 3, 1], [(0, 2), (1, 2), (0, 3)]));
        Assert.Equal(-1, Problema6.DuracionMinima([1, 1], [(0, 1), (1, 0)]));          // ciclo
    }

    [Fact]
    public void P6_coincide_con_recursion_con_memoria_al_azar()
    {
        var azar = new Random(14);
        for (int prueba = 0; prueba < 200; prueba++)
        {
            int n = azar.Next(0, 10);
            int[] dur = Enumerable.Range(0, n).Select(_ => azar.Next(0, 9)).ToArray();
            // Solo aristas de menor a mayor índice: así nunca hay ciclo.
            var deps = new List<(int, int)>();
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (azar.Next(4) == 0) deps.Add((i, j));

            var memo = new Dictionary<int, int>();
            int Fin(int t)
            {
                if (memo.TryGetValue(t, out int f)) return f;
                int inicio = deps.Where(d => d.Item2 == t).Select(d => Fin(d.Item1)).DefaultIfEmpty(0).Max();
                return memo[t] = inicio + dur[t];
            }
            int esperado = Enumerable.Range(0, n).Select(Fin).DefaultIfEmpty(0).Max();
            Assert.Equal(esperado, Problema6.DuracionMinima(dur, deps));
        }
    }
}
