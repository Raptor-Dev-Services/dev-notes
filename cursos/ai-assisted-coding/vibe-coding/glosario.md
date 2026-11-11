# Glosario Técnico  -  Vibe Coding  -  AI-Assisted Development

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### A/B Testing (de prompts)
**Categoría:** LLMOps
**Definición:** Técnica para comparar dos versiones de un prompt (por ejemplo, `V1` y `V2`) y medir cuál produce resultados de mayor calidad, menor latencia o menor costo. A diferencia del A/B testing de UI, la comparación es semántica, no binaria, porque no hay respuesta "exacta" contra la cual validar.
**En el proyecto:** En el sistema de mensajería se puede usar para comparar versiones de `ConversationSummaryPrompt.SystemV1` vs una versión `V2` mejorada y evaluar cuál resume mejor las conversaciones de soporte.

### Accountability
**Categoría:** Vibe Coding
**Definición:** Responsabilidad formal de responder por el resultado de una decisión técnica. El AI no tiene accountability porque no puede ser responsable de las consecuencias del código que genera; esa responsabilidad recae siempre en el desarrollador que commitea el código.
**En el proyecto:** Si el sistema de mensajería introduce un bug que filtra datos de un workspace a otro, el desarrollador que aceptó el código generado sin revisarlo es el responsable  -  no el AI.

### Action (ciclo ReAct)
**Categoría:** AI / Agentes
**Definición:** Segundo paso del ciclo de razonamiento de un agente (Thought -> Action -> Observation). Es la herramienta concreta que el agente ejecuta para obtener información o modificar el sistema: buscar un archivo, leer código, ejecutar un comando de terminal, hacer una llamada a una API.
**En el proyecto:** Un agente que implementa `AssignConversationHandler` ejecuta acciones como `grep` para localizar el repositorio, `read` para leer el contrato de la interfaz, y `write` para crear el archivo del handler.

### Agente (AI Agent)
**Categoría:** AI / Agentes
**Definición:** Sistema de IA que completa tareas de múltiples pasos de forma autónoma, usando herramientas (lectura de archivos, ejecución de comandos, búsqueda en el codebase) sin intervención humana en cada paso intermedio. Se diferencia del modo chat en que el agente actúa, no solo responde.
**En el proyecto:** Claude Code operando en modo agente puede recorrer todos los handlers del sistema de mensajería, detectar cuáles no tienen tests, y generarlos uno por uno sin que el desarrollador tenga que pegar código en cada iteración.

### Análisis de impacto
**Categoría:** AI Workflow
**Definición:** Primera consulta que se hace al AI antes de escribir código para una nueva feature: pedirle que identifique qué archivos existentes serán afectados por el cambio. Evita que el AI empiece a escribir código sin conocer las dependencias.
**En el proyecto:** Antes de agregar el módulo de campañas al sistema de mensajería, se pide: "¿qué archivos existentes hay que modificar para agregar envío masivo programado?", y se valida la lista antes de avanzar.

### Anti-patrón (de prompt)
**Categoría:** Vibe Coding / Prompts
**Definición:** Forma incorrecta o ineficiente de construir un prompt que produce resultados genéricos, inconsistentes o incorrectos. Los anti-patrones más comunes son: pedir todo en un solo prompt, no dar contexto del stack, no pegar un ejemplo de referencia, o describir el síntoma sin incluir el stack trace.
**En el proyecto:** "Crea un CRUD de contactos" es un anti-patrón porque el AI desconoce que el proyecto usa Result Pattern, Dapper para lecturas, repositorios específicos y paginación cursor-based.

### ASP.NET Core
**Categoría:** Backend / Stack
**Definición:** Framework web de Microsoft, parte del ecosistema .NET, usado para construir APIs RESTful, controladores HTTP y middleware. En el sistema de mensajería se usa la versión 10 junto con MediatR para el despacho de comandos y queries.
**En el proyecto:** Los endpoints del sistema de mensajería (`POST /api/v1/conversations/{id}/assign`, etc.) están implementados en controladores ASP.NET Core 10 que reciben requests y los despachan al handler correspondiente vía MediatR.

### AWS ECS Fargate
**Categoría:** Cloud / Infraestructura
**Definición:** Servicio de AWS para ejecutar contenedores Docker sin administrar servidores (serverless containers). ECS es el orquestador y Fargate es el modo de cómputo sin instancias EC2. Es stateless por diseño: cada instancia no guarda estado entre requests, lo que impacta en el diseño de funcionalidades como WebSockets.
**En el proyecto:** El backend del sistema de mensajería se despliega en ECS Fargate. Su naturaleza stateless obliga a usar un Redis backplane para SignalR, ya que dos instancias distintas de Fargate no comparten memoria.

---

## B

### Boilerplate
**Categoría:** Vibe Coding / Desarrollo
**Definición:** Código repetitivo y predecible que sigue un patrón fijo: handlers, repositorios, tests de caso feliz, DTOs, controladores. Es la tarea más segura para delegar al AI porque el resultado es determinista y verificable rápidamente.
**En el proyecto:** Crear un nuevo `GetMessagesHandler` con su `GetMessagesRequest` y `GetMessagesResponse` siguiendo el mismo patrón que `GetConversationsHandler` es boilerplate puro  -  ideal para delegar al AI.

### Bug de producción
**Categoría:** AI Workflow
**Definición:** Error que ocurre en el entorno de producción y que requiere evidencia concreta (logs, stack trace, request que falló) para diagnosticar. El flujo correcto con AI es: recopilar evidencia -> pedir hipótesis al AI -> validar cada hipótesis -> pedir el fix puntual -> pedir test de regresión.
**En el proyecto:** Si el endpoint `POST /api/v1/conversations/{id}/assign` devuelve 404 con IDs válidos, el flujo correcto es dar al AI el stack trace completo y el código del controller para que proponga las 3 causas más probables.

---

## C

