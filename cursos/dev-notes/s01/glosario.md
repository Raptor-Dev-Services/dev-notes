# Glosario Técnico - S01
## Paradigmas - SOLID - Twelve-Factor App

> Definiciones de todos los términos técnicos usados en la sesión S01.
> Ordenado alfabéticamente.

---

## A

### Abstract record
**Categoría:** C# / POO
**Definición:** Tipo base en C# que combina la semántica de un `record` (inmutabilidad, igualdad por valor, deconstrucción) con la posibilidad de ser subclasificado. Al declararse `abstract`, no puede instanciarse directamente: solo sus subtipos concretos (`sealed record`) existen en tiempo de ejecución. Es el mecanismo preferido en el back-template para modelar respuestas discriminadas (éxito / fracaso).
**En el proyecto:** `GetUserResponse` (abstract) es la raíz; `GetUserSuccess` y `UserNotFoundFailure` son sus subtipos sellados. El `Presenter` hace pattern matching sobre el resultado sin necesidad de castings inseguros.

---

### ADR - Architecture Decision Record
**Categoría:** Arquitectura / Documentación
**Definición:** Documento corto y estructurado que registra una decisión arquitectónica significativa: contexto del problema, opciones evaluadas, la opción elegida y las consecuencias esperadas. Su formato más común es Markdown en el propio repositorio (carpeta `docs/adr/`). Se anticiparon tres ADRs clave para S02.
**En el proyecto:** ADR-001 documentará por qué se eligió monolito modular sobre microservicios. ADR-002 explicará la elección de PostgreSQL. ADR-003 justificará el uso de Dapper para queries de lectura frente a EF Core.

---

### AGENTS.md
**Categoría:** AI Workflow
**Definición:** Archivo de configuración del repositorio que establece las reglas de comportamiento para agentes de IA (Claude Code, Cursor, Copilot Workspace). Complementa al `CLAUDE.md`: mientras `CLAUDE.md` describe el proyecto y su arquitectura, `AGENTS.md` listas las restricciones operativas que el agente debe seguir al generar o modificar código - convenciones de naming, patrones prohibidos, maneras de registrar servicios, etc.
**En el proyecto:** Se creó en S01 con reglas como "no usar string interpolation en Serilog", "no usar `.Result`", "no usar `:latest` como tag de Docker". El agente leerá este archivo antes de tocar cualquier archivo del repo.

---

### appsettings.json
**Categoría:** .NET / Configuración
**Definición:** Archivo de configuración principal en aplicaciones ASP.NET Core. Define parámetros en formato JSON que la aplicación lee al iniciar. Soporta jerarquía: `appsettings.json` (valores base) + `appsettings.{Environment}.json` (sobreescrituras por ambiente). Las variables de entorno tienen mayor prioridad que ambos archivos.
**En el proyecto:** Vive en la capa `Host/`. Las cadenas de conexión a PostgreSQL, configuración de Redis y parámetros JWT se declaran ahí, pero los **valores sensibles** (contraseñas, tokens) llegan exclusivamente por variables de entorno o secretos de AWS - nunca hardcoded.

---

### async / await
**Categoría:** C# / Concurrencia
**Definición:** Palabras clave de C# que permiten escribir código asíncrono con sintaxis imperativa. `async` marca un método que puede suspenderse; `await` suspende la ejecución hasta que una operación de I/O (base de datos, red, disco) complete, liberando el hilo para atender otras solicitudes. Reemplaza el modelo de callbacks y evita el bloqueo de hilos.
**En el proyecto:** Todos los métodos de repositorio, handlers y endpoints del back-template son `async`. La firma estándar es `Task<T> GetByIdAsync(Guid id, CancellationToken ct)`. Nunca se usa `.Result` ni `.Wait()` porque bloquean el hilo y pueden producir deadlocks.

---

### AWS - Amazon Web Services
**Categoría:** Cloud
**Definición:** Plataforma de servicios cloud de Amazon. Ofrece cómputo (EC2, ECS, Lambda), almacenamiento (S3), bases de datos gestionadas (RDS), redes (VPC) y muchos otros servicios. El pipeline de CI/CD del proyecto usa autenticación OIDC con AWS para desplegar sin credenciales de larga duración almacenadas en GitHub.
**En el proyecto:** El ambiente de producción/staging del sistema de mensajería se desplegará en AWS. S3 se usará para media uploads con pre-signed URLs. GitHub Actions se autenticará con AWS via OIDC.

---

## B

### BCrypt
**Categoría:** Seguridad / Autenticación
**Definición:** Función de hash adaptativa diseñada específicamente para contraseñas. A diferencia de MD5 o SHA-256 (algoritmos rápidos diseñados para integridad de datos), BCrypt es deliberadamente lento y su costo computacional es configurable mediante un "factor de trabajo" (`work factor`). Incluye un salt aleatorio por defecto, lo que lo hace resistente a ataques de rainbow tables.
**En el proyecto:** Las contraseñas de usuarios (agentes y administradores) se almacenan en PostgreSQL como hashes BCrypt. Nunca se guarda la contraseña en texto plano.

---

### back-template
**Categoría:** Arquitectura / Back-end
**Definición:** Plantilla de proyecto .NET 10 con Clean Architecture completa provista por el instructor al inicio del curso. Incluye estructura de capas (Domain / Application / Infrastructure / WebApi / Host / Common), JWT, Dapper, Serilog y los patrones Result / Presenter ya implementados con código de ejemplo (entidades `ExampleUser`). No se explica desde cero - se desensambla pieza por pieza a lo largo del curso.
**En el proyecto:** Es la base del sistema de mensajería. El equipo clonó el template en S01, eliminó las entidades de ejemplo y comenzará a reemplazarlas con las entidades reales del dominio (Conversation, Contact, Message, Agent, Channel).

---

### Boy Scout Rule
**Categoría:** Clean Code
**Definición:** Principio de Robert C. Martin que establece que al salir de acampar debes dejar el campamento más limpio de como lo encontraste. Aplicado al código: cada vez que tocas un archivo, debes dejarlo ligeramente mejor - renombrar una variable confusa, extraer un método demasiado largo, eliminar código muerto. No requiere grandes refactors; mejoras pequeñas acumulativas son suficientes.
**En el proyecto:** Durante los code reviews semanales se evaluará si el PR dejó el código mejor. No se aceptarán PRs que empeoren el estado del código sin justificación.

---

## C

### CancellationToken
**Categoría:** C# / Concurrencia
**Definición:** Estructura de .NET que permite cancelar operaciones asíncronas de forma cooperativa. Cuando el cliente HTTP cancela una solicitud (cierra el navegador, timeout), el token se activa y el código puede detenerse limpiamente sin consumir recursos innecesarios. Se propaga por toda la cadena de llamadas: endpoint -> handler -> repositorio -> base de datos.
**En el proyecto:** Todos los métodos asíncronos del back-template reciben `CancellationToken ct` como último parámetro. Dapper y los drivers de PostgreSQL respetan el token automáticamente.

---

### CI/CD - Continuous Integration / Continuous Delivery
**Categoría:** DevOps
**Definición:** Prácticas que automatizan la construcción, prueba y despliegue de software. CI (Integración Continua) ejecuta compilación y tests en cada push. CD (Entrega Continua) automatiza el despliegue a un ambiente (staging o producción) cuando los tests pasan. Elimina el "deploy manual" que convierte cada release en un evento de riesgo.
**En el proyecto:** GitHub Actions implementará el pipeline completo más adelante. El pipeline construirá la imagen Docker multi-stage, ejecutará tests y desplegará a staging en AWS usando autenticación OIDC.

---

### CLAUDE.md
**Categoría:** AI Workflow
**Definición:** Archivo de instrucciones que Claude Code lee al iniciar en un repositorio. Documenta el propósito del proyecto, el stack tecnológico, la estructura de carpetas, las convenciones de código y las reglas que el agente debe respetar. Su función es dar contexto persistente al agente para que genere código coherente con el proyecto sin necesidad de repetir instrucciones en cada sesión.
**En el proyecto:** Se creó en S01 en la raíz del repo. Documenta el stack (.NET 10, PostgreSQL, React 19), la estructura del back-template y reglas como "nunca usar string interpolation en Serilog" o "nunca registrar un servicio Scoped dentro de un Singleton".

---

