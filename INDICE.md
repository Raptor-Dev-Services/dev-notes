# INDICE — dev-notes

Índice completo de todos los documentos organizados por dominio.
Para rutas de aprendizaje ver [`roadmap/`](roadmap/).

---

## 01 — Fundamentos de Software (8 docs)

| # | Doc | Tema |
|---|-----|------|
| 01 | [SOLID](01-fundamentos/01-solid.md) | SRP, OCP, LSP, ISP, DIP con ejemplos C# |
| 02 | [Clean Code](01-fundamentos/02-clean-code.md) | Nombres, funciones, comentarios, formato — Martin |
| 03 | [Refactoring](01-fundamentos/03-refactoring.md) | Code smells y técnicas de refactor — Fowler |
| 04 | [Agile / Scrum](01-fundamentos/04-agile-scrum.md) | Sprints, roles, eventos, artefactos |
| 05 | [SDLC](01-fundamentos/05-sdlc.md) | Ciclo de vida del desarrollo de software |
| 06 | [Estimación](01-fundamentos/06-estimacion.md) | Story points, Planning Poker, Cone of Uncertainty |
| 07 | [Stakeholders](01-fundamentos/07-stakeholders.md) | Gestión de expectativas y comunicación |
| 08 | [Deuda Técnica](01-fundamentos/08-deuda-tecnica-proceso.md) | Tipos, impacto, gestión como proceso continuo |

---

## 02 — Programación: C# (19 docs)

