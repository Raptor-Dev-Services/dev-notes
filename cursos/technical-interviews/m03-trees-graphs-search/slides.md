<!-- _class: lead -->

# Módulo 3 - Árboles, grafos y búsqueda

### Preparación para entrevistas técnicas

**Rogelio Arriaga - Raptor Dev Services**

---

## Hoy

> Muchos problemas de entrevista son un grafo disfrazado.

- **Parte 1:** búsqueda binaria, búsqueda sobre la respuesta y ordenar primero
- **Parte 2:** árboles: recorridos, altura y árboles de búsqueda
- **Parte 3:** grafos: BFS, DFS, cuadrículas y dependencias
- **Práctica:** seis problemas cronometrados en parejas, una persona resuelve y la otra entrevista

Lectura: [20 - Estructuras de datos y complejidad](../../../02-programacion/csharp/20-estructuras-de-datos.md)

---

## El grafo detrás del enunciado

| El enunciado dice | El grafo es |
|-------------------|-------------|
| "¿Con cuántas transferencias llego de la sucursal A a la B?" | Sucursales como nodos, rutas como aristas; BFS |
| "¿En qué orden instalo estos paquetes?" | Paquetes y dependencias; orden topológico |
| "¿Cuántas zonas de tierra hay en este mapa?" | Celdas vecinas; contar componentes |
| "¿A quién reporta, directa o indirectamente?" | Organigrama como árbol; recorrido hacia arriba |
| "¿Cuál es la ruta más barata?" | Aristas con costo; Dijkstra |

La primera tarea en la entrevista es **nombrar los nodos y las aristas en voz alta.**

---

<!-- _class: lead -->

# Parte 1
## Buscar y ordenar

---

## Búsqueda binaria

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

Cada vuelta descarta la mitad: con un millón de datos, unas 20 comparaciones.

---

## Los errores típicos

- **Ciclo infinito:** usar `lo = medio` en vez de `lo = medio + 1`.
- **Uno de más o de menos:** mezclar `hi = Length - 1` con `while (lo < hi)`. Elige una convención (cerrada o semiabierta) y no la mezcles.
- **Desbordamiento:** `(lo + hi) / 2` se pasa de `int.MaxValue` con índices grandes; `lo + (hi - lo) / 2`, no.
- **Datos sin ordenar:** la búsqueda binaria sobre datos desordenados no falla: **devuelve basura**.

En .NET ya existen `Array.BinarySearch` y `List<T>.BinarySearch`. Si no encuentran el valor, devuelven el complemento (`~i`) de donde debería ir.

---

## Búsqueda sobre la respuesta

Si una condición es **falsa, falsa, ..., verdadera, verdadera**, la frontera se encuentra en O(log n) evaluaciones, aunque no haya ningún arreglo.

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

Señales: "el mínimo que alcanza", "el primer día en que", "la capacidad más chica que".

---

## Ejemplo: la capacidad más chica

"Hay que repartir paquetes, en orden, con un viaje por día. ¿Cuál es la capacidad mínima del camión para terminar en `dias` días?"

```csharp
// Capacidad mínima de un camión para repartir los paquetes, en orden, en a lo más `dias` viajes
// (un viaje por día). O(n log S): log S intentos de capacidad (S = suma de pesos), cada uno de O(n).
public static int CapacidadMinima(int[] pesos, int dias)
{
    int menor = pesos.Max();                 // nadie carga menos que el paquete más pesado
    int mayor = checked((int)pesos.Sum(p => (long)p)); // todo en un viaje siempre alcanza
    return PrimerVerdadero(menor, mayor, capacidad => DiasNecesarios(pesos, capacidad) <= dias);
}
```

Si una capacidad alcanza, cualquier capacidad mayor también: la condición es monótona.

<!--
Nota del instructor: pide primero la fuerza bruta (probar cada capacidad de menor a mayor, O(n x S)). Luego pregunta qué propiedad permite saltarse capacidades: la monotonía. DiasNecesarios es un recorrido voraz que llena el camión hasta que el siguiente paquete no cabe; está completo en el proyecto de pruebas.
-->

---

## Ordenar en .NET

| Método | Algoritmo | Costo | ¿Estable? |
|--------|-----------|-------|-----------|
| `Array.Sort`, `List<T>.Sort` | introsort | O(n log n), también en el peor caso | No |
| `Enumerable.OrderBy` | ordenamiento estable | O(n log n) | Sí |

**Estable** significa que dos elementos iguales conservan su orden original. Importa cuando ordenas por un segundo criterio: primero por fecha, luego por cliente, y quieres que dentro de cada cliente sigan por fecha.

---

## Ejercicio en vivo: ¿se traslapan dos reuniones?

"Dada una lista de reuniones con hora de inicio y de fin, ¿hay dos que choquen?"

