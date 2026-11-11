<!-- _class: lead -->

# U4 - Fundamentos del tratamiento de evidencia digital

### Informática forense

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 2 horas de clase más práctica y tarea. Hilo conductor: un teléfono y una laptop asegurados
en un caso de fraude. En cada parte preguntamos qué puede salir mal: sin orden judicial (parte 1),
sin cadena de custodia o con un perito parcial (parte 2), o con un procedimiento que altera el original (parte 3).
-->

---

## El lema de hoy

> No podemos culpar a la tecnología cuando comete errores.
> (Tim Berners-Lee)

La herramienta no responde ante el juez. Responde el perito: por el permiso con que actuó, por el registro que dejó y por el método que siguió.

---

## Objetivo de la unidad

Al terminar, quien participa puede:

- Explicar los fundamentos legales que rigen una investigación con evidencia digital en México.
- Identificar los principios éticos del perito y el funcionamiento de la cadena de custodia.
- Aplicar las buenas prácticas internacionales para obtener y tratar evidencia digital.

---

## Agenda

| Bloque | Tema |
|---|---|
| 1 | Por qué la evidencia digital es frágil |
| 2 | Fundamentos legales de la investigación |
| 3 | Principios éticos y cadena de custodia |
| 4 | Metodología para obtener y tratar evidencia digital |
| 5 | En la escena: el primer respondiente |
| 6 | Cierre, práctica y tarea |

Lecturas: notas 11, 12 y 13 de `12-seguridad/03-forense`.

---

## El problema: indicios que se evaporan

Los datos en medios magnéticos, ópticos o electrónicos se borran, se alteran o se pierden con facilidad. Y la escena digital cambia por factores que nadie controla del todo:

- El entorno físico y los tipos de equipos.
- Cómo se comunican entre sí y con Internet.
- El sistema operativo y sus procesos automáticos.
- Las políticas de seguridad y los planes de contingencia de la organización.

La respuesta es método científico y cadena de custodia, de la escena al laboratorio y del laboratorio al juez.

<!--
Pregunta para abrir: ¿cuántos de ustedes revisarían el celular de un detenido "solo para ver"?
Anotar las respuestas en el pizarrón y volver a ellas al terminar la parte 1.
-->

---

<!-- _class: lead -->

# Parte 1
## Fundamentos legales de la investigación

---

## ¿Quién investiga?

- **CPEUM, art. 21:** la investigación corresponde al Ministerio Público y a las policías (desde 2024 también se nombra a la secretaría federal de seguridad pública y a la Guardia Nacional), bajo la conducción y mando del Ministerio Público.
- Quien integra la carpeta y decide qué se persigue es el **agente del Ministerio Público**.
- El perito es un **auxiliar**: aporta el sustento científico, no dirige la investigación.

Actualización: la PGR es hoy la Fiscalía General de la República (Ley de la FGR, 2021) y la Policía Federal dio paso a la Guardia Nacional (2019).

---

## Policía y perito no firman lo mismo

| Auxiliar | Qué hace | Qué firma |
|---|---|---|
| Policía | Preserva el lugar y los indicios (art. 132, fr. VIII) | Informe policial |
| Perito | Analiza con método científico | Dictamen pericial |

El art. 132, fr. XIV del CNPP lo dice sin rodeos: el informe de la policía no tiene carácter pericial, aunque se apoye en conocimientos técnicos.

La LFGR (art. 43) garantiza la autonomía técnica del perito: nadie le dicta el sentido de su dictamen.

---

## Etapas del procedimiento penal (art. 211 CNPP)

| Etapa | De | A |
|---|---|---|
| Investigación inicial | Denuncia o querella | El imputado queda ante el juez de control |
| Investigación complementaria | Imputación | Cierre de la investigación |
| Intermedia | Acusación | Auto de apertura a juicio |
| Juicio | Auto de apertura | Sentencia |

El perito trabaja sobre todo en la investigación, pero su dictamen se defiende en el juicio (art. 272).

---

## ¿Necesito orden del juez?

| Sin autorización (art. 251) | Con autorización previa (art. 252) |
|---|---|
| Inspección del lugar y de vehículos | Cateo |
| Aportación de comunicaciones por un participante (art. 276) | Intervención de comunicaciones privadas |
| Entrevista de testigos | Exhumación |
| Levantamiento de cadáver | Muestras corporales si la persona se niega |

Regla práctica: si el acto afecta un derecho constitucional, va con juez.

---

## Intervenir y extraer: art. 291 CNPP

