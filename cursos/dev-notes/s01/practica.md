# S01 - Guía Hands-On

**Dev Notes**
**Temas:** Setup del repo Reddit-clone - Tour del back-template - SOLID en código real

---

## Bloque Setup del Repo

### Parte 1 - Crear el repositorio (20 min)

**Objetivo:** tener el repo `reddit-clone` en GitHub con la estructura del back-template.

```bash
# 1. Crear repo en GitHub (UI o CLI)
gh repo create reddit-clone --private --description "Reddit-clone · Dev Notes"

# 2. Clonar el back-template localmente
git clone <URL-del-back-template> reddit-clone
cd reddit-clone

# 3. Conectar al nuevo remote
git remote remove origin
git remote add origin git@github.com:<tu-org>/reddit-clone.git
git push -u origin main
```

**Estructura que deberías ver:**

```
reddit-clone/
├── Startup.Domain/
├── Startup.Application/
├── Startup.Infrastructure/
├── Startup.WebApi/
├── Startup.sln
└── docker-compose.yml
```

---

### Parte 2 - Renombrar proyectos (25 min)

**Objetivo:** adaptar el template al nombre del producto.

Renombrar en el siguiente orden para evitar romper referencias:

| Nombre en template | Nombre en Reddit-clone |
|-------------------|------------------------|
| `Startup.Domain` | `Reddit.Domain` |
| `Startup.Application` | `Reddit.Application` |
| `Startup.Infrastructure` | `Reddit.Infrastructure` |
| `Startup.WebApi` | `Reddit.WebApi` |
| `Startup.sln` | `Reddit.sln` |

**En VS Code o Rider:** usar renombrado global de namespace.
En la terminal:

```bash
# En PowerShell — renombrar carpetas
Rename-Item "Startup.Domain" "Reddit.Domain"
Rename-Item "Startup.Application" "Reddit.Application"
Rename-Item "Startup.Infrastructure" "Reddit.Infrastructure"
Rename-Item "Startup.WebApi" "Reddit.WebApi"
```

Verificar que compila:

```bash
dotnet build Reddit.sln
```

---

### Parte 3 - CLAUDE.md + AGENTS.md (20 min)

**Objetivo:** configurar el contexto para el agente de IA desde el primer commit.

Crear `AGENTS.md` en la raíz del repo con el siguiente contenido:

```markdown
# AGENTS.md — Reddit Clone

## Stack
- .NET 10 · Clean Architecture · Dapper · Serilog · PostgreSQL · Docker

## Estructura de capas
- Domain: entidades, value objects, interfaces de repositorio
- Application: handlers (CQRS), validators, DTOs
- Infrastructure: repositorios Dapper, migraciones SQL, servicios externos
- WebApi: controllers, middleware, Program.cs (DI root)

## Convenciones de naming
- Handler: `{Verbo}{Entidad}Handler` → GetPostHandler, InsertCommentHandler
- Request: `{Verbo}{Entidad}Request` → InsertUserRequest
- Response: `{Verbo}{Entidad}Response` → GetUserResponse
- Repository interface: `I{Entidad}Repository` → IUserRepository
- SQL class: `{Entidad}Sql` → UsersSql

## Reglas que NUNCA violar
- NO string interpolation en Serilog: usar Log.Information("User {Id}", id)
- NO .Result ni async void
- NO IRepository<T> genérico
- NO :latest como tag de Docker — usar :v1.x.x o SHA del commit
- NO registrar servicio Scoped dentro de un Singleton

## Flujo de un caso de uso
Request → Handler → Repository (interface) → Sql (Dapper) → Response → Presenter
```

---

#### Parte 3 _(continuación)_

Crear `CLAUDE.md` en la raíz con contexto del proyecto (ver plantilla en dev-notes).

Commit:

```bash
git add AGENTS.md CLAUDE.md
git commit -m "chore: add AGENTS.md and CLAUDE.md for AI context"
```

---

### Parte 4 - Primer commit limpio (15 min)

**Objetivo:** establecer el commit inicial con la configuración correcta.

Verificar `.gitignore` incluye:

```
bin/
obj/
*.user
appsettings.Development.json
.env
.DS_Store
```

Verificar que NO está en el repo:
- Ningún `appsettings.Development.json` con credenciales
- Ningún `.env` con secrets

Commit inicial:

```bash
git add .
git commit -m "chore: initial project setup from back-template"
git push origin main
```

---

## Bloque Tour del Back-Template y SOLID Aplicado

### Parte 5 - Tour guiado del back-template (45 min)

**Objetivo:** entender la arquitectura viendo código real, no diapositivas.

#### 5.1 Flujo de una request - seguirla de punta a punta

Abrir `GetExampleUserHandler.cs` y seguir el flujo:

```
HTTP GET /example-users/{id}
  → GetExampleUserController.GetAsync(publicId)
  → _mediator.Send(new GetExampleUserRequest(publicId))
  → GetExampleUserHandler.Handle(request, ct)
    → IExampleUserRepository.GetByPublicIdAsync(publicId, ct)
    → ExampleUsersSql.GetByPublicIdAsync(publicId, ct)   ← Dapper aquí
  → GetExampleUserResponse (Success o NotFound)
  → GetExampleUserPresenter.Handle(response, ct)
  → IActionResult
```

**Preguntas al grupo:**
- ¿Qué principio de SOLID se aplica en cada paso del flujo?
- ¿Dónde está el DIP? ¿Dónde está el SRP?
- ¿Qué pasaría si la lógica de negocio estuviera en el Controller?

---

