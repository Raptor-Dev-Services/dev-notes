# 02 — Dockerfile

El Dockerfile es el script que define cómo construir una imagen Docker. Cada instrucción crea una capa.

---

## Instrucciones esenciales

```dockerfile
# FROM — imagen base de la que parte esta imagen
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# WORKDIR — directorio de trabajo dentro del contenedor (lo crea si no existe)
WORKDIR /src

# COPY — copiar archivos del contexto de build al contenedor
COPY ["Host/Host.csproj", "Host/"]         # archivo específico
COPY ./src /app                             # carpeta completa

# RUN — ejecutar comando durante el BUILD (genera una capa)
RUN dotnet restore "Host/Host.csproj"
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# ENV — variable de entorno disponible en build Y en runtime
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# ARG — variable solo disponible durante el BUILD (no en runtime)
ARG BUILD_CONFIGURATION=Release
RUN dotnet build -c ${BUILD_CONFIGURATION}

# EXPOSE — documenta el puerto (NO lo publica — solo informativo)
EXPOSE 8080

# USER — cambiar el usuario que ejecuta los procesos
USER app

# ENTRYPOINT — comando principal del contenedor (no se sobreescribe fácilmente)
ENTRYPOINT ["dotnet", "Host.dll"]

# CMD — argumentos por defecto para ENTRYPOINT (se puede sobreescribir)
CMD ["--environment", "Production"]

# VOLUME — declara que esta ruta debe ser un volumen
VOLUME /app/logs

# LABEL — metadatos de la imagen
LABEL maintainer="Raptor Dev Services" version="1.0"

# HEALTHCHECK — cómo verificar si el contenedor está sano
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1
```

---

## Multi-stage builds — la clave para imágenes de producción

Los multi-stage builds permiten usar una imagen pesada para compilar y una ligera para correr:

```dockerfile
# ==========================================
# Stage 1: restore — solo .csproj para cachear dependencias
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copiar SOLO los .csproj y el submódulo Common primero
# Docker cachea esta capa — si los .csproj no cambian, no re-ejecuta restore
COPY ["back-template/Host/Host.csproj",                           "back-template/Host/"]
COPY ["back-template/Domain/Domain.csproj",                       "back-template/Domain/"]
COPY ["back-template/Application/Application.csproj",             "back-template/Application/"]
COPY ["back-template/Infrastructure/Infrastructure.csproj",       "back-template/Infrastructure/"]
COPY ["back-template/WebApi/WebApi.csproj",                       "back-template/WebApi/"]
COPY ["back-template/Tests/Tests.csproj",                         "back-template/Tests/"]
COPY ["back-template/Common/",                                    "back-template/Common/"]

# dotnet restore descarga todas las dependencias NuGet
RUN dotnet restore "back-template/Host/Host.csproj"

# ==========================================
# Stage 2: build — compila el código
# ==========================================
FROM restore AS build
WORKDIR /src

# Ahora sí copia TODO el código fuente
COPY . .

ARG BUILD_CONFIGURATION=Release
RUN dotnet build "back-template/Host/Host.csproj" \
    -c ${BUILD_CONFIGURATION} \
    --no-restore \
    -o /app/build

# ==========================================
# Stage 3: publish — genera el output de producción
# ==========================================
FROM build AS publish

ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "back-template/Host/Host.csproj" \
    -c ${BUILD_CONFIGURATION} \
    --no-build \
    -o /app/publish \
    /p:UseAppHost=false   # ← no generar ejecutable nativo, solo IL

# ==========================================
# Stage 4: final — imagen de producción (pequeña y segura)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
# noble-chiseled = Ubuntu Noble distroless:
# - Sin shell (/bin/sh)
# - Sin package manager (apt)
# - Sin utilidades del sistema
# - Solo lo necesario para correr .NET
# - Imagen ~60 MB vs ~200 MB de la base normal

WORKDIR /app

# Correr como usuario no-root — el usuario "app" existe en la imagen base
USER app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Copiar SOLO el output de publish desde el stage anterior
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Host.dll"]
```

