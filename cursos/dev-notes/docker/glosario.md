# Glosario Técnico  -  Docker

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### Alpine
**Categoría:** Docker / Imágenes base
**Definición:** Distribución Linux minimalista (~5 MB) utilizada como imagen base en Docker. Ofrece el gestor de paquetes `apk` y es ideal para producción por su tamaño reducido. Se usa comúnmente como sufijo en tags de imágenes oficiales: `postgres:17-alpine`, `nginx:1.27-alpine`, `node:22-alpine`.
**En el proyecto:** La imagen de PostgreSQL y Nginx del sistema de mensajería usan variantes Alpine para mantener el tamaño de las imágenes al mínimo en el entorno de desarrollo y staging.

### Alpine (node:22-alpine)
**Categoría:** Docker / Imágenes base / Frontend
**Definición:** Imagen oficial de Node.js basada en Alpine Linux. Incluye Node.js y npm en una capa base ligera. Se utiliza en el stage de build de aplicaciones frontend para compilar el bundle sin llevar toda esa cadena de herramientas a la imagen final.
**En el proyecto:** Primer stage del Dockerfile del frontend (React + Vite) del sistema de mensajería. Instala dependencias y ejecuta `npm run build` para producir el directorio `dist/`.

### ARG
**Categoría:** Docker / Dockerfile
**Definición:** Instrucción de Dockerfile que declara una variable disponible únicamente durante el proceso de build (`docker build`). A diferencia de `ENV`, un `ARG` no persiste en la imagen final ni en el contenedor en runtime. Se puede sobreescribir pasando `--build-arg NOMBRE=valor` en el comando de build.
**En el proyecto:** Se usa `ARG BUILD_CONFIGURATION=Release` para controlar si el back-end del CRM compila en modo `Release` o `Debug`, y `ARG VITE_API_URL` para inyectar la URL de la API al front-end en tiempo de compilación.

### ASPNETCORE_ENVIRONMENT
**Categoría:** Docker / .NET / Variables de entorno
**Definición:** Variable de entorno de ASP.NET Core que determina el perfil de configuración activo: `Development`, `Staging` o `Production`. En `Production` se deshabilita la página de excepciones detalladas y se activan optimizaciones. Se establece vía `ENV` en el Dockerfile o en el bloque `environment` de Compose.
**En el proyecto:** La API del sistema de mensajería debe correr en `Production` en los contenedores de staging y producción. Se sobreescribe a `Development` o `Staging` localmente vía `docker run -e`.

### ASPNETCORE_HTTP_PORTS
**Categoría:** Docker / .NET / Variables de entorno
**Definición:** Variable de entorno introducida en .NET 8+ que configura en qué puerto(s) escucha el servidor Kestrel de ASP.NET Core dentro del contenedor. Reemplaza la variable `ASPNETCORE_URLS` para la configuración de puertos HTTP. El valor típico es `8080`.
**En el proyecto:** La imagen final del sistema de mensajería API expone el puerto `8080` y la variable `ASPNETCORE_HTTP_PORTS=8080` asegura que Kestrel escuche exactamente en ese puerto.

---

## B

### Bind mount
**Categoría:** Docker / Volumes
**Definición:** Tipo de volumen en Docker donde una carpeta o archivo del sistema de archivos del host se monta directamente dentro del contenedor. A diferencia de los named volumes, el path en el host es controlado por el usuario. Es útil para inyectar configuración, scripts de inicialización o habilitar hot-reload en desarrollo.
**En el proyecto:** Se usa bind mount para montar `./init-scripts` en `/docker-entrypoint-initdb.d` de PostgreSQL (scripts SQL de inicialización) y para montar `appsettings.json` como read-only en la API.

### Bridge (driver de red)
**Categoría:** Docker / Redes
**Definición:** Driver de red por defecto en Docker. Crea una red virtual interna en el host donde los contenedores pueden comunicarse entre sí por nombre. Los contenedores en redes bridge distintas están aislados entre sí a menos que se conecten explícitamente a ambas redes.
**En el proyecto:** Las redes `frontend` y `backend` del sistema de mensajería usan el driver `bridge`. La red `backend` es además `internal: true`, bloqueando el acceso a internet desde la DB.

### Build arg (`--build-arg`)
**Categoría:** Docker / CLI
**Definición:** Flag del comando `docker build` que permite pasar valores para las variables `ARG` declaradas en el Dockerfile. Solo afecta al proceso de build; el valor no queda expuesto en la imagen final.
**En el proyecto:** Se pasa `--build-arg BUILD_CONFIGURATION=Debug` para compilar la API en modo debug durante desarrollo, y `VITE_API_URL` al construir el front-end.

### Build cache (caché de capas)
**Categoría:** Docker / Optimización
**Definición:** Mecanismo por el cual Docker reutiliza capas previamente construidas si el Dockerfile y los archivos de entrada no han cambiado. Cada instrucción del Dockerfile genera una capa; si esa instrucción y sus dependencias no cambiaron desde la última vez, Docker usa la capa del caché en lugar de re-ejecutar el comando.
**En el proyecto:** La estrategia de copiar primero los archivos `.csproj` y ejecutar `dotnet restore` antes de copiar el código fuente maximiza el uso de caché: si no se agregaron paquetes NuGet, el paso de restore (~60 segundos) se saltea en cada build.

### Build context (contexto de build)
**Categoría:** Docker / Build
**Definición:** Conjunto de archivos y directorios enviados al Docker daemon cuando se ejecuta `docker build`. Por defecto es el directorio actual (`.`). El daemon solo puede acceder a archivos dentro del contexto; todo lo excluido en `.dockerignore` no se envía. Un contexto grande ralentiza el build y puede invalidar el caché.
**En el proyecto:** El contexto del sistema de mensajería es la raíz del repositorio. Gracias al `.dockerignore`, se excluyen `bin/`, `obj/` y `node_modules/` para que el contexto no crezca innecesariamente.

---

## C

