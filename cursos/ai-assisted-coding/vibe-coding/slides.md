<!-- _class: lead -->

# Vibe Coding Profesional

## CLAUDE.md  -  AGENTS.md  -  Prompts Efectivos  -  AI Workflow  -  LLMOps

**Dev Notes  -  Raptor Dev Services**
Material extra  -  Mayo 2026

---

<!-- _class: lead -->

# Parte 1

## Vibe Coding  -  Qué es y de qué depende el resultado

---

## ¿Qué es Vibe Coding?

**Vibe Coding** es el uso deliberado de IA generativa como copiloto durante el ciclo completo de desarrollo.

La diferencia con el uso casual:

| Uso casual | Vibe Coding profesional |
|-----------|------------------------|
| "Escríbeme un CRUD" | Prompt con contexto, restricciones y ejemplo del proyecto |
| Copiar y pegar sin leer | Revisar y validar antes de commitear |
| Sesión sin preparación | CLAUDE.md con stack, convenciones y anti-patrones |
| IA decide la arquitectura | Humano diseña, IA implementa |
| Código genérico de Stack Overflow | Código que sigue las convenciones del equipo |

> El resultado de la IA es tan bueno como el contexto que le das.

---

## El dev sigue siendo responsable

El AI no sabe:

- Por qué el negocio tomó ciertas decisiones de arquitectura
- Que su equipo decidió nunca usar `IRepository<T>` genérico
- Que en producción no se usa `:latest` como tag de Docker
- Que el sistema de mensajería tiene restricciones de tenant isolation

**El AI tiene acceso al código  -  no al contexto del negocio.**

```
Humano:   Define qué construir y por qué
          Valida el diseño contra los requisitos del negocio
          Revisa el código generado antes de commitear
          Ejecuta los tests y valida los resultados

AI:       Genera el código siguiendo los patrones del proyecto
          Encuentra edge cases y los cubre en tests
          Explica código existente
          Detecta problemas en code reviews
```

---

## Señales de mal uso de la IA

El AI está fuera de contexto cuando:

- Genera código que no sigue los patrones del proyecto
- Propone cambios en archivos que no debería tocar
- Da respuestas genéricas que ignoran el stack real
- Repite un error que ya se corrigió

**Respuesta correcta:** corregir explícitamente y, si se repite, agregar la restricción a `CLAUDE.md`.

No es un problema del modelo  -  es un problema de contexto.

---

<!-- _class: lead -->

# Parte 2

## CLAUDE.md  -  Instrucciones persistentes para el AI

---

## ¿Qué problema resuelve CLAUDE.md?

Sin instrucciones persistentes, en cada sesión hay que re-explicar:

- El stack y las versiones exactas
- Las convenciones de nombres del equipo
- Qué patrones usar y cuáles están prohibidos
- La estructura de directorios del proyecto

`CLAUDE.md` almacena ese contexto **en el repositorio** y Claude Code lo carga automáticamente al iniciar cada sesión.

**Orden de precedencia:**
1. `~/.claude/CLAUDE.md`  -  instrucciones globales del usuario (todos los proyectos)
2. `CLAUDE.md` en la raíz del repo  -  instrucciones del proyecto
3. `CLAUDE.md` en subdirectorios  -  instrucciones locales

---

## Qué incluir en CLAUDE.md

Cinco secciones mínimas:

| Sección | Contenido |
|---------|-----------|
| **Stack y versiones** | .NET 10, React 19, PostgreSQL 16, Docker |
| **Arquitectura** | Capas, naming conventions, patrón de handlers |
| **Convenciones** | Idioma, Result Pattern, commits, tests |
| **Restricciones** | Qué NO hacer  -  anti-patrones explícitos |
| **Comandos frecuentes** | `dotnet build`, `dotnet test`, `docker compose up` |

> CLAUDE.md es un **onboarding document**, pero para el AI.

---

## Ejemplo: CLAUDE.md del sistema de mensajería

```markdown
# CLAUDE.md  -  Sistema de mensajería

## Stack
- .NET 10 / C# 13 / ASP.NET Core 10
- PostgreSQL 16, Dapper (lecturas), EF Core 10 (escrituras + migrations)
- React 19, Vite 7, Tailwind CSS v4
- Docker, GitHub Actions, AWS ECS Fargate

## Arquitectura  -  Clean Architecture 4 capas
Domain -> Application -> Infrastructure -> WebApi
Un UseCase = una carpeta con Handler, Request, Response
Handlers retornan Result<T>  -  nunca lanzar excepciones de negocio

## Convenciones de nombres
Handler:    GetConversationHandler
Request:    GetConversationRequest
Response:   GetConversationResponse
Repository: IConversationRepository / ConversationRepository
```

