<!-- _class: lead -->

# Diseño y Planeación de Software

## De la idea al sistema en producción

**Dev Notes**
Rogelio Arriaga  -  Raptor Dev Services

---

## Agenda

- # | Bloque | Slides
- 1 | El costo de NO planificar | 3-4
- 2 | Fase Discovery | 5-8
- 3 | Fase Diseño | 9-15
- 4 | Modelo C4 | 16-21
- 5 | Estimación | 22-25
- 6 | MVP y Priorización | 26
- 7 | Implementación estructurada | 27-34
- 8 | Cierre | 35

---

## El costo de NO planificar

Tres errores reales que se repiten en cada proyecto sin planeación:

| Error | Consecuencia | Costo real |
|-------|-------------|------------|
| Elegir microservicios desde el día 1 | Reescribir todo en monolito a las 6 semanas | ~3 semanas perdidas |
| Esquema de BD sin modelar antes | Migraciones rotas, datos inconsistentes | ~2 semanas + deuda técnica |
| UI sin wireframes validados | Rediseño completo del frontend | ~2 semanas + fricción con el cliente |

> "1 hora de diseño previo ahorra 10 horas de corrección posterior."

Los tres errores ocurrieron en proyectos reales. Los tres eran evitables.

---

## Fases de planeación

```
+------------+     +------------+     +------------------+     +------------+
| DISCOVERY  |----|  DISEÑO    |----| IMPLEMENTACIÓN   |----| VALIDACIÓN |
`------------+     `------------+     `------------------+     `-----+------+
      |                  |                     |                      |
                                                                   |
  Brief +            ADRs +               Código +                    |
  Jobs to be Done    C4 + ER +            tests +                     |
  Mapa de actores    User Stories         CI/CD                       |
                     Wireframes                                        |

                                                              <-- loop --+
```

Cada fase produce un **artefacto entregable** que el equipo revisa antes de pasar a la siguiente.
No se salta ninguna fase; se calibra su duración según el riesgo del proyecto.

---

<!-- _class: lead -->

# Fase Discovery

## Entender el problema antes de escribir una línea de código

---

## Discovery: Brief de producto

Template mínimo obligatorio antes de diseñar cualquier sistema:

| Campo | Pregunta clave | Ejemplo  -  Sistema de mensajería |
|-------|---------------|------------------------|
| **Problema** | ¿Qué dolor resuelve? | Agentes responden WhatsApp desde su celular personal; sin historial ni asignación |
| **Usuario objetivo** | ¿Quién lo usa, en qué contexto? | Agentes de soporte en empresas medianas (5-50 agentes) |
| **Métrica de éxito** | ¿Cómo sabemos que funcionó? | Tiempo de primera respuesta < 2 min; tasa de resolución en 1 turno > 60% |
| **Restricciones** | Tiempo / presupuesto / tech | 6 meses  -  equipo de 4  -  stack .NET 10 + React 19 |
| **Qué NO incluye** | Límite explícito del alcance | No incluye IA generativa, no incluye llamadas de voz |

Sin un Brief firmado, el equipo construye lo que imagina, no lo que el cliente necesita.

---

## Discovery: Jobs to be Done

> "Los usuarios no compran software, compran progreso."
>  -  Clayton Christensen

**Formato:** Cuando `[situación]` quiero `[motivación]` para `[resultado esperado]`

| # | Job | Actor |
|---|-----|-------|
| 1 | Cuando llega un mensaje de WhatsApp **quiero ver a qué agente asignarlo** para que ninguna conversación quede sin atender | Supervisor |
| 2 | Cuando un contacto escribe de nuevo **quiero ver su historial completo** para no pedirle que repita su problema | Agente |
| 3 | Cuando un mensaje lleva más de 10 minutos sin respuesta **quiero recibir una alerta** para cumplir el SLA del cliente | Agente |

Los Jobs to be Done definen las historias de usuario. Sin ellos, se construyen features que nadie usa.

---

## Discovery: Mapa de actores y flujos

```
                     +----------------------------------+
  Agente -----------|                                  |<---- WhatsApp Cloud API
  Admin  -----------|        Sistema de mensajería             |<---- Instagram Graph API
  Contacto (externo) |        (Empresa)               |<---- SMTP (Email)
                     |                                  |---- Stripe (billing)
                     `----------------------------------+
