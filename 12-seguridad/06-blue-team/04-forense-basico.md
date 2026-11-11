# 04 - Forense básico de un servidor Linux

> Lectura del curso [Defensa y respuesta a incidentes](../../cursos/blue-team/README.md), sesión 4.

## Por qué importa

Cuando un servidor se compromete hay una pregunta que decide si lo que reúnes sirve de algo: **"¿cómo sabes que no lo cambiaste?"**. Un archivo que apareció donde no debía, una conexión que quedó abierta o una hora en una bitácora valen lo que valga la forma en que se obtuvieron. Si el responsable apagó la máquina, montó el disco con escritura o copió con una herramienta que modifica las fechas de acceso, la evidencia se contaminó y el análisis pierde fuerza, aunque sus conclusiones sean correctas.

Esta nota es la versión **técnica** para sistemas Linux: en qué orden reunir, cómo copiar, cómo demostrar que la copia es fiel, cómo conservar quién la tuvo y cómo ordenar lo encontrado en una línea de tiempo. El marco **legal** (qué cuenta como indicio, cadena de custodia en México, dictamen) ya está desarrollado en las notas de la materia de forense y aquí solo se enlaza:

- [`03-forense/12-etica-cadena-custodia.md`](../03-forense/12-etica-cadena-custodia.md): ética del perito y cadena de custodia.
- [`03-forense/13-obtencion-tratamiento-evidencia.md`](../03-forense/13-obtencion-tratamiento-evidencia.md): obtención y tratamiento de la evidencia.
- [`03-forense/15-identificacion-fijacion-preservacion.md`](../03-forense/15-identificacion-fijacion-preservacion.md): identificación, fijación y preservación.
- [`03-forense/16-dictamen-pericial.md`](../03-forense/16-dictamen-pericial.md): el dictamen.

> **Escenario ilustrativo.** Los ejemplos retoman el incidente hipotético de la sesión 3: el usuario `soporte` entra a `victima` (192.168.56.20) desde `kali` (192.168.56.10) a las 02:31:17 UTC del 10 de noviembre de 2026 con una contraseña filtrada. Todo es ficticio.

## El proceso: cuatro fases

NIST SP 800-86 (Kent et al., 2006), una guía para integrar técnicas forenses a la respuesta a incidentes, describe el proceso en cuatro fases:

1. **Recolección** (collection): identificar, etiquetar, registrar y adquirir los datos de las fuentes posibles, siguiendo procedimientos que preserven su integridad. Se hace con prontitud por la probabilidad de perder datos dinámicos, como las conexiones de red actuales.
2. **Examen** (examination): procesar los datos recolectados con métodos automáticos y manuales para extraer lo de interés, preservando la integridad.
3. **Análisis** (analysis): interpretar los resultados del examen con métodos justificables para contestar las preguntas que motivaron el trabajo.
4. **Reporte** (reporting): informar los resultados, incluidas las acciones realizadas, por qué se eligieron las herramientas y qué mejoras recomienda.

La propia guía aclara que hay muchos modelos y que difieren en el nivel de detalle de cada fase y en los términos; el suyo es "una manera sencilla de verlas". Para la recolección, recomienda un proceso estándar de cuatro pasos: identificar las fuentes, preparar un plan que las **priorice** por valor probable, volatilidad y esfuerzo, adquirir los datos y **verificar su integridad** (Kent et al., 2006, sección 3.2). Esta nota sigue ese orden.

## Principios de recolección (RFC 3227)

El RFC 3227, "Guidelines for Evidence Collection and Archiving" (febrero de 2002, BCP 55), es corto y sigue siendo la referencia de los principios (IETF, 2002):

- Capturar una imagen lo más exacta posible del sistema y llevar **notas detalladas y fechadas**.
- Anotar si cada marca de tiempo está en **UTC o en hora local**, y la diferencia entre el reloj del sistema y UTC.
- **Minimizar los cambios** a los datos, incluidas las fechas de acceso de archivos y directorios.
- **Recolectar antes de analizar**.
- Trabajar de lo más volátil a lo menos volátil.
- Hacer una copia a nivel de bits del medio y analizar **una copia de la copia**.
- Procedimientos probados y automatizados cuando sea posible.

Y lo que debe evitarse (IETF, 2002, sección 2.2): no apagar antes de terminar de recolectar; no confiar en los programas del sistema; no ejecutar programas que modifican la fecha de acceso de todos los archivos, como `tar` o `xcopy`; y notar que simplemente desconectar el equipo de la red puede activar mecanismos que borran evidencia al detectar que están desconectados.

