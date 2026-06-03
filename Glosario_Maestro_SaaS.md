# Glosario Maestro de Capacitación Técnica
## Construcción de SaaS Multi-Tenant en Cloud

> ERPs · CRMs · Plataformas Digitales · Productos SaaS  
> Stack completo: .NET · React · SQL/PostgreSQL · Docker · DevOps · AWS/Azure · Multi-Tenancy · Patrones de diseño

**Instructor:** Ing. Rogelio Arriaga Gonzalez  
M.I. en Gestión de Sistemas de TI · Especialista en .NET, arquitectura backend y sistemas empresariales

---

## Introducción

Este documento es la referencia maestra de la capacitación técnica. Está diseñado para llevar a un desarrollador desde fundamentos sólidos hasta la construcción de productos SaaS reales: aplicaciones que múltiples clientes (tenants) usan simultáneamente, desplegadas en cloud, escalables, seguras y monetizables.

El alcance no es académico: cada concepto incluido aquí es uno que el desarrollador va a tocar, decidir o defender en el día a día construyendo ERPs, CRMs, plataformas verticales o productos digitales. No hay relleno. No hay buzzwords vacíos. Cada término viene con su definición, su porqué y cuándo se usa.

### Pedagogía del curso

- **Pensar en multi-tenant desde el día uno.** Un sistema mono-tenant que después se quiere convertir en SaaS es tres veces más caro de migrar que uno construido bien desde el inicio.
- **Primero la decisión, después el código.** Un desarrollador profesional decide qué patrón, qué arquitectura, qué herramienta. La sintaxis se aprende en horas; la decisión correcta se forma con años.
- **Mostrar lo real, no lo de tutorial.** Los ejemplos reflejan productos SaaS vivos: aislamiento de tenants, billing, onboarding, white-labeling, despliegue continuo en cloud.
- **Versionar todo, automatizar todo, documentar todo.** Estos tres hábitos separan a un programador de un ingeniero capaz de construir productos.

---

## Mapa del programa

| Dominio | Qué cubre | Por qué importa |
|---------|-----------|-----------------|
| 1. Fundamentos de Ingeniería de Software | Paradigmas, arquitecturas, ciclo de vida, SOLID | Es la gramática profesional. Sin esto, todo lo demás se construye sobre arena. |
| 2. Programación y Patrones de Diseño | POO, patrones GoF, DRY/KISS/YAGNI, clean code | Decide la calidad y la mantenibilidad del código. |
| 3. Arquitectura SaaS Multi-Tenant | Aislamiento, modelos de tenancy, billing, onboarding | El alma de cualquier producto SaaS. Aquí está la diferencia entre vender una vez y vender mil veces. |
| 4. Backend con .NET y C# | ASP.NET Core, EF Core, Mediator, Web API, DI | El motor de la lógica de negocio del producto. |
| 5. Bases de Datos (SQL / PostgreSQL) | Modelado, queries, índices, RLS, sharding, multi-tenant data | Donde vive la verdad del negocio. En SaaS, también donde vive la separación entre clientes. |
| 6. Frontend con React y Tailwind | React 19, hooks, design system, white-labeling, SignalR | La capa que el usuario toca y juzga. |
| 7. Control de Versiones (Git + GitFlow) | Branching, conventional commits, semver, tags, code review | Cómo trabajar en equipo sin pisarse. |
| 8. Contenedores y Registries | Docker, Compose, imágenes ligeras, multi-stage builds | Empaquetar el producto y distribuirlo a cualquier ambiente sin sorpresas. |
| 9. CI/CD y Pipelines | Azure DevOps, GitHub Actions, Terraform, automatización | Convertir 'funciona en mi máquina' en 'desplegado a 50 clientes en una hora'. |
| 10. Cloud, Linux y Operación SaaS | AWS, Azure, Nginx, observabilidad, escalado, seguridad, costos | Mantener vivo y rentable lo que ya está construido. |
| 11. Vibe Coding y Agentes de IA | LLMs, prompts, agentes, LLMOps | Multiplica la productividad del ingeniero competente. |

---

## Dominio 1 · Fundamentos de Ingeniería de Software

### 1.1 Paradigmas de programación

**Programación imperativa**  
El programador describe paso a paso cómo el programa debe ejecutarse. C, ensamblador y gran parte de los lenguajes OO.

**Programación declarativa**  
El programador describe qué resultado quiere, sin detallar el cómo. SQL, HTML, React.

**Programación orientada a objetos (POO)**  
Modela el mundo en clases y objetos. Su valor real está en encapsulamiento, herencia y polimorfismo. Dominante en software empresarial: C#, Java, TypeScript.

**Programación funcional**  
Funciones puras, inmutabilidad, sin efectos secundarios. Dada una entrada, siempre devuelve la misma salida sin alterar el mundo. Técnicas funcionales se mezclan en C# y JavaScript modernos.

**Programación reactiva**  
El sistema reacciona a flujos de eventos. Modelo natural de interfaces modernas (eventos, streams, observables) y sistemas distribuidos.

### 1.2 Arquitecturas de software

| Arquitectura | Cuándo se usa en SaaS |
|---|---|
| Monolítica modular | Una sola aplicación bien estructurada en módulos. El inicio correcto para casi todo SaaS nuevo. |
| Cliente-Servidor | Base de la web y de cualquier SaaS. |
| Multicapa (n-tier) | Separación lógica: presentación, aplicación, dominio, datos. Base del software empresarial moderno. |
| Hexagonal (Ports & Adapters) | El núcleo no depende de nada externo. Bases de datos, APIs, UI son adaptadores enchufables. |
| Microservicios | Cada servicio es una aplicación independiente. Útil cuando el producto es grande y hay equipos especializados. Costoso de operar. |
| Modular monolith | Módulos con fronteras claras dentro de una sola aplicación. Permite extraer microservicios después si se justifica. |
| Orientada a eventos | Componentes producen y consumen eventos. Acoplamiento mínimo. |
| Serverless / FaaS | El código se ejecuta solo cuando hay petición. La nube administra todo. |

### 1.3 Ciclo de vida del software (SDLC)

**Cascada (Waterfall)**: Cada fase termina antes de empezar la siguiente. Para proyectos con requisitos cerrados (gobierno, normativos).

**Iterativo / incremental**: Se entrega valor en bloques. Cada iteración revisa, ajusta y suma.

**Ágil (Agile)**: Filosofía que prioriza personas, software funcionando y respuesta al cambio. Materializada en Scrum, Kanban, XP.

**Scrum**: Sprints de 1-4 semanas, roles (Product Owner, Scrum Master, equipo), eventos (planning, daily, review, retro).

**Kanban**: Tablero visual sin sprints fijos. Flujo continuo. Útil para soporte y operación de SaaS en marcha.

**DevOps**: La frontera entre desarrollo y operaciones desaparece. CI/CD, IaC, observabilidad, automatización.

### 1.4 Principios SOLID

| Principio | Significado |
|---|---|
| S — Single Responsibility | Cada clase tiene una sola razón para cambiar. |
| O — Open/Closed | Abierto a extensión, cerrado a modificación. |
| L — Liskov Substitution | Una subclase debe poder usarse donde se espera la clase padre sin romper el comportamiento. |
| I — Interface Segregation | Mejor varias interfaces específicas que una grande. |
| D — Dependency Inversion | Depender de abstracciones, no de implementaciones concretas. |

### 1.5 Otros principios fundamentales

