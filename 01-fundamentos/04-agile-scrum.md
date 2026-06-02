# 04 — Agile y Scrum: Metodología de Desarrollo Iterativo

Agile es un conjunto de valores y principios para el desarrollo de software que prioriza la colaboración, la entrega incremental y la adaptación al cambio. Scrum es el framework ágil más utilizado — una implementación concreta de los principios Agile con roles, eventos y artefactos definidos.

> Fuente: *Agile Manifesto* (Beck et al., 2001), *Scrum Guide* (Schwaber & Sutherland, 2020), *Scrum: The Art of Doing Twice the Work in Half the Time* — Sutherland

---

## El Manifiesto Ágil — 4 valores

```
Individuos e interacciones   SOBRE   procesos y herramientas
Software funcionando         SOBRE   documentación exhaustiva
Colaboración con el cliente  SOBRE   negociación de contratos
Responder al cambio          SOBRE   seguir un plan
```

**Importante:** los ítems de la derecha tienen valor — el Manifiesto dice que los de la izquierda se valoran MÁS, no que los otros no importen.

---

## 12 Principios Ágiles — Resumen práctico

| Principio | Aplicación real |
|-----------|----------------|
| Entregar software frecuentemente | Sprint de 2 semanas, demo al final |
| El cambio es bienvenido, incluso tarde | Backlog refinement continuo |
| Software funcionando es la medida de progreso | Demo > PPT de estado |
| Colaboración diaria entre negocio y devs | Daily standup con el PO visible |
| Proyectos se construyen con personas motivadas | Autonomía + propósito + maestría |
| La conversación cara a cara es la más eficiente | Async para updates, sync para decisiones |
| Simplicidad — maximizar el trabajo no hecho | YAGNI como principio de diseño |
| Reflexión y ajuste continuo | Retrospectiva al final de cada sprint |

---

## Scrum — Los 3 Roles

### Product Owner (PO)
- Dueño del Product Backlog — decide qué se construye y en qué orden
- Maximiza el valor del producto para el negocio
- Único punto de decisión de prioridad (no hay comité de prioridades)
- **No es:** un proxy entre usuarios y devs — debe tener poder real de decisión

### Scrum Master (SM)
- Facilita el proceso Scrum — no es el jefe del equipo
- Elimina impedimentos que bloquean al equipo
- Protege al equipo de interrupciones externas durante el sprint
- **No es:** un project manager tradicional

### Development Team
- Autoorganizado — decide cómo construir lo que el PO prioriza
- Cross-functional — tiene todas las habilidades necesarias (dev, QA, diseño)
- Tamaño ideal: 3-9 personas (comunicación manejable)
- **No hay:** sub-equipos de frontend/backend dentro del equipo Scrum

---

## Scrum — Los 5 Eventos

### 1. Sprint
- Iteración de duración fija: 1-4 semanas (típicamente 2)
- No se cambia el scope durante el sprint (salvo excepciones extremas)
- Al final del sprint debe haber un Increment potencialmente entregable

### 2. Sprint Planning
- Qué: PO presenta las historias de mayor prioridad
- Quién: PO + SM + Dev Team
- Resultado: Sprint Backlog — historias comprometidas para el sprint
- Duración máx: 2 horas por semana de sprint (sprint de 2 sem → 4 horas)

### 3. Daily Standup (Daily Scrum)
- 15 minutos, misma hora, mismo lugar
- Para los DEVS — no es un reporte de estado para el PO
- Preguntas orientadoras: ¿Qué hice ayer? ¿Qué haré hoy? ¿Hay algún impedimento?
- El SM facilita, no interroga

### 4. Sprint Review (Demo)
- El Dev Team demuestra el Increment al PO y stakeholders
- Software funcionando — no slides
- El PO acepta o rechaza historias; el backlog se ajusta
- Duración máx: 1 hora por semana de sprint

### 5. Sprint Retrospectiva
- Solo el equipo (PO puede estar si hay confianza; SM siempre)
- 3 preguntas: ¿Qué salió bien? ¿Qué mejorar? ¿Qué hacemos diferente?
- Resultado: 1-3 acciones concretas con dueño y fecha
- Duración: 45 min para sprints de 2 semanas

---

## Scrum — Los 3 Artefactos

