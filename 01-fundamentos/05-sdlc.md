# 05 — SDLC: Ciclo de Vida del Software

El SDLC (Software Development Life Cycle) es el proceso estructurado que guía cómo se planifica, construye, prueba, despliega y mantiene un sistema de software. No es un modelo único: es un conjunto de fases cuya ejecución varía según el modelo adoptado (Waterfall, Ágil, DevOps).

---

## 1. Qué es el SDLC y por qué importa

Construir software sin un proceso definido produce:

- Requerimientos que cambian a mitad del desarrollo sin trazabilidad
- Bugs que llegan a producción porque no hubo fase de pruebas
- Estimaciones imposibles porque nadie documentó el alcance
- Sistemas que nadie puede mantener porque el conocimiento está en la cabeza de una sola persona

El SDLC responde tres preguntas fundamentales:

```
¿Qué vamos a construir?       → fase de análisis y diseño
¿Cómo lo vamos a construir?   → fase de implementación y testing
¿Cómo sabemos que está listo? → criterios de aceptación y despliegue
```

El SDLC no es burocracia. Es el conjunto mínimo de conversaciones y artefactos que evitan construir lo incorrecto o de forma insostenible.

---

## 2. Fases clásicas

### 2.1 Planificación

Decide qué se va a construir y si vale la pena construirlo.

| Actividad | Producto |
|-----------|---------|
| Definir alcance y objetivos | Documento de alcance / Project Charter |
| Identificar stakeholders | Lista de participantes y roles |
| Estimación inicial (esfuerzo, costo, riesgo) | Plan de proyecto o backlog inicial |
| Definir criterios de éxito | KPIs del producto |

**Quién participa:** Product Owner, Tech Lead, consultor/arquitecto, cliente.

### 2.2 Análisis de requerimientos

Traduce los objetivos de negocio en requerimientos concretos del sistema.

