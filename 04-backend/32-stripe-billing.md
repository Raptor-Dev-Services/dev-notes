# 32 — Stripe: Suscripciones y Facturación

Stripe es el estándar de facto para cobros en SaaS. Maneja suscripciones recurrentes, cambios de plan, periodos de prueba, métodos de pago y webhooks de estado de cobro. La integración correcta conecta el ciclo de vida del tenant con el ciclo de vida de la suscripción en Stripe.

---

## Conceptos clave de Stripe para SaaS

```
Customer     → el tenant en Stripe. Se crea al registrarse.
Product      → el nombre del plan ("Pro Plan")
Price        → precio de un producto ($49/mes mensual recurrente)
Subscription → el vínculo Customer ↔ Price con estado activo/past_due/canceled
PaymentMethod → tarjeta u otro método de pago
Invoice      → factura generada en cada ciclo de cobro
SetupIntent  → flujo para guardar método de pago SIN cobrar inmediatamente
```

---

## Entidad TenantBilling

```csharp
// Tenancy.Domain/Entities/TenantBilling.cs
public sealed class TenantBilling
{
    public long     Id                  { get; init; }
    public long     TenantId            { get; init; }
    public string   StripeCustomerId    { get; init; } = string.Empty;   // "cus_xxx"
    public string?  StripeSubscriptionId { get; init; }                  // "sub_xxx"
    public string?  StripePriceId       { get; init; }                   // "price_xxx"
    public string   BillingStatus       { get; init; } = "none";         // none, trial, active, past_due, canceled
    public DateTime? TrialEndsAt        { get; init; }
    public DateTime? CurrentPeriodEnd   { get; init; }
    public DateTime  CreatedAtUtc       { get; init; }
    public DateTime  UpdatedAtUtc       { get; init; }
}
```

---

## Flujo 1: Registro con trial gratuito

```
1. Tenant se registra → se crea Customer en Stripe (sin método de pago)
2. TenantBilling: status = "trial", TrialEndsAt = now + 14 días
3. Al terminar el trial → Stripe webhook: customer.subscription.trial_will_end
4. Email al Admin: "Tu trial termina en 3 días, agrega método de pago"
5. Admin agrega tarjeta (SetupIntent → PaymentMethod)
6. Se activa la suscripción → Stripe webhook: invoice.paid
7. TenantBilling: status = "active"
```

---

## Flujo 2: Upgrade de plan

```
1. Admin elige plan Pro desde el dashboard
2. API llama Stripe Subscriptions API: actualizar subscription con nuevo Price
3. Stripe cobra la diferencia prorrateada (o en el próximo ciclo)
4. Stripe webhook: customer.subscription.updated
5. Actualizar plan del tenant + TenantSettings
```

---

## Integración: Crear Customer en Stripe al registrarse

```csharp
// Authentication.Application/UseCases/RegisterTenant/RegisterTenantHandler.cs
// Después de crear el tenant y las credenciales...

var stripeCustomer = await _stripeService.CreateCustomerAsync(
    email:      request.AdminEmail,
    name:       request.TenantName,
    tenantId:   tenantId,
    ct:         ct);

await _billing.InsertAsync(
    tenantId:         tenantId,
    stripeCustomerId: stripeCustomer.Id,
    billingStatus:    "trial",
    trialEndsAt:      DateTime.UtcNow.AddDays(14),
    ct:               ct);
```

```csharp
// Tenancy.Infrastructure/Services/StripeService.cs
public sealed class StripeService : IStripeService
{
    private readonly CustomerService _customerService;
    private readonly SubscriptionService _subscriptionService;
    private readonly PaymentMethodService _paymentMethodService;

    public StripeService(IConfiguration config)
    {
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        _customerService      = new CustomerService();
        _subscriptionService  = new SubscriptionService();
        _paymentMethodService = new PaymentMethodService();
    }

    public async Task<Customer> CreateCustomerAsync(
        string email, string name, long tenantId, CancellationToken ct)
    {
        var options = new CustomerCreateOptions
        {
            Email    = email,
            Name     = name,
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = tenantId.ToString()   // para referenciar en webhooks
            }
        };
        return await _customerService.CreateAsync(options, cancellationToken: ct);
    }
}
```

