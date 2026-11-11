# 05 - Inteligencia de amenazas, MITRE ATT&CK y cacería

> Lectura del curso [Defensa y respuesta a incidentes](../../cursos/blue-team/README.md), sesión 5.

## Por qué importa

Una alerta responde a algo que alguien ya supo describir: una regla, una firma, un umbral. Un adversario que no se parece a ninguna regla pasa de largo, y su rastro se queda en las bitácoras sin que nadie lo mire. La cacería de amenazas (threat hunting) es el trabajo contrario: en lugar de esperar a que suene algo, se parte de una **hipótesis sobre cómo actuaría un adversario** en este entorno y se busca en los datos si dejó huella.

Para que esa hipótesis no salga de la nada necesita dos materias primas. La primera es **inteligencia de amenazas**: saber qué hacen los adversarios que de verdad ataquen a organizaciones como la tuya. La segunda es un **lenguaje común** para nombrar lo que hacen, y ese lenguaje es MITRE ATT&CK. Esta nota explica ambas, cómo se convierten en una cacería documentada y por qué una cacería que no encuentra nada también es un resultado.

El procedimiento en el laboratorio, con tres técnicas emuladas, está en la práctica de la sesión 5. Aquí está el marco.

## Inteligencia de amenazas: información que sirve para decidir

El NIST define la **información sobre ciberamenazas** como cualquier información que ayude a una organización a identificar, evaluar, monitorear y responder a ellas. En esa definición caben los indicadores de compromiso, las tácticas, técnicas y procedimientos (TTP), las acciones sugeridas para detectar o prevenir ataques y los hallazgos del análisis de incidentes (NIST SP 800-150, 2016).

Un dato suelto no es inteligencia. Un hash, una IP o un dominio son **datos**; se vuelven inteligencia cuando alguien los analiza en un contexto, con una fuente conocida, una fecha de recolección y un nivel de confianza, y los entrega a quien decide con una recomendación utilizable. Por eso los libros del curso insisten en el **ciclo de inteligencia**: se pregunta algo concreto, se recolecta, se procesa, se analiza, se entrega y se recibe retroalimentación para preguntar mejor la próxima vez. Palacín (2021, cap. 1) y Brown y Roberts (2023, cap. 2) lo desarrollan con sus propias etapas.

La inteligencia se consume en tres niveles, que los dos libros nombran igual:

| Nivel | Pregunta que contesta | Quién la usa |
|---|---|---|
| Estratégico | ¿Qué adversarios y qué riesgos importan a este negocio? | Dirección |
| Operativo | ¿Cómo y cuándo es probable que me ataquen? ¿Qué campañas hay? | Jefes de seguridad y de respuesta |
| Táctico | ¿Qué técnicas, herramientas y artefactos usan? | Analistas, cazadores, ingenieros de detección |

La cacería vive sobre todo en el nivel táctico, alimentada por el operativo.

## La Pirámide del Dolor: qué indicadores valen la pena

No todos los indicadores cuestan lo mismo al adversario cuando se los bloqueas. David Bianco (2013) los ordenó en una pirámide, de abajo hacia arriba:

| Nivel | Qué es | Qué le cuesta al adversario que lo detectes |
|---|---|---|
| Valores hash | Huellas de archivos concretos | Casi nada: cualquier cambio al archivo cambia su hash |
| Direcciones IP | Redes que usa | Poco: cambia de dirección |
| Nombres de dominio | Dominios y subdominios | Algo: debe registrarlos y alojarlos |
| Artefactos de red y de host | Rastros característicos: cadenas de User-Agent, llaves de registro | Tiene que cambiar sus herramientas o su configuración |
| Herramientas | Software que trae consigo | Reto: debe encontrar o fabricar reemplazos |
| TTP | Su comportamiento: qué hace y cómo | Dolor: debe aprender conductas nuevas |

La consecuencia práctica para un equipo de defensa: una lista de hashes envejece en días; una detección escrita sobre **comportamiento** (alguien crea una cuenta local y luego programa una tarea) sobrevive a que el adversario cambie de herramientas. ATT&CK existe para nombrar esa capa alta.

## MITRE ATT&CK

