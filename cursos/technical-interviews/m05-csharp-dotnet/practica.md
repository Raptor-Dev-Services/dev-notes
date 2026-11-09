# Práctica - Veinte preguntas con código que corre

## Objetivo

Contestar las veinte preguntas de C# y .NET que más aparecen en entrevistas, cada una con el mismo formato: **definición en una frase, un ejemplo que corre, cuándo importa y la trampa**. Al final deberías poder sostener una conversación técnica con ejemplos propios, no con definiciones memorizadas.

**Lecturas de apoyo:** [08 - async/await](../../../02-programacion/csharp/08-async.md), [09 - Lifetimes](../../../02-programacion/csharp/09-lifetimes.md), [10 - Genéricos](../../../02-programacion/csharp/10-generics.md), [12 - Colecciones y LINQ](../../../02-programacion/csharp/12-collections.md) y [13 - Nulabilidad](../../../02-programacion/csharp/13-nullability.md).

## Cómo se trabaja

- **En parejas, por turnos.** Quien pregunta lee la pregunta; quien contesta tiene dos minutos. Luego leen juntos la respuesta modelo y comparan.
- **En clase**, las preguntas 1 a 10. **En casa**, de la 11 a la 20, corriendo cada ejemplo.
- Cada ejemplo de esta práctica se compiló con .NET 10 y tiene una prueba xUnit que demuestra la respuesta. Para correrlos tú:

```bash
dotnet new xunit -n Preguntas.Tests
cd Preguntas.Tests
dotnet add package Microsoft.Extensions.DependencyInjection
# pega la clase del ejemplo en un archivo .cs, escribe la prueba que se describe y corre:
dotnet test
```

---

## 1. ¿Qué diferencia hay entre un tipo por valor y uno por referencia?

**Respuesta.** Un tipo por valor (`struct`, `int`, `DateTime`) se copia completo al asignarlo; un tipo por referencia (`class`) copia solo la referencia, y las dos variables apuntan al mismo objeto.

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

**Cuándo importa:** al pasar un objeto a un método que lo modifica. **La trampa:** decir que los structs "viven en la pila"; depende de dónde se declaren (un struct dentro de una clase vive en el heap con ella). **La prueba** afirma `(1, 99)` y `(99, 99)`.

## 2. ¿Qué es un record y cómo compara?

**Respuesta.** Un `record` es un tipo con igualdad por valor generada por el compilador: dos instancias con las mismas propiedades son iguales. `with` crea una copia modificada sin tocar el original.

```csharp
public static (bool Iguales, bool MismaInstancia, Dinero Copia) Comparar()
{
    var a = new Dinero("MXN", 100m);
    var b = new Dinero("MXN", 100m);
    var c = a with { Monto = 250m }; // a no cambia: c es otro objeto
    return (a == b, ReferenceEquals(a, b), c); // (true, false, Dinero { MXN, 250 })
}
```

**Cuándo importa:** en DTOs, respuestas y objetos de valor, donde lo que importa es el contenido. **La trampa:** creer que un record siempre es inmutable. Las propiedades posicionales de un `record class` son de solo inicialización, pero se le pueden declarar propiedades con `set`, y en un `record struct` las posicionales son mutables.

## 3. ¿`==` entre strings compara la referencia?

**Respuesta.** No. `string` sobrecarga `==` para comparar el contenido, carácter por carácter.

```csharp
public static (bool Igual, bool MismaReferencia) Comparar()
{
    string a = "hola";
    string b = new string(['h', 'o', 'l', 'a']); // otra instancia con el mismo texto
    return (a == b, ReferenceEquals(a, b)); // (true, false)
}
```

**Cuándo importa:** al comparar textos que vienen del usuario. **La trampa:** olvidar la cultura y las mayúsculas; para identificadores se usa `string.Equals(a, b, StringComparison.Ordinal)` o `OrdinalIgnoreCase`.

## 4. ¿Por qué string es inmutable y cuándo usar StringBuilder?

**Respuesta.** Cada operación sobre un `string` crea uno nuevo. Concatenar en un ciclo copia todo lo anterior en cada vuelta; `StringBuilder` escribe en un búfer.

