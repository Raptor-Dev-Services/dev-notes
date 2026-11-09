using System.Numerics;
using Entrevistas.M03;
using Entrevistas.M04;
using Entrevistas.M04.Practica;

namespace Entrevistas.Tests;

public class DivideYVencerasTests
{
    [Fact]
    public void Merge_sort_ordena_como_el_framework()
    {
        var azar = new Random(1);
        for (int prueba = 0; prueba < 200; prueba++)
        {
            int[] datos = Enumerable.Range(0, azar.Next(0, 60)).Select(_ => azar.Next(-50, 50)).ToArray();
            int[] copia = (int[])datos.Clone();
            int[] esperado = datos.Order().ToArray();
            Assert.Equal(esperado, DivideYVenceras.MergeSort(datos));
            Assert.Equal(copia, datos); // no modifica la entrada
        }
        Assert.Empty(DivideYVenceras.MergeSort([]));
        Assert.Equal(new[] { 4 }, DivideYVenceras.MergeSort([4]));
    }

    [Theory]
    [InlineData(2, 10, 1_000_000_007)]
    [InlineData(3, 0, 1_000_000_007)]
    [InlineData(0, 0, 1_000_000_007)]
    [InlineData(0, 5, 1_000_000_007)]
    [InlineData(123_456_789, 987_654_321, 1_000_000_007)]
    [InlineData(1_000_000_006, 1_000_000_006, 1_000_000_007)]
    [InlineData(7, 3, 1)]
    [InlineData(2_999_999_998, 2, 2_999_999_999)]
    public void Potencia_modular_coincide_con_BigInteger(long b, long e, long m)
        => Assert.Equal((long)BigInteger.ModPow(b, e, m), DivideYVenceras.PotenciaModular(b, e, m));
}

public class VorazTests
{
    [Fact]
    public void Maximo_de_reuniones()
    {
        Assert.Equal(0, Voraz.MaximoDeReuniones([]));
        Assert.Equal(1, Voraz.MaximoDeReuniones([new(9, 18)]));
        // La larga de 9 a 18 estorba; las tres cortas caben.
        Assert.Equal(3, Voraz.MaximoDeReuniones([new(9, 18), new(9, 10), new(10, 12), new(12, 13)]));
        Assert.Equal(2, Voraz.MaximoDeReuniones([new(1, 4), new(2, 3), new(3, 5)]));
    }

    [Fact]
    public void Maximo_de_reuniones_coincide_con_fuerza_bruta()
    {
        var azar = new Random(9);
        for (int prueba = 0; prueba < 200; prueba++)
        {
            var reuniones = Enumerable.Range(0, azar.Next(0, 10))
                .Select(_ => { int i = azar.Next(0, 20); return new Intervalo(i, i + azar.Next(1, 6)); }).ToList();
            int mejor = 0;
            for (int mascara = 0; mascara < 1 << reuniones.Count; mascara++)
            {
                // Por índice: dos reuniones iguales siguen siendo dos reuniones (un record compara por valor).
                var elegidas = Enumerable.Range(0, reuniones.Count).Where(k => (mascara & (1 << k)) != 0).ToList();
                bool compatibles = elegidas.All(x => elegidas.All(y => x == y
                    || reuniones[x].Fin <= reuniones[y].Inicio || reuniones[y].Fin <= reuniones[x].Inicio));
                if (compatibles) mejor = Math.Max(mejor, elegidas.Count);
            }
            Assert.Equal(mejor, Voraz.MaximoDeReuniones(reuniones));
        }
    }

    [Fact]
    public void Cambio_voraz_falla_con_monedas_1_3_4()
    {
        Assert.Equal(3, Voraz.CambioVoraz([1, 3, 4], 6));      // 4 + 1 + 1
        Assert.Equal(2, Dinamica.CambioMinimo([1, 3, 4], 6));  // 3 + 3
    }

    [Fact]
    public void Cambio_voraz_acierta_con_monedas_mexicanas()
    {
        int[] monedas = [1, 2, 5, 10, 20];
        for (int monto = 0; monto <= 200; monto++)
            Assert.Equal(Dinamica.CambioMinimo(monedas, monto), Voraz.CambioVoraz(monedas, monto));
    }

    [Fact]
    public void Cambio_voraz_imposible()
        => Assert.Equal(-1, Voraz.CambioVoraz([5, 10], 3));
}

public class DinamicaTests
{
    private static int _llamadas;

    private static long FibContando(int n)
    {
        _llamadas++;
        return n < 2 ? n : FibContando(n - 1) + FibContando(n - 2);
    }

    [Fact]
    public void Las_tres_versiones_de_fibonacci_coinciden()
    {
        for (int n = 0; n <= 25; n++)
        {
            long ingenuo = Dinamica.FibIngenuo(n);
            Assert.Equal(ingenuo, Dinamica.FibMemo(n));
            Assert.Equal(ingenuo, Dinamica.FibTabla(n));
        }
        Assert.Equal(12_586_269_025L, Dinamica.FibTabla(50));
        Assert.Equal(12_586_269_025L, Dinamica.FibMemo(50));
    }

    [Fact]
    public void Fibonacci_ingenuo_hace_un_numero_exponencial_de_llamadas()
    {
        // llamadas(n) = 2 F(n + 1) - 1: crece como F(n), que es exponencial.
        foreach (int n in new[] { 10, 20, 25 })
        {
            _llamadas = 0;
            FibContando(n);
            Assert.Equal(2 * Dinamica.FibTabla(n + 1) - 1, _llamadas);
        }
    }

