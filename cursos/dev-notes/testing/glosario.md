# Glosario Técnico  -  Testing

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### AppDbContext
**Categoría:** EF Core / Infrastructure
**Definición:** Clase que hereda de `DbContext` y representa la sesión con la base de datos. Contiene los `DbSet<T>` de cada entidad y aplica las configuraciones de mapeo. Es el punto de entrada a EF Core en el proyecto.
**En el proyecto:** `AppDbContext` se excluye del reporte de coverage con `[ExcludeFromCodeCoverage]` porque su comportamiento se valida mediante integration tests con PostgreSQL real, no unit tests.

### Architecture Test
**Categoría:** NetArchTest / Testing
**Definición:** Test automatizado que verifica que las dependencias entre capas o ensamblados respetan las reglas de arquitectura definidas. Inspecciona el IL compilado en tiempo de ejecución, no el código fuente. Si una capa importa otra que no debería, el test falla.
**En el proyecto:** Cada módulo del sistema de mensajería tiene su `*ArchitectureTests.cs` que verifica que `Domain` no depende de nadie, `Application` solo depende de `Domain`, e `Infrastructure` no depende de `Application`.

### Arg.Any\<T\>()
**Categoría:** NSubstitute
**Definición:** Argumento comodín que indica a NSubstitute que acepte cualquier valor del tipo `T` cuando configura el comportamiento de un método mockeado. Equivalente al matcher genérico en otros frameworks.
**En el proyecto:** Se usa en combinación con `Returns()` para configurar repositorios mockeados, por ejemplo `Arg.Any<CancellationToken>()` cuando el valor exacto del token no es relevante para el escenario que se prueba.

### Arg.Is\<T\>(predicate)
**Categoría:** NSubstitute
**Definición:** Argumento condicional que acepta solo valores que satisfacen el predicado lambda. Permite verificaciones más precisas que `Arg.Any<T>()` cuando el valor concreto importa para el escenario del test.
**En el proyecto:** Se usa al verificar que `InsertAsync` recibió exactamente el nombre correcto del contacto: `Arg.Is<string>(n => n == "Ana García")`.

### Assert
**Categoría:** xUnit
**Definición:** Clase estática de xUnit que provee los métodos de verificación (`Assert.Equal`, `Assert.IsType`, `Assert.NotNull`, `Assert.True`, `Assert.False`, `Assert.Contains`). Lanza `XunitException` cuando una verificación falla.
**En el proyecto:** El back-template usa `Assert` nativo de xUnit en lugar de FluentAssertions; cualquier assertion que falle muestra el nombre del test y el valor esperado vs obtenido.

### Assert.Contains()
**Categoría:** xUnit
**Definición:** Verifica que una colección contiene al menos un elemento que satisface el predicado, o que una cadena contiene una subcadena. Lanza excepción si la condición no se cumple.
**En el proyecto:** Se usa para verificar que un resultado de FluentValidation contiene un error en una propiedad específica, por ejemplo `Assert.Contains(result.Errors, e => e.PropertyName == "FullName")`.

### Assert.Equal()
**Categoría:** xUnit
**Definición:** Verifica que dos valores son iguales usando igualdad estructural. El orden de parámetros es `Assert.Equal(expected, actual)`  -  el valor esperado va primero.
**En el proyecto:** Se usa para verificar el contenido del DTO retornado, como `Assert.Equal("John Doe", success.Data.FullName)` o `Assert.Equal("access-token", success.Data.AccessToken)`.

### Assert.False()
**Categoría:** xUnit
**Definición:** Verifica que una expresión booleana es `false`. Útil para confirmar que una validación falló o que un resultado negativo se produjo correctamente.
**En el proyecto:** Se usa en tests de validators de FluentValidation para confirmar que una request inválida produce `result.IsValid == false`.

### Assert.IsAssignableFrom\<T\>()
**Categoría:** xUnit
**Definición:** Verifica que el objeto es asignable al tipo `T`, es decir, que implementa la interfaz o hereda la clase. A diferencia de `Assert.IsType<T>()`, acepta subtipos. Sin embargo, devuelve el valor como `T`, perdiendo acceso a propiedades de tipos más concretos.
**En el proyecto:** El back-template evita esta forma; se prefiere `Assert.IsType<T>()` para obtener el tipo concreto con intellisense completo sobre las propiedades del resultado.

### Assert.IsType\<T\>()
**Categoría:** xUnit
**Definición:** Verifica que el objeto es exactamente de tipo `T` (sin aceptar subtipos). Retorna el objeto casteado al tipo `T`, permitiendo acceder a sus propiedades inmediatamente. Lanza si el tipo no coincide exactamente.
**En el proyecto:** Patrón estándar en el back-template para verificar el tipo de respuesta de un Handler: `var success = Assert.IsType<GetUserProfileSuccess>(result)`, que además devuelve `success` con acceso a `success.Data`.

### Assert.NotNull()
**Categoría:** xUnit
**Definición:** Verifica que un objeto no es `null`. Si el valor es `null`, el test falla con un mensaje que incluye el nombre del parámetro verificado.
**En el proyecto:** Se usa junto con `Assert.IsType<T>()` para confirmar que el campo `Data` de un resultado de éxito contiene información antes de acceder a sus propiedades.

### Assert.NotEqual()
**Categoría:** xUnit
**Definición:** Verifica que dos valores son distintos. Útil cuando no se sabe el valor exacto esperado pero sí se sabe que no debe ser un valor específico (como un código HTTP prohibido).
**En el proyecto:** Se usa en integration tests para confirmar que un rol con acceso no recibe `403 Forbidden`: `Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode)`.