---

## Ejemplo: Restricciones del sistema de mensajería en CLAUDE.md

```markdown
## Restricciones  -  NO hacer

- No usar .Result ni .Wait() en código async
- No usar IRepository<T> genérico  -  repositorios específicos por entidad
- No usar :latest como tag de Docker  -  siempre versión semántica (v1.2.3)
- No usar string interpolation en Serilog  -  usar template strings con {Param}
- No registrar servicios Scoped dentro de un Singleton
- No queries SQL inline en handlers  -  siempre en el repositorio
- No modificar Common/ sin discutir primero con el equipo

## Entidades del dominio
Conversation, Contact, Message, Agent, Channel, BotFlow, Workspace

## Comandos
- Build:   dotnet build
- Test:    dotnet test --configuration Release
- Dev:     docker compose up -d && dotnet run --project WebApi
```

---

## Cuándo actualizar CLAUDE.md

Actualizar cuando:

- Se agrega una nueva dependencia o patrón al proyecto
- Cambia la estructura de directorios
- Se establece una nueva convención de equipo
- **El AI repite el mismo error**  -  agregar restricción explícita

| Usar CLAUDE.md | No usar CLAUDE.md |
|---------------|------------------|
| Proyectos con más de 2 sesiones de trabajo con AI | Proyectos de un solo uso o exploración |
| Cuando el AI repite errores por desconocer el contexto | Para instrucciones que cambian cada sesión |
| Convenciones no obvias que el AI no puede inferir del código | Información ya evidente en el código |

---

<!-- _class: lead -->

# Parte 3

## AGENTS.md  -  Agentes especializados

---

## AGENTS.md vs CLAUDE.md

`AGENTS.md` es el mismo concepto pero para agentes que no son Claude Code (OpenAI Codex, GitHub Copilot Workspace, etc.).

```
CLAUDE.md    <- Claude Code lee esto automáticamente
AGENTS.md    <- otros agentes leen esto
```

Si el repositorio se usa con múltiples herramientas AI: **mantener ambos archivos con el mismo contenido**.

**Los agentes van más allá del chat**  -  tienen acceso a herramientas y completan tareas de múltiples pasos sin intervención humana en cada paso.

---

## Chat vs Agente  -  cuándo usar cada uno

| Modo Chat | Modo Agente |
|-----------|------------|
| Preguntas conceptuales | Implementar una feature completa |
| Explicar código ya conocido | Analizar un bug con múltiples archivos |
| Generar un snippet pequeño | Agregar tests a todos los handlers sin tests |
| Tomar una decisión de diseño | Refactorizar un módulo completo |

**Ciclo de un agente (ReAct pattern):**

```
Thought:     "Necesito encontrar qué archivos implementan IConversationRepository"
Action:      grep("IConversationRepository", "*.cs")
Observation: ["Infrastructure/Repositories/Conversations/ConversationRepository.cs"]
Thought:     "Tengo la implementación. Ahora necesito leer el archivo."
Action:      read("Infrastructure/Repositories/Conversations/ConversationRepository.cs")
Answer:      "La implementación está en Infrastructure/Repositories/..."
```

---

## Instrucciones efectivas para agentes

```
 Vago:
"mejora el código"

 Específico:
"refactoriza GetConversationsHandler para usar el patrón de paginación
de GetContactsHandler (Infrastructure/Repositories/Contacts/),
sin cambiar el contrato público del método"

 Sin contexto:
"agrega tests"

 Con contexto:
"agrega tests unitarios al AssignConversationHandler siguiendo
el estilo de AssignAgentHandler.Tests.cs.
Cubrir: éxito, conversación no existe, agente no disponible"

 Sin restricciones:
"optimiza los queries"

 Con restricciones:
"optimiza solo el GetPagedAsync en ConversationRepository.cs.
No cambies la interfaz del repositorio."
```

---

<!-- _class: lead -->

# Parte 4

## Prompts Efectivos  -  Estructura y Anti-patrones

---

## Componentes de un prompt efectivo

| Componente | Descripción | Ejemplo |
|-----------|-------------|---------|
| **Rol / Contexto** | Quién eres y en qué proyecto estás | "Eres un desarrollador .NET trabajando en Clean Architecture" |
| **Tarea** | Qué se quiere hacer | "Crea un handler MediatR que..." |
| **Restricciones** | Qué no se puede hacer | "sin usar EF Core para la query, usar Dapper" |
| **Formato de salida** | Cómo debe verse el resultado | "solo el código, sin explicaciones" |
| **Ejemplos** | Código existente de referencia | pegar el handler similar del proyecto |

