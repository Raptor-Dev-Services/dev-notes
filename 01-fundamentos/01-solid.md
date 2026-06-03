# 01 — SOLID

Los 5 principios que guían el diseño orientado a objetos. No son reglas arbitrarias. Cada uno resuelve un tipo de fragilidad concreta que aparece en proyectos reales.

---

## S — Single Responsibility Principle (SRP)

**Una clase tiene una sola razón para cambiar.**

"Razón para cambiar" = un actor o stakeholder que puede pedir una modificación. Si dos actores distintos pueden pedir cambios a la misma clase, tiene dos responsabilidades.

```csharp
// ❌ Viola SRP — mezcla lógica de negocio + persistencia + presentación
public class UserService
{
    public string GetUserJson(Guid id)
    {
        // 1. lógica de negocio
        var user = _db.QuerySingle<User>("SELECT ...", new { id });
        if (user is null) throw new NotFoundException();

        // 2. formato de respuesta (responsabilidad de presentación)
        return JsonSerializer.Serialize(new { user.Id, user.Email });
    }
}

// ✓ Cada clase tiene UNA responsabilidad
public class GetExampleUserHandler   // responsabilidad: orquestar el caso de uso
{
    private readonly IExampleUserRepository _repo;
    public async Task<GetExampleUserResponse> Handle(GetExampleUserRequest req, CancellationToken ct)
    {
        var user = await _repo.GetByPublicIdAsync(req.PublicId, ct);
        return user is null
            ? new GetExampleUserNotFoundFailure("No encontrado.")
            : new GetExampleUserSuccess(new ExampleUserDto(user));
    }
}

public class ExampleUsersSql          // responsabilidad: acceso a datos de ExampleUsers
public class GetExampleUserPresenter  // responsabilidad: traducir Response → ViewModel
```

**En este proyecto:** cada capa tiene responsabilidades distintas. El Handler no sabe de SQL. La clase Sql no sabe de HTTP. El Presenter no sabe de repositorios.

---

## O — Open/Closed Principle (OCP)

**Las entidades de software deben estar abiertas a extensión y cerradas a modificación.**

Agregar comportamiento nuevo sin tocar código existente. Se logra con abstracciones (interfaces, clases abstractas, polimorfismo).

```csharp
// ❌ Viola OCP — para agregar un nuevo tipo de notificación hay que modificar esta clase
public class NotificationService
{
    public void Send(string type, string message)
    {
        if (type == "email")      SendEmail(message);
        else if (type == "sms")   SendSms(message);
        else if (type == "push")  SendPush(message);
        // cada nuevo tipo → modificar esta clase → riesgo de romper lo que funciona
    }
}

// ✓ OCP — agregar un canal nuevo = agregar una clase nueva, sin tocar lo existente
public interface INotificationChannel
{
    Task SendAsync(string message, CancellationToken ct);
}

public sealed class EmailChannel   : INotificationChannel { ... }
public sealed class SmsChannel     : INotificationChannel { ... }
public sealed class PushChannel    : INotificationChannel { ... }
// nueva: public sealed class SlackChannel : INotificationChannel { ... }

public class NotificationService
{
    private readonly IEnumerable<INotificationChannel> _channels;
    public async Task SendAsync(string message, CancellationToken ct)
    {
        foreach (var ch in _channels)
            await ch.SendAsync(message, ct);
    }
}
```

**En este proyecto:** para agregar un nuevo caso de uso se agregan nuevas clases (Handler, Response, Presenter) sin modificar las existentes. `BaseApiController` está cerrado a modificación. Los controllers concretos lo extienden.

---

## L — Liskov Substitution Principle (LSP)

**Los objetos de una subclase deben poder reemplazar a los de la clase base sin alterar la corrección del programa.**

Si `B extends A`, cualquier código que espera `A` debe funcionar correctamente con una instancia de `B`. Viola LSP cuando la subclase debilita precondiciones, fortalece postcondiciones, o lanza excepciones que la base no lanzaría.

```csharp
// ❌ Viola LSP — ReadOnlyList dice que es una List pero no puede Insert
public class ReadOnlyList<T> : List<T>
{
    public new void Add(T item)    => throw new NotSupportedException();
    public new void Remove(T item) => throw new NotSupportedException();
    // código que espera List<T> y llama Add() se rompe
}

// ✓ LSP con records — las subclases son sustitutos válidos de la base
public abstract record GetExampleUserResponse : IResponse;

public sealed record GetExampleUserSuccess(ExampleUserDto Data)
    : GetExampleUserResponse, ISuccess<ExampleUserDto>;

public sealed record GetExampleUserNotFoundFailure(string Message)
    : GetExampleUserResponse, INotFoundFailure;

// El presenter puede tratar cualquier GetExampleUserResponse:
public Task Handle(GetExampleUserResponse notification, CancellationToken ct)
{
    if (notification is IFailure failure)   _viewModel.Fail(failure.Message);
    else if (notification is ISuccess<ExampleUserDto> s) _viewModel.Set(s);
    return Task.CompletedTask;
}
// GetExampleUserSuccess y GetExampleUserNotFoundFailure son sustitutos válidos
```

