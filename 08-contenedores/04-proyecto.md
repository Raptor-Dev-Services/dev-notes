# 04 — Docker en este Proyecto

Cómo están configurados los archivos Docker/Compose de `back-template` y qué workflow usar según el objetivo.

> Fuente: *Docker: Up and Running 3rd Ed* (Sean Kane, Karl Matthias) — Ch.4 Working with Docker Images

---

## Archivos disponibles

```
back-template/          ← raíz del repo
├── compose-db.yaml     ← RECOMENDADO para dev diario (solo infra)
├── compose-dev.yaml    ← stack completo en Docker
├── compose-staging.yaml
├── compose.yaml        ← producción
└── back-template/
    └── Dockerfile      ← multi-stage build
```

| Archivo | Entorno | API en Docker | Quién lo usa |
|---------|---------|--------------|--------------|
| `compose-db.yaml` | Desarrollo local | No — API corre con `dotnet run` | Dev diario |
| `compose-dev.yaml` | Development | Sí — imagen `back-template:dev` | Probar imagen completa |
| `compose-staging.yaml` | Staging | Sí — imagen `back-template:staging` | Pipeline CI/CD |
| `compose.yaml` | Production | Sí — imagen `back-template:latest` | Prod / QA final |

---

## Flujo recomendado: compose-db.yaml + dotnet run

**El mejor flujo para desarrollo diario.** La infraestructura (PostgreSQL + Seq + Jaeger) corre en Docker; la API corre localmente con hot-reload, depuración e ILogger directo en la consola.

### 1. Levantar infraestructura

Desde la raíz del repo (`back-template/`):

```bash
docker compose -f compose-db.yaml up -d
```

Levanta:
- **PostgreSQL 17** en `localhost:5432` — base `back_template_dev`, usuario `postgres`, contraseña `postgres`
- **Seq** en `http://localhost:5341` — dashboard de logs estructurados
- **Jaeger** en `http://localhost:16686` — UI de trazas distribuidas (OTLP en 4317/4318)

Verificar que están sanos:
```bash
docker compose -f compose-db.yaml ps
```

Datos persistidos en `./data/` (bind mounts):
```
back-template/
└── data/
    ├── postgres/   ← datos de PostgreSQL
    └── seq/        ← datos de Seq
```

### 2. Correr la API localmente

```bash
dotnet run --project back-template/Host --launch-profile Local
```

La API usa `appsettings.Local.json` que apunta a `localhost:5432` y `localhost:5341`.

| Recurso | URL |
|---------|-----|
| API | `http://localhost:5080` |
| Swagger UI | `http://localhost:5080/swagger` |
| Health check | `http://localhost:5080/api/health` |
| Métricas (Prometheus) | `http://localhost:5080/metrics` |
| Seq (logs) | `http://localhost:5341` |
| Jaeger (trazas) | `http://localhost:16686` |

> Las migraciones SQL se aplican automáticamente al iniciar la API. La primera vez verás en los logs que se crea `dbo.ExampleUsers` y sus índices.

### 3. Depurar desde el IDE

Seleccionar el perfil **Local** y presionar F5. `appsettings.Local.json` tiene `IncludeSqlText: true` para ver el SQL completo en los logs de Serilog.

### 4. Apagar infraestructura

```bash
# Apagar contenedores (datos persisten en ./data/)
docker compose -f compose-db.yaml down

# Apagar y borrar datos (reset completo)
docker compose -f compose-db.yaml down -v
rm -rf ./data/    # también borra los bind mounts
```

---

## compose-db.yaml — Desglosado

```yaml
services:
  postgres:
    image: postgres:17-alpine          # alpine = más ligero
    environment:
      POSTGRES_DB: back_template_dev
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres      # solo dev — hardcoded es ok aquí
    ports:
      - "5432:5432"
    volumes:
      - ./data/postgres:/var/lib/postgresql/data  # bind mount local
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d back_template_dev"]
      interval: 5s
      timeout: 5s
      retries: 10
      start_period: 10s

  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_NOAUTHENTICATION: "true"  # sin login en dev
    ports:
      - "5341:80"       # UI de Seq
    volumes:
      - ./data/seq:/data

  jaeger:
    image: jaegertracing/all-in-one:latest
    environment:
      COLLECTOR_OTLP_ENABLED: "true"
    ports:
      - "4317:4317"    # OTLP gRPC
      - "4318:4318"    # OTLP HTTP
      - "16686:16686"  # Jaeger UI
```

**Por qué bind mounts (no named volumes):** Los datos quedan en `./data/` dentro del repo (en `.gitignore`). Fácil de encontrar, inspeccionar con DBeaver, borrar y rehacer.

---

## compose-dev.yaml — Stack completo

Para probar el comportamiento de la imagen de producción o cuando alguien necesita levantar todo sin tener .NET instalado.

```bash
# Primera vez o tras cambios de código
docker compose -f compose-dev.yaml up -d --build

# Sin rebuild (imagen ya existe)
docker compose -f compose-dev.yaml up -d

# Ver logs de la API en tiempo real
docker compose -f compose-dev.yaml logs -f api

# Apagar
docker compose -f compose-dev.yaml down
```

**Qué levanta:**
- `api` → imagen `back-template:dev`, `localhost:8080`
- `postgres` → `localhost:5432`, base `mydb_dev`, datos en named volume `postgres_dev_data`
- `seq` → `localhost:5341`, datos en named volume `seq_dev_data`

**Nota:** La API en este modo usa `ASPNETCORE_ENVIRONMENT=Development`, no `Local`. El SQL detallado no aparece en logs.

