# Tarea - S05 - Escalar y observar

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/10-cloud/04-observabilidad.md` | "Los tres pilares", "Prometheus - métricas" y "Health checks"; relaciónalos con lo que da `kubectl top` | 50 min |
| Documentación de Kubernetes, "Resource Management for Pods and Containers" | Requests, limits y qué pasa al pasarse | 20 min |
| Documentación de Kubernetes, "Horizontal Pod Autoscaling" | El algoritmo, la tolerancia y la estabilización | 30 min |
| Documentación de Kubernetes, "Pod Quality of Service Classes" | Las tres clases y el orden de desalojo | 20 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Kubernetes Recipes* (Stencel) | Cap. 6 - Scaling and Resiliency; cap. 9 - Performance Observability in Kubernetes |
| *Kubernetes: Up and Running* (Burns et al., 2022) | Cap. 11 - DaemonSets; cap. 12 - Jobs |
| *CKA Study Companion* (Sachdeva, 2025) | Cap. 8 - DaemonSet, CronJob y Job; cap. 10 - Autoscaling in Kubernetes |

---

## Parte 2 - Ejercicio (2 horas)

### La gráfica de su prueba, con su conclusión

Repitan la prueba de carga de la práctica **con su propia aplicación** (la API de su proyecto, o la de la práctica si no tienen una) y entreguen un documento `docs/prueba-de-carga.md`.

#### 1. La pregunta

Una pregunta que la prueba responde. Por ejemplo: "¿cuántas réplicas necesita mi API para sostener N peticiones por segundo con latencia menor a X ms?". Una pregunta sin número no se puede contestar.

#### 2. La preparación

- Los `requests` y `limits` de su aplicación, y **de dónde salió cada número** (una medición previa o una estimación declarada como tal).
- La configuración del HPA (mínimo, máximo, objetivo) y por qué.
- La línea base: CPU, memoria y tiempo de respuesta sin carga.

#### 3. La carga

Cómo la generaron (herramienta, parámetros, duración), y **cuántas veces** la repitieron. Una corrida sola no basta: entreguen al menos dos.

#### 4. La gráfica

**Una** gráfica clara, con el tiempo en el eje horizontal, las réplicas y la CPU como series, y marcados el inicio y fin de la carga, el objetivo y `maxReplicas`. Si miden latencia, una segunda gráfica.

#### 5. La conclusión

Una frase que alguien pueda desmentir con los datos, y debajo:

- Lo que la prueba **demuestra**.
- Lo que **no** demuestra (el cuello de botella real, otras cargas, otros nodos).
- Qué cambiarían de `requests`, `limits` o HPA con lo aprendido.

Toda cifra de la documentación que usen (tolerancia, ventanas, fórmula) lleva su fuente.

---

## Entregable

- `docs/prueba-de-carga.md` con las cinco secciones.
- El `medicion.csv` (o los datos que usaron) y el script que generó la gráfica.
- Una línea: "la decisión que tomo distinto después de esta prueba es ...".

---

## Criterio de evaluación

| Aspecto | Peso | Qué se busca |
|---------|------|--------------|
| Pregunta y preparación | 20 % | Pregunta con número; `requests` y `limits` justificados |
| Medición | 25 % | Línea base, al menos dos corridas, datos sin editar |
| Gráfica | 20 % | Legible, con ejes, marcas de carga, objetivo y techo |
| Conclusión desmentible | 25 % | Se sostiene con los datos; dice lo que no demuestra |
| Fuentes | 10 % | Las cifras de la documentación citan su fuente |

Una conclusión que el dato contradice se califica a la baja; un resultado "malo" (no escaló, se topó, se murió por memoria) **bien explicado** se califica alto.

---

## Para la próxima sesión

Lleven el clúster limpio y su runbook en borrador (lo que hacen hoy cuando algo falla). En la sesión 6 se siembran cinco fallas y se diagnostican con un método.
