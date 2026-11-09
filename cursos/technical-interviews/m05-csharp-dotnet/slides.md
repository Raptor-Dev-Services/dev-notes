<!-- _class: lead -->

# Módulo 5 - C# y .NET en la entrevista

### Preparación para entrevistas técnicas

**Rogelio Arriaga - Raptor Dev Services**

---

## Hoy

> Las preguntas de lenguaje se contestan con código que compila, no con definiciones.

- **Parte 1:** async y await
- **Parte 2:** inyección de dependencias y tiempos de vida
- **Parte 3:** genéricos y nulabilidad
- **Parte 4:** LINQ
- **Parte 5:** lo que de verdad preguntan
- **Práctica:** veinte preguntas típicas, cada una contestada con un ejemplo que corre

Lecturas: [08 - async/await](../../../02-programacion/csharp/08-async.md), [09 - Lifetimes](../../../02-programacion/csharp/09-lifetimes.md), [10 - Genéricos](../../../02-programacion/csharp/10-generics.md) y [13 - Nulabilidad](../../../02-programacion/csharp/13-nullability.md)

---

## Cómo contestar una pregunta de lenguaje

1. **Una frase de definición**, sin rodeos.
2. **Un ejemplo de código** que muestre la diferencia, mejor si se puede correr.
3. **Cuándo importa** en un sistema real.
4. **La trampa:** el error que comete quien solo memorizó la definición.

> "¿Qué diferencia hay entre `throw` y `throw ex`?"
> Mal: "Uno lanza la excepción y el otro también."
> Bien: "`throw` relanza conservando el rastro de pila; `throw ex` lo reinicia en esa línea. Importa al leer un log de producción: con `throw ex` pierdes dónde falló de verdad."

---

<!-- _class: lead -->

# Parte 1
## async y await

---

## Qué hace await

- `await` **no crea un hilo.** Si la operación no ha terminado, devuelve el hilo a quien lo prestó y registra lo que sigue para cuando termine.
- Sirve para **esperar entrada y salida** (base de datos, red, disco) sin bloquear un hilo mientras tanto.
- Con await, la excepción llega tal cual y se atrapa con un `catch` normal:

```csharp
// Con await la excepción llega tal cual y se atrapa con un catch normal.
public static async Task<string> ConAwaitAsync()
{
    try
    {
        await CalcularAsync();
        return "sin error";
    }
    catch (InvalidOperationException ex)
    {
        return ex.Message;
    }
}
```

---

## Una tras otra o todas juntas

```csharp
public static async Task<int> UnaTrasOtraAsync(Medidor m)
{
    await m.ConsultarAsync();            // espera a que termine antes de arrancar la siguiente
    await m.ConsultarAsync();
    await m.ConsultarAsync();
    return m.Maximo;                     // 1: nunca hubo dos a la vez
}

public static async Task<int> JuntasAsync(Medidor m)
{
    await Task.WhenAll(m.ConsultarAsync(), m.ConsultarAsync(), m.ConsultarAsync());
    return m.Maximo;                     // 3: las tres esperaron al mismo tiempo
}
```

Cada consulta espera 100 ms. Una tras otra, unos 300 ms; juntas, unos 100. `Medidor` cuenta cuántas había en curso a la vez, y la prueba lo comprueba sin depender del reloj.

<!--
Nota del instructor: la trampa clásica es creer que WhenAll "paraleliza" en varios hilos. Aquí ninguna consulta ocupa un hilo mientras espera; WhenAll solo deja que las tres esperas se enciman. Si las tres operaciones fueran cálculo puro de CPU, WhenAll no ayudaría.
-->

---

## .Result: el bloqueo y el envoltorio

```csharp
// .Result bloquea el hilo y envuelve la excepción en AggregateException.
public static Exception? ConResult()
{
    try { _ = P06.CalcularAsync().Result; return null; }
    catch (Exception ex) { return ex; }  // AggregateException con la original en InnerException
}
```

- **Bloquea** un hilo que podría atender otra petición.
- En aplicaciones con contexto de sincronización (interfaces de escritorio, ASP.NET clásico) puede producir un **interbloqueo**: el hilo espera a la tarea y la tarea espera a ese hilo.
- La excepción llega **envuelta** en `AggregateException`.

---

## Los errores que más se preguntan

