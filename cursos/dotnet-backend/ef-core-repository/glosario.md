# Glosario Técnico  -  EF Core  -  Repository Pattern

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### AddDbContext
**Categoría:** EF Core / Configuración / DI
**Definición:** Método de extensión de `IServiceCollection` que registra `AppDbContext` con ciclo de vida Scoped. Cada request HTTP recibe una nueva instancia del contexto, lo que garantiza aislamiento entre requests y permite inyectar servicios Scoped (como `ITenantContextAccessor`) en el constructor del DbContext.
**En el proyecto:** Se usa en `Host/Program.cs` del sistema de mensajería para registrar `AppDbContext` con `UseNpgsql` y la connection string de PostgreSQL. Se elige sobre `AddDbContextPool` porque el template necesita inyectar `ITenantContextAccessor` (Scoped) en el constructor del contexto.

---

### AddDbContextPool
**Categoría:** EF Core / Configuración / Performance
**Definición:** Alternativa a `AddDbContext` que mantiene un pool de instancias de `DbContext` reutilizables. Reduce el overhead de instanciación, pero las instancias se resetean entre usos y el constructor del DbContext no puede recibir dependencias Scoped.
**En el proyecto:** No se usa en el back-template del sistema de mensajería porque `AppDbContext` recibe `ITenantContextAccessor` (Scoped) en el constructor. Conocerlo sirve para evaluar si futuros módulos sin esa restricción podrían beneficiarse del pool.

---

### ApplyConfigurationsFromAssembly
**Categoría:** EF Core / Fluent API / Configuración
**Definición:** Método de `ModelBuilder` que escanea un assembly en busca de todas las clases que implementan `IEntityTypeConfiguration<T>` y las registra automáticamente. Elimina la necesidad de llamar manualmente a `mb.ApplyConfiguration(new ContactConfiguration())` por cada entidad.
**En el proyecto:** Se llama en `AppDbContext.OnModelCreating` apuntando al assembly del propio contexto. Todas las clases `*Configuration` de los módulos (por ejemplo, `ContactConfiguration`) se registran en un solo paso sin líneas adicionales.

---

### AppDbContext
**Categoría:** EF Core / Persistencia
**Definición:** Clase central de EF Core que hereda de `DbContext`. Actúa como Unidad de Trabajo (Unit of Work) y como sesión con la base de datos. Expone los `DbSet<T>` de cada entidad, configura el mapeo en `OnModelCreating` y aplica los filtros globales de multi-tenancy. Su ciclo de vida en el back-template es Scoped.
**En el proyecto:** `Shared/Database/AppDbContext.cs` en el sistema de mensajería expone `DbSet<Tenant>`, `DbSet<UserCredential>`, `DbSet<UserProfile>` y los de cada módulo que se agregue (`Contact`, `Conversation`, `Message`). Recibe `ITenantContextAccessor` para leer el tenant del request actual.

---

### AsNoTracking
**Categoría:** EF Core / Performance / Consultas
**Definición:** Método de extensión que indica al Change Tracker que no debe rastrear las entidades retornadas por la consulta. Omitir el tracking reduce el consumo de memoria y CPU, ya que EF Core no genera snapshots del estado de las entidades. Debe usarse en todos los queries de solo lectura.
**En el proyecto:** Todos los métodos `Get*Async` de `ContactRepository` usan `AsNoTracking`. Los métodos `UpdateAsync` y `DisableAsync` evitan cargar la entidad completamente usando `ExecuteUpdateAsync`.

---

### AsSplitQuery
**Categoría:** EF Core / Performance / Consultas
**Definición:** Método que divide un query con múltiples `Include` en varias consultas SQL separadas en lugar de un único JOIN. Evita el problema del producto cartesiano cuando se incluyen colecciones grandes anidadas, a costa de múltiples roundtrips a la base de datos.
**En el proyecto:** Se considera en queries de `Conversation` con `Include(c => c.Messages).ThenInclude(m => m.Attachments)` para evitar que la combinación de relaciones genere una cantidad explosiva de filas en el resultado SQL.

---

### async / await
**Categoría:** C# / Programación asíncrona
**Definición:** Palabras clave del lenguaje C# para programación asíncrona basada en `Task`. `await` suspende la ejecución del método actual hasta que el `Task` completado esté disponible, liberando el thread para procesar otras solicitudes. `async void` es un anti-patrón porque no permite capturar excepciones ni hacer `await`.
**En el proyecto:** Todos los métodos de repositorio del sistema de mensajería son `async Task` o `async Task<T>`. Nunca se usa `.Result` (bloqueo síncrono) ni `async void`.

---

### async void
**Categoría:** C# / Anti-patrones
**Definición:** Variante de método asíncrono con tipo de retorno `void` en lugar de `Task`. Es un anti-patrón porque el caller no puede hacer `await` sobre él, las excepciones no se capturan correctamente y pueden derribar el proceso silenciosamente.
**En el proyecto:** Prohibido en el back-template del sistema de mensajería. Todo método asíncrono retorna `Task` o `Task<T>`.

---

## B

### BeginTransactionAsync
**Categoría:** EF Core / Transacciones / Unit of Work
**Definición:** Método de `DatabaseFacade` (accesible como `_db.Database.BeginTransactionAsync()`) que inicia una transacción explícita en la base de datos. Retorna un `IDbContextTransaction` que debe ser confirmado (`CommitAsync`) o revertido (`RollbackAsync`). Necesario cuando la operación incluye llamadas externas al DbContext.
**En el proyecto:** Se usa en el sistema de mensajería cuando un handler crea una `Conversation` y luego llama a `_channelService.NotifyAsync`. Si la notificación falla, el `RollbackAsync` deshace el insert de la conversación en la base de datos.

---

### bool (retorno de repositorio)
**Categoría:** Repository Pattern / Diseño
**Definición:** Tipo de retorno que usan los métodos de actualización y eliminación en los repositorios del back-template. El repositorio retorna `true` si se afectó al menos una fila y `false` si no se encontró el registro. El handler convierte este resultado en la respuesta de dominio correspondiente.
**En el proyecto:** `IContactRepository.UpdateAsync` y `DisableAsync` retornan `Task<bool>`. El `UpdateContactHandler` mapea `false` a `UpdateContactNotFoundFailure("Contacto no encontrado.")` y `true` a `UpdateContactSuccess()`.

---

## C

### CancellationToken
**Categoría:** C# / Programación asíncrona
**Definición:** Estructura que permite cancelar operaciones asíncronas cooperativamente. Se pasa como último parámetro (opcional, con valor por defecto `default`) en los métodos de repositorio. EF Core lo usa en `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync` y `ExecuteUpdateAsync` para cancelar el query si el cliente HTTP cierra la conexión.
**En el proyecto:** Todos los métodos de `IContactRepository` y `IUserProfileRepository` del sistema de mensajería reciben `CancellationToken ct = default`. El middleware de ASP.NET Core lo inyecta automáticamente desde el `HttpContext`.

---

### Change Tracker
**Categoría:** EF Core / Persistencia
**Definición:** Motor interno de EF Core que observa el estado de cada entidad cargada (Added, Modified, Deleted, Unchanged, Detached) y genera el SQL correcto al llamar `SaveChangesAsync`. Tiene un costo de memoria y CPU proporcional al número de entidades rastreadas. Se desactiva con `AsNoTracking` en consultas de solo lectura.
**En el proyecto:** El Change Tracker se activa solo en operaciones de escritura del sistema de mensajería (por ejemplo, en `InsertAsync` donde se hace `_db.Contacts.Add(contact)`). En todos los queries de lectura se desactiva con `AsNoTracking`. `ExecuteUpdateAsync` lo omite completamente.

---

### Clean Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que organiza el código en capas concéntricas donde las dependencias apuntan hacia adentro (hacia el dominio). Las capas externas (Infrastructure, Presentation) dependen de las capas internas (Domain, Application), nunca al revés. Facilita la testabilidad y el reemplazo de implementaciones.
**En el proyecto:** El back-template del sistema de mensajería sigue Clean Architecture: `Domain` define `IContactRepository`, `Application` lo usa en los handlers, `Infrastructure` lo implementa con `AppDbContext`. El handler no conoce EF Core directamente.

---

