# Glosario Técnico  -  API Design

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### Abstract Record (Response base)
**Categoría:** C# / Patrones del back-template
**Definición:** Clase `record` declarada con la palabra clave `abstract` que sirve como tipo base para todas las variantes de una respuesta (éxito, not-found failure, etc.). No puede instanciarse directamente; obliga a que cada variante concreta sea un tipo propio. Permite hacer pattern matching en el Controller para discriminar resultados sin usar booleanos ni strings.
**En el proyecto:** `GetAgentProfilesResponse` es el abstract record base; `GetAgentProfilesSuccess` y `GetAgentProfileNotFoundFailure` heredan de él. El Controller evalúa `result is GetAgentProfileNotFoundFailure` para decidir entre 404 y 500.

### AddSwaggerWithJwt
**Categoría:** OpenAPI / back-template
**Definición:** Método de extensión propio del back-template que registra Swashbuckle, configura el documento `v1` y añade el esquema de seguridad Bearer JWT. Centraliza toda la configuración de Swagger en un solo lugar. Se invoca desde `Program.cs` como `services.AddSwaggerWithJwt()`.
**En el proyecto:** Expone la UI de Swagger con autenticación JWT habilitada para que el equipo pruebe los endpoints del sistema de mensajería sin necesidad de herramientas externas.

### Anidamiento de URIs (Nested Resources)
**Categoría:** REST / Diseño de URIs
**Definición:** Práctica de representar relaciones entre recursos usando segmentos anidados en la URL, por ejemplo `/api/conversations/abc/messages`. El límite recomendado es dos niveles de anidamiento; más allá las URIs se vuelven ilegibles y difíciles de mantener. Para filtros adicionales se usan query params en lugar de agregar más niveles.
**En el proyecto:** `/api/v1/conversations/{id}/messages` es válido (dos niveles). `/api/workspaces/123/agents/456/conversations/789/messages/012/attachments` es el antipatrón a evitar en el CRM.

### ApplicationBuilderEx
**Categoría:** Middleware / back-template
**Definición:** Clase estática del proyecto `Common` que expone métodos de extensión sobre `IApplicationBuilder`. Concentra el registro de middlewares propios del back-template: `UseCoreProblemDetails`, `UseCorrelationId` y `UseTenantResolution`. Mantiene `Program.cs` limpio y legible.
**En el proyecto:** Se usa en `Host.Api/Program.cs` para activar la cadena de middlewares del CRM antes de mapear los controllers.

### Authorization Header
**Categoría:** HTTP / Seguridad
**Definición:** Header HTTP estándar mediante el cual el cliente envía su credencial al servidor. En APIs REST con JWT, el valor tiene la forma `Bearer <token>`. Si el header está ausente o el token es inválido, el servidor responde 401. Es el mecanismo principal de autenticación en el back-template.
**En el proyecto:** Cada request al sistema de mensajería debe incluir `Authorization: Bearer <jwt>` excepto los endpoints públicos como `/api/v1/auth/login` y `/api/v1/auth/refresh`.

---

## B

### Backwards Compatible Change
**Categoría:** API / Versionado
**Definición:** Cambio en el contrato de una API que no rompe a los clientes existentes. Ejemplos: agregar un campo opcional a la response, agregar un nuevo endpoint, agregar un nuevo valor a un enum. Los clientes que ignoran los campos nuevos continúan funcionando sin modificaciones.
**En el proyecto:** Añadir el campo `avatarUrl` opcional al DTO de `Contact` es backwards compatible: el frontend actual que no lo consume sigue funcionando. No requiere crear `/api/v2/contacts`.

### BaseApiController
**Categoría:** ASP.NET Core / back-template
**Definición:** Clase base que heredan todos los controllers del back-template. Recibe `IMediator` por constructor y lo expone como propiedad protegida. Centraliza la configuración común (atributos, rutas base, acceso al mediador). Los controllers hijos nunca inyectan el mediador directamente.
**En el proyecto:** `AgentsController`, `ContactsController` y todos los controllers del sistema de mensajería heredan de `BaseApiController` para acceder a `Mediator.Send(...)`.

### Bearer Token
**Categoría:** HTTP / Seguridad
**Definición:** Esquema de autenticación HTTP donde el cliente presenta un token opaco (en este caso JWT) en el header `Authorization`. El servidor verifica la firma y extrae los claims del token sin consultar ninguna sesión. Es el esquema que implementa la constraint Stateless de REST.
**En el proyecto:** Cada request autenticado al sistema de mensajería lleva `Authorization: Bearer <jwt-firmado-hs256>`. El middleware de ASP.NET Core valida la firma antes de que llegue al controller.

### Breaking Change
**Categoría:** API / Versionado
**Definición:** Cambio en el contrato de una API que rompe a los clientes existentes y obliga a crear una nueva versión. Ejemplos: renombrar un campo, cambiar el tipo de dato, eliminar un campo, cambiar el status code de una respuesta, o hacer requerido un campo que era opcional.
**En el proyecto:** Renombrar `fullName` a `name` en el DTO de `Agent` es un breaking change. Requiere crear `/api/v2/agents` y mantener `/api/v1/agents` activo durante el período de migración.

### BusinessRuleException
**Categoría:** Dominio / back-template
**Definición:** Excepción personalizada del dominio que representa una violación de reglas de negocio, no un error de programación. El `ProblemDetailsMiddleware` la captura y la convierte en una respuesta 400 con Problem Details, sin exponer el stack trace al cliente.
**En el proyecto:** Lanzar `BusinessRuleException("El canal no tiene webhook configurado")` desde el dominio resulta en un 400 estructurado con `detail` legible para el cliente del CRM.

---

## C

### CamelCase (JSON)
**Categoría:** Naming Conventions / API
**Definición:** Convención de nomenclatura donde las palabras se unen sin espacios y cada palabra interna comienza con mayúscula: `firstName`, `createdAt`, `conversationsCount`. Es el estándar de facto para propiedades en JSON de APIs REST, derivado de la convención JavaScript.
**En el proyecto:** Todos los DTOs y responses del sistema de mensajería usan camelCase en JSON: `publicId`, `isActive`, `assignedTo`. Las propiedades en C# se nombran en PascalCase y la serialización de ASP.NET Core las convierte automáticamente.

### CancellationToken
**Categoría:** C# / ASP.NET Core
**Definición:** Token que permite cancelar operaciones asíncronas cuando el cliente desconecta o cuando expira un timeout. Los handlers y repositories deben recibirlo y pasarlo a todas las llamadas `async` para liberar recursos sin esperar completar la operación.
**En el proyecto:** Todos los endpoints del CRM tienen `CancellationToken ct = default` como último parámetro y lo propagan a `Mediator.Send(request, ct)` y a las llamadas de repositorio.

### Cacheable (Constraint REST)
**Categoría:** REST / Constraints
**Definición:** Una de las seis constraints de REST. Las responses deben indicar explícitamente si pueden cachearse (mediante headers HTTP como `Cache-Control`) o no. Permite que proxies, CDNs y el navegador eviten requests redundantes al servidor.
**En el proyecto:** Las listas de contactos y conversaciones del CRM no se cachean porque cambian frecuentemente. Los assets estáticos del frontend sí llevan `Cache-Control: max-age`.

