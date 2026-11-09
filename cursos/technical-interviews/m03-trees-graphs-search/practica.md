# Práctica - Seis problemas cronometrados en parejas

## Objetivo

Resolver seis problemas de búsqueda, árboles y grafos con reloj, como en una entrevista: una persona resuelve en voz alta y la otra entrevista. Al terminar deberías poder reconocer, a partir del enunciado, qué estructura hay debajo y qué recorrido usar.

**Lectura de apoyo:** [20 - Estructuras de datos y complejidad](../../../02-programacion/csharp/20-estructuras-de-datos.md).

## Cómo se trabaja

- **12 minutos por problema.** Cambian de papel en cada uno.
- **Quien entrevista** lee el enunciado en voz alta, contesta las aclaraciones con lo que dice cada problema y da las pistas en orden, **solo** si pasan dos minutos sin avance. Al final, dos comentarios: uno que funcionó y uno que mejorar.
- **Quien resuelve** sigue los cuatro pasos: aclarar, proponer (fuerza bruta y su costo), escribir y probar a mano.
- Las soluciones compilan y tienen pruebas xUnit con los casos borde de cada problema. Quien entrevista puede leer la solución antes; quien resuelve, solo al terminar.

Si el tiempo no alcanza para los seis, hagan del 1 al 3 en clase y del 4 al 6 como tarea con la misma pareja.

### Hoja del entrevistador

Marca con una palomita lo que observaste en cada problema:

| | P1 | P2 | P3 | P4 | P5 | P6 |
|---|---|---|---|---|---|---|
| Nombró la estructura (nodos y aristas, o árbol) antes de escribir | | | | | | |
| Dijo la fuerza bruta y su costo | | | | | | |
| Pidió al menos una aclaración útil | | | | | | |
| Probó a mano un caso borde | | | | | | |
| Dijo la complejidad final en tiempo y espacio | | | | | | |
| Necesitó pistas (cuántas) | | | | | | |

---

## Problema 1. La primera aparición

**Enunciado.** Dado un arreglo ordenado de menor a mayor que puede tener repetidos, devuelve el índice de la **primera** aparición de un valor, o -1 si no está.

**Aclaraciones:** ¿puede venir vacío? (sí). ¿Se puede usar `Array.IndexOf`? (no: se espera algo mejor que O(n)).

**Pistas.**
1. Una búsqueda binaria normal encuentra *alguna* aparición. ¿Cómo encuentras la primera?
2. Piensa en la condición "el valor en `k` es mayor o igual que el objetivo". ¿Cómo se ve a lo largo del arreglo?

**Solución.**

```csharp
// Primera aparición en un arreglo ordenado con repetidos: el primer índice con datos[i] >= objetivo,
// y luego se confirma que sea el objetivo. O(log n) en tiempo, O(1) en espacio.
public static int PrimeraAparicion(int[] datos, int objetivo)
{
    int i = Busqueda.PrimerVerdadero(0, datos.Length, k => datos[k] >= objetivo);
    return i < datos.Length && datos[i] == objetivo ? i : -1;
}
```

**Cómo explicarla.** "La condición `datos[k] >= objetivo` es falsa al principio y verdadera desde la primera aparición en adelante: es monótona. Busco la frontera con búsqueda binaria y confirmo que ahí esté el objetivo, porque la frontera también existe cuando el valor no está."

**Casos borde probados:** arreglo vacío, un elemento que es o no el objetivo, todos iguales (`[2, 2, 2]` da 0), el objetivo al final, uno que cae entre dos valores, uno menor que todos y uno mayor que todos. Además, 300 arreglos al azar comparados contra `Array.IndexOf`.

---

## Problema 2. La hoja más cercana

**Enunciado.** Dado un árbol binario, devuelve su profundidad mínima: el número de nodos del camino más corto de la raíz a una hoja. Un árbol vacío tiene profundidad 0.

**Aclaraciones:** ¿qué es una hoja? (un nodo **sin ningún** hijo). ¿El árbol está balanceado? (no necesariamente).

**Pistas.**
1. ¿Qué recorrido encuentra primero lo que está más cerca de la raíz?
2. Cuidado: un nodo con un solo hijo no es hoja.

**Solución.**

```csharp
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
```

**Cómo explicarla.** "Una versión recursiva que hace `1 + Math.Min(izquierda, derecha)` falla con los nodos de un solo hijo: tomaría como 0 el lado vacío. Con BFS recorro por niveles y la primera hoja que aparece es la más cercana, así que puedo parar antes de ver todo el árbol."

**Casos borde probados:** árbol vacío, un solo nodo, una hoja a nivel 2 junto a una rama más profunda, y una sola rama de cuatro nodos (la respuesta es 4, no 1).

---

## Problema 3. El ancestro común en un árbol de búsqueda

**Enunciado.** Dado un árbol binario de búsqueda y dos valores que están en él, devuelve el valor del ancestro común más bajo: el nodo más profundo que tiene a los dos en su subárbol (un nodo cuenta como ancestro de sí mismo).

**Aclaraciones:** ¿los dos valores siempre están? (sí). ¿Puede ser el mismo valor dos veces? (sí: la respuesta es ese valor).

**Pistas.**
1. Que sea un BST importa: ¿qué te dice comparar los dos valores con la raíz?
2. ¿Cuándo dejan los dos valores de estar del mismo lado?

