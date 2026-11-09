<!-- _class: lead -->

# S03 - RAG: que responda con sus documentos

### Aplicaciones con LLM: de la API al agente

**Rogelio Arriaga - Raptor Dev Services**

<!-- Instructor: abre con la pregunta "¿cuántos de ustedes ya le preguntaron algo a un chatbot de empresa y les inventó una política?". Casi todos levantan la mano. Esa es la sesión. -->

---

> Un asistente que no sabe decir "no lo sé" inventa con la misma seguridad con que acierta.

<!-- Instructor: léela en voz alta y déjala proyectada unos segundos. Al final de la sesión volvemos a ella con la evaluación: "no lo sé" cuenta como acierto cuando toca. -->

---

## Agenda del día

1. **El problema:** lo que el modelo no sabe y por qué no basta con pegarle todo
2. **Ingestión y fragmentos:** cómo se parte un documento para que se pueda encontrar
3. **Embeddings:** qué son, y la decisión de con qué los generamos
4. **El índice en PostgreSQL:** pgvector, la tabla, el índice HNSW y la consulta
5. **Recuperar, responder y citar:** el contrato y las dos capas del "no lo sé"
6. **Medirlo:** verlo fallar y la tasa de aciertos

<!-- Instructor: la práctica se hace en bloques entre partes. Parte 1 y 2 antes del primer descanso, 3 y 4 antes de comer, 5 y 6 en la tarde con la evaluación. -->

---

## Dónde va el proyecto

| Sesión | Lo que agregamos a `AsistenteCursos` |
|---|---|
| S01 | Medir tokens, costo y latencia de cada llamada |
| S02 | Prompts versionados y salida estructurada con validación |
| **S03** | **Responder con los documentos del curso, citando la fuente** |
| S04 | Un agente con herramientas y confirmación humana |
| S05 | Un servidor MCP con el catálogo |
| S06 | Defensas, evaluación y operación |

<!-- Instructor: el proyecto es uno solo. Hoy reusamos el versionado de prompts y la salida estructurada de la S02: si alguien no los tiene en verde, que lo arregle en el primer bloque de práctica. -->

---

## Al terminar puedes

- Montar una búsqueda con LLM sobre datos propios, en PostgreSQL.
- Explicar con números por qué elegiste un modelo de embeddings.
- Hacer que el asistente cite la fuente y que esa cita sea verificable.
- Medir si responde bien, incluido decir "no lo sé" cuando toca.

<!-- Instructor: la última viñeta es la que más cuesta. Mucha gente arma RAG y nunca mide; la tarea los obliga. -->

---

<!-- _class: lead -->

# Parte 1
## El problema

---

## Lo que el modelo no sabe

- **Lo que pasó después de su corte de entrenamiento.** Tu política de reembolsos de este mes.
- **Lo privado.** Tus documentos internos nunca estuvieron en sus datos.
- **La versión de tu curso.** Sabe qué es un índice GIN; no sabe qué dice *tu* lámina 14.

Y cuando no sabe, no se detiene: completa con lo más probable. Suena igual de seguro.

<!-- Instructor: el tercer punto es el sutil. El modelo sí sabe de PostgreSQL en general, y por eso es peligroso: contesta bien en general y mal para tu caso. -->

---

## Tres formas de darle tus datos

| Opción | Qué es | Cuándo |
|---|---|---|
| **Todo en el prompt** | Pegar los documentos completos en cada llamada | Pocos documentos y pocas preguntas |
| **Ajuste fino** | Reentrenar el modelo con tus ejemplos | Cambiar estilo o formato, no agregar hechos |
| **RAG** | Buscar lo relevante y dárselo en cada pregunta | Muchos documentos que cambian |

RAG (*Retrieval-Augmented Generation*): generación aumentada con recuperación.

<!-- Instructor: el ajuste fino no es para meter hechos: se olvidan, no se pueden citar y cada cambio obliga a reentrenar. RAG cambia el documento y listo. -->

---

