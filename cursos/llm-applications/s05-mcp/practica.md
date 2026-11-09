# Práctica - S05 - Model Context Protocol

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 2 h 30 min

---

## Objetivo

Exponer el catálogo de cursos como un **servidor MCP en modo lectura**, por Streamable HTTP, protegido con un token, y probarlo desde un cliente. Al final tienes tres proyectos en la solución: el asistente de las sesiones anteriores (`Asistente.Core` y `Asistente.Cli`) y un servidor nuevo, `Asistente.Mcp`, que cualquier cliente compatible puede usar.

La regla del curso sigue: **si no lo viste fallar, no lo probaste.** En esta práctica la falla que tienes que ver es el rechazo: una llamada sin credencial debe recibir 401.

## Antes de empezar

- La etapa 4 compila y sus pruebas pasan: `dotnet test --solution AsistenteCursos.slnx -- --filter-not-trait "Categoria=Eval"`.
- No necesitas llave de API para esta práctica: el servidor MCP no habla con ningún modelo.
- Tendrás dos terminales abiertas: una para el servidor y otra para el cliente.

---

## Paso 1 - Agregar el servidor a la solución (15 min)

El servidor es un proyecto web de ASP.NET Core. Se agrega a la solución y sus paquetes se fijan en el archivo central de versiones, como todos los demás.

`AsistenteCursos.slnx`:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Asistente.Cli/Asistente.Cli.csproj" />
    <Project Path="src/Asistente.Core/Asistente.Core.csproj" />
    <Project Path="src/Asistente.Mcp/Asistente.Mcp.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Asistente.Pruebas/Asistente.Pruebas.csproj" />
  </Folder>
</Solution>
```

`Directory.Packages.props` agrega el SDK de MCP para el servidor y para el cliente, y el paquete de pruebas en memoria:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Una sola versión fijada por paquete, para todo el repositorio. -->
    <PackageVersion Include="Anthropic" Version="13.6.0" />
    <PackageVersion Include="Npgsql" Version="10.0.3" />
    <PackageVersion Include="Pgvector" Version="0.3.2" />
    <PackageVersion Include="ModelContextProtocol.AspNetCore" Version="2.4.0" />
    <PackageVersion Include="ModelContextProtocol.Core" Version="2.4.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.14" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.11.0" />
    <PackageVersion Include="xunit.v3" Version="4.0.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="4.0.1" />
  </ItemGroup>
</Project>
```

`src/Asistente.Mcp/Asistente.Mcp.csproj` (el catálogo se copia junto al ejecutable):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol.AspNetCore" />
  </ItemGroup>
  <ItemGroup>
    <None Update="datos/catalogo.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

El CLI va a ser cliente MCP, así que también necesita el paquete. `src/Asistente.Cli/Asistente.Cli.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol.Core" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../Asistente.Core/Asistente.Core.csproj" />
  </ItemGroup>
</Project>
```

**Lo que debe pasar:** `dotnet restore AsistenteCursos.slnx` termina sin errores. Todavía no compila: faltan los archivos del servidor.

---

## Paso 2 - El catálogo, las herramientas y el recurso (30 min)

Los datos: un catálogo de ejemplo. Crea `src/Asistente.Mcp/datos/catalogo.json`:

```json
{
  "cursos": [
    {
      "id": "llm-applications",
      "titulo": "Aplicaciones con LLM: de la API al agente",
      "subtitulo": "Prompts, RAG, herramientas, MCP y seguridad en C#",
      "estado": "disponible",
      "unidades": [
        { "id": "s01-llm-fundamentals", "numero": 1, "titulo": "Qué es un LLM y cómo elegirlo", "minutos": 300 },
        { "id": "s02-prompts-structured-output", "numero": 2, "titulo": "Prompts como código y salida estructurada", "minutos": 300 },
        { "id": "s03-rag", "numero": 3, "titulo": "RAG: que responda con sus documentos", "minutos": 300 },
        { "id": "s04-tools-agents", "numero": 4, "titulo": "Herramientas y agentes", "minutos": 300 },
        { "id": "s05-mcp", "numero": 5, "titulo": "Model Context Protocol", "minutos": 300 },
        { "id": "s06-security-operations", "numero": 6, "titulo": "Seguridad y operación", "minutos": 300 }
      ]
    },
    {
      "id": "linux-for-developers",
      "titulo": "Linux para desarrolladores",
      "subtitulo": "La terminal, permisos, procesos y servicios",
      "estado": "proximamente",
      "unidades": [
        { "id": "s01-terminal", "numero": 1, "titulo": "La terminal sin miedo", "minutos": 300 },
        { "id": "s02-permisos", "numero": 2, "titulo": "Usuarios, grupos y permisos", "minutos": 300 }
      ]
    }
  ]
}
```

