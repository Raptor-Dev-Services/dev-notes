# Homework S05  -  Patrones de Comportamiento

**Dev Notes**
**Tiempo estimado:** 3-4 horas

**Proyecto:** `dotnet new console -n S05-Homework`

```
S05-Homework/
|-- ManualShippingStrategy.cs
|-- diagrama-flujo.md
`-- Program.cs
```

---

## Parte 1  -  Lecturas (45 min)

| Recurso | Tiempo est. |
|---------|-------------|
| `dev-notes/02-programacion/design-patterns/behavioral/20-strategy.md` | 15 min |
| `dev-notes/02-programacion/design-patterns/behavioral/17-observer.md` | 15 min |
| `dev-notes/02-programacion/design-patterns/behavioral/19-state.md` | 10 min |
| `dev-notes/02-programacion/design-patterns/behavioral/13-command.md` | 15 min |

**Recomendados:** *Dive Into Design Patterns*  -  capítulos Strategy, Observer, State, Command.

---

## Parte 2  -  ManualShippingStrategy - Skeleton

### Contexto

En el kata de S05 implementamos Standard, Express y Free shipping. Ahora agregarás una 4ª estrategia donde un supervisor establece el costo de envío de forma explícita.

**Archivo:** `ManualShippingStrategy.cs`

```csharp
public interface IShippingCostStrategy
{
    decimal CalcularCosto(decimal subtotal, decimal pesoKg);
}

public sealed class ManualShippingStrategy : IShippingCostStrategy
{
    private readonly decimal _costoFijo;

    // TODO: validar que costoFijo >= 0
    //       si < 0: throw new ArgumentException(
    //           "El costo de envío no puede ser negativo.")
    public ManualShippingStrategy(decimal costoFijo)
    {
        _costoFijo = costoFijo;  // TODO: reemplazar con validación
        throw new NotImplementedException();
    }
```

---

## Parte 2  -  ManualShippingStrategy - Skeleton (parte 2)

```csharp
    // TODO: retornar siempre _costoFijo
    //       (ignorar subtotal y pesoKg)
    public decimal CalcularCosto(decimal subtotal, decimal pesoKg) =>
        throw new NotImplementedException();
}
```

---

## Parte 2  -  Program.cs

**Program.cs (no modificar):**

```csharp
Console.WriteLine("--- ManualShippingStrategy ---");
IShippingCostStrategy est1 = new ManualShippingStrategy(costoFijo: 12.50m);
IShippingCostStrategy est2 = new ManualShippingStrategy(costoFijo: 0m);

Console.WriteLine(
    $"  Costo supervisado $12.50 -> ${est1.CalcularCosto(75m, 3m):F2}");
Console.WriteLine(
    $"  Costo acordado $0      -> ${est2.CalcularCosto(200m, 1m):F2}");

try { _ = new ManualShippingStrategy(costoFijo: -5m); }
catch (ArgumentException ex)
    { Console.WriteLine($"  Error esperado: {ex.Message}"); }
```

---

## Parte 2  -  Salida esperada y criterios

**Salida esperada:**

```
--- ManualShippingStrategy ---
  Costo supervisado $12.50 -> $12.50
  Costo acordado $0      -> $0.00
  Error esperado: El costo de envío no puede ser negativo.
```

**Criterios de aceptación:**
- [ ] Compila sin warnings
- [ ] `CalcularCosto` siempre retorna `_costoFijo` sin importar los parámetros
- [ ] Constructor lanza `ArgumentException` si `costoFijo < 0`
- [ ] La salida coincide exactamente con la esperada

---

## Parte 3  -  Diagrama ASCII del flujo (parte 1)

Crear `diagrama-flujo.md` dentro del proyecto. Completar el diagrama identificando al menos 5 patrones de diseño con nombre y razón.

```
HTTP POST /api/conversaciones/asignar
         |
           [Patrón: _____________ | Por qué: _______________]
[Capa: WebApi  -  Controller]
  ConversacionesController -> Mediator.Send(AsignarConversacionRequest)
         |
           [Patrón: _____________ | Por qué: _______________]
[Capa: Application  -  Pipeline]
  ???
         |
    -----+-----
    |         |
               [Patrón: _________ | Por qué: _______________]
[Handler]   [Presenter(s)]
  ???         ???
```

---

## Parte 3  -  Diagrama ASCII del flujo (parte 2)

```
    |
      [Patrón: _____________ | Por qué: _______________]
[Capa: ???]
  Strategy seleccionada
         |

[Capa: ???]
  PostgreSQL
```

**Criterios de aceptación:**
- [ ] `diagrama-flujo.md` existe en el directorio del proyecto
- [ ] Al menos 5 patrones identificados, cada uno con nombre y razón
- [ ] El flujo es correcto: HTTP -> Controller -> Pipeline -> Handler -> Strategy -> DB

---

## Criterios de aceptación globales

- [ ] `S05-Homework` compila sin errores ni warnings
- [ ] La salida de Program.cs coincide exactamente con la esperada
- [ ] `ManualShippingStrategy` pasa las 3 verificaciones
- [ ] `diagrama-flujo.md` existe con al menos 5 patrones

---

## Preparación para S06

> Leer antes de S06:
> - `dev-notes/03-arquitectura/01-ddd.md`
> - `dev-notes/03-arquitectura/02-cqrs.md`
> - `dev-notes/03-arquitectura/03-hexagonal-vs-clean.md`

**Pregunta de cierre:** En el back-template, ¿en qué capa vive la regla de negocio "un producto no puede desactivarse si ya está inactivo"? ¿En el Handler, en la entidad, o en el repositorio?
