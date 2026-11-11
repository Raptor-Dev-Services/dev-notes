<!-- _class: lead -->

# S04 - Evidencia y análisis forense

### Defensa y respuesta a incidentes

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Esta sesión retoma el incidente de la S03: mismas máquinas, mismo usuario, mismas horas de referencia. Quien no terminó la S03 puede seguir: la práctica trae un generador que arma la imagen del incidente sin necesitar las VM.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, el incidente de la S03 | 25 min |
| Parte 1 | Principios y orden de volatilidad | 40 min |
| Parte 2 | Qué se adquiere: red, memoria y disco | 55 min |
| Parte 3 | Integridad: la suma de verificación | 35 min |
| Parte 4 | Cadena de custodia | 25 min |
| Parte 5 | La línea de tiempo | 35 min |
| Parte 6 | Práctica: adquirir y analizar | 70 min |
| Cierre | Errores típicos, tarea y quiz | 15 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. -->

---

## El lema de la sesión

> Si no se puede demostrar quién tocó la evidencia, no sirve como evidencia.

- Tener razón no basta: hay que poder **demostrarlo**.
- Una conclusión correcta, obtenida con una evidencia contaminada, se cae en la primera pregunta.
- Hoy aprendemos a reunir evidencia que resiste una sola pregunta: **"¿cómo sabes que no la cambiaste?"**

<!-- Pregunta al grupo: ¿qué habría pasado con la S03 si el responsable hubiera copiado archivos con tar? Se contesta durante la sesión. -->

---

## Al terminar puedes

- Ordenar las fuentes de evidencia de un servidor por volatilidad y decidir cuál va primero.
- Adquirir una imagen de un disco y comprobar con una suma de verificación que es fiel.
- Explicar qué cambia en una imagen cuando se monta o se repara sin precauciones.
- Llenar una cadena de custodia que cubra cada traspaso.
- Construir una línea de tiempo en UTC a partir de bitácoras y fechas de archivos.
- Separar lo que la evidencia **demuestra** de lo que solo **sugiere**.

---

## Dónde está el marco legal

Esta sesión es la parte **técnica**. El marco legal ya está en las notas de la materia de Informática forense:

| Tema | Nota |
|---|---|
| Ética del perito y cadena de custodia | `12-seguridad/03-forense/12-etica-cadena-custodia.md` |
| Obtención y tratamiento de la evidencia | `12-seguridad/03-forense/13-obtencion-tratamiento-evidencia.md` |
| Identificación, fijación y preservación | `12-seguridad/03-forense/15-identificacion-fijacion-preservacion.md` |
| El dictamen | `12-seguridad/03-forense/16-dictamen-pericial.md` |

> **Marco legal y ético.** Todo se hace sobre **tu** laboratorio, con la carta de autorización del curso de Ciberseguridad. Nunca sobre un equipo que no sea tuyo.

---

## El incidente de la S03 (Caso hipotético)

| Dato | Valor |
|---|---|
| Equipo | `victima`, Ubuntu Server 24.04 LTS, 192.168.56.20 |
| Acceso | `soporte` desde 192.168.56.10 (kali), contraseña filtrada |
| Primer acceso | 2026-11-10 02:31:17 UTC |
| Persistencias | llave en `authorized_keys`; tarea en `/etc/cron.d/sync-cache` |
| Datos preparados | `/var/tmp/.cache/datos.tgz` |
| Detección | 02:58 UTC (el responsable entra a las 03:02) |
| Contención | 03:15 UTC: bloqueo de IP y sesión terminada |

Todo es ficticio. Hoy se reúne la evidencia de ese equipo y se contesta: ¿qué pasó, en qué orden, y cómo lo demostramos?

---

<!-- _class: lead -->

# Parte 1
## Principios y orden de volatilidad

---

## El proceso: cuatro fases

NIST SP 800-86 (Kent et al., 2006), *Guide to Integrating Forensic Techniques into Incident Response*:

<!-- _pasos: filas -->