> Un prompt vago genera código genérico. Un prompt estructurado produce código listo para integrar en el primer intento.

---

## Prompt bueno vs malo  -  crear código

```
 Malo:
"Crea un endpoint para obtener conversaciones"


 Bueno:
Contexto:
- Proyecto .NET 10 Clean Architecture
- Capa Application: UseCases con MediatR
- Todos los handlers retornan Result<T>
- Queries de lectura usan Dapper, nunca EF Core

Tarea:
Crea el UseCase completo para obtener conversaciones paginadas:
- Handler: GetConversationsHandler
- Request: GetConversationsRequest (WorkspaceId: Guid, Page: int, PageSize: int)
- Response: GetConversationsResponse (Items: IReadOnlyList<ConversationDto>, Total: int)

Sigue exactamente el mismo patrón que GetContactsHandler.
```

---

## Prompt bueno vs malo  -  corregir un bug

```
 Malo:
"Arregla el bug en el controller de conversaciones"


 Bueno:
Bug: al llamar POST /api/v1/conversations/{id}/assign con un ID válido
devuelve 404 en lugar de 200.

Stack trace:
  at ConversationController.Assign(Guid id)
  at ...

El handler existe y los tests unitarios pasan.
Busca el problema en el routing o en el registro de dependencias.
```

---

## Anti-patrones de prompts

| Anti-patrón | Problema | Corrección |
|------------|---------|-----------|
| "Crea un CRUD" | Demasiado vago, ignora el stack real | Especificar entidad, campos, capa, patrón |
| "Arregla el bug" sin contexto | El AI adivina | Pegar stack trace + código + síntoma exacto |
| "¿Cómo hago X en .NET?" | Respuesta genérica | "En este proyecto con este patrón, ¿cómo hago X?" |
| Pedir todo en un prompt | Respuesta larga e inconsistente | Dividir: handler, tests, controller por separado |
| No dar ejemplo | Código que no sigue las convenciones | Pegar un archivo existente como referencia |

---

## Técnicas avanzadas  -  Chain of Thought y Few-Shot

**Chain of thought** para decisiones de diseño:

```
Tengo que implementar mensajes en tiempo real en el sistema de mensajería.
Las opciones son: SignalR, polling cada 5 segundos, Server-Sent Events.

Antes de proponer código, analiza los trade-offs de cada opción
considerando que el backend es .NET + ECS Fargate (stateless,
múltiples instancias) y el frontend es React.

Luego recomienda una y explica por qué.
```

**Few-shot**  -  dar el patrón esperado:

```
Quiero que crees un nuevo UseCase siguiendo este patrón exacto:

[GetConversationHandler  -  código pegado]

Ahora crea el mismo patrón para AssignConversation:
- Request: ConversationId, AgentId
- Valida que la conversación exista y esté en estado Open
```

---

<!-- _class: lead -->

# Parte 5

## AI Workflow  -  Integración en el Ciclo de Desarrollo

---

## El ciclo de desarrollo con AI

```
1. Diseño          -> AI como sparring (trade-offs, alternativas)
2. Scaffolding     -> AI genera la estructura base
3. Implementación  -> AI escribe el código, humano revisa y corrige
4. Tests           -> AI genera casos, humano verifica cobertura
5. Revisión        -> AI revisa, humano aprueba
6. Documentación   -> AI redacta, humano valida
```

El humano nunca desaparece  -  decide qué hacer, valida que el AI lo hizo bien, y mantiene el contexto del negocio que el AI no tiene.

---

## Cuándo delegar al AI

| Tarea | Delegar | Por qué |
|-------|---------|---------|
| Boilerplate (handler, repository, tests de caso feliz) | Sí | Código predecible, bajo riesgo |
| Refactoring mecánico (renombrar, extraer método) | Sí | Transformación determinista |
| Generar casos de tests | Sí | El AI encuentra edge cases que el humano omite |
| Escribir docs de métodos públicos | Sí | Formato repetitivo |
| Diseño de arquitectura | No directo | El AI no conoce las restricciones del negocio |
| Lógica crítica de negocio | Parcial | El AI escribe, el humano valida con product owner |
| Decisiones de seguridad | Parcial | El AI propone, el humano verifica contra política |
| Queries SQL complejas de producción | Parcial | El AI genera, el humano analiza el plan de ejecución |

