# Glosario - M6 - Diagnóstico en producción

> Definiciones de los términos técnicos del módulo 6.
> Ordenado alfabéticamente.

---

## A

### Activity
**Categoría:** Trazas
**Definición:** En .NET, la clase `System.Diagnostics.Activity` representa una unidad de trabajo dentro de un trace. El trace es un árbol de actividades que puede cruzar procesos. El nombre común en la industria es *span*.
**En el curso:** ASP.NET y `System.Net.Http` crean y propagan actividades sin código especial.

---

## C

### collect
**Categoría:** Herramientas
**Definición:** Comando de `dotnet-counters`, `dotnet-dump` y `dotnet-trace` que recolecta datos de un proceso vivo. En `dotnet-counters` los escribe periódicamente a un archivo `csv` o `json`; en `dotnet-dump`, un volcado; en `dotnet-trace`, un archivo `.nettrace`.
**En el curso:** lanzado en segundo plano desde un script, `dotnet-counters collect` ignoró `SIGINT`; se detuvo con `kill -TERM`.

---

### Contador (Counter)
**Categoría:** Métricas
**Definición:** Instrumento de `System.Diagnostics.Metrics` (`Counter<T>`) que rastrea un valor que solo aumenta; el código reporta los incrementos con `Add` y la herramienta muestra el total o la tasa. Para valores que suben y bajan existen `UpDownCounter<T>` y `ObservableUpDownCounter<T>`.
**En el curso:** `ejemplo.peticiones` cuenta las peticiones de `/pedido/{id}`.

---

## D

### dotnet-counters
**Categoría:** Herramientas
**Definición:** Herramienta de línea de comandos para vigilar métricas de un proceso .NET sin detenerlo. Lee valores publicados con la API de `EventCounter` o la de `Meter`.
**En el curso:** se usó con `System.Runtime` y con el `Meter` propio `ApiEjemplo`.

---

### dotnet-dump
**Categoría:** Herramientas
**Definición:** Herramienta que recolecta y analiza volcados sin depurador nativo. Su comando `analyze` acepta comandos SOS.
**En el curso:** `dumpheap -stat` mostró qué tipo ocupaba la memoria y `gcroot`, quién lo retenía.

---

### dotnet-trace
**Categoría:** Herramientas
**Definición:** Herramienta que recolecta trazas de un proceso .NET en ejecución, sin perfilador nativo, sobre `EventPipe`. Su comando `report topN` resume los métodos que estuvieron más tiempo en la pila.
**En el curso:** una traza de 12 s sobre `/reporte` con la concatenación cuadrática.

---

### dumpheap
**Categoría:** SOS
**Definición:** Comando de análisis que muestra información del heap administrado. Con `-stat` agrupa los objetos por tipo (cuenta y tamaño total); con `-type` y `-min` filtra por tipo y tamaño.
**En el curso:** 11 001 `System.Byte[]` sumaban 205 757 768 bytes.

---

## E

### EventPipe
**Categoría:** Diagnóstico
**Definición:** Mecanismo del runtime de .NET que emite eventos de diagnóstico a una sesión; es la base de `dotnet-trace` y de `dotnet-counters`.
**En el curso:** por eso las herramientas se conectan a un proceso vivo sin reiniciarlo.

---

## F

### Fuga de memoria (en .NET)
**Categoría:** Memoria
**Definición:** Memoria administrada que el programa ya no necesita pero que sigue alcanzable desde una raíz, por lo que el recolector no la libera.
**En el curso:** el diccionario sin límite de `/pedido/{id}`; el heap subió a 221.6 MB y no bajó al parar la carga.

---

## G

### gcroot
**Categoría:** SOS
**Definición:** Comando de análisis que muestra la cadena de referencias desde las raíces hasta un objeto: explica por qué no se recolecta.
**En el curso:** la cadena iba del enrutamiento de ASP.NET al delegado, a su variable capturada (el diccionario) y al `Byte[]`.

