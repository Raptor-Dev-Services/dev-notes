# 11 - Los ciberdelitos en México

> Unidad 4 de *Introducción a la ciberseguridad* (Raptor Dev Services Academy, curso `cyber-intro`). Material recreado con palabras propias a partir del temario de un diplomado de ciberseguridad; las fuentes van al final en APA 7. Actualización al 2026 donde se indica.

## Por qué importa

En México no hay una sola ley de ciberdelitos. Hay más de cuarenta ordenamientos con figuras relacionadas con el uso de la tecnología, repartidos entre el Código Penal Federal, los 32 códigos penales estatales y varias leyes especiales. El cuadro comparativo del temario reunió 229 conductas delictivas vinculadas con la informática en esas legislaciones, cada una con su propia pena. Quien trabaja en seguridad necesita saber qué parte de lo que ve en un incidente cae en alguna de ellas, porque de eso depende si hay denuncia posible y ante quién.

La otra mitad del problema es lo que **no** está en ningún código: conductas dañinas que ya ocurren y que la ley todavía no describe. Esta nota recorre las dos mitades: las conductas típicas (tipificadas) y las atípicas, con los artículos federales vigentes, el panorama de los estados, los casos que la ley aún no alcanza, la discusión sobre el Convenio de Budapest y las cifras que muestran el crecimiento del fenómeno.

Los conceptos de base (tipo penal, tipicidad, conducta atípica) están en [10-conductas-hechos-y-delitos.md](10-conductas-hechos-y-delitos.md); la investigación y la prueba, en [12-investigar-el-ciberdelito.md](12-investigar-el-ciberdelito.md).

## Típicas y atípicas

| | Conducta típica (ciberdelito) | Conducta atípica (ciberconducta no regulada) |
|---|---|---|
| Qué es | Está descrita en una ley penal y tiene pena | Existe y causa daño, pero ninguna ley penal la describe |
| Qué se puede hacer | Denunciar ante el Ministerio Público; hay persecución penal | Buscar otras vías (civil, administrativa, laboral) o esperar a que se legisle |
| Ejemplo | Copiar sin autorización información de un sistema protegido del Estado | Falsificar la señal de GPS de un vehículo, sin otro delito asociado |

### Cómo se nombra y cómo se clasifica

El fenómeno recibe muchos nombres que se usan casi como sinónimos: delitos cibernéticos, ciberdelitos, delitos informáticos, cibercrimen, delincuencia de alta tecnología, delitos relacionados con las computadoras, delitos electrónicos o criminalidad informática. En términos generales, un ciberdelito es cualquier acto ilícito cometido en el espacio digital por medio de redes y dispositivos, que afecta la integridad o la confidencialidad de datos y sistemas, casi siempre para defraudar o apoderarse de información.

La doctrina distingue dos papeles que la tecnología puede jugar:

| Clasificación | La tecnología es | Ejemplo |
|---|---|---|
| Como objeto del delito | El blanco: el equipo físico o el sistema lógico que se quiere afectar, acceder o vulnerar | Borrar sin permiso la información de un servidor |
| Como medio para cometerlo | La herramienta para cometer un delito que existiría también sin tecnología | Defraudar con un sitio bancario falso |

Cámpoli separa además el **delito informático**, en el que el tipo protege la integridad física o lógica de equipos o sitios web, del **delito telemático**, que sin tocar un equipo concreto reduce su capacidad de transmitir o procesar datos a distancia. Ninguna de las dos definiciones es completa: la primera menciona sitios web pero no sistemas ni bases de datos, y la segunda deja fuera el acceso no autorizado al equipo (la intrusión).

Sus características más citadas: son conductas de cuello blanco, de oportunidad, que causan pérdidas económicas serias, ofrecen ventajas de tiempo y espacio al atacante, son sofisticadas, difíciles de investigar, tienden a multiplicarse y se denuncian poco frente a los casos que ocurren.

## El Código Penal Federal

El Estado mexicano reconoció la necesidad de regular estas conductas y abrió un apartado específico, pero la regulación sigue siendo incipiente. El núcleo federal está en el Título Noveno del Código Penal Federal (CPF), "Revelación de secretos y acceso ilícito a sistemas y equipos de informática", cuyo capítulo II se agregó en 1999. El texto vigente, con última reforma del 13 de marzo de 2026, dice:

> Artículo 211 bis 1.- Al que sin autorización modifique, destruya o provoque pérdida de información contenida en sistemas o equipos de informática protegidos por algún mecanismo de seguridad, se le impondrán de seis meses a dos años de prisión y de cien a trescientos días multa.
> Al que sin autorización conozca o copie información contenida en sistemas o equipos de informática protegidos por algún mecanismo de seguridad, se le impondrán de tres meses a un año de prisión y de cincuenta a ciento cincuenta días multa.

