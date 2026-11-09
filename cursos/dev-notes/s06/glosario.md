# Glosario Técnico  -  S06
### DDD - CQRS - Clean Architecture - Vertical Slice - Monolito Modular
**Dev Notes**
*Glosario Completo de la Sesión*

---

## A

### Abstract Record
**Categoría:** C#  -  Lenguaje
**Definición:** Tipo `record` declarado con el modificador `abstract` en C#. No puede instanciarse directamente; sirve como tipo base para una jerarquía de registros derivados. Combina la inmutabilidad de los records con el polimorfismo de las clases abstractas.
**En el proyecto:** Cada use case define un `abstract record` como base de sus respuestas  -  por ejemplo, `GetUserProfileResponse`. Los tipos concretos `GetUserProfileSuccess` y `GetUserProfileNotFoundFailure` heredan de él. El Controller hace pattern matching sobre el tipo concreto para elegir el status HTTP correcto.

---

### AddMediator
**Categoría:** Back-template  -  Infraestructura de Mediación
**Definición:** Método de extensión del back-template que registra el mini-mediator propio en el contenedor de DI. Recibe uno o más assemblies de `Application` y escanea automáticamente todos los `IRequestHandler` por convención, sin registros manuales.
**En el proyecto:** En `Program.cs` se llama `builder.Services.AddMediator(typeof(Users.Application.ServiceCollectionEx).Assembly, ...)`. Al agregar el módulo `Conversations`, solo habrá que pasar su assembly aquí.

---

### ADR (Architecture Decision Record)
**Categoría:** Arquitectura  -  Documentación
**Definición:** Documento corto que registra una decisión arquitectónica: contexto, opción elegida, alternativas descartadas, trade-offs aceptados y condición de revisión futura. Se versionan junto con el código.
**En el proyecto:** `ADR-003` documenta la decisión de Monolito Modular sobre microservicios, incluyendo la condición de revisión: "cuando un módulo supere 500 req/seg sostenidos por 4 semanas consecutivas". Convierte la deuda técnica deliberada en un item medible.

---

### Aggregate
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Cluster de objetos de dominio (entidades y value objects) tratado como una unidad de consistencia. Las modificaciones al cluster solo se hacen a través del Aggregate Root, lo que garantiza que las invariantes de negocio siempre se cumplan.
**En el proyecto:** `Conversation` es el Aggregate que agrupa `Message[]`. No se agregan mensajes directamente a la tabla  -  se llama a `conversation.AddMessage(...)`, que valida que la conversación no esté archivada antes de permitir el mensaje.

---

### Aggregate Root
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** La entidad principal de un Aggregate que actúa como única entrada para cualquier modificación del cluster. El Repository solo persiste y recupera Aggregate Roots, nunca entidades hijas por separado.
**En el proyecto:** `Conversation` es el Aggregate Root de su cluster. `Tenant` es el Aggregate Root del módulo `Tenancy`. Nunca se hace `_db.Messages.Add(msg)` directamente  -  siempre se pasa por el método público de `Conversation`.

---

### AndSpecification\<T\>
**Categoría:** Specification Pattern - Patrones
**Definición:** Clase que combina dos especificaciones con lógica AND mediante composición de expresiones lambda. Permite construir consultas complejas combinando reglas simples sin modificar el repositorio. Ejemplo: `new AndSpecification<Conversation>(unassignedSpec, openSpec)`.
**En el proyecto:** La bandeja de entrada del CRM combina `UnassignedSpec AND OpenSpec AND InChannelSpec` para construir el filtro de conversaciones visibles al agente sin agregar métodos al repositorio.

---

### Anti-corruption Layer (ACL)
**Categoría:** DDD  -  Integración entre Bounded Contexts
**Definición:** Capa de traducción que protege el modelo de un Bounded Context de la contaminación conceptual de otro contexto o sistema externo. Convierte los conceptos del sistema externo a los conceptos propios del contexto receptor.
**En el proyecto:** Cuando `Conversations` necesite datos de `Contacts`, el ACL traduce el `ContactId` (un simple `Guid`) a lo que el módulo de conversaciones necesita, sin importar la clase `Contact` completa del otro módulo. También aplica al integrar APIs externas de WhatsApp o Instagram.

---

### Application Layer
**Categoría:** Clean Architecture  -  Capas
**Definición:** Capa que orquesta los casos de uso del sistema. Contiene los Handlers (que implementan `IRequestHandler`), los objetos Request, los Response y el pipeline de comportamientos. No contiene lógica de negocio pura (eso es dominio) ni detalles de infraestructura (eso es Infrastructure).
**En el proyecto:** Cada módulo tiene su proyecto `*.Application`  -  por ejemplo, `Users.Application`. Ahí viven todas las carpetas `UseCases/{Feature}/` con Requests, Handlers y Responses. Application solo depende de Domain y de Common.

---

### Architecture Test
**Categoría:** Testing  -  Arquitectura
**Definición:** Test automatizado que verifica que las reglas de dependencia entre capas se cumplen en tiempo de compilación o en tiempo de ejecución de tests. Usa herramientas como NetArchTest para inspeccionar los assemblies y detectar violaciones.
**En el proyecto:** Se añade un test que verifica `Domain_ShouldNotDependOn_Infrastructure` usando NetArchTest. Este test corre en CI con cada Pull Request, impidiendo que alguien rompa accidentalmente las reglas de Clean Architecture.

---

### AsNoTracking
**Categoría:** EF Core  -  Performance
**Definición:** Método de EF Core que indica que las entidades retornadas por una query no deben ser rastreadas por el Change Tracker. Mejora el rendimiento significativamente en queries de solo lectura porque omite el overhead de seguimiento de cambios.
**En el proyecto:** Los Query Handlers (lecturas) pueden usar `AsNoTracking()` o directamente Dapper, ya que nunca modifican el estado. Los Command Handlers sí necesitan tracking para detectar cambios y persistirlos.

---

### Assembly
**Categoría:** .NET  -  Runtime
**Definición:** Unidad de compilación y despliegue en .NET  -  el archivo `.dll` o `.exe` generado al compilar un proyecto. Un assembly contiene tipos, recursos y metadatos. En el contexto del back-template, cada proyecto `.csproj` genera un assembly separado.
**En el proyecto:** `Users.Application` es un assembly. `Users.Domain` es otro. `AddMediator` recibe los assemblies de Application para escanear los handlers registrados en cada uno.

---

### At-Least-Once Delivery
**Categoría:** Mensajería - Sistemas Distribuidos
**Definición:** Garantía de entrega de mensajes donde el sistema asegura que cada mensaje se entrega al consumidor al menos una vez, pero posiblemente más de una vez en caso de fallos. Requiere que el consumidor sea idempotente para manejar duplicados sin efectos secundarios. Se opone a at-most-once (puede perderse) y exactly-once (muy costoso de implementar).
**En el proyecto:** El Outbox Pattern garantiza at-least-once delivery. Si el worker falla entre publicar el evento y marcarlo como procesado, lo reintentará al reiniciar. El consumidor verifica si el `MessageId` ya fue procesado antes de actuar.

---

## B

### Back-template
**Categoría:** Proyecto  -  Plantilla Base
**Definición:** Plantilla de .NET 10 con Clean Architecture, Monolito Modular, CQRS, multi-tenancy, JWT y pipeline de comportamientos ya configurados. Entregada en S01 como punto de partida del sistema de mensajería. No es un framework externo  -  es código propio que el equipo posee y puede modificar.
**En el proyecto:** Todo el sistema de mensajería se construye sobre el back-template. Los módulos `Users` y `Tenancy` ya existentes sirven de referencia para crear `Conversations`, `Contacts`, `Channels`, etc.

---

### BaseSpecification\<T\>
**Categoría:** Specification Pattern - .NET
**Definición:** Clase base abstracta que implementa `ISpecification<T>` y provee métodos protegidos para configurar criterios, includes y paginación. Las especificaciones concretas heredan de ella y llaman a `AddCriteria(expr)`, `AddInclude(expr)` y `ApplyPaging(skip, take)` en su constructor.
**En el proyecto:** Todas las especificaciones del módulo de Conversaciones (bandeja, búsqueda, SLA) heredan de `BaseSpecification<Conversation>` y configuran sus criterios en el constructor para ser inmutables una vez creadas.

---