| Fase | Qué se hace |
|---|---|
| **Recolección** | Identificar, etiquetar, registrar y adquirir los datos, preservando su integridad |
| **Examen** | Procesar lo recolectado, automática y manualmente, para extraer lo de interés |
| **Análisis** | Interpretar los resultados con métodos justificables para contestar las preguntas |
| **Reporte** | Informar los resultados y lo que se hizo, con recomendaciones |

La propia guía aclara que hay muchos modelos y que difieren en el nivel de detalle y los términos.

---

## Principios del RFC 3227

*Guidelines for Evidence Collection and Archiving*, febrero de 2002 (BCP 55):

- Capturar una imagen **lo más exacta posible** del sistema.
- Llevar **notas detalladas y fechadas**.
- Anotar si cada hora es **UTC o local** y la diferencia del reloj del sistema con UTC.
- **Minimizar los cambios** a los datos, incluidas las fechas de acceso.
- **Recolectar primero, analizar después.**
- Trabajar de lo más volátil a lo menos volátil.
- Copia a nivel de bits del medio, y análisis sobre **una copia de la copia**.

Es un documento corto, de 2002, que sigue siendo la base de lo que sigue.

---

## Lo que el RFC pide evitar

| Evitar | Por qué |
|---|---|
| Apagar antes de terminar la recolección | Se pierde lo volátil |
| Confiar en los programas del sistema | Pueden haber sido reemplazados |
| Programas que cambian la fecha de acceso de todo (`tar`, `xcopy`) | Alteran la evidencia |
| Desconectar de la red sin pensar | Puede activar mecanismos que borran evidencia |

Fuente: RFC 3227, sección 2.2.

<!-- Recuerda la S03: reiniciar, el error de la ronda 1. Aquí aparece la regla escrita. -->

---

## Orden de volatilidad

<!-- _pasos: filas -->

| # | Fuente (RFC 3227, sección 2.1) |
|---|---|
| 1 | Registros, caché |
| 2 | Tabla de ruteo, caché ARP, tabla de procesos, estadísticas del kernel, **memoria** |
| 3 | Sistemas de archivos temporales |
| 4 | **Disco** |
| 5 | Registro y monitoreo remotos relevantes |
| 6 | Configuración física, topología de red |
| 7 | Medios de archivo |

De arriba abajo: lo que se pierde más rápido, primero.

---

## En nuestro servidor

| Nivel | Qué hay en `victima` |
|---|---|
| Memoria | La tabla de procesos, las conexiones, el servicio de 8081 que nunca estuvo en disco |
| Temporales | `/tmp`, `/run`, `/dev/shm` |
| Disco | Las persistencias, el `.tgz`, `auth.log`, las fechas de archivos |
| Remoto | El SIEM (`siem`, 192.168.56.30) y cualquier bitácora enviada fuera |
| Archivo | Respaldos |

Una razón más para enviar las bitácoras a otro equipo: un atacante con privilegios puede editar las locales. Lo que ya se envió, no.

---

## No solo lo volátil: valor y esfuerzo

NIST SP 800-86, sección 3.2: al priorizar se consideran tres factores.

| Factor | Pregunta |
|---|---|
| **Valor probable** | ¿Cuánto es probable que aporte esta fuente? |
| **Volatilidad** | ¿Se pierde si no la tomo ahora? |
| **Esfuerzo** | ¿Cuánto cuesta obtenerla, en tiempo y en dinero? |

Incluso lo "no volátil" puede cambiar: las bitácoras que se sobrescriben con los nuevos eventos.

Se recomienda tener planes escritos de antemano: priorizar a las 3 de la mañana es mala idea.

---

## Error típico: copiar en vivo con `tar`

Leer un archivo puede cambiar su **fecha de acceso**. Prueba hecha al preparar la clase, en un sistema de archivos con `relatime` (la opción por omisión):

```
antes:       atime=2026-11-10 02:00:00  mtime=2026-11-10 02:00:00
despues cat: atime=2026-11-11 13:06:36  mtime=2026-11-10 02:00:00
```

