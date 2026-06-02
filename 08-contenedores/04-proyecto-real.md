# 04 — Docker en un Proyecto Real (back-template + front-template)

El problema con "funciona en mi máquina" desaparece cuando toda la pila — API, base de datos, servidor de logs — corre en contenedores definidos como código. Este documento cubre el setup completo de Docker para el stack del proyecto.

> Fuente: Documentación oficial Docker Compose v2 — https://docs.docker.com/compose/

---

## El problema que resuelve

```bash
# ❌ Sin Docker — cada dev configura manualmente
# Dev A: PostgreSQL 15 instalado globalmente con puerto 5433
# Dev B: PostgreSQL 17 en WSL2 con puerto 5432
# Dev C: sin PostgreSQL — usa SQLite para tests
# → La app se comporta distinto en cada máquina
```

```bash
# ✓ Con Docker — entorno idéntico en todas las máquinas
docker compose up -d
# → PostgreSQL 17, Seq, Redis — misma versión, mismo puerto, en todos los devs y en CI
```

---

## Dockerfile multi-stage para ASP.NET Core

```dockerfile
# back-template/Dockerfile

# ── Etapa 1: Restaurar dependencias ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
# Copiar solo los .csproj primero — Docker cachea esta capa si no cambian
COPY ["Host/Host.csproj",                               "Host/"]
COPY ["GTM.Suite.Domain/GTM.Suite.Domain.csproj",       "GTM.Suite.Domain/"]
COPY ["GTM.Suite.Application/GTM.Suite.Application.csproj", "GTM.Suite.Application/"]
COPY ["GTM.Suite.Infrastructure/GTM.Suite.Infrastructure.csproj", "GTM.Suite.Infrastructure/"]
COPY ["GTM.Suite.WebApi/GTM.Suite.WebApi.csproj",       "GTM.Suite.WebApi/"]
RUN dotnet restore "Host/Host.csproj"

# ── Etapa 2: Build ────────────────────────────────────────────────────────────
FROM restore AS build
COPY . .
WORKDIR /src/Host
RUN dotnet build "Host.csproj" -c Release --no-restore

# ── Etapa 3: Publish ──────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "Host.csproj" -c Release -o /app/publish \
    --no-build \
    /p:UseAppHost=false

# ── Etapa 4: Runtime (imagen final — sin SDK) ─────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Usuario no-root por seguridad
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Host.dll"]
```

```dockerfile
# front-template/Dockerfile

# ── Etapa 1: Build React ──────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json .
RUN npm ci --frozen-lockfile
COPY . .
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# ── Etapa 2: Nginx para servir los archivos estáticos ────────────────────────
FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

```nginx
# front-template/nginx.conf
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    # SPA fallback — todas las rutas apuntan a index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache agresivo para assets con hash en el nombre
    location ~* \.(js|css|png|jpg|svg|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Evitar cachear index.html — el punto de entrada siempre debe ser fresco
    location = /index.html {
        expires -1;
        add_header Cache-Control "no-store";
    }
}
```

---

## docker-compose.yml — entorno de desarrollo

```yaml
# docker-compose.yml — para desarrollo local
services:
  api:
    build:
      context: ./back-template
      dockerfile: Dockerfile
    image: gtm-api:dev
    container_name: gtm-api
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=gtm_dev;Username=app_user;Password=dev_pass"
      Jwt__SigningKey: ${JWT_SIGNING_KEY}          # ← viene del .env
      Seq__ServerUrl: http://seq:5341
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy      # espera a que postgres esté listo
    networks:
      - gtm-network
    volumes:
      - ~/.aspnet/https:/home/appuser/.aspnet/https:ro   # certificado HTTPS dev

  frontend:
    build:
      context: ./front-template
      dockerfile: Dockerfile
      args:
        VITE_API_URL: http://localhost:8080
    image: gtm-frontend:dev
    container_name: gtm-frontend
    ports:
      - "3000:80"
    networks:
      - gtm-network
    depends_on:
      - api

  postgres:
    image: postgres:17-alpine
    container_name: gtm-postgres
    environment:
      POSTGRES_DB: gtm_dev
      POSTGRES_USER: app_user
      POSTGRES_PASSWORD: dev_pass
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./back-template/scripts/init.sql:/docker-entrypoint-initdb.d/init.sql:ro
    ports:
      - "5432:5432"
    networks:
      - gtm-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app_user -d gtm_dev"]
      interval: 5s
      timeout: 5s
      retries: 10
      start_period: 10s

  seq:
    image: datalust/seq:2024.3
    container_name: gtm-seq
    environment:
      ACCEPT_EULA: "Y"
    volumes:
      - seqdata:/data
    ports:
      - "5341:5341"   # ingest API
      - "8081:80"     # UI en http://localhost:8081
    networks:
      - gtm-network

volumes:
  pgdata:
  seqdata:

networks:
  gtm-network:
    driver: bridge
```

---

## docker-compose.override.yml — overrides de desarrollo

```yaml
# docker-compose.override.yml — se aplica automáticamente sobre docker-compose.yml en local
# Este archivo va en .gitignore si tiene secretos reales
services:
  api:
    environment:
      Logging__LogLevel__Default: Debug
      Logging__LogLevel__Microsoft.EntityFrameworkCore: Information
    volumes:
      - ./back-template:/src:ro   # hot reload del código (con dotnet watch)
