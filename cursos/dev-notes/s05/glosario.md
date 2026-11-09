# Glosario Técnico  -  S05
### Patrones de Comportamiento - Clean Code
**Dev Notes**
*Glosario Completo de la Sesión*

---

## A

### Abstract record
**Categoría:** C#
**Definición:** Tipo `record` declarado con `abstract` que no puede instanciarse directamente. Sirve como tipo base para una jerarquía de records derivados. En C# los records son inmutables por defecto y admiten herencia como las clases.
**En el proyecto:** `AssignConversationResponse` es un `abstract record` del que derivan `AssignConversationSuccess` y `AssignConversationNotFoundFailure`.

---

### AddMediator
**Categoría:** Back-template / DI
**Definición:** Método de extensión de `Common.Messaging` que registra en el contenedor de DI todos los `IRequestHandler`, `IPipelineBehavior` e `INotificationHandler` encontrados en los assemblies proporcionados.
**En el proyecto:** Se llama en `Host.Api/Program.cs` pasando los assemblies de `Tenancy.Application`, `Users.Application` y `Authentication.Application`.

---

### AgentId
**Categoría:** Dominio  -  Sistema de mensajería
**Definición:** Identificador público (`Guid`) del agente al que se asigna una conversación. Se almacena como propiedad de la entidad `Conversation` con setter privado para que solo los métodos de dominio puedan modificarlo.
**En el proyecto:** `Conversation.AgentId` es `Guid?` (nullable) porque una conversación recién creada no tiene agente asignado. Se popula al llamar `conversation.Assign(agentId)`.

---

### Algorithm (algoritmo)
**Categoría:** Conceptos generales
**Definición:** Secuencia finita de pasos bien definidos para resolver un problema. En el contexto de patrones de diseño, un "algoritmo" puede ser cualquier lógica intercambiable: una regla de negocio, una estrategia de asignación, un método de serialización.
**En el proyecto:** Los algoritmos de asignación de conversaciones (`RoundRobinAssignmentStrategy`, `LeastLoadAssignmentStrategy`) son intercambiables.

---

### Anti-patrón (antipattern)
**Categoría:** Diseño de software
**Definición:** Solución recurrente a un problema que parece correcta pero produce consecuencias negativas a largo plazo: mayor acoplamiento, dificultad de mantenimiento, errores silenciosos. Contrapartida del patrón de diseño.
**En el proyecto:** En S05 se cubren antipatrones como Handler que llama a otro Handler directamente, o estrategia hardcoded con `if (algo == "roundrobin")`.

---

### AppDbContext
**Categoría:** EF Core / Infrastructure
**Definición:** Clase que hereda de `DbContext` de Entity Framework Core. Representa la sesión con la base de datos y expone `DbSet<T>` para cada entidad persistida.
**En el proyecto:** Los repositorios de `Conversations.Infrastructure` usan `AppDbContext` internamente para consultas con EF Core. El `AssignConversationHandler` lo usa de forma indirecta.

---

### Arrow Code
**Categoría:** Clean Code - Anti-patrón
**Definición:** Código con múltiples niveles de anidamiento en forma de flecha apuntando a la derecha, causado por `if` dentro de `if` dentro de `for`. Hace el código difícil de leer porque la lógica principal está enterrada al fondo del anidamiento. Se elimina con Guard Clauses (validar y retornar pronto) o extrayendo métodos.
**En el proyecto:** Los Handlers del CRM usan Guard Clauses para mantener el flujo principal plano. Cualquier Handler con más de 2 niveles de anidamiento es candidato a refactorización.

---

### Assembly
**Categoría:** .NET
**Definición:** Unidad de compilación y despliegue en .NET. Un proyecto C# produce un assembly (`.dll` o `.exe`). Los assemblies contienen tipos, recursos y metadatos. El sistema de DI puede escanear assemblies para registrar servicios automáticamente.
**En el proyecto:** `AddMediator()` recibe los assemblies de las capas `Application` de cada módulo para descubrir y registrar sus Handlers automáticamente.

---

### AsAsyncEnumerable
**Categoría:** EF Core / LINQ
**Definición:** Método de extensión de EF Core que convierte un `IQueryable<T>` en `IAsyncEnumerable<T>`, habilitando la lectura fila a fila desde la base de datos sin materializar toda la colección en memoria.
**En el proyecto:** Se usa en `ContactRepository.StreamByWorkspaceAsync()` para transmitir contactos de un workspace sin cargar todos en RAM simultáneamente.

---

### AsNoTracking
**Categoría:** EF Core
**Definición:** Método que indica a EF Core que no debe registrar las entidades devueltas en el change tracker. Mejora el rendimiento en consultas de solo lectura porque evita la sobrecarga de rastrear cambios en los objetos recuperados.
**En el proyecto:** Se usa en `ContactRepository.StreamByWorkspaceAsync()` porque el streaming de contactos es para lectura, no para modificación.

---

### Assigned (estado)
**Categoría:** Dominio  -  Sistema de mensajería
**Definición:** Valor del enum `ConversationStatus` que indica que la conversación tiene un agente asignado y está siendo atendida. Transición válida desde `Open`. Permite la transición hacia `Resolved` o `Archived`.
**En el proyecto:** La entidad `Conversation` solo acepta llamar a `Resolve()` si el estado es `Assigned`; de lo contrario, retorna un `Result.Fail`.

---

### Audit log / AuditLogger
**Categoría:** Arquitectura / Observabilidad
**Definición:** Registro inmutable de acciones realizadas sobre el sistema, con actor, acción, objeto y timestamp. Permite rastrear quién hizo qué y cuándo. En el back-template se implementa como un segundo `INotificationHandler`.
**En el proyecto:** `AssignConversationAuditPresenter` implementa `INotificationHandler<AssignConversationResponse>` y loguea cada asignación exitosa.

---

### await foreach
**Categoría:** C# / async
**Definición:** Construcción del lenguaje C# para iterar de forma asíncrona sobre un `IAsyncEnumerable<T>`. Equivalente a `foreach` pero libera el hilo mientras espera cada elemento.
**En el proyecto:** El caller de `StreamByWorkspaceAsync()` itera los contactos con `await foreach (var c in _contacts.StreamByWorkspaceAsync(tenantId, ct))`.

---

## B

### BaseApiController
**Categoría:** Back-template / ASP.NET Core
**Definición:** Clase abstracta en `Shared.Web` que hereda de `ControllerBase` y provee el `IMediator` a todos los Controllers. Implementa el patrón Template Method: define el acceso al Mediator y deja los endpoints para cada subclase.
**En el proyecto:** Todos los Controllers del CRM (`ConversationsController`, `ContactsController`, etc.) heredan de `BaseApiController`.

---

### Behavioral pattern (patrón de comportamiento)
**Categoría:** Patrón de diseño
**Definición:** Categoría de patrones de diseño del Gang of Four que se ocupa de cómo los objetos colaboran, se comunican y distribuyen responsabilidades entre sí. Definen el flujo de control en tiempo de ejecución.
**En el proyecto:** S05 cubre los 10 patrones de comportamiento: Command, Iterator, Template Method, Mediator, Observer, Chain of Responsibility, State, Strategy, Memento y Visitor.

---

