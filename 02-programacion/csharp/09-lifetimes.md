# 09 — DI Lifetimes: Singleton, Scoped, Transient

Cuando registras una dependencia, debes decidir **cuánto tiempo vive** el objeto que el container crea. Esta decisión impacta el rendimiento, la seguridad entre peticiones y los posibles bugs.

> Fuente: *Apps and Services with .NET 8* (Mark J. Price) — Ch.14 Dependency Injection and Service Lifetimes

---

## Los tres lifetimes

### Singleton — una instancia para toda la aplicación

```csharp
services.AddSingleton<MainDbConnectionFactory>();
```

- Se crea **una sola vez** al arrancar la aplicación (o la primera vez que se pide).
- El **mismo objeto** se reutiliza en todas las peticiones, todos los hilos, toda la vida de la app.
- Se destruye cuando la app se apaga.

**Analogía:** el gerente general de una empresa. Hay uno solo. Todos los empleados le preguntan a él. No cambia entre clientes.

```
App arranca
    → MainDbConnectionFactory creado (1 vez)
Petición 1 llega
    → usa la misma MainDbConnectionFactory
Petición 2 llega
    → usa la misma MainDbConnectionFactory (no se crea nueva)
...
App se apaga
    → MainDbConnectionFactory destruido
```

**Cuándo usar:**
- Sin estado (o estado que NO cambia entre peticiones)
- Objetos costosos de crear: fábricas, clientes HTTP, configuraciones cacheadas
- Thread-safe (puede ser usado simultáneamente por múltiples hilos)

**En este proyecto:** `MainDbConnectionFactory` — solo sabe cómo crear conexiones, no tiene estado propio.

---

### Scoped — una instancia por petición HTTP

```csharp
services.AddScoped<MainDapperDbConnection>();
services.AddScoped<ExampleUsersSql>();
services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
services.AddScoped(typeof(ResultViewModel<>));
```

- Se crea **al inicio de cada request HTTP**.
- El **mismo objeto** se reutiliza dentro de la misma petición (todos los que lo pidan en esa petición reciben la misma instancia).
- Se destruye al finalizar la petición.

**Analogía:** el cajero asignado para tu trámite en el banco. Cuando llegas, se te asigna un cajero. Todas las operaciones de tu visita son con ese mismo cajero. Cuando terminas, el cajero queda libre para otro cliente.

```
Petición 1 llega
    → MainDapperDbConnection creado (solo para esta petición)
    → ExampleUsersSql usa ESA MainDapperDbConnection
    → ExampleUserRepository usa ESA ExampleUsersSql
    → Handler usa ESE repositorio
    → Presenter usa MISMO _viewModel de la petición
Petición 1 termina
    → Todos los Scoped se destruyen

Petición 2 llega
    → NUEVOS objetos Scoped creados — no comparten estado con la petición 1
```

**Cuándo usar:**
- Todo lo que debe ser fresco por petición
- Objetos que guardan estado de la petición actual (contexto de usuario, transacciones)
- La gran mayoría de los servicios de una API

**En este proyecto:** casi todo — repositorios, handlers, presenters, ViewModels, conexión DB.

---

### Transient — nueva instancia cada vez que se pide

```csharp
services.AddTransient<IEmailSender, EmailSender>();
```

- Se crea **cada vez** que el container lo resuelve.
- No hay reutilización — cada `GetService<T>()` crea un objeto nuevo.
- Se destruye cuando el scope que lo pidió termina (si implementa IDisposable).

**Analogía:** una hoja de papel. Cada vez que la pides, te dan una nueva.

```
Petición 1 llega
    → ServicioA necesita IEmailSender → nuevo EmailSender creado
    → ServicioB necesita IEmailSender → OTRO nuevo EmailSender creado
    (dos instancias distintas en la misma petición)
```

**Cuándo usar:**
- Objetos ligeros y sin estado
- Objetos que NO deben compartirse ni siquiera dentro de la misma petición
- Raramente necesario — si el objeto no tiene estado, Singleton es mejor; si lo tiene, Scoped es mejor

