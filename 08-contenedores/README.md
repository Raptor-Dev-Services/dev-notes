# Docker — Índice

Guía completa de Docker para este proyecto, desde conceptos hasta workflows de producción.

📁 Carpeta: [`docs/docker/`](docker/)

---

## Documentos

| # | Documento | Temas cubiertos |
|---|-----------|-----------------|
| 1 | [Conceptos](docker/01-conceptos.md) | Contenedor vs VM, imagen vs contenedor, capas (layers), volúmenes, redes, `host.docker.internal`, ciclo de vida, registries |
| 2 | [Dockerfile](docker/02-dockerfile.md) | Instrucciones (FROM, RUN, COPY, ENV, ARG, EXPOSE, USER, ENTRYPOINT, CMD), multi-stage builds, `.dockerignore`, ARG vs ENV, optimizaciones de caché, imágenes base .NET |
| 3 | [Docker Compose](docker/03-compose.md) | `image` vs `build`, ports, environment, volumes, `depends_on`, healthcheck, restart, networks, `.env`, override files, profiles, `deploy`, init containers |
| 4 | [Este Proyecto](docker/04-proyecto.md) | Los 4 compose files (`compose-db.yaml`, `compose-dev.yaml`, `compose-staging.yaml`, `compose.yaml`), flujo recomendado dev, variables de entorno, troubleshooting |
| 5 | [Comandos](docker/05-comandos.md) | `docker build`, `docker run`, `docker ps`, `docker logs`, `docker exec`, `docker inspect`, `docker stats`, `docker system prune`, todos los subcomandos de `docker compose` |

---

## Flujo rápido — desarrollo diario

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

## Flujo rápido — probar imagen completa

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
