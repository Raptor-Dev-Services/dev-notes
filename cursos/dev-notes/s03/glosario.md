# Glosario Técnico - S03

### C# Avanzado - Tour del Back-Template - Herramientas - Documentación de Repositorios

**Dev Notes**
*Glosario Completo de la Sesión*

---

## A

### AOF (Append Only File)
**Categoría:** Redis - Persistencia
Mecanismo de persistencia de Redis que registra cada operación de escritura en un log secuencial. Con `appendfsync everysec` garantiza un RPO de 1 segundo. Más lento de restaurar que RDB pero más granular. Recomendado para colas de mensajes donde no se puede perder trabajo.

**En el proyecto:** El Redis de producción usa AOF para la cola de campañas masivas (`List` de mensajes pendientes). El Redis de cache puede funcionar sin persistencia (se reconstruye desde PostgreSQL).

---

### API Gateway
**Categoría:** Arquitectura - Escalabilidad
Único punto de entrada desde el exterior hacia los microservicios. Centraliza routing, autenticación, rate limiting, SSL termination y logging. Ejemplos: NGINX, Kong, AWS API Gateway, YARP (.NET). En proyectos pequeños el módulo `Host.Api` actúa como gateway.

**En el proyecto:** En el sistema de mensajería, `Host.Api/Program.cs` registra todos los módulos y actúa como gateway interno. A escala se antepone NGINX o un ALB de AWS.

---

### Abstract class
**Categoría:** C# / POO
**Definición:** Clase que no se puede instanciar directamente - define estructura y comportamiento base para subclases. Puede tener miembros abstractos (sin implementación) y miembros concretos (con implementación). Sirve para forzar que las subclases implementen ciertos contratos.
**En el proyecto:** `BaseApiController` es una clase abstracta que provee el `Mediator` protegido a todos los controllers del sistema de mensajería. Ningún código instancia directamente un `BaseApiController`.

---

### Acceptance Criteria (Criterios de Aceptación)
**Categoría:** Agile / SDLC
**Definición:** Condiciones concretas y verificables que una historia de usuario debe cumplir para considerarse terminada. Definen el comportamiento esperado desde la perspectiva del usuario. Deben poder verificarse con una herramienta (curl, Postman, Scalar) sin ambigüedad.
**En el proyecto:** Cada user story del sistema de mensajería lleva criterios en formato Gherkin. Ejemplo: `POST /api/contacts` con email duplicado debe retornar `409 Conflict` - eso es un criterio de aceptación verificable.

---

### ADR (Architecture Decision Record)
**Categoría:** Arquitectura / Documentación
**Definición:** Documento corto que registra una única decisión de arquitectura: el contexto del momento, las opciones evaluadas, la decisión tomada y sus consecuencias. Los ADRs nunca se borran - se marcan como `Deprecado` o `Supersedido` cuando la decisión cambia.
**En el proyecto:** El repositorio del CRM mantiene `/docs/adr/`. Ejemplo: `ADR-001` documenta por qué se eligió Monolito Modular en lugar de microservicios; `ADR-002` documenta el modelo Pool de multi-tenancy.

---

### AGENTS.md
**Categoría:** AI Workflow / Documentación
**Definición:** Archivo Markdown en la raíz del repositorio que provee contexto estructurado para AI agents (Claude, Copilot, Cursor). Describe la arquitectura, los patrones obligatorios, las prohibiciones del proyecto y dónde encontrar las cosas. Previene que el AI cometa errores específicos del equipo.
**En el proyecto:** El sistema de mensajería tendrá un `AGENTS.md` que lista los patrones obligatorios (Result Pattern, multi-tenancy explícito), las prohibiciones (`async void`, `.Result`, `IRepository<T>` genérico) y la estructura de carpetas de los módulos.

---

### Alpha (pre-release)
**Categoría:** SemVer / DevOps
**Definición:** Primera etapa de pre-release de una versión. Indica que el software está en prueba interna y puede cambiar radicalmente sin previo aviso. Se expresa como sufijo en SemVer: `v1.0.0-alpha.1`. No se instala automáticamente por NuGet ni npm.
**En el proyecto:** El CRM podría publicar `v1.0.0-alpha.1` para pruebas internas entre los developers antes de abrir al primer cliente.

---

### appsettings.json
**Categoría:** .NET / Configuración
**Definición:** Archivo JSON que contiene la configuración de una aplicación .NET. Siempre presente en el repositorio y contiene valores base. Puede ser sobreescrito por `appsettings.{Env}.json`, User Secrets o variables de entorno. Nunca debe contener secretos reales.
**En el proyecto:** `appsettings.json` del CRM contiene la estructura de configuración (secciones `Jwt`, `ConnectionStrings`, `Redis`) pero con valores de ejemplo o vacíos. Los valores reales se inyectan en desarrollo mediante User Secrets y en producción mediante variables de entorno o AWS Secrets Manager.

---

### Async / Await
**Categoría:** C# / Concurrencia
**Definición:** Palabras clave de C# para programación asíncrona. `async` declara que un método puede suspender su ejecución; `await` suspende el método actual y libera el hilo mientras espera una operación (normalmente I/O). El hilo no se bloquea - queda disponible para otras solicitudes.
**En el proyecto:** Toda la cadena del CRM es async de punta a punta: `Controller → Mediator.Send → Handler.Handle → Repository → Sql/Dapper → PostgreSQL`. Si algún eslabón usa `.Result` o `.Wait()`, puede causar un deadlock en ASP.NET.

---

### Auto-scaling
**Categoría:** Cloud - Escalabilidad
Mecanismo del proveedor cloud para agregar o retirar instancias automáticamente según métricas (CPU, request count, queue length). Escala hacia arriba en picos y hacia abajo en baja demanda para optimizar costo. El cold start de una nueva instancia puede tardar 30-90 segundos.

**En el proyecto:** Configurado en el Auto Scaling Group de AWS ECS. Regla: escalar cuando CPU promedio supera 70% por 3 minutos.

---

### Availability Zone (AZ)
**Categoría:** Cloud - Alta Disponibilidad
Datacenter físicamente separado dentro de la misma región AWS/Azure. Conectado a otras AZs con fibra de baja latencia (<2ms). Si una AZ falla (incendio, inundación), las otras siguen operando. El despliegue mínimo recomendado para alta disponibilidad es 2 AZs.

**En el proyecto:** RDS Multi-AZ despliega el primary en us-east-1a y un standby sincrónico en us-east-1b. Failover automático en ~60 segundos.

---

## B

### Back Pressure
**Categoría:** Escalabilidad - Resiliencia
Señal del consumidor al productor para que frene la tasa de generación de trabajo. Evita que la cola crezca sin límite hasta agotar la memoria (OOM). Se implementa con semáforos, colas bounded, o respondiendo 503 Service Unavailable temporalmente.

**En el proyecto:** El BackgroundService de campañas tiene una cola bounded. Si la cola está llena, el endpoint de activación responde 503 en lugar de encolar infinitamente.

---

### Badge (CI badge)
**Categoría:** DevOps / Documentación
**Definición:** Imagen dinámica en el README que muestra el estado actual de un aspecto del proyecto: si el pipeline de CI está en verde o rojo, la versión actual, la cobertura de tests, la licencia. Se genera automáticamente con servicios como GitHub Actions o Codecov.
**En el proyecto:** El `README.md` del CRM tendrá badges de CI (verde/rojo visible desde GitHub sin entrar al repo), versión actual (actualizado con cada tag de SemVer) y cobertura de tests.

---

### Beta (pre-release)
**Categoría:** SemVer / DevOps
**Definición:** Segunda etapa de pre-release. El software se prueba con usuarios seleccionados. La API está casi fija - pueden ocurrir cambios pero son menores. Se expresa como `v1.0.0-beta.1`. Más estable que alpha pero no lista para producción general.
**En el proyecto:** Antes del lanzamiento al primer cliente real, el CRM podría publicar una versión `v1.0.0-beta.1` para que el equipo interno de la empresa valide el flujo completo.

---

### BREAKING CHANGE
**Categoría:** SemVer / Conventional Commits
**Definición:** Cambio en el código que rompe la compatibilidad hacia atrás - los clientes que usaban la versión anterior dejarán de funcionar sin modificar su código. En Conventional Commits se declara en el footer del commit message o con `!` después del tipo. Dispara un incremento MAJOR en SemVer.
**En el proyecto:** Renombrar el campo `fullName` a `displayName` en `ContactDto` es un BREAKING CHANGE porque cualquier cliente que deserialice la respuesta buscando `fullName` fallará. Se documenta como `feat(api)!: rename fullName to displayName` y dispara `v2.0.0`.

---

### Blocker
**Categoría:** Agile / SDLC
**Definición:** Impedimento que impide a un miembro del equipo avanzar con una tarea del sprint. Se menciona en el Daily y, si no se resuelve en 30 segundos, se agenda una reunión aparte. Visible en el Burndown Chart como días donde los puntos no bajan.
**En el proyecto:** En un Daily del CRM, un blocker típico sería: "No puedo hacer el webhook de WhatsApp porque necesito las credenciales de Meta for Developers y aún no las tenemos".

---

### Bridge (modelo de multi-tenancy)
**Categoría:** Arquitectura / Multi-tenancy
**Definición:** Modelo de multi-tenancy donde cada tenant tiene su propio schema dentro de la misma base de datos. Ofrece aislamiento medio: los datos están separados lógicamente pero comparten el motor de base de datos. Más costoso que Pool pero más barato que Silo.
**En el proyecto:** El CRM eligió el modelo Pool en lugar de Bridge (ver ADR-002) porque el Bridge requiere migraciones por schema - con muchos clientes esto se vuelve operativamente complejo.

---

### Build metadata (SemVer)
**Categoría:** SemVer
**Definición:** Información adicional anexada a una versión SemVer con el prefijo `+`. Es puramente informativa - no afecta la precedencia entre versiones. Ejemplo: `v1.0.0+build.2026.05.29.001`. Dos versiones con el mismo `MAJOR.MINOR.PATCH` pero distinta metadata se consideran iguales en términos de compatibilidad.
**En el proyecto:** El pipeline de CI/CD del CRM puede agregar el número de run o el SHA del commit como metadata: `v1.0.0+sha.abc1234`. En tags de Docker se omite para evitar confusión.

---

### Burndown Chart
**Categoría:** Agile / Scrum
**Definición:** Gráfica que muestra los puntos restantes en el sprint vs los días transcurridos. La línea ideal es una bajada lineal desde el total de puntos hasta cero al finalizar el sprint. Una curva plana varios días indica un blocker; una curva que sube indica que se agregaron stories sin remover otras (scope creep).
**En el proyecto:** El equipo del CRM usa el Burndown en Jira o GitHub Projects para detectar si el sprint está en riesgo antes del último día. Si la curva es plana dos días seguidos, se escala en la Retro.

---

## C

### Cache-Aside Pattern
**Categoría:** Redis - Patrones de Cache
Patrón donde el código de la aplicación decide cuándo leer de Redis y cuándo ir a la base de datos. Flujo: (1) intentar Redis, (2) si miss ir a DB, (3) guardar resultado en Redis con TTL. Es el patrón de cache más común y el de menor complejidad.

**En el proyecto:** Usado para cachear perfiles de contacto (`Hash`) y configuración de tenant (`Hash`). El Handler intenta Redis primero; si el resultado no está (miss), consulta PostgreSQL y lo guarda en Redis con TTL de 5-10 minutos.

---

### Cache Stampede
**Categoría:** Redis - Problemas de Cache
Condición de carrera donde múltiples peticiones simultáneas sufren un cache miss al mismo tiempo (típicamente cuando una clave TTL expira) y todas van a la base de datos simultáneamente, saturándola. Se mitiga con jitter en el TTL (variación aleatoria) o con un mutex en Redis (solo un proceso reconstruye el cache).

**En el proyecto:** El TTL de las conversaciones cacheadas incluye un jitter de ±60 segundos para distribuir las expiraciónes y evitar el stampede en las horas de mayor tráfico.

---

### C4 (modelo de diagramas)
**Categoría:** Arquitectura / Documentación
**Definición:** Método de diagramación de arquitecturas de software con cuatro niveles de abstracción: Nivel 1 (System Context - el sistema y sus usuarios/sistemas externos), Nivel 2 (Container - los procesos o bases de datos que componen el sistema), Nivel 3 (Component - los componentes dentro de un container) y Nivel 4 (Code - clases). El CRM documenta Nivel 1 y 2.
**En el proyecto:** `docs/architecture.md` contiene el diagrama C4 Nivel 1 mostrando el CRM y su relación con WhatsApp Business API, Instagram Graph API y Email/SMTP, y el Nivel 2 mostrando la SPA React, la API .NET, PostgreSQL y Redis.

---

