<!-- _class: lead -->

# S01 - Fundamentos & Setup

### Dev Notes

**Rogelio Arriaga - Raptor Dev Services**

---

## Agenda del día

- **Teoría:** paradigmas - arquitecturas - SOLID - DRY/KISS/YAGNI
- **Hands-on:** setup del repo Reddit-clone
- **Desarrollo:** tour del back-template - SOLID en código real
- Code review - retrospectiva - homework

---

## El producto: Reddit-clone

```
Módulos del producto de ejemplo:

  Auth · Usuarios · Comunidades · Posts · Comentarios
  Votos · Feed (Hot/Top/New) · Búsqueda FTS · Notificaciones
  Media Upload · Real-time (SignalR) · Caching · Seguridad
  Frontend · DevOps (Docker · CI/CD · AWS)
```

**Este producto ES el examen.**

No hay proyectos de práctica separados. Todo lo que se aprende
se aplica inmediatamente al Reddit-clone.

---

## El stack

```
Back-end:    .NET 10 · Clean Architecture · CQRS · Dapper · Serilog
Base datos:  PostgreSQL 16 · Redis
Auth:        JWT HS256 · BCrypt · Refresh Tokens
Front-end:   React 19 · Vite · Tailwind v4 · TanStack Query
DevOps:      Docker multi-stage · GitHub Actions (OIDC) · AWS
AI Workflow: Claude Code · CLAUDE.md · AGENTS.md
```

---

## Lo que recibes hoy

| Entregable | Qué es |
|------------|--------|
| `back-template` | .NET 10 - Clean Architecture completa - JWT - Dapper - Serilog |
| `dev-notes` | 56 documentos técnicos - tu base de conocimiento del stack |
| `AGENTS.md` | Reglas para Claude Code/Cursor adaptadas al proyecto |

---

<!-- _class: lead -->

# Parte 1
## Paradigmas y Arquitecturas

---

## Paradigmas de programación

| Paradigma | Define | Cuándo usarlo en .NET |
|-----------|--------|-----------------------|
| **Procedural** | Secuencia de instrucciones | Scripts, migrations SQL |
| **OOP** | Objetos con estado + comportamiento | Entidades del dominio |
| **Funcional** | Funciones puras, inmutabilidad | LINQ, pipelines, transformaciones |

Los tres coexisten en .NET.

Un `Handler` es OOP - LINQ en repositorios es funcional ·
un script de seed es procedural.

---

## Arquitecturas - el espectro

```
Monolito          Monolito Modular       Microservicios
─────────         ────────────────       ──────────────
Un binario        Un binario,            N binarios
Una DB            módulos bien           Una DB por servicio
Sin fronteras     separados por          Comunicación por red
internas          feature/dominio

Simple            Escalable              Complejo desde el día 1
Fácil al inicio   Hasta ~40-50 devs      Requiere DevOps maduro
```

**Reddit-clone = Monolito Modular**

---

## El error más común - microservicios desde el día uno

**3 de 4 lo eligieron en el quiz. ¿Por qué es un error?**

- Equipo de 6 personas -> la complejidad operacional no se justifica
- El dominio aún no está completamente definido
- Microservicios requieren: service mesh, observabilidad distribuida, CI/CD maduro por servicio
- Sin CD maduro, el "deploy independiente" se convierte en coordinación manual

> *"Don't start with microservices. Start with a monolith and
> extract microservices as you learn where the boundaries are."*
> - Martin Fowler

Sí: **Monolito modular -> extraer servicios cuando el dominio lo justifica**

---

## Monolito Modular - estructura del back-template

```
el-producto/
├── Domain/             ← Entidades, Value Objects, interfaces de repositorio
├── Application/        ← Handlers, Requests, Responses, DTOs
│   └── UseCases/       ← un sub-folder por caso de uso
├── Infrastructure/     ← PostgreSQL (Dapper), Redis, S3
├── WebApi/             ← EndPoints, Presenters, RequestBodies
├── Host/               ← Program.cs, appsettings.json, DI wiring
└── Common/             ← ResultViewModel, interfaces de resultado, Mediator
```

Cada sub-folder de `Application/UseCases/` es un **módulo lógico** del monolito.
Si un módulo crece demasiado, se puede extraer a microservicio.

