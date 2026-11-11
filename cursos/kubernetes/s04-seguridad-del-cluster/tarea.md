# Tarea - S04 - Seguridad del clúster

**Kubernetes: del Compose al clúster**
**Tiempo estimado:** 4 horas

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| `dev-notes/08-contenedores/08-kubernetes-seguridad.md` | Todo el documento, con el clúster abierto | 60 min |
| Documentación de Kubernetes, "Using RBAC Authorization" | Role, ClusterRole, bindings y "Referring to resources" | 25 min |
| Documentación de Kubernetes, "Pod Security Standards" y "Pod Security Admission" | Los tres perfiles y los tres modos | 20 min |
| Documentación de Kubernetes, "Network Policies" | Aislamiento, políticas aditivas y comportamiento por omisión | 15 min |

### Recomendadas

| Libro | Capítulos |
|-------|-----------|
| *Kubernetes: Up and Running* (Burns et al., 2022) | Cap. 14 - Role-Based Access Control for Kubernetes; cap. 19 - Securing Applications in Kubernetes; cap. 20 - Policy and Governance for Kubernetes Clusters |
| *Kubernetes Recipes* (Stencel) | Cap. 11 - Security in Kubernetes |
| *Policy as Code* (Ray, 2024) | Cap. 4 - Policy as Code and Kubernetes; cap. 7 - OPA/Gatekeeper and Kubernetes; cap. 8 - Kyverno and Kubernetes |

---

## Parte 2 - Ejercicio (2 horas)

### La matriz de quién puede hacer qué en su clúster

Escriban `docs/matriz-de-permisos.md` para el clúster de su proyecto (o el de la práctica, si todavía no tienen uno). Es el documento que un auditor o una persona nueva leería para saber quién tiene qué poder.

#### 1. Identidades

Una lista de **todas** las identidades que hablan con el API: personas, grupos, cuentas de servicio y el pipeline de despliegue. Para cada una: qué es y para qué la necesitan.

#### 2. La matriz

Una tabla con una fila por identidad y una columna por recurso sensible: `pods`, `pods/exec`, `deployments`, `secrets`, `configmaps`, `namespaces`, y las que su proyecto agregue. Cada celda con el **verbo más alto** que tiene (`ninguno`, `leer`, `crear`, `todo`) y **cómo se comprobó**: el comando `kubectl auth can-i ... --as=...`.

#### 3. Los permisos que sobran

Con la matriz delante, una lista de permisos que hoy **no deberían existir** (por ejemplo, una cuenta con `get secrets` que no lo necesita) y qué los quitaría. Si la lista está vacía, expliquen por qué no creen que sobre nada.

#### 4. Las cuatro pruebas de rechazo

Adjunten `pruebas-de-rechazo.md` de la práctica, con una mejora: una **quinta** prueba de rechazo propia, para una regla que su proyecto necesite (por ejemplo, "todo pod con `limits` de memoria", o "ningún Service de tipo `NodePort`"). Escriban la política, la acción que debe fallar, y la salida real. Una política que no se vio rechazar no se entrega.

#### 5. Revisión

Qué se revisa, con qué frecuencia y quién lo revisa. Un documento de permisos sin revisión se queda viejo.

Toda afirmación sobre el comportamiento de una herramienta lleva su fuente (documentación, capítulo de libro) o la salida del comando que la demuestra.

---

## Entregable

- `docs/matriz-de-permisos.md`, con las cinco secciones.
- `pruebas-de-rechazo.md` con las cuatro pruebas de la práctica y la quinta.
- Una frase honesta: "lo que mi matriz **no** cubre todavía es ...".

---

## Criterio de evaluación

| Aspecto | Peso | Qué se busca |
|---------|------|--------------|
| Identidades y matriz | 25 % | Completas, con el verbo más alto por celda y el comando que lo comprueba |
| Permisos que sobran | 20 % | Hallazgos reales y un plan para quitarlos |
| Cuatro pruebas de rechazo | 25 % | Se ve cada rechazo con su salida; sin capturas, con texto |
| Quinta prueba propia | 20 % | La política es útil al proyecto y se vio rechazar |
| Revisión y límites | 10 % | Frecuencia, responsable y lo que no cubre |

Una prueba de rechazo cuya salida muestra que **no** rechazó nada se califica como hallazgo: corregir la política vale más que escribirla bien.

---

## Para la próxima sesión

Lleven su clúster con sus manifiestos de las sesiones anteriores. En la sesión 5 se agregan peticiones y límites, autoescalado y una prueba de carga medida.
