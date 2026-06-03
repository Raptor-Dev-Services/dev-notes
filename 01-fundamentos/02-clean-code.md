# 02 — Clean Code: Código limpio y legible

El código se escribe una vez y se lee decenas de veces. Clean Code (Robert C. Martin) establece que el código debe ser tan claro que el lector no necesite comentarios para entenderlo.

---

## Por qué importa la legibilidad

```csharp
// ❌ Código técnicamente correcto pero ilegible
public List<int[]> getThem()
{
    var list1 = new List<int[]>();
    foreach (var x in theList)
        if (x[0] == 4) list1.Add(x);
    return list1;
}

// ✓ El mismo código con nombres significativos
public List<Cell> GetFlaggedCells()
{
    var flaggedCells = new List<Cell>();
    foreach (var cell in gameBoard)
        if (cell.IsFlagged()) flaggedCells.Add(cell);
    return flaggedCells;
}
```

La lógica es idéntica. La diferencia es cuánto tiempo tarda el lector en entender qué hace.

---

## Nombres significativos
> Fuente: *Clean Code* (Martin) — Ch.2 Meaningful Names

### Clases y métodos

```csharp
// Reglas:
// Clase = sustantivo o frase sustantiva
// Método = verbo o frase verbal
// Booleano = pregunta (IsActive, HasPermission, CanDelete)

// ❌ Nombres vagos o abreviados
public class DU { }             // ¿qué es DU?
public class Manager { }        // ¿qué maneja?
public void Process() { }       // ¿procesa qué?
public bool Flag { get; set; }  // ¿qué flag?

// ✓ Nombres que revelan intención
public sealed class ExampleUser { }
public sealed class ExampleUserRepository { }
public void RegisterUser(string fullName, string email) { }
public bool IsEmailVerified { get; private set; }
```

### Una palabra por concepto

```csharp
// ❌ Varios nombres para el mismo concepto
GetExampleUser()   // en un service
FetchUser()        // en otro service
RetrieveAccount()  // en un tercero

// ✓ Un nombre consistente para el mismo concepto
GetExampleUser()   // siempre "Get" para recuperar un recurso
```

### Contexto significativo

```csharp
// ❌ Necesita comentario para entenderse
string s1;   // first name
string s2;   // last name
int d;       // days elapsed

// ✓ El nombre es el comentario
string firstName;
string lastName;
int daysElapsed;

// ✓ Mejor aún — agrupar en un Value Object si pertenecen juntos
public sealed record FullName(string First, string Last)
{
    public override string ToString() => $"{First} {Last}";
}
```

---

## Funciones limpias
> Fuente: *Clean Code* (Martin) — Ch.3 Functions

### Regla 1: tamaño pequeño

```csharp
// ❌ Función demasiado larga — hace demasiado
public async Task<Result> ProcessOrder(CreateOrderRequest request)
{
    // validar usuario (10 líneas)
    var user = await _db.GetUserAsync(request.UserId);
    if (user is null) return Result.Failure("Usuario no existe");
    if (!user.IsActive) return Result.Failure("Usuario inactivo");
    // ...

    // calcular precios (20 líneas)
    decimal subtotal = 0;
    foreach (var item in request.Items)
    {
        var product = await _db.GetProductAsync(item.ProductId);
        // ...
    }

    // aplicar descuentos (15 líneas)
    // verificar inventario (12 líneas)
    // crear orden (8 líneas)
    // enviar notificación (10 líneas)
    // ...
}

// ✓ Cada función hace UNA cosa
public async Task<Result> ProcessOrder(CreateOrderRequest request)
{
    var userResult = await ValidateUserAsync(request.UserId);
    if (userResult.IsFailure) return userResult;

    var pricingResult = await CalculatePricingAsync(request.Items);
    if (pricingResult.IsFailure) return pricingResult;

    var inventoryResult = await VerifyInventoryAsync(request.Items);
    if (inventoryResult.IsFailure) return inventoryResult;

    var order = await CreateOrderAsync(request, pricingResult.Value);
    await _notifications.NotifyOrderCreatedAsync(order);

    return Result.Success(order);
}
```

### Regla 2: un nivel de abstracción por función

