# Roadmap — Backend con ASP.NET Core

**Prerequisito:** [Roadmap C#](roadmap-csharp.md) completado o C# intermedio equivalente.
**Objetivo:** construir APIs REST production-ready con ASP.NET Core 10, Dapper, PostgreSQL.
**Duración estimada:** 6-8 semanas.

---

## Fase 1 — Base de datos primero (semana 1)

> Antes de escribir la API, dominar la capa de datos. Todos los errores de rendimiento nacen aquí.

- [ ] [SQL Fundamentos](../05-bases-de-datos/01-sql-fundamentos.md) — SELECT, JOINs, CTEs, window functions, ACID
- [ ] [Índices](../05-bases-de-datos/02-indices.md) — B-Tree, partial, compuesto, EXPLAIN ANALYZE
- [ ] [Transacciones](../05-bases-de-datos/03-transacciones.md) — isolation levels, deadlocks, savepoints
- [ ] [PostgreSQL Avanzado](../05-bases-de-datos/04-postgresql-avanzado.md) — JSONB, arrays, full-text search
- [ ] [Connection Strings](../05-bases-de-datos/06-connection-strings.md) — Npgsql, pooling, resiliencia
- [ ] [Dapper Queries](../05-bases-de-datos/05-dapper-queries.md) — QueryMultiple, multi-mapping, paginación, tenant params

**Al terminar esta fase puedes:** escribir SQL eficiente y ejecutarlo desde C# con Dapper.

---

## Fase 2 — Patrones core del back-template (semana 2)

> Los bloques fundamentales que estructuran cada caso de uso en el proyecto.

- [ ] [Result Pattern](../04-backend/01-result-pattern.md) — ISuccess/IFailure, sin excepciones para negocio
- [ ] [Repository + UoW](../04-backend/02-repository-uow.md) — interfaz en Domain, impl en Infrastructure
- [ ] [Pipeline Behaviors](../04-backend/03-pipeline-behaviors.md) — logging, validation, timing con MediatR
- [ ] [Common Library](../04-backend/18-common-library.md) — IMediator, IRequest, IResponse como submódulo

**Al terminar esta fase puedes:** implementar un caso de uso completo Handler → Repository → SQL con Result Pattern.

---

## Fase 3 — Validación, errores y configuración (semana 2-3)

> La "fontanería" que hace robusta a la API.

- [ ] [Validation](../04-backend/06-validation.md) — FluentValidation, reglas, pipeline behavior de validación
- [ ] [Problem Details](../04-backend/08-problem-details.md) — RFC 9457, middleware de errores estructurados
- [ ] [Configuración](../04-backend/09-configuracion.md) — IOptions, FluentValidation bridge, [OptionsValidator]
- [ ] [Secretos](../04-backend/10-secretos.md) — User Secrets en dev, Key Vault/Secrets Manager en prod
- [ ] [CORS](../04-backend/11-cors.md) — políticas, preflight, AllowCredentials

**Al terminar esta fase puedes:** configurar una API con validación completa, errores RFC 9457 y manejo de secretos.

---

## Fase 4 — Seguridad y autenticación (semana 3)

> JWT, rate limiting y control de acceso — imprescindibles antes de hacer públicas las APIs.

- [ ] [JWT](../04-backend/21-jwt.md) — emisión, validación, claims, JWK/JWKS, refresh token
- [ ] [Rate Limiting](../04-backend/12-rate-limiting.md) — fixed window, sliding, token bucket, concurrency
- [ ] [DI Patterns](../04-backend/46-di-patterns.md) — Decorator/Scrutor, Keyed Services, captive dependency

**Al terminar esta fase puedes:** asegurar endpoints con JWT, proteger contra abuso con rate limiting.

---

## Fase 5 — Rendimiento e infraestructura (semana 4)

> Hacer que la API escale y sea resiliente.

- [ ] [Caching](../04-backend/05-caching.md) — IMemoryCache, IDistributedCache (Redis), patrones
- [ ] [Output Caching](../04-backend/16-output-caching.md) — middleware de caché de respuestas, invalidación
- [ ] [HttpClient](../04-backend/13-http-client.md) — IHttpClientFactory, Typed/Named, Polly
- [ ] [Resiliencia / Polly](../04-backend/04-resiliencia-polly.md) — retry, circuit breaker, timeout, fallback
- [ ] [Background Services](../04-backend/07-background-services.md) — IHostedService, BackgroundService, channels
- [ ] [Hangfire](../04-backend/14-hangfire.md) — jobs fire-and-forget, recurrentes, continuations

**Al terminar esta fase puedes:** añadir caching, procesamiento en background y llamadas resilientes a APIs externas.

---

## Fase 6 — ORM, documentación y EF Core (semana 5)

- [ ] [EF Core](../04-backend/17-ef-core.md) — DbContext, fluent config, migrations, tracking vs no-tracking
- [ ] [OpenAPI](../04-backend/15-openapi.md) — .NET 9 native, Scalar UI, Swashbuckle, Problem Details
- [ ] [Performance](../04-backend/19-performance.md) — BenchmarkDotNet, Span<T>, ArrayPool
- [ ] [Memoria / GC](../04-backend/20-memoria-gc.md) — generaciones, IDisposable, pooling de objetos

**Al terminar esta fase puedes:** documentar la API automáticamente y optimizar rutas críticas de rendimiento.

---

## Fase 7 — Testing (semana 5-6)

> Sin tests no hay refactoring seguro ni evolución del sistema.

- [ ] [xUnit](../04-backend/testing/01-xunit.md) — Fixtures, Theory, colecciones de tests
- [ ] [Unit Tests](../04-backend/testing/02-unit-tests.md) — NSubstitute, AAA pattern, mocks
- [ ] [Integration Tests](../04-backend/testing/03-integration-tests.md) — WebApplicationFactory, test DB
- [ ] [Testcontainers](../04-backend/testing/04-testcontainers.md) — PostgreSQL en Docker para tests de integración
- [ ] [TDD](../04-backend/testing/05-tdd.md) — Red-Green-Refactor, cuándo aplicarlo

**Al terminar esta fase puedes:** escribir tests unitarios, de integración y correr contra una DB real en CI.

---

## Fase 8 — API Design profesional (semana 6-7)

- [ ] [HTTP](../03-arquitectura/api-design/01-http.md) — verbos, status codes, headers semánticos
- [ ] [REST](../03-arquitectura/api-design/02-rest.md) — constraints REST, naming de recursos
- [ ] [Paginación](../03-arquitectura/api-design/03-paginacion.md) — offset, cursor, keyset — cuándo usar cada uno
- [ ] [Errores y Contratos](../03-arquitectura/api-design/04-errores-contratos.md) — contratos de error robustos
- [ ] [Buenas Prácticas](../03-arquitectura/api-design/05-buenas-practicas.md) — idempotencia, versioning, naming

**Al terminar esta fase puedes:** diseñar APIs que respetan los estándares HTTP y son fáciles de consumir.

---

## Qué puedes construir al terminar

Una API REST completa con:
- Autenticación JWT con refresh token
- FluentValidation + Problem Details RFC 9457
- Dapper + PostgreSQL con paginación eficiente
- Caching por capas (memory + Redis)
- Jobs en background con Hangfire
- Tests unitarios e integración con Testcontainers
- Documentación OpenAPI con Scalar UI

---

## Siguiente paso

- [Roadmap Arquitectura](roadmap-arquitectura.md) — aplicar Clean Architecture, CQRS, DDD
- [Roadmap SaaS Multi-Tenant](roadmap-saas-multitenant.md) — si el objetivo es un producto SaaS

---

*Duración total estimada: 6-8 semanas — Nivel objetivo: backend .NET senior*
