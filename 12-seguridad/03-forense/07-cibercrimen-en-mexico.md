# 07 - Cibercrimen en México

> Lectura del curso [Informática forense](../../cursos/digital-forensics/README.md), unidad 2, tema 3.

## Por qué importa

Las técnicas para vulnerar sistemas han cambiado poco en casi dos décadas; lo que no ha avanzado al mismo ritmo son las leyes y los procedimientos para perseguir a quien las usa. Mientras esa brecha siga abierta, el costo lo pagan personas concretas: menores explotados, secuestros que terminan en homicidio, fraudes financieros de grupos organizados, manipulación política, acoso, extorsión y trata. Para investigar un ciberdelito en México hay que conocer al usuario mexicano (cuántos son, cuánto tiempo pasan conectados, con qué dispositivo, para qué), porque ese perfil explica a quién eligen los delincuentes y por dónde entran.

Esta nota aterriza en el país las tendencias globales de la [nota 06](06-cibercrimen-en-el-mundo.md) y retoma las técnicas descritas en la [nota 05](05-cibercriminalidad-fundamentos.md).

## El panorama

Los especialistas y las agencias internacionales ya cuentan con metodologías, herramientas y protocolos para manejar la evidencia digital y preservar su integridad ante un juez; el problema es que, sin una legislación adecuada que los respalde, esos esfuerzos rinden poco. Conforme el país se digitaliza y los servicios en línea se vuelven cotidianos, se multiplican las oportunidades para el fraude financiero y para los ataques a infraestructura crítica, y muchos sectores quedan expuestos.

México no destaca como productor de código malicioso. Donde sí destaca es en:

- fraudes en la banca electrónica;
- servidores que alojan pornografía infantil;
- extorsión;
- trata de personas;
- delincuencia organizada;
- robo de información confidencial;
- distribución de obras protegidas por la legislación de propiedad intelectual.

## Quién está conectado

Los datos de esta sección vienen de los estudios de hábitos de la Asociación de Internet MX (antes AMIPCI), elaborados con información del Consejo Nacional de Población (CONAPO), el INEGI, la autoridad de telecomunicaciones de la época y la consultora Elogia.

### Usuarios de internet, 2006 a 2018

| Año | Millones de usuarios | Crecimiento anual |
|---|---|---|
| 2006 | 20.2 | |
| 2007 | 23.9 | 15.5 % |
| 2008 | 27.6 | 13.4 % |
| 2009 | 30.6 | 9.8 % |
| 2010 | 34.9 | 12.3 % |
| 2011 | 40.6 | 14.0 % |
| 2012 | 45.1 | 10.0 % |
| 2013 | 51.2 | 11.9 % |
| 2014 | 53.9 | 5.0 % |
| 2015 | 65.8 | 18.1 % |
| 2016 | 70.0 | 6.0 % |
| 2017 | 79.1 | 11.5 % |
| 2018 (estimado) | 82.7 | 4.3 % |

Entre 2006 y 2017 el número de usuarios se multiplicó casi por cuatro, y en 2018 la penetración llegó al 71 % de la población de seis años o más. El estudio anticipaba que se llegaría a cerca del 80 %, unos 100 millones de personas. La conclusión del tema es que el país debe prepararse para que las TIC sean una herramienta de desarrollo y no el medio ideal para delinquir, y que la edad pesa mucho en la conciencia con la que se usa la tecnología.

> **Actualización al 2026.** La proyección se cumplió. Según la ENDUTIH del INEGI, en 2024 hubo 100.2 millones de usuarios de internet (83.1 % de la población de seis años o más) y en 2025, **104.9 millones (86.1 %)** (INEGI, 2025, 2026a). El quiz original de la unidad usaba "aproximadamente 80 millones", que era la cifra de 2017 y 2018.

### Perfil del internauta (2018)

| Edad | % de usuarios |
|---|---|
| 6 a 11 años | 12 |
| 12 a 17 años | 14 |
| 18 a 24 años | 18 |
| 25 a 34 años | 22 |
| 35 a 44 años | 14 |
| 45 a 54 años | 12 |
| 55 años o más | 8 |

