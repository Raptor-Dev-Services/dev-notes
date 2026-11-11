# 01 - Un programa de seguridad que se pueda sostener

> Lectura del curso [Defensa y respuesta a incidentes](../../cursos/blue-team/README.md), sesión 1.

## Por qué importa

Un equipo que defiende parte de una pregunta que parece tonta y casi nadie sabe contestar: **qué tenemos y qué vale**. Sin un inventario, un parche pendiente es un riesgo sin dueño; sin saber qué datos hay en cada sistema, una alerta a las tres de la mañana no se puede priorizar; sin una política escrita, "ya parchamos" depende de quién se acuerde.

Un programa de seguridad no es una herramienta ni un documento: es el conjunto de decisiones repetibles que hace que la defensa no dependa de la memoria de una persona. Esta nota cubre las cuatro piezas con las que arranca: el inventario de activos y datos, las políticas con sus estándares y procedimientos, la gestión de vulnerabilidades y el cuidado para que todo eso se sostenga con poca gente.

Todos los sistemas y organizaciones de esta nota son **ejemplos ilustrativos**. Las cifras de vulnerabilidades son reales y llevan su fecha de consulta.

## Dónde encaja en un marco: las seis funciones de NIST CSF 2.0

El Cybersecurity Framework 2.0 del NIST (publicado el 26 de febrero de 2024) organiza los resultados de seguridad en seis funciones: **Govern, Identify, Protect, Detect, Respond y Recover**. Los de esta sesión viven sobre todo en dos:

| Función | Categoría (CSF 2.0) | Qué pide, dicho en corto |
|---|---|---|
| Govern | `GV.PO` Policy | La política de ciberseguridad se establece, se comunica y se hace cumplir |
| Govern | `GV.RM` Risk Management Strategy | Hay una estrategia de gestión de riesgos |
| Identify | `ID.AM` Asset Management | Los activos (datos, hardware, software, sistemas) se identifican y se gestionan según su importancia |
| Identify | `ID.RA` Risk Assessment | Se entiende el riesgo para la organización, sus activos y las personas |
| Protect | `PR.PS` Platform Security | El hardware, el software y los servicios se gestionan de acuerdo con el riesgo (aquí cae el parchado) |

El CSF no dice **cómo**: describe resultados. Para el cómo hay otros catálogos. Uno muy usado en equipos pequeños son los **CIS Critical Security Controls** (versión 8.1, 24 de junio de 2024, 18 controles). Los que tocan esta sesión: control 1 (inventario y control de activos de la empresa), 2 (activos de software), 3 (protección de datos) y 7 (gestión continua de vulnerabilidades).

## Inventario de activos: lo que no está en la lista no se defiende

Un **activo** es todo lo que tiene valor para la organización y puede perderse, filtrarse o dejar de funcionar: equipos, servicios, cuentas, datos, proveedores. Un inventario útil no es una hoja de cálculo bonita; es una lista que **alguien mantiene** y que contesta, por cada activo, cinco preguntas:

1. ¿Qué es y dónde vive? (nombre, tipo, ubicación o proveedor)
2. ¿De quién es? (un dueño con nombre, no "sistemas")
3. ¿Qué datos toca y de qué clase?
4. ¿Qué tan crítico es para que el negocio opere?
5. ¿Qué tan expuesto está? (internet, red interna, solo localhost)

*Defensive Security Handbook* (Berlin, Brotherston y Reyor, 2024), cap. 2, trata el inventario completo: el esquema de campos, la criticidad y el riesgo, la información específica por tipo de activo (equipos, usuarios, aplicaciones, nube) y las reglas para que no se pudra (automatizar, una sola fuente de verdad, un equipo que lo cuide).

### Cómo se llena sin empezar de cero

- **Descubrimiento:** lo que el equipo no recuerda, la red lo sabe. En un laboratorio, `ss -tulpn` muestra qué escucha en cada máquina y `dpkg-query -W` qué paquetes hay instalados.
- **Fuentes que ya existen:** la consola del proveedor de nube, el DNS, el repositorio de infraestructura como código, la lista de cuentas del correo.
- **Un ciclo de vida:** un activo se da de alta, cambia y se da de baja. El activo olvidado (la VM de la demo de hace un año) es el favorito de quien ataca.

### Un esquema mínimo

```csv
id,nombre,tipo,dueno,datos,clase_datos,criticidad,exposicion,notas
A01,api-prod,aplicacion,ana,"clientes (nombre, correo)",confidencial,3,3,"detrás de nginx, puerto 443"
```

Los números de `criticidad` y `exposicion` son una escala **propia** (1 a 3 en el curso): lo importante es que sea una escala escrita y que todos la usen igual.

## Clasificar los datos

