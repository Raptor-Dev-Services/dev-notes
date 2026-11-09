# Glosario - S04
## Herramientas y agentes

> Los términos de la sesión 4, en orden alfabético.

---

## A

### Agente
**Categoría:** LLM / Arquitectura
**Definición:** Un modelo con herramientas y un bucle que lo deja decidir el siguiente paso hasta terminar la tarea. A diferencia de un flujo fijo, no se sabe de antemano cuántas llamadas va a hacer, por eso necesita límites que haga cumplir el código.
**En el curso:** `BucleAgente` con `buscar_tickets` y `crear_ticket`.

---

## B

### Bitácora
**Categoría:** Operación / Auditoría
**Definición:** Registro de cada decisión del agente: qué pidió la persona, qué dijo el modelo, qué herramienta pidió con qué entrada, quién autorizó y cómo terminó. Es la fuente para reconstruir un incidente; no debe guardar secretos ni datos personales reales.
**En el curso:** `Bitacora` escribe un renglón JSON por evento (`EventoAgente`) en `bitacoras/`, carpeta que no se versiona.

---

### Bucle del agente
**Categoría:** LLM / Arquitectura
**Definición:** El ciclo que manda la conversación al modelo, ejecuta las herramientas que pide, devuelve los resultados y repite hasta que el modelo deja de pedir o se alcanza un límite. Es código propio: ahí viven los controles.
**En el curso:** `BucleAgente.EjecutarAsync`, escrito a mano para entender cada paso.

---

## C

### Compuerta de confirmación
**Categoría:** Seguridad / Agentes
**Definición:** Punto del bucle donde una persona ve los datos exactos de una acción de escritura y decide si se ejecuta. Vive en el código, no en el prompt, y su valor por omisión es no ejecutar.
**En el curso:** el bloque `if (herramienta.Escribe)` de `EjecutarHerramientaAsync`, con `IConfirmador`. En consola, solo "si" confirma.

---

### Costura
**Categoría:** Pruebas / Diseño
**Definición:** Un punto del código donde se puede sustituir una dependencia sin cambiar el código que la usa, normalmente una interfaz. Permite probar con dobles lo que en producción depende de algo lento, caro o no determinista.
**En el curso:** `IModelo`, que en producción llama a la API y en las pruebas es `ModeloGuionado`.

---

## D

### Descripción de herramienta
**Categoría:** Herramientas / Diseño
**Definición:** El texto que lee el modelo para decidir si usa una herramienta y cuándo. Debe decir cuándo usarla, no solo qué hace; una descripción vaga produce una herramienta que no se usa o que se usa cuando no toca.
**En el curso:** "Úsala SIEMPRE antes de crear un ticket" en `buscar_tickets`.

---

## E

### Esquema de entrada
**Categoría:** Herramientas / Contratos
**Definición:** El JSON Schema que describe los argumentos de una herramienta: tipos, campos obligatorios y valores permitidos. No puede expresar longitudes ni rangos, así que las reglas de negocio se validan en el código de la herramienta.
**En el curso:** se genera de una clase de C# con `StructuredOutput.ToJsonSchema<TEntrada>()`, por ejemplo `EntradaCrearTicket`.

---

## F

### Firma del pensamiento
**Categoría:** API / Pensamiento
**Definición:** Valor que acompaña a cada bloque de pensamiento del modelo y que la API verifica cuando la conversación se reenvía. Si el bloque se edita, se quita o se cambia un turno anterior, el pensamiento puede invalidarse. Por eso la conversación de un agente solo crece.
**En el curso:** el bucle devuelve cada `ThinkingBlockParam` con su `Signature` intacta.

---

## H

### Herramienta
**Categoría:** LLM / Conceptos
**Definición:** Una función de tu sistema que el modelo puede pedir que se ejecute, descrita con nombre, descripción y esquema de entrada. El modelo no ejecuta nada: pide, y tu código decide si ejecuta.
**En el curso:** `IHerramienta` y la base `Herramienta<TEntrada>`, con la bandera `Escribe` que declara si modifica datos.

---

## I

### is_error
**Categoría:** API / Herramientas
**Definición:** Campo de un `tool_result` que indica que la herramienta falló o fue rechazada. El resultado se manda igual, con un mensaje que diga qué corregir; omitirlo deja un `tool_use` sin respuesta y la API rechaza la conversación.
**En el curso:** `IsError = error` en `ToolResultBlockParam`, usado para reglas de negocio, rechazos de la persona y errores internos (estos últimos con un aviso genérico).

