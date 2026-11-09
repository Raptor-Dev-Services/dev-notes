# Glosario - S05
## Model Context Protocol

> Los términos de la sesión 5, en orden alfabético.

---

## A

### Anfitrión (host)
**Categoría:** MCP / Arquitectura
**Definición:** La aplicación que tiene el modelo y la conversación, y que abre conexiones con uno o varios servidores MCP. Decide qué servidores conecta, qué herramientas le muestra al modelo y qué recursos adjunta. El modelo no es el anfitrión: lo usa el anfitrión.
**En el curso:** Claude Code y MCP Inspector actúan como anfitriones del servidor del catálogo. Nuestro CLI se conecta sin modelo, solo para probar.

---

### Anotaciones de herramienta
**Categoría:** MCP / Herramientas
**Definición:** Propiedades como `ReadOnly`, `Idempotent` y `Destructive` que describen cómo se comporta una herramienta. Son pistas para el cliente, que puede usarlas para decidir cuándo pedir confirmación; no son un permiso ni una garantía, y un servidor podría declararlas mal.
**En el curso:** las tres herramientas del catálogo llevan `ReadOnly = true` e `Idempotent = true`, y aun así el endpoint exige token.

---

### AuthenticationHandler
**Categoría:** ASP.NET Core / Seguridad
**Definición:** Clase base de ASP.NET Core para escribir un esquema de autenticación propio. Su método `HandleAuthenticateAsync` lee la petición y devuelve éxito con una identidad, falla, o "sin resultado" cuando la petición no trae credenciales de ese esquema.
**En el curso:** `AutenticacionPorToken` hereda de ella para validar `Authorization: Bearer <token>` contra una huella SHA-256.

---

## B

### Bearer token
**Categoría:** Seguridad / HTTP
**Definición:** Credencial que se manda en la cabecera `Authorization: Bearer <token>`. Quien la porta, la usa: no prueba identidad por sí sola, así que se protege como una contraseña y se manda solo por canales cifrados.
**En el curso:** el cliente lo toma de la variable `MCP_TOKEN`; se genera con `openssl rand -hex 32` y nunca se escribe en el repositorio.

---

## C

### Cliente MCP
**Categoría:** MCP / Arquitectura
**Definición:** La conexión que el anfitrión abre con un servidor MCP: una por servidor. Descubre las capacidades del servidor, llama herramientas, lee recursos y, si lo soporta, atiende peticiones del servidor como la elicitation.
**En el curso:** `McpClient.CreateAsync` con un `HttpClientTransport`, en el comando `mcp` del CLI y en las pruebas.

---

### Comparación en tiempo constante
**Categoría:** Seguridad / Criptografía
**Definición:** Comparación de dos secuencias de bytes que tarda lo mismo sin importar en qué posición difieren. Evita los ataques de tiempo, en los que un atacante adivina un secreto byte por byte midiendo cuánto tarda en fallar la comparación.
**En el curso:** `CryptographicOperations.FixedTimeEquals` compara la huella del token presentado con la huella guardada.

---

## E

### Elicitation
**Categoría:** MCP / Interacción
**Definición:** Mecanismo por el que el servidor, a media operación, le pide al cliente que solicite un dato o una confirmación a la persona. El esquema del formulario solo admite tipos primitivos y la persona puede aceptar, rechazar o cancelar. No se usa para pedir contraseñas ni datos de pago.
**En el curso:** `ElicitAsync<ConfirmacionInscripcion>` confirma una inscripción antes de escribirla, en la tarea.

---

## H

### Herramienta (MCP)
**Categoría:** MCP / Primitivas
**Definición:** Una operación que el servidor expone con nombre, descripción y esquema de entrada, y que el modelo decide llamar. Es la primitiva que elige el modelo; por eso su descripción debe decir cuándo usarla.
**En el curso:** `listar_cursos`, `obtener_curso` y `buscar_unidades`, declaradas con `[McpServerTool]`.

---

### Huella (SHA-256)
**Categoría:** Seguridad / Criptografía
**Definición:** El resultado de aplicar una función hash a un secreto. Permite verificar que alguien presenta el secreto correcto sin guardar el secreto: quien lee la huella no puede usarla para autenticarse.
**En el curso:** el servidor recibe `MCP_TOKEN_SHA256`, generada con `printf %s "$MCP_TOKEN" | shasum -a 256`.

---

## J

### JSON-RPC 2.0
**Categoría:** Protocolos
**Definición:** Formato de mensajes para llamadas remotas en JSON, con peticiones (con `id`), respuestas y notificaciones (sin `id`). Es el formato de todos los mensajes de MCP, sin importar el transporte.
**En el curso:** la prueba de 401 manda a mano un mensaje JSON-RPC sin credencial para ver que el servidor lo rechaza antes de leerlo.

---

## M

### MCP Inspector
**Categoría:** Herramientas de desarrollo
**Definición:** Interfaz web oficial para conectarse a un servidor MCP, listar sus herramientas, recursos y prompts, y llamarlos a mano. Se arranca con `npx @modelcontextprotocol/inspector`.
**En el curso:** segundo cliente del servidor del catálogo, con la cabecera `Authorization` configurada.

---

### McpException
**Categoría:** SDK de MCP para C#
**Definición:** Excepción del SDK que, lanzada dentro de una herramienta, llega al cliente como un resultado de error con su mensaje. Otras excepciones llegan como un error genérico, sin detalles internos.
**En el curso:** `obtener_curso` la lanza cuando el id no existe, con la pista "Usa listar_cursos para ver los ids".

