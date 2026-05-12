# 5 · Rate limiting

Limitar peticiones por segundo protege contra abuso, ataques DDoS, y tenants que (intencionalmente o no) saturan tu API. Desde .NET 7 hay middleware nativo.

## 5.1 Estrategias

| **Estrategia** | **Cómo funciona** |
|----|----|
| **Fixed Window** | X peticiones por ventana fija. Simple. Permite picos al cambio de ventana. |
| **Sliding Window** | Ventana móvil. Más justo, ligeramente más caro de calcular. |
| **Token Bucket** | Tokens se regeneran a cierta tasa. Permite ráfagas. |
| **Concurrency** | Limita peticiones simultáneas. Útil para endpoints costosos. |

## 5.2 Configuración en .NET 8+

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C# Program.cs</em></td>
</tr>
<tr>
<td><p>using System.Threading.RateLimiting;</p>
<p>builder.Services.AddRateLimiter(options =&gt;</p>
<p>{</p>
<p>options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;</p>
<p>options.AddPolicy("per-tenant", context =&gt;</p>
<p>{</p>
<p>var tenantId = context.User.FindFirst("tenant_id")?.Value ?? "anonymous";</p>
<p>return RateLimitPartition.GetSlidingWindowLimiter(tenantId, _ =&gt;</p>
<p>new SlidingWindowRateLimiterOptions</p>
<p>{</p>
<p>PermitLimit = 1000, Window = TimeSpan.FromMinutes(1),</p>
<p>SegmentsPerWindow = 6, QueueLimit = 0,</p>
<p>});</p>
<p>});</p>
<p>options.AddPolicy("auth-strict", context =&gt;</p>
<p>{</p>
<p>var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";</p>
<p>return RateLimitPartition.GetFixedWindowLimiter(ip, _ =&gt;</p>
<p>new FixedWindowRateLimiterOptions</p>
<p>{</p>
<p>PermitLimit = 5, Window = TimeSpan.FromMinutes(5), QueueLimit = 0,</p>
<p>});</p>
<p>});</p>
<p>});</p>
<p>app.UseRateLimiter();</p>
<p>app.MapPost("/api/auth/login", LoginHandler).RequireRateLimiting("auth-strict");</p>
<p>app.MapControllers().RequireRateLimiting("per-tenant");</p></td>
</tr>
</tbody>
</table>



---

*Rogelio Arriaga Gonzalez*
