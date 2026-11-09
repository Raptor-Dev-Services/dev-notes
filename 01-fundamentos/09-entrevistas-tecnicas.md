# 09 - Entrevistas técnicas

Una entrevista técnica no mide cuánto sabes: mide cuánto de lo que sabes puedes demostrar en una hora, frente a alguien que no te conoce. Esta nota reúne lo que pasa antes de escribir código (el CV, el portafolio, la historia de tus proyectos), lo que pasa en la entrevista conductual y lo que pasa al final, cuando llega una oferta y hay que evaluarla completa.

> Fuente: *C# Interview Guide* (Semenenko, 2024): caps. 1, 2, 8, 9 y 10. *The Software Engineer's Guidebook* (Orosz, 2024): primera parte, "Developer Career Fundamentals", en especial "Switching Jobs".

---

## Problema que resuelve

Quien egresa sabe construir software, pero casi nadie le enseñó a contarlo. El resultado se repite:

- Un CV de tres páginas con una lista de tecnologías y ningún resultado.
- Un proyecto de titulación de seis meses que, contado en voz alta, dura veinte segundos y no dice qué hizo esa persona.
- Respuestas conductuales hipotéticas ("yo haría...") cuando la pregunta pedía un hecho ("cuéntame de una vez que...").
- Una primera oferta aceptada por el sueldo bruto, sin comparar prestaciones, esquema de contratación ni lo que se va a aprender.

Todo eso se corrige con preparación, no con talento. La preparación tiene estructura y se puede practicar.

---

## El proceso de contratación

Cada empresa arma el suyo, pero casi todos combinan estas etapas:

| Etapa | Qué evalúan | Cómo se prepara |
|-------|-------------|-----------------|
| Filtro de CV | Si tu experiencia encaja con la vacante | CV de una página, con resultados y palabras de la vacante que de verdad apliquen |
| Llamada con reclutamiento | Interés, disponibilidad, expectativas, comunicación | Tu proyecto en dos minutos y un rango salarial investigado |
| Prueba técnica para casa | Código limpio, pruebas, README | Tratarla como entrega profesional, con tiempo acotado |
| Entrevista técnica en vivo | Cómo piensas un problema y lo resuelves en voz alta | Estructuras de datos, algoritmos y el método de cuatro pasos |
| Diseño de sistemas (semi-senior) | Criterio para elegir piezas y defenderlas | Marco de cinco pasos y dos o tres diseños practicados |
| Conductual o con el equipo | Cómo trabajas con otras personas | Banco de historias en formato STAR |
| Oferta | Que aceptes | Matriz de comparación y fuentes salariales fechadas |

**Regla:** pregunta al inicio cuántas etapas tiene el proceso y qué evalúa cada una. Es una pregunta normal y te ahorra sorpresas.

---

## El CV de una página

### Estructura

```text
Nombre completo
Ciudad, correo, teléfono, enlace a GitHub y a LinkedIn

Resumen (dos líneas): qué haces y qué buscas.

Experiencia o proyectos (lo más reciente primero)
  Puesto o proyecto, lugar, fechas
  - Viñeta de logro
  - Viñeta de logro

Habilidades técnicas (agrupadas: lenguajes, frameworks, bases de datos, herramientas)

Educación (carrera, institución, año; titulación si aplica)
```

Una página alcanza para quien tiene menos de cinco años de experiencia. Si no cabe, sobra algo.

### La viñeta de logro

Cada viñeta sigue la misma fórmula: **verbo en pasado + qué hiciste + con qué + resultado verificable**.

| Antes | Después |
|-------|---------|
| Responsable del backend del proyecto | Diseñé e implementé la API de reservas en .NET con 14 endpoints y pruebas de integración en cada uno |
| Conocimientos en SQL | Reduje de 4 s a 300 ms la consulta del reporte mensual agregando un índice compuesto, medido con EXPLAIN ANALYZE |
| Trabajo en equipo | Coordiné a tres compañeros con revisiones de código en cada pull request durante cuatro meses |
| Apasionado por la tecnología | (se borra: es un adjetivo, no un hecho) |

