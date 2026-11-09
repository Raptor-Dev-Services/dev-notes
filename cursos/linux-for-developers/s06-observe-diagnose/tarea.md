# Tarea - S06 - Observar y diagnosticar

**Linux para desarrolladores**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/21-diagnostico-linux.md` | Todo el documento: método, lista de cinco minutos, disco, fstab, servicios, puertos, logs y plantilla de runbook | 60 min |
| `dev-notes/10-cloud/04-observabilidad.md` | Los tres pilares, Health checks, SLOs y error budget, Cuándo usar cada herramienta | 40 min |

Mientras leen la nota de observabilidad, anoten qué de lo que hoy hicieron a mano (la lista de cinco minutos) podría ser una alerta que les avise antes de que el usuario lo reporte.

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Learning Modern Linux* (Michael Hausenblas, O'Reilly, 2022) | Cap. 5 - Filesystems. Cap. 8 - Observability |
| *Practical Linux System Administration* (Kenneth Hess, O'Reilly, 2023) | Cap. 7 - Managing Storage. Cap. 8 - Maintaining System Health. Cap. 9 - Monitoring Your System. Cap. 12 - Troubleshooting Linux |
| *systemd for Linux SysAdmins* (David Both, Apress) | Cap. 15 - Analyzing systemd Startup and Configuration |

Y dos lecturas cortas en línea de Brendan Gregg: *The USE Method* (brendangregg.com) y *Linux Performance Analysis in 60,000 Milliseconds* (Netflix Tech Blog).

---

## Parte 2 - Ejercicio: su runbook de diagnóstico (2 horas)

Escriban `docs/runbook.md` en el repo de su equipo. Debe servirle a alguien que **no** conoce su VM.

### 1. La línea base

Qué es "normal" en su VM: carga típica, memoria available, uso de disco e inodos, units activas y el inventario de puertos de la sesión 5. Peguen las salidas de `linea-base.txt`.

### 2. Los primeros cinco minutos

La lista, en orden, con el comando exacto y **qué valor sería anormal en su VM**. No "revisar el disco": `df -h -x tmpfs`, y anormal es más de 85% en `/`.

### 3. Un apartado por síntoma

Al menos estos cuatro, cada uno con la plantilla de la clase (cómo se ve, hipótesis con el comando que las confirma o descarta, arreglo, verificación, prevención):

- La API no contesta desde fuera (502, timeout o rechazo).
- La API se reinicia sola.
- El disco está lleno.
- Un servicio no arranca después de un cambio.

### 4. La prueba

Pídanle a **otro equipo** que les siembre un incidente que ustedes no conozcan. Puede ser uno de `sembrar.sh` o uno inventado por ellos, siempre en la VM de laboratorio y con forma de deshacerlo. Resuélvanlo **siguiendo su runbook** y anoten:

- El ticket que recibieron.
- La bitácora (hora, comando, salida, decisión).
- En qué paso el runbook los llevó a la causa, o en qué paso no alcanzó.
- La corrección que le hicieron al runbook después.

---

## Criterio de aceptación

Su tarea se considera completa cuando:

- [ ] El runbook tiene línea base, lista de cinco minutos y al menos cuatro síntomas.
- [ ] Cada comando del runbook se puede copiar y correr tal cual (sin "algo como...").
- [ ] Cada síntoma tiene su verificación: el mismo comando que mostró el problema, ahora bien.
- [ ] Hay un incidente sembrado por otro equipo, con su bitácora escrita durante el diagnóstico.
- [ ] El runbook tiene al menos una corrección que salió de esa prueba.
- [ ] No hay IP reales ni contraseñas en el documento.

---

## Preguntas frecuentes

**¿El runbook es para mi VM o para cualquier servidor?**
Para su VM, que es lo que van a defender en el proyecto final. Lo que sea general, déjenlo general; lo que sea de su VM (rutas, puertos, nombres de units), escríbanlo explícito.

**¿Qué hago si el otro equipo me siembra algo que el runbook no cubre?**
Es exactamente el objetivo. Resuélvanlo con el método de la clase y agreguen ese síntoma al runbook.

**¿Puedo reiniciar la VM durante la prueba?**
Pueden, pero anótenlo y digan por qué. Un reinicio que "arregla" sin que sepan la causa es un incidente que va a volver.

---

## Proyecto final

Dejar un servidor listo para una API: usuario de servicio, unit con límites, timer de respaldo verificado, Nginx, firewall y runbook. **Se califica cuando otro equipo rompe algo a escondidas y ustedes lo encuentran con su runbook.** Este runbook es la pieza que se pone a prueba.
