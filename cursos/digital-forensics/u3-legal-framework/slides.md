<!-- _class: lead -->

# U3 - Marco normativo para la persecución de delincuentes informáticos

### Informática forense

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 5 horas. Hilo conductor: un caso de ransomware contra una empresa mexicana cuyo servidor
de mando está en otro país. Cada bloque responde una pregunta del caso: qué dice el modelo internacional,
qué delito es en México y con qué leyes se consiguen los datos. Se cierra con la tarea (infografía
del Convenio) y la discusión del foro sobre México y el Convenio.
-->

---

## El lema de la unidad

> "El problema real no es si las máquinas piensan, sino si lo hacen los hombres." B. F. Skinner

La técnica avanza más rápido que la ley. El investigador que no conoce la ley comete errores que ninguna herramienta corrige.

---

## Objetivo de la unidad

Al terminar, puedes:

- Identificar el marco legal internacional sobre ciberdelitos y su modelo de referencia, el Convenio de Budapest.
- Ubicar en el Código Penal Federal el tipo penal que corresponde a una conducta cometida con TIC.
- Identificar las leyes complementarias que dan a la autoridad herramientas para conseguir evidencia.

<!--
Aclarar desde el inicio: no se forma abogados. Se forma peritos que saben qué pedir, qué no tocar
y cómo nombrar lo que encuentran.
-->

---

## Agenda

| Bloque | Tema |
|---|---|
| 1 | El Convenio de Budapest y la cooperación internacional |
| 2 | Conservación de datos: conexión no es contenido |
| 3 | El Código Penal Federal: de la técnica al tipo penal |
| 4 | Acceso ilícito y los demás delitos del CPF |
| 5 | Telecomunicaciones: la colaboración con la justicia |
| 6 | Comercio electrónico, derecho de autor y banca |
| 7 | Cierre, foro y tarea |

---

## El caso que nos acompaña

Una empresa de Guadalajara amanece con sus servidores cifrados y una nota que pide un pago en criptomonedas.

- El correo de entrada vino de un proveedor extranjero.
- El servidor de mando está en otro continente.
- El pago se pide a una cartera digital.

**Preguntas de hoy:** ¿qué delito es?, ¿qué datos existen?, ¿quién los tiene?, ¿cuánto tiempo duran?

---

<!-- _class: lead -->

# Parte 1
## El Convenio de Budapest

---

## Un tratado modelo

- Convenio sobre la Ciberdelincuencia, Budapest, 23 de noviembre de 2001.
- Impulsado por el Consejo de Europa (fundado en 1949, distinto de la Unión Europea).
- Abierto a Estados no europeos: Canadá, Japón, Estados Unidos, Argentina, Panamá, entre otros.
- Protocolo adicional de 2003: racismo y xenofobia en línea.
- Segundo protocolo adicional de 2022: cooperación reforzada y pruebas electrónicas.

Lectura: [nota 08](../../../12-seguridad/03-forense/08-convenio-de-budapest.md).

---

## Qué hace el Convenio

| Pieza | Para qué |
|---|---|
| Definiciones | Hablar el mismo idioma entre países |
| Derecho penal sustantivo | Lista mínima de conductas que cada Parte debe castigar |
| Derecho procesal | Conservar, pedir, registrar e interceptar datos |
| Jurisdicción | Quién puede perseguir el delito |
| Cooperación internacional | Extradición, asistencia mutua y red 24/7 |

---

## Las cuatro definiciones

- **Sistema informático:** dispositivo o conjunto que trata datos de forma automatizada al ejecutar un programa.
- **Datos informáticos:** cualquier representación de hechos o información apta para tratamiento informático, incluidos los programas.
- **Proveedor de servicios:** quien permite comunicarse por un sistema, y quien procesa o almacena datos para ese servicio.
- **Datos sobre el tráfico:** origen, destino, ruta, hora, fecha, tamaño, duración y tipo de servicio de una comunicación.

<!--
Subrayar que los datos de tráfico no incluyen el contenido. Toda la discusión de conservación de datos
gira alrededor de esa diferencia.
-->

---

## Los delitos del Convenio

