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

### Deuda técnica y código legado
> Fuente: *Refactoring with C#* — Ch.1 What Is Refactoring?

**Deuda técnica** es el costo adicional de trabajo futuro causado por tomar atajos en el presente. Acumula intereses: cada feature nueva cuesta más cuando el código subyacente es difícil de entender o modificar.

**Código legado** — definición de Michael Feathers: *"code without tests"*. No importa la antigüedad; lo que lo hace "legado" es la ausencia de una red de seguridad que permita cambiarlo con confianza.

Causas comunes de deuda técnica:
- Presión de deadlines que obliga a tomar atajos
- Falta de refactoring continuo (no aplicar la regla del Boy Scout)
- Código escrito sin tests desde el principio
- Decisiones de diseño obsoletas que nunca se revisitaron

Señales de deuda acumulada:
- El equipo evita tocar ciertos archivos ("zona peligrosa")
- Agregar una feature simple requiere cambios en muchos lugares
- Es difícil escribir tests para el código existente
- Leer el código no revela claramente su intención

### Deuda técnica como riesgo — registro de riesgos
> Fuente: *Refactoring with C#* — Ch.15 Managing Technical Debt

La deuda técnica no pagada es riesgo de proyecto. Para priorizarla de forma objetiva, tratarla como riesgos con probabilidad e impacto medibles.

```
Riesgo = Probabilidad × Impacto

Probabilidad (1-5): ¿qué tan probable es que esta deuda cause un problema?
Impacto (1-5): ¿qué tan grave sería el problema si ocurre?
Prioridad = Probabilidad × Impacto (1 = bajo riesgo, 25 = crítico)
```

```
Ejemplo de registro de riesgos (Risk Register):

ID  | Título                           | Estado  | Prob | Impacto | Prioridad
----|----------------------------------|---------|------|---------|----------
R01 | AuthService sin tests unitarios  | Abierto | 4    | 5       | 20
R02 | Queries N+1 en listado de órdenes | Abierto | 3    | 4       | 12
R03 | Dependencias desactualizadas     | Abierto | 2    | 3       | 6
R04 | Magic strings en validaciones    | Abierto | 3    | 2       | 6

Refactorizar primero los riesgos con prioridad más alta.
```

**Proceso de revisión de riesgos:**
- Revisar el registro en cada sprint planning o retrospectiva
- Reclasificar riesgos cuando cambia el contexto (nuevo módulo que toca la deuda, cliente nuevo)
- Marcar riesgos como "mitigado" cuando se refactoriza la zona afectada, no solo cuando "se cierra el ticket"

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

### Introduce Local Variable — eliminar expresiones repetidas
> Fuente: *Refactoring with C#* — Ch.2 Simplifying Methods

```csharp
// Antes — la misma expresión calculada múltiples veces
public decimal CalculateFinalPrice(Order order)
{
    if (order.Items.Sum(i => i.Price * i.Quantity) > 1000m)
        return order.Items.Sum(i => i.Price * i.Quantity) * 0.9m;
    return order.Items.Sum(i => i.Price * i.Quantity);
}

// Después — extraer a una variable con nombre que explica el propósito
public decimal CalculateFinalPrice(Order order)
{
    var subtotal = order.Items.Sum(i => i.Price * i.Quantity);
    return subtotal > 1000m ? subtotal * 0.9m : subtotal;
}
```

### Introduce Constant — eliminar números mágicos
> Fuente: *Refactoring with C#* — Ch.2 Simplifying Methods

```csharp
// Antes — ¿qué significan estos números?
public string GetExampleUserTier(ExampleUser user)
{
    if (user.TotalPurchases >= 50_000_000) return "Platinum";
    if (user.TotalPurchases >= 40_000_000) return "Gold";
    if (user.TotalPurchases >= 30_000_000) return "Silver";
    return "Standard";
}

// Después — las constantes documentan el significado del número
private const decimal PlatinumThreshold = 50_000_000m;
private const decimal GoldThreshold     = 40_000_000m;
private const decimal SilverThreshold   = 30_000_000m;

public string GetExampleUserTier(ExampleUser user) => user.TotalPurchases switch
{
    >= PlatinumThreshold => "Platinum",
    >= GoldThreshold     => "Gold",
    >= SilverThreshold   => "Silver",
    _                    => "Standard"
};
```

