# 13 - Obtención y tratamiento de evidencia digital

> Lectura del curso [Informática forense](../../cursos/digital-forensics/README.md), unidad 4, tema 3.

## Por qué importa

La investigación pericial en tecnologías de la información descansa en el método científico: plantear qué se busca, aplicar un procedimiento controlado, registrar lo que se hizo y llegar a conclusiones que otro especialista pueda reproducir. Ese rigor se exige en dos momentos: cuando se identifican, fijan y preservan los indicios, y cuando se escribe la opinión técnica que recibirá la autoridad.

La evidencia digital complica todo lo anterior porque es frágil. Se borra con un clic, cambia con solo encender el equipo y se copia sin dejar huella. Las agencias que más casos han procesado en el mundo publicaron guías de buenas prácticas para que la evidencia llegue al juez íntegra y explicable. Esta nota recorre esas guías, las define en el contexto mexicano y aterriza lo que hace quien llega primero a una escena con equipos electrónicos.

El marco legal está en la [nota 11](11-fundamentos-legales-investigacion.md) y la cadena de custodia en la [nota 12](12-etica-cadena-custodia.md).

## De dónde salen las buenas prácticas

En febrero de 1998, los directores de los laboratorios criminalísticos federales de Estados Unidos impulsaron la creación del Scientific Working Group on Digital Evidence (SWGDE), con el encargo de estandarizar la recuperación, preservación y análisis de evidencia digital a partir de lo que proponía la International Organization on Computer Evidence (IOCE). Los estándares abarcaban también audio, imagen y dispositivos electrónicos.

El documento conjunto de SWGDE e IOCE sobre principios y estándares de la evidencia digital se presentó en la conferencia internacional sobre delitos de alta tecnología celebrada en Londres del 4 al 7 de octubre de 1999. Proponía estándares internacionales para intercambiar evidencia digital entre países, y las agencias estadounidenses de investigación y procuración de justicia los adoptaron.

Sobre esa base, el National Institute of Justice (NIJ) publicó en abril de 2004 la guía *Forensic Examination of Digital Evidence: A Guide for Law Enforcement*, que fija los lineamientos para los laboratorios de Estados Unidos y que sirve de referencia a muchas otras agencias. La mayor parte de esta nota sigue su estructura.

Actualización al 2026: SWGDE sigue activo y publica sus documentos en su sitio; la guía del NIJ de 2004 no ha tenido una edición posterior y se consulta como referencia clásica. Para el trabajo actual conviene complementarla con la ISO/IEC 27037:2012 y con las guías vigentes de SWGDE.

## Conceptos básicos

| Concepto | Qué es, en palabras propias |
|---|---|
| Adquisición de evidencia digital | Momento en que los datos o los dispositivos se recolectan y se guardan para analizarlos. Supone un procedimiento legal según el lugar; un conjunto de datos solo es "evidencia" cuando el órgano judicial lo reconoce como tal |
| Objetos de datos | Información con posible valor probatorio ligada a un dispositivo físico; puede presentarse en varios formatos mientras no se altere el contenido original |
| Evidencia digital | Información con valor probatorio que se guarda o se transmite en forma binaria |
| Dispositivos físicos | Medios donde la información se almacena o por donde se transfiere |
| Evidencia digital original | Los dispositivos y los datos asociados tal como estaban al momento de adquirirlos |
| Evidencia digital duplicada | Reproducción digital exacta de todos los datos del dispositivo original |
| Copia | Reproducción exacta de la información, independiente del soporte físico original |

El duplicado (imagen forense "bit a bit") reproduce todo el medio, incluidos espacio libre y fragmentos borrados; la copia lógica solo trae los archivos visibles. Se analiza sobre duplicados, y su exactitud se prueba con un hash calculado antes y después.

## Criterios de calidad para la agencia

Para que la evidencia se obtenga, conserve, examine y transporte de forma confiable, la guía pide a cada agencia un sistema de calidad basado en procedimientos operativos estándar (SOP, por sus siglas en inglés). Sus criterios, resumidos:

1. Toda la operación con evidencia digital se rige por SOP escritos y aceptados, que cubren protocolos, equipo y materiales.
2. Cada agencia que obtiene o examina evidencia digital usa esos SOP, y sus políticas sobre evidencia digital están detalladas en ellos, publicadas bajo la supervisión de su autoridad.
3. Los SOP se revisan y actualizan cada año, porque la tecnología cambia más rápido que los procedimientos.
4. Todo procedimiento se sustenta en el método científico.
5. La agencia conserva un archivo histórico de los procedimientos técnicos usados en cada caso.
6. El hardware y el software empleados son los adecuados y probados para el análisis concreto.
7. Toda actividad de obtención, manejo, análisis y transporte se registra y queda disponible para revisión y testimonio. La confiabilidad descansa en la cadena de custodia de cada artículo. Las notas son permanentes: se escriben con tinta (el lápiz solo para diagramas), los errores se tachan y se rubrican sin borrar nada, y cada nota se identifica con firma, iniciales, firma digital u otro identificador.
8. Cualquier acción que pueda alterar, dañar o destruir la evidencia original la ejecuta solo personal calificado.

La conclusión de la guía es sencilla: la evidencia vale solo si se puede demostrar que es exacta, confiable y controlada, y eso depende de personal capacitado, equipo calibrado, software adecuado y procedimientos que se mantienen al día.

## El examen forense en cuatro pasos

```mermaid
flowchart LR
    A[1. Valoración del problema] --> B[2. Adquisición bit a bit]
    B --> C[3. Análisis]
    C --> D[4. Documentación y dictamen]
```

| Paso | Nombre en la guía | En qué consiste |
|---|---|---|
| 1 | *Assessment* | Valorar qué se busca y decidir qué acciones seguirá la investigación |
| 2 | *Acquisition* | Obtener una reproducción exacta del medio original para preservar su integridad |
| 3 | *Examination* | Extraer, analizar e interpretar los datos del medio duplicado |
| 4 | *Documenting and reporting* | Respaldar documentalmente el análisis y emitir la opinión técnica (dictamen pericial) |

### Antes del primer caso: políticas y procedimientos

La guía pide primero tener políticas documentadas, supervisadas y asimiladas por el área responsable. Para crear un procedimiento propone cinco pasos: aislar el problema, generar posibles soluciones con lluvia de ideas, probar cada una en condiciones controladas, evaluar los resultados y elegir el procedimiento que funcionó.

### Paso 1. Valoración del problema

Empieza con la solicitud de intervención pericial: identificar a la autoridad que la firma, comprobar que reúne lo necesario para atenderla y documentar el estado de los indicios que llegan (es decir, revisar la cadena de custodia).

Después se establece comunicación constante con el responsable del caso, en México el agente del Ministerio Público de la Federación, para precisar:

1. Lo que contiene el expediente y qué otros estudios hay (huellas, ADN, otros dictámenes).
2. Si conviene pedir información a un proveedor de servicios de Internet.
3. El contexto de los hechos; en un fraude, por ejemplo, qué medios se usaron: banca en línea, cajeros, telefonía.
4. Qué registros pedir a otras instancias, como bancos o proveedores.
5. Qué tan hábil técnicamente parece el autor del hecho.
6. En qué orden analizar los indicios.
7. Si hacen falta más especialistas o equipo.

Si hay que ir al lugar de los hechos, la guía remite al manual para primeros respondientes (se resume más abajo) y pide observar en un caso de TIC: cuántas computadoras hay, cómo está armada la red, quiénes administran los sistemas (para entrevistarlos), qué capacidad de procesamiento y almacenamiento existe, si hay respaldos o almacenamiento en otros sitios y qué software propio usa la organización.

Con eso se ubica el punto donde se originó el hecho para aislarlo. Lo recomendable es obtener los indicios y analizarlos en el laboratorio, un ambiente controlado, considerando el tiempo que llevará la recolección, la logística y el personal, el impacto económico de la intervención para quien la sufre, qué dispositivos y medios masivos se van a estudiar y los alcances y límites legales.

### Paso 2. La imagen forense

Las recomendaciones para obtener la copia bit a bit de un medio de almacenamiento son:

1. Asegurar la evidencia conforme a los SOP de la agencia.
2. Comprobar que el equipo del examinador funciona bien.
3. Abrir el gabinete de la computadora para ver y tocar los componentes.
4. Identificar todos los dispositivos que se van a duplicar.
5. Documentar los dispositivos internos y la configuración del hardware.
6. Extraer los dispositivos elegidos.
7. Obtener la configuración del sistema sospechoso, en particular lo que registra el BIOS.

