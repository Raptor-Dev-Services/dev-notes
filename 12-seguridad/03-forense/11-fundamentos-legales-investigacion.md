# 11 - Fundamentos legales de la investigación

> Lectura del curso [Informática forense](../../cursos/digital-forensics/README.md), unidad 4, tema 1.

## Por qué importa

Un análisis técnico impecable se pierde si la copia del teléfono se sacó sin orden judicial, si la cadena de custodia tiene un hueco o si firma el informe alguien que no podía dictaminar. El juez no califica la destreza: revisa si el dato llegó al juicio por un camino lícito y documentado.

Antes de tocar un equipo hay que saber quién pidió la intervención, en calidad de qué participa uno y qué actos requieren permiso de un juez. Esta nota lo responde con el texto vigente de la Constitución (CPEUM), del Código Nacional de Procedimientos Penales (CNPP) y de la Ley de la Fiscalía General de la República (LFGR), y cierra con la doctrina reciente sobre prueba digital.

La cadena de custodia y la ética del perito tienen su propia nota ([nota 12](12-etica-cadena-custodia.md)); el procedimiento técnico de adquisición y análisis está en la [nota 13](13-obtencion-tratamiento-evidencia.md).

## De dónde viene el marco: del código federal al nacional

En el fuero federal rigió por décadas el Código Federal de Procedimientos Penales (CFPP) de 1934: sus artículos 123 y 181 obligaban a impedir que se perdieran o alteraran los indicios y a asegurar instrumentos y productos del delito, y desde 2009 sus artículos 123 Bis a 123 Quintus regularon la cadena de custodia. La reforma constitucional del 18 de junio de 2008 sustituyó el sistema mixto y escrito por uno acusatorio y oral, y el CNPP (DOF 05-03-2014) unificó las reglas para la federación y las entidades.

Actualización al 2026: el CFPP está abrogado. El CNPP rige en todo el país desde el 18 de junio de 2016, plazo máximo de su segundo transitorio, y su última reforma es del 28-11-2025. Los artículos 123 y 181 del CFPP solo se aplican a procedimientos iniciados antes de la transición.

## Quién investiga: el Ministerio Público y sus auxiliares

La pieza central está en el artículo 21 constitucional. La investigación de un delito no le pertenece al perito ni a la policía: la conduce el Ministerio Público.

CPEUM, artículo 21, primer párrafo (texto vigente, última reforma DOF 07-10-2026):

> La investigación de los delitos corresponde al Ministerio Público, a la secretaría del ramo de seguridad pública del Ejecutivo Federal, a la Guardia Nacional y a las policías, en el ámbito de su competencia, las cuales actuarán bajo la conducción y mando de aquél en el ejercicio de esta función.

Los párrafos siguientes reservan al Ministerio Público el ejercicio de la acción penal y a la autoridad judicial, en exclusiva, la imposición de las penas.

Actualización al 2026: el primer párrafo se reformó en septiembre y diciembre de 2024. Antes solo mencionaba al Ministerio Público y a "las policías"; ahora nombra también a la secretaría federal de seguridad pública y a la Guardia Nacional, siempre bajo el mando del Ministerio Público.

Consecuencia práctica: quien integra la carpeta de investigación y decide qué se persigue es el agente del Ministerio Público; en el fuero federal, el de la Federación, adscrito a la Fiscalía General de la República.

### Policías y peritos no producen el mismo documento

Los auxiliares del Ministerio Público se reparten el trabajo de forma distinta, y la diferencia se nota en lo que firman:

| Auxiliar | Qué hace con la posible evidencia | Qué documento produce |
|---|---|---|
| Policía (de investigación, Guardia Nacional, policía con capacidades para procesar) | Protege el lugar, preserva y, si no hay especialista, recolecta y embala | Informe policial |
| Perito (servicios periciales) | Trata y analiza los indicios con método científico | Dictamen pericial |
| Analista | Cruza información y propone líneas de investigación | Productos de análisis |

El CNPP lo deja escrito en las obligaciones de la policía. Del artículo 132 interesan dos fracciones.

CNPP, artículo 132, fracciones VIII y XIV (texto vigente):