### Claim (JWT)
**Categoría:** JWT / Seguridad
**Definición:** Par clave-valor dentro del payload de un JWT que transmite información sobre el usuario o sesión. Claims estándar incluyen `sub` (subject), `exp` (expiración), `iat` (issued at). Claims custom se añaden al firmar el token.
**En el proyecto:** El JWT del CRM incluye el claim custom `tenant_id` que `CurrentTenantId` lee con `User.FindFirstValue("tenant_id")`. Sin este claim el filtro de tenant devuelve 0 y el handler no encuentra registros de ningún workspace.

### Clean Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que organiza el código en capas concéntricas donde las dependencias siempre apuntan hacia adentro, hacia el dominio. Las capas habituales son Domain, Application, Infrastructure y Presentation. El dominio no depende de ninguna tecnología específica.
**En el proyecto:** El back-template implementa Clean Architecture. Los namespaces reflejan las capas: `Agents.Domain`, `Agents.Application`, `Agents.Infrastructure`, `Agents.Presentation`.

### Client-Server (Constraint REST)
**Categoría:** REST / Constraints
**Definición:** Una de las seis constraints de REST. Frontend y backend son sistemas independientes que se comunican a través de una interfaz uniforme. Cada uno puede evolucionar de forma independiente sin afectar al otro, siempre que el contrato de la API se mantenga.
**En el proyecto:** El frontend React del CRM y el backend .NET pueden desplegarse, escalarse y modificarse por separado. El equipo de frontend consume la API sin saber si el backend corre en Docker, en AWS o en otro servidor.

### Code on Demand (Constraint REST)
**Categoría:** REST / Constraints
**Definición:** La única constraint opcional de REST. Permite que el servidor envíe código ejecutable al cliente (por ejemplo, JavaScript). Raramente se usa en APIs modernas. Todos los demás constraints son obligatorios para considerar una API como RESTful.
**En el proyecto:** No aplica al sistema de mensajería. El frontend React es una aplicación estática que no recibe lógica ejecutable del servidor.

### Constraint REST
**Categoría:** REST / Arquitectura
**Definición:** Restricción arquitectónica que una API debe cumplir para ser considerada RESTful. Roy Fielding definió seis constraints: Client-Server, Stateless, Cacheable, Uniform Interface, Layered System y Code on Demand (opcional). Cada constraint aporta una propiedad de calidad al sistema.
**En el proyecto:** El sistema de mensajería cumple las cinco constraints obligatorias. El equipo debe conocerlas para justificar decisiones de diseño durante el code review.

### Controller (ASP.NET Core)
**Categoría:** ASP.NET Core / Presentación
**Definición:** Clase que recibe requests HTTP, extrae los datos del request (headers, params, body), los delega al mediador y decide el status code de la response en función del resultado. En el back-template, el Controller no contiene lógica de dominio ni de datos.
**En el proyecto:** `AgentsController`, `ContactsController`, `ConversationsController` son los controllers del CRM. Cada uno hereda de `BaseApiController` y usa `_viewModel` para construir la response.

### CorrelationIdMiddleware
**Categoría:** Middleware / Observabilidad
**Definición:** Middleware que genera o propaga un identificador único (`X-Correlation-Id`) por cada request/response HTTP. Permite trazar un flujo completo a través de múltiples servicios o logs. Si el cliente envía el header, el middleware lo reutiliza; si no, genera un nuevo UUID.
**En el proyecto:** `app.UseCorrelationId()` en `Program.cs` añade el `X-Correlation-Id` a cada request del CRM. Este ID aparece en los logs de Serilog y en el `traceId` de las responses de error.

### Cursor Pagination (Cursor-Based Pagination)
**Categoría:** Paginación / API
**Definición:** Estrategia de paginación que usa un "cursor" opaco (normalmente un valor codificado en base64 que combina timestamp e ID) para marcar el punto de anclaje en los datos. La siguiente página solicita los registros "después de" ese cursor usando una comparación de keyset en la consulta SQL. Tiene rendimiento constante sin importar la profundidad de la página.
**En el proyecto:** El historial de mensajes de una conversación usa cursor pagination con scroll infinito. El cliente recibe `nextCursor` y `hasNextPage`; cada request usa `WHERE (CreatedAtUtc, PublicId) < (@lastCreatedAt, @lastPublicId)`.

---

## D

### Deprecated (API)
**Categoría:** API / Versionado
**Definición:** Estado de una versión de API que sigue funcionando pero que ya no se recomienda usar y tiene fecha de fin de vida. El servidor puede indicarlo mediante headers HTTP como `Deprecation` o `Sunset`, o documentarlo en OpenAPI. Los clientes tienen un período de migración antes del apagado definitivo.
**En el proyecto:** Cuando el CRM evolucione a `/api/v2/contacts`, la versión `/api/v1/contacts` se marcará como deprecated con al menos 6 meses de aviso antes de ser desactivada.

### Discriminated Union (Pattern Matching)
**Categoría:** C# / Patrones del back-template
**Definición:** Técnica que usa jerarquías de tipos (abstract record + sealed records concretos) combinada con pattern matching (`is`, `switch`) para representar un resultado que puede ser una de varias variantes. Elimina la necesidad de excepciones para el flujo de control normal.
**En el proyecto:** El resultado de `GetAgentProfileRequest` puede ser `GetAgentProfilesSuccess` o `GetAgentProfileNotFoundFailure`. El Controller hace `result is GetAgentProfileNotFoundFailure` para decidir entre 404 y 500 sin depender de try/catch para el flujo normal.

### DTO (Data Transfer Object)
**Categoría:** Arquitectura / API
**Definición:** Objeto simple que transporta datos entre capas o sistemas. No tiene lógica de dominio ni comportamiento. En el back-template se definen como `record` inmutables en el proyecto `Contracts`. La API expone DTOs, nunca entidades de dominio directamente.
**En el proyecto:** `AgentProfileDto`, `ConversationDto`, `ContactDto` son los DTOs del CRM. Solo incluyen los campos que el cliente necesita: `PublicId`, `FullName`, `IsActive`, `CreatedAtUtc`. Nunca incluyen `Id` (serial interno).

---

## E

### Endpoint
**Categoría:** API / REST
**Definición:** Combinación de un verbo HTTP y una URI que identifica una operación específica de la API. Por ejemplo, `GET /api/v1/contacts` es un endpoint distinto de `POST /api/v1/contacts` aunque compartan la URI base. Cada endpoint tiene un contrato definido: parámetros, body, status codes posibles y forma de la response.
**En el proyecto:** El sistema de mensajería expone endpoints para cada módulo: auth, agents, contacts, conversations, messages, channels y bot-flows. Cada endpoint está documentado con `[ProducesResponseType]`.

