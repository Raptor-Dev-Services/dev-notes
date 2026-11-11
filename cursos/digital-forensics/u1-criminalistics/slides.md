<!-- _class: lead -->

# U1 - La criminalística y su relación con la informática forense

### Informática forense

**Rogelio Arriaga - Raptor Dev Services**

<!--
Unidad de 5 horas. Hilo conductor: un equipo de cómputo encendido en el lugar de los hechos. Cada tema
responde una pregunta sobre ese equipo: qué permite la ley, cómo se fija sin contaminarlo, qué se busca
y dónde está cada dato. Se cierra con la tarea: fijar un equipo encendido.
-->

---

## El lema de la unidad

> "En toda pesquisa criminal, el tiempo que pasa es la verdad que huye." Edmond Locard

En TIC la frase es literal: la memoria de un equipo encendido se pierde al apagarlo, y un teléfono conectado puede borrarse a distancia mientras se atiende otra cosa.

---

## Objetivo de la unidad

Al terminar, puedes:

- Explicar cómo está ordenado el marco legal mexicano y por qué condiciona cada paso de una investigación.
- Aplicar los principios de la criminalística a delitos cometidos con TIC.
- Usar el método científico para plantear y resolver un problema pericial.
- Reconocer qué aporta un sistema de gestión de la seguridad de la información a la investigación.

---

## Agenda

| Bloque | Tema |
|---|---|
| 1 | Derecho positivo mexicano |
| 2 | Criminalística orientada a las TIC |
| 3 | Metodología de la investigación |
| 4 | Las TIC en la investigación de ciberdelitos |
| 5 | Práctica: el foro de discusión |
| 6 | Cierre, tarea y lo que sigue |

---

## Para quién es esto

Quien investiga delitos con TIC casi nunca domina todo:

- El **juez** y el **Ministerio Público** conocen la ley, no la técnica.
- La **policía de investigación** conoce el terreno, no siempre el laboratorio.
- El **perito** y el **experto en seguridad** conocen la técnica, no siempre la ley.

Esta unidad da a todos la misma base para trabajar de forma racional, controlada y fundamentada.

<!--
Pregunta de arranque: ¿quién de ustedes ha tenido que explicar algo técnico a un abogado, o algo legal
a un técnico? Esa fricción es el tema de la unidad.
-->

---

<!-- _class: lead -->

# Parte 1
## Derecho positivo mexicano

---

## La técnica sin ley contamina

- Un perito que solo sabe usar la herramienta no distingue lo que puede revisar de lo que exige orden judicial.
- Resultado: indicios contaminados, excesos y huecos que la defensa aprovecha.
- La autoridad que pide el dictamen confía en que el experto conoce también el terreno legal.

Lectura: `12-seguridad/03-forense/01-derecho-positivo-mexicano.md`.

---

## Qué es el derecho positivo

- Normas escritas por una autoridad facultada que rigen **aquí y ahora**.
- Cambian con la sociedad: el ciberacoso empujó reformas que antes no hacían falta.
- Se publican en el **Diario Oficial de la Federación**; desde ahí obligan.
- Ejemplo: el CNPP (DOF 05-03-2014) rige en todo el país desde el **18 de junio de 2016** y dejó abrogado al código federal anterior.

---

## La jerarquía de las normas

| Nivel | Norma |
|---|---|
| 1 | Constitución (artículo 133: Ley Suprema) |
| 2 | Leyes federales y tratados |
| 3 | Leyes de cada entidad federativa |
| 4 | Decretos |
| 5 | Reglamentos |
| 6 | Normas individualizadas: sentencias, contratos, resoluciones |

Si una política interna choca con un derecho constitucional, gana la Constitución.

---

## Quién inicia las leyes (artículo 71)

1. La persona titular de la Presidencia de la República.
2. Diputados y senadores.
3. Las legislaturas de los estados y de la Ciudad de México.
4. La ciudadanía: al menos el 0.13 % de la lista nominal de electores.

Iniciar no es aprobar: la iniciativa pasa por las dos cámaras.

---

## El delito y el fuero

- **Delito**: acto u omisión que sancionan las leyes penales (artículo 7o. del Código Penal Federal).
- La pena busca detener la conducta, reparar el daño y desalentar la repetición.

| | Fuero federal | Fuero común |
|---|---|---|
| Ley | Código Penal Federal y leyes federales | Código penal de cada estado |
| Investiga | Fiscalía General de la República | Fiscalía de cada estado |

<!--
Actualización: la PGR es FGR desde diciembre de 2018. Y la Policía Federal ya no existe: la Guardia
Nacional la sustituyó en 2019 y hoy se rige por la ley de julio de 2025.
-->

---

## Cuatro reglas constitucionales que el perito usa siempre

| Artículo | Regla |
|---|---|
| 1o. | Los derechos humanos se interpretan a favor de la persona |
| 16 | Las comunicaciones privadas son inviolables; solo un juez federal autoriza intervenirlas |
| 20, A, IX | La prueba obtenida violando derechos fundamentales es nula |
| 21 | La investigación la conduce el Ministerio Público |

---

