# Glosario Técnico  -  CI/CD  -  GitHub Actions

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### actions/checkout@v4
**Categoría:** GitHub Actions
**Definición:** Action oficial de GitHub que descarga el código fuente del repositorio al sistema de archivos del runner. Sin este paso el workspace está vacío y ningún comando de build puede encontrar los archivos del proyecto.
**En el proyecto:** Es siempre el primer step de cada workflow del sistema de mensajería; descarga el código de `mensajeria/` antes de ejecutar `dotnet build` o `docker build`.

### actions/setup-dotnet@v4
**Categoría:** GitHub Actions
**Definición:** Action oficial que instala una versión específica del SDK de .NET en el runner. Aunque los runners de GitHub tienen versiones preinstaladas, fijar la versión explícitamente garantiza reproducibilidad entre ejecuciones.
**En el proyecto:** Se usa en `ci.yml` para instalar .NET 10.x antes de compilar y ejecutar los tests del sistema de mensajería.

### actions/upload-artifact@v4
**Categoría:** GitHub Actions
**Definición:** Action que persiste archivos generados durante el pipeline (binarios, reportes, logs) en el almacenamiento de GitHub. Los artifacts pueden descargarse desde la UI o pasarse a jobs posteriores usando `actions/download-artifact`.
**En el proyecto:** Se usa para guardar los reportes de cobertura de código (`./coverage`) generados por `dotnet test --collect:"XPlat Code Coverage"`, con retención de 7 días.

### ADR (Architecture Decision Record)
**Categoría:** DevOps / Arquitectura
**Definición:** Documento que registra una decisión arquitectónica importante, incluyendo el contexto, las alternativas evaluadas y la justificación de la elección. Sirve como historial para que el equipo entienda por qué se tomaron ciertas decisiones.
**En el proyecto:** El equipo del sistema de mensajería utiliza ADRs para documentar decisiones como la elección de stack (.NET 10 + React 19 + PostgreSQL) y la arquitectura monolito modular.

### amazon-ecr-login@v2
**Categoría:** GitHub Actions / AWS
**Definición:** Action de AWS que autentica el daemon de Docker en el runner contra Amazon ECR, obteniendo un token de acceso temporal. Después de este step, los comandos `docker build` y `docker push` pueden interactuar con el registry privado.
**En el proyecto:** Se ejecuta en el job `deploy` de `cd-oidc.yml` justo antes del step que construye y sube la imagen del sistema de mensajería a ECR.

### amazon-ecs-deploy-task-definition@v1
**Categoría:** GitHub Actions / AWS
**Definición:** Action de AWS que registra una nueva revisión de la task definition en ECS y actualiza el servicio indicado para que use esa revisión. Puede esperar a que el servicio sea estable antes de marcar el step como exitoso.
**En el proyecto:** Es el último step del `cd-oidc.yml` del sistema de mensajería; despliega la imagen recién construida al cluster ECS y bloquea el pipeline hasta confirmar que los contenedores están healthy.

### amazon-ecs-render-task-definition@v1
**Categoría:** GitHub Actions / AWS
**Definición:** Action de AWS que toma una task definition existente y genera una nueva versión con la imagen Docker actualizada, sin necesidad de reescribir toda la definición de tarea manualmente.
**En el proyecto:** Se usa en el `cd.yml` (versión con IAM keys) para inyectar el tag de la imagen recién construida en la task definition del servicio `mensajeria-service`.

### ARN (Amazon Resource Name)
**Categoría:** AWS
**Definición:** Identificador único y canónico de cualquier recurso en AWS. Sigue el formato `arn:aws:servicio:región:cuenta:tipo/nombre`. Se usa en políticas IAM para especificar exactamente a qué recurso aplica un permiso.
**En el proyecto:** Los ARNs identifican el IAM Role que GitHub asume via OIDC, el repositorio ECR (`mensajeria-api`) y el cluster ECS donde corre el sistema de mensajería.

### Artifact
**Categoría:** CI/CD / GitHub Actions
**Definición:** Archivo o conjunto de archivos generados como resultado del pipeline  -  puede ser un binario compilado, un reporte de cobertura, un paquete NuGet o una imagen Docker. Los artifacts persisten más allá de la duración del workflow.
**En el proyecto:** Los reportes de cobertura de `dotnet test` y la imagen Docker publicada en ECR son los principales artifacts del pipeline del sistema de mensajería.

### aws-actions/configure-aws-credentials@v4
**Categoría:** GitHub Actions / AWS
**Definición:** Action que configura las credenciales de AWS en el runner, ya sea a partir de IAM Access Keys guardadas como secrets o mediante el intercambio de un token OIDC por credenciales temporales de STS. Expone las credenciales como variables de entorno estándar de AWS CLI.
**En el proyecto:** Se usa en ambas variantes del CD del sistema de mensajería: con IAM keys (variante básica) y con OIDC (variante recomendada), para que los steps posteriores puedan interactuar con ECR y ECS.

### AWS CLI
**Categoría:** AWS / DevOps
**Definición:** Herramienta de línea de comandos oficial de Amazon Web Services que permite interactuar con todos los servicios de AWS desde una terminal o un script. Los runners de GitHub la tienen preinstalada.
**En el proyecto:** Se usa directamente en los pasos de rollback para listar revisiones anteriores de la task definition y forzar un nuevo deploy con `aws ecs update-service`.

---

## B

### Baseline (métricas)
**Categoría:** Observabilidad / DevOps
**Definición:** Valores de referencia de las métricas de producción en condiciones normales (latencia, tasa de error, uso de CPU/memoria). Sirven como umbral para detectar regresiones tras un deploy.
**En el proyecto:** Después de cada deploy del sistema de mensajería se comparan la latencia p95 y la tasa de error contra el baseline para decidir si el deploy fue exitoso o si se necesita rollback.

### Branch protection
**Categoría:** GitHub / CI/CD
**Definición:** Reglas configuradas en un repositorio de GitHub que impiden hacer merge o push directo a ramas importantes (como `main`) sin cumplir ciertas condiciones, como que el CI esté en verde o que haya una aprobación de código.
**En el proyecto:** La rama `main` del sistema de mensajería debe tener branch protection activada para que el check de CI (`ci.yml`) sea obligatorio antes de cualquier merge.