### CMD
**Categoría:** Docker / Dockerfile
**Definición:** Instrucción de Dockerfile que define los argumentos por defecto para `ENTRYPOINT`, o el comando a ejecutar si no hay `ENTRYPOINT`. A diferencia de `ENTRYPOINT`, `CMD` puede sobreescribirse completamente pasando argumentos al final de `docker run`. Puede escribirse en forma exec `["cmd", "arg"]` o en forma shell `cmd arg`.
**En el proyecto:** En el Dockerfile del front-end React, `CMD ["nginx", "-g", "daemon off;"]` arranca Nginx en primer plano (modo no-daemonizado), requerido para que Docker pueda monitorear el proceso.

### Compose file (`compose.yaml`)
**Categoría:** Docker Compose
**Definición:** Archivo YAML que define la configuración completa de una aplicación multi-contenedor: servicios, redes, volúmenes y variables de entorno. El nombre canónico moderno es `compose.yaml` (Docker Compose v2); el nombre legacy es `docker-compose.yml`. Se procesa con el subcomando `docker compose`.
**En el proyecto:** El `compose.yaml` del sistema de mensajería orquesta los tres servicios principales: `api` (.NET), `web` (React/Nginx) y `postgres`.

### Condition (`service_healthy` / `service_started`)
**Categoría:** Docker Compose / Dependencias
**Definición:** Parámetro de `depends_on` que especifica el estado que debe tener el servicio dependencia antes de arrancar el servicio dependiente. `service_started` solo espera que el contenedor haya iniciado su proceso. `service_healthy` espera además que el `healthcheck` reporte estado `healthy`.
**En el proyecto:** La API del CRM usa `condition: service_healthy` para PostgreSQL, garantizando que la base de datos esté lista para aceptar conexiones antes de que la API intente conectarse.

### COPY
**Categoría:** Docker / Dockerfile
**Definición:** Instrucción de Dockerfile que copia archivos o directorios desde el contexto de build (o desde otra stage con `--from`) al sistema de archivos de la imagen. A diferencia de `ADD`, `COPY` es predecible: no descomprime automáticamente ni acepta URLs remotas. Es la instrucción preferida para copiar código fuente.
**En el proyecto:** `COPY ["Host/Host.csproj", "Host/"]` copia primero solo los archivos de proyecto para optimizar caché, y luego `COPY . .` copia el código fuente completo.

---

## D

### daemon (Docker daemon)
**Categoría:** Docker / Arquitectura
**Definición:** Proceso principal de Docker (`dockerd`) que corre en el host y gestiona imágenes, contenedores, redes y volúmenes. El CLI de Docker se comunica con el daemon a través de una API REST (por socket Unix o TCP). El build context se envía al daemon, no al CLI.
**En el proyecto:** Cuando el equipo ejecuta `docker build` o `docker compose up`, el CLI envía las instrucciones al daemon local que realmente construye y orquesta los contenedores del sistema de mensajería.

### daemon off (Nginx)
**Categoría:** Docker / Nginx
**Definición:** Flag de Nginx (`-g "daemon off;"`) que impide que el proceso se ejecute como un demonio en segundo plano. En Docker, el proceso principal del contenedor (PID 1) debe permanecer en primer plano; si Nginx se daemoniza y el proceso principal termina, Docker detiene el contenedor.
**En el proyecto:** El CMD del Dockerfile del front-end usa `daemon off;` para que Nginx sea el proceso en primer plano del contenedor y Docker pueda gestionarlo correctamente.

### dangling image
**Categoría:** Docker / Gestión de imágenes
**Definición:** Imagen sin etiqueta (tag) que queda como resultado de reconstruir una imagen con el mismo nombre: la imagen anterior pierde su tag y aparece como `<none>` en `docker images`. Ocupan espacio en disco sin ser referenciadas por ningún contenedor activo.
**En el proyecto:** Después de múltiples builds de la API o el front-end del CRM, se acumulan imágenes dangling. Se limpian con `docker image prune` o `docker system prune`.

### depends_on
**Categoría:** Docker Compose
**Definición:** Directiva de Compose que establece un orden de arranque entre servicios. Sin la clave `condition`, solo controla el orden de inicio del contenedor, no garantiza que el servicio esté listo para aceptar conexiones. Con `condition: service_healthy`, espera a que el healthcheck del servicio dependencia sea exitoso.
**En el proyecto:** `api` depende de `postgres` con `condition: service_healthy` y de `seq` (logging) con `condition: service_started`.

### distroless
**Categoría:** Docker / Seguridad / Imágenes base
**Definición:** Familia de imágenes base que contienen únicamente el runtime de la aplicación y sus dependencias mínimas, sin shell, sin gestor de paquetes (`apt`/`apk`), sin utilidades de sistema. Reduce drásticamente la superficie de ataque porque un atacante que comprometa el contenedor no tiene herramientas disponibles. Google y Microsoft ofrecen variantes distroless.
**En el proyecto:** La imagen base `aspnet:10.0-noble-chiseled` es la variante distroless de Microsoft para ASP.NET Core 10 sobre Ubuntu Noble. Mide ~110 MB frente a los ~740 MB del SDK completo.

### .dockerignore
**Categoría:** Docker / Build
**Definición:** Archivo de texto (análogo a `.gitignore`) que lista patrones de archivos y directorios a excluir del contexto de build enviado al daemon. Reducir el contexto acelera el build, evita invalidaciones de caché por archivos irrelevantes y previene que secretos o artefactos de compilación queden en la imagen.
**En el proyecto:** El `.dockerignore` del sistema de mensajería excluye `bin/`, `obj/`, `node_modules/`, `.git/`, archivos de IDE y archivos de secretos como `.env` y `appsettings.Local.json`.

### docker build
**Categoría:** Docker / CLI
**Definición:** Comando que construye una imagen Docker a partir de un Dockerfile y un contexto de build. Lee el Dockerfile instrucción por instrucción, ejecutando cada una en una capa separada. Acepta flags como `-t` (tag), `-f` (Dockerfile alternativo), `--build-arg`, `--no-cache` y `--progress`.
**En el proyecto:** `docker build -t mensajeria-api:v1.0.0 -f back-end/Dockerfile .` construye la imagen de la API; en CI/CD se usa el SHA del commit como tag.

