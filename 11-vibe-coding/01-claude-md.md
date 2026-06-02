# 01 — CLAUDE.md y AGENTS.md — Instrucciones para LLMs como Código

`CLAUDE.md` es el mecanismo de Claude Code para incluir instrucciones persistentes en el repositorio. Es equivalente a un README para el LLM: describe el proyecto, las convenciones, las restricciones y el contexto que el agente necesita para trabajar bien sin que el desarrollador tenga que repetirlo en cada sesión.

> Fuente: Documentación oficial Claude Code — https://docs.anthropic.com/en/docs/claude-code/overview

---

## El problema que resuelve

```text
❌ Sin CLAUDE.md — instrucciones repetidas en cada sesión
Dev: "Usa siempre nombres ExampleUser, ExampleUserRepository,
      escribe en español, sin Co-Authored-By en commits,
      la arquitectura es Clean Architecture con Dapper..."
Claude: [5 minutos después del resumen de contexto] lo repite mal

❌ Sin CLAUDE.md — el LLM no conoce el estilo del proyecto
Claude: genera código con nombres genéricos (User, UserRepo)
        usa emojis en los docs
        incluye comentarios de "lo que hace el código"
        olvida la sección de Glosario
```

```text
✓ Con CLAUDE.md — el contexto está en el repo, no en la sesión
Claude: lee el archivo al iniciar → trabaja con todas las convenciones
        ya aplicadas desde el primer mensaje
```

---

## Estructura recomendada de CLAUDE.md

```markdown
# CLAUDE.md — [nombre del proyecto]

## Qué es este repositorio
[1-2 párrafos. Stack, propósito, audiencia.]

## Reglas de contenido (obligatorias)
- Idioma: español / inglés
- Sin emojis / con emojis
- Formato de código: nombres de entidades, convenciones de nomenclatura

## Arquitectura de referencia
[Descripción de capas, flujo de datos, patrones usados]
[Diagrama ASCII si el proyecto lo justifica]

## Nombres de ejemplo estándar
[Tabla o lista de los nombres de ejemplo que el LLM debe usar en el código]

## Estructura de directorios
[Árbol del repo con qué hay en cada carpeta]

## Lo que NO va en este repo
[Binarios, secretos, código de producción, PDFs, etc.]

## Estado del repo
[Archivos pendientes de commit, problemas conocidos, freeze activo]
```

---

## Dónde vive CLAUDE.md

Claude Code carga automáticamente los archivos en este orden (de mayor a menor prioridad):

```
~/.claude/CLAUDE.md          ← global — aplica a todos los proyectos
~/projects/repo/CLAUDE.md    ← raíz del repo — contexto del proyecto
src/components/CLAUDE.md     ← subdirectorio — instrucciones específicas de ese módulo
```

Los archivos de subdirectorio se cargan cuando Claude Code trabaja en ese directorio — útil para instrucciones específicas de frontend vs backend dentro del mismo monorepo.

```markdown
# src/components/CLAUDE.md — ejemplo de instrucciones de subdirectorio
Componentes en este directorio usan Tailwind v4 con CSS variables.
No usar `className` directo — usar el sistema de tokens del design system.
Los nombres siguen PascalCase y deben tener un story en Storybook.
```

---

## AGENTS.md — instrucciones para agentes paralelos

`AGENTS.md` es el equivalente de `CLAUDE.md` para herramientas de agentes que corren en paralelo (Devin, SWE-agent, agentes custom con el Claude API). Claude Code también lo respeta.

```markdown
# AGENTS.md

## Reglas para agentes

### Archivos prohibidos (nunca modificar)
- CLAUDE.md, AGENTS.md, TODO.md  ← instrucciones del proyecto
- Lib/, Scripts/                  ← venv accidental, ignorar
- *.env, secrets.*                ← nunca tocar archivos de secretos

### Archivos a ignorar en commits
- CLAUDE.md, CONTEXT.md, TODO.md, .claude/, Lib/, Scripts/
- Co-Authored-By de Claude — este proyecto se monetiza, no incluir

### Convenciones de código
- Idioma: español en toda la documentación
- Nombres estándar: ExampleUser, IExampleUserRepository, ExampleUsersSql
- Sin emojis en docs (solo ✓/✗ en tablas y código)

### Estructura de cada documento
1. Párrafo de qué problema resuelve
2. Bloque ❌ → bloque ✓
3. Código con nombres estándar
4. Relación con el back-template
5. Cuándo usar / no usar
6. Glosario con todos los términos del doc

### Límites del agente
- Solo modificar archivos en los directorios asignados
- No crear archivos en la raíz del repo
- No ejecutar comandos destructivos (rm -rf, git reset --hard)
```

---

## Buenas prácticas para CLAUDE.md efectivo

### ✓ Qué incluir

```markdown
# Efectivo — concreto y accionable

## Nombres de entidades estándar
Usa siempre estos nombres en ejemplos de código:
- Entidad:          ExampleUser
- Repo interfaz:    IExampleUserRepository
- Handler:          GetExampleUserHandler
- Request:          GetExampleUserRequest(Guid PublicId)

## Restricciones de commit
- Sin Co-Authored-By de Claude (el proyecto se monetiza)
- No commitear: CLAUDE.md, TODO.md, Lib/, Scripts/

## Idioma
Toda la documentación en español.
Los identificadores de código en inglés.
```