### Chain of Thought
**Categoría:** Prompts / Técnicas avanzadas
**Definición:** Técnica de prompting que instruye al AI a razonar paso a paso antes de dar una respuesta final. Se usa especialmente para decisiones de diseño o trade-offs donde la respuesta directa podría ser incorrecta. Se activa pidiendo explícitamente al AI que analice antes de proponer.
**En el proyecto:** Para decidir entre SignalR, polling y Server-Sent Events en el sistema de mensajería, se pide al AI que analice los trade-offs de cada opción considerando que el backend corre en ECS Fargate (stateless, múltiples instancias), y que recomiende una con justificación.

### CI/CD (Continuous Integration / Continuous Delivery)
**Categoría:** DevOps / LLMOps
**Definición:** Práctica de automatizar la integración de código (CI) y su entrega al entorno de staging o producción (CD). En el contexto de LLMOps, los prompts de producción deben pasar por el mismo pipeline de CI/CD que el código, incluyendo evaluación semántica automatizada.
**En el proyecto:** El pipeline de GitHub Actions del sistema de mensajería ejecuta `dotnet test` en cada PR. Si el proyecto integra LLMs, los tests de evaluación semántica de prompts se agregarían al mismo pipeline.

### CLAUDE.md
**Categoría:** Vibe Coding / Herramientas
**Definición:** Archivo de texto en formato Markdown que Claude Code carga automáticamente al iniciar cada sesión. Contiene el contexto persistente del proyecto: stack, versiones, arquitectura, convenciones de nombres, restricciones (anti-patrones explícitos) y comandos frecuentes. Es el equivalente de un documento de onboarding para el AI.
**En el proyecto:** El `CLAUDE.md` del sistema de mensajería documenta que los handlers retornan `Result<T>`, que las queries usan Dapper (nunca EF Core), que no se usa `IRepository<T>` genérico, y las entidades del dominio: `Conversation`, `Contact`, `Message`, `Agent`, `Channel`, `BotFlow`, `Workspace`.

### Claude Code CLI
**Categoría:** Herramientas / AI
**Definición:** Herramienta de línea de comandos de Anthropic que ejecuta Claude con acceso completo al sistema de archivos real del proyecto. A diferencia del chat web, puede leer, escribir y modificar archivos directamente, ejecutar comandos de terminal, y operar como agente autónomo en tareas de múltiples pasos. Carga `CLAUDE.md` automáticamente.
**En el proyecto:** Para implementar el módulo de contactos completo (handler, repositorio, tests, controller), se usa Claude Code CLI en la raíz del repo del sistema de mensajería para que el AI pueda leer los archivos existentes como referencia sin necesidad de copiar y pegar.

### Clean Architecture
**Categoría:** Arquitectura
**Definición:** Patrón arquitectural de cuatro capas concéntricas (Domain, Application, Infrastructure, Presentation/WebApi) donde las dependencias apuntan siempre hacia adentro. La capa de dominio no conoce a las capas externas. Establece la separación de responsabilidades y facilita el testing unitario.
**En el proyecto:** El sistema de mensajería sigue Clean Architecture: `Domain` contiene las entidades (`Conversation`, `Contact`), `Application` contiene los handlers y casos de uso, `Infrastructure` contiene los repositorios y el contexto de EF Core, y `WebApi` contiene los controllers.

### Code smell
**Categoría:** Desarrollo / Calidad
**Definición:** Indicador en el código de un posible problema de diseño o mantenibilidad, sin ser necesariamente un bug. Ejemplos: método demasiado largo, clase con múltiples responsabilidades, duplicación de lógica, nombre poco descriptivo. El AI puede detectar code smells conocidos pero no siempre entiende el contexto de negocio que los justifica.
**En el proyecto:** Si un handler del sistema de mensajería tiene 200 líneas y mezcla validación, lógica de negocio y acceso a datos, el AI puede detectarlo como code smell y sugerir extraer responsabilidades, pero el desarrollador debe decidir si el refactor es prioritario.

### Contrato público
**Categoría:** Arquitectura / Desarrollo
**Definición:** Firma de una interfaz, método o endpoint que otros componentes del sistema consumen. Cambiar el contrato público implica actualizar todos los consumidores. Al pedir refactors al AI, es importante especificar que no debe cambiar el contrato público.
**En el proyecto:** La firma de `IConversationRepository.GetPagedAsync(WorkspaceId, Page, PageSize)` es el contrato público. Al pedir al AI que optimice la implementación, se especifica: "optimiza solo el `GetPagedAsync` en `ConversationRepository.cs`. No cambies la interfaz del repositorio."

### Contexto (de sesión AI)
**Categoría:** AI / LLM
**Definición:** Toda la información que el modelo de lenguaje tiene disponible en una sesión: el historial de mensajes, los archivos leídos, el contenido de CLAUDE.md, y el código generado previamente. El contexto tiene un límite (context window) y las sesiones muy largas pueden degradar la coherencia de las respuestas.
**En el proyecto:** Una sesión de Claude Code que implementa el módulo de contactos completo puede perder coherencia después de 2 horas. La solución es hacer commits frecuentes, luego usar `/clear` para reiniciar con contexto fresco y continuar con el siguiente módulo.

### Context window
**Categoría:** AI / LLM
**Definición:** Límite máximo de tokens que un modelo de lenguaje puede procesar en una sola sesión, incluyendo tanto el input (prompt + historial) como el output (respuesta). Cuando se supera, el modelo "olvida" las partes más antiguas del contexto, lo que puede causar respuestas inconsistentes.
**En el proyecto:** En sesiones largas del sistema de mensajería, si el AI empieza a generar código que contradice las convenciones establecidas al inicio (por ejemplo, usar `IRepository<T>` genérico cuando está explícitamente prohibido), es señal de que el contexto se llenó y hay que usar `/clear`.

