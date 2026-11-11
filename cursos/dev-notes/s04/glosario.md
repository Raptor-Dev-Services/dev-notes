# Glosario Técnico  -  S04

### Patrones Creacionales y Estructurales

**Dev Notes**
*Glosario Completo de la Sesión*

> Términos organizados alfabéticamente. Cada entrada incluye categoría, definición y ejemplo concreto en el proyecto de mensajería.

---

## A

### Abstract Factory
**Categoría:** Patrón de diseño  -  Creacional
**Definición:** Patrón que proporciona una interfaz para crear familias de objetos relacionados sin especificar sus clases concretas. A diferencia de Factory Method (que crea un solo tipo), Abstract Factory crea grupos de objetos que trabajan juntos. Cambiar de una fábrica a otra reemplaza toda la familia a la vez sin modificar el código consumidor.
**En el proyecto:** `IRepositoryFactory` con métodos `CreateUserProfileRepository()`, `CreateContactRepository()` y `CreateConversationRepository()`. `EfCoreRepositoryFactory` la implementa para producción y `InMemoryRepositoryFactory` para tests. Cambiar de una a otra en DI no toca ningún Handler.

---

### Adaptee
**Categoría:** Patrón de diseño  -  Adapter (rol)
**Definición:** La clase existente cuya interfaz es incompatible con lo que el sistema espera. El Adaptee tiene su propia API que no puede modificarse (o no conviene hacerlo). El patrón Adapter lo envuelve para traducir su interfaz a la que el sistema necesita.
**En el proyecto:** `AppDbContext` es el Adaptee. Tiene su propia API EF Core (`DbSet<T>`, LINQ, `SaveChangesAsync`), que es incompatible directamente con el contrato `IUserProfileRepository` que Application necesita.

---

### Adapter
**Categoría:** Patrón de diseño  -  Estructural
**Definición:** Patrón que convierte la interfaz de una clase en otra interfaz que los clientes esperan. Permite que clases con interfaces incompatibles trabajen juntas sin modificar su código fuente. El Adapter actúa como un traductor entre el sistema existente (Adaptee) y el sistema que lo consume (Target).
**En el proyecto:** `UserProfileRepository` es el Adapter. Implementa `IUserProfileRepository` (Target) y usa internamente `AppDbContext` (Adaptee). `ContactRepository`, `ConversationRepository` y `AgentRepository` aplican el mismo patrón en sus módulos respectivos.

---

### ADR (Architecture Decision Record)
**Categoría:** Arquitectura  -  Documentación
**Definición:** Documento breve que registra una decisión arquitectónica importante: el contexto del problema, las opciones consideradas, la decisión tomada y sus consecuencias. Los ADRs forman un historial de razonamiento que ayuda a equipos futuros a entender por qué el sistema es como es, en lugar de solo cómo es.
**En el proyecto:** El equipo creó `ADR-001` en S02 documentando la elección de Modular Monolith sobre microservicios para el sistema de mensajería.

---

### AgentRole
**Categoría:** C#  -  Enum, Dominio
**Definición:** Tipo enumeración (`enum`) que representa los roles disponibles para los agentes del CRM. Los valores de un enum son singletons por naturaleza en .NET: `AgentRole.Admin` siempre es el mismo objeto en memoria, sin importar cuántas veces se referencie, lo que lo convierte en un ejemplo natural del patrón Flyweight.
**En el proyecto:** `public enum AgentRole { Admin, Agent, Viewer }` definido en el dominio. Controla qué puede hacer cada usuario: los Admin configuran canales y workspace, los Agent atienden conversaciones, los Viewer solo consultan.

---

### Application Layer (capa de aplicación)
**Categoría:** Arquitectura  -  Clean Architecture
**Definición:** Capa de Clean Architecture que orquesta los casos de uso. Contiene Handlers, Requests, Responses y define interfaces de repositorios que la Infrastructure implementa. No tiene dependencias de frameworks ni de bases de datos: depende solo del Domain. Coordina el flujo: recibe un Request, consulta o modifica el dominio, produce una Response.
**En el proyecto:** Cada módulo tiene su propia capa Application, por ejemplo `Contacts.Application/UseCases/GetContact/`. Aquí viven los Handlers que producen las Responses que el Presenter consumirá.

---

### AppDbContext
**Categoría:** EF Core  -  C#
**Definición:** Clase central de Entity Framework Core que representa la sesión con la base de datos. Hereda de `DbContext` y expone `DbSet<T>` por cada entidad mapeada. Registrado como Scoped en DI (un objeto por request HTTP), actúa como Unit of Work y coordina el tracking de cambios y las transacciones.
**En el proyecto:** `AppDbContext` se registra vía `builder.Services.AddMainDatabase(builder.Configuration)` y es el Adaptee que `UserProfileRepository`, `ContactRepository` y demás repositorios adaptan a sus respectivas interfaces de dominio.

---

### AsNoTracking
**Categoría:** EF Core  -  Performance
**Definición:** Método de extensión de EF Core que indica al DbContext que no rastree los cambios en las entidades retornadas por una consulta. Mejora el rendimiento en lecturas porque se elimina el overhead del Change Tracker. Se usa en todas las consultas de solo lectura, como los métodos `GetBy...Async` de los repositorios.
**En el proyecto:** `_db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(...)` en `UserProfileRepository.GetByPublicIdAsync`. Se aplica consistentemente en todos los repositorios del CRM para consultas que no necesitan persistir cambios.

---

### Atomic Commit (commit atómico)
**Categoría:** Git  -  Convenciones
**Definición:** Principio que establece que cada commit debe representar exactamente un concepto o cambio coherente. Un commit atómico no mezcla features, bugfixes y refactors. Facilita `git bisect` para localizar bugs, `git revert` para deshacer cambios sin efectos colaterales y los code reviews al dar un propósito claro a cada cambio.
**En el proyecto:** Aplicar desde S04: `feat(conversations): agregar filtro por estado de conversación` es atómico. Mezclar ese cambio con un fix del login y un refactor del repositorio en un solo commit viola este principio.

---

## B

### Bridge
**Categoría:** Patrón de diseño  -  Estructural
**Definición:** Patrón que separa una abstracción de su implementación, permitiendo que ambas evolucionen de manera independiente. La abstracción define la interfaz pública y mantiene una referencia a la implementación. Múltiples implementaciones pueden sustituirse sin cambiar el código que usa la abstracción. A diferencia de Adapter (que resuelve incompatibilidad existente), Bridge se diseña desde el principio para desacoplar.
**En el proyecto:** `IUserProfileRepository` (en `Users.Domain`) es la abstracción. Las implementaciones son `UserProfileRepository` (EF Core, producción), `DapperUserProfileRepository` (SQL explícito, alta performance) e `InMemoryUserProfileRepository` (tests). El Handler nunca cambia al sustituir la implementación.

---

### Builder
**Categoría:** Patrón de diseño  -  Creacional
**Definición:** Patrón que permite construir objetos complejos paso a paso. Separa la construcción de un objeto de su representación. Cada paso es opcional y retorna el mismo builder (fluent interface), lo que permite encadenar llamadas. El objeto final solo se crea al llamar al método terminal (`Build()`, `ValidateOnStart()`, etc.).
**En el proyecto:** El Options Pattern de .NET usa Builder: `builder.Services.AddOptions<JwtSettings>().BindConfiguration(JwtSettings.SectionName).ValidateDataAnnotations().ValidateOnStart()`. También la construcción de entidades en `InsertAsync` de los repositorios: crear un `UserProfile` inicializando propiedades paso a paso antes de pasarlo a EF Core.

---

### Burn Rate
**Categoría:** Gestión de proyectos  -  Métricas
**Definición:** Tasa a la que un equipo consume el presupuesto o los recursos disponibles durante un período. Se expresa como gasto por unidad de tiempo (por sprint, por mes). Los stakeholders de management lo usan para proyectar cuándo se agotará el presupuesto y tomar decisiones de priorización o scope.
**En el proyecto:** Métrica relevante para comunicar el avance del sistema de mensajería a management: cuánto del presupuesto de desarrollo se ha consumido vs. el progreso real en módulos entregados.

---

## C

### CHANGELOG.md
**Categoría:** Git  -  Herramientas
**Definición:** Archivo que registra cronológicamente todos los cambios notables de un proyecto, generalmente organizados por versión. Puede generarse automáticamente a partir de commits que siguen Conventional Commits usando herramientas como `conventional-changelog` o `semantic-release`. Su existencia y mantenimiento es señal de un proceso de desarrollo maduro.
**En el proyecto:** Generado automáticamente cuando todos los commits siguen el formato `type(scope): descripción`. Un `feat(contacts): importación CSV` aparece en el CHANGELOG como nueva funcionalidad de la versión siguiente.

---

### CHANGELOG automation (conventional-changelog)
**Categoría:** Herramientas  -  CI/CD
**Definición:** Herramienta que parsea el historial de commits con formato Conventional Commits y genera automáticamente el `CHANGELOG.md`. Agrupa los commits por tipo (`feat`, `fix`, `refactor`) y por versión SemVer. Elimina el trabajo manual de documentar qué cambió en cada release.
**En el proyecto:** Integrado en el pipeline de CI/CD del sistema de mensajería para generar el CHANGELOG automáticamente al crear un tag de versión.

---

### Captive Dependency
**Categoría:** .NET  -  DI  -  Anti-patrón
Error de configuración de DI donde un servicio de lifetime largo (Singleton) inyecta un servicio de lifetime corto (Scoped o Transient). El Singleton "captura" la instancia Scoped del primer request y la reutiliza en todos los siguientes, causando bugs de estado compartido entre usuarios o tenants. El runtime de ASP.NET Core lo detecta y lanza `InvalidOperationException` si `ValidateScopes = true`.

