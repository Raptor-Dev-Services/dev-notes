# Glosario Técnico  -  Git  -  Git Flow  -  SemVer

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### alpha
**Categoría:** SemVer
**Definición:** Etiqueta de pre-release que indica desarrollo interno inestable. La API puede cambiar en cualquier commit. Se usa cuando la funcionalidad aún no está completa y no debe distribuirse externamente.
**En el proyecto:** El sistema de mensajería usará `0.1.0-alpha` durante las primeras iteraciones antes de la primera entrega funcional.

### annotated tag (tag anotado)
**Categoría:** Git
**Definición:** Objeto Git completo que almacena autor, fecha, mensaje y puede ser firmado con GPG. A diferencia del tag ligero, es un objeto de primera clase en el repositorio. Es el estándar para releases oficiales.
**En el proyecto:** Cada release del sistema de mensajería se marca con `git tag -a v1.3.0 -m "Release 1.3.0"` después del merge a `main`.

---

## B

### beta
**Categoría:** SemVer
**Definición:** Etiqueta de pre-release que indica que la funcionalidad está completa pero en fase de pruebas externas. Más estable que alpha pero sin garantía de compatibilidad de API final.
**En el proyecto:** Antes de la primera entrega pública del sistema de mensajería, el equipo puede distribuir una `1.0.0-beta` al cliente para revisión.

### branch (rama)
**Categoría:** Git
**Definición:** Puntero ligero y móvil a un commit específico. Permite trabajar en paralelo sobre el mismo repositorio sin interferir entre desarrolladores. Cada commit avanza el puntero de la rama activa automáticamente.
**En el proyecto:** El equipo mantiene al menos `main` y `develop` como ramas permanentes, más las ramas temporales de features, bugfixes, releases y hotfixes.

### branch protection
**Categoría:** Git / Proceso
**Definición:** Configuración en la plataforma de hosting (GitHub, Azure DevOps) que impide operaciones destructivas sobre ramas críticas: push directo, force push, eliminación. Requiere PR y revisiones aprobadas para fusionar.
**En el proyecto:** `main` y `develop` del sistema de mensajería deben tener branch protection activada desde el día 1 para prevenir accidentes.

### breaking change
**Categoría:** SemVer / Git
**Definición:** Cambio en la API pública que rompe la compatibilidad con versiones anteriores: renombrar endpoints, cambiar el tipo de un campo, eliminar parámetros requeridos. Exige incrementar el segmento MAJOR de SemVer.
**En el proyecto:** Ejemplo real del slides: reemplazar `agentId` por `workspaceUserId` en todos los endpoints es un breaking change -> versión sube de `1.x.x` a `2.0.0`.

### bugfix branch
**Categoría:** Git Flow
**Definición:** Rama temporal creada desde `develop` para corregir un bug encontrado durante el desarrollo normal (no en producción). Sigue el patrón `bugfix/PROY-NNN-descripcion`. Se integra de vuelta a `develop` con `--no-ff`.
**En el proyecto:** Ejemplo: `bugfix/PROY-312-fix-email-validation` para corregir la validación de email en el módulo de agentes antes de incluirla en la siguiente release.

---

## C

### CD (Continuous Delivery / Deployment)
**Categoría:** Proceso
**Definición:** Pipeline automatizado que toma el código que pasó CI y lo despliega a un ambiente (staging, producción). El tag de versión es la señal que dispara el pipeline de CD en proyectos con Git Flow.
**En el proyecto:** El pipeline de GitHub Actions del sistema de mensajería se dispara con `git push origin v1.x.x` hacia el ambiente de staging en AWS.

### cherry-pick
**Categoría:** Git
**Definición:** Operación que copia un commit específico de una rama a otra sin fusionar toda la rama. Útil para aplicar un fix puntual sin traer todo el historial de la rama origen.
**En el proyecto:** Si se necesita aplicar un fix de seguridad a `develop` sin esperar el hotfix completo, se puede cherry-pick el commit específico.