> Artículo 211 bis 2.- Al que sin autorización modifique, destruya o provoque pérdida de información contenida en sistemas o equipos de informática del Estado, protegidos por algún mecanismo de seguridad, se le impondrán de uno a cuatro años de prisión y de doscientos a seiscientos días multa.
> Al que sin autorización conozca o copie información contenida en sistemas o equipos de informática del Estado, protegidos por algún mecanismo de seguridad, se le impondrán de seis meses a dos años de prisión y de cien a trescientos días multa.
> A quien sin autorización conozca, obtenga, copie o utilice información contenida en cualquier sistema, equipo o medio de almacenamiento informáticos de seguridad pública, protegido por algún medio de seguridad, se le impondrá pena de cuatro a diez años de prisión y multa de quinientos a mil días de salario mínimo general vigente en el Distrito Federal. Si el responsable es o hubiera sido servidor público en una institución de seguridad pública, se impondrá además, destitución e inhabilitación de cuatro a diez años para desempeñarse en otro empleo, puesto, cargo o comisión pública.
> Las sanciones anteriores se duplicarán cuando la conducta obstruya, entorpezca, obstaculice, limite o imposibilite la procuración o impartición de justicia, o recaiga sobre los registros relacionados con un procedimiento penal resguardados por las autoridades competentes.

> Artículo 211 bis 3.- Al que estando autorizado para acceder a sistemas y equipos de informática del Estado, indebidamente modifique, destruya o provoque pérdida de información que contengan, se le impondrán de dos a ocho años de prisión y de trescientos a novecientos días multa.
> Al que estando autorizado para acceder a sistemas y equipos de informática del Estado, indebidamente copie información que contengan, se le impondrán de uno a cuatro años de prisión y de ciento cincuenta a cuatrocientos cincuenta días multa.
> A quien estando autorizado para acceder a sistemas, equipos o medios de almacenamiento informáticos en materia de seguridad pública, indebidamente obtenga, copie o utilice información que contengan, se le impondrá pena de cuatro a diez años de prisión y multa de quinientos a mil días de salario mínimo general vigente en el Distrito Federal. Si el responsable es o hubiera sido servidor público en una institución de seguridad pública, se impondrá además, hasta una mitad más de la pena impuesta, destitución e inhabilitación por un plazo igual al de la pena resultante para desempeñarse en otro empleo, puesto, cargo o comisión pública.

> Artículo 211 bis 4.- Al que sin autorización modifique, destruya o provoque pérdida de información contenida en sistemas o equipos de informática de las instituciones que integran el sistema financiero, protegidos por algún mecanismo de seguridad, se le impondrán de seis meses a cuatro años de prisión y de cien a seiscientos días multa.
> Al que sin autorización conozca o copie información contenida en sistemas o equipos de informática de las instituciones que integran el sistema financiero, protegidos por algún mecanismo de seguridad, se le impondrán de tres meses a dos años de prisión y de cincuenta a trescientos días multa.

> Artículo 211 bis 5.- Al que estando autorizado para acceder a sistemas y equipos de informática de las instituciones que integran el sistema financiero, indebidamente modifique, destruya o provoque pérdida de información que contengan, se le impondrán de seis meses a cuatro años de prisión y de cien a seiscientos días multa.
> Al que estando autorizado para acceder a sistemas y equipos de informática de las instituciones que integran el sistema financiero, indebidamente copie información que contengan, se le impondrán de tres meses a dos años de prisión y de cincuenta a trescientos días multa.
> Las penas previstas en este artículo se incrementarán en una mitad cuando las conductas sean cometidas por funcionarios o empleados de las instituciones que integran el sistema financiero.

> Artículo 211 bis 6.- Para los efectos de los artículos 211 Bis 4 y 211 Bis 5 anteriores, se entiende por instituciones que integran el sistema financiero, las señaladas en el artículo 400 Bis de este Código.

> Artículo 211 bis 7.- Las penas previstas en este capítulo se aumentarán hasta en una mitad cuando la información obtenida se utilice en provecho propio o ajeno.

### Cómo leer esos siete artículos

El capítulo se organiza en dos ejes: **de quién** son los sistemas (particulares, Estado, seguridad pública, sistema financiero) y **quién** actúa (alguien sin autorización o alguien autorizado que abusa de su acceso). En cada combinación se distingue el daño (modificar, destruir, provocar pérdida) del simple conocimiento o copia.

