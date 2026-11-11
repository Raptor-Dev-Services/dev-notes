# dev-notes

Base de conocimiento personal de **Rogelio Arriaga** para desarrollo de software. Organizado por los 11 dominios del Glosario Maestro de SaaS Multi-Tenant.

El listado completo de documentos está en [INDICE.md](INDICE.md) y las rutas de estudio en [roadmap/](roadmap/).

---

## Dominios

| Dominio | Descripción | Docs |
|---------|-------------|------|
| [01-fundamentos](01-fundamentos/) | SOLID, Clean Code, Refactoring, Agile/Scrum, SDLC, estimación, stakeholders, deuda técnica, entrevistas técnicas | 9 |
| [02-programacion](02-programacion/) | C# (20 docs) + Patrones GoF (22 docs) + patrones en ASP.NET Core (2 docs) | 44 |
| [03-arquitectura](03-arquitectura/) | DDD, CQRS, hexagonal, vertical slice, specification, versionado, microservicios, Keycloak, monolito modular, Clean Architecture, C4, REPR, diseño de sistemas + API design (5 docs) | 21 |
| [04-backend](04-backend/) | ASP.NET Core: result pattern, repository, pipeline behaviors, validación, caching, EF Core, Dapper, JWT, performance, multi-tenancy SaaS + testing (5 docs) | 51 |
| [05-bases-de-datos](05-bases-de-datos/) | SQL, índices, transacciones, PostgreSQL avanzado, Dapper, connection strings, migraciones multi-tenant, RLS | 8 |
| [06-frontend](06-frontend/) | React 19, Vite, Tailwind v4, design system, componentes primitivos, feature hook, capa de dominio, estado, headless, fronteras de import, TypeScript, seguridad en el navegador, pruebas, caché de assets | 15 |
| [07-git](07-git/) | Tooling, SemVer, config, Git Flow, tags, conventional commits, merge strategies, rollback, stash | 9 |
| [08-contenedores](08-contenedores/) | Docker y Kubernetes: conceptos, Dockerfile multi-stage, Compose, proyecto real, comandos, Kubernetes (conceptos, manifiestos, seguridad, diagnóstico) | 9 |
| [09-cicd](09-cicd/) | Checklists DevSecOps, GitHub Actions (OIDC + AWS), Azure DevOps, Terraform y OpenTofu, Ansible, políticas como código | 7 |
| [10-cloud](10-cloud/) | AWS y Azure, Linux + Nginx, terminal, Bash, systemd y diagnóstico, observabilidad, secretos, costos, seguridad web, Zero Trust, AWS en profundidad, tracing distribuido | 21 |
| [11-vibe-coding](11-vibe-coding/) | CLAUDE.md, prompts efectivos, AI workflow, manejo de contexto, LLMOps, arquitectura agéntica, LLM en producción, RAG, MCP, seguridad de LLM, ética y uso responsable | 11 |
| [12-seguridad](12-seguridad/) | Ruta de ciberseguridad en cinco materias (introducción, riesgos actuales, informática forense, seguridad de la información y legislación) y las notas de defensa y respuesta a incidentes y de pentest con Kali | 84 |
| [roadmap](roadmap/) | Rutas de aprendizaje por objetivo | 7 |
| [cursos](cursos/) | Los cursos de [academy.raptorcloud.dev](https://academy.raptorcloud.dev) en Markdown: clase, práctica, tarea y glosario de cada unidad | 17 |
| [resources](resources/books/README.md) | Bibliografía indexada | - |

Además, en la raíz: [Glosario Maestro SaaS](Glosario_Maestro_SaaS.md) y [Manual Práctico del Stack](Manual_Practico_Stack.md).

---

## Estructura

```
dev-notes/
├── 01-fundamentos/          9 docs
├── 02-programacion/
│   ├── csharp/              20 docs (01-20)
│   └── design-patterns/     22 GoF (creational 01-05, structural 06-12, behavioral 13-22)
│                            + 2 de ASP.NET Core + referencias-csharp/ (ejemplos compilables)
├── 03-arquitectura/         16 docs (01-16) + api-design/ (5 docs)
├── 04-backend/              46 docs (01-46) + testing/ (5 docs)
├── 05-bases-de-datos/       8 docs
├── 06-frontend/             15 docs
├── 07-git/                  9 docs
├── 08-contenedores/         9 docs
├── 09-cicd/                 7 docs
├── 10-cloud/                21 docs (01-21)
├── 11-vibe-coding/          11 docs
├── 12-seguridad/            84 docs en 7 materias (01-introduccion a 07-pentest)
├── roadmap/                 7 rutas
├── cursos/                  17 cursos de academy, en Markdown
├── resources/books/         índice de biblioteca
├── INDICE.md
├── Glosario_Maestro_SaaS.md
└── Manual_Practico_Stack.md
```

---

## Convención de placeholders

Los ejemplos no usan nombres de proyectos reales. Donde un ejemplo necesita un nombre concreto aparece un placeholder `{{UPPER_SNAKE}}`: sustitúyelo por el valor de tu proyecto. No confundir con `${{ }}` de GitHub Actions, que sí es sintaxis del workflow.

| Placeholder | Qué sustituye | Ejemplo |
|-------------|---------------|---------|
| `{{PROJECT_NAME}}` | Nombre del producto | `Mi SaaS` |
| `{{PROJECT_SLUG}}` | Nombre en kebab-case para recursos: buckets, contenedores, realms, log groups | `mi-saas` |
| `{{ROOT_NAMESPACE}}` | Prefijo de los namespaces y proyectos .NET | `MiSaas` |
| `{{PRIMARY_DB}}` | Nombre de la base de datos principal | `mi_saas` |
| `{{PROJECT_DOMAIN}}` | Dominio de negocio del producto | `reservas para clínicas` |
| `{{REPO_NAME}}` | Nombre del repositorio | `mi-saas-webclient` |
| `{{TASK_ID_PREFIX}}` | Prefijo del id de tarjeta, con su guion | `MS-` |

---

## Proyectos de referencia

Los ejemplos de código de este repositorio están basados en las dos plantillas del stack:

| Repo | Stack | Cubre |
|------|-------|-------|
| [`back-template`](https://github.com/Raptor-Dev-Services/back-template) | .NET 10 - Clean Architecture - PostgreSQL - Docker | `01-fundamentos`, `02-programacion`, `03-arquitectura`, `04-backend`, `05-bases-de-datos`, `07-git`, `08-contenedores`, `09-cicd`, `10-cloud` |
| [`front-template`](https://github.com/Raptor-Dev-Services/front-template) | React 19 - Vite - Tailwind v4 - Axios - SignalR | `06-frontend` |

---

*Rogelio Arriaga Gonzalez*