- Abarca todo sistema o programa que permita intercambiar datos, audio, video, mensajes o archivos.
- El juez resuelve en un máximo de **6 horas**.
- Desde 2016 la **extracción de información** de cualquier dispositivo, equipo o nube vinculada **también requiere autorización judicial**.
- Plazo máximo, con prórrogas: **6 meses** (art. 292), igual para teléfono que para sistemas informáticos.
- Lo obtenido sin autorización o fuera de sus términos **carece de valor probatorio** (art. 299).

<!--
Volver a la pregunta de apertura: revisar el celular del detenido sin orden es extracción de
información sin autorización. La víctima, en cambio, sí puede aportar sus propios chats (art. 276).
-->

---

## Localización y datos conservados (art. 303)

- El fiscal pide al juez que ordene a concesionarios y proveedores la **localización en tiempo real** o la **entrega de datos conservados**.
- En urgencia (vida en peligro, secuestro, extorsión, delincuencia organizada) se ordena directo y el juez debe **ratificar en 48 horas**; si no ratifica, la información no entra al proceso.
- Conservación inmediata de datos informáticos: hasta **90 días**.

Actualización al 2026: el CNPP aún remite a la Ley Federal de Telecomunicaciones y Radiodifusión, abrogada por la ley publicada en julio de 2025 con efecto desde octubre de 2025, al integrarse la Comisión Reguladora de Telecomunicaciones. La conservación de registros está hoy en el art. 183 de la nueva ley: 12 meses en consulta en tiempo real y 12 más en archivo.

---

## Dato, medio y prueba

| Término | Cuándo existe |
|---|---|
| Dato de prueba | En la investigación, antes de desahogarse |
| Medio de prueba | La fuente que permite reconstruir el hecho (el dictamen, el testimonio) |
| Prueba | Lo desahogado en juicio con inmediación y contradicción |

- **Licitud (arts. 263 y 264):** lo obtenido violando derechos fundamentales se excluye.
- **Valoración (arts. 265 y 359):** libre y lógica, motivada; condena solo más allá de toda duda razonable.

---

## ¿Quién puede ser perito? (arts. 368 y 369)

- Debe tener **título oficial** en la materia, si está reglamentada.
- Si no lo está, se admite a una persona de **idoneidad manifiesta**, de preferencia avalada por un gremio.
- Las certificaciones de herramientas ayudan, pero no sustituyen el requisito.
- El peritaje que destruye el objeto (por ejemplo, desoldar un chip) es **irreproducible** y obliga a avisar a la defensa (art. 274).

---

## Lo que dice la doctrina sobre la prueba digital

Bujosa Vadell y otros (2021) la describen como **intangible, replicable, volátil, deleble y parcial**.

- Hay que demostrar **autenticidad** (origen) e **integridad** (sin alteración).
- Para admitirla: necesidad, pertinencia, utilidad, legalidad y licitud.
- Para valorarla: autenticidad, integridad, fiabilidad y disponibilidad.
- Riesgo: confundir la imagen o el video con la realidad misma y aceptar sin crítica lo que sale de una herramienta.

---

<!-- _class: lead -->

# Parte 2
## Principios éticos y cadena de custodia

---

## Lo que no se aprende en un curso

Honestidad, profesionalismo, ecuanimidad y actitud de servicio se forman a lo largo de la vida; la capacitación solo los complementa.

En informática forense se suma uno: **respeto a la privacidad**. Un teléfono contiene una vida entera; el perito busca lo que se le pidió y guarda secreto de lo demás.

<!--
Ejemplo para discutir: en el análisis aparecen fotos íntimas que nada tienen que ver con el fraude.
¿Qué se hace con ellas? Se dejan fuera del dictamen y no se comentan.
-->

---

## El decálogo del perito (1 a 5)

Propuesto por Rafael Moreno González.

1. Reconocer los límites de su capacidad científica.
2. Ser metódico, claro y preciso al dictaminar.
3. Mantener actualizados sus conocimientos.
4. Colaborar con la autoridad para esclarecer la verdad.
5. Opinar de lo técnico, nunca de lo legal.

---

## El decálogo del perito (6 a 10)

6. Actuar con imparcialidad, acuciosidad, dedicación y prudencia.
7. Aplicar el método científico.
8. Fundar las conclusiones en hechos verificados.
9. Escuchar con apertura las objeciones a su método.
10. Excusarse solo por razones técnicas, legales o éticas.

El error más común del perito novato en TIC es el punto 5: escribir "cometió fraude" en lugar de describir lo que encontró.

---

## Cadena de custodia (art. 227 CNPP)

Sistema de control y registro del indicio desde que se localiza, descubre o aporta hasta que la autoridad ordena su conclusión.

Factores que se registran: **identidad, estado original, condiciones de recolección, preservación, empaque y traslado, lugares y fechas de permanencia, cambios en cada custodia y todas las personas que tuvieron contacto**.

