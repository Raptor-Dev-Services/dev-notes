# Glosario Técnico - S02
## DRY - KISS - YAGNI - ADRs - Principios de Diseño

> Definiciones de todos los términos técnicos usados en la sesión.
> Ordenado alfabéticamente.

---

## A

### Abstracción
**Categoría:** Principio de diseño / POO
**Definición:** Mecanismo que oculta los detalles de implementación y expone solo lo esencial para el consumidor. En código, una abstracción puede ser una interfaz, una clase base, un método de extensión o un Value Object. Una buena abstracción reduce la carga cognitiva del lector: solo necesita saber "qué hace", no "cómo lo hace".
**En el proyecto:** `Username.Create()` es una abstracción: el Handler no sabe nada sobre las reglas de validación internas - solo sabe que recibe un `Result<Username>`. Del mismo modo, los repositorios del sistema de mensajería abstraen el acceso a PostgreSQL detrás de métodos como `InsertAsync` o `GetByIdAsync`.

---

### Abstracción prematura
**Categoría:** Code smell / Anti-patrón
**Definición:** Crear interfaces, clases base, factories o jerarquías de clases antes de que exista evidencia concreta de que se necesitan. Viola YAGNI y añade complejidad sin beneficio inmediato. El costo es doble: se escribe código que nadie pidió y ese código luego dificulta el refactoring porque hay que mantener la abstracción junto con la lógica real.
**En el proyecto:** Crear `IConversationAssignmentStrategy` en S02 cuando la única regla de asignación es "round-robin" sería abstracción prematura. El momento correcto de extraerla es en S05, cuando el requisito de múltiples estrategias intercambiables esté confirmado.

---

### ADR (Architecture Decision Record)
**Categoría:** Arquitectura / Documentación
**Definición:** Documento corto (1-2 páginas) que registra una decisión de arquitectura tomada por el equipo: el contexto que forzó la decisión, la decisión en sí y las consecuencias positivas y negativas. Los ADRs son inmutables - no se editan; cuando una decisión cambia, se crea un ADR nuevo que supersede al anterior. Viven junto al código en el repositorio.
**En el proyecto:** El repo `mensajeria/docs/adr/` aloja `ADR-001-monolito-modular.md`, `ADR-002-postgresql.md` y `ADR-003-ef-core.md`. Cada vez que el equipo tome una decisión relevante (por qué Redis para caché, por qué GitHub Actions y no Azure DevOps), se crea un ADR nuevo.

---

### ADR-001 - Monolito Modular
**Categoría:** Arquitectura / Decisión documentada
**Definición:** El primer ADR del proyecto de mensajería, que justifica la elección de monolito modular con Clean Architecture sobre microservicios. Sus argumentos: equipo de 6 personas, dominio aún no completamente definido, sin DevOps maduro. Establece que los módulos se extraerán a microservicios cuando el dominio lo justifique y el equipo tenga CI/CD maduro.
**En el proyecto:** Define la arquitectura raíz de todo el sistema. Mientras el ADR esté en status `Accepted`, nadie debería proponer microservicios sin primero actualizarlo con un ADR que lo superceda.

---

### ADR-002 - PostgreSQL
**Categoría:** Arquitectura / Decisión documentada
**Definición:** El segundo ADR del proyecto, que justifica la elección de PostgreSQL 16 como base de datos principal. Los argumentos decisivos: FTS nativo con índices GIN (evita Elasticsearch en v1), JSONB para flujos del bot y RLS como capa adicional de aislamiento multi-tenant.
**En el proyecto:** Toda la capa de datos del sistema de mensajería usa PostgreSQL. Los flujos del bot se almacenan como JSONB. La búsqueda de conversaciones y contactos usa FTS nativo.

---

### ADR-003 - EF Core
**Categoría:** Arquitectura / Decisión documentada
**Definición:** El tercer ADR del proyecto, que documenta la evaluación de EF Core 10 vs. Dapper y la elección de EF Core. El argumento decisivo: el global query filter de EF Core aplica `TenantId` en todas las queries automáticamente, convirtiendo el aislamiento multi-tenant en un problema de configuración en lugar de disciplina de cada desarrollador.
**En el proyecto:** Toda la capa de persistencia usa EF Core 10 con Npgsql. Las `EntityTypeConfiguration` configuran el comportamiento por entidad. `IgnoreQueryFilters()` solo se usa en contextos de autenticación donde no hay `TenantId`.

---

### Agile
**Categoría:** Metodología / Proceso
**Definición:** Marco filosófico de desarrollo de software que prioriza la entrega iterativa de valor, la colaboración con el cliente, la respuesta al cambio sobre seguir un plan rígido, y el software funcionando sobre documentación exhaustiva. Agile no es una metodología en sí: es el conjunto de valores del Manifiesto Ágil (2001). Scrum es una de las implementaciones concretas de Agile.
**En el proyecto:** El equipo del sistema de mensajería trabaja con sprints de 2 semanas, Daily Standups y retrospectivas - todos elementos de Scrum, que es una implementación de Agile.

---

### Application (capa)
**Categoría:** Clean Architecture / Estructura de proyecto
**Definición:** Capa de la Clean Architecture que contiene los casos de uso de la aplicación: Handlers, Commands, Queries, Responses y Presenters. No contiene lógica de dominio ni de infraestructura. Depende de abstracciones definidas en Domain, no de implementaciones concretas.
**En el proyecto:** En el back-template, la carpeta `Application/UseCases/` contiene subcarpetas por feature (`Users/Register/`, `Conversations/Create/`, etc.). Cada Handler orquesta la validación, el repositorio y la respuesta.

---

### AsNoTracking()
**Categoría:** EF Core / Performance
**Definición:** Método de EF Core que le indica al contexto que no rastree los cambios de las entidades devueltas por una query. Reduce el consumo de memoria y mejora el rendimiento en operaciones de solo lectura, ya que EF Core no almacena snapshots de los objetos para detectar cambios al llamar `SaveChangesAsync()`.
**En el proyecto:** Todas las queries de listado o lectura del sistema de mensajería (listar conversaciones, buscar contactos, obtener el historial de mensajes) deben usar `.AsNoTracking()`. Solo las queries que modifican datos omiten este flag.

---

### AWS RDS
**Categoría:** Cloud / Base de datos
**Definición:** Amazon Relational Database Service - servicio administrado de bases de datos relacionales en AWS. Permite correr PostgreSQL (entre otros motores) sin gestionar el servidor subyacente: AWS maneja backups, parches, failover y escalado de almacenamiento.
**En el proyecto:** El ADR-002 establece que PostgreSQL corre en AWS RDS en el ambiente de producción. En local y staging, corre en un contenedor Docker.

---

## B

### Backlog
**Categoría:** Scrum / Agile
**Definición:** Lista priorizada de todo el trabajo pendiente del producto, gestionada por el Product Owner. Contiene historias de usuario, bugs, tareas técnicas y deuda técnica. Los ítems al tope del backlog están refinados y listos para entrar al próximo sprint; los del fondo son menos detallados.
**En el proyecto:** El backlog del sistema de mensajería incluye features de Auth, Workspace, Canales, Conversaciones, Contactos, etc. La deuda técnica también debe registrarse aquí como ítems priorizables, no solo como TODOs en el código.

---

### Boy Scout Rule
**Categoría:** Principio de desarrollo / Refactoring
**Definición:** Principio atribuido a Robert C. Martin que dice: "Deja el código más limpio de como lo encontraste." La analogía es con los Boy Scouts, que dejan el campamento más limpio de lo que lo encontraron. Implica hacer pequeñas mejoras en cada PR - renombrar una variable confusa, extraer un método largo, agregar un comentario explicativo - sin necesidad de sprints dedicados a refactoring.
**En el proyecto:** Si al implementar la búsqueda de contactos un desarrollador encuentra que `GetAllAsync` en el repositorio no usa `AsNoTracking()`, lo corrige en el mismo PR. No espera a un "sprint de refactoring" que nunca llega.

---

### Build de CI
**Categoría:** CI/CD / DevOps
**Definición:** Ejecución automatizada del pipeline de Integración Continua cuando se abre o actualiza un Pull Request. Típicamente incluye: compilación del proyecto, ejecución de tests unitarios e integración, y análisis estático de código. Un build verde (exitoso) es precondición para hacer merge.
**En el proyecto:** La DoD del sistema de mensajería exige "Build de CI verde" antes de merge. El pipeline corre en GitHub Actions y valida que el código compila y los tests pasan en cada PR.

---

## C

### Change Tracking (EF Core)
**Categoría:** EF Core / ORM
**Definición:** Mecanismo de EF Core que almacena un snapshot del estado de cada entidad cargada en memoria para detectar qué cambios se deben persistir al llamar `SaveChangesAsync()`. Es útil para operaciones de escritura, pero consume memoria innecesariamente en operaciones de solo lectura.
**En el proyecto:** El back-template usa `AsNoTracking()` en todas las queries de lectura. Para actualizaciones sin cargar la entidad completa, usa `ExecuteUpdateAsync()` - más eficiente que cargar, modificar y guardar.

