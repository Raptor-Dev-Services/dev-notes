# Glosario Técnico  -  Arquitecturas Comparadas

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### Abstract Record
**Categoría:** C# / Patrones de Diseño
**Definición:** Clase base declarada con la palabra clave `abstract` y la sintaxis `record` de C#. No se puede instanciar directamente; sirve como tipo común para las variantes concretas que la heredan. Combina la inmutabilidad y la semántica de valor de los records con la herencia de las clases abstractas.
**En el proyecto:** `GetContactResponse` es un `abstract record` del que heredan `GetContactSuccess` y `GetContactNotFoundFailure`. El mediador devuelve siempre el tipo base; el Presenter hace pattern matching para distinguir el caso de éxito del de error.

### Acoplamiento (Coupling)
**Categoría:** Principios de Diseño
**Definición:** Grado en que un componente depende de los detalles internos de otro. Acoplamiento alto significa que cambiar A obliga a cambiar B, C y D. Acoplamiento bajo significa que los componentes pueden evolucionar de forma independiente porque se comunican a través de abstracciones (interfaces), no a través de implementaciones concretas.
**En el proyecto:** El módulo `Contacts` no conoce `Conversations`; ambos se comunican a través de `Tenancy.Contracts`. Si se cambia EF Core por Dapper en `Contacts.Infrastructure`, ninguna otra capa fuera de Infrastructure se ve afectada.

### ACID (Atomicity, Consistency, Isolation, Durability)
**Categoría:** Bases de Datos
**Definición:** Propiedades que garantizan la confiabilidad de las transacciones en bases de datos relacionales. Atomicidad: o todo ocurre o nada ocurre. Consistencia: la DB pasa de un estado válido a otro. Aislamiento: las transacciones concurrentes no se interfieren. Durabilidad: una transacción confirmada persiste ante fallos.
**En el proyecto:** En el Modular Monolith del CRM, todas las operaciones que afectan a varias tablas (crear una conversación y registrar el primer mensaje) pueden ejecutarse en una sola transacción ACID. Esto desaparece si se migra a microservicios y se necesita el patrón Saga.

### ADR (Architecture Decision Record)
**Categoría:** Documentación / Gobernanza
**Definición:** Documento corto que registra una decisión arquitectónica importante: el contexto que la motivó, las opciones evaluadas, la decisión tomada y sus consecuencias. Sirve como memoria del equipo para entender por qué el sistema está diseñado de cierta manera.
**En el proyecto:** El primer ADR del sistema de mensajería (ADR-001) documenta la elección de Modular Monolith + Clean Architecture + Vertical Slice sobre microservicios, con la justificación explícita del tamaño del equipo y el plazo de entrega.

### API Gateway
**Categoría:** Microservicios / Infraestructura
**Definición:** Componente que actúa como punto de entrada único hacia un conjunto de microservicios. Se encarga de enrutar las peticiones, agregar respuestas, aplicar autenticación y gestionar rate limiting. Ejemplos: YARP (Yet Another Reverse Proxy) de Microsoft, Nginx, Kong.
**En el proyecto:** En la arquitectura de microservicios mostrada en los slides, un API Gateway (YARP / Nginx) concentra las peticiones del frontend React hacia los servicios `Contacts`, `Conversations`, `Messages` y `Channels`. En el Modular Monolith actual del CRM, este rol lo cumple el único proceso `Host.Api`.

### Application Layer (Capa de Aplicación)
**Categoría:** Clean Architecture / Arquitectura
**Definición:** Capa que contiene los casos de uso del sistema. Orquesta el flujo de información entre el Domain y la infraestructura, pero no contiene reglas de negocio propias. Solo depende del Domain; nunca depende de Infrastructure ni de Presentation.
**En el proyecto:** `Contacts.Application` contiene los Use Cases `GetContact`, `CreateContact`, `ImportContacts` y `DisableContact`. Cada uno es un slice autónomo con su propio Request, Handler y Responses.

### AppDbContext
**Categoría:** EF Core / Infraestructura
**Definición:** Clase que hereda de `DbContext` de Entity Framework Core y representa la sesión con la base de datos. Expone `DbSet<T>` por cada entidad y permite aplicar configuraciones de mapeo (EntityTypeConfiguration), global query filters y migraciones.
**En el proyecto:** El `AppDbContext` del sistema de mensajería contiene los `DbSet` de `Contact`, `Conversation`, `Agent`, `Channel` y aplica el global query filter de `TenantId` para garantizar el aislamiento entre workspaces.

### AsNoTracking
**Categoría:** EF Core / Performance
**Definición:** Método de EF Core que indica que las entidades devueltas por una consulta no deben ser rastreadas por el `ChangeTracker`. Mejora el rendimiento en lecturas puras porque EF Core no mantiene copia del estado original de cada entidad para detectar cambios.
**En el proyecto:** Todos los repositorios de lectura del CRM (como `GetContactHandler`) usan `.AsNoTracking()` porque solo consultan datos para mostrarlos; no los modifican en la misma sesión.

### Assembly
**Categoría:** .NET / Infraestructura
**Definición:** Unidad de compilación y despliegue en .NET. Un proyecto C# produce un ensamblado (`.dll` o `.exe`). En el contexto del mediador, se registran los assemblies que contienen los Handlers para que el mediador los descubra automáticamente mediante reflexión.
**En el proyecto:** `Host.Api/Program.cs` pasa los assemblies de `Tenancy.Application`, `Users.Application` y `Authentication.Application` al método `AddMediator` para que todos sus Handlers queden registrados.

---

## B

### BLL (Business Logic Layer)
**Categoría:** Layered Architecture
**Definición:** Capa de Lógica de Negocio en la arquitectura N-Tier. Contiene los servicios y reglas de negocio del sistema, situada entre Presentation y Data Access Layer. En implementaciones clásicas, los servicios dependen de clases concretas del DAL, lo que dificulta el testing.
**En el proyecto:** El back-template del CRM no tiene una BLL global. Cada módulo tiene su propia capa `Application` con casos de uso verticales, lo que evita que la lógica de negocio quede enterrada en servicios genéricos compartidos.

### Breaking Change
**Categoría:** API Design / Microservicios
**Definición:** Cambio en la interfaz pública de un servicio o API que rompe la compatibilidad con los consumidores existentes. Ejemplos: renombrar un campo, cambiar el tipo de un parámetro, eliminar un endpoint. En microservicios, un breaking change en un servicio requiere coordinar el versionado con todos los servicios que lo consumen.
**En el proyecto:** En el Modular Monolith del CRM, un cambio de contrato en `Contacts.Contracts` afecta solo a los módulos que referencian ese ensamblado y se detecta en tiempo de compilación. En microservicios, ese mismo cambio requeriría versionado de API y despliegues coordinados.