### Assert.True()
**Categoría:** xUnit
**Definición:** Verifica que una expresión booleana es `true`. La forma más simple de verificación; útil cuando el resultado ya es un booleano, como el resultado de `NetArchTest`.
**En el proyecto:** Se usa en tests de arquitectura: `Assert.True(result.IsSuccessful)` después de evaluar una regla de NetArchTest.

### AuthenticationHeaderValue
**Categoría:** Integration Testing / ASP.NET Core
**Definición:** Clase de `System.Net.Http.Headers` que representa el valor del header `Authorization`. Permite construir headers de autenticación como `Bearer <token>` para enviar en requests HTTP de tests.
**En el proyecto:** Se asigna a `_client.DefaultRequestHeaders.Authorization` en integration tests para simular un usuario autenticado con un token JWT generado por `JwtTestHelper`.

---

## B

### BCrypt
**Categoría:** Seguridad / Authentication
**Definición:** Algoritmo de hashing de contraseñas que incluye un salt aleatorio y un factor de costo configurable. Diseñado para ser lento y resistir ataques de fuerza bruta. Cada hash es distinto incluso para la misma contraseña.
**En el proyecto:** `IPasswordHasher` en el módulo de Authentication abstrae BCrypt. El método `Verify(plainPassword, hash)` es mockeado en `LoginHandlerTests` con NSubstitute para controlar el resultado sin ejecutar el algoritmo real.

### Bearer Token
**Categoría:** Seguridad / JWT
**Definición:** Esquema de autenticación HTTP donde el cliente envía un token en el header `Authorization: Bearer <token>`. El servidor valida el token sin consultar la base de datos en cada request.
**En el proyecto:** Los integration tests construyen `AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(...))` para simular requests de usuarios autenticados en los endpoints del sistema de mensajería.

### Branch Coverage
**Categoría:** Testing / Coverage
**Definición:** Métrica de cobertura que mide qué porcentaje de las ramas condicionales (if/else, switch, ternary) fueron ejecutadas por los tests. Es más exigente que la cobertura de líneas porque requiere probar ambas ramas de cada condicional.
**En el proyecto:** El reporte de coverage muestra columnas separadas para `Líneas` y `Branches`. Un Handler con dos ramas (éxito y no encontrado) tiene 2 branches; se necesita un test por cada rama para alcanzar 100% branch coverage.

---

## C

### CancellationToken
**Categoría:** .NET / Async
**Definición:** Estructura que permite cancelar operaciones asíncronas cooperativamente. Se propaga desde el endpoint HTTP hasta los repositorios. En tests se puede pasar `default` (equivale a `CancellationToken.None`) o mockear con `Arg.Any<CancellationToken>()`.
**En el proyecto:** Todos los métodos de repositorio y handlers aceptan `CancellationToken`. En unit tests se pasa `default`; en la configuración del mock se usa `Arg.Any<CancellationToken>()` para no restringir el valor.

### Clean Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que organiza el código en capas concéntricas con reglas de dependencia estrictas: las capas internas no conocen a las externas. Las capas típicas son Domain, Application, Infrastructure y Presentation.
**En el proyecto:** Los tests de arquitectura con NetArchTest son el mecanismo que hace cumplir las reglas de Clean Architecture de forma automática. Si `Application` importa algo de `Infrastructure`, el test falla antes del code review.

### Code Coverage
**Categoría:** Testing
**Definición:** Métrica que indica qué porcentaje del código de producción fue ejecutado durante la ejecución de los tests. Se mide en líneas, ramas o instrucciones. No garantiza calidad de los tests, solo que el código fue ejecutado.
**En el proyecto:** Se genera con `dotnet test --collect:"XPlat Code Coverage"`, produciendo un archivo `coverage.cobertura.xml`. El umbral objetivo es 70-80% para el solution completo, con 85-95% en Handlers y 90-100% en Validators.

### coverlet
**Categoría:** Testing / NuGet
**Definición:** Herramienta de code coverage multiplataforma para .NET que se integra con el test runner. Recolecta datos de cobertura durante la ejecución de tests y genera reportes en formatos como Cobertura XML o OpenCover.
**En el proyecto:** Agregado como `coverlet.collector` en cada `.csproj` de tests. Se activa con la flag `--collect:"XPlat Code Coverage"` al ejecutar `dotnet test`. Permite excluir namespaces mediante `coverlet.runsettings`.

---

## D

### DbContext
**Categoría:** EF Core
**Definición:** Clase base de Entity Framework Core que representa una sesión con la base de datos. Gestiona el ciclo de vida de las entidades, el change tracking y la ejecución de queries SQL.
**En el proyecto:** `AppDbContext` hereda de `DbContext`. En integration tests se usa una instancia real contra PostgreSQL; nunca se usa `InMemoryDatabase` porque no enforcea constraints ni transacciones reales.

### DidNotReceive()
**Categoría:** NSubstitute
**Definición:** Verificación que confirma que un método del sustituto nunca fue llamado. Falla si el método fue invocado al menos una vez. Es el equivalente negativo de `Received(N)`.
**En el proyecto:** Se usa para verificar efectos secundarios esperados cuando el flujo principal no debería ejecutarse. Ejemplo: si un email ya existe, `DidNotReceive().InsertAsync(...)` confirma que no se insertó ningún registro en la base de datos.

### DI  -  Dependency Injection
**Categoría:** .NET / Arquitectura
**Definición:** Patrón en el que las dependencias de una clase se proveen desde el exterior (generalmente por un contenedor IoC) en lugar de crearlas internamente. Facilita el testing al permitir sustituir dependencias reales por mocks.
**En el proyecto:** Los integration tests verifican que el contenedor de DI registra correctamente los Handlers, repositorios y servicios. Si un servicio no está registrado, el test falla con una excepción de resolución de dependencias.

