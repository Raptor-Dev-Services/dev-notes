# 10 - Activar el sistema de gestión de seguridad de la información

> Unidad 4, tema 1 de la materia *Seguridad de la información*. Lo que necesita un proyecto de SGSI para arrancar bien: apoyo de la dirección, caso de negocio, alcance, política, inventario de activos y metodología de riesgos. Revisado a noviembre de 2026.

## Por qué importa

La mayoría de los proyectos de SGSI que fracasan no lo hacen por razones técnicas, sino porque arrancaron sin patrocinio, con un alcance confuso o sin una forma acordada de medir el riesgo. Las primeras semanas deciden si el sistema se vuelve parte de la estrategia de la organización o un archivo de formatos que nadie lee. Esta nota reúne las piezas que hay que tener listas para que un proyecto basado en ISO/IEC 27001 sea viable desde el inicio y se convierta en una estrategia de seguridad a mediano y largo plazo.

## La seguridad como tema estratégico

Por la cantidad de ataques y su efecto en el desempeño de las empresas, organismos como ISO, The Open Group, ISC2, el CIS y muchos otros han publicado marcos, estándares y buenas prácticas. Su propósito es homologar cómo se entiende la implantación de los mecanismos de seguridad, cómo se evalúan los controles y, con ello, medir de forma objetiva el estado de salud de la seguridad de la información.

La seguridad tiene que formar parte de la estrategia de la organización y no verse solo como algo operativo. Un ejemplo de esa tendencia en México es la regulación bancaria: las Disposiciones de carácter general aplicables a las instituciones de crédito (la llamada Circular Única de Bancos de la CNBV) obligan a los bancos a tener un responsable de seguridad de la información con nivel directivo:

> **Artículo 168 Bis 13.-** Las Instituciones deberán contar con una persona que se desempeñe como oficial en jefe de seguridad de la información, conocido como CISO por sus siglas en inglés (Chief Information Security Officer).
>
> El oficial en jefe de seguridad de la información deberá ser designado por el director general y ocupar el nivel inmediato inferior al de este debiéndole reportar de manera directa. Será el responsable en materia de seguridad de la información de la Institución y deberá responder a los requerimientos formulados por las autoridades y al interior de la Institución en dicha materia.

La familia ISO/IEC 27000 reparte el trabajo así:

| Norma | Papel en el proyecto |
|---|---|
| ISO/IEC 27001 | Requisitos que el SGSI debe implantar y mantener, y el catálogo de controles del Anexo A |
| ISO/IEC 27002 | Recomendaciones y prácticas de implantación de cada control |
| ISO/IEC 27003 | Guía para implantar los requisitos del sistema |
| ISO/IEC 27004 | Métricas para dar seguimiento y evaluar el desempeño del SGSI |
| ISO/IEC 27005 | Marco para gestionar los riesgos del sistema |

> Precisión y actualización al 2026: el material original atribuía las métricas a ISO/IEC 27003; en realidad la medición es tema de ISO/IEC 27004 (edición 2016), y la 27003 (edición 2017) es la guía de implantación. El proyecto se arma hoy sobre ISO/IEC 27001:2022 y 27002:2022, no sobre las ediciones 2013.

## El apoyo de la dirección

Un SGSI necesita en todo momento el respaldo de los niveles directivos. Desde el primer día requiere recursos financieros, humanos y materiales; sin ellos el proyecto no termina, no aporta valor al negocio y la organización acaba por retirarle recursos o dejarlo a medias.

**Qué le toca a la dirección:**

- respaldar activamente la seguridad desde el principio, con políticas, y demostrar su compromiso;
- asignar de forma explícita las responsabilidades de seguridad a las personas adecuadas;
- aprobar la política de seguridad de la información, asignar recursos, nombrar los roles, y coordinar y revisar la implantación en toda la organización.

Un apoyo directivo abierto hace más eficaz la seguridad porque la alinea con los objetivos comerciales y estratégicos.

**Cómo conseguir ese apoyo.** Entre las técnicas que suelen recomendarse para convencer a la alta dirección:

- hablar en términos de negocio (procesos, clientes, ingresos, multas), no de tecnología;
- vincular el SGSI con un objetivo que la dirección ya persigue: entrar a un mercado, ganar una licitación que exige certificación, cumplir una auditoría de un cliente;
- cuantificar los riesgos y las obligaciones legales;
- mostrar casos de incidentes en el mismo sector;
- proponer un alcance inicial acotado y resultados rápidos, en fases, en lugar de un proyecto enorme;
- desarrollar las capacidades estratégicas propias: presentar, negociar y sostener el proyecto.

## El caso de negocio

