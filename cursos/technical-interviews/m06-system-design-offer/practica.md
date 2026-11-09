# Práctica - Un diseño en 30 minutos y dos ofertas

## Objetivo

Dos ejercicios. En el primero, diseñar en equipo un sistema sencillo con el marco de cinco pasos y defenderlo frente a alguien que pregunta. En el segundo, comparar dos ofertas ficticias completas, no solo por su sueldo, con fuentes salariales reales y fechadas. Al final está el proyecto del curso: una entrevista simulada completa.

**Lecturas de apoyo:** [16 - Diseño de sistemas](../../../03-arquitectura/16-diseno-de-sistemas.md), [12 - C4 Model](../../../03-arquitectura/12-c4-model.md) y [09 - Entrevistas técnicas](../../../01-fundamentos/09-entrevistas-tecnicas.md).

---

## Parte A. Diseñar en 30 minutos

### Organización

Equipos de tres, con papeles fijos durante los 30 minutos:

- **Quien diseña** habla y dibuja (pizarrón, papel o una herramienta de diagramas).
- **Quien entrevista** contesta las aclaraciones con lo que dice el enunciado y hace las preguntas de profundización a partir del minuto 20.
- **Quien observa** lleva el reloj y llena la rúbrica de diseño.

Elijan uno de los dos enunciados. Si su equipo ya vio el acortador en clase, elijan el de reservas.

### Enunciado 1. Un acortador de URL

"Diseña un servicio que convierta URL largas en enlaces cortos, redirija del corto al largo y cuente los clics."

**Aclaraciones para quien entrevista:** un millón de enlaces nuevos al día; cien lecturas por cada escritura; los enlaces no caducan; no hay cuentas de usuario.

**Preguntas de profundización:** ¿qué código de redirección usas y por qué? ¿Cómo generas el código corto y cuántos caracteres necesita? ¿Qué pasa si un enlace se vuelve viral? ¿Qué pasa si se cae la caché?

La solución de referencia es la del ejemplo resuelto en clase y en la nota 16.

### Enunciado 2. Un sistema de reservas

"Diseña un sistema para que los pacientes de una red de clínicas aparten citas en línea con un especialista."

**Aclaraciones para quien entrevista:** 2,000 clínicas; unas 80 citas por clínica al día; las citas son bloques fijos de 30 minutos; el paciente ve la disponibilidad y aparta; puede cancelar hasta 24 horas antes; se manda un recordatorio un día antes.

**Preguntas de profundización:** ¿qué pasa si dos pacientes apartan el mismo horario en el mismo segundo? ¿Cómo guardas las horas si hay clínicas en distintas zonas horarias? ¿Cómo mandas los recordatorios? ¿Qué pasa si el paciente abandona el pago a la mitad?

### Solución de referencia del sistema de reservas

**Requisitos.** Ver disponibilidad, apartar, cancelar, recordar. No funcional clave: **nunca dos citas en el mismo horario del mismo especialista**. Fuera de alcance: pagos, historial clínico.

**Cuentas** (supuestos del enunciado y de la entrevista):

```text
Citas:          2,000 clínicas x 80 al día  =  160,000 al día  ->  unas 2 por segundo
Consultas de
disponibilidad: 20 por cada cita apartada (supuesto)  ->  unas 40 por segundo
Pico:           10 veces el promedio un lunes a primera hora (supuesto)  ->  unas 400 lecturas por segundo
```

Conclusión: el volumen es modesto y cabe en una base relacional con una API de pocas instancias. El reto no es el volumen: es la **concurrencia** sobre el mismo horario.

**API.**

```http
GET    /api/especialistas/{id}/disponibilidad?fecha=2026-11-09
POST   /api/reservas          { "especialistaId": 42, "inicio": "2026-11-09T16:00:00Z" }
DELETE /api/reservas/{id}
```

`POST` responde 201 si apartó y **409 Conflict** si el horario ya estaba tomado.

**Datos y la regla de oro.** La base garantiza que no haya dos reservas iguales. Revisar en el código antes de insertar tiene una carrera. En la tabla, `recurso_id` es el especialista: el mismo diseño sirve para salas, equipos o mesas.

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