### docker compose
**Categoría:** Docker Compose / CLI
**Definición:** Subcomando de Docker CLI (v2) que gestiona aplicaciones multi-contenedor definidas en un `compose.yaml`. Reemplaza el binario independiente `docker-compose` (v1). Los comandos más usados son `up`, `down`, `ps`, `logs`, `exec` y `pull`.
**En el proyecto:** El stack completo del sistema de mensajería (API + web + PostgreSQL) se levanta con un único `docker compose up -d` desde la raíz del repositorio.

### docker exec
**Categoría:** Docker / CLI
**Definición:** Comando que ejecuta un proceso adicional dentro de un contenedor ya en ejecución, sin reiniciar el contenedor. Con `-it` abre una sesión interactiva (shell). Solo funciona si el contenedor tiene el binario solicitado (no disponible en imágenes distroless).
**En el proyecto:** `docker compose exec postgres psql -U app_user -d crm` abre una sesión de psql directamente en el contenedor de PostgreSQL del sistema de mensajería.

### docker images
**Categoría:** Docker / CLI
**Definición:** Comando que lista todas las imágenes descargadas o construidas localmente, mostrando repositorio, tag, ID, fecha de creación y tamaño. `docker image prune` elimina las imágenes dangling (sin tag).
**En el proyecto:** Se usa para verificar qué versiones de la imagen del CRM están disponibles localmente y para diagnosticar si un build generó imágenes dangling.

### docker logs
**Categoría:** Docker / CLI / Observabilidad
**Definición:** Comando que muestra la salida estándar (stdout) y el error estándar (stderr) de un contenedor. Con `-f` sigue el stream en tiempo real (equivalente a `tail -f`). Con `--tail N` limita a las últimas N líneas.
**En el proyecto:** `docker compose logs -f api` muestra los logs de Serilog de la API del CRM en tiempo real durante desarrollo o debugging.

### docker ps
**Categoría:** Docker / CLI
**Definición:** Comando que lista los contenedores en ejecución. Con `-a` muestra también los detenidos. Muestra nombre, imagen, estado, puertos y tiempo de ejecución. El flag `--format` permite personalizar las columnas de salida.
**En el proyecto:** `docker compose ps` muestra el estado de todos los servicios del sistema de mensajería (api, web, postgres) indicando si están `healthy`, `starting` o `exited`.

### docker pull
**Categoría:** Docker / CLI / Registry
**Definición:** Comando que descarga una imagen desde un registry (Docker Hub, GHCR, ECR, etc.) al host local. En `docker compose up`, si la imagen no existe localmente y está definida con `image:`, se hace pull automático.
**En el proyecto:** En el flujo de deploy, el servidor de producción ejecuta `docker compose pull` para descargar las nuevas versiones de la API y el web del CRM antes de recrear los contenedores.

### docker push
**Categoría:** Docker / CLI / Registry
**Definición:** Comando que sube una imagen local a un registry remoto. La imagen debe estar tageada con el nombre completo del registry (`ghcr.io/org/imagen:tag`). Requiere autenticación previa con `docker login`.
**En el proyecto:** El pipeline de CI/CD hace `docker push` de la imagen con el SHA del commit y con el tag SemVer al GHCR de la empresa después de un build exitoso.

### docker rm / docker rmi
**Categoría:** Docker / CLI / Limpieza
**Definición:** `docker rm` elimina uno o más contenedores detenidos. `docker rmi` elimina imágenes locales. Ambos fallan si el recurso está en uso. El flag `-f` fuerza la eliminación (detiene el contenedor antes de borrarlo en el caso de `docker rm -f`).
**En el proyecto:** Durante desarrollo, `docker rm -f crm-api` elimina el contenedor de la API para recrearlo con una nueva configuración sin necesidad de usar Compose.

### docker run
**Categoría:** Docker / CLI
**Definición:** Comando que crea y arranca un contenedor a partir de una imagen. Combina `docker create` y `docker start`. Los flags más usados son `-d` (background), `--name`, `-p` (puertos), `-e` (variables de entorno), `-v` (volúmenes), `--rm` (autoeliminar al terminar) y `--env-file`.
**En el proyecto:** En pruebas puntuales, `docker run --rm -e Jwt__Key=test mensajeria-api:v1.0.0` ejecuta la API del CRM de forma efímera sin dejar rastro de contenedor.

### docker stop
**Categoría:** Docker / CLI
**Definición:** Comando que detiene un contenedor en ejecución enviando primero `SIGTERM` (señal de terminación graceful) al proceso principal. Si el proceso no termina en el tiempo de gracia (10 segundos por defecto), envía `SIGKILL`. La diferencia con `docker kill` es que este último envía `SIGKILL` directamente.
**En el proyecto:** En el ciclo de deploy del sistema de mensajería, `docker compose down` llama a `docker stop` en todos los contenedores, dando a la API tiempo de cerrar conexiones activas de agentes.

### docker system prune
**Categoría:** Docker / CLI / Limpieza
**Definición:** Comando que elimina todos los recursos de Docker no utilizados: contenedores detenidos, redes no referenciadas, imágenes dangling y caché de build. Con `--volumes` también elimina volúmenes sin uso (destruye datos). `docker system df` muestra cuánto espacio ocupa Docker.
**En el proyecto:** Útil para liberar espacio en disco en las máquinas de desarrollo del equipo después de múltiples builds del sistema de mensajería.

### docker tag
**Categoría:** Docker / CLI / Registry
**Definición:** Comando que crea un alias (tag adicional) para una imagen existente. No copia la imagen; ambos tags apuntan al mismo ID. Se usa para agregar un tag de versión SemVer a una imagen ya tageada con el SHA del commit.
**En el proyecto:** En el flujo CI/CD: primero se construye con el SHA (`crm-api:abc1234`), luego `docker tag` agrega el tag de release (`crm-api:v1.2.0`) apuntando a la misma imagen.

---

## E

### ECR (Elastic Container Registry)
**Categoría:** Docker / Registry / AWS
**Definición:** Servicio de registro de imágenes Docker de Amazon Web Services. Integrado con IAM para control de acceso y con ECS/EKS para despliegue. Se accede con el patrón `<account-id>.dkr.ecr.<region>.amazonaws.com/<repo>:<tag>`.
**En el proyecto:** Alternativa a GHCR para alojar las imágenes de la API y el front-end del sistema de mensajería en la infraestructura AWS de la empresa.

