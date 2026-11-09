<!-- _class: lead -->

# Administración de Startup en Crecimiento

## De caos controlado a empresa estructurada

Dev Notes  -  Material extra  -  Raptor Dev Services

---

## Agenda

- # Slide | Bloque
- 3 | ¿Por qué estructurarse?
- 4 | Estructura societaria en México
- 5 | Contratos esenciales
- 6-7 | OKRs y KPIs
- 8-10 | Métricas SaaS + Burn rate
- 11 | Pricing strategy
- 12-14 | Gestión de producto y ceremonias ágiles
- 15-19 | Personas: onboarding, contratación, 1:1s, performance
- 20-24 | Operaciones: SOPs, postmortems, offboarding, salud del equipo
- 25-26 | Financiamiento y escalamiento
- 27 | Checklist de startup estructurada
- 28 | Cierre

---

## ¿Por qué estructurarse?

### Señales de que una startup necesita procesos

- **El equipo crece más allá de 3 personas**  -  la comunicación informal ya no escala
- **El onboarding de un nuevo integrante tarda semanas**  -  nadie sabe a qué darle acceso ni qué explicar primero
- **Las mismas preguntas se repiten todos los días**  -  "¿dónde está el runbook?" / "¿cuál es el ambiente de staging?"
- **Las decisiones no quedan registradas**  -  en 3 meses nadie recuerda por qué se eligió PostgreSQL sobre MySQL

> **Regla práctica:** si explicas algo más de dos veces, ya debería estar documentado.

La estructura no mata la agilidad  -  **la falta de estructura mata el producto**.

---

## Estructura Societaria en México

### SA de CV vs SAPI de CV

| Criterio | SA de CV | SAPI de CV |
|----------|----------|------------|
| **Flexibilidad accionaria** | Limitada  -  acciones iguales | Alta  -  series A/B, preferentes, antidilución |
| **Inversión extranjera** | Requiere permisos RNIE | Permitida directamente |
| **Costo de constitución** | Menor (~$15k-$25k MXN notario) | Mayor (~$40k-$80k MXN notario) |
| **Cuándo usarla** | Bootstrapped, equipo fundador estable | Vas a levantar capital de ángeles o VC |
| **Órgano de gobierno** | Asamblea de accionistas | Consejo de administración (board) |
| **Restricciones de compraventa** | Derecho de tanto básico | Drag-along, tag-along, ROFR negociables |


---

## Contratos Esenciales

### Los tres documentos que protegen a la empresa

**1. NDA (Non-Disclosure Agreement)**
- Firma antes de cualquier entrevista técnica o demo de producto
- Cubre: código fuente, arquitectura, roadmap, base de clientes

**2. Contrato de trabajo para desarrolladores**
- Cláusula de **propiedad intelectual obligatoria**: _"todo el código, diseño y documentación producidos durante la relación laboral, dentro o fuera del horario pactado, y relacionados con el giro de la empresa, son propiedad exclusiva del empleador"_
- Sin esta cláusula, el dev podría reclamar coautoría del producto

**3. Acuerdo de no competencia**
- En MX es válido si: tiene **límite temporal** (máx 1-2 años), **ámbito geográfico** definido y **compensación económica**
- Sin compensación económica es prácticamente inaplicable en juicio

---

## OKRs

### Objective + Key Results  -  el estándar de Google, Intel y miles de startups

**Formato:** un Objetivo cualitativo + 3 Key Results medibles

> **O:** Lanzar el canal WhatsApp del sistema de mensajería en producción
>
> **KR1:** 100 conversaciones procesadas sin errores de integración en las primeras 2 semanas
>
> **KR2:** Latencia p95 < 200 ms en webhook de recepción de mensajes
>
> **KR3:** 0 incidentes críticos (P1/P2) en el primer mes post-lanzamiento

**Reglas de oro:**
- El Objetivo debe inspirar, no ser una tarea técnica
- Los KR son **resultados**, no actividades ("publicar 10 posts" ≠ KR)
- 60-70 % de cumplimiento de KRs al final del ciclo se considera **éxito**  -  si siempre llegas al 100 %, tus OKRs son muy fáciles

