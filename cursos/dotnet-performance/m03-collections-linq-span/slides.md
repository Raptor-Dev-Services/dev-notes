<!-- _class: lead -->

# Módulo 3 - Colecciones, LINQ y Span

### .NET de alto rendimiento

**Rogelio Arriaga - Raptor Dev Services**

---

## Hoy

> El LINQ más legible no siempre es el más barato, y a veces sí.

- **Parte 1:** elegir una colección con datos, no por costumbre
- **Parte 2:** el costo de LINQ, y cuándo no importa
- **Parte 3:** `Span<T>` y `ReadOnlySpan<T>`: leer sin copiar
- **Parte 4:** `ArrayPool<T>`: reutilizar búferes
- **Parte 5:** tipos por valor, `struct` y boxing
- **Práctica:** reescribir un procesamiento de líneas con `Span<T>` y medir

Lecturas: [19 - Performance](../../../04-backend/19-performance.md) y [20 - Memoria y GC](../../../04-backend/20-memoria-gc.md)

Esta clase continúa el módulo 1 (medir) y el 2 (asignaciones y GC). Todo lo que aquí se afirma de rendimiento sale de un benchmark del [código de referencia](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Bench/M3_Colecciones.cs) o de la documentación oficial de Microsoft.

---

## Cómo se leen las cifras de esta clase