### ENTRYPOINT
**Categoría:** Docker / Dockerfile
**Definición:** Instrucción de Dockerfile que define el comando principal del contenedor. A diferencia de `CMD`, no se sobreescribe fácilmente con argumentos en `docker run` (requiere el flag `--entrypoint`). Cuando coexiste con `CMD`, `CMD` actúa como los argumentos por defecto de `ENTRYPOINT`. Se prefiere la forma exec `["cmd", "arg"]` sobre la forma shell.
**En el proyecto:** `ENTRYPOINT ["dotnet", "Host.dll"]` en el stage `final` garantiza que la API del CRM siempre arranque el proceso .NET correcto, independientemente de los argumentos que pase Compose.

### ENV
**Categoría:** Docker / Dockerfile
**Definición:** Instrucción de Dockerfile que establece variables de entorno disponibles tanto durante el build como en el contenedor en runtime. A diferencia de `ARG`, los valores de `ENV` persisten en la imagen. Se pueden sobreescribir en `docker run` con `-e` o en Compose con `environment:`.
**En el proyecto:** `ENV ASPNETCORE_HTTP_PORTS=8080` define el puerto en la imagen; `ENV ASPNETCORE_ENVIRONMENT=Production` fija el perfil de ASP.NET Core, ambos sobreescribibles por Compose en distintos entornos.

### env_file
**Categoría:** Docker Compose
**Definición:** Directiva de Compose que carga variables de entorno desde uno o más archivos `.env` en el contenedor. Diferente al `.env` que Compose lee para interpolar variables en el `compose.yaml`: `env_file` inyecta variables directamente al contenedor. Acepta múltiples archivos; los posteriores sobreescriben a los anteriores.
**En el proyecto:** Se puede usar `env_file: [.env, .env.production]` en el servicio `api` para separar la configuración base de la específica de producción.

### environment (Compose)
**Categoría:** Docker Compose
**Definición:** Directiva de Compose que define variables de entorno para el contenedor. Acepta sintaxis de lista (`- CLAVE=valor`) o de mapa (`CLAVE: valor`). Puede referenciar variables del host con `${VARIABLE}`, que Compose interpola desde el archivo `.env` del proyecto.
**En el proyecto:** La cadena de conexión de PostgreSQL, la clave JWT y el entorno de ASP.NET Core se pasan a la API del sistema de mensajería vía `environment:`, sin hardcodear secretos en el compose.yaml.

### EXPOSE
**Categoría:** Docker / Dockerfile
**Definición:** Instrucción de Dockerfile que documenta qué puerto(s) usa el contenedor, sin publicarlos en el host. Es informativa: indica a quien lee el Dockerfile qué puerto expone el servicio. La publicación real se hace con `-p` en `docker run` o con `ports:` en Compose.
**En el proyecto:** `EXPOSE 8080` en el Dockerfile de la API y `EXPOSE 80` en el del front-end documentan los puertos de Kestrel y Nginx respectivamente.

---

## F

### `--force-recreate` (Compose)
**Categoría:** Docker Compose / Deploy
**Definición:** Flag de `docker compose up` que fuerza la recreación de todos los contenedores aunque su configuración no haya cambiado. Útil en deploys donde la imagen fue actualizada en el registry pero el compose.yaml no cambió.
**En el proyecto:** El paso final del flujo CI/CD ejecuta `docker compose up -d --force-recreate` para garantizar que los contenedores del CRM se reemplacen con las imágenes recién descargadas.

### FROM
**Categoría:** Docker / Dockerfile
**Definición:** Primera instrucción de cualquier Dockerfile; especifica la imagen base sobre la que se construirá la nueva imagen. En Dockerfiles multi-stage, aparece múltiples veces con la cláusula `AS nombre` para nombrar cada stage. Se puede referenciar la imagen en un stage posterior con `COPY --from=nombre`.
**En el proyecto:** El Dockerfile del sistema de mensajería usa `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore` para el stage de compilación y `FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final` para producción.

---

## G

### GIT_SHA (como tag de imagen)
**Categoría:** Docker / CI/CD / Trazabilidad
**Definición:** Práctica de usar el hash del commit de Git (completo o corto) como tag de la imagen Docker. Garantiza que cada imagen sea trazable a un commit exacto del repositorio. Se obtiene con `git rev-parse --short HEAD` en scripts de CI. Es reproducible: dado el mismo SHA siempre se puede saber exactamente qué código contiene la imagen.
**En el proyecto:** En CI/CD del sistema de mensajería se construye con `mensajeria-api:${GIT_SHA}` y opcionalmente se agrega el tag SemVer en releases. El compose de producción usa el tag fijo, nunca `:latest`.

### GHCR (GitHub Container Registry)
**Categoría:** Docker / Registry
**Definición:** Registro de imágenes Docker de GitHub, accesible en `ghcr.io`. Integrado con GitHub Actions y permisos de repositorio. Permite publicar imágenes públicas o privadas asociadas a una organización o usuario de GitHub.
**En el proyecto:** Las imágenes del sistema de mensajería se publican en `ghcr.io/acme/mensajeria-api` y `ghcr.io/acme/mensajeria-web` como parte del pipeline de CI/CD en GitHub Actions.

### graceful shutdown
**Categoría:** Docker / Señales de proceso
**Definición:** Proceso de terminación controlada de un contenedor: Docker envía `SIGTERM` al proceso principal, dándole tiempo para completar solicitudes en vuelo, cerrar conexiones de base de datos y limpiar recursos antes de terminar. Si el proceso no responde en el tiempo de gracia, Docker envía `SIGKILL`.
**En el proyecto:** Usar la forma exec en `ENTRYPOINT ["dotnet", "Host.dll"]` garantiza que el proceso .NET recibe directamente `SIGTERM`, permitiéndole ejecutar `IHostedService.StopAsync` y cerrar conexiones de agentes del sistema de mensajería limpiamente.

---

## H

