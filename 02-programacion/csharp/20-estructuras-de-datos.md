# 20 - Estructuras de datos y complejidad

La notación Big O responde una sola pregunta: **cuánto empeora un algoritmo cuando los datos crecen**. Esta nota la aplica a las colecciones de .NET (qué cuesta cada operación de `List<T>`, `Dictionary<TKey, TValue>`, `Queue<T>` y compañía), a árboles y a grafos, que es donde se concentran las preguntas de una entrevista técnica. Para el uso diario de cada colección y de LINQ, ver [12 - Colecciones y LINQ](12-collections.md).

> Fuente: *C# Data Structures and Algorithms* (Jamro, 2024): caps. 2 a 8. *The Imposter's Handbook* (Conery): "Big O" y "Data Structures". *50 Algorithms Every Programmer Should Know* (Ahmad, 2023): caps. 3 y 5.

---

## Problema que resuelve

Con diez registros, cualquier código es rápido. Con un millón, la diferencia entre O(n) y O(n²) es la diferencia entre un segundo y varias horas. El error típico no es elegir un algoritmo exótico mal: es usar la colección equivocada sin saberlo.

- Un `List<T>.Contains` dentro de un ciclo convierte un proceso lineal en cuadrático.
- Un `Insert(0, ...)` repetido copia la lista completa en cada vuelta.
- Un árbol de categorías recorrido con consultas anidadas hace una consulta por nodo.

Saber el costo de cada operación permite detectarlo leyendo el código, antes de que lo detecte producción.

---

## Big O en una página

Big O describe cómo crece el número de pasos en función del tamaño de la entrada `n`, en el peor caso salvo que se diga otra cosa. Tres reglas bastan para casi todo:

1. **Las constantes no cuentan:** O(2n) es O(n). Importa la forma de la curva, no la pendiente.
2. **Manda el término dominante:** O(n² + n) es O(n²).
3. **Secuencia suma, anidación multiplica:** dos ciclos seguidos son O(n + m); uno dentro de otro, O(n × m).

| Clase | Nombre | Ejemplo | Pasos con n = 1,000,000 |
|-------|--------|---------|------------------------|
| O(1) | constante | leer `arreglo[i]`, buscar en un `Dictionary` | 1 |
| O(log n) | logarítmica | búsqueda binaria | unos 20 |
| O(n) | lineal | recorrer una lista | 10^6 |
| O(n log n) | lineal-logarítmica | ordenar | unos 2 × 10^7 |
| O(n²) | cuadrática | comparar todos contra todos | 10^12 |
| O(2^n) | exponencial | probar todos los subconjuntos | impracticable |

Si se supone, para redondear, una máquina que hace 10^8 operaciones simples por segundo, O(n log n) termina en una fracción de segundo y O(n²) tarda más de dos horas con el mismo millón de datos.

**El espacio también se mide.** Un algoritmo que crea un diccionario auxiliar del tamaño de la entrada usa O(n) de memoria adicional, aunque sea más rápido.

---

## Cómo leerla en el código

Dos fragmentos que confunden. Cada uno cuenta sus pasos para que una prueba lo compruebe:

```csharp
// C: la variable se duplica en cada vuelta.
public static int PasosC(int n)
{
    int pasos = 0;
    for (int i = 1; i < n; i *= 2)
        pasos++;
    return pasos;
}
```

`i` vale 1, 2, 4, 8... y se detiene al llegar a `n`: el ciclo da unas log₂ n vueltas. Con `n` = 1,000,000 son 20. **O(log n).**

```csharp
// D: el ciclo interno empieza donde va el externo.
public static int PasosD(int n)
{
    int pasos = 0;
    for (int i = 0; i < n; i++)
        for (int j = i + 1; j < n; j++)
            pasos++;
    return pasos;
}
```

Hace n(n - 1) / 2 pasos: la mitad que dos ciclos completos, pero la mitad de una curva cuadrática sigue siendo cuadrática. Al duplicar `n`, los pasos casi se cuadruplican. **O(n²).**

---

## Peor caso, promedio y amortizado

- **Peor caso:** la entrada más desfavorable. Es lo que se reporta por defecto.
- **Caso promedio:** el esperado con entradas típicas. Un `Dictionary` es O(1) en promedio y O(n) en el peor caso, si todas las claves caen en la misma cubeta.
- **Amortizado:** el promedio sobre una secuencia larga de operaciones. `List<T>.Add` a veces copia todo el arreglo interno (cuando se llena, duplica su capacidad), pero esas copias son tan raras que el costo por `Add` sale O(1) amortizado.

