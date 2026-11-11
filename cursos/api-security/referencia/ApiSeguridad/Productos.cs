using Microsoft.Data.Sqlite;

namespace ApiSeguridad;

public sealed record ProductoDto(int Id, string Nombre, decimal Precio);

/// <summary>Catalogo en SQLite en memoria (ficticio). La conexion se mantiene abierta: en memoria, cerrar es perder los datos.</summary>
public sealed class Catalogo : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private readonly object _candado = new();

    public Catalogo()
    {
        _conexion.Open();
        using var crear = _conexion.CreateCommand();
        crear.CommandText = "CREATE TABLE productos (id INTEGER PRIMARY KEY, nombre TEXT NOT NULL, precio REAL NOT NULL)";
        crear.ExecuteNonQuery();
        for (var i = 1; i <= 120; i++)
        {
            using var insertar = _conexion.CreateCommand();
            insertar.CommandText = "INSERT INTO productos (id, nombre, precio) VALUES ($id, $nombre, $precio)";
            insertar.Parameters.AddWithValue("$id", i);
            insertar.Parameters.AddWithValue("$nombre", i == 7 ? "Cuaderno O'Brien" : $"Cuaderno {i:000}");
            insertar.Parameters.AddWithValue("$precio", 20 + i);
            insertar.ExecuteNonQuery();
        }
    }

    /// <summary>Version segura: el texto del cliente viaja como parametro, nunca como parte de la sentencia.</summary>
    public List<ProductoDto> Buscar(string nombre, int limite, int pagina)
    {
        lock (_candado)
        {
            using var cmd = _conexion.CreateCommand();
            cmd.CommandText = "SELECT id, nombre, precio FROM productos WHERE nombre LIKE $patron ORDER BY id LIMIT $limite OFFSET $desde";
            cmd.Parameters.AddWithValue("$patron", "%" + nombre + "%");
            cmd.Parameters.AddWithValue("$limite", limite);
            cmd.Parameters.AddWithValue("$desde", (pagina - 1) * limite);
            return Leer(cmd);
        }
    }

    /// <summary>
    /// Version VULNERABLE del laboratorio (solo con Lab:SqlConcatenado): el texto del cliente pasa a ser parte de la sentencia.
    /// Esta aqui para que la prueba se vea fallar; no se imita en codigo real.
    /// </summary>
    public List<ProductoDto> BuscarConcatenado(string nombre, int limite, int pagina)
    {
        lock (_candado)
        {
            using var cmd = _conexion.CreateCommand();
            cmd.CommandText = "SELECT id, nombre, precio FROM productos WHERE nombre LIKE '%" + nombre + "%' ORDER BY id LIMIT " + limite + " OFFSET " + ((pagina - 1) * limite);
            return Leer(cmd);
        }
    }

    private static List<ProductoDto> Leer(SqliteCommand cmd)
    {
        var lista = new List<ProductoDto>();
        using var lector = cmd.ExecuteReader();
        while (lector.Read())
            lista.Add(new ProductoDto(lector.GetInt32(0), lector.GetString(1), (decimal)lector.GetDouble(2)));
        return lista;
    }

    public void Dispose() => _conexion.Dispose();
}
