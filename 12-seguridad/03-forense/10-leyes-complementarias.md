# 10 - Leyes complementarias contra los ciberdelitos

> Lectura del curso [Informática forense](../../cursos/digital-forensics/README.md), unidad 3, tema 3.

## Por qué importa

Identificar el tipo penal es solo la mitad del trabajo. La otra mitad es saber **qué herramientas legales tiene la autoridad para conseguir la evidencia**: a quién le puede pedir los registros de una llamada, por cuánto tiempo deben guardarse, qué valor tiene un correo como prueba, cuándo un banco puede revelar los movimientos de una cuenta. Esas respuestas no están en el Código Penal Federal sino en leyes de otras materias: telecomunicaciones, comercio, derechos de autor, banca, datos personales.

Esta nota recorre las leyes que, junto con el [Código Penal Federal](09-codigo-penal-federal.md), usan con más frecuencia el Ministerio Público, los jueces y los peritos al investigar fraudes, extorsiones, robos y usurpación de identidad. El caso del perito es claro: si no sabe qué datos está obligado a conservar un concesionario, no sabrá qué pedir ni cuándo ya es demasiado tarde para pedirlo.

## El panorama

El material del curso enumeraba siete leyes como las de mayor trascendencia. Varias cambiaron desde entonces, así que la tabla incluye su situación al 2026.

| Ley | Qué aporta a la investigación | Situación al 2026 |
|---|---|---|
| Ley Federal de Protección de Datos Personales en Posesión de los Particulares | Reglas para tratar datos personales y protegerlos de abusos y ataques | Nueva ley (DOF 20-03-2025); la autoridad ya no es el INAI sino la Secretaría Anticorrupción y Buen Gobierno |
| Ley de Seguridad Nacional | Considera amenaza a la seguridad nacional destruir o inhabilitar infraestructura estratégica (art. 5, fr. XII) | Vigente, última reforma DOF 14-11-2025 |
| Ley de Protección y Defensa al Usuario de Servicios Financieros | Protección de usuarios financieros frente a fraudes y abusos | Vigente, última reforma DOF 14-11-2025 |
| Ley Federal de Telecomunicaciones y Radiodifusión | Obligación de los concesionarios de colaborar con la justicia | **Abrogada**; la sustituye la Ley en Materia de Telecomunicaciones y Radiodifusión (DOF 16-07-2025) |
| Ley General de Transparencia y Acceso a la Información Pública | Manejo y protección de la información pública | Nueva ley (DOF 20-03-2025); garante federal: Transparencia para el Pueblo |
| Ley General de Protección de Datos Personales en Posesión de Sujetos Obligados | Datos personales en manos de autoridades | Nueva ley (DOF 20-03-2025) |
| Ley de Instituciones de Crédito | Seguridad de la operación bancaria electrónica y delitos con medios de pago | Vigente, última reforma DOF 14-11-2025 |

La revista del tema desarrollaba a fondo cuatro de ellas: la de telecomunicaciones, el Código de Comercio, la Ley Federal del Derecho de Autor y la Ley de Instituciones de Crédito. Ese es el orden de esta nota.

## Telecomunicaciones: de la LFTR a la LMTR

La ley de telecomunicaciones regula el uso y la explotación del espectro, de las redes públicas y de los servicios de telecomunicaciones y radiodifusión. Para el combate a la ciberdelincuencia importa por una razón: obliga a los operadores a **proteger sus redes y a cooperar con las autoridades** en la investigación de fraudes, espionaje y otros delitos en el entorno digital.

Actualización al 2026: la Ley Federal de Telecomunicaciones y Radiodifusión (LFTR, DOF 14-07-2014), que estudiaba el material, fue **abrogada** por la Ley en Materia de Telecomunicaciones y Radiodifusión (LMTR, DOF 16-07-2025). Según los transitorios de la nueva ley, la abrogación y la extinción del Instituto Federal de Telecomunicaciones (IFT) surtieron efecto al integrarse el Pleno de la **Comisión Reguladora de Telecomunicaciones** (CRT), un órgano desconcentrado de la Agencia de Transformación Digital y Telecomunicaciones. Las referencias al IFT en otras normas se entienden hechas a la CRT o a la Comisión Nacional Antimonopolio, según el tema.

