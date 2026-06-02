# 16 — Output Caching y Response Caching

ASP.NET Core ofrece dos tipos de caching a nivel de respuesta HTTP. Output caching (nativo desde .NET 7) es el más flexible: soporta invalidación por tags, variación por headers/query, y políticas personalizadas.

> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.9 Caching, Queuing, and Resilient Background Services  
> Fuente: *ASP.NET Core 9 Essentials* (Packt) — Ch.7 Adding Capabilities to Applications

---

## Output Caching vs Response Caching

| | Output Caching | Response Caching |
|--|----------------|-----------------|
| Almacenamiento | Servidor (en memoria) | Controlado por headers HTTP |
| Invalidación por tags | Sí (`EvictByTagAsync`) | No |
| Variación por tenant/header | Sí (`SetVaryByHeader`) | Limitado |
| Configurable desde código | Sí — políticas tipadas | No — solo headers |
| Disponible desde | .NET 7 | .NET Core 1.0 |
| Recomendado para | APIs con invalidación activa | Recursos estáticos/públicos |

---

## Output Caching — configuración

```csharp
// Program.cs
builder.Services.AddOutputCache(options =>
{
    // Política para datos públicos (planes de precios, contenido estático)
    options.AddPolicy("public-short", builder =>
        builder.Expire(TimeSpan.FromMinutes(5)));

    // Política por tenant — varía por header y query, invalidable por tag
    options.AddPolicy("per-tenant", builder =>
        builder
            .Expire(TimeSpan.FromMinutes(10))
            .SetVaryByQuery("*")                          // cachea por cada combinación de query params
            .SetVaryByHeader("X-Tenant")                  // cachea por tenant
            .Tag("tenant-data"));                         // tag para invalidar por grupo

    // Política para dashboard con autenticación
    options.AddPolicy("authenticated-short", builder =>
        builder
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByHeader("Authorization")
            .SetCacheKeyPrefix("auth-"));
});

app.UseOutputCache();   // debe ir después de UseRouting y antes de MapControllers

app.MapGet("/api/plans",     GetPublicPlans).CacheOutput("public-short");
app.MapGet("/api/dashboard", GetDashboard)  .CacheOutput("per-tenant");
```

---

## Invalidación por tags

```csharp
// Al modificar datos, invalidar el caché del grupo de tags afectado
public class PlanService(IOutputCacheStore cache)
{
    public async Task UpdatePlanAsync(Plan plan, CancellationToken ct)
    {
        await _db.UpdateAsync(plan, ct);
        await cache.EvictByTagAsync("tenant-data", ct);   // invalida todas las entradas con este tag
    }
}
```

---

## Response Caching — para recursos públicos

```csharp
// Program.cs
builder.Services.AddResponseCaching();
app.UseResponseCaching();

// En el endpoint — usando headers Cache-Control estándar
app.MapGet("/api/public/features", (HttpContext context) =>
{
    context.Response.GetTypedHeaders().CacheControl =
        new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(10),
        };
    return Results.Ok(/* datos públicos */);
});
```

---

## Diferencia con el caché de aplicación (IMemoryCache / IDistributedCache)

```
Output/Response Caching      → cachea la RESPUESTA HTTP completa (status + headers + body)
                               El cliente no repite el request o la API lo sirve sin llegar al handler

IMemoryCache                 → cachea DATOS de negocio dentro del proceso
                               El request llega al handler, pero el handler usa datos cacheados

IDistributedCache (Redis)    → igual que IMemoryCache pero compartido entre instancias
                               Necesario en deployments con múltiples réplicas
```

Para más detalles sobre `IMemoryCache` y Redis distribuido ver `04-backend/05-caching.md`.

---

## Cuándo usar output caching

- Endpoints de datos que cambian con poca frecuencia y son consultados frecuentemente
- Datos por tenant que pueden invalidarse por evento (actualización de config, cambio de plan)
- APIs públicas (planes de precios, documentación, catálogos)

**No usar** en endpoints con datos muy dinámicos por usuario o cuando la respuesta depende de datos que cambian en cada request.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Output Caching | Caché de respuesta HTTP completa gestionada por middleware de ASP.NET Core — independiente del código del endpoint |
| Response Caching | Mecanismo basado en headers HTTP Cache-Control que delega la caché al cliente o proxy |
| IOutputCacheStore | Interfaz del store de output cache — reemplazable por implementaciones custom (Redis, etc.) |
| EvictByTagAsync | Método para invalidar todas las entradas de output cache que tengan un tag específico |
| SetVaryByHeader | Configura que la caché genere entradas distintas según el valor de un header HTTP |
| CacheOutput | Atributo para aplicar una política de output cache a un endpoint o controller |
| Tag | Etiqueta asociada a entradas de caché que permite invalidación agrupada por evento |
| AddOutputCache | Método de DI que registra el middleware y permite definir políticas con nombre |
| VaryByQuery | Configuración de output cache que genera entradas distintas según parámetros de query string |
| SetVaryByRouteValue | Configura variación de caché por parámetro de ruta — útil para cachear por tenant o recurso |
| Política de caché | Configuración nombrada de output cache con duración, tags y criterios de variación |

---

*Rogelio Arriaga Gonzalez*