- `relatime` actualiza la fecha de acceso si la anterior es **anterior o igual** a la de modificación, o tiene más de un día (`mount(8)`).
- Un `tar` o un `cp -a` "rápidos" **reescriben** esas horas: destruyen una pista sobre qué leyó el atacante.

Por eso se copia la **imagen**, no archivo por archivo, y se analiza una copia de la copia.

---

<!-- _class: lead -->

# Parte 2
## Qué se adquiere

---

## Tres fuentes, tres métodos

| Fuente | Qué contiene | Cómo se adquiere |
|---|---|---|
| **Red** | Conexiones, tráfico, bitácoras de firewall y proxy | Capturas, bitácoras de equipos intermedios |
| **Memoria** | Procesos, conexiones, claves, código que no llegó al disco | Volcado de RAM con una herramienta de confianza |
| **Disco** | Archivos, borrados, fechas, bitácoras | Imagen forense, bit a bit |

*Digital Forensics and Incident Response* (Johansen, 2020), sección 2: red (cap. 4), equipo (cap. 5) e imágenes forenses (cap. 6).
*Digital Forensics with Kali Linux* (Parasram, 2023): adquisición y orden de volatilidad (cap. 7) y herramientas de adquisición (cap. 8).

---

## Memoria: dos herramientas

**LiME** (Linux Memory Extractor): un **módulo del kernel**, que se carga con `insmod`.

```bash {1|2|*}
# Fragmento: procedimiento segun el README de LiME, no ejecutado al preparar este material
insmod ./lime-$(uname -r).ko "path=/mnt/evidencia/victima-mem.lime format=lime"
```

- `path` y `format` son los parámetros; `format` puede ser `lime`, `padded` o `raw`.
- El nombre del módulo lleva el kernel en ejecución: **hay que tenerlo compilado y probado antes**.

**AVML** (Microsoft): un binario independiente.

```bash
# Fragmento: procedimiento segun el README de AVML, no ejecutado al preparar este material
avml acquire /mnt/evidencia/victima-mem.lime
```

Produce formato LiME, y no funciona si el kernel tiene activada la función `kernel_lockdown`.

---

## Reglas de método para la memoria

| Regla | Por qué |
|---|---|
| El binario viene **de un medio de confianza** | Los programas del sistema pueden estar reemplazados (RFC 3227) |
| La salida va a un **disco externo** | Escribir en el disco que vas a analizar lo contamina |
| Se anota la hora y el hash del archivo resultante | Para poder demostrarlo después |
| Se prepara **antes** del incidente | A las 3 de la mañana no se compila un módulo |

> En la S03 el responsable corrió `avml` a las 03:06:10 (aparece en el `auth.log`) hacia `/mnt/evidencia`: un disco aparte, no el de `victima`.

---

## Analizar la memoria: Volatility 3

La documentación de Volatility 3 dice que, para Linux, necesita una **tabla de símbolos** del kernel exacto:

- Los **banners** del archivo de símbolos deben coincidir **exactamente** con los de la imagen.
- Las tablas se generan con `dwarf2json` desde un kernel con símbolos de depuración.
- La mayoría de las distribuciones entregan el kernel sin esos símbolos: se instalan aparte.
- El plugin `banners` extrae el banner de la imagen.

Sin la tabla correcta puede ser imposible analizar el volcado.

> Honesto con el alcance: en la práctica de hoy **no** analizamos un volcado de memoria. Se documenta el procedimiento, y analizas el disco.

<!-- Parasram cap. 10 trata Volatility 3 con muestras. Es lectura recomendada, no práctica obligatoria. -->

---

## Red: lo que se reúne y dónde

*Digital Forensics and Incident Response* (Johansen, 2020), cap. 4: la evidencia de red depende de **estar preparada**.

| Fuente | Qué aporta |
|---|---|
| Bitácoras de firewall y proxy | Quién se conectó a dónde |
| Capturas de paquetes (`tcpdump`, Wireshark) | Contenido y tiempos de las conexiones |
| Bitácoras de servicios | `sshd`, `nginx`, `sudo` |

