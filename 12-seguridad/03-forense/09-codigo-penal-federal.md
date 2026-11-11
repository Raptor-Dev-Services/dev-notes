# 09 - El Código Penal Federal y los ciberdelitos

> Lectura del curso [Informática forense](../../cursos/digital-forensics/README.md), unidad 3, tema 2.

## Por qué importa

Quien identifica, preserva y analiza evidencia digital trabaja dentro de la ley o no sirve. El perito es auxiliar de la autoridad: si para cumplir una instrucción del Ministerio Público abre un correo sin orden judicial o entra a un sistema sin autorización, la prueba se pierde y él mismo puede terminar acusado. Saber qué está **permitido** y qué está **obligado a hacer** no es un tema de abogados; es lo que separa una investigación útil de una que se cae en la audiencia.

También es al revés: antes de empezar un análisis conviene saber **qué delito se investiga**, porque de eso depende qué buscar. No se busca lo mismo en un caso de fraude que en uno de amenazas o de pornografía infantil. Esta nota recorre los tipos penales del Código Penal Federal (CPF) que se pueden cometer usando las tecnologías de la información y la comunicación (TIC) como medio o como fin, y los conecta con las técnicas que se usan para cometerlos. El marco general del derecho mexicano está en la [nota 01](01-derecho-positivo-mexicano.md) y el modelo internacional, en la [nota 08](08-convenio-de-budapest.md).

## La idea central: la técnica no es el delito

El CPF define el delito como el acto u omisión que sancionan las leyes penales. En el entorno digital eso tiene una consecuencia que el material del curso repite con insistencia: **casi ninguna técnica de ataque es, por sí misma, un tipo penal**. Es el medio para cometer uno que ya existe.

| Lo que se dice en el medio técnico | Cómo lo encuadra la ley penal |
|---|---|
| Phishing, pharming, ingeniería social para obtener dinero | Fraude (CPF, art. 386) |
| Ransomware: cifrar información y cobrar por liberarla | Extorsión (hoy en una ley general; ver abajo) |
| Denegación de servicio contra sitios de gobierno o servicios públicos | Sabotaje (CPF, art. 140) |
| Entrar a un sistema protegido y copiar o borrar datos | Acceso ilícito a sistemas y equipos de informática (CPF, arts. 211 bis 1 a 211 bis 7) |
| Intervenir comunicaciones privadas | CPF, art. 177 |
| "Ciberbullying" o acoso en redes | Según el caso: amenazas, hostigamiento sexual, violación a la intimidad sexual, lesiones psicológicas |

El autor del material insiste en este punto por una razón práctica: si se cree que no hay ley para "el ciberbullying" o "el phishing", se pierde tiempo pidiendo leyes nuevas mientras los casos se archivan. Muchas de esas conductas están tipificadas desde hace décadas; lo que hace falta es saber vincular el término técnico con el tipo penal y documentarlo bien. También advierte que las penas de algunos tipos informáticos son bajas, así que conviene identificar **todas** las conductas que concurren en un caso: la acumulación de penas evita que el responsable salga bajo fianza y siga delinquiendo.

## Mapa de delitos y métodos

Al cierre del tema, el material resumía en una figura cada delito del CPF con los medios tecnológicos usados para cometerlo. Recreada como tabla:

| Delito | Artículos del CPF | Medios tecnológicos típicos |
|---|---|---|
| Traición a la patria | 123 | Código malicioso, ingeniería social, redes de bots, correo electrónico |
| Espionaje | 127 a 129 | Código malicioso, ingeniería social, intervención de comunicaciones, redes de bots |
| Sedición y motín | 130 y 131 | Redes sociales, blogs, telefonía celular, correo |
| Rebelión | 132 a 135 | Redes sociales, blogs, telefonía celular, correo |
| Terrorismo | 139 a 139 Quinquies | Código malicioso, páginas web, comunicaciones móviles, redes de bots |
| Sabotaje | 140 | Código malicioso, ingeniería social, accesos no autorizados, redes de bots |
| Conspiración | 141 | Redes sociales, blogs, telefonía celular, correo |
| Genocidio | 149 Bis | Redes sociales, blogs, telefonía celular, correo |
| Vías de comunicación | 167 y 168 bis | Código malicioso, intervención y decodificación de comunicaciones |
| Violación de correspondencia | 173, 176 y 177 | Código malicioso, intervención y decodificación de comunicaciones |
| Contra la salud | 193 y 194 | Difusión por internet y dispositivos móviles |
| Corrupción de menores y pornografía infantil | 200 a 202 bis | Difusión por internet y dispositivos móviles |
| Lenocinio y trata | 204 a 206 bis | Difusión por internet y dispositivos móviles |
| Revelación de secretos | 210 a 211 Bis | Código malicioso, ingeniería social, redes de bots |
| Acceso ilícito a sistemas | 211 bis 1 a 211 bis 7 | Código malicioso, ingeniería social, redes de bots |
| Falsedad | 234 a 246 | Falsificación con software y hardware, phishing, difusión en línea |
| Hostigamiento y abuso sexual | 259 Bis y 260 | Redes sociales, blogs, telefonía celular, correo |
| Amenazas | 282 a 284 | Correo, mensajería instantánea, mensajes de texto, telefonía móvil |
| Lesiones | 288 | Correo, mensajería, mensajes de texto, telefonía móvil |
| Homicidio | 302 | Código malicioso contra redes y sistemas informáticos |
| Feminicidio | 325 | Correo, mensajería, mensajes de texto, telefonía móvil |
| Robo | 367 y 368 | Código malicioso, ingeniería social, redes y sistemas informáticos |
| Abuso de confianza | 382 | Correo, mensajería, mensajes de texto, telefonía móvil |
| Fraude | 386 | Phishing, pharming, ingeniería social, código malicioso, redes de bots |
| Operaciones con recursos de procedencia ilícita | 400 Bis | Fraudes financieros, páginas web, dispositivos móviles |
| Electorales | 403 y 405 | Correo, redes sociales, mensajería |
| Derechos de autor | 424 Bis | Código malicioso, ingeniería social, redes de bots |

Actualización al 2026: dos cambios afectan esta tabla. La extorsión ya no está en el CPF: el artículo 390 fue derogado el 28 de noviembre de 2025 y el delito pasó a la Ley General para Prevenir, Investigar y Sancionar los Delitos en Materia de Extorsión. Y desde 2021 existe en el CPF el delito de violación a la intimidad sexual (arts. 199 Octies a 199 Decies), que cubre buena parte de lo que antes se describía solo como acoso en redes. Ambos se desarrollan más abajo.

## Delitos contra la seguridad de la Nación

### Traición a la patria y espionaje

El artículo 123 castiga al mexicano que traiciona a la patria. El material destacaba las fracciones que hoy se pueden cometer con TIC: someter al país a un gobierno extranjero (I), cooperar con hostilidades de otro Estado (II), reclutar gente contra México (V), informar a extranjeros para guiar una invasión (VI), entregarles datos militares (VII), auxiliar a un espía (VIII), invitar a una invasión (XI) y cometer, en guerra, sedición, rebelión, terrorismo o sabotaje (XV).

El espionaje (arts. 127 a 129) ganó visibilidad con filtraciones como las de Edward Snowden y las de WikiLeaks, y hoy combina técnicas tradicionales con código malicioso, ingeniería social y explotación de vulnerabilidades. El CPF distingue tres conductas: el extranjero que en paz o en guerra se relaciona con otro gobierno para guiar una invasión o le entrega datos militares (127), el mexicano que revela a otro gobierno información confidencial de un gobierno extranjero en perjuicio de México (128), y quien conoce la actividad e identidad de un espía y no la denuncia (129).

### Sedición, motín, rebelión y conspiración

Son delitos de organización colectiva, y la organización hoy pasa por redes sociales, mensajería y correo. La **sedición** (art. 130) es resistir o atacar en grupo y sin armas a la autoridad; el **motín** (art. 131), alterar el orden con violencia so pretexto de un derecho; la **rebelión** (arts. 132 a 135) exige armas para abolir la Constitución o destruir instituciones, y el 135 castiga a quien invita a ella "en cualquier forma o por cualquier medio", lo que alcanza una publicación en línea. La **conspiración** (art. 141) es acordar cometerlos. Quien instiga o incita recibe la misma pena (art. 142), y al servidor público se le agrava (art. 145).