### Bloqueante (en code review)
**Categoría:** Proceso / Code Review
**Definición:** Comentario de revisión que señala un problema que debe corregirse antes de aprobar el PR. Se marca explícitamente como `[bloqueante]`. Su resolución es obligatoria.
**En el proyecto:** Un Handler que llama directamente a otro Handler sin pasar por el Mediator sería un comentario bloqueante en el code review del equipo.

---

### BotFlow
**Categoría:** Dominio  -  Sistema de mensajería
**Definición:** Entidad del módulo de bots que representa el grafo de flujo de un bot conversacional. Contiene nodos y las conexiones entre ellos. Se edita visualmente con React Flow en el frontend.
**En el proyecto:** `BotFlow.CreateSnapshot()` es el caso de uso del patrón Memento para guardar el estado del flujo antes de editarlo.

---

### Bounded Context
**Categoría:** DDD / Arquitectura
**Definición:** Límite explícito dentro del cual un modelo de dominio es consistente y tiene significado único. Distintos contextos pueden tener entidades con el mismo nombre pero con atributos y comportamientos diferentes.
**En el proyecto:** El sistema de mensajería tiene contextos como `Conversations`, `Contacts`, `Authentication`, `Tenancy`.

---

### Bugfix branch
**Categoría:** Git Flow
**Definición:** Rama de vida corta destinada a corregir un defecto no urgente detectado durante el desarrollo. Parte de `develop` y regresa a `develop` al terminar.
**En el proyecto:** Convención de nombres: `bugfix/crm-fix-contact-import`. El nombre identifica el tipo de cambio y el módulo afectado.

---

## C

### CancellationToken
**Categoría:** C# / async
**Definición:** Estructura que permite cooperativamente cancelar operaciones asíncronas. Se propaga desde el caller hasta las operaciones de I/O. Cuando se cancela, las operaciones lanzan `OperationCanceledException`.
**En el proyecto:** Todos los métodos de Handler, repositorio y Controller reciben `CancellationToken ct` para soportar cancelación en requests HTTP abandonados.

---

### Chain of Responsibility (patrón)
**Categoría:** Patrón de diseño
**Definición:** Patrón de comportamiento que pasa una solicitud a lo largo de una cadena de manejadores. Cada manejador decide si procesa la solicitud y/o la pasa al siguiente. Permite apilar middlewares, validadores, etc.
**En el proyecto:** `IPipelineBehavior<TRequest, TResponse>` + `InteractorPipeline` forman la cadena. Se pueden apilar: Logging -> Validation -> Authorization -> Handler.

---

### Change tracker (EF Core)
**Categoría:** EF Core
**Definición:** Componente interno del `DbContext` que rastrea el estado de cada entidad cargada (Added, Modified, Deleted, Unchanged). Se puede deshabilitar con `AsNoTracking()` para mejorar el rendimiento.
**En el proyecto:** Las consultas de streaming de contactos y exportación usan `AsNoTracking()` para evitar la sobrecarga del change tracker.

---

### Clean Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico propuesto por Robert C. Martin que organiza el código en capas concéntricas (Domain -> Application -> Infrastructure -> Presentation) donde las dependencias solo apuntan hacia adentro.
**En el proyecto:** El back-template sigue Clean Architecture con proyectos separados y dependencias unidireccionales.

---

### Clean Code
**Categoría:** Principios - Calidad de Software
**Definición:** Conjunto de prácticas y principios para escribir código legible, mantenible y modificable, popularizado por Robert C. Martin (Uncle Bob). Principios clave: nombres que revelan intención, funciones que hacen una sola cosa, sin comentarios redundantes, sin números mágicos, sin código duplicado. El objetivo es que el código comunique su intención a futuros lectores.
**En el proyecto:** El back-template fue diseñado siguiendo Clean Code. Los Handlers tienen nombres como `GetUserProfileHandler` (no `Handler1`), los métodos de repositorio son verbos descriptivos, y no hay código comentado ni magic numbers en el código base.

---

### co-reviewer
**Categoría:** Proceso / Equipo
**Definición:** Miembro del equipo que participa activamente en la revisión de PRs como revisor técnico par, con capacidad de aprobar o bloquear cambios.

---

### Code Smell
**Categoría:** Clean Code - Refactoring
**Definición:** Señal en el código que indica un posible problema de diseño o mantenibilidad, sin ser un bug en sí mismo. Catálogo original de Martin Fowler en *Refactoring*. Ejemplos: Long Method, God Class, Feature Envy, Primitive Obsession, Data Clumps, Duplicated Code, Dead Code.
**En el proyecto:** El equipo identifica code smells en code review. Un Handler de más de 30 líneas o un repositorio con más de 10 métodos son señales de que hay code smells presentes.

---

### Command pattern (patrón Command)
**Categoría:** Patrón de diseño
**Definición:** Patrón de comportamiento que encapsula una solicitud como un objeto independiente. Desacopla el emisor del receptor. El objeto Command es serializable y puede encolarse o reintentarse.
**En el proyecto:** Cada `IRequest<TResponse>` en el back-template ES un Command. `AssignConversationRequest` encapsula la intención "asignar conversación".

---

### Common.Messaging
**Categoría:** Back-template / librería
**Definición:** Librería interna que provee las interfaces y la implementación del sistema de mensajería: `IRequest<TResponse>`, `IRequestHandler<TRequest, TResponse>`, `IMediator`, `INotificationHandler<T>`, etc.
**En el proyecto:** Todos los módulos del CRM dependen de `Common.Messaging` en lugar de MediatR, lo que da control total sobre el Mediator.

---

### ConfigureAwait(false)
**Categoría:** C# / async
**Definición:** Método que indica que la continuación de un `await` no necesita capturar el contexto de sincronización actual. Mejora el rendimiento en librerías al evitar re-scheduling costoso.
**En el proyecto:** `InteractorPipeline` usa `ConfigureAwait(false)` en `await next()` y `await Mediator.Publish(response)`.

---

### Constructor injection
**Categoría:** Dependency Injection / .NET
**Definición:** Forma de inyección de dependencias donde las dependencias se declaran como parámetros del constructor. Facilita los tests y permite que el framework resuelva la cadena automáticamente.
**En el proyecto:** Todos los Handlers, Presenters y Controllers del back-template usan constructor injection.

---

### Controller (ASP.NET Core)
**Categoría:** ASP.NET Core / Presentation
**Definición:** Clase que recibe requests HTTP, los traduce a Commands/Queries, los envía al Mediator y devuelve una respuesta. El Controller no contiene lógica de negocio  -  solo emite el Command.
**En el proyecto:** `ConversationsController` hereda de `BaseApiController`, construye un `AssignConversationRequest` y llama a `Mediator.Send()`.

---

### CQRS (Command Query Responsibility Segregation)
**Categoría:** Arquitectura
**Definición:** Patrón arquitectónico que separa las operaciones de escritura (Commands) de las de lectura (Queries) usando modelos y canales distintos.
**En el proyecto:** En el back-template, un `IRequest<TResponse>` puede ser un Command o una Query. La distinción es por convención de nomenclatura y lógica del Handler.

---