1. ¿Una reunión de 9 a 10 choca con otra de 10 a 11?
2. ¿Cuál es la fuerza bruta y su costo?
3. Si estuvieran ordenadas por hora de inicio, ¿con cuántas reuniones tendrías que comparar cada una?

<!--
Nota del instructor: la pregunta 1 es la aclaración que más se olvida. En el código del curso los intervalos son semiabiertos [inicio, fin): 9-10 y 10-11 no chocan. La fuerza bruta compara todos los pares, O(n²). Ordenadas, basta comparar cada una con la anterior.
-->

---

## La solución: ordenar primero

```csharp
// Intervalo semiabierto [Inicio, Fin): una reunión de 9 a 10 no choca con otra de 10 a 11.
public sealed record Intervalo(int Inicio, int Fin);
```

```csharp
// Ordenar primero: O(n log n) por el ordenamiento, más una pasada O(n).
public static bool HayTraslape(IEnumerable<Intervalo> reuniones)
{
    var ordenadas = reuniones.OrderBy(r => r.Inicio).ToList();
    for (int i = 1; i < ordenadas.Count; i++)
        if (ordenadas[i].Inicio < ordenadas[i - 1].Fin)
            return true;
    return false;
}
```

Ordenar primero convierte muchos problemas O(n²) en O(n log n): ya ordenados, basta mirar al vecino.

---

<!-- _class: lead -->

# Parte 2
## Árboles

---

## Vocabulario

```text
            8            raíz
          /   \
         3     10        8 es padre de 3 y de 10
        / \      \
       1   6      14     1, 4, 7 y 13 son hojas
          / \    /
         4   7  13       altura: 4 niveles
```

- **Árbol binario:** cada nodo tiene a lo más dos hijos.
- **Altura (h):** nodos del camino más largo de la raíz a una hoja.
- **Árbol binario de búsqueda (BST):** a la izquierda, todo menor; a la derecha, todo mayor.

---

## El nodo en C#

```csharp
public sealed class Nodo(int valor, Nodo? izq = null, Nodo? der = null)
{
    public int Valor { get; } = valor;
    public Nodo? Izq { get; } = izq;
    public Nodo? Der { get; } = der;
}
```

En una entrevista, **define el nodo antes de escribir el algoritmo**. Quien entrevista suele dártelo, pero si no, preguntar cómo es la estructura es una aclaración válida.

---

## Recorridos en profundidad

```csharp
public static void Inorden(Nodo? nodo, List<int> salida)
{
    if (nodo is null) return;
    Inorden(nodo.Izq, salida);
    salida.Add(nodo.Valor);      // la raíz en medio: en un BST sale ordenado
    Inorden(nodo.Der, salida);
}
```

| Recorrido | Cuándo visita la raíz | En el árbol de ejemplo |
|-----------|----------------------|------------------------|
| Preorden | antes de los hijos | 8, 3, 1, 6, 4, 7, 10, 14, 13 |
| Inorden | entre los hijos | 1, 3, 4, 6, 7, 8, 10, 13, 14 |
| Postorden | después de los hijos | 1, 4, 7, 6, 3, 13, 14, 10, 8 |

Los tres son O(n) en tiempo y O(h) en espacio, por la pila de llamadas.

---

## Por niveles: con una cola

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

El truco es guardar `cola.Count` **antes** de sacar: eso es exactamente lo que mide el nivel actual.

---

## Altura: la recursión en una línea

```csharp
// O(n) en tiempo, O(h) en espacio. Un árbol vacío mide 0.
public static int Altura(Nodo? nodo)
    => nodo is null ? 0 : 1 + Math.Max(Altura(nodo.Izq), Altura(nodo.Der));
```

Casi todo problema de árboles tiene esta forma: **qué devuelvo para el árbol vacío** y **cómo combino lo que me devuelven los hijos**.

---

## Buscar en un BST

```csharp
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
```

Si insertas 1, 2, 3, ..., n en orden, el BST degenera en una lista y buscar cuesta O(n). Por eso `SortedSet<T>` y `SortedDictionary<K, V>` usan árboles rojo-negro, que se rebalancean solos y garantizan O(log n).

---

## Ejercicio en vivo: ¿es un BST?

```text
        8
      /   \
     3     10
          /
         6
```

Cada padre con su hijo cumple la regla: 3 < 8, 10 > 8, 6 < 10. ¿Es un árbol binario de búsqueda?

<!--
Nota del instructor: no lo es. El 6 está en el subárbol derecho del 8, así que debería ser mayor que 8. Es el error más común: comparar solo con el padre. Deja que el grupo lo descubra antes de pasar a la solución.
-->

---

## La solución: pasar el rango

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

Los límites van en `long` para que un nodo con `int.MinValue` o `int.MaxValue` no choque con el límite inicial.

---

<!-- _class: lead -->