### chore
**Categoría:** Git / Conventional Commits
**Definición:** Tipo de commit para tareas de mantenimiento que no afectan la funcionalidad ni el código de producción: actualizar dependencias, configurar herramientas, modificar archivos de configuración. No produce bump en SemVer.
**En el proyecto:** `chore(release): bump version to 1.3.0` es el commit estándar dentro de una release branch del sistema de mensajería.

### ci (tipo de commit)
**Categoría:** Git / Conventional Commits
**Definición:** Tipo de commit para cambios en pipelines de integración continua o entrega continua. No afecta el código de producción ni produce bump en SemVer.
**En el proyecto:** `ci: add GitHub Actions workflow for staging deploy` al configurar el pipeline de despliegue del sistema de mensajería.

### commit
**Categoría:** Git
**Definición:** Snapshot permanente del estado del repositorio en un momento dado. Cada commit tiene un SHA único, apunta a su commit padre, y almacena autor, fecha y mensaje. Es la unidad atómica del historial de Git.
**En el proyecto:** Cada commit del sistema de mensajería debe seguir Conventional Commits para que el historial sea legible y herramientas como semantic-release puedan calcular versiones automáticamente.

### Conventional Commits
**Categoría:** Git / Proceso
**Definición:** Especificación que define un formato estructurado para mensajes de commit: `<tipo>(<alcance>): <descripción>`. Permite automatizar changelogs, calcular versiones SemVer y generar releases. Ver https://www.conventionalcommits.org.
**En el proyecto:** Todos los commits del sistema de mensajería siguen esta convención. El tipo `feat` sube MINOR, `fix` sube PATCH, `feat!` o `BREAKING CHANGE` sube MAJOR.

---

## D

### describe (git describe)
**Categoría:** Git
**Definición:** Comando que genera una versión legible basada en el tag más cercano en el historial: `v1.3.0-4-g9e2b1a0` (tag + commits después + SHA). Útil para generar números de build automáticos en CI.
**En el proyecto:** El pipeline de CI del sistema de mensajería puede usar `git describe --tags` para etiquetar artefactos de build con versión precisa.

### develop
**Categoría:** Git Flow
**Definición:** Rama permanente que representa el estado más reciente del código con todas las features integradas. Es la rama base para crear feature branches y release branches. Nunca se hace push directo: solo recibe merges con `--no-ff`.
**En el proyecto:** `develop` es la rama de integración del sistema de mensajería. Todo feature que pasa code review se integra aquí antes de pasar a una release branch.

### diff
**Categoría:** Git
**Definición:** Vista de las diferencias línea a línea entre dos estados del repositorio. `git diff` muestra cambios sin stagear; `git diff --staged` muestra los que ya están en el staging area esperando el próximo commit.
**En el proyecto:** Revisar el diff antes de cada commit es obligatorio en el equipo para no incluir código de depuración o cambios no intencionales.

### docs (tipo de commit)
**Categoría:** Git / Conventional Commits
**Definición:** Tipo de commit para cambios exclusivamente en documentación: README, comentarios de código, archivos .md. No produce bump en SemVer.
**En el proyecto:** `docs(auth): update JWT refresh token flow diagram` para actualizar la documentación del módulo de autenticación del sistema de mensajería.

---

## E

### etiqueta de pre-release
**Categoría:** SemVer
**Definición:** Sufijo que se agrega después de un guion en la versión para indicar que no es un release estable: `alpha`, `beta`, `rc.1`. Define precedencia de versiones: alpha < beta < rc < release oficial.
**En el proyecto:** Antes de entregar el módulo de contactos al cliente, el equipo puede publicar `1.2.0-rc.1` para validación final.

---

## F

### fast-forward
**Categoría:** Git
**Definición:** Tipo de merge en que Git simplemente mueve el puntero de la rama destino hasta el último commit de la rama origen, sin crear un merge commit. Solo es posible cuando no hay divergencia. Produce historial lineal pero sin trazabilidad de features.
**En el proyecto:** El equipo evita fast-forward en `develop` y `main` configurando `git config --global merge.ff false`, para preservar la trazabilidad de cada feature del sistema de mensajería.

