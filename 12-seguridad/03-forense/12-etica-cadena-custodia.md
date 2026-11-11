# 12 - Principios éticos y cadena de custodia

> Lectura del curso [Informática forense](../../cursos/digital-forensics/README.md), unidad 4, tema 2.

## Por qué importa

La técnica y la ley son necesarias, pero no alcanzan. Un perito puede dominar las herramientas y conocer el Código Nacional de Procedimientos Penales (CNPP) y aun así emitir un dictamen sesgado, opinar de lo que no sabe o callar un error para no quedar mal con quien lo pidió. El sistema de justicia confía en su palabra precisamente porque el juez no tiene forma de repetir el análisis: si la ética falla, todo lo demás falla con ella.

La cadena de custodia es la otra mitad de esa confianza. Mientras la ética garantiza que el perito diga la verdad sobre lo que analizó, la cadena de custodia garantiza que lo que analizó es lo mismo que se encontró en el lugar de los hechos. En evidencia digital, donde un archivo se copia o se modifica sin dejar rastro visible, ese registro es lo único que permite afirmar que el indicio llegó intacto.

Esta nota cubre los valores del perito, el decálogo que resume sus deberes, el concepto legal de cadena de custodia, los instrumentos normativos que la regulan y un modelo de formato pensado para tecnologías de la información. El procedimiento detallado de identificación, fijación y preservación, con la guía técnica de 2018 para evidencia digital, está en la [nota 15](15-identificacion-fijacion-preservacion.md).

## El perfil ético del perito

Antes de evaluar conocimientos, una institución que forma peritos debería mirar el carácter de quien aspira al puesto. Hay cualidades que ningún curso enseña porque se forman a lo largo de la vida, empezando en casa, y que la preparación profesional solo complementa:

- **Honestidad:** reportar lo que se encontró, incluido lo que no conviene a quien pidió el peritaje.
- **Profesionalismo:** aplicar el método completo aunque haya prisa, y documentarlo.
- **Ecuanimidad:** tratar igual la hipótesis de la acusación que la de la defensa.
- **Actitud de servicio:** entender que el dictamen sirve a la justicia, no al lucimiento personal.

En informática forense se suma un valor que en otras especialidades pesa menos: el **respeto a la privacidad**. Un disco duro o un teléfono contiene la vida entera de una persona, incluidas cosas que nada tienen que ver con el caso. El perito ve fotos familiares, conversaciones íntimas y datos de salud; su obligación es buscar lo que la autoridad pidió, no curiosear, y guardar secreto de lo demás.

## El decálogo del perito

El criminalista mexicano Rafael Moreno González resumió los deberes del perito en diez puntos, conocidos como el decálogo del perito. Se presentan aquí con lo que cada uno significa en la práctica de la informática forense.

| N.º | Deber | Cómo se ve en un caso de TIC |
|---|---|---|
| 1 | Reconocer los límites de la propia capacidad científica | Si nunca ha analizado un vehículo con sistema de infoentretenimiento, lo dice y pide apoyo de un colega o se capacita antes de dictaminar. Es preferible admitir el hueco que firmar una conclusión sin sustento |
| 2 | Ser metódico, claro y preciso al dictaminar | Las conclusiones se redactan para que un juez sin formación técnica las entienda sin ambigüedad; los tecnicismos van al cuerpo del dictamen, no a la conclusión |
| 3 | Mantener al día sus conocimientos | Sistemas operativos, cifrado, nube y dispositivos móviles cambian cada año; una técnica de hace cinco años puede ya no funcionar o producir resultados engañosos |
| 4 | Colaborar con la autoridad en el esclarecimiento de la verdad | Mantener comunicación con el Ministerio Público para entender qué se busca, sin dejar que eso incline el resultado |
| 5 | Opinar de lo técnico, nunca de lo legal | El perito dice "el archivo se creó el día X desde la cuenta Y"; no dice "el imputado cometió fraude". Tipificar el delito es tarea del Ministerio Público |
| 6 | Actuar con imparcialidad, acuciosidad, dedicación y prudencia | Un dictamen objetivo puede llevar a la cárcel al culpable y también evitar que un inocente termine preso por un prejuicio |
| 7 | Aplicar los métodos y técnicas de la investigación científica | El procedimiento se describe de modo que otro perito pueda repetirlo y llegar al mismo resultado |
| 8 | Fundar las conclusiones en hechos verificados | Cada afirmación del dictamen se apoya en una observación o en un experimento documentado, y las conclusiones no se contradicen entre sí |
| 9 | Escuchar con apertura las objeciones a su método | Si la defensa demuestra un error, el perito lo reconoce; nadie es infalible y la crítica fundada mejora el trabajo |
| 10 | Excusarse solo por razones técnicas, legales o éticas | Sin elementos suficientes, el perito se abstiene de concluir en lugar de complacer a alguien; y no se excusa por comodidad |