## ¿Y si cabe todo en la ventana de contexto?

Los modelos del curso aceptan un millón de tokens. Pegarlo todo funciona, pero:

- **Se paga en cada pregunta.** En Haiku 5.5, un prompt de más de 100 mil tokens paga la tarifa larga: 0.50 USD por millón en lugar de 0.10.
- **Tarda más.** Más entrada, más tiempo al primer token.
- **Se diluye.** Entre 300 documentos, el párrafo correcto compite con todo lo demás.
- **No escala.** El día que el corpus pase de la ventana, hay que rehacerlo.

La caché de prompts abarata lo repetido, pero no arregla la dilución ni el tope.

<!-- Instructor: no satanices el "todo en el prompt": para diez documentos que no cambian, con caché, es lo más simple y está bien. RAG es para cuando eso deja de servir. -->

---

## La tubería de RAG

**Al ingestar** (una vez, y cada que cambia un documento):

1. Leer el documento y partirlo en fragmentos.
2. Convertir cada fragmento en un vector (embedding).
3. Guardar fragmento y vector en el índice.

**Al preguntar** (en cada pregunta):

1. Convertir la pregunta en un vector, con el mismo modelo.
2. Recuperar los fragmentos más cercanos.
3. Dárselos al LLM con la pregunta, y validar lo que responde.

<!-- Instructor: dibuja las dos mitades en el pizarrón. La de arriba es un trabajo por lotes; la de abajo, una petición en línea. Fallan distinto y se miden distinto. -->

---

<!-- _class: lead -->

# Parte 2
## Ingestión y fragmentos

---

## Ingestar es un proceso, no un script

- **Idempotente:** correrlo dos veces deja lo mismo. Cada documento reemplaza sus propios fragmentos.
- **Atómico por documento:** borrar lo viejo e insertar lo nuevo en una sola transacción.
- **Trazable:** cada fragmento sabe de qué archivo y de qué sección salió.
- **Revisable:** lo que entra al índice es lo que el modelo va a leer como verdad.

<!-- Instructor: el cuarto punto es el puente a la S06. Lo que se indexa es una entrada al modelo, igual que lo que escribe el usuario. Ahí vamos a meter un documento envenenado. -->

---

## Por qué fragmentar

Un documento entero como un solo vector mezcla diez temas en un punto promedio que no se parece a ninguna pregunta.

| Fragmento | Ventaja | Costo |
|---|---|---|
| **Chico** (200 caracteres) | Muy preciso al buscar | Pierde contexto: "la tabla de abajo" sin la tabla |
| **Mediano** (1,000 a 2,000) | Una idea completa | Algo de ruido |
| **Grande** (una página o más) | Todo el contexto | Búsqueda difusa y más tokens por pregunta |

El tamaño no se adivina: se calibra con la tasa de aciertos.

<!-- Instructor: el curso arranca con 1,500 caracteres y 200 de solape. En la tarea van a mover ese número y medir. -->

---

## Partir por sección, no cada N caracteres

```text
## Gestión de costos          (fragmento 1)
...texto de costos...

## Cuándo usar / no usar      (fragmento 2)
...tabla...
```

En Markdown, el autor ya partió el documento por ideas: los encabezados. Se respeta esa estructura y solo las secciones largas se parten por párrafos.

<!-- Instructor: en PDF o HTML la estructura existe pero hay que sacarla. Por eso el curso usa Markdown: la ingestión de PDF es otro problema y no es el de hoy. -->

---

## El título viaja con el fragmento

```csharp
// Cada fragmento lleva el título de su sección: sin él, "la tabla de abajo" no significa nada.
private static FragmentoNuevo Nuevo(string fuente, string seccion, string texto) =>
    new(fuente, seccion, $"{seccion}\n\n{texto.Trim()}");
```

Una línea que sube la tasa de aciertos más que cualquier ajuste fino del umbral.

<!-- Instructor: pregunta al grupo por qué ayuda. Respuesta: la pregunta "¿cuánto cuesta?" se parece al título "Gestión de costos" aunque el párrafo hable de tokens y nunca diga "costo". -->