### Clean Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico propuesto por Robert C. Martin (Uncle Bob) que organiza el software en capas concéntricas con una regla de dependencia estricta: las capas externas dependen de las internas, nunca al revés. El dominio (reglas de negocio) está en el centro y no conoce bases de datos, frameworks ni interfaces de usuario. La inversión de dependencias hace posible sustituir Infrastructure sin tocar Domain o Application.
**En el proyecto:** El back-template implementa Clean Architecture con cinco capas: Domain (centro), Application, Infrastructure, WebApi y Host. Common es transversal. Esta estructura garantiza que cambiar de PostgreSQL a otra base de datos solo requiere modificar Infrastructure.

---

### Clean Code
**Categoría:** Principios de desarrollo
**Definición:** Filosofía y conjunto de prácticas para escribir código legible, mantenible y expresivo, popularizado por Robert C. Martin en su libro homónimo (2008). Abarca naming explícito, funciones pequeñas y de un solo propósito, ausencia de comentarios superfluos (el código debe ser autodocumentado), manejo de errores consistente y ausencia de code smells.
**En el proyecto:** Las convenciones de naming del back-template siguen directamente los principios de Clean Code: clases son sustantivos, métodos son verbos, booleanos son preguntas afirmativas (`IsActive`, `HasPermission`).

---

### Codebase (Twelve-Factor #I)
**Categoría:** Twelve-Factor App
**Definición:** El primero de los doce factores. Establece que debe existir un único repositorio de código fuente que se despliega en múltiples ambientes (desarrollo, staging, producción). Tener múltiples repositorios para el mismo sistema es un indicador de que debería ser descompuesto en servicios separados.
**En el proyecto:** Todo el sistema de mensajería vive en un único repo de GitHub. Las diferencias entre ambientes (staging vs producción) se manejan con variables de entorno, no con ramas diferentes del repositorio.

---

### Code Smell
**Categoría:** Clean Code / Refactoring
**Definición:** Término acuñado por Kent Beck y popularizado por Martin Fowler. Un code smell es una característica del código que sugiere la existencia de un problema de diseño más profundo. No es un bug - el código puede funcionar - pero indica que el diseño puede deteriorarse. Los smells más comunes son: Long Method, Duplicated Code, Long Parameter List, Feature Envy y Switch por tipo.

---

### Config (Twelve-Factor #III)
**Categoría:** Twelve-Factor App
**Definición:** El tercer factor. La configuración que varía entre ambientes (credenciales de base de datos, URLs de servicios externos, tokens de API) debe almacenarse en variables de entorno, nunca en el código fuente. La regla de oro: el código debe poder hacerse público sin revelar ningún secreto.
**En el proyecto:** Cadenas de conexión a PostgreSQL, secretos JWT y tokens de WhatsApp/Instagram se inyectan como variables de entorno. En desarrollo local se usan `appsettings.Development.json` y `dotnet user-secrets`. En producción, se usan los secretos de AWS Secrets Manager o Parameter Store.

---

### Commit convencional
**Categoría:** Git / DevOps
**Definición:** Formato estandarizado de mensajes de commit: `tipo(scope): descripción`. Los tipos más comunes son `feat` (nueva funcionalidad), `fix` (corrección de bug), `chore` (tareas de mantenimiento), `docs` (documentación), `refactor` (refactoring sin cambio de comportamiento), `test` (tests). Permite generar changelogs automáticos y calcular bumps de versión SemVer.
**En el proyecto:** El primer commit del repo fue `chore: initial project setup from back-template`. Todos los commits del equipo deben seguir esta convención. GitHub Actions puede leer los commits para determinar el tipo de bump de versión.

---

### CQRS - Command Query Responsibility Segregation
**Categoría:** Arquitectura
**Definición:** Patrón que separa las operaciones de escritura (Commands - modifican estado) de las de lectura (Queries - solo leen datos). Cada tipo tiene su propio modelo, handlers y potencialmente su propia base de datos. En su forma simple (sin event sourcing), CQRS mejora la claridad del código al hacer explícita la intención de cada operación.
**En el proyecto:** El back-template implementa CQRS ligero: los Handlers de comando usan repositorios de escritura (`IUserWriteRepository`) y los de consulta usan repositorios de lectura (`IUserReadRepository`). Esto es coherente con el principio de Interface Segregation (ISP).

---

## D

### Dapper
**Categoría:** Back-end / Base de datos
**Definición:** Micro-ORM de .NET creado por el equipo de Stack Overflow. Extiende `IDbConnection` con métodos como `QueryAsync<T>` y `ExecuteAsync` que mapean resultados SQL directamente a objetos C# con mínima configuración. Es más rápido que EF Core para queries complejas y da control total sobre el SQL generado, a costa de menos automatización.
**En el proyecto:** El back-template usa Dapper para todas las queries de lectura (repositorios de consulta en Infrastructure). Se prefirió Dapper sobre EF Core puro para queries porque el equipo mantiene control explícito del SQL - importante en consultas complejas como búsqueda FTS o feeds paginados.

---

### Dev/prod parity (Twelve-Factor #X)
**Categoría:** Twelve-Factor App
**Definición:** El décimo factor. Los ambientes de desarrollo, staging y producción deben ser lo más similares posible: mismas versiones de base de datos, misma configuración de caché, mismo sistema operativo. La divergencia entre ambientes es la causa número uno de bugs que "solo aparecen en producción".
**En el proyecto:** Docker resuelve la paridad desde S1. El equipo ejecuta localmente el mismo contenedor de PostgreSQL 16 y Redis que correrá en producción. No hay "en mi máquina funciona".

---

### Dev-notes
**Categoría:** Base de conocimiento / Capacitación
**Definición:** Repositorio de 56 documentos técnicos en formato Markdown entregados por el instructor al equipo en S01. Están organizados por dominio (fundamentos, programación, arquitectura, backend, bases de datos, frontend, git, contenedores, CI/CD, cloud, vibe coding). Son la fuente de verdad técnica del curso y deben consultarse antes de implementar cualquier patrón.
**En el proyecto:** Cada sesión referencia los archivos de dev-notes relevantes. El equipo debe leer el archivo correspondiente antes de llegar a clase - el instructor no los explica desde cero, asume que los leyeron.

---

### DI - Dependency Injection
**Categoría:** .NET / Arquitectura
**Definición:** Técnica por la cual una clase recibe sus dependencias desde el exterior (generalmente el contenedor IoC) en lugar de crearlas internamente con `new`. Tiene tres modalidades de ciclo de vida en ASP.NET Core: `Singleton` (una instancia por aplicación), `Scoped` (una instancia por solicitud HTTP) y `Transient` (nueva instancia cada vez que se solicita). La elección incorrecta del lifetime es una fuente frecuente de bugs sutiles.
**En el proyecto:** Los repositorios se registran como `Scoped` (viven por solicitud HTTP). Los servicios de infraestructura sin estado como `IHttpClientFactory` se registran como `Singleton`. Nunca se registra un servicio `Scoped` dentro de un `Singleton` - produce "captive dependency", un bug de concurrencia difícil de detectar.

---

### DIP - Dependency Inversion Principle
**Categoría:** SOLID
**Definición:** La D de SOLID. Los módulos de alto nivel (Application - lógica de negocio) no deben depender de módulos de bajo nivel (Infrastructure - base de datos, APIs externas). Ambos deben depender de abstracciones (interfaces). Las abstracciones no deben depender de detalles; los detalles deben depender de abstracciones. Hace posible sustituir implementaciones sin tocar la lógica de negocio.
**En el proyecto:** `Domain` define `IUserRepository`. `Infrastructure` implementa `UserRepository : IUserRepository` con Dapper+PostgreSQL. `Application` recibe `IUserRepository` por inyección de dependencias. Si se cambia a MongoDB, solo se modifica `Infrastructure` - `Application` no se toca.

---

### Docker
**Categoría:** Contenedores / DevOps
**Definición:** Plataforma de contenedores que empaqueta una aplicación junto con todas sus dependencias (runtime, librerías, configuración de sistema) en una unidad portátil llamada contenedor. Los contenedores son aislados, reproducibles y corren de forma idéntica en cualquier máquina que tenga Docker instalado, eliminando el problema de "en mi máquina funciona".
**En el proyecto:** El back-end y front-end del sistema de mensajería se contenerizan con Dockerfiles multi-stage. El ambiente de desarrollo usa Docker Compose para levantar PostgreSQL, Redis y la aplicación con un solo comando.

---