**Solución.**

```csharp
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
```

**Cómo explicarla.** "Si los dos son menores que el nodo, el ancestro está a la izquierda; si los dos son mayores, a la derecha. En cuanto quedan en lados distintos, o uno de ellos es el nodo, ese es el ancestro. Baja un solo camino: O(h), que es O(log n) si el árbol está balanceado."

**Casos borde probados:** valores en subárboles distintos (la raíz), uno ancestro del otro, el mismo valor dos veces, y el orden de los argumentos invertido.

---

## Problema 4. ¿Cuántos grupos de amigos?

**Enunciado.** Hay `n` personas numeradas de 0 a n-1 y una lista de amistades (pares). Si A es amiga de B y B de C, las tres están en el mismo grupo. ¿Cuántos grupos hay?

**Aclaraciones:** ¿la amistad es mutua? (sí). ¿Puede haber pares repetidos o una persona amiga de sí misma? (sí, y no deben romper nada). ¿Una persona sin amistades es un grupo? (sí).

**Pistas.**
1. Nombra los nodos y las aristas.
2. Cada vez que empiezas un recorrido desde alguien no visitado, ¿qué encontraste?

**Solución.**

```csharp
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
```

**Cómo explicarla.** "Personas como nodos, amistades como aristas no dirigidas. Cada recorrido que arranca en alguien no visitado descubre un grupo completo, así que cuento cuántas veces arranco. Cada nodo y cada arista se visitan una vez: O(V + E). La otra solución clásica es Union-Find, con un costo casi lineal."

**Casos borde probados:** cero personas, nadie con amistades (n grupos), grupos que se unen por una arista que llega al final, un lazo y una arista repetida. Además, 200 grafos al azar comparados contra Union-Find.

---

## Problema 5. El camino más corto en el almacén

**Enunciado.** El almacén es una cuadrícula donde cada celda está libre o bloqueada. Un robot sale de la esquina superior izquierda y debe llegar a la inferior derecha moviéndose arriba, abajo, izquierda o derecha. ¿Cuál es el menor número de pasos? Devuelve -1 si no puede llegar.

**Aclaraciones:** ¿se vale moverse en diagonal? (no). ¿El inicio o el destino pueden estar bloqueados? (sí: entonces -1). ¿Una cuadrícula de 1 × 1 libre? (0 pasos).

**Pistas.**
1. ¿Qué son los nodos y cuándo hay arista entre dos?
2. ¿Qué recorrido da el camino con menos aristas?

**Solución.**

```csharp
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
```

**Cómo explicarla.** "Cada celda libre es un nodo y dos celdas vecinas libres tienen una arista. Todas las aristas pesan lo mismo, así que BFS da el camino con menos pasos. La matriz `pasos` sirve a la vez de marca de visitado y de distancia. Cada celda entra una vez a la cola: O(F × C)."

**Casos borde probados:** 1 × 1 libre (0) y bloqueada (-1), cuadrícula vacía, salida o destino bloqueados, dos celdas libres solo en diagonal (-1), y un almacén de 5 × 4 con pasillos que obligan a zigzaguear (13 pasos).

---

## Problema 6. ¿Cuánto tarda el proyecto?

**Enunciado.** Un proyecto tiene `n` tareas, cada una con su duración en días, y una lista de dependencias "A antes de B". Hay gente de sobra: todas las tareas que ya pueden empezar, empiezan. ¿Cuántos días tarda el proyecto completo? Si las dependencias tienen un ciclo, devuelve -1.

**Aclaraciones:** ¿una tarea puede empezar el mismo día en que termina su dependencia? (sí). ¿Puede haber tareas sin dependencias? (sí, empiezan el día 0). ¿Duración 0? (se vale).

**Pistas.**
1. Es un grafo dirigido: ¿en qué orden conviene procesar las tareas?
2. Una tarea empieza cuando terminó la **última** de sus dependencias. ¿Qué guardas por tarea?

**Solución.**

```csharp
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
```

**Cómo explicarla.** "Proceso las tareas en orden topológico, con Kahn. Cuando una termina, empujo su hora de fin a cada tarea que depende de ella y me quedo con el máximo: una tarea no puede empezar antes de que termine la última de sus dependencias. La respuesta es el mayor fin. Es el camino más largo en un grafo sin ciclos, la ruta crítica de un proyecto. Si no se procesan todas, hay un ciclo."

**Casos borde probados:** sin tareas (0), una tarea, todas en paralelo (la más larga), todas en cadena (la suma), un caso mixto en el que la ruta crítica no es la cadena más larga en número de tareas, y un ciclo (-1). Además, 200 proyectos al azar comparados contra una recursión con memoria.

---

## Checklist de la práctica

- [ ] Resolví al menos tres problemas en voz alta, con reloj.
- [ ] Entrevisté al menos tres problemas y llené la hoja del entrevistador.
- [ ] En cada problema de grafos nombré los nodos y las aristas antes de escribir.
- [ ] Puedo explicar por qué BFS da el camino con menos pasos y cuándo deja de servir.
- [ ] Puedo explicar por qué una búsqueda binaria necesita una condición monótona.
- [ ] Anoté en qué problema necesité pistas y cuántas.