---

## Checkout Session — agregar método de pago

Para que el Admin agregue su tarjeta, crear un Checkout Session de Stripe (hosted UI) o un SetupIntent (UI propia):

### Opción A — Stripe Checkout (hosted, recomendado para empezar)

```csharp
public async Task<string> CreateCheckoutSessionAsync(
    string stripeCustomerId, string priceId, string successUrl, string cancelUrl,
    CancellationToken ct)
{
    var options = new SessionCreateOptions
    {
        Customer    = stripeCustomerId,
        Mode        = "subscription",
        LineItems   = new List<SessionLineItemOptions>
        {
            new() { Price = priceId, Quantity = 1 }
        },
        SuccessUrl  = successUrl,   // "https://app.misaas.com/billing/success?session_id={CHECKOUT_SESSION_ID}"
        CancelUrl   = cancelUrl,
        // Trial period desde la suscripción existente:
        SubscriptionData = new SessionSubscriptionDataOptions
        {
            TrialFromPlan = true
        }
    };
    var session = await new SessionService().CreateAsync(options, cancellationToken: ct);
    return session.Url;   // redirigir al frontend a esta URL
}
```

### Opción B — SetupIntent (UI propia con Stripe.js)

```csharp
public async Task<string> CreateSetupIntentAsync(
    string stripeCustomerId, CancellationToken ct)
{
    var options = new SetupIntentCreateOptions
    {
        Customer           = stripeCustomerId,
        PaymentMethodTypes = new List<string> { "card" }
    };
    var intent = await new SetupIntentService().CreateAsync(options, cancellationToken: ct);
    return intent.ClientSecret;   // enviar al frontend para usar con Stripe.js
}
```

---

## Webhooks — el corazón de la integración

Los webhooks de Stripe notifican los cambios de estado. **La API no debe hacer polling a Stripe** — escuchar los webhooks.

```csharp
// Host.Api/Controllers/StripeWebhookController.cs
[Route("api/stripe/webhook")]
[AllowAnonymous]
public sealed class StripeWebhookController : BaseApiController
{
    private readonly IStripeWebhookService _webhookService;
    private readonly string _webhookSecret;

    public StripeWebhookController(IStripeWebhookService webhookService, IConfiguration config)
        : base(null!)
    {
        _webhookService = webhookService;
        _webhookSecret  = config["Stripe:WebhookSecret"]!;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        var payload   = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Webhook inválido: {Message}", ex.Message);
            return BadRequest();
        }

        await _webhookService.HandleAsync(stripeEvent, ct);
        return Ok();
    }
}
```

