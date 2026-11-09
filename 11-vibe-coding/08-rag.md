# 08 - RAG: respuestas con los documentos propios

> Fuente: *Building Data-Driven Applications with LlamaIndex* (Gheorghiu, 2024): caps. 4 a 7; *Building LLM Powered Applications* (Alto, 2024): caps. 5 y 7. LlamaIndex es Python y los dos libros usan APIs de 2024: se leen por los conceptos. El código de esta nota es C# en .NET 10 con PostgreSQL 17, pgvector 0.8.7, `Npgsql` 10.0.3, `Pgvector` 0.3.2 y el SDK `Anthropic` 13.6.0.

## Problema que resuelve

El modelo no conoce los documentos de {{PROJECT_NAME}}: ni el manual, ni la política de reembolsos de este año, ni el procedimiento que se cambió la semana pasada. Si se le pregunta, responde con lo que aprendió en su entrenamiento, con la misma seguridad con la que acierta. Un asistente que no sabe decir "no lo sé" inventa sin avisar.

RAG (generación aumentada con recuperación) resuelve eso en dos tiempos: primero **recupera** los fragmentos de los documentos propios que se parecen a la pregunta, y luego le pide al modelo que **responda solo con esos fragmentos y cite cuáles usó**. Si no se recuperó nada relevante, la respuesta correcta es "no lo sé".

Complementa `07-llm-en-produccion.md` (salida estructurada y validación, que aquí se reutilizan) y `05-bases-de-datos/04-postgresql-avanzado.md` (la base que guarda el índice).

## La tubería de RAG

```
Ingestión (una vez por documento, y cada vez que cambia)
  documento .md  ->  fragmentos  ->  embedding de cada fragmento  ->  tabla con índice vectorial

Consulta (en cada pregunta)
  pregunta  ->  embedding  ->  los k fragmentos más cercanos  ->  ¿pasan el umbral?
     no: "no lo sé", sin llamar al modelo
     sí: prompt con los fragmentos  ->  respuesta con citas  ->  validar las citas
```

Cada flecha es un punto donde la calidad se gana o se pierde, y cada una se puede medir por separado.

## Fragmentar

El modelo de embeddings y el modelo de lenguaje trabajan mejor con pedazos coherentes que con documentos enteros. Tres decisiones:

| Decisión | Recomendación | Por qué |
|---|---|---|
| Dónde cortar | Por sección del documento (encabezados de Markdown), y una sección larga por párrafos | Un fragmento que mezcla dos temas se parece a medias a las dos preguntas |
| Tamaño | Del orden de 1 000 a 1 500 caracteres, calibrado con las preguntas reales | Muy chico pierde el contexto; muy grande diluye el parecido y gasta tokens |
| Solape | Los últimos 150 a 200 caracteres del fragmento anterior | Una idea que cruza el corte no se pierde |

Y una regla que casi nadie aplica: **cada fragmento lleva el título de su sección**. "La tabla de abajo muestra los plazos" no significa nada sin saber de qué sección viene.

```csharp
// Cada fragmento lleva el título de su sección: sin él, "la tabla de abajo" no significa nada.
private static FragmentoNuevo Nuevo(string fuente, string seccion, string texto) =>
    new(fuente, seccion, $"{seccion}\n\n{texto.Trim()}");
```

Un encabezado dentro de un bloque de código (un comentario `# ...` de bash) no es una sección: el fragmentador tiene que saber que está dentro de un bloque.

## Embeddings

Un embedding es un vector de números que representa el significado de un texto: dos textos que dicen lo mismo con otras palabras quedan cerca. Se generan con un **modelo de embeddings**, distinto del modelo que conversa.

Anthropic no ofrece un modelo de embeddings, así que la elección es aparte. Las opciones:

| Opción | A favor | En contra |
|---|---|---|
| Modelo local con Ollama (por ejemplo `bge-m3`: multilingüe, 1 024 dimensiones, 8 192 tokens de contexto, 1.2 GB) | Gratis, sin otra llave, los documentos no salen de la máquina, reproducible en CI con un contenedor | Ocupa memoria y CPU; calidad algo menor que los mejores hospedados |
| Proveedor hospedado de embeddings (la documentación de Anthropic sugiere Voyage AI) | Mejor calidad y nada que operar | Segunda llave, segundo costo, los documentos viajan a otro proveedor |

Para documentos en español conviene un modelo multilingüe: uno entrenado casi solo en inglés acerca mal "contraseña" y "clave de acceso".

Sea cual sea, va **detrás de una interfaz propia**. El resto del sistema no sabe quién genera los vectores, y cambiar de proveedor es cambiar una clase:

```csharp
public interface IGeneradorEmbeddings
{
    string Modelo { get; }
    int Dimensiones { get; }
    Task<IReadOnlyList<float[]>> GenerarAsync(IReadOnlyList<string> textos, TipoTexto tipo, CancellationToken ct = default);
}
```

```csharp
// bge-m3 no distingue documento de consulta; otros modelos piden un prefijo o un campo distinto.
using var respuesta = await http.PostAsJsonAsync("/api/embed", new { model = modelo, input = textos }, ct);
```

Dos reglas que la interfaz no puede hacer cumplir sola:

- **Vectores de modelos distintos no se comparan.** Cambiar de modelo obliga a reindexar todo; guarda qué modelo generó el índice.
- **Para probar la tubería no hace falta un modelo.** Un generador léxico por hashing de palabras (determinista, sin red) basta para probar que fragmentar, guardar y buscar funcionan. No entiende sinónimos: no sirve para producción.

## El índice en PostgreSQL con pgvector

pgvector agrega a PostgreSQL el tipo `vector`, operadores de distancia e índices aproximados. No hace falta otra base: los fragmentos viven junto a los datos del producto, con transacciones y respaldos de siempre.

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
CREATE INDEX IF NOT EXISTS fragmentos_embedding
    ON fragmentos USING hnsw (embedding vector_cosine_ops);
```

| Operador | Distancia | Uso |
|---|---|---|
| `<=>` | Coseno: 0 idéntico, 2 opuesto | El habitual para texto |
| `<->` | Euclidiana (L2) | Vectores no normalizados |
| `<#>` | Producto interno, negativo | Vectores normalizados, cuando importa la velocidad |

El índice HNSW es aproximado: rápido, a cambio de que muy de vez en cuando no devuelva el vecino exacto. El operador de clase del índice (`vector_cosine_ops`) tiene que coincidir con el operador de la consulta (`<=>`); si no, PostgreSQL no usa el índice.

Desde .NET, el paquete `Pgvector` registra el tipo en Npgsql, y todo el SQL va parametrizado, también el vector:

```csharp
var constructor = new NpgsqlDataSourceBuilder(cadenaDeConexion);
constructor.UseVector();   // registra el tipo vector de pgvector en Npgsql
return constructor.Build();
```

```csharp
await using var cmd = fuente.CreateCommand("""
    SELECT id, fuente, seccion, contenido, embedding <=> @consulta AS distancia
    FROM fragmentos
    ORDER BY embedding <=> @consulta
    LIMIT @k
    """);
cmd.Parameters.AddWithValue("consulta", new Vector(consulta));
cmd.Parameters.AddWithValue("k", k);
```

Si la extensión se crea desde la misma aplicación, la conexión todavía no conoce el tipo `vector`: después del `CREATE EXTENSION` va un `ReloadTypesAsync()`. Reingestar un documento borra sus fragmentos anteriores e inserta los nuevos en una sola transacción, para que una pregunta nunca vea el documento a medias.

## Recuperar y citar

La consulta trae los k fragmentos más cercanos (k de 3 a 8 es lo común) y se quedan solo los que pasan un **umbral de distancia**. Los que pasan se le entregan al modelo, cada uno con una clave (`F12`), y se le pide una salida estructurada:

```csharp
[SchemaClass("Respuesta a una pregunta usando solo los documentos entregados.")]
public sealed class SalidaRag
{
    [SchemaProperty("true solo si los documentos contienen la respuesta.")]
    public bool Sabe { get; set; }
    // ...
}
```

La API de Claude también trae citas nativas (documentos con `citations` habilitado), pero no se pueden combinar con la salida estructurada. Con claves propias se obtienen las dos cosas, y la validación de citas queda en tu código:

```csharp
// Segunda defensa: toda cita tiene que ser un fragmento que de verdad se le entregó.
var citadas = relevantes.Where(f => salida.Fuentes.Contains(f.Clave)).ToList();
if (citadas.Count == 0 || citadas.Count != salida.Fuentes.Distinct().Count())
    return SinRespuesta("La respuesta no citó fuentes o citó claves que no se recuperaron.", recuperadas, costo);
```

## Decir "no lo sé"

"No lo sé" no es un fracaso: es la respuesta correcta a una pregunta que los documentos no cubren. Se construye en capas, de la más barata a la más cara:

1. **Umbral de distancia.** Si ningún fragmento se parece lo suficiente, se responde "no lo sé" **sin llamar al modelo**: cero costo, cero riesgo de invento.
2. **El modelo puede decir que no.** El esquema trae `sabe`, y el prompt dice que inventar es un error grave y que no sabe es una respuesta correcta.
3. **Las citas se validan.** Una respuesta sin citas, o con una clave que no se recuperó, se convierte en "no lo sé".