### Enumeration Attack
**Categoría:** Seguridad / API
**Definición:** Ataque en el que un actor malicioso itera IDs secuenciales para descubrir recursos existentes. `GET /api/contacts/1`, `/2`, `/3`... permite conocer cuántos contactos existen y potencialmente acceder a datos de otros tenants si la autorización es deficiente. Se previene usando UUIDs como identificadores públicos.
**En el proyecto:** El CRM usa `PublicId` (UUID v4) en todas las URIs. `GET /api/contacts/550e8400-e29b-41d4-a716-446655440000` es imposible de iterar de forma aleatoria.

### Error Code (Application Error Code)
**Categoría:** API / Errores
**Definición:** Constante de string en mayúsculas que identifica de forma única un tipo de error de negocio dentro del mismo status code HTTP. Permite al cliente reaccionar diferente ante dos errores que comparten el mismo código de status. Se incluye junto al status code en la response de error.
**En el proyecto:** `EMAIL_ALREADY_EXISTS` y `CONVERSATION_ALREADY_ASSIGNED` son ambos 409 Conflict, pero el frontend del CRM muestra mensajes distintos al usuario según el `code` recibido.

---

## F

### Failure Type (INotFoundFailure, IFailure)
**Categoría:** C# / back-template / Result Pattern
**Definición:** Interfaz del proyecto `Common.Results` que marca un record de response como fallo. `IFailure` es la base; `INotFoundFailure` es el subtipo específico para recursos no encontrados. El Controller hace pattern matching sobre el tipo concreto para elegir el status code correcto.
**En el proyecto:** `GetAgentProfileNotFoundFailure : GetAgentProfileResponse, INotFoundFailure` es el tipo que el Controller evalúa con `result is GetAgentProfileNotFoundFailure ? NotFound(...) : StatusCode(500, ...)`.

### FromBody
**Categoría:** ASP.NET Core / Model Binding
**Definición:** Atributo que indica a ASP.NET Core que debe deserializar el cuerpo HTTP de la request (JSON) y mapearlo al parámetro decorado. Se usa en verbos que llevan body: `POST`, `PUT`, `PATCH`. El framework valida el Content-Type y rechaza requests malformados con 400.
**En el proyecto:** `[FromBody] CreateContactBody body` en el action de `POST /api/v1/contacts` deserializa el JSON del cliente a un record inmutable con los campos permitidos.

### FromQuery
**Categoría:** ASP.NET Core / Model Binding
**Definición:** Atributo que indica a ASP.NET Core que debe leer el valor de un parámetro desde la query string de la URL (`?page=1&pageSize=20`). Permite definir valores por defecto directamente en la firma del método. Se usa para filtros, paginación y ordenamiento.
**En el proyecto:** `[FromQuery] int page = 1` y `[FromQuery] int pageSize = 20` en `GetAll` de `AgentsController` permiten llamar al endpoint con o sin parámetros de paginación.

### Full-Text Search (FTS)
**Categoría:** Bases de datos / PostgreSQL
**Definición:** Capacidad de búsqueda sobre contenido textual que va más allá de `LIKE`. PostgreSQL implementa FTS con vectores `tsvector` e índices `GIN`, permitiendo búsquedas por stemming, ranking y operadores booleanos. Es significativamente más rápido que `LIKE '%término%'` en tablas grandes.
**En el proyecto:** La bandeja de conversaciones y el módulo de contactos del CRM usan FTS de PostgreSQL para el parámetro `?search=`. `GET /api/v1/contacts?search=maría` usa FTS en lugar de `LIKE`.

---

## G

### GUID / UUID (Globally Unique Identifier)
**Categoría:** API / Seguridad
**Definición:** Identificador de 128 bits generado de forma aleatoria que es prácticamente imposible de predecir o iterar. En C# el tipo es `Guid`. Es el tipo de dato para `PublicId` en el back-template. Las URIs exponen UUIDs en lugar de IDs secuenciales.
**En el proyecto:** `Guid` es el tipo de `PublicId` en todas las entidades del CRM: `Contact`, `Conversation`, `Message`, `Agent`. Se genera en la creación con `Guid.NewGuid()` y nunca cambia.

---

## H

### Handler (IRequestHandler)
**Categoría:** Mediator / back-template
**Definición:** Clase que implementa `IRequestHandler<TRequest, TResponse>` y contiene la lógica de aplicación de un use case específico. Recibe el request del mediador, ejecuta las operaciones de dominio y datos, y retorna una response (éxito o fallo). No conoce HTTP ni el Controller.
**En el proyecto:** `GetAgentProfilesHandler` obtiene la lista paginada de agentes del repositorio, proyecta a DTOs y retorna `GetAgentProfilesSuccess`. Usa `Task.WhenAll` para ejecutar la consulta y el conteo en paralelo.

### hasNextPage
**Categoría:** Cursor Pagination / API
**Definición:** Campo booleano en la response de cursor pagination que indica si existen más registros después del cursor actual. Permite al cliente saber si debe mostrar un botón "Cargar más" o si ya se mostraron todos los resultados. Se calcula solicitando `pageSize + 1` registros y verificando si llegaron más de `pageSize`.
**En el proyecto:** El historial de mensajes del CRM retorna `hasNextPage: true` mientras haya mensajes anteriores que cargar en el scroll infinito de la bandeja de chat.

### HATEOAS (Hypermedia As The Engine Of Application State)
**Categoría:** REST / Nivel avanzado
**Definición:** Constraint avanzada de REST donde la response incluye links hipermedia que describen las acciones disponibles sobre el recurso. El cliente descubre dinámicamente qué puede hacer sin conocer las URIs de antemano. Representa el nivel más alto del modelo de madurez de Richardson (Nivel 3).
**En el proyecto:** El back-template opera en Richardson Level 2. HATEOAS no se implementa en el sistema de mensajería por el alto costo de implementación y el bajo beneficio en APIs con un único cliente frontend conocido.

### HTTP Method / Verbo HTTP
**Categoría:** HTTP / REST
**Definición:** Palabra clave del protocolo HTTP que indica la intención semántica de la operación sobre el recurso. Los principales son `GET` (leer), `POST` (crear), `PUT` (reemplazar completo), `PATCH` (actualización parcial) y `DELETE` (eliminar). La semántica correcta permite a clientes, proxies y caches entender el comportamiento esperado.
**En el proyecto:** El CRM usa los cinco verbos: `GET` para leer conversaciones, `POST` para crear contactos y acciones de dominio, `PUT` para actualizar agentes, `DELETE` para soft-delete de recursos.

---

## I

### IMediator
**Categoría:** Mediator Pattern / back-template
**Definición:** Interfaz del sistema de mensajería propio del back-template. Recibe un request (`IRequest<TResponse>`) y lo despacha al handler registrado para ese tipo. Desacopla el Controller del Handler; el Controller solo conoce el tipo del request, no la implementación del handler.
**En el proyecto:** `Mediator.Send(new GetAgentProfilesRequest(CurrentTenantId, page, pageSize), ct)` en `AgentsController` despacha la operación sin que el Controller sepa nada del repositorio ni de la base de datos.

