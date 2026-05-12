# 05 — Comandos Docker y Docker Compose

Referencia rápida de los comandos más usados. `docker compose` (v2) sin guión es el comando actual.

---

## docker build

```bash
# Construir imagen desde el directorio actual (busca Dockerfile ahí)
docker build -t nombre:tag .

# Especificar Dockerfile y contexto
docker build -t back-template:latest -f back-template/Dockerfile .

# Pasar build args
docker build --build-arg BUILD_CONFIGURATION=Debug -t back-template:debug .

# Construir un stage específico (útil para debug)
docker build --target build -t back-template:build-stage .

# No usar caché (forzar rebuild completo)
docker build --no-cache -t back-template:latest .

# Ver el output del build en detalle
docker build --progress=plain -t back-template:latest .
```

---

## docker run

```bash
# Básico — corre en foreground, para con Ctrl+C
docker run nombre:tag

# En background (detached)
docker run -d nombre:tag

# Con nombre (para referenciarlo luego)
docker run -d --name mi-api nombre:tag

# Mapear puerto host:container
docker run -d -p 8080:8080 nombre:tag

# Variables de entorno
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Jwt__Key=mi-clave-secreta \
  nombre:tag

# Variables desde archivo
docker run -d --env-file .env nombre:tag

# Montar volumen
docker run -d -v postgres_data:/var/lib/postgresql/data postgres:17

# Bind mount (carpeta local → carpeta en contenedor)
docker run -d -v ./config:/app/config:ro nombre:tag

# Eliminar el contenedor automáticamente al parar
docker run --rm nombre:tag

# Sobreescribir el entrypoint (para debugging)
docker run --rm -it --entrypoint /bin/bash nombre:tag
# ← falla en imágenes distroless (sin shell)

# Limitar recursos
docker run -d --memory="512m" --cpus="0.5" nombre:tag
```

---

## docker ps — listar contenedores

```bash
# Contenedores en ejecución
docker ps

# Todos (incluye parados)
docker ps -a

# Solo IDs (útil para scripts)
docker ps -q

# Formato personalizado
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

---

## docker logs

```bash
# Ver logs de un contenedor
docker logs <container-id-o-nombre>

# Seguir logs en tiempo real (como tail -f)
docker logs -f <nombre>

# Últimas N líneas
docker logs --tail 100 <nombre>

# Con timestamps
docker logs -t <nombre>

# Combinar: últimas 50 líneas y seguir
docker logs --tail 50 -f <nombre>
```

---

## docker exec — ejecutar en contenedor activo

```bash
# Abrir shell interactivo (solo si tiene shell)
docker exec -it <nombre> /bin/bash
docker exec -it <nombre> /bin/sh

# Ejecutar un comando puntual
docker exec <nombre> ls /app

# Como usuario específico
docker exec -u root <nombre> /bin/bash
```

> Los contenedores con imagen `noble-chiseled` (este proyecto) no tienen shell. Para inspeccionar, usar `docker cp` o un stage `debug` sin distroless.

---

## docker stop / start / restart / rm

```bash
# Parar (envía SIGTERM, espera graceful shutdown, luego SIGKILL)
docker stop <nombre>

# Parar inmediatamente (SIGKILL)
docker kill <nombre>

# Parar con timeout personalizado (segundos)
docker stop -t 30 <nombre>

# Iniciar un contenedor parado
docker start <nombre>

# Reiniciar
docker restart <nombre>

# Eliminar contenedor parado
docker rm <nombre>

# Forzar eliminación (aunque esté corriendo)
docker rm -f <nombre>

# Eliminar todos los contenedores parados
docker container prune
```

---

## docker images

```bash
# Listar imágenes locales
docker images

# Filtrar por nombre
docker images back-template

# Solo IDs
docker images -q

# Eliminar imagen
docker rmi back-template:latest

# Forzar eliminación (aunque haya contenedores basados en ella)
docker rmi -f back-template:latest

# Eliminar imágenes sin tag (<none>:<none>)
docker image prune

# Eliminar todas las imágenes no usadas por ningún contenedor
docker image prune -a
```

---

## docker inspect

```bash
# Inspeccionar contenedor (JSON detallado)
docker inspect <nombre-o-id>

# Extraer un campo con --format
docker inspect --format='{{.State.Status}}' <nombre>
docker inspect --format='{{.NetworkSettings.IPAddress}}' <nombre>
docker inspect --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' <nombre>

# Inspeccionar imagen
docker inspect back-template:latest
```

---

## docker stats

```bash
# Uso de recursos en tiempo real (todos los contenedores)
docker stats

# Solo contenedores específicos
docker stats api postgres

# Una sola lectura (sin actualizar)
docker stats --no-stream

# Formato personalizado
docker stats --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"
```

---

## docker cp — copiar archivos

```bash
# Del contenedor al host
docker cp <nombre>:/app/logs/app.log ./logs/