- Entorno de **todas** las mediciones: .NET 10 (SDK 10.0.403, runtime 10.0.14), BenchmarkDotNet 0.15.8, trabajo `ShortRun` (1 lanzamiento, 3 iteraciones), CPU i5-11500, contenedor con 4 CPU, en un servidor compartido y con carga.
- **Los tiempos son ruidosos.** Con tres iteraciones, la columna `Error` a veces es del tamaño de la media. Un tiempo se enuncia como "en esa corrida".
- **Las asignaciones no dependen de la carga.** La columna `Allocated` sale de `GC.GetAllocatedBytesForCurrentThread` ([BenchmarkDotNet, diagnosers](https://benchmarkdotnet.org/articles/configs/diagnosers.html)): las conclusiones firmes se apoyan en ella.
- Las columnas `Gen0`, `Gen1` y `Gen2` son colecciones por cada 1000 operaciones.
- Cada benchmark verifica en su `[GlobalSetup]` que las implementaciones comparadas devuelvan lo mismo.

> Si repites esto en tu máquina, los tiempos cambiarán. Los bytes asignados deben coincidir.

---

<!-- _class: lead -->

# Parte 1
## Elegir una colección

---

## El problema

Una API recibe una lista de identificadores y debe decir cuáles existen en un conjunto conocido. El código "obvio":

```csharp
if (_lista.Contains(id)) { ... }
```

¿Es un problema? Depende de **cuántos elementos tiene `_lista`** y de **cuántas veces** se pregunta. La pregunta correcta no es "¿cuál es mejor?" sino "¿a partir de qué tamaño deja de ser aceptable, en mi caso?".

- `List<T>.Contains` recorre elementos uno por uno: su costo crece con N.
- `HashSet<T>.Contains` calcula un hash y va directo al lugar: su costo casi no depende de N.
- `Array.BinarySearch` necesita el arreglo **ordenado** y descarta la mitad en cada paso.

Eso es lo que dice la teoría (nota Big O en *Effective .NET Memory Management*, cap. 3). Veamos qué dicen los números.

---

## La medición: `Busqueda`

Mil consultas por operación (la mitad de los valores sí existe); mismas tres estructuras con los mismos datos.

```csharp {1-8|10-14|16-19|*}
[GlobalSetup]
public void Preparar()
{
    _ordenado = Enumerable.Range(0, N).Select(i => i * 2).ToArray();   // pares
    _lista = [.. _ordenado];
    _conjunto = [.. _ordenado];
    _consultas = ... 1_000 consultas, la mitad existe
}

[Benchmark(Baseline = true)]
public int ListContains()
{
    int hallados = 0;
    foreach (int q in _consultas) if (_lista.Contains(q)) hallados++;
    return hallados;
}

// HashSetContains y BinarySearch: el mismo ciclo con
// _conjunto.Contains(q)  y  Array.BinarySearch(_ordenado, q) >= 0
```

Código completo en [`referencia/Bench/M3_Colecciones.cs`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Bench/M3_Colecciones.cs), clase `Busqueda`.

---

## Resultado: depende de N

Tiempo por operación (1 000 consultas). Entorno de la lámina 3; ninguna de las tres asignó memoria.

| N | `List.Contains` | `HashSet.Contains` | `BinarySearch` |
|---|---|---|---|
| 8 | 5.714 us (error 6.229) | 4.634 us (error 4.204) | 12.812 us |
| 100 | 8.406 us | 4.375 us | 21.310 us |
| 10 000 | 415.853 us | 6.778 us | 88.821 us |

<!-- _pasos: filas -->

---

## Cómo leer esa tabla sin engañarse

- **N = 8:** la diferencia entre `List` y `HashSet` es del tamaño del error de la corrida. **No se puede afirmar un ganador.** `BinarySearch` salió más lento que ambas, en esa corrida.
- **N = 100:** `HashSet` tardó aproximadamente la mitad que `List` en esa corrida; `BinarySearch` siguió siendo el más lento.
- **N = 10 000:** `List` es del orden de 60 veces más lenta que `HashSet` (415.9 us frente a 6.8 us) y `BinarySearch` queda en medio. La brecha es muy mayor que el error.

La lección no es "usa siempre `HashSet`". Es esta:

1. El cruce depende de N. Con N pequeño, cualquier opción sirve.
2. Elegir `HashSet` **cuesta** algo: memoria extra y que el orden no se conserva.
3. `BinarySearch` solo gana si ya tienes el arreglo ordenado y no lo desordenas.

> La decisión se toma con **el N real de tu caso**, medido, no con la intuición.

---

## Lo que esto no demuestra

- Los números son de un `int` y de `Contains`. Con tipos de referencia, `HashSet` depende de que `GetHashCode` y `Equals` estén bien implementados.
- Una colección de 8 elementos que se consulta una vez por petición no es un problema de rendimiento, aunque sea `List`.
- Una consulta a la base de datos (módulo 5) pesa miles de veces más que cualquiera de estas diferencias.

Antes de cambiar una colección: ¿esa línea está en el **camino caliente** del módulo 1? Si el perfil no la señala, déjala.

---

## Error típico: la colección "elegida por costumbre"

```csharp
// Fragmento (no se compiló aquí): el mismo conjunto, consultado miles de veces
var permitidos = repositorio.ObtenerIds();          // List<int> con 50 000 ids
foreach (var pedido in pedidos)                     // 100 000 pedidos
    if (permitidos.Contains(pedido.ClienteId)) ...  // 100 000 x recorrido lineal
```

- El costo es N consultas x M elementos: crece con **los dos** tamaños.
- La corrección es una línea (`var permitidos = repositorio.ObtenerIds().ToHashSet();`), pero solo después de medir.
- Este patrón es el hermano en memoria del "N+1" que verás en el módulo 5.

---

<!-- _class: lead -->

# Parte 2
## El costo de LINQ

---

## LINQ: qué compras y qué pagas

LINQ compra **legibilidad**: `Where(...).Select(...).Sum()` dice qué quieres. Lo que **puede** cobrar (se mide, no se supone):

- objetos intermedios (iteradores) y, a veces, delegados;
- una llamada de delegado por elemento;
- si la lambda **captura** una variable local, un objeto de cierre.

La pregunta de ingeniería es **cuánto**, y **en qué código**. Medimos tres versiones de lo mismo: sumar el doble de los pares.

```csharp {1|3-7|9-13|*}
[Benchmark(Baseline = true)]
public int Linq() => _datos.Where(x => x % 2 == 0).Select(x => x * 2).Sum();

[Benchmark]
public int LinqConCaptura()
{
    int divisor = 2;   // la lambda captura una variable local
    return _datos.Where(x => x % divisor == 0).Select(x => x * divisor).Sum();
}

[Benchmark]
public int Ciclo()
{
    int suma = 0;
    foreach (int x in _datos) if (x % 2 == 0) suma += x * 2;
    return suma;
}
```

---

## Resultado: `LinqVsCiclo`

Entorno de la lámina 3. Arreglo de `int`.

| Método | N | Tiempo | Asignado |
|---|---|---|---|
| `Linq` | 100 | 324.35 ns | 104 B |
| `LinqConCaptura` | 100 | 459.02 ns | 256 B |
| `Ciclo` | 100 | 84.83 ns | 0 B |
| `Linq` | 10 000 | 27 862 ns | 104 B |
| `LinqConCaptura` | 10 000 | 45 550 ns | 256 B |
| `Ciclo` | 10 000 | 7 906 ns | 0 B |

<!-- _pasos: filas -->

---

## Qué dicen esos números

- **Asignaciones (dato firme):** el ciclo no asignó nada. `Linq` asignó 104 B y no creció con N (104 B tanto con 100 como con 10 000 elementos). Con captura, 256 B, también constantes.
- **La captura sí cuesta bytes:** el único cambio entre `Linq` y `LinqConCaptura` es que la lambda usa `divisor`. Pasó de 104 B a 256 B.
- **Tiempo (en esa corrida):** el ciclo tardó entre 3 y 4 veces menos que `Linq` en las dos escalas. Con el error de ShortRun, léelo como "claramente menor", no como un factor exacto.

No afirmamos aquí *por qué* LINQ asigna 104 B ni cómo lo implementa .NET 10 por dentro: eso no lo medimos, y la implementación cambia entre versiones. Lo que sí tienes es el método para comprobarlo: el benchmark.

> Un ciclo de 3 a 4 veces menos tiempo, pero ¿de qué? De **27.9 microsegundos** contra **7.9 microsegundos** para 10 000 elementos.

---

## Cuándo importa y cuándo no

| Situación | Decisión |
|---|---|
| Código que corre una vez por petición sobre decenas de elementos | LINQ. La diferencia es de nanosegundos a microsegundos |
| Un endpoint que se ejecuta miles de veces por segundo y el perfil lo señala | Medir; si LINQ aparece, reescribir con ciclo |
| Un ciclo interno ejecutado millones de veces (parser, procesamiento de lotes) | Ciclo, con benchmark que lo justifique |
| Consultas a la base de datos | Otro problema: se resuelve en la consulta, no en LINQ a objetos (módulo 5) |

**Regla del curso:** la legibilidad es el valor por omisión. Se cambia por rendimiento cuando **una medición del camino caliente** lo justifica, y se deja el benchmark junto al código para que el siguiente no deshaga el cambio.

---

## Error típico: optimizar LINQ sin perfil

```csharp
// Fragmento (no se compiló aquí)
// Antes: legible
var activos = usuarios.Where(u => u.Activo).Select(u => u.Correo).ToList();
// Después de una "optimización" sin medir: más líneas, mismo costo real
var activos = new List<string>();
foreach (var u in usuarios) if (u.Activo) activos.Add(u.Correo);
```

- Si esta línea corre 20 veces por hora, ganaste microsegundos a cambio de claridad.
- El módulo 1 es la defensa: **primero el perfil**, después el cambio, después el benchmark.
- Una excepción razonable: evitar la **captura** dentro de un ciclo caliente (`LinqConCaptura` asignó más), o usar `static` en la lambda cuando no necesita capturar.

---

<!-- _class: lead -->

# Parte 3
## Span: leer sin copiar

---

## El problema: las copias escondidas

Procesar un archivo de texto con líneas `id,cliente,monto,fecha`. La versión de siempre:

```csharp {1-4|5-6|*}
foreach (string linea in _lineas)
{
    string[] campos = linea.Split(',');             // un arreglo y una string por campo
    total += int.Parse(campos[0], CultureInfo.InvariantCulture)
           + decimal.Parse(campos[2], CultureInfo.InvariantCulture);
}
```

`Split` crea un arreglo y una `string` nueva por cada campo, aunque solo uses dos. Con 1 000 líneas son miles de objetos para el GC (módulo 2).

---

## Span: una vista, no una copia

La documentación de Microsoft ([Memory-related and span types](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)) lo describe así: estos tipos permiten crear algoritmos que **evitan copiar memoria o asignar en el heap administrado**; crear un span (con `Slice`, `AsSpan()` o los constructores) no duplica el búfer, solo actualiza la referencia y el desplazamiento de la "vista".

```csharp {1|2-3|4-6|7-8|*}
ReadOnlySpan<char> resto = linea;                    // vista de la string, sin copiar
int coma = resto.IndexOf(',');
int id = int.Parse(resto[..coma], CultureInfo.InvariantCulture);   // parsea la porción
resto = resto[(coma + 1)..];                         // "avanza" sin asignar
coma = resto.IndexOf(',');
resto = resto[(coma + 1)..];                         // se salta el nombre
coma = resto.IndexOf(',');
total += id + decimal.Parse(resto[..coma], CultureInfo.InvariantCulture);
```

- `ReadOnlySpan<char>` puede estar respaldado por una `string`.
- `int.Parse` y `decimal.Parse` tienen sobrecargas que reciben spans.

---

## Resultado: `ParsearLineas`

1 000 líneas por operación. Entorno de la lámina 3. Las dos versiones devuelven el mismo total (lo comprueba el `[GlobalSetup]`).

| Método | Tiempo (media / error) | Gen0 | Asignado |
|---|---|---|---|
| `ConSplit` | 406.7 us / 890.2 us | 38.0859 | 238 968 B |
| `ConSpan` | 325.4 us / 178.2 us | - | 0 B |

<!-- _pasos: filas -->

- **Firme:** pasó de 238 968 B a **0 B**. Es el cambio medible y no depende de la carga.
- **No concluyente:** el tiempo bajó de 406.7 us a 325.4 us en esa corrida, pero el error de la primera (890.2 us) es mayor que la diferencia. No hay un "X % más rápido" que podamos afirmar con esta medición.

> Menos asignaciones significa menos trabajo futuro para el GC. En una API con muchas peticiones, eso se nota en las colecciones de Gen0 y en la latencia de cola, no necesariamente en el tiempo medio de una línea.

---

## Reglas de Span

La misma documentación explica la razón: un `Span<T>` tiene que estar en el **stack**, por lo que tiene restricciones. Dos que debes conocer:

- **No puede ser un campo de una clase.**
- **No puede usarse en operaciones asíncronas** (no sobrevive a un `await`).

Lo que sí puedes hacer:

| Necesitas | Usa |
|---|---|
| Vista temporal dentro de un método síncrono | `Span<T>` / `ReadOnlySpan<T>` |
| Guardar la vista en un campo o usarla entre `await` | `Memory<T>` / `ReadOnlyMemory<T>` (pueden vivir en el heap y no tienen esas limitaciones) |
| Una vista de solo lectura sobre una `string` | `ReadOnlySpan<char>` |

Se dice que `Span<T>` es un `ref struct`: un tipo que solo vive en el stack. Las guías completas están en [Memory\<T\> and Span\<T\> usage guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines).

---

## Error típico: Span en un método `async`

```csharp
// Fragmento (no se compiló aquí): NO funciona, por las reglas de la lámina anterior
public async Task ProcesarAsync(Stream origen)
{
    Span<byte> buffer = new byte[4096];        // un Span no puede usarse en operaciones asíncronas
    await origen.ReadAsync(buffer);            // la sobrecarga con Memory<byte> es la que corresponde
}
```

- La documentación de Microsoft dice que un span no puede usarse en operaciones asíncronas. **Pruébalo en tu máquina y lee el error del compilador**; no copiamos aquí su texto exacto porque no lo verificamos en este curso.
- La corrección: `Memory<byte> buffer = new byte[4096];` y `await origen.ReadAsync(buffer);`, o separar la parte síncrona en un método aparte que reciba un `Span<byte>`.

---

<!-- _class: lead -->

# Parte 4
## ArrayPool: reutilizar búferes

---

## El problema: arreglos grandes y temporales

Cada petición necesita un búfer de 100 000 bytes para procesar algo y lo tira. ¿Qué pasa? (Módulo 2: el umbral del LOH es de 85 000 bytes.)

```csharp {1-4|6-14|*}
[Benchmark(Baseline = true)]
public int ArregloNuevo()
{
    var b = new byte[Tamano];  b[0] = 1;  b[Tamano - 1] = 2;
    return b[0] + b[Tamano - 1];
}

[Benchmark]
public int ArrayPoolRentar()
{
    byte[] b = ArrayPool<byte>.Shared.Rent(Tamano);
    try { b[0] = 1;  b[Tamano - 1] = 2;  return b[0] + b[Tamano - 1]; }
    finally { ArrayPool<byte>.Shared.Return(b); }
}
```

---

## Resultado: `Buffers`

Entorno de la lámina 3.

| Tamaño | Método | Tiempo | Gen0 / Gen1 / Gen2 | Asignado |
|---|---|---|---|---|
| 4 096 | `ArregloNuevo` | 464.95 ns | 0.6561 / - / - | 4 120 B |
| 4 096 | `ArrayPoolRentar` | 37.63 ns | - / - / - | 0 B |
| 100 000 | `ArregloNuevo` | 19 179.58 ns | 31.2195 / 31.2195 / 31.2195 | 100 034 B |
| 100 000 | `ArrayPoolRentar` | 32.05 ns | - / - / - | 0 B |

<!-- _pasos: filas -->

- Con 100 000 bytes, `ArregloNuevo` disparó colecciones de **gen2** (31.2 por cada 1000 operaciones): el arreglo supera 85 000 bytes y va al Large Object Heap, que se recolecta junto con la generación 2 ([documentación del LOH](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap), escrita para Windows; en Linux midió igual el umbral en el módulo 2).
- Con el pool, no hubo asignaciones en el ciclo medido.

---

## Las reglas de `ArrayPool<T>` (documentación)

1. **`Rent(n)` devuelve un búfer de al menos `n`**: puede ser más largo. Lleva tú la cuenta de cuántos elementos usas; no uses `b.Length` como tamaño de tus datos.
2. **Devuélvelo siempre**, en un `finally`, para que vuelva al pool en todos los caminos.
3. **Al devolverlo pierdes la propiedad:** no lo uses después, ni lo devuelvas dos veces. La documentación de `Return` lo califica de problema de seguridad de **severidad alta** (doble liberación y uso tras liberación: corrupción de datos, fuga de datos y negación de servicio).
4. **El contenido no se limpia por omisión.** `Return(b, clearArray: true)` limpia el arreglo antes de reutilizarlo si el pool lo conserva; con `false` (el valor por omisión) el contenido queda sin cambios. Si el búfer guardó datos sensibles, usa `true`.
5. El pool es **seguro entre hilos**.

Fuente: [ArrayPool\<T\>](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1) y [ArrayPool\<T\>.Return](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1.return).

---

## Error típico: usar `Length` y no devolver

```csharp
// Fragmento (no se compiló aquí)
byte[] b = ArrayPool<byte>.Shared.Rent(1000);
int leidos = await stream.ReadAsync(b.AsMemory(0, 1000));
Procesar(b);                       // MAL: b.Length puede ser mayor que 1000 y trae basura detrás
// y nadie llama a Return: el pool deja de reutilizar ese búfer
```

Corrección:

```csharp
// Fragmento (no se compiló aquí)
byte[] b = ArrayPool<byte>.Shared.Rent(1000);
try
{
    int leidos = await stream.ReadAsync(b.AsMemory(0, 1000));
    Procesar(b.AsSpan(0, leidos));          // solo la parte válida
}
finally { ArrayPool<byte>.Shared.Return(b); }
```

Fíjate: en un método `async` se usa `Memory<byte>` (`AsMemory`) para el `await`, y `Span` solo en la parte síncrona.

---

<!-- _class: lead -->

# Parte 5
## Tipos por valor y boxing

---

## Clase o struct

Un `struct` es un tipo por valor: se guarda dentro del arreglo, del objeto o del stack que lo contiene, no como un objeto aparte. Una `class` es un tipo por referencia: cada instancia es un objeto en el heap.

```csharp
public class PuntoClase { public int X, Y; public PuntoClase(int x, int y) { X = x; Y = y; } }
public struct PuntoStruct { public int X, Y; public PuntoStruct(int x, int y) { X = x; Y = y; } }
```

Se mide: un arreglo de 10 000 puntos, llenado en un ciclo y sumado.

---

## Resultado: `BoxingYValores`

10 000 elementos por operación. Entorno de la lámina 3.

| Método | Tiempo | Asignado |
|---|---|---|
| `ListaDeObject` (boxing de cada `int`) | 172.25 us | 312.55 KB |
| `ListaDeInt` | 32.85 us | 39.12 KB |
| `ArregloDeClases` | 137.03 us | 312.52 KB |
| `ArregloDeStructs` | 23.28 us | 78.15 KB |

<!-- _pasos: filas -->

- **Boxing:** `ListaDeObject` convierte cada `int` a `object` (se aloja un objeto en el heap) y asignó **312.55 KB** contra **39.12 KB** de `ListaDeInt`: unas 8 veces más memoria para los mismos datos.
- **Struct contra clase:** 78.15 KB (aproximadamente 8 bytes por punto: dos `int`) contra 312.52 KB.
- Los tiempos de esa corrida van en el mismo sentido, con el ruido de siempre.

---

## Cuándo un `struct` y cuándo no

| Sí | No |
|---|---|
| Datos pequeños e inmutables (coordenadas, rangos, identificadores) en grandes arreglos | Tipos grandes: **se copian completos** al pasarlos como argumento o asignarlos |
| Lo vas a guardar en una colección de millones de elementos | Necesitas herencia o comportamiento polimórfico |
| El perfil muestra presión del GC por esos objetos | Los vas a convertir a `object` o a una interfaz: **boxing** (pierdes la ventaja) |

- Una colección genérica (`List<int>`) evita el boxing; una no genérica o `List<object>` lo provoca.
- Mutar un `struct` que está en una lista es una trampa clásica: la lista devuelve una **copia**.
- Un `record struct` o un `readonly struct` ayudan a que el diseño sea inmutable (lecturas de C# del curso: [Dev Notes, sesión 3](https://academy.raptorcloud.dev/dev-notes/)).

---

## Error típico: struct grande "para ahorrar memoria"

```csharp
// Fragmento (no se compiló aquí)
public struct PedidoCompleto { public int Id; public string Cliente; public decimal Total; /* ...10 campos más */ }
void Procesar(PedidoCompleto p) { ... }     // cada llamada copia todos los campos
```

- El ahorro de asignación se cambia por copias más caras al pasarlo por valor.
- La regla del curso: `struct` para valores pequeños e inmutables; ante la duda, `class` y **medir**.

---

## Elegir con datos: la tabla de decisión

| Pregunta | Qué medir | Qué mirar |
|---|---|---|
| ¿Qué colección? | Búsqueda con el N real | Tiempo; el cruce depende de N |
| ¿LINQ o ciclo? | Benchmark del método del camino caliente | `Allocated` primero, tiempo después |
| ¿`Split` o `Span`? | Benchmark con comprobación de equivalencia | `Allocated`: baja a 0 B |
| ¿`new byte[]` o `ArrayPool`? | Con el tamaño real del búfer | Gen2 si pasa de 85 000 bytes |
| ¿Clase o struct? | Arreglo del tamaño real | `Allocated` y costo de copias |

Y antes de todo: **¿esa línea está en el camino caliente?** Si el perfil no la señala, el cambio no aporta, aunque el benchmark gane.

---

## Práctica de hoy

[Práctica: reescribir el procesamiento de líneas con Span y medir](practica.md)

1. Correr el benchmark de `ParsearLineas` tal como está.
2. **Verlo fallar:** un benchmark sin comprobación de equivalencia que "gana" porque devuelve otra cosa.
3. Reescribir tu propia versión con `Span<T>` y compararla.
4. Documentar qué cifra es firme y cuál es ruido.

Tarea: [tres casos con su benchmark y su decisión](tarea.md). Glosario: [términos del módulo](glosario.md).

---

## Referencias

**Documentación oficial (a noviembre de 2026)**

- [Memory-related and span types](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)
- [Memory\<T\> and Span\<T\> usage guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines)
- [ArrayPool\<T\>](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1) y [ArrayPool\<T\>.Return](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1.return)
- [Large object heap](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap)
- [BenchmarkDotNet: diagnosers](https://benchmarkdotnet.org/articles/configs/diagnosers.html) y [good practices](https://benchmarkdotnet.org/articles/guides/good-practices.html)

**Libros (cada quien consigue su copia)**

- *High-Performance Programming in C# and .NET* (Alls, 2022), cap. 3 (tipos de datos y asignaciones), cap. 6 (colecciones), cap. 7 (LINQ).
- *Effective .NET Memory Management* (Williams, 2024), cap. 3 (asignación y estructuras de datos), cap. 5 y cap. 7 (técnicas avanzadas y bajo nivel).

**Notas de este repositorio:** [19 - Performance](../../../04-backend/19-performance.md), [20 - Memoria y GC](../../../04-backend/20-memoria-gc.md).

*Las versiones se verificaron en .NET 10 a noviembre de 2026. High-Performance Programming es de .NET 6: cada técnica se comprobó aquí en .NET 10.*
