# 4 · CORS

CORS (Cross-Origin Resource Sharing) es el mecanismo del navegador que permite o bloquea peticiones de un origen distinto al del documento HTML. En SaaS típico, frontend y backend viven en dominios diferentes, así que CORS es obligatorio.

## 4.1 Configuración correcta en ASP.NET Core

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C# Program.cs</em></td>
</tr>
<tr>
<td><p>var corsOrigins = builder.Configuration</p>
<p>.GetSection("Cors:AllowedOrigins")</p>
<p>.Get&lt;string[]&gt;() ?? Array.Empty&lt;string&gt;();</p>
<p>builder.Services.AddCors(options =&gt;</p>
<p>{</p>
<p>options.AddPolicy("SpaPolicy", policy =&gt;</p>
<p>{</p>
<p>policy</p>
<p>.WithOrigins(corsOrigins)</p>
<p>.AllowAnyHeader()</p>
<p>.AllowAnyMethod()</p>
<p>.AllowCredentials()</p>
<p>.SetPreflightMaxAge(TimeSpan.FromMinutes(10));</p>
<p>});</p>
<p>});</p>
<p>// CRÍTICO: orden importa. UseCors va antes de UseAuthentication/UseAuthorization</p>
<p>app.UseCors("SpaPolicy");</p>
<p>app.UseAuthentication();</p>
<p>app.UseAuthorization();</p></td>
</tr>
</tbody>
</table>

## 4.2 Wildcard subdomains para SaaS multi-tenant

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>options.AddPolicy("TenantPolicy", policy =&gt;</p>
<p>{</p>
<p>policy.SetIsOriginAllowed(origin =&gt;</p>
<p>{</p>
<p>var uri = new Uri(origin);</p>
<p>return uri.Host == "taskflow.com"</p>
<p>|| uri.Host.EndsWith(".taskflow.com")</p>
<p>|| uri.Host == "localhost";</p>
<p>})</p>
<p>.AllowAnyHeader().AllowAnyMethod().AllowCredentials();</p>
<p>});</p></td>
</tr>
</tbody>
</table>



---

*Rogelio Arriaga Gonzalez*