El punto 7 tiene una razón práctica: el BIOS (hoy, casi siempre UEFI) guarda la fecha y la hora del equipo. Si el reloj del sistema estaba adelantado o atrasado, todas las marcas de tiempo de los archivos lo estarán también, y el perito necesita conocer esa diferencia para reconstruir la línea de tiempo.

Hay casos en que separar el disco no conviene o no se puede: un arreglo RAID, cuyos discos solo tienen sentido juntos, o una laptop cuyo disco no es accesible. Entonces se usa el propio equipo para la copia, con estas precauciones:

1. Confirmar que el disco destino está limpio en sentido forense (sin datos previos).
2. Con un bloqueador de escritura físico, conectarlo y arrancar desde un medio externo (CD o USB) para no tocar el sistema original.
3. Con un bloqueador por software, arrancar desde el medio externo y activar de inmediato el modo de solo lectura.
4. Revisar que la capacidad del disco coincida con sus particiones, para no pasar por alto una partición oculta.
5. Registrar el número de serie y todo dato que identifique al dispositivo.
6. Duplicar el sistema sospechoso hacia el medio del investigador.
7. Verificar que original y duplicado coinciden.

La verificación del punto 7 se hace hoy comparando el hash del original y el del duplicado. Si son iguales, la imagen es exacta; ese valor se anota en la cadena de custodia y en el dictamen.

### Paso 3. Análisis

| Fase | Qué se hace |
|---|---|
| Preparación | Disponer de directorios de trabajo separados donde guardar lo que se extrae |
| Extracción física | Recuperar todo el contenido del disco, sin importar sistema de archivos ni particiones |
| Extracción lógica | Trabajar con el sistema de archivos: archivos activos, borrados y el espacio sobrante al final de cada bloque (*file slack*) |
| Análisis de lo extraído | Interpretar los datos recuperados: metadatos de los archivos y bitácoras de actividad del sistema |
| Conclusiones | Documentar todos los resultados anteriores |

Un dato que explica por qué la recuperación es posible: el sistema de archivos es el componente que guarda y recupera los datos en el disco, y cuando un archivo se "borra" normalmente solo marca su espacio como disponible. El contenido sigue ahí hasta que otro archivo lo sobrescribe, y por eso un análisis físico puede recuperarlo.

### Paso 4. Documentación y dictamen

Durante el trabajo, la guía recomienda:

1. Tomar notas al consultar con el responsable de la investigación.
2. Guardar copia de esas notas.
3. Conservar copia de la solicitud de intervención.
4. Conservar la documentación de la cadena de custodia.
5. Anotar lo suficiente para que otro pueda repetir lo hecho.
6. Incluir fechas, horas y detalle de cada acción.
7. Documentar cualquier irregularidad en la ejecución de órdenes de cateo.
8. Registrar topologías de red, accesos, perfiles y contraseñas encontrados.
9. Registrar sistemas operativos y versiones de las aplicaciones relevantes.
10. Documentar accesos externos a la información y respaldos fuera del lugar.

Y el dictamen debería contener, como mínimo:

| Elemento | Ejemplo |
|---|---|
| Agencia que investiga | Servicios periciales de la fiscalía |
| Número de caso | Carpeta de investigación |
| Identificación del perito | Nombre y cargo |
| Identificación del solicitante | Agente del Ministerio Público |
| Fecha de recepción y fecha de emisión | Para medir tiempos y custodia |
| Lista descriptiva de lo estudiado | Equipos, medios y sus números de serie |
| Firma del perito | Autógrafa o digital |
| Detalle de los procedimientos | Fotografías, valores hash, estadísticas, herramientas y versiones |
| Conclusiones | Claras, sin opinión legal |

El dictamen pericial tiene su propia nota en la unidad 5 ([nota 16](16-dictamen-pericial.md)).

## Qué es la informática forense

Algunas definiciones de referencia:

| Fuente | Idea central |
|---|---|
| Federal Bureau of Investigation (FBI) | Ciencia de adquirir, preservar, obtener y presentar datos procesados electrónicamente y guardados en un medio electromagnético, óptico o electrónico |
| H. B. Wolfe | Serie metódica de técnicas y procedimientos para obtener evidencia de equipos y medios digitales que pueda presentarse ante un tribunal de forma coherente y comprensible |
| R. McKemmish | Identificar, preservar, analizar y presentar evidencia digital de manera legalmente aceptable |
| D. Farmer y W. Venema | Obtener y analizar datos de forma íntegra para reconstruir lo que ocurrió en un sistema en el pasado |

Según el FBI, la disciplina persigue tres fines: compensar los daños que causan los delincuentes, perseguirlos y procesarlos judicialmente, y crear medidas para prevenir esos delitos. Al perito que auxilia al Ministerio Público le toca el segundo, y en él lo decisivo es recolectar, manejar y analizar bien la evidencia.

Como esas definiciones se centran en computadoras y almacenamiento, Lira Arteaga (2018) propone una definición propia desde la criminalística: la investigación forense aplicada a las TIC es la rama de la criminalística que busca, trata, analiza y preserva indicios de una investigación en la que equipos de cómputo o de telecomunicaciones se usaron como fin o como medio para una conducta presuntamente delictiva. Su objetivo es auxiliar a la autoridad para descubrir la verdad histórica de los hechos e identificar a sus autores.

## Clasificación de los delitos cometidos con TIC

La mayoría de estas conductas existían antes que la tecnología; lo que cambió es el instrumento. Se agrupan en dos tipos:

| Las TIC como medio o instrumento | Las TIC como objetivo o fin |
|---|---|
| Falsificar documentos con sistemas de cómputo (tarjetas, cheques) | Ataques de negación de servicio (sabotaje) |
| Fraudes financieros | Destrucción de programas |
| Difusión de pornografía infantil por Internet o telefonía | Destrucción o robo de componentes de un sistema de cómputo o de telecomunicaciones |
| Espionaje por redes informáticas o de telecomunicaciones | Sabotaje a centros de cómputo |
| Robo de identidad | Robo de medios de almacenamiento |
| Alteración de bases de datos a través de una red | |

## Tipos de intervención pericial

Un equipo de investigación en TIC debe reunir especialistas en computación y, sobre todo, ingenieros en comunicaciones y electrónica, porque las solicitudes se reparten en tres variantes:

| Informática | Telecomunicaciones | Electrónica |
|---|---|---|
| Acceso o uso no autorizado de equipos | Identificación de dispositivos y equipos | Identificación y funcionamiento de equipo electrónico |
| Robo, alteración o copia de información | Recuperación de información almacenada | Principio de operación de circuitos |
| Falsificación de documentos con equipo de cómputo | Intervención de líneas telefónicas | Análisis de diagramas esquemáticos |
| Fraude financiero por red | Ataques o daños a una red de comunicación | Alteración de cajeros automáticos |
| Ataques a servidores y a redes | Robo de señal (televisión por cable) | |
| Robo de programas | Uso indebido de frecuencias | |
| Identificación de correos electrónicos y rastreo de servidores | | |
| Recuperación de información en medios de almacenamiento y en Internet | | |
| Análisis de licitaciones o contratos de sistemas | | |
| Clonación de bandas magnéticas o chips de tarjetas | | |

## La volatilidad de la evidencia digital

Comparada con un documento en papel o un objeto físico, la evidencia digital es frágil: puede alterarse, intervenirse o perderse con facilidad. Y al revés, los datos que se procesan o transmiten se copian sin rastro evidente, lo que abre la puerta al robo de secretos industriales, de bases de datos o de software, al espionaje telefónico y al robo de identidad. Por eso el perito tiene la responsabilidad de aplicar métodos consistentes, fundados en la criminalística y alineados con la ley del país donde investiga, con un fin que nunca cambia: localizar y proteger la evidencia.

## Principios para recolectar y manejar evidencia

Adaptados a la investigación ministerial mexicana, los principios fundamentales son cinco:

1. Al recolectar indicios no se altera la posible evidencia bajo ninguna circunstancia, salvo autorización previa del agente del Ministerio Público, que debe dar fe de cada procedimiento que realice el personal pericial.
2. Solo un profesional forense acreditado accede a los indicios digitales originales.
3. Toda actividad de recolección, acceso, almacenamiento y transferencia se documenta, se conserva y queda disponible para revisión futura.
4. Mientras la evidencia esté a su cargo, una persona responde por todo lo que se haga con ella.
5. Toda agencia que recolecta evidencia digital cumple estos principios.