**En el proyecto:** La regla del proyecto es activar `ValidateScopes = true` y `ValidateOnBuild = true` en el entorno de desarrollo. Ningún Singleton puede inyectar `AppDbContext`, `ICurrentUserService` ni `ICurrentTenantService`.

---

### Chain of Responsibility
**Categoría:** Patrón de diseño  -  Comportamental (preview S05)
**Definición:** Patrón que pasa una solicitud a través de una cadena de handlers, donde cada uno decide si la procesa o la pasa al siguiente. Desacopla al emisor de la solicitud de sus receptores. En .NET, el pipeline de MediatR behaviors implementa este patrón: cada behavior puede actuar antes y después de pasar el control al siguiente.
**En el proyecto:** Preview para S05. El `InteractorPipeline` es el primer behavior en la cadena; en S05 se añadirán `ValidationBehavior` y otros, formando una cadena completa de responsabilidades para cada Request.

---

### Clean Architecture
**Categoría:** Arquitectura  -  Diseño de software
**Definición:** Estilo arquitectónico propuesto por Robert C. Martin que organiza el código en capas concéntricas con regla de dependencia estricta: las capas internas no conocen a las externas. Las capas típicas son Domain (entidades), Application (casos de uso), Infrastructure (persistencia, servicios externos) y Presentation (API, UI). Cada módulo del CRM tiene su propia mini Clean Architecture.
**En el proyecto:** Cada módulo sigue esta estructura: `Contacts.Domain` -> `Contacts.Application` -> `Contacts.Infrastructure` + `Contacts.Presentation`. El Domain no referencia EF Core ni ASP.NET; la Infrastructure implementa las interfaces que el Domain define.

---

### Clean Code
**Categoría:** Ingeniería de software  -  Principios
**Definición:** Conjunto de prácticas de legibilidad y mantenibilidad del código descritas por Robert C. Martin. Incluye naming expresivo, funciones pequeñas con responsabilidad única, ausencia de comentarios redundantes (el código se auto-documenta), manejo explícito de errores y ausencia de código muerto. Es una práctica continua, no un estado final.
**En el proyecto:** Las convenciones del back-template siguen Clean Code: nombres de clases como `GetUserProfileHandler` y `CreateConversationRequest` expresan su propósito sin ambigüedad. Se aborda en profundidad en S05.

---

### Cohesión (Cohesion)
**Categoría:** Ingeniería de software  -  Principios
**Definición:** Grado en que los elementos de un módulo o clase están relacionados entre sí y trabajan hacia un único propósito. Alta cohesión significa que una clase hace una sola cosa bien definida. Es el complemento del acoplamiento: se busca alta cohesión y bajo acoplamiento. Las clases con baja cohesión mezclan responsabilidades y son difíciles de mantener.
**En el proyecto:** `GetUserProfileHandler` tiene alta cohesión: solo orquesta la obtención de un perfil de usuario. Si también gestionara la sesión, el JWT y el log de auditoría, su cohesión sería baja y su mantenimiento se complicaría.

---

### Command (Conventional Commits tipo)
**Categoría:** Git  -  Conventional Commits
**Definición:** En el contexto de Conventional Commits, `feat` y `fix` son los tipos más comunes. No confundir con el patrón de diseño Command. En Conventional Commits, el `type` indica la naturaleza del cambio y determina qué componente del número de versión SemVer se incrementa al hacer un release.
**En el proyecto:** `feat(contacts): agregar importación CSV` bumpeará el MINOR; `fix(auth): corregir validación de token expirado` bumpeará el PATCH.

---

### Composite
**Categoría:** Patrón de diseño  -  Estructural
**Definición:** Patrón que permite tratar objetos individuales y composiciones de objetos de manera uniforme mediante una interfaz común. Define una jerarquía de clases donde los "leaves" (hojas) son objetos simples y los "composites" son contenedores que pueden tener hijos. El cliente trata a ambos de la misma manera.
**En el proyecto:** `GetUserProfileResponse` (abstract record) es el Component raíz. `GetUserProfileSuccess` y `GetUserProfileNotFoundFailure` son los Leaves. El Presenter los maneja con el mismo método `Handle(GetUserProfileResponse notification, ...)` sin importar cuál variante llegue.

---

### Complejidad Ciclomática (Cyclomatic Complexity)
**Categoría:** Ingeniería de software  -  Métricas
**Definición:** Métrica que mide el número de caminos linealmente independientes a través del código fuente. Cada `if`, `else`, `for`, `while`, `case` aumenta la complejidad ciclomática. Un valor alto indica código difícil de probar y mantener. Ideal: mantenerla por debajo de 10 por método.
**En el proyecto:** Mencionado como ejemplo de traducción de vocabulario técnico: lo que para el dev es "complejidad ciclomática alta", para el Product Owner es "cada nueva feature tarda el doble de lo que debería". Relevante al revisar los Handlers del CRM.

---

### Conventional Commits
**Categoría:** Git  -  Estándar
**Definición:** Especificación para mensajes de commit con formato estructurado: `type(scope): descripción breve`. El `type` describe la naturaleza del cambio, el `scope` el módulo afectado y la descripción el qué en imperativo. Permite generar CHANGELOGs automáticos, determinar bumps de versión SemVer y filtrar el historial por tipo de cambio.
**En el proyecto:** Obligatorio en el repo del sistema de mensajería desde S04. Ejemplos: `feat(conversations): agregar filtro por estado`, `fix(auth): corregir validación de refresh token`, `refactor(contacts): extraer lógica de importación a servicio`.

---

### Sistema de mensajería
**Categoría:** Producto  -  la empresa
**Definición:** Sistema CRM conversacional multi-canal (WhatsApp, Instagram, Email) que es el producto entregable del curso. Implementado como Modular Monolith con .NET 10 + EF Core 10 en el back-end y React 19 + React Flow en el front-end. Cada sesión construye un módulo real del producto.
**En el proyecto:** El producto en sí. Las entidades centrales son `UserProfile`, `Contact`, `Conversation`, `Agent`, `Channel`, `Message` y `BotFlow`.

---

### CTO / Arquitecto (stakeholder)
**Categoría:** Gestión de proyectos  -  Stakeholders
**Definición:** Stakeholder técnico de alto nivel interesado en las decisiones arquitectónicas, la deuda técnica, la escalabilidad y los riesgos técnicos. Se comunica mejor con ADRs, métricas de calidad y análisis de riesgos, en lugar de historias de usuario o wireframes. Toma decisiones sobre el stack tecnológico, patrones arquitectónicos y estándares del equipo.
**En el proyecto:** En la empresa, quien debe recibir los ADRs, la documentación de arquitectura y los informes de deuda técnica del sistema de mensajería.

---

## D

### Dapper
**Categoría:** .NET  -  ORM ligero
**Definición:** Micro-ORM para .NET que ejecuta SQL explícito y mapea los resultados a objetos C#. Es más rápido que EF Core en consultas de lectura complejas porque no usa Change Tracker ni traduce LINQ. Se usa como alternativa a EF Core para queries de alta performance o consultas con JOINs complejos que LINQ expresaría torpemente.
**En el proyecto:** `DapperUserProfileRepository` es la implementación alternativa de `IUserProfileRepository` para escenarios de alta performance. Ilustra el patrón Bridge: el Handler no cambia si se sustituye EF Core por Dapper.

---

### DbContext
**Categoría:** EF Core  -  Clase base
**Definición:** Clase base de Entity Framework Core de la que hereda `AppDbContext`. Gestiona la conexión a la base de datos, el seguimiento de cambios (Change Tracker), las transacciones implícitas y la materialización de consultas LINQ a SQL. Es Scoped por diseño porque mantiene estado por request HTTP.
**En el proyecto:** `AppDbContext : DbContext` con `DbSet<UserProfile>`, `DbSet<Contact>`, `DbSet<Conversation>`, etc. Nunca se registra como Singleton porque no es thread-safe y causaría memory leaks.

---

### DbSet\<T\>
**Categoría:** EF Core  -  API
**Definición:** Propiedad de `DbContext` que representa una tabla en la base de datos. Permite realizar consultas LINQ, agregar, actualizar y eliminar entidades. EF Core traduce las operaciones LINQ sobre `DbSet<T>` a SQL en tiempo de ejecución.
**En el proyecto:** `_db.UserProfiles`, `_db.Contacts`, `_db.Conversations` son los DbSets usados en los repositorios. El Adapter (`UserProfileRepository`) usa `_db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(...)`.

---

### Decorator
**Categoría:** Patrón de diseño  -  Estructural
**Definición:** Patrón que agrega responsabilidades a un objeto de manera dinámica, sin modificar la clase original y sin herencia. El Decorator implementa la misma interfaz que el objeto decorado, lo envuelve y ejecuta comportamiento adicional antes y/o después de delegar al objeto original. Permite componer comportamientos de forma flexible.
**En el proyecto:** `InteractorPipeline<TRequest, TResponse>` implementa `IPipelineBehavior<TRequest, TResponse>` y decora al Handler real: ejecuta logging antes de llamar a `next()` (el Handler), luego logging y `Publish` después. El Handler no sabe que está siendo decorado, respetando SRP.

---

