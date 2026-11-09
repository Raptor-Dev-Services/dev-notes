# Glosario Técnico  -  Caching  -  Background Services  -  Polly

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### AbsoluteExpiration
**Categoría:** Caching
**Definición:** Tipo de expiración de caché en el que la entrada se elimina automáticamente pasado un tiempo fijo contado desde el momento en que fue guardada, independientemente de si se accede a ella o no. Configurada con `SetAbsoluteExpiration(TimeSpan)` en `IMemoryCache` o con `AbsoluteExpirationRelativeToNow` en `DistributedCacheEntryOptions`.
**En el proyecto:** Se usa para cachear la lista de canales configurados del workspace (`TimeSpan.FromMinutes(10)`) y la configuración del workspace (`TimeSpan.FromMinutes(30)`), datos que deben refrescarse periódicamente sin importar el tráfico.

### AddCoreResilience
**Categoría:** Resiliencia / Polly
**Definición:** Extension method definido en `Common/Common/Http/HttpClientBuilderEx.cs` del back-template. Encapsula la llamada a `AddStandardResilienceHandler` y permite inyectar una `IConfigurationSection` para sobreescribir los parámetros de resiliencia desde `appsettings.json` sin modificar el código fuente.
**En el proyecto:** Se aplica al registro del `HttpClient` de la Meta API en `Channels.Infrastructure/ServiceCollectionEx.cs`, aportando retry, circuit breaker y timeouts de forma estándar a las llamadas a la Graph API de Facebook/WhatsApp.

### AddHostedService
**Categoría:** Background Services / DI
**Definición:** Método de extensión de `IServiceCollection` que registra una clase como hosted service. El host de .NET crea una única instancia (ciclo de vida Singleton) y llama a `StartAsync` al iniciar la aplicación y a `StopAsync` durante el apagado.
**En el proyecto:** Se invoca dentro de los extension methods de cada módulo  -  por ejemplo, `AddConversationsInfrastructureServices` registra `ConversationCleanupService`, y `AddChannelsInfrastructureServices` registra `WebhookProcessorService`.

### AddStackExchangeRedisCache
**Categoría:** Caching / Redis
**Definición:** Extension method que configura Redis como implementación de `IDistributedCache` usando la librería `StackExchange.Redis`. Recibe la cadena de conexión y un prefijo (`InstanceName`) que se antepone a todas las claves para evitar colisiones entre aplicaciones que compartan el mismo servidor Redis.
**En el proyecto:** Configurado en `Host.Api/Extensions/CacheExtensions.cs` con la conexión `"Redis:Connection"` del `appsettings.json` y el prefijo `"mensajeria:"`.

### AddStandardResilienceHandler
**Categoría:** Resiliencia / Polly
**Definición:** Método de `Microsoft.Extensions.Http.Resilience` que agrega a un `IHttpClientBuilder` una pipeline de resiliencia estándar compuesta por: Rate Limiter, Total Timeout, Retry con exponential backoff + jitter, Circuit Breaker y Attempt Timeout. Todos los parámetros son configurables desde `appsettings.json`.
**En el proyecto:** Es el mecanismo subyacente que usa `AddCoreResilience`. Cubre todas las llamadas al canal Meta API (WhatsApp, Instagram) con políticas razonables para producción sin necesidad de configurar cada política por separado.

### AddTenantPropagation
**Categoría:** Multi-tenancy / HTTP
**Definición:** Extension method del back-template (`HttpClientBuilderEx.cs`) que agrega el `TenantPropagationHttpMessageHandler` al pipeline del `HttpClient`. Su función es leer el `tenantId` del contexto actual e inyectarlo como header `X-Tenant-Id` en cada request HTTP saliente de forma transparente.
**En el proyecto:** Útil cuando el sistema de mensajería se integre con un servicio de notificaciones externo o con microservicios internos extraídos, garantizando que el tenant viajen en todas las llamadas sin escribir código adicional en los clientes.

### AgentSessionCleanupService
**Categoría:** Background Services
**Definición:** Implementación de `BackgroundService` del módulo `Agents` que ejecuta periódicamente (cada hora mediante `PeriodicTimer`) la eliminación de sesiones de agentes expiradas. Sigue el patrón de crear un scope por ciclo de trabajo para acceder a `IAgentSessionRepository` desde un contexto Singleton.
**En el proyecto:** Mantiene la base de datos limpia de registros de sesión obsoletos, evitando que agentes desconectados aparezcan como activos en la bandeja de conversaciones del sistema de mensajería.

### appsettings.json
**Categoría:** Configuración
**Definición:** Archivo de configuración estándar de ASP.NET Core que provee pares clave-valor en formato JSON. Las secciones se mapean a clases de opciones o se leen con `IConfiguration`. Permite modificar parámetros de comportamiento (conexiones, TTLs, políticas de resiliencia) sin recompilar el código.
**En el proyecto:** Contiene las secciones `Redis:Connection`, `MetaApi:BaseUrl` y `MetaApi:Resilience` que controlan, sin tocar el código, los parámetros de caché distribuido y las políticas de Polly para la Graph API.

### Attempt Timeout
**Categoría:** Resiliencia / Polly
**Definición:** Política de Polly que establece un tiempo máximo para cada intento individual de una operación, diferenciándose del Total Timeout que abarca todos los reintentos. Si un intento supera este límite, se cancela y se dispara el mecanismo de retry configurado.
**En el proyecto:** Incluido automáticamente en `AddStandardResilienceHandler`. Evita que un intento lento hacia la Meta API bloquee el thread indefinidamente, permitiendo que el retry actúe en tiempo útil.

---

## B

### BackgroundService
**Categoría:** Background Services
**Definición:** Clase base abstracta de .NET que implementa `IHostedService` internamente. Expone el método abstracto `ExecuteAsync(CancellationToken stoppingToken)` donde se coloca el bucle de trabajo continuo. El host lanza `ExecuteAsync` en background al iniciar la aplicación y cancela el `stoppingToken` durante el apagado, esperando que el método termine limpiamente.
**En el proyecto:** Base de `WebhookProcessorService`, `ConversationCleanupService`, `AgentSessionCleanupService` y `CampaignDispatcherService`. Contrasta con `IHostedService` (para tareas de inicio únicas como `DatabaseInitializationService`).