```csharp
// Tenancy.Application/Services/StripeWebhookService.cs
public sealed class StripeWebhookService : IStripeWebhookService
{
    private readonly ITenantBillingRepository _billing;
    private readonly ITenantRepository        _tenants;
    private readonly IEmailService            _email;

    public async Task HandleAsync(Event stripeEvent, CancellationToken ct)
    {
        switch (stripeEvent.Type)
        {
            case Events.InvoicePaid:
                await HandleInvoicePaidAsync(stripeEvent.Data.Object as Invoice, ct);
                break;

            case Events.InvoicePaymentFailed:
                await HandlePaymentFailedAsync(stripeEvent.Data.Object as Invoice, ct);
                break;

            case Events.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdatedAsync(
                    stripeEvent.Data.Object as Subscription, ct);
                break;

            case Events.CustomerSubscriptionDeleted:
                await HandleSubscriptionCanceledAsync(
                    stripeEvent.Data.Object as Subscription, ct);
                break;

            case Events.CustomerSubscriptionTrialWillEnd:
                await HandleTrialWillEndAsync(
                    stripeEvent.Data.Object as Subscription, ct);
                break;
        }
    }

    private async Task HandleInvoicePaidAsync(Invoice? invoice, CancellationToken ct)
    {
        if (invoice?.CustomerId is null) return;

        var billing = await _billing.GetByStripeCustomerIdAsync(invoice.CustomerId, ct);
        if (billing is null) return;

        await _billing.UpdateStatusAsync(billing.Id,
            stripeSubscriptionId: invoice.SubscriptionId,
            status:               "active",
            currentPeriodEnd:     invoice.Lines.Data.FirstOrDefault()?.Period?.End,
            ct:                   ct);

        // Actualizar el plan del tenant
        var priceId = invoice.Lines.Data.FirstOrDefault()?.PriceId;
        if (priceId is not null)
        {
            var plan = MapPriceIdToPlan(priceId);
            await _tenants.UpdatePlanAsync(billing.TenantId, plan, ct);
        }
    }

    private async Task HandlePaymentFailedAsync(Invoice? invoice, CancellationToken ct)
    {
        if (invoice?.CustomerId is null) return;

        var billing = await _billing.GetByStripeCustomerIdAsync(invoice.CustomerId, ct);
        if (billing is null) return;

        await _billing.UpdateStatusAsync(billing.Id, status: "past_due", ct: ct);

        var tenant = await _tenants.GetByIdAsync(billing.TenantId, ct);
        if (tenant is not null)
            await _email.SendPaymentFailedAsync(tenant.AdminEmail!, invoice.AmountDue, ct);
    }

    private async Task HandleSubscriptionCanceledAsync(
        Subscription? subscription, CancellationToken ct)
    {
        if (subscription?.CustomerId is null) return;

        var billing = await _billing.GetByStripeCustomerIdAsync(subscription.CustomerId, ct);
        if (billing is null) return;

        await _billing.UpdateStatusAsync(billing.Id, status: "canceled", ct: ct);
        await _tenants.SuspendAsync(billing.TenantId, ct);

        var tenant = await _tenants.GetByIdAsync(billing.TenantId, ct);
        if (tenant is not null)
            await _email.SendSubscriptionCanceledAsync(tenant.AdminEmail!, ct);
    }

    private async Task HandleTrialWillEndAsync(Subscription? subscription, CancellationToken ct)
    {
        if (subscription?.CustomerId is null) return;

        var billing = await _billing.GetByStripeCustomerIdAsync(subscription.CustomerId, ct);
        if (billing is null) return;

        var tenant = await _tenants.GetByIdAsync(billing.TenantId, ct);
        if (tenant is not null)
        {
            var daysLeft = (int)(subscription.TrialEnd!.Value - DateTime.UtcNow).TotalDays;
            await _email.SendTrialEndingAsync(tenant.AdminEmail!, daysLeft, ct);
        }
    }

    private static string MapPriceIdToPlan(string priceId) => priceId switch
    {
        "price_pro_monthly"        => Plans.Pro,
        "price_enterprise_monthly" => Plans.Enterprise,
        _                          => Plans.Free
    };
}
```

---

## Portal de facturación (Stripe Customer Portal)

El portal de Stripe permite al Admin:
- Ver historial de facturas
- Cambiar método de pago
- Cancelar suscripción

Todo sin que el SaaS implemente esta UI:

```csharp
// Crear session del portal de facturación
public async Task<string> CreateBillingPortalSessionAsync(
    string stripeCustomerId, string returnUrl, CancellationToken ct)
{
    var options = new Stripe.BillingPortal.SessionCreateOptions
    {
        Customer  = stripeCustomerId,
        ReturnUrl = returnUrl   // "https://app.misaas.com/billing"
    };
    var session = await new Stripe.BillingPortal.SessionService()
        .CreateAsync(options, cancellationToken: ct);
    return session.Url;
}
```

```csharp
// Endpoint — Admin abre el portal
[HttpPost("billing/portal")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> BillingPortal(CancellationToken ct)
{
    var billing = await _billing.GetByTenantIdAsync(CurrentTenantId, ct);
    var url = await _stripeService.CreateBillingPortalSessionAsync(
        billing!.StripeCustomerId,
        returnUrl: "https://app.misaas.com/settings/billing",
        ct);
    return Ok(new { redirectUrl = url });
}
```

---

## Configuración en appsettings

