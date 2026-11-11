<!-- _class: lead -->

# U5 - Metodología, procedimientos y herramientas de la informática forense

### Informática forense

**Rogelio Arriaga - Raptor Dev Services**

<!--
Última unidad del curso, 5 horas. Hilo conductor: un caso que empieza con una solicitud de intervención
y termina con un dictamen firmado. Primero el procedimiento general y el equipo encendido, luego cada
escenario particular (apagado, servidor, web, correo, teléfono, fraude) y al final el documento que
llega al juez. La tarea es un pódcast sobre el equipo encendido: anúncienla desde el inicio.
-->

---

## El lema de la unidad

> "El éxito de la investigación en informática forense no depende de las herramientas informáticas utilizadas; sino de la metodología y el compromiso del investigador." Oscar Manuel Lira Arteaga

Las herramientas cambian cada año. El método, y la disciplina para seguirlo cuando hay prisa, es lo que sobrevive a un contrainterrogatorio.

<!--
Pregunten al grupo: ¿quién ha visto un caso perdido por la herramienta y quién uno perdido por el
procedimiento? Casi siempre es lo segundo.
-->

---

## Objetivo de la unidad

Al terminar, puedes:

- Reconocer los procedimientos de investigación propios de la informática forense.
- Identificar, fijar y preservar evidencia digital en los escenarios más comunes.
- Redactar un dictamen pericial que la autoridad entienda y que resista la audiencia.

---

## Agenda

| Bloque | Tema |
|---|---|
| 1 | Procedimientos de investigación: del oficio a la diligencia |
| 2 | El equipo encendido, paso a paso |
| 3 | Identificación, fijación y preservación: el árbol de decisiones |
| 4 | Casos particulares: web, correo, teléfonos y fraudes |
| 5 | El dictamen pericial |
| 6 | Cierre del curso, práctica y tarea |

---

## Dónde estamos

- En la U4 vimos el fundamento legal de la investigación, la ética y la cadena de custodia.
- Hoy los ponemos en movimiento: qué hace el perito, en qué orden y con qué cuidado.
- Al final de la sesión deben poder seguir un caso completo, desde que llega el oficio hasta que se firma el dictamen.

---

<!-- _class: lead -->

# Parte 1
## Procedimientos de investigación

---

## Seis actividades que se repiten

| Actividad | Idea clave |
|---|---|
| Preservar | Que nada cambie mientras se recolecta |
| Adquirir | Copias exactas: disco y memoria |
| Analizar | Sobre la copia, nunca sobre el original |
| Documentar | Que otro pueda repetirlo |
| Informar | Conclusiones que la autoridad entienda |
| Testificar | Sostener cada decisión en audiencia |

Lectura: [nota 14](../../../12-seguridad/03-forense/14-procedimientos-investigacion-forense.md).

---

## Campo y laboratorio

1. **Campo**: el lugar de los hechos, con prisa y con la autoridad presente.
2. **Laboratorio**: ambiente controlado, con tiempo y herramientas completas.

El flujo: solicitud, procesamiento del lugar (identificar, fijar, embalar), procesamiento de la evidencia (preservar, procesar), resultados (analizar, concluir) e integración a la investigación. La cadena de custodia acompaña todo el recorrido.

<!--
Dibujen el flujo en el pizarrón y marquen con otro color la cadena de custodia corriendo por debajo de
todas las etapas: no es un paso al final, es un hilo continuo.
-->

---

## Cinco etapas en el lugar de los hechos

| Etapa | Qué se logra |
|---|---|
| Antes de la diligencia | Entender el problema y preparar personas y equipo |
| Fijación del lugar | Constancia de cómo estaba todo |
| Búsqueda de indicios y conjeturas | Localizar equipos y formular hipótesis |
| Embalaje | Proteger, sellar y etiquetar |
| Cadena de custodia | Registrar cada cambio de manos |

---

## La solicitud de intervención

Lo mínimo que debe traer el oficio:

- Autoridad que la emite, lugar, fecha y hora.
- Número de oficio y de expediente (hoy, carpeta de investigación).
- Especialidad requerida: informática, electrónica o telecomunicaciones.
- **Planteamiento del problema**: qué se debe investigar.
- Teléfono, firma y, si los hay, los elementos que se envían a estudio.

