# Glosario Técnico  -  Result Pattern  -  Pipeline  -  Validación

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### AbstractValidator\<T\>
**Categoría:** Validación / FluentValidation
**Definición:** Clase base de la librería FluentValidation que se hereda para definir reglas de validación sobre un tipo `T`. Las reglas se declaran en el constructor usando `RuleFor`, `RuleForEach`, `When` y `Must`. El framework las ejecuta automáticamente al validar una instancia del tipo.
**En el proyecto:** Cada Request del sistema de mensajería tiene su propio validator, por ejemplo `RegisterAgentRequestValidator : AbstractValidator<RegisterAgentRequest>`, que valida nombre, email y contraseña antes de que el Handler procese el registro de un agente.

### Activity.Current
**Categoría:** Observabilidad / ASP.NET Core
**Definición:** Propiedad estática de `System.Diagnostics.Activity` que expone la actividad de rastreo activa en el hilo actual. Forma parte del sistema de tracing distribuido de .NET y es compatible con OpenTelemetry. Su `Id` se usa para correlacionar una request con sus logs y spans.
**En el proyecto:** El `GlobalExceptionHandler` y el middleware de `ProblemDetails` del sistema de mensajería lo usan para incluir el campo `traceId` en cada respuesta de error, permitiendo rastrear fallos en los logs de producción.

### AddExceptionHandler\<T\>
**Categoría:** ASP.NET Core / Configuración
**Definición:** Método de extensión que registra en el contenedor de DI una implementación de `IExceptionHandler`. ASP.NET Core invoca los handlers registrados en orden cuando una excepción no manejada atraviesa el middleware de `UseExceptionHandler`.
**En el proyecto:** En `Program.cs` del sistema de mensajería se llama `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` para que los errores de validación produzcan una respuesta `ValidationProblemDetails` en lugar de un HTML de error genérico.

### AddProblemDetails
**Categoría:** ASP.NET Core / Configuración
**Definición:** Método de extensión que configura el servicio de Problem Details en la aplicación. Acepta un delegado `CustomizeProblemDetails` para enriquecer o limpiar los detalles del error según el ambiente (desarrollo vs. producción).
**En el proyecto:** En el `Program.cs` del sistema de mensajería se usa para agregar el `traceId` a todas las respuestas de error y para suprimir el campo `Detail` en producción, evitando fugas de información interna.

### AddValidatorsFromAssembly
**Categoría:** Validación / FluentValidation / DI
**Definición:** Método de extensión de FluentValidation que escanea un ensamblado y registra automáticamente en el contenedor de DI todas las clases que heredan de `AbstractValidator<T>`. Elimina la necesidad de registrar cada validator manualmente.
**En el proyecto:** Se invoca en `AddApplicationServices` del sistema de mensajería para detectar y registrar todos los validators del ensamblado de Application en un solo paso.

### ApiController (atributo)
**Categoría:** ASP.NET Core / Web API
**Definición:** Atributo que activa comportamientos convencionales en un controlador: binding automático desde el body con `[FromBody]`, respuestas 400 automáticas ante model binding inválido (cuando se usa el sistema de model validation nativo) y validación de rutas. Se aplica a nivel de clase.
**En el proyecto:** Todos los controllers del sistema de mensajería (`AgentsController`, `ContactsController`, etc.) llevan `[ApiController]` y heredan de `ControllerBase` para no incluir vistas.

---

## B

### BadRequest (método del Controller)
**Categoría:** ASP.NET Core / HTTP
**Definición:** Método de `ControllerBase` que devuelve una respuesta HTTP 400 con un cuerpo opcional. Se usa para indicar al cliente que la request fue sintácticamente válida pero no pudo procesarse por errores en los datos enviados.
**En el proyecto:** El Controller del sistema de mensajería retorna `BadRequest(viewModel)` cuando `_viewModel.IsSuccess` es `false`, lo que ocurre al recibir del Presenter un fallo de validación, conflicto u otro error de negocio.

### BusinessRuleException
**Categoría:** Excepciones / Dominio
**Definición:** Excepción personalizada del back-template que representa la violación de una regla de negocio invariante que no puede representarse como un `Result` (por ejemplo, estados del sistema que nunca deberían ocurrir si el código es correcto). A diferencia de los errores de flujo normal, se registra con `LogError` y se propaga hacia arriba.
**En el proyecto:** El `InteractorPipeline` del sistema de mensajería captura `BusinessRuleException` de forma específica para registrarla con nivel `LogError` antes de volver a lanzarla, distinguiéndola de las excepciones inesperadas que se loguean con `LogCritical`.

---

## C

### CachingBehavior
**Categoría:** Pipeline / Rendimiento
**Definición:** Pipeline Behavior opcional que intercepta las Queries antes de ejecutar el Handler para consultar una caché (Redis, memoria en proceso, etc.). Si existe una entrada válida, la devuelve sin llegar al Handler; si no, ejecuta el Handler y almacena el resultado en caché.
**En el proyecto:** Aplicable en el sistema de mensajería a Queries frecuentes y costosas, como obtener la configuración de un canal o la lista de etiquetas de contactos, donde el resultado puede servirse desde Redis en lugar de ejecutar una consulta SQL en cada request.

### CancellationToken
**Categoría:** C# / Asincronismo
**Definición:** Estructura de .NET que permite cancelar operaciones asíncronas en curso. Se pasa como último parámetro en los métodos `Handle` de Handlers, Presenters y Behaviors. Cuando el cliente cierra la conexión, ASP.NET Core cancela el token y las operaciones asíncronas se interrumpen.
**En el proyecto:** Todos los Handlers del sistema de mensajería reciben `CancellationToken ct` y lo propagan a las llamadas al repositorio (`GetByIdAsync(id, ct)`, `InsertAsync(agent, ct)`), evitando queries inútiles cuando el cliente ya desconectó.

