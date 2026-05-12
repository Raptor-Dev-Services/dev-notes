# 15 — Iterator

**Categoría:** Conductual

**Intención:** Permite recorrer elementos de una colección sin exponer su representación subyacente (lista, árbol, pila, etc.).

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.5 Behavioral Patterns: Iterator

---

## El problema

Tienes una colección con una estructura interna compleja (árbol, grafo, lista circular). El código cliente necesita recorrerla, pero no debería saber cómo está organizada internamente. Además, quieres recorrerla de diferentes formas: en orden, reversa, solo los pares, etc. sin duplicar el código de recorrido en el cliente.

```csharp
// ❌ Sin Iterator — el cliente conoce la estructura interna
class WordsCollection
{
    public List<string> Items = new();  // expone la estructura interna
}

// El cliente depende de List<string>:
var collection = new WordsCollection();
for (int i = 0; i < collection.Items.Count; i++)
    Console.WriteLine(collection.Items[i]);  // acoplado a List y al índice

// ¿Cómo recorrer en reversa sin duplicar código?
// ¿Cómo cambiar internamente a un árbol sin modificar el cliente?
```

---

## Analogía

Un guía de tour en una ciudad. Tú (el cliente) quieres ver la ciudad (la colección) pero no necesitas saber las calles. El guía conoce el recorrido. Hay diferentes tours: histórico, gastronómico, arquitectónico — cada uno es un Iterator diferente sobre la misma colección de lugares.

---

## Estructura

```
IEnumerator (Iterator)
├── Current → objeto actual
├── MoveNext(): bool → avanza; retorna false si terminó
└── Reset() → regresa al inicio

IteratorAggregate (Collection)
└── GetEnumerator(): IEnumerator → crea el iterator

AlphabeticalOrderIterator : IEnumerator
├── _collection: WordsCollection
├── _position: int
├── _reverse: bool
└── implementa Current, MoveNext, Reset

WordsCollection : IteratorAggregate
├── _collection: List<string>
├── _direction: bool  ← flag para invertir el iterator
└── GetEnumerator() → new AlphabeticalOrderIterator(this, _direction)
```

---

## Código del ejemplo conceptual

```csharp
abstract class Iterator : IEnumerator
{
    object IEnumerator.Current => Current();

    public abstract int Key();
    public abstract object Current();
    public abstract bool MoveNext();
    public abstract void Reset();
}

// Iterator concreto — encapsula el algoritmo de recorrido
class AlphabeticalOrderIterator : Iterator
{
    private WordsCollection _collection;
    private int _position = -1;
    private bool _reverse;

    public AlphabeticalOrderIterator(WordsCollection collection, bool reverse = false)
    {
        _collection = collection;
        _reverse    = reverse;
        if (reverse)
            _position = collection.getItems().Count;  // empezar desde el final
    }

    public override object Current() => _collection.getItems()[_position];
    public override int Key() => _position;

    public override bool MoveNext()
    {
        int updatedPosition = _position + (_reverse ? -1 : 1);

        if (updatedPosition >= 0 && updatedPosition < _collection.getItems().Count)
        {
            _position = updatedPosition;
            return true;
        }
        return false;
    }

    public override void Reset()
    {
        _position = _reverse ? _collection.getItems().Count - 1 : 0;
    }
}

// La colección — crea el iterator adecuado
class WordsCollection : IEnumerable
{
    List<string> _collection = new();
    bool _direction = false;  // false = normal, true = reverso

    public void ReverseDirection() { _direction = !_direction; }
    public List<string> getItems() => _collection;
    public void AddItem(string item) => _collection.Add(item);

    public IEnumerator GetEnumerator()
    {
        return new AlphabeticalOrderIterator(this, _direction);
    }
}

// Uso — el cliente usa foreach sin saber la implementación:
var collection = new WordsCollection();
collection.AddItem("First");
collection.AddItem("Second");
collection.AddItem("Third");

Console.WriteLine("Straight traversal:");
foreach (var element in collection)   // usa el Iterator internamente
    Console.WriteLine(element);
// First, Second, Third

Console.WriteLine("\nReverse traversal:");
collection.ReverseDirection();
foreach (var element in collection)   // mismo foreach, diferente iterator
    Console.WriteLine(element);
// Third, Second, First
```

---

## Iterator en C# — `IEnumerable<T>` y `yield return`

En C# moderno, implementas el patrón Iterator usando `IEnumerable<T>` y `yield return`:

```csharp
// Sin yield — Iterator manual (verbose)
public class EvenNumbersIterator : IEnumerable<int>
{
    private readonly int[] _numbers;

    public EvenNumbersIterator(int[] numbers) { _numbers = numbers; }

    public IEnumerator<int> GetEnumerator()
    {
        for (int i = 0; i < _numbers.Length; i++)
        {
            if (_numbers[i] % 2 == 0)
                yield return _numbers[i];  // ← yield genera el iterator automáticamente
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// Con yield — el compilador genera el Iterator por ti
public static IEnumerable<int> GetEvens(int[] numbers)
{
    foreach (var n in numbers)
        if (n % 2 == 0)
            yield return n;
}

// Uso:
int[] nums = { 1, 2, 3, 4, 5, 6 };
foreach (var even in GetEvens(nums))
    Console.WriteLine(even);  // 2, 4, 6
```

### `yield` — cómo funciona

`yield return` pausa la función en ese punto y retorna el valor al consumidor. En la próxima iteración (`MoveNext()`), la función continúa desde donde pausó — el compilador genera una máquina de estados por ti.

```csharp
public IEnumerable<int> LazyRange(int from, int to)
{
    for (int i = from; i <= to; i++)
    {
        Console.WriteLine($"Generando {i}");
        yield return i;  // pausa aquí; continúa en la próxima iteración del foreach
    }
}

foreach (var n in LazyRange(1, 5))
{
    Console.WriteLine($"Consumiendo {n}");
    if (n == 3) break;  // la generación se detiene aquí — lazy
}
// Generando 1, Consumiendo 1
// Generando 2, Consumiendo 2
// Generando 3, Consumiendo 3 (break)
// 4 y 5 NUNCA se generan
```

---

## LINQ — Iterator en su máxima expresión

LINQ usa Iterator internamente. Cada operación crea un nuevo Iterator que envuelve al anterior:

```csharp
var numbers = Enumerable.Range(1, 1_000_000);  // ← lazy, no genera nada todavía

var result = numbers
    .Where(n => n % 2 == 0)    // ← Iterator 2: filtro, lazy
    .Select(n => n * n)         // ← Iterator 3: transformación, lazy
    .Take(5);                   // ← Iterator 4: límite, lazy

// Solo aquí, al iterar, se ejecuta todo — y SOLO 5 números se procesan
foreach (var n in result)
    Console.WriteLine(n);  // 4, 16, 36, 64, 100
```

---

## `IAsyncEnumerable<T>` — Iterator asíncrono (C# 8+)

```csharp
// Para streams de datos asincrónicos:
public async IAsyncEnumerable<ExampleUser> StreamAllUsersAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var batch in _repo.GetBatchesAsync(100, ct))
    {
        foreach (var user in batch)
        {
            yield return user;  // yield en contexto async
        }
    }
}

// Uso:
await foreach (var user in service.StreamAllUsersAsync(ct))
{
    await ProcessUserAsync(user);
}
// No carga todos los usuarios en RAM — procesa de a lotes
```

---

## En este proyecto

```csharp
// IEnumerable<T> es el Iterator de facto en todos lados:

// Repository retorna colecciones:
public interface IExampleUserRepository
{
    Task<IReadOnlyCollection<ExampleUser>> GetActiveAsync(CancellationToken ct);
    //   ↑ IReadOnlyCollection implementa IEnumerable — es iterable
}

// En las responses:
public sealed record GetExampleUsersSuccess(
    IReadOnlyCollection<ExampleUserDto> Users,  // ← Iterator implícito
    int Total, int Page, int PageSize)
    : GetExampleUsersResponse, ISuccess;

// En los handlers, LINQ usa Iterator internamente:
var dtos = users
    .Where(u => u.IsActive)
    .Select(u => new ExampleUserDto(u.PublicId, u.FullName, u.Email, ...))
    .ToList();  // ← materializa el lazy Iterator en una List
```

---

## Cuándo usar

- Cuando tienes una colección con una estructura interna compleja y no quieres exponer esa complejidad.
- Cuando necesitas múltiples formas de recorrer la misma colección.
- Cuando quieres que diferentes colecciones usen el mismo código de recorrido (polimorfismo).
- Siempre en C# — `IEnumerable<T>` / `yield return` / LINQ son el Iterator idiomático.

## Cuándo NO usar

- Para colecciones simples de tipo `List<T>` donde el acceso directo es suficiente.
- Cuando necesitas acceso aleatorio (por índice) — no tiene sentido crear un Iterator.


---

*Rogelio Arriaga Gonzalez*