### Terrorismo y sabotaje

El terrorismo (art. 139 y siguientes) se relaciona con la vulnerabilidad de los sistemas SCADA que controlan infraestructura crítica: energía, agua, transporte. Un ataque a esos sistemas puede producir el mismo terror en la población que un medio violento tradicional. El capítulo castiga también al que encubre a un terrorista (139 Bis), al que amenaza con cometer terrorismo (139 Ter), al que financia actividades terroristas (139 Quáter) y al que encubre a quien las financia (139 Quinquies).

Actualización al 2026: la multa del artículo 139 ya no se expresa en días multa sino en veces el valor diario de la Unidad de Medida y Actualización (reforma DOF 07-06-2024).

El sabotaje es el tipo penal en el que encaja lo que el medio técnico llama **denegación de servicio**: saturar los servidores que alojan los sitios de una dependencia o de un servicio público hasta dejarlos fuera de línea.

Código Penal Federal, artículo 140 (texto vigente, última reforma DOF 13-03-2026):

> Artículo 140.- Se impondrá pena de dos a veinte años de prisión y multa de mil a cincuenta mil pesos, al que dañe, destruya, perjudique o ilícitamente entorpezca vías de comunicación, servicios públicos, funciones de las dependencias del Estado, organismos públicos descentralizados, empresas de participación estatal, órganos constitucionales autónomos o sus instalaciones; plantas siderúrgicas, eléctricas o de las industrias básicas; centros de producción o distribución de artículos de consumo necesarios de armas, municiones o implementos bélicos, con el fin de trastornar la vida económica del país o afectar su capacidad de defensa.
>
> Se aplicará pena de seis meses a cinco años de prisión y multa hasta de cinco mil pesos, al que teniendo conocimiento de las actividades de un saboteador y de su identidad, no lo haga saber a las autoridades.

### Genocidio

El artículo 149 Bis castiga a quien busca destruir total o parcialmente a un grupo nacional, étnico, racial o religioso; el material lo incluía porque redes y telefonía se han usado para organizar campañas de odio contra minorías.

## Vías de comunicación y correspondencia

Este título interesa por partida doble: describe ataques contra la infraestructura que transporta voz y datos, y fija límites que el propio investigador no debe cruzar.

El artículo 167 castiga, entre otras conductas, a quien destruye o separa componentes de la red de telecomunicaciones (fracción II), a quien difunde información falsa que pone en riesgo una aeronave, un buque u otro vehículo de servicio público federal (fracción IX) y, sobre todo, a quien interrumpe o interfiere comunicaciones.

Código Penal Federal, artículo 167, párrafo primero y fracción VI (texto vigente):

> Artículo 167.- Se impondrán de uno a cinco años de prisión y de cien a diez mil días multa:
>
> VI.- Al que dolosamente o con fines de lucro, interrumpa o interfiera las comunicaciones, alámbricas, inalámbricas o de fibra óptica, sean telegráficas, telefónicas o satelitales, por medio de las cuales se transfieran señales de audio, de video o de datos;

El artículo 168 bis sanciona a quien, sin derecho, descifra o decodifica señales de telecomunicaciones distintas a las satelitales que portan programas, o comercia con aparatos o información que permitan hacerlo.

En **violación de correspondencia**, el material propone leer como correspondencia los correos electrónicos, la mensajería instantánea y los mensajes de texto. Con esa lectura, dos advertencias: los delincuentes cometen estas conductas con código malicioso y técnicas de intrusión, y el investigador puede cometerlas también si, por atender una instrucción ministerial, revisa comunicaciones sin la autorización correspondiente. El artículo 173 castiga a quien abre o intercepta indebidamente una comunicación escrita que no le está dirigida (se persigue por querella); el 174 excluye a padres respecto de hijos menores, a tutores y a cónyuges entre sí; el 176 sanciona al empleado de telégrafos o de una estación telefónica o inalámbrica que deja de transmitir un mensaje y causa daño. El más grave es el 177.

Código Penal Federal, artículo 177 (texto vigente):

> Artículo 177.- A quien intervenga comunicaciones privadas sin mandato de autoridad judicial competente, se le aplicarán sanciones de seis a doce años de prisión y de trescientos a seiscientos días multa.