### CancellationToken
**Categoría:** C# / Async
**Definición:** Estructura de C# que propaga una señal de cancelación a través de operaciones asíncronas. ASP.NET Core lo inyecta automáticamente en los métodos de Controller - si el cliente HTTP cierra la conexión, el token se cancela. Debe pasarse explícitamente en toda la cadena async para que PostgreSQL también cancele la query.
**En el proyecto:** Todos los métodos del CRM reciben `CancellationToken ct` como último parámetro: Controller -> Mediator -> Handler -> Repository -> Dapper -> PostgreSQL. Si no se propaga, la base de datos sigue trabajando aunque el cliente ya no espere la respuesta.

---

### CDN (Content Delivery Network)
**Categoría:** Escalabilidad - Frontend
Red de servidores distribuidos geográficamente que sirve contenido estático desde el nodo más cercano al usuario. Reduce latencia y carga en el servidor de origen. Ejemplos: CloudFront (AWS), Cloudflare, Azure CDN.

**En el proyecto:** El build de Vite genera assets con hash en el nombre (cache-busting automático). Se sirven desde CloudFront. Las respuestas de la API no van a CDN.

---

### CHANGELOG.md
**Categoría:** Documentación / SemVer
**Definición:** Archivo que registra todos los cambios notables del proyecto ordenados por versión. Sigue el formato `Keep a Changelog` con secciones: Agregado, Cambiado, Deprecado, Eliminado, Corregido, Seguridad. Va dirigido a usuarios y clientes - no a developers. Debe actualizarse en el mismo PR que introduce el cambio.
**En el proyecto:** El CHANGELOG del CRM registra qué módulos se lanzaron en cada sprint. Ejemplo: `[0.2.0] — 2026-05-15: Agregado — Módulo Contacts con CRUD completo y paginación cursor-based`.

---

### CI (Continuous Integration)
**Categoría:** DevOps / CI/CD
**Definición:** Práctica donde cada push o PR dispara automáticamente la compilación y ejecución de tests del proyecto. Detecta errores de integración lo antes posible. En el CRM, el pipeline CI corre `dotnet restore`, `dotnet build --warnaserror`, `dotnet test` y `dotnet format --verify-no-changes`.
**En el proyecto:** Ningún PR puede mergearse al CRM con el pipeline en rojo - ni aunque "el código se vea bien" o sea un test flaky. Esta es una regla absoluta del equipo.

---

### Circuit Breaker
**Categoría:** Resiliencia - Patrones
Patrón que evita llamadas repetidas a un servicio caído. Tiene tres estados: Closed (normal), Open (falla rápida sin llamar al servicio) y Half-Open (prueba si el servicio se recuperó). En .NET se implementa con Polly `AddCircuitBreaker`.

**En el proyecto:** Todos los clientes HTTP externos (WhatsApp API, email provider) usan circuit breaker. Si WhatsApp API cae, el circuit abre y el thread pool queda libre para otras peticiones.

---

### CODEOWNERS
**Categoría:** Git / GitHub
**Definición:** Archivo `.github/CODEOWNERS` que define qué usuarios o equipos son revisores automáticos de cada parte del repositorio según patrones de rutas. Cuando un PR toca archivos que coinciden con un patrón, GitHub asigna automáticamente a los owners como reviewers requeridos.
**En el proyecto:** El CRM tiene Dev A como owner global (`*`), Dev B como owner de `/src/Shared/Authentication/`, Dev C y Dev D como owners de `/frontend/`. Un PR que toca el frontend asigna a ambos automáticamente sin tener que acordarse.

---

### Complejidad Ciclomática (CC)
**Categoría:** Calidad de Código / Métricas
**Definición:** Métrica que cuenta la cantidad de caminos de ejecución independientes en un método. Se calcula contando los puntos de decisión (`if`, `else`, `for`, `while`, `case`, `&&`, `||`) y sumando 1. Umbrales: ≤ 10 aceptable, 11-15 monitorear, > 15 refactorizar.
**En el proyecto:** Un Handler del CRM con CC > 15 tiene demasiados casos edge para testear y mantener. El refactor habitual es extraer condiciones a clases con el patrón Strategy o a métodos auxiliares bien nombrados. CC = 7 está dentro del umbral aceptable - no refactorizar por refactorizar.

---

### Configuration (IConfiguration)
**Categoría:** .NET / Configuración
**Definición:** Interfaz de .NET para acceder a la configuración de la aplicación mediante claves string (`config["Jwt:Key"]`). Es el acceso de bajo nivel - sin tipado, sin validación automática, difícil de testear. Se prefiere `IOptions<T>` sobre `IConfiguration` directo para cualquier sección estructurada.
**En el proyecto:** En el CRM se usa `IConfiguration` solo en el arranque (`Program.cs`) para casos puntuales. Los services usan `IOptions<JwtSettings>`, `IOptions<WhatsAppSettings>`, etc.

---

### Conventional Commits
**Categoría:** Git / SemVer
**Definición:** Convención estándar para mensajes de commit con el formato `<tipo>[scope]: <descripción>`. Permite automatizar SemVer y generar el CHANGELOG automáticamente. Tipos principales: `feat` (MINOR), `fix` (PATCH), `feat!` o `BREAKING CHANGE` (MAJOR), `refactor`, `test`, `docs`, `ci`, `chore`, `perf`, `build`, `style`.
**En el proyecto:** El `CONTRIBUTING.md` del CRM hace Conventional Commits obligatorio. El equipo usa `feat(contacts): add search by phone` o `fix(auth): handle expired refresh token gracefully`. La herramienta `semantic-release` puede automatizar el bump de versión y el CHANGELOG a partir de estos commits.

---

### CONTRIBUTING.md
**Categoría:** Documentación / Git
**Definición:** Archivo que documenta cómo contribuir al proyecto: convenciones de branches, formato de commits, proceso de PR, SLA de review. Es el contrato del equipo - un nuevo developer lee esto antes de abrir su primer PR. Elimina preguntas repetitivas de onboarding.
**En el proyecto:** El `CONTRIBUTING.md` del CRM documenta el formato `tipo/numero-issue/descripcion-corta`, el uso obligatorio de Conventional Commits, que el pipeline debe estar verde antes de mergear y que el SLA de review es de 4 horas laborales.

---

### Cursor-based pagination (paginación cursor-based)
**Categoría:** Backend / API Design
**Definición:** Técnica de paginación donde en lugar de usar `OFFSET` (número de página), se usa un cursor opaco que apunta a un registro específico. Más eficiente en bases de datos grandes porque no requiere contar y saltar registros. El cliente recibe el cursor del último registro y lo envía en la siguiente petición.
**En el proyecto:** El módulo `Contacts` del CRM usa paginación cursor-based en `GET /api/contacts` para que el listado sea eficiente incluso con miles de contactos por workspace.

---

## D

### Daily (Daily Scrum / Daily Standup)
**Categoría:** Agile / Scrum
**Definición:** Ceremonia diaria de máximo 15 minutos donde cada miembro del equipo responde: ¿qué hice ayer? ¿qué haré hoy? ¿tengo algún blocker? No es una reunión de status para el manager - es sincronización entre pares. Los blockers que no se resuelven en 30 segundos se agenda una reunión aparte.
**En el proyecto:** El equipo del CRM hace Daily de lunes a viernes. Si Dev B tiene un blocker con las credenciales de WhatsApp, el Daily lo hace visible - pero la solución se busca fuera del Daily para no bloquear a todos.

---

### Dapper
**Categoría:** .NET / Bases de Datos
**Definición:** Micro-ORM para .NET que extiende `IDbConnection` con métodos genéricos para ejecutar SQL y mapear resultados a objetos. Más explícito que EF Core - el developer escribe el SQL directamente. Útil para queries de lectura complejas o que requieren control total del SQL generado.
**En el proyecto:** En el back-template del CRM, las clases `...Sql` en Infrastructure usan Dapper para ejecutar queries SQL tipadas: `db.QuerySingleAsync<UserProfile>(sql, new { publicId, tenantId })`. Los repositorios que hacen escrituras pueden usar EF Core.

---

### Deadlock (async)
**Categoría:** C# / Async
**Definición:** Situación de bloqueo donde un hilo espera por un resultado que nunca llegará porque la continuación de la operación async necesita ese mismo hilo. Ocurre en ASP.NET cuando se usa `.Result` o `.Wait()` en código asíncrono. El hilo queda bloqueado esperando el resultado, que a su vez espera al hilo liberado - ciclo infinito.
**En el proyecto:** Regla absoluta en el CRM: nunca usar `.Result` ni `.Wait()` en código de Controllers, Handlers o Repositories. Siempre `await`. El error es silencioso - la request simplemente cuelga sin respuesta.

---

### Deuda Técnica (Technical Debt)
**Categoría:** Agile / Calidad de Código
**Definición:** Trabajo acumulado que hace más difícil implementar trabajo nuevo. Surge de atajos intencionales o no intencionales: código sin tests, validaciones hardcodeadas, índices faltantes, etc. Si no se trackea como tickets en el backlog, se vuelve invisible y crece indefinidamente.
**En el proyecto:** El CRM mantiene un backlog de deuda técnica visible: `TD-01` (índice faltante en conversations), `TD-02` (validación hardcodeada en ContactHandler), `TD-03` (0% coverage en Auth). Se dedica ~20% de la velocity de cada sprint a pagarla.

---

### Definition of Done (DoD)
**Categoría:** Agile / Scrum
**Definición:** Acuerdo del equipo sobre qué significa que una historia de usuario esté realmente terminada. Sin DoD, "terminado" significa cosas distintas para cada miembro. El DoD es el estándar mínimo de calidad que todo ítem del sprint debe cumplir antes de considerarse completo.
**En el proyecto:** El DoD del CRM incluye: código compila sin warnings, tests pasan, pipeline en verde, PR revisado por al menos 1 persona, criterios de aceptación verificables con Scalar/Postman, CHANGELOG actualizado si hay cambio público y sin secretos hardcodeados.

---

### Dependency Inversion Principle (DIP)
**Categoría:** SOLID / Arquitectura
**Definición:** El quinto principio SOLID. Los módulos de alto nivel no deben depender de módulos de bajo nivel - ambos deben depender de abstracciones (interfaces). Las abstracciones no deben depender de detalles; los detalles deben depender de las abstracciones. Invierte la dirección de dependencia tradicional.
**En el proyecto:** `Users.Application` usa `IUserProfileRepository` (interfaz en Domain). `Users.Infrastructure` implementa esa interfaz. Application no sabe que Infrastructure existe. Si mañana se cambia EF Core por Dapper, el Handler no se modifica - solo la implementación en Infrastructure.

---

### Devirtualización
**Categoría:** C# / Performance
**Definición:** Optimización del compilador/runtime que convierte llamadas virtuales (que requieren resolver en tiempo de ejecución qué método llamar) en llamadas directas (conocidas en tiempo de compilación). Ocurre cuando el compilador puede garantizar que no hay subclases. Las clases `sealed` permiten esta optimización.
**En el proyecto:** Marcar los Handlers, Repositories y Presenters del CRM como `sealed` no solo comunica intención de diseño - también permite al JIT de .NET hacer devirtualización, mejorando el rendimiento de cada request.

---

### DI (Dependency Injection / Inyección de Dependencias)
**Categoría:** .NET / Arquitectura
**Definición:** Patrón donde las dependencias de una clase se proveen externamente (normalmente por un contenedor IoC) en lugar de ser creadas dentro de la clase. Desacopla la construcción de objetos de su uso. En .NET, el contenedor nativo (`IServiceCollection`) registra y resuelve dependencias.
**En el proyecto:** Cada módulo del CRM expone su propio método de extensión (`AddContactsApplicationServices()`) que registra sus dependencias en el contenedor. `Program.cs` los llama todos al arrancar - agregar un módulo nuevo es agregar 3 líneas ahí.

---

### Distributed Monolith
**Categoría:** Arquitectura - Anti-patrón
Anti-patrón donde los servicios parecen microservicios pero están fuertemente acoplados: comparten base de datos, requieren despliegue coordinado, o tienen llamadas síncronas en cadena. Tiene la complejidad operativa de microservicios sin sus beneficios.

**En el proyecto:** Se evita manteniendo módulos en el mismo proceso. Si se extrae un microservicio, debe tener su propia DB y contrato versionado.

---

### DTO (Data Transfer Object)
**Categoría:** Arquitectura / C#
**Definición:** Objeto sin lógica de negocio que transporta datos entre capas o sistemas. En el back-template, los DTOs son `sealed record` en la capa Contracts y se usan en los objetos de respuesta exitosa (`ISuccess<T>`). Evitan exponer directamente las entidades de dominio en la API.
**En el proyecto:** `UserProfileDto`, `ContactDto`, `ConversationDto` son DTOs del CRM. Son `sealed record` porque son inmutables, tienen igualdad por valor (útil en tests) y el compilador genera `ToString` descriptivo automáticamente.

---

## E

