# 14 - Monolito modular con hosts por rol

Un solo código, cada regla de negocio en un solo módulo, y varios procesos. Es la evolución natural del [monolito modular](09-modular-monolith.md) cuando el sistema crece y aparecen partes que se operan distinto: unas se despliegan varias veces al día, otras no se pueden cortar nunca y otras no atienden HTTP.

Es el monolito modular **desacoplado en su despliegue**. El código sigue siendo uno; lo que se separa es cómo corre.

---

## Una arquitectura, dos vistas

No son dos arquitecturas. Es una sola descrita con dos vistas, como en el modelo 4+1 de Kruchten (vista de desarrollo y vista física) o en C4 (diagrama de componentes y diagrama de despliegue):

| Vista | Pregunta | Nombre |
|---|---|---|
| De código | Cómo está organizado el código y dónde vive cada regla | **Monolito modular**: contextos delimitados, y Clean o Hexagonal dentro de cada módulo |
| De despliegue | Cuántos procesos corren, dónde y cómo se actualizan | **Despliegue multiproceso por roles**: una unidad de despliegue por tipo de proceso, los *process types* de 12-Factor, detrás de un proxy |

Nombre completo: **monolito modular con despliegue por roles**. **No son microservicios**, aunque haya varios procesos: todos salen del mismo commit, cargan las mismas reglas (los mismos ensamblados), comparten la misma base y nunca se llaman por HTTP entre sí.

Combina cuatro ideas probadas:

| Pieza | Nombre formal | Qué resuelve |
|---|---|---|
| Cómo se parte | Contextos delimitados (DDD) | Cada regla vive en el módulo dueño de ese lenguaje de negocio |
| Cómo se arma cada módulo | Clean o Hexagonal | El dominio no depende de la base ni del transporte |
| Cómo se ejecuta | Hosts por rol: un *composition root* por proceso | Partes con distinta disponibilidad o ritmo se despliegan y fallan por separado |
| Cómo se migra lo viejo | Strangler Fig | Cada sistema viejo se reemplaza por módulos, con paridad, corte gradual y regreso |

Referencias con la misma forma: *majestic monolith* (Basecamp), *componentized monolith* (Shopify), Spring Modulith y, en .NET, el *Modular Monolith* de Kamil Grzybek.

---

## Cuándo elegirla

Cuando se cumplen **las tres**:

1. Un equipo, o pocos que trabajan juntos, mantiene todo el sistema.
2. Hay una base principal compartida.
3. Hay partes con necesidades de operación distintas. Por ejemplo, la oficina despliega a diario y la operación no se puede cortar ni un minuto; o hay trabajos de fondo que no deben correr dos veces.

| Alternativa | Por qué pierde con esas tres condiciones |
|---|---|
| Microservicios | Se paga red, versionado de APIs, consistencia eventual y N pipelines sin tener el problema que resuelven, que son equipos independientes. El riesgo real es un **monolito distribuido**: servicios que se llaman por HTTP y caen juntos |
| Varios monolitos, uno por área | Las reglas que cruzan áreas se copian y se desfasan. Un cambio de negocio exige N repos, N commits y despliegues coordinados a mano |
| Un monolito en un solo proceso | Cada despliegue de una parte reinicia todas, y una falla de una tumba a las demás |
| Un host por módulo | Son microservicios con otro nombre |

**No la elijas** si hay equipos que de verdad necesitan desplegar sin coordinarse, o una parte que escala órdenes de magnitud distinto. Aun así conviene empezar aquí: un módulo con fronteras limpias se extrae a servicio cuando hace falta, y lo contrario, juntar microservicios mal partidos, cuesta mucho más.

**Los hosts se definen por operación, nunca por dominio.** La operación es disponibilidad, ritmo de despliegue y exposición. Si cada host fuera un módulo, se pierde lo que hace barato al monolito.

---

## Estructura de la solución

