# 48 - Diagnóstico en producción: contadores, volcados y trazas

En producción no hay depurador: hay contadores, volcados y trazas. Esta nota es el procedimiento para una API en .NET que crece en memoria o que está lenta, **sin pararla**. Usa las herramientas oficiales de .NET (`dotnet-counters`, `dotnet-dump`, `dotnet-trace`) y se apoya en las notas [20 - Memoria y GC](20-memoria-gc.md) y [47 - Benchmarking](47-benchmarking.md).

> Lo medido aquí viene del código de [`cursos/dotnet-performance/referencia/`](../cursos/dotnet-performance/referencia/) y de las salidas de [`referencia/mediciones/`](../cursos/dotnet-performance/referencia/mediciones/): .NET 10 (SDK 10.0.403, runtime 10.0.14), las tres herramientas en la versión 10.0.750501, dentro de un contenedor de Linux, en un servidor compartido y con carga, a noviembre de 2026. Todo contra la **API de ejemplo local**, nunca contra un servicio real.

---

## Qué herramienta contesta qué

| Pregunta | Herramienta | Qué entrega |
|---|---|---|
| ¿Qué pasa ahora con la memoria, el GC y el pool de hilos? | `dotnet-counters` | Valores periódicos de métricas (en pantalla o en csv/json) |
| ¿Qué objetos ocupan la memoria y quién los retiene? | `dotnet-dump` | Un volcado y comandos para recorrer el heap |
| ¿En qué funciones se va el tiempo? | `dotnet-trace` | Un archivo `.nettrace` con muestras |
| ¿Por dónde pasó una petición entre varios servicios? | Trazas distribuidas | Ver [`10-cloud/17-tracing-distribuido.md`](../10-cloud/17-tracing-distribuido.md) |

Las tres herramientas se conectan al proceso por el puerto de diagnóstico del runtime. En Linux, la documentación pide que la herramienta y la aplicación corran **como el mismo usuario** (o la herramienta como root) y compartan la variable `TMPDIR`; si no, el comando se agota.

---

## 1. Contadores: ¿sube la memoria y no baja?

```bash
dotnet tool install --global dotnet-counters
dotnet-counters ps                                     # procesos .NET que puede ver
dotnet-counters collect -p <PID> --refresh-interval 5 --format csv -o contadores.csv \
  --counters "System.Runtime[dotnet.gc.last_collection.heap.size,dotnet.process.memory.working_set],ApiEjemplo"
```

Fragmento: la sintaxis es la de la documentación de `dotnet-counters`. `System.Runtime` es el medidor integrado del runtime; `ApiEjemplo` es un `Meter` propio de la API de ejemplo. En la corrida del curso, con 4 clientes cargando la API con la fuga sembrada (una caché sin límite), el heap de la última recolección pasó de 51.8 MB a 221.6 MB en unos 40 segundos y **no bajó** cuando la carga terminó (`resumen-D-contadores.txt`). Con la caché acotada, el heap se mantuvo en unos 3 MB.

Dos cuidados al leer:

- **El heap y la memoria residente no son lo mismo.** En la misma corrida, el working set llegó a 330 MB y luego bajó a unos 209 MB mientras el heap seguía en 221.6 MB. Mira las dos series.
- **`dotnet.gc.last_collection.heap.size` es el tamaño del heap en la última recolección** (así lo dice el nombre), no el de este instante: puede ir atrás de lo que pasa.

Una nota práctica medida: lanzado en segundo plano desde un script, `dotnet-counters collect` ignoró `SIGINT`; se detiene con `SIGTERM` (la documentación dice "Ctrl+C o SIGTERM").

---

## 2. Volcado: ¿quién retiene los objetos?

```bash
dotnet tool install --global dotnet-dump
dotnet-dump collect -p <PID> --type Full -o fuga.dmp
dotnet-dump analyze fuga.dmp -c "dumpheap -stat" -c "exit"
```

