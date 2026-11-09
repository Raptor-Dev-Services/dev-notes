<!-- _class: lead -->

# S05 - Model Context Protocol

### Aplicaciones con LLM: de la API al agente

**Rogelio Arriaga - Raptor Dev Services**

<!-- Instructor: abre con la pregunta "¿cuántas veces reescribiríamos buscar_tickets si mañana también lo quiere usar Claude Code?". La respuesta honesta es "una por cliente", y de eso trata la sesión. -->

---

## Agenda del día

- **Parte 1.** Qué es MCP y qué problema resuelve
- **Parte 2.** Transportes: stdio y Streamable HTTP
- **Parte 3.** Las primitivas: herramientas, recursos y prompts
- **Parte 4.** El servidor del curso en C#
- **Parte 5.** Autorización: el servidor es una API
- **Parte 6.** Elicitation, y por qué ya no sampling
- **Parte 7.** La revisión 2026-07-28 de la especificación
- **Práctica:** el catálogo de cursos por MCP, y verlo rechazar

<!-- Instructor: la teoría ocupa unas dos horas; la práctica, el resto. Si el grupo va lento, la Parte 7 se puede dar como lectura. -->

---

> MCP separa al que sabe del dato del que conversa: el mismo servidor sirve a varios clientes.

<!-- Instructor: deja la frase un momento en pantalla. "El que sabe del dato" es nuestro servidor; "el que conversa" es Claude Code, un IDE o nuestro propio CLI. -->

---

## De dónde venimos

En la sesión 4 el agente tenía `buscar_tickets` y `crear_ticket` **dentro del mismo proceso**.

- Funciona, pero solo para ese programa.
- Si Claude Code, un IDE u otro equipo quieren las mismas herramientas, cada uno las reescribe.
- Cada copia trae su propia autorización, sus propios errores y su propia versión del SQL.

**Hoy:** sacamos las capacidades a un servidor que cualquier cliente compatible puede usar.

<!-- Instructor: pide al grupo que cuente cuántos lugares tendrían que cambiar si mañana se agrega una columna a tickets y hay tres clientes con su propia copia. -->

---

## Al terminar puedes

**Exponer datos y acciones de tu sistema a cualquier asistente compatible con MCP.**

- Escribir un servidor MCP en C# con herramientas y recursos.
- Elegir transporte: stdio para lo local, HTTP para lo compartido.
- Protegerlo: sin credencial válida, 401 antes de tocar el protocolo.
- Separar lectura de escritura, y pedir confirmación antes de escribir.

<!-- Instructor: esta lámina es el contrato de la sesión. Al cierre se regresa a ella y se marca qué sí quedó. -->

---

<!-- _class: lead -->

# Parte 1
## Qué es MCP

---

## El problema de N por M

Sin un protocolo común, cada aplicación con LLM se integra a mano con cada sistema.

| | Sin MCP | Con MCP |
|---|---|---|
| 3 clientes y 4 sistemas | 12 integraciones a la medida | 3 clientes y 4 servidores |
| Agregar un cliente | integrarlo con los 4 sistemas | que hable MCP |
| Agregar un sistema | integrarlo con los 3 clientes | escribir un servidor |

Es la misma idea de un puerto estándar: una vez que el enchufe es común, se conecta cualquier cosa.

<!-- Instructor: bájalo al caso del curso: tickets, catálogo y documentos son tres sistemas; el CLI, Claude Code y un IDE son tres clientes. -->

---

## Qué es MCP

**Model Context Protocol** es un protocolo abierto que define cómo una aplicación con un modelo descubre y usa las capacidades de un programa externo.

- Mensajes **JSON-RPC 2.0**: petición, respuesta y notificación.
- El servidor **anuncia** lo que ofrece; el cliente **lo descubre** y lo usa.
- No depende del modelo ni del proveedor: el servidor nunca habla con el LLM.

<!-- Instructor: subraya la última línea. Nuestro servidor no tiene llave de API ni sabe qué modelo hay del otro lado. -->

---

## Anfitrión, cliente y servidor

| Pieza | Qué es | Ejemplo |
|---|---|---|
| **Anfitrión** (host) | La aplicación que tiene el modelo y la conversación | Claude Code, un IDE, nuestro CLI |
| **Cliente** | La conexión que el anfitrión abre con un servidor; una por servidor | `McpClient` en C# |
| **Servidor** | El programa que expone capacidades | `Asistente.Mcp`, el catálogo de cursos |

