# 06 - Kubernetes: conceptos

> Fuente: *Kubernetes: Up and Running* (Burns, Beda, Hightower y Evenson, 2022): caps. 1 Introduction, 3 Deploying a Kubernetes Cluster, 4 Common kubectl Commands, 5 Pods y 6 Labels and Annotations. *Certified Kubernetes Administrator Study Companion* (Sachdeva, 2025): caps. 1 Introduction to Kubernetes y 2 Kubernetes Architecture. Documentación oficial citada en Referencias.

## Problema que resuelve

[03-compose.md](03-compose.md) levanta una aplicación completa en **una** máquina. Cuando el servidor se apaga, todo se cae; cuando la aplicación necesita varias copias, las copias viven en el mismo servidor; cuando publicas una versión nueva, tú decides cómo evitar el corte. Un **orquestador** administra contenedores sobre un grupo de máquinas a partir de un estado declarado: dice dónde corre cada uno, lo repone si falla y cambia el número de copias. Kubernetes es el orquestador más difundido.

Esta nota explica las piezas y el vocabulario. [07-kubernetes-manifiestos.md](07-kubernetes-manifiestos.md) escribe los manifiestos para desplegar; [08-kubernetes-seguridad.md](08-kubernetes-seguridad.md) los endurece y [09-kubernetes-diagnostico.md](09-kubernetes-diagnostico.md) enseña a encontrar la causa cuando algo falla.

---

## Qué aporta (y qué no)

Burns y colaboradores presentan las razones de su existencia en el cap. 1: velocidad para publicar, inmutabilidad (se reemplaza el contenedor, no se parcha), configuración declarativa, sistemas que se reparan solos, escalado y desacoplamiento. Lo que **no** aporta: una aplicación bien escrita, copias de seguridad de tus datos ni seguridad por omisión.

| Problema | Compose en un servidor | Kubernetes |
|----------|------------------------|------------|
| Se cae el servidor | Todo se cae | Los pods se reprograman en otro nodo, si hay otro nodo |
| Un contenedor deja de responder pero no termina | `restart:` no lo ve | Las probes lo detectan y el controlador lo reemplaza |
| Más copias | `--scale` en la misma máquina | Réplicas repartidas entre nodos |
| Versión nueva sin corte | La orquestas tú | Actualización gradual y regreso |
| Muchos equipos | Un archivo, un servidor | Namespaces, RBAC y políticas |

---

## Declarativo y controladores

Un manifiesto describe el **estado deseado**. Un **controlador** compara ese estado con el real, una y otra vez, y actúa sobre la diferencia. Por eso borrar un pod gestionado por un Deployment lo hace reaparecer: el estado deseado sigue diciendo "tres".

Cada objeto de la API tiene dos secciones:

- `spec`: lo que tú quieres. Lo escribes tú.
- `status`: lo que hay. Lo escribe el sistema.

---

## Piezas de un clúster

| Componente | Dónde | Qué hace |
|------------|-------|----------|
| `kube-apiserver` | Control plane | Expone la API HTTP; todo pasa por él |
| `etcd` | Control plane | Almacén clave-valor consistente con el estado del clúster |
| `kube-scheduler` | Control plane | Asigna un nodo a cada pod que no lo tiene |
| `kube-controller-manager` | Control plane | Corre los controladores |
| `cloud-controller-manager` | Control plane (opcional) | Integra con el proveedor de nube |
| `kubelet` | Cada nodo | Asegura que los pods asignados corran |
| `kube-proxy` | Cada nodo (opcional) | Mantiene las reglas de red que implementan los Services |
| Runtime de contenedores | Cada nodo | Ejecuta los contenedores |

`kubectl` habla con el `kube-apiserver`, no con los nodos.

---

## Un clúster local

| | kind | K3s |
|---|------|-----|
| Qué es | Clústeres locales con "nodos" que son contenedores de Docker | Distribución ligera de Kubernetes en un solo binario |
| Para qué | Probar Kubernetes, desarrollo local y CI | Borde, IoT, CI y laboratorios |
| Incluye | Lo mínimo | Traefik, CoreDNS, ServiceLB, kube-router y local-path-provisioner |

Instalación de K3s en una VM (lee el script antes de correrlo):

```bash
curl -sfL https://get.k3s.io | sudo sh -
sudo k3s kubectl get nodes
```

Los ejemplos del curso se probaron con K3s `v1.34.1+k3s1` el 11 de noviembre de 2026. Los comandos de `kubectl` son los mismos en cualquier distribución conforme; lo que cambia es lo que el clúster ya trae instalado.

---

## kubectl

| Comando | Para qué |
|---------|----------|
| `kubectl get <tipo>` | Listar objetos; `-o wide` y `-o yaml` dan más detalle |
| `kubectl describe <tipo> <nombre>` | Detalle y **eventos** |
| `kubectl apply -f <archivo>` | Llevar el clúster a lo que dice el archivo |
| `kubectl delete <tipo> <nombre>` | Borrar |
| `kubectl logs <pod>` | Lo que imprimió el contenedor |
| `kubectl exec <pod> -- <comando>` | Correr un comando dentro del contenedor |
| `kubectl explain <campo>` | Documentación del campo, de tu versión |
| `kubectl port-forward pod/<nombre> 8080:80` | Túnel local para probar |
| `kubectl config current-context` | A qué clúster hablas ahora |

