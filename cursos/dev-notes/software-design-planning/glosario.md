# Glosario Técnico  -  Diseño y Planificación de Software

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### ADR (Architecture Decision Record)
**Categoría:** Arquitectura
**Definición:** Documento que registra una decisión de arquitectura de software junto con su contexto, las alternativas evaluadas y las consecuencias esperadas. La clave de un ADR es capturar el "por qué" de la decisión, no solo el resultado. Se numeran secuencialmente (ADR-001, ADR-002) y se mantienen en el repositorio del proyecto junto con el código.
**En el proyecto:** ADR-001 documenta la elección de Monolito Modular + Clean Architecture + Vertical Slice para el sistema de mensajería, explicando por qué se descartaron los microservicios dado el tamaño del equipo y la ausencia de señales que justifiquen esa complejidad.

### Agente (rol en CRM)
**Categoría:** Proceso
**Definición:** Usuario operativo del sistema de CRM que atiende conversaciones entrantes, responde mensajes y gestiona contactos. Es el actor principal de la bandeja de entrada. Tiene un rol diferenciado del supervisor y del admin, con permisos acotados a las conversaciones que le son asignadas.
**En el proyecto:** La entidad `User` en el sistema de mensajería tiene un campo `Role` que distingue entre admin, agent y viewer. Los Jobs to be Done del módulo de asignación están centrados en el agente como receptor de conversaciones.

### Alcance (scope)
**Categoría:** Planificación
**Definición:** Conjunto de funcionalidades y entregables que el proyecto incluye explícitamente, así como los que quedan fuera. Un alcance bien definido tiene una lista "qué NO incluye" tan importante como la lista de lo que sí. Sin un alcance explícito, el equipo construye lo que imagina en lugar de lo que el cliente necesita.
**En el proyecto:** El Brief del sistema de mensajería declara explícitamente que el sistema no incluye IA generativa ni llamadas de voz, lo que protege al equipo de expansión no planificada del alcance (scope creep).

### Artefacto entregable
**Categoría:** Planificación
**Definición:** Producto concreto y revisable que resulta de completar una fase del proceso de desarrollo. Puede ser un documento (Brief, ADR, wireframe), un diagrama (C4, ER) o código ejecutable (pipeline CI/CD, staging funcional). La clave es que puede ser revisado y aprobado antes de pasar a la siguiente fase.
**En el proyecto:** Cada una de las cuatro fases del roadmap del sistema de mensajería termina con un artefacto visible para el cliente: login funcional en staging (Fase 1), agentes respondiendo mensajes en vivo (Fase 2), gestión completa de contactos (Fase 3), sistema desplegado y monitoreado (Fase 4).

### Arquitectura de software
**Categoría:** Arquitectura
**Definición:** Conjunto de decisiones estructurales que definen cómo se organiza un sistema: cómo se dividen sus partes, cómo se comunican entre sí, y cuáles son las restricciones que guían su evolución. Las decisiones de arquitectura son las más costosas de cambiar posteriormente, por eso se documentan en ADRs antes de empezar a codificar.
**En el proyecto:** La arquitectura del sistema de mensajería es Monolito Modular con organización por Vertical Slice, documentada en ADR-001. Cada feature vive en su carpeta con su propio Command/Query, Handler y Response.

### Asignación automática
**Categoría:** Proceso
**Definición:** Mecanismo que distribuye conversaciones entrantes entre los agentes disponibles sin intervención manual, aplicando una regla predefinida como menor carga, turno rotativo o especialización. Reduce la latencia de respuesta y elimina el costo cognitivo de que el supervisor decida manualmente quién atiende cada mensaje.
**En el proyecto:** Una historia de usuario del sistema de mensajería describe que las conversaciones entrantes se asignan automáticamente al agente con menor cantidad de conversaciones abiertas. Esta lógica vive en `ConversationService` y se activa cuando llega un webhook de WhatsApp.

---

## B

### Backlog
**Categoría:** Planificación
**Definición:** Lista priorizada de todo el trabajo pendiente de un proyecto de software: historias de usuario, bugs, tareas técnicas y deuda técnica. El backlog debe estar priorizado, estimado y suficientemente detallado para que el equipo pueda trabajar en él sin necesitar aclaraciones constantes. Se refina continuamente durante el proyecto.
**En el proyecto:** El backlog del sistema de mensajería se gestiona en Linear o GitHub Projects, con etiquetas que distinguen features, bugs y elementos de deuda técnica. El MVP define qué entra en el sprint 1.

### BackgroundService
**Categoría:** Arquitectura
**Definición:** Servicio de larga duración que corre en segundo plano dentro de un proceso, independiente del ciclo de vida de una solicitud HTTP. En .NET se implementa con `IHostedService` o la clase base abstracta `BackgroundService`. Se usa para procesamiento asíncrono, colas de trabajo, jobs periódicos y tareas que no deben bloquear la respuesta al cliente.
**En el proyecto:** El webhook de WhatsApp debe responder `200 OK` en menos de 5 segundos. El procesamiento pesado del mensaje (asignación, persistencia, notificación en tiempo real) se delega a un `BackgroundService` para no bloquear el hilo HTTP.

### Bandeja de entrada
**Categoría:** Proceso
**Definición:** Vista central de un sistema de CRM que lista las conversaciones activas y pendientes de atención. Es el punto de trabajo primario de los agentes. Debe soportar filtros por estado, canal y agente asignado, además de búsqueda de texto completo (FTS) para ser útil a volúmenes altos.
**En el proyecto:** La bandeja de entrada es uno de los tres componentes del MVP del sistema de mensajería. Se implementa en la Fase 2 del roadmap con filtros, paginación cursor-based y búsqueda FTS en la Fase 3.

### BCrypt
**Categoría:** Arquitectura
**Definición:** Algoritmo de hashing de contraseñas diseñado para ser intencionalmente lento, dificultando los ataques de fuerza bruta y de tablas rainbow. Incorpora un factor de costo ajustable y genera un salt aleatorio por cada hash para evitar ataques de precomputación. Es la alternativa segura a MD5 o SHA-1 sin salt.
**En el proyecto:** El módulo Auth del sistema de mensajería usa BCrypt para almacenar el `PasswordHash` en la entidad `User`. El factor de costo se calibra para que el hash tome entre 200 y 500 ms en el servidor de producción.

### Brief de producto
**Categoría:** Planificación
**Definición:** Documento de una página que captura los cinco elementos esenciales antes de diseñar cualquier sistema: el problema que se resuelve, el usuario objetivo, la métrica de éxito, las restricciones de tiempo/presupuesto/tecnología y los límites explícitos del alcance. Funciona como contrato de entendimiento entre el equipo técnico y el cliente antes de escribir código.
**En el proyecto:** El Brief del sistema de mensajería define que el problema es que los agentes responden WhatsApp desde celulares personales sin historial ni asignación; la métrica de éxito incluye tiempo de primera respuesta menor a 2 minutos; el sistema no incluye IA generativa ni llamadas de voz.

### Bugfix (rama)
**Categoría:** Proceso
**Definición:** Rama de Git de corta duración creada para corregir un defecto específico. En Git Flow se abre desde `develop` para bugs no urgentes, y desde `main` o `release` para hotfixes críticos en producción. Se nombra con el prefijo `bugfix/` seguido de una descripción del defecto o el número de ticket.
**En el proyecto:** Si se detecta un bug en la lógica de asignación de conversaciones durante el sprint, se crea `bugfix/fix-assignment-null-agent` desde `develop`, se corrige y se fusiona de vuelta sin tocar `main`.

---

## C

### C4 (modelo)
**Categoría:** Arquitectura
**Definición:** Modelo para comunicar la arquitectura de software a distintos niveles de detalle, creado por Simon Brown. Sus cuatro niveles son: Contexto (el sistema y sus actores), Contenedores (componentes desplegables), Componentes (organización interna de un contenedor) y Código (diagrama de clases, opcional). Cada nivel responde una pregunta diferente y está dirigido a una audiencia diferente.
**En el proyecto:** El sistema de mensajería tiene documentados el Nivel 1 (actores: Agente, Supervisor, Contacto externo; sistemas: WhatsApp, Instagram, Stripe, SMTP) y el Nivel 2 (React SPA, .NET Web API, PostgreSQL, Redis, S3/MinIO). El Nivel 3 muestra los módulos internos del API: Auth, Conversations, Contacts, Channels y Bots.