### Cursor
**Categoría:** Herramientas / AI
**Definición:** Editor de código (fork de VS Code) con IA integrada de forma nativa que soporta edición multi-archivo. A diferencia de Copilot (inline completion) o Claude Code CLI (agente autónomo), Cursor ofrece un IDE completo con IA como característica central, incluyendo chat con contexto del proyecto.
**En el proyecto:** Cursor es una alternativa válida a Claude Code CLI para equipos que prefieren un entorno gráfico. Sin embargo, para el sistema de mensajería se recomienda Claude Code CLI para tasks de agente autónomo (implementar un módulo completo) y Copilot para autocompletado inline durante la escritura.

---

## D

### Dapper
**Categoría:** Backend / Base de datos
**Definición:** Micro-ORM de .NET que ejecuta queries SQL directamente y mapea los resultados a objetos C#. Es más liviano y performante que EF Core para lecturas porque no agrega abstracción sobre el SQL. En Clean Architecture se usa Dapper en la capa de Infrastructure para queries de lectura.
**En el proyecto:** En el sistema de mensajería, todas las queries de lectura (listas paginadas, búsquedas, filtros) usan Dapper. Las escrituras y migraciones usan EF Core. Esta separación es una convención explícita en el `CLAUDE.md` del proyecto.

### Deuda técnica
**Categoría:** Desarrollo / Calidad
**Definición:** Costo futuro acumulado por tomar atajos o malas decisiones técnicas en el presente. En el contexto de Vibe Coding, la deuda técnica se acumula cuando el desarrollador acepta código generado por AI sin revisarlo, lo que resulta en código que "funciona" pero carece de tests, viola convenciones, o es difícil de mantener.
**En el proyecto:** Aceptar handlers del sistema de mensajería generados por AI sin revisar puede introducir deuda técnica: código que no usa `Result<T>`, que lanza excepciones de negocio directamente, o que usa queries SQL inline en el handler en vez del repositorio.

### Docker
**Categoría:** Contenedores / DevOps
**Definición:** Plataforma de contenedores que empaqueta una aplicación con todas sus dependencias en una imagen portable. Los contenedores son stateless por diseño. En el contexto del curso, Docker multi-stage se menciona como uno de los temas donde el equipo tiene brechas (confundirlo con soporte multi-OS).
**En el proyecto:** El backend y el frontend del sistema de mensajería se empaquetan en contenedores Docker con builds multi-stage: una etapa compila el código y otra genera la imagen de producción mínima. El tag de producción siempre es semántico (`v1.2.3`), nunca `:latest`.

---

## E

### Edge case
**Categoría:** Testing / Calidad
**Definición:** Caso límite o poco frecuente que puede causar comportamiento inesperado en el sistema. Los LLMs son buenos encontrando edge cases porque han sido entrenados con grandes volúmenes de código y pueden anticipar condiciones que el desarrollador pasa por alto.
**En el proyecto:** Al pedir tests para `AssignConversationHandler`, el AI puede identificar edge cases como: conversación en estado `Closed` (no asignable), agente desactivado, agente de otro workspace, o ID de conversación inexistente.

### EF Core (Entity Framework Core)
**Categoría:** Backend / ORM
**Definición:** ORM (Object-Relational Mapper) de Microsoft para .NET que permite interactuar con la base de datos usando objetos C# en lugar de SQL. Incluye migraciones para gestionar el esquema de la base de datos. En el sistema de mensajería se usa para escrituras y migraciones, mientras que Dapper se usa para lecturas.
**En el proyecto:** EF Core gestiona el `AppDbContext` del sistema de mensajería, las configuraciones de entidades (`EntityTypeConfiguration`), las migraciones de esquema, y las operaciones de escritura (INSERT, UPDATE, DELETE).

### Evaluación semántica
**Categoría:** LLMOps / Testing
**Definición:** Método de validación de outputs de LLMs que verifica propiedades generales (longitud, presencia de palabras clave, formato) en lugar de igualdad exacta de texto. Es necesario porque los modelos de lenguaje son no deterministas: la misma entrada puede producir respuestas ligeramente distintas en cada llamada.
**En el proyecto:** Al testear `GetSummaryAsync` en el sistema de mensajería, en lugar de comparar el texto exacto del resumen, se verifica que no sea nulo, que contenga el nombre del canal ("WhatsApp"), y que tenga menos de 1000 caracteres.

---

## F

### Feature flag
**Categoría:** Arquitectura / SaaS
**Definición:** Mecanismo que permite activar o desactivar funcionalidades del sistema por configuración, sin hacer un nuevo despliegue. Permite lanzar features a subconjuntos de usuarios, hacer A/B testing de funcionalidades, o limitar features por plan de suscripción.
**En el proyecto:** En el sistema de mensajería, un feature flag puede controlar si un workspace tiene acceso al módulo de IA (resumen de conversaciones), dependiendo de su plan de suscripción.

### Few-Shot Prompting
**Categoría:** Prompts / Técnicas avanzadas
**Definición:** Técnica que consiste en proporcionar uno o más ejemplos del patrón esperado dentro del prompt, para que el AI replique exactamente ese estilo. Es más efectivo que describir el patrón en prosa porque el AI puede inferir convenciones implícitas del ejemplo concreto.
**En el proyecto:** Para crear `AssignConversationHandler`, se pega el código completo de `GetConversationHandler` en el prompt y se pide: "Ahora crea el mismo patrón para `AssignConversation`. Request: `ConversationId`, `AgentId`. Valida que la conversación exista y esté en estado `Open`."

### Full-Text Search (FTS)
**Categoría:** Base de datos / Backend
**Definición:** Técnica de búsqueda que indexa el contenido de campos de texto para permitir búsquedas por palabras clave con relevancia semántica, más potente que un `LIKE '%term%'`. PostgreSQL incluye soporte nativo de FTS con `tsvector` y `tsquery`.
**En el proyecto:** La bandeja de entrada del sistema de mensajería usa FTS de PostgreSQL para permitir buscar conversaciones por palabras en los mensajes o en los datos del contacto.