| Sistema afectado | Sin autorización: dañar | Sin autorización: conocer o copiar | Con autorización: dañar | Con autorización: copiar |
|---|---|---|---|---|
| De particulares, protegido | 6 meses a 2 años | 3 meses a 1 año | (no previsto) | (no previsto) |
| Del Estado, protegido | 1 a 4 años | 6 meses a 2 años | 2 a 8 años | 1 a 4 años |
| De seguridad pública | (obtener, copiar o usar) 4 a 10 años | | (obtener, copiar o usar) 4 a 10 años | |
| Del sistema financiero, protegido | 6 meses a 4 años | 3 meses a 2 años | 6 meses a 4 años | 3 meses a 2 años |

Tres observaciones prácticas:

- **"Protegido por algún mecanismo de seguridad"** es un elemento del tipo. Si el sistema de un particular no tenía ninguna protección, la conducta puede no encajar. Una contraseña débil sigue siendo un mecanismo; ninguna contraseña, no.
- **El empleado con acceso que abusa** solo está previsto para sistemas del Estado, de seguridad pública y del sistema financiero. En una empresa privada, el abuso de un acceso legítimo hay que buscarlo en otras figuras (revelación de secretos, fraude, datos personales).
- **Las penas son bajas** frente al daño posible, y el artículo 211 bis 7 solo las sube cuando hay provecho.

### Otros delitos federales que se cometen con tecnología

El CPF ordena los delitos por el bien jurídico que protegen, no por el medio. Por eso hay figuras fuera del Título Noveno que en la práctica se cometen con computadoras y redes, y que el temario agrupa como parte del catálogo informático. Juntarlas en un capítulo propio alteraría esa organización, pero conviene tenerlas a la vista:

> Artículo 199 Octies.- Comete el delito de violación a la intimidad sexual, aquella persona que divulgue, comparta, distribuya o publique imágenes, videos o audios de contenido íntimo sexual de una persona que tenga la mayoría de edad, sin su consentimiento, su aprobación o su autorización.
> Así como quien videograbe, audiograbe, fotografíe, imprima o elabore, imágenes, audios o videos con contenido íntimo sexual de una persona sin su consentimiento, sin su aprobación, o sin su autorización.
> Estas conductas se sancionarán con una pena de tres a seis años de prisión y una multa de quinientas a mil Unidades de Medida y Actualización.

> Artículo 200.- Al que comercie, distribuya, exponga, haga circular u oferte, a menores de dieciocho años de edad, libros, escritos, grabaciones, filmes, fotografías, anuncios impresos, imágenes u objetos, de carácter pornográfico, reales o simulados, sea de manera física, o a través de cualquier medio, se le impondrá de seis meses a cinco años de prisión y de trescientos a quinientos días multa.

> Artículo 202.- Comete el delito de pornografía de personas menores de dieciocho años de edad o de personas que no tienen capacidad para comprender el significado del hecho o de personas que no tienen capacidad para resistirlo, quien procure, obligue, facilite o induzca, por cualquier medio, a una o varias de estas personas a realizar actos sexuales o de exhibicionismo corporal con fines lascivos o sexuales, reales o simulados, con el objeto de video grabarlos, fotografiarlos, filmarlos, exhibirlos o describirlos a través de anuncios impresos, transmisión de archivos de datos en red pública o privada de telecomunicaciones, sistemas de cómputo, electrónicos o sucedáneos. Al autor de este delito se le impondrá pena de siete a doce años de prisión y de ochocientos a dos mil días multa.

> Artículo 254 bis 1. Se sancionará con prisión de uno a tres años y con quinientos a cinco mil días de multa, a quien por sí o por interpósita persona, en la práctica de una visita de verificación, por cualquier medio altere, destruya o perturbe de forma total o parcial documentos, imágenes o archivos electrónicos que contengan información o datos, con el objeto de desviar, obstaculizar o impedir la investigación de un posible hecho delictuoso o la práctica de la diligencia administrativa.

> Artículo 424 bis.- Se impondrá prisión de tres a diez años y de dos mil a veinte mil días multa:
> I. A quien produzca, reproduzca, introduzca al país, almacene, transporte, distribuya, venda o arriende copias de obras, fonogramas, videogramas o libros, protegidos por la Ley Federal del Derecho de Autor, en forma dolosa, con fin de especulación comercial y sin la autorización que en los términos de la citada Ley deba otorgar el titular de los derechos de autor o de los derechos conexos.
> [...]
> II. A quien fabrique con fin de lucro un dispositivo o sistema cuya finalidad sea desactivar los dispositivos electrónicos de protección de un programa de computación, o