```

**Flujos críticos** (deben funcionar el día 1 del MVP):
- Recepción de mensaje -> asignación -> respuesta del agente
- Login / logout de agente
- Ver historial de conversación de un contacto

**Flujos secundarios** (post-MVP):
- Bot builder / flujos automatizados
- Campañas masivas
- Reportes y métricas

Identificar qué es crítico evita gastar las primeras semanas en flujos que el cliente puede esperar.

---

<!-- _class: lead -->

# Fase Diseño

## Traducir el problema en artefactos técnicos

---

## User Stories

**Formato:** Como `[rol]` quiero `[acción]` para `[beneficio]`

**Ejemplo  -  Asignación automática de conversación:**

> Como **supervisor**, quiero que las conversaciones entrantes se asignen automáticamente al agente disponible con menor carga, para que no tenga que asignarlas manualmente una por una.

**Criterios de aceptación (Given/When/Then):**
- **Given** hay al menos un agente disponible (status = Online)
- **When** llega un mensaje nuevo de WhatsApp a un workspace
- **Then** la conversación queda asignada al agente con menor cantidad de conversaciones abiertas

**Definition of Ready (DoR):** criterios de aceptación escritos  -  wireframe aprobado  -  dependencias identificadas

**Definition of Done (DoD):** código en PR  -  tests pasan  -  code review aprobado  -  staging validado  -  PO aprobó

---

## Wireframes y prototipos

Tres niveles  -  usar el más barato que valide el riesgo:

| Nivel | Herramienta | Cuándo usarlo | Costo |
|-------|------------|---------------|-------|
| Boceto en papel | Lápiz y papel | Primeras ideas, reunión rápida | 5 min |
| Wireframe digital | Excalidraw / draw.io | Validar flujo con el cliente | 30-60 min |
| Prototipo navegable | Figma | Flujos complejos, pruebas de usuario | 2-4h |

**Regla:** detenerse cuando el flujo crítico está validado.

No se pasa a implementación con wireframes que el cliente no ha visto.
El costo de cambiar un wireframe es cero. El costo de cambiar código en producción no lo es.

---

## Decisión de stack  -  criterios ponderados

No elegir por hype. Elegir con criterios explícitos y ponderados:

| Criterio | Peso | Justificación |
|----------|------|--------------|
| Madurez del equipo | 30% | El equipo debe poder mantener el sistema sin el instructor |
| Ecosistema y comunidad | 20% | Librerías, soporte, contrataciones futuras |
| Rendimiento y escala | 20% | ¿El sistema necesita escalar en los próximos 2 años? |
| Costo de infraestructura | 15% | Hosting, licencias, servicios cloud |
| Time-to-market | 15% | Velocidad inicial para llegar al primer cliente |

> Si dos equipos distintos usan la misma tabla y el mismo stack gana en ambos casos,
> la decisión está bien fundamentada. Si no, hay sesgo oculto.

---

## Matriz de decisión de stack  -  Sistema de mensajería

Escala 1-5 por criterio. Puntaje final = suma ponderada.

| Criterio | Peso | .NET 10 | Node.js | Go |
|----------|------|---------|---------|-----|
| Madurez del equipo | 30% | **5** | 3 | 1 |
| Ecosistema | 20% | 5 | 5 | 3 |
| Rendimiento y escala | 20% | 4 | 3 | **5** |
| Costo de infra | 15% | 3 | 4 | 4 |
| Time-to-market | 15% | 4 | 4 | 2 |
| **Puntaje** | | **4.30** | 3.65 | 2.75 |

Go es más eficiente en memoria. Pero el equipo tiene 0 experiencia en Go.
La madurez del equipo (30%) inclina la balanza aunque Go gane en rendimiento.

---

## Decisión de arquitectura  -  escala de complejidad

Pregunta clave: **¿Cuál es el menor nivel de complejidad que nos permite entregar?**

```
Script / Monolito simple
    ↓  señal: el código crece y se enreda
Monolito tradicional (capas)
    ↓  señal: módulos se pisan entre sí, tests lentos
