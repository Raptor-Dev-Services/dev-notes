# 09 - Kubernetes: diagnóstico

> Fuente: *Certified Kubernetes Administrator Study Companion* (Sachdeva, 2025): partes V y VI, caps. 18 Cluster Maintenance, 19 Kubernetes Version Upgrade Using Kubeadm, 20 Monitoring, Logging, and Alerting, 21 Troubleshooting Application Failure y 22 Troubleshooting Control Plane Failure. *Kubernetes Recipes* (Stencel): caps. 10 Control Plane Administration and Package Management y 13 Best Practices in Kubernetes. Documentación oficial citada en Referencias.

## Problema que resuelve

Un pod en `CrashLoopBackOff` es un **síntoma**. La causa está en los eventos y en los logs. Quien diagnostica adivinando reinicia, borra y espera; quien sigue un método llega a la causa de un error que nunca había visto. Esta nota trae el método, las cinco fallas más frecuentes con la salida real de cada una, y la forma de un runbook.

Las salidas se obtuvieron en un laboratorio propio con K3s `v1.37.2+k3s1` (release estable del 2026-11-03 en github.com/k3s-io/k3s/releases), de la rama 1.37 de Kubernetes, con fin de vida el 2027-10-28 según kubernetes.io/releases (consultado el 11 de noviembre de 2026). Los nombres de pods y las IP cambian en cada clúster.

Las ramas mantenidas de Kubernetes a esa fecha son 1.37, 1.36 y 1.35; esas fechas cambian y se consultan de nuevo antes de decidir.

---

## El método

1. `kubectl get pods -o wide`: ¿qué `STATUS` tiene?
2. `kubectl describe pod <pod>`: lee la sección **Events**.
3. `kubectl logs <pod>` y `kubectl logs <pod> --previous` (el contenedor anterior, tras un reinicio).
4. `kubectl get endpointslices -l kubernetes.io/service-name=<svc>`: ¿el Service tiene destinos?
5. Un pod de pruebas desde el que llamar: `kubectl run cli --image=busybox:1.37 --command -- sleep 3600`.
6. `kubectl get nodes`, `kubectl describe pvc`, `kubectl get networkpolicy`, `kubectl top nodes`.

**El estado es el síntoma; el evento y el log son la causa.** Reiniciar o borrar el pod antes de leer pierde la pista.

---

## Qué dice cada estado

| STATUS | Significa | Dónde buscar la causa |
|--------|-----------|-----------------------|
| `Pending` | Sin nodo asignado | Eventos: `FailedScheduling` |
| `ContainerCreating` (largo) | Bajando imagen o montando volumen | Eventos |
| `ErrImagePull`, `ImagePullBackOff` | No pudo bajar la imagen | Eventos: mensaje del registry |
| `CreateContainerConfigError` | Falta un Secret o ConfigMap | Eventos |
| `CrashLoopBackOff` | Arranca y muere en ciclo | **Logs** y código de salida |
| `Running` con `0/1` | No pasa la readiness | Eventos: `Readiness probe failed` |
| `Running` y `1/1`, nadie responde | Selector, puerto o red | Endpoints y NetworkPolicy |
| `OOMKilled` | Se pasó del límite de memoria | `describe`: código 137 |

---

## Falla 1: imagen mal escrita

```console
$ kubectl set image deploy/api api=nginx:1.27-alpne
$ kubectl get pods -l app=api
api-5b57696d67-26xn4   1/1   Running            0   23s
api-5b57696d67-6jzjh   1/1   Running            0   23s
api-7fdf8754b4-h4rfm   0/1   ImagePullBackOff   0   20s
$ kubectl describe pod api-7fdf8754b4-h4rfm | grep 'Failed to pull'
  Warning  Failed  ...  Failed to pull image "nginx:1.27-alpne": ... not found
```

Causa: una letra de menos. Los pods viejos siguen atendiendo (la actualización gradual protegió el servicio). Arreglo: corregir el manifiesto, o `kubectl rollout undo deploy/api`.

## Falla 2: el selector que no empata