> Artículo 426.- Se impondrá prisión de seis meses a cuatro años y de trescientos a tres mil días multa, en los casos siguientes:
> I. A quien fabrique, modifique, importe, distribuya, venda o arriende un dispositivo o sistema para descifrar una señal de satélite cifrada, portadora de programas, sin autorización del distribuidor legítimo de dicha señal;
> II. A quien realice con fines de lucro cualquier acto con la finalidad de descifrar una señal de satélite cifrada, portadora de programas, sin autorización del distribuidor legítimo de dicha señal;

> **Actualización al 2026:** los artículos 199 Octies a 199 Decies se agregaron al CPF el 1 de junio de 2021, con la reforma conocida como Ley Olimpia, que no es una ley aparte sino un conjunto de reformas para reconocer la violencia digital. El 199 Decies aumenta la pena hasta en una mitad cuando el autor es o fue pareja de la víctima, es servidor público, actúa con fines de lucro, o cuando la víctima atenta contra su vida a consecuencia del delito. El artículo 426 se reformó el 1 de julio de 2020 y ahora también castiga modificar esos dispositivos y descifrar señales de cable.

### Un error común: el "robo de datos"

En el lenguaje diario se habla de "robo de datos" o "robo de identidad", pero para el CPF el robo exige apoderarse de una **cosa ajena mueble**:

> Artículo 367.- Comete el delito de robo: el que se apodera de una cosa ajena mueble, sin derecho y sin consentimiento de la persona que puede disponer de ella con arreglo a la ley.

Copiar información no desapodera a nadie: el dueño la sigue teniendo. Por eso, en lenguaje jurídico, lo que se llama "robo de datos" encaja en otras figuras: conocer o copiar información de un sistema protegido (211 bis 1 a 5), revelar secretos (210 y 211), tratar datos personales con engaño para lucrar (ley de datos personales) o fraude. Del mismo modo, la identidad no se roba: se **suplanta** (asumir una identidad que no es la propia) o se **usurpa** (ocupar ilegalmente lo que pertenece a otro). El CPF no tiene un tipo general de suplantación de identidad; algunos códigos estatales sí lo tienen.

## Las legislaciones de los estados

Algunos estados ya incluyen la denominación "delito informático" en sus códigos penales, aunque la definición y el desarrollo de la figura siguen en proceso. En la mayoría no hay una definición clara, lo que muestra la urgencia de armonizar.

| Entidad | Pena del delito informático | Nota |
|---|---|---|
| Sinaloa | 6 meses a 2 años | Art. 217: castiga a quien entra o usa sin derecho una base de datos, sistema o red para defraudar u obtener algo, o intercepta, altera o destruye programas o datos; 90 a 300 días multa |
| Morelos | 6 meses a 2 años | |
| Veracruz | 6 meses a 2 años | |
| Nayarit | 2 a 6 años | El rango más alto entre los que tipifican la figura |
| Puebla | Varía según la fuente | El temario la agrupa con Nayarit (2 a 6 años); una presentación de 2024 sobre el marco jurídico le asigna de 3 meses a 1 año |
| Yucatán | 6 meses a 2 años y 1,000 a 2,000 días multa como mínimo | Art. 243 Bis 5; algunas fracciones permiten duplicar la pena |

La conclusión del temario es que la tipificación es mínima y las penas, relativamente bajas: falta proporción entre el daño posible de un ciberdelito y la sanción. Y como se vio en la nota anterior, la diferencia entre estados abre la puerta a que el responsable prefiera la jurisdicción más benévola.

### Por qué las leyes locales van tan despacio

- El proceso legislativo es lento y muchas iniciativas responden más a cálculos políticos o partidistas que a la necesidad de proteger la información; muchas nunca se aprueban.
- Los congresos locales tienen la facultad de legislar, pero con frecuencia les faltan iniciativa, asesores expertos o interés.
- Casi nunca se consulta a la academia, la industria, la sociedad civil o el público, y la calidad de las leyes lo resiente.
- Las propuestas valiosas suelen esperar hasta que un incidente golpea a quien tiene poder político; entonces se hacen visibles el daño, la impunidad y las lagunas.

Aun así, la legislación se fortalece a pasos cortos. Como su velocidad no alcanza a la de las conductas indebidas, el temario insiste en una idea incómoda: mientras la ley se actualiza, buena parte de la seguridad de cada usuario depende de él mismo.

> **Actualización al 2026:** la Ciudad de México incorporó el phishing como delito autónomo en el artículo 231 Bis de su Código Penal: castiga con 3 a 6 años de prisión y 200 a 600 UMA a quien obtenga datos personales, financieros o de autenticación mediante mensajes, sitios o aplicaciones falsas, con aumento de hasta la mitad si la víctima es menor de edad, adulta mayor o tiene una discapacidad (Romero Guevara, 2026). Es un ejemplo de que los estados avanzan, cada uno por su lado.

