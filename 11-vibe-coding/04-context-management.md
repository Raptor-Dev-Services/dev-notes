# 04 · Manejo de contexto en sesiones largas

## Problema que resuelve

Los modelos de lenguaje tienen una ventana de contexto finita. En sesiones largas, el contexto se comprime o se pierde, y el AI empieza a generar respuestas inconsistentes con decisiones tomadas al inicio de la sesión. El manejo deliberado del contexto mantiene la coherencia y reduce retrabajo.

## Cómo funciona el contexto en Claude Code

Claude Code mantiene el historial de la sesión en memoria. Al aproximarse al límite:
- Mensajes antiguos se comprimen automáticamente (resumen)
- El AI puede perder referencia a decisiones tempranas
- El código generado puede contradecir patrones establecidos al inicio

El límite efectivo varía por modelo, pero una sesión de 2-3 horas de trabajo intensivo puede aproximarse al límite.

## Estrategias para sesiones largas

### 1. CLAUDE.md como memoria persistente

Lo que se define en `CLAUDE.md` no consume contexto de la sesión. Se carga al inicio y permanece disponible. Ante una sesión larga, lo primero es mover a `CLAUDE.md` todo lo que el AI necesitará repetidamente:

```markdown
# en CLAUDE.md — no en el chat
## Decisiones de diseño de esta iteración
- Aprobaciones de QC usan el patrón LineQcApproval (ver existente)
- Los estados válidos son: Pending, Approved, Rejected, OnHold
- On Hold requiere comentario obligatorio
```

### 2. Commits frecuentes como checkpoints

Cada commit es un checkpoint al que el AI puede volver con `git log`. En lugar de describir cambios pasados en el chat (que consume contexto), pedir al AI que lea el historial:

```
"Lee los últimos 5 commits con git log --oneline y resume qué se implementó"
```

### 3. Partir tareas grandes en sesiones separadas

Una sesión = un módulo o una feature bien delimitada. Al terminar la sesión:

1. Commitear todo el trabajo
2. Documentar las decisiones no obvias en `CLAUDE.md`
3. Cerrar la sesión
4. En la siguiente sesión, el CLAUDE.md reconstituye el contexto

```markdown
# CLAUDE.md — agregar al terminar la sesión
## Módulo Production Orders (implementado 2026-05-11)
- Handler: ApproveProductionOrderHandler — ver UseCases/Production/
- Estados: Draft → Submitted → InReview → Approved / Rejected
- Regla: solo el supervisor del turno puede aprobar (rol: ShiftSupervisor)
- Pendiente: endpoint para listar órdenes por turno
```

### 4. Resumen explícito al inicio de sesión

Al retomar un trabajo después de un break o al día siguiente, iniciar con un resumen explícito:

```
"Ayer implementamos el módulo de QC approvals (commits abc123 a def456).
Hoy continuamos con el módulo de Production Orders.
El diseño acordado es: [descripción].
Comienza leyendo los archivos de QC para entender el patrón."
```

### 5. /clear para resetear sin perder el proyecto

Cuando el AI está generando respuestas incoherentes, `/clear` limpia el historial de la sesión pero mantiene `CLAUDE.md`. Esto da un AI "fresco" con el contexto del proyecto intacto.

```bash
# en Claude Code
/clear

# el AI reabre con CLAUDE.md cargado — sin el historial de conversación confuso
```

## Qué va en el chat vs qué va en CLAUDE.md

| En el chat (contexto de sesión) | En CLAUDE.md (contexto persistente) |
|--------------------------------|-------------------------------------|
| instrucciones específicas de la tarea actual | stack y versiones del proyecto |
| código que se está construyendo | convenciones de naming |
| errores y correcciones de la sesión | restricciones permanentes |
| preguntas exploratorias | comandos frecuentes |
| decisiones que solo aplican a esta sesión | decisiones de diseño que aplican a todo el proyecto |

## Indicadores de pérdida de contexto

- El AI genera código con patrones que ya se descartaron
- Propone agregar dependencias que ya están instaladas
- Repite una explicación que ya dio antes en la sesión
- Genera nombres de clases que no siguen las convenciones del proyecto

Ante estos síntomas: corregir explícitamente, luego evaluar si agregar la restricción a `CLAUDE.md`.

## Cache de prompts

Claude guarda en caché el prefijo del contexto (incluyendo `CLAUDE.md`) durante ~5 minutos entre mensajes. Una sesión activa dentro de ese tiempo reutiliza el caché. Las respuestas son más rápidas y el costo de tokens es menor. Sesiones inactivas por más de 5 minutos pagan el costo completo al retomar.

Implicación práctica: en sesiones de trabajo, mantener el ritmo en lugar de pausar 10-15 minutos entre mensajes.

## Memoria persistente entre proyectos

Claude Code tiene un sistema de memoria en `~/.claude/projects/<proyecto>/memory/`. Lo que se guarda ahí persiste entre sesiones sin ocupar espacio en `CLAUDE.md`:

- Preferencias del desarrollador (estilo de respuesta, idioma, nivel de detalle)
- Contexto del proyecto que cambia con el tiempo (estado del sprint, decisiones recientes)
- Referencias a recursos externos (tablero de Linear, canal de Slack del equipo)

La memoria se usa automáticamente. El AI la carga al inicio de cada sesión en ese proyecto.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| CLAUDE.md para contexto que aplica a toda la base de código | CLAUDE.md para instrucciones de una sola sesión |
| /clear cuando la sesión está generando código inconsistente | /clear como respuesta a cualquier error (primero corregir explícitamente) |
| commits frecuentes como checkpoints semánticos | commits grandes al final del día que mezclan múltiples features |
| sesiones enfocadas en una feature o módulo | sesiones largas que tocan múltiples partes del sistema |


> Fuente: *Building LLM Powered Applications* (Valentina Alto): Ch.5 Context Management and Memory

---

## Glosario

| Término | Definición |
|---------|-----------|
| Ventana de contexto | cantidad máxima de tokens que un modelo de lenguaje puede procesar en una sola llamada |
| Token | unidad mínima de procesamiento de un LLM; aproximadamente 4 caracteres en inglés o 3 en español |
| Context drift | degradación de la coherencia del AI cuando la ventana de contexto se llena o la sesión es muy larga |
| CLAUDE.md | archivo de instrucciones del proyecto que Claude carga automáticamente al inicio de cada sesión |
| AGENTS.md | variante de CLAUDE.md compatible con otros agentes de AI como Cursor, Copilot Workspace y Codex |
| /clear | comando de Claude Code que reinicia la conversación descartando el historial acumulado |
| Caché de prompts | mecanismo de Claude que reutiliza el prefijo del contexto entre mensajes para reducir latencia y costo |
| Memoria persistente | sistema de archivos en `~/.claude/projects/` donde Claude guarda contexto específico del proyecto entre sesiones |
| Checkpoint semántico | commit de git que sirve como punto de retorno seguro para una sesión de trabajo con el agente |
| System prompt | instrucciones fijas que preceden al historial de mensajes y definen el comportamiento del modelo en toda la sesión |

---

*Rogelio Arriaga Gonzalez*
