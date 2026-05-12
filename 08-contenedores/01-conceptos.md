# 01 — Conceptos de Docker

---

## ¿Qué es Docker?

Docker es una plataforma que empaqueta aplicaciones y todas sus dependencias en unidades aisladas llamadas **contenedores**. Un contenedor incluye todo lo necesario para correr: código, runtime (.NET), librerías del sistema, variables de entorno y configuración.

```
Tu máquina Windows/Mac/Linux
├── Docker Engine (el daemon)
│   ├── Contenedor: api (back-template .NET 10)
│   ├── Contenedor: postgres (PostgreSQL 17)
│   └── Contenedor: seq (Seq log server)
└── (los contenedores comparten el kernel del host pero están aislados)
```

---

## Contenedor vs Máquina Virtual

| | VM | Contenedor |
|--|-----|------------|
| Aislamiento | SO completo propio | Proceso aislado que comparte el kernel del host |
| Tamaño | GBs | MBs |
| Arranque | Minutos | Segundos / milisegundos |
| Overhead | Alto | Muy bajo |
| Portabilidad | Imagen pesada | Imagen ligera, portable |
| Uso típico | Simular OS distinto | Aislar aplicaciones y dependencias |

---

## Imagen vs Contenedor

| Concepto | Analogía | Descripción |
|---------|---------|------------|
| **Imagen** | Clase | Plantilla inmutable — define qué hay en el contenedor |
| **Contenedor** | Instancia de la clase | Imagen en ejecución — tiene estado, puede modificarse |
| **Registry** | npm / NuGet | Repositorio de imágenes: Docker Hub, GHCR, ECR |
| **Tag** | Versión | `postgres:17`, `mcr.microsoft.com/dotnet/aspnet:10.0` |

```bash
# Una imagen puede generar múltiples contenedores independientes:
docker run -d postgres:17  # instancia 1
docker run -d postgres:17  # instancia 2 — misma imagen, contenedor distinto
```

---

## Capas de imagen (layers)

Una imagen Docker está compuesta de capas inmutables apiladas:

```
Imagen: back-template:latest
├── Layer 4: COPY ./publish /app  (tu código)          [100 MB]
├── Layer 3: RUN dotnet restore   (dependencias .NET)   [200 MB — SE CACHEA]
├── Layer 2: FROM aspnet:10.0     (runtime .NET)        [100 MB — SE CACHEA]
└── Layer 1: FROM ubuntu:noble    (base OS)              [40 MB  — SE CACHEA]
```

**El caché de capas es la clave de la eficiencia:**
- Si el código cambia solo se reconstruye la capa 4.
- Las capas 1-3 permanecen cacheadas.
- Por esto es importante ordenar los `COPY` del Dockerfile de lo que **menos cambia** a lo que **más cambia**.

---

## Volúmenes

Los contenedores son efímeros — cuando se eliminan, sus datos desaparecen. Los **volúmenes** persisten datos fuera del contenedor:

```yaml
# Docker Compose — volumen nombrado
volumes:
  postgres_data:  # volumen gestionado por Docker

services:
  postgres:
    image: postgres:17
    volumes:
      - postgres_data:/var/lib/postgresql/data  # datos de la DB → volumen
```

```bash
# Tipos de volúmenes:

# 1. Named volume — gestionado por Docker (recomendado para datos)
-v postgres_data:/var/lib/postgresql/data

# 2. Bind mount — carpeta del host montada en el contenedor
-v /home/user/config:/app/config    # Linux/Mac
-v C:\Users\user\config:/app/config # Windows

# 3. tmpfs — en memoria, efímero
--tmpfs /tmp
```

```bash
# Gestión de volúmenes:
docker volume ls                     # listar
docker volume inspect postgres_data  # detalles
docker volume rm postgres_data       # eliminar (la DB se borra)
docker volume prune                  # eliminar todos los no usados
```

---

## Redes

Docker crea redes virtuales para que los contenedores se comuniquen entre sí:

```
Docker Network: back-template_default (bridge)
├── api      → hostname "api",      IP 172.18.0.2
├── postgres → hostname "postgres", IP 172.18.0.3
└── seq      → hostname "seq",      IP 172.18.0.4

api → se conecta a postgres:5432 (no localhost:5432)
api → envía logs a seq:5341     (no localhost:5341)
```

```yaml
# En Docker Compose, los servicios en el mismo compose se comunican por nombre:
services:
  api:
    environment:
      # "postgres" es el nombre del servicio — Docker lo resuelve como DNS
      ConnectionStrings__MainDbConnection: "Host=postgres;Port=5432;..."
      CustomLogging__SeqUri: "http://seq:5341"

  postgres:
    image: postgres:17

  seq:
    image: datalust/seq:latest
```

```bash
# Tipos de redes:
# bridge  — red privada entre contenedores del mismo host (default)
# host    — el contenedor usa la red del host directamente
# overlay — conecta contenedores en múltiples hosts (Docker Swarm)
# none    — sin red
```

---

## Ciclo de vida de un contenedor

```
docker run → Created → Running → (Paused) → Stopped → Removed

docker run     = docker pull (si no existe) + docker create + docker start
docker stop    = señal SIGTERM al proceso principal (graceful shutdown)
docker kill    = señal SIGKILL (forzado)
docker rm      = elimina el contenedor (los datos en volúmenes persisten)
docker rmi     = elimina la imagen (si no hay contenedores usándola)
```

---

## Registries

```bash
# Docker Hub (público por defecto)
docker pull postgres:17
docker pull mcr.microsoft.com/dotnet/aspnet:10.0  # Microsoft Container Registry

# GitHub Container Registry (GHCR)
docker pull ghcr.io/tu-org/back-template:latest

# Amazon ECR
docker pull 123456789.dkr.ecr.us-east-1.amazonaws.com/back-template:latest

# Autenticar en un registry privado
docker login ghcr.io -u USERNAME --password-stdin
```

---

## Ambiente del contenedor — variables de entorno

La forma principal de configurar una aplicación en un contenedor:

```bash
# Pasar variable al correr el contenedor
docker run -e ASPNETCORE_ENVIRONMENT=Production \
           -e Jwt__Key=mi-clave-secreta \
           back-template:latest

# Pasar desde un archivo .env
docker run --env-file .env back-template:latest
```

```yaml
# En Docker Compose — directamente
environment:
  ASPNETCORE_ENVIRONMENT: Production
  Jwt__Key: ${JWT_KEY}  # ← del .env del host

# En Docker Compose — desde archivo
env_file:
  - .env
  - .env.production
```

---

## `host.docker.internal` — conectar contenedor al host

```
Tu máquina (host)
├── PostgreSQL en localhost:5432  (levantado con compose-db.yaml)
└── Contenedor: api
    └── Necesita conectar a PostgreSQL

# Dentro del contenedor "localhost" es el propio contenedor, no el host
# "host.docker.internal" es el DNS especial que apunta al host:

ConnectionStrings__MainDbConnection=Host=host.docker.internal;Port=5432;...
```

En Linux puede requerir `--add-host=host.docker.internal:host-gateway`.

---

## Recursos del sistema

```bash
# Ver uso de recursos de todos los contenedores en tiempo real
docker stats

# Limitar recursos de un contenedor
docker run --memory="512m" --cpus="0.5" back-template:latest

# En Docker Compose:
services:
  api:
    deploy:
      resources:
        limits:
          memory: 512m
          cpus: "0.5"
        reservations:
          memory: 256m
```


---

*Rogelio Arriaga Gonzalez*