---

### CI/CD (Continuous Integration / Continuous Delivery)
**Categoría:** DevOps / Proceso
**Definición:** CI (Integración Continua) es la práctica de integrar cambios de código frecuentemente en una rama compartida, con validación automática en cada integración. CD (Entrega/Despliegue Continuo) extiende CI para que cada integración exitosa pueda (o sea) desplegada automáticamente a un ambiente.
**En el proyecto:** Se implementa más adelante con GitHub Actions. El pipeline incluye build, tests, análisis de seguridad y despliegue a staging en AWS.

---

### Clean Architecture
**Categoría:** Arquitectura de software
**Definición:** Estilo arquitectónico propuesto por Robert C. Martin que organiza el código en capas concéntricas (Domain, Application, Infrastructure, Presentation), donde las dependencias solo apuntan hacia adentro. El Domain no conoce nada de EF Core, HTTP o PostgreSQL. La infraestructura implementa abstracciones definidas en capas internas.
**En el proyecto:** El back-template del sistema de mensajería sigue Clean Architecture con las carpetas `Domain/`, `Application/`, `Infrastructure/`, `Presentation/` y `Host/`. El ADR-001 la establece como la arquitectura base del monolito modular.

---

### Code Review
**Categoría:** Proceso de desarrollo / Calidad
**Definición:** Revisión del código de un Pull Request por uno o más compañeros antes de hacer merge. Objetivos: detectar bugs, verificar que se cumplen los criterios de aceptación, asegurar que el código sigue los estándares del proyecto y transferir conocimiento entre miembros del equipo.

---

### Code Smell
**Categoría:** Calidad de código / Refactoring
**Definición:** Indicador en el código que sugiere un problema de diseño subyacente, sin ser necesariamente un bug. Los code smells no rompen el programa, pero dificultan su mantenimiento. Ejemplos: método demasiado largo (Long Method), clase que hace demasiado (God Object), código duplicado (Duplicated Code), lógica especulativa (Speculative Generality).
**En el proyecto:** El homework de S01 pedía identificar un code smell con su nombre y propuesta de fix. En el kata de S02, se identifican y eliminan code smells concretos: lógica de validación duplicada en múltiples Handlers, abstracciones innecesarias, etc.

---

### Complejidad Ciclomática
**Categoría:** Métricas de código / Calidad
**Definición:** Métrica que cuenta el número de caminos independientes de ejecución a través de un método. Cada `if`, `else if`, `case`, `while`, `for`, `catch` o `&&`/`||` en una condición suma 1 al contador. A mayor complejidad ciclomática, más difícil de entender y testear el método. El umbral recomendado es ≤10; por encima de 15 es señal de alerta para dividir el método.
**En el proyecto:** Los Handlers del sistema de mensajería deben tener complejidad ciclomática baja. Si `AssignConversationHandler` crece con múltiples ramas de lógica, es señal de extraer métodos privados o de aplicar el patrón Strategy (en S05).

---

### Connection String
**Categoría:** Base de datos / Configuración
**Definición:** Cadena de texto que contiene todos los parámetros necesarios para conectarse a una base de datos: host, puerto, nombre de base de datos, usuario y contraseña. En .NET, se obtiene de `IConfiguration` usando `GetConnectionString("Default")` y se almacena en `appsettings.json` o variables de entorno - nunca hardcodeada en el código fuente.
**En el proyecto:** El back-template lee la connection string de `IConfiguration`. En desarrollo local, viene de `appsettings.Development.json` o del archivo `.env` de Docker Compose. En producción, de AWS Secrets Manager o variables de entorno del contenedor.

---

### Contexto (ADR)
**Categoría:** Arquitectura / Documentación
**Definición:** Primera sección del formato MADR. Describe la situación, restricción o necesidad que forzó la decisión: el tamaño del equipo, el estado del dominio, las restricciones de tiempo o presupuesto, los requisitos técnicos específicos. Sin contexto, la decisión parece arbitraria.
**En el proyecto:** El contexto de ADR-001 explica por qué monolito: "equipo de 6 desarrolladores, dominio no completamente definido, sin DevOps maduro". Este contexto es lo que hace que la decisión sea comprensible para alguien que se incorpora meses después.

---

### Consecuencias (ADR)
**Categoría:** Arquitectura / Documentación
**Definición:** Tercera sección del formato MADR. Enumera los beneficios concretos (Sí: ) y los costos o limitaciones aceptados (No: ) de la decisión. No todas las decisiones son perfectas - un buen ADR reconoce honestamente los trade-offs en lugar de justificar la decisión como si fuera obviamente superior.
**En el proyecto:** El ADR-003 reconoce que el SQL generado por EF Core es menos predecible en queries complejas - y establece la mitigación: usar `FromSqlRaw` con parámetros para esos casos, nunca interpolación de strings.

---

### CTE (Common Table Expression)
**Categoría:** SQL / Base de datos
**Definición:** Subconsulta nombrada en SQL que se define con `WITH nombre AS (...)` y se puede referenciar en la consulta principal como si fuera una tabla temporal. Las CTEs recursivas permiten recorrer jerarquías (árboles de categorías, threads de conversación). Especialmente útiles en queries complejas que Dapper ejecutaría directamente pero EF Core manejaría con `FromSqlRaw`.
**En el proyecto:** El ADR-003 menciona CTEs recursivas como un escenario donde Dapper tiene ventaja sobre EF Core. Para el sistema de mensajería, los threads de conversación con respuestas anidadas podrían requerir CTEs recursivas en la fase de performance.

---

### Cursor Pagination
**Categoría:** API Design / Performance
**Definición:** Técnica de paginación donde en lugar de un número de página (`page=3`) se usa un cursor opaco que apunta al último ítem visto. El servidor devuelve los N ítems siguientes a partir del cursor. Es más eficiente que la paginación offset en tablas grandes porque no requiere `OFFSET` en SQL (que es costoso), y es más estable cuando se insertan o eliminan registros entre páginas.
**En el proyecto:** La bandeja de entrada del sistema de mensajería usa cursor-based pagination para el listado de conversaciones. El cursor típicamente encapsula el `Id` y el `CreatedAt` del último ítem devuelto.

---

## D

### Daily Standup
**Categoría:** Scrum / Agile
**Definición:** Reunión diaria de 15 minutos máximo donde cada miembro del Dev Team responde tres preguntas: ¿qué hice ayer?, ¿qué haré hoy?, ¿hay algún impedimento? No es una reunión de status para el Scrum Master - es sincronización entre pares del equipo. Si surgen conversaciones profundas, se agenda una reunión separada.
**En el proyecto:** El equipo del sistema de mensajería tiene Daily Standup cada día del sprint. Los impedimentos identificados en el standup son responsabilidad del Scrum Master removerlos antes del día siguiente.

---

### Dapper
**Categoría:** ORM / Acceso a datos
**Definición:** Micro-ORM de .NET que extiende `IDbConnection` con métodos como `QueryAsync<T>()` y `ExecuteAsync()`. Permite escribir SQL explícito y mapear los resultados a objetos fuertemente tipados. Ofrece control total sobre el SQL generado y mínimo overhead, pero requiere que el desarrollador gestione manualmente aspectos como el filtrado por `TenantId`.
**En el proyecto:** El ADR-003 evaluó Dapper y lo descartó como ORM principal por el riesgo de fuga de datos multi-tenant (el `TenantId` debe pasarse manualmente en cada query). Se reserva como opción para queries muy complejas donde EF Core genera SQL subóptimo.

---

### DatabaseInitializationService
**Categoría:** EF Core / Infraestructura
**Definición:** Servicio del back-template que corre al arrancar la aplicación y llama a `EnsureCreatedAsync()` para crear el esquema de la base de datos si no existe. Es un patrón simple para desarrollo y staging; en producción se reemplaza por migraciones explícitas.
**En el proyecto:** El back-template incluye `DatabaseInitializationService` con `EnsureCreatedAsync()`. Las `EntityTypeConfiguration` por entidad definen el esquema (nombres de tabla, columnas, índices, constraints) de forma centralizada.

---

### Deciders (ADR)
**Categoría:** Arquitectura / Documentación
**Definición:** Campo del formato MADR que registra qué personas o roles tomaron la decisión. Es importante para entender el contexto organizacional de la decisión y para saber a quién consultar si surgen dudas más adelante.

---

### Decisión (ADR)
**Categoría:** Arquitectura / Documentación
**Definición:** Segunda sección del formato MADR. Establece claramente qué se eligió y por qué, incluyendo la razón decisiva sobre las alternativas evaluadas. Debe ser directa y sin ambigüedad: "Elegimos X porque Y" - no "podríamos considerar X o Y dependiendo de Z".
**En el proyecto:** La decisión de ADR-003 es explícita: "EF Core 10 con Npgsql 10.0.1. EntityTypeConfigurations por entidad. DatabaseInitializationService con EnsureCreatedAsync() en arranque." Cualquiera que lea el ADR sabe exactamente qué se implementa.