### CsvExportVisitor
**Categoría:** Patrón de diseño / Dominio
**Definición:** Implementación del patrón Visitor que exporta contactos al formato CSV. Implementa la interfaz del Visitor y contiene la lógica de serialización sin modificar la entidad `Contact`.
**En el proyecto:** Se implementará más adelante (módulo de Contactos). El objeto `Contact` llamará a `contact.Accept(new CsvExportVisitor())`.

---

## D

### Data Clumps
**Categoría:** Clean Code - Code Smell
**Definición:** Code smell donde los mismos 3 o más datos aparecen siempre juntos como parámetros separados en varias funciones o campos de varias clases. La solución es agruparlos en un objeto (record o class). Si los datos tienen sentido por sí solos como grupo, merecen su propio tipo.
**En el proyecto:** `tenantId`, `userId` y `requestId` aparecen juntos en muchos Handlers. Se agrupan en un `RequestContext` record inyectado desde el middleware de autenticación.

---

### Dead Code
**Categoría:** Clean Code - Anti-patrón
**Definición:** Código que existe en el repositorio pero nunca se ejecuta: métodos no llamados, ramas `if` inalcanzables, variables declaradas pero no usadas, código comentado. Aumenta la carga cognitiva sin aportar valor. La regla del proyecto es eliminarlo  -  el historial de git lo conserva si se necesita recuperar.
**En el proyecto:** El CLAUDE.md del proyecto prohíbe código comentado y métodos `[Obsolete]` sin fecha de remoción. Las advertencias CS0168 (variable declarada pero no usada) y CA1859 se tratan como errores en el build del CI.

---

### DDD (Domain-Driven Design)
**Categoría:** Arquitectura
**Definición:** Enfoque de diseño que centra el diseño en el dominio del negocio. Introduce conceptos como Bounded Context, Aggregate, Entity, Value Object, Repository y Domain Service.
**En el proyecto:** Las entidades `Conversation`, `Contact`, `Message` y `BotFlow` viven en la capa `Domain` de cada módulo.

---

### Delegate / RequestHandlerDelegate
**Categoría:** C#
**Definición:** Tipo que representa una referencia a un método con una firma determinada. `RequestHandlerDelegate<TResponse>` encapsula la llamada al siguiente elemento de la cadena (el Handler o el siguiente Behavior).
**En el proyecto:** `InteractorPipeline` recibe un `RequestHandlerDelegate<TResponse> next` y lo invoca con `await next()`.

---

### Dependency Injection (DI)
**Categoría:** .NET / Arquitectura
**Definición:** Patrón de diseño donde los objetos reciben sus dependencias desde el exterior. Reduce el acoplamiento y permite intercambiar implementaciones sin modificar el código cliente.
**En el proyecto:** El contenedor de DI registra Handlers, Presenters, Strategies y Repositories.

---

### develop (rama Git)
**Categoría:** Git Flow
**Definición:** Rama permanente en Git Flow que representa el estado de integración continua. Todas las feature branches parten de y regresan a `develop`.
**En el proyecto:** El equipo trabaja con `git checkout develop && git pull origin develop` antes de crear cualquier nueva rama.

---

### Domain layer (capa de dominio)
**Categoría:** Clean Architecture
**Definición:** Capa más interna en Clean Architecture. Contiene entidades, enums, interfaces de repositorio, value objects y reglas de negocio. No depende de ninguna otra capa.
**En el proyecto:** `Conversations.Domain` contiene `Conversation.cs`, `ConversationStatus.cs`, `IConversationRepository.cs` e `IConversationAssignmentStrategy.cs`.

---

## E

### Early return
**Categoría:** Clean Code
**Definición:** Técnica que consiste en retornar del método tan pronto como se detecta una condición de fallo o caso especial, evitando anidar lógica en bloques `else`.
**En el proyecto:** Los Handlers del back-template usan early return: si la conversación no existe, retornan `NotFoundFailure` inmediatamente.

---

### EF Core (Entity Framework Core)
**Categoría:** ORM / Infrastructure
**Definición:** ORM oficial de Microsoft para .NET. Permite interactuar con bases de datos relacionales usando objetos C#. Soporta LINQ, migraciones y change tracking.
**En el proyecto:** Se usa como ORM principal para PostgreSQL. Los repositorios de `Infrastructure` usan `AppDbContext` de forma interna.

---

### Emisor (sender)
**Categoría:** Patrón de diseño
**Definición:** En el patrón Command y Mediator, el objeto que inicia la solicitud sin saber cómo se ejecutará. No tiene referencia directa al receptor.
**En el proyecto:** El `ConversationsController` es el emisor: construye un `AssignConversationRequest` y lo pasa al Mediator.

---

### EnumeratorCancellation
**Categoría:** C# / async
**Definición:** Atributo que se aplica al parámetro `CancellationToken` de un método `IAsyncEnumerable<T>`. Permite que el token de cancelación se propague correctamente en el sitio de consumo.
**En el proyecto:** Se usa en `StreamByWorkspaceAsync([EnumeratorCancellation] CancellationToken ct)`.

---

## F

### Factory (fábrica)
**Categoría:** Patrón de diseño
**Definición:** Patrón creacional que encapsula la lógica de creación de objetos. Una Factory selecciona y devuelve la Strategy correcta en tiempo de ejecución según el contexto.
**En el proyecto:** Una alternativa externa al Strategy DI es una Factory: `_strategyFactory.GetStrategy(workspace.AssignmentMode)`.

---

### Feature branch
**Categoría:** Git Flow
**Definición:** Rama de vida corta creada desde `develop` para implementar una funcionalidad específica. Se fusiona de regreso mediante un Pull Request.
**En el proyecto:** Convención de nombres: `feature/s05-behavioral-patterns`, `feature/crm-assign-conversation`.

---

### Feature Envy
**Categoría:** Clean Code - Code Smell
**Definición:** Code smell donde un método usa más datos y métodos de otra clase que de la clase a la que pertenece, sugiriendo que debería estar en esa otra clase. Viola el principio de cohesión.
**En el proyecto:** Si un método del `ConversationHandler` accede extensivamente a propiedades de `Contact`, es candidato a moverse al dominio de `Contact` o a un servicio de dominio compartido.

---

### Flujo de control (control flow)
**Categoría:** Programación
**Definición:** Orden en que se ejecutan las instrucciones de un programa. Los patrones de comportamiento se ocupan de distribuir el flujo de control entre múltiples objetos.
**En el proyecto:** El flujo `Controller -> Mediator -> Pipeline -> Handler -> Publish -> Presenters` es el estándar del back-template.

---

## G

### Gang of Four (GoF)
**Categoría:** Diseño de software
**Definición:** Autores del libro "Design Patterns" (1994). Clasificaron 23 patrones de diseño en tres categorías: creacionales, estructurales y de comportamiento.
**En el proyecto:** Los 10 patrones de comportamiento de S05 provienen directamente de la clasificación GoF.

---

### GetRequiredService
**Categoría:** .NET / DI
**Definición:** Método del contenedor de DI que resuelve un servicio registrado y lanza una excepción si no está registrado, fallando explícitamente cuando falta una dependencia.
**En el proyecto:** Se usa en el registro de Presenters en `ServiceCollectionEx.cs`.

---

