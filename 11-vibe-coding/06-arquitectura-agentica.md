# 06 · Arquitectura agéntica — catálogo de agentes, skills y reglas

## Problema que resuelve

El primer intento de configurar un asistente de código siempre es el mismo: un `CLAUDE.md` que crece hasta las 800 líneas mezclando stack, convenciones, procedimientos y advertencias. Todo se carga en cada sesión aunque la tarea sea cambiar un color, nadie lo mantiene, y cuando arranca el segundo proyecto se copia entero y las dos versiones divergen en un mes.

La salida es dejar de tratar la configuración como un documento y tratarla como **arquitectura**: piezas separadas por tipo, cada una con su formato y su momento de carga, y un catálogo central del que cada proyecto toma solo lo que aplica.

Este doc describe la arquitectura del catálogo interno (`Raptor-Dev-Services/agents`), que hoy son 10 agentes, 23 skills, 47 reglas, 6 comandos y 8 templates.

Complementa `01-claude-md.md`, que cubre el `CLAUDE.md` de un proyecto. Aquí va la capa de arriba: de dónde sale ese contenido y cómo se organiza para reusarlo.

## Las tres capas

La decisión central es la taxonomía. Tres tipos de pieza que **se complementan y no se duplican**:

| Capa | Qué es | Forma | Ejemplo |
|------|--------|-------|---------|
| **Regla** | La ley | Restricción declarativa, corta | "El SQL siempre va parametrizado" |
| **Skill** | El procedimiento | Pasos, ejemplos, checklist | Cómo montar un endpoint completo |
| **Agente** | El trabajador | Persona con criterio que aplica reglas y skills | `backend-developer` |

El tamaño promedio en el catálogo confirma que la separación es real, no nominal:

```
reglas    51 lineas promedio   (declarativas: dicen QUE, no COMO)
skills   182 lineas promedio   (procedimentales: pasos, codigo, checklist)
agentes  175 lineas promedio   (persona: identidad, criterio, prioridades)
```

**El test para saber cuál es cuál:**

- Si se puede escribir como una frase imperativa sin pasos, es **regla**.
- Si necesita "primero esto, luego aquello" o un ejemplo de código, es **skill**.
- Si la respuesta correcta depende del criterio de quién lo hace, es **agente**.

Cuando una pieza empieza a mezclar las tres, se parte. Una regla que crece hasta explicar *cómo* se hace algo dejó de ser regla: el detalle procedimental se muda a una skill y la regla se queda con la ley.

### Por qué importa la separación

Cada capa se carga en un momento distinto:

- **Las reglas se pegan en `CLAUDE.md`** y están siempre en contexto. Por eso son cortas: cada línea se paga en todas las sesiones.
- **Las skills se auto-invocan** cuando la tarea coincide con su `description`. Ocupan cero contexto hasta que hacen falta, así que pueden ser largas y detalladas.
- **Los agentes se invocan explícitamente** o por delegación, y corren con su propio contexto.

Meter un procedimiento de 180 líneas en el `CLAUDE.md` lo hace competir por atención con todo lo demás, en cada sesión, aunque nadie vaya a tocar ese tema. Ese es el costo real de no separar.

## Anatomía de cada pieza

### Regla

Markdown plano, sin frontmatter. Vive en `rules/` en el catálogo y su **contenido** se pega en el `CLAUDE.md` del proyecto.

```markdown
# Fronteras de import

Las dependencias fluyen hacia abajo y nunca de vuelta: app -> features -> shared.

- `shared` no importa de `features` ni de `app`.
- `features` no importa de `app`.
- Un feature no importa de otro salvo dependencia declarada.

## Antipatrones
- Import cruzado entre features -> acoplamiento que nadie decidio.
```

La sección `## Antipatrones` al final es una convención útil: nombra el error concreto y su consecuencia. Un modelo reconoce mejor "no hagas esto, pasa aquello" que una prescripción abstracta.

### Skill

Carpeta `skills/<nombre>/SKILL.md` con frontmatter de dos campos:

```markdown
---
name: feature-based-frontend
description: Estructura y construye frontend por feature. Usala al crear pantallas,
  features, servicios HTTP, hooks de datos o formularios. Disparadores: "nueva pagina",
  "nuevo feature", "servicio API", "consumir endpoint", "hook de datos", "formulario".
---

# Frontend por feature

## 1. Estructura obligatoria
...
## Checklist
- [ ] ...
```

**La `description` es el mecanismo de selección, no documentación.** Es lo único que el modelo ve para decidir si la skill aplica. Una descripción vaga ("ayuda con el frontend") no se dispara nunca; una con disparadores literales entre comillas se dispara cuando toca. Escribe ahí las palabras que la persona va a usar realmente, no las que un manual usaría.