El estudio señala como grupos de mayor crecimiento a los de 45 a 54 y 55 o más. El reparto por sexo era casi parejo (51 y 49 %). Por nivel socioeconómico: alto 14 %, medio alto 32 %, medio bajo 36 % y bajo 18 % (este último con 5 puntos de crecimiento en dos años). Por región: Centro Sur (Ciudad de México, Estado de México y Morelos) 25 %, Noroeste 15 %, Este 13 %, Oeste 12 %, Noreste 11 %, Centro Norte 11 %, Suroeste 7 % y Sureste 6 %.

Dos lecturas de riesgo:

- **De 6 a 24 años: 44 % de los usuarios**, unos 35 millones de personas. Es la edad más vulnerable en cuanto a seguridad personal. Para niños y jóvenes la red social es el lugar donde se socializa, y quienes extorsionan o reclutan para trata esperan un momento de tristeza o enojo para envolver a la víctima en dudas y engaños que, en muchos casos, terminan en desaparición o muerte.
- **De 25 años en adelante: el otro 56 %.** Aquí el interés del delincuente es la información y el dinero: extorsión, fraude financiero, robo de datos y suplantación de identidad con documentos falsos. Lo facilita la falta de cuidado con tabletas y teléfonos que se cambian por un modelo nuevo antes de aprender a configurarlos.

En promedio, el internauta mexicano pasaba **5.1 horas diarias** conectado. Para un delincuente son cinco horas de contacto posible con la víctima por redes sociales, o cinco horas en que un código malicioso ya instalado puede enviar documentos, copiar contactos o usar el equipo para atacar a otros (negación de servicio, propagación de código).

### A qué hora y para qué

Momentos de conexión (2019; un mismo usuario puede marcar varios):

| Franja | Horario | % que se conecta |
|---|---|---|
| Inicio del día | 6 a 9 h | 75 |
| Media mañana | 10 a 12 h | 82 |
| Mediodía | 12 a 14 h | 90 |
| Comida | 14 a 16 h | 84 |
| Media tarde | 16 a 19 h | 85 |
| Noche | 19 a 21 h | 83 |
| Fin del día | 21 a 24 h | 71 |

El mediodía y la media tarde fueron las franjas de más tráfico, y el 67 % sentía estar conectado las 24 horas.

Actividades en línea más frecuentes (2019):

| % | Actividad |
|---|---|
| 82 | Redes sociales |
| 78 | Chat y llamadas por internet |
| 77 | Correo electrónico |
| 76 | Buscar información |
| 68 | Mapas |
| 65 | Películas y series por streaming |
| 63 | Música y radio por streaming |
| 59 | Consumir contenidos de interés |
| 58 | Banca en línea (5 puntos más que en 2018) |
| 51 | Videollamadas |
| 46 | Compras en línea |
| 42 | Cursos y estudio |
| 41 | Pedir transporte |
| 31 | Trámites con el gobierno |
| 28 | Juegos en línea |
| 26 | Buscar empleo |
| 22 | Mantener sitios o blogs propios |
| 21 | Vender por internet |
| 19 | Sitios para adultos |
| 10 | Buscar pareja |

Si se cruzan estas actividades con las técnicas de la nota 05 se entiende por qué hay que cambiar hábitos: redes sociales, mensajería y streaming son justo los canales por donde llegan enlaces maliciosos, y el reporte indica que el 79 % de quienes entran a redes sociales da clic en vínculos que no conoce.

### Con qué se conectan

| Dispositivo | Lo posee (%) | Lo usa para conectarse (%) |
|---|---|---|
| Teléfono inteligente | 71 | 92 |
| Laptop | 52 | 76 |
| PC de escritorio | 30 | 48 |
| Consola de videojuegos | | 47 (45 % más que en 2018) |
| Tableta | 41 | 42 |
| Otros aparatos electrónicos | | 38 |
| Dispositivos vestibles | | 29 (no aparecían en 2018) |
| Otros | | 13 |

