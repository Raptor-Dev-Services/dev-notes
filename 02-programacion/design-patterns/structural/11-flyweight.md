# 11 — Flyweight

**Categoría:** Estructural

**Intención:** Permite incluir más objetos en la RAM disponible compartiendo partes comunes del estado entre múltiples objetos, en lugar de mantener todos los datos en cada objeto.

---

## El problema

Tienes un juego de disparos con miles de partículas (balas, misiles, metralla). Cada partícula almacena: color, sprite, coordenadas X/Y, dirección, velocidad. El color y sprite (datos grandes) son iguales para todas las balas del mismo tipo. Almacenarlos en cada instancia desperdicia cientos de MB.

```csharp
// ❌ Sin Flyweight — cada bala almacena TODO
class Particle
{
    public string Color { get; set; }    // "red" — igual para todas las balas
    public byte[] Sprite { get; set; }   // 1MB de imagen — igual para todas las balas
    public double X { get; set; }        // único por partícula
    public double Y { get; set; }        // único por partícula
    public double SpeedX { get; set; }   // único por partícula
    public double SpeedY { get; set; }   // único por partícula
}
// 10,000 partículas × 1MB sprite = 10GB de RAM
```

---

## Analogía

Tipografía de imprenta. En lugar de tener una pieza de metal por cada carácter en todo el libro, la imprenta tiene una pieza por cada carácter único (26 letras, 10 dígitos, etc.) y la reutiliza posicionándola en el lugar correcto. El estado intrínseco (la forma del carácter) se comparte; el estado extrínseco (posición en la página) es único y se pasa como parámetro.

---

## Conceptos clave

| Concepto | Definición | Ejemplo |
|----------|-----------|---------|
| **Estado intrínseco** | Compartido entre instancias — inmutable | Color, sprite, tipo de bala |
| **Estado extrínseco** | Único por instancia — pasa como parámetro | Posición X,Y, dirección, velocidad |
| **Flyweight** | Almacena estado intrínseco | El objeto compartido |
| **FlyweightFactory** | Gestiona el pool de flyweights | Retorna existente o crea nuevo |

---

## Código del ejemplo conceptual

```csharp
// La clase Car aquí sirve tanto para estado intrínseco (Company, Model, Color)
// como extrínseco (Owner, Number).
public class Car
{
    public string Owner   { get; set; }
    public string Number  { get; set; }
    public string Company { get; set; }  // ← intrínseco: compartido entre carros del mismo modelo
    public string Model   { get; set; }  // ← intrínseco
    public string Color   { get; set; }  // ← intrínseco
}

// El Flyweight almacena el estado intrínseco (Company, Model, Color)
public class Flyweight
{
    private Car _sharedState;  // estado intrínseco — compartido, inmutable

    public Flyweight(Car car) { _sharedState = car; }

    // Recibe el estado extrínseco (Owner, Number) como parámetro — no lo almacena
    public void Operation(Car uniqueState)
    {
        Console.WriteLine(
            $"Flyweight: Displaying shared {Serialize(_sharedState)} " +
            $"and unique {Serialize(uniqueState)} state.");
    }
}

// La FlyweightFactory crea y gestiona el pool de flyweights
public class FlyweightFactory
{
    private List<Tuple<Flyweight, string>> flyweights = new();

    public FlyweightFactory(params Car[] args)
    {
        foreach (var car in args)
            flyweights.Add(new(new Flyweight(car), GetKey(car)));
    }

    private string GetKey(Car key)
    {
        var elements = new List<string> { key.Model, key.Color, key.Company };
        elements.Sort();
        return string.Join("_", elements);  // clave única para el estado intrínseco
    }

    public Flyweight GetFlyweight(Car sharedState)
    {
        string key = GetKey(sharedState);
        var existing = flyweights.FirstOrDefault(t => t.Item2 == key);

        if (existing == null)
        {
            Console.WriteLine("FlyweightFactory: Creating new flyweight.");
            var newFlyweight = new Tuple<Flyweight, string>(new Flyweight(sharedState), key);
            flyweights.Add(newFlyweight);
            return newFlyweight.Item1;
        }

        Console.WriteLine("FlyweightFactory: Reusing existing flyweight.");
        return existing.Item1;
    }
}

// Uso:
var factory = new FlyweightFactory(
    new Car { Company = "BMW", Model = "M5", Color = "red" },    // flyweight 1
    new Car { Company = "BMW", Model = "X6", Color = "white" }   // flyweight 2
);

// Los datos únicos (Owner, Number) se pasan en cada operación
var flyweight = factory.GetFlyweight(new Car { Company = "BMW", Model = "M5", Color = "red" });
// "Reusing existing flyweight." — reutiliza el existente

flyweight.Operation(new Car { Number = "CL234IR", Owner = "James Doe", Company = "BMW", Model = "M5" });
// El flyweight combina su estado intrínseco compartido con el extrínseco de este carro específico
```

