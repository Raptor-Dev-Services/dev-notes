# Glosario - S01 - Del contenedor al pod

> Los términos de la sesión 1, en orden alfabético.

---

## A

### Anotación (annotation)
**Categoría:** Kubernetes / Metadatos
**Definición:** Par clave-valor en `metadata.annotations` que guarda información para personas y herramientas (el repositorio de origen, quién desplegó). A diferencia de una etiqueta, **no** sirve para seleccionar objetos.
**En el curso:** `kubectl annotate pod db repo=...` en el paso 9 de la práctica.

---

### API de Kubernetes
**Categoría:** Kubernetes / Arquitectura
**Definición:** Interfaz HTTP del `kube-apiserver` por la que `kubectl`, el scheduler, el kubelet y los controladores leen y escriben los objetos del clúster. Todo pasa por ella.
**En el curso:** `kubectl` habla con la API, nunca directamente con los nodos.

---

### apply
**Categoría:** kubectl / Comando
**Definición:** `kubectl apply -f archivo.yaml` lleva el clúster a lo que dice el archivo: crea el objeto si no existe y actualiza lo que cambió. Es la forma declarativa de trabajar.
**En el curso:** con `apply` se crean los pods `db` y `api`.

---

## C

### Clúster
**Categoría:** Kubernetes / Arquitectura
**Definición:** Conjunto de máquinas (nodos) administradas como una sola plataforma por un control plane.
**En el curso:** el clúster de laboratorio es un solo nodo de K3s.

---

### Contexto (context)
**Categoría:** kubectl / Configuración
**Definición:** Combinación de clúster, usuario y namespace guardada en el kubeconfig. `kubectl config use-context` cambia a cuál clúster le hablas.
**En el curso:** antes de un comando que cambia algo se revisa `kubectl config current-context`.

---

### Control plane
**Categoría:** Kubernetes / Arquitectura
**Definición:** Conjunto de componentes que deciden y guardan el estado del clúster: `kube-apiserver`, `etcd`, `kube-scheduler` y `kube-controller-manager` (y, si hay nube, `cloud-controller-manager`).
**En el curso:** en K3s corre en el mismo nodo que las aplicaciones.

---

### Controlador (controller)
**Categoría:** Kubernetes / Arquitectura
**Definición:** Bucle que compara el estado deseado de un objeto con el estado real y actúa para acercarlos. Los controladores integrados corren dentro del `kube-controller-manager`.
**En el curso:** es lo que le falta a un pod suelto: sin él, nadie lo repone.

---

## D

### Declarativo
**Categoría:** Kubernetes / Concepto
**Definición:** Estilo en el que describes el estado que quieres y el sistema decide los pasos, en contraste con el imperativo, donde das las órdenes una por una.
**En el curso:** el YAML del pod describe lo que quieres; `kubectl run` era la versión imperativa.

---

### describe
**Categoría:** kubectl / Comando
**Definición:** `kubectl describe <tipo> <nombre>` muestra el detalle de un objeto y, al final, sus **eventos**.
**En el curso:** es lo primero que se lee cuando un pod no arranca.

---

## E

### etcd
**Categoría:** Kubernetes / Arquitectura
**Definición:** Almacén clave-valor consistente donde la API guarda todo el estado del clúster.
**En el curso:** se menciona como el origen de por qué un Secret no está cifrado por omisión (sesión 3).

---

### Evento (event)
**Categoría:** Kubernetes / Diagnóstico
**Definición:** Registro breve de algo que le pasó a un objeto (programado, imagen descargada, falló). Se lee con `kubectl describe` o `kubectl get events`.
**En el curso:** el evento `Failed to pull image ... not found` explica el `ImagePullBackOff`.

---

### exec
**Categoría:** kubectl / Comando
**Definición:** `kubectl exec <pod> -- <comando>` corre un comando dentro de un contenedor del pod.
**En el curso:** se usa para consultar la base y para probar la red desde el pod `cliente`.

---

## I

### ImagePullBackOff
**Categoría:** Kubernetes / Estado de pod
**Definición:** Estado que muestra un pod cuando el kubelet no pudo descargar la imagen y espera cada vez más antes de reintentar. Es el síntoma; la causa (etiqueta inexistente, registry sin acceso) está en los eventos.
**En el curso:** falla 2 de la práctica, con la etiqueta `v9.9.9`.

