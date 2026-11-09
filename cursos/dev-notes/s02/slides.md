<!-- _class: lead -->

# S02 - DRY - KISS - YAGNI - ADRs

### Dev Notes

**Rogelio Arriaga - Raptor Dev Services**

---

## Agenda del día

- Revisión del homework S01 - PR review en vivo
- **Teoría:** DRY - KISS - YAGNI en profundidad - ADRs
- **Hands-on:** ADR-001, ADR-002, ADR-003 en el repo
- **Desarrollo:** refactoring kata - DRY/KISS/YAGNI aplicado
- Code review - retrospectiva - homework

---

## La premisa del día

```
"El código se escribe una vez y se lee decenas de veces."
— Robert C. Martin, Clean Code
```

**DRY - KISS - YAGNI** no son reglas de estilo.
Son decisiones de diseño que determinan el **costo futuro** del software.

Cada violación que agregamos hoy la pagamos con intereses la próxima semana.

---

<!-- _class: lead -->

# Parte 1
## DRY - Don't Repeat Yourself

---

## DRY - La definición exacta

> *"Every piece of knowledge must have a single,
> unambiguous, authoritative representation within a system."
> "Cada fragmento de conocimiento debe tener una representación única,
> inequívoca y autorizada dentro de un sistema."*
> - Andrew Hunt & David Thomas, The Pragmatic Programmer

**"Conocimiento"** - no líneas de código.

Dos bloques de código pueden ser textos idénticos
y **no** violar DRY si representan conocimiento distinto.

---

## DRY - Código similar ≠ conocimiento duplicado

```csharp
// ¿Viola DRY? Dos funciones que hacen lo mismo...
public decimal CalculatePostScore(Post post)
    => post.Upvotes - post.Downvotes;

public decimal CalculateCommentScore(Comment comment)
    => comment.Upvotes - comment.Downvotes;

// Respuesta: NO necesariamente.
// Si la regla de scoring de Posts y Comments puede cambiar
// de forma independiente → son conocimientos distintos.
// Extraerlos a una función compartida los ACOPLA innecesariamente.
```

**La pregunta correcta no es "¿se ve igual?"
sino "¿representan la misma regla de negocio?"**

---

## DRY - Cuándo SÍ es duplicación de conocimiento

```csharp
// Mal: La regla "un username válido tiene 3-20 caracteres alfanuméricos"
// está duplicada en 3 lugares distintos

// En RegisterUserHandler:
if (request.Username.Length < 3 || request.Username.Length > 20)
    return new ValidationFailure("Username inválido.");

// En UpdateProfileHandler:
if (request.Username.Length < 3 || request.Username.Length > 20)
    return new ValidationFailure("Username inválido.");

// En AdminCreateUserHandler:
if (request.Username.Length < 3 || request.Username.Length > 20)
    return new ValidationFailure("Username inválido.");

// Si la regla cambia a 5-30 → hay que buscarla en 3 archivos
```

---

## DRY - La solución: Value Object

```csharp
// Bien: Domain/ValueObjects/Username.cs — la regla vive en UN solo lugar
public sealed record Username
{
    public string Value { get; }
    private Username(string value) => Value = value;   // nadie puede llamar "new Username(...)"

    public static Result<Username> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail<Username>("Username es requerido.");
        if (value.Length < 3 || value.Length > 20)
            return Result.Fail<Username>("Username debe tener entre 3 y 20 caracteres.");
        if (!value.All(c => char.IsLetterOrDigit(c) || c == '_'))
            return Result.Fail<Username>("Solo letras, números y guión bajo.");
        return Result.OK<Username>(new Username(value.ToLowerInvariant()));
    }
}
```

Constructor `private` -> imposible crear una instancia inválida desde afuera.
`Create()` es el único camino - toda validación pasa por un solo lugar.

---

## DRY - Value Object: dónde vive y cómo se usa

```
Domain/
└── ValueObjects/
    └── Username.cs        ← aquí: la regla, nada más

Application/
└── UseCases/Users/CreateUser/
    └── CreateUserHandler.cs   ← aquí se llama Create(), se consume el Result
```