| # | Doc | Tema |
|---|-----|------|
| 01 | [Classes](02-programacion/csharp/01-classes.md) | Clases, sealed, static, partial |
| 02 | [Interfaces](02-programacion/csharp/02-interfaces.md) | Contratos, implementación explícita, default members |
| 03 | [Constructors & DI](02-programacion/csharp/03-constructors-di.md) | Constructores, primary constructors, DI manual |
| 04 | [Records](02-programacion/csharp/04-records.md) | record, record struct, with expressions, igualdad por valor |
| 05 | [Herencia](02-programacion/csharp/05-inheritance.md) | Herencia, abstract, virtual, override, new |
| 06 | [Properties & Fields](02-programacion/csharp/06-properties-fields.md) | Auto-props, init, required, readonly |
| 07 | [Methods](02-programacion/csharp/07-methods.md) | Parámetros, ref/out, extension methods, local functions |
| 08 | [Async/Await](02-programacion/csharp/08-async.md) | Task, ValueTask, ConfigureAwait, CancellationToken |
| 09 | [Lifetimes & DI](02-programacion/csharp/09-lifetimes.md) | Singleton, Scoped, Transient — cuándo usar cada uno |
| 10 | [Generics](02-programacion/csharp/10-generics.md) | Tipos genéricos, constraints, varianza |
| 11 | [Pattern Matching](02-programacion/csharp/11-pattern-matching.md) | switch expressions, is, when, list patterns |
| 12 | [Collections](02-programacion/csharp/12-collections.md) | List, Dictionary, IEnumerable, LINQ, Span<T> |
| 13 | [Nullability](02-programacion/csharp/13-nullability.md) | Nullable reference types, operadores ?., ??, null checks |
| 14 | [Exceptions](02-programacion/csharp/14-exceptions.md) | try/catch/finally, custom exceptions, filtros |
| 15 | [Strings](02-programacion/csharp/15-strings.md) | Interpolación, Span, StringBuilder, StringComparison |
| 16 | [Namespaces](02-programacion/csharp/16-namespaces.md) | Namespaces, using, global using, file-scoped |
| 17 | [Enums](02-programacion/csharp/17-enums.md) | Enums, [Flags], extensión de enums |
| 18 | [Attributes](02-programacion/csharp/18-attributes.md) | Atributos custom, source generators, reflection |
| 19 | [Configuration C#](02-programacion/csharp/19-configuration.md) | IConfiguration, Options Pattern dentro del lenguaje |

---

## 02 — Programación: Patrones de Diseño GoF (23 + 3 docs)

### Creacionales (5)

| # | Doc | Patrón |
|---|-----|--------|
| 01 | [Singleton](02-programacion/design-patterns/creational/01-singleton.md) | Una sola instancia global |
| 02 | [Factory Method](02-programacion/design-patterns/creational/02-factory-method.md) | Delegar creación a subclases |
| 03 | [Abstract Factory](02-programacion/design-patterns/creational/03-abstract-factory.md) | Familias de objetos relacionados |
| 04 | [Builder](02-programacion/design-patterns/creational/04-builder.md) | Construcción paso a paso |
| 05 | [Prototype](02-programacion/design-patterns/creational/05-prototype.md) | Clonar objetos existentes |

### Estructurales (7)

| # | Doc | Patrón |
|---|-----|--------|
| 06 | [Adapter](02-programacion/design-patterns/structural/06-adapter.md) | Interfaz compatible entre clases incompatibles |
| 07 | [Bridge](02-programacion/design-patterns/structural/07-bridge.md) | Separar abstracción de implementación |
| 08 | [Composite](02-programacion/design-patterns/structural/08-composite.md) | Árbol de objetos uniforme |
| 09 | [Decorator](02-programacion/design-patterns/structural/09-decorator.md) | Añadir comportamiento dinámicamente |
| 10 | [Facade](02-programacion/design-patterns/structural/10-facade.md) | Interfaz simplificada a subsistema complejo |
| 11 | [Flyweight](02-programacion/design-patterns/structural/11-flyweight.md) | Compartir estado común para reducir memoria |
| 12 | [Proxy](02-programacion/design-patterns/structural/12-proxy.md) | Sustituto con control de acceso |

### Conductuales / Behavioral (10)

| # | Doc | Patrón |
|---|-----|--------|
| 13 | [Chain of Responsibility](02-programacion/design-patterns/behavioral/13-chain-of-responsibility.md) | Cadena de manejadores — pipeline |
| 14 | [Command](02-programacion/design-patterns/behavioral/14-command.md) | Encapsular operaciones como objetos |
| 15 | [Iterator](02-programacion/design-patterns/behavioral/15-iterator.md) | Recorrer colecciones sin exponer estructura |
| 16 | [Mediator](02-programacion/design-patterns/behavioral/16-mediator.md) | Comunicación centralizada entre objetos |
| 17 | [Memento](02-programacion/design-patterns/behavioral/17-memento.md) | Capturar y restaurar estado |
| 18 | [Observer](02-programacion/design-patterns/behavioral/18-observer.md) | Notificar cambios a múltiples oyentes |
| 19 | [State](02-programacion/design-patterns/behavioral/19-state.md) | Cambiar comportamiento según estado interno |
| 20 | [Strategy](02-programacion/design-patterns/behavioral/20-strategy.md) | Familia de algoritmos intercambiables |
| 21 | [Template Method](02-programacion/design-patterns/behavioral/21-template-method.md) | Esqueleto de algoritmo con pasos variables |
| 22 | [Visitor](02-programacion/design-patterns/behavioral/22-visitor.md) | Operación nueva sin modificar clases |

### Patrones ASP.NET Core específicos (3)

| Doc | Tema |
|-----|------|
| [Structural ASP.NET](02-programacion/design-patterns/structural-aspnet.md) | Decorator, Composite, Adapter, Façade — Ferreira Ch.11 |
| [Object Mappers](02-programacion/design-patterns/object-mappers.md) | Manual mapping, AutoMapper, Mapperly — Ferreira Ch.15 |

---

## 03 — Arquitectura (13 + 5 docs)

| # | Doc | Tema |
|---|-----|------|
| 01 | [DDD](03-arquitectura/01-ddd.md) | Domain-Driven Design: entidades, agregados, bounded contexts |
| 02 | [CQRS](03-arquitectura/02-cqrs.md) | Command/Query separation, MediatR, message types |
| 03 | [Hexagonal](03-arquitectura/03-hexagonal.md) | Ports & Adapters, composición de adaptadores |
| — | [Hexagonal vs Clean](03-arquitectura/03-hexagonal-vs-clean.md) | Comparativa de estilos arquitectónicos |
| 04 | [Vertical Slice](03-arquitectura/04-vertical-slice.md) | Feature folders, Jimmy Bogard, coupling |
| 05 | [Specification](03-arquitectura/05-specification.md) | Specification pattern para queries reutilizables |
| 06 | [API Versioning](03-arquitectura/06-api-versioning.md) | Estrategias de versioning de APIs REST |
| 07 | [Microservicios](03-arquitectura/07-microservicios.md) | EDA, Pub-Sub, message brokers, DLQ |
| 08 | [Keycloak](03-arquitectura/08-keycloak.md) | IAM, OIDC, SSO, multi-tenancy con claims |
| — | [Autenticación Keycloak](03-arquitectura/08-autenticacion-keycloak.md) | Configuración detallada de Keycloak |
| 09 | [Monolito Modular](03-arquitectura/09-modular-monolith.md) | Módulos con Contracts, reglas de referencia |
| 10 | [Tenant Subdomain Routing](03-arquitectura/10-tenant-subdomain-routing.md) | Middleware de resolución de tenant por subdominio |
| 11 | [Clean Architecture](03-arquitectura/11-clean-arch.md) | Capas, regla de dependencia, Ferreira Ch.14 |
| 12 | [C4 Model](03-arquitectura/12-c4-model.md) | Diagramas de arquitectura como código (Structurizr DSL) |
| 13 | [REPR Pattern](03-arquitectura/13-repr.md) | Request-EndPoint-Response para Minimal APIs |

### API Design (5 docs)

| Doc | Tema |
|-----|------|
| [HTTP](03-arquitectura/api-design/01-http.md) | Verbos, status codes, headers |
| [REST](03-arquitectura/api-design/02-rest.md) | Constraints REST, HATEOAS, recursos |
| [Paginación](03-arquitectura/api-design/03-paginacion.md) | Offset, cursor, keyset pagination |
| [Errores y Contratos](03-arquitectura/api-design/04-errores-contratos.md) | Problem Details RFC 9457, error handling |
| [Buenas Prácticas](03-arquitectura/api-design/05-buenas-practicas.md) | Naming, idempotencia, versioning práctico |

---

## 04 — Backend ASP.NET Core (46 docs)

### Core patterns (8)

| # | Doc | Tema |
|---|-----|------|
| 01 | [Result Pattern](04-backend/01-result-pattern.md) | ISuccess/IFailure, Operation Result — Ferreira Ch.13 |
| 02 | [Repository + UoW](04-backend/02-repository-uow.md) | Repositorio e interfaz, Unit of Work |
| 03 | [Pipeline Behaviors](04-backend/03-pipeline-behaviors.md) | MediatR behaviors: logging, validation, timing |
| 06 | [Validation](04-backend/06-validation.md) | FluentValidation, reglas, extensiones |
| 08 | [Problem Details](04-backend/08-problem-details.md) | RFC 9457, middleware de errores, ProblemDetailsFactory |
| 18 | [Common Library](04-backend/18-common-library.md) | Submódulo NuGet compartido: IMediator, IRequest, IResponse |
| 46 | [DI Patterns](04-backend/46-di-patterns.md) | Decorator/Scrutor, Keyed Services, captive dependency |
| — | [REPR / Minimal APIs](03-arquitectura/13-repr.md) | Ver sección Arquitectura |

### Infraestructura y cross-cutting (12)

| # | Doc | Tema |
|---|-----|------|
| 04 | [Resiliencia / Polly](04-backend/04-resiliencia-polly.md) | Retry, circuit breaker, timeout, fallback |
| 05 | [Caching](04-backend/05-caching.md) | IMemoryCache, IDistributedCache, Redis |
| 07 | [Background Services](04-backend/07-background-services.md) | IHostedService, BackgroundService, canales |
| 09 | [Configuración](04-backend/09-configuracion.md) | IOptions, FluentValidation bridge, [OptionsValidator] |
| 10 | [Secretos](04-backend/10-secretos.md) | User Secrets, Azure Key Vault, BuildKit mount |
| 11 | [CORS](04-backend/11-cors.md) | Preflight, AllowCredentials, policy per-endpoint |
| 12 | [Rate Limiting](04-backend/12-rate-limiting.md) | 4 estrategias: fixed, sliding, token, concurrency |
| 13 | [HttpClient](04-backend/13-http-client.md) | IHttpClientFactory, Typed/Named, Polly |
| 14 | [Hangfire](04-backend/14-hangfire.md) | Jobs: fire-and-forget, recurrentes, continuations |
| 15 | [OpenAPI](04-backend/15-openapi.md) | .NET 9 native, Scalar UI, Problem Details |
| 16 | [Output Caching](04-backend/16-output-caching.md) | Middleware de caché de respuestas, invalidación por tag |
| 17 | [EF Core](04-backend/17-ef-core.md) | DbContext, fluent config, migrations, tracking |

### Seguridad y autenticación (3)

| # | Doc | Tema |
|---|-----|------|
| 21 | [JWT](04-backend/21-jwt.md) | Emisión, validación, claims, JWK/JWKS, key rotation |
| 38 | [2FA / MFA](04-backend/38-2fa-mfa.md) | TOTP, QR code, códigos de respaldo |
| 39 | [SSO / OIDC](04-backend/39-sso-oidc.md) | Flujos OIDC por tenant, federación |

### Performance (2)

| # | Doc | Tema |
|---|-----|------|
| 19 | [Performance](04-backend/19-performance.md) | BenchmarkDotNet, Span<T>, ArrayPool, stackalloc |
| 20 | [Memoria / GC](04-backend/20-memoria-gc.md) | Generaciones GC, IDisposable, WeakReference, LOH |

### Testing (5 docs)

| Doc | Tema |
|-----|------|
| [xUnit](04-backend/testing/01-xunit.md) | Fixtures, Theory, parametrización, colecciones |
| [Unit Tests](04-backend/testing/02-unit-tests.md) | Mocks con NSubstitute, AAA pattern |
| [Integration Tests](04-backend/testing/03-integration-tests.md) | WebApplicationFactory, test DB |
| [Testcontainers](04-backend/testing/04-testcontainers.md) | PostgreSQL en Docker para tests |
| [TDD](04-backend/testing/05-tdd.md) | Red-Green-Refactor, ciclo TDD |

### Multi-tenancy SaaS (24 docs)

| # | Doc | Tema |
|---|-----|------|
| 22 | [EF Core Multi-Tenancy](04-backend/22-ef-core-multi-tenancy.md) | Global Query Filters, ITenantContextAccessor |
| 23 | [Dapper](04-backend/23-dapper.md) | Dapper con tenant context, transacciones |
| 24 | [Multi-Tenancy Logic](04-backend/24-multi-tenancy-logic.md) | Modelo conceptual, estrategias de aislamiento |
| 25 | [Tenant Branch Logic](04-backend/25-tenant-branch-logic.md) | Lógica de branches dentro de un tenant |
| 26 | [Tenant Onboarding](04-backend/26-tenant-onboarding.md) | Flujo de registro de nuevos tenants |
| 27 | [RBAC](04-backend/27-rbac.md) | Roles por tenant (flat/jerárquico), permisos |
| 28 | [User Invitation](04-backend/28-user-invitation.md) | Flujo de invitación de usuarios al tenant |
| 29 | [Audit Trail](04-backend/29-audit-trail.md) | Registro de auditoría inmutable por tenant |
| 30 | [Tenant Config](04-backend/30-tenant-config.md) | Configuración personalizada por tenant |
| 31 | [Planes y Límites](04-backend/31-planes-limites.md) | Planes de suscripción, feature flags por plan |
| 32 | [Stripe Billing](04-backend/32-stripe-billing.md) | Customer, Subscription, webhooks de pago |
| 33 | [Rate Limiting Tenant](04-backend/33-rate-limiting-tenant.md) | Rate limiting por tenant_id |
| 34 | [API Keys](04-backend/34-api-keys.md) | API Keys para integraciones externas |
| 35 | [Tenant Leakage Tests](04-backend/35-tenant-leakage-tests.md) | Tests de aislamiento entre tenants |
| 36 | [Webhooks Salientes](04-backend/36-webhooks-salientes.md) | Webhooks outbound del SaaS |
| 37 | [Emails Transaccionales](04-backend/37-emails-transaccionales.md) | Emails por tenant (SendGrid, SES) |
| 40 | [Session Management](04-backend/40-session-management.md) | Refresh tokens, revocación de sesiones |
| 41 | [Background Jobs Multi-Tenant](04-backend/41-background-jobs-multitenant.md) | Hangfire con TenantContext en jobs |
| 42 | [SignalR Realtime](04-backend/42-signalr-realtime.md) | Grupos SignalR por tenant/branch/usuario |
| 43 | [Super Admin](04-backend/43-super-admin.md) | Panel cross-tenant para el equipo del SaaS |
| 44 | [GDPR / Data Export](04-backend/44-gdpr-data-export.md) | Exportación de datos por tenant |
| 45 | [Idempotency Keys](04-backend/45-idempotency-keys.md) | Prevención de operaciones duplicadas |

---

## 05 — Bases de Datos (8 docs)

| Doc | Tema |
|-----|------|
| [SQL Fundamentos](05-bases-de-datos/01-sql-fundamentos.md) | SELECT, JOINs, CTEs, window functions, ACID, índices |
| [Consultas](05-bases-de-datos/01-consultas.md) | Consultas avanzadas SQL |
| [Índices](05-bases-de-datos/02-indices.md) | B-Tree, GIN, GiST, partial, EXPLAIN |
| [Transacciones](05-bases-de-datos/03-transacciones.md) | ACID, isolation levels, deadlocks |
| [PostgreSQL Avanzado](05-bases-de-datos/04-postgresql-avanzado.md) | JSONB, arrays, full-text search, vacío |
| [Dapper Queries](05-bases-de-datos/05-dapper-queries.md) | QueryMultiple, multi-mapping, DynamicParameters, paginación |
| [PostgreSQL + Dapper](05-bases-de-datos/05-postgresql-dapper.md) | Integración Dapper con PostgreSQL / Npgsql |
| [Connection Strings](05-bases-de-datos/06-connection-strings.md) | Formato, pooling, resiliencia |
| [Migrations Multi-Tenant](05-bases-de-datos/07-migrations-multi-tenant.md) | Estrategias de migration por tenant |
| [Row Level Security](05-bases-de-datos/08-row-level-security.md) | RLS en PostgreSQL: políticas, bypass, performance |

---

## 06 — Frontend React (11 docs)

| # | Doc | Tema |
|---|-----|------|
| 01 | [Variables de Entorno Vite](06-frontend/01-variables-entorno-vite.md) | .env, TypeScript types, seguridad |
| 02 | [React en Producción](06-frontend/02-react-produccion.md) | Router, Axios interceptors, RHF+Zod, TanStack Query |
| 03 | [Tailwind v4](06-frontend/03-tailwind.md) | Dark mode CSS variables, tenant theming, plugins |
| 04 | [Design System](06-frontend/04-design-system.md) | Semantic tokens, tipografía, ARIA, Modal, FormField |
| 05 | [Componentes Primitivos](06-frontend/05-componentes-primitivos.md) | Button, Input, Table — base del design system |
| 06 | [Feature Hook](06-frontend/06-feature-hook.md) | Patrón de hook por feature (useExampleUser) |
| 07 | [Capa de Dominio y ACL](06-frontend/07-capa-dominio-acl.md) | Anti-Corruption Layer, modelo de dominio, Strategy, capas |
| 08 | [Estado: dónde vive](06-frontend/08-estado-donde-vive.md) | Local, URL, Context, store con selectores, persistencia |
| 09 | [Componentes Headless](06-frontend/09-componentes-headless.md) | Prop getters, hook vs render prop vs HOC, sobre-abstracción |
| 10 | [Fronteras de Import](06-frontend/10-fronteras-import.md) | app -> features -> shared con `import/no-restricted-paths` |
| 11 | [Prácticas del Código Real](06-frontend/11-practicas-codigo-real.md) | Máquina de estados, AbortController, Result en cliente, incidentes |

---

## 07 — Git (9 docs)

| # | Doc | Tema |
|---|-----|------|
| 01 | [Project Tooling](07-git/01-project-tooling.md) | .gitignore, .editorconfig, Husky, Dependabot |
| 02 | [SemVer](07-git/02-semver.md) | Versionado semántico MAJOR.MINOR.PATCH |
| 03 | [Git Config](07-git/03-git-config.md) | Configuración global y por repo |
| 04 | [Git Flow](07-git/04-git-flow.md) | Branching strategy, feature/release/hotfix |
| 05 | [Tags](07-git/05-tags.md) | Anotados vs ligeros, signed tags, push |
| 06 | [Commit Conventions](07-git/06-commit-conventions.md) | Conventional Commits, commitlint, Husky hooks |
| 07 | [Merge Strategies](07-git/07-merge-strategies.md) | Merge, squash, rebase — cuándo usar cada uno |
| 08 | [Rollback](07-git/08-rollback.md) | revert, reset, reflog, recuperación de commits |
| 09 | [Stash](07-git/09-stash.md) | git stash, pop, apply, drop, branches desde stash |

---

## 08 — Contenedores Docker (5 docs)

| # | Doc | Tema |
|---|-----|------|
| 01 | [Conceptos](08-contenedores/01-conceptos.md) | Imagen, contenedor, layer, registry, tag |
| 02 | [Dockerfile](08-contenedores/02-dockerfile.md) | Multi-stage, instrucciones, optimización de capas |
| 03 | [Compose](08-contenedores/03-compose.md) | docker-compose.yml, redes, volúmenes, profiles |
| 04 | [Proyecto Real](08-contenedores/04-proyecto-real.md) | Setup completo: API + frontend + postgres + seq |
| 05 | [Comandos](08-contenedores/05-comandos.md) | Referencia de comandos docker y docker compose |

---

## 09 — CI/CD e Infraestructura (4 docs)

| # | Doc | Tema |
|---|-----|------|
| 01 | [Checklists DevSecOps](09-cicd/01-checklists.md) | Shift-left, GitHub Actions CI gates, SBOM, SAST/DAST |
| 02 | [GitHub Actions](09-cicd/02-github-actions.md) | OIDC+AWS+ECS, reusable workflows, matrix builds |
| 03 | [Azure DevOps](09-cicd/03-azure-devops.md) | Pipelines YAML, service connections, environments |
| 04 | [Terraform](09-cicd/04-terraform.md) | HCL, módulos, estado S3+DynamoDB, remote state |

---

## 10 — Cloud (16 docs)

### Fundamentos Cloud

| Doc | Tema |
|-----|------|
| [AWS Basics](10-cloud/01-aws-basics.md) | Servicios core, regiones, AZs, cuentas |
| [Azure Basics](10-cloud/02-azure-basics.md) | Resource Groups, ARM, servicios equivalentes a AWS |
| [Linux + Nginx](10-cloud/03-linux-nginx.md) | Servidor web, reverse proxy, SSL/TLS, systemd |
| [Cloud Native](10-cloud/10-cloud-native.md) | 12-Factor App, contenedores, escalado horizontal |

### AWS en profundidad (6 docs)

| Doc | Tema |
|-----|------|
| [VPC / Networking](10-cloud/11-aws-vpc-networking.md) | VPC, subnets, IGW, NAT Gateway, Security Groups |
| [EC2](10-cloud/12-aws-compute-ec2.md) | Instancias, AMIs, ELB, Auto Scaling, User Data |
| [EBS + S3](10-cloud/13-aws-storage-ebs-s3.md) | Almacenamiento de bloque y objetos, lifecycle |
| [RDS / PostgreSQL](10-cloud/14-aws-rds-postgresql.md) | Multi-AZ, réplicas de lectura, backups, Parameter Groups |
| [IAM](10-cloud/15-aws-iam-seguridad.md) | Users, roles, policies, OIDC IRSA, least privilege |
| [CloudWatch](10-cloud/16-aws-cloudwatch.md) | Métricas, logs, dashboards, alarmas, X-Ray |

### Operaciones y Seguridad (6 docs)

| Doc | Tema |
|-----|------|
| [Observabilidad](10-cloud/04-observabilidad.md) | OpenTelemetry, Loki, Prometheus, Grafana, Tempo |
| [Secretos Producción](10-cloud/05-secretos-produccion.md) | AWS Secrets Manager, Parameter Store, rotación |
| [File Storage Tenant](10-cloud/09-file-storage-tenant.md) | S3 por tenant, presigned URLs, lifecycle |
| [Tracing Distribuido](10-cloud/09-tracing-distribuido.md) | Jaeger, Zipkin, W3C TraceContext |
| [Seguridad Web](10-cloud/07-seguridad-web.md) | OWASP Top 10, WAF, headers de seguridad |
| [Zero Trust](10-cloud/08-zero-trust.md) | Nunca confiar, siempre verificar — modelo de seguridad |
| [Costos](10-cloud/06-costos.md) | Optimización de costos AWS/Azure, Reserved Instances |

---

## 11 — Vibe Coding / AI-Assisted Development (6 docs)

| # | Doc | Tema |
|---|-----|------|
| 01 | [CLAUDE.md y AGENTS.md](11-vibe-coding/01-claude-md.md) | Instrucciones persistentes para LLMs como código |
| 02 | [Prompts Efectivos](11-vibe-coding/02-prompts-efectivos.md) | Técnicas de prompting para desarrollo de software |
| 03 | [AI Workflow](11-vibe-coding/03-ai-workflow.md) | Integrar LLMs en el ciclo de desarrollo |
| 04 | [Context Management](11-vibe-coding/04-context-management.md) | Gestión de contexto en sesiones largas |
| 05 | [LLMOps](11-vibe-coding/05-llmops.md) | Operaciones para sistemas basados en LLMs |
| 06 | [Arquitectura Agéntica](11-vibe-coding/06-arquitectura-agentica.md) | Catálogo de agentes, skills y reglas; placeholders y matriz de selección |

---

## Recursos adicionales

| Doc | Contenido |
|-----|-----------|
| [Glosario Maestro SaaS](Glosario_Maestro_SaaS.md) | 200+ términos del dominio SaaS Multi-Tenant |
| [Manual Práctico del Stack](Manual_Practico_Stack.md) | Referencia rápida del stack completo |
| [resources/books/](resources/books/README.md) | Índice de biblioteca técnica |

---

## Rutas de aprendizaje

Ver [`roadmap/`](roadmap/) para rutas estructuradas por objetivo:

| Roadmap | Para quién |
|---------|-----------|
| [C#](roadmap/roadmap-csharp.md) | Aprender el lenguaje desde cero hasta avanzado |
| [Backend .NET](roadmap/roadmap-backend-dotnet.md) | Construir APIs con ASP.NET Core |
| [Arquitectura](roadmap/roadmap-arquitectura.md) | Diseñar sistemas con Clean Architecture, CQRS, DDD |
| [SaaS Multi-Tenant](roadmap/roadmap-saas-multitenant.md) | Construir un producto SaaS completo |
| [DevOps](roadmap/roadmap-devops.md) | Git, Docker, CI/CD, Cloud, Terraform |
| [Frontend](roadmap/roadmap-frontend.md) | React 19, Vite, Tailwind v4, design system |
| [Patrones de Diseño](roadmap/roadmap-patrones.md) | GoF completo + patrones ASP.NET Core |

---

*Rogelio Arriaga Gonzalez — actualizado 2026-06-02*