### feat (tipo de commit)
**Categoría:** Git / Conventional Commits
**Definición:** Tipo de commit que introduce una nueva funcionalidad en el producto. Dispara incremento del segmento MINOR en SemVer. Es el tipo más común en desarrollo activo de producto.
**En el proyecto:** `feat(channels): add Instagram webhook integration` al implementar el canal de Instagram en el sistema de mensajería.

### feature branch
**Categoría:** Git Flow
**Definición:** Rama temporal creada desde `develop` para desarrollar una funcionalidad específica o resolver un ticket. Sigue el patrón `feature/PROY-NNN-descripcion`. Se integra a `develop` cuando pasa code review.
**En el proyecto:** Cada ticket del sistema de mensajería tiene su propia feature branch: `feature/PROY-101-whatsapp-channel`, `feature/PROY-215-contact-import-csv`.

### fetch (git fetch)
**Categoría:** Git
**Definición:** Descarga los cambios del repositorio remoto al repositorio local sin aplicarlos al working directory ni al branch actual. Es más seguro que `git pull` porque permite revisar los cambios antes de integrarlos.
**En el proyecto:** El equipo debe hacer `git fetch origin` antes de crear cualquier rama nueva para tener las referencias remotas actualizadas.

### fix (tipo de commit)
**Categoría:** Git / Conventional Commits
**Definición:** Tipo de commit que corrige un bug en el código de producción. Dispara incremento del segmento PATCH en SemVer. Se usa tanto en bugfix branches como en hotfix branches.
**En el proyecto:** `fix(auth): prevent token refresh loop on 401` para corregir el comportamiento del interceptor HTTP en el frontend del sistema de mensajería.

### force push (git push --force)
**Categoría:** Git
**Definición:** Operación que sobreescribe el historial remoto con el historial local, descartando commits que otros desarrolladores podrían tener. Es destructiva e irreversible si no hay respaldo. Está prohibida en ramas principales.
**En el proyecto:** `git push --force` a `main` o `develop` está absolutamente prohibido en el sistema de mensajería. Las ramas deben tener branch protection que lo bloquee.

### follow-tags
**Categoría:** Git
**Definición:** Opción de `git push` (`--follow-tags`) que envía automáticamente al remoto todos los tags anotados accesibles desde los commits que se están publicando. Evita olvidar el push manual de tags.
**En el proyecto:** Alternativa al push explícito de tag: `git push origin --follow-tags` después del merge de release a `main` en el sistema de mensajería.

---

## G

### Git
**Categoría:** Git
**Definición:** Sistema de control de versiones distribuido creado por Linus Torvalds en 2005. Cada copia del repositorio contiene el historial completo. Permite trabajar offline y fusionar cambios de múltiples colaboradores de forma controlada.
**En el proyecto:** El repositorio del sistema de mensajería vive en GitHub y es la fuente de verdad única para todo el equipo.

### git add
**Categoría:** Git
**Definición:** Comando que mueve cambios del working directory al staging area (index). Se puede stagear por archivo, carpeta o fragmento (`-p`). Buena práctica: `git add src/Application/` en lugar de `git add .`.
**En el proyecto:** El equipo debe stagear por carpeta o archivo para evitar incluir accidentalmente archivos de configuración local o secretos en commits del sistema de mensajería.

### git log
**Categoría:** Git
**Definición:** Comando que muestra el historial de commits del repositorio. Con opciones `--oneline --graph --decorate` produce una vista visual compacta del árbol de ramas y tags, ideal para entender el estado del repositorio.
**En el proyecto:** `git log --oneline --graph --decorate` es el comando estándar para revisar el historial del sistema de mensajería antes de crear una release.