### HEALTHCHECK
**Categoría:** Docker / Dockerfile / Confiabilidad
**Definición:** Instrucción de Dockerfile que define cómo Docker verifica si el contenedor está funcionando correctamente. Ejecuta un comando periódicamente; si falla repetidamente, el contenedor pasa a estado `unhealthy`. Los parámetros son `--interval`, `--timeout`, `--retries` y `--start_period`.
**En el proyecto:** PostgreSQL usa `pg_isready -U app_user -d crm` como healthcheck para que el servicio `api` no intente conectarse antes de que la base de datos esté lista para aceptar conexiones.

### hostname (en redes Docker)
**Categoría:** Docker / Redes
**Definición:** Dentro de una red Docker, cada servicio es accesible por otros contenedores en la misma red usando el nombre del servicio como hostname DNS. Esto elimina la necesidad de conocer IPs dinámicas de contenedores.
**En el proyecto:** La cadena de conexión de la API del CRM usa `Host=postgres` como hostname; Docker resuelve `postgres` al contenedor de PostgreSQL en la red `backend`.

---

## I

### IMAGE_TAG (variable de entorno de Compose)
**Categoría:** Docker Compose / CI/CD
**Definición:** Convención de variable de entorno usada en el `compose.yaml` para parametrizar el tag de la imagen a usar. Se define en el archivo `.env` del proyecto y permite cambiar la versión desplegada sin modificar el compose.yaml. Con la sintaxis `${IMAGE_TAG:-dev}` se provee un valor por defecto.
**En el proyecto:** `image: mensajeria-api:${IMAGE_TAG:-dev}` en el compose.yaml permite que en desarrollo use la imagen local `:dev` y en producción use el tag de release específico.

### imagen (Docker image)
**Categoría:** Docker / Conceptos fundamentales
**Definición:** Plantilla de solo lectura formada por capas inmutables apiladas, que contiene el sistema de archivos, dependencias y configuración necesaria para ejecutar una aplicación. Es el equivalente a una "clase" en programación orientada a objetos: la plantilla de la que se instancian contenedores. Una imagen puede tener múltiples tags.
**En el proyecto:** `mensajeria-api:v1.0.0` y `mensajeria-web:v1.0.0` son las imágenes de producción del sistema de mensajería construidas con `docker build`.

### `internal: true` (red Compose)
**Categoría:** Docker Compose / Seguridad / Redes
**Definición:** Opción de red en Compose que bloquea el acceso a internet desde los contenedores en esa red. Los contenedores solo pueden comunicarse entre sí dentro de la red interna, sin salida hacia el exterior.
**En el proyecto:** La red `backend` del sistema de mensajería es `internal: true`: PostgreSQL y los servicios internos no tienen acceso a internet, reduciendo la superficie de ataque.

---

## K

### Kestrel
**Categoría:** .NET / HTTP
**Definición:** Servidor web HTTP multiplataforma y de alto rendimiento incluido en ASP.NET Core. En contenedores Docker, Kestrel escucha directamente en el puerto configurado (por defecto `8080` vía `ASPNETCORE_HTTP_PORTS`). En producción detrás de Nginx o un load balancer actúa como servidor de aplicación.
**En el proyecto:** La API del sistema de mensajería corre Kestrel en el puerto `8080` dentro del contenedor. Nginx (en el contenedor `web`) hace proxy reverso hacia `mensajeria-api:8080`.

---

## L

### layer (capa de imagen)
**Categoría:** Docker / Arquitectura
**Definición:** Cada instrucción en un Dockerfile que modifica el sistema de archivos genera una capa inmutable que se apila sobre las anteriores. Las capas son compartidas entre imágenes que tienen el mismo origen, ahorrando espacio en disco y tiempo de descarga. La capa superior de un contenedor en ejecución es la única que puede escribirse (read-write layer).
**En el proyecto:** El Dockerfile del sistema de mensajería está ordenado de menor a mayor frecuencia de cambio para maximizar el reuso de capas en caché: OS base -> SDK -> archivos `.csproj` -> `dotnet restore` -> código fuente -> `dotnet publish`.

### `:latest` (tag)
**Categoría:** Docker / Antipatrón
**Definición:** Tag especial que Docker asigna por defecto cuando no se especifica ningún tag. No es una referencia fija: puede apuntar a diferentes versiones a lo largo del tiempo. En producción es un antipatrón porque impide saber exactamente qué versión está corriendo, hace imposible rollbacks reproducibles y puede romper producción en un `docker pull` automático.
**En el proyecto:** Regla del proyecto: NUNCA usar `:latest` en el compose.yaml de producción del sistema de mensajería. Siempre usar `:v1.x.x` o `:${GIT_SHA}`.

---

## M

### MCR (Microsoft Container Registry)
**Categoría:** Docker / Registry
**Definición:** Registro oficial de imágenes de Microsoft, accesible en `mcr.microsoft.com`. Es la fuente oficial de imágenes de .NET, ASP.NET Core, SQL Server y otras tecnologías Microsoft. Todas las imágenes base del back-end del CRM provienen de MCR.
**En el proyecto:** `mcr.microsoft.com/dotnet/sdk:10.0` (stage de build) y `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` (stage final) son las imágenes base del Dockerfile de la API del sistema de mensajería.

### multi-stage build
**Categoría:** Docker / Dockerfile / Optimización
**Definición:** Técnica de Dockerfile que usa múltiples instrucciones `FROM`, cada una iniciando un stage independiente. Los artefactos de un stage se copian al siguiente con `COPY --from=stage`. El resultado final es solo el stage `final`, que no incluye las herramientas de build intermedias. Reduce drásticamente el tamaño de la imagen de producción y la superficie de ataque.
**En el proyecto:** El Dockerfile del sistema de mensajería tiene cuatro stages: `restore` (cachear NuGet), `build` (compilar), `publish` (generar output de producción) y `final` (imagen distroless con solo el runtime y el binario publicado).

---

## N

