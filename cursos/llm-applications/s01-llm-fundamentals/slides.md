<!-- _class: lead -->

# S01 - Qué es un LLM y cómo elegirlo

### Aplicaciones con LLM: de la API al agente

**Rogelio Arriaga - Raptor Dev Services**

<!-- Instructor: presentación breve. Pregunta de arranque: "¿quién ya pagó una factura de API de un modelo y se sorprendió?". Anota las respuestas: regresamos a ellas en la parte de costos. -->

---

## Agenda del día (5 horas)

| Bloque | Tema | Tiempo |
|---|---|---|
| 1 | Qué es un LLM: tokens, contexto, pensamiento | 60 min |
| 2 | Los modelos y lo que cuestan | 50 min |
| 3 | Latencia: primer token y total | 30 min |
| 4 | Comparar modelos y proveedores con rúbrica | 40 min |
| 5 | Práctica: primera llamada medida y comparación | 100 min |
| 6 | Cierre, tarea y lecturas | 20 min |

<!-- Instructor: deja un receso de 10 minutos entre el bloque 3 y el 4, y otro a mitad de la práctica. -->

---

> Elegir modelo es elegir costo, latencia y calidad. Hay que medir los tres.

<!-- Instructor: lee la frase y pregunta cuál de los tres miden hoy en su trabajo. Casi siempre la respuesta es "ninguno, lo elegimos por reputación". Ese es el problema de la sesión. -->

---

## Al terminar puedes

**Justificar con números qué modelo usar y cuánto va a costar.**

- Contar tokens antes de gastar, con el mismo modelo que vas a usar
- Calcular el costo de una llamada y de un mes
- Medir el tiempo al primer token y el total
- Comparar dos modelos con una rúbrica escrita antes de ver las respuestas

<!-- Instructor: esta lámina es el contrato de la sesión. Al final regresamos a ella y cada quien marca qué puede hacer ya. -->

---

## El proyecto del curso: un asistente que crece

| Sesión | Lo que agrega |
|---|---|
| S01 | Llamadas medidas: tokens, costo y latencia |
| S02 | Extracción a JSON validado, con prompts versionados |
| S03 | RAG sobre los documentos del curso, con citas |
| S04 | Un agente con herramientas y confirmación humana |
| S05 | Un servidor MCP con el catálogo de cursos |
| S06 | Defensas contra prompt injection y evaluación en CI |

Un solo repositorio, `AsistenteCursos`, en C# con .NET 10 y el SDK oficial de Anthropic.

<!-- Instructor: deja claro que no hay ejercicios sueltos. Lo que escriban hoy lo usan en la sesión 6. -->

---

## La regla del curso

**Si no lo viste fallar, no lo probaste.**

- Una prueba que nunca vio fallar puede estar probando nada
- Cada práctica trae un paso "Verlo fallar"
- Con modelos esto pesa el doble: una respuesta que se ve bien no prueba nada

<!-- Instructor: cuenta un caso propio de una prueba verde que no probaba nada. Pide que lo recuerden cada vez que algo pase a la primera. -->

---

<!-- _class: lead -->

# Parte 1
## Qué es un LLM

---

## Un LLM predice el siguiente token

- Recibe una secuencia de tokens y calcula qué tan probable es cada token siguiente
- Elige uno, lo agrega a la secuencia y repite
- No consulta una base de datos: genera texto que se parece a lo que vio al entrenarse
- Por eso puede inventar con la misma seguridad con que acierta

**Consecuencia práctica:** todo lo que el modelo "sabe" de tu caso llega en el prompt.

<!-- Instructor: no entres en arquitectura de transformadores. Lo que importa para construir producto es la consecuencia: el modelo no sabe nada de tu empresa salvo lo que le mandes. -->

---

## Qué es un token

- La unidad en que el modelo lee y escribe: un pedazo de palabra, una palabra o un signo
- Las palabras comunes suelen ser un token; las raras se parten en varios
- El código, los números largos y los idiomas distintos al inglés suelen gastar más tokens por carácter
- **Todo se cobra en tokens:** lo que mandas (entrada) y lo que recibes (salida)

<!-- Instructor: no des una equivalencia fija "un token son 4 caracteres". Es una regla de dedo que falla justo en español y en código. Por eso medimos. -->

---

## El tokenizador cambia entre modelos