---

## G

### Generative AI (IA Generativa)
**Categoría:** AI / LLM
**Definición:** Categoría de modelos de inteligencia artificial capaces de generar texto, código, imágenes u otros tipos de contenido a partir de un prompt. Los modelos de lenguaje grandes (LLMs) como Claude o GPT-4 son ejemplos de IA generativa aplicada a texto y código.
**En el proyecto:** El sistema de mensajería usa IA generativa para funcionalidades como el resumen automático de conversaciones. Claude Code CLI es también IA generativa aplicada al desarrollo del propio proyecto.

### GitHub Actions
**Categoría:** CI/CD / DevOps
**Definición:** Plataforma de automatización integrada en GitHub que permite definir pipelines de CI/CD en archivos YAML. Ejecuta jobs automáticamente en respuesta a eventos (push, pull request, merge) para compilar, testear y desplegar el código.
**En el proyecto:** El pipeline de CI/CD del sistema de mensajería usa GitHub Actions con OID Connect para autenticarse en AWS sin credenciales estáticas, y despliega automáticamente a staging en cada merge a la rama principal.

### GitHub Copilot
**Categoría:** Herramientas / AI
**Definición:** Herramienta de autocompletado de código con IA integrada directamente en el editor (VS Code, JetBrains, etc.) que sugiere completaciones de código en tiempo real mientras el desarrollador escribe. A diferencia de Claude Code, no tiene acceso agente al sistema de archivos ni carga un archivo de instrucciones persistente.
**En el proyecto:** Copilot es útil para acelerar la escritura de código repetitivo (DTOs, tests unitarios simples) mientras se desarrolla el sistema de mensajería en el IDE, pero para tasks multi-archivo se prefiere Claude Code CLI.

---

## H

### Handler (MediatR)
**Categoría:** Backend / Arquitectura
**Definición:** Clase en la capa Application que implementa la lógica de un caso de uso específico. Recibe un `Request` y retorna un `Result<Response>`. En Clean Architecture con MediatR, cada caso de uso tiene su propio handler en una carpeta dedicada (`UseCases/NombreFeature/`).
**En el proyecto:** `GetConversationsHandler`, `AssignConversationHandler`, `ImportContactsHandler` son ejemplos de handlers del sistema de mensajería. El naming convention es `{Verbo}{Entidad}Handler`.

### Hipótesis (en debugging)
**Categoría:** AI Workflow
**Definición:** Posible causa de un bug que el AI propone tras analizar la evidencia disponible (logs, stack trace, código). El flujo correcto con AI es pedir explícitamente las N causas más probables y luego validar cada una contra el código, en lugar de pedir directamente el fix.
**En el proyecto:** Ante el bug de 404 en `POST /api/v1/conversations/{id}/assign`, el AI puede proponer tres hipótesis: routing mal configurado, falta de registro del handler en DI, o parámetro de ruta con tipo incorrecto. El desarrollador verifica cada una antes de pedir el fix.

---

## I

### Instrucciones persistentes
**Categoría:** Vibe Coding / CLAUDE.md
**Definición:** Contexto sobre el proyecto que no cambia entre sesiones de trabajo y que se almacena en `CLAUDE.md` para que el AI lo cargue automáticamente. Evita tener que re-explicar el stack, las convenciones y las restricciones al inicio de cada sesión.
**En el proyecto:** Las instrucciones persistentes del sistema de mensajería incluyen: stack (.NET 10, React 19, PostgreSQL 16), arquitectura (Clean Architecture 4 capas), restricciones (no `.Result`, no `:latest`, no `IRepository<T>` genérico), y entidades del dominio.

---

## L

### Latencia (LLM)
**Categoría:** LLMOps / Performance
**Definición:** Tiempo que tarda el modelo de lenguaje en generar una respuesta completa desde que recibe el prompt. A diferencia del código determinista, la latencia de los LLMs varía por request porque depende del tamaño de la respuesta generada (cuántos tokens produce el modelo).
**En el proyecto:** Al integrar resumen de conversaciones en el sistema de mensajería, se mide la latencia con `sw.ElapsedMilliseconds` en el logging estructurado de cada llamada al LLM, para detectar regresiones de performance al cambiar de versión de modelo o de prompt.

### LLM (Large Language Model)
**Categoría:** AI / LLM
**Definición:** Modelo de lenguaje de gran escala entrenado en cantidades masivas de texto y código. Es capaz de generar texto coherente, traducir lenguajes, resumir documentos y escribir código. Son la base de herramientas como Claude, GPT-4, y GitHub Copilot.
**En el proyecto:** El sistema de mensajería puede integrar llamadas a LLMs (como Claude API) para funcionalidades de IA: resumen de conversaciones, sugerencias de respuesta a agentes, o clasificación automática de conversaciones por sentimiento.

### LLMOps
**Categoría:** LLMOps
**Definición:** Conjunto de prácticas para operar LLMs en producción: versionado de prompts, evaluación semántica de outputs, observabilidad (logging de tokens, latencia), control de costos, y gestión del ciclo de vida del modelo. Es la evolución de MLOps aplicada a modelos de lenguaje.
**En el proyecto:** El sistema de mensajería implementa LLMOps básico: logging estructurado de cada llamada al LLM (modelo, tokens estimados, latencia), límite de tokens por usuario por día con Redis, y versionado de prompts en clases estáticas (`ConversationSummaryPrompt`).

### Logging estructurado
**Categoría:** Observabilidad / Backend
**Definición:** Técnica de logging donde cada mensaje incluye campos clave-valor en lugar de texto libre, facilitando la búsqueda y agregación en herramientas de observabilidad (Datadog, Grafana, CloudWatch). En .NET se implementa con Serilog usando template strings con parámetros nombrados.
**En el proyecto:** Las llamadas al LLM en el sistema de mensajería se loguean con campos estructurados: `{Model}`, `{Tokens}`, `{Chars}`, `{LatencyMs}`. Esto permite filtrar por modelo o detectar llamadas lentas en producción.

