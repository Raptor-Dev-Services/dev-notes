# 8 · API versioning, OpenAPI y documentación

## 8.1 API Versioning

| **Estrategia** | **Pros y contras** |
|----|----|
| **URL: /api/v1/customers** | Visible, fácil de cachear. Cambia URLs. |
| **Header: api-version: 1.0** | URLs limpias. Menos visible, complica testing manual. |
| **Query string: ?api-version=1.0** | Fácil de testear. Mancha URLs. |
| **Media type** | RESTful purista. Complejo en práctica. |

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>builder.Services.AddApiVersioning(options =&gt;</p>
<p>{</p>
<p>options.DefaultApiVersion = new ApiVersion(1);</p>
<p>options.AssumeDefaultVersionWhenUnspecified = true;</p>
<p>options.ApiVersionReader = ApiVersionReader.Combine(</p>
<p>new UrlSegmentApiVersionReader(),</p>
<p>new HeaderApiVersionReader("api-version"));</p>
<p>options.ReportApiVersions = true;</p>
<p>})</p>
<p>.AddApiExplorer(options =&gt;</p>
<p>{</p>
<p>options.GroupNameFormat = "'v'V";</p>
<p>options.SubstituteApiVersionInUrl = true;</p>
<p>});</p>
<p>[ApiController]</p>
<p>[ApiVersion("1.0")] [ApiVersion("2.0")]</p>
<p>[Route("api/v{version:apiVersion}/customers")]</p>
<p>public class CustomersController : ControllerBase</p>
<p>{</p>
<p>[HttpGet, MapToApiVersion("1.0")]</p>
<p>public Task&lt;IActionResult&gt; GetV1() { /* ... */ }</p>
<p>[HttpGet, MapToApiVersion("2.0")]</p>
<p>public Task&lt;IActionResult&gt; GetV2() { /* ... */ }</p>
<p>}</p></td>
</tr>
</tbody>
</table>

## 8.2 OpenAPI / Swagger / Scalar

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>// .NET 9+ — OpenAPI nativo</p>
<p>builder.Services.AddOpenApi();</p>
<p>app.MapOpenApi();</p>
<p>// Para UI moderna, agregar Scalar</p>
<p>// dotnet add package Scalar.AspNetCore</p>
<p>app.MapScalarApiReference();</p>
<p>// O Swashbuckle clásico:</p>
<p>builder.Services.AddSwaggerGen(options =&gt;</p>
<p>{</p>
<p>options.SwaggerDoc("v1", new OpenApiInfo</p>
<p>{</p>
<p>Title = "TaskFlow API", Version = "v1",</p>
<p>Description = "API SaaS multi-tenant"</p>
<p>});</p>
<p>options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme</p>
<p>{</p>
<p>Name = "Authorization", Type = SecuritySchemeType.Http,</p>
<p>Scheme = "bearer", BearerFormat = "JWT"</p>
<p>});</p>
<p>});</p>
<p>app.UseSwagger();</p>
<p>app.UseSwaggerUI();</p></td>
</tr>
</tbody>
</table>

## 8.3 Problem Details (RFC 7807)

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>C#</em></td>
</tr>
<tr>
<td><p>builder.Services.AddProblemDetails(options =&gt;</p>
<p>{</p>
<p>options.CustomizeProblemDetails = ctx =&gt;</p>
<p>{</p>
<p>ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;</p>
<p>ctx.ProblemDetails.Extensions["tenantId"] =</p>
<p>ctx.HttpContext.User.FindFirst("tenant_id")?.Value;</p>
<p>};</p>
<p>});</p>
<p>app.UseExceptionHandler();</p>
<p>app.UseStatusCodePages();</p></td>
</tr>
</tbody>
</table>



---

*Rogelio Arriaga Gonzalez*
