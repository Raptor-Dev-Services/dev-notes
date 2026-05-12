# dev-notes

Base de conocimiento personal de **Rogelio Arriaga** para desarrollo de software. Organizado por los 11 dominios del Glosario Maestro de SaaS Multi-Tenant.

---

## Dominios

| Dominio | Descripción | Docs |
|---------|-------------|------|
| [01-fundamentos](01-fundamentos/) | SOLID, paradigmas, principios de ingeniería | 1 |
| [02-programacion](02-programacion/) | C# completo (19 docs) + Patrones GoF (22 docs) | 41 |
| [03-arquitectura](03-arquitectura/) | DDD, CQRS, Clean Architecture, Vertical Slice, API design | 11 |
| [04-backend](04-backend/) | .NET / ASP.NET Core: Result Pattern, Repository, Pipeline Behaviors, auth, caching, validación, EF Core, Common library + testing | 28 |
| [05-bases-de-datos](05-bases-de-datos/) | SQL, índices, transacciones, PostgreSQL avanzado, Dapper, connection strings | 6 |
| [06-frontend](06-frontend/) | Tailwind v4, design system, componentes primitivos, feature hook, variables Vite, React producción | 6 |
| [07-git](07-git/) | Tooling, SemVer, Git Flow, tags, conventional commits, merge strategies, rollback, stash | 9 |
| [08-contenedores](08-contenedores/) | Docker: conceptos, Dockerfile, Compose, comandos | 5 |
| [09-cicd](09-cicd/) | Checklists, GitHub Actions (AWS ECR+ECS), Azure DevOps pipelines | 3 |
| [10-cloud](10-cloud/) | AWS basics, Azure basics, Linux+Nginx, observabilidad, secretos, costos | 6 |
| [11-vibe-coding](11-vibe-coding/) | CLAUDE.md, prompts efectivos, AI workflow, manejo de contexto | 4 |
| [resources](resources/) | Bibliografía, referencias externas | — |

---

## Estructura completa

```
dev-notes/
├── 01-fundamentos/
│   └── 01-solid.md
├── 02-programacion/
│   ├── csharp/                     (01–19: classes → configuration)
│   └── design-patterns/
│       ├── creational/             (01–05: Singleton → Prototype)
│       ├── structural/             (06–12: Adapter → Proxy)
│       └── behavioral/             (13–22: Chain → Visitor)
├── 03-arquitectura/
│   ├── 01-ddd.md
│   ├── 02-cqrs.md
│   ├── 03-hexagonal-vs-clean.md
│   ├── 04-vertical-slice.md
│   ├── 05-specification.md
│   ├── 06-api-versioning.md
│   └── api-design/                 (01–05: HTTP → buenas prácticas)
├── 04-backend/
│   ├── 01-result-pattern.md
│   ├── 02-repository-uow.md
│   ├── 03-pipeline-behaviors.md
│   ├── 04-resiliencia-polly.md
│   ├── 05-caching.md
│   ├── 06-validation.md
│   ├── 07-background-services.md
│   ├── 08-problem-details.md
│   ├── 09-configuracion.md         (Manual Práctico — .NET + Docker)
│   ├── 10-secretos.md              (Manual Práctico — Key Vault, Secrets Manager)
│   ├── 11-cors.md                  (Manual Práctico)
│   ├── 12-rate-limiting.md         (Manual Práctico)
│   ├── 13-http-client.md           (Manual Práctico — IHttpClientFactory + Polly)
│   ├── 14-hangfire.md              (Manual Práctico — jobs persistentes + SaaS tenant)
│   ├── 15-openapi.md               (Manual Práctico — API versioning + Swagger + Scalar)
│   ├── 16-output-caching.md        (Manual Práctico)
│   ├── 17-ef-core.md               (Manual Práctico — migrations, soft delete, auditing)
│   ├── 18-common-library.md        (Manual Práctico — librería Common completa)
│   └── testing/                    (01–05: xUnit → TDD)
├── 05-bases-de-datos/
│   ├── 01-consultas.md
│   ├── 02-indices.md
│   ├── 03-transacciones.md
│   ├── 04-postgresql-avanzado.md
│   ├── 05-postgresql-dapper.md
│   └── 06-connection-strings.md    (Manual Práctico)
├── 06-frontend/
│   ├── 01-variables-entorno-vite.md (Manual Práctico)
│   └── 02-react-produccion.md       (Manual Práctico — Router, Axios, RHF, i18n, TanStack)
├── 07-git/
│   ├── 01-project-tooling.md        (Manual Práctico — .gitignore, Husky, Dependabot)
│   ├── 02-semver.md                 (SemVer 2.0.0, pre-release, build metadata)
│   ├── 03-git-config.md             (global, local, multi-usuario, includeIf)
│   ├── 04-git-flow.md               (branch types, naming, feature/release/hotfix flow)
│   ├── 05-tags.md                   (annotated vs lightweight, push, delete, convenciones)
│   ├── 06-commit-conventions.md     (Conventional Commits, tipos, breaking changes)
│   ├── 07-merge-strategies.md       (--no-ff, squash, rebase, rebase interactivo)
│   ├── 08-rollback.md               (redespliegue tag, git revert, hotfix, checklists)
│   └── 09-stash.md                  (push/pop/apply, mover cambios entre ramas)
├── 08-contenedores/
│   ├── 01-conceptos.md
│   ├── 02-dockerfile.md
│   ├── 03-compose.md
│   ├── 04-proyecto.md
│   └── 05-comandos.md
├── 09-cicd/
│   └── 01-checklists.md             (Manual Práctico — commit, merge, deploy, diagnóstico)
├── 10-cloud/                        (← pendiente)
├── 11-vibe-coding/                  (← pendiente)
└── resources/
    └── books/                       (← notas de bibliografía)
```

---

## Fuentes

- `01-fundamentos/` a `04-backend/01–08`, `05-bases-de-datos/01–05`, `08-contenedores/` — documentados en sesiones anteriores, basados en `back-template`
- `04-backend/09–18`, `05-bases-de-datos/06`, `06-frontend/`, `07-git/`, `09-cicd/` — extraídos del **Manual Práctico del Stack v2.0** (Raptor Dev Services, Mayo 2026)

---

## Proyecto relacionado

[`back-template`](../back-template) — plantilla backend .NET 10 Clean Architecture sobre la que se basan los ejemplos.


---

*Rogelio Arriaga Gonzalez*