## Leyes especiales con delitos informáticos

No todo está en los códigos penales. Dos leyes generales y federales contienen figuras directamente ligadas a la tecnología:

> Artículo 20 Quáter.- Violencia digital es toda acción dolosa realizada mediante el uso de tecnologías de la información y la comunicación, por la que se exponga, distribuya, difunda, exhiba, transmita, comercialice, oferte, intercambie o comparta imágenes, audios o videos reales o simulados de contenido íntimo sexual de una persona sin su consentimiento, sin su aprobación o sin su autorización y que le cause daño psicológico, emocional, en cualquier ámbito de su vida privada o en su imagen propia.
> Así como aquellos actos dolosos que causen daño a la intimidad, privacidad y/o dignidad de las mujeres, que se cometan por medio de las tecnologías de la información y la comunicación.
> [...]
> La violencia digital será sancionada en la forma y términos que establezca el Código Penal Federal.

(Ley General de Acceso de las Mujeres a una Vida Libre de Violencia, capítulo adicionado el 1 de junio de 2021.)

> Artículo 62. Se impondrán de tres meses a tres años de prisión al que, estando autorizado para tratar datos personales, con ánimo de lucro, provoque una vulneración de seguridad a las bases de datos bajo su custodia.
> Artículo 63. Se sancionará con prisión de seis meses a cinco años al que, con el fin de alcanzar un lucro indebido, trate datos personales mediante el engaño, aprovechándose del error en que se encuentre la persona titular o la persona autorizada para transmitirlos.
> Artículo 64. Tratándose de datos personales sensibles, las penas a que se refiere este Capítulo se duplicarán.

(Ley Federal de Protección de Datos Personales en Posesión de los Particulares, publicada el 20 de marzo de 2025.)

> **Actualización al 2026:** la Ley Federal de Protección de Datos Personales en Posesión de los Particulares de 2010 fue abrogada; la nueva ley se publicó en el DOF el 20 de marzo de 2025 y entró en vigor al día siguiente. En el mismo paquete desapareció el INAI: sus funciones de protección de datos frente a particulares pasaron a la Secretaría Anticorrupción y Buen Gobierno. Los delitos se conservan, ahora en los artículos 62 a 64.

## Otras ciberconductas: lo que la ley no alcanza

El temario analiza cinco conductas lesivas que ocurren y que ningún código penal describe con precisión. No son curiosidades: muestran cómo la tecnología crea relaciones jurídicas nuevas antes de que el derecho las nombre.

1. **Ciberespionaje.** El espionaje está tipificado, pero su versión digital no tiene una figura propia, y eso deja a personas e instituciones expuestas a ataques sofisticados, reconocidos y documentados, que no se sancionan como tales. El temario recomienda la crónica de Baltazar (2018) sobre una red de bandidos mexicanos que espiaron para Estados Unidos mucho antes de que existiera la CIA: cuando una actividad no está bien definida en la ley, se adapta a cada nueva tecnología sin que la justicia la alcance.
2. **Sismicidad inducida.** Un sismo natural no es delito. Pero hay sismos provocados por actividad humana, como la fracturación hidráulica (fracking), y no existe tipo penal ni denominación jurídica para el daño que causan; la ciberseguridad entra en juego porque esas operaciones dependen de sistemas de control. EITB (2018) lo discute en una entrevista radiofónica.
3. **Biohacking.** El acceso ilícito está regulado en algunos códigos, aunque con palabras propias del idioma jurídico nacional y no con el término inglés. La idea de "hackearse a sí mismo", modificando el propio cuerpo con tecnología, desafía las categorías legales y plantea problemas éticos (Rojas Andia, 2018).
4. **Inteligencia artificial autónoma.** Una nota de 2023 reportó que un sistema de IA tomó el control de un satélite chino para buscar objetivos (Redacción, 2023). No hay regulación nacional aplicable a una IA que controla sistemas críticos, y aun si un tratado internacional cubriera algo, identificar y responsabilizar a las personas detrás es muy difícil. Las estructuras legales no están listas para asignar responsabilidad por actos de sistemas autónomos.
5. **Falsificación de GPS.** Desde 2017 se documentan ataques que engañan a los receptores de posicionamiento con señales falsas (Álvarez, 2017), y no hay precedente jurídico ni sanción específica.

Que la ley tarde no significa que no se pueda hacer nada: académicos, expertos e investigadores pueden construir desde ya las bases teóricas y dogmáticas para que los congresos legislen cuando llegue el momento. El Senado de la República (2023) organizó un foro para discutir una estrategia nacional de inteligencia artificial; es el tipo de antecedente que hace posible una regulación futura.