En nuestro laboratorio, la evidencia de red es la bitácora de SSH y la tabla `ss` que se capturó en la S03. Si hubiera una captura de paquetes previa, mostraría los intentos fallidos de 02:30:41.

---

## Disco: la imagen forense

Una **imagen forense** es una copia bit a bit del medio, incluido el espacio sin asignar: ahí viven los archivos borrados.

| Herramienta | Nota |
|---|---|
| `dd` (GNU) | Copia bloques sin interpretarlos |
| `dc3dd` | Herramienta de adquisición que Parasram (cap. 8) trata junto a `dd` |
| Guymager | Otra herramienta de adquisición del cap. 8 de Parasram |
| `VBoxManage clonemedium` | Copia el disco de una VM de VirtualBox, con `--format RAW` |

Dos precauciones: **bloqueo de escritura** y **trabajar sobre una copia**.

---

## Imagen de una VM de VirtualBox

```bash {1-2|3-4|5|*}
# Fragmento: procedimiento segun el manual de VirtualBox (VBoxManage), no ejecutado al preparar este material
# Con la memoria ya capturada y la VM apagada, se clona el disco a RAW:
VBoxManage clonemedium disk victima.vdi victima-disco.raw --format RAW
sha256sum victima-disco.raw | tee victima-disco.raw.sha256
echo "hash anotado en la cadena de custodia"
```

- La imagen RAW es la que se analiza con herramientas de Linux.
- `VBoxManage debugvm <VM> dumpvmcore --filename=...` genera un volcado de la VM.
- En el laboratorio, la fuente es un disco virtual: la **adquisición** es copiar el archivo y sellarlo con su hash.

La práctica usa una imagen generada con el mismo contenido, para poder reproducirla sin VirtualBox.

---

## Bloqueo de escritura

| Situación | Cómo se bloquea |
|---|---|
| Disco físico | Bloqueador por hardware o por software |
| Imagen (archivo) | Se monta en **solo lectura**, y en ext4 con `noload` |

La documentación del kernel advierte, sobre ext4:

> "ext4 reproducirá el journal (y por tanto escribirá en la partición) incluso cuando se monta de solo lectura."

La opción `noload` evita cargar el journal.

```bash
# Fragmento: montaje de la imagen, solo lectura (requiere root)
sudo mount -o ro,noload,noexec,nosuid,nodev,loop victima-disco.dd /mnt/imagen
```

`noexec`, `nosuid` y `nodev` impiden ejecutar nada de la imagen.

---

## Dos sistemas de archivos: ext4 y el journal

Si el sistema no se apagó limpiamente, el journal contiene transacciones pendientes. Montar la imagen con `ro` solo **las reproduce y escribe**: la imagen ya no es la original.

Dos salidas:

1. `ro,noload`: no carga el journal.
2. No montar: leer la imagen con herramientas que no la modifican (`debugfs` en solo lectura, The Sleuth Kit).

La práctica usa la segunda: `debugfs` abre la imagen en solo lectura salvo que se le pase `-w`. Así no hace falta ser root.

---

## Leer la imagen sin montarla

```bash {1-2|4-5|7-8|*}
debugfs -R "ls -l /etc/cron.d" victima-disco.dd
#  15  100664 (1)  1000  1000   57 10-Nov-2026 02:41 sync-cache

debugfs -R "cat /etc/cron.d/sync-cache" victima-disco.dd
# */5 * * * * root /var/tmp/.cache/sync.sh >/dev/null 2>&1

debugfs -R "cat /home/soporte/.ssh/authorized_keys" victima-disco.dd
# ssh-ed25519 AAAA...  soporte@kali
```

(Salida de la imagen de práctica del curso. `debugfs` abre en solo lectura por omisión.)

Aquí está la persistencia de la S03, con su fecha, **sin haber tocado** la imagen.

---

<!-- _class: lead -->

# Parte 3
## La suma de verificación

---

## Qué es un hash

Una **función hash** convierte cualquier archivo en un resumen de longitud fija. Si el archivo cambia un solo bit, el resumen cambia por completo.

