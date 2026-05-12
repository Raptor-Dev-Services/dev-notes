# 12 — Proxy

**Categoría:** Estructural

**Intención:** Proporciona un sustituto o marcador de posición para otro objeto. Un proxy controla el acceso al objeto original, permitiéndote realizar algo antes o después de que la petición llegue al objeto original.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.4 Structural Patterns: Proxy

---

## El problema

Tienes un objeto pesado que solo debería crearse cuando realmente se necesita (lazy initialization). O quieres añadir logging/seguridad a un objeto sin modificarlo. O el objeto está en otro servidor y necesitas un representante local. En todos estos casos, el Proxy actúa como intermediario.

---

## Analogía

Una tarjeta de crédito. Cuando pagas con tarjeta, el comercio no accede directamente a tu cuenta bancaria. La tarjeta es un proxy de tu cuenta — misma interfaz (pago), pero añade: verificación de fondos, registro de transacción, protección de fraude, y diferimiento del cobro real. El comercio usa la misma interfaz sin saber los detalles del banco.

---

## Estructura

```
ISubject
└── Request()

RealSubject : ISubject
└── Request() → hace el trabajo real

Proxy : ISubject
├── _realSubject: RealSubject
└── Request()
    ├── CheckAccess()       ← antes del real
    ├── _realSubject.Request()  ← delega al real
    └── LogAccess()         ← después del real

Client → ISubject (no sabe si habla con RealSubject o Proxy)
```

---

## Código del ejemplo conceptual

```csharp
// Interfaz común para RealSubject y Proxy
public interface ISubject
{
    void Request();
}

// El objeto real que hace el trabajo
class RealSubject : ISubject
{
    public void Request()
    {
        Console.WriteLine("RealSubject: Handling Request.");
    }
}

// El Proxy — misma interfaz, añade comportamiento
class Proxy : ISubject
{
    private RealSubject _realSubject;

    public Proxy(RealSubject realSubject)
    {
        _realSubject = realSubject;
    }

    public void Request()
    {
        // Antes de la petición real
        if (CheckAccess())
        {
            _realSubject.Request();  // delega al real
            LogAccess();             // después de la petición
        }
    }

    private bool CheckAccess()
    {
        Console.WriteLine("Proxy: Checking access prior to firing a real request.");
        return true;  // en producción: verificar permisos, cuotas, etc.
    }

    private void LogAccess()
    {
        Console.WriteLine("Proxy: Logging the time of request.");
    }
}

// El cliente trabaja con ISubject — no sabe si es Real o Proxy
void ClientCode(ISubject subject) { subject.Request(); }

// Con el objeto real:
var real  = new RealSubject();
ClientCode(real);
// "RealSubject: Handling Request."

// Con el proxy:
var proxy = new Proxy(real);
ClientCode(proxy);
// "Proxy: Checking access prior to firing a real request."
// "RealSubject: Handling Request."
// "Proxy: Logging the time of request."
```

---

## Tipos de Proxy

### 1. Virtual Proxy — Lazy Initialization

```csharp
// El objeto pesado se crea solo cuando se necesita
public class HeavyServiceProxy : IDataService
{
    private HeavyDataService? _service;  // null hasta primer uso

    public IEnumerable<string> GetData()
    {
        // Lazy init: solo crear cuando se necesita
        _service ??= new HeavyDataService();  // costoso de crear
        return _service.GetData();
    }
}
```

### 2. Protection Proxy — Control de acceso

```csharp
public class SecuredFileProxy : IFileAccess
{
    private readonly IFileAccess _file;
    private readonly string _userRole;

    public SecuredFileProxy(IFileAccess file, string userRole)
    {
        _file = file;
        _userRole = userRole;
    }

    public string Read(string path)
    {
        if (_userRole != "admin" && _userRole != "user")
            throw new UnauthorizedAccessException("No tiene permisos para leer.");
        return _file.Read(path);
    }

    public void Write(string path, string content)
    {
        if (_userRole != "admin")
            throw new UnauthorizedAccessException("Solo administradores pueden escribir.");
        _file.Write(path, content);
    }
}
```

