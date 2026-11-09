<!-- _class: lead -->

# Módulo 2 - Complejidad y estructuras lineales

### Preparación para entrevistas técnicas

**Rogelio Arriaga - Raptor Dev Services**

---

## Hoy

> Big O no es teoría: es la pregunta de cuánto empeora cuando los datos crecen.

- **Parte 1:** Big O, cómo leerla en el código y cómo decirla en voz alta
- **Parte 2:** arreglos y listas
- **Parte 3:** pilas, colas y colas de prioridad
- **Parte 4:** diccionarios y conjuntos, y el patrón de cambiar memoria por tiempo
- **Práctica:** cinco problemas cortos, resueltos y explicados en voz alta

Lecturas: [12 - Colecciones y LINQ](../../../02-programacion/csharp/12-collections.md) y [20 - Estructuras de datos y complejidad](../../../02-programacion/csharp/20-estructuras-de-datos.md)

---

<!-- _class: lead -->

# Parte 1
## Big O

---

## La pregunta que contesta

Con 10 registros, todo es rápido. Con un millón, no.

**Big O describe cómo crece el número de pasos cuando crece la entrada `n`.** No mide segundos: mide la forma de la curva.

Tres reglas bastan para casi todo:

1. **Las constantes no cuentan:** O(2n) es O(n).
2. **Manda el término dominante:** O(n² + n) es O(n²).
3. **Secuencia suma, anidación multiplica:** dos ciclos seguidos son O(n + m); uno dentro del otro, O(n × m).

---

## Las clases que vas a ver

| Clase | Nombre | Ejemplo | Pasos con n = 10^6 |
|-------|--------|---------|-------------------|
| O(1) | constante | `arreglo[i]`, buscar en un `Dictionary` | 1 |
| O(log n) | logarítmica | búsqueda binaria | unos 20 |
| O(n) | lineal | recorrer una lista | 10^6 |
| O(n log n) | lineal-logarítmica | ordenar | unos 2 × 10^7 |
| O(n²) | cuadrática | todos contra todos | 10^12 |
| O(2^n) | exponencial | todos los subconjuntos | impracticable |

Con una máquina supuesta de 10^8 operaciones por segundo: ordenar un millón tarda una fracción de segundo; compararlos todos contra todos, más de dos horas.

---

## Ejercicio en vivo: ¿cuál es la complejidad?

```csharp
// A: un ciclo que recorre n elementos.
public static int PasosA(int n)
{
    int pasos = 0;
    for (int i = 0; i < n; i++)
        pasos++;
    return pasos;
}
```

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

<!--
Nota del instructor: pide que voten antes de decir la respuesta. A es O(n). C es O(log n): i vale 1, 2, 4, 8... y con n = 1,000,000 da 20 vueltas. Las pruebas del proyecto lo comprueban contando: PasosC(1024) da 10 y PasosC(1025) da 11.
-->

---

## Ejercicio en vivo: ¿y estos?

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

```csharp
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
```

<!--
Nota del instructor: D es la trampa clásica. Hace n(n - 1)/2 pasos, la mitad de dos ciclos completos, pero sigue siendo O(n²): al duplicar n los pasos casi se cuadruplican (la prueba mide una razón entre 3.9 y 4.1). E es O(n + m), no O(n): son dos entradas distintas y hay que nombrar las dos.
-->

---

## El espacio también cuenta

- **Tiempo:** cuántos pasos.
- **Espacio adicional:** cuánta memoria extra, además de la entrada.

Un algoritmo O(n) en tiempo que crea un diccionario del tamaño de la entrada usa **O(n) de espacio**. A veces es la mejor opción; a veces no hay memoria para eso. **Dilo siempre en las dos dimensiones.**

---

## Peor caso, promedio y amortizado

- **Peor caso:** la entrada más desfavorable. Es lo que se reporta si no se dice otra cosa.
- **Promedio:** con entradas típicas. `Dictionary` es O(1) en promedio y O(n) si todas las claves chocan.
- **Amortizado:** el promedio sobre muchas operaciones. `List<T>.Add` a veces copia todo el arreglo (cuando se llena, duplica su capacidad), pero esas copias son tan raras que cada `Add` cuesta **O(1) amortizado**.

---

## Cómo decirlo en voz alta