```
Solucion.sln
|
+- Hosts/                        SOLO composicion: Program.cs, config, middleware
|  +- App.Admin.Host/            administracion, despliegue frecuente     -> imagen app-admin
|  +- App.Ops.Host/              operacion critica, no se corta            -> imagen app-ops
|  +- App.Worker.Host/           sin HTTP: tareas programadas y outbox     -> imagen app-worker
|
+- Modulos/
|  +- Ventas/
|     +- Ventas.Domain           reglas de negocio
|     +- Ventas.Application      casos de uso
|     +- Ventas.Infrastructure   datos
|     +- Ventas.Contracts        lo unico que otros modulos pueden usar
|     +- Ventas.Presentation     endpoints de administracion  -> los carga Admin.Host
|     +- Ventas.Ops              endpoints de operacion       -> los carga Ops.Host
|     +- Ventas.Worker           procesos de fondo            -> los carga Worker.Host
|     +- Ventas.Tests
|
+- Shared/Datos                  acceso a datos compartido, sin web
+- Shared/Web                    base de controllers, middleware, salud
```

- **Un host no tiene lógica propia.** Es su `Program.cs`: qué módulos registra, qué endpoints expone y qué hubs abre.
- **Un módulo que sirve a dos audiencias separa solo su presentación.** Su dominio, sus casos de uso y sus datos son uno solo.
- **Un módulo puede cargarse en varios hosts.** Así se comparte una regla: cargando el mismo ensamblado, nunca copiando código.
- **No se separan los módulos por host.** Un módulo es un área del negocio y casi siempre sirve a más de un host.

```csharp
// Program.cs del host de operacion: solo compone
builder.Services
    .AddSharedInfrastructure(builder.Configuration)
    .AddVentasModule()          // Domain + Application + Infrastructure
    .AddVentasOpsEndpoints()    // sus controllers de operacion
    .AddInventarioModule();     // la regla que la operacion necesita, sin endpoints de admin
```

### Dónde va cada cosa

| Si es... | Va en |
|---|---|
| Una regla o un caso de uso de un área | El módulo de esa área (`Domain`, `Application`) |
| Un endpoint para pantallas de oficina | `Modulo.Presentation` |
| Un endpoint para estaciones de operación | `Modulo.Ops` |
| Algo que corre solo, sin que nadie lo pida | `Modulo.Worker` |
| Control de acceso de las peticiones | El módulo de identidad, en su `Presentation` |
| Pieza técnica sin reglas: salud de la base, middleware | `Shared/Web` |
| Configuración y composición | El `Program.cs` del host |

**La pregunta que decide si algo va al Worker:** ¿pasaría aunque nadie tuviera la aplicación abierta? Si sí, es trabajo de fondo.

---

## Reglas de comunicación

```
          MISMO HOST                               ENTRE HOSTS
modulo A --Contracts (en memoria)--> modulo B    A escribe SU tabla --> B la lee
                                                 A guarda evento en outbox --> el Worker lo despacha
                 NUNCA:  host --HTTP sincrono--> host
```

1. **Dentro de un host:** por `Contracts`, en memoria. Nunca se referencia el `Domain` ni la `Infrastructure` de otro módulo.
2. **Entre hosts, por datos:** cada tabla tiene **un módulo dueño**, el único que escribe. Los demás leen por las consultas que el dueño publica en sus contratos.
3. **Entre hosts, por eventos:** el módulo guarda el evento en una tabla de outbox **en la misma transacción** que su cambio, y el Worker lo despacha.
4. **Prohibido el HTTP síncrono entre hosts.** Si un host necesita algo de otro, carga ese módulo o lee su tabla. Una llamada HTTP entre hosts es el primer paso a un monolito distribuido.
5. **Tiempo real:** los hubs viven en el host al que se conecta el navegador. Si el aviso nace en el Worker, lo más simple es un puente (Redis para SignalR). El outbox se reserva para eventos que otro módulo tiene que procesar sin falta.

---

## Mover procesos al Worker sin huecos ni duplicados

Es lo más delicado: un hueco deja de enviar y un traslape envía dos veces.

1. Los procesos se mudan a `Modulo.Worker` **sin cambiar su lógica**, y se dan de alta con **interruptores**: uno general y uno por proceso.
2. **Valores por omisión opuestos:** en el host viejo, encendidos; en el Worker, apagados. Desplegar no cambia nada.
3. El interruptor es una variable del despliegue, no un valor en la imagen: el corte no requiere compilar.
4. Cada proceso **reclama su trabajo de forma atómica**, con un `UPDATE` que solo devuelve la fila a quien la marcó. Así los dos hosts pueden coincidir unos segundos sin enviar doble.
5. El health del Worker dice qué procesos tiene encendidos.
6. El orden: Worker desplegado apagado; corte fuera del horario de las tareas, encendiendo en el Worker y apagando en el viejo; regresar es lo mismo al revés.

