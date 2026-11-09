using System.Collections.Concurrent;

namespace Entrevistas.M06;

// Acortador de URL: el id numérico que da la base se convierte en un código corto.
// Con 62 símbolos, 7 caracteres alcanzan para 62^7 (unos 3.5 billones) de enlaces.
public static class Base62
{
    private const string Alfabeto = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // O(log n) en base 62: un carácter por cada división entre 62, escrito de derecha a izquierda.
    public static string Codificar(long id)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        if (id == 0) return "0";
        Span<char> buffer = stackalloc char[11]; // long.MaxValue ocupa 11 caracteres en base 62
        int pos = buffer.Length;
        while (id > 0)
        {
            buffer[--pos] = Alfabeto[(int)(id % 62)];
            id /= 62;
        }
        return new string(buffer[pos..]);
    }

    public static long Decodificar(string codigo)
    {
        if (codigo.Length == 0) throw new FormatException("El código está vacío.");
        long id = 0;
        foreach (char c in codigo)
        {
            int valor = Alfabeto.IndexOf(c, StringComparison.Ordinal);
            if (valor < 0) throw new FormatException($"Carácter no válido: '{c}'.");
            id = checked(id * 62 + valor); // un código demasiado largo desborda en vez de dar basura
        }
        return id;
    }
}

// Una cola entrega "al menos una vez": el mismo mensaje puede llegar dos veces.
// El consumidor recuerda qué ids ya procesó y el segundo intento no repite el efecto.
public sealed class ConsumidorIdempotente
{
    private readonly ConcurrentDictionary<Guid, bool> _procesados = new();
    private int _clics;

    public int Clics => _clics;

    public bool Procesar(Guid idMensaje)
    {
        if (!_procesados.TryAdd(idMensaje, true)) return false; // repetido: se ignora
        Interlocked.Increment(ref _clics);
        return true;
    }
}

// Reservas: dos personas piden el mismo horario a la vez y solo una puede ganar.
// TryAdd es atómico, igual que un índice único en la base: el segundo insert falla.
public sealed class AgendaEnMemoria
{
    private readonly ConcurrentDictionary<(int Recurso, DateTime Inicio), string> _ocupados = new();

    public bool TryReservar(int recurso, DateTime inicio, string cliente)
        => _ocupados.TryAdd((recurso, inicio), cliente);

    public string? Quien(int recurso, DateTime inicio)
        => _ocupados.TryGetValue((recurso, inicio), out var cliente) ? cliente : null;
}