### INotificationHandler
**Categoría:** Mediator / Observer Pattern / back-template
**Definición:** Interfaz que implementa el Presenter para recibir automáticamente la response del Handler como una notificación (patrón Observer). Después de que el Handler retorna la response, el mediador la publica como notificación y el Presenter la recibe para poblar el ViewModel.
**En el proyecto:** `GetAgentProfilesPresenter : INotificationHandler<GetAgentProfilesResponse>` recibe el resultado del handler, discrimina con pattern matching entre `IFailure` y `GetAgentProfilesSuccess`, y llama a `_viewModel.Fail()` o `_viewModel.OK()`.

### IRequest
**Categoría:** Mediator / back-template
**Definición:** Interfaz marcadora del sistema de mensajería del back-template. Al implementar `IRequest<TResponse>`, un record declara el tipo de respuesta que espera. El mediador usa esta información para encontrar el handler correcto en tiempo de ejecución.
**En el proyecto:** `GetAgentProfilesRequest : IRequest<GetAgentProfilesResponse>` indica al mediador que debe buscar un `IRequestHandler<GetAgentProfilesRequest, GetAgentProfilesResponse>` para procesar la solicitud.

### IResponse / ISuccess / ISuccess\<T\>
**Categoría:** Result Pattern / back-template
**Definición:** Interfaces del proyecto `Common.Messaging` / `Common.Results` que marcan los records de response. `IResponse` es la base; `ISuccess` marca una respuesta exitosa sin data tipada; `ISuccess<T>` marca una exitosa con propiedad `.Data` del tipo `T`. El Presenter discrimina entre `IFailure` e `ISuccess` para poblar el ViewModel.
**En el proyecto:** `GetAgentProfilesSuccess : GetAgentProfilesResponse, ISuccess` es detectado por el Presenter con `notification is GetAgentProfilesSuccess success` y llama a `_viewModel.OK(success)`.

### Idempotencia / Idempotente
**Categoría:** HTTP / REST
**Definición:** Propiedad de una operación donde ejecutarla N veces produce el mismo resultado que ejecutarla una sola vez. `GET`, `PUT` y `DELETE` son idempotentes. `POST` no lo es (crear dos veces crea dos recursos). La idempotencia permite a clientes y proxies reintentar requests de forma segura.
**En el proyecto:** `DELETE /api/v1/contacts/abc-123` ejecutado dos veces produce el mismo resultado: el contacto queda desactivado (soft delete). El segundo intento puede retornar 404, pero el estado final es idéntico.

### Idempotency Key
**Categoría:** API / Confiabilidad
**Definición:** UUID enviado por el cliente en el header `Idempotency-Key` para garantizar que una operación `POST` no se ejecute dos veces aunque el cliente reintente. El servidor guarda el resultado asociado a esa key por un período (típicamente 24 horas). Si recibe la misma key, retorna el resultado guardado sin ejecutar la operación de nuevo.
**En el proyecto:** `POST /api/v1/messages` con `Idempotency-Key` garantiza que un mensaje no se envíe dos veces si el cliente no recibió la confirmación inicial. Crítico para los envíos de mensajes del CRM.

### ISO 8601 UTC
**Categoría:** Fechas / API
**Definición:** Estándar internacional para representar fechas y horas como cadenas de texto en formato `YYYY-MM-DDTHH:MM:SSZ`. La `Z` al final indica zona horaria UTC. Es el único formato aceptado para fechas en APIs REST porque es inequívoco y parseable automáticamente por cualquier lenguaje.
**En el proyecto:** Todos los campos de fecha del CRM usan ISO 8601 UTC: `createdAt: "2026-05-29T10:30:00Z"`. Los nombres de las propiedades en C# incluyen el sufijo `Utc`: `CreatedAtUtc`, `UpdatedAtUtc`.

---

## J

### JWT (JSON Web Token)
**Categoría:** Autenticación / Seguridad
**Definición:** Token compacto firmado que codifica claims del usuario en formato JSON y los transmite en el header `Authorization`. Tiene tres partes separadas por puntos: header (algoritmo), payload (claims) y signature (firma HMAC o RSA). El servidor verifica la firma sin consultar una base de datos de sesiones.
**En el proyecto:** El CRM usa JWT con algoritmo HS256. El payload incluye `sub` (userId), `tenant_id`, `role` y `exp`. El Controller lee `tenant_id` con `User.FindFirstValue("tenant_id")` para filtrar datos por workspace.

---

## K

### KebabCase (URI)
**Categoría:** Naming Conventions / URI
**Definición:** Convención de nomenclatura donde las palabras se separan con guiones: `bot-flows`, `rate-limiting`, `assigned-to`. Es el estándar para segmentos de URI en REST. Más legible que camelCase y snake_case en URLs, y compatible con todos los servidores y proxies HTTP.
**En el proyecto:** `/api/v1/bot-flows`, `/api/v1/assigned-agents` siguen kebab-case. El antipatrón `/api/v1/botFlows` y `/api/v1/bot_flows` están explícitamente prohibidos en las convenciones del CRM.

### Keyset Pagination
**Categoría:** Paginación / SQL
**Definición:** Técnica SQL que implementa cursor pagination usando una comparación compuesta en el `WHERE` en lugar de `OFFSET`. La consulta filtra con `(CreatedAtUtc, PublicId) < (@lastCreatedAt, @lastPublicId)` y usa un índice compuesto para rendimiento constante sin importar la posición en la tabla.
**En el proyecto:** El historial de mensajes del CRM usa keyset pagination: `WHERE (CreatedAtUtc, PublicId) < (@lastCreatedAt, @lastPublicId) ORDER BY CreatedAtUtc DESC, PublicId DESC LIMIT 20`. Elimina el problema de rendimiento del OFFSET grande.

---

## L

### Layered System (Constraint REST)
**Categoría:** REST / Constraints
**Definición:** Una de las seis constraints de REST. El cliente no sabe si se conecta directamente al servidor final o a través de intermediarios (proxy, load balancer, CDN, API gateway). Cada capa solo conoce la capa inmediatamente adyacente. Permite escalar y asegurar la arquitectura sin cambiar el cliente.
**En el proyecto:** El frontend del CRM se conecta a la API sin saber si hay un load balancer de AWS, un API gateway o un nginx proxy en el camino. La URL es la misma independientemente de la topología del servidor.

---

## M

### Math.Clamp
**Categoría:** C# / Validación
**Definición:** Función de la librería estándar de C# que restringe un valor a un rango `[min, max]`. Se usa en los handlers para normalizar los parámetros de paginación recibidos del cliente, previniendo valores negativos o page sizes extremadamente grandes que podrían causar problemas de rendimiento o de memoria.
**En el proyecto:** El handler aplica `Math.Clamp(pageSize, 1, 100)` antes de la consulta a la base de datos. Un cliente que envíe `pageSize=99999` recibirá solo 100 registros, protegiendo al servidor de consultas desproporcionadas.

### Mediator Pattern
**Categoría:** Patrones de diseño / back-template
**Definición:** Patrón de comportamiento donde un objeto central (mediador) coordina la comunicación entre otros objetos, evitando que se referencien directamente. En el back-template, el mediador recibe requests del Controller y los despacha al Handler correspondiente, y luego publica la response al Presenter.
**En el proyecto:** `Mediator.Send(request, ct)` en cada action del Controller es el único punto de comunicación entre la capa de Presentación y la capa de Aplicación en el CRM.