---

## Flujo para una nueva feature

```
1. Describir la feature al AI y pedir análisis de impacto:
   "¿qué archivos existentes hay que modificar para agregar X?"

2. Validar la lista de archivos afectados antes de escribir código

3. Pedir el diseño del UseCase primero (sin código):
   "¿cómo modelarías el handler y el dominio para X?"

4. Si el diseño es correcto, pedir la implementación:
   "Implementa el handler siguiendo el patrón de [handler existente]"

5. Revisar el código generado antes de aceptarlo

6. Pedir los tests:
   "Genera tests unitarios para el handler que acabas de crear"

7. Revisar los tests y ejecutarlos
```

---

## Flujo para un bug en producción

```
1. Recopilar evidencia: logs, stack trace, request que falló

2. Dar toda la evidencia al AI y pedir hipótesis:
   "Dada esta evidencia, ¿cuáles son las 3 causas más probables?"

3. Validar cada hipótesis contra el código

4. Cuando se identifica la causa, pedir el fix:
   "El problema está en línea X de Y. Corrígelo sin cambiar el
   comportamiento del resto del método"

5. Pedir un test de regresión para el bug
```

---

## Gestión del contexto en sesiones largas

Claude Code mantiene el contexto en memoria. Para sesiones largas:

- Abrir la sesión con el objetivo claro: "voy a implementar el módulo de Contacts"
- Hacer commits frecuentes para que el AI pueda ver el historial
- Si el AI empieza a generar código inconsistente: resetear con `/clear` y reabrir con contexto fresco
- Las sesiones muy largas (>2h) tienden a perder coherencia  -  partir la tarea

**Comandos útiles de Claude Code:**

```bash
claude               # en el directorio raíz  -  carga CLAUDE.md automáticamente

# pedir que el AI lea antes de responder
"lee src/Application/UseCases/Conversations/ y luego explica el flujo"

# buscar en el codebase sin salir del contexto
"busca todos los lugares donde se usa IConversationRepository"
```

---

<!-- _class: lead -->

# Parte 6

## LLMOps  -  Modelos en Producción

---

## LLMOps vs MLOps vs DevOps

```
DevOps:   código determinista -> tests unitarios -> CI/CD tradicional
MLOps:    modelo estadístico -> métricas (accuracy, F1) -> reentrenamiento
LLMOps:   LLM no determinista -> evaluación semántica -> prompt engineering

Diferencias clave con DevOps tradicional:
- No hay "respuesta correcta" exacta  -  se evalúa semánticamente
- Los prompts son código  -  se versionan y se testean
- El modelo puede cambiar sin aviso (el proveedor actualiza el modelo base)
- La latencia y el costo varían por request según el tamaño de la respuesta
```

---

## Prompt versioning  -  los prompts son código

```csharp
// Application/AI/Prompts/ConversationSummaryPrompt.cs
public static class ConversationSummaryPrompt
{
    // v1.0  -  prompt base
    public const string SystemV1 = """
        Eres un asistente del sistema de mensajería.
        Resume conversaciones de soporte al cliente en español.
        Sé conciso  -  máximo 3 párrafos.
        """;

    public static string BuildUserPrompt(Conversation conv) =>
        $"""
        Resume la siguiente conversación de soporte:
        Canal: {conv.Channel}
        Mensajes: {conv.MessageCount}
        Duración: {conv.DurationMinutes} minutos
        """;
}

// Registro de versiones para A/B testing
public enum PromptVersion { V1, V2 }
```

---

## Evaluación de outputs  -  cómo testear LLMs

A diferencia del código determinista, los LLMs requieren evaluación semántica:

```csharp
[Fact]
public async Task GetConversationSummary_ContainsKeyInfo()
{
    // Arrange
    var conversation = new Conversation
    {
        Channel = "WhatsApp",
        MessageCount = 12,
        DurationMinutes = 25
    };

    // Act
    var summary = await _service.GetSummaryAsync(conversation, CancellationToken.None);

    // Assert  -  evaluación semántica, NO igualdad exacta
    summary.Should().NotBeNullOrWhiteSpace();
    summary.Should().Contain("WhatsApp", StringComparison.OrdinalIgnoreCase);
    summary.Length.Should().BeLessThan(1000);
    // No verificar el texto exacto  -  el LLM puede variar la redacción
}
```

---

## Observabilidad y control de costos