Las cifras de la columna "Después" son ejemplos de la forma, no metas. Las tuyas salen de tu proyecto.

### Resultados sin inventar números

Un proyecto escolar casi nunca tiene usuarios ni ventas. No pasa nada: hay hechos verificables que sí tiene.

- **Tamaño:** cuántos endpoints, pantallas, tablas o módulos construiste tú.
- **Calidad:** cuántas pruebas, qué cobertura, si corre en CI.
- **Rendimiento:** un tiempo medido antes y después de un cambio.
- **Uso real:** un piloto con un grupo, un cliente del despacho de un conocido, la propia escuela.
- **Entrega:** está desplegado, tiene URL, tiene README que alguien más siguió.

**Regla:** un número que no puedes defender en la entrevista es peor que no poner número. Te van a preguntar cómo lo mediste.

### Lo que sobra

- Datos personales que la vacante no pide (estado civil, edad, CURP): no suman y ocupan espacio.
- Listas de tecnologías que no podrías defender en una pregunta técnica. Todo lo que escribes es una pregunta posible.
- Adjetivos sobre ti ("proactivo", "apasionado", "orientado a resultados").
- Faltas de ortografía. Un revisor que encuentra una, deja de leer con buena fe.
- Más de una página.

---

## El portafolio

Quien entrevista va a abrir tu GitHub. Lo que encuentre debe confirmar lo que dice tu CV.

- **Dos o tres repositorios fijados**, no veinte. El proyecto de titulación primero.
- **Un README por repositorio** con: qué problema resuelve, una captura, cómo correrlo en local, qué decisiones técnicas tomaste y por qué.
- **Historial legible:** commits con mensajes que expliquen qué cambió (ver [convenciones de commits](../07-git/06-commit-conventions.md)).
- **Pruebas que corren** y, si se puede, un pipeline en verde.
- **Una demo desplegada** vale más que tres párrafos.
- **LinkedIn coherente con el CV:** mismas fechas, mismos puestos.

---

## Tu proyecto en dos minutos

Es la pregunta que llega en todas las entrevistas, con alguna variante: "cuéntame de un proyecto". La respuesta se prepara y se ensaya en voz alta con cronómetro.

| Parte | Tiempo | Qué dice |
|-------|--------|----------|
| Problema | 20 s | Para quién es y qué dolor resuelve, sin tecnologías |
| Tu papel | 15 s | Qué hiciste tú, en primera persona del singular |
| La solución | 45 s | Arquitectura en una frase y **la decisión técnica más difícil**, con la alternativa que descartaste |
| Resultado | 20 s | Hechos verificables: está desplegado, lo usan, lo mediste |
| Qué harías distinto | 20 s | Una lección concreta; demuestra criterio |

Ejemplo de guion con el proyecto `{{PROJECT_NAME}}`, de `{{PROJECT_DOMAIN}}`:

```text
Problema:   Las recepcionistas agendaban por WhatsApp y se cruzaban citas dos o tres veces por semana.
Mi papel:   Éramos tres; yo hice la API y el modelo de datos.
Solución:   Una API en .NET con PostgreSQL y un panel en React. Lo difícil fue evitar que dos
            personas apartaran el mismo horario a la vez: lo resolví con un índice único en la
            base en vez de revisar en el código, porque la revisión en el código tiene una carrera.
Resultado:  Lo usa la clínica desde marzo, y desde entonces no se ha cruzado ninguna cita.
Distinto:   Escribiría las pruebas de concurrencia desde el principio; el error lo encontramos tarde.
```

### Preguntas de seguimiento

Después de los dos minutos vienen las preguntas. Prepara una respuesta corta para cada una:

- ¿Por qué esa base de datos y no otra?
- ¿Qué parte hiciste tú y qué parte otra persona?
- ¿Cómo lo probaste? ¿Qué pasa si se cae la base?
- ¿Qué fue lo que más te costó?
- Si tuviera diez veces más usuarios, ¿qué se rompe primero?