| Actividad | Producto |
|-----------|---------|
| Entrevistas y talleres con el cliente | User stories / casos de uso |
| Separar requerimientos funcionales (qué hace) y no funcionales (cómo se comporta) | PRD (Product Requirements Document) |
| Priorización (MoSCoW: Must / Should / Could / Won't) | Backlog priorizado |

**Quién participa:** Product Owner, analista, cliente, equipo de desarrollo.

### 2.3 Diseño

Define la arquitectura y el modelo de datos antes de escribir código de producción.

| Actividad | Producto |
|-----------|---------|
| Elegir arquitectura (Clean Architecture, monolito modular, etc.) | ADR (Architecture Decision Record) |
| Diseñar el modelo de datos | ERD (Entity Relationship Diagram) |
| Definir contratos de API | API Spec (OpenAPI / Swagger) |
| Diseñar flujos de usuario | Wireframes / mockups |

**Quién participa:** Tech Lead, arquitecto, desarrolladores senior.

### 2.4 Implementación

Escribir el código siguiendo el diseño acordado.

| Actividad | Producto |
|-----------|---------|
| Desarrollar features por iteraciones | Pull Requests revisados |
| Aplicar convenciones (Clean Code, SOLID, SemVer) | Código en repositorio |
| Code review sistemático | Feedback documentado en PRs |

**Quién participa:** todos los desarrolladores, Tech Lead como reviewer.

### 2.5 Testing

Verificar que el sistema cumple los requerimientos y no introduce regresiones.

| Tipo de test | Qué valida |
|-------------|-----------|
| Unit tests | Lógica de negocio aislada (Handlers, Value Objects) |
| Integration tests | Flujo completo desde Controller hasta base de datos |
| End-to-end (E2E) | Flujos de usuario en el browser |
| Regression testing | Que una nueva feature no rompió lo anterior |

**Quién participa:** desarrolladores (unit/integration), QA engineer (E2E, exploración).

### 2.6 Despliegue

Llevar el sistema del repositorio a un ambiente real (staging o producción).

| Actividad | Producto |
|-----------|---------|
| Pipeline CI/CD automatizado | GitHub Actions workflow |
| Estrategia de despliegue (blue/green, rolling, canary) | Runbook de despliegue |
| Variables de entorno y secretos en el servidor | Configuración en AWS Secrets Manager |

**Quién participa:** DevOps engineer, Tech Lead, desarrolladores.

### 2.7 Mantenimiento

Sostener el sistema en producción: correcciones, mejoras y monitoreo.

| Actividad | Producto |
|-----------|---------|
| Monitorear logs y métricas | Dashboard de observabilidad (Grafana, CloudWatch) |
| Corregir bugs de producción (hotfix) | PATCH release con SemVer |
| Agregar funcionalidad post-lanzamiento | MINOR release con SemVer |
| Gestionar deuda técnica | Tickets de refactoring priorizados |

**Quién participa:** todos; responsabilidad compartida del equipo.

---

## 3. Modelos de SDLC

### 3.1 Waterfall (Cascada)

Las fases se ejecutan en secuencia estricta. No se pasa a la siguiente hasta completar la anterior.

```
Planificación → Análisis → Diseño → Implementación → Testing → Despliegue → Mantenimiento
```

**Cuándo aplica:**
- Requerimientos completamente estables y documentados desde el inicio
- Proyectos con contratos de precio fijo y alcance cerrado
- Software embebido / firmware con certificación regulatoria

**Cuándo NO aplica:**
- Cualquier producto donde el cliente no sabe exactamente qué quiere
- Startups y proyectos de producto digital (el 90% de los proyectos modernos)

**El problema central:** el cliente ve el producto funcional solo al final; cuando algo está mal, es muy costoso cambiar.

### 3.2 Iterativo

El sistema se construye en ciclos. Cada iteración produce un incremento funcional que se puede mostrar al cliente.

```
Iteración 1: [Análisis] → [Diseño] → [Impl] → [Test] → demo
Iteración 2: [Análisis] → [Diseño] → [Impl] → [Test] → demo
...
```

**Ventaja:** retroalimentación temprana. El cliente puede corregir el rumbo antes de que sea tarde.

### 3.3 Ágil — Scrum

Iterativo con sprints de 1-4 semanas, roles definidos y ceremonias.

```
Product Backlog → Sprint Planning → Sprint (1-2 semanas) → Review → Retrospectiva → siguiente Sprint
```

| Rol | Responsabilidad |
|-----|----------------|
| Product Owner | Prioriza el backlog, representa al cliente |
| Scrum Master | Facilita ceremonias, elimina impedimentos |
| Development Team | Planifica y ejecuta el sprint |

**Ceremonias clave:**

| Ceremonia | Cuándo | Para qué |
|-----------|--------|---------|
| Sprint Planning | Inicio del sprint | Qué items del backlog entran al sprint |
| Daily Stand-up | Cada día (15 min) | ¿Qué hice ayer? ¿Qué haré hoy? ¿Hay bloqueos? |
| Sprint Review | Fin del sprint | Demo al cliente — ¿cumple la expectativa? |
| Retrospectiva | Fin del sprint | ¿Qué salió bien? ¿Qué mejorar? |

### 3.4 DevOps continuo

Extiende Ágil con automatización de despliegue y observabilidad. El objetivo: reducir el tiempo entre "código en el repo" y "valor en producción".

```
Plan → Code → Build → Test → Release → Deploy → Operate → Monitor → (Plan...)
```

Prácticas clave:
- CI/CD: cada PR dispara tests automáticos y despliegue a staging
- Infrastructure as Code (Terraform, Bicep): el servidor se define en código
- Observabilidad: logs estructurados + métricas + trazas distribuidas
- Feature flags: desplegar sin activar → activar sin desplegar

---

## 4. Documentación por fase

| Fase | Artefacto | Formato típico |
|------|----------|----------------|
| Planificación | Project Charter, RFC | Documento Word / Notion |
| Análisis | PRD (Product Requirements Document) | Markdown en el repo |
| Diseño | ADR (Architecture Decision Record) | `docs/decisions/ADR-001.md` |
| Diseño | ERD (Entity Relationship Diagram) | dbdiagram.io / draw.io |
| Diseño | API Spec | `openapi.yaml` (Swagger) |
| Implementación | Pull Requests + comentarios | GitHub / Azure DevOps |
| Despliegue | Runbook | `docs/runbooks/deploy.md` |
| Mantenimiento | Postmortem | `docs/postmortems/YYYY-MM-DD.md` |

### ADR — el artefacto más valioso

Un ADR documenta una decisión de arquitectura: qué se decidió, por qué, y qué alternativas se descartaron.

```markdown
# ADR-001: Arquitectura Monolito Modular

## Contexto
Equipo de 4 personas, producto nuevo sin carga conocida en producción.

## Decisión
Monolito modular con Clean Architecture.

## Alternativas descartadas
- Microservicios: overhead de operaciones injustificado para el tamaño del equipo.

## Consecuencias
Fácil de migrar a microservicios cuando la carga lo justifique.
```

---

## 5. SDLC en equipos pequeños — qué simplificar

Un equipo de 4-6 personas no puede aplicar un SDLC enterprise completo. La clave es **mantener la intención de cada fase sin su overhead burocrático**.

| Fase formal | Versión simplificada para equipo pequeño |
|------------|------------------------------------------|
| Planificación | Backlog en GitHub Projects / Notion con prioridad |
| PRD completo | User story con criterios de aceptación en el issue |
| ADRs formales | Un `.md` corto en `/docs/decisions/` por decisión importante |
| ERD completo | Comentarios en las EntityTypeConfigurations de EF Core |
| Sprint Planning formal | Reunión de 30 min para definir qué entra en la semana |
| Daily Stand-up | Mensaje de texto diario en el canal del equipo |
| Code review formal | PR con al menos 1 reviewer antes de merge a `main` |
| Runbook | README de despliegue en el repo |

**Lo que NUNCA se simplifica:**
- Code review antes de merge a `main`
- Tests automatizados en el pipeline
- Variables de entorno en secretos (nunca en el repo)
- SemVer en cada release

---

## 6. Métricas de salud del SDLC

Las métricas del framework DORA (DevOps Research and Assessment) son el estándar de la industria para medir qué tan bien funciona el proceso de entrega.

| Métrica | Qué mide | Elite | Alta | Media | Baja |
|---------|---------|-------|------|-------|------|
| **Deployment Frequency** | Qué tan seguido se despliega a producción | Múltiples veces/día | 1/semana | 1/mes | < 1/mes |
| **Lead Time for Changes** | Tiempo desde commit hasta producción | < 1 hora | < 1 día | 1 semana–1 mes | > 6 meses |
| **Change Failure Rate** | % de despliegues que causan incidentes | < 5% | < 10% | < 15% | > 15% |
| **MTTR** (Mean Time to Restore) | Tiempo promedio en restaurar producción | < 1 hora | < 1 día | < 1 semana | > 1 semana |

### Lead Time vs Cycle Time

```
Cycle Time: tiempo desde que se empieza a trabajar en una tarea hasta que se mergea
Lead Time:  tiempo desde que la tarea se crea (o el cliente la pide) hasta producción

Lead Time = Cycle Time + tiempo en la cola de espera
```

Un Cycle Time corto con Lead Time largo indica cuellos de botella: revisiones lentas, despliegues manuales, aprobaciones.

---

## 7. Deuda técnica y el SDLC

La deuda técnica es el costo futuro de atajos tomados hoy. No es inherentemente mala — a veces es una decisión deliberada. El problema es cuando se acumula sin control.

### En qué fases aparece cada tipo de deuda

| Tipo de deuda | Fase donde se origina | Ejemplo |
|--------------|-----------------------|---------|
| Deuda de diseño | Diseño / Implementación | No separar dominios → módulos acoplados |
| Deuda de código | Implementación | Métodos de 300 líneas, sin extraer |
| Deuda de testing | Testing omitido | Sin tests → cada cambio es un riesgo |
| Deuda de documentación | Todas | Sin ADRs → nadie sabe por qué se tomó una decisión |
| Deuda de infraestructura | Despliegue | Deploy manual → proceso irreproducible |

### El modelo de cuadrantes (Ward Cunningham + Martin Fowler)

```
                    DELIBERADA              ACCIDENTAL
IMPRUDENTE    "No hay tiempo para        "¿Qué son los patrones
              diseñar bien"              de diseño?"

PRUDENTE      "Hay que salir ahora,      "Ahora entiendo cómo
              luego refactorizamos"      debió haber sido"
```

La deuda **prudente y deliberada** es aceptable si se documenta y se paga pronto.
La deuda **imprudente** destruye la velocidad del equipo con el tiempo.

### Cómo prevenirla en el SDLC

```
Planificación   → Reservar capacidad para refactoring (20% del sprint)
Diseño          → ADRs antes de implementar cambios grandes
Implementación  → Definition of Done incluye: tests + review + documentación
Code Review     → No mergear sin test + sin pasar el linter
Retrospectiva   → Un item de deuda técnica por sprint como mínimo
```

### La regla del boy scout aplicada al SDLC

> Deja el código mejor de como lo encontraste.

No es refactoring masivo: es agregar un test faltante, extraer un método largo, mejorar un nombre. Aplicado consistentemente en cada PR, previene que la deuda se acumule.

---

## Referencias

- Fowler, M. — *Refactoring: Improving the Design of Existing Code* (2018) — Ch.1 "What Is Refactoring"
- Forsgren, N. et al. — *Accelerate: The Science of Lean Software and DevOps* (2018) — métricas DORA
- Bass, L. et al. — *Software Architecture in Practice* (4th ed.) — SDLC y decisiones de arquitectura
- `dev-notes/01-fundamentos/01-solid.md` — principios que previenen deuda de diseño
- `dev-notes/01-fundamentos/02-clean-code.md` — prevención de deuda de código
- `dev-notes/01-fundamentos/03-refactoring.md` — cómo pagar la deuda técnica existente

---

## Glosario

| Término | Definición |
|---------|-----------|
| SDLC | Software Development Life Cycle — proceso estructurado que guía la planificación, construcción, prueba, despliegue y mantenimiento de un sistema |
| Waterfall | modelo de SDLC donde las fases se ejecutan en secuencia estricta, sin iteración |
| Iterativo | modelo de SDLC que construye el sistema en ciclos, produciendo un incremento funcional al final de cada uno |
| ADR | Architecture Decision Record — documento que registra una decisión de arquitectura, sus alternativas y consecuencias |
| PRD | Product Requirements Document — artefacto que traduce los objetivos de negocio en requerimientos concretos del sistema |
| CI/CD | Continuous Integration / Continuous Delivery — práctica de integrar y desplegar cambios de forma automática y frecuente |
| Deployment Frequency | métrica DORA que mide la frecuencia con la que el equipo despliega a producción |
| Lead Time for Changes | métrica DORA que mide el tiempo entre un commit y su llegada a producción |
| MTTR | Mean Time to Restore — tiempo promedio que tarda el equipo en restaurar el servicio tras un incidente |
| Deuda técnica | costo futuro de atajos tomados en el presente; se acumula como intereses sobre cada nueva funcionalidad |
| Feature flag | mecanismo para desplegar código sin activarlo, permitiendo activar o desactivar funcionalidades sin redespliegue |
| Postmortem | documento que analiza un incidente de producción: causas, línea de tiempo e impacto para evitar su recurrencia |

---

*Rogelio Arriaga Gonzalez*
