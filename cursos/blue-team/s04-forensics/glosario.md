# Glosario - S04 - Evidencia y análisis forense

> Definiciones de los términos técnicos de la sesión 4.
> Ordenado alfabéticamente.

---

## A

### Adquisición
**Categoría:** Forense
**Definición:** Copiar los datos de una fuente de evidencia con un método que preserva su integridad. NIST SP 800-86 la define dentro de la recolección: identificar, etiquetar, registrar y adquirir los datos de las fuentes posibles.
**En el curso:** La copia de la imagen de disco con su hash.

### Análisis
**Categoría:** Forense
**Definición:** Fase del proceso forense en que se interpretan los resultados del examen con métodos justificables para contestar las preguntas que motivaron el trabajo (NIST SP 800-86).
**En el curso:** La correlación de la línea de tiempo con el `auth.log`.

### atime
**Categoría:** Linux
**Definición:** Fecha de último acceso de un archivo. Cambia al **leer**; con la opción de montaje `relatime` solo se actualiza si la anterior era anterior o igual a la de modificación o cambio, o tiene más de un día (`mount(8)`).
**En el curso:** Es la razón por la que no se copia en vivo con `tar`.

### AVML
**Categoría:** Herramientas
**Definición:** Binario de Microsoft para adquirir memoria de Linux. Se usa como `avml acquire salida.lime`, produce formato LiME y no funciona si el kernel tiene activada `kernel_lockdown`.
**En el curso:** El responsable lo corrió en la S03; aquí solo se documenta el procedimiento.

---

## B

### Banner (de Volatility)
**Categoría:** Herramientas
**Definición:** Cadena que identifica el kernel de una imagen de memoria. El archivo de símbolos debe coincidir **exactamente** con ella.
**En el curso:** El plugin `banners` la extrae de una imagen de memoria.

### Bloqueador de escritura
**Categoría:** Forense
**Definición:** Dispositivo o configuración que impide escribir en el medio original durante la adquisición.
**En el curso:** En imágenes, `ro,noload` hace de bloqueador en ext4; `debugfs` abre en solo lectura.

---

## C

### Cadena de custodia
**Categoría:** Forense
**Definición:** Registro de quién tuvo la evidencia, cuándo y con qué acciones. El RFC 3227 (4.1) pide poder contestar dónde, cuándo y por quién se recolectó y manejó, quién la tuvo y cuándo cambió de custodia.
**En el curso:** La ficha y los traspasos de la práctica.

### Copia de trabajo
**Categoría:** Forense
**Definición:** Copia de la imagen original sobre la que se analiza; la original se sella y se guarda. El RFC 3227 pide analizar "una copia de la copia".
**En el curso:** `copia-trabajo.dd`.

### ctime
**Categoría:** Linux
**Definición:** Fecha del último cambio del inodo. A diferencia de `mtime`, no se fija con `touch`: queda la hora real del cambio.
**En el curso:** En la imagen de práctica es la hora de armado de la imagen, no la del incidente.

### crtime
**Categoría:** Linux
**Definición:** Fecha de creación de un archivo; ext4 la guarda y `debugfs` la muestra.

---

## D

### `dc3dd`
**Categoría:** Herramientas
**Definición:** Herramienta de adquisición de disco que *Digital Forensics with Kali Linux* (Parasram, 2023, cap. 8) presenta junto a `dd`, con su verificación de hashes.

### `dd`
**Categoría:** Herramientas
**Definición:** Comando de GNU que copia bloques sin interpretar su contenido; sirve para imágenes forenses.

### `debugfs`
**Categoría:** Herramientas
**Definición:** Herramienta de `e2fsprogs` para examinar sistemas de archivos ext2/3/4. Abre la imagen en solo lectura salvo que se le pase `-w`.
**En el curso:** Lee archivos y fechas de la imagen sin montarla.

### `dwarf2json`
**Categoría:** Herramientas
**Definición:** Herramienta que genera tablas de símbolos de Linux para Volatility 3 a partir de un kernel con símbolos de depuración.

---

## E

### Examen
**Categoría:** Forense
**Definición:** Fase del proceso forense en que se procesan los datos recolectados, de forma automática y manual, para extraer lo de interés preservando la integridad (NIST SP 800-86).

### `e2fsck`
**Categoría:** Herramientas
**Definición:** Verificador de sistemas de archivos ext2/3/4. Con `-n` no escribe; con `-y` repara y escribe.
**En el curso:** Con `-fy` sobre la copia cambia metadatos y rompe el hash.

---

## H

### Hash
**Categoría:** Integridad
**Definición:** Resumen de longitud fija calculado sobre unos datos. Si los datos cambian un solo bit, el resumen cambia por completo.
**En el curso:** SHA-256 con `sha256sum`.

---

## I

