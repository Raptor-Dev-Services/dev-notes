# Tarea - S05 - Fichas de cacería

**Defensa y respuesta a incidentes**
**Tiempo estimado:** 3 horas

---

## Parte 1 - Lecturas (1 hora)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/12-seguridad/06-blue-team/05-threat-hunting-attack.md` | Completa | 30 min |
| Las páginas de ATT&CK de **T1136.001**, **T1053.003** y **T1098.004** (attack.mitre.org) | Descripción, mitigaciones y "Detection" de cada una | 30 min |

Mientras leen las estrategias de detección de cada técnica, subrayen **qué dato** pide cada analítica y respondan: ¿ese dato existe en `victima`?

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Practical Threat Intelligence and Data-Driven Threat Hunting* (Valentina Palacín, Packt, 2021) | Cap. 2 (el proceso de cacería, el modelo de madurez y cómo construir una hipótesis). Cap. 4 (mapear al adversario con ATT&CK). Cap. 8 y 9 (consultar los datos y cazar al adversario). Cap. 10 (documentar y automatizar) |
| *Adversary Emulation with MITRE ATT&CK* (Drinor Selmanaj, O'Reilly, 2024) | Cap. 3 (ATT&CK y estrategias de defensa informada por amenazas). Cap. 5 (uso real de las técnicas, incluida la modificación de llaves SSH autorizadas). Cap. 6 (el Navigator y las capas) |

---

## Parte 2 - Entregable: las fichas de cacería (2 horas)

Entreguen `ficha-cacerias.md` en el repo de su equipo con **cuatro fichas**: las tres técnicas de la práctica (T1136.001, T1053.003 y T1098.004) y **una cuarta con resultado vacío** (puede ser la de T1021.004 de la práctica u otra que elijan, con su técnica).

Cada ficha lleva **exactamente** estos campos:

| Campo | Qué va |
|---|---|
| Título | Una frase |
| Hipótesis | Una frase que se pueda refutar, escrita **antes** de la consulta |
| Qué la refutaría | El resultado que haría que descartes la hipótesis |
| Técnica | Nombre, ID y versión de ATT&CK consultada, con la fecha de consulta |
| Dato que lo mostraría | Fuente, archivo o campo; si no existe en su laboratorio, dígalo |
| Consulta | El comando o la consulta **exacta**, con su ventana de tiempo |
| Resultado | La salida real (recortada si es larga) |
| Lectura | Qué significa: qué fue legítimo, qué es sospechoso y por qué |
| Qué NO cubre | Qué conducta de la misma técnica esta consulta no vería |
| Siguiente paso | Incidente, línea base, mejora de datos o regla nueva |

### Ejemplo de la forma de una ficha

> **Hipótesis:** alguien con la cuenta de soporte creó una cuenta local para volver. **Qué la refutaría:** que toda cuenta nueva tenga un cambio aprobado. **Técnica:** T1136.001, Create Account: Local Account, v1.5, consultada el 2026-11-11...

(Es una forma, no una respuesta: redacten la suya con **sus** datos.)

### Una capa de cobertura

Añadan a su entrega **una tabla o una capa del ATT&CK Navigator** (exportada como JSON) con dos conjuntos de técnicas, mínimo cinco cada uno:

1. Técnicas que **sí pueden ver** hoy en su laboratorio, y con qué dato.
2. Técnicas que les preocupan y **no pueden ver**, y qué dato les falta.

Para cada técnica del segundo grupo, una línea con la mejora que propondrían (por ejemplo, "auditd sobre `authorized_keys`").

### Una cacería nueva (opcional, suma puntos extra)

Elijan una técnica de ATT&CK **distinta** a las de la práctica que sea relevante para Linux, escriban su hipótesis, consulten qué dato pide su analítica y digan si pueden cazarla con lo que tienen. No hace falta emularla.

Quien quiera emular con **Atomic Red Team** puede ejecutar la prueba 1 de T1136.001 con `Invoke-AtomicTest`, solo en su laboratorio y con limpieza; debe contar qué diferencias vio frente a la emulación a mano.

---

## Criterio de aceptación

Su tarea se considera completa cuando:

- [ ] Hay cuatro fichas, cada una con **todos** los campos.
- [ ] Cada hipótesis está escrita de forma que **se puede refutar**, y dice qué la refutaría.
- [ ] Cada consulta está completa y **otra persona puede correrla** sobre los mismos datos y obtener el mismo resultado.
- [ ] Las técnicas citan **ID y versión** de ATT&CK, con fecha de consulta.
- [ ] La ficha de resultado vacío explica qué fuente falta y **no** declara el entorno "limpio".
- [ ] Hay una tabla o capa de cobertura con al menos 5 técnicas visibles y 5 no visibles.
- [ ] Ningún dato real de personas o sistemas ajenos aparece en el documento.

---

## Cómo se evalúa

| Criterio | Peso |
|---|---|
| Hipótesis refutables y consultas repetibles | 35 % |
| Lectura honesta de resultados, incluida la ficha vacía | 25 % |
| Uso correcto de ATT&CK (ID, versión, analítica) | 20 % |
| Cobertura: lo que se ve y lo que no | 15 % |
| Claridad de la redacción | 5 % |

<!-- Califiquen repitiendo una consulta de cada equipo sobre su propio registro. Una consulta que no se puede repetir pierde la mitad del primer criterio. -->
