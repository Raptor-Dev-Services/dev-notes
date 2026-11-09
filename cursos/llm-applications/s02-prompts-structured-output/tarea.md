# Tarea - S02 - Prompts como código y salida estructurada

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1 a 1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/11-vibe-coding/07-llm-en-produccion.md` | Prompts como código, Salida estructurada, Validar el significado, Reintentos acotados, Caché de prompts | 50 min |

### Adelanto de la sesión 3

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/05-bases-de-datos/04-postgresql-avanzado.md` | Todo el documento | 30 min |

### Libros (por capítulo)

| Libro | Capítulos | Para qué |
|-------|-----------|----------|
| *Building LLM Powered Applications* (Valentina Alto, 2024) | 4 y 8 | El prompt como componente de la aplicación y su paso a producción |
| *Unlocking the Secrets of Prompt Engineering* (Gilbert Mizrahi, 2023) | 1 y 5 | Las partes de un prompt y técnicas para mejorarlo |

Los dos libros usan APIs anteriores: piden JSON en el prompt, usan prefill y ajustan la temperatura. Léelos por los conceptos y anota qué técnica reemplaza hoy la salida estructurada.

---

## Parte 2 - Ejercicio: una versión 2 del prompt, medida contra la 1 (2 a 2.5 horas)

### Instrucciones

Trabaja sobre el repositorio `AsistenteCursos` de la práctica. **No edites `prompts/extraccion-soporte.v1.md`**: la versión publicada se queda como está.

### Paso 1 - Amplía el juego de casos

Lleva `casos/extraccion-soporte.jsonl` de 10 a **por lo menos 20 casos**. Los nuevos deben cubrir:

- [ ] Al menos un caso por categoría (`acceso`, `pago`, `contenido`, `otro`) y por urgencia (`baja`, `media`, `alta`)
- [ ] Dos casos ambiguos donde el desempate del prompt decide la urgencia
- [ ] Un mensaje largo, cerca del límite de 4,000 caracteres
- [ ] Un mensaje que mezcla español con otro idioma
- [ ] Dos intentos de dar órdenes al sistema que no usen las palabras "ignora" ni "instrucciones"

Cada caso nuevo lleva el resultado esperado que tú decides **antes** de correrlo, igual que la rúbrica de la sesión 1.

### Paso 2 - Escribe la versión 2

Copia la v1 como `prompts/extraccion-soporte.v2.md` y mejórala donde la v1 falle con tus casos nuevos. Ideas, no obligaciones: un ejemplo más del caso ambiguo, una regla más precisa para la urgencia alta, una definición más clara de "dar órdenes".

Revisa que la v2 siga pasando de 512 tokens: si la recortas por debajo, la caché se apaga sin avisar. Mídela con `count_tokens`.

### Paso 3 - Haz que la evaluación pueda medir las dos versiones

La evaluación de la práctica carga siempre la versión 1. Cámbiala para que la versión salga de una variable de entorno (por ejemplo `ASISTENTE_PROMPT_EXTRACCION`), con la 1 como valor por omisión. Lo mismo para el comando `extraer`.

### Paso 4 - Mide

Corre la evaluación con la v1 y con la v2 sobre el **mismo** juego de casos:

```bash
dotnet test --solution AsistenteCursos.slnx --output detailed -- --filter-trait "Categoria=Eval"
```

Llena esta tabla en `evaluaciones/extraccion-v1-v2.md`:

| Versión | Huella | Aciertos | Tasa | Intentos promedio | Costo USD | Casos que fallaron |
|---------|--------|----------|------|-------------------|-----------|--------------------|
| v1 | | | | | | |
| v2 | | | | | | |

Para los intentos promedio, agrega a la salida de la evaluación el número de intentos de cada caso (está en `ResultadoExtraccion.Intentos`).

### Paso 5 - Decide

Un párrafo: ¿cuál versión queda y por qué? Si la v2 gana, cambia el valor por omisión a 2 **en el mismo commit** que agrega la tabla. Si no gana, la v2 se queda en el repositorio como experimento documentado y la v1 sigue en uso.

---

## Entrega

- [ ] `prompts/extraccion-soporte.v2.md`, con la v1 sin cambios (`git diff` de la v1 vacío)
- [ ] `casos/extraccion-soporte.jsonl` con 20 casos o más
- [ ] La evaluación y el comando `extraer` eligen la versión por variable de entorno
- [ ] `evaluaciones/extraccion-v1-v2.md` con la tabla y la decisión
- [ ] Las pruebas sin costo en verde: `dotnet test --solution AsistenteCursos.slnx -- --filter-not-trait "Categoria=Eval"`

---

## Criterio de aceptación

Tu tarea está completa cuando:

- [ ] Cada caso nuevo tiene su resultado esperado decidido antes de correrlo
- [ ] La tabla trae la huella de cada versión: se puede saber qué texto exacto se midió
- [ ] La comparación usa el mismo juego de casos para las dos versiones
- [ ] La decisión cita la tasa, los intentos y el costo
- [ ] Si agregaste una regla al validador, tiene su prueba unitaria y la viste fallar quitando la regla

---

## Preguntas frecuentes

**¿Y si la v2 sale peor?**
Es un resultado válido y valioso: lo documentas y no la pones en uso. Lo que no vale es cambiar el prompt sin medir.

**¿Puedo cambiar los casos para que la v2 gane?**
No. Los casos se escriben antes y son los mismos para las dos. Si un caso estaba mal, corrígelo y vuelve a medir las dos versiones.

**¿Cuánto cuesta correr 20 casos dos veces con Haiku 5.5?**
Centavos. Revisa el costo que imprime la evaluación.

---

## Preparación para la sesión 3

- Tu tabla v1 contra v2, lista para presentarla en dos minutos
- Docker funcionando en tu máquina: la sesión 3 levanta PostgreSQL con pgvector y un modelo de embeddings local
- Una copia de las notas de `dev-notes/11-vibe-coding/`: son los documentos que va a responder el asistente
