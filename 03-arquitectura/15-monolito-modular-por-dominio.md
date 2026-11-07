# 15 - Monolito modular desacoplado por dominio

En un [monolito modular](09-modular-monolith.md), cada módulo es un área del negocio. **Desacoplarlo por dominio** significa que ese límite se respeta en todo: en el código, en los datos y en cómo se hablan los módulos. Un módulo no conoce las entrañas de otro, cada tabla tiene un solo dueño y, en un proyecto nuevo, cada módulo tiene su propio esquema en la base.

Es la vista de código. La vista de despliegue, cuántos procesos corren, está en [Monolito modular con hosts por rol](14-monolito-modular-hosts-por-rol.md). Las dos se usan juntas.

---

## Los límites salen del lenguaje del negocio

Un módulo es un **contexto delimitado** (DDD): el espacio donde una palabra significa una sola cosa.

- **Si dos áreas nombran distinto la misma cosa, probablemente son dos módulos.** "Cliente" en ventas y "cliente" en cobranza no tienen por qué ser la misma entidad.
- **Si comparten tabla pero nunca se hablan, también son dos.**
- **Cuenta las conversaciones.** Dos módulos que se llaman en cada operación son un módulo mal partido.
- **Lo compartido se gana, no se supone.** Una pieza sube a la capa compartida cuando dos módulos ya la necesitan, no cuando parece que alguien la va a necesitar. Lo compartido antes de tiempo es el acoplamiento que el monolito modular venía a evitar.

---

## Cómo se arma un módulo

Seis proyectos, en el orden de sus dependencias:

| Proyecto | Referencia a | Contiene |
|---|---|---|
| `Modulo.Domain` | nada | Entidades e interfaces de repositorio |
| `Modulo.Contracts` | nada | Tipos públicos: la única vía entre módulos |
| `Modulo.Application` | Mediator y Domain | Casos de uso: `Request`, `Handler`, `Responses` |
| `Modulo.Infrastructure` | Mediator, Domain y datos compartidos | SQL, filas y repositorios |
| `Modulo.Presentation` | Mediator, Application y web compartida | Controllers y presenters |
| `Modulo.Tests` | los anteriores | Unitarias y pruebas de arquitectura |

`Application` nunca referencia a `Infrastructure`, ni al revés. Si hace falta cruzarlas, lo que falta es una interfaz en `Domain`.

---

## Cómo se hablan

```
modulo Ventas --Ventas.Contracts--> modulo Cobranza      Si: en memoria, por contrato
modulo Ventas --Cobranza.Domain---> ...                  No: conoce las entranas del otro
modulo Ventas --INSERT en tabla de Cobranza--> ...       No: dos duenos de una tabla
```

1. **Por contratos, en memoria.** Un módulo solo puede usar el proyecto `Contracts` de otro: tipos simples, sin dependencias.
2. **Por eventos de integración** cuando una reacción no tiene que ser inmediata. Si el evento no puede perderse, se guarda en una tabla de outbox en la misma transacción que el cambio.
3. **Nunca por las tablas de otro.** Leer la tabla de otro módulo directo en SQL ata los dos esquemas: el día que el dueño la cambie, se rompe el que leía.

---

## Los datos: un dueño por tabla

- **Cada tabla tiene un módulo dueño, el único que escribe.** Los demás leen por las consultas que el dueño publica en sus contratos.
- **Proyecto nuevo: un esquema por módulo desde el primer día**, como `ventas.*` o `inventario.*`, en lugar de todo en `dbo` o `public`.
- **Por módulo, no por host.** Un módulo puede cargarse en varios procesos; si el esquema fuera por proceso, la misma tabla tendría dos dueños.
- **Lo transversal va aparte:** outbox, auditoría común y catálogos de plataforma en un esquema propio, como `platform.*`.
- **Se puede reforzar en la base:** un usuario por módulo con escritura solo sobre su esquema.
- **Se decide al arrancar.** En un sistema que ya corre en un solo esquema, mover tablas toca cada consulta, procedimiento y cliente que las nombra. Ahí se documenta un **inventario de dueños de tabla** y la migración a esquemas se evalúa como proyecto propio, nunca de paso.

```sql
-- Cada modulo con su esquema y su rol
CREATE SCHEMA ventas;
CREATE SCHEMA inventario;

CREATE ROLE app_ventas LOGIN;
GRANT USAGE ON SCHEMA ventas TO app_ventas;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA ventas TO app_ventas;
-- app_ventas no recibe escritura sobre inventario: lo que necesita, lo pide por contrato
```

---

## Las fronteras se prueban

Una revisión se olvida; una prueba roja no. Cada módulo trae pruebas de arquitectura que fallan si alguien cruza un límite:

```csharp
[Fact]
public void Ventas_no_conoce_el_dominio_de_otros_modulos()
{
    var resultado = Types.InAssembly(typeof(Venta).Assembly)
        .ShouldNot()
        .HaveDependencyOnAny("Inventario.Domain", "Inventario.Infrastructure", "Cobranza.Domain")
        .GetResult();

    Assert.True(resultado.IsSuccessful);
}
```

Reglas mínimas:

- Ningún módulo referencia el `Domain` ni la `Infrastructure` de otro.
- `Contracts` no depende de nada.
- `Domain` no depende de la base ni del transporte.

---

## Señales de que el desacople se está perdiendo

| Señal | Qué significa | Qué hacer |
|---|---|---|
| Un cambio de negocio toca tres módulos | La regla no tiene un dueño | Moverla al módulo que habla ese lenguaje |
| Un módulo hace `JOIN` con tablas de otro | Dos dueños de hecho | Pedir los datos por contrato |
| `Shared` crece cada semana | Lo compartido se supone | Devolver cada pieza a su módulo hasta que dos la necesiten |
| Dos módulos se llaman en cada operación | Están mal partidos | Juntarlos en uno |
| `Contracts` expone entidades de dominio | El contrato filtra el modelo interno | Contratos con tipos simples propios |

---

## Por qué importa

Un monolito modular bien desacoplado por dominio deja la puerta abierta: el día que un módulo necesite escalar distinto o pase a otro equipo, **se extrae a un servicio** sin reescribirlo, porque ya se comunica por contratos y ya es dueño de sus datos. Si los módulos comparten tablas y se conocen por dentro, esa salida cuesta tanto como reescribir.

Ver también: [DDD](01-ddd.md), [Clean Architecture](11-clean-arch.md) y [Monolito modular con hosts por rol](14-monolito-modular-hosts-por-rol.md).

---

## Glosario

| Término | Definición |
|---|---|
| Contexto delimitado | Espacio del negocio donde cada término tiene un solo significado; en el monolito, un módulo |
| Contracts | Proyecto de un módulo con los únicos tipos que otros módulos pueden usar |
| Dueño de tabla | Módulo que es el único que escribe en una tabla |
| Esquema por módulo | Las tablas de cada módulo agrupadas en su propio esquema de la base |
| Inventario de dueños | Documento que dice qué módulo es dueño de cada tabla cuando no se pueden separar en esquemas |
| Evento de integración | Mensaje que cruza la frontera entre módulos; no es un evento de dominio interno |
| Prueba de arquitectura | Prueba automática que falla si una dependencia cruza un límite de capa o de módulo |

---

*Rogelio Arriaga Gonzalez*