ATT&CK es, en palabras de MITRE, una base de conocimiento de tácticas y técnicas adversarias basada en observaciones del mundo real, organizada por **tácticas** (el porqué: persistir, escalar privilegios, moverse), **técnicas** (el cómo) y **subtécnicas** (el cómo con más detalle). Cada técnica tiene un identificador (T1136), cada subtécnica el suyo (T1136.001) y una página con descripción, plataformas, mitigaciones y, desde 2025, estrategias de detección. A noviembre de 2026 la versión vigente es la **v20.1**, publicada el 13 de octubre de 2026 (MITRE, s. f.-a).

Dos cosas del modelo que conviene saber:

- **Matrices por dominio.** Enterprise, Mobile e ICS. A noviembre de 2026 la matriz Enterprise tiene 15 tácticas (MITRE, s. f.-b).
- **Procedimientos.** Cada técnica lista ejemplos de cómo la usaron grupos y campañas concretos. Esos ejemplos son la entrada de una emulación realista.

ATT&CK no es una lista de verificación para "cubrir todas las casillas". Es un mapa: sirve para decir qué técnicas te preocupan, cuáles puedes ver con los datos que tienes y cuáles son puntos ciegos. El libro de Selmanaj (2024, caps. 3 a 6) recorre el marco y sus herramientas, y el de Palacín (2021, cap. 4) lo usa para mapear al adversario.

### Cómo cambió ATT&CK en 2025 y 2026

Los libros del curso se escribieron antes de estos cambios; lo que sigue está tomado de las notas de versión de MITRE:

- **v18 (octubre de 2025):** los campos de "Detección" de cada técnica se reemplazaron por **estrategias de detección** (identificadores DET...), que agrupan **analíticas** (AN...) específicas por plataforma. Las fuentes de datos (Data Sources) quedaron deprecadas en favor de los **componentes de datos** (MITRE, 2025).
- **v19 (abril de 2026):** la táctica Defense Evasion se dividió en dos: **Stealth** (TA0005) y **Defense Impairment** (TA0112) (MITRE, 2026a).
- **v20 (octubre de 2026):** agrega técnicas relacionadas con IA y nuevas campañas; la matriz Enterprise sigue en 15 tácticas (MITRE, 2026b).

Si un libro o una presentación antigua dice "14 tácticas" o "fuentes de datos", lo que ves ahí es la versión de su época. Cuando cites ATT&CK, escribe la versión.

### Herramientas

- **ATT&CK Navigator:** aplicación web para ver y anotar las matrices con capas (colorear técnicas, comentar, asignar valores). Corre en el navegador y también se puede alojar localmente (MITRE, s. f.-c).
- **Datos en STIX y servidor TAXII:** el conocimiento completo se publica en formato legible por máquinas, y la biblioteca `mitreattack-python` lo consulta desde código (MITRE, s. f.-d).

## Emulación: crear el comportamiento para probar si lo ves

La mejor forma de saber si tu detección funciona es provocar el comportamiento que debería detectar. A eso se le llama **emulación de adversario**: reproducir, con autorización y en un entorno controlado, técnicas de ATT&CK. Selmanaj (2024, cap. 1) distingue la emulación de la prueba de penetración y del equipo rojo; Palacín (2021, cap. 6) presenta Atomic Red Team, Caldera y otros.

**Atomic Red Team** (Red Canary) es una biblioteca de pruebas pequeñas, cada una asociada a una técnica de ATT&CK, con su comando y su limpieza. Por ejemplo, la técnica T1136.001 tiene la prueba "Create a user account on a Linux system", que ejecuta `useradd -M -N -r -s /bin/bash -c evil_account <usuario>` y se limpia con `userdel` (Red Canary, s. f.-a). El módulo `Invoke-AtomicRedTeam` las ejecuta con PowerShell, también en Linux con `pwsh` (Red Canary, s. f.-b).

Reglas de oro: solo en laboratorio propio, con la autorización escrita del curso de Ciberseguridad, con limpieza al final y con un registro de qué se ejecutó y a qué hora. Sin esa hora, no hay forma de saber si lo que ves en las bitácoras es la emulación o un adversario de verdad.

## La cacería, paso a paso

