# Introducción a la ciberseguridad

Curso de **Raptor Dev Services Academy** y primera materia de la ruta de ciberseguridad. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/cyber-intro/](https://academy.raptorcloud.dev/cyber-intro/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio, en [`12-seguridad/01-introduccion/`](../../12-seguridad/01-introduccion/README.md).

**Prerrequisito:** ninguno técnico. Basta con usar a diario un teléfono, una computadora y redes sociales, y querer entender qué pasa con la información propia y la ajena.
**Objetivo:** analizar la dependencia tecnológica que producen las TIC, ubicar sus beneficios en los servicios y actividades del ciberentorno, y explicar con cifras de fuentes reconocidas el riesgo que viene con ellas.
**Formato:** 5 unidades de una semana, unas 5 horas de estudio cada una (25 h en total). Cada unidad trae clase, lecturas, quiz, práctica (una discusión o un caso), tarea (un reto) y glosario.
**Para quién:** quien llega a la ciberseguridad desde el derecho, la administración, la docencia o el desarrollo y necesita el panorama completo antes de entrar a lo técnico.

---

## Por qué empieza aquí la ruta

Las TIC son una de las mejores herramientas que ha producido el ingenio humano y el medio más eficaz para ejercer el derecho de acceso a la información. También traen un lado menos visible: la inseguridad. Cada vez más relaciones personales, de trabajo, de negocio y con el gobierno ocurren en entornos digitales, y con ellas nacen relaciones jurídicas nuevas que pueden dañar la identidad, la reputación, el patrimonio o la intimidad de una persona, de una empresa o de un Estado.

Casi cualquier activo puede ser atacado o quedar expuesto por medio de la tecnología: la identidad, los sistemas, los equipos, los sitios web, las bases de datos, las imágenes, la información guardada en la nube y los datos personales. Por eso la ciberseguridad se estudia como un campo interdisciplinario. La Unión Internacional de Telecomunicaciones la describe, en su Recomendación X.1205 de 2008, como el conjunto de herramientas, políticas, salvaguardas, métodos de gestión del riesgo, prácticas y formación con que se protegen los activos de una organización, de los usuarios y de los gobiernos en el ciberentorno (UIT, 2008). Esta materia arma ese panorama; las siguientes de la ruta entran en riesgos concretos, informática forense, gestión de la seguridad y legislación.

---

## Cómo se trabaja cada semana

Una hora diaria alcanza si se reparte así:

| Días | Qué se hace |
|---|---|
| Lunes a miércoles | Clase y lecturas de la unidad; organizarse con el equipo; buscar fuentes externas que respalden lo que se va a decir; primera intervención en la práctica |
| Jueves a domingo | Quiz de la unidad; tarea (el reto); respuestas a los compañeros en la práctica |
| Domingo | Fecha de entrega de todo lo de la semana |

## Cómo se evalúa

| Unidad | Tarea (reto) | Práctica (discusión) | Quiz | Total |
|---|---|---|---|---|
| 1 | 7.5 | 7.5 | 5 | 20 |
| 2 | 10 | 5 | 5 | 20 |
| 3 | 7.5 | 7.5 | 5 | 20 |
| 4 | 7.5 | 7.5 | 5 | 20 |
| 5 | 10 | 5 | 5 | 20 |
| **Materia** | **42.5** | **32.5** | **25** | **100** |

Se acredita con 60 de 100, y además entregando al menos el 90 % de las actividades: una calificación aprobatoria con huecos de entrega no acredita.

## Reglas de trabajo: fuentes e inteligencia artificial

**Toda afirmación que no sea propia lleva su fuente, en APA 7.** Lo esencial:

- **Paráfrasis** (lo que se usa casi siempre): la idea del autor con palabras propias, sin comillas, y la cita al final: (UIT, 2008), o en forma narrativa: la UIT (2008) señala que...
- **Cita textual corta** (menos de 40 palabras): entre comillas y con la página: (Apellido, año, p. 12).
- **Cita textual larga** (40 palabras o más): en bloque aparte, sin comillas, con la página al final. En este curso se reserva para leyes y reglamentos.
- **Autores:** uno (Téllez, 2009); dos (Apellido y Apellido, año); tres o más (Primer apellido et al., año). Una institución con siglas se nombra completa la primera vez, Comisión Nacional de los Derechos Humanos [CNDH], y después solo con las siglas.
- **Lista de referencias:** al final, con el título "Referencias", en orden alfabético por el primer apellido o el nombre de la institución, con sangría francesa, y con el enlace a la fuente oficial (DOI si lo tiene).

| Tipo | Ejemplo de referencia |
|---|---|
| Libro | Téllez Valdés, J. (2009). *Derecho informático*. McGraw-Hill Interamericana. |
| Documento de un organismo | Unión Internacional de Telecomunicaciones. (2008). *Aspectos generales de la ciberseguridad* (Recomendación UIT-T X.1205). https://www.itu.int/rec/T-REC-X.1205-200804-I |
| Ley mexicana | Código Penal Federal. (1931). *Diario Oficial de la Federación*. Última reforma publicada en el DOF con su fecha. https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf |
| Página web | Autor o institución. (año, día de mes). *Título de la página*. Sitio. URL |

**Inteligencia artificial.** Se vale usarla como apoyo, con cuatro condiciones: decir para qué y cómo se usó, verificar lo que entrega contra fuentes reales (inventa referencias con toda tranquilidad), no pasarle datos personales ni sensibles, y no presentar como propio lo que escribió. Una herramienta de IA se cita como cualquier otra fuente.

**Netiqueta.** En las discusiones se debate la idea, no a la persona: lenguaje respetuoso, argumentos con fuente, y nada de datos personales propios ni ajenos.

---

## Unidad 1. TIC: elementos fundamentales (5 h)

> La tecnología es una herramienta; la línea entre herramienta y arma la traza quien la usa.

**Material:** [clase](u1-ict-fundamentals/slides.md), [práctica](u1-ict-fundamentals/practica.md), [tarea](u1-ict-fundamentals/tarea.md), [glosario](u1-ict-fundamentals/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** la tecnología como herramienta; sociedades de la información y del conocimiento; características, beneficios y desventajas de las TIC; informatización, brecha digital y neutralidad de la red; derecho a la información; infotecnología e infotech; exceso tecnológico y ciberadicción.
- [ ] **Lectura:** [`12-seguridad/01-introduccion/01-revolucion-sociotecnologica-y-tic.md`](../../12-seguridad/01-introduccion/01-revolucion-sociotecnologica-y-tic.md). [`02-era-digital-y-tic.md`](../../12-seguridad/01-introduccion/02-era-digital-y-tic.md). [`03-de-la-infotecnologia-al-infotech.md`](../../12-seguridad/01-introduccion/03-de-la-infotecnologia-al-infotech.md).
- [ ] **Práctica:** "¿Somos ciberadictos?": medir el uso propio durante tres días, contrastarlo con los criterios de la OMS y replicar a tres compañeros.
- [ ] **Tarea:** "El analfabetismo digital que me rodea": una encuesta anónima de 20 preguntas a por lo menos 10 personas distintas entre sí, y un informe en cinco apartados.
- **Evaluación:** tarea 7.5, práctica 7.5, quiz 5.

**Al terminar puedes:** distinguir la herramienta del riesgo, nombrar los derechos que sostienen el uso de internet y medir el analfabetismo digital de tu entorno.

---

## Unidad 2. Ciudadanía digital y ciberprevención (5 h)

> La ciudadanía no se apaga al entrar a internet: cambian las herramientas, no los derechos ni los deberes.

**Material:** [clase](u2-digital-citizenship/slides.md), [práctica](u2-digital-citizenship/practica.md), [tarea](u2-digital-citizenship/tarea.md), [glosario](u2-digital-citizenship/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** de la ciudadanía a la ciberciudadanía y lo que el Estado debe garantizar con las TIC; la identidad como derecho; ciberparticipación y ciberpoder; el criterio de la Suprema Corte sobre funcionarios que bloquean en redes; huella digital, in-seguridad y ciberprevención.
- [ ] **Lectura:** [`12-seguridad/01-introduccion/04-cambio-sociopolitico-y-tic.md`](../../12-seguridad/01-introduccion/04-cambio-sociopolitico-y-tic.md). [`05-ciberciudadania-y-ciberparticipacion.md`](../../12-seguridad/01-introduccion/05-ciberciudadania-y-ciberparticipacion.md). [`06-ciberseguridad-inseguridad-y-ciberprevencion.md`](../../12-seguridad/01-introduccion/06-ciberseguridad-inseguridad-y-ciberprevencion.md).
- [ ] **Práctica:** discusión con fundamento sobre si la libertad de expresión en redes sociales debe tener límites, y cuáles; réplica a tres compañeros.
- [ ] **Tarea:** ensayo de cinco cuartillas como máximo sobre un caso público: el funcionario de una alcaldía de la Ciudad de México que exhibía a infractores en transmisiones en vivo.
- **Evaluación:** tarea 10, práctica 5, quiz 5.

**Al terminar puedes:** distinguir participación de ciberactivismo y de hacktivismo, defender con fundamento hasta dónde llega la libertad de expresión en línea y aplicar una lista mínima de ciberprevención personal.

---

## Unidad 3. Ciberentorno: riesgos y vulnerabilidades (5 h)

> La seguridad absoluta no existe: lo que se puede hacer es anticiparse.

**Material:** [clase](u3-risks-vulnerabilities/slides.md), [práctica](u3-risks-vulnerabilities/practica.md), [tarea](u3-risks-vulnerabilities/tarea.md), [glosario](u3-risks-vulnerabilities/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** internet frente a ciberentorno; dato, información y comunicación; confidencialidad, integridad y disponibilidad; tecnologías disruptivas; riesgo frente a vulnerabilidad y cómo se estima un riesgo; riesgos de origen humano; ciberamenazas y respuesta a incidentes.
- [ ] **Lectura:** [`12-seguridad/01-introduccion/07-ciberentorno-aproximaciones-y-tendencias.md`](../../12-seguridad/01-introduccion/07-ciberentorno-aproximaciones-y-tendencias.md). [`08-riesgos-y-vulnerabilidades.md`](../../12-seguridad/01-introduccion/08-riesgos-y-vulnerabilidades.md). [`09-ciberamenazas-principales-dimensiones.md`](../../12-seguridad/01-introduccion/09-ciberamenazas-principales-dimensiones.md).
- [ ] **Práctica:** "¿Estamos o nos sentimos ciberamenazados?": separar la sensación de la exposición demostrable, tomar postura con datos recientes y replicar a tres compañeros.
- [ ] **Tarea:** "De la curiosidad a la vulnerabilidad": tres tecnologías del internet de las cosas, sus vulnerabilidades explicadas y cómo reducirlas.
- **Evaluación:** tarea 7.5, práctica 7.5, quiz 5.

**Al terminar puedes:** distinguir riesgo, vulnerabilidad, amenaza e incidente, estimar un riesgo como probabilidad por impacto y explicar qué hacer cuando ocurre un incidente.

---

## Unidad 4. Ciberdelitos y otras conductas atípicas (5 h)

> Que algo nos dañe no lo convierte en delito: delito es lo que la ley penal describe y castiga.

**Material:** [clase](u4-cybercrime/slides.md), [práctica](u4-cybercrime/practica.md), [tarea](u4-cybercrime/tarea.md), [glosario](u4-cybercrime/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** conducta, omisión y elementos del delito; tipicidad y conducta atípica; los artículos 211 bis del Código Penal Federal y las leyes especiales; lo que la ley todavía no alcanza; autoridades, peritos, prueba digital y cadena de custodia.
- [ ] **Lectura:** [`12-seguridad/01-introduccion/10-conductas-hechos-y-delitos.md`](../../12-seguridad/01-introduccion/10-conductas-hechos-y-delitos.md). [`11-ciberdelitos-en-mexico.md`](../../12-seguridad/01-introduccion/11-ciberdelitos-en-mexico.md). [`12-investigar-el-ciberdelito.md`](../../12-seguridad/01-introduccion/12-investigar-el-ciberdelito.md).
- [ ] **Práctica:** discusión con fundamento sobre la responsabilidad personal y social para evitar y combatir el ciberdelito hoy; réplica a dos compañeros.
- [ ] **Tarea:** infografía del cibercrimen en la nube, por servicio, riesgo y peligrosidad, con el nombre jurídico correcto de cada conducta.
- **Evaluación:** tarea 7.5, práctica 7.5, quiz 5.

**Al terminar puedes:** decir si un incidente es delito, cuál es y dónde está escrito, y a qué autoridad acudir sin poner en riesgo la prueba.

---

## Unidad 5. Desafíos globales asociados a las TIC (5 h)

> La tecnología sirve bien a quien la domina y domina a quien la usa sin pensar.

**Material:** [clase](u5-global-challenges/slides.md), [práctica](u5-global-challenges/practica.md), [tarea](u5-global-challenges/tarea.md), [glosario](u5-global-challenges/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** la tríada de confidencialidad, integridad y disponibilidad, y el modelo de múltiples partes interesadas; continuidad de TI, internet de las cosas, inteligencia artificial y tecnología operativa; identidad y herencia digital, OSINT, datos abiertos y redes inalámbricas; nube, cifrado, investigación penal en la nube, ciberresiliencia y metaverso.
- [ ] **Lectura:** [`12-seguridad/01-introduccion/13-disponibilidad-de-la-informacion.md`](../../12-seguridad/01-introduccion/13-disponibilidad-de-la-informacion.md). [`14-confidencialidad.md`](../../12-seguridad/01-introduccion/14-confidencialidad.md). [`15-integridad.md`](../../12-seguridad/01-introduccion/15-integridad.md).
- [ ] **Práctica:** debate con postura fundamentada en fuentes: "¿Ya estamos preparados para hacer ciberseguridad?", con dos o tres réplicas.
- [ ] **Tarea:** presentación de 20 láminas como máximo, "Mi visión actual sobre la ciberseguridad", con una tesis propia, lo que se espera a mediano plazo y la visión de futuro. Es el trabajo que integra la materia.
- **Evaluación:** tarea 10, práctica 5, quiz 5.

**Al terminar puedes:** explicar con casos reales por qué la tríada es un desafío global y proponer cómo proteger la información propia en los próximos años.

---

## Lo que sigue en la ruta

Esta materia arma el vocabulario y el panorama. Las siguientes entran en detalle: riesgos actuales (acoso, suplantación, fraudes y amenazas emergentes), informática forense, seguridad de la información con su sistema de gestión, y legislación nacional e internacional.

## Referencias generales de la materia

Las de cada tema están al final de cada nota. Estas son las que sostienen la presentación general:

- American Psychological Association. (2020). *Publication manual of the American Psychological Association* (7th ed.). https://apastyle.apa.org/products/publication-manual-7th-edition
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Código Penal Federal* (última reforma DOF 13-03-2026). https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf
- Comisión Nacional de los Derechos Humanos e Instituto Nacional de Estudios Históricos de las Revoluciones de México. (2015). *Derecho humano de acceso a la información*. https://web.archive.org/web/20260613060942/https://constitucion1917.gob.mx/work/models/Constitucion1917/Resource/1350/DH_Acceso_Informacion.pdf
- Organización para la Cooperación y el Desarrollo Económicos. (2016). *Proyecto OCDE/G20 de Erosión de la Base Imponible y Traslado de Beneficios: nota explicativa. Informes finales 2015*. https://www.oecd.org/es/publications/proyecto-beps-nota-explicativa_9789264263567-es
- Rodríguez, C. y Gil, S. (2014). *Ética y TIC*. CEAPAT-IMSERSO. http://www.ceapat.es/InterPresent1/groups/imserso/documents/binario/eticaytic.pdf
- Secretaría de Educación Pública. (2018). *¿Qué sabemos sobre ciudadanía digital en México?* Education Futures. https://educationfutures.com/understanding-digital-citizenship-in-mexico/
- TECNO2015 [seudónimo]. (2015). *Tecnología: ¿un beneficio o una adicción? La dependencia tecnológica* [Trabajo presentado al Concurso 2015]. Universidad Católica del Uruguay. https://web.archive.org/web/20230515162605/https://ucu.edu.uy/sites/default/files/facultad/dcsp/Concurso_2015/038_Tecno2015_tecnologia_un_beneficio_o_una_adicci%C3%B3n.pdf
- Téllez Valdés, J. (2009). *Derecho informático* (4.ª ed.). McGraw-Hill Interamericana. https://www.siia.unam.mx/siia-publico/v/include/modulo_productos/revistas.php?id=11679
- Unión Internacional de Telecomunicaciones. (2008). *Recomendación UIT-T X.1205: Aspectos generales de la ciberseguridad*. https://www.itu.int/rec/T-REC-X.1205-200804-I/es

> **Actualización al 2026:** el órgano garante del acceso a la información que menciona el folleto de 2015, el INAI, se extinguió en marzo de 2025; sus funciones de acceso a la información pasaron a Transparencia para el Pueblo, órgano desconcentrado de la Secretaría Anticorrupción y Buen Gobierno, y en marzo de 2025 se publicaron una nueva Ley General de Transparencia y una nueva ley federal de datos personales. El derecho de acceso sigue en el artículo 6o. constitucional.

---

*Rogelio Arriaga Gonzalez*