```

---

## .env — variables de entorno

```env
# .env — en .gitignore, nunca al repo
JWT_SIGNING_KEY=dev-only-key-min-32-characters-long-for-hmacsha256
DB_PASSWORD=dev_pass
STRIPE_SECRET_KEY=sk_test_...
APP_VERSION=1.0.0
```

```bash
# Verificar que las variables están disponibles para Compose
docker compose config  # muestra la config con variables interpoladas
```

---

## docker-compose.prod.yml — producción

```yaml
# docker-compose.prod.yml — para producción (no incluir en git; generar en CI)
services:
  api:
    image: registry.midominio.com/gtm-api:${APP_VERSION}
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__Default: ${DB_CONNECTION_PROD}
      Jwt__SigningKey: ${JWT_SIGNING_KEY_PROD}
    deploy:
      resources:
        limits:
          memory: 512m
          cpus: "0.5"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    logging:
      driver: "awslogs"
      options:
        awslogs-group: /gtm-suite/api
        awslogs-region: us-east-1
        awslogs-stream-prefix: api
```

---

## Comandos frecuentes

```bash
# Levantar todo en background
docker compose up -d

# Ver logs en tiempo real
docker compose logs -f api
docker compose logs -f postgres

# Rebuildar solo la API (cuando cambió el código)
docker compose build api && docker compose up -d api

# Entrar al contenedor de la API
docker compose exec api sh

# Entrar al contenedor de PostgreSQL y abrir psql
docker compose exec postgres psql -U app_user -d gtm_dev

# Ver recursos usados por cada contenedor
docker stats

# Parar y eliminar contenedores (preserva volúmenes)
docker compose down

# Parar y eliminar todo incluyendo volúmenes (BORRA LA BASE DE DATOS)
docker compose down -v

# Ver estado de health checks
docker compose ps

# Aplicar migrations desde el host (usa Migrate at startup en la API)
# O bien correrlo manualmente:
docker compose exec api dotnet ef database update --project GTM.Suite.Infrastructure
```

---

## Health checks en la API (.NET)

```csharp
// Program.cs — endpoint de health check
builder.Services
    .AddHealthChecks()
    .AddNpgsql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "postgres",
        tags: ["db"])
    .AddUrlGroup(
        new Uri("http://seq:5341/health"),
        name: "seq",
        tags: ["logging"]);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});
```

---

## Relación con el back-template

El back-template expone en `Host/Program.cs`:
- El endpoint `/health` usado por el health check de Docker
- La configuración por variables de entorno (`ConnectionStrings__Default`, `Jwt__SigningKey`)
- El `ASPNETCORE_URLS` que determina el puerto interno del contenedor

Las migrations se aplican automáticamente al iniciar si `ASPNETCORE_ENVIRONMENT=Development`. En producción se aplican como un paso separado en el pipeline CI/CD.

---

## Cuándo usar / no usar Docker Compose

| Usar Docker Compose | No usar / alternativas |
|--------------------|-----------------------|
| Entorno de desarrollo local idéntico para todo el equipo | App de una sola persona sin dependencias externas |
| CI/CD — levantar la BD para tests de integración | Producción a gran escala → Kubernetes / ECS |
| Demo / staging en una sola VM | Cuando las dependencias cambian muy seguido y el rebuild es lento |
| Testing de configuraciones multi-servicio | |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Multi-stage build | Dockerfile con múltiples FROM — la imagen final solo incluye los artefactos del stage final, sin SDK |
| docker-compose.yml | Archivo que define múltiples servicios, redes y volúmenes como código versionable |
| depends_on | Directiva de Compose para especificar orden de arranque — con `condition: service_healthy` espera al health check |
| Health check | Comando que Docker ejecuta periódicamente para saber si un contenedor está listo para recibir tráfico |
| Volume | Directorio persistente fuera del contenedor — los datos sobreviven a reinicios y recreaciones |
| Named volume | `pgdata:` — gestionado por Docker, persiste entre `docker compose down` (se elimina con `-v`) |
| Bind mount | `./codigo:/src` — monta un directorio del host dentro del contenedor — útil para hot reload |
| Network bridge | Red virtual privada entre contenedores — se comunican por nombre de servicio (`postgres`, `seq`) |
| `docker compose override` | Archivo que se fusiona automáticamente — permite diferencias entre dev y CI sin duplicar YAML |
| `.env` | Archivo de variables de entorno que Docker Compose carga automáticamente — va en `.gitignore` |
| ASPNETCORE_URLS | Variable de entorno que determina el puerto y protocolo que escucha Kestrel dentro del contenedor |
| Seq | Servidor de logs estructurados con UI web — sustituto de ELK para proyectos pequeños/medianos |
| `pg_isready` | Comando de PostgreSQL usado en el health check — retorna 0 cuando la BD acepta conexiones |
| Kestrel | Servidor web embebido de ASP.NET Core — el proceso que corre dentro del contenedor de la API |

---

*Rogelio Arriaga Gonzalez*