### Middleware (ASP.NET Core)
**Categoría:** ASP.NET Core / Pipeline
**Definición:** Componente que se integra en el pipeline de procesamiento de requests HTTP. Cada middleware puede ejecutar lógica antes y después de pasar el request al siguiente componente. Se registran en `Program.cs` con `app.Use...()`. El orden de registro es el orden de ejecución.
**En el proyecto:** El CRM usa tres middlewares custom: `ProblemDetailsMiddleware`, `CorrelationIdMiddleware` y `TenantResolutionMiddleware`. Se registran antes del routing para que apliquen a todos los requests.

---

## N

### Naming Convention
**Categoría:** API / Convenciones
**Definición:** Conjunto de reglas sobre cómo nombrar campos, endpoints, parámetros y recursos de una API. La consistencia en las naming conventions es más importante que seguir el estándar "correcto": una API inconsistente obliga al cliente a leer la documentación de cada endpoint individualmente.
**En el proyecto:** El CRM sigue: camelCase en JSON, kebab-case en URIs, ISO 8601 para fechas, UUIDs para IDs públicos, sufijo `Utc` en campos de fecha en C#, y `PublicId` como nombre del identificador externo.

### nextCursor
**Categoría:** Cursor Pagination / API
**Definición:** Campo opaco en la response de cursor pagination que el cliente debe enviar como parámetro en la siguiente request para obtener la página siguiente. Generalmente está codificado en base64 y combina el timestamp y el ID del último elemento de la página actual.
**En el proyecto:** `"nextCursor": "MjAyNi0wNS0yOVQwOTowMDowMFp8NTUwZTg0MDA="` en la response de mensajes del CRM decodifica a `2026-05-29T09:00:00Z|550e8400`, que alimenta la cláusula `WHERE` de keyset pagination.

### NotFound (404)
**Categoría:** HTTP / Status Codes
**Definición:** Status code que indica que el recurso solicitado no existe en el servidor. Se debe retornar cuando el ID o UUID de un recurso no corresponde a ningún registro existente (o accesible para ese tenant). No debe confundirse con 403 Forbidden (el recurso existe pero no tienes permiso).
**En el proyecto:** `GET /api/v1/agents/550e8400-xxxx-...` retorna 404 si ese UUID no existe en el tenant del usuario autenticado. El Controller evalúa `result is GetAgentProfileNotFoundFailure ? NotFound(_viewModel) : StatusCode(500, _viewModel)`.

---

## O

### Observer Pattern
**Categoría:** Patrones de diseño / back-template
**Definición:** Patrón de comportamiento donde un sujeto notifica automáticamente a sus observadores cuando ocurre un evento. En el back-template, el Handler publica la response como notificación y el Presenter (observador) la recibe automáticamente a través del mediador.
**En el proyecto:** El Presenter implementa `INotificationHandler<GetAgentProfilesResponse>`. Cuando el Handler retorna `GetAgentProfilesSuccess`, el mediador notifica al Presenter, que puebla `_viewModel` antes de que el Controller evalúe `_viewModel.IsSuccess`.

### Offset Pagination
**Categoría:** Paginación / API
**Definición:** Estrategia de paginación que usa `LIMIT` y `OFFSET` en SQL para saltar a una página específica. Simple de implementar y permite saltar a cualquier página directamente. Tiene dos problemas: puede retornar registros duplicados si se insertan datos entre páginas, y el rendimiento degrada en páginas profundas porque la base de datos lee y descarta todos los registros anteriores al OFFSET.
**En el proyecto:** La bandeja de conversaciones y la lista de contactos del CRM usan offset pagination con `?page=2&pageSize=20`. Es apropiado para estas vistas porque los usuarios raramente navegan a páginas muy profundas.

### OOM (Out of Memory)
**Categoría:** Performance / Backend
**Definición:** Condición de error en la que el proceso consume más memoria de la disponible y el sistema operativo lo termina. En APIs sin paginación, una query que retorna millones de registros puede causar OOM al intentar cargar todo el dataset en memoria del servidor.
**En el proyecto:** Sin paginación, `GET /api/v1/messages` en una conversación activa del CRM con cientos de miles de mensajes podría causar OOM. El handler limita el page size a 100 con `Math.Clamp`.

### OpenAPI
**Categoría:** Documentación / API
**Definición:** Especificación estándar (antes llamada Swagger) para describir APIs REST en formato JSON o YAML. Define endpoints, parámetros, tipos de datos, esquemas de request/response y requisitos de seguridad. Permite generar documentación interactiva, SDKs de cliente y tests automáticos.
**En el proyecto:** El back-template configura OpenAPI con Swashbuckle a través de `AddSwaggerWithJwt`. Los `[ProducesResponseType]` y los XML comments enriquecen el documento generado automáticamente.

---

## P

### Paginación
**Categoría:** API / Performance
**Definición:** Técnica para dividir una colección grande de resultados en páginas más pequeñas, retornando solo un subconjunto por request. Es obligatoria para cualquier endpoint que pueda retornar más de algunos cientos de registros. Existen dos estrategias principales: offset y cursor.
**En el proyecto:** Todo endpoint de listado del CRM incluye paginación. La regla es: bandeja/búsqueda -> offset; historial de mensajes -> cursor. El page size máximo es 100, aplicado con `Math.Clamp`.

### Pattern Matching (C#)
**Categoría:** C# / Lenguaje
**Definición:** Característica del lenguaje C# que permite comprobar el tipo y estructura de un valor usando expresiones `is`, `switch` y `when`. En el back-template se usa para discriminar entre variantes de la response (discriminated unions) sin casting explícito ni excepciones.
**En el proyecto:** `result is GetAgentProfileNotFoundFailure` en el Controller y `notification is GetAgentProfilesSuccess success` en el Presenter son ejemplos de pattern matching del módulo de Agents del CRM.

### ProblemDetails (RFC 7807)
**Categoría:** HTTP / Errores
**Definición:** Estándar RFC 7807 que define la forma del body de una respuesta de error HTTP. Incluye los campos `type` (URI del tipo de error), `title` (descripción corta), `status` (código HTTP), `detail` (mensaje para el usuario) e `instance` (URI de la request que causó el error). Permite a clientes y herramientas reaccionar de forma estandarizada a los errores.
**En el proyecto:** `ProblemDetailsMiddleware` del back-template convierte todas las excepciones no controladas en responses RFC 7807. `BusinessRuleException` produce 400; cualquier otra `Exception` produce 500 sin exponer el stack trace.

### ProblemDetailsMiddleware
**Categoría:** Middleware / back-template
**Definición:** Middleware del back-template que envuelve toda la cadena de request handling y captura dos tipos de excepciones: `BusinessRuleException` (la convierte en 400) y cualquier `Exception` no controlada (la convierte en 500). Registra los errores con Serilog y escribe una response RFC 7807 sin exponer detalles internos en producción.
**En el proyecto:** `app.UseCoreProblemDetails()` en `Program.cs` garantiza que ningún error del CRM exponga stack traces al cliente. El `traceId` en la response permite correlacionar con los logs internos de Serilog.