### BCL (Base Class Library)
**Categoría:** .NET  -  Plataforma
**Definición:** La librería base de .NET que contiene los tipos fundamentales: `string`, `int`, `DateTime`, `List<T>`, `Task`, `CancellationToken`, etc. Toda aplicación .NET depende de ella implícitamente.
**En el proyecto:** La capa Domain solo puede importar la BCL  -  ningún paquete externo. Esto garantiza que las entidades y reglas de negocio sean completamente independientes de frameworks.

---

### Bounded Context
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Límite explícito dentro del cual un modelo de dominio tiene significado consistente. El mismo concepto del mundo real (por ejemplo, "Contact") puede representarse de formas completamente distintas en diferentes Bounded Contexts, con atributos y comportamientos diferentes.
**En el proyecto:** `Conversations` y `Contacts` son dos Bounded Contexts. El concepto "Contact" en `Conversations` incluye `AssignedAgentId` y `Channel`; en `Contacts` incluye `Tags`, `Segments` e `ImportedAtUtc`. Cada módulo del back-template es un Bounded Context.

---

## C

### CancellationToken
**Categoría:** C#  -  Async/Concurrencia
**Definición:** Mecanismo de .NET para propagar señales de cancelación en operaciones asíncronas. Se pasa a través de la cadena de llamadas y permite cancelar operaciones en vuelo cuando el cliente desconecta o el timeout expira.
**En el proyecto:** Todos los métodos de repositorio y handlers reciben `CancellationToken cancellationToken = default`. ASP.NET Core lo propaga automáticamente desde el request HTTP hasta los handlers y las queries de base de datos.

---

### Change Tracker
**Categoría:** EF Core  -  ORM
**Definición:** Componente de EF Core que rastrea el estado de las entidades cargadas desde la base de datos (Added, Modified, Deleted, Unchanged). Al llamar a `SaveChanges`, EF Core usa el Change Tracker para generar los SQL correspondientes automáticamente.
**En el proyecto:** Los Command Handlers han de depender del Change Tracker para persistir cambios en agregados. Los Query Handlers usan `AsNoTracking` o Dapper para evitar el overhead del tracking cuando solo leen datos.

---

### Clean Architecture
**Categoría:** Arquitectura  -  Estilo Arquitectónico
**Definición:** Estilo arquitectónico propuesto por Robert C. Martin que organiza el código en capas concéntricas con una regla fundamental: las dependencias solo apuntan hacia adentro (hacia el dominio). La capa más interna (Domain) no depende de ninguna otra; las capas externas dependen de las internas pero no al revés.
**En el proyecto:** Cada módulo del CRM tiene `*.Domain`, `*.Application`, `*.Infrastructure`, `*.Presentation`. `Domain` no importa EF Core ni ASP.NET. La regla se verifica automáticamente con architecture tests en CI.

---

### Command (CQRS)
**Categoría:** CQRS  -  Patrón
**Definición:** Operación que modifica el estado del sistema. En el contexto del back-template, un Command es un `IRequest<TResponse>` cuyo nombre comienza con un verbo de acción (`Create`, `Update`, `Disable`, `Assign`). Puede tener efectos secundarios y generalmente retorna solo éxito o fallo sin datos.
**En el proyecto:** `UpdateUserProfileRequest` es un Command. `DisableUserProfileRequest` es un Command. Su Response de éxito es `...Success()` sin datos  -  solo confirma que la operación se ejecutó correctamente.

---

### Command Bus
**Categoría:** CQRS  -  Infraestructura
**Definición:** Mecanismo que enruta un Command al Handler correspondiente. Puede ser sincrónico (el sender espera la respuesta) o asincrónico (el Command se encola y se procesa más tarde). En el back-template, el `IMediator` actúa como Command Bus sincrónico.
**En el proyecto:** `IMediator.Send(new UpdateUserProfileRequest(...))` es el Command Bus del back-template. En el futuro, al agregar procesamiento asíncrono con Hangfire o Background Services, el Command Bus podría enrutar hacia colas.

---

### Composition Root
**Categoría:** DI / IoC  -  Inversión de Control
**Definición:** El único lugar del sistema donde se configuran todas las dependencias y se ensamblan los componentes. En ASP.NET Core, el Composition Root es `Program.cs`. Es el único sitio que tiene permiso de conocer todas las implementaciones concretas.
**En el proyecto:** `Host.Api/Program.cs` es el Composition Root del CRM. Solo aquí se llama a `AddUsersInfrastructureServices()`, `AddTenancyApplicationServices()`, etc. Ningún otro archivo sabe qué implementaciones concretas se usan.

---

### Context Map
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Diagrama que muestra todos los Bounded Contexts del sistema y las relaciones entre ellos. Documenta cómo se comunican: Shared Kernel, Customer-Supplier, Conformist, Anti-corruption Layer, etc.
**En el proyecto:** El Context Map del sistema de mensajería muestra cómo `Conversations` depende de `Contacts` solo via Id, cómo `Channels` publica eventos que `Conversations` consume, y cómo `Authentication` es un contexto compartido (Shared) entre todos los módulos.

---

### Controller
**Categoría:** ASP.NET Core  -  Presentación
**Definición:** Clase que maneja las peticiones HTTP. En Clean Architecture, el Controller es parte de la capa de Presentación y su responsabilidad es mínima: recibir el request HTTP, construir el objeto Request del dominio, enviarlo al Mediator y traducir el Response a un HTTP status code.
**En el proyecto:** `UsersController` hereda de un ControllerBase propio que expone `Mediator` y `CurrentTenantId`. El método `GetById` construye `GetUserProfileRequest`, llama a `Mediator.Send`, y hace pattern matching sobre el resultado para elegir `200 OK` o `404 Not Found`.

---

### CQRS (Command Query Responsibility Segregation)
**Categoría:** Arquitectura  -  Patrón
**Definición:** Patrón que separa el modelo de lectura (Query) del modelo de escritura (Command). Las operaciones de lectura y escritura tienen caminos de código, modelos y a veces bases de datos completamente separados. Popularizado por Greg Young como evolución del patrón CQS de Bertrand Meyer.
**En el proyecto:** Cada feature del back-template sigue CQRS: `Get...Request` / `List...Request` son Queries que solo leen y retornan DTOs. `Create...`, `Update...`, `Disable...` son Commands que modifican estado. El mismo `IRequestHandler` sirve para ambos.

---

## D

### Dapper
**Categoría:** Base de Datos  -  Micro-ORM
**Definición:** Micro-ORM de .NET que ejecuta SQL puro y mapea los resultados directamente a objetos C# con reflexión mínima. Más rápido que EF Core para queries complejas de lectura porque no tiene Change Tracker ni grafo de objetos.
**En el proyecto:** Los Query Handlers que necesitan JOINs complejos (por ejemplo, listar conversaciones con nombre del agente y canal) pueden usar Dapper directamente en lugar de cargar el Aggregate completo con EF Core. El back-template incluye soporte para ambos.

---

### Definition of Done
**Categoría:** Agilidad  -  Proceso
**Definición:** Lista de criterios que debe cumplir un item de backlog para considerarse completo. Incluye no solo la funcionalidad sino también calidad: tests, documentación, code review, hygiene técnica.
**En el proyecto:** La Definition of Done del CRM incluye cobertura mínima de tests, sin warnings del compilador, architecture tests en verde y revisión por al menos un par. El 20% de capacidad para tech debt forma parte de la DoD.

---

### Dependency Injection (DI)
**Categoría:** .NET  -  Inversión de Control
**Definición:** Patrón en el que las dependencias de una clase se proporcionan desde el exterior (generamente por un contenedor de IoC) en lugar de que la clase las cree internamente. En ASP.NET Core, el contenedor de DI nativo gestiona los lifetimes (Singleton, Scoped, Transient).
**En el proyecto:** `GetUserProfileHandler` recibe `IUserProfileRepository` por constructor  -  no crea el repositorio. El contenedor registra la implementación concreta `UserProfileRepository` en `AddUsersInfrastructureServices()`.

---

### Domain Event
**Categoría:** DDD - Mensajería
**Definición:** Hecho que ocurrió en el dominio del negocio, representado como un objeto inmutable. Se publica y consume dentro del mismo bounded context y en la misma transacción de base de datos. En .NET se implementa con `INotification` de MediatR y se despacha con `IMediator.Publish()`. No cruza módulos.
**En el proyecto:** `ConversationAssignedEvent` es un Domain Event que dispara el audit log interno del módulo de Conversaciones. Ocurre en la misma transacción que el `UPDATE` de asignación, garantizando atomicidad.