#### 5.2 Identificar SOLID en el código real

Para cada principio, el grupo señala el ejemplo en el template:

**S - SRP:**
- `GetExampleUserHandler` -> única responsabilidad: orquestar el caso de uso
- `ExampleUsersSql` -> única responsabilidad: SQL de ExampleUsers
- `GetExampleUserPresenter` -> única responsabilidad: traducir Response a HTTP

**O - OCP:**
- Agregar un nuevo caso de uso = crear nuevos archivos, no modificar Handler existente
- `BaseApiController` está cerrado a modificación; los controllers lo extienden

**L - LSP:**
- `GetExampleUserSuccess` y `GetExampleUserNotFoundFailure` son sustitutos válidos de `GetExampleUserResponse`
- El Presenter trabaja con cualquier instancia de `GetExampleUserResponse` sin sorpresas

**I - ISP:**
- `IExampleUserRepository` expone solo los métodos que el dominio necesita
- `ISuccess<T>`, `IFailure`, `INotFoundFailure` - cada interfaz define una capacidad

**D - DIP:**
- `GetExampleUserHandler` inyecta `IExampleUserRepository` (abstracción del Domain)
- `ExampleUserRepository` (Infrastructure) implementa `IExampleUserRepository`
- El DI root (`Program.cs`) conecta: `services.AddScoped<IExampleUserRepository, ExampleUserRepository>()`

---

#### 5.3 DI Lifetimes en Program.cs (20 min)

Abrir `Program.cs` y localizar los registros de DI. Analizar juntos:

| Lifetime | Cuándo usar | Ejemplo en el template |
|----------|-------------|------------------------|
| **Singleton** | Una instancia para toda la vida de la app | `IMemoryCache`, `ILogger` |
| **Scoped** | Una instancia por request HTTP | Handlers, Repositories, DbConnection |
| **Transient** | Nueva instancia cada vez que se pide | Validators, servicios stateless ligeros |

**El error del quiz - definiciones invertidas:**
- Singleton ≠ "solo para DBs"
- Scoped ≠ "para objetos que cambian"
- Transient ≠ "para cosas temporales"

**Trampa real:** registrar un servicio Scoped dentro de un Singleton -> el Singleton captura el Scoped y lo convierte en Singleton de facto. .NET lanza `InvalidOperationException` al inicio.

---

#### 5.4 Code Smells - buscar y proponer fix (30 min)

**Ejercicio en equipo:** cada persona revisa un archivo del template y encuentra:

1. Un nombre que podría ser más descriptivo (naming smell)
2. Una función que hace más de una cosa (Long Method)
3. Una duplicación de lógica (Duplicated Code)


**Rogelio aplica el Boy Scout Rule en vivo:** refactoriza un code smell en tiempo real, commiteando después de cada paso pequeño.

```bash
# Flujo de refactoring seguro
git checkout -b refactor/extract-email-validation

# 1. Pequeño cambio
dotnet test   # ← siempre correr tests después de cada cambio

git commit -m "refactor(users): extract email validation to Email value object"

# 2. Siguiente cambio pequeño
dotnet test
git commit -m "refactor(users): use Email value object in InsertHandler"
```

---

#### 5.5 Configurar Conventional Commits (15 min)

El equipo instala y configura commitlint o usa la convención manualmente:

**Formato:** `<tipo>(<scope>): <descripción en imperativo>`

| Tipo | Cuándo |
|------|--------|
| `feat` | Nueva funcionalidad |
| `fix` | Corrección de bug |
| `refactor` | Cambio que no agrega feature ni corrige bug |
| `chore` | Tareas de mantenimiento (config, deps, scripts) |
| `docs` | Solo documentación |
| `test` | Solo tests |
| `ci` | Cambios en CI/CD |

```bash
# Ejemplos correctos
git commit -m "feat(auth): add JWT refresh token endpoint"
git commit -m "fix(users): handle null email in registration"
git commit -m "refactor(posts): extract pagination logic to helper"
```

---

## Bloque Code Review y Retrospectiva

Revisar el trabajo del día - Dar feedback - Asignar el homework.

### Checklist de code review - rúbrica S01

Usar la rúbrica entregada hoy:

| Criterio | / No: | Notas |
|----------|---------|-------|
| La solución compila sin warnings | | |
| Namespaces correctos (`Reddit.*`) | | |
| `AGENTS.md` en la raíz con todas las reglas | | |
| `CLAUDE.md` en la raíz con contexto del proyecto | | |
| `.gitignore` excluye `bin/`, `obj/`, `.env` | | |
| Commits siguen Conventional Commits | | |
| No hay credenciales en el repositorio | | |
| No hay `:latest` en ningún Dockerfile | | |
| Ningún `async void` ni `.Result` en el código | | |
| Handlers siguen la convención de naming | | |

---

### Retrospectiva S01 (15 min)

- ¿Qué fue lo más sorprendente del template?
- ¿Qué principio de SOLID era el más confuso antes de hoy?
- ¿Qué pregunta quedó sin responder?

### Asignación del homework

Ver `S01-homework.md` para las instrucciones completas.

---

## Checklist del facilitador

- [ ] Todos tienen el repo clonado y compilando
- [ ] Todos tienen `AGENTS.md` y `CLAUDE.md` en la raíz
- [ ] Todos entendieron el flujo de una request de punta a punta
- [ ] Todos pueden señalar SRP, OCP y DIP en el código real
- [ ] La distinción Singleton/Scoped/Transient quedó clara (no solo la definición - el cuándo)
- [ ] El homework fue asignado con criterio de aceptación claro