## En la escena: la guía para primeros respondientes

El NIJ publicó en 2008 la segunda edición de *Electronic Crime Scene Investigation: A Guide for First Responders* y, en noviembre de 2009, una versión de bolsillo para consultar en el lugar. Su secuencia se resume en cuatro tareas: identificar, preservar, recolectar y asegurar.

### 1. Identificar

Además de computadoras de escritorio, portátiles y servidores, hay que reconocer como posibles fuentes de evidencia los medios de almacenamiento (discos internos y externos, memorias USB, tarjetas de memoria, incluso disfrazadas de objetos cotidianos), los dispositivos de mano (teléfonos, cámaras, receptores GPS, reproductores), los periféricos (módems, enrutadores, impresoras, escáneres) y las redes. La evidencia está en el aparato, en la función que cumple y en lo que guarda: documentos, fotos, correo, historial de navegación, conversaciones, bitácoras, números marcados, documentos escaneados o impresos recientemente, y datos de red como direcciones IP y MAC. Un equipo también puede tener huellas dactilares o ADN que hay que conservar.

### 2. Asegurar y documentar el lugar

- No cambiar el estado de ningún equipo: si está apagado, se queda apagado.
- Retirar del área a toda persona no autorizada.
- Buscar señales de que un equipo está encendido (ventiladores, luces, discos girando) y de que hay borrado en curso o acceso remoto en la pantalla.
- Separar a los adultos presentes y preguntarles para qué sirve cada equipo, quién lo usa, qué proveedor de Internet tienen, usuarios, cuentas, contraseñas, redes sociales y si hay programas de autodestrucción o cifrado.
- Documentar tipo, ubicación, posición, estado y energía de cada equipo, lo que muestra cada pantalla, cada conexión física e inalámbrica, con fotografías, video, notas y croquis.

Esa documentación es la **fijación** del lugar, que en criminalística combina cuatro técnicas: la descripción escrita, la fotografía (de lo general a lo particular: vistas generales, medianos acercamientos, acercamientos y grandes acercamientos), el croquis y la planimetría con medidas. En una escena digital se agrega la fotografía de cada pantalla y de cada puerto conectado.

### 3. Recolectar

Quien no tiene formación en evidencia digital no explora el contenido, no oprime teclas ni mueve el ratón (salvo el leve movimiento que indica la guía para despertar la pantalla), no mueve un equipo encendido y no acepta ayuda de personas no autorizadas; pide apoyo especializado. Antes de asegurar cualquier cosa, confirma que tiene la autoridad legal para hacerlo.

| Situación | Qué indica la guía |
|---|---|
| Equipo apagado | Fotografiar y dibujar las conexiones, etiquetar cada cable, desconectar la alimentación desde el equipo (en laptops, también la batería), cubrir con cinta ranuras y botón de encendido, anotar marca, modelo y serie, y embalar |
| Equipo encendido, caso general | Retirar la alimentación suele ser lo más seguro; en un entorno típico de escritorio conserva datos del último usuario y de los documentos recientes |
| Pantalla muestra borrado o sobrescritura en curso | Cortar la energía de inmediato |
| Pantalla muestra evidencia, chats, cifrado, almacenamiento remoto o actividad ilícita evidente | No apagar; pedir personal con experiencia en captura de datos volátiles |
| Teléfonos móviles | Dejarlos en el estado de energía en que se encontraron |
| Servidores, mainframes y redes | No apagarlos sin especialista: se pierden datos y puede haber responsabilidad civil |

### 4. Embalar, transportar y asegurar

- Todo lo recolectado queda documentado, etiquetado, fotografiado e inventariado, incluidas las conexiones, para poder rearmar el sistema.
- Se usan empaques antiestáticos; el plástico común genera estática y humedad.
- Se protege contra golpes, dobleces y rayones, y se recogen cargadores y adaptadores.
- Los teléfonos van en material que bloquea la radiofrecuencia (bolsa o caja de Faraday), para que no reciban señales ni órdenes de borrado remoto.
- En el traslado se evitan campos magnéticos (radios, bocinas), asientos calefactados, alfombras que generan estática y largas estancias en un vehículo con calor, frío o humedad.
- El transporte se documenta para no romper la cadena de custodia.