### Docker Compose
**Categoría:** Contenedores / DevOps
**Definición:** Herramienta que permite definir y ejecutar aplicaciones multi-contenedor. Un archivo `docker-compose.yml` describe los servicios (aplicación, base de datos, caché), sus imágenes, puertos, volúmenes y dependencias. Con `docker compose up` se levanta todo el stack en orden correcto.
**En el proyecto:** El `docker-compose.yml` del sistema de mensajería levantará: back-end .NET, PostgreSQL 16 y Redis. En una sesión posterior el equipo escribirá este archivo desde cero.

---

### Docker multi-stage build
**Categoría:** Contenedores / DevOps
**Definición:** Técnica de Dockerfile que usa múltiples etapas `FROM` en el mismo archivo. La primera etapa (`build`) usa una imagen pesada con el SDK completo para compilar el código. La segunda etapa (`runtime`) usa solo el runtime mínimo y copia los binarios compilados. El resultado es una imagen final mucho más pequeña y con menor superficie de ataque.
**En el proyecto:** El Dockerfile del back-end tiene dos etapas: `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build` para compilar, y `FROM mcr.microsoft.com/dotnet/aspnet:10.0` para el runtime. La imagen de producción no contiene el compilador ni el código fuente.

---

### Docker tag
**Categoría:** Contenedores / DevOps
**Definición:** Etiqueta que identifica una versión específica de una imagen Docker. El tag `:latest` siempre apunta a la imagen más reciente, lo que hace que los deploys no sean reproducibles (hoy `:latest` es v1.2.0, mañana puede ser v1.3.0). En producción se debe usar siempre un tag explícito: `:v1.2.3` o el SHA del commit de Git.
**En el proyecto:** El pipeline de GitHub Actions etiqueta las imágenes con la versión SemVer del release. El manifest de deployment en AWS siempre referencia el tag exacto: `mensajeria-api:v1.2.3`, nunca `mensajeria-api:latest`.

---

### Domain
**Categoría:** Clean Architecture / DDD
**Definición:** La capa más interna de Clean Architecture. Contiene las entidades del negocio, value objects, interfaces de repositorio y reglas de dominio. No depende de ninguna otra capa del proyecto ni de frameworks externos. Es el "corazón" de la aplicación - lo que permanece cuando cambias de base de datos o de framework web.
**En el proyecto:** La capa `Domain/` del back-template contiene las entidades como `User`, `Post`, `Community` y las interfaces `IUserRepository`, `IPostRepository`. Las clases concretas que acceden a PostgreSQL viven en `Infrastructure/`, no aquí.

---

### DRY - Don't Repeat Yourself
**Categoría:** Principios de diseño
**Definición:** Principio formulado por Andrew Hunt y David Thomas en *The Pragmatic Programmer* (1999). Establece que "cada pieza de conocimiento debe tener una representación única, inequívoca y autoritativa dentro de un sistema". La duplicación de lógica (no de texto) es el problema - si la misma regla de negocio existe en tres lugares, un cambio debe hacerse en tres lugares, y es casi seguro que uno quedará desactualizado.
**En el proyecto:** La validación del formato de email, las reglas de contraseña y los límites de longitud de campos son ejemplos de conocimiento que deben existir en un solo lugar. Se implementan como Value Objects en `Domain/` para que todos los handlers los usen sin duplicar la lógica.

---

## E

### EditorConfig
**Categoría:** Tooling / Convenciones
**Definición:** Archivo `.editorconfig` en la raíz del repositorio que define convenciones de formato de código (indentación, espacios vs tabs, fin de línea, encoding) que los editores respetan automáticamente. Garantiza consistencia entre los IDEs del equipo sin depender de configuración manual por persona.
**En el proyecto:** Se configuró en S01 junto con el `back-template`. Define que el proyecto usa 4 espacios de indentación, UTF-8 y LF como fin de línea - importante cuando el equipo tiene mezcla de Windows y macOS/Linux.

---

### EF Core - Entity Framework Core
**Categoría:** Back-end / Base de datos
**Definición:** ORM (Object-Relational Mapper) oficial de .NET que permite mapear clases C# a tablas de base de datos y ejecutar queries mediante LINQ sin escribir SQL explícito. Maneja migraciones de esquema, relaciones y tracking de cambios. Es más conveniente que Dapper para operaciones CRUD simples, pero genera SQL menos predecible en queries complejas.
**En el proyecto:** Se mencionó como una de las razones del ADR-003 (Dapper para queries). EF Core se usará en sesiones posteriores para operaciones de escritura donde las migraciones y el tracking de cambios aportan más valor.

---

### EndPoint
**Categoría:** WebApi / Clean Architecture
**Definición:** En el back-template (que usa Minimal API de ASP.NET Core), un `EndPoint` es una clase pequeña que registra una ruta HTTP, valida el cuerpo de la solicitud, llama al `Handler` a través del mediator y entrega el resultado al `Presenter`. Su único trabajo es ser el punto de entrada HTTP - no contiene lógica de negocio.
**En el proyecto:** Por ejemplo, `GetUserEndPoint` registra `GET /users/{id}`, recibe el `id` de la ruta, crea un `GetUserRequest` y lo despacha. El handler resuelve la lógica; el presenter formatea la respuesta HTTP.

---

### Extract Method
**Categoría:** Refactoring
**Definición:** Técnica de refactoring que consiste en extraer un fragmento de código de un método largo y convertirlo en un nuevo método con un nombre descriptivo. Reduce la longitud de los métodos, mejora la legibilidad y hace el código más testeable (los métodos pequeños son más fáciles de probar en aislamiento).
**En el proyecto:** Es el refactoring más aplicado en los code reviews. Un handler que tiene 60 líneas probablemente tiene varios pasos que pueden nombrarse explícitamente: `ValidateRequest()`, `FindExistingUser()`, `ApplyBusinessRule()`, `PersistChanges()`.

---

## F

### Feature Envy
**Categoría:** Code Smell
**Definición:** Code smell donde un método de una clase usa más datos y métodos de otra clase que los de la propia. Es señal de que el método está en el lugar equivocado - debería estar en la clase cuyos datos usa más.
**En el proyecto:** Si un método en `GetConversationHandler` accede repetidamente a propiedades de `Contact` para calcular algo, probablemente esa lógica debería vivir como método en `Contact` o en un servicio del dominio.

---

### Fowler, Martin
**Categoría:** Personas / Referencias
**Definición:** Ingeniero de software y autor influyente en ingeniería de software moderna. Autor de *Refactoring: Improving the Design of Existing Code* (1999, 2nd ed. 2018) - el catálogo de referencia de code smells y técnicas de refactoring. También documentó los patrones de arquitectura empresarial (*Patterns of Enterprise Application Architecture*, 2002) y es conocido por su blog martinfowler.com donde acuñó términos como "Microservices" y "Continuous Integration".
**En el proyecto:** La cita directa de la sesión es de Fowler: *"Don't start with microservices. Start with a monolith and extract microservices as you learn where the boundaries are."* Su libro *Refactoring* es lectura de referencia para S02+.

---

## G

### Generics
**Categoría:** C#
**Definición:** Característica de C# que permite definir clases, métodos e interfaces parametrizados por tipo. En lugar de escribir `List<User>` y `List<Post>` por separado, se escribe `List<T>` y el compilador genera la implementación concreta al usarse. Proporciona type safety en tiempo de compilación sin sacrificar reutilización.
**En el proyecto:** El back-template usa generics en interfaces como `ISuccess<T>` (donde `T` es el tipo del dato exitoso). Los métodos de repositorio como `Task<User?> GetByIdAsync(Guid id, CancellationToken ct)` usan el tipo concreto - no se usa `IRepository<T>` genérico porque viola ISP y complica las implementaciones.

---

### GitHub Actions
**Categoría:** CI/CD / DevOps
**Definición:** Plataforma de CI/CD integrada en GitHub. Los workflows se definen en archivos YAML en `.github/workflows/`. Cada workflow tiene triggers (push, pull request, schedule), jobs que corren en runners (máquinas virtuales) y steps que ejecutan comandos. Soporta autenticación OIDC con proveedores cloud (AWS, Azure) para evitar credenciales de larga duración.
**En el proyecto:** El pipeline del sistema de mensajería usará GitHub Actions para: compilar la imagen Docker, ejecutar tests, pushear la imagen a un registry y desplegar a staging en AWS usando OIDC (sin access keys almacenados en los secrets de GitHub).

---