### Git Flow
**Categoría:** Git Flow
**Definición:** Modelo de branching propuesto por Vincent Driessen en 2010. Define ramas permanentes (`main`, `develop`) y temporales (`feature/*`, `bugfix/*`, `release/*`, `hotfix/*`) con reglas claras de integración y merge strategy.
**En el proyecto:** El sistema de mensajería usa Git Flow desde el primer commit para mantener trazabilidad, facilitar releases y permitir hotfixes sin interrumpir el desarrollo normal.

### git pull
**Categoría:** Git
**Definición:** Combinación de `git fetch` + `git merge` (o `git rebase` con `--rebase`). Descarga y aplica los cambios remotos a la rama actual. Sin opciones usa fast-forward o merge según configuración.
**En el proyecto:** `git pull origin develop` es el primer comando antes de crear cualquier feature branch en el sistema de mensajería.

### git revert
**Categoría:** Git
**Definición:** Crea un nuevo commit que deshace los cambios de un commit anterior, sin modificar el historial existente. Es la forma segura de deshacer cambios en ramas compartidas. Con `-m 1` deshace un merge commit.
**En el proyecto:** `git revert -m 1 <sha-merge-commit>` para revertir una feature completa en `develop` del sistema de mensajería sin reescribir historial.

### git show
**Categoría:** Git
**Definición:** Muestra el detalle completo de un objeto Git: en el caso de commits, muestra metadatos (autor, fecha, mensaje) más el diff completo de los cambios introducidos.
**En el proyecto:** `git show <sha>` es útil durante code review para inspeccionar un commit específico del sistema de mensajería sin salir de la terminal.

### git status
**Categoría:** Git
**Definición:** Muestra el estado actual del working directory y el staging area: archivos modificados, archivos en staging, archivos sin trackear. Es el comando más básico y el primero que se debe ejecutar antes de cualquier operación.
**En el proyecto:** Regla del equipo: nunca hacer `git add` sin revisar `git status` primero para evitar incluir archivos no deseados en commits del sistema de mensajería.

---

## H

### HEAD
**Categoría:** Git
**Definición:** Referencia simbólica que apunta al commit actual del working directory, normalmente a través de una rama. Siempre indica "dónde estás parado" en el historial. Se mueve automáticamente con cada nuevo commit.
**En el proyecto:** `HEAD` apunta a `develop` durante el desarrollo normal del sistema de mensajería. Al crear una feature branch, `HEAD` se mueve a esa rama.

### HEAD detached
**Categoría:** Git
**Definición:** Estado en que `HEAD` apunta directamente a un SHA o tag en lugar de a una rama. Los commits nuevos en este estado no pertenecen a ninguna rama y se perderán al cambiar de rama. Se resuelve creando una rama nueva.
**En el proyecto:** Ocurre al hacer `git checkout v1.2.0` para inspeccionar esa versión. Antes de trabajar, crear rama: `git checkout -b hotfix/1.2.1-fix-crash v1.2.0`.

### HMAC
**Categoría:** Proceso
**Definición:** Hash-based Message Authentication Code. Algoritmo criptográfico para verificar la integridad y autenticidad de un mensaje usando una clave secreta compartida. Los proveedores de webhooks (WhatsApp, Instagram) firman sus payloads con HMAC.
**En el proyecto:** El handler de webhooks del canal WhatsApp en el sistema de mensajería valida la firma HMAC antes de procesar cualquier mensaje recibido.

### hotfix branch
**Categoría:** Git Flow
**Definición:** Rama temporal creada desde `main` para corregir un bug crítico en producción que no puede esperar al ciclo normal de release. Sigue el patrón `hotfix/{MAJOR}.{MINOR}.{PATCH}-descripcion`. Se integra a `main` Y a `develop`.
**En el proyecto:** `hotfix/1.2.1-login-null-token` para corregir un crash de producción en el módulo de autenticación del sistema de mensajería sin interrumpir el desarrollo activo en `develop`.

---

## I

