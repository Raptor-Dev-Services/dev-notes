<!-- _class: lead -->

# Módulo 6 - Diseño de sistemas y la oferta

### Preparación para entrevistas técnicas

**Rogelio Arriaga - Raptor Dev Services**

---

## Hoy

> La primera oferta se evalúa completa: salario, crecimiento, equipo y lo que se aprende.

- **Parte 1:** el marco de cinco pasos y las piezas de un sistema
- **Parte 2:** un acortador de URL resuelto de punta a punta
- **Parte 3:** cómo leer, comparar y negociar una oferta
- **Práctica:** diseñar en 30 minutos un acortador o un sistema de reservas, y comparar dos ofertas ficticias

Lecturas: [16 - Diseño de sistemas](../../../03-arquitectura/16-diseno-de-sistemas.md), [12 - C4 Model](../../../03-arquitectura/12-c4-model.md), [07 - Microservicios](../../../03-arquitectura/07-microservicios.md), [05 - Caching](../../../04-backend/05-caching.md) y [09 - Entrevistas técnicas](../../../01-fundamentos/09-entrevistas-tecnicas.md)

---

## Qué se espera de ti en diseño

Nadie espera que un perfil junior o semi-senior diseñe la infraestructura de una empresa grande. Se espera que:

- convierta un enunciado vago en **requisitos**,
- haga **cuentas de servilleta** con supuestos dichos en voz alta,
- arme un diseño con **piezas conocidas** y diga por qué cada una,
- encuentre **qué se rompe primero** y cómo lo arreglaría.

Lo que se califica es el razonamiento, no la cantidad de cajas.

---

<!-- _class: lead -->

# Parte 1
## El marco y las piezas

---

## Cinco pasos en 45 minutos

| Paso | Minutos | Qué produces |
|------|---------|--------------|
| 1. Requisitos | 5 | Funciones, atributos de calidad y lo que queda fuera |
| 2. Estimación | 5 | Peticiones por segundo, almacenamiento, lecturas contra escrituras |
| 3. API y datos | 10 | Endpoints principales y tablas con sus llaves e índices |
| 4. Alto nivel | 10 | Diagrama de contenedores con el camino de una petición |
| 5. Profundizar | 15 | Cuello de botella, punto único de falla y cómo resolverlos |

El paso 5 suele dirigirlo quien entrevista: "¿y si se cae la base?". Deja tiempo para eso.

---

## Requisitos: lo que se pregunta

- **Funcionales**, como verbos: crear, redirigir, reservar, cancelar, contar.
- **No funcionales:**
  - ¿Cuántos usuarios y operaciones al día?
  - ¿Se lee mucho más de lo que se escribe?
  - ¿Qué tan rápido debe responder lo más común?
  - ¿Qué pasa si se pierde un dato o si dos hacen lo mismo a la vez?
  - ¿Cuánto tiempo se guardan los datos?
- **Fuera de alcance:** dilo en voz alta. Recortar también es criterio.

---

## Cuentas de servilleta

| Dato útil | Valor |
|-----------|-------|
| Segundos en un día | 86,400, redondeado a 10^5 para hacer cuentas de cabeza |
| Un millón de eventos al día | unos 12 por segundo en promedio |
| Pico | un múltiplo del promedio que se **supone** y se dice |
| KB, MB, GB, TB | 10^3, 10^6, 10^9 y 10^12 bytes |

No se busca precisión: se busca saber si cabe en un servidor o en cien. Si quien entrevista corrige un supuesto, rehaces la cuenta. Eso suma.

<!--
Nota del instructor: haz en vivo la cuenta de un caso que el grupo proponga (por ejemplo, las fotos de una red social de la escuela) y deja que corrijan los supuestos. Lo importante es que vean que el resultado cambia de orden de magnitud con un solo supuesto.
-->

---

## Las piezas