Cómo y dónde (2019):

| Tipo de conexión | % | Lugar | % |
|---|---|---|---|
| Wifi contratado | 47 | Casa | 84 |
| Plan de datos de contrato | 39 | En cualquier lugar, con el móvil | 67 |
| Plan de datos de prepago | 29 | Trabajo | 42 (8 puntos menos que en 2018) |
| Wifi público | 18 | Lugares públicos | 22 |
| Otra (wifi del trabajo, módem móvil) | 21 | Escuela | 8 |
| | | Cibercafé | 7 |
| | | Otros | 18 |

Siete de cada diez internautas pagaban conexión fija y móvil a la vez, coherente con conectarse en casa y en la calle.

## Comercio electrónico: más compras, más blancos

Valor del comercio electrónico en México según la Asociación de Internet MX (miles de millones de pesos):

| Año | Valor | Año | Valor |
|---|---|---|---|
| 2009 | 24.50 | 2014 | 162.10 (12.2 mil millones de dólares) |
| 2010 | 36.50 | 2015 | 257.09 (16.22 mil millones de dólares) |
| 2011 | 54.50 | 2016 | 329.85 (17.63 mil millones de dólares) |
| 2012 | 85.70 | 2017 | 396.04 (20.95 mil millones de dólares, +20.1 %) |
| 2013 | 121.60 | | |

La conversión a dólares usa el tipo de cambio promedio de 2017 (18.90 pesos por dólar). La revista lo redondea como "396.4 billones de pesos"; la unidad correcta es miles de millones.

Para la economía es una buena noticia; para el delincuente significa más usuarios, contraseñas y números de tarjeta que robar con código malicioso, sobre todo porque el **64 %** pagaba con tarjeta de crédito. La recomendación práctica del tema: no hacer operaciones bancarias o de compra desde el mismo equipo con el que juegan los hijos o con el que se abren correos y páginas de todo tipo. Lo ideal es un equipo con antivirus al día, usuario y contraseña, dedicado en lo posible solo a finanzas en sitios confiables. Y una pregunta para el usuario: cuánto vale su cuenta bancaria y cuánto estaría dispuesto a pagar si un extorsionador supiera cuánto gasta en internet.

## El impacto medido: el informe Norton 2013

El primer estudio sobre delitos con TIC en México lo publicó Norton con datos de finales de 2012 a finales de 2013:

| Indicador | México | Mundo |
|---|---|---|
| Costo total del cibercrimen | 39 mil millones de pesos | |
| Costo por víctima | 4,381 pesos (1,963 en 2012: 123 % más) | |
| Adultos víctimas de algún delito ligado a la pérdida o robo del teléfono | 54 % | 27 % |
| Adultos víctimas de incidentes en computadora o en línea | 61 % | 50 % |

Con más de 80 millones de usuarios, el libro estima que eso significaba más de 40 millones de víctimas en un año, y que la incidencia en México era del doble que en el promedio mundial.

Los hábitos tampoco ayudaban. Según el mismo estudio:

- el 70 % usaba el mismo dispositivo móvil para jugar y para trabajar;
- el 66 % revisaba su correo personal en el equipo del trabajo;
- el 58 % entraba a redes sociales desde el equipo del trabajo;
- el 46 % guardaba información personal en el equipo del trabajo;
- el 41 % consideraba inadecuadas las políticas de seguridad de su empresa;
- el 37 % guardaba información personal y laboral en la misma cuenta de nube, que además compartía con amigos y familiares.

La conclusión es tajante: por mucho que una empresa invierta en seguridad perimetral, basta un archivo infectado descargado en un equipo de trabajo para comprometer la infraestructura de la organización y los datos personales del empleado.

## Caso de estudio: los ataques al SPEI (2018)

El Sistema de Pagos Electrónicos Interbancarios (SPEI) del Banco de México mueve las transferencias entre bancos. Lo que dice el propio Banco de México en su informe de 2018:

- En la segunda mitad de abril y la primera de mayo de 2018, **cinco participantes** del SPEI sufrieron ataques contra los aplicativos con los que preparan sus órdenes de pago y se conectan al sistema; en la segunda mitad de octubre hubo un sexto caso, en una institución financiera no bancaria.
- **El sistema central del SPEI no fue atacado** y los recursos de los clientes no estuvieron en riesgo.
- El patrón: transferencias ilegítimas hacia cuentas receptoras reales, casi siempre desde cuentas emisoras inexistentes, y retiro posterior del dinero en efectivo.
- La respuesta: migrar a los participantes afectados y a los de perfil de riesgo parecido a una plataforma alterna (COA-SPEI), poner alertas en el SPEI central para detectar mensajes de pago anómalos, ajustar la regulación y el manual de operación, nombrar un oficial de seguridad del SPEI que verifique el cumplimiento de los requisitos de seguridad informática, establecer un protocolo de reacción con cuatro niveles de alerta y firmar, con otras autoridades financieras, bases de coordinación en seguridad de la información.

Lo que agrega el material de la unidad, con base en notas de prensa de 2019: la Unidad de Inteligencia Financiera bloqueó cuentas de una banda con base en León, Guanajuato, que habría robado hasta 100 millones de pesos al mes mediante hackeo y código malicioso en cajeros, y depositaba el dinero en cuentas de terceros en Jalisco, Tamaulipas y Sinaloa. La Fiscalía General de la República detuvo a ocho integrantes, cateó 11 residencias y aseguró 27 autos de lujo. El material estima lo sustraído al SPEI en unos 400 millones de pesos y afirma que el Banco de México creó a raíz del ataque un área de ciberseguridad dentro de su dirección de tecnologías de la información. El informe anual no publica el monto, pero el Banco de México informó el 22 de mayo de 2018 que los envíos irregulares sujetos a revisión sumaban aproximadamente 300 millones de pesos; los 400 millones del material y los demás datos de prensa se toman como estimaciones.

## Cajeros automáticos y clonación de tarjetas

Los cajeros han sido un problema en México por más de dos décadas (las conclusiones de la unidad hablan de tres). La técnica evolucionó así:

1. **Primera generación.** Un lector falso (*skimmer*) se combinaba con una caja con una computadora que se metía en el hueco del cajero. La víctima pasaba su tarjeta, tecleaba su NIP, la pantalla mostraba un "error del sistema" y la persona se iba a otro cajero. El aparato acumulaba bandas y NIP durante tres a cinco horas.
2. **Generación actual.** La víctima usa el cajero con normalidad y no nota nada: el lector falso va montado sobre el original y una lámina delgada arriba de la pantalla esconde una cámara y un micrófono que apuntan al teclado. Las fotos que acompañan el tema muestran justo lo que esa cámara graba: la mano sobre el teclado, con fecha y hora.
3. **Registradores de teclas.** Físicos o por software, envían todo lo tecleado a una cuenta del delincuente.
4. **Vaciado por software (*jackpotting*).** Algunas vulnerabilidades permiten que un programa ordene al cajero entregar todo su efectivo.

El *skimmer* no se limita a los cajeros: es lo bastante pequeño para usarse en restaurantes, gasolineras y cualquier comercio que cobre con tarjeta. El material complementario remite a un caso documentado en Pensilvania, donde un dispositivo de este tipo permitió robar más de 300 mil dólares de un solo cajero.

Para clonar o usar una tarjeta, el delincuente necesita alguna de estas combinaciones:

| Combinación | Dónde la usa |
|---|---|
| Datos de la banda magnética + NIP | Cajero automático |
| Datos de la banda + firma autógrafa | Comercio con terminal |
| Número, nombre del titular, fecha de vencimiento y código de seguridad del reverso | Compras en internet; bastan fotos de ambas caras de la tarjeta |

## Ejemplos recientes que menciona el material

