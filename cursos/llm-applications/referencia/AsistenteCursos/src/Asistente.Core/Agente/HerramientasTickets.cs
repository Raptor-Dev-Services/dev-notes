using System.Text.Json;
using Anthropic.Helpers;
using Asistente.Core.Extraccion;

namespace Asistente.Core.Agente;

[SchemaClass("Búsqueda de tickets de soporte existentes.")]
public sealed class EntradaBuscarTickets
{
    [SchemaProperty("Una o dos palabras clave del problema, por ejemplo 'contraseña' o 'cargo doble'.")]
    public string Texto { get; set; } = "";

    [SchemaProperty("true para ver solo los tickets abiertos.")]
    public bool SoloAbiertos { get; set; }
}

[SchemaClass("Un ticket nuevo para el equipo de soporte.")]
public sealed class EntradaCrearTicket
{
    [SchemaProperty("Título corto del problema, máximo 80 caracteres.")]
    public string Titulo { get; set; } = "";

    [SchemaProperty("Qué pasó, con los datos que dio el alumno. Sin inventar nada.")]
    public string Descripcion { get; set; } = "";

    [SchemaProperty("Prioridad del ticket.")]
    public Urgencia Prioridad { get; set; }
}

public sealed class HerramientaBuscarTickets(RepositorioTickets repositorio) : Herramienta<EntradaBuscarTickets>
{
    public override string Nombre => "buscar_tickets";
    public override bool Escribe => false;
    protected override string Descripcion =>
        "Busca tickets de soporte por palabra clave. Úsala SIEMPRE antes de crear un ticket, " +
        "para no duplicar uno abierto, y cuando el alumno pregunte por el estado de un reporte.";

    protected override async Task<string> HacerAsync(EntradaBuscarTickets e, CancellationToken ct)
    {
        var tickets = await repositorio.BuscarAsync(e.Texto, e.SoloAbiertos, ct: ct);
        if (tickets.Count == 0) return "No hay tickets que coincidan.";
        return JsonSerializer.Serialize(tickets.Select(t => new { id = $"T-{t.Id}", t.Titulo, t.Estado, t.Prioridad }));
    }
}

public sealed class HerramientaCrearTicket(RepositorioTickets repositorio) : Herramienta<EntradaCrearTicket>
{
    public override string Nombre => "crear_ticket";
    public override bool Escribe => true;
    protected override string Descripcion =>
        "Crea un ticket para el equipo de soporte. Úsala solo si buscar_tickets no encontró uno abierto " +
        "sobre el mismo problema. Una persona revisa y autoriza cada ticket antes de crearlo.";

    protected override async Task<string> HacerAsync(EntradaCrearTicket e, CancellationToken ct)
    {
        // El esquema no puede limitar longitudes: la regla de negocio se valida aquí.
        if (string.IsNullOrWhiteSpace(e.Titulo) || e.Titulo.Length > 80)
            throw new ArgumentException("El título debe tener entre 1 y 80 caracteres.");
        if (string.IsNullOrWhiteSpace(e.Descripcion))
            throw new ArgumentException("La descripción no puede ir vacía.");
        long id = await repositorio.CrearAsync(e.Titulo, e.Descripcion, e.Prioridad.ToString().ToLowerInvariant(), ct);
        return $"Ticket T-{id} creado.";
    }

    protected override string Describir(EntradaCrearTicket e) =>
        $"  Título: {e.Titulo}\n  Prioridad: {e.Prioridad}\n  Descripción: {e.Descripcion}";
}
