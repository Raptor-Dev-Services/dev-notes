# 04 · Git Flow — Estrategia de ramas

## Problema que resuelve

Sin una convención de ramas, los equipos mezclan trabajo en progreso con código listo para producción. Git Flow define ramas con responsabilidades claras: desarrollo continuo, preparación de releases y correcciones de emergencia operan en ramas separadas.

## Tipos de ramas

| Rama | Tipo | Vida | Propósito |
|------|------|------|-----------|
| `main` | permanente | indefinida | código en producción, solo recibe merges de `release/*` y `hotfix/*` |
| `develop` | permanente | indefinida | integración de features, base para nuevos desarrollos |
| `feature/*` | temporal | hasta merge | una funcionalidad o ticket |
| `bugfix/*` | temporal | hasta merge | corrección de bug en `develop` |
| `release/*` | temporal | hasta release | preparación de versión: solo fixes menores y bump de versión |
| `hotfix/*` | temporal | hasta merge | corrección urgente en producción, sale de `main` |

## Convenciones de nombres

```
feature/GTM-123-descripcion-corta
bugfix/GTM-456-fix-login-error
release/1.2.0
hotfix/1.2.1-fix-null-pointer
```

Reglas:
- minúsculas, guiones como separador
- incluir número de ticket cuando existe
- descripción breve, sin espacios ni caracteres especiales

## Crear ramas

```bash
# feature nueva desde develop
git checkout develop
git pull origin develop
git checkout -b feature/GTM-123-user-auth

# bugfix en develop
git checkout develop
git checkout -b bugfix/GTM-456-fix-email-validation

# release desde develop
git checkout develop
git checkout -b release/1.3.0

# hotfix desde main
git checkout main
git pull origin main
git checkout -b hotfix/1.2.1-fix-crash-on-login
```

## Flujo de trabajo

### Feature normal

```
develop ──────────────────────────────────────► develop
         \                                    /
          feature/GTM-123 ──── commits ──────
```

```bash
# trabajar en la feature
git add .
git commit -m "feat(auth): add JWT validation"

# cuando está lista, merge a develop
git checkout develop
git merge --no-ff feature/GTM-123-user-auth
git push origin develop
git branch -d feature/GTM-123-user-auth
```

### Release

```bash
# crear release desde develop
git checkout -b release/1.3.0

# solo fixes menores + bump de versión
git commit -m "chore(release): bump version to 1.3.0"

# cuando está lista: merge a main y a develop
git checkout main
git merge --no-ff release/1.3.0
git tag -a v1.3.0 -m "Release 1.3.0"
git push origin main --tags

git checkout develop
git merge --no-ff release/1.3.0
git push origin develop

git branch -d release/1.3.0
```

### Hotfix

```bash
# crear hotfix desde main
git checkout main
git checkout -b hotfix/1.2.1-fix-crash

# corregir el bug
git commit -m "fix(auth): handle null token on refresh"

# merge a main
git checkout main
git merge --no-ff hotfix/1.2.1-fix-crash
git tag -a v1.2.1 -m "Hotfix 1.2.1"
git push origin main --tags

# merge también a develop para no perder la corrección
git checkout develop
git merge --no-ff hotfix/1.2.1-fix-crash
git push origin develop

git branch -d hotfix/1.2.1-fix-crash
```

## Relación con back-template

El back-template usa este flujo. Las PRs en GitHub/Azure DevOps tienen como base `develop`. La rama `main` se protege con reglas que requieren PR aprobada antes de merge. Las releases se sincronizan con el pipeline de CI/CD que se dispara al hacer push a `main`.

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| equipos con releases planificadas y ciclos definidos | proyectos personales o de un solo desarrollador |
| múltiples versiones en producción simultáneas | equipos que hacen deploys continuos (trunk-based es mejor) |
| proyectos con QA formal antes de cada release | proyectos donde `main` se despliega automáticamente en cada commit |

---

## Trunk-Based Development — alternativa a Git Flow
> Fuente: *Pro Git* (Chacon, Straub): Ch.5 Distributed Git

En proyectos con despliegue continuo (CI/CD agresivo), Git Flow puede ser overhead. Trunk-based development (TBD) es la alternativa.

```
Git Flow:             Trunk-Based:
main ─────────        main ──────────────────────► (siempre deployable)
      ↑                    ↑  ↑  ↑  ↑
   release                 |  |  |  |
      ↑                short-lived feature branches (max 1-2 días)
   develop                  (feature flags controlan el comportamiento)
      ↑
  feature/*
```

### Cuándo usar cada uno

| Criterio | Git Flow | Trunk-Based |
|----------|----------|-------------|
| Ciclo de release | Semanas/meses | Diario/continuo |
| Múltiples versiones en producción | ✓ | ✗ |
| Feature flags disponibles | No necesario | Casi obligatorio |
| Tamaño del equipo | Cualquiera | Mejor con equipo maduro en CI/CD |
| QA formal antes de release | ✓ | Tests automatizados reemplazan QA manual |

### Feature flags con trunk-based

```csharp
// En lugar de rama de larga duración, el código nuevo está "detrás de un flag"
public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken ct)
{
    // Feature flag — el código nuevo llega a main pero solo se activa para usuarios beta
    if (_featureFlags.IsEnabled("new-user-detail-v2", request.TenantId))
    {
        return await _newDetailService.GetAsync(request.PublicId, ct);
    }

    // Código actual (siempre disponible)
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
    return user is null
        ? new GetExampleUserNotFoundFailure("No encontrado.")
        : new GetExampleUserSuccess(new ExampleUserDto(user));
}
```

### Protección de `main` en ambos flujos

```bash
# Reglas de branch protection en GitHub (aplica a main en ambos flujos)
# Configurar en: Settings → Branches → Branch protection rules

# Requisitos recomendados:
# ✓ Require pull request before merging
# ✓ Require status checks to pass (CI)
# ✓ Require branches to be up to date before merging
# ✓ Require linear history (para trunk-based)
# ✓ Restrict who can push to main (solo CI/CD o leads)
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Git Flow | Estrategia de ramas que define roles claros para `main`, `develop`, `feature/*`, `release/*` y `hotfix/*` |
| main | Rama permanente que contiene únicamente el código en producción; recibe merges de release y hotfix |
| develop | Rama permanente de integración donde confluyen todas las features completadas |
| feature branch | Rama temporal para desarrollar una funcionalidad específica; se crea desde `develop` y se merge a `develop` |
| release branch | Rama temporal para preparar una versión; acepta solo fixes menores y bump de versión antes del merge a `main` |
| hotfix branch | Rama temporal creada directamente desde `main` para corregir un defecto crítico en producción |
| --no-ff | Opción de `git merge` que fuerza la creación de un merge commit aunque el fast-forward sea posible |
| fast-forward | Merge que avanza el puntero de la rama destino sin crear un commit adicional porque no hay divergencia |
| Trunk-based development | Alternativa a Git Flow donde todos trabajan sobre la rama principal con ramas de corta duración |
| Feature flag | Mecanismo para activar o desactivar código en producción sin desplegar, clave en trunk-based development |
| Branch protection | Regla en GitHub/Azure DevOps que requiere PR aprobada y checks de CI antes de hacer merge a `main` |

---

*Rogelio Arriaga Gonzalez*
