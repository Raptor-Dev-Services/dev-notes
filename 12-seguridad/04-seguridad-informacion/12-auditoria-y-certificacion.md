# 12 - Auditoría y certificación del sistema de gestión de seguridad de la información

> Unidad 4, tema 3 de la materia *Seguridad de la información*. Revisión por la dirección, auditoría interna, el proceso de certificación en dos fases, los tipos de auditoría y de hallazgo, y lo que un certificado prueba y lo que no. Revisado a noviembre de 2026.

## Por qué importa

Un SGSI que nadie revisa se degrada: los controles dejan de operar, los registros se atrasan y la evaluación de riesgos describe una empresa que ya no existe. La auditoría es el mecanismo que mantiene el sistema honesto. Pero conviene tener clara una idea desde el principio: **la certificación no acredita que una empresa sea más o menos segura**. Acredita que su sistema de gestión opera conforme a la norma. Por eso la mejora continua debe ser mucho más estricta al evaluar la seguridad real que al revisar si los formatos están llenos; si se pierde de vista eso, el sistema pierde su razón de ser.

## Evaluar todo el tiempo

Un SGSI requiere evaluar de forma continua la implantación de los controles. Antes de que un tercero revise el sistema para acreditar que cumple la norma, la organización debe:

- hacer **auditorías internas**, y
- asegurarse de que los controles que implanta corresponden a sus necesidades y características.

### Revisión por la dirección

La dirección revisa el SGSI **al menos una vez al año** para comprobar que sigue siendo idóneo, adecuado y eficaz. En esa revisión:

- evalúa las oportunidades de mejora y la necesidad de cambios, incluidos la política y los objetivos de seguridad;
- documenta los resultados y conserva los registros.

La revisión forma parte de la fase de **verificación** del ciclo PDCA: cualquier acción correctiva que surja se gestiona en consecuencia (nota [03](03-sgsi-iso-27001.md)).

### Revisiones internas

ISO/IEC 27001 se basa en el ciclo PDCA, de modo que hay que revisar constantemente el sistema, comprobar que los controles se ejecuten y tener un proceso para mejorarlos. Todas las revisiones generan **registros**, que después evaluarán los auditores externos si se decide certificar; por eso su documentación es tan importante. Los registros son la evidencia de que los controles existen y operan.

### Antes de certificar

La organización revisa a fondo el SGSI y la declaración de aplicabilidad, y demuestra que recorrió el ciclo PDCA completo y que cumple el requisito de **mejora** de la norma, indispensable para la mejora continua. Los auditores de certificación buscarán pruebas en forma de registros: evaluaciones de riesgo, revisiones por la dirección, informes de incidentes y acciones correctivas. De ahí una consecuencia práctica: el SGSI necesita tiempo para establecerse, operar con normalidad y generar registros después de implantado. Certificar un sistema recién estrenado es certificar papel.

> Actualización al 2026: el material original ubicaba la mejora en la "cláusula 8", numeración de la edición 2005. En ISO/IEC 27001:2022 la evaluación del desempeño (seguimiento, auditoría interna y revisión por la dirección) está en la cláusula 9, y la mejora (no conformidades, acciones correctivas y mejora continua) en la cláusula 10.

## Qué es una auditoría

Una **auditoría** es un proceso sistemático, independiente y documentado para obtener evidencias y evaluarlas de manera objetiva, con el fin de determinar en qué medida se cumplen los criterios de auditoría. Tres términos acompañan la definición:

- **Alcance de la auditoría:** su extensión y sus límites. Suele describir las ubicaciones físicas, las unidades de la organización, las actividades y procesos y el periodo que se revisa.
- **Evidencia objetiva:** información, registros o declaraciones de hechos, cualitativos o cuantitativos, verificables y basados en la observación.
- **Hallazgo:** el resultado de evaluar la evidencia recopilada frente a los criterios de auditoría.

### Una metodología de auditoría

Una buena metodología asegura que se evalúen todas las características del sistema y lo vuelve sostenible. Siguiendo las directrices de ISO 19011 (auditoría de sistemas de gestión) e ISO/IEC 27007 (auditoría de SGSI), una auditoría recorre estas etapas:

| Etapa | Qué se hace |
|---|---|
| Programa | Se planean las auditorías del año según los riesgos y la importancia de los procesos |
| Preparación | Se fijan objetivos, alcance y criterios; se revisa la documentación; se arma el plan y las listas de verificación |
| Reunión de apertura | Se confirma el plan con los auditados |
| Recolección de evidencia | Entrevistas, observación, revisión de registros y muestreo de controles |
| Hallazgos | Se contrasta la evidencia con los criterios y se clasifican los hallazgos |
| Reunión de cierre | Se presentan los hallazgos y se acuerdan los siguientes pasos |
| Informe y seguimiento | Se entrega el informe y se verifica que las acciones correctivas se cumplan |