---

### Metadatos de recurso protegido
**Categoría:** Seguridad / OAuth
**Definición:** Documento que publica un recurso protegido por OAuth para que el cliente descubra qué servidor de autorización emite sus tokens y qué permisos acepta. La especificación de MCP lo pide para servidores HTTP; el SDK cita la revisión 2025-11-25.
**En el curso:** se explica en clase; la práctica simplifica con un token Bearer emitido a mano.

---

### Mínimo privilegio
**Categoría:** Seguridad / Principios
**Definición:** Cada usuario, cliente o agente recibe solo los permisos que su trabajo necesita. Reduce el daño de un error, de un token robado o de un modelo manipulado.
**En el curso:** el token de la práctica trae solo `cursos:leer`; escribir pide `cursos:escribir` y otro token.

---

### Model Context Protocol (MCP)
**Categoría:** Protocolos / IA
**Definición:** Protocolo abierto que define cómo una aplicación con un modelo descubre y usa las capacidades de un programa externo: herramientas, recursos y prompts. Separa al que sabe del dato del que conversa, para que un mismo servidor sirva a varios clientes.
**En el curso:** el catálogo de cursos se expone por MCP y lo usan el CLI, MCP Inspector y Claude Code.

---

### Multi Round-Trip Requests (MRTR)
**Categoría:** MCP / Especificación
**Definición:** Mecanismo de la revisión 2026-07-28 (SEP-2322) para que el servidor responda a una petición diciendo que necesita algo del cliente, como una elicitation, y el cliente vuelva con la respuesta para completar la operación.
**En el curso:** es como viaja la confirmación por elicitation cuando cliente y servidor negocian la revisión 2026-07-28; el código de la herramienta no cambia.

---

## P

### Prompt (primitiva MCP)
**Categoría:** MCP / Primitivas
**Definición:** Plantilla con nombre y argumentos que el servidor ofrece y que la persona invoca, por ejemplo como un comando del anfitrión. Es la primitiva que elige la persona.
**En el curso:** se explica en clase; la práctica no la usa.

---

## R

### Recurso (MCP)
**Categoría:** MCP / Primitivas
**Definición:** Contenido de solo lectura que el servidor expone con una URI, a veces con plantilla. Lo elige la aplicación o la persona para dárselo al modelo como contexto.
**En el curso:** `catalogo://cursos/{id}`, declarado con `[McpServerResource]`.

---

### Revisión de la especificación
**Categoría:** MCP / Especificación
**Definición:** Cada versión publicada de MCP, identificada por fecha. Cliente y servidor negocian cuál usar. El SDK de C# 2.4.0 soporta 2024-11-05, 2025-03-26, 2025-06-18, 2025-11-25 y 2026-07-28, y prefiere la última.
**En el curso:** la revisión 2026-07-28 quitó el saludo `initialize` y dejó sampling en desuso; por eso los libros de 2025 se leen por los conceptos.

---

## S

### Sampling
**Categoría:** MCP / Interacción
**Definición:** Mecanismo por el que un servidor le pedía al modelo del cliente que generara texto. Quedó en desuso en la revisión 2026-07-28 (SEP-2577); el SDK de C# 2.4.0 marca sus tipos como obsoletos con el aviso MCP9005.
**En el curso:** no se usa. Si el servidor necesita un modelo, lo llama él, con su llave y su tope de gasto.

---

### Scope
**Categoría:** Seguridad / Autorización
**Definición:** Un permiso con nombre que acompaña a un token, como `cursos:leer` o `cursos:escribir`. Las políticas de autorización exigen un scope para dejar pasar una operación.
**En el curso:** el claim `scope` que agrega `AutenticacionPorToken`, y la política `cursos:escribir` de la tarea.

---

### Servidor MCP
**Categoría:** MCP / Arquitectura
**Definición:** Programa que expone capacidades por MCP. No habla con el modelo ni necesita llave de API: anuncia lo que ofrece, ejecuta lo que se le pide y decide quién puede pedirlo.
**En el curso:** `Asistente.Mcp`, un proyecto de ASP.NET Core con `AddMcpServer().WithHttpTransport()`.

---

### stdio (transporte)
**Categoría:** MCP / Transportes
**Definición:** Transporte en el que el anfitrión lanza el servidor como subproceso y se comunican por entrada y salida estándar. Todo lo que el servidor escribe en stdout es protocolo, así que sus registros van a stderr.
**En el curso:** se muestra con un servidor de ejemplo; la práctica usa HTTP porque el catálogo es compartido.

---

### Streamable HTTP
**Categoría:** MCP / Transportes
**Definición:** Transporte en el que el servidor atiende un endpoint HTTP y responde con JSON o con un flujo de eventos cuando la respuesta tarda. Permite varios clientes y servidores remotos, y por eso exige autenticación.
**En el curso:** `MapMcp("/mcp").RequireAuthorization()` en `Asistente.Mcp`.

---

## W

### WebApplicationFactory
**Categoría:** ASP.NET Core / Pruebas
**Definición:** Clase de `Microsoft.AspNetCore.Mvc.Testing` que levanta una aplicación de ASP.NET Core en memoria para probarla con un `HttpClient`, sin puertos ni procesos aparte.
**En el curso:** `ServidorMcpPruebas` la usa para probar el 401 y para conectar un cliente MCP real al servidor.