### Deuda Técnica (Technical Debt)
**Categoría:** Ingeniería de software  -  Gestión
**Definición:** Costo implícito del trabajo adicional causado por elegir soluciones rápidas o subóptimas en lugar de las mejores a largo plazo. Como la deuda financiera, genera "intereses": cada nueva feature sobre código con deuda técnica cuesta más. La deuda puede ser intencional (decisión consciente con plan de pago) o accidental (resultado de falta de conocimiento o tiempo).
**En el proyecto:** Traducción para el PO: "complejidad ciclomática alta = cada nueva feature tarda el doble". Identificar y comunicar la deuda técnica del sistema de mensajería forma parte del rol del Tech Lead.

---

### Dependency Injection (DI)
**Categoría:** C#  -  Patrón, ASP.NET Core
**Definición:** Técnica donde un objeto recibe sus dependencias del exterior (inyectadas) en lugar de crearlas internamente. ASP.NET Core incluye un contenedor de DI integrado que gestiona la creación y el ciclo de vida de los objetos. Elimina el acoplamiento directo, facilita el testing con mocks y aplica el principio de inversión de dependencias (DIP).
**En el proyecto:** El back-template usa DI en toda su arquitectura. Los Handlers reciben sus repositorios por constructor: `public GetUserProfileHandler(IUserProfileRepository profiles) => _profiles = profiles;`. El contenedor resuelve `IUserProfileRepository` a `UserProfileRepository` (o `InMemoryUserProfileRepository` en tests).

---

### Design Pattern (Patrón de diseño)
**Categoría:** Ingeniería de software  -  Fundamentos
**Definición:** Solución reutilizable y probada a un problema recurrente en un contexto determinado. No es código copiable sino una plantilla de solución. Los patrones tienen nombre (vocabulario común), intención (el problema que resuelven), estructura (participantes y relaciones) y consecuencias (trade-offs). El catálogo GoF define 23 patrones clásicos divididos en Creacionales, Estructurales y de Comportamiento.
**En el proyecto:** S04 cubre los 12 patrones GoF presentes en el back-template (5 creacionales + 7 estructurales). Identificarlos en código existente es más valioso que aprenderlos en abstracto.

---

### Dev Experience (DX)
**Categoría:** Ingeniería de software  -  Cultura de equipo
**Definición:** Calidad de la experiencia que tienen los desarrolladores al trabajar con un codebase, herramientas o plataforma. Incluye: tiempo de compilación, calidad de la documentación, facilidad de configuración del entorno, claridad de los errores y autonomía del equipo. Es el equivalente del UX para desarrolladores y afecta directamente la velocidad y calidad del trabajo.
**En el proyecto:** Stakeholder "Dev Team" le importa especialmente: deuda técnica, herramientas como el back-template bien documentado, y retrospectivas para mejorar el proceso del sistema de mensajería.

---

### DI Lifetime (ciclo de vida de DI)
**Categoría:** ASP.NET Core  -  Dependency Injection
**Definición:** Configuración que determina cuándo se crea y destruye una instancia de un servicio en el contenedor de DI. ASP.NET Core ofrece tres lifetimes: Singleton (una instancia para toda la aplicación), Scoped (una instancia por request HTTP) y Transient (una instancia por cada vez que se resuelve). Mezclar lifetimes incorrectamente causa bugs difíciles de rastrear.
**En el proyecto:** `ITenantContextAccessor` -> Singleton; `AppDbContext` -> Scoped; `IValidator<T>` -> Transient. El error más peligroso: inyectar un servicio Scoped dentro de un Singleton (Captive Dependency).

---

### DIP (Dependency Inversion Principle)
**Categoría:** SOLID  -  Principios
**Definición:** Quinto principio de SOLID. Establece que los módulos de alto nivel no deben depender de módulos de bajo nivel; ambos deben depender de abstracciones. Las abstracciones no deben depender de los detalles; los detalles deben depender de las abstracciones. Se implementa definiendo interfaces en la capa de alto nivel que la capa de bajo nivel implementa.
**En el proyecto:** `IUserProfileRepository` se define en `Users.Domain` (alto nivel). `UserProfileRepository` en `Users.Infrastructure` (bajo nivel) implementa esa interfaz. El Handler depende de la abstracción, no de EF Core directamente.

---

### Docker (multi-stage)
**Categoría:** Contenedores  -  Dockerfile
**Definición:** Técnica de Dockerfile que usa múltiples etapas (`FROM ... AS build`, `FROM ... AS runtime`) para separar el entorno de compilación del entorno de ejecución. El resultado es una imagen de producción más pequeña y segura que no incluye el SDK ni las herramientas de compilación, solo el artefacto final y el runtime.
**En el proyecto:** El Dockerfile del back-end del sistema de mensajería usará multi-stage: etapa `build` con `mcr.microsoft.com/dotnet/sdk:10.0` y etapa `runtime` con `mcr.microsoft.com/dotnet/aspnet:10.0`. Se abordará en detalle más adelante.

---

### Domain Layer (capa de dominio)
**Categoría:** Arquitectura  -  Clean Architecture
**Definición:** Capa más interna de Clean Architecture. Contiene las entidades de negocio, las interfaces de repositorios y las reglas de dominio. No tiene dependencias de ninguna otra capa ni de frameworks externos (ni EF Core, ni ASP.NET). Es el núcleo que define el lenguaje ubicuo del negocio.
**En el proyecto:** `Users.Domain/Entities/UserProfile.cs` y `Users.Domain/Repositories/IUserProfileRepository.cs`. El Domain no sabe que existe EF Core, solo define qué operaciones necesita sobre `UserProfile`.

---

## E

### EF Core (Entity Framework Core)
**Categoría:** .NET  -  ORM
**Definición:** ORM (Object-Relational Mapper) oficial de Microsoft para .NET. Traduce operaciones C# (LINQ, DbSet) a SQL. Gestiona el ciclo de vida de las entidades, el tracking de cambios, las migraciones de base de datos y las transacciones. La versión 10 trae mejoras de performance y nuevas funcionalidades para consultas complejas.
**En el proyecto:** ORM principal del sistema de mensajería. `AppDbContext` es la clase central. Todos los repositorios de producción usan EF Core internamente. Se profundiza más adelante.

---

### Entidad de dominio (Domain Entity)
**Categoría:** DDD  -  Dominio
**Definición:** Objeto del dominio con identidad única que persiste en el tiempo. Su identidad no cambia aunque cambien sus atributos. Se distingue de los Value Objects (que se identifican por sus valores). En .NET, las entidades del dominio son clases POCO sin dependencias de frameworks.
**En el proyecto:** `UserProfile`, `Contact`, `Conversation`, `Agent`, `Channel`, `Message` son las entidades principales. Cada una tiene un `Id` (long, clave primaria) y un `PublicId` (Guid, expuesto externamente).

---

### Estimación (Story Points)
**Categoría:** Gestión de proyectos  -  Metodologías ágiles
**Definición:** Unidad relativa de medida de la complejidad de una historia de usuario. No representa horas sino dificultad comparativa respecto a otras historias del mismo equipo. Permite al equipo estimar velocidad (cuántos puntos por sprint) sin comprometerse a tiempos absolutos inciertos.
**En el proyecto:** Escala: 1-2 (trivial), 3-5 (pequeño/medio), 8 (complejo, como integración WhatsApp), 13 (muy complejo, como billing completo), 21 (demasiado grande: dividir). Se usa en la planificación de los módulos del CRM.

---

## F

### Facade
**Categoría:** Patrón de diseño  -  Estructural
**Definición:** Patrón que proporciona una interfaz simplificada a un sistema complejo de clases, una biblioteca o un framework. No elimina la complejidad subyacente, la oculta detrás de una API simple. El Facade coordina múltiples componentes internos y expone solo lo que el cliente necesita.
**En el proyecto:** `Mediator.Send(new GetUserProfileRequest(id, CurrentTenantId))` en el Controller es la Facade. Esa única línea oculta: la pipeline de MediatR, el `InteractorPipeline` (Decorator), el Handler, el repositorio, EF Core, el Presenter y el ViewModel. El Controller solo ve el resultado final.

---

### Factory Method
**Categoría:** Patrón de diseño  -  Creacional
**Definición:** Patrón que define una interfaz para crear un objeto, pero deja que las subclases (o el método en sí) decidan qué clase concreta instanciar. El creador (factory) produce un objeto cuyo tipo exacto depende de una condición evaluada en tiempo de ejecución. Desacopla al cliente del tipo concreto del objeto creado.
**En el proyecto:** Ningún componente del back-template es un Factory Method estricto. El Handler decide con un condicional qué Response construir (`new GetUserProfileSuccess(...)` o `new GetUserProfileNotFoundFailure(...)`); eso es creación condicional sobre una jerarquía de Responses, no el patrón GoF, que exige un método de creación que las subclases sobrescriben. El ejemplo del patrón son las katas (`NotificacionFactory`).

---

### Fluent Interface
**Categoría:** C#  -  API Design
**Definición:** Estilo de API donde cada método retorna el objeto mismo (o el builder) para permitir encadenar llamadas en una sola expresión. Es la base del patrón Builder cuando se aplica en .NET. Hace el código más legible al expresar una secuencia de pasos como una oración continua.
**En el proyecto:** `builder.Services.AddOptions<JwtSettings>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()` es una fluent interface. Cada método retorna `OptionsBuilder<JwtSettings>`, permitiendo encadenar el siguiente paso.

---

### Flyweight
**Categoría:** Patrón de diseño  -  Estructural
**Definición:** Patrón que comparte objetos para soportar eficientemente un gran número de instancias similares. Separa el estado intrínseco (compartido, inmutable) del extrínseco (único por instancia). Reduce el consumo de memoria cuando muchos objetos comparten el mismo estado base.
**En el proyecto:** Tres ejemplos en .NET: (1) string interning  -  literales iguales comparten la misma referencia en memoria; (2) `AgentRole` enum  -  cada valor es un singleton; (3) `IOptions<JwtSettings>` registrado Singleton  -  toda la app comparte un solo objeto de configuración.