Este SQL se probó en PostgreSQL 17: la primera reserva devuelve su `id`, la segunda del mismo horario no devuelve fila, y la tabla queda con una sola. Si las citas fueran de duración libre, la nota 16 muestra la restricción de exclusión con rangos.

Para explicar el principio en la pizarra sin base de datos:

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

La prueba de esta clase lanza 200 intentos en paralelo por el mismo horario y comprueba que gana exactamente uno.

**Profundizar.**
- **Zonas horarias:** se guarda en UTC (`timestamptz`) y se convierte a la zona de la clínica solo al mostrar.
- **Recordatorios:** un worker programado busca las citas de mañana y encola un mensaje por cita; el envío es idempotente, con el id de la cita como llave.
- **Pago abandonado:** si hubiera pago, el horario se aparta temporalmente con una columna de vencimiento, y un worker libera los apartados vencidos.
- **Disponibilidad:** es la lectura más frecuente; se puede cachear por especialista y día con un TTL corto, invalidando al reservar o cancelar.

### Rúbrica de diseño

Quien observa califica cada criterio del 1 al 4 y anota una frase de evidencia.

| Criterio | 1 | 2 | 3 | 4 |
|----------|---|---|---|---|
| Requisitos | Empezó a dibujar sin preguntar | Preguntó algo, sin no funcionales | Funcionales y no funcionales claros | Además dijo qué quedaba fuera y por qué |
| Estimación | No hizo cuentas | Cuentas sin supuestos dichos | Cuentas con supuestos, orden de magnitud correcto | Además sacó una conclusión que guió el diseño |
| API y datos | No los definió | Endpoints o tablas, no los dos | Endpoints y tablas con llaves | Además los índices y restricciones que exige la operación clave |
| Alto nivel | Cajas sin explicar | Piezas correctas sin justificar | Cada pieza con su motivo | Además el camino de una petición de punta a punta |
| Profundizar | No encontró problemas | Encontró uno al preguntarle | Encontró el cuello de botella y el punto único de falla | Además resolvió la concurrencia o la clave caliente con una pieza concreta |
| Ajuste | Defendió el diseño contra toda corrección | Aceptó correcciones sin cambiar el diseño | Ajustó el diseño al cambiar un supuesto | Ajustó y explicó qué otra parte cambiaba por eso |

---

## Parte B. Comparar dos ofertas

Las dos ofertas son ficticias. **No traen cifras de sueldo:** se expresan en múltiplos de `S`, el punto medio del rango que tú vas a investigar. Así el ejercicio sirve el día que se haga, con datos de ese día.

### Paso 1. Encuentra S

Busca tres fuentes locales y fechadas para el puesto "desarrollador .NET junior" en tu ciudad o en remoto desde México, y llena:

| Fuente | Fecha de publicación | Ciudad o modalidad | Nivel | ¿Bruto o neto? | Rango |
|--------|----------------------|--------------------|-------|----------------|-------|
| `<FUENTE_1>` | `<FECHA>` | | | | `<RANGO>` |
| `<FUENTE_2>` | `<FECHA>` | | | | `<RANGO>` |
| `<FUENTE_3>` | `<FECHA>` | | | | `<RANGO>` |

```text
Rango que voy a usar: <RANGO_SALARIAL_SEGUN_FUENTE_Y_FECHA> bruto mensual
S (punto medio):      <S_BRUTO_MENSUAL>
```

Descarta fuentes de hace más de un año y no mezcles brutos con netos.

### Paso 2. Lee las ofertas

**Oferta A**
- Nómina. Sueldo bruto mensual: **1.15 × S**.
- Prestaciones de ley, nada más.
- Presencial cinco días; traslado de 1 h 15 min por sentido.
- Equipo de tres personas, ninguna senior. Desde el primer mes serías responsable de un módulo completo. No hay revisiones de código.
- Revisión salarial "cuando haya presupuesto"; no hay ruta de niveles escrita.

