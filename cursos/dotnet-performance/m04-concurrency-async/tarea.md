# Tarea - M4 - Concurrencia y async

**.NET de alto rendimiento**
**Tiempo estimado:** 2 a 3 horas

---

## Parte 1 - Lecturas (30 a 45 minutos)

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/02-programacion/csharp/08-async.md` | Toda la nota | 20 min |
| Las láminas del módulo 4 | Partes 1 y 2 | 20 min |
| [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices) | "Avoid blocking calls" | 10 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *High-Performance Programming in C# and .NET* (Alls, 2022) | Cap. 14, Multi-Threaded Programming; Cap. 15, Parallel Programming; Cap. 16, Asynchronous Programming |
| *Systems Programming with C# and .NET* (Vroegop, 2024) | Cap. 4 |
| *Tools and Skills for .NET 10* (Price, 2025) | Cap. 9, Multitasking and Concurrency |

Los libros se citan por capítulo; cada quien consigue su copia. El de Alls es de .NET 6: compara con lo que mides en .NET 10.

---

## Parte 2 - Auditar y medir una corrección (1.5 a 2 horas)

1. **Auditoría.** En un proyecto tuyo (o uno de práctica) busca `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `Task.Run` envuelto en `await` inmediato, y `async void`:
   ```bash
   grep -rnE "\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)|async void" --include=*.cs .
   ```
   Entrega una tabla: archivo, línea, qué hace, si está en un camino de petición (caliente) o en el arranque, y qué propones.
2. **Una corrección medida.** Elige **un** caso en un camino de petición y corrígelo. Si tu proyecto no tiene ninguno, reproduce uno en la API de ejemplo (un endpoint nuevo con `.Result`).
3. **Medición antes y después**, con el cliente `Carga` **solo contra una copia local** de la API: calentamiento de 2 clientes durante 3 s, luego 100 clientes durante 15 s, API con `DOTNET_PROCESSOR_COUNT=2`. Anota peticiones, errores, p50 y `hilosDelPool` de `/diag` (o el equivalente de tu proyecto).
4. **Repite cada medición dos veces.** Si las dos cifras de peticiones por segundo son muy distintas, dilo y explica por qué no puedes dar un "X veces".
5. **Una condición de carrera.** Escribe un fragmento de 10 líneas con un contador compartido, demuestra la pérdida de actualizaciones y corrígelo con `Interlocked`. Entrega las dos salidas.

---

## Qué entregar

Un documento con: la tabla de la auditoría, el código antes y después, las mediciones (con el comando completo, el entorno y las dos repeticiones) y una conclusión de un párrafo que distinga **lo que mediste** de **lo que supones**.

## Criterios de evaluación

| Criterio | Puntos |
|---|---|
| La auditoría es completa y distingue caminos calientes de arranque | 20 |
| La corrección elimina el bloqueo de todo el camino (no solo de una capa) | 20 |
| Las mediciones traen calentamiento, entorno, comando y dos repeticiones | 25 |
| La conclusión no afirma cifras ni "X veces" que las mediciones no sostienen | 20 |
| La condición de carrera se demuestra con salida real y se corrige | 15 |
| **Total** | **100** |

Reglas: la carga **solo** contra copias locales; nada de pruebas de carga contra servicios reales.
