# 03 — Refactoring: Mejorar código existente sin cambiar comportamiento

Refactoring es el proceso de reestructurar código existente sin cambiar su comportamiento externo observable. El objetivo es mejorar la legibilidad, la extensibilidad y reducir la deuda técnica.

---

## Qué es y qué no es refactoring
> Fuente: *Refactoring with C#* — Ch.1 What Is Refactoring?

```
Refactoring:
  ✓ Renombrar variable para más claridad
  ✓ Extraer un método largo en métodos más pequeños
  ✓ Eliminar código duplicado
  ✓ Simplificar lógica condicional compleja
  → El comportamiento externo NO cambia

No es refactoring:
  ✗ Agregar nueva funcionalidad (eso es feature development)
  ✗ Corregir un bug (eso es bug fix)
  ✗ Optimizar performance (eso puede cambiar el comportamiento observable)
  ✗ Reescribir desde cero (eso es rewrite)
```

### La regla del Boy Scout

"Deja el código más limpio de como lo encontraste." No hace falta un sprint de refactoring — mejorar un poco cada vez que se toca el código.

---

## Code Smells — señales de que el código necesita refactoring

Los "code smells" son patrones en el código que indican un problema subyacente.

### 1. Long Method (Método largo)

```csharp
// ❌ Método de 80 líneas que hace todo
public async Task<IActionResult> ProcessPayment(PaymentRequest request)
{
    // validar usuario (15 líneas)
    // verificar fondos (20 líneas)
    // calcular comisiones (15 líneas)
    // procesar cargo (10 líneas)
    // enviar confirmación (10 líneas)
    // registrar auditoría (10 líneas)
}

// ✓ Extraer métodos — cada uno hace una cosa
public async Task<IActionResult> ProcessPayment(PaymentRequest request)
{
    var user       = await ValidateUserAsync(request.UserId);
    var fundCheck  = await VerifyFundsAsync(user, request.Amount);
    var commission = CalculateCommission(request.Amount);
    var charge     = await ProcessChargeAsync(user, request.Amount + commission);

    await SendConfirmationAsync(charge);
    await LogAuditAsync(charge);

    return Ok(new PaymentResult(charge.Id));
}
```

### 2. Duplicated Code (Código duplicado)

```csharp
// ❌ La misma validación de email en tres handlers
// InsertExampleUserHandler:
if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
    return new InsertExampleUserValidationFailure("Email inválido.");

// UpdateExampleUserHandler:
if (string.IsNullOrWhiteSpace(request.NewEmail) || !request.NewEmail.Contains('@'))
    return new UpdateExampleUserValidationFailure("Email inválido.");

// ✓ Extraer a un Value Object o método compartido
public sealed record Email
{
    public string Value { get; }
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new ArgumentException("Email inválido.");
        Value = value.ToLowerInvariant().Trim();
    }
    public static implicit operator string(Email e) => e.Value;
}

// En los handlers — la validación está en UN solo lugar
var email = new Email(request.Email);  // lanza ArgumentException si inválido
```

### 3. Long Parameter List (Lista de parámetros larga)

```csharp
// ❌ 7 parámetros — señal de que la función hace demasiado o falta encapsulación
public void CreateExampleUser(
    string firstName, string lastName, string email, string password,
    string role, Guid tenantId, bool sendWelcomeEmail)

// ✓ Encapsular en un command/request
public sealed record CreateExampleUserCommand(
    string FullName,
    string Email,
    string Password,
    string Role,
    Guid   TenantId,
    bool   SendWelcomeEmail = true);

public void CreateExampleUser(CreateExampleUserCommand command)
```

### 4. Feature Envy (Envidia de características)

```csharp
// ❌ El método usa más datos de ExampleUser que de su propia clase
public sealed class OrderService
{
    public decimal CalculateDiscount(Order order, ExampleUser user)
    {
        // Este método usa mayormente datos de ExampleUser
        if (user.IsVip && user.CreatedAtUtc < DateTime.UtcNow.AddYears(-1) && user.TotalPurchases > 10000)
            return order.Total * 0.2m;
        return 0;
    }
}

// ✓ El método pertenece donde están los datos
public sealed class ExampleUser
{
    public decimal CalculateDiscount(decimal orderTotal)
    {
        if (IsVip && AccountAgeDays > 365 && TotalPurchases > 10000)
            return orderTotal * 0.2m;
        return 0;
    }
}
```

### 5. Switch Statements (Switch/if-else encadenados por tipo)