---

## El solape

```csharp
// Los últimos caracteres del fragmento anterior, cortados en un espacio para no partir palabras.
private static string Cola(string texto, int solape)
```

Cuando una sección larga se parte, el fragmento siguiente arranca con la cola del anterior. Una idea que quedó partida a la mitad aparece completa en uno de los dos.

<!-- Instructor: el solape cuesta: duplica un poco de texto y de tokens. 200 caracteres sobre 1,500 es un 13 % más, aceptable. -->

---

## Un `#` dentro de código no es un encabezado

```bash
# esto es un comentario de bash
echo hola
```

Si el fragmentador no sabe que está dentro de un bloque de código, esa línea abre una "sección" nueva a la mitad del ejemplo.

```csharp
if (linea.TrimStart().StartsWith("```")) enCodigo = !enCodigo;
bool esEncabezado = !enCodigo && linea.StartsWith('#');
```

<!-- Instructor: este error lo encontré en un proyecto real; los ejemplos de bash quedaban partidos. Por eso hay una prueba con ese caso exacto. -->

---

## La prueba que lo protege

```csharp
[Fact]
public void Un_encabezado_dentro_de_un_bloque_de_codigo_no_parte_la_seccion()
```

Y otra que comprueba que al partir una sección larga **no se pierde ningún párrafo**: cada uno aparece en algún fragmento.

<!-- Instructor: perder texto al fragmentar es silencioso. El asistente simplemente "no sabe" algo que sí estaba en el documento. Sin esta prueba, nadie se entera. -->

---

<!-- _class: lead -->

# Parte 3
## Embeddings

---

## Qué es un embedding

Un vector de números (aquí, 1,024) que representa el **significado** de un texto.

- Textos que dicen lo mismo quedan cerca, aunque no compartan palabras.
- "¿Cuánto me cuesta cada llamada?" queda cerca de "El costo se mide por token".
- Se compara con la **distancia coseno**.

El modelo de embeddings no genera texto: solo convierte texto en vectores.

<!-- Instructor: aclara que es otro modelo distinto al LLM. El LLM de la práctica es claude-haiku-5-5; los embeddings los hace bge-m3. -->

---

## Distancia coseno

| Distancia | Significa |
|---|---|
| 0 | Mismo sentido |
| alrededor de 1 | Sin relación |
| 2 | Sentido opuesto |

En pgvector es el operador `<=>`. Más chica, más parecido.

El umbral del curso arranca en **0.55**: más lejos que eso, el fragmento no cuenta.

<!-- Instructor: el 0.55 es un punto de partida, no una verdad. Depende del modelo de embeddings y del corpus. En la práctica anotan las distancias reales de preguntas buenas y malas para calibrarlo. -->

---

## Anthropic no ofrece embeddings

La API de Claude genera texto; no tiene un modelo de embeddings. Hay que elegir otro proveedor.

Tres criterios para elegir:

1. **Idioma:** los documentos del curso están en español.
2. **Dónde viven los datos:** cada fragmento se manda a quien genera el vector.
3. **Costo y operación:** una segunda llave, una segunda factura, otro servicio que puede caerse.

<!-- Instructor: la documentación de Anthropic recomienda un proveedor de embeddings (Voyage AI). Es una buena opción; no la elegimos por las razones de la siguiente lámina, no porque sea mala. -->

---

## Las opciones

| Opción | A favor | En contra |
|---|---|---|
| **Modelo local con Ollama** (bge-m3) | Gratis, sin segunda llave, los datos no salen de la máquina, corre igual en CI | Usa RAM y CPU propios; calidad algo menor que los mejores hospedados |
| **Proveedor hospedado** (por ejemplo Voyage AI) | Calidad alta, sin infraestructura | Segunda llave y factura; los fragmentos salen a un tercero |
| **Hash léxico** (`EmbeddingsPorHash`) | Sin modelo ni red, determinista | No entiende sinónimos: solo sirve para probar la tubería |

<!-- Instructor: en producción con datos sensibles, el local gana por privacidad. En producción con volumen y sin restricción de datos, el hospedado suele ganar por calidad. En un curso con 30 alumnos, el local gana por costo y reproducibilidad. -->

---

## La decisión del curso: bge-m3 con Ollama

- **Multilingüe:** entrenado para muchos idiomas, español incluido.
- **1,024 dimensiones** y hasta **8,192 tokens** de entrada por texto.
- **1.2 GB** de descarga, una sola vez.
- **Local:** `ollama/ollama:0.40.15` en un contenedor, en `127.0.0.1`.
- **Reproducible:** el mismo contenedor corre en CI para la evaluación de la S06.

<!-- Instructor: la decisión queda en el resumen de la clase EmbeddingsOllama y aquí. Si alguien la cambia en su proyecto, que escriba por qué; es un ADR chico. -->

---

## Lo que cuesta esa decisión

- **Memoria:** el modelo cargado ocupa del orden de 2 GB de RAM.
- **Velocidad:** en CPU, ingestar cientos de fragmentos tarda minutos, no segundos.
- **Calidad:** buena para el curso; un modelo hospedado de primera línea suele recuperar mejor.
- **Mantenimiento:** la versión de la imagen se fija y se actualiza a propósito.

Toda decisión técnica tiene costo. Escribirlo es lo que la hace una decisión.

<!-- Instructor: si una laptop del grupo no aguanta, que use EmbeddingsPorHash para probar la tubería y corra la evaluación en CI (S06). El puerto de Ollama se queda en 127.0.0.1: no se expone a la red. -->

---

## La interfaz propia

```csharp
public interface IGeneradorEmbeddings
{
    string Modelo { get; }
    int Dimensiones { get; }
    Task<IReadOnlyList<float[]>> GenerarAsync(IReadOnlyList<string> textos, TipoTexto tipo, CancellationToken ct = default);
}
```

El asistente, el almacén y la ingestión dependen de esto, no de Ollama. Cambiar de proveedor es escribir otra clase.

<!-- Instructor: TipoTexto existe porque varios proveedores tratan distinto un documento y una consulta (un prefijo o un campo). bge-m3 no lo necesita, pero la interfaz ya lo soporta. -->

---

## La implementación con Ollama

```csharp
using var respuesta = await http.PostAsJsonAsync("/api/embed", new { model = modelo, input = textos }, ct);
respuesta.EnsureSuccessStatusCode();
```

```csharp
if (cuerpo.Embeddings.Any(v => v.Length != dimensiones))
    throw new InvalidOperationException($"El modelo {modelo} no devuelve {dimensiones} dimensiones.");