```csharp
// Bien: Back-template — Application/UseCases/Users/Register/RegisterHandler.cs
// El Command lleva string (primitivo serializable); el Handler crea el Value Object
public async Task<RegisterResponse> Handle(RegisterRequest request, CancellationToken ct)
{
    var usernameResult = Username.Create(request.Username);
    if (usernameResult is FailureResult<Username> f)
        return new RegisterValidationFailure(f.Message);

    // usernameResult garantizado válido — nadie puede crear Username("") desde afuera
    var credential = await _credentials.InsertAsync(
        request.TenantId, usernameResult.Value, request.Email, ct);
    return new RegisterSuccess(credential.PublicId);
}
```

Si la regla cambia -> modificas `Username.Create()`. Un solo archivo. Todos los Handlers reciben el fix automáticamente.

---

## DRY - Alternativas al Value Object con `Create()`

```csharp
// Mal: Constructor lanzando — para flujos predecibles no es excepción
public Username(string value) {
    if (string.IsNullOrWhiteSpace(value))
        throw new ArgumentException("Username requerido.");   // excepción para control de flujo
}

// Aceptable: CanCreate(out errors) — válido, old-school, out params no componen bien
public static bool CanCreate(string value, out List<string> errors) { ... }
if (!Username.CanCreate(request.Username, out var errors))
    return new RegisterValidationFailure(string.Join(", ", errors));

// Aceptable: TryCreate pattern — idiomático .NET BCL, bueno para parsers (Zebra, QR, etc.)
public static bool TryCreate(string value, out Username? result) { ... }
if (!Username.TryCreate(request.Username, out var username))
    return new RegisterValidationFailure("Username inválido.");

// Bien: Back-template: Result<T> Create() — compone bien, sin excepciones de negocio
var usernameResult = Username.Create(request.Username);   // Result.OK<T> o Result.Fail<T>
```

> **¿Cuándo usar TryCreate?** Para parsers donde la conversión puede fallar y es la operación central (parsear un código de barras, un GUID en string, una fecha). Para validación de negocio, `Create()` + `Result<T>` es más expresivo.

---

## DRY - La Regla de Tres

**No abstraigas demasiado pronto.**

```
Primera vez que escribes algo → escríbelo directamente.
Segunda vez que lo repites   → toléralo, anota el duplicado.
Tercera vez que aparece      → ahora sí abstrae.
```

¿Por qué no desde la segunda vez?

- La tercera instancia revela el **patrón real**.
- Abstraer con solo dos instancias produce abstracciones equivocadas
  que después hay que deshacer (deuda técnica).

---

## DRY - en Configuración (Factor III)

```csharp
// Mal: Connection string hardcoded — viola DRY + Factor III
public class UserRepository
{
    private readonly NpgsqlConnection _conn = new(
        "Host=localhost;Port=5432;Database=reddit;Username=dev;Password=secret");
}

// Bien: La configuración vive en UN solo lugar (appsettings / env var)
public class UserRepository
{
    private readonly string _connString;
    public UserRepository(IConfiguration config)
        => _connString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing connection string.");
}
```

Si el host cambia -> cambias el `.env` o `appsettings.json`. Un solo lugar.

---

<!-- _class: lead -->

# Parte 2
## KISS - Keep It Simple, Stupid

---

## KISS - La premisa

**La complejidad es deuda técnica inmediata.**

Cada capa de abstracción que agregas sin justificación:
- Es un archivo más para navegar
- Es una indirección más para razonar
- Es una superficie más para que los bugs se escondan

> *"La perfección no se alcanza cuando no hay nada que agregar,
> sino cuando no hay nada que quitar."*
> - Antoine de Saint-Exupéry

---

## KISS - Complejidad Ciclomática

La complejidad ciclomática cuenta el número de caminos independientes
a través de un método. Más caminos = más difícil de entender y testear.

```csharp
// Complejidad ciclomática = 5 (un if por cada rama)
public string GetFeedType(User user, Community community)
{
    if (user.IsAdmin)                return "all";
    if (community.IsPrivate)         return "members-only";
    if (user.IsMemberOf(community))  return "subscribed";
    if (user.HasKarma(100))          return "trusted";
    return "default";
}

// Umbral recomendado: ≤ 10 por método
// Señal de alerta:   > 15 → dividir en métodos más pequeños
```

---

## KISS - El costo de la abstracción prematura