Esto se lee junto con el artículo 16 constitucional: las comunicaciones privadas son inviolables y solo un juez federal puede autorizar su intervención, a petición de la autoridad que faculte la ley. Ver la [nota 10](10-leyes-complementarias.md) y la [nota 11](11-fundamentos-legales-investigacion.md).

## Delitos contra la salud

Internet y la telefonía móvil se usan para vender, anunciar y financiar sustancias ilegales. El artículo 193 remite a la Ley General de Salud para definir los narcóticos, y el 194 castiga con diez a veinticinco años a quien los comercia sin autorización, a quien financia esas actividades y a quien hace **publicidad o propaganda** para su consumo.

## Delitos contra el libre desarrollo de la personalidad

Para el autor es el capítulo más alarmante: detrás del material sexual con menores que circula en línea hay explotación, secuestro y, en los peores casos, desaparición y muerte. Conocer los artículos evita que quienes aplican los procedimientos normalicen estas conductas.

| Artículo | Conducta |
|---|---|
| 200 | Comerciar, distribuir, exponer u ofrecer a menores de 18 años material pornográfico, real o simulado, físico o por cualquier medio (con excepción del material científico, artístico o educativo aprobado por la autoridad) |
| 201 | Corrupción: obligar, inducir, facilitar o procurar que menores, o personas que no comprenden el hecho o no pueden resistirlo, realicen ciertos actos, entre ellos exhibicionismo corporal o actos sexuales con fin lascivo. No son corrupción los programas educativos aprobados ni las imágenes que constituyen recuerdos familiares; en caso de duda, el juez pide dictamen pericial, también para determinar la edad de la víctima |
| 202 | Pornografía de personas menores de 18 años o que no comprenden o no pueden resistir |
| 202 Bis | Almacenar, comprar o arrendar ese material sin fines de comercio, con tratamiento psiquiátrico obligatorio |
| 203 | Turismo sexual: promover o facilitar por cualquier medio viajes para realizar actos sexuales con esas personas |
| 204 | Lenocinio de menores: explotar su cuerpo o inducirlos al comercio sexual |
| 206 y 206 Bis | Lenocinio en general |
| 208 | Provocar públicamente a cometer un delito o hacer apología de él o de algún vicio |
| 209 | No impedir, pudiendo hacerlo sin riesgo, alguno de estos delitos, o no avisar a la autoridad; alcanza a quienes pertenecen a instituciones religiosas, educativas, deportivas o culturales que protegen al responsable |

Código Penal Federal, artículo 202, párrafo primero (texto vigente):

> Artículo 202.- Comete el delito de pornografía de personas menores de dieciocho años de edad o de personas que no tienen capacidad para comprender el significado del hecho o de personas que no tienen capacidad para resistirlo, quien procure, obligue, facilite o induzca, por cualquier medio, a una o varias de estas personas a realizar actos sexuales o de exhibicionismo corporal con fines lascivos o sexuales, reales o simulados, con el objeto de video grabarlos, fotografiarlos, filmarlos, exhibirlos o describirlos a través de anuncios impresos, transmisión de archivos de datos en red pública o privada de telecomunicaciones, sistemas de cómputo, electrónicos o sucedáneos. Al autor de este delito se le impondrá pena de siete a doce años de prisión y de ochocientos a dos mil días multa.

Los párrafos siguientes extienden la pena a quien fija, graba o fotografía esos actos y a quien reproduce, almacena, distribuye, vende, transmite, importa o exporta el material. Para el perito, cada verbo es una línea de análisis: no es lo mismo encontrar un archivo que demostrar que se distribuyó.

Actualización al 2026: la reforma conocida como Ley Olimpia (DOF 01-06-2021) agregó al CPF el delito de **violación a la intimidad sexual**. Antes, la difusión de imágenes íntimas de adultos sin consentimiento solo se podía encuadrar con dificultad en otros tipos.

Código Penal Federal, artículo 199 Octies (texto vigente):