> VIII. Preservar el lugar de los hechos o del hallazgo y en general, realizar todos los actos necesarios para garantizar la integridad de los indicios. En su caso deberá dar aviso a la Policía con capacidades para procesar la escena del hecho y al Ministerio Público conforme a las disposiciones previstas en este Código y en la legislación aplicable;

> XIV. Emitir el informe policial y demás documentos, de conformidad con las disposiciones aplicables. Para tal efecto se podrá apoyar en los conocimientos que resulten necesarios, sin que ello tenga el carácter de informes periciales, y

Aunque un policía sepa de informática, lo que escribe es un informe y no un peritaje: el sustento científico sigue siendo trabajo del perito. Si no hay perito de la especialidad en la diligencia, la policía resguarda lugar e indicios e informa lo que encontró y lo que hizo.

### La estructura federal: de la PGR a la FGR

La antigua Ley Orgánica de la Procuraduría General de la República (artículo 22) clasificaba a los auxiliares en directos (oficiales ministeriales, policía federal ministerial, policía federal y servicios periciales) y suplementarios (como la policía federal preventiva y los ministerios públicos locales).

Actualización al 2026: la PGR se transformó en la Fiscalía General de la República (FGR), autónoma, en diciembre de 2018; la ley que hoy la rige es la LFGR (DOF 20-05-2021, última reforma 14-11-2025). La Policía Federal se extinguió en 2019 y la sustituyó la Guardia Nacional. El CNPP todavía dice "Procuraduría" y "Procurador" (artículos 291 y 303): se leen como referencias a la FGR y a su titular.

En la LFGR, el artículo 42 encarga a los peritos emitir dictámenes a solicitud del Ministerio Público, auxiliar en la búsqueda y preservación de indicios y acudir al lugar de los hechos. El artículo 43 protege la independencia del criterio técnico.

Ley de la Fiscalía General de la República, artículo 43 (texto vigente):

> Artículo 43. Las personas peritas en ejercicio de su encargo tienen libertad y autonomía técnica para emitir y determinar el sentido de sus informes, opiniones o dictámenes, por lo que las solicitudes de las personas agentes del Ministerio Público de la Federación o de las personas Policías Federales Ministeriales no influyen, dirigen, condicionan ni afectan los criterios que emitan en sus informes, documentos, opiniones y dictámenes.

## Las etapas del procedimiento penal acusatorio

Ubicar en qué etapa está un caso ayuda a saber qué se espera del perito. El artículo 211 del CNPP divide el procedimiento en tres etapas, y la primera tiene dos fases.

| Etapa | Empieza | Termina | Papel típico del perito en TIC |
|---|---|---|---|
| Investigación inicial | Con la denuncia o querella | Cuando el imputado queda a disposición del juez de control para la imputación | Procesar el lugar, asegurar equipos, adquirir copias y emitir primeros dictámenes |
| Investigación complementaria | Con la formulación de la imputación | Con el cierre de la investigación | Ampliar análisis, contestar nuevas preguntas del Ministerio Público |
| Intermedia | Con la acusación | Con el auto de apertura a juicio | Su dictamen se ofrece como medio de prueba; la defensa puede cuestionarlo |
| Juicio | Con el auto de apertura | Con la sentencia | Declarar en audiencia y sostener el método frente al interrogatorio |

## El CNPP en lo que toca al investigador de TIC

El CNPP es de orden público y aplica en todo el país a delitos federales y locales (artículo 1). Su objeto (artículo 2) es esclarecer los hechos, proteger al inocente, evitar la impunidad y reparar el daño con respeto a los derechos humanos: encontrar la verdad sin atropellar derechos explica casi todas las reglas que siguen.

### Lo que le toca al Ministerio Público

El artículo 127 le da la conducción de la investigación y la coordinación de policías y peritos. De las obligaciones del artículo 131, las que más tocan a la evidencia digital son: vigilar los derechos humanos (fracción I), recibir denuncias incluso por medios digitales o anónimas (II), ordenar o supervisar que los indicios no se pierdan ni alteren y que se sigan los protocolos de preservación (IV), pedir peritajes e informes (VII y IX) y solicitar al juez de control los actos que lo requieran (X).

### El deber de investigar

