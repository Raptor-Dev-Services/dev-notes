<!-- _class: lead -->

# U3 - Normas, estándares y mejores prácticas

### Legislación nacional e internacional en ciberseguridad

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 2 horas de clase. Hilo conductor: la ley dice "protege los datos", pero no dice cómo; esta unidad
estudia quién escribe el "cómo" (NIST, ISO, las NOM) y qué fuerza jurídica tiene cada fuente.
Lecturas de apoyo: notas 07, 08 y 09 de 12-seguridad/05-legislacion.
-->

---

## La idea de hoy

> La ley fija el deber; la norma técnica dice cómo se cumple y cómo se demuestra.

Cuando hay una filtración, la pregunta que decide el caso no es si la empresa "tenía seguridad", sino si siguió un estándar reconocido.

---

## Objetivos de la unidad

Al terminar la unidad podrás:

1. Explicar qué es el NIST y por qué sus guías pesan en todo el mundo aunque no sea un regulador.
2. Ubicar a ISO, su proceso de normalización y la familia ISO/IEC 27000.
3. Distinguir una Norma Oficial Mexicana de un estándar voluntario y conocer la norma mexicana de ciberseguridad.

---

## Agenda

| Bloque | Qué vemos |
|---|---|
| 1 | De la ley al control técnico |
| 2 | NIST: FISMA, serie SP 800 y el Marco de Ciberseguridad |
| 3 | ISO: proceso, normas populares y familia 27000 |
| 4 | NOM: naturaleza jurídica, ley de 2020 y la NMX-I-27032 |
| 5 | Cierre, práctica y tarea |

---

## De la ley al control técnico

| Nivel | Ejemplo |
|---|---|
| Constitución y tratados | Derecho a la protección de datos personales |
| Leyes | Ley de protección de datos: "medidas de seguridad administrativas, técnicas y físicas" |
| Reglamentos y disposiciones | Cómo documentar esas medidas |
| Normas técnicas obligatorias | Una NOM que fija especificaciones |
| Estándares voluntarios | NIST, ISO, NMX |
| Políticas internas | El procedimiento de la empresa |

<!--
Pregunta para el grupo: ¿quién decide si una medida de seguridad fue "razonable"? Normalmente un juez o un
regulador, y para decidirlo mira estándares. Por eso esta unidad es de derecho aunque hable de controles.
-->

---

## Por qué los estándares importan al derecho

- Son el punto de encuentro entre lo técnico y lo jurídico.
- Dan un procedimiento común para temas críticos: **evidencia digital**, **tratamiento de datos**, **seguridad de sistemas y usuarios**.
- Permiten demostrar diligencia: "seguimos un método aceptado internacionalmente".
- No sustituyen a la ley: la concretan.

---

<!-- _class: lead -->

# Parte 1
## NIST

---

## Qué es el NIST

| Rasgo | Detalle |
|---|---|
| Origen | Laboratorio federal fundado en 1901 para corregir el rezago de medición de la industria estadounidense |
| Dependencia | Departamento de Comercio de Estados Unidos |
| Naturaleza | Agencia **no reguladora**: publica guías, métricas y estándares |
| Papel clave | Escribe los estándares que las agencias federales deben seguir por mandato de FISMA |

<!--
Insistir en "no reguladora": el NIST no multa. Su fuerza viene de FISMA para el gobierno federal y, para el
sector privado, de contratos y de la costumbre del mercado.
-->

---

## FISMA: la ley detrás de las guías

- **2002:** *Federal Information Security Management Act*, dentro de la ley de gobierno electrónico.
- Obliga a cada agencia federal a tener un programa de seguridad de la información.
- Encarga al NIST los estándares técnicos para cumplirlo.
- **Actualización:** desde 2014 es la *Federal Information Security Modernization Act*, con funciones operativas para la agencia CISA.

---

## Nueve pasos para cumplir FISMA

| # | Paso | Fase del RMF |
|---|---|---|
| 1 | Clasificar la información a proteger | Categorizar |
| 2 | Fijar la línea base de controles mínimos | Seleccionar |
| 3 | Ajustarla con una evaluación de riesgos | Seleccionar |
| 4 | Documentarla en un plan de seguridad | Seleccionar |
| 5 | Implementar los controles | Implementar |
| 6 | Medir su eficacia | Evaluar |
| 7 | Determinar el riesgo para la agencia | Autorizar |
| 8 | Autorizar el sistema para operar | Autorizar |
| 9 | Vigilar los controles de forma continua | Monitorear |

<!--
El paso 8 es el más jurídico: una persona con cargo firma la aceptación del riesgo residual. La
responsabilidad queda asignada.
-->