> "Esta solución es O(n) en tiempo porque recorro el arreglo una vez, y O(n) en espacio porque guardo cada valor en un diccionario. Con fuerza bruta sería O(n²) en tiempo y O(1) en espacio."

Tres partes: **la complejidad, por qué, y contra qué la comparas.**

Quien entrevista no quiere oír "creo que es eficiente".

---

<!-- _class: lead -->

# Parte 2
## Arreglos y listas

---

## `T[]` y `List<T>`

| Operación | `T[]` | `List<T>` |
|-----------|-------|-----------|
| Leer por índice | O(1) | O(1) |
| Buscar un valor (`Contains`) | O(n) | O(n) |
| Agregar al final | no cambia de tamaño | O(1) amortizado |
| Insertar o quitar al principio | no cambia de tamaño | O(n) |
| Quitar al final | no cambia de tamaño | O(1) |

`List<T>` es un arreglo que crece. Leer es igual de barato; mover elementos, no.

---

## La trampa de Insert(0)

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

---

## `LinkedList<T>`: menos útil de lo que parece

- Insertar o quitar junto a un nodo que **ya tienes**: O(1).
- Encontrar ese nodo: O(n). No tiene índice.
- Sus nodos están dispersos en memoria; los de `List<T>` están juntos, y el procesador lee memoria contigua mucho más rápido.

**En la práctica, `List<T>` casi siempre gana.** Si te preguntan cuándo usarías una lista ligada, la respuesta honesta incluye esa advertencia.

---

<!-- _class: lead -->

# Parte 3
## Pilas, colas y colas de prioridad

---

## Pila y cola

| | `Stack<T>` | `Queue<T>` |
|---|---|---|
| Orden | el último que entra sale primero (LIFO) | el primero que entra sale primero (FIFO) |
| Meter | `Push`: O(1) amortizado | `Enqueue`: O(1) amortizado |
| Sacar | `Pop`: O(1) | `Dequeue`: O(1) |
| Para qué | deshacer, emparejar aperturas y cierres, recorrer en profundidad | procesar en orden de llegada, recorrer por niveles |

**Señal en el enunciado:** "el más reciente", "anidado", "deshacer" huelen a pila; "en orden", "por turnos", "nivel por nivel", a cola.

---

## Ejercicio en vivo: paréntesis balanceados

"Dada una cadena con `()`, `[]` y `{}`, di si cada apertura tiene su cierre en el orden correcto."

Antes de ver la solución, en parejas:

1. ¿Qué preguntarías para aclarar? (¿cadena vacía?, ¿hay otros caracteres?)
2. ¿Qué estructura usarías y por qué?
3. Escribe tres casos de prueba, uno que deba fallar por orden: `"([)]"`.

<!--
Nota del instructor: 5 minutos. La pista si se atoran: el último paréntesis que se abrió es el primero que se debe cerrar. Eso es LIFO.
-->

---

## La solución

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

Los dos casos borde que más se olvidan: un cierre con la pila vacía (`")"`) y aperturas que sobran al final (`"(()"`).

---

## `PriorityQueue<TElement, TPriority>`

Siempre entrega primero el de **menor** prioridad. Es un montículo guardado en un arreglo.

| Operación | Costo |
|-----------|-------|
| `Enqueue` | O(log n) |
| `Dequeue` | O(log n) |
| `Peek` | O(1) |

- Para "el siguiente más urgente", "los k mayores", "la sala que se libera primero".
- No garantiza orden entre prioridades iguales.
- Para una cola de **máximos**, se encola con la prioridad negada o con un comparador invertido.

---

<!-- _class: lead -->

# Parte 4
## Diccionarios y conjuntos

---

## Tabla hash: O(1) en promedio

`Dictionary<TKey, TValue>` y `HashSet<T>` calculan, a partir de la clave, en qué cubeta guardarla.

| Operación | Promedio | Peor caso |
|-----------|----------|-----------|
| Agregar, buscar, quitar | O(1) | O(n), si todas las claves caen en la misma cubeta |

Dos condiciones:

- `Equals` y `GetHashCode` coherentes: dos claves iguales dan el mismo hash. Los `record` ya lo cumplen.
- La clave no cambia mientras está dentro; si cambia su hash, ya no se encuentra.

---

## El patrón: cambiar memoria por tiempo