Los principios del auditor son integridad, presentación imparcial, debido cuidado profesional, confidencialidad, independencia y un enfoque basado en la evidencia y en el riesgo.

> Actualización al 2026: ISO 19011 tiene cuarta edición, ISO 19011:2026, publicada en mayo de 2026, que sustituye a la de 2018 y da más orientación para las auditorías remotas. ISO/IEC 27007 sigue vigente en su edición de 2020.

## La certificación

Certificar significa que un **organismo de certificación** evalúa el SGSI de la organización contra ISO/IEC 27001. El organismo se asegura de que la evaluación de riesgos refleje bien las actividades del negocio en todo el alcance del sistema, y verifica que la organización haya analizado y tratado sus riesgos y los siga gestionando de forma sistemática. Un certificado emitido por un organismo **acreditado** tiene credibilidad frente a otras organizaciones.

Hay que distinguir dos figuras:

| Figura | Qué hace | Ejemplos |
|---|---|---|
| Entidad de acreditación | Organismo oficial, por lo general uno por país, que acredita a las certificadoras como competentes | EMA (México), ENAC (España), UKAS (Reino Unido), OAA (Argentina) |
| Organismo de certificación | Empresa acreditada que audita y certifica sistemas de gestión según normas como ISO/IEC 27001, ISO 9001 o ISO 14001 | Las certificadoras comerciales acreditadas |

Los requisitos que deben cumplir los organismos que certifican SGSI están en ISO/IEC 27006. La validez de un certificado acreditado se puede consultar en bases públicas como IAF CertSearch.

> Actualización al 2026: ISO/IEC 27006:2015 y su enmienda de 2020 se retiraron el 1 de marzo de 2024. Las sustituyó ISO/IEC 27006-1:2024, que se aplica junto con ISO/IEC 17021-1, la norma general para quienes auditan y certifican sistemas de gestión.

### El proceso con el auditor

Conviene conocer qué hará el auditor y qué acompañamiento darle. El proceso típico:

1. **Elegir** un organismo de certificación acreditado y acordar alcance, fechas y costo.
2. **Preauditoría** (opcional): un ensayo para detectar brechas antes de la auditoría formal.
3. **Fase 1 de auditoría.** Sobre todo revisión documental: se analiza el SGSI en el contexto de la política de seguridad, los objetivos, el alcance, la evaluación de riesgos, la declaración de aplicabilidad y los documentos principales. Sirve para comprobar que la organización está lista y para planear la fase 2.
4. **Fase 2 de auditoría.** Se comprueba en sitio que la organización cumple sus propias políticas, objetivos y procedimientos, que el SGSI cumple los requisitos de ISO/IEC 27001 y que es **eficaz**.
5. **Acciones correctivas.** Si hubo no conformidades, se corrigen y se presenta la evidencia.
6. **Certificado.** Si no hay no conformidades pendientes y se verifica la implantación correcta, el auditor emite un informe favorable y el sistema queda certificado.

Una revisión de un tercero **no genera evidencia de que la empresa sea segura**, ni comprueba que los controles estén bien elegidos o bien implantados en sentido técnico: comprueba conformidad. Para lo otro hacen falta evaluaciones de seguridad, pruebas de penetración y métricas de eficacia.

### Tipos de auditoría a lo largo del ciclo

| Tipo | Cuándo | Para qué |
|---|---|---|
| Certificación | Al inicio | Fases 1 y 2; si se verifica la implantación correcta, se certifica |
| Seguimiento o vigilancia | Cada seis meses o cada año, según el contrato y la norma del organismo | Revisa partes concretas del sistema por su corta duración; comprueba que el SGSI se usa y fomenta la mejora |
| Recertificación | A los tres años, vigencia del certificado | Repite el proceso completo |

### Tipos de hallazgo

| Hallazgo | Qué significa | Qué exige |
|---|---|---|
| Observación | Declaración sustentada en evidencia objetiva que señala una oportunidad de mejora o un riesgo de incumplir más adelante | Analizarla; no impide certificar |
| No conformidad menor | Desviación de los criterios de auditoría con poca influencia en la eficacia del sistema | Plan de acción correctiva; se verifica en la siguiente auditoría |
| No conformidad mayor | Impide demostrar conformidad con la norma, afecta la eficacia del sistema o un requisito se incumple por completo | Corregirla antes de obtener o conservar el certificado |

Una **acción correctiva** elimina la causa de una no conformidad para que no se repita; no basta con arreglar el síntoma.

## Más allá del certificado: evaluar el desempeño de los controles