---

### Domain Layer
**Categoría:** Clean Architecture  -  Capas
**Definición:** La capa más interna de Clean Architecture. Contiene las entidades, value objects, interfaces de repositorio, domain services y domain events. No tiene dependencias externas  -  solo la BCL de .NET. Es el corazón del negocio.
**En el proyecto:** `Users.Domain` contiene `UserProfile` (entity), `IUserProfileRepository` (interfaz), sin referencias a EF Core, Dapper ni ASP.NET. `Tenancy.Domain` contiene `Tenant` (Aggregate Root) y `ITenantRepository`.

---

### Domain Model
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Representación abstracta del dominio de negocio en código. Incluye las entidades, value objects, aggregates y las reglas de negocio que operan sobre ellos. El Domain Model es el vocabulario compartido entre el equipo técnico y el equipo de negocio.
**En el proyecto:** El Domain Model del CRM incluye `Conversation`, `Contact`, `Message`, `Agent`, `Channel`, `BotFlow`  -  cada uno con sus invariantes de negocio (una conversación archivada no puede recibir mensajes, un contacto debe tener email válido, etc.).

---

### Domain Service
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Componente del dominio que encapsula lógica de negocio que no pertenece naturalmente a ninguna entidad o aggregate específico. A diferencia de los Application Services, un Domain Service contiene reglas de negocio puras, sin lógica de orquestación ni acceso a infraestructura.
**En el proyecto:** `IConversationAssignmentStrategy` es un Domain Service que encapsula la lógica de asignar conversaciones a agentes según reglas configurables (round-robin, por disponibilidad, por carga). La interfaz vive en Domain; las implementaciones concretas en Infrastructure o Application.

---

### DTO (Data Transfer Object)
**Categoría:** Arquitectura  -  Patrones de Transferencia
**Definición:** Objeto plano, sin lógica de negocio, que transfiere datos entre capas o entre sistemas. Los DTOs evitan exponer las entidades de dominio directamente a la capa de presentación o a otros módulos.
**En el proyecto:** `UserProfileDto` es el DTO que `GetUserProfileSuccess` devuelve. Vive en `Users.Contracts` para que otros módulos puedan referenciarlo sin importar `Users.Domain`. Es un `sealed record` inmutable.

---

### Dual-Write Problem
**Categoría:** Sistemas Distribuidos - Anti-patrón
**Definición:** Problema que ocurre al intentar escribir en dos sistemas independientes (base de datos + message bus) sin coordinación transaccional. Si el segundo write falla, el primer write ya commitó y los sistemas quedan inconsistentes. No tiene solución perfecta sin un coordinador de transacciones distribuidas. El Outbox Pattern lo resuelve convirtiendo el segundo write en parte de la misma transacción de DB.
**En el proyecto:** Al recibir un mensaje de WhatsApp, el CRM necesita guardar en PostgreSQL Y publicar un evento. Sin Outbox, si la publicación falla, el mensaje existe en DB pero nadie es notificado. El Outbox elimina este problema.

---

## E

### EF Core (Entity Framework Core)
**Categoría:** .NET  -  ORM
**Definición:** ORM oficial de Microsoft para .NET que mapea objetos C# a tablas relacionales. Gestiona el Change Tracker, genera SQL automáticamente, soporte para migraciones, y tiene extensiones para query filters globales, multi-tenancy y más.
**En el proyecto:** `AppDbContext` (en Infrastructure) usa EF Core para mapear las entidades del CRM a PostgreSQL. Las `EntityTypeConfiguration` configuran las tablas, índices y relaciones sin contaminar las entidades de Domain.

---

### Entity
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Objeto del dominio que tiene identidad propia  -  se distingue de otros objetos de su mismo tipo por su identificador (Id), no por sus atributos. Una entidad puede mutar a lo largo del tiempo manteniendo su identidad.
**En el proyecto:** `Contact`, `Conversation`, `Agent`, `Message` son Entities. Cada uno tiene `Id` (long, surrogate key) y `PublicId` (Guid, expuesto en la API). Su identidad persiste aunque cambien todos sus atributos.

---

### EntityTypeConfiguration
**Categoría:** EF Core  -  Configuración
**Definición:** Clase que implementa `IEntityTypeConfiguration<T>` y centraliza la configuración de mapeo de una entidad (tabla, columnas, índices, relaciones, constraints) fuera de la entidad misma. En Clean Architecture, vive en la capa Infrastructure.
**En el proyecto:** `ContactConfiguration : IEntityTypeConfiguration<Contact>` define la tabla, índices únicos sobre `PublicId`, longitud máxima de `FullName` y relaciones con otras entidades. Esto mantiene `Contact` en Domain puro, sin atributos de EF Core.

---

### Eventual Consistency
**Categoría:** Arquitectura Distribuida  -  Consistencia
**Definición:** Modelo de consistencia en el que, después de una actualización, el sistema garantiza que eventualmente todos los nodos o componentes verán el estado actualizado, pero no necesariamente de forma inmediata. Contrasta con la consistencia inmediata (ACID).
**En el proyecto:** Al usar Domain Events asíncronos entre módulos, hay un período de eventual consistency. Por ejemplo, cuando se importa un contacto y se publica `ContactImported`, el módulo de analytics puede tardar milisegundos en reflejar el nuevo total. En operaciones críticas del CRM se prefiere consistencia fuerte con transacciones ACID locales.

---

### Event Bus
**Categoría:** Arquitectura  -  Mensajería
**Definición:** Infraestructura que permite publicar eventos y que múltiples suscriptores los reciban de forma desacoplada. Puede ser in-process (MediatR `Publish`) o out-of-process (RabbitMQ, Azure Service Bus, Kafka).
**En el proyecto:** En el Monolito Modular del CRM, el Event Bus in-process del mini-mediator propio (via `IMediator.Publish`) es suficiente. Si en el futuro se extraen módulos a microservicios, se migra a RabbitMQ o similar.

---

### Event Sourcing
**Categoría:** Arquitectura  -  Patrón de Persistencia
**Definición:** Patrón donde el estado del sistema se deriva de la secuencia completa de eventos ocurridos, en lugar de almacenar solo el estado actual. El aggregate se reconstruye "replaying" todos sus eventos desde el inicio.
**En el proyecto:** El sistema de mensajería no usa Event Sourcing completo  -  persiste el estado actual directamente en PostgreSQL. Sin embargo, el Audit Trail (historial de cambios) implementa un concepto similar: cada cambio relevante queda registrado como evento inmutable en una tabla separada.

---

## F

### Feature
**Categoría:** Arquitectura  -  Organización del Código
**Definición:** Unidad de funcionalidad del sistema desde la perspectiva del usuario. En Vertical Slice Architecture, cada feature es una carpeta autónoma que contiene todo lo necesario para implementar esa funcionalidad: Request, Handler, Responses, validación y mapeo.
**En el proyecto:** `GetUserProfile`, `UpdateUserProfile`, `DisableUserProfile` son features del módulo `Users`. `AssignConversation`, `ArchiveConversation` serán features del módulo `Conversations`. Cada uno vive en su propia carpeta bajo `UseCases/`.

---

### Feature Flag
**Categoría:** SaaS  -  Gestión de Características
**Definición:** Mecanismo que permite activar o desactivar funcionalidades del sistema en tiempo de ejecución, sin desplegar nuevo código. Permite lanzamientos graduales (canary), A/B testing y habilitar features por plan de suscripción.
**En el proyecto:** En el CRM, los feature flags controlarán qué canales (WhatsApp, Instagram, Email) tiene disponibles cada workspace según su plan de suscripción, sin necesidad de deploys separados.

---

### Feature Slice
**Categoría:** Vertical Slice Architecture  -  Organización
**Definición:** Sinónimo de "slice" en Vertical Slice Architecture. Una carpeta que agrupa todos los archivos necesarios para un caso de uso específico: desde el Request HTTP hasta la respuesta, pasando por validación, lógica y mapeo.
**En el proyecto:** `Users.Application/UseCases/GetUserProfile/` es un Feature Slice completo. Contiene `GetUserProfileRequest.cs`, `GetUserProfileHandler.cs` y la carpeta `Responses/` con todos los tipos de respuesta posibles.

