# Homework S06  -  DDD - CQRS - Clean Architecture

## Dev Notes

**Tiempo estimado:** 3-4 horas
**Proyecto:** `dotnet new console -n S06-Homework`

| Parte | Actividad | Tiempo |
|-------|-----------|--------|
| 1 | Lecturas obligatorias | 1 hora |
| 2 | `EliminarProductoHandler` (Command  -  soft delete) | 1.5 horas |
| 3 | ADR-005: Monolito Modular vs Microservicios | 1 hora |

---

## Parte 1  -  Lecturas (1 hora)

| Recurso | Tiempo est. |
|---------|-------------|
| `dev-notes/03-arquitectura/01-ddd.md` | 25 min |
| `dev-notes/03-arquitectura/02-cqrs.md` | 20 min |
| `dev-notes/03-arquitectura/03-hexagonal-vs-clean.md` | 15 min |

**Recomendados:**

| Libro | Capítulo |
|-------|----------|
| *Clean Architecture* (Martin) | Cap. 22  -  The Clean Architecture |
| *Domain-Driven Design* (Evans) | Cap. 1  -  Putting the Domain Model to Work |

---

## Parte 2  -  Estructura del proyecto

```
S06-Homework/
|-- Dominio/
|   `-- Producto.cs        <- copiar de S06-Kata (incluye los tipos Result)
|-- Aplicacion/
|   |-- IProductoRepository.cs
|   |-- ProductoDto.cs
|   `-- EliminarProductoHandler.cs
|-- Infraestructura/
|   `-- InMemoryProductoRepository.cs
`-- Program.cs             <- NO modificar
```

> Reutilizar `Producto.cs`, `IProductoRepository.cs`, `InMemoryProductoRepository.cs` y `ProductoDto.cs` del proyecto `S06-Kata`. Copiarlos sin cambios.

---

## Parte 2  -  Contexto: soft delete

El catálogo necesita poder **desactivar** un producto. En lugar de un `DELETE` físico (que rompería integridad referencial con pedidos y conversaciones), se hace un **soft delete**: `EstaActivo = false`.

El producto sigue en la base de datos pero desaparece de todas las consultas que filtran `EstaActivo = true`. Este es un **Command** (escribe estado).

**Archivo:** `Aplicacion/EliminarProductoHandler.cs`

```csharp
public sealed record EliminarProductoCommand(Guid Id);
```

---

## Parte 2  -  `EliminarProductoHandler` (skeleton)

```csharp
public sealed class EliminarProductoHandler
{
    private readonly IProductoRepository _repo;
    public EliminarProductoHandler(IProductoRepository repo) => _repo = repo;

    // TODO: 1. Buscar el producto por Id en el repo
    //       2. Si no existe -> Result.Fail("Producto no encontrado.")
    //       3. Si existe -> llamar producto.Desactivar()
    //       4. Si Desactivar() falla (ya estaba inactivo) -> Result.Fail(error)
    //       5. Si Desactivar() éxito -> guardar en repo y retornar Result.OK()
    public async Task<Result> HandleAsync(
        EliminarProductoCommand command, CancellationToken ct = default) =>
        throw new NotImplementedException();
}
```

---

## Parte 2  -  `Program.cs` (no modificar)  -  parte 1

```csharp
Console.WriteLine("--- EliminarProductoHandler ---");

IProductoRepository repo = new InMemoryProductoRepository();

var r1 = Producto.Create("Auriculares Bluetooth", 349m);
var r2 = Producto.Create("Mousepad XL", 129m);
await repo.GuardarAsync(r1.Value!);
await repo.GuardarAsync(r2.Value!);

var eliminarHandler = new EliminarProductoHandler(repo);

// Eliminar el primero
var del1 = await eliminarHandler.HandleAsync(
    new EliminarProductoCommand(r1.Value!.Id));
Console.WriteLine(del1.IsSuccess
    ? "  [OK] Auriculares eliminados"
    : $"  [Err] {del1.Error}");
```

---

## Parte 2  -  `Program.cs` (no modificar)  -  parte 2

