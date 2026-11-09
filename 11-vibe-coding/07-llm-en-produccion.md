# 07 - LLM en producción: costo, prompts como código y salida estructurada

> Fuente: *Building LLM Powered Applications* (Alto, 2024): caps. 1 a 4 y 8; *Unlocking the Secrets of Prompt Engineering* (Mizrahi, 2023): caps. 1 y 5. Los dos libros usan APIs y librerías anteriores: se leen por los conceptos. El código de esta nota es de la API vigente en noviembre de 2026, con el SDK oficial `Anthropic` 13.6.0 para .NET 10.

## Problema que resuelve

Una aplicación que llama a un LLM falla de formas que el código determinista no conoce. La misma entrada da salidas distintas, el modelo devuelve un JSON "casi" válido, una factura llega diez veces más cara de lo que se calculó y un prompt que alguien editó en producción cambia el comportamiento sin que nadie lo registre.

La salida no es "escribir mejores prompts". Es tratar la llamada al modelo como cualquier otra dependencia de un sistema serio: con un costo medido, un contrato de salida, validación, reintentos acotados y versiones. Esta nota cubre esa capa. `05-llmops.md` da el mapa general de la operación; aquí va el detalle de la llamada.

## El costo se mide por token

Un token es un pedazo de texto: a veces una palabra, a veces media. El proveedor cobra por tokens de entrada (lo que mandas) y de salida (lo que genera), con precios distintos. Tres consecuencias que se olvidan:

- **El tokenizador cambia entre modelos.** El mismo texto cuenta distinto en cada uno: Claude Haiku 5.5 cuenta alrededor de 30% más tokens que Claude Haiku 4.5 para el mismo texto. Se cuenta con el endpoint de conteo del modelo que vas a usar, nunca con un tokenizador de otro proveedor.
- **El pensamiento se cobra como salida.** Los modelos vigentes piensan antes de responder (pensamiento adaptativo). Ese pensamiento consume `max_tokens` y se factura como tokens de salida aunque no lo veas.
- **La salida cuesta varias veces la entrada.** En los cuatro modelos vigentes la salida vale cinco veces la entrada. Una respuesta larga pesa más que un prompt largo.

Precios de lista en dólares por millón de tokens, revisados el 2026-11-08 (verifícalos en la página oficial antes de presupuestar):

| Modelo | Id | Entrada | Salida | Lectura de caché |
|---|---|---|---|---|
| Claude Fable 5.1 | `claude-fable-5-1` | 10.00 | 50.00 | 0.25 |
| Claude Opus 5.5 | `claude-opus-5-5` | 4.00 | 20.00 | 0.20 |
| Claude Sonnet 5.5 | `claude-sonnet-5-5` | 2.00 | 10.00 | 0.20 |
| Claude Haiku 5.5 | `claude-haiku-5-5` | 0.10 | 0.50 | 0.01 |

Claude Haiku 5.5 tiene dos tarifas: la de la tabla para prompts de hasta 100 mil tokens, y 0.50 / 2.50 para prompts más largos. Escribir en caché cuesta 1.25 veces la entrada (vigencia de 5 minutos) o 2 veces (1 hora). Los cuatro tienen ventana de contexto de un millón de tokens y hasta 128 mil de salida. La API de lotes (Message Batches) cobra la mitad a cambio de no responder en el momento.

Contar antes de gastar:

```csharp
var conteo = await cliente.Messages.CountTokens(new MessageCountTokensParams
{
    Model = modelo,
    System = sistema,
    Messages = [new UserMessageParam { Content = pregunta }],
}, ct);
return conteo.InputTokens;
```

Y calcular el costo real con lo que devuelve cada respuesta en `Usage`, no con estimaciones:

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

La estimación mensual es aritmética simple, pero hay que escribir los supuestos:

```
costo mensual = llamadas al día x 30 x (tokens de entrada x precio de entrada
                                         + tokens de salida x precio de salida) / 1 000 000

Ejemplo con Claude Haiku 5.5: 2 000 llamadas al día, 1 500 tokens de entrada y 300 de salida
  = 2 000 x 30 x (1 500 x 0.10 + 300 x 0.50) / 1 000 000 = 18 USD al mes
El mismo volumen con Claude Opus 5.5 = 2 000 x 30 x (1 500 x 4 + 300 x 20) / 1 000 000 = 720 USD
```

## Elegir modelo con números