### Orden de volatilidad

La sección 2.1 del RFC lista las fuentes de la más volátil a la menos volátil:

| Orden | Fuente según RFC 3227 | En un servidor Linux |
|---|---|---|
| 1 | Registros, caché | Prácticamente inalcanzable en un incidente de rutina |
| 2 | Tabla de ruteo, caché ARP, tabla de procesos, estadísticas del kernel, **memoria** | `ss`, `ip neigh`, `ps`, `/proc`, volcado de RAM |
| 3 | Sistemas de archivos temporales | `/tmp`, `/run`, `/dev/shm` |
| 4 | **Disco** | Imagen del disco o del volumen |
| 5 | Registro y monitoreo remotos relevantes | El SIEM, el servidor de bitácoras |
| 6 | Configuración física, topología de red | Diagramas |
| 7 | Medios de archivo | Respaldos |

NIST SP 800-86 añade que el orden de adquisición debe considerar además el **valor probable** de cada fuente y el **esfuerzo** que cuesta, y que incluso lo "no volátil" puede ser dinámico, como las bitácoras que se sobrescriben.

## Qué se adquiere y cómo

*Digital Forensics and Incident Response* (Johansen, 2020) dedica su sección 2 a la adquisición: evidencia de red (cap. 4), evidencia del equipo (cap. 5) e imágenes forenses (cap. 6). *Digital Forensics with Kali Linux* (Parasram, 2023) cubre el mismo terreno con herramientas de Kali: la adquisición y el orden de volatilidad en el cap. 7, y las herramientas de adquisición (`dc3dd`, `dd`, Guymager) y los hashes de integridad en el cap. 8.

### Memoria

La memoria contiene procesos, conexiones y, a veces, credenciales y código que nunca llegó al disco. En Linux hay dos tipos de herramienta que ilustran el método:

- **LiME** (Linux Memory Extractor) es un módulo del kernel que se carga con `insmod`, con parámetros `path` y `format`; sus formatos son `lime`, `padded` y `raw`. Su ejemplo carga un módulo con el nombre del kernel en ejecución (`lime-$(uname -r).ko`); conviene tenerlo compilado y probado **antes** del incidente, no a las 3 de la mañana.
- **AVML** (Acquire Volatile Memory for Linux), de Microsoft, es un binario que se usa como `avml acquire salida.lime`; produce formato LiME y puede comprimir con `--compress`. Su documentación indica que no funciona si el kernel tiene activada la función `kernel_lockdown`.

Dos normas de método: el binario de adquisición debe venir **de un medio de confianza**, no del sistema comprometido, y la salida debe escribirse a un **medio externo**, no al disco que se va a analizar. Para analizar el volcado, Volatility 3 necesita una tabla de símbolos del kernel exacto: sus banners deben coincidir **exactamente** con los de la imagen y las tablas se generan con `dwarf2json` a partir de un kernel con símbolos de depuración (Volatility Foundation, s. f.). Sin esos símbolos puede ser imposible analizar el volcado.

### Red y sistema en vivo

Las conexiones y los procesos se capturan como texto, en segundos, antes de cualquier cambio. Un `ls -l /proc/<PID>/exe` que termina en `(deleted)` indica que el binario en ejecución fue borrado del disco; el manual de `proc` dice que el enlace "contendrá la cadena ' (deleted)' agregada" al nombre original (Linux man-pages project, s. f.). Mientras el proceso viva, el ejecutable se puede copiar desde `/proc/<PID>/exe`; si se mata el proceso, se pierde.

### Disco: la imagen forense

Una **imagen forense** es una copia bit a bit del medio, incluido el espacio sin asignar, que permite buscar archivos borrados. Se hace con herramientas que no interpretan el contenido: `dd` de GNU, `dc3dd` o Guymager (Parasram, 2023, cap. 8). Dos precauciones:

- **Bloqueo de escritura.** En medio físico se usa un bloqueador por hardware o software; en una imagen, se monta **solo lectura**. En ext4, solo lectura no basta: la documentación del kernel advierte que ext4 "reproducirá el journal (y por tanto escribirá en la partición) incluso cuando se monta de solo lectura", y la opción `noload` evita cargar el journal (Linux kernel documentation, s. f.). Se combinan `ro,noload`, y además `noexec,nosuid,nodev` para no ejecutar nada de la imagen (`mount(8)`).
- **Trabajar sobre una copia.** La original se sella y se guarda; el análisis ocurre sobre una copia de la copia.