### GetServices (plural)
**Categoría:** .NET / DI
**Definición:** Método que resuelve todas las implementaciones registradas para un tipo dado como `IEnumerable<T>`. Permite que el Mediator descubra todos los `INotificationHandler<T>` registrados.
**En el proyecto:** `_serviceProvider.GetServices<INotificationHandler<TNotification>>()` en `Mediator.Publish()`.

---

### Git Flow
**Categoría:** Git / Proceso
**Definición:** Modelo de ramificación para Git que define cinco tipos de ramas con responsabilidades claras: `main`, `develop`, `feature/*`, `release/*` y `hotfix/*`.
**En el proyecto:** El equipo adopta Git Flow desde S05. La rama `feature/s05-behavioral-patterns` es un ejemplo de la convención de nombres.

---

### God Class
**Categoría:** Clean Code - Code Smell
**Definición:** Code smell donde una clase acumula demasiadas responsabilidades y conoce demasiado sobre el sistema. Síntoma: archivo de más de 300-500 líneas con dependencias en todas direcciones. Viola el Single Responsibility Principle. Se refactoriza extrayendo clases por responsabilidad.
**En el proyecto:** El `ContactsController` original del equipo tenía validación, lógica de negocio, acceso a DB y formateo de respuestas en una sola clase. Se refactorizó al patrón Handler + Presenter para eliminar la God Class.

---

### Guard Clause
**Categoría:** Clean Code - Patrones
**Definición:** Validación al inicio de un método que retorna (o lanza excepción) inmediatamente si una condición inválida se cumple. Elimina el anidamiento profundo (Arrow Code) y deja la lógica principal en el nivel más superficial del método. Principio: "valida y sal pronto". Evita el anidamiento excesivo de `if/else`.
**En el proyecto:** Todos los Handlers del CRM usan Guard Clauses. Si el repositorio devuelve `null`, el Handler retorna inmediatamente: `if (conversation is null) return new AssignConversationNotFoundFailure(...)`, en lugar de continuar en un bloque `else` anidado.

---

## H

### Handler (IRequestHandler)
**Categoría:** Back-template / Patrón Command
**Definición:** Clase que implementa `IRequestHandler` y contiene la lógica de negocio para procesar un Command o Query específico. Solo existe un Handler por tipo de Request.
**En el proyecto:** `AssignConversationHandler` es el único receptor de `AssignConversationRequest`. Consulta el repositorio y aplica la Strategy.

---

### hardcoded (valor hardcodeado)
**Categoría:** Clean Code / Anti-patrón
**Definición:** Valor o lógica embebida directamente en el código fuente sin posibilidad de configuración externa. Impide la reutilización y dificulta el mantenimiento.
**En el proyecto:** Usar `if (algo == "roundrobin")` en el Handler es un antipatrón de Strategy; la solución es inyectar la interfaz por DI.

---

### hotfix branch
**Categoría:** Git Flow
**Definición:** Rama de vida corta que parte directamente de `main` para corregir un error crítico en producción. Se fusiona tanto en `main` como en `develop`.
**En el proyecto:** Si el CRM en producción tiene un bug crítico, se crea `hotfix/crm-fix-assign-logic` desde `main`.

---

### HTTP status code
**Categoría:** HTTP / API
**Definición:** Código numérico que indica el resultado de una petición HTTP. Comunes: `200 OK`, `201 Created`, `400 Bad Request`, `404 Not Found`, `500 Error`.
**En el proyecto:** El Controller mapea el tipo del Response a un status code: si `_viewModel.IsSuccess` -> `200 OK`; si hay un `INotFoundFailure` -> `404 Not Found`.

---

## I

### IAsyncEnumerable\<T\>
**Categoría:** C# / async
**Definición:** Interfaz que representa una secuencia asíncrona de elementos producidos uno a uno. El caller la consume con `await foreach`. Permite procesar grandes volúmenes de datos.
**En el proyecto:** `ContactRepository.StreamByWorkspaceAsync()` retorna esta interfaz para transmitir contactos desde PostgreSQL sin cargar la lista completa.

---

### IConflictFailure
**Categoría:** Back-template / Result Pattern
**Definición:** Interfaz que marca un resultado de fallo por conflicto (duplicado o violación de unicidad). El Presenter detecta este tipo y devuelve `409 Conflict`.
**En el proyecto:** Si se intenta crear un contacto con un email que ya existe en el workspace, el Handler retorna un record que implementa esta interfaz.

---

### IConversationAssignmentStrategy
**Categoría:** Back-template / Strategy
**Definición:** Interfaz del dominio que define el contrato para los algoritmos de asignación de conversaciones. Las implementaciones concretas viven en Infrastructure.
**En el proyecto:** Definida en `Conversations.Domain/Services/`. Implementada por `RoundRobinAssignmentStrategy` y `LeastLoadAssignmentStrategy`.

---

### IFailure
**Categoría:** Back-template / Result Pattern
**Definición:** Interfaz del Result Pattern que marca cualquier resultado de fallo. Tipos más específicos como `INotFoundFailure` heredan de ella.
**En el proyecto:** En el Presenter, `IFailure` va siempre en el primer `if` del pattern matching para capturar todos los fallos.

---

### IMediator
**Categoría:** Back-template / Common.Messaging
**Definición:** Interfaz del Mediator que expone `Send<TResponse>` para Commands/Queries y `Publish<TNotification>` para notificaciones. Centraliza la comunicación entre capas.
**En el proyecto:** Inyectado en `BaseApiController` y en `InteractorPipeline`. El Controller lo usa para despachar Commands.

---

### INotFoundFailure
**Categoría:** Back-template / Result Pattern
**Definición:** Interfaz que marca un resultado de fallo porque el recurso solicitado no existe. El Presenter devuelve `404 Not Found` cuando detecta este tipo.
**En el proyecto:** `AssignConversationNotFoundFailure` implementa esta interfaz y se retorna cuando no se encuentra la conversación en la BD.

---

### INotification
**Categoría:** Back-template / Common.Messaging
**Definición:** Interfaz marcadora que deben implementar los tipos que se publican a través del Mediator. Los `IResponse` también la implementan para ser distribuidos a los Presenters.
**En el proyecto:** `AssignConversationResponse` implementa `IResponse` que a su vez implementa `INotification`.

---

### INotificationHandler\<TNotification\>
**Categoría:** Back-template / Observer
**Definición:** Interfaz del patrón Observer. Los Presenters la implementan para suscribirse a una notificación. El Mediator los notifica automáticamente mediante `Publish()`.
**En el proyecto:** `AssignConversationPresenter` y `AssignConversationAuditPresenter` implementan esta interfaz para recibir la notificación en paralelo.

---

### InteractorPipeline
**Categoría:** Back-template / Chain of Responsibility
**Definición:** Implementación que actúa como el nodo central de la cadena. Loguea el request, delega al Handler real y publica el response a los Presenters.
**En el proyecto:** Es el Behavior registrado globalmente. Sin él, los Presenters nunca serían notificados del resultado del Handler.

---

### InvalidOperationException
**Categoría:** C# / .NET
**Definición:** Excepción que indica que se intentó realizar una operación inválida dado el estado actual. En el Mediator, se lanza si no hay un Handler registrado para un Command.
**En el proyecto:** `Mediator.Send()` lanza esta excepción si no encuentra un Handler en el DI container.