---

### Deuda Técnica
**Categoría:** Gestión técnica / Calidad
**Definición:** Trabajo técnico pendiente que el equipo acepta conscientemente (o acumula por descuido) a cambio de entregar más rápido hoy, y que deberá pagarse con intereses más adelante. La metáfora financiera es de Ward Cunningham: igual que la deuda financiera genera intereses, la deuda técnica genera fricción que ralentiza el desarrollo futuro.
**En el proyecto:** DRY, KISS y YAGNI previenen tres tipos de deuda: duplicación (cambiar una regla en N lugares), complejidad innecesaria (entender código sobre-abstracto) y funcionalidad no pedida (mantener código que nadie usa). Los TODOs en el código deben incluir `// TODO: [razón] — pagar en S0X`.

---

### Deuda Técnica Accidental
**Categoría:** Gestión técnica / Calidad
**Definición:** Deuda técnica que surge por falta de conocimiento, descuido o presión de tiempo no planificada. No fue una decisión consciente - es el resultado de no saber mejor en el momento. Ejemplo: duplicar lógica de validación en tres Handlers porque el desarrollador no conocía el patrón Value Object.

---

### Deuda Técnica Deliberada
**Categoría:** Gestión técnica / Calidad
**Definición:** Deuda técnica aceptada conscientemente como decisión de gestión: "Entregamos ahora con este atajo y lo refactorizamos en el sprint X." La clave es que sea explícita, documentada y tenga una fecha de pago. La deuda deliberada prudente es una herramienta legítima; la deliberada imprudente ("no hay tiempo para diseñar bien") es simplemente negligencia.
**En el proyecto:** En S02 se puede tomar deuda deliberada prudente: "Hardcodeamos el timeout del HttpClient ahora - lo configuramos como opción más adelante cuando veamos el Options Pattern." Esto se documenta con un TODO y un ítem en el backlog.

---

### DI (Dependency Injection)
**Categoría:** Diseño de software / .NET
**Definición:** Patrón donde las dependencias de una clase se le inyectan desde afuera (típicamente por el constructor) en lugar de que la clase las cree internamente. Esto invierte el control, facilita el testing (se pueden inyectar mocks) y desacopla las clases de sus implementaciones concretas. En .NET, el contenedor de DI de `Microsoft.Extensions.DependencyInjection` gestiona el ciclo de vida de las dependencias.
**En el proyecto:** Todos los Handlers, Repositories y Services del sistema de mensajería reciben sus dependencias por constructor. El registro se hace en `Host/` o en módulos de extensión.

---

### DIP (Dependency Inversion Principle)
**Categoría:** SOLID / Principios de diseño
**Definición:** El quinto principio SOLID. Establece que: 1) los módulos de alto nivel no deben depender de los de bajo nivel - ambos deben depender de abstracciones; 2) las abstracciones no deben depender de los detalles - los detalles deben depender de las abstracciones. En la práctica: los Handlers de Application dependen de interfaces de repositorio, no de clases concretas de EF Core.
**En el proyecto:** El homework de S01 pedía identificar la cadena "interfaz -> implementación -> consumidor -> DI root". En el back-template, `ICredentialRepository` (en Application) es la abstracción; `CredentialRepository` (en Infrastructure) es la implementación; el Handler es el consumidor; el DI root registra la implementación concreta.

---

### Docker
**Categoría:** Contenedores / DevOps
**Definición:** Plataforma de contenedores que empaqueta aplicaciones y sus dependencias en imágenes portables. Un contenedor de Docker corre de forma aislada del sistema operativo host pero comparte el kernel. Docker Compose permite orquestar múltiples contenedores (app + base de datos + cache) con un solo comando.
**En el proyecto:** En desarrollo local, PostgreSQL corre en Docker. En una sesión posterior se crea el Dockerfile multi-stage para el back-end y front-end. En una sesión posterior se automatiza el despliegue de contenedores a AWS.

---

### Docker Compose
**Categoría:** Contenedores / DevOps
**Definición:** Herramienta que permite definir y correr aplicaciones multi-contenedor con un archivo `docker-compose.yml`. Permite especificar servicios, redes, volúmenes y variables de entorno de forma declarativa. Un solo `docker compose up` levanta toda la infraestructura local.
**En el proyecto:** El `docker-compose.yml` del sistema de mensajería levanta PostgreSQL, la API de .NET y en fases avanzadas Redis para caché. El prereq de S03 es tener el back-template compilando con Docker y PostgreSQL corriendo.

---

### Domain (capa)
**Categoría:** Clean Architecture / DDD
**Definición:** Capa más interna de la Clean Architecture. Contiene entidades, Value Objects, enumeraciones y reglas de negocio puras. No depende de ninguna otra capa - no conoce EF Core, ASP.NET, ni PostgreSQL. Es la capa más estable del sistema: cambia solo cuando las reglas de negocio cambian.
**En el proyecto:** En el back-template, la carpeta `Domain/` contiene entidades como `Conversation`, `Contact`, `Message`, `Agent`, `BotFlow`, y Value Objects como `Username` y `Email`. Las reglas de validación viven aquí, no en los Handlers.

---

### DRY (Don't Repeat Yourself)
**Categoría:** Principio de diseño
**Definición:** Principio formulado por Andrew Hunt y David Thomas en "The Pragmatic Programmer": "Cada fragmento de conocimiento debe tener una representación única, inequívoca y autorizada dentro de un sistema." La unidad de análisis es el **conocimiento** (una regla de negocio, una configuración, un algoritmo), no las líneas de código - dos bloques textualmenten idénticos pueden no violar DRY si representan conocimiento distinto.
**En el proyecto:** La regla "un username válido tiene 3-20 caracteres alfanuméricos" es un fragmento de conocimiento. Si aparece en tres Handlers distintos, viola DRY. La solución es el Value Object `Username` en `Domain/ValueObjects/`, donde la regla vive en un único método `Create()`.

---

## E

### EF Core (Entity Framework Core)
**Categoría:** ORM / .NET / Base de datos
**Definición:** ORM (Object-Relational Mapper) de Microsoft para .NET. Permite trabajar con bases de datos usando objetos C# y LINQ en lugar de SQL explícito. Gestiona el cambio de estado de las entidades (Change Tracking), genera SQL optimizado para las queries y ofrece características como Global Query Filters, migraciones y configuraciones por entidad.
**En el proyecto:** El ADR-003 elige EF Core 10 como ORM principal. La razón decisiva es el Global Query Filter, que aplica `TenantId` en todas las queries automáticamente, garantizando el aislamiento multi-tenant sin disciplina manual de cada desarrollador.

---

### EntityTypeConfiguration
**Categoría:** EF Core / Infraestructura
**Definición:** Clase que implementa `IEntityTypeConfiguration<TEntity>` y centraliza la configuración de mapeo de una entidad de dominio a la base de datos: nombre de tabla, columnas, claves primarias, índices, constraints y relaciones. Separa la configuración de persistencia de la entidad de dominio, manteniendo el Domain libre de atributos de EF Core.
**En el proyecto:** Cada entidad del sistema de mensajería tiene su `EntityTypeConfiguration`: `ConversationConfiguration`, `ContactConfiguration`, `MessageConfiguration`, etc. Estas clases viven en `Infrastructure/Persistence/`.

---

### EnsureCreatedAsync()
**Categoría:** EF Core / Base de datos
**Definición:** Método de `DbContext` que crea la base de datos y el esquema basándose en las configuraciones de las entidades, si la base de datos no existe. No usa migraciones - simplemente crea el esquema tal como lo definen las `EntityTypeConfiguration`. Apropiado para desarrollo y testing; en producción se usan migraciones explícitas.
**En el proyecto:** El `DatabaseInitializationService` del back-template llama a `EnsureCreatedAsync()` al arrancar. En una sesión posterior, cuando el equipo aprenda EF Core avanzado, se puede evolucionar a migraciones.

---

### ExecuteUpdateAsync()
**Categoría:** EF Core / Performance
**Definición:** Método de EF Core 7+ que ejecuta un `UPDATE` SQL directamente en la base de datos sin cargar la entidad en memoria. Es más eficiente que el patrón clásico de cargar la entidad, modificarla en memoria y llamar `SaveChangesAsync()`. No activa los interceptores de Change Tracking.
**En el proyecto:** Para actualizaciones simples como "marcar una conversación como cerrada" o "actualizar el estado de un agente", `ExecuteUpdateAsync()` es preferible a cargar la entidad completa. El ADR-003 lo menciona como una ventaja de EF Core sobre Dapper para updates frecuentes.

---

### Extension Method
**Categoría:** C# / Programación
**Definición:** Método estático en C# que se puede llamar como si fuera un método de instancia de un tipo existente, sin modificar ese tipo. Se define en una clase estática con el primer parámetro prefijado con `this`. Son útiles para centralizar lógica de transformación reutilizable (un caso de abstracción DRY) sin herencia.
**En el proyecto:** Un ejemplo: `string.ToSlug()` para generar slugs de nombres de canales, o `IQueryable<T>.ApplyPagination(cursor)` para aplicar lógica de cursor pagination de forma reutilizable.