### 3. Logging/Caching Proxy

```csharp
public class CachingRepositoryProxy<T> : IRepository<T>
{
    private readonly IRepository<T> _repo;
    private readonly IMemoryCache _cache;

    public CachingRepositoryProxy(IRepository<T> repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        string key = $"{typeof(T).Name}:{id}";

        if (_cache.TryGetValue(key, out T? cached))
            return cached;  // hit: retorna del cache

        var item = await _repo.GetByIdAsync(id);  // miss: va a la DB

        if (item != null)
            _cache.Set(key, item, TimeSpan.FromMinutes(5));  // cachea para el futuro

        return item;
    }
}
```

### 4. Remote Proxy

```csharp
// El cliente habla con el proxy local como si fuera el objeto remoto
public class RemoteServiceProxy : IInventoryService
{
    private readonly HttpClient _http;

    public RemoteServiceProxy(HttpClient http) { _http = http; }

    public async Task<Product?> GetProductAsync(Guid id)
    {
        // "Delega" al servicio real que está en otro servidor
        var response = await _http.GetAsync($"/api/products/{id}");
        return await response.Content.ReadFromJsonAsync<Product>();
    }
}
// El cliente usa IInventoryService sin saber si es local o remoto
```

---

## En este proyecto

```csharp
// MainDapperDbConnection es un Proxy sobre Dapper:
// - Misma "interfaz" (mismos métodos: QueryAsync, ExecuteAsync, etc.)
// - Añade: logging de duración, gestión de conexión, patrón de errores

public sealed class MainDapperDbConnection
{
    private readonly MainDbConnectionFactory _factory;
    private readonly ILogger<MainDapperDbConnection> _logger;

    // Proxy: mismo método que Dapper, pero con logging agregado
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param, ...)
    {
        using var conn = _factory.OpenConnection();
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await conn.QueryAsync<T>(sql, param);  // el "RealSubject" = Dapper
            _logger.LogDebug("SQL OK {Elapsed}ms: {Sql}", sw.ElapsedMilliseconds, sql);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL ERROR {Elapsed}ms: {Sql}", sw.ElapsedMilliseconds, sql);
            throw;
        }
    }
}
// El código cliente (ExampleUsersSql) llama _db.QueryAsync<T>() sin saber del logging
```

---

## Proxy vs Decorator vs Adapter

| Patrón | Interfaz del wrapper | Propósito principal |
|--------|---------------------|---------------------|
| **Proxy** | Idéntica al original | Controlar acceso (lazy, cache, security, remote) |
| **Decorator** | Idéntica al original | Extender comportamiento (añadir capas) |
| **Adapter** | Diferente al original | Convertir interfaz incompatible |

**Proxy vs Decorator:** Ambos implementan la misma interfaz. La diferencia es semántica:
- Proxy **gestiona el ciclo de vida** del objeto real (puede crearlo, destruirlo, decidir si acceder).
- Decorator **asume que el objeto ya existe** y solo añade comportamiento.

---

## Cuándo usar

- **Lazy initialization (Virtual Proxy):** Cuando un objeto costoso solo se necesita a veces.
- **Control de acceso (Protection Proxy):** Cuando diferentes usuarios tienen distintos permisos.
- **Caché (Caching Proxy):** Para evitar llamadas repetidas a recursos costosos (DB, API).
- **Logging/timing:** Para agregar observabilidad sin modificar el objeto real.
- **Remote Proxy:** Para representar un objeto en otro proceso/servidor.

## Cuándo NO usar

- Cuando el procesamiento adicional añade latencia inaceptable en el camino crítico.
- Cuando el objeto real es simple — el overhead del proxy no está justificado.
- Cuando puedes añadir el comportamiento directamente al objeto real sin violaciones de principios.


---

*Rogelio Arriaga Gonzalez*