### Invert If — retorno temprano para eliminar anidamiento
> Fuente: *Refactoring with C#* — Ch.3 Conditional Logic

```csharp
// Antes — lógica principal anidada dentro de múltiples if
public async Task<ProcessResult> ProcessOrderAsync(Guid userId, Guid orderId, CancellationToken ct)
{
    var user = await _users.GetByIdAsync(userId, ct);
    if (user is not null)
    {
        if (user.IsActive)
        {
            var order = await _orders.GetByIdAsync(orderId, ct);
            if (order is not null)
            {
                // lógica principal
                return new ProcessSuccess();
            }
            return new ProcessFailure("Orden no encontrada.");
        }
        return new ProcessFailure("Usuario inactivo.");
    }
    return new ProcessFailure("Usuario no encontrado.");
}

// Después — invertir: validar el camino infeliz primero, lógica principal al final sin anidamiento
public async Task<ProcessResult> ProcessOrderAsync(Guid userId, Guid orderId, CancellationToken ct)
{
    var user = await _users.GetByIdAsync(userId, ct);
    if (user is null)    return new ProcessFailure("Usuario no encontrado.");
    if (!user.IsActive)  return new ProcessFailure("Usuario inactivo.");

    var order = await _orders.GetByIdAsync(orderId, ct);
    if (order is null)   return new ProcessFailure("Orden no encontrada.");

    // lógica principal sin anidamiento
    return new ProcessSuccess();
}
```

### Drop Else After Return — eliminar else redundante
> Fuente: *Refactoring with C#* — Ch.3 Conditional Logic

```csharp
// Antes — else después de return no aporta información; solo agrega anidamiento
public string GetExampleUserStatus(ExampleUser user)
{
    if (!user.IsActive)
    {
        return "Inactivo";
    }
    else
    {
        if (user.IsSuspended)
        {
            return "Suspendido";
        }
        else
        {
            return "Activo";
        }
    }
}

// Después — sin else; el flujo es lineal
public string GetExampleUserStatus(ExampleUser user)
{
    if (!user.IsActive)   return "Inactivo";
    if (user.IsSuspended) return "Suspendido";
    return "Activo";
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

## Refactoring a gran escala — Strangler Fig pattern
> Fuente: *Refactoring with C#* — Ch.17 Refactoring Large Applications

Cuando el sistema legado es demasiado grande para refactorizar in-situ, el patrón Strangler Fig permite reemplazarlo incrementalmente sin detener el desarrollo ni hacer una reescritura total.

### El problema con las reescrituras totales

```
El "rewrite trap":
  ✗ El equipo estima 6 meses → tarda 2 años
  ✗ El sistema legado sigue recibiendo bugs mientras se construye el nuevo
  ✗ Al llegar, el nuevo sistema tiene los mismos problemas de diseño
     que el viejo (porque el equipo no entendía el dominio)
  ✗ El negocio perdió 2 años de features

Strangler Fig es mejor: reemplazar por rebanadas verticales,
incrementalmente, sin detener el negocio.
```

### Cómo funciona Strangler Fig

```
Fase 1: Identificar una "rebanada" funcional pequeña (ej: módulo de reportes)
Fase 2: Construir la nueva implementación en paralelo (mismo dominio, nuevo código)
Fase 3: Redirigir el tráfico de esa rebanada al nuevo servicio via proxy/gateway
Fase 4: Monitorear — si hay problemas, regresar al legado (feature flag)
Fase 5: Una vez estable, eliminar el código legado de esa rebanada
Fase 6: Repetir con la siguiente rebanada

Resultado: el sistema legado "se estrangula" gradualmente, como la higuera
(strangler fig) que crece alrededor de un árbol hasta reemplazarlo.
```

### Implementación con YARP (proxy de tráfico)

```csharp
// El API Gateway redirige tráfico al sistema legado o al nuevo servicio
// según la feature flag o la ruta — sin que el cliente note la diferencia