---

## G

### Global Query Filter
**Categoría:** EF Core  -  Multi-tenancy
**Definición:** Filtro configurado en el `DbContext` que se aplica automáticamente a todas las queries de un tipo de entidad. En escenarios multi-tenant se usa para filtrar por `TenantId` en todas las consultas sin tener que escribirlo explícitamente en cada una.
**En el proyecto:** `HasQueryFilter(e => e.TenantId == _tenantContextAccessor.TenantId)` en el `AppDbContext` garantiza que ningún query retorne datos de otro tenant. Es la primera línea de defensa contra tenant leakage.

---

### God Class
**Categoría:** Anti-patrón  -  Clean Code
**Definición:** Clase que tiene demasiadas responsabilidades, sabe demasiado sobre el sistema y hace demasiado. Viola SRP y se convierte en un cuello de botella que todos los cambios deben pasar.
**En el proyecto:** El ejemplo del slides muestra `ConversationService` como God Class potencial  -  800 líneas donde vive toda la lógica de conversaciones. Con Vertical Slice y handlers pequeños, cada caso de uso tiene su propia clase de máximo ~50 líneas.

---

## H

### Handler
**Categoría:** CQRS / Mediator  -  Patrón
**Definición:** Clase que implementa `IRequestHandler<TRequest, TResponse>` y contiene la lógica para procesar un Request específico. Es el equivalente a un "use case interactor"  -  tiene una sola responsabilidad y orquesta las operaciones necesarias.
**En el proyecto:** `GetUserProfileHandler` y `UpdateUserProfileHandler` son Handlers del módulo `Users`. Cada handler tiene exactamente un método público `Handle(request, cancellationToken)`. Se registran automáticamente en el contenedor via `AddMediator`.

---

### Hexagonal Architecture (Ports & Adapters)
**Categoría:** Arquitectura  -  Estilo Arquitectónico
**Definición:** Estilo arquitectónico propuesto por Alistair Cockburn que organiza el sistema en un núcleo de aplicación rodeado por puertos (interfaces) y adaptadores (implementaciones). Los puertos definen cómo el sistema interactúa con el exterior; los adaptadores implementan esos puertos para tecnologías específicas.
**En el proyecto:** Clean Architecture es conceptualmente similar a Hexagonal. Las interfaces de repositorio (`IUserProfileRepository`) son "puertos". Las implementaciones de EF Core (`UserProfileRepository` en Infrastructure) son "adaptadores". El dominio nunca conoce los adaptadores.

---

### Host.Api
**Categoría:** Back-template  -  Estructura del Proyecto
**Definición:** El proyecto de entrada de la aplicación ASP.NET Core. Contiene `Program.cs` (Composition Root), extensiones de registro de servicios, middleware y la configuración del pipeline HTTP. No contiene lógica de negocio  -  solo orquestación.
**En el proyecto:** `Host.Api/Program.cs` registra todos los módulos, configura el pipeline HTTP, establece los middlewares de autenticación, CORS y tenant claims, y expone los endpoints de health check y métricas Prometheus.

---

## I

### IConflictFailure
**Categoría:** Back-template  -  Result Pattern
**Definición:** Interfaz del back-template que marca una respuesta de fallo por conflicto (por ejemplo, recurso ya existe). El Controller la mapea a HTTP 409 Conflict.
**En el proyecto:** Cuando se intenta crear un `Contact` con un email que ya existe en el workspace, el Handler retorna un record que implementa `IConflictFailure`. El Controller detecta el tipo y responde con 409.

---

### IEntityTypeConfiguration
**Categoría:** EF Core  -  Configuración
**Definición:** Interfaz de EF Core que permite separar la configuración del mapeo de una entidad en una clase dedicada, fuera del `DbContext`. Implementa el método `Configure(EntityTypeBuilder<T>)` donde se definen tabla, columnas, índices y relaciones.
**En el proyecto:** Cada entidad del CRM tiene su `*Configuration : IEntityTypeConfiguration<*>` en el proyecto `*.Infrastructure`. Esto mantiene las entidades en Domain puras y sin atributos de EF Core.

---

### IFailure
**Categoría:** Back-template  -  Result Pattern
**Definición:** Interfaz base del back-template para todos los tipos de fallo. Expone una propiedad `Message` de tipo string. El `ResultViewModel` la usa para poblar el mensaje de error. El pipeline la detecta para marcar la respuesta como fallida.
**En el proyecto:** `INotFoundFailure`, `IValidationFailure` e `IConflictFailure` heredan de `IFailure`. El Controller verifica `_viewModel.IsSuccess` (que internamente detecta `IFailure`) antes de elegir el status HTTP.

---

### IInteractor
**Categoría:** Back-template  -  Abstracciones
**Definición:** Interfaz en `Common.Abstractions` que extiende `IRequestHandler<TRequest, TResponse>` añadiendo la constraint `where TResponse : IResponse`. Garantiza que todos los handlers del back-template devuelvan tipos compatibles con el pipeline de respuestas.
**En el proyecto:** Los handlers del CRM implementan `IInteractor<TRequest, TResponse>` en lugar de `IRequestHandler` directamente, lo que garantiza que el pipeline automático pueda procesar sus respuestas uniformemente.

---

### IMediator
**Categoría:** Back-template  -  Mediación
**Definición:** Interfaz del mini-mediator propio del back-template. Expone `Send<TResponse>(IRequest<TResponse>, CancellationToken)` para despachar requests y `Publish<TNotification>(notification, CancellationToken)` para publicar Domain Events.
**En el proyecto:** El Controller llama a `Mediator.Send(new GetUserProfileRequest(...))`. `IMediator` es la única dependencia que el Controller necesita  -  no conoce qué Handler procesará el request.

---

### INotFoundFailure
**Categoría:** Back-template  -  Result Pattern
**Definición:** Interfaz del back-template que marca una respuesta de fallo por recurso no encontrado. El Controller la mapea a HTTP 404 Not Found.
**En el proyecto:** `GetUserProfileNotFoundFailure` implementa `INotFoundFailure`. Cuando el perfil no existe, el Handler retorna este tipo y el Controller responde con 404 sin lanzar excepciones.

---

### INotification
**Categoría:** Back-template  -  Mensajería
**Definición:** Interfaz del back-template que marca un tipo como notificación publicable via `IMediator.Publish`. Los Domain Events implementan esta interfaz para poder ser publicados y consumidos por múltiples handlers.
**En el proyecto:** `ConversationAssigned(Guid ConversationId, Guid AgentId, Guid ContactId)` implementa `INotification`. Múltiples handlers pueden suscribirse a este evento sin que `Conversations` sepa cuántos ni cuáles son.

---

### IQueryable
**Categoría:** .NET / EF Core  -  Anti-patrón de Repository
**Definición:** Interfaz de .NET que representa una query compuesta por expresiones LINQ que se ejecutan de forma diferida. Cuando un repositorio devuelve `IQueryable<T>`, la query puede ser modificada fuera del repositorio, rompiendo la encapsulación.
**En el proyecto:** Los repositorios del back-template nunca devuelven `IQueryable<T>`. Devuelven `IReadOnlyList<T>`, `T?` o DTOs. Retornar `IQueryable` filtraría los query filters de multi-tenancy y permitiría que código de Application construya queries sin restricciones.

---

### IReadOnlyList
**Categoría:** C#  -  Colecciones
**Definición:** Interfaz de .NET que expone una colección de solo lectura  -  permite iterar y acceder por índice pero no modificar. Es el tipo de retorno preferido en repositorios cuando se devuelven colecciones, porque comunica la intención de "solo leer".
**En el proyecto:** `IReadOnlyList<Message> Messages => _messages.AsReadOnly()` en `Conversation` expone los mensajes del aggregate de forma segura. Nadie puede agregar mensajes a la lista interna directamente  -  solo via `AddMessage(...)`.

---

### IRequest
**Categoría:** Back-template  -  Abstracciones de Mediación
**Definición:** Interfaz genérica del back-template que marca un objeto como request procesable por el mediator. El parámetro genérico `TResponse` indica el tipo de respuesta que el Handler devolverá.
**En el proyecto:** `GetUserProfileRequest : IRequest<GetUserProfileResponse>` le indica al mediator que el handler correspondiente devolverá un `GetUserProfileResponse`. El compilador verifica que el Handler retorne exactamente ese tipo.

