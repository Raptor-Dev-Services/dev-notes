namespace Entrevistas.M02.Practica;

public static class Problema1
{
    // O(n) en tiempo y O(n) en espacio. HashSet.Add devuelve false si el valor ya estaba.
    public static bool TieneDuplicados(int[] numeros)
    {
        var vistos = new HashSet<int>();
        foreach (int x in numeros)
            if (!vistos.Add(x))
                return true;
        return false;
    }
}

public static class Problema2
{
    // Dos pasadas: contar y luego buscar el primero con cuenta 1. O(n) en tiempo, O(k) en espacio.
    public static int PrimerCaracterUnico(string texto)
    {
        var cuenta = new Dictionary<char, int>();
        foreach (char c in texto)
            cuenta[c] = cuenta.GetValueOrDefault(c) + 1;

        for (int i = 0; i < texto.Length; i++)
            if (cuenta[texto[i]] == 1)
                return i;
        return -1;
    }
}

public static class Problema3
{
    // O(n) en tiempo y O(k) en espacio. Compara carácter por carácter, sin ignorar mayúsculas.
    public static bool SonAnagramas(string a, string b)
    {
        if (a.Length != b.Length) return false;

        var cuenta = new Dictionary<char, int>();
        foreach (char c in a)
            cuenta[c] = cuenta.GetValueOrDefault(c) + 1;

        foreach (char c in b)
        {
            int restantes = cuenta.GetValueOrDefault(c) - 1;
            if (restantes < 0) return false;
            cuenta[c] = restantes;
        }
        return true;
    }
}

// Problema 4. Cada elemento entra una vez a cada pila y sale una vez de cada una:
// Enqueue es O(1) y Dequeue es O(1) amortizado.
public sealed class ColaConDosPilas<T>
{
    private readonly Stack<T> _entrada = new();
    private readonly Stack<T> _salida = new();

    public int Count => _entrada.Count + _salida.Count;

    public void Enqueue(T valor) => _entrada.Push(valor);

    public T Dequeue()
    {
        Rellenar();
        return _salida.Pop();
    }

    public T Peek()
    {
        Rellenar();
        return _salida.Peek();
    }

    private void Rellenar()
    {
        if (_salida.Count > 0) return;
        if (_entrada.Count == 0) throw new InvalidOperationException("La cola está vacía.");
        while (_entrada.Count > 0)
            _salida.Push(_entrada.Pop());
    }
}

public static class Problema5
{
    // Ventana deslizante: O(n) en tiempo y O(1) en espacio. La suma va en long para no desbordar.
    public static long SumaMaximaDeVentana(int[] numeros, int k)
    {
        if (k < 1 || k > numeros.Length)
            throw new ArgumentOutOfRangeException(nameof(k), "k debe estar entre 1 y el tamaño del arreglo.");

        long ventana = 0;
        for (int i = 0; i < k; i++)
            ventana += numeros[i];

        long mejor = ventana;
        for (int i = k; i < numeros.Length; i++)
        {
            ventana += (long)numeros[i] - numeros[i - k];
            mejor = Math.Max(mejor, ventana);
        }
        return mejor;
    }
}