`dumpheap -stat` resume el heap por tipo. En el volcado de la fuga, `System.Byte[]` tenía 11 001 objetos y 205 757 768 bytes de un total de 243 393 926 bytes en 138 845 objetos: el tipo que más pesaba. Con una dirección de uno de esos arreglos, `gcroot` muestra la cadena de referencias que lo mantiene vivo; en la corrida, terminaba en un `ConcurrentDictionary<Int32, Byte[]>` capturado por la función del endpoint (`tools-D3.log`): la caché sin límite.

Advertencias de la documentación y del propio volcado:

- **Un volcado completo contiene la memoria del proceso**: secretos, tokens y datos personales. Trátalo como dato sensible, no lo subas a un ticket y bórralo al terminar. `--type Triage` genera uno pequeño sin datos personales, según la documentación.
- Con un límite de memoria en el contenedor, la documentación advierte que un volcado `Full` o `Heap` puede hacer que el sistema mate el contenedor: prueba el límite antes.
- En contenedores, `dotnet-dump` necesita `ptrace` (la documentación sugiere `--cap-add=SYS_PTRACE`).
- El volcado de la corrida pesó 553 492 480 bytes para un proceso con unos 220 MB de heap.

---

## 3. Traza: ¿en qué funciones se va el tiempo?

```bash
dotnet tool install --global dotnet-trace
dotnet-trace collect -p <PID> --duration 00:00:12 -o cpu.nettrace
dotnet-trace report cpu.nettrace topN -n 12
```

La salida lista funciones por tiempo exclusivo e inclusivo. En la corrida del curso (el endpoint con concatenación `+=`), lo más alto eran **esperas de hilos ociosos** (`Monitor.Wait`, `LowLevelLifoSemaphore`), y más abajo aparecían `String.Concat`, `Buffer.MemmoveInternal` y la función del propio endpoint: hay que saltarse las esperas y buscar los frames de tu código (`tools-D3.log`). Un `topN` es un punto de partida: da funciones, no la historia de una petición.

---

## El procedimiento

1. **Mide primero** (contadores): ¿qué crece, qué se dispara?
2. **Reproduce en pequeño** con carga local contra un entorno de prueba, no contra producción.
3. **Toma un volcado** cuando la memoria ya creció, `dumpheap -stat` y `gcroot`.
4. **Corrige** y **vuelve a medir** con las mismas condiciones: la prueba del arreglo es que la curva cambie (en la corrida, el heap quedó plano).
5. **Borra los volcados.**

---

## Checklist

- [ ] ¿Las herramientas corren con el mismo usuario que la API?
- [ ] ¿Compararon heap y working set, y no uno solo?
- [ ] ¿El volcado se trató como dato sensible y se borró?
- [ ] ¿El arreglo se comprobó con una nueva medición?

---

## Referencias

- Microsoft Learn, *dotnet-counters*: https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-counters
- Microsoft Learn, *dotnet-dump*: https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-dump
- Microsoft Learn, *Creating metrics* (instrumentación, a noviembre de 2026): https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation
- Microsoft Learn, *dotnet-trace*: https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace
- *Effective .NET Memory Management* (Williams, 2024), cap. 6. *Systems Programming with C# and .NET* (Vroegop, 2024), caps. 10 y 11. *Tools and Skills for .NET 10* (Price, 2025), cap. 5.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Contador / métrica | valor numérico que el runtime o la aplicación publica periódicamente |
| Meter | API de .NET para publicar métricas propias que `dotnet-counters` puede leer. Un `Counter<T>` solo aumenta; un `ObservableGauge<T>` se lee con un callback en cada ciclo de recolección |
| Volcado (dump) | copia de la memoria de un proceso en un momento dado |
| `dumpheap -stat` | comando de SOS que resume el heap por tipo, con conteo y tamaño |
| `gcroot` | comando de SOS que muestra la cadena de referencias que mantiene vivo a un objeto |
| Raíz (GC root) | referencia desde la que el recolector considera vivo un objeto: variables estáticas, pila, handles |
| Working set | memoria residente del proceso según el sistema operativo; no es igual al heap administrado |
| `.nettrace` | archivo de traza que produce `dotnet-trace` |

---

*Rogelio Arriaga Gonzalez*
