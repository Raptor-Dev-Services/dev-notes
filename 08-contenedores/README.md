# Docker - Índice

Guía completa de Docker para este proyecto, desde conceptos hasta workflows de producción.

---

## Documentos

| # | Documento | Temas cubiertos |
|---|-----------|-----------------|
| 1 | [Conceptos](01-conceptos.md) | Contenedor vs VM, imagen vs contenedor, capas (layers), volúmenes, redes, `host.docker.internal`, ciclo de vida, registries |
| 2 | [Dockerfile](02-dockerfile.md) | Instrucciones (FROM, RUN, COPY, ENV, ARG, EXPOSE, USER, ENTRYPOINT, CMD), multi-stage builds, `.dockerignore`, ARG vs ENV, optimizaciones de caché, imágenes base .NET |
| 3 | [Docker Compose](03-compose.md) | `image` vs `build`, ports, environment, volumes, `depends_on`, healthcheck, restart, networks, `.env`, override files, profiles, `deploy`, init containers |
| 4 | [Proyecto Real](04-proyecto-real.md) | Dockerfile multi-stage del back y del front, docker-compose de desarrollo, override, `.env`, compose de producción, health checks, workflow diario, troubleshooting |
| 5 | [Comandos](05-comandos.md) | `docker build`, `docker run`, `docker ps`, `docker logs`, `docker exec`, `docker inspect`, `docker stats`, `docker system prune`, todos los subcomandos de `docker compose` |
| 6 | [Kubernetes: conceptos](06-kubernetes-conceptos.md) | Orquestador, componentes del clúster, K3s y kind, `kubectl`, pods, etiquetas, de Compose a pods, cuándo no hace falta |
| 7 | [Kubernetes: manifiestos](07-kubernetes-manifiestos.md) | Deployment, actualización gradual y regreso, probes, Service, Ingress |
| 8 | [Kubernetes: seguridad](08-kubernetes-seguridad.md) | RBAC, Pod Security, políticas como código, NetworkPolicy |
| 9 | [Kubernetes: diagnóstico](09-kubernetes-diagnostico.md) | Método de diagnóstico, cinco fallas, versiones, Kustomize y Helm, runbook |

---

## Flujo rápido - desarrollo diario

```bash
# 1. Levantar infraestructura (PostgreSQL + Seq + Jaeger)
docker compose -f compose-db.yaml up -d

# 2. Correr la API localmente
dotnet run --project back-template/Host --launch-profile Local

# 3. Al terminar
docker compose -f compose-db.yaml down
```

| URL | Recurso |
|-----|---------|
| `http://localhost:5080/swagger` | Swagger UI |
| `http://localhost:5080/api/health` | Health check |
| `http://localhost:5341` | Seq (logs) |
| `http://localhost:16686` | Jaeger (trazas) |

---

## Flujo rápido - probar imagen completa

```bash
# Construir y levantar todo en Docker
docker compose -f compose-dev.yaml up -d --build

# Ver logs
docker compose -f compose-dev.yaml logs -f api

# Apagar
docker compose -f compose-dev.yaml down
```


---

*Rogelio Arriaga Gonzalez*
