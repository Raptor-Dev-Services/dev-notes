# Informática forense

Curso de **Raptor Dev Services Academy**, tercera materia de la ruta de ciberseguridad. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/digital-forensics/](https://academy.raptorcloud.dev/digital-forensics/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Las leyes se citan textuales y verificadas contra su texto vigente; los libros, por capítulo: cada quien consigue su copia.

**Prerrequisito:** las dos primeras materias de la ruta (fundamentos y riesgos de ciberseguridad), o experiencia equivalente en redes y sistemas operativos. No se pide formación jurídica.
**Objetivo:** aportar a una autoridad indicios digitales cuya integridad se pueda demostrar en cada etapa de la investigación, para que se acepten como evidencia en un asunto administrativo, laboral, mercantil o penal. Eso exige responder con método qué pasó, cómo, cuándo, dónde, por qué y con qué, cuando las tecnologías de la información fueron el fin o el medio de una conducta probablemente delictiva.
**Formato:** 5 unidades de 5 horas (25 h), una por semana. Cada unidad trae clase, quiz, práctica (discusión o caso), tarea y glosario.
**Para quién:** responsables de TI y de seguridad, peritos en formación, abogados y policías que necesitan entender la parte técnica, y desarrolladores que un día tendrán que entregar sus registros a una autoridad.

### Al terminar el curso puedes

- Aplicar las técnicas de la criminalística para identificar, fijar y preservar evidencia digital.
- Explicar el fenómeno de los ciberdelitos y su efecto en México y en el mundo.
- Ubicar el marco legal que sirve para perseguir a quien delinque con medios informáticos.
- Reconocer los documentos y estándares que rigen el tratamiento de la evidencia digital.
- Seguir una metodología forense, con técnicas y herramientas, que se sostenga ante el marco legal.

### Ritmo sugerido de cada semana

| Días | Qué se hace |
|---|---|
| Lunes a miércoles | Clase y lecturas de la unidad; buscar fuentes propias; primera aportación a la práctica |
| Jueves a domingo | Quiz; tarea; responder y comentar en la práctica |

Unos 60 minutos diarios bastan. Todo trabajo es propio y cita sus fuentes, incluido lo que se haga con apoyo de inteligencia artificial.

> "En toda pesquisa criminal, el tiempo que pasa es la verdad que huye." Edmond Locard

---

## Unidad 1. La criminalística y su relación con la informática forense (5 h)

> "La ciencia es el gran antídoto contra el veneno del entusiasmo y la superstición." Adam Smith

**Material:** [clase](u1-criminalistics/slides.md), [práctica](u1-criminalistics/practica.md), [tarea](u1-criminalistics/tarea.md), [glosario](u1-criminalistics/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** derecho positivo mexicano y jerarquía de normas, principios y técnicas de la criminalística aplicados a TIC, método científico (MC-14 y Arias Galicia) y el SGSI como fuente para el perito.
- [ ] **Lectura:** [`12-seguridad/03-forense/01-derecho-positivo-mexicano.md`](../../12-seguridad/03-forense/01-derecho-positivo-mexicano.md), [`12-seguridad/03-forense/02-criminalistica-tic.md`](../../12-seguridad/03-forense/02-criminalistica-tic.md), [`12-seguridad/03-forense/03-metodologia-investigacion.md`](../../12-seguridad/03-forense/03-metodologia-investigacion.md), [`12-seguridad/03-forense/04-tic-investigacion-ciberdelitos.md`](../../12-seguridad/03-forense/04-tic-investigacion-ciberdelitos.md).
- [ ] **Práctica:** actividad de apertura para presentarse al grupo, y foro sobre la utilidad de los métodos criminalísticos para investigar ciberdelitos, con el caso de un equipo que alguien apagó "para que nadie lo tocara".
- [ ] **Tarea:** identificar y fijar un equipo de cómputo encendido: cuadro sinóptico de las etapas del manejo de indicios, bitácora, serie fotográfica, croquis, registro de lo volátil y hash SHA-256 de todo lo generado.
- **Libros:** Lira Arteaga, *Cibercriminalidad: fundamentos de investigación en México*. Moreno González, *Introducción a la criminalística*. Montiel Sosa, *Criminalística*.

**Al terminar puedes:** fijar un equipo encendido sin perder lo volátil y justificar cada paso con la ley vigente y con el método científico.

---

## Unidad 2. Cibercriminalidad (5 h)

> "La tecnología es un siervo útil, pero un amo peligroso." Christian Lous Lange

**Material:** [clase](u2-cybercrime/slides.md), [práctica](u2-cybercrime/practica.md), [tarea](u2-cybercrime/tarea.md), [glosario](u2-cybercrime/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** qué es el ciberdelito (medio o fin), los CERT, catálogo de código malicioso, nube, teléfono, BYOD, troyanos financieros y hacktivismo; tendencias mundiales del material contrastadas con cifras de 2025 y 2026; el usuario mexicano, el caso SPEI y la clonación de tarjetas.
- [ ] **Lectura:** [`12-seguridad/03-forense/05-cibercriminalidad-fundamentos.md`](../../12-seguridad/03-forense/05-cibercriminalidad-fundamentos.md), [`12-seguridad/03-forense/06-cibercrimen-en-el-mundo.md`](../../12-seguridad/03-forense/06-cibercrimen-en-el-mundo.md), [`12-seguridad/03-forense/07-cibercrimen-en-mexico.md`](../../12-seguridad/03-forense/07-cibercrimen-en-mexico.md).
- [ ] **Práctica:** foro sobre si las tendencias mundiales del cibercrimen afectan a México: una postura con tres datos con fuente (uno reciente y oficial) y la réplica a tres compañeros.
- [ ] **Tarea:** mapa mental de los delitos informáticos de la lectura, con cada técnica marcada como medio o fin y una tabla que compara tres técnicas.

**Al terminar puedes:** reconocer las técnicas y el código malicioso detrás de un ciberdelito, ubicar a México en las tendencias mundiales y distinguir un dato histórico de uno vigente.

---

## Unidad 3. Marco normativo para la persecución de delincuentes informáticos (5 h)

> "El problema real no es si las máquinas piensan, sino si lo hacen los hombres." B. F. Skinner

**Material:** [clase](u3-legal-framework/slides.md), [práctica](u3-legal-framework/practica.md), [tarea](u3-legal-framework/tarea.md), [glosario](u3-legal-framework/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** el Convenio de Budapest y la cooperación internacional, conexión frente a contenido en la conservación de datos, de la técnica al tipo penal en el Código Penal Federal, y las leyes complementarias (telecomunicaciones, Código de Comercio, derecho de autor y banca).
- [ ] **Lectura:** [`12-seguridad/03-forense/08-convenio-de-budapest.md`](../../12-seguridad/03-forense/08-convenio-de-budapest.md), [`12-seguridad/03-forense/09-codigo-penal-federal.md`](../../12-seguridad/03-forense/09-codigo-penal-federal.md), [`12-seguridad/03-forense/10-leyes-complementarias.md`](../../12-seguridad/03-forense/10-leyes-complementarias.md).
- [ ] **Práctica:** foro sobre por qué México no se ha incorporado al Convenio de Budapest y qué consecuencias tiene, verificando antes su estatus actual en el DOF y con el Consejo de Europa.
- [ ] **Tarea:** infografía con la clasificación de los delitos del Convenio y su equivalente en la ley mexicana vigente.
- **Fuentes base:** Convenio sobre la Ciberdelincuencia (Consejo de Europa, 2001), Código Penal Federal y Ley en Materia de Telecomunicaciones y Radiodifusión (2025), que abrogó a la ley federal de 2014.

**Al terminar puedes:** ubicar una conducta cometida con TIC en su tipo penal y decir qué herramienta legal permite obtener cada dato que la prueba.

---

## Unidad 4. Fundamentos del tratamiento de evidencia digital (5 h)

> "No podemos culpar a la tecnología cuando comete errores." Tim Berners-Lee

**Material:** [clase](u4-digital-evidence/slides.md), [práctica](u4-digital-evidence/practica.md), [tarea](u4-digital-evidence/tarea.md), [glosario](u4-digital-evidence/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** quién investiga y quién dictamina, actos con y sin control judicial, intervención y extracción de comunicaciones, decálogo del perito, cadena de custodia (CNPP, Acuerdo A/009/15, ISO/IEC 27037), examen forense en cuatro pasos y actuación del primer respondiente.
- [ ] **Lectura:** [`12-seguridad/03-forense/11-fundamentos-legales-investigacion.md`](../../12-seguridad/03-forense/11-fundamentos-legales-investigacion.md), [`12-seguridad/03-forense/12-etica-cadena-custodia.md`](../../12-seguridad/03-forense/12-etica-cadena-custodia.md), [`12-seguridad/03-forense/13-obtencion-tratamiento-evidencia.md`](../../12-seguridad/03-forense/13-obtencion-tratamiento-evidencia.md).
- [ ] **Práctica:** discusión sobre la prueba digital en el proceso penal a partir de Bujosa Vadell y otros (2021), con un caso de cuatro actos para calificar su licitud con el CNPP.
- [ ] **Tarea:** video de 5 a 8 minutos y su guion que expliquen identificar, preservar, recolectar y asegurar evidencia en una escena electrónica, según la guía del NIJ para primeros respondientes.
- **Fuentes base:** CNPP; Acuerdo A/009/15 (DOF, 2015); NIJ (2004, 2008 y 2009); Bujosa Vadell, Bustamante Rúa y Toro Garzón (2021).

**Al terminar puedes:** decidir qué actos sobre un equipo requieren orden judicial, sostener una cadena de custodia completa para un indicio digital y actuar como primer respondiente sin alterar la evidencia.

---

## Unidad 5. Metodología, procedimientos y herramientas de la informática forense (5 h)

> "El éxito de la investigación en informática forense no depende de las herramientas informáticas utilizadas, sino de la metodología y el compromiso del investigador." Óscar Manuel Lira Arteaga

**Material:** [clase](u5-forensic-methodology/slides.md), [práctica](u5-forensic-methodology/practica.md), [tarea](u5-forensic-methodology/tarea.md), [glosario](u5-forensic-methodology/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** procedimientos de investigación, de la solicitud a la diligencia; el equipo encendido paso a paso; el árbol de decisiones para equipos apagados, servidores y discos cifrados; rastreo de páginas web y correos; teléfonos celulares; fraudes financieros; el dictamen pericial y su defensa en audiencia.
- [ ] **Lectura:** [`12-seguridad/03-forense/14-procedimientos-investigacion-forense.md`](../../12-seguridad/03-forense/14-procedimientos-investigacion-forense.md), [`12-seguridad/03-forense/15-identificacion-fijacion-preservacion.md`](../../12-seguridad/03-forense/15-identificacion-fijacion-preservacion.md), [`12-seguridad/03-forense/16-dictamen-pericial.md`](../../12-seguridad/03-forense/16-dictamen-pericial.md).
- [ ] **Práctica:** foro de cadena de custodia digital sobre un caso de fuga de información con laptop encendida, memoria USB, teléfono, servidor en producción y correo web: procedimiento por elemento, autorizaciones y consecuencias de romper la cadena; retroalimentar a tres compañeros.
- [ ] **Tarea:** un pódcast de 8 a 15 minutos, con su guion, que explique el análisis de un equipo de cómputo encendido etapa por etapa y cierre con por qué garantizar la prueba digital decide un juicio.
- **Libros:** Lira Arteaga, *Cibercriminalidad: fundamentos de investigación en México*, cap. V, pp. 540-554. NIJ, *Forensic Examination of Digital Evidence* (2004).

**Al terminar puedes:** seguir un caso de informática forense desde la solicitud de intervención hasta un dictamen firmado que otro perito puede repetir y un juez puede entender.

---

## Libros y fuentes base

| Obra | Autor o emisor | Año | Para qué |
|---|---|---|---|
| [Cibercriminalidad: fundamentos de investigación en México](https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/) | Óscar Manuel Lira Arteaga (Ubijus; 1.ª ed. INACIPE, 2010) | 2018 | Libro base de casi todas las unidades |
| [Ciberdelitos: perspectiva para su persecución](https://editorial.tirant.com/mex/libro/ciberdelitos-perspectiva-para-su-persecucion-oscar-manuel-lira-arteaga-9788491902980) | Óscar Manuel Lira Arteaga (Tirant lo Blanch) | 2019 | Persecución de ciberdelitos; profundización de las unidades 2 y 3 |
| [Intervención de comunicaciones en telefonía celular y su agravio a los derechos de la víctima](https://www.dijuris.com/shop/9786079389727-intervencion-de-comunicaciones-en-telefonia-celular-y-su-agravio-a-los-derechos-de-la-victima-1-a-ed-2017-79572) | Óscar Manuel Lira Arteaga (Ubijus) | 2017 | Intervención de comunicaciones; profundización de la unidad 4 |
| [Constitución Política de los Estados Unidos Mexicanos](https://www.diputados.gob.mx/LeyesBiblio/pdf/CPEUM.pdf) | Congreso de la Unión | 1917, última reforma DOF 07-10-2026 | Derechos, competencias y jerarquía normativa |
| [Código Penal Federal](https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf) | Congreso de la Unión | 1931, última reforma DOF 13-03-2026 | Tipos penales informáticos |
| [Código Nacional de Procedimientos Penales](https://www.diputados.gob.mx/LeyesBiblio/pdf/CNPP.pdf) | Congreso de la Unión | 2014, última reforma DOF 28-11-2025 | Investigación, cadena de custodia y prueba pericial |
| [Convenio sobre la Ciberdelincuencia](https://rm.coe.int/1680081561) | Consejo de Europa | 2001 | Marco internacional |
| [Electronic Crime Scene Investigation: An On-the-Scene Reference for First Responders](https://www.ojp.gov/pdffiles1/nij/227050.pdf) | National Institute of Justice | 2009 | Actuación del primer respondiente |

Actualización al 2026: el material original citaba el Código Penal Federal en su versión de 2014 y el CNPP en la de 2016; aquí se enlazan los textos vigentes, y las notas dicen en cada caso qué cambió.

---

*Rogelio Arriaga Gonzalez*
