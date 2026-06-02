# Design Patterns — Índice

Guía completa de los 22 patrones de diseño (GoF — Gang of Four), con ejemplos en C# del código fuente en [`docs/design-patterns-C#/`](design-patterns-C#/) y relación con este proyecto.

---

## ¿Qué es un patrón de diseño?

Un patrón de diseño es una solución probada y reutilizable a un problema recurrente en el diseño de software. No es código — es una plantilla o receta que describes cómo resolver un problema. Los patrones fueron catalogados en 1994 por el libro *Design Patterns* (Gang of Four: Gamma, Helm, Johnson, Vlissides).

**Tres categorías:**

| Categoría | Propósito |
|-----------|-----------|
| **Creacionales** | Cómo crear objetos — desacoplar la creación del uso |
| **Estructurales** | Cómo componer objetos — relaciones entre clases e interfaces |
| **Conductuales** | Cómo se comunican los objetos — algoritmos y responsabilidades |

---

## Patrones Creacionales

| # | Patrón | Problema que resuelve | En el proyecto |
|---|--------|----------------------|----------------|
| 1 | [Singleton](design-patterns/creational/01-singleton.md) | Una sola instancia global | `MainDbConnectionFactory` (Singleton DI) |
| 2 | [Factory Method](design-patterns/creational/02-factory-method.md) | Subclase decide qué crear | `MainDbConnectionFactory.OpenConnection()` |
| 3 | [Abstract Factory](design-patterns/creational/03-abstract-factory.md) | Familias de objetos compatibles | No aplica directamente — concepto base del sistema de presenters |
| 4 | [Builder](design-patterns/creational/04-builder.md) | Construcción paso a paso | Encadenamiento fluent de middleware en `Program.cs` |
| 5 | [Prototype](design-patterns/creational/05-prototype.md) | Copiar objetos sin depender de su clase | Record `with` expression en C# |

---

## Patrones Estructurales

| # | Patrón | Problema que resuelve | En el proyecto |
|---|--------|----------------------|----------------|
| 6 | [Adapter](design-patterns/structural/06-adapter.md) | Conectar interfaces incompatibles | `MainDapperDbConnection` adapta Dapper/Npgsql |
| 7 | [Bridge](design-patterns/structural/07-bridge.md) | Separar abstracción de implementación | Repositorios: `IExampleUserRepository` ↔ implementación concreta |
| 8 | [Composite](design-patterns/structural/08-composite.md) | Árbol donde partes y todo se tratan igual | Pipeline de middleware ASP.NET Core |
| 9 | [Decorator](design-patterns/structural/09-decorator.md) | Agregar comportamiento sin modificar clase | `[Authorize]`, `[HttpGet]` como decoradores; pipeline behaviors |
| 10 | [Facade](design-patterns/structural/10-facade.md) | Interfaz simple sobre sistema complejo | `BaseApiController` — oculta complejidad del mediador |
| 11 | [Flyweight](design-patterns/structural/11-flyweight.md) | Compartir estado común entre muchos objetos | Cache de queries SQL compiladas en Dapper |
| 12 | [Proxy](design-patterns/structural/12-proxy.md) | Sustituto que controla acceso al objeto real | `MainDapperDbConnection` como proxy de Dapper |

---

## Patrones Conductuales