```

Un solo `POST` con varios textos. Y una validación que parece exagerada hasta el día que alguien cambia el modelo.

<!-- Instructor: la ingestión manda lotes de 16. Mandar uno por uno funciona y es varias veces más lento. -->

---

## Cambiar de modelo obliga a reindexar

Vectores de modelos distintos **no se comparan**: viven en espacios distintos aunque tengan las mismas dimensiones.

- Pregunta con el modelo A contra fragmentos del modelo B: distancias sin sentido, sin ningún error.
- Por eso la ingestión imprime con qué modelo indexó.
- Cambiar de modelo es: borrar, reindexar todo, volver a calibrar el umbral, volver a medir.

<!-- Instructor: este es el error silencioso más caro de RAG. Nada truena; simplemente responde peor. Solo la evaluación lo detecta. -->

---

## `EmbeddingsPorHash`: para probar, no para producción

- Cuenta palabras en 1,024 cubetas por hash. Sin modelo, sin red, determinista.
- "Respaldo diario" se parece a "respaldo diario"; "copia de seguridad" no se le parece nada.
- Sirve para probar fragmentar, guardar y buscar en milisegundos.

```csharp
if (norma == 0) { v[0] = 1f; return v; }   // pgvector no define el coseno de un vector en cero
```

<!-- Instructor: la prueba de integración usa este generador para no depender de Ollama. Así la prueba mide la base y la consulta, no al modelo. -->

---

<!-- _class: lead -->

# Parte 4
## El índice en PostgreSQL

---

## ¿Por qué no una base vectorial aparte?

- **Ya tienes PostgreSQL.** Respaldos, permisos y monitoreo que ya existen.
- **Transacciones.** El documento y sus vectores cambian juntos o no cambian.
- **SQL.** Filtrar por fuente, fecha o permiso en la misma consulta que busca.
- **Un sistema menos** que operar, actualizar y asegurar.

Una base vectorial dedicada tiene sentido con cientos de millones de vectores. Para la mayoría de los productos, pgvector alcanza.

<!-- Instructor: la lectura 05-bases-de-datos/04-postgresql-avanzado.md cubre índices GIN y extensiones; pgvector es una extensión más. -->

---

## Las piezas y sus versiones

| Pieza | Versión fijada |
|---|---|
| Imagen de la base | `pgvector/pgvector:0.8.7-pg17` (PostgreSQL 17) |
| Paquete de .NET | `Pgvector` 0.3.2 |
| Driver | `Npgsql` 10.0.3 |
| Modelo de embeddings | `bge-m3` en `ollama/ollama:0.40.15` |

Todas fijadas en un archivo: `Directory.Packages.props` y el `docker-compose.yml`.

<!-- Instructor: en esta máquina se probó la combinación Pgvector 0.3.2 con Npgsql 10.0.3 contra la imagen 0.8.7-pg17. Si suben una versión, corren la prueba de integración antes de dar nada por hecho. -->

---

## La tabla

```sql
CREATE EXTENSION IF NOT EXISTS vector;
CREATE TABLE IF NOT EXISTS fragmentos (
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    fuente     text        NOT NULL,
    seccion    text        NOT NULL,
    contenido  text        NOT NULL,
    embedding  vector(1024) NOT NULL,
    creado_utc timestamptz NOT NULL DEFAULT now()
);
```

La dimensión es parte del tipo: un vector de otro tamaño ni siquiera entra.

<!-- Instructor: en el código la dimensión sale de la configuración del generador, no está escrita a mano. Es un entero, nunca texto del usuario; por eso es de los pocos lugares donde se arma SQL con interpolación. -->

---

## Registrar el tipo en Npgsql

```csharp
var constructor = new NpgsqlDataSourceBuilder(cadenaDeConexion);
constructor.UseVector();   // registra el tipo vector de pgvector en Npgsql
return constructor.Build();
```

```csharp
await conexion.ReloadTypesAsync(ct);   // la extensión recién creada trae el tipo vector
```

Sin `UseVector`, Npgsql no sabe enviar un `Vector`. Sin `ReloadTypesAsync`, la conexión no conoce el tipo que se acaba de crear.

<!-- Instructor: el segundo error aparece solo la primera vez, en una base nueva. Por eso es tan confuso: en la máquina del que lo escribió ya funciona. -->

---

## El índice HNSW

```sql
CREATE INDEX IF NOT EXISTS fragmentos_embedding
    ON fragmentos USING hnsw (embedding vector_cosine_ops);