```csharp
// ❌ Mezcla niveles de abstracción — algunos pasos son de alto nivel, otros de bajo nivel
public async Task Handle(RegisterUserRequest request, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        throw new ArgumentException("Email inválido");   // ← bajo nivel (validación puntual)

    await _repo.InsertAsync(new ExampleUser { Email = request.Email }, ct);  // ← alto nivel

    var smtp = new SmtpClient("mail.example.com", 587);   // ← bajo nivel (detalles de SMTP)
    smtp.Send(new MailMessage("from@example.com", request.Email, "Bienvenido", "..."));
}

// ✓ Un nivel de abstracción por función
public async Task Handle(RegisterUserRequest request, CancellationToken ct)
{
    _validator.ValidateAndThrow(request);
    var user = ExampleUser.Create(request.FullName, request.Email);
    await _repo.InsertAsync(user, ct);
    await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, ct);
}
```

### Regla 3: argumentos mínimos

```csharp
// ❌ Demasiados argumentos — indica que la función hace demasiado
public void CreateUser(
    string name, string email, string password,
    string role, string tenantId, bool isActive, DateTime birthDate)

// ✓ Encapsular en un objeto
public void CreateUser(CreateUserCommand command)

// ✓ O usar el patrón builder para objetos complejos
var user = ExampleUser.Create(request.FullName, request.Email);   // solo los obligatorios
user.SetRole(request.Role);      // opcionales por separado
user.SetBirthDate(request.BirthDate);
```

---

## Comentarios
> Fuente: *Clean Code* (Martin) — Ch.4 Comments

El mejor comentario es el que no se necesita. El código bien nombrado se explica solo.

```csharp
// ❌ Comentario que explica QUÉ hace el código (redundante si el nombre es bueno)
// Incrementa el contador de reintentos del usuario
user.RetryCount++;

// ❌ Comentario desactualizado (más peligroso que no tener comentario)
// Retorna el usuario por email
public async Task<ExampleUser?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
// ... (el nombre del método cambió pero el comentario no)

// ✓ Comentario válido: explica el POR QUÉ (no el qué)
// BCrypt es intencional — scrypt sería más seguro pero causa timeouts en la RAM de los Lambdas
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);

// ✓ Comentario válido: advertencia o invariante no obvio
// Thread-safe: IMemoryCache es thread-safe pero IDistributedCache.GetAsync no es idempotente
// bajo alta concurrencia — usar el SemaphoreSlim en esta clase para evitar stampede
```

---

## Formato y estructura
> Fuente: *Clean Code* (Martin) — Ch.5 Formatting

```csharp
// ✓ Separar conceptos con líneas en blanco — como párrafos
public sealed class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    private readonly IExampleUserRepository _repo;
    private readonly ICacheService          _cache;

    // Constructor separado de las propiedades
    public GetExampleUserHandler(IExampleUserRepository repo, ICacheService cache)
    {
        _repo  = repo;
        _cache = cache;
    }

    // Método separado del constructor
    public async Task<GetExampleUserResponse> Handle(
        GetExampleUserRequest request, CancellationToken ct)
    {
        var cacheKey = $"user:{request.PublicId}";

        // Verificar caché primero
        if (_cache.TryGet<ExampleUserDto>(cacheKey, out var cached) && cached is not null)
            return new GetExampleUserSuccess(cached);

        // Ir a la base de datos si no hay caché
        var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);

        if (user is null)
            return new GetExampleUserNotFoundFailure("Usuario no encontrado.");

        var dto = new ExampleUserDto(user);
        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));

        return new GetExampleUserSuccess(dto);
    }
}
```

### Ley de Demeter — no hablar con extraños

```csharp
// ❌ La ley de Demeter: no encadenar más de un nivel de acceso
var city = user.Address.HomeAddress.City.Name;
// Si Address cambia, o HomeAddress cambia, o City cambia → esta línea rompe

// ✓ El objeto expone lo que se necesita
var city = user.GetHomeCity();

// ✓ O en el contexto del Read Model — un DTO plano evita el problema
public sealed record ExampleUserDto(
    Guid   PublicId,
    string FullName,
    string Email,
    string HomeCity);
```

---

## Cohesión y acoplamiento
> Fuente: *Clean Code with C#* — Ch.3 Classes, Objects, and Data Structures

**Alta cohesión**: una clase tiene una responsabilidad bien definida y sus métodos son todos relevantes a esa responsabilidad. El resultado: código fácil de entender, probar y modificar.

**Bajo acoplamiento**: las clases interactúan solo a través de abstracciones (interfaces). Cambiar una clase no obliga a cambiar las demás.

