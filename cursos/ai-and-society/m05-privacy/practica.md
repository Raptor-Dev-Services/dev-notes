# Práctica - M5 - Reidentificar a una persona en un conjunto "anónimo"

**IA, ética y criterio profesional**
**Tiempo estimado:** 45 minutos en clase o 90 minutos por tu cuenta.

> **Estudio individual.** Esta actividad se resuelve por tu cuenta: guarda tu trabajo y evalúalo tú mismo con la solución y el checklist del final. Si llevas el curso en grupo, el instructor te indica cómo se entrega.

---

## Objetivo

Comprobar con tus propias manos que quitar el nombre no hace anónimo un conjunto de datos: cruzas una tabla "anónima" con otra que sí trae nombres, ves qué pasa cuando omites un dato del cruce, haces el conjunto más difícil de cruzar con k-anonimato y pruebas la idea de ruido con una dinámica de monedas.

**Aviso:** todos los datos de esta práctica son inventados. Los nombres, los códigos postales, las fechas y los motivos de consulta no corresponden a personas reales. No uses esta técnica con datos de personas reales sin autorización de quien tiene derecho a decidir.

---

## Antes de empezar

- Haber visto la clase del módulo 5.
- Una hoja de cálculo o papel y lápiz.
- Una moneda por persona, para el paso 6.

---

## Las dos tablas (Ejemplo ilustrativo)

### Tabla A. Atenciones de una clínica (publicada "sin nombres")

| Folio | Código postal | Nacimiento | Sexo | Motivo |
|---|---|---|---|---|
| A01 | 87300 | 1984-03-12 | F | Control de diabetes |
| A02 | 87300 | 1984-07-30 | F | Consulta general |
| A03 | 87310 | 1991-11-02 | M | Lesión de rodilla |
| A04 | 87310 | 1991-05-19 | M | Consulta general |
| A05 | 87320 | 1978-01-25 | F | Control de hipertensión |
| A06 | 87320 | 1978-01-25 | M | Consulta general |
| A07 | 87300 | 1990-09-08 | M | Atención por ansiedad |
| A08 | 87330 | 1985-04-14 | F | Consulta general |
| A09 | 87330 | 1985-04-14 | F | Consulta general |
| A10 | 87310 | 1969-12-03 | M | Control de colesterol |
| A11 | 87340 | 1996-06-21 | F | Consulta general |
| A12 | 87300 | 1984-03-12 | M | Consulta general |

### Tabla B. Padrón de socios de un club deportivo (con nombres)

| Nombre | Código postal | Nacimiento | Sexo |
|---|---|---|---|
| Elena Garza Rivas | 87300 | 1984-03-12 | F |
| Marta Ibarra Solís | 87300 | 1984-07-30 | F |
| Lucía Treviño Peña | 87300 | 1984-11-05 | F |
| Joaquín Salas Mena | 87310 | 1991-11-02 | M |
| Rodrigo Cantú Leal | 87310 | 1991-05-19 | M |
| Iván Delgado Ruiz | 87310 | 1969-12-03 | M |
| Paola Montes Cruz | 87320 | 1978-01-25 | F |
| Sergio Lara Ochoa | 87320 | 1978-01-25 | M |
| Diego Pérez Olvera | 87300 | 1990-09-08 | M |
| Camila Núñez Soto | 87330 | 1985-04-14 | F |
| Regina Flores Cano | 87330 | 1985-04-14 | F |
| Tomás Aguirre Vela | 87300 | 1984-03-12 | M |
| Andrea Rojas Lugo | 87340 | 1996-06-21 | F |
| Héctor Mora Bazán | 87340 | 1980-08-17 | M |
| Valeria Ríos Pons | 87320 | 1992-02-10 | F |
| Natalia Cervantes Ayala | 87310 | 1991-11-02 | F |

Los códigos postales son ilustrativos. Cada tabla tiene personas que no están en la otra: no todos los socios fueron a la clínica.

---

## Paso 1 - Cruza con los tres datos

Para cada folio de la tabla A, busca en la tabla B a la persona con el **mismo código postal, la misma fecha de nacimiento y el mismo sexo**. Llena:

| Folio | Candidatas de la tabla B (nombres) | ¿Una sola? |
|---|---|---|
| A01 | | |
| A02 | | |
| ... | | |
| A12 | | |