### Build
**Categoría:** CI/CD
**Definición:** Proceso de compilar el código fuente y resolver sus dependencias para producir un ejecutable o artefacto desplegable. En el contexto de .NET, se ejecuta con `dotnet build`; en Docker, con `docker build`.
**En el proyecto:** `dotnet build --no-restore --configuration Release` compila la solución del sistema de mensajería en modo Release; `docker build -t $IMAGE .` genera la imagen que se sube a ECR.

---

## C

### CD (Continuous Delivery)
**Categoría:** CI/CD
**Definición:** Práctica en la que el software se mantiene siempre en un estado listo para ser desplegado a producción. El pipeline automatiza hasta la etapa de staging o pre-producción; el deploy final lo decide y dispara un humano.
**En el proyecto:** El `cd-oidc.yml` puede configurarse para que deploya automáticamente a staging pero requiera aprobación manual (via environment protection rules) para pasar a producción.

### CD (Continuous Deployment)
**Categoría:** CI/CD
**Definición:** Extensión de Continuous Delivery donde el deploy a producción ocurre automáticamente cada vez que el CI pasa. No hay intervención humana entre el merge y la actualización en producción.
**En el proyecto:** En el pipeline del sistema de mensajería, el push a `main` o un tag `v*.*.*` dispara automáticamente el `cd-oidc.yml`, que es el comportamiento de Continuous Deployment para el ambiente de staging.

### CI (Continuous Integration)
**Categoría:** CI/CD
**Definición:** Práctica de integrar cambios de código con frecuencia (varias veces al día) en una rama compartida, acompañada de una suite automatizada de compilación y pruebas que valida cada integración. El objetivo es detectar errores lo antes posible.
**En el proyecto:** El `ci.yml` del sistema de mensajería valida cada PR hacia `main` o `develop`: compila con .NET 10 y ejecuta todos los tests. Un CI rojo bloquea el merge.

### Claim (JWT / OIDC)
**Categoría:** Seguridad / OIDC
**Definición:** Par clave-valor dentro del payload de un token JWT que describe propiedades del sujeto o del token mismo. En el contexto de OIDC, claims como `sub` (subject) y `aud` (audience) identifican quién emitió el token y para qué servicio.
**En el proyecto:** La trust policy del IAM Role del sistema de mensajería usa el claim `sub` para restringir el acceso solo al repo `acme/mensajeria`, impidiendo que otros repos asuman el mismo rol.

### CloudWatch
**Categoría:** AWS / Observabilidad
**Definición:** Servicio de monitoreo y observabilidad de AWS que recopila logs, métricas y eventos de los servicios de AWS (ECS, EC2, RDS, etc.) y permite crear dashboards, alarmas y búsquedas sobre esos datos.
**En el proyecto:** Los contenedores del sistema de mensajería en ECS envían sus logs a CloudWatch Logs, donde el equipo los inspecciona durante y después de cada deploy para detectar errores nuevos.

### Cluster (ECS)
**Categoría:** AWS / ECS
**Definición:** Agrupación lógica de capacidad de cómputo (instancias EC2 o Fargate) en Amazon ECS donde se ejecutan los servicios y tareas. Un cluster puede alojar múltiples servicios independientes.
**En el proyecto:** El cluster `mensajeria-cluster` agrupa todos los servicios del sistema de mensajería desplegados en ECS.

### configure-aws-credentials
**Categoría:** GitHub Actions / AWS
**Definición:** Ver `aws-actions/configure-aws-credentials@v4`.
**En el proyecto:** Primer step de autenticación en los workflows de CD del sistema de mensajería, antes de interactuar con ECR o ECS.

### Container (contenedor)
**Categoría:** Contenedores / DevOps
**Definición:** Instancia en ejecución de una imagen Docker. Aísla el proceso de la aplicación junto con sus dependencias del sistema operativo del host, garantizando que se comporte igual en desarrollo, staging y producción.
**En el proyecto:** En ECS, cada tarea corre un contenedor del sistema de mensajería basado en la imagen publicada en ECR. En rollback, se detiene el contenedor actual y se inicia uno con la imagen anterior.

### Cobertura de código (Code Coverage)
**Categoría:** Testing / CI
**Definición:** Métrica que indica el porcentaje de líneas, ramas o condiciones del código fuente que son ejercidas por los tests automatizados. Se genera con herramientas como Coverlet en .NET y se reporta en formatos como Cobertura XML.
**En el proyecto:** El `ci.yml` del sistema de mensajería puede ejecutar `dotnet test --collect:"XPlat Code Coverage"` y subir el reporte como artifact para inspección o integración con SonarCloud.

---

## D

### Deployment branches (protection rule)
**Categoría:** GitHub Actions / Seguridad
**Definición:** Regla de protección de environment en GitHub que restringe qué ramas o tags pueden disparar un deploy a ese environment. Previene que ramas de feature accidentalmente actualicen producción.
**En el proyecto:** El environment `production` del sistema de mensajería debe configurarse para aceptar deploys solo desde `main` o tags que coincidan con `v*.*.*`.

### Docker
**Categoría:** Contenedores
**Definición:** Plataforma de contenedores que permite empaquetar una aplicación junto con todas sus dependencias en una imagen portable y ejecutarla en cualquier entorno que tenga el daemon de Docker instalado.
**En el proyecto:** El `back-template` del sistema de mensajería incluye un `Dockerfile` multi-stage que genera la imagen del API. Esa imagen es construida en el CD y desplegada en ECS.

### docker build
**Categoría:** Docker / CI/CD
**Definición:** Comando que lee el `Dockerfile` del directorio actual (o el especificado con `-f`) y construye una imagen Docker, ejecutando cada instrucción como una capa. El flag `-t` asigna un nombre y tag a la imagen resultante.
**En el proyecto:** En el CD del sistema de mensajería: `docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG .` genera la imagen que se publica en ECR.

