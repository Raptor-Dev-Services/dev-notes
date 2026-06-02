# 04 — Builder

**Categoría:** Creacional

**Intención:** Permite construir objetos complejos paso a paso. El patrón te permite producir diferentes tipos y representaciones de un objeto usando el mismo código de construcción.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.3 Creational Patterns: Builder

---

## El problema

Tienes una clase `House` con decenas de parámetros opcionales: garage, piscina, jardín, sistema de calefacción, etc. El constructor se vuelve monstruoso:

```csharp
// ❌ El constructor telescópico — imposible de leer
var house = new House(4, 2, true, false, true, null, "marble", 200, true, false, 3, "oak");
// ¿Qué es el cuarto parámetro? ¿El séptimo?
```

Alternativa con setters: el objeto queda en estado incompleto e inválido durante la construcción.

---

## Analogía

Un director de obra y su equipo. El director da instrucciones paso a paso: "construye los cimientos", "levanta las paredes", "coloca el techo". El equipo de construcción (builder) sabe cómo ejecutar cada paso. Al final, el director pide el resultado terminado. La misma secuencia de instrucciones puede producir una casa de madera o una de piedra — depende del builder que uses.

---

## Estructura

```
IBuilder
├── BuildPartA()
├── BuildPartB()
└── BuildPartC()

ConcreteBuilder : IBuilder
├── BuildPartA() → agrega PartA al producto
├── BuildPartB() → agrega PartB al producto
├── BuildPartC() → agrega PartC al producto
└── GetProduct(): Product

Director
├── Builder: IBuilder   ← se le asigna el builder concreto
├── BuildMinimalViableProduct() → llama BuildPartA
└── BuildFullFeaturedProduct()  → llama A, B, C

Product → el objeto construido
```

---

## Código del ejemplo conceptual

```csharp
public interface IBuilder
{
    void BuildPartA();
    void BuildPartB();
    void BuildPartC();
}

public class ConcreteBuilder : IBuilder
{
    private Product _product = new Product();

    public ConcreteBuilder() { Reset(); }

    public void Reset() { _product = new Product(); }

    public void BuildPartA() { _product.Add("PartA1"); }
    public void BuildPartB() { _product.Add("PartB1"); }
    public void BuildPartC() { _product.Add("PartC1"); }

    // GetProduct resetea el builder para el siguiente objeto
    public Product GetProduct()
    {
        Product result = _product;
        Reset();  // listo para construir otro objeto
        return result;
    }
}

public class Product
{
    private List<string> _parts = new();

    public void Add(string part) => _parts.Add(part);

    public string ListParts() => "Product parts: " + string.Join(", ", _parts);
}

// Director — define los pasos en un orden específico
public class Director
{
    private IBuilder _builder;

    public IBuilder Builder { set { _builder = value; } }

    // Producto mínimo — solo PartA
    public void BuildMinimalViableProduct() => _builder.BuildPartA();

    // Producto completo — A + B + C
    public void BuildFullFeaturedProduct()
    {
        _builder.BuildPartA();
        _builder.BuildPartB();
        _builder.BuildPartC();
    }
}

// Uso:
var director = new Director();
var builder  = new ConcreteBuilder();
director.Builder = builder;

// Producto básico
director.BuildMinimalViableProduct();
Console.WriteLine(builder.GetProduct().ListParts());
// "Product parts: PartA1"

// Producto completo
director.BuildFullFeaturedProduct();
Console.WriteLine(builder.GetProduct().ListParts());
// "Product parts: PartA1, PartB1, PartC1"

// Sin director — el cliente controla directamente
builder.BuildPartA();
builder.BuildPartC();
Console.WriteLine(builder.GetProduct().ListParts());
// "Product parts: PartA1, PartC1"
```

---

## Fluent Builder — el más común en la práctica

El Builder fluent (encadenado) es la variante más usada en C# moderno:

```csharp
// Fluent Builder — cada método retorna 'this' para encadenar
public class QueryBuilder
{
    private string _table  = string.Empty;
    private string _where  = string.Empty;
    private int    _limit  = 100;
    private string _order  = string.Empty;

    public QueryBuilder From(string table)
    {
        _table = table;
        return this;  // ← retorna this para encadenar
    }

    public QueryBuilder Where(string condition)
    {
        _where = condition;
        return this;
    }

    public QueryBuilder Limit(int limit)
    {
        _limit = limit;
        return this;
    }

    public QueryBuilder OrderBy(string column)
    {
        _order = column;
        return this;
    }

    public string Build()
    {
        var sql = $"SELECT * FROM {_table}";
        if (!string.IsNullOrEmpty(_where)) sql += $" WHERE {_where}";
        if (!string.IsNullOrEmpty(_order)) sql += $" ORDER BY {_order}";
        sql += $" LIMIT {_limit}";
        return sql;
    }
}

// Uso fluent — legible como inglés
var query = new QueryBuilder()
    .From("Users")
    .Where("IsActive = true")
    .OrderBy("CreatedAt DESC")
    .Limit(50)
    .Build();
```

---

## Builder en ASP.NET Core — `WebApplicationBuilder`

El Builder más visible en este proyecto es el de ASP.NET Core en `Program.cs`:

```csharp
// Program.cs — WebApplicationBuilder ES un Builder pattern
var builder = WebApplication.CreateBuilder(args);  // Director + Builder

// Paso a paso: agregar servicios (BuildPart*)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddLocalhostCors();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddHealthServices(builder.Configuration);

// GetProduct() — obtener la app construida
var app = builder.Build();

// Director configura el pipeline de middleware (otro Builder implícito)
app.UseHttpsRedirection();
app.UseCors(CorsExtensions.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealth();

app.Run();
```

Cada llamada a `builder.Services.Add*()` y `app.Use*()` es un paso del builder. `builder.Build()` es el `GetProduct()` que retorna la app completamente configurada.

---

## En este proyecto

```csharp
// 1. WebApplicationBuilder en Program.cs — el más obvio
// 2. NpgsqlDataSourceBuilder para configurar Npgsql
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseNodaTime();       // BuildPartA
dataSourceBuilder.EnableDynamicJson(); // BuildPartB
var dataSource = dataSourceBuilder.Build(); // GetProduct

// 3. El encadenamiento fluent de middleware es Builder
app.UseAuthentication()
   .UseAuthorization()
   .UseCors();
```

---

## Cuándo usar

- Cuando el objeto tiene muchos parámetros opcionales (evitar constructores telescópicos).
- Cuando la creación es compleja y tiene múltiples pasos.
- Cuando quieres producir diferentes representaciones del mismo objeto.
- Cuando quieres encadenar configuraciones de forma fluida y legible.

## Cuándo NO usar

- Para objetos simples con pocos parámetros — un constructor basta.
- Cuando los pasos de construcción son siempre los mismos — no hay variabilidad.
- Cuando C# records con object initializers resuelven el problema más simplemente:
  ```csharp
  var user = new CreateUserCommand { Name = "Ana", Email = "ana@test.com" };
  // Esto es más simple que un Builder para objetos de datos simples
  ```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Builder | Patrón creacional que permite construir objetos complejos paso a paso usando el mismo proceso para distintas representaciones |
| Director | Clase que define el orden en que se ejecutan los pasos del builder para producir configuraciones específicas del producto |
| ConcreteBuilder | Implementación que sabe cómo construir y ensamblar las partes de un producto específico |
| Fluent Builder | Variante del Builder donde cada método retorna `this` para permitir el encadenamiento de llamadas |
| Constructor telescópico | Anti-patrón donde un constructor acumula decenas de parámetros opcionales, haciéndose ilegible |
| `WebApplicationBuilder` | Implementación del patrón Builder en ASP.NET Core usada en `Program.cs` para configurar la aplicación |
| `NpgsqlDataSourceBuilder` | Builder de Npgsql que permite configurar la fuente de datos de PostgreSQL paso a paso |
| `builder.Build()` | Llamada equivalente al método `GetProduct()` — materializa el objeto completamente configurado |
| Object initializer | Alternativa de C# a un Builder simple: `new Clase { Prop1 = val1, Prop2 = val2 }` |
| Pipeline de middleware | Secuencia de componentes de ASP.NET Core configurada mediante llamadas encadenadas — Builder implícito |
| Producto | El objeto complejo que resulta del proceso de construcción dirigido por el Builder |

---

*Rogelio Arriaga Gonzalez*