> Artículo 199 Octies.- Comete el delito de violación a la intimidad sexual, aquella persona que divulgue, comparta, distribuya o publique imágenes, videos o audios de contenido íntimo sexual de una persona que tenga la mayoría de edad, sin su consentimiento, su aprobación o su autorización.
>
> Así como quien videograbe, audiograbe, fotografíe, imprima o elabore, imágenes, audios o videos con contenido íntimo sexual de una persona sin su consentimiento, sin su aprobación, o sin su autorización.
>
> Estas conductas se sancionarán con una pena de tres a seis años de prisión y una multa de quinientas a mil Unidades de Medida y Actualización.

El 199 Nonies aplica la misma pena cuando el material no corresponde a la persona señalada (incluidas las imágenes fabricadas), y el 199 Decies la agrava, entre otros casos, cuando el responsable fue pareja de la víctima o es servidor público.

## Revelación de secretos y acceso ilícito a sistemas

Es el único título del CPF que habla expresamente de sistemas informáticos. Cubre el robo de secretos industriales, el acceso no autorizado a un equipo y la alteración de datos por quien sí tenía acceso. La **revelación de secretos** (art. 210) castiga con trabajo comunitario a quien, sin justa causa y en perjuicio de alguien, revela un secreto conocido por su empleo; el 211 sube la pena cuando lo revela un profesional, técnico o servidor público o cuando el secreto es industrial, y el 211 Bis castiga con seis a doce años a quien revela o usa información o imágenes obtenidas en una intervención de comunicación privada.

El capítulo de **acceso ilícito** distingue dos variables: si el autor tenía autorización o no, y de quién es el sistema (particular, del Estado o del sistema financiero). En todos los casos el sistema debe estar **protegido por algún mecanismo de seguridad**, un requisito que el perito debe documentar: si no lo demuestra, no hay tipo penal.

| Artículo | Sistema | ¿Tenía autorización? | Modificar, destruir o provocar pérdida | Conocer o copiar |
|---|---|---|---|---|
| 211 bis 1 | Cualquiera protegido | No | 6 meses a 2 años | 3 meses a 1 año |
| 211 bis 2 | Del Estado | No | 1 a 4 años | 6 meses a 2 años |
| 211 bis 2, tercer párrafo | Seguridad pública | No | 4 a 10 años (conocer, obtener, copiar o utilizar) | |
| 211 bis 3 | Del Estado | Sí | 2 a 8 años | 1 a 4 años (copiar) |
| 211 bis 3, tercer párrafo | Seguridad pública | Sí | 4 a 10 años (obtener, copiar o utilizar) | |
| 211 bis 4 | Sistema financiero | No | 6 meses a 4 años | 3 meses a 2 años |
| 211 bis 5 | Sistema financiero | Sí | 6 meses a 4 años | 3 meses a 2 años (más la mitad si es empleado) |

Código Penal Federal, artículos 211 bis 1, 211 bis 2, 211 bis 6 y 211 bis 7 (texto vigente):

> Artículo 211 bis 1.- Al que sin autorización modifique, destruya o provoque pérdida de información contenida en sistemas o equipos de informática protegidos por algún mecanismo de seguridad, se le impondrán de seis meses a dos años de prisión y de cien a trescientos días multa.
>
> Al que sin autorización conozca o copie información contenida en sistemas o equipos de informática protegidos por algún mecanismo de seguridad, se le impondrán de tres meses a un año de prisión y de cincuenta a ciento cincuenta días multa.

> Artículo 211 bis 2.- Al que sin autorización modifique, destruya o provoque pérdida de información contenida en sistemas o equipos de informática del Estado, protegidos por algún mecanismo de seguridad, se le impondrán de uno a cuatro años de prisión y de doscientos a seiscientos días multa.
>
> Al que sin autorización conozca o copie información contenida en sistemas o equipos de informática del Estado, protegidos por algún mecanismo de seguridad, se le impondrán de seis meses a dos años de prisión y de cien a trescientos días multa.
>
> A quien sin autorización conozca, obtenga, copie o utilice información contenida en cualquier sistema, equipo o medio de almacenamiento informáticos de seguridad pública, protegido por algún medio de seguridad, se le impondrá pena de cuatro a diez años de prisión y multa de quinientos a mil días de salario mínimo general vigente en el Distrito Federal. Si el responsable es o hubiera sido servidor público en una institución de seguridad pública, se impondrá además, destitución e inhabilitación de cuatro a diez años para desempeñarse en otro empleo, puesto, cargo o comisión pública.
>
> Las sanciones anteriores se duplicarán cuando la conducta obstruya, entorpezca, obstaculice, limite o imposibilite la procuración o impartición de justicia, o recaiga sobre los registros relacionados con un procedimiento penal resguardados por las autoridades competentes.