Un sistema puede estar certificado y tener controles que no funcionan bien. Por eso hay propuestas para evaluar **el desempeño de los controles**, no solo del sistema de gestión. Una de ellas (Berrío, 2016) usa el método Delphi, es decir, la opinión de expertos en rondas sucesivas, para seleccionar los controles de ISO/IEC 27001 más importantes para una organización y definirles indicadores de desempeño. La idea de fondo es medir lo que importa: si el control reduce el riesgo para el que se eligió.

## Para cerrar la unidad

Implantar un SGSI basado en ISO/IEC 27001 ayuda a homologar el criterio para aplicar los controles, pero **no asegura que los activos estén protegidos**. Aun con un sistema de gestión hay que evaluar la seguridad en todo momento para evitar que los activos se afecten y pierdan valor. Certificarse puede parecer un camino largo; es mucho más corto cuando la capa estratégica tiene claro que lo necesita para operar, y por eso hay que conseguir y conservar patrocinadores convencidos del proyecto y de la estrategia.

- Los registros dejan evidencia de la implantación de los controles.
- La auditoría interna es un requisito de la norma, genera evidencia de cumplimiento y produce acciones de mejora.
- La auditoría externa revisa que se cumplan los requisitos y que correspondan al sistema planteado.
- La certificación es el reconocimiento de un organismo externo acreditado de que el sistema cumple la norma.

## Glosario

| Término | Definición |
|---|---|
| Auditoría | Proceso sistemático, independiente y documentado para obtener y evaluar evidencias contra criterios |
| Alcance de la auditoría | Extensión y límites de la auditoría: ubicaciones, unidades, procesos y periodo |
| Criterio de auditoría | Requisito contra el cual se compara la evidencia: norma, política, contrato |
| Evidencia objetiva | Información verificable, basada en la observación, que respalda un hallazgo |
| Hallazgo | Resultado de evaluar la evidencia frente a los criterios |
| Observación | Hallazgo que señala una oportunidad de mejora sin ser incumplimiento |
| No conformidad | Incumplimiento de un requisito; puede ser menor o mayor |
| Acción correctiva | Acción que elimina la causa de una no conformidad para evitar que se repita |
| Revisión por la dirección | Evaluación periódica del SGSI por la alta dirección para decidir mejoras |
| Entidad de acreditación | Organismo oficial que reconoce la competencia de las certificadoras |
| Organismo de certificación | Empresa acreditada que audita y certifica sistemas de gestión |
| Auditoría de seguimiento | Auditoría periódica que comprueba que el sistema sigue operando entre certificaciones |
| Recertificación | Auditoría completa que renueva el certificado al terminar su vigencia de tres años |

## Referencias

- Barba, M. [Madeline Barba]. (2017). *Metodología de auditoría de sistemas y normas ISO 27001* [Video]. YouTube. https://youtu.be/QJHmWj0LD9E
- Berrío López, J. P. (2016). *Metodología para la evaluación del desempeño de controles en sistemas de gestión de seguridad de la información sobre la norma ISO/IEC 27001* [Tesis de maestría, Universidad Nacional de Colombia]. Repositorio UNAL. https://bffrepositorio.unal.edu.co/server/api/core/bitstreams/474e6f61-98c8-46e5-9a55-d16d15b8114c/content
- Entidad Mexicana de Acreditación. (s. f.). *Entidad Mexicana de Acreditación*. https://www.ema.org.mx/portal_v3/
- INCIBE. (2010). *SGSI 09: Gestión y tratamiento de los riesgos* [Video]. YouTube. https://youtu.be/9T9X0q2y6vQ
- International Accreditation Forum. (s. f.). *IAF CertSearch*. https://www.iafcertsearch.org/
- International Organization for Standardization. (2018). *ISO 19011:2018 Guidelines for auditing management systems*. ISO. https://www.iso.org/standard/70017.html
- International Organization for Standardization. (2026). *ISO 19011:2026 Guidelines for auditing management systems*. ISO. https://www.iso.org/standard/19011
- International Organization for Standardization e International Electrotechnical Commission. (2020). *ISO/IEC 27007:2020 Information security, cybersecurity and privacy protection. Guidelines for information security management systems auditing*. ISO. https://www.iso.org/standard/77802.html
- International Organization for Standardization e International Electrotechnical Commission. (2024). *ISO/IEC 27006-1:2024 Information security, cybersecurity and privacy protection. Requirements for bodies providing audit and certification of information security management systems. Part 1: General*. ISO. https://www.iso.org/standard/82908.html
- ISO 27001 Paso a Paso. (s. f.). *Fase 10: El proceso de certificación ISO 27001*. https://normaiso27001.es/fase-10-el-proceso-de-certificacion-iso-27001/

---

*Rogelio Arriaga Gonzalez*