```csharp
// O(n²) caracteres copiados: cada += crea un string nuevo con todo lo anterior.
public static string UnirConcatenando(int n)
{
    string s = "";
    for (int i = 0; i < n; i++)
        s += i % 10;
    return s;
}

// O(n): con la capacidad reservada desde el inicio, cada Append escribe al final sin copiar lo anterior.
public static string UnirConBuilder(int n)
{
    var sb = new StringBuilder(n);
    for (int i = 0; i < n; i++)
        sb.Append(i % 10);
    return sb.ToString();
}
```

**Cuándo importa:** al armar textos grandes en ciclos (reportes, CSV). **La trampa:** usar `StringBuilder` para unir tres textos; ahí la interpolación es más clara y la diferencia de costo no importa.

## 5. ¿await secuencial o Task.WhenAll?

**Respuesta.** Con `await` uno tras otro, cada operación empieza cuando termina la anterior. Con `Task.WhenAll`, todas empiezan y se espera a que terminen todas.

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

**Cuándo importa:** cuando una pantalla necesita tres consultas independientes. **La trampa:** usar `WhenAll` con operaciones que comparten un `DbContext`, que no admite dos consultas a la vez. **Las pruebas** comprueban que el máximo simultáneo es 1 y 3, y que el tiempo total es de unos 300 ms contra unos 100 ms.

## 6. ¿Qué pasa con las excepciones en código asíncrono? ¿Y con async void?

**Respuesta.** Con `await`, la excepción de la tarea llega tal cual y se atrapa con un `catch` normal.

```csharp
public static async Task<int> CalcularAsync()
{
    await Task.Yield();
    throw new InvalidOperationException("falló el cálculo");
}
```

**Cuándo importa:** siempre que un método asíncrono pueda fallar. **La trampa:** `async void`. No devuelve una `Task`, así que nadie puede esperarlo ni atrapar su excepción, y un error puede tirar el proceso. Solo se usa en manejadores de eventos.

## 7. ¿Por qué no usar .Result?

**Respuesta.** `.Result` bloquea el hilo hasta que la tarea termina y envuelve la excepción en `AggregateException`.

```csharp
// .Result bloquea el hilo y envuelve la excepción en AggregateException.
public static Exception? ConResult()
{
    try { _ = P06.CalcularAsync().Result; return null; }
    catch (Exception ex) { return ex; }  // AggregateException con la original en InnerException
}
```

**Cuándo importa:** en un servidor, cada hilo bloqueado es una petición que no se atiende. **La trampa:** con un contexto de sincronización (escritorio, ASP.NET clásico) puede haber interbloqueo. **La prueba** afirma que la excepción es `AggregateException` y que su `InnerException` es la original.

## 8. ¿Cómo se cancela una operación?

**Respuesta.** Con un `CancellationToken` que se recibe y se pasa a cada operación que lo acepte. Al cancelarse, la operación lanza `OperationCanceledException` (o su derivada `TaskCanceledException`).

```csharp
// P08. Cancelar una operación con CancellationToken.
public static class P08
{
    public static async Task EsperarAsync(TimeSpan cuanto, CancellationToken ct)
        => await Task.Delay(cuanto, ct); // si ct se cancela, lanza TaskCanceledException
}
```

**Cuándo importa:** en ASP.NET Core el token se cancela si el cliente se desconecta. **La trampa:** recibir el token y no pasarlo a la consulta de la base. **La prueba** cancela a los 20 ms una espera de 30 s.

## 9. ¿Singleton, Scoped o Transient?

**Respuesta.** Singleton: una instancia para toda la aplicación. Scoped: una por alcance (por petición en ASP.NET Core). Transient: una nueva cada vez que se pide.

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

**Cuándo importa:** un `DbContext` es scoped; un cliente de configuración puede ser singleton. **La trampa:** un singleton con estado mutable que no es seguro entre hilos. **La prueba** crea dos alcances y compara los `Id` de cada servicio.

## 10. ¿Qué es una dependencia cautiva?