Lo que el material decía de la LFTR sigue valiendo en lo esencial, porque la LMTR conservó casi palabra por palabra el capítulo de colaboración con la justicia. Cambió la numeración:

| Tema | LFTR (abrogada) | LMTR (vigente) |
|---|---|---|
| Objeto de la ley | Art. 1 | Art. 1 |
| Telecomunicaciones como servicio público de interés general | Art. 2 | Art. 2 |
| Definición de internet | Art. 3, fr. XXXII | Art. 3, fr. XXXIX (mismo texto) |
| Atender mandamientos escritos de la autoridad | Art. 189 | Art. 182 |
| Localización en tiempo real, conservación de registros, entrega de datos, área 24/7, robo de equipos | Art. 190 | Art. 183 |
| Órgano regulador | Instituto Federal de Telecomunicaciones | Comisión Reguladora de Telecomunicaciones |

### Lo que define la ley

Entre las definiciones del artículo 3 que el material destacaba están el **acceso al usuario final** (el circuito físico que une el punto terminal en el domicilio con la central de la red pública), la **infraestructura activa** (los elementos de red que almacenan, emiten, procesan, reciben o transmiten información de cualquier tipo) y la **infraestructura pasiva** (todo lo que la soporta: torres, postes, ductos, cableado, energía de respaldo, climatización, derechos de vía).

La definición de **internet** merece la crítica que hacía el autor y que se desarrolla en la [nota 08](08-convenio-de-budapest.md): describirla como un conjunto descentralizado de redes que funcionan como una sola red lógica es incorrecto desde el punto de vista técnico y legal. Para él, el acceso a internet es uno de los servicios digitales (junto con telefonía y televisión) que se comercializan sobre la **Red Pública de Telecomunicaciones**, cuyas condiciones técnicas mínimas de interconexión fijó en su momento un acuerdo del regulador. Entender esa distinción evita confundir qué se le puede pedir a quién.

### La colaboración con la justicia

Este capítulo explica por qué la autoridad puede conocer con detalle la actividad de una línea telefónica de los últimos **24 meses**, algo que salva vidas en secuestros y extorsiones y permite condenar a los responsables.

Ley en Materia de Telecomunicaciones y Radiodifusión, artículo 182 (nueva ley DOF 16-07-2025):

> Artículo 182. Los concesionarios de telecomunicaciones y, en su caso, los autorizados y los proveedores de servicios de aplicaciones y contenidos, están obligados a atender todo mandamiento por escrito, fundado y motivado de la autoridad competente en los términos que establezcan las leyes.
>
> Los titulares de las instancias de seguridad y procuración de justicia designarán a las personas servidoras públicas encargadas de gestionar los requerimientos que se realicen a los concesionarios y, en su caso, autorizados a recibir la información correspondiente, mediante acuerdos publicados en el Diario Oficial de la Federación.

El artículo 183 enumera las obligaciones concretas de los concesionarios (y de los autorizados que determine la Comisión). Su fracción II es la que más usa un perito, porque dice qué registros existen.

Ley en Materia de Telecomunicaciones y Radiodifusión, artículo 183, fracción II, primer párrafo e incisos (nueva ley DOF 16-07-2025):