El marco **PEAK** (Prepare, Execute, Act with Knowledge) de Bianco y Fetterman (2023) ordena una cacería en tres fases: preparar (escoger el tema e investigar), ejecutar (analizar los datos) y actuar con lo aprendido (documentar, automatizar lo que se pueda y comunicar). Distingue tres tipos de cacería: **basada en hipótesis**, **de línea base** (definir lo normal y buscar desviaciones) y **asistida por modelos**. Esta sesión trabaja la primera.

Una hipótesis útil se puede refutar con datos. Se escribe con cinco partes:

1. **Hipótesis:** "Un adversario con acceso a una cuenta de soporte creó una cuenta local para volver después."
2. **Técnica ATT&CK:** T1136.001, Create Account: Local Account.
3. **Dato que lo mostraría:** el registro de `useradd` en la bitácora de autenticación.
4. **Consulta:** la búsqueda concreta, con su ventana de tiempo.
5. **Resultado y lectura:** qué apareció, qué se descartó como legítimo y, si no apareció nada, **qué significa**.

### Tres técnicas de persistencia en Linux

| Técnica | ID | Rastro típico | Dónde se ve |
|---|---|---|---|
| Create Account: Local Account | T1136.001 | `new user: name=...` de `useradd` | `/var/log/auth.log` o el journal |
| Scheduled Task/Job: Cron | T1053.003 | `crontab[pid]: (usuario) BEGIN EDIT`, `REPLACE`; ejecuciones `CRON[pid]: (usuario) CMD (...)` | `/var/log/syslog` o el journal (en Ubuntu, el servicio cron escribe ahí); los archivos soltados en `/etc/cron.d` **no** dejan línea de edición |
| Account Manipulation: SSH Authorized Keys | T1098.004 | El cambio del archivo `authorized_keys` **no** deja línea de bitácora por sí mismo; sí el login posterior con una huella nueva | `auth.log` (login) y, para el cambio, auditd o monitoreo de integridad de archivos |

Los nombres, identificadores y plataformas son los de las páginas de ATT&CK consultadas en noviembre de 2026 (MITRE, s. f.-e, s. f.-f, s. f.-g). La última fila muestra el límite principal de la cacería: **sin el dato, la pregunta no se puede contestar**. Una consulta que regresa vacío porque la fuente nunca registró el evento no demuestra que el evento no ocurrió; demuestra un punto ciego, y ese es un hallazgo que va al reporte.

### Qué hacer con lo que sale

- **Algo malicioso:** es un incidente. Se pasa a respuesta ([nota 03](03-respuesta-a-incidentes.md)) y se conserva la evidencia ([nota 04](04-forense-basico.md)).
- **Algo legítimo pero inesperado:** se documenta y se agrega a la línea base para que la próxima vez no cueste una hora.
- **Nada:** se documenta igual, con la consulta, la ventana de tiempo y las fuentes que se revisaron. Eso permite repetirla y mostrar qué cubre el equipo.
- **Un hueco de datos:** se levanta como mejora (por ejemplo, "registrar cambios en `authorized_keys`").
- **Una consulta buena:** se convierte en regla de detección, con su prueba de disparo ([nota 02](02-bitacoras-y-deteccion.md)).

Cada hallazgo de la cacería alimenta el ciclo de inteligencia: lo que aprendiste del adversario en tu entorno es el mejor insumo para la siguiente hipótesis.

## Actualización al 2026

- ATT&CK v20.1 (13 de octubre de 2026): 15 tácticas Enterprise; Defense Evasion ya no existe como táctica única (Stealth y Defense Impairment desde la v19); las estrategias de detección y las analíticas reemplazan los campos de detección de las técnicas (desde la v18).
- Los libros citados en el curso describen versiones anteriores del marco. Los conceptos se mantienen; los nombres de tácticas y la sección de detección cambiaron.

## Glosario

