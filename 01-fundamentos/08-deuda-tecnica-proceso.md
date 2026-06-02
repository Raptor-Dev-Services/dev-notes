# 08 — Deuda Técnica como Proceso

La deuda técnica es el costo implícito del trabajo adicional causado por elegir una solución rápida ahora en lugar de una mejor solución que tomaría más tiempo. Como la deuda financiera, acumula intereses: cada nueva funcionalidad construida sobre código de baja calidad cuesta más de lo que debería.

> Fuente: Ward Cunningham (inventor del término), *Refactoring* — Fowler, *Working Effectively with Legacy Code* — Feathers

---

## El Cuadrante de Fowler

Martin Fowler propone clasificar la deuda técnica en dos ejes:
- **Deliberada vs Accidental:** ¿fue una decisión consciente?
- **Prudente vs Imprudente:** ¿conocíamos el trade-off?

```
                  DELIBERADA                 ACCIDENTAL
            ┌─────────────────────┬─────────────────────┐
PRUDENTE    │ "Debemos lanzar     │ "Ahora entendemos   │
            │  ahora y lidiaremos │  cómo deberíamos    │
            │  con las consecuen- │  haberlo hecho."    │
            │  cias después."     │                     │
            ├─────────────────────┼─────────────────────┤
IMPRUDENTE  │ "No hay tiempo para │ "¿Qué es capas de   │
            │  diseñar."          │  arquitectura?"     │
            └─────────────────────┴─────────────────────┘
```

**Solo la Deliberada/Prudente es aceptable** — y debe documentarse (ADR).

---

## Tipos de deuda técnica

### Deuda de código
```
✗ Duplicación masiva (DRY violation no resuelto)
✗ Métodos de 200 líneas (Long Method)
✗ God Classes (una clase que hace todo)
✗ Magic strings y números mágicos
✗ Código comentado ("por si acaso")
```

### Deuda arquitectural
```
✗ Capas mezcladas (EF Core en Domain)
✗ Handlers que llaman a otros Handlers directamente
✗ Lógica de negocio en controllers
✗ Repositorios que exponen IQueryable<T>
```

### Deuda de testing
```
✗ Cobertura < 60% en lógica de negocio crítica
✗ Tests que solo prueban el happy path
✗ Tests frágiles (dependen de orden o estado global)
```

### Deuda de dependencias
```
✗ Librerías con vulnerabilidades conocidas sin parchear
✗ Versiones de .NET fuera de LTS
✗ NuGet packages abandonados o sin mantenimiento
```

---

## Cómo se mide

### Herramientas

| Herramienta | Qué mide |
|-------------|---------|
| **SonarQube / SonarCloud** | Complejidad ciclomática, duplicación, code smells, vulnerabilidades |
| **CodeClimate** | Puntuación de mantenibilidad por archivo, hotspots |
| **Rider / ReSharper** | Issues inline en el IDE |
| **dotnet-coverage** | Cobertura de tests por namespace |

### Métricas clave

```
Complejidad ciclomática:  idealmente < 10 por método
Duplicación:              idealmente < 3% del total de líneas
Deuda estimada:           SonarQube calcula "días de trabajo" para resolverla
Hotspot:                  archivos con alta complejidad Y alta frecuencia de cambio
```

**Hotspot analysis:** cruzar "¿qué archivos cambian más?" (git log) con "¿qué archivos tienen más deuda?" (SonarQube). Los hotspots son la deuda que más duele — priorizar esos.

---

## Proceso de gestión

### 1. Inventario — hacer visible la deuda

Convertir deuda técnica en items concretos del backlog:

```
Ejemplo de item técnico:
  Título: Refactorizar ConversationService — God Class
  Descripción: ConversationService tiene 800 líneas y 23 métodos.
               Romper en AssignmentService, StatusService, HistoryService.
  Criterio de aceptación: cada clase < 200 líneas, cobertura > 70%
  Estimación: 5 story points
  Etiqueta: tech-debt, hotspot
```

### 2. Priorización

No toda la deuda vale la pena pagar. Priorizar por:

```
Impacto =  (frecuencia de cambio) × (complejidad) × (criticidad del módulo)

Alta prioridad:   módulos que cambian cada sprint Y tienen bugs frecuentes
Media prioridad:  módulos que cambian mensualmente Y hacen onboarding lento
Baja prioridad:   módulos estables que no se tocan en meses
```

### 3. Pagar la deuda — opciones

```
Boy Scout Rule:       "Deja mejor de lo que encontraste" — refactor pequeño 
                      con cada PR que toca el archivo
20% del sprint:       reservar 20% de la capacidad del sprint para tech debt
Tech Debt Sprint:     un sprint dedicado cuando la deuda bloquea features
Strangler Fig:        reemplazar incrementalmente un módulo legado sin reescribir todo
```

