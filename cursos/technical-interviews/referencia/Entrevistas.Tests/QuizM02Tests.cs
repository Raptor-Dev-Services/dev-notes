using Entrevistas.M02;

namespace Entrevistas.Tests;

public class QuizM02Tests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(1024, 10)]
    [InlineData(1_000_000, 19)]
    public void Mitades_es_logaritmica(int n, int esperado) => Assert.Equal(esperado, QuizM02.Mitades(n));

    [Fact]
    public void Constante_interna_es_lineal()
    {
        Assert.Equal(5 * 1000, QuizM02.ConstanteInterna(new int[1000]));
        Assert.Equal(2 * QuizM02.ConstanteInterna(new int[1000]), QuizM02.ConstanteInterna(new int[2000]));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1000)]
    public void Triangular_es_cuadratica(int n) => Assert.Equal(n * (n - 1) / 2, QuizM02.Triangular(n));

    [Fact]
    public void RemoveAt_cero_mueve_un_numero_cuadratico_de_elementos()
    {
        Assert.Equal(1000L * 999 / 2, QuizM02.MovimientosConRemoveAt(1000));
        Assert.Equal(1000, QuizM02.ProcesadosConCola(1000));
    }
}