### Command (marker interface)
**Categoría:** CQRS / Mensajería
**Definición:** Interfaz marcadora que distingue los mensajes de escritura (Commands) de los de lectura (Queries) en la capa de Application. Permite que ciertos Pipeline Behaviors, como `TransactionBehavior`, se apliquen selectivamente solo a Commands mediante la restricción genérica `where TRequest : ICommand<TResponse>`.
**En el proyecto:** En el sistema de mensajería los casos de uso que modifican estado (registrar agente, enviar mensaje, actualizar conversación) implementarán `ICommand<TResponse>`, mientras que los que solo leen implementarán `IQuery<TResponse>`.

### ConfigureAwait(false)
**Categoría:** C# / Asincronismo
**Definición:** Llamada que indica al runtime que la continuación del `await` no necesita capturar el contexto de sincronización original (por ejemplo, el contexto de ASP.NET Core). Mejora el rendimiento en aplicaciones de servidor al evitar la reentrada innecesaria al contexto.
**En el proyecto:** El `InteractorPipeline` del sistema de mensajería usa `await next().ConfigureAwait(false)` y `await Mediator.Publish(response).ConfigureAwait(false)` siguiendo la convención estándar de librerías de infraestructura.

### ConflictFailure (IConflictFailure)
**Categoría:** Result Pattern / Marcadores
**Definición:** Interfaz marcadora de fallo que representa un conflicto de datos, típicamente un recurso que ya existe (email duplicado, nombre de workspace ya tomado). El Presenter la detecta vía pattern matching para devolver HTTP 409 Conflict.
**En el proyecto:** En el sistema de mensajería, intentar registrar un agente con un email que ya existe devuelve `RegisterAgentEmailConflictFailure : IConflictFailure`, y el `RegisterAgentPresenter` mapea ese resultado a una respuesta 409.

### Controller (en el back-template)
**Categoría:** ASP.NET Core / Presentación
**Definición:** Clase delgada que recibe la HTTP request, construye el `Request` de dominio, llama a `mediator.Send()` y devuelve la respuesta usando el `ResultViewModel`. No contiene lógica de negocio ni pattern matching; su responsabilidad se limita al enlace HTTP -> dominio -> HTTP.
**En el proyecto:** `AgentsController`, `ContactsController` y `ConversationsController` del sistema de mensajería siguen este patrón: tres líneas de lógica real (construir request, enviar, devolver viewModel).

### Cross-cutting concern
**Categoría:** Arquitectura
**Definición:** Comportamiento que aplica transversalmente a múltiples casos de uso sin pertenecer a ninguno en particular: logging, validación, medición de rendimiento, manejo de transacciones, caché. Los Pipeline Behaviors son el mecanismo idóneo para implementarlos en el back-template.
**En el proyecto:** El logging de requests y responses, la validación con FluentValidation y el manejo de transacciones de base de datos son los cross-cutting concerns centrales del sistema de mensajería, cada uno encapsulado en su propio Pipeline Behavior.

---

## D

### Dependency Injection (DI)
**Categoría:** Arquitectura / .NET
**Definición:** Patrón en el que las dependencias de una clase se proporcionan desde el exterior (contenedor de IoC) en lugar de ser instanciadas por la propia clase. En .NET el contenedor nativo de `Microsoft.Extensions.DependencyInjection` gestiona el ciclo de vida (Singleton, Scoped, Transient) de cada servicio registrado.
**En el proyecto:** Todos los Handlers, Presenters, Behaviors y Repositories del sistema de mensajería se registran en el contenedor de DI. El Pipeline Behaviors se registra como `Scoped` usando el tipo genérico abierto `typeof(IPipelineBehavior<,>)`.

---

## E

### EmailAddress (regla FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Regla predefinida de FluentValidation que verifica que un campo de texto tenga formato de dirección de correo electrónico válida. Internamente usa el mismo validador de email de ASP.NET Core Data Annotations.
**En el proyecto:** El `RegisterAgentRequestValidator` del sistema de mensajería aplica `.EmailAddress()` al campo `Email` para rechazar entradas malformadas antes de llegar al Handler.

---

## F

### FailureResult
**Categoría:** Result Pattern / Common.Results
**Definición:** Clase concreta `sealed` que implementa `Result` e `IFailure`. Encapsula un `Exception` interno y expone su mensaje como `string Message`. Existen dos versiones: `FailureResult` (sin datos) y `FailureResult<T>` (para Responses genéricas). El método de fábrica `Result.Fail(...)` crea instancias de estas clases.
**En el proyecto:** Usado internamente en la jerarquía `Common.Results` del sistema de mensajería. En la práctica los Handlers retornan `sealed records` propios (como `GetContactNotFoundFailure`) que implementan `IFailure`, no `FailureResult` directamente.

### FluentValidation
**Categoría:** Validación / Librería externa
**Definición:** Librería .NET de código abierto que permite definir reglas de validación de forma fluida y expresiva usando un DSL orientado a métodos encadenados. Separa las reglas de validación de las clases de modelo y los handlers de negocio. Compatible con el contenedor de DI nativo y con Pipeline Behaviors.
**En el proyecto:** Librería de validación oficial del back-template del sistema de mensajería. Cada Request tiene un `AbstractValidator<TRequest>` asociado; el `ValidationBehavior` ejecuta todos los validators registrados antes de llamar al Handler.

### FromBody (atributo)
**Categoría:** ASP.NET Core / Binding
**Definición:** Atributo que indica a ASP.NET Core que el parámetro del action se debe deserializar desde el cuerpo de la HTTP request (JSON por defecto). Se aplica al parámetro del action method del Controller.
**En el proyecto:** Los actions del sistema de mensajería como `Register([FromBody] RegisterAgentBody body, ...)` usan este atributo para recibir el JSON del cliente y construir el Request antes de enviarlo por el mediator.

---

## G