Contesta:

1. ¿Cuántos folios quedaron con una sola candidata? Esos son los **reidentificados**.
2. ¿Qué motivo de consulta le conoces ahora a cada una?
3. ¿Qué folios quedaron con más de una candidata y por qué?

---

## Paso 2 - Verlo fallar: omite un dato del cruce

Repite el cruce quitando un dato cada vez. Cuenta cuántos folios quedan con **una sola** candidata.

| Datos que usas | Folios con una sola candidata |
|---|---|
| Código postal, nacimiento y sexo (paso 1) | |
| Solo nacimiento y sexo (sin código postal) | |
| Solo código postal y sexo (sin nacimiento) | |
| Solo código postal y nacimiento (sin sexo) | |
| Solo código postal | |

Luego contesta:

1. ¿Qué dato aporta más a la identificación? ¿Cuál sobra para este conjunto?
2. Con el folio A03 sin usar el sexo, ¿a cuántas personas puedes confundir? ¿Quiénes son? (Pista: una socia nació el mismo día y vive en la misma zona.)
3. ¿Qué lección sacas para decidir qué columnas se publican?

**Lo que debes ver:** con los tres datos casi todos quedan identificados; sin uno de ellos, el número baja, pero sigue habiendo personas únicas. El nombre no hacía falta para identificar.

---

## Paso 3 - Mide el k-anonimato de la tabla A

Agrupa la tabla A por **código postal, nacimiento y sexo**. Cuenta cuántos folios hay en cada grupo. El **k** de la tabla es el tamaño del grupo más pequeño.

1. ¿Cuál es el k de la tabla A?
2. ¿Qué grupos tienen más de un folio?
3. En el grupo de más de un folio, ¿qué motivo tienen? ¿Qué se deduce de una persona que sabes que está en ese grupo, aunque no sepas cuál folio es? (Este es el límite del k-anonimato.)

---

## Paso 4 - Generaliza y suprime hasta lograr k = 2

Haz la tabla A menos precisa:

1. Cambia la **fecha de nacimiento** por la **década** (por ejemplo, 1984 pasa a "198x").
2. Cambia el **código postal** de 5 dígitos por sus **primeros 3 dígitos**.
3. **Quita la columna de sexo.**
4. Agrupa por (código postal de 3 dígitos, década) y cuenta.

Contesta:

1. ¿Qué grupo sigue teniendo un solo folio? ¿Qué debes hacer con él para lograr k = 2? (Pista: suprímelo.)
2. Con la tabla generalizada, intenta reidentificar el folio A05 en la tabla B usando solo código postal de 3 dígitos y década. ¿A cuántas personas puedes confundir?
3. ¿Qué perdió la tabla en detalle? Si fueras una investigadora, ¿qué pregunta ya no podrías contestar con ella?

**Lo que debes ver:** protegerse cuesta detalle. No hay una respuesta única: la decisión es cuánta protección y cuánta utilidad.

---

## Paso 5 - El ataque por diferencias

Ejemplo ilustrativo. Un grupo de 10 personas publica su calificación promedio: 8.1. Una persona (Ana) sale del grupo y se publica el promedio de las 9 restantes: 8.0.

1. Calcula la suma con Ana y la suma sin Ana.
2. ¿Cuál es la calificación de Ana?
3. Ahora suponte que cada promedio publicado trae un poco de ruido: 8.3 y 7.9. Repite la cuenta. ¿Obtienes algo útil?

---

## Paso 6 - La moneda: respuesta aleatorizada

En grupo (o con una moneda y 20 tiradas de papel si lo haces solo):

1. Cada persona piensa una pregunta delicada que tenga respuesta sí o no, **sin decirla**. Para esta dinámica, todos usan la misma pregunta de ejemplo: "¿Has copiado en un examen?".
2. Cada persona tira su moneda sin que nadie vea. Si sale cara, responde con la verdad. Si sale cruz, tira otra vez: cara responde "sí" y cruz responde "no", sin importar la verdad.
3. Se cuentan los "sí". Con q = fracción de "sí", la estimación de la proporción real es **2q - 0.5**.

Contesta:

1. ¿Qué proporción estimas?
2. Si alguien responde "sí", ¿puedes saber si copió? ¿Por qué no?
3. **Verlo fallar:** un grupo de 8 personas obtiene un solo "sí". Calcula la estimación. ¿Tiene sentido? Haz lo mismo con 7 "sí" de 8.