// appsettings.json — YARP como Strangler Fig proxy
{
  "ReverseProxy": {
    "Routes": {
      "reports-new": {
        "ClusterId": "reports-new-cluster",
        "Match": { "Path": "/api/reports/{**catch-all}" },
        "Metadata": { "RequireFeatureFlag": "NewReportsService" }
      },
      "legacy-fallback": {
        "ClusterId": "legacy-cluster",
        "Match": { "Path": "/{**catch-all}" }
      }
    },
    "Clusters": {
      "reports-new-cluster": {
        "Destinations": {
          "reports-api": { "Address": "http://reports-service-v2:8080" }
        }
      },
      "legacy-cluster": {
        "Destinations": {
          "legacy-api": { "Address": "http://legacy-monolith:8080" }
        }
      }
    }
  }
}
```

```csharp
// Middleware que inspecciona la feature flag antes de dejar pasar al proxy
public sealed class FeatureFlagRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFeatureManager _features;

    public async Task InvokeAsync(HttpContext context)
    {
        var requiresFlag = context.GetEndpoint()?.Metadata
            .GetMetadata<RouteMetadataAttribute>()?.Values
            .GetValueOrDefault("RequireFeatureFlag");

        if (requiresFlag is not null && !await _features.IsEnabledAsync(requiresFlag))
        {
            // Redirigir al legado si la feature flag no está activa
            context.Request.Path = "/legacy" + context.Request.Path;
        }

        await _next(context);
    }
}
```

### Feature flags para despliegue seguro del refactoring

```csharp
// Microsoft.FeatureManagement — habilitar gradualmente el nuevo servicio
// appsettings.json
{
  "FeatureManagement": {
    "NewReportsService": {
      "EnabledFor": [
        {
          "Name": "Percentage",
          "Parameters": { "Value": 10 }   // 10% del tráfico al nuevo servicio
        }
      ]
    }
  }
}

// Uso en el handler
public sealed class GetReportHandler
{
    private readonly IFeatureManager       _features;
    private readonly INewReportService     _newService;
    private readonly ILegacyReportService  _legacyService;

    public async Task<ReportDto> Handle(GetReportRequest req, CancellationToken ct)
    {
        if (await _features.IsEnabledAsync("NewReportsService"))
            return await _newService.GetReportAsync(req.ReportId, ct);

        return await _legacyService.GetReportAsync(req.ReportId, ct);
    }
}
```

### Estrategias ágiles de refactoring
> Fuente: *Refactoring with C#* — Ch.17 Agile Refactoring Strategies

```
Opción 1 — Work items dedicados
  Crear tickets de refactoring en el backlog con la misma prioridad que features.
  Ventaja: visibilidad. Desventaja: compiten con features por tiempo.

Opción 2 — Refactorizar mientras se toca el código (Boy Scout Rule)
  Cada vez que un desarrollador toca un archivo, lo deja más limpio.
  Ventaja: sin overhead de planificación. Desventaja: puede ser inconsistente.

Opción 3 — Sprints de refactoring dedicados
  Un sprint cada N sprints (ej: cada 4) dedicado exclusivamente a deuda técnica.
  Ventaja: esfuerzo concentrado. Desventaja: el negocio lo percibe como "sin features".

Recomendación: combinar Opción 2 para mejoras pequeñas + Opción 1 para
refactorings significativos que requieren coordinación de equipo.
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

## Glosario

| Término | Definición |
|---------|-----------|
| Refactoring | reestructuración del código existente sin cambiar su comportamiento externo observable |
| Code smell | patrón en el código que indica un problema subyacente de diseño o calidad |
| Deuda técnica | costo adicional del trabajo futuro causado por tomar atajos en el presente |
| Código legado | código sin tests — sin importar su antigüedad, es difícil cambiar con confianza |
| Long Method | método demasiado largo que hace más de una cosa — señal de necesidad de extracción |
| Feature Envy | método que usa más datos de otra clase que de la suya propia |
| Strangler Fig | patrón para reemplazar un sistema legado incrementalmente sin detener el desarrollo |
| Boy Scout Rule | práctica de dejar el código más limpio de como se encontró en cada modificación |
| Extract Method | técnica de refactoring que extrae un bloque de código a un método separado con nombre descriptivo |
| Introduce Constant | técnica que reemplaza números o strings mágicos por constantes con nombre significativo |
| Risk Register | registro de riesgos con probabilidad e impacto para priorizar la deuda técnica de forma objetiva |
| Feature flag | mecanismo para activar o desactivar funcionalidad sin necesidad de un nuevo deployment |

---

*Rogelio Arriaga Gonzalez*