Un **contexto** une clúster, usuario y namespace. Antes de cambiar algo, confirma el contexto.

---

## Pods

El pod es la unidad mínima que Kubernetes programa: uno o más contenedores que comparten red y almacenamiento. Cada pod tiene su propia IP dentro del clúster.

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: web
  labels:
    app: web
spec:
  containers:
    - name: web
      image: traefik/whoami:v1.10.3
      ports:
        - containerPort: 80
```

Dos hechos que sorprenden a quien viene de Compose:

1. **Un pod suelto no se repone.** Si lo borras, no vuelve. Para eso existen los controladores (ReplicaSet, Deployment).
2. **El nombre de un pod no es un nombre de DNS.** Un pod `db` no resuelve como `db`; lo que da un nombre estable es un Service.

---

## Etiquetas, selectores y anotaciones

- **Etiqueta:** par clave-valor en `metadata.labels`.
- **Selector:** elige objetos por etiqueta: `app=web` o `app in (api,db)`.
- **Anotación:** metadatos para personas y herramientas; no sirve para seleccionar.

Un Service selecciona sus pods por etiqueta, y un Deployment también. Una etiqueta mal escrita no produce error: el selector simplemente no encuentra nada.

---

## De Compose a pods

| Compose | Kubernetes | Nota |
|---------|------------|------|
| `services.<nombre>` | Pod, o un Deployment que los crea | El nombre deja de ser nombre de host |
| `image` | `containers[].image` | Evita `latest` |
| `ports` | `containerPort` + Service o Ingress | `containerPort` solo informa |
| `environment` | `env` | Lo sensible va a Secret |
| `volumes` | `volumes` + `volumeMounts` + PVC | |
| `depends_on` | No existe | Probes y reintentos |
| `restart` | `restartPolicy` + controlador | |
| `healthcheck` | `livenessProbe` / `readinessProbe` | |

---

## Cuándo no hace falta

Kubernetes cuesta: más piezas, más superficie de ataque, más operación. Sachdeva dedica una sección de su cap. 1 a "When Not to Use Kubernetes". Preguntas para decidir:

- ¿Necesito más de un servidor para que el servicio no se caiga?
- ¿Publico varias veces al día sin cortes?
- ¿La carga varía y debe escalar sola?
- ¿Varios equipos despliegan en la misma infraestructura?
- ¿Hay quien sepa operar un clúster?

Si casi todo es no, Compose y un servidor bien administrado cubren la necesidad. Un servicio administrado de tu proveedor de nube traslada la operación del control plane, no la de tus aplicaciones.

### ADR: dejar la decisión escrita

```markdown
# ADR-001: Cómo desplegamos la API

- Estado: aceptada (AAAA-MM-DD)
- Contexto: una API, una base, un equipo de tres, un servidor.
- Opciones: (A) Compose en un servidor; (B) Kubernetes administrado.
- Decisión: A.
- Consecuencias: un solo anfitrión es un punto único de falla.
- Se revisa: cuando haya más de un servidor.
```

---

## Errores comunes

| Error | Qué pasa | Qué hacer |
|-------|----------|-----------|
| Esperar que un pod suelto se reponga | Desaparece al borrarlo | Un Deployment |
| Imagen con etiqueta inexistente | `ImagePullBackOff` | `kubectl describe`: el evento dice la causa |
| Usar `latest` | Hoy una imagen, mañana otra | Etiqueta con versión |
| Llamar a un pod por su nombre | `NXDOMAIN` | Un Service |
| Contexto equivocado | Cambios en otro clúster | `kubectl config current-context` |
| `ports` pensando que publica | Sin acceso desde fuera | Service, Ingress o `port-forward` |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Orquestador | Sistema que decide dónde corre cada contenedor y lo repone o escala según un estado declarado |
| Clúster | Conjunto de nodos administrados como una plataforma |
| Nodo | Máquina que corre pods |
| Control plane | Componentes que deciden y guardan el estado del clúster |
| Pod | Unidad mínima desplegable: contenedores que comparten red y almacenamiento |
| Controlador | Bucle que acerca el estado real al deseado |
| `spec` / `status` | Estado deseado y estado observado de un objeto |
| Etiqueta / selector | Par clave-valor y la expresión que elige por ellos |
| Namespace | Agrupación lógica de objetos en un clúster |
| Contexto | Clúster, usuario y namespace que usa `kubectl` |
| ADR | Registro corto de una decisión de arquitectura |

---

## Referencias

- Kubernetes, "Kubernetes Components": https://kubernetes.io/docs/concepts/overview/components/ (consultada el 11 de noviembre de 2026).
- Kubernetes, "Labels and Selectors": https://kubernetes.io/docs/concepts/overview/working-with-objects/labels/
- Kubernetes, "Pods": https://kubernetes.io/docs/concepts/workloads/pods/
- K3s, documentación: https://docs.k3s.io/ (componentes incluidos, consultada el 11 de noviembre de 2026).
- kind: https://kind.sigs.k8s.io/
- Burns, B., Beda, J., Hightower, K. y Evenson, L. (2022). *Kubernetes: Up and Running* (3.ª ed.). O'Reilly.
- Sachdeva, P. (2025). *Certified Kubernetes Administrator Study Companion*. Apress.

---

*Rogelio Arriaga Gonzalez*