La guía cierra con listas de evidencia típica por delito (de la explotación infantil al terrorismo) y con lo que conviene entregar al laboratorio: autorización para examinar, resumen del caso, contacto, palabras clave, contraseñas conocidas, reportes preliminares y datos del sospechoso.

## Errores comunes

- **Encender un equipo apagado "para ver qué tiene".** Cambia fechas de acceso y puede activar borrados.
- **Apagar un servidor como si fuera una laptop.** Se pierde la memoria y se puede dañar el servicio de un tercero.
- **Analizar sobre el original.** El análisis se hace sobre el duplicado verificado con hash.
- **No anotar la hora del BIOS ni aislar el teléfono.** Sin lo primero no se corrige la línea de tiempo; sin lo segundo llega un borrado remoto.

## Glosario

| Término | Definición |
|---------|-----------|
| SWGDE | Grupo científico de trabajo sobre evidencia digital, creado en 1998 en Estados Unidos para estandarizar procedimientos |
| IOCE | Organización internacional sobre evidencia de computadoras que propuso los primeros principios de intercambio |
| SOP | Procedimientos operativos estándar: protocolos escritos que rigen el trabajo con evidencia |
| Imagen forense | Duplicado exacto, bit a bit, de todo un medio de almacenamiento |
| Copia lógica | Reproducción de los archivos visibles, sin espacio libre ni fragmentos borrados |
| Valor hash | Resumen numérico de los datos que cambia si cambia un solo bit; demuestra que un duplicado es exacto |
| Bloqueador de escritura | Dispositivo o programa que permite leer un medio sin modificarlo |
| File slack | Espacio sobrante al final de un bloque de disco, que puede guardar restos de datos anteriores |
| Metadatos | Datos sobre un archivo: fechas, autor, ubicación, programa que lo creó |
| BIOS o UEFI | Firmware del equipo que, entre otras cosas, lleva la fecha y la hora del sistema |
| Primer respondiente | Persona que llega primero a la escena y debe preservarla |
| Fijación | Registro del lugar y de los indicios por descripción escrita, fotografía, croquis y planimetría |
| Bolsa de Faraday | Empaque que bloquea señales de radio para aislar un dispositivo de la red |
| Datos volátiles | Información que se pierde al apagar el equipo, como la memoria RAM |

## Referencias

- Farmer, D. y Venema, W. (2005). *Forensic discovery*. Addison-Wesley. http://www.porcupine.org/forensics/forensic-discovery/
- International Organization for Standardization e International Electrotechnical Commission. (2012). *Guidelines for identification, collection, acquisition and preservation of digital evidence* (ISO/IEC 27037:2012). https://www.iso.org/standard/44381.html
- Lira Arteaga, O. M. (2018). *Cibercriminalidad: fundamentos de investigación en México*. Ubijus. Edición anterior: Instituto Nacional de Ciencias Penales, 2010, ISBN 978-607-7882-13-8, https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/ (incluye las definiciones del FBI, Wolfe y McKemmish que se citan aquí).
- Mission Darkness. (2017, 11 de septiembre). *Mission Darkness Dry Shield 40L Backpack* [Video]. YouTube. https://www.youtube.com/@MissionDarkness
- Mission Darkness. (2018, 2 de octubre). *Mission Darkness BlockBox Touch and Touch USB overview* [Video]. YouTube. https://www.youtube.com/@MissionDarkness
- National Institute of Justice. (2004). *Forensic examination of digital evidence: A guide for law enforcement* (NCJ 199408). U.S. Department of Justice. https://www.ojp.gov/pdffiles1/nij/199408.pdf
- National Institute of Justice. (2008). *Electronic crime scene investigation: A guide for first responders* (2.ª ed.; NCJ 219941). U.S. Department of Justice. https://www.ojp.gov/pdffiles1/nij/219941.pdf
- National Institute of Justice. (2009). *Electronic crime scene investigation: An on-the-scene reference for first responders* (NCJ 227050). U.S. Department of Justice. https://www.ojp.gov/pdffiles1/nij/227050.pdf
- Scientific Working Group on Digital Evidence. (s. f.). *SWGDE*. https://www.swgde.org/

---

*Rogelio Arriaga Gonzalez*
