# Glosario - S03 - Responder a un incidente

> Definiciones de los términos técnicos de la sesión 3.
> Ordenado alfabéticamente.

---

## B

### Bitácora de respuesta
**Categoría:** Respuesta a incidentes
**Definición:** Registro, con hora, de cada acción de la respuesta: quién la hizo, qué comando o decisión fue, qué resultado tuvo y dónde está la prueba. NIST SP 800-61r3 (RS.AN-06) pide registrar las acciones de la investigación y preservar la integridad y la procedencia de esos registros.
**En el curso:** La lleva el escriba en vivo, con hora UTC; sin ella el reporte no se puede verificar.

---

## C

### Categorizar
**Categoría:** Respuesta a incidentes
**Definición:** Clasificar un incidente por tipo (brecha de datos, ransomware, toma de cuenta, negación de servicio) para decidir cómo responder. Corresponde a RS.MA-03.
**En el curso:** El incidente de `victima` se categoriza como toma de cuenta.

### Contención
**Categoría:** Respuesta a incidentes
**Definición:** Acciones que evitan que un incidente se expanda. NIST (RS.MI-01) señala que previene daño adicional y evita desbordar los recursos de la organización.
**En el curso:** Bloquear la IP con `ufw` y terminar la sesión con `loginctl`, sin apagar ni reiniciar.

### Criterio de incidente
**Categoría:** Respuesta a incidentes
**Definición:** Condición escrita de antemano que, si se cumple, obliga a declarar un incidente (DE.AE-08). Considera los falsos positivos conocidos.
**En el curso:** "Acceso de una cuenta desde una IP nunca vista, fuera de horario" es un criterio de ejemplo.

### CSF 2.0
**Categoría:** Marcos
**Definición:** Marco de Ciberseguridad de NIST, versión 2.0. Organiza los resultados de ciberseguridad en seis funciones: Govern, Identify, Protect, Detect, Respond y Recover. NIST SP 800-61r3 lo usa para ordenar la respuesta a incidentes.
**En el curso:** Sustituye al ciclo de cuatro fases de la Rev. 2.

---

## D

### Declarar un incidente
**Categoría:** Respuesta a incidentes
**Definición:** Decisión formal, con dueño, de que un conjunto de eventos adversos cumple los criterios de incidente. Activa el plan de respuesta, el líder y la bitácora.
**En el curso:** Es la primera fila de la bitácora de la ronda 2.

### Detect (DE)
**Categoría:** Marcos
**Definición:** Función del CSF 2.0: los posibles ataques y compromisos se encuentran y analizan.
**En el curso:** Alertas, bitácoras y la revisión que lleva a declarar.

---

## E

### Elevar
**Categoría:** Respuesta a incidentes
**Definición:** Involucrar a un nivel superior de gestión en la respuesta. Se distingue de *escalar*, que aumenta recursos o plazos (RS.MA-04).
**En el curso:** Avisar a dirección general de una decisión que el equipo no puede tomar.

### Erradicación
**Categoría:** Respuesta a incidentes
**Definición:** Mitigar los efectos de un incidente y eliminar la persistencia y los puntos de entrada: borrar malware, deshabilitar cuentas vulneradas, mitigar las vulnerabilidades explotadas (RS.MI-02).
**En el curso:** Recorrer el listado completo de persistencias, no solo cortar la sesión.

### Escalar
**Categoría:** Respuesta a incidentes
**Definición:** Aumentar los recursos o los plazos dedicados a un incidente (RS.MA-04).
**En el curso:** Pedir más técnicos o más horas.

### Escriba
**Categoría:** Respuesta a incidentes
**Definición:** Papel que registra la cronología y las decisiones de la respuesta mientras otros actúan.
**En el curso:** Separar escriba y técnico evita que la bitácora se escriba después.

### Evento
**Categoría:** Respuesta a incidentes
**Definición:** Cualquier ocurrencia observable que involucra activos de cómputo, como un inicio de sesión (NIST SP 800-61r3, sección 1).
**En el curso:** La mayoría de los eventos no son incidentes.

---

## F

### F3EAD
**Categoría:** Inteligencia
**Definición:** Find, Fix, Finish, Exploit, Analyze, Disseminate. Modelo que une operaciones e inteligencia: Find elige el objetivo; Fix lo localiza; Finish ejecuta la acción operativa; Exploit reúne lo producido; Analyze lo desarrolla para el siguiente ciclo; Disseminate lo comparte (Brown y Roberts, 2023, cap. 3; Roberts, 2015).
**En el curso:** El incidente termina cuando lo aprendido llega a quien detecta.

### Falso positivo
**Categoría:** Detección
**Definición:** Alerta que parece un incidente y no lo es.
**En el curso:** Un administrador legítimo desde una IP nueva sería uno; por eso se mantiene una lista de IP conocidas.

### Función (del CSF)
**Categoría:** Marcos
**Definición:** Nivel más alto de organización del CSF 2.0: Govern, Identify, Protect, Detect, Respond, Recover.
**En el curso:** Detect, Respond y Recover son la respuesta; las otras tres, la preparación.

