namespace Entrevistas.M03;

// Intervalo semiabierto [Inicio, Fin): una reunión de 9 a 10 no choca con otra de 10 a 11.
public sealed record Intervalo(int Inicio, int Fin);

public static class Busqueda
{
    // O(log n). El arreglo debe venir ordenado de menor a mayor.
    public static int Binaria(int[] datos, int objetivo)
    {
        int lo = 0, hi = datos.Length - 1;
        while (lo <= hi)
        {
            int medio = lo + (hi - lo) / 2; // (lo + hi) / 2 puede desbordar
            if (datos[medio] == objetivo) return medio;
            if (datos[medio] < objetivo) lo = medio + 1;
            else hi = medio - 1;
        }
        return -1;
    }

    // Búsqueda sobre la respuesta: el menor x en [lo, hi) para el que la condición es verdadera.
    // La condición debe ser monótona: falsa, falsa, ..., verdadera, verdadera. Si nunca se cumple, devuelve hi.
    // O(log(hi - lo)) evaluaciones de la condición.
    public static int PrimerVerdadero(int lo, int hi, Func<int, bool> condicion)
    {
        while (lo < hi)
        {
            int medio = lo + (hi - lo) / 2;
            if (condicion(medio)) hi = medio;
            else lo = medio + 1;
        }
        return lo;
    }

    // Capacidad mínima de un camión para repartir los paquetes, en orden, en a lo más `dias` viajes
    // (un viaje por día). O(n log S): log S intentos de capacidad (S = suma de pesos), cada uno de O(n).
    public static int CapacidadMinima(int[] pesos, int dias)
    {
        int menor = pesos.Max();                 // nadie carga menos que el paquete más pesado
        int mayor = checked((int)pesos.Sum(p => (long)p)); // todo en un viaje siempre alcanza
        return PrimerVerdadero(menor, mayor, capacidad => DiasNecesarios(pesos, capacidad) <= dias);
    }

    private static int DiasNecesarios(int[] pesos, int capacidad)
    {
        int dias = 1, carga = 0;
        foreach (int p in pesos)
        {
            if (carga + p > capacidad) { dias++; carga = 0; }
            carga += p;
        }
        return dias;
    }
}

public static class Reuniones
{
    // Ordenar primero: O(n log n) por el ordenamiento, más una pasada O(n).
    public static bool HayTraslape(IEnumerable<Intervalo> reuniones)
    {
        var ordenadas = reuniones.OrderBy(r => r.Inicio).ToList();
        for (int i = 1; i < ordenadas.Count; i++)
            if (ordenadas[i].Inicio < ordenadas[i - 1].Fin)
                return true;
        return false;
    }
}