### EF Core (Entity Framework Core)
**Categoría:** .NET / ORM
**Definición:** ORM (Object-Relational Mapper) oficial de Microsoft para .NET. Permite trabajar con bases de datos usando objetos C# en lugar de SQL. Incluye migraciones (versionado del schema), LINQ para queries y Global Query Filters (filtros automáticos en todas las queries, clave para multi-tenancy).
**En el proyecto:** El CRM usa EF Core 10 para escrituras y para el schema de base de datos (EntityTypeConfiguration, migraciones). Para lecturas complejas puede usar Dapper directamente. El `AppDbContext` tiene Global Query Filters con `tenantId` para garantizar aislamiento entre workspaces.

---

### editorconfig
**Categoría:** Herramientas / Formato
**Definición:** Archivo `.editorconfig` en la raíz del repositorio que define el estilo de código: indentación, saltos de línea, encoding, espaciado. Es soportado por Visual Studio, Rider y VS Code automáticamente. El CI puede verificarlo con `dotnet format --verify-no-changes` para rechazar PRs mal formateados.
**En el proyecto:** El `.editorconfig` del CRM define: `indent_size = 4` para C#, `indent_size = 2` para JSON/YAML, `end_of_line = lf` y prefijo `_` para campos privados. Elimina debates de estilo en code review - el CI los resuelve automáticamente.

---

### Escalabilidad Horizontal (Scale Out)
**Categoría:** Escalabilidad
Estrategia de añadir más instancias del mismo servidor detrás de un load balancer. No tiene límite teórico. Requiere que la aplicación sea stateless. Permite alta disponibilidad (N+1) y auto-scaling. Opuesto a escalabilidad vertical.

**En el proyecto:** La API de .NET escala horizontalmente. Todo estado persiste en PostgreSQL o Redis, nunca en memoria de la instancia.

---

### Escalabilidad Vertical (Scale Up)
**Categoría:** Escalabilidad
Estrategia de añadir más recursos (CPU, RAM) al mismo servidor. Simple de implementar sin cambios en el código, pero tiene límite físico y es single point of failure. La base de datos típicamente escala verticalmente antes de pasar a réplicas de lectura.

**En el proyecto:** La base de datos PostgreSQL escala verticalmente primero (RDS t3.medium -> r6g.large) antes de agregar réplicas de lectura.

---

### Extension method (método de extensión)
**Categoría:** C#
**Definición:** Método estático que puede llamarse como si fuera un método de instancia de otro tipo, sin modificar ese tipo. Se define en una `static class` con la palabra clave `this` como primer parámetro. Permite agregar funcionalidad a tipos existentes o crear APIs fluidas.
**En el proyecto:** Los módulos del CRM exponen sus dependencias mediante extension methods: `AddUsersApplicationServices(this IServiceCollection services)`. `Program.cs` los llama como `builder.Services.AddUsersApplicationServices()` - fluido y organizado por módulo.

---

## F

### Fail fast
**Categoría:** .NET / Configuración
**Definición:** Principio de diseño donde el sistema detecta errores lo antes posible en lugar de fallar más tarde. En configuración de .NET, `ValidateOnStart()` hace que la aplicación falle al arrancar si falta configuración requerida - mejor que descubrir el problema en la primera request en producción.
**En el proyecto:** `builder.Services.AddOptions<JwtSettings>().ValidateOnStart()` hace que el CRM no arranque si falta `Jwt:Key` o si es menor a 32 caracteres. El error aparece en el log de startup, no a las 2 AM cuando el primer usuario intenta hacer login.

---

### Feature branch
**Categoría:** Git / Flujo de trabajo
**Definición:** Branch (rama) de Git creada para desarrollar una funcionalidad o fix específico, aislado del branch principal (`main`). Se crea desde `main`, se desarrolla en ella y se mergea de vuelta mediante un Pull Request. La convención de nombre incluye el tipo, el número de issue y una descripción corta.
**En el proyecto:** Formato obligatorio en el CRM: `feature/42/add-contact-endpoint`, `bugfix/87/fix-null-ref-in-handler`, `hotfix/91/patch-jwt-expiry`. El número de issue permite que GitHub cierre el issue automáticamente al mergear con `Closes #42` en el PR.

---

### Fibonacci scale (escala Fibonacci)
**Categoría:** Agile / Story Points
**Definición:** Secuencia matemática (1, 2, 3, 5, 8, 13, 21) usada para estimar Story Points. Los saltos crecen exponencialmente, reflejando que la incertidumbre aumenta con el tamaño. Evita la falsa precisión de estimar "6" o "7" - o es un 5 o es un 8. Cualquier story estimada en 13+ debería dividirse.
**En el proyecto:** El equipo del CRM usa Fibonacci en Planning Poker. Una story de 13 puntos como "Módulo Bots completo" es señal de que debe dividirse en stories más pequeñas que quepan en un sprint individual.

---

### FluentValidation
**Categoría:** .NET / Validación
**Definición:** Librería de .NET para definir reglas de validación usando una API fluida en C#. Separa la lógica de validación de los objetos validados. Se integra con ASP.NET Core y con Pipeline Behaviors de MediatR para validar automáticamente los Requests antes de que lleguen al Handler.
**En el proyecto:** El CRM usa FluentValidation como deuda técnica pendiente (`TD-02`): la validación del ContactHandler está hardcodeada y debe extraerse a un `CreateContactValidator : AbstractValidator<CreateContactRequest>`.

---

### Full-text Search (FTS)
**Categoría:** PostgreSQL / Backend
**Definición:** Capacidad de PostgreSQL para búsqueda de texto avanzada: analiza, tokeniza y crea índices invertidos sobre columnas de texto. Permite buscar palabras y variantes (stems) en lugar de hacer `LIKE '%término%'` que no usa índices. Mucho más eficiente para búsquedas en grandes volúmenes de datos.
**En el proyecto:** El módulo `Contacts` del CRM usará FTS de PostgreSQL para la búsqueda por nombre, email o teléfono. Mencionado también en el feature request de `Conversations` (`issue #35`).

---

## G

### Generic types (Tipos Genéricos)
**Categoría:** C#
**Definición:** Tipos parametrizados con uno o más tipos de tipo (`T`) que se especifican al usarlos. Permiten escribir código reutilizable que funciona con cualquier tipo de dato. Los constraints (`where T : class`) restringen qué tipos son válidos. Los genéricos abiertos (`typeof(ResultViewModel<>)`) se usan en el contenedor de DI.
**En el proyecto:** `IRequestHandler<TRequest, TResponse>`, `ISuccess<T>`, `ResultViewModel<T>` y `QuerySingleAsync<T>` son genéricos del CRM. El `ResultViewModel<T>` usa `T` como discriminador para que el contenedor de DI cree una instancia distinta por Controller.

---

### Gherkin
**Categoría:** Agile / Testing
**Definición:** Lenguaje legible por negocio para describir el comportamiento esperado de una funcionalidad. Usa palabras clave: `Feature`, `Scenario`, `Given` (contexto inicial), `When` (action), `Then` (resultado esperado), `And` (condición adicional). Puede usarse como documentación ejecutable con herramientas como SpecFlow.
**En el proyecto:** Los Acceptance Criteria del CRM se escriben en Gherkin. Ejemplo: `Scenario: Email duplicado en el mismo workspace / Given ya existe "ana@example.com" en workspace "acme-corp" / When envío POST /api/contacts con el mismo email / Then recibo 409 Conflict`.

---

### Global Query Filter (EF Core)
**Categoría:** EF Core / Multi-tenancy
**Definición:** Filtro configurado en `DbContext` que se aplica automáticamente a todas las queries de una entidad. Se define en `OnModelCreating`. En el contexto de multi-tenancy, permite filtrar por `tenant_id` en todas las consultas sin tener que recordarlo en cada repositorio.
**En el proyecto:** El `AppDbContext` del CRM tiene un Global Query Filter en cada entidad: `.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)`. Si el filtro falla, datos de un tenant pueden filtrarse a otro - los tests de leakage son obligatorios.

---

## H

### Handler
**Categoría:** Arquitectura / Mediator Pattern
**Definición:** Clase que implementa `IRequestHandler<TRequest, TResponse>` and contiene la lógica de un caso de uso específico. Recibe un Request inmutable, ejecuta la lógica de negocio consultando o escribiendo datos a través de interfaces del Domain, y retorna un objeto Response polimórfico. Siempre `sealed`.
**En el proyecto:** Ejemplo: `GetUserProfileHandler` recibe `GetUserProfileRequest(PublicId, TenantId)`, llama `_profiles.GetByPublicIdAsync()` y retorna `GetUserProfileSuccess` o `GetUserProfileNotFoundFailure`. La lógica de negocio está aquí - nunca en el Controller.

---

### Hash (Redis)
**Categoría:** Redis - Tipos de Datos
Tipo de dato de Redis que almacena un mapa de campo -> valor dentro de una sola clave. Permite leer y actualizar campos individuales sin deserializar el objeto completo. Más eficiente que serializar un objeto JSON como String cuando solo se actualiza un campo.

**En el proyecto:** Los perfiles de agente y la configuración de tenant se almacenan como Hash: `HSET agent:tenant:1:agent:7 fullName "Ana" role "admin"`. Se actualiza el rol con un solo `HSET` sin reescribir todos los campos.

---

### HMAC (Hash-based Message Authentication Code)
**Categoría:** Seguridad / HTTP
**Definición:** Algoritmo de autenticación de mensajes que usa una función hash criptográfica combinada con una clave secreta. En el contexto de webhooks, el emisor firma el cuerpo del request con HMAC-SHA256 y el receptor verifica la firma para confirmar que el mensaje es auténtico y no fue alterado en tránsito.
**En el proyecto:** El webhook de WhatsApp del CRM verifica la firma HMAC-SHA256 usando `WhatsApp__AppSecret` antes de procesar cualquier mensaje entrante. Esto previene que actores maliciosos envíen mensajes falsos al endpoint del webhook.

---

### hotfix branch
**Categoría:** Git
**Definición:** Branch creado directamente desde un tag de producción para corregir un bug crítico sin pasar por el proceso normal de feature branch. Se mergea tanto a `main` como posiblemente a otras branches activas. Usa el prefijo `hotfix/` en la convención de nombres.
**En el proyecto:** Si en producción del CRM se descubre un bug en el cálculo de expiración de JWT, se crea `hotfix/91/patch-jwt-expiry-calculation` desde el último tag estable, se corrige y se despliega urgentemente.

---

## I

### IConflictFailure
**Categoría:** C# / Result Pattern
**Definición:** Interfaz del Result Pattern que extiende `IFailure`. Representa un fallo por conflicto de datos - el recurso ya existe o hay una violación de unicidad. El Controller que recibe este tipo en el Presenter responde con HTTP `409 Conflict`.
**En el proyecto:** `CreateContactEmailConflictFailure : GetContactResponse, IConflictFailure` se retorna cuando se intenta crear un contacto con un email que ya existe en el mismo tenant. El Controller responde `409 Conflict`.

---

### IConnectionMultiplexer
**Categoría:** Redis - .NET - StackExchange.Redis
Interfaz principal de la librería `StackExchange.Redis` que representa la conexión TCP al servidor Redis. Se registra como Singleton en DI porque la conexión es costosa de establecer. Provee acceso a `IDatabase` (operaciones clave-valor), `ISubscriber` (Pub/Sub) y scripts Lua.

**En el proyecto:** Registrado en `Program.cs` como Singleton. Los servicios que necesitan Pub/Sub (SignalR backplane, publicación de Integration Events) lo inyectan directamente. Los servicios que solo hacen cache usan `IDistributedCache`.

---

### IDistributedCache
**Categoría:** Redis - .NET - ASP.NET Core
Interfaz estándar de .NET para cache distribuido. Implementada por `StackExchange.Redis` cuando se configura `AddStackExchangeRedisCache(...)`. Operaciones: `GetAsync`, `SetAsync`, `RemoveAsync` con soporte de `DistributedCacheEntryOptions` para TTL absoluto y sliding.

**En el proyecto:** Inyectada en los servicios de cache estándar del CRM (conversaciones, contactos, config de tenant). Para operaciones avanzadas (Pub/Sub, Sorted Sets, Lua scripts) se usa `IConnectionMultiplexer` directamente.

---

### IFailure
**Categoría:** C# / Result Pattern
**Definición:** Interfaz base de la jerarquía de fallos en el Result Pattern del back-template. Define solo `string Message { get; }`. Otras interfaces la extienden: `INotFoundFailure`, `IValidationFailure`, `IConflictFailure`. En el Presenter, se chequea primero con `is IFailure` para capturar cualquier tipo de fallo.
**En el proyecto:** En el Presenter del CRM: `if (notification is IFailure failure) _viewModel.Fail(failure.Message)`. Va primero porque captura toda la jerarquía de fallos con un solo chequeo. El Controller luego distingue el tipo específico para decidir el status code HTTP.

---

