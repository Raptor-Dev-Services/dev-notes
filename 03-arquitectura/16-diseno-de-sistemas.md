# 16 - Diseño de sistemas

En una entrevista de diseño de sistemas nadie espera que una persona junior o semi-senior diseñe la infraestructura de una empresa grande. Se espera que convierta un enunciado vago en requisitos, que haga cuentas de servilleta, que arme un diseño con piezas conocidas (API, base de datos, caché, colas) y que sepa decir qué se rompe primero y cómo lo arreglaría. Esta nota da el marco, las piezas y dos ejemplos resueltos.

> Fuente: *The Software Engineer's Guidebook* (Orosz, 2024): tercera parte, "Software Architecture", y quinta parte, "Reliable Software Systems". Para dibujar el diseño, ver [12 - C4 Model](12-c4-model.md); para el debate de monolito contra servicios, [07 - Microservicios](07-microservicios.md); para la caché, [05 - Caching](../04-backend/05-caching.md).

---

## Problema que resuelve

El enunciado de una entrevista de diseño cabe en una línea ("diseña un acortador de URL") y no tiene respuesta única. Los errores típicos son dos, y opuestos:

- **Dibujar de inmediato:** cajas y flechas sin haber preguntado cuántos usuarios hay ni qué es lo importante. El diseño resuelve un problema que nadie pidió.
- **Copiar la arquitectura de una empresa gigante:** microservicios, Kubernetes y tres bases de datos para un sistema que cabe en un servidor.

Lo que se califica es el razonamiento: qué preguntaste, qué supusiste, por qué elegiste cada pieza y qué alternativa descartaste.

---

## El marco de cinco pasos

Para una sesión de 45 minutos:

| Paso | Minutos | Qué produces |
|------|---------|--------------|
| 1. Requisitos | 5 | Lista de funciones y de atributos de calidad, con lo que queda fuera |
| 2. Estimación | 5 | Peticiones por segundo, almacenamiento y la proporción entre lecturas y escrituras |
| 3. API y datos | 10 | Los endpoints principales y las tablas con sus llaves |
| 4. Diseño de alto nivel | 10 | Un diagrama de contenedores con el camino de una petición |
| 5. Profundizar | 15 | El cuello de botella, el punto único de falla y cómo los resuelves |

Quien entrevista suele dirigir el paso 5 ("¿y si se cae la base?", "¿y si un enlace se vuelve viral?"). Deja tiempo para eso.

---

## 1. Requisitos

**Funcionales:** qué hace el sistema. Se escriben como verbos: "crear un enlace corto", "redirigir al original", "ver cuántos clics tuvo".

**No funcionales:** cómo debe comportarse. Las preguntas que los sacan:

- ¿Cuántos usuarios al día? ¿Cuántas operaciones de cada tipo?
- ¿Se lee mucho más de lo que se escribe, o al revés?
- ¿Qué tan rápido debe responder la operación más común?
- ¿Qué pasa si se pierde un dato? ¿Y si dos personas hacen lo mismo al mismo tiempo?
- ¿Cuánto tiempo se guardan los datos?

**Regla:** di en voz alta qué dejas fuera ("no voy a diseñar cuentas de usuario"). Recortar el alcance también es criterio.

---

## 2. Estimación de servilleta

No se busca precisión: se busca el orden de magnitud, para saber si el sistema cabe en un servidor o en cien.

| Dato útil | Valor |
|-----------|-------|
| Segundos en un día | 86,400, que se redondea a 10^5 para hacer cuentas de cabeza |
| Un millón de eventos al día | unos 12 por segundo en promedio |
| Pico | se supone un múltiplo del promedio (por ejemplo, 5 veces) y se dice que es supuesto |
| Kilobyte, megabyte, gigabyte, terabyte | 10^3, 10^6, 10^9 y 10^12 bytes |

La cuenta se hace en voz alta y con los supuestos escritos. Si un supuesto está mal, quien entrevista lo corrige y tú rehaces la cuenta: eso suma, no resta.

---

## 3. API y modelo de datos

- **Los endpoints principales**, con método, ruta, qué recibe y qué devuelve. No hacen falta todos: los de la operación más frecuente y la más delicada.
- **Las tablas**, con su llave primaria, las columnas que se consultan y **los índices que necesita la consulta más común**.
- **Relacional por defecto.** PostgreSQL resuelve la mayoría de los casos con transacciones, índices y restricciones. Una base no relacional se justifica con un motivo concreto (volumen de escritura enorme, esquema que cambia por registro), no por moda.

