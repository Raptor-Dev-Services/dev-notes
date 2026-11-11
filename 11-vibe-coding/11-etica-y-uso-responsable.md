# 11 - Ética y uso responsable de la IA en el trabajo

> Fuente: *A Citizen's Guide to Artificial Intelligence* (Zerilli et al., 2021): caps. 4, 6, 8 y 10; *Moral Codes* (Blackwell, 2024): caps. 1 a 3 y 11; Reglamento (UE) 2024/1689, texto consolidado al 27-07-2026; LFPDPPP y LFDA vigentes; OWASP Top 10 para aplicaciones con LLM, edición 2025; NIST AI RMF 1.0 (2023). Los libros se leen por los conceptos. La regulación cambia rápido: lo fechado "a noviembre de 2026" se revisa antes de usarlo.

## Problema que resuelve

Un asistente de IA hace que cada persona del equipo pueda producir código, texto y análisis en minutos. Lo que no hace es decidir qué datos se le pueden dar, quién revisa lo que entrega, de quién es lo que genera ni quién responde si algo sale mal. Si nadie lo decide antes, se decide después, con un incidente de por medio.

Esta nota cubre esas decisiones, una por una, y termina con una **plantilla de política de uso** para un equipo. Complementa `03-ai-workflow.md` (qué se delega y qué no) y `10-seguridad-llm.md` (cómo se ataca y se defiende una aplicación con LLM); aquí el foco es el uso profesional y sus reglas.

## Qué datos no se comparten

Los datos personales entran a una herramienta de IA en dos momentos: cuando se entrena un modelo con ellos y cuando alguien los escribe o los sube para usarla. El segundo es de quien usa la herramienta.

**Qué es un dato personal.** La LFPDPPP (última reforma DOF 14-11-2025, art. 2, fr. V) lo define como cualquier información concerniente a una persona identificada o identificable, y una persona es identificable cuando su identidad puede determinarse directa o indirectamente. Los **datos sensibles** (art. 2, fr. VI) afectan la esfera más íntima o su mal uso puede dar origen a discriminación o riesgo grave; la lista es enunciativa, no limitativa, e incluye salud, origen racial o étnico, información genética, creencias, opiniones políticas y preferencia sexual.

**Quitar el nombre no anonimiza.** Un conjunto sin nombres pero con código postal, fecha de nacimiento y sexo puede cruzarse con otra lista y señalar a una persona (Sweeney, 2002). La disociación que reconoce la LFPDPPP (art. 2, fr. IX) exige que los datos no puedan asociarse a la persona ni permitir, por su estructura, contenido o **grado de desagregación**, identificarla.

**Qué riesgo dice OWASP.** En la edición 2025, LLM02 (*Sensitive Information Disclosure*) incluye datos personales, financieros, historiales médicos, información confidencial del negocio, credenciales y documentos legales, y pide a las aplicaciones sanear los datos para que no entren al entrenamiento y ofrecer términos de uso claros y la opción de excluirse.

**Regla de tres colores.** Clasificación práctica, ajustable a la organización:

| Color | Datos | Qué hacer |
|---|---|---|
| Rojo | Datos sensibles, credenciales, llaves, expedientes, información de terceros sin autorización | No se escriben en ninguna herramienta de IA externa |
| Amarillo | Datos personales ordinarios, código propio, información confidencial del negocio | Solo con una herramienta autorizada, con contrato que defina uso y retención, y reducidos al mínimo |
| Verde | Información pública o inventada para el caso | Se puede usar |

Ante la duda, es amarillo y se pregunta. Antes de pegar nada, aplica la **minimización**: el RGPD (art. 5, apartado 1, letra c) pide datos adecuados, pertinentes y limitados a lo necesario; la LFPDPPP (art. 12) pide que el tratamiento sea necesario, adecuado y relevante.

## Revisión humana que revisa de verdad

Que haya una persona en el proceso no es lo mismo que haya revisión. El Reglamento de IA (art. 14, apartado 4) pide que quien supervisa pueda entender capacidades y límites del sistema, ser consciente de la tendencia a confiar en exceso en sus resultados (**sesgo de automatización**), interpretarlos, descartarlos o revertirlos e interrumpir el sistema. Y el art. 26, apartado 2, pide encomendar la supervisión a personas con **competencia, formación y autoridad**.

