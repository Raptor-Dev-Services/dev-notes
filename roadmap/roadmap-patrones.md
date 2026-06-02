# Roadmap — Patrones de Diseño (GoF + ASP.NET Core)

**Prerequisito:** [Roadmap C#](roadmap-csharp.md) completado. Conocer interfaces y herencia.
**Objetivo:** dominar los 23 patrones GoF y sus aplicaciones concretas en ASP.NET Core.
**Duración estimada:** 4-5 semanas.

---

## Introducción: los 3 grupos del GoF

| Grupo | Qué resuelven | Patrones |
|-------|--------------|---------|
| **Creacionales** | Cómo se crean los objetos | Singleton, Factory Method, Abstract Factory, Builder, Prototype |
| **Estructurales** | Cómo se componen los objetos | Adapter, Bridge, Composite, Decorator, Facade, Flyweight, Proxy |
| **Conductuales** | Cómo se comunican los objetos | Chain of Responsibility, Command, Iterator, Mediator, Memento, Observer, State, Strategy, Template Method, Visitor |

---

## Fase 1 — Patrones Creacionales (semana 1)

> Controlar cómo se instancian los objetos. Muchos ya están en el DI container de .NET.

- [ ] [01 — Singleton](../02-programacion/design-patterns/creational/01-singleton.md)
  - En .NET: `services.AddSingleton<T>()` — el DI container lo gestiona
  - Cuándo es problemático: estado mutable compartido, test isolation

- [ ] [02 — Factory Method](../02-programacion/design-patterns/creational/02-factory-method.md)
  - Delegar la creación a subclases — útil para familias de objetos
  - En .NET: `IHttpClientFactory`, `ILoggerFactory`

- [ ] [03 — Abstract Factory](../02-programacion/design-patterns/creational/03-abstract-factory.md)
  - Familias de objetos relacionados sin especificar clases concretas
  - Ejemplo: factory de repositorios por tenant

- [ ] [04 — Builder](../02-programacion/design-patterns/creational/04-builder.md)
  - Construcción paso a paso de objetos complejos
  - En .NET: `WebApplicationBuilder`, `StringBuilder`, `IHostBuilder`

- [ ] [05 — Prototype](../02-programacion/design-patterns/creational/05-prototype.md)
  - Clonar objetos existentes para crear nuevos
  - En C#: `ICloneable`, records con `with`

**Al terminar esta fase puedes:** elegir la estrategia de creación correcta para cada tipo de objeto.

---

## Fase 2 — Patrones Estructurales (semana 2)

> Componer objetos y clases en estructuras más grandes.

- [ ] [06 — Adapter](../02-programacion/design-patterns/structural/06-adapter.md)
  - Interfaz compatible entre clases incompatibles
  - Caso clave: envolver un SDK externo detrás de `IExampleUserRepository`

- [ ] [07 — Bridge](../02-programacion/design-patterns/structural/07-bridge.md)
  - Separar abstracción de implementación — dos jerarquías independientes

- [ ] [08 — Composite](../02-programacion/design-patterns/structural/08-composite.md)
  - Árbol de objetos donde leaf y composite implementan la misma interfaz
  - Ejemplo: menú anidado, árbol de categorías, permisos jerárquicos

- [ ] [09 — Decorator](../02-programacion/design-patterns/structural/09-decorator.md)
  - Añadir comportamiento sin modificar la clase
  - En ASP.NET Core: `services.Decorate<IExampleUserRepository, CachedExampleUserRepository>()`

- [ ] [10 — Facade](../02-programacion/design-patterns/structural/10-facade.md)
  - Interfaz simplificada a un subsistema complejo
  - Opaca (subsistemas internos) vs transparente (subsistemas inyectables)

- [ ] [11 — Flyweight](../02-programacion/design-patterns/structural/11-flyweight.md)
  - Compartir estado intrínseco para reducir memoria
  - Ejemplo: pool de conexiones, cache de strings

- [ ] [12 — Proxy](../02-programacion/design-patterns/structural/12-proxy.md)
  - Sustituto con control de acceso, logging o lazy loading

**Doc adicional:** [Structural ASP.NET Core](../02-programacion/design-patterns/structural-aspnet.md) — Decorator, Composite, Adapter, Façade con ejemplos reales del back-template (Ferreira Ch.11)

**Al terminar esta fase puedes:** componer comportamiento dinámicamente sin modificar clases existentes.

---

## Fase 3 — Patrones Conductuales (semanas 2-3)

> Cómo se comunican los objetos y se distribuye la responsabilidad.

### Grupo A — Los más usados en ASP.NET Core (leer primero)

- [ ] [16 — Mediator](../02-programacion/design-patterns/behavioral/16-mediator.md)
  - Comunicación centralizada — la base de MediatR en el back-template
  - Cada `IRequest<T>` → `IRequestHandler<T>` es Mediator en acción

- [ ] [20 — Strategy](../02-programacion/design-patterns/behavioral/20-strategy.md)
  - Familia de algoritmos intercambiables — inyectar la estrategia
  - Ejemplo: estrategia de pricing por plan, algoritmos de búsqueda

- [ ] [13 — Chain of Responsibility](../02-programacion/design-patterns/behavioral/13-chain-of-responsibility.md)
  - Cadena de manejadores — pipeline de comportamientos
  - En ASP.NET Core: middleware pipeline, MediatR pipeline behaviors
  - Ferreira Ch.12: intérprete de alarmas + Scrutor Decorate

- [ ] [18 — Observer](../02-programacion/design-patterns/behavioral/18-observer.md)
  - Notificar cambios a múltiples suscriptores
  - En .NET: eventos C#, `INotificationHandler<T>` en MediatR

- [ ] [21 — Template Method](../02-programacion/design-patterns/behavioral/21-template-method.md)
  - Esqueleto de algoritmo con pasos variables en subclases
  - Ferreira Ch.12: SearchMachine abstracta con LinearSearch / BinarySearch

### Grupo B — Complementarios

- [ ] [14 — Command](../02-programacion/design-patterns/behavioral/14-command.md)
  - Encapsular operaciones como objetos — deshacer, enqueue, log
  - Diferencia con Mediator: Command modela la intención, Mediator la enruta

- [ ] [19 — State](../02-programacion/design-patterns/behavioral/19-state.md)
  - Cambiar comportamiento según el estado interno del objeto
  - Ejemplo: orden con estados (Pending → Processing → Shipped → Delivered)

- [ ] [15 — Iterator](../02-programacion/design-patterns/behavioral/15-iterator.md)
  - Recorrer colecciones sin exponer la estructura interna
  - En C#: `IEnumerable<T>`, `yield return`, LINQ

- [ ] [17 — Memento](../02-programacion/design-patterns/behavioral/17-memento.md)
  - Capturar y restaurar estado sin exponer los detalles
  - Ejemplo: historial de cambios, undo/redo

- [ ] [22 — Visitor](../02-programacion/design-patterns/behavioral/22-visitor.md)
  - Operación nueva en una jerarquía de clases sin modificarlas

**Al terminar esta fase puedes:** aplicar el patrón correcto para cada tipo de comunicación entre objetos.

---

## Fase 4 — Patrones específicos de ASP.NET Core (semana 4)

> Más allá del GoF: patrones que emergen del stack .NET.

- [ ] [Object Mappers](../02-programacion/design-patterns/object-mappers.md) — manual mapping, AutoMapper, Mapperly (Ferreira Ch.15)
  - Manual: `IMapper<TSource, TDestination>` — control total, sin magia
  - AutoMapper: perfiles, `ProjectTo`, validación de configuración
  - Mapperly: source generator, código generado visible, sin reflection

- [ ] [DI Patterns](../04-backend/46-di-patterns.md) — Decorator con Scrutor, Keyed Services, captive dependency
  - Scrutor: `Decorate<T, D>()` — el Decorator pattern sin boilerplate
  - Keyed Services: múltiples implementaciones de una misma interfaz por clave

- [ ] [Structural ASP.NET](../02-programacion/design-patterns/structural-aspnet.md) — repaso del grupo estructural aplicado

- [ ] [REPR Pattern](../03-arquitectura/13-repr.md) — Request-EndPoint-Response (Minimal APIs)

**Al terminar esta fase puedes:** aplicar patrones como un toolkit — elegir el correcto en base al problema, no al nombre.

---

## Mapa de patrones → casos de uso reales en el back-template

| Patrón | Dónde aparece en el back-template |
|--------|----------------------------------|
| Mediator | `IMediator.Send(request)` — cada handler es un Mediator |
| Chain of Responsibility | MediatR pipeline behaviors: logging → validation → handler |
| Strategy | Estrategias de pricing, algoritmos de búsqueda inyectables |
| Observer | `INotificationHandler<T>` — eventos de dominio |
| Template Method | `SearchMachine` abstracta, base classes de repositorio |
| Decorator | `CachedExampleUserRepository` vía Scrutor |
| Facade | `IExampleUserService` que agrupa múltiples repositorios |
| Adapter | `ExampleUserRepository` que envuelve Dapper detrás de `IExampleUserRepository` |
| Singleton | `MainDbConnectionFactory` — una instancia global |
| Factory Method | `IHttpClientFactory` para HttpClient por nombre |
| Builder | `WebApplicationBuilder` en Program.cs |
| Proxy | Middleware de autenticación, caché como proxy de repositorio |

---

*Duración total estimada: 4-5 semanas — Nivel objetivo: dominar GoF aplicado a .NET*
