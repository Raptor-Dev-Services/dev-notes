<!-- _class: lead -->

# Git  -  Git Flow  -  SemVer  -  Merge Strategies

## Material Extra  -  Dev Notes
### Sistema de mensajería  -  Mayo 2026

---

<!-- _class: lead -->

# Parte 1: Git Fundamentos

---

## Los tres estados de Git

```
Working Directory   ->   Staging Area (Index)   ->   Repository (.git)
      (modified)            (staged)                  (committed)

git add <file>      ----------------------
                    git commit -m "..."    ----------------------
```

| Zona | Qué hay aquí |
|------|-------------|
| Working Directory | archivos que editaste, aún no trackeados para el commit |
| Staging Area | snapshot de lo que irá en el próximo commit |
| Repository | historial permanente de commits |

**Regla:** nunca hagas `git add .` sin revisar primero `git status`.

---

## Comandos esenciales del día a día

```bash
git status                        # ver estado del repo
git diff                          # ver cambios sin stagear
git diff --staged                 # ver cambios ya en staging

git add src/Application/          # stagear carpeta específica
git commit -m "feat(auth): ..."   # commit con mensaje convencional

git log --oneline --graph         # historial visual compacto
git show <sha>                    # ver detalle de un commit

git fetch origin                  # bajar cambios sin aplicar
git pull origin develop           # fetch + merge
git push origin feature/X         # publicar rama
```

---

## Branches y HEAD

```bash
git branch                        # listar ramas locales
git branch -a                     # incluir remotas
git checkout -b feature/whatsapp-channel  # crear y cambiar
git checkout develop              # cambiar a rama existente
git branch -d feature/whatsapp-channel    # borrar (tras merge)
```

```
HEAD -> (apunta a la rama actual)
         ↓
      develop -- commit A -- commit B -- commit C (HEAD)
```

**HEAD detached:** ocurre cuando haces `git checkout <sha>` o `git checkout <tag>`.
Siempre crea una rama antes de trabajar: `git checkout -b hotfix/1.2.1-fix-crash v1.2.0`

---

## merge vs rebase  -  concepto

```
Situación inicial:
develop: -- A -- B -- C
                  \
feature:           D -- E

merge --no-ff:              rebase:
develop: -- A -- B -- C -- M    develop: -- A -- B -- C
                  \       /                              \
feature:           D -- E                       feature:  D' -- E'
```

| | merge --no-ff | rebase |
|-|--------------|--------|
| Historial | bifurcado, preserva contexto | lineal, limpio |
| Reescribe SHAs | No | **Sí** |
| Ramas compartidas | Seguro | **Peligroso** |

---

## merge: el grafo real de commits

```sim git
paso: commit
paso: branch feature
paso: checkout feature
paso: commit
paso: commit
paso: checkout main
paso: commit
paso: merge feature
```

**Qué hacer:** avanza con **Siguiente**, un paso a la vez. Es la situación de la lámina anterior (la rama principal aquí se llama `main`; allá, `develop`): `feature` sale del segundo commit (`C1`; `C0` es el commit inicial que la simulación crea sola) con dos commits propios y la rama principal avanza por su lado. Cuando las dos historias se separaron, el merge crea un commit nuevo con **dos padres**.

> Los identificadores son `C0`, `C1`... en lugar de los hashes reales; el grafo (padres y ramas) se calcula como lo hace Git.

---

## rebase: otros SHAs, y luego un avance rápido

```sim git
paso: commit
paso: branch feature
paso: checkout feature
paso: commit
paso: commit
paso: checkout main
paso: commit
paso: checkout feature
paso: rebase main
paso: checkout main
paso: merge feature
```

**Qué hacer:** compara con la lámina anterior. Tras `rebase main`, los commits de `feature` se reaplican sobre la punta de `main` con identificadores nuevos (los `D'` y `E'` del esquema) y los originales quedan sin rama. El merge final ya no necesita un commit nuevo: es un **avance rápido** (*fast-forward*) y la historia queda lineal.

> Por eso la regla de oro: reescribir ids de commits que otros ya tienen es lo que rompe su historia. El rebase de la simulación no reaplica commits de merge.

---

<!-- _class: lead -->

# Parte 2: Git Flow

---