---

## M

### MediatR
**Categoría:** Backend / Patrones
**Definición:** Librería de .NET que implementa el patrón Mediator para desacoplar quien envía un comando/query de quien lo ejecuta. En Clean Architecture, los controllers envían requests a través de MediatR y este los despacha al handler correcto sin que el controller conozca al handler.
**En el proyecto:** Los controllers del sistema de mensajería inyectan `IMediator` y llaman `await _mediator.Send(request)`. MediatR localiza y ejecuta el handler correspondiente (`GetConversationsHandler`, `AssignConversationHandler`, etc.).

### Middleware
**Categoría:** Backend / ASP.NET Core
**Definición:** Componente en la pipeline de ASP.NET Core que procesa cada request HTTP antes de llegar al controller, o cada response antes de salir. Se usan para autenticación, logging, manejo de errores, rate limiting y control de tokens de LLMs.
**En el proyecto:** El control de límite de tokens por usuario en el sistema de mensajería se implementa como middleware: intercepta cada request a los endpoints de IA, verifica el contador en Redis, y retorna 429 si se superó el límite diario.

### MLOps
**Categoría:** LLMOps
**Definición:** Conjunto de prácticas para operar modelos de machine learning en producción, incluyendo pipelines de entrenamiento, evaluación con métricas cuantitativas (accuracy, F1, precision) y reentrenamiento. Se diferencia de LLMOps en que los modelos de ML son deterministas y se evalúan con métricas exactas.
**En el proyecto:** El sistema de mensajería no entrena modelos propios (no requiere MLOps), pero sí consume LLMs externos (LLMOps). La distinción es importante para saber qué prácticas aplicar.

### Modo Chat
**Categoría:** AI / Herramientas
**Definición:** Interacción directa con un modelo de lenguaje mediante preguntas y respuestas, sin acceso a herramientas externas. El AI solo puede responder con el conocimiento de su entrenamiento y el contexto del hilo de conversación. Útil para preguntas conceptuales o aprender algo nuevo.
**En el proyecto:** Para decidir entre SignalR y Server-Sent Events en el sistema de mensajería, una consulta al chat web puede explicar los trade-offs teóricos. Pero para implementar la feature en el código real del proyecto, se usa Claude Code CLI (modo agente).

### Modo Agente
**Categoría:** AI / Agentes
**Definición:** Modo de operación de un AI que le permite ejecutar herramientas (leer archivos, buscar en el codebase, ejecutar comandos) de forma autónoma para completar una tarea de múltiples pasos. El agente razona, actúa, observa el resultado y ajusta su siguiente acción (ciclo ReAct).
**En el proyecto:** Claude Code en modo agente puede implementar el módulo de contactos completo del sistema de mensajería: leer los archivos existentes, crear el handler, el repositorio, la migración y los tests, sin que el desarrollador intervenga en cada paso.

---

## N

### No determinismo (LLMs)
**Categoría:** LLMOps / AI
**Definición:** Propiedad de los modelos de lenguaje donde la misma entrada puede producir respuestas ligeramente distintas en cada ejecución, a diferencia del código determinista que siempre produce el mismo output para el mismo input. Esto afecta cómo se testean y evalúan los outputs de LLMs.
**En el proyecto:** El test de `GetConversationSummary` en el sistema de mensajería no puede verificar el texto exacto del resumen porque el LLM puede variar la redacción. En cambio, verifica propiedades invariantes: no nulo, contiene "WhatsApp", menos de 1000 caracteres.

---

## O

### Observation (ciclo ReAct)
**Categoría:** AI / Agentes
**Definición:** Tercer paso del ciclo de razonamiento de un agente (Thought -> Action -> Observation). Es el resultado de ejecutar la acción: el contenido del archivo leído, los resultados de la búsqueda, el output del comando ejecutado. El agente usa la observación para decidir el siguiente Thought.
**En el proyecto:** Cuando un agente ejecuta `grep("IConversationRepository", "*.cs")` (Action) y recibe la ruta del archivo (Observation), usa esa información para decidir que el siguiente paso es leer ese archivo específico.

### Onboarding document
**Categoría:** Vibe Coding / CLAUDE.md
**Definición:** Documento que explica a un nuevo integrante (o al AI) todo lo que necesita saber para trabajar en el proyecto: stack, arquitectura, convenciones, qué evitar, y cómo ejecutar el proyecto. `CLAUDE.md` cumple este rol para el AI.
**En el proyecto:** El `CLAUDE.md` del sistema de mensajería es el onboarding document del AI: le explica el stack (.NET 10, PostgreSQL 16), la arquitectura (Clean Architecture), las convenciones (naming de handlers, Result Pattern) y los anti-patrones prohibidos.

---

## P

### Paginación cursor-based
**Categoría:** Backend / Base de datos
**Definición:** Técnica de paginación que usa un cursor (generalmente el ID del último elemento obtenido) en lugar de offset numérico (`OFFSET 100 LIMIT 20`). Es más eficiente en tablas grandes porque no requiere que la base de datos cuente y salte filas.
**En el proyecto:** La bandeja de entrada del sistema de mensajería usa paginación cursor-based para cargar conversaciones. El `GetConversationsRequest` incluye un `Cursor` (ID de la última conversación vista) en lugar de un número de página.

### Pipeline Behavior (MediatR)
**Categoría:** Backend / Arquitectura
**Definición:** Interceptor en la pipeline de MediatR que envuelve la ejecución de todos los handlers. Se usa para aplicar comportamientos transversales: logging, validación, manejo de excepciones, métricas. Similar al middleware de ASP.NET Core pero para la capa Application.
**En el proyecto:** El sistema de mensajería tiene pipeline behaviors para: validar el request con FluentValidation antes de llegar al handler, loguear el nombre del handler y su duración, y manejar excepciones no controladas.