**En proyectos de equipo:** el "nosotros" esconde tu aporte. Di "nosotros" para el contexto y "yo" para tus acciones.

---

## La entrevista conductual

Las preguntas conductuales empiezan con "cuéntame de una vez que...". Preguntan por el pasado porque lo que hiciste predice mejor lo que harás que lo que dices que harías.

### STAR

| Letra | Qué cuenta | Peso aproximado |
|-------|-----------|-----------------|
| **S**ituación | Dónde y cuándo, en una o dos frases | poco |
| **T**area | Qué te tocaba a ti resolver | poco |
| **A**cción | Qué hiciste tú, paso por paso, y por qué | la mitad de la respuesta |
| **R**esultado | Qué pasó, con hechos, y qué aprendiste | lo suficiente para cerrar |

### Ejemplo

Pregunta: "Cuéntame de una vez que tuviste un desacuerdo con alguien de tu equipo."

```text
S: En el proyecto de titulación, a dos semanas de la entrega, un compañero quería reescribir
   el frontend con otro framework.
T: Yo coordinaba las entregas y tenía que decidir sin romper el equipo.
A: Le pedí que me mostrara qué problema resolvía el cambio. Hicimos una lista de lo que faltaba
   y estimamos ambas opciones. La reescritura costaba más de lo que nos quedaba. Le propuse
   anotarla como mejora para después de la entrega y que mientras tanto él se encargara de la
   parte que más le molestaba del código actual.
R: Entregamos a tiempo. Su refactor de esa parte quedó en la versión final, y aprendí a discutir
   con estimaciones en lugar de opiniones.
```

### Banco de historias

Seis historias bien preparadas cubren casi todas las preguntas conductuales. Escríbelas una vez en formato STAR y practica cuál usar para cada pregunta.

| Historia | Sirve para preguntas sobre |
|----------|---------------------------|
| Un conflicto o desacuerdo | trabajo en equipo, comunicación, manejo de conflictos |
| Un error tuyo | responsabilidad, aprendizaje, honestidad |
| Algo que aprendiste rápido | adaptabilidad, autonomía |
| Un plazo apretado | presión, priorización |
| Una decisión técnica que defendiste | criterio, influencia |
| El logro del que más te enorgulleces | motivación, impacto |

### Señales que restan

- **Culpar a otros:** el resultado malo siempre fue culpa del equipo, del profesor o del cliente.
- **Todo en plural:** después de tres minutos no se sabe qué hiciste tú.
- **Sin resultado:** la historia termina en la acción.
- **Hipotética:** "yo haría..." cuando la pregunta pedía un hecho.
- **Inventada:** las preguntas de seguimiento la desarman en un minuto.

### Las preguntas difíciles

- **"¿Cuál es tu mayor debilidad?"** Una real, que no sea central para el puesto, y lo que haces para mejorarla. Nada de "soy demasiado perfeccionista".
- **"¿Por qué te quieres ir de tu trabajo actual?"** Lo que buscas, no lo que te molesta. Nunca hables mal de un empleador anterior.
- **"¿Cuáles son tus pretensiones salariales?"** Un rango investigado en fuentes fechadas (ver más abajo), no una cifra improvisada.

---

## La entrevista técnica en vivo

El detalle está en dos notas: [20 - Estructuras de datos y complejidad](../02-programacion/csharp/20-estructuras-de-datos.md) para los problemas de código, y [16 - Diseño de sistemas](../03-arquitectura/16-diseno-de-sistemas.md) para la entrevista de diseño. El método es el mismo en ambas:

```text
1. Aclarar    Repite el problema con tus palabras. Pregunta tamaños, casos borde y qué devolver.
2. Proponer   Di primero la solución obvia y su costo. Luego busca dónde se repite trabajo.
3. Probar     Recorre tu código a mano con un ejemplo pequeño y con los casos borde.
4. Mejorar    Di qué cambiarías con más tiempo o con otros supuestos.
```

