# 07 - Perspectiva de riesgo de la sociedad de la información

> Unidad 3, tema 1 de la materia *Seguridad de la información*. El valor de los datos, los principios de la Cumbre Mundial sobre la Sociedad de la Información, los riesgos globales y el panorama de amenazas, y los tipos de control que responden a ellos. Revisado a noviembre de 2026.

## Por qué importa

Las tecnologías de la información ya no son un área de la empresa: son el medio por el que la gente compra, trabaja, vota, se informa y se atiende la salud. Eso cambió la forma de los riesgos. Un ataque a un proveedor de nube puede detener hospitales en otro continente; una base de datos mal protegida expone a millones de personas que nunca supieron que estaban en ella. Para elegir controles con criterio hay que entender primero cuánto valen los datos, qué principios rigen la sociedad que los produce, qué riesgos globales se están observando y qué amenazas concretas los materializan.

## El valor de los datos

Los riesgos de la sociedad de la información siempre se miden contra el **valor de los datos en el mercado**. Ese valor depende del tipo de dato y de la calidad con que se gestiona su ciclo de vida, desde que se crea hasta que se destruye (el ciclo se estudia en la nota [09](09-seguridad-logica-y-ciclo-de-vida-de-los-datos.md)).

Valorar un dato exige mirarlo desde tres ángulos:

| Ángulo | Pregunta | Ejemplo |
|---|---|---|
| Cumplimiento | ¿Se gestiona según las leyes y regulaciones que le aplican? | Datos personales sujetos a la ley de protección de datos; datos de tarjeta sujetos a PCI DSS |
| Seguridad | ¿Está protegido contra acceso no autorizado, pérdida o corrupción? | Cifrado en reposo, respaldos probados, control de acceso |
| Gobierno | ¿Se administra con eficiencia, responsabilidad y en línea con los objetivos de la organización? | Dueño asignado, calidad del dato, retención definida |

El **valor** de algo combina la importancia que se le concede con lo que cuesta mantenerlo o reponerlo. La seguridad forma parte de la cadena de valor del dato junto con el cumplimiento y la regulación, y por eso pesa en decisiones como **dónde** se guarda y se procesa: en qué centro de datos, en qué nube y bajo qué jurisdicción.

## La sociedad de la información y sus principios

La **sociedad de la información** es la transformación profunda de la vida y de las relaciones entre personas, gobiernos, empresas y organizaciones que provoca el uso intensivo de las TIC. Esas tecnologías facilitan crear, distribuir y manipular información, y ocupan un lugar central en las actividades sociales, culturales y económicas.

El concepto tomó forma en las Naciones Unidas con la **Cumbre Mundial sobre la Sociedad de la Información** (CMSI), organizada por la Unión Internacional de Telecomunicaciones en dos fases: Ginebra en 2003 y Túnez en 2005. Fue el resultado del trabajo de múltiples partes interesadas: gobiernos, organismos internacionales, sector privado y sociedad civil. La Declaración de Principios de Ginebra fija once principios para una sociedad de la información centrada en las personas:

1. El papel de los gobiernos y de todas las partes interesadas en la promoción de las TIC.
2. La infraestructura de la información y las comunicaciones.
3. El acceso a la información y al conocimiento.
4. La creación de capacidades.
5. **El fomento de la confianza y la seguridad en la utilización de las TIC.**
6. Un entorno habilitador (jurídico, regulatorio y de políticas).
7. Las aplicaciones de las TIC en todos los ámbitos de la vida.
8. La diversidad cultural, la identidad, la diversidad lingüística y el contenido local.
9. Los medios de comunicación.
10. La dimensión ética de la sociedad de la información.
11. La cooperación internacional y regional.

El principio 5 es el que toca directamente a esta materia. En esencia dice que la confianza es una condición para que la sociedad de la información exista, y que esa confianza se construye con seguridad de la información y de las redes, autenticación, privacidad y protección al consumidor; que hace falta una cultura global de ciberseguridad desarrollada en cooperación con todas las partes; que debe impedirse el uso de las TIC con fines delictivos o terroristas, respetando siempre los derechos humanos; y que deben atenderse problemas como el correo no deseado.

Estos principios son la base de los foros de gobernanza de internet que existen hoy. La Agenda de Túnez de 2005 creó el **Foro para la Gobernanza de Internet** (IGF), que se reúne cada año desde 2006. En ese contexto, la seguridad en internet es el resultado de la visión de los diversos grupos de interés que buscan proteger todos los activos involucrados; genera confianza en el uso de esos recursos y reconoce como amenazas el uso indebido de las TIC y la ciberdelincuencia.

## Los riesgos globales

