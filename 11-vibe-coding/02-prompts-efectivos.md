# 02 · Prompts efectivos para desarrollo

> Fuente: *Unlocking the Secrets of Prompt Engineering* (Packt) — Ch.1 Components of an LLM prompt, Ch.7 AI Pair Programmers

## Problema que resuelve

Un prompt vago genera código genérico que no encaja con el proyecto. Un prompt bien estructurado produce código listo para integrar, con las convenciones correctas, en el primer intento.

## Componentes de un prompt efectivo

Del libro (Ch.1, p.37-40), los componentes son:

| Componente | Descripción | Ejemplo |
|-----------|-------------|---------|
| Rol / Contexto | quién eres y en qué proyecto estás | "Eres un desarrollador .NET trabajando en Clean Architecture" |
| Tarea | qué se quiere hacer | "Crea un handler MediatR que..." |
| Restricciones | qué no se puede hacer | "sin usar EF Core para la query, usar Dapper" |
| Formato de salida | cómo debe verse el resultado | "solo el código, sin explicaciones" |
| Ejemplos | código existente de referencia | pegar el handler similar del proyecto |

## Prompts por tipo de tarea

### Crear código nuevo desde cero

Incluir: arquitectura, naming, patrón a seguir, ejemplo existente.

```
Contexto:
- Proyecto .NET 10 Clean Architecture
- Capa Application: UseCases con MediatR
- Todos los handlers retornan Result<T> (ver el patrón en 04-backend/01-result-pattern.md)
- Queries de lectura usan Dapper, nunca EF Core

Tarea:
Crea el UseCase completo para aprobar una orden de compra:
- Handler: ApproveOrderHandler
- Request: ApproveOrderRequest (OrderId: Guid, Comments: string)
- Response: ApproveOrderResponse (OrderId, ApprovedAt)
- El handler verifica que la orden exista y esté en estado Pending
- Si no existe: Result.Failure("Order not found")
- Si no está en Pending: Result.Failure("Order cannot be approved in current state")

Sigue exactamente el mismo patrón que GetExampleUserHandler.
```

### Corregir un bug

Incluir: síntoma exacto, stack trace si hay, fragmento de código relevante.

```
Bug: al llamar POST /api/v1/orders/{id}/approve con un ID válido
devuelve 404 en lugar de 200.

Stack trace:
  at OrderController.Approve(Guid id)
  at ...

Código del controller:
[código pegado]

El handler existe y los tests unitarios pasan.
Busca el problema en el routing o en el registro de dependencias.
```

### Explicar código existente

```
Explica qué hace este pipeline behavior de MediatR y
por qué captura las excepciones en lugar de dejarlas propagar:

[código pegado]

Enfócate en las implicaciones de diseño, no en describir línea por línea.
```

### Refactorizar

```
Refactoriza este repositorio para eliminar la duplicación.
Los métodos GetByIdAsync y GetByEmailAsync tienen el mismo
bloque de manejo de errores.

Restricciones:
- Mantener la misma firma pública de los métodos
- No introducir clases base — usar un método privado local
- No cambiar los tests

[código pegado]
```

### Generar tests

```
Genera tests unitarios xUnit para ApproveOrderHandler.

Casos a cubrir:
1. Orden no existe → Result.Failure con mensaje "Order not found"
2. Orden en estado diferente a Pending → Result.Failure
3. Orden en Pending → Result.Success con ApprovedAt seteado

Usar Moq para IOrderRepository.
Seguir el patrón de Arrange/Act/Assert sin comentarios.
No usar AutoFixture.
```

## Anti-patrones de prompts

| Anti-patrón | Problema | Corrección |
|------------|---------|-----------|
| "Crea un CRUD" | demasiado vago, ignora el stack real | especificar entidad, campos, capa, patrón |
| "Arregla el bug" sin contexto | el AI adivina | pegar stack trace + código + síntoma exacto |
| "¿Cómo hago X en .NET?" | respuesta genérica | "en este proyecto con este patrón, ¿cómo hago X?" |
| Pedir todo en un prompt | respuesta larga e inconsistente | dividir en tareas pequeñas (handler, tests, controller por separado) |
| No dar ejemplo | código que no sigue las convenciones | pegar un archivo existente como referencia |

## Técnicas avanzadas

### Chain of thought para decisiones de diseño

```
Tengo que implementar notificaciones en tiempo real.
Las opciones son: SignalR, polling cada 5 segundos, Server-Sent Events.

Antes de proponer código, analiza los trade-offs de cada opción
considerando que:
- El backend es .NET + ECS Fargate (stateless, múltiples instancias)
- El frontend es React
- Los clientes son navegadores web y posiblemente mobile después

Luego recomienda una y explica por qué.
```

### Few-shot: dar ejemplos del patrón esperado

```
Quiero que crees un nuevo UseCase siguiendo este patrón exacto:

Ejemplo existente (GetExampleUserHandler):
[código del handler]
[código del request]
[código del response]

Ahora crea el mismo patrón para UpdateExampleUser:
- Request: fullName, email, department (todos opcionales)
- Valida que el usuario exista antes de actualizar
```

### Role prompting para revisión

```
Actúa como un arquitecto de software senior que revisa este código
buscando violaciones de Clean Architecture:
- Dependencias que van en la dirección equivocada
- Lógica de negocio en la capa de Infrastructure
- Handlers que hacen más de una cosa

[código pegado]

Da la retroalimentación como lista de problemas concretos,
no como elogios generales.
```

## Prompts en Claude Code (modo interactivo)

Claude Code tiene acceso a los archivos del proyecto. Aprovechar esto:

```
# en lugar de pegar código, referenciar el archivo
Lee src/Application/UseCases/Quality/LineQcApprovals y 
luego crea un UseCase similar para el módulo de Production.
```

```
# para tareas que requieren contexto de múltiples archivos
Antes de escribir el test, lee:
- el handler en Application/UseCases/Orders/ApproveOrder/
- el contrato del repositorio en Domain/Repositories/IOrderRepository.cs
- un test existente similar en Application.Tests/UseCases/

Luego crea los tests para ApproveOrderHandler.
```

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| prompts estructurados con contexto explícito para código de producción | prompts conversacionales para código que va directo a main |
| ejemplos del mismo proyecto como referencia | ejemplos genéricos de Stack Overflow |
| dividir tareas grandes en pasos | pedir handler + tests + controller + migration en un solo prompt |
| pedir solo el código cuando ya se entiende el diseño | pedir código antes de validar el diseño con el AI |


---

*Rogelio Arriaga Gonzalez*