Monolito Modular (Clean Architecture + Vertical Slice)   <-- el sistema de mensajería aquí
    ↓  señal: despliegue coordinado supera el beneficio de escalar partes independientes
Microservicios
```

**Señales reales para subir de nivel:**
- Equipos distintos necesitan deployar de forma independiente
- Un módulo tiene requisitos de escala radicalmente distintos al resto
- El tiempo de build del monolito supera los 10 minutos

Si ninguna señal aplica hoy, **no subas de nivel**. El costo de complejidad innecesaria es real.

---

## ADRs  -  Architecture Decision Records

Un ADR documenta **por qué** se tomó una decisión, no solo cuál fue.

**Formato mínimo:**

```markdown
# ADR-001: Monolito Modular + Clean Architecture + Vertical Slice

**Fecha:** 2026-05-01
**Estado:** Aceptado

## Contexto
Equipo de 4 desarrolladores. Primer producto SaaS. Sin historial de microservicios.
Necesitamos iterar rápido y mantener la deuda técnica bajo control.

## Decisión
Usar Monolito Modular con Clean Architecture y organización por Vertical Slice.
Cada feature vive en su propia carpeta con su Command/Query, Handler y Response.

## Consecuencias
(+) Un solo repositorio, un solo deploy, menor complejidad operacional.
(+) Módulos desacoplados permiten migrar a microservicios si fuera necesario.
(-) Disciplina necesaria para no crear dependencias directas entre módulos.
```

Documentar decisiones es tan importante como el código. Sin ADRs, el equipo repite debates resueltos.

---

<!-- _class: lead -->

# Modelo C4

## Comunicar arquitectura a distintos niveles de detalle

---

## C4 Nivel 1: Contexto del sistema

Responde: ¿qué hace el sistema y quién interactúa con él?

```
  [Agente / Supervisor]              [Contacto externo]
         |                                  |
         | usa                              | escribe por

  +-----------------------------------------------------+
  |                   Sistema de mensajería                     |
  |           Sistema de gestión conversacional         |
  |              multi-canal para la empresa             |
  `-----------------------------------------------------+
         |              |              |           |

  WhatsApp        Instagram       Stripe        SMTP
  Cloud API       Graph API       (billing)     (email)
```

El nivel 1 es para el cliente y stakeholders. Sin tecnicismos. Solo actores y sistemas externos.

---

## C4 Nivel 2: Contenedores

Responde: ¿cuáles son los componentes desplegables y cómo se comunican?

```
  [Agente / Admin]
         | HTTPS

  +------------------+     REST/JSON      +---------------------+
  |   React SPA      |------------------|   .NET 10 Web API   |
  |   (Vite  -  Tw4)   |<-----------------|   (Clean Arch)      |
  `------------------+   WebSocket/      `----------+----------+
                         SignalR                     |
                                          +----------+----------+

                                     PostgreSQL    Redis       S3 / MinIO
                                     (datos)      (cache +    (archivos)
                                                   backplane)
```

Cada contenedor tiene su tecnología explícita. El nivel 2 es para el equipo técnico completo.

---

## C4 Nivel 3: Componentes del API

Responde: ¿cómo está organizado el interior del contenedor más importante?

```
  +-----------------------------------------------------------------+
  |                        .NET 10 Web API                          |
  |                                                                 |
  |  +-------------+  +-------------------+  +------------------+  |
  |  | Auth Module |  |Conversations Mod. |  |  Contacts Mod.   |  |
  |  | JWT  -  BCrypt|  |Bandeja  -  Asignac. |  |  CRUD  -  CSV imp. |  |
  |  `------+------+  `--------+----------+  `--------+---------+  |
  |         |                  |                       |            |
  |  +------+------------------+-----------------------+---------+  |
  |  |              Shared Kernel / Common Library               |  |
  |  |         Result Pattern  -  Middleware  -  EF Core             |  |
  |  `-----------------------------------------------------------+  |
  |                                                                 |
  |  +-------------+  +-----------------+                          |
  |  |Channels Mod.|  |  Bots Module    |                          |
  |  |WA  -  IG  -  Em.|  |  Flows  -  Rules  |                          |
  |  `-------------+  `-----------------+                          |
  `-----------------------------------------------------------------+