### gitignore
**Categoría:** Git / Tooling
**Definición:** Archivo `.gitignore` que lista patrones de archivos y directorios que Git no debe rastrear. Previene que archivos generados (binarios, `bin/`, `obj/`), secretos (`.env`, `appsettings.Development.json` con credenciales reales) y archivos de IDE (`.vs/`, `.idea/`) lleguen al repositorio.
**En el proyecto:** Se configuró en S01. Incluye patrones para .NET (`bin/`, `obj/`, `*.user`), variables de entorno locales y archivos de rider/visual studio. Las cadenas de conexión reales nunca deben estar en el repositorio.

---

### Guid
**Categoría:** C#
**Definición:** Globally Unique Identifier - identificador de 128 bits generado de forma que la probabilidad de colisión es prácticamente nula. En .NET, `Guid.NewGuid()` genera un UUID v4 aleatorio. Se usa como identificador de entidades cuando se necesita que el cliente pueda generar el ID antes de la inserción, o cuando el sistema es distribuido.
**En el proyecto:** El back-template usa `long Id` para la clave primaria interna de base de datos y `Guid PublicId` para la clave expuesta en la API. El `PublicId` es el que viaja en las URLs (`GET /users/{publicId}`).

---

## H

### Handler
**Categoría:** Clean Architecture / CQRS
**Definición:** Clase en la capa `Application` que contiene la lógica de un caso de uso específico. Recibe un `Request` (o Command/Query en CQRS), coordina las llamadas a repositorios y servicios del dominio, y devuelve un `Response`. En el back-template, cada caso de uso tiene exactamente un Handler. El Mediator despacha el Request al Handler correcto.
**En el proyecto:** `GetUserHandler` recibe `GetUserRequest`, llama a `IUserReadRepository.GetByIdAsync()`, y devuelve `GetUserSuccess` o `UserNotFoundFailure`. No sabe nada de HTTP, ni de cómo se serializará la respuesta.

---

### Host
**Categoría:** Clean Architecture / .NET
**Definición:** La capa más externa del back-template. Contiene `Program.cs` (punto de entrada de la aplicación), `appsettings.json`, y el cableado de DI (registro de todos los servicios). Es el único lugar donde se permiten referencias a todas las capas simultáneamente - es el "pegamento" que une Domain, Application, Infrastructure y WebApi.
**En el proyecto:** `Program.cs` en `Host/` registra `services.AddScoped<IUserRepository, UserRepository>()`, configura Serilog, registra middleware y llama a `app.Run()`. No contiene lógica de negocio.

---

## I

### Infrastructure
**Categoría:** Clean Architecture
**Definición:** Capa que implementa las interfaces definidas en `Domain`. Contiene los repositorios concretos (acceso a PostgreSQL via Dapper), clientes de servicios externos (S3, WhatsApp API), y cualquier adaptador de tecnología. Es la única capa que puede tener dependencias de librerías externas de infraestructura.
**En el proyecto:** `UserRepository : IUserRepository` vive en `Infrastructure/`. Usa Dapper para ejecutar SQL contra PostgreSQL. Si se cambia de PostgreSQL a CockroachDB, solo se modifica esta capa.

---

### Interface
**Categoría:** C# / POO
**Definición:** Contrato que define un conjunto de métodos y propiedades que una clase debe implementar, sin proveer ninguna implementación concreta. En C#, se declara con `interface I{Nombre}`. Permite programar contra abstracciones (DIP) y facilita el testing mediante mocks o stubs.
**En el proyecto:** `IUserRepository` define los métodos de acceso a datos de usuarios. `INotificationChannel` define el contrato para envío de notificaciones. Las clases concretas en `Infrastructure` implementan estas interfaces.

---

### Interface Segregation Principle (ISP)
**Categoría:** SOLID
**Definición:** La I de SOLID. Los clientes no deben depender de interfaces que no usan. En lugar de una interfaz grande con muchos métodos, se prefieren varias interfaces pequeñas y cohesivas. Una clase puede implementar múltiples interfaces pequeñas sin que los consumidores que solo necesitan un subconjunto conten con el peso de los demás métodos.
**En el proyecto:** En lugar de una sola `IUserRepository` con `GetById`, `Insert` y `Update`, el back-template tiene `IUserReadRepository` (solo lectura) e `IUserWriteRepository` (escritura). Los Handlers de consulta dependen solo de `IUserReadRepository`.

---

### IoC - Inversion of Control
**Categoría:** Arquitectura / .NET
**Definición:** Principio de diseño donde el control del flujo de ejecución se invierte: en lugar de que el código de negocio instancie sus dependencias, un framework externo (contenedor IoC) las crea e inyecta. ASP.NET Core tiene un contenedor IoC integrado que gestiona los ciclos de vida (Singleton, Scoped, Transient) y resuelve el grafo de dependencias automáticamente.
**En el proyecto:** El contenedor IoC de ASP.NET Core está configurado en `Host/Program.cs`. Cuando un endpoint recibe una solicitud, el contenedor construye el Handler con todas sus dependencias resueltas automáticamente.

---

### IFailure
**Categoría:** back-template / Result Pattern
**Definición:** Interface del back-template que marca un `Response` como representación de un resultado fallido. Junto con `ISuccess<T>`, forma el sistema de tipos del Result Pattern. El `Presenter` hace pattern matching sobre el Response recibido y responde con el código HTTP apropiado según si implementa `ISuccess<T>` o `IFailure`.
**En el proyecto:** `UserNotFoundFailure` implementa `INotFoundFailure` (que extiende `IFailure`). El Presenter detecta esto y retorna `404 Not Found` con el mensaje de error, sin lanzar excepciones.

---

### ISuccess\<T\>
**Categoría:** back-template / Result Pattern
**Definición:** Interface genérica del back-template que marca un `Response` como resultado exitoso y portador de datos. `T` es el tipo del dato de éxito (generalmente un DTO). El Presenter detecta que el Response implementa `ISuccess<T>`, extrae el dato y retorna `200 OK` con el payload JSON.
**En el proyecto:** `GetUserSuccess` implementa `ISuccess<UserDto>`. Contiene el `UserDto` con los datos del usuario. El Presenter hace `response is ISuccess<UserDto> success` y retorna `Results.Ok(success.Data)`.

---

## J

### JWT - JSON Web Token
**Categoría:** Seguridad / Autenticación
**Definición:** Estándar abierto (RFC 7519) para transmitir información entre partes como un objeto JSON compacto y firmado digitalmente. Un JWT tiene tres partes: header (algoritmo), payload (claims - datos del usuario) y signature (verificación de integridad). El servidor valida la firma en cada solicitud sin consultar la base de datos (stateless).
**En el proyecto:** El sistema de mensajería usa JWT HS256 (HMAC-SHA256 con clave simétrica) para autenticación. El token incluye claims como `userId`, `workspaceId` y `role`. Se implementa más adelante.

---

## K

### KISS - Keep It Simple, Stupid
**Categoría:** Principios de diseño
**Definición:** Principio de ingeniería que favorece la solución más simple que resuelve el problema correctamente. Atribuido a Kelly Johnson (ingeniero aeronáutico de Lockheed, 1960). En software, significa resistir la tentación de agregar abstracciones, patrones o frameworks antes de que la complejidad del problema los justifique. La complejidad accidental (producida por el desarrollador) es el enemigo.
**En el proyecto:** Antes de agregar una abstracción al código, la pregunta obligatoria es: ¿qué problema concreto resuelve hoy? Un `IQueryStrategy` con `Factory` y `Director` para listar usuarios activos es KISS violado - basta con `GetAllActiveAsync()` en el repositorio.

---

## L

### LINQ - Language Integrated Query
**Categoría:** C# / Funcional
**Definición:** Conjunto de métodos de extensión en .NET que permiten consultar y transformar colecciones usando sintaxis funcional (lambdas). Métodos como `Where`, `Select`, `OrderBy`, `GroupBy`, `First` y `Any` operan sobre `IEnumerable<T>` e `IQueryable<T>`. LINQ es el ejemplo principal de programación funcional integrado en C#.
**En el proyecto:** Se usa LINQ en los repositorios para transformar resultados de Dapper, filtrar colecciones en memoria y proyectar DTOs. Ejemplo: `results.Select(r => new UserDto(r.Id, r.Email)).ToList()`.

---