- Cada familia de modelos puede partir el mismo texto de forma distinta
- Ejemplo real: **Claude Haiku 5.5 cuenta cerca de 30 % más tokens** que Haiku 4.5 para el mismo texto
- Un `max_tokens` o un presupuesto afinado con un modelo no sirve para otro
- Al cambiar de modelo: **se vuelve a medir**, no se reutilizan los conteos

<!-- Instructor: este es el error típico al migrar. El tablero de costos sube 30 % sin que cambie el tráfico y alguien abre un incidente. No es un incidente: es otro tokenizador. -->

---

## Contar con el modelo correcto

- La API tiene un endpoint de conteo: `POST /v1/messages/count_tokens`
- Recibe lo mismo que la llamada real: modelo, sistema y mensajes
- **Nunca uses tiktoken** ni otro tokenizador ajeno: es de otro proveedor y cuenta de menos

```csharp
var conteo = await cliente.Messages.CountTokens(new MessageCountTokensParams
{
    Model = modelo,
    System = sistema,
    Messages = [new UserMessageParam { Content = pregunta }],
}, ct);
return conteo.InputTokens;
```

<!-- Instructor: el conteo solo mide la entrada. La salida no se sabe hasta que el modelo responde; por eso el costo real se toma del uso que regresa la respuesta. -->

---

## La ventana de contexto

- Es el máximo de tokens que el modelo ve en una llamada: entrada más salida
- Los cuatro modelos vigentes de Claude tienen **1 millón de tokens** de contexto
- Que quepa no quiere decir que convenga:
  - más contexto, más costo por llamada
  - más contexto, más latencia
  - más contexto irrelevante, más ruido para el modelo

**Regla:** manda lo que el modelo necesita para esta tarea, no todo lo que tienes.

<!-- Instructor: pregunta qué harían con un manual de 800 páginas. La respuesta de la sesión 3 es RAG: mandar solo los fragmentos que importan. -->

---

## La salida máxima y el streaming

- `max_tokens` es el tope de la respuesta; el modelo no lo ve, el servidor lo aplica
- Los modelos vigentes aceptan hasta **128 mil tokens** de salida
- Con salidas largas, el SDK pide **streaming**: la respuesta llega por eventos y no choca con el tiempo de espera HTTP
- Si la respuesta llega a `max_tokens`, se corta a media frase: `stop_reason` vale `max_tokens`

<!-- Instructor: recalca que max_tokens corto no ahorra dinero si te obliga a repetir la llamada. Se cobra lo que se generó, aunque no sirva. -->

---

## Pensamiento adaptativo

- Los modelos vigentes **piensan antes de responder** cuando la tarea lo amerita
- El pensamiento genera tokens que **se cobran como salida** y **cuentan contra `max_tokens`**
- Por omisión el texto del pensamiento no regresa: llega un bloque `thinking` vacío con su firma
- En Opus 5.5 el pensamiento no se puede apagar: se controla con `effort`

<!-- Instructor: este es el cambio que más sorprende a quien viene de modelos anteriores. Un max_tokens de 50 para una clasificación de una palabra puede quedarse sin espacio para la respuesta. -->

---

## El parámetro effort

| Nivel | Para qué |
|---|---|
| `low` | Chat, clasificación, extracción, tareas de alto volumen |
| `medium` | Valor por omisión en Opus 5.5 y Haiku 5.5 |
| `high` | Valor por omisión en Sonnet 5.5; trabajo que exige criterio |
| `xhigh`, `max` | Problemas difíciles donde la calidad pesa más que el costo |

```csharp
OutputConfig = new OutputConfig { Effort = Effort.Low },
```

<!-- Instructor: effort es la primera palanca de costo dentro de un mismo modelo. Antes de bajar a un modelo más chico, prueben el mismo modelo con effort más bajo. -->

---

## Temperatura: el concepto

- Al elegir el siguiente token, el modelo tiene una distribución de probabilidades
- La **temperatura** decía qué tan plana o picuda era esa distribución al muestrear
  - baja: casi siempre el token más probable, respuestas más repetibles
  - alta: más variedad, más sorpresas
- `top_p` y `top_k` recortaban la lista de candidatos antes de muestrear

**Lo verás en todos los libros y tutoriales de 2023 y 2024.**