El punto 5 merece un subrayado porque es el error más frecuente de los peritos noveles en TIC. Encontrar un correo con la contraseña de una cuenta bancaria no autoriza a escribir que hubo robo de identidad. El dictamen describe lo que hay, cómo se encontró y qué significa técnicamente; la calificación jurídica la hace la autoridad.

## La cadena de custodia

### El concepto

La expresión viene del inglés *chain of custody*. Nació como un documento que las policías de muchos países usan para seguir con método el recorrido de cada indicio, desde que se levanta en el lugar de los hechos hasta que se presenta ante un juez, de modo que se pueda demostrar que no se alteró ni se contaminó. En ese documento queda cada persona que lo manipuló, lo trasladó o lo estudió.

En México, la definición legal está en el CNPP.

Código Nacional de Procedimientos Penales, artículo 227 (texto vigente, última reforma DOF 28-11-2025):

> Artículo 227. Cadena de custodia
>
> La cadena de custodia es el sistema de control y registro que se aplica al indicio, evidencia, objeto, instrumento o producto del hecho delictivo, desde su localización, descubrimiento o aportación, en el lugar de los hechos o del hallazgo, hasta que la autoridad competente ordene su conclusión.
>
> Con el fin de corroborar los elementos materiales probatorios y la evidencia física, la cadena de custodia se aplicará teniendo en cuenta los siguientes factores: identidad, estado original, condiciones de recolección, preservación, empaque y traslado; lugares y fechas de permanencia y los cambios que en cada custodia se hayan realizado; igualmente se registrará el nombre y la identificación de todas las personas que hayan estado en contacto con esos elementos.

Los factores del segundo párrafo funcionan como una lista de comprobación. Para un indicio digital se traducen así:

| Factor del artículo 227 | Qué se registra para un equipo o un archivo |
|---|---|
| Identidad | Marca, modelo, número de serie, IMEI, capacidad, y el valor hash de la imagen forense |
| Estado original | Encendido o apagado, pantalla visible, conexiones, daños, sellos de fábrica |
| Condiciones de recolección | Quién, cuándo, con qué herramienta y con qué bloqueador de escritura |
| Preservación | Bolsa antiestática o aislante de radiofrecuencia, temperatura, humedad |
| Empaque y traslado | Embalaje, sello, etiqueta, vehículo y ruta |
| Lugares y fechas de permanencia | Bodega de indicios, laboratorio, sala de audiencias |
| Cambios en cada custodia | Apertura del embalaje para análisis, nuevo sellado, copias generadas |
| Personas en contacto | Nombre, cargo, institución y firma de cada eslabón |

Código Nacional de Procedimientos Penales, artículo 228, primer párrafo (texto vigente):

> La aplicación de la cadena de custodia es responsabilidad de quienes en cumplimiento de las funciones propias de su encargo o actividad, en los términos de ley, tengan contacto con los indicios, vestigios, evidencias, objetos, instrumentos o productos del hecho delictivo.

El segundo párrafo del artículo 228 aclara que un indicio alterado durante la custodia conserva su valor probatorio, salvo que la autoridad compruebe que la modificación le quitó eficacia, y que debe concatenarse con otros medios de prueba; todo ello sin perjuicio de la responsabilidad del servidor público que no siguió el procedimiento. La lectura correcta para el perito es prudente: es el juez quien decide si una prueba vale, pero una cadena incompleta es justo el pretexto que la defensa necesita para sembrar duda razonable sobre la integridad de la evidencia digital, por cuidadoso que haya sido el análisis.

### Objetivos de la cadena de custodia

Los instrumentos nacionales coinciden en que la cadena de custodia busca cuatro cosas:

1. **Mismidad:** que el indicio que se presenta en juicio sea el mismo que se encontró.
2. **Autenticidad:** que se pueda demostrar su origen.
3. **Continuidad:** que no haya periodos sin responsable conocido.
4. **Trazabilidad:** que cada movimiento quede registrado y se pueda reconstruir.