> II. Conservar un registro y control de comunicaciones que se realicen desde cualquier tipo de equipo terminal o línea que utilice numeración propia o arrendada, bajo cualquier modalidad, que permitan identificar con precisión los siguientes datos:
> a) Nombre, denominación o razón social y domicilio del suscriptor;
> b) Tipo de comunicación (transmisión de voz, buzón vocal, conferencia, datos), servicios suplementarios (incluidos el reenvío o transferencia de llamada) o servicios de mensajería o multimedia empleados (incluidos los servicios de mensajes cortos, mensajes instantáneos, servicios multimedia y avanzados);
> c) Datos necesarios para rastrear e identificar el origen y destino de las comunicaciones de telefonía móvil: número de destino, modalidad de líneas con contrato o plan tarifario, como en la modalidad de líneas de prepago;
> d) Datos necesarios para determinar la fecha, hora y duración de la comunicación, así como el servicio de mensajería o multimedia;
> e) Además de los datos anteriores, se deberá conservar la fecha y hora de la primera activación del servicio y la etiqueta de localización (identificador de celda) desde la que se haya activado el servicio;
> f) En su caso, identificación y características técnicas de los dispositivos, incluyendo, entre otros, los códigos internacionales de identidad de fabricación del equipo y del suscriptor;
> g) La ubicación digital del posicionamiento geográfico de las líneas telefónicas, y
> h) La obligación de conservación de datos, comenzará a contarse a partir de la fecha en que se haya producido la comunicación.

El resto del artículo 183 se resume así:

| Fracción | Obligación | Plazo o detalle |
|---|---|---|
| I | Colaborar en la localización geográfica en tiempo real de los equipos terminales | La omisión se sanciona penalmente; la Comisión fija los lineamientos |
| II | Conservar el registro anterior | 12 meses en sistemas de consulta y entrega en tiempo real, y 12 meses más en almacenamiento electrónico (entrega en 48 horas); proteger los datos contra manipulación, acceso ilícito o destrucción |
| III | Entregar los datos conservados a la autoridad que los pida | Máximo 24 horas desde la notificación; prohibido usarlos para otros fines |
| IV | Tener un área responsable disponible las 24 horas, los 365 días | Para requerimientos de información, localización e intervención; el cambio de responsable se avisa con 24 horas de anticipación |
| V y VI | Recibir reportes de robo o extravío de equipos y suspender su servicio | El reporte incluye el código de identidad de fabricación; los concesionarios intercambian listas de equipos robados |
| VII | Bloquear de inmediato líneas reportadas como robadas y suspender el servicio cuando lo ordene la autoridad para hacer cesar un delito | |
| VIII a XII | Anular señales en centros penitenciarios, número único de emergencias, prioridad en emergencias e investigación de soluciones contra el uso de equipos para delinquir | Agregadas o ampliadas en la LMTR |

El mismo artículo cierra recordando que las comunicaciones privadas son inviolables y que solo la autoridad judicial federal puede autorizar su intervención. Y para el tratamiento de los datos personales conservados, remite a la ley de protección de datos en posesión de particulares.

Actualización al 2026: la LMTR agrega una obligación que cambia el panorama de la telefonía prepago: los concesionarios móviles solo pueden activar y mantener activas las líneas asociadas a una persona usuaria que presentó una identificación oficial con CURP (o RFC para personas morales; pasaporte para extranjeros), conforme a los lineamientos de la Comisión (artículo 103; para las comercializadoras, artículo 164, fracción III).

### El hueco de internet

La conservación de 24 meses cubre la telefonía. En internet la situación era, según el material, muy distinta: sin una obligación legal específica, la cooperación de los proveedores de acceso era deficiente, y el autor señalaba que solo un operador conservaba durante seis meses la información para rastrear comunicaciones por IP, gracias a un acuerdo de colaboración renovado en 2006 con la entonces Procuraduría General de la República (hoy Fiscalía General de la República). Sin datos de conexión no hay rastreo, y sin rastreo la investigación se detiene.

La herramienta que sí existe es la conservación inmediata hasta por 90 días del artículo 303 del Código Nacional de Procedimientos Penales, citado textual en la [nota 08](08-convenio-de-budapest.md). Ese artículo todavía remite a la LFTR y a su artículo 190; hoy debe leerse con el 183 de la LMTR.

