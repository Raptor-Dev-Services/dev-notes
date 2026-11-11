# Práctica - M4 - Concurrencia y async

**.NET de alto rendimiento**
**Duración:** 45 a 60 minutos, individual, en tu máquina (o en un contenedor `mcr.microsoft.com/dotnet/sdk:10.0`).

---

## Objetivo

Levantar la API de ejemplo con un servidor "chico" (2 procesadores), medirla bajo carga con un endpoint asíncrono y con uno bloqueante, **ver cómo el bloqueante se rompe**, leer el estado del pool de hilos y corregirlo.

> **Advertencia.** La carga se corre **solo contra la API de ejemplo en tu máquina** (`127.0.0.1`). Nunca contra un servicio real, ni de tu trabajo ni de nadie.

Los tiempos que verás **serán distintos** a los de esta guía. Lo que debe coincidir es el comportamiento: el bloqueante no completa sus peticiones; el asíncrono sí.

Código: [`referencia/ApiEjemplo`](../referencia/ApiEjemplo/Program.cs) y [`referencia/Carga`](../referencia/Carga/Program.cs).

---

## Antes de empezar

```bash
cd cursos/dotnet-performance/referencia
dotnet --version        # esta guía se midió con 10.0.403
dotnet build -c Release ApiEjemplo
dotnet build -c Release Carga
```

Si usas contenedor (uno a la vez, y bórralo al terminar):

```bash
docker run --rm -it --cpus 4 -v "$PWD":/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 bash
```

Las salidas de esta guía se midieron con: .NET 10 (SDK 10.0.403, runtime 10.0.14), i5-11500, contenedor con 4 CPU, API y cliente **en el mismo contenedor**, servidor compartido y con carga.

Necesitas dos terminales: una para la API y otra para el cliente.

---

## Paso 1 - Arrancar la API con 2 procesadores

Terminal 1:

```bash
DOTNET_PROCESSOR_COUNT=2 ASPNETCORE_URLS=http://127.0.0.1:5080 \
  dotnet ApiEjemplo/bin/Release/net10.0/ApiEjemplo.dll
```

Terminal 2:

```bash
curl -s http://127.0.0.1:5080/ping ; echo
curl -s http://127.0.0.1:5080/diag ; echo
```

**Salida esperada** de `/diag` (medida; tus valores de heap y recolecciones pueden variar):

```
{"procesadores":2,"hilosDelPool":3,"minimoDeHilos":2,"trabajosPendientes":0,"heapMB":1,"gc0":0,"gc1":0,"gc2":0,"entradasEnCache":0}
```

Anota `procesadores`, `minimoDeHilos` e `hilosDelPool`. `DOTNET_PROCESSOR_COUNT=2` hace que el proceso crea que tiene 2 procesadores.

---

## Paso 2 - Calentar y medir el endpoint asíncrono

El primer arranque es lento (arranque en frío): por eso se **calienta** antes de medir. En una medición sin calentamiento, las primeras peticiones tardaron hasta 12 s.

```bash
# calentamiento: 2 clientes, 3 segundos (no se anota)
dotnet Carga/bin/Release/net10.0/Carga.dll "http://127.0.0.1:5080/espera-async?ms=50&n={n}" 2 3
# medición: 100 clientes simultáneos, 15 segundos
dotnet Carga/bin/Release/net10.0/Carga.dll "http://127.0.0.1:5080/espera-async?ms=50&n={n}" 100 15
curl -s http://127.0.0.1:5080/diag ; echo
```

**Salida esperada** (de la corrida 2 medida; la 1 dio 5 909 peticiones y 394 por segundo: el ruido es grande):

```
peticiones: 18169  errores: 0  concurrencia: 100  duracion: 15 s
peticiones/s: 1211
latencia ms  p50: 56.9  p95: 80.3  p99: 857.1  max: 6607.0
{"procesadores":2,"hilosDelPool":6,"minimoDeHilos":2,"trabajosPendientes":0,...}
```

Anota: peticiones, errores, p50 y `hilosDelPool`. Lo que debe coincidir: **cero errores**, p50 cercano a 50 ms y un pool de pocos hilos (5 o 6 en las mediciones).

---

## Paso 3 - Verlo fallar: el endpoint bloqueante

Detén la API (Ctrl+C en la terminal 1) y arráncala de nuevo, para empezar con un pool limpio:

