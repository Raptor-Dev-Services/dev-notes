using Asistente.Core.Rag;

namespace Asistente.Pruebas;

public class FragmentadorPruebas
{
    [Fact]
    public void Parte_por_secciones_y_cada_fragmento_lleva_su_titulo()
    {
        var md = "# Nota\n\nIntro.\n\n## Costos\n\nEl costo se mide por token.\n\n## Latencia\n\nSe mide en milisegundos.";
        var fragmentos = Fragmentador.Partir("nota.md", md);
        Assert.Equal(["Nota", "Costos", "Latencia"], fragmentos.Select(f => f.Seccion));
        Assert.StartsWith("Costos", fragmentos[1].Contenido);
    }

    [Fact]
    public void Un_encabezado_dentro_de_un_bloque_de_codigo_no_parte_la_seccion()
    {
        var md = "## Ejemplo\n\n```bash\n# esto es un comentario de bash\necho hola\n```\n";
        var fragmentos = Fragmentador.Partir("nota.md", md);
        Assert.Single(fragmentos);
        Assert.Contains("# esto es un comentario de bash", fragmentos[0].Contenido);
    }

    [Fact]
    public void Una_seccion_larga_se_parte_sin_perder_texto_y_con_solape()
    {
        var parrafos = Enumerable.Range(1, 30).Select(i => $"Párrafo {i}: " + new string('x', 100)).ToList();
        var md = "## Larga\n\n" + string.Join("\n\n", parrafos);
        var fragmentos = Fragmentador.Partir("nota.md", md, maxCaracteres: 600, solape: 120);

        Assert.True(fragmentos.Count > 1);
        foreach (var p in parrafos)
            Assert.Contains(fragmentos, f => f.Contenido.Contains(p));
        Assert.All(fragmentos, f => Assert.True(f.Contenido.Length <= 600 + 120 + "Larga\n\n".Length));
    }
}