**En este proyecto:** no se usa actualmente.

---

## Tabla comparativa

| Lifetime | Se crea | Cuándo destruye | Instancias en 3 peticiones simultáneas |
|----------|---------|-----------------|----------------------------------------|
| `Singleton` | 1 vez al arrancar | Al apagar la app | 1 (compartida) |
| `Scoped` | 1 vez por petición | Al terminar la petición | 3 (una por petición) |
| `Transient` | Cada vez que se pide | Al salir del scope | N (una por pedido) |

---

## El error más peligroso — Captive Dependency

Ocurre cuando **un Singleton captura un Scoped**. El Scoped debería vivir solo una petición, pero el Singleton lo retiene para siempre.

```csharp
// ❌ ERROR GRAVE — Singleton con dependencia Scoped
public sealed class CacheGlobal   // Singleton
{
    private readonly MainDapperDbConnection _db;  // Scoped — vive solo una petición
    
    public CacheGlobal(MainDapperDbConnection db) => _db = db;
    // El container crea CacheGlobal una sola vez.
    // _db apunta a la conexión de LA PRIMERA petición.
    // Después de que esa petición termina, _db está "muerta".
    // Las siguientes peticiones usan una conexión ya cerrada → excepción o datos corruptos.
}

// Registro incorrecto:
services.AddSingleton<CacheGlobal>();   // Singleton
services.AddScoped<MainDapperDbConnection>(); // Scoped
// ASP.NET Core detecta esto y lanza InvalidOperationException al arrancar (en modo desarrollo)
```

**Solución — opciones:**

```csharp
// Opción A — Hacer todo Singleton (si es thread-safe)
services.AddSingleton<MainDbConnectionFactory>();
// Y la caché usa la factory para crear conexiones cuando las necesita — no guarda la conexión

// Opción B — Usar IServiceProvider para resolver Scoped dentro de Singleton
public sealed class CacheGlobal
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public CacheGlobal(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
    
    public async Task<ExampleUser?> GetAsync(Guid id)
    {
        // Crear un scope temporal para cada operación
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDapperDbConnection>();
        return await db.QuerySingleAsync<ExampleUser>("SELECT ...", new { id });
    }
}
```

---

## Detectar problemas de lifetime en desarrollo

ASP.NET Core, en modo desarrollo (`ASPNETCORE_ENVIRONMENT = Development`), lanza una excepción al arrancar si detecta:
- Un Singleton que depende de un Scoped
- Un Singleton que depende de un Transient

```
InvalidOperationException: Cannot consume scoped service 'MainDapperDbConnection'
from singleton 'CacheGlobal'.
```

En producción este chequeo puede estar desactivado — pero el bug existe igual.

Para activar el chequeo en producción:

```csharp
// Program.cs
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;     // detecta Scoped en Singleton
    options.ValidateOnBuild = true;    // valida al arrancar, no al usar
});
```

---

## Registro de todos los servicios del proyecto

```csharp
// ═══════════════════════════════════════════════════════
// Application/ServiceCollectionEx.cs
// ═══════════════════════════════════════════════════════
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    // AddMediator escanea el ensamblado y registra:
    // - Todos los IRequestHandler<TRequest, TResponse> como Transient (comportamiento interno)
    // - El pipeline de interacción (InteractorPipeline)
    services.AddMediator(typeof(GetExampleUserHandler).Assembly);
    return services;
}

// ═══════════════════════════════════════════════════════
// Infrastructure/ServiceCollectionEx.cs
// ═══════════════════════════════════════════════════════
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services, IConfiguration configuration)
{
    // Singleton — sin estado, costosa de crear, thread-safe
    services.AddSingleton<MainDbConnectionFactory>();
    
    // Scoped — una conexión por petición
    services.AddScoped<MainDapperDbConnection>();
    
    // Scoped — los Sql objects dependen de MainDapperDbConnection (Scoped)
    services.AddScoped<ExampleUsersSql>();
    
    // Scoped — el repositorio depende de ExampleUsersSql (Scoped)
    services.AddScoped<IExampleUserRepository, ExampleUserRepository>();
    
    return services;
}

// ═══════════════════════════════════════════════════════
// WebApi/ServiceCollectionEx.cs
// ═══════════════════════════════════════════════════════
public static IServiceCollection AddWebApiServices(this IServiceCollection services)
{
    // Scoped — un ViewModel por petición, genérico abierto
    services.AddScoped(typeof(ResultViewModel<>));
    
    // Scoped — presenters dependen de ResultViewModel<> (Scoped)
    services.AddScoped<
        INotificationHandler<GetExampleUserResponse>,
        GetExampleUserPresenter>();
    
    services.AddScoped<
        INotificationHandler<GetExampleUsersResponse>,
        GetExampleUsersPresenter>();
    
    // Repite para cada caso de uso que tenga presenter...
    
    return services;
}
```