---

### Footer (Conventional Commits)
**Categoría:** Git  -  Conventional Commits
**Definición:** Sección opcional al final del mensaje de commit (después de una línea en blanco) que indica información estructurada como breaking changes (`BREAKING CHANGE: ...`) o referencias a issues (`Closes #123`). Un `BREAKING CHANGE` en el footer dispara automáticamente un bump MAJOR en SemVer.
**En el proyecto:** `BREAKING CHANGE: cambio en la firma del endpoint /api/contacts` en el footer indica que los clientes del API deben actualizar su integración.

---

## G

### Gang of Four (GoF)
**Categoría:** Ingeniería de software  -  Historia
**Definición:** Referencia al libro "Design Patterns: Elements of Reusable Object-Oriented Software" (1994) escrito por Erich Gamma, Richard Helm, Ralph Johnson y John Vlissides. Documentó 23 patrones de diseño organizados en Creacionales, Estructurales y de Comportamiento. Es la obra de referencia fundamental para patrones de diseño en orientación a objetos.
**En el proyecto:** Los 12 patrones de S04 (5 creacionales + 7 estructurales) pertenecen al catálogo GoF. S05 completará los patrones de comportamiento del mismo catálogo.

---

### git bisect
**Categoría:** Git  -  Comandos
**Definición:** Comando de Git que realiza una búsqueda binaria en el historial de commits para encontrar el commit exacto que introdujo un bug. Requiere indicar un commit "bueno" (anterior al bug) y uno "malo" (con el bug). Git va dividiendo el rango a la mitad, preguntando si cada commit es bueno o malo, hasta identificar el commit culpable.
**En el proyecto:** Funciona correctamente solo cuando los commits son atómicos. Si un commit mezcla una feature y un fix, `git bisect` puede identificarlo pero el reversal será complejo.

---

### git revert
**Categoría:** Git  -  Comandos
**Definición:** Comando de Git que crea un nuevo commit que deshace los cambios introducidos por un commit anterior. A diferencia de `git reset`, no reescribe el historial: es una operación segura para ramas compartidas. El reversal limpio es posible solo cuando el commit objetivo es atómico.
**En el proyecto:** Si `feat(contacts): agregar importación CSV` introduce un bug en producción, `git revert <hash>` crea un commit de reversal limpio sin afectar otros cambios en el historial.

---

### Global Query Filter (EF Core)
**Categoría:** EF Core  -  Multi-tenancy
**Definición:** Filtro configurado a nivel de `DbContext` que se aplica automáticamente a todas las consultas de un tipo de entidad. Se usa principalmente para soft delete (`IsActive == true`) y multi-tenancy (`TenantId == currentTenantId`). Elimina la necesidad de repetir la condición en cada repositorio.
**En el proyecto:** Configurado en `AppDbContext` para que todas las consultas de `Contact`, `Conversation` y `UserProfile` filtren automáticamente por el `TenantId` del request actual. Se profundiza más adelante (Multi-tenant).

---

## H

### Handler
**Categoría:** Back-template  -  Patrón de aplicación
**Definición:** Clase que implementa `IRequestHandler<TRequest, TResponse>` and contiene la lógica de un caso de uso específico. Recibe un Request, ejecuta la operación (consultando repositorios o modificando el dominio) y retorna una Response. El Handler es el "corazón" del caso de uso y debe tener alta cohesión: solo orquesta, no contiene lógica de dominio compleja.
**En el proyecto:** `GetUserProfileHandler`, `CreateContactHandler`, `SendMessageHandler`, etc. Cada use case tiene exactamente un Handler. El Handler decide con un condicional qué tipo de Response retornar (creación condicional sobre una jerarquía; no es el patrón Factory Method de GoF).

---

### Homework (tarea)
**Categoría:** Capacitación
**Definición:** Asignación para completar entre sesiones, consistente en lecturas de dev-notes (1-2 horas) y un ejercicio de código (2 horas). El ejercicio siempre usa entidades reales del sistema de mensajería y tiene criterios de aceptación explícitos. Se revisa en el bloque inicial del siguiente sábado.
**En el proyecto:** S04: implementar `IRepositoryFactory` con `EfCoreRepositoryFactory` e `InMemoryRepositoryFactory`, y usar el operador `with {}` en tests de `GetUserProfileHandler`. Detalle completo en `S04/homework.md`.

---

## I

### IFailure
**Categoría:** Back-template  -  Interfaces
**Definición:** Interfaz marcadora del back-template que identifica una Response como caso de fallo. Las implementaciones específicas son `INotFoundFailure`, `IValidationFailure`, `IConflictFailure`, etc. El Presenter verifica primero si la Response implementa `IFailure` antes de verificar `ISuccess<T>`, garantizando que los errores se manejen correctamente sin importar cuántas variantes de fallo existan.
**En el proyecto:** Regla universal: en todos los Presenters del CRM, el bloque `if (notification is IFailure failure)` va siempre primero. `GetUserProfileNotFoundFailure : GetUserProfileResponse, INotFoundFailure` donde `INotFoundFailure : IFailure`.

---

### INotFoundFailure
**Categoría:** Back-template  -  Interfaces
**Definición:** Interfaz especializada que hereda de `IFailure`. Indica que el recurso solicitado no existe. El Presenter la usa para determinar que el Controller debe retornar HTTP 404. Es una de las variantes de fallo del patrón Composite de Responses.
**En el proyecto:** `GetUserProfileNotFoundFailure : GetUserProfileResponse, INotFoundFailure`. En el Controller: `return result is GetUserProfileNotFoundFailure ? NotFound(_viewModel) : StatusCode(500, _viewModel)`.

---

### InMemory Repository
**Categoría:** Testing  -  Patrones
**Definición:** Implementación de un repositorio que almacena datos en memoria (típicamente en un `Dictionary<TKey, TEntity>`) en lugar de una base de datos real. Se usa en tests unitarios para aislar la lógica del Handler sin infraestructura. Es más rápido y predecible que una base de datos real en tests.
**En el proyecto:** `InMemoryUserProfileRepository`, `InMemoryContactRepository`, `InMemoryConversationRepository` implementan las interfaces de dominio para uso en tests. La `InMemoryRepositoryFactory` crea toda la familia a la vez.

---

### Infrastructure Layer (capa de infraestructura)
**Categoría:** Arquitectura  -  Clean Architecture
**Definición:** Capa que implementa las interfaces definidas en el Domain y la Application, usando frameworks y tecnologías concretas (EF Core, Redis, SendGrid, etc.). Conoce la base de datos, los servicios externos y los detalles de implementación. No debe ser referenciada por el Domain ni la Application (las referencias van al revés).
**En el proyecto:** `Users.Infrastructure/Repositories/UserProfileRepository.cs` es el ejemplo principal. Implementa `IUserProfileRepository` usando EF Core. También incluiría implementaciones de servicios de email, push notifications, etc.

---

### Interface (interfaz en C#)
**Categoría:** C#  -  POO
**Definición:** Contrato que define qué métodos y propiedades debe implementar una clase, sin especificar cómo. Permite el polimorfismo sin herencia. En C# 8+, las interfaces pueden tener implementaciones por defecto, aunque se usa con moderación. Son la herramienta principal para lograr DIP e inversión de control.
**En el proyecto:** `IUserProfileRepository`, `IContactRepository`, `IConversationRepository` son las interfaces de repositorios definidas en el Domain. Permiten que los Handlers dependan de abstracciones y no de EF Core directamente.

---

### Interface Segregation Principle (ISP)
**Categoría:** SOLID  -  Principios
**Definición:** Cuarto principio de SOLID. Establece que los clientes no deben verse obligados a depender de interfaces que no usan. Es mejor tener muchas interfaces pequeñas y específicas que una interfaz grande y general. Evita que las implementaciones deban proporcionar métodos vacíos o que lancen `NotImplementedException`.
**En el proyecto:** En lugar de una `ICrudRepository<T>` con todos los métodos CRUD, el CRM usa `IUserProfileRepository` con solo los métodos que el dominio necesita: `GetByPublicIdAsync`, `InsertAsync`, `UpdateAsync`, `DisableAsync`.

---

### InteractorPipeline
**Categoría:** Back-template  -  Pipeline
**Definición:** Behavior de MediatR del back-template que implementa el patrón Decorator sobre los Handlers. Ejecuta logging del Request antes de invocar al Handler (`next()`), y logging del Response + `Publish` de la Response después. Garantiza que todas las operaciones del sistema sean trazables sin que cada Handler implemente logging individualmente.
**En el proyecto:** `InteractorPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>`. Se registra en el contenedor de DI para ejecutarse automáticamente en cada llamada a `Mediator.Send(...)`.

---

### IOptions\<T\>
**Categoría:** ASP.NET Core  -  Options Pattern
**Definición:** Interfaz del Options Pattern de ASP.NET Core para inyectar configuración tipada. `IOptions<T>` retorna siempre la misma instancia (Singleton). Hay variantes: `IOptionsSnapshot<T>` (Scoped, recarga por request) y `IOptionsMonitor<T>` (Singleton con recarga en vivo). Permite separar la configuración de la lógica de negocio.
**En el proyecto:** `IOptions<JwtSettings>` inyectado en los componentes de autenticación del CRM. `JwtSettings` se configura con `.AddOptions<JwtSettings>().BindConfiguration(JwtSettings.SectionName).ValidateDataAnnotations().ValidateOnStart()`.

