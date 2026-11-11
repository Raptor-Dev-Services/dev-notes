# Tarea - M1 - Medir antes de tocar

**.NET de alto rendimiento**
**Tiempo estimado:** 2 a 3 horas

---

## Parte 1 - Lecturas (45 minutos)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---|---|---|
| `dev-notes/04-backend/19-performance.md` | El proceso de optimización, y Cuándo optimizar / cuándo no | 20 min |
| `dev-notes/04-backend/47-benchmarking.md` | Completa | 20 min |
| Las láminas del módulo 1 | Partes 3, 4 y 5 | 15 min |

Mientras lees la nota 19, marca cada afirmación de rendimiento y anota si trae medición o fuente a la vista. Ese es el criterio que este curso aplica a todo lo que enseña.

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *High-Performance Programming in C# and .NET* (Jason Alls, 2022) | Cap. 5, Application Profiling and Tracing |
| *Tools and Skills for .NET 10* (Mark J. Price, 2025) | Cap. 4, Debugging and Memory Troubleshooting; cap. 13, Benchmarking Performance, Load, and Stress Testing |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Ejercicio: medir una función tuya (1.5 a 2 horas)

Elige **una función de un proyecto tuyo** (o de la API de ejemplo del curso) que:

- sea pura o casi pura (sin red ni base de datos: eso viene en el módulo 5);
- tenga al menos una alternativa razonable de implementación (otra forma de recorrer, de construir un texto, de buscar en una colección...).

### Paso 1 - La línea base

Escribe un benchmark con BenchmarkDotNet que mida **tu versión actual** y **una alternativa**:

- `[MemoryDiagnoser]`, `Baseline = true` en la actual;
- `[Params]` con **tres tamaños**: pequeño, típico y grande de tus datos reales;
- `[GlobalSetup]` con los datos y una **comprobación de que ambas dan el mismo resultado**;
- correr en `-c Release`.

Guarda la salida **completa** de la consola (incluidas las líneas de entorno: procesador, versión de .NET, trabajo).

### Paso 2 - El objetivo

Escribe una frase con un número que diga cuándo está "bien". Por ejemplo, "por llamada debe asignar menos de la mitad", o "el endpoint que la usa debe responder en menos de X ms". Si no puedes formularlo, explica por qué esta función **quizá no valía la pena medirla**: también es una respuesta válida.

### Paso 3 - El cambio y la nueva medición

Aplica **un** cambio (la alternativa) a tu código real, corre el mismo benchmark y guarda la salida completa de nuevo.

### Paso 4 - El informe (máximo una página)

| Sección | Qué lleva |
|---|---|
| Entorno | Procesador, hilos, sistema operativo, versión de .NET, trabajo de BenchmarkDotNet, y si la máquina estaba ocupada |
| Antes y después | Las dos tablas, copiadas tal cual (no reescritas a mano) |
| Lectura | Qué columna sostiene tu conclusión y por qué (`Allocated` o tiempo) |
| Ruido | El `Error` como porcentaje del `Mean` en tus filas principales, y qué te dice |
| Decisión | ¿Adoptas el cambio? ¿Con qué costo de legibilidad? |
| Lo que **no** sabes | Al menos una limitación de tu medición |

### Paso 5 - Una corrida que miente (obligatorio)

Corre el benchmark **una vez en Debug** y pega el mensaje que obtienes. Explica en dos líneas por qué no debes usar números obtenidos así.

---

## Criterios de evaluación

| Criterio | Puntos | Qué se espera |
|---|---|---|
| Método | 20 | Línea base, objetivo, **un** cambio y nueva medición, en ese orden |
| Benchmark correcto | 25 | Release, `Baseline`, `[Params]` con tres tamaños, `MemoryDiagnoser`, resultado devuelto y comprobación de igualdad |
| Evidencia | 20 | Salidas **completas** y sin editar, con su entorno |
| Lectura honesta | 20 | Distingue lo que sostiene `Allocated` de lo que sostiene el tiempo, y cita el `Error` |
| Limitaciones y decisión | 15 | Dice lo que no midió y justifica adoptar o no el cambio |
| **Total** | **100** | |

**Se descuenta** toda cifra de rendimiento sin medición propia o sin fuente oficial, y todo "X veces más rápido" que no salga de una tabla anexa.

---

## Entrega

Un archivo `informe.md` y una carpeta con el proyecto del benchmark. Sin `bin/` ni `obj/`.
