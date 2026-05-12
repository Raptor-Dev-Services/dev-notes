# 01 — Singleton

**Categoría:** Creacional

**Intención:** Garantizar que una clase tenga una única instancia y proporcionar un punto de acceso global a ella.

---

## El problema

Imagina que tienes una clase `ConfigurationManager` que lee un archivo de configuración. Si cada parte del sistema crea su propia instancia, tienes N lecturas del archivo, posibles estados inconsistentes, y desperdicio de recursos.

```csharp
// ❌ Sin Singleton — cada llamada crea una instancia nueva
var config1 = new ConfigurationManager();  // lee el archivo
var config2 = new ConfigurationManager();  // lee el archivo OTRA VEZ
// config1 y config2 son objetos distintos
```

---

## Analogía

El gobierno de un país. Un país puede tener un solo gobierno oficial. No importa cuántas veces "accedas" al gobierno — siempre es el mismo. Los ciudadanos no crean nuevos gobiernos — acceden al existente.

---

## Estructura

```
Singleton
├── constructor privado          ← nadie puede crear instancias desde fuera
├── _instance: Singleton         ← la única instancia, almacenada en campo estático
├── _lock: object                ← para thread-safety
└── GetInstance(): Singleton     ← único punto de acceso
```

---

## Implementación — Non-Thread-Safe (simple, solo un hilo)

```csharp
public sealed class Singleton
{
    private Singleton() { }

    private static Singleton _instance;

    public static Singleton GetInstance()
    {
        if (_instance == null)
        {
            _instance = new Singleton();
        }
        return _instance;
    }

    public void SomeBusinessLogic() { /* ... */ }
}

// Uso:
Singleton s1 = Singleton.GetInstance();
Singleton s2 = Singleton.GetInstance();
// s1 == s2 → true. Misma instancia.
```

**Problema:** No es seguro con múltiples hilos. Dos hilos pueden pasar `null` simultáneamente y crear dos instancias.

---

## Implementación — Thread-Safe (doble verificación con lock)

Del archivo fuente `ThreadSafe/Program.cs`:

```csharp
class Singleton
{
    private Singleton() { }

    private static Singleton _instance;

    // Lock para sincronizar hilos en el primer acceso
    private static readonly object _lock = new object();

    public static Singleton GetInstance(string value)
    {
        // Verificación rápida sin lock — una vez creada, no necesitamos lock
        if (_instance == null)
        {
            // Solo el primer hilo pasa aquí — los demás esperan
            lock (_lock)
            {
                // Verificación dentro del lock — otro hilo pudo haberla creado
                if (_instance == null)
                {
                    _instance = new Singleton();
                    _instance.Value = value;
                }
            }
        }
        return _instance;
    }

    public string Value { get; set; }
}
```

**Por qué doble verificación:**
- El `if` externo evita entrar al `lock` en cada llamada una vez creada la instancia → rendimiento.
- El `if` interno dentro del `lock` evita la creación doble si dos hilos pasan el `if` externo simultáneamente.

---

## La forma moderna en C# — `Lazy<T>`

```csharp
public sealed class Singleton
{
    private static readonly Lazy<Singleton> _lazy =
        new Lazy<Singleton>(() => new Singleton());

    private Singleton() { }

    public static Singleton Instance => _lazy.Value;

    public void DoWork() { /* ... */ }
}

// Uso:
var instance = Singleton.Instance;
```

`Lazy<T>` maneja el thread-safety automáticamente. Es la forma más limpia en C# moderno.

---

## Singleton en ASP.NET Core — via DI

En .NET moderno, el patrón Singleton se implementa generalmente a través del contenedor de DI, **no manualmente**:

```csharp
// Registrar como Singleton en DI:
services.AddSingleton<IMyService, MyService>();
// El contenedor garantiza que solo existe una instancia por aplicación.

// ❌ Evitar Singleton manual en código de aplicación:
// Dificulta el testing (no se puede mockear)
// Crea estado global difícil de razonar
```

**Regla:** En una aplicación ASP.NET Core, preferir `services.AddSingleton<>()` sobre implementar el patrón manualmente.

---

## En este proyecto

```csharp
// Infrastructure/ServiceCollectionEx.cs
services.AddSingleton<MainDbConnectionFactory>();
// ↑ Una sola instancia de la fábrica de conexiones para toda la vida de la app.
// Eficiente: el NpgsqlDataSource (costoso de crear) se reutiliza.

// No se usa Singleton manual — todo pasa por DI.
```

---

## Cuándo usar

- Cuando necesitas exactamente una instancia compartida por toda la aplicación.
- Recursos costosos de crear: pools de conexiones, caches, loggers, configuración.
- Cuando el estado global tiene sentido (raro, pero existe).

## Cuándo NO usar

- Como sustituto de variables globales (code smell).
- Para objetos que tienen estado mutable que varía por request (usa Scoped).
- Cuando dificulta el testing — prefiere DI con `AddSingleton`.
- En objetos que tienen lógica de negocio dependiente de contexto.

---

## Problemas comunes

### Captive Dependency — el error más peligroso

```csharp
// ❌ Singleton consume un Scoped — el Scoped queda "capturado" en el Singleton
public class MyService  // registrado como Singleton
{
    private readonly IRepository _repo;  // registrado como Scoped

    public MyService(IRepository repo)   // ← el Scoped es inyectado y vive tanto como el Singleton
    {
        _repo = repo;
    }
}
// El _repo ya no es Scoped — vive para siempre dentro del Singleton.
// Resultado: un repositorio compartido por TODAS las requests. Comportamiento indefinido.
```

**Solución:** Ver documento [09 — DI Lifetimes](../../csharp-primer/09-lifetimes.md).

---

## Comparación: Singleton manual vs DI Singleton

| Aspecto | Singleton manual | `services.AddSingleton<>()` |
|---------|-----------------|----------------------------|
| Testing | Difícil (no se puede mockear) | Fácil (inyectar mock) |
| Thread-safety | Debes implementarla | El container lo maneja |
| Lazy init | Manual con `Lazy<T>` | Automático |
| Ciclo de vida | Para siempre | Para siempre (mismo resultado) |
| Recomendado | Raro, casos muy específicos | Sí — uso normal en .NET |


---

*Rogelio Arriaga Gonzalez*