### Backoff Exponencial
**Categoría:** Resiliencia / Polly
**Definición:** Estrategia de espera entre reintentos en la que el tiempo de espera crece exponencialmente con cada intento (por ejemplo, 2 s, 4 s, 8 s). Reduce la presión sobre el servicio externo al distribuir temporalmente los reintentos, a diferencia del retry lineal que golpea al servidor con la misma frecuencia.
**En el proyecto:** Configurado en la pipeline de Polly para las llamadas a Meta API. Combinado con jitter evita que múltiples instancias del CRM reintenten simultáneamente tras un pico de errores de la Graph API.

### BreakDuration
**Categoría:** Resiliencia / Circuit Breaker
**Definición:** Tiempo que el circuit breaker permanece en estado Open antes de pasar a Half-Open para probar si el servicio se recuperó. Durante este período todas las llamadas se rechazan inmediatamente con una excepción, sin llegar al servicio externo.
**En el proyecto:** Configurado en 30 segundos en el ejemplo del módulo. Si la Meta API cae, el CRM no agota recursos enviando requests fallidas durante ese tiempo, protegiendo tanto el sistema propio como el servicio externo.

---

## C

### Cache-Aside
**Categoría:** Caching / Patrón
**Definición:** Patrón de caché en el que la aplicación (no la infraestructura) es responsable de: 1) intentar leer del caché, 2) si hay miss ir a la fuente de datos, 3) guardar el resultado en caché y 4) devolver el dato. Al escribir, la aplicación invalida explícitamente la entrada correspondiente.
**En el proyecto:** Implementado en handlers como `GetChannelsHandler` y `GetContactHandler`. Al hacer GET se consulta el caché primero; al hacer POST/PUT/DELETE en el handler correspondiente se llama a `RemoveAsync` o `EvictByTagAsync` para invalidar la entrada.

### Cache Key (Clave de Caché)
**Categoría:** Caching
**Definición:** Identificador único (string) que representa una entrada en el caché. Una clave mal diseñada puede hacer que distintos contextos compartan el mismo dato (colisión) o que en aplicaciones multi-tenant un workspace vea datos de otro (filtración). La clave debe incluir todos los parámetros que hacen única la respuesta.
**En el proyecto:** Las claves siguen el patrón `"entidad:{tenantId}:{id}"`. Ejemplos: `$"channels:{tenantId}"`, `$"contact:{tenantId}:{publicId}"`, `$"conversations:{tenantId}:{agentId}:{status}"`. Incluir siempre el `tenantId` es regla de seguridad.

### Cache Miss
**Categoría:** Caching
**Definición:** Situación en la que la clave buscada no existe en el caché (expiró, fue invalidada o es la primera vez que se solicita). Al ocurrir un miss, la aplicación debe ir a la fuente de datos original (base de datos, API externa) y, opcionalmente, guardar el resultado en el caché para futuros hits.
**En el proyecto:** En `GetChannelsHandler`, si `GetAsync` devuelve `null` se ejecuta la consulta a PostgreSQL mediante `IChannelRepository`, se mapea a `ChannelDto` y se guarda en caché con TTL de 10 minutos.

### CampaignDispatcherService
**Categoría:** Background Services
**Definición:** Implementación de `BackgroundService` del módulo `Campaigns` que verifica cada minuto (mediante `PeriodicTimer`) si existen campañas programadas cuya hora de envío ya llegó, las despacha a través de `IMessageSender` y las marca como enviadas. Crea un scope por ciclo para acceder a `ICampaignRepository` e `IMessageSender`.
**En el proyecto:** Habilita el envío masivo programado de mensajes (campañas de marketing o notificaciones) a contactos del workspace sin necesidad de un request HTTP explícito, usando solo el loop interno de la API.

### CancellationToken
**Categoría:** Async / Background Services
**Definición:** Mecanismo de .NET para cooperativamente cancelar operaciones asíncronas. En background services, el host proporciona un `stoppingToken` que se cancela cuando la aplicación se está apagando. Toda operación async dentro de `ExecuteAsync` debe recibir y respetar este token para permitir un apagado limpio.
**En el proyecto:** Se pasa a `PeriodicTimer.WaitForNextTickAsync`, a todos los métodos de repositorio, al mediator y a los métodos del caché. Sin él, el proceso podría tardar más de `ShutdownTimeout` en apagarse y ser terminado abruptamente.

### Captive Dependency
**Categoría:** DI / Background Services
**Definición:** Anti-patrón en el que un servicio de vida larga (Singleton, como un `BackgroundService`) inyecta directamente en su constructor un servicio de vida corta (Scoped, como un repositorio o `DbContext`). El resultado es que el servicio Scoped se "captura" y nunca se libera, causando datos obsoletos, DbContext compartido entre ciclos y posibles fugas de memoria.
**En el proyecto:** El error más común al crear `BackgroundService` en el CRM. La solución correcta es inyectar `IServiceScopeFactory` y crear un scope nuevo en cada ciclo de `DoWorkAsync`.

### Channel\<T\> (System.Threading.Channels)
**Categoría:** Background Services / Concurrencia
**Definición:** Estructura de datos de .NET para comunicación productor-consumidor asíncrona en memoria. El productor escribe con `Writer.WriteAsync` y el consumidor lee con `Reader.ReadAllAsync`. Es más eficiente que una cola concurrente porque está diseñado para patrones async/await y permite backpressure configurable.
**En el proyecto:** Usado en `WebhookProcessorService`. El endpoint `WebhookController` escribe el payload recibido en el `Channel<IncomingWebhook>` y responde `200 OK` en microsegundos; el background service lee y procesa los webhooks de forma desacoplada.