`src/Asistente.Mcp/Catalogo.cs` lo carga una vez al arrancar:

```csharp
using System.Text.Json;

namespace Asistente.Mcp;

public sealed record Unidad(string Id, int Numero, string Titulo, int Minutos);

public sealed record Curso(string Id, string Titulo, string Subtitulo, string Estado, IReadOnlyList<Unidad> Unidades);

/// <summary>El catálogo de cursos, en modo lectura. Se carga una vez al arrancar.</summary>
public sealed class Catalogo(IReadOnlyList<Curso> cursos)
{
    public IReadOnlyList<Curso> Cursos => cursos;

    public Curso? Buscar(string id) => cursos.FirstOrDefault(c => c.Id == id);

    public static Catalogo Cargar(string ruta)
    {
        var archivo = JsonSerializer.Deserialize<Archivo>(File.ReadAllText(ruta), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"El catálogo {ruta} está vacío.");
        return new Catalogo(archivo.Cursos);
    }

    private sealed record Archivo(IReadOnlyList<Curso> Cursos);
}
```

Las herramientas. Fíjate en tres cosas: cada descripción dice **cuándo** usarla, las anotaciones `ReadOnly` e `Idempotent` son pistas para el cliente (no un permiso), y un error de negocio sale como `McpException`, con un mensaje que el modelo puede usar para corregirse. `src/Asistente.Mcp/HerramientasCatalogo.cs`:

```csharp
using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Asistente.Mcp;

/// <summary>Herramientas de solo lectura. ReadOnly e Idempotent son pistas para el cliente, no un permiso.</summary>
[McpServerToolType]
public sealed class HerramientasCatalogo(Catalogo catalogo)
{
    [McpServerTool(Name = "listar_cursos", ReadOnly = true, Idempotent = true)]
    [Description("Lista los cursos del catálogo con su id, título, estado y número de unidades. Úsala para saber qué cursos existen.")]
    public IReadOnlyList<ResumenCurso> ListarCursos() =>
        catalogo.Cursos.Select(c => new ResumenCurso(c.Id, c.Titulo, c.Estado, c.Unidades.Count)).ToList();

    [McpServerTool(Name = "obtener_curso", ReadOnly = true, Idempotent = true)]
    [Description("Devuelve un curso con todas sus unidades. Úsala cuando ya conoces el id del curso.")]
    public Curso ObtenerCurso([Description("Id del curso, por ejemplo llm-applications.")] string id) =>
        catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'. Usa listar_cursos para ver los ids.");

    [McpServerTool(Name = "buscar_unidades", ReadOnly = true, Idempotent = true)]
    [Description("Busca, en todos los cursos, las unidades cuyo título contiene el texto dado.")]
    public IReadOnlyList<UnidadEncontrada> BuscarUnidades([Description("Texto a buscar, por ejemplo RAG.")] string texto) =>
        catalogo.Cursos
            .SelectMany(c => c.Unidades.Select(u => new UnidadEncontrada(c.Id, u.Id, u.Titulo, u.Minutos)))
            .Where(u => u.Titulo.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();
}

public sealed record ResumenCurso(string Id, string Titulo, string Estado, int Unidades);

public sealed record UnidadEncontrada(string Curso, string Unidad, string Titulo, int Minutos);
```

El mismo catálogo como recurso, con una URI plantilla. Un recurso lo elige la aplicación cliente; una herramienta la elige el modelo. `src/Asistente.Mcp/RecursosCatalogo.cs`:

```csharp
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Asistente.Mcp;

/// <summary>Un recurso lo elige la aplicación cliente para dárselo al modelo; una herramienta la elige el modelo.</summary>
[McpServerResourceType]
public sealed class RecursosCatalogo(Catalogo catalogo)
{
    [McpServerResource(UriTemplate = "catalogo://cursos/{id}", Name = "curso", MimeType = "application/json")]
    [Description("Un curso del catálogo, completo, en JSON.")]
    public string Curso(string id) =>
        JsonSerializer.Serialize(catalogo.Buscar(id) ?? throw new McpException($"No existe el curso '{id}'."));
}
```

---

## Paso 3 - La autenticación por token (25 min)