> Artículo 211 bis 6.- Para los efectos de los artículos 211 Bis 4 y 211 Bis 5 anteriores, se entiende por instituciones que integran el sistema financiero, las señaladas en el artículo 400 Bis de este Código.

> Artículo 211 bis 7.- Las penas previstas en este capítulo se aumentarán hasta en una mitad cuando la información obtenida se utilice en provecho propio o ajeno.

Actualización al 2026: el material transcribía los artículos 211 bis 2 y 211 bis 3 sin los párrafos agregados en 2009 (sistemas de seguridad pública) ni el de 2016 (pena doble cuando se obstruye la procuración de justicia o se tocan registros de un procedimiento penal). El texto citado es el vigente.

## Falsedad

Las impresoras y los equipos de cómputo al alcance de cualquiera permiten falsificar billetes, títulos profesionales y documentos oficiales. Por eso el perito debe saber qué buscar cuando el Ministerio Público le pide revisar un equipo en un caso de falsedad: plantillas, imágenes escaneadas de sellos, diseños de billetes, archivos de documentos oficiales.

| Capítulo | Artículos | Qué castiga |
|---|---|---|
| Moneda | 234 y 235 | Producir, almacenar, distribuir o introducir piezas que imitan monedas o billetes de curso legal; fabricar papel con apariencia de billete; enajenar máquinas que solo sirven para fabricar moneda |
| Títulos y documentos de crédito | 239 y 240 | Falsificar títulos al portador, obligaciones de deuda pública o títulos de empresas, e introducirlos al país |
| Sellos, marcas, llaves | 241 y 242 | Falsificar sellos oficiales, punzones y matrices, sellos de particulares, de bancos o de comercios, y usarlos |
| Documentos en general | 243 a 245 | Falsificar documentos públicos (4 a 8 años) o privados (6 meses a 5 años), agravada si lo hace un servidor público |

El artículo 244 enumera los medios de falsificación (firmas falsas, alteración de un documento verdadero, cláusulas o hechos añadidos, identificaciones oficiales hechas sin autorización, entre otros) y el 245 exige tres requisitos para que la falsificación sea punible: que el falsario busque un provecho o un perjuicio, que resulte o pueda resultar perjuicio a alguien y que actúe sin consentimiento del afectado.

## Delitos contra la libertad sexual y contra la paz de las personas

El hostigamiento sexual es, para el autor, una puerta de entrada: fotografías y videos publicados en redes sirven para iniciar un camino que pasa por la extorsión y puede terminar en trata. El artículo 259 Bis castiga a quien, con fines lascivos, asedia reiteradamente a otra persona valiéndose de una posición de jerarquía laboral, docente, doméstica o de otra subordinación; solo es punible si causa perjuicio y se persigue a petición de parte. El 260 define el abuso sexual e incluye obligar a la víctima a observar un acto sexual o a exhibir su cuerpo sin consentimiento.

Actualización al 2026: la multa del artículo 259 Bis subió de cuarenta a hasta ochocientos días multa, y al servidor público hostigador ahora también se le puede inhabilitar hasta por un año (reforma DOF 15-06-2018).

Las **amenazas** (arts. 282 a 284) son buena parte de lo que los medios llaman ciberacoso. El autor critica el término: llamar "ciberbullying" a conductas que la ley ya castiga como amenazas u hostigamiento desvía la atención mientras cobran vidas de adolescentes, y alimenta la idea falsa de que estamos indefensos.

Código Penal Federal, artículo 282 (texto vigente):

