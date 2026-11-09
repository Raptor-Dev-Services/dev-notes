using Entrevistas.M03;

namespace Entrevistas.M04.Practica;

public static class ProblemaA
{
    // Mínimo de salas: ordenar por inicio y llevar en un montículo la hora de fin de cada sala ocupada.
    // Si la sala que se libera primero ya está libre, se reutiliza. O(n log n) en tiempo, O(n) en espacio.
    public static int MinimoDeSalas(IEnumerable<Intervalo> reuniones)
    {
        var finesPorSala = new PriorityQueue<int, int>();
        foreach (var r in reuniones.OrderBy(r => r.Inicio))
        {
            if (finesPorSala.Count > 0 && finesPorSala.Peek() <= r.Inicio)
                finesPorSala.Dequeue();          // esa sala ya se desocupó: se reutiliza
            finesPorSala.Enqueue(r.Fin, r.Fin);
        }
        return finesPorSala.Count;
    }
}

public static class ProblemaB
{
    // Subsecuencia estrictamente creciente más larga.
    // Primera versión: largo[i] = la más larga que termina en i. O(n²) en tiempo, O(n) en espacio.
    public static int LongitudCuadratica(int[] datos)
    {
        if (datos.Length == 0) return 0;
        var largo = new int[datos.Length];
        int mejor = 0;
        for (int i = 0; i < datos.Length; i++)
        {
            largo[i] = 1;
            for (int j = 0; j < i; j++)
                if (datos[j] < datos[i])
                    largo[i] = Math.Max(largo[i], largo[j] + 1);
            mejor = Math.Max(mejor, largo[i]);
        }
        return mejor;
    }

    // Mejora: colas[k] = el menor final posible de una subsecuencia creciente de largo k + 1.
    // colas siempre está ordenada, así que se busca con búsqueda binaria. O(n log n) en tiempo, O(n) en espacio.
    public static int LongitudNLogN(int[] datos)
    {
        var colas = new List<int>();
        foreach (int x in datos)
        {
            int i = colas.BinarySearch(x);
            if (i < 0) i = ~i;                   // no está: ~i es donde debería ir
            if (i == colas.Count) colas.Add(x);  // extiende la subsecuencia más larga
            else colas[i] = x;                   // mejora un final: más chico deja más espacio
        }
        return colas.Count;
    }
}

public static class ProblemaC
{
    // Combinar intervalos que se traslapan o se tocan: [1, 3] y [3, 5] quedan como [1, 5].
    // O(n log n) por el ordenamiento, más una pasada O(n).
    public static List<Intervalo> Combinar(IEnumerable<Intervalo> intervalos)
    {
        var resultado = new List<Intervalo>();
        foreach (var actual in intervalos.OrderBy(i => i.Inicio))
        {
            if (resultado.Count > 0 && actual.Inicio <= resultado[^1].Fin)
            {
                var ultimo = resultado[^1];
                resultado[^1] = ultimo with { Fin = Math.Max(ultimo.Fin, actual.Fin) };
            }
            else
            {
                resultado.Add(actual);
            }
        }
        return resultado;
    }
}