La integración de las TIC en la vida diaria de empresas y personas creó un contexto nuevo de riesgos, que pueden afectar al ciberespacio o a los elementos físicos donde se alojan los datos. El **Foro Económico Mundial** publica cada año desde 2006 su *Global Risks Report*, basado en encuestas a especialistas de todo el mundo, donde señala las amenazas con potencial catastrófico para la sociedad y para la cooperación entre países y ciudadanos.

En su edición 2019 (la decimocuarta), los riesgos tecnológicos aparecieron entre los más probables junto a los ambientales y sociales, como la escasez de agua o los conflictos entre países:

| Los cinco riesgos más probables, edición 2019 | Tipo |
|---|---|
| Eventos meteorológicos extremos | Ambiental |
| Fracaso de la mitigación y adaptación al cambio climático | Ambiental |
| Desastres naturales | Ambiental |
| Fraude o robo de datos | Tecnológico |
| Ciberataques | Tecnológico |

Que dos riesgos tecnológicos compitan con los ambientales muestra el valor que los datos tienen ya para la sociedad.

> Actualización al 2026: el *Global Risks Report 2026* (vigesimoprimera edición, 14 de enero de 2026) coloca la **confrontación geoeconómica** como el principal riesgo a dos años, seguida de la desinformación, la polarización social, los eventos meteorológicos extremos y los conflictos armados entre Estados. La **inseguridad cibernética** sigue dentro de los diez primeros. A diez años dominan los riesgos ambientales.

Los riesgos asociados a las infraestructuras críticas y a las tecnologías de la información crecen porque esas tecnologías se usan en todo. Los esquemas de seguridad actuales no van a sostenerse frente a los riesgos que traen las tecnologías nuevas y los usos nuevos de la información: hay que pensar en paradigmas distintos ante el **cómputo cuántico**, la **nanotecnología** o las **tecnologías alimentarias**, entre otras.

Para tratar estos riesgos con orden se usan marcos internacionales de gestión del riesgo, como ISO/IEC 27005, el marco de gestión del riesgo empresarial de **COSO** (*Committee of Sponsoring Organizations of the Treadway Commission*) y la metodología **OCTAVE** (*Operationally Critical Threat, Asset, and Vulnerability Evaluation*), entre otros. Las amenazas y vulnerabilidades que originan los riesgos son diversas y se estudian desde varios enfoques: objetivos, usos y efectos tecnológicos, para decidir si requieren protección en el espacio físico, en el ciberespacio o en ambos, y qué normas aplican.

## El panorama de amenazas

Los estudios de riesgos dan una visión general de impacto y probabilidad. Para elegir controles hace falta además conocer los tipos de amenazas y ataques, y los métodos con que se ejecutan. La **Agencia de la Unión Europea para la Ciberseguridad** (ENISA) publica cada año su *Threat Landscape*, que evalúa las afectaciones reales o potenciales en los países miembros y da una perspectiva que sirve en otras latitudes para invertir mejor.

En su edición 2021, ENISA identificó nueve amenazas principales:

| Amenaza | En qué consiste |
|---|---|
| Ransomware | Secuestro de la información mediante cifrado y, cada vez más, extorsión con su publicación |
| Malware | Código malicioso de todo tipo: troyanos, gusanos, programas espía |
| Criptominado ilegítimo (*cryptojacking*) | Uso de equipos ajenos para minar criptomonedas |
| Amenazas por correo electrónico | Phishing, fraude del director general, suplantación de proveedores |
| Amenazas contra los datos | Filtraciones y robo de información |
| Amenazas contra la disponibilidad y la integridad | Denegación de servicio, alteración de sitios y servicios |
| Desinformación | Difusión deliberada de información falsa o engañosa |
| Amenazas no maliciosas | Errores humanos, configuraciones equivocadas, fallas físicas |
| Ataques a la cadena de suministro | Comprometer a un proveedor para llegar a sus clientes |

Los tipos de ataque, sus motivos y sus autores tienen una relación directa con las amenazas y con las vulnerabilidades de los sistemas. Entender esa relación es lo que permite reaccionar bien y elegir controles eficaces.

> Actualización al 2026: el *ENISA Threat Landscape 2025* (1 de octubre de 2025) analizó 4,875 incidentes ocurridos entre julio de 2024 y junio de 2025. Según el informe y sus resúmenes, la denegación de servicio impulsada por hacktivistas fue la mayor parte de los incidentes registrados, el phishing siguió siendo la principal vía de entrada, el ransomware el más dañino, y la frontera entre ciberdelincuencia, espionaje y hacktivismo es cada vez más difusa. La inteligencia artificial aparece a la vez como herramienta de los atacantes y como superficie de ataque nueva.

## Los tipos de control

Una vez entendido el contexto de riesgos, amenazas y ataques, en el entorno físico y en el ciberespacio, se pueden elegir controles adecuados a cada organización, Estado o persona. Por su función, los controles se agrupan en cinco tipos:

| Tipo | Función | Ejemplos |
|---|---|---|
| Preventivo | Anticipar y evitar que ocurra el evento no deseado; suele implantarse dentro de la organización | Control de acceso, cifrado, segregación de funciones |
| Detectivo | Encontrar lo que representa una amenaza para los activos y avisar | Detección de intrusos, revisión de bitácoras, cámaras con monitoreo |
| Correctivo | Reparar errores o daños una vez ocurrido el ataque | Restaurar respaldos, aplicar parches después del incidente |
| Disuasivo | Persuadir a una persona de no realizar una actividad; funciona como medida precautoria | Avisos de vigilancia, políticas con sanciones conocidas, guardias visibles |
| Compensatorio | Sustituir temporalmente un control que no se puede cumplir de inmediato; es la antesala del cumplimiento real | Revisión manual diaria mientras llega un sistema automatizado |

Dos precisiones conceptuales. Las **amenazas** son factores, a menudo externos, que no se controlan y que afectan la operación natural de los activos: lo que sí se controla son las vulnerabilidades y el impacto. Y un control compensatorio no es un control de segunda: es una respuesta legítima mientras se implanta el principal, siempre que se documente y tenga fecha de término.

## Glosario

| Término | Definición |
|---|---|
| Sociedad de la información | Transformación de las relaciones entre personas, gobiernos y organizaciones por el uso intensivo de las TIC |
| CMSI | Cumbre Mundial sobre la Sociedad de la Información, celebrada en Ginebra (2003) y Túnez (2005) |
| Gobernanza de internet | Desarrollo y aplicación compartida de principios, normas y procedimientos que dan forma al uso de internet |
| Valor del dato | Importancia que se concede a un dato combinada con el costo de mantenerlo o reponerlo |
| Ciberriesgo | Riesgo de pérdida financiera, interrupción o daño por fallas de las tecnologías digitales |
| Ciberespacio | Red interdependiente de infraestructuras de tecnologías de la información, internet incluida |
| Panorama de amenazas | Estudio periódico de las amenazas observadas, sus tendencias y sus actores |
| Ransomware | Código malicioso que cifra la información de la víctima para exigir un rescate |
| Control preventivo | Control que evita que ocurra un evento no deseado |
| Control detectivo | Control que identifica y notifica un evento no deseado |
| Control correctivo | Control que repara el daño después de un incidente |
| Control disuasivo | Control que desalienta a una persona de cometer una acción indebida |
| Control compensatorio | Control temporal que sustituye a otro que no se puede implantar de inmediato |

## Referencias

- Agencia de la Unión Europea para la Ciberseguridad. (2021). *ENISA threat landscape 2021*. ENISA. https://www.enisa.europa.eu/publications/enisa-threat-landscape-2021
- Agencia de la Unión Europea para la Ciberseguridad. (2025). *ENISA threat landscape 2025*. ENISA. https://www.enisa.europa.eu/publications/enisa-threat-landscape-2025
- Caralli, R. A., Stevens, J. F., Young, L. R., y Wilson, W. R. (2007). *Introducing OCTAVE Allegro: Improving the information security risk assessment process* (CMU/SEI-2007-TR-012). Software Engineering Institute. https://insights.sei.cmu.edu/library/introducing-octave-allegro-improving-the-information-security-risk-assessment-process/
- Committee of Sponsoring Organizations of the Treadway Commission. (2017). *Enterprise risk management: Integrating with strategy and performance*. COSO. https://www.coso.org/enterprise-risk-management
- International Organization for Standardization e International Electrotechnical Commission. (2022). *ISO/IEC 27005:2022 Information security, cybersecurity and privacy protection. Guidance on managing information security risks*. ISO. https://www.iso.org/standard/80585.html
- Unión Internacional de Telecomunicaciones. (2003). *Declaración de principios. Construir la sociedad de la información: un desafío global para el nuevo milenio* (WSIS-03/GENEVA/4-S). UIT. https://www.itu.int/net/wsis/docs/geneva/official/dop-es.html
- Unión Internacional de Telecomunicaciones. (2005). *Agenda de Túnez para la sociedad de la información* (WSIS-05/TUNIS/DOC/6(Rev.1)-S). UIT. https://www.itu.int/net/wsis/docs2/tunis/off/6rev1-es.html
- Videos BPS. (2018). *¿Cómo pueden las empresas extraer valor real de los datos?* [Video]. YouTube. https://youtu.be/xUbmi5rm8aA
- World Economic Forum. (2019). *The global risks report 2019* (14.ª ed.). WEF. https://www.weforum.org/publications/the-global-risks-report-2019/
- World Economic Forum. (2026). *Global risks report 2026* (21.ª ed.). WEF. https://www.weforum.org/publications/global-risks-report-2026/

---

*Rogelio Arriaga Gonzalez*
