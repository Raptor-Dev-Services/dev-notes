# 07 — Gestión de Stakeholders en Proyectos de Software

Los stakeholders son todas las personas o grupos que tienen interés en el resultado de un proyecto de software — desde los usuarios finales hasta el equipo de operaciones. Gestionarlos bien determina si un proyecto técnicamente exitoso se percibe como un fracaso, o viceversa.

> Fuente: *Soft Skills* — Sonmez, *The Manager's Path* — Fournier, *Staff Engineer* — Larson

---

## Quiénes son los stakeholders

| Tipo | Ejemplos | Qué les importa |
|------|----------|----------------|
| **Internos técnicos** | Devs, QA, arquitecto, DevOps | Deuda técnica, tiempo de build, DX |
| **Internos de negocio** | Product Owner, management, ventas | Features, fechas, métricas de negocio |
| **Externos directos** | Clientes, usuarios finales | Uptime, velocidad, UX |
| **Externos reguladores** | Legal, auditoría, compliance | Seguridad, privacidad, trazabilidad |
| **Operativos** | Soporte, onboarding, customer success | Documentación, observabilidad, rollouts limpios |

**Cada grupo habla un idioma diferente.** Lo que para un dev es "refactorizar el repository pattern" para el PO es "invertir puntos en trabajo no visible al usuario".

---

## Cuadrante de Poder/Interés

Mapear stakeholders por poder (capacidad de influir en el proyecto) e interés (qué tanto les afecta el resultado):

```
                PODER ALTO
                    │
   Gestionar    │   Gestionar
   de Cerca     │   de Cerca
   (involucrar  │   (decisiones
   en decisiones)   clave)
                │
INTERÉS ────────┼──────────── INTERÉS
BAJO            │             ALTO
                │
   Monitorear   │   Mantener
   (informar    │   Satisfecho
   ocasional-   │   (informar
   mente)       │   regularmente)
                │
                PODER BAJO
```

**Estrategias por cuadrante:**
- **Gestionar de cerca (poder alto + interés alto):** involucrar en decisiones, reuniones regulares, ADRs compartidos
- **Mantener satisfecho (poder alto + interés bajo):** actualizaciones periódicas sin detalles técnicos
- **Mantener informado (poder bajo + interés alto):** canales de comunicación directa, changelog, demo days
- **Monitorear (poder bajo + interés bajo):** comunicación mínima, solo en hitos mayores

---

## Comunicación técnica a no-técnicos

El error más común: hablar de soluciones cuando el stakeholder pregunta por impactos.

### Analogías efectivas

| Concepto técnico | Analogía para no-técnicos |
|-----------------|--------------------------|
| Deuda técnica | "El código está en préstamo — pagamos intereses en forma de bugs y lentitud" |
| Refactoring | "Reorganizar el taller para que ensamblar el siguiente producto sea 3× más rápido" |
| Tests automatizados | "Control de calidad automatizado — detecta defectos antes de que lleguen al cliente" |
| Microservicios | "Dividir una tienda grande en puestos independientes — más flexible pero más caro de coordinar" |
| Migration de DB | "Reorganizar el archivo físico — mientras se hace, algunos cajones no están disponibles" |

### Estructura del "elevator pitch" técnico

```
Problema:    "Actualmente, agregar un canal nuevo (ej: Telegram) tarda 3 semanas."
Causa:       "El código de cada canal está mezclado con la lógica de negocio."
Solución:    "Separamos cada canal en un módulo independiente (Strategy Pattern)."
Impacto:     "El próximo canal tomará 3 días, no 3 semanas."
Inversión:   "2 sprints de refactoring sin features nuevas."
Riesgo:      "Sin este cambio, la promesa de 'WhatsApp en 2 semanas' no es alcanzable."
```

---

## Managing Expectations

### Cómo decir "no" con alternativas

```
❌ "Eso no es posible."
❌ "No tenemos tiempo."

✅ "Podemos hacer X en 2 sprints. Si lo priorizamos sobre Y, que está en el backlog,
    tendríamos X listo para el 15 de junio. ¿Eso funciona?"

✅ "La versión básica de X cabe en este sprint. La versión completa que describes
    requiere 3 sprints más. ¿Arrancamos con la básica?"
```

### Comunicar retrasos

Comunicar temprano es mejor que sorprender:

```
Plantilla:
"Detectamos un bloqueo en [componente]. El entregable previsto para [fecha]
se retrasa aproximadamente [N días/sprints]. Causa: [una oración].
Opciones: A) Reducir scope para mantener la fecha, B) Mantener scope y
ajustar la fecha a [nueva fecha]. ¿Cuál prefieres?"
```