**En este proyecto:** la jerarquía de Responses (Success + Failure) respeta LSP. El Presenter puede trabajar con cualquier instancia de la respuesta base.

---

## I — Interface Segregation Principle (ISP)

**Los clientes no deben depender de interfaces que no usan.**

Interfaces grandes fuerzan a implementar métodos innecesarios. Interfaces pequeñas y específicas dan más flexibilidad.

```csharp
// ❌ Viola ISP — un repositorio de solo-lectura se ve forzado a implementar métodos de escritura
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken ct);
    Task InsertAsync(User user, CancellationToken ct);       // ← no lo necesito
    Task UpdateAsync(User user, CancellationToken ct);       // ← no lo necesito
    Task DeleteAsync(Guid id, CancellationToken ct);         // ← no lo necesito
}

// ✓ ISP — interfaces separadas por responsabilidad
public interface IUserReadRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken ct);
}

public interface IUserWriteRepository
{
    Task InsertAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

// El Handler de un query solo depende de lo que necesita:
public class GetUsersHandler
{
    private readonly IUserReadRepository _repo;  // no conoce Insert/Update/Delete
}
```

**Interfaces de Common.Results como ejemplo de ISP:**

```csharp
// Cada interfaz define UNA capacidad — no hay una "IGodResponse"
public interface ISuccess { }
public interface ISuccess<T> : ISuccess { T Data { get; } }
public interface IFailure       { string Message { get; } }
public interface INotFoundFailure : IFailure { }
public interface IConflictFailure : IFailure { }
public interface IValidationFailure : IFailure { }
```

**En este proyecto:** `IExampleUserRepository` expone solo los métodos que el dominio realmente necesita. Los handlers solo inyectan la interfaz mínima necesaria.

---

## D — Dependency Inversion Principle (DIP)

**Los módulos de alto nivel no deben depender de módulos de bajo nivel. Ambos deben depender de abstracciones.**

La dirección de dependencias en el código debe ser la inversa a la dirección del flujo de datos.

```csharp
// ❌ Viola DIP — Application depende de una clase concreta de Infrastructure
// Application sabe que hay PostgreSQL → cambiar de DB requiere tocar Application
public class GetExampleUserHandler
{
    private readonly ExampleUserRepository _repo;  // ← clase concreta de Infrastructure
    //                ↑ Application no debería conocer esto
}

// ✓ DIP — Application depende de una abstracción, Infrastructure implementa esa abstracción
// Domain define el contrato:
public interface IExampleUserRepository       // ← en Domain
{
    Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
}

// Infrastructure implementa el contrato:
public sealed class ExampleUserRepository     // ← en Infrastructure
    : IExampleUserRepository
{
    private readonly ExampleUsersSql _sql;
    public async Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
        => await _sql.GetByPublicIdAsync(publicId, ct);
}

// Application usa la abstracción:
public class GetExampleUserHandler
{
    private readonly IExampleUserRepository _repo;  // ← abstracción del Domain
    // Application no sabe si hay Postgres, MongoDB, o un array en memoria
}
```

**Diagrama de dependencias (este proyecto):**

```
Domain          ← define IExampleUserRepository (la abstracción)
    ↑
Application     ← usa IExampleUserRepository (depende de la abstracción)
    ↑
Infrastructure  ← implementa IExampleUserRepository (depende de la abstracción)

Host (DI)       ← conecta: services.AddScoped<IExampleUserRepository, ExampleUserRepository>()
```

El Host "invierte" la dependencia: en runtime Infrastructure implementa la interfaz del Domain, pero Domain no importa Infrastructure.

---

## Los 5 principios juntos en este proyecto

| Principio | Dónde se aplica |
|-----------|----------------|
| **SRP** | Handler solo orquesta, Sql solo accede a datos, Presenter solo formatea |
| **OCP** | Agregar caso de uso = agregar clases, no modificar las existentes |
| **LSP** | Success y Failure son sustitutos válidos de su Response base |
| **ISP** | `IExampleUserRepository` expone solo lo que necesita; `ISuccess<T>` interfaces pequeñas |
| **DIP** | Domain define interfaces, Infrastructure las implementa, Application las consume |

---

## Errores comunes

### SRP: confundir "una responsabilidad" con "un método"

Una clase puede tener muchos métodos y tener una sola responsabilidad. `ExampleUsersSql` tiene `GetByIdAsync`, `GetAllAsync`, `InsertAsync`, etc. Todos son responsabilidad de la misma cosa: acceso a datos de `ExampleUsers`.