```markdown
# Inefectivo — vago y no accionable

## Buenas prácticas
Escribe código limpio y siguiendo las convenciones del proyecto.
Sé consistente con el estilo existente.
```

### ✓ Mantenerlo actualizado

```markdown
## Estado del repo (2026-06-02)

### Archivos sin trackear (pendientes de commit)
- 03-arquitectura/03-hexagonal.md
- 05-bases-de-datos/01-sql-fundamentos.md

### Problema conocido
Lib/ y Scripts/ son un venv de Python creado accidentalmente.
No commitear. Agregar a .gitignore si no están.
```

### ✗ Qué no incluir

- Código de producción o lógica de negocio — va en el código, no en CLAUDE.md
- Información que cambia a diario — Claude.md debe mantenerse, no desactualizarse
- Secretos o credenciales — nunca, aunque sea solo para contexto

---

## Gestión del contexto en sesiones largas

Claude Code compacta el contexto automáticamente cuando la sesión crece. Para que las sesiones largas funcionen bien:

```markdown
# CLAUDE.md — sección de contexto

## Archivos de contexto adicional
Si necesitas ver el estado completo del proyecto, lee:
- `TODO.md` — pendientes actuales con prioridad
- `CONTEXT.md` — resumen del estado del repo (si existe)

## Convenciones de memoria entre sesiones
Las memorias del proyecto se guardan en:
~/.claude/projects/{path}/memory/
```

```bash
# El usuario puede ver el contexto actual del proyecto
cat TODO.md

# Ver memorias guardadas por Claude Code
ls ~/.claude/projects/*/memory/
```

---

## Ejemplo: CLAUDE.md mínimo para un nuevo proyecto

```markdown
# CLAUDE.md — mi-proyecto-api

API REST en .NET 10 con Clean Architecture, PostgreSQL y Dapper.

## Stack
- Backend: ASP.NET Core 10, Dapper, PostgreSQL 17
- Arquitectura: Clean Architecture (Domain / Application / Infrastructure / WebApi)
- Idioma del código: inglés
- Idioma de comentarios y docs: español

## Convenciones
- Entidad de ejemplo: `Product`
- Repo interfaz: `IProductRepository`
- Sin comentarios que explican "qué" hace el código — solo "por qué"
- Sin emojis en ningún archivo

## Commits
- Mensaje en inglés, prefijo convencional: feat, fix, docs, refactor
- Sin Co-Authored-By de Claude

## Lo que NO va en el repo
- Archivos .env con valores reales
- Binarios o artefactos de build
```

---

## Relación con el back-template

El `CLAUDE.md` de este repositorio (`dev-notes`) define:
- Los nombres estándar de ejemplo que aparecen en todo el código de todos los docs
- La estructura fija de cada documento (problema → ❌ → ✓ → código → relación → cuándo usar → glosario)
- Las restricciones de commits (sin Co-Authored-By, no commitear CLAUDE.md/TODO.md/Lib/Scripts)
- El estado del repo con archivos pendientes de commit

El `back-template` tendría su propio `CLAUDE.md` describiendo la arquitectura de módulos, las convenciones de DI, los nombres de servicios, etc.

---

## Cuándo usar / no usar CLAUDE.md

| Usar CLAUDE.md | No usar |
|---------------|---------|
| Proyecto con sesiones de Claude Code recurrentes | Proyecto de un solo uso / PoC descartable |
| Equipo que usa Claude Code en el mismo repo | Proyecto donde Claude nunca trabaja directamente |
| Convenciones no estándar que el LLM no puede inferir del código | Proyectos con convenciones completamente estándar (Rails, Django) |
| Restricciones legales o comerciales (sin Co-Authored-By, idioma, etc.) | |

---

## Glosario

| Término | Definición |
|---------|-----------|
| CLAUDE.md | Archivo de instrucciones persistentes para Claude Code — se carga automáticamente al iniciar una sesión |
| AGENTS.md | Instrucciones para agentes paralelos y herramientas de automatización — Claude Code también lo respeta |
| Contexto | La "memoria" de trabajo de Claude Code — conversación, archivos leídos, historial de herramientas |
| Compactación de contexto | Proceso automático que resume el historial cuando la sesión crece — preserva las instrucciones de CLAUDE.md |
| Prompt persistente | Instrucciones que se re-inyectan en cada sesión sin que el usuario las repita — función de CLAUDE.md |
| Vibe coding | Estilo de desarrollo donde el LLM genera código a partir de intención en lenguaje natural — requiere buenas instrucciones |
| Memory system | Sistema de archivos en `~/.claude/projects/*/memory/` donde Claude Code guarda contexto entre sesiones |
| CONTEXT.md | Archivo opcional que complementa a CLAUDE.md — resumen del estado actual del proyecto, más volátil |
| Subdirectory instructions | CLAUDE.md en un subdirectorio — aplica solo cuando Claude trabaja en ese scope |
| Co-Authored-By | Línea de atribución en commits de Git — en proyectos comerciales se omite para claridad de autoría |
| LLM-as-agent | Uso de un LLM con acceso a herramientas (leer archivos, ejecutar código) — Claude Code es este modelo |
| Tool use | Capacidad del LLM para llamar herramientas externas (Read, Edit, Bash) — base del funcionamiento de Claude Code |

---

*Rogelio Arriaga Gonzalez*
