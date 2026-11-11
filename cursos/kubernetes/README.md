# Kubernetes: del Compose al clúster

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/kubernetes/](https://academy.raptorcloud.dev/kubernetes/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** Docker y Compose ([`08-contenedores`](../../08-contenedores/)) y Linux para desarrolladores, o equivalente.
**Objetivo:** desplegar, asegurar, escalar y diagnosticar una aplicación en Kubernetes, y saber cuándo no hace falta.
**Formato:** 6 sesiones de 5 horas (30 h). Cada sesión trae clase, quiz, práctica, tarea y glosario, como Titulación 2026.
**Para quién:** quien ya despliega con Compose y se topa con Kubernetes en ofertas de trabajo o en un cliente.
**Versión de referencia:** las prácticas se probaron con K3s `v1.34.1+k3s1` (11 de noviembre de 2026). Según kubernetes.io/releases (consultado ese día), la rama 1.34 tiene fin de vida el 2026-10-27: sirve para el laboratorio, no se recomienda para producción.

---


## Sesión 1. Del contenedor al pod, y cuándo no (5 h)

> Kubernetes resuelve problemas de escala. Sin esos problemas, solo agrega piezas.

**Material:** [clase](s01-contenedor-al-pod/slides.md), [práctica](s01-contenedor-al-pod/practica.md), [tarea](s01-contenedor-al-pod/tarea.md), [glosario](s01-contenedor-al-pod/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** qué resuelve un orquestador, un clúster local con K3s, `kubectl`, pods y etiquetas. Compose o Kubernetes: la decisión, escrita como ADR.
- [ ] **Lectura:** [`08-contenedores/03-compose.md`](../../08-contenedores/03-compose.md). [`08-contenedores/06-kubernetes-conceptos.md`](../../08-contenedores/06-kubernetes-conceptos.md).
- [ ] **Práctica:** llevar el `compose.yaml` de un proyecto a pods y comparar línea por línea. Verlo fallar: un pod suelto que no se repone, una imagen mal escrita, y una base que no se encuentra por nombre.
- [ ] **Tarea:** el ADR de su proyecto, Compose o Kubernetes, con dos opciones reales.
- **Libros:** *Kubernetes: Up and Running* (Burns et al., 2022), caps. 1 a 6. *Certified Kubernetes Administrator Study Companion* (Sachdeva, 2025), caps. 1 a 4.
- **Caso real:** El instructor despliega sus productos con Compose y todavía no cambia a Kubernetes.

**Al terminar puedes:** levantar un clúster local y decidir con argumentos si su proyecto lo necesita.

---

## Sesión 2. Deployments, Services e Ingress (5 h)

> Un despliegue sin readiness probe manda tráfico a una aplicación que todavía no puede contestar.

**Material:** [clase](s02-deployments-services-ingress/slides.md), [práctica](s02-deployments-services-ingress/practica.md), [tarea](s02-deployments-services-ingress/tarea.md), [glosario](s02-deployments-services-ingress/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** ReplicaSets y Deployments, actualización gradual y reversa, probes, Services, Ingress.
- [ ] **Lectura:** [`08-contenedores/07-kubernetes-manifiestos.md`](../../08-contenedores/07-kubernetes-manifiestos.md).
- [ ] **Práctica:** desplegar API y front, actualizar y regresar. Verlo fallar: una readiness probe mal escrita deja el despliegue detenido.
- [ ] **Tarea:** manifiestos versionados en el repositorio, sin `kubectl` a mano.
- **Libros:** *Kubernetes: Up and Running*, caps. 7 a 10. *Kubernetes Recipes* (Stencel), cap. 2. *CKA Study Companion*, caps. 5 y 6.

**Al terminar puedes:** publicar una versión nueva sin cortar el servicio y regresar si falla.

---

## Sesión 3. Configuración, secretos y datos con estado (5 h)

> Un Secret de Kubernetes está en base64, no cifrado: cualquiera que lo lea lo tiene.

**Material:** [clase](s03-config-secretos-datos/slides.md), [práctica](s03-config-secretos-datos/practica.md), [tarea](s03-config-secretos-datos/tarea.md), [glosario](s03-config-secretos-datos/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** ConfigMaps y Secrets, volúmenes persistentes y StatefulSets.
- [ ] **Lectura:** [`10-cloud/05-secretos-produccion.md`](../../10-cloud/05-secretos-produccion.md).
- [ ] **Práctica:** PostgreSQL con volumen persistente. Verlo fallar: borrar el pod y comprobar que los datos siguen; decodificar un Secret con un solo comando; encontrar el valor en la anotación `last-applied-configuration`.
- [ ] **Tarea:** dónde vivirían sus secretos en producción, y por qué.
- **Libros:** *Kubernetes: Up and Running*, caps. 13 y 16. *Kubernetes Recipes*, caps. 3 y 7. *CKA Study Companion*, cap. 15.

**Al terminar puedes:** correr una base de datos en el clúster sin perder datos ni dejar secretos expuestos.

---

## Sesión 4. Seguridad del clúster (5 h)

> Una política que nunca rechazó nada no se ha probado.

**Material:** [clase](s04-seguridad-del-cluster/slides.md), [práctica](s04-seguridad-del-cluster/practica.md), [tarea](s04-seguridad-del-cluster/tarea.md), [glosario](s04-seguridad-del-cluster/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** RBAC, cuentas de servicio, Pod Security, NetworkPolicies y políticas como código.
- [ ] **Lectura:** [`08-contenedores/08-kubernetes-seguridad.md`](../../08-contenedores/08-kubernetes-seguridad.md).
- [ ] **Práctica:** una política que impide correr como root, vista rechazar un pod, una política propia que prohíbe `latest` y una NetworkPolicy que aísla la base.
- [ ] **Tarea:** la matriz de quién puede hacer qué en su clúster.
- **Libros:** *Kubernetes: Up and Running*, caps. 14, 19 y 20. *Kubernetes Recipes*, cap. 11. *Policy as Code* (Ray, 2024), caps. 4, 7 y 8.

**Al terminar puedes:** demostrar que el clúster rechaza lo que su política prohíbe.

---

## Sesión 5. Escalar y observar (5 h)

> Escalar sin medir es gastar más sin saber si sirvió.

**Material:** [clase](s05-escalar-y-observar/slides.md), [práctica](s05-escalar-y-observar/practica.md), [tarea](s05-escalar-y-observar/tarea.md), [glosario](s05-escalar-y-observar/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** peticiones y límites, autoescalado horizontal, DaemonSets y Jobs, logs y métricas.
- [ ] **Lectura:** [`10-cloud/04-observabilidad.md`](../../10-cloud/04-observabilidad.md).
- [ ] **Práctica:** una prueba de carga que dispara el autoescalado, medida antes y después. Verlo fallar: un contenedor que excede su límite de memoria y termina `OOMKilled`.
- [ ] **Tarea:** la gráfica de su prueba, con su conclusión.
- **Libros:** *Kubernetes Recipes*, caps. 6 y 9. *Kubernetes: Up and Running*, caps. 11 y 12. *CKA Study Companion*, caps. 8 a 10.

**Al terminar puedes:** decir cuántas réplicas necesita su aplicación, con datos.

---

## Sesión 6. Operar y diagnosticar (5 h)

> CrashLoopBackOff es un síntoma. La causa está en los eventos y en los logs.

**Material:** [clase](s06-operar-y-diagnosticar/slides.md), [práctica](s06-operar-y-diagnosticar/practica.md), [tarea](s06-operar-y-diagnosticar/tarea.md), [glosario](s06-operar-y-diagnosticar/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** arquitectura del clúster, actualizaciones, gestión de manifiestos (Kustomize y Helm) y método de diagnóstico.
- [ ] **Lectura:** [`08-contenedores/09-kubernetes-diagnostico.md`](../../08-contenedores/09-kubernetes-diagnostico.md).
- [ ] **Práctica:** cinco fallas sembradas: imagen mal escrita, selector que no empata, volumen pendiente, CrashLoopBackOff y NetworkPolicy que corta.
- [ ] **Tarea:** el runbook de diagnóstico de su aplicación.
- **Libros:** *Certified Kubernetes Administrator Study Companion* (Sachdeva, 2025), partes V y VI (caps. 18 a 23). *Kubernetes Recipes*, caps. 10 y 13.

**Al terminar puedes:** encontrar por qué un pod no arranca sin adivinar.

---

## Proyecto final

La API de Titulación desplegada en el clúster con manifiestos versionados, políticas que se vieron rechazar, autoescalado medido y runbook. Se califica cuando otro equipo le siembra una falla y la encuentran con su runbook.

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| Kubernetes: Up and Running | Brendan Burns, Joe Beda, Kelsey Hightower y Lachlan Evenson | 2022 | Columna del curso |
| Kubernetes Recipes | Grzegorz Stencel | s/f | Recetas por tema |
| Certified Kubernetes Administrator Study Companion | Piyush Sachdeva | 2025 | Operación y certificación |
| Policy as Code | Jimmy Ray | 2024 | Políticas en el clúster |
| Deploy Container Applications Using Kubernetes | Shiva Subramanian | s/f | Lectura alterna |

## Notas nuevas de este curso

| Nota | Sesión |
|---|---|
| [`08-contenedores/06-kubernetes-conceptos.md`](../../08-contenedores/06-kubernetes-conceptos.md) | 1 |
| [`08-contenedores/07-kubernetes-manifiestos.md`](../../08-contenedores/07-kubernetes-manifiestos.md) | 2 |
| [`08-contenedores/08-kubernetes-seguridad.md`](../../08-contenedores/08-kubernetes-seguridad.md) | 4 |
| [`08-contenedores/09-kubernetes-diagnostico.md`](../../08-contenedores/09-kubernetes-diagnostico.md) | 6 |

---

*Rogelio Arriaga Gonzalez*
