namespace Entrevistas.M03;

public sealed class Nodo(int valor, Nodo? izq = null, Nodo? der = null)
{
    public int Valor { get; } = valor;
    public Nodo? Izq { get; } = izq;
    public Nodo? Der { get; } = der;
}

public static class Recorridos
{
    // Los tres recorridos en profundidad: O(n) en tiempo, O(h) en espacio por la pila de llamadas.
    public static void Preorden(Nodo? nodo, List<int> salida)
    {
        if (nodo is null) return;
        salida.Add(nodo.Valor);      // primero la raíz
        Preorden(nodo.Izq, salida);
        Preorden(nodo.Der, salida);
    }

    public static void Inorden(Nodo? nodo, List<int> salida)
    {
        if (nodo is null) return;
        Inorden(nodo.Izq, salida);
        salida.Add(nodo.Valor);      // la raíz en medio: en un BST sale ordenado
        Inorden(nodo.Der, salida);
    }

    public static void Postorden(Nodo? nodo, List<int> salida)
    {
        if (nodo is null) return;
        Postorden(nodo.Izq, salida);
        Postorden(nodo.Der, salida);
        salida.Add(nodo.Valor);      // la raíz al final
    }

    // En anchura, nivel por nivel, con una cola: O(n) en tiempo, O(w) en espacio (w = nivel más ancho).
    public static List<List<int>> PorNiveles(Nodo? raiz)
    {
        var niveles = new List<List<int>>();
        if (raiz is null) return niveles;

        var cola = new Queue<Nodo>();
        cola.Enqueue(raiz);
        while (cola.Count > 0)
        {
            int enEsteNivel = cola.Count;
            var nivel = new List<int>(enEsteNivel);
            for (int i = 0; i < enEsteNivel; i++)
            {
                var nodo = cola.Dequeue();
                nivel.Add(nodo.Valor);
                if (nodo.Izq is not null) cola.Enqueue(nodo.Izq);
                if (nodo.Der is not null) cola.Enqueue(nodo.Der);
            }
            niveles.Add(nivel);
        }
        return niveles;
    }
}

public static class Arboles
{
    // O(n) en tiempo, O(h) en espacio. Un árbol vacío mide 0.
    public static int Altura(Nodo? nodo)
        => nodo is null ? 0 : 1 + Math.Max(Altura(nodo.Izq), Altura(nodo.Der));

    // Cada nodo debe caer dentro del rango que le heredan sus ancestros, no solo comparar con su padre.
    // O(n) en tiempo, O(h) en espacio. No admite valores repetidos.
    public static bool EsBst(Nodo? raiz) => EsBst(raiz, long.MinValue, long.MaxValue);

    private static bool EsBst(Nodo? nodo, long minimo, long maximo)
    {
        if (nodo is null) return true;
        if (nodo.Valor <= minimo || nodo.Valor >= maximo) return false;
        return EsBst(nodo.Izq, minimo, nodo.Valor) && EsBst(nodo.Der, nodo.Valor, maximo);
    }

    // Buscar en un BST: O(h). Balanceado, h = log n; degenerado (una sola rama), h = n.
    public static bool Contiene(Nodo? nodo, int valor)
    {
        while (nodo is not null)
        {
            if (valor == nodo.Valor) return true;
            nodo = valor < nodo.Valor ? nodo.Izq : nodo.Der;
        }
        return false;
    }
}