<!-- _class: lead -->

# Parte 2
## Criminalística orientada a las TIC

---

## De dónde viene

| Año | Hito |
|---|---|
| 1892 | Hans Gross usa el término "criminalística"; Galton publica *Finger Prints* |
| 1894 | Vucetich: cuatro patrones dactilares |
| 1901 | Landsteiner: grupos sanguíneos A, B y O |
| 1910 | Locard funda su laboratorio y formula el principio de intercambio |
| 1932 | Laboratorio forense del FBI |
| 1975 | Academia Mexicana de Criminalística |

<!--
Detalle para contar: el manual de Gross ya incluía criptografía y el rastreo de cómo se comunicaban
los involucrados. Lo técnico estuvo ahí desde el principio.
-->

---

## Qué estudia la criminalística

- Disciplina que aplica métodos científicos a los **indicios** para demostrar un hecho y señalar a sus autores.
- Cinco objetivos: probar el hecho, reconstruir su mecánica, identificar a la víctima, identificar a los autores y aportar pruebas de la participación de cada quien.
- En TIC el reto es doble: dispositivos cada vez más variados y pequeños, y datos que se pueden borrar a distancia.

---

## Los cuatro principios, traducidos

| Principio | En TIC |
|---|---|
| Intercambio | Toda transacción queda en alguna bitácora |
| Correspondencia | El hash es la huella del archivo |
| Reconstrucción | Lo que dejó rastro se puede rastrear |
| Probabilidad | Una IP con puerto y hora exacta reduce los candidatos |

<!--
Actualización: con CGNAT, una IPv4 pública la comparten cientos de clientes. Sin puerto de origen y
hora exacta con zona horaria, la IP sola no identifica a nadie.
-->

---

## La regla de oro

> No tocar ni mover nada hasta que esté identificado, medido y fotografiado.

- En un equipo encendido, apagarlo es mover algo: se pierde la RAM.
- Objetivo final, ya en el laboratorio: contestar las **siete preguntas de oro**: ¿qué?, ¿quién?, ¿cómo?, ¿cuándo?, ¿dónde?, ¿por qué? y ¿con qué?

---

## Proteger el lugar

1. Mantener el área sin cambios hasta que llegue la autoridad.
2. Cerrar y vigilar accesos; al aire libre, acordonar.
3. Registrar a cada persona que entra y sale.
4. Nada se toca sin guantes ni se cambia de posición.
5. Documentar pasillos y accesos para no confundir rastros propios con indicios.

En TIC, las "puertas" también son nodos de red, wifi, bluetooth y líneas telefónicas.

---

## Fijar el lugar

| Técnica | Lo esencial en TIC |
|---|---|
| Fotografía | Vistas generales, medias, acercamientos y grandes acercamientos; procesos en pantalla; conexiones |
| Descripción | De lo general a lo particular; dos personas si no hay grabadora |
| Croquis | Medidas, posición de la cámara, escala, norte; periféricos que no parecen informáticos |
| Planimetría | Ubicación a escala de cada equipo e indicio |

Una foto que será prueba se protege con su hash desde que se toma.

---

## Ligar el equipo con una persona

- Encontrar archivos es lo fácil; probar quién estaba al teclado, lo difícil.
- **Huellas dactilares**: arco, presilla interna, presilla externa, verticilo; minucias comparadas por AFIS.
- **Pelo**: la raíz aporta ADN nuclear; la fase del pelo decide si sirve.
- **Escamas de piel**: debajo de las teclas y en el polvo de los ventiladores.

---

## Embalar sin destruir

- Antes de embalar: ¿encendido o apagado?, ¿recibe comunicaciones?, ¿contraseña?, ¿batería?
- Teléfono encendido: aislarlo de la red (aluminio o bolsa de Faraday).
- Guantes y pulsera antiestática; protección contra golpes, humedad y temperatura.
- Embalaje **sellado**, con sellos fotografiados, y cadena de custodia (artículos 227 y 228 del CNPP).

<!--
Pregunta: ¿por qué un teléfono encendido no se apaga sin más? Respuesta esperada: se activa el cifrado
completo y sin el código de desbloqueo se pierde el acceso; además, la RAM se pierde.
-->

---

## Lo que dice el CNPP sobre la prueba

- Todo hecho se prueba con cualquier medio **lícito** (artículo 259).
- El juez valora de forma libre y lógica (artículos 265 y 402).
- Nadie es condenado si hay duda razonable; **la duda favorece al acusado**.

Un equipo apagado sin registro, una copia sin hash o un embalaje sin sello son la duda razonable que busca la defensa.

---

<!-- _class: lead -->

# Parte 3
## Metodología de la investigación

---

## El método científico

- Camino sistemático para obtener y comprobar conocimiento.
- **Reproducibilidad**: otro perito, con la misma copia, llega al mismo resultado.
- **Falsabilidad**: toda conclusión debe poder contrastarse y caer si algo la contradice.

---

## Métodos aceptados