### ILogger\<T\>
**Categoría:** .NET - Logging
Interfaz genérica de logging inyectada automáticamente por el contenedor de DI de ASP.NET Core. El parámetro `T` es la clase consumidora, que se convierte en la propiedad `SourceContext` en los logs. No requiere registro explícito en Program.cs.

**En el proyecto:** Cada Handler, Service y Worker del CRM inyecta `ILogger<T>` en el constructor para registrar eventos de negocio y errores con contexto de clase.

---

### IMediator
**Categoría:** C# / Mediator Pattern
**Definición:** Interfaz del patrón Mediator que actúa como bus de mensajes in-process. Recibe un Request y lo despacha al Handler registrado para ese tipo de Request. También puede publicar notificaciones a múltiples handlers. En el back-template, `IMediator` vive en `Common/Messaging/`.
**En el proyecto:** El `BaseApiController` del CRM inyecta `IMediator` como propiedad protegida. Los Controllers llaman `await Mediator.Send(new GetUserProfileRequest(id, TenantId), ct)` sin saber qué Handler lo procesará.

---

### INotFoundFailure
**Categoría:** C# / Result Pattern
**Definición:** Interfaz del Result Pattern que extiende `IFailure`. Representa un fallo porque el recurso solicitado no fue encontrado en la base de datos. El Controller que recibe este tipo responde con HTTP `404 Not Found`.
**En el proyecto:** `GetUserProfileNotFoundFailure : GetUserProfileResponse, INotFoundFailure` se retorna cuando el Handler no encuentra el usuario por su `PublicId` en el tenant. El Controller: `return result is GetUserProfileNotFoundFailure ? NotFound(_viewModel) : StatusCode(500, _viewModel)`.

---

### INotificationHandler
**Categoría:** C# / Mediator Pattern
**Definición:** Interfaz que implementan los Presenters en el back-template. Recibe una notificación (el objeto Response del Handler) y la procesa para actualizar el `ResultViewModel`. El nombre "notification" viene de MediatR - la respuesta del Handler se publica como notificación al Presenter.
**En el proyecto:** `GetUserProfilePresenter : INotificationHandler<GetUserProfileResponse>`. El Handler retorna `GetUserProfileResponse`, el Mediator lo publica al Presenter, y el Presenter actualiza el `ResultViewModel<UsersController>`.

---

### IOptions\<T\>
**Categoría:** .NET / Configuración
**Definición:** Interfaz genérica de .NET para acceder a configuración tipada. `T` es una clase POCO que mapea una sección del `appsettings.json`. Soporta validación con Data Annotations y `ValidateOnStart()` para fallo rápido. Es inyectable en constructores - más testeable que `IConfiguration` directo.
**En el proyecto:** `JwtService(IOptions<JwtSettings> options)` en el CRM recibe la configuración de JWT tipada. `JwtSettings` tiene `[Required, MinLength(32)]` en `Key` - si falta o es muy corta, la app no arranca.

---

### IReadOnlyCollection\<T\>
**Categoría:** C#
**Definición:** Interfaz de C# que representa una colección de solo lectura: tiene `Count` y es enumerable pero no permite agregar, eliminar ni modificar elementos. Es preferida sobre `List<T>` en contratos de API (interfaces de repositorio, DTOs) porque comunica que el caller no debe modificar la colección.
**En el proyecto:** `Task<IReadOnlyCollection<UserProfile>> GetPagedAsync(long tenantId, int page, int pageSize, CancellationToken ct)` - el Repository del CRM retorna `IReadOnlyCollection<T>` para que el Handler no pueda modificar accidentalmente la colección retornada.

---

### IRequest\<TResponse\>
**Categoría:** C# / Mediator Pattern
**Definición:** Interfaz marcadora del Mediator que conecta un Request con su tipo de Response esperado. Un record que implementa `IRequest<GetUserResponse>` le dice al Mediator que cuando alguien lo envíe, debe buscar un `IRequestHandler<EsteRequest, GetUserResponse>`.
**En el proyecto:** `public sealed record GetUserProfileRequest(Guid PublicId, long TenantId) : IRequest<GetUserProfileResponse>`. El compilador verifica que el Handler tenga exactamente esos tipos - error en compilación si no coinciden.

---

### IRequestHandler\<TRequest, TResponse\>
**Categoría:** C# / Mediator Pattern
**Definición:** Interfaz genérica que deben implementar los Handlers. `TRequest` debe implementar `IRequest<TResponse>` (constraint). Define el único método `Task<TResponse> Handle(TRequest request, CancellationToken ct)`. El Mediator usa reflexión en startup para registrar automáticamente todos los Handlers.
**En el proyecto:** `GetUserProfileHandler : IRequestHandler<GetUserProfileRequest, GetUserProfileResponse>`. El compilador verifica que `GetUserProfileRequest` implemente `IRequest<GetUserProfileResponse>` - si los tipos no coinciden, error de compilación.

---

### ISuccess\<T\>
**Categoría:** C# / Result Pattern
**Definición:** Interfaz genérica del Result Pattern que representa un resultado exitoso. `T` es el tipo del dato retornado. El Presenter chequea `is ISuccess<UserProfileDto> success` para extraer el dato y actualizarlo en el ViewModel. Cuidado con el anti-patrón donde `T = SelfType` (causa referencia circular en serialización JSON).
**En el proyecto:** `GetUserProfileSuccess(UserProfileDto Data) : GetUserProfileResponse, ISuccess<UserProfileDto>`. Cuando el Presenter recibe esto, llama `_viewModel.Set(success)` para que el Controller retorne `200 OK` con el DTO.

---

### init (property modifier)
**Categoría:** C#
**Definición:** Modificador de propiedad en C# que permite asignar el valor solo durante la inicialización del objeto (en el constructor o en un object initializer), pero no después. Los records posicionales generan propiedades `init` automáticamente. Hace el objeto efectivamente inmutable después de su construcción.
**En el proyecto:** Las propiedades de los records del CRM son `init`: `public Guid PublicId { get; init; }`. Esto garantiza que un `GetUserProfileRequest` no puede ser modificado una vez creado - los mensajes del Mediator son inmutables.

---

### INVEST
**Categoría:** Agile / User Stories
**Definición:** Acrónimo de los 6 criterios de una buena User Story: Independent (independiente de otras stories no terminadas), Negotiable (el cómo es negociable, solo el qué está fijo), Valuable (aporta valor real), Estimable (el equipo puede estimarla), Small (cabe en un sprint, ≤ 8 puntos), Testable (tiene criterios de aceptación verificables).
**En el proyecto:** Una story del CRM como "Mejorar el sistema para que sea más rápido" falla INVEST: no es estimable, no es testeable y no es acotada. Una story como "Como agente, buscar contactos por nombre o email para encontrarlos en < 1 segundo" pasa todos los criterios.

---

### IPresenter
**Categoría:** Arquitectura / Presenter Pattern
**Definición:** Interfaz de la capa `Common/Abstractions/` que define el contrato del Presenter en el back-template. El Presenter recibe el Response del Handler (vía `INotificationHandler`) y actualiza el `ResultViewModel` para que el Controller pueda retornar la respuesta HTTP correcta.
**En el proyecto:** El flujo del CRM: `Handler → publica Response → Presenter (INotificationHandler) → actualiza ResultViewModel → Controller lee ViewModel y retorna IActionResult`.

---

### IResultInteractor
**Categoría:** Arquitectura / Back-template
**Definición:** Interfaz de `Common/Abstractions/` que combina `IInteractor` e `IPresenter`. Define el contrato para los casos de uso que tienen resultado. Permite al Controller ejecutar el caso de uso y luego leer el ViewModel sin preocuparse por los detalles de la comunicación Mediator-Presenter.
**En el proyecto:** Parte de las abstracciones del Common del CRM. Permite que los Controllers sean delgados: solo invocan el interactor y convierten el ViewModel a una respuesta HTTP.

---

### ITenantContextAccessor
**Categoría:** Arquitectura / Multi-tenancy
**Definición:** Interfaz de `Common/MultiTenancy/` que provee acceso al contexto del tenant actual de la request. El middleware `TenantClaimsMiddleware` extrae el `tenant_id` del JWT y lo almacena aquí. Los servicios que necesitan el `tenantId` sin recibirlo en el Request pueden usarlo (aunque en el CRM se prefiere pasarlo explícitamente).
**En el proyecto:** `TenantClaimsMiddleware` extrae el `tenant_id` del claim del JWT y lo almacena en `ITenantContextAccessor`. Los Controllers leen `CurrentTenantId` directamente del claim con `User.FindFirstValue("tenant_id")`.

---

### IValidationFailure
**Categoría:** C# / Result Pattern
**Definición:** Interfaz del Result Pattern que extiende `IFailure`. Representa un fallo por datos de entrada inválidos - campos requeridos faltantes, formato incorrecto, valor fuera de rango. El Controller que lo recibe responde con HTTP `400 Bad Request` o `422 Unprocessable Entity`.
**En el proyecto:** Un Handler del CRM que valida que el email tenga formato correcto puede retornar `CreateContactValidationFailure : CreateContactResponse, IValidationFailure` si la validación falla.

---

## J

### JWT (JSON Web Token)
**Categoría:** Seguridad / Autenticación
**Definición:** Estándar abierto (RFC 7519) para transmitir información entre partes como un objeto JSON firmado digitalmente. Tiene tres partes: Header (algoritmo), Payload (claims: datos del usuario y tenant), Signature (firma). El back-template usa HS256 (HMAC-SHA256). Sin estado - el servidor no guarda sesión.
**En el proyecto:** El CRM usa JWT HS256 para autenticar requests. El claim `tenant_id` en el Payload es crucial - el Controller lo extrae con `User.FindFirstValue("tenant_id")` y lo incluye en cada Request al Handler para el aislamiento multi-tenant.

---

## K

### Keep a Changelog
**Categoría:** Documentación / SemVer
**Definición:** Estándar de formato para archivos `CHANGELOG.md` definido en `keepachangelog.com`. Especifica secciones (Agregado, Cambiado, Deprecado, Eliminado, Corregido, Seguridad), orden cronológico descendente y la versión `[Sin publicar]` para cambios aún no liberados. Es el formato más adoptado en el ecosistema open source.
**En el proyecto:** El `CHANGELOG.md` del CRM sigue Keep a Changelog. Cada sección de versión tiene la fecha de lanzamiento y los cambios agrupados por tipo. Se actualiza en el mismo PR que introduce el cambio - no en un PR separado de "actualizar docs".

---

### Kebab-case
**Categoría:** Convenciones / Naming
**Definición:** Convención de nomenclatura donde las palabras se separan con guiones (`-`) y todo en minúsculas. Usada para rutas de URLs REST, nombres de branches de Git y nombres de archivos en algunos ecosistemas.
**En el proyecto:** Los endpoints del CRM usan kebab-case: `/api/contact-groups`, `/api/bot-flows`. Los branches también: `feature/42/add-contact-search`. Nunca guiones bajos ni espacios.

---

## L

### Latencia p95 / p99
**Categoría:** Observabilidad - Escalabilidad
Percentiles de latencia. p95 = el 95% de las peticiones respondieron en ≤ X ms. p99 = el 99% respondieron en ≤ Y ms. El 1% restante (los más lentos) suele ser el que arruina la experiencia del usuario. Es más relevante que el promedio.

**En el proyecto:** El objetivo de SLA inicial es p95 < 200ms y p99 < 1000ms para endpoints de la bandeja de conversaciones.

---

### Leaky Bucket
**Categoría:** Rate Limiting - Algoritmos
Algoritmo de rate limiting que procesa peticiones a tasa constante, independientemente de ráfagas de entrada. Si la cola (cubo) se llena, descarta nuevas peticiones. Útil para shaping de tráfico donde la uniformidad de salida importa más que permitir ráfagas.

**En el proyecto:** Usado internamente en el BackgroundService de campañas para enviar a WhatsApp API a tasa constante (10 mensajes/seg).

---

### LGTM (Looks Good To Me)
**Categoría:** Code Review
**Definición:** Frase usada en code review para aprobar un PR sin análisis real. Es un antipatrón peligroso - el reviewer que aprueba sin entender el código es corresponsable de los bugs que no detectó.
**En el proyecto:** Antipatrón explícitamente prohibido en el proceso de Code Review del CRM. Todo comentario de aprobación debe referenciar qué se revisó. Si el código es demasiado grande para revisar: pedir que el PR se divida.

---

### Load Balancer
**Categoría:** Escalabilidad - Infraestructura
Componente que distribuye peticiones entrantes entre múltiples instancias de la aplicación. Algoritmos: Round Robin (turno rotativo), Least Connections (a la menos ocupada), IP Hash (mismo cliente -> misma instancia). Realiza health checks y retira instancias no saludables.

**En el proyecto:** AWS ALB (Application Load Balancer) en modo Round Robin con health checks activos cada 30 segundos.