### named volume
**Categoría:** Docker / Volumes
**Definición:** Tipo de volumen creado y gestionado por Docker, identificado por un nombre. Docker decide dónde almacena los datos en el host (típicamente en `/var/lib/docker/volumes/`). Sobrevive a `docker compose down` pero se elimina con `docker compose down -v`. Es la opción recomendada para datos de bases de datos en producción.
**En el proyecto:** `postgres_data` es el named volume que persiste los datos de PostgreSQL del sistema de mensajería entre reinicios de contenedor.

### networks (Compose)
**Categoría:** Docker Compose / Redes
**Definición:** Sección del `compose.yaml` que define redes virtuales personalizadas para los servicios. Permite segmentar la comunicación: servicios en la misma red se ven entre sí; en redes distintas, no. Cada servicio declara a qué redes pertenece en su sección `networks:`.
**En el proyecto:** El sistema de mensajería define dos redes: `frontend` (api y web) y `backend` (api y postgres, `internal: true`). PostgreSQL no es accesible desde el contenedor de Nginx.

### nginx.conf
**Categoría:** Docker / Nginx / Frontend
**Definición:** Archivo de configuración de Nginx que define el comportamiento del servidor web: puertos de escucha, rutas, proxies y caché. En aplicaciones SPA (Single Page Application), se configura `try_files` para redirigir todas las rutas al `index.html`, permitiendo que React Router maneje el enrutamiento del lado del cliente.
**En el proyecto:** El `nginx.conf` del sistema de mensajería configura el fallback SPA, el proxy reverso hacia la API (evitando CORS en producción) y caché de un año para los assets compilados por Vite (que llevan hashes únicos en su nombre).

### `--no-cache` (docker build)
**Categoría:** Docker / CLI
**Definición:** Flag de `docker build` que deshabilita el uso del caché de capas, forzando la re-ejecución de todas las instrucciones del Dockerfile. Útil cuando se sospecha que el caché está corrupto o cuando se quiere asegurar una build completamente limpia.
**En el proyecto:** Se usa ocasionalmente en CI/CD del sistema de mensajería cuando se actualiza la imagen base y se quiere forzar el pull de la nueva versión de `aspnet:10.0-noble-chiseled`.

### `noble-chiseled`
**Categoría:** Docker / Imágenes base / Microsoft
**Definición:** Variante de imagen base de Microsoft para ASP.NET Core construida sobre Ubuntu Noble (24.04) en modo "chiseled" (distroless). No contiene shell, no contiene `apt`, corre como usuario no-root por defecto. Pesa ~110 MB frente a los ~740 MB del SDK. Máxima seguridad y mínima superficie de ataque para contenedores de producción.
**En el proyecto:** `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` es la imagen base del stage `final` del Dockerfile de la API del sistema de mensajería.

### NuGet
**Categoría:** .NET / Gestión de paquetes
**Definición:** Gestor de paquetes oficial de .NET, equivalente a npm para JavaScript o pip para Python. Los paquetes se definen en archivos `.csproj` y se restauran con `dotnet restore`. En Docker, restaurar las dependencias NuGet es el paso más costoso en tiempo, por lo que se optimiza con caché de capas.
**En el proyecto:** El stage `restore` del Dockerfile del CRM copia solo los `.csproj` y ejecuta `dotnet restore` para cachear todos los paquetes NuGet. Si no cambia ningún `.csproj`, este stage (~60 segundos) se saltea en builds posteriores.

---

## O

### Onboarding (contexto DevOps)
**Categoría:** Docker / Productividad
**Definición:** Proceso de incorporar a un nuevo desarrollador al proyecto: instalar herramientas, configurar el entorno local, clonar el repositorio y ejecutar la aplicación. Sin Docker, este proceso puede tomar días por incompatibilidades de versiones. Con Docker, se reduce a `git clone` + `docker compose up`.
**En el proyecto:** El `compose.yaml` del sistema de mensajería incluye todos los servicios necesarios: un nuevo desarrollador de la empresa puede tener el entorno completo corriendo en minutos.

---

## P

### pg_isready
**Categoría:** PostgreSQL / Docker / Healthcheck
**Definición:** Utilidad de PostgreSQL que verifica si el servidor está listo para aceptar conexiones. Retorna código de salida 0 si el servidor está disponible, 1 si no. Se usa ampliamente como comando de healthcheck en Compose para esperar a que PostgreSQL esté listo antes de arrancar servicios dependientes.
**En el proyecto:** `pg_isready -U app_user -d crm` es el test del healthcheck del contenedor `postgres` en el `compose.yaml` del sistema de mensajería.

### ports (Compose)
**Categoría:** Docker Compose
**Definición:** Directiva de Compose que publica puertos del contenedor en el host, siguiendo el formato `"host_port:container_port"`. Con el formato `"127.0.0.1:puerto:puerto"` solo acepta conexiones desde localhost, sin exponerlo a la red local ni a internet  -  más seguro para servicios internos.
**En el proyecto:** La API del CRM expone `8080:8080` y el web `3000:80`. En un servidor de producción detrás de un reverse proxy, solo Nginx estaría expuesto externamente.

### `proxy_pass` (Nginx)
**Categoría:** Docker / Nginx / Proxy
**Definición:** Directiva de Nginx que redirige las solicitudes entrantes hacia otro servidor (upstream). Se usa para que Nginx actúe como proxy reverso hacia la API. Las cabeceras `X-Real-IP`, `X-Forwarded-For` y `X-Forwarded-Proto` se agregan para que el backend conozca la IP y protocolo reales del cliente original.
**En el proyecto:** `proxy_pass http://mensajeria-api:8080/api/` en el `nginx.conf` redirige todas las llamadas a `/api/` desde el navegador hacia el contenedor de la API, eliminando problemas de CORS en producción.

### publish (dotnet publish)
**Categoría:** .NET / Build
**Definición:** Comando de .NET que compila la aplicación y genera un directorio de salida listo para producción, incluyendo el binario, las DLLs de dependencias y los archivos estáticos necesarios. El flag `/p:UseAppHost=false` omite la generación del ejecutable nativo del host (no necesario en contenedores Linux).
**En el proyecto:** `dotnet publish "Host/Host.csproj" -c Release --no-build -o /app/publish` en el stage `publish` genera el output que se copia al stage `final` de la imagen de producción del sistema de mensajería.

---

## R