**Regla:** pensar en voz alta no es opcional. Quien entrevista califica cómo piensas; si no hablas, solo puede calificar el resultado.

---

## Tus preguntas al final

"¿Tienes preguntas para nosotros?" siempre llega. No preguntar nada parece desinterés. Algunas que dicen mucho del lugar:

- ¿Cómo es un día normal de alguien en este puesto?
- ¿Cómo se mide el éxito en los primeros tres meses?
- ¿Cómo se revisa el código? ¿Hay pruebas automáticas y despliegue continuo?
- ¿Quién sería mi persona de referencia los primeros meses?
- ¿Qué es lo más difícil del equipo ahora mismo?

---

## La oferta

Una oferta se evalúa completa: sueldo, prestaciones, esquema de contratación, crecimiento, equipo y lo que vas a aprender. Comparar solo el bruto mensual es la forma más común de elegir mal.

### Qué comparar

| Concepto | Pregunta que lo aclara |
|----------|------------------------|
| Sueldo bruto mensual | ¿Es bruto o neto? ¿Es fijo o tiene variable? |
| Esquema de contratación | ¿Nómina, honorarios o contrato como independiente? ¿Quién paga impuestos y seguridad social? |
| Prestaciones de ley | ¿Aguinaldo, vacaciones, prima vacacional, IMSS, Infonavit y PTU desde el primer día? |
| Prestaciones superiores | ¿Seguro de gastos médicos, vales, fondo de ahorro, días adicionales? |
| Modalidad y horario | ¿Presencial, híbrido o remoto? ¿Cuánto tiempo y dinero cuesta el traslado? |
| Periodo de prueba | ¿Cuánto dura y cómo se evalúa? |
| Crecimiento | ¿Hay una ruta de niveles escrita? ¿Cada cuánto se revisa el sueldo? |
| Aprendizaje | ¿Hay revisiones de código, mentoría, presupuesto de capacitación? |
| Equipo | ¿Quién sería tu líder? ¿Hay personas senior de quienes aprender? |

### Bruto y neto

El sueldo que se negocia suele ser bruto; el que llega a tu cuenta es el neto, después de retener ISR y la cuota del trabajador al IMSS. Para comparar dos ofertas:

1. Convierte las dos a **neto mensual** con la tarifa de ISR vigente y las cuotas del IMSS del año en curso.
2. Suma el valor anual de las prestaciones y divídelo entre doce.
3. Si una oferta es como independiente, resta lo que tendrías que pagar tú de impuestos y de seguridad social, y lo que dejas de recibir en prestaciones.

**Regla:** nunca compares el bruto de una oferta en nómina contra el bruto de una oferta por honorarios. No son la misma cantidad de dinero.

Los mínimos de ley (por ejemplo, el aguinaldo de al menos 15 días de salario) están en la Ley Federal del Trabajo. Revisa la versión vigente el día que evalúes la oferta: las reformas cambian esos mínimos.

### Rangos salariales: fuentes locales y fechadas

Esta nota no trae cifras de sueldo a propósito: envejecen en meses y cambian por ciudad, sector y modalidad. Los libros base hablan del mercado de Estados Unidos, que no sirve como referencia aquí. Lo que sí se puede enseñar es el método:

1. **Junta al menos tres fuentes locales:** encuestas salariales de comunidades de desarrollo en México, vacantes publicadas que traigan rango, reportes de agencias de reclutamiento y datos oficiales de ocupación e ingreso.
2. **Anota de cada una:** el puesto exacto, la ciudad o modalidad, el nivel (junior, semi-senior), la fecha de publicación y si la cifra es bruta o neta.
3. **Descarta lo viejo:** una cifra de hace más de un año ya no describe el mercado.
4. **Escribe tu rango así:**

