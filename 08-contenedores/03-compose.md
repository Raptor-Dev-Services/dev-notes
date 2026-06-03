# 03 — Docker Compose

Docker Compose permite definir y correr aplicaciones multi-contenedor con un archivo YAML. Con un solo comando levantas toda la infraestructura.

---

## Estructura de un compose file

```yaml
# compose.yaml (o docker-compose.yaml — Docker Compose v2 prefiere compose.yaml)
name: back-template  # nombre del proyecto — prefijo de contenedores y redes

services:            # los contenedores que forman la app
  api:               # nombre del servicio → hostname en la red interna
    build: ...
    ports: ...
    environment: ...
    depends_on: ...

  postgres:
    image: postgres:17
    volumes: ...

volumes:             # volúmenes nombrados
  postgres_data:

networks:            # redes personalizadas (opcional — Docker crea una por defecto)
  internal:
    driver: bridge
```

---

## `services` — la sección principal

### `image` vs `build`

```yaml
services:
  # Opción 1: usar una imagen ya construida
  postgres:
    image: postgres:17          # imagen de Docker Hub
    image: postgres:17-alpine   # variante alpine (más ligera)
    image: ghcr.io/org/app:1.0  # desde registry privado

  # Opción 2: construir desde un Dockerfile
  api:
    build:
      context: .                       # contexto de build
      dockerfile: back-template/Dockerfile  # path al Dockerfile
      args:
        BUILD_CONFIGURATION: Release   # ARG del Dockerfile
      target: final                    # stage final del multi-stage build

  # Forma corta (contexto = directorio actual, busca Dockerfile ahí):
  api:
    build: .
```

### `ports` — exponer puertos

```yaml
services:
  api:
    ports:
      - "8080:8080"        # host_port:container_port
      - "127.0.0.1:8080:8080"  # solo en localhost del host (más seguro)
      - "8080"             # puerto random del host → container 8080

  postgres:
    ports:
      - "5432:5432"        # expone Postgres al host (para conectar con DBeaver)

  seq:
    ports:
      - "5341:80"          # Seq UI en 5341 del host → 80 del contenedor
      - "5342:5341"        # Ingestion port
```

### `environment` — variables de entorno

```yaml
services:
  api:
    environment:
      # Formato lista (más común):
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__Key=${JWT_KEY}              # ← del .env del host
      - ConnectionStrings__MainDbConnection=Host=postgres;Port=5432;Database=mydb;Username=postgres;Password=${POSTGRES_PASSWORD}

      # Formato mapa (equivalente):
      ASPNETCORE_ENVIRONMENT: Production
      Jwt__Key: ${JWT_KEY}

    # O referenciar un archivo .env:
    env_file:
      - .env
      - .env.production     # sobreescribe .env si hay colisiones
```

### `volumes` — montar volúmenes

```yaml
services:
  postgres:
    volumes:
      - postgres_data:/var/lib/postgresql/data    # named volume (datos persistentes)
      - ./init-scripts:/docker-entrypoint-initdb.d  # bind mount (scripts de init)
      - type: tmpfs                                # en memoria
        target: /tmp

  api:
    volumes:
      - ./back-template/Host/appsettings.json:/app/appsettings.json:ro  # read-only
      #                                                                 ↑ :ro = read-only

volumes:
  postgres_data:          # volumen nombrado gestionado por Docker
    driver: local         # (default)

  postgres_data:
    external: true        # volumen ya existe — no crear
    name: shared_db_data  # nombre exacto del volumen externo
```

### `depends_on` — orden de inicio

```yaml
services:
  api:
    depends_on:
      postgres:
        condition: service_healthy  # espera a que postgres esté healthy
      seq:
        condition: service_started  # solo espera a que arranque (sin health check)

  postgres:
    image: postgres:17
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s  # tiempo antes de empezar a verificar
```

**Importante:** `depends_on` controla el orden de inicio de contenedores, NO garantiza que el servicio esté listo para recibir conexiones. Para eso se necesita `condition: service_healthy` con un `healthcheck` definido.

### `healthcheck`