**DRY (Don't Repeat Yourself)**: Cada pieza de conocimiento tiene una representación única en el sistema.

**KISS (Keep It Simple, Stupid)**: Si una solución simple y una compleja resuelven lo mismo, gana la simple.

**YAGNI (You Aren't Gonna Need It)**: No construyas funcionalidad "por si acaso". Especialmente importante en SaaS donde el feature creep mata productos.

**Fail Fast**: Detecta y reporta errores lo antes posible.

**Separation of Concerns (SoC)**: Cada componente atiende una responsabilidad clara.

**Twelve-Factor App**: Metodología para construir aplicaciones SaaS modernas: codebase única, dependencias explícitas, configuración en entorno, procesos sin estado, logs como streams, etc.

---

## Dominio 2 · Programación orientada a objetos y patrones de diseño

### 2.1 Conceptos centrales de POO

**Clase**: Plantilla que define estructura (atributos) y comportamiento (métodos).

**Objeto / Instancia**: Materialización de una clase en memoria con estado propio.

**Encapsulamiento**: Esconder detalles internos y exponer solo lo necesario.

**Herencia**: Una clase hija recibe atributos y métodos de la clase padre. Útil con moderación.

**Polimorfismo**: Un mismo método se comporta distinto según el tipo concreto del objeto.

**Abstracción**: Definir qué hace algo sin definir cómo. Una interfaz es abstracción pura.

**Composición sobre herencia**: En lugar de "A es un B", preferir "A tiene un B". Más flexible, menos acoplamiento.

### 2.2 Patrones creacionales

**Singleton**: Una sola instancia de una clase en todo el sistema. Útil para logger, configuración, cache compartido. Mal usado, mata la testabilidad.

**Factory Method**: Delega la creación de objetos a un método especializado. En SaaS multi-tenant, común para crear conexiones o servicios según el tenant activo.

**Abstract Factory**: Familia de fábricas relacionadas. Útil cuando hay que crear conjuntos coherentes de objetos por tenant.

**Builder**: Construye objetos complejos paso a paso. Cuando un objeto tiene muchos parámetros opcionales evita constructores monstruosos.

**Prototype**: Crea nuevos objetos clonando una plantilla. Útil para clonar configuraciones de tenant.

### 2.3 Patrones estructurales

**Adapter**: Convierte la interfaz de una clase en otra que el cliente espera. El "traductor" para integrar componentes legacy o de terceros.

**Decorator**: Agrega responsabilidades a un objeto en tiempo de ejecución sin modificar su clase. Base de los middlewares en ASP.NET y de logging y caching transversales.

**Facade**: Expone una interfaz simple sobre un subsistema complejo. Un `BillingFacade` que oculta Stripe, webhooks, notificaciones y actualización de estado del tenant.

**Proxy**: Sustituto que controla el acceso a otro objeto. Para caching, control de acceso, lazy loading.

**Composite**: Trata objetos individuales y grupos de forma uniforme. Estructura clásica de árbol: DOM, sistema de archivos, jerarquías de permisos.

### 2.4 Patrones de comportamiento

**Strategy**: Familia de algoritmos intercambiables. Reemplaza if/else por composición. Ejemplo SaaS: estrategias de pricing por plan.

**Observer**: Dependencia uno-a-muchos: cuando un objeto cambia, todos sus dependientes son notificados. Base de eventos y suscripciones.

**Command**: Encapsula una solicitud como un objeto. Permite hacer/deshacer, encolar. Es el pilar del CQRS y de los handlers de Mediator.

**Mediator**: Un objeto centraliza la comunicación entre componentes para que no se conozcan entre sí. En .NET, MediatR o el mediator de Common son implementaciones dominantes.

**Chain of Responsibility**: Cadena de manejadores donde cada uno decide si procesa la solicitud o la pasa al siguiente. Modelo del middleware HTTP.

**State**: Permite que un objeto cambie su comportamiento cuando cambia su estado interno. Útil para máquinas de estado de suscripciones (trial, active, past_due, canceled).

**Template Method**: Define el esqueleto de un algoritmo en una clase base y permite que las subclases redefinan ciertos pasos.

### 2.5 Patrones de arquitectura empresarial

**Repository**: Abstrae el acceso a datos. La capa de aplicación pide al repositorio objetos sin saber si vienen de SQL, un archivo o un cache.

**Unit of Work**: Agrupa operaciones de persistencia para que se hagan commit o rollback como una sola transacción. En EF Core, el `DbContext` actúa como Unit of Work nativo.

**CQRS (Command Query Responsibility Segregation)**: Separa operaciones que modifican estado (commands) de las que solo leen (queries). Permite optimizar cada lado por separado. Muy útil en SaaS donde la lectura es el 90% del tráfico.

**Specification**: Encapsula criterios de búsqueda o validación como objetos combinables con operadores lógicos (`And`, `Or`, `Not`). Permite construir queries dinámicas sin condicionales dispersos.

**Result Pattern**: Encapsula éxito o falla como un tipo de retorno en lugar de lanzar excepciones. `Result<T>` puede ser `Success<T>`, `NotFound`, `Forbidden`, `Conflict`, `ValidationFailure`. Las excepciones quedan reservadas para errores realmente excepcionales.

**Outbox Pattern**: Garantiza que los eventos de dominio se publiquen incluso si el broker cae. Los eventos se persisten en una tabla `OutboxMessages` en la misma transacción de BD que el cambio de estado. Un `BackgroundService` los lee y los publica al broker, marcándolos como procesados.

**Event Sourcing**: En lugar de guardar el estado actual, se guardan todos los eventos que llevaron a él. El estado se reconstruye reproduciendo los eventos. Útil para auditoría fuerte y reportes históricos.

**Saga**: Patrón para coordinar transacciones distribuidas que no pueden hacer rollback ACID. Cada paso tiene su compensación. Crítico cuando hay integraciones externas: cobro, envío de email, provisioning de recursos cloud.

**DTO (Data Transfer Object)**: Objeto plano que solo lleva datos, sin lógica. Se usa en las fronteras del sistema (API requests/responses) para no exponer entidades internas.

**MVC**: Modelo, Vista, Controlador. Base de ASP.NET MVC.

**MVVM**: Variante con ViewModel que adapta el modelo a la vista. Natural en frameworks con binding declarativo.

**Anti-Corruption Layer (ACL)**: Capa de traducción que aísla el dominio propio de modelos externos (ERPs legacy, APIs de terceros). Evita que el vocabulario externo contamine el modelo del dominio.

**Shared Kernel**: Código compartido entre múltiples bounded contexts que cambia raramente y ambos equipos acuerdan explícitamente. Tipos de valor como `UserId`, `Money`, `TenantId`.

### 2.6 Clean Code

- **Nombres expresivos.** `daysSinceLastPayment` es claridad; `d` es deuda técnica.
- **Funciones cortas.** Una función hace una cosa y cabe en una pantalla.
- **Sin comentarios obvios.** Comenta el porqué, no el qué.
- **Sin código muerto.** Si está comentado, bórralo. Git lo recuerda.
- **Sin números mágicos.** Usa constantes con nombre.
- **Manejo explícito de errores.** Un `catch {}` vacío es un crimen.
- **Niveles de abstracción consistentes.** Una función no mezcla lógica de negocio con IO.

---

## Dominio 3 · Arquitectura SaaS Multi-Tenant

### 3.1 Conceptos fundamentales

**SaaS (Software as a Service)**: Software ofrecido como servicio, accesible vía web, facturado por suscripción. El cliente no instala ni opera infraestructura.

**Tenant**: Cliente del SaaS. Una empresa, una organización, una cuenta. Cada tenant tiene sus propios datos, usuarios, configuración y plan.

**Multi-tenancy**: Capacidad del sistema de servir a múltiples tenants desde la misma base de código e infraestructura. La separación entre tenants es lógica.

**Tenant isolation**: Garantía de que un tenant nunca pueda ver, modificar o afectar los datos de otro. Es el requisito más crítico de cualquier SaaS.

**Onboarding**: Proceso por el que un nuevo tenant entra al sistema: registro, verificación, configuración inicial, primer uso.

**Self-service**: El cliente se da de alta, configura y opera sin intervención humana. Marca de un SaaS maduro.

**White-labeling**: Capacidad de presentar el producto con la marca del tenant: logo, colores, dominio personalizado.

### 3.2 Modelos de tenancy

| Modelo | Cómo funciona | Cuándo conviene |
|---|---|---|
| Database per tenant | Cada tenant tiene su propia base de datos. Aislamiento físico total. | Clientes enterprise con regulación estricta. Costoso de mantener. |
| Schema per tenant | Una sola base, un schema por tenant. Aislamiento lógico fuerte. | Compromiso intermedio. Hasta cientos de tenants. |
| Shared schema (Pool) | Una sola base, columna `TenantId` en todas las tablas. | Modelo más común. Escala a miles de tenants. Requiere disciplina. |
| Híbrido | Pool por defecto; clientes premium en database/schema dedicado. | Atender pequeños clientes con bajo costo y enterprise con aislamiento. |

**Regla práctica:** Empieza con shared schema. Solo evolucionar a schema o database dedicado cuando un cliente específico lo justifique comercial o regulatoriamente.

### 3.3 Resolución de tenant

**Por subdominio**: `acme.miproducto.com → tenant 'acme'`. Opción más común y la mejor UX. Requiere wildcard DNS y SSL multi-dominio.

**Por path**: `miproducto.com/acme/...`. Más simple operativamente, peor UX.

**Por header**: Header `X-Tenant-Id` en cada request. Útil para APIs públicas.

**Por dominio personalizado**: `tenant.cliente.com` apunta al SaaS. Requisito de white-labeling. Implica gestión de certificados SSL automática.

**Por claim del JWT**: El token contiene el `tenantId`. Combina bien con las anteriores.

**Tenant Resolver Middleware**: Componente del pipeline HTTP que identifica el tenant al inicio del request y lo deja disponible para toda la cadena posterior.

### 3.4 Tenant context y propagación

**ITenantContext**: Servicio inyectable que expone `TenantId`, nombre, plan, configuración.

**AsyncLocal<T>**: Mecanismo de .NET para almacenar valores que viajan con el flujo asíncrono. Implementación típica de TenantContext en peticiones HTTP.

**Tenant scoping en queries**: En modelo pool, cada query debe incluir `WHERE TenantId = @currentTenant`. Olvidarlo expone datos de otros tenants. Se combina con Row-Level Security o filtros globales del ORM.

**Tenant en jobs en background**: Un job debe recibir el `TenantId` como parámetro explícito. Nunca asumir el contexto actual del trabajador.

### 3.5 Modelos de billing y monetización

**Per-seat / per-user**: Se cobra por usuario activo. Modelo de Slack, Salesforce.

**Per-usage / metered**: Se cobra por consumo: peticiones, mensajes, GB almacenados. Modelo de Twilio, AWS.

**Tiered / planes**: Planes Free / Pro / Business / Enterprise con features y límites distintos. Requiere feature flagging por plan.

**Freemium**: Plan gratuito con limitaciones, plan pagado para crecer.

**Annual contract**: Suscripción anual con descuento. Mejora retención y previsibilidad.

**Trial**: Período de prueba gratuito al inicio (14-30 días). Con tarjeta convierte 5x más.

### 3.6 Métricas SaaS críticas

| Métrica | Qué mide |
|---|---|
| MRR (Monthly Recurring Revenue) | Ingreso mensual recurrente. La métrica norte de cualquier SaaS. |
| ARR (Annual Recurring Revenue) | MRR × 12. El indicador que importa a inversionistas. |
| ARPU (Average Revenue Per User) | Ingreso promedio por cuenta. Mide eficiencia comercial. |
| Churn rate | Porcentaje de tenants que cancelan al mes. >5% mensual es preocupante en B2B. |
| LTV (Lifetime Value) | Ingreso total esperado de un tenant durante su vida útil. |
| CAC (Customer Acquisition Cost) | Costo de adquirir un cliente nuevo. LTV/CAC > 3 es saludable. |
| NRR (Net Revenue Retention) | Retención + expansión - churn. >100% significa que crece sin nuevos clientes. |
| Activation rate | Porcentaje de signups que llegan al "aha moment". |
| Time to value | Tiempo desde el signup hasta el primer valor real percibido. |

### 3.7 Gateways de pago

**Stripe**: El estándar global. APIs limpias, soporte nativo de suscripciones, webhooks, multi-moneda.

**Conekta / OpenPay / Mercado Pago**: Gateways con énfasis en mercado latinoamericano. OXXO, SPEI, tarjetas locales.

**Webhooks de pago**: El gateway envía eventos al SaaS: pago exitoso, pago fallido, cancelación, disputa.

**Idempotency key**: Identificador que se envía con peticiones a gateways para evitar cobros duplicados ante reintentos.

**3D Secure / SCA**: Autenticación reforzada para pagos. Obligatoria en muchos países.

### 3.8 Feature flags y control por plan

**Feature flag**: Switch de configuración que activa o desactiva una feature sin redeploy. Permite habilitar features por tenant, por plan, por porcentaje de usuarios o por geografía.

**Feature por plan**: Asociación entre features y planes de suscripción. Si el plan Free no incluye 'export to Excel', la feature debe estar deshabilitada para esos tenants.

**Limits enforcement**: Aplicar límites del plan: número de usuarios, registros, peticiones, GB. Idealmente con grace period y notificaciones antes de bloquear.

**Herramientas**: LaunchDarkly, Unleash, Flagsmith. Para empezar, basta una tabla `feature_flags(tenant_id, feature_key, enabled)` con cache.

### 3.9 Onboarding y activación

- Signup mínimo: solo email y password (o OAuth).
- Verificación de email asíncrona. No bloquear el primer uso.
- Wizard de configuración inicial con pocos pasos.
- Datos de muestra en el primer login. Una pantalla vacía mata la activación.
- Aha moment medible. El equipo de producto lo define y lo optimiza.
- Email de bienvenida con próximos pasos.
- Detección de inactividad a los 3 días para intervención proactiva.

### 3.10 Aislamiento, seguridad y compliance

**Cross-tenant access**: Bug donde un tenant accede a datos de otro. Generalmente por una query sin filtro de `TenantId`. Es el bug más grave posible en SaaS.

**Defense in depth**: Múltiples capas: autenticación, autorización, filtro de tenant, RLS en BD, validación de pertenencia en handlers.

**Data residency**: Requisito legal de algunos clientes: los datos deben vivir en su país.

**GDPR / LFPDPPP**: Regulaciones de protección de datos personales (Europa y México). Consentimiento, derecho de acceso, derecho de borrado.

**SOC 2**: Estándar de seguridad y disponibilidad para SaaS. Exigido por enterprise.

**ISO 27001**: Estándar internacional de gestión de seguridad de la información.

---

## Dominio 4 · Backend con .NET y C#

### 4.1 Conceptos del runtime

**.NET Runtime**: Motor que ejecuta el código C# compilado. Incluye GC, JIT y librerías base. Multiplataforma.

**CLR (Common Language Runtime)**: Runtime histórico de .NET Framework. En .NET moderno es CoreCLR.

**IL (Intermediate Language)**: Código intermedio al que se compila C#. El JIT lo convierte a código nativo en ejecución.

**JIT (Just-In-Time)**: Compilación del IL a código nativo en el momento de ejecución.

**AOT (Ahead-Of-Time)**: Compilación a código nativo antes de ejecutar. Arranque más rápido, menor consumo de memoria.

**Garbage Collector (GC)**: Libera automáticamente memoria de objetos sin referencias. Generacional: Gen 0 (corta vida), Gen 1, Gen 2, LOH (Large Object Heap, objetos > 85 KB).

**NuGet**: Gestor de paquetes de .NET.

### 4.2 ASP.NET Core

**Kestrel**: Servidor web nativo de ASP.NET Core. Rápido, ligero, multiplataforma.

**Middleware**: Componentes que procesan cada petición HTTP en cadena. Authentication, logging, CORS, tenant resolution, manejo de errores.

**Dependency Injection (DI)**: Sistema integrado para resolver dependencias. Tres ciclos de vida: Singleton (vida del proceso), Scoped (vida del request), Transient (cada vez que se pide).

**Controller**: Clase que expone endpoints HTTP. En arquitecturas modernas, solo despacha al Mediator.

**Minimal APIs**: Endpoints sin controladores usando expresiones lambda. Para servicios pequeños.

**Routing**: Mapea URLs a endpoints. Soporta rutas con parámetros y restricciones.

**Model Binding**: Conversión automática de datos HTTP a parámetros tipados de C#.

**Filters**: Lógica transversal en endpoints: validación, autorización, logging. Alternativa al middleware cuando el contexto es de controlador.

### 4.3 Patrón de casos de uso (UseCase / Mediator)

En sistemas serios, los controladores son delgados. Cada operación de negocio se modela como caso de uso:

```
UseCases/Customers/CreateCustomer/
  Request.cs      → IRequest<TResponse> con los parámetros de entrada
  Handler.cs      → IRequestHandler<Request, TResponse> con la lógica
  Responses.cs    → contratos tipados de salida
```

El controlador recibe la petición, la convierte a `Request`, llama al Mediator, y este encuentra el Handler correcto.

**Pipeline Behaviors**: Middleware del Mediator. Se ejecutan en orden antes y después del Handler. Usos típicos: validación (FluentValidation), caching de queries, logging de performance. Se registran globalmente y aplican a todos los requests que cumplen la restricción de tipo.

```csharp
// Behavior que cachea queries que implementan ICacheableQuery
public sealed class QueryCachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery
{
    // ...
}
```

### 4.4 ORMs y acceso a datos

| ORM | Características |
|---|---|
| Entity Framework Core | ORM completo. Modelos en C#, LINQ a SQL, migraciones. Ideal para SaaS con esquema controlado por la app. |
| Dapper | Micro-ORM. Tú escribes el SQL, Dapper mapea los resultados. Más control, mejor rendimiento en reportes. |

**Migrations**: Versiones del esquema de BD como código. Cada cambio es una migración. EF Core las genera y aplica.

**Global Query Filters**: Feature de EF Core para aplicar filtros automáticos a todas las queries de una entidad. Implementación natural para tenant scoping y soft delete.

**AsNoTracking**: Deshabilita el tracking de EF Core para queries de solo lectura. Reduce uso de memoria y mejora rendimiento significativamente.

**Async/await**: Cualquier operación de IO debe ser async. Bloquear un hilo en una API desperdicia capacidad del servidor.

**Connection pooling**: Reutilización de conexiones a la BD. ADO.NET y EF lo manejan automáticamente.

### 4.5 Autenticación y autorización

**Autenticación**: Verificar quién es el usuario. Resultado: una identidad.

**Autorización**: Verificar qué puede hacer ese usuario autenticado. Resultado: permitir o denegar.

**JWT (JSON Web Token)**: Token firmado que contiene claims. Stateless: el servidor no guarda sesión. Estándar dominante en APIs modernas. El payload es solo base64, no está cifrado. Nunca poner información sensible.

**OAuth 2.0**: Protocolo para delegar autorización a un proveedor externo.

**OpenID Connect (OIDC)**: Capa de autenticación encima de OAuth. Lo que realmente usas con login de Google/Microsoft.

**RBAC (Role-Based Access Control)**: Permisos asignados a roles y roles a usuarios. Simple y comprensible.

**ABAC (Attribute-Based Access Control)**: Decisión basada en atributos del usuario, recurso y contexto. Más flexible, más complejo.

**Refresh token**: Token de larga duración para obtener nuevos JWTs sin volver a pedir credenciales. Se rota en cada uso (rotation pattern).

**Token Blacklist**: Para revocar JWTs antes de su expiración, el `jti` del token se almacena en Redis hasta que expire. Un middleware verifica en cada request.

**HS256 vs RS256**: HS256 usa un secreto compartido para firmar y verificar. RS256 usa par de claves: la privada firma (solo el auth server), la pública verifica (cualquier servicio). RS256 es preferible en microservicios.

**Keycloak**: Servidor de identidad open-source. Implementa OAuth 2.0 y OIDC. Soporta multi-tenancy por realms. Útil para M2M (machine-to-machine) con client credentials flow.

### 4.6 SignalR — comunicación en tiempo real

**SignalR**: Empuja datos del servidor al cliente sin polling. Base de notificaciones, dashboards en vivo, chat.

**Hub**: Clase del lado del servidor donde se definen métodos invocables por el cliente.

**WebSocket**: Protocolo bidireccional. Transporte preferido de SignalR. Cae a SSE o long-polling como fallback.

**Group**: Conjunto lógico de clientes. En SaaS, lo natural es agrupar por tenant para que las notificaciones lleguen solo a usuarios del tenant correcto.

**Backplane**: Escala SignalR horizontalmente: Redis o Azure SignalR Service. Necesario cuando hay múltiples instancias.

### 4.7 Validación y FluentValidation

**Data Annotations**: Atributos en propiedades de modelos. Simple, integrado al binding.

**FluentValidation**: Separa validación del modelo en clases dedicadas. Más expresiva, mejor testabilidad.

**Validator pipeline**: Cuando se combina FluentValidation con MediatR/Mediator, las validaciones se ejecutan automáticamente antes de cada handler.

### 4.8 Resiliencia con Polly

**Retry**: Reintentar operaciones fallidas con backoff exponencial y jitter. Útil para llamadas HTTP a servicios externos.

**Circuit Breaker**: Abre el circuito cuando las fallas superan un umbral. Evita cascada de fallos. Se cierra automáticamente después de un período.

**Timeout**: Límite de tiempo para una operación. Fail fast si el servicio externo no responde.

**Hedging**: Envía múltiples requests en paralelo y usa la primera respuesta. Para latencia P99.

**Microsoft.Extensions.Http.Resilience**: Paquete oficial que integra Polly en `IHttpClientFactory`.

### 4.9 Caching

**In-memory cache**: Cache en el proceso. Rápido. No compartido entre instancias. Útil para datos de solo lectura que cambian raramente.

**Distributed cache (Redis)**: Cache compartido entre instancias. Estándar para SaaS de múltiples réplicas.

**Output Caching**: Cachea la respuesta HTTP completa por ruta y parámetros. .NET 7+ incluye middleware nativo con políticas e invalidación por tags.

**Cache aside**: La app pregunta al cache; si no encuentra, consulta la BD y guarda en cache.

**Cache stampede**: Cuando el cache expira, muchos requests simultáneos van a la BD. Solución: `SemaphoreSlim` con doble check o locking distribuido.

**Cache key con TenantId**: En multi-tenant, toda key de cache debe incluir el TenantId. `customer:123` es bug; `tenant:acme:customer:123` es correcto.

### 4.10 Background services

**IHostedService / BackgroundService**: Servicios en segundo plano que corren con la vida de la aplicación. `ExecuteAsync` con `CancellationToken`.

**PeriodicTimer**: Timer que no acumula drift. Preferible a `Task.Delay` en bucles de ejecución periódica.

**Channel<T>**: Cola en memoria para comunicación productor-consumidor dentro del proceso. Alternativa ligera a un broker externo para volúmenes bajos.

**Hangfire**: Jobs persistentes con dashboard web. Soporta enqueueing, scheduling y recurrentes. Persiste en PostgreSQL.

**Tenant en jobs**: Regla crítica: cada job debe recibir el `TenantId` como parámetro explícito. Nunca asumir el contexto del thread actual.

### 4.11 Performance en .NET

**Span<T>**: Representa un segmento de memoria contigua sin allocar. Permite procesar strings, arrays y buffers sin copias. Solo en stack.

**Memory<T>**: Versión de Span<T> que puede almacenarse en heap. Útil en async.

**MemoryPool<T>**: Pool de buffers reutilizables. Evita allocations en hot paths.

**ObjectPool<T>**: Pool de objetos costosos de crear (`StringBuilder`, conexiones, etc.).

**BenchmarkDotNet**: Framework para micro-benchmarks en .NET. `[MemoryDiagnoser]` reporta allocations y GC collections junto con tiempo.

**StringBuilder**: Concatenar strings con `+` en bucles produce N-1 strings intermedios. `StringBuilder` es O(n). `StringWriter` es aún más eficiente para output pesado.

### 4.12 Testing

**xUnit**: Framework de testing dominante en .NET. `[Fact]` para tests individuales, `[Theory]` con `[InlineData]` / `[MemberData]` para tests parametrizados.

**Fluent Assertions**: API expresiva para assertions. `result.Should().BeEquivalentTo(expected)`.

**Unit Tests**: Prueban lógica de dominio aislada. Sin IO. Sin mocks de base de datos. Rápidos.

**Integration Tests**: Prueban la integración de capas reales. Usan base de datos real (no mock).

**Testcontainers**: Levanta contenedores Docker en el test. Permite PostgreSQL, Redis real en integration tests.

**TDD (Test-Driven Development)**: Red, Green, Refactor. Escribir el test primero fuerza a pensar en el API antes que en la implementación.

**Evaluación semántica**: En tests de LLMs no se verifica igualdad exacta sino criterios: contiene palabras clave, tiene longitud razonable, está en el idioma correcto.

**Snapshot testing**: Serializa el resultado y lo compara contra un archivo guardado. Útil para respuestas de API complejas. `Verify.Xunit`.

---

## Dominio 5 · Bases de datos relacionales

### 5.1 Conceptos fundamentales

**RDBMS**: Motor de bases de datos relacionales. PostgreSQL, SQL Server, MySQL.

**Tabla**: Estructura tabular con columnas (atributos) y filas (registros).

**Esquema**: Conjunto lógico de objetos (tablas, vistas, procedimientos). En multi-tenant "schema per tenant", cada tenant tiene su esquema.

**Llave primaria (PK)**: Columna que identifica unívocamente cada fila.

**Llave foránea (FK)**: Columna que referencia la PK de otra tabla. Mantiene integridad referencial.

**Índice**: Estructura que acelera búsquedas. Cada índice acelera lecturas pero penaliza escrituras.

**Vista**: Consulta guardada que se comporta como tabla.

**Trigger**: Código que se dispara automáticamente ante eventos. Útil para auditoría; peligroso si se usa para lógica de negocio.

### 5.2 Modelado y normalización

| Forma Normal | Qué garantiza |
|---|---|
| 1NF | Cada celda contiene un solo valor. |
| 2NF | Cumple 1NF y los atributos no clave dependen de toda la PK. |
| 3NF | Cumple 2NF y los atributos no clave dependen solo de la PK. |

**Desnormalización**: Introducir redundancia controlada por rendimiento. Se hace cuando se ha medido, no por costumbre.

**OLTP**: Cargas transaccionales: muchas escrituras pequeñas, baja latencia.

**OLAP**: Cargas analíticas: pocas consultas pesadas sobre grandes volúmenes.

### 5.3 Multi-tenant en bases de datos

**TenantId column**: En modelo pool, columna presente en cada tabla del dominio. Toda query la filtra, toda inserción la pone explícitamente.

**Row-Level Security (RLS)**: Feature de PostgreSQL que aplica filtros automáticamente a nivel de fila según el contexto de sesión. Defensa crítica en pool.

**Composite PK con TenantId**: Hacer `(TenantId, Id)` la llave primaria garantiza por diseño que no se mezclan entidades entre tenants.

**Índices con TenantId al inicio**: Todo índice debe tener TenantId como primera columna. Las queries siempre filtran por tenant; el índice debe servir ese patrón.

**Sharding por tenant**: Distribuir tenants entre múltiples bases físicas para escalar. Necesario solo a gran escala.

### 5.4 SQL avanzado

**CTE (Common Table Expression)**: Sub-consulta nombrada con `WITH`. Mejora legibilidad y permite recursión.

**Window Functions**: Funciones que calculan valores sobre una "ventana" de filas relacionadas sin colapsar el resultado.
- `ROW_NUMBER()`: Numeración única por partición.
- `RANK()` / `DENSE_RANK()`: Ranking con o sin gaps en empates.
- `LAG()` / `LEAD()`: Valor de la fila anterior/siguiente.
- `SUM() OVER (PARTITION BY ... ORDER BY ...)`: Totales acumulados.

**JOIN**: `INNER` (solo coincidencias), `LEFT` (todo de izquierda), `RIGHT`, `FULL`.

**DDL / DML / DCL / TCL**: Data Definition, Manipulation, Control, Transaction Language.

### 5.5 Transacciones y concurrencia

**Transacción**: Conjunto de operaciones que se completan todas o se deshacen todas.

**ACID**: Atomicidad, Consistencia, Aislamiento, Durabilidad.

**Niveles de aislamiento**: READ UNCOMMITTED, READ COMMITTED, REPEATABLE READ, SERIALIZABLE, SNAPSHOT.

**Deadlock**: Dos transacciones se bloquean mutuamente. El motor detecta y aborta la "víctima".

**Optimistic concurrency**: Asumir que los conflictos son raros. Validar al guardar con una columna `Version`. Si `rowsAffected == 0`, hubo conflicto.

**Pessimistic concurrency**: Bloquear el recurso al leerlo (`SELECT FOR UPDATE`). Más restrictivo, menor concurrencia. Se usa cuando el conflicto es probable.

### 5.6 Rendimiento y planes de ejecución

**EXPLAIN ANALYZE**: Muestra el plan real de ejecución con tiempos y costos. El principal instrumento de diagnóstico en PostgreSQL.

Patrones de alerta en un plan:
- `Seq Scan` en tabla grande: falta índice.
- `Rows Removed by Filter: N` alto: el índice no aplica o el filtro no es sargable.
- `Nested Loop` con miles de iteraciones: N+1 problem.
- `Sort`: considerar índice sobre las columnas del ORDER BY.

**Sargability**: Una condición es "sargable" si permite usar índice. `WHERE YEAR(fecha) = 2025` NO es sargable. `WHERE fecha >= '2025-01-01'` sí lo es.

**N+1 problem**: Una query principal y luego una por cada resultado. Solución: `Include`/`Join` explícitos.

### 5.7 Particionamiento en PostgreSQL

**PARTITION BY RANGE**: Divide una tabla en sub-tablas por rango de valores (fecha, ID). Mejora rendimiento de queries que filtran por esa columna.

**PARTITION BY LIST**: Divide por valores discretos. Útil para particionar por `TenantId` cuando los tenants son estables.

### 5.8 PostgreSQL para SaaS

- Tipos JSON nativos (`JSONB`) con índices GIN. Híbrido relacional/documental.
- Extensiones: PostGIS (geoespacial), `pg_trgm` (búsqueda fuzzy), pgcrypto (cifrado).
- Row-Level Security nativo y maduro.
- Replicación lógica para read replicas y migraciones cero downtime.
- MVCC: lecturas no bloquean escrituras.

---

## Dominio 6 · Frontend con React, Vite y Tailwind

### 6.1 React: conceptos centrales

**Componente**: Función que recibe props y devuelve JSX. Unidad de composición.

**JSX**: Sintaxis que mezcla XML con JavaScript. Se compila a `React.createElement`. No es HTML.

**Props**: Datos que un componente recibe del padre. Inmutables dentro del componente.

**State**: Datos internos del componente que sí cambian. Cuando cambia, el componente se re-renderiza.

**Hook**: Función que permite usar características de React en componentes funcionales.

| Hook | Para qué sirve |
|---|---|
| `useState` | State local |
| `useEffect` | Efectos colaterales: HTTP, suscripciones, timers |
| `useMemo` | Memoiza un valor calculado |
| `useCallback` | Memoiza una función |
| `useContext` | Consume un contexto compartido |
| `useRef` | Referencia mutable que no causa re-render |
| `useReducer` | State complejo con lógica de actualización |

**Custom Hook**: Función propia que combina otros hooks. Extrae y reutiliza lógica con estado.

**Virtual DOM**: Representación en memoria del árbol de componentes. React calcula el diff y aplica solo los cambios al DOM real.

### 6.2 Patrones modernos en React

**Composición**: Construir componentes complejos a partir de simples. Prefiere composición sobre configuraciones gigantes.

**Lifting state up**: Mover el estado al ancestro común más cercano cuando varios componentes lo necesitan compartir.

**Compound components**: Familia de componentes que trabajan juntos: `<Tabs>`, `<Tabs.List>`, `<Tabs.Tab>`.

**Suspense**: Muestra un fallback mientras un hijo asíncrono carga.

**Error Boundary**: Captura errores de sus hijos y muestra UI de respaldo.

### 6.3 State management

| Solución | Cuándo usar |
|---|---|
| `useState` local | El 80% de los casos |
| Context | Auth, theme, tenant. No es solución general. |
| Zustand / Jotai | Estado global ligero y moderno |
| TanStack Query | State de servidor: caching, sincronización, invalidación |

### 6.4 Vite

**Dev server**: Sirve módulos individualmente con HMR instantáneo.

**HMR**: Reemplaza módulos sin recargar la página.

**Build**: Para producción usa Rollup: tree-shaking, code splitting, optimización.

**Alias**: `'@components/Button'` resuelve a `'src/components/Button'`. Evita rutas relativas frágiles.

**Proxy**: El dev server puede proxiar peticiones a APIs reales evitando CORS en desarrollo.

**Variables de entorno**: Solo las variables con prefijo `VITE_` se exponen al cliente. Nunca poner secretos con ese prefijo.

### 6.5 Tailwind CSS

**Utility class**: Clase que hace una sola cosa: `p-4`, `text-lg`, `bg-slate-900`.

**Responsive prefixes**: `md:flex` aplica `flex` a partir del breakpoint mediano. Mobile-first por defecto.

**Variants**: `hover:`, `focus:`, `disabled:`, `dark:` aplican utilidades en estados específicos.

**Design system**: Centralizar combinaciones de utilidades en tokens semánticos. `ui.controls.primaryButton`.

**Tailwind 4**: Engine completamente reescrito. Configuración por CSS, sin `tailwind.config.js` obligatorio.

### 6.6 White-labeling y theming

**Theme tokens**: Colores, fuentes, radios definidos como variables CSS. Cambiarlos por tenant cambia toda la UI sin tocar componentes.

**CSS variables**: Variables nativas (`--color-primary`). Cambian en runtime sin recompilar.

**Custom domain**: `tenant.cliente.com` en lugar de `tenant.miproducto.com`. Implica DNS y SSL automático.

---

## Dominio 7 · Control de versiones con Git

### 7.1 Conceptos centrales

**Repositorio (repo)**: Carpeta versionada con todo el histórico.

**Commit**: Snapshot inmutable del proyecto con autor, fecha, mensaje y hash SHA único.

**Branch**: Línea de desarrollo paralela. Es solo un puntero a un commit; crear una rama es gratis.

**HEAD**: Puntero al commit actual.

**Working directory**: Los archivos tal como están en disco.

**Staging area (index)**: Área intermedia donde preparas qué va al próximo commit.

**Remote**: Repositorio remoto. `origin` es el alias convencional del remoto principal.

**Tag**: Puntero inmutable a un commit. Marca versiones. En releases siempre usa tags anotados.

### 7.2 Comandos esenciales

| Comando | Qué hace |
|---|---|
| `git clone <url>` | Descarga un repo remoto a local |
| `git status` | Muestra archivos modificados, en stage, sin trackear |
| `git add <ruta>` | Marca archivos para el próximo commit |
| `git commit -m "..."` | Crea un commit con los cambios staged |
| `git push origin <rama>` | Envía commits locales al remoto |
| `git pull --rebase` | Trae cambios remotos y reorganiza los locales encima |
| `git switch <rama>` | Cambia a otra rama |
| `git switch -c <rama>` | Crea y cambia a una nueva rama |
| `git merge <rama>` | Integra otra rama a la actual |
| `git rebase <rama>` | Reaplica los commits actuales sobre otra rama |
| `git stash` | Guarda cambios sin commitear para retomarlos después |
| `git tag -a v1.2.3 -m "..."` | Crea un tag anotado |
| `git reset --hard <hash>` | Reinicia la rama a un commit (destructivo) |
| `git revert <hash>` | Crea un commit que deshace otro (no destructivo) |
| `git cherry-pick <hash>` | Aplica un commit específico de otra rama |
| `git bisect start` | Inicia búsqueda binaria del commit que introdujo un bug |

### 7.3 git bisect

Herramienta para encontrar el commit exacto que introdujo un bug mediante búsqueda binaria. Divide el histórico a la mitad en cada paso hasta aislar el commit culpable.

Puede automatizarse con un script de prueba: Git marca automáticamente cada commit como bueno o malo según el exit code del script.

### 7.4 Estrategias de branching

**GitFlow**: `main` (producción), `develop` (integración), `feature/*`, `release/*`, `hotfix/*`. Ideal para releases planeados.

**GitHub Flow**: `main` siempre desplegable, ramas feature, pull requests, deploy continuo.

**Trunk-Based Development**: Ramas muy cortas (horas o un día), integración constante a main, feature flags para código no terminado. Modelo de equipos de alto rendimiento.

### 7.5 Conventional Commits

```
<type>[scope opcional]: <descripción corta>
[cuerpo opcional]
[footer opcional — BREAKING CHANGE, Refs]
```

| Type | Cuándo |
|---|---|
| `feat` | Nueva funcionalidad visible al usuario |
| `fix` | Corrección de un bug |
| `refactor` | Cambio en código sin cambio en comportamiento |
| `docs` | Solo documentación |
| `chore` | Mantenimiento, dependencias, configuración |
| `test` | Agregar o ajustar pruebas |
| `perf` | Mejora de rendimiento |
| `ci` | Cambios a pipelines y configuración de CI |

### 7.6 Semantic Versioning (SemVer)

`MAJOR.MINOR.PATCH`:
- **MAJOR**: Cambios incompatibles (breaking changes).
- **MINOR**: Nueva funcionalidad compatible hacia atrás.
- **PATCH**: Corrección de bug compatible hacia atrás.

Versiones pre-release: `1.2.0-alpha.1`, `1.2.0-rc.2`.

### 7.7 Code review

- Revisa código pequeño. Un PR de 1500 líneas no se revisa, se aprueba con desgana.
- Comenta el porqué: "Esto puede fallar cuando X" enseña; "cambia esto" frustra.
- Distingue must-fix, should-fix y nit.
- Como autor, responde a cada comentario.

---

## Dominio 8 · Contenedores con Docker

### 8.1 Conceptos centrales

**Imagen**: Plantilla inmutable. Contiene sistema base, dependencias, código y configuración.

**Contenedor**: Instancia en ejecución de una imagen. Tiene su propia red, sistema de archivos y procesos.

**Dockerfile**: Receta para construir una imagen. Cada instrucción crea una capa cacheable.

**Capa (layer)**: Cada instrucción del Dockerfile produce una capa inmutable y reutilizable.

**Volumen**: Mecanismo para persistir datos fuera del ciclo de vida del contenedor.

**Red (network)**: Mecanismo de comunicación entre contenedores. Una red bridge permite que se vean por nombre.

**Registry**: Almacén de imágenes. Docker Hub (público), Harbor, ACR, AWS ECR (privados).

### 8.2 Dockerfile

```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime (sin SDK ni código fuente)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Saas.Api.dll"]
```

**Multi-stage build**: Compila en una etapa con SDK completo, copia solo el binario a la etapa runtime. Imagen final pequeña.

**Instrucciones clave:**

| Instrucción | Para qué sirve |
|---|---|
| `FROM` | Imagen base |
| `WORKDIR` | Directorio de trabajo |
| `COPY` | Copia archivos del host |
| `RUN` | Ejecuta comando durante el build |
| `ENV` | Variable de entorno |
| `EXPOSE` | Documenta el puerto expuesto |
| `ENTRYPOINT` | Comando principal del contenedor |
| `HEALTHCHECK` | Verifica si el contenedor está sano |

### 8.3 Seguridad en Dockerfiles

**Non-root user**: Crear un usuario sin privilegios y ejecutar el proceso con él. Un contenedor root que es comprometido tiene acceso root al host si el aislamiento falla.

**No secrets en el Dockerfile**: Las capas son inspeccionables. Los secretos se pasan por variables de entorno o secret managers en runtime.

**Distroless images**: Imágenes sin shell ni herramientas del sistema. Superficie de ataque mínima. `gcr.io/distroless/dotnet`.

**BuildKit — mount secrets**: `--mount=type=secret` permite pasar tokens y credenciales durante el build sin que queden en ninguna capa.

```dockerfile
RUN --mount=type=secret,id=nuget_token \
    dotnet nuget add source --username ... \
    --password "$(cat /run/secrets/nuget_token)" ...
```

### 8.4 Docker Compose

Orquesta múltiples contenedores con un solo archivo YAML. Ideal para desarrollo local y despliegues simples.

**compose.test.yaml**: Variante de Compose para tests de integración en CI. Levanta solo las dependencias (PostgreSQL, Redis) sin el API, que corre directamente en el runner de CI.

**Depends on**: Controla el orden de inicio de servicios. `condition: service_healthy` espera a que el health check del servicio sea verde.

### 8.5 Registries y versionado

**Image digest**: Hash SHA256 inmutable. Más confiable que el tag para producción.

**Vulnerability scanning**: Harbor, ACR y ECR integran escaneo contra CVEs.

**Reglas de operación seria:**
- Una imagen por release. El tag de imagen coincide con el tag de Git: `v1.3.0 → :v1.3.0`.
- Inmutabilidad. Nunca sobreescribir un tag publicado.
- `latest` es solo para desarrollo.
- Promoción entre ambientes: la misma imagen probada en staging va a producción.

### 8.6 Kubernetes (cuando hace falta)

**Kubernetes (K8s)**: Orquestador de contenedores. Programa contenedores en un cluster, los reinicia si fallan, escala automáticamente.

**Pod**: Unidad mínima de despliegue.

**Deployment**: Especifica cuántas réplicas queremos.

**Service**: IP y nombre estable para un conjunto de pods.

**Ingress**: Reglas para exponer servicios al exterior.

**Helm**: Gestor de paquetes para K8s.

Para muchos SaaS, Docker Compose en uno o dos servidores es suficiente durante años.

---

## Dominio 9 · CI/CD y automatización

### 9.1 Conceptos

**Continuous Integration (CI)**: Cada cambio se integra al tronco principal con frecuencia. Cada integración dispara build y pruebas automáticas.

**Continuous Delivery (CD)**: Cada commit que pasa CI queda en un estado desplegable.

**Continuous Deployment**: Cada cambio que pasa pruebas se despliega automáticamente.

**Pipeline**: Secuencia automatizada de etapas: build, test, paquete, deploy. Definida como código (YAML).

**Stage**: Bloque lógico del pipeline.

**Job**: Unidad de ejecución dentro de un stage. Puede correr en paralelo.

**Step / Task**: Acción dentro de un job: restore, build, docker push.

**Trigger**: Evento que dispara el pipeline: push, tag, schedule, manual.

**Artifact**: Producto del pipeline: binarios, imágenes Docker. Se publican y consumen entre jobs.

### 9.2 Azure DevOps

**Pipeline YAML**: Definición versionada del pipeline. Vive en el repo, evoluciona con el código.

**Variable groups**: Grupos de variables compartidas entre pipelines. Donde viven secrets en Library.

**Service connection**: Credencial guardada para conectar con servicios externos.

**Environment**: Representación de un ambiente (staging, production) con gates de aprobación.

**Approval**: Paso manual antes de continuar. Para producción.

### 9.3 GitHub Actions

**Workflow**: Archivo YAML en `.github/workflows/`. Se dispara por eventos del repositorio.

**Reusable workflow**: Workflow que puede ser invocado por otros workflows con `workflow_call`. Permite compartir lógica de CI/CD entre repositorios del mismo equipo.

**OIDC (OpenID Connect)**: Permite que GitHub Actions obtenga credenciales de AWS/Azure temporales sin guardar access keys como secrets. El workflow solicita un token OIDC de GitHub, AWS lo valida contra una política de confianza y devuelve credenciales temporales. Elimina la necesidad de credenciales de larga vida en el repositorio.

### 9.4 Terraform

**Infrastructure as Code (IaC)**: La infraestructura se define en archivos de configuración versionables y reproducibles.

**Provider**: Plugin que conecta Terraform con un cloud (aws, azurerm, google).

**Resource**: Un componente de infraestructura (instancia EC2, base de datos, etc.).

**Data Source**: Lee información de recursos ya existentes sin crearlos.

**State**: Archivo (`terraform.tfstate`) que representa el estado actual de la infraestructura. Se guarda en S3 con DynamoDB para locking en equipos.

**Plan**: `terraform plan` muestra qué va a cambiar sin aplicar. Siempre revisar antes de `apply`.

**Variable `sensitive = true`**: No se muestra en logs ni en output. Obligatorio para passwords y tokens.

**Deployment circuit breaker en ECS**: Rollback automático si el deploy falla el health check.

**deletion_protection en RDS**: Protección contra `terraform destroy` accidental.

### 9.5 Agentes self-hosted

Los agentes self-hosted acceden a la red interna, tienen herramientas precargadas y se organizan en pools por propósito o ambiente.

### 9.6 Estrategias de deploy

| Estrategia | Cómo funciona |
|---|---|
| Recreate | Apaga la versión vieja, sube la nueva. Hay downtime. |
| Rolling update | Reemplaza instancias gradualmente. Sin downtime pero versiones conviven. |
| Blue-Green | Dos ambientes idénticos; conmuta el tráfico. Rollback instantáneo. |
| Canary | Nueva versión a un porcentaje pequeño de tráfico. Si las métricas son sanas, se incrementa. |
| Feature flag deploy | El código se despliega pero la feature está apagada. Desacopla deploy de release. |

### 9.7 Buenas prácticas en pipelines

- Pipeline as code. Vive en el repo, se revisa en PR.
- Falla rápido. Smoke tests primero, integración después.
- Cache agresivo. Restore de paquetes, layers de Docker, dependencias npm.
- Idempotencia. Correr el mismo pipeline dos veces produce el mismo resultado.
- Secrets nunca en logs.
- Migraciones de BD seguras. Probarlas en staging con dato real es obligatorio.

---

## Dominio 10 · Cloud, Linux y operación SaaS

### 10.1 Linux esencial

| Comando | Qué hace |
|---|---|
| `ls -lah` | Lista archivos con permisos, tamaño y ocultos |
| `grep / rg` | Buscar texto en archivos |
| `find . -name patrón` | Buscar archivos por nombre |
| `chmod / chown` | Cambiar permisos / propietarios |
| `systemctl status/start/stop` | Gestionar servicios systemd |
| `journalctl -u servicio -f` | Ver logs de servicios en tiempo real |
| `ss -tulnp` | Ver puertos en escucha |
| `scp / rsync` | Copiar archivos entre máquinas |

**systemd**: Sistema de inicio y gestión de servicios en Linux moderno.

**Cron**: Programador de tareas. Útil para backups y mantenimiento.

**SSH key**: Par de llaves (pública/privada) para autenticarse sin password.

**Bash scripting**: `set -euo pipefail` en todo script de producción: falla ante error (`-e`), variable no definida (`-u`) o error en pipe (`-o pipefail`).

### 10.2 Nginx

**Reverse proxy**: Recibe peticiones del exterior y las redirige a servidores internos. Centraliza TLS, balanceo, headers, caching.

**Server block**: Configuración para un dominio o sitio específico.

**TLS / HTTPS**: Nginx termina TLS; las apps internas hablan HTTP plano.

**Let's Encrypt + certbot**: Certificados TLS gratuitos automatizables.

**Wildcard certificate**: Cubre todos los subdominios (`*.miproducto.com`) sin necesidad de uno por cada tenant.

**SNI (Server Name Indication)**: Permite servir muchos dominios desde la misma IP.

### 10.3 Cloud computing

| Modelo | Qué administra el proveedor |
|---|---|
| IaaS | Hardware, virtualización, red |
| PaaS | Hardware, OS, runtime, escalado |
| SaaS | Todo |
| FaaS / Serverless | Todo menos el código de la función |
| CaaS / Containers | Hardware, OS, orquestación |

### 10.4 AWS

| Servicio | Para qué |
|---|---|
| EC2 | Máquinas virtuales |
| S3 | Almacenamiento de objetos |
| RDS | Bases de datos administradas |
| Aurora | PostgreSQL/MySQL distribuido de AWS |
| VPC | Red virtual aislada |
| IAM | Identidades y permisos granulares |
| Lambda | FaaS |
| ECS / EKS | Contenedores |
| ALB | Application Load Balancer |
| CloudFront | CDN |
| CloudWatch | Monitoreo y logs |
| Route 53 | DNS |
| Secrets Manager | Secretos con rotación automática |
| SES | Email transaccional |
| SQS / SNS | Cola / pub-sub |

### 10.5 Azure equivalencias

| AWS | Azure |
|---|---|
| EC2 | Virtual Machines |
| S3 | Blob Storage |
| RDS / Aurora | Azure SQL / DB for PostgreSQL |
| VPC | VNet |
| IAM | Azure AD + RBAC |
| Lambda | Azure Functions |
| ECS / EKS | Container Instances / AKS |
| ALB | Application Gateway / Front Door |
| CloudWatch | Azure Monitor + Log Analytics |
| Secrets Manager | Key Vault |
| SQS / SNS | Service Bus / Event Grid |

### 10.6 Observabilidad

**Tres pilares: logs, métricas, trazas.**

**Logs estructurados**: Eventos con campos consultables. Permiten filtrar "todos los errores del tenant X en la última hora".

**Métricas técnicas**: CPU, memoria, latencia, throughput, tasas de error.

**Métricas de negocio**: Signups, MRR, conversiones, churn.

**APM**: Datadog, New Relic, Application Insights. Combinan los tres pilares.

**SLI (Service Level Indicator)**: Métrica real que mide calidad: porcentaje de peticiones bajo 200 ms en los últimos 30 días.

**SLO (Service Level Objective)**: Objetivo medible de calidad: "99.9% de peticiones bajo 200 ms". Más estricto que el SLA.

**SLA (Service Level Agreement)**: Compromiso contractual con el cliente. Penalización económica si se incumple.

**Error Budget**: Margen de incumplimiento tolerado por un SLO. Si el budget se gasta, se prioriza estabilidad sobre nuevas features.

**OpenTelemetry**: Estándar abierto para emitir trazas, métricas y logs. Vendor-neutral.

**Serilog**: Logging estructurado para .NET. Emite eventos con propiedades, no cadenas.

**Prometheus**: Sistema de métricas con scraping. Combinado con Grafana, es el estándar abierto.

**Health Checks**: Endpoints que reportan si la app y sus dependencias están sanas.

**Tenant en logs**: Cada log debe incluir `TenantId` como propiedad estructurada.

### 10.7 Secretos en producción

**Reglas absolutas:**
- Cero secretos en el repositorio.
- Cero secretos en el Dockerfile.
- Cero secretos en logs.
- Rotación periódica. Cada secreto tiene fecha de caducidad.
- Acceso mínimo. Cada servicio usa un secreto distinto con permisos mínimos.

**detect-secrets**: Herramienta de Yelp que escanea archivos y commits buscando patrones de secretos (tokens, passwords, API keys). Se instala como pre-commit hook para bloquear commits con secretos.

**AWS Macie**: Servicio de AWS que escanea buckets S3 buscando datos sensibles con ML.

**GitHub Secret Scanning**: Escaneo automático de secretos en repositorios de GitHub.

### 10.8 Seguridad web (OWASP Top 10)

| # | Vulnerabilidad | Prevención principal |
|---|---|---|
| A01 | Broken Access Control | Verificar ownership en cada handler, policies granulares |
| A02 | Cryptographic Failures | BCrypt para passwords, HTTPS obligatorio, no loguear passwords |
| A03 | Injection | Parámetros en SQL, `ArgumentList` en process start |
| A04 | Insecure Design | Rate limiting en login, validación en todas las capas |
| A05 | Security Misconfiguration | Security headers, `AddServerHeader = false`, no exponer stack trace |
| A06 | Vulnerable Components | Dependabot, `dotnet outdated`, audit de paquetes |
| A07 | Auth Failures | HttpOnly cookies, rotación de refresh tokens, token blacklist |
| A08 | Software Integrity | Validación de dependencias, supply chain |
| A09 | Logging Failures | Loguear todos los accesos, incluyendo los exitosos |
| A10 | SSRF | Validar y allowlist URLs en requests del servidor |

**Security headers obligatorios:**
- `X-Frame-Options: DENY`: Previene clickjacking.
- `X-Content-Type-Options: nosniff`: Previene MIME sniffing.
- `Content-Security-Policy`: Controla qué recursos puede cargar el browser.
- `Strict-Transport-Security`: Solo HTTPS.
- `Permissions-Policy`: Deshabilita APIs del browser no necesarias.

**CORS restrictivo**: Solo origenes conocidos (`WithOrigins`). Nunca `AllowAnyOrigin()` con `AllowCredentials()`.

**Rate limiting**: Limitar peticiones en endpoints de auth por IP. `.NET 7+` incluye middleware nativo.

### 10.9 Zero Trust

**Modelo perimetral (viejo)**: "Si estás dentro del firewall, eres de confianza". Si un atacante entra a la red, tiene acceso a todo.

**Zero Trust**: "Nunca confiar, siempre verificar". Cada request se autentica y autoriza independientemente del origen.

**Los cinco pilares:**
1. Verificar identidad: autenticación fuerte (MFA) para usuarios y servicios.
2. Verificar el dispositivo: solo dispositivos conocidos y sanos acceden.
3. Limitar acceso: least privilege: acceso mínimo necesario.
4. Inspeccionar el tráfico: cifrar y monitorear incluso tráfico interno.
5. Asumir breach: diseñar como si el atacante ya estuviera dentro.

**mTLS (Mutual TLS)**: Ambos lados (cliente y servidor) validan el certificado del otro. Estándar para autenticación entre microservicios.

**Least Privilege**: Policies granulares por operación (`Orders:Read`, `Orders:Write`, `Orders:Delete`) en lugar de roles amplios (`Admin`).

**Asumir Breach**: Validar datos en cada capa, aunque el request venga de un microservicio interno. Una capa comprometida no debe dar acceso ilimitado.

### 10.10 Costos en cloud

- **Tag everything.** Sin tags no hay reportes útiles.
- **Reserved instances / Savings plans.** 30-70% de descuento a cambio de compromiso de 1-3 años.
- **Auto-scaling.** Pagar por lo que usas, no por el pico.
- **Lifecycle policies.** Mover datos viejos a tiers más baratos (S3 Glacier).
- **Alertas de presupuesto.** Si el gasto se proyecta 20% arriba, alguien debe saberlo el día 5, no el día 30.

---

## Dominio 11 · Vibe Coding y agentes de IA

### 11.1 Conceptos

**LLM (Large Language Model)**: Modelo de lenguaje grande: GPT, Claude, Gemini, Llama. Generan texto y código a partir de un prompt.

**Prompt**: Instrucción que le das al modelo. La calidad del prompt es el factor más determinante de la calidad de la respuesta.

**Context window**: Cantidad máxima de tokens que el modelo puede procesar en una sola interacción.

**Token**: Unidad de procesamiento del modelo. ~0.75 palabras en inglés. Las APIs se facturan en tokens.

**Vibe coding**: Programar colaborativamente con asistentes de IA: el desarrollador define intención y restricciones, la IA propone implementación. El humano valida, ajusta y decide.

**Agente**: Sistema que combina LLM + herramientas + ejecución autónoma. Recibe un objetivo y ejecuta acciones (leer archivos, ejecutar comandos, llamar APIs) hasta cumplirlo.

**CLAUDE.md / AGENTS.md**: Archivo de contexto para agentes en un proyecto. Documenta convenciones, stack, reglas y patrones.

**ReAct (Reason + Act)**: Patrón de razonamiento de agentes: Thought, Action, Observation; el ciclo se repite hasta completar el objetivo. El agente alterna entre razonamiento interno y acciones en el entorno.

### 11.2 Configurar un proyecto para vibe coding

Un buen archivo de contexto incluye:
- Stack y versiones exactas.
- Estructura de carpetas y convenciones de nombres.
- Patrones obligatorios con ejemplos de código real.
- Reglas no negociables: TenantId en queries, manejo de errores, herramientas.
- Comandos típicos: build, tests, deploy local.

### 11.3 Prompts efectivos

- **Sé específico.** "Hazme una API" es ruido; "agrega endpoint POST /api/customers que reciba {name, email, plan}, valide con FluentValidation y devuelva el customer creado con su id" es señal.
- **Da contexto que importa.** Si usas EF Core, dilo. Si usas Mediator, dilo.
- **Restricciones explícitas.** "Recuerda agregar el filtro de TenantId. Usa los repositorios existentes. No instales librerías nuevas."
- **Define el formato.** ¿Código solo? ¿Con explicación? ¿Como diff?
- **Itera.** La primera respuesta rara vez es perfecta.

**Delegar vs no delegar:**

| Delegar | No delegar |
|---|---|
| Código boilerplate con patrones conocidos | Decisiones arquitectónicas finales |
| Migrar/refactorizar código con convenciones claras | Selección de tecnología sin contexto |
| Generar tests desde implementación existente | Manejo directo de credenciales y secretos |
| Documentar código ya escrito | Deploys a producción sin gate de aprobación |

### 11.4 LLMOps

**LLMOps**: Conjunto de prácticas para diseñar, deployar, monitorear y mejorar aplicaciones que usan LLMs en producción.

```
DevOps:  código determinista → tests unitarios → CI/CD tradicional
MLOps:   modelo estadístico → métricas de modelo (accuracy, F1) → reentrenamiento
LLMOps:  LLM no determinista → evaluación semántica → fine-tuning / prompt engineering
```

**Abstracción del proveedor**: `IAICompletionService` como interfaz única. La implementación concreta (`AnthropicCompletionService`, `OpenAiCompletionService`) puede reemplazarse sin cambiar la capa de aplicación.

**Streaming de tokens**: `IAsyncEnumerable<string>` con SSE (Server-Sent Events) para respuestas incrementales en tiempo real.

**Versionado de prompts**: Los prompts son código: se versionan, se testean y se despliegan con el mismo rigor. Un `PromptVersion` enum permite A/B testing entre versiones.

**Evaluación semántica**: No verificar igualdad exacta sino criterios: contiene palabras clave, tiene longitud razonable, está en el idioma correcto, no contiene información sensible.

**Guardrails**: Validar que la respuesta del LLM cumple restricciones antes de devolverla al usuario: bloquear patrones de información sensible, verificar idioma, limitar longitud.

**Observabilidad de LLMs**: Loguear por cada llamada: modelo usado, tokens estimados, longitud de respuesta, latencia. Decorator `ObservableAIService` alrededor del servicio base.

**Control de costos**: Middleware que limita tokens por usuario/tenant por día. Si se supera el límite, devolver `429 Too Many Requests`.

**Cuándo construir vs usar servicio gestionado:**

| Construir propio | Usar servicio gestionado |
|---|---|
| Datos sensibles que no pueden salir de la infraestructura | Apps sin restricciones de privacidad |
| Control total sobre el modelo y el fine-tuning | Prototipo o MVP |
| Volumen muy alto donde el costo de API es prohibitivo | Equipo sin expertise en infraestructura de LLMs |

### 11.5 Lo que la IA no debe hacer (todavía)

- Decisiones arquitectónicas finales sin revisión humana.
- Manejo directo de credenciales y secretos.
- Despliegues a producción sin gate de aprobación.
- Migraciones de datos irreversibles.
- Operaciones que afecten a múltiples tenants sin verificación humana.

---

## Cierre · Mentalidad de ingeniero, no de programador

La diferencia entre escribir código y construir productos no está en saber sintaxis. Está en cómo se toman decisiones bajo incertidumbre, cómo se comunica con el equipo, y cómo se piensa el sistema más allá del módulo en el que se está trabajando.

Construir SaaS amplifica esto: cada decisión de diseño impacta a múltiples clientes simultáneamente.

### Toma de decisiones técnicas

- Empieza por el problema, no por la solución.
- Decide al último momento responsable.
- Toda decisión tiene un costo: librería, microservicio, capa nueva.
- Documenta el porqué. **ADR (Architecture Decision Record):** contexto, opciones consideradas, decisión tomada, consecuencias.
- Piensa en multi-tenant primero. Cualquier feature debe diseñarse considerando 10, 1000, 100,000 tenants.

### Carrera de largo plazo

- Aprender un lenguaje nuevo es fácil. Construir un mapa mental de sistemas distribuidos, arquitectura, persistencia, redes y operación es una década de trabajo.
- La industria cambia de moda cada año. Los fundamentos cambian cada veinte.
- Especialízate en algo, mantente alfabetizado en mucho.
- Lee código de otros, especialmente mejor que el tuyo.
- Construye un producto propio en algún momento. Vivir el ciclo completo (idea, build, ship, soporte, churn) enseña lo que ningún empleo puede.

---

*Rogelio Arriaga Gonzalez*