**Por qué multi-stage:**
- La imagen final solo tiene el `publish` output + runtime `.NET` (~100 MB)
- Sin SDK (hundreds of MB), sin código fuente, sin dependencias de build
- Sin shell ni utilidades → menor superficie de ataque
- Imagen de prod más pequeña = deploys más rápidos, menos costo de storage

---

## .dockerignore — excluir del contexto de build

El contexto de build es todo lo que Docker envía al daemon. `.dockerignore` es como `.gitignore` para Docker:

```dockerignore
# Directorios de build — no los necesita el Dockerfile
**/bin/
**/obj/

# Git
.git/
.gitmodules
**/.gitignore

# IDE
.vs/
.idea/
**/*.user
**/*.suo

# Documentación
docs/
*.md
!README.md

# Tests (opcional — si no los corres en el build)
**/Tests/

# Archivos de configuración local
**/appsettings.Local.json
**/.env
**/.env.*

# Docker
**/Dockerfile
**/.dockerignore
**/compose*.yaml

# Logs y temporales
**/logs/
**/*.log
**/tmp/

# Node.js (si hay frontend)
**/node_modules/
```

**Por qué importa:**
- Un `.dockerignore` sin `bin/` y `obj/` puede enviar GBs al daemon innecesariamente
- Cada `COPY . .` sin `.dockerignore` invalida el caché de capas con archivos irrelevantes
- Mejora la seguridad al no incluir archivos `.env` con secretos

---

## Build context — entender qué se envía

```bash
# El contexto es el directorio pasado al final del build command
docker build -t back-template:latest .
#                                     ↑ contexto = directorio actual

# Para este proyecto (desde la raíz del repo):
cd back-template   # directorio raíz que contiene compose.yaml y la carpeta back-template/
docker build -t back-template:latest -f back-template/Dockerfile .
#                                    ↑ path al Dockerfile           ↑ contexto

# Tamaño del contexto:
# Sin .dockerignore: potencialmente GB (incluye bin/, obj/, .git/)
# Con .dockerignore: solo el código fuente — MB
```

---

## ARG vs ENV

```dockerfile
# ARG — solo disponible durante el BUILD
ARG BUILD_CONFIGURATION=Release   # valor por defecto
ARG VERSION

# Pasar ARG en docker build:
# docker build --build-arg BUILD_CONFIGURATION=Debug --build-arg VERSION=1.2.3 .

RUN dotnet publish -c ${BUILD_CONFIGURATION}

# ENV — disponible durante el BUILD y en el CONTENEDOR en runtime
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Se puede sobreescribir al correr el contenedor:
# docker run -e ASPNETCORE_ENVIRONMENT=Development ...
```

---

## ENTRYPOINT vs CMD

```dockerfile
# ENTRYPOINT — el comando que siempre corre (no se sobreescribe con docker run args)
ENTRYPOINT ["dotnet", "Host.dll"]

# CMD — argumentos por defecto para ENTRYPOINT (se sobreescribe con docker run args)
CMD ["--environment", "Production"]

# Resultado: dotnet Host.dll --environment Production

# Sobreescribir CMD en docker run:
docker run back-template:latest --environment Development
# Resultado: dotnet Host.dll --environment Development

# Sobreescribir ENTRYPOINT (para debugging):
docker run --entrypoint /bin/bash back-template:latest
# ← falla en distroless (no hay /bin/bash) — solo en imágenes con shell
```

---

## Optimizaciones de caché

```dockerfile
# ❌ Sin optimización — cualquier cambio de código re-ejecuta restore (lento)
FROM sdk:10.0
COPY . .
RUN dotnet restore
RUN dotnet build

# ✓ Con optimización — restore cacheado mientras .csproj no cambie
FROM sdk:10.0
# Primero: copiar .csproj (cambia rara vez)
COPY ["Host/Host.csproj", "Host/"]
RUN dotnet restore
# Segundo: copiar código (cambia frecuentemente)
COPY . .
RUN dotnet build
```

---

## Imágenes base de .NET — cuándo usar cuál

