# Tarea - S06 - Operar y diagnosticar

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/08-contenedores/09-kubernetes-diagnostico.md` | Todo el documento, con las cinco fallas de la práctica a la mano | 60 min |
| Documentación de Kubernetes, "Debug Pods" y "Debug Running Pods" | Los pasos de depuración de un pod y `kubectl debug` | 25 min |
| Documentación de Kubernetes, "Version Skew Policy" | Qué versiones pueden convivir | 15 min |
| Documentación de Kubernetes, "Upgrading kubeadm clusters" | El orden de la actualización y el `drain` | 20 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Certified Kubernetes Administrator Study Companion* (Sachdeva, 2025) | Parte V: cap. 18 - Cluster Maintenance y cap. 19 - Kubernetes Version Upgrade Using Kubeadm. Parte VI: cap. 20 - Monitoring, Logging, and Alerting; cap. 21 - Troubleshooting Application Failure; cap. 22 - Troubleshooting Control Plane Failure |
| *Kubernetes Recipes* (Stencel) | Cap. 10 - Control Plane Administration and Package Management; cap. 13 - Best Practices in Kubernetes |

---

## Parte 2 - Ejercicio (2 horas)

### El runbook de diagnóstico de su aplicación

Terminen `docs/runbook.md` (el borrador de la práctica) y **pruébenlo con otro equipo**.

#### 1. Estructura

Para **su** aplicación (la API de su proyecto o la del proyecto final), con las secciones de la clase:

- **Síntomas:** al menos cinco síntomas distintos que un usuario o una alerta podrían reportar (por ejemplo, "responde 502", "tarda más de lo normal", "no se pueden subir archivos").
- **Primera lectura:** para cada síntoma, el primer comando y qué buscar en su salida.
- **Árbol de decisión:** si la salida dice A, siguiente paso X; si dice B, siguiente paso Y.
- **Causas conocidas:** las que ya vieron en las prácticas más las propias de su aplicación (base de datos caída, secreto vencido, migración fallida...), con su arreglo.
- **Cuándo escalar:** a quién se avisa y con qué datos.
- **Cómo regresar:** el comando exacto y qué se pierde.
- **Última revisión:** fecha y quién lo probó.

#### 2. La prueba con otro equipo

Intercambien con otro equipo: cada uno **siembra una falla escondida** en el clúster del otro (una de las cinco de la práctica, o una propia, **sin decir cuál**) y el otro la encuentra **siguiendo solo el runbook**.

Registren, para cada prueba:

- La falla sembrada (la dice quien la sembró, después).
- Los comandos que corrió quien diagnosticó, en orden, y cuánto tardó.
- Dónde el runbook ayudó y dónde **faltaba algo**.
- El cambio que le hicieron al runbook por eso.

#### 3. Qué se actualiza

Una sección corta de "mantenimiento" con: qué versión de Kubernetes usa su clúster, hasta cuándo recibe parches **según kubernetes.io** (con la fecha en que consultaron), y qué haría su equipo para actualizar sin saltarse versiones menores.

Todo comando o comportamiento que cite lleva su fuente o la salida que lo demuestra.

---

## Entregable

- `docs/runbook.md` terminado y probado.
- El registro de la prueba con otro equipo (`docs/prueba-runbook.md`): falla sembrada, comandos, tiempo, cambios.
- La sección de mantenimiento, con la fecha de consulta de las versiones.
- Una línea: "la falla que mi runbook **no** cubre todavía es ...".

---

## Criterio de evaluación

| Aspecto | Peso | Qué se busca |
|---------|------|--------------|
| Síntomas y primera lectura | 25 % | Al menos cinco; cada uno con su comando y qué buscar |
| Árbol de decisión y causas | 25 % | Ramas claras; causas propias de su aplicación, no solo genéricas |
| Prueba con otro equipo | 30 % | Hay falla sembrada, comandos reales, tiempos y cambios hechos al runbook por lo aprendido |
| Mantenimiento y versiones | 10 % | Fechas y fuente de la consulta; no se salta versiones |
| Honestidad | 10 % | Dice lo que el runbook no cubre |

Un runbook al que le faltó algo en la prueba y **lo corrigieron** vale más que uno que "funcionó a la primera" sin evidencia.

---

## Proyecto final

Con esta tarea termina el curso. El proyecto final reúne todo: la API de Titulación desplegada en el clúster con manifiestos versionados, políticas que se vieron rechazar, autoescalado medido y este runbook. Se califica cuando otro equipo le siembra una falla y la encuentran con su runbook.
