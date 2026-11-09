namespace Entrevistas.M03;

public static class Grafos
{
    // Lista de adyacencia de un grafo no dirigido: memoria O(V + E).
    public static Dictionary<string, List<string>> NoDirigido(IEnumerable<(string A, string B)> aristas)
    {
        var grafo = new Dictionary<string, List<string>>();
        foreach (var (a, b) in aristas)
        {
            if (!grafo.TryGetValue(a, out var vecinosA)) grafo[a] = vecinosA = [];
            if (!grafo.TryGetValue(b, out var vecinosB)) grafo[b] = vecinosB = [];
            vecinosA.Add(b);
            vecinosB.Add(a);
        }
        return grafo;
    }

    // BFS: el menor número de aristas entre dos nodos de un grafo sin pesos. -1 si no hay camino.
    // O(V + E): cada nodo entra a la cola una vez y cada arista se revisa una vez por extremo.
    public static int CaminoMasCorto(Dictionary<string, List<string>> grafo, string origen, string destino)
    {
        if (origen == destino) return 0;
        var distancia = new Dictionary<string, int> { [origen] = 0 };
        var cola = new Queue<string>();
        cola.Enqueue(origen);
        while (cola.Count > 0)
        {
            var actual = cola.Dequeue();
            foreach (var vecino in grafo.GetValueOrDefault(actual) ?? [])
            {
                if (distancia.ContainsKey(vecino)) continue; // ya visitado
                distancia[vecino] = distancia[actual] + 1;
                if (vecino == destino) return distancia[vecino];
                cola.Enqueue(vecino);
            }
        }
        return -1;
    }

    // DFS iterativo con una pila: todo lo alcanzable desde el origen. O(V + E).
    public static HashSet<string> Alcanzables(Dictionary<string, List<string>> grafo, string origen)
    {
        var visitados = new HashSet<string>();
        var pila = new Stack<string>();
        pila.Push(origen);
        while (pila.Count > 0)
        {
            var actual = pila.Pop();
            if (!visitados.Add(actual)) continue;
            foreach (var vecino in grafo.GetValueOrDefault(actual) ?? [])
                if (!visitados.Contains(vecino))
                    pila.Push(vecino);
        }
        return visitados;
    }

    // Orden topológico (algoritmo de Kahn) para n tareas numeradas 0..n-1.
    // Devuelve null si hay un ciclo, porque entonces ningún orden cumple todas las dependencias.
    // O(V + E) en tiempo y en espacio.
    public static int[]? OrdenTopologico(int n, IEnumerable<(int Antes, int Despues)> dependencias)
    {
        var siguientes = new List<int>[n];
        for (int i = 0; i < n; i++) siguientes[i] = [];
        var pendientes = new int[n]; // cuántas dependencias le faltan a cada tarea

        foreach (var (antes, despues) in dependencias)
        {
            siguientes[antes].Add(despues);
            pendientes[despues]++;
        }

        var listas = new Queue<int>();
        for (int i = 0; i < n; i++)
            if (pendientes[i] == 0) listas.Enqueue(i);

        var orden = new List<int>(n);
        while (listas.Count > 0)
        {
            int tarea = listas.Dequeue();
            orden.Add(tarea);
            foreach (int s in siguientes[tarea])
                if (--pendientes[s] == 0)
                    listas.Enqueue(s);
        }
        return orden.Count == n ? orden.ToArray() : null;
    }
}

public static class Cuadricula
{
    // Cada celda de tierra es un nodo; sus vecinos son arriba, abajo, izquierda y derecha.
    // O(F × C) en tiempo y en espacio. No modifica el mapa que recibe.
    public static int ContarIslas(bool[,] tierra)
    {
        int filas = tierra.GetLength(0), columnas = tierra.GetLength(1);
        var visto = new bool[filas, columnas];
        int islas = 0;

        for (int f = 0; f < filas; f++)
            for (int c = 0; c < columnas; c++)
            {
                if (!tierra[f, c] || visto[f, c]) continue;
                islas++;
                Inundar(tierra, visto, f, c);
            }
        return islas;
    }

    private static readonly (int Df, int Dc)[] Vecinos = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    // BFS con cola en vez de recursión: una isla enorme no desborda la pila.
    private static void Inundar(bool[,] tierra, bool[,] visto, int f0, int c0)
    {
        var cola = new Queue<(int F, int C)>();
        cola.Enqueue((f0, c0));
        visto[f0, c0] = true;
        while (cola.Count > 0)
        {
            var (f, c) = cola.Dequeue();
            foreach (var (df, dc) in Vecinos)
            {
                int nf = f + df, nc = c + dc;
                if (nf < 0 || nc < 0 || nf >= tierra.GetLength(0) || nc >= tierra.GetLength(1)) continue;
                if (!tierra[nf, nc] || visto[nf, nc]) continue;
                visto[nf, nc] = true;
                cola.Enqueue((nf, nc));
            }
        }
    }
}

public static class Dijkstra
{
    // Distancia mínima desde el origen con pesos NO negativos. O((V + E) log V) con PriorityQueue.
    // PriorityQueue no permite bajar una prioridad: se encola de nuevo y se descarta la entrada vieja.
    public static Dictionary<int, long> Distancias(Dictionary<int, List<(int Destino, int Peso)>> grafo, int origen)
    {
        var distancia = new Dictionary<int, long> { [origen] = 0 };
        var cola = new PriorityQueue<int, long>();
        cola.Enqueue(origen, 0);

        while (cola.TryDequeue(out int actual, out long d))
        {
            if (d > distancia[actual]) continue; // entrada vieja: ya se encontró algo mejor
            foreach (var (destino, peso) in grafo.GetValueOrDefault(actual) ?? [])
            {
                long nueva = d + peso;
                if (nueva < distancia.GetValueOrDefault(destino, long.MaxValue))
                {
                    distancia[destino] = nueva;
                    cola.Enqueue(destino, nueva);
                }
            }
        }
        return distancia;
    }
}
