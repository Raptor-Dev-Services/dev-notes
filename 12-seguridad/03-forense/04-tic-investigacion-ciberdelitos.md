# 04 - Las TIC en la investigación de ciberdelitos

> Lectura del curso [Informática forense](../../cursos/digital-forensics/README.md), unidad 1, tema 4.

## Por qué importa

Cuando un incidente compromete la infraestructura de software o hardware de una organización, el perito llega a un lugar que alguien más diseñó, configuró y (con suerte) documentó. Para investigar con eficacia necesita entender cómo funciona ese entorno: qué activos hay, quién es responsable de cada uno, qué riesgos se habían identificado, qué políticas existían y qué registros se guardaban. Esa información vive, cuando existe, en un **sistema de gestión de la seguridad de la información (SGSI)**.

Saber de seguridad de la información le permite al especialista dos cosas: reconocer si la organización tenía un SGSI y aprovechar sus documentos (análisis de riesgos, análisis de vulnerabilidades, políticas), o detectar que no lo tenía y asumir la tarea de reconstruir los procesos relacionados con el incidente. Esta nota explica qué son las TIC, por qué son centrales en la investigación de ciberdelitos y qué le aportan al perito los marcos COBIT e ISO/IEC 27001.

## Qué son las tecnologías de la información

El término **tecnologías de la información** traduce el inglés *Information Technology* (IT). La Information Technology Association of America (ITAA) lo definió como el estudio, diseño, desarrollo, implementación, soporte y administración de sistemas de información basados en computadoras, lo que incluye tanto el software (programas de aplicación) como el hardware (componentes físicos).

Actualización al 2026: la ITAA ya no existe con ese nombre. En 2009 se fusionó con la AeA (antes American Electronics Association) para formar TechAmerica; la definición sigue siendo útil como punto de partida.

Más allá de la definición formal, lo que caracteriza a las TIC es que manejan datos de forma electrónica a lo largo de todo su ciclo:

| Proceso | Ejemplo en una investigación |
|---|---|
| Entrada | Un usuario captura una transferencia en la banca en línea |
| Procesamiento | El servidor valida la operación y calcula saldos |
| Almacenamiento | La base de datos guarda el movimiento y una bitácora registra quién lo hizo |
| Recuperación | Un analista consulta el movimiento días después |
| Transmisión | La operación viaja cifrada entre el navegador, el banco y la cámara de compensación |
| Recepción | El banco destino acredita el depósito |

Todo eso puede ocurrir con texto, imágenes, sonido o video. Además, las TIC controlan máquinas y dispositivos electrónicos de todo tipo, desde una cerradura electrónica hasta una línea de producción, lo que amplía mucho el universo de lugares donde puede quedar un rastro.

### De IT a ICT

Precisamente porque el campo es tan amplio, definirlo en pocas palabras es difícil. La tendencia ha sido ampliar el término a **Information and Communication Technology** (ICT), en español **tecnologías de la información y la comunicación** (TIC), para reconocer que hoy procesar información y comunicarla son la misma cosa. Para el investigador ese cambio de nombre tiene contenido: un delito con TIC casi nunca vive en una sola máquina, sino en la comunicación entre varias.

## Por qué las TIC son centrales en la investigación

Las TIC cambian la forma en que se gestionan y analizan los datos, y con ello la forma de investigar. Le dan al investigador las herramientas para:

- **Recolectar** evidencia digital de equipos, redes y servicios.
- **Analizar** grandes volúmenes de datos con búsquedas, filtros y correlaciones que serían imposibles a mano.
- **Preservar** la integridad de lo recolectado con copias forenses y funciones hash.
- **Reconstruir** lo que pasó a partir de registros de distintas fuentes.

Manejar y entender estos sistemas es indispensable para identificar, prevenir y resolver delitos informáticos. Las conclusiones de la unidad lo resumen bien: la investigación forense no se limita a obtener imágenes forenses; exige comprender cómo encajan las TIC en el contexto del incidente para evaluar el evento de forma completa. Las TIC no solo sirven para recolectar evidencia, también para interpretarla.

## El sistema de gestión de la seguridad de la información

Un SGSI es el conjunto de políticas, procesos, responsables y controles con que una organización administra la seguridad de su información. Su propósito es que los riesgos sean **conocidos, asumidos, gestionados y minimizados** de una manera documentada, sistemática, estructurada, repetible y eficiente, y que se adapte cuando cambian los riesgos, el entorno o la tecnología. Dos marcos de referencia aparecen una y otra vez en este terreno.

### COBIT

COBIT son las siglas de *Control Objectives for Information and Related Technologies* (objetivos de control para la información y las tecnologías relacionadas). Lo desarrolló ISACA (Information Systems Audit and Control Association) a partir de una investigación con expertos de varios países.

