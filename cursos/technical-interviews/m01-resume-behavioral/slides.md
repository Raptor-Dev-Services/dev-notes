<!-- _class: lead -->

# Módulo 1 - CV, portafolio y entrevista conductual

### Preparación para entrevistas técnicas

**Rogelio Arriaga - Raptor Dev Services**

<!--
Nota del instructor: abre preguntando quién ya tuvo una entrevista y cómo le fue. Anota en el pizarrón dos o tres cosas que salieron mal: casi siempre aparecen "no supe qué decir de mi proyecto" y "me preguntaron algo que puse en el CV y no lo sabía". Esas dos frases son el módulo.
-->

---

## Hoy

> Un proyecto que no se puede contar en dos minutos no existe para quien entrevista.

- **Parte 1:** el CV de una página y el portafolio
- **Parte 2:** tu proyecto en dos minutos
- **Parte 3:** la entrevista conductual con STAR
- **Práctica:** en tríos, cada quien cuenta su proyecto y otra persona lo califica con rúbrica

Lectura: [09 - Entrevistas técnicas](../../../01-fundamentos/09-entrevistas-tecnicas.md)

---

## Cómo es un proceso de contratación

| Etapa | Qué evalúan |
|-------|-------------|
| Filtro de CV | Si tu experiencia encaja con la vacante |
| Llamada con reclutamiento | Interés, expectativas, comunicación |
| Prueba técnica | Código, pruebas, README |
| Entrevista técnica en vivo | Cómo piensas un problema en voz alta |
| Diseño de sistemas (semi-senior) | Criterio para elegir piezas |
| Conductual o con el equipo | Cómo trabajas con otras personas |
| Oferta | Que aceptes |

**Pregunta al inicio cuántas etapas hay y qué evalúa cada una.** Es normal y te ahorra sorpresas.

---

## Lo que se busca en un perfil junior

No se espera que sepas todo. Se espera evidencia de cuatro cosas:

1. **Terminas lo que empiezas:** algo desplegado, con README, que otra persona pudo correr.
2. **Aprendes rápido:** una tecnología que no conocías y que dominaste para un proyecto.
3. **Comunicas:** explicas una decisión técnica a alguien que no estuvo ahí.
4. **Tienes criterio:** sabes por qué elegiste algo y qué alternativa descartaste.

Todo el módulo es cómo **mostrar** esas cuatro cosas, no cómo afirmarlas.

---

<!-- _class: lead -->

# Parte 1
## El CV de una página y el portafolio

---

## La estructura

```text
Nombre completo
Ciudad, correo, teléfono, GitHub, LinkedIn

Resumen (dos líneas): qué haces y qué buscas.

Experiencia o proyectos (lo más reciente primero)
  Puesto o proyecto, lugar, fechas
  - Viñeta de logro
  - Viñeta de logro

Habilidades técnicas (agrupadas)
Educación
```

Una página. Si no cabe, sobra algo.

---

## La viñeta de logro

**Verbo en pasado + qué hiciste + con qué + resultado verificable.**

| Antes | Después |
|-------|---------|
| Responsable del backend | Diseñé la API de reservas en .NET con 14 endpoints y pruebas de integración en cada uno |
| Conocimientos en SQL | Bajé de 4 s a 300 ms el reporte mensual con un índice compuesto, medido con EXPLAIN ANALYZE |
| Trabajo en equipo | Coordiné revisiones de código en cada pull request de un equipo de cuatro durante cuatro meses |

Las cifras son ejemplos de la forma. Las tuyas salen de tu proyecto.

<!--
Nota del instructor: insiste en que los números de la tabla son inventados para mostrar la forma. Pregunta: ¿qué pasa si alguien escribe "mejoré el rendimiento un 80 %" y le preguntan cómo lo midió? Que lo contesten ellos.
-->

---

## Resultados sin inventar números

Un proyecto escolar casi nunca tiene ventas. Sí tiene hechos verificables:

- **Tamaño:** endpoints, pantallas, tablas o módulos que construiste tú.
- **Calidad:** cuántas pruebas, si corren en CI.
- **Rendimiento:** un tiempo medido antes y después de un cambio.
- **Uso real:** un piloto, un cliente pequeño, la propia escuela.
- **Entrega:** está desplegado y tiene URL.

**Un número que no puedes defender es peor que no poner número.**

---

## Lo que sobra

- Datos que la vacante no pide: estado civil, edad, CURP.
- Tecnologías que no podrías defender: **todo lo que escribes es una pregunta posible**.
- Adjetivos: "proactivo", "apasionado", "orientado a resultados".
- Faltas de ortografía.
- La segunda página.

---

## Ejercicio en vivo: reescribe tres viñetas

