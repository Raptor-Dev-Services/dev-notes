# Tarea - M2 - Memoria y recolector de basura

**.NET de alto rendimiento**
**Tiempo estimado:** 2 a 3 horas

---

## Parte 1 - Lecturas (30 a 45 minutos)

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/04-backend/20-memoria-gc.md` | Generaciones, memory leaks, value vs reference types | 25 min |
| Las láminas del módulo 2 | Partes 2, 3 y 4 | 20 min |
| Microsoft Learn, *Fundamentals of garbage collection* | Generaciones; Supervivencia y promoción | 20 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Effective .NET Memory Management* (Williams, 2024) | Cap. 1, Memory Management Fundamentals; Cap. 2, Object Lifetimes and Garbage Collection; Cap. 3, Memory Allocation and Data Structures; Cap. 4, Memory Leaks and Resource Management |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Mide un endpoint propio (1.5 a 2 horas)

Elige **un** endpoint o método de un proyecto tuyo (o uno de práctica) que produzca una respuesta construida a partir de una colección: un reporte, una exportación, un listado transformado.

1. **Línea base.** Mide sus asignaciones con `GC.GetAllocatedBytesForCurrentThread` (si es síncrono) o con un benchmark con `[MemoryDiagnoser]`, a dos tamaños de entrada.
2. **Ubicar.** Señala en el código las 2 o 3 líneas responsables de la mayoría de los bytes y di qué tipo de culpable es (cadenas intermedias, boxing, objetos por elemento, arreglos grandes).
3. **Cambiar.** Haz **un** cambio, el más grande.
4. **Volver a medir.** Mismos tamaños, mismo entorno.
5. **Comprobar** que el resultado es el mismo (hash, comparación o prueba).
6. **Concluir.** Qué mejoró, cuánto (en bytes) y qué no se midió.

Entorno: .NET que uses, hardware, si hubo carga en la máquina. Los tiempos, si los reportas, se llaman "ruido" salvo que los sustentes con más corridas.

---

## Parte 3 - Una pregunta de diseño (30 minutos)

Responde en media página: **en tu entregable, ¿qué objetos viven más que la petición** (estáticos, cachés, eventos) y por qué no se vuelven un problema, o qué harías para acotarlos? No necesitas medición: usa lo visto en la Parte 5 de la clase.

---

## Entregable

Un documento (Markdown o PDF) con las tres partes, la salida literal de las mediciones (pegada, no resumida) y el código antes y después.

## Criterio de evaluación

| Criterio | Peso | Qué se espera |
|---|---:|---|
| Medición antes y después | 30 % | Mismos tamaños y entorno; salida pegada; bytes, no solo impresiones |
| Diagnóstico | 20 % | Señala las líneas y el tipo de culpable con evidencia |
| Cambio y equivalencia | 20 % | Un cambio justificado y comprobación de que el resultado no cambió |
| Honestidad de las conclusiones | 20 % | Distingue lo firme (bytes) del ruido (tiempos); no extrapola |
| Parte 3 | 10 % | Razonamiento correcto sobre raíces y generaciones |

Un porcentaje de mejora sin medición o sin comprobar que el resultado es igual vale 0 en esa parte.
