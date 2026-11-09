# Glosario - Módulo 3
## Árboles, grafos y búsqueda

> Términos usados en el módulo 3. Ordenado alfabéticamente.

---

## A

### Altura de un árbol
**Categoría:** Árboles
**Definición:** Número de nodos del camino más largo entre la raíz y una hoja. Define el costo de buscar en un árbol de búsqueda: O(h), que es O(log n) si el árbol está balanceado y O(n) si degeneró en una sola rama.

---

### Árbol binario de búsqueda (BST)
**Categoría:** Árboles
**Definición:** Árbol binario en el que todo lo que está en el subárbol izquierdo de un nodo es menor que él y todo lo del derecho es mayor. La regla es sobre subárboles completos, no solo entre padre e hijo. Recorrido en inorden, da los valores ordenados.

---

### Árbol rojo-negro
**Categoría:** Árboles
**Definición:** Árbol de búsqueda que se rebalancea solo al insertar y al borrar, de modo que su altura siempre es O(log n). Es la estructura interna de `SortedSet<T>` y `SortedDictionary<TKey, TValue>`.

---

## B

### BFS (búsqueda en anchura)
**Categoría:** Grafos
**Definición:** Recorrido que visita primero a los vecinos de un nodo, luego a los vecinos de sus vecinos, usando una cola. En un grafo sin pesos da el camino con menos aristas. Cuesta O(V + E).

---

### Búsqueda binaria
**Categoría:** Búsqueda
**Definición:** Búsqueda que, en datos ordenados, compara con el elemento de en medio y descarta la mitad en cada paso. Cuesta O(log n). Sobre datos desordenados no falla: devuelve resultados incorrectos.

---

### Búsqueda sobre la respuesta
**Categoría:** Búsqueda
**Definición:** Uso de la búsqueda binaria sobre un rango de posibles respuestas cuando una condición es monótona (falsa hasta cierto valor y verdadera de ahí en adelante), como "la capacidad mínima que alcanza".

---

## C

### Componente conexa
**Categoría:** Grafos
**Definición:** Grupo máximo de nodos de un grafo no dirigido en el que todos se pueden alcanzar entre sí. Contar componentes es contar cuántas veces hay que empezar un recorrido nuevo desde un nodo no visitado.

---

## D

### DFS (búsqueda en profundidad)
**Categoría:** Grafos
**Definición:** Recorrido que avanza por un camino hasta el fondo antes de regresar, con recursión o con una pila. Sirve para saber qué es alcanzable, contar componentes y detectar ciclos. Cuesta O(V + E).

---

### Dijkstra
**Categoría:** Grafos
**Definición:** Algoritmo de camino más barato en grafos con pesos no negativos. Saca siempre el nodo pendiente más cercano con una cola de prioridad y cuesta O((V + E) log V).

---

## L

### Lista de adyacencia
**Categoría:** Grafos
**Definición:** Representación de un grafo que guarda, por cada nodo, la lista de sus vecinos. Usa O(V + E) de memoria y es la opción por defecto, porque los grafos reales tienen muchas menos aristas que V².

---

## O

### Orden topológico
**Categoría:** Grafos
**Definición:** Secuencia de los nodos de un grafo dirigido en la que cada nodo aparece después de todos los que le apuntan. Solo existe si no hay ciclos. El algoritmo de Kahn lo construye en O(V + E).

---

### Ordenamiento estable
**Categoría:** Ordenamiento
**Definición:** Ordenamiento que conserva el orden original entre elementos iguales. En .NET, `Enumerable.OrderBy` es estable; `Array.Sort` y `List<T>.Sort` no lo son.

---

## R

### Recorridos en profundidad de un árbol
**Categoría:** Árboles
**Definición:** Preorden (la raíz antes de los hijos), inorden (la raíz entre ellos) y postorden (la raíz después). Los tres cuestan O(n) en tiempo y O(h) en espacio por la pila de llamadas.

---

### Ruta crítica
**Categoría:** Grafos
**Definición:** La cadena de tareas dependientes cuya duración total es la mayor; marca cuánto tarda un proyecto aunque haya gente de sobra para trabajar en paralelo. Se calcula sobre el orden topológico.