> **Actualización al 2026:** la Unión Europea aprobó el Reglamento (UE) 2024/1689, conocido como Ley de Inteligencia Artificial, publicado en julio de 2024 y en vigor desde el 1 de agosto de ese año, con obligaciones escalonadas: las prácticas prohibidas aplican desde febrero de 2025, las de los modelos de uso general desde agosto de 2025 y las de transparencia desde agosto de 2026. En julio de 2026, el Ómnibus digital sobre IA (Reglamento (UE) 2026/1744) movió las obligaciones de los sistemas de alto riesgo a diciembre de 2027 y agosto de 2028. México no tiene todavía una ley equivalente.

## Sharenting: compartir la vida de los hijos

El temario cierra el recorrido por conductas sin regular con una reflexión de Silvina Tantone, especialista en educación y nuevas tecnologías, publicada por WeLiveSecurity: el hábito de madres y padres de publicar fotos y datos de sus hijos en redes sociales (*sharenting*, de *share* y *parenting*). No es delito, pero tiene consecuencias:

- **Privacidad y seguridad.** Las imágenes y detalles personales exponen a los menores al acoso y a la explotación en línea.
- **Desarrollo emocional.** La exposición constante los somete a presión social y a comentarios que afectan su autoestima.
- **Permanencia.** Lo que se publica es muy difícil de borrar por completo, y los datos se vuelven más sensibles conforme el menor crece.
- **Consentimiento.** Publicar sin que el menor pueda consentir plantea problemas éticos y legales.

## El Convenio de Budapest y lo que viene después

El Convenio sobre la Ciberdelincuencia del Consejo de Europa (Budapest, 2001) es la referencia mundial: define los delitos contra la confidencialidad, la integridad y la disponibilidad de sistemas y datos, y fija reglas de cooperación internacional. Funciona como ley modelo para los países que construyen su propia legislación. Tiene, sin embargo, más de veinte años, y la infraestructura y las circunstancias de Europa no son las de América Latina; eso influye en cómo se determina la responsabilidad de los autores y en cómo se aplica el convenio en cada país.

Como algunas legislaciones nacionales dificultan una adhesión plena, el temario plantea la posibilidad de un convenio regional latinoamericano, adaptado a las realidades de la región, y recomienda leer el apartado *El debate sobre ciberdelitos más allá del Convenio de Budapest* del informe de Martins dos Santos (2022, pp. 35-37). Para participar en algo así hay que conocer bien el convenio original.

> **Actualización al 2026:** el Convenio de Budapest llegó a 84 Partes el 14 de octubre de 2026, cuando se adhirió Kazajistán; para ese país entra en vigor el 1 de febrero de 2027 (Consejo de Europa, 2026). México sigue sin ser Parte: figura entre los Estados invitados a adherirse.

> **Actualización al 2026:** la Asamblea General de la ONU adoptó el 24 de diciembre de 2024 la Convención de las Naciones Unidas contra la Ciberdelincuencia (resolución 79/243), primer tratado mundial en la materia. Se abrió a firma en Hanói en octubre de 2025; a noviembre de 2026 suma 96 firmantes y 3 ratificaciones y aún no entra en vigor (necesita 40). México no aparece entre los firmantes; la firma sigue abierta en Nueva York hasta el 31 de diciembre de 2026 (Naciones Unidas, 2026).

## Las cifras: un crecimiento exponencial

El temario usa las estadísticas para mostrar que el ciberdelito crece mucho más rápido que su regulación. Las series que maneja y su actualización:

| Indicador | Dato | Fuente |
|---|---|---|
| Internautas en México | 84.1 millones (2021), 88.6 millones (2022), 96 millones (2023), 101.9 millones (2024) | Asociación de Internet MX, estudios anuales de hábitos de usuarios |
| Internautas en México (cifra oficial) | 104.9 millones, el 86.1 % de la población de 6 años y más (2025) | INEGI, ENDUTIH 2025 |
| Víctimas de ciberacoso | 19.4 millones de usuarios de 12 años y más, el 20.4 % (2025); 21.5 % de las mujeres y 19.2 % de los hombres | INEGI, MOCIBA 2025 |
| Persona acosadora | 61.1 % de las víctimas no sabía quién era | INEGI, MOCIBA 2025 |
| Costo mundial del cibercrimen | Proyección de 10.5 billones de dólares anuales para 2025 | Cybersecurity Ventures |