```bash
DOTNET_PROCESSOR_COUNT=2 ASPNETCORE_URLS=http://127.0.0.1:5080 \
  dotnet ApiEjemplo/bin/Release/net10.0/ApiEjemplo.dll
```

Calienta con **2 clientes** (el bloqueante se porta bien con poca carga) y mide con 100:

```bash
dotnet Carga/bin/Release/net10.0/Carga.dll "http://127.0.0.1:5080/espera-bloqueante?ms=50&n={n}" 2 3
dotnet Carga/bin/Release/net10.0/Carga.dll "http://127.0.0.1:5080/espera-bloqueante?ms=50&n={n}" 100 15
curl -s http://127.0.0.1:5080/diag ; echo
```

El cliente espera hasta 30 s por petición, así que esta medición **tarda cerca de 30 s en terminar**.

**Salida esperada** (medida en dos corridas con esta misma receta):

```
peticiones: 32 por segundo en el calentamiento (2 clientes)
...
peticiones: 100  errores: 100  concurrencia: 100  duracion: 15 s
peticiones/s: 7
latencia ms  p50: 30013.5  p95: 30019.2  p99: 30035.8  max: 30035.8
{"procesadores":2,"hilosDelPool":44,...}
```

Lee lo que pasó:

- Con 2 clientes, la API iba a unas 32 a 35 peticiones por segundo.
- Con 100, **las 100 peticiones dieron error**: se acabó el tiempo límite del cliente (30 s).
- `hilosDelPool` pasó de 2 a más de 40: el pool creció, pero no a tiempo.

> Cada petición bloqueante retiene un hilo del pool durante su espera. Con 2 hilos de mínimo y 100 peticiones, el resto hace cola mientras el pool agrega hilos poco a poco.

Pregunta para anotar: si la espera es de 50 ms, ¿por qué una petición tarda 30 s?

---

## Paso 4 - Corregirlo

Abre `ApiEjemplo/Program.cs` y localiza:

```csharp
app.MapGet("/espera-bloqueante", (int ms) => { Task.Delay(ms).Wait(); return Results.Ok(); });
```

Agrega a la API un tercer endpoint, **`/espera-corregida`**, que haga lo mismo sin bloquear. Pista: ya conoces la forma en `/espera-async`; lo importante es que **no hay `.Wait()` ni `.Result`** en ninguna parte del camino.

```bash
dotnet build -c Release ApiEjemplo
```

Arranca la API como en el Paso 1.

---

## Paso 5 - Verificar con la misma medición

```bash
dotnet Carga/bin/Release/net10.0/Carga.dll "http://127.0.0.1:5080/espera-corregida?ms=50&n={n}" 2 3
dotnet Carga/bin/Release/net10.0/Carga.dll "http://127.0.0.1:5080/espera-corregida?ms=50&n={n}" 100 15
curl -s http://127.0.0.1:5080/diag ; echo
```

Debe parecerse al Paso 2: **cero errores**, p50 cercano a 50 ms, pocos hilos en el pool.

Escribe una tabla con las tres mediciones (asíncrono, bloqueante, corregido): peticiones, errores, p50 y `hilosDelPool`.

---

## Paso 6 - Task.Run no es la solución (opcional)

Prueba un cuarto endpoint donde, en vez de arreglarlo, envuelves el bloqueo:

```csharp
// Fragmento
app.MapGet("/espera-taskrun", async (int ms) => { await Task.Run(() => Task.Delay(ms).Wait()); return Results.Ok(); });
```

Mídelo igual. La guía de ASP.NET Core dice que `Task.Run` no evita el bloqueo del hilo; comprueba **tú** si lo que ves coincide. Anota tu medición, no tu suposición.

---

## Paso 7 - La carrera de datos

```bash
dotnet run -c Release --project Demos -- race
```

**Salida esperada** (la cifra de en medio cambia en cada corrida; lo que debe coincidir es que sea menor a 4 000 000):

```
esperado: 4,000,000
leer-sumar-escribir sin proteger:  2,548,716
Interlocked:   4,000,000
```

Corre el demo tres veces y anota la cifra de en medio. ¿Se repite?

---

## Limpieza

```bash
# Ctrl+C en la terminal de la API; si usaste contenedor, salir de él lo borra (--rm)
```

No dejes procesos vivos.

---

## Qué entregar

- La tabla de tres mediciones (Paso 5), con el comando que usaste y el hardware.
- La respuesta a la pregunta del Paso 3.
- El código de tu `/espera-corregida`.
- Las tres cifras del Paso 7.