# Parte 3
## Grafos

---

## Nodos, aristas y cómo guardarlos

- **Dirigido o no:** "A sigue a B" es dirigido; "A es vecino de B", no.
- **Con peso o sin peso:** distancia, costo, tiempo.

| Representación | Memoria | ¿Hay arista a-b? | Vecinos de a |
|----------------|---------|------------------|--------------|
| Lista de adyacencia | O(V + E) | O(grado de a) | O(grado de a) |
| Matriz de adyacencia | O(V²) | O(1) | O(V) |

La lista de adyacencia es la opción por defecto: los grafos reales tienen muchas menos aristas que V².

---

## Construir la lista de adyacencia

```csharp
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
```

En un grafo no dirigido, cada arista se guarda en los dos sentidos.

---

## BFS: el camino con menos aristas

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

Se marca como visitado **al encolar**, no al sacar: si no, un nodo puede entrar varias veces a la cola.

---

## DFS: qué se alcanza

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

BFS y DFS tienen el mismo costo. BFS da distancias en número de aristas; DFS es más natural para ciclos, componentes y "¿se puede llegar?".

---

## Una cuadrícula también es un grafo

```csharp
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
```

`Inundar` es un BFS desde esa celda que marca toda la isla. Con cola en vez de recursión, una isla de un millón de celdas no desborda la pila: hay una prueba que lo comprueba.

---

## Dependencias: orden topológico

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

---

## Con pesos: Dijkstra

- BFS da el camino con **menos aristas**. Si las aristas cuestan distinto, ese no es el más barato.
- **Dijkstra** saca siempre el nodo pendiente más cercano con una `PriorityQueue`: O((V + E) log V).
- Solo funciona con pesos **no negativos**. Con negativos, Bellman-Ford: O(V × E).

El código completo, con una prueba que lo compara contra Bellman-Ford en grafos al azar, está en la nota 20.

---

## Ejercicio en vivo: encuentra el grafo

Para cada enunciado: ¿qué son los nodos?, ¿qué son las aristas?, ¿dirigido o no?, ¿qué algoritmo?

1. "Un usuario puede invitar a otros. ¿A cuántas personas llegó, directa o indirectamente, la invitación de Ana?"
2. "Cada materia tiene prerrequisitos. ¿En qué orden se pueden cursar todas?"
3. "En el mapa del almacén, ¿cuál es el camino con menos pasos del muelle a un estante, sin atravesar pasillos bloqueados?"

<!--
Nota del instructor: 1, personas e invitaciones, dirigido, DFS o BFS desde Ana y contar alcanzables. 2, materias y prerrequisitos, dirigido, orden topológico; si hay ciclo, el plan de estudios está mal. 3, celdas libres de una cuadrícula, no dirigido, BFS. Pide que digan el costo de cada uno.
-->

---

## De la señal al algoritmo

| Señal en el enunciado | Algoritmo | Costo |
|-----------------------|-----------|-------|
| "Datos ordenados", "el mínimo que alcanza" | búsqueda binaria | O(log n) |
| "¿Chocan?", "agrupar los que se tocan" | ordenar primero | O(n log n) |
| "Nivel por nivel", "a distancia k" | BFS en árbol | O(n) |
| "Menos pasos", "menos transferencias" | BFS en grafo | O(V + E) |
| "¿Se puede llegar?", "¿cuántos grupos?" | DFS o BFS | O(V + E) |
| "Antes de", "prerrequisito" | orden topológico | O(V + E) |
| "Ruta más barata" con costos | Dijkstra | O((V + E) log V) |

---

## La práctica de hoy

**Seis problemas cronometrados, en parejas.** Una persona resuelve en voz alta; la otra hace de entrevistador:

- Lee el enunciado y contesta las aclaraciones con lo que dice la práctica.
- Da una pista **solo** si pasan dos minutos de silencio.
- Al final dice qué funcionó y qué mejorar.

12 minutos por problema; cambian de papel en cada uno.

<!--
Nota del instructor: 72 minutos de práctica no caben en una sesión de 2 horas con la clase. Asigna los problemas 1 a 3 en clase y los 4 a 6 de tarea en la misma pareja, o la mitad del grupo hace los impares y la otra los pares, y al final se explican entre sí.
-->

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas

- Búsqueda binaria sobre datos y sobre la respuesta, sin errores de límites
- Los recorridos de un árbol y la forma de casi todo problema recursivo
- Reconocer el grafo detrás de un enunciado y elegir BFS, DFS u orden topológico

**Libros:** *C# Data Structures and Algorithms* (Jamro, 2024), caps. 3, 7 y 8. *50 Algorithms Every Programmer Should Know* (Ahmad, 2023), sección 1, caps. 3 y 5.

**Siguiente módulo:** diseñar un algoritmo bajo presión.