> **Actualización al 2026:** las dos filas de INEGI son las más recientes publicadas (ENDUTIH 2025, junio de 2026; MOCIBA 2025, julio de 2026) y sustituyen a las cifras de pandemia que usaba el material. El MOCIBA 2025 también reporta que 36.8 % de las mujeres y 37.8 % de los hombres víctimas fueron contactados con identidades falsas.

Las lecturas del tema invitan a un análisis crítico de las conductas cometidas por medios informáticos y a construir una cultura de ciberseguridad y de legalidad. El temario también recoge las áreas jurídicas donde hay más por hacer: definiciones actualizadas (identidad, metaversos), perfiles claros para ciberpolicías y peritos, regulación por tipo de servicio de internet, ubicación del derecho informático como rama, actualización penal, reglas de territorialidad, identificación certera del usuario sin sacrificar el derecho al anonimato y protocolos científicos de evidencia digital con reconocimiento jurídico.

## Glosario

| Término | Definición |
|---|---|
| Ciberdelito | Conducta tipificada en una ley penal que se comete por medios informáticos o contra ellos |
| Delito informático | Denominación que usan varios códigos estatales; para Cámpoli, el que protege la integridad física o lógica de equipos y sitios |
| Delito telemático | El que reduce la capacidad de transmitir o procesar datos a distancia sin afectar un equipo concreto |
| Ciberconducta atípica | Conducta dañina en el entorno digital que ninguna ley penal describe |
| Acceso ilícito | Conocer, copiar, modificar o destruir información de un sistema sin autorización o abusando de ella (CPF, 211 bis 1 a 5) |
| Mecanismo de seguridad | Protección del sistema que el tipo penal federal exige para que el acceso sin autorización sea delito |
| Ley Olimpia | Conjunto de reformas de 2021 que reconoce la violencia digital y castiga la difusión de contenido íntimo sin consentimiento |
| Violencia digital | Acción dolosa con TIC que expone contenido íntimo sin consentimiento o daña la intimidad o dignidad de las mujeres |
| Suplantación de identidad | Asumir una identidad que no es la propia |
| Usurpación | Tomar u ocupar ilegalmente algo que pertenece a otro |
| Ciberespionaje | Obtención encubierta de información por medios digitales; sin tipo penal propio en México |
| Sismicidad inducida | Movimiento sísmico provocado por actividad humana, como el fracking |
| Biohacking | Modificación del propio cuerpo o de material biológico con técnicas de tecnología e informática |
| Suplantación de GPS | Envío de señales falsas para engañar a un receptor de posicionamiento |
| Sharenting | Publicación habitual, por madres y padres, de fotos y datos de sus hijos en redes |
| Convenio de Budapest | Tratado del Consejo de Europa sobre ciberdelincuencia (2001), referencia mundial |

## Referencias