Clasificar es decidir cuánto cuidado merece cada tipo de dato, **antes** de que haya un incidente. El libro (cap. 2, "Data Classification") propone crear un sistema de clasificación propio; no existe una escala obligatoria única. Una de cuatro clases, que es la que usa el curso como ejemplo, es:

| Clase | Qué es | Ejemplo ilustrativo |
|---|---|---|
| Público | Se puede publicar sin daño | La página de precios |
| Interno | No es secreto, pero no es para fuera | El calendario del equipo |
| Confidencial | Su filtración daña a personas o al negocio | Correos y teléfonos de clientes |
| Restringido | Su filtración es grave y de acceso muy limitado | Contraseñas, llaves, respaldos completos |

Lo que importa de una clase es lo que **dispara**: quién puede verla, si va cifrada, cuánto se conserva y qué hacer si se pierde. Una clase que no cambia ninguna de esas decisiones sobra.

Si los datos son **personales**, la clasificación se cruza con la ley aplicable (en México, la Ley Federal de Protección de Datos Personales en Posesión de los Particulares): consulta la [nota de protección de datos personales](../05-legislacion/12-proteccion-datos-personales.md).

## Priorizar: criticidad por exposición

Con el inventario completo hay que decidir **qué se atiende primero**, porque no alcanza para todo. Una forma sencilla y defendible: multiplicar la criticidad del activo por su exposición. Es un método **cualitativo** (el puntaje es una convención del equipo, no un estándar), pero tiene dos virtudes: obliga a escribir el criterio y produce una lista ordenada.

| Activo (ejemplo) | Criticidad (1-3) | Exposición (1-3) | Puntaje |
|---|---|---|---|
| api-prod | 3 | 3 | 9 |
| respaldos | 3 | 1 | 3 |
| servidor de pruebas | 1 | 3 | 3 |

El servidor de pruebas empata con los respaldos porque está expuesto a internet aunque valga poco: por eso la exposición pesa. Un puntaje no reemplaza al juicio: sirve para empezar la conversación y para decir "por qué primero esto".

## Políticas, estándares, procedimientos y guías

*Defensive Security Handbook*, caps. 3 y 4, separa cuatro documentos que se confunden entre sí. La distinción que usa el curso:

| Documento | Responde | Cambia | Ejemplo ilustrativo |
|---|---|---|---|
| **Política** | Qué se busca y por qué; quién manda | Poco (años) | "Los sistemas se mantienen actualizados para reducir el riesgo de explotación" |
| **Estándar** | Qué requisito concreto y obligatorio se cumple | A veces | "Las actualizaciones de seguridad críticas se instalan en 7 días" |
| **Procedimiento** | Cómo se hace, paso a paso | Seguido | "Para parchar la VM: `apt update`, `apt upgrade`, reiniciar si lo pide, comprobar el servicio" |
| **Guía** | Qué se recomienda cuando hay opciones | Seguido | "Conviene parchar fuera de horas de más uso" |

Cada documento tiene el nivel de cambio que le toca. Si el plazo de siete días estuviera en la política, cambiarlo exigiría la firma de quien dirige; en un estándar basta con la del responsable técnico. Si los pasos de `apt` estuvieran en el estándar, cada cambio de herramienta lo reescribiría.

### Qué lleva una política que alguien va a leer

El cap. 3 del libro detalla el lenguaje y el contenido. Resumido: una política corta, en lenguaje que entienda quien no es técnico, con **propósito, alcance, roles y responsabilidades, la regla misma, excepciones y consecuencias**, con un dueño y una fecha de revisión. Una política que nadie encuentra no existe: se publica donde la gente trabaja.

### La excepción también se documenta

Habrá sistemas que no se pueden parchar a tiempo (un proveedor que no certifica la actualización, una ventana de mantenimiento fija). La política necesita un camino explícito para eso: quién autoriza, por cuánto tiempo, con qué control compensatorio (por ejemplo, quitar la exposición a internet) y cuándo se vuelve a revisar. Sin ese camino, las excepciones ocurren de todos modos, pero en silencio.

## Gestión de vulnerabilidades

Una **vulnerabilidad** es una debilidad que puede aprovecharse. La gestión de vulnerabilidades es un ciclo repetido (el control 7 de CIS lo llama "continua"), no una auditoría anual:

1. **Descubrir:** qué software y versiones hay (el inventario) y qué vulnerabilidades conocidas tienen.
2. **Priorizar:** de las que aplican, cuáles importan más aquí y ahora.
3. **Remediar:** parchar, cambiar la configuración, quitar la exposición o aceptar el riesgo por escrito.
4. **Verificar:** comprobar que el arreglo funcionó (volver a medir).
5. **Reportar:** cuántas hay abiertas y cuánto tardan en cerrarse.

