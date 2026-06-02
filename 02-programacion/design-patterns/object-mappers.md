# Object Mappers

El patrón Object Mapper encapsula la lógica de copiar las propiedades de un objeto a otro. Surge inevitablemente cuando distintas capas de la aplicación usan modelos diferentes (entidad de dominio, DTO de respuesta, DTO de entrada) y alguien debe hacer la conversión entre ellos.

> Fuente: *Architecting ASP.NET Core Applications, 3a ed.* (Carl-Hugo Marcotte) — Ch.15 Object Mappers

---

## El problema que resuelve

```csharp
// ❌ Sin mapper — la lógica de conversión está inline en cada endpoint
app.MapGet("/products", async (IProductRepository repo, CancellationToken ct) =>
{
    var products = await repo.AllAsync(ct);

    // Cada endpoint repite la misma transformación Product → DTO
    return products.Select(p => new
    {
        p.Id,
        p.Name,
        p.QuantityInStock
    });
});

// Si Product agrega un campo, hay que buscar y actualizar todos los sitios
// donde se hace esta conversión manual.
```

Con mappers, la transformación vive en una sola clase y los endpoints la delegan:

```csharp
// ✓ Con mapper — el endpoint no conoce los detalles de la conversión
app.MapGet("/products", async (
    IProductRepository repo,
    IMapper<Product, ProductDetails> mapper,
    CancellationToken ct) =>
{
    var products = await repo.AllAsync(ct);
    return products.Select(p => mapper.Map(p));
});
```

---

## Diseño base — interfaz IMapper\<TSource, TDestination\>

```csharp
// En Core (la capa de dominio) — independiente de cualquier librería
namespace Core.Mappers;
public interface IMapper<TSource, TDestination>
{
    TDestination Map(TSource entity);
}
```

Esta interfaz es el contrato mínimo que permite:
- Inyectar el mapper por DI sin acoplarse a la implementación.
- Cambiar la implementación (manual, AutoMapper, Mapperly) sin tocar los consumers.
- Registrar la misma clase como múltiples implementaciones si mapea más de un par de tipos.

---

## Implementación manual

### DTOs de ejemplo

```csharp
// Input DTOs
public record class AddStocksCommand(int Amount);
public record class RemoveStocksCommand(int Amount);

// Output DTOs
public record class StockLevel(int QuantityInStock);
public record class ProductDetails(int Id, string Name, int QuantityInStock);
public record class ProductNotFound(int ProductId, string Message);
public record class NotEnoughStock(int AmountToRemove, int QuantityInStock, string Message);
```

### Clases de mapper

```csharp
// Mapper de entidad a DTO
public class ProductMapper : IMapper<Product, ProductDetails>
{
    public ProductDetails Map(Product entity)
        => new(entity.Id ?? default, entity.Name, entity.QuantityInStock);
}

// Una clase implementando múltiples interfaces de mapper
public class ExceptionsMapper
    : IMapper<ProductNotFoundException, ProductNotFound>,
      IMapper<NotEnoughStockException, NotEnoughStock>
{
    public ProductNotFound Map(ProductNotFoundException exception)
        => new(exception.ProductId, exception.Message);

    public NotEnoughStock Map(NotEnoughStockException exception)
        => new(exception.AmountToRemove, exception.QuantityInStock, exception.Message);
}

// Por qué mapear excepciones a DTOs:
// 1. Evitar filtrar información interna al cliente (stack trace, tipos internos)
// 2. Intentar serializar una excepción lanza NotSupportedException en .NET
// 3. Uniformizar la estructura de errores en toda la API
```

### Registro en el contenedor de DI

```csharp
// Binding estándar — una implementación por interfaz
builder.Services
    .AddSingleton<IMapper<Product, ProductDetails>, ProductMapper>()
    .AddSingleton<IMapper<ProductNotFoundException, ProductNotFound>, ExceptionsMapper>()
    .AddSingleton<IMapper<NotEnoughStockException, NotEnoughStock>, ExceptionsMapper>();

// Alternativa — reutilizar la misma instancia para múltiples interfaces
builder.Services
    .AddSingleton<ExceptionsMapper>()
    .AddSingleton<IMapper<ProductNotFoundException, ProductNotFound>, ExceptionsMapper>(
        sp => sp.GetRequiredService<ExceptionsMapper>())
    .AddSingleton<IMapper<NotEnoughStockException, NotEnoughStock>, ExceptionsMapper>(
        sp => sp.GetRequiredService<ExceptionsMapper>());
// DIP: los consumers dependen solo de IMapper<T,U>, no de ExceptionsMapper
```

---

## Code smell: demasiadas dependencias