```csharp
// Primera defensa contra inventar: si nada se parece, no se le pregunta al modelo.
if (relevantes.Count == 0)
    return SinRespuesta("Ningún fragmento pasó el umbral de distancia.", recuperadas, 0m);
```

El umbral no se adivina: se calibra con preguntas reales. Con uno muy estricto el asistente dice "no lo sé" a todo; con uno muy laxo, el modelo recibe basura y la tentación de inventar.

## Medir la calidad

Un RAG sin medición es una opinión. El juego mínimo es un archivo de casos, cada uno con la pregunta, si los documentos la contienen y de qué documento debe salir la respuesta:

```json
{"id":"rag-01","pregunta":"¿Por qué hay que versionar los prompts?","sabe":true,"fuente":"05-llmops.md"}
{"id":"rag-04","pregunta":"¿Cuál es la capital de Australia?","sabe":false}
```

| Métrica | Qué mide | Qué se mueve para mejorarla |
|---|---|---|
| Acierto con cita correcta | Respondió y citó el documento esperado | Fragmentación, k, prompt |
| "No lo sé" correcto | Ante una pregunta sin respuesta, no inventó | Umbral, prompt |
| Recuperación | El fragmento correcto estaba entre los k | Embeddings, tamaño de fragmento, k |
| Costo por pregunta | Tokens por la tabla de precios | k, tamaño de fragmento, modelo |

Se mide primero la recuperación sin modelo (¿el fragmento correcto aparece entre los k?), porque si no aparece, ningún prompt lo arregla. Y se incluyen preguntas sin respuesta en el juego de casos: un asistente que nunca dice "no lo sé" saca 100% en un juego que solo trae preguntas con respuesta.

## Errores comunes

- **Mezclar vectores de dos modelos en la misma tabla.** Las distancias dejan de significar algo y no hay error que lo avise.
- **No guardar el título de la sección en el fragmento.** La recuperación empeora y la cita no dice de dónde salió.
- **Sin umbral.** El modelo siempre recibe algo, y con algo siempre se puede inventar una respuesta.
- **Confiar en la cita sin validarla.** El modelo puede citar una clave que no se le entregó.
- **Un índice HNSW con un operador distinto al de la consulta.** La consulta funciona, pero escanea toda la tabla.
- **Medir solo preguntas con respuesta.** La métrica premia al asistente que inventa.
- **Indexar cualquier documento que llega.** Lo que entra al índice entra al prompt: ver `10-seguridad-llm.md`.

## Cuándo usar / no usar

| Usar RAG | No usar |
|-----|---------|
| Las respuestas dependen de documentos propios que cambian | Todo el conocimiento cabe en el prompt de sistema |
| Hay que citar de dónde sale cada respuesta | La tarea es redactar, no consultar |
| Los documentos son demasiados para mandarlos completos en cada llamada | Son pocos y estables: mandarlos completos con caché de prompts sale más simple |
| "No lo sé" es una respuesta aceptable para el negocio | El negocio exige una respuesta siempre, aunque no esté documentada |

## Glosario

| Término | Definición |
|---------|-----------|
| RAG | generación aumentada con recuperación: el modelo responde con fragmentos recuperados de documentos propios |
| Ingestión | proceso que lee documentos, los fragmenta, genera sus embeddings y los guarda en el índice |
| Fragmento | pedazo coherente de un documento que se indexa y se recupera como unidad |
| Solape | texto repetido entre fragmentos consecutivos para no perder una idea que cruza el corte |
| Embedding | vector numérico que representa el significado de un texto |
| Modelo de embeddings | modelo que convierte texto en embeddings; distinto del que conversa |
| pgvector | extensión de PostgreSQL que agrega el tipo `vector`, operadores de distancia e índices aproximados |
| Distancia coseno | medida de qué tan distintos son dos vectores por su ángulo; en pgvector va de 0 a 2 |
| HNSW | índice vectorial aproximado basado en grafos, rápido para buscar vecinos cercanos |
| Top-k | los k fragmentos más cercanos a la pregunta |
| Umbral de distancia | distancia máxima a partir de la cual un fragmento ya no se considera relevante |
| Cita | clave del fragmento que respalda una respuesta, validada contra lo que se recuperó |
| Reindexar | regenerar todos los embeddings, obligatorio al cambiar de modelo de embeddings |
| Tasa de aciertos | proporción de preguntas del juego de casos que el asistente responde bien, incluidas las de "no lo sé" |

---

*Rogelio Arriaga Gonzalez*