---

### IPipelineBehavior\<TRequest, TResponse\>
**Categoría:** Back-template / Chain of Responsibility
**Definición:** Interfaz que deben implementar los nodos de la cadena de responsabilidad del Mediator. Puede ejecutar lógica antes y después del Handler.
**En el proyecto:** `InteractorPipeline` implementa esta interfaz. Se pueden agregar más behaviors (validación, autorización) registrándolos en el DI container.

---

### IRequest\<TResponse\>
**Categoría:** Back-template / Command
**Definición:** Interfaz marcadora del patrón Command. Cualquier `sealed record` que la implemente es un Command o Query. El tipo genérico indica el resultado que producirá su Handler.
**En el proyecto:** `AssignConversationRequest : IRequest<AssignConversationResponse>`.

---

### IRequestHandler\<TRequest, TResponse\>
**Categoría:** Back-template / Command
**Definición:** Interfaz que deben implementar los Handlers. El método `Handle()` contiene la lógica de negocio. El DI container registra una implementación por tipo de Request.
**En el proyecto:** `AssignConversationHandler : IRequestHandler<AssignConversationRequest, AssignConversationResponse>`.

---

### IResponse
**Categoría:** Back-template / Result Pattern
**Definición:** Interfaz base para todos los tipos de resultado que produce un Handler. Implementa `INotification` para poder ser publicado a los Presenters.
**En el proyecto:** Todos los `abstract record` de Response del sistema implementan `IResponse`.

---

### ISuccess\<T\>
**Categoría:** Back-template / Result Pattern
**Definición:** Interfaz genérica que marca un resultado exitoso y proporciona el valor producido. `T` es el tipo del dato devuelto (ej. `Guid` del recurso creado).
**En el proyecto:** `AssignConversationSuccess(Guid ConversationId) : AssignConversationResponse, ISuccess<Guid>`.

---

### IsSuccess / IsFailure
**Categoría:** Back-template / Result Pattern
**Definición:** Propiedades booleanas del `ResultViewModel<T>` que indican si el resultado fue exitoso o fallido. El Controller las usa para decidir el status code HTTP.
**En el proyecto:** `return _viewModel.IsSuccess ? Ok(_viewModel) : NotFound(_viewModel)` en el Controller.

---

### Iterator (patrón)
**Categoría:** Patrón de diseño
**Definición:** Patrón de comportamiento que proporciona acceso secuencial a los elementos de una colección sin exponer su representación interna. `IEnumerable` es una implementación.
**En el proyecto:** `IAsyncEnumerable<Contact>` en `ContactRepository.StreamByWorkspaceAsync()` es el Iterator del CRM.

---

## J

### JsonExportVisitor
**Categoría:** Patrón de diseño / Dominio
**Definición:** Implementación del patrón Visitor que exporta contactos al formato JSON. Encapsula la lógica de serialización fuera de la entidad `Contact`.
**En el proyecto:** Se implementará más adelante. El objeto `Contact` llamará a `contact.Accept(new JsonExportVisitor())`.

---

## L

### Law of Demeter (LoD)
**Categoría:** Clean Code / Principios
**Definición:** Principio de mínimo conocimiento: un objeto solo debe comunicarse con sus dependencias directas. Reduce el acoplamiento y evita cadenas largas de llamadas.
**En el proyecto:** El Controller no debe acceder a dependencias internas del Mediator. Solo conoce la interfaz `IMediator`.

---

### LeastLoadAssignmentStrategy
**Categoría:** Back-template / Strategy
**Definición:** Implementación que selecciona el agente con menor número de conversaciones abiertas. Usa una subconsulta SQL que cuenta conversaciones activas por agente.
**En el proyecto:** Alternativa a Round Robin. Se registra en `ServiceCollectionEx.cs` como la implementación de `IConversationAssignmentStrategy`.

---

### Lifetime (DI lifetime)
**Categoría:** .NET / DI
**Definición:** Define cuánto tiempo vive una instancia en el contenedor. `Transient` (nueva cada vez), `Scoped` (una por request) y `Singleton` (una por toda la vida de la app).
**En el proyecto:** Handlers y Presenters se registran como `Scoped` porque acceden al `AppDbContext` que también es `Scoped`.

---

### LINQ (Language Integrated Query)
**Categoría:** C#
**Definición:** Métodos de extensión que permiten consultar fuentes de datos con sintaxis uniforme integrada en C#. EF Core traduce estas consultas a SQL.
**En el proyecto:** EF Core traduce consultas LINQ como `.Where(c => c.TenantId == tenantId)` a SQL de PostgreSQL.

---

### Logger / ILogger\<T\>
**Categoría:** .NET / Serilog
**Definición:** Interfaz de logging de .NET que provee métodos estructurados (`LogInformation`, `LogError`). El tipo genérico `T` se usa como categoría para identificar la fuente.
**En el proyecto:** `AssignConversationAuditPresenter` usa `ILogger<AssignConversationAuditPresenter>` para registrar cada asignación exitosa.

---

### LogInformation
**Categoría:** .NET / Serilog
**Definición:** Método de logging de nivel `Information`. En `InteractorPipeline` se usa con el operador `@` para serializar el objeto request completo como JSON.
**En el proyecto:** `Logger.LogInformation("{@Request}", request)` en `InteractorPipeline` captura todos los parámetros del Command.

---

### Long Method
**Categoría:** Clean Code - Code Smell
**Definición:** Code smell donde una función o método es demasiado largo (generalmente más de 20-25 líneas). Los métodos largos son difíciles de entender, testear y modificar. La solución es extraer partes a métodos privados con nombres descriptivos.
**En el proyecto:** Métrica de referencia del proyecto: ningún método público debe superar 20 líneas; ningún método privado debe superar 15. Los PRs con métodos más largos reciben comentario de refactorización en code review.

---

## M

### Magic Number
**Categoría:** Clean Code - Anti-patrón
**Definición:** Número literal hardcodeado en el código sin explicación de su significado. Hace el código difícil de entender. Ejemplo: `if (messages.Count > 100)`  -  no está claro por qué 100 ni qué representa. La solución es extraerlo como constante con nombre: `const int MaxMessagesBeforeArchiving = 100`.
**En el proyecto:** El linter del proyecto (Roslyn analyzers) detecta magic numbers en condiciones y asignaciones. Los únicos literales permitidos sin constante son 0 y 1 en contextos evidentes. En una query EF Core, un `.Take(20)` también es un magic number; debe ser `const int MaxContactsPerPage = 20`.

---

### MakeGenericType
**Categoría:** C# / Reflection
**Definición:** Método de reflection que construye un tipo genérico en tiempo de ejecución. Permite crear tipos cerrados sin conocerlos en tiempo de compilación.
**En el proyecto:** El `Mediator.Send()` lo usa para resolver el Handler correcto del DI container a partir del tipo del request recibido.

---

### main (rama Git)
**Categoría:** Git Flow
**Definición:** Rama permanente que contiene el código actualmente en producción. Solo recibe merges de `release/*` y `hotfix/*`.
**En el proyecto:** El pipeline CI/CD del CRM solo despliega a producción cuando se hace merge a `main`. Cada commit tiene un tag SemVer.