### Cardinalidad
**Categoría:** Bases de datos
**Definición:** Expresión de la relación numérica entre dos entidades en un modelo de datos: uno a uno (1:1), uno a muchos (1:N) o muchos a muchos (N:M). Se representa en diagramas ER con notación crow's foot. Definir la cardinalidad correctamente antes de implementar evita esquemas de base de datos que no reflejan la realidad del negocio.
**En el proyecto:** En el ER del sistema de mensajería, `Workspace` tiene relación 1:N con `Contact`, `Channel` tiene 1:N con `Conversation`, y `Conversation` tiene 1:N con `Message`. Estas cardinalidades definen las claves foráneas y los índices de la base de datos.

### Change Failure Rate
**Categoría:** Proceso
**Definición:** Métrica DORA que mide el porcentaje de despliegues a producción que resultan en un incidente, degradación del servicio o necesidad de rollback. Se calcula como (deployments fallidos / total deployments) × 100. El nivel élite es menor al 5%. Un valor alto indica falta de cobertura de tests o de validación en staging.
**En el proyecto:** El pipeline CI/CD del sistema de mensajería incluye tests unitarios y de integración obligatorios antes del deploy, contribuyendo a mantener un Change Failure Rate bajo desde las primeras semanas.

### CI/CD (Continuous Integration / Continuous Delivery)
**Categoría:** Proceso
**Definición:** Conjunto de prácticas que automatizan la integración de cambios de código y su entrega a ambientes de staging o producción. CI ejecuta automáticamente el build y los tests en cada push al repositorio. CD automatiza el despliegue cuando el pipeline pasa. Juntos eliminan el "funciona en mi máquina" y acortan el tiempo desde código hasta producción.
**En el proyecto:** El pipeline del sistema de mensajería corre en GitHub Actions con OIDC para autenticarse en AWS. La Fase 1 del roadmap incluye el pipeline CI/CD como entregable obligatorio, no como elemento opcional para más adelante.

### Clean Architecture
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico propuesto por Robert C. Martin que organiza el código en capas concéntricas con dependencias que solo apuntan hacia adentro: Domain (sin dependencias externas), Application (casos de uso), Infrastructure (implementaciones concretas) y Presentation (controladores, endpoints). El dominio no conoce nada de la infraestructura.
**En el proyecto:** El back-template del sistema de mensajería sigue Clean Architecture. Los namespaces son `Domain/`, `Application/`, `Infrastructure/`, `Presentation/` y `Host/`. Los handlers de CQRS viven en Application; EF Core vive en Infrastructure.

### Code Review
**Categoría:** Proceso
**Definición:** Proceso formal de revisión de código en el que uno o más desarrolladores distintos del autor examinan los cambios propuestos en un Pull Request antes de fusionarlos. El objetivo es detectar bugs, verificar la adherencia a los estándares del equipo, la cobertura de tests y la correcta implementación del criterio de aceptación.

### Componente (C4 nivel 3)
**Categoría:** Arquitectura
**Definición:** En el modelo C4, un componente es una unidad lógica dentro de un contenedor: un módulo, un servicio, una librería o un conjunto de clases relacionadas con una responsabilidad clara. El nivel 3 muestra cómo está organizado internamente el contenedor más importante del sistema.
**En el proyecto:** Los componentes del .NET API del sistema de mensajería son los módulos Auth, Conversations, Contacts, Channels y Bots, todos comunicados a través del Shared Kernel / Common Library sin referencias directas entre módulos.

### Complejidad operacional
**Categoría:** Arquitectura
**Definición:** Carga de trabajo adicional que genera una decisión de arquitectura sobre el equipo de operaciones: despliegues coordinados, monitoreo de múltiples servicios, gestión de comunicación distribuida, troubleshooting en red. Es uno de los costos ocultos de los microservicios que los equipos pequeños suelen subestimar.
**En el proyecto:** Elegir Monolito Modular sobre microservicios para el sistema de mensajería reduce la complejidad operacional a un solo proceso, un solo deploy y una sola base de datos, sin sacrificar la modularidad interna.

### Contenedor (C4 nivel 2)
**Categoría:** Arquitectura
**Definición:** En el modelo C4, un contenedor es cualquier elemento desplegable de forma independiente: una aplicación web, una API, una base de datos, un servicio de caché o un almacenamiento de archivos. El diagrama de contenedores muestra qué piezas existen y cómo se comunican entre sí con sus tecnologías explícitas.
**En el proyecto:** Los contenedores del sistema de mensajería son: React SPA (Vite + Tailwind v4), .NET 10 Web API (Clean Architecture), PostgreSQL (datos), Redis (caché + backplane SignalR) y S3/MinIO (archivos adjuntos).

### Contexto del sistema (C4 nivel 1)
**Categoría:** Arquitectura
**Definición:** Primer nivel del modelo C4. Muestra el sistema como una caja negra y representa quiénes lo usan (actores humanos) y con qué sistemas externos se integra. Es el diagrama más accesible y va dirigido a stakeholders no técnicos: clientes, gerentes y cualquier persona interesada en el sistema pero no en sus detalles técnicos.
**En el proyecto:** El diagrama de contexto del sistema de mensajería muestra los actores Agente/Supervisor y Contacto externo, y los sistemas externos WhatsApp Cloud API, Instagram Graph API, Stripe y SMTP. No aparece ni una línea de código ni una tabla de base de datos.

### CQRS (Command Query Responsibility Segregation)
**Categoría:** Arquitectura
**Definición:** Patrón que separa las operaciones de escritura (Commands) de las de lectura (Queries) en modelos distintos. Un Command modifica el estado del sistema y no retorna datos de negocio. Una Query lee datos y no modifica el estado. Esta separación hace explícita la intención de cada operación y permite optimizar cada lado de forma independiente.
**En el proyecto:** En el back-template del sistema de mensajería, cada feature tiene su propio Command o Query con su Handler. Por ejemplo, `AssignConversationCommand` con `AssignConversationHandler`, y `GetConversationsQuery` con `GetConversationsQueryHandler`.

### Criterios de aceptación
**Categoría:** Planificación
**Definición:** Condiciones específicas y verificables que una historia de usuario debe satisfacer para considerarse completa. Se expresan en formato Given/When/Then (Gherkin), lo que los hace objetivos y evita interpretaciones ambiguas. Son el puente entre el lenguaje de negocio y las pruebas automatizadas.
**En el proyecto:** La historia de asignación automática del sistema de mensajería tiene los criterios: Given hay al menos un agente Online, When llega un mensaje nuevo de WhatsApp, Then la conversación queda asignada al agente con menor carga. Deben verificarse en staging antes de cerrar el ticket.

### Crow's foot (notación)
**Categoría:** Bases de datos
**Definición:** Notación gráfica para representar cardinalidades en diagramas entidad-relación. Usa símbolos en los extremos de las líneas: una línea simple indica "uno", un tridente indica "muchos", un círculo indica "cero" y una línea perpendicular indica "uno obligatorio". Es la notación más usada en herramientas modernas de modelado de datos como draw.io o Mermaid.
**En el proyecto:** El diagrama ER del sistema de mensajería usa notación crow's foot para expresar que un `Workspace` tiene cero o muchos `Contact` y que una `Conversation` contiene cero o muchos `Message`.

---

## D

### Deuda técnica (tech debt)
**Categoría:** Proceso
**Definición:** Costo futuro acumulado por tomar atajos técnicos en el presente. Puede ser deliberada (se decide conscientemente cortar esquinas para entregar más rápido) o accidental (resultado de desconocimiento o de código que envejece). La deuda técnica no es inherentemente mala; el problema es acumularla sin registrarla ni planificar su pago.
**En el proyecto:** Usar una cola en memoria en el MVP en lugar de Redis es un ejemplo de deuda técnica deliberada en el sistema de mensajería. Se registra en el backlog con etiqueta `tech-debt`, estimación de 4h y fecha tentativa de pago más adelante (sprint de canales y background services).

### Definition of Done (DoD)
**Categoría:** Proceso
**Definición:** Lista de criterios acordados por todo el equipo que un incremento de software debe cumplir para considerarse completo. Va más allá del código: incluye tests, code review, build limpio, validación en staging y aprobación del Product Owner. La DoD no se negocia por presión de tiempo; si hay presión, se reduce el alcance del ticket.
**En el proyecto:** La DoD del sistema de mensajería incluye diez criterios: PR abierto, tests unitarios en verde, tests de integración pasando, code review aprobado, build sin warnings, validación en staging, sin secretos hardcodeados, documentación actualizada, aprobación del PO y ticket movido a Done.