## Los instrumentos que la regulan

| Instrumento | Quién lo emite y cuándo | Qué aporta |
|---|---|---|
| Artículos 227 y 228 del CNPP | Congreso de la Unión; CNPP publicado en 2014, vigente en todo el país desde 2016 | Definición legal, factores y responsables |
| Acuerdo A/009/15 | Procuraduría General de la República; publicado en el DOF el 12 de febrero de 2015 | Directrices obligatorias para los servidores públicos federales que intervienen en la cadena de custodia |
| Guía Nacional de Cadena de Custodia | Aprobada en 2015 por las conferencias nacionales de procuración de justicia y de secretarios de seguridad pública | Homologa la actuación de policías, ministerios públicos, peritos y fuerzas armadas en todo el país |
| ISO/IEC 27037:2012 | Organización Internacional de Normalización y Comisión Electrotécnica Internacional | Directrices para identificar, recolectar, adquirir y preservar evidencia digital; útil sobre todo fuera del ámbito ministerial |
| NMX-I-289-NYCE-2016 | Norma mexicana voluntaria; publicada en el DOF el 19 de septiembre de 2016 | Metodología de análisis forense de datos y guías de ejecución |
| Guía técnica de cadena de custodia de evidencia digital | Unidad de investigaciones cibernéticas de la Agencia de Investigación Criminal, 2018 | Procedimiento específico para indicios digitales (ver [nota 15](15-identificacion-fijacion-preservacion.md)) |

### El Acuerdo A/009/15

El acuerdo fija las directrices para los servidores públicos que intervienen en la cadena de custodia, cubre la preservación del lugar, el procesamiento, el traslado, el análisis, el almacenamiento y el destino final de los indicios, y abrogó los acuerdos anteriores A/002/10 y A/078/12. Trae cinco anexos que conviene conocer porque dan nombre a los documentos del procedimiento:

1. Guía de Cadena de Custodia.
2. Formato de entrega y recepción del lugar de intervención.
3. Registro de Cadena de Custodia.
4. Formato de entrega y recepción de indicios o elementos materiales probatorios.
5. Formato de etiqueta para embalaje.

Actualización al 2026: el acuerdo lo emitió la entonces Procuraduría General de la República, hoy Fiscalía General de la República. No se localizó un acuerdo posterior que lo abrogue de forma expresa; la literatura institucional reciente lo sigue citando como referencia, junto con la Guía Nacional de Cadena de Custodia.

### La Guía Nacional de Cadena de Custodia

La guía nacional se emitió porque el CNPP exige criterios homologados para el personal que actúa en el sistema acusatorio. Su objetivo general es asegurar la mismidad y autenticidad de los indicios con registros que demuestren continuidad y trazabilidad, para que puedan entrar al proceso como medio de prueba. Divide el procedimiento en cinco etapas:

```mermaid
flowchart LR
    A[Procesamiento en el lugar de intervención] --> B[Traslado]
    B --> C[Análisis en laboratorio]
    C --> D[Almacenamiento en la bodega de indicios]
    D --> E[Presentación en juicio]
```

En cada etapa la guía define sus límites, los responsables, las actividades mínimas, los controles de verificación y lo que debe quedar registrado.

### Vocabulario del procedimiento

Los instrumentos nacionales comparten un vocabulario que el perito usa en sus registros. Los términos más frecuentes, explicados con palabras propias:

| Término | Qué significa |
|---|---|
| Lugar de intervención | Sitio del hecho, o aquel donde se localizan o aportan indicios relacionados |
| Acordonamiento | Delimitar ese sitio con cinta, cuerda u otra barrera para que nadie lo altere |
| Equipo de protección personal | Guantes, cubrebocas, overol y demás, que protegen al interviniente y evitan contaminar los indicios |
| Indicio | Huella, vestigio o señal que puede o no relacionarse con el hecho |
| Elemento material probatorio | Objeto o producto relacionado con el delito que puede llegar a ser prueba |
| Identificación | Asignar a cada indicio un número, una letra o una combinación única desde que se encuentra |
| Documentación | Dejar constancia fiel de cómo estaban lugares, personas e indicios |
| Recolección | Levantar el indicio con una técnica que conserve su integridad |
| Embalaje | Material que envuelve y protege el indicio, permite identificarlo y deja ver si alguien lo abrió sin permiso |
| Sellado | Cerrar el embalaje con cinta o calor de modo que cualquier apertura deje marca |
| Etiqueta | Rótulo del embalaje con los datos que identifican el indicio |
| Bodega de indicios | Espacio con condiciones controladas donde se resguardan los indicios |
| Registro de cadena de custodia | Documento que sigue al indicio y a todas las personas que lo tienen a su cargo |

