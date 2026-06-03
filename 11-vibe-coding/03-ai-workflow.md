# 03 · AI workflow — Claude Code en el ciclo de desarrollo

## Problema que resuelve

Sin un flujo definido, el AI se usa de forma reactiva ("arregla esto que falló") en lugar de proactiva. Un workflow deliberado integra el AI en cada fase del ciclo (diseño, implementación, revisión y documentación), multiplicando la velocidad sin comprometer la calidad.

## El ciclo de desarrollo con AI

```
1. Diseño          → AI como sparring (trade-offs, alternativas)
2. Scaffolding     → AI genera la estructura base
3. Implementación  → AI escribe el código, humano revisa y corrige
4. Tests           → AI genera casos, humano verifica cobertura
5. Revisión        → AI revisa, humano aprueba
6. Documentación   → AI redacta, humano valida
```

El humano nunca desaparece. Decide qué hacer, valida que el AI lo hizo bien, y mantiene el contexto del negocio que el AI no tiene.

## Cuándo delegar al AI

| Tarea | Delegar | Por qué |
|-------|---------|---------|
| Boilerplate (handler, repository, tests de caso feliz) | sí | código predecible, bajo riesgo |
| Refactoring mecánico (renombrar, extraer método) | sí | transformación determinista |
| Generar casos de tests | sí | el AI encuentra edge cases que el humano omite |
| Escribir docs de métodos públicos | sí | formato repetitivo |
| Diseño de arquitectura | no directo | el AI no conoce las restricciones del negocio |
| Lógica crítica de negocio | parcial | el AI escribe, el humano valida con product owner |
| Decisiones de seguridad | parcial | el AI propone, el humano verifica contra política |
| Queries SQL complejas de producción | parcial | el AI genera, el humano analiza el plan de ejecución |

## Flujo por tarea

### Nueva feature

```
1. Describir la feature al AI y pedir análisis de impacto:
   "¿qué archivos existentes hay que modificar para agregar X?"

2. Validar la lista de archivos afectados antes de escribir código

3. Pedir el diseño del UseCase primero (sin código):
   "¿cómo modelarías el handler y el dominio para X?"

4. Si el diseño es correcto, pedir la implementación:
   "Implementa el handler siguiendo el patrón de [handler existente]"

5. Revisar el código generado antes de aceptarlo

6. Pedir los tests:
   "Genera tests unitarios para el handler que acabas de crear"

7. Revisar los tests y ejecutarlos
```

### Bug en producción

```
1. Recopilar evidencia: logs, stack trace, request que falló

2. Dar toda la evidencia al AI y pedir hipótesis:
   "Dada esta evidencia, ¿cuáles son las 3 causas más probables?"

3. Validar cada hipótesis contra el código (el AI puede leer los archivos)

4. Cuando se identifica la causa, pedir el fix:
   "El problema está en línea X de Y. Corrígelo sin cambiar el comportamiento
   del resto del método"

5. Pedir un test de regresión para el bug
```

### Code review

```
1. Pedir al AI que revise el diff:
   "Revisa este PR buscando: violaciones de Clean Architecture,
   problemas de seguridad, queries N+1, y code smells"

2. Evaluar los comentarios — el AI puede señalar falsos positivos

3. Responder al AI con el contexto que le falta:
   "Este comportamiento es intencional porque [razón]"
```

## Comandos útiles de Claude Code

```bash
# iniciar sesión con contexto del proyecto
claude                    # en el directorio raíz — carga CLAUDE.md automáticamente

# pedir revisión del último cambio
/diff                     # ver qué cambió

# buscar en el codebase sin salir del contexto
"busca todos los lugares donde se usa IOrderRepository"

# pedir que el AI lea un archivo antes de responder
"lee src/Application/UseCases/Orders/ y luego explica el flujo"
```

## Gestión del contexto en la sesión

Claude Code mantiene el contexto de la sesión en memoria. Para sesiones largas:

- Abrir la sesión con el objetivo claro: "voy a implementar el módulo de production orders"
- Hacer commits frecuentes para que el AI pueda ver el historial
- Si el AI empieza a generar código inconsistente, resetear con `/clear` y reabrir con contexto fresco
- Las sesiones muy largas (>2h) tienden a perder coherencia. Conviene partir la tarea.

## División de trabajo típica

```
Humano:
  - Define qué construir y por qué
  - Valida el diseño contra los requisitos del negocio
  - Revisa el código generado antes de commitear
  - Ejecuta los tests y valida los resultados
  - Hace el commit con el mensaje correcto

AI:
  - Genera el código siguiendo los patrones del proyecto
  - Encuentra edge cases y los cubre en tests
  - Explica el código existente
  - Detecta problemas en code reviews
  - Redacta documentación técnica
```

## Señales de que el AI está fuera de contexto

- Genera código que no sigue los patrones del proyecto
- Propone cambios en archivos que no debería tocar
- Da respuestas genéricas que ignoran el stack real
- Repite un error que ya se corrigió