Es un modelo para **auditar la gestión y el control** de los sistemas de información y la tecnología, pensado para toda la organización: administradores de TI, usuarios y auditores. Evalúa y monitorea con énfasis en el control del negocio y en la seguridad de TI, y aborda los controles de tecnología desde la perspectiva de los objetivos de la empresa. Ayuda a alinear la tecnología con esos objetivos y con la regulación, y a aprovechar mejor los recursos.

Actualización al 2026: la versión vigente es **COBIT 2019**, que sustituyó a COBIT 5 (2012) y organiza 40 objetivos de gobierno y gestión en cinco dominios: evaluar, orientar y supervisar (EDM); alinear, planificar y organizar (APO); construir, adquirir e implementar (BAI); entregar, dar servicio y soporte (DSS), y supervisar, evaluar y valorar (MEA). ISACA sigue dando soporte a COBIT 5 para quien ya lo usa.

### ISO/IEC 27001

A diferencia de COBIT, que es un modelo de auditoría y control, la norma ISO/IEC 27001 define un **sistema de gestión**: una serie de actividades que la organización realiza mediante procesos sistemáticos, documentados y conocidos por todos, para establecer, implementar, mantener y mejorar su SGSI. Su objetivo es proteger la confidencialidad, la integridad y la disponibilidad de la información a partir de evaluar los riesgos y aplicar controles. Es certificable: un organismo externo puede auditar que la organización la cumple.

Actualización al 2026: la edición vigente es **ISO/IEC 27001:2022**, con la enmienda 1 de 2024 (que agrega consideraciones de cambio climático entre los requisitos de las partes interesadas). Su anexo A pasó de 114 controles a **93**, agrupados en cuatro temas: organizacionales, de personas, físicos y tecnológicos. Quien lea documentación de una organización certificada antes de 2022 encontrará la estructura anterior (2013) y la tendrá que mapear.

| Aspecto | COBIT 2019 | ISO/IEC 27001:2022 |
|---|---|---|
| Qué es | Marco de gobierno y gestión de la información y la tecnología | Norma de requisitos para un sistema de gestión de seguridad de la información |
| Pregunta que responde | ¿La tecnología se gobierna y controla en función del negocio? | ¿Los riesgos de seguridad de la información se gestionan de forma sistemática? |
| Quién lo publica | ISACA | ISO e IEC |
| Certificable | No como organización (hay certificaciones para personas) | Sí, mediante auditoría de un organismo acreditado |
| Lo que le da al perito | Procesos, responsables y controles de TI documentados | Inventario de activos, análisis de riesgos, políticas, controles y evidencias de su operación |

## Lo que el perito debe solicitar

Para responder a un incidente, el experto en informática forense pide la documentación que describa la infraestructura, los programas, los accesos y su configuración. Según el caso, eso incluye:

| Documento | Para qué le sirve al perito |
|---|---|
| Inventario de activos dentro del alcance del SGSI, con sus responsables directos (propietarios) | Saber qué equipos y sistemas existen y a quién preguntar por cada uno |
| Identificación de amenazas sobre esos activos | Contrastar el incidente con los riesgos que la organización ya conocía |
| Análisis de impacto en confidencialidad, integridad y disponibilidad | Dimensionar el daño y priorizar qué revisar primero |
| Políticas de seguridad | Saber qué estaba permitido y qué no; una conducta contraria a la política orienta la investigación |
| Diagramas de red, configuraciones y cuentas con privilegios | Reconstruir por dónde pudo entrar y moverse quien atacó |
| Política de bitácoras: qué se registra, dónde y por cuánto tiempo | Saber si existen los registros que el principio de intercambio promete |

### Qué cuenta como información

Para un SGSI, **información** es toda la que posee la organización, sin importar cómo se guarda o se transmite (escrita, en diagramas, impresa, almacenada en medios electrónicos, proyectada, enviada por fax o correo, o incluso dicha de viva voz en persona o por teléfono), de dónde viene (de la propia organización o de fuera) y cuándo se generó. Para el perito, esta definición amplia es un recordatorio: la evidencia de un incidente puede estar en un pizarrón fotografiado o en una conversación telefónica grabada, no solo en un servidor.

### La administración de la seguridad

Mantener la integridad de la información y proteger los activos de TI requiere un proceso continuo de administración de la seguridad, que incluye:

1. Definir y mantener roles y responsabilidades de seguridad.
2. Establecer políticas, estándares y procedimientos de TI.
3. Monitorear la seguridad y hacer pruebas periódicas.
4. Corregir las debilidades y atender los incidentes que se identifiquen.