```csharp
// Logging estructurado de llamadas a LLMs
_logger.LogInformation(
    "LLM call: {Model} | PromptTokens: {Tokens} est. | " +
    "ResponseLength: {Chars} chars | Latency: {LatencyMs}ms",
    options.Model,
    EstimateTokens(prompt),   // text.Length / 4  -  estimación
    response.Length,
    sw.ElapsedMilliseconds);
```

```csharp
// Control de costos  -  límite de tokens por usuario por día
var key   = $"ai_tokens:{userId}:{DateTime.UtcNow:yyyy-MM-dd}";
var used  = int.Parse(await _cache.GetStringAsync(key) ?? "0");
var limit = 50_000;  // 50k tokens por día por usuario

if (used >= limit)
{
    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Límite diario de tokens alcanzado. Intenta mañana."
    });
    return;
}
```

---

<!-- _class: lead -->

# Parte 7

## Herramientas  -  Claude Code, Copilot, Cursor

---

## Comparación de herramientas

| Herramienta | Fortaleza principal | Cuándo usar |
|-------------|--------------------| ------------|
| **Claude Code CLI** | Acceso completo al codebase, CLAUDE.md, agente autónomo | Features completas, refactors grandes, code review |
| **GitHub Copilot** | Autocompletado inline en el editor | Escritura rápida, tab-complete mientras codeas |
| **Cursor** | Editor completo con IA integrada, multi-archivo | Equipos que quieren todo en un IDE con IA |
| **Chat web (Claude/ChatGPT)** | Sin contexto del proyecto | Preguntas conceptuales, aprender algo nuevo |

**Regla práctica:**
- Snippet rápido -> Copilot inline
- Feature completa con múltiples archivos -> Claude Code CLI
- Pregunta de arquitectura sin código -> Chat web

---

## Claude Code CLI  -  ventajas específicas

Claude Code **lee el sistema de archivos real**  -  no necesitas pegar código:

```bash
# En lugar de pegar código, referenciar el archivo
"Lee src/Application/UseCases/Conversations/ y
luego crea un UseCase similar para el módulo de BotFlows"

# Para tareas con múltiples archivos
"Antes de escribir el test, lee:
- el handler en Application/UseCases/Contacts/ImportContacts/
- el contrato del repositorio en Domain/Repositories/IContactRepository.cs
- un test existente en Application.Tests/UseCases/

Luego crea los tests para ImportContactsHandler."
```

Claude Code carga `CLAUDE.md` automáticamente  -  no hay que re-explicar el stack.

---

<!-- _class: lead -->

# Parte 8

## Lo que la IA NO puede hacer

---

## Limitaciones reales de la IA en desarrollo

| Lo que SÍ hace bien | Lo que NO puede hacer |
|--------------------|----------------------|
| Generar boilerplate siguiendo un patrón | Decidir la arquitectura sin contexto de negocio |
| Encontrar edge cases para tests | Hacer code review real con accountability |
| Explicar código existente | Ejecutar pruebas de aceptación con el usuario |
| Detectar code smells conocidos | Tomar decisiones éticas o de política de datos |
| Refactoring mecánico bien definido | Entender las restricciones no escritas del negocio |

> La IA es un multiplicador  -  no un reemplazo del juicio del desarrollador.

---

## El riesgo de aceptar código sin revisar

Un desarrollador que acepta todo lo que genera el AI sin revisar:

1. **Introduce bugs** que el AI no puede detectar porque no conoce el negocio
2. **Rompe convenciones** del equipo que no estaban en CLAUDE.md
3. **Acumula deuda técnica** que parece código pero no tiene tests reales
4. **Pierde el entendimiento** del sistema  -  ya no puede depurar sin el AI
5. **Viola la seguridad** al aceptar código con vulnerabilidades sutiles

**La regla de oro:**
> No commitees código que no entiendes.
> Si el AI lo escribió y no lo entiendes, pídele que lo explique primero.

---

## Resumen  -  Vibe Coding profesional

| Principio | Practica |
|-----------|---------|
| El AI necesita contexto | CLAUDE.md con stack, convenciones y restricciones |
| Los prompts son código | Estructurar: contexto -> tarea -> restricciones -> formato |
| Diseñar antes de implementar | Pedir diseño primero, código después |
| Revisar antes de commitear | Nunca aceptar código sin leerlo |
| Los prompts se versionan | Tratar los prompts de producción como código fuente |
| El límite de contexto importa | Sesiones largas pierden coherencia  -  usar `/clear` |
| El humano mantiene el juicio | IA implementa, humano decide, valida y aprueba |