### Product Backlog
- Lista ordenada de todo lo que podría necesitar el producto
- El PO es el dueño — puede añadir, reordenar, eliminar
- Items en formato User Story o tarea técnica
- El backlog nunca se "termina" — evoluciona con el producto

### Sprint Backlog
- Subconjunto del Product Backlog comprometido para el sprint
- Incluye el plan de cómo el equipo logrará el Objetivo del Sprint
- Solo el Dev Team puede modificarlo durante el sprint

### Increment
- Suma de todos los ítems del Product Backlog completados durante el sprint
- Debe cumplir la Definición de Done para ser un Increment válido
- Potencialmente entregable — aunque el PO decida no desplegarlo

---

## Definición de Done (DoD)

La DoD es el acuerdo del equipo sobre qué significa "terminado". Sin DoD, cada dev define "listo" de forma diferente.

### Ejemplo de DoD para CRM Messenger

```markdown
# Definición de Done — CRM Messenger

Una historia está "Done" cuando:
- [ ] El código compila sin warnings
- [ ] Tests unitarios y de integración pasan
- [ ] Cobertura de tests no baja del 70% en el módulo afectado
- [ ] SonarQube no introduce nuevos code smells críticos
- [ ] Code review aprobado por al menos 1 par (Jairo o peer)
- [ ] PR mergeado a `develop` (no a `main` directamente)
- [ ] ADR escrito si se tomó una decisión arquitectural
- [ ] El feature fue probado en ambiente local contra datos reales
```

---

## User Stories — Formato y criterios

### Formato estándar

```
Como [rol de usuario]
quiero [acción o funcionalidad]
para [beneficio o valor de negocio]
```

**Ejemplo real:**
```
Como agente de soporte
quiero ver todas las conversaciones sin responder de los últimos 7 días
para priorizar mi bandeja de entrada sin perder mensajes urgentes
```

### Criterios de aceptación

Los criterios de aceptación definen cuándo la historia está completa:

```
Dado que soy un agente autenticado
Cuando accedo a "Mi bandeja"
Entonces veo las conversaciones ordenadas por fecha de último mensaje (DESC)
Y puedo filtrar por estado: Sin responder / En progreso / Cerrado
Y el contador de "Sin responder" se actualiza en tiempo real (SignalR)
```

---

## Kanban vs Scrum

| Aspecto | Scrum | Kanban |
|---------|-------|--------|
| Iteraciones | Sprints de duración fija | Flujo continuo |
| Roles definidos | PO, SM, Dev Team | Ninguno obligatorio |
| Planning | Sprint Planning formal | Tomar del backlog cuando hay capacidad |
| WIP limits | No definido por el framework | Límite explícito por columna |
| Mejor para | Productos con releases planeadas | Soporte, bugfix, operaciones continuas |
| Métricas clave | Velocity, sprint burndown | Lead time, cycle time, throughput |

**Conclusión:** Scrum para desarrollo de features nuevo; Kanban para mantenimiento y soporte.

---

## Scrum en un equipo de 4-6 devs con sprints de 2 semanas

### Cadencia tipo

```
Lunes (inicio de sprint):
  09:00 — Sprint Planning (2-3 horas)

Martes a Jueves:
  09:00 — Daily Standup (15 min)

Viernes (fin de sprint):
  15:00 — Sprint Review / Demo (1 hora)
  16:00 — Retrospectiva (45 min)

Miércoles (mid-sprint):
  Backlog Refinement — grooming de historias para el próximo sprint (1 hora)
```

---

## Errores comunes en equipos Scrum

```
❌ Daily como reporte de estado para el manager
   → Fix: El SM recuerda que el Daily es para el Dev Team, no para informar

❌ Sprint sin DoD — "listo" significa cosas diferentes para cada dev
   → Fix: Acordar la DoD en la primera Retrospectiva y hacerla visible

❌ PO ausente — toma decisiones 3 días después de la pregunta
   → Fix: PO disponible diariamente para clarificar, no solo en Planning/Review

❌ Sprint Backlog cambia constantemente mid-sprint
   → Fix: Todo lo nuevo va al Product Backlog; el sprint actual no se interrumpe

❌ Retrospectiva omitida "porque no hay tiempo"
   → Fix: Sin retro, los mismos problemas se repiten indefinidamente

❌ "Somos ágiles" = sin documentación, sin tests, sin arquitectura
   → Fix: Agile no significa ad-hoc; significa iterativo con calidad sostenida
```