El anfitrión decide qué llega al modelo. El servidor decide qué permite hacer.

<!-- Instructor: la confusión más común es llamar "cliente" al modelo. El modelo no es cliente de nada: lo usa el anfitrión. -->

---

## Quién decide qué

- El **servidor** decide qué existe y quién puede usarlo.
- El **anfitrión** decide qué servidores conecta y qué le muestra al modelo.
- El **modelo** decide qué herramienta llamar, dentro de lo que le muestran.
- La **persona** decide si autoriza lo que escribe.

Cada capa tiene su control. Si una falla, las otras siguen ahí.

<!-- Instructor: conecta con la sesión 4: la confirmación humana vivía en el bucle del cliente. Hoy veremos que también puede vivir en el servidor. -->

---

## Herramientas locales contra un servidor MCP

| | Herramienta en el proceso (S04) | Servidor MCP (S05) |
|---|---|---|
| Quién la usa | Solo ese programa | Cualquier cliente compatible |
| Dónde vive la autorización | En el programa | En el servidor, una vez |
| Cómo se despliega | Con la aplicación | Por separado, con su versión |
| Costo extra | Ninguno | Un proceso o un servicio más |

<!-- Instructor: no vendas MCP como reemplazo universal. Es una frontera más, y las fronteras cuestan. -->

---

## Cuándo no usar MCP

- La herramienta solo la usa **tu** aplicación y nadie más la va a pedir.
- Necesitas latencia mínima y la llamada extra pesa.
- No puedes operar otro servicio: monitoreo, versiones, credenciales.

Una función local con su esquema, como en la sesión 4, sigue siendo la opción más simple.

<!-- Instructor: la regla práctica: si hoy hay un solo cliente y no se ve un segundo, no hace falta MCP todavía. -->

---

<!-- _class: lead -->

# Parte 2
## Transportes: stdio y Streamable HTTP

---

## stdio: el cliente lanza el proceso

El anfitrión arranca el servidor como subproceso y hablan por **entrada y salida estándar**.

```csharp
var builder = Host.CreateApplicationBuilder(args);
// En stdio, la salida estándar es el canal del protocolo: los registros van a stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<HerramientasDeHora>();
await builder.Build().RunAsync();
```

<!-- Instructor: es el transporte de las herramientas locales: un servidor que lee archivos del equipo, una base local. Sin red, sin puertos. -->

---

## La trampa de stdio

En stdio, **todo lo que escribes en stdout es protocolo**.

- Un `Console.WriteLine("arrancando...")` corrompe el canal.
- El cliente recibe algo que no es JSON-RPC y corta la conexión.
- El error que ves dice "conexión cerrada", no "escribiste en la consola".

**Regla:** en un servidor stdio, los registros van a stderr. Siempre.

<!-- Instructor: si alguien quiere verlo fallar, que agregue un Console.WriteLine al arranque del servidor stdio de ejemplo y lo conecte a un cliente. -->

---

## Streamable HTTP: un servicio compartido

El servidor escucha en un endpoint HTTP, por ejemplo `POST /mcp`.

- Responde con JSON o con un flujo de eventos (SSE) cuando la respuesta tarda.
- Lo usan varios clientes a la vez, desde otras máquinas.
- Se despliega como cualquier API: contenedor, TLS, monitoreo.
- **Necesita autenticación.** Un endpoint abierto es una puerta abierta.

<!-- Instructor: es el transporte de la práctica. ASP.NET Core nos da autenticación, autorización y pruebas en memoria sin inventar nada. -->

---

## stdio o HTTP

| | stdio | Streamable HTTP |
|---|---|---|
| Dónde corre | En la máquina del anfitrión | Donde lo despliegues |
| Cuántos clientes | Uno: el que lo lanzó | Muchos |
| Credenciales | Las del usuario local | Hay que autenticar cada petición |
| Para qué | Archivos, herramientas locales | Datos de la empresa, servicios |

<!-- Instructor: pregunta: "¿el catálogo de cursos de la academia es local o compartido?". Compartido: HTTP. -->

---

