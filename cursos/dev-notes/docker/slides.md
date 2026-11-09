<!-- _class: lead -->

# Docker
## Dockerfile  -  Docker Compose  -  Comandos esenciales

Dev Notes  -  Material extra
Rogelio Arriaga  -  Raptor Dev Services

---

<!-- _class: lead -->

# Parte 1
## Conceptos fundamentales

---

## El problema sin Docker

Sin Docker, cada dev tiene un entorno diferente:

| Problema | Consecuencia |
|----------|-------------|
| "En mi máquina funciona" | Bugs solo reproducibles en prod |
| Versiones distintas de .NET / Node | Builds que fallan según el dev |
| PostgreSQL local con datos distintos | Tests no deterministas |
| Onboarding = 2 días instalando cosas | Tiempo perdido |

**Docker resuelve esto empaquetando la app y su entorno en una unidad reproducible.**

---

## Imagen vs Contenedor

| Concepto | Analogía | Descripción |
|----------|----------|-------------|
| **Imagen** | Clase | Plantilla de solo lectura  -  capas inmutables apiladas |
| **Contenedor** | Instancia | Imagen en ejecución con una capa de escritura temporal |
| **Registry** | npm / NuGet | Repositorio de imágenes (Docker Hub, GHCR, ECR) |

```
Imagen (read-only)          Contenedor (en ejecución)
+-----------------+         +---------------------+
|  Layer 3: app   |  +  RW  |  Layer RW (temporal) |
|  Layer 2: .NET  |  layer  |  Layer 3: app        |
|  Layer 1: OS    | ------ |  Layer 2: .NET       |
`-----------------+         |  Layer 1: OS         |
                            `---------------------+
```

---

## Layer cache  -  por qué importa

Cada instrucción del Dockerfile crea una capa. Docker **reutiliza capas en caché** si no cambiaron.

```
docker build -t mensajeria-api:v1.0.0 .

 => [1/6] FROM mcr.microsoft.com/dotnet/sdk:10.0         CACHED  <- reutilizado
 => [2/6] WORKDIR /src                                    CACHED
 => [3/6] COPY *.csproj .                                 CACHED  <- no cambió
 => [4/6] RUN dotnet restore                              CACHED  <- 60s ahorrados
 => [5/6] COPY . .                                        DONE    <- código cambió
 => [6/6] RUN dotnet publish                              DONE    <- re-ejecutado
```

**Regla de oro:** copiar lo que cambia poco (`.csproj`) antes que lo que cambia frecuente (código).

---

## Registry  -  imagen a producción

```
+-------------+   docker build   +--------------+   docker push   +--------------+
|  Código     | --------------  |  Imagen      | -------------- |  Registry    |
|  CRM API    |                  |  crm-api     |                 |  GHCR / ECR  |
`-------------+                  |  :v1.2.0     |                 `--------------+
                                 `--------------+                        |
                                                                  docker pull
                                                                         |
                                                                  +------------+
                                                                  |  Servidor   |
                                                                  |  producción |
                                                                  `-------------+
```

> Tags en producción: `:v1.2.0` o `:${GIT_SHA}`  -  **NUNCA `:latest`**

---

<!-- _class: lead -->

# Parte 2
## Dockerfile  -  instrucciones esenciales

---

## Instrucciones principales

```dockerfile
# FROM  -  imagen base de la que parte esta imagen
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# WORKDIR  -  directorio de trabajo (lo crea si no existe)
WORKDIR /src

# COPY  -  copiar archivos del contexto de build al contenedor
COPY ["Host/Host.csproj", "Host/"]
COPY . .

# RUN  -  ejecutar comando durante el BUILD (genera una capa)
RUN dotnet restore "Host/Host.csproj"

# ENV  -  variable de entorno disponible en BUILD y en runtime
ENV ASPNETCORE_ENVIRONMENT=Production

# ARG  -  variable solo disponible durante el BUILD (no en runtime)
ARG BUILD_CONFIGURATION=Release
```

---

## Instrucciones principales (cont.)

```dockerfile
# EXPOSE  -  documenta el puerto (informativo, no lo publica)
EXPOSE 8080

# USER  -  cambiar el usuario que ejecuta los procesos
USER app

# ENTRYPOINT  -  comando principal (no se sobreescribe fácilmente)
ENTRYPOINT ["dotnet", "Host.dll"]

# CMD  -  argumentos por defecto para ENTRYPOINT (sobreescribible)
CMD ["--environment", "Production"]
# Resultado combinado: dotnet Host.dll --environment Production

# HEALTHCHECK  -  cómo verificar si el contenedor está sano
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1
```