```

| Opción | Cómo busca | Cuándo |
|---|---|---|
| Sin índice | Compara contra todos: exacto | Pocos miles de filas |
| **HNSW** | Grafo de vecinos: aproximado y rápido | Lo normal hoy |
| IVFFlat | Agrupa en listas; necesita datos para crearlas | Mucho volumen y poca memoria |

El índice corresponde a un operador: `vector_cosine_ops` va con `<=>`.

<!-- Instructor: aproximado significa que puede perderse un vecino de vez en cuando a cambio de mucha velocidad. Para RAG es un buen trato; para un sistema que exige exactitud, no. -->

---

## La consulta

```sql
SELECT id, fuente, seccion, contenido, embedding <=> @consulta AS distancia
FROM fragmentos
ORDER BY embedding <=> @consulta
LIMIT @k
```

```csharp
cmd.Parameters.AddWithValue("consulta", new Vector(consulta));
cmd.Parameters.AddWithValue("k", k);
```

El vector y el `k` van como parámetros. La regla del SQL parametrizado no tiene excepción para los vectores.

<!-- Instructor: el ORDER BY con el mismo operador del índice es lo que hace que PostgreSQL use HNSW. Si ordenan por otra expresión, recorre la tabla completa. -->

---

## Reemplazar un documento completo

```csharp
await using var tx = await conexion.BeginTransactionAsync(ct);
```

1. `DELETE FROM fragmentos WHERE fuente = @fuente`
2. Un `INSERT` por fragmento, con su vector.
3. `COMMIT`.

Si la ingestión se cae a la mitad, queda la versión vieja completa. Nunca media versión.

<!-- Instructor: pregunta qué pasaría sin transacción y con 1,000 alumnos preguntando mientras se reingesta. Respuesta: durante unos segundos el documento no existe y el asistente dice "no lo sé" a preguntas que sí sabe. -->

---

<!-- _class: lead -->

# Parte 5
## Recuperar, responder y citar

---

## K y el umbral

```csharp
public sealed record OpcionesRag(int K = 5, double DistanciaMaxima = 0.55);
```

- **K:** cuántos fragmentos se traen. Más K, más contexto y más tokens.
- **DistanciaMaxima:** el más lejano que todavía cuenta. Los demás se descartan.

Se traen K y luego se filtran por el umbral: pueden quedar menos de K, o ninguno.

<!-- Instructor: los dos se calibran con los casos, no a ojo. La tarea pide una tabla con la tasa antes y después de moverlos. -->

---

## Primera capa del "no lo sé"

```csharp
// Primera defensa contra inventar: si nada se parece, no se le pregunta al modelo.
if (relevantes.Count == 0)
    return SinRespuesta("Ningún fragmento pasó el umbral de distancia.", recuperadas, 0m);
