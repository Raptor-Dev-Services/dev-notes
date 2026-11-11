# 02 - Normas internacionales en materia de seguridad de la información

> Unidad 1, tema 2 de la materia *Seguridad de la información*. Las dos escuelas que más pesan, NIST e ISO, con la guía de gestión de riesgos que se usa para enseñar el proceso. Revisado a noviembre de 2026.

## Por qué importa

Nadie empieza un plan de seguridad desde una hoja en blanco. Hay organismos cuyas propuestas se adoptan en todo el mundo, y conocerlas ahorra años de prueba y error. Pero también conviene leerlas con ojo crítico: cada norma responde a quien la escribió, a su contexto y a sus intereses. Esta nota presenta las dos fuentes de referencia más influyentes, el NIST estadounidense y la ISO, y muestra en qué se parecen y en qué no.

## Antes de las normas: ¿en quién confías?

Richard M. Stallman, promotor del movimiento del software libre, pregunta en su ensayo *¿Puede confiar en su ordenador?* a quién obedece realmente una computadora. Su argumento: cuando el código es cerrado y el fabricante controla lo que el equipo puede o no hacer (por ejemplo, con mecanismos de gestión digital de derechos o con "computación confiable" que en realidad confía en el proveedor y no en el dueño), el usuario deja de controlar su propia máquina. Stallman la llama, con ironía, *computación traicionera*.

El software libre propone lo contrario: código que cualquiera puede usar, estudiar, modificar y redistribuir. Esas cuatro libertades permiten auditar lo que hace un programa, corregirlo y compartir la corrección, y fomentan una comunidad que aprende en conjunto.

La conexión con este tema es directa: **todo depende del cristal con que se mire**. Una norma, un producto o un marco de referencia también expresan la visión de quien los diseñó. Elegir uno es también decidir en quién confiar.

## Los dos organismos de referencia

En seguridad de la información hay múltiples definiciones, posturas y maneras de organizar el trabajo. Dos organismos destacan porque sus propuestas se difunden y se adoptan en organizaciones de todo el mundo.

| | NIST | ISO (con IEC) |
|---|---|---|
| Qué es | Instituto Nacional de Estándares y Tecnología de Estados Unidos, fundado en 1901 para promover la innovación en ciencia y tecnología | Organización Internacional de Normalización; en tecnologías de la información trabaja en conjunto con la Comisión Electrotécnica Internacional |
| Qué publica | Una gran cantidad de publicaciones especiales (serie SP 800) y marcos, gratuitos, que se usan como referencia internacional, ciberseguridad incluida | Normas internacionales de pago, como la familia ISO/IEC 27000 |
| Enfoque | Metodologías claras y directas, fáciles de aplicar en distintos entornos | Marco general, basado en procesos y en mejora continua, aplicable a cualquier tipo y tamaño de organización |
| Alcance de "activo" | Activos ligados a las tecnologías de la información; deja fuera activos de otra naturaleza, como las personas | Cualquier cosa que tenga valor para la organización |
| Lo que entrega | Un procedimiento que se puede seguir paso a paso | Requisitos que se cumplen; el cómo queda a criterio de quien implementa |

Ninguno es "mejor". El NIST destaca por su sencillez y claridad estructural; la ISO, por su generalidad. Muchas organizaciones combinan los dos: el marco de gestión de ISO y las guías técnicas del NIST para hacer el trabajo.

## La propuesta del NIST: SP 800-30

Para gestionar el riesgo en sistemas de información, el NIST publicó en 2002 la *Special Publication 800-30, Risk Management Guide for Information Technology Systems*. Existe una revisión posterior, pero la versión original sigue siendo muy útil para aprender, porque explica el proceso completo de forma lineal.

En esa guía, la **gestión del riesgo** tiene tres procesos:

1. **Evaluación del riesgo** (*risk assessment*): determinar qué tan grandes son las amenazas y los riesgos de un sistema.
2. **Mitigación del riesgo**: priorizar, evaluar e implantar los controles que reducen el riesgo.
3. **Evaluación continua**: revisar periódicamente, porque los sistemas, las amenazas y la organización cambian.