El artículo 127 del CNPP pone la conducción en el Ministerio Público; el perito comparte, en lo ético, la responsabilidad del resultado.

---

## Quién va y qué se pregunta

- Al menos **dos peritos en TIC** donde la red es extensa: uno para la infraestructura, otro para los indicios.
- Un **fotógrafo forense** y un **criminalista**, junto con el Ministerio Público.

Antes de tocar nada se registran diez datos: fecha y hora, domicilio, responsable de los sistemas, recursos de TIC, accesos físicos, infraestructura de comunicaciones, control de accesos y perfiles, rango de IP, bitácoras activas y proveedores de comunicaciones.

---

## Búsqueda de indicios y conjeturas

1. Fijar el lugar (criminalista, fotógrafo y perito en TIC).
2. Pedir a quien opere los equipos que retire las manos del teclado.
3. Localizar los equipos y anotar si están encendidos o apagados.
4. Lo apagado se embala pieza por pieza.
5. Con autorización, recoger teléfonos y medios de almacenamiento.

Ocho escenarios para generar hipótesis: acceso no autorizado, código malicioso, correo electrónico, intervención de comunicaciones, denegación de servicio, error humano, sabotaje e identificación de dispositivos.

---

## El equipo encendido: por qué la RAM primero

- La memoria guarda procesos, conexiones, sesiones y contraseñas que tal vez nunca llegaron al disco.
- Si el equipo se apaga, eso se pierde para siempre.
- Por eso se fotografía la pantalla en lugar de capturarla: la captura escribe en la memoria.
- Opera el personal del lugar, bajo la dirección del perito, y todo ante la autoridad.

<!--
Momento de práctica rápida: pidan a dos voluntarios que actúen la escena. Uno es el empleado sentado,
otro el perito. ¿Qué es lo primero que dice el perito? Respuesta: que retire las manos y que nadie
toque nada; lo siguiente es anotar la hora.
-->

---

## Los ocho pasos con el equipo encendido

1. Fecha y hora del sistema, y su desfase con la hora real.
2. Memoria RAM a un medio externo limpio, con su hash.
3. Procesos, archivos recientes y documentos abiertos.
4. Previsualizar las unidades.
5. Segunda muestra de la RAM, si se puede.
6. Imagen forense de las unidades, si corresponde.
7. Hora de fin de la obtención de datos volátiles.
8. Conexiones y dispositivos externos.

---

## Cerrar la intervención

1. Desconectar **desde el cable de alimentación**, no desde el sistema operativo.
2. Documentar la desconexión de cada cable.
3. Embalar, etiquetar y fotografiar el embalaje.
4. Llenar la cadena de custodia.

Si el equipo no se puede llevar, la imagen hecha en el lugar, con la autoridad dando fe, se trata como el original.

---

<!-- _class: lead -->

# Parte 2
## Identificación, fijación y preservación

---

## Inspección y cateo

| | Inspección | Cateo |
|---|---|---|
| Fundamento | Art. 267 CNPP | Art. 282 CNPP |
| Requiere juez | No | Sí, orden previa |
| Para qué sirve al perito | Identificar equipos y discos para probar después que no los cambiaron | Asegurar equipos en un domicilio o propiedad privada |

Lectura: [nota 15](../../../12-seguridad/03-forense/15-identificacion-fijacion-preservacion.md).

---

## El árbol de decisiones

- ¿Está encendido? Si se puede, primero lo volátil.
- ¿Se puede apagar? Si no, imagen en vivo.
- ¿Se puede incautar? Desconectar la alimentación, embalar y llevar.
- ¿Se puede extraer el disco? Bloqueador de escritura, imagen y hash.
- ¿No se puede extraer? Arranque forense desde el BIOS con un medio externo.
- Si no hay contraseña ni autorización: documentar motivos y responsable de la negativa.

<!--
La nota 15 tiene el diagrama completo. Recórranlo con un caso: laptop apagada, en préstamo de la empresa,
que el dueño no deja llevar y cuyo disco está soldado. ¿Qué rama sigue?
-->