El cuerpo lleva pasos numerados, ejemplos de código y un checklist final.

### Agente

`agents/<nombre>.md` con frontmatter nativo:

```markdown
---
name: frontend-developer
description: Desarrollador frontend que implementa features de extremo a extremo.
  Usalo cuando haya que crear pantallas, hooks de datos, tablas con paginacion,
  formularios con validacion, manejo de estados loading/empty/error/success.
tools: Read, Edit, Glob, Grep
---

# Frontend Developer

Eres Frontend Developer de {{PROJECT_NAME}}...

## Identidad
- Stack, estructura, HTTP, routing, regla absoluta

## Reglas criticas
1. El design system es la unica fuente de verdad
2. Todo flujo tiene skeleton
...
```

Un agente **no repite el contenido de las reglas**: las referencia y aporta el criterio de cuándo pesa más una que otra. La sección "Reglas críticas" numerada funciona bien porque da un orden de prioridad explícito cuando dos restricciones chocan.

El campo `tools` acota qué puede hacer. Un agente de revisión no necesita `Edit`.

### Comando

`commands/<nombre>.md`. Slash commands que la persona invoca a mano (`/fix-bug`, `/deploy`).

```markdown
---
description: Corrige un bug documentando hallazgos por severidad
argument-hint: descripcion del bug o numero de tarjeta
---

1. Mapea el flujo afectado antes de tocar nada.
2. Documenta cada bug: Estado / Archivos / Descripcion / Fix + severidad.
3. Corrige por orden de severidad.
4. Valida el build.
```

La diferencia con una skill: la skill se auto-invoca por contexto, el comando lo dispara la persona. Un flujo que debe correr **completo y en orden** es comando; uno que aplica cuando la tarea lo pide es skill.

### Template

Markdown plano que se copia al proyecto como punto de arranque: `CLAUDE.md.template`, `adr.template.md`, `pull-request.template.md`, `board.template.md`.

## El catálogo como fuente de verdad

Dos decisiones sostienen la reutilización:

**1. Todo es genérico.** Ninguna pieza menciona un proyecto real. Los valores concretos son placeholders `{{UPPER_SNAKE}}`:

```markdown
- Centralizar todo el acceso HTTP en `{{API_DIR}}`.
- El cliente HTTP vive en `{{HTTP_CLIENT_PATH}}`.
- Toda cadena visible pasa por `{{I18N_PATH}}` (`{{LOCALES}}`).
```

**2. Un solo diccionario de placeholders.** `templates/project-variables.md` es la fuente de verdad: qué significa cada uno, un ejemplo, y una columna en blanco para el valor del proyecto. Antes de inventar un placeholder nuevo se revisa que no exista con otro nombre.

Sin el diccionario aparecen `{{API_DIR}}`, `{{API_PATH}}` y `{{SERVICES_DIR}}` para la misma carpeta, y el find-and-replace deja la mitad sin rellenar.

Al copiar al proyecto:

```
agents/*.md           ->  <repo>/.claude/agents/
skills/<n>/SKILL.md   ->  <repo>/.claude/skills/<n>/SKILL.md
commands/*.md         ->  <repo>/.claude/commands/
rules/*.md            ->  se PEGA el contenido en <repo>/CLAUDE.md
templates/*           ->  raiz del repo destino
```

## La matriz de selección: no copiar todo

El error más caro es copiar el catálogo entero "por si acaso". **Una regla que no usas es ruido para el modelo**: compite por atención con las que sí aplican, y en el peor caso lo empuja a aplicar un patrón que el proyecto no usa.

La solución es un procedimiento de onboarding que decide **qué se copia y qué se omite** según el stack. Tres pasos:

**Paso 1, detectar antes de preguntar.** No preguntes lo que el repo ya dice:

| Señal en el repo | Conclusión |
|------------------|------------|
| `*.csproj`, `Program.cs` | backend .NET |
| `Modules/` con varios proyectos | monolito modular |
| Entidades con `TenantId`, query filters | multi-tenant |
| `package.json` con `react` + `vite` | frontend React |
| `Dockerfile`, `.github/workflows/` | DevOps |

**Paso 2, cuestionario solo de los huecos.** Lo que no se pudo detectar.

**Paso 3, matriz.** Una tabla por condición, con lo que entra y lo que se omite:

```
Universales (siempre)
  agents: code-reviewer, test-engineer
  rules:  clean-code, solid, security-baseline, testing-strategy, ...

Si hay backend .NET
  agents: backend-developer, database-engineer
  skills: clean-architecture-backend, api-error-handling

Acceso a datos - elige UNO
  No multi-tenant  -> skills/dapper-data-access    (omite EF Core y tenancy)
  Si multi-tenant  -> skills/ef-core-data-access   (omite Dapper)
                    + skills/multi-tenancy + rules/multi-tenancy

Condicionales
  usa i18n          -> rules/i18n-frontend
  usa tiempo real   -> rules/realtime-delivery
  usa Stripe        -> agents/payments-engineer + skills/stripe-payments
```