## Las 6 ramas de Git Flow

| Rama | Tipo | Vida | Propósito |
|------|------|------|-----------|
| `main` | permanente | indefinida | código en producción |
| `develop` | permanente | indefinida | integración de features |
| `feature/*` | temporal | hasta merge | una funcionalidad o ticket |
| `bugfix/*` | temporal | hasta merge | bug en develop |
| `release/*` | temporal | hasta release | bump de versión + fixes menores |
| `hotfix/*` | temporal | hasta merge | corrección urgente de producción |

**Regla de oro:** `main` solo recibe merges de `release/*` y `hotfix/*`.
Nunca hacer push directo a `main` ni a `develop`.

---

## Convención de nombres de ramas

```bash
# Patrón: tipo/ticket-descripcion-corta
feature/PROY-101-whatsapp-channel
feature/PROY-215-contact-import-csv
bugfix/PROY-312-fix-email-validation
release/1.3.0
hotfix/1.2.1-login-null-token
```

Reglas:
- minúsculas, guiones como separador
- incluir número de ticket cuando existe
- sin espacios ni caracteres especiales

**Error común:** crear `feature/WhatsApp_Channel` con mayúsculas y guión bajo  -  rompe scripts de CI/CD que usan el nombre de rama.

---

## Flujo completo: feature -> develop

```bash
# 1. partir SIEMPRE desde develop actualizado
git checkout develop
git pull origin develop

# 2. crear la feature branch
git checkout -b feature/PROY-101-whatsapp-channel

# 3. trabajar y commitear
git commit -m "feat(channels): add WhatsApp webhook handler"
git commit -m "feat(channels): validate HMAC signature"

# 4. merge a develop cuando está lista (vía PR o local)
git checkout develop
git merge --no-ff feature/PROY-101-whatsapp-channel
git push origin develop
git branch -d feature/PROY-101-whatsapp-channel
```

---

## Flujo completo: release

```bash
# 1. crear release desde develop
git checkout develop
git checkout -b release/1.3.0

# 2. solo fixes menores + bump de versión
git commit -m "chore(release): bump version to 1.3.0"
git commit -m "fix(docs): update API changelog for v1.3.0"

# 3. merge a main + tag
git checkout main
git merge --no-ff release/1.3.0
git tag -a v1.3.0 -m "Release 1.3.0"
git push origin main
git push origin v1.3.0

# 4. merge también a develop (para no perder los fixes)
git checkout develop
git merge --no-ff release/1.3.0
git push origin develop
git branch -d release/1.3.0
```

---

## Flujo completo: hotfix

```bash
# 1. partir desde main (el código en producción)
git checkout main
git pull origin main
git checkout -b hotfix/1.2.1-login-null-token

# 2. corregir el bug
git commit -m "fix(auth): handle null token on refresh"

# 3. merge a main + tag
git checkout main
git merge --no-ff hotfix/1.2.1-login-null-token
git tag -a v1.2.1 -m "Hotfix 1.2.1  -  null token on refresh"
git push origin main
git push origin v1.2.1

# 4. merge también a develop (¡no olvidar!)
git checkout develop
git merge --no-ff hotfix/1.2.1-login-null-token
git push origin develop
git branch -d hotfix/1.2.1-login-null-token
```

**Error más común:** olvidar el merge del hotfix a `develop`. El bug regresa en la siguiente release.

---

<!-- _class: lead -->

# Parte 3: Commit Conventions

---

## Formato Conventional Commits

```
<tipo>(<alcance>): <descripción corta>

<cuerpo opcional>

<pie opcional>
```

| Tipo | Impacto SemVer | Cuándo usar |
|------|---------------|-------------|
| `feat` | MINOR | nueva funcionalidad |
| `fix` | PATCH | corrección de bug |
| `refactor` |  -  | reestructurar sin cambiar comportamiento |
| `test` |  -  | agregar o modificar pruebas |
| `docs` |  -  | documentación |
| `chore` |  -  | dependencias, config, mantenimiento |
| `ci` |  -  | pipelines de CI/CD |

---

## Breaking change y ejemplos reales

