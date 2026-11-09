using Asistente.Core.Seguridad;

namespace Asistente.Pruebas;

public class FiltroDeSalidaPruebas
{
    private readonly FiltroDeSalida filtro = new(["academia.example"]);

    [Fact]
    public void Quita_las_imagenes_aunque_apunten_a_un_dominio_permitido()
    {
        var r = filtro.Filtrar("Listo. ![estado](https://academia.example/p.png)");
        Assert.DoesNotContain("![", r.Texto);
        Assert.Single(r.Retirados);
    }

    [Theory]
    [InlineData("https://reembolsos.atacante.example/formulario")]
    [InlineData("http://academia.example.atacante.example/x")]
    [InlineData("https://academia.examplex/x")]
    public void Retira_enlaces_a_dominios_ajenos_o_que_se_disfrazan(string url)
    {
        var r = filtro.Filtrar($"Llena el formulario en {url} hoy.");
        Assert.DoesNotContain(url, r.Texto);
        Assert.Contains(FiltroDeSalida.Retirado, r.Texto);
    }

    [Theory]
    [InlineData("https://academia.example/ayuda")]
    [InlineData("https://soporte.academia.example/ayuda")]
    public void Conserva_los_enlaces_permitidos(string url) =>
        Assert.Contains(url, filtro.Filtrar($"Consulta {url}").Texto);

    [Fact]
    public void Retira_correos_de_dominios_ajenos_y_conserva_los_propios()
    {
        var r = filtro.Filtrar("Escribe a soporte@atacante.example o a ayuda@academia.example.");
        Assert.DoesNotContain("atacante.example", r.Texto);
        Assert.Contains("ayuda@academia.example", r.Texto);
    }
}