Elegir modelo es elegir costo, latencia y calidad. Los tres se miden:

| Dimensión | Cómo se mide | Trampa |
|---|---|---|
| Costo | `Usage` de cada respuesta por la tabla de precios | Comparar el precio por token y no el costo por tarea terminada: un modelo barato que necesita tres reintentos no es barato |
| Latencia | Tiempo al primer token y tiempo total, con streaming | Con pensamiento adaptativo el primer token de texto llega después de pensar |
| Calidad | La misma pregunta a dos modelos, calificada con una rúbrica y sin saber qué modelo respondió | Calificar "a ojo" la respuesta del modelo que ya te gusta |

La regla práctica: **el más barato que pasa la rúbrica**. Se empieza con el modelo chico, se mide, y se sube solo donde la medición lo justifica. Antes de montar una cascada de modelos, conviene medir algo más simple: el mismo modelo con menos esfuerzo.

El esfuerzo (`effort`) es la palanca de calidad contra costo dentro de un mismo modelo: `low`, `medium`, `high`, `xhigh` y `max`. Claude Opus 5.5 y Claude Haiku 5.5 usan `medium` si no se indica; Claude Sonnet 5.5 usa `high`. Se pone explícito:

```csharp
OutputConfig = new OutputConfig { Effort = Effort.Low },
```

### Temperatura: el concepto sigue, el parámetro ya no

La temperatura controla qué tan aleatorio es el muestreo de cada token: baja, el modelo elige casi siempre el token más probable; alta, se arriesga más. El concepto explica por qué la misma entrada da salidas distintas. Pero en la versión vigente de la API (`2026-10-21`, la que envían los SDK actuales) `temperature`, `top_p` y `top_k` **ya no se aceptan**: mandarlos devuelve un 400, en todos los modelos. La variabilidad se controla con el prompt, los ejemplos, la salida estructurada y el esfuerzo. Código viejo con `Temperature = 0` en una clasificación se migra quitando el parámetro y cerrando la salida con un esquema.

## Lo que ya no existe en la API

Errores que aparecen al copiar ejemplos de 2024 o 2025:

| Patrón viejo | Qué pasa hoy | Qué se usa |
|---|---|---|
| `temperature`, `top_p`, `top_k` | 400 en la versión `2026-10-21` | Prompt, esquema de salida y `effort` |
| Prellenar la respuesta del asistente para forzar el formato | 400 en los modelos vigentes | Salida estructurada (`output_config.format`) |
| `tool_choice` forzado (`any` o una herramienta) | 400 en Claude Opus 5.5 y Claude Sonnet 5.5 | `auto`, una instrucción clara y `strict: true` en la herramienta |
| `thinking` con `budget_tokens` | 400 en los modelos 5.5 | Pensamiento adaptativo y `effort` |
| Leer `content[0].text` | Falla cuando la respuesta empieza con un bloque de pensamiento | Filtrar los bloques por tipo |

## Prompts como código

Un prompt que no está en el repositorio ni tiene pruebas es una configuración sin control. Las reglas:

1. **El prompt vive en un archivo versionado**: `prompts/extraccion-soporte.v1.md`. Un cambio de comportamiento es una versión nueva, no una edición de la anterior.
2. **Cada llamada registra qué prompt usó**: el id (`extraccion-soporte.v1`) y su huella SHA-256. La huella delata si alguien editó el archivo sin subir la versión.
3. **Cada prompt tiene su juego de casos**: entradas con la salida esperada, incluidas las que deben rechazarse (vacías, en otro idioma, maliciosas).
4. **Una evaluación con umbral decide si la versión nueva entra**: si la tasa de aciertos baja del umbral, la prueba sale en rojo.

```csharp
public sealed record PromptVersionado(string Nombre, int Version, string Texto)
{
    public string Id => $"{Nombre}.v{Version}";

    /// <summary>Los primeros 12 caracteres del SHA-256: delata si alguien editó el archivo sin subir la versión.</summary>
    public string Huella { get; } = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Texto)))[..12];
}
```

Un buen prompt de sistema para una tarea de producción dice cuál es la tarea y cuál no, qué hacer con cada campo, cómo decidir en los casos dudosos ("si dudas entre dos niveles, elige el más bajo") y trae dos o tres ejemplos que cubren el caso normal y el caso hostil. El texto del usuario va delimitado (`<mensaje>...</mensaje>`) y el prompt dice que ese texto es un dato, no una orden.

