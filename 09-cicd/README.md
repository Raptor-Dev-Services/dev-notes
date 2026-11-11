# 09 - CI/CD y Automatización

Documentación sobre pipelines de integración y entrega continua.

## Archivos

| Archivo | Contenido |
|---------|-----------|
| [01-checklists.md](01-checklists.md) | Checklists de commit, merge, deploy y diagnóstico en producción |
| [02-github-actions.md](02-github-actions.md) | CI en PR + CD a AWS ECR + ECS Fargate, permisos IAM mínimos |
| [03-azure-devops.md](03-azure-devops.md) | Pipelines YAML: CI, CD a ACR + App Service, environments con aprobación |
| [04-terraform.md](04-terraform.md) | HCL, módulos, estado remoto en S3 con bloqueo en DynamoDB, entornos separados |
| [05-terraform-modulos-y-estado.md](05-terraform-modulos-y-estado.md) | Módulos, `for_each` contra `count`, entornos, bloqueo del estado y `moved`, con OpenTofu |
| [06-ansible.md](06-ansible.md) | Inventario, playbooks, idempotencia, handlers y roles |
| [07-policy-as-code.md](07-policy-as-code.md) | Políticas en Rego sobre el plan de OpenTofu, con Conftest y `opa test` |


---

*Rogelio Arriaga Gonzalez*
