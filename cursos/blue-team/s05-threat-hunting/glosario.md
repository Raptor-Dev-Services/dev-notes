# Glosario - S05 - Inteligencia de amenazas y cacería

> Definiciones de los términos técnicos de la sesión 5.
> Ordenado alfabéticamente.

---

## A

### Analítica (ATT&CK)
**Categoría:** MITRE ATT&CK
**Definición:** Descripción, por plataforma, de qué buscar y en qué datos para detectar una técnica. Desde la v18 (octubre de 2025) las analíticas (`AN...`) se agrupan en estrategias de detección.
**En el curso:** La analítica AN1236 de T1136.001 menciona `useradd` y `adduser`: de ahí sale la primera consulta de la práctica.

---

### ATT&CK
**Categoría:** Inteligencia de amenazas
**Definición:** Base de conocimiento de MITRE de tácticas y técnicas adversarias basada en observaciones del mundo real. Se organiza por dominios (Enterprise, Mobile, ICS) y se publica por versiones.
**En el curso:** A noviembre de 2026 la versión vigente es la v20.1 (13 de octubre de 2026).

---

### Atomic Red Team
**Categoría:** Emulación
**Definición:** Biblioteca de Red Canary de pruebas pequeñas, cada una asociada a una técnica de ATT&CK, con su comando y su limpieza. Se ejecuta con el módulo `Invoke-AtomicRedTeam`.
**En el curso:** La prueba 1 de T1136.001 crea una cuenta con `useradd`; en la práctica se hace a mano.

---

## C

### Cacería de amenazas (threat hunting)
**Categoría:** Cacería
**Definición:** Búsqueda proactiva en los datos a partir de una hipótesis sobre el comportamiento de un adversario, sin esperar una alerta.
**En el curso:** Cada cacería se documenta con cinco partes: hipótesis, técnica, dato, consulta y resultado con su lectura.

---

### Ciclo de inteligencia
**Categoría:** Inteligencia de amenazas
**Definición:** Proceso repetido en que se define una pregunta, se recolecta y procesa información, se analiza, se entrega a quien decide y se recibe retroalimentación. Cada libro nombra las etapas a su manera.
**En el curso:** Lo que una cacería aprende de tu entorno es el insumo de la siguiente pregunta.

---

### Componente de datos
**Categoría:** MITRE ATT&CK
**Definición:** Elemento de ATT&CK que describe un tipo de dato observable (por ejemplo, la creación de un trabajo programado). Reemplaza a las fuentes de datos, que quedaron deprecadas desde la v18.
**En el curso:** Los libros del curso hablan de "fuentes de datos"; en el marco actual son componentes.

---

## D

### Dato frente a inteligencia
**Categoría:** Inteligencia de amenazas
**Definición:** Un dato es una observación suelta (una IP, un hash). Inteligencia es un dato analizado, con contexto, fuente, fecha y confianza, para decidir algo.
**En el curso:** "30 fallos desde una IP que luego entra" es inteligencia; "192.168.56.10" es un dato.

---

## E

### Emulación de adversario
**Categoría:** Emulación
**Definición:** Reproducir, con autorización y en un entorno controlado, técnicas de ATT&CK para saber si la defensa las detecta. A diferencia del equipo rojo, el equipo azul conoce el plan.
**En el curso:** Se emulan tres técnicas en `victima` con la carta de autorización del módulo 1 de Ciberseguridad.

---

### Estrategia de detección
**Categoría:** MITRE ATT&CK
**Definición:** Enfoque de alto nivel para detectar una técnica; agrupa analíticas específicas de cada plataforma. Sus identificadores empiezan con `DET`.
**En el curso:** T1136.001 tiene DET0447; T1053.003, DET0290; T1098.004, DET0126.

---

## H

### Hipótesis de cacería
**Categoría:** Cacería
**Definición:** Afirmación sobre lo que haría un adversario en tu entorno, escrita para poder refutarse con datos.
**En el curso:** Se escribe antes de la consulta, junto con el resultado que la refutaría.

---

## I

### Indicador de compromiso (IoC)
**Categoría:** Inteligencia de amenazas
**Definición:** Rastro observable de una intrusión: un hash, una IP, un dominio o un artefacto. Es información sobre ciberamenazas según NIST SP 800-150.
**En el curso:** Son los niveles bajos de la Pirámide del Dolor.

---

## L

### Línea base
**Categoría:** Cacería
**Definición:** Descripción de lo normal en el entorno (cuentas, tareas, llaves autorizadas) contra la que se mide lo nuevo.
**En el curso:** Sin `cron-base.txt` y sin `huellas-base.txt` las consultas no distinguen lo normal de lo malicioso.

---

## N

### Navigator (ATT&CK Navigator)
**Categoría:** MITRE ATT&CK
**Definición:** Aplicación web de MITRE para ver y anotar las matrices con capas: colorear técnicas, comentar y asignar valores. Una capa es un archivo JSON.
**En el curso:** Se usa para cruzar las técnicas que preocupan con las que se pueden ver.

---

## P

### PEAK
**Categoría:** Cacería
**Definición:** Marco de cacería de Splunk (SURGe): Prepare, Execute, Act with Knowledge. Distingue cacerías basadas en hipótesis, de línea base y asistidas por modelos.
**En el curso:** Ordena las fases de cada ficha.

---

### Pirámide del Dolor
**Categoría:** Inteligencia de amenazas
**Definición:** Modelo de David Bianco (2013) que ordena los indicadores en seis niveles, de abajo hacia arriba: valores hash, direcciones IP, nombres de dominio, artefactos de red y de host, herramientas y TTP. Mientras más alto, más le cuesta al adversario cambiarlo.
**En el curso:** Justifica escribir detecciones sobre comportamiento y no solo sobre hashes.

---

### Punto ciego
**Categoría:** Cacería
**Definición:** Técnica que el equipo no puede ver porque la fuente de datos no existe o no se recolecta.
**En el curso:** El cambio de `authorized_keys` y el SSH saliente de `victima` son puntos ciegos con solo `auth.log`.

---

## S

### Sesgo de confirmación
**Categoría:** Cacería
**Definición:** Tendencia a buscar solo lo que se espera encontrar y detenerse al encontrarlo.
**En el curso:** Se combate escribiendo qué resultado refutaría la hipótesis antes de consultar.

---

### Subtécnica
**Categoría:** MITRE ATT&CK
**Definición:** Forma específica de una técnica, con su propio identificador (por ejemplo, T1136.001 es una subtécnica de T1136).
**En el curso:** Las tres técnicas de la práctica son subtécnicas.

---

## T

### Táctica
**Categoría:** MITRE ATT&CK
**Definición:** El objetivo táctico del adversario, el porqué de una técnica. A noviembre de 2026 Enterprise tiene 15; la antigua Defense Evasion se dividió en Stealth y Defense Impairment en la v19.
**En el curso:** Persistence (TA0003) agrupa las tres técnicas de la práctica.

---

### Técnica
**Categoría:** MITRE ATT&CK
**Definición:** El cómo de una táctica, con identificador `T` y cuatro dígitos.
**En el curso:** T1053 (Scheduled Task/Job) tiene la subtécnica .003, Cron.

---

### TTP
**Categoría:** Inteligencia de amenazas
**Definición:** Tácticas, técnicas y procedimientos: el comportamiento de un adversario, la capa más alta de la Pirámide del Dolor.
**En el curso:** ATT&CK es el vocabulario para nombrar TTP.