---

### IRequest\<TResponse\>
**Categoría:** Back-template  -  MediatR
**Definición:** Interfaz de MediatR que marca un objeto como solicitud (Request) que produce una respuesta de tipo `TResponse`. Implementada por todas las clases Request del sistema. MediatR usa esta interfaz para enrutar el Request al Handler correspondiente y ejecutar el pipeline de behaviors.
**En el proyecto:** `GetUserProfileRequest : IRequest<GetUserProfileResponse>`, `CreateContactRequest : IRequest<CreateContactResponse>`. Cada use case tiene exactamente un Request y un Handler.

---

### IRequestHandler\<TRequest, TResponse\>
**Categoría:** Back-template  -  MediatR
**Definición:** Interfaz de MediatR que implementan los Handlers. Define el método `Handle(TRequest request, CancellationToken cancellationToken)` que contiene la lógica del caso de uso. MediatR resuelve el Handler correcto en tiempo de ejecución basándose en el tipo del Request.
**En el proyecto:** `GetUserProfileHandler : IRequestHandler<GetUserProfileRequest, GetUserProfileResponse>`. Registrado automáticamente por MediatR al configurar el ensamblado de Application.

---

### IResponse
**Categoría:** Back-template  -  Interfaces
**Definición:** Interfaz marcadora base del back-template que implementan todas las Response abstratas de los use cases. Permite que el `InteractorPipeline` y otros components traten cualquier tipo de Response de manera uniforme sin conocer el tipo concreto. Es la raíz del árbol de tipos de Response.
**En el proyecto:** `public abstract record GetUserProfileResponse : IResponse`. Todos los abstract records de Response implementan `IResponse`.

---

### IServiceScopeFactory
**Categoría:** .NET  -  DI  -  Background Services
Interfaz que permite crear scopes de DI manualmente dentro de un servicio Singleton o BackgroundService. Necesaria cuando un `IHostedService` (Singleton por naturaleza) necesita usar servicios Scoped como `AppDbContext`. El scope creado debe usarse dentro de un `using` para que los Scoped se disponsen correctamente al terminar cada ciclo.

**En el proyecto:** Todos los `BackgroundService` del CRM (OutboxPublisherWorker, CampaignWorker) inyectan `IServiceScopeFactory` en lugar de inyectar `AppDbContext` directamente, para evitar la Captive Dependency.

---

### ISuccess\<TData\>
**Categoría:** Back-template  -  Interfaces
**Definición:** Interfaz genérica del back-template que identifica una Response como caso de éxito con datos. `TData` es el tipo del DTO que contiene los datos del resultado exitoso. El Presenter verifica esta interfaz para extraer los datos y poblar el ViewModel.
**En el proyecto:** `GetUserProfileSuccess : GetUserProfileResponse, ISuccess<UserProfileDto>`. El Presenter hace `if (notification is ISuccess<UserProfileDto> success) _viewModel.Set(success)`.

---

## J

### JwtSettings
**Categoría:** Back-template  -  Configuración
**Definición:** Clase de configuración tipada del back-template que contiene los parámetros del JWT (clave secreta, emisor, audiencia, tiempo de expiración). Se configura mediante el Options Pattern con validación en startup para detectar configuración incorrecta antes de que llegue el primer request.
**En el proyecto:** Ejemplo de Builder: `builder.Services.AddOptions<JwtSettings>().BindConfiguration(JwtSettings.SectionName).ValidateDataAnnotations().ValidateOnStart()`. También ejemplo de Flyweight: un único objeto en memoria compartido por todos los componentes de autenticación.

---

## L

### Leaf (Composite)
**Categoría:** Patrón de diseño  -  Composite (rol)
**Definición:** En el patrón Composite, un Leaf es un objeto sin hijos que implementa la interfaz Component. Realiza trabajo real cuando se le invoca. Los Leaves son los casos concretos que el sistema maneja: en el contexto del back-template, son las variantes específicas de una Response.
**En el proyecto:** `GetUserProfileSuccess` y `GetUserProfileNotFoundFailure` son los Leaves de la jerarquía `GetUserProfileResponse`. El Presenter los discrimina con pattern matching.

---

### Lenguaje Ubicuo (Ubiquitous Language)
**Categoría:** DDD  -  Principios
**Definición:** Vocabulario común compartido entre el equipo técnico y el negocio para describir el dominio. Todos los términos del lenguaje ubicuo deben reflejarse en el código: nombres de clases, métodos y variables. Elimina la traducción entre lo que el negocio dice y lo que el código representa.
**En el proyecto:** `Conversation`, `Contact`, `Agent`, `Channel`, `BotFlow` son términos del lenguaje ubicuo del sistema de mensajería. El código usa exactamente estos nombres, no sinónimos como `Chat`, `Client`, `Employee` o `Integration`.

---

## M

### Management (stakeholder)
**Categoría:** Gestión de proyectos  -  Stakeholders
**Definición:** Stakeholder de nivel ejecutivo interesado en fechas de entrega, costos del proyecto y riesgos de negocio. Se comunica mejor con milestones, burn rate, porcentajes de avance y riesgos con sus mitigaciones, no con detalles técnicos. Necesita información que le permita tomar decisiones de negocio.
**En el proyecto:** En la empresa, quienes deben recibir reportes de avance de los módulos del sistema de mensajería con lenguaje de negocio, no de ingeniería.

---

### MediatR
**Categoría:** .NET  -  Librería
**Definición:** Librería de .NET que implementa el patrón Mediator. Desacopla los emisores de solicitudes (Controllers) de los procesadores (Handlers) a través de un bus de mensajes en proceso. Soporta Request/Response (síncrono conceptualmente) y Notification/Publish (para múltiples handlers). También soporta Pipeline Behaviors para cross-cutting concerns.
**En el proyecto:** La columna vertebral del back-template. `Mediator.Send(request)` en el Controller despacha al Handler correcto. `_mediator.Publish(response)` en el `InteractorPipeline` notifica al Presenter.

---

### Mediator (patrón)
**Categoría:** Patrón de diseño  -  Comportamental (preview S05)
**Definición:** Patrón que define un objeto mediador que encapsula cómo interactúan un conjunto de objetos. Los objetos no se comunican directamente entre sí, sino a través del mediador. Reduce las dependencias directas entre componentes, centralizando la comunicación.
**En el proyecto:** Preview para S05. MediatR implementa este patrón: el Controller no conoce al Handler, el Handler no conoce al Presenter. Todos se comunican a través de MediatR.

---

### Milestone
**Categoría:** Gestión de proyectos  -  Planificación
**Definición:** Punto de control en el cronograma del proyecto que marca la finalización de una fase o entregable significativo. Los milestones son binarios (se alcanzaron o no) y tienen fecha objetivo. Se usan para comunicar el avance a stakeholders de management sin entrar en detalles de implementación.
**En el proyecto:** Los módulos principales del sistema de mensajería (Auth completo, Conversaciones funcionando, Front-end desplegado) son milestones de comunicación hacia management.

---

### Modular Monolith (Monolito Modular)
**Categoría:** Arquitectura  -  Patrones
**Definición:** Arquitectura donde la aplicación se despliega como un único proceso pero el código está organizado en módulos con límites explícitos, contratos definidos y mínimo acoplamiento entre ellos. Combina la simplicidad operativa del monolito con la organización del código de los microservicios. Permite migrar a microservicios en el futuro si es necesario.
**En el proyecto:** Arquitectura elegida para el sistema de mensajería (ADR-001). Módulos: `Users`, `Contacts`, `Conversations`, `Channels`, `Messages`, `BotFlows`. Cada módulo tiene sus propias capas Domain, Application, Infrastructure y Presentation.

---

## N

### Namespace
**Categoría:** C#  -  Organización del código
**Definición:** Mecanismo de C# para organizar clases en espacios de nombres jerárquicos, evitando conflictos de nombres y aportando estructura al proyecto. En el back-template, los namespaces reflejan la estructura de carpetas y la capa arquitectónica.
**En el proyecto:** Convenciones del back-template: `Users.Application.UseCases.GetUserProfile`, `Contacts.Infrastructure.Repositories`, `Conversations.Presentation.Controllers`. El namespace expresa la ubicación arquitectónica de cada clase.

---

### Naming Convention (convenciones de nomenclatura)
**Categoría:** C#  -  Clean Code
**Definición:** Reglas que gobiernan cómo se nombran clases, métodos, variables y otros elementos del código. En C#, las convenciones estándar son: PascalCase para clases y métodos, camelCase para variables locales y parámetros, prefijo `I` para interfaces, prefijo `_` para campos privados.
**En el proyecto:** El back-template sigue naming conventions estrictas: `GetUserProfileHandler`, `IUserProfileRepository`, `_profiles`, `GetUserProfileRequest`. La consistencia en el naming hace el código predecible y reducible.

---

## O

### Object Composition (composición de objetos)
**Categoría:** POO  -  Principios
**Definición:** Técnica de diseño donde un objeto se construye conteniendo referencias a otros objetos, en lugar de heredar de ellos. "Prefer composition over inheritance" es un principio fundamental del diseño orientado a objetos. La composición es más flexible que la herencia porque puede cambiarse en tiempo de ejecución.
**En el proyecto:** El Decorator (`InteractorPipeline`) compone al Handler en lugar de heredar de él. Los Repositories componen `AppDbContext` en lugar de heredar de él. Los Handlers componen sus repositorios inyectados.

---