### Integridad: la suma de verificación

La integridad se demuestra con una **función hash**: se calcula el resumen del original y de la copia y se comparan; si coinciden, la copia es idéntica (Kent et al., 2006, sección 3.2). La práctica usa SHA-256 con `sha256sum`. Tres reglas:

1. Calcular el hash **al adquirir**, antes de cualquier otra operación, y registrarlo.
2. Verificarlo cada vez que cambia de manos y antes de analizar (`sha256sum -c`).
3. Un hash distinto no significa necesariamente manipulación, pero significa que **ya no puede demostrarse** la fidelidad: hay que repetir desde la original.

El hash no demuestra autoría ni hora: demuestra que los bytes no cambiaron desde que se calculó. Quien calcula un hash tarde ha perdido la prueba de lo que ocurrió antes de calcularlo.

## Cadena de custodia

La cadena de custodia documenta quién tuvo la evidencia y cuándo. El RFC 3227 (sección 4.1) pide poder contestar: dónde, cuándo y por quién se descubrió y recolectó; dónde, cuándo y por quién se manejó o examinó; quién la tuvo bajo custodia, en qué periodo y cómo se almacenó; y cuando cambió de custodia, cuándo y cómo ocurrió la transferencia. NIST SP 800-86 agrega: llevar un registro de cada persona que tuvo custodia física, documentar las acciones y la hora, guardar la evidencia en un lugar seguro, examinar solo copias y verificar la integridad de la original y de la copia; y, si no está claro si hay que preservar, por omisión se preserva.

El formato concreto en México, con sus eslabones y la regulación aplicable, está en [`03-forense/12-etica-cadena-custodia.md`](../03-forense/12-etica-cadena-custodia.md). Para un equipo de respuesta, el formato debe caber en una hoja y llenarse **al momento**.

## La línea de tiempo

Una **línea de tiempo** ordena en una sola lista lo que dicen las distintas fuentes: bitácoras, fechas de archivos, historial, conexiones. Se construye en tres pasos:

1. **Normalizar el tiempo**: todo a UTC, con la diferencia de reloj anotada.
2. **Reunir marcas**: de cada archivo, las marcas de modificación (M), acceso (A), cambio de metadatos (C) y creación (B, si el sistema la guarda), que se resumen como *MACB*.
3. **Ordenar y correlacionar**: la entrada de bitácora a las 02:41:50 que registra un `sudo tee /etc/cron.d/sync-cache` y la modificación del archivo a las 02:41:50 son **dos fuentes que dicen lo mismo**, y eso es lo que da confianza.

The Sleuth Kit automatiza esto: `fls -r -m <punto> imagen` recorre el sistema de archivos y genera un archivo "body" en formato de tiempo, y `mactime -b body -d` lo convierte en una línea de tiempo con valores separados por coma (The Sleuth Kit, s. f.). La práctica construye una versión mínima, para entender qué hay debajo.

Cuidados con las marcas de tiempo:

- La fecha de **acceso** cambia por el simple hecho de leer, y con la opción de montaje por omisión `relatime` solo se actualiza si la anterior era anterior o igual a la de modificación o cambio, o tiene más de un día (`mount(8)`). Por eso copiar archivos **en vivo** altera evidencia, y por eso se trabaja sobre la imagen.
- `touch -d` cambia la fecha de modificación, pero la de cambio de metadatos (ctime) queda con la hora real en que se hizo el cambio: la práctica lo muestra con `debugfs`. Una discrepancia entre ambas es una pista, que se interpreta con cuidado porque copiar, extraer o montar archivos también crea marcas nuevas.
- Una línea de tiempo **sugiere**; no prueba. Las conclusiones se sostienen con más de una fuente.

## Artefactos comunes en Linux

| Artefacto | Qué cuenta | Cuidado |
|---|---|---|
| `/var/log/auth.log` o el journal (`journalctl -u ssh`) | Inicios de sesión, `sudo`, cambios de cuenta | Un atacante con privilegios puede editarlos; compararlos con el SIEM |
| `wtmp` y `last` | Registro de inicios y cierres de sesión | `last` lee el archivo wtmp y `-f` permite apuntar a una copia (man `last`) |
| `~/.ssh/authorized_keys` | Llaves que dan entrada sin contraseña | Su fecha de modificación fecha la persistencia |
| `/etc/cron.d`, `crontab`, timers | Ejecución programada | Persistencia clásica |
| `/tmp`, `/var/tmp`, directorios ocultos | Herramientas y datos preparados para exfiltrar | Buscar archivos creados dentro de la ventana del incidente |
| `.bash_history` | Comandos escritos | Bash guarda el historial en el archivo cuando el shell termina con normalidad (manual de Bash); si la sesión se mató, es posible que falte, y su ausencia también es un dato |