### GetGenericArguments
**Categoría:** C# / Reflexión
**Definición:** Método de `System.Type` que devuelve los argumentos de tipo genérico de un tipo genérico construido. Se usa en tiempo de ejecución para obtener el nombre del tipo de datos encapsulado dentro de un genérico, por ejemplo `Result<ContactDto>` devolvería `[ContactDto]`.
**En el proyecto:** El `InteractorPipeline` del sistema de mensajería lo usa para extraer el nombre del tipo de Response al loguear un `IFailure`, produciendo mensajes como `GetContactNotFoundFailure: Contacto no encontrado` en lugar del nombre genérico del tipo padre.

### GlobalExceptionHandler
**Categoría:** ASP.NET Core / Manejo de errores
**Definición:** Clase que implementa `IExceptionHandler` y centraliza la conversión de excepciones no manejadas en respuestas Problem Details estructuradas. Se registra con `AddExceptionHandler<T>` y se activa vía `UseExceptionHandler()`. Cada implementación maneja un tipo específico de excepción y retorna `true` si la manejó.
**En el proyecto:** El `GlobalExceptionHandler` del sistema de mensajería captura `ValidationException` de FluentValidation y produce una respuesta `ValidationProblemDetails` con errores agrupados por campo. Las excepciones no reconocidas retornan `false` para que el siguiente handler las procese.

---

## H

### Handler (IRequestHandler / IInteractor)
**Categoría:** CQRS / Mensajería
**Definición:** Clase que contiene la lógica de negocio de un caso de uso específico. Implementa `IRequestHandler<TRequest, TResponse>` (o su alias del back-template `IInteractor<TRequest, TResponse>`). Recibe un `Request`, ejecuta la lógica y devuelve una `Response` tipada. No lanza excepciones de flujo normal; retorna tipos que representan cada resultado posible.
**En el proyecto:** `GetContactHandler`, `RegisterAgentHandler`, `SendMessageHandler` son los Handlers del sistema de mensajería. Cada uno retorna un `sealed record` de su jerarquía de Response propia.

### HttpContext
**Categoría:** ASP.NET Core
**Definición:** Objeto que encapsula toda la información de la HTTP request y response actuales: headers, ruta, query string, body, estado de autenticación, servicio de respuesta. Es accesible en middlewares, exception handlers y filtros.
**En el proyecto:** El `GlobalExceptionHandler` del sistema de mensajería accede a `httpContext.Request.Path` para rellenar el campo `instance` del Problem Details y a `httpContext.TraceIdentifier` como fallback del `traceId`.

---

## I

### IConflictFailure
**Categoría:** Result Pattern / Marcadores
**Definición:** Ver **ConflictFailure (IConflictFailure)**.

### IExceptionHandler
**Categoría:** ASP.NET Core
**Definición:** Interfaz de ASP.NET Core (introducida en .NET 8) que define el contrato `TryHandleAsync(HttpContext, Exception, CancellationToken)` para manejar excepciones no manejadas. Devuelve `true` si el handler procesó la excepción (cortocircuitando la cadena) o `false` para pasar al siguiente.
**En el proyecto:** `GlobalExceptionHandler` del sistema de mensajería implementa esta interfaz para interceptar `ValidationException` y producir respuestas estructuradas con Problem Details.

### IFailure
**Categoría:** Result Pattern / Marcadores
**Definición:** Interfaz marcadora base para todos los tipos de fallo en el sistema de Results. Expone una propiedad `string Message`. Las subinterfaces `IValidationFailure`, `INotFoundFailure` e `IConflictFailure` heredan de ella y permiten discriminar el tipo de fallo en el Presenter mediante pattern matching.
**En el proyecto:** El `InteractorPipeline` del sistema de mensajería comprueba `response is IFailure` para decidir si loguear con `LogWarning`. El Presenter comprueba subinterfaces más específicas para asignar el HTTP status code correcto.

### IInteractor\<TRequest, TResponse\>
**Categoría:** Mensajería / back-template
**Definición:** Interfaz del back-template que extiende `IRequestHandler<TRequest, TResponse>` con restricciones adicionales: `TRequest : IRequest<TResponse>` y `TResponse : IResponse`. Es la interfaz que todos los Handlers del proyecto deben implementar en lugar de `IRequestHandler` directamente, siguiendo la convención del template.
**En el proyecto:** `GetContactHandler : IInteractor<GetContactRequest, GetContactResponse>` es el patrón de declaración estándar en el sistema de mensajería.

### IMediator
**Categoría:** Mensajería / Mediator Pattern
**Definición:** Interfaz del mediator que expone los métodos `Send<TResponse>(IRequest<TResponse>, CancellationToken)` y `Publish(INotification, CancellationToken)`. El Controller usa `Send` para despachar Requests; el `InteractorPipeline` usa `Publish` para notificar al Presenter.
**En el proyecto:** Inyectado en todos los Controllers del sistema de mensajería para enviar Requests al pipeline y recibir el response una vez que el Presenter llenó el `ResultViewModel`.

### INotFoundFailure
**Categoría:** Result Pattern / Marcadores
**Definición:** Interfaz marcadora de fallo que representa que un recurso buscado no existe. El Presenter la detecta para devolver HTTP 404 Not Found. Hereda de `IFailure`.
**En el proyecto:** `GetContactNotFoundFailure : INotFoundFailure` es el tipo que retorna `GetContactHandler` cuando el contacto solicitado no existe en la base de datos del sistema de mensajería.

### INotificationHandler\<TNotification\>
**Categoría:** Mensajería / Mediator Pattern
**Definición:** Interfaz que define un receptor de notificaciones publicadas por `IMediator.Publish`. Puede haber múltiples handlers registrados para el mismo tipo de notificación; todos se ejecutan cuando se publica ese tipo. En el back-template los Presenters implementan esta interfaz.
**En el proyecto:** `GetContactPresenter : INotificationHandler<GetContactResponse>` es notificado por el `InteractorPipeline` del sistema de mensajería después de que el Handler retorna su Response.

