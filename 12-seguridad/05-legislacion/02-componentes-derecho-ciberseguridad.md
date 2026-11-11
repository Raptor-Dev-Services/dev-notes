# 02 - Componentes del derecho de la ciberseguridad

> Materia 5 (Legislación nacional e internacional en ciberseguridad), unidad 1 (Derecho y ciberseguridad), tema 2. Nota recreada a partir del material de la materia y actualizada al 10 de noviembre de 2026.

## Por qué importa

La nota 01 justificó la existencia de un derecho de la ciberseguridad. Esta responde la pregunta siguiente: ¿de qué está hecho? Si se le pide a un abogado, a un ingeniero de seguridad y a un funcionario que describan "la regulación de la ciberseguridad en México", cada uno mirará una pieza distinta: el primero pensará en el Código Penal, el segundo en ISO/IEC 27001 y el tercero en la estrategia nacional. Los tres tienen razón y los tres se quedan cortos. El campo tiene seis componentes que funcionan juntos, y saber distinguirlos permite ubicar cualquier problema concreto (una fuga de datos, un fraude, una auditoría, una política institucional) en el lugar correcto del mapa.

## El mapa: seis componentes

| # | Componente | Pregunta que responde | Instrumentos típicos |
|---|---|---|---|
| 1 | Política pública e institucionalidad | ¿Qué se propone el país y quién lo coordina? | Estrategias, planes, agendas digitales, órganos responsables |
| 2 | Presencia de la ciberseguridad en el orden jurídico | ¿Qué leyes, nacionales e internacionales, la tocan? | Constitución, leyes federales y generales, tratados |
| 3 | Normas técnicas y estándares | ¿Cómo se hace bien, técnicamente? | ISO/IEC, NIST, guías de ENISA, NOM y normas mexicanas |
| 4 | Complemento de la privacidad y los datos personales | ¿Cómo se protegen las personas titulares? | Leyes de protección de datos, autoridades garantes |
| 5 | Relación con el derecho penal | ¿Qué conductas se castigan y cómo se investigan? | Tipos penales, evidencia digital, procuración de justicia |
| 6 | Cultura de ciberseguridad | ¿Cómo cambia la conducta de la gente? | Divulgación, prevención, autoprotección, formación |

Ninguno basta por sí solo. Una estrategia sin respaldo legal no obliga a nadie; una ley sin estándares técnicos no dice cómo cumplirla; un estándar sin cultura se queda en el papel. Las secciones que siguen recorren cada componente.

## 1. Política pública e institucionalidad

El primer componente es la planeación del Estado: estrategias, programas y agendas que fijan qué se quiere lograr en ciberseguridad a corto, mediano y largo plazo, y qué institución se hace cargo. Muchos países tienen planes o estrategias nacionales; en México, durante años, el único documento de este tipo fue la **Estrategia Nacional de Ciberseguridad (ENCS) de 2017**. Esa guía abarcaba aspectos tecnológicos y organizacionales, la cooperación entre instituciones dentro y fuera del país, la capacitación, la investigación y el desarrollo, el uso de normas técnicas y los aspectos jurídicos.

La institucionalidad importa tanto como el documento. Un plan necesita un responsable con atribuciones claras, presupuesto y continuidad entre administraciones; sin eso, la estrategia se vuelve una declaración de buenas intenciones.

**Actualización al 2026:** la ENCS de 2017 dejó de tener seguimiento y en la práctica perdió vigencia. En diciembre de 2025 la Agencia de Transformación Digital y Telecomunicaciones (ATDT) presentó un Plan Nacional de Ciberseguridad 2025-2030 y publicó en el DOF del 17 de diciembre de 2025 el acuerdo que emite la Política General de Ciberseguridad para la Administración Pública Federal (modelo de confianza cero, reporte de incidentes críticos en 24 horas, responsables institucionales de ciberseguridad). México sigue sin una ley específica de ciberseguridad; al cierre de esta nota había iniciativas en el Congreso, ninguna aprobada. La nota 06 desarrolla la estrategia de 2017 y su relevo.

## 2. La ciberseguridad en el orden jurídico

El segundo componente es la presencia de la ciberseguridad, explícita o implícita, en el conjunto de normas nacionales e internacionales. En México se trata sobre todo de leyes federales (reglamentarias de la Constitución, generales o propiamente federales) que regulan algún aspecto de la información, las redes o los sistemas. Algunos ejemplos:

| Ordenamiento | Relación con la ciberseguridad |
|---|---|
| Constitución Política de los Estados Unidos Mexicanos | Derecho de acceso a las TIC, libertad de difusión, protección de datos personales, inviolabilidad de las comunicaciones privadas |
| Ley en Materia de Telecomunicaciones y Radiodifusión (2025) | Redes, servicios, colaboración con la justicia, seguridad de las telecomunicaciones |
| Código Penal Federal | Acceso ilícito a sistemas y equipos de informática, revelación de secretos, fraude, delitos contra la intimidad |
| Código de Comercio | Comercio electrónico, mensajes de datos y firma electrónica |
| Ley de Instituciones de Crédito | Operaciones por medios electrónicos, secreto bancario, seguridad en la banca |
| Leyes de protección de datos personales | Deberes de seguridad del responsable, derechos de las personas titulares |

**Actualización al 2026:** el material original citaba la Ley Federal de Telecomunicaciones y Radiodifusión de 2014. Esa ley fue abrogada por la Ley en Materia de Telecomunicaciones y Radiodifusión, publicada en el DOF el 16 de julio de 2025. El Instituto Federal de Telecomunicaciones (IFT) se extinguió en octubre de 2025, cuando se integró el pleno de la nueva Comisión Reguladora de Telecomunicaciones, órgano ligado a la ATDT.

### Por qué el marco jurídico es indispensable

- La política pública necesita un sustento normativo: alguien tiene que estar facultado para ejecutarla, y sus actos tienen que tener fundamento legal.
- El marco jurídico da certeza sobre cómo se aplican las normas técnicas: cuáles son obligatorias y cuáles solo orientan, tanto en el sector público como en el privado.
- Bien diseñado, permite que todas las organizaciones adopten medidas de seguridad sin caer en la sobrerregulación, de modo que la economía aproveche la era digital y las tecnologías emergentes en lugar de frenarse.

## 3. Normas técnicas y estándares

El tercer componente lo forman los documentos técnicos que dicen cómo proteger tecnologías, datos e información. Hay muchísimos, publicados por organismos como la Organización Internacional de Normalización (ISO), la Comisión Electrotécnica Internacional (IEC), el NIST estadounidense o la agencia europea ENISA. Su peso no viene de una imposición estatal sino de cómo se construyen: participan expertos de todo el mundo, de sectores y perfiles muy distintos, y se revisan periódicamente.

Las normas de la familia ISO/IEC 27000 (en particular 27000, que presenta la familia y sus conceptos; 27001, de sistemas de gestión, y 27002, de controles) son la referencia central de la seguridad de la información. En México **no son obligatorias por sí mismas**, pero suelen servir de base para las normas mexicanas y para las Normas Oficiales Mexicanas, que sí son vinculantes cuando la autoridad las expide en su ámbito (la unidad 3 lo explica a fondo).

### ISO/IEC 27032 y la ciberseguridad

La norma ISO/IEC 27032, publicada en julio de 2012 con el título "Tecnologías de la información, técnicas de seguridad, directrices para la ciberseguridad", buscaba mejorar el estado de la ciberseguridad tomando en cuenta su relación con otros dominios:

- la seguridad de la información;
- la seguridad de las redes;
- la seguridad en internet;
- la seguridad de las aplicaciones;
- la protección de las infraestructuras críticas de información (CIIP, por sus siglas en inglés).

Esa edición entendía la ciberseguridad como la preservación de la confidencialidad, la integridad y la disponibilidad de la información **en el ciberespacio**. La lección de fondo sigue vigente: la ciberseguridad no es un problema solo tecnológico, y tampoco se resuelve solo con leyes. Involucra disciplinas y actores diversos, y las normas técnicas ayudan a orientar cómo se desarrollan y se usan las tecnologías.

**Actualización al 2026:** ISO/IEC 27032:2012 fue retirada. La reemplazó la segunda edición, ISO/IEC 27032:2023, con nuevo título ("Cybersecurity. Guidelines for Internet security") y nueva estructura: ahora se centra en la seguridad en internet, ofrece orientación de alto nivel para proteger la información y los servicios que se usan a través de la red y trae un anexo que relaciona sus controles con los de ISO/IEC 27002. La familia también se renovó: ISO/IEC 27001 y 27002 tienen ediciones de 2022 (la 27001, con una enmienda de 2024 sobre cambio climático), y en julio de 2026 salió la sexta edición de ISO/IEC 27000, que sustituye a la de 2018 y ya no es un documento de vocabulario.