### Imagen forense
**Categoría:** Forense
**Definición:** Copia bit a bit de un medio de almacenamiento, incluido el espacio sin asignar.
**En el curso:** `victima-disco.dd`.

### Integridad (de la evidencia)
**Categoría:** Forense
**Definición:** Propiedad de que la evidencia no cambió desde que se adquirió. Se demuestra comparando el hash de la original y el de la copia (NIST SP 800-86, 3.2).

---

## J

### Journal (de ext4)
**Categoría:** Linux
**Definición:** Registro de transacciones pendientes del sistema de archivos. Al montar ext4, incluso en solo lectura, se reproduce y **escribe** salvo que se use `noload` (documentación del kernel).

---

## K

### `kernel_lockdown`
**Categoría:** Linux
**Definición:** Función de seguridad del kernel de Linux. Según la documentación de AVML, con ella activa la adquisición no funciona.

---

## L

### LiME
**Categoría:** Herramientas
**Definición:** Linux Memory Extractor: módulo del kernel que se carga con `insmod` y vuelca la memoria en formatos `lime`, `padded` o `raw`.
**En el curso:** Se prepara antes del incidente para el kernel exacto.

### Línea de tiempo
**Categoría:** Forense
**Definición:** Lista ordenada de hechos con su hora, construida con varias fuentes (bitácoras, fechas de archivos, conexiones).
**En el curso:** `linea.csv`, en UTC.

---

## M

### MACB
**Categoría:** Forense
**Definición:** Siglas de las marcas de tiempo de un archivo: modificación (M), acceso (A), cambio de metadatos (C) y creación (B, de *birth*).

### `mactime`
**Categoría:** Herramientas
**Definición:** Programa de The Sleuth Kit que crea una línea de tiempo ASCII a partir de un archivo "body" generado con `fls -m` o `ils -m`.

### mtime
**Categoría:** Linux
**Definición:** Fecha de última modificación del contenido. Se puede cambiar con `touch -d`.

---

## N

### `noload`
**Categoría:** Linux
**Definición:** Opción de montaje de ext4 que evita cargar el journal, y con ella `ro` no escribe en la partición.

---

## O

### Orden de volatilidad
**Categoría:** Forense
**Definición:** Orden en que se reúnen las fuentes, de la que se pierde más rápido a la que dura más. El RFC 3227 (2.1) lo lista: registros y caché; tabla de ruteo, ARP, procesos y memoria; sistemas de archivos temporales; disco; registros remotos; configuración física; medios de archivo.

---

## R

### Recolección
**Categoría:** Forense
**Definición:** Primera fase del proceso forense (NIST SP 800-86): identificar, etiquetar, registrar y adquirir datos con procedimientos que preserven su integridad.

### `relatime`
**Categoría:** Linux
**Definición:** Opción de montaje por omisión que actualiza `atime` solo si la anterior era anterior o igual a `mtime` o `ctime`, o si tiene más de un día.

### Reporte
**Categoría:** Forense
**Definición:** Fase final del proceso forense: informar resultados y acciones, con recomendaciones.

---

## S

### SHA-256
**Categoría:** Integridad
**Definición:** Función hash de la familia SHA con resumen de 256 bits. Es la que se usa en el curso.

### `sha256sum`
**Categoría:** Linux
**Definición:** Comando que calcula SHA-256; con `-c` verifica una lista de hashes contra los archivos.

### Superbloque
**Categoría:** Linux
**Definición:** Estructura de ext4 con los datos globales del sistema de archivos, entre ellos la hora de la última escritura (`s_wtime`) y la de la última revisión (`s_lastcheck`).
**En el curso:** Es lo que cambia `e2fsck -fy` y rompe el hash.

---

## T

### The Sleuth Kit
**Categoría:** Herramientas
**Definición:** Conjunto de herramientas de línea de comandos para analizar imágenes de disco; incluye `fls` y `mactime`. Autopsy se apoya en él.

### Tabla de símbolos
**Categoría:** Herramientas
**Definición:** Archivo que describe las estructuras de un kernel concreto para que Volatility 3 interprete un volcado de memoria.

### Traspaso
**Categoría:** Forense
**Definición:** Cambio de custodia de la evidencia: quién entrega, quién recibe, cuándo, por qué y con qué hash verificado.

---

## U

### UTC
**Categoría:** Forense
**Definición:** Tiempo universal coordinado. El RFC 3227 pide anotar si cada marca de tiempo es UTC o local, y la diferencia del reloj del sistema respecto a UTC.
**En el curso:** Todas las líneas de tiempo van en UTC.

---

## V

### Volatility 3
**Categoría:** Herramientas
**Definición:** Marco de análisis de memoria. Para Linux necesita una tabla de símbolos del kernel exacto de la imagen.

### Volátil
**Categoría:** Forense
**Definición:** Dato que se pierde al apagar el equipo o con el paso del tiempo, como la memoria y las conexiones.