### ProducesResponseType
**Categoría:** ASP.NET Core / OpenAPI
**Definición:** Atributo de ASP.NET Core que documenta los posibles status codes y tipos de respuesta de un action method. Alimenta la especificación OpenAPI generada por Swashbuckle. Sin este atributo, Swagger no puede mostrar qué errores puede retornar cada endpoint.
**En el proyecto:** `[ProducesResponseType(typeof(ResultViewModel<ContactDto>), 201)]` y `[ProducesResponseType(typeof(ProblemDetails), 409)]` en `ContactsController` documentan el contrato completo del endpoint de creación de contactos.

### PublicId
**Categoría:** API / Seguridad / back-template
**Definición:** Campo `Guid` de las entidades del back-template que actúa como identificador externo. Es el único ID que aparece en las responses de la API y en las URIs. Se diferencia del `Id` (serial `long`) que solo se usa para joins internos en la base de datos y nunca sale al exterior.
**En el proyecto:** Todas las entidades del CRM (`Contact`, `Conversation`, `Message`, `Agent`) tienen `PublicId`. Las URIs son `/api/v1/contacts/550e8400-e29b-41d4-a716-446655440000` nunca `/api/v1/contacts/42`.

---

## Q

### Query Param (Query String Parameter)
**Categoría:** HTTP / URI
**Definición:** Par clave-valor añadido al final de una URI después del símbolo `?`, separados por `&`. Se usan para filtros opcionales, paginación, ordenamiento y búsqueda. No forman parte del identificador del recurso; representan modificadores sobre la colección.
**En el proyecto:** `GET /api/v1/conversations?page=1&pageSize=20&status=open&assignedTo=agent-uuid` usa cuatro query params. `[FromQuery]` en el Controller los mapea automáticamente a los parámetros del action method.

---

## R

### Rate Limiting
**Categoría:** API / Seguridad / Performance
**Definición:** Mecanismo que limita el número de requests que un cliente puede hacer en un período de tiempo. Protege la API contra abuso, ataques de fuerza bruta y clientes con bugs que generan tráfico excesivo. El servidor responde con 429 y headers que indican el límite, los restantes y cuándo reintentar.
**En el proyecto:** El CRM implementa rate limiting con el middleware built-in de .NET: 100 requests por minuto por usuario. La response 429 incluye `Retry-After: 60`, `X-RateLimit-Limit: 100` y `X-RateLimit-Remaining: 0`.

### Record (C# Sealed Record)
**Categoría:** C# / Lenguaje
**Definición:** Tipo de referencia inmutable introducido en C# 9 que genera automáticamente igualdad estructural, `ToString()` y deconstrucción. Con la palabra clave `sealed` se evita la herencia. En el back-template se usa para Requests, Responses y Request Bodies por ser inmutables y expresivos.
**En el proyecto:** `public sealed record UpdateAgentProfileBody(string FullName)` es el request body de la actualización de agentes. `public sealed record GetAgentProfilesRequest(long TenantId, int Page, int PageSize)` es el request inmutable que el Controller envía al mediador.

### RequestBody
**Categoría:** HTTP / API
**Definición:** Datos enviados en el cuerpo de la request HTTP, típicamente en formato JSON. Se usa con verbos `POST`, `PUT` y `PATCH`. En el back-template se modela como un `sealed record` por cada operación, conteniendo solo los campos que el cliente puede enviar (nunca `Id` ni `TenantId`).
**En el proyecto:** `UpdateAgentProfileBody(string FullName)` es el único campo que el cliente puede cambiar de un agente. `TenantId` viene del JWT y `Id`/`PublicId` vienen de la URI, nunca del body.

### Resource (Recurso REST)
**Categoría:** REST / Arquitectura
**Definición:** Unidad de diseño fundamental de REST. Cualquier cosa que pueda nombrarse puede ser un recurso: un documento, una colección, un objeto de dominio. Los recursos se modelan como sustantivos en plural en la URI (`/contacts`, `/conversations`) y se manipulan con verbos HTTP.
**En el proyecto:** Los recursos del CRM son: `contacts`, `conversations`, `messages`, `agents`, `channels`, `bot-flows`. Cada uno tiene su colección (`GET /api/v1/contacts`) y su elemento individual (`GET /api/v1/contacts/{id}`).

### REST (Representational State Transfer)
**Categoría:** Arquitectura / API
**Definición:** Estilo arquitectónico definido por Roy Fielding en su disertación de 2000. No es un protocolo ni un estándar: es un conjunto de seis constraints que, al cumplirse, producen APIs escalables, desacopladas y con semántica predecible. Una API que cumple todos los constraints se llama "RESTful".
**En el proyecto:** El sistema de mensajería implementa una API RESTful sobre HTTP/HTTPS. Cumple las cinco constraints obligatorias: Client-Server, Stateless, Cacheable, Uniform Interface y Layered System.

### ResultViewModel\<T\>
**Categoría:** back-template / ViewModel
**Definición:** Clase del proyecto `Common.ViewModels` que encapsula la forma de todas las responses de la API. Tiene cuatro campos: `IsSuccess` (bool), `Data` (object?), `Message` (string?), `UtcTimeStamp` (DateTime). El Presenter la puebla con `.OK(data)` o `.Fail(message)`. El Controller la retorna como body de la response HTTP.
**En el proyecto:** Todos los endpoints del CRM retornan `ResultViewModel<T>`. El frontend puede siempre evaluar `response.isSuccess` para saber si la operación fue exitosa sin necesidad de parsear el status code.

### Retry-After
**Categoría:** HTTP / Rate Limiting
**Definición:** Header HTTP estándar que el servidor envía junto a una respuesta 429 o 503, indicando cuántos segundos debe esperar el cliente antes de reintentar. Permite al cliente implementar backoff exacto sin adivinar el tiempo de espera.
**En el proyecto:** `Retry-After: 60` en la response 429 del CRM indica al frontend que debe esperar 60 segundos antes de reintentar. El cliente legítimo lee este header; un cliente sin control generaría más carga innecesaria al servidor.

### RFC 7807
**Categoría:** Estándares / HTTP / Errores
**Definición:** Estándar de la IETF que define el formato "Problem Details for HTTP APIs". Establece un esquema JSON reutilizable para representar errores de forma estructurada: `type`, `title`, `status`, `detail`, `instance`. Las extensiones como `traceId` o `errors` son válidas siempre que no colisionen con los campos estándar.
**En el proyecto:** El back-template implementa RFC 7807 a través de `ProblemDetailsMiddleware`. Todos los errores del CRM siguen este formato, permitiendo al frontend y a herramientas de monitoreo procesar errores de forma uniforme.

### Richardson Maturity Model
**Categoría:** REST / Madurez
**Definición:** Modelo de cuatro niveles (0-3) que describe el grado de adopción de REST en una API. Nivel 0: HTTP como transporte (RPC). Nivel 1: Recursos. Nivel 2: Verbos HTTP correctos y status codes semánticos. Nivel 3: HATEOAS. La mayoría de las APIs modernas operan en Nivel 2.
**En el proyecto:** El back-template y el sistema de mensajería operan en Richardson Level 2: usan recursos con URIs sustantivos y verbos HTTP con semántica correcta. HATEOAS (Nivel 3) no se implementa por el costo/beneficio.

