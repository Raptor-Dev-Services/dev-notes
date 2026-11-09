using Entrevistas.M03;

namespace Entrevistas.M04;

public static class DivideYVenceras
{
    // Merge sort: log n niveles de división y O(n) de trabajo por nivel: O(n log n) en tiempo, O(n) en espacio.
    // Es estable: ante empate toma primero el de la izquierda.
    public static int[] MergeSort(int[] datos)
    {
        if (datos.Length <= 1) return (int[])datos.Clone();
        int medio = datos.Length / 2;
        int[] izq = MergeSort(datos[..medio]);
        int[] der = MergeSort(datos[medio..]);
        return Mezclar(izq, der);
    }

    private static int[] Mezclar(int[] a, int[] b)
    {
        var salida = new int[a.Length + b.Length];
        int i = 0, j = 0, k = 0;
        while (i < a.Length && j < b.Length)
            salida[k++] = a[i] <= b[j] ? a[i++] : b[j++];
        while (i < a.Length) salida[k++] = a[i++];
        while (j < b.Length) salida[k++] = b[j++];
        return salida;
    }

    // Potencia rápida: b^e mod m con O(log e) multiplicaciones. Pide b >= 0, e >= 0 y 1 <= m < 3e9,
    // para que el producto de dos residuos quepa en long.
    public static long PotenciaModular(long b, long e, long m)
    {
        long resultado = 1 % m;
        b %= m;
        while (e > 0)
        {
            if ((e & 1) == 1) resultado = resultado * b % m;
            b = b * b % m;
            e >>= 1;
        }
        return resultado;
    }
}

public static class Voraz
{
    // Ordenar por hora de fin y tomar cada reunión que empiece cuando terminó la anterior.
    // O(n log n) por el ordenamiento. La elección voraz es segura: terminar antes nunca estorba.
    public static int MaximoDeReuniones(IEnumerable<Intervalo> reuniones)
    {
        int cuenta = 0;
        int finUltima = int.MinValue;
        foreach (var r in reuniones.OrderBy(r => r.Fin))
        {
            if (r.Inicio >= finUltima)
            {
                cuenta++;
                finUltima = r.Fin;
            }
        }
        return cuenta;
    }

    // Cambio voraz: siempre la moneda más grande que quepa. Rápido, pero NO siempre óptimo.
    public static int CambioVoraz(int[] monedas, int monto)
    {
        int cuenta = 0;
        foreach (int moneda in monedas.OrderByDescending(m => m))
        {
            cuenta += monto / moneda;
            monto %= moneda;
        }
        return monto == 0 ? cuenta : -1;
    }
}

public static class Dinamica
{
    // Sin memoria: cada llamada abre dos más. El número de llamadas crece de forma exponencial (cota O(2^n)).
    public static long FibIngenuo(int n) => n < 2 ? n : FibIngenuo(n - 1) + FibIngenuo(n - 2);

    // De arriba hacia abajo con memoria: cada valor se calcula una vez. O(n) en tiempo y en espacio.
    public static long FibMemo(int n, Dictionary<int, long>? memo = null)
    {
        memo ??= [];
        if (n < 2) return n;
        if (memo.TryGetValue(n, out long hecho)) return hecho;
        long valor = FibMemo(n - 1, memo) + FibMemo(n - 2, memo);
        memo[n] = valor;
        return valor;
    }

    // De abajo hacia arriba: solo hacen falta los dos anteriores. O(n) en tiempo, O(1) en espacio.
    public static long FibTabla(int n)
    {
        long anterior = 0, actual = 1;
        if (n == 0) return 0;
        for (int i = 2; i <= n; i++)
            (anterior, actual) = (actual, anterior + actual);
        return actual;
    }

    // Mínimo de monedas para formar el monto, o -1 si no se puede.
    // Estado: minimo[x] = menos monedas para formar x. O(monto × k) en tiempo y O(monto) en espacio.
    public static int CambioMinimo(int[] monedas, int monto)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monto);
        const int Imposible = int.MaxValue;
        var minimo = new int[monto + 1];
        Array.Fill(minimo, Imposible);
        minimo[0] = 0;

        for (int x = 1; x <= monto; x++)
            foreach (int moneda in monedas)
                if (moneda <= x && minimo[x - moneda] != Imposible)
                    minimo[x] = Math.Min(minimo[x], minimo[x - moneda] + 1);

        return minimo[monto] == Imposible ? -1 : minimo[monto];
    }

    // Kadane: la mejor suma que TERMINA en i es el elemento solo o el elemento más la mejor anterior.
    // O(n) en tiempo, O(1) en espacio. El arreglo no puede venir vacío.
    public static long MaximaSumaSubarreglo(int[] datos)
    {
        if (datos.Length == 0) throw new ArgumentException("El arreglo no puede venir vacío.", nameof(datos));
        long terminaAqui = datos[0], mejor = datos[0];
        for (int i = 1; i < datos.Length; i++)
        {
            terminaAqui = Math.Max(datos[i], terminaAqui + datos[i]);
            mejor = Math.Max(mejor, terminaAqui);
        }
        return mejor;
    }
}
