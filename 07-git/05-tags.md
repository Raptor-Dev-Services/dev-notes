# 05 · Tags de Git

## Problema que resuelve

Las ramas son punteros móviles: su HEAD avanza con cada commit. Para marcar un punto exacto en la historia (una versión en producción, un release candidate) se necesitan referencias inmutables. Los tags cumplen esa función.

## Tipos de tags

| Tipo | Comando | Metadatos | Uso recomendado |
|------|---------|-----------|-----------------|
| Ligero (lightweight) | `git tag v1.0.0` | ninguno | marcadores temporales, uso local |
| Anotado (annotated) | `git tag -a v1.0.0 -m "msg"` | autor, fecha, mensaje, firma GPG opcional | releases oficiales |

Los tags anotados son objetos Git completos; los ligeros son solo un alias al commit. Para releases de producción siempre usar anotados.

## Crear tags

```bash
# tag anotado en el commit actual (HEAD)
git tag -a v1.3.0 -m "Release 1.3.0 — user auth, rate limiting"

# tag anotado en un commit específico
git tag -a v1.2.1 9fceb02 -m "Hotfix 1.2.1"

# tag ligero
git tag v1.3.0-draft
```

## Listar y ver tags

```bash
# listar todos los tags
git tag

# filtrar por patrón
git tag -l "v1.*"

# ver detalles del tag anotado
git show v1.3.0
```

## Publicar tags

```bash
# push de un tag específico
git push origin v1.3.0

# push de todos los tags locales
git push origin --tags

# push de tags anotados solamente
git push origin --follow-tags
```

`git push` no envía tags por defecto. Siempre hacer push explícito del tag al crear un release.

## Eliminar tags

```bash
# eliminar local
git tag -d v1.3.0-draft

# eliminar en remoto
git push origin --delete v1.3.0-draft
# o equivalente:
git push origin :refs/tags/v1.3.0-draft
```

## Convención de nombres con SemVer

```
v{MAJOR}.{MINOR}.{PATCH}
v{MAJOR}.{MINOR}.{PATCH}-{pre-release}

v1.0.0
v1.3.0
v2.0.0-beta.1
v2.0.0-rc.1
v2.0.0
```

El prefijo `v` es convención amplia en Git; los archivos `.csproj` o `package.json` no lo incluyen.

## Checkout de un tag

```bash
# ver el código en el estado exacto del tag
git checkout v1.2.0
```

Esto pone el repo en "detached HEAD". Para hacer trabajo desde ese punto:

```bash
git checkout -b hotfix/1.2.1-fix-crash v1.2.0
```

## Ciclo de vida de un release

```bash
# 1. finalizar rama release
git checkout main
git merge --no-ff release/1.3.0

# 2. crear tag anotado
git tag -a v1.3.0 -m "Release 1.3.0"

# 3. push del merge y el tag
git push origin main
git push origin v1.3.0

# 4. limpiar rama release
git branch -d release/1.3.0
git push origin --delete release/1.3.0
```

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| marcar cada versión que llega a producción | marcar commits intermedios de desarrollo |
| referenciar el punto exacto de un hotfix | reemplazar el uso de ramas para trabajo en progreso |
| disparar pipelines de CD por evento de tag | crear tags anotados para cambios de configuración que no son releases |


> Fuente: *Pro Git 2nd Ed* (Scott Chacon, Ben Straub) — Ch.2 Git Basics: Tagging

---

## Glosario

| Término | Definición |
|---------|-----------|
| Tag | Referencia inmutable en Git que apunta a un commit específico, usada para marcar versiones de release |
| Tag anotado | Tag que es un objeto Git completo con autor, fecha, mensaje y opcionalmente firma GPG; recomendado para releases |
| Tag ligero | Alias simple a un commit sin metadatos adicionales; adecuado para marcadores temporales de uso local |
| --follow-tags | Opción de `git push` que envía al remoto solo los tags anotados que apuntan a commits del push actual |
| Detached HEAD | Estado de Git donde el puntero HEAD apunta a un commit directamente en lugar de a una rama |
| SemVer tag | Tag de Git con prefijo `v` que sigue el formato `vMAJOR.MINOR.PATCH` (ej. `v1.3.0`) |
| git tag -a | Comando para crear un tag anotado especificando el mensaje con `-m` |
| git show | Comando que muestra los metadatos de un tag anotado y los cambios del commit al que apunta |
| Release | Versión de software marcada con un tag anotado que puede disparar el pipeline de CD |
| Hotfix tag | Tag creado desde una rama `hotfix/*` que incrementa el PATCH de la versión (ej. `v1.2.1`) |

---

*Rogelio Arriaga Gonzalez*
