# Tarea - S02 - Deployments, Services e Ingress

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/08-contenedores/07-kubernetes-manifiestos.md` | Todo el documento, con el clúster abierto | 60 min |
| Documentación de Kubernetes, "Deployments" (kubernetes.io) | "Updating a Deployment", "Rolling Back a Deployment" y "Strategy" | 25 min |
| Documentación de Kubernetes, "Configure Liveness, Readiness and Startup Probes" | Las tres probes y sus campos | 20 min |
| Documentación de Kubernetes, "Service" e "Ingress" | Tipos de Service y "Ingress" desde el inicio | 15 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Kubernetes: Up and Running* (Burns et al., 2022) | Cap. 7 - Service Discovery; cap. 8 - HTTP Load Balancing with Ingress; cap. 9 - ReplicaSets; cap. 10 - Deployments |
| *Kubernetes Recipes* (Stencel) | Cap. 2 - Configuring Stateless Applications |
| *CKA Study Companion* (Sachdeva, 2025) | Cap. 5 - ReplicaSets, Replication Controller y Deployments; cap. 6 - Services |

---

## Parte 2 - Ejercicio (2 horas)

### Manifiestos versionados, sin kubectl a mano

Tomen la aplicación de su proyecto (o la API y el front de la práctica) y entreguen una carpeta `k8s/` en su repositorio con **todo lo necesario para publicarla**:

1. **Un Deployment por componente** con `replicas`, versión explícita de imagen (nunca `latest`), `readinessProbe`, `livenessProbe` y la estrategia declarada, aunque sea la de omisión.
2. **Un Service** por cada Deployment al que algo deba llamar por nombre.
3. **Un Ingress** (o una explicación, si su proyecto solo tiene una API interna, de por qué no lo lleva).
4. Un archivo `k8s/README.md` con el **orden de aplicación** y los comandos para publicar y para regresar.

#### Reglas

- Ningún cambio al clúster que no esté en un commit. Si necesitaron un `kubectl edit` para descubrir algo, vuelvan a escribirlo en el archivo.
- Antes de cada `apply`, `kubectl diff` y `--dry-run=server`. Copien la salida de ambos en el README, para una publicación.
- Un commit por cambio. El historial debe mostrar al menos: el despliegue inicial, la actualización de versión y el arreglo de la probe rota.

#### Las dos pruebas que tienen que mostrar

1. **Actualización sin cortar:** el ciclo de `curl` de la práctica corriendo durante una actualización de versión, con su salida pegada. Si apareció algún error durante el cambio, explíquenlo; no lo escondan.
2. **Probe rota y regreso:** el despliegue detenido por una readiness mal escrita, su evento (`Readiness probe failed`), el `rollout undo` y el commit que corrige el archivo.

Todo dato que cite (valores por omisión, comportamiento de un campo) lleva su fuente o la salida del comando que lo demuestra.

---

## Entregable

- Carpeta `k8s/` con los manifiestos y su README.
- Las dos pruebas, con salida pegada (texto, no capturas).
- Respuesta de cinco líneas: "en mi proyecto, ¿la liveness y la readiness deben comprobar lo mismo? ¿Por qué?".

---

## Criterio de evaluación

| Aspecto | Peso | Qué se busca |
|---------|------|--------------|
| Manifiestos correctos | 30 % | Pasan `--dry-run=server`; imágenes con versión; probes y estrategia declaradas |
| Sin `kubectl` a mano | 20 % | El historial de Git cuenta la historia; el clúster coincide con el repositorio (`kubectl diff` sin diferencias) |
| Actualización sin cortar | 20 % | Hay evidencia del ciclo de `curl` y se interpreta con honestidad |
| Probe rota y regreso | 20 % | Se ve el evento y el `undo`; el archivo del repositorio quedó corregido |
| Respuesta liveness vs readiness | 10 % | Distingue las dos preguntas y evita que la liveness dependa de terceros |

---

## Para la próxima sesión

Lleven su `k8s/` aplicado en su clúster. En la sesión 3 se agrega configuración, un Secret y una base con volumen.