---

## KPIs vs OKRs

### ¿Cuándo usar cada uno?

| Criterio | KPI | OKR |
|----------|-----|-----|
| **Propósito** | Medir salud operativa continua | Impulsar cambio y mejora |
| **Ritmo** | Siempre activo (dashboard) | Ciclos fijos (trimestral / semestral) |
| **Quién los define** | Gerencia / área funcional | Equipo completo (bottom-up + top-down) |
| **Consecuencia de fallar** | Alerta / acción correctiva | Revisión de estrategia |
| **Ejemplo** | Uptime 99.9 %, tiempo de respuesta p99 | "Reducir churn de 8 % a 4 % en Q3" |

**Regla práctica:**
- Los **KPIs** te dicen si el motor funciona
- Los **OKRs** te dicen si vas en la dirección correcta

Ambos son necesarios. No son alternativos.

---

## Métricas SaaS  -  Ingresos Recurrentes

### MRR, ARR y Churn Rate

**MRR (Monthly Recurring Revenue)**
```
MRR = Σ (precio mensual × clientes activos)
Ejemplo: 12 clientes × $2,500 MXN/mes = $30,000 MXN MRR
```

**ARR (Annual Recurring Revenue)**
```
ARR = MRR × 12
Ejemplo: $30,000 × 12 = $360,000 MXN ARR
```

**Churn Rate mensual**
```
Churn = clientes perdidos en el mes / clientes al inicio del mes
Ejemplo: perdiste 2 de 50 -> Churn = 4 %
```

**Benchmarks SaaS:** Churn < 2 %/mes = saludable  -  > 5 %/mes = problema estructural

---

## Métricas SaaS  -  Rentabilidad por Cliente

### LTV, CAC y el ratio que define la viabilidad del negocio

**LTV (Lifetime Value)**
```
LTV = MRR por cliente / Churn Rate mensual
Ejemplo: $2,500 MXN / 0.04 = $62,500 MXN LTV
```

**CAC (Customer Acquisition Cost)**
```
CAC = gasto total en ventas y marketing / nuevos clientes adquiridos
Ejemplo: $30,000 MXN gastado -> 4 clientes = $7,500 MXN CAC
```

**Ratio LTV / CAC**
```
Ratio = LTV / CAC
Ejemplo: $62,500 / $7,500 = 8.3x  <- muy saludable
```

| Ratio | Diagnóstico |
|-------|-------------|
| > 3x | Saludable  -  el negocio escala |
| 1x - 3x | Zona de mejora  -  ajustar precio o reducir CAC |
| < 1x | Alarma  -  pierdes dinero por cada cliente adquirido |

---

## Burn Rate y Runway

### Cuánto tiempo le queda a la empresa

**Burn Rate mensual**
```
Burn Rate = egresos totales del mes - ingresos del mes
Ejemplo: gastos $120,000 - ingresos $40,000 = $80,000 MXN/mes de burn
```

**Runway**
```
Runway (meses) = caja disponible / Burn Rate mensual
Ejemplo: $400,000 en banco / $80,000 burn = 5 meses de runway
```

### Acciones cuando el runway cae a < 6 meses

1. **Reducir burn:** pausar contrataciones, renegociar proveedores, eliminar SaaS sin uso
2. **Acelerar ingresos:** cierre anticipado de clientes en pipeline, cobro por adelantado (anual con descuento)
3. **Levantar capital:** iniciar conversaciones con ángeles  -  los procesos toman 3-4 meses
4. **Pivot de modelo:** si el producto no vende, es una señal de mercado, no de ejecución

> **Regla:** Nunca dejes que el runway llegue a 3 meses sin un plan concreto.

---

## Pricing Strategy

### Cómo monetizar sin datos históricos

**Modelos de cobro:**