En 5 minutos, convierte cada una en viñeta de logro. Puedes suponer los hechos que falten, pero dilos en voz alta.

1. "Participé en el desarrollo de una aplicación web."
2. "Manejo de bases de datos relacionales."
3. "Me encargué de las pruebas del sistema."

Después, dos personas leen la suya y el grupo pregunta: **¿cómo lo comprobarías?**

<!--
Nota del instructor: 5 minutos de escritura y 5 de lectura. Si alguien escribe un número, pregunta cómo lo midió. La lección no es la redacción: es que cada viñeta se tiene que poder defender.
-->

---

## El portafolio

Quien entrevista va a abrir tu GitHub. Debe confirmar lo que dice tu CV.

- **Dos o tres repositorios fijados**, el de titulación primero.
- **README por repositorio:** qué resuelve, captura, cómo correrlo, qué decidiste y por qué.
- **Historial legible:** mensajes de commit que digan qué cambió.
- **Pruebas que corren** y, si se puede, CI en verde.
- **Una demo desplegada** vale más que tres párrafos.
- **LinkedIn coherente:** mismas fechas, mismos puestos.

---

<!-- _class: lead -->

# Parte 2
## Tu proyecto en dos minutos

---

## La plantilla

| Parte | Tiempo | Qué dice |
|-------|--------|----------|
| Problema | 20 s | Para quién es y qué dolor resuelve, sin tecnologías |
| Tu papel | 15 s | Qué hiciste tú, en primera persona |
| La solución | 45 s | Arquitectura en una frase y **la decisión más difícil**, con la alternativa descartada |
| Resultado | 20 s | Hechos: desplegado, lo usan, lo mediste |
| Qué harías distinto | 20 s | Una lección concreta |

Se ensaya en voz alta, con cronómetro, hasta que salga sin leer.

---

## Un ejemplo

```text
Problema:   Las recepcionistas agendaban por WhatsApp y se cruzaban citas
            dos o tres veces por semana.
Mi papel:   Éramos tres; yo hice la API y el modelo de datos.
Solución:   Una API en .NET con PostgreSQL y un panel en React. Lo difícil fue
            que dos personas no apartaran el mismo horario a la vez: lo resolví
            con un índice único en la base, porque revisar en el código tiene
            una carrera.
Resultado:  Lo usa la clínica desde marzo y no se ha cruzado ninguna cita.
Distinto:   Escribiría las pruebas de concurrencia desde el principio.
```

<!--
Nota del instructor: léelo en voz alta con cronómetro; debe durar cerca de un minuto y medio. Pregunta qué parte fue la más convincente. Casi siempre es la decisión técnica con su alternativa descartada: eso es lo que separa a quien construyó de quien estuvo presente.
-->

---

## Las preguntas que vienen después

Prepara una respuesta corta para cada una:

- ¿Por qué esa base de datos y no otra?
- ¿Qué parte hiciste tú y qué parte otra persona?
- ¿Cómo lo probaste? ¿Qué pasa si se cae la base?
- ¿Qué fue lo que más te costó?
- Con diez veces más usuarios, ¿qué se rompe primero?

**En proyectos de equipo:** "nosotros" para el contexto, "yo" para tus acciones.

---

## Ejercicio en vivo: tu guion

**10 minutos.** Escribe tu guion con la plantilla, para tu proyecto de titulación o el más reciente.

- Una línea por parte, no párrafos.
- La decisión técnica difícil, con la alternativa que descartaste.
- Un resultado que puedas comprobar.

Luego cuéntalo en voz alta a la persona de al lado. Ella toma el tiempo y te dice en qué parte se perdió.

---

<!-- _class: lead -->

# Parte 3
## La entrevista conductual

---

## Por qué preguntan por el pasado

Las preguntas conductuales empiezan con **"cuéntame de una vez que..."**.

- Lo que hiciste predice mejor lo que harás que lo que dices que harías.
- Por eso una respuesta hipotética ("yo haría...") no contesta la pregunta.
- Y por eso una historia inventada se cae en las preguntas de seguimiento.

---

## STAR

| Letra | Qué cuenta | Peso |
|-------|-----------|------|
| **S**ituación | Dónde y cuándo, en una o dos frases | poco |
| **T**area | Qué te tocaba resolver a ti | poco |
| **A**cción | Qué hiciste tú, paso por paso, y por qué | la mitad |
| **R**esultado | Qué pasó, con hechos, y qué aprendiste | lo justo para cerrar |

El error más común es gastar un minuto en la situación y diez segundos en la acción.

---

## Un ejemplo: un desacuerdo en el equipo