### IPipelineBehavior\<TRequest, TResponse\>
**Categoría:** Pipeline / Mensajería
**Definición:** Interfaz que define un comportamiento que envuelve la ejecución de un Handler. Funciona como un decorador: recibe la `request` y un delegado `next` que apunta al siguiente eslabón de la cadena (otro behavior o el Handler final). Permite ejecutar código antes y después del Handler sin modificarlo.
**En el proyecto:** `ValidationBehavior` e `InteractorPipeline` del sistema de mensajería implementan esta interfaz. Se registran en el contenedor de DI con tipo genérico abierto para aplicar a todos los Handlers.

### IRequest\<TResponse\>
**Categoría:** Mensajería / CQRS
**Definición:** Interfaz marcadora que identifica un mensaje de comando o consulta que espera una respuesta del tipo `TResponse`. El mediator la usa para enrutar el mensaje al Handler correcto y construir el pipeline de behaviors correspondiente.
**En el proyecto:** Todos los Request del sistema de mensajería (`GetContactRequest`, `RegisterAgentRequest`, `SendMessageRequest`) implementan `IRequest<TResponse>` donde `TResponse` es el `abstract record` de su jerarquía de responses.

### IRequestHandler\<TRequest, TResponse\>
**Categoría:** Mensajería / CQRS
**Definición:** Interfaz base del patrón mediator que define el contrato `Handle(TRequest, CancellationToken) : Task<TResponse>`. En el back-template se usa a través de su alias `IInteractor<TRequest, TResponse>`.
**En el proyecto:** Ver **IInteractor**.

### IResponse
**Categoría:** Mensajería / Common.Messaging
**Definición:** Interfaz del `Common.Messaging` del back-template que hereda de `INotification`. Es el contrato que el `InteractorPipeline` necesita para hacer `Mediator.Publish(response)`. Todos los `abstract record Response` de los casos de uso la implementan.
**En el proyecto:** `GetContactResponse : IResponse`, `RegisterAgentResponse : IResponse`, etc. son las bases de las jerarquías de responses del sistema de mensajería.

### ISuccess\<T\>
**Categoría:** Result Pattern / Marcadores
**Definición:** Interfaz marcadora de éxito que expone la propiedad `T Data`. Hereda de `ISuccess` (sin datos). El Presenter la detecta en pattern matching para extraer el dato y pasarlo al `ResultViewModel`. El tipo `T` es el DTO que se serializará como response JSON.
**En el proyecto:** `GetContactSuccess : ISuccess<ContactDto>` lleva el ContactDto al Presenter del sistema de mensajería, que lo asigna al `ResultViewModel` para ser serializado como JSON por el Controller.

### IValidationFailure
**Categoría:** Result Pattern / Marcadores
**Definición:** Interfaz marcadora de fallo para errores de validación de negocio que ocurren dentro del Handler (distintos de los errores de formato que maneja FluentValidation). Hereda de `IFailure`. El Presenter la usa para devolver HTTP 400.
**En el proyecto:** `RegisterAgentValidationFailure : IValidationFailure` se usa en el sistema de mensajería para casos donde la validación semántica falla dentro del Handler, por ejemplo cuando el rol solicitado no es compatible con la configuración del workspace.

### IWebHostEnvironment
**Categoría:** ASP.NET Core / Configuración
**Definición:** Interfaz que provee información sobre el ambiente de ejecución de la aplicación: nombre del ambiente (`Development`, `Staging`, `Production`), ruta raíz del contenido, etc. Se usa para tomar decisiones de configuración que dependen del ambiente.
**En el proyecto:** El middleware de `ProblemDetails` del sistema de mensajería consulta `IsDevelopment()` para decidir si incluir el campo `Detail` en los errores  -  se muestra en desarrollo pero se suprime en producción.

---

## L

### LogCritical
**Categoría:** Logging / Serilog / Microsoft.Extensions.Logging
**Definición:** Nivel de log más alto en la jerarquía de Microsoft.Extensions.Logging (`Trace < Debug < Information < Warning < Error < Critical`). Se usa para errores que indican una falla catastrófica del sistema, como excepciones no esperadas que sugieren un bug en el código.
**En el proyecto:** El `InteractorPipeline` del sistema de mensajería registra con `LogCritical` cualquier excepción que no sea `BusinessRuleException`, lo que la distingue de los fallos esperados de negocio y facilita el triaje en los dashboards de observabilidad.

### LogInformation
**Categoría:** Logging / Serilog / Microsoft.Extensions.Logging
**Definición:** Nivel de log estándar para eventos de flujo normal: inicio de un proceso, operación completada con éxito, hito del ciclo de vida de la aplicación. Es el nivel más usado en condiciones normales de operación.
**En el proyecto:** El `InteractorPipeline` del sistema de mensajería emite `LogInformation` al inicio de cada Request (`{@Request}`) y al finalizar con éxito (`{@Response}`), generando una traza auditada de cada operación.

### LogWarning
**Categoría:** Logging / Serilog / Microsoft.Extensions.Logging
**Definición:** Nivel de log para situaciones anómalas pero esperadas: un recurso no encontrado, un conflicto de datos, una regla de negocio que falló por datos del usuario. No indica un bug, sino un resultado de negocio que requiere atención.
**En el proyecto:** El `InteractorPipeline` del sistema de mensajería registra con `LogWarning` cuando la Response implementa `IFailure`, incluyendo el tipo de Response y el mensaje, para facilitar la detección de patrones de error en producción.

---

## M