### CommitAsync
**Categoría:** EF Core / Transacciones
**Definición:** Método de `IDbContextTransaction` que confirma la transacción y hace permanentes todos los cambios realizados dentro de ella. Se llama después de que todas las operaciones han completado exitosamente. Si no se llama (por ejemplo, si se lanza una excepción), la transacción queda pendiente hasta el rollback o el timeout.
**En el proyecto:** Se llama al final del bloque `try` en los handlers que usan `BeginTransactionAsync` en el sistema de mensajería, por ejemplo después de que `_channelService.NotifyAsync` haya completado sin errores.

---

### Connection String
**Categoría:** Base de datos / Configuración
**Definición:** Cadena de texto que contiene los parámetros de conexión a la base de datos: host, puerto, nombre de base de datos, usuario y contraseña. En .NET se almacena en `appsettings.json` bajo la clave `ConnectionStrings:DefaultConnection` y se accede vía `IConfiguration.GetConnectionString`.
**En el proyecto:** En desarrollo del sistema de mensajería apunta a PostgreSQL local (`Host=localhost;Port=5432;Database=mensajeria`). En producción se reemplaza por un secreto inyectado vía variables de entorno o AWS Secrets Manager, nunca se hardcodea.

---

### ContactDto
**Categoría:** Contratos / DTO
**Definición:** Data Transfer Object que representa un contacto en la capa de contratos (`Contacts.Contracts/Dtos/`). Contiene solo los campos necesarios para la respuesta de la API, desacoplando la representación interna (entidad de dominio) de la representación que recibe el cliente HTTP.
**En el proyecto:** `new ContactDto(contact.PublicId, contact.FullName, contact.Email)` se construye en el handler `GetContactHandler` y se envuelve en `GetContactSuccess(contactDto)`. El `long Id` interno de la entidad nunca forma parte del DTO.

---

### ContactRepository
**Categoría:** Repository Pattern / Infrastructure
**Definición:** Implementación concreta de `IContactRepository` que vive en `Contacts.Infrastructure/Repositories/`. Contiene el código de acceso a datos con EF Core: referencia a `AppDbContext`, LINQ, `AsNoTracking`, `ExecuteUpdateAsync`. El resto del sistema solo conoce la interfaz `IContactRepository`.
**En el proyecto:** Registrado como Scoped en `Contacts.Infrastructure/ServiceCollectionEx.cs`. Implementa `GetByPublicIdAsync`, `GetPagedAsync`, `GetCountAsync`, `InsertAsync`, `UpdateAsync` y `DisableAsync` para la entidad `Contact` del sistema de mensajería.

---

### CRUD
**Categoría:** Diseño de APIs / Persistencia
**Definición:** Acrónimo de Create, Read, Update, Delete. Representa las cuatro operaciones básicas de persistencia. En el back-template, cada operación tiene su propio caso de uso (handler + request + response), lo que permite que cada una evolucione de forma independiente.
**En el proyecto:** `IContactRepository` expone las cuatro operaciones: `InsertAsync` (Create), `GetByPublicIdAsync` / `GetPagedAsync` (Read), `UpdateAsync` (Update) y `DisableAsync` (Delete lógico / soft delete) del sistema de mensajería.

---

## D

### Dapper
**Categoría:** ORM / Micro-ORM
**Definición:** Micro-ORM de .NET que extiende `IDbConnection` con métodos como `QueryAsync<T>` y `ExecuteAsync`. No tiene Change Tracker ni Migrations; mapea resultados de SQL manual a objetos C#. Es más rápido que EF Core en lecturas masivas al no tener overhead de tracking.
**En el proyecto:** Estrategia recomendada para queries complejos del sistema de mensajería: bandeja de conversaciones con filtros, reportes y Full Text Search. Los commands (escrituras) usan EF Core; los queries complejos usan Dapper con `IDbConnection` directamente.

---

### DatabaseFacade
**Categoría:** EF Core / API
**Definición:** Objeto accesible vía `DbContext.Database` que expone operaciones a nivel de base de datos: gestión de transacciones (`BeginTransactionAsync`), ejecución de SQL crudo (`ExecuteSqlRawAsync`) y aplicación de migraciones (`MigrateAsync`). Abstrae las operaciones que van más allá del mapeo O/R estándar.
**En el proyecto:** Se usa como `_db.Database.BeginTransactionAsync(ct)` en los handlers con transacciones explícitas, y como `db.Database.MigrateAsync()` en el startup de desarrollo y staging del sistema de mensajería.

---

### DbContext
**Categoría:** EF Core / Persistencia
**Definición:** Clase base de EF Core que representa una sesión con la base de datos. Combina los roles de Unit of Work (acumula cambios y los persiste atómicamente) y repositorio de alto nivel (expone los `DbSet`). Debe tener ciclo de vida Scoped en aplicaciones web para evitar compartir estado entre requests.
**En el proyecto:** `AppDbContext` en `Shared/Database/` hereda de `DbContext`. Es la clase que recibe `ITenantContextAccessor`, aplica `HasQueryFilter` y registra todas las configuraciones de entidades via `ApplyConfigurationsFromAssembly`.

---

### DbContextOptions\<T\>
**Categoría:** EF Core / Configuración
**Definición:** Objeto de configuración que contiene todas las opciones del `DbContext`: proveedor de base de datos, connection string, pooling y opciones de logging. Se pasa al constructor base de `DbContext` y es construido e inyectado automáticamente por el contenedor de DI.
**En el proyecto:** El constructor de `AppDbContext` recibe `DbContextOptions<AppDbContext> options` y lo pasa a `base(options)`. Se configura en `Program.cs` mediante `builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(...))`.

---

### DbSet\<T\>
**Categoría:** EF Core / Persistencia
**Definición:** Propiedad del `DbContext` que representa una tabla de la base de datos. Permite consultar (`AsNoTracking`, `Where`, `Include`), agregar (`Add`) y eliminar (`Remove`) entidades. EF Core traduce las operaciones LINQ sobre el `DbSet` a SQL en tiempo de ejecución.
**En el proyecto:** `AppDbContext` expone `DbSet<Tenant> Tenants`, `DbSet<UserCredential> Credentials`, `DbSet<UserProfile> UserProfiles`. Cada módulo del sistema de mensajería agrega su propio `DbSet`: `DbSet<Contact> Contacts`, `DbSet<Conversation> Conversations`.

---

### DIP (Dependency Inversion Principle)
**Categoría:** SOLID / Arquitectura
**Definición:** Quinto principio SOLID. Los módulos de alto nivel (Application) no deben depender de módulos de bajo nivel (Infrastructure); ambos deben depender de abstracciones. En la práctica: los handlers dependen de interfaces de repositorio, no de las implementaciones concretas.
**En el proyecto:** `GetContactHandler` recibe `IContactRepository` por constructor, no `ContactRepository`. No tiene referencia a `Microsoft.EntityFrameworkCore`. Esto permite reemplazar la implementación de persistencia o usar un mock en tests sin cambiar el handler.

---

### DisableAsync
**Categoría:** Repository Pattern / Soft Delete
**Definición:** Método de `IContactRepository` que implementa soft delete: actualiza `IsActive = false` y `UpdatedAtUtc = DateTime.UtcNow` usando `ExecuteUpdateAsync`, sin cargar la entidad en memoria. Retorna `true` si se afectó al menos una fila.
**En el proyecto:** Permite que los contactos del sistema de mensajería sean "eliminados" lógicamente sin borrar la fila en la base de datos. El `HasQueryFilter` filtra automáticamente los registros con `IsActive = false` en todos los queries normales del sistema.

---

### dotnet ef
**Categoría:** EF Core / CLI / Migraciones
**Definición:** Herramienta de línea de comandos para gestionar migraciones, scaffolding de base de datos y otras operaciones de EF Core. Requiere el paquete `Microsoft.EntityFrameworkCore.Tools` y el flag `--startup-project` apuntando al proyecto host de la aplicación.
**En el proyecto:** Se usa para crear migraciones (`dotnet ef migrations add`), aplicarlas (`dotnet ef database update`) y generar scripts SQL idempotentes para el pipeline de CI/CD del sistema de mensajería.

---

### DTO (Data Transfer Object)
**Categoría:** Arquitectura / Diseño
**Definición:** Objeto simple que transporta datos entre capas sin lógica de negocio. Los DTOs viven en el proyecto `*.Contracts/Dtos/` del módulo y son los tipos que el Presenter devuelve al cliente HTTP. Se construyen desde la entidad de dominio; la entidad nunca se expone directamente.
**En el proyecto:** `ContactDto(Guid PublicId, string FullName, string Email)` es el DTO que devuelve `GetContactSuccess` en el sistema de mensajería. Los campos internos como `long Id` o `long TenantId` nunca se incluyen en los DTOs de respuesta.