---

## C

### CancellationToken
**Categoría:** .NET / Async
**Definición:** Estructura de .NET que permite cancelar operaciones asíncronas de forma cooperativa. Se pasa a través de toda la cadena de llamadas (Controller -> Handler -> Repository) para que cualquier capa pueda abortar la operación si el cliente cancela la petición HTTP.
**En el proyecto:** Todos los métodos de repositorio del CRM reciben `CancellationToken cancellationToken = default`. El ASP.NET Core runtime cancela automáticamente el token si el cliente cierra la conexión.

### Circuit Breaker
**Categoría:** Resiliencia / Microservicios
**Definición:** Patrón de resiliencia que "abre el circuito" (detiene los intentos de llamada) cuando un servicio externo falla repetidamente, evitando que los reintentos saturen un sistema ya caído. Después de un tiempo de espera, permite pasar algunas llamadas de prueba para verificar si el servicio se recuperó.
**En el proyecto:** En la arquitectura de microservicios, si `Conversations` llama a `Contacts` por HTTP y `Contacts` está caído, un Circuit Breaker evitaría que `Conversations` colapse esperando respuestas. En el Modular Monolith actual, este problema no existe porque todo corre en el mismo proceso.

### Clean Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico propuesto por Robert C. Martin (Uncle Bob) en 2012. Organiza el software en capas concéntricas donde las dependencias apuntan siempre hacia adentro, hacia el dominio. Las capas son: Domain (núcleo), Application, Infrastructure y Presentation. El dominio no conoce nada de las capas externas.
**En el proyecto:** El back-template del CRM sigue Clean Architecture en cada módulo: `Contacts.Domain` no importa EF Core; `Contacts.Application` no importa `Contacts.Infrastructure`. Esta regla se verifica en compilación mediante referencias de proyecto explícitas.

### CQRS (Command Query Responsibility Segregation)
**Categoría:** Arquitectura / Patrones
**Definición:** Patrón que separa las operaciones de escritura (Commands) de las de lectura (Queries). Los Commands modifican el estado del sistema; las Queries solo lo leen y devuelven datos. Esta separación permite optimizar cada lado de forma independiente.
**En el proyecto:** En el CRM, `CreateContactRequest` es un Command (modifica estado), mientras que `GetContactRequest` es una Query (solo lee). Ambos son manejados por el mediador pero con Handlers independientes.

### Consistencia Eventual (Eventual Consistency)
**Categoría:** Microservicios / Bases de Datos Distribuidas
**Definición:** Modelo de consistencia en el que, tras una actualización, el sistema garantiza que eventualmente todos los nodos verán el mismo valor, pero no necesariamente de forma inmediata. Es la alternativa a la consistencia fuerte (ACID) en sistemas distribuidos.
**En el proyecto:** En el Modular Monolith del CRM, no hay consistencia eventual porque todo ocurre en la misma transacción ACID. Si en el futuro se extrae `Conversations` a un microservicio, operaciones como "cerrar conversación y notificar al contacto" podrían quedar en estado de consistencia eventual si un servicio falla a mitad de la operación.

### Contracts (*.Contracts)
**Categoría:** Modular Monolith / Clean Architecture
**Definición:** Proyecto que expone los DTOs y eventos de integración de un módulo hacia el exterior. Contiene solo tipos de datos (records, interfaces de eventos), sin dependencias de EF Core ni del Domain directamente. Es el único proyecto que otros módulos pueden referenciar de ese módulo.
**En el proyecto:** `Contacts.Contracts` expone `ContactDto`. Si `Conversations` necesita datos de un contacto, referencia `Contacts.Contracts`, no `Contacts.Domain` ni `Contacts.Infrastructure`. Esto garantiza que los módulos permanezcan desacoplados.

### Controller (BaseApiController)
**Categoría:** ASP.NET Core / Presentation
**Definición:** Clase que recibe las peticiones HTTP, extrae los datos necesarios (parámetros de ruta, body, claims del JWT), construye el Request correspondiente y lo envía al mediador. Luego transforma la respuesta del Presenter en un `IActionResult` HTTP apropiado.
**En el proyecto:** `ContactsController` extiende `BaseApiController`, extrae el `TenantId` del claim JWT, construye `GetContactRequest(id, CurrentTenantId)` y lo envía via `Mediator.Send`. El Presenter se encarga de traducir la respuesta a HTTP.

---

## D

### DAL (Data Access Layer)
**Categoría:** Layered Architecture
**Definición:** Capa de Acceso a Datos en la arquitectura N-Tier. Contiene los repositorios, el ORM y las consultas SQL. En Layered Architecture clásica, el BLL depende directamente de las clases concretas del DAL, lo que dificulta cambiar el mecanismo de persistencia.
**En el proyecto:** En el CRM, el equivalente al DAL es `*.Infrastructure`. La diferencia es que `Contacts.Domain` define la interfaz `IContactRepository`, e `Contacts.Infrastructure` la implementa. Esto invierte la dependencia: el dominio no depende de la infraestructura.

### Dapper
**Categoría:** Infraestructura / Bases de Datos
**Definición:** Micro-ORM de .NET que extiende `IDbConnection` con métodos de extensión para ejecutar SQL crudo y mapear los resultados a clases de C# de forma eficiente. Es más liviano que EF Core y más cercano al SQL nativo.
**En el proyecto:** En el back-template, `Users.Infrastructure` puede tener tanto un `UserProfileRepository` con EF Core para escrituras como uno con Dapper para consultas complejas de lectura. Ambos implementan la misma interfaz de Domain; Application no distingue cuál se usa.

### Dead Letter Queue
**Categoría:** Microservicios / Mensajería
**Definición:** Cola especial a la que van los mensajes que no pudieron procesarse correctamente después de varios reintentos. Permite inspeccionar y reprocesar mensajes fallidos sin perderlos. Común en sistemas de mensajería como RabbitMQ, Azure Service Bus o Amazon SQS.
**En el proyecto:** Si el CRM migrara a microservicios y usara mensajería para notificar a `Conversations` cuando se crea un contacto, los mensajes que fallen repetidamente irían a una dead letter queue. En el Modular Monolith actual, el manejo de errores es sincrónico y no requiere este mecanismo.

### DI Root (Dependency Injection Root)
**Categoría:** Inyección de Dependencias / Arquitectura
**Definición:** Punto único del sistema donde se compone el árbol de dependencias completo. Es el lugar donde se registran todos los servicios en el contenedor de DI. En una aplicación .NET, este es el archivo `Program.cs` o `Startup.cs`.
**En el proyecto:** `Host.Api/Program.cs` es el DI root del sistema de mensajería. Es el único lugar que conoce a todos los módulos (`Contacts`, `Conversations`, `Agents`, `Channels`, `Tenancy`, `Authentication`) y los ensambla registrando sus dependencias en el contenedor.

