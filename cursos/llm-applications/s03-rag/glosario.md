# Glosario - S03
## RAG: que responda con sus documentos

> Los términos de la sesión 3, en orden alfabético.

---

## A

### Abstención
**Categoría:** RAG / Calidad
**Definición:** Respuesta en la que el asistente dice explícitamente que no sabe, porque los documentos no contienen lo que se preguntó. No es una falla: cuando la respuesta no está, abstenerse es el resultado correcto, y una evaluación que no tiene casos sin respuesta no puede medirlo.
**En el curso:** el campo `sabe` del contrato `SalidaRag` y la constante `NoLoSe` de `AsistenteDocumentos`. Los casos `rag-04` y `rag-05` cuentan como acierto solo si el asistente se abstiene.

---

### Alucinación
**Categoría:** LLM / Calidad
**Definición:** Respuesta que el modelo presenta con seguridad pero que no se sostiene en ningún dato: un hecho, una cifra o una política inventada. En RAG aparece cuando el modelo contesta con lo que sabe por su cuenta, o cuando completa lo que a los fragmentos les falta.
**En el curso:** se combate en tres capas: el umbral de distancia, el campo `sabe` y la validación de que cada cita sea un fragmento recuperado.

---

## B

### bge-m3
**Categoría:** Embeddings / Modelo
**Definición:** Modelo de embeddings multilingüe, de código abierto, que produce vectores de 1,024 dimensiones y acepta textos de hasta 8,192 tokens. Se descarga una vez (unos 1.2 GB) y corre en la máquina propia.
**En el curso:** el modelo de embeddings por omisión, servido por Ollama. Se eligió por idioma, privacidad, costo cero por llamada y reproducibilidad en CI.

---

### Búsqueda aproximada de vecinos (ANN)
**Categoría:** Índices / Algoritmos
**Definición:** Búsqueda que encuentra los vectores más cercanos a una consulta sin compararla contra todos, a cambio de que, de vez en cuando, se pierda alguno de los vecinos exactos. Es lo que permite buscar entre millones de vectores en milisegundos.
**En el curso:** el índice HNSW de la tabla `fragmentos` es una búsqueda aproximada.

---

## C

### Cita verificable
**Categoría:** RAG / Seguridad
**Definición:** Referencia de una respuesta a la fuente de la que salió, que el sistema puede comprobar en código: la clave citada tiene que ser uno de los fragmentos que de verdad se entregaron al modelo. Una cita que no se puede comprobar es decoración.
**En el curso:** las claves `F{id}` de `FragmentoRecuperado`. Si el modelo cita una clave que no se recuperó, la respuesta se trata como "no lo sé".

---

### Corpus
**Categoría:** RAG / Datos
**Definición:** El conjunto de documentos sobre el que responde el asistente. Lo que entra al corpus es lo que el modelo va a leer como verdad, así que su origen y su revisión importan tanto como el código.
**En el curso:** las notas de `dev-notes/11-vibe-coding` copiadas a la carpeta `docs/` del proyecto.

---

## D

### Distancia coseno
**Categoría:** Embeddings / Matemáticas
**Definición:** Medida de qué tan distinto apuntan dos vectores, sin importar su longitud. Va de 0 (mismo sentido) a 2 (sentido opuesto), y alrededor de 1 significa que no tienen relación. En pgvector se calcula con el operador `<=>`.
**En el curso:** la columna `distancia` de la búsqueda y el umbral `DistanciaMaxima` de `OpcionesRag`, que arranca en 0.55.

---

## E

### Embedding
**Categoría:** Embeddings / Conceptos
**Definición:** Vector de números que representa el significado de un texto, generado por un modelo especializado. Textos que dicen lo mismo quedan cerca aunque no compartan palabras. Vectores generados por modelos distintos no se pueden comparar entre sí.
**En el curso:** la interfaz `IGeneradorEmbeddings` y sus dos implementaciones, `EmbeddingsOllama` y `EmbeddingsPorHash`.

---

## F

### Fragmento
**Categoría:** RAG / Ingestión
**Definición:** Pedazo de un documento que se indexa y se recupera como unidad. Su tamaño es un compromiso: chico es preciso pero pierde contexto; grande conserva el contexto pero se recupera peor y gasta más tokens.
**En el curso:** `FragmentoNuevo` al ingestar y `FragmentoRecuperado` al buscar. El fragmentador parte por sección, agrega el título de la sección y usa 1,500 caracteres con 200 de solape.

---

## H