---

## E

### Eager Loading
**Categoría:** EF Core / Consultas
**Definición:** Estrategia de carga de relaciones que obtiene la entidad principal y sus entidades relacionadas en un único query (con JOIN). Se implementa con `Include` y `ThenInclude`. Evita el problema N+1 a costa de un query potencialmente más complejo o con más datos.
**En el proyecto:** Al consultar conversaciones con sus mensajes en el sistema de mensajería, se usa `_db.Conversations.Include(c => c.Messages)` para obtener todo en un solo roundtrip en lugar de cargar los mensajes uno por uno dentro de un bucle.

---

### EntityTypeBuilder\<T\>
**Categoría:** EF Core / Fluent API
**Definición:** Clase del Fluent API de EF Core recibida como parámetro en el método `Configure` de `IEntityTypeConfiguration<T>`. Expone métodos para configurar la tabla (`ToTable`), la clave primaria (`HasKey`), las propiedades (`Property`) y las relaciones (`HasMany`, `HasOne`).
**En el proyecto:** `ContactConfiguration` recibe `EntityTypeBuilder<Contact> builder` y lo usa para configurar la tabla `Contacts`, la PK, los índices únicos sobre `PublicId` y `Email`, y las restricciones de longitud de `FullName`.

---

### ExecuteDeleteAsync
**Categoría:** EF Core / Performance
**Definición:** Método introducido en EF Core 7 que ejecuta un `DELETE` SQL directamente sin cargar las entidades en memoria ni pasar por el Change Tracker. Equivalente de `ExecuteUpdateAsync` para eliminaciones físicas.
**En el proyecto:** No se usa directamente en el sistema de mensajería porque el patrón es soft delete (marcar `IsActive = false` con `ExecuteUpdateAsync`), pero es relevante para tareas de mantenimiento o background jobs de purga de datos históricos.

---

### ExecuteUpdateAsync
**Categoría:** EF Core / Performance
**Definición:** Método de EF Core 7+ que ejecuta un `UPDATE` SQL directamente sobre las filas que cumplen el `Where`, sin cargar las entidades en memoria ni pasar por el Change Tracker. Recibe una lambda con llamadas a `SetProperty`. Es la única forma de actualizar entidades con propiedades `init` sin cambiarlas a `set`.
**En el proyecto:** `UpdateAsync` y `DisableAsync` de `ContactRepository` usan `ExecuteUpdateAsync` para actualizar `FullName`, `IsActive` y `UpdatedAtUtc`. Esto reduce el número de roundtrips a la base de datos de dos (SELECT + UPDATE) a uno (UPDATE directo).

---

## F

### FirstOrDefaultAsync
**Categoría:** EF Core / Consultas
**Definición:** Método de extensión que retorna la primera entidad que cumple el predicado o `null` si no existe. Genera un `SELECT ... LIMIT 1` en SQL. Es el método correcto para buscar una entidad por su identificador único.
**En el proyecto:** `GetByPublicIdAsync` de `ContactRepository` usa `FirstOrDefaultAsync(e => e.PublicId == publicId && e.IsActive, ct)`. El handler comprueba si el resultado es `null` para retornar `GetContactNotFoundFailure("Contacto no encontrado.")`.

---

### Flyway / Liquibase
**Categoría:** Base de datos / Migraciones
**Definición:** Herramientas de gestión de migraciones de base de datos independientes del ORM. Gestionan scripts SQL versionados y garantizan que se apliquen en orden y una sola vez. Son alternativas a las migraciones de EF Core para equipos con DBA o bases de datos grandes.
**En el proyecto:** Se mencionan como alternativas para el pipeline de CI/CD del sistema de mensajería en producción, donde el equipo puede preferir revisar el SQL antes de aplicarlo en lugar de confiar en `MigrateAsync` automáticamente.

---

### Fluent API
**Categoría:** EF Core / Configuración
**Definición:** Sistema de configuración del modelo de EF Core basado en métodos encadenados sobre `ModelBuilder` y `EntityTypeBuilder`. Alternativa a las Data Annotations. Preferida en proyectos productivos porque mantiene las entidades de dominio libres de atributos de infraestructura.
**En el proyecto:** Toda la configuración de tablas, índices, longitudes y relaciones del sistema de mensajería se hace con Fluent API en clases `*Configuration` dentro de `*.Infrastructure/Configurations/`. Las entidades de dominio no tienen atributos de EF Core.

---

### FTS (Full Text Search)
**Categoría:** Base de datos / PostgreSQL
**Definición:** Capacidad de búsqueda de texto que analiza el contenido de columnas de texto, las tokeniza y permite búsquedas por relevancia. PostgreSQL ofrece FTS nativo con operadores `tsvector` y `tsquery`. Es más eficiente que `LIKE '%texto%'` para búsquedas en texto largo.
**En el proyecto:** La bandeja de entrada del sistema de mensajería usa FTS para buscar conversaciones y mensajes por contenido. Este tipo de query complejo se implementa con Dapper (SQL directo) en lugar de LINQ de EF Core.

---

## G

### gen_random_uuid()
**Categoría:** PostgreSQL / Funciones
**Definición:** Función de PostgreSQL que genera un UUID v4 aleatorio. Se usa como valor por defecto en columnas `PublicId` para que la base de datos asigne automáticamente un identificador único a cada fila insertada, sin que la aplicación deba generarlo.
**En el proyecto:** En `ContactConfiguration`, `builder.Property(c => c.PublicId).HasDefaultValueSql("gen_random_uuid()")` garantiza que cada `Contact` tenga un `PublicId` único asignado por PostgreSQL incluso si el repositorio no lo pasa explícitamente.

---

### GetByPublicIdAsync
**Categoría:** Repository Pattern / Consultas
**Definición:** Método de repositorio que busca una entidad por su identificador público (`Guid PublicId`) dentro de un tenant. Retorna `T?` (nullable). Es el método estándar para que los endpoints busquen una entidad por el identificador que el cliente HTTP proporciona en la URL.
**En el proyecto:** `IContactRepository.GetByPublicIdAsync(Guid publicId, long tenantId, CancellationToken ct)` retorna `Contact?`. El `GetContactHandler` comprueba `contact is null` para retornar `GetContactNotFoundFailure`.

---

### GetCountAsync
**Categoría:** Repository Pattern
**Definición:** Método de repositorio que retorna el número total de registros activos para un tenant. Se usa para calcular el total de páginas en paginación, sin traer todos los registros a memoria. Genera un `SELECT COUNT(*)` en SQL.
**En el proyecto:** `IContactRepository.GetCountAsync(long tenantId, CancellationToken ct)` permite que el endpoint de lista de contactos del sistema de mensajería incluya el total de elementos en la respuesta paginada.

---

### GetPagedAsync
**Categoría:** Repository Pattern / Paginación
**Definición:** Método de repositorio que retorna una página de resultados usando `Skip` y `Take`. Recibe `page` y `pageSize` y devuelve `IReadOnlyCollection<T>` (implementado con `ToArrayAsync`). Siempre incluye `AsNoTracking` y `OrderByDescending`.
**En el proyecto:** `ContactRepository.GetPagedAsync` retorna una página de contactos activos del sistema de mensajería ordenados por `CreatedAtUtc` descendente. El handler construye la respuesta paginada combinando este resultado con `GetCountAsync`.

---

### Global Query Filter
**Categoría:** EF Core / Multi-tenancy
**Definición:** Condición de filtrado que EF Core aplica automáticamente a todos los queries de una entidad, como un `WHERE` implícito. Se configura con `HasQueryFilter` en `OnModelCreating`. Se puede omitir en queries específicos con `IgnoreQueryFilters()`.
**En el proyecto:** `AppDbContext` aplica `HasQueryFilter(e => e.TenantId == CurrentTenantId)` a las entidades con soporte multi-tenant. Garantiza que ningún query del sistema de mensajería acceda accidentalmente a datos de otro workspace.

---

### Guid (PublicId)
**Categoría:** C# / Diseño de APIs
**Definición:** Tipo de identificador único universal de 128 bits. En el back-template se usa como `PublicId` para exponer entidades hacia el exterior. La PK interna es `long Id` (más eficiente en índices), mientras que el `Guid PublicId` es el identificador que el cliente usa en las requests.
**En el proyecto:** Los endpoints del sistema de mensajería usan `PublicId` (Guid) en las rutas (`GET /api/contacts/{publicId}`). El `long Id` nunca se expone en las respuestas ni en las URLs.