```sim hash
texto: /etc/cron.d/sync-cache modificado a las 02:41:50
bit: 0
```

- Si el hash de la copia **coincide** con el del original, la copia es idéntica.
- No prueba autoría ni hora: prueba que **los bytes no cambiaron desde que se calcularon**.

---

## El hash se calcula al adquirir

```bash {1-2|4-5|7-8|*}
sha256sum victima-disco.dd | tee victima-disco.dd.sha256
# 1270825e591b9bb338a231b20353de25c566d73a9e807693b63530967798387e  victima-disco.dd

cp victima-disco.dd copia-trabajo.dd
sha256sum victima-disco.dd copia-trabajo.dd

sha256sum -c victima-disco.dd.sha256
# victima-disco.dd: OK
```

(Salida de una imagen de práctica; la tuya tendrá otro hash.)

Tres reglas:

1. Calcular el hash **antes** de cualquier otra operación, y registrarlo.
2. Verificarlo cada vez que cambia de manos, y antes de analizar.
3. Analizar **una copia** de la copia.

---

## Verlo fallar: el hash tarde

Una "reparación" inocente sobre la copia:

```bash {1-2|4-5|7-8|*}
e2fsck -fn copia-trabajo.dd     # -n: solo lectura, contesta no a todo
sha256sum copia-trabajo.dd      # 1270825e591b...  (igual)

e2fsck -fy copia-trabajo.dd     # -y: repara y escribe
sha256sum copia-trabajo.dd      # 4caf381185a8...  (distinto)

sha256sum -c victima-disco.dd.sha256   # contra la original: OK
sed 's/victima-disco.dd/copia-trabajo.dd/' victima-disco.dd.sha256 | sha256sum -c   # FAILED
```

`cmp -l` muestra **9 bytes** distintos, y `dumpe2fs -h` dice cuáles: *Last write time* y *Last checked* cambiaron.

---

## Qué cambió exactamente

```
== victima-disco.dd
Last write time:          Wed Nov 11 13:08:18 2026
Last checked:             Wed Nov 11 13:08:18 2026
== copia-trabajo.dd
Last write time:          Wed Nov 11 13:16:34 2026
Last checked:             Wed Nov 11 13:16:34 2026
```

- Ninguno de los 9 bytes es "contenido" del atacante: son metadatos del propio sistema de archivos (superbloque).
- Aun así, **el hash ya no coincide**: la copia dejó de poder demostrarse idéntica.
- Si esa copia era la **única**, la evidencia no se puede defender. Con una original sellada, se repite la copia y listo.

Un hash distinto no prueba manipulación; prueba que **ya no puedes demostrar fidelidad**.

---

## Y si se calcula tarde

| Secuencia | Qué demuestras |
|---|---|
| Adquirir, **hash**, analizar | Que lo analizado es lo adquirido |
| Adquirir, analizar, **hash** | Solo que la copia **final** no cambió desde el hash; el análisis pudo alterarla |
| Adquirir sin hash | Nada |

El hash de la **original** es el que ancla la cadena: se calcula en el momento de la adquisición, de preferencia por dos personas o por una herramienta que lo registre sola.

---

## Algoritmos

- Los libros de forense mencionan **MD5** y la familia **SHA** como hashes de integridad (Parasram, 2023, cap. 7).
- En este curso se usa **SHA-256**.
- Regla práctica: el algoritmo se **escribe en la cadena de custodia**, junto al hash.

```bash
sha256sum archivo            # SHA-256
sha256sum -c lista.sha256    # verifica una lista de hashes contra los archivos
```

---

<!-- _class: lead -->

# Parte 4
## Cadena de custodia

---

## Qué debe poder contestar

RFC 3227, sección 4.1:

- ¿**Dónde, cuándo y por quién** fue descubierta y recolectada la evidencia?
- ¿**Dónde, cuándo y por quién** fue manejada o examinada?
- ¿**Quién la tuvo** bajo custodia, durante qué periodo, y cómo se almacenó?
- Cuando **cambió de custodia**: cuándo y cómo ocurrió la transferencia.