### Deuda técnica traducida a riesgo de negocio

```
❌ "Hay mucha deuda técnica en el módulo de conversaciones."

✅ "El módulo de conversaciones tiene un riesgo de deuda técnica medio-alto.
    Síntomas: los últimos 3 bugs de producción vinieron de ahí.
    Impacto: si no lo atendemos, la probabilidad de incidente aumenta 40%
    conforme añadamos los filtros avanzados planeados para Q3."
```

---

## Clasificación de stakeholders por dependencia
> Fuente: *The Software Engineer's Guidebook* (Orosz) — Ch.18 Stakeholder Management

Además del cuadrante poder/interés, clasificar stakeholders por dirección de dependencia:

```
Upstream dependencies — equipos cuyo trabajo necesitas para poder avanzar.
  Ejemplo: el equipo de autenticación que debe exponer un endpoint antes de que
  puedas integrar login en tu feature.

Downstream dependencies — equipos que dependen de tu trabajo.
  Ejemplo: el equipo de reporting que necesita que tu API esté lista para
  construir sus dashboards.

Strategic stakeholders — personas o equipos que deben estar informados y
  que pueden desbloquear upstream dependencies.
  Ejemplo: el director de Marketing que quiere lanzar una campaña cuando
  tu feature salga — no es un bloqueador técnico pero sí uno político.
```

**Aplicación práctica:**
- Cambiar una API → comunicar a todos los downstream que dependen de ella
- Necesitar una API de otro equipo → confirmar con ese upstream que no habrá breaking changes
- Proyecto con stakeholder de Legal → incluirlos desde el inicio, no al final

### Cómo encontrar a los stakeholders
Identificar stakeholders tarde es caro. La regla: incluirlos en el kickoff, no cuando el proyecto ya está terminado.

```
1. Preguntar al PO: "¿Qué equipos de negocio podrían tener interés en esto?"
2. Revisar el código: identificar qué servicios serán modificados y contactar
   a sus dueños (upstream/downstream)
3. "Usual suspects" por defecto: Seguridad, Legal, DevOps, Data, Marketing
4. Preguntar a tech leads de proyectos similares anteriores
```

### Comunicación con stakeholders — formatos

```
Reunión:          cuando necesitas input significativo o hay cambios de scope
Update asíncrono: email o mensaje de chat para actualizaciones regulares
Híbrido:          email para actualizaciones rutinarias + reunión para hitos

Template de update email:
  Asunto: [Proyecto X] — Update semana del [fecha]

  Estado: En progreso / En riesgo / Bloqueado
  
  Completado esta semana:
  - [ítem 1]
  
  Riesgos activos:
  - [riesgo]: [mitigación]
  
  Siguiente semana:
  - [ítem]
  
  ¿Necesitas algo de los stakeholders?: [sí/no + qué]
```

### Stakeholders problemáticos

| Tipo | Comportamiento | Respuesta |
|------|---------------|-----------|
| Upstream bloqueador | No entrega lo que necesitas | Hablar en persona, explicar el impacto del bloqueo, escalar si no hay avance |
| Downstream impaciente | Presiona constantemente por fechas | Mostrar progreso concreto + update email regular para reducir ansiedad |
| Estratégico exigente | Pide updates continuos | Agregar al mailing list de updates, proactivo sobre riesgos |

---

## El desarrollador como stakeholder

Los desarrolladores también son stakeholders. Ignorar sus necesidades genera:

```
Síntomas de DevEx pobre:
  - Build times > 3 minutos → 4 interrupciones/hora para esperar
  - Sin tests → miedo a refactorizar → deuda acumulada
  - Onboarding > 1 semana → costo de rotación alto
  - Sin feedback rápido (CI > 15 min) → ciclos de desarrollo lentos
```

### Costo de rotación de un desarrollador

```
Costo estimado de perder un dev mid-senior:
  + Reclutamiento:          2-4 meses de salario
  + Onboarding:             3-6 meses hasta productividad plena
  + Conocimiento perdido:   incalculable (decisiones no documentadas)
  Total:                    6-12× el salario mensual del dev

Por eso: invertir en DX y autonomía técnica tiene ROI positivo.
```

---

## Decisiones técnicas con stakeholders

### Cuándo involucrar al PO

