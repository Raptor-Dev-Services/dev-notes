# 06 · Conventional Commits

## Problema que resuelve

Los mensajes de commit libres ("fix", "changes", "wip") no permiten automatizar changelogs, generar releases semánticos ni entender el historial sin leer el diff. Conventional Commits define un formato estándar que herramientas como `semantic-release`, `conventional-changelog` y los pipelines de CI pueden procesar.

## Formato

```
<tipo>(<alcance opcional>): <descripción corta>

<cuerpo opcional>

<pie opcional>
```

La primera línea (header) no debe superar 72 caracteres.

## Tipos

| Tipo | Produce | Descripción |
|------|---------|-------------|
| `feat` | MINOR | nueva funcionalidad |
| `fix` | PATCH | corrección de bug |
| `docs` | — | cambios de documentación |
| `style` | — | formato, espacios, sin cambio de lógica |
| `refactor` | — | restructuración de código sin agregar ni corregir |
| `test` | — | agregar o modificar pruebas |
| `chore` | — | tareas de mantenimiento, dependencias, config |
| `perf` | PATCH | mejora de rendimiento |
| `ci` | — | cambios en pipelines de CI/CD |
| `build` | — | cambios en sistema de build o dependencias externas |
| `revert` | — | revertir un commit anterior |

## Breaking changes → MAJOR

Se indica con `!` en el header o con el pie `BREAKING CHANGE:`:

```
feat(api)!: remove v1 endpoints

BREAKING CHANGE: /api/v1 routes are no longer available. Migrate to /api/v2.
```

Cualquier tipo puede tener breaking change. `feat!` o `fix!` incrementa MAJOR.

## Ejemplos

```bash
# feature
git commit -m "feat(auth): add refresh token rotation"

# bug fix
git commit -m "fix(users): handle null email on profile update"

# con cuerpo y referencia a ticket
git commit -m "fix(orders): prevent duplicate invoice generation

Previously, a race condition in the checkout flow could create two invoices
for the same order when the user double-clicked the submit button.

Closes #GTM-421"

# breaking change
git commit -m "feat(api)!: replace userId with tenantUserId in all responses

BREAKING CHANGE: all endpoints that returned userId now return tenantUserId.
Consumers must update their deserialization models."

# chore sin alcance
git commit -m "chore: upgrade xUnit to 2.9.3"

# ci
git commit -m "ci: add SonarCloud quality gate to PR workflow"
```

## Alcance (scope)

El alcance es opcional y describe la parte del sistema afectada:

```
feat(auth): ...
fix(orders): ...
refactor(repository): ...
test(users): ...
```

Usar nombres consistentes en el equipo. En back-template el alcance suele ser el nombre del bounded context o la capa afectada.

## Reglas

- descripción en imperativo presente: "add", "fix", "remove" (no "added", "fixing")
- sin punto al final de la primera línea
- cuerpo separado del header por una línea en blanco
- pie separado del cuerpo por una línea en blanco
- `Closes #NNN` en el pie para cerrar issues automáticamente en GitHub/GitLab

## Herramientas

```bash
# commitlint — valida mensajes en el hook commit-msg
npm install --save-dev @commitlint/cli @commitlint/config-conventional

# commitizen — asistente interactivo para escribir el mensaje
npm install --save-dev commitizen cz-conventional-changelog
```

```json
// package.json
{
  "commitlint": {
    "extends": ["@commitlint/config-conventional"]
  }
}
```

```bash
# .husky/commit-msg
npx --no -- commitlint --edit "$1"
```

## Relación con back-template

El back-template incluye Husky configurado con `commit-msg` hook. `commitlint` valida cada mensaje antes de que el commit se registre. La combinación con `semantic-release` en el pipeline permite generar automáticamente el `CHANGELOG.md` y bumps de versión sin intervención manual.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| proyectos con más de un desarrollador | commits de exploración/prototipo descartable |
| repos que generan changelogs automáticos | cuando el historial nunca se revisa |
| pipelines que derivan versión del historial | ramas locales antes de squash-merge |


> Fuente: *Pro Git 2nd Ed* (Scott Chacon, Ben Straub) — Ch.5 Distributed Git: Contributing to a Project

---

*Rogelio Arriaga Gonzalez*