---

## Dos excepciones que no se olvidan

- **Un servidor encendido no se apaga.** Detenerlo tumba un servicio y la copia se hace en vivo.
- **Un equipo encendido con cifrado no se apaga.** Al apagarlo, los datos quedan cifrados sin clave.

En ambos casos el riesgo de apagar es mayor que el de trabajar sobre un sistema en ejecución.

---

## Embalar y custodiar

- Cada pieza identificada, etiquetada, sellada y firmada por perito, Ministerio Público y policía.
- Fotografía del embalaje antes de salir hacia el laboratorio.
- Etiquetas foliadas y bolsas antiestáticas para los discos.
- Formato de cadena de custodia **por duplicado**: quien recibe firma la copia del perito.

---

## Copia lógica contra imagen forense

| | Copia lógica | Imagen bit a bit |
|---|---|---|
| Qué copia | Carpetas y archivos visibles | Todo el medio, incluido el espacio no asignado |
| Archivos borrados | No | Sí |
| Fechas de archivo | Pueden cambiar | Se conservan |
| Uso forense | No | Sí |

Hash: MD5 son 32 caracteres hexadecimales, SHA-256 son 64. Hoy se prefiere SHA-256. El formato E01 agrega CRC por bloque y un hash del contenido completo.

---

## Páginas web: pasivo y activo

| Pasivo (sin tocar el sitio) | Activo (con contacto) |
|---|---|
| Whois y registros de dominio | Capturas de pantalla |
| Versiones archivadas (Wayback Machine) | Descarga completa con Wget, con autorización |
| Búsqueda avanzada y por código fuente | Traza de ruta con `tracert` o `traceroute` |

Actualización: Google retiró en 2024 el operador `cache:` que mencionaba el material.

---

## Correo electrónico: cuatro saltos

1. Las **cabeceras** del mensaje.
2. El proveedor de correo de origen.
3. La bitácora de conexión del proveedor de Internet.
4. La máquina de origen.

El dato que abre todas las puertas: **la fecha y hora exactas** del envío. Entrar a una cuenta ajena exige orden judicial (artículo 16 constitucional).

---

## Teléfonos celulares

| Etapa | Clave |
|---|---|
| Aislamiento | Papel aluminio en cuatro capas y caja de Faraday: sin red no hay borrado remoto |
| Fijación | Marca, modelo, serie y estado; guantes; no quitar la batería |
| Identificación | Manual técnico del modelo |
| Extracción | Herramienta especializada o manual; siempre con orden judicial (art. 291 CNPP) |
| Embalaje y dictamen | Etiquetar y plasmar todo en el dictamen |

---

## Fraudes financieros

- Cada operación deja IP de origen y destino, fecha, hora, cuentas y medio.
- Dos preguntas: por qué medio se hizo y desde qué equipo.
- Los bancos guardan bitácoras de banca electrónica al menos 180 días (CUB, art. 316 Bis 15), y el usuario puede pedirlas.
- Los concesionarios de telecomunicaciones conservan datos 24 meses (art. 183 de la ley de 2025).

<!--
Actualización frente al material original: antes no había obligación legal de conservar datos de
conexión y los ISP los guardaban menos de tres meses. Hoy sí la hay.
-->

---

<!-- _class: lead -->

# Parte 3
## El dictamen pericial

---

## Qué es y qué dice la ley

- Informe del perito que aclara lo técnico para que la autoridad decida.
- Claro, preciso y objetivo; con conclusiones contundentes.
- El dictamen escrito **no exime** al perito de declarar en juicio (art. 272 CNPP).
- En el interrogatorio le pueden plantear hipótesis sobre su dictamen (art. 372).

Lectura: [nota 16](../../../12-seguridad/03-forense/16-dictamen-pericial.md).

---

## Los rubros

1. Fecha de emisión.
2. Solicitante.
3. Planteamiento del problema, transcrito tal cual.
4. Observación del lugar.
5. Análisis y consideraciones técnicas, con fotografías y pantallas.
6. Bibliografía.
7. Glosario.
8. Conclusiones en lenguaje llano.
9. Firma de los peritos.