| Familia | Ejemplos |
|---|---|
| Empírico-analítico | Experimental, fenomenológico, histórico, sistémico, sintético, lógico |
| Lógico deductivo | Directo o mediante silogismo; usa extrapolación |
| Lógico inductivo | Completo o incompleto (enumeración, inducción científica); usa interpolación |
| Analogía | Semejanzas observadas sugieren otras; se comprueban después |

---

## El modelo MC-14

| Estadio | Pasos |
|---|---|
| I. Observación | 1 a 4: observar, definir el problema, objetivos, recopilar evidencias |
| II. Inducción generalizada | 5 y 6: alternativas y evaluación de pruebas |
| III. Hipótesis | 7 |
| IV. Prueba por experimentación | 8 a 10: probar, concluir, sostener con mente abierta |
| V. Tesis | 11: revisión por pares |
| VI. Ingredientes | 12 a 14: métodos, objetivos, aptitudes |

El dictamen llega hasta el **paso 9**: responde a una autoridad, no busca aceptación científica.

---

## El modelo de Arias Galicia

1. Planteamiento del problema: ¿qué se necesita saber?
2. Planeación: ¿qué recursos y actividades?
3. Recopilación: ¿cómo y con qué se obtienen los datos?
4. Procesamiento de datos.
5. Explicación e interpretación.
6. Comunicación de resultados.

---

## El error más caro: "imprímanlo todo"

- Se han entregado dictámenes de **500 mil hojas** que nadie leyó.
- Un buen planteamiento convierte cientos de miles de resultados en unos cuantos documentos.
- Pregunta general (la esencia del problema) y preguntas específicas (fechas, autoría, cuentas).

<!--
Ejercicio rápido: un fraude financiero. Que el grupo formule la pregunta general y tres específicas
en dos minutos.
-->

---

<!-- _class: lead -->

# Parte 4
## Las TIC en la investigación de ciberdelitos

---

## Qué son las TIC

- Estudio, diseño, desarrollo, implementación, soporte y administración de sistemas basados en computadoras.
- Ciclo completo del dato: entrada, procesamiento, almacenamiento, recuperación, transmisión y recepción.
- De IT a ICT: procesar y comunicar ya son lo mismo, y el delito vive entre varias máquinas.

---

## El SGSI le ahorra semanas al perito

| Marco | Qué aporta |
|---|---|
| COBIT 2019 (ISACA) | Procesos, responsables y controles de TI alineados al negocio |
| ISO/IEC 27001:2022 | Inventario de activos, riesgos, políticas y 93 controles en cuatro temas |

Si no hay SGSI, el perito documenta él mismo los procesos ligados al incidente.

---

## Qué se solicita a la organización

- Activos dentro del alcance y sus propietarios.
- Amenazas e impactos en confidencialidad, integridad y disponibilidad.
- Políticas de seguridad.
- Diagramas de red, configuraciones, cuentas privilegiadas.
- Qué bitácoras existen y cuánto tiempo se conservan.

---

## Orden de volatilidad (RFC 3227)

1. Registros y caché del procesador.
2. Tablas de red y procesos, memoria RAM.
3. Archivos temporales.
4. Disco.
5. Registros remotos y monitoreo.
6. Topología y configuración física.
7. Respaldos.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Conclusiones de la unidad

- Las TIC son el conjunto de herramientas con que hoy se gestiona y analiza la información; investigarlas exige entenderlas.
- Normas como ISO/IEC 27001 y marcos como COBIT dicen qué documentos y controles debería haber, y ayudan a proteger la integridad de la evidencia.
- La informática forense no se reduce a sacar imágenes de discos: hay que entender cómo encajan las TIC en el incidente.
- Ley, criminalística, método y TIC juntos permiten responder las siete preguntas de oro.

---

## Práctica: el foro de discusión

**¿Sirven o no los métodos criminalísticos para investigar ciberdelitos?**

- Primer momento: tu postura, argumentada con lo visto en la unidad.
- Segundo momento: comentar el trabajo de tres compañeros, diciendo si coinciden y qué cambiarían.

Instrucciones completas en la práctica de la unidad.

---

## Tarea

**Fijar un equipo de cómputo encendido aplicando los procedimientos criminalísticos.**

- Cuadro sinóptico de las etapas del manejo de indicios digitales.
- Bitácora de fijación de un equipo encendido (en laboratorio o en un caso simulado).
- Cálculo de hash de cada archivo generado.

Instrucciones y rúbrica en la tarea de la unidad.

---

## Lecturas

- `12-seguridad/03-forense/01-derecho-positivo-mexicano.md`
- `12-seguridad/03-forense/02-criminalistica-tic.md`
- `12-seguridad/03-forense/03-metodologia-investigacion.md`
- `12-seguridad/03-forense/04-tic-investigacion-ciberdelitos.md`

---

## Siguiente unidad: cibercriminalidad

> "La tecnología es un buen sirviente, pero un amo peligroso." Christian Lous Lange

- Qué es un ciberdelito y cómo se clasifica.
- Cómo se ve el fenómeno en el mundo y en México.
- Qué soluciones técnicas y legales hay para frenarlo.

Traigan su cuadro sinóptico: lo usaremos para ubicar cada ciberdelito en su etapa de investigación.