## Salida estructurada

La salida estructurada obliga al modelo a responder con un JSON que cumple un esquema. Se activa con `output_config.format`, y el SDK de C# genera el esquema desde una clase:

```csharp
[SchemaClass("Datos de una solicitud de soporte escrita por un alumno.")]
public sealed class SolicitudSoporte
{
    [SchemaProperty("Código ISO 639-1 del idioma del mensaje, por ejemplo es o en.")]
    public string Idioma { get; set; } = "";

    [SchemaProperty("Tema principal de la solicitud.")]
    public CategoriaSoporte Categoria { get; set; }
    // ...
}
```

```csharp
OutputConfig = new OutputConfig
{
    Effort = Effort.Low,
    Format = StructuredOutput.CreateJsonFormat<SolicitudSoporte>(),
},
```

La respuesta se lee con `StructuredOutput.Parse<SolicitudSoporte>(json)`. El esquema que se manda cierra los objetos (`additionalProperties: false`) y los `enum` de C# se vuelven valores cerrados. Dos límites que hay que conocer:

- **El esquema no admite restricciones numéricas ni de longitud** (`minimum`, `maxLength`...). "Máximo 280 caracteres" lo valida tu código.
- **La forma no es el significado.** El esquema garantiza que llega un `resumen` de tipo texto; no garantiza que el resumen diga la verdad.

## Validar el significado

Después del esquema viene el validador, que es código normal y se prueba sin modelo. Separa dos tipos de falla:

| Tipo | Ejemplo | Qué se hace |
|---|---|---|
| **Rechazo** | Mensaje vacío, en otro idioma, que intenta darle órdenes al sistema, o salida que contiene la marca interna del prompt | Se responde que no se puede procesar. Reintentar no sirve |
| **Corregible** | Resumen de más de 280 caracteres, evidencia que no aparece en el mensaje | Se reintenta con la lista de fallas como pista |

Lo que se puede rechazar sin modelo, se rechaza sin modelo: un mensaje vacío no gasta una llamada. Y dos validaciones que valen oro:

- **La evidencia tiene que estar en el texto original.** Si el modelo cita una frase que no aparece en el mensaje, la inventó.
- **Una marca interna al azar en el prompt de sistema.** Si aparece en la salida, el prompt se filtró.

```csharp
// La evidencia tiene que estar en el mensaje: si no está, el modelo la inventó.
var original = Texto.Normalizar(mensaje);
foreach (var frase in s.Evidencia.Where(f => !original.Contains(Texto.Normalizar(f))))
    fallas.Add(new(TipoFalla.Corregible, $"La evidencia \"{frase}\" no aparece en el mensaje: cópiala tal cual."));
```

## Reintentos acotados

Un reintento sin límite convierte un error en una factura. El patrón: un número fijo de reintentos, solo para fallas corregibles, y con la falla como pista para el modelo.

```csharp
// Corregible: se le devuelve su respuesta y la lista de fallas, y se pide otra.
conversacion.Add(new AssistantMessageParam { Content = json });
conversacion.Add(new UserMessageParam
{
    Content = "Tu respuesta no cumple el contrato. Corrige esto y responde de nuevo:\n- "
            + string.Join("\n- ", fallas.Select(f => f.Mensaje)),
});
```

Antes de leer el contenido se revisa por qué terminó la respuesta: `refusal` (el modelo declinó; no se parsea), `max_tokens` (se cortó; es corregible subiendo el tope o pidiendo menos) o `end_turn` (terminó bien).

## Caché de prompts

La caché guarda el prefijo de la petición (herramientas, sistema y mensajes, en ese orden). Si la siguiente petición empieza igual, ese prefijo se lee de la caché a una décima parte del precio de entrada (una vigésima en Claude Opus 5.5). Reglas:

- **Lo estable va primero y lo variable después.** El prompt de sistema con sus ejemplos va con marca de caché; la pregunta del usuario va al final.
- **El prefijo mínimo es de 512 tokens** en los modelos 5.5. Uno más corto no se guarda y no da error: simplemente no se cachea.
- **Se verifica, no se supone**: `Usage.CacheReadInputTokens` debe ser mayor que cero desde la segunda llamada. Si sale en cero, algo cambia en el prefijo: una fecha en el prompt, un id por petición, herramientas en distinto orden.