### Distributed Tracing (Trazabilidad Distribuida)
**Categoría:** Observabilidad / Microservicios
**Definición:** Técnica para seguir el flujo de una petición a través de múltiples servicios distribuidos, asignando un identificador de correlación (trace ID) que se propaga entre servicios. Herramientas: Jaeger, Zipkin, OpenTelemetry.
**En el proyecto:** En el Modular Monolith del CRM, el tracing de una petición completa es trivial porque todo ocurre en un solo proceso. Si se adoptaran microservicios, sería necesario instrumentar cada servicio con OpenTelemetry para poder correlacionar los logs de `Contacts`, `Conversations` y `Channels` en una sola traza.

### Domain (Domain Layer)
**Categoría:** Clean Architecture / DDD
**Definición:** Capa más interna de la arquitectura. Contiene las entidades de negocio, los value objects, las interfaces de repositorio y las reglas de negocio puras. No tiene dependencias de ninguna biblioteca externa: ni EF Core, ni ASP.NET Core, ni ningún framework.
**En el proyecto:** `Contacts.Domain` contiene la entidad `Contact` (con `long Id`, `Guid PublicId`, `long TenantId`, `bool IsActive`, timestamps), la interfaz `IContactRepository` y cualquier regla de negocio pura sobre contactos. No referencia `Microsoft.EntityFrameworkCore`.

### Domain Model
**Categoría:** DDD / Onion Architecture
**Definición:** El núcleo absoluto de la Onion Architecture. Representa las entidades de negocio y sus relaciones en su forma más pura, sin ninguna influencia de la infraestructura o la interfaz de usuario. Es el corazón que todas las demás capas deben preservar intacto.
**En el proyecto:** En el CRM, el Domain Model incluye `Contact`, `Conversation`, `Agent`, `Channel`, `Message` y las relaciones entre ellos (por ejemplo, una `Conversation` pertenece a un `Contact` y tiene muchos `Message`). Este modelo no cambia aunque se migre de PostgreSQL a MongoDB.

### Domain Services
**Categoría:** DDD / Onion Architecture
**Definición:** En Onion Architecture, anillo explícito entre el Domain Model y Application. Contiene lógica de negocio que no pertenece naturalmente a una sola entidad, sino que involucra la coordinación entre varias entidades del dominio. En Clean Architecture, esta lógica suele vivir en Application.
**En el proyecto:** En el CRM, la lógica de "asignar automáticamente una conversación al agente con menos carga" es un Domain Service porque involucra a `Conversation` y a `Agent`. En el back-template (Clean Architecture), esta lógica viviría en `Conversations.Application` como parte del Handler de `AssignConversation`.

### DTO (Data Transfer Object)
**Categoría:** Patrones de Diseño / API Design
**Definición:** Objeto simple que transporta datos entre capas o entre servicios. No tiene comportamiento (métodos de lógica de negocio). Se usa para exponer solo los campos necesarios hacia el exterior, sin exponer la entidad de dominio directamente.
**En el proyecto:** `ContactDto` en `Contacts.Contracts` expone `PublicId`, `FullName`, `IsActive` y los timestamps. El API no devuelve nunca la entidad `Contact` directamente; siempre devuelve un DTO para controlar qué información sale del sistema.

---

## E

### EF Core (Entity Framework Core)
**Categoría:** Infraestructura / ORM
**Definición:** ORM (Object-Relational Mapper) oficial de Microsoft para .NET. Permite trabajar con bases de datos relacionales usando objetos C# y LINQ, sin escribir SQL manualmente para la mayoría de operaciones. Soporta migraciones automáticas, global query filters, y múltiples bases de datos (PostgreSQL, SQL Server, SQLite).
**En el proyecto:** `Contacts.Infrastructure` usa EF Core (`AppDbContext`) para implementar `IContactRepository`. La entidad `Contact` tiene su `EntityTypeConfiguration` que mapea las propiedades C# a columnas de PostgreSQL, define índices y configura el filtro global por `TenantId`.

### EntityTypeConfiguration
**Categoría:** EF Core / Infraestructura
**Definición:** Clase que implementa `IEntityTypeConfiguration<T>` y centraliza la configuración de mapeo de una entidad en EF Core: nombre de tabla, esquema, columnas, índices, restricciones y relaciones. Es la alternativa preferida a las Data Annotations para mantener las entidades de Domain libres de atributos de infraestructura.
**En el proyecto:** `ContactConfiguration` define que la tabla se llama `contacts` en el esquema `contacts`, que `PublicId` tiene un índice único, y que hay un índice compuesto en `(TenantId, IsActive)` para las consultas más frecuentes.

### Evento de Integración
**Categoría:** Modular Monolith / Microservicios
**Definición:** Mensaje que un módulo publica para notificar a otros módulos o servicios que ocurrió algo relevante en su dominio. El módulo emisor no sabe quién escucha; los consumidores reaccionan de forma desacoplada. En un Modular Monolith pueden usarse eventos en memoria (via mediador); en microservicios se usan brokers de mensajes.
**En el proyecto:** Cuando se crea un `Contact` en el CRM, el módulo `Contacts` puede publicar un `ContactCreatedEvent`. El módulo `Conversations` puede suscribirse para pre-crear una conversación de bienvenida. El módulo `Contacts` no sabe que `Conversations` existe.

---

## F

### Fat Controller
**Categoría:** Anti-patrones / Layered Architecture
**Definición:** Anti-patrón en el que el controlador HTTP contiene lógica de negocio que debería estar en la capa de Application o Domain. Surge cuando no hay separación clara entre Presentation y Business Logic. Resulta en controladores difíciles de testear y mantener.
**En el proyecto:** En el CRM, los controladores (`ContactsController`, `ConversationsController`) solo extraen datos de la petición HTTP, construyen el Request y lo envían al mediador. Toda la lógica de negocio vive en el Handler correspondiente, nunca en el controlador.

### Feature Flag
**Categoría:** SaaS / Configuración
**Definición:** Mecanismo que permite activar o desactivar funcionalidades del sistema en tiempo de ejecución sin necesidad de redeploy. Puede estar controlado por tenant, por plan de suscripción o por entorno. Permite hacer releases incrementales y pruebas A/B.
**En el proyecto:** En el CRM, el módulo de `Bot Flow Builder` podría estar habilitado solo para workspaces con plan Premium mediante un feature flag. El tenant con plan básico ve el módulo deshabilitado sin que sea necesario un deploy diferente.

---

## G