### Liskov Substitution Principle (LSP)
**Categoría:** SOLID
**Definición:** La L de SOLID, formulada por Barbara Liskov (1987). Si `S` es un subtipo de `T`, cualquier instancia de `T` puede ser reemplazada por una instancia de `S` sin alterar el comportamiento correcto del programa. Viola LSP cuando una subclase lanza excepciones para métodos heredados, o cuando precondiciones son más fuertes o postcondiciones más débiles que en la base.
**En el proyecto:** Los `sealed record` `GetUserSuccess` y `UserNotFoundFailure` son sustitutos válidos de `GetUserResponse`. El Presenter puede tratar cualquiera de los dos polimórficamente sin sorpresas. Un `ReadOnlyList<T>` que hereda de `List<T>` y lanza en `Add()` viola LSP.

---

### Logs (Twelve-Factor #XI)
**Categoría:** Twelve-Factor App
**Definición:** El undécimo factor. Las aplicaciones no deben gestionar el enrutamiento o almacenamiento de sus logs. Deben escribir a `stdout` (salida estándar) como un stream de eventos ordenados cronológicamente. El ambiente de ejecución (Docker, Kubernetes) es responsable de capturar, almacenar y enrutar esos logs.
**En el proyecto:** Serilog está configurado para escribir a `stdout` en formato estructurado (JSON). Docker Compose captura esa salida. En producción, AWS CloudWatch o un stack ELK recoge los logs del contenedor. No se escriben archivos de log en disco.

---

### Long Method
**Categoría:** Code Smell
**Definición:** Código smell donde un método tiene demasiadas líneas (típicamente 20+, claramente problemático en 60+). Los métodos largos mezclan múltiples niveles de abstracción, son difíciles de leer, nombrar y probar. La solución habitual es Extract Method para crear métodos más pequeños con nombres que expliquen su propósito.
**En el proyecto:** Un Handler de más de 30 líneas es candidato a revisión. Si el handler hace validación, búsqueda, transformación, persistencia y notificación en secuencia, cada paso merece su propio método o servicio.

---

### Long Parameter List
**Categoría:** Code Smell
**Definición:** Code smell donde un método o constructor recibe 5 o más parámetros. Es difícil recordar el orden, y la firma cambia cada vez que se necesita un dato adicional. La solución es crear un objeto "Parameter Object" - en el back-template, este rol lo cumplen los `Request` records.
**En el proyecto:** En lugar de `CreateConversation(Guid contactId, Guid agentId, string channelType, string initialMessage, Guid workspaceId)`, se usa `CreateConversationRequest { ContactId, AgentId, ChannelType, InitialMessage, WorkspaceId }`.

---

## M

### Martin, Robert C. (Uncle Bob)
**Categoría:** Personas / Referencias
**Definición:** Ingeniero de software, autor y conferencista. Acuñó el acrónimo SOLID para cinco principios de diseño orientado a objetos. Autor de *Clean Code* (2008), *Clean Architecture* (2017) y *The Clean Coder* (2011). Fue uno de los firmantes del Manifiesto Ágil (2001). Su alias "Uncle Bob" viene de su rol de "el tío que te da consejos de código".
**En el proyecto:** SOLID y Clean Architecture - los pilares del back-template - provienen directamente de sus libros. *Clean Code* es lectura obligatoria del homework de S01.

---

### Mediator
**Categoría:** back-template / Patrón de diseño
**Definición:** Patrón de comportamiento que desacopla al emisor de un mensaje de su receptor. En el back-template, el Mediator interno (implementado en `Common/`) recibe un `Request` de un EndPoint y lo despacha al Handler registrado para ese tipo de Request. El EndPoint no conoce al Handler directamente - solo conoce al Mediator.
**En el proyecto:** `IMediator.SendAsync(request, ct)` en el EndPoint despacha hacia el Handler correspondiente. Esto permite agregar casos de uso sin que los EndPoints existentes cambien (OCP).

---

### Microservicios
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico donde el sistema se divide en múltiples servicios pequeños e independientes, cada uno con su propia base de datos, desplegable de forma autónoma y comunicándose por red (HTTP/gRPC/mensajería). Maximiza la independencia de despliegue pero requiere inversión significativa en infraestructura: service mesh, observabilidad distribuida, manejo de fallos de red, CI/CD por servicio.
**En el proyecto:** El sistema de mensajería NO comienza como microservicios. El equipo de 6 personas y el dominio aún no completamente definido hacen que la complejidad operacional no se justifique. La arquitectura de monolito modular permite extraer servicios en el futuro si el dominio lo requiere.

---

### Monolito
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico donde toda la aplicación se compila y despliega como un único binario. Simple al inicio, con una sola base de datos y sin comunicación por red entre módulos. La desventaja principal es que los módulos tienden a acoplarse con el tiempo si no hay disciplina de diseño, lo que dificulta el mantenimiento a medida que el sistema crece.
**En el proyecto:** El back-template es un monolito, pero modular (ver Monolito Modular). La diferencia es que tiene fronteras internas claras que evitan el acoplamiento, aunque corre en un único proceso.

---

### Monolito Modular
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que combina lo mejor del monolito (simplicidad operacional, un solo proceso, una base de datos) con fronteras internas bien definidas entre módulos (como si fueran microservicios lógicos). Cada módulo es responsable de su dominio, tiene su propia lógica y puede evolucionar de forma semi-independiente. Si un módulo crece lo suficiente, se puede extraer como microservicio con menor riesgo.
**En el proyecto:** El sistema de mensajería es un monolito modular. Los módulos lógicos son `Auth`, `Users`, `Conversations`, `Contacts`, `Messages`, `Channels`, `Bots`, etc. Cada módulo tiene su carpeta en `Application/UseCases/`. No hay comunicación por red entre ellos - se llaman directamente.

---

## N

### Naming - Convenciones de naming
**Categoría:** Clean Code
**Definición:** Reglas sobre cómo nombrar clases, métodos, variables e interfaces para maximizar la legibilidad. El nombre debe revelar la intención sin necesidad de comentarios. Las convenciones del back-template son: clases = sustantivos (`User`, `Channel`), métodos = verbos (`GetByIdAsync`, `CreatePost`), booleanos = preguntas afirmativas (`IsActive`, `CanDelete`), interfaces = `I` + sustantivo (`IUserRepository`).
**En el proyecto:** Los nombres de Handlers, Requests y Responses siguen el patrón `{Acción}{Entidad}{Tipo}`: `GetUserHandler`, `InsertUserRequest`, `GetUserResponse`. Esta consistencia permite entender el propósito de una clase sin leer su implementación.

---

### NpgsqlConnection
**Categoría:** Back-end / Base de datos
**Definición:** Clase del driver oficial de PostgreSQL para .NET (Npgsql). Es la implementación concreta de `IDbConnection` para PostgreSQL. Se usa con Dapper para ejecutar queries: `await conn.QueryAsync<User>(sql, parameters)`. El back-template inyecta `NpgsqlConnection` directamente en los repositorios de Infrastructure.
**En el proyecto:** Los repositorios de Infrastructure del back-template declaran `private readonly NpgsqlConnection _conn` en el constructor. La cadena de conexión se configura en `appsettings.json` y se inyecta por DI.

---

## O

### OCP - Open/Closed Principle
**Categoría:** SOLID
**Definición:** La O de SOLID, formulado por Bertrand Meyer (1988). Las entidades de software (clases, módulos, funciones) deben estar abiertas a extensión pero cerradas a modificación. Agregar nueva funcionalidad debe hacerse agregando nuevo código, no modificando el código existente. Se logra mediante abstracciones (interfaces) y polimorfismo.
**En el proyecto:** Agregar un nuevo canal de comunicación (WebPush, Telegram) no modifica los handlers existentes - solo se crea `WebPushChannel : INotificationChannel`. Agregar un nuevo caso de uso no modifica los handlers existentes - se crea un nuevo Handler.

---

### OIDC - OpenID Connect
**Categoría:** Seguridad / CI/CD
**Definición:** Protocolo de autenticación basado en OAuth 2.0 que permite a una aplicación verificar la identidad de un usuario (o sistema) a través de un proveedor de identidad externo. En el contexto de CI/CD, OIDC permite a GitHub Actions autenticarse con AWS sin necesidad de almacenar access keys de larga duración como secretos en GitHub.
**En el proyecto:** El pipeline de GitHub Actions más adelante usará OIDC para asumir un IAM Role en AWS. GitHub Actions demuestra su identidad con un token firmado; AWS verifica ese token y otorga acceso temporal. No hay `AWS_ACCESS_KEY_ID` guardado en ningún lado.

---