### Definition of Ready (DoR)
**Categoría:** Proceso
**Definición:** Conjunto de condiciones que una historia de usuario debe cumplir antes de que el equipo pueda comenzar a trabajar en ella. Su propósito es evitar que el equipo tome trabajo que no puede completar por falta de información, wireframes sin aprobar o dependencias no resueltas. Complementa la Definition of Done.
**En el proyecto:** La DoR del sistema de mensajería requiere que cada historia tenga criterios de aceptación escritos en formato Given/When/Then, wireframe aprobado por el cliente y dependencias identificadas antes de entrar al sprint.

### Deployment Frequency
**Categoría:** Proceso
**Definición:** Métrica DORA que mide con qué frecuencia un equipo despliega cambios a producción. Equipos de élite despliegan varias veces al día; equipos de bajo rendimiento despliegan una vez al mes o menos. Una frecuencia alta requiere automatización robusta de CI/CD y una cultura de cambios pequeños y frecuentes.
**En el proyecto:** El pipeline de GitHub Actions del sistema de mensajería está diseñado para permitir deploys frecuentes a staging desde el sprint 1, estableciendo el hábito de entrega continua desde el inicio del proyecto.

### Diagrama de secuencia
**Categoría:** Arquitectura
**Definición:** Diagrama UML que muestra la interacción entre actores y componentes a lo largo del tiempo en un orden específico. Los actores se representan como columnas verticales (líneas de vida) y los mensajes como flechas horizontales en orden cronológico de arriba a abajo. Es ideal para documentar flujos críticos complejos con múltiples participantes.
**En el proyecto:** El slides documenta el flujo de recepción de mensaje de WhatsApp con un diagrama de secuencia que involucra cinco actores: WhatsApp, .NET API, ConversationService, SignalR Hub y el navegador del agente. El diagrama revela que el API debe responder 200 OK antes de hacer el procesamiento pesado.

### Diagrama ER (Entidad-Relación)
**Categoría:** Bases de datos
**Definición:** Representación gráfica del modelo de datos de un sistema que muestra las entidades (tablas), sus atributos principales y las relaciones con su cardinalidad. Se diseña antes de implementar el esquema de base de datos para detectar inconsistencias y relaciones faltantes sin el costo de una migración.
**En el proyecto:** El ER del sistema de mensajería define las relaciones entre `Workspace`, `User`, `Contact`, `Channel`, `Conversation` y `Message`. Diseñarlo antes de crear las `EntityTypeConfiguration` de EF Core evita migraciones rotas que cuestan semanas de corrección.

### Discovery (fase)
**Categoría:** Planificación
**Definición:** Primera fase del proceso de diseño y planificación de software. Su objetivo es entender el problema real antes de proponer ninguna solución técnica. Los artefactos típicos son el Brief de producto, los Jobs to be Done y el mapa de actores y flujos. No se escribe código durante esta fase.
**En el proyecto:** La fase Discovery del sistema de mensajería produce el Brief firmado, los Jobs to be Done por rol (supervisor, agente) y el mapa de actores que identifica los sistemas externos de integración.

### DORA (métricas)
**Categoría:** Proceso
**Definición:** Conjunto de cuatro métricas establecidas por el programa DevOps Research and Assessment de Google para medir la salud del proceso de entrega de software: Deployment Frequency, Lead Time for Changes, Change Failure Rate y MTTR. No miden la calidad del código directamente; miden la eficiencia del pipeline de entrega.
**En el proyecto:** El equipo del sistema de mensajería debe aspirar a métricas DORA de élite: Lead Time menor a 1 hora y Change Failure Rate menor al 5%. El primer paso es tener un pipeline CI/CD funcional desde la Fase 1, que es el prerequisito para poder medir estas métricas.

---

## E

### EF Core (Entity Framework Core)
**Categoría:** Arquitectura
**Definición:** ORM (Object-Relational Mapper) de Microsoft para .NET que permite mapear clases de dominio a tablas de base de datos y ejecutar consultas mediante LINQ en lugar de SQL directo. Gestiona migraciones de esquema, relaciones, carga perezosa/anticipada y transacciones. Se configura mediante `DbContext` y clases `EntityTypeConfiguration`.
**En el proyecto:** El sistema de mensajería usa EF Core como ORM principal con PostgreSQL. La configuración de cada entidad vive en clases `EntityTypeConfiguration` separadas; las migraciones siguen el esquema definido en el diagrama ER.

### Entidad
**Categoría:** Arquitectura
**Definición:** Objeto del dominio que tiene identidad propia (un identificador único) que lo distingue de otros objetos aunque sus atributos sean iguales. Las entidades persisten a través del tiempo y sus cambios de estado son relevantes para el negocio. En DDD, las entidades son el núcleo del modelo de dominio.
**En el proyecto:** Las entidades del sistema de mensajería son: `Workspace`, `User`, `Contact`, `Conversation`, `Message` y `Channel`. Cada una tiene un campo `Id` de tipo `long` con inicializador `init`, siguiendo la convención del back-template.

### Escala de complejidad (arquitectura)
**Categoría:** Arquitectura
**Definición:** Progresión de estilos arquitectónicos ordenados por complejidad creciente: Script/Monolito simple -> Monolito tradicional por capas -> Monolito Modular -> Microservicios. La regla es subir de nivel solo cuando exista una señal real que lo justifique, no por anticipación o preferencia técnica.
**En el proyecto:** El sistema de mensajería se ubica deliberadamente en Monolito Modular. Las señales que justificarían subir a microservicios (equipos independientes, requisitos de escala radicalmente distintos por módulo, build superior a 10 minutos) no aplican al equipo actual de 4 personas.

### Estimación (de software)
**Categoría:** Planificación
**Definición:** Proceso de cuantificar el esfuerzo, complejidad y riesgo necesarios para completar una historia de usuario. Las técnicas más comunes son Story Points con Planning Poker (complejidad relativa) y la técnica de 3 puntos PERT (tiempo con incertidumbre alta). Estimar solo en horas ignora la incertidumbre inherente al software.
**En el proyecto:** El equipo del sistema de mensajería usa Story Points con escala de Fibonacci. Si una historia supera 8 puntos o hay alta dispersión en las estimaciones, se parte antes de continuar. La historia de importar contactos desde CSV se partió en dos historias de 8 puntos.

---

## F

### Fase de diseño
**Categoría:** Planificación
**Definición:** Segunda fase del proceso de planificación de software. Traduce el problema entendido en la fase Discovery a artefactos técnicos: historias de usuario con criterios Given/When/Then, wireframes validados, decisión de stack documentada, ADRs, diagramas C4 y diagrama ER. Ningún artefacto de esta fase debe crearse antes de tener el Brief firmado.
**En el proyecto:** La fase de diseño del sistema de mensajería produce: User Stories con criterios de aceptación, wireframes de flujos críticos, ADR-001 de arquitectura, diagramas C4 Nivel 1 y 2, diagrama ER y la decisión de stack documentada con matriz ponderada.

### Fibonacci (escala de estimación)
**Categoría:** Planificación
**Definición:** Secuencia numérica (1, 2, 3, 5, 8, 13, 21) usada para estimar Story Points. Los saltos no lineales reflejan que la incertidumbre crece con la complejidad; la diferencia entre 13 y 21 es mayor que entre 1 y 2. La regla práctica es que cualquier historia mayor a 8 puntos debe partirse antes de comprometerse en un sprint.
**En el proyecto:** El equipo usa Fibonacci en Planning Poker. La alta dispersión en la historia de importación CSV (8, 13, 13, 21) reveló falta de entendimiento compartido del alcance, y se decidió partir la historia antes de continuar.

### Figma
**Categoría:** Proceso
**Definición:** Herramienta de diseño colaborativo en la nube usada para crear wireframes, mockups y prototipos navegables. Permite trabajar simultáneamente en el mismo archivo y compartir prototipos con clientes para pruebas de usuario. Es el nivel más alto de fidelidad antes de escribir código; se usa para flujos complejos o de alto riesgo.
**En el proyecto:** Figma se usa en el sistema de mensajería para prototipos navegables de flujos complejos como el bot builder con React Flow. Para validaciones rápidas de flujo se usa Excalidraw, que cuesta entre 30 y 60 minutos en lugar de 2 a 4 horas.

