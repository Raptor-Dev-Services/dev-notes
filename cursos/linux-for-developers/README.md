# Linux para desarrolladores

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/linux-for-developers/](https://academy.raptorcloud.dev/linux-for-developers/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** saber programar y haber abierto una terminal alguna vez.
**Objetivo:** administrar con criterio el servidor Linux donde corre su aplicación: terminal, permisos, scripts, servicios, red y diagnóstico.
**Formato:** 6 sesiones de 5 horas (30 h). Cada sesión trae clase, quiz, práctica, tarea y glosario, como Titulación 2026.
**Para quién:** egresados y desarrolladores que despliegan en una VM y hoy copian comandos sin saber qué hacen.

---

## Sesión 1. La terminal eficiente (5 h)

> Quien domina las tuberías contesta en una línea lo que otros resuelven con una hoja de cálculo.

**Material:** [clase](s01-efficient-terminal/slides.md), [práctica](s01-efficient-terminal/practica.md), [tarea](s01-efficient-terminal/tarea.md), [glosario](s01-efficient-terminal/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** qué hace el shell antes de correr un comando, tuberías, historial, expansión y moverse por el sistema de archivos.
- [ ] **Lectura:** [`10-cloud/03-linux-nginx.md`](../../10-cloud/03-linux-nginx.md) (comandos esenciales). [`10-cloud/18-terminal-eficiente.md`](../../10-cloud/18-terminal-eficiente.md).
- [ ] **Práctica:** con el log de nginx de un servidor de laboratorio, contestar en una línea cada una: las 10 IP que más piden, los 5xx por hora, la ruta más lenta.
- [ ] **Tarea:** su hoja de atajos propia, con un ejemplo real por comando.
- **Libros:** *Efficient Linux at the Command Line* (Barrett, 2022), caps. 1 a 4. *Learning Modern Linux* (Hausenblas, 2022), cap. 3.

**Al terminar puedes:** encadenar comandos para responder preguntas sobre archivos y logs sin abrir un editor.

---

## Sesión 2. Usuarios, permisos y paquetes (5 h)

> El mínimo privilegio empieza en el sistema operativo, no en la aplicación.

**Material:** [clase](s02-users-permissions/slides.md), [práctica](s02-users-permissions/practica.md), [tarea](s02-users-permissions/tarea.md), [glosario](s02-users-permissions/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** usuarios y grupos, sudo, permisos y umask, usuarios de servicio sin shell, gestor de paquetes.
- [ ] **Lectura:** [`10-cloud/03-linux-nginx.md`](../../10-cloud/03-linux-nginx.md) (usuarios y permisos).
- [ ] **Práctica:** un usuario de servicio sin shell y una carpeta compartida por grupo. Verlo fallar: otro usuario intenta leer y recibe `Permission denied`.
- [ ] **Tarea:** auditar quién puede usar sudo en su VM y justificar cada caso.
- **Libros:** *Practical Linux System Administration* (Hess, 2023), caps. 2, 4 y 6. *Learning Modern Linux*, caps. 4 y 6.

**Al terminar puedes:** decidir qué usuario corre cada cosa y demostrar que no puede hacer más de lo que debe.

---

## Sesión 3. Bash que falla en voz alta (5 h)

> Un script que sale con 0 después de fallar es peor que no tener script.

**Material:** [clase](s03-bash-scripting/slides.md), [práctica](s03-bash-scripting/practica.md), [tarea](s03-bash-scripting/tarea.md), [glosario](s03-bash-scripting/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** `set -euo pipefail`, parámetros, funciones, comillas, códigos de salida, depuración con `bash -x`.
- [ ] **Lectura:** [`10-cloud/19-bash-scripting.md`](../../10-cloud/19-bash-scripting.md).
- [ ] **Práctica:** un respaldo que verifica su propio resultado: si el archivo sale vacío, termina con error y no lo deja pasar por bueno.
- [ ] **Tarea:** reescribir uno de sus scripts viejos y mostrar el caso que antes pasaba en silencio.
- **Libros:** *Pro Bash* (Varma), caps. 3 a 6, 10 y 13. *Efficient Linux at the Command Line*, caps. 8 y 9.

**Al terminar puedes:** escribir scripts que nadie tiene que vigilar, porque avisan cuando algo sale mal.

---

## Sesión 4. Servicios con systemd (5 h)

> Si su aplicación no vuelve sola después de reiniciar el servidor, no está desplegada.

**Material:** [clase](s04-systemd-services/slides.md), [práctica](s04-systemd-services/practica.md), [tarea](s04-systemd-services/tarea.md), [glosario](s04-systemd-services/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** arranque del sistema, units, `systemctl`, timers en lugar de cron, el journal, y cgroups para limitar memoria y CPU.
- [ ] **Lectura:** [`10-cloud/20-systemd.md`](../../10-cloud/20-systemd.md).
- [ ] **Práctica:** convertir una API en servicio con usuario propio, reinicio automático, límite de memoria y un timer de limpieza. Verlo fallar: `sudo reboot` y comprobar que todo vuelve.
- [ ] **Tarea:** un timer que corra su respaldo de la sesión 3 y deje rastro en el journal.
- **Libros:** *systemd for Linux SysAdmins* (Both), caps. 2 a 5, 8, 9, 11 y 14.
- **Caso real:** la limpieza nocturna de los runners del dev-stack del instructor es un timer de systemd y no un workflow, porque un job no puede detener al runner que lo corre.

**Al terminar puedes:** dejar un servicio que arranca solo, se limita y se puede auditar.

---

## Sesión 5. Red, firewall y SSH (5 h)

> Lo que escucha en un puerto es superficie de ataque, se haya planeado o no.

**Material:** [clase](s05-network-firewall-ssh/slides.md), [práctica](s05-network-firewall-ssh/practica.md), [tarea](s05-network-firewall-ssh/tarea.md), [glosario](s05-network-firewall-ssh/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** interfaces y DNS, `ss` para ver qué escucha, SSH con llaves, firewall, y Nginx como proxy inverso.
- [ ] **Lectura:** [`10-cloud/03-linux-nginx.md`](../../10-cloud/03-linux-nginx.md) (Nginx y Certbot), [`10-cloud/08-zero-trust.md`](../../10-cloud/08-zero-trust.md).
- [ ] **Práctica:** publicar la API detrás de Nginx, cerrar todo lo demás y probar desde otra máquina. Verlo fallar: un contenedor publica un puerto y Docker se salta el firewall.
- [ ] **Tarea:** inventario de puertos antes y después, con el porqué de cada uno.
- **Libros:** *Learning Modern Linux*, cap. 7. *Practical Linux System Administration*, caps. 5 y 13. *systemd for Linux SysAdmins*, caps. 10 y 12.

**Al terminar puedes:** explicar cada puerto abierto de su servidor y probar desde fuera que los demás están cerrados.

---

## Sesión 6. Observar y diagnosticar (5 h)

> Diagnosticar es un método, no una corazonada.

**Material:** [clase](s06-observe-diagnose/slides.md), [práctica](s06-observe-diagnose/practica.md), [tarea](s06-observe-diagnose/tarea.md), [glosario](s06-observe-diagnose/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** disco, memoria, CPU y procesos; almacenamiento y `fstab`; leer logs con método; una lista de diagnóstico de cinco minutos.
- [ ] **Lectura:** [`10-cloud/04-observabilidad.md`](../../10-cloud/04-observabilidad.md). [`10-cloud/21-diagnostico-linux.md`](../../10-cloud/21-diagnostico-linux.md).
- [ ] **Práctica:** tres incidentes sembrados: disco lleno por logs, servicio que no arranca y puerto ocupado. Se resuelven con la lista, no adivinando.
- [ ] **Tarea:** su runbook de diagnóstico, probado con un incidente que les siembre otro equipo.
- **Libros:** *Learning Modern Linux*, caps. 5 y 8. *Practical Linux System Administration*, caps. 7 a 9 y 12. *systemd for Linux SysAdmins*, cap. 15.
- **Caso real:** en el server de staging, un segundo disco montado con `nofail`: si falla, el sistema arranca igual y las carpetas aparecen vacías en el disco principal.

**Al terminar puedes:** encontrar la causa de una caída con un procedimiento que otra persona puede repetir.

---

## Proyecto final

Dejar un servidor listo para una API: usuario de servicio, unit con límites, timer de respaldo verificado, Nginx, firewall y runbook. Se califica cuando otro equipo rompe algo a escondidas y ustedes lo encuentran con su runbook.


## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| Efficient Linux at the Command Line | Daniel J. Barrett | 2022 | Terminal y tuberías |
| Learning Modern Linux | Michael Hausenblas | 2022 | Panorama moderno: permisos, red, observabilidad |
| Practical Linux System Administration | Kenneth Hess | 2023 | Administración del día a día |
| systemd for Linux SysAdmins | David Both | s/f | Servicios, timers, journal y cgroups |
| Pro Bash | Jayant Varma | s/f | Scripting |
| Using and Administering Linux, vols. 1 a 3 | David Both | s/f | Lectura de profundización |
| CompTIA Linux+ Certification Companion | Ahmed F. Sheikh | s/f | Para quien quiera certificarse después |

---

*Rogelio Arriaga Gonzalez*