### Observer (patrón)
**Categoría:** Patrón de diseño  -  Comportamental (preview S05)
**Definición:** Patrón que define una dependencia uno-a-muchos entre objetos, de modo que cuando uno cambia de estado, todos sus dependientes son notificados automáticamente. En MediatR, `INotificationHandler<T>` implementa este patrón: múltiples handlers pueden suscribirse a la misma notificación.
**En el proyecto:** Preview para S05. `_mediator.Publish(response)` en el `InteractorPipeline` notifica a todos los `INotificationHandler<TResponse>` registrados. El Presenter (`INotificationHandler<GetUserProfileResponse>`) es uno de los suscriptores.

---

### Open/Closed Principle (OCP)
**Categoría:** SOLID  -  Principios
**Definición:** Segundo principio de SOLID. Establece que las entidades de software (clases, módulos, funciones) deben estar abiertas para extensión pero cerradas para modificación. Al agregar un nuevo comportamiento, se agrega nuevo código en lugar de modificar el existente. Protege el código existente de regresiones.
**En el proyecto:** Agregar un nuevo tipo de fallo (`CreateConversationDuplicateFailure`) al Composite de Responses no requiere modificar el Handler ni el Presenter existentes. Se agrega un nuevo Leaf a la jerarquía. El Presenter existente lo maneja vía `IFailure`.

---

### Options Pattern
**Categoría:** ASP.NET Core  -  Configuración
**Definición:** Patrón de ASP.NET Core para inyectar configuración tipada y validada. La configuración se bindea a una clase POCO (`JwtSettings`, `DatabaseSettings`) desde `appsettings.json`. Soporta validación con Data Annotations y validación en startup para detectar errores de configuración antes de que llegue el primer request.
**En el proyecto:** `builder.Services.AddOptions<JwtSettings>().BindConfiguration(JwtSettings.SectionName).ValidateDataAnnotations().ValidateOnStart()`. Es el ejemplo de Builder en el back-template.

---

## P

### Pattern Matching (C#)
**Categoría:** C#  -  Características del lenguaje
**Definición:** Característica de C# que permite inspeccionar un valor y hacer branching basado en su tipo o estructura, usando el operador `is` o expresiones `switch`. Más expresivo y seguro que castings explícitos. En C# 10+, el pattern matching es muy poderoso y porta patrones de tipo, posicionales, de propiedad y combinados.
**En el proyecto:** El Presenter usa pattern matching: `if (notification is IFailure failure)` y `else if (notification is ISuccess<UserProfileDto> success)`. También en el Controller: `result is GetUserProfileNotFoundFailure`. El orden del matching importa: IFailure siempre primero.

---

### Planning Poker
**Categoría:** Gestión de proyectos  -  Metodologías ágiles
**Definición:** Técnica de estimación donde todos los miembros del equipo votan simultáneamente (levantando una carta con un número de Story Points) sin influenciarse mutuamente. Las divergencias en los votos revelan riesgos ocultos o comprensiones diferentes de la historia, no diferencias de habilidad. Es más valiosa la conversación que surge que la estimación final.
**En el proyecto:** Si el Tech Lead dice "8" y el dev junior dice "3", la diferencia puede revelar que el junior no consideró la complejidad de la integración con WhatsApp. La conversación resuelve el riesgo oculto.

---

### Pipeline (MediatR Pipeline)
**Categoría:** Back-template  -  Arquitectura
**Definición:** Cadena de behaviors que se ejecutan secuencialmente antes y después de invocar al Handler. Cada behavior implementa `IPipelineBehavior<TRequest, TResponse>` y recibe una función `next` que llama al siguiente elemento de la cadena. Es la implementación de Chain of Responsibility en el back-template.
**En el proyecto:** El pipeline actual: `InteractorPipeline` -> Handler. En una sesión posterior se agregará `ValidationBehavior` al pipeline. El orden de registro en DI determina el orden de ejecución.

---

### POCO (Plain Old CLR Object)
**Categoría:** C#  -  Patrones
**Definición:** Clase C# simple sin dependencias de frameworks o herencia de clases base específicas de framework. Los POCOs son clases que representan solo datos o lógica de dominio, sin contaminación de infraestructura. Las entidades de dominio del CRM son POCOs: no heredan de `Entity<T>` ni implementan interfaces de EF Core.
**En el proyecto:** `UserProfile`, `Contact`, `Conversation` son POCOs. La configuración de EF Core se hace en `EntityTypeConfiguration<T>` separada, no en la entidad, manteniendo el POCO limpio.

---

### Polimorfismo (Polymorphism)
**Categoría:** POO  -  Principios
**Definición:** Capacidad de un objeto de tomar muchas formas. En C#, se logra mediante herencia (override), interfaces (implementaciones múltiples) y generics. Permite escribir código que trabaja con la abstracción sin conocer el tipo concreto. Es la base de varios patrones GoF.
**En el proyecto:** El Presenter trabaja polimórficamente con `GetUserProfileResponse`: no sabe si es `GetUserProfileSuccess` o `GetUserProfileNotFoundFailure` hasta que evalúa el pattern matching. El Handler retorna polimórficamente uno u otro.

---

### Presenter
**Categoría:** Back-template  -  Patrón de presentación
**Definición:** Clase que implementa `INotificationHandler<TResponse>` y transforma la Response de dominio en datos listos para el HTTP layer. Recibe la notificación publicada por el `InteractorPipeline`, evalúa si es éxito o fallo mediante pattern matching, y puebla el `ResultViewModel<T>`. Desacopla el tipo de Response del código de status HTTP.
**En el proyecto:** `GetUserProfilePresenter : INotificationHandler<GetUserProfileResponse>`. Regla: `IFailure` siempre primero en el pattern matching. Cada use case tiene su propio Presenter.

---

### Product Owner (PO)
**Categoría:** Gestión de proyectos  -  Stakeholders
**Definición:** Stakeholder que representa los intereses del negocio y los usuarios finales. Prioriza el backlog, define las historias de usuario y acepta o rechaza el trabajo completado. Le interesa el impacto en el usuario y la velocidad del equipo, no los detalles técnicos. No puede negociar los Story Points, solo el scope.
**En el proyecto:** En la empresa, la persona que define qué módulos del sistema de mensajería se construyen en qué orden y acepta o rechaza el trabajo de cada sprint.

---

### Prototype
**Categoría:** Patrón de diseño  -  Creacional
**Definición:** Patrón que crea nuevos objetos copiando (clonando) una instancia existente, en lugar de crear desde cero. Es útil cuando la creación de un objeto es costosa o cuando se necesitan muchas variaciones de un objeto base. En C#, los `record` con el operador `with` son la implementación idiomática moderna del patrón Prototype.
**En el proyecto:** `var otroUsuario = baseRequest with { PublicId = Guid.NewGuid() }` en los tests de `GetUserProfileHandler`. Más limpio que repetir `new GetUserProfileRequest(...)` con todos los campos. Se aplica a todos los Request del CRM en los tests.

---

### Proxy
**Categoría:** Patrón de diseño  -  Estructural
**Definición:** Patrón que proporciona un sustituto o marcador de posición para otro objeto, controlando el acceso a él. El Proxy implementa la misma interfaz que el objeto real, interceptando las llamadas para agregar funcionalidad (control de acceso, lazy loading, logging, caching). A diferencia del Decorator, el Proxy típicamente controla el acceso más que agregar comportamiento.
**En el proyecto:** El ejemplo real de Proxy es el lazy loading de EF Core (proxies que cargan la navegación al acceder a ella) y la kata `ReportePesadoProxy`. `ResultViewModel<T>` no es un Proxy: no implementa la interfaz de la Response ni controla el acceso a ella; es un ViewModel de salida.

---

## R

### Record (C#)
**Categoría:** C#  -  Tipos de referencia
**Definición:** Tipo de referencia de C# 9+ diseñado para datos inmutables. Genera automáticamente igualdad por valor, `ToString()`, `GetHashCode()` y el operador `with` para copia no destructiva (Prototype). Los `sealed record` no pueden heredarse. Los `abstract record` son la base de jerarquías polimórficas.
**En el proyecto:** Los Requests son `sealed record`: `public sealed record GetUserProfileRequest(Guid PublicId, long TenantId) : IRequest<GetUserProfileResponse>`. Las Responses son `abstract record` (base) y `sealed record` (variantes). El operador `with` habilita el patrón Prototype en tests.

---

### Refactoring
**Categoría:** Ingeniería de software  -  Prácticas
**Definición:** Proceso de reestructurar el código existente sin cambiar su comportamiento observable externo. El objetivo es mejorar la legibilidad, reducir la complejidad y eliminar la deuda técnica. Un commit de tipo `refactor` en Conventional Commits indica exactamente esto: cambio interno sin cambio de comportamiento.
**En el proyecto:** `refactor(contacts): extraer lógica de importación a servicio` es un ejemplo válido. El comportamiento final es el mismo, pero el código está mejor organizado.

---

### Repository Pattern
**Categoría:** Patrón de diseño  -  Arquitectura
**Definición:** Patrón que abstrae el acceso a datos detrás de una interfaz de colección. El repositorio se comporta como una colección en memoria: permite agregar, buscar y eliminar objetos sin que el código consumidor sepa qué tecnología de persistencia se usa. Implementa los patrones Adapter y Bridge en el back-template.
**En el proyecto:** `IUserProfileRepository`, `IContactRepository`, `IConversationRepository` definen los contratos. Sus implementaciones (`UserProfileRepository`, etc.) adaptan EF Core a esos contratos. Permiten sustituir EF Core por Dapper o una implementación InMemory sin tocar los Handlers.

---

