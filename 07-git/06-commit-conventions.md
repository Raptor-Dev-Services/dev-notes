# 06 · Conventional Commits y semantic-release

Los mensajes de commit libres ("fix", "changes", "wip") no permiten automatizar changelogs, generar releases semánticos ni entender el historial sin leer el diff. Conventional Commits define un formato estándar que herramientas como `semantic-release`, `conventional-changelog` y los pipelines de CI pueden procesar automáticamente para determinar versiones y generar notas de release.

> Fuentes: *Pro Git 2nd Ed* (Scott Chacon, Ben Straub) — Ch.5 Distributed Git; *Learning GitHub Actions* (Brent Laster) — Ch.9 Actions and Security, Ch.12 Advanced Workflows

---

## Formato

```
<tipo>(<alcance opcional>): <descripción corta>

<cuerpo opcional>

<pie opcional>
```

La primera línea (header) no debe superar 72 caracteres.

---

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

---

## Breaking changes → MAJOR

Se indica con `!` en el header o con el pie `BREAKING CHANGE:`:

```
feat(api)!: remove v1 endpoints

BREAKING CHANGE: /api/v1 routes are no longer available. Migrate to /api/v2.
```

Cualquier tipo puede tener breaking change. `feat!` o `fix!` incrementa MAJOR.

---

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

---

## Alcance (scope)

El alcance es opcional y describe la parte del sistema afectada:

```
feat(auth): ...
fix(orders): ...
refactor(repository): ...
test(users): ...
```

Usar nombres consistentes en el equipo. En back-template el alcance suele ser el nombre del bounded context o la capa afectada.

---

## Reglas

- descripción en imperativo presente: "add", "fix", "remove" (no "added", "fixing")
- sin punto al final de la primera línea
- cuerpo separado del header por una línea en blanco
- pie separado del cuerpo por una línea en blanco
- `Closes #NNN` en el pie para cerrar issues automáticamente en GitHub/GitLab

---

## Herramientas — commitlint + Husky

`commitlint` valida el mensaje en el hook `commit-msg` antes de que el commit se registre.

```bash
npm install --save-dev @commitlint/cli @commitlint/config-conventional husky
npx husky init
```

```json
// package.json
{
  "commitlint": {
    "extends": ["@commitlint/config-conventional"],
    "rules": {
      "scope-enum": [2, "always", ["auth", "orders", "users", "tenants", "billing", "api", "db", "ci", "docs"]],
      "header-max-length": [2, "always", 72]
    }
  }
}
```

```bash
# .husky/commit-msg — se ejecuta cuando el desarrollador hace git commit
npx --no -- commitlint --edit "$1"
```

Si el mensaje no cumple el formato, el commit es rechazado antes de registrarse.

---

## Validación en CI — bloquear PRs con mensajes inválidos

Además del hook local, el pipeline valida los commits del PR para que nadie pueda hacer bypass con `--no-verify`.

```yaml
# .github/workflows/commitlint.yml
name: Validate Commit Messages

on:
  pull_request:
    types: [opened, synchronize, reopened]

jobs:
  commitlint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0   # historia completa necesaria para analizar el rango de commits

      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'

      - run: npm ci

      - name: Validar mensajes de commit del PR
        run: |
          npx commitlint \
            --from ${{ github.event.pull_request.base.sha }} \
            --to ${{ github.event.pull_request.head.sha }} \
            --verbose
```

---

## semantic-release — versión y changelog automáticos

`semantic-release` analiza el historial de commits desde el último tag, determina el bump de versión y genera el CHANGELOG automáticamente. El pipeline hace el release sin intervención manual.

### Instalación

```bash
npm install --save-dev semantic-release \
  @semantic-release/changelog \
  @semantic-release/git \
  @semantic-release/github \
  @semantic-release/exec
```

### Configuración