### OCP: no significa nunca modificar código

Significa que el código maduro y probado debería cambiar lo menos posible. Un bug fix es válido. Agregar un campo a un DTO es válido. Lo que OCP evita es agregar comportamiento abriendo el mismo archivo cada vez.

### DIP: no es "usar interfaces en todos lados"

Es que los módulos de **alto nivel** (reglas de negocio) no dependan de los de **bajo nivel** (infraestructura concreta). Una clase utilitaria interna no necesita una interfaz solo porque DIP existe.

---

## Principios complementarios a SOLID

> Fuente: *Clean Code* (Martin) — Ch.2 Nombres, Ch.3 Funciones, Ch.17 Smells and Heuristics; *Clean Architecture* Ch.12-14 Cohesión y Acoplamiento de Componentes

### DRY — Don't Repeat Yourself

**Cada pieza de conocimiento debe tener una representación única, inequívoca y autoritativa en el sistema.**

No se trata solo de no copiar código. Se trata de no duplicar *conocimiento*. Dos bloques de código pueden ser similares en estructura pero representar lógica distinta; eso no viola DRY.

```csharp
// ❌ Viola DRY — la regla "email debe tener @" está duplicada en tres lugares
public class RegistrationService
{
    public void Register(string email)
    {
        if (!email.Contains('@')) throw new ArgumentException("Email inválido");
        // ...
    }
}

public class ProfileService
{
    public void UpdateEmail(string email)
    {
        if (!email.Contains('@')) throw new ArgumentException("Email inválido"); // duplicado
        // ...
    }
}

public class InvitationService
{
    public void Invite(string email)
    {
        if (!email.Contains('@')) throw new ArgumentException("Email inválido"); // duplicado
        // ...
    }
}

// ✓ DRY — la regla vive en un solo lugar (Value Object)
public sealed record Email
{
    public string Value { get; }
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new ArgumentException($"'{value}' no es un email válido.");
        Value = value.Trim().ToLowerInvariant();
    }
}

// Todos los servicios usan el mismo tipo:
public class RegistrationService  { public void Register(Email email) { ... } }
public class ProfileService       { public void UpdateEmail(Email email) { ... } }
public class InvitationService    { public void Invite(Email email) { ... } }
```

### KISS — Keep It Simple, Stupid

**La solución más simple que funciona correctamente es siempre preferible a la más sofisticada.**

La complejidad innecesaria es deuda técnica inmediata. Antes de agregar una capa de abstracción, preguntarse: ¿qué problema concreto resuelve hoy?

```csharp
// ❌ Over-engineered para un CRUD simple
public interface IExampleUserQueryStrategy { }
public class ActiveUserQueryStrategy : IExampleUserQueryStrategy { }
public class ExampleUserQueryStrategyFactory { ... }
public class ExampleUserQueryBuilder { ... }

// ✓ KISS — un repositorio con un método claro
public interface IExampleUserRepository
{
    Task<IEnumerable<ExampleUser>> GetAllActiveAsync(CancellationToken ct);
}
```

### YAGNI — You Aren't Gonna Need It

**No implementar funcionalidad hasta que sea necesaria.** No diseñar para requisitos hipotéticos futuros.

```csharp
// ❌ YAGNI violado — "por si algún día necesitamos soportar múltiples DBs"
public interface IDatabaseProvider { }
public class PostgreSqlProvider : IDatabaseProvider { }
public class SqlServerProvider  : IDatabaseProvider { } // nadie lo pidió
public class MongoDbProvider    : IDatabaseProvider { } // nadie lo pidió

// ✓ YAGNI — solo lo que se necesita hoy
public class ExampleUserRepository
{
    private readonly NpgsqlConnection _connection; // PostgreSQL, punto
}
```

YAGNI no significa ignorar la mantenibilidad. Significa no adelantarse a requisitos que no existen. Si mañana se necesita otra DB, se refactoriza entonces con información real.

---

## Cohesión y acoplamiento de componentes

> Fuente: *Clean Architecture* — Part IV Component Principles (Ch.12-14)

### Principio de Cierre Común (CCP)

Las clases que cambian juntas por las mismas razones deben estar en el mismo componente (proyecto/namespace). Las clases que cambian por razones distintas deben estar en componentes distintos.

```
// ✓ CCP aplicado en la estructura del back-template
GTM.Suite.Domain/           ← cambia cuando cambian las reglas de negocio
GTM.Suite.Application/      ← cambia cuando cambian los casos de uso
GTM.Suite.Infrastructure/   ← cambia cuando cambia la tecnología (DB, servicios externos)
GTM.Suite.WebApi/           ← cambia cuando cambia el protocolo HTTP
```

Cada proyecto tiene una razón de cambio diferente. Un cambio de base de datos no debería tocar `Domain`. Un cambio de regla de negocio no debería tocar `Infrastructure`.