```csharp
// ❌ Baja cohesión — una clase con responsabilidades no relacionadas
public sealed class ExampleUserManager
{
    public ExampleUser GetUser(Guid id) { ... }
    public void SendWelcomeEmail(ExampleUser user) { ... }    // ← responsabilidad de email
    public void CalculateTax(ExampleUser user) { ... }        // ← responsabilidad de contabilidad
    public void GeneratePayStub(ExampleUser user) { ... }     // ← responsabilidad de nómina
}

// ✓ Alta cohesión — cada clase hace una sola cosa
public sealed class ExampleUserRepository { ... }     // solo acceso a datos
public sealed class EmailService { ... }              // solo envío de emails
public sealed class TaxCalculator { ... }             // solo cálculo de impuestos
```

```csharp
// ❌ Acoplamiento fuerte — ExampleUserService depende de una implementación concreta
public sealed class ExampleUserService
{
    private readonly SqlExampleUserRepository _repo = new SqlExampleUserRepository();  // ← hardcoded
}

// ✓ Acoplamiento débil — depende de la abstracción, no de la implementación
public sealed class ExampleUserService
{
    private readonly IExampleUserRepository _repo;

    public ExampleUserService(IExampleUserRepository repo)  // ← DI por interfaz
        => _repo = repo;
}
// Se puede cambiar SqlExampleUserRepository por InMemoryExampleUserRepository sin tocar ExampleUserService
```

**Regla práctica:** si para testear una clase hay que instanciar 5 dependencias concretas, el acoplamiento es demasiado fuerte. Las dependencias deben inyectarse como interfaces.

---

## Manejo de errores
> Fuente: *Clean Code* (Martin) — Ch.7 Error Handling

```csharp
// ❌ Código de error mezclado con lógica de negocio
public async Task<ExampleUser?> RegisterAsync(string name, string email)
{
    if (string.IsNullOrEmpty(name)) return null;    // ¿null significa qué?
    if (!email.Contains('@')) return null;           // mismo código de error para distintos problemas
    if (await _repo.EmailExistsAsync(email)) return null;  // imposible distinguir qué falló

    // ... lógica de registro
    return user;
}

// ✓ Usar Result Pattern o excepciones con tipos específicos
public async Task<Result<ExampleUser>> RegisterAsync(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        return Result<ExampleUser>.Failure("El nombre es requerido.");

    if (!email.Contains('@'))
        return Result<ExampleUser>.Failure("El email no es válido.");

    if (await _repo.EmailExistsAsync(email))
        return Result<ExampleUser>.Failure("El email ya está registrado.");

    var user = ExampleUser.Create(name, email);
    await _repo.InsertAsync(user);
    return Result<ExampleUser>.Success(user);
}
```

---

## Cuándo aplicar / Cuándo no sobre-aplicar

| Aplicar | No sobre-aplicar |
|---------|-----------------|
| Renombrar para claridad cuando hay confusión | Renombrar sin razón lo que ya está claro |
| Extraer función cuando hay dos niveles de abstracción mezclados | Extraer funciones triviales de una línea |
| Eliminar comentarios redundantes que repiten el código | Eliminar comentarios que explican restricciones no obvias |
| Reducir argumentos encapsulando en objetos cuando hay 4+ | Crear objetos para funciones con 2-3 argumentos naturales |


---

## Glosario

| Término | Definición |
|---------|-----------|
| Legibilidad | cualidad del código que permite entender su propósito sin necesidad de comentarios adicionales |
| Nombre significativo | identificador que revela la intención del elemento que nombra sin abreviaciones ni vaguedad |
| Función limpia | función que hace una sola cosa, en un solo nivel de abstracción, con argumentos mínimos |
| Ley de Demeter | principio que establece que un objeto solo debe comunicarse con sus colaboradores directos |
| Comentario redundante | comentario que repite lo que el código ya expresa claramente — debe eliminarse |
| Nivel de abstracción | capa conceptual de un sistema; una función no debe mezclar niveles alto y bajo en el mismo cuerpo |
| Alta cohesión | propiedad de una clase cuyos métodos son todos relevantes a una única responsabilidad |
| Bajo acoplamiento | dependencia exclusiva a través de abstracciones (interfaces), no de implementaciones concretas |
| Result Pattern | patrón para comunicar éxito o fallo sin usar excepciones para flujo de negocio |
| Manejo de errores | estrategia para comunicar condiciones de fallo de forma explícita y tipada |
| Magic string | literal de texto usada directamente en el código sin nombre ni constante que explique su significado |

---

*Rogelio Arriaga Gonzalez*
