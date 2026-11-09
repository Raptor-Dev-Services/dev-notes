# 10 - Cloud, Linux y Operación

Documentación sobre infraestructura cloud, administración de Linux y operación de servicios en producción.

## Archivos

| Archivo | Contenido |
|---------|-----------|
| [01-aws-basics.md](01-aws-basics.md) | EC2, S3, RDS, IAM, grupos de seguridad, shared responsibility model |
| [02-azure-basics.md](02-azure-basics.md) | App Service, ACR, PostgreSQL Flexible Server, Blob Storage, Managed Identity |
| [03-linux-nginx.md](03-linux-nginx.md) | Comandos esenciales, systemd, Nginx reverse proxy, SSL con Certbot, UFW |
| [04-observabilidad.md](04-observabilidad.md) | Logs estructurados, Seq, CloudWatch, CloudTrail, Grafana, health checks |
| [05-secretos-produccion.md](05-secretos-produccion.md) | AWS Secrets Manager, Parameter Store, Azure Key Vault, user-secrets local |
| [06-costos.md](06-costos.md) | Reserved Instances, Savings Plans, rightsizing, tagging, Budgets, Organizations |
| [07-seguridad-web.md](07-seguridad-web.md) | OWASP Top 10, WAF, headers de seguridad |
| [08-zero-trust.md](08-zero-trust.md) | Modelo Zero Trust: nunca confiar, siempre verificar |
| [09-file-storage-tenant.md](09-file-storage-tenant.md) | S3 por tenant, presigned URLs, lifecycle |
| [10-cloud-native.md](10-cloud-native.md) | 12-Factor App, contenedores, escalado horizontal |
| [11-aws-vpc-networking.md](11-aws-vpc-networking.md) | VPC, subnets, IGW, NAT Gateway, Security Groups |
| [12-aws-compute-ec2.md](12-aws-compute-ec2.md) | Instancias, AMIs, ELB, Auto Scaling, User Data |
| [13-aws-storage-ebs-s3.md](13-aws-storage-ebs-s3.md) | Almacenamiento de bloque y de objetos, lifecycle |
| [14-aws-rds-postgresql.md](14-aws-rds-postgresql.md) | Multi-AZ, réplicas de lectura, backups, Parameter Groups |
| [15-aws-iam-seguridad.md](15-aws-iam-seguridad.md) | Users, roles, policies, OIDC, mínimo privilegio |
| [16-aws-cloudwatch.md](16-aws-cloudwatch.md) | Métricas, logs, dashboards, alarmas, X-Ray |
| [17-tracing-distribuido.md](17-tracing-distribuido.md) | Jaeger, Zipkin, W3C TraceContext |
| [18-terminal-eficiente.md](18-terminal-eficiente.md) | Expansión del shell, redirecciones, tuberías, preguntas a un log de Nginx en una línea, historial |
| [19-bash-scripting.md](19-bash-scripting.md) | Códigos de salida, set -Eeuo pipefail y sus huecos, comillas, trap, verificar el resultado, ShellCheck |
| [20-systemd.md](20-systemd.md) | Arranque, units y drop-ins, unit endurecida para una API, cgroups, timers en lugar de cron, journal |
| [21-diagnostico-linux.md](21-diagnostico-linux.md) | Método USE, lista de cinco minutos, disco lleno, fstab y nofail, puerto ocupado, servicio que no arranca, runbook |


---

*Rogelio Arriaga Gonzalez*