---

## 4. Diseño de alto nivel: las piezas

| Pieza | Para qué | Cuándo no hace falta |
|-------|----------|----------------------|
| Balanceador de carga | Repartir peticiones entre varias instancias de la API | Con una sola instancia |
| API sin estado | Atender peticiones; cualquier instancia puede atender cualquiera | Nunca sobra, pero el estado de sesión va fuera del proceso |
| Base de datos relacional | Fuente de verdad, transacciones, restricciones | Casi nunca sobra |
| Réplicas de lectura | Repartir lecturas cuando una sola base ya no alcanza | Mientras la base principal aguante |
| Caché (por ejemplo, Redis) | Responder lo que se lee mucho sin ir a la base | Si las lecturas son pocas o cada una es distinta |
| Cola y worker | Sacar del camino de la petición el trabajo lento o que puede esperar | Si todo se puede hacer en milisegundos dentro de la petición |
| Almacenamiento de objetos | Archivos, imágenes, respaldos | Si no hay archivos |
| CDN | Servir contenido estático cerca del usuario | Con pocos usuarios o en una sola región |

### Caché

El patrón por defecto es **cache-aside**: la API busca en la caché; si no está, lee de la base, guarda el resultado con un tiempo de vida (TTL) y lo devuelve. Las preguntas que vienen después:

- **¿Qué pasa si el dato cambia?** Se invalida la entrada al escribir, o se acepta que esté desactualizada hasta que venza el TTL.
- **¿Qué pasa si vence una entrada muy leída?** Muchas peticiones van a la base a la vez (estampida). Se mitiga con un candado por clave o con renovación anticipada.

El detalle en .NET está en [05 - Caching](../04-backend/05-caching.md).

### Colas

Una cola desacopla a quien produce un trabajo de quien lo hace. La petición del usuario solo encola y responde; un worker procesa después. Dos consecuencias:

- **Casi todas las colas entregan al menos una vez:** el mismo mensaje puede llegar dos veces (por un reintento o una caída a la mitad). El consumidor tiene que ser **idempotente**: procesar dos veces el mismo mensaje debe dejar el mismo resultado que procesarlo una.
- **El usuario no ve el resultado al instante.** Hay que decidir qué ve mientras tanto.

```csharp
// Una cola entrega "al menos una vez": el mismo mensaje puede llegar dos veces.
// El consumidor recuerda qué ids ya procesó y el segundo intento no repite el efecto.
public sealed class ConsumidorIdempotente
{
    private readonly ConcurrentDictionary<Guid, bool> _procesados = new();
    private int _clics;

    public int Clics => _clics;

    public bool Procesar(Guid idMensaje)
    {
        if (!_procesados.TryAdd(idMensaje, true)) return false; // repetido: se ignora
        Interlocked.Increment(ref _clics);
        return true;
    }
}
```

En producción el registro de mensajes procesados vive en la base, en la misma transacción que el efecto, no en memoria.

### Escalar

- **Vertical:** una máquina más grande. Es lo primero que se hace y lo más barato de operar, hasta que se acaba.
- **Horizontal:** más máquinas iguales detrás de un balanceador. Exige que la API **no guarde estado** en memoria: la sesión va en un token o en Redis.
- **La base es lo último que se escala horizontalmente.** Antes vienen índices, caché y réplicas de lectura. Particionar (sharding) es caro y se justifica con números.

### Monolito o servicios

En una entrevista, un **monolito modular** bien dividido es una respuesta defendible para casi cualquier sistema de tamaño medio. Proponer microservicios obliga a hablar de red entre servicios, consistencia eventual, despliegues independientes y observabilidad distribuida. Ver [07 - Microservicios](07-microservicios.md) y [09 - Monolito modular](09-modular-monolith.md).

---

## 5. Profundizar y cuellos de botella

Con el diseño dibujado, recórrelo buscando:

- **Punto único de falla:** si esta pieza se cae, ¿se cae todo? (una sola base, un solo worker).
- **Cuello de botella:** con el pico estimado, ¿qué pieza se satura primero?
- **Clave caliente:** un solo registro que recibe una parte enorme del tráfico (un enlace viral).
- **Concurrencia:** ¿qué pasa si dos peticiones hacen lo mismo al mismo tiempo?
- **Datos perdidos:** ¿qué se pierde si el worker muere a la mitad?

---

## Ejemplo resuelto: acortador de URL

### Requisitos