---

## K

### K3s
**Categoría:** Kubernetes / Distribución
**Definición:** Distribución de Kubernetes ligera, empaquetada como un solo binario, que trae Traefik, CoreDNS, ServiceLB, kube-router y local-path-provisioner.
**En el curso:** el clúster de todas las prácticas.

---

### kind
**Categoría:** Kubernetes / Herramienta
**Definición:** Herramienta para correr clústeres locales de Kubernetes usando contenedores de Docker como nodos. Fue diseñada sobre todo para probar Kubernetes mismo, y sirve también para desarrollo local y CI.
**En el curso:** alternativa a K3s.

---

### kubectl
**Categoría:** kubectl / Herramienta
**Definición:** Cliente de línea de comandos de Kubernetes.
**En el curso:** la herramienta de todas las sesiones.

---

### kube-apiserver
**Categoría:** Kubernetes / Arquitectura
**Definición:** Componente del control plane que expone la API HTTP de Kubernetes.
**En el curso:** punto por el que pasa todo comando.

---

### kube-proxy
**Categoría:** Kubernetes / Arquitectura
**Definición:** Componente de nodo que mantiene reglas de red para implementar los Services.
**En el curso:** lo retomamos en la sesión 2.

---

### kube-scheduler
**Categoría:** Kubernetes / Arquitectura
**Definición:** Componente del control plane que asigna cada pod sin nodo a un nodo adecuado.
**En el curso:** `Successfully assigned default/web to <nodo>` en los eventos viene de él.

---

### kubelet
**Categoría:** Kubernetes / Arquitectura
**Definición:** Agente que corre en cada nodo y se asegura de que los pods asignados a él y sus contenedores estén corriendo.
**En el curso:** descarga las imágenes y arranca los contenedores; sus mensajes son los eventos `Pulling`, `Created` y `Started`.

---

## L

### Etiqueta (label)
**Categoría:** Kubernetes / Metadatos
**Definición:** Par clave-valor en `metadata.labels` con el que otros objetos seleccionan. Es el mecanismo de conexión de Kubernetes.
**En el curso:** `app=db`, `app=api`, `entorno=lab`.

---

## N

### Namespace
**Categoría:** Kubernetes / Organización
**Definición:** Agrupación lógica de objetos dentro de un clúster. Un clúster nuevo trae `default`, `kube-system`, `kube-public` y `kube-node-lease`.
**En el curso:** los pods de la práctica viven en `default`; los de K3s, en `kube-system`.

---

### Nodo (node)
**Categoría:** Kubernetes / Arquitectura
**Definición:** Máquina, física o virtual, que corre pods.
**En el curso:** `kubectl get nodes`.

---

## O

### Orquestador
**Categoría:** Contenedores / Concepto
**Definición:** Sistema que decide dónde corre cada contenedor, lo reemplaza si falla y lo escala, a partir de un estado declarado.
**En el curso:** Kubernetes es uno; Compose, en un solo anfitrión, no llega a serlo.

---

## P

### Pod
**Categoría:** Kubernetes / Carga de trabajo
**Definición:** Unidad mínima desplegable: uno o más contenedores que comparten red y almacenamiento, con una IP propia dentro del clúster.
**En el curso:** `db`, `api` y `cliente`.

---

### port-forward
**Categoría:** kubectl / Comando
**Definición:** `kubectl port-forward pod/<nombre> 8080:80` abre un túnel entre un puerto local y uno del pod. Sirve para probar, no para publicar.
**En el curso:** paso 5, para ver la respuesta de la API.

---

## S

### Selector
**Categoría:** Kubernetes / Metadatos
**Definición:** Expresión que elige objetos por sus etiquetas, por igualdad (`app=web`) o por conjunto (`app in (api,db)`).
**En el curso:** `kubectl get pods -l app=db`.

---

### spec y status
**Categoría:** Kubernetes / Objetos
**Definición:** `spec` es el estado deseado, lo escribe el usuario; `status` es el estado observado, lo escribe el sistema.
**En el curso:** `kubectl get pod db -o yaml` muestra ambos.

---

## V

### Verlo fallar
**Categoría:** Método del curso
**Definición:** Romper algo a propósito y leer el error, para reconocerlo después cuando aparezca solo.
**En el curso:** el paso de cada práctica.

---

*Rogelio Arriaga Gonzalez*
