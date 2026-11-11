# 03 - Respuesta a incidentes

> Lectura del curso [Defensa y respuesta a incidentes](../../cursos/blue-team/README.md), sesión 3.

## Por qué importa

Un incidente no avisa cuándo empieza y casi nunca llega solo. Lo que separa una noche mala de un desastre es, sobre todo, si el equipo sabe **qué hacer primero** cuando suena la alerta. Quien improvisa el orden comete dos errores opuestos y ambos son caros: contener antes de entender (apagar, reiniciar, borrar) destruye la evidencia que explica qué pasó, y entender sin contener (seguir mirando "un ratito más") le regala tiempo al atacante.

Esta nota explica cómo ordena la respuesta la publicación vigente de NIST, qué roles intervienen, cómo se decide cuándo contener, qué es erradicar de verdad y qué se comunica y a quién. Cierra con el modelo F3EAD, que le da a la respuesta un método de inteligencia: cada incidente deja datos que mejoran la defensa del siguiente.

> **Escenario ilustrativo.** Los ejemplos usan el laboratorio del curso (`kali` 192.168.56.10, `victima` 192.168.56.20, `siem` 192.168.56.30). Es un **caso hipotético**: personas, hosts y datos son ficticios.

## Evento, incidente y por qué la diferencia importa

NIST SP 800-61r3 define un **evento** como cualquier ocurrencia observable que involucra activos de cómputo, y un **incidente de ciberseguridad** con la definición de la ley FISMA de 2014: una ocurrencia que pone en peligro, real o inminentemente y sin autoridad legal, la integridad, la confidencialidad o la disponibilidad de la información o de un sistema de información, o que constituye una violación o amenaza inminente de violación de la ley, de las políticas o de los procedimientos de seguridad (NIST, 2025, sección 1).

Un inicio de sesión es un evento. Un inicio de sesión desde una IP que nunca había entrado, a las 2:31 de la mañana, con una contraseña que ya estaba filtrada, **puede** ser un incidente. La publicación dice que "a menudo se necesita análisis adicional" para saber si un evento adverso indica un incidente, y la subcategoría DE.AE-08 pide declararlo cuando los eventos cumplen **criterios de incidente definidos de antemano** y considerando los falsos positivos conocidos. Declarar es una decisión con dueño: a partir de ahí corre el plan de respuesta.

## Cómo organiza NIST la respuesta (Actualización al 2026)

La **revisión 2 (agosto de 2012)** de la guía de manejo de incidentes dibujaba un ciclo de cuatro fases: preparación; detección y análisis; contención, erradicación y recuperación; y actividad posterior al incidente. La **revisión 3 (abril de 2025)** sustituye esa publicación y abandona el ciclo cerrado: su alcance, dice, cambió "significativamente" porque los detalles de cómo hacer respuesta cambian demasiado como para mantenerlos en un documento estático. En su lugar organiza las recomendaciones con las seis funciones del Marco de Ciberseguridad (CSF) 2.0, como un *perfil comunitario* (NIST, 2025, secciones 1.1 y 2.1).

| Función CSF 2.0 | Qué logra | Papel en la respuesta |
|---|---|---|
| Govern (GV) | La estrategia, expectativas y política de riesgo de ciberseguridad se establecen, comunican y vigilan | Preparación: políticas, roles, autoridad |
| Identify (ID) | Se entienden los riesgos actuales | Preparación; su categoría Improvement (ID.IM) recibe las lecciones aprendidas de todas las funciones |
| Protect (PR) | Se usan salvaguardas | Preparación: reducir la probabilidad y el impacto |
| Detect (DE) | Los posibles ataques y compromisos se encuentran y analizan | Respuesta: hallar, analizar y declarar |
| Respond (RS) | Se actúa sobre un incidente detectado | Respuesta: gestionar, analizar, comunicar y mitigar |
| Recover (RC) | Se restauran los activos y operaciones afectados | Respuesta: restaurar y comunicar la recuperación |