```text
S: A dos semanas de la entrega, un compañero quería reescribir el frontend
   con otro framework.
T: Yo coordinaba las entregas y tenía que decidir sin romper el equipo.
A: Le pedí que me mostrara qué problema resolvía el cambio. Hicimos la lista
   de lo que faltaba y estimamos ambas opciones. La reescritura no cabía.
   Le propuse anotarla para después de la entrega y que mientras tanto
   arreglara la parte del código que más le molestaba.
R: Entregamos a tiempo, su refactor quedó en la versión final, y aprendí a
   discutir con estimaciones en vez de opiniones.
```

---

## Tu banco de historias

Seis historias bien preparadas cubren casi todas las preguntas.

| Historia | Sirve para preguntas sobre |
|----------|---------------------------|
| Un conflicto o desacuerdo | equipo, comunicación |
| Un error tuyo | responsabilidad, aprendizaje |
| Algo que aprendiste rápido | adaptabilidad, autonomía |
| Un plazo apretado | presión, prioridades |
| Una decisión técnica que defendiste | criterio, influencia |
| El logro del que más te enorgulleces | motivación, impacto |

---

## Señales que restan

- **Culpar a otros:** siempre fue el equipo, el profesor o el cliente.
- **Todo en plural:** a los tres minutos no se sabe qué hiciste tú.
- **Sin resultado:** la historia termina en la acción.
- **Hipotética:** "yo haría..." cuando pidieron un hecho.
- **Inventada:** la segunda pregunta de seguimiento la desarma.

---

## Ejercicio en vivo: ¿qué le falta?

Para cada respuesta, ¿qué letra de STAR falta o qué señal resta?

1. "Siempre trato de escuchar a todos y llegar a acuerdos."
2. "El equipo estaba atrasado porque nadie hacía su parte, así que el profesor nos dio más tiempo."
3. "Teníamos que migrar la base. Investigué cómo hacerlo con scripts versionados, los probé en una copia y los corrimos un sábado."
4. "Yo en ese caso hablaría con mi líder para entender la prioridad."

<!--
Nota del instructor: respuestas esperadas. 1: no es una historia, no hay situación ni hechos. 2: culpa a otros y el resultado no viene de una acción propia. 3: le falta el resultado (¿funcionó?, ¿qué aprendió?). 4: es hipotética.
-->

---

## Las preguntas difíciles

- **"¿Cuál es tu mayor debilidad?"** Una real, no central para el puesto, y qué haces para mejorarla.
- **"¿Por qué te quieres ir de tu trabajo actual?"** Lo que buscas, no lo que te molesta. Nunca hables mal de un empleador.
- **"¿Cuáles son tus pretensiones salariales?"** Un rango investigado en fuentes fechadas. Lo vemos en el módulo 6.

---

## Tus preguntas al final

"¿Tienes preguntas para nosotros?" siempre llega. No preguntar nada parece desinterés.

- ¿Cómo es un día normal en este puesto?
- ¿Cómo se mide el éxito en los primeros tres meses?
- ¿Cómo se revisa el código? ¿Hay pruebas automáticas?
- ¿Quién sería mi persona de referencia al principio?
- ¿Qué es lo más difícil del equipo ahora mismo?

---

## Después de cada entrevista

En los primeros quince minutos, anota en tu bitácora:

```text
Fecha / empresa / etapa:
Qué preguntaron (lo más literal que recuerdes):
Qué contesté bien:
Dónde me atoré y qué debí decir:
Qué estudio antes de la siguiente:
```

Si te rechazan, pide retroalimentación. No siempre la dan; cuando la dan, vale oro.

---

## La práctica de hoy

**En tríos, tres rondas de 12 minutos.** En cada ronda rotan los papeles:

- **Candidato:** cuenta su proyecto en dos minutos, contesta dos preguntas de seguimiento y una conductual.
- **Entrevistador:** cronometra y hace las preguntas.
- **Observador:** califica con la rúbrica de la práctica y da retroalimentación al final.

La rúbrica y las preguntas están en la práctica del módulo.

<!--
Nota del instructor: 36 minutos de práctica más 10 de cierre en grupo. Pide que al final cada trío comparta la observación que más les sirvió. Si alguien no tiene proyecto propio, que use el de titulación o uno del curso de Dev Notes.
-->

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas

- Un CV de una página con viñetas de logro que puedes defender
- Tu proyecto contado en dos minutos, con la decisión técnica difícil
- Un banco de seis historias en formato STAR

**Libros:** *C# Interview Guide* (Semenenko, 2024), caps. 1, 2 y 8. *The Software Engineer's Guidebook* (Orosz, 2024), primera parte.

**Siguiente módulo:** complejidad y estructuras lineales.