---

### IRequestHandler
**Categoría:** Back-template  -  Abstracciones de Mediación
**Definición:** Interfaz genérica del back-template que marca una clase como handler de un Request específico. Expone el método `Handle(TRequest request, CancellationToken cancellationToken)` que contiene la lógica del caso de uso.
**En el proyecto:** `GetUserProfileHandler : IRequestHandler<GetUserProfileRequest, GetUserProfileResponse>` es el handler del caso de uso "obtener perfil". El mediator lo encuentra en tiempo de ejecución por convención de tipos.

---

### IResponse
**Categoría:** Back-template  -  Abstracciones
**Definición:** Interfaz del back-template que marca un tipo como respuesta válida de un Handler. Hereda de `INotification` para poder ser publicada como evento. Es la base de todos los Response types del sistema.
**En el proyecto:** `GetUserProfileResponse : IResponse` es el tipo base abstracto. El pipeline del mediator detecta si el Response implementa `IFailure` o `ISuccess<T>` para poblar el `ResultViewModel` automáticamente.

---

### ISpecification\<T\>
**Categoría:** Specification Pattern - .NET
**Definición:** Interfaz que encapsula un criterio de consulta como un objeto con una expresión lambda `Expression<Func<T, bool>>`. Permite al repositorio ejecutar cualquier criterio sin conocer las reglas de negocio. Opcionalmente incluye propiedades para Includes (eager loading), OrderBy y paginación.
**En el proyecto:** `IConversationRepository` tiene un único método `GetAsync(ISpecification<Conversation>)` que reemplaza los 20+ métodos con combinaciones de filtros que tenía la versión original.

---

### ISuccess
**Categoría:** Back-template  -  Result Pattern
**Definición:** Interfaz genérica del back-template que marca una respuesta como exitosa y expone el dato retornado via la propiedad `Data` de tipo `TData`. El `ResultViewModel` la usa para poblar el campo `Data` de la respuesta JSON.
**En el proyecto:** `GetUserProfileSuccess(UserProfileDto Data) : ISuccess<UserProfileDto>` es la respuesta exitosa del caso de uso. El pipeline detecta `ISuccess<T>` automáticamente y llama a `_viewModel.Set(success)` para serializar el DTO.

---

### ITenantContextAccessor
**Categoría:** Back-template  -  Multi-tenancy
**Definición:** Abstracción que expone el contexto del tenant activo para el request en curso. Permite que cualquier componente (Handler, Repository) acceda al `TenantId` sin depender de `HttpContext` directamente.
**En el proyecto:** `TenantClaimsMiddleware` extrae el `tenant_id` del JWT y lo almacena en `ITenantContextAccessor`. Los handlers leen `CurrentTenantId` del Controller (que viene del claim), y los repositorios pueden usar `ITenantContextAccessor` para el Global Query Filter de EF Core.

---

### IValidationFailure
**Categoría:** Back-template  -  Result Pattern
**Definición:** Interfaz del back-template que marca una respuesta de fallo por validación de inputs inválidos. El Controller la mapea a HTTP 422 Unprocessable Entity.
**En el proyecto:** Si `UpdateUserProfileRequest` llega con `FullName` vacío, el pipeline de validación (FluentValidation en un behavior) retorna un record que implementa `IValidationFailure` antes de llegar al Handler.

---

### Idempotencia (Consumidor)
**Categoría:** Mensajería - Sistemas Distribuidos
**Definición:** Propiedad de una operación que produce el mismo resultado si se ejecuta una o más veces. Un consumidor idempotente puede recibir el mismo mensaje varias veces sin efectos secundarios duplicados. Se implementa guardando los IDs de mensajes ya procesados en una tabla `processed_events`.
**En el proyecto:** El consumidor de `MessageReceivedIntegrationEvent` verifica si el `MessageId` ya existe en `processed_events` antes de enviar la notificación SignalR. Si ya existe, descarta el duplicado silenciosamente.

---

### Idempotency
**Categoría:** Arquitectura  -  Propiedades de API
**Definición:** Propiedad de una operación en la que ejecutarla múltiples veces produce el mismo resultado que ejecutarla una sola vez. Las Queries siempre son idempotentes. Los Commands pueden o no serlo según la regla de negocio.
**En el proyecto:** `GET /api/users/{id}` es idempotente  -  siempre retorna el mismo perfil. `POST /api/conversations/{id}/assign` podría no serlo si asignar dos veces tiene efectos distintos. Las APIs críticas del CRM que manejan pagos o webhooks usarán Idempotency Keys.

---

### Integration Event
**Categoría:** DDD - Mensajería - Arquitectura
**Definición:** Evento que cruza el boundary de un bounded context o módulo, publicado a través de un message bus (Redis Pub/Sub, RabbitMQ, AWS SQS). A diferencia de los Domain Events, los Integration Events son asíncronos, no transaccionales, y deben ser idempotentes. El publicador no conoce quién consume.
**En el proyecto:** `MessageReceivedIntegrationEvent` es publicado por el módulo Messages y consumido por el módulo Notifications (SignalR) y Analytics (métricas). Viaja por Redis Pub/Sub y se almacena primero en la tabla Outbox.

---

### InteractorPipeline
**Categoría:** Back-template  -  Pipeline de Comportamientos
**Definición:** Pipeline de comportamientos (behaviors) que envuelve la ejecución de cada Handler. Implementa cross-cutting concerns como logging, error handling, validación y métricas sin contaminar el código del Handler.
**En el proyecto:** El flujo real es: `IMediator.Send` -> `InteractorPipeline (logging, error handling)` -> `Handler.Handle`. El pipeline captura excepciones inesperadas, las registra en Serilog y devuelve un `IFailure` sin exponer el stack trace al cliente.

---

### Invariant (DDD)
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Regla de negocio que debe cumplirse siempre durante la vida del Aggregate. Es responsabilidad del Aggregate Root garantizar que los invariantes nunca se violen, rechazando operaciones que los romperían.
**En el proyecto:** "No se puede enviar mensajes a una conversación archivada" es un invariante de `Conversation`. `AddMessage(...)` verifica `Status == ConversationStatus.Archived` y retorna `Result.Fail(...)` si se viola. Nunca se puede romper este invariante desde fuera del Aggregate.

---

## J

### JWT (JSON Web Token)
**Categoría:** Seguridad  -  Autenticación
**Definición:** Estándar abierto (RFC 7519) para transmitir claims entre partes de forma segura como objeto JSON firmado. Contiene header, payload y firma. El payload incluye claims como `sub` (subject), `exp` (expiration) y claims personalizados como `tenant_id`.
**En el proyecto:** Los usuarios del CRM reciben un JWT HS256 al hacer login. El token contiene `tenant_id`, `user_id` y `role`. `TenantClaimsMiddleware` extrae el `tenant_id` del claim y lo pone en `ITenantContextAccessor` para todos los handlers del request.

---

## L

### Layered Architecture
**Categoría:** Arquitectura  -  Estilo Arquitectónico
**Definición:** Organización del código por tipo de responsabilidad tecnológica: Controllers juntos, Handlers juntos, Repositories juntos, Entities juntas. Cada capa sirve a la capa superior. Contrasta con Vertical Slice Architecture.
**En el proyecto:** El back-template combina Layered (la estructura de proyectos: `*.Domain`, `*.Application`, `*.Infrastructure`) con Vertical Slice (dentro de Application, cada feature en su propia carpeta `UseCases/{Feature}/`). Son complementarios, no excluyentes.

---

## M

### MediatR
**Categoría:** .NET  -  Librería
**Definición:** Librería open source popular de Jimmy Bogard que implementa el patrón Mediator para .NET. El back-template tiene su propio mini-mediator en lugar de MediatR, con las mismas abstracciones pero sin dependencia externa.
**En el proyecto:** El back-template NO usa MediatR  -  tiene `Common.Messaging` con sus propias interfaces `IMediator`, `IRequest<T>`, `IRequestHandler<TRequest, TResponse>`. El patrón es idéntico pero la implementación es propia, lo que permite extenderla sin restricciones de la librería.

---