| Pieza | Para qué | Cuándo sobra |
|-------|----------|--------------|
| Balanceador | Repartir peticiones entre instancias | Con una sola instancia |
| API sin estado | Que cualquier instancia atienda cualquier petición | Nunca; la sesión va fuera del proceso |
| Base relacional | Fuente de verdad, transacciones, restricciones | Casi nunca |
| Réplicas de lectura | Repartir lecturas | Mientras la principal aguante |
| Caché | Lo que se lee mucho, sin ir a la base | Si cada lectura es distinta |
| Cola y worker | Trabajo lento o que puede esperar | Si todo cabe en la petición |
| Almacenamiento de objetos | Archivos e imágenes | Si no hay archivos |

**Relacional por defecto.** Una base no relacional se justifica con un motivo concreto, no por moda.

---

## Caché

- **Cache-aside:** busca en la caché; si no está, lee de la base, guarda con un TTL y devuelve.
- **¿Y si el dato cambia?** Se invalida al escribir, o se acepta que esté viejo hasta que venza el TTL.
- **¿Y si vence una entrada muy leída?** Muchas peticiones van a la base a la vez: estampida. Se mitiga con un candado por clave o renovando antes de que venza.

El detalle en .NET (`IMemoryCache`, `IDistributedCache`, Redis) está en la nota 05 de backend.

---

## Colas: al menos una vez

La petición encola y responde; un worker procesa después. Casi todas las colas entregan **al menos una vez**: un mensaje puede llegar repetido. El consumidor debe ser **idempotente**.

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

La prueba entrega cada uno de 500 mensajes diez veces, en paralelo, y cuenta exactamente 500. En producción, el registro de procesados vive en la base, en la misma transacción que el efecto.

---

## Escalar sin exagerar

- **Vertical primero:** una máquina más grande es lo más barato de operar, hasta que se acaba.
- **Horizontal:** más instancias detrás de un balanceador. Exige una API sin estado.
- **La base es lo último en escalar horizontalmente:** antes, índices, caché y réplicas de lectura.
- **Monolito modular por defecto.** Proponer microservicios obliga a hablar de red entre servicios, consistencia eventual y despliegues independientes. Hazlo solo si el problema lo pide.

Para dibujarlo: los niveles 1 (contexto) y 2 (contenedores) del modelo C4 bastan en una entrevista.

---

<!-- _class: lead -->

# Parte 2
## Ejemplo resuelto: un acortador de URL

---

## Requisitos y cuentas

- **Funcionales:** crear un enlace corto, redirigir al original, contar clics.
- **No funcionales:** la redirección muy rápida; un enlace nunca cambia de destino.
- **Fuera:** cuentas de usuario, enlaces personalizados.

```text
Enlaces nuevos:   1,000,000 al día (supuesto)  ->  unos 12 por segundo
Lecturas:         100 por escritura (supuesto)  ->  unas 1,200 por segundo; pico x5: unas 5,800
Almacenamiento:   500 B x 1,000,000 x 365  =  182.5 GB al año; unos 0.9 TB en 5 años
Enlaces en 5 años: 1,825,000,000
Caché:            20 % de los enlaces de un mes = 0.2 x 30,000,000 x 500 B = 3 GB
```

**Conclusión:** lectura intensa; cabe en una base relacional por años; la caché cabe en un Redis.

---

## La API, y 301 o 302

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

Con **301** el navegador guarda la redirección y las visitas repetidas ya no llegan: menos carga, pero no se cuentan. Con **302** cada clic pasa por el servidor. Contar es requisito: 302.

---

## Generar el código corto

| Opción | A favor | En contra |
|--------|---------|-----------|
| Contador de la base en base 62 | Sin colisiones, simple | Consecutivos: se pueden adivinar |
| Hash de la URL recortado | La misma URL da el mismo código | Colisiones que hay que detectar |
| Siete caracteres al azar con índice único | No se adivinan | Reintentar en la rara colisión |

Con 62 símbolos: 5 caracteres dan unos 9.2 × 10^8 códigos y **no alcanzan** para 1,825 millones; 6 dan unos 5.7 × 10^10; 7, unos 3.5 × 10^12.

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

---

## Los contenedores

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

El camino caliente (la redirección) toca solo la caché en la mayoría de los casos. El conteo sale del camino de la petición.

---

## Profundizar