Aplicado al trabajo con asistentes:

| Señal | Revisión de papel | Revisión real |
|---|---|---|
| Tiempo | Se acepta lo que sale en segundos | Se asigna tiempo según el riesgo de lo que se revisa |
| Información | Solo se ve el resultado | Se ve el origen, las pruebas y lo que cambió |
| Autoridad | Rechazar exige justificarse | Rechazar es normal y no cuesta nada |
| Registro | Solo queda "aprobado" | Queda quién, cuándo y por qué |

Parasuraman y Manzey (2010) concluyen, en una revisión de estudios, que el sesgo de automatización aparece tanto en personas novatas como expertas y no se previene solo con capacitación ni instrucciones: por eso se rediseña el proceso, no solo se regaña a la persona. Qué se delega y qué se queda con una persona está en `03-ai-workflow.md` (sección "Cuándo delegar al AI").

Lo que entrega la herramienta se **verifica**: una cita, una cifra o una ley que no puedas comprobar en la fuente no entra al trabajo. OWASP lista como LLM09 la desinformación: respuestas falsas dichas con seguridad.

## Propiedad intelectual y licencias

La Ley Federal del Derecho de Autor (última reforma DOF 14-05-2026) establece, entre otras cosas:

- **Autor** es la persona física que ha creado una obra literaria y artística (art. 12).
- Los programas de computación, incluidos los de inteligencia artificial, se protegen en los mismos términos que las obras literarias (art. 102).
- Salvo pacto en contrario, los derechos patrimoniales de un programa creado por empleados en el ejercicio de sus funciones o siguiendo instrucciones del empleador corresponden al empleador (art. 103).
- La imagen y la voz de las personas artistas intérpretes solo pueden usarse con su consentimiento expreso, y la protección abarca los resultados generados por sistemas de IA (art. 87).

Esas líneas no dicen cómo se trata el material generado por una herramienta, ni qué licencia tiene el código que sugiere. Eso se resuelve con los **términos de la herramienta** y con asesoría legal. Lo mínimo para un equipo:

1. Leer y registrar los términos de servicio de cada herramienta autorizada: quién es dueño de la salida y qué restricciones de uso hay.
2. Revisar la licencia de toda dependencia o fragmento de terceros que el asistente proponga, igual que si lo hubiera propuesto una persona.
3. Declarar en el entregable qué partes se generaron con ayuda de una herramienta, cuando el cliente o el contrato lo requiera.
4. No generar ni manipular la imagen o la voz de una persona sin su consentimiento.

## Sesgo y equidad en lo que se produce y se usa

Un asistente puede reproducir sesgos de los datos con que se entrenó, y quien lo usa los hereda. El marco NIST AI RMF 1.0 (NIST AI 100-1, 2023) incluye entre las características de un sistema confiable el ser **justo, con sesgos dañinos gestionados**, y el Reglamento de IA considera de alto riesgo, en el anexo III, los sistemas usados para contratar, evaluar a trabajadores, evaluar la solvencia crediticia o decidir el acceso a servicios esenciales.

En el trabajo diario:

- No uses un asistente para **decidir** sobre personas (contratar, calificar, negar un servicio) sin supervisión humana real y sin poder explicar el resultado a la persona afectada.
- Si una decisión usa un sistema automatizado, recuerda que la persona tiene derechos: la LFPDPPP (art. 26, fr. II) reconoce la oposición al tratamiento automatizado que le produzca efectos jurídicos no deseados o la afecte significativamente, destinado a evaluarla sin intervención humana.
- Revisa las muestras de lo que genera la herramienta con ojos distintos a los tuyos: ¿a quién deja fuera, a quién describe con un estereotipo?

## Transparencia hacia quien recibe el trabajo