### Prompt
**Categoría:** AI / Prompts
**Definición:** Instrucción o mensaje que se envía a un modelo de lenguaje para obtener una respuesta. La calidad del prompt determina directamente la calidad de la respuesta. Un prompt estructurado incluye: rol/contexto, tarea específica, restricciones, formato de salida esperado, y ejemplos de referencia.
**En el proyecto:** Los prompts para trabajar en el sistema de mensajería siempre incluyen el stack real (.NET 10, Clean Architecture), el patrón de referencia (un handler existente), y las restricciones del proyecto (no `.Result`, no queries SQL inline en handlers).

### Prompt Engineering
**Categoría:** AI / LLMOps
**Definición:** Disciplina de diseñar y optimizar prompts para obtener los mejores resultados de un modelo de lenguaje. Incluye técnicas como Chain of Thought, Few-Shot, instrucciones de rol, y estructuración de restricciones. En LLMOps, los prompts se tratan como código: se versionan y se testean.
**En el proyecto:** El prompt de resumen de conversaciones del sistema de mensajería (`ConversationSummaryPrompt`) es el resultado de prompt engineering: define el rol del AI ("eres un asistente del sistema de mensajería"), la tarea (resumir en español), y la restricción de formato (máximo 3 párrafos).

### Prompt versioning
**Categoría:** LLMOps
**Definición:** Práctica de tratar los prompts de producción como código fuente: versionarlos en el repositorio (en una clase o archivo), registrar los cambios entre versiones, y nunca modificar un prompt en producción sin pasar por revisión de código. Permite hacer rollback a una versión anterior si el cambio degrada la calidad.
**En el proyecto:** En el sistema de mensajería, `ConversationSummaryPrompt.SystemV1` es la primera versión del prompt de resumen. Cuando se mejore, se agrega `SystemV2` manteniendo la versión anterior, permitiendo A/B testing y rollback.

---

## R

### Rate limiting (de tokens)
**Categoría:** LLMOps / Seguridad
**Definición:** Control que limita la cantidad de tokens que un usuario puede consumir en llamadas a LLMs en un período de tiempo (por ejemplo, 50,000 tokens por día). Previene el abuso y controla los costos de la API del proveedor de LLM.
**En el proyecto:** El sistema de mensajería implementa rate limiting de tokens en Redis: la clave `ai_tokens:{userId}:{fecha}` acumula los tokens consumidos. Si se supera el límite diario de 50,000, el middleware devuelve 429 con un mensaje explicativo.

### ReAct Pattern
**Categoría:** AI / Agentes
**Definición:** Patrón de razonamiento de agentes de IA que alterna entre razonar (Reasoning) y actuar (Acting): Thought -> Action -> Observation -> Thought -> ... El agente nunca actúa sin razonar primero, y nunca razona sin actualizar su modelo mental con las observaciones.
**En el proyecto:** Un agente que implementa `IConversationRepository` en el sistema de mensajería sigue ReAct: piensa qué archivos necesita leer (Thought), ejecuta grep (Action), recibe los resultados (Observation), decide leer el archivo específico (Thought), lo lee (Action), y finalmente escribe la implementación.

### Redis
**Categoría:** Infraestructura / Caching
**Definición:** Base de datos en memoria de alta velocidad usada como cache, message broker y backplane para SignalR. En el sistema de mensajería se usa para: cache de datos frecuentes, contadores de rate limiting de tokens, y como backplane de SignalR para sincronizar mensajes entre múltiples instancias.
**En el proyecto:** El control de tokens LLM por usuario usa Redis con claves temporales: `ai_tokens:{userId}:{yyyy-MM-dd}`. SignalR usa Redis backplane para que múltiples instancias de ECS Fargate puedan enviarse mensajes entre sí.

### Refactoring mecánico
**Categoría:** Vibe Coding / Desarrollo
**Definición:** Transformación del código que no cambia su comportamiento observable: renombrar una clase, extraer un método, reorganizar imports, eliminar código duplicado. Es seguro delegar al AI porque es determinista y verificable con los tests existentes.
**En el proyecto:** Renombrar `ConversationDto` a `ConversationSummaryDto` en todos los archivos del sistema de mensajería, o extraer la lógica de paginación a un método privado, son ejemplos de refactoring mecánico que el AI puede ejecutar de forma segura.

### Request (MediatR)
**Categoría:** Backend / Arquitectura
**Definición:** Objeto de datos que representa la entrada de un caso de uso. Contiene los parámetros necesarios para ejecutar el handler. Equivalente al DTO de entrada en la capa Application. El naming convention es `{Verbo}{Entidad}Request`.
**En el proyecto:** `GetConversationsRequest` contiene `WorkspaceId: Guid`, `Page: int`, `PageSize: int`. `AssignConversationRequest` contiene `ConversationId: Guid` y `AgentId: Guid`.

### Response (MediatR)
**Categoría:** Backend / Arquitectura
**Definición:** Objeto de datos que representa la salida de un caso de uso. El handler retorna `Result<Response>`. Generalmente es un record de solo lectura. El naming convention es `{Verbo}{Entidad}Response`.
**En el proyecto:** `GetConversationsResponse` contiene `Items: IReadOnlyList<ConversationDto>` y `Total: int`. El handler retorna `Result<GetConversationsResponse>`, nunca lanza excepciones de negocio.

### Repositorio específico
**Categoría:** Backend / Arquitectura
**Definición:** Interfaz e implementación de repositorio dedicada a una sola entidad del dominio, a diferencia del repositorio genérico `IRepository<T>`. Permite métodos con nombre descriptivo y firma específica para las necesidades de esa entidad. Es la convención explícita del proyecto.
**En el proyecto:** El sistema de mensajería tiene `IConversationRepository` (con `GetPagedAsync`, `GetByIdAsync`, `AssignAsync`), `IContactRepository`, `IMessageRepository`. Nunca `IRepository<Conversation>` genérico.

