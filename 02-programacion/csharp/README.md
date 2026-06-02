# CSharpPrimer — Índice

Guía completa de C# y .NET orientada a este proyecto. Cada documento cubre un tema en profundidad máxima, con ejemplos del código real.

---

## Documentos

| # | Documento | Temas cubiertos |
|---|-----------|-----------------|
| 1 | [Clases](csharp-primer/01-classes.md) | `class`, `sealed`, `abstract`, `static`, `partial`, modificadores de acceso (`public`, `private`, `protected`, `internal`), object initializers, nested classes |
| 2 | [Interfaces](csharp-primer/02-interfaces.md) | `interface`, contratos, coding-to-abstractions, por qué no inyectar la clase concreta, default interface methods, implementación explícita, marker interfaces, interface vs clase abstracta |
| 3 | [Constructores y DI](csharp-primer/03-constructors-di.md) | Constructor por defecto, con parámetros, encadenado (`this()`), base (`base()`), estático, primary constructor C# 12, DI: el problema del `new`, cómo funciona el container, registro de dependencias, circular dependency |
| 4 | [Records](csharp-primer/04-records.md) | `record` vs `class`, comparación por valor, posicional vs propiedades, `with`, deconstruction, `record struct`, `abstract record`, `sealed record`, herencia, cuándo NO usar record, trampa de `ISuccess<TSelf>` |
| 5 | [Herencia y Polimorfismo](csharp-primer/05-inheritance.md) | `:`, `virtual`, `override`, `abstract`, `base()`, `sealed override`, polimorfismo, herencia en records, `is` + casting, shadowing (`new`), anti-patrones |
| 6 | [Propiedades y Campos](csharp-primer/06-properties-fields.md) | Field, `readonly`, `const`, `static`, auto-property, backing field, `get`/`set`, `private set`, `init`, propiedades calculadas, `required` (C# 11) |
| 7 | [Métodos](csharp-primer/07-methods.md) | Anatomía, retornos (`void`, `Task`, `Task<T>`), parámetros (obligatorios, opcionales, named, `params`, `ref`, `out`, `in`), sobrecarga, métodos de extensión, expression-bodied, métodos locales, lambdas |
| 8 | [async / await / Task](csharp-primer/08-async.md) | Por qué async, `Task<T>`, la cadena async, `_ = await`, `Task.WhenAll`, `Task.WhenAny`, `CancellationToken` completo, `CancellationTokenSource`, `ValueTask<T>`, errores comunes (async void, .Result, olvidar await) |
| 9 | [DI Lifetimes](csharp-primer/09-lifetimes.md) | `Singleton`, `Scoped`, `Transient` con analogías, tabla comparativa, captive dependency (el error más peligroso), `IDisposable` + lifetimes, acceso fuera del contexto HTTP, validación en desarrollo |
| 10 | [Genéricos](csharp-primer/10-generics.md) | `<T>` en clases y métodos, múltiples parámetros, constraints (`where`), genérico abierto en DI, `ISuccess<T>`, inferencia de tipos, covarianza/contravarianza |
| 11 | [Pattern Matching](csharp-primer/11-pattern-matching.md) | `is` (verificación, asignación, null), `switch` expression, `when`, patrones (tipo, constante, relacional, propiedad, posicional, `and/or/not`), exhaustiveness, presenters del proyecto |
| 12 | [Colecciones y LINQ](csharp-primer/12-collections.md) | `List<T>`, `IEnumerable<T>`, `IReadOnlyCollection<T>`, `Dictionary`, `HashSet`, arrays, tabla de elección, LINQ completo (Where, Select, OrderBy, First, Count, Any, GroupBy, SelectMany, paginación), lazy vs materializado, `IAsyncEnumerable<T>` |
| 13 | [Nulabilidad](csharp-primer/13-nullability.md) | Nullable reference types, `?` en tipo, `?.`, `??`, `??=`, `!` (null-forgiving), flow analysis, guard clauses, nullable en colecciones, `int?` vs `int`, activar en `.csproj` |
| 14 | [Excepciones](csharp-primer/14-exceptions.md) | `try/catch/finally`, capturar tipos específicos, `when`, `throw` vs `throw ex`, inner exception, excepciones personalizadas, `OperationCanceledException`, global exception handler, errores comunes |
| 15 | [Strings](csharp-primer/15-strings.md) | `string.Empty`, interpolación `$""`, formatos (números, fechas, Guid), raw strings `""" """`, verbatim `@""`, `StringBuilder`, operaciones comunes, comparación (`StringComparison`), encoding, Range/Index |
| 16 | [Namespaces y using](csharp-primer/16-namespaces.md) | `namespace`, file-scoped namespace C# 10, convención del proyecto, `using`, `global using`, `using static`, aliases, `using` para `IDisposable` |
| 17 | [Enums](csharp-primer/17-enums.md) | Declaración, tipo subyacente, valores explícitos, `[Flags]` con bitwise, enums en DB (int vs string), extensiones sobre enum, `Enum.GetValues<T>()`, `Enum.TryParse<T>()`, serialización JSON |
| 18 | [Atributos](csharp-primer/18-attributes.md) | Routing (`[HttpGet]`, constraints), binding (`[FromBody]`, `[FromQuery]`), autorización (`[Authorize]`, `[AllowAnonymous]`), DataAnnotations, xUnit (`[Fact]`, `[Theory]`), JSON (`[JsonPropertyName]`, `[JsonIgnore]`), atributos personalizados, reflexión |
| 19 | [Configuración](csharp-primer/19-configuration.md) | Capas de config (appsettings → env vars → CLI), `IConfiguration`, `IOptions<T>`, `IOptionsSnapshot`, `IOptionsMonitor`, `ValidateOnStart`, separador `__` en env vars, User Secrets, prioridades |

---

## Mapa por nivel de experiencia

### Sub-junior — empezar aquí

1. [Clases](csharp-primer/01-classes.md) — qué es una clase, modificadores de acceso, tipos
2. [Propiedades y Campos](csharp-primer/06-properties-fields.md) — cómo almacena datos un objeto
3. [Métodos](csharp-primer/07-methods.md) — cómo hace cosas un objeto
4. [Constructores y DI](csharp-primer/03-constructors-di.md) — cómo nace un objeto, DI básico
5. [Interfaces](csharp-primer/02-interfaces.md) — contratos y por qué son centrales
6. [Namespaces y using](csharp-primer/16-namespaces.md) — organización del código
7. [Strings](csharp-primer/15-strings.md) — manipulación de texto

### Junior — continuar aquí

8. [Records](csharp-primer/04-records.md) — tipos inmutables, base de DTOs y Responses
9. [Herencia y Polimorfismo](csharp-primer/05-inheritance.md) — reutilizar y extender
10. [Nulabilidad](csharp-primer/13-nullability.md) — evitar NullReferenceException
11. [Colecciones y LINQ](csharp-primer/12-collections.md) — trabajar con listas
12. [Excepciones](csharp-primer/14-exceptions.md) — manejo de errores robusto
13. [async / await / Task](csharp-primer/08-async.md) — operaciones asíncronas

### Semi-senior / Senior — completar con

14. [DI Lifetimes](csharp-primer/09-lifetimes.md) — cuánto vive cada objeto
15. [Genéricos](csharp-primer/10-generics.md) — código reutilizable entre tipos
16. [Pattern Matching](csharp-primer/11-pattern-matching.md) — lógica expresiva y segura
17. [Enums](csharp-primer/17-enums.md) — tipos enumerados, Flags, serialización
18. [Atributos](csharp-primer/18-attributes.md) — metadata, ASP.NET Core, xUnit, personalización
19. [Configuración](csharp-primer/19-configuration.md) — IOptions, capas, User Secrets

---

## Relación de documentos con el código del proyecto

| Documento | Dónde se aplica en el proyecto |
|-----------|-------------------------------|
| Clases | Todos los handlers, repos, sql, presenters |
| Interfaces | `IExampleUserRepository`, `IMediator`, `ISuccess<T>`, todas las de `Common` |
| Constructores y DI | `ServiceCollectionEx.cs` de cada capa, `Program.cs` |
| Records | Requests, Responses, DTOs en `Application/` |
| Herencia | `BaseApiController`, jerarquía de Responses |
| Propiedades | Entidades en `Domain/Entities/`, DTOs |
| Métodos | Sql classes, repositorios, handlers |
| async/await | Todo el acceso a datos y handlers |
| Lifetimes | `ServiceCollectionEx.cs` — Singleton/Scoped decisions |
| Genéricos | `ResultViewModel<T>`, `IRequestHandler<,>`, `ISuccess<T>` |
| Pattern matching | Todos los Presenters |
| Colecciones | Responses de colecciones, LINQ en handlers |
| Nulabilidad | Interfaces de repositorio (retornan `T?`), configuración |
| Excepciones | Controllers (try/catch), Program.cs |
| Strings | SQL raw strings, logging, configuración |
| Namespaces | Estructura de carpetas de cada capa |
| Enums | Estados en entidades de dominio, roles, tipos de error |
| Atributos | Controllers (`[Route]`, `[Authorize]`, `[HttpGet]`), DTOs (`[Required]`), tests (`[Fact]`) |
| Configuración | `JwtAuthExtensions.cs`, `Infrastructure/ServiceCollectionEx.cs`, `appsettings*.json` |


---

*Rogelio Arriaga Gonzalez*