## 4. Complemento de la vida privada y los datos personales

El cuarto componente ve la ciberseguridad como herramienta al servicio del derecho a la vida privada y a la protección de datos personales, tanto en el ámbito público como en el privado. Sin medidas de seguridad no hay privacidad posible: el deber de proteger los datos es, en buena parte, un deber de ciberseguridad.

En México el esquema descansa en dos leyes y en autoridades que vigilan su cumplimiento:

| Ley | A quién obliga |
|---|---|
| Ley Federal de Protección de Datos Personales en Posesión de los Particulares | Personas físicas y morales de carácter privado que tratan datos personales, con algunas excepciones |
| Ley General de Protección de Datos Personales en Posesión de Sujetos Obligados | Autoridades, órganos y organismos de los tres poderes y órdenes de gobierno, órganos autónomos, partidos políticos, fideicomisos y fondos públicos |

La autoridad de datos personales puede orientar, verificar, sancionar el tratamiento indebido y, cuando la conducta parece delito, dar vista a la autoridad ministerial.

**Actualización al 2026:** las dos leyes citadas por el material (de 2010 y 2017) fueron abrogadas y sustituidas por leyes nuevas con el mismo nombre, publicadas en el DOF el 20 de marzo de 2025. El órgano garante que mencionaba el material, el INAI, desapareció con la reforma constitucional de simplificación orgánica (DOF 20-12-2024). Para el sector privado, la autoridad es ahora la Secretaría Anticorrupción y Buen Gobierno, que sanciona infracciones con apercibimientos y multas de hasta 320,000 veces la Unidad de Medida y Actualización (art. 59). Para el sector público, la función se reparte entre esa Secretaría y las autoridades garantes de cada poder y órgano autónomo. Los delitos por tratamiento indebido de datos siguen en la ley de particulares (arts. 62 a 64). La nota 12 lo desarrolla.

## 5. Relación con el derecho penal

El quinto componente es el terreno de la responsabilidad penal. Aparece en dos situaciones:

- Cuando una conducta tipificada como delito afecta la información o un sistema de información (por ejemplo, modificar o destruir datos sin autorización).
- Cuando el daño final recae sobre otro derecho (el patrimonio, la intimidad, la libertad sexual), pero el primer paso del delito fue acceder o tratar indebidamente datos e información.

A este componente pertenecen también los procesos de investigación y procuración de justicia: la preservación de la evidencia digital, la cadena de custodia y la cooperación entre países para obtener pruebas que están en servidores extranjeros. La nota 11 revisa los tipos penales del Código Penal Federal.

## 6. Cultura de ciberseguridad

El último componente es el más amplio: divulgación, prevención, concientización, autoprotección y uso responsable de la tecnología y de la información. Se dirige a toda la población, desde niñas y niños de primaria hasta funcionarios de alto nivel, y se apoya en los principios de buena fe y colaboración entre actores. Sin cultura, los otros cinco componentes chocan con el eslabón más frágil: la persona que hace clic, reutiliza una contraseña o publica más de la cuenta.

## La mirada regional: ¿estamos preparados?

En 2016 el Banco Interamericano de Desarrollo (BID) y la Organización de los Estados Americanos (OEA) publicaron un estudio pionero sobre el grado de preparación en ciberseguridad de América Latina y el Caribe. Evaluó a cada país con un modelo de madurez de capacidades que incluye, entre otras variables, la dimensión regulatoria y el análisis de los documentos de política pública existentes. Su valor para esta unidad es doble: muestra dónde estaba la región y permite medir avances a partir de los documentos de política que se fueron aprobando.

Las conclusiones apuntan en la misma dirección que el resto de la materia:

- La complejidad del problema exige colaboración activa entre gobiernos, empresas y sociedad civil, con un enfoque multidisciplinario en el diagnóstico, el diseño de estrategias y la implementación.
- La coordinación entre organismos como el Comité Interamericano contra el Terrorismo de la OEA (CICTE) y el BID es clave para fortalecer a la región.
- Hace falta una agenda común que reconozca las diferencias entre países y siente bases para acciones conjuntas: armonizar la legislación regional y firmar acuerdos internacionales sobre protección de datos, cibercrimen y cooperación, para proteger mejor los derechos de las personas.

