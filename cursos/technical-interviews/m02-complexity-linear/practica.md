# Práctica - Cinco problemas en voz alta

## Objetivo

Resolver cinco problemas cortos como se resuelven en una entrevista: aclarando primero, diciendo la fuerza bruta y su costo, mejorándola con la estructura correcta y probando a mano los casos borde. Lo que se practica no es la respuesta, sino el recorrido para llegar a ella hablando.

**Lecturas de apoyo:** [12 - Colecciones y LINQ](../../../02-programacion/csharp/12-collections.md) y [20 - Estructuras de datos y complejidad](../../../02-programacion/csharp/20-estructuras-de-datos.md).

## Cómo se trabaja

En parejas, 8 minutos por problema. Una persona resuelve en voz alta; la otra solo puede hacer preguntas de aclaración y tomar el tiempo. Cambian de papel en cada problema. Para cada uno:

1. Repite el problema con tus palabras y pregunta lo que no esté claro.
2. Di la fuerza bruta y su costo, aunque no la escribas.
3. Escribe la mejor versión que encuentres en C#.
4. Recórrela a mano con un caso normal y dos casos borde.
5. Di su complejidad en tiempo y en espacio, y por qué.

Las soluciones de abajo compilan y tienen pruebas xUnit que cubren cada caso borde mencionado. **Léelas después de intentarlo.**

---

## Problema 1. ¿Hay repetidos?

**Enunciado.** Dado un arreglo de enteros, devuelve `true` si algún valor aparece más de una vez.

**Aclaraciones que conviene pedir:** ¿puede venir vacío? (sí: no hay repetidos). ¿Hay negativos? (sí). ¿Importa cuál se repite? (no).

**Pistas.**
1. La fuerza bruta compara cada par: ¿cuánto cuesta?
2. Ordenar primero deja los repetidos juntos: ¿cuánto cuesta eso?
3. ¿Qué estructura contesta "¿ya vi este valor?" en O(1)?

**Solución.**

```csharp
// O(n) en tiempo y O(n) en espacio. HashSet.Add devuelve false si el valor ya estaba.
public static bool TieneDuplicados(int[] numeros)
{
    var vistos = new HashSet<int>();
    foreach (int x in numeros)
        if (!vistos.Add(x))
            return true;
    return false;
}
```

**Cómo explicarla.** "Tres caminos: comparar todos contra todos es O(n²) sin memoria extra; ordenar y comparar vecinos es O(n log n) y modifica el arreglo o lo copia; un conjunto es O(n) en tiempo y O(n) en espacio. Elijo el conjunto, y aprovecho que `Add` ya me dice si el valor estaba, así no busco dos veces."

**Casos borde probados:** arreglo vacío, un solo elemento, sin repetidos, repetidos negativos.

---

## Problema 2. El primer carácter que no se repite

**Enunciado.** Dada una cadena, devuelve la posición del primer carácter que aparece exactamente una vez, o `-1` si no hay ninguno.

**Aclaraciones:** ¿mayúsculas y minúsculas cuentan como distintas? (sí). ¿Cadena vacía? (devuelve -1).

**Pistas.**
1. Para saber si un carácter es único hay que haber visto la cadena completa.
2. Dos pasadas no cambian la complejidad: O(2n) es O(n).

**Solución.**

```csharp
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
```

**Cómo explicarla.** "La primera pasada cuenta, la segunda recorre en el orden original y devuelve el primero con cuenta 1. El espacio es O(k), con k caracteres distintos; si el alfabeto es fijo, eso es una constante."

**Casos borde probados:** cadena vacía, un carácter, todos repetidos (`"aabb"` da -1), mayúsculas distintas de minúsculas (`"Aa"` da 0). Con `"abacabad"` la respuesta es 3 (la `c`), no 7: la `d` también es única, pero llega después.

---

## Problema 3. ¿Son anagramas?

**Enunciado.** Dadas dos cadenas, di si una es reacomodo de la otra: los mismos caracteres, las mismas veces.