### Flujo crítico
**Categoría:** Planificación
**Definición:** Secuencia de acciones que el usuario debe poder completar en el MVP para que el sistema tenga valor. Un flujo crítico falla si cualquiera de sus pasos no funciona. Identificarlos desde la fase Discovery protege al equipo de gastar las primeras semanas en funcionalidades secundarias mientras el núcleo del producto está incompleto.
**En el proyecto:** Los tres flujos críticos del sistema de mensajería son: recepción de mensaje -> asignación -> respuesta del agente; login/logout de agente; y ver historial de conversación de un contacto. Estos deben funcionar el día 1 del MVP.

### Flujo secundario
**Categoría:** Planificación
**Definición:** Funcionalidad que agrega valor al sistema pero no es indispensable para el primer lanzamiento. Se prioriza después de validar que los flujos críticos funcionan correctamente con usuarios reales. Incluir flujos secundarios en el MVP es uno de los errores más comunes que retrasan el lanzamiento.
**En el proyecto:** El bot builder, las campañas masivas y los reportes son flujos secundarios del sistema de mensajería implementados en fases posteriores al MVP. Están clasificados como Should o Could en el MoSCoW del primer sprint.

### FTS (Full-Text Search)
**Categoría:** Bases de datos
**Definición:** Búsqueda de texto completo en una base de datos que indexa el contenido de campos de texto y permite consultas por relevancia, no solo coincidencia exacta. En PostgreSQL se implementa con tipos `tsvector` y `tsquery` acelerados con índices GIN. Es más eficiente que `LIKE '%término%'` para volúmenes grandes y soporta operadores booleanos y raíces de palabras.
**En el proyecto:** La bandeja de entrada del sistema de mensajería implementa FTS sobre conversaciones y mensajes en la Fase 3. Permite a los agentes encontrar conversaciones por palabras clave del contenido sin depender de búsquedas por coincidencia exacta.

---

## G

### Git Flow
**Categoría:** Proceso
**Definición:** Modelo de ramificación de Git que define ramas con roles específicos: `main` (producción estable), `develop` (integración continua), `feature/*` (nuevas funcionalidades), `release/*` (preparación de versiones) y `hotfix/*` (correcciones urgentes en producción). Cada rama tiene reglas explícitas sobre desde dónde se abre y hacia dónde se fusiona.
**En el proyecto:** El sistema de mensajería usa Git Flow desde el primer sprint. Ningún cambio va directamente a `main`; todo pasa por `develop`, luego por una rama `release/vX.Y.Z` y finalmente a `main` con un tag de versión SemVer.

### Given/When/Then (Gherkin)
**Categoría:** Planificación
**Definición:** Formato de escritura de criterios de aceptación popularizado por el lenguaje Gherkin y el framework BDD (Behavior-Driven Development). Given establece el contexto previo, When describe la acción del usuario o sistema, y Then especifica el resultado esperado. Convierte los criterios de aceptación en condiciones verificables que pueden automatizarse como tests.
**En el proyecto:** Los criterios de aceptación de todas las historias del sistema de mensajería siguen el formato Given/When/Then. Ejemplo: Given hay al menos un agente Online, When llega un mensaje nuevo de WhatsApp, Then la conversación queda asignada al agente con menor carga.

### GitHub Actions
**Categoría:** Proceso
**Definición:** Plataforma de automatización integrada en GitHub que permite definir workflows de CI/CD como archivos YAML en el repositorio. Soporta triggers por push, pull request, tags y schedules. Se integra con proveedores de nube mediante OIDC para evitar credenciales estáticas. Es el estándar del ecosistema GitHub y gratuito para repositorios públicos.
**En el proyecto:** El pipeline del sistema de mensajería corre en GitHub Actions. Se configura en la Fase 1 con autenticación OIDC hacia AWS, eliminando la necesidad de almacenar access keys en los secrets del repositorio.

### Grafana
**Categoría:** Arquitectura
**Definición:** Plataforma open source de visualización y análisis de métricas y logs. Se conecta a fuentes de datos como Prometheus, Loki, Seq o InfluxDB y genera dashboards interactivos. Se usa para observabilidad operacional: ver el comportamiento del sistema en producción en tiempo real y configurar alertas automáticas.
**En el proyecto:** El sistema de mensajería usa Grafana junto con Serilog y Seq para observabilidad en la Fase 4. Los dashboards muestran métricas de negocio (conversaciones por hora, tiempo de primera respuesta) y métricas técnicas (latencia del API, uso de Redis).

---

## H

### HMAC (Hash-based Message Authentication Code)
**Categoría:** Arquitectura
**Definición:** Mecanismo de autenticación de mensajes que usa una función de hash criptográfica combinada con una clave secreta para verificar la integridad y autenticidad de un mensaje. El receptor puede verificar que el mensaje no fue alterado y que fue enviado por alguien que conoce la clave secreta. Es el mecanismo estándar para validar webhooks entrantes.
**En el proyecto:** El endpoint `POST /webhook` del sistema de mensajería valida el HMAC enviado por WhatsApp Cloud API en el header `X-Hub-Signature-256` antes de procesar el mensaje. Si la firma no coincide, el mensaje se rechaza con un 401.

### Hotfix (rama)
**Categoría:** Proceso
**Definición:** Rama de Git de muy corta duración creada para corregir un bug crítico directamente en producción. En Git Flow se abre desde `main`, se corrige el bug, y se fusiona tanto a `main` (con un nuevo tag de versión patch) como a `develop`, para que la corrección no se pierda en la siguiente versión.
**En el proyecto:** Si hay un bug crítico en producción del sistema de mensajería (por ejemplo, el webhook de WhatsApp responde 500), se crea `hotfix/fix-webhook-500` desde `main`, se corrige, se genera el tag `v1.0.1` y se fusiona también a `develop`.

### Hype tecnológico
**Categoría:** Planificación
**Definición:** Tendencia a elegir una tecnología porque está de moda o genera entusiasmo en la comunidad, en lugar de evaluarla con criterios objetivos y ponderados. Es uno de los sesgos más comunes en decisiones de stack, especialmente en equipos jóvenes sin experiencia en proyectos de escala real.
**En el proyecto:** La matriz de decisión de stack del sistema de mensajería existe precisamente para evitar elegir Go por su reputación de rendimiento cuando el equipo tiene experiencia cero en ese lenguaje. El criterio de madurez del equipo (30%) penaliza el hype.

---

## I

### Instagram Graph API
**Categoría:** Arquitectura
**Definición:** API de Meta que permite a aplicaciones externas enviar y recibir mensajes de Instagram Direct, gestionar comentarios en publicaciones y acceder a métricas de perfil de negocios. Requiere una página de Facebook vinculada, aprobación de permisos y recibe mensajes vía webhooks similares a los de WhatsApp Cloud API.
**En el proyecto:** Instagram es el segundo canal del sistema de mensajería, implementado como flujo secundario en la Fase 2 del roadmap. La entidad `Channel` tiene un campo `Type` que distingue entre WhatsApp, Instagram y Email.

---

## J

### Jobs to be Done (JTBD)
**Categoría:** Planificación
**Definición:** Marco de referencia de innovación propuesto por Clayton Christensen que afirma que los usuarios no compran productos sino "progreso" hacia un resultado deseado en un contexto específico. El formato es: "Cuando [situación], quiero [motivación], para [resultado esperado]". Evita construir features basadas en lo que el equipo imagina que los usuarios quieren.
**En el proyecto:** Los Jobs to be Done del sistema de mensajería incluyen: cuando llega un mensaje de WhatsApp quiero ver a qué agente asignarlo (supervisor), cuando un contacto vuelve a escribir quiero ver su historial completo (agente), cuando un mensaje lleva más de 10 minutos sin respuesta quiero recibir una alerta para cumplir el SLA (agente).

### JWT (JSON Web Token)
**Categoría:** Arquitectura
**Definición:** Estándar abierto (RFC 7519) para transmitir información entre partes de forma compacta y segura como un objeto JSON firmado digitalmente. Consta de tres partes en Base64URL: header, payload y firma. Se usa para autenticación stateless: el servidor emite el token y el cliente lo envía en cada solicitud sin que el servidor mantenga sesión.
**En el proyecto:** El módulo Auth del sistema de mensajería usa JWT HS256. El token incluye el `WorkspaceId` y el `Role` del usuario en el payload, permitiendo al middleware extraer el contexto del tenant sin consultar la base de datos en cada request.

---

## L