*Defensive Security Handbook*, cap. 18, cubre este tema: qué es, cómo se hace un programa y cómo se priorizan los hallazgos.

### Tres señales para priorizar, y por qué una sola engaña

| Señal | Qué dice | Qué no dice |
|---|---|---|
| **CVSS** (FIRST) | La **severidad técnica** de la vulnerabilidad. v4.0 se publicó el 1 de noviembre de 2023; tiene los grupos de métricas Base, Threat, Environmental y Supplemental. Calificación: None 0.0, Low 0.1-3.9, Medium 4.0-6.9, High 7.0-8.9, Critical 9.0-10.0 | Si alguien la está explotando; si en **tu** sistema importa |
| **EPSS** (FIRST) | La **probabilidad** de que la CVE se explote en la naturaleza en los próximos 30 días, de 0 a 1, actualizada diariamente | Qué daño haría; no se usa sola |
| **KEV** (CISA) | Lista de vulnerabilidades que **ya se explotan** activamente en la naturaleza. Cada entrada trae fecha de alta y fecha límite para las agencias federales de EE. UU. | No es exhaustiva: que no esté no significa que nadie la explote |

El **contexto propio** completa el cuadro: ¿el servicio está expuesto?, ¿qué datos toca?, ¿hay un control que lo compense?

**Ejemplo real, consultado el 11 de noviembre de 2026.** CVE-2024-6387, la condición de carrera en `sshd` de OpenSSH que afecta a las versiones 8.5p1 a 9.7p1 y se corrigió en la 9.8 (publicada el 1 de julio de 2024):

- CVSS v3.1 base **8.1 (High)**, vector `AV:N/AC:H/PR:N/UI:N/S:U/C:H/I:H/A:H`, según la NVD (`AC:H`, complejidad de ataque alta, es la condición que lo separa de los 9.8 típicos de un vector de red sin condiciones).
- EPSS **0.995** con percentil 0.999 en la fecha 2026-11-10, según la API de FIRST.
- **No aparece** en el catálogo KEV (versión 2026.11.10).

Las tres señales cuentan una historia distinta: severa pero difícil de explotar, probabilidad alta según el modelo, y sin explotación confirmada en el catálogo. La decisión correcta depende de si ese `sshd` está en internet. Y las notas de la versión 9.8 de OpenSSH aclaran un detalle que los titulares omitieron: la explotación demostrada fue en sistemas Linux/glibc de 32 bits con ASLR, y en laboratorio requirió en promedio de 6 a 8 horas de conexiones continuas. Las cifras cambian a diario: no las memorices, aprende a consultarlas.

### La política de parches: quién y cuándo

Una política de parches responde, por tipo de activo y por urgencia, cuatro preguntas: **qué** se parcha, **quién** lo hace, **cuándo** (plazo máximo) y **cómo se comprueba**. Un plazo sin dueño no se cumple.

Una tabla de plazos de ejemplo ilustrativo (los plazos son decisión de cada organización, no un estándar):

| Condición | Plazo máximo | Quién |
|---|---|---|
| Explotada activamente (en KEV o con evidencia propia) | 3 días | Responsable técnico |
| CVSS crítico o alto en un servicio expuesto a internet | 7 días | Responsable técnico |
| CVSS alto en un servicio interno | 30 días | Administrador del sistema |
| El resto | En el siguiente mantenimiento mensual | Administrador del sistema |

Para tener una referencia real de plazos: las entradas recientes del catálogo KEV traen fechas límite de **3 o 14 días** después de su alta (observado en las entradas agregadas entre junio y noviembre de 2026). Es una obligación para agencias federales de EE. UU., no para cualquier organización; sirve como orden de magnitud de lo que una autoridad considera urgente.

### Parches automáticos en Ubuntu

En Ubuntu Server, el paquete `unattended-upgrades` instala solo las actualizaciones de los orígenes que permita su configuración. Los archivos relevantes son `/etc/apt/apt.conf.d/20auto-upgrades` (qué tan seguido) y `/etc/apt/apt.conf.d/50unattended-upgrades` (qué orígenes y si reinicia). En una instalación de Ubuntu 26.04 revisada para esta nota, el origen permitido por omisión incluye `${distro_id}:${distro_codename}-security` y el reinicio automático (`Unattended-Upgrade::Automatic-Reboot`) viene comentado, es decir, sin activar. Lo que vale para una política: un parche de kernel o de una biblioteca que no se reinicia **no está aplicado del todo**, así que la política también decide quién reinicia y cuándo.

## Métricas que sirven y métricas que no