---

## Un dictamen real, en lecciones

| Lo que hizo el perito | Lo que protegió |
|---|---|
| Fotografió el paquete cerrado | Que nadie diga que llegó abierto |
| Trabajó sobre un duplicado del disco | El original intacto |
| Informó el atraso de 1 h 44 min del reloj del DVR | La interpretación de los horarios |
| Usó bloqueador de escritura | Que el análisis no tocara la copia |
| Entregó un hash por cada video | Que cualquiera pueda verificarlos |

---

## Tres documentos, tres lectores

| Documento | Para quién |
|---|---|
| Informe técnico | Otros especialistas: técnicas, herramientas y secuencias |
| Informe ejecutivo | Directivos: qué pasó y cómo evitarlo, sin jerga |
| Dictamen | La autoridad: método, medios y conclusiones |

Si un hecho admite dos explicaciones, el dictamen considera las dos y trata de descartar cada una con método.

---

## Práctica de la unidad

**Foro: la cadena de custodia digital en un caso real.**

- Expliquen los procesos metodológicos para tratar la evidencia digital.
- Argumenten por qué la cadena de custodia decide si la evidencia sirve en un proceso administrativo, mercantil o penal.
- Comenten el trabajo de tres compañeros, con postura y fundamento.

Instrucciones completas en la práctica de la unidad.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones de la unidad

- Una investigación forense en TIC arranca con un planteamiento claro, sigue con el procesamiento del lugar y el análisis de la evidencia, y termina con conclusiones fundadas.
- Una computadora se preserva con su imagen forense; un teléfono, cortándole la comunicación.
- Las páginas web se rastrean por métodos pasivos y activos, y las cabeceras de un correo dicen cuándo y desde dónde se generó.
- El dictamen es el producto técnico y científico de todo el trabajo, y sus conclusiones deben dar a la autoridad lo que necesita para decidir con justicia.

---

## Al terminar puedes

Seguir un caso de informática forense de la solicitud al dictamen.

- Preparar una diligencia y saber qué preguntar antes de tocar un equipo.
- Preservar un equipo encendido en el orden correcto.
- Decidir qué hacer con un equipo apagado, un servidor, un disco cifrado o un teléfono.
- Rastrear un sitio web o un correo sin violar la ley.
- Escribir un dictamen con sus rubros y defenderlo en audiencia.

---

## Tarea

**Un pódcast sobre el análisis de un equipo de cómputo encendido.**

- Explicar cada etapa del procedimiento, de la llegada al lugar a la cadena de custodia.
- Cerrar con por qué garantizar la prueba digital decide un proceso judicial.
- Entregar el audio y su guion.

Instrucciones y rúbrica en la tarea de la unidad.

---

## Lecturas

- [`14-procedimientos-investigacion-forense.md`](../../../12-seguridad/03-forense/14-procedimientos-investigacion-forense.md): el procedimiento y el equipo encendido.
- [`15-identificacion-fijacion-preservacion.md`](../../../12-seguridad/03-forense/15-identificacion-fijacion-preservacion.md): el árbol de decisiones y los casos particulares.
- [`16-dictamen-pericial.md`](../../../12-seguridad/03-forense/16-dictamen-pericial.md): el dictamen y su defensa.
- Lira Arteaga, *Cibercriminalidad: fundamentos de investigación en México*, cap. V, pp. 540-554.
- Para ver las herramientas en acción: el video de DFIRScience sobre Autopsy 4 y el de extracción de un iPhone con Cellebrite UFED (en las referencias de las notas).

---

## Cierre del curso

> Si la evidencia no se puede verificar, no es evidencia: es una opinión.

- Cinco unidades: criminalística, cibercriminalidad, marco legal, tratamiento de la evidencia y método.
- Todas convergen en el mismo documento: un dictamen que otro perito puede repetir y un juez puede entender.
- Sigan practicando con casos propios: cada caso cerrado es una oportunidad para afinar el método.

<!--
Cierre del curso completo. Pidan a cada participante una sola frase: qué haría distinto mañana en su
trabajo después de estas cinco unidades.
-->
