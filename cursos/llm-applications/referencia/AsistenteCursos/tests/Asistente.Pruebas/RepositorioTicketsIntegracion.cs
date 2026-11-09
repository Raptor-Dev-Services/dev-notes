using Asistente.Core.Agente;
using Asistente.Core.Rag;

namespace Asistente.Pruebas;

[Trait("Categoria", "Integracion")]
[Collection("BaseDeDatos")]
public class RepositorioTicketsIntegracion
{
    [Fact]
    public async Task Crea_y_encuentra_un_ticket_y_los_comodines_se_buscan_literales()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fuente = AlmacenVectorial.CrearFuente(Requiere.BaseDePruebas());
        var repo = new RepositorioTickets(fuente);
        await repo.CrearEsquemaAsync(ct);

        var marca = Guid.NewGuid().ToString("N")[..8];
        long id = await repo.CrearAsync($"No llega el correo {marca}", "El alumno no recibe el correo de confirmación.", "alta", ct);

        var encontrados = await repo.BuscarAsync(marca, soloAbiertos: true, ct: ct);
        Assert.Contains(encontrados, t => t.Id == id && t.Estado == "abierto");
        // "%" no es comodín: no debe traer todos los tickets.
        Assert.Empty(await repo.BuscarAsync($"%{marca}%x", soloAbiertos: false, ct: ct));
    }
}