### Request
**Categoría:** Back-template  -  MediatR
**Definición:** Objeto que encapsula los parámetros de un caso de uso. Implementa `IRequest<TResponse>` y es el primer objeto que entra al pipeline de MediatR. Debe ser inmutable (implementado como `record` en el back-template). MediatR lo enruta al Handler correspondiente basándose en su tipo.
**En el proyecto:** `GetUserProfileRequest(Guid PublicId, long TenantId)`, `CreateContactRequest(...)`, `SendMessageRequest(...)`. El scope siempre incluye el `TenantId` para garantizar el aislamiento multi-tenant.

---

### Response (abstract record)
**Categoría:** Back-template  -  Patrón
**Definición:** Abstract record que es la raíz del árbol de tipos de resultado de un use case. Implementa `IResponse` y es la base del patrón Composite. El Handler retorna una instancia de alguna subclase concreta (Leaf), pero el tipo estático es el abstract record. El Presenter lo discrimina con pattern matching.
**En el proyecto:** `public abstract record GetUserProfileResponse : IResponse`. Las variantes concretas son `GetUserProfileSuccess` y `GetUserProfileNotFoundFailure`. Mismo patrón para todos los use cases: `GetContactResponse`, `CreateConversationResponse`, etc.

---

### ResultViewModel\<T\>
**Categoría:** Back-template  -  Patrón
**Definición:** Clase del back-template que es el ViewModel de salida entre la Response de dominio y la respuesta HTTP (no es un Proxy: no comparte interfaz con la Response). El Presenter la puebla; el Controller la lee. Contiene `Data` (el DTO de éxito), `IsSuccess` (bool) y `Message` (mensaje de error en caso de fallo). Es serializable a JSON directamente en la respuesta HTTP.
**En el proyecto:** `ResultViewModel<UsersController>` es el tipo genérico usado en el módulo Users. El Controller lee `if (_viewModel.IsSuccess) return Ok(_viewModel)`. La `T` genérica ayuda a que el contenedor de DI resuelva el ViewModel correcto por módulo.

---

### ROI (Return on Investment)
**Categoría:** Gestión de proyectos  -  Métricas de negocio
**Definición:** Retorno sobre la inversión. Métrica financiera que mide el beneficio obtenido en relación al costo de una inversión. Para el Product Owner, el ROI de un feature es su valor para el usuario (y por ende el negocio) dividido por el costo de implementación (Story Points × velocidad del equipo × costo por punto).
**En el proyecto:** El PO prioriza el backlog del sistema de mensajería basándose en ROI: features de alto impacto para el usuario (bandeja de conversaciones, asignación de agentes) van antes que mejoras internas.

---

## S

### SaveChangesAsync
**Categoría:** EF Core  -  API
**Definición:** Método de `DbContext` que persiste todos los cambios pendientes (entidades agregadas, modificadas o eliminadas) a la base de datos en una sola transacción. Es el método que "materializa" las operaciones acumuladas en el Change Tracker. Siempre debe ser awaited y recibir el CancellationToken.
**En el proyecto:** `await _db.SaveChangesAsync(cancellationToken)` al final de `InsertAsync`, `UpdateAsync` y `DisableAsync` en los repositorios. Parte del patrón Builder: primero se construye la entidad, luego se persiste en un solo paso.

---

### Scoped (DI Lifetime)
**Categoría:** .NET  -  DI  -  Lifetimes
Lifetime de DI donde se crea una nueva instancia del servicio por cada petición HTTP. Todos los componentes que lo inyectan dentro de la misma petición reciben la misma instancia. Se destruye al terminar la petición. Es el lifetime correcto para `AppDbContext` (una transacción por petición) y para servicios que leen datos del JWT actual.

**En el proyecto:** `AppDbContext`, todos los repositorios (`IConversationRepository`, `IContactRepository`, etc.), `ICurrentUserService` y `ICurrentTenantService` son Scoped. Todos los Handlers de MediatR también son Scoped para compartir el mismo DbContext dentro de una petición.

---

### sealed class / sealed record
**Categoría:** C#  -  Modificadores
**Definición:** Modificador `sealed` que impide que una clase o record sea heredada. En los Handlers del back-template, `sealed` es la regla: los Handlers no están pensados para ser heredados, y `sealed` lo expresa explítamente. Además, el compilador puede optimizar llamadas a métodos de clases sealed. Los Leaf del patrón Composite son sealed records.
**En el proyecto:** `public sealed class GetUserProfileHandler`, `public sealed record GetUserProfileSuccess(...)`, `public sealed record GetUserProfileNotFoundFailure(...)`. Todos los Handlers y todos los Leaf de Responses son sealed.

---

### SemVer (Semantic Versioning)
**Categoría:** Git  -  Versionado
**Definición:** Especificación de versionado con formato `MAJOR.MINOR.PATCH`. PATCH: bugfix compatible con versiones anteriores. MINOR: nueva funcionalidad compatible. MAJOR: cambio incompatible (breaking change). Los commits con Conventional Commits determinan automáticamente qué componente del número incrementar.
**En el proyecto:** Aplicado desde S04 al repo del sistema de mensajería. Un `feat` commit bumpeará MINOR: `v2.3.5 + feat -> v2.4.0`. Un `fix` bumpeará PATCH: `v2.4.0 + fix -> v2.4.1`. Un `BREAKING CHANGE` bumpeará MAJOR: `v2.4.1 + breaking -> v3.0.0`.

---

### Singleton (DI Lifetime)
**Categoría:** .NET  -  DI  -  Lifetimes
Lifetime de DI donde se crea exactamente una instancia del servicio al arrancar la aplicación y se comparte entre todas las peticiones hasta que la aplicación se apaga. Correcto para objetos sin estado de petición y thread-safe: conexiones reutilizables, configuración inmutable, caches compartidas. Incorrecto para `AppDbContext` o cualquier servicio que guarde estado por usuario.

**En el proyecto:** `IConnectionMultiplexer` (Redis), `ITenantContextAccessor` y las opciones de configuración leídas con `IOptions<T>` son Singleton. El contenedor de DI garantiza thread-safety al crear la instancia.

---

### Singleton (patrón de diseño)
**Categoría:** Patrón de diseño  -  Creacional
**Definición:** Patrón que garantiza que una clase tenga exactamente una instancia y proporciona un punto de acceso global a ella. En .NET moderno, el contenedor de DI es la forma preferida de implementar Singleton (registrando el servicio con `AddSingleton`) en lugar de implementar el patrón manualmente con instancias estáticas.
**En el proyecto:** `builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>()`. El contenedor garantiza que todos los componentes que inyecten `ITenantContextAccessor` reciban el mismo objeto.

---

### SRP (Single Responsibility Principle)
**Categoría:** SOLID  -  Principios
**Definición:** Primer principio de SOLID. Establece que una clase debe tener una sola razón para cambiar, es decir, una sola responsabilidad. Una clase con múltiples responsabilidades es difícil de mantener, probar y entender. El SRP no significa que la clase tenga un solo método, sino que todos sus métodos trabajen hacia el mismo propósito cohesivo.
**En el proyecto:** `GetUserProfileHandler` tiene SRP: solo orquesta la obtención de un perfil. El logging es responsabilidad del `InteractorPipeline` (Decorator). La transformación HTTP es responsabilidad del Presenter. La persistencia es responsabilidad del Repository.

---

### Stakeholder
**Categoría:** Gestión de proyectos  -  Conceptos
**Definición:** Cualquier persona, grupo u organización que tiene interés o está afectada por un proyecto. Cada stakeholder tiene diferentes intereses, motivaciones y formas preferidas de comunicación. Identificar y gestionar las expectativas de los stakeholders es responsabilidad del equipo de desarrollo y del Tech Lead.
**En el proyecto:** Product Owner (features/ROI), CTO/Arquitecto (decisiones técnicas), Management (fechas/costos/riesgos), Dev Team (DX/autonomía), Usuario final (UX/performance).

---

### static class
**Categoría:** C#  -  Tipos
**Definición:** Clase que no puede instanciarse ni heredarse. Solo puede contener miembros estáticos. Útil para utilidades puras sin estado y para métodos de extensión (`extension methods`). A diferencia del Singleton registrado en DI, una static class no puede ser reemplazada por otra implementación en tests.
**En el proyecto:** Se prefiere el Singleton mediante DI sobre las static classes porque el DI permite sustituir la implementación en tests. Las static classes quedan reservadas para helpers puramente funcionales (métodos de extensión, conversiones) sin estado.

---

### Strategy (patrón)
**Categoría:** Patrón de diseño  -  Comportamental (preview S05)
**Definición:** Patrón que define una familia de algoritmos, encapsula cada uno y los hace intercambiables. Permite que el algoritmo varíe independientemente de los clientes que lo usan. Se diferencia del Decorator en que el Strategy cambia el núcleo del comportamiento, mientras el Decorator añade comportamiento alrededor.
**En el proyecto:** Preview para S05. Caso de uso: lógica de asignación de conversaciones a agentes. `IRoundRobinStrategy`, `ISkillBasedStrategy`, `ILoadBalancedStrategy` serían implementaciones intercambiables de `IConversationAssignmentStrategy`. El Handler llama a la estrategia sin saber cuál es la implementación concreta.

---

### String Interning
**Categoría:** C#  -  Gestión de memoria
**Definición:** Técnica de .NET donde el runtime mantiene un pool de strings únicos. Los strings literales iguales en el código compilado apuntan al mismo objeto en memoria. `object.ReferenceEquals("admin", "admin")` retorna `true` para literales. Es un ejemplo natural del patrón Flyweight en .NET.
**En el proyecto:** `var role1 = "admin"; var role2 = "admin"; object.ReferenceEquals(role1, role2) == true`. El runtime no crea dos objetos string distintos, comparte el mismo. Flyweight automático de .NET.