---

### Log Level (Nivel de Log)
**Categoría:** Logging - Observabilidad
Clasificación jerárquica de la importancia de un evento de log: Trace < Debug < Information < Warning < Error < Critical. En producción el nivel mínimo es Information; Trace y Debug solo en desarrollo. Los sistemas de alertas típicamente se disparan en Error o Critical.

**En el proyecto:** `appsettings.json` configura `MinimumLevel.Default = "Information"` y overrides para `Microsoft.*` y `System.*` en `"Warning"` para reducir ruido de la infraestructura.

---

### LogContext (Serilog)
**Categoría:** Logging - Serilog
Mecanismo de Serilog para enriquecer todos los logs de un scope con propiedades adicionales sin pasarlas manualmente a cada llamada. Se usa con `LogContext.PushProperty("Nombre", valor)` dentro de un `using` block. Las propiedades se eliminan automáticamente al salir del using.

**En el proyecto:** Un middleware de tenant agrega `TenantId` y `UserId` al LogContext al inicio de cada petición HTTP, de modo que todos los logs de esa petición incluyen esas propiedades automáticamente.

---

## M

### MAJOR (SemVer)
**Categoría:** SemVer
**Definición:** Primer número en la versión `MAJOR.MINOR.PATCH`. Se incrementa cuando se introduce un breaking change - una modificación que rompe la compatibilidad con clientes existentes. Al incrementar MAJOR, MINOR y PATCH se resetean a 0.
**En el proyecto:** El CRM pasará de `v1.x.x` a `v2.0.0` cuando renombre `workspace` a `organization` en la API pública, ya que cualquier cliente que use el campo `workspace` dejará de funcionar.

---

### Mediator pattern
**Categoría:** Arquitectura / Design Patterns
**Definición:** Patrón de comportamiento que centraliza la comunicación entre objetos a través de un objeto mediador. Los objetos no se referencian directamente entre sí - todos se comunican a través del mediador. En el back-template, el Mediator es el bus de mensajes entre Controllers y Handlers.
**En el proyecto:** El Controller del CRM llama `Mediator.Send(request, ct)` sin saber qué Handler existe. El Handler no sabe de qué Controller vino la solicitud. El Mediator los desacopla completamente. Esto permite testear el Handler en aislamiento sin necesidad de un Controller.

---

### Merge (Git)
**Categoría:** Git
**Definición:** Operación de Git que combina los cambios de un branch en otro. En el CRM se usa Squash & Merge al cerrar PRs - todos los commits del feature branch se combinan en un único commit limpio en `main`. Esto mantiene el historial de `main` legible.
**En el proyecto:** El proceso de PR del CRM termina con Squash & Merge. El mensaje del commit squashed debe seguir Conventional Commits: `feat(contacts): add search by phone number (#42)`. La branch se elimina automáticamente después del merge.

---

### Message Template (Serilog)
**Categoría:** Logging - Structured Logging
Cadena con placeholders nombrados `{NombrePropiedad}` que Serilog usa para capturar tanto el mensaje de texto como los valores estructurados por separado. A diferencia de string interpolation, los valores se guardan como propiedades consultables. Ejemplo: `"Usuario {UserId} creó conversación {ConvId}"`.

**En el proyecto:** Todos los logs del CRM usan message templates. La regla del proyecto prohíbe string interpolation `$"..."` en llamadas a ILogger - el analizador CA2254 lo verifica en tiempo de compilación.

---

### Microservicio
**Categoría:** Arquitectura
Unidad de despliegue independiente con un único dominio de responsabilidad, su propia base de datos, y comunicación vía red. Se despliega, escala y versiona independientemente. Apropiado cuando el equipo es grande (50+ personas) o cuando dominios necesitan escalar de forma muy diferente.

**En el proyecto:** El sistema de mensajería usa Monolito Modular, no microservicios. Si el módulo de mensajes necesitara escalar 10× más que el resto, se extraería como microservicio en ese momento.

---

### MINOR (SemVer)
**Categoría:** SemVer
**Definición:** Segundo número en la versión `MAJOR.MINOR.PATCH`. Se incrementa cuando se agrega una funcionalidad nueva compatible hacia atrás (no rompe clientes existentes). Al incrementar MINOR, PATCH se resetea a 0.
**En el proyecto:** Agregar el campo `"phone": null` a la respuesta de `GET /api/contacts/:id` es una feature nueva compatible - los clientes que no conocen `phone` simplemente lo ignoran. Dispara `v1.2.0 → v1.3.0`.

---

### Minimal API
**Categoría:** .NET / ASP.NET Core
**Definición:** Estilo de API en ASP.NET Core que define endpoints directamente en `Program.cs` sin Controllers ni clases adicionales. Usa lambdas y métodos de extensión. Más simple para APIs pequeñas. En el back-template se usa la arquitectura de Controllers con el Presenter Pattern, no Minimal API.
**En el proyecto:** El CRM usa Controllers (no Minimal API) porque el Presenter Pattern requiere DI de `ResultViewModel<T>` que se integra naturalmente con Controllers. Las katas del hands-on sí usan Minimal API para mayor simplicidad.

---

### Modular Monolith (Monolito Modular)
**Categoría:** Arquitectura
**Definición:** Patrón arquitectónico donde la aplicación se divide en módulos con límites claros (Domain, Application, Infrastructure, Presentation) pero se despliega como un único proceso. Ofrece la organización de microservicios con la simplicidad operativa del monolito. Los módulos se comunican a través de interfaces bien definidas, no llamadas de red.
**En el proyecto:** El sistema de mensajería es un Monolito Modular. Los módulos (Auth, Users, Contacts, Conversations, Messages, Bots, Channels) viven en el mismo proceso pero tienen límites explícitos. Se puede extraer un módulo como microservicio en el futuro si el equipo y el volumen crecen (ADR-001).

---

### Multi-tenancy
**Categoría:** Arquitectura / SaaS
**Definición:** Arquitectura donde una única instancia del software sirve a múltiples clientes (tenants), con sus datos completamente aislados entre sí. Existen tres modelos: Silo (una DB por tenant), Pool (DB compartida con tenant_id), Bridge (schema por tenant en la misma DB).
**En el proyecto:** El CRM usa el modelo Pool (ADR-002): todos los workspaces (tenants) comparten la misma PostgreSQL, y cada tabla tiene `tenant_id bigint NOT NULL`. El `tenantId` viaja explícitamente en cada Request - si alguien lo olvida, el código no compila.

---

## N

### Ngrok
**Categoría:** Herramientas / Desarrollo
**Definición:** Herramienta que crea un túnel seguro desde una URL pública (ngrok.io) hasta un servidor local. Permite que servicios externos (como los webhooks de WhatsApp de Meta) envíen requests a una API corriendo en `localhost` durante el desarrollo.
**En el proyecto:** Para probar el webhook de WhatsApp del CRM en desarrollo local, el developer usa ngrok para exponer `http://localhost:5000` con una URL pública que Meta pueda alcanzar.

---

### Nitpicking (Code Review)
**Categoría:** Code Review
**Definición:** Comentarios de code review sobre detalles menores subjetivos de estilo que no afectan la funcionalidad ni la legibilidad significativa: tabs vs espacios, orden de propiedades, naming casi correcto. Es un antipatrón porque consume tiempo del reviewer y del author sin aportar valor real.
**En el proyecto:** El `.editorconfig` del CRM automatiza el formato. Si alguien comenta en un PR "deberías usar 2 espacios aquí", la respuesta correcta es "el CI ya verifica formato con `dotnet format` - no es necesario comentarlo en review".

---

### Nullable reference type
**Categoría:** C#
**Definición:** Característica de C# 8+ que distingue entre tipos que pueden ser `null` (`string?`, `UserProfile?`) y los que no (`string`, `long`). Con `<Nullable>enable</Nullable>` en el proyecto, el compilador emite warnings cuando se usa una referencia que podría ser null sin verificación previa. Elimina NullReferenceExceptions en tiempo de compilación.
**En el proyecto:** El back-template del CRM tiene nullable reference types habilitados. `GetByPublicIdAsync` retorna `UserProfile?` - el Handler debe verificar `if (profile is null)` antes de usarlo, o el compilador emite un warning que falla en CI (`--warnaserror`).

---

## O

### Open-closed Principle (OCP)
**Categoría:** SOLID
**Definición:** Segundo principio SOLID. Las clases deben estar abiertas para extensión pero cerradas para modificación. Para agregar comportamiento nuevo, se extiende la clase (por herencia o composición) en lugar de modificar su código existente. Evita romper funcionalidad existente al agregar features.
**En el proyecto:** Los Presenters del CRM son cerrados a modificación - si se agrega un nuevo tipo de fallo (`IThrottledFailure`), se agrega una nueva clase que implementa `IFailure`, sin modificar los Presenters existentes.

---

## P

### PATCH (SemVer)
**Categoría:** SemVer
**Definición:** Tercer número en la versión `MAJOR.MINOR.PATCH`. Se incrementa cuando se corrige un bug sin agregar funcionalidad ni romper compatibilidad. Un fix debe ser compatible hacia atrás - los clientes no necesitan cambiar su código.
**En el proyecto:** Corregir que `GET /contacts` no aplicaba el filtro de tenant en queries de búsqueda es un PATCH: `v0.2.0 → v0.2.1`. El comportamiento ya estaba prometido; solo se corrigió la implementación incorrecta.

---

### Pattern matching
**Categoría:** C#
**Definición:** Familia de expresiones en C# que combinan prueba de tipo, asignación y deconstrucción en una sola operación. Incluye: `is` con asignación (`if (x is SomeType s)`), `switch` expression, patrones de tipo, patrones de propiedad y patrones relacionales. Hace el código más expresivo y elimina casteos explícitamente.
**En el proyecto:** Los Presenters del CRM usan `is` para verificar el tipo del Response: `if (notification is IFailure failure)`. Los Controllers usan ternario con `is`: `return result is GetUserProfileNotFoundFailure ? NotFound(_viewModel) : StatusCode(500, _viewModel)`.

---

### Pipeline verde (Green pipeline)
**Categoría:** CI/CD
**Definición:** Estado del pipeline de CI donde todos los pasos (build, tests, lint/format) pasaron exitosamente. Es un requisito obligatorio para mergear un PR. Un pipeline rojo (algún paso falló) bloquea el merge independientemente de la urgencia o la opinión de los reviewers.
**En el proyecto:** Regla absoluta del CRM: si el pipeline está en rojo, el PR no se mergea. Ni aunque "sea solo un test flaky" - los tests flaky se arreglan primero o se eliminan.

---

### Planning Poker
**Categoría:** Agile / Story Points
**Definición:** Técnica de estimación colaborativa donde cada miembro del equipo elige en secreto un Story Point para una historia y todos revelan simultáneamente. La revelación simultánea evita el sesgo de anclaje (que la estimación del primero influya en los demás). Si hay divergencia, los extremos explican su razonamiento y se vota de nuevo.
**En el proyecto:** El equipo del CRM hace Planning Poker en Sprint Planning. Cuando Dev B estima 3 y Dev D estima 8 para la misma story, la discusión revela información nueva (Dev D incluye el trabajo de frontend que Dev B no había considerado) y convergen en 8.

---

### Posicional record
**Categoría:** C#
**Definición:** Forma compacta de declarar un record donde los parámetros del constructor se declaran directamente después del nombre del tipo. El compilador genera automáticamente: propiedades `init`, constructor, `Equals`, `GetHashCode`, `==`, `ToString` y `Deconstruct`. Es la forma más usada de record en el back-template.
**En el proyecto:** `public sealed record GetUserProfileRequest(Guid PublicId, long TenantId) : IRequest<GetUserProfileResponse>` - en una línea se declara el Request con sus dos propiedades inmutables y el tipo de respuesta que espera.

---

### PR (Pull Request)
**Categoría:** Git / Flujo de trabajo
**Definición:** Mecanismo de GitHub para proponer cambios de un feature branch a la rama principal. Incluye: descripción de los cambios, cómo testearlos, link al issue relacionado y un checklist. Requiere al menos una aprobación y el pipeline en verde antes de mergearse. Los comentarios constructivos con sugerencias concretas son el corazón del proceso.
**En el proyecto:** El PR template del CRM incluye: ¿Qué hace este PR?, Tipo de cambio, Issue relacionado (`Closes #`), cómo testear con pasos concretos (curl/Postman/Scalar) y el checklist del DoD.

---

### Pre-release (SemVer)
**Categoría:** SemVer
**Definición:** Versión de software que precede a una versión estable, indicada con un sufijo después de un guión: `v1.0.0-alpha.1`, `v1.0.0-beta.2`, `v1.0.0-rc.1`. No se instalan automáticamente por gestores de paquetes - requieren indicación explícita. Secuencia: alpha -> beta -> rc -> release estable.
**En el proyecto:** El CRM puede publicar `v0.X.0-rc.1` para una prueba interna de 48 horas antes de cada release a staging de un cliente nuevo de la empresa.