### Marker interface (interfaz marcadora)
**Categoría:** Diseño de software / C#
**Definición:** Interfaz sin miembros (o con miembros mínimos) que se usa exclusivamente para clasificar tipos mediante el sistema de tipos de C#. Permite el pattern matching (`is INotFoundFailure`), las restricciones genéricas (`where TRequest : ICommand`) y el enrutamiento de comportamientos en el pipeline.
**En el proyecto:** `ISuccess`, `IFailure`, `INotFoundFailure`, `IConflictFailure`, `IValidationFailure` y `IResponse` son las interfaces marcadoras del sistema de Results del sistema de mensajería.

### Matches (regla FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Regla que valida un campo de texto contra una expresión regular. Se usa para verificar que el valor cumple un patrón específico (presencia de mayúsculas, dígitos, caracteres especiales, etc.).
**En el proyecto:** El `RegisterAgentRequestValidator` del sistema de mensajería usa `.Matches("[A-Z]")` y `.Matches("[0-9]")` para garantizar que las contraseñas de agentes cumplan con la política de seguridad mínima.

### MaximumLength (regla FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Regla que verifica que la longitud de una cadena no supere el valor indicado. Complementa `MinimumLength` para definir rangos de longitud válidos en campos de texto.
**En el proyecto:** Usado en el sistema de mensajería para limitar el campo `FullName` de un agente a 100 caracteres y `Email` a 200 caracteres, coherentes con las restricciones de las columnas de la base de datos PostgreSQL.

### Mediator Pattern
**Categoría:** Patrones de diseño / Arquitectura
**Definición:** Patrón de comportamiento que introduce un objeto intermediario para gestionar la comunicación entre objetos, eliminando las dependencias directas entre ellos. En el back-template el mediator desacopla los Controllers de los Handlers y los Handlers de los Presenters.
**En el proyecto:** El mediator del sistema de mensajería acepta un `Request` del Controller, lo pasa por el pipeline de behaviors, ejecuta el Handler y publica la Response hacia el Presenter, sin que ninguno de estos componentes se conozca directamente.

### MinimumLength (regla FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Regla que verifica que la longitud de una cadena supere o iguale el valor mínimo indicado. Se usa frecuentemente para contraseñas y códigos donde una longitud mínima es un requisito de seguridad o negocio.
**En el proyecto:** El `RegisterAgentRequestValidator` del sistema de mensajería aplica `.MinimumLength(8)` al campo `Password` para enforcer la política de contraseñas antes de que el Handler procese el registro.

### Must (regla FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Regla de validación personalizada que acepta un predicado `Func<T, bool>` (o `Func<T, CancellationToken, Task<bool>>` para la variante asíncrona `MustAsync`). Permite validaciones arbitrarias que las reglas predefinidas no cubren.
**En el proyecto:** En el sistema de mensajería se usa `Must(date => date > DateTime.UtcNow)` para validar que las fechas de campañas programadas sean futuras, y `MustAsync` para verificar en base de datos que un email no esté duplicado antes del registro.

### MustAsync (regla FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Versión asíncrona de `Must` que acepta un predicado `Func<T, CancellationToken, Task<bool>>`. Permite realizar consultas a base de datos u otros servicios asíncronos como parte de la validación.
**En el proyecto:** Usado en el sistema de mensajería para verificar en el validator si un email ya existe en la base de datos (`!await _repo.EmailExistsAsync(email, ct)`), complementando la verificación semántica que también se hace en el Handler.

---

## N

### next() (RequestHandlerDelegate)
**Categoría:** Pipeline / Mensajería
**Definición:** Delegado de tipo `RequestHandlerDelegate<TResponse>` que representa el siguiente eslabón en la cadena del pipeline. Al invocarlo con `await next()`, el behavior cede el control al siguiente behavior registrado o, si es el último, al Handler. Permite ejecutar código antes (`pre-handler`) y después (`post-handler`) de la ejecución del Handler.
**En el proyecto:** El `ValidationBehavior` del sistema de mensajería llama `return await next()` solo si no hay errores de validación; el `InteractorPipeline` lo llama para obtener la Response y luego publicarla al Presenter.

### NotEmpty (regla FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Regla que verifica que un campo no sea `null`, cadena vacía, ni cadena de solo espacios en blanco (para strings). Equivalente a `!string.IsNullOrWhiteSpace(value)`. Es la regla de presencia más usada en validators.
**En el proyecto:** Aplicada en todos los validators del sistema de mensajería para campos obligatorios: `FullName`, `Email`, `Password` en `RegisterAgentRequestValidator`; `Content`, `ChannelId` en `SendMessageRequestValidator`.

### NotFoundException
**Categoría:** Excepciones / Dominio
**Definición:** Excepción personalizada (antipatrón en el back-template) que representa que un recurso no fue encontrado. El back-template la reemplaza con el `sealed record ...NotFoundFailure : INotFoundFailure` para que el caso "no encontrado" sea un resultado tipado, no una excepción costosa.
**En el proyecto:** El sistema de mensajería no usa `NotFoundException`; en cambio, `GetContactHandler` retorna `GetContactNotFoundFailure("Contacto no encontrado.")` cuando el contacto no existe, siguiendo el Result Pattern.

---

## O

### Ok (método del Controller)
**Categoría:** ASP.NET Core / HTTP
**Definición:** Método de `ControllerBase` que devuelve una respuesta HTTP 200 con el objeto serializado como cuerpo JSON. Se usa cuando la operación completó exitosamente.
**En el proyecto:** El Controller del sistema de mensajería retorna `Ok(viewModel)` cuando `_viewModel.IsSuccess` es `true`, serializando el `ResultViewModel` con `Data` e `IsSuccess` como cuerpo JSON.

---

## P

### Pattern matching (is)
**Categoría:** C# / Características del lenguaje
**Definición:** Característica del lenguaje C# que permite comprobar el tipo de un objeto y extraer sus propiedades en una sola expresión. La sintaxis `notification is INotFoundFailure notFound` comprueba si `notification` implementa `INotFoundFailure` y, si es así, lo asigna a `notFound` con ese tipo.
**En el proyecto:** Los Presenters del sistema de mensajería usan pattern matching para discriminar entre los diferentes tipos de Response y asignar la acción correcta al `ResultViewModel` (`Fail` o `Set`), determinando así el HTTP status code que el Controller devolverá.