- **Funcionales:** crear un enlace corto a partir de una URL larga; redirigir del corto al largo; contar clics.
- **No funcionales:** la redirección debe ser muy rápida; un enlace no debe cambiar de destino nunca; se lee mucho más de lo que se escribe.
- **Fuera de alcance:** cuentas de usuario, enlaces personalizados, edición de enlaces.

### Estimación (todos los números son supuestos de la entrevista)

```text
Enlaces nuevos:       1,000,000 al día  ->  1,000,000 / 86,400  =  unos 12 por segundo
Lecturas:             100 por cada escritura  ->  unas 1,200 por segundo en promedio
Pico:                 5 veces el promedio  ->  unas 5,800 lecturas por segundo
Tamaño por registro:  500 bytes
Almacenamiento:       500 B x 1,000,000 x 365  =  182.5 GB al año;  unos 0.9 TB en 5 años
Enlaces en 5 años:    1,825,000,000
Caché:                20 % de los enlaces de un mes = 0.2 x 30,000,000 x 500 B = 3 GB
```

La conclusión importa más que los números: el sistema es de **lectura intensa**, los datos caben en una sola base relacional durante años, y la caché de los enlaces más leídos cabe en la memoria de un solo Redis.

### API

```http
POST /api/links
Content-Type: application/json

{ "url": "https://example.com/una/ruta/muy/larga?con=parametros" }

201 Created
{ "codigo": "aZ3k9Qx", "corto": "https://corto.example/aZ3k9Qx" }
```

```http
GET /aZ3k9Qx

302 Found
Location: https://example.com/una/ruta/muy/larga?con=parametros
```

**¿301 o 302?** Con 301 (permanente) el navegador guarda la redirección y las visitas repetidas ya no llegan al servidor: menos carga, pero se pierden esos clics en el conteo. Con 302 (temporal) cada clic pasa por el servidor y se puede contar. Como contar clics es requisito, 302.

### Generar el código corto

| Opción | A favor | En contra |
|--------|---------|-----------|
| Contador de la base convertido a base 62 | Sin colisiones, simple | Los códigos son consecutivos: se pueden adivinar y enumerar |
| Hash de la URL recortado a 7 caracteres | La misma URL da el mismo código | Puede haber colisiones; hay que detectarlas y reintentar |
| 7 caracteres al azar con índice único | No se adivinan | Hay que reintentar en la rara colisión |

Con 62 símbolos (dígitos, minúsculas y mayúsculas), 5 caracteres dan unos 9.2 × 10^8 códigos, que **no alcanzan** para 1,825 millones de enlaces; 6 dan unos 5.7 × 10^10, y 7 dan unos 3.5 × 10^12, con margen de sobra.

```csharp
// O(log n) en base 62: un carácter por cada división entre 62, escrito de derecha a izquierda.
public static string Codificar(long id)
{
    ArgumentOutOfRangeException.ThrowIfNegative(id);
    if (id == 0) return "0";
    Span<char> buffer = stackalloc char[11]; // long.MaxValue ocupa 11 caracteres en base 62
    int pos = buffer.Length;
    while (id > 0)
    {
        buffer[--pos] = Alfabeto[(int)(id % 62)];
        id /= 62;
    }
    return new string(buffer[pos..]);
}
```

### Diagrama de contenedores

```text
[Navegador]
     |  GET /aZ3k9Qx
     v
[Balanceador] ---> [API sin estado x N] ---> [Redis: codigo -> url]
                          |       (si no está)
                          |---------------------> [PostgreSQL: enlaces]
                          |
                          | encola el clic
                          v
                     [Cola] ---> [Worker de conteo] ---> [PostgreSQL: clics por día]
```

### Profundizar

- **La redirección no espera al conteo:** el clic se encola y la API responde de inmediato. El worker agrega por enlace y por día.
- **El worker es idempotente:** cada clic lleva un id, y un clic repetido no se cuenta dos veces.
- **Un enlace viral** es una clave caliente: la caché lo absorbe, y si hiciera falta se replica en la memoria de cada instancia de la API con un TTL corto.
- **Si se cae Redis**, la API lee de la base: más lenta, pero sigue funcionando. Si se cae la base, las redirecciones ya cacheadas siguen funcionando y la creación de enlaces falla.

---

## Ejemplo: sistema de reservas

El requisito que define este sistema es uno: **dos personas no pueden apartar el mismo horario del mismo recurso**. Revisar en el código ("si no está ocupado, lo guardo") tiene una carrera: las dos peticiones revisan al mismo tiempo, las dos ven libre y las dos guardan.