---

## Lo que cuesta cada colección de .NET

| Colección | Estructura interna | Leer por índice o clave | Buscar un valor | Agregar | Quitar |
|-----------|-------------------|------------------------|-----------------|---------|--------|
| `T[]` | arreglo contiguo | O(1) | O(n) | no cambia de tamaño | no cambia de tamaño |
| `List<T>` | arreglo que crece | O(1) | O(n) | O(1) amortizado al final; O(n) en medio | O(1) al final; O(n) en medio |
| `LinkedList<T>` | lista doblemente ligada | no tiene índice | O(n) | O(1) junto a un nodo conocido | O(1) con el nodo; O(n) por valor |
| `Stack<T>` | arreglo | `Peek` O(1) | O(n) | `Push` O(1) amortizado | `Pop` O(1) |
| `Queue<T>` | arreglo circular | `Peek` O(1) | O(n) | `Enqueue` O(1) amortizado | `Dequeue` O(1) |
| `Dictionary<K,V>` | tabla hash | O(1) promedio | por valor O(n) | O(1) promedio | O(1) promedio |
| `HashSet<T>` | tabla hash | no aplica | `Contains` O(1) promedio | O(1) promedio | O(1) promedio |
| `SortedDictionary<K,V>` | árbol rojo-negro | O(log n) | por valor O(n) | O(log n) | O(log n) |
| `SortedList<K,V>` | dos arreglos ordenados | O(log n) | por valor O(n) | O(n); O(log n) si llega en orden | O(n) |
| `SortedSet<T>` | árbol rojo-negro | `Min` y `Max` O(log n) | `Contains` O(log n) | O(log n) | O(log n) |
| `PriorityQueue<E,P>` | montículo de mínimos | `Peek` O(1) | no aplica | `Enqueue` O(log n) | `Dequeue` O(log n) |

`LinkedList<T>` gana en teoría al insertar en medio, pero solo si ya tienes el nodo; encontrarlo cuesta O(n). En la práctica `List<T>` casi siempre es más rápida, porque sus elementos están juntos en memoria.

---

## Arreglos y List<T>

La trampa más común es insertar al principio dentro de un ciclo:

```csharp
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
```

Si de verdad hace falta sacar y meter por ambos extremos, la estructura es una cola (`Queue<T>`) o una `LinkedList<T>`, no una lista.

---

## Pilas y colas

- **Pila (`Stack<T>`):** el último que entra es el primero que sale (LIFO). Sirve para deshacer, para emparejar aperturas con cierres y para recorrer en profundidad sin recursión.
- **Cola (`Queue<T>`):** el primero que entra es el primero que sale (FIFO). Sirve para procesar en orden de llegada y para recorrer en anchura.

```csharp
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
```

---

## Diccionarios y conjuntos

Una tabla hash calcula, a partir de la clave, en qué cubeta guardarla. Por eso buscar cuesta O(1) en promedio sin importar cuántos elementos haya. Dos condiciones:

- `GetHashCode` y `Equals` deben ser coherentes: dos claves iguales deben dar el mismo hash. Los `record` ya lo cumplen.
- La clave no debe cambiar mientras está en el diccionario; si cambia su hash, ya no se encuentra.

El patrón de entrevista más útil es **cambiar memoria por tiempo**: guardar lo que ya viste para no volver a buscarlo.

```csharp
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
```

La versión con dos ciclos anidados resuelve lo mismo en O(n²) sin memoria extra. Ninguna es "la correcta": depende de si sobra memoria o falta tiempo, y esa es justo la conversación que espera quien entrevista.

La misma idea corrige una trampa común en LINQ:

```csharp
// O(n × m): Contains recorre la lista b completa por cada elemento de a.
public static List<int> ComunesLento(List<int> a, List<int> b)
    => a.Where(x => b.Contains(x)).ToList();

// O(n + m): construir el conjunto cuesta m, y cada consulta cuesta O(1) en promedio.
public static List<int> ComunesRapido(List<int> a, List<int> b)
{
    var enB = new HashSet<int>(b);
    return a.Where(enB.Contains).ToList();
}
```