### Los nueve pasos de la evaluación del riesgo

| Paso | Qué se hace | Qué se obtiene |
|---|---|---|
| 1. Caracterizar el sistema | Definir límites, hardware, software, interfaces, datos, personas y la misión que apoya | Límites y funciones del sistema, criticidad y sensibilidad de los datos |
| 2. Identificar amenazas | Revisar la historia de ataques al sistema y fuentes externas (agencias de inteligencia, centros de respuesta, medios) | Declaración de amenazas: fuentes con su motivación y acciones posibles |
| 3. Identificar vulnerabilidades | Consultar evaluaciones previas, auditorías, listas públicas de vulnerabilidades, avisos de fabricantes y resultados de pruebas (escaneo automatizado, pruebas de seguridad, pruebas de penetración) | Lista de vulnerabilidades que una fuente de amenaza podría explotar |
| 4. Analizar controles | Revisar los controles actuales y los planeados | Lista de controles existentes y por implantar |
| 5. Determinar la probabilidad | Combinar la motivación y capacidad de la fuente, la naturaleza de la vulnerabilidad y la eficacia de los controles | Calificación alta, media o baja |
| 6. Analizar el impacto | Medir la pérdida de integridad, disponibilidad y confidencialidad sobre la misión, apoyándose en un análisis de impacto al negocio | Calificación alta, media o baja |
| 7. Determinar el riesgo | Cruzar probabilidad e impacto | Nivel de riesgo para cada par amenaza y vulnerabilidad |
| 8. Recomendar controles | Proponer controles que reduzcan el riesgo a un nivel aceptable, considerando eficacia, legislación, política, impacto operativo, seguridad y confiabilidad | Controles recomendados |
| 9. Documentar resultados | Escribir el informe para la dirección | Informe de evaluación del riesgo |

### Fuentes de amenaza humanas

La guía clasifica a las fuentes humanas por su motivación, lo que conecta con la distinción entre amenaza y fuente de la nota [01](01-principios-y-conceptos.md):

| Fuente | Motivación típica |
|---|---|
| Intruso por reto o reconocimiento | Ego, rebeldía, desafío |
| Delincuente informático | Dinero, destrucción de información, revelación ilegal |
| Terrorista | Chantaje, destrucción, venganza, explotación |
| Espionaje industrial o de otros gobiernos | Ventaja competitiva, espionaje económico |
| Personal interno (mal capacitado, descontento, negligente o deshonesto) | Curiosidad, ganancia económica, venganza, errores no intencionales |

### La matriz de riesgo

La SP 800-30 propone una matriz sencilla de tres por tres. A la probabilidad se le asigna un valor (alta 1.0, media 0.5, baja 0.1) y al impacto otro (alto 100, medio 50, bajo 10). El riesgo es el producto:

| Probabilidad \ Impacto | Bajo (10) | Medio (50) | Alto (100) |
|---|---|---|---|
| Alta (1.0) | 10, bajo | 50, medio | 100, alto |
| Media (0.5) | 5, bajo | 25, medio | 50, medio |
| Baja (0.1) | 1, bajo | 5, bajo | 10, bajo |

Un riesgo alto exige acciones correctivas lo antes posible; uno medio, un plan en un plazo razonable; uno bajo, decidir si se corrige o se acepta.

### Opciones para mitigar

La misma guía lista las opciones de mitigación: **asumir** el riesgo (operar sabiendo que existe o implantar controles para bajarlo), **evitarlo** (eliminar la causa), **limitarlo** (controles que reduzcan el efecto), **planificarlo** (un plan que priorice e implante controles), **investigar y reconocer** (admitir la vulnerabilidad y buscar controles) y **transferirlo** (por ejemplo, con un seguro). Son las mismas estrategias de la nota [01](01-principios-y-conceptos.md) con otros nombres.

