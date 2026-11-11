# Tarea - S03 - Responder a un incidente

**Defensa y respuesta a incidentes**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/12-seguridad/06-blue-team/03-respuesta-a-incidentes.md` | Evento, incidente y por qué la diferencia importa; Cómo organiza NIST la respuesta | 30 min |
| `dev-notes/12-seguridad/06-blue-team/03-respuesta-a-incidentes.md` | Quién hace qué; Triage; Contener sin destruir la evidencia | 30 min |
| `dev-notes/12-seguridad/06-blue-team/03-respuesta-a-incidentes.md` | Erradicar; Recuperar; Comunicar; F3EAD; Errores comunes | 30 min |
| NIST SP 800-61r3 (abril de 2025), https://doi.org/10.6028/NIST.SP.800-61r3 | Secciones 1 y 2 completas | 30 min |

Lee la nota **con tu bitácora de la práctica abierta**: cada recomendación de NIST que menciona la nota debe poder ligarse a una fila de tu bitácora, o a un hueco de ella.

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Intelligence-Driven Incident Response* (Rebekah Brown y Scott J. Roberts, O'Reilly, 2023) | Cap. 3 - Basics of Incident Response; Caps. 4 a 9 - Find, Fix, Finish, Exploit, Analyze, Disseminate |
| *Digital Forensics and Incident Response* (Gerard Johansen, Packt, 2020) | Sección 1: Cap. 1 - Understanding Incident Response; Cap. 2 - Managing Cyber Incidents |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Entregable: el reporte del incidente (2 horas)

Un reporte del incidente de **tu** simulacro, de la ronda 2. Se escribe para dos lectores: **dirección** (que decide) y **el equipo técnico** (que repite o audita). Un archivo `reporte-incidente-s03.md`.

### Estructura obligatoria

| Sección | Qué lleva | Extensión |
|---|---|---|
| 1. Resumen para dirección | Qué pasó, qué se afectó, qué se hizo, cuál es el estado, sin jerga | Una página como máximo |
| 2. Datos del incidente | Identificador, fechas de inicio, detección, contención, erradicación y fin; líder; categoría (por tipo, como RS.MA-03) | Una tabla |
| 3. Línea de tiempo | Cada hecho con hora UTC, **fuente** (de qué archivo o comando sale) y quién lo hizo | Una tabla, mínimo 20 filas |
| 4. Alcance e impacto | Qué cuentas, equipos y datos se tocaron; qué se **sabe**, qué **no** se sabe | Media página |
| 5. Causa raíz | Cómo entró y por qué pudo (contraseña filtrada, usuario en `sudo`, etc.) | Media página |
| 6. Acciones de respuesta | Preservación, contención, erradicación, recuperación; para cada una, el comando, la hora y el resultado | Una tabla |
| 7. Persistencias encontradas | Cada una, dónde estaba, cuándo se modificó y qué se hizo | Una tabla |
| 8. Comunicación | Qué se avisó, a quién, cuándo y con qué texto | Una tabla |
| 9. Lecciones aprendidas | Cada una con **responsable y fecha**, y la subcategoría de NIST que mejora | Una tabla |
| 10. Anexos | Bitácora completa, `SHA256SUMS`, salidas de `ss` y `ps` | Archivos |

### La línea de tiempo

- Todo en **UTC**, con la diferencia del reloj de `victima` anotada si la hay.
- Cada fila con su fuente: `auth.log`, `journalctl`, `stat`, bitácora del equipo.
- Los hechos del **atacante** y las acciones del **equipo** en la misma tabla, en columnas separadas, para ver el orden real.
- Si una hora es una **estimación**, se marca como tal.

Ejemplo del formato (Ejemplo ilustrativo; las horas son las de la referencia del curso, no las de tu simulacro):

| Hora UTC | Qué ocurrió | Quién | Fuente |
|---|---|---|---|
| 02:31:17 | Acceso exitoso de `soporte` desde 192.168.56.10 | Atacante | `auth.log` (sshd) |
| 02:41:50 | Escritura de `/etc/cron.d/sync-cache` con `sudo tee` | Atacante | `auth.log` (sudo), mtime del archivo |
| 03:15:10 | `ufw deny from 192.168.56.10` | Responsable técnico | `auth.log` (sudo), bitácora |

### Reglas del reporte

- **Un hecho por fila.** "Entró y puso persistencias" son tres filas.
- **Nada sin fuente.** Una afirmación que no se pueda ligar a un archivo, comando o bitácora se marca como hipótesis.
- **Lo que no se sabe va explícito.** "No sabemos si se leyó `clientes.csv`" es una frase válida y necesaria.
- Sin culpables: se describe qué falló en el sistema, no quién se equivocó.

---

## Parte 3 - Criterio de evaluación

| Criterio | Peso | Qué se revisa |
|---|---|---|
| Línea de tiempo | 25 % | 20 filas o más, UTC, fuente en cada fila, orden verificable |
| Respuesta | 20 % | Preservó antes de contener; contuvo sin apagar; erradicó con el listado completo |
| Análisis | 20 % | Causa raíz creíble, alcance con lo conocido y lo desconocido separados |
| Comunicación | 10 % | Resumen entendible por alguien que no es técnico; mensajes con hora |
| Lecciones | 15 % | Cada una con responsable, fecha y la subcategoría de NIST que mejora |
| Honestidad del reporte | 10 % | Las partes en que la ronda 1 salió mal están dichas, no escondidas |

**Falta grave (descuenta hasta la mitad de la nota):** una línea de tiempo con horas inventadas, o una acción en el reporte que no aparece en la bitácora.

---

## Parte 4 - Para mejorar el sistema (opcional, +10 %)

Dos correcciones del incidente, hechas y verificadas en `victima`:

1. `soporte` ya no está en `sudo`. Demuestra con `getent group sudo` y un intento fallido de `sudo`.
2. Una regla de detección escrita en palabras con la línea exacta del `auth.log` que la dispara. (La regla formal se implementa en la sesión 2 en el SIEM.)

---

## Entrega

Un `.zip` o una carpeta con:

- `reporte-incidente-s03.md`.
- La bitácora de la ronda 2 y la carpeta de evidencia con `SHA256SUMS`.
- Las correcciones opcionales, si las hiciste.

<!-- La rúbrica no premia el reporte más largo, sino el más verificable. Un reporte con 20 filas bien fuentadas vale más que uno de 60 con horas redondas. -->
