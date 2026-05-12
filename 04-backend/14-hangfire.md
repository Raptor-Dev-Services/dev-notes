# 7 · Background jobs y trabajos programados

Tareas que corren fuera del request HTTP: envío de emails, generación de reportes, sincronizaciones, procesamiento asíncrono.

## 7.1 IHostedService — el más simple

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>public class EmailSenderService : BackgroundService</p>
<p>{</p>
<p>private readonly ILogger&lt;EmailSenderService&gt; _logger;</p>
<p>private readonly IServiceProvider _services;</p>
<p>public EmailSenderService(ILogger&lt;EmailSenderService&gt; logger, IServiceProvider services)</p>
<p>{</p>
<p>_logger = logger;</p>
<p>_services = services;</p>
<p>}</p>
<p>protected override async Task ExecuteAsync(CancellationToken stoppingToken)</p>
<p>{</p>
<p>while (!stoppingToken.IsCancellationRequested)</p>
<p>{</p>
<p>try</p>
<p>{</p>
<p>using var scope = _services.CreateScope();</p>
<p>var queue = scope.ServiceProvider.GetRequiredService&lt;IEmailQueue&gt;();</p>
<p>var pending = await queue.DequeuePendingAsync(stoppingToken);</p>
<p>foreach (var email in pending) await SendAsync(email, stoppingToken);</p>
<p>}</p>
<p>catch (Exception ex) { _logger.LogError(ex, "Error procesando emails"); }</p>
<p>await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);</p>
<p>}</p>
<p>}</p>
<p>}</p>
<p>builder.Services.AddHostedService&lt;EmailSenderService&gt;();</p></td>
</tr>
</tbody>
</table>

## 7.2 Hangfire — jobs persistentes con UI

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>builder.Services.AddHangfire(config =&gt; config</p>
<p>.UsePostgreSqlStorage(c =&gt;</p>
<p>c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));</p>
<p>builder.Services.AddHangfireServer();</p>
<p>app.UseHangfireDashboard("/hangfire", new DashboardOptions</p>
<p>{</p>
<p>Authorization = new[] { new AdminOnlyAuthorization() }</p>
<p>});</p>
<p>BackgroundJob.Enqueue&lt;IEmailService&gt;(svc =&gt; svc.SendWelcomeAsync(userId));</p>
<p>BackgroundJob.Schedule&lt;IEmailService&gt;(svc =&gt; svc.SendReminderAsync(userId),</p>
<p>TimeSpan.FromHours(24));</p>
<p>RecurringJob.AddOrUpdate&lt;IBillingService&gt;(</p>
<p>"daily-invoice-generation",</p>
<p>svc =&gt; svc.GenerateDailyInvoicesAsync(),</p>
<p>Cron.Daily(2));</p></td>
</tr>
</tbody>
</table>

## 7.3 En SaaS multi-tenant: TenantId en jobs

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><p><strong>Regla crítica para jobs en SaaS</strong></p>
<p>Cada job DEBE recibir el TenantId como parámetro explícito. Nunca confiar en 'el contexto actual' porque el job corre en otro thread/proceso. El handler del job recrea el contexto del tenant antes de ejecutar lógica de negocio.</p></td>
</tr>
</tbody>
</table>

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>// MAL — el job no sabe a qué tenant pertenece</p>
<p>public Task SendWelcomeEmail(Guid userId) {</p>
<p>var user = _db.Users.Find(userId); // ← qué tenant filtra?</p>
<p>}</p>
<p>// BIEN — el TenantId es explícito</p>
<p>public async Task SendWelcomeEmail(Guid tenantId, Guid userId) {</p>
<p>_tenantContext.SetTenant(tenantId);</p>
<p>var user = await _db.Users.FindAsync(userId);</p>
<p>}</p></td>
</tr>
</tbody>
</table>



---

*Rogelio Arriaga Gonzalez*
