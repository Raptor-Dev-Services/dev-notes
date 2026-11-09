namespace Entrevistas.M02;

// Fragmentos del quiz del módulo 2, instrumentados para que una prueba compruebe la complejidad.
public static class QuizM02
{
    public static int Mitades(int n)               // m02-01: O(log n)
    {
        int pasos = 0;
        for (int i = n; i > 1; i /= 2) { pasos++; }
        return pasos;
    }

    public static int ConstanteInterna(int[] datos) // m02-02: O(n)
    {
        int pasos = 0, suma = 0, n = datos.Length;
        for (int i = 0; i < n; i++) for (int j = 0; j < 5; j++) { suma += datos[i]; pasos++; }
        return pasos;
    }

    public static int Triangular(int n)             // m02-07: O(n²)
    {
        int pasos = 0;
        for (int i = 0; i < n; i++) for (int j = 0; j < i; j++) pasos++;
        return pasos;
    }

    // m02-04: cuántos elementos se mueven al vaciar una lista quitando siempre el primero.
    public static long MovimientosConRemoveAt(int n)
    {
        var pendientes = Enumerable.Range(0, n).ToList();
        long movidos = 0;
        while (pendientes.Count > 0) { movidos += pendientes.Count - 1; pendientes.RemoveAt(0); }
        return movidos;
    }

    public static int ProcesadosConCola(int n)
    {
        var pendientes = new Queue<int>(Enumerable.Range(0, n));
        int procesados = 0;
        while (pendientes.Count > 0) { pendientes.Dequeue(); procesados++; }
        return procesados;
    }
}