**Respuesta.** Un servicio de vida larga que recibe uno de vida corta y lo retiene más de lo debido, como un singleton que recibe un scoped.

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

**Cuándo importa:** con un `DbContext` capturado, varias peticiones comparten un objeto que no es seguro entre hilos. **La trampa:** que sin validación todo "funciona" en desarrollo. **Las pruebas** muestran las dos caras: con `ValidateScopes` lanza `InvalidOperationException`; sin validación, dos alcances distintos reciben el mismo `Repositorio`.

## 11. ¿Para qué sirven las restricciones genéricas?

**Respuesta.** Para prometerle al compilador algo sobre `T` y poder usarlo. Sin `where T : IComparable<T>`, no se puede llamar a `CompareTo`.

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

**Cuándo importa:** en repositorios genéricos, validadores y utilidades. **La trampa:** usar `object` y castear, que pierde la seguridad de tipos y, con structs, obliga a encajonar (boxing). **La prueba** la usa con `int`, `string` y `DateOnly`, y con una secuencia vacía.

## 12. ¿Qué es la covarianza?

**Respuesta.** Que una interfaz que solo *entrega* `T` (marcada `out T`) acepte un tipo más específico: un `IEnumerable<string>` sirve donde se pide un `IEnumerable<object>`. La contravarianza (`in T`) es lo contrario, para lo que solo *recibe* `T`.

```csharp
public static int Contar()
{
    IEnumerable<object> objetos = new List<string> { "a", "b" }; // compila: IEnumerable<out T>
    // List<object> lista = new List<string>();                  // no compila: List<T> es invariante
    return objetos.Count();
}
```

**Cuándo importa:** al diseñar interfaces genéricas que otros van a consumir. **La trampa:** esperar que `List<string>` se convierta en `List<object>`; no puede, porque entonces se podría meter un `int`.

## 13. ¿Qué diferencia hay entre `int?` y `string?`?

**Respuesta.** `int?` es otro tipo, `Nullable<int>`, con `HasValue` y `Value`. `string?` es una anotación: en ejecución sigue siendo `string`, y solo el compilador la usa para advertir.

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

**Cuándo importa:** al leer advertencias del compilador y al diseñar firmas. **La trampa:** creer que `string?` agrega una comprobación en ejecución; no agrega nada.

## 14. ¿Qué hace el operador `!`?

**Respuesta.** Nada en ejecución: le dice al compilador que confíe en que el valor no es nulo y calla la advertencia.

```csharp
// P14. El operador ! solo calla la advertencia: no evita el NullReferenceException.
public static class P14
{
    public static int LongitudForzada(string? texto) => texto!.Length;
}
```

**Cuándo importa:** en revisiones de código, cada `!` merece una pregunta. **La trampa:** usarlo para "arreglar" advertencias. **La prueba** llama `LongitudForzada(null)` y obtiene `NullReferenceException`.

## 15. ¿Qué es la ejecución diferida en LINQ?

**Respuesta.** Que operadores como `Where` y `Select` solo describen la consulta; se ejecuta al recorrerla (`foreach`, `Count`, `ToList`).

```csharp
public static int Contar()
{
    var numeros = new List<int> { 1, 2, 3 };
    var mayoresQueUno = numeros.Where(n => n > 1); // todavía no se ejecuta nada
    numeros.Add(4);
    return mayoresQueUno.Count();                  // se ejecuta aquí: 2, 3 y 4
}
```

**Cuándo importa:** una consulta recorrida dos veces se ejecuta dos veces; con una base de datos, son dos viajes. **La trampa:** no materializar con `ToList()` antes de recorrer varias veces.

## 16. ¿IEnumerable o IQueryable?

**Respuesta.** `IEnumerable<T>` ejecuta los operadores en memoria, como código. `IQueryable<T>` los guarda como árbol de expresión para que un proveedor, como EF Core, los traduzca a SQL.

```csharp
public static (string Metodo, Type TipoDelFiltro) Inspeccionar()
{
    IQueryable<int> consulta = new[] { 1, 2, 3 }.AsQueryable().Where(n => n > 1);
    var llamada = (MethodCallExpression)consulta.Expression;
    var filtro = (UnaryExpression)llamada.Arguments[1]; // la lambda, citada como datos
    return (llamada.Method.Name, filtro.Operand.Type);  // ("Where", Func<int, bool>)
}
```

