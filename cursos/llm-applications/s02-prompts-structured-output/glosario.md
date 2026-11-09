# Glosario Técnico - S02
## Prompts como código y salida estructurada

> Los términos de la sesión 2, en orden alfabético.

---

## C

### Contrato de salida
**Categoría:** Diseño / Calidad
**Definición:** Acuerdo explícito sobre qué forma y qué significado debe tener la respuesta de un modelo para que el resto del sistema la acepte. La forma se expresa con un esquema JSON; el significado, con reglas que comprueba el código (longitudes, evidencia, idioma). Una respuesta que no cumple el contrato no sale del extractor.
**En el curso:** La clase `SolicitudSoporte` más las reglas de `ValidadorSolicitud`.

### Corregible (falla)
**Categoría:** Diseño / Calidad
**Definición:** Falla de validación que el modelo puede arreglar si se le explica: un resumen demasiado largo, una evidencia que no aparece en el texto, un JSON cortado por `max_tokens`. Se reintenta devolviendo la respuesta y la lista exacta de fallas, dentro de un tope.
**En el curso:** `TipoFalla.Corregible` en `Falla.cs`.

---

## D

### Delimitador
**Categoría:** Prompts / Seguridad
**Definición:** Marca que separa la entrada del usuario de las instrucciones, por ejemplo etiquetas como `<mensaje>` y `</mensaje>`. Ayuda al modelo a tratar ese contenido como dato y no como orden. Es una ayuda, no una garantía: un texto malicioso puede intentar cerrar la etiqueta, y por eso la validación sigue siendo necesaria.
**En el curso:** El extractor envuelve cada mensaje del alumno en `<mensaje>` antes de mandarlo.

---

## E

### Ejemplos (few-shot)
**Categoría:** Prompts
**Definición:** Casos resueltos que se incluyen en el prompt para mostrar el criterio esperado. Funcionan mejor pocos y variados que muchos parecidos, y deben incluir el caso difícil. Con salida estructurada ya no hacen falta para enseñar el formato: sirven para enseñar el criterio.
**En el curso:** El prompt v1 trae tres: un cargo doble, un video que falla y un mensaje que intenta dar órdenes.

### Esquema JSON
**Categoría:** Datos / API
**Definición:** Descripción formal de la forma de un documento JSON: qué campos lleva, de qué tipo, cuáles son obligatorios y qué valores admite. En la salida estructurada de Claude se admiten tipos, `enum`, `required` y `additionalProperties: false`, pero no restricciones de longitud ni de rango numérico.
**En el curso:** Se genera desde la clase C# con `StructuredOutput.CreateJsonFormat<SolicitudSoporte>()`.

### Evaluación con umbral
**Categoría:** Pruebas / Calidad
**Definición:** Prueba que corre un prompt real contra un juego de casos, calcula una tasa de aciertos y falla si queda por debajo de un mínimo acordado. Cuesta dinero porque llama al modelo, por eso se marca aparte y corre en CI y al cambiar el prompt. Su reporte debe decir qué versión y huella de prompt se midió.
**En el curso:** `EvalExtraccion` con umbral de 0.9 y el rasgo `Categoria=Eval`.

### Evidencia
**Categoría:** Calidad / Validación
**Definición:** Frases que el modelo copia tal cual de la entrada para justificar su clasificación. Como deben existir en el texto original, el código puede comprobarlas: si una frase no aparece, el modelo la inventó. Es una de las técnicas más baratas contra la invención.
**En el curso:** El campo `Evidencia` de `SolicitudSoporte` y su comprobación en `ValidadorSolicitud`.

---

## H

### Huella (hash) del prompt
**Categoría:** Versionado
**Definición:** Resumen criptográfico del texto de un prompt, en este caso los primeros 12 caracteres de su SHA-256. Cambia con cualquier edición, por mínima que sea. Registrarla en cada llamada permite saber con qué texto exacto se obtuvo un resultado y detectar ediciones hechas sin subir la versión.
**En el curso:** La propiedad `Huella` de `PromptVersionado`, que imprimen el comando `extraer` y la evaluación.

---

## I

### Invalidador silencioso de caché
**Categoría:** Costos / Rendimiento
**Definición:** Cualquier cosa que cambia el inicio de un prompt entre llamadas y hace que la caché nunca se lea, sin producir ningún error: una fecha u hora en el sistema, un identificador por petición, JSON serializado con orden variable, herramientas que cambian de orden o un cambio de modelo.
**En el curso:** La marca interna del extractor va después del bloque con caché para no ser uno de ellos.

---

## M

### Marca interna
**Categoría:** Seguridad
**Definición:** Cadena aleatoria que se agrega al prompt de sistema con la instrucción de no repetirla nunca. Si aparece en la salida, alguien logró que el modelo filtrara sus instrucciones. Es una defensa determinista: no depende de que el modelo reconozca el ataque.
**En el curso:** `marcaInterna` en `ExtractorSoporte`, generada al azar en cada proceso y buscada por `ValidadorSolicitud`.

### Microsoft.Testing.Platform
**Categoría:** Herramientas / Pruebas
**Definición:** La plataforma de ejecución de pruebas de .NET que reemplaza a VSTest. xunit.v3 la usa, y en el SDK de .NET 10 hay que activarla en `global.json` para que `dotnet test` la corra. Los filtros de pruebas se pasan después de `--`, por ejemplo `--filter-not-trait "Categoria=Eval"`.
**En el curso:** La sección `test` de `global.json` y el comando para correr solo las pruebas sin costo.