---

## ARG vs ENV

```dockerfile
# ARG  -  solo durante el BUILD, no llega al contenedor en runtime
ARG BUILD_CONFIGURATION=Release
ARG VERSION                         # sin default  -  debe pasarse en build

RUN dotnet publish -c ${BUILD_CONFIGURATION}
```

```bash
# Pasar ARG desde el comando de build:
docker build \
  --build-arg BUILD_CONFIGURATION=Debug \
  --build-arg VERSION=1.2.3 \
  -t mensajeria-api:debug .
```

```dockerfile
# ENV  -  disponible en BUILD y en el CONTENEDOR en runtime
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
```

```bash
# Sobreescribir ENV al correr el contenedor:
docker run -e ASPNETCORE_ENVIRONMENT=Staging mensajeria-api:v1.0.0
```

---

## ENTRYPOINT vs CMD

```dockerfile
# ENTRYPOINT  -  siempre corre, no se sobreescribe con docker run args
ENTRYPOINT ["dotnet", "Host.dll"]

# CMD  -  argumentos por defecto, se sobreescriben con docker run args
CMD ["--environment", "Production"]
# Resultado: dotnet Host.dll --environment Production
```

```bash
# Sobreescribir CMD en docker run:
docker run mensajeria-api:v1.0.0 --environment Development
# Resultado: dotnet Host.dll --environment Development

# Sobreescribir ENTRYPOINT (debugging):
docker run --entrypoint /bin/bash mensajeria-api:v1.0.0
# <- falla en imágenes distroless (no hay shell)
```

> Usa siempre la forma **exec** `["cmd", "arg"]`, no la forma shell `cmd arg`.
> La forma shell lanza un proceso `/bin/sh -c` intermediario que no recibe SIGTERM.

---

## .dockerignore  -  obligatorio

El contexto de build es todo lo que Docker envía al daemon. Sin `.dockerignore` puedes enviar GBs.

```dockerignore
# Directorios de build  -  el Dockerfile los genera desde cero
**/bin/
**/obj/

# Node.js
**/node_modules/

# Git
.git/
.gitmodules

# IDE
.vs/
.idea/
**/*.user

# Secretos  -  NUNCA en la imagen
**/.env
**/.env.*
**/appsettings.Local.json

# Docker
**/compose*.yaml
**/Dockerfile
```

> Sin `bin/` y `obj/`: el contexto puede crecer GBs e invalidar el caché con archivos irrelevantes.

---

<!-- _class: lead -->

# Parte 3
## Multi-stage build para .NET 10

---

## ¿Por qué multi-stage?

| Stage | Imagen base | Tamaño | Contiene |
|-------|-------------|--------|----------|
| `restore` / `build` | `sdk:10.0` | ~740 MB | SDK + herramientas de build |
| `final` (producción) | `aspnet:10.0-noble-chiseled` | ~110 MB | Solo runtime ASP.NET |

**Sin multi-stage:** la imagen final incluiría el SDK completo (~740 MB), el código fuente y las herramientas de compilación  -  una superficie de ataque enorme.

**Con multi-stage:** la imagen final solo contiene el `publish` output + el runtime necesario.

> `noble-chiseled` = Ubuntu Noble distroless: sin shell, sin `apt`, sin usuario root. Superficie de ataque mínima.

---

## Dockerfile multi-stage .NET 10  -  stages 1 y 2

```dockerfile
# Stage 1: restore  -  cachear dependencias NuGet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY ["Host/Host.csproj",                     "Host/"]
COPY ["Domain/Domain.csproj",                 "Domain/"]
COPY ["Application/Application.csproj",       "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["WebApi/WebApi.csproj",                 "WebApi/"]

RUN dotnet restore "Host/Host.csproj"

# Stage 2: build  -  compilar el código
FROM restore AS build
WORKDIR /src
COPY . .

ARG BUILD_CONFIGURATION=Release
RUN dotnet build "Host/Host.csproj" \
    -c ${BUILD_CONFIGURATION} \
    --no-restore \
    -o /app/build
```

---

## Dockerfile multi-stage .NET 10  -  stages 3 y 4

```dockerfile
# Stage 3: publish  -  output de producción
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Host/Host.csproj" \
    -c ${BUILD_CONFIGURATION} \
    --no-build \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 4: final  -  imagen de producción (distroless)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app

USER app          # usuario no-root incluido en la imagen base
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Solo el output de publish  -  sin SDK, sin código fuente
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Host.dll"]
```

---

