# 08 · Estrategias de rollback

## Problema que resuelve

Cuando una versión en producción tiene un defecto crítico hay dos presiones simultáneas: restaurar el servicio lo antes posible y no destruir el historial de Git. Las estrategias de rollback se dividen según el objetivo: volver el sistema al estado anterior (operacional) o corregir y avanzar (Git history).

## Estrategia A — Redespliegue del tag anterior (recomendada)

La forma más rápida de restaurar producción es redesplegar la versión que funcionaba. No toca Git en absoluto.

```bash
# identificar el último tag estable
git tag -l "v*" --sort=-version:refname | head -5

# el pipeline de CD recibe el tag como parámetro
# en GitHub Actions / Azure DevOps: disparar workflow con tag específico

# verificar qué commit está en ese tag
git show v1.2.0 --stat
```

**Cuándo:** incidente en producción, se necesita restaurar en minutos.

## Estrategia B — git revert (historial limpio)

Crea un nuevo commit que deshace los cambios de un commit anterior. No reescribe historial; es la forma segura en ramas compartidas.

```bash
# revertir un commit específico
git revert <sha>
git push origin main

# revertir un merge commit (--no-ff)
git revert -m 1 <merge-commit-sha>
git push origin main
```

El `-m 1` indica que se quiere revertir al primer padre del merge (la rama destino). El resultado es un nuevo commit que deshace todo lo que entró con ese merge.

**Cuándo:** el bug llegó a `main`, se quiere dejar evidencia en el historial de que hubo una reversión.

## Estrategia C — git reset (local, antes de push)

Solo válido cuando el commit problemático no se ha publicado en remoto.

```bash
# deshacer el último commit, mantener cambios en staging
git reset --soft HEAD~1

# deshacer el último commit, mantener cambios sin staging
git reset HEAD~1

# deshacer el último commit y descartar cambios (destructivo)
git reset --hard HEAD~1
```

**Cuándo:** el commit local está mal, todavía no se ha hecho push.

## Estrategia D — Hotfix desde el tag anterior

Cuando el código en `main` está roto y se necesita una corrección urgente:

```bash
# crear rama hotfix desde el último tag estable
git checkout -b hotfix/1.2.1-fix-crash v1.2.0

# aplicar la corrección
git commit -m "fix(auth): handle null token on session resume"

# merge a main con tag nuevo
git checkout main
git merge --no-ff hotfix/1.2.1-fix-crash
git tag -a v1.2.1 -m "Hotfix 1.2.1 — fix auth null token"
git push origin main --tags

# propagar a develop
git checkout develop
git merge --no-ff hotfix/1.2.1-fix-crash
git push origin develop

git branch -d hotfix/1.2.1-fix-crash
```

## Comparación

| Estrategia | Velocidad | Toca historial | Código resultante |
|-----------|-----------|----------------|-------------------|
| A — redespliegue de tag | muy rápida | no | versión anterior en prod |
| B — git revert | media | agrega commit | avanza con reversión |
| C — git reset | rápida | reescribe | solo local, antes de push |
| D — hotfix desde tag | lenta (ciclo completo) | agrega commits | versión corregida en prod |

## Checklist de incidente — decisión de rollback

**A. Detección**
- [ ] identificar el síntoma exacto (error 500, timeout, datos corruptos)
- [ ] confirmar que el problema empezó con el último deploy
- [ ] evaluar el impacto en usuarios activos

**B. Decisión**
- [ ] ¿se puede revertir con un deploy del tag anterior? → estrategia A
- [ ] ¿el bug está en el código y hay fix en minutos? → ir directo a D
- [ ] ¿es un problema de datos o de infra, no de código? → no hacer rollback de Git

**C. Rollback operacional (estrategia A)**
- [ ] identificar el tag de la versión estable anterior
- [ ] disparar el pipeline de CD apuntando a ese tag
- [ ] verificar que el servicio responde correctamente
- [ ] comunicar al equipo que se está en la versión anterior

**D. Preservar evidencia**
- [ ] capturar logs del incidente antes de que roten
- [ ] documentar el SHA del commit problemático
- [ ] anotar hora de detección, hora de rollback, usuarios afectados

**E. Corrección**
- [ ] crear rama `hotfix/` desde el tag estable (estrategia D) o
- [ ] aplicar `git revert` en `main` (estrategia B)
- [ ] verificar en staging antes de hacer push
- [ ] merge a `main` con nuevo tag, push

**F. Post-deploy**
- [ ] monitorear métricas por 30 minutos mínimo
- [ ] confirmar que el problema original no reaparece
- [ ] propagar el fix a `develop`

**G. Retrospectiva**
- [ ] documentar causa raíz
- [ ] identificar qué prueba hubiera detectado el bug antes
- [ ] actualizar checklist de deploy si aplica

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| estrategia A para restaurar servicio inmediatamente | `git reset --hard` en ramas remotas compartidas |
| `git revert` cuando el commit ya está en `main` | `git push --force` en `main` para deshacer |
| hotfix flow cuando se puede tolerar 30-60 min de proceso | redespliegue si el problema es de datos y no de código |


> Fuente: *Learning Git* (Anna Skoulikari) — Ch.9 Undoing Changes

---

*Rogelio Arriaga Gonzalez*