```json
{
  "Stripe": {
    "PublishableKey": "pk_live_...",
    "SecretKey":      "sk_live_...",
    "WebhookSecret":  "whsec_...",
    "Prices": {
      "Pro":        "price_pro_monthly",
      "Enterprise": "price_enterprise_monthly"
    }
  }
}
```

**Nunca en appsettings.json comprometido en git.** Usar variables de entorno o secrets:

```
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
```

---

## Idempotencia de webhooks

Stripe puede reenviar el mismo webhook si no recibe un 200 en el primer intento. Guardar los IDs de eventos procesados:

```csharp
// Tenancy.Infrastructure/Repositories/ProcessedStripeEventRepository.cs
public async Task<bool> HasBeenProcessedAsync(string eventId, CancellationToken ct) =>
    await _db.ProcessedStripeEvents
        .AnyAsync(e => e.StripeEventId == eventId, ct);

public async Task MarkAsProcessedAsync(string eventId, CancellationToken ct)
{
    _db.ProcessedStripeEvents.Add(new ProcessedStripeEvent
    {
        StripeEventId = eventId,
        ProcessedAtUtc = DateTime.UtcNow
    });
    await _db.SaveChangesAsync(ct);
}

// En StripeWebhookService.HandleAsync:
if (await _processedEvents.HasBeenProcessedAsync(stripeEvent.Id, ct))
    return;   // ya procesado — ignorar duplicado

// ... procesar el evento ...
await _processedEvents.MarkAsProcessedAsync(stripeEvent.Id, ct);
```

---

## Checklist

- [ ] Crear Stripe Customer al registrar el tenant
- [ ] Guardar `stripe_customer_id` en `TenantBilling`
- [ ] Webhook endpoint `[AllowAnonymous]` con validación de firma `Stripe-Signature`
- [ ] Idempotencia: ignorar eventos ya procesados
- [ ] `invoice.paid` → activar tenant, actualizar plan
- [ ] `invoice.payment_failed` → marcar `past_due`, enviar email
- [ ] `customer.subscription.deleted` → suspender tenant
- [ ] `customer.subscription.trial_will_end` → email de advertencia
- [ ] Stripe keys NUNCA en git — variables de entorno
- [ ] Portal de facturación de Stripe para que el Admin gestione su suscripción
- [ ] Precio IDs centralizados en configuración — no hardcodeados

---

## Relación con multi-tenant y branches

Stripe opera a nivel **Tenant** — un Customer por tenant, no por branch. Los branches no tienen facturación independiente. El plan del tenant se aplica a todos sus branches.

Ver `04-backend/31-planes-limites.md` para cómo el plan determina los límites por tenant.
Ver `04-backend/26-tenant-onboarding.md` para cuándo se crea el Customer de Stripe.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Stripe Customer | Objeto de Stripe que representa a un tenant — almacena métodos de pago y historial de facturación |
| Subscription | Objeto de Stripe que modela la suscripción activa de un tenant a un plan con precio recurrente |
| Price | Objeto de Stripe que define el costo de un plan: monto, moneda, intervalo de facturación |
| Invoice | Factura generada por Stripe para cada ciclo de facturación — puede ser paid, open o void |
| SetupIntent | Objeto de Stripe para registrar un método de pago sin cobrar inmediatamente |
| CheckoutSession | Sesión de Stripe que redirige al usuario a la página de pago alojada en Stripe |
| BillingPortal | Portal de Stripe donde el cliente puede gestionar su suscripción, tarjeta e historial de pagos |
| StripeWebhookService | Servicio que procesa los eventos Stripe recibidos via webhook y actualiza el estado del tenant |
| WebhookSecret | Secreto de Stripe usado para validar la firma de cada webhook con ConstructEvent |
| Idempotencia de webhooks | Manejo de eventos duplicados de Stripe usando el EventId como clave de idempotencia |
| StripeCustomerId | ID del Customer en Stripe almacenado en la entidad Tenant para asociar facturación |
| customer.subscription.updated | Evento Stripe que notifica cambios en el plan o estado de la suscripción de un tenant |

---

*Rogelio Arriaga Gonzalez*