```csharp
// Intentar eliminar de nuevo (ya desactivado)
var del2 = await eliminarHandler.HandleAsync(
    new EliminarProductoCommand(r1.Value!.Id));
Console.WriteLine(del2.IsSuccess ? "  [OK] Eliminado" : $"  [Err] {del2.Error}");

// Intentar eliminar Id inexistente
var del3 = await eliminarHandler.HandleAsync(
    new EliminarProductoCommand(Guid.NewGuid()));
Console.WriteLine(del3.IsSuccess ? "  [OK] Eliminado" : $"  [Err] {del3.Error}");

// Listar activos  -  solo debe quedar Mousepad
var activos = await repo.ListarActivosAsync();
Console.WriteLine($"  Activos restantes: {activos.Count}");
foreach (var p in activos) Console.WriteLine($"    - {p.Nombre}");
```

---

## Parte 2  -  Salida esperada y criterios

**Salida esperada:**
```
--- EliminarProductoHandler ---
  [OK] Auriculares eliminados
  [Err] El producto ya está desactivado.
  [Err] Producto no encontrado.
  Activos restantes: 1
    - Mousepad XL
```

**Criterios de aceptación:**
- [ ] El proyecto compila sin warnings
- [ ] La salida coincide exactamente con la esperada
- [ ] `EliminarProductoHandler` delega la regla de negocio a `producto.Desactivar()`
- [ ] El Handler NO implementa `if (!producto.EstaActivo)`  -  usa el `Result` de `Desactivar()`
- [ ] El soft delete no elimina el registro  -  solo marca `EstaActivo = false`

---

## Parte 3  -  ADR-005: Formato a completar (parte 1)

Crear el archivo `ADR-005-monolito-modular.md` dentro de `S06-Homework/`.

```markdown
# ADR-005  -  Arquitectura: Monolito Modular sobre Microservicios

**Status:** Accepted
**Date:** YYYY-MM-DD
**Deciders:** Tech Lead, Arquitecto, Equipo de Desarrollo

## Contexto

[Describir: equipo pequeño, producto nuevo, dominio no completamente definido,
sin clientes en producción, escala desconocida.
¿Por qué esta decisión es necesaria ahora?]

## Alternativas consideradas

| Alternativa | Pro | Contra |
|-------------|-----|--------|
| Monolito Modular | | |
| Microservicios desde el inicio | | |
| Monolito sin modularidad | | |
```

---

## Parte 3  -  ADR-005: Formato a completar (parte 2)

```markdown
## Decisión

[La decisión: Monolito Modular.
Justificar con 2-3 razones concretas.]

## Consecuencias

### Positivas
- [Al menos 3 puntos]

### Negativas / Trade-offs
- [Al menos 2 puntos]

### Condición de revisión
[Criterio medible  -  ej: "cuando un módulo supere X requests/seg
de forma sostenida durante Y semanas consecutivas"]
```

---

## Parte 3  -  Criterios de aceptación

- [ ] `ADR-005-monolito-modular.md` tiene el formato completo (Status, Date, Deciders, 3 secciones)
- [ ] La tabla de alternativas tiene al menos 2 pros y 2 contras por opción
- [ ] La sección "Condición de revisión" tiene un criterio medible  -  no basta "cuando sea necesario"
- [ ] El ADR es individual  -  no copia del compañero

---

## Criterios de aceptación globales

- [ ] `S06-Homework` compila sin errores ni warnings (`dotnet build`)
- [ ] La salida de `Program.cs` coincide exactamente con la esperada
- [ ] `EliminarProductoHandler` delega las reglas de negocio a la entidad  -  no las reimplementa
- [ ] `ADR-005-monolito-modular.md` tiene el formato completo

---

## Preparación más adelante

**Lecturas para la próxima sesión:**
- `dev-notes/04-backend/22-ef-core-multi-tenancy.md`
- `dev-notes/04-backend/24-multi-tenancy-logic.md`

**Pregunta de preparación:**

En el back-template, `ITenantContextAccessor` expone el `TenantId` del workspace actual. Si un Handler consulta la base de datos sin filtrar por `TenantId`, ¿qué datos podría ver? ¿Cómo lo previene EF Core con `HasQueryFilter`?

> Traer la respuesta escrita para discutirla al inicio de una sesión posterior.
