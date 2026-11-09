namespace Entrevistas.M02;

// Fragmentos de "¿cuál es la complejidad?". Cada uno cuenta sus pasos para que la prueba compruebe
// que el crecimiento es el que dice la lámina.
public static class Complejidad
{
    // A: un ciclo que recorre n elementos.
    public static int PasosA(int n)
    {
        int pasos = 0;
        for (int i = 0; i < n; i++)
            pasos++;
        return pasos;
    }

    // B: dos ciclos anidados sobre los mismos n elementos.
    public static int PasosB(int n)
    {
        int pasos = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                pasos++;
        return pasos;
    }

    // C: la variable se duplica en cada vuelta.
    public static int PasosC(int n)
    {
        int pasos = 0;
        for (int i = 1; i < n; i *= 2)
            pasos++;
        return pasos;
    }

    // D: el ciclo interno empieza donde va el externo.
    public static int PasosD(int n)
    {
        int pasos = 0;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                pasos++;
        return pasos;
    }

    // E: dos ciclos uno después del otro, sobre colecciones distintas.
    public static int PasosE(int n, int m)
    {
        int pasos = 0;
        for (int i = 0; i < n; i++)
            pasos++;
        for (int j = 0; j < m; j++)
            pasos++;
        return pasos;
    }

    // F: un ciclo de n vueltas con un ciclo que se duplica adentro.
    public static int PasosF(int n)
    {
        int pasos = 0;
        for (int i = 0; i < n; i++)
            for (int j = 1; j < n; j *= 2)
                pasos++;
        return pasos;
    }
}