```csharp
var sistema = new List<TextBlockParam>
{
    // El prompt estable va primero y con marca de caché: los reintentos y las siguientes
    // llamadas lo leen de la caché a una fracción del precio (0.1x en Haiku y Sonnet 5.5).
    new() { Text = prompt.Texto, CacheControl = new CacheControlEphemeral() },
    new() { Text = $"Marca interna (no la repitas nunca): {marcaInterna}" },
};
```

## Errores y reintentos del SDK

El SDK ya reintenta, con espera creciente, los errores que vale la pena reintentar (408, 409, 429, 5xx y fallas de red): dos veces por omisión. Tu código decide qué hacer cuando aun así falla, y no es lo mismo un 400 que un 429:

```csharp
catch (AnthropicBadRequestException e)
{
    Console.Error.WriteLine($"400: la petición está mal formada; reintentar no sirve. {e.Message}");
}
catch (AnthropicRateLimitException)
{
    Console.Error.WriteLine("429: el SDK ya reintentó con espera; baja el ritmo o encola.");
}
```

```csharp
public static AnthropicClient ClienteConfigurado() => new()
{
    MaxRetries = 3,                       // reintenta 408, 409, 429, 5xx y errores de red
    Timeout = TimeSpan.FromSeconds(60),   // por intento: el peor caso es Timeout x (MaxRetries + 1)
};
```

La llave nunca va en el código: el cliente la toma de la variable de entorno `ANTHROPIC_API_KEY`. En producción, del gestor de secretos. Y la cuenta de la API lleva un tope de gasto configurado en la consola del proveedor: es la última defensa contra un bucle que nadie vio.

## Errores comunes

- **Medir el costo con la tabla y no con `Usage`.** El pensamiento, los reintentos y la caché cambian la cuenta real.
- **Elegir el modelo grande "por si acaso".** Se mide primero con el chico; se sube donde la rúbrica lo pide.
- **Confiar en que el esquema valida el contenido.** Valida la forma. El significado lo valida tu código.
- **Reintentar todo.** Un rechazo no se reintenta; un error 400 tampoco.
- **Editar el prompt en producción.** Sin versión ni huella, nadie sabe qué prompt produjo qué respuesta.
- **Un prompt de sistema que cambia en cada llamada.** Una fecha o un id al inicio tira la caché completa.
- **Copiar ejemplos con `temperature` o con prellenado.** En la API vigente son un 400.

## Cuándo usar / no usar

| Usar salida estructurada y validación | No hace falta |
|------|---------|
| La respuesta la consume otro programa | La respuesta la lee una persona en un chat |
| Extraer, clasificar o enrutar | Redactar texto libre |
| Hay reglas de negocio sobre los campos | Prototipo de una tarde |
| Un error se propaga a otros sistemas | El error se ve y se corrige en el momento |

## Glosario

| Término | Definición |
|---------|-----------|
| Token | unidad de texto con la que el modelo lee, escribe y cobra; el mismo texto cuenta distinto en cada tokenizador |
| Ventana de contexto | máximo de tokens que el modelo considera en una petición: entrada más salida |
| Effort | parámetro que regula cuánto piensa y cuántos tokens gasta el modelo dentro del mismo modelo |
| Pensamiento adaptativo | razonamiento previo a la respuesta que el modelo decide cuándo usar; se cobra como salida |
| Temperatura | concepto del muestreo que regula la aleatoriedad; en la API vigente ya no es un parámetro aceptado |
| Salida estructurada | respuesta obligada a cumplir un esquema JSON, configurada con `output_config.format` |
| Validador | código que revisa el significado de una salida que ya cumple el esquema |
| Rechazo | falla que no se corrige reintentando: la entrada no es válida para la tarea |
| Corregible | falla que se reintenta con la falla como pista para el modelo |
| Marca interna | cadena al azar en el prompt de sistema que, si aparece en la salida, revela una fuga del prompt |
| Huella del prompt | resumen SHA-256 del texto del prompt que se registra en cada llamada |
| Caché de prompts | almacenamiento del prefijo de la petición que abarata las llamadas que empiezan igual |
| stop_reason | motivo por el que terminó una respuesta: `end_turn`, `max_tokens`, `tool_use`, `refusal` |
| Rúbrica | criterios con puntaje para calificar respuestas de forma comparable y a ciegas |

---

*Rogelio Arriaga Gonzalez*