El servidor guarda la **huella SHA-256** del token, nunca el token. Compara huellas en tiempo constante, para que nadie pueda adivinar el token midiendo cuánto tarda en fallar. `src/Asistente.Mcp/AutenticacionPorToken.cs`:

```csharp
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Asistente.Mcp;

public sealed class OpcionesToken : AuthenticationSchemeOptions
{
    /// <summary>SHA-256 del token, en hexadecimal. El servidor guarda la huella, nunca el secreto.</summary>
    public string HuellaDelToken { get; set; } = "";
}

/// <summary>Valida "Authorization: Bearer &lt;token&gt;" comparando huellas en tiempo constante.</summary>
public sealed class AutenticacionPorToken(IOptionsMonitor<OpcionesToken> opciones, ILoggerFactory registro, UrlEncoder codificador)
    : AuthenticationHandler<OpcionesToken>(opciones, registro, codificador)
{
    public const string Esquema = "TokenDeAcceso";
    private const string Prefijo = "Bearer ";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? cabecera = Request.Headers.Authorization;
        if (cabecera is null || !cabecera.StartsWith(Prefijo, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.NoResult());

        byte[] presentada = SHA256.HashData(Encoding.UTF8.GetBytes(cabecera[Prefijo.Length..].Trim()));
        byte[] esperada = Convert.FromHexString(Options.HuellaDelToken);
        if (!CryptographicOperations.FixedTimeEquals(presentada, esperada))
            return Task.FromResult(AuthenticateResult.Fail("Token inválido."));

        var identidad = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "cliente-mcp"), new Claim("scope", "cursos:leer")], Esquema);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema)));
    }
}
```

`src/Asistente.Mcp/Program.cs` registra la autenticación, el servidor MCP con su transporte HTTP y el endpoint `/mcp` con `RequireAuthorization`. La última línea, `public partial class Program;`, existe para que las pruebas puedan levantar el servidor en memoria.

```csharp
using Asistente.Mcp;

var builder = WebApplication.CreateBuilder(args);

var huella = builder.Configuration["MCP_TOKEN_SHA256"];
if (string.IsNullOrWhiteSpace(huella) || huella.Length != 64 || !huella.All(Uri.IsHexDigit))
    throw new InvalidOperationException("Define MCP_TOKEN_SHA256 con el SHA-256 (64 caracteres hexadecimales) del token.");

builder.Services.AddSingleton(Catalogo.Cargar(Path.Combine(AppContext.BaseDirectory, "datos", "catalogo.json")));
builder.Services
    .AddAuthentication(AutenticacionPorToken.Esquema)
    .AddScheme<OpcionesToken, AutenticacionPorToken>(AutenticacionPorToken.Esquema, o => o.HuellaDelToken = huella);
builder.Services.AddAuthorization();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<HerramientasCatalogo>()
    .WithResources<RecursosCatalogo>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();   // sin credencial válida: 401 antes de tocar el protocolo
app.Run();

public partial class Program;
```

**Lo que debe pasar:** `dotnet build AsistenteCursos.slnx` termina con 0 errores y 0 advertencias.

---

## Paso 4 - Generar el token y levantar el servidor (15 min)

En la **terminal del servidor**:

```bash
export MCP_TOKEN=$(openssl rand -hex 32)
export MCP_TOKEN_SHA256=$(printf %s "$MCP_TOKEN" | shasum -a 256 | cut -d' ' -f1)
echo "$MCP_TOKEN"     # cópialo: lo necesita la terminal del cliente
dotnet run --project src/Asistente.Mcp -- --urls http://localhost:5100
```

Usa `printf %s`, no `echo`: `echo` agrega un salto de línea, la huella sale distinta y todo da 401.

**Lo que debe pasar:** el servidor escribe `Now listening on: http://localhost:5100`.

Prueba también el camino malo: detén el servidor (Ctrl+C), corre `unset MCP_TOKEN_SHA256` y vuelve a arrancarlo. Debe negarse a arrancar con este mensaje:

```text
System.InvalidOperationException: Define MCP_TOKEN_SHA256 con el SHA-256 (64 caracteres hexadecimales) del token.
```

Un servidor que arranca sin su configuración de seguridad es peor que uno que no arranca. Vuelve a exportar la huella y deja el servidor corriendo.

---

## Paso 5 - Un cliente MCP en C# (20 min)

El CLI gana el comando `mcp`: se conecta, lista las herramientas y llama `listar_cursos`. No usa ningún modelo; sirve para ver el servidor como lo ve cualquier anfitrión. `src/Asistente.Cli/Comandos/ClienteMcp.cs`:

```csharp
using System.Net;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Asistente.Cli.Comandos;

/// <summary>Un cliente MCP mínimo: lista las herramientas del servidor y llama una.</summary>
public static class ClienteMcp
{
    public static async Task<int> EjecutarAsync(string[] args, CancellationToken ct)
    {
        var url = Environment.GetEnvironmentVariable("MCP_URL") ?? "http://localhost:5100/mcp";
        var token = args.Contains("--sin-token") ? null : Environment.GetEnvironmentVariable("MCP_TOKEN");

        var opciones = new HttpClientTransportOptions { Endpoint = new Uri(url), Name = "asistente-cli" };
        if (token is not null)
            opciones.AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" };

        try
        {
            await using var cliente = await McpClient.CreateAsync(new HttpClientTransport(opciones), cancellationToken: ct);
            foreach (var h in await cliente.ListToolsAsync(cancellationToken: ct))
                Console.WriteLine($"- {h.Name}: {h.Description}");

            var resultado = await cliente.CallToolAsync("listar_cursos", new Dictionary<string, object?>(), cancellationToken: ct);
            foreach (var bloque in resultado.Content.OfType<TextContentBlock>())
                Console.WriteLine(bloque.Text);
            return resultado.IsError == true ? 7 : 0;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            Console.Error.WriteLine("El servidor rechazó la conexión: 401, sin credencial válida.");
            return 6;
        }
    }
}
```

Y una línea más en el `switch` de `src/Asistente.Cli/Program.cs`:

```csharp
    return args[0] switch
    {
        "comparar" => await Comparar.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "extraer" => await Extraer.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "ingestar" => await Ingestar.EjecutarAsync(args[1..], cancelar.Token),
        "preguntar" => await Preguntar.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "agente" => await Agente.EjecutarAsync(cliente, args[1..], cancelar.Token),
        "mcp" => await ClienteMcp.EjecutarAsync(args[1..], cancelar.Token),
        _ => Desconocido(args[0]),
    };
```

En la **terminal del cliente** (pega el token que imprimió el servidor):

```bash
export MCP_TOKEN=<SU_TOKEN>
export MCP_URL=http://localhost:5100/mcp
dotnet run --project src/Asistente.Cli -- mcp
```

**Lo que debe pasar:**

```text
- listar_cursos: Lista los cursos del catálogo con su id, título, estado y número de unidades. Úsala para saber qué cursos existen.
- obtener_curso: Devuelve un curso con todas sus unidades. Úsala cuando ya conoces el id del curso.
- buscar_unidades: Busca, en todos los cursos, las unidades cuyo título contiene el texto dado.
[{"id":"llm-applications","titulo":"Aplicaciones con LLM: de la API al agente","estado":"disponible","unidades":6},{"id":"linux-for-developers","titulo":"Linux para desarrolladores","estado":"proximamente","unidades":2}]
```

---

## Paso 6 - El mismo servidor desde otros clientes (15 min)

El punto de MCP es que el servidor no sabe quién lo usa. Conéctalo a por lo menos uno de estos dos.

**MCP Inspector**, una interfaz web para explorar servidores:

```bash
npx @modelcontextprotocol/inspector
```

En la interfaz elige el transporte Streamable HTTP, la URL `http://localhost:5100/mcp` y agrega la cabecera `Authorization` con el valor `Bearer <SU_TOKEN>`. Lista las herramientas, llama `obtener_curso` con `llm-applications` y lee el recurso `catalogo://cursos/llm-applications`.

**Claude Code**, desde la terminal del cliente:

```bash
claude mcp add --transport http cursos http://localhost:5100/mcp \
  --header "Authorization: Bearer $MCP_TOKEN"
```

Pregúntale: "¿Qué cursos hay en el catálogo y cuántas unidades tiene cada uno?". Debe usar `listar_cursos`. Pídele después un curso con un id que no existe: el modelo recibe el error con su mensaje y debería recurrir a `listar_cursos`.

**Lo que debe pasar:** los dos clientes ven las mismas tres herramientas que vio el CLI.

---

## Paso 7 - Las pruebas en memoria (20 min)

Probar contra un servidor corriendo depende de puertos y de procesos que alguien olvida apagar. `WebApplicationFactory` levanta el servidor en memoria, dentro de la prueba.