### Normas para el ámbito privado

En el sector privado (auditorías internas, respuesta a incidentes, litigios civiles o laborales), el Acuerdo A/009/15 no obliga, pero sirve de modelo. El sustento metodológico más aceptado ahí es la norma ISO/IEC 27037, guía para identificar, recolectar, adquirir y preservar evidencia digital, junto con la norma mexicana NMX-I-289-NYCE-2016.

Actualización al 2026: alguna literatura afirma que la ISO/IEC 27037 se revisó por última vez en 2018. La edición vigente sigue siendo la de 2012: se ha confirmado en revisiones periódicas, Europa la adoptó sin cambios como EN ISO/IEC 27037:2016 y Estados Unidos la reafirmó en 2019 y 2024. La NMX-I-289-NYCE-2016, que canceló a la versión de 2013, aparece como vigente en el Catálogo Mexicano de Normas.

## Un formato de cadena de custodia para TIC

Aunque la cadena de custodia es obligatoria, en la práctica muchas veces se llena mal o con formatos pensados para indicios biológicos o balísticos, que no piden lo que distingue a un equipo de cómputo. El siguiente modelo, basado en el que propone Lira Arteaga (2018), se divide en tres apartados.

### Apartado 1. Datos generales

Identifica a la autoridad que pide la intervención y a quien embala. Se llena una sola vez, al identificar el indicio, y no se modifica durante toda la vida del proceso.

| Campo | Contenido esperado |
|---|---|
| Fecha y hora | Momento del aseguramiento |
| Expediente | Número de carpeta de investigación |
| Autoridad solicitante | Nombre, unidad o fiscalía y teléfono |
| Responsable del embalaje | Nombre, unidad o coordinación y teléfono |
| Domicilio del aseguramiento | Dirección completa del lugar de intervención |
| Lugar del aseguramiento y observaciones | Punto exacto dentro del inmueble y condiciones relevantes |

### Apartado 2. Datos particulares del indicio

Describe el objeto con detalle suficiente para reconocerlo sin abrir el embalaje. También es fijo: no se vuelve a llenar.

| Campo | Contenido esperado |
|---|---|
| Tipo de indicio | Computadora de escritorio, laptop, teléfono, memoria, disco externo |
| Marca, modelo y número de serie | Tal como aparecen en la etiqueta del fabricante |
| Inventario | Número patrimonial, si el equipo pertenece a una organización |
| Color y marcas | Señas particulares: calcomanías, golpes, grabados |
| Observaciones | Estado de encendido, contraseñas o bloqueos visibles, accesorios |

Si se trata de un equipo de cómputo, se agregan las características y el número de serie de cada dispositivo de almacenamiento que contiene, porque el disco es lo que se va a analizar y puede separarse del gabinete.

### Apartado 3. Personal responsable (eslabones)

Es la parte que crece: cada vez que el indicio cambia de manos, para transporte, análisis o resguardo, se agrega un bloque.

| Campo | Contenido esperado |
|---|---|
| Nombre y firma de quien recibe | Persona que asume la custodia |
| Unidad o coordinación y teléfono | Para localizarla después |
| Fecha y hora | Del momento exacto de la entrega |
| Observaciones | Estado del sello, motivo de la entrega, cambios hechos |

### Llevar el registro en un sistema

Con la conectividad disponible hoy, el mismo autor sugiere capturar estos datos en un sistema de base de datos accesible desde computadoras o tabletas, para que los mandos sigan en tiempo real dónde está cada indicio y quién lo tiene. Un sistema así debería, además, guardar la bitácora de cambios de forma que no se pueda editar en silencio, registrar el valor hash de cada imagen forense y permitir firmar cada entrega. La idea no sustituye al papel que exige el procedimiento; lo complementa y facilita la supervisión.

## Errores comunes

- **Opinar de lo legal en el dictamen.** "El imputado cometió fraude" no es una conclusión pericial.
- **Llenar dos veces los datos fijos.** Los apartados 1 y 2 se escriben una vez; si cambian, hay un problema que explicar.
- **Dejar huecos de tiempo.** Una noche en la cajuela de una patrulla sin registro es un eslabón roto.
- **Describir el equipo y olvidar el disco.** El número de serie del gabinete no identifica el medio que se analizó.
- **Usar el formato de otra especialidad.** Un formato pensado para indicios biológicos no pide IMEI, estado de energía ni hash.
- **Callar un error propio.** El decálogo obliga a reconocerlo; ocultarlo es peor para el caso que admitirlo.

