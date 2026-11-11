# Seguridad de la información

Curso de **Raptor Dev Services Academy** y cuarta materia de la ruta de ciberseguridad. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/information-security/](https://academy.raptorcloud.dev/information-security/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio, en [`12-seguridad/04-seguridad-informacion/`](../../12-seguridad/04-seguridad-informacion/README.md).

**Prerrequisito:** las materias anteriores de la ruta, o experiencia equivalente: saber qué es una red, un sistema de información y un riesgo informático.
**Objetivo:** describir los principios de la seguridad de la información a partir de la normatividad internacional; relacionarlos con los procesos críticos de una organización mediante la cadena de valor; vincular los controles físicos y lógicos con los activos de información; establecer un plan de seguridad con marcos de referencia internacionales, y distinguir los planes de contingencia por sus objetivos.
**Formato:** 5 unidades de una semana, unas 5 horas de estudio cada una (25 h en total). Cada unidad trae clase, lecturas, quiz, práctica (una discusión o un caso), tarea (un reto) y glosario.
**Para quién:** quien ya entiende los riesgos y ahora tiene que organizar su gestión: responsables de TI que heredan la seguridad, auditores, consultores y desarrolladores que van a pasar una certificación ISO/IEC 27001.

---

## Por qué esta materia

Hoy no basta con proteger la información que sostiene la operación de las organizaciones públicas y privadas. Cada persona que valora su privacidad, sus datos personales y hasta la información de su ADN tiene algo que proteger, y las áreas de seguridad ya no son las únicas responsables. La disciplina dejó de ser la administración de un firewall: abarca planeación estratégica, cumplimiento legal, auditoría, matemáticas, tecnología y personas. Esta materia ordena esa complejidad en un sistema: qué se protege, contra qué, con qué controles, cómo se gestiona y cómo se sobrevive a un desastre.

## Cómo se trabaja cada semana

| Días | Qué se hace |
|---|---|
| Lunes a miércoles | Clase y lecturas; primera intervención en la práctica |
| Jueves a domingo | Quiz; tarea; respuestas a tres compañeros en la práctica |
| Domingo | Entrega de todo lo de la semana |

## Cómo se evalúa

| Unidad | Tarea (reto) | Práctica (discusión) | Quiz | Total |
|---|---|---|---|---|
| 1 a 5, cada una | 7.5 | 7.5 | 5 | 20 |
| **Materia** | **37.5** | **37.5** | **25** | **100** |

Se acredita con 60 de 100 y entregando al menos el 90 % de las actividades. Toda afirmación que no sea propia lleva su fuente en APA 7, y los trabajos se entregan en Arial 12 con interlineado de 1.5, con introducción, desarrollo, conclusiones personales y referencias.

---

## Unidad 1. Introducción a la seguridad de la información (5 h)

> Antes de proteger hay que saber qué vale, contra qué y cuánto cuesta no hacerlo.

**Material:** [clase](u1-security-fundamentals/slides.md), [práctica](u1-security-fundamentals/practica.md), [tarea](u1-security-fundamentals/tarea.md), [glosario](u1-security-fundamentals/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** la información como poder; activo, tríada, vulnerabilidad, amenaza y su fuente, incidente, impacto, control y riesgo; NIST e ISO; el SGSI y la gestión de riesgos de ISO/IEC 27005.
- [ ] **Lectura:** [`01-principios-y-conceptos.md`](../../12-seguridad/04-seguridad-informacion/01-principios-y-conceptos.md), [`02-normas-internacionales.md`](../../12-seguridad/04-seguridad-informacion/02-normas-internacionales.md), [`03-sgsi-iso-27001.md`](../../12-seguridad/04-seguridad-informacion/03-sgsi-iso-27001.md).
- [ ] **Práctica:** debate sobre censura y control de contenidos en internet: ¿deben los gobiernos y las empresas vigilar lo que hacemos en línea?
- [ ] **Tarea:** mapa conceptual que conecte la tríada, las amenazas y los controles con los procesos y los activos de una organización.
- **Fuentes base:** Harkins, *Managing Risk and Information Security* (2016), pp. 1-16; Lundgren y Möller, *Defining Information Security* (2019); NIST SP 800-30 (2002) y Rev. 1 (2012); ISO/IEC 27000, 27001 y 27005.

**Al terminar puedes:** explicar con precisión los conceptos fundamentales de la seguridad de la información y describir cómo se establece un SGSI y se evalúa el riesgo según la familia ISO/IEC 27000.

---

## Unidad 2. Seguridad de la información en las organizaciones (5 h)

> Si un proyecto no contribuye a los objetivos más altos de la organización, no debería ejecutarse.

**Material:** [clase](u2-security-in-organizations/slides.md), [práctica](u2-security-in-organizations/practica.md), [tarea](u2-security-in-organizations/tarea.md), [glosario](u2-security-in-organizations/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** evaluar un proyecto de seguridad como inversión o como proyecto social; las dimensiones legal, matemática, tecnológica y geopolítica; procesos e infraestructuras críticas; el marco de ciberseguridad del NIST; SABSA y Zachman.
- [ ] **Lectura:** [`04-gasto-o-inversion.md`](../../12-seguridad/04-seguridad-informacion/04-gasto-o-inversion.md), [`05-complejidad-en-las-organizaciones.md`](../../12-seguridad/04-seguridad-informacion/05-complejidad-en-las-organizaciones.md), [`06-marcos-de-referencia-y-arquitecturas.md`](../../12-seguridad/04-seguridad-informacion/06-marcos-de-referencia-y-arquitecturas.md).
- [ ] **Práctica:** en equipo, describir un escenario laboral real (sin datos que lo identifiquen), investigar al menos cuatro marcos de trabajo y justificar cuál le conviene.
- [ ] **Tarea:** procesos críticos y catálogo de activos de su entorno laboral, con la cadena de valor como herramienta.
- **Fuentes base:** Orden Ejecutiva 13636 (2013); NIST CSF 1.1 (2018) y CSF 2.0 (2024); Sherwood, Clark y Lynas, *Enterprise Security Architecture* (2005); Vacca, *Computer and Information Security Handbook*.

**Al terminar puedes:** presentar un proyecto de seguridad en términos de negocio, distinguir un proceso crítico de uno ordinario y elegir con argumentos el marco de referencia que conviene a una organización.

---

## Unidad 3. Seguridad física y lógica (5 h)

> Los controles lógicos más avanzados no sirven si alguien puede llegar al hardware.

**Material:** [clase](u3-physical-logical-security/slides.md), [práctica](u3-physical-logical-security/practica.md), [tarea](u3-physical-logical-security/tarea.md), [glosario](u3-physical-logical-security/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** el valor de los datos y los riesgos globales; tipos de control; defensa en profundidad; zonas seguras y controles físicos y ambientales; roles, cadena de intrusión, ciclo de vida de los datos y CIS Controls.
- [ ] **Lectura:** [`07-riesgo-en-la-sociedad-de-la-informacion.md`](../../12-seguridad/04-seguridad-informacion/07-riesgo-en-la-sociedad-de-la-informacion.md), [`08-seguridad-fisica.md`](../../12-seguridad/04-seguridad-informacion/08-seguridad-fisica.md), [`09-seguridad-logica-y-ciclo-de-vida-de-los-datos.md`](../../12-seguridad/04-seguridad-informacion/09-seguridad-logica-y-ciclo-de-vida-de-los-datos.md).
- [ ] **Práctica:** mecanismos de seguridad para el teletrabajo a partir del capítulo XII Bis de la Ley Federal del Trabajo.
- [ ] **Tarea:** mapa de las instalaciones de una organización con cinco puntos de riesgo físico y los controles que los mitigan, según el manual del NIST.
- **Fuentes base:** Declaración de Principios de la CMSI (2003); *Global Risks Report* del Foro Económico Mundial; *ENISA Threat Landscape*; NIST SP 800-12, cap. 15; CIS Controls v7.1 y v8.1.

**Al terminar puedes:** proponer controles físicos y lógicos para los activos de una organización, justificarlos con el riesgo que atienden y ubicarlos en el ciclo de vida de los datos.

---

## Unidad 4. Gestión de la seguridad de la información (5 h)

> La certificación prueba que el sistema de gestión cumple la norma, no que la empresa sea segura.

**Material:** [clase](u4-isms-management/slides.md), [práctica](u4-isms-management/practica.md), [tarea](u4-isms-management/tarea.md), [glosario](u4-isms-management/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** apoyo de la dirección y caso de negocio; alcance, política, inventario y metodología de riesgos; declaración de aplicabilidad y Anexo A; roles; auditoría interna, certificación y hallazgos.
- [ ] **Lectura:** [`10-activar-el-sgsi.md`](../../12-seguridad/04-seguridad-informacion/10-activar-el-sgsi.md), [`11-implementar-el-sgsi.md`](../../12-seguridad/04-seguridad-informacion/11-implementar-el-sgsi.md), [`12-auditoria-y-certificacion.md`](../../12-seguridad/04-seguridad-informacion/12-auditoria-y-certificacion.md).
- [ ] **Práctica:** qué controles del Anexo A de ISO/IEC 27001 exige en la práctica el Reglamento General de Protección de Datos (artículos 31 a 39) y si hace falta un SGSI completo.
- [ ] **Tarea:** caso de negocio para implantar un SGSI en una organización real o realista, con normativa mexicana aplicable, costos y plan de trabajo.
- **Fuentes base:** ISO/IEC 27001:2022, 27002:2022, 27003, 27004 y 27005; ISO 19011 e ISO/IEC 27007; Reglamento (UE) 2016/679; Disposiciones de la CNBV para instituciones de crédito.

**Al terminar puedes:** armar el expediente de arranque de un SGSI, escribir una declaración de aplicabilidad y preparar a una organización para su auditoría de certificación.

---

## Unidad 5. Planes de contingencia (5 h)

> Un plan de continuidad que nunca se probó es una hipótesis.

**Material:** [clase](u5-contingency-planning/slides.md), [práctica](u5-contingency-planning/practica.md), [tarea](u5-contingency-planning/tarea.md), [glosario](u5-contingency-planning/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** continuidad y resiliencia; tipos de planes del NIST; enfoque europeo; BIA, MTD, RTO y RPO; controles preventivos y sitios alternos; tipos de prueba y mantenimiento.
- [ ] **Lectura:** [`13-continuidad-y-resiliencia.md`](../../12-seguridad/04-seguridad-informacion/13-continuidad-y-resiliencia.md), [`14-plan-de-continuidad.md`](../../12-seguridad/04-seguridad-informacion/14-plan-de-continuidad.md), [`15-pruebas-y-mantenimiento-del-plan.md`](../../12-seguridad/04-seguridad-informacion/15-pruebas-y-mantenimiento-del-plan.md).
- [ ] **Práctica:** qué exige la regulación bancaria mexicana a los planes de continuidad de las instituciones de crédito y cómo deben evaluarse.
- [ ] **Tarea:** análisis de impacto al negocio de una institución de crédito conforme al artículo 164 Bis y al Anexo 67 de las Disposiciones de la CNBV.
- **Fuentes base:** NIST SP 800-34 Rev. 1 (2010); ENISA, *Business and IT Continuity* (2008); INCIBE, *Plan de contingencia y continuidad de negocio*; ISO 22301:2019; NIST SP 800-61 Rev. 2 y Rev. 3.

**Al terminar puedes:** distinguir los planes de contingencia por su objetivo, conducir un BIA con métricas de recuperación y diseñar el programa de pruebas y mantenimiento de un plan de continuidad.

---

## Proyecto integrador

El portafolio de las cinco tareas forma un expediente completo para una misma organización: mapa conceptual, procesos y activos críticos, controles físicos, caso de negocio del SGSI y análisis de impacto al negocio. Se califica cuando otra persona puede tomar el expediente y continuar el proyecto sin preguntar.

---

*Rogelio Arriaga Gonzalez*