### OOP - Object-Oriented Programming
**Categoría:** Paradigmas
**Definición:** Paradigma de programación que organiza el software en objetos - estructuras que combinan estado (datos) y comportamiento (métodos). Los cuatro pilares son: encapsulamiento (ocultar el estado interno), herencia (reusar comportamiento de una clase base), polimorfismo (tratar distintos tipos de forma uniforme) y abstracción (modelar solo lo relevante del dominio).
**En el proyecto:** Las entidades del dominio (`User`, `Conversation`, `Message`) son objetos OOP en C#. Los Handlers y repositorios también. El paradigma OOP coexiste con elementos funcionales (LINQ, records inmutables).

---

## P

### Pattern Matching
**Categoría:** C#
**Definición:** Característica de C# (desde C# 7, extendida significativamente en C# 8+) que permite comparar un valor contra un patrón y, si coincide, extraer información de él. El `switch expression` con patrones de tipo es especialmente útil para discriminar sobre tipos de respuesta (Result Pattern).
**En el proyecto:** El `Presenter` usa pattern matching sobre el `Response` del Handler: `response switch { GetUserSuccess s => Results.Ok(s.Data), UserNotFoundFailure f => Results.NotFound(f.Msg), _ => Results.Problem() }`. Esto es más seguro que casting explícito.

---

### PostgreSQL
**Categoría:** Base de datos
**Definición:** Sistema de gestión de bases de datos relacional de código abierto, orientado a objetos. Es uno de los RDBMS más completos y robustos disponibles: soporta tipos JSON, arrays, full-text search nativo, window functions, Row Level Security (RLS), y extensiones como `pgcrypto` y `pg_trgm`. El ADR-002 del proyecto documenta por qué se eligió PostgreSQL.
**En el proyecto:** Es la base de datos principal del sistema de mensajería. Almacena usuarios, conversaciones, mensajes, contactos y configuración de canales. Se usa PostgreSQL 16 tanto en desarrollo (Docker) como en producción (AWS RDS o instancia propia).

---

### Presenter
**Categoría:** back-template / Clean Architecture
**Definición:** Clase en la capa `WebApi` responsable de transformar el `Response` del Handler en una respuesta HTTP concreta. Hace pattern matching sobre el tipo de Response y decide el código de estado (200, 404, 400, 500) y el cuerpo de la respuesta. Mantiene la separación entre la lógica de negocio (Handler) y el protocolo HTTP (Presenter).
**En el proyecto:** `GetExampleUserPresenter` recibe `GetExampleUserResponse` y retorna `IResult` de Minimal API. Si el response es `GetExampleUserSuccess`, retorna `Results.Ok(dto)`. Si es `GetExampleUserNotFoundFailure`, retorna `Results.NotFound(mensaje)`.

---

### Procedural (paradigma)
**Categoría:** Paradigmas
**Definición:** Paradigma de programación más básico: el código es una secuencia de instrucciones que el computador ejecuta en orden. No hay objetos ni funciones puras de primer orden - solo procedimientos (funciones que pueden tener efectos secundarios). Es el estilo más cercano al lenguaje ensamblador. C, SQL y scripts de bash son principalmente procedurales.
**En el proyecto:** Los scripts de migración SQL y los seeds de base de datos son procedurales por naturaleza. En C#, los procedurales puros son raros - el lenguaje empuja hacia OOP y funcional.

---

### Pull Request (PR)
**Categoría:** Git / Workflow
**Definición:** Mecanismo en GitHub (y GitLab/Bitbucket) para proponer cambios de una rama a otra. El autor crea el PR, describe los cambios, y uno o más revisores hacen code review antes de que el código se integre a la rama principal. Los PRs son la unidad de trabajo del equipo - cada homework se entrega como PR.
**En el proyecto:** El homework de S01 requiere crear un PR con comentarios que identifiquen ejemplos de SRP y DIP en el back-template y un code smell con propuesta de fix.

---

## R

### React
**Categoría:** Front-end
**Definición:** Librería de JavaScript para construir interfaces de usuario, desarrollada por Meta. Usa un modelo declarativo basado en componentes y un DOM virtual para actualizaciones eficientes. En su versión 19 introduce mejoras al modelo de concurrencia, Server Components estables y Actions para manejo de formularios.
**En el proyecto:** El front-end del sistema de mensajería usa React 19 con Vite como bundler y Tailwind v4 para estilos. El bot flow builder usa React Flow. TanStack Query maneja el estado del servidor (cache, refetch, mutations).

---

### Record (C#)
**Categoría:** C#
**Definición:** Tipo de referencia en C# (desde C# 9) diseñado para datos inmutables. A diferencia de las clases, los records implementan igualdad por valor automáticamente (`==` compara propiedades, no referencias), son inmutables por defecto con propiedades `init`, y soportan deconstrucción. Los `sealed record` no pueden heredarse; los `abstract record` sí.
**En el proyecto:** Los `Request` y `Response` del back-template son records: `public sealed record GetUserRequest(Guid PublicId)`. Los Responses discriminados son abstract records con subtipos sealed. La inmutabilidad evita mutaciones accidentales durante el procesamiento del Handler.

---

### Redis
**Categoría:** Back-end / Infraestructura
**Definición:** Base de datos en memoria de tipo clave-valor, de código abierto. Soporta estructuras de datos ricas (strings, hashes, listas, sets, sorted sets, pub/sub). Se usa principalmente para caché distribuida, sesiones, rate limiting y como backplane de SignalR para escalar horizontalmente los hubs de WebSocket.
**En el proyecto:** Redis se usará para caché de respuestas frecuentes y como backplane de SignalR para mensajes en tiempo real. En desarrollo corre en Docker junto con PostgreSQL.

---

### Refresh Token
**Categoría:** Seguridad / Autenticación
**Definición:** Token de larga duración que se usa para obtener un nuevo `Access Token` (JWT) cuando este expira, sin requerir que el usuario ingrese su contraseña nuevamente. El Access Token tiene vida corta (15-60 minutos) y el Refresh Token tiene vida larga (7-30 días). El Refresh Token se almacena en base de datos (o Redis) para poder revocarlo.
**En el proyecto:** El flujo de autenticación del sistema de mensajería implementa JWT + Refresh Token. El endpoint `POST /auth/refresh` recibe el Refresh Token y devuelve un nuevo par Access Token / Refresh Token.

---

### Repository Pattern
**Categoría:** Arquitectura / DDD
**Definición:** Patrón de diseño que abstrae el acceso a datos detrás de una interfaz orientada a colecciones. El repositorio parece una colección de entidades en memoria desde el punto de vista del Handler - no sabe si los datos vienen de PostgreSQL, MongoDB o un archivo CSV. La implementación concreta vive en Infrastructure.
**En el proyecto:** `IUserRepository` en `Domain/` es la interfaz de repositorio. `UserRepository` en `Infrastructure/` es la implementación con Dapper+PostgreSQL. El back-template NO usa un repositorio genérico `IRepository<T>` porque viola ISP.

---