Quien recibe un texto, una imagen, un informe o un código tiene derecho a saber cuándo es obra de una herramienta si eso cambia cómo debe leerlo. El Reglamento de IA lo exige en ciertos casos (art. 50, aplicable desde el 2 de agosto de 2026): informar de que se interactúa con un sistema de IA, marcar el contenido sintético en formato legible por máquina, hacer público que una ultrafalsificación es artificial y divulgar el texto generado para informar al público sobre asuntos de interés público, salvo que haya revisión humana o control editorial con responsabilidad editorial.

Aunque tu caso no caiga en la ley, es una buena práctica: **declara qué herramienta usaste, para qué y quién revisó.** Ejemplo ilustrativo de nota al pie: "Borrador de la sección 2 generado con una herramienta de IA a partir de las notas del equipo; revisado y corregido por [persona responsable]."

## Seguridad de los asistentes con permisos

Un asistente con acceso al repositorio, al correo o a producción puede hacer daño por un error o por una instrucción maliciosa escondida en un documento. OWASP lo recoge como LLM06 (agencia excesiva: más herramientas, permisos o autonomía de los que la tarea requiere). En política de equipo se traduce en:

1. **Permisos mínimos:** el asistente solo accede a lo que la tarea necesita.
2. **Aprobación humana** de lo que importa: un pago, un borrado, un envío, un cambio en producción.
3. **Registro:** qué hizo el asistente, con qué permisos y quién aprobó.

El detalle técnico (inyección de prompts, defensas en capas, topes de gasto) está en `10-seguridad-llm.md`.

## Marco regulatorio, a noviembre de 2026

| Norma | Qué aporta a un equipo |
|---|---|
| Reglamento (UE) 2024/1689, modificado por el Reglamento (UE) 2026/1744 | Clasifica por riesgo. Prohibiciones (art. 5) y alfabetización (art. 4) desde 2025; transparencia (art. 50) desde el 2 de agosto de 2026; alto riesgo del anexo III desde el 2 de diciembre de 2027. Alcanza a quien use resultados en la Unión (art. 2) |
| NIST AI RMF 1.0 (2023) | Marco voluntario: Govern, Map, Measure, Manage |
| LFPDPPP y LGPDPPSO (México, DOF 14-11-2025) | Principios, finalidad y consentimiento; oposición al tratamiento automatizado (arts. 26, fr. II, y 41, fr. II) |
| LFDA (México, DOF 14-05-2026) | Programas de computación, incluidos los de IA (art. 102); imagen y voz y resultados generados por IA (art. 87) |

México no tiene, a noviembre de 2026, una ley general de IA publicada: el texto vigente de la Constitución (última reforma DOF 07-10-2026) no menciona la IA y una iniciativa para facultar al Congreso a legislar en la materia se recibió el 5 de agosto de 2026 (Gaceta Parlamentaria, 17-08-2026). Se verifica el estado el día de la consulta.

## Política de uso de IA de un equipo

Una política es **corta, concreta, comprobable y firmada**. Una o dos cuartillas, ocho apartados:

```markdown
# Política de uso de IA - [Equipo] - versión [n] - [fecha]

## 1. Alcance
A quién y a qué aplica (personas, proyectos, tipos de herramienta).

## 2. Herramientas autorizadas
| Herramienta | Para qué | Datos permitidos (color) | Responsable de la cuenta |
|---|---|---|---|

## 3. Datos
- Rojo (no se escriben en ninguna herramienta externa): ...
- Amarillo (solo en herramientas del apartado 2, con el mínimo necesario): ...
- Verde: ...
Si dudas, es amarillo y preguntas a [rol].

## 4. Revisión humana
Qué se revisa siempre (código que toca seguridad, datos personales, cifras
y citas), quién revisa, con qué criterio y qué queda registrado.

## 5. Propiedad intelectual
Qué se puede usar, cómo se revisan licencias de lo que propone la herramienta
y qué se declara al cliente.

## 6. Transparencia
Qué se dice a quien recibe el trabajo (herramienta, para qué, quién revisó).

## 7. Incidentes
Qué es un incidente (dato rojo en una herramienta, contenido falso publicado),
a quién se avisa y en cuánto tiempo, y qué se hace para contener y corregir.

## 8. Responsable y revisión
Quién firma, quién responde ante el cliente y cada cuándo se revisa la política
(o cuando cambie una ley o los términos de una herramienta).
```