- Álvarez, R. (2017, 11 de agosto). Se presenta el primer caso de falsificación de GPS: una nueva forma de ataque electrónico. *Xataka*. https://www.xataka.com/seguridad/se-presenta-el-primer-caso-de-falsificacion-de-gps-una-nueva-forma-de-ataque-electronico
- Asociación de Internet MX. (2024). *20 Estudio sobre los hábitos de usuarios de internet en México 2024*. https://irp.cdn-website.com/81280eda/files/uploaded/20_Ha-bitos_de_Usuarios_de_Internet_en_Me-xico_2024_VP.pdf
- Baltazar, E. (2018, 4 de agosto). "La Compañía", la increíble historia de los bandidos mexicanos que se convirtieron en espías para Estados Unidos mucho antes de la CIA. *Infobae*. https://www.infobae.com/america/mexico/2018/08/04/la-compania-la-increible-historia-de-los-bandidos-mexicanos-que-se-convirtieron-en-espias-para-estados-unidos-mucho-antes-de-la-cia/
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Código Penal Federal* (Última reforma DOF 13-03-2026). https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Ley General de Acceso de las Mujeres a una Vida Libre de Violencia* (Última reforma DOF 15-01-2026). https://www.diputados.gob.mx/LeyesBiblio/pdf/LGAMVLV.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley Federal de Protección de Datos Personales en Posesión de los Particulares* (DOF 20-03-2025, última reforma DOF 14-11-2025). https://www.diputados.gob.mx/LeyesBiblio/pdf/LFPDPPP.pdf
- Congreso del Estado de Sinaloa. (2018). *Código Penal para el Estado de Sinaloa*. http://www.congresosinaloa.gob.mx/images/congreso/leyes/zip/codigo_penal_12-feb-2018.pdf
- Consejo de Europa. (2001). *Convenio sobre la Ciberdelincuencia* (STE n.º 185). https://www.coe.int/en/web/cybercrime/the-budapest-convention
- Consejo de Europa. (2026, 14 de octubre). *Kazakhstan becomes the 84th Party to the Convention on Cybercrime* [Noticia del Comité del Convenio sobre la Ciberdelincuencia]. https://www.coe.int/en/web/cybercrime/t-cy-news
- EITB. (2018, 5 de noviembre). *El reto de la ciberseguridad y fracking y riesgo de sismicidad inducida* [Programa de radio]. En *La mecánica del caracol*. Radio Euskadi. https://www.eitb.eus/es/radio/radio-euskadi/programas/la-mecanica-del-caracol/detalle/5969358/el-reto-ciberseguridad-fracking-riesgo-sismicidad-inducida/
- Instituto Nacional de Estadística y Geografía. (2026). *Encuesta Nacional sobre Disponibilidad y Uso de Tecnologías de la Información en los Hogares (ENDUTIH) 2025* [Reporte de resultados]. https://www.inegi.org.mx/contenidos/saladeprensa/boletines/2026/endutih/ENDUTIH_25_RR.pdf
- Instituto Nacional de Estadística y Geografía. (2026). *Módulo sobre Ciberacoso (MOCIBA) 2025* [Reporte de resultados 24/26]. https://www.inegi.org.mx/contenidos/saladeprensa/boletines/2026/mociba/MOCIBA2025_RR.pdf
- Martins dos Santos, B. (2022). *Convenio de Budapest sobre la Ciberdelincuencia en América Latina: Un breve análisis sobre adhesión e implementación en Argentina, Brasil, Chile, Colombia y México*. Derechos Digitales. https://www.derechosdigitales.org/wp-content/uploads/ESP-Ciberdelincuencia-2022.pdf
- Morgan, S. (2020). Cybercrime to cost the world $10.5 trillion annually by 2025. *Cybercrime Magazine*. https://cybersecurityventures.com/hackerpocalypse-cybercrime-report-2016/
- Naciones Unidas. (2026). *Estado de la Convención de las Naciones Unidas contra la Ciberdelincuencia* (capítulo XVIII-16). United Nations Treaty Collection. https://treaties.un.org/Pages/ViewDetails.aspx?src=TREATY&mtdsg_no=XVIII-16&chapter=18&clang=_en
- Redacción. (2023, 22 de abril). Una inteligencia artificial tomó el control de un satélite chino y buscó a los enemigos del país. *El Imparcial*. https://web.archive.org/web/20230426030519/https://www.elimparcial.com/tecnologia/Una-inteligencia-artificial-tomo-el-control-de-un-satelite-chino-y-busco-a-los-enemigos-del-pais-20230422-0034.html
- Rojas Andia, K. (2018, 12 de julio). Biohacking o la tendencia de hackearse a sí mismo. *Gestión*. https://web.archive.org/web/20180713215615/https://gestion.pe/tendencias/estilos/biohacking-tendencia-hackearse-238241
- Romero Guevara, V. (2026, 27 de julio). CDMX ya convirtió el phishing en un delito y quienes roben datos podrían pasar hasta nueve años en la cárcel. *Xataka México*. https://www.xataka.com.mx/legislacion-y-derechos/cdmx-convirtio-phishing-delito-quienes-roben-datos-podrian-pasar-nueve-anos-carcel
- Senado de la República. (2023). *Hacia una Estrategia Nacional de Inteligencia Artificial* [Video]. YouTube. https://www.youtube.com/watch?v=3m_i5HBv22o
- Tantone, S. (2019, 20 de mayo). Una invitación a reflexionar sobre el alcance del sharenting y sus consecuencias. *WeLiveSecurity*. https://www.welivesecurity.com/la-es/2019/05/20/invitacion-reflexionar-alcance-sharenting/
- Tapia, E. (2018). Guía: qué son el phishing, pharming, vishing, smishing y cómo prevenirte. *WeLiveSecurity*. https://www.welivesecurity.com/la-es/
- Unión Europea. (2024). *Reglamento (UE) 2024/1689 del Parlamento Europeo y del Consejo, por el que se establecen normas armonizadas en materia de inteligencia artificial*. EUR-Lex. https://eur-lex.europa.eu/eli/reg/2024/1689/oj
- Unión Europea. (2026). *Reglamento (UE) 2026/1744 por el que se modifican los Reglamentos (UE) 2024/1689, (UE) 2018/1139 y (UE) 2023/1230 en lo que respecta a la simplificación de la aplicación de normas armonizadas en materia de inteligencia artificial (Ómnibus digital sobre IA)*. EUR-Lex. https://eur-lex.europa.eu/eli/reg/2026/1744/oj

---

*Rogelio Arriaga Gonzalez*
