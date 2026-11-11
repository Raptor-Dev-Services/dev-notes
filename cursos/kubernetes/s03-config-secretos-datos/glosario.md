# Glosario - S03 - Configuración, secretos y datos con estado

> Los términos de la sesión 3, en orden alfabético.

---

## A

### Aprovisionamiento dinámico
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Creación automática de un PersistentVolume cuando una PVC lo pide, según su StorageClass.
**En el curso:** `local-path` crea la carpeta cuando arranca `db-0`.

---

## B

### base64
**Categoría:** Codificación / Concepto
**Definición:** Codificación de bytes a texto. No es cifrado: se revierte con `base64 -d`. Los valores de `data` de un Secret van así.
**En el curso:** La contraseña se leyó con `base64 -d`.

---

## C

### Cifrado en reposo
**Categoría:** Kubernetes / Seguridad
**Definición:** Cifrar los datos del API antes de guardarlos en etcd. Por omisión los Secrets se guardan sin cifrar; K3s lo activa con `--secrets-encryption` al primer arranque del servidor.
**En el curso:** Se recomienda, no alcanza solo.

---

### ConfigMap
**Categoría:** Kubernetes / Configuración
**Definición:** Objeto con pares clave-valor en texto que un pod consume como variables o archivos. Límite: 1 MiB.
**En el curso:** `api-config`.

---

### CreateContainerConfigError
**Categoría:** Kubernetes / Estado de pod
**Definición:** Estado de un pod cuyo contenedor no se pudo crear por un error de configuración, por ejemplo un Secret o ConfigMap que no existe.
**En el curso:** Falla 3 de la práctica.

---

## H

### Headless Service
**Categoría:** Kubernetes / Red
**Definición:** Service con `clusterIP: None`: no tiene IP virtual; sirve para dar nombres estables a los pods de un StatefulSet.
**En el curso:** `db`.

---

## I

### immutable
**Categoría:** Kubernetes / Configuración
**Definición:** Campo (desde v1.19) que vuelve un ConfigMap o Secret imposible de modificar; solo se reemplaza.
**En el curso:** Se menciona.

---

## L

### last-applied-configuration
**Categoría:** kubectl / Anotación
**Definición:** Anotación donde `kubectl apply` guarda una copia del último manifiesto aplicado. Si el manifiesto traía un valor secreto, queda ahí en claro.
**En el curso:** Falla 1 de la práctica.

---

## M

### Modo de acceso (access mode)
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Cómo se puede montar un volumen: `ReadWriteOnce`, `ReadOnlyMany` o `ReadWriteMany`.
**En el curso:** `ReadWriteOnce` para la base.

---

## O

### Opaque
**Categoría:** Kubernetes / Secret
**Definición:** Tipo de Secret genérico, el que se usa por omisión.
**En el curso:** `db-credenciales`.

---

## P

### PersistentVolume (PV)
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Trozo de almacenamiento del clúster, independiente del pod que lo use.
**En el curso:** `kubectl get pv`.

---

### PersistentVolumeClaim (PVC)
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Petición de almacenamiento de un pod: tamaño y modo de acceso. Se une a un PV.
**En el curso:** `datos-db-0`.

---

### PGDATA
**Categoría:** PostgreSQL / Variable
**Definición:** Variable de entorno de la imagen de PostgreSQL con la ruta del directorio de datos. Se apunta a una subcarpeta del punto de montaje, como aconseja la documentación de PostgreSQL.
**En el curso:** `/var/lib/postgresql/data/pgdata`.

---

## R

### Política de reclamación (reclaim policy)
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Qué pasa con un PV cuando se borra su PVC: `Delete` borra el volumen y los datos; `Retain` los conserva.
**En el curso:** `local-path` usa `Delete`.

---

## S

### Secret
**Categoría:** Kubernetes / Configuración
**Definición:** Objeto parecido a un ConfigMap para datos sensibles. Por omisión se guarda sin cifrar en etcd; cualquiera con acceso al API puede leerlo.
**En el curso:** `db-credenciales`.

---

### StatefulSet
**Categoría:** Kubernetes / Carga de trabajo
**Definición:** Controlador para aplicaciones con estado: da a cada pod nombre ordinal estable, su propio volumen y arranque en orden. Borrarlo no borra sus volúmenes.
**En el curso:** `db`.

---

### StorageClass
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Describe cómo se crea el almacenamiento cuando una PVC lo pide: aprovisionador, política de reclamación y modo de enlace.
**En el curso:** `local-path (default)`.

---

## V

### volumeClaimTemplates
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Campo del StatefulSet con la plantilla de PVC que se crea para cada pod.
**En el curso:** `datos`.

---

### Volumen
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Directorio montado dentro de un pod; puede venir de un ConfigMap, un Secret o una PVC.
**En el curso:** `/etc/api` y `/var/lib/postgresql/data`.

---

## W

### WaitForFirstConsumer
**Categoría:** Kubernetes / Almacenamiento
**Definición:** Modo de enlace en el que el volumen no se crea hasta que un pod lo usa; una PVC `Pending` sin pod es normal.
**En el curso:** `local-path`.

---

*Rogelio Arriaga Gonzalez*