La página del tema cita como ejemplos un ciberataque contra la red del Servicio de Administración Tributaria en 2023 y un ataque de ransomware a Telmex, además del crecimiento de fraudes y phishing en banca y comercio electrónico. No se localizó una confirmación oficial de esos dos casos concretos, así que se registran solo como mención del material; lo que sí está documentado por fuentes oficiales es la tendencia general.

> **Actualización al 2026.** El Módulo sobre Ciberacoso (MOCIBA) del INEGI estimó que en 2025 el **20.4 %** de la población de 12 años o más usuaria de internet, **19.4 millones** de personas, vivió alguna situación de ciberacoso (21.5 % de las mujeres y 19.2 % de los hombres) (INEGI, 2026b). La Procuraduría General de la República se transformó en la Fiscalía General de la República en diciembre de 2018. La Policía Federal fue absorbida por la Guardia Nacional a partir de 2019, y la vigilancia preventiva en internet quedó a cargo de esta última, cuya ley vigente se publicó en 2025.

Ley de la Guardia Nacional, artículo 9, fracción XXXVII (nueva ley, DOF 16-07-2025):

> Artículo 9. La Guardia Nacional tendrá las atribuciones y obligaciones siguientes:
>
> XXXVII. Realizar acciones de vigilancia, identificación, monitoreo y rastreo en la red pública de Internet sobre sitios web, con el fin de prevenir conductas delictivas;

Desde 2021, la difusión de contenido íntimo sin consentimiento, que el tema incluye entre las formas de acoso, tiene tipo penal federal propio (la reforma conocida como Ley Olimpia). Código Penal Federal, artículo 199 Octies (texto vigente, última reforma DOF 13-03-2026):

> Artículo 199 Octies.- Comete el delito de violación a la intimidad sexual, aquella persona que divulgue, comparta, distribuya o publique imágenes, videos o audios de contenido íntimo sexual de una persona que tenga la mayoría de edad, sin su consentimiento, su aprobación o su autorización.
>
> Así como quien videograbe, audiograbe, fotografíe, imprima o elabore, imágenes, audios o videos con contenido íntimo sexual de una persona sin su consentimiento, sin su aprobación, o sin su autorización.
>
> Estas conductas se sancionarán con una pena de tres a seis años de prisión y una multa de quinientas a mil Unidades de Medida y Actualización.

## Una respuesta integral

El tema cierra con la idea de que ninguna pieza basta sola: hay que actualizar las leyes de forma continua, fortalecer las capacidades de investigación y formar a la población en ciberseguridad. Las conclusiones de la unidad lo resumen así: con más de 80 millones de usuarios en su momento (hoy más de 100), aun una fracción mínima con intención delictiva representa cientos de miles de posibles atacantes; los usuarios mexicanos son muy vulnerables, y fraudes como la clonación de tarjetas en cajeros llevan décadas sin resolverse. Las herramientas legales con las que se cuenta se estudian en la unidad 3, a partir de la [nota 08](08-convenio-de-budapest.md).

## Glosario

| Término | Definición |
|---------|-----------|
| Internauta | Persona que usa internet |
| Penetración | Proporción de la población que usa un servicio, aquí internet |
| Nivel socioeconómico (NSE) | Clasificación de hogares por ingreso, educación y bienes; se usa para segmentar estudios de mercado |
| SPEI | Sistema de Pagos Electrónicos Interbancarios del Banco de México |
| Participante del SPEI | Banco u otra entidad financiera conectada al SPEI para enviar y recibir transferencias |
| Skimmer | Lector oculto que copia los datos de la banda magnética de una tarjeta |
| Jackpotting | Ataque que hace que un cajero entregue todo su efectivo mediante software o un dispositivo conectado |
| NIP | Número de identificación personal de una tarjeta bancaria |
| Comercio electrónico | Compra y venta de bienes o servicios por internet |
| Ciberacoso | Agresión reiterada a una persona por medios digitales: insultos, amenazas, difusión de contenido o suplantación |
| Violación a la intimidad sexual | Delito federal de difundir o producir contenido íntimo de una persona adulta sin su consentimiento |
| Guardia Nacional | Institución de seguridad pública federal que, entre otras funciones, vigila sitios de internet para prevenir delitos |