Al inyectar un `IMapper` separado por cada conversión, los endpoints acumulan dependencias rápidamente:

```csharp
// ❌ Demasiadas dependencias — señal de que la clase hace demasiado
app.MapPost("/products/{productId}/remove-stocks", async (
    int productId,
    RemoveStocksCommand command,
    StockService stockService,
    IMapper<ProductNotFoundException, ProductNotFound> notFoundMapper,
    IMapper<NotEnoughStockException, NotEnoughStock> notEnoughStockMapper,
    CancellationToken ct) => { ... });
```

**Regla de Ferreira:** más de 3-4 dependencias es una señal para investigar si la clase tiene demasiadas responsabilidades. Dos soluciones:

1. **Aggregate Services** — agrupa mappers relacionados en un servicio.
2. **Mapping Facade** — expone un método genérico `Map<TSource, TDestination>`.

---

## Aggregate Services

Agrupa múltiples mappers bajo una interfaz cohesiva para reducir el número de dependencias:

```csharp
// Interfaz del aggregate
public interface IProductMappers
{
    IMapper<Product, ProductSummary> EntityToDto { get; }
    IMapper<InsertProduct, Product> InsertDtoToEntity { get; }
    IMapper<UpdateProduct, Product> UpdateDtoToEntity { get; }
}

// Implementación — agrega los mappers individuales
public class ProductMappers : IProductMappers
{
    public ProductMappers(
        IMapper<Product, ProductSummary> entityToDto,
        IMapper<InsertProduct, Product> insertDtoToEntity,
        IMapper<UpdateProduct, Product> updateDtoToEntity)
    {
        EntityToDto       = entityToDto       ?? throw new ArgumentNullException(nameof(entityToDto));
        InsertDtoToEntity = insertDtoToEntity ?? throw new ArgumentNullException(nameof(insertDtoToEntity));
        UpdateDtoToEntity = updateDtoToEntity ?? throw new ArgumentNullException(nameof(updateDtoToEntity));
    }

    public IMapper<Product, ProductSummary> EntityToDto { get; }
    public IMapper<InsertProduct, Product> InsertDtoToEntity { get; }
    public IMapper<UpdateProduct, Product> UpdateDtoToEntity { get; }
}

// Consumer — inyecta solo un servicio en lugar de tres
public class ProductsController
{
    private readonly IProductMappers _mapper;
    public ProductsController(IProductMappers mapper) { _mapper = mapper; }

    public ProductSummary GetSummary(Product product)
        => _mapper.EntityToDto.Map(product);
}
```

> **Advertencia:** mover dependencias a un aggregate no resuelve el problema si la clase subyacente sigue teniendo demasiadas responsabilidades — solo lo oculta. Usar aggregates solo cuando hay cohesión real entre las dependencias agrupadas.

---

## Mapping Facade

Una interfaz genérica que despacha al mapper correcto internamente:

```csharp
// Interfaz genérica — el consumer no sabe qué mapper interno se usa
public interface IMappingService
{
    TDestination Map<TSource, TDestination>(TSource entity);
}

// Implementación con Service Locator
public class ServiceLocatorMappingService : IMappingService
{
    private readonly IServiceProvider _sp;
    public ServiceLocatorMappingService(IServiceProvider sp) { _sp = sp; }

    public TDestination Map<TSource, TDestination>(TSource entity)
    {
        var mapper = _sp.GetRequiredService<IMapper<TSource, TDestination>>();
        return mapper.Map(entity);
    }
}

// Consumer — solo una dependencia sin importar cuántos tipos se mapeen
public class ProductsController
{
    private readonly IMappingService _mapper;
    public ProductsController(IMappingService mapper) { _mapper = mapper; }

    public ProductSummary GetSummary(Product product)
        => _mapper.Map<Product, ProductSummary>(product);
}
```

---

## AutoMapper

AutoMapper es la librería de object mapping más establecida del ecosistema .NET. Usa convenciones de nombre para generar la lógica de copia automáticamente.

```bash
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```

### Crear un perfil

```csharp
// Los perfiles agrupan las reglas de mapping por módulo/capa
// Deben estar en la capa que los usa (no en Core) para no crear dependencias innecesarias
public class WebProfile : Profile
{
    public WebProfile()
    {
        // Por convención: AutoMapper copia propiedades con el mismo nombre
        CreateMap<Product, ProductDetails>();
        CreateMap<NotEnoughStockException, NotEnoughStock>();
        CreateMap<ProductNotFoundException, ProductNotFound>();
    }
}
```

### Registro y uso