    [Theory]
    [InlineData(new[] { 1, 2, 5 }, 11, 3)]
    [InlineData(new[] { 2 }, 3, -1)]
    [InlineData(new[] { 1 }, 0, 0)]
    [InlineData(new[] { 7 }, 0, 0)]
    [InlineData(new[] { 3, 7 }, 11, -1)]
    [InlineData(new[] { 3, 7 }, 13, 3)]
    [InlineData(new[] { 186, 419, 83, 408 }, 6249, 20)]
    public void Cambio_minimo(int[] monedas, int monto, int esperado)
        => Assert.Equal(esperado, Dinamica.CambioMinimo(monedas, monto));

    [Fact]
    public void Cambio_minimo_monto_negativo_lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Dinamica.CambioMinimo([1], -1));

    [Theory]
    [InlineData(new[] { -2, 1, -3, 4, -1, 2, 1, -5, 4 }, 6)]
    [InlineData(new[] { 5 }, 5)]
    [InlineData(new[] { -3, -1, -2 }, -1)]
    [InlineData(new[] { 1, 2, 3 }, 6)]
    public void Kadane(int[] datos, long esperado)
        => Assert.Equal(esperado, Dinamica.MaximaSumaSubarreglo(datos));

    [Fact]
    public void Kadane_coincide_con_fuerza_bruta_y_no_desborda()
    {
        var azar = new Random(4);
        for (int prueba = 0; prueba < 200; prueba++)
        {
            int[] d = Enumerable.Range(0, azar.Next(1, 25)).Select(_ => azar.Next(-10, 10)).ToArray();
            long bruta = long.MinValue;
            for (int i = 0; i < d.Length; i++)
            {
                long suma = 0;
                for (int j = i; j < d.Length; j++) { suma += d[j]; bruta = Math.Max(bruta, suma); }
            }
            Assert.Equal(bruta, Dinamica.MaximaSumaSubarreglo(d));
        }
        Assert.Equal(2L * int.MaxValue, Dinamica.MaximaSumaSubarreglo([int.MaxValue, int.MaxValue]));
        Assert.Throws<ArgumentException>(() => Dinamica.MaximaSumaSubarreglo([]));
    }
}

public class PracticaM04Tests
{
    [Fact]
    public void A_minimo_de_salas()
    {
        Assert.Equal(0, ProblemaA.MinimoDeSalas([]));
        Assert.Equal(1, ProblemaA.MinimoDeSalas([new(9, 10)]));
        Assert.Equal(1, ProblemaA.MinimoDeSalas([new(9, 10), new(10, 11), new(11, 12)]));
        Assert.Equal(2, ProblemaA.MinimoDeSalas([new(0, 30), new(5, 10), new(15, 20)]));
        Assert.Equal(3, ProblemaA.MinimoDeSalas([new(1, 5), new(2, 6), new(3, 7), new(6, 8)]));
    }

    [Fact]
    public void A_coincide_con_el_maximo_de_reuniones_simultaneas()
    {
        var azar = new Random(8);
        for (int prueba = 0; prueba < 300; prueba++)
        {
            var reuniones = Enumerable.Range(0, azar.Next(0, 15))
                .Select(_ => { int i = azar.Next(0, 30); return new Intervalo(i, i + azar.Next(1, 8)); }).ToList();
            int pico = 0;
            for (int t = 0; t < 40; t++)
                pico = Math.Max(pico, reuniones.Count(r => r.Inicio <= t && t < r.Fin));
            Assert.Equal(pico, ProblemaA.MinimoDeSalas(reuniones));
        }
    }

    [Theory]
    [InlineData(new int[0], 0)]
    [InlineData(new[] { 7 }, 1)]
    [InlineData(new[] { 10, 9, 2, 5, 3, 7, 101, 18 }, 4)]
    [InlineData(new[] { 0, 1, 0, 3, 2, 3 }, 4)]
    [InlineData(new[] { 7, 7, 7, 7 }, 1)]
    [InlineData(new[] { 5, 4, 3, 2, 1 }, 1)]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 5)]
    public void B_subsecuencia_creciente(int[] datos, int esperado)
    {
        Assert.Equal(esperado, ProblemaB.LongitudCuadratica(datos));
        Assert.Equal(esperado, ProblemaB.LongitudNLogN(datos));
    }

    [Fact]
    public void B_las_dos_versiones_coinciden_al_azar()
    {
        var azar = new Random(6);
        for (int prueba = 0; prueba < 300; prueba++)
        {
            int[] d = Enumerable.Range(0, azar.Next(0, 40)).Select(_ => azar.Next(-5, 15)).ToArray();
            Assert.Equal(ProblemaB.LongitudCuadratica(d), ProblemaB.LongitudNLogN(d));
        }
    }

    [Fact]
    public void C_combinar_intervalos()
    {
        Assert.Empty(ProblemaC.Combinar([]));
        Assert.Equal([new Intervalo(1, 6), new Intervalo(8, 10), new Intervalo(15, 18)],
            ProblemaC.Combinar([new(8, 10), new(1, 3), new(2, 6), new(15, 18)]));
        Assert.Equal([new Intervalo(1, 5)], ProblemaC.Combinar([new(1, 3), new(3, 5)]));   // se tocan
        Assert.Equal([new Intervalo(1, 10)], ProblemaC.Combinar([new(1, 10), new(2, 3)])); // uno dentro de otro
        Assert.Equal([new Intervalo(1, 2), new Intervalo(3, 4)], ProblemaC.Combinar([new(3, 4), new(1, 2)]));
    }
}