Tres ideas de la publicación importan al operar:

1. **Govern, Identify y Protect no son parte de la respuesta**, son preparación. Detect, Respond y Recover sí son la respuesta (NIST, 2025, sección 2.1).
2. **La mejora es continua**, no una fase al final: las lecciones se comparten en cuanto se identifican, no se esperan hasta que termina la recuperación.
3. **Cada organización elige el modelo que le sirva.** La propia guía dice que debe usarse el que mejor se ajuste, y su tabla 1 mapea las fases viejas a las funciones nuevas: Preparación a Govern, Identify y Protect; Detección y análisis a Detect; Contención, erradicación y recuperación a Respond y Recover; Actividad posterior al incidente a la categoría de mejora de Identify.

### El orden clásico como lista de acciones

Los libros de respuesta siguen ordenando las acciones de un incidente en el mismo orden que el ciclo clásico, y ese orden sigue siendo útil en el teclado. *Intelligence-Driven Incident Response* (Brown y Roberts, 2023), cap. 3, presenta el ciclo con seis etapas: preparación, identificación, contención, erradicación, recuperación y lecciones aprendidas. En este curso se usa como **lista de comprobación operativa**, y cada etapa se amarra a la subcategoría de NIST que la sostiene:

| Etapa operativa | Qué se hace | Subcategorías de NIST SP 800-61r3 |
|---|---|---|
| Identificar | Triage, validar, categorizar, declarar | DE.AE-08, RS.MA-02, RS.MA-03 |
| Preservar y analizar | Registrar acciones, reunir datos con su integridad, establecer qué pasó | RS.AN-03, RS.AN-06, RS.AN-07 |
| Contener | Evitar que el incidente se extienda | RS.MI-01 |
| Estimar magnitud | Buscar indicadores y persistencia en otros activos | RS.AN-08 |
| Erradicar | Eliminar persistencia y puntos de entrada | RS.MI-02 |
| Recuperar | Restaurar, verificar integridad, declarar el fin | RC.RP-01 a RC.RP-06 |
| Comunicar | Coordinar, notificar y compartir | RS.CO-02, RS.CO-03, RC.CO-03, RC.CO-04 |
| Aprender | Informe posterior, mejoras | RC.RP-06 R1 y categoría ID.IM |

## Quién hace qué

La publicación subraya que ya no basta con el equipo técnico: la respuesta depende de muchas partes internas y externas, con roles que varían según la organización y el incidente (NIST, 2025, sección 2.2). Sus ejemplos de roles son liderazgo, **incident handlers** (quienes verifican el incidente, reúnen y analizan datos, priorizan y limitan el daño), profesionales de tecnología, área legal, comunicación y relaciones públicas, recursos humanos, seguridad física, y dueños de los activos. Los terceros, como un proveedor de servicios de seguridad administrados o un proveedor de nube, operan bajo un modelo de responsabilidad compartida que debe quedar claro en el contrato.

Para un equipo pequeño conviene fijar al menos cuatro papeles antes del incidente:

| Papel | Responsabilidad | Detalle |
|---|---|---|
| Líder del incidente | Decide y coordina | La subcategoría RS.MA-01 sugiere designar un líder por incidente (consideración C1) |
| Responsable técnico | Ejecuta contención y análisis | Es quien toca el sistema; lleva la bitácora de acciones |
| Escriba | Registra cronología y decisiones | Separar este papel del técnico evita que la bitácora se escriba "después" |
| Comunicaciones | Habla con dirección, clientes y terceros | Todo mensaje externo pasa por aquí |

Con una sola persona hay que ejercer los cuatro papeles, y justo por eso conviene escribir la bitácora **mientras** se actúa. *Digital Forensics and Incident Response* (Johansen, 2020), caps. 1 y 2, describe los modelos de equipo de respuesta (CSIRT), la escalación desde un centro de operaciones de seguridad y la sala de guerra.

