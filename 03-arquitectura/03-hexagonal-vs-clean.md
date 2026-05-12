# 06 — Hexagonal Architecture vs Clean Architecture

Dos arquitecturas que se confunden frecuentemente porque resuelven el mismo problema con el mismo principio. Entender las diferencias ayuda a razonar mejor sobre las decisiones del proyecto.

> Fuente: *Architecting ASP.NET Core Applications* (Carl-Hugo Marcotte) — Ch.7 Layering and Clean Architecture

---

## El problema que ambas resuelven

La lógica de negocio no debe depender de detalles técnicos — ni de la base de datos, ni del framework web, ni del protocolo de comunicación. Si cambias de PostgreSQL a MongoDB, o de REST a gRPC, la lógica de negocio no debería cambiar.

```
Problema:
  Lógica de negocio → SQL directo → acoplada a PostgreSQL
  Lógica de negocio → HttpContext → acoplada a ASP.NET Core
  Lógica de negocio → File.ReadAll → acoplada al sistema de archivos

Solución (ambas arquitecturas):
  Lógica de negocio → Interface → [cualquier implementación]
```

---

## Hexagonal Architecture (Ports & Adapters)

Propuesta por Alistair Cockburn en 2005. La metáfora: la aplicación es un hexágono con puertos (interfaces) en cada lado. Los adaptadores conectan el mundo exterior a esos puertos.

```
                    ┌─────────────────────┐
   REST Controller  │                     │  Test Double
   ──── Adapter ───►│   PORT (in)         │◄──── Adapter ────
                    │                     │
   gRPC Handler     │   APLICACIÓN        │  PostgreSQL Repo
   ──── Adapter ───►│   (lógica de        │◄──── Adapter ────
                    │    negocio)         │
   CLI Command      │   PORT (out)        │  Redis Cache
   ──── Adapter ───►│                     │◄──── Adapter ────
                    └─────────────────────┘
```

**Puerto de entrada (Driving/Primary Port):** define cómo el mundo exterior llama a la aplicación.

```csharp
// Puerto de entrada — lo que la aplicación expone para ser llamada
public interface IUserUseCases
{
    Task<GetExampleUserResponse> GetByIdAsync(Guid publicId, CancellationToken ct);
    Task<InsertExampleUserResponse> RegisterAsync(string fullName, string email, CancellationToken ct);
}

// Adaptador de entrada — conecta REST al puerto
[Route("api/example/users")]
public sealed class ExampleUsersController : BaseApiController
{
    // El controller es un ADAPTADOR que traduce HTTP → IUserUseCases
}

// Otro adaptador de entrada — conecta CLI al mismo puerto
public class UserCli
{
    private readonly IUserUseCases _useCases;
    public async Task RunAsync(string[] args)
    {
        var result = await _useCases.GetByIdAsync(Guid.Parse(args[0]), default);
        // ...
    }
}
```

**Puerto de salida (Driven/Secondary Port):** define lo que la aplicación necesita del mundo exterior.

```csharp
// Puerto de salida — lo que la aplicación necesita
public interface IExampleUserRepository      // ← Puerto de salida (en Domain)
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task InsertAsync(ExampleUser user, CancellationToken ct);
}

// Adaptador de salida — conecta el puerto a PostgreSQL
public sealed class ExampleUserRepository    // ← Adaptador de salida (en Infrastructure)
    : IExampleUserRepository
{
    private readonly ExampleUsersSql _sql;
    // ...
}

// Otro adaptador de salida — para tests
public sealed class InMemoryExampleUserRepository : IExampleUserRepository
{
    private readonly List<ExampleUser> _store = new();
    public Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
        => Task.FromResult(_store.FirstOrDefault(u => u.PublicId == publicId));
    // ...
}
```

---

## Clean Architecture

Propuesta por Robert C. Martin en 2012. La metáfora: capas concéntricas donde las dependencias solo apuntan hacia adentro (hacia el centro).

```
┌──────────────────────────────────────────┐
│  Infrastructure / Frameworks & Drivers   │
│  ┌────────────────────────────────────┐  │
│  │  Interface Adapters                │  │
│  │  (Controllers, Presenters, Repos)  │  │
│  │  ┌──────────────────────────────┐  │  │
│  │  │  Application Business Rules  │  │  │
│  │  │  (Use Cases / Handlers)      │  │  │
│  │  │  ┌────────────────────────┐  │  │  │
│  │  │  │  Enterprise Business   │  │  │  │
│  │  │  │  Rules (Entities)      │  │  │  │
│  │  │  │  Domain                │  │  │  │
│  │  │  └────────────────────────┘  │  │  │
│  │  └──────────────────────────────┘  │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
        ← dependencias apuntan hacia adentro
```