## Optimización del caché de capas

```dockerfile
#  Sin optimizar  -  cualquier cambio de código re-ejecuta restore (~60s)
FROM sdk:10.0
COPY . .
RUN dotnet restore
RUN dotnet build
```

```dockerfile
# [OK] Optimizado  -  restore se cachea mientras .csproj no cambie
FROM sdk:10.0
# Primero: .csproj (cambian rara vez)
COPY ["Host/Host.csproj", "Host/"]
RUN dotnet restore
# Segundo: código fuente (cambia frecuentemente)
COPY . .
RUN dotnet build
```

> El truco: copiar **solo los archivos que cambian poco** antes de ejecutar `dotnet restore`.
> Si los `.csproj` no cambiaron, Docker reutiliza la capa de restore completa.

---

<!-- _class: lead -->

# Parte 4
## Dockerfile para React (Vite + Nginx)

---

## Multi-stage React  -  build con Node, serve con Nginx

```dockerfile
# Stage 1: build  -  instalar deps y compilar la app
FROM node:22-alpine AS build
WORKDIR /app

# Copiar package files primero para cachear node_modules
COPY package.json package-lock.json ./
RUN npm ci --frozen-lockfile

# Copiar código fuente y compilar
COPY . .
ARG VITE_API_URL=https://api.mensajeria.com
ENV VITE_API_URL=${VITE_API_URL}
RUN npm run build

# Stage 2: final  -  servir el bundle con Nginx
FROM nginx:1.27-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

---

## nginx.conf  -  SPA routing + proxy a la API

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    # Todas las rutas que no sean archivos -> index.html (React Router)
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Proxy a la API (evita CORS en producción)
    location /api/ {
        proxy_pass http://mensajeria-api:8080/api/;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Cache de assets compilados (Vite genera hashes únicos)
    location ~* \.(js|css|png|jpg|svg|ico|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

---

<!-- _class: lead -->

# Parte 5
## Docker Compose  -  orquestar múltiples servicios

---

## Estructura de un compose file

```yaml
# compose.yaml  -  Docker Compose v2 prefiere este nombre
name: mensajeria   # prefijo de contenedores y redes

services:             # los contenedores de la app
  api:
    build: ...
    ports: ...
    environment: ...
    depends_on: ...

  web:
    build: ...

  postgres:
    image: postgres:17-alpine
    volumes: ...

volumes:              # volúmenes nombrados (datos persistentes)
  postgres_data:

networks:             # redes personalizadas
  backend:
    driver: bridge
```

---

## `image` vs `build`

```yaml
services:
  # Opción A: imagen ya construida (desde registry)
  postgres:
    image: postgres:17-alpine

  # Opción B: construir desde Dockerfile local
  api:
    build:
      context: .                            # raíz del repo
      dockerfile: back-end/Dockerfile       # path al Dockerfile
      args:
        BUILD_CONFIGURATION: Release        # ARG del Dockerfile
      target: final                         # stage final del multi-stage
```

```yaml
  # Forma corta (busca Dockerfile en el directorio actual)
  api:
    build: .
```

---

## `ports`, `environment`, `env_file`

```yaml
services:
  api:
    ports:
      - "8080:8080"           # host_port:container_port
      - "127.0.0.1:8080:8080" # solo en localhost (más seguro)

    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__Key=${JWT_KEY}             # <- viene del .env del host
      - ConnectionStrings__MainDb=Host=postgres;Port=5432;Database=crm;...

    # Alternativa: referenciar un archivo .env
    env_file:
      - .env
      - .env.production       # sobreescribe .env si hay colisiones
```

```bash
# .env  -  variables del host (NO del contenedor), en .gitignore
POSTGRES_PASSWORD=s3cr3t_p4ss
JWT_KEY=mensajeria-jwt-key-al-menos-32-chars!!
```

---

## `volumes`  -  tipos

```yaml
services:
  postgres:
    volumes:
      # Named volume: Docker gestiona dónde viven los datos
      - postgres_data:/var/lib/postgresql/data

      # Bind mount: carpeta local -> carpeta en contenedor
      - ./init-scripts:/docker-entrypoint-initdb.d

  api:
    volumes:
      # Read-only bind mount
      - ./appsettings.json:/app/appsettings.json:ro

volumes:
  postgres_data:        # named volume declarado aquí
    driver: local