---

### meaningful name (nombre significativo)
**Categoría:** Clean Code
**Definición:** Principio donde los nombres deben revelar la intención sin necesidad de comentarios. Un buen nombre responde a "¿qué hace?" o "¿qué representa?".
**En el proyecto:** `_conversations.GetByPublicIdAsync(...)` es un nombre significativo. `_repo.Get(id)` no lo es.

---

### MediatR (NuGet)
**Categoría:** Librería externa / .NET
**Definición:** Librería que implementa el patrón Mediator. Provee interfaces para Requests y Handlers. El back-template implementa su propia versión en `Common.Messaging`.
**En el proyecto:** La mecánica en el back-template es idéntica a MediatR, lo que facilita que el equipo consulte documentación externa.

---

### Mediator pattern (patrón Mediator)
**Categoría:** Patrón de diseño
**Definición:** Patrón de comportamiento que centraliza la comunicación entre múltiples objetos en un objeto mediador. Reduce el acoplamiento O(n²) a O(n).
**En el proyecto:** `IMediator` coordina Controllers, Handlers y Presenters. El Controller nunca crea ni conoce directamente un Handler.

---

### Memento pattern (patrón Memento)
**Categoría:** Patrón de diseño
**Definición:** Patrón que captura el estado interno de un objeto en un objeto externo sin violar la encapsulación. Permite restaurar el estado posteriormente (Undo/Redo).
**En el proyecto:** Se aplicará más adelante con `BotFlow.CreateSnapshot()` para permitir revertir cambios si el usuario cancela la edición del flujo.

---

### merge (Git)
**Categoría:** Git
**Definición:** Operación de Git que integra los cambios de una rama en otra. En Git Flow, sigue flujos definidos como `feature/*` -> `develop`.
**En el proyecto:** Al terminar una funcionalidad, se crea un PR y el merge sucede tras la aprobación del code review.

---

### middleware
**Categoría:** ASP.NET Core
**Definición:** Componente de la pipeline de ASP.NET Core que procesa requests y responses HTTP. Similar conceptualmente al Chain of Responsibility.
**En el proyecto:** Los `IPipelineBehavior` del back-template son el equivalente de middlewares pero a nivel de la capa de aplicación.

---

### Modular Monolith (Monolito Modular)
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que organiza el código en módulos bien delimitados dentro de un único proceso desplegable. Cada módulo tiene su propio dominio.
**En el proyecto:** El back-template sigue esta arquitectura: módulos `Conversations`, `Contacts`, etc., son proyectos separados que se compilan juntos.

---

## N

### namespace
**Categoría:** C#
**Definición:** Mecanismo de organización de tipos que evita colisiones de nombres. Siguen la convención `[Módulo].[Capa].[SubCarpeta]`.
**En el proyecto:** Los namespaces comunican la ubicación y responsabilidad: `Conversations.Application.UseCases.AssignConversation`.

---

### N+1 (problema N+1)
**Categoría:** EF Core / Performance
**Definición:** Antipatrón de acceso a datos donde se ejecuta una consulta para N entidades y luego N consultas adicionales para sus relaciones. Se resuelve con `Include()`.
**En el proyecto:** Cargar conversaciones e iterar pidiendo el agente por cada una sería N+1. La solución es un `JOIN` o proyección con `Select`.

---

### nit (en code review)
**Categoría:** Proceso / Code Review
**Definición:** Comentario de bajo impacto (estético o de preferencia) que no bloquea la aprobación del PR. El autor puede decidir si lo aplica.
**En el proyecto:** Un nombre de variable ligeramente menos descriptivo sería un `[nit]`. Un error de lógica sería `[bloqueante]`.

---

### null (nullable / Guid?)
**Categoría:** C#
**Definición:** Valor que indica ausencia de valor. `Guid?` puede ser null. En C# moderno, la nulidad debe ser explícita mediante nullable reference types.
**En el proyecto:** `Conversation.AgentId` es `Guid?` porque una conversación nueva no tiene agente. `AssignAsync()` retorna `Guid?` por si no hay agentes.

---

### Null Object pattern
**Categoría:** Patrón de diseño
**Definición:** Patrón que provee un objeto vacío en lugar de retornar `null`. Elimina cheques de nulidad en el código cliente.
**En el proyecto:** Relevante al diseñar `IConversationAssignmentStrategy`: se podría tener un `NoAgentAvailableStrategy` que retorne el fallo apropiado.

---

## O

### Observer pattern (patrón Observer)
**Categoría:** Patrón de diseño
**Definición:** Patrón donde un objeto (publisher) notifica a una lista de dependientes (subscribers) cuando su estado cambia. El publisher solo conoce la interfaz.
**En el proyecto:** `InteractorPipeline` es el publisher que llama a `Mediator.Publish()`. Los Presenters son los observadores que reaccionan al resultado.

---

### Open (estado)
**Categoría:** Dominio  -  Sistema de mensajería
**Definición:** Valor inicial del enum `ConversationStatus`. Una conversación recién creada entra en este estado. Puede transicionar a `Assigned` o `Archived`.
**En el proyecto:** `Status { get; private set; } = ConversationStatus.Open` es el valor por defecto. Solo desde aquí se puede llamar a `Assign()`.

---

### ORM (Object-Relational Mapper)
**Categoría:** Base de datos
**Definición:** Librería que mapea entre objetos C# y tablas relacionales. Elimina la necesidad de escribir SQL crudo para operaciones comunes.
**En el proyecto:** El back-template usa EF Core como ORM principal y Dapper para consultas de lectura complejas.

---

## P

### paginate / paginación
**Categoría:** API Design / Performance
**Definición:** Técnica para dividir resultados grandes en páginas de tamaño fijo. Evita cargar miles de registros en una sola respuesta.
**En el proyecto:** La bandeja de conversaciones usará paginación cursor-based más adelante para manejar miles de conversaciones activas.

---

### pattern matching (coincidencia de patrones)
**Categoría:** C#
**Definición:** Característica de C# que permite comprobar el tipo y estructura de un valor con la sintaxis `is`. Más seguro que los cast explícitos.
**En el proyecto:** En el Presenter, `if (notification is IFailure failure)` se usa para distinguir los casos de resultado.

---

### pipeline
**Categoría:** Arquitectura / Patrones
**Definición:** Secuencia de etapas de procesamiento donde la salida de una es la entrada de la siguiente. Aplica tanto a código como a CI/CD.
**En el proyecto:** El flujo `InteractorPipeline -> Handler -> Publish -> Presenters` es la pipeline de procesamiento del CRM.

---

### PostgreSQL
**Categoría:** Base de datos
**Definición:** Motor de base de datos relacional de alto rendimiento. Soporta tipos avanzados (JSONB, UUID) y es la base del stack del sistema de mensajería.
**En el proyecto:** EF Core y Dapper se conectan a PostgreSQL. Las queries de asignación se traducen a SQL nativo de PostgreSQL.

---

### PR (Pull Request)
**Categoría:** Git / Proceso
**Definición:** Solicitud de fusión de una rama en otra. Es el mecanismo estándar para el code review: el autor propone y los reviewers inspeccionan.
**En el proyecto:** Cada feature branch se integra a `develop` mediante un PR, tras revisar lógica, seguridad, legibilidad y performance.