```csharp
// Registro — escanea el assembly en busca de perfiles
builder.Services.AddAutoMapper(typeof(WebProfile).Assembly);

// Uso — IMapper se inyecta automáticamente
app.MapGet("/products", async (
    IProductRepository repo,
    IMapper mapper,
    CancellationToken ct) =>
{
    var products = await repo.AllAsync(ct);
    return products.Select(p => mapper.Map<Product, ProductDetails>(p));
});

// Con EF Core — ProjectTo limita los campos que la query recupera
public IEnumerable<ProductDto> GetAllProducts()
    => _mapper.ProjectTo<ProductDto>(_db.Products);
```

### Validar la configuración (test)

```csharp
// Atrapar errores de configuración antes de llegar a producción
[Fact]
public async Task AutoMapper_configuration_is_valid()
{
    await using var application = new WebApplicationFactory<Program>();
    var mapper = application.Services.GetRequiredService<IMapper>();
    mapper.ConfigurationProvider.AssertConfigurationIsValid();
}
```

**Cuándo usar AutoMapper:**
- Cuando los nombres de propiedades son iguales (o similares) en source y destination.
- En proyectos con muchos DTOs que requieren conversión mecánica.
- Para eliminar boilerplate de mapping que no aporta lógica de negocio.

**Cuándo NO usar AutoMapper:**
- Cuando la lógica de conversión es compleja — el magic de convenciones dificulta el debug.
- Cuando el mapping es mínimo — agregar AutoMapper por 2-3 conversiones es sobreingeniería.
- Cuando el rendimiento es crítico — Mapperly es significativamente más rápido.

---

## Mapperly

Mapperly usa **source generators** para generar el código de mapping en tiempo de compilación. Es más rápido que AutoMapper porque no usa reflexión en runtime; el compilador escribe el boilerplate por ti.

```bash
dotnet add package Riok.Mapperly
```

### Mapper inyectable (instancia)

```csharp
// La clase debe ser partial para que el source generator la extienda
[Mapper]
public partial class ProductMapper
{
    // El source generator genera la implementación de este método
    public partial ProductDetails MapToProductDetails(Product product);
}

// Código generado por el compilador (solo lectura, no editar):
// public partial class ProductMapper
// {
//     public partial ProductDetails MapToProductDetails(Product product)
//     {
//         var target = new ProductDetails(
//             product.Id ?? throw new ArgumentNullException(nameof(product.Id)),
//             product.Name,
//             product.QuantityInStock
//         );
//         return target;
//     }
// }

// Registro
builder.Services.AddSingleton<ProductMapper>();

// Uso
app.MapGet("/products", async (IProductRepository repo, ProductMapper mapper, CancellationToken ct) =>
{
    var products = await repo.AllAsync(ct);
    return products.Select(p => mapper.MapToProductDetails(p));
});
```

### Centralizar todos los mappers en una clase + interfaz

```csharp
public interface IMapper
{
    NotEnoughStock MapToDto(NotEnoughStockException source);
    ProductNotFound MapToDto(ProductNotFoundException source);
    ProductDetails MapToProductDetails(Product product);
}

[Mapper]
public partial class Mapper : IMapper
{
    public partial NotEnoughStock   MapToDto(NotEnoughStockException source);
    public partial ProductNotFound  MapToDto(ProductNotFoundException source);
    public partial ProductDetails   MapToProductDetails(Product product);
}
```

### Mapper estático

```csharp
[Mapper]
public static partial class ExceptionMapper
{
    // Método estático — no requiere DI
    public static partial ProductNotFound Map(ProductNotFoundException exception);
}

// Uso — sin inyección de dependencias (crea acoplamiento fuerte)
catch (ProductNotFoundException ex)
{
    return Results.NotFound(ExceptionMapper.Map(ex));
}
```

### Extension method mapper

```csharp
[Mapper]
public static partial class ExceptionMapper
{
    // Extension method — más elegante que llamada estática directa
    public static partial NotEnoughStock ToDto(this NotEnoughStockException exception);
}

// Uso — natural en el flujo del código
catch (NotEnoughStockException ex)
{
    return Results.Conflict(ex.ToDto());
}
```

### Inspeccionar el código generado

```xml
<!-- En el .csproj para ver los archivos generados por el source generator -->
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
<!-- Archivos en: obj\Debug\net8.0\generated\ -->
```

---

## Comparativa de enfoques

| Aspecto | Manual | Aggregate Service | AutoMapper | Mapperly |
|---------|--------|------------------|-----------|---------|
| Velocidad runtime | ✓ | ✓ | Regular (reflexión) | ✓ (código generado) |
| Magia implícita | Ninguna | Ninguna | Alta (convenciones) | Baja (contratos explícitos) |
| Error en compilación | Si el tipo no coincide | Si el tipo no coincide | No — error en runtime | ✓ — errores en compilación (RMG013) |
| Boilerplate | Alto | Medio | Bajo | Muy bajo |
| Depuración | Trivial | Trivial | Difícil | Fácil (ver código generado) |
| Configuración avanzada | Código C# directo | Código C# directo | Profiles, TypeConverters | Attributes de Mapperly |
| Caso ideal | Pocas conversiones complejas | Muchos mappers cohesivos | Muchos DTOs simples | Rendimiento + type safety |

