# Tarea - S03 - RAG: que responda con sus documentos

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 4 a 5 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/11-vibe-coding/08-rag.md` | La tubería de RAG; Fragmentar; Embeddings; El índice en PostgreSQL con pgvector; Recuperar y citar; Decir "no lo sé"; Medir la calidad | 60 min |
| `dev-notes/05-bases-de-datos/04-postgresql-avanzado.md` | Todo el documento, con atención a índices y extensiones | 30 min |

### Libros (por capítulo)

| Libro | Capítulos | Para qué |
|-------|-----------|----------|
| *Building Data-Driven Applications with LlamaIndex* (Andrei Gheorghiu, 2024) | Caps. 4 a 7 | Ingestión, índices y recuperación de punta a punta |
| *Building LLM Powered Applications* (Valentina Alto, 2024) | Caps. 5 y 7 | Cómo encaja la búsqueda en una aplicación con LLM |

Los dos libros son de 2024 y usan librerías de Python que ya cambiaron. Léelos por los conceptos
(qué decide cada etapa y por qué); el código del curso es el que escribiste en la práctica.

---

## Parte 2 - Ejercicio (2 a 3 horas)

**Medir la tasa de aciertos de tu asistente con 20 preguntas y mejorarla.**

### Paso 1 - Los 20 casos

Sube `casos/rag.jsonl` de 5 a 20 casos. Reglas:

- **Al menos 5 casos sin respuesta** en los documentos (`"sabe": false`). Que sean plausibles: preguntas
  que un alumno de verdad haría y que el corpus no contesta, no solo preguntas absurdas.
- En los casos con respuesta, `fuente` es el archivo donde está, por ejemplo `"05-llmops.md"`.
- Escribe tú las preguntas leyendo las notas. Si se las pides a un modelo, va a escribir las que
  él sabe contestar y la evaluación va a salir mejor de lo que es.
- Al menos 3 preguntas que **no usen las palabras** del documento (por ejemplo, "¿cuánto me cuesta
  cada llamada?" para una sección que habla de tokens).

```json
{"id":"rag-06","pregunta":"¿Qué conviene medir antes de cambiar de modelo?","sabe":true,"fuente":"05-llmops.md"}
{"id":"rag-07","pregunta":"¿Cuál es el horario de atención del soporte?","sabe":false}
```

### Paso 2 - La línea base

Corre la evaluación sin cambiar nada más:

```bash
dotnet test --solution AsistenteCursos.slnx -- --filter-trait "Categoria=Eval"
```

Anota la tasa, cuántas fallas son "respondió cuando no debía", cuántas son "dijo no lo sé cuando sí
estaba" y cuántas son "citó el archivo equivocado". Anota también el costo aproximado de la corrida.

### Paso 3 - Mejorar, una palanca a la vez

Mueve **una sola cosa por corrida** y vuelve a medir. Palancas posibles:

| Palanca | Dónde |
|---------|-------|
| Umbral de distancia | `OpcionesRag.DistanciaMaxima` |
| Número de fragmentos | `OpcionesRag.K` |
| Tamaño y solape del fragmento | `Fragmentador.Partir(..., maxCaracteres, solape)` y reingestar |
| Reglas del prompt | Una versión nueva: `prompts/asistente-documentos.v2.md` (no edites la v1) |

Si cambias el tamaño del fragmento, **reingesta todo** antes de medir. Si cambias el prompt, crea una
versión nueva y cambia el número en `Preguntar.cs` y en `EvalRag.cs`: la v1 se queda como estaba.

> En la sesión 6 el curso agrega una versión del prompt con reglas de seguridad. Si aquí ya creaste
> la v2, en la S06 tu v2 es el punto de partida y las reglas nuevas van en la versión siguiente.

### Paso 4 - La tabla

Entrega una tabla como esta, con una fila por corrida:

| Corrida | Cambio | Tasa | Respondió de más | No lo sé de más | Fuente equivocada |
|---------|--------|------|------------------|-----------------|-------------------|
| 0 | Línea base (K=5, umbral 0.55, 1,500/200, v1) | | | | |
| 1 | | | | | |
| 2 | | | | | |

Y un párrafo final: tu configuración elegida y **por qué**, con los números de la tabla.

---

## Entrega

- [ ] `casos/rag.jsonl` con 20 casos, al menos 5 sin respuesta, en tu repositorio.
- [ ] La salida de la evaluación de la línea base y de la corrida final.
- [ ] La tabla de corridas con un cambio por fila.
- [ ] El párrafo con la configuración elegida y su justificación.
- [ ] Si cambiaste el prompt, el archivo de la versión nueva, sin tocar la anterior.
- [ ] El commit, sin `.env` ni llaves en el diff.

---

## Criterio de aceptación

- [ ] Los 20 casos están escritos por ti y al menos 5 no tienen respuesta en el corpus.
- [ ] Cada fila de la tabla cambia una sola cosa respecto a la anterior o a la línea base.
- [ ] La configuración final está justificada con números, no con impresiones.
- [ ] La evaluación final pasa el umbral de 85 % o, si no lo pasa, explicas qué la detiene.
- [ ] `dotnet build` sigue con 0 errores y 0 warnings.

---

## Preguntas frecuentes

**¿Y si la línea base ya sale en 100 %?**
Tus casos son demasiado fáciles. Agrega preguntas con otras palabras y preguntas sin respuesta que se
parezcan mucho a algo que sí está. Una evaluación que nunca falla no está midiendo.

**¿Puedo cambiar el modelo de embeddings?**
Sí, como una fila más de la tabla, pero recuerda: hay que reindexar todo y volver a calibrar el
umbral, porque las distancias de otro modelo no se parecen a las de `bge-m3`.

**¿Cuánto cuesta correr la evaluación?**
Con `claude-haiku-5-5` y 20 preguntas, centavos de dólar. Los casos que caen en el umbral no llaman
al modelo y cuestan cero. Aun así, no la corras en un bucle sin mirar.

---

## Preparación para S04

- Tu práctica de S03 en verde y la base levantada: el agente de la S04 guarda sus tickets ahí.
- Una pregunta concreta sobre algo de RAG que no te quedó claro.
- Piensa en una acción de tu trabajo que te daría miedo dejarle a un agente sin que nadie la revise.