---

### Presenter pattern
**Categoría:** Arquitectura / Design Patterns
**Definición:** Patrón donde un objeto Presenter recibe el resultado del caso de uso y lo transforma en una forma que el Controller puede usar para construir la respuesta HTTP. En el back-template, el Presenter implementa `INotificationHandler<TResponse>` y actualiza el `ResultViewModel`.
**En el proyecto:** El flujo completo del CRM: Controller envía Request al Mediator -> Mediator despacha al Handler -> Handler retorna Response -> Mediator publica Response al Presenter -> Presenter actualiza ResultViewModel -> Controller lee ViewModel y retorna `Ok()`, `NotFound()`, etc.

---

### Product Owner (PO)
**Categoría:** Agile / Scrum
**Definición:** Rol en Scrum responsable de maximizar el valor del producto y gestionar el Product Backlog. Define el Sprint Goal, prioriza las historias por valor de negocio y acepta o rechaza el trabajo completado en the Sprint Review. Es la voz del cliente/negocio ante el equipo de desarrollo.
**En el proyecto:** En el CRM, el Product Owner es quien define las prioridades de la empresa. En Sprint Planning, el PO presenta el Sprint Goal y el equipo estima y compromete qué puede completar.

---

### Pub/Sub (Redis)
**Categoría:** Redis - Mensajería en Tiempo Real
Mecanismo de Redis para mensajería asíncrona donde un publicador envía mensajes a un canal y todos los suscriptores activos los reciben simultáneamente. No hay persistencia - si un suscriptor no está activo cuando se publica, pierde el mensaje.

**En el proyecto:** Usado como backplane de SignalR. Cuando llega un mensaje nuevo de WhatsApp, la instancia de la API que lo recibe publica en `channel:tenant:{id}:conversations`. Todas las demás instancias están suscritas y reenvían el evento a sus clientes WebSocket conectados.

---

## Q

### QuerySingleAsync\<T\> (Dapper)
**Categoría:** C# / Dapper
**Definición:** Método genérico de Dapper que ejecuta una query SQL y mapea el resultado a una instancia del tipo `T`. Si la query devuelve 0 o más de 1 resultado, usa `QuerySingleOrDefaultAsync<T>` para devolver `null` en lugar de lanzar excepción. `T` se especifica explícitamente porque Dapper no puede inferirlo del SQL.
**En el proyecto:** `var user = await db.QuerySingleAsync<UserProfile>(sql, new { publicId, tenantId })` - Dapper mapea las columnas del resultado a las propiedades de `UserProfile` automáticamente por nombre.

---

### Queue-based Load Leveling
**Categoría:** Escalabilidad - Patrones
Patrón que interpone una cola de mensajes entre el productor y el consumidor para absorber picos de carga. El productor encola trabajo a la tasa que llega; el consumidor procesa a la tasa que puede manejar. La cola actúa como buffer que suaviza los picos.

**En el proyecto:** Las campañas masivas encolan mensajes en una cola (SQS o canal interno). El worker los consume a 10/seg respetando el rate limit de WhatsApp API.

---

## R

### Rate Limiting
**Categoría:** Seguridad - Escalabilidad
Mecanismo que limita cuántas peticiones puede hacer un cliente en un período de tiempo. Protege la API de abuso, bugs en clientes y ataques de fuerza bruta. Responde con HTTP 429 Too Many Requests cuando se supera el límite. El header `Retry-After` indica cuándo reintentar.

**En el proyecto:** ASP.NET Core `AddRateLimiter` con Sliding Window de 200 req/min por usuario autenticado. Límites por tenant según plan (Starter: 500/min, Professional: 2000/min).

---

### RC (Release Candidate)
**Categoría:** SemVer / DevOps
**Definición:** Versión candidata a release estable. La API está fija - solo se aceptan fixes críticos antes de convertirse en la versión estable. Se expresa como `v1.0.0-rc.1`. Si no aparecen bugs críticos en el período de prueba, el RC se convierte en la versión estable.
**En el proyecto:** El CRM publicará una `rc` antes de cada cliente nuevo de la empresa para hacer una prueba interna de 48 horas con el equipo técnico antes del lanzamiento oficial.

---

### RDB (Redis Database Snapshot)
**Categoría:** Redis - Persistencia
Mecanismo de persistencia de Redis que guarda una foto completa del estado en disco periódicamente (configurable). Rápido de restaurar al arrancar pero puede perder datos entre snapshots (RPO = tiempo entre snapshots). Recomendado para cache donde la pérdida de datos es aceptable.

**En el proyecto:** El Redis de cache (conversaciones, perfiles) usa RDB o no tiene persistencia. Si Redis reinicia, el cache se reconstituye desde PostgreSQL en los primeros accesos.

---

### Record (C#)
**Categoría:** C#
**Definición:** Tipo de referencia en C# (disponible desde C# 9) que el compilador configura automáticamente para tener igualdad por valor, `ToString` descriptivo, `Deconstruct` y `with` expressions. Ideal para objetos inmutables que representan datos. Contrasta con `class` que tiene igualdad por referencia (identidad).
**En el proyecto:** Los Requests, Responses, DTOs y Value Objects del CRM son records. Las entidades de Domain son clases (Dapper requiere propiedades con setter). La regla: si el objeto representa datos inmutables -> record; si tiene estado mutable o ciclo de vida -> class.

---

### Redis Sentinel
**Categoría:** Redis - Alta Disponibilidad
Arquitectura de Redis para alta disponibilidad sin sharding. Tres o más procesos Sentinel monitorean el Primary. Si el Primary falla, los Sentinels votan y promueven automáticamente una Replica como nuevo Primary en menos de 30 segundos. El endpoint DNS no cambia durante el failover.

**En el proyecto:** Arquitectura de Redis en producción para el CRM. En desarrollo se usa Redis Standalone (un solo proceso). El connection string apunta al Sentinel, no al Primary directamente.

---

### Reference story (historia de referencia)
**Categoría:** Agile / Story Points
**Definición:** Historia previamente implementada que el equipo usa como ancla para calibrar estimaciones. Cuando el equipo dice "esta story es un 5", todos piensan en la historia de referencia de 5 puntos. Se establecen al inicio del proyecto y se revisan cada 3-4 sprints.
**En el proyecto:** Las reference stories del CRM: 1pt = cambiar texto de un botón; 3pts = GET /contacts/:id (patrón Users ya existe); 5pts = POST /contacts con validación; 8pts = webhook de WhatsApp con integración Meta; 13pts = módulo completo de Bots.

---

### Región (Cloud)
**Categoría:** Cloud
Conjunto de datacenters en la misma zona geográfica. AWS tiene 30+ regiones. Cada región tiene 2-6 Availability Zones. Elegir la región más cercana a los usuarios reduce latencia. Los datos no salen de la región salvo configuración explícita (relevante para GDPR).

**En el proyecto:** Región primaria: sa-east-1 (São Paulo) o mexicocentral (Azure). Backups cross-region a S3 para disaster recovery.

---

### Repository pattern
**Categoría:** Arquitectura / Design Patterns
**Definición:** Patrón que abstrae el acceso a datos detrás de una interfaz. La interfaz vive en el Domain, la implementación en Infrastructure. Los Handlers solo conocen la interfaz - no saben si los datos vienen de PostgreSQL, Dapper, EF Core o un mock. Facilita el testing y permite cambiar la tecnología de persistencia sin modificar la lógica de negocio.
**En el proyecto:** `IUserProfileRepository` en `Users.Domain/Repositories/` define el contrato. `UserProfileRepository : IUserProfileRepository` en `Users.Infrastructure/Repositories/` implementa con Dapper/EF Core. Si mañana se cambia de Dapper a EF Core, el Handler no cambia.

---

### Result Pattern
**Categoría:** Arquitectura / C#
**Definición:** Patrón donde los métodos retornan un objeto que representa éxito o fallo explícitamente en lugar de lanzar excepciones para flujos de negocio esperados. En el back-template, los Handlers retornan un `abstract record Response` con variantes `sealed record Success : ISuccess<T>` y `sealed record Failure : IFailure`. El Presenter distingue el tipo con pattern matching.
**En el proyecto:** Los Handlers del CRM nunca lanzan excepciones para fallos de negocio (usuario no encontrado, email duplicado). Retornan objetos específicos: `GetUserProfileNotFoundFailure`, `CreateContactEmailConflictFailure`. Las excepciones solo ocurren por bugs o condiciones inesperadas del sistema.

---

### Retrospectiva (Sprint Retrospective / Retro)
**Categoría:** Agile / Scrum
**Definición:** Ceremonia al final del sprint donde el equipo reflexiona sobre el proceso: qué salió bien, qué mejorar y qué acciones concretas tomar en el siguiente sprint. Dura ~45 minutos. Es la oportunidad de mejorar el proceso continuamente - no es una reunión de quejas.
**En el proyecto:** La Retro del equipo del CRM al final de cada sprint produce 1-3 acciones concretas para el siguiente sprint: "agregar un SLA de review de 4h en CONTRIBUTING.md", "automatizar el formato con CI para no debatirlo en review".

---

### Rollback
**Categoría:** DevOps / Git
**Definición:** Proceso de revertir una versión en producción a una versión anterior estable después de detectar un bug crítico en el release más reciente. En el CRM, se implementa creando un nuevo tag sobre el commit de la versión estable anterior, lo que dispara el pipeline de producción con esa versión.
**En el proyecto:** El `docs/deployment.md` del CRM documenta el proceso de rollback: identificar el último tag estable con `git tag --list | sort -V | tail -5`, crear un nuevo tag y empujarlo al remoto para que el pipeline lo desplegara automáticamente.

---

### RPO (Recovery Point Objective)
**Categoría:** Alta Disponibilidad
Cuánto tiempo de datos se pueden perder en caso de fallo catastrófico. RPO = 5 min significa que en el peor caso se pierden 5 minutos de transacciones. Determina la frecuencia de backups y la necesidad de replicación síncrona vs asíncrona.

**En el proyecto:** RPO objetivo: 5 minutos. Se logra con RDS automated backups cada 5 minutos y réplica sincrónica Multi-AZ.

---

### RTO (Recovery Time Objective)
**Categoría:** Alta Disponibilidad
Tiempo máximo tolerable de downtime para recuperarse de un fallo. RTO = 1 hora significa que el sistema debe estar operativo en máximo 60 minutos. Determina si se necesita failover automático o manual.

**En el proyecto:** RTO objetivo: 15 minutos. El failover de RDS Multi-AZ es automático (~60s). El failover manual de instancias requiere intervención del equipo.

---

### Runbook
**Categoría:** DevOps / Documentación
**Definición:** Documento operacional que describe paso a paso cómo ejecutar una operación repetitiva: despliegue, rollback, restauración de backup, rotación de secretos. Escrito para poder ser ejecutado por cualquier miembro del equipo, incluso bajo presión, sin necesitar conocimiento implícito.
**En el proyecto:** `docs/deployment.md` del CRM es el Runbook principal. Documenta el despliegue a staging (automático vía CI), el despliegue a producción (semi-manual, requiere tag) y el rollback de emergencia con comandos exactos.

---

## S

### Scalar (herramienta)
**Categoría:** Herramientas / API
**Definición:** Cliente de API moderno y minimalista que se integra con OpenAPI/Swagger. Similar a Postman pero más ligero - se levanta automáticamente con la API de .NET. Permite probar endpoints directamente desde el browser sin configurar colecciones.
**En el proyecto:** En el CRM, el Scalar UI está disponible en `http://localhost:5000/swagger` cuando la app corre en Development. Los criterios de aceptación de las stories se verifican con Scalar o Postman.

---

### Scope creep
**Categoría:** Agile / SDLC
**Definición:** Fenómeno donde se agregan historias o requerimientos al sprint activo sin remover otras. Rompe el compromiso del Sprint Planning y es una señal de alerta en el Burndown Chart: la curva sube en lugar de bajar. Debe ser resistido activamente - las stories nuevas van al backlog del siguiente sprint.
**En el proyecto:** Si durante el sprint del CRM el PO pide agregar "también el módulo de campañas" sin remover stories existentes, eso es scope creep. La velocity prometida ya no es alcanzable y el sprint fracasará.

---

### Scoped lifetime
**Categoría:** .NET / Dependency Injection
**Definición:** Lifetime del contenedor de DI donde se crea una instancia por request HTTP (o por scope explícito). La instancia es compartida dentro del mismo request pero una request diferente tiene su propia instancia. Es el lifetime correcto para DbContext, Repositories y ResultViewModel.
**En el proyecto:** `services.AddScoped(typeof(ResultViewModel<>))` - cada request del CRM tiene su propio `ResultViewModel`. Si fuera Singleton, el ViewModel de un request contaminaría la respuesta de otro request concurrent.

---