| Título | Delitos (artículos) |
|---|---|
| Confidencialidad, integridad y disponibilidad | Acceso ilícito (2), interceptación ilícita (3), interferencia en datos (4) y en el sistema (5), abuso de dispositivos (6) |
| Delitos informáticos | Falsificación informática (7), fraude informático (8) |
| Contenido | Pornografía infantil (9) |
| Propiedad intelectual | Infracciones a derechos de autor y afines (10) |
| Otras formas de responsabilidad | Tentativa y complicidad (11), personas jurídicas (12), sanciones (13) |

---

## Una salvedad para quien hace seguridad

El artículo 6 no castiga producir o usar herramientas cuando el fin son **pruebas autorizadas o la protección de un sistema**.

La misma herramienta es auditoría o delito según la autorización y el propósito. Por eso un pentest sin autorización escrita no es un pentest.

---

## Las herramientas procesales

| Medida | Artículo |
|---|---|
| Conservación rápida de datos almacenados (hasta 90 días, renovable) | 16 |
| Conservación y revelación parcial de datos de tráfico | 17 |
| Orden de presentación | 18 |
| Registro y confiscación | 19 |
| Obtención en tiempo real de datos de tráfico | 20 |
| Interceptación de contenido | 21 |

Todas sujetas a salvaguardas: control judicial, motivación y proporcionalidad (art. 15).

---

## Cooperación y red 24/7

- Por la vía diplomática tradicional, pedir datos de conexión a otro país podía tardar hasta seis meses.
- El Convenio permite pedir a otra Parte que **congele** los datos mientras llega la solicitud formal (arts. 29 y 30).
- Cada Parte mantiene un punto de contacto disponible las 24 horas, los 7 días (art. 35).

<!--
Volver al caso: con el servidor de mando en otro país, sin cooperación rápida los registros se pierden
antes de que llegue el oficio.
-->

---

## ¿Y México?

- Hasta noviembre de 2026 no hay en el DOF decreto de aprobación ni promulgatorio del Convenio.
- Algunas fuentes secundarias ya lo listan como Parte: hay que verificarlo en el depositario antes de afirmarlo.
- La Convención de la ONU contra la Ciberdelincuencia (adoptada en diciembre de 2024, firma en Hanói en octubre de 2025) tampoco tenía firma de México en la Colección de Tratados.

<!--
Es el tema del foro. Pedir que no den por buena ninguna cifra sin fuente: la vigencia de un tratado
se verifica en el DOF y con el depositario.
-->

---

<!-- _class: lead -->

# Parte 2
## Conexión no es contenido

---

## Dos tipos de información

| Conexión | Contenido |
|---|---|
| IP, fecha, hora, duración, servicio | Lo que se dijo o se envió |
| Permite ubicar el equipo del atacante | Es confidencial |
| Lo que la ley debería obligar a conservar | Solo con autorización de juez federal |

Mezclarlos produce debates interminables sobre privacidad que frenan la conservación de lo que sí sirve para rastrear.

---

## La conservación en México

- El CNPP (art. 303) permite pedir la **conservación inmediata hasta por 90 días**, pero solo cuando la autoridad ya conoce el hecho.
- La denuncia suele llegar días o meses después.
- Según la experiencia del autor del material, había proveedores que guardaban la conexión unas horas y otros hasta seis meses.

Resultado: muchas órdenes de conservación ya no encuentran nada.

---

## El modelo europeo y su caída

- Directiva 2006/24/CE: obligó a conservar datos de tráfico y localización, nunca contenido, de seis meses a dos años.
- Definió qué conservar por pregunta: origen, destino, fecha y duración, tipo de servicio, equipo y ubicación.
- En 2014 el Tribunal de Justicia de la UE la declaró **inválida** (*Digital Rights Ireland*) por desproporcionada.

Hoy sirve como mapa técnico de qué dato responde qué pregunta, no como norma vigente.

---

## ¿Qué es internet para la ley?

La ley mexicana de telecomunicaciones la define como un conjunto descentralizado de redes que funcionan como una red lógica única.

La crítica del material: técnicamente, el acceso a internet es un servicio más de la **red pública de telecomunicaciones**. Mientras la ley no lo entienda así, regulará la telefonía con detalle y casi nada de internet.

---

<!-- _class: lead -->

# Parte 3
## El Código Penal Federal

---

## La técnica no es el delito

| Término técnico | Tipo penal |
|---|---|
| Phishing, pharming | Fraude (CPF 386) |
| Ransomware | Extorsión (ley general de 2025) |
| Denegación de servicio | Sabotaje (CPF 140) |
| Intrusión en un sistema protegido | Acceso ilícito (CPF 211 bis 1 a 7) |
| Intervenir comunicaciones | CPF 177 |
| "Ciberbullying" | Amenazas, hostigamiento, violación a la intimidad sexual |