| Imagen | Tamaño | Contiene | Usar para |
|--------|--------|----------|-----------|
| `sdk:10.0` | ~740 MB | SDK + runtime + herramientas de build | Build (stages 1-3) |
| `aspnet:10.0` | ~220 MB | Runtime + ASP.NET | Producción estándar |
| `aspnet:10.0-noble-chiseled` | ~110 MB | Runtime + ASP.NET, sin shell | Producción segura (este proyecto) |
| `runtime:10.0` | ~190 MB | Solo runtime .NET | Consolas, workers (sin web) |
| `runtime-deps:10.0` | ~120 MB | Solo dependencias nativas | Self-contained deployments |

---

## Debugging con Dockerfile

```dockerfile
# Para debug: usar imagen con shell (no distroless)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS debug
WORKDIR /app
COPY --from=publish /app/publish .

# Instalar herramientas de diagnóstico
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

# NO usar USER app en debug — necesitas acceso root a veces
EXPOSE 8080
ENTRYPOINT ["dotnet", "Host.dll"]
```

```bash
# Entrar al contenedor mientras corre (si tiene shell):
docker exec -it <container-id> /bin/bash
docker exec -it <container-id> /bin/sh

# Inspeccionar el sistema de archivos:
docker run --rm -it --entrypoint /bin/sh aspnet:10.0
```

---

## Seguridad en Dockerfiles
> Fuente: *Docker Up and Running* (Kane, Matthias) — Ch.11 Docker Security

```dockerfile
# ❌ Prácticas inseguras comunes

# 1. Correr como root (default si no se especifica USER)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENTRYPOINT ["dotnet", "app.dll"]
# Si el proceso es comprometido, tiene acceso root al contenedor

# 2. Copiar archivos de configuración con secretos
COPY appsettings.Production.json .   # ← secretos hardcoded en la imagen
COPY .env .                           # ← expone credenciales en docker history

# 3. Imagen enorme con herramientas innecesarias
FROM mcr.microsoft.com/dotnet/sdk:10.0  # ← SDK en producción = superficie de ataque enorme
```

```dockerfile
# ✓ Prácticas seguras

# 1. Usuario no-root
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
USER app   # ← usuario no-root incluido en la imagen base
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Host.dll"]

# 2. Secretos como variables de entorno en runtime (nunca en la imagen)
# En compose.yaml o en el orquestador (ECS, K8s):
# environment:
#   ConnectionStrings__MainDb: ${DB_CONNECTION_STRING}
#   Jwt__Key: ${JWT_KEY}

# 3. Imagen distroless (noble-chiseled) para producción
# - Sin shell → no se puede "entrar" al contenedor
# - Sin package manager → no se puede instalar herramientas
# - Solo lo necesario para .NET → superficie de ataque mínima

# 4. Imagen sin capas con secretos
# ❌ Aunque se sobreescriba, la capa anterior sigue en la imagen
RUN echo "$SECRET" > /tmp/secret && do-something && rm /tmp/secret
# ✓ Los secretos nunca deben llegar a una capa de la imagen
# Usar --secret en BuildKit para secretos durante el build:
RUN --mount=type=secret,id=nuget_token \
    NUGET_TOKEN=$(cat /run/secrets/nuget_token) dotnet restore
```

```bash
# Auditar una imagen con Docker Scout (o Trivy)
docker scout cves back-template:latest
docker scout recommendations back-template:latest

# Trivy (alternativa open source)
trivy image back-template:latest
```

---

## BuildKit — características avanzadas

BuildKit es el motor de build moderno de Docker (habilitado por defecto desde Docker 23.x).

```bash
# Habilitar BuildKit si no está por defecto
export DOCKER_BUILDKIT=1

# Build con BuildKit
docker build --progress=plain -t back-template:latest .
```

```dockerfile
# Syntax directive para usar BuildKit features
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Cache mount — compartir caché NuGet entre builds
RUN --mount=type=cache,target=/root/.nuget \
    dotnet restore "Host/Host.csproj"

# Secret mount — usar secreto sin dejarlo en la imagen
RUN --mount=type=secret,id=github_token \
    GITHUB_TOKEN=$(cat /run/secrets/github_token) \
    dotnet restore --source "https://nuget.pkg.github.com/org/index.json"
```

```bash
# Pasar secreto al build sin que quede en ninguna capa
docker build \
  --secret id=github_token,src=~/.github_token \
  -t back-template:latest .
```


---

*Rogelio Arriaga Gonzalez*
