# dev-notes

Base de conocimiento personal de **Rogelio Arriaga** para desarrollo de software. Organizado por los 11 dominios del Glosario Maestro de SaaS Multi-Tenant.

---

## Dominios

| Dominio | Descripción | Docs |
|---------|-------------|------|
| [01-fundamentos](01-fundamentos/) | SOLID, Clean Code, Refactoring | 3 |
| [02-programacion](02-programacion/) | C# completo (19 docs) + Patrones GoF (22 docs) | 41 |
| [03-arquitectura](03-arquitectura/) | DDD, CQRS, Clean Architecture, Vertical Slice, microservicios, Keycloak, API design | 13 |
| [04-backend](04-backend/) | ASP.NET Core: Result Pattern, Repository, Pipeline Behaviors, auth, caching, validación, EF Core, performance, memoria, JWT + testing | 24 |
| [05-bases-de-datos](05-bases-de-datos/) | SQL, índices, transacciones, PostgreSQL avanzado, Dapper, connection strings | 6 |
| [06-frontend](06-frontend/) | React 19, Vite, Tailwind v4, design system, componentes primitivos, feature hook | 6 |
| [07-git](07-git/) | Tooling, SemVer, Git Flow, tags, conventional commits, merge strategies, rollback, stash | 9 |
| [08-contenedores](08-contenedores/) | Docker: conceptos, Dockerfile multi-stage, Compose, comandos | 5 |
| [09-cicd](09-cicd/) | Checklists, GitHub Actions (OIDC + AWS), Azure DevOps, Terraform | 4 |
| [10-cloud](10-cloud/) | AWS, Azure, Linux+Nginx, observabilidad, secretos, costos, seguridad web (OWASP), Zero Trust, tracing distribuido | 9 |
| [11-vibe-coding](11-vibe-coding/) | CLAUDE.md, prompts efectivos, AI workflow, manejo de contexto, LLMOps | 5 |
| [resources](resources/) | Bibliografía indexada | — |

---

## Estructura completa

```
dev-notes/
├── 01-fundamentos/
│   ├── 01-solid.md
│   ├── 02-clean-code.md
│   └── 03-refactoring.md
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
│   ├── 07-microservicios.md
│   ├── 08-autenticacion-keycloak.md
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
│   ├── 09-configuracion.md
│   ├── 10-secretos.md
│   ├── 11-cors.md
│   ├── 12-rate-limiting.md
│   ├── 13-http-client.md
│   ├── 14-hangfire.md
│   ├── 15-openapi.md
│   ├── 16-output-caching.md
│   ├── 17-ef-core.md
│   ├── 18-common-library.md
│   ├── 19-performance.md
│   ├── 20-memoria-gc.md
│   ├── 21-jwt.md
│   └── testing/                    (01–05: xUnit → TDD)
├── 05-bases-de-datos/
│   ├── 01-consultas.md
│   ├── 02-indices.md
│   ├── 03-transacciones.md
│   ├── 04-postgresql-avanzado.md
│   ├── 05-postgresql-dapper.md
│   └── 06-connection-strings.md
├── 06-frontend/
│   ├── 01-variables-entorno-vite.md
│   ├── 02-react-produccion.md
│   ├── 03-tailwind.md
│   ├── 04-design-system.md
│   ├── 05-componentes-primitivos.md
│   └── 06-feature-hook.md
├── 07-git/
│   ├── 01-project-tooling.md
│   ├── 02-semver.md
│   ├── 03-git-config.md
│   ├── 04-git-flow.md
│   ├── 05-tags.md
│   ├── 06-commit-conventions.md
│   ├── 07-merge-strategies.md
│   ├── 08-rollback.md
│   └── 09-stash.md
├── 08-contenedores/
│   ├── 01-conceptos.md
│   ├── 02-dockerfile.md
│   ├── 03-compose.md
│   ├── 04-proyecto.md
│   └── 05-comandos.md
├── 09-cicd/
│   ├── 01-checklists.md
│   ├── 02-github-actions.md
│   ├── 03-azure-devops.md
│   └── 04-terraform.md
├── 10-cloud/
│   ├── 01-aws-basics.md
│   ├── 02-azure-basics.md
│   ├── 03-linux-nginx.md
│   ├── 04-observabilidad.md
│   ├── 05-secretos-produccion.md
│   ├── 06-costos.md
│   ├── 07-seguridad-web.md
│   ├── 08-zero-trust.md
│   └── 09-tracing-distribuido.md
├── 11-vibe-coding/
│   ├── 01-agents-md.md
│   ├── 02-prompts-efectivos.md
│   ├── 03-ai-workflow.md
│   ├── 04-context-management.md
│   └── 05-llmops.md
└── resources/
    └── books/README.md             (índice de biblioteca — Capacitacion\Bibliografias)
```

---

## Proyectos de referencia

Los ejemplos de código de este repositorio están basados en las dos plantillas del stack:

| Repo | Stack | Cubre |
|------|-------|-------|
| [`back-template`](../back-template) | .NET 10 · Clean Architecture · PostgreSQL · Docker | `01-fundamentos`, `02-programacion`, `03-arquitectura`, `04-backend`, `05-bases-de-datos`, `07-git`, `08-contenedores`, `09-cicd`, `10-cloud` |
| [`front-template`](../front-template) | React 19 · Vite · Tailwind v4 · Axios · SignalR | `06-frontend` |

---

*Rogelio Arriaga Gonzalez*