---

## La serie SP 800

| Publicación | Tema | Edición vigente |
|---|---|---|
| SP 800-53 | Catálogo de controles de seguridad y privacidad | Rev. 5 (2020), versión 5.2.0 de agosto de 2025 |
| SP 800-37 | Marco de Gestión de Riesgos (RMF) | Rev. 2 (diciembre de 2018) |
| SP 800-137 | Monitoreo continuo (ISCM) | Septiembre de 2011, con su complemento 800-137A |

La versión 5.2.0 de la SP 800-53 responde a una orden ejecutiva de 2025 y se concentra en la seguridad de las actualizaciones y parches de software.

---

## Monitoreo continuo e inventario por valor

1. Identificar los activos y ordenarlos por prioridad.
2. Fijar umbrales de riesgo para actuar.
3. Decidir la frecuencia de vigilancia de cada control y reportar a quien autoriza.

**Inventario basado en valor:** primero se localiza la información cuya pérdida haría más daño, y alrededor de ella se concentran las defensas.

---

## El Marco de Ciberseguridad (CSF) 1.1

| Función | Qué busca |
|---|---|
| Identificar | Saber qué se tiene y quién toca información sensible |
| Proteger | Controlar accesos, cifrar, capacitar |
| Detectar | Monitorear y revisar actividad inusual |
| Responder | Contener, avisar a clientes y empleados, ajustar la política |
| Recuperar | Restaurar equipos y datos y volver a operar |

<!--
El glosario de la unidad define las cinco funciones con ejemplos de una organización pequeña. Pedir a dos
personas que den un ejemplo propio de cada función.
-->

---

## Actualización: CSF 2.0 (26 de febrero de 2024)

- Agrega una sexta función, **Gobernar**, al centro de las demás: estrategia, roles, políticas y riesgo de la cadena de suministro.
- Deja de dirigirse solo a infraestructura crítica: aplica a cualquier organización.
- Núcleo con 22 categorías y 106 subcategorías.

| GV | ID | PR | DE | RS | RC |
|---|---|---|---|---|---|
| Gobernar | Identificar | Proteger | Detectar | Responder | Recuperar |

---

## Cumplir NIST no es estar seguro

- El cumplimiento es un piso, no un techo: siempre queda **riesgo residual**.
- Lecciones del tema:
  - Sin inventario de dispositivos y sistemas no hay estándar que aplicar.
  - Roles y responsabilidades por escrito, incluidos los proveedores.
  - Política formal para borrar archivos y dar de baja equipos.
  - En una crisis, informar a empleados y clientes es parte del plan.

---

<!-- _class: lead -->

# Parte 2
## ISO

---

## Un nombre que no es sigla

- **ISO** viene del griego *isos*, "igual", como en isométrico o isonomía.
- Un solo nombre en todos los idiomas, en lugar de una sigla distinta por país.
- Federación **no gubernamental** de organismos nacionales de normalización, uno por país, con sede en Ginebra.
- Sus normas se **venden**: tienen derechos de autor.
- **Actualización:** el material hablaba de 164 países; hoy son alrededor de 170 miembros (la página de ISO menciona 177).

---

## ISO, IEC y el JTC 1

| Pieza | Qué hace |
|---|---|
| ISO | Normalización internacional en casi todos los campos |
| IEC | Normalización eléctrica y electrónica |
| ISO/IEC JTC 1 | Comité conjunto para las tecnologías de la información |
| SC 27 | Subcomité de seguridad de la información, ciberseguridad y privacidad |

Por eso las normas de seguridad llevan el prefijo doble **ISO/IEC**.

---

## Cómo nace una norma

1. Una industria o un grupo de consumidores la pide.
2. ISO reúne expertos en un **comité técnico**.
3. Dos rondas de borradores; el segundo se vota como **FDIS** (borrador final).
4. Si se aprueba, la secretaría central lo certifica y se publica.
5. Revisión sistemática periódica: por eso cada norma se cita **con su año**.

<!--
Ejercicio rápido: "ISO 27001" sin año, ¿a cuál se refiere? Puede ser 2005, 2013 o 2022.
-->

---

## Normas populares en tecnología

| Norma | Tema | Situación |
|---|---|---|
| OSI (ISO 7498) | Modelo de siete capas | Edición conocida 1994 |
| ISO/IEC 27001 | Sistema de gestión de seguridad (SGSI) | Vigente 2022, con enmienda de 2024 |
| ISO/IEC 17799 | Buenas prácticas de controles | Renombrada 27002 en 2007 |
| ISO/IEC 20000-1 | Gestión de servicios de TI | Vigente 2018, con enmienda de 2024 |
| ISO 31000 | Gestión de riesgos | Vigente 2018; revisión en curso |
| ISO/IEC/IEEE 12207 | Ciclo de vida del software | Vigente 2026 (sustituyó a la de 2017) |