## Glosario

| Término | Definición |
|---------|-----------|
| Decálogo del perito | Diez deberes éticos del perito propuestos por Rafael Moreno González |
| Imparcialidad | Analizar sin favorecer a la acusación ni a la defensa |
| Cadena de custodia | Sistema de control y registro que acompaña al indicio desde que se localiza hasta que la autoridad ordena concluirla |
| Eslabón | Cada persona que recibe el indicio y queda registrada como su responsable |
| Mismidad | Cualidad de que el indicio presentado sea el mismo que se encontró |
| Trazabilidad | Posibilidad de reconstruir cada movimiento del indicio a partir de los registros |
| Continuidad | Ausencia de periodos sin responsable conocido en la custodia |
| Acuerdo A/009/15 | Directrices de la entonces PGR para los servidores públicos que intervienen en la cadena de custodia |
| Guía Nacional de Cadena de Custodia | Documento de 2015 que homologa el procedimiento en todo el país |
| ISO/IEC 27037 | Norma internacional con directrices para identificar, recolectar, adquirir y preservar evidencia digital |
| NMX-I-289-NYCE-2016 | Norma mexicana sobre metodología de análisis forense de datos |
| Embalaje | Material que protege e identifica al indicio y revela aperturas no autorizadas |
| Bodega de indicios | Lugar de resguardo con condiciones controladas |
| Registro de cadena de custodia | Documento que acompaña al indicio con todos sus eslabones |

## Referencias

- Cámara de Diputados del H. Congreso de la Unión. (2025). *Código Nacional de Procedimientos Penales* [Última reforma DOF 28-11-2025]. Diario Oficial de la Federación. https://www.diputados.gob.mx/LeyesBiblio/pdf/CNPP.pdf
- Conferencia Nacional de Procuración de Justicia y Conferencia Nacional de Secretarios de Seguridad Pública. (2015). *Guía Nacional de Cadena de Custodia* (versión 1.0). https://www.policiabancaria.cdmx.gob.mx/storage/app/media/DOCS%20PDF%202022/PROTOCOLOS%20DE%20ACTUACION%20POLICIAL/31_GuaNacionalCadenadeustodia.pdf
- International Organization for Standardization e International Electrotechnical Commission. (2012). *Information technology. Security techniques. Guidelines for identification, collection, acquisition and preservation of digital evidence* (ISO/IEC 27037:2012). https://www.iso.org/standard/44381.html
- Lira Arteaga, O. M. (2018). *Cibercriminalidad: fundamentos de investigación en México*. Ubijus. Edición anterior: Instituto Nacional de Ciencias Penales, 2010, ISBN 978-607-7882-13-8, https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/
- Moreno González, R. (s. f.). *Decálogo del perito*. Citado en Lira Arteaga, O. M. (2018). *Cibercriminalidad: fundamentos de investigación en México*. Ubijus. Edición anterior: Instituto Nacional de Ciencias Penales, 2010, ISBN 978-607-7882-13-8, https://www.marcialpons.es/libros/cibercriminalidad/9786077882138/
- Procuraduría General de la República. (2015, 12 de febrero). Acuerdo A/009/15 por el que se establecen las directrices que deberán observar los servidores públicos que intervengan en materia de cadena de custodia. *Diario Oficial de la Federación*. https://dof.gob.mx/nota_detalle.php?codigo=5381699&fecha=12/02/2015
- Secretaría de Economía. (2016). *NMX-I-289-NYCE-2016. Tecnologías de la información. Metodología de análisis forense de datos y guías de ejecución*. Catálogo Mexicano de Normas. https://platiica.economia.gob.mx/normalizacion/?p=44952
- Unidad de Investigaciones Cibernéticas y Operaciones Tecnológicas. (2018). *Guía técnica de cadena de custodia de evidencia digital*. Agencia de Investigación Criminal, Procuraduría General de la República. https://www.gob.mx/fgr (documento institucional sin dirección pública estable; se cita la sede de la institución sucesora; su desarrollo está en la [nota 15](15-identificacion-fijacion-preservacion.md)).

---

*Rogelio Arriaga Gonzalez*
