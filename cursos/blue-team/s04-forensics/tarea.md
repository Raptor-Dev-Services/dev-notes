# Tarea - S04 - Evidencia y análisis forense

**Defensa y respuesta a incidentes**
**Tiempo estimado:** 3 a 4 horas

---

## Parte 1 - Lecturas (1.5 a 2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/12-seguridad/06-blue-team/04-forense-basico.md` | El proceso, Principios de recolección, Orden de volatilidad | 25 min |
| `dev-notes/12-seguridad/06-blue-team/04-forense-basico.md` | Qué se adquiere y cómo; Integridad; Cadena de custodia; La línea de tiempo | 35 min |
| `dev-notes/12-seguridad/03-forense/12-etica-cadena-custodia.md` | La cadena de custodia; Un formato de cadena de custodia para TIC; Errores comunes | 30 min |
| RFC 3227, *Guidelines for Evidence Collection and Archiving* (2002) | Secciones 2 y 4 | 15 min |

Lee las notas **con tu práctica abierta**: cada campo de tu formato tiene que poder defenderse con una frase de la lectura.

### Recomendadas (para quien quiere profundizar)

| Recurso | Qué |
|-------|-----------|
| *Digital Forensics and Incident Response* (Gerard Johansen, Packt, 2020) | Sección 2: Cap. 4 - Collecting Network Evidence; Cap. 5 - Acquiring Host-Based Evidence; Cap. 6 - Forensic Imaging. Sección 3: Cap. 7 a 10 (análisis de red, memoria, almacenamiento y bitácoras) |
| *Digital Forensics with Kali Linux* (Shiva V. N. Parasram, Packt, 2023) | Cap. 7 - Incident Response, Data Acquisitions, and DFIR Frameworks; Cap. 8 - Evidence Acquisition Tools |
| NIST SP 800-86 (2006) | Secciones 2 y 3: el proceso forense y la recolección |

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Parte 2 - Entregable: el formato de cadena de custodia de tu equipo (2 horas)

Un formato que tu equipo **usaría de verdad** en la siguiente noche de incidente, y la prueba de que funciona con la evidencia de la práctica. Un archivo `cadena-de-custodia-equipo.md` más los anexos.

### Qué lleva

| Sección | Contenido | Extensión |
|---|---|---|
| 1. La ficha de evidencia | Todos los campos de la clase, más al menos **tres propios** que justifiques (por ejemplo, ubicación física del medio, motivo de la adquisición, quién autorizó) | Una tabla |
| 2. La tabla de traspasos | Fecha y hora UTC, entrega, recibe, motivo, hash verificado, firma o confirmación | Una tabla |
| 3. Instrucciones de llenado | Una línea por campo: qué se escribe y qué **no** | Media página |
| 4. Reglas de rechazo | Cuándo tu equipo **no acepta** una evidencia: hash ausente, hueco de tiempo, hora local sin diferencia de reloj, etc. | Lista de al menos 6 |
| 5. Prueba con la práctica | La ficha y **tres traspasos** llenados con la imagen de la S04 | Tablas llenas |
| 6. Lo que el formato no cubre | Qué falta (memoria no capturada, evidencia en la nube, otros) | Media página |

### Los campos obligatorios

El formato debe poder contestar las cuatro preguntas de la sección 4.1 del RFC 3227:

1. ¿Dónde, cuándo y por quién se descubrió y recolectó?
2. ¿Dónde, cuándo y por quién se manejó o examinó?
3. ¿Quién la tuvo bajo custodia, durante qué periodo, y cómo se almacenó?
4. Cuando cambió de custodia, ¿cuándo y cómo ocurrió la transferencia?

Y llevar el algoritmo y el valor del **hash**, la **hora en UTC** y la **herramienta con su versión**.

### La prueba con la práctica

- Los tres traspasos son reales en tu equipo: cada uno con su `sha256sum -c` hecho **en el momento** y anotado.
- Al menos uno incluye una **verificación fallida**: introduce a propósito un cambio en la copia (por ejemplo, la reparación con `e2fsck -fy` del paso 3) y documenta qué hizo el custodio: ¿se rechaza, se recupera de la original, se anota como incidente?

### Reglas del entregable

- **Una hoja.** El formato debe caber en una hoja imprimible por ambos lados. Un formato de cinco páginas no se llena a las 3 de la mañana.
- **Sin conclusiones.** La cadena dice qué pasó **con la evidencia**, no qué prueba.
- **Todo en UTC.** Si el equipo usa hora local, se declara la diferencia y se justifica.
- No inventes un marco legal: si lo mencionas, cítalo de las notas de la materia de forense.

---

## Parte 3 - Criterio de evaluación

| Criterio | Peso | Qué se revisa |
|---|---|---|
| Cobertura del formato | 25 % | Contesta las cuatro preguntas del RFC 3227 y lleva hash, UTC y herramienta |
| Reglas de rechazo | 20 % | Al menos seis, concretas y verificables |
| Prueba con la práctica | 25 % | Tres traspasos con hash verificado y al menos una verificación fallida documentada |
| Utilidad real | 15 % | Cabe en una hoja; un compañero lo llena sin preguntar |
| Honestidad | 15 % | La sección 6 nombra lo que el formato **no** cubre |

**Falta grave (descuenta hasta la mitad):** un hash o una hora que no corresponden a la salida real de los comandos; una cadena con un periodo sin responsable.

---

## Parte 4 - Para quien quiere más (opcional, +10 %)

Escribe un guion de **diez comandos** que un responsable pueda copiar y pegar para reunir lo volátil de un Linux (conexiones, procesos, sesiones, bitácoras) a un disco externo, con hash final. Pruébalo en tu `victima`. El guion debe:

- Escribir **solo** en el disco externo.
- Registrar la hora UTC al inicio y al final.
- Terminar con un `sha256sum` de lo reunido y la ruta de la lista.

---

## Entrega

Un `.zip` o una carpeta con:

- `cadena-de-custodia-equipo.md`.
- La ficha y los traspasos llenados con la evidencia de la práctica, y la salida de los `sha256sum -c`.
- El guion opcional, si lo hiciste.

<!-- La mejor señal de que un formato funciona es que un compañero lo llene sin hacer una sola pregunta. Pídele a otro equipo que lo pruebe en clase. -->