### Global Query Filter
**Categoría:** EF Core / Multi-Tenancy
**Definición:** Filtro configurado a nivel del `AppDbContext` que se aplica automáticamente a todas las consultas de un `DbSet` específico. Se usa para implementar multi-tenancy (filtrar por `TenantId`) o soft delete (filtrar registros donde `IsActive = true`).
**En el proyecto:** El `AppDbContext` del CRM configura un global query filter en `Contact`, `Conversation`, `Agent` y `Channel` que añade automáticamente `WHERE TenantId = @currentTenantId` a todas las consultas, garantizando que un workspace no vea datos de otro.

---

## H

### Handler
**Categoría:** CQRS / Clean Architecture
**Definición:** Clase que implementa `IRequestHandler<TRequest, TResponse>` y contiene la lógica de un caso de uso específico. Recibe un Request, consulta o modifica el estado usando el repositorio, y devuelve un Response tipado. Es el corazón de la capa Application.
**En el proyecto:** `GetContactHandler` recibe `GetContactRequest`, llama a `IContactRepository.GetByPublicIdAsync`, y devuelve `GetContactSuccess` (si existe) o `GetContactNotFoundFailure` (si no). El Handler nunca toca el HttpContext; recibe todo lo que necesita a través del Request.

### Hexagonal Architecture (Ports & Adapters)
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico propuesto por Alistair Cockburn en 2005. El núcleo de la aplicación (el hexágono) define Ports (interfaces) y desconoce completamente los adaptadores que se conectan a él. Los Primary Adapters (HTTP, CLI, tests) invocan a la aplicación; los Secondary Adapters (DB, APIs externas) son invocados por la aplicación.
**En el proyecto:** En el CRM, `IContactRepository` y `IWhatsAppGateway` son Ports. `ContactsController` es un Primary Adapter. `ContactRepository` (EF Core) y `WhatsAppApiClient` son Secondary Adapters. El núcleo de la aplicación puede probarse con Secondary Adapters en memoria sin cambiar ninguna línea del dominio.

### Host.Api
**Categoría:** Modular Monolith / .NET
**Definición:** Proyecto de punto de entrada de la aplicación .NET. Contiene `Program.cs` (el DI root), la configuración del pipeline HTTP de ASP.NET Core, y el registro de todos los módulos. Es el único proyecto que tiene referencias a todos los módulos.
**En el proyecto:** `Host.Api` del CRM registra `Contacts`, `Conversations`, `Agents`, `Channels`, `Tenancy` y `Authentication`. También aplica el middleware `TenantClaimsMiddleware` y mapea los controladores. Los módulos no se referencian entre sí; solo `Host.Api` los conoce a todos.

---

## I

### IConflictFailure
**Categoría:** Result Pattern / Common
**Definición:** Interfaz del proyecto `Common.Results` que marca un Response como error de conflicto (HTTP 409). Se usa cuando la operación no puede completarse porque violaría una restricción de unicidad o estado del sistema, como intentar crear un recurso que ya existe.
**En el proyecto:** `CreateContactConflictFailure` implementa `IConflictFailure` y se devuelve cuando se intenta crear un contacto con un email que ya existe en el workspace. El Presenter traduce esto a un `StatusCode(409)` en la respuesta HTTP.

### IFailure
**Categoría:** Result Pattern / Common
**Definición:** Interfaz base del proyecto `Common.Results` que indica que un Response representa un error. Tiene una propiedad `string Message` con la descripción del error. Otras interfaces más específicas heredan de ella: `INotFoundFailure`, `IConflictFailure`, `IValidationFailure`.
**En el proyecto:** En el Presenter del CRM, la condición `notification is IFailure failure` captura cualquier tipo de error, sea un Not Found, un Conflict o un error de validación. Esto evita tener que enumerar cada tipo de fallo por separado.

### INotFoundFailure
**Categoría:** Result Pattern / Common
**Definición:** Interfaz del proyecto `Common.Results` que hereda de `IFailure` y marca un Response como "recurso no encontrado" (HTTP 404). Se usa cuando el Handler no puede localizar el recurso solicitado en el repositorio.
**En el proyecto:** `GetContactNotFoundFailure` implementa `INotFoundFailure`. El Controller hace pattern matching sobre el resultado: si es `INotFoundFailure`, devuelve `NotFound(_viewModel)`; si es otro tipo de fallo, devuelve `StatusCode(500, _viewModel)`.

### IRequest / IRequestHandler
**Categoría:** Mediator Pattern / Common.Messaging
**Definición:** Interfaces del sistema de mensajería interno (mediador) del back-template. `IRequest<TResponse>` marca un objeto como mensaje que espera una respuesta de tipo `TResponse`. `IRequestHandler<TRequest, TResponse>` lo implementa el Handler que procesa ese mensaje.
**En el proyecto:** `GetContactRequest : IRequest<GetContactResponse>` le dice al mediador qué tipo de respuesta esperar. `GetContactHandler : IRequestHandler<GetContactRequest, GetContactResponse>` es el único Handler registrado para ese Request. El mediador los conecta automáticamente.

### ISuccess\<T\>
**Categoría:** Result Pattern / Common
**Definición:** Interfaz genérica del proyecto `Common.Results` que indica que un Response representa una operación exitosa con datos de tipo `T`. El Presenter hace pattern matching sobre `ISuccess<T>` para extraer los datos y escribirlos en el ViewModel.
**En el proyecto:** `GetContactSuccess(ContactDto Data) : GetContactResponse, ISuccess<ContactDto>` contiene el DTO del contacto encontrado. El Presenter llama a `_viewModel.Set(success)` cuando detecta este tipo de Response.

### Inversión de Dependencias (DIP)
**Categoría:** SOLID
**Definición:** El principio D de SOLID. Los módulos de alto nivel no deben depender de módulos de bajo nivel; ambos deben depender de abstracciones. Las abstracciones no deben depender de los detalles; los detalles deben depender de las abstracciones. Es el fundamento de Clean Architecture, Onion y Hexagonal.
**En el proyecto:** `Contacts.Domain` define `IContactRepository` (abstracción). `Contacts.Infrastructure` implementa `ContactRepository` (detalle). `Contacts.Application` depende de `IContactRepository`, nunca de `ContactRepository`. En tests, se inyecta un fake de `IContactRepository` sin necesitar una base de datos real.

---

## J

### Jaeger
**Categoría:** Observabilidad / Distributed Tracing
**Definición:** Plataforma de código abierto para trazabilidad distribuida, originalmente creada por Uber. Permite visualizar el flujo de peticiones a través de múltiples servicios, identificar cuellos de botella y rastrear errores entre servicios.
**En el proyecto:** Relevante para una futura migración a microservicios del CRM. En el Modular Monolith actual, toda la observabilidad se gestiona con Serilog y los logs estructurados en un solo proceso, sin necesidad de Jaeger.