**Lo que debes ver:** en un grupo chico, el ruido domina y la estimación puede salir fuera de 0 a 1 (negativa o mayor que 1), lo que no puede ser una proporción. La protección individual se paga con exactitud, y se necesitan grupos grandes.

---

## Preguntas de cierre

- ¿Qué parte de lo que hiciste lo haría un programa en segundos con tablas grandes?
- Si tuvieras que publicar la tabla A para investigación, ¿qué harías antes?
- ¿Qué pregunta de las siete de "antes de compartir o entrenar" habría detenido la publicación de la tabla A original?

---

## Solución

**Paso 1.** Con los tres datos, 10 de los 12 folios quedan con una sola candidata:
A01 Elena Garza Rivas, A02 Marta Ibarra Solís, A03 Joaquín Salas Mena, A04 Rodrigo Cantú Leal, A05 Paola Montes Cruz, A06 Sergio Lara Ochoa, A07 Diego Pérez Olvera, A10 Iván Delgado Ruiz, A11 Andrea Rojas Lugo, A12 Tomás Aguirre Vela. Los folios A08 y A09 tienen las mismas tres características y coinciden con Camila Núñez Soto y Regina Flores Cano: con las dos candidatas y los dos folios no se puede decir cuál es cuál.

**Paso 2.** Folios con una sola candidata: los tres datos, 10; solo nacimiento y sexo, 10 (el código postal sobra en este conjunto); solo código postal y sexo, 2 (A06 y A11); solo código postal y nacimiento, 5; solo código postal, 0. Sin el sexo, A03 se confunde con Joaquín Salas Mena y con Natalia Cervantes Ayala (misma fecha y mismo código postal).

**Paso 3.** El k de la tabla A es 1. Solo A08 y A09 comparten grupo (k = 2 para ellos). Los dos tienen el mismo motivo ("Consulta general"), así que quien sepa que una persona está en ese grupo sabe su motivo.

**Paso 4.** Agrupando por código postal de 3 dígitos y década quedan 4 grupos: 873-198 con 5 folios (A01, A02, A08, A09, A12), 873-199 con 4 (A03, A04, A07, A11), 873-197 con 2 (A05, A06) y 873-196 con 1 (A10). Se suprime A10 para lograr k = 2. A05 coincide con Paola Montes Cruz y con Sergio Lara Ochoa: 2 personas.

**Paso 5.** Sumas: 8.1 × 10 = 81 y 8.0 × 9 = 72; Ana tiene 81 - 72 = 9. Con ruido: 8.3 × 10 - 7.9 × 9 = 11.9, que no es una calificación posible ni la de Ana.

**Paso 6.** Con 40 "sí" de 100: 2 × 0.4 - 0.5 = 0.30. Con 1 "sí" de 8: 2 × 0.125 - 0.5 = -0.25. Con 7 de 8: 2 × 0.875 - 0.5 = 1.25.

**Verificación de las tablas:** las dos tablas y todas las cifras de esta solución se verificaron con un programa que cruza las tablas y cuenta las candidatas.

---

## Checklist de autoevaluación

- [ ] Reidentificaste 10 de los 12 folios en el paso 1 y explicaste por qué A08 y A09 no se distinguen.
- [ ] Hiciste la tabla del paso 2 y nombraste el dato que sobra.
- [ ] Calculaste el k de la tabla A y lo explicaste.
- [ ] Lograste k = 2 en el paso 4 y anotaste qué detalle se perdió.
- [ ] Hiciste el cálculo del paso 5 con y sin ruido.
- [ ] Hiciste la dinámica del paso 6 y viste una estimación fuera de rango.

---

## Referencias

- Sweeney, L. (2002). k-anonymity: a model for protecting privacy. *International Journal of Uncertainty, Fuzziness and Knowledge-Based Systems*, 10(5), 557-570.
- Dwork, C., McSherry, F., Nissim, K. y Smith, A. (2006). Calibrating noise to sensitivity in private data analysis. *TCC 2006*, LNCS 3876, 265-284.
- Cowan, E., Pereira, M. y Shemate, M. (2024). *Hands-On Differential Privacy*, caps. 1, 2 y 11.