### Lead Time for Changes
**Categoría:** Proceso
**Definición:** Métrica DORA que mide el tiempo transcurrido desde que un desarrollador hace un commit hasta que ese cambio está corriendo en producción. Incluye tiempo de revisión, ejecución del pipeline CI/CD y deploy. Es la métrica más relevante para startups que necesitan iterar rápido; el nivel élite es menor a 1 hora.
**En el proyecto:** El objetivo del sistema de mensajería es un Lead Time menor a 1 hora. Esto requiere que el pipeline de GitHub Actions sea rápido (menos de 10 minutos para build + tests) y que el proceso de code review no bloquee el merge por días.

---

## M

### Mapa de actores y flujos
**Categoría:** Planificación
**Definición:** Diagrama de la fase Discovery que identifica todos los actores que interactúan con el sistema (usuarios internos, usuarios externos, sistemas de terceros) y sus relaciones principales. Establece el perímetro del sistema y revela integraciones necesarias antes de diseñar la arquitectura. Es el artefacto que cierra la Discovery.
**En el proyecto:** El mapa de actores del sistema de mensajería muestra: Agente y Admin como actores internos, Contacto externo como actor externo, y WhatsApp Cloud API, Instagram Graph API, SMTP y Stripe como sistemas externos. Informa directamente el diagrama C4 Nivel 1.

### Matriz de decisión de stack
**Categoría:** Planificación
**Definición:** Herramienta de toma de decisiones que evalúa múltiples opciones de tecnología contra criterios ponderados. Cada criterio tiene un peso porcentual y cada opción recibe una puntuación del 1 al 5 por ese criterio. El puntaje final es la suma ponderada. Hace explícitos los criterios antes de evaluar, eliminando sesgos por preferencia personal o hype.
**En el proyecto:** La matriz de decisión del sistema de mensajería evaluó .NET 10, Node.js y Go en cinco criterios ponderados. .NET 10 ganó con 4.30 puntos sobre 5 gracias a la madurez del equipo (30% del peso), aunque Go supera en rendimiento y escala.

### Métricas de éxito
**Categoría:** Planificación
**Definición:** Indicadores cuantificables definidos en el Brief de producto que permiten verificar si el sistema cumple su propósito. Deben ser objetivos y medibles: no "mejorar la experiencia del usuario" sino "reducir el tiempo de primera respuesta a menos de 2 minutos". Sin métricas de éxito, no se puede saber si el producto funciona.
**En el proyecto:** Las métricas de éxito del sistema de mensajería son: tiempo de primera respuesta menor a 2 minutos y tasa de resolución en un solo turno mayor al 60%. Deben ser medibles en producción desde el MVP, no solo en reportes manuales.

### Microservicios
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que divide un sistema en servicios pequeños e independientes, cada uno con su propio proceso, su propia base de datos y su propio ciclo de despliegue. Ofrece escala y autonomía por servicio, pero a un costo operacional muy alto: orquestación (Kubernetes), comunicación distribuida, observabilidad distribuida y un equipo maduro en ops.
**En el proyecto:** Los microservicios son el antipatrón elegido por muchos equipos sin experiencia. El sistema de mensajería empieza deliberadamente como Monolito Modular; los microservicios solo se justifican cuando equipos distintos necesitan deployar de forma independiente o cuando un módulo tiene requisitos de escala radicalmente distintos al resto.

### Migración (base de datos)
**Categoría:** Bases de datos
**Definición:** Script versionado que aplica un cambio incremental al esquema de la base de datos: crear una tabla, agregar una columna, crear un índice o modificar una restricción. EF Core genera las migraciones automáticamente al comparar el modelo de dominio con el esquema actual. Son la única forma segura de evolucionar el esquema en equipo.
**En el proyecto:** Las migraciones del sistema de mensajería se generan con `dotnet ef migrations add` y se aplican en el pipeline CI/CD antes de cada deploy. Un esquema de BD mal modelado desde el inicio resulta en migraciones rotas que cuestan semanas de corrección.

### MinIO
**Categoría:** Arquitectura
**Definición:** Servidor de almacenamiento de objetos open source compatible con la API de Amazon S3. Permite correr localmente (en Docker) un servicio que se comporta igual que S3 en producción, facilitando el desarrollo y las pruebas sin costos de nube. Se usa para almacenar archivos adjuntos, imágenes de perfil y exportaciones.
**En el proyecto:** El sistema de mensajería usa MinIO en el ambiente local y S3 en producción para almacenar archivos adjuntos de mensajes. La URL del archivo se guarda en el campo `MediaUrl` de la entidad `Message`.

### Miro
**Categoría:** Proceso
**Definición:** Herramienta de pizarra colaborativa en la nube usada para sesiones de Discovery, mapas de actores, customer journey maps y workshops de planificación. Permite que equipos distribuidos trabajen visualmente en tiempo real. Es útil en la fase Discovery para capturar ideas de forma no estructurada antes de formalizarlas en documentos.
**En el proyecto:** Se usa Miro en la fase Discovery del sistema de mensajería para mapas de actores colaborativos, Jobs to be Done y sesiones de priorización con el cliente. El resultado se formaliza posteriormente en el Brief de producto.

### Monolito Modular
**Categoría:** Arquitectura
**Definición:** Estilo arquitectónico que organiza un sistema como un único proceso desplegable con módulos internos bien delimitados que no se llaman directamente entre sí. Cada módulo tiene su propia capa de dominio, sus propios casos de uso y sus propias tablas de base de datos. Es el punto óptimo entre la simplicidad del monolito tradicional y la autonomía de los microservicios.
**En el proyecto:** El sistema de mensajería es un Monolito Modular con módulos Auth, Conversations, Contacts, Channels y Bots. Los módulos se comunican a través de interfaces o eventos de dominio del Shared Kernel, nunca con referencias directas entre namespaces de módulos distintos.

### MoSCoW (priorización)
**Categoría:** Planificación
**Definición:** Técnica de priorización que clasifica los requisitos en cuatro categorías: Must have (sin esto no hay producto), Should have (alta prioridad pero no bloquea el MVP), Could have (deseable si hay capacidad) y Won't have (fuera del alcance actual). Se usa para definir el contenido del primer sprint y facilita la conversación de alcance con el cliente.
**En el proyecto:** Para el sprint 1 del sistema de mensajería, los Must son Auth + bandeja de entrada + WhatsApp. Los Should incluyen Instagram y asignación automática. Los Won't incluyen IA generativa y llamadas de voz, alineado con el Brief firmado.

### MTTR (Mean Time to Recover)
**Categoría:** Proceso
**Definición:** Métrica DORA que mide el tiempo medio que tarda el equipo en restablecer el servicio después de un incidente en producción. Un MTTR bajo requiere observabilidad (logs, métricas, alertas), un proceso claro de respuesta a incidentes y la capacidad de hacer rollback rápidamente. El nivel élite es menor a 1 hora.
**En el proyecto:** Para el sistema de mensajería, un MTTR élite requiere tener dashboards de Grafana/Serilog configurados desde la Fase 4, con alertas que notifiquen al equipo antes de que los clientes reporten el incidente.

### MVP (Minimum Viable Product)
**Categoría:** Planificación
**Definición:** La versión más pequeña del sistema que resuelve el problema principal y puede llegar a manos de un usuario real. Tres características lo definen: funciona en producción (no es un prototipo), resuelve el flujo crítico completo, y recoge feedback accionable. Un MVP no es el 20% de cada feature; es el 100% de las features indispensables.
**En el proyecto:** El MVP del sistema de mensajería tiene cuatro componentes: Auth completo (registro, login, JWT, refresh), WhatsApp conectado, bandeja de entrada básica y asignación manual de conversaciones. Con esto, un supervisor puede recibir mensajes de WhatsApp y asignarlos a su equipo.

---

## N

### Notion
**Categoría:** Proceso
**Definición:** Herramienta de documentación y gestión del conocimiento que combina wiki, base de datos y gestión de proyectos en un solo espacio. Se usa para documentar briefs de producto, roadmaps, decisiones de diseño y bases de conocimiento del equipo. Es colaborativa y flexible, ideal para equipos pequeños que necesitan centralizar su documentación.
**En el proyecto:** Se puede usar Notion para documentar el Brief del sistema de mensajería y los ADRs en versión legible por el cliente, compartiendo el mismo roadmap entre el equipo técnico y los stakeholders de la empresa.

---

## O

### OIDC (OpenID Connect)
**Categoría:** Arquitectura
**Definición:** Capa de identidad construida sobre OAuth 2.0 que permite que aplicaciones verifiquen la identidad de un usuario basándose en la autenticación realizada por un proveedor de identidad externo. En contextos de CI/CD, OIDC permite que GitHub Actions se autentique con AWS sin almacenar credenciales estáticas en el repositorio.
**En el proyecto:** El pipeline de GitHub Actions del sistema de mensajería usa OIDC para autenticarse con AWS, eliminando la necesidad de almacenar AWS Access Keys en los secrets del repositorio y reduciendo el riesgo de exposición de credenciales.