NIST SP 800-86 añade: guardar la evidencia en lugar seguro, examinar **solo copias**, y verificar la integridad de la original y de la copia. Si no está claro si hay que preservar, **por omisión se preserva**.

---

## Un formato mínimo

| Campo | Ejemplo |
|---|---|
| Identificador | `EV-2026-11-10-001` |
| Descripción | Imagen del disco de `victima` (RAW, 16 MiB en el laboratorio) |
| Origen | `victima` (192.168.56.20), disco virtual de 16 MiB |
| Recolectó | Nombre, fecha y hora UTC |
| Método y herramienta | `dd` / `VBoxManage clonemedium`, versión |
| Hash (algoritmo) | SHA-256 `1270825e…8387e` |
| Medio de almacenamiento | Disco externo `EVID-02` |

Y una tabla de **traspasos**: quién entrega, quién recibe, fecha y hora UTC, motivo, hash verificado.

---

## Los traspasos

| Fecha y hora UTC | Entrega | Recibe | Motivo | Hash verificado |
|---|---|---|---|---|
| 2026-11-10 03:30 | Responsable técnico | Escriba | Resguardo | Sí |
| 2026-11-10 09:00 | Escriba | Analista | Análisis sobre copia | Sí |

(Ejemplo ilustrativo; nombres omitidos.)

Un periodo **sin responsable** conocido es un eslabón roto. Una noche en un cajón sin registro cuenta como tal.

El marco legal mexicano de la cadena de custodia, con sus instrumentos, está en la nota `12-etica-cadena-custodia`.

---

## Lo que no se escribe en la cadena

- Conclusiones. La cadena dice **qué pasó con la evidencia**, no qué prueba.
- Un traspaso a medias: "se lo llevó Beto" no es un traspaso.
- Un cambio de hash "por corrección". Si cambia, es una evidencia nueva con su propia fila.

<!-- Aprovecha este momento para pedir que cada equipo defina quién es el "custodio" de su evidencia en la práctica. Una sola persona. -->

---

<!-- _class: lead -->

# Parte 5
## La línea de tiempo

---

## Qué es

Una **línea de tiempo** ordena en una sola lista lo que dicen varias fuentes: bitácoras, fechas de archivos, historial, conexiones.

<!-- _pasos: lista -->

1. **Normalizar el tiempo.** Todo a UTC, con la diferencia de reloj anotada.
2. **Reunir marcas.** De cada archivo: modificación (**M**), acceso (**A**), cambio de metadatos (**C**) y creación (**B**), llamadas en conjunto *MACB*.
3. **Ordenar y correlacionar.** Dos fuentes que dicen lo mismo dan confianza.

Una línea de tiempo **sugiere**; no prueba. Se sostiene con más de una fuente.

---

## Dos fuentes, un hecho

Un `sudo tee` en el `auth.log` y la modificación de un archivo, a la misma hora:

```
auth.log   02:41:50  sudo: soporte : COMMAND=/usr/bin/tee /etc/cron.d/sync-cache
mtime      02:41:50  /etc/cron.d/sync-cache (57 bytes)
```

- La bitácora dice **quién y con qué comando**.
- La fecha del archivo dice **qué cambió y cuándo**.
- Juntas, dicen que `soporte` creó la tarea programada a las 02:41:50.

Una sola de las dos permite dudar. Las dos, una junto a la otra, cuesta más refutarlas.

---

## Marcas de tiempo en ext4

| Marca | Qué significa | Cuidado |
|---|---|---|
| mtime | Última modificación del contenido | Se puede cambiar con `touch -d` |
| atime | Último acceso | Cambia al **leer** (con `relatime`, bajo condiciones) |
| ctime | Último cambio del inodo | No se fija con `touch`; queda la hora real del cambio |
| crtime | Creación | ext4 lo guarda; `debugfs` lo muestra |