La solución es que **la base garantice la regla**, con una restricción única:

```sql
CREATE TABLE reservas (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    recurso_id  bigint      NOT NULL,
    inicio      timestamptz NOT NULL,
    cliente_id  bigint      NOT NULL,
    creada_en   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_reservas_recurso_inicio UNIQUE (recurso_id, inicio)
);

-- Si dos peticiones llegan a la vez, la segunda no inserta nada y no devuelve fila.
INSERT INTO reservas (recurso_id, inicio, cliente_id)
VALUES (@RecursoId, @Inicio, @ClienteId)
ON CONFLICT (recurso_id, inicio) DO NOTHING
RETURNING id;
```

Si la consulta no devuelve `id`, el horario ya estaba tomado y la API responde 409 Conflict. Los parámetros van con `@`, como los pasa Dapper con Npgsql.

Si los horarios no son bloques fijos sino rangos de duración libre, la restricción única no basta: dos rangos distintos pueden traslaparse. PostgreSQL lo resuelve con una restricción de exclusión:

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE reservas_por_rango (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    recurso_id  bigint    NOT NULL,
    horario     tstzrange NOT NULL,
    cliente_id  bigint    NOT NULL,
    CONSTRAINT ex_reservas_sin_traslape EXCLUDE USING gist (recurso_id WITH =, horario WITH &&)
);
```

El mismo principio, en memoria y en C#, para explicarlo en la pizarra: una operación atómica que solo deja ganar a uno.

```csharp
// Reservas: dos personas piden el mismo horario a la vez y solo una puede ganar.
// TryAdd es atómico, igual que un índice único en la base: el segundo insert falla.
public sealed class AgendaEnMemoria
{
    private readonly ConcurrentDictionary<(int Recurso, DateTime Inicio), string> _ocupados = new();

    public bool TryReservar(int recurso, DateTime inicio, string cliente)
        => _ocupados.TryAdd((recurso, inicio), cliente);

    public string? Quien(int recurso, DateTime inicio)
        => _ocupados.TryGetValue((recurso, inicio), out var cliente) ? cliente : null;
}
```

Para profundizar: **apartado temporal** (el horario se bloquea unos minutos mientras se paga, con una columna de vencimiento que un worker limpia), **recordatorios** (una cola con mensajes programados) y **zonas horarias** (guardar en UTC y convertir solo al mostrar).

---

## Errores comunes en la entrevista de diseño

- **Empezar a dibujar sin requisitos.** Primero cinco minutos de preguntas.
- **No decir los supuestos.** Una cuenta sin supuestos escritos no se puede corregir.
- **Elegir por moda.** Cada pieza debe tener un motivo que se pueda decir en una frase.
- **Ignorar la concurrencia.** "¿Qué pasa si dos lo hacen a la vez?" llega casi siempre.
- **Defender el diseño contra toda corrección.** Cuando quien entrevista cambia un supuesto, ajustar el diseño es la respuesta correcta.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Requisito no funcional | Atributo de calidad que el sistema debe cumplir: latencia, disponibilidad, durabilidad, volumen |
| Estimación de servilleta | Cálculo rápido de órdenes de magnitud (peticiones por segundo, almacenamiento) con supuestos explícitos |
| Proporción de lectura y escritura | Cuántas lecturas hay por cada escritura; define si conviene caché y réplicas |
| Cache-aside | Patrón en el que la aplicación busca en la caché y, si no encuentra, lee de la base y guarda el resultado |
| TTL | Tiempo de vida de una entrada en caché; al vencer, se vuelve a leer de la fuente |
| Entrega al menos una vez | Garantía de una cola: ningún mensaje se pierde, pero alguno puede llegar repetido |
| Idempotencia | Propiedad de una operación que, aplicada varias veces, deja el mismo resultado que aplicada una |
| Escalamiento horizontal | Agregar instancias iguales detrás de un balanceador, en vez de una máquina más grande |
| API sin estado | API que no guarda datos de sesión en memoria, para que cualquier instancia atienda cualquier petición |
| Réplica de lectura | Copia de la base que recibe solo lecturas, para quitar carga a la principal |
| Punto único de falla | Pieza cuya caída tumba el sistema completo |
| Clave caliente | Registro que concentra una parte desproporcionada del tráfico |
| Restricción de exclusión | Restricción de PostgreSQL que impide que dos filas cumplan a la vez una condición, como dos rangos que se traslapan |

---

*Rogelio Arriaga Gonzalez*