### index (índice)
**Categoría:** Git
**Definición:** Nombre técnico del staging area. Es el archivo `.git/index` que almacena el snapshot de lo que irá en el próximo commit. `git add` escribe en el index; `git commit` lo convierte en un objeto tree permanente.
**En el proyecto:** Sinónimo de staging area en el contexto del flujo diario del equipo Sistema de mensajería.

---

## L

### lightweight tag (tag ligero)
**Categoría:** Git
**Definición:** Puntero simple a un commit sin metadatos adicionales. No almacena autor, fecha ni mensaje. Útil para marcadores temporales o locales, pero no debe usarse para releases oficiales.
**En el proyecto:** El equipo no usa tags ligeros para releases del sistema de mensajería. Solo tags anotados con `git tag -a` son válidos como releases oficiales.

---

## M

### main
**Categoría:** Git Flow
**Definición:** Rama permanente que representa el código actualmente en producción. Solo recibe merges de `release/*` y `hotfix/*`. Cada commit en `main` debe corresponder a un tag de versión SemVer. Nunca se hace push directo.
**En el proyecto:** `main` del sistema de mensajería refleja exactamente lo que está desplegado en el ambiente productivo de la empresa. Cada merge va acompañado de su tag `v{version}`.

### MAJOR
**Categoría:** SemVer
**Definición:** Primer segmento de la versión semántica. Se incrementa cuando se introduce un breaking change que rompe la compatibilidad con la API pública anterior. Al incrementar MAJOR, MINOR y PATCH se resetean a 0.
**En el proyecto:** Si el equipo renombra el campo `agentId` a `workspaceUserId` en la API del sistema de mensajería, la versión pasa de `1.x.x` a `2.0.0`.

### merge
**Categoría:** Git
**Definición:** Operación que integra el historial de dos ramas. Existen tres estrategias principales: fast-forward (lineal), merge commit con `--no-ff` (bifurcado con commit explícito) y squash (aplana todos los commits en uno solo).
**En el proyecto:** Git Flow en el sistema de mensajería usa `--no-ff` para todas las integraciones en ramas permanentes, preservando la trazabilidad de cada feature y hotfix.

### merge commit
**Categoría:** Git
**Definición:** Commit especial generado por `git merge --no-ff` que tiene dos padres: el último commit de cada rama fusionada. Preserva la historia de la bifurcación y permite revertir toda la feature con un solo `git revert -m 1`.
**En el proyecto:** Cada feature integrada a `develop` del sistema de mensajería genera un merge commit con el mensaje `Merge branch 'feature/PROY-NNN-...' into develop`.

### merge conflict
**Categoría:** Git
**Definición:** Situación en que dos ramas modificaron la misma línea o zona de un archivo de formas incompatibles. Git no puede resolver el conflicto automáticamente y requiere intervención manual. Se resuelve editando los marcadores `<<<<<<<`, `=======`, `>>>>>>>`.
**En el proyecto:** Mantener features pequeñas y hacer `git pull origin develop` frecuentemente reduce los conflictos al integrar en el sistema de mensajería.

### MINOR
**Categoría:** SemVer
**Definición:** Segundo segmento de la versión semántica. Se incrementa cuando se agrega nueva funcionalidad compatible con versiones anteriores (sin romper nada existente). Al incrementar MINOR, PATCH se resetea a 0.
**En el proyecto:** Error clásico: agregar el canal WhatsApp (nueva feature) y subir solo PATCH en lugar de MINOR. Correcto: `v1.2.5` + WhatsApp -> `v1.3.0`.

---

## N

### no-ff (--no-ff)
**Categoría:** Git
**Definición:** Opción de `git merge` que fuerza la creación de un merge commit incluso cuando el fast-forward sería posible. Preserva la trazabilidad de la rama fusionada en el historial. Es el estándar de Git Flow.
**En el proyecto:** Configuración recomendada para el repo del sistema de mensajería: `git config --global merge.ff false` para que `--no-ff` sea el comportamiento por defecto.

---

## O

