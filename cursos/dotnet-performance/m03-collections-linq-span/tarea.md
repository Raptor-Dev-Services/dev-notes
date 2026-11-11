# Tarea - M3 - Colecciones, LINQ y Span

**.NET de alto rendimiento**
**Tiempo estimado:** 2 a 3 horas

---

## Parte 1 - Lecturas (30 a 45 minutos)

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/04-backend/19-performance.md` | Colecciones eficientes y LINQ | 20 min |
| `dev-notes/04-backend/20-memoria-gc.md` | ArrayPool y Value Types vs Reference Types | 20 min |
| Las láminas del módulo 3 | Partes 1, 3 y 4 | 20 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *High-Performance Programming in C# and .NET* (Alls, 2022) | Cap. 3, Predefined Data Types and Memory Allocations; Cap. 6, The .NET Collections; Cap. 7, LINQ Performance |
| *Effective .NET Memory Management* (Williams, 2024) | Cap. 5, Advanced Memory Management Techniques; Cap. 7, Low-Level Programming |

Los libros se citan por capítulo; cada quien consigue su copia. El de Alls es de .NET 6: compara sus técnicas con lo que mides en .NET 10.

---

## Parte 2 - Tres casos, tres decisiones (1.5 a 2 horas)

Elige **tres** fragmentos de código real (de un proyecto tuyo o de uno de práctica) que cumplan, uno para cada tipo:

1. **Una búsqueda en colección** (`List.Contains`, `Where(...).First()` sobre una lista que se consulta muchas veces).
2. **Un LINQ o un `Split`/`Substring`** en un método que se ejecuta con frecuencia.
3. **Un búfer o un objeto temporal** que se crea y se tira (arreglos, `StringBuilder`, DTO pequeño).

Para cada caso entrega:

1. El código original y la **versión alternativa**.
2. Un benchmark de BenchmarkDotNet con `[MemoryDiagnoser]`, `[Params]` con **dos tamaños realistas** y una **comprobación de equivalencia** en `[GlobalSetup]`.
3. La salida **completa** de BenchmarkDotNet (la tabla y el encabezado con el hardware y la versión de .NET).
4. Una **decisión** de una frase: "me quedo con la versión legible" o "cambio", con el motivo.

### La regla de la decisión

- Si la diferencia de **bytes asignados** es grande y el código está en el camino caliente: se puede cambiar.
- Si la diferencia es de tiempo y es menor que el `Error`: **no concluyente**, y se queda la versión legible.
- Si el código no está en el camino caliente (un perfil o una métrica lo demuestra): se queda la legible aunque el benchmark gane.

Al menos **uno** de los tres casos debe terminar en "me quedo con la versión legible" y explicarlo con datos.

---

## Parte 3 - Qué entregar

Un solo documento (Markdown o PDF) con:

- El entorno de medición (hardware, versión de .NET y de BenchmarkDotNet, si fue contenedor o no).
- Los tres casos, cada uno con sus cuatro puntos.
- Un párrafo final: ¿qué colección, patrón de LINQ o estructura cambiarías **primero** en tu proyecto y por qué, según tus datos?

Los tiempos son ruidosos: si corres en un equipo compartido, dilo.

---

## Rúbrica

| Criterio | Puntos |
|----------|--------|
| Cada benchmark incluye comprobación de equivalencia y `[MemoryDiagnoser]` | 20 |
| Se usan dos tamaños distintos y se discute si el resultado cambia con ellos | 15 |
| La salida de BenchmarkDotNet está completa, con entorno | 15 |
| Las conclusiones de tiempo respetan el `Error` (no se afirma lo que el error no sostiene) | 20 |
| Al menos una decisión justificada de conservar la versión legible | 10 |
| La decisión está ligada al camino caliente, no solo al benchmark | 10 |
| Redacción clara y sin cifras sin medición | 10 |
| **Total** | **100** |
