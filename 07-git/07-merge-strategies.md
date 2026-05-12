# 07 · Estrategias de merge

## Problema que resuelve

Git ofrece varias formas de integrar ramas. La elección afecta la legibilidad del historial, la capacidad de revertir cambios y la trazabilidad entre commits y features. Sin una convención de equipo, el historial queda inconsistente.

## Las tres estrategias principales

| Estrategia | Comando | Historial | Revertir |
|-----------|---------|-----------|---------|
| Merge con commit | `git merge --no-ff` | preserva ramas con merge commit | revertir el merge commit |
| Squash merge | `git merge --squash` | un commit limpio por feature | revertir ese único commit |
| Rebase | `git rebase` | historia lineal, sin merge commits | reescribe SHAs — difícil en compartido |

## Merge --no-ff (recomendado en Git Flow)

Fast-forward fusiona commits directamente en la rama destino, sin crear un merge commit. Con `--no-ff` se fuerza la creación del merge commit aunque no sea necesario.

```bash
git checkout develop
git merge --no-ff feature/GTM-123-user-auth
git push origin develop
```

El historial queda:

```
*   a4f3c21 Merge branch 'feature/GTM-123-user-auth' into develop
|\
| * 9e2b1a0 feat(auth): validate JWT expiry
| * 3c7d4f1 feat(auth): add token refresh endpoint
|/
* 8b1a3d2 previous develop commit
```

Ventajas:
- se puede revertir toda la feature con `git revert -m 1 <merge-commit-sha>`
- el historial muestra exactamente qué commits pertenecen a cada feature
- trazabilidad completa feature → commits → código

Configurar como default:

```bash
git config --global merge.ff false
```

## Squash merge

Aplana todos los commits de la rama en uno solo antes de integrar. Útil cuando la rama tiene commits intermedios de trabajo sucio ("wip", "fix typo") que no se quieren en el historial principal.

```bash
git checkout develop
git merge --squash feature/GTM-456-cleanup
git commit -m "feat(users): add bulk delete endpoint"
git push origin develop
```

El historial queda:

```
* d8a1c30 feat(users): add bulk delete endpoint   ← único commit
* 8b1a3d2 previous develop commit
```

La rama original sigue existiendo con su historial completo hasta que se borre.

```bash
git branch -d feature/GTM-456-cleanup
```

## Rebase

Reescribe los commits de la rama actual aplicándolos sobre la punta de la rama destino. Produce historial lineal sin merge commits.

```bash
# desde la feature branch, ponerse al día con develop
git checkout feature/GTM-789-reports
git rebase develop
```

```
Antes:                    Después:
                          * c1f2a3b feat: add export to PDF    (nuevo SHA)
* 9e2b1a0 feat: reports   * 7b3d4e1 feat: add bar chart        (nuevo SHA)
* 3c7d4f1 feat: chart      |
|                develop --+
* 8b1a3d2 develop base
```

**Regla de oro:** nunca hacer rebase de ramas que ya están publicadas en remoto y que otros están usando. Reescribe los SHAs y rompe el historial de los demás.

### Rebase interactivo

Reordenar, unir o editar commits antes de un merge:

```bash
git rebase -i HEAD~4   # editar los últimos 4 commits
```

Comandos disponibles en el editor:

| Comando | Acción |
|---------|--------|
| `pick` | mantener el commit tal cual |
| `reword` | cambiar el mensaje del commit |
| `edit` | pausar para modificar el commit |
| `squash` | combinar con el commit anterior, editar mensaje |
| `fixup` | combinar con el anterior, descartar este mensaje |
| `drop` | eliminar el commit |

## Estrategia recomendada por tipo de rama (Git Flow)

| Merge | Estrategia |
|-------|-----------|
| `feature/*` → `develop` | `--no-ff` |
| `bugfix/*` → `develop` | `--no-ff` |
| `release/*` → `main` | `--no-ff` |
| `release/*` → `develop` | `--no-ff` |
| `hotfix/*` → `main` | `--no-ff` |
| `hotfix/*` → `develop` | `--no-ff` |
| actualizar feature con develop | `rebase` (local, antes de publicar) |

## Revertir un merge

```bash
# revertir merge commit: -m 1 indica el primer padre (la rama destino)
git revert -m 1 a4f3c21
git push origin develop
```

Esto crea un nuevo commit que deshace los cambios del merge, sin reescribir historial.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| `--no-ff` para todos los merges de feature/hotfix/release | fast-forward en ramas principales |
| squash para limpiar historial de ramas con commits de trabajo | squash cuando se necesita trazabilidad commit a commit |
| rebase interactivo en ramas locales antes de PR | rebase en ramas ya publicadas en remoto con colaboradores |

---

## Cherry-pick — aplicar un commit específico
> Fuente: *Pro Git* (Chacon, Straub) — Ch.7 Git Tools

`cherry-pick` copia un commit de una rama a otra sin hacer merge de toda la rama.

```bash
# Ver el SHA del commit que quieres copiar
git log --oneline develop

# Aplicar ese commit en la rama actual
git cherry-pick 9e2b1a0

# Cherry-pick sin hacer commit automático (para inspeccionar antes)
git cherry-pick --no-commit 9e2b1a0

# Múltiples commits
git cherry-pick 9e2b1a0 3c7d4f1

# Rango de commits
git cherry-pick 3c7d4f1..9e2b1a0
```

```
Caso de uso: un fix está en develop pero necesitas también en main (sin hacer release completa)

develop: ─── A ─── B ─── C (fix crítico) ─── D ───
                                ↓ cherry-pick
main:    ─────────────────── C' ───────────────────
```

**Cuándo usar cherry-pick:**
- Un fix crítico de develop que necesitas en main/hotfix sin esperar la release
- Backport: llevar un fix de la versión 2.0 a la rama de mantenimiento 1.x
- Un commit que se hizo en la rama equivocada

**Cuándo NO usar cherry-pick:**
- No usarlo para integrar features completas → usa merge
- Si el commit depende de commits anteriores que no están en la rama destino → conflictos

---

## git bisect — encontrar el commit que introdujo un bug

```bash
# Iniciar la búsqueda binaria
git bisect start

# Marcar el estado actual como "malo" (tiene el bug)
git bisect bad

# Marcar un commit anterior conocido como "bueno" (sin el bug)
git bisect good v1.2.0

# Git mueve automáticamente al commit intermedio
# Prueba si el bug existe y marca:
git bisect good    # si este commit no tiene el bug
git bisect bad     # si este commit tiene el bug

# Git va dividiendo en mitades hasta encontrar el primer commit malo
# Al terminar, muestra: "a1b2c3d is the first bad commit"

# Terminar la búsqueda
git bisect reset
```

```bash
# Automatizar con un script que retorna 0 (bueno) o 1 (malo)
git bisect start HEAD v1.2.0
git bisect run dotnet test --filter "FeatureName=UserAuth"
# git bisect ejecuta el script en cada commit y encuentra el culpable automáticamente
```


---

*Rogelio Arriaga Gonzalez*