### Circuit Breaker
**Categoría:** Resiliencia / Polly
**Definición:** Patrón de resiliencia que monitorea la tasa de fallos en una ventana de tiempo. Cuando el porcentaje de fallos supera un umbral (`FailureRatio`) con un volumen mínimo (`MinimumThroughput`), el circuito se "abre" y todas las llamadas siguientes se rechazan inmediatamente sin llegar al servicio externo. Tras `BreakDuration`, pasa a Half-Open para probar la recuperación.
**En el proyecto:** Protege las llamadas a la Meta API (WhatsApp/Instagram). Si la Graph API está caída, el CRM deja de acumular threads esperando respuestas y puede responder rápido con un error controlado, en lugar de agotar el pool de conexiones.

### ConversationCleanupService
**Categoría:** Background Services
**Definición:** Implementación de `BackgroundService` del módulo `Conversations` que ejecuta cada hora la lógica de archivar conversaciones inactivas por más de 30 días. Delega en `IConversationRepository.ArchiveInactiveAsync` y registra en log el número de registros archivados.
**En el proyecto:** Mantiene la bandeja de conversaciones del CRM limpia y eficiente, moviendo automáticamente conversaciones antiguas a un estado archivado sin intervención manual del agente ni del administrador del workspace.

---

## D

### DatabaseInitializationService
**Categoría:** Background Services / Back-Template
**Definición:** Hosted service incluido en el back-template real (`Shared/Database/ServiceCollectionEx.cs`) que implementa `IHostedService`. En `StartAsync` crea un scope, establece el `TenantContext` a `"0"` (tenant neutro) y llama a `EnsureCreatedAsync` sobre el `AppDbContext`. Sirve como referencia de patrón para otros hosted services.
**En el proyecto:** Modelo de referencia para el `CacheWarmupService` y cualquier otra tarea de inicialización. Muestra cómo resolver servicios Scoped desde un Singleton usando `IServiceScopeFactory`.

### DelegatingHandler
**Categoría:** HTTP / Polly
**Definición:** Clase base de .NET para middleware HTTP en el pipeline de `HttpClient`. Permite interceptar requests y responses antes o después de que lleguen al handler real (que hace el I/O de red). Se encadenan en orden, similar a los middlewares de ASP.NET Core.
**En el proyecto:** `TenantPropagationHttpMessageHandler` hereda de `DelegatingHandler` para agregar el header `X-Tenant-Id` a cada request saliente sin modificar el código del cliente HTTP (`IMetaApiClient`, `INotificationClient`).

### DistributedCacheEntryOptions
**Categoría:** Caching / Redis
**Definición:** Clase que configura las opciones de expiración para una entrada en `IDistributedCache`. Expone `AbsoluteExpirationRelativeToNow` (expiración absoluta contada desde ahora), `AbsoluteExpiration` (fecha exacta) y `SlidingExpiration` (expiración deslizante). Si se omite, la entrada persiste indefinidamente.
**En el proyecto:** Usada en `DistributedCacheService.SetAsync` con `AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)`, garantizando siempre un TTL por defecto aunque el caller no especifique uno.

### DistributedCacheService
**Categoría:** Caching
**Definición:** Implementación de `ICacheService` que envuelve `IDistributedCache` (Redis) y gestiona la serialización/deserialización JSON con `System.Text.Json`. Expone `GetAsync<T>`, `SetAsync<T>` y `RemoveAsync` como métodos genéricos y asíncronos, ocultando los bytes crudos de Redis al resto de la aplicación.
**En el proyecto:** Registrada como `ICacheService` cuando el sistema de mensajería opera con múltiples instancias (escala horizontal). Reemplaza a `MemoryCacheService` sin cambiar el código de los handlers.

---

## E

### EvictByTagAsync
**Categoría:** Output Caching
**Definición:** Método de `IOutputCacheStore` que invalida todas las respuestas HTTP cacheadas que fueron etiquetadas con un tag específico, sin necesidad de conocer las claves exactas. Permite invalidación masiva y semántica (por dominio de datos, no por URL).
**En el proyecto:** En `CreateChannelHandler` se llama `await _outputCache.EvictByTagAsync("channels", ct)` tras crear un canal, invalidando simultáneamente todas las respuestas cacheadas del endpoint `GET /api/channels` de todos los tenants.

### ExecuteAsync
**Categoría:** Background Services
**Definición:** Método abstracto de `BackgroundService` que debe ser implementado por las subclases. Contiene el bucle principal del servicio. El host lo lanza en background (no bloquea el arranque de la aplicación) y cancela el `stoppingToken` cuando comienza el apagado. Si `ExecuteAsync` lanza una excepción no manejada, el host la registra pero el proceso puede continuar.
**En el proyecto:** Contiene el `PeriodicTimer` o el loop de lectura del `Channel<T>` en cada background service del CRM. Siempre pasa el `stoppingToken` a `WaitForNextTickAsync` para salir limpiamente.

### Exponential Backoff
**Categoría:** Resiliencia / Polly
**Definición:** Ver _Backoff Exponencial_.

---

## F

### FailureRatio
**Categoría:** Resiliencia / Circuit Breaker
**Definición:** Parámetro del circuit breaker que define el porcentaje de fallos necesario en la ventana de muestreo (`SamplingDuration`) para abrir el circuito. Un valor de `0.5` significa que si el 50% o más de las llamadas en la ventana fallan (y se supera `MinimumThroughput`), el circuito se abre.
**En el proyecto:** Configurado en 0.5 en el `ResiliencePipelineBuilder` del módulo. Calibrado para que errores esporádicos de la Meta API no abran el circuito prematuramente, pero fallos sostenidos sí lo hagan.

---

## G

### GetStringAsync / SetStringAsync
**Categoría:** Caching / Redis
**Definición:** Par de métodos de `IDistributedCache` que trabajan con strings UTF-8 en lugar de arrays de bytes crudos. Son el par más común para datos serializados como JSON. Internamente convierten el string a bytes antes de enviarlo a Redis.
**En el proyecto:** Usados en ejemplos de acceso directo a Redis para obtener y guardar `ChannelDto` serializado como JSON. En la implementación final del CRM se encapsulan dentro de `DistributedCacheService` para no dispersar la lógica de serialización.