Para que la dirección se comprometa, la información debe presentarse de forma que todos la entiendan en términos de lo que el proyecto rinde. Esa es la función del **caso de negocio**: un documento que explica por qué conviene hacer el proyecto, qué opciones hay, cuánto cuesta, qué beneficios trae y qué riesgos tiene, y termina con una recomendación. Documentar con claridad y objetividad la estrategia que persigue el SGSI es clave para que se apruebe.

Un argumento que siempre pesa son las consecuencias legales de no proteger la información. El material original resumía así las sanciones en México, con montos calculados en 2017:

| Norma | Riesgo | Sanción (montos de 2017) |
|---|---|---|
| Ley Federal de Protección de Datos Personales en Posesión de los Particulares | No cumplir los principios de protección de datos de clientes, empleados y proveedores | Prisión de 3 meses a 10 años; multa de 7,010 pesos a 44.8 millones de pesos |
| Código Penal Federal | Modificar, destruir o provocar la pérdida de información de clientes, socios o gobierno | Prisión de 3 meses a 8 años; multa de 7,010 a 42,060 pesos |
| Ley de la Propiedad Industrial | Divulgar secretos industriales, usar indebidamente marcas y software | Prisión de 2 a 10 años; multa de 7,010 pesos a 14 millones de pesos |
| Código de Comercio | Incumplir contratos | Según el caso |

> Actualización al 2026: la Ley Federal de Protección de Datos Personales en Posesión de los Particulares de 2010 fue abrogada por una nueva ley publicada el 20 de marzo de 2025; sus multas van de 100 a 320,000 Unidades de Medida y Actualización y se agravan con datos sensibles. La Ley de la Propiedad Industrial fue abrogada por la Ley Federal de Protección a la Propiedad Industrial (DOF 1 de julio de 2020), que tipifica los delitos en su artículo 402 y fija las penas en el 403. El Código Penal Federal conserva los artículos 211 bis 1 a 211 bis 7 sobre acceso ilícito a sistemas y equipos de informática. Para un caso de negocio actual, calcula los montos con la UMA vigente y revisa el texto de cada ley en el sitio de la Cámara de Diputados.

## El alcance del SGSI

La dirección define el **alcance** según la naturaleza del negocio, la organización, su ubicación, sus activos de información y sus tecnologías. Reglas que conviene respetar:

- Toda exclusión se justifica y se documenta.
- Las áreas fuera del alcance son, por definición, menos confiables. Cualquier proceso de negocio que pase información a través de la frontera puede necesitar controles adicionales.
- Recortar el alcance suele reducir los beneficios comerciales del SGSI: un certificado que cubre solo un centro de datos dice poco a un cliente que contrata un servicio completo.
- Si un control común se considera no aplicable, la decisión se justifica y se registra en la **declaración de aplicabilidad** (ver la nota [11](11-implementar-el-sgsi.md)).

Un alcance claro permite al implementador, a los directivos y a los auditores enfocar recursos y actividades.

## La política del SGSI

La declaración de la **política de seguridad de la información** es el marco de referencia de todos los lineamientos del sistema: fija el rumbo para administrar los recursos. Tener una estrategia definida y lineamientos claros es el punto de partida; las políticas específicas (uso aceptable, control de acceso, respaldos, clasificación) se derivan de ella.

## El inventario de activos

Hay que desarrollar y mantener un **inventario** de los activos de información importantes. Para cada uno se registra, al menos:

| Campo | Ejemplo |
|---|---|
| Tipo y formato | Software, físico o impreso, servicio, persona, intangible |
| Ubicación | Centro de datos principal, nube, archivo de la oficina |
| Responsable o propietario | Gerencia de nómina |
| Información de respaldo | Diario, cifrado, copia fuera del sitio |
| Información de licencia | Licencia por usuario, vence en diciembre |
| Valor para el negocio | Procesos que dependen del activo |

El inventario se puede llevar con los procesos de **gestión de la configuración** que propone ITIL, que permiten seguir los cambios y las características de cada activo en una base de datos de configuración (CMDB).

## La metodología de riesgos

Contar con una **metodología documentada** para valorar y analizar los riesgos permite a todos los integrantes del sistema conocer cómo se detectan los eventos que pueden afectar a los activos según su criticidad. Su finalidad principal es homologar conceptos y criterios.

Fijar una **línea base** para cada elemento del proceso de gestión de riesgos permite detectar con facilidad las anomalías. La metodología debe también dar a los auditores una guía para evaluar si los controles que gestionan esos riesgos son eficaces.

Lo que debe cumplir una evaluación de riesgos bien hecha:

- **Identificar, cuantificar y priorizar** los riesgos con los criterios de aceptación y los objetivos de la organización.
- **Estimar de forma sistemática** la magnitud de los riesgos (análisis) y **compararlos** con los criterios para decidir su importancia (valoración).
- **Guiar las acciones:** los resultados determinan las prioridades de gestión y los controles que se implantan.
- **Repetirse:** en distintas partes de la organización y de los sistemas, de forma periódica y cada vez que algo cambie.
- **Tener un alcance claro** y complementar, cuando corresponda, las evaluaciones de riesgo de otros aspectos del negocio.

