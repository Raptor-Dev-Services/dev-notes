# 05 - Cibercriminalidad: fundamentos de investigación en México

> Lectura del curso [Informática forense](../../cursos/digital-forensics/README.md), unidad 2, tema 1.

## Por qué importa

Quien investiga un delito cometido con tecnología necesita reconocer el arma antes de buscar al autor. Un correo que pide "confirmar" una cuenta, un cajero con una pieza de plástico de más, una página de banco que se ve idéntica a la real: cada uno deja rastros distintos y exige preguntas distintas. Esta nota reúne el vocabulario técnico y el panorama de tendencias que el resto de la materia da por sabido: qué es un código malicioso y cómo se clasifica, qué publican los equipos de respuesta a incidentes, por qué la nube y el teléfono cambiaron el mapa de riesgos, y qué hacen los grupos hacktivistas.

El tema sigue el capítulo de tendencias del libro base de la materia (Lira Arteaga). La [nota 01](01-derecho-positivo-mexicano.md) explica el marco del derecho positivo mexicano que se apoya en el mismo libro; aquí se desarrolla el fenómeno criminal y sus herramientas.

## Un término que todavía se discute

En la legislación de muchos países, México incluido, "cibercrimen" no tiene una definición cerrada. Lo que sí cambió la conversación fue la escala de las amenazas: los atentados del 11 de septiembre de 2001 se planearon con apoyo de tecnologías de información y comunicación (TIC), y desde entonces el terrorismo digital contra sistemas financieros, de defensa y bases de datos, junto con la propagación de virus, empujó a los Estados a legislar de forma coordinada sobre las conductas que usan equipos de cómputo y redes **como fin** (el sistema es el blanco) o **como medio** (el sistema es la herramienta).

El instrumento que sirvió de modelo a muchos países es el **Convenio de Budapest** (Convenio sobre la Ciberdelincuencia del Consejo de Europa, 2001). Define conductas delictivas relacionadas con la tecnología, sus sanciones y las obligaciones de quienes alojan y transmiten información, como los proveedores de servicios de internet. Se estudia a fondo en la [nota 08](08-convenio-de-budapest.md).

Para efectos prácticos, en México la **ciberdelincuencia** es el conjunto de conductas que los códigos penales ya sancionan y que se cometen usando las TIC como medio o como fin. Su origen está en la propia sociedad conectada: productores, distribuidores y consumidores necesitan comunicarse, y los proveedores de telefonía, internet y servicios de valor agregado compiten por ofrecer equipos y servicios más rápidos y más cómodos (incluidos el procesamiento y el almacenamiento remotos, la nube), aunque muchas veces menos seguros. A eso se suma la falta de cultura de seguridad de la información, dispositivos portátiles que salen al mercado sin protección suficiente y la costumbre de compartir de más en redes sociales. El libro calcula que los delincuentes tienen a su alcance a más de 4,388 millones de víctimas potenciales cada día (la cifra de usuarios de internet de 2019).

## Delitos viejos con herramientas nuevas

Casi todo lo que se persigue como ciberdelito ya existía antes de las computadoras. Lo nuevo es el medio: equipos de cómputo y comunicación conectados a redes públicas o privadas de telefonía y datos (internet, intranets). Ejemplos que el libro enumera:

- distribución no autorizada de obras literarias, musicales y cinematográficas protegidas por derechos de autor;
- distribución de pornografía infantil;
- amenazas y extorsión;
- fraude;
- espionaje;
- falsificación de documentos y suplantación de identidad (el texto lo llama "robo de personalidad");
- robo y revelación de secretos industriales.

Frente a ellos hay dos frentes de respuesta: uno **técnico**, los equipos de respuesta a emergencias de cómputo (CERT), y uno **legal**, las leyes que cada país adopta, con el Convenio de Budapest como referencia.

## Los equipos de respuesta a emergencias de cómputo (CERT)

