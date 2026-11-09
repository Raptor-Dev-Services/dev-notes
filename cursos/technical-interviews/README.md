# Preparación para entrevistas técnicas

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/technical-interviews/](https://academy.raptorcloud.dev/technical-interviews/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** C# básico (Dev Notes, sesiones 1 a 3, o equivalente).
**Objetivo:** llegar a una entrevista de desarrollador junior o semi-senior con el CV, la historia de sus proyectos, estructuras de datos, algoritmos, C# y diseño básico de sistemas practicados en voz alta.
**Formato:** 6 módulos de 2 horas (12 h), con clase, quiz, práctica y glosario.
**Para quién:** egresados de Titulación, el público natural de la academia, el semestre en que buscan su primer trabajo.

---

## Módulo 1. CV, portafolio y entrevista conductual (2 h)

> Un proyecto que no se puede contar en dos minutos no existe para quien entrevista.

**Material:** [clase](m01-resume-behavioral/slides.md), [práctica](m01-resume-behavioral/practica.md), [glosario](m01-resume-behavioral/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** CV de una página, el proyecto de titulación contado con resultados, y la entrevista conductual con situación, tarea, acción y resultado.
- [ ] **Lectura:** [`01-fundamentos/09-entrevistas-tecnicas.md`](../../01-fundamentos/09-entrevistas-tecnicas.md).
- [ ] **Práctica:** cada quien cuenta su proyecto en dos minutos y otro integrante lo califica con rúbrica.
- **Libros:** *C# Interview Guide* (Semenenko, 2024), caps. 1, 2 y 8. *The Software Engineer's Guidebook* (Orosz, 2024), primera parte.

**Al terminar puedes:** presentar su experiencia con hechos y resultados, no con adjetivos.

---

## Módulo 2. Complejidad y estructuras lineales (2 h)

> Big O no es teoría: es la pregunta de cuánto empeora cuando los datos crecen.

**Material:** [clase](m02-complexity-linear/slides.md), [práctica](m02-complexity-linear/practica.md), [glosario](m02-complexity-linear/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** notación Big O, arreglos, listas, pilas, colas, diccionarios y conjuntos, y cuándo usar cada uno en .NET.
- [ ] **Lectura:** [`02-programacion/csharp/12-collections.md`](../../02-programacion/csharp/12-collections.md).
- [ ] **Práctica:** cinco problemas cortos, resueltos y explicados en voz alta.
- **Libros:** *C# Data Structures and Algorithms* (Jamro, 2024), caps. 2 a 6. *The Imposter's Handbook* (Conery), capítulos "Big O" y "Data Structures".

**Al terminar puedes:** elegir la estructura correcta y decir su costo sin dudar.

---

## Módulo 3. Árboles, grafos y búsqueda (2 h)

> Muchos problemas de entrevista son un grafo disfrazado.

**Material:** [clase](m03-trees-graphs-search/slides.md), [práctica](m03-trees-graphs-search/practica.md), [glosario](m03-trees-graphs-search/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** árboles, recorridos, grafos, búsqueda en anchura y en profundidad, ordenamiento y búsqueda binaria.
- [ ] **Práctica:** seis problemas cronometrados, en parejas: uno resuelve en voz alta y el otro entrevista.
- **Libros:** *C# Data Structures and Algorithms*, caps. 3, 7 y 8. *50 Algorithms Every Programmer Should Know* (Ahmad, 2023), caps. 3 y 5: ordenamiento, búsqueda y grafos.

**Al terminar puedes:** reconocer el problema de grafos detrás de un enunciado de negocio.

---

## Módulo 4. Diseñar un algoritmo bajo presión (2 h)

> Quien entrevista califica cómo piensa, no solo si llega.

**Material:** [clase](m04-algorithm-design/slides.md), [práctica](m04-algorithm-design/practica.md), [glosario](m04-algorithm-design/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** divide y vencerás, algoritmos voraces, programación dinámica, y cómo pensar en voz alta: aclarar, proponer, probar y mejorar.
- [ ] **Práctica:** entrevista simulada de 45 minutos con rúbrica, grabada.
- **Libros:** *50 Algorithms Every Programmer Should Know*, cap. 4 ("Designing Algorithms"). *C# Interview Guide*, cap. 7.

**Al terminar puedes:** avanzar en un problema que no conocían sin quedarse en blanco.

---

## Módulo 5. C# y .NET en la entrevista (2 h)

> Las preguntas de lenguaje se contestan con código que compila, no con definiciones.

**Material:** [clase](m05-csharp-dotnet/slides.md), [práctica](m05-csharp-dotnet/practica.md), [glosario](m05-csharp-dotnet/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** async y await, ciclo de vida de la inyección de dependencias, genéricos, nulabilidad, LINQ y lo que de verdad preguntan.
- [ ] **Lectura:** [`02-programacion/csharp/08-async.md`](../../02-programacion/csharp/08-async.md), [`09-lifetimes.md`](../../02-programacion/csharp/09-lifetimes.md), [`10-generics.md`](../../02-programacion/csharp/10-generics.md) y [`13-nullability.md`](../../02-programacion/csharp/13-nullability.md).
- [ ] **Práctica:** veinte preguntas típicas, cada una contestada con un ejemplo que corre.
- **Libros:** *C# Interview Guide*, caps. 3 a 6. Para quien va a frontend: *React Interview Guide* (Jonna, 2023), caps. 2 a 5.

**Al terminar puedes:** sostener una conversación técnica de C# y .NET con ejemplos propios.

---

## Módulo 6. Diseño de sistemas y la oferta (2 h)

> La primera oferta se evalúa completa: salario, crecimiento, equipo y lo que se aprende.

**Material:** [clase](m06-system-design-offer/slides.md), [práctica](m06-system-design-offer/practica.md), [glosario](m06-system-design-offer/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** diseño básico de sistemas: API, base de datos, caché y colas. Cómo leer y negociar una oferta.
- [ ] **Lectura:** [`03-arquitectura/12-c4-model.md`](../../03-arquitectura/12-c4-model.md), [`03-arquitectura/07-microservicios.md`](../../03-arquitectura/07-microservicios.md) y [`04-backend/05-caching.md`](../../04-backend/05-caching.md).
- [ ] **Práctica:** diseñar en 30 minutos un acortador de URL o un sistema de reservas, y comparar dos ofertas ficticias.
- **Libros:** *C# Interview Guide*, caps. 9 a 11. *The Software Engineer's Guidebook*, primera parte (cambiar de empleo) y tercera parte (arquitectura de software).

**Al terminar puedes:** dibujar y defender un diseño sencillo, y evaluar una oferta más allá del sueldo.

---

## Proyecto final

Una entrevista simulada completa, grabada: conductual, técnica y de diseño, con retroalimentación por rúbrica de otra persona.


## Código de referencia

En [`referencia/`](referencia/) está la solución del instructor, completa y compilable con .NET 10: el código de las prácticas sale de ahí. Para comprobarla: `dotnet test` dentro de esa carpeta. Úsenla para comparar después de intentarlo, no antes.

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| C# Interview Guide | Konstantin Semenenko | 2024 | Columna del curso |
| C# Data Structures and Algorithms | Marcin Jamro | 2024 | Estructuras y algoritmos en C# |
| 50 Algorithms Every Programmer Should Know | Imran Ahmad | 2023 | Diseño de algoritmos |
| The Imposter's Handbook | Rob Conery | s/f | Complejidad para quien no estudió teoría |
| The Software Engineer's Guidebook | Gergely Orosz | 2024 | Carrera y niveles |
| React Interview Guide | Sudheer Jonna | 2023 | Variante para frontend |

---

*Rogelio Arriaga Gonzalez*