<!-- _class: lead -->

# Parte 3
## Las primitivas: herramientas, recursos y prompts

---

## Tres primitivas, tres dueños

| Primitiva | La elige | Para qué |
|---|---|---|
| **Herramienta** | El modelo | Hacer algo o consultar algo: `listar_cursos` |
| **Recurso** | La aplicación | Darle contexto al modelo: `catalogo://cursos/{id}` |
| **Prompt** | La persona | Plantillas que el usuario invoca, como un comando |

La diferencia no es técnica: es **quién decide** que se use.

<!-- Instructor: basta con la tabla y el criterio de "quién decide"; el detalle de cada primitiva viene en las láminas siguientes. -->

---

## Una herramienta en C#

```csharp
[McpServerToolType]
public sealed class HerramientasCatalogo(Catalogo catalogo)
{
    [McpServerTool(Name = "obtener_curso", ReadOnly = true, Idempotent = true)]
    [Description("Devuelve un curso con todas sus unidades. Úsala cuando ya conoces el id del curso.")]
    public Curso ObtenerCurso([Description("Id del curso, por ejemplo llm-applications.")] string id) =>
        catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'. Usa listar_cursos para ver los ids.");
}
```

El SDK genera el esquema JSON de la entrada a partir de la firma del método.

<!-- Instructor: el constructor recibe Catalogo por inyección de dependencias, igual que un controlador. -->

---

## La descripción es el contrato con el modelo

El modelo decide **solo** con el nombre, la descripción y el esquema.

- Di **cuándo** usarla, no solo qué hace: "Úsala cuando ya conoces el id".
- Describe cada parámetro con un ejemplo real.
- Si dos herramientas se parecen, la descripción dice cuál va primero.

Una mala descripción no truena en compilación: se ve como un agente que llama la herramienta equivocada.

<!-- Instructor: mismo principio que en la sesión 4 con crear_ticket: "úsala solo si buscar_tickets no encontró uno abierto". -->

---

## Anotaciones: pistas, no permisos

`ReadOnly`, `Idempotent` y `Destructive` le dicen al cliente qué esperar.

- Un cliente puede pedir confirmación solo para las destructivas.
- Un cliente puede ignorarlas por completo.
- Un servidor malicioso puede mentir en ellas.

**La seguridad no se apoya en una anotación.** Se apoya en la autorización del servidor.

<!-- Instructor: pregunta trampa para el quiz: "marcamos la herramienta como ReadOnly, ¿ya no hace falta autorización?". No. -->

---

## Errores que el modelo puede leer

```csharp
catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'. Usa listar_cursos para ver los ids.");
```

- `McpException` vuelve al cliente como un resultado con `IsError`, con tu mensaje.
- El modelo lo lee y corrige: busca los ids y vuelve a intentar.
- Cualquier otra excepción llega como un error genérico: no filtra detalles internos.

<!-- Instructor: es el mismo patrón que el bucle de la sesión 4: los errores de negocio regresan al modelo, los internos se registran y se ocultan. -->

---

## Recursos: contexto que elige la aplicación

```csharp
[McpServerResource(UriTemplate = "catalogo://cursos/{id}", Name = "curso", MimeType = "application/json")]
[Description("Un curso del catálogo, completo, en JSON.")]
public string Curso(string id) =>
    JsonSerializer.Serialize(catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'."));
```

- Se identifican con una URI; `{id}` es una plantilla.
- La aplicación los lista y decide cuáles adjuntar a la conversación.

<!-- Instructor: en Claude Code los recursos se adjuntan con una mención; en nuestro CLI se leen con ReadResourceAsync. -->

---

## ¿Herramienta o recurso?

| Pregunta | Herramienta | Recurso |
|---|---|---|
| ¿Lo pide el modelo a media conversación? | Sí | No |
| ¿Tiene parámetros que el modelo decide? | Sí | La URI |
| ¿La persona lo adjunta a propósito? | No | Sí |
| ¿Puede cambiar algo? | Puede | Nunca: es lectura |

En la práctica exponemos el catálogo de las dos formas.

<!-- Instructor: no hay respuesta única; la pregunta útil es quién debe decidir que ese dato entre al contexto. -->

---

## Prompts: plantillas que elige la persona