```csharp
// ❌ Switch sobre tipo — se extiende en múltiples lugares
public decimal CalculateShipping(Order order)
{
    switch (order.ShippingType)
    {
        case ShippingType.Standard: return order.Weight * 10m;
        case ShippingType.Express:  return order.Weight * 25m;
        case ShippingType.Overnight: return order.Weight * 50m;
        default: throw new ArgumentException("Tipo desconocido");
    }
}
// Este switch se repite en CalculateDeliveryTime, CalculateInsurance, etc.

// ✓ Polimorfismo — cada tipo sabe calcular su propio costo
public abstract class ShippingStrategy
{
    public abstract decimal CalculateCost(decimal weight);
    public abstract int EstimatedDays { get; }
}

public sealed class StandardShipping : ShippingStrategy
{
    public override decimal CalculateCost(decimal weight) => weight * 10m;
    public override int EstimatedDays => 5;
}

public sealed class ExpressShipping : ShippingStrategy
{
    public override decimal CalculateCost(decimal weight) => weight * 25m;
    public override int EstimatedDays => 2;
}
```

---

## Técnicas de refactoring

### Extract Method — extraer método

```csharp
// Antes
public async Task<RegisterUserResponse> Handle(RegisterUserRequest request, CancellationToken ct)
{
    // Validar email
    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        return new RegisterUserValidationFailure("Email inválido.");
    var normalizedEmail = request.Email.ToLowerInvariant().Trim();

    // Hashear password
    var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

    // Crear usuario
    var user = new ExampleUser
    {
        PublicId     = Guid.NewGuid(),
        FullName     = request.FullName.Trim(),
        Email        = normalizedEmail,
        PasswordHash = hash,
        IsActive     = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    await _repo.InsertAsync(user, ct);
    return new RegisterUserSuccess(new ExampleUserDto(user));
}

// Después — cada paso extraído
public async Task<RegisterUserResponse> Handle(RegisterUserRequest request, CancellationToken ct)
{
    if (!TryNormalizeEmail(request.Email, out var email))
        return new RegisterUserValidationFailure("Email inválido.");

    var user = ExampleUser.Create(request.FullName, email, HashPassword(request.Password));
    await _repo.InsertAsync(user, ct);
    return new RegisterUserSuccess(new ExampleUserDto(user));
}

private static bool TryNormalizeEmail(string raw, out string normalized)
{
    normalized = string.Empty;
    if (string.IsNullOrWhiteSpace(raw) || !raw.Contains('@')) return false;
    normalized = raw.ToLowerInvariant().Trim();
    return true;
}

private static string HashPassword(string password)
    => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
```

### Introduce Parameter Object — introducir objeto parámetro

```csharp
// Antes
public Task<IEnumerable<ExampleUser>> GetPagedAsync(
    int page, int pageSize, string? nameFilter, bool? isActive, DateTime? createdAfter)

// Después
public sealed record ExampleUserFilter(
    int       Page         = 1,
    int       PageSize     = 20,
    string?   NameContains = null,
    bool?     IsActive     = null,
    DateTime? CreatedAfter = null);

public Task<IEnumerable<ExampleUser>> GetPagedAsync(ExampleUserFilter filter)
```

### Replace Conditional with Polymorphism

```csharp
// Antes — condicional por tipo
public string FormatAddress(Address address)
{
    if (address.Country == "MX")
        return $"{address.Street}, {address.City}, {address.State} {address.ZipCode}, México";
    else if (address.Country == "US")
        return $"{address.Street}, {address.City}, {address.State} {address.ZipCode}";
    else
        return $"{address.Street}, {address.City}, {address.Country}";
}

// Después — el formato es responsabilidad de cada variante
public abstract class Address
{
    public abstract string Format();
}

public sealed class MexicanAddress : Address
{
    public string Street  { get; init; } = string.Empty;
    public string City    { get; init; } = string.Empty;
    public string State   { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;

    public override string Format()
        => $"{Street}, {City}, {State} {ZipCode}, México";
}
```

---

## Refactoring seguro — el proceso

```
1. Asegurarse de tener tests que cubran el código a refactorizar
   (si no hay tests, escribirlos ANTES de tocar el código)

2. Hacer el refactoring en pasos pequeños, no en un cambio masivo

3. Correr los tests después de CADA paso

4. Commitear cuando los tests pasan (el commit es el checkpoint)

5. Si algo se rompe → revertir al último commit que pasó los tests
```

```bash
# Flujo con git
git checkout -b refactor/extract-email-validation
# ... hacer cambio pequeño ...
dotnet test
git add -A && git commit -m "refactor(users): extract email validation to Value Object"
# ... otro cambio pequeño ...
dotnet test
git add -A && git commit -m "refactor(users): use Email value object in InsertHandler"
# ... y así sucesivamente
```

---

## Cuándo NO refactorizar

| No refactorizar cuando... | Por qué |
|---------------------------|---------|
| Código que nadie toca | El riesgo de introducir bugs supera el beneficio |
| Antes de entender qué hace el código | Refactorizar código que no se entiende es peligroso |
| Sin tests que respalden el cambio | Sin tests no sabes si rompiste algo |
| En medio de un bug crítico en producción | Primero arreglar el bug, luego limpiar |
| Deadline inminente sin margen de error | Dejar la deuda técnica anotada y pagar en el siguiente sprint |


---

*Rogelio Arriaga Gonzalez*