<!-- Instructor: explica el concepto porque lo van a leer en todos lados. La siguiente lámina es la corrección. -->

---

## Temperatura: la corrección

- En la API vigente de Claude (versión **2026-10-21**, la que manda el SDK de C# 13.x) **`temperature`, `top_p` y `top_k` ya no se aceptan**: la petición regresa **400**
- En la clase `MessageCreateParams` del SDK ni siquiera existe la propiedad
- Lo que controla la variabilidad hoy:
  - un prompt de sistema con reglas verificables
  - ejemplos del caso difícil
  - salida estructurada con esquema (sesión 2)
  - `effort`

<!-- Instructor: el error clásico es copiar temperature: 0 de un tutorial para "hacerlo determinista". Hoy truena. Y aun cuando existía, temperatura 0 no garantizaba la misma respuesta. La repetibilidad se consigue con contrato de salida y validación. -->

---

## Cómo se ve una llamada por debajo

```csharp
using var http = new HttpClient { BaseAddress = new Uri("https://api.anthropic.com") };
http.DefaultRequestHeaders.Add("x-api-key", Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
http.DefaultRequestHeaders.Add("anthropic-version", "2026-10-21");

using var respuesta = await http.PostAsJsonAsync("/v1/messages", new
{
    model = "claude-haiku-5-5",
    max_tokens = 1024,
    messages = new[] { new { role = "user", content = pregunta } },
}, ct);
```

Una sola ruta, `POST /v1/messages`, para todo: texto, herramientas y salida estructurada.

<!-- Instructor: lo muestro para quitarle la magia al SDK. En el proyecto usamos el SDK oficial, que agrega reintentos, tipos y streaming. No escriban HttpClient a mano salvo que el SDK no cubra algo. -->

---

## La respuesta trae bloques

```json
{
  "content": [
    { "type": "thinking", "thinking": "", "signature": "..." },
    { "type": "text", "text": "Un índice GIN conviene cuando..." }
  ],
  "stop_reason": "end_turn",
  "usage": { "input_tokens": 41, "output_tokens": 180 }
}
```

- El contenido es una **lista de bloques**: se filtra por tipo, **nunca por posición**
- Con pensamiento, el primer bloque ya no es el texto

<!-- Instructor: content[0].text es el error que más veo en código migrado. Con pensamiento adaptativo el primer bloque es thinking y el código revienta o regresa vacío. Los números del ejemplo son ilustrativos. -->

---

## stop_reason: léelo siempre

| Valor | Qué significa | Qué haces |
|---|---|---|
| `end_turn` | Terminó normal | Usar la respuesta |
| `max_tokens` | Se cortó | Subir el tope o bajar `effort` |
| `tool_use` | Quiere usar una herramienta | Sesión 4 |
| `refusal` | Se negó por seguridad | Registrar y no usar el contenido |

<!-- Instructor: un sistema en producción que no revisa stop_reason trata una respuesta cortada como si fuera completa. Pregunta qué pasa si guardan en base un JSON cortado a la mitad. -->

---

<!-- _class: lead -->

# Parte 2
## Los modelos y lo que cuestan

---

## La familia vigente de Claude

| Modelo | Id en la API | Cuándo pensar en él |
|---|---|---|
| Claude Fable 5.1 | `claude-fable-5-1` | Lo más difícil: razonamiento largo, agentes de horas |
| Claude Opus 5.5 | `claude-opus-5-5` | Trabajo exigente de código y agentes |
| Claude Sonnet 5.5 | `claude-sonnet-5-5` | Uso diario: agentes, código, documentos |
| Claude Haiku 5.5 | `claude-haiku-5-5` | Alto volumen: clasificar, extraer, enrutar |

Los cuatro con 1 millón de contexto y hasta 128 mil de salida.

<!-- Instructor: el id va tal cual, sin sufijo de fecha. En el proyecto viven en una sola clase, Modelos, para cambiarlos en un solo lugar. -->

---

## Precios de lista (revisados el 2026-11-08)

| Modelo | Entrada USD por millón | Salida USD por millón |
|---|---|---|
| Fable 5.1 | 10.00 | 50.00 |
| Opus 5.5 | 4.00 | 20.00 |
| Sonnet 5.5 | 2.00 | 10.00 |
| Haiku 5.5 | 0.10 | 0.50 |

**Antes de presupuestar, verifica en la página oficial de precios.** Cambian y tu código no se entera.

<!-- Instructor: haz notar la relación entrada contra salida: la salida cuesta cinco veces la entrada en los cuatro. Por eso una respuesta larga pesa más que un prompt largo. -->

---

## Haiku 5.5 tiene dos tarifas

- Hasta **100 mil tokens de prompt**: 0.10 de entrada y 0.50 de salida por millón
- Con prompts más largos: **0.50 y 2.50** por millón
- El prompt cuenta todo lo que entra: tokens normales, leídos de caché y escritos en caché

```csharp
if (modelo == Modelos.Haiku && tokensDelPrompt > LimitePromptCortoHaiku)
    return HaikuPromptLargo;
```

<!-- Instructor: este detalle cambia la decisión. Haiku con prompts gigantes cuesta cinco veces más por token. Si el caso de uso manda documentos enteros, el cálculo cambia. -->

---

## Caché de prompts: el descuento más grande

| Concepto | Multiplicador sobre el precio de entrada |
|---|---|
| Escribir en caché (5 minutos) | 1.25 |
| Escribir en caché (1 hora) | 2.0 |
| Leer de caché | 0.1 (Opus 5.5: 0.05; Fable 5.1: 0.025) |

- Sirve cuando el **inicio** del prompt se repite: instrucciones, ejemplos, documentos fijos
- Con 5 minutos de vida, dos llamadas ya pagan la escritura

<!-- Instructor: lo usamos en la sesión 2 con el prompt de extracción. Aquí solo quiero que lo metan en la cuenta del costo mensual. -->

---

## Batches: la mitad si no hay prisa

- La API de lotes (`/v1/messages/batches`) procesa peticiones de forma asíncrona
- Cuesta **50 % menos** que la llamada normal
- Los resultados llegan en cualquier orden: se identifican por `custom_id`, nunca por posición
- Útil para: reportes nocturnos, reclasificar un histórico, evaluaciones grandes

<!-- Instructor: pregunta qué procesos de su trabajo no necesitan respuesta en segundos. Casi todos tienen alguno, y nadie lo manda por lotes. -->

---

## La fórmula del costo de una llamada

```
costo = ( entrada        x precio_entrada
        + salida         x precio_salida
        + leídos_caché   x precio_lectura_caché
        + escritos_caché x precio_escritura_caché ) / 1 000 000
```

- **Entrada y salida se toman del uso que regresa la respuesta**, no de una estimación
- La salida incluye el pensamiento

<!-- Instructor: la fórmula vive en TablaDePrecios.Costo. Pide que la escriban en una hoja de cálculo también: la tarea la usa. -->

---

## La fórmula en código

```csharp
public static decimal Costo(string modelo, long entrada, long salida, long cacheLeidos, long cacheEscritos)
{
    var p = Para(modelo, entrada + cacheLeidos + cacheEscritos);
    return (entrada * p.Entrada
          + salida * p.Salida
          + cacheLeidos * p.LecturaCache
          + cacheEscritos * p.EscrituraCache) / 1_000_000m;
}
```

`decimal`, no `double`: es dinero.

<!-- Instructor: pregunten por qué decimal. Con double, sumar millones de llamadas de fracciones de centavo acumula error de redondeo binario. -->

---

## Un ejemplo con números

Una respuesta de soporte: **2,000 tokens de entrada y 400 de salida.**

| Modelo | Costo por llamada | 30,000 llamadas al mes |
|---|---|---|
| Haiku 5.5 | 0.0004 USD | 12 USD |
| Sonnet 5.5 | 0.008 USD | 240 USD |
| Opus 5.5 | 0.016 USD | 480 USD |
| Fable 5.1 | 0.04 USD | 1,200 USD |

Cien veces de diferencia entre el extremo barato y el caro. **La pregunta es si la calidad también es cien veces mejor para tu tarea.**

<!-- Instructor: haz la cuenta de Haiku en el pizarrón: 2000 x 0.10 / 1e6 + 400 x 0.50 / 1e6. Que vean que no es magia. Sin caché y sin pensamiento extra. -->

---

## Del costo por llamada al costo mensual

```
costo_mes = llamadas_por_día x días x costo_por_llamada x (1 + tasa_de_reintentos)
```

Supuestos que hay que escribir:

- Tokens de entrada y salida **medidos** en una muestra real, no adivinados
- Qué parte del prompt sale de caché
- Cuántas llamadas se repiten por error o validación
- El pico, no solo el promedio

<!-- Instructor: la tarea de hoy es exactamente esta tabla para un caso de uso suyo, con tres escenarios. -->

---

## El costo que no se ve

- **Reintentos:** cada uno se cobra completo
- **Pensamiento:** una tarea difícil con `effort` alto puede triplicar la salida
- **Contexto que crece:** en una conversación cada turno reenvía todo lo anterior
- **Agentes:** cada vuelta del bucle es una llamada (sesión 4)

**Mide el costo por tarea terminada**, no por llamada: una llamada barata que necesita tres intentos no es barata.

<!-- Instructor: este es el concepto clave para elegir modelo. A veces el modelo caro resuelve en una llamada lo que el barato resuelve en cuatro. -->

---

## El tope de gasto

- La llave de la API se crea en la consola con un **límite de gasto**
- Si el límite se alcanza, las llamadas fallan en lugar de seguir cobrando
- En este curso: una llave por grupo con tope, y modelos chicos en las prácticas
- En tu producto: tope por organización en la consola **y** tope por usuario en tu código (sesión 6)

<!-- Instructor: muestra en la consola dónde se configura el límite. Nadie debería tener una llave sin tope, y menos en una laptop. -->

---

<!-- _class: lead -->

# Parte 3
## Latencia

---

## Dos latencias distintas

| Medida | Qué es | Cuándo importa |
|---|---|---|
| **Tiempo al primer token** | Desde que mandas hasta que llega el primer texto | Chat, cualquier cosa que una persona ve escribirse |
| **Tiempo total** | Hasta el último token | Procesos en segundo plano, extracción, lotes |

- Con streaming, la persona empieza a leer en el primer token
- Sin streaming, espera el total completo

<!-- Instructor: pregunta qué ven en ChatGPT o en Claude cuando escriben una pregunta. El texto aparece poco a poco: eso es streaming, y la latencia que perciben es la del primer token. -->

---

## Medir el primer token

```csharp
await foreach (var evento in cliente.Messages.CreateStreaming(parametros, ct))
{
    if (evento.TryPickStart(out var inicio))
    {
        entrada = inicio.Message.Usage.InputTokens;
    }
    else if (evento.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var fragmento))
    {
        primerToken ??= reloj.Elapsed;
        texto.Append(fragmento.Text);
    }
}
```

<!-- Instructor: el operador ??= guarda el tiempo solo la primera vez. Con pensamiento adaptativo, el primer token de TEXTO llega después de pensar: eso también es latencia que el usuario siente. -->

---

## El uso llega repartido

- `message_start`: los tokens de **entrada** y de caché
- `content_block_delta`: los pedazos de texto
- `message_delta`: los tokens de **salida** acumulados y el `stop_reason`

```csharp
else if (evento.TryPickDelta(out var cierre))
{
    salida = cierre.Usage.OutputTokens;
    stopReason = cierre.Delta.StopReason?.ToString() ?? stopReason;
}
```

<!-- Instructor: si solo leen el primer evento, el costo de salida les sale en cero. Error real que he visto en tableros de costo. -->

---

## Qué mueve la latencia

- **El modelo:** los chicos responden antes
- **effort:** más pensamiento, más espera antes del primer texto
- **El tamaño de la salida:** el tiempo total crece con cada token generado
- **El tamaño de la entrada:** más contexto, más tiempo de procesamiento
- **La caché:** un prefijo leído de caché se procesa más rápido

**Pedir respuestas cortas es la forma más barata de bajar latencia y costo a la vez.**

<!-- Instructor: la instrucción "máximo 120 palabras" del sistema de la práctica no es estética: baja costo y latencia. -->

---

## El promedio miente

- Una medición sola no dice nada: la latencia varía entre llamadas
- Reporta **percentiles**: p50 (la mitad tarda menos) y p95 (el peor caso común)
- Un p50 de 2 segundos con un p95 de 25 es un producto lento para uno de cada veinte usuarios

<!-- Instructor: en la práctica medimos una sola vez por modelo para aprender a medir. En la tarea y en producción, una muestra. -->

---

<!-- _class: lead -->

# Parte 4
## Comparar modelos y proveedores

---

## Con números, no con impresiones

- "Este modelo es más inteligente" no es un criterio de compra
- La pregunta correcta: **¿cumple esta tarea, con esta calidad, a este costo y en este tiempo?**
- Se mide en **tu** tarea, con **tus** datos: los rankings públicos miden otras cosas

<!-- Instructor: pregunta cuántos eligieron su modelo actual por un benchmark público. Ese benchmark no tenía sus tickets de soporte. -->

---

## Una rúbrica, escrita antes

| Criterio | 0 | 1 | 2 |
|---|---|---|---|
| Exactitud técnica | Error grave | Impreciso | Correcto |
| Responde lo que se preguntó | No | En parte | Sí |
| Respeta el formato pedido | No | Casi | Sí |
| Da un ejemplo concreto | No | Vago | Sí |

**Se escribe antes de ver las respuestas.** Si la escribes después, la ajustas al favorito.

<!-- Instructor: esta es la rúbrica que genera el comando comparar en comparacion.md. En la tarea pueden cambiar los criterios por los de su caso. -->

---

## A ciegas

- Quien califica **no sabe qué modelo escribió cada respuesta**
- Un compañero renombra los archivos como A y B; tú calificas; después se revela
- Sin esto, el modelo caro gana por reputación

<!-- Instructor: en la práctica lo hacen en parejas. Es incómodo y por eso funciona. -->

---

## Una pregunta no es una evaluación

- La comparación de la práctica es un ejercicio de medición, no una decisión
- Para decidir: **de 20 a 50 casos reales**, con los difíciles incluidos
- Se califican todos con la misma rúbrica y se saca la tasa de aciertos
- En la sesión 2 esto se vuelve una prueba automática con umbral

<!-- Instructor: adelanta la idea de evaluación automática. Aquí la rúbrica la aplica una persona; después la aplica el código y, en parte, otro modelo como juez. -->

---

## Comparar proveedores

Misma tarea, mismos casos, misma rúbrica. Lo que cambia y hay que anotar:

- **El tokenizador:** los tokens no son comparables entre proveedores; compara **dólares por tarea**
- Límites de peticiones por minuto
- Retención de datos y regiones donde corre
- Plataformas donde está disponible (API directa o nube)
- Calidad del SDK para tu lenguaje

<!-- Instructor: si alguien dice "el proveedor X da respuestas de menos tokens", la respuesta es: ¿y cuánto cuesta la tarea terminada? -->

---

## Elegir: el más barato que cumple

1. Define el umbral de calidad para la tarea (por ejemplo, 90 % de aciertos)
2. Mide los modelos candidatos con los mismos casos
3. Descarta los que no llegan al umbral
4. De los que quedan, elige el de **menor costo por tarea** que cumpla la latencia
5. Antes de bajar de modelo, prueba el mismo modelo con **effort más bajo**

<!-- Instructor: el paso 5 suele ganar. Un modelo grande con effort bajo puede igualar a uno chico con effort alto, con menos reintentos. -->

---

## Por tipo de tarea, como punto de partida

| Tarea | Empieza con |
|---|---|
| Clasificar, extraer, enrutar, alto volumen | Haiku 5.5, effort bajo |
| Responder con documentos, agentes de soporte | Sonnet 5.5 o Haiku 5.5, según la evaluación |
| Código complejo, agentes largos | Opus 5.5 |
| El problema más difícil que tengas | Fable 5.1 |

**Es un punto de partida, no una decisión.** La decisión sale de medir.

<!-- Instructor: en las prácticas usamos Haiku 5.5 por costo. La variable ASISTENTE_MODELO deja cambiarlo sin recompilar. -->

---

<!-- _class: lead -->

# Parte 5
## Manos a la obra

---

## La llave: del entorno, nunca del código

```bash
export ANTHROPIC_API_KEY='<SU_LLAVE>'
```

- La llave vive en una variable de entorno o en un gestor de secretos
- **Nunca** en el código, en `appsettings.json` ni en el repositorio
- `.gitignore` excluye `.env` desde el primer commit
- Llave del curso con tope de gasto: si se filtra, el daño tiene techo

<!-- Instructor: reparte las llaves del curso por un canal privado, nunca en el chat del grupo. Si alguien la sube a un repositorio público, se revoca de inmediato. -->

---

## El SDK oficial, con versión fijada

```xml
<PackageVersion Include="Anthropic" Version="13.6.0" />
```

- Paquete NuGet `Anthropic`, el SDK oficial de C#
- La versión vive en `Directory.Packages.props`: una sola para todo el repositorio
- Fijar la versión importa: el SDK 13 cambió la versión de la API que manda

<!-- Instructor: explica la gestión central de paquetes. Cuando el curso suba de versión, se cambia una línea y se corren las pruebas. -->

---

## El cliente no conoce la llave

```csharp
// La llave sale del entorno (ANTHROPIC_API_KEY): nunca del código ni del repositorio.
var cliente = new AnthropicClient();
```

```csharp
catch (AnthropicUnauthorizedException)
{
    Console.Error.WriteLine("La API rechazó la credencial (401). Revisa la variable ANTHROPIC_API_KEY.");
    return 2;
}
```

<!-- Instructor: el constructor sin argumentos lee la variable de entorno. El catch convierte una traza de 20 líneas en un mensaje que una persona entiende. -->

---

## Lo que construimos hoy

- `Modelos`: los ids en un solo lugar
- `TablaDePrecios`: la fórmula del costo, con la tarifa doble de Haiku
- `LlamadaMedida`: cuenta, llama en streaming y mide
- `Comparar`: la misma pregunta a Haiku 5.5 y a Sonnet 5.5, y un `comparacion.md` con la rúbrica

```bash
dotnet run --project src/Asistente.Cli -- comparar
```

<!-- Instructor: abre la práctica y que cada quien vaya a su ritmo. Pasa por los lugares en el paso 7, que es donde aparecen los problemas de llave y de red. -->

---

## Verlo fallar

1. **Sin llave:** corre `comparar` sin la variable de entorno. Debe salir el mensaje del 401, no una traza
2. **Sin espacio:** baja `MaxTokens` a 16 por un momento. La salida se corta, `stop_reason` vale `max_tokens` y el pensamiento se come el presupuesto
3. Regresa los valores y vuelve a correr: todo en verde

<!-- Instructor: el segundo caso es el que enseña. Que vean que con 16 tokens a veces no llega ni una palabra de texto, porque el pensamiento se quedó con todo. -->

---

## Los libros de esta sesión

| Libro | Capítulos | Para qué |
|---|---|---|
| *Building LLM Powered Applications* (Valentina Alto, 2024) | 1 a 3 | Qué es un LLM, cómo se elige y cómo se integra en una aplicación |

**Ojo:** es de 2024 y usa APIs y librerías que ya cambiaron (por ejemplo, temperatura). Léelo por los conceptos; el código del curso va contra la API vigente.

<!-- Instructor: pide que anoten en los márgenes cada cosa del libro que hoy ya no es así. Es un buen ejercicio de lectura crítica. -->

---

## Tarea

**Una tabla de costo mensual estimado para un caso de uso suyo.**

- Un caso real de su trabajo o de un proyecto propio
- Tokens medidos con `count_tokens` y con el comando `comparar`, no adivinados
- Tres escenarios (bajo, esperado y pico), con caché y sin caché, en al menos dos modelos
- La recomendación final, justificada con números

El detalle está en la tarea de la sesión.

<!-- Instructor: la próxima sesión arranca revisando dos o tres tablas en voz alta. -->

---

## Lecturas para la sesión 2

- `dev-notes/11-vibe-coding/02-prompts-efectivos.md`
- `dev-notes/11-vibe-coding/05-llmops.md`
- Opcional: `dev-notes/11-vibe-coding/07-llm-en-produccion.md`, secciones "El costo se mide por token" y "Elegir modelo con números"

<!-- Instructor: la nota 05 usa un modelo anterior y el parámetro temperature. Que la lean por los conceptos y detecten qué ya no aplica: es parte del ejercicio. -->

---

## Cierre

**Al terminar puedes justificar con números qué modelo usar y cuánto va a costar.**

- ¿Puedes contar tokens con el modelo correcto?
- ¿Puedes calcular el costo de una llamada y de un mes?
- ¿Mediste el primer token y el total?
- ¿Calificaste a ciegas con una rúbrica escrita antes?

<!-- Instructor: que cada quien conteste las cuatro preguntas para sí. Lo que quede en "no", se lleva a la tarea. -->