| Error | Qué pasa | Qué hacer |
|-------|----------|-----------|
| `async void` | La excepción no se puede atrapar con `await` y puede tirar el proceso | `async Task`, salvo en manejadores de eventos |
| `.Result` o `.Wait()` | Bloquea un hilo; riesgo de interbloqueo | `await` hasta arriba de la cadena |
| Olvidar el `await` | La tarea corre sola; sus excepciones se pierden | Revisar las advertencias del compilador |
| No pasar el `CancellationToken` | La petición cancelada sigue consumiendo recursos | Recibirlo y pasarlo a cada llamada |

---

## Cancelar

```csharp
// P08. Cancelar una operación con CancellationToken.
public static class P08
{
    public static async Task EsperarAsync(TimeSpan cuanto, CancellationToken ct)
        => await Task.Delay(cuanto, ct); // si ct se cancela, lanza TaskCanceledException
}
```

En ASP.NET Core, el token de la petición se cancela cuando el cliente se desconecta. Pasarlo hasta la consulta a la base evita trabajar para nadie.

**Pregunta típica:** "¿async hace más rápido mi código?" No: una operación sola tarda lo mismo. Lo que mejora es **cuántas peticiones atiende el servidor** con los mismos hilos.

---

<!-- _class: lead -->

# Parte 2
## Inyección de dependencias

---

## Los tres tiempos de vida

```csharp
public static ServiceProvider Construir()
{
    var servicios = new ServiceCollection();
    servicios.AddSingleton<ServicioSingleton>(); // uno para toda la aplicación
    servicios.AddScoped<ServicioScoped>();       // uno por alcance (en ASP.NET Core, por petición)
    servicios.AddTransient<ServicioTransient>(); // uno nuevo cada vez que se pide
    return servicios.BuildServiceProvider();
}
```

| Se pide... | Singleton | Scoped | Transient |
|------------|-----------|--------|-----------|
| Dos veces en el mismo alcance | la misma instancia | la misma | dos distintas |
| En dos alcances distintos | la misma | distintas | distintas |

Cada servicio del ejemplo tiene un `Guid` propio; la prueba compara los `Id` para comprobar cada celda de la tabla.

---

## La dependencia cautiva

```csharp
public sealed class Repositorio { }                    // scoped: vive lo que dura la petición
public sealed class CacheGlobal(Repositorio repo)       // singleton: vive toda la aplicación
{
    public Repositorio Repo { get; } = repo;
}
```

```csharp
// El singleton capturaría para siempre un servicio que debía morir con la petición.
public static ServiceProvider ConValidacion()
{
    var servicios = new ServiceCollection();
    servicios.AddScoped<Repositorio>();
    servicios.AddSingleton<CacheGlobal>();
    return servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
}
```

Con `ValidateScopes`, pedir `CacheGlobal` lanza `InvalidOperationException`. Sin validación, el "scoped" queda vivo para siempre dentro del singleton, compartido entre peticiones: si es un `DbContext`, el error aparece en producción, con carga. ASP.NET Core activa esta validación en el entorno de desarrollo.

---

## ¿Cuál uso?

| Tiempo de vida | Para | Cuidado con |
|----------------|------|-------------|
| Singleton | Lo que no tiene estado o lo comparte a propósito: configuración, cachés, clientes HTTP con fábrica | Que sea seguro entre hilos; que no dependa de nada scoped |
| Scoped | Lo que vive una petición: `DbContext`, unidad de trabajo, el usuario actual | Usarlo fuera de una petición (un worker) sin crear un alcance |
| Transient | Objetos ligeros y sin estado | Que sea `IDisposable` y caro de crear |

**Regla de oro:** un servicio solo puede depender de otros que vivan **igual o más** que él.

<!--
Nota del instructor: pregunta cómo usaría un worker en segundo plano (que es singleton) un DbContext. La respuesta esperada es IServiceScopeFactory: crear un alcance por cada unidad de trabajo y pedir el DbContext dentro de él. Es una pregunta de seguimiento frecuente.
-->

---

<!-- _class: lead -->

# Parte 3
## Genéricos y nulabilidad

---

## Restricciones genéricas

```csharp
public static T Mayor<T>(IEnumerable<T> valores) where T : IComparable<T>
{
    using var e = valores.GetEnumerator();
    if (!e.MoveNext()) throw new InvalidOperationException("La secuencia está vacía.");
    T mayor = e.Current;
    while (e.MoveNext())
        if (e.Current.CompareTo(mayor) > 0)
            mayor = e.Current;
    return mayor;
}
```

Sin `where T : IComparable<T>`, el compilador no deja llamar a `CompareTo`: no sabe nada de `T`. La restricción es la promesa que lo permite. La misma función sirve para `int`, `string` y `DateOnly`.

---

## Varianza: in y out