### PerformanceBehavior
**Categoría:** Pipeline / Observabilidad
**Definición:** Pipeline Behavior que mide el tiempo de ejecución del Handler y emite una alerta (generalmente `LogWarning`) cuando supera un umbral configurable (típicamente 500ms). Ayuda a identificar Handlers lentos en producción sin modificarlos.
**En el proyecto:** Recomendado en el sistema de mensajería para Handlers de consultas pesadas como la búsqueda full-text en conversaciones (`SearchConversationsHandler`) o la generación de reportes de contactos.

### Pipeline Behavior
**Categoría:** Arquitectura / Mensajería
**Definición:** Componente que se registra en el contenedor de DI y envuelve la ejecución de Handlers en el mediator. Funciona como middleware para requests de dominio. Los behaviors se encadenan en el orden en que se registran; el primero es el más externo. Implementan `IPipelineBehavior<TRequest, TResponse>`.
**En el proyecto:** El sistema de mensajería usa `ValidationBehavior` (primero) e `InteractorPipeline` (segundo) como behaviors obligatorios. `PerformanceBehavior` y `TransactionBehavior` son opcionales según el tipo de operación.

### ProblemDetails
**Categoría:** HTTP / RFC 7807 / ASP.NET Core
**Definición:** Clase de ASP.NET Core que implementa el estándar RFC 7807 para describir errores HTTP en formato JSON. Contiene los campos `type`, `title`, `status`, `detail` e `instance`. El `Content-Type` de la respuesta es `application/problem+json`.
**En el proyecto:** Usado en el sistema de mensajería para todas las respuestas de error: el `GlobalExceptionHandler` produce `ValidationProblemDetails` para errores 400, y el middleware de `UseExceptionHandler` produce `ProblemDetails` genéricos para errores 500.

### PublicId
**Categoría:** Dominio / Diseño de entidades
**Definición:** Campo de tipo `Guid` de las entidades del back-template que actúa como identificador externo expuesto en la API. Distinto del `Id` interno (long) de base de datos, el `PublicId` evita exponer secuencias predecibles y facilita la portabilidad entre entornos.
**En el proyecto:** Las entidades del sistema de mensajería (`Agent`, `Contact`, `Conversation`, `Channel`) tienen `long Id` como clave primaria interna y `Guid PublicId` como identificador público que aparece en las URLs y DTOs de la API.

---

## R

### RFC 7807
**Categoría:** Estándares HTTP / Interoperabilidad
**Definición:** Estándar IETF (Request for Comments), obsoletado por el RFC 9457 (julio de 2023, que conserva el formato), que define un formato JSON uniforme para describir errores en APIs HTTP. Especifica los campos `type`, `title`, `status`, `detail` e `instance`, y permite extensiones adicionales. Garantiza que los clientes puedan procesar errores de forma consistente independientemente del endpoint.
**En el proyecto:** El sistema de mensajería adopta RFC 7807 a través de `ProblemDetails` y `ValidationProblemDetails` de ASP.NET Core. Todas las respuestas de error siguen esta estructura, lo que facilita el procesamiento uniforme de errores en los clientes React.

### RequestHandlerDelegate\<TResponse\>
**Categoría:** Mensajería / Pipeline
**Definición:** Ver **next() (RequestHandlerDelegate)**.

### Result (clase abstracta)
**Categoría:** Result Pattern / Common.Results
**Definición:** Clase abstracta base del sistema de Results del back-template que implementa `IResponse`. Expone métodos de fábrica estáticos: `Result.OK()`, `Result.Fail(message)`, `Result.OK<T>(data)`, `Result.Fail<T>(message)`. En la práctica los Handlers retornan subtipos de `IResponse`, no de `Result` directamente.
**En el proyecto:** Define la API de fábrica del sistema de Results del sistema de mensajería. Los métodos `Result.OK<T>` y `Result.Fail<T>` pueden usarse para construir resultados en utilidades compartidas o en tests.

### Result Pattern
**Categoría:** Arquitectura / Patrones de diseño
**Definición:** Patrón que hace explícitos en la firma del método todos los posibles resultados de una operación: éxito con datos, fallo por validación, fallo por recurso no encontrado, fallo por conflicto, etc. Evita usar excepciones para flujos de negocio esperados, mejorando el rendimiento, la legibilidad y la documentación implícita del contrato.
**En el proyecto:** Patrón central del back-template del sistema de mensajería. Cada Handler retorna un `abstract record Response` con variantes `sealed record` que implementan interfaces marcadoras como `ISuccess<T>`, `INotFoundFailure`, `IConflictFailure`. El Presenter discrimina esas variantes para producir la respuesta HTTP correcta.

### ResultViewModel\<T\>
**Categoría:** Presentación / DTO HTTP
**Definición:** Clase del `Common.ViewModels` del back-template que actúa como DTO entre el Presenter y el Controller. Contiene `IsSuccess`, `Data`, `Message` y `UtcTimeStamp`. El Presenter la llena vía `Fail(message)` o `Set(success)`; el Controller la serializa directamente como cuerpo JSON de la respuesta.
**En el proyecto:** Inyectada en los Controllers del sistema de mensajería con lifetime Scoped (`ResultViewModel<AgentsController>`, `ResultViewModel<ContactsController>`) para que cada request tenga su propia instancia aislada.

### Route (atributo)
**Categoría:** ASP.NET Core / Routing
**Definición:** Atributo que define la plantilla de ruta de un controlador o action. A nivel de controlador establece la ruta base; a nivel de action especifica el segmento adicional y el verbo HTTP. Combinado con `[ApiController]` permite el enrutamiento declarativo.
**En el proyecto:** Los controllers del sistema de mensajería usan `[Route("api/agents")]`, `[Route("api/contacts")]`, etc. con actions como `[HttpPost("register")]` o `[HttpGet("{publicId:guid}")]`.