---

## Ejemplo real: cache de tipos de artículos en un juego

```csharp
// Estado intrínseco: datos del tipo de árbol (pesados, compartidos)
public class TreeType
{
    public string Name    { get; }
    public string Color   { get; }
    public byte[] Texture { get; }  // imagen grande — se comparte

    public TreeType(string name, string color, byte[] texture)
    {
        Name = name; Color = color; Texture = texture;
    }

    public void Draw(int x, int y)
    {
        // Dibuja usando Texture (compartida) en la posición x,y (extrínseco)
        Console.WriteLine($"Drawing {Name} tree at ({x},{y}) with color {Color}");
    }
}

// Factory/Registry
public static class TreeTypeFactory
{
    private static Dictionary<string, TreeType> _cache = new();

    public static TreeType GetTreeType(string name, string color, byte[] texture)
    {
        string key = $"{name}_{color}";

        if (!_cache.TryGetValue(key, out var treeType))
        {
            treeType = new TreeType(name, color, texture);  // solo si no existe
            _cache[key] = treeType;
        }

        return treeType;  // siempre retorna el mismo objeto para el mismo tipo
    }
}

// Árbol individual (estado extrínseco: posición)
public class Tree
{
    public int X { get; }
    public int Y { get; }
    private TreeType _type;  // referencia al flyweight compartido

    public Tree(int x, int y, TreeType type) { X = x; Y = y; _type = type; }

    public void Draw() => _type.Draw(X, Y);  // pasa su estado extrínseco al flyweight
}

// Bosque: 10,000 árboles, pero solo 3 TreeType en memoria
var forest = new List<Tree>();
var oakTexture = File.ReadAllBytes("oak.png");  // 1MB — cargado UNA VEZ

for (int i = 0; i < 10000; i++)
{
    var oakType = TreeTypeFactory.GetTreeType("Oak", "Green", oakTexture);  // reutilizado
    forest.Add(new Tree(Random.Shared.Next(0, 1000), Random.Shared.Next(0, 1000), oakType));
}
// 10,000 Trees × (8 bytes posición + puntero) vs 10,000 × 1MB
```

---

## En este proyecto

```csharp
// Dapper compila y cachea los queries SQL — esto es Flyweight implícito:
// El texto SQL compilado (estado intrínseco) se comparte entre todas las ejecuciones.
// Los parámetros (state extrínseco) son únicos por llamada.

// Las clases ...Sql del proyecto tienen las queries como constantes:
private const string GetByPublicIdSql = """
    SELECT Id, PublicId, FullName, Email
    FROM dbo.ExampleUsers
    WHERE PublicId = @publicId;
    """;
// Este string es el estado intrínseco compartido entre todas las llamadas.
// El @publicId (estado extrínseco) varía por llamada.
```

---

## Cuándo usar

- Cuando la aplicación necesita crear un número enorme de objetos similares.
- Cuando los objetos consumen demasiada RAM.
- Cuando los objetos contienen estados duplicados que se pueden extraer y compartir.
- Juegos (partículas, árboles, enemigos), procesadores de texto (carácteres), editores gráficos (iconos).

## Cuándo NO usar

- Cuando tienes pocas instancias — la complejidad adicional no vale.
- Cuando el estado extrínseco es tan grande que no hay ahorro real.
- Cuando los objetos no comparten suficiente estado intrínseco.
- Cuando la performance no es un problema — no optimizar prematuramente.

---

## Tradeoffs

| Aspecto | Sin Flyweight | Con Flyweight |
|---------|--------------|---------------|
| Uso de RAM | Alto (duplicación) | Bajo (compartido) |
| Complejidad | Baja | Alta (separar estado intrínseco/extrínseco) |
| CPU | Menor (sin buscar en cache) | Mayor (buscar en factory) |
| Cuándo | Pocos objetos | Millones de objetos similares |


---

*Rogelio Arriaga Gonzalez*