```

- Cuesta cero: no hay llamada al modelo.
- No depende del modelo: es una comparación de números.
- Es la más barata y la más tonta: no sabe si el fragmento cercano de verdad contesta.

<!-- Instructor: "¿Cuál es la capital de Australia?" cae aquí con el modelo de embeddings real. Con el de hash también. Es la primera falla que van a provocar en la práctica. -->

---

## El contrato de salida

```csharp
[SchemaProperty("true solo si los documentos contienen la respuesta.")]
public bool Sabe { get; set; }
```

```csharp
[SchemaProperty("Las claves de los documentos usados, por ejemplo F12. Vacía si sabe es false.")]
public List<string> Fuentes { get; set; } = [];
```

Salida estructurada, como en la S02: el modelo no puede contestar en otra forma. Y "no sé" es un valor del contrato, no un texto libre que hay que adivinar.

<!-- Instructor: poner Sabe en el contrato es lo que hace medible el "no lo sé". Sin el campo, habría que buscar frases como "no encontré" en el texto, y eso falla. -->

---

## El prompt, versionado

`prompts/asistente-documentos.v1.md`:

1. Responde solo con lo que dicen los documentos. Si no está, `sabe` en `false`.
2. No uses lo que sabes por tu cuenta, aunque estés seguro.
3. En `fuentes`, las claves de los documentos que usaste, y solo esas.
4. En español, máximo 150 palabras.

El prompt vive en el repositorio, con versión y huella, igual que el de extracción.

<!-- Instructor: la regla 2 es la que más rompen los modelos fuertes: saben la respuesta y la dan. Para el alumno, la respuesta "correcta en general" es incorrecta si el curso dice otra cosa. -->

---

## Segunda capa: el modelo dice que no sabe

```csharp
if (!salida.Sabe)
    return SinRespuesta("El modelo indicó que los documentos no lo contienen.", recuperadas, costo);
