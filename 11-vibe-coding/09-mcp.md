# 09 - MCP: Model Context Protocol

> Fuente: *Learn Model Context Protocol with TypeScript* (Noring, 2025): caps. 1 a 3, 7 y 9 a 11. El libro está en TypeScript y es anterior a la revisión 2026-07-28 de la especificación: se lee por los conceptos. El código de esta nota es C# con el SDK oficial `ModelContextProtocol` 2.4.0 (`ModelContextProtocol.AspNetCore` para el servidor HTTP y `ModelContextProtocol.Core` para el cliente).

## Problema que resuelve

Cada asistente que quiere usar los datos de {{PROJECT_NAME}} necesita su propia integración: una para el chat interno, otra para el asistente de código del equipo, otra para el agente de soporte. Cada una con su forma de describir las herramientas, su autenticación y sus errores. Tres clientes y tres sistemas son nueve integraciones.

MCP (Model Context Protocol) es un protocolo abierto que separa al que sabe del dato del que conversa. El sistema expone sus datos y acciones **una vez**, en un servidor MCP; cualquier aplicación compatible (un IDE, un chat, un agente propio) se conecta a ese servidor como cliente. El mismo servidor sirve a varios clientes, y el mismo cliente habla con varios servidores.

Complementa `06-arquitectura-agentica.md` (cómo se organizan agentes y herramientas) y la sesión de herramientas del curso de aplicaciones con LLM: una herramienta MCP es una herramienta de las de siempre, solo que publicada detrás de un protocolo.

## Clientes, servidores y transportes

| Pieza | Qué es | Ejemplo |
|---|---|---|
| Aplicación anfitriona | La aplicación donde vive el modelo y conversa la persona | Un IDE, un asistente de escritorio, un agente en C# |
| Cliente MCP | El componente de la anfitriona que mantiene la conexión con un servidor | Uno por cada servidor conectado |
| Servidor MCP | El programa que expone herramientas, recursos y prompts | El catálogo de cursos, la base de tickets |

Dos transportes:

| Transporte | Cómo funciona | Cuándo |
|---|---|---|
| **stdio** | El cliente lanza el servidor como proceso y hablan por la entrada y la salida estándar | Herramientas locales de una persona: archivos, git, una base local |
| **Streamable HTTP** | El servidor es un servicio web con un endpoint (por ejemplo `/mcp`); las respuestas pueden venir en streaming | Servicios compartidos por un equipo o expuestos a otros sistemas |

En stdio, la salida estándar **es** el canal del protocolo: cualquier `Console.WriteLine` o registro que caiga ahí corrompe los mensajes. Los registros van a la salida de errores:

```csharp
// En stdio, la salida estándar es el canal del protocolo: los registros van a stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<HerramientasDeHora>();
```

## Herramientas, recursos y prompts

Un servidor ofrece tres tipos de cosas, y la diferencia está en **quién decide usarlas**:

| Primitiva | Quién la elige | Para qué | Ejemplo |
|---|---|---|---|
| Herramienta | El modelo, durante la conversación | Consultar o hacer algo: tiene entrada y efecto | `obtener_curso(id)` |
| Recurso | La aplicación o la persona | Darle contexto al modelo: es un dato con URI | `catalogo://cursos/llm-applications` |
| Prompt | La persona, normalmente desde un menú | Plantillas de instrucciones que el servidor recomienda | "Resume este curso para un alumno nuevo" |

Las herramientas pueden declarar pistas: solo lectura, idempotente, destructiva, mundo abierto. **Son pistas para el cliente, no permisos**: un cliente puede usarlas para decidir si pide confirmación, pero el servidor no puede confiar en que lo haga. La autorización real vive en el servidor.

La descripción de cada herramienta es un prompt: el modelo la lee para decidir cuándo usarla. Conviene decir **cuándo** usarla, no solo qué hace.

## Un servidor en C#

Un servidor HTTP con ASP.NET Core, herramientas en una clase con inyección de dependencias y el endpoint protegido:

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<HerramientasCatalogo>()
    .WithResources<RecursosCatalogo>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();   // sin credencial válida: 401 antes de tocar el protocolo