## Triage: validar, categorizar, priorizar

Los pasos de RS.MA son un buen guion para la primera media hora:

1. **Validar:** una revisión preliminar para comprobar que ocurrió un incidente y estimar severidad y urgencia (RS.MA-02 R1). Existe también un canal para que terceros reporten, y esos reportes se toman en serio (RS.MA-02 R2).
2. **Categorizar:** por tipo, por ejemplo brecha de datos, ransomware, toma de cuenta o negación de servicio (RS.MA-03 R1).
3. **Priorizar** por alcance, impacto probable, urgencia y recursos disponibles (RS.MA-03 R2).
4. **Escalar o elevar:** escalar significa aumentar recursos o plazos; elevar, involucrar a un nivel superior de gestión (RS.MA-04 N1).

## Contener sin destruir la evidencia

Contener es evitar que el incidente se expanda; ayuda a prevenir más daño y a no desbordar los recursos, y la mayoría de los incidentes lo requieren (RS.MI-01 N1). Lo difícil es el **conflicto de intereses** entre dos objetivos legítimos:

- Reunir evidencia **volátil** antes de que desaparezca (memoria, conexiones, procesos).
- Cortar la actividad del atacante cuanto antes.

La publicación lo reconoce: elegir una estrategia exige equilibrar la necesidad de recuperarse rápido con la de observar al atacante o investigar a fondo, y "cada decisión de estrategia tiene concesiones" (RS.MA-03 R3 y N1). Una estrategia que observa al atacante puede dejarlo escalar su acceso o comprometer otros sistemas; si se piensa redirigirlo a un entorno aislado, primero se discute con el área legal (RS.MI, R1). Para decidir con rapidez conviene tener **criterios y procedimientos escritos**, que consideren el tipo de incidente y la duración de la medida de contención (de una solución de emergencia por horas a una definitiva).

Dos referencias de buenas prácticas de recolección, que vuelven en la nota de forense:

- RFC 3227: reunir primero lo más volátil, no apagar el equipo antes de terminar la recolección, no confiar en los programas del sistema, y recordar que cortar la red puede activar mecanismos que borran evidencia (IETF, 2002, secciones 2 y 2.2).
- Cada acción sobre el sistema cambia algo. Lo que importa es que sea **mínima, deliberada y registrada** (NIST, 2025, RS.AN-06).

Orden razonable para un servidor Linux comprometido, con una sesión activa del atacante, cuando la decisión del líder es contener rápido pero conservando lo volátil:

1. Anotar hora y estado, **sin tocar nada**.
2. Capturar conexiones y procesos (instantáneas de texto, segundos).
3. Capturar memoria si hay herramienta de confianza lista.
4. Aislar: bloquear la IP del atacante en el cortafuegos y terminar su sesión. Evitar apagar o reiniciar.
5. Verificar que el acceso se cortó.

## Erradicar es más que terminar la sesión

Cortar la conexión **no** es erradicar. La erradicación elimina los mecanismos de persistencia y los puntos de entrada: borrar malware, deshabilitar cuentas vulneradas, identificar y mitigar todas las vulnerabilidades explotadas (RS.MI-02 N1). La guía pide identificar **todos** los equipos y servicios afectados para remediar las fallas en todos (RS.MI-02 R1) y pide estimar y validar la magnitud buscando indicadores y señales de persistencia no solo en los activos que se sabe que se atacaron sino en otros posibles objetivos; hacerlo superficialmente puede subestimar el incidente y dejar que continúe "indefinidamente" en otros objetivos sin que la organización lo sepa (RS.AN-08 R1 y N1).

En un servidor Linux las persistencias típicas viven en pocos lugares, y revisarlos es un listado fijo: llaves en `authorized_keys`, tareas programadas (`cron`, timers de systemd), cuentas y grupos nuevos, servicios, y archivos modificados durante la ventana del incidente. La práctica de la sesión 3 recorre esa lista.