### docker push
**Categoría:** Docker / CI/CD
**Definición:** Comando que sube una imagen Docker local a un registry remoto (Docker Hub, ECR, GHCR, etc.). Requiere que el daemon esté autenticado en el registry de destino.
**En el proyecto:** Después de `docker build`, el CD usa `docker push $IMAGE` para subir la imagen del sistema de mensajería al repositorio ECR configurado.

### docker pull
**Categoría:** Docker / CI/CD
**Definición:** Comando que descarga una imagen Docker desde un registry remoto al host local. En operaciones de rollback, se usa para obtener la versión anterior de la imagen.
**En el proyecto:** En un rollback en EC2, el equipo usa `docker pull mensajeria-api:v1.1.0` para descargar la imagen de la versión estable anterior desde ECR.

### docker run
**Categoría:** Docker
**Definición:** Comando que crea e inicia un contenedor a partir de una imagen. Acepta flags para puertos, variables de entorno, volúmenes y modo de ejecución (foreground o detached con `-d`).
**En el proyecto:** En el flujo de rollback en EC2 del sistema de mensajería, tras detener el contenedor actual se usa `docker run -d --name mensajeria ... mensajeria-api:v1.1.0` para levantar la versión anterior.

### docker stop / docker rm
**Categoría:** Docker
**Definición:** `docker stop` envía SIGTERM al proceso principal del contenedor para detenerlo limpiamente. `docker rm` elimina el contenedor detenido del sistema (la imagen sigue disponible localmente).
**En el proyecto:** En el rollback manual en EC2 del sistema de mensajería, se ejecutan en secuencia para detener el contenedor actual antes de levantar uno con la imagen de la versión anterior.

### Dockerfile
**Categoría:** Docker / CI/CD
**Definición:** Archivo de texto con instrucciones que describen cómo construir una imagen Docker: sistema base, instalación de dependencias, copia de archivos y comando de inicio. Un Dockerfile multi-stage usa múltiples etapas para reducir el tamaño de la imagen final.
**En el proyecto:** El `back-template` del sistema de mensajería incluye un Dockerfile multi-stage que compila el proyecto .NET en una etapa de build y copia solo el binario resultante a la imagen final, evitando incluir el SDK en producción.

### dotnet build
**Categoría:** .NET / CI
**Definición:** Comando del CLI de .NET que compila el proyecto o solución. La opción `--no-restore` omite la restauración de paquetes (cuando ya se hizo en un step previo) y `--configuration Release` compila en modo optimizado.
**En el proyecto:** En el `ci.yml` del sistema de mensajería se ejecuta como step independiente tras `dotnet restore`, permitiendo cachear los paquetes NuGet entre ejecuciones.

### dotnet restore
**Categoría:** .NET / CI
**Definición:** Comando que descarga y restaura los paquetes NuGet declarados en los archivos `.csproj` de la solución. Se separa del build para permitir que la capa de cache del runner reutilice los paquetes cuando no cambia el archivo de dependencias.
**En el proyecto:** Es el tercer step del `ci.yml` del sistema de mensajería. Descarga todos los paquetes de `Mensajeria.Application`, `Mensajeria.Domain`, `Mensajeria.Infrastructure` y `Mensajeria.Api`.

### dotnet test
**Categoría:** .NET / CI / Testing
**Definición:** Comando que descubre y ejecuta todos los proyectos de test (`*.Tests.csproj`) en la solución. Las opciones `--no-build` evitan recompilar, `--configuration Release` asegura que se ejecutan los binarios de Release, y `--collect:"XPlat Code Coverage"` habilita la medición de cobertura.
**En el proyecto:** Es el step final del `ci.yml` del sistema de mensajería; corre los tests de `Mensajeria.Tests/` y falla el job (bloqueando el PR) si algún test no pasa.

---

## E

### ECR (Amazon Elastic Container Registry)
**Categoría:** AWS / Docker
**Definición:** Registry privado de imágenes Docker totalmente administrado por AWS. Integra autenticación IAM, escaneo de vulnerabilidades y control de versiones de imágenes. Las imágenes se acceden con su URI completo (`cuenta.dkr.ecr.región.amazonaws.com/repo:tag`).
**En el proyecto:** El repositorio `mensajeria-api` en ECR almacena cada versión de la imagen Docker del sistema de mensajería. El CD sube la imagen tras el build; ECS la descarga al escalar o hacer rollback.

### ECS (Amazon Elastic Container Service)
**Categoría:** AWS / Contenedores
**Definición:** Servicio de orquestación de contenedores de AWS que gestiona el ciclo de vida de los contenedores Docker en un cluster. Soporta dos modos de ejecución: EC2 (instancias propias) y Fargate (serverless).
**En el proyecto:** ECS ejecuta el API del sistema de mensajería en el cluster `mensajeria-cluster`. El CD actualiza el servicio `mensajeria-service` con cada nueva imagen publicada en ECR.

### Environment (GitHub Actions)
**Categoría:** GitHub Actions / CI/CD
**Definición:** Contexto lógico en GitHub que agrupa secrets específicos de un ambiente (staging, production) y puede tener protection rules que controlan quién y cuándo puede deployar a ese ambiente.
**En el proyecto:** El sistema de mensajería usa el environment `production` en GitHub con protection rules que requieren aprobación manual antes de que el CD ejecute el deploy al cluster de producción.

### Exit code
**Categoría:** CI/CD / Linux
**Definición:** Valor numérico que devuelve un proceso al terminar. `0` indica éxito; cualquier otro valor indica error. GitHub Actions interpreta cualquier exit code distinto de `0` como fallo del step, lo que detiene el job y marca el workflow como fallido.
**En el proyecto:** Si `dotnet build` o `dotnet test` fallan (exit code != 0), el `ci.yml` del sistema de mensajería falla y bloquea el merge del PR.

---

## F

### Federated identity
**Categoría:** Seguridad / AWS / OIDC
**Definición:** Mecanismo que permite a un sistema externo (como GitHub) actuar como proveedor de identidad para AWS, de modo que las cargas de trabajo autenticadas por ese sistema puedan obtener credenciales temporales de AWS sin necesitar credenciales de larga duración.
**En el proyecto:** La integración OIDC entre GitHub Actions y AWS usa federated identity: GitHub certifica la identidad del workflow; AWS STS confía en esa certificación y emite credenciales temporales para el deploy del sistema de mensajería.