### Mediator Pattern
**Categoría:** Diseño  -  Patrones de Comportamiento
**Definición:** Patrón de diseño que reduce las dependencias directas entre objetos haciéndolos comunicarse a través de un objeto mediador. En el contexto de aplicaciones, el mediator enruta Requests a sus Handlers sin que el emisor conozca al receptor.
**En el proyecto:** `IMediator` es el mediator. El Controller (emisor) no conoce `GetUserProfileHandler` (receptor)  -  solo conoce `IMediator` y el tipo `GetUserProfileRequest`. El mediator resuelve qué handler procesa cada request en tiempo de ejecución.

---

### Microservice
**Categoría:** Arquitectura  -  Estilo Arquitectónico
**Definición:** Estilo donde la aplicación se descompone en servicios pequeños, independientes, cada uno con su propio proceso y base de datos. Se comunican via APIs o mensajes. Requieren infraestructura compleja: service mesh, tracing distribuido, event bus, API gateway.
Para un equipo de 6 personas con dominio no probado, microservicios añadirían semanas de infraestructura antes de escribir lógica de negocio. El CRM arranca como Monolito Modular, migrando módulos a microservicios cuando el dominio y la escala lo justifiquen.

---

### Middleware
**Categoría:** ASP.NET Core  -  Pipeline HTTP
**Definición:** Componente que forma parte del pipeline de procesamiento de requests HTTP en ASP.NET Core. Cada middleware puede inspeccionar, modificar o cortocircuitar la request/response. Se encadenan en el orden en que se registran en `Program.cs`.
**En el proyecto:** El pipeline del CRM incluye `UseCoreProblemDetails`, `UseCorrelationId`, `UseCors`, `UseAuthentication`, `UseAuthorization` y `UseMiddleware<TenantClaimsMiddleware>`. El orden importa: la autenticación debe ejecutarse antes de extraer los claims del tenant.

---

### Modular Monolith
**Categoría:** Arquitectura  -  Estilo Arquitectónico
**Definición:** Variante de monolito donde el código se organiza en módulos bien aislados con límites explícitos, comparable a la separación de microservicios pero dentro de un solo proceso y deployment. Los módulos no se llaman directamente entre sí  -  se comunican via interfaces o eventos.
**En el proyecto:** El sistema de mensajería es un Modular Monolith. Tiene un solo `Host.Api`, una sola base de datos PostgreSQL y un solo deployment Docker. Los módulos `Users`, `Tenancy`, `Conversations`, `Contacts` están en carpetas separadas con sus propias capas internas y reglas de comunicación.

---

### Monolith
**Categoría:** Arquitectura  -  Estilo Arquitectónico
**Definición:** Aplicación desplegada como una sola unidad de proceso. Puede ser bien estructurada (Modular Monolith) o un "Big Ball of Mud" sin separación de responsabilidades. No es inherentemente malo  -  es el punto de partida correcto para la mayoría de los proyectos.
**En el proyecto:** El CRM comienza como monolito porque el dominio no está probado en producción, el equipo es de 6 personas y se necesita velocidad de iteración. Un monolito bien estructurado es 10× más fácil de operar que microservicios prematuros.

---

## N

### NetArchTest
**Categoría:** Testing  -  Herramienta
**Definición:** Librería de .NET para escribir tests de arquitectura que verifican las reglas de dependencia entre capas y namespaces. Permite expresar en código: "Domain no debe tener dependencias en Infrastructure".
**En el proyecto:** El módulo `Users.Tests` incluye architecture tests que verifican `Domain_ShouldNotDependOn_Infrastructure`. Estos tests corren en CI con cada push, actuando como guardianes automáticos de las reglas de Clean Architecture.

---

### Namespace
**Categoría:** C#  -  Organización del Código
**Definición:** Mecanismo de C# para organizar y agrupar tipos relacionados, evitando conflictos de nombres. En el back-template, los namespaces reflejan la estructura de carpetas y el módulo al que pertenece cada tipo.
**En el proyecto:** `Users.Application.UseCases.GetUserProfile` es el namespace del handler. `Tenancy.Domain.Entities` es el namespace de `Tenant`. Los namespaces permiten que los architecture tests verifiquen dependencias sin inspeccionar el código manualmente.

---

## O

### Orchestration vs Choreography
**Categoría:** Arquitectura Distribuida  -  Coordinación de Servicios
**Definición:** Dos enfoques para coordinar múltiples servicios en un flujo de negocio. **Orchestration**: un orquestador central dirige el flujo y decide qué componente hace qué. **Choreography**: cada componente sabe qué hacer cuando ocurre un evento, sin un coordinador central.
**En el proyecto:** En el Modular Monolith del CRM, el `AssignConversationHandler` usa orchestration: llama secuencialmente al repositorio, verifica el contacto y asigna el agente. Para side effects (notificaciones, audit trail), se usa choreography via Domain Events  -  el handler publica el evento y los suscriptores reaccionan independientemente.

---

### Outbox Pattern
**Categoría:** Mensajería - Patrones - Sistemas Distribuidos
**Definición:** Patrón que garantiza la publicación de eventos de integración guardándolos primero en una tabla `outbox_events` dentro de la misma transacción de base de datos que la acción principal. Un worker separado lee la tabla periódicamente y publica los eventos pendientes al message bus. Elimina el Dual-Write Problem y garantiza at-least-once delivery.
**En el proyecto:** Al procesar un webhook de WhatsApp, el Handler guarda el mensaje y el `MessageReceivedIntegrationEvent` en la misma transacción de PostgreSQL. El `OutboxPublisherWorker` los publica a Redis Pub/Sub cada 5 segundos y marca los procesados con `processed_at`.

---

### Outbox Table
**Categoría:** Outbox Pattern - Base de Datos
**Definición:** Tabla de base de datos que actúa como buffer transaccional para eventos de integración pendientes de publicar. Columnas típicas: `id`, `tenant_id`, `event_type`, `payload` (JSONB), `created_at`, `processed_at` (NULL si pendiente), `retry_count`, `last_error`. Un índice parcial `WHERE processed_at IS NULL` garantiza lecturas eficientes del worker.
**En el proyecto:** `outbox_events` en PostgreSQL con índice parcial en `created_at WHERE processed_at IS NULL`. El `OutboxPublisherWorker` toma lotes de 20 eventos no procesados ordenados por `created_at`.

---

## P

### Pattern Matching
**Categoría:** C#  -  Lenguaje
**Definición:** Característica de C# que permite examinar el tipo y la estructura de un objeto y ejecutar código diferente según el resultado. En el back-template, los Controllers usan pattern matching sobre los Response types para elegir el status HTTP correcto.
**En el proyecto:** `return result is GetUserProfileNotFoundFailure ? NotFound(_viewModel) : StatusCode(500, _viewModel)` es pattern matching. El Controller distingue entre tipos concretos de fallo para dar la respuesta HTTP semánticamente correcta.

---

### Pipeline Behavior
**Categoría:** Mediator  -  Cross-cutting Concerns
**Definición:** Componente que envuelve la ejecución de un Handler en el pipeline del mediator, similar a un middleware pero para casos de uso. Permite agregar comportamientos transversales (logging, validación, métricas) sin modificar los Handlers.
**En el proyecto:** `InteractorPipeline` es el behavior del back-template. Antes de llamar al Handler, registra el inicio en Serilog. Después, detecta si el resultado es `IFailure` y popula el `ResultViewModel`. El Handler nunca sabe que el pipeline existe.

---

### Ports & Adapters
**Categoría:** Hexagonal Architecture  -  Componentes
**Definición:** Los "Ports" son las interfaces que definen cómo el núcleo de la aplicación se comunica con el exterior (por ejemplo, `IUserProfileRepository`). Los "Adapters" son las implementaciones concretas de esos ports para tecnologías específicas (por ejemplo, la implementación con EF Core).
**En el proyecto:** `IConversationRepository` es un Port. `ConversationRepository` en `*.Infrastructure` es un Adapter. Este patrón es la base del principio de inversión de dependencias aplicado a la persistencia.

---

### Presentation Layer
**Categoría:** Clean Architecture  -  Capas
**Definición:** Capa que contiene los Controllers, endpoints y cualquier componente que interactúa directamente con el protocolo HTTP. Transforma los requests HTTP en objetos del dominio y los Response del dominio en respuestas HTTP.
**En el proyecto:** `Users.Presentation` contiene `UsersController`. La capa de Presentación puede importar `Application` (para construir Requests) y `Domain.Contracts` (para tipos de DTOs), pero no debe importar `Infrastructure` directamente.

