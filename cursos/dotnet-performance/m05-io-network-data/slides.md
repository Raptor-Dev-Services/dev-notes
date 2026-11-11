<!-- _class: lead -->

# Módulo 5 - I/O, red y base de datos

### .NET de alto rendimiento

**Rogelio Arriaga - Raptor Dev Services**

---

## Hoy

> En una API, el cuello de botella casi siempre está fuera del proceso.

- **Parte 1:** leer archivos sin retenerlo todo
- **Parte 2:** `HttpClient`: un cliente por petición contra uno reutilizado
- **Parte 3:** acceso a datos medido: índice, EF Core y Dapper
- **Parte 4:** N+1 y el tamaño de la conversación
- **Parte 5:** ¿en mi código, en la red o en la base? Un método
- **Práctica:** la misma consulta con EF Core y Dapper, con y sin índice

Lecturas: [05 - Índices](../../../05-bases-de-datos/02-indices.md), [05 - Dapper queries](../../../05-bases-de-datos/05-dapper-queries.md) y [13 - HttpClient](../../../04-backend/13-http-client.md)

Esta clase continúa [Backend con .NET](https://academy.raptorcloud.dev/dotnet-backend/): ahí se construye el acceso a datos; aquí se **mide**. No se repite cómo se configura EF Core ni el patrón repositorio.

---

## Cómo se leen las cifras de esta clase

- Entorno de **todas** las mediciones de BenchmarkDotNet: .NET 10 (SDK 10.0.403, runtime 10.0.14), BenchmarkDotNet 0.15.8, trabajo `ShortRun` (1 lanzamiento, 3 iteraciones), CPU i5-11500, contenedor con 4 CPU, en un servidor compartido y con carga.
- **Los tiempos son ruidosos.** Un tiempo se enuncia como "en esa corrida". Lo firme es la columna `Allocated`.
- **Todo es local.** Los archivos están en el disco del contenedor y la base es **SQLite en un archivo, dentro del mismo proceso**: no hay red de por medio. Eso cambia qué se puede concluir, y la clase lo marca cada vez.
- El código está en [`referencia/Bench/M5_Datos.cs`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Bench/M5_Datos.cs) y [`referencia/Demos/Program.cs`](https://github.com/Raptor-Dev-Services/dev-notes/blob/main/cursos/dotnet-performance/referencia/Demos/Program.cs).

> Nunca corras estas pruebas contra un servicio de producción ni contra uno ajeno. Todo el módulo se hace en tu máquina.

---

<!-- _class: lead -->

# Parte 1
## Archivos y streams

---

## El problema

Un proceso nocturno lee el log de un día (200 000 líneas) y cuenta cuántas hay con cierto texto. El código "obvio":

```csharp
string[] lineas = File.ReadAllLines(ruta);
```

Funciona. La pregunta es qué cuesta: `ReadAllLines` **carga todo el archivo en memoria** antes de devolver nada. Hay dos alternativas que entregan las líneas de una en una:

- `File.ReadLines(ruta)`: devuelve un `IEnumerable<string>` que lee mientras recorres.
- `StreamReader.ReadLine()` en un ciclo, con el tamaño de búfer que tú eliges.

---

## La medición

Archivo local de 200 000 líneas, ya en la caché de página del sistema (por eso no es una medición de disco). Cada benchmark solo cuenta las líneas:

```csharp {1-4|6-11|13-18}
[Benchmark(Baseline = true)]
public int ReadAllLines() => File.ReadAllLines(_ruta).Length;

// ReadLines: una línea a la vez
[Benchmark]
public int ReadLines()
{
    int n = 0;
    foreach (string _ in File.ReadLines(_ruta)) n++;
    return n;
}

[Benchmark]
public int StreamReaderBuffer64K()
{
    using var lector = new StreamReader(_ruta, Encoding.UTF8, true, 64 * 1024);
    int n = 0;
    while (lector.ReadLine() is not null) n++;
    return n;
}
```

---

## El resultado

| Método | Mean | Gen2 | Allocated |
|---|---:|---:|---:|
| `ReadAllLines` | 76.28 ms | 750 | 30.37 MB |
| `ReadLines` | 18.63 ms | - | 24.85 MB |
| `StreamReader` con búfer de 64 KB | 10.82 ms | 46.875 | 25.03 MB |

<!-- _pasos: filas -->

Medido en .NET 10, `ShortRun`, i5-11500, contenedor de 4 CPU, servidor con carga. Las columnas `Gen` son recolecciones por 1000 operaciones.

---

## Cómo leerlo con honestidad

- **Las asignaciones totales son casi iguales** (30.37 MB contra 24.85 y 25.03 MB): en los tres casos cada línea termina siendo un `string`. La conclusión **no** es "ReadLines no asigna".
- Lo que sí cambia es la **columna `Gen2`**: `ReadAllLines` provocó 750 recolecciones de gen 2 por 1000 operaciones y `ReadLines` ninguna, en esa corrida. Retener todas las líneas a la vez deja objetos vivos mucho tiempo, y eso lo paga el recolector.
- El **tiempo** a favor de `ReadLines` (18.63 ms contra 76.28 ms) y de `StreamReader` (10.82 ms) es de esa corrida; con `ShortRun` el error de las medias pasa de los 20 ms. El orden se sostuvo; la razón no es una constante.
- El archivo estaba en la caché de página: **no se midió el disco**. En un disco lento, el tiempo de lectura sería otro costo que este benchmark no ve.

> La regla que sale de aquí: si solo vas a **recorrer** el archivo una vez, no lo cargues entero. Si necesitas acceso por índice o varias pasadas, `ReadAllLines` puede ser lo correcto.

---

## Error típico: leer todo para procesar de a poco

```csharp
// Fragmento: carga 200 000 líneas para quedarse con unas cuantas
var errores = File.ReadAllLines(ruta).Where(l => l.Contains("ERROR")).ToList();

// Fragmento: recorre y se queda solo con lo que importa
var errores = File.ReadLines(ruta).Where(l => l.Contains("ERROR")).ToList();
```

- El segundo no retiene el archivo; retiene solo `errores`.
- Cuidado: `ReadLines` mantiene el archivo abierto mientras se enumera. Si lo enumeras dos veces, lo lees dos veces.

---

<!-- _class: lead -->

# Parte 2
## HttpClient

---

## El problema

Cada vez que tu API llama a otro servicio, alguien escribe:

```csharp
using var cliente = new HttpClient();
using var respuesta = await cliente.GetAsync(url);
```

Se ve correcto: `using`, se libera todo. La documentación oficial dice que **cada instancia de `HttpClient` usa su propio grupo de conexiones**, y que los puertos TCP no se liberan al instante al cerrar una conexión (estado `TIME-WAIT`, [RFC 9293](https://www.rfc-editor.org/rfc/rfc9293.html#section-3.3.2)). Si el ritmo de peticiones es alto, se puede agotar el límite de puertos del sistema.

Fuente: [HttpClient guidelines for .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines).

---

## La demostración

`Demos http-nuevo` y `Demos http-compartido` hacen 3 000 peticiones a `/ping` de la **API de ejemplo local** y cuentan los sockets en estado `TIME_WAIT` leyendo `/proc/net/tcp` (Linux), antes y después:

```bash {1-2|4}
dotnet run -c Release --project Demos -- http-compartido http://127.0.0.1:5080/ping 3000
dotnet run -c Release --project Demos -- http-nuevo http://127.0.0.1:5080/ping 3000

# cada demo corre en su propio proceso, uno después del otro
```

---

## El resultado

| Variante | Peticiones | `TIME_WAIT` antes | `TIME_WAIT` después |
|---|---:|---:|---:|
| Un `HttpClient` compartido | 3 000 | 228 | 209 |
| Un `HttpClient` por petición | 3 000 | 210 | 3 157 |

<!-- _pasos: filas -->

- Con un cliente por petición, el conteo subió unos 2 947 sockets (3 157 menos 210): casi uno por petición.
- Con el cliente compartido no subió.
- Medido en un contenedor de Linux contra la API de ejemplo, sobre loopback. El número exacto depende del sistema y de cuánto tarde en expirar `TIME_WAIT`.

---

## Los tiempos salieron al revés, y eso importa

En esas mismas corridas, el cliente compartido tardó **11 991 ms** y el de uno por petición **3 919 ms**. Es lo contrario de lo que cabría esperar.

- **No se concluye nada por esos tiempos.** Fueron corridas únicas, en un servidor con carga, y la API de ejemplo mostró arranque en frío lento en sus primeras peticiones (la primera petición de otra prueba tardó más de 11 segundos; no se investigó la causa).
- La conclusión firme es el **conteo de sockets**, que además coincide con la documentación oficial.
- Es el ejemplo exacto de por qué un tiempo suelto no sostiene una afirmación.

> Un cliente por petición sobre loopback casi no cuesta tiempo. El daño aparece a otra escala: muchas peticiones por segundo, hacia un servidor real, con un límite de puertos finito.

---

## Qué recomienda la documentación

Para .NET Core y .NET 5 o posterior, dos caminos:

- Un `HttpClient` **estático o singleton**, con `PooledConnectionLifetime` configurado (el ejemplo de la documentación usa 15 minutos y dice que se eligió arbitrariamente; otra sugerencia es 2 minutos, según los cambios de DNS que esperes).
- Clientes de vida corta creados por **`IHttpClientFactory`**, que agrupa los `HttpMessageHandler` y evita el agotamiento de sockets.

```csharp
// Fragmento, según la documentación oficial
var manejador = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(15)
};
var compartido = new HttpClient(manejador);
```

Por qué existe el límite de vida: `HttpClient` solo resuelve DNS **al crear la conexión**; no respeta el TTL del servidor DNS. La nota [13 - HttpClient](../../../04-backend/13-http-client.md) ya cubre la fábrica y los clientes tipados.

---

<!-- _class: lead -->

# Parte 3
## Acceso a datos medido

---

## El problema

El endpoint "pedidos de un cliente" tarda más cada mes. Alguien propone cambiar EF Core por Dapper. Otra persona, agregar un índice. ¿Quién tiene razón? **Se mide las dos cosas, con los mismos datos.**

- Base: SQLite en un archivo local, **100 000 pedidos** y 1 000 clientes (100 pedidos por cliente).
- Consulta: todos los pedidos de un cliente (100 filas).
- Variantes: Dapper, EF Core con `AsNoTracking` y EF Core con seguimiento (el valor por omisión).
- Cada operación abre su conexión o su `DbContext`, como una petición real.

---

## Lo que NO dice esta medición

- **SQLite corre dentro del proceso.** No hay red, ni servidor de base de datos, ni autenticación, ni planificador de PostgreSQL.
- Por eso **no se extrapola a PostgreSQL por red**. Las cifras de tiempo de esta parte sirven para comparar **entre sí** las variantes de este experimento, no para predecir los milisegundos de tu API.
- Lo más portátil es la columna `Allocated`: el costo en memoria del acceso a datos del lado de .NET.

---

## Primero, el índice

`WHERE cliente_id = @c` sobre 100 000 filas, con Dapper:

| Índice en `cliente_id` | Mean | Allocated |
|---|---:|---:|
| No | 8 498.0 us | 20.63 KB |
| Sí | 264.8 us | 20.63 KB |

<!-- _pasos: filas -->

- En esa corrida, con índice tardó unas 32 veces menos (8 498.0 entre 264.8; cálculo de las dos medias).
- **La memoria no cambió:** devuelve las mismas 100 filas. Un índice cambia **dónde busca la base**, no cuánto materializa .NET.
- Sin índice, la base recorre las 100 000 filas. Es el cambio de mayor efecto de todo el módulo, y no se escribe una línea de C#.

Lectura: [02 - Índices](../../../05-bases-de-datos/02-indices.md).

---

## Después, EF Core contra Dapper (con índice)

| Variante | Mean | Allocated |
|---|---:|---:|
| Dapper | 264.8 us | 20.63 KB |
| EF Core `AsNoTracking` | 551.1 us | 91.99 KB |
| EF Core con seguimiento | 1 527.7 us | 148.62 KB |

<!-- _pasos: filas -->

- **Firme:** EF Core asignó unas 4.5 veces más que Dapper con `AsNoTracking` (91.99 KB contra 20.63 KB) y unas 7.2 con seguimiento (148.62 KB).
- **Ruidoso:** los tiempos. El error de EF con seguimiento (10 018.6 us) es mayor que su media; solo el orden es de fiar, no las razones.
- Sin índice (la otra mitad de la tabla de resultados), las tres variantes cayeron a 8 a 10 ms y la diferencia entre ellas casi desapareció: **el trabajo lo hacía la base**.

---

## Por qué `AsNoTracking`

La documentación de EF Core: una consulta con seguimiento **detecta cambios y hace "identity resolution"** (si el resultado trae la misma entidad dos veces, devuelve la misma instancia); una consulta sin seguimiento "es generalmente más rápida porque no hay que preparar la información de seguimiento" y sirve para escenarios de **solo lectura**.

```csharp
// Fragmento: lectura que no va a modificar nada
var pedidos = db.Pedidos
    .AsNoTracking()
    .Where(p => p.ClienteId == 123)
    .ToList();
```

- Si vas a modificar las entidades y llamar a `SaveChanges`, necesitas el seguimiento.
- Si solo vas a devolver datos, `AsNoTracking` evita pagar por algo que no usas.

Fuente: [Tracking vs. No-Tracking Queries](https://learn.microsoft.com/en-us/ef/core/querying/tracking).

---

## Error típico: culpar a la herramienta equivocada

- Cambiar EF Core por Dapper con el índice faltante: se pasa de ~9 ms a ~8.5 ms (en esa corrida) y el problema sigue.
- Agregar el índice: de ~8.5 a ~10 ms a entre 0.26 y 1.5 ms, según la variante (en esa corrida).
- La decisión "EF Core o Dapper" se toma **después** de que la consulta ya usa índice, y con la columna `Allocated` a la vista.

> Medir antes de cambiar de tecnología es lo que ahorra el trabajo de migrarla.

---

<!-- _class: lead -->

# Parte 4
## N+1 y el tamaño de la conversación

---

## El problema

Para 100 clientes, el código pide los pedidos de cada uno:

```csharp
// Fragmento: una consulta por cliente (N+1)
foreach (long c in clientes)
    total += cn.Query<long>("SELECT id FROM pedidos WHERE cliente_id = @c", new { c }).AsList().Count;
```

La alternativa trae todo en una sola consulta:

```csharp
// Fragmento: una sola consulta
cn.Query<long>("SELECT id FROM pedidos WHERE cliente_id IN @cs", new { cs = clientes }).AsList().Count;
```

---

## La medición, y su límite

Con índice, 100 clientes, 10 000 filas devueltas en total:

| Variante | Mean | Allocated |
|---|---:|---:|
| 100 consultas | 8.861 ms | 604.99 KB |
| 1 consulta con `IN` | 6.016 ms | 537.89 KB |

<!-- _pasos: filas -->

- La diferencia es **modesta** (6.016 ms contra 8.861 ms; el error de la primera es de 12.4 ms).
- **No hay red entre el código y SQLite**, así que cada una de las 100 idas y vueltas casi no cuesta. Contra un servidor remoto, cada consulta paga además la latencia de la red: **eso no se midió aquí**, y la clase no pone cifra.
- Lo que sí enseña la tabla: el costo por consulta de la parte de .NET (preparar, ejecutar, materializar) no es cero.

---

## Qué hacer con un N+1

- Detectarlo: contar las consultas por petición (el log de EF Core o el de tu base) y ver si el número crece con los datos.
- Corregirlo: una consulta con `IN`, un `JOIN`, o `Include` en EF Core.
- Medirlo **en el entorno donde importa**: con la base en otra máquina, o con una latencia agregada a propósito. El efecto del N+1 depende de cuánto cuesta cada ida y vuelta.

---

<!-- _class: lead -->

# Parte 5
## ¿Dónde está la lentitud?

---

## Un método de tres tramos

Una petición lenta tarda `T` en total. Se parte en tramos y se mide cada uno:

```mermaid {A,B|C|D}
flowchart LR
  A[Cliente] -->|red| B[API en .NET]
  B -->|código propio| C[Cliente de base de datos]
  C -->|red| D[(Base de datos)]
```

1. **Tiempo total** de la petición, visto desde fuera.
2. **Tiempo dentro de la base:** el que reporta la propia base para la consulta (por ejemplo, su plan de ejecución o su registro de consultas lentas).
3. **Tiempo de tu código:** lo que queda al restar. Se afina con un cronómetro alrededor de cada fase o con una traza.
4. **Tiempo de red:** la diferencia entre lo que ve el cliente de base de datos y lo que reporta la base.

Si un tramo se lleva más de la mitad de `T`, ese es el que se ataca primero.

---

## Qué mide cada herramienta

| Pregunta | Herramienta | Módulo |
|---|---|---|
| ¿Cuál de dos implementaciones es mejor? | BenchmarkDotNet | 1 |
| ¿Cuánto asigna esta ruta? | `MemoryDiagnoser`, `GC.GetAllocatedBytesForCurrentThread` | 2 |
| ¿Cuántos hilos y cuántas peticiones están esperando? | `/diag`, `dotnet-counters` | 4 y 6 |
| ¿Qué tramo de la petición es el lento? | Trazas | 6 |

<!-- _pasos: filas -->

---

## Error típico

- Optimizar el tramo que **se ve** en el código (un `foreach`, un `Split`) cuando el tiempo está en la base.
- Medir en el servidor de desarrollo con 100 filas y concluir sobre producción con 10 millones.
- Concluir con un solo tiempo (como los 11 991 ms del `HttpClient` compartido): **un tiempo suelto no es una medición**.

---

## Práctica

[Práctica: la misma consulta con EF Core y Dapper, con y sin índice](practica.md)

1. Correr `ConsultaPorCliente` y anotar `Mean` y `Allocated`.
2. **Verlo fallar:** quitar el índice y ver el cambio; y un `HttpClient` por petición con el conteo de sockets.
3. Decidir con los datos: ¿índice, `AsNoTracking` o cambiar de herramienta?

Tarea: [localizar la lentitud con mediciones](tarea.md). Glosario: [términos del módulo](glosario.md).

---

## Referencias

**Documentación oficial (a noviembre de 2026)**

- [HttpClient guidelines for .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [Tracking vs. No-Tracking Queries (EF Core)](https://learn.microsoft.com/en-us/ef/core/querying/tracking)
- [RFC 9293, sección 3.3.2 (TIME-WAIT)](https://www.rfc-editor.org/rfc/rfc9293.html#section-3.3.2)
- [BenchmarkDotNet: diagnosers](https://benchmarkdotnet.org/articles/configs/diagnosers.html)

**Libros (cada quien consigue su copia)**

- *High-Performance Programming in C# and .NET* (Alls, 2022), cap. 8 (archivos y streams), cap. 9 (aplicaciones en red), caps. 10 y 11 (base de datos y benchmark de acceso a datos).
- *Systems Programming with C# and .NET* (Vroegop, 2024), cap. 5 (sistema de archivos) y cap. 8 (red).

**Notas de este repositorio:** [02 - Índices](../../../05-bases-de-datos/02-indices.md), [05 - Dapper queries](../../../05-bases-de-datos/05-dapper-queries.md), [13 - HttpClient](../../../04-backend/13-http-client.md).

*Las versiones se verificaron en .NET 10 a noviembre de 2026. High-Performance Programming es de .NET 6: cada técnica se comprobó aquí en .NET 10.*