```
 ctime: Wed Nov 11 13:08:18 2026
 atime: Tue Nov 10 02:41:50 2026
 mtime: Tue Nov 10 02:41:50 2026
crtime: Wed Nov 11 13:08:18 2026
```

(Del `stat` de `/etc/cron.d/sync-cache` en la imagen de práctica.)

---

## La imagen de práctica "miente" en dos marcas

- `mtime` y `atime`: las del incidente (se fijaron con `touch -d` antes de construir la imagen).
- `ctime` y `crtime`: **13:08 del 11 de noviembre**, el momento en que se armó la imagen.

En una imagen **real** de `victima`, `ctime` y `crtime` sí contarían la historia. En la práctica de hoy sirven para una lección: cuando `mtime` y `ctime` **no concuerdan**, alguien cambió una fecha o se copió el archivo. La discrepancia es una pista, no una acusación.

> Dato para el reporte: la hora de adquisición (la de la imagen) siempre es posterior a la del incidente.

---

## Una línea de tiempo con The Sleuth Kit

```bash {1-2|3|*}
# Fragmento: procedimiento segun las paginas de manual de fls y mactime, no ejecutado al preparar este material
fls -r -m / victima-disco.dd > cuerpo.txt
mactime -b cuerpo.txt -d -y > linea.csv
```

- `fls -r -m /` recorre el sistema de archivos y genera un archivo "body" con el punto de montaje como prefijo.
- `mactime -b` lo lee; `-d` saca valores separados por coma y `-y`, fechas ISO 8601.
- Autopsy, el navegador forense que trata Parasram (2023, cap. 12), se apoya en The Sleuth Kit.

La práctica construye una versión mínima con `debugfs` y Python: sirve para entender qué hay debajo.

---

## Nuestra línea de tiempo (modificaciones)

```
2026-11-10T02:36:10Z M /home/soporte/.ssh/authorized_keys   95
2026-11-10T02:41:50Z M /etc/cron.d/sync-cache               57
2026-11-10T02:42:20Z M /var/tmp/.cache/sync.sh             116
2026-11-10T02:43:12Z M /var/tmp/.cache/datos.tgz           208
2026-11-10T03:15:00Z M /var/tmp/.cache/.alive              147
2026-11-10T03:15:10Z M /etc/ufw/user.rules                 132
2026-11-10T03:15:41Z M /var/log/auth.log                  4411
```

(Salida del script de la práctica sobre la imagen de ejemplo.)

- 02:36 a 02:43: **el atacante** deja persistencia y prepara datos.
- 03:15:00: la **tarea programada** sigue escribiendo, como una marca de vida.
- 03:15:10 y 03:15:41: **el responsable** bloquea la IP y cierra la sesión. Son nuestras huellas, y hay que distinguirlas.

---

## Qué dice y qué no dice

| La línea de tiempo **dice** | La línea de tiempo **no dice** |
|---|---|
| Qué archivos cambiaron y cuándo | Quién los cambió, si no hay bitácora |
| Que la tarea escribe cada 5 minutos | Si el atacante leyó `clientes.csv` (hay que mirar `atime` y otras fuentes) |
| Que el responsable actuó a las 03:15 | Si hubo otro acceso por un camino que no dejó archivos |

Las conclusiones se escriben **con su fuente**. Lo que no se puede concluir, se dice: "no hay evidencia de..." no es "no ocurrió".

---

## Artefactos comunes en Linux

| Artefacto | Qué cuenta |
|---|---|
| `/var/log/auth.log`, `journalctl -u ssh` | Accesos, `sudo`, cambios de cuenta |
| `wtmp` y `last` | Inicios y cierres de sesión (`last -f` lee una copia) |
| `~/.ssh/authorized_keys` | Llaves; su fecha fecha la persistencia |
| `/etc/cron.d`, crontabs, timers | Ejecución programada |
| `/tmp`, `/var/tmp`, directorios ocultos | Herramientas y datos preparados |
| `.bash_history` | Comandos; se guarda cuando el shell termina con normalidad |

En nuestro caso **no hay** `.bash_history` de `soporte`: la sesión se terminó a la fuerza. Su ausencia también es un dato.

