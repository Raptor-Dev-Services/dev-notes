# Glosario - M2 - Memoria y recolector de basura

> Definiciones de los términos técnicos del módulo 2.
> Ordenado alfabéticamente.

---

## A

### Allocated
**Categoría:** Medición
**Definición:** Bytes asignados por operación, según BenchmarkDotNet con `[MemoryDiagnoser]`. Sale de `GC.GetAllocatedBytesForCurrentThread` y no depende de qué tan cargada esté la máquina.
**En el curso:** `Concatenando` con 1000 pedidos asignó 33665.39 KB; `ConStringBuilder`, 146.73 KB.

---

### Asignación (allocation)
**Categoría:** Memoria
**Definición:** Crear un objeto en el heap administrado. En .NET es barata porque el runtime avanza un puntero; su costo llega después, en la recolección.
**En el curso:** El módulo mide cuántos bytes asigna una operación para estimar cuánto trabajo le deja al GC.

---

## B

### Boxing
**Categoría:** Memoria
**Definición:** Envolver un tipo por valor en un objeto (por ejemplo, un `int` en un `object`). Cada boxing asigna un objeto en el heap.
**En el curso:** `List<object>` con 10,000 enteros asignó 312.55 KB; `List<int>`, 39.12 KB.

---

## C

### Compactación
**Categoría:** Recolector
**Definición:** Fase de una recolección que junta los objetos vivos y elimina los huecos de los muertos. En el LOH normalmente no se hace, porque copiar objetos grandes es caro.
**En el curso:** La documentación dice que el LOH solo se compacta bajo demanda o con ciertos límites de memoria.

---

### Concurrent GC
**Categoría:** Recolector
**Definición:** Modo del recolector que reduce las pausas haciendo parte del trabajo mientras la aplicación sigue corriendo.
**En el curso:** BenchmarkDotNet imprimió `GC = Concurrent Workstation` en las mediciones del curso.

---

## E

### Escape analysis
**Categoría:** JIT
**Definición:** Análisis del compilador JIT para saber si un objeto puede salir del método que lo crea. Si no escapa, el JIT puede asignarlo en el stack. .NET 10 amplió este análisis a arreglos, delegados y campos de structs, según el blog oficial.
**En el curso:** Un ciclo de `new byte[100]` sin usar el resultado asignó 1.3 MB; guardando cada arreglo en un campo estático, 128.0 MB.

---

## G

### GC (recolector de basura)
**Categoría:** Recolector
**Definición:** Componente del runtime que administra el heap: marca lo alcanzable desde las raíces y libera el resto.
**En el curso:** El curso lo trata como un costo medible, no como una caja negra.

---

### GC.GetAllocatedBytesForCurrentThread
**Categoría:** Medición
**Definición:** Método que devuelve los bytes asignados por el hilo actual desde que empezó. La diferencia entre dos llamadas mide un tramo de código.
**En el curso:** El endpoint `/reporte` lo usa para la cabecera `X-Bytes-Asignados`.

---

### GC.GetGeneration
**Categoría:** Medición
**Definición:** Devuelve la generación actual de un objeto. Los del LOH regresan 2.
**En el curso:** `byte[84976]` regresó 2 al nacer; `byte[84975]`, 0.

---

### Gen 0, gen 1, gen 2
**Categoría:** Recolector
**Definición:** Las tres generaciones del heap. Los objetos nuevos nacen en gen 0; los que sobreviven a una recolección suben a gen 1 y luego a gen 2. Recolectar una generación incluye las más jóvenes.
**En el curso:** `GC.GetGeneration` devolvió 0, 1 y 2 para el mismo objeto tras cada `GC.Collect` de la demo.

---

## H

### Heap administrado
**Categoría:** Memoria
**Definición:** Región de memoria donde el runtime asigna los objetos (tipos por referencia). Se divide en heap de objetos pequeños y LOH.
**En el curso:** Todas las medidas del módulo cuentan bytes en este heap.

---

## L

### LOH (Large Object Heap)
**Categoría:** Memoria
**Definición:** Parte del heap para objetos de 85,000 bytes o más, normalmente arreglos. Se recolecta junto con gen 2 y normalmente no se compacta.
**En el curso:** Medido en Linux: `byte[84976]` ya nace en el LOH.

---

## P

### Promoción
**Categoría:** Recolector
**Definición:** Pasar un objeto que sobrevivió a una recolección a la generación siguiente.
**En el curso:** Un objeto guardado en una caché estática termina en gen 2.

---

## R

### Raíz (root)
**Categoría:** Recolector
**Definición:** Punto de partida del GC para saber qué está vivo: campos estáticos, variables locales en el stack, registros de la CPU, GC handles y la cola de finalización.
**En el curso:** Un diccionario estático mantiene vivos todos los objetos que contiene.

---

## S

### Stack
**Categoría:** Memoria
**Definición:** Región de memoria de cada hilo donde viven los marcos de llamada: parámetros y variables locales. Un tipo por valor vive donde lo declares, no siempre en el stack.
**En el curso:** Un `struct` dentro de una `class` vive dentro de ese objeto, en el heap.

---

### StringBuilder
**Categoría:** Strings
**Definición:** Clase que acumula texto en un búfer mutable y crea el `string` final una sola vez. Admite una capacidad inicial.
**En el curso:** Con 1000 pedidos bajó el reporte de 33665.39 KB a 146.73 KB; la capacidad inicial, a 145.45 KB.

---

### Supervivencia
**Categoría:** Recolector
**Definición:** Que un objeto siga alcanzable tras una recolección. La frecuencia y duración de los GC dependen del volumen de asignaciones y de lo que sobrevive.
**En el curso:** Lo que sobrevive se copia y sube de generación: es lo caro.

---

## T

### Tipo por referencia
**Categoría:** Memoria
**Definición:** Tipo cuyas variables guardan una referencia a un objeto en el heap (`class`, `string`, arreglos, delegados).
**En el curso:** Cada elemento de un arreglo de `class` es un objeto aparte.

---

### Tipo por valor
**Categoría:** Memoria
**Definición:** Tipo cuya variable guarda el valor mismo (`int`, `bool`, `struct`). Boxing lo convierte en objeto.
**En el curso:** Un arreglo de 10,000 `struct` asignó 78.15 KB; el de `class`, 312.52 KB.

---

## W

### Workstation GC / Server GC
**Categoría:** Recolector
**Definición:** Los dos modos del recolector documentados por Microsoft; el de servidor está pensado para aplicaciones con muchos hilos. Qué conviene se decide con medición propia.
**En el curso:** Las mediciones del curso se hicieron en workstation concurrente.

---

*Rogelio Arriaga Gonzalez*