```csharp
// Mal: Over-engineered para renderizar el feed del Reddit-clone
public interface IFeedRenderStrategy        { }
public interface IFeedRenderContext         { }
public class FeedRenderStrategyFactory      { }
public class FeedRenderContextBuilder       { }
public class DefaultFeedRenderStrategy      : IFeedRenderStrategy { }
public class FeedRenderDirector             { }
// 6 clases para... ¿qué problema concreto?

// Bien: KISS — resolver el problema concreto de hoy
public class GetFeedHandler
{
    public async Task<GetFeedResponse> Handle(GetFeedRequest req, CancellationToken ct)
    {
        var posts = req.Sort switch {
            "hot"  => await _repo.GetHotAsync(req.CommunityId, req.Cursor, ct),
            "top"  => await _repo.GetTopAsync(req.CommunityId, req.Period, req.Cursor, ct),
            _      => await _repo.GetNewAsync(req.CommunityId, req.Cursor, ct),
        };
        return new GetFeedSuccess(posts);
    }
}
```

---

## KISS - Cuándo una abstracción SÍ vale su costo

Una abstracción justificada resuelve **al menos uno** de estos problemas:

| Problema | Abstracción que lo resuelve |
|----------|---------------------------|
| Lógica que cambia independientemente | Strategy, Policy |
| Dependencia de infraestructura concreta | Interface + DIP |
| Código que se repite ≥3 veces (Regla de Tres) | Helper, Value Object, Extension Method |
| Complejidad ciclomática > 10 | Extract Method |

**Si no resuelve ninguno de estos hoy -> YAGNI.**

---

<!-- _class: lead -->

# Parte 3
## YAGNI - You Aren't Gonna Need It

---

## YAGNI - La definición exacta

> *"Always implement things when you actually need them,
> never when you just foresee that you need them."
> "Implementa siempre las cosas cuando realmente las necesites,
> nunca cuando simplemente preveas que las necesitarás."*
> - Ron Jeffries (XP)

**No es:** "no escribas tests"
**No es:** "no pienses en el futuro"
**Sí es:** no implementes funcionalidad que ningún requisito actual exige

---

## YAGNI - El error del quiz (aclaración)


Esto es incorrecto. Analicemos por qué:

- YAGNI habla de **funcionalidad de producto**, no de testing
- Los tests respaldan refactoring futuro -> son inversión, no especulación
- "100% de cobertura" es un requisito de calidad - no es YAGNI aunque sea alto

**YAGNI en contexto del sistema de mensajería:**
- ¿Alguien pidió soporte multi-idioma en v1? No -> YAGNI
- ¿Alguien pidió exportar reportes a Excel? No (aún) -> YAGNI
- ¿Alguien pidió tests unitarios? Sí -> no es YAGNI, es el plan

---

## YAGNI - Speculative Generality (el smell)

```csharp
// Mal: Speculative Generality — "por si acaso necesitamos esto después"
public abstract class BasePostScorer<T> where T : class { }     // nadie lo pidió
public interface IPostScoringAlgorithm { }                      // nadie lo pidió
public class HotScoringAlgorithm  : IPostScoringAlgorithm { }
public class TopScoringAlgorithm  : IPostScoringAlgorithm { }
public class NewScoringAlgorithm  : IPostScoringAlgorithm { }
public class PostScoringFactory   { }                           // nadie lo pidió

// Bien: YAGNI — el requisito actual es: hot/top/new en el mismo Handler
public async Task<GetFeedResponse> Handle(GetFeedRequest req, CancellationToken ct)
{
    var posts = req.Sort switch {
        "hot" => await _repo.GetHotAsync(req.CommunityId, req.Cursor, ct),
        "top" => await _repo.GetTopAsync(req.CommunityId, req.Period, req.Cursor, ct),
        _     => await _repo.GetNewAsync(req.CommunityId, req.Cursor, ct),
    };
    return new GetFeedSuccess(posts);
}
// Si en S05 el algoritmo de ranking se vuelve intercambiable → ENTONCES se extrae Strategy
```

---

## Los tres juntos - cómo se complementan

| Principio | Pregunta que responde |
|-----------|----------------------|
| **DRY** | ¿Este conocimiento ya existe en otro lugar? |
| **KISS** | ¿Esta abstracción resuelve un problema concreto hoy? |
| **YAGNI** | ¿Alguien pidió esta funcionalidad en los requisitos actuales? |

