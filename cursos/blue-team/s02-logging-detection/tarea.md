# Tarea - S02 - Bitácoras y detección

**Defensa y respuesta a incidentes**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/12-seguridad/06-blue-team/02-bitacoras-y-deteccion.md` | Todo el documento: qué registrar, centralizar, anatomía de una regla, Wazuh, Sigma y pruebas | 50 min |
| `dev-notes/10-cloud/04-observabilidad.md` | Los tres pilares, logs estructurados y alertas. Anoten qué parte de la nota es de la aplicación y qué parte es del sistema operativo | 40 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Defensive Security Handbook* (Amanda Berlin, Lee Brotherston y William F. Reyor III, O'Reilly, 2024) | Cap. 21 - Understanding IDSs and IPSs. Cap. 22 - Logging and Monitoring |
| *Threat Hunting with Elastic Stack* (Andrew Pease, Packt, 2021) | Cap. 3 - Introduction to the Elastic Stack. Caps. 4 y 5 - Building Your Hunting Lab. Cap. 6 - Data Collection with Beats and the Elastic Agent. Cap. 7 - Using Kibana to Explore and Visualize Data |

Y dos lecturas cortas en línea: la técnica T1110 (Brute Force) en `attack.mitre.org/techniques/T1110/` y la página de reglas de Sigma en `sigmahq.io/docs/basics/rules.html`.

---

## Parte 2 - Entregable: tres reglas más (2.5 horas)

Escriban **tres reglas nuevas** sobre las bitácoras de su `victima` (o sobre registros de ejemplo con el formato real), distintas de las dos que vimos en clase (fallos de SSH por IP y 404 de nginx por IP). Cada una se **dispara a propósito** y se prueba **también sin el ataque**.

### Ideas (elijan tres, o propongan otras con su razón)

| Regla | Qué detecta | Técnica ATT&CK de referencia |
|---|---|---|
| Usuarios distintos por IP | Rociado de contraseñas o enumeración de usuarios | T1110.003 |
| Fallos seguidos de un éxito | Que la fuerza bruta funcionó: **un incidente** | T1110 y T1078 |
| Uso de `sudo` fuera de lo normal | Escalada o uso de una cuenta que no suele hacerlo | La que ustedes justifiquen |
| Ráfaga de 4xx o 5xx en nginx | Sondeo o fallo de la aplicación | La que ustedes justifiquen |
| Fuente en silencio | Un archivo o agente que dejó de reportar | Ninguna: es una regla de cobertura |
| Acceso fuera de horario | Un `Accepted` a una hora en que nadie trabaja | T1078 |

Al menos **una** de las tres debe usar una fuente distinta de `sshd` y de los 404 de nginx, y al menos **una** debe ser una regla de **éxito**, no solo de fallo.

### Qué lleva cada regla

Un apartado por regla en `reglas.md`, con esta plantilla:

````markdown
## Regla N - <nombre>

**Hipótesis.** Qué ataque o falla detecta y por qué importa (con su técnica de ATT&CK si aplica).

**Anatomía.**
- Fuente: ...
- Condición: ...
- Agrupación: ...
- Umbral: ...
- Ventana: ...
- Severidad: ...

**Implementación.** El script o la regla (de su SIEM o en Sigma, marcando como "fragmento" lo que no ejecutaron).

**Se dispara.** El registro de ataque (real o de ejemplo, diciendo cuál), el comando y la **salida real** de la alerta.

**Calla.** El registro normal y la salida (vacía) de la regla.

**Mutación.** Qué le cambiaron para romperla y qué prueba **cayó**.

**Medición.** Tabla con al menos tres umbrales o ventanas distintas: falsos positivos y falsos negativos de cada uno sobre sus registros, y cuál eligieron y por qué.

**Runbook.** Los primeros tres pasos para confirmar la alerta.
````

### Reglas del ejercicio

- Los registros de ejemplo se generan con un script (como en la práctica) o salen de su laboratorio. **Nada de bitácoras de sistemas ajenos.** Si el ataque es real, es contra su `victima`, con la carta de autorización del curso de Ciberseguridad.
- Cada alerta debe mostrar **las líneas que la causaron**, no solo un conteo.
- Las tres pruebas se agregan a `probar-reglas.sh` (o a un archivo de pruebas equivalente) y todas deben pasar en verde, con la mutación comprobada.
- **Ninguna regla puede depender de que el formato de fecha o el nombre de proceso sean los de su máquina** sin avisar: si no pueden leer una línea que parece relevante, la regla debe decirlo (como en la práctica).
- Lo que no hayan ejecutado se marca como fragmento. No se inventan salidas.

---

## Criterio de evaluación

Se considera completa cuando:

- [ ] Las tres reglas tienen su apartado completo.
- [ ] Cada regla trae la salida **real** de su disparo y la de su silencio sobre tráfico normal.
- [ ] Cada regla trae una mutación con la prueba que cayó.
- [ ] Cada medición compara al menos tres umbrales o ventanas, con falsos positivos y negativos.
- [ ] Una regla es de éxito (no solo de fallo) y una usa una fuente distinta de `sshd` y de los 404.
- [ ] Ninguna regla se queda ciega en silencio ante un formato que no reconoce.
- [ ] Cada regla trae un runbook de tres pasos.
- [ ] Ningún dato ni dirección es de un sistema ajeno al laboratorio.

| Criterio | Peso |
|---|---|
| Reglas disparadas, con salida real | 30 % |
| Pruebas: calla sin el ataque y mutación | 30 % |
| Medición de falsos positivos y negativos | 20 % |
| Runbook y claridad | 20 % |

---

## Qué se entrega

En el repositorio del equipo, en la carpeta `blue-team/s02/`:

- `reglas.md` (uno por equipo).
- Los scripts de las reglas y `probar-reglas.sh` actualizado.
- La bitácora y los entregables de la práctica.

---

## Preguntas frecuentes

**¿Puedo hacer las reglas en el SIEM en lugar de scripts?**
Sí: la regla en XML de Wazuh o en Kibana (si usaron Elastic) cuenta igual, con la prueba de `wazuh-logtest` o su equivalente como evidencia de que suena y de que calla.

**¿Qué hago si mi regla no suena?**
Primero, mídanlo: ¿llegó la línea? ¿la reconoce el decodificador? ¿el umbral es alcanzable con el ataque que lanzaron? Una regla que no suena es un resultado válido si explican por qué.

**¿Y si una regla suena demasiado?**
Es una medición de falsos positivos. Ajusten con sus datos normales y escriban el criterio.

**¿Puedo usar Sigma?**
Sí, pero la regla se entrega también ejecutada en algún motor (scripts, Wazuh o Elastic). Una regla Sigma sin convertir ni probar se marca como fragmento.

---

## Preparación para S03

Trae a la sesión 3:

- El laboratorio completo encendido (`kali`, `victima` y el SIEM) con las reglas de la sesión funcionando.
- La lista de los usuarios de laboratorio que existen en `victima` (`cut -d: -f1 /etc/passwd`, anotando cuáles tienen shell). La sesión 3 parte de un ingreso por SSH con una contraseña filtrada y hay que saber qué cuentas existen.
- La respuesta a esta pregunta: si la alerta de fuerza bruta de hoy sonara a las tres de la mañana, ¿quién debería enterarse y qué haría primero?