- El servidor ofrece plantillas con nombre y argumentos.
- El anfitrión las muestra como comandos que la persona invoca.
- Sirven para estandarizar tareas: "resume este curso para un correo".

No las usamos en la práctica: con herramientas y recursos cubrimos el caso. Existen para que el servidor también comparta **cómo pedir**, no solo **qué datos**.

<!-- Instructor: en el SDK se declaran con McpServerPromptType y McpServerPrompt, con el mismo estilo que las herramientas. -->

---

<!-- _class: lead -->

# Parte 4
## El servidor del curso en C#

---

## Lo que vamos a construir

```
src/Asistente.Mcp/          ASP.NET Core, transporte HTTP
  Program.cs                autenticación, autorización y MapMcp
  Catalogo.cs               el catálogo, cargado de datos/catalogo.json
  HerramientasCatalogo.cs   listar_cursos, obtener_curso, buscar_unidades
  RecursosCatalogo.cs       catalogo://cursos/{id}
  AutenticacionPorToken.cs  Bearer con huella SHA-256
src/Asistente.Cli/          el comando mcp: un cliente en C#
tests/Asistente.Pruebas/    el servidor en memoria con WebApplicationFactory
```

Modo **lectura**: nada en esta etapa escribe.

<!-- Instructor: el proyecto es el mismo de las sesiones anteriores; se agrega un tercer proyecto a la solución. -->

---

## El registro del servidor

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<HerramientasCatalogo>()
    .WithResources<RecursosCatalogo>();
```

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();   // sin credencial válida: 401 antes de tocar el protocolo
```

<!-- Instructor: MapMcp devuelve un endpoint de ASP.NET Core normal; por eso RequireAuthorization funciona igual que en cualquier API. -->

---

## Versiones fijadas

| Paquete | Versión | Para qué |
|---|---|---|
| `ModelContextProtocol.AspNetCore` | 2.4.0 | El servidor por HTTP |
| `ModelContextProtocol.Core` | 2.4.0 | El cliente en el CLI y en las pruebas |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.14 | El servidor en memoria para las pruebas |

Viven en `Directory.Packages.props`: una sola versión por paquete, para todo el repositorio.

<!-- Instructor: el SDK de MCP cambia rápido y ha renombrado clases entre versiones mayores. Por eso se fija y se actualiza a propósito, con las pruebas corriendo. -->

---

## Un cliente en C#

```csharp
var opciones = new HttpClientTransportOptions { Endpoint = new Uri(url), Name = "asistente-cli" };
if (token is not null)
    opciones.AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" };
```

```csharp
await using var cliente = await McpClient.CreateAsync(new HttpClientTransport(opciones), cancellationToken: ct);
foreach (var h in await cliente.ListToolsAsync(cancellationToken: ct))
    Console.WriteLine($"- {h.Name}: {h.Description}");
```

<!-- Instructor: este cliente no tiene modelo. Sirve para probar el servidor como lo vería cualquier anfitrión: descubrir y llamar. -->

---

## Llamar una herramienta

```csharp
var resultado = await cliente.CallToolAsync("listar_cursos", new Dictionary<string, object?>(), cancellationToken: ct);
foreach (var bloque in resultado.Content.OfType<TextContentBlock>())
    Console.WriteLine(bloque.Text);
```

- El resultado es una lista de bloques de contenido, como en la API de mensajes.
- `IsError` dice si la herramienta falló; el texto dice por qué.

<!-- Instructor: haz notar el paralelo con la sesión 4: allá el modelo pedía tool_use; aquí el cliente hace tools/call. El agente que conecta las dos cosas es el anfitrión. -->

---

## El mismo servidor, tres clientes

```bash
# 1. El CLI del curso
MCP_URL=http://localhost:5100/mcp dotnet run --project src/Asistente.Cli -- mcp

# 2. MCP Inspector: una interfaz web para explorar servidores
npx @modelcontextprotocol/inspector

# 3. Claude Code
claude mcp add --transport http cursos http://localhost:5100/mcp \
  --header "Authorization: Bearer $MCP_TOKEN"
```

Ninguno de los tres sabe cómo está hecho el catálogo. Los tres ven lo mismo.