> Artículo 282.- Se aplicará sanción de tres días a un año de prisión o de 180 a 360 días multa:
>
> I.- Al que de cualquier modo amenace a otro con causarle un mal en su persona, en sus bienes, en su honor o en sus derechos, o en la persona, honor, bienes o derechos de alguien con quien esté ligado con algún vínculo, y
>
> II.- Al que por medio de amenazas de cualquier género trate de impedir que otro ejecute lo que tiene derecho a hacer.
>
> Si el ofendido fuere alguno de los parientes o personas a que se refieren los artículos 343 bis y 343 ter, en este último caso siempre y cuando habiten en el mismo domicilio, se aumentará la pena que corresponda hasta en una tercera parte en su mínimo y en su máximo.
>
> Si el ofendido por la amenaza fuere víctima u ofendido o testigo en un procedimiento penal, la pena será de cuatro a ocho años de prisión y de cien a trescientos días multa.
>
> Los delitos previstos en este artículo se perseguirán por querella, con excepción del establecido en el párrafo anterior que se perseguirá de oficio.

El juez puede exigir una caución de no ofender cuando el daño amenazado es leve o evitable, cuando la amenaza se hace con emblemas, señas o frases de doble sentido, o cuando busca impedir un hecho ilícito (art. 283). Si el amenazador cumple, las penas se acumulan (art. 284).

En los delitos **contra la vida y la integridad corporal**, las lesiones incluyen las psicológicas (art. 288): lo que se envía por redes o mensajes para martirizar a una víctima deja secuelas. En homicidio (302) y feminicidio (325) los registros de comunicación suelen ser evidencia central.

## Delitos patrimoniales

El **fraude** es el destino de casi todo el phishing, el pharming, la ingeniería social y el código malicioso con fines económicos, desde la venta de mercancía inexistente hasta el saqueo de cuentas por medio de los sistemas de los bancos.

Código Penal Federal, artículo 386 (texto vigente):

> Artículo 386.- Comete el delito de fraude el que engañando a uno o aprovechándose del error en que éste se halla se hace ilícitamente de alguna cosa o alcanza un lucro indebido.
>
> El delito de fraude se castigará con las penas siguientes:
>
> I.- Con prisión de 3 días a 6 meses o de 30 a 180 días multa, cuando el valor de lo defraudado no exceda de diez veces el salario;
>
> II.- Con Prisión de 6 meses a 3 años y multa de 10 a 100 veces el salario, cuando el valor de lo defraudado excediera de 10, pero no de 500 veces el salario;
>
> III.- Con prisión de tres a doce años y multa hasta de ciento veinte veces el salario, si el valor de lo defraudado fuere mayor de quinientas veces el salario.

La **extorsión** es lo que hacen las bandas que, por correo, mensajería, llamadas o mensajes de texto, amenazan a la víctima para que pague por detener un secuestro simulado, por "protección" o, en el caso de empresas, por recuperar información cifrada con ransomware.

Actualización al 2026: el artículo 390 del CPF fue **derogado** (DOF 28-11-2025). La extorsión se rige ahora por la Ley General para Prevenir, Investigar y Sancionar los Delitos en Materia de Extorsión, publicada el mismo día, que eleva la pena base a quince a veinticinco años y la agrava cuando se usan medios de telecomunicación.

Ley General para Prevenir, Investigar y Sancionar los Delitos en Materia de Extorsión, artículo 15 y artículo 17, fracción V (nueva ley DOF 28-11-2025):

> Artículo 15. A quien, sin derecho, obligue a otro a dar, hacer, dejar de hacer o tolerar algo, obteniendo un beneficio o lucro para sí o para otro o causando a alguien un daño o perjuicio patrimonial, moral, físico o psicológico, se le impondrán de quince a veinticinco años de prisión y una multa de trescientas a quinientas veces el valor diario de la Unidad de Medida y Actualización.

> Artículo 17. Además de las penas señaladas en el artículo 15 de la presente Ley, se aumentará la pena de cinco a doce años de prisión, cuando en la comisión del delito de extorsión se presente alguna de las circunstancias siguientes:
>
> V. Se utilicen dispositivos, medios, servicios o plataformas a través de los cuales se pueda realizar la emisión, transmisión o recepción de signos, señales, escritos, imágenes, voz, datos, sonidos o información de cualquier naturaleza que se efectué a través de hilos, radioelectricidad, medios ópticos, físicos, sistemas electromagnéticos o cualquier otro medio electrónico;

