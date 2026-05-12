# 01 · CLAUDE.md / AGENTS.md — instrucciones persistentes para el AI

## Problema que resuelve

Sin instrucciones persistentes, en cada sesión se tiene que re-explicar el stack, las convenciones del proyecto y las restricciones del equipo. `CLAUDE.md` (para Claude Code) y `AGENTS.md` (convención general de agentes) almacenan ese contexto en el repositorio y se cargan automáticamente al inicio de cada sesión.

## Qué es CLAUDE.md

`CLAUDE.md` es un archivo de instrucciones que Claude Code lee al iniciar en un directorio. Es el equivalente a un onboarding document, pero para el AI.

Claude Code lo carga en orden de precedencia:
1. `~/.claude/CLAUDE.md` — instrucciones globales del usuario (aplica a todos los proyectos)
2. `CLAUDE.md` en la raíz del repositorio — instrucciones del proyecto
3. `CLAUDE.md` en subdirectorios — instrucciones locales al directorio actual

## Qué incluir

### 1. Stack y versiones

```markdown
## Stack
- Backend: .NET 10, C# 13, ASP.NET Core, Dapper, PostgreSQL 16
- Frontend: React 19, Vite 7, Tailwind CSS v4, Headless UI
- Infraestructura: Docker, AWS ECS Fargate, ECR, RDS PostgreSQL
- CI/CD: GitHub Actions
```

### 2. Arquitectura del proyecto

```markdown
## Arquitectura
Clean Architecture con 4 capas:
- Domain: entidades, repositorios (interfaces), eventos
- Application: UseCases (handlers MediatR), DTOs, servicios de aplicación
- Infrastructure: implementaciones de repositorios, EF Core / Dapper, servicios externos
- WebApi: controllers, middleware, hubs SignalR

Patrón de nombres estándar:
- Handler: GetExampleUserHandler
- Request/Response: GetExampleUserRequest / GetExampleUserResponse
- Repository interface: IExampleUserRepository
- Implementación: ExampleUserRepository
```

### 3. Convenciones de código

```markdown
## Convenciones
- Idioma: español en comentarios y documentación, inglés en código
- Nombres de clases y métodos: PascalCase
- Variables locales: camelCase
- Result Pattern: todos los handlers retornan Result<T>
- Sin emojis en código ni documentación técnica
- Commits: Conventional Commits (feat/fix/chore/etc.)
```

### 4. Restricciones

```markdown
## Restricciones
- No usar Entity Framework para queries de lectura — siempre Dapper
- No crear métodos de más de 30 líneas sin justificación
- No modificar archivos en Infrastructure/Migrations/ directamente
- No commitear appsettings.Production.json con valores reales
- Tests de integración requieren TestContainers, no mocks de base de datos
```

### 5. Comandos frecuentes

```markdown
## Comandos
- Compilar: `dotnet build`
- Tests: `dotnet test`
- Levantar DB local: `docker compose up -d postgres`
- Migrations: `dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi`
- Frontend dev: `cd frontend && npm run dev`
```

## Ejemplo de CLAUDE.md completo (back-template)

```markdown
# CLAUDE.md — back-template

## Stack
- .NET 10 / C# 13 / ASP.NET Core 10
- PostgreSQL 16, Dapper (lecturas), EF Core (escrituras y migrations)
- Docker, GitHub Actions, AWS ECS Fargate

## Arquitectura
Clean Architecture 4 capas. Ver `docs/architecture.md` para diagrama.

Capa Application:
- Un UseCase = una carpeta con Handler, Request, Response
- Handlers retornan `Result<T>` — nunca lanzar excepciones para flujo de negocio

Naming: `GetExampleUserHandler`, `IExampleUserRepository`, `ExampleUserRepository`

## Convenciones
- Result Pattern en todos los handlers (ver `04-backend/01-result-pattern.md`)
- Validación con FluentValidation, no DataAnnotations
- No queries SQL inline en handlers — siempre en repositorio

## Restricciones
- No modificar `Common/` sin discutir primero
- No usar `var` cuando el tipo no es obvio
- Todos los tests de integración usan TestContainers

## Comandos
- Build: `dotnet build`
- Test: `dotnet test --configuration Release`
- Dev: `docker compose up -d && dotnet run --project WebApi`
```

## AGENTS.md — convención para otros agentes

`AGENTS.md` es la misma idea pero para agentes que no son Claude Code (OpenAI Codex, GitHub Copilot Workspace, etc.). El formato es el mismo — instrucciones en Markdown para el agente.

Si el repositorio se usa con múltiples herramientas AI, tener ambos archivos con el mismo contenido:

```
CLAUDE.md    ← Claude Code lee esto
AGENTS.md    ← otros agentes leen esto
```

## Cuándo actualizar CLAUDE.md

- Al agregar una nueva dependencia o patrón al proyecto
- Al cambiar la estructura de directorios
- Al establecer una nueva convención de equipo
- Al detectar que el AI repite el mismo error (agregar restricción explícita)

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| proyectos con más de 2 sesiones de trabajo con AI | proyectos de un solo uso o exploración |
| cuando el AI repite errores por desconocer el contexto | para instrucciones que cambian cada sesión |
| convenciones no obvias que el AI no puede inferir del código | información que ya está clara en el código |


> Fuente: *Building LLM Powered Applications* (Valentina Alto) — Ch.3 Building AI Agents with Tools

---

*Rogelio Arriaga Gonzalez*