```yaml
services:
  postgres:
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-postgres}"]
      interval: 10s    # verificar cada 10s
      timeout: 5s      # timeout por verificación
      retries: 5       # intentos antes de marcar como unhealthy
      start_period: 15s # tiempo extra al inicio (para que arranque)

  api:
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/api/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 20s

  # Deshabilitar healthcheck de la imagen base:
  some-service:
    healthcheck:
      disable: true
```

### `restart` — política de reinicio

```yaml
services:
  api:
    restart: no              # nunca reiniciar (default)
    restart: always          # siempre reiniciar
    restart: on-failure      # solo si el proceso falla (exit code != 0)
    restart: on-failure:3    # máximo 3 reintentos
    restart: unless-stopped  # reiniciar siempre excepto si se para manualmente
```

---

## `networks` — redes personalizadas

```yaml
services:
  api:
    networks:
      - frontend
      - backend    # puede estar en múltiples redes

  postgres:
    networks:
      - backend    # solo en la red backend — api no expone DB al frontend

networks:
  frontend:
    driver: bridge
  backend:
    driver: bridge
    internal: true  # sin acceso a internet
```

---

## Variables y `.env`

Docker Compose lee automáticamente un archivo `.env` en el directorio donde se corre el comando:

```bash
# .env — variables del entorno del host (NO del contenedor)
POSTGRES_PASSWORD=mi-password-local
JWT_KEY=mi-jwt-key-de-al-menos-32-caracteres!!
POSTGRES_USER=postgres
POSTGRES_DB=back_template_dev
```

```yaml
# compose.yaml — usa ${VARIABLE} para referenciar del .env
services:
  postgres:
    environment:
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_USER: ${POSTGRES_USER:-postgres}     # ← con default
      POSTGRES_DB: ${POSTGRES_DB:-back_template_dev}

  api:
    environment:
      Jwt__Key: ${JWT_KEY:?JWT_KEY es requerido}    # ← falla si está vacío
```

---

## Override files — sobrescribir la configuración

```bash
# Docker Compose aplica compose.yaml + compose.override.yaml automáticamente
# compose.override.yaml sobreescribe/extiende compose.yaml

# Caso de uso: compose.yaml tiene la config base; compose.override.yaml la config de dev
docker compose up                         # aplica compose.yaml + compose.override.yaml
docker compose -f compose.yaml up         # solo compose.yaml
docker compose -f compose.yaml -f compose-dev.yaml up  # compose.yaml + compose-dev.yaml
```

---

## Profiles — servicios opcionales

```yaml
services:
  api:
    image: back-template:latest
    # sin profile = siempre se incluye

  pgadmin:
    image: dpage/pgadmin4
    profiles:
      - tools    # solo se incluye con --profile tools

  seq:
    image: datalust/seq:latest
    profiles:
      - logging  # solo se incluye con --profile logging
```

```bash
docker compose --profile tools up        # incluye pgadmin
docker compose --profile logging up      # incluye seq
docker compose --profile tools --profile logging up  # ambos
```

---

## `deploy` — recursos y réplicas (Swarm / producción)

```yaml
services:
  api:
    deploy:
      replicas: 3           # 3 instancias del contenedor
      restart_policy:
        condition: on-failure
        max_attempts: 3
      resources:
        limits:
          cpus: "0.5"
          memory: 512M
        reservations:
          cpus: "0.25"
          memory: 256M
      update_config:
        parallelism: 1       # actualizar de a 1 réplica
        delay: 10s
        failure_action: rollback
```

---

## Interpolación y variables avanzadas

```yaml
# Valor por defecto si la variable no está definida:
image: back-template:${VERSION:-latest}

# Falla si la variable está vacía:
image: back-template:${VERSION:?La versión es requerida}

# Sustituye solo si NO está definida:
image: back-template:${VERSION:+custom}

# Valor por defecto de variable no definida (sin error):
PORT: ${APP_PORT-8080}
```

---

## `configs` y `secrets` — Swarm mode

```yaml
# Para datos sensibles en producción con Docker Swarm:
services:
  api:
    secrets:
      - jwt_key
      - db_password

secrets:
  jwt_key:
    external: true   # gestionado por Docker Swarm
  db_password:
    file: ./db_password.txt  # desde un archivo local
```

