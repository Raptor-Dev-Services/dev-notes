# Glosario - Módulo 4
## Diseñar un algoritmo bajo presión

> Términos usados en el módulo 4. Ordenado alfabéticamente.

---

## A

### Aclarar
**Categoría:** Método de entrevista
**Definición:** Primer paso del método: repetir el problema con tus palabras y preguntar tamaños, casos borde, qué devolver si no hay respuesta y qué restricciones hay. Evita resolver el problema equivocado y suma en el eje de comunicación.

---

### Algoritmo voraz
**Categoría:** Diseño de algoritmos
**Definición:** Algoritmo que en cada paso toma la mejor opción del momento sin reconsiderarla. Es rápido, pero solo es correcto si se puede demostrar que esa elección nunca estorba, por ejemplo con un argumento de intercambio.

---

### Argumento de intercambio
**Categoría:** Diseño de algoritmos
**Definición:** Forma de justificar un algoritmo voraz: si una solución óptima no hace la elección voraz, se muestra que se puede cambiar por ella sin empeorar. En las reuniones, cambiar cualquier primera reunión por la que termina antes no quita espacio a las demás.

---

## C

### Caso borde
**Categoría:** Pruebas
**Definición:** Entrada en el límite de lo válido: vacía, de un solo elemento, con todos iguales, con valores extremos o con la respuesta al principio o al final. Encontrarlos sin ayuda es uno de los ejes de la rúbrica.

---

### Contraejemplo
**Categoría:** Diseño de algoritmos
**Definición:** Una entrada concreta en la que un algoritmo da una respuesta incorrecta. Con monedas de 1, 3 y 4 y monto 6, el voraz da tres monedas y el óptimo son dos: ese es el contraejemplo que descarta al voraz.

---

## D

### Divide y vencerás
**Categoría:** Diseño de algoritmos
**Definición:** Técnica que parte el problema en subproblemas independientes más chicos, los resuelve recursivamente y combina sus resultados. Merge sort y la potencia rápida son ejemplos.

---

## E

### Estado (programación dinámica)
**Categoría:** Diseño de algoritmos
**Definición:** El subproblema que describe cada celda de la tabla, como "el menor número de monedas para formar x" o "la mejor suma que termina en i". Elegirlo bien es la mitad del problema.

---

## M

### Memoización
**Categoría:** Diseño de algoritmos
**Definición:** Programación dinámica de arriba hacia abajo: la recursión normal, pero guardando cada resultado para no volver a calcularlo. Convierte el Fibonacci ingenuo, exponencial, en O(n).

---

### Merge sort
**Categoría:** Ordenamiento
**Definición:** Ordenamiento por divide y vencerás: parte el arreglo en mitades, las ordena y las mezcla. Cuesta O(n log n) en tiempo y O(n) en espacio, y es estable.

---

## P

### Pensar en voz alta
**Categoría:** Método de entrevista
**Definición:** Narrar lo que piensas mientras resuelves: el plan, la duda, el costo, el caso que vas a probar. Es la única forma de que quien entrevista califique tu razonamiento y no solo tu resultado.

---

### Programación dinámica
**Categoría:** Diseño de algoritmos
**Definición:** Técnica para problemas cuyos subproblemas se repiten: se resuelve cada uno una vez y se guarda su resultado. Se describe con estado, transición, caso base, orden y respuesta.

---

## R

### Recurrencia
**Categoría:** Complejidad
**Definición:** Expresión del costo de un algoritmo recursivo en función de sus subproblemas, como T(n) = 2 T(n/2) + O(n) para merge sort. En una entrevista basta con resolverla dibujando el árbol: cuántos niveles y cuánto trabajo por nivel.

---

### Rúbrica de entrevista
**Categoría:** Evaluación
**Definición:** Tabla con los ejes que se califican (comunicación, resolución, código, pruebas y pistas) y la descripción de cada nivel. Cada calificación se acompaña de una frase de evidencia.

---

## T

### Tabulación
**Categoría:** Diseño de algoritmos
**Definición:** Programación dinámica de abajo hacia arriba: se llena la tabla con un ciclo, de los casos base hacia la respuesta. No depende de la pila de llamadas y a veces permite guardar solo las últimas celdas, con espacio O(1).