---

## H

### Handler
**Categoría:** Clean Architecture / Application
**Definición:** Clase de la capa Application que implementa `IRequestHandler<TRequest, TResponse>` y contiene la lógica de un caso de uso específico. Recibe las dependencias por constructor (repositorios, servicios) y orquesta la operación, retornando una respuesta de dominio.
**En el proyecto:** `GetContactHandler`, `UpdateContactHandler` y `CreateConversationHandler` son handlers del sistema de mensajería. Cada uno conoce solo las interfaces de repositorio que necesita, nunca la implementación concreta ni EF Core.

---

### HasDefaultSchema
**Categoría:** EF Core / Fluent API
**Definición:** Método de `ModelBuilder` que establece el schema por defecto para todas las tablas del contexto. Las configuraciones individuales pueden sobreescribirlo con `ToTable("nombre", "esquema")`.
**En el proyecto:** `AppDbContext.OnModelCreating` llama `mb.HasDefaultSchema("dbo")`. Las tablas que no especifiquen schema explícito heredan este valor en el sistema de mensajería.

---

### HasDefaultValueSql
**Categoría:** EF Core / Fluent API
**Definición:** Método que configura el valor por defecto de una columna usando una expresión SQL evaluada por la base de datos al momento del `INSERT`. A diferencia de los valores por defecto en C#, este se aplica en la DB y funciona incluso si la columna no viene en la sentencia SQL.
**En el proyecto:** `builder.Property(c => c.PublicId).HasDefaultValueSql("gen_random_uuid()")` en `ContactConfiguration` configura que PostgreSQL genere el UUID si la aplicación no lo provee.

---

### HasIndex / IsUnique
**Categoría:** EF Core / Fluent API / Base de datos
**Definición:** `HasIndex` crea un índice en la columna especificada para acelerar las búsquedas. `IsUnique()` encadenado convierte ese índice en un índice único, que impide valores duplicados a nivel de base de datos.
**En el proyecto:** `ContactConfiguration` define índices únicos sobre `Email` y `PublicId` de la tabla `Contacts`. Esto garantiza que no puedan existir dos contactos con el mismo email o el mismo identificador público en el sistema de mensajería.

---

### HasKey
**Categoría:** EF Core / Fluent API
**Definición:** Método del Fluent API que configura explícitamente la clave primaria de una entidad. Por convención, EF Core detecta automáticamente la PK si la propiedad se llama `Id`, pero `HasKey` añade claridad explícita y es necesario cuando el nombre difiere o se requiere una clave compuesta.
**En el proyecto:** `ContactConfiguration` usa `builder.HasKey(c => c.Id)` para ser explícito sobre cuál propiedad es la PK, siguiendo el principio de configuración explícita sobre convención del back-template.

---

### HasMany / WithOne
**Categoría:** EF Core / Fluent API / Relaciones
**Definición:** Métodos del Fluent API para configurar relaciones uno-a-muchos. `HasMany` define el lado "uno" que tiene múltiples del otro; `WithOne` define el lado "muchos" que apunta al "uno". Se encadena con `HasForeignKey` para especificar la columna de clave foránea.
**En el proyecto:** La relación entre `Conversation` y `Message` en el sistema de mensajería se configura con `builder.HasMany(c => c.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId)` en `ConversationConfiguration`.

---

### HasMaxLength
**Categoría:** EF Core / Fluent API
**Definición:** Método que configura la longitud máxima de una columna de texto. En PostgreSQL, las columnas `string` sin restricción se mapean a `text` (longitud ilimitada). `HasMaxLength` genera una columna `varchar(N)` con la restricción de longitud.
**En el proyecto:** `ContactConfiguration` aplica `HasMaxLength(200)` a `FullName` y `HasMaxLength(320)` a `Email` (320 es la longitud máxima de un email según RFC 5321).

---

### HasQueryFilter
**Categoría:** EF Core / Multi-tenancy / Filtros globales
**Definición:** Método de `ModelBuilder` que registra una condición de filtrado automático para una entidad. EF Core agrega esta condición a todos los queries de la entidad como un `WHERE` adicional. Se evalúa en tiempo de ejecución y puede usar valores dinámicos como el `TenantId` actual.
**En el proyecto:** `AppDbContext.OnModelCreating` llama `mb.Entity<UserProfile>().HasQueryFilter(e => e.TenantId == CurrentTenantId)`. Cada entidad del sistema de mensajería que requiera aislamiento por tenant agrega su propio `HasQueryFilter`.

---

## I

### IConflictFailure
**Categoría:** Result Pattern / Common
**Definición:** Interfaz de `Common.Results` que hereda de `IFailure` y marca una respuesta como un conflicto semántico. El Presenter lo mapea a una respuesta HTTP 409 Conflict. Se usa cuando la operación viola una regla de unicidad.
**En el proyecto:** Se usa cuando se intenta crear un `Contact` con un email que ya existe en el workspace del sistema de mensajería. El Presenter detecta `IConflictFailure` y retorna `409 Conflict` al cliente.

---

### IContactRepository
**Categoría:** Repository Pattern / Domain
**Definición:** Interfaz que define el contrato de acceso a datos para la entidad `Contact`. Vive en `Contacts.Domain/Repositories/` sin referencias a EF Core ni a `Shared.Database`. Los handlers de la capa Application dependen de esta interfaz, no de la implementación concreta.
**En el proyecto:** Define `GetByPublicIdAsync`, `GetPagedAsync`, `GetCountAsync`, `InsertAsync`, `UpdateAsync` y `DisableAsync`. Permite que los tests unitarios de los handlers usen mocks sin necesidad de una base de datos.

---

### IDbContextTransaction
**Categoría:** EF Core / Transacciones
**Definición:** Interfaz que representa una transacción de base de datos abierta. Retornada por `BeginTransactionAsync`. Expone `CommitAsync` y `RollbackAsync`. Debe usarse con `await using` para garantizar su liberación correcta aunque ocurra una excepción.
**En el proyecto:** Se usa en handlers del sistema de mensajería que coordinan escrituras en la base de datos con llamadas a servicios externos, como al crear una conversación y notificar el canal de WhatsApp o Instagram.

---

### IEntityTypeConfiguration\<T\>
**Categoría:** EF Core / Fluent API
**Definición:** Interfaz de EF Core que se implementa para configurar el mapeo de una entidad de forma separada del `DbContext`. Cada clase implementa `Configure(EntityTypeBuilder<T> builder)` y define la tabla, columnas, índices y relaciones de la entidad `T`. Se registran automáticamente con `ApplyConfigurationsFromAssembly`.
**En el proyecto:** Cada entidad del sistema de mensajería tiene su clase de configuración: `ContactConfiguration`, `ConversationConfiguration`, `MessageConfiguration`, en el directorio `*.Infrastructure/Configurations/` de su módulo.

---

### IFailure
**Categoría:** Result Pattern / Common
**Definición:** Interfaz base de la jerarquía de fallos del back-template. Tiene una propiedad `string Message` que describe el error. Las sub-interfaces (`IValidationFailure`, `INotFoundFailure`, `IConflictFailure`) permiten que el Presenter determine el código HTTP adecuado con pattern matching.
**En el proyecto:** El `GetContactPresenter` usa `if (notification is IFailure failure)` para capturar cualquier tipo de fallo y llamar `_viewModel.Fail(failure.Message)`, independientemente del tipo específico de error.

---

### IgnoreQueryFilters
**Categoría:** EF Core / Filtros globales
**Definición:** Método de extensión que desactiva todos los `HasQueryFilter` para un query específico. Se usa cuando se necesita acceder a datos de todos los tenants o a registros marcados como inactivos, como en funciones de administración o auditoría.
**En el proyecto:** Un super-admin del sistema de mensajería que necesite listar todos los contactos inactivos de todos los workspaces usaría `_db.Contacts.IgnoreQueryFilters().Where(e => !e.IsActive)` para ignorar el filtro de tenant activo.

---

### Include
**Categoría:** EF Core / Eager Loading
**Definición:** Método LINQ de EF Core que instruye al query builder a incluir una entidad relacionada mediante un JOIN. Recibe una expresión lambda que apunta a la propiedad de navegación. Para relaciones anidadas se encadena con `ThenInclude`.
**En el proyecto:** `_db.Conversations.AsNoTracking().Include(c => c.Messages).Where(c => c.AgentId == agentId)` carga las conversaciones de un agente con todos sus mensajes en un solo query SQL, evitando el problema N+1.