---

## H

### Heap
**Categoría:** Memoria
**Definición:** Memoria administrada por el recolector, dividida en generaciones y el heap de objetos grandes.
**En el curso:** no es lo mismo que el working set.

---

## M

### Meter
**Categoría:** Métricas
**Definición:** Objeto de `System.Diagnostics.Metrics` que agrupa instrumentos con un nombre; ese nombre es el "proveedor" que se pasa a `--counters`.
**En el curso:** `new Meter("ApiEjemplo")`.

---

### Medidor observable (ObservableGauge)
**Categoría:** Métricas
**Definición:** Instrumento (`ObservableGauge<T>`) cuyo valor se obtiene con un callback que la herramienta invoca en cada ciclo de recolección, en lugar de registrarlo en cada cambio. Los callbacks de los observables corren en secuencia y en un hilo distinto del que actualiza el valor, así que hay que sincronizar. Su par que empuja el valor es `Gauge<T>` con `Record`.
**En el curso:** `ejemplo.cache.entradas` devuelve `fuga.Count` en cada ciclo de recolección.

---

## P

### PII
**Categoría:** Seguridad
**Definición:** Información personal identificable. El tipo de volcado `Triage` de `dotnet-dump` la elimina.
**En el curso:** un volcado `Full` puede contenerla porque incluye toda la memoria.

---

### ptrace
**Categoría:** Linux
**Definición:** Llamada del sistema que permite a un proceso observar a otro. `dotnet-dump` la necesita en un contenedor; se suele conceder con la capacidad `SYS_PTRACE`.
**En el curso:** dentro del mismo contenedor y con el mismo usuario, el volcado funcionó sin agregar capacidades.

---

## R

### Raíz (GC root)
**Categoría:** Memoria
**Definición:** Referencia desde la que el recolector empieza a buscar objetos vivos (variables de pila, estáticos, entre otras). Lo alcanzable desde una raíz no se libera.
**En el curso:** el enrutamiento de la aplicación fue la raíz de la fuga.

---

### Runbook
**Categoría:** Operación
**Definición:** Documento de pasos para atender una situación conocida, que otra persona pueda seguir sin conocer el sistema.
**En el curso:** la tarea del módulo.

---

## S

### Span
**Categoría:** Trazas
**Definición:** Nombre común en la industria de una unidad de trabajo de un trace; en .NET se llama `Activity`.
**En el curso:** equivale a `Activity`.

---

### SOS
**Categoría:** Diagnóstico
**Definición:** Extensión de depuración de .NET cuyos comandos (`dumpheap`, `gcroot`, `clrstack`...) se usan dentro de `dotnet-dump analyze`.
**En el curso:** se corrió con `-c "comando"`.

---

## T

### Trace distribuido
**Categoría:** Trazas
**Definición:** Árbol de actividades de una misma petición que atraviesa varios procesos, unido por un identificador (en W3C TraceContext, un `trace-id` de 16 bytes).
**En el curso:** se explica a nivel conceptual; la nota 17 de `10-cloud` lo monta.

---

### Triage (volcado)
**Categoría:** Herramientas
**Definición:** Tipo de volcado de `dotnet-dump`: pequeño, con módulos, hilos, excepciones y pilas, y sin información personal.
**En el curso:** alternativa al volcado `Full` cuando no hace falta el contenido del heap.

---

## V

### Volcado (dump)
**Categoría:** Diagnóstico
**Definición:** Archivo con la memoria de un proceso en un momento. Un volcado `Full` incluye toda la memoria y los módulos.
**En el curso:** el de la fuga pesó 553 492 480 bytes y se trató como dato sensible.

---

## W

### Working set
**Categoría:** Memoria
**Definición:** Memoria física que el sistema operativo tiene asignada al proceso. No coincide con el heap administrado.
**En el curso:** llegó a 330 MB y bajó a unos 209 MB mientras el heap seguía en 221.6 MB.