| Decisión | Involucrar al PO | Razón |
|----------|-----------------|-------|
| Cambiar base de datos (PostgreSQL → MySQL) | ✅ Sí — ADR formal | Impacta costos, skills, soporte |
| Refactoring interno sin cambio de API | ❌ No | Decisión técnica pura |
| Agregar Redis para caching | ✅ Sí — informar | Nueva infraestructura = nuevo costo |
| Renombrar variables | ❌ No | Detalles de implementación |
| Introducir dependency injection framework | ✅ Sí — ADR | Cambio de arquitectura |
| Extraer un método largo | ❌ No | Boy scout rule |

### RFC Process (Request for Comments)

Para decisiones técnicas grandes, hacer un RFC antes del ADR:

```markdown
# RFC-001 — Migrar a PostgreSQL

**Propuesto por:** [dev]   **Deadline de comentarios:** [fecha + 1 semana]

## Propuesta
[Una oración de qué se quiere cambiar]

## Motivación
[Por qué ahora, qué problema resuelve]

## Alternativas consideradas
[Al menos 2 opciones con pros/cons]

## Impacto en el equipo
[Skills necesarios, tiempo de migración, riesgos]
```

---

## Reuniones efectivas

### Checklist pre-reunión

```
✓ Hay una agenda escrita y enviada con ≥ 24h de anticipación
✓ Los decisores correctos están invitados (no "por si acaso")
✓ El objetivo es claro: ¿decisión, alineación o update?
✓ Duración máxima definida (defaultear a 30 min, no 60)
```

### Tipos de reunión y formato correcto

| Tipo | Formato | Frecuencia |
|------|---------|------------|
| Daily standup | 3 preguntas, de pie, 15 min max | Diaria |
| Sprint planning | Story by story, equipo completo | Inicio de sprint |
| Sprint review | Demo al PO/stakeholders, no PPT | Fin de sprint |
| Retrospectiva | Solo el equipo, safe space | Fin de sprint |
| ADR review | Asíncrono (comentarios en PR) | Por demanda |
| 1:1 dev-manager | Agenda del dev, no del manager | Semanal |

### Nota de decisión (post-reunión)

```markdown
**Reunión:** [nombre]   **Fecha:** [fecha]   **Asistentes:** [lista]

## Decisiones tomadas
- [Decisión 1]
- [Decisión 2]

## Acciones
| Qué | Quién | Cuándo |
|-----|-------|--------|
| [acción] | [nombre] | [fecha] |

## Temas abiertos para siguiente reunión
- [tema pendiente]
```

---

## Conflictos comunes en proyectos de software

| Conflicto | Señal de alerta | Cómo manejarlo |
|-----------|----------------|---------------|
| "¿Cuándo estará listo?" | Presión para dar fechas sin scope claro | "Dame el scope definido y te doy la fecha. Sin scope fijo, la fecha es una suposición." |
| Feature creep | El scope crece sin ajustar la fecha | "Eso es una historia nueva — va al backlog. La fecha original es para el scope original." |
| Prioridades cambiantes | Sprint interrumpido cada semana | "Cada cambio mid-sprint nos cuesta [X] días de contexto. ¿Quieres ese trade-off?" |
| "Los devs siempre dicen que va a tardar más" | Falta de confianza en estimaciones | Mostrar velocity histórica + triangular con historias similares completadas |
| Scope vs calidad | "¿Para qué sirven los tests?" | Mostrar costo de bugs en producción vs costo de tests: típicamente 5-15× más caro sin tests |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Stakeholder | persona o grupo con interés en el resultado de un proyecto; puede ser interno (equipo) o externo (cliente, regulador) |
| Cuadrante Poder/Interés | herramienta para clasificar stakeholders según su capacidad de influencia y su nivel de interés en el proyecto |
| Upstream dependency | equipo o sistema del que depende el equipo para poder avanzar en su propio trabajo |
| Downstream dependency | equipo o sistema que depende del trabajo del equipo para construir el suyo |
| RFC | Request for Comments — documento previo al ADR para proponer y debatir cambios técnicos significativos con el equipo |
| ADR | Architecture Decision Record — documento que formaliza una decisión arquitectural con su contexto y alternativas |
| Feature creep | acumulación de funcionalidades no planificadas que expande el scope sin ajustar la fecha de entrega |
| Elevator pitch técnico | presentación concisa (problema → causa → solución → impacto → inversión → riesgo) para comunicar propuestas técnicas a no-técnicos |
| DevEx (Developer Experience) | calidad de la experiencia de trabajo del desarrollador: tiempos de build, feedback loops, herramientas y autonomía |
| Managing expectations | práctica de alinear proactivamente las expectativas de los stakeholders con la realidad del proyecto |

---

*Rogelio Arriaga Gonzalez*