### Domain Assembly
**Categoría:** NetArchTest / .NET
**Definición:** Ensamblado compilado que contiene las clases del dominio (entidades, value objects, interfaces de repositorio). En NetArchTest se referencia mediante `typeof(UnaEntidad).Assembly` para obtener el objeto `Assembly` y aplicar reglas sobre él.
**En el proyecto:** Cada módulo expone su `DomainAssembly` como `typeof(UserProfile).Assembly`, `typeof(UserCredential).Assembly` o `typeof(Tenant).Assembly` para construir los tests de arquitectura del módulo correspondiente.

### dotnet test
**Categoría:** .NET CLI
**Definición:** Comando de la CLI de .NET que descubre y ejecuta todos los tests del proyecto o solución. Soporta filtros, niveles de verbosidad y colección de cobertura mediante flags adicionales.
**En el proyecto:** El equipo ejecuta `dotnet test` como primer paso de validación antes de crear un PR. Con `--filter` se pueden aislar tests por clase, módulo o categoría. Con `--collect:"XPlat Code Coverage"` se obtiene el reporte de cobertura.

---

## E

### E2E Tests  -  End-to-End Tests
**Categoría:** Testing
**Definición:** Tests que verifican flujos completos de usuario interactuando con la aplicación a través de la interfaz real (browser o cliente HTTP). Son los más costosos en tiempo y mantenimiento, pero los que más se parecen al uso real del sistema.
**En el proyecto:** Representan el 10% de la pirámide de tests. Herramienta recomendada: Playwright. Cubren flujos completos como registro de usuario, login, creación de conversación y asignación de agente.

### Ensamblado (Assembly)
**Categoría:** .NET
**Definición:** Unidad de compilación y despliegue en .NET. Un proyecto `.csproj` compila a un ensamblado (`.dll`). Contiene los tipos compilados, sus metadatos y los recursos asociados.
**En el proyecto:** NetArchTest opera sobre ensamblados compilados, no sobre código fuente. Cada capa (Domain, Application, Infrastructure) es un ensamblado separado, lo que permite verificar las dependencias entre ellos.

### ExcludeFromCodeCoverage
**Categoría:** Testing / .NET
**Definición:** Atributo de `System.Diagnostics.CodeAnalysis` que indica a las herramientas de cobertura que excluyan la clase, método o propiedad decorada del reporte de coverage. No afecta la ejecución de los tests.
**En el proyecto:** Se aplica a `AppDbContext` y `ServiceCollectionExtensions` porque su cobertura no aporta información útil  -  son cubiertas indirectamente por integration tests, no por unit tests directos.

---

## F

### [Fact]
**Categoría:** xUnit
**Definición:** Atributo de xUnit que marca un método como un test con un único escenario de datos fijos. El método no recibe parámetros. Cada `[Fact]` es ejecutado exactamente una vez por el test runner.
**En el proyecto:** Se usa para los casos concretos de Handler: un escenario de éxito, uno de no encontrado, uno de conflicto. Cada `[Fact]` tiene nombre en formato `Método_WhenEscenario_ResultadoEsperado`.

### FK Constraints  -  Foreign Key Constraints
**Categoría:** Base de Datos
**Definición:** Restricciones de integridad referencial en la base de datos que garantizan que un campo que referencia otra tabla apunta a un registro existente. Previenen datos huérfanos e inconsistencias.
**En el proyecto:** `EF Core InMemory` no enforcea FK constraints, lo que hace que los integration tests con `InMemoryDatabase` sean poco confiables. Por eso el equipo usa PostgreSQL real con Testcontainers, donde las constraints sí se aplican.

### FluentAssertions
**Categoría:** Testing / NuGet
**Definición:** Librería de assertions alternativa a `Assert` de xUnit que ofrece una sintaxis más expresiva con método encadenado: `result.Should().Be(42)`. Popular en la comunidad .NET pero agrega una dependencia adicional.
**En el proyecto:** El back-template NO usa FluentAssertions. Se usa `Assert` nativo de xUnit para reducir dependencias y mantener consistencia. Esta decisión está documentada en los packages del proyecto de tests.

### FluentValidation
**Categoría:** .NET / Validación
**Definición:** Librería para definir reglas de validación de objetos de forma fluida mediante una sintaxis encadenada. Integra con el pipeline de ASP.NET Core para validar requests automáticamente antes de llegar al Handler.
**En el proyecto:** Los Validators de FluentValidation (`CreateContactRequestValidator`, etc.) son ideales para unit tests con `[Theory]` e `[InlineData]` porque son objetos puros sin dependencias externas. El objetivo de coverage para validators es 90-100%.

---

## G

### GetResult()
**Categoría:** NetArchTest
**Definición:** Método de NetArchTest que ejecuta la regla de arquitectura configurada y retorna un objeto `TestResult` con las propiedades `IsSuccessful` (booleano) y `FailingTypes` (lista de tipos que violaron la regla).
**En el proyecto:** Siempre se invoca al final de la cadena de NetArchTest: `Types.InAssembly(...).ShouldNot().HaveDependencyOn(...).GetResult()`, cuyo resultado se pasa a `Assert.True(result.IsSuccessful)`.

### Green (TDD)
**Categoría:** TDD
**Definición:** Segunda fase del ciclo TDD. El objetivo es escribir el mínimo de código de producción que haga pasar el test que estaba en rojo. No se busca elegancia ni cobertura de casos adicionales  -  solo que el test compile y pase.
**En el proyecto:** Al implementar `AssignConversationHandler`, el código mínimo para pasar el primer test (happy path) se escribe antes de pensar en los casos de error. Los casos de error se añaden en tests posteriores.