```

| Tipo | Dónde viven los datos | Uso |
|------|-----------------------|-----|
| Named volume | Gestionado por Docker | Datos de DB en producción |
| Bind mount | Carpeta del host | Config, scripts de init, dev hot-reload |

---

## `depends_on` y `healthcheck`

```yaml
services:
  api:
    depends_on:
      postgres:
        condition: service_healthy  # espera a que postgres esté sano
      seq:
        condition: service_started  # solo espera que arranque

  postgres:
    image: postgres:17-alpine
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s
```

> `depends_on` sin `condition: service_healthy` solo controla el **orden de inicio**, no garantiza que el servicio esté listo para conexiones. Siempre usa `healthcheck` con PostgreSQL.

---

## `networks`  -  aislamiento entre servicios

```yaml
services:
  api:
    networks:
      - frontend    # accesible desde el navegador/web
      - backend     # accede a la DB

  web:
    networks:
      - frontend    # solo en la red pública

  postgres:
    networks:
      - backend     # no expuesto al frontend  -  solo api puede conectarse

networks:
  frontend:
    driver: bridge
  backend:
    driver: bridge
    internal: true  # sin acceso a internet
```

> Los servicios en la misma network se comunican por **nombre de servicio** como hostname: `Host=postgres;Port=5432`.

---

<!-- _class: lead -->

# Parte 6
## docker-compose.yml del proyecto de mensajería

---

## compose.yaml completo  -  parte 1 (api + web)

```yaml
name: mensajeria

services:
  api:
    build:
      context: .
      dockerfile: back-end/Dockerfile
      target: final
    image: mensajeria-api:${IMAGE_TAG:-dev}
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_HTTP_PORTS=8080
      - ConnectionStrings__MainDb=Host=postgres;Port=5432;Database=crm;Username=app_user;Password=${POSTGRES_PASSWORD}
      - Jwt__Key=${JWT_KEY}
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - backend
    restart: unless-stopped
```

---

## compose.yaml completo  -  parte 2 (web + postgres)

```yaml
  web:
    build:
      context: .
      dockerfile: front-end/Dockerfile
      args:
        VITE_API_URL: ${VITE_API_URL:-http://localhost:8080}
    image: mensajeria-web:${IMAGE_TAG:-dev}
    ports:
      - "3000:80"
    depends_on:
      - api
    networks:
      - frontend
    restart: unless-stopped

  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB:       crm
      POSTGRES_USER:     app_user
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD es requerido}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app_user -d crm"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 15s
    networks:
      - backend
    restart: unless-stopped
```

---

## compose.yaml completo  -  parte 3 (volumes + networks + .env)

```yaml
volumes:
  postgres_data:
    driver: local

networks:
  frontend:
    driver: bridge
  backend:
    driver: bridge
    internal: true
```

```bash
# .env (en .gitignore  -  NUNCA commitear)
POSTGRES_PASSWORD=s3cr3tp4ss
JWT_KEY=mensajeria-jwt-key-de-al-menos-32-chars!!
VITE_API_URL=http://localhost:8080
IMAGE_TAG=v1.0.0
```

```bash
# Levantar el stack completo
docker compose up -d

# Ver estado
docker compose ps

# Logs de la API
docker compose logs -f api
```

---

## Tags en producción  -  la regla que no se rompe

```bash
#  NUNCA en producción  -  :latest no es reproducible
docker build -t mensajeria-api:latest .
docker compose pull  # <- ¿qué versión descarga? nadie sabe

# [OK] Usar el SHA del commit  -  reproducible y trazable
GIT_SHA=$(git rev-parse --short HEAD)
docker build -t ghcr.io/acme/mensajeria-api:${GIT_SHA} .

# [OK] O usar SemVer en releases
docker build -t ghcr.io/acme/mensajeria-api:v1.2.0 .
docker build -t ghcr.io/acme/mensajeria-api:v1.2.0 .
```

```yaml
# En el compose de producción:
services:
  api:
    image: ghcr.io/acme/mensajeria-api:v1.2.0  # <- version fija
```

> `:latest` en producción = no sabes qué versión está corriendo, no puedes hacer rollback reproducible, y un `docker pull` puede romper prod sin avisar.

---

<!-- _class: lead -->

# Parte 7
## Comandos esenciales

---

## docker build

```bash
# Construir imagen (busca Dockerfile en el directorio actual)
docker build -t mensajeria-api:v1.0.0 .

# Especificar Dockerfile y contexto
docker build -t mensajeria-api:v1.0.0 -f back-end/Dockerfile .

# Pasar build args
docker build --build-arg BUILD_CONFIGURATION=Debug \
             -t mensajeria-api:debug .

# No usar caché (forzar rebuild completo)
docker build --no-cache -t mensajeria-api:v1.0.0 .