**En este proyecto:**

```
Domain          ← Enterprise Business Rules (entidades, interfaces de repo)
Application     ← Application Business Rules (handlers, DTOs, requests)
Infrastructure  ← Interface Adapters + Frameworks (repos concretos, SQL)
WebApi          ← Interface Adapters (controllers, presenters)
Host            ← Frameworks & Drivers (composition root, Program.cs)
```

---

## Diferencias clave

| Aspecto | Hexagonal | Clean Architecture |
|---------|-----------|-------------------|
| **Metáfora** | Hexágono con puertos | Capas concéntricas |
| **Foco** | Separar driving/driven adapters | Regla de dependencia (hacia adentro) |
| **Capas** | No define capas internas | Define capas explícitas |
| **Puertos** | Concepto explícito (in/out) | Interfaces en capas internas |
| **Adapters** | Concepto explícito | Controllers/Repos son implícitamente adapters |
| **Entrada** | Driving adapters (REST, CLI, tests) | Interface Adapters layer |
| **Salida** | Driven adapters (DB, cache, email) | Infrastructure layer |

**En la práctica:** Clean Architecture agrega la estructura en capas sobre el concepto de puertos y adaptadores de Hexagonal. Son complementarias — este proyecto usa Clean Architecture con la mentalidad de Ports & Adapters.

---

## Cómo se ve en el código

```
Hexagonal         → Clean Architecture    → Este proyecto
──────────────────────────────────────────────────────────
Port (in)         → Use Case Interface    → IRequest + IRequestHandler
Driving Adapter   → Controller            → ExampleUsersController
Core Application  → Application layer     → GetExampleUserHandler
Port (out)        → Repository Interface  → IExampleUserRepository
Driven Adapter    → Infrastructure        → ExampleUserRepository + ExampleUsersSql
```

```csharp
// Puerto de entrada (Hexagonal) = IRequest (Clean Application layer)
public sealed record GetExampleUserRequest(Guid PublicId)
    : IRequest<GetExampleUserResponse>;

// Adaptador de entrada (Hexagonal) = Controller (Clean Interface Adapters)
public sealed class ExampleUsersController : BaseApiController
{
    // traduce: HTTP GET /api/example/users/{id} → GetExampleUserRequest
}

// Puerto de salida (Hexagonal) = Interface en Domain (Clean Enterprise Rules)
public interface IExampleUserRepository { ... }

// Adaptador de salida (Hexagonal) = Repository impl (Clean Infrastructure)
public sealed class ExampleUserRepository : IExampleUserRepository { ... }
```

---

## La Dependency Rule

La regla fundamental de Clean Architecture (equivalente al concepto de puertos en Hexagonal):

> **Las dependencias de código solo pueden apuntar hacia adentro.** Nada en una capa interna puede conocer nada de una capa externa.

```
✓ Application conoce Domain
✓ Infrastructure conoce Domain
✓ WebApi conoce Application
✗ Domain conoce Infrastructure     ← viola la regla
✗ Application conoce Infrastructure ← viola la regla
✗ Domain conoce WebApi              ← viola la regla
```

Esta regla se implementa con DI: la capa externa implementa la interface definida en la capa interna. La capa interna nunca importa la externa.

---

## Onion Architecture — la tercera variante

Propuesta por Jeffrey Palermo. Muy similar a Clean Architecture:

```
Domain Model (centro)
    → Domain Services
        → Application Services
            → Infrastructure / UI / Tests
```

La diferencia principal: Onion nombra "Domain Services" explícitamente (lógica que no pertenece a ninguna entidad pero es de dominio puro). Clean Architecture los llama "Use Cases".

**Los tres son la misma idea expresada diferente.** Aprender uno es aprender todos.

---

## ¿Cuál usar?

Para este proyecto y la mayoría de backends .NET: **Clean Architecture con mentalidad Hexagonal**.

- Clean Architecture da la estructura de capas concreta
- La mentalidad de puertos/adaptadores ayuda a razonar sobre qué es una interface y qué es una implementación
- No mezclar terminologías en el mismo proyecto — el equipo necesita una sola metáfora

**Cuándo podría ser mejor Vertical Slice** (ver doc 08): equipos grandes, muchos módulos independientes, cuando las capas horizontales crean más fricción que valor.


---

*Rogelio Arriaga Gonzalez*