### Guid
**Categoría:** .NET
**Definición:** Identificador único global de 128 bits. En .NET se representa con la estructura `System.Guid`. Se usa como identificador público de entidades para evitar exponer IDs secuenciales de base de datos en las APIs.
**En el proyecto:** Todas las entidades del sistema de mensajería tienen `PublicId` de tipo `Guid`. En los tests se genera con `Guid.NewGuid()` para crear identificadores únicos por escenario y con `Arg.Any<Guid>()` para matchers genéricos.

---

## H

### Handler
**Categoría:** CQRS / Application
**Definición:** Clase que implementa `IRequestHandler<TRequest, TResponse>` y contiene la lógica de negocio para procesar un caso de uso específico. Es orquestado por MediatR y recibe sus dependencias por inyección de constructor.
**En el proyecto:** Los Handlers son el objetivo principal de los unit tests. Ejemplos: `GetUserProfileHandler`, `LoginHandler`, `CreateContactHandler`, `AssignConversationHandler`. Cada Handler tiene su clase de tests correspondiente.

### HaveDependencyOn()
**Categoría:** NetArchTest
**Definición:** Método de NetArchTest que filtra los tipos que tienen una dependencia (directa o transitiva) sobre un namespace específico. Se combina con `ShouldNot()` para crear reglas de arquitectura negativas.
**En el proyecto:** Se usa en todos los tests de arquitectura: `ShouldNot().HaveDependencyOn("Users.Infrastructure")` verifica que ningún tipo en `Application` importa algo del namespace de `Infrastructure`.

### HttpClient
**Categoría:** .NET / Integration Testing
**Definición:** Clase de `System.Net.Http` que envía requests HTTP y recibe responses. En integration tests se obtiene de `WebApplicationFactory.CreateClient()` y apunta a la API en memoria, sin realizar llamadas de red reales.
**En el proyecto:** `_client = factory.CreateClient()` en `UserProfileApiTests` crea un cliente HTTP que habla directamente con la API levantada en memoria por `WebApplicationFactory`. Se usa para probar endpoints completos.

### HttpStatusCode
**Categoría:** .NET / HTTP
**Definición:** Enumeración de `System.Net` con los códigos de estado HTTP estándar (200 OK, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict, etc.). Se usa en integration tests para verificar el código de respuesta del endpoint.
**En el proyecto:** `Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)` verifica que un endpoint protegido rechaza requests sin token. `Assert.Equal(HttpStatusCode.OK, response.StatusCode)` confirma el happy path autenticado.

---

## I

### IAsyncLifetime
**Categoría:** xUnit
**Definición:** Interfaz de xUnit con métodos `InitializeAsync()` y `DisposeAsync()` que permiten configurar y limpiar recursos asíncronos en fixtures de tests. El runner llama a `InitializeAsync()` antes del primer test y `DisposeAsync()` después del último.
**En el proyecto:** `WebAppFixture` implementa `IAsyncLifetime` para poder inicializar recursos asíncronos como contenedores Docker de PostgreSQL (Testcontainers) antes de ejecutar los integration tests.

### IClassFixture\<T\>
**Categoría:** xUnit
**Definición:** Interfaz de xUnit que indica que una clase de tests comparte una única instancia del fixture `T` entre todos sus tests. La fixture se crea una vez y se destruye cuando todos los tests de la clase terminan.
**En el proyecto:** `UserProfileApiTests : IClassFixture<WebAppFixture>` garantiza que el servidor en memoria se levanta una sola vez para todos los tests de la clase, mejorando el rendimiento sin necesidad de reiniciar la API entre tests.

### IConflictFailure
**Categoría:** Result Pattern / Common
**Definición:** Interfaz del Result Pattern que marca una respuesta de fallo por conflicto de datos (por ejemplo, un email que ya existe). Implementada en el namespace `Common/Results`. Se traduce típicamente a HTTP 409 Conflict en el Presenter.
**En el proyecto:** `CreateContactConflictFailure` implementa `IConflictFailure`. En `CreateContactHandlerTests`, `Assert.IsType<CreateContactConflictFailure>(result)` verifica que el Handler retorna este tipo cuando `EmailExistsAsync` retorna `true`.

### IFailure
**Categoría:** Result Pattern / Common
**Definición:** Interfaz base del Result Pattern que marca una respuesta como fallo. Las interfaces específicas (`INotFoundFailure`, `IConflictFailure`, `IValidationFailure`) la extienden para representar categorías de error distintas que el Presenter mapea a códigos HTTP.
**En el proyecto:** El Presenter usa pattern matching sobre `IFailure` y sus subinterfaces para retornar el código HTTP apropiado. Los tests verifican el tipo concreto, no la interfaz base.

### INotFoundFailure
**Categoría:** Result Pattern / Common
**Definición:** Interfaz del Result Pattern que marca una respuesta de fallo por recurso no encontrado. Implementada en `Common/Results`. El Presenter la mapea a HTTP 404 Not Found.
**En el proyecto:** `GetUserProfileNotFoundFailure` y `AssignConversationNotFoundFailure` implementan esta interfaz. Los tests verifican el tipo concreto (`Assert.IsType<GetUserProfileNotFoundFailure>`) en lugar de la interfaz para mantener acceso a las propiedades específicas.

### IRequest\<TResponse\>
**Categoría:** MediatR / CQRS
**Definición:** Interfaz de MediatR que marca un record como un mensaje de comando o query. El tipo genérico `TResponse` define qué tipo de respuesta produce el Handler asociado.
**En el proyecto:** Todos los Request records implementan `IRequest<TResponse>`: `GetUserProfileRequest`, `LoginRequest`, `AssignConversationRequest`. En los tests, el Handler se instancia manualmente y se invoca su método `Handle()` directamente.

