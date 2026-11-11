# Tarea - S01 - Del contenedor al pod

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/08-contenedores/03-compose.md` | Todo el documento, con el `compose.yaml` de su proyecto abierto al lado | 40 min |
| `dev-notes/08-contenedores/06-kubernetes-conceptos.md` | Todo el documento | 50 min |
| Documentación de Kubernetes, "Kubernetes Components" (kubernetes.io) | Control plane y componentes de nodo | 15 min |
| Documentación de Kubernetes, "Labels and Selectors" (kubernetes.io) | Sintaxis de selectores por igualdad y por conjunto | 15 min |

### Recomendadas (para quien quiere profundizar)

| Libro | Capítulos |
|-------|-----------|
| *Kubernetes: Up and Running* (Burns, Beda, Hightower y Evenson, 2022) | Caps. 1 a 6: introducción, contenedores, despliegue de un clúster, comandos de `kubectl`, pods, etiquetas y anotaciones |
| *Certified Kubernetes Administrator Study Companion* (Sachdeva, 2025) | Caps. 1 a 4: el problema que resuelve, arquitectura, instalación con KinD y pods |

---

## Parte 2 - Ejercicio (2 horas)

### El ADR de su proyecto: Compose o Kubernetes

Escriban `docs/adr/001-compose-o-kubernetes.md` en el repositorio de su proyecto (o en la carpeta `~/k8s-lab` si el proyecto no es de ustedes). Es una nota corta; no se califica la extensión sino el **criterio**.

#### Estructura obligatoria

1. **Contexto.** Qué aplicación es, cuántos servicios tiene, dónde corre hoy, cuántas personas la operan y con qué frecuencia publican.
2. **Opciones.** **Dos opciones reales** (no una real y un espantapájaros). Por ejemplo: (A) Docker Compose en un servidor; (B) Kubernetes administrado; (C) K3s en un servidor propio. Elijan las dos que de verdad estén sobre la mesa de su proyecto.
3. **Criterios.** Al menos cinco, tomados de la tabla de decisión de la clase: disponibilidad entre servidores, frecuencia de despliegue, autoescalado, equipos que despliegan, quién sabe operar el clúster. Pueden agregar los suyos (costo, cumplimiento).
4. **Evaluación.** Una tabla con cada opción contra cada criterio, y una frase por celda que diga **por qué**, no solo "mejor" o "peor".
5. **Decisión y consecuencias.** Qué se elige y qué se acepta a cambio (el riesgo que queda).
6. **Cuándo se revisa.** Una condición **medible** que haría cambiar la decisión (por ejemplo, "si necesitamos un segundo servidor por disponibilidad").

#### Anexo: la traducción de su compose.yaml

Tomen el `compose.yaml` real de su proyecto y traduzcan **un servicio** a un pod (como en la práctica). Entreguen el YAML y una lista de tres líneas del compose que **no** se pudieron traducir y por qué.

Todo valor que cite debe tener su fuente (la documentación o el libro con capítulo) o decir que es una estimación suya.

---

## Entregable

- `docs/adr/001-compose-o-kubernetes.md`, con el anexo.
- El YAML del pod, validado con `kubectl apply --dry-run=client -f pod.yaml` (copien la salida al final del ADR).
- Su respuesta, en una línea, a: "si mañana mi proyecto necesitara Kubernetes, ¿cuál sería la primera señal?".

---

## Criterio de evaluación

| Aspecto | Peso | Qué se busca |
|---------|------|--------------|
| Dos opciones reales | 20 % | Ambas son viables para el proyecto y están explicadas con honestidad |
| Criterios y evaluación | 30 % | Cinco criterios o más; cada celda justificada, no solo calificada |
| Decisión y consecuencias | 20 % | La decisión sigue de la evaluación; dice qué riesgo se acepta |
| Condición de revisión | 10 % | Medible, no un "algún día" |
| Anexo del pod | 20 % | El YAML pasa `--dry-run=client`; las líneas no traducidas están bien explicadas |

Una decisión de **no** usar Kubernetes con buenos argumentos vale lo mismo que una de usarlo. Lo que no vale es una decisión sin argumentos.

---

## Para la próxima sesión

Lleven su ADR y el pod del anexo. En la sesión 2 el pod suelto se convierte en un Deployment con Service e Ingress.
