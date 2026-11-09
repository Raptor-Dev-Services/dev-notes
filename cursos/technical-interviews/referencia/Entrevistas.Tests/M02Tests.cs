using Entrevistas.M02;
using Entrevistas.M02.Practica;

namespace Entrevistas.Tests;

public class ComplejidadTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    public void A_es_lineal(int n) => Assert.Equal(n, Complejidad.PasosA(n));

    [Theory]
    [InlineData(10)]
    [InlineData(300)]
    public void B_es_cuadratica(int n) => Assert.Equal(n * n, Complejidad.PasosB(n));

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(1024, 10)]
    [InlineData(1025, 11)]
    [InlineData(1_000_000, 20)]
    public void C_es_logaritmica(int n, int esperado) => Assert.Equal(esperado, Complejidad.PasosC(n));

    [Theory]
    [InlineData(10)]
    [InlineData(1000)]
    public void D_es_la_mitad_de_n_cuadrada_y_sigue_siendo_cuadratica(int n)
        => Assert.Equal(n * (n - 1) / 2, Complejidad.PasosD(n));

    [Fact]
    public void D_al_duplicar_n_casi_cuadruplica_los_pasos()
    {
        double razon = (double)Complejidad.PasosD(2000) / Complejidad.PasosD(1000);
        Assert.InRange(razon, 3.9, 4.1);
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(1000, 1)]
    public void E_es_n_mas_m(int n, int m) => Assert.Equal(n + m, Complejidad.PasosE(n, m));

    [Theory]
    [InlineData(1024, 1024 * 10)]
    [InlineData(1000, 1000 * 10)]
    public void F_es_n_log_n(int n, int esperado) => Assert.Equal(esperado, Complejidad.PasosF(n));
}

public class LinealesTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("()", true)]
    [InlineData("{[()()]}", true)]
    [InlineData("a(b[c]{d}e)f", true)]
    [InlineData("(", false)]
    [InlineData(")", false)]
    [InlineData("(]", false)]
    [InlineData("([)]", false)]
    [InlineData("(()", false)]
    public void Parentesis_balanceados(string texto, bool esperado)
        => Assert.Equal(esperado, Parentesis.EstanBalanceados(texto));

    public static TheoryData<int[], int, (int, int)?> CasosDosSuma => new()
    {
        { new[] { 2, 7, 11, 15 }, 9, (0, 1) },
        { new[] { 3, 2, 4 }, 6, (1, 2) },
        { new[] { 3, 3 }, 6, (0, 1) },
        { new[] { -1, -2, -3, -4 }, -7, (2, 3) },
        { new[] { 1, 2, 3 }, 100, null },
        { Array.Empty<int>(), 0, null },
        { new[] { 5 }, 10, null },
    };

    [Theory]
    [MemberData(nameof(CasosDosSuma))]
    public void Dos_suma_las_dos_versiones_coinciden(int[] numeros, int objetivo, (int, int)? esperado)
    {
        Assert.Equal(esperado, DosSuma.FuerzaBruta(numeros, objetivo));
        Assert.Equal(esperado, DosSuma.ConDiccionario(numeros, objetivo));
    }

    [Fact]
    public void Dos_suma_aleatoria_las_dos_encuentran_pares_validos()
    {
        var azar = new Random(7);
        for (int prueba = 0; prueba < 200; prueba++)
        {
            int[] nums = Enumerable.Range(0, azar.Next(0, 30)).Select(_ => azar.Next(-20, 20)).ToArray();
            int objetivo = azar.Next(-30, 30);
            var a = DosSuma.FuerzaBruta(nums, objetivo);
            var b = DosSuma.ConDiccionario(nums, objetivo);
            Assert.Equal(a.HasValue, b.HasValue);
            if (b is var (i, j))
            {
                Assert.True(i < j);
                Assert.Equal(objetivo, nums[i] + nums[j]);
            }
        }
    }

    [Fact]
    public void Frecuencias_ignora_mayusculas()
    {
        var cuenta = Frecuencias.Contar(["Hola", "hola", "adiós", "HOLA"]);
        Assert.Equal(3, cuenta["hola"]);
        Assert.Equal(1, cuenta["ADIÓS"]);
        Assert.Equal(2, cuenta.Count);
        Assert.Empty(Frecuencias.Contar([]));
    }

    [Fact]
    public void Comunes_lento_y_rapido_dan_lo_mismo()
    {
        var a = new List<int> { 1, 2, 3, 4, 2 };
        var b = new List<int> { 2, 4, 6 };
        Assert.Equal([2, 4, 2], TrampasLinq.ComunesLento(a, b));
        Assert.Equal(TrampasLinq.ComunesLento(a, b), TrampasLinq.ComunesRapido(a, b));
        Assert.Empty(TrampasLinq.ComunesRapido([], b));
        Assert.Empty(TrampasLinq.ComunesRapido(a, []));
    }

    [Fact]
    public void Invertir_lento_y_rapido_dan_lo_mismo()
    {
        var origen = new List<int> { 1, 2, 3, 4 };
        Assert.Equal([4, 3, 2, 1], TrampasLista.InvertirLento(origen));
        Assert.Equal([4, 3, 2, 1], TrampasLista.Invertir(origen));
        Assert.Empty(TrampasLista.Invertir([]));
        Assert.Equal([1, 2, 3, 4], origen); // no toca la lista original
    }
}

