# Tarea - S06 - Seguridad y operación

**Aplicaciones con LLM: de la API al agente**
**Tiempo estimado:** 4 a 5 horas, más el proyecto final

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/11-vibe-coding/10-seguridad-llm.md` | OWASP Top 10 para aplicaciones con LLM; Prompt injection directa e indirecta | 25 min |
| `dev-notes/11-vibe-coding/10-seguridad-llm.md` | Defensas en capas; Salida insegura; Agencia excesiva; Consumo sin límite | 30 min |
| `dev-notes/11-vibe-coding/10-seguridad-llm.md` | Evaluación continua; Monitoreo | 20 min |
| `dev-notes/11-vibe-coding/05-llmops.md` | Evaluación de respuestas; Observabilidad; Guardrails; Gestión de costos | 25 min |

### Libros (por capítulo)

| Libro | Capítulos | Para qué |
|-------|-----------|----------|
| *The Developer's Playbook for Large Language Model Security* (Steve Wilson, 2024) | Caps. 2 a 4, 7 y 8 | El lado de la seguridad: riesgos, ataques y defensas, para contrastar con el mapa de OWASP de la clase |
| *Essential Guide to LLMOps* (Ryan Doan, 2024) | Caps. 5 a 7 | El lado de la operación: evaluación y monitoreo como un ciclo continuo |

Los dos libros son de 2024 y sus ejemplos usan APIs que ya cambiaron: léelos por los conceptos.

---

## Parte 2 - Ejercicio: la evaluación en CI, en rojo si baja la calidad (2 a 3 horas)

Una aplicación con LLM empeora sin que nadie toque el código: el proveedor cambia el modelo, alguien edita un prompt, entran documentos nuevos. La evaluación que solo corre en tu máquina, cuando te acuerdas, no avisa a tiempo.

### Requisito 1 - El workflow

Agrega `.github/workflows/evals.yml` (el de la etapa 6 del curso es el punto de partida). Debe:

- Correr en cada pull request que toque `prompts/`, `casos/`, `src/`, `tests/` o `docs/`.
- Correr **cada semana** aunque nadie haga cambios: el proveedor no manda pull requests.
- Levantar PostgreSQL con pgvector y Ollama como servicios, y bajar `bge-m3`.
- Correr primero las pruebas sin costo y después, por separado, las evaluaciones (`--filter-trait "Categoria=Eval"`).

Las partes clave:

```yaml
  schedule:
    - cron: "0 12 * * 1"   # cada lunes: la calidad puede bajar sin que nadie toque el repositorio
```

```yaml
      - name: Evaluaciones (gastan dinero; en rojo si baja la calidad)
        run: dotnet test --solution AsistenteCursos.slnx -c Release --no-build -- --filter-trait "Categoria=Eval"