Con noticia de un posible delito, el Ministerio Público dirige la investigación sin suspenderla salvo excepción legal, y debe hacerlo de forma inmediata, exhaustiva e imparcial (artículo 212). Su fin es reunir indicios y, en su caso, datos de prueba para la acción penal, la acusación y la reparación (artículo 213). Arranca por denuncia, querella o equivalente; si la noticia es anónima, la policía primero verifica que sea cierta (artículo 221).

### Cadena de custodia y aseguramiento

Los artículos 227 y 228 (citados en la [nota 12](12-etica-cadena-custodia.md)) definen la cadena de custodia y a sus responsables: lo es cualquiera que tenga contacto con el indicio, y un indicio alterado conserva su valor salvo que se compruebe que la alteración lo volvió ineficaz. No es permiso para descuidarse: en evidencia digital, una falla documental basta para que la defensa siembre duda sobre la integridad. El artículo 229 ordena asegurar instrumentos, objetos y productos del delito relacionados con el lugar de los hechos, con controles de resguardo según su naturaleza; un servidor, un teléfono o una memoria USB entran ahí.

### Actos con y sin control judicial

Esta distinción es la que más errores evita. El CNPP separa lo que la policía y el Ministerio Público pueden hacer por su cuenta (artículo 251) de lo que necesita autorización previa del juez de control (artículo 252).

| Sin autorización previa del juez (art. 251) | Con autorización previa del juez (art. 252) |
|---|---|
| Inspección del lugar de los hechos o del hallazgo, y de otros lugares | Órdenes de cateo |
| Inspección de personas y de vehículos; revisión corporal | Intervención de comunicaciones privadas y correspondencia |
| Levantamiento e identificación de cadáver | Exhumación de cadáveres |
| Aportación de comunicaciones entre particulares | Toma de muestras corporales cuando la persona se niega (salvo víctima u ofendido) |
| Reconocimiento de personas y entrevista de testigos | Examen físico de una persona que se niega |
| Entrega vigilada y operaciones encubiertas (con autorización del titular de la fiscalía) | Todo acto que afecte derechos constitucionales y los que señalen otras leyes |

La fila clave para el perito informático es la segunda de la derecha. Revisar un celular asegurado para leer sus conversaciones no es una inspección cualquiera: es una intervención de comunicaciones privadas o una extracción de información, y requiere orden judicial. La excepción es la del artículo 276: uno de los participantes de la conversación puede entregarla por su voluntad, siempre que tenga relación estrecha con el delito y no rompa un deber de confidencialidad (salvo consentimiento expreso de quien está protegido por ese deber).

## Las comunicaciones privadas: el terreno más delicado

### La regla constitucional

El artículo 16 constitucional declara inviolables las comunicaciones privadas, sanciona penalmente atentar contra ellas y admite como excepción que un participante las aporte de forma voluntaria. Quién puede ordenar una intervención lo dice el párrafo siguiente.

CPEUM, artículo 16, párrafo sobre la intervención de comunicaciones (texto vigente):

> Exclusivamente la autoridad judicial federal, a petición de la autoridad federal que faculte la ley o del titular del Ministerio Público de la entidad federativa correspondiente, podrá autorizar la intervención de cualquier comunicación privada. Para ello, la autoridad competente deberá fundar y motivar las causas legales de la solicitud, expresando además, el tipo de intervención, los sujetos de la misma y su duración. La autoridad judicial federal no podrá otorgar estas autorizaciones cuando se trate de materias de carácter electoral, fiscal, mercantil, civil, laboral o administrativo, ni en el caso de las comunicaciones del detenido con su defensor.

### La intervención y la extracción en el CNPP

El artículo 291 es el más importante para quien analiza dispositivos: desde 2016 exige orden judicial también para extraer información de cualquier equipo. Su primer párrafo faculta al titular de la fiscalía federal (el texto aún dice Procuraduría) o a quien delegue, y a los de las entidades, para pedir la autorización al juez federal de control expresando objeto y necesidad.

CNPP, artículo 291, párrafos segundo a cuarto (texto vigente; reformados y adicionado DOF 17-06-2016):