La misma ley agrava la pena, entre otros supuestos, cuando se usan datos personales o imágenes reales, manipuladas o alteradas de la víctima para coaccionarla (art. 17, fr. IV), cuando se finge tener privada de la libertad a una persona (art. 17, fr. VII) y cuando el autor se hace pasar por empleado de un banco o de una dependencia para obtener datos personales o financieros (art. 18, fr. VIII).

Las **operaciones con recursos de procedencia ilícita** (art. 400 Bis, de cinco a quince años) se facilitan con las transferencias móviles: el dinero cruza cuentas de terceros, a veces con ayuda de personal interno, más rápido de lo que los bancos alcanzan a detectar.

## Derechos de autor

La venta y distribución por internet de obras protegidas, desde libros hasta audio y video, es uno de los problemas de mayor impacto económico. El artículo 424 bis castiga con tres a diez años a quien reproduce, distribuye o vende copias de obras protegidas con fin de especulación comercial y sin autorización, y a quien provee los insumos para hacerlo. Su fracción II interesa en especial al informático.

Código Penal Federal, artículo 424 bis, fracción II (texto vigente):

> II. A quien fabrique con fin de lucro un dispositivo o sistema cuya finalidad sea desactivar los dispositivos electrónicos de protección de un programa de computación, o

Actualización al 2026: la reforma de 2020 (DOF 01-07-2020) agregó una fracción III que castiga grabar o copiar una película exhibida en una sala de cine sin autorización.

## Cierre

Aquí solo se revisaron los tipos más frecuentes, y aun así casi cualquier delito del CPF puede cometerse con la tecnología como medio o como fin. La tarea del perito es ubicar la conducta en su tipo penal y documentar los elementos que ese tipo exige, sin cometer él mismo un delito en el camino.

## Glosario

| Término | Definición |
|---------|-----------|
| Tipo penal | Descripción en la ley de la conducta que se castiga y de sus elementos |
| Medio y fin | La TIC es medio cuando se usa para cometer un delito tradicional y fin cuando el ataque va contra el propio sistema o sus datos |
| Querella | Petición de la víctima sin la cual ciertos delitos no se persiguen |
| De oficio | Persecución que la autoridad inicia sin necesidad de querella |
| Acumulación de penas | Suma de las penas de varios delitos cometidos en el mismo caso |
| Mecanismo de seguridad | Control (contraseña, cifrado, autenticación) que debe proteger el sistema para que exista acceso ilícito |
| Sabotaje | Dañar o entorpecer servicios públicos o funciones del Estado; ahí encaja la denegación de servicio |
| Phishing | Engaño, normalmente por correo o mensaje, para que la víctima entregue datos o dinero; se persigue como fraude |
| Pharming | Redirección del usuario a un sitio falso manipulando la resolución de nombres; también se persigue como fraude |
| Ransomware | Código malicioso que cifra información y exige un pago por liberarla; se persigue como extorsión |
| Violación a la intimidad sexual | Difundir o producir sin consentimiento contenido íntimo sexual de una persona adulta (arts. 199 Octies a 199 Decies) |
| Caución de no ofender | Garantía que el juez puede exigir a quien amenaza para que no cumpla su amenaza |
| Unidad de Medida y Actualización | Referencia económica que sustituye al salario mínimo para fijar multas en las leyes recientes |
| SCADA | Sistemas de supervisión y control de infraestructura industrial y crítica |

## Referencias

- Cámara de Diputados del H. Congreso de la Unión. (2026). *Código Penal Federal* [Última reforma DOF 13-03-2026]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley General para Prevenir, Investigar y Sancionar los Delitos en Materia de Extorsión* [Nueva ley DOF 28-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LGPISDME.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Constitución Política de los Estados Unidos Mexicanos* [Última reforma DOF 07-10-2026]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CPEUM.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Código Nacional de Procedimientos Penales* [Última reforma DOF 28-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CNPP.pdf
- Consejo de Europa. (2001). *Convenio sobre la Ciberdelincuencia* (Serie de Tratados Europeos n.º 185). https://rm.coe.int/1680081561
- Lira Arteaga, O. M. (2018). *Cibercriminalidad: fundamentos de investigación en México*. Ubijus. Edición anterior: Instituto Nacional de Ciencias Penales, 2010, ISBN 978-607-7882-13-8, https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/

---

*Rogelio Arriaga Gonzalez*