### RPC sobre HTTP (Anti-patrón REST)
**Categoría:** Arquitectura / Anti-patrón
**Definición:** Estilo de API donde las operaciones se modelan como llamadas a procedimientos en lugar de operaciones sobre recursos. Resulta en URIs con verbos: `/getContacts`, `/createContact`, `/deleteMessage`. No aprovecha la semántica HTTP y es el anti-patrón más común en APIs que dicen ser REST pero no lo son.
**En el proyecto:** El antipatrón `GET /api/getContacts`, `POST /api/createContact` es lo que el equipo debe evitar. La corrección es `GET /api/v1/contacts`, `POST /api/v1/contacts`.

---

## S

### Scroll Infinito (Infinite Scroll)
**Categoría:** Frontend / UX
**Definición:** Patrón de interfaz donde el contenido se carga automáticamente al llegar al final del viewport, en lugar de mostrar botones de página. Es el caso de uso principal para cursor pagination, ya que no hay concepto de "página N" y los datos deben cargarse en orden secuencial continuo.
**En el proyecto:** La bandeja de chat del CRM (historial de mensajes de una conversación) usa scroll infinito con cursor pagination. El usuario hace scroll hacia arriba para ver mensajes anteriores y el frontend solicita la siguiente página con el cursor recibido.

### Sealed Record
**Categoría:** C# / Lenguaje
**Definición:** Combinación de `sealed` (impide herencia) y `record` (tipo inmutable con igualdad estructural). Es el tipo preferido del back-template para Requests, Responses concretas y Request Bodies porque comunica claramente la intención: este tipo es el final de la jerarquía y no debe modificarse.
**En el proyecto:** `public sealed record GetAgentProfilesSuccess(IReadOnlyCollection<AgentProfileDto> Profiles, int Total, int Page, int PageSize)` es el sealed record de success del módulo de Agents del CRM.

### Soft Delete
**Categoría:** Base de datos / back-template
**Definición:** Técnica donde los registros no se eliminan físicamente de la base de datos. En su lugar se marca un campo (típicamente `IsActive = false` o `DeletedAt = DateTime.UtcNow`) y las queries filtran los registros inactivos. Preserva el historial y evita problemas de integridad referencial.
**En el proyecto:** El back-template nunca borra físicamente. `DELETE /api/v1/agents/{id}` ejecuta el handler `DisableAgentProfileRequest` que marca `IsActive = false`. Las queries incluyen `WHERE IsActive = true` o `WHERE DeletedAt IS NULL`.

### sortBy / sortDir
**Categoría:** API / Query Params
**Definición:** Query params convencionales para ordenar colecciones en una API. `sortBy` acepta el nombre del campo en camelCase; `sortDir` acepta `asc` o `desc`. Son opcionales: si no se envían, el handler aplica el orden por defecto (generalmente `createdAt desc`).
**En el proyecto:** `GET /api/v1/contacts?sortBy=createdAt&sortDir=desc` ordena los contactos del CRM del más reciente al más antiguo. El handler valida que el valor de `sortBy` sea uno de los campos permitidos para evitar inyección SQL.

### Stateless (Constraint REST)
**Categoría:** REST / Constraints
**Definición:** La constraint más importante de REST. Cada request debe contener toda la información necesaria para que el servidor la procese, sin depender de estado almacenado en el servidor (sesiones). El estado de la conversación vive en el cliente (JWT, cookies) no en el servidor.
**En el proyecto:** El CRM no usa sesiones en el servidor. El JWT incluye `tenant_id`, `userId` y `role` en cada request. El servidor puede escalar horizontalmente sin sincronizar estado de sesión entre instancias.

### Status Code HTTP
**Categoría:** HTTP / API
**Definición:** Número de tres dígitos en la respuesta HTTP que indica el resultado de la operación. El primer dígito define la categoría: 2xx (éxito), 3xx (redirección), 4xx (error del cliente), 5xx (error del servidor). El status code es la primera señal que el cliente recibe y debe ser semánticamente correcto.
**En el proyecto:** El CRM usa: 200 (OK), 201 (Created), 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found), 409 (Conflict), 422 (Unprocessable Entity), 429 (Too Many Requests), 500 (Internal Server Error).

### Sub-recurso de Acción (Action Sub-resource)
**Categoría:** REST / Diseño de URIs
**Definición:** Patrón para modelar operaciones que no encajan en el CRUD estándar. Se añade un segmento de acción al final del recurso y se usa `POST` como verbo. Ejemplo: `POST /conversations/abc/close`. El segmento final actúa como nombre de la operación dentro del contexto del recurso padre.
**En el proyecto:** El CRM usa sub-recursos de acción para: `POST /api/v1/conversations/{id}/assign`, `POST /api/v1/conversations/{id}/close`, `POST /api/v1/contacts/{id}/archive`, `POST /api/v1/messages/{id}/resend`.

### Swagger / Swashbuckle
**Categoría:** OpenAPI / Herramientas
**Definición:** Swashbuckle es la librería de .NET que genera automáticamente un documento OpenAPI (Swagger) inspeccionando los controllers, atributos y tipos del proyecto. Swagger UI es la interfaz web interactiva que permite probar la API directamente desde el navegador.
**En el proyecto:** El back-template configura Swashbuckle con `services.AddSwaggerWithJwt()`. La UI de Swagger disponible en `/swagger` permite al equipo del CRM probar endpoints con JWT sin necesidad de Postman u otras herramientas externas.

---

## T

### Task.WhenAll
**Categoría:** C# / Asincronía
**Definición:** Método de la librería estándar que ejecuta múltiples tareas asíncronas en paralelo y espera a que todas completen. Es más eficiente que `await` secuencial cuando las tareas son independientes entre sí.
**En el proyecto:** El handler `GetAgentProfilesHandler` usa `await Task.WhenAll(listTask, totalTask)` para ejecutar la query de lista y la query de conteo en paralelo, reduciendo la latencia de la operación de listado paginado.

### TenantId (CurrentTenantId)
**Categoría:** Multi-tenancy / back-template
**Definición:** Identificador del workspace o empresa del usuario autenticado, extraído del claim `tenant_id` del JWT. Se usa para filtrar todos los datos en la base de datos, garantizando que un usuario solo acceda a los datos de su propio tenant. Sin un `TenantId` válido, el filtro retorna 0 y no se obtiene ningún registro.
**En el proyecto:** `CurrentTenantId` en `AgentsController` lee `long.TryParse(User.FindFirstValue("tenant_id"), ...)`. Este valor se pasa a todos los requests y handlers del CRM, que lo incluyen en el `WHERE TenantId = @tenantId` de todas las queries.

### TenantResolutionMiddleware
**Categoría:** Middleware / Multi-tenancy / back-template
**Definición:** Middleware que resuelve el identificador del tenant actual a partir del subdominio de la URL o de un header HTTP antes de que el request llegue al controller. Complementa el claim `tenant_id` del JWT, permitiendo validar que el tenant del JWT coincida con el tenant de la URL.
**En el proyecto:** `app.UseTenantResolution()` se registra en `Program.cs` del CRM. Resuelve el tenant del subdominio (ej: `acme.app.example.com`) o del header `X-Tenant-Id` en requests de clientes de integración.