### Result Pattern
**Categoría:** Backend / Patrones
**Definición:** Patrón que retorna un objeto `Result<T>` en lugar de lanzar excepciones de negocio. El resultado puede ser exitoso (`Result.OK(value)`) o fallido (`Result.Fail(error)`). El llamador decide cómo manejar cada caso usando pattern matching, en lugar de capturar excepciones.
**En el proyecto:** Todos los handlers del sistema de mensajería retornan `Result<T>`. El Presenter en el controller usa pattern matching para convertir el Result en la respuesta HTTP correcta: 200, 404, 422, etc. Nunca `throw new BusinessException(...)` en handlers.

### Routing (ASP.NET Core)
**Categoría:** Backend / ASP.NET Core
**Definición:** Sistema de ASP.NET Core que mapea URLs entrantes a los métodos de controller correctos. Los bugs de routing (404 cuando el endpoint existe) son una causa común de errores que el AI puede diagnosticar si se le proporciona el stack trace y el código del controller.
**En el proyecto:** El endpoint `POST /api/v1/conversations/{id}/assign` del sistema de mensajería tiene routing definido con `[Route("api/v1/[controller]")]` y `[HttpPost("{id}/assign")]`. Un error en estos atributos causa 404 aunque el handler exista y funcione.

---

## S

### Scaffolding
**Categoría:** AI Workflow / Desarrollo
**Definición:** Generación de la estructura base de un módulo o feature: archivos, carpetas, clases vacías con la firma correcta. Es una de las tareas más seguras para delegar al AI porque el resultado es verificable visualmente y no contiene lógica de negocio.
**En el proyecto:** Para el módulo de BotFlows del sistema de mensajería, el AI puede generar el scaffolding completo: carpeta `UseCases/BotFlows/`, archivos `CreateBotFlowHandler.cs`, `CreateBotFlowRequest.cs`, `CreateBotFlowResponse.cs`, e `IBotFlowRepository.cs` con las firmas correctas pero sin implementación.

### SemVer (Semantic Versioning)
**Categoría:** Git / DevOps
**Definición:** Convención de versionado que usa tres números `MAJOR.MINOR.PATCH`: MAJOR para cambios incompatibles, MINOR para nuevas features compatibles, PATCH para bug fixes.
**En el proyecto:** Los tags de Docker del sistema de mensajería siguen SemVer: `v1.0.0` para el lanzamiento inicial, `v1.1.0` al agregar el módulo de bots, `v1.1.1` para un bug fix. Nunca `:latest` en producción.

### Serilog
**Categoría:** Backend / Observabilidad
**Definición:** Librería de logging estructurado para .NET. La convención crítica es usar template strings con parámetros nombrados en lugar de string interpolation: `_logger.LogInformation("LLM call: {Model}", model)` en vez de `_logger.LogInformation($"LLM call: {model}")`. La interpolación destruye el logging estructurado.
**En el proyecto:** El `CLAUDE.md` del sistema de mensajería incluye explícitamente la restricción: "No usar string interpolation en Serilog  -  usar template strings con `{Param}`". Esta restricción está ahí porque el AI tiende a generar logging con interpolación si no se especifica.

### Server-Sent Events (SSE)
**Categoría:** Backend / Real-time
**Definición:** Protocolo HTTP unidireccional que permite al servidor enviar eventos al cliente en tiempo real. A diferencia de WebSockets, es solo del servidor al cliente. Más simple de implementar pero con limitaciones (no bidireccional, no funciona bien con proxies HTTP/1.1).
**En el proyecto:** SSE fue evaluada como alternativa a SignalR para mensajes en tiempo real en el sistema de mensajería. Se descartó porque los agentes también necesitan enviar mensajes al servidor (bidireccional), lo que requiere WebSockets o SignalR.

### SignalR
**Categoría:** Backend / Real-time
**Definición:** Librería de ASP.NET Core que abstrae la comunicación en tiempo real (WebSockets, Server-Sent Events, long polling) entre servidor y cliente. Para escenarios con múltiples instancias del servidor, requiere un backplane (Redis) para sincronizar mensajes entre instancias.
**En el proyecto:** El sistema de mensajería usa SignalR para que los mensajes de WhatsApp/Instagram lleguen en tiempo real a la bandeja del agente. Con ECS Fargate (múltiples instancias), es obligatorio configurar Redis backplane para que la instancia que recibe el webhook del proveedor pueda notificar al agente conectado a otra instancia.

### Singleton (DI Lifetime)
**Categoría:** Backend / Inyección de dependencias
**Definición:** Lifetime de inyección de dependencias donde se crea una sola instancia del servicio para toda la vida de la aplicación.
**En el proyecto:** En el sistema de mensajería, el `CLAUDE.md` tiene la restricción explícita: "No registrar servicios Scoped dentro de un Singleton". Esta restricción existe porque el AI puede cometer este error al generar el registro de dependencias.

### Sparring (AI como)
**Categoría:** AI Workflow
**Definición:** Uso del AI como interlocutor para explorar trade-offs, alternativas y consecuencias de decisiones de diseño antes de comprometerse con una implementación. El AI actúa como un "devil's advocate" que desafía las decisiones y propone alternativas.
**En el proyecto:** Antes de decidir la arquitectura de real-time del sistema de mensajería, se usa el AI como sparring: "¿Cuáles son los trade-offs de SignalR vs polling vs SSE considerando ECS Fargate y React en el frontend?" El AI propone, el desarrollador decide.

### Stack trace
**Categoría:** Debugging / Backend
**Definición:** Registro de la secuencia de llamadas de métodos en el momento en que ocurrió un error. Es información esencial para diagnosticar bugs con el AI. Sin el stack trace, el AI solo puede adivinar; con él, puede localizar el problema con precisión.
**En el proyecto:** Cuando el endpoint `POST /api/v1/conversations/{id}/assign` del sistema de mensajería devuelve 404 inesperadamente, proporcionar el stack trace completo al AI le permite identificar si el problema está en el routing, el controller, el handler o el registro de dependencias.