```csharp
public static int Contar()
{
    IEnumerable<object> objetos = new List<string> { "a", "b" }; // compila: IEnumerable<out T>
    // List<object> lista = new List<string>();                  // no compila: List<T> es invariante
    return objetos.Count();
}
```

- **Covarianza (`out T`):** si la interfaz solo *entrega* `T`, una de `string` sirve donde se pide una de `object`.
- **`List<T>` es invariante** porque también *recibe* `T`: si se pudiera, alguien metería un `int` en una lista de `string`.
- **Contravarianza (`in T`):** `Action<object>` sirve donde se pide `Action<string>`: quien acepta cualquier cosa acepta texto.

---

## int? y string? no son lo mismo

```csharp
// P13. int? es otro tipo (Nullable<int>); string? es solo una anotación para el compilador.
public static class P13
{
    public static int Longitud(string? texto) => texto?.Length ?? 0;

    public static (bool IntEsNullable, Type TipoDelParametro) Revisar()
    {
        bool intEsNullable = typeof(int?) == typeof(Nullable<int>);
        Type parametro = typeof(P13).GetMethod(nameof(Longitud))!.GetParameters()[0].ParameterType;
        return (intEsNullable, parametro); // (true, typeof(string)): en ejecución string? es string
    }
}
```

`int?` es una estructura distinta, con `HasValue` y `Value`. `string?` es solo una anotación: en ejecución sigue siendo `string`, y el compilador la usa para advertir.

---

## El operador ! no protege de nada

```csharp
// P14. El operador ! solo calla la advertencia: no evita el NullReferenceException.
public static class P14
{
    public static int LongitudForzada(string? texto) => texto!.Length;
}
```

`LongitudForzada(null)` lanza `NullReferenceException`. El `!` le dice al compilador "confía en mí"; si te equivocas, el error llega en ejecución. **En una revisión de código, cada `!` pide una explicación.**

<!--
Nota del instructor: pregunta qué harían en vez del signo de admiración: una guarda al inicio (ArgumentNullException.ThrowIfNull), un patrón is not null, o cambiar la firma para que no acepte nulos. Cada opción comunica algo distinto a quien lee el código.
-->

---

<!-- _class: lead -->

# Parte 4
## LINQ

---

## Ejecución diferida

```csharp
public static int Contar()
{
    var numeros = new List<int> { 1, 2, 3 };
    var mayoresQueUno = numeros.Where(n => n > 1); // todavía no se ejecuta nada
    numeros.Add(4);
    return mayoresQueUno.Count();                  // se ejecuta aquí: 2, 3 y 4
}
```

Devuelve 3, no 2. `Where` solo describe la consulta; se ejecuta al recorrerla. Si la recorres dos veces, se ejecuta dos veces: materializa con `ToList()` cuando la vayas a usar más de una vez.

---

## IEnumerable o IQueryable

```csharp
public static (string Metodo, Type TipoDelFiltro) Inspeccionar()
{
    IQueryable<int> consulta = new[] { 1, 2, 3 }.AsQueryable().Where(n => n > 1);
    var llamada = (MethodCallExpression)consulta.Expression;
    var filtro = (UnaryExpression)llamada.Arguments[1]; // la lambda, citada como datos
    return (llamada.Method.Name, filtro.Operand.Type);  // ("Where", Func<int, bool>)
}
```

- **`IEnumerable<T>`** ejecuta el filtro en memoria, como código.
- **`IQueryable<T>`** guarda el filtro como **árbol de expresión**, como datos, para que un proveedor (EF Core) lo traduzca a SQL.
- **La trampa:** un `AsEnumerable()` o un `ToList()` antes del `Where` trae **toda la tabla** a memoria y filtra después.

<!--
Nota del instructor: si alguien del grupo usa EF Core, pide que active el log de SQL (LogTo o el nivel Information de Microsoft.EntityFrameworkCore.Database.Command) y compare el SQL de las dos versiones. Ver el SELECT sin WHERE convence más que la explicación.
-->

---

## First, FirstOrDefault y Single

```csharp
public static readonly int[] Datos = [5, 8, 8, 13];

public static int Primero() => Datos.First(n => n > 6);                    // 8
public static int PrimeroOCero() => Datos.FirstOrDefault(n => n > 100);    // 0: default(int)
public static int Unico() => Datos.Single(n => n > 6);                     // lanza: hay tres
```

| Método | Si no hay ninguno | Si hay varios |
|--------|-------------------|---------------|
| `First` | lanza | devuelve el primero |
| `FirstOrDefault` | devuelve `default` | devuelve el primero |
| `Single` | lanza | **lanza** |
| `SingleOrDefault` | devuelve `default` | **lanza** |