> Actualización al 2026: la SP 800-30 de 2002 fue retirada y sustituida por la *SP 800-30 Rev. 1, Guide for Conducting Risk Assessments* (septiembre de 2012). La revisión organiza la evaluación en cuatro pasos (prepararla, conducirla, comunicar los resultados y mantenerla al día) y la sitúa dentro del proceso de gestión de riesgos de la SP 800-39, en tres niveles: organización, procesos de misión o negocio y sistema de información. Para el trabajo real se usa la Rev. 1; para entender el proceso por primera vez, la de 2002 sigue siendo didáctica.

## La propuesta de ISO: el sistema de gestión

La ISO no entrega una metodología paso a paso sino un **marco**. Su visión es general, centrada en la mejora continua y en el enfoque de procesos, y se materializa en el **sistema de gestión de seguridad de la información (SGSI)**: la base para manejar los riesgos de forma ordenada hasta un nivel que las partes interesadas acepten.

De acuerdo con ISO/IEC 27000 en su edición de 2014, un SGSI se construye alrededor de la evaluación de los riesgos del negocio, y sirve para establecer, implantar, operar, vigilar, revisar, mantener y mejorar la seguridad de la información de una organización.

Alrededor de esa definición se publicó una familia de normas que se actualiza de forma continua (no siempre con la velocidad deseable) y cuyo propósito es que cualquier tipo de organización pueda aplicarlas.

### La familia ISO/IEC 27000

ISO/IEC 27000 describe la familia y la ordena por el tipo de documento:

| Tipo | Normas | Para qué |
|---|---|---|
| Panorama y vocabulario | ISO/IEC 27000 | Conceptos comunes y relación entre las normas de la familia |
| Normas que especifican requisitos | ISO/IEC 27001 | Requisitos del SGSI; es la única certificable para una organización |
| | ISO/IEC 27006 | Requisitos para los organismos que auditan y certifican SGSI |
| | ISO/IEC 27009 | Cómo escribir normas sectoriales que agreguen requisitos a la 27001 |
| Normas de guía | ISO/IEC 27002 | Catálogo de controles con su guía de implantación |
| | ISO/IEC 27003 | Guía para implantar los requisitos del SGSI |
| | ISO/IEC 27004 | Seguimiento, medición, análisis y evaluación del SGSI |
| | ISO/IEC 27005 | Gestión de riesgos de seguridad de la información |
| | ISO/IEC 27007 | Guía para auditar SGSI |
| | ISO/IEC 27014 | Gobierno de la seguridad de la información |
| Guías sectoriales | ISO/IEC 27011, 27017, 27018, 27019, ISO 27799 | Telecomunicaciones, servicios en la nube, datos personales en la nube pública, sector energético y salud |

Leer la familia completa, al menos su panorama, es obligado para cualquier profesional del área. Algunas normas tienen errores de concepto y redacciones discutibles; criticarlas con argumentos es justamente lo que permite diseñar estrategias de seguridad que respondan a las necesidades actuales y futuras de cada organización, en lugar de copiar plantillas.

> Actualización al 2026: ISO/IEC 27001 y 27002 están vigentes en su edición de 2022 (la 27001 con la enmienda 1:2024 sobre cambio climático) y la 27005 en su edición de 2022. La edición 2018 de ISO/IEC 27000, que se citaba en este tema, fue reemplazada en julio de 2026 por la sexta edición, ISO/IEC 27000:2026, que ya no es un documento de vocabulario y ordena la familia por función y no por tipo de documento, así que la tabla de arriba corresponde a las ediciones anteriores. La nueva ordenación distingue la especificación del SGSI (27001); los controles candidatos (27002 y guías sectoriales como 27011, 27017 y 27019); las guías para cumplir los requisitos (27003, 27004, 27005 y 27007); el uso del SGSI (27013, 27014 y 27016); la evaluación de controles y de competencias (27008, 27021, 27022 y 27028), y la evaluación de la conformidad, donde la 27006 ya no aparece: la sustituyó en marzo de 2024 ISO/IEC 27006-1:2024. Las normas de la familia cambiaron de título genérico: hoy se agrupan bajo *Information security, cybersecurity and privacy protection*.