### Force new deployment (ECS)
**Categoría:** AWS / ECS / Rollback
**Definición:** Opción del comando `aws ecs update-service --force-new-deployment` que obliga a ECS a reemplazar las tareas en ejecución por nuevas instancias de la task definition especificada, aunque no haya cambios en la definición.
**En el proyecto:** En el rollback del sistema de mensajería se usa `--force-new-deployment` junto con una revisión anterior de la task definition para volver a la versión estable sin tener que deployar una imagen nueva.

---

## G

### GITHUB_OUTPUT
**Categoría:** GitHub Actions
**Definición:** Variable de entorno que apunta a un archivo especial del runner donde los steps pueden escribir outputs con el formato `NOMBRE=VALOR`. Estos outputs pueden ser leídos por steps posteriores mediante `${{ steps.ID_DEL_STEP.outputs.NOMBRE }}`.
**En el proyecto:** El step `Set image tag` del CD escribe el tag de la imagen (`IMAGE_TAG`) en `$GITHUB_OUTPUT`, y el step de build usa ese valor para nombrar la imagen antes de subirla a ECR.

### GITHUB_REF
**Categoría:** GitHub Actions
**Definición:** Variable de entorno que contiene la referencia completa que disparó el workflow (ej. `refs/heads/main`, `refs/tags/v1.2.0`). Se usa para diferenciar en el pipeline si el trigger fue un push a una rama o un tag de versión.
**En el proyecto:** En el CD del sistema de mensajería se evalúa `${{ github.ref }}` para determinar si usar el tag semántico (`v1.2.0`) o el SHA del commit como tag de la imagen Docker.

### github.sha
**Categoría:** GitHub Actions
**Definición:** Contexto de GitHub Actions que contiene el SHA completo del commit que disparó el workflow. Es único por commit y garantiza trazabilidad entre la imagen Docker y el código fuente exacto que la generó.
**En el proyecto:** Cuando el CD se dispara por push a `main` (no por tag), la imagen del sistema de mensajería se etiqueta con `${{ github.sha }}`, permitiendo saber exactamente qué commit está corriendo en producción.

### GitHub Actions
**Categoría:** CI/CD / GitHub
**Definición:** Plataforma de automatización integrada en GitHub que permite definir workflows en archivos YAML ubicados en `.github/workflows/`. Puede ejecutar código en respuesta a eventos del repositorio (push, PR, schedule) en runners hospedados por GitHub o self-hosted.
**En el proyecto:** Es la plataforma de CI/CD elegida para el sistema de mensajería. Los workflows `ci.yml` y `cd-oidc.yml` automatizan la validación de PRs y el deploy a AWS ECS respectivamente.

### GitHub-hosted runner
**Categoría:** GitHub Actions
**Definición:** Máquina virtual provisionada y mantenida por GitHub que ejecuta los jobs de los workflows. Incluye herramientas preinstaladas (Docker, AWS CLI, .NET, Node.js). Se destruye al terminar el job. El runner `ubuntu-latest` es el más común.
**En el proyecto:** Todos los jobs del sistema de mensajería usan `runs-on: ubuntu-latest`, que es un runner Linux gestionado por GitHub con las herramientas de build y AWS preinstaladas.

---

## H

### Health check
**Categoría:** DevOps / ECS
**Definición:** Verificación periódica que un orquestador (ECS, Kubernetes) o un load balancer realiza contra un endpoint del servicio (típicamente `/health`) para determinar si el contenedor está listo para recibir tráfico. Un contenedor que falla los health checks es reemplazado automáticamente.
**En el proyecto:** ECS verifica el endpoint `/health` del API del sistema de mensajería tras cada deploy. Si los health checks fallan, `wait-for-service-stability: true` hace que el pipeline también falle, evitando que un deploy roto pase desapercibido.

---

## I

### IAM (Identity and Access Management)
**Categoría:** AWS / Seguridad
**Definición:** Servicio de AWS para gestionar la autenticación y autorización de usuarios, servicios y cargas de trabajo. Define quién puede hacer qué en AWS mediante usuarios, roles, políticas y permisos.
**En el proyecto:** El pipeline del sistema de mensajería usa un IAM Role con permisos mínimos para ECR (push de imágenes) y ECS (actualización del servicio). Con OIDC, GitHub asume ese rol temporalmente en lugar de usar credenciales de usuario.

### IAM Access Key
**Categoría:** AWS / Seguridad
**Definición:** Par de credenciales (`AWS_ACCESS_KEY_ID` + `AWS_SECRET_ACCESS_KEY`) de larga duración asociadas a un usuario IAM. Son válidas hasta que se deshabilitan manualmente, por lo que representan un riesgo si se filtran.
**En el proyecto:** El enfoque inicial del `cd.yml` usa IAM Access Keys guardadas como secrets en GitHub. El `cd-oidc.yml` elimina esta necesidad reemplazándolas con tokens OIDC temporales.

### IAM Role
**Categoría:** AWS / Seguridad
**Definición:** Identidad de AWS que puede ser asumida temporalmente por servicios, usuarios u otras cuentas para obtener un conjunto acotado de permisos. A diferencia de los usuarios IAM, los roles no tienen credenciales permanentes; cada vez que se asumen generan credenciales de sesión temporales.
**En el proyecto:** El rol `github-actions-deploy-role` es asumido por el runner de GitHub durante el pipeline del sistema de mensajería. Tiene permisos mínimos sobre ECR y ECS, y su trust policy limita el acceso al repo `acme/mensajeria`.

### id-token: write (permission)
**Categoría:** GitHub Actions / OIDC
**Definición:** Permiso de GitHub Actions que habilita al workflow para solicitar un token OIDC firmado por GitHub. Sin este permiso declarado en la sección `permissions:`, GitHub no emite el token y la autenticación OIDC falla.
**En el proyecto:** Es un campo obligatorio en el `cd-oidc.yml` del sistema de mensajería. Se declara junto con `contents: read` para seguir el principio de mínimo privilegio en los permisos del workflow.