- **Sirven:** porcentaje de activos con dueño; porcentaje del inventario con datos clasificados; tiempo medio de remediación por severidad; vulnerabilidades abiertas más allá de su plazo.
- **Engañan:** "cero vulnerabilidades" (nadie las ve todas), el número bruto de hallazgos del escáner (premia al escáner que más ruido hace), el porcentaje de parches instalados sin decir de qué.

## Errores típicos

- **Inventario de una sola vez.** Se hace en enero y en marzo ya no sirve. Lo que no se actualiza solo se desactualiza.
- **Un dueño colectivo.** "Sistemas" no contesta el teléfono a las tres de la mañana; una persona con nombre sí.
- **Priorizar solo por CVSS.** Llena la cola de críticos que nadie puede explotar en tu entorno y deja pasar uno medio que sí está en internet.
- **Política copiada.** Un documento de otra empresa dice cosas que nadie aquí hace; el día de una auditoría es peor que no tener ninguno.
- **Sin camino de excepción.** Los sistemas que no se pueden parchar se esconden en lugar de documentarse.
- **Parchar sin verificar.** Se instaló el paquete, pero el servicio sigue corriendo con el binario viejo hasta que se reinicie.

## Actualización al 2026

- NIST CSF pasó de la versión 1.1 a la **2.0** (26 de febrero de 2024), que incluye la función **Govern**; los resultados de esta sesión sobre política y estrategia de riesgo viven ahí.
- CVSS **v4.0** (1 de noviembre de 2023) convive con la v3.1 que todavía publican las bases de datos de vulnerabilidades; revisa cuál versión trae el puntaje que lees.
- El CIS Controls v8.1 (24 de junio de 2024) alinea sus funciones con CSF 2.0 y añade gobernanza.
- Cifras de KEV y EPSS de esta nota: consultadas el 11 de noviembre de 2026.

## Glosario

| Término | Definición |
|---|---|
| Activo | Todo lo que tiene valor para la organización: equipos, servicios, cuentas, datos, proveedores |
| Inventario de activos | Lista mantenida de los activos con su dueño, sus datos, su criticidad y su exposición |
| Clasificación de datos | Asignar a cada tipo de dato una clase que determina cómo se protege |
| Criticidad | Qué tanto depende el negocio de que el activo funcione |
| Exposición | Qué tan alcanzable es el activo para quien ataca (internet, red interna, local) |
| Política | Documento de alto nivel que fija qué se busca y quién manda; cambia poco |
| Estándar | Requisito concreto y obligatorio que concreta una política |
| Procedimiento | Pasos para ejecutar una tarea |
| Guía | Recomendación para cuando hay más de una opción válida |
| Vulnerabilidad | Debilidad que puede aprovecharse |
| CVE | Identificador público de una vulnerabilidad conocida (`CVE-año-número`) |
| CVSS | Sistema de calificación de la severidad técnica de una vulnerabilidad |
| EPSS | Estimación de la probabilidad de que una CVE se explote en los próximos 30 días |
| KEV | Catálogo de CISA de vulnerabilidades explotadas activamente |
| Control compensatorio | Medida que reduce el riesgo cuando el arreglo directo no es posible a tiempo |
| Excepción | Aceptación documentada, con dueño y fecha, de no cumplir una regla |

## Referencias

- Berlin, A., Brotherston, L. y Reyor, W. F., III. (2024). *Defensive security handbook: Best practices for securing infrastructure* (2.ª ed.). O'Reilly Media. Caps. 1 a 4 y 18.
- Center for Internet Security. (2024). *CIS Critical Security Controls Version 8.1*. https://www.cisecurity.org/controls/v8-1
- Cybersecurity and Infrastructure Security Agency. (2026, 10 de noviembre). *Known Exploited Vulnerabilities Catalog* (versión 2026.11.10). https://www.cisa.gov/known-exploited-vulnerabilities-catalog
- Forum of Incident Response and Security Teams. (2024). *Common Vulnerability Scoring System version 4.0: Specification document* (rev. 1.2). https://www.first.org/cvss/v4-0/specification-document
- Forum of Incident Response and Security Teams. (s. f.). *Exploit Prediction Scoring System (EPSS)*. https://www.first.org/epss/
- National Institute of Standards and Technology. (2024). *The NIST Cybersecurity Framework (CSF) 2.0* (NIST CSWP 29). https://doi.org/10.6028/NIST.CSWP.29
- National Institute of Standards and Technology. (s. f.). *CVE-2024-6387*. National Vulnerability Database. https://nvd.nist.gov/vuln/detail/CVE-2024-6387
- OpenSSH. (2024, 1 de julio). *OpenSSH 9.8 release notes*. https://www.openssh.org/txt/release-9.8

---

*Rogelio Arriaga Gonzalez*