### Graceful Shutdown
**Categoría:** Background Services
**Definición:** Proceso de apagado limpio en el que el host de .NET notifica a todos los hosted services (cancela el `stoppingToken`) y espera un tiempo configurable (`ShutdownTimeout`, por defecto 5 s) para que terminen su trabajo en curso antes de forzar el cierre del proceso.
**En el proyecto:** Se amplía a 30 segundos con `Configure<HostOptions>` en `Program.cs` porque `CampaignDispatcherService` puede estar en medio de un despacho masivo que tarda más de 5 segundos en completarse.

---

## H

### Half-Open (Estado)
**Categoría:** Resiliencia / Circuit Breaker
**Definición:** Estado intermedio del circuit breaker entre Open y Closed. Tras `BreakDuration`, el breaker permite una sola llamada de prueba. Si tiene éxito, el circuito vuelve a Closed; si falla, vuelve a Open por otro período. Evita que el sistema intente recuperarse prematuramente sobrecargando un servicio todavía inestable.
**En el proyecto:** Fase de prueba automática tras un período de caída de la Meta API. Permite que el CRM retome el envío de mensajes sin intervención manual del equipo de operaciones.

### HttpClientBuilder
**Categoría:** HTTP / DI
**Definición:** Interfaz de .NET que permite configurar un `HttpClient` tipado con middlewares, políticas de resiliencia y configuración de base address antes de que el `HttpClientFactory` lo construya. Se encadena con métodos como `AddHttpMessageHandler`, `AddStandardResilienceHandler` y `AddCoreResilience`.
**En el proyecto:** Punto de entrada para configurar `IMetaApiClient` y `INotificationClient` en los `ServiceCollectionEx` de sus módulos respectivos, aplicando resiliencia y propagación de tenant de forma declarativa.

### HostOptions
**Categoría:** Background Services / Configuración
**Definición:** Clase de opciones del host de .NET que controla parámetros globales del proceso, entre ellos `ShutdownTimeout`. Se configura con `services.Configure<HostOptions>(...)` en `Program.cs`.
**En el proyecto:** Se usa para extender el `ShutdownTimeout` a 30 segundos, dando tiempo suficiente a `CampaignDispatcherService` para terminar despachos en curso antes de que el proceso de API sea terminado por el orchestrador (Docker, Kubernetes).

---

## I

### ICacheService
**Categoría:** Caching / Abstracción
**Definición:** Interfaz propia del proyecto (no de .NET) que abstrae las operaciones de caché: `GetAsync<T>`, `SetAsync<T>` y `RemoveAsync`. Permite intercambiar `MemoryCacheService` por `DistributedCacheService` cambiando únicamente el registro en el contenedor DI, sin modificar los handlers.
**En el proyecto:** Inyectada en handlers como `GetChannelsHandler` y `GetContactHandler`. El registro en `CacheExtensions.cs` determina qué implementación concreta se usa (memoria o Redis) según la fase del producto.

### IDistributedCache
**Categoría:** Caching
**Definición:** Interfaz estándar de .NET (`Microsoft.Extensions.Caching.Distributed`) para caché distribuido. Define operaciones con bytes crudos y strings. Puede ser implementada por Redis (via `StackExchange.Redis`), SQL Server, NCache u otros backends intercambiables.
**En el proyecto:** Implementación subyacente de `DistributedCacheService`. Al migrar de IMemoryCache a Redis, solo cambia el registro en `CacheExtensions.cs`; el resto del código permanece igual gracias a la abstracción `ICacheService`.

### IdempotencyKey
**Categoría:** Resiliencia / API Design
**Definición:** Identificador único incluido en una request que permite al servidor detectar si ya procesó esa operación y devolver la respuesta anterior sin ejecutar la acción de nuevo. Hace seguro reintentar operaciones no idempotentes como INSERT o envío de mensajes.
**En el proyecto:** Se incluye como `message.PublicId.ToString()` en las llamadas a `IMetaApiClient.SendMessageAsync`. Si Polly reintenta el envío tras un error de red, la Meta API reconoce el `IdempotencyKey` y no entrega el mensaje al contacto por segunda vez.

### IHostedService
**Categoría:** Background Services
**Definición:** Interfaz de .NET con dos métodos: `StartAsync(CancellationToken)` llamado al iniciar el host, y `StopAsync(CancellationToken)` llamado al apagar. Pensada para tareas únicas de inicialización, no para loops continuos. `BackgroundService` implementa esta interfaz añadiendo el patrón de loop.
**En el proyecto:** Implementada directamente por `DatabaseInitializationService` (back-template) y `SchemaMigrationHostedService` (Common.PostgreSql), ambos para tareas de arranque únicas. Los servicios con loop usan `BackgroundService`.

### IMediator
**Categoría:** Arquitectura / Mediator
**Definición:** Interfaz que desacopla el emisor de un comando/query de su handler. El emisor llama a `Send(request, ct)` sin conocer qué clase lo procesa; el mediator resuelve y ejecuta el handler correcto. En el back-template se usa `Common.Messaging.IMediator`.
**En el proyecto:** Usado en `WebhookProcessorService` para despachar `ProcessWebhookCommand` dentro de un scope temporal, manteniendo el background service desacoplado de los detalles de procesamiento del webhook.

### IMemoryCache
**Categoría:** Caching
**Definición:** Interfaz estándar de .NET para caché en memoria del proceso. Almacena objetos arbitrarios (no requiere serialización) con acceso en nanosegundos. No persiste entre reinicios de la aplicación y no es compartido entre múltiples instancias del proceso.
**En el proyecto:** Implementación inicial recomendada para el sistema de mensajería mientras opera con una sola instancia. Al escalar horizontalmente se migra a Redis cambiando únicamente el registro en `CacheExtensions.cs`.

### IOutputCacheStore
**Categoría:** Output Caching
**Definición:** Interfaz de `Microsoft.AspNetCore.OutputCaching` que permite invalidar el output cache programáticamente desde el código de la aplicación. Expone `EvictByTagAsync(tag, ct)` para invalidar por etiqueta y otros métodos para invalidar por clave específica.
**En el proyecto:** Inyectada en `CreateChannelHandler` para poder invalidar las respuestas cacheadas del endpoint `GET /api/channels` cuando se crea o modifica un canal, asegurando que los clientes reciban la lista actualizada.