## Recuperar y declarar el fin

Recuperar es restaurar los sistemas a su operación normal, confirmar que funcionan y remediar vulnerabilidades. Incluye restaurar desde copias limpias, reconstruir sistemas, sustituir archivos por versiones limpias, instalar parches, cambiar contraseñas y reforzar controles; si el atacante es muy sofisticado y no se conoce todo su alcance, a veces hay que llegar a sustituir hardware (RC, N2). Las reglas de oro, todas de RC.RP:

- Verificar la **integridad de las copias** antes de restaurar: buscar indicadores de compromiso y corrupción (RC.RP-03).
- Restaurar los servicios esenciales en el orden adecuado y con los dueños del sistema (RC.RP-04).
- Comprobar los activos restaurados, y remediar las causas raíz **antes** de volver a producción (RC.RP-05).
- Declarar el fin con criterios y completar la documentación; preparar un informe posterior que documente el incidente, las acciones y las lecciones aprendidas (RC.RP-06).

## Comunicar

La publicación distingue cuatro tipos de comunicación en un incidente (RS.CO N1): **coordinación** entre las partes con roles; **notificación** formal a clientes, empleados, socios o reguladores; **comunicación pública** (medios) e **intercambio de información** de amenazas con otros. Las notificaciones deben cumplir las leyes y regulaciones aplicables, un tema que "evoluciona" con frecuencia (RS.CO-02 R3). Para el marco legal mexicano de datos personales, ver [`05-legislacion/12-proteccion-datos-personales.md`](../05-legislacion/12-proteccion-datos-personales.md); esta nota no lo repite.

Reglas prácticas de una comunicación sana:

- Un solo canal de mando y un solo escriba.
- Lo que se dice al exterior sale de **un** documento aprobado.
- Los datos sensibles de la respuesta (vulnerabilidades explotadas, usuarios implicados) se protegen: la bitácora de respuesta se resguarda y solo la ve personal autorizado (RS.AN-06 R1 y N2).

## F3EAD: la respuesta como ciclo de inteligencia

Brown y Roberts (2023, cap. 3) presentan **F3EAD** como modelo para unir operaciones e inteligencia: **Find, Fix, Finish, Exploit, Analyze, Disseminate**. Su origen es militar: Roberts (2015) lo atribuye a la colaboración entre fuerzas especiales y equipos de inteligencia durante las guerras de Afganistán e Irak. Aplicado a la respuesta a incidentes:

| Fase | Qué significa en una respuesta | En el laboratorio del curso |
|---|---|---|
| Find | Elegir el objetivo: qué investigar | Una alerta de acceso desde una IP nueva |
| Fix | Localizar al adversario en tu entorno | Qué sesión, qué usuario, qué procesos y qué conexiones |
| Finish | Ejecutar la acción que cumple el objetivo operativo: contener y erradicar | Bloquear la IP, cerrar la sesión, quitar persistencias |
| Exploit | Reunir y enriquecer todo lo que dejó el incidente | Bitácoras, memoria, archivos, tiempos |
| Analyze | Desarrollar esos datos para el siguiente ciclo | Qué más hizo ese actor y qué buscar en otros equipos |
| Disseminate | Compartir con quien necesita actuar | Informe al equipo, reglas nuevas para el SIEM |

Los capítulos 4 a 9 del libro dedican uno a cada fase. El capítulo 6 se titula "Finishing Is Not Hacking Back": terminar el incidente no significa atacar de vuelta, y en este curso **nunca** se actúa contra sistemas ajenos. F3EAD también cambia la pregunta de fondo: en vez de cerrar el caso al restaurar, se pregunta qué aprendimos del adversario que sirva para detectarlo antes la próxima vez.

## Errores comunes

