# IA, ética y criterio profesional

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/ai-and-society/](https://academy.raptorcloud.dev/ai-and-society/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** ninguno. No hay código en el curso. Leer en inglés ayuda: salvo uno, los libros base están en inglés, y por eso cada clase resume lo que necesitas de ellos.
**Objetivo:** razonar con criterio sobre sistemas de IA: qué pueden y qué no, sesgo y equidad, transparencia, responsabilidad, privacidad y regulación, y llevarlo a decisiones de tu trabajo.
**Formato:** 6 módulos de 90 minutos (9 h). Cada módulo trae clase, quiz, práctica, tarea y glosario.
**Para quién:** cualquier carrera. Es el curso abierto de la academia para quien no programa, y el complemento ético de [Aplicaciones con LLM](../llm-applications/README.md) y de [Vibe coding profesional](../ai-assisted-coding/README.md).

> **Las leyes cambian.** Cada norma que cita el curso (el Reglamento de IA de la Unión Europea, la ley mexicana de datos personales, los marcos del NIST) lleva la fecha del texto que se consultó. La regulación de la IA se mueve rápido: antes de aplicar algo en un caso real, confirma el texto vigente.

---

## Módulo 1. Qué es, y qué no es, la IA (90 min)

> Entre la prensa y la realidad hay una distancia que se puede medir.

**Material:** [clase](m01-what-is-ai/slides.md), [práctica](m01-what-is-ai/practica.md), [tarea](m01-what-is-ai/tarea.md), [glosario](m01-what-is-ai/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** de dónde viene la IA, qué hace bien un modelo y dónde se rompe: la barrera del significado.
- [ ] **Lectura:** [`12-seguridad/02-riesgos/13-inteligencia-artificial.md`](../../12-seguridad/02-riesgos/13-inteligencia-artificial.md) (la misma herramienta para atacar y defender).
- [ ] **Práctica:** diez titulares de prensa sobre IA, clasificados en hecho, exageración o falso, con argumento.
- [ ] **Tarea:** auditar una promesa de IA de un producto real con la lista del módulo.
- **Libros:** *Inteligencia artificial* (Mitchell, 2024, en español), partes I y V. *Artificial Unintelligence* (Broussard, 2018), parte I (caps. 1 a 4). *A Citizen's Guide to Artificial Intelligence* (Zerilli et al., 2021), cap. 1.

**Al terminar puedes:** separar una capacidad real de una promesa de marketing.

---

## Módulo 2. Sesgo y equidad (90 min)

> Hay definiciones de equidad que no se pueden cumplir al mismo tiempo: hay que elegir, y decirlo.

**Material:** [clase](m02-bias-fairness/slides.md), [práctica](m02-bias-fairness/practica.md), [tarea](m02-bias-fairness/tarea.md), [glosario](m02-bias-fairness/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** de dónde sale el sesgo, cómo se mide la equidad y por qué sus definiciones chocan.
- [ ] **Práctica:** con un conjunto ficticio de solicitudes de crédito, medir dos definiciones de equidad y mostrar que no se pueden cumplir a la vez.
- [ ] **Tarea:** decir qué equidad persigue un sistema real y a quién deja fuera.
- **Libros:** *AI Fairness* (Leben, 2025), caps. 1 a 3, 5 y 8. *A Citizen's Guide to Artificial Intelligence*, cap. 3. *Responsible AI in the Enterprise* (Masood y Dawe, 2023), cap. 2.

**Al terminar puedes:** explicar qué tipo de equidad persigue un sistema y a quién deja fuera.

---

## Módulo 3. Transparencia y explicación (90 min)

> Una decisión que no se puede explicar a quien la recibe no se puede impugnar.

**Material:** [clase](m03-transparency/slides.md), [práctica](m03-transparency/practica.md), [tarea](m03-transparency/tarea.md), [glosario](m03-transparency/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** cajas negras, explicabilidad, y a quién se le debe una explicación.
- [ ] **Práctica:** explicar la decisión de un modelo sencillo a la persona afectada, en un párrafo sin tecnicismos.
- [ ] **Tarea:** redactar la explicación y la vía de impugnación de una decisión automatizada.
- **Libros:** *A Citizen's Guide to Artificial Intelligence*, cap. 2. *Responsible AI in the Enterprise*, caps. 3 y 7.

**Al terminar puedes:** exigir y redactar una explicación útil de una decisión automatizada.

---

## Módulo 4. Responsabilidad, juicio humano y trabajo (90 min)

> Cuando un sistema falla, alguien responde. La pregunta es quién, y se decide antes.

**Material:** [clase](m04-responsibility-work/slides.md), [práctica](m04-responsibility-work/practica.md), [tarea](m04-responsibility-work/tarea.md), [glosario](m04-responsibility-work/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** cómo juzgamos a las máquinas frente a las personas, responsabilidad legal, control humano y el efecto en el empleo.
- [ ] **Práctica:** debate sobre un caso de responsabilidad, con roles asignados.
- [ ] **Tarea:** proponer por escrito quién responde y quién supervisa en un sistema con IA.
- **Libros:** *How Humans Judge Machines* (Hidalgo, 2021), caps. 1 a 3 y 7. *A Citizen's Guide to Artificial Intelligence*, caps. 4, 5 y 9. *Robot-Proof* (Aoun, 2024), caps. 1 y 2.

**Al terminar puedes:** proponer quién responde y quién supervisa en un sistema con IA.

---

## Módulo 5. Privacidad (90 min)

> Quitar el nombre no hace anónimo un conjunto de datos.

**Material:** [clase](m05-privacy/slides.md), [práctica](m05-privacy/practica.md), [tarea](m05-privacy/tarea.md), [glosario](m05-privacy/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** datos personales en el entrenamiento y en el uso, reidentificación, y la idea de la privacidad diferencial.
- [ ] **Lectura:** [`12-seguridad/05-legislacion/12-proteccion-datos-personales.md`](../../12-seguridad/05-legislacion/12-proteccion-datos-personales.md).
- [ ] **Práctica:** reidentificar a una persona en un conjunto "anónimo" ficticio cruzándolo con otro.
- [ ] **Tarea:** revisar un caso de compartir o entrenar con datos y dar el veredicto con sus riesgos.
- **Libros:** *A Citizen's Guide to Artificial Intelligence*, cap. 6. *Hands-On Differential Privacy* (Cowan, Pereira y Shemate, 2024), caps. 1 y 2, solo los conceptos, y del cap. 11 las secciones de *record linkage* y de las debilidades de *k-anonymity*.

**Al terminar puedes:** detectar un riesgo de privacidad antes de compartir o entrenar con datos.

---

## Módulo 6. Regulación y uso profesional de la IA (90 min)

> Usar IA en el trabajo también es una decisión de seguridad y de propiedad intelectual.

**Material:** [clase](m06-regulation-use/slides.md), [práctica](m06-regulation-use/practica.md), [tarea](m06-regulation-use/tarea.md), [glosario](m06-regulation-use/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** el panorama regulatorio, los algoritmos en el gobierno, y una política de uso de IA para un equipo.
- [ ] **Lectura:** [`11-vibe-coding/03-ai-workflow.md`](../../11-vibe-coding/03-ai-workflow.md). [`11-vibe-coding/11-etica-y-uso-responsable.md`](../../11-vibe-coding/11-etica-y-uso-responsable.md).
- [ ] **Práctica:** escribir la política de uso de IA de un equipo de software: qué datos no se comparten, qué se revisa y quién firma.
- [ ] **Tarea:** defender esa política ante las objeciones más probables.
- **Libros:** *A Citizen's Guide to Artificial Intelligence*, caps. 8 y 10. *Moral Codes* (Blackwell, 2024), caps. 1 a 3 y 11.

**Al terminar puedes:** escribir y defender las reglas de uso de IA de tu equipo.

---

## Proyecto final

Evaluar un sistema de IA público con la guía del curso: qué decide, a quién afecta, qué sesgo puede tener, cómo se explica, quién responde y qué datos usa. Cada módulo aporta una sección del informe; la tarea del módulo 6 lo cierra con las reglas de uso que propondrías.

## Libros base, por capítulo

| Libro | Capítulos que usa el curso | Módulos |
|---|---|---|
| *A Citizen's Guide to Artificial Intelligence* (Zerilli et al., 2021) | 1 *What Is Artificial Intelligence?*; 2 *Transparency*; 3 *Bias*; 4 *Responsibility and Liability*; 5 *Control*; 6 *Privacy*; 8 *Algorithms in Government*; 9 *Employment*; 10 *Oversight and Regulation* | 1 a 6 (columna del curso) |
| *Inteligencia artificial* (Mitchell, 2024) | Parte I (caps. 1 a 3); parte V (caps. 14 a 16) | 1 |
| *Artificial Unintelligence* (Broussard, 2018) | Parte I (caps. 1 a 4) | 1 |
| *AI Fairness* (Leben, 2025) | 1 *The Problem of Measuring Fairness*; 2 *Theories of Fairness*; 3 *Demo: AI for Mortgages*; 5 *Equal Treatment*; 8 *Fairness versus Accuracy* | 2 |
| *Responsible AI in the Enterprise* (Masood y Dawe, 2023) | 2 *Algorithms Gone Wild*; 3 *Opening the Algorithmic Black Box*; 7 *Interpretability Toolkits and Fairness Measures* | 2 y 3 |
| *How Humans Judge Machines* (Hidalgo, 2021) | 1 *The Ethics of Artificial Minds*; 2 *Unpacking the Ethics of AI*; 3 *Judged by Machines*; 7 *Liable Machines* | 4 |
| *Robot-Proof* (Aoun, 2024) | 1 *Fears of an AI Future*; 2 *Working Intelligence* | 4 |
| *Hands-On Differential Privacy* (Cowan et al., 2024) | 1 *Welcome to Differential Privacy*; 2 *Differential Privacy Fundamentals*; 11 *Protecting Your Data Against Privacy Attacks* (parcial) | 5 |
| *Moral Codes* (Blackwell, 2024) | 1 *Are You Paying Attention?*; 2 *Would You Like Me to Do the Rest? When AI Makes Code*; 3 *Why Is Code Not Like AI?*; 11 *How Can Stochastic Parrots Help Us Code?* | 6 |

Los capítulos se citan por su número y título en el índice del libro; el curso no reproduce su texto.

### Lecturas de profundización

Para quien quiera seguir después del curso, de la colección *The Human Side of AI* (MIT Press):

| Libro | Capítulos | Con qué módulo conecta |
|---|---|---|
| *Awkward Intelligence* (Zweig, 2022) | 1 *Robo-judges ... with Poor Judgment*; 8 *Algorithms, Discrimination, and Ideology*; 9 *How to Stay in Control*; 10 *Who Wants Machines Making Decisions about People, Anyway?* | 2 y 4 |
| *Machines We Trust* (Pelillo y Scantamburlo, 2021) | 4 *The Issue of Bias*; 5 *Adjudicating with Inscrutable Decision Rules* | 2 y 3 |
| *AI and Humanity* (Nourbakhsh y Keating, 2019) | 3 *Labor and the Self*; 5 *Surveillance, Information, Network* | 4 y 5 |
| *The New Fire* (Buchanan e Imbrie, 2022) | 1 *Data*; 4 *Failure*; 8 *Lying* | 1 y 6 |
| *The Silicon Shrink* (Oberhaus, 2025) | 6 *Who Diagnoses the Diagnosis?* | 3 y 4 |
| *The Smart Wife* (Strengers y Kennedy, 2020) | 6 *Bitches with Glitches*; 8 *The Smart Wife Reboot* | 2 |
| *Collaborative Intelligence* (Lane y Sethumadhavan, 2024) | 2 *Automatic Authorities: Power and AI*; 4 *AI Audits: Who, When, How ... or Even If?* | 3 y 6 |
| *The Car That Knew Too Much* (Bonnefon) | 7 *"Programmed to Kill"*; 18 *The Uber Accident* | 4 |
| *Cyber Republic* (Zarkadakis, 2020) | 3 *A World without Work*; 6 *Citizen Assemblies* | 4 y 6 |

## Cómo se trabaja

- **Clase y lectura primero, práctica después.** La práctica de cada módulo termina siempre con un paso de "verlo fallar": romper algo a propósito (un criterio de equidad, una explicación, un conjunto "anónimo") y leer qué se rompió.
- **Todo lo ficticio dice que es ficticio.** Los conjuntos de datos, las empresas y los casos inventados para enseñar llevan el rótulo "Ejemplo ilustrativo" o "Caso hipotético". Los casos reales llevan su fuente primaria y su fecha.
- **Inteligencia artificial como apoyo.** Puedes usarla para estudiar. Di para qué la usaste, verifica lo que te entrega contra fuentes (inventa referencias con toda tranquilidad) y no le pases datos personales ni sensibles.

---

*Rogelio Arriaga Gonzalez*