---

## Colecciones ordenadas y PriorityQueue

- **`SortedDictionary<K,V>` y `SortedSet<T>`** son árboles balanceados: todo cuesta O(log n) y se recorren en orden de clave. Útiles cuando además de buscar necesitas "el siguiente mayor" o recorrer en orden.
- **`PriorityQueue<TElement, TPriority>`** siempre entrega primero el elemento de **menor** prioridad. Encolar y desencolar cuestan O(log n). No garantiza orden entre prioridades iguales y no tiene una operación barata para cambiar la prioridad de algo ya encolado: se encola de nuevo y se descarta el viejo al sacarlo (así lo hace Dijkstra, más abajo).

---

## Ordenar y buscar

- `Array.Sort` y `List<T>.Sort` usan introsort: O(n log n) también en el peor caso, y **no son estables** (dos elementos iguales pueden cambiar de orden).
- `Enumerable.OrderBy` **es estable**. Importa cuando ordenas por un segundo criterio y quieres conservar el orden del primero.
- **Ordenar primero** es una técnica en sí misma: muchos problemas O(n²) bajan a O(n log n) porque, ya ordenados, basta comparar vecinos.

La búsqueda binaria descarta la mitad en cada paso: O(log n), siempre que los datos estén ordenados.

```csharp
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
```

La misma idea sirve para **buscar sobre la respuesta**: si una condición es falsa hasta cierto valor y verdadera de ahí en adelante, la frontera se encuentra en O(log n) evaluaciones.

```csharp
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
```

---

## Árboles

Un árbol es un grafo sin ciclos con una raíz. En un **árbol binario** cada nodo tiene a lo más dos hijos. La **altura** `h` es el número de nodos del camino más largo de la raíz a una hoja.

```csharp
public sealed class Nodo(int valor, Nodo? izq = null, Nodo? der = null)
{
    public int Valor { get; } = valor;
    public Nodo? Izq { get; } = izq;
    public Nodo? Der { get; } = der;
}
```

Los recorridos en profundidad cambian solo el momento en que se visita la raíz: antes de los hijos (preorden), entre ellos (inorden) o después (postorden). Todos son O(n) en tiempo y O(h) en espacio, por la pila de llamadas.

```csharp
public static void Inorden(Nodo? nodo, List<int> salida)
{
    if (nodo is null) return;
    Inorden(nodo.Izq, salida);
    salida.Add(nodo.Valor);      // la raíz en medio: en un BST sale ordenado
    Inorden(nodo.Der, salida);
}
```

El recorrido por niveles usa una cola. Es O(n) en tiempo y, además del resultado, guarda en la cola a lo más dos niveles a la vez:

```csharp
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
```

En un **árbol binario de búsqueda (BST)** todo lo de la izquierda es menor que el nodo y todo lo de la derecha es mayor. Buscar cuesta O(h): O(log n) si está balanceado y O(n) si degeneró en una sola rama (por ejemplo, al insertar datos ya ordenados). Por eso `SortedSet<T>` y `SortedDictionary<K,V>` usan árboles que se rebalancean solos.

El error clásico al validar un BST es comparar cada nodo solo con su padre. La regla es para **todos** sus ancestros, y se cumple pasando un rango:

```csharp
// Cada nodo debe caer dentro del rango que le heredan sus ancestros, no solo comparar con su padre.
// O(n) en tiempo, O(h) en espacio. No admite valores repetidos.
public static bool EsBst(Nodo? raiz) => EsBst(raiz, long.MinValue, long.MaxValue);

private static bool EsBst(Nodo? nodo, long minimo, long maximo)
{
    if (nodo is null) return true;
    if (nodo.Valor <= minimo || nodo.Valor >= maximo) return false;
    return EsBst(nodo.Izq, minimo, nodo.Valor) && EsBst(nodo.Der, nodo.Valor, maximo);
}
```

---

## Grafos

Un grafo son **nodos** (V) unidos por **aristas** (E). Puede ser dirigido o no, y las aristas pueden tener peso. Muchos problemas de negocio son grafos disfrazados: sucursales y rutas, tareas y sus dependencias, personas y a quién reportan, categorías y subcategorías.