### Idempotencia (en deploys)
**Categoría:** DevOps
**Definición:** Propiedad de una operación que produce el mismo resultado sin importar cuántas veces se ejecute. En CI/CD, un deploy idempotente puede correrse múltiples veces sin efectos secundarios indeseados (ej. duplicación de recursos).
**En el proyecto:** El uso de tags de versión semántica en las imágenes del sistema de mensajería garantiza que re-deployar la misma versión produzca el mismo resultado, facilitando tanto el rollback como la recuperación ante fallas del pipeline.

### Image tag (Docker)
**Categoría:** Docker / CI/CD
**Definición:** Etiqueta asociada a una imagen Docker que identifica una versión específica de esa imagen dentro de un repositorio. El tag `:latest` apunta siempre a la última versión publicada; los tags semánticos (`v1.2.0`) son inmutables y permiten rollback.
**En el proyecto:** Las imágenes del sistema de mensajería se etiquetan con el SHA del commit (para deploys de `main`) o con el tag semántico (para deploys de versiones). Nunca se usa `:latest` en producción.

---

## J

### Job
**Categoría:** GitHub Actions
**Definición:** Unidad de trabajo en un workflow de GitHub Actions que corre en un runner. Cada job es independiente por defecto (corre en paralelo con otros jobs del mismo workflow), tiene su propio filesystem y puede depender de otros jobs via `needs:`.
**En el proyecto:** Los workflows del sistema de mensajería tienen un único job principal (`build-and-test` en CI, `deploy` en CD). En configuraciones multi-ambiente se pueden tener jobs `deploy-staging` y `deploy-production` con dependencias secuenciales.

### JWT (JSON Web Token)
**Categoría:** Seguridad / Autenticación
**Definición:** Estándar abierto (RFC 7519) para transmitir claims de forma segura entre partes como un objeto JSON compacto y firmado digitalmente. En el contexto de OIDC, GitHub emite JWTs firmados que AWS STS valida para otorgar credenciales temporales.
**En el proyecto:** El token OIDC que GitHub emite para el runner del sistema de mensajería es un JWT con claims como `sub` (repositorio) y `aud` (audience). AWS STS verifica la firma y los claims antes de emitir credenciales de sesión.

---

## L

### Latencia p95
**Categoría:** Observabilidad / Métricas
**Definición:** El percentil 95 de la distribución de tiempos de respuesta de un servicio: el 95% de las solicitudes responde en ese tiempo o menos. Es una métrica más representativa que el promedio porque captura el comportamiento para casi todos los usuarios, ignorando solo los outliers extremos.
**En el proyecto:** El checklist post-deploy del sistema de mensajería incluye verificar que la latencia p95 de las operaciones críticas (login, bandeja de conversaciones) se mantenga dentro del baseline establecido antes del deploy.

### Logs
**Categoría:** Observabilidad / DevOps
**Definición:** Registros estructurados o de texto plano emitidos por una aplicación o servicio durante su ejecución, que documentan eventos, errores, advertencias y trazas de operaciones. Son la primera fuente de diagnóstico cuando algo falla.
**En el proyecto:** El sistema de mensajería usa Serilog para emitir logs estructurados que ECS captura y envía a CloudWatch Logs. El checklist post-deploy requiere revisar los logs de los primeros 15 minutos tras cada deploy.

---

## M

### main (rama)
**Categoría:** Git / CI/CD
**Definición:** Rama principal del repositorio que representa el estado actual del código en producción (o listo para producción). En la estrategia de branching del proyecto, `main` siempre debe estar en un estado deployable.
**En el proyecto:** En el sistema de mensajería, el trigger `push: branches: [main]` del `cd-oidc.yml` dispara automáticamente el deploy a cada merge aprobado. La regla de oro es: ningún merge a `main` sin CI verde.

### Merge
**Categoría:** Git / CI/CD
**Definición:** Operación que integra los cambios de una rama en otra. En el contexto de CI/CD, el merge a `main` es el evento que dispara el CD. Con branch protection activada, solo se permite el merge si el CI está en verde.
**En el proyecto:** Cada PR del sistema de mensajería debe superar el `ci.yml` antes de poder hacer merge a `main`. El merge dispara automáticamente el `cd-oidc.yml` que publica la nueva imagen en ECR y actualiza ECS.

### Migración de base de datos
**Categoría:** DevOps / Base de datos
**Definición:** Script o proceso controlado que modifica el esquema de la base de datos de forma incremental y versionada. Las migraciones reversibles son críticas en deploys, ya que permiten deshacer cambios de esquema si el deploy falla.
**En el proyecto:** El checklist pre-deploy del sistema de mensajería incluye verificar que las migraciones de EF Core pendientes sean reversibles antes de hacer merge a `main`.

### Multi-stage build (Docker)
**Categoría:** Docker
**Definición:** Técnica de Dockerfile que usa múltiples instrucciones `FROM` para separar el proceso de compilación del artefacto final. La etapa de build tiene el SDK completo; la imagen final solo contiene el runtime y los binarios compilados, resultando en imágenes más pequeñas y seguras.
**En el proyecto:** El `Dockerfile` del `back-template` del sistema de mensajería usa multi-stage build: primera etapa compila con `mcr.microsoft.com/dotnet/sdk:10.0`; segunda etapa copia solo el binario publicado al runtime `mcr.microsoft.com/dotnet/aspnet:10.0`.

---

## N

### needs (keyword)
**Categoría:** GitHub Actions
**Definición:** Propiedad de un job que declara dependencias con otros jobs del mismo workflow. El job que declara `needs: [otro-job]` no se ejecuta hasta que todos los jobs listados completen exitosamente. Si algún job dependencia falla, el job dependiente se cancela.
**En el proyecto:** En una configuración multi-ambiente del sistema de mensajería, `deploy-production` declara `needs: deploy-staging` para garantizar que la producción solo se actualiza si staging fue exitoso.