### JWT (JSON Web Token)
**Categoría:** Autenticación / Seguridad
**Definición:** Estándar (RFC 7519) para tokens de acceso que contienen claims (pares clave-valor) firmados digitalmente. El servidor puede verificar la firma sin consultar una base de datos, lo que lo hace stateless. Los claims pueden incluir el identificador del usuario, sus roles y el `tenant_id`.
**En el proyecto:** El CRM usa JWT HS256. El claim `tenant_id` dentro del token es lo que permite a `TenantClaimsMiddleware` identificar el workspace. `CurrentTenantId` en cada controlador extrae este claim y lo inyecta en el Request para el Handler.

---

## K

### Kubernetes
**Categoría:** Contenedores / Microservicios / Infraestructura
**Definición:** Plataforma de orquestación de contenedores que gestiona el despliegue, escalado y recuperación automática de servicios en contenedores. En microservicios, cada servicio corre en sus propios pods de Kubernetes con su propio ciclo de vida.
**En el proyecto:** En el Modular Monolith del CRM, Kubernetes no es necesario en las primeras etapas. Un solo contenedor Docker con la API de .NET y un contenedor de PostgreSQL son suficientes. Si se migra a microservicios, Kubernetes se vuelve necesario para gestionar los múltiples servicios.

---

## L

### Layered Architecture (N-Tier)
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que organiza el sistema en capas horizontales: Presentation, Business Logic Layer, Data Access Layer y Database. Cada capa solo interactúa con la inmediatamente inferior. Es el estilo más antiguo y conocido, simple de entender pero propenso al acoplamiento en cascada cuando no se invierte la dependencia.
**En el proyecto:** El back-template del CRM NO sigue Layered Architecture pura. Usa Clean Architecture donde la dirección de las dependencias está invertida respecto al Layered clásico: el dominio no depende de la infraestructura.

### Lifetime (DI Lifetimes)
**Categoría:** Inyección de Dependencias / .NET
**Definición:** Define cuánto tiempo vive una instancia de un servicio en el contenedor de DI. Singleton: una instancia para toda la vida de la aplicación. Scoped: una instancia por petición HTTP. Transient: una nueva instancia cada vez que se solicita. Usar el lifetime incorrecto puede causar memory leaks o errores de concurrencia.
**En el proyecto:** En el CRM, `AppDbContext` es Scoped (una sesión por petición HTTP). `IContactRepository` es Scoped. Inyectar un servicio Scoped dentro de un Singleton es un error grave (captive dependency) que se debe evitar.

---

## M

### Mediator (Mediador)
**Categoría:** Patrones de Diseño / Application
**Definición:** Patrón de diseño que reduce el acoplamiento entre objetos haciendo que se comuniquen a través de un objeto mediador central. En el contexto del back-template, el mediador recibe un Request, localiza el Handler registrado para ese tipo de Request, y le pasa la ejecución.
**En el proyecto:** `Mediator.Send(new GetContactRequest(id, tenantId), ct)` en el Controller envía el Request al mediador, que encuentra `GetContactHandler` y lo ejecuta. El Controller no conoce al Handler directamente; los desacopla el mediador.

### Middleware
**Categoría:** ASP.NET Core / Pipeline HTTP
**Definición:** Componente del pipeline de ASP.NET Core que intercepta las peticiones HTTP antes de que lleguen al controlador y/o las respuestas antes de que salgan al cliente. Se configura en `Program.cs` con `app.UseMiddleware<T>()`. Puede leer headers, modificar el contexto de la petición, o cortocircuitar la pipeline.
**En el proyecto:** `TenantClaimsMiddleware` en el CRM extrae el `tenant_id` del JWT en cada petición y lo escribe en el `TenantContextAccessor`. Esto garantiza que el contexto de tenant esté disponible en cualquier punto del pipeline sin tener que leer el JWT manualmente en cada controlador.

### Modular Monolith
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que corre en un único proceso con módulos bien aislados, cada uno con sus propias capas (Domain, Application, Infrastructure, Presentation) y contratos explícitos. Combina la simplicidad de despliegue del monolito con el aislamiento y la claridad de límites de los microservicios.
**En el proyecto:** El sistema de mensajería es un Modular Monolith con módulos `Contacts`, `Conversations`, `Agents`, `Channels`, `Tenancy` y `Authentication`. Un solo proceso, una sola base de datos, un solo deploy. La decisión está documentada en ADR-001.

### Multi-Tenancy
**Categoría:** SaaS / Arquitectura
**Definición:** Arquitectura de software en la que una única instancia de la aplicación sirve a múltiples clientes (tenants) manteniendo sus datos completamente aislados entre sí. Cada tenant puede ser una empresa, un workspace o una organización.
**En el proyecto:** En el CRM, cada empresa cliente tiene su propio workspace (tenant) identificado por un `TenantId`. Todos los datos de `Contact`, `Conversation`, `Agent` y `Channel` pertenecen a un tenant. El aislamiento se garantiza por el global query filter de EF Core y el filtro explícito en cada consulta de repositorio.

---

## N

### Namespace
**Categoría:** C# / Organización de Código
**Definición:** Mecanismo de C# para organizar y agrupar tipos relacionados, evitando colisiones de nombres entre diferentes partes del sistema o bibliotecas externas. Sigue la convención `Módulo.Capa` en el back-template.
**En el proyecto:** El CRM usa namespaces como `Contacts.Domain.Entities`, `Contacts.Application.UseCases.GetContact`, `Contacts.Infrastructure.Repositories` y `Contacts.Presentation.Controllers`. Esta convención hace evidente a qué capa y módulo pertenece cada clase.

---

## O

### Onion Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico propuesto por Jeffrey Palermo en 2008. Organiza el sistema en anillos concéntricos (como una cebolla): Domain Model en el núcleo, Domain Services, Application y Infrastructure en los anillos exteriores. Las dependencias apuntan siempre hacia el centro. Difiere de Clean Architecture principalmente en que hace explícito el anillo de Domain Services.
**En el proyecto:** El back-template del CRM sigue Clean Architecture. Onion y Clean son casi idénticos en la práctica; la diferencia es filosófica. Si se encontrara lógica de negocio que involucra múltiples entidades de dominio, se podría usar el concepto de Domain Services de Onion sin cambiar la estructura del proyecto.

### OpenTelemetry
**Categoría:** Observabilidad / Estándares
**Definición:** Estándar abierto (CNCF) para la generación, recopilación y exportación de datos de observabilidad: trazas, métricas y logs. Permite instrumentar aplicaciones una vez y enviar los datos a múltiples backends (Jaeger, Prometheus, Grafana, AWS X-Ray).
**En el proyecto:** En el CRM, OpenTelemetry se configuraría en `Host.Api` para instrumentar automáticamente ASP.NET Core, EF Core y HttpClient. En una futura migración a microservicios, los trace IDs de OpenTelemetry correlacionarían logs entre los diferentes servicios.

