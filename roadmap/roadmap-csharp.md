# Roadmap — C# de cero a avanzado

**Prerequisito:** conocimiento básico de programación (variables, if, loops) en cualquier lenguaje.
**Objetivo:** dominar C# al nivel necesario para trabajar con ASP.NET Core y el back-template.
**Duración estimada:** 3-4 semanas (dedicación media: 2-3 horas/día).

---

## Fase 1 — Tipos y estructura del lenguaje (semana 1)

> Cimientos: cómo se define y organiza el código en C#.

- [ ] [01 — Classes](../02-programacion/csharp/01-classes.md) — clases, sealed, static, partial
- [ ] [16 — Namespaces](../02-programacion/csharp/16-namespaces.md) — namespaces, using global, file-scoped
- [ ] [06 — Properties & Fields](../02-programacion/csharp/06-properties-fields.md) — auto-props, init, required, readonly
- [ ] [07 — Methods](../02-programacion/csharp/07-methods.md) — parámetros, ref/out, extension methods
- [ ] [17 — Enums](../02-programacion/csharp/17-enums.md) — enums, [Flags], extensión

**Al terminar esta fase puedes:** definir clases con propiedades, métodos y enums correctamente.

---

## Fase 2 — Abstracciones y diseño orientado a objetos (semana 1-2)

> Cómo modelar comportamiento y compartir contratos entre tipos.

- [ ] [02 — Interfaces](../02-programacion/csharp/02-interfaces.md) — contratos, implementación explícita
- [ ] [05 — Herencia](../02-programacion/csharp/05-inheritance.md) — abstract, virtual, override, new
- [ ] [03 — Constructors & DI](../02-programacion/csharp/03-constructors-di.md) — constructores, primary constructors
- [ ] [04 — Records](../02-programacion/csharp/04-records.md) — record, record struct, igualdad por valor, with
- [ ] [18 — Attributes](../02-programacion/csharp/18-attributes.md) — atributos custom, Data Annotations

**Al terminar esta fase puedes:** modelar un dominio con interfaces, herencia y records inmutables.

---

## Fase 3 — Tipado avanzado y seguridad (semana 2)

> C# moderno: eliminar null, manejar errores y trabajar con tipos genéricos.

- [ ] [13 — Nullability](../02-programacion/csharp/13-nullability.md) — nullable reference types, ?., ??, !
- [ ] [14 — Exceptions](../02-programacion/csharp/14-exceptions.md) — try/catch/finally, custom exceptions
- [ ] [10 — Generics](../02-programacion/csharp/10-generics.md) — tipos genéricos, constraints, varianza
- [ ] [11 — Pattern Matching](../02-programacion/csharp/11-pattern-matching.md) — switch expressions, is, when, list patterns

**Al terminar esta fase puedes:** escribir código null-safe con tipos genéricos y pattern matching expresivo.

---

## Fase 4 — Colecciones y procesamiento de datos (semana 2-3)

> Trabajar con listas, diccionarios y transformar datos con LINQ.

- [ ] [12 — Collections](../02-programacion/csharp/12-collections.md) — List, Dictionary, IEnumerable, LINQ, Span
- [ ] [15 — Strings](../02-programacion/csharp/15-strings.md) — interpolación, Span, StringBuilder, comparación
- [ ] [11 — Pattern Matching](../02-programacion/csharp/11-pattern-matching.md) *(revisar LINQ en collections)*

**Al terminar esta fase puedes:** procesar colecciones de datos con LINQ, filtrar, transformar y agregar.

---

## Fase 5 — Programación asíncrona (semana 3)

> El modelo async/await es esencial para I/O en APIs — es el tema más importante de esta sección.

- [ ] [08 — Async/Await](../02-programacion/csharp/08-async.md) — Task, ValueTask, ConfigureAwait, CancellationToken
- [ ] [09 — Lifetimes & DI](../02-programacion/csharp/09-lifetimes.md) — Singleton, Scoped, Transient

**Al terminar esta fase puedes:** escribir métodos async correctamente y entender los lifetimes de DI.

---

## Fase 6 — Temas avanzados (semana 3-4)

> Configuración, rendimiento, y el sistema de DI de .NET.

- [ ] [19 — Configuration](../02-programacion/csharp/19-configuration.md) — IConfiguration, Options Pattern
- [ ] [20 — Memoria / GC](../04-backend/20-memoria-gc.md) — GC, IDisposable, LOH *(referencia cruzada)*

**Al terminar esta fase puedes:** configurar aplicaciones .NET, gestionar recursos y entender cómo funciona la memoria.

---

## Siguiente paso

Con C# dominado, el roadmap natural es:

- [Roadmap Backend .NET](roadmap-backend-dotnet.md) — construir APIs REST con ASP.NET Core
- [Roadmap Patrones de Diseño](roadmap-patrones.md) — aplicar los 23 patrones GoF en C#

---

## Referencia rápida: tipos de datos C#

| Tipo | Uso |
|------|-----|
| `string` | Texto — inmutable, reference type |
| `int`, `long`, `decimal` | Números — int para IDs, decimal para dinero |
| `bool` | true/false |
| `Guid` | Identificadores únicos universales |
| `DateTime`, `DateOnly` | Fechas — preferir `DateOnly` para fechas sin hora |
| `record` | DTOs inmutables — igualdad por valor |
| `class` | Entidades con identidad y comportamiento |
| `interface` | Contratos — base de DI y testing |
| `enum` | Valores discretos con nombre |
| `T?` | Tipo nullable — `string?`, `int?`, `Guid?` |

---

*Duración total estimada: 3-4 semanas — Nivel objetivo: C# intermedio/avanzado*