```bash
# feature nueva en el módulo de canales
git commit -m "feat(channels): add Instagram webhook integration"

# bug fix con referencia a ticket
git commit -m "fix(auth): prevent token refresh loop on 401

Previously, the HTTP interceptor was retrying indefinitely.
Now it limits retries to 1 and redirects to login.

Closes #PROY-421"

# breaking change: ! en el header + BREAKING CHANGE en pie
git commit -m "feat(api)!: replace agentId with workspaceUserId in responses

BREAKING CHANGE: all endpoints that returned agentId now return
workspaceUserId. Update client deserialization models."
```

**Regla:** descripción en imperativo presente  -  "add", "fix", "remove" (no "added" ni "fixing").

---

<!-- _class: lead -->

# Parte 4: SemVer

---

## MAJOR.MINOR.PATCH  -  el contrato

```
MAJOR.MINOR.PATCH
  2   . 4  .  0
```

| Segmento | Incrementa cuando | Reset al incrementar |
|----------|-------------------|----------------------|
| MAJOR | breaking change  -  rompe compatibilidad | MINOR y PATCH -> 0 |
| MINOR | nueva funcionalidad compatible | PATCH -> 0 |
| PATCH | corrección de bug compatible | ninguno |

```
2.3.5 + corrección de bug   ->  2.3.6
2.3.5 + nueva feature       ->  2.4.0   (PATCH se resetea)
2.3.5 + breaking change     ->  3.0.0   (MINOR y PATCH se resetean)
```

**Error clásico:** v2.3.5 con nueva feature -> v2.3.6. INCORRECTO. Es v2.4.0.

---

## Fase 0.x.y y pre-releases

```
0.x.y   -  API inestable, cualquier versión puede romper
1.0.0   -  primera API pública estable
```

```
# Ciclo de release de una feature grande:
1.5.0-alpha    -> desarrollo interno, inestable
1.5.0-beta     -> pruebas externas, funcionalidad completa
1.5.0-rc.1     -> release candidate, listo para producción
1.5.0          -> release oficial
```

**Precedencia:** `1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-beta < 1.0.0-rc.1 < 1.0.0`

Para el sistema de mensajería:
- Inicio del curso: `0.1.0`
- Primera entrega funcional (auth + workspace): `1.0.0`
- Feature WhatsApp: `1.1.0`
- Hotfix en producción: `1.1.1`

---

<!-- _class: lead -->

# Parte 5: Merge Strategies

---

## Las cuatro estrategias

| Estrategia | Comando | Historial | Cuándo usar |
|-----------|---------|-----------|-------------|
| Fast-forward | `git merge` (default) | lineal, sin merge commit | ramas locales de un solo dev |
| Merge commit | `git merge --no-ff` | bifurcado, merge commit explícito | Git Flow  -  todas las integraciones |
| Squash | `git merge --squash` | un commit limpio por feature | limpiar historial de trabajo sucio |
| Rebase | `git rebase <rama>` | lineal, sin merge commits | actualizar feature local antes de PR |

---

## Merge --no-ff (recomendado en Git Flow)

```bash
git checkout develop
git merge --no-ff feature/PROY-101-whatsapp-channel
git push origin develop
```

```
*   a4f3c21 Merge branch 'feature/PROY-101-whatsapp-channel' into develop
|\
| * 9e2b1a0 feat(channels): validate HMAC signature
| * 3c7d4f1 feat(channels): add WhatsApp webhook handler
|/
* 8b1a3d2 chore: previous develop commit
```

Ventaja clave: revertir toda la feature con un solo comando:
```bash
git revert -m 1 a4f3c21   # -m 1 = primer padre (develop)
```

Configurar como default del repo:
```bash
git config --global merge.ff false
```

---

## Squash merge  -  historial limpio

```bash
git checkout develop
git merge --squash feature/PROY-456-contact-import
git commit -m "feat(contacts): add CSV import with tag assignment"
git push origin develop
git branch -d feature/PROY-456-contact-import
```

```
* d8a1c30 feat(contacts): add CSV import with tag assignment   <- único commit
* 8b1a3d2 chore: previous develop commit
```

**Cuándo usarlo:** la rama tiene commits "wip", "fix typo", "try again" que no aportan al historial.
**Cuándo NO:** cuando necesitas trazabilidad commit a commit (ej. auditorías).

---

## Rebase  -  regla de oro