---

## P

### Pattern Matching
**Categoría:** C# / Programación Funcional
**Definición:** Característica de C# (desde C# 8) que permite comprobar el tipo y las propiedades de un objeto en una expresión `is` o en un `switch`. Permite escribir código más expresivo y seguro en cuanto a tipos cuando se trabaja con jerarquías de respuestas polimórficas.
**En el proyecto:** El Presenter del CRM usa pattern matching para distinguir tipos de Response: `if (notification is IFailure failure)` y `else if (notification is ISuccess<ContactDto> success)`. Esto es más seguro que casting directo porque el compilador verifica los tipos.

### Port (Puerto)
**Categoría:** Hexagonal Architecture
**Definición:** En Hexagonal Architecture, interfaz que define cómo el núcleo de la aplicación interactúa con el mundo exterior. Los Primary Ports definen cómo se invoca a la aplicación (casos de uso). Los Secondary Ports definen qué necesita la aplicación del exterior (repositorios, servicios externos).
**En el proyecto:** `IContactRepository` es un Secondary Port del CRM. `IWhatsAppGateway` es otro Secondary Port. El Handler de `CreateContact` depende de `IContactRepository`; nunca sabe si la implementación usa EF Core, Dapper, o un fake en memoria para tests.

### PostgreSQL
**Categoría:** Base de Datos
**Definición:** Sistema de gestión de bases de datos relacional de código abierto, altamente extensible y con soporte para tipos de datos avanzados (JSONB, arrays, full-text search). Es el motor de base de datos del sistema de mensajería.
**En el proyecto:** El CRM usa un único servidor PostgreSQL con esquemas separados por módulo (`contacts`, `conversations`, `agents`, `channels`). EF Core gestiona las migraciones. A futuro, se podrían usar índices GIN de PostgreSQL para la búsqueda de texto completo en mensajes y contactos.

### Presenter
**Categoría:** Clean Architecture / MVP
**Definición:** Clase que implementa `INotificationHandler<TResponse>` y es responsable de traducir el Response del Handler a un formato apropiado para la capa de Presentation (el ViewModel). Usa pattern matching sobre los diferentes tipos de Response para determinar si fue éxito o error.
**En el proyecto:** `GetContactPresenter` recibe el `GetContactResponse` publicado por el mediador. Si es `IFailure`, llama a `_viewModel.Fail(failure.Message)`. Si es `ISuccess<ContactDto>`, llama a `_viewModel.Set(success)`. El Controller solo lee el ViewModel; no interpreta el Response directamente.

### Primary Adapter
**Categoría:** Hexagonal Architecture
**Definición:** En Hexagonal Architecture, componente que invoca a la aplicación desde el exterior. Traduce el protocolo externo (HTTP, CLI, eventos) a llamadas al núcleo de la aplicación. Ejemplos: controladores HTTP, comandos de consola, clientes de tests de integración.
**En el proyecto:** `ContactsController` es el Primary Adapter HTTP del módulo Contacts. En los tests de integración, el cliente de test (`WebApplicationFactory`) actúa como Primary Adapter de tipo Test, invocando los mismos casos de uso que el controlador HTTP.

### Program.cs
**Categoría:** ASP.NET Core / .NET
**Definición:** Punto de entrada de una aplicación .NET desde .NET 6 con el modelo de hosting minimalista. Contiene la configuración del builder (DI, logging, configuración) y del pipeline HTTP (middleware, rutas). En el patrón del back-template, es el DI root.
**En el proyecto:** `Host.Api/Program.cs` del CRM ensambla todos los módulos, registra el mediador con los assemblies de Application de cada módulo, configura el middleware de autenticación y tenancy, y mapea los controladores.

---

## Q

### Query Filter (Global)
**Categoría:** EF Core
**Definición:** Ver "Global Query Filter".

---

## R

### Record (C#)
**Categoría:** C#
**Definición:** Tipo de referencia de C# (desde C# 9) con semántica de valor: la igualdad se basa en los valores de sus propiedades, no en la referencia. Inmutable por defecto con `init`. Ideal para DTOs, Requests y Responses porque garantiza que no se modifiquen después de creados.
**En el proyecto:** Todos los Requests del CRM son `sealed record`: `GetContactRequest(Guid PublicId, long TenantId)`. Todos los DTOs en `*.Contracts` son `sealed record`. Los Responses son `abstract record` (base) y `sealed record` (variantes concretas).

### Repository (Repositorio)
**Categoría:** Patrones de Diseño / Domain
**Definición:** Patrón que abstrae el acceso a la capa de persistencia presentando una interfaz orientada a colecciones de objetos del dominio. El Domain define la interfaz; Infrastructure proporciona la implementación. Permite aislar el Domain de los detalles de persistencia.
**En el proyecto:** `IContactRepository` en `Contacts.Domain` define los métodos de acceso a datos para contactos. `ContactRepository` en `Contacts.Infrastructure` lo implementa usando EF Core. El Handler solo conoce la interfaz; se puede cambiar la implementación sin tocar Application ni Domain.

### Request
**Categoría:** CQRS / Mediator Pattern
**Definición:** Objeto inmutable (generalmente un `sealed record`) que encapsula todos los datos necesarios para ejecutar un caso de uso. Es el mensaje que se envía al mediador. Puede representar un Command (escritura) o una Query (lectura).
**En el proyecto:** `CreateContactRequest(long TenantId, string FullName, string Email, string Phone)` encapsula todo lo que necesita el Handler para crear un contacto. El Controller construye el Request y lo envía al mediador; el Handler lo recibe ya validado y completo.

### Response
**Categoría:** CQRS / Result Pattern
**Definición:** Objeto que el Handler devuelve como resultado de procesar un Request. En el back-template, cada caso de uso tiene su propia jerarquía de Responses: un `abstract record` base y varios `sealed record` concretos (Success, NotFound, Conflict, etc.).
**En el proyecto:** `GetContactResponse` es el tipo base. `GetContactSuccess` y `GetContactNotFoundFailure` son las variantes concretas. El mediador publica el Response como notificación; el Presenter lo recibe y hace pattern matching.

### Result Pattern
**Categoría:** Patrones de Diseño / Application
**Definición:** Patrón que representa el resultado de una operación como un objeto tipado en lugar de lanzar excepciones para el flujo de control. El resultado puede ser éxito (con datos) o fallo (con mensaje de error). Hace explícito en el tipo del retorno que una operación puede fallar.
**En el proyecto:** En el back-template del CRM, el Result Pattern se implementa con las interfaces `ISuccess<T>`, `IFailure`, `INotFoundFailure` e `IConflictFailure` del proyecto `Common.Results`. Cada Handler devuelve un Response que implementa una de estas interfaces.