Responsables: todos los que tocan el indicio (art. 228).

Busca **mismidad, autenticidad, continuidad y trazabilidad**.

---

## Los instrumentos

| Instrumento | Para qué |
|---|---|
| CNPP, arts. 227 y 228 | Definición y responsables |
| Acuerdo A/009/15 (DOF 12-02-2015) | Directrices federales y cinco anexos (guía, formatos y etiqueta) |
| Guía Nacional de Cadena de Custodia (2015) | Homologa el procedimiento: procesamiento, traslado, análisis, bodega y juicio |
| ISO/IEC 27037:2012 | Evidencia digital, sobre todo en el sector privado; sigue vigente |
| NMX-I-289-NYCE-2016 | Norma mexicana de análisis forense de datos |

---

## Un formato pensado para TIC

| Apartado | Qué lleva | ¿Cambia? |
|---|---|---|
| 1. Datos generales | Fecha, hora, expediente, autoridad, quien embala, domicilio y lugar | No |
| 2. Datos del indicio | Tipo, marca, modelo, serie, inventario, color, marcas, observaciones y **serie de cada disco** | No |
| 3. Eslabones | Nombre y firma de quien recibe, unidad, fecha, hora y observaciones | Crece con cada entrega |

Mejor aún si se captura en un sistema con bitácora inalterable y el hash de cada imagen.

---

<!-- _class: lead -->

# Parte 3
## Metodología para obtener y tratar evidencia digital

---

## De dónde salen las buenas prácticas

- **1998:** nace el SWGDE en Estados Unidos para estandarizar la recuperación, preservación y análisis de evidencia digital.
- **1999:** SWGDE e IOCE presentan en Londres sus principios y estándares de evidencia digital.
- **2004:** el NIJ publica *Forensic Examination of Digital Evidence: A Guide for Law Enforcement*.
- **2008 y 2009:** el NIJ publica la guía para primeros respondientes y su versión de bolsillo.

---

## Original, duplicado y copia

| Término | Qué es |
|---|---|
| Evidencia digital | Información con valor probatorio guardada o transmitida en binario |
| Original | Los dispositivos y datos tal como estaban al adquirirlos |
| Duplicado | Reproducción exacta, bit a bit, de todo el medio |
| Copia | Reproducción exacta de la información, independiente del soporte |

El análisis se hace sobre el duplicado, y el **hash** demuestra que es idéntico al original.

---

## Calidad: los procedimientos operativos estándar

- Procedimientos escritos para todo, revisados cada año.
- Sustento en el método científico.
- Archivo histórico de lo hecho en cada caso.
- Hardware y software adecuados y probados.
- Todo se registra: notas con tinta, errores tachados y rubricados, nada se borra.
- Solo personal calificado toca lo que puede alterar el original.

La evidencia vale si se puede mostrar **exacta, confiable y controlada**.

---

## El examen forense en cuatro pasos

```mermaid
flowchart LR
    A[Valoración] --> B[Adquisición bit a bit]
    B --> C[Análisis]
    C --> D[Documentación y dictamen]
```

- **Valoración:** qué se busca, quién lo pide, en qué estado llegan los indicios y qué dice el Ministerio Público del caso.
- **Adquisición:** imagen forense con bloqueador de escritura y verificación por hash.
- **Análisis:** extracción física y lógica, archivos borrados, *file slack*, metadatos y bitácoras.
- **Dictamen:** procedimiento reproducible y conclusiones claras.

---

## La imagen forense, paso a paso

1. Asegurar la evidencia según los procedimientos de la agencia.
2. Probar el equipo del examinador.
3. Abrir el gabinete, identificar y documentar dispositivos y configuración.
4. Extraer los discos y registrar la **fecha y hora del BIOS o UEFI**.
5. Si no se puede extraer el disco (RAID, laptop cerrada): arrancar desde medio externo con bloqueo de escritura.
6. Revisar particiones ocultas, duplicar y **verificar** original contra duplicado.

<!--
Por qué la hora del BIOS: si el reloj del equipo estaba desfasado, todas las marcas de tiempo lo
están. Y por qué se recupera lo borrado: el sistema de archivos solo marca el espacio como libre.
-->

---

## Qué investiga el perito en TIC

| TIC como medio | TIC como fin |
|---|---|
| Falsificación, fraude financiero, pornografía infantil, espionaje, robo de identidad, alteración de bases de datos | Negación de servicio, destrucción de programas, robo o daño de equipos y medios, sabotaje a centros de cómputo |

Tres variantes de intervención: **informática** (accesos, fraudes, correos, recuperación de información, clonación de tarjetas), **telecomunicaciones** (equipos, intervención de líneas, robo de señal, frecuencias) y **electrónica** (circuitos, diagramas, cajeros alterados).