# Del host al contenedor
docker cp ./config.json <nombre>:/app/config.json
```

---

## Volúmenes

```bash
docker volume ls                      # listar volúmenes
docker volume inspect postgres_data   # detalles del volumen
docker volume rm postgres_data        # eliminar (solo si no hay contenedor usándolo)
docker volume prune                   # eliminar todos los no usados
```

---

## Redes

```bash
docker network ls                     # listar redes
docker network inspect back-template_default  # detalles
docker network create mi-red          # crear red
docker network rm mi-red              # eliminar
```

---

## Limpieza del sistema

```bash
# Eliminar contenedores parados, redes sin uso, imágenes dangling, build cache
docker system prune

# Incluir también volúmenes sin uso (¡borra datos!)
docker system prune --volumes

# Sin confirmación (para scripts CI)
docker system prune -f

# Ver cuánto espacio usa Docker
docker system df

# Ver desglose detallado
docker system df -v
```

---

## docker compose — comandos principales

Todos estos comandos se corren desde el directorio donde está el archivo compose (o con `-f`).

### up / down

```bash
# Levantar todos los servicios (en background)
docker compose up -d

# Levantar y forzar rebuild de imágenes
docker compose up -d --build

# Forzar recrear contenedores aunque no haya cambios
docker compose up -d --force-recreate

# Especificar archivo compose
docker compose -f compose-db.yaml up -d
docker compose -f compose-dev.yaml up -d --build

# Levantar solo servicios específicos
docker compose up -d postgres seq

# Apagar y eliminar contenedores (volúmenes persisten)
docker compose down

# Apagar y eliminar contenedores + volúmenes nombrados
docker compose down -v

# Apagar con timeout (segundos para graceful shutdown)
docker compose down -t 30

# Apagar y eliminar imágenes construidas
docker compose down --rmi all
```

### ps / logs

```bash
# Estado de los servicios del compose
docker compose ps
docker compose -f compose-db.yaml ps

# Logs de todos los servicios
docker compose logs

# Seguir logs en tiempo real
docker compose logs -f

# Logs de un servicio específico
docker compose logs -f api
docker compose logs --tail 100 postgres

# Con timestamps
docker compose logs -t api
```

### exec / run

```bash
# Ejecutar en un servicio en ejecución
docker compose exec api /bin/bash
docker compose exec postgres psql -U postgres -d back_template_dev

# Correr un contenedor one-off (nuevo contenedor temporal)
docker compose run --rm api dotnet --info
docker compose run --rm -e ASPNETCORE_ENVIRONMENT=Staging api
```

### build / pull

```bash
# Solo construir imágenes (sin levantar)
docker compose build
docker compose -f compose-dev.yaml build api

# Sin caché
docker compose build --no-cache

# Descargar imágenes actualizadas
docker compose pull
docker compose -f compose-dev.yaml pull postgres
```

### stop / start / restart

```bash
# Parar servicios (sin eliminar contenedores)
docker compose stop
docker compose stop api

# Iniciar servicios parados
docker compose start

# Reiniciar
docker compose restart
docker compose restart api
```

### config

```bash
# Ver la configuración final (con variables resueltas)
docker compose config
docker compose -f compose.yaml config

# Validar sintaxis del compose
docker compose config --quiet
echo $?   # 0 = válido
```

### top / port

```bash
# Ver procesos corriendo en cada servicio
docker compose top

# Ver el puerto del host mapeado a un puerto del contenedor
docker compose port api 8080
```

---

## Patrones de uso frecuente

### Reset completo del entorno dev

```bash
docker compose -f compose-db.yaml down -v
rm -rf ./data/
docker compose -f compose-db.yaml up -d
```

### Reconstruir solo la API

```bash
docker compose -f compose-dev.yaml up -d --build --no-deps api
```

`--no-deps` = no reconstruir las dependencias (postgres, seq), solo `api`.

### Conectar a PostgreSQL interactivo

```bash
docker compose -f compose-db.yaml exec postgres psql -U postgres -d back_template_dev
```

O con un cliente externo como DBeaver: `localhost:5432`, user `postgres`, password `postgres`.

### Ver queries lentos en Seq

Abrir `http://localhost:5341` → filter: `@Properties['QueryTime'] > 500`

### Inspeccionar la imagen final

```bash
# Ver todas las capas
docker history back-template:latest

# Usar dive para análisis visual (herramienta externa)
dive back-template:latest
```

### Correr tests dentro de Docker

```bash
docker compose run --rm api dotnet test Tests/Tests.csproj
```

### Ver variables de entorno de un servicio en ejecución

```bash
docker compose exec api printenv
# ← falla en imágenes distroless
docker inspect back-template-api-1 --format='{{range .Config.Env}}{{.}}{{"\n"}}{{end}}'
```

---

## Flujo completo: build → test → run (CI)

```bash
# 1. Build
docker build -t back-template:${GIT_SHA} -f back-template/Dockerfile .

# 2. Tests (opcional — si corres tests dentro de Docker)
docker run --rm back-template:${GIT_SHA} dotnet test Tests/Tests.csproj

# 3. Tag
docker tag back-template:${GIT_SHA} back-template:latest

# 4. Push a registry
docker push ghcr.io/org/back-template:${GIT_SHA}
docker push ghcr.io/org/back-template:latest

# 5. Deploy (en el servidor)
docker compose pull
docker compose up -d --force-recreate
```


---

*Rogelio Arriaga Gonzalez*