### ResultViewModel
**Categoría:** Presentation / Common
**Definición:** Clase de `Common.ViewModels` que actúa como intermediario entre el Presenter y el Controller. El Presenter escribe en el ViewModel (éxito o error). El Controller lee el ViewModel para determinar qué código HTTP y qué body devolver.
**En el proyecto:** `ResultViewModel<ContactsController>` se inyecta en el Controller y en el Presenter (ambos reciben la misma instancia Scoped). El Presenter llama a `_viewModel.Set(success)` o `_viewModel.Fail(message)`. El Controller lee `_viewModel.IsSuccess` para decidir entre `Ok()` o `NotFound()`.

---

## S

### Saga Pattern
**Categoría:** Microservicios / Transacciones Distribuidas
**Definición:** Patrón para gestionar transacciones de larga duración en sistemas distribuidos donde no es posible una transacción ACID entre múltiples servicios. Una saga es una secuencia de transacciones locales coordinadas mediante eventos o coreografía. Si una paso falla, se ejecutan transacciones compensatorias.
**En el proyecto:** Si el CRM migrara a microservicios, la operación "crear conversación y asignar agente" (que hoy es una sola transacción ACID) requeriría un Saga. El Modular Monolith evita esta complejidad completamente.

### Secondary Adapter
**Categoría:** Hexagonal Architecture
**Definición:** En Hexagonal Architecture, implementación de un Secondary Port que conecta la aplicación con recursos externos como bases de datos, APIs de terceros, sistemas de caché o brokers de mensajes. Es la contraparte de los Primary Adapters.
**En el proyecto:** `ContactRepository` (EF Core -> PostgreSQL), `WhatsAppApiClient` (HTTP -> WhatsApp Business API), y `RedisCacheService` (StackExchange.Redis -> Redis) son Secondary Adapters del CRM. Todos implementan interfaces (Ports) definidas en el Domain o Application.

### Separation of Concerns (SoC)
**Categoría:** Principios de Diseño
**Definición:** Principio de diseño que establece que cada componente del sistema debe tener una única responsabilidad claramente definida y separada de las demás. Es el fundamento de la arquitectura en capas y de todos los estilos arquitectónicos cubiertos en este módulo.
**En el proyecto:** En el CRM, el Controller es responsable del protocolo HTTP. El Handler es responsable de la lógica del caso de uso. El Repository es responsable de la persistencia. El Presenter traduce entre Application y Presentation. Ninguno invade la responsabilidad del otro.

### Service Discovery
**Categoría:** Microservicios / Infraestructura
**Definición:** Mecanismo por el que los servicios en una arquitectura de microservicios se localizan entre sí de forma dinámica. En lugar de configurar URLs estáticas, los servicios se registran en un catálogo (Consul, Kubernetes DNS, AWS Cloud Map) y los consumidores consultan ese catálogo para encontrar la dirección del servicio.
**En el proyecto:** En el Modular Monolith del CRM, el service discovery no es necesario porque los módulos se comunican mediante llamadas a método en el mismo proceso. Si se extrae `Contacts` a un microservicio, se necesitaría service discovery para que `Conversations` encuentre la URL del servicio de `Contacts`.

### ServiceCollectionEx (ServiceCollectionExtensions)
**Categoría:** .NET / Modular Monolith
**Definición:** Clase estática con métodos de extensión sobre `IServiceCollection` que registra todas las dependencias de un módulo en el contenedor de DI. Cada módulo expone sus propios métodos de extensión; `Host.Api` los llama en secuencia durante el startup.
**En el proyecto:** `Contacts.Application` expone `AddContactsApplicationServices()`. `Contacts.Infrastructure` expone `AddContactsInfrastructureServices()`. `Host.Api/Program.cs` llama a ambos. Esto permite que cada módulo sea responsable de su propio registro de dependencias.

### Slug
**Categoría:** Web / Identificadores
**Definición:** Identificador legible por humanos derivado del nombre de un recurso, usado en URLs. Se forma convirtiendo el nombre a minúsculas, reemplazando espacios por guiones y eliminando caracteres especiales. Ejemplo: "Acme Corp" -> "acme-corp".
**En el proyecto:** La entidad `Tenant` del CRM tiene una propiedad `Slug` que identifica al workspace en las URLs de la API (por ejemplo, `api/acme-corp/contacts`). Este slug debe ser único en todo el sistema y se genera al crear el workspace.

### Soft Delete
**Categoría:** Patrones de Base de Datos
**Definición:** Técnica que marca un registro como inactivo en lugar de eliminarlo físicamente de la base de datos. Se implementa con una columna booleana (`IsActive`) o un timestamp de eliminación (`DeletedAtUtc`). Preserva el historial y evita problemas de integridad referencial.
**En el proyecto:** En el CRM, `Contact`, `Agent` y `Conversation` usan soft delete con la propiedad `bool IsActive { get; init; }`. El global query filter de EF Core filtra automáticamente `WHERE IsActive = true` en todas las consultas, por lo que los registros eliminados son invisibles para la aplicación sin configuración adicional.

---

## T

### TenantClaimsMiddleware
**Categoría:** Multi-Tenancy / ASP.NET Core
**Definición:** Middleware personalizado del back-template que extrae el `tenant_id` del JWT en cada petición HTTP y lo almacena en el `TenantContextAccessor`. Garantiza que el contexto de tenant esté disponible en todo el pipeline de la petición.
**En el proyecto:** En el CRM, `TenantClaimsMiddleware` se registra en `Host.Api/Program.cs`. Cuando llega una petición, extrae `tenant_id` del claim JWT y lo pone disponible. Los controladores también leen el claim directamente via `User.FindFirstValue("tenant_id")` para construir los Requests.

### TenantContextAccessor
**Categoría:** Multi-Tenancy / .NET
**Definición:** Servicio Scoped que almacena el `TenantId` del tenant activo durante la duración de una petición HTTP. Permite acceder al `TenantId` desde cualquier parte del sistema sin necesidad de pasarlo manualmente por parámetros.
**En el proyecto:** En el CRM, aunque existe el `TenantContextAccessor`, el back-template prefiere pasar el `TenantId` explícitamente dentro de cada Request (`GetContactRequest(Guid PublicId, long TenantId)`) para que los Handlers sean testeables sin depender del contexto HTTP.

