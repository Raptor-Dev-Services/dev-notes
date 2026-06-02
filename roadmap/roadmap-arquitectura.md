# Roadmap — Arquitectura de Software

**Prerequisito:** [Roadmap Backend .NET](roadmap-backend-dotnet.md) completado o backend .NET intermedio.
**Objetivo:** diseñar sistemas mantenibles y escalables con Clean Architecture, CQRS, DDD y patrones avanzados.
**Duración estimada:** 4-6 semanas.

---

## Fase 1 — Fundamentos de ingeniería de software (semana 1)

> La base conceptual que da sentido a todas las decisiones arquitectónicas.

- [ ] [SOLID](../01-fundamentos/01-solid.md) — los 5 principios, por qué importan, ejemplos malos y buenos
- [ ] [Clean Code](../01-fundamentos/02-clean-code.md) — nombres, funciones, comentarios — Martin
- [ ] [Refactoring](../01-fundamentos/03-refactoring.md) — code smells, técnicas — Fowler
- [ ] [Deuda Técnica](../01-fundamentos/08-deuda-tecnica-proceso.md) — tipos, impacto, gestión continua

**Al terminar esta fase puedes:** identificar qué viola los principios SOLID y aplicar refactors básicos.

---

## Fase 2 — Estilos arquitectónicos (semana 1-2)

> Los tres estilos que dan forma al back-template.

- [ ] [Hexagonal (Ports & Adapters)](../03-arquitectura/03-hexagonal.md) — puertos, adaptadores, composición
- [ ] [Clean Architecture](../03-arquitectura/11-clean-arch.md) — capas, regla de dependencia, Ferreira Ch.14
- [ ] [Hexagonal vs Clean](../03-arquitectura/03-hexagonal-vs-clean.md) — comparativa y casos de uso
- [ ] [Vertical Slice](../03-arquitectura/04-vertical-slice.md) — feature folders, coupling por feature

**Al terminar esta fase puedes:** elegir el estilo arquitectónico correcto para un proyecto y defenderlo.

---

## Fase 3 — Domain-Driven Design (semana 2)

> Modelar el dominio de negocio de manera que el código lo refleje fielmente.

- [ ] [DDD](../03-arquitectura/01-ddd.md) — entidades, agregados, value objects, bounded contexts, ubiquitous language
- [ ] [Specification Pattern](../03-arquitectura/05-specification.md) — queries reutilizables y combinables

**Al terminar esta fase puedes:** definir un modelo de dominio con DDD y expresar reglas de negocio como especificaciones.

---

## Fase 4 — CQRS y flujo de datos (semana 2-3)

> El patrón que estructura cada caso de uso en el back-template.

- [ ] [CQRS](../03-arquitectura/02-cqrs.md) — Commands vs Queries, MediatR, message types, pipeline
- [ ] [REPR Pattern](../03-arquitectura/13-repr.md) — Request-EndPoint-Response para Minimal APIs
- [ ] [Result Pattern](../04-backend/01-result-pattern.md) — 6 formas, Operation Result, OperationStatus

**Al terminar esta fase puedes:** implementar casos de uso como Commands/Queries con Result Pattern, sin excepciones para negocio.

---

## Fase 5 — Arquitecturas de escala (semana 3-4)

> Cómo crecer más allá de una sola API monolítica.

- [ ] [Monolito Modular](../03-arquitectura/09-modular-monolith.md) — módulos con Contracts, reglas de referencia
- [ ] [Microservicios](../03-arquitectura/07-microservicios.md) — EDA, Pub-Sub, message brokers, DLQ
- [ ] [API Versioning](../03-arquitectura/06-api-versioning.md) — URL, header, query string — cuándo usar cada uno

**Al terminar esta fase puedes:** decidir entre monolito modular y microservicios con criterios técnicos, no de moda.

---

## Fase 6 — Diseño de APIs (semana 4)

> La superficie pública de cualquier sistema.

- [ ] [HTTP](../03-arquitectura/api-design/01-http.md) — semántica de verbos, status codes, headers
- [ ] [REST](../03-arquitectura/api-design/02-rest.md) — constraints, naming de recursos, HATEOAS
- [ ] [Paginación](../03-arquitectura/api-design/03-paginacion.md) — offset vs cursor vs keyset
- [ ] [Errores y Contratos](../03-arquitectura/api-design/04-errores-contratos.md) — Problem Details RFC 9457
- [ ] [Buenas Prácticas](../03-arquitectura/api-design/05-buenas-practicas.md) — idempotencia, caching, versioning

**Al terminar esta fase puedes:** diseñar APIs que siguen los estándares HTTP y son predecibles para sus consumidores.

---

## Fase 7 — Comunicación y documentación de arquitectura (semana 4-5)

- [ ] [C4 Model](../03-arquitectura/12-c4-model.md) — diagramas de arquitectura como código con Structurizr DSL
- [ ] [Keycloak](../03-arquitectura/08-keycloak.md) — IAM, OIDC, SSO — autenticación delegada

**Al terminar esta fase puedes:** documentar la arquitectura con diagramas C4 y diseñar la capa de identidad con un IdP externo.

---

## Fase 8 — Proceso y estimación (semana 5)

> Arquitectura no es solo código — también es proceso de toma de decisiones.

- [ ] [Agile / Scrum](../01-fundamentos/04-agile-scrum.md) — sprints, roles, eventos, artefactos
- [ ] [SDLC](../01-fundamentos/05-sdlc.md) — fases del ciclo de vida del desarrollo
- [ ] [Estimación](../01-fundamentos/06-estimacion.md) — story points, Planning Poker, Cone of Uncertainty
- [ ] [Stakeholders](../01-fundamentos/07-stakeholders.md) — gestión de expectativas, comunicación técnica

---

## Qué puedes diseñar al terminar

- Un sistema con Clean Architecture bien separado por capas
- Bounded contexts de DDD con reglas de negocio expresivas
- Módulos independientes que se comunican por Contracts
- APIs REST bien diseñadas con versionado y manejo de errores RFC 9457
- Diagramas C4 que documentan la arquitectura para stakeholders técnicos y no técnicos

---

## Siguiente paso

- [Roadmap SaaS Multi-Tenant](roadmap-saas-multitenant.md) — aplicar todo esto a un producto SaaS
- [Roadmap Patrones de Diseño](roadmap-patrones.md) — los 23 patrones GoF en el contexto de este stack

---

*Duración total estimada: 4-6 semanas — Nivel objetivo: arquitecto de software junior/semi-senior*
