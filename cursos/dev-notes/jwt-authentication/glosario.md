# Glosario Técnico  -  JWT  -  Autenticación

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### Access Token
**Categoría:** JWT / Autenticación
**Definición:** Token de corta duración (60 minutos en el proyecto) que el cliente incluye en cada request HTTP para demostrar su identidad. Es un JWT firmado que contiene claims como `sub`, `email`, `role` y `tenant_id`. Tiene vida corta para minimizar el daño si es interceptado.
**En el proyecto:** El sistema de mensajería emite el Access Token al hacer login o registro. Se envía en el header `Authorization: Bearer <access_token>` en cada llamada a la API.

---

### AddAuthentication
**Categoría:** .NET / Middleware
**Definición:** Método de extensión de ASP.NET Core que registra el servicio de autenticación en el contenedor DI y define el esquema de autenticación por defecto. Se encadena con `.AddJwtBearer(...)` para indicar que el esquema es Bearer JWT.
**En el proyecto:** Se llama en `JwtAuthExtensions.cs` con `JwtBearerDefaults.AuthenticationScheme` como esquema predeterminado, luego se configura con `TokenValidationParameters`.

---

### AddAuthorization
**Categoría:** .NET / Middleware
**Definición:** Método de extensión que registra el servicio de autorización y permite definir políticas nombradas con `options.AddPolicy(...)`. Las políticas pueden combinar requisitos de roles, claims o lógica personalizada.
**En el proyecto:** Se usa en `Program.cs` para definir políticas como `"RequireAdmin"` y `"RequireAgentOrAdmin"` que se aplican con el atributo `[Authorize(Policy = "...")]`.

---

### AddJwtBearer
**Categoría:** .NET / Autenticación
**Definición:** Método de extensión de `Microsoft.AspNetCore.Authentication.JwtBearer` que configura el manejador de autenticación Bearer JWT. Recibe un delegate de opciones donde se define `TokenValidationParameters` y, opcionalmente, `Authority` para proveedores externos.
**En el proyecto:** Se invoca dentro de `JwtAuthExtensions.cs` para configurar la validación de firma, issuer, audience y lifetime con `ClockSkew = TimeSpan.Zero`.

---

### AddJwtAuthentication
**Categoría:** Back-template / Extensión
**Definición:** Método de extensión propio del back-template que encapsula toda la configuración de autenticación JWT en un solo punto de entrada. Lee `Jwt:Key`, `Jwt:Issuer` y `Jwt:Audience` de `IConfiguration` y lanza excepción si alguno falta.
**En el proyecto:** Se registra en `Program.cs` con `builder.Services.AddJwtAuthentication(builder.Configuration)`.

---

### AddPolicy
**Categoría:** ASP.NET Core / Autorización
**Definición:** Método de `AuthorizationOptions` que define una política de autorización con nombre. Cada política especifica requisitos como roles, claims o handlers personalizados. Se aplica en controladores con `[Authorize(Policy = "NombrePolítica")]`.
**En el proyecto:** Se definen políticas `"RequireAdmin"`, `"RequireAgentOrAdmin"` y `"RequireAnyRole"` para controlar el acceso a endpoints de administración, asignación y visualización respectivamente.

---

### AllowAnonymous
**Categoría:** ASP.NET Core / Autorización
**Definición:** Atributo de ASP.NET Core que excluye un controlador o acción de cualquier requisito de autenticación o autorización. Permite el acceso público aunque haya un filtro de autorización global aplicado.
**En el proyecto:** Se aplica a `AuthController` completo porque los endpoints `/api/auth/login`, `/api/auth/register` y `/api/auth/refresh` son públicos por definición  -  el usuario aún no tiene token.

---

### Audience (`aud`)
**Categoría:** JWT / Claims
**Definición:** Claim estándar (RFC 7519) que identifica el receptor o conjunto de receptores para quienes fue emitido el token. La API valida que el claim `aud` del token coincida con el valor configurado, rechazando tokens emitidos para otros servicios.
**En el proyecto:** Se configura como `"mensajeria-client"` en `appsettings.json` bajo `Jwt:Audience`. El middleware verifica este valor en cada request.

---

### Authority
**Categoría:** OAuth2 / OpenID Connect
**Definición:** URL base del servidor de autorización (Identity Provider). El SDK de .NET descarga automáticamente el documento de descubrimiento en `/.well-known/openid-configuration` para obtener las claves públicas y los endpoints disponibles.
**En el proyecto:** Solo se usa en la variante Keycloak (`options.Authority = "https://auth.ejemplo.io/realms/crm"`). En la implementación propia con JWT, no se usa `Authority`  -  la clave se configura manualmente.

---

### Authorization Header
**Categoría:** HTTP / Autenticación
**Definición:** Header HTTP estándar (`Authorization: Bearer <token>`) que el cliente incluye en cada request para enviar el Access Token. El middleware de ASP.NET Core lo extrae automáticamente y lo valida antes de que llegue al controlador.
**En el proyecto:** La SPA del CRM incluye este header en cada llamada autenticada. El Refresh Token nunca viaja en este header  -  tiene su propio endpoint dedicado.

---

### Authorization Server
**Categoría:** OAuth2
**Definición:** En el modelo OAuth2, el servidor responsable de autenticar al usuario (Resource Owner) y emitir tokens de acceso al cliente. Implementa los flujos de autorización definidos en RFC 6749.
**En el proyecto:** En la implementación propia, la propia API actúa como Authorization Server. En el escenario con Keycloak, sería un servidor Keycloak separado.

---

## B

### Base64url
**Categoría:** JWT / Codificación
**Definición:** Variante de Base64 que reemplaza `+` por `-` y `/` por `_`, y omite el padding `=`. Se usa en JWT porque es seguro para URL y headers HTTP sin necesidad de encoding adicional. No cifra  -  solo codifica.
**En el proyecto:** Las tres partes del JWT (Header, Payload, Signature) se codifican en Base64url. El Payload puede decodificarse sin clave  -  nunca debe contener datos sensibles como contraseñas.

---

### BCrypt
**Categoría:** Seguridad / Hashing
**Definición:** Algoritmo de hashing de contraseñas diseñado para ser lento por diseño (adaptive cost factor). Incluye salt automático y es resistente a ataques de diccionario y fuerza bruta. No es reversible  -  para verificar se hashea de nuevo y se compara.
**En el proyecto:** Se usa a través de la interfaz `IPasswordHasher` para hashear la contraseña al registrar y para verificarla al hacer login con `_hasher.Verify(request.Password, credential.PasswordHash)`.

---

### Bearer Token
**Categoría:** HTTP / Autenticación
**Definición:** Esquema de autenticación HTTP (RFC 6750) donde el portador del token (`bearer`) tiene acceso al recurso sin necesidad de demostrar propiedad adicional. El nombre refleja que "quien tenga el token, puede usarlo"  -  de ahí la importancia de protegerlo.
**En el proyecto:** Todos los endpoints protegidos del CRM usan Bearer Token. El middleware `AddJwtBearer` extrae el token del header `Authorization: Bearer <token>`.

---

## C

### Captive Dependency
**Categoría:** DI / Anti-patrones
**Definición:** Error de diseño donde un servicio de mayor lifetime (Singleton) depende de un servicio de menor lifetime (Scoped o Transient). El Scoped queda "capturado" por el Singleton y no se libera correctamente, pudiendo causar comportamientos incorrectos o errores de concurrencia.
**En el proyecto:** Se debe evitar registrar `JwtTokenService` como Singleton si tuviera dependencias Scoped como `DbContext`. En la implementación actual solo depende de `IConfiguration` (Singleton), por lo que el registro como Singleton es correcto.