---

### Presenter
**Categoría:** Back-template / Observer
**Definición:** Clase que implementa `INotificationHandler` y actúa como observador. Traduce el resultado de dominio a una representación apta para HTTP en el ViewModel.
**En el proyecto:** `AssignConversationPresenter` recibe la respuesta, distingue Success de Failure y actualiza el `ResultViewModel`.

---

### primary constructor (constructor primario)
**Categoría:** C#
**Definición:** Característica de C# 12 que permite declarar parámetros del constructor directamente en la declaración de la clase o record.
**En el proyecto:** `InteractorPipeline<TRequest, TResponse>(IMediator Mediator, ILogger Logger)` usa este tipo de constructor para inyectar dependencias.

---

### Primitive Obsession
**Categoría:** Clean Code - Code Smell
**Definición:** Code smell donde se usan tipos primitivos (`string`, `int`, `bool`) para representar conceptos de dominio que merecen su propio tipo. Ejemplo: usar `string phoneNumber` en lugar de un Value Object `PhoneNumber` que valida el formato. Lleva a validaciones duplicadas y a bugs difíciles de rastrear.
**En el proyecto:** `Email`, `PhoneNumber` y `Money` son Value Objects en el dominio del CRM, no strings primitivos. El compilador garantiza que no se puede asignar un string sin validar donde se espera un `Email`.

---

### PublicId (Guid)
**Categoría:** Dominio  -  Sistema de mensajería
**Definición:** Identificador externo de una entidad expuesto en la API. Previene enumeration attacks al no usar el ID secuencial interno de la BD.
**En el proyecto:** `Conversation.PublicId` aparece en las URLs de la API (`/api/conversations/{publicId}/assign`).

---

## Q

### QueryFirstOrDefaultAsync (Dapper)
**Categoría:** Dapper / SQL
**Definición:** Método de Dapper que ejecuta una query SQL y devuelve el primer resultado como objeto tipado o `null`. Alternativa eficiente a EF Core.
**En el proyecto:** La implementación del repositorio podría usarlo para buscar una conversación por su ID público de forma rápida.

---

## R

### Receiver (receptor)
**Categoría:** Patrón de diseño
**Definición:** En el patrón Command, el objeto que sabe cómo ejecutar la operación. Tiene la lógica real de negocio y no conoce al emisor.
**En el proyecto:** `AssignConversationHandler` es el receptor del Command. Sabe cómo asignar; el Controller no necesita saberlo.

---

### record (C#)
**Categoría:** C#
**Definición:** Tipo de referencia con igualdad basada en valores. Genera automáticamente `ToString` y `==`. Son inmutables por diseño.
**En el proyecto:** `AssignConversationRequest` es un `sealed record`. Son ideales para Commands y Responses por ser inmutables.

---

### Refactoring
**Categoría:** Clean Code - Ingeniería de Software
**Definición:** Proceso de restructurar código existente sin cambiar su comportamiento observable externo. Objetivo: mejorar legibilidad, reducir complejidad, eliminar duplicación. Se hace en pasos pequeños con cobertura de tests que verifiquen que el comportamiento no cambió. Martin Fowler catalogó más de 60 técnicas de refactoring.
**En el proyecto:** El 20% de cada sprint se dedica a refactoring de deuda técnica. Los refactorings se rastrean como tickets `TD-XX` en el backlog y se fusionan en PRs separados de las features para mantener historial limpio.

---

### Reflection
**Categoría:** C# / .NET
**Definición:** Capacidad de C# de inspeccionar y manipular tipos en tiempo de ejecución. Permite crear instancias de tipos desconocidos en compilación.
**En el proyecto:** `Mediator.Send()` usa reflection para resolver el Handler correcto sin conocer el tipo del request de antemano.

---

### release branch
**Categoría:** Git Flow
**Definición:** Rama temporal creada desde `develop` para lanzar una versión. Solo se permiten bugfixes menores y el incremento de versión.
**En el proyecto:** Cuando el CRM tenga su versión 1.0 lista, se creará `release/1.0.0` para ajustes finales.

---

### Repository (repositorio)
**Categoría:** Patrón de diseño / DDD
**Definición:** Patrón que abstrae el acceso a datos detrás de una interfaz de dominio. La infraestructura provee la implementación concreta.
**En el proyecto:** `IConversationRepository` está en `Conversations.Domain`. Su implementación con EF Core vive en `Infrastructure`.

---

### Resolved (estado)
**Categoría:** Dominio  -  Sistema de mensajería
**Definición:** Valor del enum que indica que la conversación fue resuelta por el agente. Solo puede alcanzarse desde `Assigned`.
**En el proyecto:** `conversation.Resolve()` solo funciona si el estado es `Assigned`. Si no, retorna un `Result.Fail` con el mensaje de error.

---

### Result Pattern
**Categoría:** Back-template / Arquitectura
**Definición:** Patrón que representa el resultado de una operación como un objeto tipado (Success/Failure) en lugar de usar excepciones para flujo de control.
**En el proyecto:** `Conversation.Assign()` retorna `Result`. El Controller nunca hace try/catch para lógica de negocio.

---

### ResultViewModel\<T\>
**Categoría:** Back-template / Presenter
**Definición:** Clase que actúa como "pizarrón compartido" entre el Presenter y el Controller. El Presenter escribe y el Controller lee para decidir la respuesta.
**En el proyecto:** Registrada como `Scoped`. El Presenter llama a `_viewModel.Fail()` o `Set()`.

---

### Round Robin
**Categoría:** Algoritmo
**Definición:** Algoritmo que asigna recursos en orden circular, de forma equitativa. En el CRM, asigna cada conversación nueva al siguiente agente en la lista.
**En el proyecto:** `RoundRobinAssignmentStrategy` ordena los agentes por `LastAssignedAt` ascendente y selecciona al que lleva más tiempo sin recibir una.

---

### RoundRobinAssignmentStrategy
**Categoría:** Back-template / Strategy
**Definición:** Implementación que asigna conversaciones usando Round Robin. Es la estrategia por defecto del CRM. Vive en `Conversations.Infrastructure`.
**En el proyecto:** Se registra mediante DI. Cambiarla a otra estrategia requiere modificar solo una línea en el registro de servicios.

---

## S

### sealed (C#)
**Categoría:** C#
**Definición:** Modificador que impide que una clase sea heredada. Mejora el rendimiento y comunica que la implementación es final.
**En el proyecto:** Handlers y Presenters son `sealed` porque no están diseñados para ser heredados.

---

### SemVer (Semantic Versioning)
**Categoría:** Git / DevOps
**Definición:** Convención de versionado `MAJOR.MINOR.PATCH`. `MAJOR` por breaking changes, `MINOR` por features y `PATCH` por bugfixes.
**En el proyecto:** Los tags de Git en `main` siguen SemVer: `v1.0.0`, `v1.1.0`. Una feature nueva es un `MINOR bump`.

---

### ServiceCollectionEx (ServiceCollection Extensions)
**Categoría:** Back-template / DI
**Definición:** Clase de extensión que agrupa el registro de servicios de una capa. Sigue la convención de encapsular la configuración de DI.
**En el proyecto:** `Conversations.Presentation/ServiceCollectionEx.cs` registra los Presenters del módulo.

