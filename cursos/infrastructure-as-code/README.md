# Infraestructura como código

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev](https://academy.raptorcloud.dev/infrastructure-as-code/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** [Linux para desarrolladores](../linux-for-developers/README.md), Git, y nociones de nube (Dev Notes, [`10-cloud/01-aws-basics.md`](../../10-cloud/01-aws-basics.md)).
**Objetivo:** crear y configurar servidores desde código, con estado remoto, módulos, pruebas, CI y políticas que rechazan lo inseguro antes de que exista.
**Formato:** 6 sesiones de 5 horas (30 h). Cada sesión trae clase, quiz, práctica, tarea y glosario, como Titulación 2026.
**Para quién:** quien ya endureció una VM a mano (sesión 2 de Titulación, sesión 5 de Linux para desarrolladores) y no quiere volver a hacerlo.
**Herramientas:** OpenTofu en lugar de Terraform (licencia abierta; el HCL y los conceptos del libro de Terraform sirven tal cual), Ansible, Molecule, Open Policy Agent y Conftest. Las versiones exactas con las que se probó el material están en las referencias de cada sesión, a noviembre de 2026.
**Por qué este curso:** Dev Notes tiene una sola nota de Terraform ([`09-cicd/04-terraform.md`](../../09-cicd/04-terraform.md)) y ninguna de Ansible ni de políticas como código; las tres notas nuevas de esta ruta cierran ese hueco.

---

## Sesión 1. Por qué IaC, y el estado (5 h)

> El estado de Terraform es la memoria del sistema: si dos lo escriben a la vez, se corrompe.

**Material:** [clase](s01-why-iac-state/slides.md), [práctica](s01-why-iac-state/practica.md), [tarea](s01-why-iac-state/tarea.md), [glosario](s01-why-iac-state/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** qué resuelve la infraestructura como código, el primer recurso y el estado remoto con bloqueo.
- [ ] **Lectura:** [`09-cicd/04-terraform.md`](../../09-cicd/04-terraform.md).
- [ ] **Práctica:** el primer recurso con estado remoto. Verlo fallar: dos `apply` a la vez y el bloqueo que lo impide.
- [ ] **Tarea:** importar a código un recurso que hoy existe solo a mano.
- **Libros:** *Terraform: Up and Running* (Brikman, 2022), caps. 1 a 3.

**Al terminar puedes:** crear infraestructura desde código sin que dos personas se pisen.

---

## Sesión 2. Módulos y entornos (5 h)

> Copiar y pegar un entorno es la forma más rápida de que dev y producción dejen de parecerse.

**Material:** [clase](s02-modules-environments/slides.md), [práctica](s02-modules-environments/practica.md), [tarea](s02-modules-environments/tarea.md), [glosario](s02-modules-environments/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** módulos, variables, ciclos y condicionales, y separar entornos.
- [ ] **Lectura:** [`09-cicd/05-terraform-modulos-y-estado.md`](../../09-cicd/05-terraform-modulos-y-estado.md).
- [ ] **Práctica:** un módulo reutilizable para dev y staging con diferencias declaradas.
- [ ] **Tarea:** su módulo con versión fijada y un ejemplo de uso.
- **Libros:** *Terraform: Up and Running*, caps. 4, 5 y 8.

**Al terminar puedes:** mantener varios entornos iguales salvo por lo que se decide que cambie.

---

## Sesión 3. Secretos, pruebas y trabajo en equipo (5 h)

> El archivo de estado puede contener secretos en texto plano.

**Material:** [clase](s03-secrets-testing-teamwork/slides.md), [práctica](s03-secrets-testing-teamwork/practica.md), [tarea](s03-secrets-testing-teamwork/tarea.md), [glosario](s03-secrets-testing-teamwork/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** secretos con Terraform, pruebas de infraestructura y el plan revisado en un pull request.
- [ ] **Lectura:** [`10-cloud/05-secretos-produccion.md`](../../10-cloud/05-secretos-produccion.md).
- [ ] **Práctica:** el `plan` en cada pull request por CI. Verlo fallar: un secreto que aparece en el estado, y cómo evitarlo.
- [ ] **Tarea:** la regla de revisión de cambios de infraestructura de su equipo.
- **Libros:** *Terraform: Up and Running*, caps. 6, 9 y 10.

**Al terminar puedes:** revisar un cambio de infraestructura como se revisa código.

---

## Sesión 4. Ansible: configurar lo que se creó (5 h)

> Un playbook que cambia algo en la segunda corrida no es idempotente.

**Material:** [clase](s04-ansible-configure/slides.md), [práctica](s04-ansible-configure/practica.md), [tarea](s04-ansible-configure/tarea.md), [glosario](s04-ansible-configure/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** inventario, playbooks, variables, hechos y roles.
- [ ] **Lectura:** [`09-cicd/06-ansible.md`](../../09-cicd/06-ansible.md). [`10-cloud/03-linux-nginx.md`](../../10-cloud/03-linux-nginx.md).
- [ ] **Práctica:** un rol que endurece la VM con los controles de la sesión 2 de Titulación: SSH sin contraseña, firewall y fail2ban. Verlo fallar: la segunda corrida debe reportar cero cambios.
- [ ] **Tarea:** el checklist de la VM de Titulación, completo en Ansible.
- **Libros:** *Ansible: Up and Running* (Meijer et al., 2022), caps. 3 a 5 y 9.

**Al terminar puedes:** dejar un servidor endurecido con un comando, las veces que haga falta.

---

## Sesión 5. Calidad y CI para Ansible (5 h)

> Un rol sin prueba se rompe la primera vez que alguien más lo usa.

**Material:** [clase](s05-ansible-quality-ci/slides.md), [práctica](s05-ansible-quality-ci/practica.md), [tarea](s05-ansible-quality-ci/tarea.md), [glosario](s05-ansible-quality-ci/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** depuración de playbooks, pruebas con Molecule, Ansible en CI y buenas prácticas.
- [ ] **Lectura:** [`09-cicd/06-ansible.md`](../../09-cicd/06-ansible.md) (pruebas y buenas prácticas). [`09-cicd/02-github-actions.md`](../../09-cicd/02-github-actions.md).
- [ ] **Práctica:** Molecule prueba el rol de la sesión 4 y el CI lo corre en cada pull request.
- [ ] **Tarea:** un cambio al rol que el CI detenga, y su corrección.
- **Libros:** *Ansible: Up and Running*, caps. 8, 14, 22 y 24.

**Al terminar puedes:** cambiar la configuración de sus servidores con la misma confianza que su código.

---

## Sesión 6. Políticas como código (5 h)

> La política correcta rechaza el bucket público antes de que exista, no después del incidente.

**Material:** [clase](s06-policy-as-code/slides.md), [práctica](s06-policy-as-code/practica.md), [tarea](s06-policy-as-code/tarea.md), [glosario](s06-policy-as-code/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** Open Policy Agent, políticas sobre Terraform, cumplimiento como código y controles de acceso automatizados.
- [ ] **Lectura:** [`09-cicd/07-policy-as-code.md`](../../09-cicd/07-policy-as-code.md). [`09-cicd/01-checklists.md`](../../09-cicd/01-checklists.md).
- [ ] **Práctica:** una política que rechaza un puerto 22 abierto al mundo o un bucket público, vista fallar en el CI.
- [ ] **Tarea:** tres políticas de su organización escritas como código.
- **Libros:** *Policy as Code* (Ray, 2024), caps. 1, 2, 11 y 12. *Security as Code* (Das y Chu, 2023), caps. 3 y 5. *Cloud Native Security Cookbook* (Armitage, 2022), caps. 6 y 7.

**Al terminar puedes:** hacer que el pipeline rechace infraestructura insegura sin que nadie tenga que recordarlo.

---

## Proyecto final

Crear y endurecer un servidor desde cero con un solo comando: OpenTofu crea, Ansible configura, el CI prueba y una política rechaza lo inseguro. Cada pieza, vista fallar.

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| Terraform: Up and Running | Yevgeniy Brikman | 2022 | Columna de Terraform |
| Ansible: Up and Running | Bas Meijer, Lorin Hochstein y René Moser | 2022 | Columna de Ansible |
| Policy as Code | Jimmy Ray | 2024 | Políticas |
| Security as Code | BK Sarthak Das y Virginia Chu | 2023 | DevSecOps aplicado a la infraestructura |
| Cloud Native Security Cookbook | Josh Armitage | 2022 | Cumplimiento como código |
| Python for DevOps | Noah Gift y otros | 2019 | Lectura de apoyo |

---

*Rogelio Arriaga Gonzalez*