### read-only layer / read-write layer
**Categoría:** Docker / Arquitectura
**Definición:** Las capas de una imagen son todas de solo lectura (read-only). Cuando se crea un contenedor, Docker agrega una capa de lectura-escritura (read-write layer) encima. Cualquier modificación al sistema de archivos dentro del contenedor ocurre en esta capa temporal, que se destruye cuando el contenedor se elimina.
**En el proyecto:** Los datos de PostgreSQL del sistema de mensajería se almacenan en el named volume `postgres_data`, no en la capa read-write del contenedor, garantizando persistencia entre reinicios.

### registry
**Categoría:** Docker / Distribución
**Definición:** Servicio que almacena y distribuye imágenes Docker. Es el equivalente a npm o NuGet para imágenes. Los más comunes son Docker Hub (público), GHCR (GitHub), ECR (AWS), ACR (Azure) y registries privados auto-hospedados. Las imágenes se identifican como `registry/namespace/nombre:tag`.
**En el proyecto:** La empresa usa GHCR (`ghcr.io/acme/`) para almacenar las imágenes del sistema de mensajería. El servidor de producción hace `docker pull` desde GHCR para obtener nuevas versiones.

### restart (política de reinicio)
**Categoría:** Docker Compose / Confiabilidad
**Definición:** Directiva de Compose que define cuándo Docker debe reiniciar automáticamente un contenedor. Las opciones son `no` (nunca), `always` (siempre), `on-failure` (solo si termina con error) y `unless-stopped` (siempre, excepto si se detuvo manualmente).
**En el proyecto:** Todos los servicios del sistema de mensajería (api, web, postgres) usan `restart: unless-stopped` para sobrevivir reinicios del servidor y recuperarse de crashes inesperados.

### RUN
**Categoría:** Docker / Dockerfile
**Definición:** Instrucción de Dockerfile que ejecuta un comando durante el proceso de build y crea una nueva capa con los cambios resultantes. Se usa para instalar paquetes, compilar código o ejecutar scripts de configuración. Cada `RUN` genera una capa; se pueden encadenar comandos con `&&` para reducir capas.
**En el proyecto:** `RUN dotnet restore`, `RUN dotnet build` y `RUN dotnet publish` son las instrucciones `RUN` de los stages del Dockerfile del sistema de mensajería.

---

## S

### SDK (.NET SDK)
**Categoría:** .NET / Docker
**Definición:** Software Development Kit de .NET que incluye el compilador, las herramientas de build (`dotnet build`, `dotnet publish`, `dotnet restore`) y el runtime. Pesa ~740 MB. No debe incluirse en imágenes de producción; solo se necesita durante el build. La imagen de producción solo requiere el runtime (ASP.NET Core runtime, ~110 MB).
**En el proyecto:** El stage `restore` y `build` del Dockerfile del sistema de mensajería usan `mcr.microsoft.com/dotnet/sdk:10.0`. El stage `final` usa solo `aspnet:10.0-noble-chiseled` (runtime únicamente).

### SemVer (Semantic Versioning)
**Categoría:** DevOps / Versionado
**Definición:** Estándar de versionado de software con el formato `MAJOR.MINOR.PATCH`. `MAJOR` cambia cuando hay cambios incompatibles de API, `MINOR` cuando se agrega funcionalidad compatible hacia atrás, `PATCH` para correcciones de bugs. Se usa como tag de imagen Docker en releases oficiales.
**En el proyecto:** Las imágenes de producción del sistema de mensajería se tagean con SemVer: `mensajeria-api:v1.2.0`. Un nuevo feature bump el MINOR (v1.2.0 -> v1.3.0), un bug fix bump el PATCH (v1.2.0 -> v1.2.1).

### service (Compose)
**Categoría:** Docker Compose
**Definición:** Cada contenedor definido en la sección `services:` del `compose.yaml`. Un service puede definir imagen o build, puertos, variables de entorno, volúmenes, redes, dependencias y política de reinicio. Compose permite escalar services con `--scale`, aunque para el sistema de mensajería cada service corre como una instancia única.
**En el proyecto:** El sistema de mensajería tiene tres services principales: `api` (back-end .NET), `web` (front-end React/Nginx) y `postgres` (base de datos).

### SIGKILL
**Categoría:** Docker / Señales de proceso
**Definición:** Señal de terminación forzada enviada al proceso de un contenedor. A diferencia de `SIGTERM`, no puede ser capturada ni ignorada por el proceso. Docker la envía cuando el tiempo de gracia de `docker stop` expira. Causa terminación inmediata sin posibilidad de limpieza.
**En el proyecto:** Si la API del sistema de mensajería no termina sus solicitudes pendientes dentro del tiempo de gracia de `SIGTERM`, Docker envía `SIGKILL`, interrumpiendo abruptamente las conversaciones en vuelo.

### SIGTERM
**Categoría:** Docker / Señales de proceso
**Definición:** Señal de terminación "amable" enviada al proceso principal de un contenedor cuando se ejecuta `docker stop`. El proceso puede capturarla para realizar un cierre ordenado (graceful shutdown): completar solicitudes en curso, cerrar conexiones y liberar recursos. Solo llega directamente al proceso si se usa la forma exec en `ENTRYPOINT`.
**En el proyecto:** El `ENTRYPOINT ["dotnet", "Host.dll"]` en forma exec garantiza que la API del CRM recibe `SIGTERM` directamente, permitiendo que ASP.NET Core ejecute los `IHostedService.StopAsync` registrados.

### SPA routing (Single Page Application routing)
**Categoría:** Frontend / Nginx
**Definición:** Configuración de servidor web que redirige todas las rutas que no corresponden a archivos estáticos al `index.html` de la aplicación. Necesaria para que el router del lado del cliente (React Router) gestione la navegación sin que Nginx devuelva 404 en rutas como `/conversations/123`.
**En el proyecto:** `try_files $uri $uri/ /index.html;` en el `nginx.conf` del sistema de mensajería permite navegar directamente a cualquier ruta de la bandeja de entrada, el chat o los contactos sin obtener error 404.