| Modelo | Cuándo usarlo | Ejemplo |
|--------|---------------|---------|
| Per-seat | Producto de colaboración de equipo | $500 MXN/agente/mes |
| Por uso | Plataformas con consumo variable | $0.05 MXN/mensaje enviado |
| Flat + tiers | Claridad para el comprador | Starter $1k  -  Pro $3k  -  Enterprise negociado |

**Freemium vs Free Trial:**
- **Freemium:** plan gratuito permanente  -  atrae volumen pero dilluye ventas; útil si tienes efecto de red
- **Free Trial:** acceso completo por 14-30 días  -  mejor conversión a pago; recomendado para B2B

**Value-based pricing (sin datos históricos):**
1. Estima cuánto **ahorra o gana** el cliente con tu producto
2. Cobra entre el 10 % y el 20 % de ese valor
3. Valida con 5 entrevistas de cliente antes de publicar precios

---

## Gestión de Backlog

### Herramienta correcta para cada etapa

| Herramienta | Fortaleza | Cuándo usarla |
|-------------|-----------|----------------|
| **Notion** | Flexibilidad total  -  wikis + tareas | Etapa inicial, equipo < 5, muchos documentos |
| **Linear** | Velocidad de dev  -  ciclos, prioridades, integración con GitHub | Equipo técnico maduro, sprints regulares |
| **GitHub Projects** | Integrado con PRs e issues | Cuando el 90 % del trabajo vive en código |

**Reglas de backlog saludable:**
- Cada ítem tiene: título + criterio de aceptación + responsable + estimado
- Backlog refinado cada 2 semanas  -  elimina lo que nunca se priorizó en 3 ciclos
- Separar **bugs** de **features** de **deuda técnica**  -  la deuda técnica debe ser entre el 15-20 % de cada sprint

---

## Roadmap de Producto

### Visibilidad sin compromisos prematuros

**Now / Next / Later vs Timeline:**

| Formato | Ventaja | Riesgo |
|---------|---------|--------|
| **Now / Next / Later** | No hay fechas  -  evita promesas imposibles | Clientes quieren fechas |
| **Timeline con fechas** | Claridad comercial | Una demora destruye la confianza |

**Recomendación:** usa Now/Next/Later **públicamente**, timeline **internamente**.

**Ejemplo: sistema de mensajería:**

| Horizonte | Qué incluye |
|-----------|-------------|
| **Now** | Canal WhatsApp funcional, bandeja de conversaciones, login JWT |
| **Next** | Canal Instagram, bot flow builder, búsqueda FTS |
| **Later** | Campañas masivas, IA en respuestas, integración CRM externo |

> No publiques fechas antes de tener el feature en staging y probado.

---

## Ceremonias Ágiles para Equipo Pequeño

### Qué conservar cuando el equipo es < 5 personas

**Standup diario  -  15 minutos máximo**
- ¿Qué hice ayer?
- ¿Qué haré hoy?
- ¿Qué me bloquea?

No es una reunión de status para el líder  -  es sincronización entre pares.

**Planning mensual  -  1-2 horas**
- Revisar OKRs del ciclo
- Priorizar backlog del mes
- Compromisos realistas (no deseos)

**Retrospectiva quincenal  -  45 minutos**
- ¿Qué salió bien? / ¿Qué mejorar? / ¿Qué experimentar?
- Genera máximo 2 acciones concretas con dueño y fecha

**Qué eliminar con equipo < 5:**
- Story points complejos  -  usa tallas de playera (S/M/L/XL)
- Sprints de 2 semanas formales  -  ciclos de 1 semana o kanban continuo
- Reuniones de estimación de 3 horas

---

## Onboarding Técnico Estructurado

### Checklist para integrar a un nuevo dev sin perder una semana

**Día 1**
- [ ] Cuenta GitHub con acceso al repo del producto
- [ ] Accesos: AWS staging, Notion, Slack, Linear / GitHub Projects
- [ ] Leer `README.md` + `CLAUDE.md` del repositorio
- [ ] Setup local: clonar, levantar Docker Compose, correr tests

