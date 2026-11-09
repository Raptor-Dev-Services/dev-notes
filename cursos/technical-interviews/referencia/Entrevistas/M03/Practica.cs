namespace Entrevistas.M03.Practica;

public static class Problema1
{
    // Primera aparición en un arreglo ordenado con repetidos: el primer índice con datos[i] >= objetivo,
    // y luego se confirma que sea el objetivo. O(log n) en tiempo, O(1) en espacio.
    public static int PrimeraAparicion(int[] datos, int objetivo)
    {
        int i = Busqueda.PrimerVerdadero(0, datos.Length, k => datos[k] >= objetivo);
        return i < datos.Length && datos[i] == objetivo ? i : -1;
    }
}

public static class Problema2
{
    // Profundidad mínima: nodos del camino más corto de la raíz a una hoja.
    // BFS se detiene en la primera hoja que encuentra: O(n) en el peor caso, O(w) en espacio.
    public static int ProfundidadMinima(Nodo? raiz)
    {
        if (raiz is null) return 0;
        var cola = new Queue<(Nodo Nodo, int Nivel)>();
        cola.Enqueue((raiz, 1));
        while (cola.Count > 0)
        {
            var (nodo, nivel) = cola.Dequeue();
            if (nodo.Izq is null && nodo.Der is null) return nivel; // la hoja más cercana
            if (nodo.Izq is not null) cola.Enqueue((nodo.Izq, nivel + 1));
            if (nodo.Der is not null) cola.Enqueue((nodo.Der, nivel + 1));
        }
        throw new InvalidOperationException("Inalcanzable: todo árbol finito tiene al menos una hoja.");
    }
}

public static class Problema3
{
    // Ancestro común más bajo en un BST (los dos valores deben estar en el árbol).
    // Mientras los dos queden del mismo lado, se baja por ese lado. O(h) en tiempo, O(1) en espacio.
    public static int? AncestroComun(Nodo? raiz, int a, int b)
    {
        var nodo = raiz;
        while (nodo is not null)
        {
            if (a < nodo.Valor && b < nodo.Valor) nodo = nodo.Izq;
            else if (a > nodo.Valor && b > nodo.Valor) nodo = nodo.Der;
            else return nodo.Valor; // se separan aquí, o uno de los dos es este nodo
        }
        return null;
    }
}

public static class Problema4
{
    // Cuántos grupos de amigos hay entre n personas (0..n-1): componentes conexas de un grafo no dirigido.
    // O(V + E) en tiempo y en espacio.
    public static int ContarGrupos(int n, IEnumerable<(int A, int B)> amistades)
    {
        var vecinos = new List<int>[n];
        for (int i = 0; i < n; i++) vecinos[i] = [];
        foreach (var (a, b) in amistades)
        {
            vecinos[a].Add(b);
            vecinos[b].Add(a);
        }

        var visto = new bool[n];
        int grupos = 0;
        var pila = new Stack<int>();
        for (int inicio = 0; inicio < n; inicio++)
        {
            if (visto[inicio]) continue;
            grupos++;
            visto[inicio] = true;
            pila.Push(inicio);
            while (pila.Count > 0)
                foreach (int v in vecinos[pila.Pop()])
                    if (!visto[v]) { visto[v] = true; pila.Push(v); }
        }
        return grupos;
    }
}

public static class Problema5
{
    // Menos pasos de la esquina superior izquierda a la inferior derecha, sin pisar celdas bloqueadas.
    // BFS sobre la cuadrícula: O(F × C) en tiempo y en espacio. -1 si no hay camino.
    public static int PasosMinimos(bool[,] libre)
    {
        int filas = libre.GetLength(0), columnas = libre.GetLength(1);
        if (filas == 0 || columnas == 0 || !libre[0, 0] || !libre[filas - 1, columnas - 1]) return -1;

        var pasos = new int[filas, columnas];
        for (int f = 0; f < filas; f++)
            for (int c = 0; c < columnas; c++)
                pasos[f, c] = -1;                // -1: todavía no visitada

        var cola = new Queue<(int F, int C)>();
        pasos[0, 0] = 0;
        cola.Enqueue((0, 0));
        (int Df, int Dc)[] movimientos = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        while (cola.Count > 0)
        {
            var (f, c) = cola.Dequeue();
            if (f == filas - 1 && c == columnas - 1) return pasos[f, c];
            foreach (var (df, dc) in movimientos)
            {
                int nf = f + df, nc = c + dc;
                if (nf < 0 || nc < 0 || nf >= filas || nc >= columnas) continue;
                if (!libre[nf, nc] || pasos[nf, nc] != -1) continue;
                pasos[nf, nc] = pasos[f, c] + 1;
                cola.Enqueue((nf, nc));
            }
        }
        return -1;
    }
}

public static class Problema6
{
    // Ruta crítica: con gente de sobra para trabajar en paralelo, ¿cuánto tarda el proyecto completo?
    // Cada tarea empieza cuando terminó la última de sus dependencias. Orden topológico y, sobre él,
    // el inicio más temprano de cada tarea. O(V + E). Devuelve -1 si hay un ciclo.
    public static int DuracionMinima(int[] duracion, IEnumerable<(int Antes, int Despues)> dependencias)
    {
        int n = duracion.Length;
        var siguientes = new List<int>[n];
        for (int i = 0; i < n; i++) siguientes[i] = [];
        var pendientes = new int[n];
        foreach (var (antes, despues) in dependencias)
        {
            siguientes[antes].Add(despues);
            pendientes[despues]++;
        }

        var inicio = new int[n];                 // el momento más temprano en que puede empezar
        var listas = new Queue<int>();
        for (int i = 0; i < n; i++)
            if (pendientes[i] == 0) listas.Enqueue(i);

        int procesadas = 0, fin = 0;
        while (listas.Count > 0)
        {
            int t = listas.Dequeue();
            procesadas++;
            int terminaEn = inicio[t] + duracion[t];
            fin = Math.Max(fin, terminaEn);
            foreach (int s in siguientes[t])
            {
                inicio[s] = Math.Max(inicio[s], terminaEn);
                if (--pendientes[s] == 0) listas.Enqueue(s);
            }
        }
        return procesadas == n ? fin : -1;
    }
}