---

## Init containers — ejecutar algo antes que el servicio

```yaml
# Usando depends_on para ejecutar migraciones antes de la API:
services:
  migration:
    image: back-template:latest
    entrypoint: ["dotnet", "Migrator.dll"]
    depends_on:
      postgres:
        condition: service_healthy
    restart: "no"   # solo corre una vez

  api:
    image: back-template:latest
    depends_on:
      migration:
        condition: service_completed_successfully
      postgres:
        condition: service_healthy

---

## Compose para entornos de CI/CD
> Fuente: *Docker Up and Running* (Kane, Matthias): Ch.8 Continuous Integration

Compose es muy útil para levantar servicios de apoyo (DB, caché, message broker) en el pipeline de CI.

```yaml
# compose.test.yaml — solo lo necesario para los tests de integración
name: back-template-test

services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_USER:     testuser
      POSTGRES_PASSWORD: testpass
      POSTGRES_DB:       testdb
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U testuser"]
      interval: 5s
      timeout: 3s
      retries: 10
    # Sin exposición de puertos al host — solo accesible desde otros contenedores
    # ports:
    #   - "5432:5432"  # ← comentado para CI

  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: Y
    # Sin UI en CI
```

```yaml
# compose.yaml — base común
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__MainDb: "Host=postgres;Port=5432;Database=devdb;Username=postgres;Password=${POSTGRES_PASSWORD}"
    depends_on:
      postgres:
        condition: service_healthy

# compose.override.yaml — sobreescribe para desarrollo local
services:
  api:
    ports:
      - "8080:8080"
    volumes:
      - ./appsettings.Development.json:/app/appsettings.Development.json:ro

  postgres:
    ports:
      - "5432:5432"   # exponer para DBeaver en local
```

```bash
# En CI (GitHub Actions / Azure DevOps)

# Levantar solo la infraestructura de test
docker compose -f compose.test.yaml up -d --wait

# Correr los tests (los tests se conectan a los servicios del compose)
dotnet test --filter "Category=Integration"

# Bajar todo al terminar
docker compose -f compose.test.yaml down -v  # -v elimina también los volúmenes
```

```yaml
# .github/workflows/ci.yaml — fragmento relevante
jobs:
  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Levantar servicios de test
        run: docker compose -f compose.test.yaml up -d --wait

      - name: Ejecutar integration tests
        run: dotnet test --filter "Category=Integration"
        env:
          ConnectionStrings__MainDb: "Host=localhost;Port=5432;Database=testdb;Username=testuser;Password=testpass"

      - name: Bajar servicios
        if: always()   # ejecutar aunque los tests fallen
        run: docker compose -f compose.test.yaml down -v
```
```

---

## Glosario

| Término | Definición |
|---------|-----------|
| Docker Compose | herramienta que define y orquesta múltiples contenedores como un único servicio usando un archivo YAML |
| Service | definición de un contenedor en Compose; equivale a un proceso de la aplicación (api, postgres, seq) |
| `depends_on` | clave de Compose que define el orden de inicio y la condición de salud requerida antes de arrancar un servicio |
| Healthcheck | comando que Compose ejecuta periódicamente para verificar si un servicio está listo para recibir tráfico |
| Volume mount | mapeo entre un directorio del host y un directorio del contenedor para persistir datos o inyectar archivos |
| Port mapping | configuración `host:container` que expone un puerto del contenedor al host |
| Profile | etiqueta que agrupa servicios opcionales; solo se levantan cuando se especifica `--profile` |
| Override file | archivo `compose.override.yaml` que sobreescribe o extiende la configuración base de `compose.yaml` |
| `docker compose up -d` | comando que levanta todos los servicios del compose en modo daemon (background) |
| `docker compose down -v` | comando que detiene todos los servicios y elimina también los volúmenes nombrados |
| Graceful shutdown | proceso de terminación controlada donde el contenedor espera que las conexiones activas se completen |

---

*Rogelio Arriaga Gonzalez*