```text
Desarrollador .NET junior, remoto desde México:
<RANGO_SALARIAL_SEGUN_FUENTE_Y_FECHA> bruto mensual
Fuentes: <FUENTE_1, FECHA>, <FUENTE_2, FECHA>, <FUENTE_3, FECHA>
```

### Matriz ponderada

Para decidir entre dos ofertas, pon peso a lo que te importa y califica cada una del 1 al 5:

| Criterio | Peso | Oferta A | Oferta B |
|----------|------|----------|----------|
| Neto mensual más prestaciones | 30 % | 4 | 3 |
| Aprendizaje y mentoría | 25 % | 2 | 5 |
| Estabilidad y esquema de contratación | 20 % | 3 | 4 |
| Modalidad y traslado | 15 % | 5 | 3 |
| Ruta de crecimiento | 10 % | 2 | 4 |
| **Total ponderado** | 100 % | **3.25** | **3.80** |

El total se calcula multiplicando cada calificación por su peso y sumando. La matriz no decide por ti, pero te obliga a decir qué te importa antes de ver los números.

### Negociar

- **Agradece y pide la oferta por escrito**, con sueldo bruto, esquema y prestaciones.
- **Pregunta el plazo** para responder. Pedir dos o tres días es normal.
- **Contrapropón con fundamento:** "según tres fuentes de este mes, el rango para el puesto es...". Un número sin fuente es una opinión.
- **Negocia el paquete, no solo el sueldo:** fecha de revisión salarial, capacitación, días de trabajo remoto.
- **No inventes otras ofertas.** Si existe otra, dilo; si no, no.
- **Conoce tu mínimo** antes de la llamada. Si no lo alcanzan, declina con cortesía: el mundo es chico.

### Señales de alerta

- No hay contrato por escrito o te piden empezar antes de firmarlo.
- Parte del sueldo se paga "por fuera" de la nómina.
- Presión para aceptar el mismo día.
- La prueba técnica es trabajo real del cliente, sin pago.
- Nadie del equipo técnico participó en el proceso.

---

## Después de cada entrevista

Lleva una bitácora. Al terminar cada entrevista, en los primeros quince minutos, anota:

```text
Fecha / empresa / etapa:
Qué preguntaron (literal, lo que recuerdes):
Qué contesté bien:
Dónde me atoré y qué debí decir:
Qué tengo que estudiar antes de la siguiente:
```

Un correo breve de agradecimiento el mismo día no decide nada por sí solo, pero cierra bien. Si te rechazan, pide retroalimentación: no siempre la dan, y cuando la dan vale oro.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Banco de historias | Conjunto de seis o más experiencias propias escritas en formato STAR para contestar preguntas conductuales |
| Bruto y neto | El bruto es el sueldo antes de impuestos y cuotas; el neto es lo que llega a tu cuenta |
| Entrevista conductual | Entrevista que pregunta por hechos pasados ("cuéntame de una vez que...") para predecir cómo trabajarás |
| Esquema de contratación | Forma legal de la relación laboral: nómina, honorarios o contrato como independiente; define impuestos y prestaciones |
| Filtro de CV | Primera etapa del proceso, en la que se decide si el perfil encaja con la vacante |
| Matriz ponderada | Tabla que califica opciones por criterios con peso, para decidir con prioridades declaradas |
| Pitch del proyecto | Relato de dos minutos de un proyecto: problema, papel, solución, resultado y lección |
| Pregunta de seguimiento | Pregunta que profundiza en tu respuesta para comprobar que la historia es tuya y la entiendes |
| Prestaciones de ley | Beneficios mínimos que la Ley Federal del Trabajo obliga a dar en una relación laboral |
| Rango salarial fundamentado | Rango de sueldo con puesto, nivel, modalidad, fuente y fecha de cada dato |
| STAR | Situación, Tarea, Acción y Resultado: estructura para contar una experiencia con hechos |
| Viñeta de logro | Línea del CV con verbo en pasado, qué hiciste, con qué y un resultado verificable |

---

*Rogelio Arriaga Gonzalez*