- **La redirección no espera al conteo:** el clic se encola y la API responde de inmediato.
- **El worker es idempotente:** cada clic lleva un id y no se cuenta dos veces.
- **Un enlace viral** es una clave caliente: la caché lo absorbe; si hiciera falta, se guarda también en la memoria de cada instancia con un TTL corto.
- **Si se cae Redis,** la API lee de la base: más lenta, pero funciona.
- **Si se cae la base,** las redirecciones ya cacheadas siguen funcionando; crear enlaces falla.

---

## Ejercicio en vivo: ¿qué se rompe primero?

En equipos de tres, 5 minutos por escenario. Digan qué pieza sufre y qué cambiarían:

1. Un enlace aparece en un programa de televisión y recibe el 30 % de todo el tráfico durante una hora.
2. Se pide poder **editar** el destino de un enlace ya creado.
3. Se pide un reporte de clics por país, por hora, de los últimos dos años.

<!--
Nota del instructor: 1, clave caliente; la caché lo absorbe y conviene una copia local en cada instancia; revisar el TTL. 2, con 301 los navegadores no se enteran del cambio: refuerza la elección de 302, y hay que invalidar la caché al editar. 3, el worker ya agrega por día; agregar por país y hora en una tabla aparte, y no consultar la tabla de clics crudos para el reporte. Pide que digan qué supuesto cambió en cada caso.
-->

---

## Si eliges el sistema de reservas

El requisito que lo define: **dos personas no pueden apartar el mismo horario del mismo recurso.**

- Revisar en el código ("si está libre, lo guardo") tiene una **carrera**: las dos peticiones ven libre y las dos guardan.
- La regla la garantiza **la base**: una restricción única sobre recurso y horario, o una restricción de exclusión si los horarios son rangos de duración libre.
- Si el `INSERT` no inserta, el horario estaba tomado: **409 Conflict**.

El SQL completo, probado en PostgreSQL 17, está en la nota 16 y en la práctica.

---

<!-- _class: lead -->

# Parte 3
## La oferta

---

## Qué trae una oferta

| Concepto | Pregunta que lo aclara |
|----------|------------------------|
| Sueldo | ¿Bruto o neto? ¿Fijo o con variable? |
| Esquema | ¿Nómina, honorarios o independiente? ¿Quién paga impuestos y seguridad social? |
| Prestaciones de ley | ¿Aguinaldo, vacaciones, prima vacacional, IMSS, Infonavit y PTU desde el primer día? |
| Prestaciones superiores | ¿Gastos médicos, vales, fondo de ahorro? |
| Modalidad | ¿Presencial, híbrido o remoto? ¿Cuánto cuesta el traslado? |
| Crecimiento | ¿Ruta de niveles escrita? ¿Cada cuánto se revisa el sueldo? |
| Aprendizaje y equipo | ¿Revisiones de código, mentoría, personas senior? |

---

## Bruto, neto y esquema

- Lo que se negocia suele ser **bruto**; lo que llega a tu cuenta es el **neto**, después de ISR y de la cuota del trabajador al IMSS.
- Para comparar: convierte las dos ofertas a **neto mensual** con la tarifa de ISR y las cuotas vigentes, y suma el valor mensual de las prestaciones.
- **Por honorarios o como independiente** pagas tú impuestos y seguridad social, y no recibes prestaciones de ley.

**Nunca compares el bruto de una nómina contra el bruto de unos honorarios:** no son la misma cantidad de dinero.

Los mínimos de ley están en la Ley Federal del Trabajo. Revisa la versión vigente el día que evalúes la oferta.

---

## Rangos salariales: fuente y fecha

Este curso no trae cifras de sueldo a propósito: envejecen en meses y los libros hablan del mercado de Estados Unidos.

1. Junta **tres fuentes locales**: encuestas de comunidades de desarrollo en México, vacantes publicadas con rango, reportes de agencias de reclutamiento, datos oficiales de ocupación e ingreso.
2. De cada una anota **puesto, nivel, ciudad o modalidad, fecha y si es bruto o neto**.
3. Descarta lo de hace más de un año.