### IServiceScopeFactory
**Categoría:** DI / Background Services
**Definición:** Factoría del contenedor DI de .NET que permite crear `IServiceScope` (o `IAsyncDisposable` scope) en cualquier momento. Fundamental en Singletons que necesitan resolver servicios Scoped, ya que el scope creado tiene su propio ciclo de vida y libera los servicios al hacer dispose.
**En el proyecto:** Inyectada en todos los `BackgroundService` del CRM como única vía correcta de acceder a `IConversationRepository`, `IAgentSessionRepository`, `ICampaignRepository` e `IMediator`, todos ellos servicios Scoped.

---

## J

### Jitter
**Categoría:** Resiliencia / Polly
**Definición:** Ruido aleatorio añadido al tiempo de espera entre reintentos para evitar que múltiples clientes que fallaron simultáneamente reintenten también simultáneamente (tormenta de reintentos). Con jitter, los reintentos se distribuyen en el tiempo y el servicio puede recuperarse gradualmente.
**En el proyecto:** Activado con `UseJitter = true` en `RetryStrategyOptions` y por defecto en `AddStandardResilienceHandler`. Crítico cuando muchos workspaces del CRM hagan llamadas concurrentes a Meta API y esta responda con errores transitorios.

### JsonSerializerOptions
**Categoría:** Serialización
**Definición:** Clase de `System.Text.Json` que configura el comportamiento del serializador JSON: case sensitivity, naming policy, converters, etc. Declarar una instancia estática readonly evita allocations innecesarias al serializar/deserializar en hot paths.
**En el proyecto:** Declarada como `private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true }` en `DistributedCacheService` para ser reutilizada en cada operación de serialización hacia Redis.

---

## M

### MemoryCacheEntryOptions
**Categoría:** Caching
**Definición:** Clase que configura las propiedades de una entrada en `IMemoryCache`: tipo de expiración (absoluta o deslizante), prioridad de evicción, callbacks al expirar y límites de tamaño. Se crea por cada entrada a guardar en caché.
**En el proyecto:** Usada en `MemoryCacheService.Set<T>` para aplicar expiración deslizante de 5 minutos por defecto, o absoluta cuando el caller especifica un `TimeSpan`.

### MemoryCacheService
**Categoría:** Caching
**Definición:** Implementación de `ICacheService` que envuelve `IMemoryCache`. Expone `TryGet<T>`, `Set<T>` y `Remove`. Al ser en memoria, opera en nanosegundos y no requiere serialización, pero los datos no son compartidos entre instancias ni persisten ante reinicios.
**En el proyecto:** Implementación recomendada para la fase inicial del sistema de mensajería (una sola instancia de API). Sustituida por `DistributedCacheService` al escalar horizontalmente, sin cambiar el código de los handlers.

### MetaApiClient / IMetaApiClient
**Categoría:** HTTP / Canales
**Definición:** Cliente HTTP tipado que encapsula todas las llamadas a la Graph API de Meta (Facebook, WhatsApp, Instagram). Se registra como cliente tipado con `AddHttpClient<IMetaApiClient, MetaApiClient>` y recibe la `BaseAddress`, `Timeout`, y las políticas de resiliencia de forma transparente.
**En el proyecto:** Principal punto de integración del CRM con la plataforma de mensajería. Todos los envíos de mensajes, consultas de estado y operaciones sobre la API de WhatsApp/Instagram pasan por este cliente.

### Microsoft.Extensions.Http.Resilience
**Categoría:** Resiliencia / NuGet
**Definición:** Paquete NuGet oficial de Microsoft (disponible para .NET 8+) que integra Polly v8 con el pipeline de `IHttpClientBuilder`. Proporciona `AddStandardResilienceHandler` y otros helpers que eliminan la necesidad de configurar Polly manualmente para casos comunes.
**En el proyecto:** Referenciado en `Channels.Infrastructure.csproj` con versión `9.*`. Su presencia en el back-template como `HttpClientBuilderEx` lo convierte en la forma estándar de agregar resiliencia a cualquier `HttpClient` del CRM.

### MinimumThroughput
**Categoría:** Resiliencia / Circuit Breaker
**Definición:** Parámetro del circuit breaker que define el número mínimo de requests que deben haberse realizado en la `SamplingDuration` antes de evaluar si abrir el circuito. Evita que el circuito se abra por un pequeño número de fallos cuando el volumen de tráfico es bajo.
**En el proyecto:** Configurado en 10 en el ejemplo del módulo. Si el CRM tiene poco tráfico hacia Meta API en un período (madrugada, fines de semana), 2 fallos no abrirán el circuito; se requieren al menos 10 requests para que `FailureRatio` sea significativo.

---

## O

### OnClosed / OnOpened
**Categoría:** Resiliencia / Circuit Breaker
**Definición:** Callbacks del circuit breaker que se ejecutan cuando el estado cambia. `OnOpened` se dispara al abrirse el circuito (demasiados fallos) y `OnClosed` al cerrarse (recuperación exitosa). Permiten emitir logs estructurados, métricas y alertas al monitoreo.
**En el proyecto:** Usados con `_logger.LogError` (en `OnOpened`) y `_logger.LogInformation` (en `OnClosed`) para que el equipo de operaciones vea en los logs de la API cuándo la Meta API está caída y cuándo se recuperó.

### OnRetry
**Categoría:** Resiliencia / Polly
**Definición:** Callback de la estrategia de retry de Polly que se ejecuta antes de cada reintento. Recibe el número de intento, el delay aplicado y la causa del fallo (`args.Outcome.Exception`). Ideal para logging estructurado que permita auditar la frecuencia de fallos transitorios.
**En el proyecto:** Configurado para registrar con `LogWarning` el número de reintento, el delay en segundos y el tipo de excepción. Permite detectar patrones de inestabilidad en la Meta API antes de que el circuit breaker se abra.