## Errores comunes

- **Calcular el hash después de analizar.** Prueba la copia que quedó, no la que se adquirió.
- **Montar la imagen con escritura**, o solo con `ro` en ext4 sin `noload`.
- **Escribir el volcado de memoria en el mismo disco** que se va a imagen.
- **Usar `tar` o `cp -a` en vivo** para "respaldar rápido": cambian las fechas de acceso y no son una imagen.
- **Mezclar zonas horarias.** Una línea de tiempo con horas locales y UTC revueltas es inservible.
- **No anotar los comandos propios.** Los rastros del responsable aparecen en la línea de tiempo; si no están registrados, parecen del atacante.
- **Confundir examen con análisis.** Listar archivos no es concluir qué pasó.

## Glosario

| Término | Definición |
|---|---|
| Adquisición | Copiar datos de una fuente de evidencia con un método que preserva su integridad |
| Bloqueador de escritura | Dispositivo o configuración que impide escribir en el medio original |
| Cadena de custodia | Registro de quién tuvo la evidencia, cuándo y con qué acciones |
| Hash | Resumen de longitud fija calculado sobre unos datos; si los datos cambian, el resumen cambia |
| Imagen forense | Copia bit a bit de un medio de almacenamiento |
| `kernel_lockdown` | Función del kernel que, activada, impide a AVML adquirir la memoria |
| LiME | Módulo del kernel de Linux para capturar la memoria |
| MACB | Marcas de modificación, acceso, cambio de metadatos y creación de un archivo |
| Orden de volatilidad | Orden de recolección de lo que se pierde más rápido a lo que dura más |
| `relatime` | Opción de montaje que actualiza la fecha de acceso solo bajo ciertas condiciones |
| Tabla de símbolos | Archivo que describe las estructuras del kernel para que Volatility interprete un volcado |

## Referencias

- The Sleuth Kit. (s. f.). *mactime* y *fls* [Páginas de manual]. https://www.sleuthkit.org/sleuthkit/man/mactime.html y https://www.sleuthkit.org/sleuthkit/man/fls.html
- Internet Engineering Task Force. (2002). *Guidelines for evidence collection and archiving* (RFC 3227; BCP 55). https://www.rfc-editor.org/rfc/rfc3227
- Johansen, G. (2020). *Digital forensics and incident response* (2.ª ed.). Packt Publishing. Caps. 4 a 6 (sección 2) y caps. 7 a 10 (sección 3).
- Kent, K., Chevalier, S., Grance, T. y Dang, H. (2006). *Guide to integrating forensic techniques into incident response* (NIST SP 800-86). https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-86.pdf
- Linux man-pages project. (s. f.). *proc_pid_exe(5)* [Página de manual]. https://man7.org/linux/man-pages/man5/proc_pid_exe.5.html
- Linux kernel documentation. (s. f.). *ext4 general information*. https://docs.kernel.org/admin-guide/ext4.html
- Microsoft. (s. f.). *AVML: Acquire volatile memory for Linux* [Repositorio]. https://github.com/microsoft/avml
- Parasram, S. V. N. (2023). *Digital forensics with Kali Linux* (3.ª ed.). Packt Publishing. Caps. 7 y 8.
- 504ensics Labs. (s. f.). *LiME: Linux memory extractor* [Repositorio]. https://github.com/504ensicsLabs/LiME
- Volatility Foundation. (s. f.). *Volatility 3: Symbol tables*. https://volatility3.readthedocs.io/en/latest/symbol-tables.html
- Free Software Foundation. (s. f.). *Bash history facilities* [Manual de referencia de GNU Bash]. https://www.gnu.org/software/bash/manual/html_node/Bash-History-Facilities.html
- Linux man-pages project. (s. f.). `last(1)` y `mount(8)` [Páginas de manual]. https://man7.org/linux/man-pages/man1/last.1.html y https://man7.org/linux/man-pages/man8/mount.8.html

---

*Rogelio Arriaga Gonzalez*