### RuleFor
**Categoría:** Validación / FluentValidation
**Definición:** Método de `AbstractValidator<T>` que inicia la definición de una cadena de reglas para una propiedad específica del tipo validado. Acepta una expresión lambda que selecciona la propiedad y devuelve un builder al que se encadenan las reglas (`NotEmpty`, `MaximumLength`, `EmailAddress`, etc.).
**En el proyecto:** `RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200)` es el patrón estándar en los validators del sistema de mensajería.

### RuleForEach
**Categoría:** Validación / FluentValidation
**Definición:** Variante de `RuleFor` que aplica las reglas encadenadas a cada elemento de una colección (arrays, listas). Permite validar que todos los elementos de un campo de tipo lista cumplan ciertos criterios.
**En el proyecto:** Usado en el sistema de mensajería para validar colecciones como las etiquetas de un contacto (`Tags`) o los destinatarios de una campaña masiva, asegurando que ningún elemento individual supere la longitud permitida o esté vacío.

---

## S

### sealed record
**Categoría:** C# / Tipos de datos
**Definición:** Tipo de registro en C# que combina inmutabilidad por valor, igualdad estructural, deconstrucción y la garantía de que ninguna clase puede heredar de él (`sealed`). Se usa en el back-template para las variantes concretas de las jerarquías de Response, garantizando que el pattern matching sea exhaustivo y que los tipos no puedan extenderse inadvertidamente.
**En el proyecto:** `GetContactSuccess`, `GetContactNotFoundFailure`, `RegisterAgentEmailConflictFailure` son `sealed records` en el sistema de mensajería. Su inmutabilidad garantiza que la Response no se modifique entre el Handler y el Presenter.

### ServiceCollectionEx / AddApplicationServices
**Categoría:** DI / Configuración
**Definición:** Método de extensión de `IServiceCollection` (por convención llamado `AddApplicationServices`) que centraliza el registro de todos los servicios de la capa Application: behaviors del pipeline, validators de FluentValidation, Handlers e Interactors. Sigue el principio de extensión de servicios recomendado por Microsoft.
**En el proyecto:** En el sistema de mensajería este método registra `ValidationBehavior`, `InteractorPipeline` y llama a `AddValidatorsFromAssembly` en el orden correcto para que la cadena del pipeline funcione como se espera.

### Serilog
**Categoría:** Logging / Librería externa
**Definición:** Librería de logging estructurado para .NET que permite serializar objetos completos como propiedades de log (en lugar de interpolación de strings). Usa el operador `{@Property}` para serialización destructurada. Compatible con múltiples sinks (Seq, Elasticsearch, Application Insights, etc.).
**En el proyecto:** Logger oficial del back-template del sistema de mensajería. El `InteractorPipeline` usa `Logger.LogInformation("{@Request}", request)` y `Logger.LogInformation("{@Response}", response)` para registrar objetos completos de forma estructurada.

### Stack unwind (desenrollado de pila)
**Categoría:** C# / Rendimiento
**Definición:** Proceso que ocurre cuando se lanza una excepción: el runtime de .NET recorre la pila de llamadas hacia arriba buscando un bloque `catch` apropiado, captura el estado de cada frame y construye el stack trace. Este proceso es significativamente más caro en tiempo de CPU que retornar un objeto de resultado ordinario.
**En el proyecto:** El Result Pattern del sistema de mensajería evita el stack unwind para casos de negocio esperados (not found, conflict, validation). Las excepciones se reservan para bugs e inconsistencias del sistema donde el costo del stack trace está justificado por su valor diagnóstico.

### SuccessResult
**Categoría:** Result Pattern / Common.Results
**Definición:** Clase concreta `sealed` que implementa `Result` e `ISuccess`. `SuccessResult` no lleva datos; `SuccessResult<T>` lleva el dato en la propiedad `Data`. Incluye un operador de conversión implícita de `T` a `SuccessResult<T>` para simplificar la construcción. El método de fábrica `Result.OK<T>(data)` crea instancias.
**En el proyecto:** Análogo a `FailureResult`, usado internamente. En los Handlers del sistema de mensajería se retornan `sealed records` propios (`GetContactSuccess`, etc.) en lugar de `SuccessResult` directamente.

---

## T

### TenantId
**Categoría:** Multi-tenancy / Dominio
**Definición:** Identificador del tenant (organización o workspace) al que pertenece una entidad o request. En una arquitectura multi-tenant se incluye en las queries para aislar los datos entre clientes. En el back-template aparece como parámetro en los métodos de repositorio.
**En el proyecto:** El `GetUserProfileHandler` del sistema de mensajería pasa `request.TenantId` al repositorio para garantizar que solo se accedan perfiles del workspace correcto, implementando el aislamiento de datos entre organizaciones clientes.

### TraceIdentifier
**Categoría:** ASP.NET Core / Observabilidad
**Definición:** Propiedad de `HttpContext` que contiene un identificador único generado por ASP.NET Core para cada request HTTP. Se usa como fallback del `traceId` cuando no hay una actividad de tracing distribuido activa (`Activity.Current` es null).
**En el proyecto:** Incluido en el campo `traceId` de las respuestas de error del sistema de mensajería para correlacionar errores reportados por clientes con las entradas de log en el servidor, incluso en ambientes sin OpenTelemetry configurado.

### TransactionBehavior
**Categoría:** Pipeline / Base de datos
**Definición:** Pipeline Behavior que abre una transacción de base de datos antes de ejecutar el Handler y la commitea si el Handler retorna con éxito, o la revierte si ocurre una excepción. Solo se aplica a Commands (escrituras) usando una interfaz marcadora como `ICommand<TResponse>`.
**En el proyecto:** Aplicable en el sistema de mensajería a Commands que realizan múltiples escrituras relacionadas: registrar un agente y su perfil de configuración, enviar un mensaje y actualizar el estado de la conversación, o crear un contacto con sus etiquetas iniciales.