---

## Clean Architecture - las capas del back-template

```
       ┌──────────────────────────────────────┐
       │                Host                  │ ← Program.cs, appsettings, DI root
       ├──────────────────────────────────────┤
       │               WebApi                 │ ← EndPoints, Presenters, Middleware
       ├──────────────────────────────────────┤
       │          Infrastructure              │ ← PostgreSQL (Dapper), Redis, S3
       ├──────────────────────────────────────┤
       │           Application                │ ← Handlers, Requests, DTOs
       ├──────────────────────────────────────┤
       │             Domain                   │ ← Entidades, Interfaces (el núcleo)
       └──────────────────────────────────────┘
       ╌╌╌╌╌╌╌╌ Common (transversal) ╌╌╌╌╌╌╌╌
       ResultViewModel · Mediator · ISuccess · IFailure

Regla de dependencias: las flechas siempre apuntan hacia adentro.
Domain no conoce Application. Application no conoce Infrastructure.
```

---

## Twelve-Factor App - los más críticos para hoy

| # | Factor | Qué dice | Cómo lo aplicamos |
|---|--------|----------|-------------------|
| I | Codebase | Un repo, muchos deploys | GitHub -> staging -> prod |
| III | Config | Config en variables de entorno | `appsettings.json` + env vars, nunca hardcoded |
| VI | Processes | Stateless | No guardar estado en memoria del servidor |
| X | Dev/prod parity | Dev ≈ Prod | Docker resuelve esto desde S1 |
| XI | Logs | Eventos a stdout | Serilog -> structured logging |

---

<!-- _class: lead -->

# Parte 2
## SOLID

---

## SOLID - 5 principios - 5 fragilidades

| Letra | Principio | Fragilidad que resuelve |
|-------|-----------|------------------------|
| **S** | Single Responsibility | Clases que cambian por múltiples razones |
| **O** | Open/Closed | Agregar features rompe código existente |
| **L** | Liskov Substitution | Herencias que engañan al compilador |
| **I** | Interface Segregation | Implementar métodos innecesarios |
| **D** | Dependency Inversion | Alto nivel atado a bajo nivel (tecnologías) |

---

## S - Single Responsibility Principle

**Una clase tiene una sola razón para cambiar.**

```csharp
// Mal: UserService: mezcla negocio + persistencia + presentación
public class UserService {
    public string GetUserJson(Guid id) {
        var user = _db.QuerySingle<User>("SELECT * ...", new { id });
        return JsonSerializer.Serialize(new { user.Id, user.Email });
    }
}

// Bien: Responsabilidades separadas
public class GetUserHandler   { }  // orquesta el caso de uso
public class ExampleUsersSql  { }  // accede a datos (SQL)
public class GetUserPresenter { }  // serializa → ViewModel
```

---

## O - Open/Closed Principle

**Abierto a extensión - Cerrado a modificación**

```csharp
// Mal: Agregar canal nuevo → tocar esta función → riesgo de romper lo existente
public void Send(string type, string message) {
    if (type == "email")    SendEmail(message);
    else if (type == "sms") SendSms(message);
}

// Bien: Nuevo canal = nueva clase, nada existente cambia
public interface INotificationChannel {
    Task SendAsync(string message, CancellationToken ct);
}
public sealed class EmailChannel : INotificationChannel { ... }
public sealed class SmsChannel   : INotificationChannel { ... }
// Para agregar WebPush → solo creo WebPushChannel : INotificationChannel
```

---

## L - Liskov Substitution Principle

**Una subclase debe poder reemplazar a su base sin romper el programa.**

```csharp
// Mal: ReadOnlyList "hereda" de List pero lanza si llamas Add()
public class ReadOnlyList<T> : List<T> {
    public new void Add(T item) => throw new NotSupportedException();
    // Código que espera List<T> y llama Add() → explota en runtime
}

// Bien: Records — sustitutos válidos (el Presenter trata cualquier Response)
public abstract record GetUserResponse;
public sealed record GetUserSuccess(UserDto Data)       : GetUserResponse;
public sealed record UserNotFoundFailure(string Msg)    : GetUserResponse;
// GetUserSuccess y UserNotFoundFailure son sustitutos válidos de GetUserResponse
```