---

## O

### output_config.format
**Categoría:** API
**Definición:** Campo de la petición a la API de mensajes donde se declara el esquema JSON que debe cumplir la respuesta (`type: json_schema`). Reemplaza al parámetro anterior `output_format`. Es incompatible con las citas nativas de documentos y con el prefill.
**En el curso:** Lo llena `OutputConfig.Format` en `ExtractorSoporte`.

---

## P

### Prefill
**Categoría:** API / Prompts
**Definición:** Técnica antigua de terminar la conversación con un mensaje de asistente ya empezado (por ejemplo con `{`) para que el modelo lo continúe. En los modelos vigentes de Claude regresa 400. Su reemplazo para obtener un formato es la salida estructurada.
**En el curso:** Se menciona porque aparece en libros y en código heredado.

### Prompt de sistema
**Categoría:** Prompts
**Definición:** Las instrucciones estables que definen el rol, la tarea, las reglas y los ejemplos de una llamada. Las escribe el equipo, no el usuario, y por eso tienen más autoridad que el contenido de los mensajes. Al ser estable, es el candidato natural para la caché de prompts.
**En el curso:** `prompts/extraccion-soporte.v1.md`.

### Prompt versionado
**Categoría:** Versionado
**Definición:** Prompt guardado en el repositorio como archivo con nombre y número de versión (`nombre.vN.md`). Una versión publicada no se edita: un cambio crea la siguiente versión, pasa por revisión y por la evaluación, y la anterior se queda como forma de regresar.
**En el curso:** `RepositorioPrompts` carga `extraccion-soporte.v1.md` y regresa un `PromptVersionado` con id y huella.

### Prueba unitaria sin modelo
**Categoría:** Pruebas
**Definición:** Prueba que verifica la parte determinista del sistema (validadores, esquemas, conversión de datos) sin llamar al modelo: no cuesta, no necesita red y siempre da el mismo resultado. Es donde se prueban las reglas del contrato de salida.
**En el curso:** `ValidadorSolicitudPruebas` y `ContratoSolicitudPruebas`.

### Punto de caché (cache_control)
**Categoría:** API / Costos
**Definición:** Marca que se pone en un bloque del prompt para indicar hasta dónde se guarda el prefijo en caché. Todo lo anterior a la marca debe ser idéntico entre llamadas para que se lea de caché. En los modelos 5.5, el prefijo debe tener al menos 512 tokens o la marca se ignora en silencio. Se admiten hasta cuatro por petición.
**En el curso:** `CacheControl = new CacheControlEphemeral()` en el bloque del prompt de sistema.

---

## R

### Rechazo (falla)
**Categoría:** Diseño / Calidad
**Definición:** Falla de validación que no se arregla reintentando, porque el problema está en la entrada o es una regla del negocio: mensaje vacío, en otro idioma, intento de dar órdenes al sistema, fuga de la marca interna. Reintentar un rechazo solo cobra otra vez por la misma respuesta inválida.
**En el curso:** `TipoFalla.Rechazo` en `Falla.cs`.

### Reintento acotado
**Categoría:** Diseño / Costos
**Definición:** Repetición de una llamada con un número máximo de intentos y con la falla como pista para el modelo. El tope evita que un caso imposible cobre para siempre, y el número de intentos usados es una métrica de la calidad del prompt. No se confunde con los reintentos de transporte que hace el SDK ante errores 429 o 5xx.
**En el curso:** `MaxReintentos = 2` en `ExtractorSoporte`.

---

## S

### Salida estructurada
**Categoría:** API
**Definición:** Función de la API que restringe la generación del modelo a un esquema JSON, de modo que la respuesta siempre se puede leer. Garantiza la forma, no el significado. Si la respuesta termina por `refusal` o por `max_tokens`, el esquema ya no está garantizado.
**En el curso:** Es la base del extractor de solicitudes de soporte.

### StructuredOutput (ayudante del SDK)
**Categoría:** Herramientas
**Definición:** Clase del SDK de C# de Anthropic que genera un esquema JSON a partir de una clase (`CreateJsonFormat<T>`, `ToJsonSchema<T>`) y convierte la respuesta de vuelta en esa clase (`Parse<T>`). Los atributos `SchemaClass` y `SchemaProperty` agregan descripciones que el modelo lee.
**En el curso:** Se usa en `ExtractorSoporte` y en `ContratoSolicitudPruebas`.

---

## T

### tool_choice forzado
**Categoría:** API
**Definición:** Opción de la API para obligar al modelo a usar una herramienta concreta (`tool`) o alguna herramienta (`any`). En Claude Opus 5.5, Sonnet 5.5 y Fable 5.1 regresa 400. Para obtener JSON se usa la salida estructurada; para herramientas reales, `tool_choice` automático con `strict: true` y la instrucción en el prompt.
**En el curso:** Se menciona como patrón de libro que ya no aplica; las herramientas llegan en la sesión 4.

---

## V

### Validación de significado
**Categoría:** Calidad / Validación
**Definición:** Comprobaciones en código de lo que el esquema no puede garantizar: longitudes, que la evidencia exista en el texto, que el idioma sea el aceptado, que no se haya filtrado el prompt. Se aplica antes de la llamada (entrada) y después (salida), y clasifica cada falla como rechazo o corregible.
**En el curso:** `ValidadorSolicitud.ValidarEntrada` y `ValidadorSolicitud.ValidarSalida`.