## Código de Comercio: el mensaje de datos como prueba

El Código de Comercio importa porque define la **firma electrónica** y las condiciones para que un mensaje de datos sea confiable. En el comercio por internet el investigador tiene muchas limitaciones jurídicas para rastrear el servidor usado en un fraude; saber qué valor legal tiene cada mensaje y cuándo se presume enviado por quien aparece como emisor es parte del análisis.

### Conservar la correspondencia

Entre las obligaciones de todo comerciante (arts. 47 a 49) está guardar archivadas las cartas, telegramas y documentos que reciben o envían sobre su negocio. Las copias pueden obtenerse por medios mecánicos, fotográficos o electrónicos que permitan reproducirlas íntegras. Los originales en que consten contratos u obligaciones se conservan por lo menos diez años.

Código de Comercio, artículo 49, párrafo segundo (texto vigente, última reforma DOF 14-11-2025):

> Para efectos de la conservación o presentación de originales, en el caso de mensajes de datos, se requerirá que la información se haya mantenido íntegra e inalterada a partir del momento en que se generó por primera vez en su forma definitiva y sea accesible para su ulterior consulta. La Secretaría de Economía emitirá la Norma Oficial Mexicana que establezca los requisitos que deberán observarse para la conservación de mensajes de datos.

Actualización al 2026: el material atribuía esa norma a la Secretaría de Comercio y Fomento Industrial; el texto vigente ya dice Secretaría de Economía (reforma DOF 09-04-2012).

### El comercio electrónico

El artículo 89 establece que estas reglas rigen en todo el país en asuntos mercantiles y que se interpretan conforme a cuatro principios: **neutralidad tecnológica**, **autonomía de la voluntad**, **compatibilidad internacional** y **equivalencia funcional** del mensaje de datos frente al papel y de la firma electrónica frente a la autógrafa. Luego define los términos, que conviene tener a mano al redactar un dictamen:

| Término | Sentido en el Código de Comercio |
|---|---|
| Mensaje de datos | Información generada, enviada, recibida o archivada por medios electrónicos, ópticos o cualquier otra tecnología |
| Emisor | Quien envía o genera el mensaje en nombre propio o por cuya cuenta se envía, sin actuar como intermediario |
| Destinatario | Quien el emisor designa para recibir el mensaje |
| Intermediario | Quien, por cuenta de otro, envía, recibe o archiva el mensaje o presta un servicio sobre él |
| Sistema de información | Cualquier sistema para generar, enviar, recibir, archivar o procesar mensajes de datos |
| Firma electrónica | Datos asociados a un mensaje que identifican al firmante y muestran que aprueba su contenido; es admisible como prueba en juicio |
| Firma electrónica avanzada o fiable | La que cumple los requisitos del artículo 97 |
| Datos de creación de firma | Claves o códigos únicos y secretos con que el firmante crea su firma |
| Certificado | Mensaje o registro que confirma el vínculo entre un firmante y sus datos de creación de firma |
| Prestador de servicios de certificación | Persona o institución que expide certificados y presta servicios de firma |
| Parte que confía | Quien actúa sobre la base de un certificado o una firma |
| Titular del certificado | La persona a cuyo favor se expidió |

La firma digital se considera una especie de firma electrónica.

Código de Comercio, artículo 89 bis (texto vigente):

> Artículo 89 bis.- No se negarán efectos jurídicos, validez o fuerza obligatoria a cualquier tipo de información por la sola razón de que esté contenida en un Mensaje de Datos. Por tanto, dichos mensajes podrán ser utilizados como medio probatorio en cualquier diligencia ante autoridad legalmente reconocida, y surtirán los mismos efectos jurídicos que la documentación impresa, siempre y cuando los mensajes de datos se ajusten a las disposiciones de este Código y a los lineamientos normativos correspondientes.