---

## U

### UseExceptionHandler
**Categoría:** ASP.NET Core / Middleware
**Definición:** Middleware de ASP.NET Core que intercepta excepciones no manejadas que burbujean desde los middleware internos y las canaliza a los `IExceptionHandler` registrados. Sin argumentos, delega completamente a los handlers registrados con `AddExceptionHandler<T>`.
**En el proyecto:** Registrado en el `Program.cs` del sistema de mensajería para que el `GlobalExceptionHandler` capture las `ValidationException` de FluentValidation y las convierta en respuestas `ValidationProblemDetails` en lugar de páginas de error HTML.

### UseStatusCodePages
**Categoría:** ASP.NET Core / Middleware
**Definición:** Middleware que intercepta respuestas HTTP sin cuerpo con códigos de estado de error (4xx, 5xx) y les agrega un cuerpo de respuesta estándar. Al combinarlo con `AddProblemDetails`, convierte respuestas vacías como 404 de rutas inexistentes en respuestas Problem Details.
**En el proyecto:** Registrado en el `Program.cs` del sistema de mensajería para que las respuestas 404 por rutas no encontradas y 405 por métodos no permitidos produzcan automáticamente un cuerpo Problem Details consistente.

---

## V

### ValidationBehavior
**Categoría:** Pipeline / Validación
**Definición:** Pipeline Behavior que recibe todos los validators `IValidator<TRequest>` registrados para el tipo de Request, los ejecuta con `ValidationContext<TRequest>` y, si hay errores, lanza una `ValidationException` con la lista de fallos. Si no hay validators para el Request, pasa directamente al siguiente behavior.
**En el proyecto:** Primer behavior del pipeline del sistema de mensajería. Garantiza que ningún Handler procese datos con formato inválido: si `RegisterAgentRequest` llega con email malformado o contraseña corta, `ValidationBehavior` corta la cadena antes de que `RegisterAgentHandler` ejecute lógica de negocio.

### ValidationContext\<T\>
**Categoría:** Validación / FluentValidation
**Definición:** Objeto que encapsula la instancia a validar y metadatos opcionales (selector de propiedades, datos de contexto adicionales). Se pasa al método `Validate` de cada `IValidator<T>`. Permite validaciones parciales y la inyección de contexto adicional en los validators.
**En el proyecto:** El `ValidationBehavior` del sistema de mensajería crea un `new ValidationContext<TRequest>(request)` y lo pasa a cada validator registrado para que evalúen el objeto de request completo.

### ValidationException (FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Excepción lanzada por `ValidationBehavior` cuando uno o más validators fallan. Contiene la colección de `ValidationFailure` con el nombre de la propiedad (`PropertyName`), el mensaje de error y el valor que causó el fallo. El `GlobalExceptionHandler` la intercepta para convertirla en una respuesta 400.
**En el proyecto:** Nexo entre `ValidationBehavior` y `GlobalExceptionHandler` en el sistema de mensajería. No llega al Handler ni al Presenter  -  es interceptada antes por el middleware de excepciones y convertida en `ValidationProblemDetails`.

### ValidationFailure
**Categoría:** Validación / FluentValidation
**Definición:** Objeto que representa un error de validación individual. Contiene `PropertyName` (nombre del campo), `ErrorMessage` (mensaje configurado con `WithMessage`) y `AttemptedValue` (valor que causó el fallo). Una `ValidationException` puede contener múltiples instancias.
**En el proyecto:** El `GlobalExceptionHandler` del sistema de mensajería agrupa los `ValidationFailure` por `PropertyName` para construir el diccionario de errores del `ValidationProblemDetails`, resultando en respuestas JSON con errores por campo como `{ "Email": ["Email con formato inválido."] }`.

### ValidationProblemDetails
**Categoría:** ASP.NET Core / RFC 7807
**Definición:** Subclase de `ProblemDetails` específica para errores 400 de validación. Extiende la estructura base con un diccionario `Errors` donde las claves son los nombres de los campos y los valores son arrays de mensajes de error para ese campo. Es el formato estándar de ASP.NET Core para errores de validación.
**En el proyecto:** Producido por el `GlobalExceptionHandler` del sistema de mensajería cuando captura una `ValidationException`, agrupando los errores por nombre de campo para que el frontend React pueda mostrarlos junto a los inputs correspondientes.

---

## W

### When (regla FluentValidation)
**Categoría:** Validación / FluentValidation
**Definición:** Modificador condicional de FluentValidation que hace que la regla precedente solo se evalúe cuando el predicado especificado es verdadero. Permite validaciones contextuales donde ciertos campos solo son requeridos en condiciones específicas.
**En el proyecto:** Usado en el sistema de mensajería para validaciones condicionales: por ejemplo, el campo `DiscountCode` solo es obligatorio cuando `HasDiscount` es `true`, o el campo `ScheduledAt` solo se valida cuando el tipo de envío de campaña es programado.

### WithMessage
**Categoría:** Validación / FluentValidation
**Definición:** Método encadenable en FluentValidation que personaliza el mensaje de error de la regla precedente. Sin `WithMessage`, FluentValidation genera un mensaje en inglés por defecto. Con `WithMessage` se provee el mensaje en el idioma y tono apropiado para el usuario final.
**En el proyecto:** Todos los validators del sistema de mensajería usan `WithMessage` con mensajes en español para los campos de usuarios (`"El nombre es requerido."`, `"Email con formato inválido."`, `"Mínimo 8 caracteres."`), garantizando mensajes de error localizados.

---

*Instructor: Rogelio Arriaga González  -  Raptor Dev Services*