### IRequestHandler\<TRequest, TResponse\>
**Categoría:** MediatR / CQRS
**Definición:** Interfaz de MediatR que debe implementar todo Handler. Define el método `Handle(TRequest request, CancellationToken ct)` que contiene la lógica de negocio. MediatR resuelve el Handler correcto para cada tipo de Request.
**En el proyecto:** Los Handlers se prueban directamente instanciándolos con `new GetUserProfileHandler(repo)` sin pasar por MediatR. Los integration tests verifican que MediatR está configurado correctamente en el contenedor de DI.

### IResponse
**Categoría:** Result Pattern / Common
**Definición:** Interfaz marcador base del Result Pattern de la que heredan todos los records de respuesta de los Handlers. Permite que el Presenter acepte cualquier tipo de respuesta y aplique pattern matching para determinar el código HTTP.
**En el proyecto:** `GetUserProfileResponse`, `LoginResponse`, `AssignConversationResponse` son records abstractos que implementan `IResponse`. En los tests se trabaja con los tipos concretos (`GetUserProfileSuccess`, `LoginSuccess`) para acceder a sus datos.

### ISuccess\<T\>
**Categoría:** Result Pattern / Common
**Definición:** Interfaz del Result Pattern que marca una respuesta como éxito y expone la propiedad `Data` del tipo `T` con el DTO resultante. El Presenter la mapea a HTTP 200 OK y serializa `Data` como body JSON.
**En el proyecto:** `GetUserProfileSuccess(UserProfileDto Data)` implementa `ISuccess<UserProfileDto>`. En tests: `var success = Assert.IsType<GetUserProfileSuccess>(result); Assert.Equal("John Doe", success.Data.FullName)`.

### IUserCredentialRepository
**Categoría:** Domain / Repository
**Definición:** Contrato (interfaz) en el dominio del módulo Authentication que define las operaciones de persistencia para las credenciales de usuario. La implementación concreta vive en Infrastructure. Los tests mockean la interfaz, no la implementación.
**En el proyecto:** En `LoginHandlerTests` se crea con `Substitute.For<IUserCredentialRepository>()` y se configura con `Returns()` para simular usuarios encontrados o no encontrados sin tocar la base de datos.

### IL  -  Intermediate Language
**Categoría:** .NET
**Definición:** Lenguaje intermedio al que compila el código C# (y otros lenguajes .NET) antes de ser convertido a código máquina por el JIT. Los ensamblados `.dll` contienen IL que NetArchTest inspecciona para detectar dependencias.
**En el proyecto:** NetArchTest analiza el IL del ensamblado compilado para determinar si un tipo tiene referencias a otro namespace. Por eso los tests de arquitectura deben ejecutarse con `dotnet test` y no pueden ser verificados solo leyendo el código fuente.

### InlineData
**Categoría:** xUnit
**Definición:** Atributo que se aplica junto con `[Theory]` para proporcionar un conjunto de argumentos concretos. Se puede aplicar múltiples veces al mismo método de test; xUnit ejecuta el método una vez por cada `[InlineData]`.
**En el proyecto:** Se usa para probar todos los valores inválidos de un campo en un solo método de test. Por ejemplo, tres `[InlineData]` con `""`, `"   "` y `null` verifican que el Validator rechaza todas las formas de nombre vacío.

### Integration Test
**Categoría:** Testing
**Definición:** Test que verifica múltiples componentes del sistema trabajando juntos, incluyendo el pipeline HTTP completo, el contenedor de DI, los Handlers, los repositorios y la base de datos real. Más lento que los unit tests pero verifica cosas que estos no pueden.
**En el proyecto:** Representan el 20% de la pirámide. Usan `WebApplicationFactory` para levantar la API en memoria y PostgreSQL real vía Testcontainers. Verifican routing, DI, migraciones, constraints y serialización JSON.

### IPasswordHasher
**Categoría:** Authentication / Domain
**Definición:** Contrato que abstrae el algoritmo de hashing de contraseñas. Define los métodos `Hash(plainText)` y `Verify(plainText, hash)`. La implementación concreta usa BCrypt pero los tests mockean la interfaz.
**En el proyecto:** En `LoginHandlerTests`, `_hasher.Verify("password", "hash").Returns(true)` simula que la contraseña es correcta, y `.Returns(false)` simula que es incorrecta, sin ejecutar el algoritmo BCrypt real.

---

## J

### IJwtTokenService
**Categoría:** Authentication / Application
**Definición:** Contrato que abstrae la generación y validación de tokens JWT. Define métodos como `GenerateAccessToken`, `GenerateRefreshToken` y `GetRefreshTokenExpiry`. Permite mockear la generación de tokens en tests.
**En el proyecto:** En `LoginHandlerTests`, se configura `_jwt.GenerateAccessToken(...).Returns("access-token")` para verificar que el Handler retorna el token en la respuesta sin necesidad de generar un JWT real con firma criptográfica.

### JwtSecurityToken
**Categoría:** JWT / Seguridad
**Definición:** Clase de `System.IdentityModel.Tokens.Jwt` que representa un token JWT con sus claims, issuer, audience, fecha de expiración y credenciales de firma. Se serializa a la cadena compacta `header.payload.signature`.
**En el proyecto:** `JwtTestHelper` crea instancias de `JwtSecurityToken` con claims de prueba (userId, tenantId, email, role) para generar tokens válidos que los integration tests envían en el header `Authorization`.

### JwtTestHelper
**Categoría:** Integration Testing / Helpers
**Definición:** Clase estática utilitaria que genera tokens JWT firmados con la clave de prueba configurada en `WebAppFixture`. Permite crear tokens con distintos roles, userId y tenantId sin pasar por el sistema de autenticación completo.
**En el proyecto:** Vive en `Tests/Helpers/JwtTestHelper.cs`. Los integration tests llaman a `JwtTestHelper.GenerateToken(userId: agentId, role: "agent")` para construir el header `Authorization: Bearer <token>`.

---

## M