Actualización al 2026: el material transcribía solo la primera oración; la reforma de 2016 añadió de forma expresa que los mensajes de datos sirven como **medio probatorio** con los mismos efectos que la documentación impresa.

### Cuándo se presume que un mensaje viene de quien dice

El código responde varias preguntas que aparecen en cualquier peritaje sobre correos o transacciones:

- **¿Quién lo envió?** Se presume que proviene del emisor si lo envió él mismo, si se usaron sus claves o contraseñas o lo envió alguien facultado, o si lo envió un sistema que él programó para operar solo (art. 90). El destinatario puede actuar en consecuencia si aplicó el procedimiento acordado o si el mensaje resulta de un intermediario que tenía acceso al método de identificación del emisor, salvo que ya sepa, o debiera saber con la debida diligencia, que el mensaje no venía de él (art. 90 bis).
- **¿Cuándo se recibió?** Al entrar al sistema de información designado por el destinatario; si llegó a otro sistema, cuando el destinatario lo recupera; si no designó ninguno, cuando entra a cualquier sistema suyo (art. 91). Se tiene por expedido cuando entra a un sistema que ya no controla el emisor (art. 91 bis).
- **¿Hubo acuse?** Si se pidió acuse sin fijar forma, basta cualquier comunicación o acto del destinatario que indique que lo recibió; si el emisor condicionó los efectos al acuse, el mensaje no se tiene por enviado hasta recibirlo (art. 92).
- **¿Cumple con la forma escrita y la firma?** Sí, si la información se mantiene íntegra y accesible y si el mensaje es atribuible a las partes (art. 93). Se considera original si se conservó su integridad desde que se generó y puede mostrarse; es íntegro si permanece completo e inalterado aunque cambie el medio que lo contiene (art. 93 bis).
- **¿Dónde se expidió y recibió?** En los establecimientos de emisor y destinatario; si tienen varios, el más ligado a la operación; si no tienen, su residencia habitual (art. 94).
- **¿Es un duplicado?** Cada mensaje recibido se presume distinto, salvo que el destinatario sepa o deba saber que era una copia (art. 95).

Código de Comercio, artículo 93, párrafos primero y segundo (texto vigente):

> Artículo 93.- Cuando la ley exija la forma escrita para los actos, convenios o contratos, este supuesto se tendrá por cumplido tratándose de Mensaje de Datos, siempre que la información en él contenida se mantenga íntegra y sea accesible para su ulterior consulta, sin importar el formato en el que se encuentre o represente.
>
> Cuando adicionalmente la ley exija la firma de las partes, dicho requisito se tendrá por cumplido tratándose de Mensaje de Datos, siempre que éste sea atribuible a dichas partes.

### Las firmas

Ningún método para crear una firma electrónica puede excluirse de efectos jurídicos (art. 96). La firma se considera **avanzada o fiable** cuando cumple cuatro requisitos.

Código de Comercio, artículo 97 (texto vigente):

> Artículo 97.- Cuando la ley requiera o las partes acuerden la existencia de una Firma en relación con un Mensaje de Datos, se entenderá satisfecho dicho requerimiento si se utiliza una Firma Electrónica que resulte apropiada para los fines para los cuales se generó o comunicó ese Mensaje de Datos.
>
> La Firma Electrónica se considerará Avanzada o Fiable si cumple por lo menos los siguientes requisitos:
>
> I. Los Datos de Creación de la Firma, en el contexto en que son utilizados, corresponden exclusivamente al Firmante;
>
> II. Los Datos de Creación de la Firma estaban, en el momento de la firma, bajo el control exclusivo del Firmante;
>
> III. Es posible detectar cualquier alteración de la Firma Electrónica hecha después del momento de la firma, y
>
> IV. Respecto a la integridad de la información de un Mensaje de Datos, es posible detectar cualquier alteración de ésta hecha después del momento de la firma.
>
> Lo dispuesto en el presente artículo se entenderá sin perjuicio de la posibilidad de que cualquier persona demuestre de cualquier otra manera la fiabilidad de una Firma Electrónica; o presente pruebas de que una Firma Electrónica no es fiable.