`Single` afirma que hay exactamente uno: úsalo cuando un segundo resultado sería un error de datos.

---

<!-- _class: lead -->

# Parte 5
## Lo que de verdad preguntan

---

## Valor, referencia y record

```csharp
public static (int Original, int Copia) ConStruct()
{
    var a = new PuntoValor { X = 1 };
    var b = a;        // copia el valor completo
    b.X = 99;
    return (a.X, b.X); // (1, 99)
}

public static (int Original, int Copia) ConClase()
{
    var a = new PuntoReferencia { X = 1 };
    var b = a;        // copia la referencia: los dos apuntan al mismo objeto
    b.X = 99;
    return (a.X, b.X); // (99, 99)
}
```

Un `record` es una clase (o estructura) con **igualdad por valor**: dos `Dinero("MXN", 100m)` son iguales con `==` aunque sean instancias distintas, y `with` crea una copia modificada sin tocar el original.

---

## throw y throw ex

```csharp
public static string ConThrow()
{
    try
    {
        try { Fallar(); }
        catch (InvalidOperationException) { throw; }      // conserva el rastro original
    }
    catch (InvalidOperationException ex) { return ex.StackTrace ?? ""; }
    return "";
}
```

Con `throw;`, el rastro de pila incluye el método `Fallar`, donde nació el error. Con `throw ex;`, el rastro empieza en el `catch` y `Fallar` desaparece. La prueba revisa las dos cosas en el texto del rastro.

---

## ¿Qué imprime?

```csharp
public static List<int> ConFor()
{
    var acciones = new List<Func<int>>();
    for (int i = 0; i < 3; i++)
        acciones.Add(() => i);          // las tres capturan la MISMA variable i
    return acciones.Select(a => a()).ToList(); // [3, 3, 3]
}
```

Con `for`, las tres lambdas capturan **la misma** variable, que al final vale 3. Con `foreach`, desde C# 5, cada vuelta tiene su propia variable y el resultado es `[0, 1, 2]`. Es la pregunta de "¿qué imprime?" más común en C#.

---

## Las veinte preguntas de la práctica

| | | |
|---|---|---|
| 1. Valor o referencia | 8. Cancelar | 15. Ejecución diferida |
| 2. Records | 9. Singleton, Scoped, Transient | 16. IEnumerable o IQueryable |
| 3. `==` entre strings | 10. Dependencia cautiva | 17. First o Single |
| 4. StringBuilder | 11. Restricciones genéricas | 18. `throw` o `throw ex` |
| 5. await o WhenAll | 12. Covarianza | 19. using y Dispose |
| 6. async void | 13. `int?` o `string?` | 20. Cierres en ciclos |
| 7. .Result | 14. El operador `!` | |

Cada una tiene su ejemplo en C# y una prueba xUnit que demuestra la respuesta.

---

## Si vas a frontend

Las preguntas de lenguaje cambian, pero el formato es el mismo: **definición, ejemplo que corre, cuándo importa y la trampa.**

- Ciclo de vida y renderizado, hooks y sus reglas, estado y efectos, enrutamiento.
- *React Interview Guide* (Jonna, 2023), caps. 2 a 5.

Prepara cinco respuestas con un componente que corra, igual que aquí con C#.

---

## La práctica de hoy

**Veinte preguntas, en parejas, por turnos.**

- Quien pregunta lee la pregunta de la práctica.
- Quien contesta tiene **dos minutos**: definición, ejemplo, cuándo importa y la trampa.
- Al final se lee la respuesta modelo y se corre la prueba que la demuestra.

Las preguntas que no salgan en clase quedan de estudio, con el proyecto de pruebas para correrlas.

<!--
Nota del instructor: 20 preguntas a 2 minutos más la revisión no caben en una sesión; asigna las primeras diez en clase y las otras diez como estudio en casa con el proyecto de pruebas. Pide que cada pareja escoja una pregunta que le haya costado y la explique al grupo.
-->

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas

- Una forma de contestar: definición, ejemplo, cuándo importa y la trampa
- async, inyección de dependencias, genéricos, nulabilidad y LINQ con código que corre
- Veinte respuestas preparadas, cada una con su prueba

**Libros:** *C# Interview Guide* (Semenenko, 2024), caps. 3 a 6. Para frontend: *React Interview Guide* (Jonna, 2023), caps. 2 a 5.

**Siguiente módulo:** diseño de sistemas y la oferta.