---

## Principios de manejo de evidencia

1. No se altera la posible evidencia, salvo autorización del Ministerio Público, que da fe de lo hecho.
2. Solo un profesional forense acreditado toca los originales.
3. Todo acceso, almacenamiento y traslado se documenta y queda disponible.
4. Quien la tiene a su cargo responde por ella.
5. Toda agencia que recolecta evidencia digital cumple estos principios.

---

<!-- _class: lead -->

# Parte 4
## En la escena: el primer respondiente

---

## Identificar y asegurar

- Fuentes: computadoras, discos, memorias (a veces disfrazadas), teléfonos, cámaras, GPS, enrutadores, impresoras, redes.
- No cambiar el estado de nada: **si está apagado, se queda apagado**.
- Sacar a las personas no autorizadas y entrevistar a los presentes: usos, usuarios, cuentas, contraseñas, proveedor de Internet.
- **Fijar** el lugar: descripción escrita, fotografía de lo general a lo particular, croquis y planimetría; además, cada pantalla y cada puerto.

---

## ¿Apago o no apago?

| Situación | Qué hacer |
|---|---|
| Equipo apagado | Documentar, etiquetar cables, desconectar, cubrir botón y ranuras, embalar |
| Encendido, caso general | Retirar la alimentación suele ser lo más seguro |
| Borrado en curso en pantalla | Cortar la energía de inmediato |
| Evidencia visible, chats, cifrado o nube | No apagar; pedir especialista en datos volátiles |
| Teléfono | Dejarlo como está y aislarlo de la red |
| Servidores y redes | Nunca apagarlos sin especialista |

Antes de todo: confirmar que hay **autoridad legal** para asegurar.

---

## Embalar y transportar

- Etiquetar todo, incluidas las conexiones, para rearmar el sistema.
- Empaques antiestáticos; nunca bolsas de plástico comunes.
- Teléfonos en **bolsa o caja de Faraday**: sin señal no hay borrado remoto.
- Lejos de imanes, calor, frío, humedad y alfombras.
- El traslado también es un eslabón de la cadena de custodia.

<!--
Material complementario: videos del fabricante de cajas y mochilas de Faraday. Mostrar uno corto
y preguntar qué pasaría con el teléfono si viajara en la guantera sin aislar.
-->

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones de la unidad

- **Tema 1.** El CNPP fija alcances y límites de la investigación: quién conduce, qué requiere juez, cómo se intervienen comunicaciones, quién dictamina y cuánto dura cada autorización.
- **Tema 2.** El perito reconoce sus límites técnicos y no opina de lo legal. La cadena de custodia, regulada en el CNPP, el Acuerdo A/009/15 y estándares nacionales e internacionales, garantiza integridad y trazabilidad hasta que la autoridad la concluye.
- **Tema 3.** Las guías del SWGDE y del NIJ ordenan el trabajo en valoración, adquisición, análisis y dictamen. El BIOS da la hora del equipo y el sistema de archivos permite recuperar lo que se "borró".

---

## Al terminar puedes

- Decir qué actos de investigación necesitan orden judicial y cuáles no.
- Distinguir un informe policial de un dictamen pericial.
- Aplicar el decálogo del perito a un caso concreto.
- Llenar un registro de cadena de custodia para un equipo de cómputo.
- Explicar los cuatro pasos del examen forense y por qué se analiza un duplicado.
- Actuar como primer respondiente ante un equipo encendido o apagado.

---

## Práctica: la prueba digital en el proceso penal

Discusión a partir de Bujosa Vadell y otros (2021):

- ¿Por qué la prueba digital exige más cuidado que la documental?
- ¿Qué requisitos debe cumplir para admitirse y para valorarse?
- ¿Cómo se resolvería en México con el CNPP?

Aportación propia y réplica fundamentada a tres compañeros. Instrucciones en la práctica de la unidad.

---

## Tarea: la escena de un crimen electrónico

Con base en la guía del NIJ para primeros respondientes, un video de 5 a 8 minutos y su guion, que expliquen las etapas de **identificar, preservar, recolectar y asegurar** la evidencia en una escena con equipos electrónicos.

Instrucciones y rúbrica en la tarea de la unidad.

---

## Lecturas

- `12-seguridad/03-forense/11-fundamentos-legales-investigacion.md`
- `12-seguridad/03-forense/12-etica-cadena-custodia.md`
- `12-seguridad/03-forense/13-obtencion-tratamiento-evidencia.md`
- Para profundizar: el CNPP completo en el sitio de la Cámara de Diputados y la guía del NIJ para primeros respondientes.

Siguiente unidad: procedimientos, identificación, fijación y preservación de evidencia digital, y el dictamen pericial.