**En la práctica:** aplica los tres en ese orden al diseñar.
Si alguna respuesta es "no" -> no lo hagas (todavía).

---

<!-- _class: lead -->

# Parte 4
## Architecture Decision Records (ADRs)

---

## El problema que los ADRs resuelven

**¿Cuántas veces un equipo vuelve a debatir la misma decisión técnica?**

- "¿Por qué usamos EF Core y no Dapper?"
- "¿Por qué monolito y no microservicios?"
- "¿Por qué PostgreSQL y no SQL Server?"

Sin registro escrito:
- Cada dev nuevo hace la misma pregunta
- La decisión original se pierde con la rotación del equipo
- El equipo pierde tiempo re-debatiendo en lugar de construir

**Los ADRs cortan este ciclo.**

---

## ¿Qué es un ADR?

Un **Architecture Decision Record** es un documento corto que registra:

1. **El contexto** - ¿qué situación forzó la decisión?
2. **La decisión** - ¿qué elegimos?
3. **Las consecuencias** - ¿qué ganamos y qué perdemos?

**Características:**
- Son **inmutables** - no se editan, se superseden
- Viven **en el repo** junto al código
- Son **breves** - 1-2 páginas máximo

---

## Formato MADR (Markdown ADR)

```markdown
# ADR-001 — [Título]

**Status:** Accepted | Proposed | Deprecated | Superseded by ADR-XXX
**Date:** YYYY-MM-DD
**Deciders:** [nombres o roles]

## Contexto

[¿Qué situación, restricción o necesidad forzó esta decisión?]

## Decisión

[¿Qué elegimos y por qué?]

## Consecuencias

**Positivas:**
- [beneficios concretos]

**Negativas / Trade-offs:**
- [costos o limitaciones que aceptamos]
```

---

## ADR-001 - Monolito Modular como arquitectura inicial

```markdown
# ADR-001 — Monolito Modular como arquitectura inicial

**Status:** Accepted · 2026-05-17
**Deciders:** Equipo técnico

## Contexto
Equipo de 6 desarrolladores construyendo el sistema de mensajería desde cero.
El dominio no está completamente definido. No hay DevOps maduro aún.

## Decisión
Iniciar con un monolito modular (Clean Architecture).
Extraer módulos a microservicios cuando el dominio lo justifique
y cuando el equipo tenga CI/CD maduro (estimado: fase IV del programa).

## Consecuencias
Sí: Menor complejidad operacional desde el inicio
Sí: Dominio más fácil de refactorizar mientras se aprende
Sí: Un solo pipeline de CI/CD para toda la app
No: Escalar módulos individualmente requiere refactoring futuro
```

---

## ADR-002 - PostgreSQL como base de datos principal

```markdown
# ADR-002 — PostgreSQL como base de datos principal

**Status:** Accepted · 2026-05-17
**Deciders:** Equipo técnico

## Contexto
El sistema de mensajería necesita FTS nativo para búsqueda de conversaciones y contactos,
JSONB para flujos del bot, y RLS como capa adicional de aislamiento multi-tenant.

## Decisión
PostgreSQL 16. Alojado en AWS RDS en producción, Docker en local.

## Consecuencias
Sí: FTS nativo con índices GIN (evita Elasticsearch en v1)
Sí: JSONB para definición de flujos del bot
Sí: RLS como segunda capa de aislamiento por workspace
Sí: Open source — sin licencia
No: El equipo conoce SQL Server — requiere aprender PostgreSQL-specific syntax
```

---

## ADR-003 - ORM: EF Core vs. Dapper

El proceso de decisión es tan importante como la decisión. Dos opciones evaluadas:

```markdown
### Opción A — Dapper (SQL explícito)
Sí: Control total del SQL generado
Sí: Sin overhead de change tracking en reads
Sí: Ideal para CTEs recursivas, window functions, cursor pagination
No: TenantId debe pasarse manualmente a cada query — riesgo de fuga de datos
No: Más código de mapeo manual
```