---

## I - Interface Segregation Principle

**Los clientes no deben depender de interfaces que no usan.**

```csharp
// Mal: Handler de query conoce métodos de escritura que no necesita
public interface IUserRepository {
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task InsertAsync(User user, CancellationToken ct);    // ← no lo necesito
    Task UpdateAsync(User user, CancellationToken ct);    // ← no lo necesito
}

// Bien: Interfaces segregadas — cada Handler depende solo de lo que usa
public interface IUserReadRepository  { Task<User?> GetByIdAsync(...); }
public interface IUserWriteRepository { Task InsertAsync(...); Task UpdateAsync(...); }

public class GetUserHandler {
    private readonly IUserReadRepository _repo;  // no conoce Insert ni Update
}
```

---

## D - Dependency Inversion Principle

**Los módulos de alto nivel no deben depender de los de bajo nivel.**

```csharp
// Mal: Application sabe que existe PostgreSQL
public class GetUserHandler {
    private readonly UserRepository _repo;   // clase concreta de Infrastructure
}   // cambiar de DB → modificar Application → viola DIP

// Bien: Application depende de abstracción · Infrastructure la implementa
// En Domain:
public interface IUserRepository { Task<User?> GetByIdAsync(Guid id, CancellationToken ct); }
// En Infrastructure:
public sealed class UserRepository : IUserRepository { ... }
// En Application:
public class GetUserHandler {
    private readonly IUserRepository _repo;  // no sabe si es Postgres o cualquier otra cosa
}
// En WebApi (DI root): services.AddScoped<IUserRepository, UserRepository>();
```

---

## SOLID en el back-template - resumen

| Principio | Dónde lo ves en el template |
|-----------|----------------------------|
| **SRP** | `GetExampleUserHandler` orquesta - `ExampleUsersSql` accede a datos - `GetExampleUserPresenter` formatea |
| **OCP** | Agregar caso de uso = agregar `Handler + Response + Sql` sin tocar los existentes |
| **LSP** | `GetExampleUserSuccess` y `GetExampleUserNotFoundFailure` son sustitutos válidos de `GetExampleUserResponse` |
| **ISP** | `ISuccess<T>`, `IFailure`, `INotFoundFailure` - interfaces de un solo propósito |
| **DIP** | Domain define `IExampleUserRepository` - Infrastructure implementa - Application consume |

**En el hands-on lo veremos en código real.**

---

<!-- _class: lead -->

# Parte 3
## DRY - KISS - YAGNI - Clean Code

---

## DRY - Don't Repeat Yourself

**Cada pieza de conocimiento: una representación única, inequívoca y autoritativa.**

```csharp
// Mal: La regla "email válido" duplicada en 3 handlers
if (!email.Contains('@')) return Failure("Email inválido.");  // InsertUserHandler
if (!email.Contains('@')) return Failure("Email inválido.");  // UpdateUserHandler
if (!email.Contains('@')) return Failure("Email inválido.");  // InviteHandler

// Bien: La regla vive en UN solo lugar — Value Object
public sealed record Email {
    public string Value { get; }
    public Email(string v) {
        if (string.IsNullOrWhiteSpace(v) || !v.Contains('@'))
            throw new ArgumentException("Email inválido.");
        Value = v.Trim().ToLowerInvariant();
    }
}
// Todos los handlers: var email = new Email(request.Email);
```

---

## KISS - Keep It Simple, Stupid

**La solución más simple que funciona correctamente es siempre la mejor.**

```csharp
// Mal: Over-engineered para un CRUD de usuarios
public interface IUserQueryStrategy        { }
public class ActiveUserQueryStrategy       : IUserQueryStrategy { }
public class UserQueryStrategyFactory      { ... }
public class UserQueryDirector             { ... }

// Bien: Un repositorio con un método claro
public interface IUserRepository {
    Task<IEnumerable<User>> GetAllActiveAsync(CancellationToken ct);
}
```

> Antes de agregar una abstracción: ¿qué problema concreto resuelve **hoy**?

---

## YAGNI - You Aren't Gonna Need It

**No implementar funcionalidad hasta que sea concretamente necesaria.**

