# 9 · Output caching y response caching

.NET 7+ trae output caching nativo con políticas finas, incluyendo invalidación por tags.

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>builder.Services.AddOutputCache(options =&gt;</p>
<p>{</p>
<p>options.AddPolicy("public-short", builder =&gt;</p>
<p>builder.Expire(TimeSpan.FromMinutes(5)));</p>
<p>options.AddPolicy("per-tenant", builder =&gt;</p>
<p>builder</p>
<p>.Expire(TimeSpan.FromMinutes(10))</p>
<p>.SetVaryByQuery("*")</p>
<p>.SetVaryByHeader("X-Tenant")</p>
<p>.Tag("tenant-data"));</p>
<p>});</p>
<p>app.UseOutputCache();</p>
<p>app.MapGet("/api/plans", GetPublicPlans).CacheOutput("public-short");</p>
<p>app.MapGet("/api/dashboard", GetDashboard).CacheOutput("per-tenant");</p>
<p>public class PlanService(IOutputCacheStore cache)</p>
<p>{</p>
<p>public async Task UpdatePlanAsync(Plan plan, CancellationToken ct)</p>
<p>{</p>
<p>// ... actualizar BD</p>
<p>await cache.EvictByTagAsync("tenant-data", ct);</p>
<p>}</p>
<p>}</p></td>
</tr>
</tbody>
</table>



> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.9 Caching, Queuing, and Resilient Background Services

---

*Rogelio Arriaga Gonzalez*
