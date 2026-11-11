# Tarea - M6 - Diagnóstico en producción

**.NET de alto rendimiento**
**Tiempo estimado:** 2 a 3 horas

---

## Parte 1 - Lecturas (30 a 45 minutos)

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/04-backend/48-diagnostico-en-produccion.md` | Completo | 15 min |
| `dev-notes/10-cloud/17-tracing-distribuido.md` | Conceptos fundamentales | 10 min |
| Las láminas del módulo 6 | Partes 3 y 4 | 15 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Effective .NET Memory Management* (Williams, 2024) | Cap. 6, Memory Profiling and Optimization |
| *Systems Programming with C# and .NET* (Vroegop, 2024) | Cap. 10, Systems Check-Ups; Cap. 11, Debugging |
| *Tools and Skills for .NET 10* (Price, 2025) | Cap. 5, Logging, Tracing, and Metrics for Observability |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Su runbook de diagnóstico de una API que crece en memoria (1.5 a 2 horas)

Escribe un **runbook**: un documento que otra persona de tu equipo pueda seguir a las tres de la mañana, sin ti, para diagnosticar una API que crece en memoria.

1. **Antes de empezar (pre-requisitos):** herramientas y versión, usuario, `TMPDIR`, permisos, si el servicio corre en contenedor (y si hay límite de memoria), a quién avisar y qué permiso se necesita para tomar un volcado.
2. **Confirmar con contadores:** el comando exacto de `dotnet-counters`, qué contadores mirar y **qué patrón significa fuga** y cuál no.
3. **Separar heap de working set:** qué cifra mira cada quien y por qué.
4. **Volcado:** comando, tipo (`Full`, `Heap`, `Mini` o `Triage`) y por qué ese; dónde se guarda; quién puede leerlo; cuándo se borra.
5. **Análisis:** `dumpheap -stat` y `gcroot`, con una salida de ejemplo **de tu práctica** (la del módulo).
6. **Decisión:** cómo se pasa de "este tipo ocupa la memoria" a "esto lo retiene", y qué se corrige.
7. **Verificación:** cómo se comprueba, con los mismos contadores, que quedó corregido.
8. **Qué NO hacer:** al menos tres cosas, con su razón.

**Pruébalo:** pide a una compañera o compañero que siga **solo tu documento** sobre la API de ejemplo con `CACHE=fuga` (módulo 6, práctica) y anota dónde se atoró. Corrige el documento con lo que encontró.

Todo, en un entorno local: **nunca** pruebes el runbook contra un servicio de producción o ajeno.

---

## Entregable

Un solo archivo `runbook-memoria.md` con las 8 secciones, más una nota breve (media página) con lo que cambió después de la prueba con otra persona.

---

## Criterio de evaluación

| Criterio | Puntos | Qué se revisa |
|----------|--------|---------------|
| Orden y comandos | 25 | Cada paso tiene su comando exacto y se puede seguir sin adivinar |
| Interpretación | 25 | Distingue fuga de subida normal, y heap de working set |
| Seguridad | 15 | Trata el volcado como dato sensible y explica cuándo se borra |
| Evidencia propia | 15 | Incluye salidas reales de la práctica, con su entorno |
| Verificación | 10 | Dice cómo se comprueba la corrección con los mismos contadores |
| Prueba con otra persona | 10 | Documenta dónde se atoró y qué cambió |

**Total: 100 puntos.** Lo que afirmes sobre una herramienta lleva su fuente (documentación oficial) o tu medición; lo que no puedas respaldar, no lo escribas.