```csharp
// Mal: "Por si acaso necesitamos soportar múltiples DBs algún día..."
public interface IDatabaseProvider         { }
public class PostgreSqlProvider            : IDatabaseProvider { } // usamos esto
public class SqlServerProvider             : IDatabaseProvider { } // nadie lo pidió
public class MongoDbProvider               : IDatabaseProvider { } // nadie lo pidió

// Bien: Solo lo que se necesita hoy
public class UserRepository {
    private readonly NpgsqlConnection _conn;  // PostgreSQL. Punto.
}
```

YAGNI habla de **funcionalidad especulativa**, no de testing.

---

## Clean Code - Naming - convenciones del template

| Tipo | Convención | Ejemplo |
|------|-----------|---------|
| Clase | Sustantivo | `User`, `Community`, `Post` |
| Método | Verbo | `GetByPublicIdAsync`, `CreatePost` |
| Booleano | Pregunta afirmativa | `IsActive`, `HasPermission`, `CanDelete` |
| Interface | `I` + sustantivo | `IUserRepository` |
| Handler | `{Acción}{Entidad}Handler` | `GetUserHandler`, `InsertPostHandler` |
| Request | `{Acción}{Entidad}Request` | `InsertUserRequest` |
| Response | `{Acción}{Entidad}Response` | `GetUserResponse` |

---

## Code Smells - señales de alerta

| Smell | Síntoma | Refactoring |
|-------|---------|-------------|
| **Long Method** | Función de 60+ líneas | Extract Method |
| **Duplicated Code** | Misma lógica en ≥3 lugares | Value Object / shared helper |
| **Long Parameter List** | 5+ parámetros | Parameter Object (Command/Request) |
| **Feature Envy** | Método usa más datos de otra clase | Mover el método |
| **Switch por tipo** | `if/else if/else` sobre el tipo en varios archivos | Polimorfismo (Strategy) |

**Boy Scout Rule:** "Deja el código más limpio de como lo encontraste."

---

## AI Workflow desde el día 1

**CLAUDE.md y AGENTS.md** - reglas que Claude Code lee antes de modificar código.

```markdown
## Reglas que NUNCA violar
- No usar string interpolation en Serilog
  Sí: Log.Information("User {Id} created", userId)
  No: Log.Information($"User {userId} created")
- No usar .Result ni async void
- No usar IRepository<T> genérico
- No usar :latest como tag de Docker — usar :v1.x.x o SHA
- No registrar Scoped dentro de un Singleton
```

Hoy configuramos esto en el repo.
El agente seguirá estas reglas en **cada** sesión.

---

## Hands-on
### Setup del repo (el producto)

1. Crear repo en GitHub (nombre provisional - el nombre final se define en S02)
2. Clonar el `back-template` como base - los namespaces del template vienen listos
3. Crear `CLAUDE.md` + `AGENTS.md` en la raíz del repo
4. Configurar `.gitignore` y `EditorConfig`
5. Primer commit: `chore: initial project setup from back-template`

**Instrucciones detalladas -> guía hands-on S01**

---

## Homework S01

### Lecturas (2-3 horas)
- `dev-notes/01-fundamentos/01-solid.md` - releer con el template abierto
- `dev-notes/01-fundamentos/02-clean-code.md` - capítulos de naming y funciones
- *Clean Code* (Martin) - Ch.2 Meaningful Names - Ch.3 Functions

### Ejercicio (1 hora)
En tu fork del template, abrir un PR con comentarios en el código que identifiquen:

1. Un ejemplo de **SRP aplicado** (¿dónde? ¿por qué?)
2. Un ejemplo de **DIP aplicado** (¿dónde? ¿por qué?)
3. Un **code smell** que encuentres + propuesta de fix


---

## S02 - Preview

**DRY - KISS - YAGNI profundo - Architecture Decision Records**

- ¿Por qué ADRs? El equipo que no documenta sus decisiones las repite.
- ADR-001: ¿Por qué monolito modular y no microservicios?
- ADR-002: ¿Por qué PostgreSQL?
- ADR-003: ¿Por qué Dapper sobre EF Core para queries?
- Refactoring kata con el Reddit-clone


---

<!-- _class: lead -->

# ¡A trabajar!
## Hands-on
### Guía en el documento S01-hands-on
