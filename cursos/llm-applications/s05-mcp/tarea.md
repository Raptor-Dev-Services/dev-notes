# Tarea - S05 - Model Context Protocol

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 4 a 5 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/11-vibe-coding/09-mcp.md` | Clientes, servidores y transportes; Herramientas, recursos y prompts | 25 min |
| `dev-notes/11-vibe-coding/09-mcp.md` | Un servidor en C#; Autorización | 25 min |
| `dev-notes/11-vibe-coding/09-mcp.md` | Elicitation y sampling; La revisión 2026-07-28 | 20 min |

### Libros (por capítulo)

| Libro | Capítulos | Para qué |
|-------|-----------|----------|
| *Learn Model Context Protocol with TypeScript* (Christoffer Noring, 2025) | Caps. 1 a 3, 7 y 9 a 11 | Los conceptos de MCP de la sesión. Anota, por capítulo, qué cambió en la revisión 2026-07-28 |

El libro está en TypeScript y describe revisiones anteriores de la especificación (con el saludo `initialize` y con sampling). Léelo por los conceptos; el código de la tarea es C# con `ModelContextProtocol` 2.4.0.

---

## Parte 2 - Ejercicio: una herramienta de escritura con su autorización (2 a 3 horas)

El servidor de la práctica solo lee. Ahora agrega **una** herramienta que escribe, por ejemplo `inscribir_alumno(curso, correo)`, que agregue la inscripción a una lista en memoria o a una tabla. Lo que se califica no es la escritura: es que **solo pueda escribir quien debe, y solo después de confirmar**.

### Requisito 1 - Un permiso distinto para escribir

- Una política de autorización `cursos:escribir` que exija el claim `scope` con ese valor.
- Un **segundo** token, con su propia huella en la configuración (por ejemplo `MCP_TOKEN_ESCRITURA_SHA256`), que al autenticarse reciba el scope `cursos:escribir`. El token de la práctica sigue recibiendo solo `cursos:leer`.
- La herramienta lleva `[Authorize(Policy = "cursos:escribir")]` y el servidor registra `AddAuthorizationFilters()`.

El patrón, ya compilado:

```csharp
[McpServerTool(Name = "inscribir_alumno", Destructive = false, Idempotent = true)]
[Authorize(Policy = "cursos:escribir")]
[Description("Inscribe a un alumno en un curso. Pide confirmación a la persona antes de escribir.")]
public static async Task<string> InscribirAlumnoAsync(
    McpServer servidor, string curso, string correo, CancellationToken ct)
```

```csharp
builder.Services.AddAuthorization(o =>
    o.AddPolicy("cursos:escribir", p => p.RequireClaim("scope", "cursos:escribir")));
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .AddAuthorizationFilters()   // aplica los [Authorize] de cada herramienta
    .WithTools<HerramientasDeEscritura>();
```

### Requisito 2 - Confirmación antes de escribir

Antes de escribir, la herramienta pide confirmación por elicitation. Si la persona no acepta, o si el cliente no soporta elicitation, **no se escribe nada** y la herramienta lo dice.

```csharp
var respuesta = await servidor.ElicitAsync<ConfirmacionInscripcion>(
    $"¿Inscribo a {correo} en {curso}?", cancellationToken: ct);
if (respuesta.Action != "accept" || respuesta.Content?.Confirmo != true)
    return "La persona no confirmó; no se inscribió a nadie.";
```

### Requisito 3 - Validación de la entrada

El esquema no basta: valida que el curso exista en el catálogo y que el correo tenga forma de correo. Un error de negocio sale como `McpException` con un mensaje útil para el modelo.

### Requisito 4 - Las pruebas

En `ServidorMcpPruebas` (o en una clase nueva), con `WebApplicationFactory`:

1. Con el token de **lectura**, `tools/list` **no** incluye `inscribir_alumno`.
2. Con el token de **lectura**, llamar `inscribir_alumno` por su nombre **no** inscribe a nadie (el resultado es un error).
3. Con el token de **escritura**, `tools/list` sí la incluye.

### Requisito 5 - Verlo fallar

Quita `AddAuthorizationFilters()`, corre las pruebas y anota cuáles caen y con qué mensaje. Regrésalo y confirma que vuelven a pasar. Si nada cae al quitarlo, tus pruebas no prueban la autorización: corrígelas antes de entregar.

---

## Entrega

Un pull request en tu repositorio del curso, con el título:

```
tarea/s05 - herramienta de escritura con autorización - [Tu nombre]
```

En la descripción del PR:

- [ ] La salida de las pruebas en verde.
- [ ] La salida de las pruebas en rojo sin `AddAuthorizationFilters()` (requisito 5).
- [ ] Una captura de la confirmación por elicitation en un cliente que la soporte (MCP Inspector o Claude Code), aceptando una vez y rechazando otra.
- [ ] Un párrafo: ¿por qué la confirmación vive en el servidor y no solo en el agente?

---

## Criterio de aceptación

- [ ] El token de lectura no ve ni puede usar la herramienta de escritura.
- [ ] Sin confirmación no se escribe nada, y el mensaje lo dice.
- [ ] Ningún token ni huella real está en el repositorio: todo sale de variables de entorno.
- [ ] La solución compila con 0 errores y 0 advertencias.
- [ ] Las pruebas de la práctica siguen pasando.

---

## Preguntas frecuentes

**¿Puedo usar sampling para que el servidor redacte el mensaje de bienvenida?**
No. Sampling quedó en desuso en la revisión 2026-07-28 de la especificación y el SDK 2.4.0 lo marca obsoleto (MCP9005). Si el servidor necesita un modelo, lo llama él, con su propia llave y su propio tope de gasto.

**¿Tengo que montar OAuth completo?**
No para esta tarea. La especificación pide OAuth para servidores HTTP en producción; aquí basta con dos tokens con scopes distintos. Lo que sí tiene que quedar es la separación de permisos y su prueba.

**¿El token de escritura puede ser el mismo para todo el equipo?**
En la tarea, sí. En producción, cada cliente y cada persona tiene el suyo, para poder revocarlo sin afectar a los demás.

---

## Preparación para S06

- Trae funcionando la etapa 3: el asistente que responde con documentos (`ingestar` y `preguntar`).
- En la sesión 6 lo vamos a atacar con un documento envenenado.