```

Llegó algo parecido, pero no contesta. El modelo lo lee y lo dice.

Esta capa ya cuesta una llamada, y depende de que el modelo siga la regla. Por eso hay una tercera revisión, en el código.

<!-- Instructor: ejemplo típico: la pregunta es "¿cuánto cuesta el curso?" y llega un fragmento sobre el costo por token. Está cerca en el espacio de vectores y no contesta nada. -->

---

## Las citas se validan contra lo recuperado

```csharp
// Segunda defensa: toda cita tiene que ser un fragmento que de verdad se le entregó.
var citadas = relevantes.Where(f => salida.Fuentes.Contains(f.Clave)).ToList();
if (citadas.Count == 0 || citadas.Count != salida.Fuentes.Distinct().Count())
    return SinRespuesta("La respuesta no citó fuentes o citó claves que no se recuperaron.", recuperadas, costo);
```

Una respuesta sin cita, o con una cita inventada, se trata como "no lo sé". La cita no es decoración: es la prueba.

<!-- Instructor: un modelo puede inventar "F99" con la misma facilidad con que inventa un dato. Si la clave no existe entre lo que se le dio, la respuesta no sale. -->

---

## ¿Y las citas nativas de la API?

La API puede citar por su cuenta: se le mandan documentos con `citations` activado y devuelve el texto citado con su ubicación.

Pero **no se combina con salida estructurada**: la petición devuelve 400.

| Camino | Ganas | Pierdes |
|---|---|---|
| Citas nativas | El texto exacto citado | El contrato JSON con `sabe` |
| **Claves propias (curso)** | Contrato, `sabe` y validación en código | El texto exacto: hay que mostrar el fragmento |

<!-- Instructor: no hay una opción mejor en abstracto. Elegimos el contrato porque el "no lo sé" medible es el objetivo de la sesión. -->

---

## Lo que cuesta una pregunta

Cinco fragmentos de unos 400 tokens, más el prompt y la pregunta: unos 2,500 tokens de entrada y unos 300 de salida (el pensamiento cuenta como salida).

| Modelo | Por pregunta | 1,000 preguntas al día, 30 días |
|---|---|---|
| `claude-haiku-5-5` | unos 0.0004 USD | unos 12 USD |
| `claude-sonnet-5-5` | unos 0.008 USD | unos 240 USD |

Y lo que cae en la primera capa cuesta cero.

<!-- Instructor: haz la cuenta en el pizarrón con la tabla de precios de la S01: Haiku 0.10 y 0.50 por millón, Sonnet 2 y 10. Son estimaciones; la cifra real sale de la medición de cada llamada. -->

---

<!-- _class: lead -->

# Parte 6
## Medirlo

---

> Si no lo viste fallar, no lo probaste.

<!-- Instructor: la regla del curso. Hoy la aplicamos tres veces: una pregunta sin respuesta, quitar el umbral y romper la búsqueda. -->

---

## Verlo fallar, en tres pasos

1. **La pregunta sin respuesta.** "¿Cuál es la capital de Australia?" tiene que salir "no lo sé", con costo cero.
2. **Quitar la primera capa.** `DistanciaMaxima` en 2.0: todo pasa el umbral. ¿El modelo dice `sabe: false` o contesta por su cuenta?
3. **Romper la búsqueda.** `ORDER BY ... DESC`: la prueba de integración tiene que caer.

Si alguna de las tres no falla como esperabas, la defensa que creías tener no existe.

<!-- Instructor: el paso 2 es el más revelador. Con un modelo fuerte, muchas veces dice "no sé" bien; a veces contesta "Canberra". Esa diferencia es lo que mide la evaluación. -->

---

## Los casos

```json
{"id":"rag-01","pregunta":"¿Por qué hay que versionar los prompts?","sabe":true,"fuente":"05-llmops.md"}
{"id":"rag-04","pregunta":"¿Cuál es la capital de Australia?","sabe":false}
```

- Un renglón por caso, en `casos/rag.jsonl`, versionado junto al prompt.
- Los casos sin respuesta pesan igual que los otros.
- La práctica trae cinco. La tarea los sube a veinte.

<!-- Instructor: los casos los escriben personas que conocen el contenido, no el modelo. Si el modelo escribe las preguntas, escribe las que él sabe contestar. -->

---

## Qué cuenta como acierto

| Caso | Acierto si |
|---|---|
| `sabe: true` | Respondió **y** citó un fragmento del archivo esperado |
| `sabe: false` | Dijo "no lo sé", por cualquiera de las capas |

```csharp
bool ok = r.Sabe == caso.Sabe
    && (!caso.Sabe || r.Citadas.Any(f => f.Fuente.EndsWith(caso.Fuente!, StringComparison.Ordinal)));