---

## Comunicar la deuda técnica al Product Owner

El PO no entiende "complejidad ciclomática 45". Traducir a impacto de negocio:

```
❌ "Necesitamos refactorizar ConversationService porque tiene alta complejidad ciclomática."

✅ "Agregar filtros de conversación tarda 3 días cuando debería tardar 4 horas,
    porque toda la lógica está mezclada en un archivo de 800 líneas.
    Con 5 puntos de tech debt este sprint, las próximas 10 features de
    conversaciones serán 60% más rápidas."
```

### El costo de no pagar

```
Velocidad del equipo sin gestionar deuda:
  Sprint 1:  8 story points completados
  Sprint 5:  6 story points (la deuda empieza a frenar)
  Sprint 10: 4 story points (cada cambio rompe algo)
  Sprint 15: 2 story points (el equipo tiene miedo de tocar el código)
```

---

## Prevención — construir sin acumular deuda

### Definition of Done (DoD) con criterios de calidad

```markdown
# Definition of Done — CRM Messenger

Una historia está "Done" cuando:
- [ ] El código compila sin warnings
- [ ] Los tests pasan (unitarios + integración)
- [ ] Cobertura no baja del 70% en el módulo afectado
- [ ] SonarQube no introduce nuevos code smells críticos
- [ ] Code review aprobado por al menos 1 par
- [ ] ADR escrito si se tomó una decisión arquitectural
- [ ] CHANGELOG actualizado (si afecta API pública)
```

### Continuous Refactoring

```
✓ Renombrar variables en cada PR que se toca el archivo
✓ Extraer constantes cuando aparece un string repetido
✓ Agregar tests al código que se modifica, aunque no estaba cubierto
✓ Actualizar dependencias en sprints dedicados (trimestral)
✗ Acumular "TODO:" comments sin crear items en el backlog
✗ Aprobar PRs con code smells "porque hay prisa"
```

---

## ADRs como historial de deuda deliberada

Cada decisión de deuda consciente debe documentarse en un ADR:

```markdown
# ADR-003 — Monolito Modular sobre Microservicios

**Status:** Accepted
**Date:** 2026-05-01

## Contexto
Equipo de 4 devs, producto en fase MVP, dominio no completamente definido.

## Decisión
Monolito Modular. Módulos bien separados en el código pero un solo deployment.

## Consecuencias
+ Despliegue simple, una sola base de datos, transacciones ACID.
− Escalar un módulo individualmente requiere migrar a microservicio.

## Condición de revisión
Cuando un módulo supere 500 requests/segundo de forma sostenida durante
4 semanas consecutivas, evaluar extracción a microservicio independiente.
```

**La condición de revisión convierte la deuda deliberada en una deuda con fecha de vencimiento medible.**

---

## Referencias

- Fowler, M. — *Refactoring: Improving the Design of Existing Code* (2018)
- Feathers, M. — *Working Effectively with Legacy Code* (2004)
- Cunningham, W. — "The WyCash Portfolio Management System" (OOPSLA 1992)
- Sonmez, J. — *Soft Skills: The Software Developer's Life Manual* — Cap. Deuda técnica

---

## Glosario

| Término | Definición |
|---------|-----------|
| Deuda técnica | costo implícito del trabajo adicional causado por elegir una solución rápida en lugar de una solución mejor |
| Cuadrante de Fowler | modelo que clasifica la deuda técnica en dos ejes: deliberada/accidental y prudente/imprudente |
| Code smell | señal en el código que sugiere un problema de diseño subyacente; no es un bug pero dificulta el mantenimiento |
| Hotspot | módulo del código que cambia frecuentemente, tiene alta complejidad y acumula la mayor parte de los bugs |
| Boy Scout Rule | principio de dejar el código levemente mejor de como se encontró en cada cambio, sin necesidad de refactoring masivo |
| Strangler Fig | patrón que reemplaza un módulo legado de forma incremental, sin reescribir todo de una vez |
| ADR | Architecture Decision Record — documento que registra la justificación de una decisión de deuda técnica deliberada |
| Definition of Done | criterios acordados por el equipo que debe cumplir toda historia para considerarse terminada, incluyendo calidad |
| Complejidad ciclomática | métrica que mide el número de caminos independientes en el código; valores altos indican alta complejidad de prueba |
| Continuous Refactoring | práctica de mejorar el código de forma incremental en cada PR, sin esperar sprints dedicados |
| Deuda deliberada prudente | tipo de deuda aceptable cuando se documenta explícitamente y se planifica su pago |

---

*Rogelio Arriaga Gonzalez*