### TenantId
**Categoría:** Multi-Tenancy
**Definición:** Identificador único del tenant (workspace) que distingue los datos de un cliente de los de otro. Generalmente es un `long` o `Guid`. Aparece como claim en el JWT, fluye a través de cada Request, y es parte de todos los filtros de consulta en la base de datos.
**En el proyecto:** En el CRM, `TenantId` es `long` y está presente como propiedad en todas las entidades de dominio: `Contact`, `Conversation`, `Agent`, `Channel`. Cada consulta al repositorio incluye `WHERE TenantId = @tenantId` explícitamente en el método o implícitamente via global query filter.

### Testabilidad
**Categoría:** Calidad de Software / Arquitectura
**Definición:** Facilidad con que un sistema puede ser probado de forma automatizada y aislada. Una arquitectura con alta testabilidad permite probar el dominio y los casos de uso sin necesitar una base de datos real, un servidor HTTP activo, o servicios externos.
**En el proyecto:** En el CRM, `GetContactHandler` es completamente testeable pasando un fake de `IContactRepository` que devuelve datos predefinidos. No se necesita PostgreSQL ni EF Core para el test unitario del Handler. Los tests de integración usan `WebApplicationFactory` con una DB en memoria o Testcontainers.

### Twelve-Factor App
**Categoría:** DevOps / SaaS
**Definición:** Metodología que describe doce principios para construir aplicaciones SaaS modernas: configuración en variables de entorno, dependencias declaradas, logs como streams, procesos stateless, entre otros. Publicada por Heroku en 2012, sigue siendo la referencia para aplicaciones cloud-native.
**En el proyecto:** El CRM sigue varios factores: configuración via variables de entorno (no hardcoded), dependencias declaradas en el `.csproj`, logs como streams (Serilog), procesos stateless (el estado de conversación vive en PostgreSQL, no en memoria del proceso).

---

## U

### Uncle Bob (Robert C. Martin)
**Categoría:** Referentes de la Industria
**Definición:** Autor y consultor de software que formalizó Clean Architecture en 2012, sistematizó los principios SOLID, y co-firmó el Manifiesto Ágil. Sus libros "Clean Code", "Clean Architecture" y "The Clean Coder" son referencia en la industria.
**En el proyecto:** Clean Architecture, que es la base del back-template del CRM, fue propuesta por Uncle Bob. La regla de dependencias ("las flechas apuntan hacia adentro") es el concepto central de su propuesta y es el hilo conductor de todo el módulo de Arquitecturas Comparadas.

### Use Case (Caso de Uso)
**Categoría:** Clean Architecture / Application
**Definición:** Unidad atómica de funcionalidad de la aplicación que representa una acción que un actor puede realizar sobre el sistema. En el back-template, cada Use Case tiene su propia carpeta con Request, Handler y Responses dentro de la capa Application.
**En el proyecto:** `GetContact`, `CreateContact`, `ImportContacts`, `DisableContact`, `AssignConversation`, `CloseConversation` son Use Cases del CRM. Cada uno es un slice autónomo en `*.Application/UseCases/`. Agregar un nuevo Use Case no modifica ningún Use Case existente.

---

## V

### Value Object
**Categoría:** DDD / Domain
**Definición:** Objeto de dominio definido por sus atributos y no por su identidad. Dos Value Objects con los mismos atributos son iguales. Son inmutables. Ejemplos: `Money`, `Email`, `PhoneNumber`, `DateRange`. Encapsulan lógica de validación y comportamiento que le pertenece al concepto de negocio.
**En el proyecto:** En el CRM, `Email` podría ser un Value Object en `Contacts.Domain` que encapsula la validación del formato de email. Esto evita que la validación de email esté dispersa en múltiples Handlers o controladores.

### Vertical Slice Architecture (VSA)
**Categoría:** Arquitectura
**Definición:** Estilo que organiza el código por feature completa (slice) en lugar de por capa técnica. Cada slice contiene todo lo necesario para implementar esa feature: Request, Handler, Validator, DTO y Responses. Un PR para una feature nueva toca una sola carpeta.
**En el proyecto:** El back-template aplica VSA dentro de la capa Application de cada módulo. `Contacts.Application/UseCases/CreateContact/` contiene todo lo necesario para crear un contacto. No toca `GetContact/` ni `ImportContacts/`. Las capas Domain e Infrastructure siguen siendo horizontales.

---

## W

### WebApplicationFactory
**Categoría:** Testing / ASP.NET Core
**Definición:** Clase de `Microsoft.AspNetCore.Mvc.Testing` que permite levantar la aplicación completa en memoria para tests de integración. Crea un servidor HTTP de prueba que ejecuta el pipeline completo de ASP.NET Core, incluyendo middleware, autenticación y controladores.
**En el proyecto:** En los tests de integración del CRM, `WebApplicationFactory<Program>` levanta el `Host.Api` completo. Los tests de integración envían peticiones HTTP reales al servidor en memoria y verifican las respuestas. Puede configurarse para usar SQLite en memoria en lugar de PostgreSQL.

### WhatsApp Gateway (IWhatsAppGateway)
**Categoría:** Infraestructura / Secondary Adapter
**Definición:** Interfaz (Port) que abstrae la comunicación con la API de WhatsApp Business. Define los métodos que la aplicación necesita del canal de WhatsApp sin depender de los detalles del SDK o la API de Meta.
**En el proyecto:** `IWhatsAppGateway` en `Channels.Domain` o `Channels.Application` define métodos como `SendMessageAsync` y `GetMessageStatusAsync`. `WhatsAppApiClient` en `Channels.Infrastructure` lo implementa usando `HttpClient`. En tests, se usa un fake de `IWhatsAppGateway` que no hace llamadas HTTP reales.

---

## Y

### YARP (Yet Another Reverse Proxy)
**Categoría:** Microservicios / Infraestructura
**Definición:** Biblioteca de código abierto de Microsoft para crear reverse proxies y API Gateways en .NET. Configurable vía JSON o código C#, soporta load balancing, transformación de peticiones y autenticación.
**En el proyecto:** YARP aparece en los slides como ejemplo de API Gateway en la arquitectura de microservicios del CRM. En el Modular Monolith actual, no es necesario; `Host.Api` es el único punto de entrada.

### YAGNI (You Aren't Gonna Need It)
**Categoría:** Principios de Diseño / XP
**Definición:** Principio de Extreme Programming que establece que no se debe implementar funcionalidad hasta que realmente se necesite. Evita el desperdicio de construir abstracciones, features o infraestructura especulativa que quizás nunca se use.
**En el proyecto:** En el contexto del CRM, YAGNI justifica empezar con Modular Monolith en lugar de microservicios: no se necesita la complejidad de distribución hasta que un módulo específico necesite escalar de forma independiente. Agregar microservicios "por si acaso" viola YAGNI.

---

*Material extra  -  Arquitecturas Comparadas*
*Dev Notes  -  Raptor Dev Services*