Criterio de calidad: **cada regla se puede comprobar.** Alguien debe poder mirar un caso y decir si se cumplió. "Usa tu criterio" no es una regla.

## Errores comunes

| Error | Por qué falla | Qué hacer |
|---|---|---|
| Prohibir la IA sin ofrecer una herramienta autorizada | El riesgo es el uso en la sombra: cada persona usa la suya sin reglas | Autoriza herramientas concretas y di para qué sirven |
| "Usa tu criterio" | No se puede comprobar | Reglas por tipo de dato y tarea |
| Una política de decenas de páginas | Nadie la lee | Una o dos cuartillas, con ejemplos |
| Revisión humana "para cumplir" | La persona no tiene tiempo, información ni autoridad | Diseña el proceso para que rechazar sea normal |
| Quitar el nombre y creer que ya es anónimo | Los cuasi-identificadores se cruzan con otra lista | Mide cuántas veces aparece cada combinación |
| Dar por buena una cita de la herramienta | Puede estar inventada | Verifica en la fuente o no la uses |
| Sin fecha de revisión | La ley y los términos cambian | Revisa cada seis meses o ante un cambio |

## Glosario

| Término | Definición |
|---------|-----------|
| Dato personal | información concerniente a una persona identificada o identificable |
| Dato sensible | dato personal de la esfera más íntima o cuyo mal uso puede causar discriminación o un riesgo grave |
| Disociación | procedimiento por el que los datos no pueden asociarse a la persona ni permitir identificarla por su estructura, contenido o grado de desagregación |
| Cuasi-identificador | dato que no identifica solo pero sí combinado con otros |
| Minimización | usar solo los datos necesarios para el fin |
| Sesgo de automatización | tendencia a confiar en exceso en los resultados de un sistema |
| Supervisión humana | vigilancia efectiva de un sistema por personas con competencia, formación y autoridad |
| Agencia excesiva | más herramientas, permisos o autonomía de los que la tarea requiere |
| Uso en la sombra | uso de herramientas de IA personales y no autorizadas para trabajo de la organización |
| Política de uso de IA | documento corto y firmado con las reglas de un equipo: herramientas, datos, revisión, propiedad intelectual y responsables |

## Referencias

- Reglamento (UE) 2024/1689 del Parlamento Europeo y del Consejo, de 13 de junio de 2024 (Reglamento de Inteligencia Artificial), texto consolidado al 27-07-2026: arts. 2, 4, 5, 14, 26 y 50 y anexo III.
- Reglamento (UE) 2026/1744 del Parlamento Europeo y del Consejo, de 8 de julio de 2026, que modifica el Reglamento (UE) 2024/1689.
- Reglamento (UE) 2016/679 (RGPD), art. 5.
- Ley Federal de Protección de Datos Personales en Posesión de los Particulares, última reforma DOF 14-11-2025, arts. 2, 12 y 26.
- Ley General de Protección de Datos Personales en Posesión de Sujetos Obligados, última reforma DOF 14-11-2025, art. 41.
- Ley Federal del Derecho de Autor, última reforma DOF 14-05-2026, arts. 12, 87, 102 y 103.
- Constitución Política de los Estados Unidos Mexicanos, texto vigente, última reforma DOF 07-10-2026, art. 73. Gaceta Parlamentaria, Cámara de Diputados, año XXIX, núm. 7105, 17-08-2026.
- NIST (2023). *Artificial Intelligence Risk Management Framework (AI RMF 1.0)*, NIST AI 100-1. https://doi.org/10.6028/NIST.AI.100-1
- OWASP (2024). *OWASP Top 10 for LLM Applications 2025*: LLM02, LLM06 y LLM09.
- Sweeney, L. (2002). k-anonymity: a model for protecting privacy. *International Journal of Uncertainty, Fuzziness and Knowledge-Based Systems*, 10(5), 557-570.
- Parasuraman, R. y Manzey, D. H. (2010). Complacency and bias in human use of automation: an attentional integration. *Human Factors*, 52(3), 381-410. https://doi.org/10.1177/0018720810376055

---

*Rogelio Arriaga Gonzalez*