### NuGet
**Categoría:** .NET
**Definición:** Gestor de paquetes oficial del ecosistema .NET. Los paquetes se declaran en los archivos `.csproj` y se descargan mediante `dotnet restore`. En CI, los paquetes se pueden cachear entre ejecuciones para reducir el tiempo del pipeline.
**En el proyecto:** Los paquetes de las capas del sistema de mensajería (MediatR, EF Core, Serilog, etc.) se restauran en el step `dotnet restore` del `ci.yml` antes de la compilación.

---

## O

### OIDC (OpenID Connect)
**Categoría:** Seguridad / Autenticación
**Definición:** Capa de identidad construida sobre OAuth 2.0 que permite a un servicio (como AWS STS) verificar la identidad de una carga de trabajo mediante tokens firmados emitidos por un proveedor de identidad externo (como GitHub). Elimina la necesidad de credenciales de larga duración.
**En el proyecto:** El `cd-oidc.yml` del sistema de mensajería usa OIDC para que GitHub emita un token firmado que AWS STS valida contra el proveedor `token.actions.githubusercontent.com`, obteniendo credenciales temporales (por omisión de 1 hora, configurable) para el deploy.

### OIDC Provider (AWS)
**Categoría:** AWS / Seguridad
**Definición:** Configuración en una cuenta de AWS que registra un proveedor de identidad externo (en este caso `token.actions.githubusercontent.com`) como emisor confiable de tokens OIDC. Es el prerequisito en AWS para habilitar la autenticación OIDC desde GitHub Actions.
**En el proyecto:** Se configura una sola vez en la cuenta AWS de la empresa con el ARN `arn:aws:iam::ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com`, habilitando todos los repositorios que tengan trust policies apropiadas.

### On-call
**Categoría:** DevOps / SRE
**Definición:** Práctica en la que uno o más ingenieros están disponibles para responder incidentes fuera del horario laboral normal. Antes de un deploy a producción, debe confirmarse que hay alguien on-call en caso de que el deploy cause problemas.
**En el proyecto:** El checklist pre-deploy del sistema de mensajería incluye confirmar que hay un ingeniero on-call disponible antes de pushear un tag de versión que dispare el deploy a producción.

---

## P

### p95 (percentil 95)
**Categoría:** Observabilidad
**Definición:** Ver Latencia p95.
**En el proyecto:** Métrica de referencia en los checklists post-deploy del sistema de mensajería para validar que el rendimiento del API no se degradó con el nuevo deploy.

### Permission (GitHub Actions)
**Categoría:** GitHub Actions / Seguridad
**Definición:** Conjunto de privilegios que se otorgan al token de GitHub (`GITHUB_TOKEN`) durante la ejecución de un workflow. Se declaran en la sección `permissions:` del workflow y siguen el principio de mínimo privilegio. Los permisos no declarados se deniegan.
**En el proyecto:** El `cd-oidc.yml` del sistema de mensajería declara `permissions: id-token: write, contents: read`. El permiso `id-token: write` es obligatorio para la autenticación OIDC; `contents: read` permite el checkout del código.

### Pipeline
**Categoría:** CI/CD
**Definición:** Secuencia automatizada de pasos (build, test, package, deploy) que se ejecutan al detectar un cambio en el código fuente. El pipeline garantiza que el software pase por todas las validaciones necesarias antes de llegar a producción.
**En el proyecto:** El pipeline del sistema de mensajería consta de dos workflows: `ci.yml` (validación en PRs) y `cd-oidc.yml` (deploy en push a `main` o tags). Juntos garantizan que `main` siempre sea deployable.

### PR (Pull Request)
**Categoría:** Git / CI/CD
**Definición:** Mecanismo de GitHub para proponer cambios de una rama a otra. Permite revisión de código, comentarios y la ejecución de checks automáticos (CI) antes de aprobar el merge. Es el punto de control central del flujo de trabajo del equipo.
**En el proyecto:** Todos los cambios al sistema de mensajería pasan por un PR hacia `main` o `develop`. El trigger `pull_request` del `ci.yml` bloquea automáticamente el merge si el build o los tests fallan.

### Protection rules (Environment)
**Categoría:** GitHub Actions / CI/CD
**Definición:** Reglas configuradas en un environment de GitHub que añaden controles adicionales antes de que un job pueda ejecutarse en ese contexto: aprobación de reviewers, timer de espera y restricción de ramas fuente.
**En el proyecto:** El environment `production` del sistema de mensajería puede configurarse con Required reviewers (un humano aprueba antes del deploy), Wait timer (espera tras staging) y Deployment branches (solo `main` o tags `v*.*.*`).

---

## R

### Registry (Docker)
**Categoría:** Docker / CI/CD
**Definición:** Repositorio centralizado donde se almacenan y distribuyen imágenes Docker. Puede ser público (Docker Hub) o privado (Amazon ECR, GitHub Container Registry). Las imágenes se identifican por `registry/repositorio:tag`.
**En el proyecto:** Amazon ECR es el registry privado usado por el sistema de mensajería. El CD publica cada nueva imagen al registry y ECS la descarga para ejecutarla en el cluster.

### Required reviewers
**Categoría:** GitHub Actions / CI/CD
**Definición:** Protection rule de un environment de GitHub que requiere la aprobación explícita de uno o más usuarios o equipos antes de que el job que usa ese environment pueda comenzar. El pipeline queda en pausa esperando la aprobación.

### Retention days (artifacts)
**Categoría:** GitHub Actions
**Definición:** Número de días durante los cuales GitHub conserva un artifact antes de eliminarlo automáticamente. El valor máximo en repositorios públicos es 90 días; en privados depende del plan.
**En el proyecto:** Los reportes de cobertura del sistema de mensajería se guardan con `retention-days: 7`, suficiente para revisarlos en la semana siguiente al deploy sin acumular almacenamiento innecesario.