## Referencias

- Asociación de Internet MX. (2018). *Estudio de comercio electrónico en México 2018*. https://irp.cdn-website.com/81280eda/files/uploaded/Estudio%20de%20comercio%20electro%CC%81nico_2018.pdf
- Asociación de Internet MX. (2019). *15.º Estudio sobre los hábitos de los usuarios de internet en México 2019*. https://irp-cdn.multiscreensite.com/81280eda/files/uploaded/15%2BEstudio%2Bsobre%2Blos%2BHa_bitos%2Bde%2Blos%2BUsuarios%2Bde%2BInternet%2Ben%2BMe_xico%2B2019%2Bversio_n%2Bpu_blica.pdf
- Asociación de Internet MX. (2020). *Estudio sobre comercio electrónico en México 2020*. https://irp.cdn-website.com/81280eda/files/uploaded/Estudio%20de%20Comercio%20Electro%CC%81nico%20en%20Me%CC%81xico%202020.pdf
- Banco de México. (2018). *Información sobre los ataques a participantes del Sistema de Pagos Electrónicos Interbancarios (SPEI)* [Extracto del Informe trimestral enero-marzo 2018, recuadro 6]. https://www.banxico.org.mx/publicaciones-y-prensa/informes-trimestrales/recuadros/%7B86A498AE-5F8A-57CE-2C11-B5059AB9EB20%7D.pdf
- Banco de México. (2018, 22 de mayo). *Puntos importantes sobre la situación actual del SPEI*. https://www.banxico.org.mx/spei/d/%7BB806F1E8-686D-B9F1-0452-EC375543C801%7D.pdf
- Banco de México. (2019). *Informe anual sobre las infraestructuras de los mercados financieros 2018*. https://www.banxico.org.mx/publicaciones-y-prensa/informe-anual-sobre-las-infraestructuras-de-los-me/%7B47E2C88D-F4B1-1D34-2913-8E25F249E6F1%7D.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2026). *Código Penal Federal* [Última reforma DOF 13-03-2026]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CPF.pdf
- Cámara de Diputados del H. Congreso de la Unión. (2025). *Ley de la Guardia Nacional* [Nueva ley DOF 16-07-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/LGN.pdf
- Instituto Nacional de Estadística y Geografía. (2025). *Encuesta Nacional sobre Disponibilidad y Uso de Tecnologías de la Información en los Hogares (ENDUTIH) 2024* [Comunicado]. https://www.inegi.org.mx/contenidos/saladeprensa/boletines/2025/endutih/ENDUTIH_24_RR.pdf
- Instituto Nacional de Estadística y Geografía. (2026a). *ENDUTIH 2025. Reporte de resultados* (16 de junio de 2026). https://www.inegi.org.mx/contenidos/saladeprensa/boletines/2026/endutih/ENDUTIH_25_RR.pdf
- Instituto Nacional de Estadística y Geografía. (2026b). *Módulo sobre Ciberacoso (MOCIBA) 2025. Reporte de resultados* (7 de julio de 2026). https://www.inegi.org.mx/contenidos/saladeprensa/boletines/2026/mociba/MOCIBA2025_RR.pdf
- Lira Arteaga, O. M. (2018). *Cibercriminalidad: fundamentos de investigación en México*. Ubijus. Edición anterior: Instituto Nacional de Ciencias Penales, 2010, ISBN 978-607-7882-13-8, https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/
- Norton. (2013). *Reporte Norton 2013* [Kit de prensa de Symantec para México]. https://web.archive.org/web/20131117035954/http://www.symantec.com/es/mx/about/news/resources/press_kits/detail.jsp?pkid=norton-report-2013

---

*Rogelio Arriaga Gonzalez*
