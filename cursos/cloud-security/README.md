# Seguridad en la nube y Zero Trust

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/cloud-security/](https://academy.raptorcloud.dev/cloud-security/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** la unidad de nube (AWS y Azure) de Dev Notes, Linux básico y, de preferencia, Infraestructura como código.
**Objetivo:** proteger una carga de trabajo en la nube: responsabilidad compartida, identidad y mínimo privilegio, red, Zero Trust, detección y respuesta, y cumplimiento como código.
**Formato:** 6 módulos de 2 horas (12 h). Cada módulo trae clase, quiz, práctica, tarea y glosario.
**Para quién:** quien ya despliega en AWS o Azure y responde por lo que pasa ahí.
**Alcance:** metodología y defensa. Cada mala configuración se explica (qué es y por qué ocurre), se detecta, se corrige y se verifica. No hay técnicas de ataque paso a paso, ni nada contra cuentas de nube reales.

---

## Cómo se practica sin cuenta de nube

Todas las prácticas corren **en tu máquina, sin costo y sin tocar una cuenta real**:

- **moto** (un servidor que imita la API de AWS) para ver una política negar una acción, un inventario o una bitácora. Es un simulador: implementa un subconjunto de AWS y cada práctica dice dónde difiere.
- **checkov** y **cfn-lint** para revisar plantillas de infraestructura antes de desplegarlas.
- Contenedores desechables sin salida a red, para comprobar «desde fuera no responde».
- Azure se trabaja con **fragmentos** de Bicep o de la CLI, marcados como no ejecutados y con su fuente en la documentación de Microsoft.

Versiones con las que se escribió (a noviembre de 2026): Python 3.14, moto 5.2.5, boto3 1.44.0, checkov 3.2.459, cfn-lint 1.60.0.

---

## Módulo 1. Responsabilidad compartida y activos (2 h)

> El proveedor protege la nube; lo que pongan dentro es de ustedes.

**Material:** [clase](m01-shared-responsibility/slides.md), [práctica](m01-shared-responsibility/practica.md), [tarea](m01-shared-responsibility/tarea.md), [glosario](m01-shared-responsibility/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** el modelo de responsabilidad compartida en AWS y Azure, inventario de activos con dueño y clasificación, etiquetas, acceso público y cifrado básico.
- [ ] **Lectura:** [`10-cloud/01-aws-basics.md`](../../10-cloud/01-aws-basics.md), [`10-cloud/06-costos.md`](../../10-cloud/06-costos.md).
- [ ] **Práctica:** inventario de una cuenta de laboratorio simulada, con dueño y clasificación de cada recurso; corregir lo que no los tiene y volver a medir.
- [ ] **Tarea:** tabla de responsabilidades de la carga de ejemplo, con fuente oficial por fila.
- **Libros:** *Practical Cloud Security* (Dotson, 2023), caps. 1 a 3. *AWS Certified Security - Specialty (SCS-C02) Exam Guide* (Book y Scott, 2024), cap. 1. *Security Architecture for Hybrid Cloud* (Buckwell et al., 2024), cap. 7. *Attack Surface Management* (Eddings y Kaufmann, 2025), caps. 2 a 4.

**Al terminar puedes:** decir qué le toca proteger a tu equipo y qué al proveedor.

---

## Módulo 2. Identidad y mínimo privilegio (2 h)

> Una identidad con demasiado permiso convierte cualquier fallo en un incidente grande.

**Material:** [clase](m02-identity-least-privilege/slides.md), [práctica](m02-identity-least-privilege/practica.md), [tarea](m02-identity-least-privilege/tarea.md), [glosario](m02-identity-least-privilege/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** identidades humanas y de carga, roles en lugar de llaves, credenciales de corta vida, evaluación de políticas, mínimo privilegio, y el equivalente en Azure.
- [ ] **Lectura:** [`10-cloud/15-aws-iam-seguridad.md`](../../10-cloud/15-aws-iam-seguridad.md).
- [ ] **Práctica:** tres identidades sobre un bucket; ver negar lo que no debe permitirse; detectar una política amplia antes de desplegarla.
- [ ] **Tarea:** auditar una identidad: política amplia y mínima, con la prueba que las distingue.
- **Libros:** *Practical Cloud Security*, cap. 4. *Identity-Native Infrastructure Access Management* (Kontsevoy et al., 2023), caps. 2 a 6. *AWS Certified Security - Specialty (SCS-C02) Exam Guide*, caps. 13 a 16. *Cloud Native Security Cookbook* (Armitage, 2022), cap. 2. *Keycloak - Identity and Access Management for Modern Applications* (Thorgersen y Silva, 2023), cap. 3.

**Al terminar puedes:** demostrar que cada identidad puede solo lo que necesita.

---

## Módulo 3. Red y segmentación (2 h)

> Una base de datos con IP pública es un incidente que todavía no pasa.

**Material:** [clase](m03-network-segmentation/slides.md), [práctica](m03-network-segmentation/practica.md), [tarea](m03-network-segmentation/tarea.md), [glosario](m03-network-segmentation/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** VPC, subredes, grupos de seguridad, tráfico de salida y segmentación.
- [ ] **Lectura:** [`10-cloud/11-aws-vpc-networking.md`](../../10-cloud/11-aws-vpc-networking.md).
- [ ] **Práctica:** dejar la base en una subred privada y comprobar desde fuera que no responde.
- [ ] **Tarea:** diagrama y reglas de la red de la carga de ejemplo, con su verificación.
- **Libros:** *Practical Cloud Security*, cap. 6. *Zero Trust Networks* (Rais et al., 2024), cap. 8. *Cloud Native Security Cookbook*, cap. 5. *AWS Certified Advanced Networking - Specialty Exam Guide* (Sluga, 2019), caps. 2 y 3. *Defensive Security Handbook* (Berlin et al., 2024), caps. 16 y 17.

**Al terminar puedes:** diseñar una red donde solo lo necesario es alcanzable.

---

## Módulo 4. Zero Trust (2 h)

> Estar dentro de la red no es una credencial.

**Material:** [clase](m04-zero-trust/slides.md), [práctica](m04-zero-trust/practica.md), [tarea](m04-zero-trust/tarea.md), [glosario](m04-zero-trust/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** confiar en la identidad, el dispositivo y la aplicación, no en la red; decisiones de autorización con contexto; el modelo de referencia del NIST.
- [ ] **Lectura:** [`10-cloud/08-zero-trust.md`](../../10-cloud/08-zero-trust.md).
- [ ] **Práctica:** cambiar un acceso por VPN a un acceso por identidad y registrar quién entra.
- [ ] **Tarea:** plan de adopción de Zero Trust para un acceso concreto.
- **Libros:** *Zero Trust Networks* (Rais et al., 2024), caps. 1 a 7 y 9. *Identity-Native Infrastructure Access Management*, caps. 3 a 6. *Practical Cloud Security*, cap. 1.

**Al terminar puedes:** explicar y aplicar Zero Trust sin que se quede en el eslogan.

---

## Módulo 5. Detección y respuesta en la nube (2 h)

> En la nube, la evidencia está en las bitácoras del proveedor, si se encendieron a tiempo.

**Material:** [clase](m05-detection-response/slides.md), [práctica](m05-detection-response/practica.md), [tarea](m05-detection-response/tarea.md), [glosario](m05-detection-response/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** bitácoras del proveedor, métricas y alarmas, eventos de seguridad y respuesta a incidentes en la nube.
- [ ] **Lectura:** [`10-cloud/16-aws-cloudwatch.md`](../../10-cloud/16-aws-cloudwatch.md).
- [ ] **Práctica:** encender las bitácoras, provocar una acción sospechosa propia y encontrarla en el registro.
- [ ] **Tarea:** guion de respuesta a un incidente de identidad en la nube.
- **Libros:** *Practical Cloud Security*, cap. 7. *AWS Certified Security - Specialty (SCS-C02) Exam Guide*, caps. 4 a 9. *Incident Response in the Age of Cloud* (Ozkaya, 2021), cap. 11. *Cloud Native Security Cookbook*, cap. 3.

**Al terminar puedes:** saber qué pasó en tu cuenta y cuándo, a partir de las bitácoras.

---

## Módulo 6. Cumplimiento como código y arquitectura (2 h)

> Una revisión manual cada trimestre deja tres meses de deriva sin ver.

**Material:** [clase](m06-compliance-as-code/slides.md), [práctica](m06-compliance-as-code/practica.md), [tarea](m06-compliance-as-code/tarea.md), [glosario](m06-compliance-as-code/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** cumplimiento como código, patrones de arquitectura segura y decisiones con su registro.
- [ ] **Práctica:** una regla de cumplimiento que marca un recurso mal configurado en cuanto aparece.
- [ ] **Tarea:** convertir una política escrita en un control automático, con su prueba.
- **Libros:** *Cloud Native Security Cookbook* (Armitage, 2022), caps. 6 y 7. *Security Architecture for Hybrid Cloud* (Buckwell et al., 2024), caps. 7 y 9. *Policy as Code* (Ray, 2024), caps. 1 y 11 a 13. *Security as Code* (Das y Chu, 2023), caps. 3 a 5.

**Al terminar puedes:** convertir una política de seguridad en un control automático.

---

## Proyecto final

Revisar la seguridad de una cuenta de laboratorio (simulada) con una carga de trabajo: identidades, red, bitácoras y cumplimiento, con cada hallazgo corregido y verificado, y un registro de las decisiones tomadas. Para cada hallazgo se entrega: qué es, por qué ocurre, cómo se detectó, cómo se corrigió y cómo se verificó la corrección.

Otros cursos de la plataforma que se combinan con este: Git, Docker, CI/CD y nube (`/devops-essentials/`), Infraestructura como código (`/infrastructure-as-code/`), Kubernetes (`/kubernetes/`) y Defensa y respuesta a incidentes (`/blue-team/`).

---

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| Practical Cloud Security | Chris Dotson | 2023 | Columna del curso |
| Zero Trust Networks | Razi Rais y otros | 2024 | Zero Trust |
| Identity-Native Infrastructure Access Management | Ev Kontsevoy y otros | 2023 | Acceso por identidad |
| AWS Certified Security - Specialty (SCS-C02) Exam Guide | Adam Book y Stuart Scott | 2024 | Identidad, detección y respuesta en AWS |
| Cloud Native Security Cookbook | Josh Armitage | 2022 | Recetas en AWS, Azure y GCP; cumplimiento como código |
| Security Architecture for Hybrid Cloud | Mark Buckwell y otros | 2024 | Arquitectura |

## Lecturas de profundización

| Libro | Autor | Año | Módulos |
|---|---|---|---|
| Incident Response in the Age of Cloud | Erdal Ozkaya | 2021 | 5 |
| Security and Microservice Architecture on AWS | Gaurav Raje | 2021 | 2, 3 |
| AWS Certified Advanced Networking - Specialty Exam Guide | Marko Sluga | 2019 | 3 |
| Defensive Security Handbook | Amanda Berlin y otros | 2024 | 3, 5, 6 |
| Building Secure and Reliable Systems | Heather Adkins y otros | 2020 | 2, 5 |
| Practical Cybersecurity Architecture | Ed Moyle y Diana Kelley | 2020 | 6 |
| Attack Surface Management | Ron Eddings y MJ Kaufmann | 2025 | 1, 3 |
| Threat Modeling | Izar Tarandach y Matthew J. Coles | 2020 | 6 |
| Policy as Code | Jimmy Ray | 2024 | 6 |
| Security as Code | BK Sarthas Das y Virginia Chu | 2023 | 6 |
| Keycloak - Identity and Access Management for Modern Applications | Stian Thorgersen y Pedro Igor Silva | 2023 | 2 |
| Cybersecurity Tabletop Exercises | Robert Lelewski y John Hollenberger | s/f | 5 |
| Serious Cryptography | Jean-Philippe Aumasson | s/f | 1 (cifrado) |
| CIS Kubernetes Benchmark v1.11.0 | Center for Internet Security | 2025 | 3 |

---

*Rogelio Arriaga Gonzalez*