**Semana 1**
- [ ] Tour del `back-template` con el líder técnico (2 horas)
- [ ] Primer PR entregado en < 48 horas hábiles  -  pequeño pero funcional
- [ ] Code review recibido y cambios aplicados

**Mes 1**
- [ ] Feature completa desarrollada de forma autónoma
- [ ] Participó en al menos una retrospectiva
- [ ] Documentó algo que le costó trabajo entender (mejora al wiki)

> **Indicador de éxito:** si el nuevo dev hace su primer PR en < 48 horas, el onboarding funciona.

---

## Proceso de Contratación Técnica

### Cómo contratar devs sin perder tiempo en candidatos equivocados

**Etapa 1  -  Screening de CV (30 min)**
- ¿Hay proyectos reales con código en GitHub?
- ¿El stack coincide con el 70 % de lo que necesitas?
- Descartar: CVs sin proyectos demostrables, stack completamente diferente

**Etapa 2  -  Prueba técnica en casa (48 horas, repo propio)**
- Problema acotado: una API REST pequeña o un componente React real
- Lo que evalúas: estructura del código, README, decisiones de diseño, historial de commits
- No uses plataformas de coding challenges  -  el candidato trabaja como trabajaría en tu empresa

**Etapa 3  -  Entrevista técnica live (60 min)**
- Code review en vivo sobre su propia prueba técnica
- Preguntas situacionales: "¿Qué cambiarías de esto?" / "¿Por qué elegiste este patrón?"

**Etapa 4  -  Culture fit (30 min)**
- ¿Cómo reacciona ante feedback negativo?
- ¿Pregunta o asume?
- ¿Documenta o solo codifica?

---

## 1:1s Efectivos

### La herramienta de retención más poderosa que existe

**Estructura recomendada (30 minutos):**
- **10 min  -  agenda del reporte:** lo que él/ella quiere hablar (bloqueantes, ideas, frustraciones)
- **10 min  -  agenda del líder:** feedback, contexto de negocio, expectativas
- **10 min  -  carrera:** crecimiento, objetivos personales, próximo paso en la empresa

**Frecuencia:**
- Junior / nuevo: semanal
- Senior / autónomo: quincenal

**Qué NO hacer en un 1:1:**
- Convertirlo en un status meeting ("¿cómo va el ticket?")  -  para eso existe el standup
- Cancelarlo cuando hay mucho trabajo  -  es exactamente cuando más se necesita
- Tomar notas y no hacer seguimiento  -  destruye la confianza

> **Dato:** el 50 % de los devs que renuncian nunca mencionaron el problema en un 1:1. Si no tienes 1:1s, no tienes señales tempranas.

---

## Performance Review para Devs

### Evaluar impacto, no actividad

**Criterios técnicos objetivos:**

| Criterio | Cómo medirlo |
|----------|--------------|
| Calidad de código | Code review score  -  % de comentarios resueltos sin revert |
| Autonomía | % de tickets cerrados sin bloqueos externos |
| Comunicación | Claridad en PRs, actualizaciones proactivas en standup |
| Impacto en producto | Features entregadas / bugs introducidos / deuda técnica generada |

**Ciclo:** semestral  -  no anual (demasiado largo para una startup)

**Cómo evitar el sesgo de recencia:**
- Mantener un "brag document" continuo por persona  -  logros documentados en tiempo real
- Revisar los últimos 6 meses de PRs, no solo el último mes
- Pedir autoevaluación antes de la reunión

**Resultado de la review:** acción concreta  -  promoción, ajuste salarial, plan de mejora con fecha, o separación conversada.

---

## SOPs y Runbooks

### Documentar para que el sistema funcione sin héroes

**SOP (Standard Operating Procedure)  -  proceso repetible**
- Define cómo ejecutar una tarea de forma consistente
- Ejemplo: _"Cómo hacer deploy a producción"_
- Formato mínimo: Título  -  Responsable  -  Prerrequisitos  -  Pasos numerados  -  Verificación  -  Links

