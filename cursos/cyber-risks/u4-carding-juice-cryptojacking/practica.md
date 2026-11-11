# Práctica - U4 - Un día de viaje, tres ataques

**Riesgos actuales en ciberseguridad**
**Tiempo estimado:** 2 horas, individual con discusión en grupo (en foro o en sesión).

---

## Objetivo

Poner a trabajar lo visto en la unidad sobre una situación de todos los días: explicar, con tus palabras y con argumentos de las notas, de qué forma el carding, el juice jacking y el cryptojacking dañan a una persona común y qué tendría que hacer para protegerse. Después, contrastar tu análisis con el de tres compañeros y defender o ajustar tu postura.

---

## Antes de empezar

- Lee las tres notas de la unidad: [10-carding.md](../../../12-seguridad/02-riesgos/10-carding.md), [11-juice-jacking.md](../../../12-seguridad/02-riesgos/11-juice-jacking.md) y [12-cryptojacking.md](../../../12-seguridad/02-riesgos/12-cryptojacking.md).
- Ten abiertas tres tablas, porque las vas a necesitar: "De dónde salen los datos" (nota 10), los pasos de "Cómo protegerse" (nota 11) y "Cómo llega a un dispositivo" (nota 12).
- Si usas una fuente que no esté en las notas, va al final de tu aportación en formato APA 7.

---

## El caso

Una consultora viaja cada semana entre dos ciudades. Un jueves cualquiera le pasa esto:

1. **7:40, sala de espera del aeropuerto.** Su teléfono marca 4 % de batería. No trae cargador, así que usa el cable que cuelga de un quiosco de carga gratuita junto a la puerta de abordaje. Al conectarlo, el teléfono le pregunta si confía en el equipo; acepta sin leer porque ya están llamando a su vuelo.
2. **8:05, todavía en la sala.** Desde la Wi-Fi gratuita del aeropuerto paga en línea el hotel de esa noche. No es la agencia de siempre: la encontró en un anuncio que prometía un descuento grande. Captura el número, el vencimiento y el código de seguridad, y marca "guardar tarjeta para la próxima".
3. **22:30, en el hotel.** Su laptop tarda en abrir el correo, el ventilador no se detiene y la batería le dura la mitad que de costumbre. Por la tarde instaló una extensión del navegador "para descargar videos" que alguien recomendó en un foro.
4. **Tres semanas después.** Su estado de cuenta muestra varios cargos de unos cuantos pesos, todos de madrugada, y después una compra fuerte en una tienda de electrónica que no conoce.

El caso es ficticio, pero cada detalle corresponde a algo que estudiaste en la unidad.

---

## Momento 1 - Tu aportación

Escribe un comentario de **400 a 600 palabras** dividido en tres secciones con estos títulos: **Carding**, **Juice jacking** y **Cryptojacking**. En cada sección responde tres cosas:

1. **Cómo afecta al usuario.** Qué perdió o puede perder la consultora (dinero, datos, tiempo, equipo, tranquilidad) y en qué momento del caso se abrió la puerta.
2. **Cómo debió protegerse.** Al menos tres medidas concretas, y por qué cada una corta el ataque justo en ese punto.
3. **Qué hacer ahora.** Los pasos para contener el daño que ya ocurrió.

### Preguntas guía

No tienes que contestarlas todas; úsalas para pensar.

**Carding**

- ¿Qué indican los cargos pequeños de madrugada antes de la compra grande?
- ¿Cuál de los canales de obtención de la nota 10 pudo intervenir: el anuncio, la red pública o un comercio comprometido? ¿Puede haber más de uno?
- ¿Por qué no conviene dejar guardada la tarjeta en un sitio que casi no se usa?
- ¿A quién acude si su banco no le resuelve la aclaración?

**Juice jacking**

- ¿Qué cambia si hubiera conectado su propio cargador a un contacto de corriente?
- ¿Qué debió responder al aviso del teléfono, y por qué ese aviso por sí solo ya no basta después de ChoiceJacking?
- Un cable de solo carga, ¿la protege? ¿Carga más rápido, como se suele decir?

**Cryptojacking**

- ¿Qué síntomas de la nota 12 reconoces en la laptop?
- ¿Por qué la extensión es sospechosa, y qué revisarías primero para confirmar o descartar la sospecha?
- Si el mismo minero estuviera en un servidor de su empresa en la nube, ¿qué otro costo aparecería y qué pregunta tendría que hacerse el área de seguridad?

**Una regla para las tres secciones:** cada afirmación técnica se apoya en lo visto en la unidad. "Es peligroso" no basta; "es peligroso porque el teléfono usa el mismo conector para cargarse y para pasar datos" sí.

---

## Momento 2 - Réplica a tres compañeros

Comenta la aportación de **tres compañeros**, de preferencia a quienes nadie haya respondido todavía. Cada réplica lleva entre 100 y 150 palabras y tres partes:

1. **Postura.** Di si estás de acuerdo, en desacuerdo, o de acuerdo con matices.
2. **Comparación.** Señala una medida que tu compañero propuso y tú no, o al revés, y explica cuál de las dos detiene antes el ataque.
3. **Corrección o complemento.** Si encuentras algo impreciso, corrígelo con el dato de la nota y di de cuál nota sale. Errores frecuentes que vale la pena atrapar: que el BIN son los 16 dígitos de la tarjeta, que un cable de solo carga siempre carga más rápido, que las criptomonedas son anónimas o que el cryptojacking solo ocurre en el navegador.

Una opinión personal no es suficiente: compara lo que tú encontraste con lo que presenta tu compañero. Cuida el tono: se discuten las ideas, no a las personas.

---

## Lo que se entrega

| Momento | Qué | Extensión |
|---|---|---|
| 1 | Comentario con las tres secciones tituladas, cada una con afectación, protección y contención | 400 a 600 palabras |
| 2 | Tres réplicas con postura, comparación y corrección o complemento | 100 a 150 palabras cada una |
| Los dos | Referencias en APA 7 si usaste fuentes fuera de las notas | Al final |

---

## Cómo se evalúa

| Criterio | Qué se busca | Puntos |
|---|---|---|
| Análisis de la afectación | En las tres secciones identifica qué pierde el usuario y en qué momento del caso se abrió la puerta | 3 |
| Medidas de protección y contención | Medidas concretas y correctas para cada ataque, cada una con su porqué, más los pasos para contener el daño | 3 |
| Réplicas | Tres réplicas con postura clara, comparación y argumentos tomados de la unidad, no solo opinión | 2.5 |
| Precisión técnica | Sin errores de concepto; los datos llevan su fuente | 1 |
| Redacción y etiqueta | Ortografía, claridad, extensión respetada y trato respetuoso | 0.5 |
| **Total** | | **10** |
