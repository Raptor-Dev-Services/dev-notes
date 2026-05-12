# Testing — Índice

Tests automatizados en .NET: unitarios, integración, base de datos real y TDD.

📁 Carpeta: [`testing/`](testing/)

---

## Documentos

| # | Documento | Temas cubiertos |
|---|-----------|-----------------|
| 1 | [xUnit](testing/01-xunit.md) | Fact, Theory, InlineData, MemberData, FluentAssertions, IClassFixture, IAsyncLifetime, convenciones |
| 2 | [Unit Tests](testing/02-unit-tests.md) | NSubstitute, Substitute.For, Returns, Received, tests de Handlers, Presenters y Validators |
| 3 | [Integration Tests](testing/03-integration-tests.md) | WebApplicationFactory, TestServer, JWT en tests, estado compartido, limpiar DB |
| 4 | [TestContainers](testing/04-testcontainers.md) | PostgreSQL real, DatabaseFixture, ICollectionFixture, transacciones por test, WebApp + DB |
| 5 | [TDD](testing/05-tdd.md) | Red/Green/Refactor, example completo Handler de login, TDD outside-in |

---

## Mapa de relación con el proyecto

| Documento | Dónde se aplica en el proyecto |
|-----------|-------------------------------|
| xUnit | `Tests/Tests.csproj` — configuración base y convenciones |
| Unit Tests | `Tests/Unit/` — todos los Handlers, Presenters y Validators |
| Integration Tests | `Tests/Integration/` — endpoints HTTP completos |
| TestContainers | `Tests/Fixtures/DatabaseFixture.cs` — tests de repositorios con PostgreSQL 17 real |
| TDD | Flujo de desarrollo: escribir test antes de implementar el Handler |

---

## Pirámide de tests

```
         /\
        /  \         E2E tests         ← pocos, lentos, frágiles
       /----\
      /      \       Integration tests ← verifican el stack completo
     /--------\
    /          \     Unit tests        ← muchos, rápidos, precisos
   /____________\
```

**Distribución recomendada:**
- 70% unit tests (Handlers, Validators, Presenters) — rápidos, baratos de multiplicar
- 20% integration tests (endpoints HTTP + DB) — verifican el DI y el flujo completo
- 10% E2E tests (Playwright, si hay frontend)

---

## Orden de lectura sugerido

1. [xUnit](testing/01-xunit.md) — estructura base y assertions
2. [Unit Tests](testing/02-unit-tests.md) — la mayoría de los tests del proyecto
3. [TDD](testing/05-tdd.md) — cómo integrar los tests en el flujo de desarrollo
4. [TestContainers](testing/04-testcontainers.md) — cuando se necesita DB real
5. [Integration Tests](testing/03-integration-tests.md) — tests de la API completa


---

*Rogelio Arriaga Gonzalez*