### Output Cache
**Categoría:** Caching / HTTP
**Definición:** Mecanismo de ASP.NET Core (disponible desde .NET 7) que almacena la respuesta HTTP completa (headers + body) antes de enviarla al cliente. Las requests posteriores con los mismos parámetros reciben la respuesta cacheada sin ejecutar el handler ni el mediator. Configurable por políticas con tags, vary-by-header y vary-by-query.
**En el proyecto:** Aplicado al endpoint `GET /api/channels` mediante el atributo `[OutputCache(PolicyName = "channels-list")]`. Cachea la respuesta durante 5 minutos variando por `X-Tenant-Id`, devolviendo la lista correcta de canales por workspace sin tocar la base de datos.

### OutputCacheExtensions
**Categoría:** Output Caching
**Definición:** Extension method estático (`AddOutputCacheServices`) que centraliza la configuración de todas las políticas de output cache, siguiendo el mismo patrón modular del back-template. Define políticas reutilizables con nombre como `"channels-list"` y `"per-tenant"`.
**En el proyecto:** Registrado en `Program.cs` junto a los demás extension methods de módulo. Mantiene `Program.cs` limpio delegando la configuración de output cache a este helper.

---

## P

### PeriodicTimer
**Categoría:** Background Services
**Definición:** Clase de .NET 6+ (en `System.Threading`) para crear timers periódicos que se integran nativamente con `async/await`. A diferencia de `System.Timers.Timer`, no acumula ticks perdidos y `WaitForNextTickAsync` devuelve `false` cuando el token de cancelación es cancelado, facilitando el exit limpio del loop.
**En el proyecto:** Usado en `ConversationCleanupService` (cada hora), `AgentSessionCleanupService` (cada hora) y `CampaignDispatcherService` (cada minuto). Es el patrón preferido sobre `Task.Delay` en loops de background services.

### Pipeline de Resiliencia
**Categoría:** Resiliencia / Polly
**Definición:** Cadena ordenada de políticas de Polly que se aplican en secuencia a cada operación. El orden importa: la política más externa envuelve a las internas. En `AddStandardResilienceHandler` el orden es: Rate Limiter -> Total Timeout -> Retry -> Circuit Breaker -> Attempt Timeout.
**En el proyecto:** `AddCoreResilience` aplica esta pipeline completa a las llamadas de `IMetaApiClient`. Si se necesita control fino (distinto orden, políticas adicionales) se usa `ResiliencePipelineBuilder` manualmente.

### Polly
**Categoría:** Resiliencia
**Definición:** Librería open-source de .NET para implementar políticas de resiliencia: retry, circuit breaker, timeout, rate limiter, fallback y hedging. En su versión v8 (integrada en `Microsoft.Extensions.Http.Resilience`) usa `ResiliencePipelineBuilder` y se integra nativamente con el sistema de `IHttpClientBuilder` de .NET.
**En el proyecto:** Presente en el CRM a través de `Microsoft.Extensions.Http.Resilience` para proteger todas las llamadas a Meta API. Las políticas se configuran desde `appsettings.json` sin recompilar el código.

---

## R

### Rate Limiter (Polly)
**Categoría:** Resiliencia / Polly
**Definición:** Política de Polly que limita el número de requests salientes por unidad de tiempo, evitando que la aplicación sobrecargue un servicio externo. Diferente al rate limiting de ASP.NET Core (que limita requests entrantes); este limita las salidas.
**En el proyecto:** Primera capa en la pipeline de `AddStandardResilienceHandler`. Protege la Meta API de ser inundada con requests del CRM, lo que podría resultar en bloqueo de la cuenta de Graph API por parte de Meta.

### ReadAllAsync (Channel\<T\>)
**Categoría:** Background Services / Concurrencia
**Definición:** Método del `ChannelReader<T>` que devuelve un `IAsyncEnumerable<T>`, permitiendo consumir ítems del canal con `await foreach` a medida que se producen. El bucle termina automáticamente cuando el canal se marca como completo o cuando el `CancellationToken` es cancelado.
**En el proyecto:** Usado en el loop principal de `WebhookProcessorService` para procesar mensajes del `Channel<IncomingWebhook>` a medida que llegan desde el endpoint de WhatsApp, sin polling ni sleep.

### Redis
**Categoría:** Caching / Infraestructura
**Definición:** Base de datos en memoria de estructura clave-valor, open-source, usada ampliamente como caché distribuido, message broker y store de sesiones. Persiste datos en disco (AOF/RDB), soporta expiración nativa de claves y puede replicarse. El cliente de .NET más usado es `StackExchange.Redis`.
**En el proyecto:** Backend del `IDistributedCache` cuando el sistema de mensajería escala a múltiples instancias. Prefijo `"mensajeria:"` en todas las claves para evitar colisiones con otras aplicaciones en el mismo servidor Redis.

### RedisInsight
**Categoría:** Caching / Herramientas
**Definición:** Interfaz gráfica oficial de Redis Labs para visualizar, debuggear y monitorear instancias de Redis. Permite ver claves, valores, TTLs, métricas de memoria, slow log y el flujo de comandos en tiempo real.
**En el proyecto:** Herramienta recomendada para el equipo de desarrollo durante pruebas locales con Docker Compose, para verificar que las claves del CRM siguen el patrón correcto y que los TTLs se aplican como esperado.

### ResiliencePipeline / ResiliencePipelineBuilder
**Categoría:** Resiliencia / Polly
**Definición:** API fluida de Polly v8 para construir pipelines de resiliencia con control total sobre cada política. `ResiliencePipelineBuilder<T>` acepta un tipo de resultado genérico para políticas condicionadas al tipo de respuesta (ej. `HttpResponseMessage`). Alternativa a `AddStandardResilienceHandler` cuando se necesita configuración más granular.
**En el proyecto:** Usado en el ejemplo `MetaApiResiliencePipeline.cs` del módulo para mostrar cómo construir una pipeline con condiciones específicas (reintentar solo en `HttpRequestException` o status >= 500) que `AddStandardResilienceHandler` no permite expresar directamente.