### Rollback
**Categoría:** DevOps / CI/CD
**Definición:** Proceso de revertir un sistema a una versión anterior conocida y estable cuando un deploy causa problemas. La velocidad del rollback depende de cómo estén versionadas las imágenes y de la disponibilidad de definiciones de tarea anteriores en ECS.
**En el proyecto:** En ECS, el rollback del sistema de mensajería consiste en actualizar el servicio para usar una revisión anterior de la task definition. En EC2, se detiene el contenedor actual y se lanza uno con la imagen de la versión anterior desde ECR.

### Runner
**Categoría:** GitHub Actions
**Definición:** Máquina (física o virtual) que ejecuta los jobs de un workflow de GitHub Actions. Puede ser hospedada por GitHub (`ubuntu-latest`, `windows-latest`, `macos-latest`) o self-hosted en infraestructura propia.
**En el proyecto:** Todos los workflows del sistema de mensajería usan `runs-on: ubuntu-latest`, que es un runner Linux de GitHub con 2 CPUs, 7 GB de RAM y herramientas como Docker, AWS CLI y el SDK de .NET preinstaladas.

---

## S

### Schedule (trigger)
**Categoría:** GitHub Actions
**Definición:** Trigger que ejecuta un workflow en un horario definido mediante expresiones cron (ej. `0 2 * * 1` para los lunes a las 2 AM). Útil para tareas periódicas como escaneo de vulnerabilidades, limpieza de artifacts o deploys programados.
**En el proyecto:** Aunque no está en los workflows actuales del sistema de mensajería, el trigger `schedule` puede configurarse para ejecutar análisis de seguridad o limpieza de imágenes antiguas en ECR de forma automática.

### Secrets (GitHub)
**Categoría:** GitHub Actions / Seguridad
**Definición:** Valores sensibles (tokens, passwords, ARNs, connection strings) almacenados de forma cifrada en GitHub y accesibles en los workflows como `${{ secrets.NOMBRE }}`. GitHub los enmascara automáticamente en los logs para evitar exposición accidental.
**En el proyecto:** El sistema de mensajería usa secrets para `ECR_REGISTRY`, `AWS_DEPLOY_ROLE_ARN`, `ECS_CLUSTER`, `ECS_SERVICE` y otros valores que varían por ambiente. Las connection strings de la base de datos y el JWT secret se inyectan desde AWS Secrets Manager, no desde GitHub Secrets.

### Secrets Manager (AWS)
**Categoría:** AWS / Seguridad
**Definición:** Servicio de AWS para almacenar, rotar y distribuir secretos (passwords, API keys, connection strings) de forma segura. Las aplicaciones leen sus secretos en runtime sin necesidad de tenerlos en variables de entorno o en el código.
**En el proyecto:** Las variables de entorno sensibles del sistema de mensajería (connection string de PostgreSQL, JWT secret, API keys de WhatsApp/Instagram) se almacenan en AWS Secrets Manager y se inyectan en la task definition de ECS, no en el Dockerfile ni en GitHub Secrets.

### Self-hosted runner
**Categoría:** GitHub Actions
**Definición:** Runner que se ejecuta en infraestructura propia (on-premise o en la nube) y se registra en GitHub para procesar jobs. Útil cuando se necesita hardware específico, acceso a redes privadas o costos menores en pipelines intensivos.
**En el proyecto:** No se usa en el pipeline actual del sistema de mensajería, pero puede considerarse si el build de la imagen Docker tarda demasiado en los runners de GitHub o si se necesita acceso directo a recursos en la VPC de AWS.

### SemVer (Semantic Versioning)
**Categoría:** Git / CI/CD
**Definición:** Estándar de versionado que usa el formato `MAJOR.MINOR.PATCH`. MAJOR: cambios incompatibles; MINOR: nueva funcionalidad compatible; PATCH: correcciones de bugs. Una feature nueva incrementa el número MINOR (ej. `v2.3.5` + feature -> `v2.4.0`).
**En el proyecto:** Los tags de versión del sistema de mensajería siguen SemVer (`v1.2.0`). El trigger `tags: ['v*.*.*']` del CD se dispara exactamente con estos tags, y la imagen Docker se etiqueta con el mismo número para hacer rollback posible.

### Service (ECS)
**Categoría:** AWS / ECS
**Definición:** Abstracción de ECS que gestiona el ciclo de vida de un conjunto de tareas basadas en una task definition. Garantiza que siempre haya el número deseado de tareas corriendo, reemplazando las que fallen y gestionando los deploys rolling.
**En el proyecto:** El servicio `mensajeria-service` en ECS mantiene en ejecución las instancias del API del sistema de mensajería. El CD actualiza este servicio con `aws-actions/amazon-ecs-deploy-task-definition@v1`.

### SHA (commit SHA)
**Categoría:** Git / CI/CD
**Definición:** Hash SHA-1 o SHA-256 que identifica de forma única un commit en git. En GitHub Actions está disponible como `${{ github.sha }}`. Usar el SHA como tag de imagen Docker garantiza trazabilidad completa entre código fuente e imagen desplegada.
**En el proyecto:** Cuando el CD del sistema de mensajería se dispara por push a `main` (sin tag de versión), la imagen Docker se etiqueta con el SHA del commit, permitiendo identificar exactamente qué código está corriendo en ECS.

### Soak time / Wait timer
**Categoría:** DevOps / CI/CD
**Definición:** Período de espera configurado en un environment de GitHub después de que el deploy a staging es exitoso, antes de iniciar el deploy a producción. Da tiempo para detectar problemas que solo aparecen bajo carga real.
**En el proyecto:** El environment `production` del sistema de mensajería puede configurarse con un Wait timer de 30 minutos tras el deploy a staging, para validar que el sistema es estable antes de actualizar producción.

### SonarCloud
**Categoría:** CI/CD / Calidad de código
**Definición:** Plataforma de análisis estático de código en la nube que detecta bugs, vulnerabilidades, code smells y calcula métricas de cobertura. Se integra con GitHub Actions y puede bloquear PRs si la calidad cae por debajo de umbrales definidos.
**En el proyecto:** Se menciona en el contexto de los artifacts de cobertura del sistema de mensajería. Los reportes generados por `dotnet test --collect:"XPlat Code Coverage"` pueden enviarse a SonarCloud para análisis continuo de calidad.