<!-- Instructor: si hay tiempo, conecta Claude Code en vivo y pídele "¿qué cursos hay y cuántas unidades tiene cada uno?". Es la frase de la sesión hecha demostración. -->

---

<!-- _class: lead -->

# Parte 5
## Autorización: el servidor es una API

---

## Un servidor MCP por HTTP es una API pública

- Cualquiera que llegue al puerto puede pedir `tools/list` y `tools/call`.
- No importa que "solo lo use Claude Code": el protocolo no autentica por ti.
- Una herramienta de solo lectura también filtra datos.

**Sin credencial válida, el servidor responde 401 antes de procesar el mensaje.**

<!-- Instructor: es la prueba que vamos a ver fallar en la práctica. -->

---

## Lo que pide la especificación

Para transportes HTTP, la autorización de MCP se basa en **OAuth**:

- El servidor MCP es un **recurso protegido**.
- Publica sus metadatos de recurso protegido para que el cliente descubra a qué servidor de autorización pedir el token (así lo describe la revisión 2025-11-25, la que cita el SDK).
- El cliente obtiene un token con los permisos (scopes) que necesita y lo manda en cada petición.

En producción se delega en un proveedor de identidad. En el curso simulamos la última parte: un token Bearer.

<!-- Instructor: el SDK de C# trae soporte para los metadatos de recurso protegido y el flujo OAuth del cliente; montar un servidor de autorización queda fuera del alcance de las 5 horas. -->

---

## El token del curso: Bearer con huella

```csharp
byte[] presentada = SHA256.HashData(Encoding.UTF8.GetBytes(cabecera[Prefijo.Length..].Trim()));
byte[] esperada = Convert.FromHexString(Options.HuellaDelToken);
if (!CryptographicOperations.FixedTimeEquals(presentada, esperada))
    return Task.FromResult(AuthenticateResult.Fail("Token inválido."));
```

- El servidor guarda la **huella SHA-256**, nunca el secreto.
- La comparación es en **tiempo constante**: no se puede adivinar el token midiendo cuánto tarda en fallar.

<!-- Instructor: pregunta: "si alguien lee la configuración del servidor, ¿puede conectarse?". No: tiene la huella, no el token. -->

---

## Generar el token y su huella

```bash
# Una sola vez, en tu máquina
export MCP_TOKEN=$(openssl rand -hex 32)
export MCP_TOKEN_SHA256=$(printf %s "$MCP_TOKEN" | shasum -a 256 | cut -d' ' -f1)
```

- `MCP_TOKEN` lo tiene el cliente.
- `MCP_TOKEN_SHA256` lo tiene el servidor.
- Ninguno de los dos va al repositorio.

<!-- Instructor: printf %s y no echo: echo agrega un salto de línea y la huella sale distinta. Es el error más común del paso. -->

---

## Lectura y escritura no se mezclan

El token del curso trae el permiso `cursos:leer`. Para escribir hace falta otro.

```csharp
[McpServerTool(Name = "inscribir_alumno", Destructive = false, Idempotent = true)]
[Authorize(Policy = "cursos:escribir")]
```

```csharp
o.AddPolicy("cursos:escribir", p => p.RequireClaim("scope", "cursos:escribir")));
```

```csharp
.AddAuthorizationFilters()   // aplica los [Authorize] de cada herramienta
```

<!-- Instructor: es la tarea. Sin AddAuthorizationFilters, el atributo Authorize en la herramienta no hace nada: hay que verlo fallar. -->

---

## Mínimo privilegio, otra vez

- El token de lectura no debe ver la herramienta de escritura en `tools/list`.
- Si la llama por nombre, la respuesta es un error de autorización, no una inscripción.
- Cada cliente recibe el token con los permisos que su trabajo necesita, y nada más.

Es la misma idea de la sesión 4: el agente podía consultar sin preguntar y escribir solo con confirmación.

<!-- Instructor: OWASP llama "agencia excesiva" a darle al modelo más permisos de los que la tarea pide. Lo vemos completo en la sesión 6. -->

---

## Probarlo sin red

```csharp
private static WebApplicationFactory<Program> Servidor() =>
    new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        b.UseSetting("MCP_TOKEN_SHA256", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Token)))));
```

- El servidor corre en memoria: sin puertos, sin procesos sueltos.
- Una prueba manda `initialize` sin token, con token equivocado y con otro esquema: las tres esperan 401.
- Otra conecta un `McpClient` real con el token correcto.