**Actualización al 2026:** el BID y la OEA publicaron en 2020 una segunda edición, el *Reporte Ciberseguridad 2020: riesgos, avances y el camino a seguir en América Latina y el Caribe*, que volvió a aplicar el modelo de madurez para medir cuánto se había avanzado desde 2016. Para datos actuales de la región conviene partir de esa edición y no de la de 2016.

## Un nuevo desafío para la comunidad internacional

El ciberespacio es un ámbito global que ha puesto en aprietos los paradigmas del derecho internacional. La economía digital crece rápido, pero también crece el cibercrimen entre países, y los especialistas tienen que repensar la noción tradicional de **soberanía**, que suponía fronteras claras y un territorio sobre el cual el Estado ejerce su jurisdicción. Un ataque puede originarse en un país, pasar por servidores de otros tres y afectar a víctimas en un quinto.

De ahí se desprenden tres tareas:

1. Construir alianzas y colaboración entre todos los actores para fortalecer a la sociedad y proteger derechos dentro y fuera de internet.
2. Madurar el diálogo internacional entre Estados para evitar que los incidentes escalen a conflictos bélicos o comerciales.
3. Llevar el ciberespacio a la agenda diplomática y crear mecanismos internacionales que prevengan la ciberguerra y el ciberterrorismo.

A medida que crecen los usuarios de internet y la sociedad de la información, el ciberespacio se vuelve un activo poderoso y, en la misma proporción, aumentan los ataques e incidentes. La diplomacia ya no puede tratarlo como un asunto técnico menor.

**Actualización al 2026:** desde que se escribió el material aparecieron instrumentos nuevos. La Asamblea General de la ONU adoptó en diciembre de 2024 la Convención de las Naciones Unidas contra la Ciberdelincuencia, abierta a firma en Hanói en octubre de 2025; según la Colección de Tratados de la ONU, en noviembre de 2026 sumaba 96 firmas y 3 ratificaciones y aún no entraba en vigor (requiere 40). México no aparece entre los firmantes en esa consulta y tampoco es parte del Convenio de Budapest, al que está invitado a adherirse. La nota 04 revisa estos foros.

## Lo que se lleva el tema

- El derecho de la ciberseguridad no es una ley: es un sistema de seis componentes que se necesitan entre sí.
- La política pública sin respaldo jurídico, sin responsable y sin presupuesto no se sostiene.
- Los estándares internacionales no obligan por sí mismos en México, pero orientan a las normas que sí obligan y son la referencia práctica de cualquier programa de seguridad.
- La protección de datos personales y la ciberseguridad son inseparables: el deber de seguridad es el núcleo de ambas.
- El derecho penal llega tarde por definición; la cultura de ciberseguridad es la que previene.
- La región avanza, pero necesita armonizar leyes y cooperar, porque el delito no respeta fronteras.

## Glosario

| Término | Definición |
|---------|-----------|
| Política pública de ciberseguridad | Plan del Estado que fija objetivos, responsables y acciones en ciberseguridad a corto, mediano y largo plazo |
| Institucionalidad | Existencia de órganos con atribuciones, presupuesto y continuidad para ejecutar una política |
| Estrategia Nacional de Ciberseguridad | Documento de planeación que define objetivos, principios y ejes de un país en la materia |
| Norma técnica | Documento que establece especificaciones o buenas prácticas para un producto, proceso o servicio |
| Estándar internacional | Norma técnica elaborada por consenso en un organismo internacional como ISO o IEC |
| ISO/IEC 27032 | Norma internacional de orientación sobre ciberseguridad; desde 2023, centrada en la seguridad en internet |
| CIIP | Protección de las infraestructuras críticas de información |
| Sobrerregulación | Exceso de obligaciones que encarece o frena la actividad sin mejorar la seguridad |
| Autoridad garante | Órgano que vigila el cumplimiento de las leyes de datos personales y sanciona su incumplimiento |
| Evidencia digital | Información en formato electrónico que puede servir como prueba en un proceso |
| Cultura de ciberseguridad | Conjunto de hábitos y conocimientos de prevención y autoprotección extendidos en la población |
| Modelo de madurez de capacidades | Instrumento que califica el avance de un país en varias dimensiones de la ciberseguridad |
| Soberanía digital | Capacidad del Estado de ejercer jurisdicción sobre lo que ocurre en redes que cruzan fronteras |
| Armonización jurídica | Proceso para que leyes de distintos países describan y sancionen las mismas conductas de forma compatible |

