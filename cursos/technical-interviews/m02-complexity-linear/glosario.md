# Glosario - Módulo 2
## Complejidad y estructuras lineales

> Términos usados en el módulo 2. Ordenado alfabéticamente.

---

## B

### Big O
**Categoría:** Complejidad
**Definición:** Notación que describe cómo crece el número de pasos (o la memoria) de un algoritmo cuando crece la entrada `n`, sin constantes ni términos menores. O(2n + 5) se escribe O(n). Si no se dice otra cosa, describe el peor caso.

---

## C

### Cola (`Queue<T>`)
**Categoría:** Estructura de datos
**Definición:** Colección en la que el primero que entra es el primero que sale (FIFO). `Enqueue` cuesta O(1) amortizado y `Dequeue`, O(1). Sirve para procesar en orden de llegada y para recorrer por niveles.

---

### Cola de prioridad (`PriorityQueue<TElement, TPriority>`)
**Categoría:** Estructura de datos
**Definición:** Colección que siempre entrega primero el elemento de menor prioridad. Está hecha con un montículo: encolar y desencolar cuestan O(log n) y ver el siguiente, O(1). No garantiza orden entre prioridades iguales.

---

### Complejidad amortizada
**Categoría:** Complejidad
**Definición:** Costo promedio por operación sobre una secuencia larga, aunque alguna operación aislada sea cara. `List<T>.Add` es O(1) amortizado: a veces copia todo el arreglo, pero tan pocas veces que el promedio es constante.

---

### Complejidad en espacio
**Categoría:** Complejidad
**Definición:** Memoria adicional que usa un algoritmo, además de su entrada, expresada en Big O. Un diccionario auxiliar del tamaño de la entrada es O(n) de espacio.

---

## F

### Fuerza bruta
**Categoría:** Resolución de problemas
**Definición:** La solución que prueba todas las posibilidades sin aprovechar ninguna estructura del problema. En una entrevista se dice primero, con su costo, porque es el punto de partida para mejorar.

---

## L

### Lista ligada (`LinkedList<T>`)
**Categoría:** Estructura de datos
**Definición:** Colección de nodos enlazados entre sí. Insertar o quitar junto a un nodo conocido cuesta O(1), pero encontrarlo cuesta O(n) y no tiene acceso por índice. En la práctica suele ser más lenta que `List<T>` porque sus nodos están dispersos en memoria.

---

## P

### Peor caso
**Categoría:** Complejidad
**Definición:** El costo con la entrada más desfavorable posible. Una búsqueda lineal es O(n) en el peor caso (el valor no está o está al final), aunque a veces lo encuentre a la primera.

---

### Pila (`Stack<T>`)
**Categoría:** Estructura de datos
**Definición:** Colección en la que el último que entra es el primero que sale (LIFO). `Push` cuesta O(1) amortizado y `Pop`, O(1). Sirve para deshacer, para emparejar aperturas con cierres y para recorrer en profundidad sin recursión.

---

## T

### Tabla hash
**Categoría:** Estructura de datos
**Definición:** Estructura que calcula, a partir del hash de la clave, en qué cubeta guardar cada elemento. Base de `Dictionary<TKey, TValue>` y `HashSet<T>`: agregar, buscar y quitar cuestan O(1) en promedio y O(n) en el peor caso, si todas las claves chocan.

---

### Término dominante
**Categoría:** Complejidad
**Definición:** El término de una expresión de costo que crece más rápido y que, por eso, define su clase. En n² + 100n + 7, el término dominante es n² y la complejidad es O(n²).

---

## V

### Ventana deslizante
**Categoría:** Técnica
**Definición:** Técnica para problemas sobre subarreglos consecutivos: en vez de recalcular cada ventana desde cero, se suma el elemento que entra y se resta el que sale. Convierte un O(n × k) en O(n).