**Cuándo importa:** en cada consulta con EF Core. **La trampa:** un `ToList()` o `AsEnumerable()` antes del `Where` trae toda la tabla a memoria. **La prueba** comprueba que el árbol guarda la llamada a `Where` con su lambda.

## 17. ¿First, FirstOrDefault o Single?

**Respuesta.** `First` devuelve el primero y lanza si no hay ninguno; `FirstOrDefault` devuelve `default` si no hay; `Single` exige exactamente uno y lanza si hay cero o varios.

```csharp
public static readonly int[] Datos = [5, 8, 8, 13];

public static int Primero() => Datos.First(n => n > 6);                    // 8
public static int PrimeroOCero() => Datos.FirstOrDefault(n => n > 100);    // 0: default(int)
public static int Unico() => Datos.Single(n => n > 6);                     // lanza: hay tres
```

**Cuándo importa:** buscar un usuario por correo debería ser `Single`: dos resultados son un error de datos. **La trampa:** `FirstOrDefault` sobre enteros devuelve 0, que puede ser un valor válido; no distingue "no hay" de "es cero".

## 18. ¿throw o throw ex?

**Respuesta.** `throw;` relanza conservando el rastro de pila original. `throw ex;` lo reinicia en esa línea y se pierde dónde nació el error.

```csharp
public static string ConThrowEx()
{
    try
    {
        try { Fallar(); }
        catch (InvalidOperationException ex) { throw ex; } // reinicia el rastro aquí
    }
    catch (InvalidOperationException ex) { return ex.StackTrace ?? ""; }
    return "";
}
```

**Cuándo importa:** al leer un log de producción. **La trampa:** atrapar solo para registrar y relanzar con `throw ex`. **La prueba** busca el nombre del método `Fallar` en el rastro: está con `throw;` y no está con `throw ex;`.

## 19. ¿Qué garantiza using?

**Respuesta.** Que se llame a `Dispose` al salir del bloque, también si hay una excepción. Es un `try/finally` escrito por el compilador.

```csharp
public static void UsarYFallar(Conexion conexion)
{
    using (conexion)
    {
        throw new InvalidOperationException("algo salió mal");
    }
}
```

**Cuándo importa:** con conexiones, archivos y cualquier recurso que no administra el recolector de basura. **La trampa:** creer que el recolector de basura cierra la conexión a tiempo; puede tardar mucho, y mientras tanto el pool se agota. **La prueba** comprueba que la conexión quedó cerrada aunque el bloque lanzó.

## 20. ¿Qué imprime este ciclo con lambdas?

**Respuesta.** Una lambda captura la **variable**, no su valor. Con `for`, las tres lambdas comparten la misma `i`, que al final vale 3.

```csharp
public static List<int> ConForeach()
{
    var acciones = new List<Func<int>>();
    foreach (int i in new[] { 0, 1, 2 })
        acciones.Add(() => i);          // desde C# 5 cada vuelta tiene su propia i
    return acciones.Select(a => a()).ToList(); // [0, 1, 2]
}
```

**Cuándo importa:** al crear tareas o manejadores dentro de un ciclo. **La trampa:** la versión con `for` devuelve `[3, 3, 3]`; la solución es copiar `i` a una variable local dentro del ciclo. **La prueba** comprueba las dos versiones.

---

## Checklist de la práctica

- [ ] Contesté las preguntas 1 a 10 en voz alta, en menos de dos minutos cada una.
- [ ] En cada respuesta di la definición, un ejemplo, cuándo importa y la trampa.
- [ ] Corrí en mi máquina al menos cinco ejemplos con su prueba.
- [ ] Estudié las preguntas 11 a 20 con su ejemplo.
- [ ] Puedo explicar la dependencia cautiva con un `DbContext` como ejemplo.
- [ ] Anoté las tres preguntas que más me costaron para repasarlas antes de una entrevista.
