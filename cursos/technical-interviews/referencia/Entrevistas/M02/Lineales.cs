namespace Entrevistas.M02;

public static class Parentesis
{
    // O(n) en tiempo y O(n) en espacio: cada carácter entra y sale de la pila a lo más una vez.
    public static bool EstanBalanceados(string texto)
    {
        var pila = new Stack<char>();
        foreach (char c in texto)
        {
            if (c is '(' or '[' or '{')
            {
                pila.Push(c);
            }
            else if (c is ')' or ']' or '}')
            {
                if (pila.Count == 0) return false;
                char abre = pila.Pop();
                if ((c == ')' && abre != '(') || (c == ']' && abre != '[') || (c == '}' && abre != '{'))
                    return false;
            }
        }
        return pila.Count == 0;
    }
}

public static class DosSuma
{
    // Fuerza bruta: O(n²) en tiempo, O(1) en espacio.
    public static (int I, int J)? FuerzaBruta(int[] numeros, int objetivo)
    {
        for (int i = 0; i < numeros.Length; i++)
            for (int j = i + 1; j < numeros.Length; j++)
                if (numeros[i] + numeros[j] == objetivo)
                    return (i, j);
        return null;
    }

    // Con diccionario: O(n) en tiempo, O(n) en espacio. Se cambia memoria por tiempo.
    public static (int I, int J)? ConDiccionario(int[] numeros, int objetivo)
    {
        var vistos = new Dictionary<int, int>(); // valor, índice donde apareció
        for (int j = 0; j < numeros.Length; j++)
        {
            if (vistos.TryGetValue(objetivo - numeros[j], out int i))
                return (i, j);
            vistos[numeros[j]] = j;
        }
        return null;
    }
}

public static class Frecuencias
{
    // O(n) en tiempo; O(k) en espacio, con k palabras distintas.
    public static Dictionary<string, int> Contar(IEnumerable<string> palabras)
    {
        var cuenta = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var palabra in palabras)
            cuenta[palabra] = cuenta.GetValueOrDefault(palabra) + 1;
        return cuenta;
    }
}

public static class TrampasLinq
{
    // O(n × m): Contains recorre la lista b completa por cada elemento de a.
    public static List<int> ComunesLento(List<int> a, List<int> b)
        => a.Where(x => b.Contains(x)).ToList();

    // O(n + m): construir el conjunto cuesta m, y cada consulta cuesta O(1) en promedio.
    public static List<int> ComunesRapido(List<int> a, List<int> b)
    {
        var enB = new HashSet<int>(b);
        return a.Where(enB.Contains).ToList();
    }
}

public static class TrampasLista
{
    // O(n²): cada Insert(0, ...) recorre toda la lista para hacer espacio.
    public static List<int> InvertirLento(List<int> origen)
    {
        var resultado = new List<int>();
        foreach (var x in origen)
            resultado.Insert(0, x);
        return resultado;
    }

    // O(n): se recorre de atrás hacia adelante y cada Add es O(1) amortizado.
    public static List<int> Invertir(List<int> origen)
    {
        var resultado = new List<int>(origen.Count);
        for (int i = origen.Count - 1; i >= 0; i--)
            resultado.Add(origen[i]);
        return resultado;
    }
}