Las filas **"elige UNO"** son las que más valen: copiar las dos estrategias de acceso a datos deja al modelo eligiendo entre patrones incompatibles cada vez.

Este procedimiento vive en un `SETUP.md` del catálogo y lo ejecuta un agente: se le dice "lee `SETUP.md` y configura mi `.claude/` según mi stack", detecta, pregunta lo que falta, aplica la matriz, copia y rellena placeholders.

## Cómo se mantiene

Cinco reglas que evitan que el catálogo se degrade:

1. **Las reglas son cortas.** Si una crece explicando el cómo, el detalle se muda a una skill.
2. **Cero contenido de un proyecto real.** Si hace falta un ejemplo concreto, va marcado como "Ejemplo en {{STACK}}".
3. **Todo placeholder nuevo va al diccionario**, con la convención `{{UPPER_SNAKE}}`.
4. **Frontmatter nativo**, sin campos inventados. El nombre del archivo coincide con el `name`.
5. **Una pieza, un concepto.** Si dos piezas dicen lo mismo con distinta palabra, se fusionan. Divergen en silencio.

Una práctica que aporta más de lo que cuesta: cuando una regla nace de un incidente real, **escribe el incidente**. "Filtrar en cliente sobre datos paginados esconde renglones sin dar error" se recuerda y se respeta; "filtra en el servidor" se discute.

## Errores comunes

| Error | Qué pasa |
|-------|----------|
| Todo en un `CLAUDE.md` de 800 líneas | Se carga completo cada sesión; el modelo diluye atención; nadie lo mantiene |
| Regla que explica el procedimiento | Ocupa contexto permanente con detalle que se necesita una vez al mes |
| Skill con `description` vaga | No se dispara nunca; la escribiste para nada |
| Agente que repite el texto de las reglas | Dos copias que divergen; el agente debe referenciar, no duplicar |
| Copiar el catálogo entero al proyecto | Reglas de patrones que no usas empujando al modelo en la dirección equivocada |
| Copiar las dos opciones de una decisión excluyente | El modelo elige distinto cada vez |
| Placeholder nuevo sin registrar | Nombres duplicados para el mismo valor; find-and-replace incompleto |
| Piezas versionadas solo en el proyecto | Cada repo evoluciona su copia; a los seis meses no hay estándar |

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Varios proyectos comparten stack y convenciones | Un solo proyecto, sin previsión de más |
| El `CLAUDE.md` ya pasó de ~200 líneas | Configuración que cabe en una página |
| Hay procedimientos que se repiten entre proyectos | Cada proyecto usa un stack sin nada en común |
| Entran personas nuevas y hay que transferir criterio | Trabajas solo y el criterio no sale de tu cabeza |
| Quieres que las convenciones sobrevivan al proyecto | Prototipo desechable |

## Glosario

| Término | Definición |
|---------|-----------|
| Arquitectura agéntica | organización de la configuración de un asistente de código en piezas separadas por tipo (reglas, skills, agentes, comandos, templates) con un catálogo central reutilizable |
| Catálogo | repositorio que contiene las piezas genéricas, fuente de verdad desde la que se copia a cada proyecto |
| Regla | restricción declarativa y corta que se pega en el `CLAUDE.md` del proyecto y permanece siempre en contexto |
| Skill | procedimiento paso a paso en `skills/<n>/SKILL.md` que el modelo auto-invoca cuando la tarea coincide con su `description` |
| Agente | rol con criterio propio, definido en `agents/<n>.md`, que aplica reglas y skills; puede acotarse con el campo `tools` |
| Slash command | flujo que la persona dispara explícitamente por nombre (`/fix-bug`), a diferencia de la skill que se auto-invoca |
| Frontmatter | bloque YAML al inicio del archivo que declara `name`, `description` y opcionalmente `tools` o `argument-hint` |
| Placeholder | marcador `{{UPPER_SNAKE}}` que representa un valor concreto del proyecto, resuelto al copiar la pieza |
| Diccionario de placeholders | archivo único que define cada placeholder con su significado y ejemplo, para evitar sinónimos divergentes |
| Matriz de selección | tabla que decide qué piezas del catálogo se copian y cuáles se omiten según el stack detectado |
| Disparador | palabra o frase literal incluida en la `description` de una skill para que el modelo la seleccione cuando aparece en la petición |
| Auto-invocación | mecanismo por el que el modelo carga una skill sin que la persona la nombre, comparando la tarea contra las descripciones disponibles |

---

*Rogelio Arriaga Gonzalez*
