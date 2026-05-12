# Configuración en .NET y Docker

Extracto del Manual Práctico del Stack — secciones 1.2 y 1.3.


.NET tiene un sistema de configuración por capas que combina múltiples fuentes. La configuración se lee como un árbol; cualquier fuente puede sobreescribir a la anterior.

Orden de precedencia (de menor a mayor)

- appsettings.json — la base, va al repo, valores neutros.

- appsettings.{Environment}.json — overrides por ambiente: appsettings.Development.json, appsettings.Production.json.

- User Secrets (solo en Development) — secretos locales del dev, NO van al repo.

- Variables de entorno — sobreescriben todo lo anterior.

- Argumentos de línea de comandos — la última palabra.

Cómo leerlas con IOptions tipado (recomendado)

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>// 1. Define la clase POCO que mapea la sección</p>
<p>public sealed class JwtOptions</p>
<p>{</p>
<p>public const string SectionName = "Jwt";</p>
<p>public string Issuer { get; init; } = string.Empty;</p>
<p>public string Audience { get; init; } = string.Empty;</p>
<p>public int AccessTokenLifetimeMinutes { get; init; } = 15;</p>
<p>public int RefreshTokenLifetimeDays { get; init; } = 30;</p>
<p>public string SigningKey { get; init; } = string.Empty;</p>
<p>}</p>
<p>// 2. Regístralo en Program.cs con validación</p>
<p>builder.Services</p>
<p>.AddOptions&lt;JwtOptions&gt;()</p>
<p>.Bind(builder.Configuration.GetSection(JwtOptions.SectionName))</p>
<p>.ValidateDataAnnotations()</p>
<p>.ValidateOnStart();</p>
<p>// 3. Inyecta donde lo necesites</p>
<p>public class TokenService</p>
<p>{</p>
<p>private readonly JwtOptions _options;</p>
<p>public TokenService(IOptions&lt;JwtOptions&gt; options) {</p>
<p>_options = options.Value;</p>
<p>}</p>
<p>}</p></td>
</tr>
</tbody>
</table>

Variables de entorno para sobreescribir configuración

En .NET, una variable de entorno con doble underscore (\_\_) navega secciones del JSON. Esta es la forma estándar en Linux/Docker.

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>env</em></td>
</tr>
<tr>
<td><p>ASPNETCORE_ENVIRONMENT=Production</p>
<p>ConnectionStrings__Default="Host=db.prod;Port=5432;..."</p>
<p>Jwt__SigningKey="..."</p>
<p>Stripe__SecretKey="sk_live_..."</p>
<p>Cors__AllowedOrigins__0="https://app.taskflow.com"</p>
<p>Logging__LogLevel__Default="Warning"</p></td>
</tr>
</tbody>
</table>

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><p><strong>Diferencia Linux vs Windows</strong></p>
<p>En Windows también funciona el doble underscore. En Linux (donde corre Docker) es la única forma porque ':' no es válido en nombres de variables. Usa siempre __ para máxima portabilidad.</p></td>
</tr>
</tbody>
</table>

User Secrets en desarrollo local

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>bash</em></td>
</tr>
<tr>
<td><p>cd src/TaskFlow.Api</p>
<p>dotnet user-secrets init</p>
<p>dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."</p>
<p>dotnet user-secrets set "Jwt:SigningKey" "super-long-random-key-for-dev"</p>
<p>dotnet user-secrets list</p>
<p>dotnet user-secrets remove "Stripe:SecretKey"</p>
<p># Los secretos viven en:</p>
<p># Windows: %APPDATA%\Microsoft\UserSecrets\&lt;id&gt;\secrets.json</p>
<p># Linux/Mac: ~/.microsoft/usersecrets/&lt;id&gt;/secrets.json</p></td>
</tr>
</tbody>
</table>


## 1.3 Variables en Docker y docker-compose

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>docker-compose.yml</em></td>
</tr>
<tr>
<td><p>services:</p>
<p>api:</p>
<p>image: registry/taskflow-api:${APP_VERSION:-latest}</p>
<p>environment:</p>
<p>ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}</p>
<p>ConnectionStrings__Default: ${DB_CONNECTION}</p>
<p>Jwt__SigningKey: ${JWT_SIGNING_KEY}</p>
<p>Stripe__SecretKey: ${STRIPE_SECRET_KEY}</p>
<p>env_file:</p>
<p>- .env.docker</p>
<p>ports:</p>
<p>- "${API_PORT:-8080}:8080"</p>
<p>depends_on:</p>
<p>- postgres</p>
<p>postgres:</p>
<p>image: postgres:17-alpine</p>
<p>environment:</p>
<p>POSTGRES_DB: ${DB_NAME}</p>
<p>POSTGRES_USER: ${DB_USER}</p>
<p>POSTGRES_PASSWORD: ${DB_PASSWORD}</p>
<p>volumes:</p>
<p>- pgdata:/var/lib/postgresql/data</p>
<p>volumes:</p>
<p>pgdata:</p></td>
</tr>
</tbody>
</table>



> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.2 Managing Configuration and Secrets

---

*Rogelio Arriaga Gonzalez*