| Término | Definición |
|---------|-----------|
| Ciclo de inteligencia | Proceso en que se plantea una pregunta, se recolecta, procesa y analiza información y se entrega a quien decide, con retroalimentación para la siguiente vuelta |
| TTP | Tácticas, técnicas y procedimientos: el comportamiento de un adversario |
| Indicador de compromiso (IoC) | Rastro observable de una intrusión: un hash, una IP, un dominio, un artefacto |
| Pirámide del Dolor | Modelo de Bianco que ordena los indicadores por lo que cuesta al adversario cambiarlos |
| ATT&CK | Base de conocimiento de MITRE sobre comportamiento adversario, organizada en tácticas, técnicas y subtécnicas |
| Táctica | El objetivo táctico de una técnica (el porqué), por ejemplo Persistence |
| Estrategia de detección | En ATT&CK desde la v18, enfoque de alto nivel para detectar una técnica; agrupa analíticas por plataforma |
| Analítica | En ATT&CK, descripción de qué buscar y en qué datos para una plataforma concreta |
| Navigator | Aplicación web de MITRE para ver y anotar matrices de ATT&CK con capas |
| Emulación de adversario | Reproducir técnicas de ATT&CK en un entorno controlado y autorizado para probar la detección |
| Atomic Red Team | Biblioteca de pruebas pequeñas por técnica de ATT&CK, con comando y limpieza |
| Cacería de amenazas | Búsqueda proactiva en los datos a partir de una hipótesis sobre el adversario |
| PEAK | Marco de cacería: Prepare, Execute, Act with Knowledge |
| Línea base | Descripción de lo normal en el entorno, contra la que se miden las desviaciones |
| Punto ciego | Técnica que el equipo no puede ver porque la fuente de datos no existe o no se recolecta |

## Referencias

- Bianco, D. J. (2013, 1 de marzo). *The pyramid of pain* [Entrada de blog, actualizada el 17 de enero de 2014]. Enterprise Detection & Response. https://detect-respond.blogspot.com/2013/03/the-pyramid-of-pain.html
- Bianco, D. J. y Fetterman, R. (2023, 18 de abril). *The PEAK threat hunting framework*. Splunk (SURGe). https://www.splunk.com/en_us/blog/security/peak-threat-hunting-framework.html
- Brown, R. y Roberts, S. J. (2023). *Intelligence-driven incident response* (2.ª ed.). O'Reilly Media.
- MITRE. (2025). *ATT&CK v18: updates, octubre de 2025*. https://attack.mitre.org/resources/updates/updates-october-2025/
- MITRE. (2026a). *ATT&CK v19: updates, abril de 2026*. https://attack.mitre.org/resources/updates/updates-april-2026/
- MITRE. (2026b). *ATT&CK v20: updates, octubre de 2026*. https://attack.mitre.org/resources/updates/updates-october-2026/
- MITRE. (s. f.-a). *ATT&CK versions*. https://attack.mitre.org/resources/versions/ (consultado en noviembre de 2026)
- MITRE. (s. f.-b). *Enterprise matrix*. https://attack.mitre.org/matrices/enterprise/ (consultado en noviembre de 2026)
- MITRE. (s. f.-c). *ATT&CK Navigator*. https://github.com/mitre-attack/attack-navigator
- MITRE. (s. f.-d). *ATT&CK data and tools*. https://attack.mitre.org/resources/attack-data-and-tools/
- MITRE. (s. f.-e). *Create account: Local account, T1136.001*. https://attack.mitre.org/techniques/T1136/001/ (consultado en noviembre de 2026)
- MITRE. (s. f.-f). *Scheduled task/job: Cron, T1053.003*. https://attack.mitre.org/techniques/T1053/003/ (consultado en noviembre de 2026)
- MITRE. (s. f.-g). *Account manipulation: SSH authorized keys, T1098.004*. https://attack.mitre.org/techniques/T1098/004/ (consultado en noviembre de 2026)
- National Institute of Standards and Technology. (2016). *Guide to cyber threat information sharing* (SP 800-150). https://csrc.nist.gov/pubs/sp/800/150/final
- Palacín, V. (2021). *Practical threat intelligence and data-driven threat hunting*. Packt Publishing.
- Red Canary. (s. f.-a). *Atomic Red Team: T1136.001 Create account: Local account*. https://github.com/redcanaryco/atomic-red-team/blob/master/atomics/T1136.001/T1136.001.md
- Red Canary. (s. f.-b). *Invoke-AtomicRedTeam: Execute atomic tests (local)*. https://github.com/redcanaryco/invoke-atomicredteam/wiki/Execute-Atomic-Tests-(Local)
- Selmanaj, D. (2024). *Adversary emulation with MITRE ATT&CK*. O'Reilly Media.

---

*Rogelio Arriaga Gonzalez*