**Runbook  -  respuesta a incidente**
- Define qué hacer cuando algo sale mal
- Ejemplo: _"La base de datos de producción no responde"_
- Formato mínimo: Título  -  Síntomas  -  Pasos de diagnóstico  -  Acciones de mitigación  -  Escalación  -  Post-incidente

**Diferencia clave:**

| | SOP | Runbook |
|-|-----|---------|
| Cuándo se usa | Proceso planificado | Incidente activo |
| Tono | Instruccional | Urgente / diagnóstico |
| Autor | Quien diseña el proceso | Quien resolvió el incidente |

> **Regla:** si un incidente se repite y no tienes runbook, el segundo incidente es culpa tuya.

---

## Cultura de Documentación

### El conocimiento que vive solo en la cabeza de alguien es un riesgo operativo

**Qué documentar y dónde:**

| Qué | Dónde | Frecuencia |
|-----|-------|------------|
| Decisiones de arquitectura | ADR en el repo (`docs/adr/`) | Cada vez que cambia una decisión |
| Procesos operativos | Wiki del proyecto (Notion/Confluence) | Cuando se crean o cambian |
| Setup de desarrollo | `README.md` del repo | Cada vez que cambia el setup |
| Contexto para IA (Claude) | `CLAUDE.md` / `AGENTS.md` | Cada sprint o cuando cambia la arquitectura |

**Regla de dos explicaciones:**
> Si explicas algo verbalmente más de dos veces, ya perdiste más tiempo del que te tomaría documentarlo.

**Anti-patrón común:** documentar solo al final del proyecto  -  cuando nadie recuerda los detalles.

**Patrón correcto:** documentar en el momento en que la decisión se toma o el proceso se diseña.

---

## Blameless Postmortems

### Aprender de los incidentes sin destruir la cultura del equipo

**Por qué "sin culpables":**
- Si el equipo sabe que habrá culpables, **no reportarán incidentes a tiempo**
- Los sistemas fallan  -  las personas actúan dentro de sistemas mal diseñados
- El objetivo es mejorar el sistema, no castigar a quien ejecutó

**Estructura de un blameless postmortem:**

1. **Timeline**  -  cronología objetiva de qué pasó y cuándo (hechos, no interpretaciones)
2. **Causa raíz**  -  el "5 Whys" aplicado hasta llegar al fallo sistémico
3. **Impacto**  -  tiempo de afectación, clientes impactados, pérdida estimada
4. **Acciones correctivas**  -  lista con owner nombrado y fecha de cierre
5. **Lecciones aprendidas**  -  qué cambiaría el equipo si pudiera volver atrás

**Tiempo límite:** el postmortem debe publicarse en < 48 horas del incidente mientras los detalles están frescos.

---

## Offboarding Seguro

### El momento más riesgoso para la seguridad de la empresa

**El mismo día que se anuncia la salida:**

- [ ] Revocar acceso a GitHub / GitLab
- [ ] Revocar acceso a AWS / GCP / Azure
- [ ] Revocar acceso a Slack / Notion / Linear
- [ ] Cambiar contraseñas de cuentas compartidas que conocía
- [ ] Revocar tokens de API personales y SSH keys

**Semana siguiente:**
- [ ] Sesión de transferencia de conocimiento (documentar lo que sabe y nadie más sabe)
- [ ] Transferir ownership de repositorios, dashboards, alertas
- [ ] Recuperar equipo de la empresa (laptop, periféricos)
- [ ] Cierre formal de contrato con firma de finiquito

**Riesgo legal de no hacerlo:**
- Un ex-empleado con acceso activo puede leer código fuente, datos de clientes o modificar infraestructura
- En MX, si hay incidente post-salida y el acceso no fue revocado, la empresa es responsable

> **Regla de oro:** trata el offboarding con la misma seriedad que el onboarding.

---

## Indicadores de Salud del Equipo

### Medir el sistema humano, no solo el técnico