---

### Program.cs
**Categoría:** ASP.NET Core  -  Composition Root
**Definición:** Punto de entrada de una aplicación ASP.NET Core (desde .NET 6 en adelante, con el modelo minimal hosting). Es el Composition Root: aquí se construye el `WebApplication`, se registran todos los servicios y se configura el pipeline HTTP.
**En el proyecto:** `Host.Api/Program.cs` registra todos los módulos del CRM (`AddUsersApplicationServices`, `AddConversationsInfrastructureServices`, etc.), configura `AddMediator` con todos los assemblies, establece los middlewares y arranca la aplicación.

---

## Q

### Query (CQRS)
**Categoría:** CQRS  -  Patrón
**Definición:** Operación que solo lee datos del sistema sin modificar su estado. En el back-template, un Query es un `IRequest<TResponse>` cuyo nombre comienza con `Get`, `List`, `Search` o `Find`. Siempre es idempotente y puede usar `AsNoTracking` o Dapper para optimizar rendimiento.
**En el proyecto:** `GetUserProfileRequest` y `GetUserProfilesRequest` son Queries. Su Response de éxito incluye un DTO con datos (`ISuccess<UserProfileDto>`). Pueden usar Dapper con SQL directo cuando el query requiere JOINs complejos.

---

## R

### Read Model
**Categoría:** CQRS  -  Modelo de Lectura
**Definición:** Representación optimizada de datos diseñada específicamente para operaciones de lectura. Puede ser un DTO plano, una vista materializada o una tabla desnormalizada que facilite queries rápidas sin necesidad de cargar el Aggregate completo.
**En el proyecto:** `UserProfileDto` es el Read Model del módulo `Users`. `ConversationListItemDto` (futuro) será el Read Model de la bandeja de conversaciones  -  incluirá nombre del agente y canal en un solo objeto plano, evitando múltiples queries.

---

### Record (C#)
**Categoría:** C#  -  Lenguaje
**Definición:** Tipo de referencia de C# 9+ diseñado para datos inmutables. Genera automáticamente igualdad por valor, `ToString()`, deconstrucción y `with` expressions. Ideal para DTOs, Value Objects y tipos de Request/Response.
**En el proyecto:** Todos los Requests, Responses y DTOs del back-template son `sealed record`. `GetUserProfileRequest(Guid PublicId, long TenantId)` es un record  -  inmutable, comparable por valor, con deconstrucción automática.

---

### Repository (DDD)
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Abstracción que encapsula la lógica de persistencia y recuperación de Aggregate Roots. Desde el punto de vista del dominio, es una colección en memoria de aggregates. La interfaz vive en Domain; la implementación concreta (con EF Core o Dapper) vive en Infrastructure.
**En el proyecto:** `IConversationRepository` en `Conversations.Domain` define `GetByIdAsync`, `InsertAsync`, `UpdateStatusAsync`. `ConversationRepository` en `Conversations.Infrastructure` implementa estos métodos usando EF Core con `AppDbContext`.

---

### Result Pattern
**Categoría:** Back-template  -  Patrón de Manejo de Errores
**Definición:** Patrón que evita el uso de excepciones para errores de negocio esperados. Los Handlers retornan objetos que representan éxito o fallo, con información suficiente para que el llamador decida qué hacer. Hace el código más explícito y testeable.
**En el proyecto:** `GetUserProfileHandler` retorna `GetUserProfileSuccess(dto)` o `GetUserProfileNotFoundFailure("...")` en lugar de lanzar `NotFoundException`. El Controller hace pattern matching sin necesidad de try/catch para lógica de negocio esperada.

---

### ResultViewModel
**Categoría:** Back-template  -  Capa de Presentación
**Definición:** Objeto genérico `ResultViewModel<T>` que encapsula todas las respuestas HTTP del sistema. Expone `Data`, `IsSuccess`, `Message` y `UtcTimeStamp`. Es el "sobre JSON" uniforme que todos los endpoints devuelven.
**En el proyecto:** El pipeline llama automáticamente a `_viewModel.Set(success)` o `_viewModel.Set(failure)` según el tipo de respuesta del Handler. El Controller solo verifica `_viewModel.IsSuccess` para elegir el status HTTP.

---

## S

### Saga
**Categoría:** Arquitectura Distribuida  -  Transacciones Distribuidas
**Definición:** Patrón para gestionar transacciones de larga duración que abarcan múltiples servicios o bounded contexts. Una Saga es una secuencia de transacciones locales, donde cada paso publica un evento que dispara el siguiente. Si un paso falla, se ejecutan transacciones compensatorias para deshacer los pasos anteriores.
**En el proyecto:** En el Monolito Modular del CRM, las transacciones ACID locales evitan la necesidad de Sagas para la mayoría de los flujos. Al integrar con WhatsApp o pasarelas de pago, una Saga gestionaría: crear conversación -> notificar canal externo -> confirmar -> rollback si el canal falla.

---

### Sealed
**Categoría:** C#  -  Lenguaje
**Definición:** Modificador de C# que impide que una clase sea heredada o que un método virtual sea sobreescrito en clases derivadas. Para types de dominio y DTOs que no deben extenderse, `sealed` es una buena práctica de diseño defensivo.
**En el proyecto:** Todas las entidades, records de Request/Response y DTOs del back-template son `sealed`. `public sealed class Contact`, `public sealed record GetUserProfileRequest`. Esto previene extensiones no intencionadas y permite al compilador optimizar el dispatch.

---

### Service (DDD Domain Service)
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Ver "Domain Service". Componente que encapsula lógica de negocio que opera sobre múltiples Aggregates o Entities y no pertenece naturalmente a ninguna de ellas. Se diferencia de un Application Service en que contiene reglas de negocio puras sin lógica de orquestación.
**En el proyecto:** `IConversationAssignmentStrategy` es un Domain Service que determina a qué agente asignar una conversación nueva. Puede tener implementaciones: `RoundRobinAssignmentStrategy`, `LoadBalancedAssignmentStrategy`, inyectadas según configuración del workspace.

---

### ServiceCollectionEx
**Categoría:** Back-template  -  Registro de Servicios
**Definición:** Clase estática de extensión de cada módulo que expone los métodos `Add*ApplicationServices()`, `Add*InfrastructureServices()` y `Add*WebApiServices()`. Centraliza el registro de todas las dependencias del módulo.
**En el proyecto:** `Users.Application.ServiceCollectionEx` tiene `AddUsersApplicationServices()`. `Users.Infrastructure.ServiceCollectionEx` tiene `AddUsersInfrastructureServices()` que registra `UserProfileRepository` como implementación de `IUserProfileRepository`.

---

### Slice (Vertical Slice)
**Categoría:** Arquitectura  -  Unidad de Organización
**Definición:** Una "rebanada vertical" del sistema que atraviesa todas las capas (Request -> Handler -> Repository -> DB) para implementar un único caso de uso. Es la unidad de organización en Vertical Slice Architecture.
**En el proyecto:** Cada carpeta bajo `UseCases/` es un Slice: `GetUserProfile/`, `UpdateUserProfile/`, `DisableUserProfile/`. Agregar un nuevo endpoint al CRM significa crear una nueva carpeta de Slice completa, sin tocar otras carpetas existentes.

---

### Soft Delete
**Categoría:** Base de Datos  -  Patrón
**Definición:** Patrón donde los registros no se eliminan físicamente de la base de datos sino que se marcan con un flag (`IsActive = false`, `DeletedAt`, etc.). Permite recuperación de datos, auditoría completa y mantener integridad referencial.
**En el proyecto:** `UserProfile` tiene `IsActive: bool` como soft delete. `DisableUserProfileHandler` llama a `_profiles.DisableAsync(...)` que actualiza `IsActive = false`. El perfil sigue en la base de datos  -  nunca se borra físicamente.

---

