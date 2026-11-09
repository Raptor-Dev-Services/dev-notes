using Npgsql;
using Pgvector;

namespace Asistente.Core.Rag;

public sealed record FragmentoRecuperado(long Id, string Fuente, string Seccion, string Contenido, double Distancia)
{
    /// <summary>La clave con la que el modelo cita el fragmento.</summary>
    public string Clave => $"F{Id}";
}

/// <summary>Fragmentos y vectores en PostgreSQL con pgvector. Todo el SQL va parametrizado.</summary>
public sealed class AlmacenVectorial(NpgsqlDataSource fuente)
{
    public static NpgsqlDataSource CrearFuente(string cadenaDeConexion)
    {
        var constructor = new NpgsqlDataSourceBuilder(cadenaDeConexion);
        constructor.UseVector();   // registra el tipo vector de pgvector en Npgsql
        return constructor.Build();
    }

    public async Task CrearEsquemaAsync(int dimensiones, CancellationToken ct = default)
    {
        await using var conexion = await fuente.OpenConnectionAsync(ct);
        // El DDL no admite parámetros: la dimensión es un entero de configuración, nunca texto de un usuario.
        await using (var cmd = new NpgsqlCommand($"""
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS fragmentos (
                id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                fuente     text        NOT NULL,
                seccion    text        NOT NULL,
                contenido  text        NOT NULL,
                embedding  vector({dimensiones}) NOT NULL,
                creado_utc timestamptz NOT NULL DEFAULT now()
            );
            CREATE INDEX IF NOT EXISTS fragmentos_fuente ON fragmentos (fuente);
            CREATE INDEX IF NOT EXISTS fragmentos_embedding
                ON fragmentos USING hnsw (embedding vector_cosine_ops);
            """, conexion))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await conexion.ReloadTypesAsync(ct);   // la extensión recién creada trae el tipo vector
    }

    /// <summary>Reingestar un documento reemplaza sus fragmentos, en una sola transacción.</summary>
    public async Task ReemplazarFuenteAsync(
        string fuenteDoc, IReadOnlyList<FragmentoNuevo> fragmentos, IReadOnlyList<float[]> vectores, CancellationToken ct = default)
    {
        if (fragmentos.Count != vectores.Count)
            throw new ArgumentException("Cada fragmento necesita su vector.", nameof(vectores));

        await using var conexion = await fuente.OpenConnectionAsync(ct);
        await using var tx = await conexion.BeginTransactionAsync(ct);
        await using (var borrar = new NpgsqlCommand("DELETE FROM fragmentos WHERE fuente = @fuente", conexion, tx))
        {
            borrar.Parameters.AddWithValue("fuente", fuenteDoc);
            await borrar.ExecuteNonQueryAsync(ct);
        }
        for (int i = 0; i < fragmentos.Count; i++)
        {
            await using var insertar = new NpgsqlCommand("""
                INSERT INTO fragmentos (fuente, seccion, contenido, embedding)
                VALUES (@fuente, @seccion, @contenido, @embedding)
                """, conexion, tx);
            insertar.Parameters.AddWithValue("fuente", fuenteDoc);
            insertar.Parameters.AddWithValue("seccion", fragmentos[i].Seccion);
            insertar.Parameters.AddWithValue("contenido", fragmentos[i].Contenido);
            insertar.Parameters.AddWithValue("embedding", new Vector(vectores[i]));
            await insertar.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    /// <summary>Los k fragmentos más cercanos por distancia coseno (operador &lt;=&gt;): 0 es idéntico, 2 es opuesto.</summary>
    public async Task<IReadOnlyList<FragmentoRecuperado>> BuscarAsync(float[] consulta, int k, CancellationToken ct = default)
    {
        await using var cmd = fuente.CreateCommand("""
            SELECT id, fuente, seccion, contenido, embedding <=> @consulta AS distancia
            FROM fragmentos
            ORDER BY embedding <=> @consulta
            LIMIT @k
            """);
        cmd.Parameters.AddWithValue("consulta", new Vector(consulta));
        cmd.Parameters.AddWithValue("k", k);

        var resultado = new List<FragmentoRecuperado>();
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        while (await lector.ReadAsync(ct))
            resultado.Add(new(lector.GetInt64(0), lector.GetString(1), lector.GetString(2), lector.GetString(3), lector.GetDouble(4)));
        return resultado;
    }
}