---

### Structural Patterns (patrones estructurales)
**Categoría:** Patrón de diseño  -  Clasificación GoF
**Definición:** Familia de patrones GoF que tratan de cómo se componen las clases y los objetos para formar estructuras más grandes. Se centran en la relación entre objetos. Los 7 patrones estructurales del catálogo GoF son: Adapter, Bridge, Composite, Decorator, Facade, Flyweight y Proxy.
**En el proyecto:** Los 7 patrones estructurales se cubren en S04, Parte 2. Todos están presentes en el back-template: Adapter (repositorios), Bridge (interfaces de dominio), Composite (jerarquía de Responses), Decorator (InteractorPipeline), Facade (Mediator.Send), Flyweight (IOptions), Proxy (lazy loading de EF Core).

---

## T

### Target (Adapter)
**Categoría:** Patrón de diseño  -  Adapter (rol)
**Definición:** La interfaz que el sistema espera. En el patrón Adapter, el Target es la interfaz que el código consumidor conoce y usa. El Adaptee tiene una interfaz diferente; el Adapter traduce entre ambas sin modificar ninguna de las dos.
**En el proyecto:** `IUserProfileRepository` es el Target. `Application` y `Domain` lo conocen. `UserProfileRepository` (Adapter) implementa este Target internamente usando `AppDbContext` (Adaptee).

---

### Tech Lead
**Categoría:** Roles  -  Equipo
**Definición:** Rol responsable de las decisiones técnicas del equipo, la arquitectura del sistema, la calidad del código y el mentoring técnico. Hace code reviews, define estándares, identifica riesgos técnicos y facilita la comunicación entre el equipo y los stakeholders técnicos. En equipos maduros, el Tech Lead empodera al equipo para tomar decisiones técnicas de forma autónoma.

---

### TenantContextAccessor
**Categoría:** Back-template  -  Multi-tenancy
**Definición:** Servicio del back-template registrado como Singleton que almacena el contexto del tenant actual (workspace) del request HTTP en curso. Lo leen el `AppDbContext` (para los global query filters), el logger (para los structured logs) y otros servicios transversales.
**En el proyecto:** Ejemplo del patrón Singleton en DI: `builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>()`. Es seguro como Singleton porque su estado se mantiene en `AsyncLocal<T>` o similar, que es thread-safe.

---

### Transient (DI Lifetime)
**Categoría:** .NET  -  DI  -  Lifetimes
Lifetime de DI donde se crea una nueva instancia del servicio cada vez que se inyecta, incluso dentro de la misma petición. Correcto para servicios stateless y baratos de crear. Si el servicio implementa `IDisposable`, cada instancia se disposa al salir del scope del consumidor.

**En el proyecto:** `IPasswordHasher` y `IEmailRenderer` son Transient porque son stateless y su construcción es barata. Si tuvieran estado mutable compartido entre llamadas, Transient garantizaría que cada consumidor tenga su propia instancia limpia.

---

### Triangulación (estimación)
**Categoría:** Gestión de proyectos  -  Metodologías ágiles
**Definición:** Técnica de estimación donde una historia nueva se compara con historias ya completadas por el mismo equipo, usando esas historias como referencia de calibración. Convierte el proceso de "adivinar" en "comparar con evidencia real". Mejora la precisión de las estimaciones a lo largo del tiempo.
**En el proyecto:** Antes de estimar "integración con API de WhatsApp", comparar con una historia similar ya completada. Si "integración con SendGrid" tomó 5 puntos, la de WhatsApp podría ser 8 por mayor complejidad de la API.

---

## U

### Unit of Work
**Categoría:** Patrón de diseño  -  Persistencia
**Definición:** Patrón que mantiene una lista de objetos afectados por una transacción de negocio y coordina la escritura de cambios y la resolución de problemas de concurrencia. `DbContext` de EF Core implementa Unit of Work implícitamente: acumula los cambios en el Change Tracker y los persiste todos juntos en `SaveChangesAsync()`.
**En el proyecto:** `AppDbContext` actúa como Unit of Work. Un request HTTP puede modificar múltiples entidades (`Contact` + `Conversation` + `Message`) y `SaveChangesAsync()` los persiste en una sola transacción.

---

### Use Case (caso de uso)
**Categoría:** Arquitectura  -  Clean Architecture
**Definición:** Unidad atómica de funcionalidad que el sistema proporciona a un actor. En Clean Architecture, cada caso de uso tiene su propia carpeta con Request, Handler, Responses y a veces Validator. Un caso de uso hace exactamente una cosa y es independiente de los demás.
**En el proyecto:** `GetUserProfile`, `CreateContact`, `SendMessage`, `AssignConversation`, `ImportContactsCsv` son casos de uso del sistema de mensajería. Cada uno vive en `ModuleName.Application/UseCases/UseCaseName/`.

---

### UserProfile
**Categoría:** Sistema de mensajería  -  Entidad
**Definición:** Entidad de dominio que representa el perfil de un usuario (agente) del workspace. Tiene `Id` (long, PK), `PublicId` (Guid, expuesto externamente), `TenantId` (long, aislamiento multi-tenant), `FullName` (string), `IsActive` (bool, soft delete), `CreatedAtUtc` y `UpdatedAtUtc`. Es la entidad principal del módulo Users.
**En el proyecto:** Usada en S04 como ejemplo en todos los patrones: Singleton (su contexto de accessor), la jerarquía de Responses (Handler que retorna Success o NotFound), Prototype (request with `{}`), Adapter (Repository adapta DbContext), etc.

---

## V

### ValidateOnStart
**Categoría:** ASP.NET Core  -  Options Pattern
**Definición:** Método del Options Pattern que configura la validación de las opciones al arrancar la aplicación, antes de que llegue el primer request. Si la configuración es inválida (falta una clave requerida, un valor fuera de rango), la aplicación no arranca en lugar de fallar en el primer request que use la configuración.
**En el proyecto:** `.ValidateOnStart()` en la configuración de `JwtSettings`. Si `JwtSettings:SecretKey` no está configurado en `appsettings.json`, la aplicación falla al arrancar con un mensaje claro, en lugar de lanzar una excepción críptica en el primer intento de autenticación.

---

### ValidateScopes / ValidateOnBuild
**Categoría:** .NET  -  DI  -  Diagnóstico
Opciones del `ServiceProvider` que activan validación de lifetimes en desarrollo. `ValidateScopes = true` detecta Captive Dependencies (Scoped inyectado en Singleton) y lanza `InvalidOperationException`. `ValidateOnBuild = true` valida todos los registros al construir el contenedor, antes de recibir la primera petición.

**En el proyecto:** Activados en `Program.cs` condicionalmente para el entorno de desarrollo. En producción se desactivan por rendimiento. Su presencia en dev garantiza que ningún bug de lifetime llegue a producción sin ser detectado.

---

### Velocity (velocidad del equipo)
**Categoría:** Gestión de proyectos  -  Métricas ágiles
**Definición:** Cantidad de Story Points que un equipo completa en promedio por sprint. Se calcula como el promedio de los últimos 3-5 sprints. Permite al Product Owner proyectar cuándo se completará el backlog y planificar releases. No es una métrica de performance individual.
**En el proyecto:** Una vez que el equipo del sistema de mensajería tenga 3-4 sprints completados, la velocidad permite proyectar cuándo estarán listos los módulos de Canales, Conversaciones y Front-end.

---

### Vertical Slice Architecture
**Categoría:** Arquitectura  -  Patrones
**Definición:** Organización del código por feature (caso de uso) en lugar de por capa técnica. Cada feature contiene todo lo que necesita: Request, Handler, Validator, Repository interface, DTO, etc. Contrasta con la organización horizontal (una carpeta para todos los Controllers, otra para todos los Repositories). Se complementa con Clean Architecture por módulo.
**En el proyecto:** Estructura actual: `Contacts.Application/UseCases/GetContact/` contiene `GetContactRequest.cs`, `GetContactHandler.cs`, `Responses/GetContactResponse.cs`, etc. Todo lo del use case en un mismo lugar.

---

## W

### `with` operator (C# records)
**Categoría:** C#  -  Records
**Definición:** Operador de C# 9+ exclusivo de los `record` que crea una copia del record con una o más propiedades modificadas. La copia es una nueva instancia; el original no se modifica (inmutabilidad). Es la implementación idiomática del patrón Prototype en C# moderno.
**En el proyecto:** `var otroUsuario = baseRequest with { PublicId = Guid.NewGuid() }` en los tests. Más conciso y expresivo que crear un nuevo `GetUserProfileRequest` repitiendo todos los parámetros. Se usa en los tests de todos los Handlers del CRM.

---

## Y

### YAGNI (You Aren't Gonna Need It)
**Categoría:** Ingeniería de software  -  Principios XP
**Definición:** Principio de Extreme Programming que dice: no implementes funcionalidad que no necesitas ahora, aunque "creas que la necesitarás en el futuro". El costo del código no usado es real: complejidad adicional, tiempo de mantenimiento, confusión para quien lo lea. Implementa lo mínimo necesario para que el sistema funcione correctamente hoy.
**En el proyecto:** Relevante al diseñar los repositorios del CRM: no agregar `GetAllAsync`, `FindBySpecificationAsync`, `PaginateAsync` si el use case actual solo necesita `GetByPublicIdAsync`. Agregar los métodos cuando haya un use case concreto que los requiera.

---

# Fin del Glosario  -  S04

**Rogelio Arriaga  -  Raptor Dev Services**
*Mayo 2026*
