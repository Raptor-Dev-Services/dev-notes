# Roadmap — DevOps: Git, Docker, CI/CD, Cloud

**Prerequisito:** saber programar — no se necesita experiencia previa en DevOps.
**Objetivo:** dominar Git, contenedores Docker, pipelines CI/CD y despliegue en cloud.
**Duración estimada:** 4-6 semanas.

---

## Fase 1 — Git profesional (semana 1)

> Control de versiones es la base de todo trabajo en equipo. No hay CI/CD sin Git bien usado.

- [ ] [Project Tooling](../07-git/01-project-tooling.md) — .gitignore, .editorconfig, Husky, Dependabot
- [ ] [Git Config](../07-git/03-git-config.md) — configuración global y por repo, aliases
- [ ] [Commit Conventions](../07-git/06-commit-conventions.md) — Conventional Commits, commitlint, semantic-release
- [ ] [Git Flow](../07-git/04-git-flow.md) — branching strategy, feature/release/hotfix branches
- [ ] [Merge Strategies](../07-git/07-merge-strategies.md) — merge, squash, rebase — cuándo usar cada uno
- [ ] [SemVer](../07-git/02-semver.md) — versionado semántico MAJOR.MINOR.PATCH
- [ ] [Tags](../07-git/05-tags.md) — tags anotados, signed, push a remoto
- [ ] [Rollback](../07-git/08-rollback.md) — revert, reset, reflog, recuperación de commits perdidos
- [ ] [Stash](../07-git/09-stash.md) — guardar trabajo sin commitear, pop, apply, drop

**Al terminar esta fase puedes:** trabajar en equipo con Git de manera profesional — sin perder trabajo ni mezclar código.

---

## Fase 2 — Contenedores Docker (semana 2)

> Docker es el estándar para empaquetar y ejecutar aplicaciones de manera reproducible.

- [ ] [Conceptos](../08-contenedores/01-conceptos.md) — imagen, contenedor, layer, registry, tag
- [ ] [Dockerfile](../08-contenedores/02-dockerfile.md) — multi-stage build, instrucciones, optimización de capas
- [ ] [Compose](../08-contenedores/03-compose.md) — docker-compose.yml, redes, volúmenes, profiles, depends_on
- [ ] [Proyecto Real](../08-contenedores/04-proyecto-real.md) — API + frontend + PostgreSQL + Seq en Docker
- [ ] [Comandos](../08-contenedores/05-comandos.md) — referencia completa de comandos docker y compose

**Al terminar esta fase puedes:** levantar el stack completo del proyecto con `docker compose up -d` y escribir Dockerfiles eficientes.

---

## Fase 3 — CI/CD con GitHub Actions (semana 2-3)

> Automatizar build, tests y deploy — el cambio más importante en productividad del equipo.

- [ ] [Checklists DevSecOps](../09-cicd/01-checklists.md) — shift-left, SAST/DAST, SBOM, GitHub Actions CI gates
- [ ] [GitHub Actions](../09-cicd/02-github-actions.md) — OIDC+AWS+ECS, reusable workflows, matrix builds, secrets
- [ ] [Azure DevOps](../09-cicd/03-azure-devops.md) — pipelines YAML, service connections, environments, approvals

**Al terminar esta fase puedes:** configurar un pipeline completo que build, testea y despliega en AWS con OIDC (sin credenciales hardcoded).

---

## Fase 4 — Cloud: fundamentos AWS (semana 3-4)

> Los servicios mínimos para desplegar un SaaS en producción.

- [ ] [AWS Basics](../10-cloud/01-aws-basics.md) — regiones, AZs, cuentas, IAM, Console vs CLI
- [ ] [VPC / Networking](../10-cloud/11-aws-vpc-networking.md) — VPC, subnets públicas/privadas, IGW, NAT, Security Groups
- [ ] [EC2](../10-cloud/12-aws-compute-ec2.md) — instancias, AMIs, ELB, Auto Scaling, tipos
- [ ] [EBS + S3](../10-cloud/13-aws-storage-ebs-s3.md) — almacenamiento de bloque y objetos, lifecycle, costos
- [ ] [RDS / PostgreSQL](../10-cloud/14-aws-rds-postgresql.md) — Multi-AZ, réplicas, backups, Parameter Groups
- [ ] [IAM](../10-cloud/15-aws-iam-seguridad.md) — users, roles, policies, OIDC IRSA, least privilege
- [ ] [CloudWatch](../10-cloud/16-aws-cloudwatch.md) — métricas, logs, dashboards, alarmas

**Al terminar esta fase puedes:** desplegar una API con RDS PostgreSQL en AWS con VPC correctamente configurada.

---

## Fase 5 — Infraestructura como código con Terraform (semana 4)

> Definir toda la infraestructura en archivos HCL versionados en Git.

- [ ] [Terraform](../09-cicd/04-terraform.md) — HCL, módulos, estado S3+DynamoDB, remote_state, partial config

**Al terminar esta fase puedes:** crear y destruir infraestructura AWS completa con `terraform apply`, con estado compartido en S3.

---

## Fase 6 — Operaciones y observabilidad (semana 5)

> Ver qué pasa dentro del sistema en producción.

- [ ] [Observabilidad](../10-cloud/04-observabilidad.md) — OpenTelemetry, Loki, Prometheus, Grafana, Tempo
- [ ] [Tracing Distribuido](../10-cloud/09-tracing-distribuido.md) — Jaeger, W3C TraceContext, correlación de requests
- [ ] [Linux + Nginx](../10-cloud/03-linux-nginx.md) — servidor web, reverse proxy, SSL/TLS con Let's Encrypt
- [ ] [Secretos Producción](../10-cloud/05-secretos-produccion.md) — AWS Secrets Manager, Parameter Store, rotación automática

**Al terminar esta fase puedes:** diagnosticar problemas en producción con trazas, métricas y logs correlacionados.

---

## Fase 7 — Seguridad (semana 5-6)

- [ ] [Seguridad Web](../10-cloud/07-seguridad-web.md) — OWASP Top 10, WAF, headers de seguridad, CSP
- [ ] [Zero Trust](../10-cloud/08-zero-trust.md) — nunca confiar, siempre verificar — arquitectura de seguridad moderna
- [ ] [Costos](../10-cloud/06-costos.md) — optimización de costos AWS/Azure, Reserved Instances, Spot

---

## Qué puedes construir al terminar

Un pipeline completo:

```
git push → GitHub Actions
    ↓ build + lint + tests (Testcontainers)
    ↓ SAST (CodeQL) + SCA (Dependabot)
    ↓ docker build + push a ECR
    ↓ terraform plan (review)
    ↓ terraform apply (prod)
    ↓ ECS rolling deploy
    ↓ CloudWatch alarms + Grafana dashboards
```

Con infraestructura definida como código, secretos en Secrets Manager, y observabilidad completa.

---

## Azure como alternativa

Si el stack es Azure en lugar de AWS:

- [ ] [Azure Basics](../10-cloud/02-azure-basics.md) — Resource Groups, ARM, servicios equivalentes
- [ ] [Azure DevOps](../09-cicd/03-azure-devops.md) — pipelines, service connections, approvals

---

*Duración total estimada: 4-6 semanas — Nivel objetivo: DevOps engineer junior/mid*