---

## Actualización: de 17799 a 27002:2022

- 2005: ISO/IEC 17799 con más de 100 controles.
- 2007: cambia de número a **ISO/IEC 27002**.
- 2013: 114 controles en 14 dominios.
- 2022: **93 controles en 4 temas**: organizacionales, de personas, físicos y tecnológicos.
- ISO/IEC 27001:2022 ya no se explica como "seis pasos": sigue la estructura común de sistemas de gestión y un anexo con esos 93 controles.

---

## El atractivo del ciberdelito

Según *ISOfocus* 132 (enero y febrero de 2019):

| Razón | Por qué atrae |
|---|---|
| Red enorme de interacciones | Cada dispositivo conectado es una puerta |
| Castigos relativamente leves | Poca probabilidad de ser atrapado |
| Reglas desiguales contra el lavado | Ganancias que pueden ser gigantescas |

La respuesta propuesta es de gestión: preparación, anticipar vulnerabilidades y resiliencia.

---

## La familia ISO/IEC 27000

| Norma | Tema |
|---|---|
| 27000 | Panorama de la familia (edición 2026; ya no es de vocabulario) |
| 27001 | Requisitos del SGSI, certificable |
| 27002 | Controles |
| 27005 | Gestión de riesgos |
| 27017 y 27018 | Nube y datos personales en la nube |
| 27042 y 27043 | Evidencia digital e investigación de incidentes |
| 27701 | Gestión de la privacidad; norma propia desde la edición 2025 (antes proyecto 27552) |

<!--
27042 y 27043 conectan con la materia de informática forense: la norma no solo previene, también ayuda a
llevar a un responsable ante un juez con evidencia bien tratada.
-->

---

## ISO/IEC 27032: ciberseguridad

- 2012: preservar confidencialidad, integridad y disponibilidad **en el ciberespacio**.
- Integra seguridad de la información, de redes e internet y protección de la infraestructura crítica (CIIP).
- Beneficios: proteger datos, sostener un programa, mejores políticas, continuidad, confianza, respuesta más rápida.
- **Actualización:** la edición 2023 la sustituye, cambia el título a *Guidelines for Internet security* y relaciona sus controles con la 27002.

---

## ISO frente a NIST

| Aspecto | ISO/IEC 27001 | NIST CSF |
|---|---|---|
| Origen | Consenso internacional | Agencia federal de EE. UU. |
| Forma | Requisitos de un sistema de gestión | Resultados por funciones |
| Certificación | Sí | No oficial |
| Costo | De pago | Gratuito |

Muchas organizaciones diagnostican con el NIST y certifican con la 27001.

---

<!-- _class: lead -->

# Parte 3
## Normas Oficiales Mexicanas

---

## NOM y estándar

- **NOM:** regulación técnica **obligatoria** que expide una autoridad normalizadora para que un bien, proceso o servicio se comercialice en México.
- **Estándar (antes NMX):** documento técnico de uso común y repetido, **voluntario**.
- **Actualización:** la Ley Federal sobre Metrología y Normalización de 1992 fue abrogada por la **Ley de Infraestructura de la Calidad** (DOF 1-07-2020), que llama "Estándares" a lo que eran NMX.

<!--
Leer en voz alta las fracciones X y XVI del artículo 4 de la ley, citadas en la nota 09.
-->

---

## Naturaleza jurídica

- Formalmente: **acto administrativo**. Materialmente: norma **general, abstracta y obligatoria**.
- La Segunda Sala de la SCJN resolvió que expedirlas dentro de la competencia de la dependencia no viola legalidad, reserva de ley ni subordinación (arts. 16, 49 y 73, fr. X).
- Se pueden impugnar por vicios de procedimiento o por inconstitucionalidad material.
- **Reserva de ley:** una NOM no puede restringir derechos ni crear sanciones.
- Riesgo: su proliferación desordenada erosiona la certeza jurídica.

---

## Contenido, vigencia y conformidad

| Tema | Regla |
|---|---|
| Quién la expide | La secretaría competente; su sigla va en la clave (SCFI, STPS, SEMARNAT...) |
| Qué contiene | Clave y finalidad, objeto, especificaciones, métodos de prueba, etiquetado, quién vigila |
| Novedad 2020 | Cada NOM trae su procedimiento de evaluación de la conformidad (art. 30) |
| Vigencia | Revisión sistemática al menos cada cinco años (art. 32) |
| Conformidad | Una entidad de acreditación reconoce al organismo; la autoridad normalizadora lo aprueba |