```json
// .releaserc.json (o "release" en package.json)
{
  "branches": ["main"],
  "plugins": [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    [
      "@semantic-release/changelog",
      { "changelogFile": "CHANGELOG.md" }
    ],
    [
      "@semantic-release/exec",
      {
        "prepareCmd": "sed -i 's/\"version\": \".*\"/\"version\": \"${nextRelease.version}\"/' package.json"
      }
    ],
    [
      "@semantic-release/git",
      {
        "assets": ["CHANGELOG.md", "package.json"],
        "message": "chore(release): ${nextRelease.version} [skip ci]"
      }
    ],
    "@semantic-release/github"
  ]
}
```

### Lógica de versión

`semantic-release` sigue estas reglas para determinar el bump:

| Commits desde el último release | Bump |
|--------------------------------|------|
| Solo `fix`, `perf`, `refactor` | PATCH (1.0.0 → 1.0.1) |
| Al menos un `feat` | MINOR (1.0.0 → 1.1.0) |
| Al menos un `BREAKING CHANGE` | MAJOR (1.0.0 → 2.0.0) |
| Solo `chore`, `docs`, `style`, `test`, `ci` | sin release |

### Workflow de CI/CD con semantic-release

```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    branches: [main]

permissions:
  contents: write      # para commitear CHANGELOG.md y crear el tag
  issues: write        # para comentar en issues cerrados
  pull-requests: write # para comentar en PRs

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          persist-credentials: false   # semantic-release usa GH_TOKEN propio
          fetch-depth: 0

      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'

      - run: npm ci

      - name: Build y Tests
        run: |
          npm run build
          npm test

      - name: Release
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          NPM_TOKEN: ${{ secrets.NPM_TOKEN }}   # si se publica a npm
        run: npx semantic-release
```

### CHANGELOG generado automáticamente

Ejemplo de `CHANGELOG.md` generado tras un release:

```markdown
# [2.1.0] (2026-06-15)

### Features

* **auth:** add refresh token rotation ([abc1234])
* **orders:** add bulk export to CSV ([def5678])

### Bug Fixes

* **users:** handle null email on profile update ([ghi9012])
```

---

## Commitizen — asistente interactivo

Para equipos que prefieren un prompt guiado en lugar de escribir el mensaje manualmente:

```bash
npm install --save-dev commitizen cz-conventional-changelog
```

```json
// package.json
{
  "scripts": {
    "commit": "cz"
  },
  "config": {
    "commitizen": {
      "path": "cz-conventional-changelog"
    }
  }
}
```

```bash
# En lugar de git commit -m "..."
npm run commit
# → seleccionar tipo, alcance, descripción con prompts interactivos
```

---

## Relación con back-template

El back-template incluye Husky configurado con `commit-msg` hook. `commitlint` valida cada mensaje antes de que el commit se registre. La combinación con `semantic-release` en el pipeline permite generar automáticamente el `CHANGELOG.md` y bumps de versión sin intervención manual. El commit de release incluye `[skip ci]` en el mensaje para evitar loops en el pipeline.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| proyectos con más de un desarrollador | commits de exploración/prototipo descartable |
| repos que generan changelogs automáticos | cuando el historial nunca se revisa |
| pipelines que derivan versión del historial | ramas locales antes de squash-merge |
| semantic-release en proyectos con releases frecuentes | semantic-release si se versiona a mano o con tags manuales |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Conventional Commits | especificación de formato de mensajes de commit diseñada para ser procesada por herramientas |
| commitlint | herramienta que valida que los mensajes de commit cumplan el formato configurado |
| Husky | herramienta que registra git hooks en el repositorio (commit-msg, pre-commit, pre-push) |
| semantic-release | herramienta que automatiza versión y changelog basándose en los commits desde el último tag |
| CHANGELOG.md | archivo que documenta todos los cambios relevantes de cada versión del proyecto |
| breaking change | cambio que rompe la compatibilidad con la versión anterior — incrementa MAJOR en SemVer |
| `[skip ci]` | texto en el mensaje de commit que indica al pipeline que no ejecute el workflow |
| commit-msg hook | script de git que se ejecuta justo antes de registrar un commit, con acceso al mensaje |
| Commitizen | asistente interactivo para construir mensajes de commit que cumplen la especificación |

---

*Rogelio Arriaga Gonzalez*