### HNSW
**Categoría:** Índices / pgvector
**Definición:** *Hierarchical Navigable Small World*: índice de búsqueda aproximada que organiza los vectores en un grafo por capas y salta de vecino en vecino hacia la consulta. Es rápido y preciso, a cambio de más memoria y de un tiempo de construcción mayor que otras opciones.
**En el curso:** `CREATE INDEX ... USING hnsw (embedding vector_cosine_ops)`, que corresponde al operador `<=>`.

---

## I

### Ingestión
**Categoría:** RAG / Procesos
**Definición:** El proceso de leer documentos, partirlos en fragmentos, generar sus vectores y guardarlos en el índice. Debe ser idempotente (correrlo dos veces deja lo mismo) y atómico por documento.
**En el curso:** el comando `ingestar`, que reemplaza los fragmentos de cada documento en una sola transacción con `ReemplazarFuenteAsync`.

---

## O

### Ollama
**Categoría:** Herramientas / Infraestructura
**Definición:** Servidor que descarga y ejecuta modelos de lenguaje y de embeddings en la máquina propia, con una API HTTP local. Para embeddings se usa `POST /api/embed`, que acepta varios textos en una sola petición.
**En el curso:** el contenedor `ollama/ollama:0.40.15`, publicado solo en `127.0.0.1:11434`, con el modelo `bge-m3`.

---

## P

### pgvector
**Categoría:** Bases de datos / PostgreSQL
**Definición:** Extensión de PostgreSQL que agrega el tipo `vector`, los operadores de distancia y los índices aproximados. Permite guardar vectores junto a los demás datos y buscarlos con SQL, dentro de transacciones.
**En el curso:** la imagen `pgvector/pgvector:0.8.7-pg17`, el paquete `Pgvector` 0.3.2 sobre Npgsql 10.0.3 y la llamada `UseVector()` al construir la fuente de datos.

---

## R

### RAG
**Categoría:** LLM / Arquitectura
**Definición:** *Retrieval-Augmented Generation*, generación aumentada con recuperación: antes de contestar, se buscan los fragmentos relevantes de un corpus propio y se le entregan al modelo junto con la pregunta. Permite responder con datos privados o recientes y citar de dónde salió cada respuesta.
**En el curso:** `AsistenteDocumentos`, que recupera de pgvector, responde con salida estructurada y valida las citas.

---

### Recuperación
**Categoría:** RAG / Procesos
**Definición:** El paso de RAG que convierte la pregunta en un vector y trae los fragmentos más cercanos del índice. Si la recuperación trae lo equivocado, el mejor modelo del mundo contesta mal.
**En el curso:** `AlmacenVectorial.BuscarAsync`, con `ORDER BY embedding <=> @consulta LIMIT @k`.

---

### Reindexar
**Categoría:** RAG / Operación
**Definición:** Volver a generar los vectores de todo el corpus. Es obligatorio al cambiar de modelo de embeddings, porque los vectores de modelos distintos no se comparan, y después hay que volver a calibrar el umbral y a medir.
**En el curso:** se reindexa corriendo otra vez `ingestar docs`; el comando imprime con qué modelo indexó.

---

## S

### Solape
**Categoría:** RAG / Ingestión
**Definición:** Texto que comparten dos fragmentos consecutivos de la misma sección: el siguiente arranca con la cola del anterior. Evita que una idea partida a la mitad quede incompleta en los dos.
**En el curso:** 200 caracteres por omisión, cortados en un espacio para no partir palabras (`Fragmentador.Cola`).

---

## T

### Tasa de aciertos
**Categoría:** Evaluación / Métricas
**Definición:** Porcentaje de casos de prueba en los que el asistente hace lo correcto: responder citando la fuente esperada cuando la respuesta existe, o abstenerse cuando no. Solo sirve si el juego de casos incluye preguntas sin respuesta.
**En el curso:** `EvalRag` la calcula sobre `casos/rag.jsonl` y falla por debajo de 85 %. La tarea sube los casos a 20.

---

### Top-k
**Categoría:** RAG / Parámetros
**Definición:** Número de fragmentos más cercanos que trae la recuperación. Más k da más contexto y más tokens por pregunta; menos k es más barato y arriesga dejar fuera el fragmento correcto.
**En el curso:** `OpcionesRag.K`, que vale 5 por omisión. Se calibra con la tasa de aciertos.

---

## U

### Umbral de distancia
**Categoría:** RAG / Parámetros
**Definición:** La distancia máxima a la que un fragmento todavía cuenta como relevante. Los más lejanos se descartan y, si no queda ninguno, el sistema se abstiene sin llamar al modelo. Depende del modelo de embeddings y del corpus.
**En el curso:** `OpcionesRag.DistanciaMaxima`, 0.55 por omisión. Es la primera capa del "no lo sé" y cuesta cero.