---

### `init` (propiedad)
**Categoría:** C# / Diseño de entidades
**Definición:** Modificador de acceso introducido en C# 9 que permite asignar una propiedad solo durante la inicialización del objeto (en el constructor o en un object initializer), pero no después. Hace las propiedades inmutables una vez construido el objeto. EF Core puede hidratar propiedades `init` correctamente al leer de la base de datos.
**En el proyecto:** Todas las propiedades de las entidades del back-template usan `init` en lugar de `set`. Esto significa que `ExecuteUpdateAsync` es la única forma de actualizar una entidad persistida, ya que la asignación `contact.FullName = value` no compila.

---

### InsertAsync
**Categoría:** Repository Pattern
**Definición:** Método de los repositorios del back-template que recibe parámetros escalares (no una entidad) y construye la entidad internamente con un object initializer antes de llamar a `_db.Set.Add(entity)` y `SaveChangesAsync`. El handler no construye la entidad; solo pasa los valores.
**En el proyecto:** `ContactRepository.InsertAsync(Guid publicId, long tenantId, string fullName, string email, CancellationToken ct)` construye el `Contact` internamente y lo persiste. El handler pasa los valores escalares sin conocer la estructura de la entidad.

---

### INotFoundFailure
**Categoría:** Result Pattern / Common
**Definición:** Sub-interfaz de `IFailure` que indica que el recurso buscado no existe. El Presenter lo mapea a una respuesta HTTP 404 Not Found. Se implementa en records del tipo `*NotFoundFailure`.
**En el proyecto:** `GetContactNotFoundFailure` implementa `INotFoundFailure` y se retorna cuando `GetByPublicIdAsync` devuelve `null`. El Presenter de `GetContact` detecta este tipo con pattern matching y responde con 404.

---

### IReadOnlyCollection\<T\>
**Categoría:** C# / Colecciones
**Definición:** Interfaz de colección que expone solo operaciones de lectura (`Count`, enumeración), sin métodos de modificación (`Add`, `Remove`). Comunica al caller que el resultado no debe ser modificado. `ToArrayAsync()` retorna un array que implementa esta interfaz.
**En el proyecto:** Los métodos `GetPagedAsync` de los repositorios retornan `IReadOnlyCollection<Contact>`. El back-template usa `ToArrayAsync()` (en lugar de `ToListAsync()`) porque el array es semánticamente de solo lectura y tamaño fijo.

---

### IRepository\<T\> genérico
**Categoría:** Anti-patrones / Repository Pattern
**Definición:** Anti-patrón que consiste en crear una única interfaz genérica de repositorio (`IRepository<T>`) con métodos CRUD comunes para todas las entidades. Parece conveniente pero oculta el contrato real de cada módulo y expone métodos que no todas las entidades necesitan.
**En el proyecto:** Prohibido en el back-template del sistema de mensajería. En su lugar, cada entidad tiene su repositorio específico: `IContactRepository`, `IConversationRepository`, `IUserProfileRepository`, con los métodos exactos que ese dominio necesita.

---

### IRequestHandler\<TRequest, TResponse\>
**Categoría:** Clean Architecture / Mensajería
**Definición:** Interfaz de la librería de mensajería del back-template que implementan los handlers. Define el método `Handle(TRequest request, CancellationToken ct)` que procesa una solicitud y retorna una respuesta.
**En el proyecto:** `GetContactHandler : IRequestHandler<GetContactRequest, GetContactResponse>` y `UpdateContactHandler : IRequestHandler<UpdateContactRequest, UpdateContactResponse>` son ejemplos del sistema de mensajería.

---

### IResponse
**Categoría:** Result Pattern / Common
**Definición:** Interfaz marcadora (sin miembros) de la librería Common que identifica un tipo como respuesta de un caso de uso. El record abstracto base de cada caso de uso (`GetContactResponse`, `UpdateContactResponse`) implementa `IResponse`.
**En el proyecto:** `public abstract record GetContactResponse : IResponse` es el tipo base de las respuestas del caso de uso GetContact. Los subtipos concretos (`GetContactSuccess`, `GetContactNotFoundFailure`) heredan de este record abstracto.

---

### ISuccess / ISuccess\<T\>
**Categoría:** Result Pattern / Common
**Definición:** Interfaces de `Common.Results` que marcan una respuesta como exitosa. `ISuccess<T>` añade la propiedad `T Data` con el payload del resultado. El Presenter usa pattern matching sobre `ISuccess<T>` para extraer el dato y construir la respuesta HTTP 200.
**En el proyecto:** `GetContactSuccess(ContactDto Data) : GetContactResponse, ISuccess<ContactDto>`. El Presenter detecta `ISuccess<ContactDto> success` y llama `_viewModel.Set(success)`, que retorna HTTP 200 con el DTO serializado.

---

### ITenantContextAccessor
**Categoría:** Multi-tenancy / Infraestructura
**Definición:** Interfaz del módulo de multi-tenancy que expone el contexto del tenant del request actual. Su implementación resuelve el tenant desde el JWT, el subdominio o el encabezado HTTP. Se inyecta en `AppDbContext` para que `HasQueryFilter` pueda leer el `TenantId` dinámicamente.
**En el proyecto:** `AppDbContext` recibe `ITenantContextAccessor tenantAccessor` en el constructor. La propiedad `CurrentTenantId` lee `_tenantAccessor.Current?.TenantId` y la usa en el `HasQueryFilter` para filtrar por workspace en el sistema de mensajería.

---

### IValidationFailure
**Categoría:** Result Pattern / Common
**Definición:** Sub-interfaz de `IFailure` que marca una respuesta como un fallo de validación. El Presenter lo mapea a HTTP 422 Unprocessable Entity. Se usa cuando los datos de entrada no cumplen las reglas de validación del dominio.
**En el proyecto:** Se usa en el sistema de mensajería cuando se intenta crear un `Contact` con un nombre vacío o un email con formato inválido. El Presenter detecta `IValidationFailure` y retorna 422 con el mensaje de error.

---

## L

### Lazy Loading
**Categoría:** EF Core / Consultas
**Definición:** Estrategia donde las entidades relacionadas se cargan automáticamente la primera vez que se accede a la propiedad de navegación, ejecutando un query adicional por cada acceso. Es el origen más común del problema N+1 en EF Core.
**En el proyecto:** No se usa Lazy Loading en el back-template del sistema de mensajería. Las relaciones se cargan explícitamente con `Include` (eager loading) o no se cargan cuando no se necesitan.

---

### LINQ (Language Integrated Query)
**Categoría:** C# / EF Core
**Definición:** API de C# que permite escribir consultas sobre colecciones y fuentes de datos con sintaxis de métodos encadenados. EF Core traduce las expresiones LINQ a SQL en tiempo de ejecución. Es la capa de abstracción entre el código C# y el SQL generado.
**En el proyecto:** Los repositorios del sistema de mensajería usan LINQ para construir queries: `.Where(e => e.IsActive)`, `.OrderByDescending(e => e.CreatedAtUtc)`, `.Skip(...).Take(...)`, `.Select(c => new ContactDto(...))`.

---

## M

### MigrateAsync
**Categoría:** EF Core / Migraciones
**Definición:** Método de `DatabaseFacade` que aplica todas las migraciones pendientes a la base de datos al arrancar la aplicación. Conveniente para desarrollo y staging, pero riesgoso en producción con múltiples instancias porque puede haber race conditions.
**En el proyecto:** Se usa en el startup del sistema de mensajería en desarrollo y staging. En producción se usa un script SQL idempotente generado con `dotnet ef migrations script --idempotent` que el pipeline de CI/CD aplica antes del deploy.

---

### Migration (migración)
**Categoría:** EF Core / Base de datos
**Definición:** Archivo C# generado por `dotnet ef migrations add` que describe los cambios de esquema de la base de datos de forma incremental. Cada migración tiene un método `Up` (aplicar cambios) y `Down` (revertir). EF Core registra las migraciones aplicadas en la tabla `__EFMigrationsHistory`.
**En el proyecto:** Cada vez que se agrega o modifica una entidad del sistema de mensajería, se crea una migración con `dotnet ef migrations add <nombre>` apuntando al proyecto de Infrastructure del módulo correspondiente.

---