En términos forenses, los cuatro requisitos se reducen a tres propiedades: **identidad** del firmante, **control exclusivo** de la clave e **integridad** verificable de firma y contenido. Los prestadores de certificación deben informar si sus firmas cumplen esos requisitos, con criterios compatibles con las normas internacionales (art. 98). El firmante, por su parte, debe usar su firma con diligencia, evitar el uso no autorizado de sus datos de creación, verificar que lo declarado en su certificado sea exacto y responder por el uso indebido cuando no actuó con cuidado (art. 99).

## Ley Federal del Derecho de Autor

Las redes informáticas se usan para sustraer y distribuir obras protegidas, y el programa de cómputo es una obra. El artículo 13 reconoce catorce ramas, entre ellas la literaria, la musical, la cinematográfica, la fotográfica, los **programas de cómputo** y las compilaciones como enciclopedias, antologías y **bases de datos** que por su selección o disposición sean creaciones intelectuales. El 14 aclara lo que no se protege: ideas, fórmulas, métodos, sistemas y principios en sí mismos, su aprovechamiento industrial, esquemas para juegos o negocios, letras o colores aislados, nombres y títulos sueltos, formatos en blanco, emblemas oficiales, textos legislativos y judiciales, y el contenido informativo de las noticias. El 66 extiende el contrato de radiodifusión a la transmisión por cable, fibra óptica, satélite u otro medio análogo.

El capítulo de programas de computación y bases de datos (arts. 101 a 114) es el que interesa al informático forense.

Ley Federal del Derecho de Autor, artículos 101 y 102 (texto vigente, última reforma DOF 14-05-2026):

> Artículo 101.- Se entiende por programa de computación la expresión original en cualquier forma, lenguaje o código, de un conjunto de instrucciones que, con una secuencia, estructura y organización determinada, tiene como propósito que una computadora o dispositivo realice una tarea o función específica.

> Artículo 102.- Los programas de computación, incluidos los de inteligencia artificial, se protegen en los mismos términos que las obras literarias. Dicha protección se extiende tanto a los programas operativos como a los programas aplicativos, ya sea en forma de código fuente o de código objeto. Se exceptúan aquellos programas de cómputo o inteligencia artificial, que tengan por objeto causar efectos nocivos a otros programas o equipos o que violen los derechos de terceros tutelados por esta Ley.

Actualización al 2026: la reforma publicada el 14 de mayo de 2026 incluyó expresamente los programas de **inteligencia artificial** en el artículo 102. Nótese también la última oración: un programa diseñado para dañar otros programas o equipos, como un código malicioso, **no** tiene protección autoral.

El resto del capítulo, en síntesis:

| Artículo | Regla |
|---|---|
| 103 | El programa creado por empleados en sus funciones pertenece al empleador, salvo pacto; la cesión no tiene límite de plazo |
| 104 | El titular conserva el derecho de autorizar o prohibir el arrendamiento de las copias vendidas |
| 105 | El usuario legítimo puede hacer las copias que permita la licencia, o una sola si es indispensable para usar el programa o como respaldo; el respaldo se destruye al terminar el derecho de uso |
| 106 | El titular decide sobre reproducción, traducción o adaptación, distribución y alquiler, y sobre la **decompilación, la ingeniería inversa y el desensamblado** |
| 107 y 108 | Las bases de datos originales se protegen como compilaciones; las no originales, durante cinco años a favor de quien las elaboró |
| 109 | Acceder a información privada de las personas en esas bases, o publicarla, requiere su autorización, **salvo las investigaciones de las autoridades de procuración e impartición de justicia** |
| 110 | El titular de una base de datos controla la reproducción, adaptación, distribución y comunicación pública de su estructura |
| 111 | Se protegen los elementos visuales, sonoros o animados de los programas |
| 112 | Se prohíbe importar, fabricar, distribuir o usar aparatos o servicios para eliminar la protección técnica de programas y transmisiones |
| 113 y 114 | Se protegen las obras transmitidas por redes de telecomunicaciones y el espectro |