---

## Despliegue en contenedores

- **Una imagen por host, construidas del mismo commit.** El dominio nunca se mezcla entre versiones.
- **Un pipeline y un tag con prefijo por host**, como `admin-v1.4.0` o `ops-v1.4.0`.
- **El host crítico con dos réplicas** detrás del proxy: se actualiza una, se comprueba su health y luego la otra.
- **El Worker con una réplica**, o con candado por tarea si algún día son dos.
- **La versión que reporta cada host sale del ensamblado**, no de una variable de entorno.
- **Docker Compose alcanza en un servidor.** Un orquestador solo cuando haya varios; la arquitectura no cambia.
- **Cambios de esquema en dos tiempos (expand/contract):** dos hosts pueden correr versiones distintas unos minutos. Primero se agrega de forma compatible; lo que se quita va después de desplegar todos los hosts.

---

## Migrar lo viejo por estrangulamiento

1. **Inventario:** qué es cada sistema, qué reglas trae, a qué módulo pertenece y en qué host corre.
2. **Congela las copias.** Manda el sistema que está vivo en producción.
3. **Porta 1:1 con sus pruebas** a un módulo, y mide contra producción antes de creerle al código.
4. **Paridad:** las dos versiones contra la misma base, comparando respuestas.
5. **Corte gradual** con regreso inmediato.
6. **Retira** el sistema viejo y su despliegue.

El orden se decide por dolor y riesgo, no por tamaño: primero lo que hoy obliga a coordinar más repos por cada cambio.

---

## Antipatrones

| Antipatrón | Síntoma | Arreglo |
|---|---|---|
| Host con lógica | Reglas en `Program.cs` o en controllers del host | Moverlas al módulo dueño |
| Regla copiada | El mismo cálculo en dos módulos, desfasado | Un dueño; el otro usa sus `Contracts` |
| HTTP entre hosts | Un host llama a la API de otro | Cargar el módulo o leer la tabla del dueño |
| Dos dueños de una tabla | Dos módulos escriben la misma tabla | Uno escribe; el otro pide por contrato |
| Hosts por dominio | Un host por módulo | Agrupar por operación |
| Tareas en el host de API | La tarea corre una vez por réplica | Moverla al Worker con interruptores |
| Host gordo | Acceso, salud y despachadores dentro del host | Tabla "Dónde va cada cosa" |

---

## Checklist para adoptarla

- [ ] ADR con las alternativas descartadas y por qué.
- [ ] Mapa de módulos (de qué se encarga cada uno y de qué no) y mapa de hosts (qué carga cada uno y por qué existe).
- [ ] Inventario de dueños de tabla.
- [ ] Pruebas de arquitectura de módulos y de hosts en el pipeline.
- [ ] Una imagen, un pipeline y un tag con prefijo por host.
- [ ] Host crítico con dos réplicas y health que reporta la versión.
- [ ] Outbox y Worker antes del primer evento entre hosts.
- [ ] Diagrama de código y diagrama de despliegue al día.

Ver también: [Monolito modular desacoplado por dominio](15-monolito-modular-por-dominio.md) y [C4 Model](12-c4-model.md).

---

## Glosario

| Término | Definición |
|---|---|
| Host por rol | Proceso que carga los módulos que necesita según cómo se opera: administración, operación crítica, trabajos de fondo |
| Composition root | Punto único donde se registran las dependencias de un proceso; aquí, el `Program.cs` de cada host |
| Monolito distribuido | Servicios separados que se llaman por HTTP de forma síncrona y caen juntos: lo peor de los dos mundos |
| Outbox | Tabla donde un módulo guarda sus eventos en la misma transacción que su cambio, para despacharlos después |
| Expand/contract | Cambiar el esquema en dos tiempos: primero se agrega lo compatible y después se quita lo viejo |
| Interruptor | Variable de despliegue que enciende o apaga un proceso de fondo sin compilar |
| Strangler Fig | Reemplazar un sistema viejo pieza por pieza, con paridad y regreso, hasta retirarlo |

---

*Rogelio Arriaga Gonzalez*