### traceId
**Categoría:** Observabilidad / Errores
**Definición:** Identificador único incluido en las responses de error (Problem Details) que correlaciona el error que ve el cliente con la línea de log en el sistema de logging del servidor. El cliente puede reportar el `traceId` al equipo de backend para localizar exactamente qué sucedió sin que se hayan expuesto detalles internos.
**En el proyecto:** `"traceId": "00-abc123def456ghi789..."` en las responses de error del CRM permite al equipo buscar en los logs de Serilog sin que el cliente haya visto detalles técnicos del error.

---

## U

### Uniform Interface (Constraint REST)
**Categoría:** REST / Constraints
**Definición:** Una de las seis constraints de REST. Define que la interacción entre cliente y servidor debe seguir una interfaz uniforme: URL identifica el recurso, verbo HTTP indica la acción, y JSON es la representación. Esta uniformidad simplifica la arquitectura y permite la evolución independiente de cliente y servidor.
**En el proyecto:** El CRM sigue la uniform interface: `GET /api/v1/contacts` para leer, `POST /api/v1/contacts` para crear, etc. El cliente predice el comportamiento de cualquier endpoint sin leer el código fuente.

### URI (Uniform Resource Identifier)
**Categoría:** HTTP / REST
**Definición:** Cadena que identifica unívocamente un recurso en la web. En REST, la URI identifica al recurso (el sustantivo) y el verbo HTTP indica la acción. Las URIs deben seguir convenciones de nomenclatura: sustantivos en plural, minúsculas, separados con guiones, sin verbos, máximo dos niveles de anidamiento.
**En el proyecto:** `/api/v1/conversations/abc-123/messages` es una URI válida del CRM. Identifica los mensajes (`messages`) de una conversación específica (`conversations/abc-123`) en la versión 1 de la API.

### UUID v4
**Categoría:** Identificadores / Seguridad
**Definición:** Versión 4 del estándar UUID que genera identificadores de 128 bits de forma completamente aleatoria. Con 2^122 posibles valores, la probabilidad de colisión es despreciable para cualquier aplicación práctica. Es el tipo recomendado para identificadores públicos en APIs.
**En el proyecto:** `Guid.NewGuid()` en .NET genera UUID v4. Todos los `PublicId` del CRM son UUID v4. El formato en JSON es `"550e8400-e29b-41d4-a716-446655440000"` (con guiones, 32 dígitos hexadecimales).

---

## V

### Versionado de API (API Versioning)
**Categoría:** API / Evolución
**Definición:** Estrategia para introducir cambios incompatibles en una API sin romper a los clientes existentes. Se crea una nueva versión (v2) con el nuevo contrato mientras la versión anterior (v1) sigue activa durante un período de migración. Las tres estrategias principales son: URI (`/api/v1/`), header (`Api-Version: 1`) y query param (`?api-version=1`).
**En el proyecto:** El CRM usa versionado por URI desde el día 1: `/api/v1/contacts`, `/api/v1/conversations`. Aunque no se prevean cambios inmediatos, el prefijo permite introducir `/api/v2/` en el futuro sin URIs de migración complicadas.

### ViewModel (ResultViewModel)
**Categoría:** Presentación / back-template
**Definición:** Objeto que encapsula los datos que se enviarán al cliente en la response HTTP. En el back-template, el ViewModel es `ResultViewModel<T>`: una instancia scoped por request que el Presenter puebla y el Controller retorna. Separa la construcción de la respuesta (Presenter) de la decisión del status code (Controller).
**En el proyecto:** `ResultViewModel<AgentsController>` es inyectado por DI en `AgentsController`. El Presenter lo puebla vía `.OK(success)` o `.Fail(message)`. El Controller retorna `Ok(_viewModel)`, `NotFound(_viewModel)` o `StatusCode(500, _viewModel)`.

---

## W

### Webhook
**Categoría:** Integración / API
**Definición:** Mecanismo donde un servidor notifica a otro sistema enviando un HTTP POST cuando ocurre un evento. El receptor registra una URL que el emisor invoca al ocurrir el evento. Es el patrón opuesto al polling: en lugar de preguntar periódicamente, el sistema es notificado de forma activa.
**En el proyecto:** Los canales de WhatsApp e Instagram del CRM reciben mensajes entrantes vía webhooks: la plataforma (Meta) envía un POST al endpoint `/api/v1/channels/{id}/webhook` del CRM cuando llega un mensaje nuevo.

---

## X

### X-Correlation-Id
**Categoría:** HTTP Headers / Observabilidad
**Definición:** Header HTTP custom que transporta un identificador único por cada request a través de toda la cadena de procesamiento. Generado por `CorrelationIdMiddleware` si el cliente no lo envía. Se incluye en los logs de Serilog y en las responses, facilitando la correlación de eventos en sistemas distribuidos.
**En el proyecto:** `CorrelationIdMiddleware` del CRM añade `X-Correlation-Id` a cada request y response. El frontend puede incluirlo en reportes de bug para que el equipo backend localice el log exacto de una operación fallida.

### X-RateLimit-Limit / X-RateLimit-Remaining / X-RateLimit-Reset
**Categoría:** HTTP Headers / Rate Limiting
**Definición:** Headers HTTP convencionales que comunican el estado del rate limit al cliente: el límite total (`X-RateLimit-Limit`), las requests restantes en la ventana actual (`X-RateLimit-Remaining`) y el timestamp Unix cuando se resetea la ventana (`X-RateLimit-Reset`).
**En el proyecto:** El middleware de rate limiting del CRM añade estos tres headers a cada response. Un cliente bien implementado los lee y reduce su tasa de requests antes de llegar al 429, en lugar de esperar a ser bloqueado.

### XML Comments (C#)
**Categoría:** Documentación / C#
**Definición:** Comentarios estructurados en C# con prefijo `///` que describen clases, métodos y parámetros. Swashbuckle los incluye en el documento OpenAPI generado, enriqueciendo la UI de Swagger con descripciones de cada endpoint, sus parámetros y sus posibles responses.
**En el proyecto:** Los controllers del CRM usan XML comments para documentar cada action: `/// <summary>Obtiene todos los mensajes de una conversación.</summary>` y `/// <param name="cursor">Cursor opaco de la respuesta anterior (base64)</param>`.

---

## Z

### Zero Trust (en APIs)
**Categoría:** Seguridad / Arquitectura
**Definición:** Principio de seguridad donde ningún actor (usuario, servicio, red) se considera confiable por defecto, aunque provenga de la red interna. Cada request debe autenticarse y autorizarse independientemente. Se implementa con JWT validation en cada request, sin sesiones implícitas ni confianza basada en IP.
**En el proyecto:** El CRM implementa Zero Trust: cada request a `/api/v1/` debe llevar un JWT válido con `tenant_id` y `role`. El controller valida tanto la autenticación (`[Authorize]`) como la autorización por rol (`[Authorize(Roles = "Admin")]`) en cada operación sensible.
