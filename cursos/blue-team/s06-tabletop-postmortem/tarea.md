# Tarea - S06 - El postmortem del ejercicio

**Defensa y respuesta a incidentes**
**Tiempo estimado:** 3 horas

---

## Parte 1 - Lecturas (1 hora)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/12-seguridad/06-blue-team/03-respuesta-a-incidentes.md` | Completa | 25 min |
| Google, *Site Reliability Engineering*, cap. 15 "Postmortem Culture: Learning from Failure" (sre.google/sre-book/postmortem-culture/) | Completo | 20 min |
| NIST SP 800-84, sección 4 "Tabletop Exercises" y el informe posterior de ejemplo (apéndice A.3) | Secciones 4.1 a 4.6 y A.3 | 15 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Cybersecurity Tabletop Exercises* (Robert Lelewski y John Hollenberger, No Starch Press, 2025) | Parte I, caps. 1 a 5 (por qué, planeación, desarrollo, facilitación y qué hacer con lo aprendido). Parte II, caps. 6 a 8 (escenarios de ejemplo) |
| *Intelligence-Driven Incident Response* (Rebekah Brown y Scott J. Roberts, O'Reilly, 2023) | Cap. 10 (inteligencia estratégica) y cap. 11 (construir un programa de inteligencia): cómo pasar de un incidente a un programa |

---

## Parte 2 - Entregable: el postmortem (2 horas)

Con las hojas de su ejercicio (injects, línea de tiempo, evaluación y respuestas de "verlo fallar"), escriban `postmortem-ejercicio.md` en el repo de su equipo. **Es el postmortem del ejercicio de mesa**: el incidente es el ejercicio, y los "hechos" son lo que pasó en la mesa.

### 1. Resumen (máximo 5 líneas)

Qué se ensayó, con qué escenario (**caso hipotético**), cuántas personas, cuánto duró y qué se concluyó en una frase.

### 2. Impacto

El "impacto" de un ejercicio es lo que **descubrió**: qué partes del plan funcionaron y cuáles no. Una tabla con los cinco objetivos (O1 a O5), su calificación (lograda, parcial, no lograda) y la evidencia de las hojas.

### 3. Línea de tiempo

Tabla con **reloj real, reloj de escenario, qué se decidió y fuente** (la hoja de decisión del inject). Al menos las siete decisiones principales.

### 4. Causas contribuyentes (sin culpables)

Para cada objetivo no logrado o parcial: **qué condiciones del plan, de la información o del proceso** permitieron el resultado. Reglas de redacción:

- Describan **condiciones**, no personas: "el plan no tenía el contacto vigente", no "alguien no actualizó el plan".
- Si usan un nombre de rol, que sea para describir **qué información le faltó**, no qué hizo mal.
- Eviten "faltó capacitación" como única causa: expliquen qué en el sistema permitió que la falta importara.

### 5. Qué salió bien

Mínimo tres cosas que **hay que conservar**, con evidencia.

### 6. Acciones de seguimiento

Una tabla con **mínimo ocho acciones**. Cada una con estas cinco columnas:

| Qué | Quién (rol) | Cuándo | Cómo se verifica que está hecha | Objetivo al que responde |
|---|---|---|---|---|
| Desmontar el NAS del servidor y montarlo solo durante el respaldo | Líder técnico | AAAA-MM-DD | Con el servidor comprometido en una prueba, el NAS no es accesible | O4 |
| ... | ... | ... | ... | ... |

Condiciones de calidad (criterio de este curso):

- Cada acción empieza con un **verbo** y describe un cambio concreto.
- La fecha es **una fecha**, no "pronto" ni "próximo trimestre".
- La verificación es algo que **se puede mostrar**.
- Al menos **dos** acciones de datos o detección (por ejemplo, retener bitácoras más tiempo o alertar inicios de sesión de la VPN fuera de horario), conectadas con las sesiones 2 y 5.
- Al menos **una** acción que actualice el plan de respuesta, con el cambio exacto (por ejemplo, reemplazar el paso 3 "apagar" por "aislar de la red y conservar memoria").

### 7. Lo que no sabemos

Dudas del ejercicio que quedaron abiertas y qué haría falta para contestarlas.

---

## Parte 3 - Informe posterior (opcional, suma puntos extra)

Un informe posterior de una página, en el formato del NIST (SP 800-84, apéndice A.3): introducción con propósito, objetivos y participantes; observaciones con su recomendación. Explicen **en dos líneas** en qué se distingue de su postmortem.

---

## Criterio de aceptación

Su tarea se considera completa cuando:

- [ ] Están las siete secciones y la línea de tiempo cita la hoja de decisión de cada inject.
- [ ] Las causas describen condiciones, **no personas**.
- [ ] Hay al menos ocho acciones, cada una con **responsable (rol), fecha y verificación**.
- [ ] Hay al menos dos acciones de detección o datos y una de actualización del plan.
- [ ] Cada objetivo no logrado tiene al menos una acción que lo atienda.
- [ ] El documento aclara que el escenario es un caso hipotético.
- [ ] Ningún dato real de personas o sistemas ajenos aparece.

---

## Cómo se evalúa

| Criterio | Peso |
|---|---|
| Causas contribuyentes redactadas sin culpables y con evidencia | 30 % |
| Calidad de las acciones (qué, quién, cuándo, verificación) | 30 % |
| Línea de tiempo fiel a las hojas del ejercicio | 20 % |
| Conexión con lo visto en el curso (S1 a S5) | 10 % |
| Claridad de la redacción | 10 % |

<!-- Califiquen leyendo primero las causas: si aparece un nombre propio con un reproche, devuelvan el documento a reescribir antes de seguir. -->

---

## Proyecto final

Esta tarea es la última pieza del **proyecto final**: un laboratorio de detección con cinco reglas, cada una vista disparar, y el informe completo del incidente simulado (línea de tiempo, evidencia y postmortem). Junten sus entregables de las sesiones 2 a 6 en un solo repositorio, con un `README` que explique el orden.