### MediatR
**Categoría:** CQRS / .NET
**Definición:** Librería que implementa el patrón Mediator en .NET. Desacopla quien emite un Request (Minimal API Endpoint) de quien lo procesa (Handler). Resuelve el Handler correcto mediante el contenedor de DI.
**En el proyecto:** Los unit tests instancian los Handlers directamente con `new Handler(mock)` sin usar MediatR. Los integration tests verifican que MediatR resuelve el Handler correcto cuando se hace un request HTTP real al endpoint.

### [MemberData]
**Categoría:** xUnit
**Definición:** Atributo que junto con `[Theory]` permite proporcionar datasets complejos (con múltiples columnas) desde un método estático o propiedad que retorna `IEnumerable<object[]>`. Cada elemento de la enumeración es un conjunto de argumentos para una ejecución del test.
**En el proyecto:** Se usa cuando los datos de prueba son más complejos que simples primitivos, como validar distintos teléfonos inválidos con su mensaje de error esperado: `yield return new object[] { "123", "Mínimo 10 dígitos." }`.

### Microsoft.NET.Test.Sdk
**Categoría:** Testing / NuGet
**Definición:** Paquete NuGet que provee la infraestructura necesaria para que el CLI de .NET (`dotnet test`) descubra y ejecute los tests. Es un prerequisito para cualquier proyecto de tests en .NET.
**En el proyecto:** Incluido en todos los archivos `.csproj` de tests junto con `xunit` y `xunit.runner.visualstudio`. Sin este paquete, `dotnet test` no puede descubrir los tests del proyecto.

### Mock
**Categoría:** Testing
**Definición:** Objeto que simula el comportamiento de una dependencia real para propósitos de testing. Permite controlar qué valores retorna, verificar que fue llamado y aislar la clase bajo prueba de sus dependencias externas.
**En el proyecto:** NSubstitute es la librería de mocking usada. Los repositorios (`IContactRepository`, `IConversationRepository`) y servicios (`IJwtTokenService`, `IPasswordHasher`) se mockean para que los unit tests de Handlers sean rápidos y determinísticos.

---

## N

### NetArchTest
**Categoría:** Testing / NuGet
**Definición:** Librería para escribir tests de arquitectura en .NET. Provee una API fluida para definir reglas sobre las dependencias entre ensamblados y tipos. Analiza el IL compilado, no el código fuente.
**En el proyecto:** Cada módulo del sistema de mensajería tiene su `*ArchitectureTests.cs`. La API se usa así: `Types.InAssembly(assembly).ShouldNot().HaveDependencyOn(namespace).GetResult()`.

### Namespace
**Categoría:** .NET / Arquitectura
**Definición:** Espacio de nombres que organiza y agrupa tipos relacionados en C#. En NetArchTest, los namespaces son el mecanismo para identificar qué capa o módulo se está referenciando al definir reglas de dependencia.
**En el proyecto:** Las constantes de namespace en los tests de arquitectura (`"Users.Application"`, `"Users.Infrastructure"`, `"Users.Presentation"`) identifican las capas cuyas dependencias se verifican.

### NSubstitute
**Categoría:** Testing / NuGet
**Definición:** Librería de mocking para .NET con una API simple y sin necesidad de configurar setups complejos. Permite crear sustitutos de interfaces, configurar valores de retorno con `Returns()` y verificar llamadas con `Received()` y `DidNotReceive()`.
**En el proyecto:** Librería estándar de mocking en el back-template. Se usa exclusivamente para mockear interfaces de repositorios y servicios en unit tests. La sintaxis es: `var mock = Substitute.For<IInterfaz>(); mock.Metodo().Returns(valor)`.

---

## P

### PasswordHash
**Categoría:** Seguridad / Authentication
**Definición:** Representación derivada e irreversible de una contraseña en texto plano. Se almacena en la base de datos en lugar de la contraseña original. La verificación compara el hash almacenado con el hash del password ingresado.
**En el proyecto:** En `LoginHandlerTests`, `credential.PasswordHash = "hash"` es el valor almacenado en la entidad mockeada. El mock de `IPasswordHasher.Verify("password", "hash")` retorna `true` o `false` según el escenario del test.

### Pirámide de Tests
**Categoría:** Testing
**Definición:** Modelo que describe la distribución óptima de tipos de tests: muchos unit tests (base), pocos integration tests (medio) y muy pocos E2E tests (cima). La base es más rápida y barata; la cima es más lenta y frágil.
**En el proyecto:** La distribución recomendada para el sistema de mensajería es 70% unit tests, 20% integration tests y 10% E2E (Playwright). Los unit tests cubren lógica de Handlers y Validators; los integration tests verifican el stack HTTP completo.

### Playwright
**Categoría:** Testing / E2E
**Definición:** Framework de testing E2E de Microsoft que automatiza browsers (Chromium, Firefox, WebKit). Permite simular acciones de usuario reales como clicks, formularios y navegación, y verificar el estado de la UI.
**En el proyecto:** Herramienta recomendada para el 10% de tests E2E del sistema de mensajería. Cubriría flujos completos: login de agente, apertura de conversación, envío de mensaje y verificación en la bandeja de entrada.

### Program.cs
**Categoría:** .NET / Bootstrap
**Definición:** Archivo de punto de entrada de la aplicación ASP.NET Core donde se configura el host, los servicios de DI y el pipeline de middleware. En .NET 6+ usa top-level statements sin clase explícita.
**En el proyecto:** Se excluye del reporte de coverage porque no contiene lógica de negocio. La covertura objetivo para bootstrapping es 0-10%. Los integration tests verifican indirectamente que la configuración es correcta.