Lectura: [nota 09](../../../12-seguridad/03-forense/09-codigo-penal-federal.md).

<!--
Pregunta al grupo con el caso: ¿qué delito es el ransomware de la empresa? Extorsión. Y si además
copiaron la base de clientes, también acceso ilícito: acumulación de penas.
-->

---

## Acceso ilícito: la tabla que hay que saberse

| Sistema | Sin autorización: modificar o destruir | Sin autorización: conocer o copiar |
|---|---|---|
| Cualquiera protegido (211 bis 1) | 6 meses a 2 años | 3 meses a 1 año |
| Del Estado (211 bis 2) | 1 a 4 años | 6 meses a 2 años |
| Sistema financiero (211 bis 4) | 6 meses a 4 años | 3 meses a 2 años |

Si el autor **sí** tenía acceso, aplican el 211 bis 3 (Estado) y el 211 bis 5 (sistema financiero). Todo se agrava hasta la mitad si la información se usa en provecho propio o ajeno (211 bis 7).

---

## El requisito que el perito debe probar

Para que exista acceso ilícito, el sistema debe estar **protegido por algún mecanismo de seguridad**.

- Documentar qué control existía: contraseña, cifrado, autenticación.
- Documentar cómo se evadió o se abusó de él.
- Sin esa evidencia, la conducta puede no encajar en el tipo.

---

## Otros delitos del CPF que se cometen con TIC

- Contra la seguridad de la Nación: traición, espionaje, sedición, motín, rebelión, terrorismo, sabotaje, conspiración.
- Vías de comunicación y correspondencia: interferir comunicaciones (167 VI), decodificar señales (168 bis).
- Libre desarrollo de la personalidad: corrupción y pornografía de menores (200 a 202 bis), turismo sexual, lenocinio.
- Falsedad: moneda, títulos, sellos y documentos (234 a 245).
- Contra las personas: hostigamiento, abuso sexual, amenazas, lesiones psicológicas.
- Patrimoniales: robo, abuso de confianza, fraude, recursos de procedencia ilícita; derechos de autor (424 bis).

---

## Lo que cambió desde el material

- **Extorsión:** el artículo 390 del CPF se derogó (DOF 28-11-2025); ahora es una ley general con pena base de 15 a 25 años, agravada si se usan medios de telecomunicación.
- **Violación a la intimidad sexual:** CPF 199 Octies a Decies (2021), para la difusión de contenido íntimo sin consentimiento.
- **Acceso ilícito:** párrafos de 2009 y 2016 para sistemas de seguridad pública y pena doble si se obstruye la justicia.
- **Hostigamiento sexual:** multa de hasta 800 días (2018).

---

## Cuidado: el investigador también puede delinquir

- Abrir un correo o leer mensajes sin orden puede ser violación de correspondencia o intervención de comunicaciones.
- La Constitución (art. 16) reserva al juez federal la autorización para intervenir comunicaciones privadas.
- Cumplir una instrucción ministerial no autoriza lo que solo un juez puede autorizar.

---

<!-- _class: lead -->

# Parte 4
## Leyes complementarias

---

## De la LFTR a la LMTR

- La Ley Federal de Telecomunicaciones y Radiodifusión fue **abrogada** por la Ley en Materia de Telecomunicaciones y Radiodifusión (DOF 16-07-2025).
- El IFT se extinguió; el regulador es la Comisión Reguladora de Telecomunicaciones.
- El capítulo de colaboración con la justicia pasó casi igual: arts. 189 y 190 ahora son 182 y 183.

Lectura: [nota 10](../../../12-seguridad/03-forense/10-leyes-complementarias.md).

---

## Lo que deben conservar los concesionarios

| Dato | Ejemplo |
|---|---|
| Suscriptor | Nombre o razón social y domicilio |
| Tipo de comunicación | Voz, buzón, datos, mensajes, multimedia |
| Origen y destino | Número de destino, plan o prepago |
| Fecha, hora y duración | De cada comunicación |
| Primera activación | Fecha, hora y celda |
| Equipo | Identificadores de fabricación del equipo y del suscriptor |
| Ubicación | Posicionamiento geográfico de la línea |

---