Un CERT (*Computer Emergency Response Team*) concentra y difunde información sobre ataques y fallas de seguridad en redes y sistemas. El primero nació en Estados Unidos en noviembre de 1988, en el Software Engineering Institute de la Universidad Carnegie Mellon y con fondos de la agencia de proyectos de investigación de defensa (DARPA), como reacción al **gusano de Morris**, el programa de Robert Morris que ese mismo mes infectó alrededor de 6,000 computadoras conectadas a la red. Hoy hay equipos de este tipo en los cinco continentes; para que uno sea reconocido como el equipo de respuesta de un país debe cumplir ciertas reglas, y su tarea es implantar y gestionar medidas técnicas que bajen el riesgo para los sistemas que dan servicio a la comunidad.

Lo que publican estos equipos se agrupa en cuatro tipos de información:

| # | Qué publican | Para qué le sirve al investigador |
|---|---|---|
| 1 | Vulnerabilidades de los sistemas operativos | Saber por dónde pudo entrar el atacante |
| 2 | Códigos maliciosos detectados y sus contramedidas | Reconocer la muestra y cómo se neutraliza |
| 3 | Recomendaciones de buenas prácticas de seguridad de la información | Contrastar lo que la víctima hizo con lo que debió hacer |
| 4 | Incidentes en el mundo | Ubicar el caso dentro de una campaña mayor |

El libro insiste en una idea: los delincuentes tienen éxito porque usuarios y empresas desconocen la "sintomatología" de los sistemas con los que procesan, guardan y transmiten datos. Por eso los CERT son la mejor fuente para identificar tendencias y aplicar medidas proactivas y reactivas, pero solo sirven si los responsables o custodios de la información se mantienen al tanto de lo que pasa en el país y en el mundo.

> **Actualización al 2026.** En México el centro nacional de respuesta a incidentes es el **CERT-MX**, que nació en la División Científica de la Policía Federal. Esa corporación fue absorbida a partir de 2019 por la **Guardia Nacional**, cuya ley vigente (DOF 16-07-2025) le asigna la vigilancia preventiva de sitios en internet; se cita en la [nota 07](07-cibercrimen-en-mexico.md). El CERT/CC original sigue operando dentro del Software Engineering Institute.

## La escala del problema

La página del tema propone un ejercicio para dimensionar el riesgo: si apenas el 1 % de los usuarios de internet tuviera inclinaciones delictivas, con algo más de 4 mil millones de usuarios habría más de 40 millones de posibles ciberdelincuentes; las conclusiones de la unidad repiten la cuenta con 5 mil millones de usuarios (unos 50 millones). No es una estadística, es una forma de ver que conectarse desde casa pone a cualquiera al alcance de muchísima gente con malas intenciones, dispuesta a robar, defraudar, extorsionar o incluso cometer actos de terrorismo.

Cifras históricas que reúne el libro (todas con su año y su fuente; son referencias de tendencia, no datos actuales):

| Dato | Año | Fuente citada |
|---|---|---|
| Más de 550 millones de víctimas de ciberdelincuentes, cuando había unos 2,000 millones de usuarios de internet | 2012 | Blog español Intercriminis |
| Más de 340 millones de nuevos códigos maliciosos, 36 % más que el año anterior | 2015 | Norton / Symantec, reporte de amenazas 2016 |
| 529 millones de identidades expuestas, un alza de 29 % | 2015 | Norton / Symantec, 2016 |
| Más de un millón de ataques lanzados desde sitios web legítimos; más del 75 % de los sitios con vulnerabilidades aprovechables | 2015 | Norton / Symantec, 2016 |
| Ataques de extorsión por cifrado (*ransomware*) 35 % arriba, ya también en Linux, Mac, relojes y televisiones inteligentes | 2015 | Symantec |
| Costo de una fuga de datos: 3.79 millones de dólares, contra 3.52 millones el año previo | 2015 | IBM X-Force 2016 |
| Pronóstico de impacto del cibercrimen: 2.1 billones de dólares (en el texto, "trillones", calco del inglés *trillion*) | 2019 | Proyección citada por el libro |

Las tendencias con más presencia en ese periodo fueron: delitos que cruzan fronteras, fraude con tarjetas de crédito, ataques contra dispositivos móviles, ataques que aprovechan el anonimato y el cifrado, fraudes con los nuevos esquemas de pago y ataques a sistemas biométricos. El libro aclara que, contra lo que podría esperarse, la situación no mejoró desde su primera edición (2010), y que México aparece en las estadísticas mundiales entre los países con más víctimas en proporción.