```bash
# actualizar feature local con los cambios de develop
git checkout feature/PROY-789-bot-flow
git rebase develop
```

```
Antes del rebase:          Después del rebase:
develop: A -- B -- C       develop: A -- B -- C
              \                                  \
feature:       D -- E      feature:               D' -- E'  (SHAs nuevos)
```

**Regla de oro:** nunca hacer rebase de una rama que ya está publicada en remoto y que otros están usando. Reescribe los SHAs y rompe el historial de los demás.

**Uso seguro:** rebase local de tu feature antes de abrir la PR, para que el historial quede limpio sobre la punta de develop.

---

## Estrategia por tipo de merge (Git Flow)

| Merge | Estrategia | Comando |
|-------|-----------|---------|
| `feature/*` -> `develop` | --no-ff | `git merge --no-ff feature/X` |
| `bugfix/*` -> `develop` | --no-ff | `git merge --no-ff bugfix/X` |
| `release/*` -> `main` | --no-ff | `git merge --no-ff release/X` |
| `release/*` -> `develop` | --no-ff | `git merge --no-ff release/X` |
| `hotfix/*` -> `main` | --no-ff | `git merge --no-ff hotfix/X` |
| `hotfix/*` -> `develop` | --no-ff | `git merge --no-ff hotfix/X` |
| feature local <- develop | rebase | `git rebase develop` (local) |

---

<!-- _class: lead -->

# Parte 6: Tags y Release Workflow

---

## Tags anotados vs ligeros

| Tipo | Comando | Metadatos | Uso |
|------|---------|-----------|-----|
| Ligero | `git tag v1.0.0` | ninguno | marcadores temporales, locales |
| Anotado | `git tag -a v1.0.0 -m "msg"` | autor, fecha, mensaje | **releases oficiales** |

```bash
# tag anotado en HEAD (después del merge a main)
git tag -a v1.3.0 -m "Release 1.3.0  -  WhatsApp + Instagram channels"

# push del tag (¡git push NO envía tags por defecto!)
git push origin v1.3.0
# o push de todos los tags anotados:
git push origin --follow-tags
```

**Error clásico:** hacer el merge a main, push a main, y olvidar el `git push origin v1.3.0`. El pipeline de CD no se dispara.

---

## Ciclo completo de release con tag

```bash
# 1. finalizar release branch
git checkout main
git merge --no-ff release/1.3.0

# 2. crear tag anotado
git tag -a v1.3.0 -m "Release 1.3.0"

# 3. push del merge Y el tag
git push origin main
git push origin v1.3.0

# 4. limpiar rama release local y remota
git branch -d release/1.3.0
git push origin --delete release/1.3.0

# 5. ver el historial con tags
git log --oneline --decorate --graph
```

---

## Convención de nombres de tags

```
v{MAJOR}.{MINOR}.{PATCH}
v{MAJOR}.{MINOR}.{PATCH}-{pre-release}

v1.0.0          <- primera release estable
v1.3.0          <- feature release
v1.3.1          <- patch (hotfix)
v2.0.0-beta.1   <- pre-release pública
v2.0.0-rc.1     <- release candidate
v2.0.0          <- release mayor con breaking changes
```

El prefijo `v` es convención en Git; los archivos `.csproj` o `package.json` no lo incluyen:

```xml
<!-- Sistema de mensajería  -  {{ROOT_NAMESPACE}}.Admin.WebClient.csproj -->
<Version>1.3.0</Version>
```

---

## Errores más comunes  -  resumen

| Error | Consecuencia | Solución |
|-------|-------------|----------|
| `git push --force` a `main` | destruye historial del equipo | nunca; protect branch en GitHub |
| `:latest` en Docker en lugar de tag semver | no se puede hacer rollback exacto | siempre `:v1.3.0` |
| Olvidar merge hotfix a `develop` | el bug regresa en el siguiente release | checklist de hotfix |
| Rebase de rama publicada | rompe el repo de los demás | rebase solo local |
| fast-forward en ramas principales | se pierde la trazabilidad de features | `git merge --no-ff` siempre |
| `feat` nueva pero bump a PATCH | versión comunica mentiras a consumidores | feat -> MINOR bump |
| Olvidar `git push origin v1.x.x` | pipeline de CD no se dispara | push tags explícito |
