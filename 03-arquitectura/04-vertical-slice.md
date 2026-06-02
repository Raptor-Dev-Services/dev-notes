# 08 — Vertical Slice Architecture

Vertical Slice Architecture organiza el código por **feature** en lugar de por **capa técnica**. Es la alternativa principal a Clean Architecture y vale la pena entender cuándo elegir una sobre la otra.

> Fuente: *Architecting ASP.NET Core Applications* (Carl-Hugo Marcotte) — Ch.17 Vertical Slice Architecture

---

## La diferencia fundamental

```
Clean Architecture (horizontal layers):
back-template/
├── Domain/
│   └── Entities/               ← todas las entidades juntas
├── Application/
│   └── UseCases/               ← todos los handlers juntos
├── Infrastructure/
│   └── Repositories/           ← todos los repos juntos
└── WebApi/
    └── EndPoints/              ← todos los controllers juntos

Vertical Slice Architecture (features):
back-template/
└── Features/
    ├── Users/
    │   ├── GetUser/
    │   │   ├── GetUserQuery.cs        ← request, handler, response, presenter juntos
    │   │   ├── GetUserHandler.cs
    │   │   └── GetUserPresenter.cs
    │   ├── InsertUser/
    │   │   ├── InsertUserCommand.cs
    │   │   ├── InsertUserHandler.cs
    │   │   └── InsertUserPresenter.cs
    │   └── UsersController.cs
    └── Orders/
        ├── PlaceOrder/
        └── CancelOrder/
```

---

## Principio central — Jimmy Bogard

> "Minimize coupling between slices and maximize coupling within a slice."

Dos objetivos en tensión:

- **Minimizar el acoplamiento entre slices** → modificar un slice no requiere tocar otros
- **Maximizar el acoplamiento dentro de un slice** → todo el código de una feature vive junto (cohesión)

### Niveles de código compartido

```
Slice code        → código único de un feature (debería ser la MAYORÍA del código)
                    Alta cohesión, sin acoplamiento entre features
                    
Cross-slice code  → código compartido entre features del mismo dominio
                    (ej: entidad Shipment compartida entre Create, List y Details)
                    Menor cantidad que el slice code
                    
Global code       → código compartido entre dominios no relacionados
                    (ej: middleware de error handling, helpers de serialización)
                    MÍNIMO — es el mayor fuente de acoplamiento global
```

**Regla práctica:** escribir primero código de feature (slice-specific), refactorizar a código compartido solo cuando emerge la necesidad real — no anticipar abstracciones.

---

## Clean Architecture — ventajas y problemas

### Ventajas

- Separación técnica clara: Domain sin dependencias, Application solo lógica
- Un equipo puede trabajar en "la capa de Infrastructure" sin tocar otras
- Los patrones son bien conocidos (.NET tiene fuerte tradición de esta estructura)
- Fácil de razonar sobre la dirección de dependencias

### Problemas a escala

```
Agregar una feature "Crear Pedido":
    1. Domain/Entities/Orders/Order.cs
    2. Domain/Repositories/Orders/IOrderRepository.cs
    3. Application/UseCases/Orders/PlaceOrder/PlaceOrderRequest.cs
    4. Application/UseCases/Orders/PlaceOrder/PlaceOrderHandler.cs
    5. Application/UseCases/Orders/PlaceOrder/Responses/PlaceOrderResponse.cs
    6. Application/UseCases/Orders/PlaceOrder/Responses/PlaceOrderSuccess.cs
    7. Application/UseCases/Orders/PlaceOrder/Responses/PlaceOrderFailure.cs
    8. Infrastructure/Persistence/SQLDB/Main/Orders/OrdersSql.cs
    9. Infrastructure/Repositories/Orders/OrderRepository.cs
    10. WebApi/EndPoints/Orders/Presenters/PlaceOrderPresenter.cs
    11. WebApi/EndPoints/Orders/OrdersController.cs

→ Modificar 5 proyectos distintos para una sola feature
→ Un PR toca archivos en 5 carpetas distintas
→ Conflict de merge frecuente en ServiceCollectionEx.cs de cada capa
```