### ModelBuilder
**Categoría:** EF Core / Fluent API
**Definición:** Clase recibida como parámetro en `OnModelCreating` que permite configurar el modelo completo: esquema por defecto, configuraciones de entidades y filtros globales. Es el punto de entrada del Fluent API.
**En el proyecto:** `AppDbContext.OnModelCreating(ModelBuilder mb)` usa `mb` para establecer el esquema `dbo`, aplicar todas las configuraciones del assembly con `ApplyConfigurationsFromAssembly` y registrar los `HasQueryFilter` de las entidades multi-tenant.

---

### Multi-tenancy
**Categoría:** Arquitectura SaaS
**Definición:** Patrón arquitectónico donde una sola instancia de la aplicación sirve a múltiples clientes (tenants), manteniendo sus datos completamente aislados. En el modelo "pool" (base de datos compartida), el aislamiento se logra filtrando por `TenantId` en cada tabla.
**En el proyecto:** El sistema de mensajería usa el modelo pool: todas las empresas (workspaces) comparten la misma base de datos PostgreSQL. El aislamiento se garantiza mediante `HasQueryFilter` en `AppDbContext` y el parámetro `tenantId` explícito en los repositorios.

---

## N

### N+1 (problema)
**Categoría:** EF Core / Performance
**Definición:** Anti-patrón de acceso a datos donde se ejecuta 1 query para obtener una lista de N entidades y luego N queries adicionales para cargar una relación de cada entidad. La solución es usar `Include` (eager loading) o `AsSplitQuery`.
**En el proyecto:** Cargar conversaciones y luego iterar sobre ellas para acceder a `.Messages` dentro de un bucle genera N+1 queries en el sistema de mensajería. La solución es `.Include(c => c.Messages)` en el query inicial.

---

### Namespace
**Categoría:** C# / Organización de código
**Definición:** Mecanismo de C# para organizar y agrupar tipos relacionados, evitando conflictos de nombres. En el back-template, los namespaces siguen la estructura de carpetas: `Contacts.Domain.Entities`, `Contacts.Infrastructure.Repositories`, `Contacts.Application.UseCases.GetContact`.
**En el proyecto:** `IContactRepository` vive en `Contacts.Domain.Repositories`; `ContactRepository` en `Contacts.Infrastructure.Repositories`. Esta separación de namespaces refuerza el DIP: Domain no depende de Infrastructure.

---

### Npgsql
**Categoría:** PostgreSQL / .NET
**Definición:** Proveedor de EF Core para PostgreSQL. Se configura con `options.UseNpgsql(connectionString)` al registrar el `DbContext`. Traduce las expresiones LINQ y los tipos de EF Core a SQL específico de PostgreSQL, incluyendo tipos propios como `uuid`, `jsonb` y `tsvector`.
**En el proyecto:** Registrado en `Host/Program.cs` del sistema de mensajería: `options.UseNpgsql(connectionString)`. La connection string usa el formato de Npgsql: `Host=localhost;Port=5432;Database=mensajeria;Username=...`.

---

### null (retorno nullable)
**Categoría:** C# / Repository Pattern
**Definición:** Los métodos `GetBy*` de los repositorios del back-template retornan `T?` (nullable) para indicar que el registro puede no existir. El handler comprueba explícitamente si el resultado es `null` y retorna el `*NotFoundFailure` correspondiente. No se usan excepciones para este flujo de control.
**En el proyecto:** `GetByPublicIdAsync` de `ContactRepository` retorna `Contact?`. En `GetContactHandler`, `if (contact is null) return new GetContactNotFoundFailure(...)` es el patrón estándar del sistema de mensajería.

---

## O

### Object Initializer
**Categoría:** C# / Sintaxis
**Definición:** Sintaxis de C# que permite asignar propiedades de un objeto durante su construcción con la notación `new Tipo { Propiedad = valor }`. Con propiedades `init`, es la única forma de asignar valores fuera del constructor. EF Core también usa esta sintaxis internamente para hidratar entidades desde la base de datos.
**En el proyecto:** `InsertAsync` en `ContactRepository` construye el `Contact` con object initializer: `new Contact { PublicId = publicId, TenantId = tenantId, FullName = fullName, ... }`. No hay factory method `Create(...)` en las entidades del back-template.

---

### OnModelCreating
**Categoría:** EF Core / DbContext
**Definición:** Método virtual de `DbContext` que se llama una sola vez al inicializar el modelo de EF Core. Aquí se registran las configuraciones de entidades, los filtros globales y el esquema por defecto. Su resultado se cachea para el ciclo de vida de la aplicación.
**En el proyecto:** `AppDbContext.OnModelCreating` en el sistema de mensajería establece el esquema `dbo`, aplica todas las configuraciones del assembly y registra los `HasQueryFilter` para las entidades multi-tenant.

---