> La intervención de comunicaciones privadas, abarca todo sistema de comunicación, o programas que sean resultado de la evolución tecnológica, que permitan el intercambio de datos, informaciones, audio, video, mensajes, así como archivos electrónicos que graben, conserven el contenido de las conversaciones o registren datos que identifiquen la comunicación, los cuales se pueden presentar en tiempo real.
>
> La solicitud deberá ser resuelta por la autoridad judicial de manera inmediata, por cualquier medio que garantice su autenticidad, o en audiencia privada con la sola comparecencia del Ministerio Público, en un plazo que no exceda de las seis horas siguientes a que la haya recibido.
>
> También se requerirá autorización judicial en los casos de extracción de información, la cual consiste en la obtención de comunicaciones privadas, datos de identificación de las comunicaciones; así como la información, documentos, archivos de texto, audio, imagen o video contenidos en cualquier dispositivo, accesorio, aparato electrónico, equipo informático, aparato de almacenamiento y todo aquello que pueda contener información, incluyendo la almacenada en las plataformas o centros de datos remotos vinculados con éstos.

Quienes ejecutan la medida responden de que se haga en los términos exactos de la resolución.

Actualización al 2026: antes de la reforma del 17-06-2016, el segundo párrafo decía que las comunicaciones podían presentarse "en tiempo real o con posterioridad"; hoy dice solo "en tiempo real", y lo almacenado quedó cubierto por el párrafo nuevo de extracción de información.

El artículo 292 fija los requisitos de la solicitud (fundada y motivada; personas, lugares, tipo de comunicación, duración, procedimiento, líneas o equipos y, en su caso, la empresa concesionaria) y su límite de tiempo.

CNPP, artículo 292, segundo párrafo (texto vigente):

> El plazo de la intervención, incluyendo sus prórrogas, no podrá exceder de seis meses. Después de dicho plazo, sólo podrán autorizarse nuevas intervenciones cuando el Ministerio Público acredite nuevos elementos que así lo justifiquen.

El tope de seis meses vale igual para telefonía que para sistemas informáticos, porque el artículo 294 abarca cualquier forma de comunicar, desde la oral hasta la que pasa por equipos informáticos, y repite las materias excluidas y la comunicación del detenido con su defensor.

Los artículos siguientes regulan lo que pasa durante y después de la medida:

| Artículo | Regla | Por qué le importa al perito |
|---|---|---|
| 293 | El juez fija características, modalidades y límites, y puede ordenar a instituciones públicas o privadas cómo colaborar | Lo que no está en la autorización no se hace |
| 295 | Si aparece un delito distinto, se anota en el registro para abrir otra investigación | El hallazgo casual se documenta, no se explota por cuenta propia |
| 296 | Ampliar la intervención a otras personas o lugares exige nueva solicitud al mismo juez | El alcance no se estira de facto |
| 297 y 298 | La intervención se registra por un medio que no altere fidelidad, autenticidad ni contenido; el registro lleva fechas, inventario detallado, participantes y numeración progresiva del original y del duplicado | Es una cadena de custodia específica para comunicaciones |
| 299 | Al terminar, la policía o el perito informa de inmediato al Ministerio Público y levanta acta; lo hecho sin autorización o fuera de sus términos carece de valor probatorio | Un exceso técnico anula la prueba |
| 300 | El juez ordena destruir los registros ajenos a la investigación o fuera de autorización | La copia de trabajo también tiene fin |
| 301 | Concesionarios y titulares de los medios deben colaborar y tener capacidad técnica para ejecutar la orden | Base para pedir apoyo a operadores y plataformas |
| 302 | Deber de secrecía de quienes participan | La confidencialidad es obligación legal, no cortesía |

### Localización geográfica y datos conservados

El artículo 303 permite que el titular de la fiscalía pida al juez de control que ordene a concesionarios, autorizados y proveedores de aplicaciones y contenidos la localización en tiempo real de un equipo móvil o la entrega de datos conservados. Su párrafo de excepción es el que más se usa en casos de urgencia.

CNPP, artículo 303, párrafos sexto y séptimo (texto vigente):