### Specification Pattern
**Categoría:** Patrones - DDD - Arquitectura
**Definición:** Patrón de diseño que encapsula una regla de negocio o criterio de consulta en un objeto separado y reutilizable. Permite componer criterios complejos (AND/OR/NOT) sin contaminar el repositorio con métodos específicos para cada combinación. Cada especificación es testeable independientemente. Definido por Eric Evans en *Domain-Driven Design*.
**En el proyecto:** Usado en los módulos de Conversaciones y Contactos. Reemplaza el repositorio con 20+ métodos de filtrado por una interfaz única `GetAsync(ISpecification<T>)` más especificaciones componibles como `UnassignedOpenSpec`, `ByAgentSpec`, `SearchByContactSpec`.

---

### Strong Typing
**Categoría:** C#  -  Lenguaje y Diseño
**Definición:** Práctica de usar tipos específicos en lugar de primitivos (`string`, `int`) para representar conceptos del dominio. Permite al compilador detectar errores de uso incorrecto en tiempo de compilación.
**En el proyecto:** En lugar de `string email`, el CRM usa el Value Object `Email` con validación en el constructor. En lugar de `long tenantId` y `long userId` intercambiables, se pueden crear tipos `TenantId` y `UserId` que el compilador no permitiría confundir.

---

### Subdomain
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Subdivisión del dominio de negocio principal en áreas más pequeñas y cohesivas. Los subdomains se clasifican en: **Core** (diferenciador de negocio principal), **Supporting** (necesario pero no diferenciador) y **Generic** (commodity que puede comprarse).
**En el proyecto:** El CRM tiene múltiples subdomains: Conversaciones y Bot Builder son **Core** (diferenciadores de la empresa), Contactos es **Supporting**, Autenticación y Facturación son **Generic** (se podría usar Keycloak y Stripe). Esta clasificación guía cuánta inversión de diseño merece cada módulo.

---

## T

### Technical Debt
**Categoría:** Ingeniería de Software  -  Gestión de Calidad
**Definición:** Metáfora introducida por Ward Cunningham para describir el costo futuro de no hacer las cosas correctamente ahora. Puede ser deliberada (decisión consciente con plan de pago) o accidental (resultado de ignorancia o descuido).
**En el proyecto:** El slides muestra el Cuadrante de Fowler para clasificar deuda técnica. Solo la deuda Deliberada/Prudente es aceptable y debe documentarse en un ADR con condición de revisión. Las violaciones de Clean Architecture generan deuda Imprudente que bloquea sprints meses después.

---

### TenantClaimsMiddleware
**Categoría:** Back-template  -  Middleware
**Definición:** Middleware del back-template que extrae el `tenant_id` del JWT autenticado y lo almacena en `ITenantContextAccessor`. Corre después de `UseAuthentication` y `UseAuthorization` para garantizar que el token ya fue validado.
**En el proyecto:** Después de que ASP.NET Core valida el JWT, `TenantClaimsMiddleware` lee el claim `tenant_id` y llama a `ITenantContextAccessor.SetTenantId(tenantId)`. Todos los handlers y repositorios downstream pueden acceder al tenant sin leer `HttpContext`.

---

### Tenant Leakage
**Categoría:** SaaS  -  Seguridad
**Definición:** Bug silencioso donde datos de un tenant (workspace/empresa) son visibles para otro tenant. Es uno de los bugs más graves en aplicaciones SaaS  -  puede exponer datos confidenciales de clientes sin ningún error visible.
**En el proyecto:** El Global Query Filter de EF Core (`HasQueryFilter`) es la primera línea de defensa. En una sesión posterior se implementarán integration tests específicos de tenant leakage: un agente del Tenant A no debe ver conversaciones del Tenant B, ni siquiera si adivina los IDs.

---

### Trade-off
**Categoría:** Arquitectura  -  Toma de Decisiones
**Definición:** Situación donde elegir una opción implica sacrificar algo de otra opción. En arquitectura de software, todos los patrones tienen trade-offs  -  no existe la solución perfecta para todos los contextos.
**En el proyecto:** El slides muestra los trade-offs de Vertical Slice (menos reutilización pero más autonomía), Layered (más reutilización pero más acoplamiento horizontal) y Monolito Modular (más simple de operar pero más difícil de escalar módulos individualmente). ADR-003 documenta el trade-off de Monolito Modular.

---

## U

### Ubiquitous Language
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Vocabulario compartido entre el equipo técnico y el equipo de negocio que se usa consistentemente en el código, la documentación, las conversaciones y los tickets. Elimina la traducción constante entre términos técnicos y de negocio.
**En el proyecto:** En el CRM, "Conversación", "Agente", "Canal", "Contacto", "Bandeja de entrada" son términos del Ubiquitous Language. El código usa exactamente esos nombres: `Conversation`, `Agent`, `Channel`, `Contact`. No hay `CustomerChat`, `Operator` ni `CommunicationThread`.

---

### Unit of Work
**Categoría:** Patrones de Persistencia  -  Data Access
**Definición:** Patrón que agrupa múltiples operaciones de base de datos en una sola transacción atómica. Mantiene un registro de los objetos afectados y coordina el commit o rollback de todos en conjunto.
**En el proyecto:** `AppDbContext` de EF Core implementa implícitamente Unit of Work: todas las operaciones de un Handler que usen el mismo `DbContext` participan en la misma transacción. `SaveChangesAsync()` es el commit que persiste todos los cambios atómicamente.

---

### Use Case
**Categoría:** Arquitectura  -  Capa de Aplicación
**Definición:** Caso de uso del sistema desde la perspectiva del actor. Describe qué puede hacer el sistema, no cómo lo hace internamente. En el back-template, cada Use Case corresponde a una carpeta `UseCases/{Feature}/` con su Request, Handler y Responses.
**En el proyecto:** "Obtener perfil de usuario", "Actualizar perfil", "Deshabilitar usuario" son Use Cases del módulo `Users`. "Asignar conversación a agente", "Archivar conversación" serán Use Cases de `Conversations`. Cada Use Case es una carpeta de Vertical Slice.

---

## V

### Value Object
**Categoría:** DDD  -  Domain-Driven Design
**Definición:** Objeto del dominio sin identidad propia  -  se distingue de otros por sus atributos, no por un ID. Es inmutable: una vez creado, no cambia. Encapsula validación y comportamiento relacionado con el valor. Dos Value Objects con los mismos atributos son iguales.
**En el proyecto:** `Email` y `ConversationStatus` son Value Objects. `Email` valida formato en el constructor, garantizando que nunca exista un `Email` inválido en el sistema. `ConversationStatus` es un enum-like que expresa el estado semánticamente sin usar strings literales.

---

### Vertical Slice Architecture
**Categoría:** Arquitectura  -  Estilo de Organización
**Definición:** Estilo de organización del código donde cada feature del sistema tiene su propia carpeta que contiene todo lo necesario para implementar esa funcionalidad, atravesando verticalmente las capas tradicionales. Propuesto por Jimmy Bogard. El principio es "share nothing"  -  cada slice es autónomo.
**En el proyecto:** Cada carpeta `UseCases/{Feature}/` en `*.Application` es un Vertical Slice. `GetUserProfile/` tiene su Request, Handler y Responses propios. No comparte tipos de Response con `UpdateUserProfile/`. Cambiar un feature toca solo esa carpeta.

---

## W

### Write Model
**Categoría:** CQRS  -  Modelo de Escritura
**Definición:** Representación del dominio optimizada para operaciones de escritura. Contiene el Aggregate completo con sus invariantes, validaciones y comportamientos. Contrasta con el Read Model que es plano y optimizado para lectura.
**En el proyecto:** `Conversation` con su lista de `Message[]`, su `Status` y su método `AddMessage(...)` es el Write Model. Se usa solo en Command Handlers que necesitan validar invariantes antes de escribir. Los Query Handlers nunca cargan el Aggregate completo.

---

## Z

### Zero Trust
**Categoría:** Seguridad  -  Arquitectura
**Definición:** Modelo de seguridad que asume que ningún actor dentro o fuera de la red es confiable por defecto. Requiere verificación explícita para cada request, acceso mínimo necesario y asume compromiso permanente.
**En el proyecto:** El CRM aplica Zero Trust verificando el JWT en cada request (no confía en sesiones previas), validando el `tenant_id` en cada query (no confía en que el cliente sea del tenant correcto), y aplicando RBAC estricto (no confía en que un agente tenga permisos que no están en el token).

---

# Fin del Glosario  -  S06
**Rogelio Arriaga  -  Raptor Dev Services**
*Mayo 2026*