### Retry
**Categoría:** Resiliencia / Polly
**Definición:** Política de Polly que reintenta automáticamente una operación fallida un número máximo de veces (`MaxRetryAttempts`), con un tipo de backoff configurable (lineal, exponencial). Debe combinarse con condiciones (`ShouldHandle`) para reintentar solo los fallos transitorios y con jitter para evitar tormentas de reintentos.
**En el proyecto:** Configurado en la pipeline de `IMetaApiClient` para reintentar hasta 3 veces llamadas fallidas por errores HTTP 5xx o `HttpRequestException`. No se usa para operaciones de escritura sin `IdempotencyKey`.

### RetryStrategyOptions
**Categoría:** Resiliencia / Polly
**Definición:** Clase de configuración de la estrategia de retry en Polly v8. Contiene: `MaxRetryAttempts`, `BackoffType` (`Exponential`, `Linear`, `Constant`), `UseJitter`, `ShouldHandle` (predicate para decidir si reintentar) y `OnRetry` (callback de logging).
**En el proyecto:** Configurada con `MaxRetryAttempts = 3`, `BackoffType = Exponential`, `UseJitter = true` y `ShouldHandle` que filtra `HttpRequestException` y status codes >= 500 (errores de servidor de la Meta API).

---

## S

### SamplingDuration
**Categoría:** Resiliencia / Circuit Breaker
**Definición:** Ventana de tiempo en la que el circuit breaker acumula estadísticas de éxito/fallo para evaluar si abrir el circuito. Requests fuera de la ventana no cuentan. Un `SamplingDuration` muy corto hace el breaker sensible a picos momentáneos; muy largo lo hace lento para detectar degradación sostenida.
**En el proyecto:** Configurado en 60 segundos. La Meta API puede tener latencias altas breves; este valor evita que el circuit breaker se abra por ráfagas momentáneas de errores de menos de un minuto.

### Scoped (DI Lifetime)
**Categoría:** DI
**Definición:** Ciclo de vida del contenedor DI de .NET en el que se crea una nueva instancia por cada scope (normalmente equivalente a un request HTTP). Los repositorios y `DbContext` son típicamente Scoped porque cada request debe operar con su propia unidad de trabajo.
**En el proyecto:** `IConversationRepository`, `IAgentSessionRepository`, `ICampaignRepository` y `AppDbContext` son Scoped. No pueden inyectarse directamente en `BackgroundService` (Singleton); requieren `IServiceScopeFactory`.

### SchemaMigrationHostedService
**Categoría:** Background Services / Common
**Definición:** Hosted service del paquete `Common.PostgreSql` del back-template que ejecuta las migraciones de base de datos al arrancar la aplicación. Implementa `IHostedService` para ejecutarse una sola vez al inicio, similar a `DatabaseInitializationService`.
**En el proyecto:** Segundo hosted service "built-in" del template (junto a `DatabaseInitializationService`). Garantiza que el esquema de PostgreSQL esté siempre actualizado al desplegar una nueva versión del sistema de mensajería sin intervención manual.

### ShouldHandle
**Categoría:** Resiliencia / Polly
**Definición:** Predicate configurable en las estrategias de Polly (retry, circuit breaker) que decide si un resultado o excepción específico debe activar la política. Permite ser selectivos: reintentar errores de red pero no errores de validación (400), abrir el circuito solo por errores de servidor (5xx) pero no por errores del cliente (4xx).
**En el proyecto:** Configurado para reintentar `HttpRequestException` (fallo de red/DNS) y responses con `StatusCode >= 500` (errores de servidor de Meta API). Los errores 400/401/403 no se reintentan porque son errores del cliente CRM, no del servidor.

### ShutdownTimeout
**Categoría:** Background Services / Configuración
**Definición:** Tiempo máximo que el host de .NET espera a que todos los hosted services completen su `StopAsync` antes de forzar la terminación del proceso. El valor por defecto es 5 segundos. Se configura mediante `HostOptions.ShutdownTimeout`.
**En el proyecto:** Ampliado a 30 segundos en `Program.cs` para dar tiempo suficiente al `CampaignDispatcherService` de terminar el despacho en curso al escalar hacia abajo una instancia en el entorno de staging/producción.

### Singleton (DI Lifetime)
**Categoría:** DI
**Definición:** Ciclo de vida del contenedor DI de .NET en el que se crea una única instancia compartida por toda la vida de la aplicación. Los `BackgroundService`, `IMemoryCache`, `Channel<T>` y otros servicios de estado global son Singleton.
**En el proyecto:** `WebhookProcessorService`, `ConversationCleanupService`, `AgentSessionCleanupService`, `CampaignDispatcherService` y `WebhookChannel` son todos Singleton. No pueden recibir servicios Scoped directamente en su constructor.

### SlidingExpiration
**Categoría:** Caching
**Definición:** Tipo de expiración de caché en el que el TTL se reinicia con cada acceso a la entrada. Si no se accede al dato durante el período configurado, expira. Útil para datos de sesión activa donde la actividad del usuario debe mantener el dato vivo.
**En el proyecto:** Configurado en `MemoryCacheService.Set<T>` con `TimeSpan.FromMinutes(5)` como valor por defecto. Apropiado para el perfil del agente activo: mientras el agente sigue usando el CRM, su perfil permanece en caché; al desconectarse, expira naturalmente.

### StackExchange.Redis
**Categoría:** Redis / NuGet
**Definición:** Librería cliente oficial de Redis para .NET, desarrollada por Stack Overflow. Es la implementación que usa `Microsoft.Extensions.Caching.StackExchangeRedis` para `IDistributedCache`. Maneja multiplexing de conexiones, reconexión automática y pipelines de comandos.
**En el proyecto:** Depedencia transitiva de `Microsoft.Extensions.Caching.StackExchangeRedis`. El paquete de alto nivel es el que se referencia en `Channels.Infrastructure.csproj`; `StackExchange.Redis` se usa internamente.