```markdown
### Opción B — EF Core 10 (ORM completo)
Sí: Global query filter — TenantId aplicado automáticamente en *todas* las queries
Sí: LINQ fuertemente tipado — el compilador detecta errores de mapeo
Sí: AsNoTracking() en reads, ExecuteUpdateAsync() sin cargar la entidad
No: SQL generado menos predecible en queries muy complejas (usar FromSqlRaw controlado)
No: Curva de aprendizaje en configuraciones avanzadas
```

---

## ADR-003 - Decisión: EF Core 10

```markdown
# ADR-003 — EF Core 10 como ORM principal

**Status:** Accepted · 2026-05-17
**Deciders:** Equipo técnico

## La razón decisiva
El global query filter de EF Core aplica TenantId en *todas* las queries
automáticamente. Con Dapper, un query sin el filtro expone datos de otro
workspace. EF Core convierte el aislamiento multi-tenant en un problema de
configuración, no de disciplina de cada desarrollador.

## Decisión
EF Core 10 con Npgsql 10.0.1. EntityTypeConfigurations por entidad.
DatabaseInitializationService con EnsureCreatedAsync() en arranque.

## Consecuencias
Sí: Aislamiento de datos por workspace garantizado por el ORM
Sí: ExecuteUpdateAsync() para updates sin cargar la entidad completa
Sí: IgnoreQueryFilters() solo en auth (login/refresh sin tenant context)
No: Para queries muy complejas → FromSqlRaw con parámetros, nunca interpolación
```

---

## Dónde viven los ADRs en el repo

```
mensajeria/
├── docs/
│   └── adr/
│       ├── ADR-001-monolito-modular.md
│       ├── ADR-002-postgresql.md
│       └── ADR-003-ef-core.md
├── Domain/
├── Application/
├── Infrastructure/
├── WebApi/
├── Host/
└── Common/
```

**Convención de nombrado:**
`ADR-{número 3 dígitos}-{slug-de-la-decisión}.md`

Cuando una decisión cambia -> crear `ADR-004-...-supersedes-ADR-001.md`
y actualizar el Status del ADR original a `Superseded by ADR-004`.

---

## Los ADRs no son documentación de arquitectura

| ADR | Documentación de arquitectura |
|-----|-------------------------------|
| "¿Por qué elegimos X sobre Y?" | "¿Cómo funciona el sistema?" |
| Inmutable - registra el pasado | Actualizable - describe el presente |
| 1-2 páginas | Puede ser extenso |
| Vive en el repo | Puede estar en Confluence/Notion |
| Orientado a decisiones puntuales | Orientado a vista de sistema |

Los ADRs **complementan** la documentación, no la reemplazan.

---

## Hands-on - ### ADRs en el repo del sistema de mensajería

1. Crear `docs/adr/` en el repo
2. Escribir `ADR-001-monolito-modular.md` en equipo
3. Cada persona redacta `ADR-002` o `ADR-003` individualmente
4. PR -> review -> merge

**Objetivo:** al final del bloque, el repo tiene 3 ADRs mergeados.

---

## Refactoring Kata - ### DRY/KISS/YAGNI en el código del proyecto

**Kata 1 - DRY:** Extraer `Username` como Value Object
**Kata 2 - KISS:** Simplificar el handler más complejo del template
**Kata 3 - YAGNI:** Identificar abstracciones innecesarias y eliminarlas

**Regla:** un paso a la vez - commit después de cada cambio - tests pasan antes del commit.

---

## Homework S02

### Lecturas (1-2 horas)
- `dev-notes/01-fundamentos/03-refactoring.md` - técnicas de refactoring
- *The Pragmatic Programmer* (Hunt & Thomas) - Ch.7 DRY (si está disponible)

### Ejercicio (1-2 horas)
Crear en tu fork del repo:

1. Value Object `Username` con validación completa
2. Value Object `Email` (si no lo hiciste en S01)
3. Un ADR adicional a los 3 del hands-on - una decisión técnica real que hayas tomado

**Criterio de aceptación:** PR con los 2 Value Objects y el ADR - proyecto compila - se revisa en S03.

---

## S03 - Preview

**C# avanzado: el lenguaje que usaremos todo el programa**

- Records, Pattern Matching, Generics, Async/Await en profundidad
- Collections y LINQ funcional
- Configuration y Options Pattern
- Tour completo del back-template: cada pieza del puzzle

**Prerrequisito:** tener el back-template compilando localmente
con Docker y PostgreSQL corriendo.