```text
Desarrollador .NET junior, remoto desde México:
<RANGO_SALARIAL_SEGUN_FUENTE_Y_FECHA> bruto mensual
Fuentes: <FUENTE_1, FECHA>, <FUENTE_2, FECHA>, <FUENTE_3, FECHA>
```

<!--
Nota del instructor: llena el marcador en clase con las fuentes de esa semana, en vivo, y deja anotada la fecha en la lámina. Nunca reutilices el rango de un grupo anterior sin volver a buscarlo.
-->

---

## La matriz ponderada

| Criterio | Peso | Oferta A | Oferta B |
|----------|------|----------|----------|
| Neto mensual más prestaciones | 30 % | 4 | 3 |
| Aprendizaje y mentoría | 25 % | 2 | 5 |
| Estabilidad y esquema | 20 % | 3 | 4 |
| Modalidad y traslado | 15 % | 5 | 3 |
| Ruta de crecimiento | 10 % | 2 | 4 |
| **Total ponderado** | 100 % | **3.25** | **3.80** |

Cada calificación por su peso, y se suma. La oferta que paga más no ganó. **Decide los pesos antes de ver las ofertas.**

<!--
Nota del instructor: haz la cuenta de la Oferta A en voz alta: 4 x 0.30 + 2 x 0.25 + 3 x 0.20 + 5 x 0.15 + 2 x 0.10 = 3.25. Luego pide a dos personas con prioridades distintas que cambien los pesos y muestren que gana la otra oferta.
-->

---

## Negociar

- **Agradece y pide la oferta por escrito:** sueldo bruto, esquema y prestaciones.
- **Pregunta el plazo.** Pedir dos o tres días es normal.
- **Contrapropón con fundamento:** "según tres fuentes de este mes, el rango es...". Un número sin fuente es una opinión.
- **Negocia el paquete:** fecha de revisión salarial, capacitación, días remotos.
- **No inventes otras ofertas.**
- **Conoce tu mínimo** antes de la llamada. Si no lo alcanzan, declina con cortesía.

---

## Señales de alerta

- No hay contrato por escrito, o te piden empezar antes de firmar.
- Parte del sueldo se paga "por fuera" de la nómina.
- Presión para aceptar el mismo día.
- La prueba técnica es trabajo real, sin pago.
- Nadie del equipo técnico participó en el proceso.

---

## La práctica de hoy

**Parte A, 30 minutos, en equipos de tres:** diseñar un acortador de URL o un sistema de reservas con los cinco pasos. Una persona dibuja, otra lleva el reloj y otra hace de entrevistadora con las preguntas de la práctica.

**Parte B, 20 minutos, individual:** comparar dos ofertas ficticias con la matriz ponderada, después de llenar el rango salarial con fuentes fechadas.

<!--
Nota del instructor: la Parte A se califica con la rúbrica de diseño de la práctica. En la Parte B, discute al final por qué distintas personas eligieron ofertas distintas con los mismos números: los pesos son personales, y eso es justo lo que la matriz hace visible.
-->

---

## El proyecto final

**Una entrevista simulada completa, grabada, de 60 minutos:**

| Bloque | Minutos | Se califica con |
|--------|---------|-----------------|
| Conductual: proyecto en dos minutos y dos preguntas STAR | 15 | Rúbrica del módulo 1 |
| Técnica: un problema que no conoces | 30 | Rúbrica del módulo 4 |
| Diseño: un sistema sencillo | 15 | Rúbrica de diseño de este módulo |

Te entrevista otra persona del curso y te devuelve la rúbrica con evidencia. Los detalles están en la práctica.

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas

- Un marco de cinco pasos para diseñar y defender un sistema sencillo
- Las piezas (API, base, caché, colas) y cuándo sobra cada una
- Un método para evaluar una oferta completa, con fuentes y fechas

**Libros:** *C# Interview Guide* (Semenenko, 2024), caps. 9 a 11. *The Software Engineer's Guidebook* (Orosz, 2024), primera parte ("Switching Jobs") y tercera parte ("Software Architecture").

**Fin del curso.** Lo que sigue es la entrevista de verdad.