---

## Vertical Slice — ventajas y problemas

### Ventajas

```csharp
// Features/Users/GetUser/
// Todo lo de "obtener usuario" está en UN lugar
public sealed record GetUserQuery(Guid PublicId) : IRequest<GetUserResponse>;

public abstract record GetUserResponse : IResponse;
public sealed record GetUserSuccess(UserDto Data) : GetUserResponse, ISuccess<UserDto>;
public sealed record GetUserNotFoundFailure(string Message) : GetUserResponse, INotFoundFailure;

public sealed class GetUserHandler : IRequestHandler<GetUserQuery, GetUserResponse>
{
    private readonly IDbConnection _db;
    public async Task<GetUserResponse> Handle(GetUserQuery query, CancellationToken ct)
    {
        var user = await _db.QuerySingleOrDefaultAsync<UserDto>(
            "SELECT PublicId, FullName, Email FROM dbo.Users WHERE PublicId = @id",
            new { id = query.PublicId });
        return user is null
            ? new GetUserNotFoundFailure("No encontrado.")
            : new GetUserSuccess(user);
    }
}

public sealed class GetUserPresenter : IPresenter<GetUserResponse>
{
    // ...
}
```

- Toda la feature en un directorio → un PR toca una carpeta
- Los desarrolladores trabajan en features completas sin tocar otras
- Copiar/eliminar/mover una feature es trivial
- Cada slice puede tener su propio modelo, sin forzar una abstracción compartida

### Problemas

- **Duplicación:** dos features pueden tener SQL similar y nadie lo unifica
- **Consistencia difícil de mantener:** cada feature puede evolucionar diferente
- **Menos estructura en capas:** las dependencias son más libres, más fácil meter DB access directo en un Handler
- **Curva de entrada:** los desarrolladores Junior a veces prefieren la estructura explícita de capas

---

## Comparación directa

| Aspecto | Clean Architecture | Vertical Slice |
|---------|-------------------|----------------|
| Organización | Por capa técnica | Por feature/use case |
| PR de una feature | Toca 4-5 carpetas | Toca 1 carpeta |
| Reutilización | Alta (capas compartidas) | Baja (cada slice es independiente) |
| Duplicación | Poca (los repos se comparten) | Puede haber más |
| Equipo grande | Confictos frecuentes en capas | Equipos paralelos en features |
| Onboarding | Patrón conocido | Más libre, necesita convenciones |
| Testing | Unit tests por capa | Integration tests por feature |
| Complejidad | Alta (muchos proyectos) | Media (menos archivos cruzados) |

---

## Cuándo elegir cada una

### Elegir Clean Architecture cuando:

- El equipo conoce bien Clean Architecture
- El dominio es estable y las entidades se reusan en muchos casos de uso
- Se necesita testear las capas independientemente
- El proyecto tiene < 5-10 desarrolladores
- Este proyecto es el ejemplo: las entidades y repos se comparten entre muchos handlers

### Elegir Vertical Slice cuando:

- El equipo es grande (10+ devs) y trabajan en features paralelas
- Los features son muy independientes entre sí (poca lógica de dominio compartida)
- Se quiere maximizar autonomía de equipos
- Las features cambian frecuentemente y de forma independiente
- Hay muchos módulos/dominios distintos

---

## Vertical Slice sobre Clean Architecture (híbrido)

Una opción válida: mantener la separación de capas para el dominio y la infraestructura, pero organizar los casos de uso como vertical slices dentro de Application:

```
back-template/
├── Domain/                          ← entidades e interfaces (compartido)
├── Infrastructure/                  ← implementaciones (compartido)
└── Application/
    └── Features/                    ← slices dentro de Application
        ├── Users/
        │   ├── GetUser/             ← Request + Handler + Response juntos
        │   └── InsertUser/
        └── Orders/
            ├── PlaceOrder/
            └── CancelOrder/
```

Este híbrido da los beneficios de independencia por feature dentro de Application, manteniendo la arquitectura de capas para Domain e Infrastructure.

---

## Ejemplo completo de un slice

```csharp
// Features/Users/CreateUser/CreateUser.cs
// Todos los tipos en un solo archivo (patrón común en Vertical Slice)

// --- Request ---
public sealed record CreateUserCommand(string FullName, string Email)
    : IRequest<CreateUserResponse>;

// --- Responses ---
public abstract record CreateUserResponse : IResponse;

public sealed record CreateUserSuccess(Guid UserId)
    : CreateUserResponse, ISuccess;

public sealed record CreateUserConflict(string Message)
    : CreateUserResponse, IConflictFailure;

// --- Handler ---
public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IDbConnection _db;
    public CreateUserHandler(IDbConnection db) => _db = db;

    public async Task<CreateUserResponse> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var exists = await _db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM dbo.Users WHERE Email = @email)",
            new { email = cmd.Email });

        if (exists)
            return new CreateUserConflict("Email ya registrado.");

        var publicId = Guid.NewGuid();
        await _db.ExecuteAsync(
            """
            INSERT INTO dbo.Users (PublicId, FullName, Email, CreatedAtUtc)
            VALUES (@publicId, @fullName, @email, @now)
            """,
            new { publicId, fullName = cmd.FullName, email = cmd.Email, now = DateTime.UtcNow });

        return new CreateUserSuccess(publicId);
    }
}

// --- Presenter ---
public sealed class CreateUserPresenter : IPresenter<CreateUserResponse>
{
    private readonly ResultViewModel<UsersController> _viewModel;
    public CreateUserPresenter(ResultViewModel<UsersController> vm) => _viewModel = vm;

    public Task Handle(CreateUserResponse notification, CancellationToken ct)
    {
        if (notification is IFailure f)  _viewModel.Fail(f.Message);
        else if (notification is ISuccess) _viewModel.OK(new { message = "Usuario creado." });
        return Task.CompletedTask;
    }
}
```

---

## Decisión para este proyecto

Este proyecto usa Clean Architecture por sus ventajas de estructura clara y el patrón bien establecido en .NET. Si el proyecto crece a múltiples módulos grandes con equipos independientes, migrar la capa Application a Vertical Slices es el paso natural siguiente — sin tocar Domain ni Infrastructure.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Vertical Slice Architecture | estilo arquitectural donde el código se organiza por caso de uso (feature) en lugar de por capa técnica |
| Feature slice | unidad de organización que agrupa todos los elementos de un caso de uso: request, handler, respuesta y presenter |
| Clean Architecture | arquitectura en capas (Domain, Application, Infrastructure, WebApi) con dependencias apuntando hacia el dominio |
| Acoplamiento horizontal | dependencia entre features del mismo nivel; el principal problema que Vertical Slice busca eliminar |
| REPR (Request-Endpoint-Response) | variante de Vertical Slice para APIs donde cada endpoint tiene su propio handler y respuestas encapsuladas |
| Minimal API | forma de definir endpoints en ASP.NET Core sin Controllers, compatible con el patrón REPR |
| Co-location | práctica de colocar archivos relacionados en el mismo directorio o archivo para facilitar la navegación |
| Cross-cutting concern | preocupación transversal (logging, validación, autorización) que aplica a múltiples features y se implementa en el pipeline |
| Cohesión por feature | propiedad de un módulo de feature donde todos sus componentes colaboran para un único caso de uso |
| Acoplamiento por capa | patrón opuesto a Vertical Slice, donde los cambios en una capa técnica impactan a múltiples features |

---

*Rogelio Arriaga Gonzalez*