```console
$ kubectl get endpointslices -l kubernetes.io/service-name=api
NAME        ADDRESSTYPE   PORTS     ENDPOINTS   AGE
api-wcvxd   IPv4          <unset>   <unset>     34s
$ kubectl get svc api -o jsonpath='{.spec.selector}'
{"app":"apii"}
$ kubectl get pods --show-labels -l app=api | head -2
api-5b57696d67-26xn4   1/1   Running   0   38s   app=api,pod-template-hash=5b57696d67
```

Los pods están `1/1` y nada responde. La pista es `ENDPOINTS` vacío. Una etiqueta mal escrita no produce error: el selector no encuentra nada.

## Falla 3: el volumen que no se une

```console
$ kubectl get pod usa-pvc
usa-pvc   0/1   Pending   0   8s
$ kubectl describe pvc datos | tail -1
  Warning  ProvisioningFailed  ...  storageclass.storage.k8s.io "rapida" not found
$ kubectl describe pod usa-pvc | tail -1
  Warning  FailedScheduling  ...  0/1 nodes are available: pod has unbound immediate PersistentVolumeClaims.
```

El pod dice que su PVC no está unida; la PVC dice por qué. `storageClassName` de una PVC no se puede cambiar una vez creada: se borra y se recrea.

## Falla 4: CrashLoopBackOff

```console
$ kubectl get pod crash
crash   0/1   CrashLoopBackOff   2 (26s ago)   40s
$ kubectl logs crash
no encuentro la variable DB_HOST
$ kubectl describe pod crash | grep -E 'Exit Code|Restart Count'
    Exit Code:    1
    Restart Count:  2
```

Código de salida 1: error de la aplicación. 137 sería "lo mataron" (señal 9), y con `OOMKilled` en el estado, por memoria. Tras un reinicio, el log anterior se pide con `--previous`.

## Falla 5: la NetworkPolicy que corta

Una política que selecciona los pods de `web` y solo permite entrada desde `app=frontal` por el puerto 8080, cuando nginx escucha en el 80:

```console
$ kubectl exec frontal -- wget -qO- -T 3 http://web/
wget: can't connect to remote host (<IP-del-Service>): Connection refused
$ kubectl get endpointslices
web-ddqtj   IPv4   80   <IP-pod-1>,<IP-pod-2>   43s
$ kubectl describe networkpolicy solo-lo-necesario | sed -n '/Allowing/,/Policy Types/p'
  Allowing ingress traffic:
    To Port: 8080/TCP
    From:
      PodSelector: app=frontal
```

Todo se ve sano (pods `1/1`, endpoints presentes); solo la prueba desde un pod lo revela. Se distingue de la falla 2 porque los endpoints **no** están vacíos. Se corrige el puerto de la política.

## Dos `Pending` con causas distintas

```console
$ kubectl describe pod grande | tail -1
  Warning  FailedScheduling  ...  0/1 nodes are available: 1 Insufficient cpu. ...
$ kubectl describe pod nuevo | tail -1
  Warning  FailedScheduling  ...  0/1 nodes are available: 1 node(s) were unschedulable. ...
```

El primero pidió más CPU de la que tiene el nodo; el segundo se programó con el nodo en `cordon`. Los dos son `Pending`: el evento del scheduler dice cuál.

---

## `kubectl debug`

Cuando la imagen no trae ni `sh`, `kubectl debug` agrega un contenedor efímero al pod en marcha, que comparte su red:

```console
$ kubectl debug -it pod/api-5b57696d67-26xn4 --image=busybox:1.37 --target=api -- sh
/ # wget -qO- http://127.0.0.1/ | head -c 25
<!DOCTYPE html>
<html>
<h
```

No toca la imagen de la aplicación ni su manifiesto.

---

## Mantener el clúster al día

| Componente | Regla de la política de versiones de Kubernetes |
|------------|--------------------------------------------------|
| `kubelet` | No puede ser más nuevo que el API; puede estar hasta tres versiones menores atrás |
| `kubectl` | Dentro de una versión menor del API |
| `kube-apiserver` en alta disponibilidad | Entre el más nuevo y el más viejo, una versión menor |