> Excepcionalmente, cuando esté en peligro la integridad física o la vida de una persona o se encuentre en riesgo el objeto del delito, así como en hechos relacionados con la privación ilegal de la libertad, secuestro, extorsión o delincuencia organizada, el Procurador, o el servidor público en quien se delegue la facultad, bajo su más estricta responsabilidad, ordenará directamente la localización geográfica en tiempo real o la entrega de los datos conservados a los concesionarios de telecomunicaciones, los autorizados o proveedores de servicios de aplicaciones y contenidos, quienes deberán atenderla de inmediato y con la suficiencia necesaria. A partir de que se haya cumplimentado el requerimiento, el Ministerio Público deberá informar al Juez de control competente por cualquier medio que garantice su autenticidad, dentro del plazo de cuarenta y ocho horas, a efecto de que ratifique parcial o totalmente de manera inmediata la subsistencia de la medida, sin perjuicio de que el Ministerio Público continúe con su actuación.
>
> Cuando el Juez de control no ratifique la medida a que hace referencia el párrafo anterior, la información obtenida no podrá ser incorporada al procedimiento penal.

El último párrafo del mismo artículo permite además pedir la conservación inmediata de datos en redes, sistemas o equipos informáticos hasta por noventa días, y remite a las obligaciones de conservación del artículo 190, fracción II, de la Ley Federal de Telecomunicaciones y Radiodifusión.

Actualización al 2026: esa ley fue abrogada por la Ley en Materia de Telecomunicaciones y Radiodifusión (LMTR), publicada en el DOF el 16 de julio de 2025; según los transitorios Sexto y Octavo de la LMTR, la abrogación y la extinción del IFT surtieron efecto en octubre de 2025, al día siguiente de que se integró el Pleno de la Comisión Reguladora de Telecomunicaciones. La obligación de conservar los registros de comunicaciones pasó al artículo 183, fracción II, de la LMTR: los datos se guardan doce meses en sistemas de consulta y entrega en tiempo real, y doce meses más en almacenamiento electrónico; los de ese segundo periodo se entregan dentro de las 48 horas siguientes a la notificación de la solicitud. El CNPP no se ha armonizado y sigue citando la ley anterior. El detalle está en la [nota 10](10-leyes-complementarias.md).

## Datos de prueba, medios de prueba y pruebas

El CNPP usa tres palabras que en la práctica se confunden. Conviene separarlas porque cada una corresponde a un momento del procedimiento.

CNPP, artículo 261 (texto vigente):

> Artículo 261. Datos de prueba, medios de prueba y pruebas
>
> El dato de prueba es la referencia al contenido de un determinado medio de convicción aún no desahogado ante el Órgano jurisdiccional, que se advierta idóneo y pertinente para establecer razonablemente la existencia de un hecho delictivo y la probable participación del imputado.
>
> Los medios o elementos de prueba son toda fuente de información que permite reconstruir los hechos, respetando las formalidades procedimentales previstas para cada uno de ellos.
>
> Se denomina prueba a todo conocimiento cierto o probable sobre un hecho, que ingresando al proceso como medio de prueba en una audiencia y desahogada bajo los principios de inmediación y contradicción, sirve al Tribunal de enjuiciamiento como elemento de juicio para llegar a una conclusión cierta sobre los hechos materia de la acusación.

Para el perito: la imagen forense y su hash, obtenidos en la investigación inicial, sostienen datos de prueba; el dictamen y la declaración son el medio de prueba; y solo lo desahogado en juicio, con la defensa pudiendo contradecirlo, es prueba. Por eso el artículo 259 admite cualquier medio lícito, pide valoración libre y lógica y niega valor de sentencia a los antecedentes de investigación (registros de la carpeta, artículo 260) salvo excepciones.

Dos artículos funcionan como filtro de licitud. El 263 exige que datos y pruebas se obtengan, produzcan y reproduzcan lícitamente, y el 264 dice qué pasa si no.

CNPP, artículo 264 (texto vigente):

> Artículo 264. Nulidad de la prueba
>
> Se considera prueba ilícita cualquier dato o prueba obtenidos con violación de los derechos fundamentales, lo que será motivo de exclusión o nulidad.
>
> Las partes harán valer la nulidad del medio de prueba en cualquier etapa del proceso y el juez o Tribunal deberá pronunciarse al respecto.