### Step
**Categoría:** GitHub Actions
**Definición:** Unidad mínima de ejecución dentro de un job. Puede ser un comando shell (`run:`) o una action reutilizable (`uses:`). Los steps de un job comparten el mismo filesystem y se ejecutan secuencialmente. Si un step falla, los siguientes no se ejecutan.
**En el proyecto:** El `ci.yml` del sistema de mensajería tiene 5 steps: checkout, setup-dotnet, restore, build y test. El `cd-oidc.yml` añade steps para autenticación OIDC, login a ECR, etiquetado de imagen, build/push y deploy a ECS.

### STS (AWS Security Token Service)
**Categoría:** AWS / Seguridad
**Definición:** Servicio de AWS que emite credenciales de seguridad temporales para usuarios o cargas de trabajo autenticadas. Las credenciales tienen una duración limitada (por defecto 1 hora, configurable) y se usan igual que las credenciales de larga duración.
**En el proyecto:** En el flujo OIDC del sistema de mensajería, GitHub presenta el token OIDC a AWS STS, que lo valida y emite credenciales temporales (por omisión de 1 hora, configurables desde 15 minutos hasta la duración máxima del rol, según la referencia de AWS STS para AssumeRoleWithWebIdentity: https://docs.aws.amazon.com/STS/latest/APIReference/API_AssumeRoleWithWebIdentity.html) para que el runner pueda hacer push a ECR y actualizar ECS.

---

## T

### Tag (git)
**Categoría:** Git / CI/CD
**Definición:** Referencia estática que apunta a un commit específico en git, usada para marcar versiones de lanzamiento. A diferencia de las ramas, los tags no avanzan. Los tags anotados incluyen metadata (autor, fecha, mensaje).
**En el proyecto:** El deploy de versiones del sistema de mensajería se dispara con `git tag v1.2.0 && git push --tags`. El workflow CD detecta el patrón `v*.*.*` y usa el tag como nombre de la imagen Docker en ECR.

### Task definition (ECS)
**Categoría:** AWS / ECS
**Definición:** Especificación JSON que describe cómo ejecutar uno o más contenedores en ECS: imagen Docker, CPU, memoria, variables de entorno, volúmenes, logging y networking. Cada actualización genera una nueva revisión numerada; las revisiones anteriores se preservan para rollback.
**En el proyecto:** La task definition `mensajeria-task` especifica la imagen del sistema de mensajería (referenciada desde ECR), los límites de recursos y las variables de entorno. Cada deploy del CD genera una nueva revisión con la imagen actualizada.

### Trigger (`on:`)
**Categoría:** GitHub Actions
**Definición:** Sección del workflow YAML que define los eventos de GitHub que disparan la ejecución del pipeline. Los triggers más comunes son `push`, `pull_request`, `schedule` y `workflow_dispatch`. Se pueden filtrar por rama, tag o ruta de archivo.
**En el proyecto:** El `ci.yml` del sistema de mensajería usa `pull_request` (corre en cada PR); el `cd-oidc.yml` usa `push` filtrado por `branches: [main]` y `tags: ['v*.*.*']` (corre solo en merges a main o releases).

### Trust policy (IAM)
**Categoría:** AWS / IAM / Seguridad
**Definición:** Política JSON asociada a un IAM Role que define quién (qué principal) puede asumir ese rol y bajo qué condiciones. En el contexto de OIDC, la trust policy especifica el proveedor de identidad y los claims que deben coincidir para que el rol sea asumible.
**En el proyecto:** El rol `github-actions-deploy-role` del sistema de mensajería tiene una trust policy que permite solo al repo `acme/mensajeria` asumir el rol. El claim `sub` con `StringLike` asegura que ningún otro repositorio pueda obtener permisos de deploy.

---

## W

### wait-for-service-stability
**Categoría:** GitHub Actions / AWS / ECS
**Definición:** Parámetro de la action `amazon-ecs-deploy-task-definition` que hace que el step espere activamente hasta que ECS confirme que el servicio alcanzó el estado estable (todas las tareas nuevas pasaron health checks y las antiguas fueron drenadas). Si el servicio no estabiliza, el step falla.
**En el proyecto:** Se usa en el CD del sistema de mensajería con `wait-for-service-stability: true`. Si los contenedores nuevos no pasan los health checks, el pipeline falla y alerta al equipo antes de que el tráfico de usuarios sea afectado.

### workflow
**Categoría:** GitHub Actions
**Definición:** Archivo YAML ubicado en `.github/workflows/` que define una automatización completa: triggers, variables de entorno, jobs y steps. Un repositorio puede tener múltiples workflows independientes que se activan con diferentes eventos.
**En el proyecto:** El sistema de mensajería tiene dos workflows: `ci.yml` (validación de PRs) y `cd-oidc.yml` (deploy). Esta separación permite que CI corra en cada PR sin disparar un deploy, y que el deploy solo ocurra en merges aprobados o releases.

### workflow_dispatch
**Categoría:** GitHub Actions
**Definición:** Trigger que permite ejecutar un workflow manualmente desde la interfaz de GitHub (pestaña Actions -> Run workflow) o via API. Puede aceptar inputs opcionales para parametrizar la ejecución.
**En el proyecto:** Puede agregarse al `cd-oidc.yml` del sistema de mensajería para permitir re-ejecutar un deploy manualmente sin hacer un nuevo commit, útil para rollbacks o re-deploys de emergencia.

---

## Y

### YAML
**Categoría:** CI/CD / Configuración
**Definición:** Formato de serialización de datos legible por humanos usado por GitHub Actions (y muchas otras herramientas DevOps) para definir configuraciones. Es sensible a la indentación y no permite tabs. Los archivos de workflow de GitHub Actions son archivos YAML.
**En el proyecto:** Todos los workflows del sistema de mensajería (`ci.yml`, `cd-oidc.yml`) son archivos YAML. Los errores de indentación en YAML son errores de sintaxis que impiden que el workflow se ejecute.

---

*Generado para el Dev Notes  -  Rogelio Arriaga González, Raptor Dev Services  -  2026*