## Los plazos que hay que recordar

- 12 meses en sistemas de consulta y entrega **en tiempo real**.
- 12 meses más en almacenamiento electrónico: entrega en **48 horas**.
- Entrega ordinaria a la autoridad: máximo **24 horas** desde la notificación.
- Un área responsable disponible **24 horas, 365 días**.
- Nuevo en la LMTR: líneas móviles ligadas a una identificación con CURP.

<!--
Pregunta de repaso: ¿cuánto tiempo hay registros de telefonía? 24 meses. ¿Y de IP? No hay una
obligación equivalente: ese es el hueco que señala el material.
-->

---

## Código de Comercio: el mensaje de datos

- Principios: neutralidad tecnológica, autonomía de la voluntad, compatibilidad internacional y equivalencia funcional.
- Un mensaje de datos tiene validez y sirve como prueba (art. 89 bis).
- Reglas para saber quién lo envió, cuándo se recibió, si hubo acuse y si cumple con la forma escrita (arts. 90 a 95).

---

## La firma electrónica avanzada

Cumple cuatro requisitos (art. 97), que en lenguaje forense son tres propiedades:

| Propiedad | Requisito |
|---|---|
| Identidad | Los datos de creación corresponden solo al firmante |
| Control exclusivo | El firmante los controlaba al firmar |
| Integridad | Se detecta cualquier alteración posterior de la firma y del mensaje |

---

## Derecho de autor y banca

- **LFDA:** el programa de cómputo se protege como obra literaria (desde 2026, incluidos los de inteligencia artificial); el código malicioso no tiene protección; la autoridad puede acceder a bases de datos con información personal para investigar (art. 109).
- **Ley de Instituciones de Crédito:** clonación de tarjetas y skimming (112 Bis), acceso ilícito al sistema bancario (112 Quáter), disposición indebida de recursos de clientes (113 Bis, 5 a 15 años).
- **Secreto bancario (art. 142):** la información se pide por la vía correcta, fundada y motivada.

---

## De vuelta al caso

| Pregunta | Respuesta |
|---|---|
| ¿Qué delito? | Extorsión (ley general) y, si copiaron datos, acceso ilícito (CPF 211 bis 1) |
| ¿Qué datos existen? | Conexión en el proveedor de correo, registros del servidor, movimientos de la cartera |
| ¿Quién los tiene? | Proveedores extranjeros y nacionales, la propia empresa |
| ¿Cuánto duran? | Lo que decida cada proveedor: pedir conservación de inmediato (CNPP 303) |

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones de la unidad

- El Convenio de Budapest es el modelo legislativo de referencia para perseguir ciberdelitos más allá de las fronteras; su valor está en las definiciones comunes y la cooperación.
- En el Código Penal Federal, los ciberdelitos son acciones u omisiones sancionadas que usan las TIC: el ransomware se persigue como extorsión, el phishing como fraude y la denegación de servicio como sabotaje.
- Las leyes complementarias completan el marco: la de telecomunicaciones fija el rastreo de la telefonía (pero no el de IP), la LFDA protege el software y el Código de Comercio da validez a los mensajes de datos y a la firma electrónica.

---

## Foro y tarea

**Foro:** ¿por qué México no se ha incorporado al Convenio de Budapest, y qué consecuencias tiene? Postura propia, con fuentes, y réplica a tres compañeros.

**Tarea:** infografía con la clasificación de los ciberdelitos del Convenio y cada uno de sus tipos penales, con su equivalente en la ley mexicana.

Instrucciones completas en la práctica y la tarea de la unidad.

---

## Lecturas

- [Nota 08 - Legislación internacional: el Convenio de Budapest](../../../12-seguridad/03-forense/08-convenio-de-budapest.md)
- [Nota 09 - El Código Penal Federal y los ciberdelitos](../../../12-seguridad/03-forense/09-codigo-penal-federal.md)
- [Nota 10 - Leyes complementarias contra los ciberdelitos](../../../12-seguridad/03-forense/10-leyes-complementarias.md)

---

## Siguiente unidad: fundamentos del tratamiento de evidencia digital

- Los fundamentos legales de la investigación en el CNPP.
- Principios éticos y cadena de custodia.
- La metodología para obtener y tratar la evidencia digital.

Traigan claro qué puede pedir la autoridad y qué no puede tocar el perito sin orden: es la base de la cadena de custodia.