---

## Relación con el back-template

En el back-template, los handlers construyen los DTOs manualmente:

```csharp
// Handler retorna el DTO construido manualmente
public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);

    if (user is null)
        return new GetExampleUserNotFoundFailure("Usuario no encontrado.");

    // Mapping manual: entidad → DTO
    var dto = new ExampleUserDto(user.PublicId, user.FullName, user.Email, user.IsActive);
    return new GetExampleUserSuccess(dto);
}
```

Para proyectos que escalan, se puede introducir un mapper de Mapperly sin cambiar la interfaz del handler:

```csharp
// Con Mapperly — el handler delega el mapping
[Mapper]
public partial class ExampleUserMapper
{
    public partial ExampleUserDto MapToDto(ExampleUser user);
}

public async Task<GetExampleUserResponse> Handle(
    GetExampleUserRequest request, CancellationToken ct)
{
    var user = await _repo.GetByPublicIdAsync(request.PublicId, ct);
    if (user is null)
        return new GetExampleUserNotFoundFailure("Usuario no encontrado.");
    return new GetExampleUserSuccess(_mapper.MapToDto(user));
}
```

---

## Cuándo usar

- Cuando la aplicación tiene múltiples capas con modelos propios (Domain entity, Application DTO, API ViewModel).
- Cuando el mapping manual se repite en varios handlers — centralizar en una clase de mapper.
- Cuando el mapping es mecánico (nombres de propiedades iguales) — usar Mapperly o AutoMapper.
- Cuando se necesita testear la lógica de conversión por separado — los mappers son fáciles de unit-testear.

## Cuándo NO usar

- Cuando las capas comparten el mismo modelo — mapear es overhead innecesario.
- Cuando solo hay 1-2 conversiones simples — un mapper es sobreingeniería.
- Cuando la lógica de conversión es tan específica que no justifica una abstracción — hacerla inline con un comentario explicativo es válido.

---

## Glosario

| Término | Definición |
|---------|-----------|
| Object Mapper | Clase responsable de copiar las propiedades de un objeto de un tipo a otro, encapsulando esa transformación lejos de los consumers |
| IMapper\<TSource, TDestination\> | Interfaz genérica con un método `Map(TSource)` que retorna `TDestination` — contrato mínimo para inyectar mappers por DI |
| DTO (Data Transfer Object) | Objeto plano que transporta solo los datos que el consumer necesita — no tiene lógica de negocio |
| Profile (AutoMapper) | Clase donde se configuran las reglas de mapping de AutoMapper — organiza las reglas por módulo o capa |
| `CreateMap<T, U>()` | Método de AutoMapper en un Profile que registra una conversión automática entre dos tipos |
| `IMapper.ProjectTo<T>()` | Método de AutoMapper para EF Core que genera una SELECT con solo los campos del DTO — mejora el rendimiento |
| Mapperly | Librería de source generation que genera código de mapping en tiempo de compilación — sin reflexión, sin overhead en runtime |
| Source Generator | Componente del compilador de C# que genera código adicional durante la compilación — base técnica de Mapperly |
| `[Mapper]` | Atributo de Mapperly que decora la clase parcial para que el source generator genere las implementaciones |
| `partial class` | Modificador de C# que permite dividir una clase en múltiples archivos — necesario para que los source generators extiendan la clase |
| `partial method` | Método definido en una parte de una clase parcial pero implementado en otra — el source generator escribe la implementación |
| Aggregate Services | Patrón que agrupa múltiples dependencias relacionadas en un único servicio para reducir el número de inyecciones en los consumers |
| Mapping Facade | Interfaz genérica `Map<TSource, TDestination>` que delega internamente al mapper específico — el consumer no necesita saber qué mapper existe |
| IMappingService | Implementación del Mapping Facade usando Service Locator para despachar al mapper correcto |
| Code smell: Too Many Dependencies | Señal de diseño cuando una clase recibe más de 3-4 dependencias — indica posible violación de SRP |
| `AssertConfigurationIsValid()` | Método de AutoMapper que lanza una excepción si algún mapa está mal configurado — útil en tests de startup |
| RMG013 | Error del analizador de Mapperly cuando el tipo destino no tiene constructor mappeable — detectado en compilación |

---

*Rogelio Arriaga Gonzalez*