**Aclaraciones:** ¿se ignoran mayúsculas o espacios? (en esta versión no: se compara carácter por carácter). ¿Hay acentos y eñes? (sí, y cuentan como caracteres propios).

**Pistas.**
1. Ordenar las dos y compararlas funciona: ¿cuánto cuesta?
2. Si los largos son distintos, ya terminaste.
3. Un solo diccionario: suma con una cadena y resta con la otra.

**Solución.**

```csharp
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
```

**Cómo explicarla.** "Ordenar sería O(n log n). Con un conteo es O(n): sumo los caracteres de la primera y resto los de la segunda; si alguno queda negativo, la segunda tiene un carácter de más. Como los largos son iguales, si nadie quedó negativo, nadie quedó positivo."

**Casos borde probados:** dos cadenas vacías (sí son anagramas), largos distintos, mismas letras con distinta frecuencia (`"aab"` y `"abb"`), mayúsculas (`"Roma"` y `"amor"` no lo son en esta versión), y caracteres con acento (`"ñandú"` y `"dúnña"` sí).

---

## Problema 4. Una cola hecha con dos pilas

**Enunciado.** Implementa una cola (`Enqueue`, `Dequeue`, `Peek`, `Count`) usando solo dos `Stack<T>`. `Dequeue` y `Peek` deben lanzar `InvalidOperationException` si la cola está vacía.

**Aclaraciones:** ¿qué costo se espera? (que el promedio por operación sea O(1)).

**Pistas.**
1. Pasar todos los elementos de una pila a otra invierte su orden.
2. No hace falta pasarlos en cada `Dequeue`: solo cuando la pila de salida se vacía.

**Solución.**

```csharp
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
```

**Cómo explicarla.** "Un `Dequeue` aislado puede costar O(n), cuando vacía la pila de entrada. Pero cada elemento se mueve de una pila a la otra una sola vez en toda su vida, así que sobre n operaciones el trabajo total es O(n): O(1) amortizado por operación." Este es el mejor ejemplo para explicar qué significa amortizado.

**Casos borde probados:** sacar de una cola vacía (lanza), meter y sacar uno y volver a sacar (lanza), y mil operaciones al azar intercalando `Enqueue` y `Dequeue`, comparadas contra una `Queue<T>` real.

---

## Problema 5. La mejor ventana de k elementos

**Enunciado.** Dado un arreglo de enteros y un número `k`, devuelve la mayor suma de `k` elementos consecutivos. Si `k` es menor que 1 o mayor que el arreglo, lanza `ArgumentOutOfRangeException`.

**Aclaraciones:** ¿puede haber negativos? (sí, incluso todos). ¿Los valores pueden ser grandes? (sí: la suma puede no caber en `int`).

**Pistas.**
1. La fuerza bruta suma cada ventana desde cero: O(n × k).
2. Al mover la ventana una posición, entra un elemento y sale otro.

**Solución.**

```csharp
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
```

**Cómo explicarla.** "Calculo la primera ventana y luego la deslizo: sumo el que entra y resto el que sale. Cada elemento entra y sale una vez: O(n). La suma va en `long` y la resta se hace ya en `long`, porque `int.MaxValue - int.MinValue` desborda un `int`."

**Casos borde probados:** un solo elemento, todos negativos con `k = 1` y con `k` igual al largo, valores extremos (`int.MaxValue` dos veces seguidas da `2 × int.MaxValue` sin desbordar) y `k` fuera de rango por abajo y por arriba.

---

## Checklist de la práctica

- [ ] Resolví al menos tres problemas en voz alta, con la otra persona tomando el tiempo.
- [ ] En cada uno dije la fuerza bruta y su costo antes de mejorarla.
- [ ] Probé a mano cada solución con al menos dos casos borde.
- [ ] Dije la complejidad en tiempo y en espacio, con el porqué.
- [ ] Puedo explicar qué significa O(1) amortizado con el problema 4.
- [ ] Anoté en qué problema me atoré y en qué paso.