| # | Patrón | Problema que resuelve | En el proyecto |
|---|--------|----------------------|----------------|
| 13 | [Chain of Responsibility](design-patterns/behavioral/13-chain-of-responsibility.md) | Pasar petición por cadena de handlers | Pipeline de middleware HTTP; `InteractorPipeline` del mediador |
| 14 | [Command](design-patterns/behavioral/14-command.md) | Encapsular petición como objeto | `IRequest` / `{Accion}Request` — cada caso de uso es un comando |
| 15 | [Iterator](design-patterns/behavioral/15-iterator.md) | Recorrer colección sin exponer su estructura | `IEnumerable<T>`, `foreach`, LINQ |
| 16 | [Mediator](design-patterns/behavioral/16-mediator.md) | Reducir dependencias directas entre objetos | `Common.Messaging.IMediator` — núcleo del flujo Controller→Handler→Presenter |
| 17 | [Memento](design-patterns/behavioral/17-memento.md) | Guardar y restaurar estado de un objeto | No aplica — sin estado mutable que deshacer en este template |
| 18 | [Observer](design-patterns/behavioral/18-observer.md) | Notificar a múltiples objetos de un evento | `INotificationHandler<TResponse>` — el presenter es el observer |
| 19 | [State](design-patterns/behavioral/19-state.md) | Cambiar comportamiento cuando cambia el estado | `ResultViewModel<T>` — estado `IsSuccess` determina el flujo |
| 20 | [Strategy](design-patterns/behavioral/20-strategy.md) | Familia de algoritmos intercambiables | Presenters por acción — cada uno es una estrategia de presentación |
| 21 | [Template Method](design-patterns/behavioral/21-template-method.md) | Esqueleto de algoritmo con pasos extensibles | `BaseApiController` — define el esqueleto del endpoint |
| 22 | [Visitor](design-patterns/behavioral/22-visitor.md) | Separar algoritmo del objeto sobre el que opera | Pattern matching en presenters — el switch es un visitor implícito |

---

## Mapa por nivel de experiencia

### Sub-junior — empezar aquí

1. [Singleton](design-patterns/creational/01-singleton.md) — el más simple y cotidiano
2. [Factory Method](design-patterns/creational/02-factory-method.md) — delegación de creación
3. [Facade](design-patterns/structural/10-facade.md) — ocultar complejidad
4. [Strategy](design-patterns/behavioral/20-strategy.md) — algoritmos intercambiables
5. [Observer](design-patterns/behavioral/18-observer.md) — eventos y notificaciones

### Junior — continuar aquí

6. [Builder](design-patterns/creational/04-builder.md) — construcción fluida
7. [Adapter](design-patterns/structural/06-adapter.md) — integrar código incompatible
8. [Decorator](design-patterns/structural/09-decorator.md) — capas de comportamiento
9. [Command](design-patterns/behavioral/14-command.md) — encapsular acciones
10. [Mediator](design-patterns/behavioral/16-mediator.md) — desacoplar comunicación
11. [Template Method](design-patterns/behavioral/21-template-method.md) — algoritmos con ganchos

### Semi-senior / Senior — completar con

12. [Abstract Factory](design-patterns/creational/03-abstract-factory.md) — familias de objetos
13. [Prototype](design-patterns/creational/05-prototype.md) — clonación profunda/superficial
14. [Bridge](design-patterns/structural/07-bridge.md) — separar abstracciones
15. [Composite](design-patterns/structural/08-composite.md) — estructuras de árbol
16. [Proxy](design-patterns/structural/12-proxy.md) — control de acceso
17. [Flyweight](design-patterns/structural/11-flyweight.md) — optimización de memoria
18. [Chain of Responsibility](design-patterns/behavioral/13-chain-of-responsibility.md) — pipelines
19. [Iterator](design-patterns/behavioral/15-iterator.md) — recorrido de colecciones
20. [State](design-patterns/behavioral/19-state.md) — máquinas de estado
21. [Memento](design-patterns/behavioral/17-memento.md) — deshacer/rehacer
22. [Visitor](design-patterns/behavioral/22-visitor.md) — doble dispatch

---

## Relación de patrones con el proyecto

```
Common.Messaging.IMediator           → Mediator + Command + Observer
Controller → Handler → Presenter     → Chain of Responsibility + Command
ResultViewModel<T>                   → State
Presenter pattern                    → Strategy + Visitor (via pattern matching)
BaseApiController                    → Template Method + Facade
IExampleUserRepository               → Bridge (interface ↔ implementación)
MainDapperDbConnection               → Proxy + Adapter
MainDbConnectionFactory              → Factory Method + Singleton (via DI)
ServiceCollectionEx (DI chain)       → Builder
appsettings.json + env vars          → Abstract Factory (múltiples ambientes)
Record `with` expression             → Prototype
IEnumerable / foreach / LINQ         → Iterator
[Authorize] / middleware pipeline    → Decorator + Chain of Responsibility
```


---

*Rogelio Arriaga Gonzalez*