---

## El Plan de Acciones del IFT (2018)

| Objetivos estratégicos | Acciones transversales |
|---|---|
| Seguridad en dispositivos e infraestructura | Investigación, desarrollo e innovación |
| Seguridad en redes | Desarrollo de capacidades |
| Colaboración en seguridad y justicia | Coordinación y colaboración |
| Cultura de ciberseguridad | Medición y seguimiento |
| Colaboración con la Estrategia Nacional | |

Enfoque de gestión de riesgos, seguridad desde el diseño, laboratorio de pruebas y certificación para IoT.

---

## Actualización: del IFT a la CRT

- Reforma constitucional de diciembre de 2024: el IFT deja de ser órgano autónomo.
- **Ley en Materia de Telecomunicaciones y Radiodifusión** (DOF 16-07-2025) abroga la ley de 2014.
- Octubre de 2025: se integra la **Comisión Reguladora de Telecomunicaciones** y el IFT se extingue.
- La CRT es un órgano desconcentrado de la Agencia de Transformación Digital y **puede emitir NOM** para equipos que se conectan a las redes.
- El plan de 2018 queda como antecedente.

---

## NMX-I-27032-NYCE-2018

| Dato | Valor |
|---|---|
| Título | Lineamientos para la ciberseguridad |
| Elaboró | NYCE |
| DOF | 26 de junio de 2018; en vigor el 25 de agosto |
| Concordancia | Idéntica a ISO/IEC 27032:2012 |
| Carácter | Voluntaria; vigente en el catálogo |

**Objetivo:** guiar la mejora de la ciberseguridad en México: información, redes, internet e infraestructura crítica, con colaboración entre partes interesadas.

---

## La NMX frente a 27001 y al NIST

| Frente a | Diferencia | Complemento |
|---|---|---|
| ISO/IEC 27001 | La 27001 protege activos internos con un SGSI; la NMX mira riesgos de la interconexión (redes compartidas, aplicaciones web) | Amenazas internas y externas |
| Marco del NIST | El NIST es flexible y general; la NMX es más específica para el ciberespacio y enfatiza la colaboración | Medir madurez y coordinarse con el ecosistema |

**Actualización:** la NMX copia la edición 2012, ya sustituida por ISO/IEC 27032:2023.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones

- **NIST:** la base es identificar equipos y sistemas; roles claros; borrado seguro; comunicación en crisis.
- **ISO:** homologa términos, protege a consumidores, ahorra costos a empresas y da a los gobiernos opinión experta y comercio abierto.
- **NOM:** dan confianza en lo cotidiano, operan con métodos probados, se verifican con pruebas, inspecciones y certificaciones, y reflejan el desarrollo y la cultura del país.

---

## Al terminar puedes

- Explicar por qué una guía del NIST pesa aunque el NIST no regule.
- Nombrar las seis funciones del CSF 2.0 y las publicaciones 800-53, 800-37 y 800-137.
- Describir cómo nace una norma ISO y ubicar las piezas de la familia 27000.
- Distinguir NOM de estándar y explicar sus límites constitucionales.
- Describir la NMX-I-27032-NYCE-2018 y compararla con 27001 y con el NIST.

---

## Práctica y tarea

**Práctica:** foro sobre la importancia de los estándares internacionales, la necesidad de normas mexicanas y sus aplicaciones a la ciberseguridad, con réplica a tres compañeros.

**Tarea:** infografía sobre el objetivo de la NMX-I-27032-NYCE-2018 y su relación con las tecnologías de la información, las técnicas de seguridad y los lineamientos para la ciberseguridad.

Instrucciones completas en la práctica y la tarea de la unidad.

---

## Lecturas

- `12-seguridad/05-legislacion/07-nist.md`
- `12-seguridad/05-legislacion/08-iso.md`
- `12-seguridad/05-legislacion/09-normas-oficiales-mexicanas.md`
- Videos de apoyo: *NIST Illustrated*, *The Cybersecurity Framework*, *What is ISO 27001?* e *Improve cybersecurity posture by using ISO/IEC 27032*, citados en las notas.

---

## Siguiente unidad: leyes federales y ciberseguridad en México

> Un control técnico sin respaldo legal es una recomendación; con respaldo legal, es una obligación.

- La Constitución y los derechos digitales.
- Los delitos informáticos del Código Penal Federal.
- La nueva ley de protección de datos personales de 2025.