### PublicId
**Categoría:** Domain / DDD
**Definición:** Identificador de tipo `Guid` que se expone en las APIs externas. Separa el identificador interno de base de datos (`long Id`) del identificador público para evitar exponer IDs secuenciales a los clientes.
**En el proyecto:** Todas las entidades del sistema de mensajería (`UserProfile`, `UserCredential`, `Contact`, `Conversation`) tienen `PublicId` de tipo `Guid`. Los tests usan `Guid.NewGuid()` para crear instancias con identificadores únicos por escenario.

---

## R

### Received(N)
**Categoría:** NSubstitute
**Definición:** Verificación que confirma que un método del sustituto fue llamado exactamente N veces. Si el conteo no coincide, NSubstitute lanza una excepción con el conteo real vs el esperado.
**En el proyecto:** `await _repo.Received(1).InsertAsync(...)` en `CreateContactHandlerTests` verifica que el Handler insertó exactamente un registro cuando el email no existía previamente. Nunca debe llamarse dos veces por request.

### Red (TDD)
**Categoría:** TDD
**Definición:** Primera fase del ciclo TDD. Se escribe un test para un comportamiento que aún no existe. El test debe fallar  -  puede no compilar, lo cual es válido y esperado. El test describe el contrato que el código de producción deberá cumplir.
**En el proyecto:** Al crear `AssignConversationHandler`, el test se escribe primero referenciando `AssignConversationHandler` y `AssignConversationRequest` que no existen. El error de compilación es el "rojo" esperado.

### Refactor (TDD)
**Categoría:** TDD
**Definición:** Tercera fase del ciclo TDD. Con todos los tests en verde, se mejora el código de producción sin cambiar su comportamiento: se eliminan duplicaciones, se extraen métodos, se mejoran nombres. Los tests son el cinturón de seguridad que confirma que el comportamiento no cambió.
**En el proyecto:** En el ejemplo del módulo de Conversaciones, el refactor extrae el método privado `NotFound()` para eliminar la instanciación inline de `AssignConversationNotFoundFailure` y hacer el código más legible.

### Regresión
**Categoría:** Testing
**Definición:** Bug que reaparece en código que anteriormente funcionaba correctamente, generalmente como consecuencia de un cambio en otra parte del sistema. Los tests automatizados son la principal defensa contra regresiones.
**En el proyecto:** Si se arregla un bug en la lógica de asignación de conversaciones y se escribe un test para reproducirlo, ese test garantiza que el bug no vuelva a aparecer en refactors o cambios futuros del Handler.

### Result Pattern
**Categoría:** Arquitectura / Domain
**Definición:** Patrón que retorna un objeto discriminado (union type) en lugar de lanzar excepciones para representar errores de negocio esperados. Los Handlers retornan un tipo abstracto con variantes concretas para éxito y cada tipo de fallo.
**En el proyecto:** Implementado con records abstractos (`GetUserProfileResponse`) y sealed records concretos (`GetUserProfileSuccess`, `GetUserProfileNotFoundFailure`). Los tests verifican el tipo exacto del resultado con `Assert.IsType<T>`.

### Returns()
**Categoría:** NSubstitute
**Definición:** Método de NSubstitute que configura el valor que retornará un método del sustituto cuando sea llamado. Se encadena después de la invocación del método: `mock.Metodo(args).Returns(valor)`.
**En el proyecto:** Ejemplos: `_repo.GetByPublicIdAsync(id, 1, ...).Returns(profile)` para simular que se encontró el perfil; `.Returns((UserProfile?)null)` para simular que no existe.

---

## S

### ServiceCollectionExtensions
**Categoría:** .NET / DI
**Definición:** Clases estáticas con métodos de extensión sobre `IServiceCollection` que registran grupos de servicios relacionados. Siguen el patrón `AddXxx()` para organizar el registro de DI por módulo.
**En el proyecto:** Se excluyen del reporte de coverage con `[ExcludeFromCodeCoverage]`. Su correcto funcionamiento se verifica indirectamente mediante integration tests que levantan el contenedor de DI completo.

### Substitute.For\<T\>()
**Categoría:** NSubstitute
**Definición:** Método estático de NSubstitute que crea un sustituto (mock) de la interfaz o clase abstracta `T`. El sustituto implementa todos los miembros de `T` con comportamiento por defecto (retorna `null`, `0`, `false` o `Task.CompletedTask`) hasta que se configura con `Returns()`.
**En el proyecto:** Patrón de inicio de todos los unit tests: `private readonly IUserProfileRepository _repo = Substitute.For<IUserProfileRepository>()`. Se declara como campo de la clase de tests para compartirlo entre todos los `[Fact]`.

---

## T

### TDD  -  Test-Driven Development
**Categoría:** Testing / Metodología
**Definición:** Metodología de desarrollo en la que los tests se escriben antes del código de producción. El ciclo es: Red (test que falla) -> Green (código mínimo que pasa el test) -> Refactor (mejorar sin romper). El ciclo es corto: un test a la vez.
**En el proyecto:** TDD agrega más valor en lógica de Handlers y Validators, y al reproducir bugs de producción. Es menos útil para código de infraestructura (SQL, HTTP) que requiere recursos externos. Se practica desde la fase IV del programa.

### TenantId
**Categoría:** Multi-Tenancy / Domain
**Definición:** Identificador del workspace o tenant al que pertenece un recurso. Se incluye en las operaciones de base de datos para garantizar aislamiento de datos entre clientes. En el proyecto se usa como `long`.
**En el proyecto:** Presente en todas las entidades y requests del sistema de mensajería. En los tests se pasa como literal `1L` o `Arg.Any<long>()` según si el escenario requiere un tenant específico o cualquiera.