---

<!-- _class: lead -->

# ¡A escribir ADRs!
## Hands-on
### Guía en el documento S02-hands-on

---

<!-- _class: lead -->

# Parte IV - Metodología y Proceso

---

## Agile / Scrum en nuestro equipo

**Scrum en 3 roles:**

| Rol | Responsabilidad |
|-----|----------------|
| **Product Owner** | Prioriza el backlog - define criterios de aceptación |
| **Scrum Master** | Facilita ceremonias - remueve impedimentos |
| **Dev Team** | Auto-organizado - entrega el Increment |

**Nuestra cadencia - sprints de 2 semanas:**

- **Lunes S1** - Sprint Planning (¿qué comprometemos? ¿cómo lo hacemos?)
- **Diario** - Daily Standup - 15 min - 3 preguntas
- **Viernes S2** - Sprint Review + Demo al PO
- **Viernes S2** - Retrospectiva (¿qué cambiamos el próximo sprint?)

> Un sprint termina con un **Increment potencialmente liberable** - no con trabajo al 80%.

---

## Definición de Done (DoD)

La DoD es el acuerdo de equipo que define cuándo una historia está **terminada de verdad**.

**Sin DoD, el sprint nunca termina.**

**DoD mínima para el sistema de mensajería:**

- Código en rama de feature, compilando
- Tests unitarios pasando
- PR con criterios de aceptación validados
- Code review aprobado por al menos 1 compañero
- Build de CI verde (compila + tests)
- Merged a `develop`

**Lo que NO cuenta como Done:**
- "Funciona en mi máquina"
- "El PR está abierto"
- "Solo falta un detalle"

---

## Ciclo de Vida del Software (SDLC)

**Fases del SDLC - visión general:**

```
Requerimientos → Diseño → Desarrollo → Testing → Despliegue → Mantenimiento
```

**Dónde encaja Scrum:**

| Fase | Scrum la resuelve con... |
|------|--------------------------|
| Requerimientos | Product Backlog + Refinamiento |
| Diseño | Sprint Planning + decisiones técnicas del Dev Team |
| Desarrollo | El sprint en sí (iteración fija) |
| Testing | Parte de la DoD - no una fase separada |
| Despliegue | Increment al final de cada sprint |
| Mantenimiento | Bugs como ítems del Product Backlog |

> Scrum no elimina las fases - las **integra en cada iteración**.
> No hay una fase de "testing" al final de 6 meses.

---

## Deuda Técnica - Qué es y por qué importa

**Definición:**
> Trabajo técnico pendiente que aceptamos conscientemente (o acumulamos por descuido)
> a cambio de entregar más rápido hoy - y que pagaremos con intereses mañana.

**Dos tipos:**

| Tipo | Origen | Ejemplo |
|------|--------|---------|
| **Deliberada** | Decisión consciente con plan de pago | "Hardcodeamos el timeout ahora, lo configuramos más adelante" |
| **Accidental** | Falta de conocimiento o descuido | Duplicar lógica de validación en 3 handlers |

**Cómo DRY - KISS - YAGNI la previenen:**

- **DRY** - elimina la deuda de duplicación (cambios en N lugares)
- **KISS** - elimina la deuda de complejidad innecesaria
- **YAGNI** - elimina la deuda de código que nadie pidió

---

## Deuda Técnica en el backlog

**Cuadrante de Fowler:**

| | Deliberada | Accidental |
|---|---|---|
| **Prudente** | "Entregamos ahora, refactorizamos más adelante" | "Ahora que lo vemos, el patrón era equivocado" |
| **Imprudente** | "No hay tiempo para diseñar bien" | "¿Qué es el diseño?" |

> La deuda prudente y deliberada es una **herramienta de gestión**.
> La deuda imprudente es simplemente descuido.

**Cómo gestionarla:**

- Documentar con `// TODO: [razón] — pagar en S0X` (no TODOs sin contexto)
- Crear un ítem en el backlog cuando la deuda bloquea velocidad
- Priorizar junto al PO - la deuda técnica compite con features nuevas

**Boy Scout Rule** (de `03-refactoring.md`):
> *"Deja el código más limpio de como lo encontraste."*
> Pequeñas mejoras en cada PR reducen la deuda gradualmente - sin sprints de refactoring.
