using Npgsql;

namespace Asistente.Core.Agente;

public sealed record Ticket(long Id, string Titulo, string Descripcion, string Prioridad, string Estado, DateTime CreadoUtc);

/// <summary>Consultas fijas y parametrizadas: el modelo elige los valores, nunca escribe SQL.</summary>
public sealed class RepositorioTickets(NpgsqlDataSource fuente)
{
    public async Task CrearEsquemaAsync(CancellationToken ct = default)
    {
        await using var cmd = fuente.CreateCommand("""
            CREATE TABLE IF NOT EXISTS tickets (
                id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                titulo      text        NOT NULL CHECK (length(titulo) BETWEEN 1 AND 80),
                descripcion text        NOT NULL,
                prioridad   text        NOT NULL CHECK (prioridad IN ('baja', 'media', 'alta')),
                estado      text        NOT NULL DEFAULT 'abierto' CHECK (estado IN ('abierto', 'cerrado')),
                creado_utc  timestamptz NOT NULL DEFAULT now()
            );
            """);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<Ticket>> BuscarAsync(string texto, bool soloAbiertos, int limite = 10, CancellationToken ct = default)
    {
        await using var cmd = fuente.CreateCommand("""
            SELECT id, titulo, descripcion, prioridad, estado, creado_utc
            FROM tickets
            WHERE (titulo ILIKE @patron OR descripcion ILIKE @patron)
              AND (NOT @soloAbiertos OR estado = 'abierto')
            ORDER BY creado_utc DESC
            LIMIT @limite
            """);
        // % y _ son comodines de ILIKE: se escapan para que el texto se busque literal.
        var literal = texto.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        cmd.Parameters.AddWithValue("patron", $"%{literal}%");
        cmd.Parameters.AddWithValue("soloAbiertos", soloAbiertos);
        cmd.Parameters.AddWithValue("limite", limite);

        var tickets = new List<Ticket>();
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        while (await lector.ReadAsync(ct))
            tickets.Add(new(lector.GetInt64(0), lector.GetString(1), lector.GetString(2),
                lector.GetString(3), lector.GetString(4), lector.GetDateTime(5)));
        return tickets;
    }

    public async Task<long> CrearAsync(string titulo, string descripcion, string prioridad, CancellationToken ct = default)
    {
        await using var cmd = fuente.CreateCommand("""
            INSERT INTO tickets (titulo, descripcion, prioridad)
            VALUES (@titulo, @descripcion, @prioridad)
            RETURNING id
            """);
        cmd.Parameters.AddWithValue("titulo", titulo);
        cmd.Parameters.AddWithValue("descripcion", descripcion);
        cmd.Parameters.AddWithValue("prioridad", prioridad);
        return (long)(await cmd.ExecuteScalarAsync(ct))!;
    }
}