El artículo 109 es un buen ejemplo de para qué sirven estas leyes: le dice al perito que, con la orden correspondiente, la autoridad sí puede acceder a una base de datos con información personal sin pedir permiso a cada titular.

## Ley de Instituciones de Crédito

El objetivo de revisar esta ley, según el material, es que el investigador sepa qué información puede pedirle al Ministerio Público, quien a su vez la solicita a la institución financiera. La información bancaria es del más alto grado de confidencialidad, así que la solicitud tiene que ser precisa. La ley regula el servicio de banca y crédito, que solo pueden prestar las instituciones de banca múltiple y de banca de desarrollo (arts. 1 y 2), y tipifica delitos que el CPF no cubre con el mismo detalle.

| Artículo | Conducta | Pena de prisión vigente |
|---|---|---|
| 112 Bis | Producir, alterar, poseer o usar sin derecho tarjetas, cheques u otros medios de pago; obtener o usar información de clientes; copiar la banda magnética o el medio electrónico; poseer equipos para extraer esa información (*skimming*) | 3 a 9 años |
| 112 Ter | Poseer, usar o vender esos medios de pago sabiendo que están alterados o falsificados | 3 a 9 años |
| 112 Quáter | Acceder sin derecho a los equipos o medios electrónicos del sistema bancario, o alterar los cajeros, para obtener recursos o información | 3 a 9 años |
| 113 | Consejeros, funcionarios o empleados que alteran registros, presentan datos falsos a la Comisión Nacional Bancaria y de Valores, otorgan créditos con información falsa o **destruyen información o archivos, incluso electrónicos**, para obstruir la supervisión | 2 a 10 años |
| 113 Bis | Usar, obtener, transferir o disponer indebidamente de recursos de los clientes o de la institución | 5 a 15 años; 7 a 15 si lo hace personal interno o un tercero con acceso autorizado |

Ley de Instituciones de Crédito, artículos 112 Quáter y 113 Bis (texto vigente, última reforma DOF 14-11-2025):

> Artículo 112 Quáter.- Se sancionará con prisión de tres a nueve años y de treinta mil a trescientos mil días multa, al que sin causa legítima o sin consentimiento de quien esté facultado para ello:
> I. Acceda a los equipos o medios electrónicos, ópticos o de cualquier otra tecnología del sistema bancario mexicano, para obtener recursos económicos, información confidencial o reservada, o
> II. Altere o modifique el mecanismo de funcionamiento de los equipos o medios electrónicos, ópticos o de cualquier otra tecnología para la disposición de efectivo de los usuarios del sistema bancario mexicano, para obtener recursos económicos, información confidencial o reservada.

> Artículo 113 Bis.- A quien en forma indebida utilice, obtenga, transfiera o de cualquier otra forma, disponga de recursos o valores de los clientes de las instituciones de crédito o de los recursos o valores de estas últimas, se le aplicará una sanción de cinco a quince años de prisión y multa de quinientos a treinta mil días de salario.
>
> Si quienes cometen el delito que se describe en el párrafo anterior son funcionarios o empleados de las instituciones de crédito o terceros ajenos pero con acceso autorizado por éstas a los sistemas de las mismas, la sanción será de siete a quince años de prisión y multa de mil a cincuenta mil días de salario.

Actualización al 2026: el material citaba para el 113 Bis una pena de tres a diez años; la reforma de 2014 (DOF 10-01-2014) la subió a cinco a quince y agregó el agravante para personal interno y terceros con acceso.

