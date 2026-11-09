# Glosario Técnico - S01
## Qué es un LLM y cómo elegirlo

> Los términos de la sesión 1, en orden alfabético. Las cifras de precios son de noviembre de 2026: verifícalas en la página oficial antes de usarlas en un presupuesto.

---

## B

### Batches (lotes de mensajes)
**Categoría:** API / Costos
**Definición:** Forma asíncrona de mandar muchas peticiones a la API de mensajes en un solo envío (`/v1/messages/batches`). Cuesta 50 % menos que la llamada normal a cambio de no tener respuesta inmediata. Los resultados llegan en cualquier orden y se identifican por un `custom_id` que pone quien manda el lote.
**En el curso:** Se menciona como la palanca para procesos sin prisa (reportes nocturnos, evaluaciones grandes) al calcular el costo mensual de la tarea.

---

## C

### Caché de prompts
**Categoría:** API / Costos
**Definición:** Mecanismo que guarda el inicio de un prompt que se repite (instrucciones, ejemplos, documentos fijos) para que las siguientes llamadas lo lean en lugar de procesarlo de nuevo. Escribir en la caché cuesta 1.25 veces el precio de entrada (vida de 5 minutos) y leer cuesta 0.1 veces en la mayoría de los modelos. Funciona por coincidencia exacta del prefijo: cualquier byte distinto al inicio la invalida.
**En el curso:** En esta sesión entra en la fórmula del costo; en la sesión 2 se aplica al prompt de extracción.

### Costo por tarea
**Categoría:** Costos / Medición
**Definición:** Lo que cuesta llegar a un resultado útil, sumando todas las llamadas que hicieron falta: reintentos, validaciones fallidas y vueltas de un agente. Es la medida correcta para comparar modelos, porque un modelo barato que necesita tres intentos puede costar más que uno caro que resuelve en uno.
**En el curso:** Es el criterio para elegir modelo y la columna principal de la tabla de la tarea.

### count_tokens
**Categoría:** API
**Definición:** Endpoint de la API (`POST /v1/messages/count_tokens`) que regresa cuántos tokens de entrada tendría una petición, con el tokenizador del modelo indicado. Recibe lo mismo que la llamada real: modelo, sistema y mensajes. No mide la salida, que solo se conoce cuando el modelo responde.
**En el curso:** `LlamadaMedida.ContarTokensAsync` lo usa antes de cada llamada para comparar lo contado contra lo cobrado.

---

## E

### Effort (esfuerzo)
**Categoría:** API / Calidad
**Definición:** Parámetro dentro de `output_config` que indica cuánto debe pensar y trabajar el modelo: `low`, `medium`, `high`, `xhigh` o `max`. Controla la profundidad del pensamiento y el gasto de tokens. El valor por omisión es `medium` en Opus 5.5 y Haiku 5.5, y `high` en Sonnet 5.5.
**En el curso:** La práctica usa `Effort.Low` porque la pregunta es corta; antes de bajar a un modelo más chico conviene probar el mismo modelo con menos esfuerzo.

### Evaluación a ciegas
**Categoría:** Medición / Calidad
**Definición:** Calificación de respuestas en la que quien califica no sabe qué modelo produjo cada una. Evita que la reputación de un modelo influya en la nota. Se complementa con una rúbrica escrita antes de ver las respuestas.
**En el curso:** En la práctica, un compañero renombra las respuestas como A y B antes de calificar `comparacion.md`.

---

## L

### Latencia al primer token
**Categoría:** Medición / Rendimiento
**Definición:** Tiempo desde que se manda la petición hasta que llega el primer pedazo de texto de la respuesta. Es la latencia que siente una persona cuando la respuesta se muestra en streaming. Con pensamiento adaptativo, incluye el tiempo que el modelo pasó pensando antes de escribir.
**En el curso:** `LlamadaMedida` la mide con un `Stopwatch` y la reporta junto al tiempo total.

### LLM (modelo de lenguaje grande)
**Categoría:** Conceptos
**Definición:** Modelo entrenado con grandes cantidades de texto que, dada una secuencia de tokens, calcula la probabilidad del siguiente y genera texto token por token. No consulta una base de datos: lo que sabe de un caso concreto llega en el prompt, y puede inventar con la misma seguridad con que acierta.
**En el curso:** Los cuatro modelos vigentes de Claude (Fable 5.1, Opus 5.5, Sonnet 5.5 y Haiku 5.5) son el objeto de la comparación.

---

## M

### max_tokens
**Categoría:** API
**Definición:** Tope de tokens que puede generar una respuesta. El servidor lo aplica; el modelo no lo ve. Incluye el pensamiento, así que un tope muy bajo puede agotarse antes de que aparezca texto. Si se alcanza, la respuesta se corta y `stop_reason` vale `max_tokens`.
**En el curso:** En "Verlo fallar" se baja temporalmente a 16 para ver el corte y cómo el pensamiento consume el presupuesto.

---

## P

### Pensamiento adaptativo
**Categoría:** Conceptos / API
**Definición:** Comportamiento de los modelos vigentes en el que el modelo decide cuánto razonar antes de responder. Ese razonamiento genera tokens que se cobran como salida y cuentan contra `max_tokens`. Por omisión el texto del pensamiento no se devuelve: llega un bloque `thinking` vacío con su firma. En Opus 5.5 no se puede apagar; se regula con `effort`.
**En el curso:** Explica por qué el primer bloque de una respuesta puede no ser texto y por qué el costo de salida sube en tareas difíciles.