---

### Claim
**Categoría:** JWT / Autenticación
**Definición:** Par clave-valor dentro del Payload de un JWT que describe al sujeto del token. Hay claims estándar definidos en RFC 7519 (`sub`, `iss`, `aud`, `exp`, `iat`, `jti`) y claims personalizados que cada aplicación define según su necesidad.
**En el proyecto:** El CRM usa `sub` (GUID del agente), `email`, `ClaimTypes.Role` (`Admin`/`Agent`/`Viewer`), `tenant_id` y `jti` en cada Access Token.

---

### ClaimTypes.Role
**Categoría:** .NET / Claims
**Definición:** Constante de .NET que representa el URI completo del claim de rol: `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. ASP.NET Core lo usa internamente para el atributo `[Authorize(Roles = "...")]`.
**En el proyecto:** Se incluye en el JWT como `new Claim(ClaimTypes.Role, role)` donde `role` es `"Admin"`, `"Agent"` o `"Viewer"`. Los endpoints usan `[Authorize(Roles = "Admin")]` para restringir acceso por rol.

---

### ClockSkew
**Categoría:** JWT / Validación
**Definición:** Tolerancia de tiempo que el validador de JWT aplica al verificar la expiración del token. Por defecto .NET usa 5 minutos, lo que significa que un token "expirado" sigue siendo aceptado 5 minutos después de su `exp`. Se puede eliminar estableciéndolo a `TimeSpan.Zero`.
**En el proyecto:** El back-template usa `ClockSkew = TimeSpan.Zero` para máxima seguridad. Los servidores en AWS (EC2/ECS) tienen NTP configurado por defecto, garantizando sincronía de relojes.

---

### Common.Results
**Categoría:** Back-template / Librería
**Definición:** Namespace del back-template que define las interfaces del patrón Result: `IFailure`, `ISuccess<T>`, `IValidationFailure`, `INotFoundFailure` e `IConflictFailure`. Establece el contrato que los Handlers usan para comunicar el resultado de un caso de uso.
**En el proyecto:** Todos los Responses de autenticación implementan estas interfaces. El Presenter hace pattern matching sobre `IFailure` / `ISuccess<TokenDto>` para poblar el ViewModel.

---

## D

### DI Lifetime (Singleton / Scoped / Transient)
**Categoría:** .NET / Dependency Injection
**Definición:** Política que controla cuándo se crea y destruye una instancia de un servicio registrado en el contenedor DI. Singleton crea una única instancia por proceso; Scoped crea una por request HTTP; Transient crea una nueva cada vez que se solicita.
**En el proyecto:** `JwtTokenService` se registra como Singleton porque solo depende de `IConfiguration`. Los repositorios se registran como Scoped porque dependen de `DbContext` (que es Scoped por naturaleza).

---

## E

### ExpiresAtUtc
**Categoría:** Dominio / RefreshToken
**Definición:** Propiedad de la entidad `RefreshToken` que almacena la fecha y hora de expiración en formato UTC. Se compara con `DateTime.UtcNow` al validar un Refresh Token para determinar si sigue siendo válido.
**En el proyecto:** Se persiste en la tabla `RefreshTokens` de PostgreSQL. El handler verifica `existing.ExpiresAtUtc <= DateTime.UtcNow` antes de rotar el token.

---

## F

### FindFirstValue
**Categoría:** .NET / Claims
**Definición:** Método de extensión de `ClaimsPrincipal` que retorna el valor del primer claim que coincida con el tipo especificado, o `null` si no existe. Es la forma idiomática de leer claims desde un controlador ASP.NET Core.
**En el proyecto:** Se usa en `BaseAuthController` para extraer `tenant_id`, `sub` y `ClaimTypes.Role` del JWT del request actual sin acceder directamente al token.

---

## G

### GenerateAccessToken
**Categoría:** Back-template / JWT
**Definición:** Método de `IJwtTokenService` que construye y firma un JWT con los claims del agente. Recibe `Guid userPublicId`, `string email`, `string role` y `long tenantId`  -  exactamente los cuatro datos que se incluyen en cada token.
**En el proyecto:** Se llama en `LoginHandler`, `RegisterHandler` y `RefreshTokenHandler` para emitir un nuevo Access Token después de validar credenciales o rotar tokens.

---

### GenerateRefreshToken
**Categoría:** Back-template / JWT
**Definición:** Método de `IJwtTokenService` que genera un token opaco de 64 bytes aleatorios codificados en Base64 (86 caracteres resultantes). No es un JWT  -  no tiene estructura ni firma, su validez depende de encontrarlo en la base de datos.
**En el proyecto:** Genera el Refresh Token que se persiste en la tabla `RefreshTokens` de PostgreSQL. Al rotar, el token anterior se revoca y este reemplaza al anterior.

---

### GlobalQueryFilter
**Categoría:** EF Core / Multi-tenancy
**Definición:** Filtro que EF Core aplica automáticamente a todas las consultas de una entidad sin que el desarrollador lo indique explícitamente. Se configura en `OnModelCreating` con `modelBuilder.Entity<T>().HasQueryFilter(...)`.
**En el proyecto:** `ITenantContextAccessor` alimenta el global query filter de EF Core, asegurando que cada agente solo vea conversaciones, contactos y mensajes de su propio workspace.

---

### GUID / Guid
**Categoría:** .NET / Identificadores
**Definición:** Globally Unique Identifier  -  número de 128 bits que garantiza unicidad global sin coordinación central. Se usa como identificador público de entidades que serán expuestas en APIs o tokens, evitando exponer IDs internos secuenciales.
**En el proyecto:** El claim `sub` del JWT es el GUID público del agente (`credential.PublicId`). El ID interno (`credential.Id`) es un `long` secuencial que nunca se expone en tokens ni en la API.

---

## H

### Handler
**Categoría:** Back-template / Arquitectura
**Definición:** Clase que implementa `IRequestHandler<TRequest, TResponse>` y contiene la lógica de negocio de un caso de uso. Recibe el `Request`, ejecuta la operación y retorna un `Response`. Es el núcleo de la capa Application en el patrón Mediator.
**En el proyecto:** `LoginHandler`, `RegisterHandler` y `RefreshTokenHandler` son los tres handlers de autenticación. Coordinan repositorios, el servicio JWT y publican eventos de integración.

---

### Header (JWT)
**Categoría:** JWT / Estructura
**Definición:** Primera parte del JWT, codificada en Base64url. Contiene el tipo de token (`"typ": "JWT"`) y el algoritmo de firma (`"alg": "HS256"` o `"alg": "RS256"`). El validador usa el `alg` para saber cómo verificar la firma.
**En el proyecto:** El header del CRM siempre indica `HS256` porque usamos `SecurityAlgorithms.HmacSha256` en `JwtTokenService`.

---

### HMAC-SHA256 / HmacSha256
**Categoría:** Criptografía / JWT
**Definición:** Hash-based Message Authentication Code usando SHA-256. Combina la clave secreta con el contenido del mensaje para producir una firma que garantiza integridad y autenticidad. Es el algoritmo detrás de HS256.
**En el proyecto:** Se especifica con `SecurityAlgorithms.HmacSha256` al crear `SigningCredentials`. Tanto la firma como la verificación usan la misma clave `Jwt:Key`.

---

### HS256
**Categoría:** JWT / Algoritmos
**Definición:** Algoritmo de firma simétrico para JWT basado en HMAC-SHA256. Usa una única clave secreta compartida tanto para firmar el token como para verificarlo. Es simple, rápido e ideal para sistemas con un único servicio que emite y valida tokens.
**En el proyecto:** Algoritmo elegido para el sistema de mensajería por ser un monolito modular con un único punto de validación. La clave está en `Jwt:Key` (mínimo 32 caracteres / 256 bits).

---

### HttpContext
**Categoría:** ASP.NET Core / Runtime
**Definición:** Objeto que encapsula toda la información de una request HTTP: headers, body, usuario autenticado (`User`), response en construcción, y servicios del request actual. Es el punto de acceso al estado de la request dentro del pipeline.
**En el proyecto:** `TenantClaimsMiddleware` accede a `context.User` para leer el claim `tenant_id` del JWT y poblarlo en `ITenantContextAccessor`.

---

## I

### IConfiguration
**Categoría:** .NET / Configuración
**Definición:** Interfaz de .NET que proporciona acceso de lectura a todos los proveedores de configuración registrados (appsettings.json, variables de entorno, User Secrets, etc.) mediante una API unificada de clave-valor.
**En el proyecto:** `JwtTokenService` recibe `IConfiguration` por constructor para leer `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes` y `Jwt:RefreshTokenExpiryDays` sin acoplarse a un proveedor específico.

---

### IConflictFailure
**Categoría:** Common.Results / Interfaces
**Definición:** Interfaz del patrón Result que extiende `IFailure` y semanticamente representa un conflicto de datos  -  típicamente un duplicado o violación de unicidad. El Controller la mapea a `409 Conflict`.
**En el proyecto:** `RegisterEmailConflictFailure` implementa esta interfaz. Cuando el Controller detecta `result is RegisterEmailConflictFailure`, retorna `Conflict(_viewModel)`.

---

### Identity Brokering
**Categoría:** Keycloak / OAuth2
**Definición:** Capacidad de un Identity Provider de delegar la autenticación a proveedores externos (Google, Microsoft, GitHub, Facebook) y federar la identidad resultante en su propio sistema de usuarios. Elimina la necesidad de implementar OAuth2 social por separado para cada proveedor.
**En el proyecto:** No está implementado en el sistema de mensajería actual. Es una de las ventajas de Keycloak sobre JWT propio si la empresa necesitara login con Google o Microsoft en el futuro.

---

### Identity Provider (IdP)
**Categoría:** Seguridad / OAuth2
**Definición:** Servicio que autentica usuarios y emite tokens de identidad. Centraliza la gestión de credenciales, MFA, sesiones y federación de identidad. Ejemplos: Keycloak, Auth0, Azure AD, Okta.
**En el proyecto:** En la implementación actual, la propia API del CRM actúa como IdP básico. Keycloak se presentó como alternativa externa si se requiriera SSO o login social con Google/Microsoft.

---

### IFailure
**Categoría:** Common.Results / Interfaces
**Definición:** Interfaz del patrón Result que representa cualquier resultado fallido con un mensaje descriptivo. Los Presenters hacen pattern matching contra `IFailure` para detectar cualquier tipo de falla sin conocer el tipo concreto.
**En el proyecto:** Los Presenters de autenticación verifican `if (notification is IFailure failure)` y llaman `_viewModel.Fail(failure.Message)`. Los tipos concretos (`LoginInvalidCredentialsFailure`, `RefreshTokenInvalidFailure`) implementan esta interfaz.

---

### IJwtTokenService
**Categoría:** Back-template / Interfaces
**Definición:** Interfaz que abstrae las operaciones de generación y validez de tokens JWT. Define tres métodos: `GenerateAccessToken`, `GenerateRefreshToken` y `GetRefreshTokenExpiry`. Permite testear los Handlers sin instanciar la implementación real.
**En el proyecto:** Se inyecta en `LoginHandler`, `RegisterHandler` y `RefreshTokenHandler`. La implementación concreta `JwtTokenService` vive en la capa Infrastructure.

---

### INotFoundFailure
**Categoría:** Common.Results / Interfaces
**Definición:** Interfaz del patrón Result que extiende `IFailure` y representa un recurso no encontrado. El Controller la mapea a `404 Not Found`.
**En el proyecto:** `RegisterTenantNotFoundFailure` implementa esta interfaz. Se retorna cuando el `TenantId` enviado en el registro no corresponde a ningún workspace activo en la base de datos.

---

### INotificationHandler
**Categoría:** Back-template / Mediator
**Definición:** Interfaz del patrón Mediator que los Presenters implementan para reaccionar a los Responses publicados como notificaciones. Recibe el Response y decide qué hacer con él  -  normalmente poblar el ViewModel.
**En el proyecto:** `LoginPresenter`, `RegisterPresenter` y `RefreshTokenPresenter` implementan `INotificationHandler<TResponse>` para recibir sus respectivos Responses y traducirlos al ViewModel del controlador.

---

### IPasswordHasher
**Categoría:** Back-template / Seguridad
**Definición:** Interfaz que abstrae las operaciones de hashing y verificación de contraseñas. Define al menos dos métodos: `Hash(password)` para crear el hash y `Verify(password, hash)` para comparar. La implementación concreta usa BCrypt.
**En el proyecto:** Se inyecta en `LoginHandler` (para verificar) y en `RegisterHandler` (para hashear la contraseña antes de persistir la credencial).

---

### IRefreshTokenRepository
**Categoría:** Back-template / Repositorios
**Definición:** Interfaz del repositorio de Refresh Tokens que define operaciones específicas del dominio: `GetByTokenAsync`, `InsertAsync`, `RevokeAsync`. Abstrae el acceso a la tabla `RefreshTokens` de PostgreSQL.
**En el proyecto:** Se inyecta en los tres handlers de autenticación. `RefreshTokenHandler` lo usa para buscar, revocar e insertar tokens en el flujo de rotación.

---

### IRequest
**Categoría:** Back-template / Mediator
**Definición:** Interfaz marker que convierte un record en un comando o query del patrón Mediator. Especifica el tipo de Response esperado como parámetro genérico. El Mediator enruta el Request al Handler correspondiente.
**En el proyecto:** `LoginRequest`, `RegisterRequest` y `RefreshTokenRequest` implementan `IRequest<TResponse>`. Se envían con `Mediator.Send(request, ct)` desde el Controller.

---

### IResponse
**Categoría:** Back-template / Mediator
**Definición:** Interfaz base marker que todos los tipos de Response deben implementar. Permite que el Mediator los publique como `INotification` para que los Presenters los reciban. Los abstract records base (`LoginResponse`, `RegisterResponse`) implementan esta interfaz.
**En el proyecto:** Todos los abstract records de respuesta de autenticación implementan `IResponse`. Es parte del contrato del back-template para mantener consistencia en el patrón Response/Presenter.

---

### IsRevoked
**Categoría:** Dominio / RefreshToken
**Definición:** Propiedad booleana de la entidad `RefreshToken` que indica si el token fue invalidado explícitamente. Permite implementar logout, rotación de tokens y detección de reuse attacks sin depender únicamente de la expiración.
**En el proyecto:** `RefreshTokenHandler` verifica `existing.IsRevoked` antes de aceptar un Refresh Token. `RevokeAsync` actualiza esta propiedad a `true` en la base de datos.

---

### ISuccess / ISuccess\<T\>
**Categoría:** Common.Results / Interfaces
**Definición:** Interfaces del patrón Result que representan un resultado exitoso. `ISuccess<T>` agrega la propiedad `Data` del tipo correspondiente. Los Presenters las usan para extraer el dato de éxito y poblarlo en el ViewModel.
**En el proyecto:** `LoginSuccess`, `RegisterSuccess` y `RefreshTokenSuccess` implementan `ISuccess<TokenDto>`. El Presenter verifica `if (notification is ISuccess<TokenDto> success)` y llama `_viewModel.Set(success)`.

---

### ITenantContextAccessor
**Categoría:** Back-template / Multi-tenancy
**Definición:** Interfaz que proporciona acceso al contexto del tenant activo en el request actual. Se puebla por `TenantClaimsMiddleware` y es consumida por EF Core en el global query filter para aislar los datos por workspace.
**En el proyecto:** `TenantClaimsMiddleware` asigna `tenantCtx.Current = new TenantContext(tenantId)` después de extraer `tenant_id` del JWT. Cada handler puede acceder al tenant sin leer el JWT directamente.

---

### IUserCredentialRepository
**Categoría:** Back-template / Repositorios
**Definición:** Interfaz del repositorio de credenciales de usuario que define operaciones como `GetForLoginAsync`, `ExistsByEmailAsync`, `GetByIdAsync` e `InsertAsync`. Abstrae el acceso a la tabla de credenciales en PostgreSQL.
**En el proyecto:** Se inyecta en los handlers de autenticación. `LoginHandler` lo usa para buscar credenciales por email; `RegisterHandler` para verificar unicidad e insertar la nueva credencial.

---

### IValidationFailure
**Categoría:** Common.Results / Interfaces
**Definición:** Interfaz del patrón Result que extiende `IFailure` y representa errores de validación de entrada. El Controller la mapea típicamente a `400 Bad Request` o `422 Unprocessable Entity`.
**En el proyecto:** Está definida en `Common.Results` aunque en el módulo de autenticación no se usa directamente  -  la validación de formato se delega a FluentValidation en el pipeline de behaviors.

---

## J

### jti (JWT ID)
**Categoría:** JWT / Claims
**Definición:** Claim estándar (RFC 7519) que contiene un identificador único para el token JWT. Permite detectar y rechazar tokens duplicados o robados mediante una lista de tokens revocados (denylist). Se genera como un nuevo GUID en cada emisión.
**En el proyecto:** Se incluye como `new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())` en `JwtTokenService`. Habilita la revocación granular de tokens individuales sin invalidar toda la sesión.

---

### JWT (JSON Web Token)
**Categoría:** Estándar / Autenticación
**Definición:** Estándar abierto (RFC 7519) para representar claims entre dos partes como un objeto JSON compacto y firmado digitalmente. Consta de tres partes separadas por puntos: Header, Payload y Signature, cada una codificada en Base64url.
**En el proyecto:** Es el mecanismo central de autenticación del sistema de mensajería. La API emite JWTs al login/register y los valida en cada request protegido.

---

### JwtBearerDefaults.AuthenticationScheme
**Categoría:** .NET / JWT
**Definición:** Constante de `Microsoft.AspNetCore.Authentication.JwtBearer` con el valor `"Bearer"`. Se usa para especificar que el esquema de autenticación predeterminado es JWT Bearer, lo que indica al pipeline dónde buscar y cómo validar el token.
**En el proyecto:** Se pasa a `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` en `JwtAuthExtensions.cs` para establecer Bearer como esquema por defecto en todos los endpoints.

---

### JwtRegisteredClaimNames
**Categoría:** .NET / JWT
**Definición:** Clase estática de .NET que expone constantes para los nombres de claims estándar del RFC 7519: `Sub`, `Email`, `Jti`, `Iss`, `Aud`, `Exp`, `Iat`, entre otros. Evita escribir strings literales al construir claims.
**En el proyecto:** Se usa en `JwtTokenService` con `JwtRegisteredClaimNames.Sub`, `JwtRegisteredClaimNames.Email` y `JwtRegisteredClaimNames.Jti` para asegurar nombres de claim correctos y consistentes.

---

### JwtSecurityToken
**Categoría:** .NET / JWT
**Definición:** Clase de `System.IdentityModel.Tokens.Jwt` que representa un JWT sin firmar. Recibe issuer, audience, claims, expiration y `SigningCredentials` para construir el token. Se serializa a string con `JwtSecurityTokenHandler().WriteToken(token)`.
**En el proyecto:** Se instancia en `JwtTokenService.GenerateAccessToken` con todos los claims del agente y se convierte a la cadena Base64url que se incluye en `TokenDto`.

---

### JwtSecurityTokenHandler
**Categoría:** .NET / JWT
**Definición:** Clase de .NET que maneja la creación y validación de JWTs. El método `WriteToken` serializa un `JwtSecurityToken` a string. El método `ReadJwtToken` deserializa sin validar (uso peligroso en producción). La validación real se hace a través del middleware.
**En el proyecto:** Se usa en `JwtTokenService` solo con `WriteToken(token)` para serializar el token generado. La validación la hace automáticamente el middleware `AddJwtBearer`.

---

### JwtTokenService
**Categoría:** Back-template / Infrastructure
**Definición:** Implementación concreta de `IJwtTokenService` que vive en la capa Infrastructure. Genera Access Tokens firmados con HS256, Refresh Tokens opacos de 64 bytes y calcula la fecha de expiración del Refresh Token leyendo la configuración.
**En el proyecto:** Se registra como Singleton en el contenedor DI porque solo depende de `IConfiguration` (también Singleton). Genera los tokens en los tres flujos de autenticación.

---

## K

### Keycloak
**Categoría:** Identity Provider / Open Source
**Definición:** Servidor de identidad y acceso open-source de Red Hat que implementa OAuth2, OpenID Connect y SAML 2.0. Provee gestión de usuarios, roles, SSO, MFA y federación con proveedores externos sin necesidad de código personalizado.
**En el proyecto:** No está implementado en el sistema de mensajería v1  -  se usa JWT propio. Keycloak sería la alternativa si la empresa necesitara SSO entre múltiples productos o login social con Google/Microsoft.

---

## L

### localStorage
**Categoría:** Web / Seguridad
**Definición:** API del navegador que persiste datos en el dispositivo del usuario de forma indefinida, accesible desde cualquier script JavaScript del mismo origen. Su accesibilidad desde JS lo hace vulnerable a ataques XSS que pueden robar tokens almacenados.
**En el proyecto:** Está explícitamente prohibido como almacenamiento del Access Token. La regla del proyecto es: el Access Token vive solo en memoria (variable JS). El Refresh Token tampoco debe almacenarse en `localStorage`.

---

### LoginHandler
**Categoría:** Back-template / Handlers
**Definición:** Handler que implementa el caso de uso de login. Busca credenciales por email, verifica la contraseña con BCrypt, verifica que la cuenta esté activa, emite Access Token y Refresh Token, y los persiste. Retorna `LoginSuccess` o `LoginInvalidCredentialsFailure`.
**En el proyecto:** Es el primer punto de entrada al sistema. Coordina `IUserCredentialRepository`, `IPasswordHasher`, `IJwtTokenService` e `IRefreshTokenRepository` para completar el flujo de autenticación.

---

### LoginInvalidCredentialsFailure
**Categoría:** Back-template / Responses
**Definición:** Response concreto de falla que implementa `IFailure`. Se retorna cuando las credenciales son incorrectas, la cuenta no existe o la cuenta está inactiva. El mensaje intencionalmente es genérico ("Credenciales inválidas") para no revelar si el email existe.
**En el proyecto:** El Controller lo detecta con `result is LoginInvalidCredentialsFailure` y retorna `401 Unauthorized`. El mensaje genérico es una práctica de seguridad para prevenir enumeración de usuarios.

---

### LoginPresenter
**Categoría:** Back-template / Presenters
**Definición:** Clase que implementa `INotificationHandler<LoginResponse>` y traduce el Response del Handler al ViewModel del Controller. Hace pattern matching sobre `IFailure` e `ISuccess<TokenDto>` para determinar si la operación fue exitosa o fallida.
**En el proyecto:** Recibe la notificación del Mediator después de que `LoginHandler` termina y decide si llama `_viewModel.Fail(message)` o `_viewModel.Set(success)`.

---

### LoginRequest
**Categoría:** Back-template / Commands
**Definición:** Record sellado que implementa `IRequest<LoginResponse>` y encapsula los datos de entrada del caso de uso de login: `Email` y `Password`. Es el comando que el Controller envía al Mediator.
**En el proyecto:** Se construye en `AuthController.Login` desde el body del request (`LoginBody`) y se despacha con `Mediator.Send(new LoginRequest(body.Email, body.Password), ct)`.

---

### LoginSuccess
**Categoría:** Back-template / Responses
**Definición:** Response concreto de éxito que implementa `ISuccess<TokenDto>`. Contiene el `TokenDto` con el Access Token, Refresh Token y fecha de expiración. El Controller lo mapea a `200 OK`.
**En el proyecto:** `LoginHandler` lo retorna cuando las credenciales son válidas y la cuenta está activa. El Presenter lo detecta con `notification is ISuccess<TokenDto> success`.

---

## M

### MFA (Multi-Factor Authentication)
**Categoría:** Seguridad / Autenticación
**Definición:** Mecanismo de autenticación que requiere al menos dos factores independientes: algo que sabes (contraseña), algo que tienes (dispositivo/OTP) o algo que eres (biometría). Reduce significativamente el riesgo de acceso no autorizado por robo de contraseñas.
**En el proyecto:** No está implementado en el sistema de mensajería v1. Es una de las ventajas de adoptar Keycloak  -  MFA se configura en la UI de administración sin código adicional.

---

### Mediator
**Categoría:** Back-template / Patrón
**Definición:** Patrón de diseño que desacopla el emisor de un mensaje de sus receptores mediante un intermediario (el Mediator). En el back-template se implementa con MediatR: el Controller envía un Request y el Mediator lo enruta al Handler correcto y publica el Response a los Presenters.
**En el proyecto:** `Mediator.Send(request, ct)` en el Controller desencadena el flujo Handler -> Presenter -> ViewModel. Mantiene al Controller sin lógica de negocio.

---

### Middleware
**Categoría:** ASP.NET Core / Pipeline
**Definición:** Componente del pipeline de ASP.NET Core que procesa requests HTTP en un orden determinado. Cada middleware puede inspeccionar, modificar o cortocircuitar el request. El orden de registro en `Program.cs` determina el orden de ejecución.
**En el proyecto:** El orden crítico es `UseAuthentication()` -> `UseAuthorization()` -> `UseMiddleware<TenantClaimsMiddleware>()`. `TenantClaimsMiddleware` debe ir después de la autenticación porque requiere que `context.User` ya esté poblado.

---

### Multi-tenancy
**Categoría:** Arquitectura / SaaS
**Definición:** Arquitectura donde una única instancia del software sirve a múltiples clientes (tenants) aislando sus datos. El aislamiento puede ser por base de datos separada (silo), esquema separado (bridge) o filtro de fila (pool).
**En el proyecto:** El CRM usa el modelo pool con EF Core global query filter. El `tenant_id` en el JWT garantiza que cada agente solo accede a los datos de su propio workspace.

---

## N

### NTP (Network Time Protocol)
**Categoría:** Infraestructura / Seguridad
**Definición:** Protocolo que sincroniza los relojes de sistemas en red con servidores de tiempo de referencia. Crítico para JWT cuando `ClockSkew = TimeSpan.Zero` porque una diferencia de reloj entre cliente y servidor podría rechazar tokens válidos.
**En el proyecto:** AWS EC2 y contenedores ECS tienen NTP configurado por defecto, lo que hace seguro usar `ClockSkew = TimeSpan.Zero` sin riesgo de rechazar tokens válidos por diferencias de reloj.

---

## O

### OAuth2
**Categoría:** Protocolo / Autorización
**Definición:** Protocolo de autorización (RFC 6749) que permite a aplicaciones obtener acceso limitado a recursos en nombre de un usuario sin compartir credenciales. Define roles: Resource Owner, Client, Authorization Server y Resource Server.
**En el proyecto:** La implementación propia del CRM sigue principios de OAuth2 (emisión de tokens, refresh flow) sin implementar el protocolo completo. Keycloak implementa OAuth2 completamente con todos sus flujos.

---

### OpenID Connect (OIDC)
**Categoría:** Protocolo / Autenticación
**Definición:** Capa de identidad sobre OAuth2 (especificación de la OpenID Foundation) que agrega autenticación al protocolo de autorización. Introduce el ID Token como JWT estándar y el endpoint `/userinfo` para obtener datos del usuario autenticado.
**En el proyecto:** No implementado directamente. Keycloak lo implementa nativamente  -  el SDK de .NET lo consume configurando `Authority` para descargar automáticamente el documento de descubrimiento.

---

## P

### Payload (JWT)
**Categoría:** JWT / Estructura
**Definición:** Segunda parte del JWT, codificada en Base64url, que contiene los claims del token como un objeto JSON. No está cifrado  -  cualquiera puede decodificarlo. La firma garantiza que no fue modificado, no que sea confidencial.
**En el proyecto:** Contiene `sub`, `email`, `role`, `tenant_id` y `jti`. Nunca debe contener contraseñas, números de tarjeta ni datos sensibles, porque cualquier parte que reciba el token puede leer el Payload.

---

### Pattern Matching
**Categoría:** C# / Programación
**Definición:** Característica de C# que permite verificar el tipo y estructura de un objeto en una expresión `is` o `switch`. En el contexto del back-template se usa para discriminar entre tipos concretos de Response e interfaces de resultado.
**En el proyecto:** Los Presenters hacen `if (notification is IFailure failure)` y los Controllers hacen `if (result is LoginInvalidCredentialsFailure)`. Mantiene el código legible sin casting explícito.

---

### PKI (Public Key Infrastructure)
**Categoría:** Criptografía / Seguridad
**Definición:** Conjunto de políticas, roles, hardware, software y procedimientos para gestionar certificados digitales y pares de claves pública/privada. Necesario para implementar RS256 en producción de forma segura.
**En el proyecto:** No se usa en el sistema de mensajería (HS256 no requiere PKI). Se mencionó como overhead operacional de RS256  -  una de las razones para preferir HS256 en un monolito modular.

---

### Presenter
**Categoría:** Back-template / Arquitectura
**Definición:** Clase que actúa como puente entre el Handler y el Controller. Implementa `INotificationHandler<TResponse>` y transforma el Response del Handler en el estado del ViewModel que el Controller consulta para construir la respuesta HTTP.
**En el proyecto:** `LoginPresenter`, `RegisterPresenter` y `RefreshTokenPresenter`. Hacen pattern matching sobre interfaces de resultado (`IFailure`, `ISuccess<T>`)  -  nunca sobre tipos concretos  -  para mantener independencia del Controller.

---

## R

### RandomNumberGenerator
**Categoría:** .NET / Criptografía
**Definición:** Clase de `System.Security.Cryptography` que genera números aleatorios criptográficamente seguros. A diferencia de `Random`, sus valores son impredecibles y adecuados para tokens de seguridad, claves y nonces.
**En el proyecto:** Se usa en `JwtTokenService.GenerateRefreshToken()` con `RandomNumberGenerator.GetBytes(64)` para generar 64 bytes seguros que luego se codifican en Base64 para formar el Refresh Token opaco.

---

### RefreshToken (entidad)
**Categoría:** Dominio / Autenticación
**Definición:** Entidad de persistencia que almacena un Refresh Token en la base de datos. Contiene el token opaco, la referencia al credential del agente, la fecha de expiración, un flag de revocación y la fecha de creación. No contiene lógica de negocio.
**En el proyecto:** Se persiste en la tabla `RefreshTokens` de PostgreSQL. La lógica de validación y rotación vive en `RefreshTokenHandler`, no en la entidad.

---

### Refresh Token
**Categoría:** JWT / Autenticación
**Definición:** Token de larga duración (30 días en el proyecto) que el cliente usa para obtener nuevos Access Tokens sin requerir credenciales del usuario. No es un JWT  -  es un token opaco que solo tiene significado al encontrarlo en la base de datos.
**En el proyecto:** Se genera con `GenerateRefreshToken()` (64 bytes aleatorios en Base64). Se persiste en PostgreSQL. Al usarlo, se revoca el anterior y se emite uno nuevo (rotación).

---

### Refresh Token Rotation
**Categoría:** Seguridad / Autenticación
**Definición:** Práctica de seguridad donde cada vez que se usa un Refresh Token para obtener nuevos tokens, el Refresh Token usado se revoca y se emite uno nuevo. Si el token antiguo se usa de nuevo, es señal de un posible robo de sesión (reuse attack).
**En el proyecto:** `RefreshTokenHandler` implementa rotación: llama `RevokeAsync(request.Token)` antes de insertar el nuevo. Si un token revocado se reutiliza, el sistema puede invalidar todos los tokens del agente.

---

### RegisterEmailConflictFailure
**Categoría:** Back-template / Responses
**Definición:** Response concreto de falla que implementa `IConflictFailure`. Se retorna cuando el email enviado en el registro ya está registrado en el sistema. El Controller lo mapea a `409 Conflict`.
**En el proyecto:** `RegisterHandler` lo retorna después de verificar `await _credentials.ExistsByEmailAsync(request.Email, cancellationToken)`. El mensaje informa al usuario que ya existe una cuenta con ese email.

---

### RegisterHandler
**Categoría:** Back-template / Handlers
**Definición:** Handler del caso de uso de registro. Verifica que el tenant exista y esté activo, verifica unicidad del email, hashea la contraseña, crea la credencial, publica un evento de integración para crear el User en el módulo Users y emite tokens.
**En el proyecto:** Es el handler más complejo de autenticación porque además del flujo de tokens publica `UserShouldBeCreatedIntegrationEvent` para que el módulo de Users cree el perfil del agente.

---

### RegisterRequest
**Categoría:** Back-template / Commands
**Definición:** Record sellado con cinco campos: `Email`, `Password`, `TenantId` (long), `FullName` y `Role` (string). Implementa `IRequest<RegisterResponse>`. El `TenantId` es el ID interno del workspace y el `Role` debe ser uno de `"Admin"`, `"Agent"` o `"Viewer"`.
**En el proyecto:** El Controller construye este record desde `RegisterBody` del body del request y lo envía al Mediator. `TenantId` y `Role` se incluyen en el JWT que se emite al finalizar el registro exitoso.

---

### RegisterTenantNotFoundFailure
**Categoría:** Back-template / Responses
**Definición:** Response concreto de falla que implementa `INotFoundFailure`. Se retorna cuando el `TenantId` enviado no corresponde a ningún workspace activo. El Controller lo mapea a `404 Not Found`.
**En el proyecto:** `RegisterHandler` lo retorna si `tenant is null || !tenant.IsActive`. Es la primera validación del flujo de registro  -  antes de verificar el email.

---

### RequireHttpsMetadata
**Categoría:** ASP.NET Core / JWT
**Definición:** Opción de `JwtBearerOptions` que controla si el endpoint de descubrimiento del proveedor de identidad debe ser HTTPS. En producción debe ser `true`; en desarrollo local puede ser `false` para funcionar con HTTP.
**En el proyecto:** Solo aplica en la variante Keycloak: `options.RequireHttpsMetadata = configuration.GetValue<bool>("Keycloak:RequireHttps")`. En la implementación JWT propia no se usa porque no hay endpoint de descubrimiento.

---

### Resource Owner
**Categoría:** OAuth2
**Definición:** En OAuth2, el usuario final que posee los recursos protegidos y concede autorización al cliente para acceder a ellos en su nombre. Es quien ingresa sus credenciales para autenticarse.
**En el proyecto:** El agente del sistema de mensajería (Admin, Agent o Viewer) es el Resource Owner. Ingresa email y contraseña al hacer login para obtener tokens que le permiten acceder a los recursos de la API.

---

### Resource Server
**Categoría:** OAuth2
**Definición:** En OAuth2, el servidor que aloja los recursos protegidos y acepta Access Tokens para autorizar el acceso. Valida el token en cada request antes de procesar la operación.
**En el proyecto:** La API .NET del sistema de mensajería es el Resource Server. Valida el JWT en cada request protegido usando el middleware `AddJwtBearer` con `TokenValidationParameters`.

---

### ResultViewModel
**Categoría:** Back-template / ViewModels
**Definición:** Clase genérica que actúa como contenedor de estado compartido entre el Presenter y el Controller. El Presenter lo puebla con éxito o falla; el Controller lo consulta con `_viewModel.IsSuccess` para decidir el status code HTTP.
**En el proyecto:** `ResultViewModel<AuthController>` se usa en los tres flujos de autenticación. El Presenter llama `_viewModel.Set(success)` o `_viewModel.Fail(message)`, y el Controller retorna el ViewModel completo en la respuesta HTTP.

---

### Reuse Attack
**Categoría:** Seguridad / Autenticación
**Definición:** Ataque donde un actor malicioso reutiliza un Refresh Token robado después de que el usuario legítimo ya lo usó y rotó. La detección ocurre cuando el token ya revocado se intenta usar de nuevo  -  señal inequívoca de compromiso.
**En el proyecto:** La rotación de Refresh Tokens protege contra reuse attacks. Si se detecta uso de un token revocado, la respuesta correcta es invalidar todos los tokens del agente afectado.

---

### RevokeAsync
**Categoría:** Back-template / Repositorios
**Definición:** Método de `IRefreshTokenRepository` que marca un Refresh Token como revocado en la base de datos (`IsRevoked = true`). Es la operación central del logout y la rotación de tokens.
**En el proyecto:** Se llama en `RefreshTokenHandler` como paso 4 del flujo de rotación: revocar el token anterior antes de insertar el nuevo. En el logout, se llama directamente con el token activo del agente.

---

### RFC 7519
**Categoría:** Estándares / JWT
**Definición:** RFC que define el estándar JSON Web Token (JWT), incluyendo la estructura de tres partes, los claims registrados estándar (`sub`, `iss`, `aud`, `exp`, `iat`, `jti`) y los algoritmos de firma soportados.
**En el proyecto:** El módulo de autenticación del CRM implementa los claims estándar del RFC 7519 más claims personalizados (`tenant_id`) siguiendo las convenciones del estándar.

---

### Roles (Admin / Agent / Viewer)
**Categoría:** Autorización / Sistema de mensajería
**Definición:** Los tres roles del sistema de control de acceso del sistema de mensajería. `Admin` tiene acceso completo incluyendo gestión del workspace. `Agent` puede gestionar conversaciones y contactos. `Viewer` tiene acceso de solo lectura.
**En el proyecto:** Se almacenan como string en la credencial y se incluyen en el JWT como `ClaimTypes.Role`. Los endpoints usan `[Authorize(Roles = "Admin")]` o `[Authorize(Roles = "Admin,Agent")]` para controlar el acceso.

---

### RS256
**Categoría:** JWT / Algoritmos
**Definición:** Algoritmo de firma asimétrico para JWT basado en RSA-SHA256. Usa una clave privada para firmar (solo el servidor de auth) y una clave pública para verificar (cualquier servicio). Más seguro en microservicios porque los servicios verificadores no necesitan conocer la clave privada.
**En el proyecto:** No se usa en el sistema de mensajería (monolito modular). Se presentó como alternativa para microservicios o cuando múltiples servicios independientes necesitan validar tokens.

---

## S

### Salt
**Categoría:** Criptografía / Seguridad
**Definición:** Valor aleatorio que se agrega a una contraseña antes de hashearla. Garantiza que dos contraseñas idénticas produzcan hashes distintos, previniendo ataques con rainbow tables. BCrypt incluye el salt automáticamente dentro del hash resultante.
**En el proyecto:** BCrypt maneja el salt de forma transparente. El hash resultante incluye el salt embebido, por lo que `IPasswordHasher.Verify(password, hash)` puede verificar sin parámetros adicionales.

---

### Scoped
**Categoría:** .NET / Dependency Injection
**Definición:** Lifetime de DI donde se crea una instancia por scope, que en ASP.NET Core corresponde a una instancia por request HTTP. Es el lifetime adecuado para servicios que comparten estado dentro de una request, como `DbContext`.
**En el proyecto:** Los repositorios (`IUserCredentialRepository`, `IRefreshTokenRepository`) son Scoped porque dependen de `DbContext` (que es Scoped). Un Singleton no puede depender de un Scoped  -  Captive Dependency.

---

### sessionStorage
**Categoría:** Web / Seguridad
**Definición:** API del navegador similar a `localStorage` pero limitada a la pestaña del navegador  -  los datos se eliminan al cerrar la pestaña. Igualmente accesible desde JavaScript, por lo que comparte la vulnerabilidad a ataques XSS.
**En el proyecto:** También está prohibido para almacenar el Access Token. La misma regla que `localStorage` aplica: el Access Token vive exclusivamente en memoria (variable JS de módulo).

---

### Signature (JWT)
**Categoría:** JWT / Estructura
**Definición:** Tercera parte del JWT, resultado de aplicar el algoritmo de firma (HS256 o RS256) al Header y Payload concatenados con un punto. Garantiza la integridad del token  -  cualquier modificación al Header o Payload invalida la firma.
**En el proyecto:** Se calcula con `HMACSHA256(base64url(header) + "." + base64url(payload), Jwt:Key)`. El middleware `AddJwtBearer` verifica la firma en cada request.

---

### SigningCredentials
**Categoría:** .NET / JWT
**Definición:** Clase de `Microsoft.IdentityModel.Tokens` que encapsula la clave de seguridad y el algoritmo de firma. Se pasa al constructor de `JwtSecurityToken` para indicar cómo debe firmarse el token.
**En el proyecto:** Se crea con `new SigningCredentials(key, SecurityAlgorithms.HmacSha256)` donde `key` es un `SymmetricSecurityKey` construido con la clave `Jwt:Key` codificada en UTF-8.

---

### Singleton
**Categoría:** .NET / Dependency Injection
**Definición:** Lifetime de DI donde se crea una única instancia para todo el ciclo de vida del proceso. Se reutiliza en todos los requests. Adecuado para servicios sin estado o con estado inmutable compartido como configuración.
**En el proyecto:** `JwtTokenService` se registra como Singleton porque solo depende de `IConfiguration` (también Singleton). No tiene estado mutable, por lo que es seguro compartirlo entre requests concurrentes.

---

### SSO (Single Sign-On)
**Categoría:** Autenticación / Seguridad
**Definición:** Mecanismo que permite a un usuario autenticarse una sola vez y acceder a múltiples aplicaciones sin necesidad de ingresar credenciales de nuevo. Requiere un Identity Provider centralizado que gestione la sesión entre aplicaciones.
**En el proyecto:** No implementado en el sistema de mensajería v1. Es una de las ventajas clave de Keycloak  -  permitiría que un agente de la empresa iniciara sesión una vez y accediera a múltiples productos de la suite.

---

### sub (Subject)
**Categoría:** JWT / Claims
**Definición:** Claim estándar (RFC 7519) que identifica de forma única al sujeto del token  -  típicamente el usuario. Debe ser un identificador único e inmutable. Se recomienda usar un GUID público en lugar del ID de base de datos.
**En el proyecto:** Contiene el GUID público del agente (`credential.PublicId`). Se lee en `BaseAuthController` con `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` para identificar al agente sin exponer el ID interno.

---

### SymmetricSecurityKey
**Categoría:** .NET / Criptografía
**Definición:** Clase de `Microsoft.IdentityModel.Tokens` que representa una clave criptográfica simétrica. Se construye a partir de un array de bytes (usualmente la clave `Jwt:Key` codificada en UTF-8) y se usa tanto para firmar como para verificar tokens HS256.
**En el proyecto:** Se crea con `new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))` tanto en `JwtTokenService` (para firmar) como en `JwtAuthExtensions` (para validar).

---

## T

### TenantClaimsMiddleware
**Categoría:** Back-template / Middleware
**Definición:** Middleware personalizado que extrae el claim `tenant_id` del JWT del usuario autenticado y lo inyecta en `ITenantContextAccessor`. Debe ejecutarse después de `UseAuthentication` porque requiere que el usuario ya esté autenticado.
**En el proyecto:** Se registra con `app.UseMiddleware<TenantClaimsMiddleware>()` como tercer paso del pipeline de autenticación/autorización. Habilita que EF Core aplique el global query filter de multi-tenancy sin que los handlers accedan al JWT directamente.

---

### tenant_id
**Categoría:** JWT / Claims
**Definición:** Claim personalizado del sistema de mensajería que contiene el ID interno del workspace al que pertenece el agente autenticado. Se incluye en el JWT para evitar consultas adicionales a la base de datos en cada request.
**En el proyecto:** Se incluye como `new Claim("tenant_id", tenantId.ToString())` en cada Access Token. `TenantClaimsMiddleware` lo lee y lo inyecta en `ITenantContextAccessor`, que EF Core usa en el global query filter.

---

### TokenDto
**Categoría:** Back-template / DTOs
**Definición:** Record sellado que representa el contrato de respuesta de los tres casos de uso de autenticación exitosa. Contiene `AccessToken` (string), `RefreshToken` (string) y `ExpiresAtUtc` (DateTime)  -  la fecha de expiración del Access Token.
**En el proyecto:** Es el tipo `Data` en `ISuccess<TokenDto>`. Los tres Responses de éxito (`LoginSuccess`, `RegisterSuccess`, `RefreshTokenSuccess`) lo encapsulan como propiedad `Data`.

---

### TokenValidationParameters
**Categoría:** .NET / JWT
**Definición:** Clase de configuración que define todas las reglas de validación de un JWT: verificar firma (`ValidateIssuerSigningKey`), issuer (`ValidateIssuer`), audience (`ValidateAudience`), lifetime (`ValidateLifetime`) y la tolerancia de tiempo (`ClockSkew`).
**En el proyecto:** Se configura en `JwtAuthExtensions.cs` con todas las validaciones habilitadas y `ClockSkew = TimeSpan.Zero`. Es la configuración central de seguridad del sistema de autenticación.

---

### Transient
**Categoría:** .NET / Dependency Injection
**Definición:** Lifetime de DI donde se crea una nueva instancia cada vez que el servicio es solicitado desde el contenedor. Adecuado para servicios ligeros sin estado que no comparten información entre usos.
**En el proyecto:** Un Singleton puede depender de un Transient (a diferencia de Scoped). Sin embargo, en la práctica del proyecto la mayoría de servicios son Scoped o Singleton por la naturaleza de las operaciones de base de datos.

---

## U

### Unix Timestamp
**Categoría:** JWT / Estándares
**Definición:** Representación de una fecha y hora como el número de segundos transcurridos desde el 1 de enero de 1970 00:00:00 UTC. JWT usa este formato para los claims `exp` (expiración), `iat` (emitido) y `nbf` (no válido antes de).
**En el proyecto:** El claim `exp` del Access Token del CRM se establece a `DateTime.UtcNow.AddMinutes(60)` convertido internamente a Unix Timestamp por `JwtSecurityToken`. El middleware verifica este valor en cada request.

---

### UseAuthentication
**Categoría:** ASP.NET Core / Middleware
**Definición:** Método de extensión que agrega el middleware de autenticación al pipeline. Inspecciona el header `Authorization`, extrae el token Bearer, lo valida y puebla `HttpContext.User` con los claims del token.
**En el proyecto:** Debe registrarse antes de `UseAuthorization` en `Program.cs`. Sin él, `HttpContext.User` no tiene claims y los atributos `[Authorize]` no funcionan correctamente.

---

### UseAuthorization
**Categoría:** ASP.NET Core / Middleware
**Definición:** Método de extensión que agrega el middleware de autorización al pipeline. Verifica que el usuario autenticado (ya establecido por `UseAuthentication`) tiene los permisos necesarios para acceder al endpoint solicitado.
**En el proyecto:** Va inmediatamente después de `UseAuthentication`. Aplica los atributos `[Authorize]`, `[Authorize(Roles = "...")]` y `[Authorize(Policy = "...")]` configurados en los controladores.

---

### User Secrets
**Categoría:** .NET / Configuración
**Definición:** Mecanismo de .NET para almacenar configuración sensible en desarrollo fuera del directorio del proyecto, evitando que secretos sean incluidos accidentalmente en el control de versiones. Los valores se almacenan en el perfil del usuario del sistema operativo.
**En el proyecto:** Se usa para almacenar `Jwt:Key` en desarrollo con `dotnet user-secrets set "Jwt:Key" "..."`. En producción, el secreto viene de AWS Secrets Manager inyectado como variable de entorno.

---

### UserShouldBeCreatedIntegrationEvent
**Categoría:** Back-template / Eventos
**Definición:** Evento de integración publicado por `RegisterHandler` después de crear exitosamente una credencial. Contiene el GUID público, TenantId, nombre completo, email y rol del nuevo agente. Permite al módulo de Users crear el perfil sin acoplamiento directo.
**En el proyecto:** `RegisterHandler` lo publica con `_mediator.Publish(new UserShouldBeCreatedIntegrationEvent(...))`. El módulo de Users escucha este evento para crear la entidad `User` correspondiente.

---

## V

### ValidateAudience
**Categoría:** .NET / JWT
**Definición:** Parámetro de `TokenValidationParameters` que habilita la verificación del claim `aud` del JWT. Cuando es `true`, el validador rechaza cualquier token cuyo `aud` no coincida con `ValidAudience`.
**En el proyecto:** Se establece en `true` en `JwtAuthExtensions.cs`. El valor esperado es `"mensajeria-client"` configurado en `Jwt:Audience`. Previene que tokens de otros sistemas sean aceptados por la API del CRM.

---

### ValidateIssuer
**Categoría:** .NET / JWT
**Definición:** Parámetro de `TokenValidationParameters` que habilita la verificación del claim `iss` del JWT. Cuando es `true`, el validador rechaza tokens emitidos por un issuer diferente al configurado en `ValidIssuer`.
**En el proyecto:** Se establece en `true`. El issuer esperado es `"mensajeria-api"` configurado en `Jwt:Issuer`. Garantiza que solo tokens emitidos por la propia API sean aceptados.

---

### ValidateIssuerSigningKey
**Categoría:** .NET / JWT
**Definición:** Parámetro de `TokenValidationParameters` que habilita la verificación de la firma del JWT usando la clave configurada en `IssuerSigningKey`. Sin esta validación, cualquier JWT con el formato correcto sería aceptado sin importar si fue firmado con la clave correcta.
**En el proyecto:** Se establece en `true` con `IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))`. Es la validación de seguridad más crítica del sistema.

---

### ValidateLifetime
**Categoría:** .NET / JWT
**Definición:** Parámetro de `TokenValidationParameters` que habilita la verificación del claim `exp` del JWT. Cuando es `true`, el validador rechaza tokens expirados. La tolerancia se controla con `ClockSkew`.
**En el proyecto:** Se establece en `true` con `ClockSkew = TimeSpan.Zero`. Cualquier token con `exp` menor a `DateTime.UtcNow` es rechazado inmediatamente sin tolerancia adicional.

---

### ViewModel
**Categoría:** Back-template / Arquitectura
**Definición:** Objeto de estado que el Presenter puebla y el Controller consulta para construir la respuesta HTTP. Separa la lógica de decisión del Presenter de la lógica de serialización del Controller. En el back-template se implementa con `ResultViewModel<TController>`.
**En el proyecto:** El Controller verifica `_viewModel.IsSuccess` para decidir el status code HTTP y pasa el ViewModel completo como body de la respuesta. El cliente recibe la estructura del ViewModel en JSON.

---

## W

### Workspace
**Categoría:** Sistema de mensajería / Dominio
**Definición:** Término de dominio del sistema de mensajería que representa a una empresa cliente (tenant). Cada workspace tiene sus propios agentes, conversaciones, contactos y configuración de canales, completamente aislados de otros workspaces.
**En el proyecto:** El `tenant_id` en el JWT identifica el workspace del agente autenticado. Todo el aislamiento de datos en EF Core se hace con base en este identificador a través de `ITenantContextAccessor`.

---

## X

### XSS (Cross-Site Scripting)
**Categoría:** Seguridad Web / OWASP
**Definición:** Vulnerabilidad web donde un atacante inyecta scripts maliciosos en páginas vistas por otros usuarios. Si un Access Token está almacenado en `localStorage`, un ataque XSS puede robarlo con una simple línea de JavaScript.
**En el proyecto:** Es la razón principal por la que el Access Token nunca se almacena en `localStorage` ni `sessionStorage`. Al mantenerlo en memoria (variable JS), un ataque XSS no puede exfiltrarlo directamente.

---

*Glosario generado para el módulo Material Extra  -  JWT  -  Autenticación*
*Dev Notes  -  Raptor Dev Services*