El artículo 265 obliga al juez a justificar el valor que asigna a cada dato y prueba con una apreciación conjunta, integral y armónica. El 359 lleva esa exigencia al juicio oral: el tribunal debe explicar por qué creyó o descartó cada prueba desahogada, y solo puede condenar si queda convencido de la culpabilidad más allá de toda duda razonable; si la duda persiste, absuelve. Para el perito significa que su método se va a examinar, no solo su conclusión.

## El perito y el peritaje

### Cuándo y cómo se pide un peritaje

El artículo 272 permite que el Ministerio Público, o la policía con su conocimiento, ordene los peritajes que necesite durante la investigación, y aclara que entregar el dictamen por escrito no libera al perito de ir a declarar al juicio. El artículo 273 garantiza al perito acceso a los indicios sobre los que dictamina en todo momento.

El artículo 274 regula el peritaje irreproducible: si el estudio consume el objeto y otro perito ya no podrá repetirlo, el Ministerio Público avisa a la defensa para que su perito participe o presencie. En informática pasa al desoldar el chip de un teléfono dañado o al adquirir en vivo la memoria RAM, que cambia con cada operación. El artículo 275 prevé equipos interdisciplinarios para peritajes especiales, como los de víctimas de agresión sexual, para no repetirles entrevistas.

### Quién puede ser perito

El artículo 368 abre la prueba pericial a cualquier caso en que se necesiten conocimientos especiales de una ciencia, arte, técnica u oficio. El 369 dice quién puede rendirla.

CNPP, artículo 369 (texto vigente):

> Artículo 369. Título oficial
>
> Los peritos deberán poseer título oficial en la materia relativa al punto sobre el cual dictaminarán y no tener impedimentos para el ejercicio profesional, siempre que la ciencia, el arte, la técnica o el oficio sobre la que verse la pericia en cuestión esté reglamentada; en caso contrario, deberá designarse a una persona de idoneidad manifiesta y que preferentemente pertenezca a un gremio o agrupación relativa a la actividad sobre la que verse la pericia.

Ese requisito no aplica a quien declara como testigo de algo que presenció por casualidad, aunque al contarlo use sus conocimientos técnicos.

En TIC, lo que acredita al perito es el título oficial de la disciplina (ingeniería en computación o en sistemas, por ejemplo); si la especialidad no está reglamentada, se admite idoneidad manifiesta, de preferencia avalada por una agrupación del ramo. Las certificaciones de herramientas ayudan, pero no sustituyen el requisito.

## La prueba digital vista desde la doctrina

Bujosa Vadell, Bustamante Rúa y Toro Garzón (2021) comparan cómo España y Colombia obtienen, admiten y valoran la prueba digital de la vigilancia secreta (interceptaciones, registros remotos, agente encubierto informático, geolocalización). Sus conclusiones sirven para leer el CNPP.

**Qué la hace distinta.** Siguiendo a De la Torre Rodríguez, los autores describen la prueba digital como intangible (solo se aprecia mediante procesos informáticos), replicable (se copia sin límite, lo que complica saber cuál es el original), volátil, fácil de borrar aunque el soporte sobreviva, y a menudo parcial, repartida en muchos archivos y lugares.

**Autenticidad e integridad.** Por esas características, la cadena de custodia tiene que demostrar dos cosas distintas: que la fuente es la que se dice (autenticidad) y que nadie la modificó (integridad). A esas dos los autores suman, con base en la jurisprudencia constitucional colombiana, la inalterabilidad, la rastreabilidad hasta la fuente original, la recuperabilidad para consultarla después y la conservación en el tiempo.

**Admisión y valoración.** Para admitirla, el juez revisa necesidad, pertinencia, utilidad y legalidad, y la doctrina añade licitud, integridad, autenticidad y claridad. Para valorarla se distinguen requisitos intrínsecos (autenticidad, integridad, fiabilidad, disponibilidad) y extrínsecos (legalidad, es decir, que se cumplieron los requisitos legales; y licitud, que no se violaron derechos fundamentales). Si una interceptación se hizo violando un derecho fundamental, no tiene valor y no puede vencer la presunción de inocencia, idea que coincide con los artículos 264 y 299 del CNPP.