### oneline (--oneline)
**Categoría:** Git
**Definición:** Opción de `git log` que comprime cada commit en una sola línea mostrando solo el SHA corto y el mensaje. Combinado con `--graph` y `--decorate` produce la vista más útil del historial.
**En el proyecto:** `git log --oneline --graph --decorate` es el comando estándar para revisar el estado del historial del sistema de mensajería antes de una release.

### origin
**Categoría:** Git
**Definición:** Nombre convencional del repositorio remoto principal, establecido automáticamente por `git clone`. Es un alias para la URL del remoto. Los comandos `git push origin` y `git fetch origin` operan contra este remoto.
**En el proyecto:** `origin` apunta al repositorio del sistema de mensajería en GitHub. El equipo hace push de sus feature branches a `origin` para abrir Pull Requests.

---

## P

### PATCH
**Categoría:** SemVer
**Definición:** Tercer segmento de la versión semántica. Se incrementa cuando se corrige un bug de forma compatible con versiones anteriores (sin agregar funcionalidad ni romper nada). Es el segmento que incrementan los hotfixes.
**En el proyecto:** `hotfix/1.2.1-login-null-token` produce el tag `v1.2.1` en el sistema de mensajería  -  corrección del bug de null token sin nueva funcionalidad.

### PR (Pull Request)
**Categoría:** Git / Proceso
**Definición:** Mecanismo de las plataformas de hosting (GitHub, Azure DevOps) para proponer la integración de una rama a otra. Permite code review, comentarios inline, checks automatizados y aprobación formal antes del merge.

### pre-release
**Categoría:** SemVer
**Definición:** Versión que precede a la release oficial, indicada con un sufijo separado por guion: `-alpha`, `-beta`, `-rc.1`. Tiene menor precedencia que la versión base correspondiente: `1.0.0-rc.1 < 1.0.0`.
**En el proyecto:** El sistema de mensajería puede usar pre-releases para entregas intermedias al cliente antes de la release oficial de cada módulo.

### precedencia de versiones
**Categoría:** SemVer
**Definición:** Orden definido por SemVer para comparar versiones: se comparan MAJOR, MINOR y PATCH de izquierda a derecha numéricamente. Las pre-releases tienen menor precedencia que la versión base. Ejemplo: `1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-beta < 1.0.0-rc.1 < 1.0.0`.
**En el proyecto:** El pipeline de CD del sistema de mensajería puede usar precedencia SemVer para determinar automáticamente si un deploy es upgrade o rollback.

---

## R

### rc (release candidate)
**Categoría:** SemVer
**Definición:** Versión candidata a release. Indica que la funcionalidad está completa y se considera lista para producción, salvo que se encuentre algún bug crítico en las pruebas finales. Se etiqueta como `-rc.1`, `-rc.2`, etc.
**En el proyecto:** `1.5.0-rc.1` en el sistema de mensajería indica que la versión está lista para revisión final del cliente antes del deploy a producción.

### rebase
**Categoría:** Git
**Definición:** Operación que reescribe el historial de una rama cambiando el commit base. Aplica los commits de la rama sobre la punta de la rama destino, generando nuevos SHAs. Produce historial lineal pero es destructivo en ramas publicadas.
**En el proyecto:** Uso seguro en el sistema de mensajería: `git rebase develop` de manera local en una feature branch antes de abrir la PR, nunca después de hacer push de la rama.

### refactor (tipo de commit)
**Categoría:** Git / Conventional Commits
**Definición:** Tipo de commit para reestructuración interna del código sin cambiar su comportamiento observable desde el exterior. No agrega funcionalidad ni corrige bugs. No produce bump en SemVer.
**En el proyecto:** `refactor(conversations): extract assignment logic to domain service` para mejorar la organización del código del sistema de mensajería sin cambiar su comportamiento.

