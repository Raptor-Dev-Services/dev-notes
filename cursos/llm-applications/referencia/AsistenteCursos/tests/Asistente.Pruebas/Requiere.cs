using Npgsql;

namespace Asistente.Pruebas;

/// <summary>Lo que necesita red, dinero o una base se marca como omitido si falta, nunca como aprobado.</summary>
public static class Requiere
{
    public static void LlaveDeApi() =>
        Assert.SkipWhen(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")),
            "Sin ANTHROPIC_API_KEY: la evaluación no corrió.");

    public static string BaseDeDatos()
    {
        var cadena = Environment.GetEnvironmentVariable("ASISTENTE_DB");
        Assert.SkipWhen(string.IsNullOrEmpty(cadena), "Sin ASISTENTE_DB: la prueba de integración no corrió.");
        return cadena!;
    }

    /// <summary>
    /// Las pruebas de integración escriben en su propio esquema, "pruebas": sus vectores léxicos
    /// nunca se mezclan con los del índice real, que son de otro modelo.
    /// </summary>
    public static string BaseDePruebas()
    {
        var cadena = BaseDeDatos();
        using (var conexion = new NpgsqlConnection(cadena))
        {
            conexion.Open();
            // La extensión va en public para que la vean todos los esquemas.
            using var cmd = new NpgsqlCommand(
                "CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public; CREATE SCHEMA IF NOT EXISTS pruebas;", conexion);
            cmd.ExecuteNonQuery();
        }
        return new NpgsqlConnectionStringBuilder(cadena) { SearchPath = "pruebas, public" }.ConnectionString;
    }
}

/// <summary>Las clases que tocan la base corren una tras otra, no en paralelo.</summary>
[CollectionDefinition("BaseDeDatos", DisableParallelization = true)]
public sealed class ColeccionBaseDeDatos;