### Testcontainers
**Categoría:** Integration Testing / Docker
**Definición:** Librería que permite levantar contenedores Docker desde código de tests .NET. Se usa para crear bases de datos reales (PostgreSQL, SQL Server, Redis) que se inicializan antes de los tests y se destruyen al terminar.
**En el proyecto:** Recomendado para los integration tests que necesitan PostgreSQL real. La alternativa de usar `InMemoryDatabase` se descarta porque no enforcea FK constraints, no soporta transacciones reales y no detecta bugs específicos de PostgreSQL.

### [Theory]
**Categoría:** xUnit
**Definición:** Atributo de xUnit que marca un método de test como paramétrico: recibe datos externos y se ejecuta una vez por cada conjunto de datos. Requiere al menos un atributo de datos (`[InlineData]`, `[MemberData]` o `[ClassData]`).
**En el proyecto:** Ideal para Validators de FluentValidation: un solo método de test verifica que el Validator rechaza múltiples inputs inválidos (string vacío, espacios, null) con `[InlineData("")]`, `[InlineData("   ")]`, `[InlineData(null)]`.

### Types.InAssembly()
**Categoría:** NetArchTest
**Definición:** Punto de entrada de la API de NetArchTest que selecciona todos los tipos del ensamblado especificado para aplicarles reglas de arquitectura. Se encadena con filtros (`That()`, `ShouldNot()`) y termina en `GetResult()`.
**En el proyecto:** Todos los tests de arquitectura comienzan con `Types.InAssembly(DomainAssembly)` o `Types.InAssembly(ApplicationAssembly)` para seleccionar el ensamblado cuyas dependencias se van a verificar.

---

## U

### Unit Test
**Categoría:** Testing
**Definición:** Test que verifica una sola clase o función en aislamiento total de sus dependencias externas. Las dependencias se reemplazan por mocks. Son rápidos (menos de 1 ms), determinísticos y no requieren infraestructura externa.
**En el proyecto:** Representan el 70% de la pirámide. Cubren Handlers, Validators y lógica de dominio. Cada Handler tiene su clase de tests con un `[Fact]` por escenario: éxito, no encontrado, conflicto, credenciales inválidas.

### UseEnvironment()
**Categoría:** ASP.NET Core / Testing
**Definición:** Método de `IWebHostBuilder` que establece el nombre del ambiente de la aplicación. En tests se usa `"Testing"` para que la aplicación cargue configuración específica de tests y evite registrar servicios de producción.
**En el proyecto:** `builder.UseEnvironment("Testing")` en `WebAppFixture.ConfigureWebHost()` activa la configuración de tests. Permite usar cadenas de conexión de Testcontainers y claves JWT de prueba en lugar de las de producción.

---

## V

### Value Object
**Categoría:** DDD / Domain
**Definición:** Objeto de dominio sin identidad propia que se define por sus atributos. Es inmutable y se compara por valor, no por referencia. Ejemplos: Email, PhoneNumber, Money.
**En el proyecto:** Los Value Objects del dominio son candidatos ideales para unit tests porque son objetos puros sin dependencias externas. El objetivo de coverage para domain entities y Value Objects es 80-90%.

### Validator
**Categoría:** FluentValidation / Application
**Definición:** Clase que hereda de `AbstractValidator<T>` y define reglas de validación para un tipo de request. Se ejecuta antes de que el Handler procese la request, normalmente mediante un Pipeline Behavior.
**En el proyecto:** `CreateContactRequestValidator` es el ejemplo del módulo de Contactos. Sus tests usan `[Theory]` con `[InlineData]` para verificar que rechaza nombres vacíos, emails inválidos y teléfonos con formato incorrecto.

---

## W

### WebApplicationFactory\<TProgram\>
**Categoría:** Integration Testing / ASP.NET Core
**Definición:** Clase de `Microsoft.AspNetCore.Mvc.Testing` que levanta la aplicación ASP.NET Core en memoria para tests de integración, sin abrir un puerto de red real. `TProgram` es el tipo del punto de entrada de la aplicación.
**En el proyecto:** `WebAppFixture : WebApplicationFactory<Program>` es la fixture compartida entre los integration tests. Permite sobrescribir configuración (`ConfigureWebHost`) para usar JWT de prueba y base de datos de Testcontainers.

---

## X

### xUnit
**Categoría:** Testing / NuGet
**Definición:** Framework de testing para .NET sucesor de NUnit y MSTest. Soporta `[Fact]` para tests unitarios y `[Theory]` para tests paramétricos. Integra con `dotnet test`, Visual Studio y Rider. Es el framework estándar elegido por el back-template.
**En el proyecto:** Declarado como `<PackageReference Include="xunit" Version="2.9.3" />` en cada `.csproj` de tests. Junto con `xunit.runner.visualstudio` y `Microsoft.NET.Test.Sdk` forma el stack mínimo para ejecutar tests con `dotnet test`.

### xunit.runner.visualstudio
**Categoría:** Testing / NuGet
**Definición:** Adaptador que permite a Visual Studio y al CLI de .NET (`dotnet test`) descubrir y ejecutar tests escritos con xUnit. Es el puente entre el runner de tests del IDE y el framework xUnit.
**En el proyecto:** Declarado en todos los `.csproj` de tests. Sin este paquete, Visual Studio no muestra los tests en el Test Explorer y `dotnet test` no puede ejecutarlos correctamente.

### XPlat Code Coverage
**Categoría:** Testing / Coverage
**Definición:** Colector de cobertura multiplataforma que se integra con el SDK de .NET para recolectar datos de cobertura durante la ejecución de tests. Es el colector recomendado para proyectos .NET modernos, reemplazando al colector de Windows-only.
**En el proyecto:** Se activa con `dotnet test --collect:"XPlat Code Coverage"`. Genera un archivo `coverage.cobertura.xml` que puede visualizarse con la extensión Coverage Gutters en VS Code o convertirse a HTML con `reportgenerator`.