### Observabilidad
**Categoría:** Arquitectura
**Definición:** Capacidad de entender el estado interno de un sistema a partir de sus salidas externas: logs, métricas y trazas distribuidas. Un sistema observable permite diagnosticar un problema en producción sin necesidad de adjuntar un debugger o conectarse al servidor. Los tres pilares son: logs estructurados, métricas y trazas distribuidas.
**En el proyecto:** El sistema de mensajería implementa observabilidad en la Fase 4 con Serilog para logs, Seq para consulta de logs en tiempo real y Grafana para dashboards de métricas. Las trazas distribuidas se implementan con OpenTelemetry.

---

## P

### PERT (Program Evaluation and Review Technique)
**Categoría:** Planificación
**Definición:** Técnica de estimación que usa tres escenarios (Optimista, Probable y Pesimista) para calcular una estimación ponderada con la fórmula E = (O + 4M + P) / 6. El factor 4 en el escenario probable refleja que es el más probable. Su ventaja es que obliga al equipo a nombrar explícitamente el escenario pesimista, que generalmente existe pero nadie quiere mencionar.
**En el proyecto:** El slides aplica PERT a la importación CSV de contactos del sistema de mensajería: O=2 días (CSV limpio), M=4 días (validaciones básicas), P=10 días (encodings raros, miles de filas, transacciones complejas). El resultado E ≈ 4.7 días es más honesto que estimar directamente "4 días".

### Pipeline CI/CD
**Categoría:** Proceso
**Definición:** Secuencia automatizada de pasos que transforma código fuente en un artefacto desplegado en producción: compilación, ejecución de tests unitarios, tests de integración, análisis de código, build de imagen Docker, publicación en registry y despliegue al ambiente objetivo. Cada paso es una puerta de calidad; si falla, el pipeline se detiene.
**En el proyecto:** El pipeline del sistema de mensajería corre en GitHub Actions. Incluye build de .NET 10, tests unitarios y de integración, build de imagen Docker multi-stage, push al registry de AWS y deploy a staging automáticamente en cada merge a `develop`.

### Planning Poker
**Categoría:** Planificación
**Definición:** Técnica de estimación colaborativa en la que cada miembro del equipo revela simultáneamente su estimación en Story Points. La revelación simultánea evita el sesgo de anclaje, donde el primero en hablar influye en los demás. La dispersión alta en las estimaciones es una señal de que el equipo no tiene el mismo entendimiento del alcance de la historia.
**En el proyecto:** El equipo del sistema de mensajería usa Planning Poker para estimar cada historia antes de comprometerse en el sprint. La historia de asignación automática mostró dispersión (5, 8, 8, 13), generando una conversación para alinear el alcance antes de finalizar la estimación.

### PostgreSQL
**Categoría:** Bases de datos
**Definición:** Sistema de gestión de bases de datos relacional open source orientado a objetos. Soporta tipos avanzados (JSON, arrays, rangos), búsqueda de texto completo (FTS), Row Level Security (RLS), extensiones y procedimientos almacenados. Es la base de datos relacional open source de mayor adopción en nuevos proyectos SaaS.
**En el proyecto:** PostgreSQL es la base de datos principal del sistema de mensajería. Se usa EF Core para el CRUD estándar y Dapper para consultas de reporting complejas. La FTS se implementa con índices GIN sobre los campos de texto de conversaciones y mensajes.

### Product Owner (PO)
**Categoría:** Proceso
**Definición:** Rol responsable de maximizar el valor del producto y del trabajo del equipo de desarrollo. Define y prioriza el backlog, valida las historias de usuario y verifica que los criterios de aceptación se cumplan en staging antes de cerrar el ticket. En startups puede ser el mismo fundador o CTO.
**En el proyecto:** En la Definition of Done del sistema de mensajería, el Product Owner debe validar el criterio de aceptación en staging antes de que el ticket pueda cerrarse. Esta validación no es opcional aunque haya presión de tiempo.

### Prototipo navegable
**Categoría:** Planificación
**Definición:** Simulación interactiva de una interfaz de usuario que permite navegar entre pantallas como si fuera la aplicación real, pero sin backend. Se crea en Figma y se comparte con usuarios para pruebas de usabilidad. Es el nivel de fidelidad más alto antes de escribir código y se usa para flujos complejos o de alto riesgo.
**En el proyecto:** Se crea un prototipo navegable en Figma para el bot builder del sistema de mensajería (basado en React Flow) antes de comenzar la implementación, dado que es el flujo de mayor complejidad de interacción del sistema.

### Pull Request (PR)
**Categoría:** Proceso
**Definición:** Mecanismo de Git para proponer la fusión de una rama en otra, abriendo un espacio de revisión donde los revisores pueden comentar, solicitar cambios y aprobar. Es la unidad fundamental del code review. Un PR bien hecho es pequeño, tiene un propósito claro, incluye una descripción del cambio y referencia el ticket correspondiente.
**En el proyecto:** Ningún cambio llega a `develop` o `main` del sistema de mensajería sin pasar por un PR. La Definition of Done requiere que el PR tenga al menos una aprobación y que el pipeline CI pase sin warnings antes del merge.

---

## R

### React Flow
**Categoría:** Arquitectura
**Definición:** Librería de React para construir editores de grafos y flujos de trabajo interactivos en el navegador. Permite arrastrar nodos, conectarlos con aristas y definir comportamientos personalizados para cada tipo de nodo. Es el estándar para interfaces de diseño visual de flujos en aplicaciones web modernas.
**En el proyecto:** El sistema de mensajería usa React Flow en el módulo de Bots para el diseñador visual de flujos de conversación automatizados. Los nodos representan acciones (enviar mensaje, esperar respuesta, transferir a agente) y las aristas representan las transiciones entre estados.

### Redis
**Categoría:** Arquitectura
**Definición:** Almacén de estructuras de datos en memoria, open source, usado como caché, broker de mensajes y base de datos de sesiones. Se usa como backplane para SignalR en aplicaciones multi-instancia, permitiendo que los eventos de tiempo real lleguen a todos los clientes sin importar a qué instancia del servidor estén conectados.
**En el proyecto:** El sistema de mensajería usa Redis en dos roles: caché de datos frecuentes (configuración de workspace, permisos de usuario) y backplane de SignalR para distribuir notificaciones en tiempo real a todos los agentes conectados. En el MVP se puede omitir y usar in-memory; se introduce en la Fase 3.

### Release (rama)
**Categoría:** Proceso
**Definición:** Rama de Git creada en Git Flow cuando `develop` está lista para una nueva versión. Aísla la estabilización de la versión (bugs de último minuto, ajustes de configuración) sin bloquear el trabajo nuevo en `develop`. Al finalizar, se fusiona tanto a `main` (con el tag de versión) como a `develop` para que los fixes queden integrados.
**En el proyecto:** Cuando el sistema de mensajería alcanza el estado de MVP completo, se crea la rama `release/v1.0.0`, se estabiliza y se fusiona a `main` con el tag `v1.0.0`. La primera feature post-MVP (Instagram conectado) generará `release/v1.1.0`.

### Restricción (de proyecto)
**Categoría:** Planificación
**Definición:** Limitación explícita que el Brief de producto debe registrar: tiempo disponible, tamaño del equipo, tecnologías obligatorias o presupuesto fijo. Las restricciones definen el espacio de soluciones posibles y evitan propuestas irrealizables que el equipo no puede sostener.
**En el proyecto:** Las restricciones del sistema de mensajería son: 6 meses de desarrollo, equipo de 4 personas y stack .NET 10 + React 19 definido en la propuesta comercial firmada.

### Resultado Pattern (Result Pattern)
**Categoría:** Arquitectura
**Definición:** Patrón de diseño que representa el resultado de una operación como un objeto que puede ser éxito (con valor) o fallo (con error), en lugar de lanzar excepciones para flujos de negocio. Hace explícito en el tipo de retorno que una operación puede fallar y obliga al llamador a manejar ambos casos.
**En el proyecto:** Todos los handlers de CQRS del sistema de mensajería retornan un `Result<T>`. El Presenter hace pattern matching sobre el resultado: si es OK retorna 200/201; si es fallo retorna el Problem Details correspondiente (400, 404, 409, etc.).