El proyecto de pruebas gana dos referencias. `tests/Asistente.Pruebas/Asistente.Pruebas.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="ModelContextProtocol.Core" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Asistente.Core/Asistente.Core.csproj" />
    <ProjectReference Include="../../src/Asistente.Mcp/Asistente.Mcp.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

`tests/Asistente.Pruebas/ServidorMcpPruebas.cs`: tres llamadas sin credencial válida (sin cabecera, con un token equivocado y con otro esquema) que deben recibir 401, y un cliente MCP real con el token correcto.

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using ModelContextProtocol.Client;

namespace Asistente.Pruebas;

/// <summary>El servidor MCP en memoria: sin puertos y sin red.</summary>
public class ServidorMcpPruebas
{
    private const string Token = "token-de-prueba-que-solo-existe-en-esta-prueba";

    private static WebApplicationFactory<Program> Servidor() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.UseSetting("MCP_TOKEN_SHA256", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Token)))));

    private static StringContent Inicializar() => new("""
        {"jsonrpc":"2.0","id":1,"method":"initialize",
         "params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"prueba","version":"1.0"}}}
        """, Encoding.UTF8, "application/json");

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer token-equivocado")]
    [InlineData("Basic no-es-un-token")]
    public async Task Sin_credencial_valida_responde_401(string? autorizacion)
    {
        await using var servidor = Servidor();
        var http = servidor.CreateClient();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = Inicializar() };
        peticion.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (autorizacion is not null) peticion.Headers.TryAddWithoutValidation("Authorization", autorizacion);

        using var respuesta = await http.SendAsync(peticion, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_token_un_cliente_MCP_lista_y_llama_herramientas()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var servidor = Servidor();
        var http = servidor.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        var transporte = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp") }, http, ownsHttpClient: false);

        await using var cliente = await McpClient.CreateAsync(transporte, cancellationToken: ct);
        var herramientas = await cliente.ListToolsAsync(cancellationToken: ct);
        var inexistente = await cliente.CallToolAsync("obtener_curso",
            new Dictionary<string, object?> { ["id"] = "no-existe" }, cancellationToken: ct);

        Assert.Contains(herramientas, h => h.Name == "listar_cursos");
        Assert.True(inexistente.IsError);
    }
}
```

```bash
dotnet test --solution AsistenteCursos.slnx -- --filter-class "Asistente.Pruebas.ServidorMcpPruebas"
```

**Lo que debe pasar:** `total: 4, failed: 0, succeeded: 4`.

---

## Paso 8 - Verlo fallar (20 min)

Dos fallas. Las dos se ven con tus ojos y se anotan en la evidencia.

**Falla 1: una llamada sin credencial.** En la terminal del cliente, con el servidor corriendo:

```bash
dotnet run --project src/Asistente.Cli -- mcp --sin-token
MCP_TOKEN=token-equivocado dotnet run --project src/Asistente.Cli -- mcp
```

Las dos deben terminar así, con código de salida 6:

```text
El servidor rechazó la conexión: 401, sin credencial válida.
```

**Falla 2: la protección quitada.** En `src/Asistente.Mcp/Program.cs`, quita `.RequireAuthorization()` de la línea de `MapMcp` y corre las pruebas del paso 7. Las tres pruebas de 401 deben caer:

```text
failed Asistente.Pruebas.ServidorMcpPruebas.Sin_credencial_valida_responde_401(autorizacion: null)
  Expected: Unauthorized
  Actual:   OK
```

`Actual: OK` quiere decir que, sin esa línea, cualquiera que llegue al puerto usa el servidor. Si al quitarla las pruebas siguen verdes, tus pruebas no estaban probando la autorización. Regresa la línea y confirma que vuelven a pasar.

---

## Evidencia que entregas

- [ ] Captura o texto de la salida del paso 5: las tres herramientas y el JSON de `listar_cursos`.
- [ ] Captura del servidor usado desde MCP Inspector o desde Claude Code (paso 6).
- [ ] La salida de `mcp --sin-token` con el mensaje de 401 (paso 8, falla 1).
- [ ] La salida de las pruebas en rojo sin `RequireAuthorization`, y en verde con ella (paso 8, falla 2).
- [ ] El token **no** aparece en ningún archivo del repositorio: `git grep "$MCP_TOKEN"` no devuelve nada.

## Checklist del facilitador

- [ ] Todos levantaron el servidor y lo usaron desde el CLI.
- [ ] Todos vieron el servidor negarse a arrancar sin `MCP_TOKEN_SHA256`.
- [ ] Todos vieron las tres pruebas de 401 caer sin `RequireAuthorization`.
- [ ] Quedó claro por qué `ReadOnly` no reemplaza a la autorización.