Respuesta: corregir explícitamente y, si se repite, agregar la restricción a `CLAUDE.md`.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| para acelerar trabajo repetitivo y bien definido | como sustituto del entendimiento propio del código |
| para obtener una segunda opinión en decisiones de diseño | para tomar decisiones de arquitectura sin comprenderlas |
| para explorar alternativas rápidamente | para obviar la revisión de código generado |
| para documentar código que ya funciona | para parchear código sin entender el bug |

---

## Agentes autónomos — delegar tareas completas
> Fuente: *Building LLM Powered Applications*: Ch.6 AI Agents and Tooling; Ch.8 Multi-Step Reasoning

Los agentes van más allá del chat: tienen acceso a herramientas (leer archivos, ejecutar comandos, hacer búsquedas) y pueden completar tareas de múltiples pasos sin intervención humana en cada paso.

```
Chat tradicional:
  Humano escribe → AI responde → Humano lee → Humano actúa → repetir

Agente:
  Humano da objetivo → AI planifica → AI ejecuta paso 1 → AI evalúa resultado
  → AI ejecuta paso 2 → ... → AI reporta resultado
```

### Ciclo de un agente (ReAct pattern)

```
Thought:  "Necesito encontrar qué archivos implementan IExampleUserRepository"
Action:   grep("IExampleUserRepository", "*.cs")
Observation: ["Infrastructure/Repositories/ExampleUsers/ExampleUserRepository.cs"]
Thought:  "Tengo la implementación. Ahora necesito leer el archivo."
Action:   read("Infrastructure/Repositories/ExampleUsers/ExampleUserRepository.cs")
Observation: [contenido del archivo]
Thought:  "Ahora puedo responder la pregunta del usuario."
Answer:   "La implementación está en Infrastructure/Repositories/... y hace..."
```

### Cuándo usar modo agente vs chat simple

| Modo Chat | Modo Agente |
|-----------|-------------|
| Preguntas conceptuales | Implementar una feature completa |
| Explicar código ya conocido | Analizar un bug con múltiples archivos |
| Generar un snippet pequeño | Agregar tests a todos los handlers sin tests |
| Tomar una decisión de diseño | Refactorizar un módulo completo |

### Instrucciones efectivas para agentes

```
❌ Vago: "mejora el código"
✓ Específico: "refactoriza GetExampleUsersHandler para usar el patrón de
   paginación de GetProductsHandler (Infrastructure/Repositories/Products/),
   sin cambiar el contrato público del método"

❌ Sin contexto: "agrega tests"
✓ Con contexto: "agrega tests unitarios al InsertExampleUserHandler siguiendo
   el estilo de InsertProductHandler.Tests.cs. Cubrir: éxito, email duplicado,
   nombre vacío"

❌ Sin restricciones: "optimiza los queries"
✓ Con restricciones: "optimiza solo el GetPagedAsync en ExampleUsersSql.cs.
   No cambies la interfaz del repositorio. Usa EXPLAIN ANALYZE para verificar."
```

### Checkpoints — controlar el agente en tareas largas

```bash
# Establecer checkpoints explícitos al inicio
"Implementa el módulo de Production Orders. 
Haz commits después de cada etapa:
1. Domain entities (ExampleOrder, ExampleOrderLine)
2. Repository interface + SQL
3. Use cases (Insert, Get, GetPaged)
4. Unit tests
5. Controller + Presenter

Muéstrame el diseño antes de escribir código."
```

```bash
# Si el agente se desvía del plan
"Para. Revisa lo que acabas de hacer contra el patrón en
InsertExampleUserHandler. ¿Qué diferencias hay? Corrige antes de continuar."
```

---

## Glosario

| Término | Definición |
|---------|-----------|
| Vibe coding | estilo de desarrollo donde el programador describe la intención en lenguaje natural y el AI genera el código |
| Agente de AI | sistema que usa un modelo de lenguaje con acceso a herramientas para completar tareas de múltiples pasos de forma autónoma |
| ReAct pattern | patrón de razonamiento de agentes: Thought → Action → Observation → Thought... hasta llegar a la respuesta |
| Tool use | capacidad de un agente de invocar funciones externas (leer archivos, ejecutar comandos, hacer búsquedas) |
| Checkpoint | instrucción explícita de hacer commit o pedir aprobación después de cada etapa de una tarea larga del agente |
| Drift | desviación gradual del agente respecto al patrón o convenciones del proyecto, acumulada en tareas largas |
| Human-in-the-loop | diseño de flujo de trabajo donde el humano aprueba o corrige al agente en puntos críticos de la tarea |
| Mode chat | uso del AI para preguntas, explicaciones y snippets pequeños sin herramientas ni autonomía |
| Mode agent | uso del AI como agente con acceso a herramientas para tareas complejas de múltiples archivos |
| Scaffolding | generación automática de la estructura de archivos y código boilerplate para un nuevo módulo o feature |

---

*Rogelio Arriaga Gonzalez*