```

```csharp
[McpServerToolType]
public sealed class HerramientasCatalogo(Catalogo catalogo)
{
    [McpServerTool(Name = "obtener_curso", ReadOnly = true, Idempotent = true)]
    [Description("Devuelve un curso con todas sus unidades. Úsala cuando ya conoces el id del curso.")]
    public Curso ObtenerCurso([Description("Id del curso, por ejemplo llm-applications.")] string id) =>
        catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'. Usa listar_cursos para ver los ids.");
    // ...
}
```

El SDK genera el esquema de entrada desde los parámetros del método y las descripciones desde los atributos `[Description]`. Una `McpException` llega al cliente como un resultado con error y un mensaje útil para el modelo; cualquier otra excepción llega como un error genérico, sin filtrar detalles internos.

Un recurso se declara con una plantilla de URI:

```csharp
[McpServerResource(UriTemplate = "catalogo://cursos/{id}", Name = "curso", MimeType = "application/json")]
[Description("Un curso del catálogo, completo, en JSON.")]
public string Curso(string id) =>
    JsonSerializer.Serialize(catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'."));
```

## Autorización

Un servidor MCP por HTTP es una API: lo que expone lo puede llamar cualquiera que llegue al endpoint. Se protege como cualquier API, en dos niveles.

**Quién se conecta.** La especificación de autorización para transportes HTTP se basa en OAuth 2.1: el servidor publica los metadatos de su recurso protegido y el cliente obtiene un token del servidor de autorización. Para un servicio interno basta un token Bearer validado por un esquema de autenticación de ASP.NET Core. El servidor guarda la **huella** del token, no el token, y compara en tiempo constante:

```csharp
byte[] presentada = SHA256.HashData(Encoding.UTF8.GetBytes(cabecera[Prefijo.Length..].Trim()));
byte[] esperada = Convert.FromHexString(Options.HuellaDelToken);
if (!CryptographicOperations.FixedTimeEquals(presentada, esperada))
    return Task.FromResult(AuthenticateResult.Fail("Token inválido."));
```

**Qué puede hacer.** Leer y escribir son permisos distintos. Con `AddAuthorizationFilters()`, cada herramienta puede llevar su propia política, y una herramienta que el token no autoriza ni siquiera aparece en la lista:

```csharp
[McpServerTool(Name = "inscribir_alumno", Destructive = false, Idempotent = true)]
[Authorize(Policy = "cursos:escribir")]
[Description("Inscribe a un alumno en un curso. Pide confirmación a la persona antes de escribir.")]
```

```csharp
builder.Services.AddAuthorization(o =>
    o.AddPolicy("cursos:escribir", p => p.RequireClaim("scope", "cursos:escribir")));
```

## Elicitation y sampling

**Elicitation** es el servidor pidiéndole información a la persona a través del cliente, a mitad de una operación: un dato que falta, o una confirmación antes de escribir. El SDK construye el formulario desde una clase (solo tipos primitivos):

```csharp
var respuesta = await servidor.ElicitAsync<ConfirmacionInscripcion>(
    $"¿Inscribo a {correo} en {curso}?", cancellationToken: ct);
if (respuesta.Action != "accept" || respuesta.Content?.Confirmo != true)
    return "La persona no confirmó; no se inscribió a nadie.";
```

Es la confirmación humana de un agente, pero puesta del lado de quien escribe: no depende de que el cliente decida preguntar.

**Sampling** permitía que el servidor le pidiera al modelo del cliente que generara texto, sin tener llave propia. **La revisión 2026-07-28 de la especificación lo dejó en desuso** (SEP-2577), y el SDK 2.4.0 marca sus APIs como obsoletas con el aviso `MCP9005`. Se menciona para entender servidores existentes; no se diseña nada nuevo sobre él. Si el servidor necesita un modelo, lo llama con su propia llave y su propio tope de gasto.

## La revisión 2026-07-28

La especificación se versiona por fecha. El cliente de C# 2.4.0 entiende 2024-11-05, 2025-03-26, 2025-06-18, 2025-11-25 y 2026-07-28, prefiere la más reciente y cae a una anterior si el servidor es más viejo. Según la documentación del SDK, la revisión 2026-07-28:

- **Quita el saludo inicial** (`initialize`): el cliente descubre al servidor con `server/discover` y manda la versión del protocolo, su identidad y sus capacidades en los metadatos de cada petición (SEP-2575).
- **Quita la suscripción a recursos** (`resources/subscribe` y `resources/unsubscribe`).
- **Mueve las peticiones del servidor al cliente**, como la elicitation, a un mecanismo de varias vueltas sobre la misma petición (Multi Round-Trip Requests, SEP-2322).
- **Deja en desuso** el sampling y la capacidad de registro.

Lo práctico: fija la versión del SDK, lee sus notas de versión al subirla, y prueba el servidor contra el cliente real que lo va a usar.

## Probarlo

Tres formas, de la más automática a la más manual:

1. **Pruebas con el servidor en memoria.** `WebApplicationFactory` levanta el servidor sin puertos; una prueba manda la petición sin credencial y espera un 401, y otra conecta un cliente MCP real con el token:

```csharp
var transporte = new HttpClientTransport(
    new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp") }, http, ownsHttpClient: false);

await using var cliente = await McpClient.CreateAsync(transporte, cancellationToken: ct);
var herramientas = await cliente.ListToolsAsync(cancellationToken: ct);
```

2. **MCP Inspector**, la herramienta oficial para explorar un servidor a mano: `npx @modelcontextprotocol/inspector`, y se conecta a la URL del servidor con la cabecera de autorización.
3. **Un cliente real.** Por ejemplo, un asistente de código: `claude mcp add --transport http cursos http://localhost:5100/mcp --header "Authorization: Bearer $MCP_TOKEN"`.

La prueba que más importa es la que falla: **una llamada sin credencial debe ser rechazada**. Si quitas `RequireAuthorization()` y la prueba sigue en verde, la prueba no prueba nada.

## Errores comunes

- **Escribir en la salida estándar de un servidor stdio.** El cliente recibe basura y la conexión se cae sin un error claro.
- **Confiar en las pistas** (`ReadOnly`, `Destructive`). Son información para el cliente, no una barrera.
- **Un solo token para leer y escribir.** El asistente que solo consulta termina con permiso de modificar.
- **Exponer un servidor HTTP sin autenticación "porque es interno".** Interno no es lo mismo que autorizado.
- **Guardar el token en el servidor.** Se guarda su huella; el token lo tiene solo el cliente.
- **Mensajes de error con detalles internos.** Lo que devuelve una herramienta lo lee el modelo y, a veces, la persona.
- **Construir sobre sampling.** Quedó en desuso en la revisión 2026-07-28.
- **Herramientas que devuelven tablas enteras.** El resultado entra al contexto del modelo y se paga en tokens.

## Cuándo usar / no usar

| Usar MCP | No usar |
|----------|---------|
| Varios asistentes o agentes necesitan los mismos datos o acciones | Una sola aplicación con dos herramientas propias |
| Quieres que el equipo use tus datos desde su IDE o su chat | La integración es una llamada fija que no decide un modelo |
| El sistema de datos y el asistente los mantienen equipos distintos | Todo vive en el mismo proceso y cambia junto |
| Necesitas un contrato estable entre el que sabe del dato y el que conversa | El costo de operar otro servicio supera el beneficio |

## Glosario

| Término | Definición |
|---------|-----------|
| MCP | Model Context Protocol: protocolo abierto para que aplicaciones con modelos de lenguaje usen datos y acciones de otros sistemas |
| Servidor MCP | programa que expone herramientas, recursos y prompts por el protocolo |
| Cliente MCP | componente de la aplicación anfitriona que mantiene la conexión con un servidor |
| Aplicación anfitriona | aplicación donde conversa la persona y vive el modelo; contiene uno o más clientes |
| stdio | transporte en el que el cliente lanza el servidor como proceso y hablan por entrada y salida estándar |
| Streamable HTTP | transporte en el que el servidor es un servicio web con un endpoint que admite respuestas en streaming |
| Herramienta | operación con entrada que el modelo decide invocar |
| Recurso | dato identificado por una URI que la aplicación elige darle al modelo |
| Prompt (MCP) | plantilla de instrucciones que el servidor ofrece para que la persona la elija |
| Pistas de herramienta | anotaciones como solo lectura o destructiva; informan al cliente, no autorizan |
| Elicitation | petición del servidor a la persona, a través del cliente, de un dato o una confirmación |
| Sampling | petición del servidor al modelo del cliente; en desuso desde la revisión 2026-07-28 |
| Huella del token | resumen SHA-256 del token que guarda el servidor en lugar del token |
| MCP Inspector | herramienta oficial para explorar y probar un servidor MCP a mano |

---

*Rogelio Arriaga Gonzalez*