### start_period (healthcheck)
**Categoría:** Docker / Healthcheck
**Definición:** Parámetro del `HEALTHCHECK` que define un período de gracia inicial durante el cual los fallos del healthcheck no se cuentan hacia el límite de `retries`. Permite que servicios con arranque lento (como bases de datos) tengan tiempo de inicializarse antes de ser marcados como `unhealthy`.
**En el proyecto:** PostgreSQL del sistema de mensajería tiene `start_period: 15s` para que no se marque como unhealthy durante la inicialización de la base de datos en el primer arranque.

---

## T

### tag (imagen Docker)
**Categoría:** Docker / Versionado
**Definición:** Etiqueta que identifica una versión específica de una imagen Docker. Forma parte del nombre completo de la imagen: `registro/namespace/nombre:tag`. Si se omite, Docker asume `:latest`. En producción se deben usar tags fijos y reproducibles como versiones SemVer o hashes de commit Git.
**En el proyecto:** Las imágenes del sistema de mensajería usan `v1.x.x` para releases y el SHA del commit en el pipeline de CI/CD. El tag `:latest` está prohibido en el compose.yaml de producción.

### target (Compose build)
**Categoría:** Docker Compose / Multi-stage
**Definición:** Directiva dentro de `build:` en Compose que especifica hasta qué stage del Dockerfile multi-stage construir. Permite usar el mismo Dockerfile para construir imágenes de distintos stages (por ejemplo, un stage de testing o de desarrollo).
**En el proyecto:** `target: final` en el servicio `api` del `compose.yaml` asegura que solo se construya y use el stage `final` (distroless) en vez de un stage intermedio con herramientas de build.

---

## U

### USER
**Categoría:** Docker / Dockerfile / Seguridad
**Definición:** Instrucción de Dockerfile que cambia el usuario bajo el que corren los procesos subsiguientes del contenedor. Por defecto, Docker corre los procesos como `root`, lo que representa un riesgo de seguridad. Usar un usuario no-root limita el daño en caso de compromiso del contenedor.
**En el proyecto:** `USER app` en el stage `final` del Dockerfile del sistema de mensajería hace que la API corra como el usuario `app` (incluido en la imagen `aspnet:10.0-noble-chiseled`), sin privilegios de root.

---

## V

### Vite
**Categoría:** Frontend / Build
**Definición:** Herramienta de build y servidor de desarrollo para aplicaciones JavaScript/TypeScript. Genera un bundle optimizado en el directorio `dist/` con `npm run build`. Los archivos del bundle incluyen hashes en su nombre para invalidación de caché (cache-busting). Variables de entorno con prefijo `VITE_` se inyectan en el bundle en tiempo de build.
**En el proyecto:** El frontend del sistema de mensajería (React 19) se compila con Vite. El Dockerfile del front-end corre `npm run build` en el stage `build` y copia el `dist/` resultante al stage de Nginx.

### VITE_API_URL
**Categoría:** Docker / Frontend / Variables de entorno
**Definición:** Variable de entorno con prefijo `VITE_` que Vite inyecta en el bundle JavaScript en tiempo de compilación. A diferencia de variables de entorno en runtime (como las de .NET), las variables `VITE_` deben estar disponibles cuando se ejecuta `npm run build`, por lo que se pasan como `ARG` al Dockerfile.
**En el proyecto:** `ARG VITE_API_URL=https://api.mensajeria.com` se pasa al Dockerfile del front-end durante CI/CD para configurar el endpoint de la API del sistema de mensajería en el bundle de producción.

### volumes (Compose)
**Categoría:** Docker Compose / Persistencia
**Definición:** Sección del `compose.yaml` donde se declaran los named volumes usados por los servicios. Los volumes declarados aquí persisten entre `docker compose down` y `docker compose up`. Con `docker compose down -v` se eliminan, haciendo un reset completo de los datos.
**En el proyecto:** `postgres_data` es el único named volume del sistema de mensajería, asegurando que los datos de conversaciones, contactos y mensajes persistan entre reinicios del stack.

---

## W

### WORKDIR
**Categoría:** Docker / Dockerfile
**Definición:** Instrucción de Dockerfile que establece el directorio de trabajo para todas las instrucciones subsiguientes (`RUN`, `COPY`, `ENTRYPOINT`, `CMD`). Si el directorio no existe, lo crea. Usar `WORKDIR` es preferible a `RUN mkdir && cd` porque es explícito y compatible con todas las plataformas.
**En el proyecto:** `WORKDIR /src` se usa en los stages de build del sistema de mensajería para establecer el contexto de compilación, y `WORKDIR /app` en el stage `final` como directorio de trabajo del runtime de producción.

---

## X

### X-Forwarded-For
**Categoría:** HTTP / Nginx / Proxy
**Definición:** Cabecera HTTP que un proxy reverso (como Nginx) agrega a las solicitudes reenviadas al backend, conteniendo la IP real del cliente original. Necesaria para que el back-end pueda realizar rate limiting, logging y auditoría basados en la IP del cliente real, no en la IP del proxy.
**En el proyecto:** El `nginx.conf` del sistema de mensajería agrega `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;` para que la API de .NET pueda registrar la IP real de los agentes que acceden al sistema.

### X-Forwarded-Proto
**Categoría:** HTTP / Nginx / Proxy
**Definición:** Cabecera HTTP que indica al backend si la conexión original del cliente era HTTP o HTTPS. Necesaria para que ASP.NET Core genere URLs absolutas correctas y para redirigir HTTP a HTTPS cuando es apropiado.
**En el proyecto:** El proxy de Nginx hacia la API del CRM incluye `proxy_set_header X-Forwarded-Proto $scheme;` para que el middleware de forwarded headers de ASP.NET Core funcione correctamente.

### X-Real-IP
**Categoría:** HTTP / Nginx / Proxy
**Definición:** Cabecera HTTP que un proxy reverso agrega para transmitir la IP original del cliente al backend. Similar a `X-Forwarded-For` pero contiene únicamente la IP del cliente directo, sin la cadena de proxies intermedios.
**En el proyecto:** Incluida en el bloque `location /api/` del `nginx.conf` del sistema de mensajería para que la API pueda identificar y registrar la IP real de cada agente.