### Estabilidad de componentes

Un componente **estable** es uno del que muchos otros dependen. Cambiarlo es costoso. Un componente **inestable** es uno que depende de muchos otros. Cambiarlo es fácil.

```
Estabilidad:    Domain (muy estable)
                    ↑
                Application
                    ↑
                Infrastructure (inestable — depende de tecnologías externas)
```

Las reglas de negocio (Domain) deben estar en el componente más estable. Las implementaciones concretas (Infrastructure) pueden cambiar sin arrastrar cambios hacia arriba.

---

## Nombrado limpio (Clean Code Ch.2)

Los nombres son el comentario más importante del código. Un buen nombre elimina la necesidad de un comentario.

```csharp
// ❌ Nombres que obligan a leer el cuerpo para entender qué hacen
public List<int[]> getThem()
{
    List<int[]> list1 = new();
    foreach (var x in theList)
        if (x[0] == 4) list1.Add(x);
    return list1;
}

// ✓ Nombres que explican la intención
public List<Cell> getFlaggedCells()
{
    List<Cell> flaggedCells = new();
    foreach (var cell in gameBoard)
        if (cell.IsFlagged()) flaggedCells.Add(cell);
    return flaggedCells;
}
```

**Reglas de nombrado aplicadas en este proyecto:**

| Tipo | Convención | Ejemplo |
|------|-----------|---------|
| Clases | Sustantivo o frase sustantiva | `ExampleUser`, `GetExampleUserHandler` |
| Métodos | Verbo o frase verbal | `GetByPublicIdAsync`, `UpdateEmail` |
| Booleanos | Pregunta afirmativa | `IsActive`, `HasPermission`, `EmailExists` |
| Interfaces | `I` + sustantivo | `IExampleUserRepository` |
| Handlers | `{Verbo}{Entidad}Handler` | `GetExampleUserHandler` |
| Requests | `{Verbo}{Entidad}Request` | `InsertExampleUserRequest` |

---

## Funciones limpias (Clean Code Ch.3)

```csharp
// ❌ Función con múltiples niveles de abstracción mezclados
public async Task ProcessUser(CreateUserDto dto)
{
    // nivel alto: validar
    if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentException("...");

    // nivel bajo: SQL directo
    var sql = "INSERT INTO Users (Email, CreatedAt) VALUES (@email, @now)";
    await _db.ExecuteAsync(sql, new { email = dto.Email, now = DateTime.UtcNow });

    // nivel alto: publicar evento
    await _mediator.Publish(new UserCreatedEvent(dto.Email));
}

// ✓ Un nivel de abstracción por función — cada función hace una cosa
public async Task<InsertExampleUserResponse> Handle(
    InsertExampleUserRequest req, CancellationToken ct)
{
    if (await _repo.EmailExistsAsync(req.Email, ct))
        return new InsertExampleUserConflictFailure("El email ya existe.");

    var user = ExampleUser.Create(req.FullName, req.Email);
    await _repo.InsertAsync(user, ct);
    await _mediator.Publish(new UserRegisteredEvent(user.PublicId, user.Email), ct);
    return new InsertExampleUserSuccess(new ExampleUserDto(user));
}
// Cada colaborador (_repo, _mediator) encapsula su propio nivel de abstracción
```

**Regla:** una función debe hacer una cosa, hacerla bien, y hacerla solo.


---

## Glosario

| Término | Definición |
|---------|-----------|
| SRP (Single Responsibility) | principio que establece que una clase debe tener una sola razón para cambiar |
| OCP (Open/Closed) | principio que establece que las entidades deben estar abiertas a extensión y cerradas a modificación |
| LSP (Liskov Substitution) | principio que garantiza que los subtipos pueden reemplazar a sus tipos base sin alterar el programa |
| ISP (Interface Segregation) | principio que establece que los clientes no deben depender de interfaces que no usan |
| DIP (Dependency Inversion) | principio que establece que los módulos de alto nivel no deben depender de los de bajo nivel |
| DRY (Don't Repeat Yourself) | principio que establece que cada pieza de conocimiento debe tener una representación única en el sistema |
| KISS (Keep It Simple) | principio que prefiere la solución más simple que funcione correctamente |
| YAGNI (You Aren't Gonna Need It) | principio que establece no implementar funcionalidad hasta que sea necesaria |
| CCP (Common Closure Principle) | las clases que cambian juntas por las mismas razones deben estar en el mismo componente |
| Value Object | objeto definido completamente por sus atributos, sin identidad propia, e inmutable |
| Acoplamiento | grado de dependencia entre módulos — bajo acoplamiento facilita el cambio independiente |
| Cohesión | medida de cuán relacionadas están las responsabilidades dentro de un componente |

---

*Rogelio Arriaga Gonzalez*