public class PracticaM02Tests
{
    [Theory]
    [InlineData(new int[0], false)]
    [InlineData(new[] { 1 }, false)]
    [InlineData(new[] { 1, 2, 3 }, false)]
    [InlineData(new[] { 1, 2, 1 }, true)]
    [InlineData(new[] { -5, 0, -5 }, true)]
    public void P1_duplicados(int[] numeros, bool esperado)
        => Assert.Equal(esperado, Problema1.TieneDuplicados(numeros));

    [Theory]
    [InlineData("", -1)]
    [InlineData("a", 0)]
    [InlineData("aabb", -1)]
    [InlineData("abacabad", 3)]
    [InlineData("reconocer", 4)]
    [InlineData("Aa", 0)]
    public void P2_primer_caracter_unico(string texto, int esperado)
        => Assert.Equal(esperado, Problema2.PrimerCaracterUnico(texto));

    [Theory]
    [InlineData("", "", true)]
    [InlineData("roma", "amor", true)]
    [InlineData("aab", "abb", false)]
    [InlineData("abc", "ab", false)]
    [InlineData("Roma", "amor", false)]
    [InlineData("ñandú", "dúnña", true)]
    public void P3_anagramas(string a, string b, bool esperado)
        => Assert.Equal(esperado, Problema3.SonAnagramas(a, b));

    [Fact]
    public void P4_cola_con_dos_pilas_respeta_el_orden_al_intercalar()
    {
        var cola = new ColaConDosPilas<int>();
        var referencia = new Queue<int>();
        var azar = new Random(3);
        for (int i = 0; i < 1000; i++)
        {
            if (referencia.Count == 0 || azar.Next(3) > 0)
            {
                cola.Enqueue(i);
                referencia.Enqueue(i);
            }
            else
            {
                Assert.Equal(referencia.Peek(), cola.Peek());
                Assert.Equal(referencia.Dequeue(), cola.Dequeue());
            }
            Assert.Equal(referencia.Count, cola.Count);
        }
    }

    [Fact]
    public void P4_cola_vacia_lanza()
    {
        var cola = new ColaConDosPilas<string>();
        Assert.Throws<InvalidOperationException>(() => cola.Dequeue());
        Assert.Throws<InvalidOperationException>(() => cola.Peek());
        cola.Enqueue("a");
        Assert.Equal("a", cola.Dequeue());
        Assert.Throws<InvalidOperationException>(() => cola.Dequeue());
    }

    [Theory]
    [InlineData(new[] { 1, 4, 2, 10, 2, 3, 1, 0, 20 }, 4, 24)]
    [InlineData(new[] { 5 }, 1, 5)]
    [InlineData(new[] { -3, -1, -2 }, 1, -1)]
    [InlineData(new[] { -3, -1, -2 }, 3, -6)]
    [InlineData(new[] { 2, 1, 5, 1, 3, 2 }, 3, 9)]
    public void P5_suma_maxima_de_ventana(int[] numeros, int k, long esperado)
        => Assert.Equal(esperado, Problema5.SumaMaximaDeVentana(numeros, k));

    [Fact]
    public void P5_no_desborda_con_valores_extremos()
    {
        int[] grandes = [int.MaxValue, int.MaxValue, int.MinValue, int.MaxValue];
        Assert.Equal(2L * int.MaxValue, Problema5.SumaMaximaDeVentana(grandes, 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void P5_k_fuera_de_rango_lanza(int k)
        => Assert.Throws<ArgumentOutOfRangeException>(() => Problema5.SumaMaximaDeVentana([1, 2, 3], k));
}