Con kubeadm: se actualiza primero el control plane y luego los nodos de trabajo; saltarse versiones menores no está soportado; antes de actualizar el kubelet de un nodo en una versión menor, se **drena** (`kubectl drain`). Antes de actualizar, respaldos de los datos de la aplicación. Después, repetir las pruebas de la aplicación y las pruebas de rechazo de seguridad.

`kubectl cordon <nodo>` deja un nodo sin pods nuevos (los que corren siguen); `kubectl uncordon <nodo>` lo revierte. K3s documenta su propio procedimiento de actualización; no es kubeadm.

---

## Organizar manifiestos

**Kustomize** viene en `kubectl`: una base con los manifiestos comunes y *overlays* por entorno.

```yaml
# k/overlays/produccion/kustomization.yaml
resources:
  - ../../base
namePrefix: prod-
replicas:
  - name: web
    count: 3
images:
  - name: nginx
    newTag: 1.27.5-alpine
```

```bash
kubectl kustomize k/overlays/produccion      # solo muestra
kubectl apply -k k/overlays/produccion
```

**Helm** es el gestor de paquetes: un paquete es un *chart* e instalarlo crea un *release*. Sirve sobre todo para software de terceros con muchas piezas. Este curso no lo instala.

---

## El runbook

| Sección | Contenido |
|---------|-----------|
| Síntoma | Lo que se ve |
| Primera lectura | El comando que se corre primero y qué buscar |
| Árbol de decisión | Si dice A, ve a...; si dice B, ve a... |
| Causas conocidas | Las de esta aplicación y su arreglo |
| Cuándo escalar | A quién se avisa y con qué datos |
| Cómo regresar | `rollout undo` y qué se pierde |
| Última revisión | Fecha y quién lo probó |

Un runbook sin probar es una suposición: se prueba sembrándole una falla a otro equipo.

---

## Errores comunes

| Error | Qué pasa | Qué hacer |
|-------|----------|-----------|
| Leer solo `STATUS` | No se llega a la causa | Eventos y logs |
| Reiniciar o borrar antes de leer | Se pierde la pista | Leer primero |
| Culpar al pod `Pending` | La causa es el volumen o el nodo | Leer también lo que pide |
| Confundir selector con red | Dos "Connection refused" iguales | Mirar los endpoints |
| Parchar con `kubectl edit` | El repositorio contradice al clúster | Corregir el archivo |
| Saltarse versiones menores | Combinación no soportada | Una a la vez |
| No probar el runbook | Falla cuando hace falta | Siembra de fallas |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Evento | Registro breve de lo que le pasó a un objeto; sección `Events` de `describe` |
| CrashLoopBackOff | Arranca, muere y espera cada vez más para reintentar |
| `FailedScheduling` | Evento del scheduler: no encontró nodo para el pod |
| `cordon` / `drain` | Marcar un nodo como no programable / sacar sus pods con orden |
| Contenedor efímero | Contenedor de depuración que `kubectl debug` agrega a un pod vivo |
| Kustomize | Combinación de base y *overlays* integrada en `kubectl` |
| Helm | Gestor de paquetes: charts y releases |
| Runbook | Documento que otra persona sigue sin ti cuando algo falla |

---

## Referencias

- Kubernetes, "Version Skew Policy": https://kubernetes.io/releases/version-skew-policy/ (consultada el 11 de noviembre de 2026).
- Kubernetes, "Releases": https://kubernetes.io/releases/ (ramas mantenidas y fin de vida, consultada el 11 de noviembre de 2026).
- Kubernetes, "Upgrading kubeadm clusters": https://kubernetes.io/docs/tasks/administer-cluster/kubeadm/kubeadm-upgrade/
- Kubernetes, "Debug Running Pods": https://kubernetes.io/docs/tasks/debug/debug-application/debug-running-pod/
- Helm, documentación: https://helm.sh/docs/
- Sachdeva, P. (2025). *Certified Kubernetes Administrator Study Companion*. Apress.
- Stencel, G. *Kubernetes Recipes*. Apress.

---

*Rogelio Arriaga Gonzalez*