<!-- Instructor: el token de la prueba es una cadena descriptiva que solo existe en la prueba; no tiene formato de secreto real. -->

---

<!-- _class: lead -->

# Parte 6
## Elicitation, y por qué ya no sampling

---

## Elicitation: el servidor le pregunta a la persona

A media herramienta, el servidor pide un dato o una confirmación, y el cliente se lo muestra a la persona.

```csharp
var respuesta = await servidor.ElicitAsync<ConfirmacionInscripcion>(
    $"¿Inscribo a {correo} en {curso}?", cancellationToken: ct);
if (respuesta.Action != "accept" || respuesta.Content?.Confirmo != true)
    return "La persona no confirmó; no se inscribió a nadie.";
```

<!-- Instructor: el tipo ConfirmacionInscripcion define el formulario. El SDK arma el esquema con sus propiedades. -->

---

## Las reglas de elicitation

- El esquema solo admite **tipos primitivos**: texto, número, booleano, enumeración.
- La persona puede **aceptar**, **rechazar** o **cancelar**: las tres se manejan.
- Si el cliente no soporta elicitation, la llamada falla: el servidor decide qué hacer, y lo seguro es no escribir.
- **Nunca** se pide por elicitation una contraseña ni un número de tarjeta.

<!-- Instructor: la última regla es de la especificación y de sentido común: un formulario dentro de una conversación es el lugar perfecto para un phishing. -->

---

## Confirmar en el cliente o en el servidor

| | En el bucle del agente (S04) | En el servidor (S05) |
|---|---|---|
| Quién la impone | El programa que llama | El dueño del dato |
| Si el cliente se equivoca | La escritura pasa | El servidor sigue preguntando |
| Funciona con cualquier anfitrión | No | Sí, si soporta elicitation |

Para una escritura que importa, la confirmación vive **del lado que escribe**.

<!-- Instructor: no se excluyen. En el proyecto final el agente confirma en su bucle y el servidor confirma por su lado: dos capas. -->

---

## Sampling: qué era

La especificación permitía que el **servidor** le pidiera al **modelo del cliente** que generara texto.

- La idea: un servidor sin llave de API que, aun así, puede redactar o resumir.
- El anfitrión mostraba la petición a la persona y elegía el modelo.

Aparece en los libros y tutoriales de 2025, porque era parte de la especificación desde su primera revisión.

<!-- Instructor: explícalo como historia, no como receta. Lo que sigue es por qué no lo vamos a usar. -->

---

## Sampling quedó en desuso

En la revisión **2026-07-28** de la especificación, sampling se marcó en desuso (SEP-2577).

- El SDK de C# 2.4.0 marca sus tipos y métodos como obsoletos: aviso **MCP9005**.
- Con `TreatWarningsAsErrors`, el proyecto ni compila si lo usas.
- Puede desaparecer en una versión futura.

**Decisión del curso:** no se construye nada nuevo sobre sampling. Si el servidor necesita un modelo, lo llama él, con su propia llave y su propio tope de gasto.

<!-- Instructor: esta es una corrección a la propuesta original del curso, que listaba sampling entre los temas. Lo verificamos al compilar: el aviso MCP9005 apareció en cuanto lo usamos. -->

---

<!-- _class: lead -->

# Parte 7
## La revisión 2026-07-28 de la especificación

---

## La especificación tiene fechas

El SDK de C# 2.4.0 soporta estas revisiones:

| Revisión | Nota |
|---|---|
| 2024-11-05 | La primera publicada |
| 2025-03-26 | Streamable HTTP reemplaza a HTTP con SSE |
| 2025-06-18 | Elicitation |
| 2025-11-25 | La que cita el SDK para la autorización |
| **2026-07-28** | **La que prefiere el cliente de C# 2.4.0** |

Si el servidor es más viejo, el cliente cae a una revisión anterior.

<!-- Instructor: no memoricen la tabla; lo importante es que la especificación cambia y que el cliente y el servidor negocian. -->

---

## Qué cambió en 2026-07-28

Lo que documenta el SDK de C#:

- **Sin el saludo `initialize`:** el cliente usa `server/discover` y manda versión, datos del cliente y capacidades en los metadatos de cada petición (SEP-2575).
- **Se quitó** `resources/subscribe` y `resources/unsubscribe`.
- **Elicitation** viaja con *Multi Round-Trip Requests*: el servidor responde "necesito esto" y el cliente vuelve con la respuesta (SEP-2322).
- **Sampling** quedó en desuso (SEP-2577).

<!-- Instructor: no hace falta entender cada SEP. Basta saber que un tutorial que empieza con "primero el cliente manda initialize" describe una revisión anterior. -->

---

## Por qué te importa

- Los libros y tutoriales de 2025 enseñan el saludo `initialize` y sampling, porque así era la especificación.
- El concepto sigue igual: descubrir, llamar, autorizar.
- El código cambia: nombres de clases, mensajes, capacidades.

**Por eso:** el libro se lee por los conceptos, y el código del curso fija `ModelContextProtocol` 2.4.0 y se prueba.

<!-- Instructor: nuestra prueba de 401 manda un initialize con la revisión 2025-06-18 a propósito: la autenticación ocurre antes del protocolo, así que la versión no importa para ese caso. -->

---

<!-- _class: lead -->

# Práctica
## El catálogo de cursos por MCP

---

## La práctica en seis pasos

1. Agregar `Asistente.Mcp` a la solución y fijar los paquetes.
2. El catálogo, las herramientas y el recurso.
3. La autenticación por token y `RequireAuthorization`.
4. Levantar el servidor y probarlo con el CLI.
5. Las pruebas en memoria con `WebApplicationFactory`.
6. **Verlo fallar:** sin token, y sin `RequireAuthorization`.

<!-- Instructor: el paso 4 se hace con dos terminales: una con el servidor, otra con el cliente. -->

---

## Si no lo viste fallar, no lo probaste

Dos fallas que tienes que ver con tus ojos:

- `mcp --sin-token` debe decir **"401, sin credencial válida"**.
- Si quitas `RequireAuthorization`, las tres pruebas `Sin_credencial_valida_responde_401` deben caer: **esperado Unauthorized, obtenido OK**.

Si al quitar la protección todo sigue verde, la prueba no estaba probando nada.

<!-- Instructor: haz que lo quiten de verdad, corran las pruebas, vean el rojo y lo regresen. Es la evidencia más valiosa de la sesión. -->

---

## Errores comunes

| Error | Síntoma | Arreglo |
|---|---|---|
| `Console.WriteLine` en un servidor stdio | El cliente corta la conexión | Registros a stderr |
| `echo` en lugar de `printf %s` para la huella | Todo da 401 | La huella sin salto de línea |
| Confiar en `ReadOnly` | Herramientas sin autorización | La autorización vive en el servidor |
| `[Authorize]` sin `AddAuthorizationFilters` | El token de lectura escribe | Registrar los filtros y probarlo |
| Copiar código de sampling de un tutorial | Aviso MCP9005 | Llamar al modelo desde el servidor |

<!-- Instructor: la fila de AddAuthorizationFilters es la trampa de la tarea; que la vean venir. -->

---

## Tarea

**Agregar una herramienta de escritura con su autorización.**

- Política `cursos:escribir` y un segundo token con ese permiso.
- Confirmación por elicitation antes de escribir.
- Una prueba que demuestre que el token de lectura **no la ve ni la puede llamar**.
- Verlo fallar: sin `AddAuthorizationFilters`, esa prueba debe caer.

<!-- Instructor: el detalle completo está en tarea.md. -->

---

## Lecturas

- `dev-notes/11-vibe-coding/09-mcp.md`
- *Learn Model Context Protocol with TypeScript* (Noring, 2025), caps. 1 a 3, 7 y 9 a 11.

El libro está en TypeScript: léelo por los conceptos (primitivas, transportes, seguridad). El código del curso es C# con el SDK 2.4.0.

<!-- Instructor: avisa que el libro describe revisiones anteriores de la especificación; lo vimos en la Parte 7. -->

---

<!-- _class: lead -->

# Preguntas

### Siguiente sesión: Seguridad y operación

<!-- Instructor: adelanta que en la sesión 6 atacamos el asistente de la sesión 3 con un documento envenenado. Que traigan funcionando la etapa 3. -->