---

<!-- _class: lead -->

# Parte 6
## La práctica

---

## Qué haces

1. **Preparar el escenario:** con el generador, o con la VM de la S03.
2. **Adquirir:** copia con `dd`, hash al instante, lista de hashes.
3. **Verlo fallar:** reparar la copia con `e2fsck -fy`, y ver cómo el hash ya no coincide.
4. **Leer la imagen sin montarla** con `debugfs`: las persistencias y el `auth.log`.
5. **Línea de tiempo** con el script de Python.
6. **Cadena de custodia** del equipo, con traspasos.

Detalle paso a paso en la guía de práctica.

---

## Lo que se espera

- Un hash registrado **antes** de cualquier otra operación.
- Una cadena de custodia con al menos dos traspasos y el hash verificado en cada uno.
- Una línea de tiempo en UTC con por lo menos diez filas y su fuente.
- Una página con **lo que la evidencia demuestra** y **lo que no**.
- La explicación del hash roto, en tus palabras.

---

<!-- _class: lead -->

# Cierre
## Lo que te llevas

---

## Errores típicos de la sesión

| Error | Qué provoca | Cómo se evita |
|---|---|---|
| Calcular el hash después de analizar | No se demuestra la copia adquirida | Hash en la adquisición |
| Montar la imagen con escritura, o solo `ro` en ext4 | El journal se reproduce y la imagen cambia | `ro,noload`, o no montar |
| Volcado de memoria al mismo disco | Se contamina lo que se va a imagen | Disco externo |
| `tar` o `cp -a` en vivo | Se reescriben fechas de acceso | Imagen y copia de la copia |
| Mezclar UTC con hora local | Línea de tiempo inservible | Todo en UTC, diferencia anotada |
| No anotar los comandos propios | Tus huellas parecen del atacante | Bitácora y cadena de custodia |
| Confundir examen con análisis | Listar no es concluir | Fases separadas |

---

## Lo que te llevas

- Las fuentes se reúnen de la **más volátil a la menos volátil** (RFC 3227), ponderando valor probable y esfuerzo (NIST SP 800-86).
- El hash se calcula **al adquirir**; un hash que cambia no se explica, se repite desde la original.
- En ext4, solo lectura **no basta** sin `noload`: el journal se reproduce.
- La cadena de custodia no tiene huecos de tiempo.
- La línea de tiempo se construye en UTC y con dos fuentes por hecho clave.
- Se separa **lo que la evidencia demuestra** de lo que solo sugiere.

---

## Tarea

**El formato de cadena de custodia de tu equipo**, probado con la evidencia de la práctica.

- Un formato de una hoja, con los campos de la clase y tus propios campos.
- Llenado con la imagen de la práctica: ficha, hash y al menos tres traspasos.
- Una regla escrita para cuándo se **rechaza** una evidencia por falta de cadena.

Detalle completo en la guía de tarea.

---

## Lecturas y libros

**Obligatorias**

- `dev-notes/12-seguridad/06-blue-team/04-forense-basico.md`, completa.
- `dev-notes/12-seguridad/03-forense/12-etica-cadena-custodia.md`.

**Recomendadas**

- RFC 3227 (corto) y NIST SP 800-86, secciones 2 y 3.
- *Digital Forensics and Incident Response* (Johansen, 2020), secciones 2 y 3 (caps. 4 a 10).
- *Digital Forensics with Kali Linux* (Parasram, 2023), caps. 7 y 8.

---

## Siguiente sesión

### S05 - Inteligencia de amenazas y cacería

- Partir de una hipótesis sobre el adversario, no de la alerta.
- MITRE ATT&CK: un lenguaje común para técnicas.
- Emular tres técnicas en tu laboratorio y cazarlas en las bitácoras.

> Cazar es partir de una hipótesis sobre el adversario, no esperar la alerta.

---

## Quiz: evidencia y análisis forense

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin la VM abierta.
- Lo que falles, búscalo en la nota `04-forense-basico` antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