### release branch
**Categoría:** Git Flow
**Definición:** Rama temporal creada desde `develop` cuando la funcionalidad de la próxima versión está completa. Solo acepta fixes menores y el bump de versión. Al finalizarse se integra a `main` (con tag) y a `develop`.
**En el proyecto:** `release/1.3.0` se crea cuando los módulos de WhatsApp e Instagram están listos en `develop`. Se usa para estabilizar antes del deploy oficial del sistema de mensajería.

### remote (remoto)
**Categoría:** Git
**Definición:** Versión del repositorio alojada en un servidor externo (GitHub, Azure DevOps, GitLab). `git remote -v` lista los remotos configurados. El principal suele llamarse `origin`.
**En el proyecto:** El remoto `origin` del sistema de mensajería en GitHub es la fuente de verdad compartida. Todos los merges de ramas permanentes deben reflejarse en el remoto.

### repository (.git)
**Categoría:** Git
**Definición:** El directorio oculto `.git` en la raíz del proyecto. Contiene el historial completo de commits, referencias a ramas y tags, configuración, y todos los objetos Git (blobs, trees, commits, tags). Eliminar esta carpeta destruye todo el historial local.
**En el proyecto:** El repositorio del sistema de mensajería vive en GitHub. La carpeta `.git` local es la copia completa del historial para cada miembro del equipo.

### rollback
**Categoría:** Git / Proceso
**Definición:** Proceso de volver a una versión anterior estable del software después de detectar un problema en producción. En Git Flow se logra con `git revert` del merge commit o desplegando el tag de la versión anterior.
**En el proyecto:** Si `v1.3.0` del sistema de mensajería tiene un bug crítico, el equipo puede redeployar `v1.2.1` inmediatamente mientras prepara `v1.3.1` con el fix. Esto solo es posible con tags SemVer correctos (nunca `:latest`).

---

## S

### SemVer (Semantic Versioning)
**Categoría:** SemVer
**Definición:** Especificación para asignar versiones de software con formato `MAJOR.MINOR.PATCH`. Define reglas precisas sobre cuándo incrementar cada segmento basándose en el tipo de cambio. Permite a consumidores predecir el impacto de una actualización. Ver https://semver.org.
**En el proyecto:** El sistema de mensajería sigue SemVer desde `0.1.0` inicial hasta `1.0.0` en la primera entrega estable, incrementando MINOR por features y PATCH por hotfixes.

### SHA (SHA-1 / SHA-256)
**Categoría:** Git
**Definición:** Identificador hash único de 40 caracteres (SHA-1) que Git asigna a cada objeto: blobs, trees, commits y tags. Garantiza la integridad del historial  -  cualquier modificación produce un SHA diferente. Git usa los primeros 7-8 caracteres como abreviación.
**En el proyecto:** Los merge commits del sistema de mensajería se identifican por su SHA en el historial: `git revert -m 1 a4f3c21` usa el SHA corto del merge commit.

### squash merge
**Categoría:** Git
**Definición:** Estrategia de merge que combina todos los commits de una rama en un único commit nuevo en la rama destino. Produce historial limpio pero pierde la granularidad de los commits individuales y la referencia a la rama origen.
**En el proyecto:** Se usa en el sistema de mensajería cuando una feature branch tiene commits de trabajo sucio ("wip", "fix typo"). El commit squash resultante debe tener un mensaje Conventional Commits limpio.

### staging area
**Categoría:** Git
**Definición:** Zona intermedia entre el working directory y el repositorio. Los cambios agregados con `git add` se acumulan aquí hasta el próximo `git commit`. Permite seleccionar exactamente qué cambios incluir en cada commit de forma granular.
**En el proyecto:** El equipo del sistema de mensajería usa el staging area para hacer commits atómicos: solo los cambios de una funcionalidad específica, no "todo lo que estaba pendiente".

### stash
**Categoría:** Git
**Definición:** Área temporal donde Git almacena cambios del working directory y staging area sin crear un commit. `git stash push` guarda los cambios; `git stash pop` los restaura. Útil para cambiar de contexto rápidamente sin commitear trabajo incompleto.
**En el proyecto:** Cuando llega un hotfix urgente mientras se trabaja en una feature del sistema de mensajería, `git stash push` guarda el trabajo en progreso para retomarlo después.