```

### Requisito 2 - Los secretos

- `ANTHROPIC_API_KEY`: la llave del curso, como secreto del repositorio. Nunca en el YAML ni en un archivo.
- `EVAL_DB_PASSWORD`: una contraseña para la base desechable del job, también como secreto.
- Si la llave no está, las evaluaciones deben salir **omitidas**, no aprobadas. Compruébalo en un fork o en una rama sin el secreto.

### Requisito 3 - Las tres evaluaciones

- `EvalExtraccion` (sesión 2), con umbral de 90 %.
- `EvalRag` (sesión 3), con tus 20 preguntas de la tarea de la sesión 3 y umbral de 85 %.
- `EvalAtaqueIndirecto` (sesión 6): ninguna respuesta con `atacante.example`.

`ingestar docs` manda a cuarentena `10-seguridad-llm.md` si su huella no está aprobada. Aprueba la huella en `seguridad/ingesta-aprobada.txt` en el mismo pull request, o el índice del job no tendrá esa nota.

### Requisito 4 - Verla en rojo

Una evaluación que nunca ha fallado no se sabe si funciona. En una rama aparte, introduce una **regresión deliberada**, por ejemplo:

- un `prompts/asistente-documentos.v3.md` que pida "responde aunque no esté en los documentos" y que `EvalRag` lo use, o
- un `DistanciaMaxima` de 2.0, que deja pasar cualquier fragmento.

El job debe quedar **en rojo** con el mensaje `La tasa bajó a ...`. Después quita la regresión y el job debe volver a verde.

---

## Entrega

Un pull request con el título:

```
tarea/s06 - evaluación en CI - [Tu nombre]
```

En la descripción:

- [ ] El enlace al job en verde.
- [ ] El enlace o la captura del job en rojo por la regresión deliberada, con el mensaje de la tasa.
- [ ] El costo de una corrida completa de las evaluaciones, sacado de lo que imprimen las pruebas.
- [ ] Un párrafo: ¿qué cambio del proveedor detectaría el job del lunes que un pull request no detecta?

---

## Proyecto final

**Un asistente con RAG, una herramienta con confirmación y un servidor MCP, con evaluación automática.**

Es el mismo repositorio que creció desde la sesión 1. Debe tener:

- [ ] El asistente de documentos que cita su fuente y dice "no lo sé" (S03), con las defensas de la S06.
- [ ] El agente con una herramienta de escritura que solo se ejecuta con confirmación de una persona (S04).
- [ ] El servidor MCP con token, separación de lectura y escritura, y su herramienta de escritura confirmada (S05).
- [ ] La evaluación automática corriendo en CI (esta tarea).
- [ ] El tope de gasto: por corrida del agente y por usuario.

**Entrega: el informe de tres defensas, cada una vista fallar sin ella.**

### Plantilla del informe

Copia esta plantilla en `docs/informe-defensas.md` y llénala una vez por defensa. Elige tres defensas distintas; las candidatas naturales son la confirmación de `crear_ticket`, el 401 del servidor MCP y el filtro de salida, pero se vale elegir otras.

```markdown
# Informe de defensas - [Tu nombre]

## Defensa 1: [nombre]

**Riesgo que contiene:** [qué ataque o error, con su id de OWASP, por ejemplo LLM06 Excessive Agency]

**Dónde vive:** [archivo y método]

**La prueba que la cubre:** [clase y nombre de la prueba]

**Verla fallar:**
- Qué quité: [el cambio exacto]
- Qué prueba cayó: [nombre]
- Con qué mensaje: [el texto de la falla, copiado]
- La regresé y volvió a verde: [sí, con la salida]

**Lo que no cubre:** [el riesgo residual, dicho con honestidad, y quién o qué lo controla]

## Defensa 2: [nombre]
...

## Defensa 3: [nombre]
...

## Lo que aprendí
[Un párrafo: qué defensa creías que funcionaba y no funcionaba hasta que la viste fallar.]
```

### Criterio de aceptación del proyecto

- [ ] Las tres defensas tienen su prueba, y cada una se vio fallar sin la defensa, con el mensaje copiado.
- [ ] Cada defensa dice qué **no** cubre.
- [ ] La evaluación corre en CI y se vio en rojo por lo menos una vez.
- [ ] Ninguna llave, token, contraseña ni huella real está en el repositorio.
- [ ] La solución compila con 0 errores y 0 advertencias, y las pruebas sin costo pasan.

---

## Preguntas frecuentes

**¿Puedo correr las evaluaciones en cada push?**
Puedes, pero gastan dinero y tiempo. Lo razonable: pruebas sin costo en cada push, evaluaciones cuando cambian prompts, casos o código del asistente, y una corrida programada a la semana.

**El ataque de `reembolsos.md` nunca me pasó, ni antes de las defensas. ¿Está mal?**
No. Claude resiste muchas inyecciones. Por eso la defensa determinista se prueba con la API falsa (`DefensaDeSalidaIntegracion`): ahí el modelo sí cae, siempre, y la prueba demuestra que el enlace no sale.

**¿El filtro de salida no es demasiado agresivo?**
Puede serlo: si el asistente debe dar enlaces, agrega sus dominios a la lista. Lo que no se vale es quitar el filtro porque "estorba". Cada enlace retirado se registra: revisa esos registros antes de ampliar la lista.