Cuando funciona, protege todos los activos de TI y reduce el impacto de vulnerabilidades e incidentes sobre el negocio. Cuando el perito encuentra evidencia de que funcionaba (bitácoras de monitoreo, reportes de pruebas, tickets de incidentes), tiene una línea de tiempo previa al hecho que vale oro.

### Si no hay documentación

Muchas organizaciones no tienen un SGSI, o lo tienen solo en papel. En ese caso, el investigador asume la responsabilidad de **identificar y documentar** los procesos relacionados con el incidente, para darle orden y sentido a la investigación. Esa reconstrucción (quién administra qué, qué se registra, qué se respalda) se convierte en parte del expediente y del dictamen.

## Del SGSI al equipo encendido

La documentación dice qué debería haber; el lugar de los hechos dice qué hay. Cuando el perito encuentra equipos encendidos, el orden en que recoge la información importa, porque algunos datos desaparecen en segundos y otros duran años. La guía RFC 3227 del IETF propone recolectar primero lo más volátil:

| Orden | Dónde está el dato | Cuánto dura |
|---|---|---|
| 1 | Registros y caché del procesador | Nanosegundos |
| 2 | Tabla de enrutamiento, caché ARP, tabla de procesos, estadísticas del núcleo y memoria RAM | Lo que dure encendido el equipo |
| 3 | Sistemas de archivos temporales | Hasta el siguiente reinicio o limpieza |
| 4 | Disco | Hasta que se sobrescriba |
| 5 | Registros remotos y datos de monitoreo | Lo que fije la política de retención |
| 6 | Configuración física y topología de la red | Hasta que alguien la cambie |
| 7 | Medios de archivo y respaldos | Años |

Este orden conecta los cuatro temas de la unidad: el [derecho positivo](01-derecho-positivo-mexicano.md) dice qué se puede recolectar y con qué autorización; la [criminalística](02-criminalistica-tic.md) dice cómo fijarlo sin contaminarlo; la [metodología](03-metodologia-investigacion.md) dice qué buscar según el planteamiento del problema, y el conocimiento de las TIC dice dónde está cada dato y cuánto tiempo va a seguir ahí.

## Glosario

| Término | Definición |
|---------|-----------|
| TIC | Tecnologías de la información y la comunicación: hardware, software y redes que capturan, procesan, guardan y transmiten información |
| Tecnologías de la información | Estudio, diseño, desarrollo, implementación, soporte y administración de sistemas de información basados en computadoras |
| SGSI | Sistema de gestión de la seguridad de la información: políticas, procesos y controles para administrar los riesgos de la información |
| COBIT | Marco de ISACA para gobernar, gestionar y auditar la tecnología en función de los objetivos del negocio |
| ISO/IEC 27001 | Norma internacional certificable que fija los requisitos de un SGSI |
| Activo | Todo lo que tiene valor para la organización y debe protegerse: información, equipos, aplicaciones, personas |
| Propietario del activo | Persona responsable de un activo dentro del SGSI |
| Amenaza | Causa potencial de un incidente que puede dañar un activo |
| Confidencialidad, integridad y disponibilidad | Las tres propiedades que la seguridad de la información busca proteger |
| Política de seguridad | Documento que fija qué está permitido y qué no respecto de la información |
| Bitácora | Registro automático de eventos de un sistema; fuente principal del principio de intercambio en TIC |
| Orden de volatilidad | Prioridad de recolección según cuánto tarda en perderse cada dato |

## Referencias

- Brezinski, D. y Killalea, T. (2002). *Guidelines for evidence collection and archiving* (RFC 3227). Internet Engineering Task Force. https://www.rfc-editor.org/rfc/rfc3227
- International Organization for Standardization e International Electrotechnical Commission. (2022). *ISO/IEC 27001:2022. Information security, cybersecurity and privacy protection: Information security management systems: Requirements*. https://www.iso.org/standard/27001
- International Organization for Standardization e International Electrotechnical Commission. (2024). *ISO/IEC 27001:2022/Amd 1:2024. Climate action changes*. https://www.iso.org/standard/88435.html
- ISACA. (2018). *COBIT 2019 framework: Introduction and methodology*. https://www.isaca.org/resources/cobit
- Universidad EAFIT. (s. f.). *COBIT: Modelo para auditoría y control de sistemas de información*. https://www.eafit.edu.co/escuelas/administracion/consultorio-contable/Documents/A%20COBIT.pdf (dirección original del material; en 2026 redirige al sitio general de la universidad).
- Washington Technology. (2009, febrero). *Merged AeA and ITAA organization has new name*. https://www.washingtontechnology.com/2009/02/merged-aea-and-itaa-organization-has-new-name/323130

---

*Rogelio Arriaga Gonzalez*