```

Los módulos se comunican a través de interfaces o eventos, nunca referencias directas.

---

## Diagramas ER  -  entidades del CRM

Notación crow's foot simplificada. Cardinalidades clave:

```
Workspace ||--o{ User          : "tiene"
Workspace ||--o{ Contact        : "tiene"
Workspace ||--o{ Channel        : "conecta"
Channel   ||--o{ Conversation   : "recibe"
Contact   ||--o{ Conversation   : "inicia"
User      ||--o{ Conversation   : "atiende (asignado)"
Conversation ||--o{ Message     : "contiene"
```

**Atributos clave por entidad:**

| Entidad | Atributos principales |
|---------|----------------------|
| `Workspace` | Id, Name, Slug, Plan, CreatedAt |
| `User` | Id, WorkspaceId, Email, Role, PasswordHash |
| `Contact` | Id, WorkspaceId, Phone, Email, Name, Tags |
| `Conversation` | Id, WorkspaceId, ContactId, AssignedUserId, Status, Channel |
| `Message` | Id, ConversationId, Body, Direction, SentAt, MediaUrl |

---

## Diagramas de secuencia  -  recepción de mensaje WhatsApp

Flujo crítico: desde webhook hasta notificación en tiempo real al agente.

```
WhatsApp    .NET API        ConversationService   SignalR Hub   Agente (browser)
    |            |                   |                |               |
    |--POST /webhook--              |                |               |
    |            |--ValidateHmac--  |                |               |
    |            |<--OK-------------|                |               |
    |            |--ProcessMessage-|                |               |
    |            |                  |--FindOrCreate--               |
    |            |                  |   Conversation                 |
    |            |                  |--AssignToAgent-               |
    |            |                  |--SaveMessage--|               |
    |            |                  |                |--SendAsync-- |
    |            |                  |                |  NewMessage   |
    |<--200 OK---|                  |                |               |
```

El webhook debe responder `200 OK` en menos de 5 segundos o WhatsApp reintenta. El procesamiento pesado va en un BackgroundService separado.

---

<!-- _class: lead -->

# Estimación

## Cuantificar el esfuerzo sin mentirse

---

## Story Points y Planning Poker

Por qué **no** estimar en horas:
- Las horas ignoran la incertidumbre y la complejidad relativa
- El equipo miente (consciente o no) para quedar bien
- Los puntos miden esfuerzo + complejidad + riesgo juntos

**Escala de Fibonacci:** 1  -  2  -  3  -  5  -  8  -  13  -  21

> Si una historia es **> 8 puntos**, partirla. Siempre.

**Ejemplo  -  Planning Poker del CRM:**

| Historia | Estimación del equipo | Puntos finales |
|----------|-----------------------|----------------|
| Login con JWT (sign-in + refresh) | 3, 3, 5, 3 | 3 |
| Asignación automática de conversaciones | 5, 8, 8, 13 | 8 |
| Importar contactos desde CSV con validación | 13, 13, 21, 8 | **partir** -> 2 historias de 8 |

La dispersión alta en la última historia revela que el equipo no está alineado en el alcance. Partir antes de estimar.

---

## Técnica de 3 puntos  -  PERT

Cuando la incertidumbre es alta, los 3 puntos dan una estimación más honesta:

**Fórmula:** `E = (O + 4M + P) / 6`

Donde: O = Optimista  -  M = Probable  -  P = Pesimista

**Ejemplo  -  Feature: importación CSV de contactos**

| Escenario | Días | Detalle |
|-----------|------|---------|
| Optimista (O) | 2 | CSV limpio, sin validaciones complejas, sin duplicados |
| Probable (M) | 4 | Validaciones básicas, detección de duplicados por email |
| Pesimista (P) | 10 | CSVs con encoding raro, miles de filas, transacciones largas |

`E = (2 + 4×4 + 10) / 6 = 28 / 6 ≈ 4.7 días`

> El escenario pesimista que el equipo no menciona siempre existe. La técnica obliga a nombrarlo.

---

## Priorización  -  MoSCoW y RICE

### MoSCoW  -  para el primer sprint

| Categoría | Criterio | Ejemplo CRM |
|-----------|----------|-------------|
| **Must** | Sin esto no hay producto | Auth, bandeja de entrada, WhatsApp |
| **Should** | Alta prioridad pero no bloquea | Instagram, asignación automática |
| **Could** | Deseable si hay tiempo | Campañas, reportes básicos |
| **Won't** | Fuera del alcance actual | IA generativa, llamadas de voz |

### RICE  -  para roadmap de mediano plazo

`RICE = (Reach × Impact × Confidence) / Effort`

- Reach: cuántos usuarios lo usan por mes
- Impact: 3=masivo, 2=alto, 1=medio, 0.5=bajo
- Confidence: % de certeza de la estimación
- Effort: personas-mes

Usar **MoSCoW** para definir el sprint 1. Usar **RICE** para priorizar el roadmap de los próximos 3 meses.

---

<!-- _class: lead -->

# MVP

## La versión mínima que entrega valor real

---

## MVP  -  qué es y qué NO es

**MVP = Minimum Viable Product:** la versión más pequeña del sistema que resuelve el problema principal y puede llegar a manos de un usuario real.

| MVP real | NO es un MVP |
|----------|-------------|
| Funciona en producción | Prototipo con datos hardcodeados |
| Resuelve el flujo crítico completo | Solo el 20% de un feature a medias |
| El usuario puede completar su Job to be Done | Demo que solo el instructor sabe operar |
| Recoge feedback accionable | Presentación de PowerPoint con screenshots |

**MVP del sistema de mensajería:**
1. Auth (registro + login + JWT + refresh)
2. Un canal conectado (WhatsApp Cloud API)
3. Bandeja de entrada básica (lista de conversaciones + chat)
4. Asignación manual de conversación a agente

Con esto, un supervisor puede recibir mensajes de WhatsApp y asignarlos a su equipo. Eso es suficiente para validar el producto con el primer cliente.

---

<!-- _class: lead -->

# Implementación estructurada

## Del backlog al sistema en producción

---

## Roadmap técnico por fases

Cada fase termina con algo que el cliente puede ver y validar.

| Fase | Semanas | Entregables | Qué puede ver el cliente |
|------|---------|-------------|--------------------------|
| **Fase 1** | 1-4 | Auth + estructura del repo + CI pipeline | Login funcional en staging |
| **Fase 2** | 5-10 | WhatsApp + Instagram + conversaciones + bandeja | Agente recibe y responde mensajes |
| **Fase 3** | 11-16 | Contactos + etiquetas + bots básicos + búsqueda | Gestión completa de contactos |
| **Fase 4** | 17-22 | DevOps completo + observabilidad + deploy prod | Sistema desplegado y monitoreado |

> Si una fase no termina en algo que el cliente puede usar, es demasiado grande. Partirla.

Las fases NO son silos; el código del núcleo (auth, EF Core, logging, CI) se hace en Fase 1 para no arrastrar esa deuda al resto.

---

## Git Flow aplicado a la planeación

Las ramas reflejan el roadmap. Nada de `main` directo después del día 1.

```
main        ------------------------------------------ v1.0.0 -- v1.1.0
                                                          ^
release/v1.0 -------------------------------------------+
                                              ^
develop     ----------------------------------+----------------------
               ^      ^       ^
feature/...----+      |       |
feature/...-----------+       |
bugfix/...--------------------+
```

**SemVer en la práctica:**
- `v1.0.0` -> MVP completo llega a producción (Fase 1 completa)
- `v1.1.0` -> primera feature nueva post-MVP (Instagram conectado)
- `v1.1.1` -> bug crítico corregido en producción
- `v2.0.0` -> cambio de API que rompe compatibilidad (raro, deliberado)

---

## Definition of Done real

Checklist de un ticket antes de cerrar el PR. Sin excepciones.

```
[ ] Código en Pull Request (rama feature/* -> develop)
[ ] Tests unitarios del caso nuevo escritos y en verde
[ ] Tests de integración pasan en el pipeline
[ ] Code review aprobado por al menos 1 persona
[ ] Build de CI pasa sin warnings
[ ] Integración probada en ambiente staging
[ ] No hay secretos hardcodeados en el código
[ ] Documentación actualizada si cambió contrato de API
[ ] Product Owner validó el criterio de aceptación en staging
[ ] Ticket movido a Done en el board
```

Un ticket que no cumple el DoD no se cierra. Se regresa a In Progress.
El DoD no se negocia por presión de tiempo; si hay presión, se reduce el alcance del ticket.

---

## Deuda técnica  -  tipos y gestión

### Tipos

| Tipo | Descripción | Ejemplo en el CRM |
|------|-------------|-------------------|
| **Deliberada** | Decisión consciente de cortar esquinas | Usar in-memory queue en MVP en vez de Redis |
| **Accidental** | No sabíamos que había una mejor forma | Usar `async void` en un event handler |

### Cómo registrarla

```
Título: [tech-debt] Reemplazar in-memory queue por Redis BackgroundService
Estimación del costo actual: 4h
Costo de NO pagarlo: bloquea escala horizontal en Fase 4
Prioridad: pagar más adelante (sprint de canales y background services)
```

**Regla:** pagar la deuda técnica cuando **bloquea una feature nueva**, no antes ni mucho después.
El backlog de tech-debt con etiqueta `tech-debt` y estimación visible es suficiente.

---

## Métricas DORA

Cuatro métricas que miden la salud del proceso de entrega, no solo del código.

| Métrica | Qué mide | Meta élite |
|---------|----------|------------|
| **Deployment Frequency** | Con qué frecuencia se despliega a producción | Varias veces al día |
| **Lead Time for Changes** | Tiempo desde commit hasta producción | < 1 hora |
| **Change Failure Rate** | % de deployments que causan incidentes | < 5% |
| **MTTR** | Tiempo medio de recuperación ante un incidente | < 1 hora |

> **Lead Time for Changes** es la más relevante para startups que necesitan iterar rápido.
> Si el Lead Time es alto, el pipeline tiene un cuello de botella que bloquea el aprendizaje.

Un equipo que despliega en horas aprende en horas. Uno que despliega en semanas, aprende en semanas.

---

## Herramientas por fase

| Fase | Herramienta | Para qué |
|------|------------|----------|
| **Discovery** | Miro, Notion | Mapas de actores, brief colaborativo, Jobs to be Done |
| **Diseño** | Figma | Wireframes y prototipos navegables |
| **Diseño técnico** | Excalidraw, draw.io | Diagramas C4, ER, secuencia |
| **Planeación** | Linear, GitHub Projects | Backlog, sprints, priorización |
| **Implementación** | VS Code, Rider, GitHub | Código, PRs, code review |
| **AI asistido** | Claude Code + CLAUDE.md | Scaffolding, review, documentación |
| **CI/CD** | GitHub Actions | Build, tests, deploy automático |
| **Observabilidad** | Serilog + Seq, Grafana | Logs estructurados, dashboards de métricas |
| **Secretos** | AWS Secrets Manager, .env local | Configuración sin hardcodear credenciales |

No usar todas las herramientas desde el día 1. Incorporarlas cuando el equipo las necesite.

---

## Checklist: de la idea a producción

Artefactos que deben existir antes de considerar el sistema "planificado correctamente":

```
[ ] Brief de producto (problema  -  usuario  -  métrica  -  restricciones  -  fuera de alcance)
[ ] Jobs to be Done documentados (mínimo 3 por rol)
[ ] User Stories con criterios Given/When/Then
[ ] Wireframes del flujo crítico validados con el cliente
[ ] ADR-001: decisión de arquitectura documentada
[ ] Diagrama C4 Nivel 1 (contexto) y Nivel 2 (contenedores)
[ ] Diagrama ER con entidades principales y cardinalidades
[ ] Estimación por historias (story points o 3 puntos)
[ ] MVP claramente definido y diferenciado del producto completo
[ ] Roadmap por fases con entregables observables
[ ] DoD acordada por todo el equipo
[ ] Pipeline CI/CD corriendo desde el sprint 1
```

Si falta alguno de estos antes de empezar a codificar, hay riesgo de retrabajo costoso.

---

<!-- _class: lead -->

# "El mejor código es el que resuelve el problema correcto."

## La planeación define cuál es ese problema.

**Próximo paso:** aplicar este proceso al sistema de mensajería  -
comenzando por el Brief y el ADR-001 de arquitectura.