```

Umbral de la evaluación: 85 %. Por debajo, la prueba truena.

<!-- Instructor: esta métrica no juzga si la redacción es buena; juzga si recuperó lo correcto y si se abstuvo cuando tocaba. Juzgar la redacción con otro modelo es tema de la S06. -->

---

## Qué mover cuando la tasa es baja

| Síntoma | Palanca |
|---|---|
| Dice "no lo sé" a preguntas que sí están | Subir el umbral o K; revisar el fragmento esperado |
| Contesta preguntas que no están | Bajar el umbral; reforzar la regla 2 del prompt |
| Cita el archivo equivocado | Fragmentos más chicos; títulos más claros |
| Acierta pero gasta mucho | Bajar K; fragmentos más chicos |

Un cambio a la vez, y se vuelve a medir. Si mueves tres cosas, no sabes cuál ayudó.

<!-- Instructor: la tarea pide exactamente esto: tabla antes y después, un cambio por fila. -->

---

## Errores comunes

- **Mezclar modelos de embeddings** en la misma tabla: distancias sin sentido, sin errores.
- **Fragmentos sin título:** recuperan peor y nadie sabe por qué.
- **Umbral a ojo:** sin casos, el número es una opinión.
- **Confiar en la cita sin validarla:** la clave también se inventa.
- **No medir el "no lo sé":** un asistente que nunca se abstiene saca 100 % en las preguntas fáciles.
- **Indexar sin revisar:** lo que entra al índice, el modelo lo lee como verdad.

<!-- Instructor: el último es la entrada a la S06. Lo dejamos sembrado. -->

---

## Tarea

**Medir la tasa de aciertos con 20 preguntas y mejorarla.**

1. Sube `casos/rag.jsonl` a 20 casos, al menos 5 sin respuesta en los documentos.
2. Corre la evaluación y anota la tasa y el costo: es tu línea base.
3. Cambia una palanca a la vez (umbral, K, tamaño de fragmento, prompt) y vuelve a medir.
4. Entrega la tabla antes y después, y tu configuración final con su porqué.

Detalle completo en la tarea de la sesión.

<!-- Instructor: insiste en "una palanca a la vez". El que mueve todo junto y reporta "subió a 95 %" no puede decir qué funcionó. -->

---

## Lecturas y libros

**Lecturas (dev-notes):**

- `11-vibe-coding/08-rag.md`
- `05-bases-de-datos/04-postgresql-avanzado.md`

**Libros, por capítulo:**

- *Building Data-Driven Applications with LlamaIndex* (Gheorghiu, 2024), caps. 4 a 7.
- *Building LLM Powered Applications* (Alto, 2024), caps. 5 y 7.

Son de 2024 y en Python: se leen por los conceptos. El código del curso es el de hoy.

<!-- Instructor: LlamaIndex y LangChain resuelven la tubería con abstracciones; aquí la escribimos a mano para ver cada pieza. Cuando la entiendan, las librerías se leen fácil. -->

---

<!-- _class: lead -->

# Próxima sesión
## Herramientas y agentes

El modelo deja de solo contestar: pide acciones. Y una persona confirma antes de cualquier escritura.

<!-- Instructor: pide que lleguen con la práctica de hoy en verde y la base levantada; el agente de la S04 guarda sus tickets en la misma base. -->