### sealed class
**Categoría:** C#
**Definición:** Clase que no puede ser heredada. Comunica que la clase no fue diseñada como base para otras clases. Permite al compilador realizar optimizaciones (devirtualización). Se usa en Handlers, Repositories, Controllers, Presenters y DTOs.
**En el proyecto:** Todos los Handlers, Repositories y Presenters del CRM son `sealed class`. Regla: si no diseñaste la clase para ser heredada, márcala como `sealed`. Si alguien intenta heredar accidentalmente, el compilador falla con un error claro.

---

### sealed record
**Categoría:** C#
**Definición:** Record que no puede ser heredado. Combina la inmutabilidad y igualdad por valor de los records con la finalidad de `sealed`. Los Records posicionales del back-template son casi siempre `sealed` - representan mensajes o datos concretos, no jerarquías.
**En el proyecto:** `public sealed record GetUserProfileRequest(Guid PublicId, long TenantId)` - es un mensaje concreto del CRM, no una clase base. `abstract record GetUserProfileResponse` es la excepción - es explitamente la base de la jerarquía de respuestas.

---

### SECURITY.md
**Categoría:** Seguridad / Documentación
**Definición:** Archivo en la raíz del repositorio que documenta la política de seguridad del proyecto: qué versiones tienen soporte activo y cómo reportar vulnerabilidades de forma responsable (sin abrir issues públicos que darían ventaja a actores maliciosos).
**En el proyecto:** El CRM tiene `SECURITY.md` con el proceso: enviar email a `security@example.com` con pasos para reproducir, impacto y versión afectada. La respuesta se da en < 48 horas hábiles y el parche en < 7 días si se confirma.

---

### SemVer (Semantic Versioning)
**Categoría:** DevOps / Control de Versiones
**Definición:** Estándar de versionado `MAJOR.MINOR.PATCH` donde cada segmento tiene significado semántico: PATCH = bug fix, MINOR = feature nueva compatible, MAJOR = breaking change. Permite a los consumidores de una API entender el impacto de actualizar sin leer el código.
**En el proyecto:** El CRM usa SemVer desde `v0.1.0`. Los Conventional Commits automatizan el bump: `fix:` -> PATCH, `feat:` -> MINOR, `feat!:` o `BREAKING CHANGE:` -> MAJOR. La herramienta `semantic-release` puede automatizar todo el proceso con un GitHub Action.

---

### Seq
**Categoría:** Logging - Herramientas - Desarrollo
Servidor local de logs estructurados con interfaz web para visualizar, filtrar y consultar eventos de Serilog. Se ejecuta en Docker durante el desarrollo. Permite queries tipo SQL sobre las propiedades de los logs (ej: `TenantId = 1 and @Level = 'Error'`).

**En el proyecto:** El stack de desarrollo incluye Seq en `docker-compose.yml` en el puerto 8080. Serilog lo configura como sink adicional junto a la consola.

---

### Service Discovery
**Categoría:** Microservicios - Infraestructura
Mecanismo para que los servicios encuentren las direcciones IP/puerto de otros servicios en tiempo de ejecución. La IP puede cambiar en auto-scaling o despliegues. Soluciones: DNS (Kubernetes lo hace automáticamente), Consul, AWS Cloud Map.

**En el proyecto:** En el Monolito Modular no es necesario. Si se extraen microservicios, AWS ECS Service Connect proporciona service discovery automático.

---

### ServiceCollectionEx
**Categoría:** .NET / Arquitectura
**Definición:** Clase estática por módulo que expone extension methods para registrar las dependencias de ese módulo en el contenedor de DI. Sigue el principio de que cada módulo es responsable de registrar sus propios servicios. `Program.cs` solo llama los métodos de extensión de cada módulo.
**En el proyecto:** Cada módulo del CRM tiene su `ServiceCollectionEx` en `Application/`, `Infrastructure/` y `Presentation/`: `AddContactsApplicationServices()`, `AddContactsInfrastructureServices()`, `AddContactsWebApiServices()`. Agregar un módulo nuevo = 3 líneas en `Program.cs`.

---

### Sesgo de anclaje
**Categoría:** Agile / Psicología
**Definición:** Sesgo cognitivo donde la primera pieza de información recibida (el "ancla") tiene un peso desproporcionado en las decisiones posteriores. En Planning Poker, si alguien dice "yo estimo 5" antes de la votación, los demás tenderán inconscientemente a estimar cerca de 5 aunque su evaluación independiente sería diferente.
**En el proyecto:** Por esto Planning Poker requiere que todos revelen sus estimaciones simultáneamente. Si Dev A dice "5" primero, los otros tenderán hacia 5 sin importar su evaluación real. La revelación simultánea fuerza estimaciones independientes.

---

### Sink (Serilog)
**Categoría:** Logging - Serilog
Destino de salida de los logs en Serilog. Cada sink recibe todos los eventos de log que superan su nivel mínimo. En desarrollo: consola + Seq. En producción: CloudWatch (AWS), Application Insights (Azure) o Grafana Loki.

**En el proyecto:** `Program.cs` configura `.WriteTo.Console(...)` y `.WriteTo.Seq(url)`. En producción se reemplaza Seq por el sink del proveedor cloud.

---

### Singleton lifetime
**Categoría:** .NET / Dependency Injection
**Definición:** Lifetime del contenedor de DI donde se crea una única instancia de la clase para toda la vida del proceso. Es correcto para objetos sin estado compartido o con estado compartido intencional (caché, configuración). NUNCA inyectar Scoped dentro de Singleton - el Scoped viviría más de lo debido (captive dependency).
**En el proyecto:** `IOptions<JwtSettings>` es Singleton en el CRM - la configuración de JWT no cambia durante la vida del proceso y no tiene estado de request. `ResultViewModel<T>` es Scoped - una instancia por request, nunca Singleton.

---

### SLA (Service Level Agreement)
**Categoría:** Alta Disponibilidad - Negocio
Porcentaje de tiempo que el sistema promete estar disponible. 99.9% = 8.76 horas de downtime al año. 99.99% = 52.6 minutos. El SLA es un compromiso contractual con el cliente; incumplirlo puede significar créditos o penalizaciones.

**En el proyecto:** SLA inicial del CRM: 99.9%. Se alcanza con Multi-AZ sin multi-región. El SLA de AWS RDS Multi-AZ es 99.95%.

---

### Sliding Window (Rate Limiting)
**Categoría:** Rate Limiting - Algoritmos
Algoritmo de rate limiting más preciso que Fixed Window. Calcula el conteo ponderando la ventana anterior y la actual según el tiempo transcurrido. Evita el burst problem del Fixed Window al cambio de ventana. Requiere solo 2 contadores en Redis (eficiente en memoria).

**En el proyecto:** Algoritmo elegido para el rate limiting de la API. Implementado con `AddSlidingWindowLimiter` de ASP.NET Core.

---

### Sorted Set (Redis)
**Categoría:** Redis - Tipos de Datos
Tipo de dato de Redis que almacena elementos únicos con un score numérico asociado. Permite recuperar elementos ordenados por score en tiempo O(log N). Usado para leaderboards, colas con prioridad y rate limiting con sliding window.

**En el proyecto:** Rate limiting por usuario: cada petición agrega un elemento con timestamp como score. Para contar peticiones en el último minuto se usa `ZCOUNT` con rango de tiempo. También para la cola de mensajes urgentes de campañas con prioridad por fecha de programación.

---

### SQL injection
**Categoría:** Seguridad / Bases de Datos
**Definición:** Vulnerabilidad de seguridad donde un atacante inserta SQL malicioso en los parámetros de entrada que, si no son sanitizados, se ejecuta como parte de la query. Se previene usando queries parametrizadas - nunca concatenar strings de usuario directamente en SQL.
**En el proyecto:** Dapper parametrizado en el CRM previene SQL injection automáticamente: `db.QuerySingleAsync<UserProfile>(sql, new { publicId, tenantId })`. El `new { publicId, tenantId }` crea parámetros seguros - Dapper nunca interpola los valores directamente en el SQL.

---

### Sprint
**Categoría:** Agile / Scrum
**Definición:** Período de tiempo fijo (normalmente 1-4 semanas) durante el cual el equipo trabaja para completar un conjunto de historias comprometidas en el Sprint Planning. Al final del sprint, el software debe ser potencialmente entregable. El CRM usa sprints de 2 semanas.
**En el proyecto:** Cada sprint del CRM produce un incremento del producto (nuevas funcionalidades funcionando). El Sprint 1 produjo Auth + Users CRUD (`v0.1.0`). El historial de SemVer del CRM mapea directamente con los sprints.

---

### Sprint Backlog
**Categoría:** Agile / Scrum
**Definición:** Lista de historias comprometidas para el sprint actual, más las tareas técnicas derivadas de cada historia. Se define en Sprint Planning y, idealmente, no se modifica durante el sprint (excepto para remover items si se agrega scope, respetando el compromiso total).
**En el proyecto:** El Sprint Backlog del CRM está en Jira o GitHub Projects. Las stories tienen criterios de aceptación en Gherkin. Las tareas técnicas son: crear el módulo, implementar el Handler, el Repository, los tests, actualizar CHANGELOG.

---

### Sprint Planning
**Categoría:** Agile / Scrum
**Definición:** Ceremonia de ~2 horas al inicio de cada sprint donde el equipo selecciona historias del backlog (ordenado por valor de negocio), las descompone en tareas técnicas y las estima en Story Points. El resultado es el Sprint Backlog y el Sprint Goal.
**En el proyecto:** En el Sprint Planning del CRM, el equipo estima con Planning Poker. Las reference stories son el ancla: "¿es más complejo que un GET /contacts/:id (3pts)? ¿Menos que un webhook de WhatsApp (8pts)?".

---

### Sprint Review
**Categoría:** Agile / Scrum
**Definición:** Ceremonia al final del sprint donde el equipo hace una demo al Product Owner y stakeholders de las historias completadas (que cumplen el DoD). Es una conversación, no una presentación formal. El PO acepta o rechaza el trabajo según los criterios de aceptación.
**En el proyecto:** En el Sprint Review del CRM, el equipo demuestra las funcionalidades con Postman o en la UI. Las historias que no cumplen el DoD completo no se demuestran - vuelven al backlog.

---

### Squash & Merge
**Categoría:** Git
**Definición:** Estrategia de merge donde todos los commits de un feature branch se combinan en un único commit antes de mergearse a `main`. Mantiene el historial de `main` limpio y legible (un commit por feature/fix). Los commits intermedios del desarrollo quedan ocultos pero el PR los conserva.
**En el proyecto:** El CRM usa Squash & Merge como estrategia estándar de merge. El mensaje del commit squashed debe seguir Conventional Commits: `feat(contacts): add search by name and email (#42)`.

---

### Stateless (API sin estado)
**Categoría:** Escalabilidad - Arquitectura
Propiedad de una aplicación donde cada petición es completamente autocontenida y no depende de estado guardado en memoria de la instancia anterior. Prerequisito para escalar horizontalmente. Todo estado persiste en DB, Redis u otro almacén compartido.

**En el proyecto:** La API de .NET es stateless. El JWT contiene el userId y tenantId. No hay Session en memoria. Cualquier instancia puede procesar cualquier petición.

---

### static class
**Categoría:** C#
**Definición:** Clase que no puede instanciarse ni heredarse. Solo puede contener miembros estáticos. Se usa para agrupar métodos que no necesitan estado de instancia: extension methods, métodos utilitarios, constantes. En el back-template, los `ServiceCollectionEx` de cada módulo son clases estáticas.
**En el proyecto:** `ServiceCollectionEx` en cada módulo del CRM es `static class` con extension methods. También las clases de extensión de `Program.cs`: `JwtAuthExtensions`, `CorsExtensions`, `SwaggerExtensions` en `Host.Api/Extensions/`.

---

### Story Point
**Categoría:** Agile / Estimación
**Definición:** Unidad relativa de medición del esfuerzo, complejidad e incertidumbre de una historia de usuario. No corresponde directamente a horas - mide complejidad relativa. Una historia de 5 puntos no tarda el doble que una de 2.5 horas, pero sí tiene el doble de complejidad relativa que una de 2.5 puntos.
**En el proyecto:** El equipo del CRM usa Story Points en escala Fibonacci. Los puntos son consistentes entre sprints - si el equipo completa 21 puntos en un sprint, puede predecir cuántos sprints quedan para terminar el backlog.

---

### Structured Logging (Logging Estructurado)
**Categoría:** Logging - Observabilidad
Técnica de logging donde cada evento se registra como un objeto con propiedades tipadas (no solo texto plano). Permite filtrar, agregar y alertar sobre propiedades específicas. Se opone al logging concatenado de strings. Serilog es la librería de structured logging más usada en el ecosistema .NET.

**En el proyecto:** Toda la capa Application del CRM usa structured logging. La propiedad `TenantId` en cada log permite filtrar eventos por tenant en Seq o CloudWatch.