---

## L

### Límite de pasos
**Categoría:** Agentes / Control
**Definición:** El número máximo de vueltas del bucle. Evita que un agente que busca, no encuentra y vuelve a buscar lo haga para siempre. Se fija en varias veces lo normal y, al alcanzarlo, el agente se detiene y lo dice.
**En el curso:** `LimitesAgente.MaxPasos`, 8 por omisión.

---

## R

### Respuesta guionada
**Categoría:** Pruebas / Dobles
**Definición:** Respuesta fija que un doble de prueba devuelve en lugar del modelo real, en el orden que dicta un guion. Permite probar el bucle sin red, sin costo y de forma determinista; no prueba si el modelo decide bien.
**En el curso:** `ModeloGuionado`, que arma objetos `Message` del SDK desde el JSON que devolvería la API.

---

## S

### stop_reason
**Categoría:** API / Respuestas
**Definición:** Campo de la respuesta que dice por qué se detuvo el modelo: `end_turn` (terminó), `tool_use` (pidió herramientas), `max_tokens` (se cortó), `refusal` (declinó por política), entre otros. El bucle decide su siguiente paso con este campo.
**En el curso:** `StopReason.ToolUse` hace seguir al bucle; `StopReason.Refusal` lo detiene antes de leer nada más.

---

### strict
**Categoría:** API / Herramientas
**Definición:** Opción de la definición de una herramienta que garantiza que la entrada que manda el modelo cumple el esquema exactamente. Requiere `additionalProperties: false`. Garantiza la forma, no las reglas de negocio.
**En el curso:** `Strict = true` en `Herramienta<TEntrada>.Definicion`.

---

## T

### Tool runner
**Categoría:** SDK / Herramientas
**Definición:** Ayudante del SDK que hace el bucle del agente por ti: llama, ejecuta las funciones que registraste, devuelve los resultados y repite. Permite intervenir antes de ejecutar, por ejemplo para pedir confirmación.
**En el curso:** el `BetaToolRunner` del SDK de C# se menciona pero no se usa: el bucle se escribe a mano para entender cada paso.

---

### tool_choice
**Categoría:** API / Herramientas
**Definición:** Parámetro que controla si el modelo puede o debe usar herramientas: `auto` (decide él), `none` (no puede), `any` o `tool` (lo obligan). Claude Opus 5.5 y Claude Sonnet 5.5 rechazan con 400 el forzado con `any` o `tool`.
**En el curso:** todo el curso usa `auto` con una instrucción clara en el prompt y `strict` en las herramientas.

---

### tool_result
**Categoría:** API / Herramientas
**Definición:** Bloque que tu código manda de vuelta al modelo con el resultado de una herramienta, ligado por `tool_use_id` al pedido que contesta. Todos los resultados de un turno van en un solo mensaje de usuario.
**En el curso:** `ToolResultBlockParam`, armado en `BucleAgente.Resultado`.

---

### tool_use
**Categoría:** API / Herramientas
**Definición:** Bloque de la respuesta del modelo que pide ejecutar una herramienta, con su id, su nombre y su entrada en JSON. Una respuesta puede traer varios.
**En el curso:** el bucle los lee con `TryPickToolUse`, los devuelve en el eco del asistente y ejecuta cada uno.

---

### Tope de gasto
**Categoría:** Operación / Costos
**Definición:** El costo máximo que puede acumular una ejecución del agente, medido con el uso real que reporta la API. Al pasarlo, el agente se detiene sin ejecutar lo que el modelo pidió en esa respuesta.
**En el curso:** `LimitesAgente.TopeUsd`, 0.05 USD por omisión, calculado con `TablaDePrecios`.

---

## U

### Uso paralelo de herramientas
**Categoría:** API / Herramientas
**Definición:** Cuando el modelo pide varias herramientas en una sola respuesta. Se ejecutan todas y sus resultados se devuelven juntos, en un solo mensaje; partirlos en varios mensajes le enseña al modelo a dejar de pedir en paralelo.
**En el curso:** el bucle junta la lista `resultados` y la manda una vez por vuelta.