---

## F

### Factor III (Twelve-Factor App)
**Categoría:** Arquitectura / Twelve-Factor App
**Definición:** El tercer factor de la metodología Twelve-Factor App: "Store config in the environment" (almacena la configuración en el entorno). La configuración que varía entre ambientes (dev, staging, prod) debe estar en variables de entorno, no hardcodeada en el código. Las connection strings, API keys y URLs de servicios externos son ejemplos de Factor III.
**En el proyecto:** El back-template lee la connection string de `IConfiguration` - cumple Factor III. El archivo `.env` de Docker Compose inyecta las variables de entorno en el contenedor local. En producción, AWS SSM Parameter Store o Secrets Manager proveen los valores.

---

### Feature Flag
**Categoría:** Arquitectura SaaS / Gestión de funcionalidad
**Definición:** Mecanismo que permite activar o desactivar funcionalidades del sistema en tiempo de ejecución sin desplegar código nuevo. Útil para: lanzamientos graduales (activar para el 10% de usuarios primero), A/B testing, y control de acceso a funciones premium por plan de suscripción.
**En el proyecto:** En una sesión posterior se introduce feature flags para controlar qué funcionalidades están disponibles por plan (por ejemplo, el bot flow builder solo para plan Pro). Los feature flags evitan if-en-if en el código - la lógica de qué tiene acceso a qué vive en un solo lugar.

---

### FTS (Full-Text Search)
**Categoría:** Base de datos / PostgreSQL
**Definición:** Búsqueda de texto completo - capacidad de buscar texto libre en columnas de una base de datos con índices especializados que tokenizan y normalizan el texto. PostgreSQL ofrece FTS nativo con tipos `tsvector` y `tsquery`, e índices GIN que hacen las búsquedas muy eficientes sin necesidad de un motor externo como Elasticsearch.
**En el proyecto:** El ADR-002 elige PostgreSQL precisamente por FTS nativo. La búsqueda de conversaciones y contactos en la bandeja de entrada usa FTS con índices GIN, evitando la complejidad operacional de Elasticsearch en v1.

---

### FromSqlRaw()
**Categoría:** EF Core / SQL
**Definición:** Método de EF Core que permite ejecutar SQL raw (escrito a mano) y mapear el resultado a entidades. A diferencia de la interpolación de strings, `FromSqlRaw` acepta parámetros separados que EF Core sanitiza, previniendo SQL injection. Se usa para queries muy complejas que el generador de EF Core no optimiza bien.
**En el proyecto:** El ADR-003 establece: "Para queries muy complejas -> `FromSqlRaw` con parámetros, nunca interpolación." Ejemplo: una query con CTEs recursivas para threads de conversación.

---

## G

### GIN (Generalized Inverted Index)
**Categoría:** PostgreSQL / Índices
**Definición:** Tipo de índice de PostgreSQL optimizado para búsquedas en estructuras compuestas: arrays, JSONB y `tsvector` (FTS). Un índice GIN sobre una columna `tsvector` permite búsquedas de texto completo muy eficientes. Es más costoso de mantener (actualizaciones más lentas) pero mucho más rápido en lecturas.
**En el proyecto:** El ADR-002 menciona los índices GIN para FTS. En una sesión posterior, al implementar la búsqueda de conversaciones y contactos, se crean índices GIN sobre las columnas de texto relevantes.

---

### GitHub Actions
**Categoría:** CI/CD / DevOps
**Definición:** Plataforma de CI/CD integrada en GitHub que permite definir pipelines de automatización como archivos YAML en el repositorio. Cada workflow se dispara por eventos (push, PR, schedule) y ejecuta jobs en runners (máquinas virtuales) alojadas por GitHub o auto-hospedadas.
**En el proyecto:** El pipeline de CI/CD del sistema de mensajería se implementa más adelante con GitHub Actions. Incluye build, tests, análisis de seguridad y despliegue a AWS usando OIDC para autenticación sin secretos de larga duración.

---

### Global Query Filter
**Categoría:** EF Core / Multi-tenancy
**Definición:** Filtro configurado en `OnModelCreating` de EF Core que se aplica automáticamente a todas las queries de una entidad. El caso de uso principal es multi-tenancy: filtrar por `TenantId` de forma automática para que ningún developer pueda olvidarse de incluir el filtro. Se puede omitir explícitamente con `.IgnoreQueryFilters()`.
**En el proyecto:** Este es el argumento decisivo del ADR-003 para elegir EF Core sobre Dapper. Con Global Query Filter, `TenantId` se aplica automáticamente. Con Dapper, un developer que olvide el filtro en un query expone datos de otro workspace - una vulnerabilidad crítica en un CRM con datos de clientes.

---

### God Object
**Categoría:** Code smell / Anti-patrón
**Definición:** Clase que hace demasiadas cosas - conoce demasiado o controla demasiado del sistema. Es una violación de SRP. Los God Objects son difíciles de testear, de mantener y de entender porque concentran responsabilidades que deberían estar distribuidas en múltiples clases.
**En el proyecto:** Un `ConversationService` que maneja creación, asignación, cierre, búsqueda, notificaciones y facturación de conversaciones sería un God Object. Clean Architecture previene esto distribuyendo la lógica en Handlers específicos por caso de uso.

---

## H

### Handler
**Categoría:** Clean Architecture / CQRS
**Definición:** Clase de la capa Application que implementa un caso de uso específico. Recibe un Request (Command o Query), orquesta las operaciones necesarias (validación, acceso a repositorios, llamadas a servicios) y devuelve un Response. En el back-template, cada Handler tiene su propia carpeta en `Application/UseCases/`.
**En el proyecto:** `RegisterHandler` recibe `RegisterRequest`, crea el Value Object `Username`, inserta la credencial y devuelve `RegisterResponse` (que puede ser `RegisterSuccess` o `RegisterValidationFailure`). Cada funcionalidad del sistema de mensajería tiene su Handler: `CreateConversationHandler`, `AssignAgentHandler`, `SearchContactsHandler`, etc.

---

### Hardcode
**Categoría:** Code smell / Anti-patrón
**Definición:** Práctica de escribir valores de configuración directamente en el código fuente en lugar de obtenerlos de variables de entorno, archivos de configuración o un sistema de secretos. Viola DRY (el valor aparece en el código fuente en lugar de en la configuración) y el Factor III de Twelve-Factor App. También representa un riesgo de seguridad si el valor es sensible (contraseña, API key).
**En el proyecto:** La connection string hardcodeada en `UserRepository` es el ejemplo del slides. En el sistema de mensajería, ningún valor de configuración (URLs de APIs externas, credenciales de WhatsApp/Instagram, llaves JWT) puede aparecer hardcodeado en el código fuente.

---

### Homework (S02)
**Categoría:** Proceso pedagógico
**Definición:** Tarea asignada al final de cada sesión, a completar durante la semana. En S02 consiste en: leer `dev-notes/01-fundamentos/03-refactoring.md` y el capítulo 7 de "The Pragmatic Programmer" (DRY); crear el Value Object `Username` con validación completa; crear el Value Object `Email`; y redactar un ADR adicional sobre una decisión técnica real.
**En el proyecto:** El criterio de aceptación del homework de S02 es concreto: "PR con los 2 Value Objects y el ADR, proyecto compila." Esto se revisa en el bloque inicial de S03.

---

## I

### IgnoreQueryFilters()
**Categoría:** EF Core / Multi-tenancy
**Definición:** Método de EF Core que omite los Global Query Filters para una query específica. Solo debe usarse en contextos donde el filtro de tenant no aplica: el proceso de login (donde se busca por email en todos los tenants para autenticar), el proceso de refresh token, o consultas de administración super-admin.
**En el proyecto:** El Handler de login usa `IgnoreQueryFilters()` porque necesita buscar credenciales por email sin filtrar por workspace. En cualquier otro contexto, este método es una señal de alerta que debe revisarse en code review.

---

### Immutability (ADR)
**Categoría:** Arquitectura / Documentación
**Definición:** Propiedad de los ADRs que establece que no se editan una vez creados. Si una decisión cambia, se crea un nuevo ADR con status `Superseded by ADR-XXX` y se actualiza el status del ADR original. Esto preserva el historial completo de decisiones, incluyendo las que se tomaron y luego se revisaron.
**En el proyecto:** Si más adelante el equipo decide migrar de EF Core a una arquitectura híbrida con Dapper para reads, se crea `ADR-007-dapper-reads-supersedes-ADR-003.md` y se actualiza el ADR-003 para referenciar al nuevo.

---