> **Actualización al 2026.** La Unión Internacional de Telecomunicaciones estima unos **6 mil millones** de personas en línea en 2025, cerca de tres cuartas partes de la población mundial (UIT, 2025). El Centro de Denuncias de Delitos en Internet del FBI recibió en 2025 **1,008,597** denuncias con pérdidas reportadas por **20,877 millones de dólares**, 26 % más que en 2024 (IC3, 2026). IBM y Ponemon calculan en su edición 2026 un costo promedio mundial de **4.99 millones de dólares** por fuga de datos, 12 % más que el año anterior (IBM, 2026).

## La nube: datos ajenos en territorio ajeno

Guardar información en servidores remotos tiene un problema jurídico que el investigador no puede ignorar: la infraestructura casi nunca está en el país donde se contrata el servicio. Si hay una fuga o una pérdida, la investigación depende de las leyes del lugar donde están los servidores. Además, como un solo proveedor concentra datos de muchos clientes importantes, comprometer un sistema le abre al atacante una enorme cantidad de información, y las probabilidades de identificarlo y procesarlo se vuelven muy bajas.

El cómputo en la nube abarca tres modelos de servicio: software como servicio (SaaS), plataforma como servicio (PaaS) e infraestructura como servicio (IaaS), este último con un crecimiento notable en las empresas. Mientras más datos migran, más interés despiertan entre los delincuentes. Lo que el libro toma del informe de Symantec de 2016:

- la causa principal de los incidentes en la nube era la **mala administración**, no una falla exótica; por ella se publicaron más de 11 mil archivos con datos personales sensibles, que luego se vendían en el mercado negro por menos de 10 dólares;
- vulnerabilidades como **VENOM** permitían escapar de una máquina virtual hacia el sistema anfitrión para robar datos;
- el 16 % de las variantes de código malicioso detectaba si corría en un ambiente virtual, lo que le permitía adaptar su comportamiento;
- en 2015 se documentaron 5,585 vulnerabilidades en este tipo de servicios de almacenamiento y procesamiento.

Conclusión del libro: la nube y las máquinas virtuales exigen administradores más atentos y una configuración de seguridad más robusta.

## El teléfono como blanco

Según los datos que cita el libro:

- En 2015 se vendieron más de 1,400 millones de teléfonos inteligentes, 10 % más que en 2014 (el texto escribe "1.4 billones", otro calco del inglés).
- De acuerdo con el rastreador trimestral de IDC (enero de 2016), cinco de cada seis teléfonos usaban Android y uno de cada siete iOS.
- Ericsson pronosticaba 6,400 millones de suscripciones de teléfonos inteligentes para 2020.
- Gartner, citado por el centro antifraude de RSA en su reporte de 2015, esperaba que las transacciones bancarias desde el teléfono crecieran 42 % al año entre 2011 y 2016.

Las técnicas más comunes contra el usuario móvil son el **vishing** (engaño por llamada telefónica para robar datos) y el **smishing** (lo mismo, por mensaje de texto). Un ejemplo es el servicio de suplantación de remitentes iSPOOF, que mandaba SMS con un nombre falso en el campo del remitente (del tipo "Servicio al cliente del banco XYZ"), ocultaba el número real del delincuente y llevaba a la víctima a un teléfono o a un enlace fraudulento.

En el terreno del código malicioso móvil, el libro describe un troyano bancario de la familia Citadel (el texto lo escribe "Cidetel") y su variante para el canal móvil, **CitMo** (*Citadel in the Mobile*). Pensado para burlar la autenticación fuera de banda que los bancos hacen con contraseñas enviadas al celular, se presentaba como un supuesto programa de seguridad. Una vez instalado, ocultaba los SMS entrantes y silenciaba las alertas del teléfono mientras interceptaba las claves que el banco mandaba, de modo que el atacante podía entrar a la cuenta, manipular operaciones y apoderarse del dinero.