## Referencias

- Banco Interamericano de Desarrollo y Organización de los Estados Americanos. (2016). *Ciberseguridad: ¿estamos preparados en América Latina y el Caribe?* BID. https://publications.iadb.org/publications/spanish/viewer/Ciberseguridad-%C2%BFEstamos-preparados-en-Am%C3%A9rica-Latina-y-el-Caribe.pdf
- Banco Interamericano de Desarrollo y Organización de los Estados Americanos. (2020). *Reporte Ciberseguridad 2020: riesgos, avances y el camino a seguir en América Latina y el Caribe*. BID. https://publications.iadb.org/es/reporte-ciberseguridad-2020-riesgos-avances-y-el-camino-a-seguir-en-america-latina-y-el-caribe
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Código Penal Federal* (última reforma DOF 13-03-2026). https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Constitución Política de los Estados Unidos Mexicanos* (última reforma DOF 07-10-2026). https://www.diputados.gob.mx/LeyesBiblio/pdf/CPEUM.pdf
- Cámara de Diputados del H. Congreso de la Unión. (s. f.). *Código de Comercio* (texto vigente). https://www.diputados.gob.mx/LeyesBiblio/pdf/CCom.pdf
- Cámara de Diputados del H. Congreso de la Unión. (s. f.). *Ley de Instituciones de Crédito* (texto vigente). https://www.diputados.gob.mx/LeyesBiblio/pdf/LIC.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley en Materia de Telecomunicaciones y Radiodifusión* (DOF 16-07-2025). https://www.diputados.gob.mx/LeyesBiblio/pdf/LMTR.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley Federal de Protección de Datos Personales en Posesión de los Particulares* (DOF 20-03-2025, última reforma 14-11-2025). https://www.diputados.gob.mx/LeyesBiblio/pdf/LFPDPPP.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley General de Protección de Datos Personales en Posesión de Sujetos Obligados* (DOF 20-03-2025, última reforma 14-11-2025). https://www.diputados.gob.mx/LeyesBiblio/pdf/LGPDPPSO.pdf
- Carlini, A. (2016). *Ciberseguridad: un nuevo desafío para la comunidad internacional* (Documento de Opinión 67/2016). Instituto Español de Estudios Estratégicos. https://www.ieee.es/Galerias/fichero/docs_opinion/2016/DIEEEO67-2016_Ciberseguridad_Desafio_ComunidadInt_ACarlini.pdf
- Diario Oficial de la Federación. (2025, 17 de diciembre). *Acuerdo por el que se emite la Política General de Ciberseguridad para la Administración Pública Federal*. https://dof.gob.mx/nota_detalle.php?codigo=5776454&fecha=17/12/2025
- Gobierno de la República. (2017). *Estrategia Nacional de Ciberseguridad*. https://www.gob.mx/cms/uploads/attachment/file/271884/Estrategia_Nacional_Ciberseguridad.pdf
- International Organization for Standardization. (2012). *ISO/IEC 27032:2012. Information technology. Security techniques. Guidelines for cybersecurity* (retirada). https://www.iso.org/standard/44375.html
- International Organization for Standardization. (2023). *ISO/IEC 27032:2023. Cybersecurity. Guidelines for Internet security*. https://www.iso.org/standard/76070.html
- International Organization for Standardization. (2022). *ISO/IEC 27001:2022. Information security, cybersecurity and privacy protection. Information security management systems. Requirements*. https://www.iso.org/standard/27001
- International Organization for Standardization. (2026). *ISO/IEC 27000:2026. Information security, cybersecurity and privacy protection. Information security management systems. Overview*. https://www.iso.org/standard/27000
- Naciones Unidas. (2024). *Convención de las Naciones Unidas contra la Ciberdelincuencia* (estado de firmas y ratificaciones). Colección de Tratados de las Naciones Unidas. https://treaties.un.org/Pages/ViewDetails.aspx?src=TREATY&mtdsg_no=XVIII-16&chapter=18&clang=_en

---

*Rogelio Arriaga Gonzalez*
