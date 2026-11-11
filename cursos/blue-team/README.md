# Defensa y respuesta a incidentes

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/blue-team/](https://academy.raptorcloud.dev/blue-team/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** el curso de [Ciberseguridad](../cybersecurity/README.md) (ataque básico con autorización) o Titulación 2026, y Linux básico ([Linux para desarrolladores](../linux-for-developers/README.md)).
**Objetivo:** montar detección sobre bitácoras reales, responder a un incidente con método, conservar evidencia, cazar amenazas con MITRE ATT&CK y conducir un ejercicio de mesa con su postmortem.
**Formato:** 6 sesiones de 5 horas (30 h). Cada sesión trae clase, quiz, práctica, tarea y glosario, como Titulación 2026.
**Para quién:** quien terminó Ciberseguridad y quiere el otro lado: defender y responder.

> **Marco legal y ético.** Toda práctica con ataque o emulación se hace en un laboratorio propio (red aislada `192.168.56.0/24`: `kali` 192.168.56.10, `victima` 192.168.56.20 y `siem` 192.168.56.30), con la carta de autorización del curso de Ciberseguridad. Los datos, usuarios, direcciones y empresas de los ejemplos son ficticios y están rotulados como tales. Nada se prueba contra sistemas ajenos.

---

## Sesión 1. Un programa de seguridad que se pueda sostener (5 h)

> No se defiende lo que no se sabe que existe.

**Material:** [clase](s01-security-program/slides.md), [práctica](s01-security-program/practica.md), [tarea](s01-security-program/tarea.md), [glosario](s01-security-program/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** inventario de activos y datos, políticas, estándares y procedimientos, y gestión de vulnerabilidades.
- [ ] **Lectura:** [`12-seguridad/06-blue-team/01-programa-de-seguridad.md`](../../12-seguridad/06-blue-team/01-programa-de-seguridad.md).
- [ ] **Práctica:** inventario priorizado de los activos y datos de un sistema de ejemplo, y la decisión sobre una vulnerabilidad real combinando CVSS, EPSS, KEV y contexto.
- [ ] **Tarea:** la política de parches de ese sistema, con quién y cuándo.
- **Libros:** *Defensive Security Handbook* (Berlin, Brotherston y Reyor, 2024), caps. 1 a 4 (programa de seguridad, activos y documentación, políticas, estándares y procedimientos) y 18 (gestión de vulnerabilidades).

**Al terminar puedes:** decir qué se protege primero y por qué.

---

## Sesión 2. Bitácoras y detección (5 h)

> Una alerta que nunca sonó en una prueba no se sabe si funciona.

**Material:** [clase](s02-logging-detection/slides.md), [práctica](s02-logging-detection/practica.md), [tarea](s02-logging-detection/tarea.md), [glosario](s02-logging-detection/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** qué registrar, centralizar las bitácoras, reglas de detección y alertas.
- [ ] **Lectura:** [`10-cloud/04-observabilidad.md`](../../10-cloud/04-observabilidad.md). [`12-seguridad/06-blue-team/02-bitacoras-y-deteccion.md`](../../12-seguridad/06-blue-team/02-bitacoras-y-deteccion.md).
- [ ] **Práctica:** detectores para la fuerza bruta de SSH y el sondeo de rutas de nginx, disparados a propósito; laboratorio con Wazuh (Elastic como alternativa). Verlo fallar: la fuerza bruta del módulo de Hydra de Ciberseguridad tiene que hacer sonar la alerta, y la regla de fábrica no alcanza a verla.
- [ ] **Tarea:** tres reglas más, cada una disparada a propósito.
- **Libros:** *Defensive Security Handbook*, caps. 21 (IDS e IPS) y 22 (bitácoras y monitoreo). *Threat Hunting with Elastic Stack* (Pease, 2021), caps. 3 a 7 (la pila de Elastic, el laboratorio, la recolección con Beats y Elastic Agent, y Kibana).

**Al terminar puedes:** detectar en sus bitácoras el ataque que aprendieron a hacer.

---

## Sesión 3. Responder a un incidente (5 h)

> Contener antes de entender borra la evidencia; entender sin contener deja al atacante adentro.

**Material:** [clase](s03-incident-response/slides.md), [práctica](s03-incident-response/practica.md), [tarea](s03-incident-response/tarea.md), [glosario](s03-incident-response/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** la respuesta según NIST SP 800-61 Rev. 3, roles, comunicación, y el modelo F3EAD aplicado a la respuesta.
- [ ] **Lectura:** [`12-seguridad/06-blue-team/03-respuesta-a-incidentes.md`](../../12-seguridad/06-blue-team/03-respuesta-a-incidentes.md).
- [ ] **Práctica:** simulacro: alguien entra por SSH con una contraseña filtrada. Detectar, preservar, contener, erradicar y recuperar, con bitácora de cada paso. Verlo fallar: dos maneras de contener mal.
- [ ] **Tarea:** el reporte del incidente, con su línea de tiempo.
- **Libros:** *Intelligence-Driven Incident Response* (Brown y Roberts, 2023), caps. 3 a 9 (ciclo de respuesta y F3EAD: Find, Fix, Finish, Exploit, Analyze, Disseminate). *Digital Forensics and Incident Response* (Johansen, 2020), sección 1, caps. 1 a 3.

**Al terminar puedes:** conducir la respuesta a un incidente sin improvisar el orden.

---

## Sesión 4. Evidencia y análisis forense (5 h)

> Si no se puede demostrar quién tocó la evidencia, no sirve como evidencia.

**Material:** [clase](s04-forensics/slides.md), [práctica](s04-forensics/practica.md), [tarea](s04-forensics/tarea.md), [glosario](s04-forensics/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** orden de volatilidad, adquisición de evidencia de red, del equipo y de memoria; imagen forense, suma de verificación, cadena de custodia y línea de tiempo.
- [ ] **Lectura:** [`12-seguridad/06-blue-team/04-forense-basico.md`](../../12-seguridad/06-blue-team/04-forense-basico.md). [`12-seguridad/03-forense/12-etica-cadena-custodia.md`](../../12-seguridad/03-forense/12-etica-cadena-custodia.md).
- [ ] **Práctica:** adquirir y analizar la evidencia de disco del simulacro de la sesión 3, con suma de verificación y línea de tiempo. Verlo fallar: reparar la copia cambia su hash.
- [ ] **Tarea:** el formato de cadena de custodia de su equipo.
- **Libros:** *Digital Forensics and Incident Response* (Johansen, 2020), secciones 2 y 3, caps. 4 a 10. *Digital Forensics with Kali Linux* (Parasram, 2023), como apoyo: caps. 7 y 8 (adquisición y hashes), 10 (Volatility 3) y 12 (Autopsy).

**Al terminar puedes:** reunir evidencia que resiste la pregunta "¿cómo sabes que no la cambiaste?".

---

## Sesión 5. Inteligencia de amenazas y cacería (5 h)

> Cazar es partir de una hipótesis sobre el adversario, no esperar la alerta.

**Material:** [clase](s05-threat-hunting/slides.md), [práctica](s05-threat-hunting/practica.md), [tarea](s05-threat-hunting/tarea.md), [glosario](s05-threat-hunting/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** inteligencia de amenazas, MITRE ATT&CK, hipótesis de cacería y consultas.
- [ ] **Lectura:** [`12-seguridad/06-blue-team/05-threat-hunting-attack.md`](../../12-seguridad/06-blue-team/05-threat-hunting-attack.md).
- [ ] **Práctica:** emular tres técnicas de ATT&CK en su propio laboratorio, con autorización, y cazarlas en las bitácoras. Verlo fallar: una consulta que no regresa nada no prueba que esté limpio.
- [ ] **Tarea:** documentar cada cacería: hipótesis, consulta, resultado.
- **Libros:** *Practical Threat Intelligence and Data-Driven Threat Hunting* (Palacín, 2021), caps. 4, 6, 8, 9 y 10. *Adversary Emulation with MITRE ATT&CK* (Selmanaj, 2024), caps. 3, 5 y 6.

**Al terminar puedes:** buscar a un adversario concreto en sus datos y documentar qué encontraron y qué no.

---

## Sesión 6. Ejercicio de mesa y postmortem (5 h)

> El primer incidente de verdad no debe ser el primer ensayo.

**Material:** [clase](s06-tabletop-postmortem/slides.md), [práctica](s06-tabletop-postmortem/practica.md), [tarea](s06-tabletop-postmortem/tarea.md), [glosario](s06-tabletop-postmortem/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** cómo diseñar y conducir un ejercicio de mesa, y el postmortem sin culpables.
- [ ] **Lectura:** [`12-seguridad/06-blue-team/03-respuesta-a-incidentes.md`](../../12-seguridad/06-blue-team/03-respuesta-a-incidentes.md). [`12-seguridad/04-seguridad-informacion/15-pruebas-y-mantenimiento-del-plan.md`](../../12-seguridad/04-seguridad-informacion/15-pruebas-y-mantenimiento-del-plan.md).
- [ ] **Práctica:** ejercicio de mesa con un escenario hipotético de ransomware, con roles, siete injects y reloj.
- [ ] **Tarea:** el postmortem del ejercicio, con acciones, responsables y fechas.
- **Libros:** *Cybersecurity Tabletop Exercises* (Lelewski y Hollenberger, 2025), partes I y II (caps. 1 a 8). *Intelligence-Driven Incident Response* (Brown y Roberts, 2023), caps. 10 y 11.

**Al terminar puedes:** ensayar un incidente con su equipo y convertirlo en mejoras con dueño.

---

## Proyecto final

Un laboratorio de detección con cinco reglas, cada una vista disparar, y el informe completo del incidente simulado: línea de tiempo, evidencia y postmortem.

## Lecturas de profundización

Libros de la biblioteca de ciberseguridad que amplían una sesión sin ser la columna del curso. Se leen después de la sesión, no antes; los capítulos son los del índice de cada libro (en los de Packt, por orden de aparición). Cada quien consigue su copia.

| Libro | Capítulos | Sesión y para qué |
|---|---|---|
| *Executive's Cybersecurity Program Handbook* (Jason Brown, 2023) | 1 The First 90 Days; 4 Establishing Governance through Policy; 6 Risk Management; 12 Testing Your Security and Building Metrics | 1. Cómo arrancar el programa, sostenerlo con políticas y medir si funciona. |
| *Attack Surface Management* (Ron Eddings y MJ Kaufmann, 2025) | 4 Identification and Classification of Assets; 5 Automating Asset Discovery; 6 Prioritization and Crown Jewel Analysis; 8 Remediation | 1. Profundiza el inventario de activos y qué se protege primero. |
| *Building a Cyber Risk Management Program* (Brian Allen, Brandon Bapst y Terry Allan Hicks, 2023) | 2 The Cyber Risk Management Program; 5 Risk-Based Strategy and Execution | 1. Priorizar por riesgo, no sólo por puntaje. |
| *CIS Ubuntu Linux 24.04 LTS Benchmark* v1.0.0 (Center for Internet Security, 2024) | Secciones 1.2 Package Management; 5.1 Configure SSH Server; 5.2 Configure privilege escalation; 6.1 System Logging; 6.2 System Auditing | 1 y 2. Un estándar de configuración real contra el cual medir `victima`; 6.1 y 6.2 acompañan qué registrar. |
| *CIS Docker Benchmark* v1.7.0 (Center for Internet Security, 2024) | Secciones 1 Host Configuration; 6 Docker Security Operations | 1 y 2. Estándar de endurecimiento si el sistema de ejemplo corre contenedores. |
| *Cybersecurity: Attack and Defense Strategies* (Yuri Diogenes y Erdal Ozkaya, 2.ª ed., 2019) | 12 Active Sensors; 17 Log Analysis | 2. Qué ven los sensores y cómo se leen las bitácoras de sistema, firewall y servidor web. |
| *Infosec Strategies and Best Practices* (Joseph MacMillan, 2021) | 6 Designing and Managing Security Testing Processes; 7 Owning Security Operations | 2. Cómo operar la detección como proceso continuo. |
| *Incident Response in the Age of Cloud* (Erdal Ozkaya, 2021) | 3 How to Organize an Incident Response Team; 5 Methods and Tools of Incident Response Processes (OODA, playbooks); 6 Incident Handling; 8 Incident Reporting | 3. Roles, playbooks y a quién se reporta; el cap. 11 (Incident Response in the Cloud) queda como contexto si el sistema vive en la nube. |
| *Cybersecurity: Attack and Defense Strategies* (Diogenes y Ozkaya, 2019) | 2 Incident Response Process; 14 Investigating an Incident; 15 Recovery Process | 3. Otro recorrido del ciclo de respuesta, con la recuperación. |
| *Building Secure and Reliable Systems* (Heather Adkins y otros, 2020) | 15 Investigating Systems; 17 Crisis Management; 18 Recovery and Aftermath | 3. La respuesta vista desde quien opera el servicio: investigar, manejar la crisis y recuperar. |
| *Executive's Cybersecurity Program Handbook* (Brown, 2023) | 7 Incident Response | 3. Cómo se inserta la respuesta en el programa de la sesión 1. |
| *Practical Forensic Imaging* (Bruce Nikkel, 2016) | 3 Forensic Image Formats; 4 Planning and Preparation; 6 Forensic Image Acquisition; 7 Forensic Image Management | 4. Profundiza la adquisición de la imagen, el bloqueo de escritura, la verificación de integridad y el manejo de la copia. |
| *Cybersecurity: Attack and Defense Strategies* (Diogenes y Ozkaya, 2019) | 13 Threat Intelligence | 5. Inteligencia de amenazas para alimentar la hipótesis de cacería. |
| *Cybersecurity Threats, Malware Trends, and Strategies* (Tim Rains, 2.ª ed., 2023) | Capítulo «What to Know about Threat Intelligence» (el índice no lo numera) | 5. Qué es y qué no es la inteligencia de amenazas, con sus límites. |
| *Incident Response in the Age of Cloud* (Ozkaya, 2021) | 10 Cyber Threat Intelligence Sharing; 12 Building a Culture of Incident Readiness | 5 y 6. Compartir indicadores y convertir la cacería y el ensayo en cultura. |
| *Incident Response in the Age of Cloud* (Ozkaya, 2021) | 13 Incident Response Best Practices (probar el plan, revisiones posteriores) | 6. Refuerza el ejercicio de mesa y el postmortem. |
| *Building Secure and Reliable Systems* (Adkins y otros, 2020) | 16 Disaster Planning; 18 Recovery and Aftermath; Apéndice A Disaster Risk Assessment Matrix | 6. Planear el desastre antes de que ocurra y aprender del después. |

**Revisados y no incluidos:** *Practical Malware Analysis*, *Adversarial Tradecraft in Cybersecurity* y *Malware Development for Ethical Hackers* (ingeniería inversa y técnica ofensiva, más allá de este curso); *Zero Trust Networks*, *Practical Cybersecurity Architecture* y *Software Supply Chain Security* (arquitectura y cadena de suministro, que piden un curso propio); *The Cybersecurity Playbook for Modern Enterprises* y *Building a Cyber Resilient Business* (orientados a dirección ejecutiva).

---

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| Defensive Security Handbook | Amanda Berlin, Lee Brotherston y William F. Reyor III | 2024 | Columna del curso |
| Intelligence-Driven Incident Response | Rebekah Brown y Scott J. Roberts | 2023 | Respuesta guiada por inteligencia |
| Digital Forensics and Incident Response | Gerard Johansen | 2020 | Evidencia y forense |
| Practical Threat Intelligence and Data-Driven Threat Hunting | Valentina Palacín | 2021 | Cacería |
| Threat Hunting with Elastic Stack | Andrew Pease | 2021 | Laboratorio de detección |
| Adversary Emulation with MITRE ATT&CK | Drinor Selmanaj | 2024 | Emulación |
| Cybersecurity Tabletop Exercises | Robert Lelewski y John Hollenberger | 2025 | Ejercicio de mesa |
| Digital Forensics with Kali Linux | Shiva V. N. Parasram | 2023 | Apoyo de la sesión 4 |

---

*Rogelio Arriaga Gonzalez*