### Increment (Scrum)
**Categoría:** Scrum / Agile
**Definición:** El resultado concreto y potencialmente liberable de un sprint. Es la suma de todo el trabajo completado que cumple la Definition of Done durante el sprint, más el trabajo de todos los sprints anteriores. "Potencialmente liberable" no significa que se desplegará - significa que está en condiciones de hacerlo si el Product Owner lo decide.
**En el proyecto:** Al final de cada sprint del sistema de mensajería, el Increment podría incluir el módulo de Auth completo o los canales de WhatsApp e Instagram. El sprint no termina cuando el código está escrito - termina cuando el Increment cumple la DoD.

---

### Interfaz (Interface)
**Categoría:** C# / Principios de diseño
**Definición:** Contrato en C# que define qué métodos y propiedades debe exponer una clase, sin implementación. Las interfaces son el mecanismo principal para aplicar DIP: la capa Application define interfaces de repositorio; la capa Infrastructure las implementa. Esto permite sustituir implementaciones (real vs. mock) sin cambiar el consumidor.
**En el proyecto:** `ICredentialRepository`, `IConversationRepository`, `IContactRepository` son interfaces definidas en Application o Domain. Sus implementaciones (`CredentialRepository`, etc.) viven en Infrastructure y usan EF Core + Npgsql.

---

## J

### JSONB
**Categoría:** PostgreSQL / Base de datos
**Definición:** Tipo de columna de PostgreSQL que almacena datos JSON en formato binario optimizado para búsqueda y operaciones. A diferencia de `JSON` (texto plano), `JSONB` soporta índices (incluyendo GIN), operadores de consulta eficientes y descomposición. Ideal para datos semi-estructurados que varían entre registros.
**En el proyecto:** El ADR-002 menciona JSONB para almacenar los flujos del bot (BotFlow). Cada flujo es un grafo de nodos y conexiones - una estructura que varía por flujo y que no encaja bien en columnas relacionales fijas. JSONB permite almacenar y consultar estos flujos de forma flexible.

---

## K

### Kata (Refactoring Kata)
**Categoría:** Proceso pedagógico / Práctica
**Definición:** Ejercicio de programación repetible diseñado para practicar una habilidad específica hasta que se vuelve natural. El término viene de las artes marciales. Un Refactoring Kata presenta código con problemas conocidos y el objetivo es mejorarlo aplicando principios (DRY, KISS, YAGNI) paso a paso, con un commit después de cada cambio.
**En el proyecto:** El bloque de de S02 es una Refactoring Kata en tres partes: extraer `Username` como Value Object (DRY), simplificar el Handler más complejo (KISS), e identificar y eliminar abstracciones innecesarias (YAGNI).

---

### KISS (Keep It Simple, Stupid)
**Categoría:** Principio de diseño
**Definición:** Principio de diseño que establece que la simplicidad debe ser una meta de diseño y que la complejidad innecesaria debe evitarse. Cada capa de abstracción no justificada agrega archivos para navegar, indirecciones para razonar y superficies donde los bugs se ocultan. Una abstracción justificada resuelve un problema concreto hoy; una no justificada es deuda técnica inmediata.
**En el proyecto:** En el sistema de mensajería, un `switch` expression en un Handler que maneja tres casos de asignación es más simple (y preferible bajo KISS) que seis clases con factory, strategy e interfaces que resuelven el mismo problema. KISS se viola más a menudo por exceso que por defecto.

---

## L

### LINQ (Language Integrated Query)
**Categoría:** C# / EF Core
**Definición:** Conjunto de extensiones del lenguaje C# que permiten escribir consultas sobre colecciones y fuentes de datos usando sintaxis integrada en el lenguaje. Con EF Core, LINQ se traduce a SQL. Es fuertemente tipado - el compilador detecta errores de mapeo en tiempo de compilación, a diferencia del SQL de cadenas.
**En el proyecto:** Todas las queries de EF Core en el sistema de mensajería se escriben con LINQ. `context.Conversations.Where(c => c.Status == ConversationStatus.Open).AsNoTracking()` es más seguro y refactorizable que SQL en strings.

---

### Long Method
**Categoría:** Code smell / Refactoring
**Definición:** Code smell que describe un método demasiado largo - generalmente más de 20-30 líneas. Los métodos largos mezclan múltiples niveles de abstracción, son difíciles de entender de un vistazo y generalmente tienen complejidad ciclomática alta. La solución es "Extract Method": extraer bloques lógicos en métodos privados con nombres descriptivos.
**En el proyecto:** Un `CreateConversationHandler.Handle()` que valida el request, busca el agente disponible, crea la conversación, asigna el agente y envía la notificación - todo en un solo método de 80 líneas - viola este principio. Cada responsabilidad merece su propio método privado o clase.

---

## M

### MADR (Markdown Any Decision Records)
**Categoría:** Arquitectura / Documentación
**Definición:** Formato ligero y estándar para escribir ADRs en Markdown. Define secciones específicas: título, Status, Date, Deciders, Contexto, Decisión y Consecuencias. "Any" (antes "Architecture") refleja que el formato es útil para cualquier tipo de decisión, no solo de arquitectura.
**En el proyecto:** Los tres ADRs iniciales del sistema de mensajería siguen el formato MADR. La carpeta `docs/adr/` aloja los archivos con la convención `ADR-{3 dígitos}-{slug}.md`.

---

### Martin, Robert C. (Uncle Bob)
**Categoría:** Persona / Referencia
**Definición:** Ingeniero de software estadounidense, autor de "Clean Code" (2008) y "Clean Architecture" (2017), y uno de los firmantes del Manifiesto Ágil. Popularizó los principios SOLID, el concepto de Clean Architecture y la Boy Scout Rule. Su frase "el código se escribe una vez y se lee decenas de veces" abre la sesión S02.
**En el proyecto:** Clean Architecture (la base del back-template) es su propuesta arquitectónica. Las reglas de estilo de código del proyecto (nombres descriptivos, métodos cortos, sin comentarios obvios) vienen de "Clean Code".

---

### Merge (Git)
**Categoría:** Git / Control de versiones
**Definición:** Operación de Git que integra los cambios de una rama en otra. En el contexto de Pull Requests, merge es el paso final después de que el código pasa code review y CI verde. El equipo define la estrategia de merge (merge commit, squash merge, rebase) más adelante.
**En el proyecto:** La DoD exige "Merged a `develop`" como paso final. Los PRs de ADRs del hands-on de S02 deben mergearse antes de terminar el bloque de 13:00.

---

### Microservicios
**Categoría:** Arquitectura de software
**Definición:** Estilo arquitectónico que divide una aplicación en servicios pequeños e independientes, cada uno con su propia base de datos, desplegable de forma autónoma y comunicándose a través de APIs o eventos. Ofrece escalabilidad independiente por servicio y autonomía de equipos, pero agrega complejidad operacional significativa: service discovery, circuit breakers, distributed tracing, múltiples pipelines de CI/CD.
**En el proyecto:** El ADR-001 justifica explícitamente por qué NO se empieza con microservicios: equipo pequeño, dominio no definido, sin DevOps maduro.

---

### Monolito Modular
**Categoría:** Arquitectura de software
**Definición:** Arquitectura donde toda la aplicación se despliega como una sola unidad (monolito) pero el código está organizado en módulos bien definidos con límites claros entre sí. Ofrece la simplicidad operacional del monolito (un solo proceso, un solo pipeline de CI/CD) con la organización del código que facilita una futura extracción a microservicios si el dominio lo justifica.
**En el proyecto:** El ADR-001 establece el monolito modular con Clean Architecture como arquitectura inicial del sistema de mensajería. Los módulos (Auth, Conversations, Contacts, Channels, BotFlows) tienen límites claros en el código pero se despliegan juntos.

---

### MRR / ARR
**Categoría:** Métricas SaaS / Negocios
**Definición:** Monthly Recurring Revenue (MRR) y Annual Recurring Revenue (ARR) - métricas clave de negocio SaaS que miden los ingresos recurrentes previsibles. MRR es la suma de todos los ingresos por suscripción en un mes. ARR es MRR × 12. Son indicadores centrales de la salud financiera de un negocio SaaS.
**En el proyecto:** Aunque el sistema de mensajería no implementa billing en las primeras fases, el equipo debe entender estas métricas porque influyen en decisiones técnicas: qué features priorizar, qué planes ofrecer, qué límites imponer por plan.

---

## N

### Npgsql
**Categoría:** .NET / PostgreSQL
**Definición:** Driver de .NET para PostgreSQL - la biblioteca que permite que EF Core (y Dapper) se comuniquen con PostgreSQL. El ADR-003 especifica Npgsql 10.0.1 compatible con EF Core 10.
**En el proyecto:** `Npgsql.EntityFrameworkCore.PostgreSQL` es el paquete NuGet que conecta EF Core con PostgreSQL. Se registra en la configuración del DbContext en `Infrastructure/`.

---

## O