| Indicador | Qué mide | Frecuencia |
|-----------|----------|------------|
| **eNPS** (Employee Net Promoter Score) | ¿Recomendarías esta empresa como lugar de trabajo? (0-10) | Mensual o trimestral |
| **Tiempo medio de resolución de PR** | Velocidad de revisión de código y colaboración | Semanal (automatizable con GitHub) |
| **Frecuencia de deploys** | Madurez del proceso de entrega continua | Semanal |
| **Número de incidentes P1/P2 por mes** | Estabilidad del sistema en producción | Mensual |

**Cómo leerlos juntos:**
- eNPS bajo + muchos incidentes = burnout + deuda técnica acumulada
- Deploys frecuentes + tiempo de PR largo = bottleneck en revisión de código
- eNPS alto + pocos deploys = equipo feliz pero lento  -  revisar proceso

**Benchmark:** eNPS > 30 es saludable para equipos técnicos. Medir sin actuar sobre el resultado es peor que no medir.

---

## Financiamiento

### Cuándo buscar inversión y cuándo el revenue es suficiente

**Bootstrapped vs Angel vs VC:**

| Fuente | Cuándo | Dilución | Velocidad |
|--------|--------|----------|-----------|
| **Bootstrapped** | Validación del producto | 0 % | Lenta pero sostenible |
| **Ángel** | Primeros clientes, necesitas escalar | 5-15 % | Media (3-6 meses) |
| **VC (Venture Capital)** | Producto probado, mercado grande, escala agresiva | 20-30 % por ronda | Alta (6-12 meses) |

**Cap table básico (ejemplo):**
- Fundador A: 50 %  -  Fundador B: 30 %  -  Pool de opciones para empleados: 10 %  -  Ángel: 10 %

**Cuándo NO buscar inversión:**
- Cuando no tienes claro el modelo de negocio  -  el capital acelera lo que ya funciona
- Cuando el equipo no está alineado en la visión  -  el dinero no resuelve conflictos internos
- Cuando el revenue puede financiar el crecimiento  -  el revenue es la mejor fuente de capital

---

## Escalamiento

### Contratar bien es más difícil que contratar rápido

**¿Cuándo contratar?**
> Cuando un rol bloquea el crecimiento del negocio por más de 2 semanas seguidas.

No contrates anticipadamente  -  cada persona agrega complejidad de coordinación.

**¿Cuándo externalizar?**
> Cuando la tarea no es core competency de la empresa y tiene solución de mercado probada.

| Contratar | Externalizar |
|-----------|--------------|
| Desarrollo del producto central | Contabilidad / nómina |
| Soporte técnico de clientes enterprise | Diseño gráfico puntual |
| DevOps si el producto vive en la nube | Legal / notaría |

**Cómo evitar sobre-contratar:**
1. Primero valida que el cuello de botella es el rol, no el proceso
2. Contrata con una prueba de 3 meses con criterios de éxito definidos
3. No contrates en cascada  -  un nuevo dev no desbloquea a otro nuevo dev

---

## Checklist de Startup Estructurada

### ¿Tu empresa está lista para escalar?

**Legal y contratos**
- [ ] NDA firmado con todos los colaboradores
- [ ] Contratos con cláusula de propiedad intelectual
- [ ] Estructura societaria adecuada al modelo de inversión

**Estrategia y métricas**
- [ ] OKRs definidos para el trimestre en curso
- [ ] MRR, Churn, LTV/CAC monitoreados en dashboard
- [ ] Burn rate y runway calculados mensualmente

**Operaciones**
- [ ] Onboarding documentado (día 1 / semana 1 / mes 1)
- [ ] SOPs para los 3 procesos más críticos
- [ ] Runbooks para incidentes de producción críticos
- [ ] Offboarding checklist aplicado en cada salida

**Equipo**
- [ ] 1:1s en calendario (semanal o quincenal)
- [ ] Matriz de habilidades actualizada
- [ ] Blameless postmortem en < 48h después de cada incidente P1

> Si marcas menos de 10 de 12, tienes deuda organizacional  -  tan costosa como la deuda técnica.

---

<!-- _class: lead -->

# Una startup sin estructura escala el caos.

## Una startup con estructura escala el producto.

**Dev Notes  -  Material extra  -  Raptor Dev Services**