Los detalles del proceso de evaluación están en la nota [03](03-sgsi-iso-27001.md).

## Para cerrar el tema

- El caso de negocio da a conocer a la alta dirección los beneficios y los requisitos del proyecto.
- El alcance define a qué elementos y a quién aplica el sistema.
- La política fija las reglas de conducta y de operación que preservan lo que establece el alcance.
- El inventario de activos dice sobre qué opera el sistema y quién responde por cada activo.
- Sin compromiso de la dirección no hay recursos, y sin recursos el compromiso no llega al resto del equipo.

## Glosario

| Término | Definición |
|---|---|
| Compromiso de la dirección | Alineación firme de la dirección con establecer, operar, revisar, mantener y mejorar el SGSI |
| Caso de negocio | Documento que justifica un proyecto con sus opciones, costos, beneficios, riesgos y una recomendación |
| CISO | Oficial en jefe de seguridad de la información: responsable de la seguridad con nivel directivo |
| Alcance del SGSI | Procesos, ubicaciones, activos y tecnologías que cubre el sistema |
| Exclusión | Parte de la organización o control que queda fuera del alcance, con justificación documentada |
| Política del SGSI | Declaración que fija el rumbo y el marco de los lineamientos del sistema |
| Activo de información | Información o elemento relacionado con su tratamiento (sistemas, soportes, edificios, personas) que tiene valor |
| Inventario de activos | Registro de los activos con su tipo, ubicación, responsable, respaldo, licencia y valor |
| CMDB | Base de datos de la gestión de la configuración donde se registran los activos y sus relaciones |
| Metodología de riesgos | Procedimiento documentado y acordado para identificar, analizar y valorar riesgos |
| Línea base | Estado de referencia que permite detectar desviaciones |
| UMA | Unidad de Medida y Actualización: referencia económica para calcular multas en la legislación mexicana |

## Referencias

- bvconsulting. (2010). *¿Qué es un business case?* [Video]. YouTube. https://youtu.be/drPWUq_iGmg
- Cámara de Diputados del H. Congreso de la Unión. (2020). *Ley Federal de Protección a la Propiedad Industrial*. https://www.diputados.gob.mx/LeyesBiblio/pdf/LFPPI.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley Federal de Protección de Datos Personales en Posesión de los Particulares* (DOF 20-03-2025, última reforma DOF 14-11-2025). https://www.diputados.gob.mx/LeyesBiblio/pdf/LFPDPPP.pdf
- Cámara de Diputados del H. Congreso de la Unión. (s. f.-a). *Código de Comercio*. https://www.diputados.gob.mx/LeyesBiblio/pdf/CCom.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Código Penal Federal* (última reforma DOF 13-03-2026). https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf
- Comisión Nacional Bancaria y de Valores. (2005, con reformas posteriores). *Disposiciones de carácter general aplicables a las instituciones de crédito*. CNBV. https://www.cnbv.gob.mx/Normatividad/Disposiciones%20de%20car%C3%A1cter%20general%20aplicables%20a%20las%20instituciones%20de%20cr%C3%A9dito.pdf
- INCIBE. (2008). *Alcance SGSI* [Video]. YouTube. https://youtu.be/vB0IL4FeA9Q
- INCIBE. (2010a). *SGSI 06: Definición de las políticas, organización, alcance* [Video]. YouTube. https://youtu.be/qawa_QcuFfc
- INCIBE. (2010b). *SGSI 08: Análisis y valoración de riesgos. Metodologías* [Video]. YouTube. https://youtu.be/g7EPuzN5Awg
- International Organization for Standardization e International Electrotechnical Commission. (2016). *ISO/IEC 27004:2016 Information technology. Security techniques. Information security management. Monitoring, measurement, analysis and evaluation*. ISO. https://www.iso.org/standard/64120.html
- International Organization for Standardization e International Electrotechnical Commission. (2017). *ISO/IEC 27003:2017 Information technology. Security techniques. Information security management systems. Guidance*. ISO. https://www.iso.org/standard/63417.html
- ISOTools Excellence. (2016, 15 de septiembre). *ISO 27001: Técnicas para convencer a la alta dirección de su implementación* [Entrada de blog]. https://iso-27001.blog/2016/09/iso-27001-tecnicas-convencer-alta-direccion-implementacion/
- KAWAK. (2016). *ISO 27001 desde un enfoque práctico* [Video]. YouTube. https://youtu.be/Nv8EuEjV5ZA
- PeopleCert. (s. f.). *ITIL service management*. AXELOS. https://www.axelos.com/certifications/itil-service-management

---

*Rogelio Arriaga Gonzalez*
