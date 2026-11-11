# Glosario - M3 - Colecciones, LINQ y Span

> Definiciones de los términos técnicos del módulo 3.
> Ordenado alfabéticamente.

---

## A

### ArrayPool<T>
**Categoría:** Memoria
**Definición:** Un pool de recursos que permite reutilizar arreglos `T[]`. Se pide un arreglo con `Rent` y se devuelve con `Return`. Es seguro entre hilos. Reduce la presión sobre el GC cuando los arreglos se crean y se tiran con frecuencia.
**En el curso:** `ArrayPool<byte>.Shared.Rent(100000)` no asignó nada en el ciclo medido, mientras que `new byte[100000]` asignó 100 034 B por operación.

---

## B

### BinarySearch
**Categoría:** Colecciones
**Definición:** Búsqueda que, sobre un arreglo **ordenado**, descarta la mitad de los elementos en cada paso. `Array.BinarySearch` devuelve un valor negativo si no encuentra el elemento.
**En el curso:** con 8 y 100 elementos fue más lenta que `List.Contains` en esa corrida, y con 10 000 quedó entre `List` y `HashSet`.

---

### Boxing
**Categoría:** Tipos por valor
**Definición:** Convertir un tipo por valor (como `int`) a `object` o a una interfaz, lo que aloja un objeto en el heap. El proceso inverso es el *unboxing*.
**En el curso:** `List<object>` con 10 000 enteros asignó 312.55 KB; `List<int>`, 39.12 KB.

---

## C

### Camino caliente
**Categoría:** Método
**Definición:** El código que se ejecuta con tanta frecuencia que su costo domina el costo total. Solo ahí vale la pena optimizar, y se identifica con un perfil, no con intuición.
**En el curso:** se cambia LINQ por un ciclo, o `Split` por `Span`, solo si la línea está en el camino caliente.

---

### Captura (de una lambda)
**Categoría:** LINQ
**Definición:** Cuando una expresión lambda usa una variable local del método que la contiene; el compilador crea un objeto de cierre para guardarla.
**En el curso:** `LinqConCaptura` asignó 256 B contra 104 B de la versión sin captura.

---

### Comprobación de equivalencia
**Categoría:** Método
**Definición:** En un benchmark, una verificación (por ejemplo en `[GlobalSetup]`) de que las implementaciones que se comparan devuelven el mismo resultado.
**En el curso:** sin ella, un método que hace menos trabajo parece más rápido.

---

## G

### Gen0 / Gen1 / Gen2 (columnas)
**Categoría:** BenchmarkDotNet
**Definición:** Con `[MemoryDiagnoser]`, el número de recolecciones del GC de cada generación por cada 1000 operaciones.
**En el curso:** `new byte[100000]` mostró 31.2 en Gen2; `ArrayPool`, ninguna.

---

## H

### HashSet<T>
**Categoría:** Colecciones
**Definición:** Conjunto sin orden que localiza un elemento por su hash. Su costo de búsqueda casi no depende del número de elementos, a cambio de más memoria y de no conservar el orden.
**En el curso:** con 10 000 elementos buscó 1 000 valores en 6.8 us contra 415.9 us de `List.Contains`.

---

## L

### Large Object Heap (LOH)
**Categoría:** Memoria
**Definición:** Zona del heap para objetos de 85 000 bytes o más. Se recolecta junto con la generación 2.
**En el curso:** por eso un `byte[100000]` temporal provocó colecciones Gen2.

---

### LINQ
**Categoría:** Lenguaje
**Definición:** Consultas integradas en el lenguaje (`Where`, `Select`, `Sum`...). Priorizan la legibilidad; su costo se mide caso por caso.
**En el curso:** el ciclo equivalente asignó 0 B contra 104 B del LINQ sin captura.

---

## M

### Memory<T>
**Categoría:** Memoria
**Definición:** Envoltura sobre una región contigua de memoria que puede almacenarse en el heap y por eso no tiene las restricciones de `Span<T>`: puede ser campo de clase y usarse en métodos `async`.
**En el curso:** se usa donde un `Span<T>` no se puede (por ejemplo, para `ReadAsync`).

---

## P

### Pool de objetos
**Categoría:** Memoria
**Definición:** Colección de instancias reutilizables para no crear y destruir objetos costosos o grandes.
**En el curso:** `ArrayPool<T>` es el del framework para arreglos.

---

## R

### ReadOnlySpan<T>
**Categoría:** Memoria
**Definición:** Versión de solo lectura de `Span<T>`; puede estar respaldada por una `string`.
**En el curso:** `ReadOnlySpan<char> resto = linea;` permite recortar sin copiar.

---

### ref struct
**Categoría:** Tipos por valor
**Definición:** Un tipo por valor que solo puede vivir en el stack. `Span<T>` es un `ref struct`, por eso no puede ser campo de una clase ni usarse en operaciones asíncronas.
**En el curso:** es la razón de que, para guardar una vista, se use `Memory<T>`.

---

### Rent / Return
**Categoría:** Memoria
**Definición:** Los dos métodos de `ArrayPool<T>`. `Rent(n)` devuelve un búfer de **al menos** `n` elementos; `Return(b, clearArray)` lo devuelve al pool y, con `clearArray: true`, lo limpia antes de reutilizarlo. Tras devolverlo no se debe usar ni devolver de nuevo.
**En el curso:** siempre dentro de un `try/finally`.

---

## S

### Slice
**Categoría:** Memoria
**Definición:** Crear un span sobre una parte de otro, sin copiar. En C#, el operador de rango (`resto[(coma + 1)..]`) lo hace.
**En el curso:** el parser de líneas avanza con rebanadas en lugar de crear `string`.

---

### Span<T>
**Categoría:** Memoria
**Definición:** Vista sobre una región contigua de memoria (un arreglo, un búfer de `stackalloc` o memoria no administrada). Crearla no copia los datos. No puede ser campo de una clase ni usarse en operaciones asíncronas.
**En el curso:** bajó las asignaciones del parser de 238 968 B a 0 B.

---

### struct
**Categoría:** Tipos por valor
**Definición:** Tipo por valor: sus datos viven dentro del objeto, arreglo o stack que lo contiene. Se copia completo al asignarlo o pasarlo como argumento.
**En el curso:** un arreglo de 10 000 `PuntoStruct` asignó 78.15 KB contra 312.52 KB de 10 000 `PuntoClase`.