### ORM (Object-Relational Mapper)
**Categoría:** Acceso a datos / .NET
**Definición:** Biblioteca que mapea objetos de programación orientada a objetos a tablas relacionales, permitiendo trabajar con la base de datos usando el lenguaje de la aplicación en lugar de SQL explícito. Gestiona la traducción bidireccional: de objetos a filas (INSERT/UPDATE) y de filas a objetos (SELECT).
**En el proyecto:** EF Core es el ORM elegido en ADR-003. Maneja el mapeo entre entidades C# (como `Conversation`, `Contact`) y tablas PostgreSQL, con la ventaja adicional del Global Query Filter para multi-tenancy.

---

## P

### Patrón Strategy
**Categoría:** Patrones de diseño / Comportamiento
**Definición:** Patrón de diseño que define una familia de algoritmos, encapsula cada uno en una clase separada y los hace intercambiables en tiempo de ejecución. Permite que el algoritmo varíe independientemente del cliente que lo usa. Es aplicable cuando existe una lógica que cambia independientemente del resto del sistema.
En S05, se implementa Strategy para la asignación de conversaciones: "round-robin", "por carga de trabajo" y "por habilidades" son estrategias intercambiables. En S02, KISS dicta que NO se extrae Strategy todavía - el requisito actual no lo justifica.

---

### Pipeline Behaviors
**Categoría:** MediatR / Clean Architecture
**Definición:** Middleware para el pipeline de MediatR que se ejecuta antes y/o después de cada Handler. Permiten aplicar cross-cutting concerns (validación, logging, caching, autorización) de forma centralizada sin duplicar código en cada Handler.
**En el proyecto:** En una sesión posterior se implementan Pipeline Behaviors: `ValidationBehavior` (ejecuta validaciones con FluentValidation antes del Handler), `LoggingBehavior` (registra la entrada y salida de cada Handler) y posiblemente `CachingBehavior` para queries.

---

### PostgreSQL
**Categoría:** Base de datos / Infraestructura
**Definición:** Sistema de gestión de bases de datos relacional de código abierto, conocido por su robustez, extensibilidad y soporte de características avanzadas: FTS nativo, JSONB, RLS, window functions, CTEs recursivas, tipos de datos geoespaciales, etc. Es el estándar de facto para aplicaciones .NET modernas que no requieren SQL Server por razones de licencia.
**En el proyecto:** Elegido en ADR-002 como base de datos principal. Corre en Docker en local y en AWS RDS en producción. El equipo conocía SQL Server y debe aprender las especificidades de PostgreSQL (sintaxis ligeramente diferente, tipos de datos, funciones propias).

---

### PR (Pull Request)
**Categoría:** Git / Proceso de desarrollo
**Definición:** Solicitud para integrar los cambios de una rama de feature en la rama principal (develop o main). Es el mecanismo central de code review: el autor describe los cambios, los revisores los inspeccionan, solicitan modificaciones o aprueban, y finalmente se hace merge si el CI pasa y hay al menos un review aprobado.
**En el proyecto:** La DoD exige "PR con criterios de aceptación validados" y "Code review aprobado por al menos 1 compañero." En S02, los ADRs del hands-on se crean y mergean vía PR.

---

### Pragmatic Programmer, The (libro)
**Categoría:** Bibliografía / Referencia
**Definición:** Libro de Andrew Hunt y David Thomas (1999, 2a edición 2019) considerado uno de los libros fundacionales de la ingeniería de software profesional. Introduce conceptos como DRY, Tracer Bullets, la metáfora del programador como artesano, y muchos otros principios de desarrollo pragmático. El capítulo 7 es la fuente original de la definición de DRY.
**En el proyecto:** Es lectura de homework de S02. La definición exacta de DRY que se usa en la sesión viene de este libro. El equipo debería leer al menos el capítulo 7 antes de S03.

---

### Presenter
**Categoría:** Clean Architecture / back-template
**Definición:** Componente que transforma el Response devuelto por un Handler en la respuesta HTTP correspondiente. En el back-template, usa pattern matching sobre los subtipos de Response para devolver el código HTTP correcto: `RegisterSuccess` -> `201 Created`, `RegisterValidationFailure` -> `400 Bad Request`.
**En el proyecto:** Cada endpoint del sistema de mensajería tiene su Presenter. El pattern matching garantiza que todos los casos del Response están cubiertos - si se agrega un nuevo subtipo, el compilador obliga a manejarlo.

---

### Product Owner (PO)
**Categoría:** Scrum / Roles
**Definición:** Rol en Scrum responsable de maximizar el valor del producto. Gestiona y prioriza el Product Backlog, define los criterios de aceptación de las historias de usuario, y decide qué se construye en cada sprint. Es el puente entre el negocio y el Dev Team.
**En el proyecto:** El Product Owner del sistema de mensajería es quien representa a la empresa (el cliente). Aprueba el Increment al final de cada sprint en el Sprint Review. La deuda técnica que bloquea velocidad se prioriza con él - compite con features nuevas en el backlog.

---

### Pull Request (ver PR)

---

## R

### Refactoring
**Categoría:** Ingeniería de software / Calidad
**Definición:** Proceso de modificar el código interno de un sistema sin cambiar su comportamiento externo observable. El objetivo es mejorar la legibilidad, mantenibilidad y diseño del código. Los tests unitarios son el safety net que garantiza que el comportamiento no cambia durante el refactoring.
**En el proyecto:** El kata de S02 es un ejercicio de refactoring. La regla es "un paso a la vez, commit después de cada cambio, tests pasan antes del commit." La Boy Scout Rule propone pequeños refactorings en cada PR, evitando sprints de refactoring masivos que el PO rechaza porque "no aportan features."

---