| Representación | Memoria | Saber si hay arista entre a y b | Recorrer los vecinos de a |
|----------------|---------|---------------------------------|---------------------------|
| Lista de adyacencia (`Dictionary<T, List<T>>`) | O(V + E) | O(grado de a) | O(grado de a) |
| Matriz de adyacencia (`bool[V, V]`) | O(V²) | O(1) | O(V) |

La lista de adyacencia es la opción por defecto: casi todos los grafos reales tienen muchas menos aristas que V².

### En anchura (BFS): el camino con menos aristas

```csharp
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
```

BFS encuentra el camino más corto **solo si las aristas no tienen peso** (o todas pesan lo mismo). Con pesos distintos hace falta Dijkstra.

### En profundidad (DFS): qué se alcanza

DFS avanza por un camino hasta el fondo antes de regresar. Sirve para saber qué es alcanzable, contar componentes y detectar ciclos. Con una pila explícita no depende del tamaño de la pila de llamadas:

```csharp
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
```

### Orden topológico: dependencias

Si las tareas dependen unas de otras, el orden topológico da una secuencia que respeta todas las dependencias. Si hay un ciclo, ese orden no existe, y el algoritmo lo detecta:

```csharp
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
```

### Dijkstra: el camino más barato con pesos

Con pesos no negativos, Dijkstra saca siempre el nodo pendiente más cercano, y para eso usa una cola de prioridad:

```csharp
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
```

Con pesos negativos Dijkstra da resultados incorrectos; ahí se usa Bellman-Ford, que es O(V × E).

---

## Qué estructura para qué

| Señal en el enunciado | Estructura o técnica | Costo típico |
|-----------------------|---------------------|--------------|
| "¿Ya apareció?", "sin repetidos" | `HashSet<T>` | O(1) por consulta |
| "Cuántas veces", "agrupar por" | `Dictionary<K, int>` o `GroupBy` | O(n) |
| "Deshacer", "emparejar aperturas y cierres" | `Stack<T>` | O(n) |
| "En orden de llegada", "por niveles" | `Queue<T>` | O(n) |
| "Los k mayores", "el siguiente más urgente" | `PriorityQueue<E, P>` | O(n log k) o O(log n) por operación |
| "Datos ordenados", "el primer día en que..." | búsqueda binaria | O(log n) |
| "Rango ordenado", "el siguiente mayor" | `SortedSet<T>` o `SortedDictionary<K, V>` | O(log n) |
| "Subarreglo de tamaño k", "ventana" | ventana deslizante | O(n) |
| "Conectado", "alcanzable", "menos pasos" | grafo con BFS o DFS | O(V + E) |
| "Antes de", "depende de", "prerrequisito" | orden topológico | O(V + E) |
| "Ruta más barata" con costos | Dijkstra | O((V + E) log V) |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Big O | Notación que describe cómo crece el costo de un algoritmo cuando crece la entrada, sin constantes ni términos menores |
| Complejidad amortizada | Costo promedio por operación sobre una secuencia larga, aunque alguna operación aislada sea cara |
| Tabla hash | Estructura que calcula la posición de cada clave a partir de su hash; búsqueda O(1) en promedio |
| Árbol binario de búsqueda (BST) | Árbol binario donde todo lo de la izquierda es menor que el nodo y todo lo de la derecha es mayor |
| Altura de un árbol | Número de nodos del camino más largo entre la raíz y una hoja; define el costo de buscar en un BST |
| Árbol balanceado | Árbol que se reacomoda para que su altura se mantenga en O(log n), como el rojo-negro de `SortedSet<T>` |
| Montículo (heap) | Árbol guardado en un arreglo que mantiene el mínimo arriba; base de `PriorityQueue<E, P>` |
| Lista de adyacencia | Representación de un grafo que guarda, por cada nodo, la lista de sus vecinos; memoria O(V + E) |
| BFS | Recorrido en anchura con una cola; da el camino con menos aristas en grafos sin pesos |
| DFS | Recorrido en profundidad con pila o recursión; sirve para alcanzabilidad, componentes y ciclos |
| Orden topológico | Secuencia de los nodos de un grafo dirigido sin ciclos que respeta todas sus dependencias |
| Dijkstra | Algoritmo de camino más barato con pesos no negativos, apoyado en una cola de prioridad |
| Ordenamiento estable | El que conserva el orden original entre elementos iguales; `OrderBy` lo es y `Array.Sort` no |

---

*Rogelio Arriaga Gonzalez*