"Dado un arreglo y un objetivo, encuentra dos posiciones cuyos valores sumen el objetivo."

```csharp
// Fuerza bruta: O(n²) en tiempo, O(1) en espacio.
public static (int I, int J)? FuerzaBruta(int[] numeros, int objetivo)
{
    for (int i = 0; i < numeros.Length; i++)
        for (int j = i + 1; j < numeros.Length; j++)
            if (numeros[i] + numeros[j] == objetivo)
                return (i, j);
    return null;
}
```

**Primero di esta solución y su costo.** Luego pregúntate: ¿qué trabajo se repite? El ciclo interno busca, una y otra vez, si existe el complemento.

---

## Con diccionario

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

Se busca el complemento **antes** de guardar el valor actual: así `[3, 3]` con objetivo 6 da `(0, 1)` y nunca se usa el mismo elemento dos veces.

<!--
Nota del instructor: pregunta qué pasa con [3] y objetivo 6 si se guardara primero y se buscara después. Devolvería (0, 0): el mismo elemento dos veces. Ese es el tipo de caso borde que quien entrevista espera que el candidato encuentre solo.
-->

---

## Contar frecuencias

```csharp
// O(n) en tiempo; O(k) en espacio, con k palabras distintas.
public static Dictionary<string, int> Contar(IEnumerable<string> palabras)
{
    var cuenta = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var palabra in palabras)
        cuenta[palabra] = cuenta.GetValueOrDefault(palabra) + 1;
    return cuenta;
}
```

"¿Cuántas veces?", "el más frecuente", "agrupar por", "¿son anagramas?": todos empiezan con un diccionario de conteo.

---

## La trampa en LINQ

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

Con 100,000 elementos en cada lista y pocos en común: hasta 10^10 comparaciones contra unas 2 × 10^5 operaciones.

---

## Colecciones ordenadas

| Colección | Estructura | Costo |
|-----------|-----------|-------|
| `SortedDictionary<K, V>` | árbol rojo-negro | O(log n) agregar, buscar y quitar |
| `SortedSet<T>` | árbol rojo-negro | O(log n), con `Min`, `Max` y rangos |
| `SortedList<K, V>` | dos arreglos ordenados | buscar O(log n), insertar O(n) |

Úsalas cuando además de buscar necesitas **recorrer en orden** o **el siguiente mayor**. Si solo buscas, `Dictionary` es más barato.

---

## Ejercicio en vivo: ¿qué estructura usarías?

Di la estructura y el costo de la operación principal:

1. Saber si un correo ya está registrado, entre un millón.
2. Atender solicitudes de soporte en orden de llegada.
3. El botón de deshacer de un editor.
4. Despachar primero los pedidos urgentes, luego los normales.
5. Mostrar las calificaciones de un grupo siempre ordenadas mientras se capturan.

<!--
Nota del instructor: 1, HashSet, O(1) promedio. 2, Queue, O(1). 3, Stack, O(1). 4, PriorityQueue, O(log n) por operación. 5, SortedDictionary o SortedSet (ojo: SortedSet no admite repetidos; dos alumnos con la misma calificación necesitan otra clave), O(log n) por inserción. La 5 abre la conversación sobre repetidos: déjala crecer.
-->

---

## La práctica de hoy

Cinco problemas cortos. Para cada uno, **en voz alta**:

1. Repite el problema y pregunta lo que no esté claro.
2. Di la fuerza bruta y su costo.
3. Escribe la mejor versión que encuentres.
4. Pruébala a mano con un caso normal y dos casos borde.
5. Di su complejidad en tiempo y en espacio.

Las soluciones explicadas, con sus pruebas, están en la práctica.

<!--
Nota del instructor: en parejas, 8 minutos por problema. Una persona resuelve y la otra solo puede hacer preguntas de aclaración. Cambian en cada problema.
-->

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas

- Leer la complejidad de un fragmento y decirla en tiempo y en espacio
- El costo de cada operación de las colecciones de .NET
- El patrón de cambiar memoria por tiempo con un diccionario

**Libros:** *C# Data Structures and Algorithms* (Jamro, 2024), caps. 2 a 6. *The Imposter's Handbook* (Conery), "Big O" y "Data Structures".

**Siguiente módulo:** árboles, grafos y búsqueda.