**Oferta B**
- Nómina. Sueldo bruto mensual: **1.00 × S**.
- Prestaciones de ley, más seguro de gastos médicos mayores, vales de despensa y fondo de ahorro.
- Híbrido: dos días presenciales; traslado de 30 min por sentido.
- Equipo de ocho personas, dos senior. Revisión de código en cada pull request y una persona mentora asignada.
- Ruta de niveles escrita, con revisión salarial cada seis meses.

### Paso 3. Haz las cuentas

1. **Neto mensual** de A y de B con la tarifa de ISR y las cuotas del IMSS vigentes en la fecha del ejercicio. Anota de dónde sacaste las tablas.
2. **Valor mensual de las prestaciones superiores de B:** pregunta o investiga cuánto costaría por tu cuenta un seguro de gastos médicos equivalente, y suma vales y fondo de ahorro. Si no encuentras el dato, déjalo como `<VALOR_PRESTACIONES_B>` y dilo.
3. **Costo del traslado:** horas al mes y dinero al mes en cada oferta.
4. **Neto efectivo** = neto mensual más prestaciones menos traslado.

### Paso 4. Llena tu matriz

Decide **tus** pesos antes de calificar. Califica cada oferta del 1 al 5:

| Criterio | Tu peso | Oferta A | Oferta B |
|----------|---------|----------|----------|
| Neto efectivo | | | |
| Aprendizaje y mentoría | | | |
| Estabilidad y esquema | | | |
| Modalidad y traslado | | | |
| Ruta de crecimiento | | | |
| **Total ponderado** | 100 % | | |

### Paso 5. Escribe tu respuesta

Para la oferta que elijas, escribe en cinco líneas un correo que agradezca, confirme lo que entendiste por escrito y proponga **una** mejora con fundamento (por ejemplo, la fecha de la primera revisión salarial o un ajuste al sueldo citando tus fuentes fechadas).

### Variante: una tercera oferta como independiente

**Oferta C:** contrato como independiente con un cliente del extranjero, pagado en dólares. El bruto equivale hoy a **1.60 × S**, sin prestaciones de ley, cancelable con dos semanas de aviso.

Antes de compararla, resta lo que pagarías tú: impuestos como persona física con actividad empresarial, seguridad social por tu cuenta, un seguro médico y el equivalente de aguinaldo y vacaciones que no vas a recibir. Considera también el riesgo del tipo de cambio. Escribe en una línea **a partir de qué múltiplo de S** te convendría C sobre B.

---

## Proyecto final del curso

Una entrevista simulada completa, grabada, de 60 minutos. Te entrevista otra persona del curso, que no sea tu pareja habitual.

| Bloque | Minutos | Qué pasa | Rúbrica |
|--------|---------|----------|---------|
| Conductual | 15 | Tu proyecto en dos minutos, dos preguntas de seguimiento y dos preguntas STAR | Módulo 1 |
| Técnica | 30 | Un problema que no conoces, de los módulos 2 a 4, resuelto en voz alta | Módulo 4 |
| Diseño | 15 | Un sistema sencillo con el marco de cinco pasos, en versión corta | La de diseño de esta práctica |

**Entregas:**

- La grabación (con permiso de las dos personas), que se borra al terminar el curso.
- Las tres rúbricas llenas por quien te entrevistó, **con una frase de evidencia por criterio**.
- Una autoevaluación de una página: tu calificación más baja en cada bloque, qué la explica y **tres cosas concretas** que vas a cambiar antes de tu siguiente entrevista real.

**Cómo se aprueba:** no hay un puntaje mínimo. Se aprueba con las tres rúbricas completas con evidencia y una autoevaluación que identifique, con ejemplos de la grabación, qué cambiar.

---

## Checklist de la práctica

- [ ] Diseñé en equipo un sistema en 30 minutos y recibí la rúbrica de diseño.
- [ ] Puedo explicar por qué la regla de "no dos reservas iguales" la garantiza la base y no el código.
- [ ] Encontré tres fuentes salariales locales y fechadas y calculé S.
- [ ] Comparé las ofertas A y B en neto efectivo, no en bruto.
- [ ] Llené mi matriz con pesos decididos antes de calificar.
- [ ] Escribí la respuesta a la oferta elegida con una mejora fundamentada.
- [ ] Agendé mi entrevista del proyecto final.