## "Mejores prácticas" o "buenas prácticas"

El término *mejores prácticas* se volvió muy común en la gestión de tecnologías de la información y de la seguridad. Con el tiempo quedó claro que es relativo: lo que funciona de maravilla en un banco puede ser excesivo para una pyme o insuficiente para una planta de energía. Por eso hoy se prefiere hablar de **buenas prácticas**: formas de trabajar probadas y recomendables, que se adaptan al contexto en lugar de imponerse como la única respuesta correcta.

## Glosario

| Término | Definición |
|---|---|
| NIST | Instituto Nacional de Estándares y Tecnología de Estados Unidos; publica guías gratuitas como la serie SP 800 |
| ISO | Organización Internacional de Normalización; con la IEC publica la familia ISO/IEC 27000 |
| Software libre | Programas que se pueden usar, estudiar, modificar y redistribuir sin restricciones del autor |
| Gestión del riesgo | Conjunto de procesos para identificar, evaluar y tratar los riesgos hasta un nivel aceptable |
| Evaluación del riesgo | Proceso que identifica amenazas y vulnerabilidades y estima probabilidad, impacto y nivel de riesgo |
| Matriz de riesgo | Tabla que cruza probabilidad e impacto para asignar un nivel de riesgo |
| Probabilidad | Calificación de qué tan factible es que una fuente de amenaza explote una vulnerabilidad |
| Impacto | Magnitud del daño que causaría la explotación de una vulnerabilidad |
| SGSI | Sistema de gestión de seguridad de la información: marco de procesos, políticas y controles para manejar el riesgo de forma continua |
| Mejora continua | Ciclo permanente de planear, hacer, verificar y actuar para que el sistema mejore con el tiempo |
| Norma de requisitos | Documento que dice qué debe cumplirse y contra el cual se puede auditar |
| Norma de guía | Documento que orienta cómo cumplir, sin ser obligatorio para certificar |
| Buenas prácticas | Formas de trabajo probadas que se adaptan al contexto de cada organización |

## Referencias

- International Organization for Standardization e International Electrotechnical Commission. (2014). *ISO/IEC 27000:2014 Information technology. Security techniques. Information security management systems. Overview and vocabulary*. ISO. https://www.iso.org/standard/63411.html
- International Organization for Standardization e International Electrotechnical Commission. (2018). *ISO/IEC 27000:2018 Information technology. Security techniques. Information security management systems. Overview and vocabulary*. ISO. https://www.iso.org/standard/73906.html
- International Organization for Standardization e International Electrotechnical Commission. (2022). *ISO/IEC 27001:2022 Information security, cybersecurity and privacy protection. Information security management systems. Requirements*. ISO. https://www.iso.org/standard/27001
- International Organization for Standardization e International Electrotechnical Commission. (2024). *ISO/IEC 27006-1:2024 Information security, cybersecurity and privacy protection. Requirements for bodies providing audit and certification of information security management systems. Part 1: General*. ISO. https://www.iso.org/standard/82908.html
- International Organization for Standardization e International Electrotechnical Commission. (2026). *ISO/IEC 27000:2026 Information security, cybersecurity and privacy protection. Information security management systems. Overview*. ISO. https://www.iso.org/standard/27000
- Joint Task Force Transformation Initiative. (2011). *Managing information security risk: Organization, mission, and information system view* (NIST Special Publication 800-39). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/39/final
- Joint Task Force Transformation Initiative. (2012). *Guide for conducting risk assessments* (NIST Special Publication 800-30 Rev. 1). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/30/r1/final
- Stallman, R. M. (2017). *¿Puede confiar en su ordenador?* Free Software Foundation. https://www.gnu.org/philosophy/can-you-trust.es.html
- Stoneburner, G., Goguen, A., y Feringa, A. (2002). *Risk management guide for information technology systems* (NIST Special Publication 800-30). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/30/final

---

*Rogelio Arriaga Gonzalez*