---

## G

### Govern (GV)
**Categoría:** Marcos
**Definición:** Función del CSF 2.0: la estrategia, expectativas y política de gestión de riesgo de ciberseguridad se establecen, comunican y vigilan.

---

## H

### Hacking back
**Categoría:** Ética y legal
**Definición:** Atacar de vuelta a quien atacó. *Intelligence-Driven Incident Response* dedica al tema el título "Finishing Is Not Hacking Back" en el cap. 6.
**En el curso:** Nunca. Todo ocurre en el laboratorio propio.

---

## I

### Identify (ID)
**Categoría:** Marcos
**Definición:** Función del CSF 2.0: se entienden los riesgos de ciberseguridad actuales. Incluye la categoría de mejora (ID.IM).

### Incident handler
**Categoría:** Respuesta a incidentes
**Definición:** Persona que verifica un incidente, reúne y analiza datos, prioriza y limita el daño (NIST SP 800-61r3, sección 2.2).
**En el curso:** El responsable técnico del equipo.

### Incidente de ciberseguridad
**Categoría:** Respuesta a incidentes
**Definición:** Ocurrencia que pone en peligro, real o inminentemente y sin autoridad legal, la integridad, confidencialidad o disponibilidad de la información o de un sistema, o que viola o amenaza violar la ley o las políticas de seguridad (FISMA 2014, citada por NIST SP 800-61r3).

### Indicador de compromiso
**Categoría:** Detección
**Definición:** Señal observable de que un activo pudo haber sido comprometido. NIST (RS.AN-08) pide buscarlos y buscar persistencia en los activos atacados y en otros posibles objetivos.
**En el curso:** Una llave nueva en `authorized_keys` es uno.

### Informe posterior
**Categoría:** Respuesta a incidentes
**Definición:** Documento que describe el incidente, la respuesta y la recuperación, y las lecciones aprendidas (RC.RP-06).
**En el curso:** Es el reporte de la tarea.

---

## L

### Líder del incidente
**Categoría:** Respuesta a incidentes
**Definición:** Persona designada para decidir y coordinar un incidente concreto (RS.MA-01, consideración C1).

### `loginctl terminate-session`
**Categoría:** Linux
**Definición:** Comando de systemd que termina una sesión: mata todos sus procesos y libera sus recursos (man `loginctl`).
**En el curso:** Cierra la sesión del atacante sin detener SSH.

---

## M

### Magnitud
**Categoría:** Respuesta a incidentes
**Definición:** Alcance real de un incidente: qué activos y cuentas están implicados. Estimarla y validarla es de las tareas más difíciles (RS.AN-08).
**En el curso:** ¿Solo `victima` o también otros equipos con la misma contraseña?

---

## P

### Persistencia
**Categoría:** Ataque y defensa
**Definición:** Mecanismo que permite a un atacante volver a entrar o seguir ejecutando código después de perder su sesión.
**En el curso:** Una llave en `authorized_keys` y una tarea en `/etc/cron.d`.

### Perfil comunitario
**Categoría:** Marcos
**Definición:** Selección de resultados del CSF adaptada a un tema; NIST SP 800-61r3 es un perfil comunitario de respuesta a incidentes.

### Protect (PR)
**Categoría:** Marcos
**Definición:** Función del CSF 2.0: se usan salvaguardas para gestionar los riesgos.

---

## R

### Recover (RC)
**Categoría:** Marcos
**Definición:** Función del CSF 2.0: se restauran los activos y operaciones afectados por un incidente.

### Recuperación
**Categoría:** Respuesta a incidentes
**Definición:** Restaurar sistemas a su operación normal, confirmar que funcionan y remediar la causa; incluye verificar la integridad de las copias antes de usarlas (RC.RP-03).

### Respond (RS)
**Categoría:** Marcos
**Definición:** Función del CSF 2.0: se actúa sobre un incidente detectado.

---

## S

### `ss`
**Categoría:** Linux
**Definición:** Comando que muestra sockets. `ss -tanp` lista las conexiones TCP con su proceso; `ss -tlnp`, los puertos que escuchan.
**En el curso:** Se capturan antes de contener: viven en memoria.

---

## T

### Triage
**Categoría:** Respuesta a incidentes
**Definición:** Revisión preliminar de un reporte para verificar que ocurrió un incidente y estimar su severidad y urgencia (RS.MA-02).

---

## U

### `ufw`
**Categoría:** Linux
**Definición:** Interfaz simplificada del firewall de Ubuntu. `ufw deny from <IP>` bloquea el tráfico de una dirección.
**En el curso:** Una de las dos acciones de contención.

---

## V

### Volátil
**Categoría:** Forense
**Definición:** Dato que se pierde al apagar el equipo o con el paso del tiempo: memoria, conexiones, procesos (RFC 3227, sección 2.1).
**En el curso:** Se preserva antes de aislar; en la sesión 4 se ordena formalmente.