### Retrabajo
**Categoría:** Proceso
**Definición:** Trabajo que debe rehacerse porque la salida de una etapa previa no fue suficientemente validada. Es la principal consecuencia de saltarse fases de planificación. Una hora de diseño previo ahorra en promedio diez horas de corrección posterior, según la regla citada en el módulo.
**En el proyecto:** Los tres errores de no planificación citados en el módulo (microservicios prematuros, esquema sin modelar, UI sin wireframes) generaron semanas de retrabajo en proyectos reales. El proceso de cuatro fases del módulo está diseñado para evitar exactamente estos costos.

### RICE (priorización)
**Categoría:** Planificación
**Definición:** Fórmula de priorización para roadmaps: RICE = (Reach × Impact × Confidence) / Effort. Reach es cuántos usuarios usan el feature por mes, Impact es el efecto en esos usuarios (escala 0.5 a 3), Confidence es el porcentaje de certeza en la estimación, y Effort son las personas-mes. Un puntaje alto indica alto impacto con bajo costo y alta certeza.
**En el proyecto:** Se usa RICE para priorizar el roadmap de los próximos 3 meses del sistema de mensajería después de validar el MVP. Por ejemplo, campañas masivas vs. bot builder vs. reportes avanzados se decide con RICE, no con intuición del equipo.

### Roadmap técnico
**Categoría:** Planificación
**Definición:** Plan de alto nivel que describe las fases o versiones del producto y sus entregables esperados en el tiempo. No es un cronograma rígido de fechas; comunica dirección y prioridades. Cada fase debe terminar con algo que el cliente puede ver y validar. Un roadmap honesto incluye tanto lo que se va a hacer como lo que no.
**En el proyecto:** El roadmap del sistema de mensajería tiene cuatro fases: Auth + CI pipeline (semanas 1-4), canales + conversaciones + bandeja (5-10), contactos + bots + búsqueda (11-16), y DevOps completo + observabilidad + producción (17-22).

### Rollback
**Categoría:** Proceso
**Definición:** Proceso de revertir un despliegue fallido a la versión anterior estable. Puede ser un re-deploy de la imagen Docker anterior, un revert de commit en Git o un rollback de migración de base de datos. Debe estar documentado y probado antes de que ocurra el primer incidente; no se improvisa bajo presión.
**En el proyecto:** El pipeline del sistema de mensajería mantiene las imágenes Docker de las últimas versiones en el registry de AWS, lo que permite hacer rollback a la versión anterior en minutos si un deploy a producción causa un incidente.

---

## S

### S3 (Amazon Simple Storage Service)
**Categoría:** Arquitectura
**Definición:** Servicio de almacenamiento de objetos de AWS con durabilidad del 99.999999999%. Soporta pre-signed URLs que permiten que el cliente suba o descargue archivos directamente sin pasar por el servidor de la aplicación, reduciendo la carga y el ancho de banda del API. Es el estándar para almacenamiento de archivos en aplicaciones SaaS en AWS.
**En el proyecto:** El sistema de mensajería usa S3 en producción para almacenar archivos adjuntos de mensajes. El API genera pre-signed URLs que el cliente React usa para subir imágenes directamente a S3, sin que el archivo pase por el .NET API.

### SemVer (Semantic Versioning)
**Categoría:** Proceso
**Definición:** Convención de versionado de software con el formato MAJOR.MINOR.PATCH. MAJOR se incrementa cuando hay breaking changes. MINOR cuando se agrega funcionalidad nueva sin romper la compatibilidad existente. PATCH cuando se corrigen bugs sin agregar funcionalidad. Es el estándar de la industria para versionar APIs y librerías.
**En el proyecto:** El sistema de mensajería aplica SemVer conectado al Git Flow: v1.0.0 es el MVP completo, v1.1.0 es la primera feature nueva post-MVP (Instagram conectado), v1.1.1 es el primer bug crítico corregido, y v2.0.0 implicaría un cambio de contrato del API que rompe compatibilidad con clientes existentes.

### Seq
**Categoría:** Arquitectura
**Definición:** Servidor de logs estructurados que indexa eventos JSON y proporciona una interfaz web para consultarlos con filtros, búsquedas y alertas. Se integra nativamente con Serilog en .NET. En desarrollo y staging es más práctico que Elasticsearch por su menor complejidad operacional.
**En el proyecto:** El sistema de mensajería envía los logs de Serilog a Seq en los ambientes de desarrollo y staging. Los eventos incluyen el `WorkspaceId` como propiedad, permitiendo filtrar todos los logs de un tenant específico durante la investigación de un incidente.

### Serilog
**Categoría:** Arquitectura
**Definición:** Biblioteca de logging estructurado para .NET que permite escribir eventos con propiedades tipadas (no solo texto) y enrutarlos a múltiples sinks de forma simultánea (consola, archivo, Seq, Application Insights). Se configura mediante fluent API o appsettings.json y es el estándar de facto para logging en proyectos .NET modernos.
**En el proyecto:** El back-template del sistema de mensajería incluye Serilog preconfigurado con sink de consola y Seq. La regla crítica: nunca usar string interpolation en los mensajes de Serilog; usar templates con `{PropertyName}` para que las propiedades queden estructuradas y consultables.

### SignalR
**Categoría:** Arquitectura
**Definición:** Biblioteca de ASP.NET Core que facilita la comunicación bidireccional en tiempo real entre servidor y clientes usando WebSockets (con fallback a Server-Sent Events y long polling). Los clientes se conectan a un Hub y pueden suscribirse a grupos. Con Redis como backplane, funciona en arquitecturas multi-instancia.
**En el proyecto:** El sistema de mensajería usa SignalR para notificar a los agentes en tiempo real cuando llega un nuevo mensaje o cuando una conversación es reasignada. El Hub de SignalR es el destino final del flujo que comienza con el webhook de WhatsApp.

### SLA (Service Level Agreement)
**Categoría:** Proceso
**Definición:** Acuerdo formal entre un proveedor de servicio y un cliente que define métricas mínimas de calidad: tiempo de respuesta, disponibilidad, tiempo de resolución. En el contexto de soporte conversacional, el SLA define en cuánto tiempo debe responder un agente a un mensaje entrante. Su incumplimiento puede tener consecuencias contractuales.
**En el proyecto:** Un Job to be Done del sistema de mensajería menciona explícitamente el SLA: "cuando un mensaje lleva más de 10 minutos sin respuesta quiero recibir una alerta para cumplir el SLA del cliente". Esta alerta se implementa como un background job periódico.

### Slug
**Categoría:** Arquitectura
**Definición:** Identificador legible por humanos derivado del nombre de un recurso, usado en URLs en lugar del ID numérico. Se construye normalizando el nombre: minúsculas, sin espacios (reemplazados por guiones), sin caracteres especiales. Mejora la legibilidad de las URLs y puede usarse para enrutamiento por subdominio en arquitecturas multi-tenant.
**En el proyecto:** La entidad `Workspace` del sistema de mensajería tiene un campo `Slug` (ejemplo: `acme-corp`) usado para identificar el workspace en la URL del API y potencialmente como subdominio en una arquitectura multi-tenant avanzada.

### Scope creep
**Categoría:** Planificación
**Definición:** Fenómeno en el que el alcance de un proyecto crece de forma no controlada durante el desarrollo, generalmente por la adición de requerimientos no contemplados en el Brief inicial. Es una de las causas más comunes de retrasos y sobrecostos en proyectos de software sin un alcance explícito firmado.
**En el proyecto:** El Brief del sistema de mensajería previene el scope creep al listar explícitamente qué no incluye el sistema: IA generativa y llamadas de voz. Cualquier solicitud fuera de ese límite requiere un cambio formal de alcance acordado con el cliente.

### Sprint
**Categoría:** Proceso
**Definición:** Ciclo de trabajo de duración fija (típicamente 1 o 2 semanas) en el que el equipo se compromete a completar un conjunto de historias del backlog. Al final del sprint, el equipo debe tener un incremento potencialmente entregable. El sprint 1 es el más crítico porque establece el ritmo del equipo y valida el pipeline CI/CD.
**En el proyecto:** El roadmap del sistema de mensajería se organiza en sprints de dos semanas. El sprint 1 incluye Auth, la estructura del repositorio y el pipeline CI/CD. No entra ninguna funcionalidad de negocio en sprint 1 si el pipeline no está corriendo.