Android, por ser la plataforma más usada y de código abierto, concentraba los ataques: en 2012 se detectaron 350 mil aplicaciones maliciosas, contra unas mil en 2011. Un caso documentado es **Backdoor.AndroidOS.Torec.a**, que usaba la red TOR para que el atacante robara datos sin revelar su ubicación. TOR es una red superpuesta a internet que reenvía el tráfico por varios nodos para que ni el destino ni los intermediarios conozcan la dirección IP real de quien navega.

Otra infección frecuente es la de tipo **adware** o molestia (*annoyance*): el usuario da clic en un anuncio dentro de una página o red social, el equipo queda infectado, aparecen mensajes no pedidos y se alteran registros de configuración para que ciertas aplicaciones se ejecuten solas.

## Trae tu propio dispositivo (BYOD)

Muchas organizaciones no pueden darle a cada área el equipo que necesita, y aceptan que el personal trabaje con sus propios teléfonos, tabletas y laptops. Eso crea un problema de protección de datos. Lo que el libro destaca:

- Una encuesta de SANS (2012) encontró que el 62 % de las organizaciones solo pedía contraseña para acceder a dispositivos móviles.
- En México, más de la mitad de los adultos deja que sus hijos jueguen con el mismo dispositivo, y la mayoría no sabe que existen antivirus para teléfonos.
- Sin políticas de uso, sin capacitación del personal y con directivos y administradores poco informados, el dispositivo móvil termina siendo "una caja fuerte llena de tesoros" cuya combinación está al alcance de cualquier delincuente medianamente preparado.
- RSA (enero de 2015) reportó que en 2014 aparecían cada mes unas 170 mil aplicaciones móviles de alto riesgo, cerca de 2 millones en el año, y que el 98 % de los ataques móviles apuntaba a Android.

## Troyanos financieros y mercados negros

Las agencias que rastrean el delito informático son cada vez más eficientes y sus métodos más homogéneos al llevar un caso ante un tribunal, aunque las leyes procesales y penales se actualizan con lentitud. Los delincuentes lo saben, y por eso los troyanos ahora se desarrollan y venden en círculos cerrados, más difíciles de infiltrar con investigaciones encubiertas. Casos que el libro documenta:

| Familia | Qué hacía | Dato citado |
|---|---|---|
| Zeus, Ice IX, SpyEye, Citadel | Troyanos bancarios clásicos | Su presencia había bajado |
| Bugat v2 (también Feodo o Sodast) | Robo de credenciales de banca en línea; controlado por un grupo privado | Cerca del 14 % de los ataques con troyanos financieros que RSA investigó en 2012 |
| Gozi Prinimalka | Campaña contra bancos de Estados Unidos, atribuida a delincuentes aparentemente rusos; clonaba la configuración del equipo de la víctima en una máquina virtual para que el operador usara su dirección IP genuina | Detectada por RSA en septiembre de 2012; buscaba reclutar más de 100 operadores de botnet |
| Carberp | Robo de credenciales y transferencias fraudulentas | Llegó a venderse en unos 40 mil dólares, cuatro veces más que Zeus o SpyEye |

Una de las técnicas favoritas es el secuestro de la sesión en el navegador (*man in the browser*): el troyano se activa cuando la víctima entra a ciertos sitios y puede leer y alterar en tiempo real lo que viaja por una conexión supuestamente segura. Aite Group estimó pérdidas por robo de cuentas bancarias de más de 455 millones de dólares en 2012 y proyectó 794 millones para 2016.

## Hacktivismo

El hacktivismo surge de la facilidad para formar grupos de opinión y de la velocidad con la que las redes sociales difunden información. Son grupos con motivaciones religiosas, económicas, políticas o sociales que buscan provocar conflicto amparados en el anonimato aparente de internet. El libro menciona a **Anonymous**, conocido por sus ataques de negación de servicio contra gobiernos, y a **Al-Qassam Cyber Fighters**, que saboteaba a instituciones financieras con pérdidas estimadas en 30 mil dólares por cada minuto fuera de línea. La combinación preocupante es la de hacktivistas y delincuentes comunes que aprenden a convertir en dinero la información robada.

Sus métodos:

- **Negación de servicio distribuida (DDoS).** Miles de equipos infectados envían peticiones al mismo servidor en una fecha y hora acordadas, hasta que no puede responder y deja de funcionar. Desde la óptica penal, el libro lo califica de sabotaje.
- **Doxing.** Robar y publicar información comprometedora de personas públicas para presionarlas a actuar como quiere el grupo.
- **Hackeo y exposición.** Robar y divulgar información confidencial de una empresa o institución para extorsionarla o dañar su reputación y sus finanzas.

En el Código Penal Federal, el sabotaje en sentido estricto exige un fin específico (trastornar la vida económica del país o afectar su defensa), y la alteración de información en equipos protegidos tiene su propio tipo. Los dos artículos, en su texto vigente (última reforma DOF 13-03-2026):

Código Penal Federal, artículo 140, primer párrafo:

> Artículo 140.- Se impondrá pena de dos a veinte años de prisión y multa de mil a cincuenta mil pesos, al que dañe, destruya, perjudique o ilícitamente entorpezca vías de comunicación, servicios públicos, funciones de las dependencias del Estado, organismos públicos descentralizados, empresas de participación estatal, órganos constitucionales autónomos o sus instalaciones; plantas siderúrgicas, eléctricas o de las industrias básicas; centros de producción o distribución de artículos de consumo necesarios de armas, municiones o implementos bélicos, con el fin de trastornar la vida económica del país o afectar su capacidad de defensa.

Código Penal Federal, artículo 211 bis 1:

> Artículo 211 bis 1.- Al que sin autorización modifique, destruya o provoque pérdida de información contenida en sistemas o equipos de informática protegidos por algún mecanismo de seguridad, se le impondrán de seis meses a dos años de prisión y de cien a trescientos días multa.
>
> Al que sin autorización conozca o copie información contenida en sistemas o equipos de informática protegidos por algún mecanismo de seguridad, se le impondrán de tres meses a un año de prisión y de cincuenta a ciento cincuenta días multa.

El análisis completo de estos tipos penales está en la [nota 09](09-codigo-penal-federal.md).

## Botnets y filtrado de datos

Las redes de equipos infectados (botnets) aparecieron hacia 2006 como un medio para propagar código malicioso. Con todo lo que recolectan, los delincuentes hacen lo mismo que una empresa con su análisis de grandes volúmenes de datos (*big data*): filtran la información para elegir víctimas o sectores vulnerables desde el punto de vista social, político o económico.

## Código malicioso: definición y clasificación

**Código malicioso** es un programa diseñado para que un equipo, fijo o móvil, haga cosas distintas de aquellas para las que fue programado, sin que el usuario lo autorice. Sus blancos posibles:

- el sistema de archivos;
- el sistema operativo;
- los procesos de cómputo;
- los sistemas de comunicación;
- los medios de almacenamiento;
- archivos con extensiones específicas;
- aplicaciones específicas.

Catálogo de nombres y categorías que maneja el libro, con definiciones propias:

| Nombre | Qué es |
|---|---|
| Fraude BEC (*business email compromise*) | Tras entrar al sistema de una empresa, el atacante obtiene los contactos del director general y escribe a sus subordinados haciéndose pasar por él para que transfieran dinero a cuentas que no deberían |
| Bomba lógica o de tiempo | Código que espera una condición (una fecha, un valor en un registro, una interrupción) para ejecutar una acción destructiva; el retraso aleja al autor del momento del daño |
| Gusano | Programa que se reproduce a sí mismo hacia otros equipos de la red. El de anfitrión (*host computer worm*) vive completo en una máquina y, al copiarse a otra, suele terminar en la de origen; el de red (*network worm*) se reparte en segmentos que corren en varias máquinas, coordinados por uno principal (el "pulpo") |
| Hoax | Aviso falso sobre un supuesto virus nuevo y destructivo; su objetivo es que los propios usuarios lo reenvíen hasta saturar buzones y conexiones |
| Bugware | Programa legítimo con errores de diseño, de manejo de recursos o de pruebas que daña datos o abre la puerta a intrusiones y fugas |
| Script de mIRC | Archivo `script.ini` malicioso que llega por el protocolo de intercambio directo (DCC) del cliente de chat IRC, reemplaza al original en la carpeta del programa y le da al atacante acceso a datos privados, incluso para suplantar la sesión de chat |
| Red bot (botnet) | Conjunto de equipos controlados a distancia que se vende o renta en el mercado negro para distribuir código malicioso de forma masiva |
| Ofuscación | Enmascarar el propio código para que los antivirus y los detectores de intrusos no lo reconozcan |
| Pharming | Alterar la resolución de nombres de dominio (en un servidor DNS o en el equipo del usuario) para que, al escribir una dirección legítima, el navegador llegue a la página que eligió el atacante |
| Rootkit | Herramienta o conjunto de herramientas que esconde los procesos y archivos con los que el intruso conserva el acceso; existe para Linux, Solaris, Windows y otros sistemas |
| Infección intermitente (*parse*) | El código infecta solo bajo ciertas condiciones, por ejemplo una de cada diez ejecuciones o solo archivos pequeños, para no llamar la atención |
| Virus blindado (*armored*) | Se rodea de capas de protección y compresión que dificultan rastrearlo y desensamblarlo |
| Virus furtivo (*stealth*) | Mientras está activo oculta sus huellas: tamaño de archivos, fechas, atributos y hasta la memoria que consume |
| Virus mutante o polimórfico | Cambia su código cada vez que se cifra a sí mismo, de modo que ninguna copia tiene la misma firma; es de los más difíciles de detectar |
| Retrovirus | Ataca directamente al antivirus instalado para desactivarlo o burlarlo |
| Macrovirus | Escrito en el lenguaje de macros de una aplicación (procesadores de texto, hojas de cálculo, editores de imagen); no depende del sistema operativo |
| Virus acompañante (*companion*) | En lugar de modificar un archivo, crea otro con el mismo nombre que el sistema ejecuta primero (un `IEXPLORE.COM` falso junto al `IEXPLORE.EXE` real); los verificadores de integridad no lo ven porque solo revisan archivos existentes |
| Spyware | Programa que registra la actividad del usuario (tráfico, claves, cuentas, tarjetas) y la envía a su autor; algunos incluyen un registrador de teclas (*keylogger*) |
| Troyano | Instrucciones ocultas dentro de un programa aparentemente legítimo que hacen algo que el usuario no sabe ni quiere; cuando permite vigilar o controlar el equipo a distancia se le llama puerta trasera (*backdoor*) |
| Phishing | Páginas falsas que imitan dominios legítimos para que la víctima entregue sus datos; suele combinarse con spam y lleva a fraude, suplantación de identidad o falsificación de medios de pago |
| Spoofing | Suplantación a nivel de tablas de ruteo y direcciones IP para interceptar la comunicación en el camino o desviar al usuario a un sitio fraudulento sin que lo note |
| Spam | Correo masivo no solicitado que busca robar o defraudar; tras los desastres de Haití y Japón circularon mensajes que pedían donativos falsos |
| Ingeniería social | Ganarse la confianza de la víctima con engaños para defraudarla o robarle; no requiere conocimientos técnicos, solo psicología |

## Los desafíos de investigar en México

La página del tema subraya que, para entender los alcances y límites de la procuración de justicia en delitos digitales, hay que mirar los obstáculos de quienes investigan:

1. **Tecnología que cambia más rápido que los procedimientos.** Cada plataforma nueva trae evidencia nueva y formas nuevas de destruirla.
2. **Normas que tardan.** Sin leyes procesales y penales adecuadas, la mejor técnica forense puede quedarse sin valor ante un juez.
3. **Recursos y capacitación.** Los equipos que dirigen las investigaciones necesitan herramientas, procedimientos y personal formado para responder con eficacia.
4. **Expectativa de las víctimas.** Quien denuncia espera resultados; la eficiencia del investigador es lo que separa una denuncia de un caso resuelto.

Conocer estos problemas es el primer paso para proponer ajustes en procedimientos, herramientas y leyes; el marco legal con el que hoy se cuenta se revisa en la unidad 3.

## Glosario

