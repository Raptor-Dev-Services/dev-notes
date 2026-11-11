# Glosario - S06 - Operar y diagnosticar

> Los términos de la sesión 6, en orden alfabético.

---

## C

### Contenedor efímero
**Categoría:** Kubernetes / Depuración
**Definición:** Contenedor temporal que `kubectl debug` agrega a un pod en marcha; comparte su red y, con `--target`, sus procesos.
**En el curso:** `kubectl debug -it pod/... --image=busybox:1.37`.

---

### cordon / uncordon
**Categoría:** kubectl / Mantenimiento
**Definición:** `kubectl cordon <nodo>` marca un nodo como no programable: no recibe pods nuevos y los que corren siguen. `uncordon` revierte.
**En el curso:** En un nodo único dejó los pods nuevos en `Pending`.

---

### CrashLoopBackOff
**Categoría:** Kubernetes / Estado de pod
**Definición:** El contenedor arranca, muere y el kubelet espera cada vez más antes de reintentar. Es un síntoma; la causa está en los logs y el código de salida.
**En el curso:** Falla 4.

---

## D

### drain
**Categoría:** kubectl / Mantenimiento
**Definición:** `kubectl drain <nodo>` saca ordenadamente los pods de un nodo antes de darle mantenimiento. La documentación de kubeadm exige drenar un nodo antes de actualizar su kubelet en una actualización de versión menor.
**En el curso:** Mencionado; no se practica en un solo nodo.

---

## E

### Evento (de pod)
**Categoría:** Kubernetes / Diagnóstico
**Definición:** Registro breve de algo que le pasó a un objeto. La sección `Events` de `kubectl describe` es lo primero que se lee.
**En el curso:** `FailedScheduling`, `Failed`, `Unhealthy`.

---

## F

### FailedScheduling
**Categoría:** Kubernetes / Eventos
**Definición:** Evento del scheduler cuando no encuentra nodo para un pod: sin CPU suficiente, nodo no programable, PVC sin unir.
**En el curso:** Falla 3 y los dos extras.

---

## H

### Helm
**Categoría:** Kubernetes / Paquetes
**Definición:** Gestor de paquetes de Kubernetes. Un paquete es un *chart*; instalar un chart crea un *release*.
**En el curso:** Se explica; no se instala.

---

## K

### Kustomize
**Categoría:** Kubernetes / Manifiestos
**Definición:** Herramienta integrada en `kubectl` para combinar una base de manifiestos con *overlays* por entorno, sin plantillas.
**En el curso:** `kubectl apply -k`.

---

## O

### Overlay
**Categoría:** Kustomize / Concepto
**Definición:** Carpeta con lo que cambia respecto a una base: nombre, réplicas, imagen.
**En el curso:** `overlays/produccion`.

---

## P

### Pending
**Categoría:** Kubernetes / Estado de pod
**Definición:** Pod aceptado pero sin nodo asignado, o sin sus recursos. La causa está en los eventos del scheduler y de lo que el pod pide (PVC, recursos).
**En el curso:** Falla 3.

---

### Política de versiones (version skew)
**Categoría:** Kubernetes / Mantenimiento
**Definición:** Regla de qué versiones pueden convivir: el kubelet no puede ser más nuevo que el API y puede estar hasta tres versiones menores atrás; `kubectl`, a una versión menor.
**En el curso:** Se actualiza el control plane primero.

---

## R

### readyz
**Categoría:** Kubernetes / API
**Definición:** Endpoint del API (`/readyz?verbose`) que lista sus comprobaciones de salud una por una.
**En el curso:** `kubectl get --raw='/readyz?verbose'`.

---

### Runbook
**Categoría:** Operación / Documento
**Definición:** Documento que otra persona sigue, sin ti, cuando algo falla: síntoma, primera lectura, árbol de decisión, causas, escalamiento, regreso y fecha de revisión.
**En el curso:** La tarea.

---

## S

### Selector que no empata
**Categoría:** Kubernetes / Diagnóstico
**Definición:** Un Service cuyo `selector` no coincide con las etiquetas de ningún pod: sus EndpointSlices quedan vacíos. No produce error.
**En el curso:** Falla 2.

---

### Siembra de fallas
**Categoría:** Método del curso
**Definición:** Introducir a propósito una falla en un entorno propio para probar un runbook.
**En el curso:** La prueba con otro equipo.

---

## V

### Versión menor (MINOR)
**Categoría:** Kubernetes / Versiones
**Definición:** El número del medio de una versión (1.35, 1.36, 1.37). La documentación de kubeadm no admite saltarse versiones menores al actualizar.
**En el curso:** Una a la vez.

---

*Rogelio Arriaga Gonzalez*