### Variables hardcodeadas en compose-dev.yaml

```yaml
# compose-dev.yaml — ok para dev, NUNCA para prod
Jwt__Key: "dev-secret-key-change-me-at-least-32-chars!!"
ConnectionStrings__Postgres: "Host=postgres;Port=5432;Database=mydb_dev;..."
```

---

## compose-staging.yaml — Staging

Requiere variables de entorno del host (o archivo `.env`):

```bash
# Opción 1: variables de entorno del shell
export POSTGRES_PASSWORD=tu_password_staging
export JWT_KEY=tu_jwt_key_de_al_menos_32_chars!!

docker compose -f compose-staging.yaml up -d

# Opción 2: archivo .env en la raíz del repo
# (crear .env.staging y pasarlo explícitamente)
docker compose -f compose-staging.yaml --env-file .env.staging up -d
```

**Qué levanta:**
- `api` → imagen `back-template:staging`, `localhost:8080`, `ASPNETCORE_ENVIRONMENT=Staging`
- `postgres` → base `mydb_staging`, usuario `app_user`
- `seq` → `localhost:5341`
- Todos los servicios con `restart: unless-stopped`

---

## compose.yaml — Producción

```bash
# Requiere .env con POSTGRES_PASSWORD y JWT_KEY
docker compose up -d

# Ver logs
docker compose logs -f api

# Actualizar tras nuevo build
docker compose pull
docker compose up -d --force-recreate
```

**Qué levanta:**
- `api` → imagen `back-template:latest`, `ASPNETCORE_ENVIRONMENT=Production`
- `postgres` → base `mydb`, usuario `app_user`, contraseña desde `${POSTGRES_PASSWORD}`
- `restart: unless-stopped` en todos los servicios

**Diferencia clave con staging:** Solo `api` y `postgres` (sin Seq). En producción los logs van a un stack externo (ELK, Datadog, etc.) o se configura Seq por separado.

---

## Dockerfile — Multi-stage

El Dockerfile está en `back-template/Dockerfile`. Se construye desde la raíz del repo.

### Construir manualmente

```bash
# Desde la raíz del repo (back-template/)
docker build -t back-template:latest -f back-template/Dockerfile .

# Ver el tamaño final
docker images back-template

# Correr la imagen contra la DB local (levantada con compose-db.yaml)
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__MainDbConnection="Host=host.docker.internal;Port=5432;Database=back_template_dev;Username=postgres;Password=postgres" \
  -e Jwt__Key="dev-secret-key-change-me-at-least-32-chars!!" \
  back-template:latest
```

`host.docker.internal` permite al contenedor acceder a servicios en el host (PostgreSQL levantado con `compose-db.yaml`).

### Stages del Dockerfile

| Stage | Base | Qué hace |
|-------|------|---------|
| `restore` | `sdk:10.0` | Copia `.csproj` + `Common/`, corre `dotnet restore` |
| `build` | `restore` | Copia todo el código, `dotnet build -c Release` |
| `publish` | `build` | `dotnet publish -c Release /p:UseAppHost=false` |
| `final` | `aspnet:10.0-noble-chiseled` | Solo el output de publish, usuario `app` no-root |

**`noble-chiseled`** = imagen distroless de Ubuntu Noble:
- Sin shell (`/bin/sh`), sin `apt`, sin usuario root
- ~110 MB vs ~220 MB de la imagen base estándar
- Superficie de ataque mínima — no se puede `docker exec` interactivo

---

## Variables de entorno — referencia rápida

ASP.NET Core convierte `__` en `:` para mapear a secciones del `appsettings.json`:

| Variable de entorno | Equivalente en appsettings |
|--------------------|---------------------------|
| `ConnectionStrings__MainDbConnection` | `ConnectionStrings:MainDbConnection` |
| `Jwt__Key` | `Jwt:Key` |
| `Jwt__Issuer` | `Jwt:Issuer` |
| `Jwt__Audience` | `Jwt:Audience` |
| `CustomLogging__SeqUri` | `CustomLogging:SeqUri` |
| `Observability__OtlpEndpoint` | `Observability:OtlpEndpoint` |
| `ASPNETCORE_ENVIRONMENT` | Entorno (Local/Development/Staging/Production) |
| `ASPNETCORE_HTTP_PORTS` | Puerto HTTP dentro del contenedor |

---

## Problemas comunes

### La API no conecta a PostgreSQL

```yaml
# ✓ Correcto — coincide con el key en appsettings.json
ConnectionStrings__MainDbConnection: "Host=postgres;..."

# ✗ Incorrecto — no coincide
ConnectionStrings__Postgres: "Host=postgres;..."
```

### Puerto 5432 ya en uso en el host

```bash
# Ver qué proceso usa el puerto
netstat -ano | findstr :5432

# Solución: mapear a un puerto diferente en el compose
ports:
  - "5433:5432"   # host 5433 → container 5432
```

Si cambias el puerto, actualiza `appsettings.Local.json`:
```json
"ConnectionStrings": {
  "MainDbConnection": "Host=localhost;Port=5433;..."
}
```

### Imagen desactualizada tras cambios de código

```bash
docker compose -f compose-dev.yaml up -d --build --force-recreate
```

### Ver logs del build completo

```bash
docker compose -f compose-dev.yaml up --build 2>&1 | tee build.log
```

### Base de datos con estado corrupto / datos viejos

```bash
# Reset completo — elimina volúmenes y datos
docker compose -f compose-db.yaml down -v
rm -rf ./data/

# O para compose-dev (named volumes):
docker compose -f compose-dev.yaml down -v
```


---

*Rogelio Arriaga Gonzalez*