### Percentil 95 (p95)
**Categoría:** Medición
**Definición:** Valor por debajo del cual queda el 95 % de las mediciones. En latencia, describe el peor caso común: el que le toca a uno de cada veinte usuarios. Se reporta junto al p50 (mediana) porque el promedio esconde las colas lentas.
**En el curso:** La práctica mide una vez para aprender a medir; la tarea y la operación real piden una muestra y percentiles.

---

## R

### Rúbrica
**Categoría:** Medición / Calidad
**Definición:** Lista de criterios con niveles de calificación (por ejemplo 0, 1 y 2) que se aplica igual a todas las respuestas. Se escribe antes de ver los resultados para no ajustarla al favorito. Convierte una impresión en un número comparable.
**En el curso:** El comando `comparar` escribe en `comparacion.md` una rúbrica de cuatro criterios para calificar a mano.

---

## S

### SDK oficial
**Categoría:** Herramientas
**Definición:** Librería que publica el proveedor para llamar a su API desde un lenguaje. Para C# es el paquete NuGet `Anthropic`: agrega tipos, reintentos automáticos, streaming y excepciones por código de error. Su versión determina qué versión de la API se manda.
**En el curso:** Se usa `Anthropic` 13.6.0, fijado en `Directory.Packages.props`, que manda la versión de API 2026-10-21.

### stop_reason
**Categoría:** API
**Definición:** Campo de la respuesta que dice por qué terminó el modelo: `end_turn` (terminó normal), `max_tokens` (se cortó), `tool_use` (quiere usar una herramienta) o `refusal` (se negó por seguridad), entre otros. Un sistema que no lo revisa trata respuestas cortadas como completas.
**En el curso:** `LlamadaMedida` lo guarda en la `Medicion`; en la sesión 2 decide si el JSON se puede leer.

### Streaming
**Categoría:** API / Rendimiento
**Definición:** Modo en que la respuesta llega por eventos mientras se genera, en lugar de esperar a que termine. Permite mostrar texto en cuanto aparece y es obligatorio en el SDK para salidas muy largas, para no chocar con el tiempo de espera HTTP. El uso de tokens llega repartido entre el primer y el último evento.
**En el curso:** `LlamadaMedida.PreguntarAsync` usa `CreateStreaming` para medir el primer token.

---

## T

### Temperatura
**Categoría:** Conceptos / API
**Definición:** Parámetro de muestreo que, en APIs anteriores, hacía más plana o más picuda la distribución de probabilidades del siguiente token: baja para respuestas repetibles, alta para variedad. Junto con `top_p` y `top_k`, ya no se acepta en la versión 2026-10-21 de la API de Claude: mandarlo regresa 400. La variabilidad se controla con el prompt, los ejemplos, la salida estructurada y `effort`.
**En el curso:** Se explica como concepto porque aparece en los libros, y se corrige: el SDK 13.x ni siquiera expone la propiedad.

### Token
**Categoría:** Conceptos / Costos
**Definición:** Unidad en que el modelo lee y escribe texto: un pedazo de palabra, una palabra completa o un signo. Todo se cobra en tokens, por separado los de entrada y los de salida. No existe una equivalencia fija con caracteres: depende del idioma, del tipo de texto y del tokenizador.
**En el curso:** Es la unidad de la fórmula de costo de `TablaDePrecios`.

### Tokenizador
**Categoría:** Conceptos
**Definición:** El componente que convierte texto en tokens. Cambia entre proveedores y entre generaciones de un mismo proveedor: Claude Haiku 5.5 cuenta cerca de 30 % más tokens que Haiku 4.5 para el mismo texto. Por eso los conteos de un modelo no sirven para otro y nunca se cuenta con el tokenizador de otro proveedor.
**En el curso:** Es la razón de usar `count_tokens` con el mismo modelo que se va a llamar.

### Tope de gasto
**Categoría:** Seguridad / Costos
**Definición:** Límite de dinero que se configura en la consola del proveedor para una organización o espacio de trabajo; al alcanzarlo, las llamadas fallan en lugar de seguir cobrando. Se complementa con topes por usuario en el código de la aplicación.
**En el curso:** La llave del curso tiene tope. En la sesión 4 el agente tiene un tope por ejecución y en la sesión 6 uno diario por usuario.

---

## V

### Ventana de contexto
**Categoría:** Conceptos
**Definición:** Máximo de tokens que el modelo puede considerar en una llamada, sumando entrada y salida. Los cuatro modelos vigentes de Claude tienen 1 millón de tokens. Que algo quepa no significa que convenga: más contexto es más costo, más latencia y más ruido.
**En el curso:** Motiva la sesión 3: mandar solo los fragmentos relevantes en lugar de documentos enteros.

### Versión de la API
**Categoría:** API
**Definición:** Fecha que se manda en la cabecera `anthropic-version` y que fija el contrato de la API: qué campos acepta y cómo responde. Los cambios incompatibles salen en versiones nuevas. La versión 2026-10-21 quitó `temperature`, `top_p` y `top_k`; el SDK de C# la manda desde su versión 13.0.0.
**En el curso:** El fragmento con `HttpClient` de la clase la manda a mano; el SDK la manda solo.