### Request
**Categoría:** back-template / CQRS
**Definición:** Objeto inmutable (record en C#) que encapsula todos los datos necesarios para ejecutar un caso de uso. Es el mensaje que viaja del EndPoint al Handler a través del Mediator. Reemplaza la lista larga de parámetros del método (Long Parameter List). El Handler no conoce HTTP - solo conoce el Request.
**En el proyecto:** `InsertUserRequest { string Email, string Password, string DisplayName }` es un record que el EndPoint crea a partir del cuerpo JSON de la solicitud HTTP y lo despacha al Handler. Si se necesita un nuevo campo, se agrega al record sin cambiar la firma de ningún método.

---

### Response
**Categoría:** back-template / CQRS
**Definición:** Objeto inmutable (abstract record en C#) que representa el resultado de un caso de uso. Puede ser un éxito (implementa `ISuccess<T>`) o un fracaso (implementa `IFailure` con subtipo específico como `INotFoundFailure` o `IValidationFailure`). El Presenter hace pattern matching sobre el Response para generar la respuesta HTTP correcta.
**En el proyecto:** `GetUserResponse` (abstract) tiene dos subtipos: `GetUserSuccess(UserDto Data) : GetUserResponse, ISuccess<UserDto>` y `UserNotFoundFailure(string Msg) : GetUserResponse, INotFoundFailure`.

---

### Result Pattern
**Categoría:** back-template / Arquitectura
**Definición:** Patrón donde los métodos retornan un objeto que representa tanto el éxito como el fracaso, en lugar de lanzar excepciones para flujos de negocio esperados. Las excepciones se reservan para condiciones verdaderamente excepcionales (errores de infraestructura). El Result Pattern hace explícitos los casos de fallo en el tipo de retorno, forzando al llamador a manejarlos.
**En el proyecto:** Los Handlers devuelven subtipos de `Response` - el tipo del response hace explícito si el resultado es exitoso o fallido. El Presenter usa pattern matching en lugar de `try/catch` para manejar los distintos casos.

---

### Refactoring
**Categoría:** Ingeniería de software
**Definición:** Proceso de modificar la estructura interna del código sin cambiar su comportamiento observable externamente. El objetivo es mejorar la legibilidad, mantenibilidad y extensibilidad. No agrega funcionalidad ni corrige bugs - solo mejora el diseño. El libro de referencia es *Refactoring* de Martin Fowler (2018, 2da edición).
**En el proyecto:** Los code reviews semanales incluyen una sección de refactoring sugerido. El Boy Scout Rule impulsa pequeños refactorings en cada PR. Técnicas comunes: Extract Method, Introduce Parameter Object, Replace Conditional with Polymorphism.

---

## S

### S3 - Amazon Simple Storage Service
**Categoría:** Cloud / AWS
**Definición:** Servicio de almacenamiento de objetos de Amazon Web Services. Permite almacenar archivos de cualquier tamaño con alta durabilidad (99.999999999%) y disponibilidad. Soporta pre-signed URLs que permiten al cliente subir o descargar archivos directamente a S3 sin pasar por el servidor de la aplicación.
**En el proyecto:** El módulo de Media Upload usará S3 (o MinIO en desarrollo local) para almacenar adjuntos de conversaciones, imágenes de contactos y media de campañas. Los clientes recibirán pre-signed URLs para subir directamente a S3.

---

### Scoped (DI Lifetime)
**Categoría:** .NET / DI
**Definición:** Ciclo de vida de servicio en el contenedor IoC de ASP.NET Core donde se crea una instancia por solicitud HTTP. Todos los servicios resueltos durante la misma solicitud comparten la misma instancia. Es el lifetime correcto para repositorios (que encapsulan una conexión de base de datos por solicitud) y DbContexts de EF Core.
**En el proyecto:** Los repositorios (`UserRepository`, `ConversationRepository`) se registran como `Scoped`. Si se registraran como `Singleton`, múltiples solicitudes concurrentes compartirían la misma conexión de base de datos - causando errores de concurrencia.

---

### Sealed
**Categoría:** C#
**Definición:** Modificador de C# aplicado a clases y records que impide que sean heredados. Una clase `sealed` comunica explícitamente "este tipo no está diseñado para ser base de herencia". Permite al compilador y runtime hacer optimizaciones (devirtualización). En el back-template, los subtipos concretos de Response son `sealed record`.
**En el proyecto:** `GetUserSuccess` es `sealed record` - nadie debe heredar de él. Esto garantiza que el pattern matching del Presenter sea exhaustivo y que el compilador pueda avisar si falta un caso.

---

### SemVer - Semantic Versioning
**Categoría:** DevOps / Tooling
**Definición:** Estándar de versionado de software en formato `MAJOR.MINOR.PATCH`. `PATCH` se incrementa para bug fixes compatibles hacia atrás. `MINOR` se incrementa al agregar funcionalidad nueva compatible hacia atrás - la feature no cambia la API existente. `MAJOR` se incrementa cuando hay cambios que rompen compatibilidad hacia atrás (breaking changes).
Las imágenes Docker se etiquetan con el tag SemVer del release.

---

### Serilog
**Categoría:** Back-end / Observabilidad
**Definición:** Librería de logging estructurado para .NET. A diferencia de `Console.WriteLine`, Serilog registra eventos como objetos con propiedades tipadas (no solo strings), lo que permite búsquedas y filtros eficientes en sistemas de observabilidad (Elasticsearch, Seq, CloudWatch). Soporta múltiples "sinks" (destinos de salida): console, archivos, Elasticsearch, etc.
**En el proyecto:** Serilog está configurado en el back-template para escritura estructurada a stdout. La regla crítica: nunca usar string interpolation en Serilog (`$"User {userId}"`) - siempre usar message templates (`"User {UserId}", userId`). La interpolación derrota el propósito del logging estructurado.

---

### Service Mesh
**Categoría:** Arquitectura / Microservicios
**Definición:** Capa de infraestructura que gestiona la comunicación entre microservicios: descubrimiento de servicios, load balancing, circuit breaking, retries, mTLS (comunicación encriptada entre servicios), y observabilidad distribuida. Herramientas como Istio o Linkerd implementan service meshes. Es uno de los requisitos que hacen los microservicios complejos desde el día 1.
**En el proyecto:** El sistema de mensajería no necesita un service mesh porque es un monolito modular - la comunicación entre módulos es una llamada a método en memoria. Si en el futuro se extrae un servicio, se evaluará si el service mesh es necesario.

---

### SignalR
**Categoría:** Back-end / Real-time
**Definición:** Librería de ASP.NET Core que facilita la comunicación bidireccional en tiempo real entre servidor y clientes mediante WebSockets (con fallback a Long Polling y Server-Sent Events). El servidor puede "pushear" mensajes a clientes conectados sin que estos tengan que hacer polling. Los "Hubs" son el punto central de comunicación.
**En el proyecto:** Los mensajes de conversación en tiempo real se implementarán con SignalR. Cuando llega un nuevo mensaje de WhatsApp, el servidor notifica via WebSocket al agente que tiene la conversación abierta. Redis actúa como backplane para escalar horizontalmente.

---

### Singleton (DI Lifetime)
**Categoría:** .NET / DI
**Definición:** Ciclo de vida de servicio en el contenedor IoC de ASP.NET Core donde se crea exactamente una instancia para toda la vida de la aplicación. Todas las solicitudes HTTP comparten la misma instancia. Es apropiado para servicios sin estado o con estado compartido deliberadamente (configuración, caché en memoria, connection pools).
**En el proyecto:** `IHttpClientFactory` y servicios de configuración se registran como Singleton. NUNCA se resuelve un servicio Scoped desde un Singleton - produce "captive dependency": el servicio Scoped queda atrapado en el Singleton y su ciclo de vida se extiende incorrectamente.

---

### SOLID
**Categoría:** Principios de diseño
**Definición:** Acrónimo de cinco principios de diseño orientado a objetos formulados por Robert C. Martin: Single Responsibility (S), Open/Closed (O), Liskov Substitution (L), Interface Segregation (I), Dependency Inversion (D). Juntos guían el diseño de software hacia sistemas más mantenibles, extensibles y resistentes al cambio. Cada principio resuelve una fragilidad concreta y diferente.
**En el proyecto:** Los cinco principios están visibles en el back-template: SRP en la separación Handler/Sql/Presenter, OCP en el sistema de casos de uso extensibles, LSP en los Records de Response, ISP en las interfaces Read/Write, DIP en Domain->Interface->Infrastructure.

---

### SRP - Single Responsibility Principle
**Categoría:** SOLID
**Definición:** La S de SOLID. Una clase o módulo debe tener una única razón para cambiar - es decir, una única responsabilidad. Si una clase mezcla lógica de acceso a datos, reglas de negocio y formateo de respuesta, cambia cuando cualquiera de esas tres cosas cambia. Separar responsabilidades hace el código más cohesivo y los cambios más localizados.
**En el proyecto:** En el back-template, `GetUserHandler` orquesta el caso de uso, `ExampleUsersSql` accede a los datos, y `GetExampleUserPresenter` formatea la respuesta HTTP. Cada clase tiene una sola razón para cambiar.

---

### Stack (tecnológico)
**Categoría:** Arquitectura / General
**Definición:** Conjunto de tecnologías, frameworks y herramientas que conforman una aplicación. Se habla del stack del front-end, del back-end, de base de datos y de infraestructura/DevOps. La elección del stack debe estar documentada y justificada (ADRs).
**En el proyecto:** Back-end: .NET 10 + Dapper + Serilog. Base de datos: PostgreSQL 16 + Redis. Autenticación: JWT HS256 + BCrypt. Front-end: React 19 + Vite + Tailwind v4 + TanStack Query. DevOps: Docker + GitHub Actions (OIDC) + AWS. AI Workflow: Claude Code + CLAUDE.md + AGENTS.md.

---

### Strategy Pattern
**Categoría:** Patrones de diseño / Comportamiento
**Definición:** Patrón de comportamiento (GoF) que define una familia de algoritmos o comportamientos, los encapsula en clases separadas con una interfaz común, y los hace intercambiables en tiempo de ejecución. El contexto delega el comportamiento al Strategy seleccionado sin conocer los detalles de su implementación.
**En el proyecto:** El equipo no identificó el patrón en el quiz cuando se preguntó sobre lógica de pricing intercambiable. El ejemplo de la sesión: `INotificationChannel` es un Strategy - `EmailChannel`, `SmsChannel` y `WebPushChannel` son estrategias intercambiables. Se profundiza en S5 con la lógica de asignación de conversaciones.

---

### Structured Logging
**Categoría:** Observabilidad / Back-end
**Definición:** Técnica de logging donde cada evento se registra como un objeto con propiedades tipadas (pares clave-valor) en lugar de texto plano. Permite búsquedas, filtros y agregaciones precisas en sistemas como Elasticsearch o Kibana. `"User {UserId} logged in"` con `UserId = 42` es un log estructurado - el sistema puede filtrar `UserId = 42` sin parsear texto.
**En el proyecto:** Serilog implementa logging estructurado. La regla del proyecto es siempre usar message templates (`"Processing request {RequestId}"`, `requestId`) - nunca string interpolation que destruye la estructura.

---

## T

### TanStack Query
**Categoría:** Front-end
**Definición:** Librería de React (anteriormente React Query) para gestión del estado del servidor: fetching, caching, sincronización y actualización de datos remotos. Abstrae los patrones de loading/error/success, deduplicación de requests, revalidación en foco y optimistic updates. Complementa (no reemplaza) el estado local de React.
**En el proyecto:** El front-end del sistema de mensajería usa TanStack Query para todas las llamadas a la API del back-end. La bandeja de conversaciones, la lista de contactos y el historial de mensajes se manejan con queries de TanStack Query que se revalidan automáticamente.

---

### Tailwind CSS
**Categoría:** Front-end
**Definición:** Framework de CSS utilitario donde los estilos se aplican mediante clases predefinidas directamente en el HTML/JSX (`class="flex items-center gap-4 bg-blue-500 text-white"`). En su versión 4, usa una nueva arquitectura basada en variables CSS y un compilador más rápido. Elimina la necesidad de escribir CSS personalizado para la mayoría de los casos.
**En el proyecto:** El front-end del sistema de mensajería usa Tailwind v4 para todos los estilos. El design system del proyecto define los tokens de diseño (colores, espaciado, tipografía) como variables CSS que Tailwind consume.

---

### Transient (DI Lifetime)
**Categoría:** .NET / DI
**Definición:** Ciclo de vida de servicio en el contenedor IoC de ASP.NET Core donde se crea una nueva instancia cada vez que el servicio es solicitado. Apropiado para servicios ligeros y sin estado donde crear una nueva instancia es barato. Si el servicio tiene estado, `Transient` garantiza que no haya contaminación entre usos.
**En el proyecto:** Servicios de validación o transformación simples y sin estado se pueden registrar como Transient. Los Handlers en el back-template son típicamente Transient - cada solicitud obtiene una instancia fresca.

---

### Twelve-Factor App
**Categoría:** Arquitectura / DevOps
**Definición:** Metodología para construir software como servicio (SaaS) escalable y mantenible, formulada por Adam Wiggins (co-fundador de Heroku, 2012). Define doce factores: Codebase, Dependencies, Config, Backing services, Build/release/run, Processes, Port binding, Concurrency, Disposability, Dev/prod parity, Logs, Admin processes. Los factores más críticos para el proyecto son I, III, VI, X y XI.
**En el proyecto:** El sistema de mensajería sigue los doce factores desde S1: un repo en GitHub, configuración en variables de entorno, procesos stateless, paridad dev/prod via Docker, y logs estructurados a stdout con Serilog.

---

## U

### Uncle Bob
**Categoría:** Personas / Referencias
**Definición:** Alias de Robert C. Martin (ver Martin, Robert C.). El apodo "Uncle Bob" es ampliamente usado en la comunidad de desarrollo de software para referirse a él informalmente, especialmente en el contexto de sus libros (Clean Code, Clean Architecture) y sus principios SOLID.
**En el proyecto:** La arquitectura limpia del back-template está directamente inspirada en los principios de Uncle Bob. La regla de dependencias de Clean Architecture ("las flechas siempre apuntan hacia adentro") es su formulación.

---

## V

### Value Object
**Categoría:** DDD / Domain
**Definición:** Concepto de Domain-Driven Design (DDD). Un Value Object es un objeto definido por sus atributos (no por identidad). Dos Value Objects con los mismos atributos son iguales. Son inmutables - en lugar de modificarlos, se crea uno nuevo. Encapsulan reglas de validación del dominio. Ejemplos clásicos: `Email`, `Money`, `DateRange`, `PhoneNumber`.
**En el proyecto:** `Email` como Value Object garantiza que cualquier string de email en el sistema ya ha sido validado. No puede existir un `Email` inválido - su constructor lanza `ArgumentException` si el formato es incorrecto. Esto implementa DRY: la regla de validación vive en un solo lugar.

---

### Vertical Slice Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico donde el código se organiza por funcionalidad (feature) en lugar de por capa técnica. Cada "slice" (caso de uso) contiene todo lo necesario: endpoint, handler, repositorio y query SQL. Se mencionó como alternativa a la organización por capas horizontales. El back-template usa una variante que mantiene las capas pero agrupa dentro de ellas por caso de uso (`UseCases/` folder).
**En el proyecto:** La carpeta `Application/UseCases/` organiza el código por feature (caso de uso), no solo por capa. Cada caso de uso tiene su propio subfolder con su Handler, Request y Response. Esto facilita encontrar todo el código de un feature en un solo lugar.

---

### Vite
**Categoría:** Front-end / Tooling
**Definición:** Bundler y servidor de desarrollo para aplicaciones web modernas. A diferencia de Webpack (que bundlea todo antes de servir), Vite sirve módulos ES nativos en desarrollo (arranque instantáneo) y usa Rollup para el bundle de producción. Soporta HMR (Hot Module Replacement) ultrarrápido y configuración mínima.
**En el proyecto:** El front-end del sistema de mensajería usa Vite como bundler. El comando `vite build` genera los assets optimizados para producción. Las variables de entorno del front-end se configuran con el prefijo `VITE_` según el factor III de Twelve-Factor.

---

## W

### WebApi
**Categoría:** Clean Architecture / .NET
**Definición:** Capa del back-template que contiene los EndPoints (rutas HTTP), Presenters (formateo de respuestas) y Middleware de ASP.NET Core. Es la capa que enfrenta al cliente HTTP. No contiene lógica de negocio - solo traduce solicitudes HTTP en llamadas al Mediator y traduce los Responses del Handler en respuestas HTTP.
**En el proyecto:** `WebApi/` en el back-template contiene una carpeta por módulo funcional, con el EndPoint y Presenter correspondientes. Por ejemplo, `WebApi/Users/GetUserEndPoint.cs` y `WebApi/Users/GetUserPresenter.cs`.

---

### Wiggins, Adam
**Categoría:** Personas / Referencias
**Definición:** Co-fundador de Heroku y autor de la metodología The Twelve-Factor App (2012). Su contribución fue articular las mejores prácticas para construir aplicaciones SaaS escalables y mantenibles, destiladas de la experiencia de operar millones de apps en la plataforma de Heroku. El documento original está disponible en twelve-factor.net.
**En el proyecto:** La metodología Twelve-Factor define cómo el sistema de mensajería gestiona configuración, logs y paridad de ambientes. Es la referencia de facto para aplicaciones cloud-native.

---

## Y

### YAGNI - You Aren't Gonna Need It
**Categoría:** Principios de diseño
**Definición:** Principio de Extreme Programming (XP) formulado por Ron Jeffries. Establece que no se debe implementar funcionalidad hasta que sea concretamente necesaria para el requerimiento actual. La tentación de "hacerlo genérico por si acaso" o "agregar soporte para X que podríamos necesitar" genera código sin usar que aumenta la complejidad sin aportar valor. YAGNI habla de funcionalidad especulativa, no de calidad del código.
YAGNI dice: no implementes `SqlServerProvider` ni `MongoDbProvider` si el proyecto usa PostgreSQL. No dice nada sobre cuántos tests escribir.

---

*Glosario preparado por Rogelio Arriaga - Raptor Dev Services*