---

### switch expression
**Categoría:** C#
**Definición:** Expresión de C# 8+ que evalúa un valor contra múltiples patrones y retorna un resultado. Es la versión expresión del `switch` statement - más concisa y retorna un valor directamente. Soporta pattern matching, constantes, `_` como default y desestructuración.
**En el proyecto:** En Handlers del CRM: `var conversations = req.Channel switch { "whatsapp" => await _repo.GetByChannelAsync(...), "instagram" => await _repo.GetByChannelAsync(...), _ => await _repo.GetAllAsync(...) }`. Más legible que múltiples `if/else if`.

---

## T

### Task\<T\>
**Categoría:** C# / Async
**Definición:** Tipo genérico de C# que representa una operación asíncrona que retornará un valor de tipo `T`. Los métodos async retornan `Task<T>`. El llamador usa `await` para obtener el valor una vez que la operación completa. `Task` (sin T) representa una operación que no retorna valor.
**En el proyecto:** Todos los métodos async del CRM retornan `Task<T>`: `Task<GetUserProfileResponse>` para el Handler, `Task<UserProfile?>` para el Repository, `Task<IActionResult>` para el Controller. Nunca `async void` (excepciones no capturables).

---

### Tenant / TenantId
**Categoría:** Arquitectura / Multi-tenancy
**Definición:** En SaaS multi-tenant, un tenant es un cliente que usa la aplicación (una empresa, un workspace). El `tenantId` es el identificador único que distingue los datos de cada tenant. En el modelo Pool, cada fila de la base de datos pertenece a exactamente un tenant.
**En el proyecto:** En el CRM, cada workspace de la empresa es un tenant. El `tenantId` es un `long` en la base de datos y en el JWT. Viaja explícitamente en cada Request al Handler: `GetUserProfileRequest(Guid PublicId, long TenantId)`. Si alguien olvida pasarlo, no compila.

---

### TenantClaimsMiddleware
**Categoría:** .NET / Middleware / Multi-tenancy
**Definición:** Middleware de ASP.NET Core en `Host.Api/Middleware/` que procesa cada request HTTP y extrae el `tenant_id` del JWT (si está autenticado), poniéndolo disponible en el `ITenantContextAccessor`. Se ejecuta después del middleware de autenticación.
**En el proyecto:** `TenantClaimsMiddleware` en el CRM garantiza que cualquier request autenticado tenga el `tenantId` disponible. Los Controllers lo leen directamente del claim: `User.FindFirstValue("tenant_id")`.

---

### Thread pool (pool de hilos)
**Categoría:** .NET / Concurrencia
**Definición:** Conjunto de hilos de ejecución reutilizables mantenidos por el runtime. En ASP.NET Core, cada request toma un hilo del pool. Con async/await, el hilo se libera mientras espera I/O (base de datos, red), permitiendo que el mismo hilo sirva otras requests mientras espera. Maximiza el throughput con el mismo hardware.
**En el proyecto:** Un servidor del CRM con 8 hilos puede atender cientos de requests concurrentes si los Handlers son async de punta a punta. Si un Handler bloquea el hilo con `.Result`, ese hilo no puede servir otras requests durante los 200ms que tarda PostgreSQL.

---

### Throttling
**Categoría:** Escalabilidad - Rate Limiting
Técnica de ralentizar peticiones que superan una tasa (delay artificial) en lugar de bloquearlas (429). A diferencia del rate limiting, el cliente recibe 200 OK pero más lento. Útil para jobs internos, migraciones o scraping propio.

**En el proyecto:** El worker de campañas usa throttling interno: `await Task.Delay(100ms)` entre cada envío para mantenerse en 10 mensajes/segundo.

---

### Token Bucket (Rate Limiting)
**Categoría:** Rate Limiting - Algoritmos
Algoritmo de rate limiting que permite ráfagas cortas. El cubo tiene capacidad N. Se agregan K tokens por segundo. Cada petición consume 1 token. Si no hay tokens, la petición recibe 429. Usado por AWS API Gateway, Stripe, Twitter API.

**En el proyecto:** Alternativa al Sliding Window para endpoints que toleran ráfagas cortas, como la búsqueda de contactos.

---

### Transient lifetime
**Categoría:** .NET / Dependency Injection
**Definición:** Lifetime del contenedor de DI donde se crea una nueva instancia cada vez que el tipo se solicita. Correcto para objetos ligeros sin estado compartido. Si se inyectan en clases con lifetime más largo (Singleton), cada resolución crea una nueva instancia - no hay estado acumulado.
**En el proyecto:** Los servicios stateless del CRM que no tienen estado de request ni de proceso pueden ser Transient. La mayoría de los services son Scoped (por request) - Transient se usa para factories o helpers muy ligeros.

---

### TTL (Time To Live - Redis)
**Categoría:** Redis - Cache
Tiempo de vida de una clave en Redis. Al expirar, Redis elimina la clave automáticamente. Se establece con `EX` en el `SET`, con `EXPIRE` en claves existentes, o con `DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow` en ASP.NET Core. Clave sin TTL (`TTL key` devuelve -1) vive indefinidamente hasta eliminación manual.

**En el proyecto:** Todas las claves de cache tienen TTL. Sesiones de onboarding: 15 min. Cache de conversaciones: 5 min. Config de tenant: 10 min. Contadores de rate limit: 1 min. Sin TTL en las colas de campañas (deben persistir hasta ser procesadas).

---

## U

### User Secret (dotnet user-secrets)
**Categoría:** .NET / Seguridad
**Definición:** Mecanismo de .NET para almacenar secretos de desarrollo local fuera del repositorio, en una carpeta específica del usuario del sistema operativo. Se accede con `dotnet user-secrets set/get`. Solo aplica en el ambiente `Development`. En staging/producción se usan variables de entorno o AWS Secrets Manager.
**En el proyecto:** En el CRM, `Jwt__Key` y credenciales de WhatsApp se guardan con User Secrets para desarrollo local: `dotnet user-secrets set "Jwt:Key" "clave-local-dev-32-chars!!!!!" --project src/Host.Api`. Nunca se suben al repositorio.

---

### User Story
**Categoría:** Agile / Scrum
**Definición:** Descripción concisa de una funcionalidad desde la perspectiva del usuario final, en el formato: "Como [tipo de usuario] quiero [funcionalidad] para [beneficio]". Captura el quién, el qué y el por qué sin prescribir el cómo técnico. El cómo se negocia con el equipo técnico.
**En el proyecto:** Ejemplos del CRM: "Como agente de soporte, quiero ver todas las conversaciones sin atender en la bandeja, para priorizarlas y no perder clientes." Las stories bien redactadas incluyen criterios de aceptación en Gherkin.

---

## V

### ValidateOnStart
**Categoría:** .NET / Configuración
**Definición:** Método del builder de `IOptions<T>` que hace que la validación de configuración ocurra al arrancar la aplicación, antes de procesar cualquier request. Si la validación falla, la app no arranca. Implementa el principio de "fail fast" para la configuración.
**En el proyecto:** `builder.Services.AddOptions<JwtSettings>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart()` - el CRM no arranca si `Jwt:Key` falta o tiene menos de 32 caracteres. Mejor descubrirlo en deploy que a las 2 AM cuando falla el primer login.

---

### Value Object
**Categoría:** DDD / Arquitectura
**Definición:** Objeto de dominio que se define por su valor, no por su identidad. Dos Value Objects con los mismos valores son iguales. Son inmutables - para "cambiar" un Value Object se crea uno nuevo. En C#, los records son la implementación natural de Value Objects.
**En el proyecto:** En el dominio del CRM, `Email`, `PhoneNumber` o `WorkspaceName` podrían ser Value Objects: `public sealed record Email(string Value)`. Tienen reglas de validación propias y garantizan que nunca existe un `Email` inválido en the dominio.

---

### Velocity
**Categoría:** Agile / Story Points
**Definición:** Promedio de Story Points completados por sprint, calculado sobre los últimos 3-4 sprints. Se usa para predecir cuántos sprints faltan para completar el backlog o para comprometer cuánto trabajo cabe en el siguiente sprint. La velocity es un indicador - presionar para "subirla" lleva a inflar estimaciones.
**En el proyecto:** Si el equipo del CRM completa 21, 18, 24 y 22 puntos en los últimos 4 sprints, la velocity es ~21 puntos/sprint. Con 126 puntos en el backlog, se estiman 6 sprints (~12 semanas) para completarlos.

---

### Vertical Slice
**Categoría:** Arquitectura
**Definición:** Organización del código por feature (caso de uso) en lugar de por capa técnica. Cada feature contiene todas las capas que necesita: Controller, Handler, Repository, DTO. Facilita encontrar todo el código de una funcionalidad sin saltar entre carpetas de capas.
**En el proyecto:** El back-template del CRM organiza por módulo de negocio y dentro de cada módulo por caso de uso: `UseCases/GetUserProfile/` contiene Request, Handler, y Responses. Esto es Vertical Slice dentro de la arquitectura Modular Monolith.

---

## W

### Webhook
**Categoría:** Backend / Integraciones
**Definición:** Mecanismo donde un sistema externo llama activamente a un endpoint HTTP del sistema receptor cuando ocurre un evento, en lugar de que el receptor haga polling periódico. El receptor debe verificar la autenticidad de la llamada (firma HMAC) antes de procesarla.
**En el proyecto:** WhatsApp usa webhook para notificar al CRM de mensajes entrantes: Meta envía un `POST` al endpoint del CRM cuando llega un mensaje. Latencia < 1 segundo vs el polling que tendría hasta N segundos de latencia (ADR-003).

---

### `with` expression
**Categoría:** C#
**Definición:** Expresión de C# para crear una copia de un record con una o más propiedades modificadas. Como los records son inmutables, no se puede modificar una propiedad directamente - se crea una copia nueva. El original queda sin cambios. Útil en tests para crear variantes de un objeto base.
**En el proyecto:** `var inactivo = contactDto with { IsActive = false }` - el CRM puede crear variantes de DTOs en tests sin modificar el objeto base. Útil también para actualizar state: si el `Contact` tuviera que actualizarse, se crearía con `with` (aunque las entidades mapeadas por Dapper son `class` con setters).

---

### Workspace
**Categoría:** SaaS / Multi-tenancy
**Definición:** En el contexto del sistema de mensajería, el espacio aislado de cada empresa cliente. Equivale al concepto de "tenant" - cada empresa tiene su propio workspace con sus agentes, contactos, conversaciones y canales completamente aislados de otros workspaces.
**En el proyecto:** Cada cliente es un workspace en el CRM. El `tenantId` identifica el workspace en todas las queries. NOTA: el `ADR-002` anticipa que el término `workspace` podría cambiar a `organization` en la API pública en una versión futura (breaking change -> `v2.0.0`).

---

### Write-Through (Cache Pattern)
**Categoría:** Redis - Patrones de Cache
Patrón donde al actualizar la base de datos se actualiza también el cache en el mismo flujo, manteniendo ambos sincronizados inmediatamente. Alternativa a la invalidación de cache (borrar la clave y dejar que el siguiente acceso la reconstruya). Write-Through es más rápido para lecturas posteriores; la invalidación es más simple de implementar.

**En el proyecto:** Al actualizar el status de una conversación se invalida (borra) la clave del cache en lugar de usar write-through. La próxima lectura hará cache miss y reconstruirá desde PostgreSQL. Esta decisión simplifica la lógica de actualización.

---

## Y

### YAGNI (You Aren't Gonna Need It)
**Categoría:** Principios de Diseño / Extreme Programming
**Definición:** Principio que dice: no implementes funcionalidad que ningún requisito actual exige. Evita la sobreingeniería. Tiene una excepción importante: cuando el costo de agregar la abstracción después es significativamente mayor que hacerlo ahora - como el caso de las interfaces de repositorio (DIP).
**En el proyecto:** YAGNI no significa "no crees `IUserProfileRepository` porque solo hay una implementación". El costo de retrofittarla después (1-2 horas + riesgo de regresiones en todos los Handlers) supera el costo de crearla hoy (~5 minutos). Cuando DIP aplica, el costo de no hacerlo siempre supera el de hacerlo.

---

## Z

### 0.x.y (SemVer fase inestable)
**Categoría:** SemVer
**Definición:** Cuando `MAJOR = 0`, la API pública se considera inestable según SemVer. Cualquier versión puede introducir cambios breaking sin necesariamente incrementar MAJOR - el equipo decide. Se usa durante el desarrollo inicial hasta que el producto es estable y tiene usuarios reales.
**En el proyecto:** El CRM empieza en `v0.1.0`. Pasará a `v1.0.0` cuando el MVP esté en staging con el primer cliente real y el equipo considere que la API es estable y no planea cambios breaking en el corto plazo.

---

# Fin del Glosario - S03

**Rogelio Arriaga - Raptor Dev Services**
*Mayo 2026*