# Ver output detallado del build
docker build --progress=plain -t mensajeria-api:v1.0.0 .

# Tag con SHA del commit (CI/CD)
GIT_SHA=$(git rev-parse --short HEAD)
docker build -t mensajeria-api:${GIT_SHA} .
```

---

## docker run

```bash
# Correr en background con nombre y puerto
docker run -d --name crm-api -p 8080:8080 mensajeria-api:v1.0.0

# Con variables de entorno
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Jwt__Key=mi-clave-secreta \
  mensajeria-api:v1.0.0

# Desde archivo .env
docker run -d --env-file .env mensajeria-api:v1.0.0

# Montar volumen (datos persistentes)
docker run -d -v postgres_data:/var/lib/postgresql/data postgres:17

# Eliminar automáticamente al parar (útil para pruebas)
docker run --rm mensajeria-api:v1.0.0
```

---

## docker ps  -  docker logs  -  docker exec

```bash
# Contenedores en ejecución
docker ps

# Todos (incluye parados)
docker ps -a

# Formato personalizado
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

```bash
# Ver logs de un contenedor
docker logs crm-api

# Seguir en tiempo real (tail -f)
docker logs -f crm-api

# Últimas 100 líneas y seguir
docker logs --tail 100 -f crm-api
```

```bash
# Abrir shell interactivo (si tiene shell)
docker exec -it crm-api /bin/bash

# Ejecutar comando puntual
docker exec crm-api ls /app
```

---

## docker stop  -  rm  -  images  -  system prune

```bash
# Parar (SIGTERM -> graceful shutdown -> SIGKILL)
docker stop crm-api

# Eliminar contenedor parado
docker rm crm-api

# Parar y eliminar en un paso
docker rm -f crm-api
```

```bash
# Listar imágenes locales
docker images

# Eliminar imagen
docker rmi mensajeria-api:v1.0.0

# Eliminar imágenes sin tag (<none>)
docker image prune
```

```bash
# Limpiar todo lo no usado (contenedores, redes, imágenes dangling, build cache)
docker system prune

# Incluir volúmenes sin uso (¡borra datos!)
docker system prune --volumes

# Ver cuánto espacio usa Docker
docker system df
```

---

## docker compose  -  comandos del día a día

```bash
# Levantar todos los servicios en background
docker compose up -d

# Levantar y reconstruir imágenes
docker compose up -d --build

# Especificar archivo compose
docker compose -f compose-dev.yaml up -d --build

# Apagar (sin borrar volúmenes)
docker compose down

# Apagar y borrar volúmenes nombrados (reset completo)
docker compose down -v

# Estado de los servicios
docker compose ps

# Logs de todos los servicios
docker compose logs -f

# Logs de un servicio específico
docker compose logs -f api

# Ejecutar en un servicio en ejecución
docker compose exec postgres psql -U app_user -d crm
```

---

## Flujo CI/CD  -  build to deploy

```bash
# 1. Build con SHA del commit
GIT_SHA=$(git rev-parse --short HEAD)
docker build -t ghcr.io/acme/mensajeria-api:${GIT_SHA} \
             -f back-end/Dockerfile .

# 2. Tag como release
docker tag ghcr.io/acme/mensajeria-api:${GIT_SHA} \
           ghcr.io/acme/mensajeria-api:v1.2.0

# 3. Push a registry
docker push ghcr.io/acme/mensajeria-api:${GIT_SHA}
docker push ghcr.io/acme/mensajeria-api:v1.2.0

# 4. Deploy en servidor
docker compose pull
docker compose up -d --force-recreate
```

> El tag `:latest` puede existir en local para conveniencia, pero en el compose de producción siempre va la versión fija.

---

## Resumen  -  reglas del proyecto

| Regla |  Antipatrón | [OK] Correcto |
|-------|--------------|-----------|
| Tags en producción | `:latest` | `:v1.2.0` o `:${GIT_SHA}` |
| Imagen producción | Una sola stage con SDK | Multi-stage: build -> publish -> final |
| Imagen base final | `sdk:10.0` (740 MB) | `aspnet:10.0-noble-chiseled` (110 MB) |
| `.dockerignore` | Ausente -> GBs de contexto | `bin/`, `obj/`, `node_modules/`, `.env` |
| Secretos en imagen | `COPY .env .` o `ARG SECRET` | Vars de entorno en runtime (`-e` / compose) |
| Usuario en contenedor | root (default) | `USER app` (no-root) |
| `depends_on` con DB | Sin `healthcheck` | `condition: service_healthy` + `healthcheck` |