---

### Single Responsibility Principle (SRP)
**Categoría:** SOLID / Principios
**Definición:** Principio donde una clase debe tener una sola razón para cambiar. Cada clase debe encapsular una única responsabilidad.
**En el proyecto:** El Handler solo ejecuta lógica de negocio; el Presenter solo traduce resultados; el Controller solo despacha al Mediator.

---

### snapshot (Memento)
**Categoría:** Patrón de diseño
**Definición:** Copia del estado interno de un objeto en un momento dado. Es opaca para el exterior y permite restaurar el objeto original.
**En el proyecto:** `botFlow.CreateSnapshot()` captura nodos y conexiones. Permite revertir cambios si el usuario cancela la edición.

---

### Stakeholder
**Categoría:** Gestión de producto
**Definición:** Persona o grupo con interés o influencia sobre el producto. Incluye usuarios, sponsors, equipo de desarrollo y operaciones.
**En el proyecto:** La empresa tiene stakeholders como el PO (fechas/valor) y el CTO (escalabilidad/deuda).

---

### State pattern (patrón State)
**Categoría:** Patrón de diseño
**Definición:** Patrón donde el comportamiento de un objeto cambia según su estado interno. Encapsula las transiciones en la entidad o clases de estado.
**En el proyecto:** La entidad `Conversation` + métodos `Assign()`, `Resolve()` es una versión "ligera" del State Pattern.

---

### Strategy pattern (patrón Strategy)
**Categoría:** Patrón de diseño
**Definición:** Patrón que define una familia de algoritmos y los hace intercambiables. El cliente usa la estrategia a través de una interfaz.
**En el proyecto:** `IConversationAssignmentStrategy` permite intercambiar entre Round Robin y Carga Mínima de forma transparente.

---

### streaming (de datos)
**Categoría:** Performance / EF Core
**Definición:** Técnica que lee y procesa elementos uno a uno en lugar de cargar toda la colección. Reduce el uso de RAM significativamente.
**En el proyecto:** `StreamByWorkspaceAsync()` en el repositorio de contactos transmite datos desde PostgreSQL usando streaming.

---

### Subject (sujeto/publisher)
**Categoría:** Patrón de diseño / Observer
**Definición:** En el patrón Observer, el objeto que mantiene el estado y notifica a los observadores. No conoce los tipos concretos de los mismos.
**En el proyecto:** `InteractorPipeline` es el sujeto que llama a `Mediator.Publish(response)` tras ejecutar el Handler.

---

## T

### Task.WhenAll
**Categoría:** C# / async
**Definición:** Método que ejecuta múltiples Tasks en paralelo y espera a que todas completen. Propaga excepciones si alguna falla.
**En el proyecto:** `Mediator.Publish()` lo usa para notificar a múltiples Presenters simultáneamente, mejorando el tiempo de respuesta.

---

### Template Method pattern (patrón Template Method)
**Categoría:** Patrón de diseño
**Definición:** Patrón donde la clase base define el esqueleto de un algoritmo y las subclases sobreescriben los pasos variables.
**En el proyecto:** `BaseApiController` define el acceso fijo a `IMediator` y los controllers definen sus propios endpoints.

---

### TenantId / CurrentTenantId
**Categoría:** Dominio  -  Sistema de mensajería / Multi-tenancy
**Definición:** Identificador del workspace al que pertenece la operación. Crítico para el aislamiento de datos en arquitecturas multi-tenant.
**En el proyecto:** Se extrae del token JWT en el Controller base y se incluye en todos los Commands para filtrar datos correctamente.

---

### trade-off
**Categoría:** Arquitectura / Toma de decisiones
**Definición:** Intercambio o compensación: obtener una ventaja técnica implica ceder o aceptar una desventaja en otro aspecto.
**En el proyecto:** Usar Strategy Pattern tiene el trade-off de agregar un archivo por estrategia a cambio de flexibilidad total.

---

### Transient (lifetime DI)
**Categoría:** .NET / DI
**Definición:** Lifetime donde se crea una nueva instancia cada vez que se solicita. Adecuado para servicios ligeros y sin estado.
**En el proyecto:** Los Behaviors podrían registrarse como Transient, mientras que los Handlers suelen ser Scoped por el DbContext.

---

## U

### UpdatedAtUtc
**Categoría:** Dominio  -  Sistema de mensajería
**Definición:** Propiedad que registra la última modificación en UTC. Se actualiza automáticamente en cada método de transición de estado.
**En el proyecto:** Se llama en `Assign()`, `Resolve()`, etc., para permitir auditar cuándo ocurrió cada cambio de estado.

---

### Unit Test
**Categoría:** Testing
**Definición:** Prueba automática que verifica el comportamiento de una unidad aislada (clase/método) reemplazando dependencias reales por mocks.
**En el proyecto:** Los Handlers y las transiciones de estado de las entidades son ideales para ser validados con unit tests.

---

## V

### Visitor pattern (patrón Visitor)
**Categoría:** Patrón de diseño
**Definición:** Patrón que permite agregar operaciones a una jerarquía de clases sin modificarlas. Las clases aceptan un "visitante" (`Accept`).
**En el proyecto:** Se aplicará más adelante para exportar contactos en distintos formatos (JSON, CSV) sin modificar la entidad `Contact`.

---

### ViewModel / ResultViewModel
**Categoría:** Back-template / Presentation
**Definición:** Objeto que representa los datos enviados al cliente. En el back-template, sirve como canal entre Presenter y Controller.
**En el proyecto:** `ResultViewModel<ConversationsController>` es el tipo concreto usado. Se inyecta como `Scoped` para durar lo que dure el request.

---

## W

### where (constraint genérico)
**Categoría:** C#
**Definición:** Cláusula que restringe los tipos que pueden usarse como argumentos genéricos. Garantiza que el tipo implemente una interfaz.
**En el proyecto:** `InteractorPipeline<TRequest, TResponse>` usa `where` para asegurar type-safety en la cadena de mensajes.

---

### WithCancellation
**Categoría:** C# / async
**Definición:** Método que asocia un `CancellationToken` a un `IAsyncEnumerable<T>`. Permite que la iteración se detenga limpiamente si se cancela.
**En el proyecto:** Se usa en `StreamByWorkspaceAsync()` para que si el request HTTP se cancela, la lectura de la BD también se detenga.

---

### workspace
**Categoría:** Dominio  -  Sistema de mensajería
**Definición:** Unidad de aislamiento de datos que corresponde a la organización cliente. Equivale al "tenant" en arquitecturas multi-tenant.
**En el proyecto:** Cada workspace tiene su propia configuración de asignación. El `tenantId` en la BD garantiza que no haya cruce de datos.

---

## Y

### yield return
**Categoría:** C#
**Definición:** Instrucción que produce un elemento de una secuencia y suspende la ejecución hasta que el caller pide el siguiente.
**En el proyecto:** `StreamByWorkspaceAsync()` lo usa para producir contactos de PostgreSQL uno a uno, habilitando el streaming eficiente.

---

# Fin del Glosario  -  S05
**Rogelio Arriaga  -  Raptor Dev Services**
*Mayo 2026*