---

## IDisposable y lifetimes

Si un servicio implementa `IDisposable`, el container lo destruye automáticamente:

```csharp
public sealed class ConexionBd : IDisposable
{
    private NpgsqlConnection _connection;
    
    public ConexionBd(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }
    
    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}

// Con Scoped:
services.AddScoped<ConexionBd>();
// Al terminar la petición HTTP, el container llama Dispose() automáticamente → conexión cerrada

// Con Singleton:
services.AddSingleton<ConexionBd>();
// Dispose() solo se llama al apagar la app
```

---

## Acceder a servicios fuera del contexto HTTP

En workers, jobs, migraciones — donde no hay petición HTTP:

```csharp
// Opción A — IServiceScopeFactory (la más limpia)
public class MiWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public MiWorker(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Crear un scope manualmente (simula una "petición")
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExampleUserRepository>();
            
            var users = await repo.GetPagedAsync(1, 100, ct);
            // ... procesar
            
            // Al salir del using, el scope se destruye y los Scoped se disponen
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }
}
```

---

## Resumen de reglas

1. **Usa `Singleton`** para: fábricas, configuraciones, clientes HTTP, objetos sin estado que son costosos de crear.
2. **Usa `Scoped`** para: todo lo que necesita estado por petición — repositorios, handlers, presenters, ViewModels, transacciones.
3. **Usa `Transient`** para: objetos muy ligeros sin estado que no deben compartirse (raramente necesario).
4. **Nunca** inyectes un Scoped en un Singleton.
5. **Nunca** inyectes un Transient en un Singleton si el Transient tiene estado.
6. Cuando dudes: **Scoped** es la elección más segura para una API web.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Lifetime (ciclo de vida) | Duración de la instancia de un servicio gestionada por el DI container: Singleton, Scoped o Transient |
| Singleton | Lifetime donde se crea una única instancia compartida por toda la aplicación desde el arranque hasta el apagado |
| Scoped | Lifetime donde se crea una instancia por cada petición HTTP; todos los servicios dentro de la misma petición comparten la instancia |
| Transient | Lifetime donde se crea una instancia nueva cada vez que el servicio es solicitado al container |
| Captive Dependency | Error donde un Singleton retiene un servicio Scoped, haciendo que este viva más tiempo del previsto y comparta estado entre peticiones |
| `IServiceScope` | Ámbito de DI creado manualmente; necesario para consumir servicios Scoped desde un Singleton o un job en segundo plano |
| `MainDbConnectionFactory` | Singleton del proyecto: sin estado propio, compartir entre peticiones es seguro y eficiente |
| `MainDapperDbConnection` | Scoped del proyecto: una instancia por petición — gestiona la conexión durante el ciclo de vida de la request |
| `ResultViewModel<T>` | Scoped del proyecto: almacena el resultado de la operación para que el controller lo lea al final |
| Thread-safety | Propiedad requerida para cualquier Singleton: su código debe funcionar correctamente cuando múltiples hilos lo usan simultáneamente |
| `AddSingleton<>()` / `AddScoped<>()` / `AddTransient<>()` | Métodos de registro de DI en ASP.NET Core que definen el lifetime de cada servicio |

---

*Rogelio Arriaga Gonzalez*