Para obtener la información de una cuenta, el artículo 142 establece el **secreto bancario**: la institución solo informa al titular y a sus representantes, salvo que la pida la autoridad judicial en un juicio donde el titular sea parte o acusado, o alguna de las autoridades que la propia ley enumera (entre ellas la Fiscalía General de la República y las fiscalías de los estados, para comprobar un delito), con solicitud fundada y motivada que se tramita por conducto de la Comisión Nacional Bancaria y de Valores. De ahí la recomendación del material: el perito no pide directamente; formula con claridad qué necesita para que el Ministerio Público lo solicite por la vía correcta.

## Glosario

| Término | Definición |
|---------|-----------|
| Concesionario | Persona física o moral titular de una concesión para prestar servicios de telecomunicaciones o radiodifusión |
| Concesión única | Título que autoriza a prestar de forma convergente cualquier servicio público de telecomunicaciones o radiodifusión; el espectro se obtiene aparte |
| Autorizado | Quien presta ciertos servicios (por ejemplo, como comercializadora) con autorización y sin ser concesionario |
| Comercializadora | Quien vende servicios de telecomunicaciones usando capacidad de redes ajenas, sin ser concesionario |
| Red pública de telecomunicaciones | Red con la que se explotan comercialmente servicios de telecomunicaciones; el autor ubica ahí el acceso a internet |
| Infraestructura activa | Equipos de red que procesan, transmiten o almacenan información |
| Infraestructura pasiva | Torres, ductos, postes, cableado, energía y demás soporte físico de la red |
| Banda ancha | Acceso de alta capacidad que permite servicios convergentes sobre infraestructura confiable |
| Identificador de celda | Etiqueta de la antena desde la que se activó o se usó una línea móvil |
| Comisión Reguladora de Telecomunicaciones | Órgano desconcentrado que desde 2025 sustituye al IFT como regulador del sector |
| Mensaje de datos | Información generada, enviada, recibida o archivada por medios electrónicos u ópticos |
| Equivalencia funcional | Principio por el que el mensaje de datos cumple las funciones del papel y la firma electrónica las de la autógrafa |
| Firma electrónica avanzada | La que asegura identidad, control exclusivo de la clave e integridad verificable de firma y contenido |
| Secreto bancario | Deber de las instituciones de crédito de no revelar operaciones sino al titular o a la autoridad facultada |
| Skimming | Copia de la banda o el chip de una tarjeta con un lector oculto; encuadra en el artículo 112 Bis de la Ley de Instituciones de Crédito |

## Referencias

- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley en Materia de Telecomunicaciones y Radiodifusión* [Nueva ley DOF 16-07-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LMTR.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley Federal de Telecomunicaciones y Radiodifusión* [Ley abrogada DOF 16-07-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/abro/lftr/LFTR_abro.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Código de Comercio* [Última reforma DOF 14-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CCom.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Ley Federal del Derecho de Autor* [Última reforma DOF 14-05-2026]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LFDA.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley de Instituciones de Crédito* [Última reforma DOF 14-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LIC.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley Federal de Protección de Datos Personales en Posesión de los Particulares* [Última reforma DOF 14-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LFPDPPP.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley General de Protección de Datos Personales en Posesión de Sujetos Obligados* [Última reforma DOF 14-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LGPDPPSO.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley General de Transparencia y Acceso a la Información Pública* [Nueva ley DOF 20-03-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LGTAIP.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley de Seguridad Nacional* [Última reforma DOF 14-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LSN.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley de Protección y Defensa al Usuario de Servicios Financieros* [Última reforma DOF 14-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LPDUSF.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Código Nacional de Procedimientos Penales* [Última reforma DOF 28-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CNPP.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Constitución Política de los Estados Unidos Mexicanos* [Última reforma DOF 07-10-2026]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CPEUM.pdf
- Lira Arteaga, O. M. (2018). *Cibercriminalidad: fundamentos de investigación en México*. Ubijus. Edición anterior: Instituto Nacional de Ciencias Penales, 2010, ISBN 978-607-7882-13-8, https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/

---

*Rogelio Arriaga Gonzalez*