### ORM (Object-Relational Mapper)
**Categoría:** Persistencia / Arquitectura
**Definición:** Herramienta que traduce entre el modelo orientado a objetos (clases C#) y el modelo relacional (tablas SQL). Gestiona el mapeo de propiedades a columnas, las relaciones entre entidades y la generación de SQL.
**En el proyecto:** EF Core es el ORM principal del back-template del sistema de mensajería para CRUD, migraciones y multi-tenancy. Para queries complejos (reportes, FTS) se usa Dapper como complemento.

---

## P

### Paginación por offset (Skip / Take)
**Categoría:** EF Core / Consultas / Performance
**Definición:** Estrategia de paginación que usa `Skip((page - 1) * pageSize).Take(pageSize)` para obtener una página de resultados. Simple de implementar, pero puede ser lenta en páginas muy avanzadas de tablas grandes. Para tablas con millones de filas se prefiere paginación por cursor (keyset pagination).
**En el proyecto:** `GetPagedAsync` de `ContactRepository` usa paginación por offset. Para la bandeja de conversaciones del sistema de mensajería con alto volumen de mensajes se considera migrar a paginación por cursor en módulos posteriores.

---

### Pattern Matching
**Categoría:** C# / Programación funcional
**Definición:** Característica de C# que permite comprobar el tipo y estructura de un objeto y extraer sus valores en una sola expresión. Se usa en los Presenters del back-template para manejar los diferentes tipos de respuesta sin necesidad de casting explícito.
**En el proyecto:** `GetContactPresenter` usa `if (notification is IFailure failure)` y `else if (notification is ISuccess<ContactDto> success)` para determinar cómo mapear la respuesta del handler a la respuesta HTTP correcta.

---

### Presenter
**Categoría:** Clean Architecture / Presentation
**Definición:** Clase de la capa Presentation que implementa `INotificationHandler<TResponse>` y traduce la respuesta del handler en la respuesta HTTP. Usa pattern matching sobre la jerarquía de respuestas para determinar el código HTTP y el cuerpo. Se registra como Scoped en DI.
**En el proyecto:** `GetContactPresenter` recibe `GetContactResponse` del pipeline de mensajería y actualiza `ResultViewModel` con `_viewModel.Fail(failure.Message)` o `_viewModel.Set(success)`. El endpoint del controlador lee el `ResultViewModel` para construir la `IActionResult`.

---

### PublicId
**Categoría:** Diseño de APIs / Seguridad
**Definición:** Identificador público de tipo `Guid` que se expone en las URLs y respuestas de la API. Complementa al `Id` interno de tipo `long`. El `PublicId` no revela información sobre el volumen de datos ni permite enumerar recursos fácilmente.
**En el proyecto:** Todas las entidades del sistema de mensajería tienen `long Id` (PK interna) y `Guid PublicId` (identificador externo). Los endpoints usan `publicId` en la ruta: `GET /api/contacts/{publicId}`. El `Id` nunca aparece en las respuestas.

---

## R

### Race Condition
**Categoría:** Concurrencia / Sistemas distribuidos
**Definición:** Condición de error donde el resultado de una operación depende del orden o timing de múltiples procesos concurrentes. En el contexto de migraciones, dos instancias de la aplicación arrancando simultáneamente podrían intentar aplicar la misma migración, corrompiendo el historial.
**En el proyecto:** Es la razón por la que `Database.MigrateAsync()` al startup no se usa en producción del sistema de mensajería. En su lugar, las migraciones se aplican desde el pipeline de CI/CD antes de iniciar las instancias.

---

### Repository Pattern
**Categoría:** Arquitectura / Design Patterns
**Definición:** Patrón que encapsula la lógica de acceso a datos detrás de una interfaz, separando la lógica de negocio (Application/Domain) de los detalles de persistencia (Infrastructure). Cada entidad tiene su propio repositorio con métodos semánticamente ricos.
**En el proyecto:** `IContactRepository` en `Contacts.Domain` y `ContactRepository` en `Contacts.Infrastructure` son la implementación del patrón en el sistema de mensajería. Si se cambia de PostgreSQL a otro motor, solo cambia la implementación en Infrastructure.

---

### Result Pattern
**Categoría:** Arquitectura / Diseño
**Definición:** Patrón que reemplaza las excepciones para el flujo de control con tipos de retorno explícitos que representan éxito o fallo. En el back-template se implementa con jerarquías de records abstractos e interfaces (`ISuccess`, `IFailure`, `INotFoundFailure`) en lugar de un tipo genérico `Result<T>`.
**En el proyecto:** Cada caso de uso del sistema de mensajería tiene su propia jerarquía: `GetContactResponse` (abstracto), `GetContactSuccess` (éxito), `GetContactNotFoundFailure` (fallo). No hay `Result<T>` genérico compartido entre todos los casos de uso.

---

### ResultViewModel\<TController\>
**Categoría:** Clean Architecture / Presentation
**Definición:** Clase de la librería Common que actúa como contenedor mutable compartido entre el Presenter y el Controller para pasar el resultado de la operación. El Presenter escribe en él y el Controller lo lee para construir la `IActionResult`.
**En el proyecto:** `ResultViewModel<ContactsController>` es inyectado en el Presenter y en el Controller del módulo Contacts del sistema de mensajería con ciclo de vida Scoped, garantizando que ambos compartan la misma instancia dentro del request.

---

### RollbackAsync
**Categoría:** EF Core / Transacciones
**Definición:** Método de `IDbContextTransaction` que revierte todos los cambios realizados dentro de la transacción. Se llama en el bloque `catch` cuando alguna operación falla dentro de la transacción, garantizando que la base de datos quede en el estado anterior.
**En el proyecto:** En el handler de creación de conversaciones del sistema de mensajería, si `_channelService.NotifyAsync` lanza una excepción, el bloque `catch` llama `await tx.RollbackAsync(ct)` para deshacer el insert de la conversación.

---

### Roundtrip (base de datos)
**Categoría:** Performance
**Definición:** Ciclo completo de enviar una query a la base de datos y recibir su respuesta. Cada roundtrip implica latencia de red, overhead de parsing y ejecución. Minimizar roundtrips es una de las principales estrategias de performance en acceso a datos.
**En el proyecto:** `ExecuteUpdateAsync` reduce a un roundtrip lo que el patrón clásico (cargar + modificar + guardar) hace en dos. La elección de `Include` vs lazy loading puede significar la diferencia entre 1 roundtrip y N+1 roundtrips en el sistema de mensajería.

---

## S

### SaveChangesAsync
**Categoría:** EF Core / Unit of Work
**Definición:** Método del `DbContext` que persiste en la base de datos todos los cambios acumulados en el Change Tracker desde la última llamada. Ejecuta todos los `INSERT`, `UPDATE` y `DELETE` pendientes dentro de una sola transacción. Es la implementación del patrón Unit of Work en EF Core.
**En el proyecto:** Llamado al final de `InsertAsync` en `ContactRepository`. Si dos repositorios del sistema de mensajería comparten el mismo `AppDbContext` Scoped en el mismo request, los cambios de ambos pueden persistirse en la misma transacción con un único `SaveChangesAsync`.

---

### Scoped (ciclo de vida DI)
**Categoría:** .NET / Inyección de Dependencias
**Definición:** Ciclo de vida de DI donde se crea una nueva instancia del servicio por cada request HTTP y esa instancia se reutiliza durante todo el request. Es el ciclo de vida correcto para `AppDbContext` y para los repositorios, garantizando que todos los repositorios de un request compartan el mismo `DbContext`.
**En el proyecto:** `AppDbContext` se registra como Scoped con `AddDbContext`. `IContactRepository` se registra como Scoped en `ServiceCollectionEx`. Esto garantiza que en un handler con dos repositorios, ambos compartan el mismo `AppDbContext` y sus cambios se puedan persistir en la misma transacción.

---

### Select (proyección LINQ)
**Categoría:** EF Core / LINQ / Performance
**Definición:** Operador LINQ que proyecta cada elemento de una colección a un nuevo tipo. Cuando se usa antes de la materialización del query (antes de `ToListAsync`), EF Core genera un `SELECT` con solo las columnas necesarias en lugar de traer toda la fila.
**En el proyecto:** `_db.Contacts.AsNoTracking().Where(c => c.IsActive).Select(c => new ContactDto(c.PublicId, c.FullName, c.Email)).ToListAsync(ct)` genera `SELECT public_id, full_name, email FROM contacts WHERE is_active = true`, omitiendo columnas innecesarias.

---

### Serilog (logging estructurado)
**Categoría:** Logging / Observabilidad
**Definición:** Librería de logging estructurado para .NET que registra propiedades individuales del log como campos indexables. La regla crítica es usar propiedades estructuradas, no interpolación de strings, para preservar la estructura del log.
**En el proyecto:** El back-template usa Serilog configurado en `Host/Program.cs` del sistema de mensajería. Se debe escribir `Log.Information("Contacto creado {ContactId}", contact.PublicId)` en lugar de `Log.Information($"Contacto creado {contact.PublicId}")`, que destruye la estructura.

---

### ServiceCollectionEx
**Categoría:** DI / Modularidad
**Definición:** Clase estática de extensión de `IServiceCollection` que cada módulo expone para registrar sus propios servicios en el contenedor de DI. Permite que `Host/Program.cs` registre módulos completos con una sola llamada, manteniendo el archivo de startup limpio.
**En el proyecto:** `Contacts.Infrastructure/ServiceCollectionEx.cs` expone `AddContactsInfrastructureServices(this IServiceCollection services)` que registra `IContactRepository -> ContactRepository` como Scoped. `Program.cs` llama a todos los `Add*InfrastructureServices` de cada módulo.

---

### SetProperty
**Categoría:** EF Core / ExecuteUpdateAsync
**Definición:** Método del fluent builder dentro de `ExecuteUpdateAsync` que especifica qué columna actualizar y con qué valor. Se encadena múltiples veces para actualizar varias columnas en un solo `UPDATE`. Recibe dos lambdas: una que selecciona la propiedad y otra que proporciona el nuevo valor.
**En el proyecto:** `s.SetProperty(e => e.FullName, fullName).SetProperty(e => e.UpdatedAtUtc, DateTime.UtcNow)` en `UpdateAsync` de `ContactRepository` genera `UPDATE Contacts SET full_name = @p0, updated_at_utc = @p1 WHERE public_id = @p2 AND is_active = true`.

---

### Singleton (ciclo de vida DI)
**Categoría:** .NET / Inyección de Dependencias
**Definición:** Ciclo de vida de DI donde se crea una única instancia del servicio para toda la vida de la aplicación. Los servicios Singleton no pueden depender de servicios Scoped (captive dependency), porque el Singleton duraría más que la scope, causando bugs de estado compartido entre requests.
**En el proyecto:** Registrar `AppDbContext` como Singleton sería un bug crítico de tenant leakage: el mismo contexto (y el mismo `TenantId`) se compartiría entre todos los requests de todos los tenants del sistema de mensajería.

---

### Skip / Take
**Categoría:** LINQ / EF Core / Paginación
**Definición:** Operadores LINQ para paginación por offset. `Skip(n)` descarta los primeros `n` elementos; `Take(m)` toma los siguientes `m`. En combinación generan: `Skip((page-1) * pageSize).Take(pageSize)` en C# -> `OFFSET (page-1)*pageSize LIMIT pageSize` en SQL.
**En el proyecto:** Usados en `GetPagedAsync` de `ContactRepository` para devolver una página específica de contactos del sistema de mensajería ordenados por fecha de creación descendente.

---

### Soft Delete
**Categoría:** Diseño de datos / Patrones
**Definición:** Técnica de eliminación lógica donde los registros no se borran físicamente de la base de datos, sino que se marcan como inactivos (con `IsActive = false` o `DeletedAt`). Preserva el historial de datos y permite recuperación. Las queries normales excluyen los registros inactivos.
**En el proyecto:** `DisableAsync` de `ContactRepository` implementa soft delete con `ExecuteUpdateAsync` seteando `IsActive = false`. El `HasQueryFilter` filtra automáticamente los contactos inactivos del sistema de mensajería en todos los queries normales.

---

## T

### Tenant
**Categoría:** SaaS / Multi-tenancy
**Definición:** Unidad de aislamiento en una aplicación multi-tenant. Un tenant es un cliente (empresa/workspace) que usa la aplicación. Todos sus datos (agentes, contactos, conversaciones) están aislados de los datos de otros tenants mediante el `TenantId`.
**En el proyecto:** Entidad `Tenant` en `Tenancy.Domain.Entities` del sistema de mensajería. Cada workspace de empresa es un tenant. El `TenantId` presente en todas las entidades de negocio garantiza que los datos de un workspace no sean visibles para otro.

---

### Tenant Leakage
**Categoría:** Multi-tenancy / Seguridad
**Definición:** Bug crítico de seguridad donde datos de un tenant son accesibles por otro. Puede ocurrir si se olvida agregar el filtro de tenant en una query, si se reutiliza un `AppDbContext` entre requests de diferentes tenants (Captive Dependency con Singleton), o si el `HasQueryFilter` no cubre todas las entidades sensibles.
**En el proyecto:** Se previene en el sistema de mensajería con: (1) `HasQueryFilter` automático en `AppDbContext`, (2) parámetro `tenantId` explícito en las interfaces de repositorio como documentación del contrato, (3) prohibición de registrar `AppDbContext` como Singleton.

---

### TenantId
**Categoría:** Multi-tenancy / Base de datos
**Definición:** Identificador de tipo `long` que se almacena en cada tabla que requiere aislamiento multi-tenant. Es la columna clave que permite que `HasQueryFilter` separe los datos de cada cliente en el modelo pool de base de datos compartida.
**En el proyecto:** Todas las entidades principales del sistema de mensajería (`Contact`, `Conversation`, `Message`, `UserProfile`) tienen una columna `TenantId` de tipo `long`. El `HasQueryFilter` en `AppDbContext` usa este valor para aislar los datos por workspace.

---

### ThenInclude
**Categoría:** EF Core / Consultas / Eager Loading
**Definición:** Método que se encadena después de `Include` para cargar relaciones de segundo o mayor nivel. Permite cargar una relación de la entidad relacionada (por ejemplo, los adjuntos de los mensajes de una conversación).
**En el proyecto:** `_db.Conversations.Include(c => c.Messages).ThenInclude(m => m.Attachments)` carga en un solo query las conversaciones con sus mensajes y los adjuntos de cada mensaje en el sistema de mensajería.

---

### ToArrayAsync
**Categoría:** EF Core / LINQ / Materialización
**Definición:** Método de materialización que ejecuta el query y retorna los resultados como un array (`T[]`). El back-template lo prefiere sobre `ToListAsync` porque `T[]` implementa `IReadOnlyCollection<T>`, comunicando que el resultado no debe modificarse.
**En el proyecto:** `GetPagedAsync` en `ContactRepository` usa `ToArrayAsync(ct)` para retornar `IReadOnlyCollection<Contact>`. Los callers del repositorio no pueden agregar ni remover elementos del array resultante.

---

### ToListAsync
**Categoría:** EF Core / LINQ / Materialización
**Definición:** Método de materialización de LINQ que ejecuta el query y retorna los resultados como `List<T>`. Es el método de materialización más común en EF Core, pero `List<T>` implica mutabilidad. El back-template lo sustituye por `ToArrayAsync` en repositorios que retornan `IReadOnlyCollection<T>`.
**En el proyecto:** Aparece en los ejemplos didácticos de las slides del sistema de mensajería, pero la implementación real de repositorios usa `ToArrayAsync` para comunicar la intención de solo lectura.

---

### ToTable
**Categoría:** EF Core / Fluent API
**Definición:** Método de `EntityTypeBuilder` que configura explícitamente el nombre de la tabla en la base de datos. Sin esta configuración, EF Core usa el nombre del `DbSet` como nombre de tabla (convención). Se recomienda en proyectos productivos para hacer el mapeo evidente.
**En el proyecto:** `ContactConfiguration` usa `builder.ToTable("Contacts")` para ser explícito, incluso cuando coincide con la convención, siguiendo el principio de configuración explícita sobre convención del back-template del sistema de mensajería.

---

### Transacción (Database Transaction)
**Categoría:** Base de datos / EF Core
**Definición:** Unidad de trabajo que agrupa una o más operaciones de base de datos garantizando las propiedades ACID (Atomicidad, Consistencia, Aislamiento, Durabilidad). EF Core implementa transacciones implícitamente en cada `SaveChangesAsync` y permite transacciones explícitas con `BeginTransactionAsync`.
**En el proyecto:** `SaveChangesAsync` del sistema de mensajería envuelve todos los cambios acumulados en el Change Tracker en una transacción implícita. Las operaciones que involucran servicios externos requieren transacciones explícitas con `BeginTransactionAsync`.

---

### Transient (ciclo de vida DI)
**Categoría:** .NET / Inyección de Dependencias
**Definición:** Ciclo de vida de DI donde se crea una nueva instancia del servicio cada vez que se solicita al contenedor, sin importar si es en el mismo request o scope. Es el ciclo de vida más corto. Apropiado para servicios ligeros sin estado que no comparten recursos.
**En el proyecto:** No se usa para repositorios ni para `AppDbContext` en el sistema de mensajería. Registrar un repositorio como Transient significaría que dos inyecciones del mismo repositorio en el mismo handler tendrían instancias distintas, rompiendo la Unit of Work implícita.

---

## U

### Unit of Work (UoW)
**Categoría:** Arquitectura / Design Patterns
**Definición:** Patrón que mantiene una lista de cambios realizados durante una transacción de negocio y coordina su escritura a la base de datos. EF Core implementa UoW implícitamente en `DbContext`. Para operaciones con servicios externos se usa transacción explícita con `BeginTransactionAsync`.
**En el proyecto:** UoW implícito en el sistema de mensajería: `SaveChangesAsync` al final de `InsertAsync`. UoW explícito con `BeginTransactionAsync / CommitAsync / RollbackAsync` cuando el handler coordina escrituras en DB con llamadas externas (notificación de canal).

---

### UpdateAsync
**Categoría:** Repository Pattern
**Definición:** Método de los repositorios del back-template que actualiza campos específicos de un registro existente con `ExecuteUpdateAsync`. Recibe parámetros escalares y retorna `Task<bool>` indicando si se encontró y actualizó el registro.
**En el proyecto:** `ContactRepository.UpdateAsync` actualiza `FullName` y `UpdatedAtUtc` del `Contact` identificado por `PublicId` en el sistema de mensajería. Al usar `ExecuteUpdateAsync`, no requiere cargar la entidad en memoria ni modificar las propiedades `init`.

---

### UseNpgsql
**Categoría:** EF Core / PostgreSQL / Configuración
**Definición:** Método de extensión del proveedor Npgsql que configura EF Core para usar PostgreSQL como base de datos. Se llama en `AddDbContext` pasando la connection string. Habilita funcionalidades específicas de PostgreSQL como `gen_random_uuid()`, tipos `jsonb` y full text search.
**En el proyecto:** `options.UseNpgsql(connectionString)` en `Host/Program.cs` configura `AppDbContext` para conectarse a la base de datos PostgreSQL del sistema de mensajería, ya sea local en desarrollo o en RDS en producción.

---

## V

### Vertical Slice Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que organiza el código por funcionalidad (feature) en lugar de por capa técnica. Cada feature tiene todos sus componentes juntos (handler, request, response, repositorio) en lugar de tener todos los handlers en una capa y todos los repositorios en otra.
**En el proyecto:** El back-template del sistema de mensajería usa Vertical Slice dentro de cada módulo: `Contacts.Application/UseCases/GetContact/` contiene el handler, el request y las responses del caso de uso GetContact, todos juntos.

---

## W

### Where (LINQ)
**Categoría:** EF Core / LINQ
**Definición:** Operador LINQ que filtra elementos de una colección según un predicado. En EF Core se traduce a una cláusula `WHERE` en SQL. Se puede encadenar múltiples veces; EF Core los combina en una sola cláusula `WHERE` con `AND`.
**En el proyecto:** `.Where(c => c.IsActive)` en `GetPagedAsync` filtra contactos activos. `.Where(e => e.PublicId == publicId && e.IsActive)` en `GetByPublicIdAsync` combina dos condiciones. El `HasQueryFilter` añade automáticamente `AND tenant_id = @tenantId`.

---

*Rogelio Arriaga  -  Raptor Dev Services  -  Mayo 2026*