### Staging (ambiente)
**Categoría:** Proceso
**Definición:** Ambiente de despliegue que replica producción lo más fielmente posible, usado para validar cambios antes de que lleguen a los usuarios reales. La Definition of Done del sistema de mensajería requiere que el Product Owner valide el criterio de aceptación en staging, no en el ambiente local del desarrollador.
**En el proyecto:** El sistema de mensajería tiene un ambiente de staging en AWS configurado desde la Fase 1. Cada PR mergeado a `develop` desencadena un deploy automático a staging vía GitHub Actions.

### Story Points
**Categoría:** Planificación
**Definición:** Unidad relativa de medida del esfuerzo, la complejidad y la incertidumbre de una historia de usuario. No representan horas sino tamaño relativo: una historia de 2 puntos es aproximadamente el doble de fácil que una de 4. El equipo calibra su propio significado de los puntos a lo largo de los sprints, y la velocidad (puntos completados por sprint) se convierte en métrica de predicción.
**En el proyecto:** La velocidad del equipo del sistema de mensajería se mide en Story Points por sprint. Los primeros dos sprints sirven para calibrar la velocidad; el roadmap se ajusta proyectando esa velocidad sobre las historias restantes.

### Stripe
**Categoría:** Arquitectura
**Definición:** Plataforma de procesamiento de pagos que ofrece APIs para cobro de suscripciones, pagos únicos, facturación y gestión de planes. Notifica eventos (pago exitoso, pago fallido, suscripción cancelada) vía webhooks que el backend debe procesar de forma idempotente. Es el estándar de la industria para billing de SaaS.
**En el proyecto:** Stripe aparece en el mapa de actores del sistema de mensajería como sistema externo de billing. El módulo de billing gestiona planes y límites por workspace, con integración a los webhooks de Stripe para actualizar el estado de la suscripción.

---

## T

### Terraform
**Categoría:** Proceso
**Definición:** Herramienta de Infrastructure as Code (IaC) de HashiCorp que permite definir y provisionar infraestructura en la nube usando archivos HCL declarativos. Los cambios de infraestructura pasan por el mismo proceso de review y versionado que el código de la aplicación. Soporta múltiples proveedores: AWS, Azure, GCP.
**En el proyecto:** El sistema de mensajería define su infraestructura de AWS (ECS, RDS PostgreSQL, ElastiCache Redis, S3) con Terraform. Los archivos `.tf` viven en el repositorio y los cambios de infraestructura pasan por PRs con el mismo proceso de review que el código de la aplicación.

### Time-to-market
**Categoría:** Planificación
**Definición:** Tiempo que tarda una organización desde que concibe una idea hasta que entrega valor al usuario final. En el contexto de decisión de stack, es la velocidad inicial del equipo para construir las primeras features. Un stack maduro para el equipo tiene menor time-to-market que uno nuevo aunque sea técnicamente superior.
**En el proyecto:** El time-to-market es uno de los cinco criterios ponderados en la decisión de stack del sistema de mensajería (15% del peso). .NET 10 y Node.js empatan en este criterio (4/5), mientras que Go obtiene 2/5 por su curva de aprendizaje para el equipo actual.

---

## U

### User Story (Historia de usuario)
**Categoría:** Planificación
**Definición:** Descripción breve de una funcionalidad desde la perspectiva del usuario, con el formato: "Como [rol], quiero [acción], para [beneficio]". Es la unidad de trabajo del equipo en metodologías ágiles. Se complementa obligatoriamente con criterios de aceptación en formato Given/When/Then y con la Definition of Ready antes de entrar al sprint.
**En el proyecto:** Las historias del sistema de mensajería usan los roles reales del sistema: supervisor, agente, admin. Ejemplo: "Como supervisor, quiero que las conversaciones entrantes se asignen automáticamente al agente disponible con menor carga, para no tener que asignarlas manualmente una por una."

---

## V

### Vertical Slice
**Categoría:** Arquitectura
**Definición:** Estilo de organización del código donde cada feature tiene su propia carpeta con todas las capas necesarias: Command/Query, Handler, Response, Validator y mapeos de EF Core. En lugar de organizar por capas horizontales (todos los repositorios juntos, todos los servicios juntos), se organiza por funcionalidad vertical. Facilita encontrar y modificar todo lo relacionado con una funcionalidad sin navegar entre múltiples carpetas.
**En el proyecto:** El back-template del sistema de mensajería organiza el código por Vertical Slice. La feature de asignación de conversaciones tiene `AssignConversationCommand`, `AssignConversationHandler` y `AssignConversationResponse` en su propia carpeta `Features/Conversations/Assign/`.

### Velocidad del equipo
**Categoría:** Planificación
**Definición:** Número de Story Points que el equipo completa por sprint en promedio. Se calcula después de los primeros dos o tres sprints y se usa para proyectar cuándo se completarán las historias restantes del backlog. No es un objetivo en sí mismo; es una métrica de predicción que el equipo usa para comprometerse de forma realista.
**En el proyecto:** La velocidad del equipo del sistema de mensajería se calibra durante los primeros sprints y se usa para ajustar el roadmap de cuatro fases. Si el equipo completa 20 puntos por sprint, se puede proyectar cuántos sprints quedan hasta completar el MVP.

---

## W

### WebSocket
**Categoría:** Arquitectura
**Definición:** Protocolo de comunicación que establece una conexión persistente y bidireccional entre cliente y servidor sobre una única conexión TCP. A diferencia de HTTP, el servidor puede enviar datos al cliente sin que este lo solicite. Es la base de las comunicaciones en tiempo real en aplicaciones web modernas y el transporte preferido de SignalR.
**En el proyecto:** El sistema de mensajería usa WebSockets a través de SignalR para entregar notificaciones en tiempo real a los agentes: nuevos mensajes, reasignaciones de conversaciones y cambios de estado de la bandeja de entrada.

### Webhook
**Categoría:** Arquitectura
**Definición:** Mecanismo de notificación HTTP en el que un sistema externo llama al API del sistema receptor cuando ocurre un evento, en lugar de que el receptor haga polling periódico. El receptor expone un endpoint que el sistema externo llama con un POST HTTP. Es el mecanismo que usan WhatsApp, Instagram, Stripe y otros servicios para notificar eventos al backend.
**En el proyecto:** El sistema de mensajería expone el endpoint `POST /webhook` que recibe notificaciones de WhatsApp Cloud API cuando llega un nuevo mensaje. El endpoint valida el HMAC, responde 200 OK en menos de 5 segundos y delega el procesamiento pesado a un BackgroundService.

### WhatsApp Cloud API
**Categoría:** Arquitectura
**Definición:** API oficial de Meta para enviar y recibir mensajes de WhatsApp desde aplicaciones de negocio. Usa webhooks para notificar mensajes entrantes y endpoints REST para enviar mensajes salientes. Requiere un número de teléfono verificado, una cuenta de Business Manager de Meta y aprobación de plantillas de mensajes.
**En el proyecto:** WhatsApp Cloud API es el canal principal del sistema de mensajería y el único incluido en el MVP. La integración cubre la recepción de webhooks con validación HMAC y el envío de respuestas mediante la API REST de Meta con autenticación por token de acceso permanente.

### Wireframe
**Categoría:** Planificación
**Definición:** Representación esquemática de baja fidelidad de una interfaz de usuario que muestra la estructura y el flujo de navegación sin detalle visual (sin colores, tipografías definitivas ni imágenes reales). Su propósito es validar el flujo y la arquitectura de información con el cliente a bajo costo. Existen tres niveles: boceto en papel (5 min), wireframe digital en Excalidraw/draw.io (30-60 min) y prototipo navegable en Figma (2-4 h).
**En el proyecto:** Los flujos críticos del sistema de mensajería (bandeja de entrada + chat) se validan con wireframes antes de comenzar la implementación de React. La regla es: ningún flujo crítico entra en sprint sin wireframe aprobado por el cliente. El costo de cambiar un wireframe es cero; el costo de cambiar código en producción no lo es.

### Workspace
**Categoría:** Arquitectura
**Definición:** Unidad organizativa que agrupa todos los recursos de un tenant en una aplicación SaaS: usuarios, contactos, canales, conversaciones y configuraciones. Es el límite del aislamiento de datos entre clientes. En el sistema de mensajería, cada empresa cliente tiene exactamente un workspace con su propio Id, Name, Slug y Plan de suscripción.
**En el proyecto:** La entidad `Workspace` es la raíz del modelo de datos del sistema de mensajería. Todas las entidades principales (`User`, `Contact`, `Channel`, `Conversation`) tienen un `WorkspaceId` como clave foránea que garantiza que los datos de un cliente nunca se mezclan con los de otro.