- **Reiniciar "para limpiar".** Borra memoria, conexiones y procesos, justo lo que más cuesta recuperar (IETF, 2002, sección 2.2).
- **Confiar en los programas del sistema comprometido.** Un `ps` o un `ls` reemplazado miente. Hay que usar binarios de confianza desde medios externos.
- **Cambiar la contraseña y darlo por cerrado.** Si quedó una llave SSH, una tarea programada o un servicio, el atacante vuelve sin contraseña.
- **Declarar el fin sin criterio.** RC.RP-06 pide criterios de fin de recuperación y documentación completa.
- **Escribir la bitácora al final.** Se pierde el orden y la hora de cada acción, y esa precisión es lo que sostiene el informe.
- **Olvidar las zonas horarias.** Cada marca de tiempo debe indicar si es UTC o local, y la diferencia del reloj del sistema con UTC (IETF, 2002, sección 2).
- **Dejar la comunicación a todos.** Varios mensajes distintos al exterior son peores que ninguno.

## Lista mínima de respuesta (para imprimir)

1. Hora, quién reportó, qué se observó. Sin tocar el sistema.
2. Líder y escriba designados.
3. Validar y categorizar. Declarar incidente con criterio.
4. Instantánea de conexiones y procesos; memoria si es posible.
5. Contener (cortafuegos, terminar sesión); registrar cada comando.
6. Estimar magnitud: otros equipos, mismas credenciales.
7. Erradicar persistencia; rotar credenciales; parchear la causa raíz.
8. Restaurar y verificar; vigilar.
9. Declarar el fin; informe con línea de tiempo.
10. Lecciones aprendidas con dueño y fecha.

## Glosario

| Término | Definición |
|---|---|
| Evento | Ocurrencia observable que involucra activos de cómputo |
| Incidente de ciberseguridad | Ocurrencia que pone en peligro, real o inminentemente, la integridad, confidencialidad o disponibilidad de la información o de un sistema, o viola la ley o las políticas de seguridad |
| Incident handler | Persona que verifica un incidente, reúne y analiza datos, prioriza y limita el daño |
| Triage | Revisión preliminar para confirmar un incidente y estimar su severidad y urgencia |
| Escalar | Aumentar recursos o plazos de la respuesta |
| Elevar | Involucrar un nivel superior de gestión |
| Contención | Acciones que evitan la expansión de un incidente |
| Erradicación | Eliminar la persistencia y los puntos de entrada, y mitigar los efectos |
| Recuperación | Restaurar sistemas a su operación normal y verificar su integridad |
| Persistencia | Mecanismo que permite al atacante volver a entrar o seguir ejecutando código |
| Perfil comunitario CSF 2.0 | Conjunto de resultados del Marco de Ciberseguridad adaptado a un tema, como la respuesta a incidentes |
| F3EAD | Find, Fix, Finish, Exploit, Analyze, Disseminate: modelo que une operaciones e inteligencia |
| Informe posterior | Documento que describe el incidente, las acciones tomadas y las lecciones aprendidas |

## Referencias

- Brown, R. y Roberts, S. J. (2023). *Intelligence-driven incident response* (2.ª ed.). O'Reilly Media. Cap. 3 (*Basics of Incident Response*) y caps. 4 a 9 (Find, Fix, Finish, Exploit, Analyze, Disseminate).
- Internet Engineering Task Force. (2002). *Guidelines for evidence collection and archiving* (RFC 3227; BCP 55). https://www.rfc-editor.org/rfc/rfc3227
- Johansen, G. (2020). *Digital forensics and incident response* (2.ª ed.). Packt Publishing. Caps. 1 y 2.
- National Institute of Standards and Technology. (2025). *Incident response recommendations and considerations for cybersecurity risk management: A CSF 2.0 community profile* (NIST SP 800-61r3). https://doi.org/10.6028/NIST.SP.800-61r3
- Roberts, S. J. (2015, 24 de marzo). *Intelligence concepts: F3EAD*. https://papermod--sroberts-blog.netlify.app/blog/2015-03-24-intelligence-concepts-f3ead

---

*Rogelio Arriaga Gonzalez*