---

## T

### tag
**Categoría:** Git
**Definición:** Referencia fija a un commit específico, a diferencia de las ramas que son móviles. Existen dos tipos: ligeros (solo un puntero) y anotados (objeto Git completo con metadatos). Los tags de release nunca deben moverse ni eliminarse.
**En el proyecto:** Cada release del sistema de mensajería tiene un tag anotado: `v1.0.0`, `v1.1.0`, `v1.1.1`, etc. Los pipelines de CD usan estos tags para desplegar versiones exactas.

### test (tipo de commit)
**Categoría:** Git / Conventional Commits
**Definición:** Tipo de commit para agregar, modificar o corregir pruebas automáticas sin afectar el código de producción. No produce bump en SemVer.
**En el proyecto:** `test(auth): add integration tests for JWT refresh token endpoint` al agregar cobertura de tests al módulo de autenticación del sistema de mensajería.

### ticket
**Categoría:** Proceso
**Definición:** Unidad de trabajo rastreable en un sistema de gestión de proyectos (Jira, Linear, GitHub Issues). El número de ticket se incluye en el nombre de la rama y puede referenciarse en el pie del commit para trazabilidad bidireccional.
**En el proyecto:** Todos los branches del sistema de mensajería incluyen el ticket: `feature/PROY-101-whatsapp-channel`. El prefijo `PROY` identifica el proyecto en el sistema de tracking de la empresa.

### tracking branch
**Categoría:** Git
**Definición:** Rama local configurada para hacer seguimiento de una rama remota específica. Permite usar `git pull` y `git push` sin especificar el remoto y la rama. Se configura con `git push -u origin <rama>`.
**En el proyecto:** Las ramas `main` y `develop` del sistema de mensajería están configuradas como tracking branches de `origin/main` y `origin/develop` respectivamente.

---

## V

### version 0.x.y
**Categoría:** SemVer
**Definición:** Fase de desarrollo inicial donde la API pública es inestable y cualquier versión puede introducir cambios incompatibles. La versión `1.0.0` marca el primer release público con API estable.
**En el proyecto:** El sistema de mensajería inicia en `0.1.0` durante las primeras sesiones de desarrollo. La primera entrega funcional con auth + workspace completos marcará la `1.0.0`.

### version bump
**Categoría:** SemVer / Proceso
**Definición:** Acción de incrementar el número de versión en los archivos de configuración del proyecto (`.csproj`, `package.json`) antes de crear la release. En Git Flow, ocurre dentro de la release branch con un commit de tipo `chore(release)`.
**En el proyecto:** El bump de versión en el sistema de mensajería se hace en `release/1.3.0` con el commit `chore(release): bump version to 1.3.0` y se refleja en el archivo `{{ROOT_NAMESPACE}}.Admin.WebClient.csproj`.

---

## W

### working directory
**Categoría:** Git
**Definición:** El directorio del sistema de archivos donde viven los archivos del proyecto tal como los ves en el editor. Es el primer estado en el flujo de Git: working directory -> staging area -> repository. Los cambios aquí son locales hasta hacer `git add`.
**En el proyecto:** Los archivos del sistema de mensajería que edita el equipo en su editor viven en el working directory. Hasta hacer `git add`, esos cambios son invisibles para Git y para los demás.

---

## Z

### zero-trust en branching
**Categoría:** Git / Proceso
**Definición:** Principio que aplica la filosofía zero-trust al flujo de trabajo con Git: ninguna rama, sin importar quién la creó, puede integrarse a ramas permanentes sin pasar por PR, revisión y checks automáticos. No hay excepciones, ni siquiera para el tech lead.
**En el proyecto:** El sistema de mensajería aplica zero-trust en branching mediante branch protection en GitHub: toda integración a `main` y `develop` requiere PR aprobada y pipeline verde, sin excepciones.