### StoppingToken
**Categoría:** Background Services
**Definición:** `CancellationToken` específico que el host de .NET proporciona al método `ExecuteAsync` de un `BackgroundService`. Se cancela cuando el host comienza el proceso de apagado, señalizando al servicio que debe terminar su trabajo en curso y salir limpiamente.
**En el proyecto:** Pasado a `PeriodicTimer.WaitForNextTickAsync`, a todos los repositorios, al mediator y a los métodos del caché. Su cancelación hace que `WaitForNextTickAsync` retorne `false`, terminando el `while` loop limpiamente.

---

## T

### TenantContextAccessor / TenantContext
**Categoría:** Multi-tenancy
**Definición:** Par de clases del back-template que almacenan y exponen el `tenantId` del request actual a través del pipeline de ASP.NET Core. `TenantContext` contiene el ID y `ITenantContextAccessor` lo hace disponible en cualquier servicio inyectado.
**En el proyecto:** Usado por `TenantPropagationHttpMessageHandler` para leer el tenant del request actual y añadirlo como header en las llamadas salientes a la Meta API o a servicios internos, propagando el aislamiento de datos entre workspaces.

### TenantPropagationHttpMessageHandler
**Categoría:** HTTP / Multi-tenancy
**Definición:** `DelegatingHandler` del back-template que intercepta cada request HTTP saliente del `HttpClient`, lee el `tenantId` del contexto actual mediante `ITenantContextAccessor` y lo añade como header (`X-Tenant-Id`) configurado en `MultiTenantOptions.TenantHeaderName`.
**En el proyecto:** Aplicado a `IMetaApiClient` y a cualquier `HttpClient` registrado con `AddTenantPropagation()`. Garantiza que el tenant viaje en todas las llamadas HTTP sin código adicional en cada cliente.

### Timeout (Polly)
**Categoría:** Resiliencia / Polly
**Definición:** Política de Polly que cancela una operación si no termina en el tiempo configurado, lanzando una excepción de timeout. En la pipeline estándar hay dos niveles: `TotalRequestTimeout` (para toda la operación incluyendo reintentos) y `AttemptTimeout` (por intento individual).
**En el proyecto:** `TotalRequestTimeout` configurado en 30 segundos para las llamadas a Meta API. Evita que un request colgado bloquee un thread del pool indefinidamente, lo que degradaría el rendimiento general del CRM.

### TTL (Time To Live)
**Categoría:** Caching
**Definición:** Tiempo de vida de una entrada en el caché. Pasado el TTL, la entrada expira y el siguiente acceso genera un cache miss. Un TTL muy largo puede servir datos obsoletos; muy corto reduce el beneficio del caché. Debe calibrarse según la frecuencia de cambio del dato.
**En el proyecto:** Lista de canales: 10 minutos. Configuración del workspace: 30 minutos. Perfil de contacto: 5 minutos. Datos en tiempo real (mensajes activos, estado de conversaciones): sin caché.

### TotalRequestTimeout
**Categoría:** Resiliencia / Polly
**Definición:** Política de timeout en Polly que abarca todo el tiempo total de la operación, incluyendo todos los reintentos y los tiempos de espera entre ellos. Si el tiempo total supera este límite, la operación falla inmediatamente aunque haya reintentos pendientes.
**En el proyecto:** Configurado en 30 segundos en `appsettings.json` bajo `MetaApi:Resilience:TotalRequestTimeout:Timeout`. Garantiza que ninguna llamada a la Meta API, por lenta que sea o muchos reintentos que tenga, bloquee el sistema más de 30 segundos.

---

## V

### Vary-by Header / Vary-by Query
**Categoría:** Output Caching
**Definición:** Dimensiones del output cache que generan entradas separadas según el valor de un header HTTP o un parámetro query. Permiten que un mismo endpoint tenga respuestas cacheadas distintas por usuario, tenant, locale u otros parámetros sin codificar esa lógica en la clave manualmente.
**En el proyecto:** La política `"channels-list"` usa `SetVaryByHeader("X-Tenant-Id")` para que cada workspace tenga su propia entrada cacheada en `GET /api/channels`, y `SetVaryByQuery("*")` para que distintos filtros de query string generen entradas independientes.

---

## W

### WebhookChannel (Channel\<IncomingWebhook\>)
**Categoría:** Background Services / Concurrencia
**Definición:** Instancia Singleton de `Channel<IncomingWebhook>` que actúa como buffer entre el endpoint HTTP de webhooks (productor) y el `WebhookProcessorService` (consumidor). Registrada en el contenedor DI como `AddSingleton<WebhookChannel>` para ser inyectada en ambos lados.
**En el proyecto:** Permite que `WebhookController` responda a Meta API en < 500ms (escribir en el canal es casi instantáneo) mientras `WebhookProcessorService` procesa los payloads de WhatsApp/Instagram a su propio ritmo en background.

### WebhookProcessorService
**Categoría:** Background Services
**Definición:** Implementación de `BackgroundService` del módulo `Channels` que consume mensajes del `Channel<IncomingWebhook>` usando `ReadAllAsync` y los procesa enviando un `ProcessWebhookCommand` via `IMediator` dentro de un scope DI temporal. No usa `PeriodicTimer`; trabaja en modo continuo reactor.
**En el proyecto:** Componente central del pipeline de recepción de mensajes de WhatsApp e Instagram. Desacopla la velocidad de recepción de webhooks (impuesta por Meta) de la velocidad de procesamiento (limitada por la base de datos y APIs externas).

---

## X

### X-Tenant-Id
**Categoría:** Multi-tenancy / HTTP
**Definición:** Header HTTP personalizado que identifica el workspace (tenant) al que pertenece una request. Usado en el sistema de mensajería para propagar el contexto de tenant entre servicios. El nombre exacto del header es configurable en `MultiTenantOptions.TenantHeaderName`.
**En el proyecto:** Añadido automáticamente por `TenantPropagationHttpMessageHandler` en todas las llamadas HTTP salientes. También es el header por el que varían las respuestas en el output cache (`SetVaryByHeader("X-Tenant-Id")`), garantizando aislamiento de datos entre workspaces.