---

## T

### Tenant isolation
**Categoría:** Arquitectura / SaaS
**Definición:** Garantía de que los datos de un workspace (tenant) nunca son accesibles para otro workspace. En el sistema de mensajería es crítico: las conversaciones, contactos y mensajes de una empresa no deben ser visibles para otra. Se implementa con filtros globales de EF Core y validación en los handlers.
**En el proyecto:** El `CLAUDE.md` del sistema de mensajería menciona explícitamente "restricciones de tenant isolation" como contexto que el AI no puede inferir del código. El AI debe aplicar el filtro de workspace en cada query sin que se lo repitan en cada prompt.

### Test de regresión
**Categoría:** Testing / Calidad
**Definición:** Test que verifica que un bug corregido no vuelve a aparecer. Debe crearse inmediatamente después de cada bug fix para prevenir regresiones futuras. En el flujo de bug en producción con AI, el último paso es siempre pedir el test de regresión.
**En el proyecto:** Después de corregir el bug de 404 en `AssignConversationHandler`, el AI genera un test de integración que verifica que `POST /api/v1/conversations/{id}/assign` retorna 200 con un ID válido, y 404 con un ID inexistente.

### Token (LLM)
**Categoría:** AI / LLM
**Definición:** Unidad básica de texto que el LLM procesa. Aproximadamente una palabra, aunque los tokens varían por idioma y modelo. El costo de una llamada a la API de un LLM se calcula por tokens procesados (input + output). Una estimación rápida es `texto.Length / 4` tokens.
**En el proyecto:** El sistema de mensajería estima los tokens de cada prompt con `text.Length / 4` para el logging de observabilidad. El límite de 50,000 tokens por usuario por día se define con base en el costo esperado por usuario del plan de suscripción.

---

## U

### UseCase
**Categoría:** Arquitectura / Clean Architecture
**Definición:** Carpeta en la capa Application que agrupa todos los archivos de un caso de uso específico: Handler, Request, Response, y opcionalmente Validator. La convención es una carpeta por caso de uso con todos sus artefactos juntos.
**En el proyecto:** `Application/UseCases/Conversations/GetConversations/` contiene `GetConversationsHandler.cs`, `GetConversationsRequest.cs`, `GetConversationsResponse.cs`. No se mezclan casos de uso en la misma carpeta.

---

## V

### Validación (FluentValidation)
**Categoría:** Backend / Calidad
**Definición:** Verificación de que el input de un caso de uso cumple las reglas de negocio antes de ejecutar el handler. En el stack del sistema de mensajería se implementa con FluentValidation y se intercepta en la pipeline de MediatR para que el handler nunca reciba un request inválido.
**En el proyecto:** `AssignConversationRequest` tiene un validador de FluentValidation que verifica que `ConversationId` y `AgentId` no sean `Guid.Empty`. El pipeline behavior ejecuta el validador y retorna `Result.Fail(...)` antes de llegar al handler.

### Versionado semántico
**Categoría:** Git / DevOps
**Definición:** Ver **SemVer**. Convención estándar de la industria para versionar software. El versionado semántico comunica el tipo de cambio (breaking, feature, fix) solo con los números de versión.
**En el proyecto:** Ver **SemVer**.

### Vibe Coding
**Categoría:** Vibe Coding
**Definición:** Uso deliberado y estructurado de IA generativa como copiloto durante el ciclo completo de desarrollo: diseño, scaffolding, implementación, tests, revisión y documentación. Se diferencia del uso casual en que el desarrollador mantiene el control, valida el output del AI, y provee contexto estructurado (CLAUDE.md, prompts con restricciones).
**En el proyecto:** Vibe Coding profesional en el sistema de mensajería significa: tener `CLAUDE.md` actualizado, usar prompts con contexto específico del proyecto, revisar todo el código antes de commitear, y tratar los prompts de producción como código versionado.

### Vulnerabilidad de seguridad
**Categoría:** Seguridad / Vibe Coding
**Definición:** Defecto en el código que permite a un atacante comprometer la seguridad del sistema: inyección SQL, exposición de datos sensibles, autenticación incorrecta. El AI puede generar código con vulnerabilidades sutiles que el desarrollador debe identificar en la revisión.
**En el proyecto:** Al aceptar código generado por AI para el sistema de mensajería, el desarrollador debe verificar especialmente: que los queries parametrizados no sean vulnerables a inyección SQL, que los endpoints de webhooks validen el HMAC del proveedor, y que los tokens JWT se validen correctamente.

---

## W

### WebSocket
**Categoría:** Backend / Real-time
**Definición:** Protocolo de comunicación bidireccional y persistente sobre TCP que permite al servidor y al cliente enviarse mensajes en cualquier momento. Es la base de SignalR. A diferencia de HTTP (request-response), la conexión WebSocket permanece abierta.
**En el proyecto:** El sistema de mensajería usa WebSockets (vía SignalR) para que los mensajes entrantes de WhatsApp/Instagram lleguen al agente en tiempo real sin que el cliente haga polling. Con ECS Fargate, el Redis backplane es obligatorio para sincronizar entre instancias.

---

## Z

### Zero-shot (prompting)
**Categoría:** Prompts / Técnicas
**Definición:** Tipo de prompt que no incluye ejemplos del patrón esperado  -  solo describe la tarea. Funciona bien para tareas simples o cuando el modelo tiene suficiente conocimiento del dominio. Para código con convenciones específicas del proyecto, es menos efectivo que few-shot.
**En el proyecto:** "Crea un endpoint para obtener conversaciones" es un prompt zero-shot  -  no incluye ejemplos del stack ni del patrón. En el sistema de mensajería, el zero-shot produce código genérico que no sigue las convenciones del equipo. Se prefiere few-shot con un handler existente como referencia.
