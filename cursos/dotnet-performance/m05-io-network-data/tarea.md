# Tarea - M5 - I/O, red y base de datos

**.NET de alto rendimiento**
**Tiempo estimado:** 2 a 3 horas

---

## Parte 1 - Lecturas (30 a 45 minutos)

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/05-bases-de-datos/02-indices.md` | Completo | 15 min |
| `dev-notes/05-bases-de-datos/05-dapper-queries.md` | Completo | 10 min |
| `dev-notes/04-backend/13-http-client.md` | Ciclo de vida del cliente | 10 min |
| Las láminas del módulo 5 | Partes 3 y 5 | 10 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *High-Performance Programming in C# and .NET* (Alls, 2022) | Cap. 8, File and Stream I/O; Cap. 9, Enhancing the Performance of Networked Applications; Cap. 11, Benchmarking Relational Data Access Frameworks |
| *Systems Programming with C# and .NET* (Vroegop, 2024) | Cap. 5, Filesystem; Cap. 8, Network |

Los libros se citan por capítulo; cada quien consigue su copia. El de Alls es de .NET 6: compara con lo que mides en .NET 10.

---

## Parte 2 - ¿Dónde está la lentitud? (1.5 a 2 horas)

Elige **un flujo** de un proyecto tuyo (o de uno de práctica) que haga al menos: una consulta a base de datos y una llamada HTTP o lectura de archivo. Trabaja **solo en tu máquina**, con datos de prueba, nunca contra producción.

1. Describe el flujo en tres líneas y el tiempo total que mide desde fuera (anota cómo lo mediste y cuántas veces).
2. Parte el flujo en **tramos** (código propio, base de datos, red o archivo) y mide cada uno con el método de la clase. Si un tramo no puedes medirlo, dilo.
3. Señala el tramo dominante y **un cambio** que lo ataque (índice, `AsNoTracking`, una consulta en vez de N, un `HttpClient` reutilizado, `ReadLines`...).
4. Mide **antes y después** con BenchmarkDotNet o con un conteo reproducible (asignaciones, consultas por petición, sockets).
5. Declara qué parte de tu conclusión **no** se puede extrapolar (por ejemplo, si medías con SQLite o con loopback).

### Entregable

Un documento de 2 a 3 páginas con: el flujo, la tabla de tramos, el cambio, la salida completa de las mediciones (con .NET, paquete y máquina) y una sección "Lo que no puedo concluir".

---

## Criterio de evaluación

| Criterio | Puntos |
|---|---|
| Los tramos están medidos, no supuestos | 25 |
| La conclusión se apoya en una columna robusta (`Allocated`, conteos) y no en un tiempo suelto | 25 |
| Antes y después, con el entorno declarado | 20 |
| "Lo que no puedo concluir" es honesto y concreto | 20 |
| Todo se hizo en local, con datos de prueba | 10 |
| **Total** | **100** |