### Request (Command/Query)
**Categoría:** Clean Architecture / back-template
**Definición:** Objeto de datos que encapsula los parámetros de entrada de un caso de uso. En el back-template, cada caso de uso tiene su propio Request: `RegisterRequest`, `CreateConversationRequest`, etc. Los Commands modifican estado; las Queries lo leen. Ambos son objetos simples (records en C#) sin lógica de negocio.
**En el proyecto:** El `RegisterRequest` lleva `string Username` (primitivo serializable), no un `Username` Value Object. El Handler es quien crea el Value Object a partir del primitivo - esto facilita la serialización desde JSON.

---

### Response (abstract record)
**Categoría:** Clean Architecture / back-template
**Definición:** Jerarquía de types en el back-template que representa todos los resultados posibles de un Handler. El tipo base es un `abstract record Response`; los subtipos concretos representan cada caso: éxito con datos, error de validación, no encontrado, etc. El Presenter usa pattern matching para convertirlos en respuestas HTTP.
**En el proyecto:** `RegisterResponse` es el tipo base. `RegisterSuccess(Guid PublicId)` y `RegisterValidationFailure(string Message)` son los subtipos. Este patrón elimina excepciones como mecanismo de control de flujo.

---

### Result Pattern (Result<T>)
**Categoría:** back-template / Diseño de software
**Definición:** Patrón que encapsula el resultado de una operación que puede fallar, exponiendo si fue exitosa (`Result.OK<T>`) o fallida (`Result.Fail<T>`) sin lanzar excepciones para flujos de negocio predecibles. En el back-template, `Result<T>` es el tipo de retorno de los Value Objects y operaciones de dominio. El consumidor verifica el resultado con pattern matching antes de acceder al valor.
**En el proyecto:** `Username.Create()` devuelve `Result<Username>`. El Handler verifica: `if (usernameResult is FailureResult<Username> f) return new RegisterValidationFailure(f.Message);`. Las excepciones se reservan para errores inesperados (bugs), no para validaciones de negocio.

---

### Retrospectiva (Scrum)
**Categoría:** Scrum / Agile
**Definición:** Ceremonia al final de cada sprint donde el Dev Team reflexiona sobre el proceso y define mejoras concretas para el próximo sprint. Preguntas típicas: ¿qué salió bien?, ¿qué mejorar?, ¿qué hacer diferente? Las mejoras son ítems de acción con dueño y fecha.
Las mejoras del proceso de desarrollo del sistema de mensajería se identifican y aplican semana a semana.

---

### RLS (Row Level Security)
**Categoría:** PostgreSQL / Seguridad / Multi-tenancy
**Definición:** Característica de PostgreSQL que permite definir políticas de acceso a nivel de fila - cada usuario de la base de datos (o rol) solo ve las filas que cumplen la política definida. Es una segunda capa de aislamiento de datos para multi-tenancy, complementaria al Global Query Filter de EF Core.
**En el proyecto:** El ADR-002 menciona RLS como una ventaja de PostgreSQL para aislamiento multi-tenant. En fases avanzadas, se puede configurar RLS como seguridad en profundidad: incluso si el Global Query Filter falla, la base de datos no devuelve datos de otro workspace.

---

### Ron Jeffries
**Categoría:** Persona / Referencia
**Definición:** Ingeniero de software y uno de los creadores de Extreme Programming (XP). Es la fuente de la definición exacta de YAGNI: "Always implement things when you actually need them, never when you just foresee that you need them." También es autor de varios libros sobre desarrollo ágil.
**En el proyecto:** Su definición de YAGNI es la que se usa en la sesión. XP (Extreme Programming), la metodología que co-creó, también introduce prácticas como TDD, pair programming y refactoring continuo que complementan el programa.

---

## S

### Saint-Exupéry, Antoine de
**Categoría:** Persona / Referencia cultural
**Definición:** Escritor y aviador francés (1900-1944), autor de "El Principito." Su frase sobre la perfección - "La perfección no se alcanza cuando no hay nada que agregar, sino cuando no hay nada que quitar" - es citada en la sección de KISS para ilustrar que la simplicidad es una meta de diseño activa, no pasiva.
**En el proyecto:** La frase captura la esencia de KISS en el sistema de mensajería: el código más simple que resuelve el problema de hoy es preferible al código más sofisticado que anticipa problemas de mañana.

---

### Scrum
**Categoría:** Metodología / Agile
**Definición:** Marco de gestión de proyectos ágil que organiza el trabajo en sprints (iteraciones cortas de 2-4 semanas), con roles definidos (Product Owner, Scrum Master, Dev Team) y ceremonias específicas (Sprint Planning, Daily Standup, Sprint Review, Retrospectiva). Scrum no prescribe prácticas de ingeniería (eso es XP) - se enfoca en el proceso de gestión.
**En el proyecto:** El equipo del sistema de mensajería trabaja con sprints de 2 semanas.

---

### Scrum Master
**Categoría:** Scrum / Roles
**Definición:** Rol en Scrum responsable de facilitar las ceremonias, eliminar impedimentos y proteger al Dev Team de interrupciones externas. No es un gestor de proyecto ni asigna trabajo - es un facilitador que maximiza la efectividad del equipo. Puede ser un miembro del Dev Team o un rol dedicado.

---

### SDLC (Software Development Life Cycle)
**Categoría:** Proceso de desarrollo / Ingeniería de software
**Definición:** Ciclo de vida del desarrollo de software - el conjunto de fases por las que pasa un sistema desde su concepción hasta su retiro: Requerimientos, Diseño, Desarrollo, Testing, Despliegue y Mantenimiento. Scrum no elimina estas fases - las integra en cada iteración (sprint) en lugar de tratarlas como fases secuenciales separadas (cascada).
**En el proyecto:** Cada sprint del sistema de mensajería pasa por todas las fases del SDLC en miniatura: se definen los requerimientos del sprint (Planning), se diseña la solución (ADRs, discusión técnica), se desarrolla (el sprint), se testea (DoD incluye tests), se despliega (Increment) y se mantiene (bugs en el backlog).

---

### Sealed Record (C#)
**Categoría:** C# / Programación
**Definición:** Tipo de dato en C# que combina las características de `record` (igualdad por valor, inmutabilidad por convención, deconstrucción) con `sealed` (no se puede heredar). Los `sealed record` son ideales para Value Objects porque expresan que el tipo es un leaf en la jerarquía - nadie puede extenderlo y cambiar su semántica.
**En el proyecto:** `Username` se define como `public sealed record Username`. El constructor `private` garantiza que solo `Create()` puede instanciarlo. `sealed` evita subclases que podrían evadir la validación.

---

### Slug (convención de nombres)
**Categoría:** Convenciones / Documentación
**Definición:** Versión de un texto en formato URL-friendly: minúsculas, palabras separadas por guiones, sin caracteres especiales. Los ADRs usan slugs en su nombre de archivo para que sean legibles y compatibles con sistemas de archivos de todos los sistemas operativos.
**En el proyecto:** El archivo `ADR-001-monolito-modular.md` usa el slug `monolito-modular`. La convención es `ADR-{3 dígitos}-{slug-de-la-decisión}.md`.

---

### Speculative Generality
**Categoría:** Code smell / YAGNI
**Definición:** Code smell que describe abstracciones creadas "por si acaso" se necesitan en el futuro, sin un requisito actual que las justifique. Resulta en código más complejo de entender y mantener, sin beneficio presente. Es la violación más común de YAGNI en equipos con experiencia en patrones de diseño.
**En el proyecto:** Crear `IPostScoringAlgorithm`, `BasePostScorer<T>` y `PostScoringFactory` cuando el requisito actual solo pide mostrar posts ordenados por "hot/top/new" es Speculative Generality. El kata de S02 practica identificar y eliminar estas abstracciones.

---

### Sprint
**Categoría:** Scrum / Agile
**Definición:** Iteración de tiempo fijo (timeboxed) en Scrum, típicamente de 1-4 semanas, durante la cual el Dev Team construye un Increment potencialmente liberable. El alcance de un sprint se define en el Sprint Planning y no cambia durante el sprint - los nuevos requerimientos entran al backlog del próximo sprint.
**En el proyecto:** Los sprints del sistema de mensajería son de 2 semanas. El Increment al final de cada sprint debe cumplir la DoD completa.

---

### Sprint Planning
**Categoría:** Scrum / Agile
**Definición:** Ceremonia al inicio de cada sprint donde el Dev Team selecciona los ítems del Product Backlog que se compromete a completar y planifica cómo hacerlo. El Product Owner presenta las prioridades; el equipo estima y negocia el alcance. Resulta en el Sprint Backlog.
**En el proyecto:** Ocurre el lunes del primer día de cada sprint. El Dev Team se compromete a las historias que entran en el sprint basándose en su velocidad histórica y la capacidad del período.

---

### Sprint Review
**Categoría:** Scrum / Agile
**Definición:** Ceremonia al final del sprint donde el Dev Team demuestra el Increment al Product Owner y stakeholders. No es una revisión técnica (eso es el code review) - es una demo del producto funcionando para validar que el trabajo cumple las expectativas del negocio.
**En el proyecto:** Ocurre el viernes del último día del sprint. El equipo muestra las funcionalidades implementadas del sistema de mensajería al Product Owner de la empresa. Las observaciones del PO alimentan el backlog del próximo sprint.

---

### SRP (Single Responsibility Principle)
**Categoría:** SOLID / Principios de diseño
**Definición:** El primero de los principios SOLID, formulado por Robert C. Martin: cada módulo o clase debe tener una sola razón para cambiar. Más precisamente: una clase debe tener un solo actor (persona o sistema) que pueda solicitar cambios en ella. No significa "hace una sola cosa" - significa "cambia por una sola razón."
**En el proyecto:** El homework de S01 pedía identificar un ejemplo de SRP con la explicación del actor que podría pedir el cambio. En el sistema de mensajería, `ConversationRepository` cambia cuando la persistencia de conversaciones cambia; `ConversationValidator` cambia cuando las reglas de negocio de validación cambian - actores distintos.

---

### Status (ADR)
**Categoría:** Arquitectura / Documentación
**Definición:** Campo del formato MADR que indica el estado actual de la decisión: `Proposed` (en discusión), `Accepted` (vigente), `Deprecated` (obsoleta, pero no reemplazada) o `Superseded by ADR-XXX` (reemplazada por una decisión posterior). El status permite entender rápidamente qué decisiones están vigentes sin leer el contenido completo.
**En el proyecto:** Los ADRs del sistema de mensajería comienzan en `Accepted` - las decisiones ya fueron tomadas. Si en fases avanzadas cambia la arquitectura, el status se actualiza a `Superseded by ADR-XXX` y se crea el nuevo ADR.

---

### Strategy Pattern (ver Patrón Strategy)

---

## T

### TDD (Test-Driven Development)
**Categoría:** Proceso de desarrollo / Testing
**Definición:** Práctica de desarrollo donde los tests se escriben antes que el código de producción. El ciclo es: 1) escribir un test que falle (Red), 2) escribir el mínimo código para que el test pase (Green), 3) refactorizar manteniendo los tests en verde (Refactor). Los tests actúan como especificación ejecutable de la funcionalidad.
**En el proyecto:** La DoD exige "tests unitarios pasando." Si bien el programa no impone TDD estricto, el kata de S02 incluye la regla "tests pasan antes del commit" - los tests deben existir y pasar en cada paso del refactoring.

---

### TenantId
**Categoría:** Multi-tenancy / Arquitectura
**Definición:** Identificador único del workspace o tenant en una arquitectura multi-tenant. Se usa para aislar los datos de cada cliente en un sistema SaaS que comparte la misma base de datos. En el sistema de mensajería, cada conversación, contacto, mensaje y agente pertenece a un workspace identificado por su `TenantId`.
**En el proyecto:** El Global Query Filter de EF Core aplica `.Where(e => e.TenantId == _tenantContext.TenantId)` automáticamente en todas las queries. Esta es la razón decisiva del ADR-003. El `TenantId` se inyecta desde el HTTP context al principio de cada request.

---

### Thomas, David / Hunt, Andrew
**Categoría:** Persona / Referencia
**Definición:** Co-autores de "The Pragmatic Programmer" (1999), uno de los libros más influyentes de ingeniería de software. David Thomas y Andrew Hunt formularon el principio DRY en su forma exacta: "Every piece of knowledge must have a single, unambiguous, authoritative representation within a system." También acuñaron el término "Tracer Bullets" y muchos otros conceptos del desarrollo pragmático.
**En el proyecto:** Son la fuente directa de la definición de DRY usada en S02. Su libro es lectura recomendada en el homework de S02.

