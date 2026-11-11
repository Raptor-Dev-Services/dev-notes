# Tarea - S01 - Un programa de seguridad que se pueda sostener

**Defensa y respuesta a incidentes**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/12-seguridad/06-blue-team/01-programa-de-seguridad.md` | Todo el documento: inventario, clasificación, políticas, vulnerabilidades y política de parches | 60 min |
| `dev-notes/12-seguridad/04-seguridad-informacion/09-seguridad-logica-y-ciclo-de-vida-de-los-datos.md` | El ciclo de vida de los datos, para ubicar dónde vive cada dato de su inventario | 30 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Defensive Security Handbook* (Amanda Berlin, Lee Brotherston y William F. Reyor III, O'Reilly, 2024) | Cap. 1 - Creating a Security Program. Cap. 2 - Asset Management and Documentation. Cap. 3 - Policies. Cap. 4 - Standards and Procedures. Cap. 18 - Vulnerability Management |

Y dos lecturas cortas en línea: la especificación de CVSS v4.0 de FIRST (`first.org/cvss/v4-0/specification-document`, la sección que explica los grupos de métricas) y la página de EPSS (`first.org/epss`).

---

## Parte 2 - Entregable: la política de parches (2.5 horas)

Escriban `politica-de-parches.md` para el **sistema de ejemplo de la práctica** (la tienda: 12 activos, con las correcciones que hicieron en el paso 5). Una a dos páginas, en lenguaje que entienda quien no es técnico.

### Estructura obligatoria

1. **Propósito.** Una frase: qué riesgo reduce.
2. **Alcance.** Qué activos del inventario cubre y cuáles **no** (con su motivo). Citen los `id` del inventario.
3. **Roles.** Quién decide, quién ejecuta, quién verifica. **Personas con nombre**, no áreas.
4. **Cómo se prioriza.** Qué señales usan (CVSS, EPSS, KEV, exposición, datos) y en qué orden las leen. Una tabla.
5. **Plazos.** Una tabla con condición, plazo máximo y responsable. Al menos cuatro filas, y una de ellas para vulnerabilidades explotadas activamente.
6. **Cómo se parcha.** Qué se hace en cada caso y cuándo se reinicia. Debe decir **quién reinicia y cuándo** si el parche lo exige.
7. **Cómo se verifica.** Qué comando o evidencia demuestra que quedó (paquete **y** proceso).
8. **Excepciones.** Cómo se piden, quién autoriza, duración máxima, control compensatorio obligatorio y quién las revisa.
9. **Métricas.** Dos que sí cambien decisiones, con su meta.
10. **Dueño y fecha de revisión.**

### Un anexo: tres casos

Apliquen su política a tres vulnerabilidades **reales** que ustedes elijan (por ejemplo, de la lista de KEV o una que afecte un paquete de su `victima`). Por cada una, en una tabla:

| CVE | CVSS (versión) | EPSS (fecha) | ¿En KEV? | Activo afectado | Exposición | Plazo que da su política | Decisión |
|---|---|---|---|---|---|---|---|

Reglas del anexo:

- Cada valor se consulta **hoy** y lleva su fecha. No se copian de internet sin consultar.
- Al menos una de las tres debe terminar en **excepción** o en **aceptación del riesgo**, con su justificación.
- Al menos una debe tener un EPSS alto y un CVSS medio (o al revés) y explicar cuál señal pesó más.

### Reglas

- **Una política es tuya cuando la puedes cumplir.** No escriban plazos que su equipo no pueda sostener.
- Nada de copiar una política de internet: si una plantilla les sirvió, reescriban cada línea con sus activos.
- Los activos y personas son del ejemplo ilustrativo. Nada real.

---

## Criterio de evaluación

Se considera completa cuando:

- [ ] Las diez secciones están y la política cabe en dos páginas.
- [ ] Cada rol tiene una persona con nombre.
- [ ] La tabla de plazos tiene al menos cuatro filas y una de ellas cubre la explotación activa.
- [ ] La política dice quién reinicia y cuándo.
- [ ] El proceso de excepción exige fecha de vencimiento y control compensatorio.
- [ ] El anexo trae tres casos con valores consultados hoy, con fecha, y al menos uno termina en excepción.
- [ ] La verificación pide evidencia del paquete **y** del proceso.
- [ ] Ningún plazo contradice la capacidad real del equipo (explicada en una frase).

| Criterio | Peso |
|---|---|
| Claridad y cumplimiento posible de la política | 30 % |
| Plazos, roles y verificación | 25 % |
| Anexo de tres casos con datos consultados | 25 % |
| Excepciones y métricas | 20 % |

---

## Qué se entrega

En el repositorio del equipo, en la carpeta `blue-team/s01/`:

- `politica-de-parches.md` (uno por equipo).
- `inventario.csv` y `bitacora.md` de la práctica.

---

## Preguntas frecuentes

**¿Los plazos tienen que ser los de la clase?**
No. Los de la clase son un ejemplo ilustrativo. Lo que necesitan es que los suyos estén escritos, que alguien responda por ellos y que el equipo los pueda cumplir.

**¿Puedo usar CVSS v4.0 o v3.1?**
La que traiga la fuente que consultaron. Escriban cuál es en cada fila.

**¿Qué hago si una CVE no tiene EPSS?**
Anótenlo. Las CVE muy recientes pueden no tenerlo todavía. Decidan con las otras señales y digan que faltó esta.

**¿Una política debe incluir el nombre de las herramientas?**
No: los nombres de herramientas van en el procedimiento. La política dura más que cualquier herramienta.

---

## Preparación para S02

Trae a la sesión 2:

- `victima` encendida, con `ssh` y `nginx` funcionando y su dirección 192.168.56.20.
- El laboratorio de Ciberseguridad (`kali`) listo con Hydra y las listas pequeñas de la práctica del módulo 6.
- La respuesta a esta pregunta: ¿cuántos intentos fallidos de SSH registró tu `victima` la última vez que corriste Hydra? Búscalo con `sudo grep -c 'Failed password' /var/log/auth.log`.