**El riesgo al valorar.** Los autores advierten dos sesgos: confundir lo que muestra un video, una imagen o un registro con la realidad misma, cuando es solo una representación posiblemente parcial; y el "embelesamiento acrítico" que lleva a aceptar sin discusión todo lo que viene de una herramienta tecnológica. Proponen interrogar como perito a quien obtuvo los datos, para que la defensa pueda contradecir el método; en México lo cumple la declaración del perito en juicio (artículo 272).

## Errores comunes

- **Revisar el celular "solo para ver" antes de tener la orden.** Es extracción de información sin autorización (artículo 291) y lo obtenido es prueba ilícita.
- **Confundir la aportación voluntaria con la intervención.** La víctima puede entregar sus propios chats (artículo 276); la autoridad no puede tomar los del imputado sin juez.
- **Que el policía firme un "peritaje".** Su documento es un informe (artículo 132, fracción XIV); el dictamen corresponde al perito.
- **Exceder el alcance autorizado.** Lo que se obtiene fuera de los términos de la orden carece de valor (artículo 299).
- **Citar normas abrogadas como vigentes.** CFPP, Ley Orgánica de la PGR o Ley Federal de Telecomunicaciones y Radiodifusión ya no rigen; se citan solo como antecedente.
- **Pensar que el dictamen escrito basta.** El perito tiene que sostenerlo en audiencia (artículo 272).

## Glosario

| Término | Definición |
|---------|-----------|
| Ministerio Público | Autoridad que conduce la investigación, coordina a policías y peritos y ejerce la acción penal |
| Juez de control | Juez que autoriza los actos de investigación que afectan derechos y vigila la legalidad de la etapa de investigación |
| Tribunal de enjuiciamiento | Órgano que desahoga las pruebas en el juicio oral y dicta sentencia |
| Carpeta de investigación | Conjunto de registros que integra el Ministerio Público durante la investigación |
| Informe policial | Documento en que la policía relata su actuación y lo que encontró; no tiene carácter pericial |
| Dictamen pericial | Opinión técnica y científica que emite por escrito un perito sobre lo que se sometió a su estudio |
| Dato de prueba | Referencia al contenido de un medio de convicción aún no desahogado, útil para sostener la probable existencia del delito |
| Medio de prueba | Fuente de información que permite reconstruir los hechos con las formalidades del caso |
| Prueba | Conocimiento que entra al juicio y se desahoga con inmediación y contradicción |
| Prueba ilícita | Dato o prueba obtenidos violando derechos fundamentales; se excluye o se anula |
| Extracción de información | Obtención de comunicaciones, datos y archivos de un dispositivo o servicio remoto; requiere autorización judicial |
| Peritaje irreproducible | Estudio que consume o altera el objeto de modo que no puede repetirse, y obliga a dar aviso a la defensa |
| Datos conservados | Registros de comunicaciones que los concesionarios deben guardar y entregar a la autoridad |
| Duda razonable | Estándar del juicio oral: si persiste, el tribunal absuelve |

## Referencias

- Bujosa Vadell, L. M., Bustamante Rúa, M. M. y Toro Garzón, L. O. (2021). La prueba digital producto de la vigilancia secreta: obtención, admisibilidad y valoración en el proceso penal en España y Colombia. *Revista Brasileira de Direito Processual Penal, 7*(2). https://doi.org/10.22197/rbdpp.v7i2.482
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Constitución Política de los Estados Unidos Mexicanos* [Última reforma DOF 07-10-2026]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CPEUM.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Código Nacional de Procedimientos Penales* [Última reforma DOF 28-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CNPP.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2016). *Código Federal de Procedimientos Penales* [Abrogado por decreto DOF 05-03-2014; última reforma DOF 17-06-2016]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/abro/cfpp/CFPP_abro.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley de la Fiscalía General de la República* [Última reforma DOF 14-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LFGR.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley en Materia de Telecomunicaciones y Radiodifusión* [Nueva ley DOF 16-07-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LMTR.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley Federal de Telecomunicaciones y Radiodifusión* [Abrogada DOF 16-07-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/abro/lftr/LFTR_abro.pdf
- Lira Arteaga, O. M. (2018). *Cibercriminalidad: fundamentos de investigación en México*. Ubijus. Edición anterior: Instituto Nacional de Ciencias Penales, 2010, ISBN 978-607-7882-13-8, https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/

---

*Rogelio Arriaga Gonzalez*