---

### TODO (comentario de código)
**Categoría:** Convenciones / Deuda técnica
**Definición:** Comentario en código que marca trabajo pendiente. En el contexto del programa, los TODOs deben incluir contexto: `// TODO: [razón] — pagar en S0X`. Un TODO sin contexto es inutilizable semanas después - nadie recuerda por qué ni cuándo se puso.
**En el proyecto:** Los TODOs de deuda técnica deliberada deben tener su par en el backlog como ítem priorizable. Ejemplo: `// TODO: mover timeout a IOptions<HttpClientOptions> — pagar más adelante con Options Pattern`.

---

### Trade-off
**Categoría:** Arquitectura / Toma de decisiones
**Definición:** Compensación entre dos características que no pueden optimizarse simultáneamente - mejorar una empeora la otra. Todas las decisiones de arquitectura implican trade-offs. Un buen ADR los documenta honestamente en la sección Consecuencias en lugar de presentar la decisión como obviamente superior.
**En el proyecto:** EF Core vs. Dapper es un trade-off clásico: EF Core ofrece seguridad multi-tenant automática (Sí: ) a costa de SQL menos predecible en queries complejas (No: ). Elegir EF Core no significa que Dapper es malo - significa que en el contexto del sistema de mensajería los beneficios de EF Core superan sus costos.

---

### TryCreate Pattern
**Categoría:** C# / Value Objects
**Definición:** Patrón donde un método estático devuelve `bool` e incluye el resultado como parámetro `out`. Idiomático en la BCL de .NET: `int.TryParse()`, `Guid.TryParse()`, `DateTime.TryParse()`. Es útil cuando la conversión puede fallar y es la operación central (parsear un código de barras, un GUID en string). Para validación de negocio, `Create()` + `Result<T>` es más expresivo porque incluye el mensaje de error.
**En el proyecto:** El back-template prefiere `Create()` + `Result<T>` para Value Objects de negocio. `TryCreate` se usa en parsers: parsear un código QR escaneado por un dispositivo Zebra, convertir un string UUID de una URL a `Guid`.

---

### Twelve-Factor App
**Categoría:** Arquitectura / Buenas prácticas
**Definición:** Metodología para construir aplicaciones SaaS desarrollada por desarrolladores de Heroku. Define 12 factores que una aplicación debe cumplir para ser desplegable, escalable y mantenible en la nube. El Factor III (configuración en el entorno) es el más relevante para S02. Los demás factores incluyen codebase único, dependencias explícitas, procesos sin estado, etc.
**En el proyecto:** El back-template cumple los factores más críticos. Factor III: configuración vía `IConfiguration` + variables de entorno. Factor VI (procesos sin estado): cada request es independiente, el estado persiste en PostgreSQL y Redis.

---

## U

### Unit Test (Test Unitario)
**Categoría:** Testing / Calidad
**Definición:** Test que verifica el comportamiento de una unidad de código (método, clase) de forma aislada, sin dependencias externas reales (base de datos, API, filesystem). Las dependencias se reemplazan con mocks o stubs. Son rápidos de ejecutar (milisegundos), deterministas y forman el safety net para refactoring.
**En el proyecto:** La DoD del sistema de mensajería exige "tests unitarios pasando" antes de merge. Los Value Objects (como `Username`) son especialmente fáciles de testear unitariamente - no tienen dependencias externas. Los Handlers se testean con repositorios mockeados.

---

## V

### Value Object
**Categoría:** DDD / Clean Architecture / C#
**Definición:** Objeto de dominio definido por su valor, no por su identidad. Dos Value Objects con el mismo valor son iguales. En C#, los `record` implementan igualdad por valor automáticamente. Los Value Objects encapsulan invariantes de dominio (validaciones, restricciones de formato) y su constructor privado + método `Create()` garantizan que nunca existe una instancia inválida.
**En el proyecto:** `Username` y `Email` son los Value Objects del kata de S02. En el sistema de mensajería completo, otros Value Objects candidatos son `PhoneNumber` (para contactos de WhatsApp), `ChannelType` (para discriminar entre WhatsApp/Instagram/Email) y `ConversationStatus`.

---

### Velocidad (Scrum)
**Categoría:** Scrum / Métricas
**Definición:** Cantidad de trabajo (medida en puntos de historia) que el Dev Team completa en promedio por sprint. Se usa para planificar sprints futuros: si la velocidad histórica es 30 puntos, el equipo no debe comprometer más de 30 puntos en el próximo Sprint Planning. La velocidad no es una métrica de productividad individual.
**En el proyecto:** La velocidad del equipo del sistema de mensajería se establece en los primeros sprints y se usa para proyectar cuándo estará listo cada módulo. La deuda técnica alta reduce la velocidad - otro argumento para invertir en DRY, KISS y YAGNI desde el inicio.

---

### Vertical Slice Architecture
**Categoría:** Arquitectura de software
**Definición:** Estilo arquitectónico donde el código se organiza por feature completa (todas las capas de una funcionalidad juntas) en lugar de por capa técnica (todos los repositorios juntos, todos los servicios juntos). Cada slice incluye desde el endpoint hasta la query de base de datos para una funcionalidad específica.
**En el proyecto:** Se introduce en S06. El back-template del sistema de mensajería organiza `Application/UseCases/` por feature (`Users/Register/`, `Conversations/Create/`, `Contacts/Search/`), lo cual es una implementación de Vertical Slice dentro de Clean Architecture.

---

### Viabilidad técnica
**Categoría:** Arquitectura / Gestión de proyecto
**Definición:** Evaluación de si una solución técnica puede implementarse dentro de las restricciones reales del proyecto: tiempo, tamaño del equipo, nivel de madurez técnica, presupuesto de infraestructura. Una arquitectura técnicamente superior puede no ser viable para un equipo y contexto específico.
**En el proyecto:** La viabilidad técnica es el argumento central del ADR-001: microservicios son técnicamente posibles, pero no viables para un equipo de 6 personas sin DevOps maduro construyendo desde cero.

---

## W

### Window Function (PostgreSQL)
**Categoría:** SQL / PostgreSQL avanzado
**Definición:** Función SQL que realiza cálculos sobre un conjunto de filas relacionadas con la fila actual ("ventana"), sin agrupar las filas como `GROUP BY`. Ejemplos: `ROW_NUMBER()`, `RANK()`, `LAG()`, `LEAD()`, `SUM() OVER(PARTITION BY ...)`. Son más expresivas que subqueries para muchos cálculos analíticos.
**En el proyecto:** El ADR-003 menciona window functions como un área donde Dapper tiene ventaja (SQL explícito). En una sesión posterior, al implementar métricas de la bandeja de entrada (conversaciones por agente, tiempo promedio de respuesta), se pueden usar window functions con `FromSqlRaw` o con Dapper puntual.

---

### Workspace
**Categoría:** Multi-tenancy / Sistema de mensajería
**Definición:** Término del dominio del sistema de mensajería que representa un tenant - la organización o empresa que contrata el servicio. Cada workspace tiene sus propios agentes, conversaciones, contactos, canales y configuración. El `TenantId` identifica el workspace de cada request.
**En el proyecto:** `Workspace` es una entidad de dominio del sistema de mensajería. El módulo de Workspace incluye setup, configuración e invitaciones. El Global Query Filter filtra todos los datos por workspace automáticamente.

---

## X

### XP (Extreme Programming)
**Categoría:** Metodología / Agile
**Definición:** Metodología ágil creada por Kent Beck que combina prácticas de ingeniería como TDD, pair programming, refactoring continuo, integración continua y releases pequeños y frecuentes. Es más prescriptiva en prácticas técnicas que Scrum. Ron Jeffries (fuente de YAGNI) es co-creador de XP.
**En el proyecto:** El programa incorpora prácticas de XP: TDD como parte de la DoD, refactoring continuo (Boy Scout Rule), integración continua (GitHub Actions). No adopta pair programming formal, pero el code review cumple una función similar.

---

## Y

### YAGNI (You Aren't Gonna Need It)
**Categoría:** Principio de diseño / XP
**Definición:** Principio de Extreme Programming formulado por Ron Jeffries: "Implementa siempre las cosas cuando realmente las necesites, nunca cuando simplemente preveas que las necesitarás." YAGNI habla de funcionalidad de producto - no prohíbe tests ni buenas prácticas de ingeniería. Prohíbe implementar funcionalidades que ningún requisito actual exige.
En el sistema de mensajería, soporte multi-idioma en v1, exportación a Excel o internacionalización son ejemplos de YAGNI - nadie los pidió aún. Los tests unitarios NO son YAGNI porque son un requisito explícito del plan.

---

*Glosario generado para S02 - Dev Notes. Instructor: Rogelio Arriaga González - Raptor Dev Services.*