| Término | Definición |
|---------|-----------|
| Ciberdelincuencia | Conductas que la ley penal ya sanciona y que se cometen usando las TIC como medio o como blanco |
| CERT | Equipo que recibe, analiza y difunde información sobre incidentes, vulnerabilidades y código malicioso, y coordina la respuesta |
| Código malicioso | Programa que hace que un equipo actúe distinto de lo previsto y sin autorización de su usuario |
| Nube | Uso de infraestructura remota de un tercero para procesar, guardar o comunicar datos (SaaS, PaaS, IaaS) |
| Vishing | Engaño por llamada telefónica para obtener datos personales o financieros |
| Smishing | Engaño por mensaje de texto con el mismo fin |
| TOR | Red superpuesta que reenvía el tráfico por varios nodos para ocultar la dirección IP de quien navega |
| BYOD | Política o práctica de trabajar con dispositivos personales del empleado |
| Man in the browser | Troyano que se instala en el navegador y lee o altera las operaciones en línea de la víctima |
| Hacktivismo | Ataques informáticos con fines ideológicos, políticos o sociales |
| DDoS | Saturar un servicio con peticiones de muchos equipos a la vez hasta dejarlo inaccesible |
| Doxing | Exhibir información privada de una persona para presionarla o dañarla |
| Botnet | Red de equipos infectados que un tercero controla a distancia |
| Pharming | Desvío del usuario a un sitio falso manipulando la resolución de nombres de dominio |
| Ingeniería social | Manipulación psicológica para que la víctima entregue información o dinero |

## Referencias

- Banco de México. (2019). *Informe anual sobre las infraestructuras de los mercados financieros 2018*. https://www.banxico.org.mx/publicaciones-y-prensa/informe-anual-sobre-las-infraestructuras-de-los-me/%7B47E2C88D-F4B1-1D34-2913-8E25F249E6F1%7D.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Código Penal Federal* [Última reforma DOF 13-03-2026]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley de la Guardia Nacional* [Nueva ley DOF 16-07-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LGN.pdf
- Consejo de Europa. (2001). *Convenio sobre la Ciberdelincuencia* (Serie de Tratados Europeos n.º 185). https://rm.coe.int/1680081561
- Federal Bureau of Investigation, Internet Crime Complaint Center. (2026). *2025 IC3 annual report*. https://www.ic3.gov/AnnualReport/Reports/2025_IC3Report.pdf
- IBM y Ponemon Institute. (2026). *Cost of a data breach report 2026*. https://www.ibm.com/reports/data-breach
- Lira Arteaga, O. M. (2018). *Cibercriminalidad: fundamentos de investigación en México*. Ubijus. Edición anterior: Instituto Nacional de Ciencias Penales, 2010, ISBN 978-607-7882-13-8, https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/
- Lledó Benito, I. (2022). *El derecho penal, robots, IA y cibercriminalidad: desafíos éticos y jurídicos. ¿Hacia una distopía?* Dykinson. https://doi.org/10.2307/j.ctv2gz3tqm
- Pérez Arias, J. (2021). Cibercriminalidad: hacia la nueva realidad (virtual) del derecho penal. *Revista Internacional de Doctrina y Jurisprudencia, 26*, 175-193. https://doi.org/10.25115/ridj.vi26.7063
- SANS Institute. (2012). *SANS mobility/BYOD security survey*. Citada por Lira Arteaga (2018).
- Software Engineering Institute. (s. f.). *CERT Division*. Carnegie Mellon University. https://www.sei.cmu.edu/divisions/cert/
- SonicWall. (2022). *2022 SonicWall cyber threat report*. https://www.sonicwall.com/2022-cyber-threat-report
- Symantec. (2016). *Internet security threat report* (vol. 21). Citado por Lira Arteaga (2018); los informes posteriores se publican en https://www.security.com
- Unión Internacional de Telecomunicaciones. (2025, 17 de noviembre). *Connecting the world and beyond* [Comunicado de Facts and Figures 2025]. https://www.itu.int/en/mediacentre/Pages/PR-2025-11-17-Facts-and-Figures.aspx

---

*Rogelio Arriaga Gonzalez*
