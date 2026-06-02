# 17 — Memento

**Categoría:** Conductual

**Intención:** Permite guardar y restaurar el estado previo de un objeto sin revelar los detalles de su implementación.

> Fuente: *Dive Into Design Patterns* (Alexander Shvets) — Ch.5 Behavioral Patterns: Memento

---

## El problema

Quieres implementar Undo en un editor de texto. El editor tiene estado complejo (texto, cursor, selección, historial de formato). Para guardar el estado, necesitas acceder a los campos privados del editor — violar el encapsulamiento. Si expones esos campos, cualquier otro objeto puede modificarlos inesperadamente.

```csharp
// ❌ Sin Memento — rompe el encapsulamiento para guardar estado
class TextEditor
{
    public string Text { get; set; }       // debe ser público para guardar estado
    public int CursorPosition { get; set; } // debe ser público para restaurar
    // ← ahora cualquier clase puede modificar estos campos directamente
}
```

---

## Analogía

Una fotografía. Tomar una foto captura el estado de una escena (el Originator) en un momento específico. La foto (el Memento) almacena ese estado. Si algo se mueve, puedes volver a mirar la foto — pero la foto no permite *modificar* la escena original a través de ella. El álbum de fotos (el Caretaker) gestiona las fotos sin saber qué contienen.

---

## Estructura

```
Originator
├── _state: string                 ← estado privado que queremos guardar
├── DoSomething() → modifica estado
├── Save(): IMemento               ← captura el estado en un memento
└── Restore(IMemento)              ← restaura desde un memento

IMemento
├── GetState(): string             ← solo el Originator usa este método
├── GetName(): string              ← solo el Caretaker usa esto (metadata)
└── GetDate(): DateTime

ConcreteMemento : IMemento
├── _state: string
└── _date: DateTime

Caretaker
├── _mementos: List<IMemento>
├── _originator: Originator
├── Backup() → _mementos.Add(_originator.Save())
├── Undo()   → _originator.Restore(_mementos.Last())
└── ShowHistory()
```

---

## Código del ejemplo conceptual

```csharp
class Originator
{
    private string _state;

    public Originator(string state)
    {
        _state = state;
        Console.WriteLine("Originator: My initial state is: " + state);
    }

    // Cambia el estado (simulando alguna operación de negocio)
    public void DoSomething()
    {
        Console.WriteLine("Originator: I'm doing something important.");
        _state = GenerateRandomString(30);
        Console.WriteLine($"Originator: and my state has changed to: {_state}");
    }

    // Guarda el estado actual en un Memento
    public IMemento Save()
    {
        return new ConcreteMemento(_state);  // solo el Originator sabe qué guardar
    }

    // Restaura el estado desde un Memento
    public void Restore(IMemento memento)
    {
        if (memento is not ConcreteMemento concrete)
            throw new Exception("Unknown memento class.");

        _state = concrete.GetState();  // solo el Originator puede leer el estado del Memento
        Console.WriteLine($"Originator: My state has changed to: {_state}");
    }

    private string GenerateRandomString(int length) =>
        new string(Enumerable.Range(0, length).Select(_ => (char)('a' + Random.Shared.Next(26))).ToArray());
}

// El Memento almacena el estado del Originator
public interface IMemento
{
    string GetName();    // metadata para el Caretaker
    string GetState();   // el estado real — solo el Originator debería usar esto
    DateTime GetDate();
}

class ConcreteMemento : IMemento
{
    private string _state;
    private DateTime _date;

    public ConcreteMemento(string state)
    {
        _state = state;
        _date  = DateTime.Now;
    }

    public string GetState()    => _state;
    public string GetName()     => $"{_date:s} / ({_state[..9]})...";
    public DateTime GetDate()   => _date;
}

// El Caretaker solo gestiona los Mementos — no los lee ni modifica
class Caretaker
{
    private List<IMemento> _mementos = new();
    private Originator _originator;

    public Caretaker(Originator originator) { _originator = originator; }

    public void Backup()
    {
        Console.WriteLine("\nCaretaker: Saving Originator's state...");
        _mementos.Add(_originator.Save());
    }

    public void Undo()
    {
        if (_mementos.Count == 0) return;

        var memento = _mementos.Last();
        _mementos.Remove(memento);

        Console.WriteLine("Caretaker: Restoring state to: " + memento.GetName());
        _originator.Restore(memento);
    }

    public void ShowHistory()
    {
        Console.WriteLine("Caretaker: Here's the list of mementos:");
        foreach (var m in _mementos)
            Console.WriteLine(m.GetName());
    }
}

// Uso:
var originator = new Originator("Super-duper-super-puper-super.");
var caretaker  = new Caretaker(originator);

caretaker.Backup();         // guarda estado 1
originator.DoSomething();   // modifica

caretaker.Backup();         // guarda estado 2
originator.DoSomething();   // modifica

caretaker.ShowHistory();    // muestra historial
caretaker.Undo();           // regresa a estado 2
caretaker.Undo();           // regresa a estado 1
```

---

## Ejemplo real: editor de texto con Undo/Redo

```csharp
public class EditorState  // el Memento
{
    public string Content { get; }
    public int CursorPos  { get; }
    public DateTime SavedAt { get; }

    internal EditorState(string content, int cursorPos)
    {
        Content   = content;
        CursorPos = cursorPos;
        SavedAt   = DateTime.UtcNow;
    }
}

public class TextEditor  // el Originator
{
    private string _content   = string.Empty;
    private int    _cursorPos = 0;

    public void Type(string text)
    {
        _content   = _content.Insert(_cursorPos, text);
        _cursorPos += text.Length;
    }

    public void Delete(int count)
    {
        int start = Math.Max(0, _cursorPos - count);
        _content   = _content.Remove(start, Math.Min(count, _content.Length - start));
        _cursorPos = start;
    }

    // Guarda el estado actual (encapsulado — los campos son privados)
    public EditorState SaveState() => new EditorState(_content, _cursorPos);

    // Restaura desde un estado guardado
    public void RestoreState(EditorState state)
    {
        _content   = state.Content;
        _cursorPos = state.CursorPos;
    }

    public string GetContent() => _content;
}

public class EditorHistory  // el Caretaker
{
    private readonly Stack<EditorState> _undoStack = new();
    private readonly Stack<EditorState> _redoStack = new();
    private readonly TextEditor _editor;

    public EditorHistory(TextEditor editor) { _editor = editor; }

    public void Save()
    {
        _undoStack.Push(_editor.SaveState());
        _redoStack.Clear();  // nueva acción limpia el redo
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push(_editor.SaveState());  // guarda estado actual para redo
        _editor.RestoreState(_undoStack.Pop());
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push(_editor.SaveState());
        _editor.RestoreState(_redoStack.Pop());
    }
}

// Uso:
var editor  = new TextEditor();
var history = new EditorHistory(editor);

history.Save();
editor.Type("Hello ");
history.Save();
editor.Type("World");
history.Save();
editor.Delete(5);  // borra "World"

Console.WriteLine(editor.GetContent());  // "Hello "
history.Undo();
Console.WriteLine(editor.GetContent());  // "Hello World"
history.Redo();
Console.WriteLine(editor.GetContent());  // "Hello "
```

---

## Memento con Records en C# — la forma moderna

Los records de C# son inmutables y se copian con `with` — la forma más limpia de Memento:

```csharp
// El estado ES el Memento — inmutable, no se puede modificar
public sealed record DocumentState(
    string Title,
    string Body,
    IReadOnlyList<string> Tags,
    DateTime LastModified);

public class Document  // Originator
{
    private DocumentState _currentState;

    public Document(string title)
    {
        _currentState = new DocumentState(title, "", Array.Empty<string>(), DateTime.UtcNow);
    }

    public void UpdateTitle(string title)
    {
        _currentState = _currentState with { Title = title, LastModified = DateTime.UtcNow };
    }

    // El estado ES el Memento — retornar el record (inmutable) es suficiente
    public DocumentState GetMemento() => _currentState;
    public void RestoreMemento(DocumentState state) { _currentState = state; }
}
```

---

## En este proyecto

```
El proyecto no usa Memento explícitamente — no hay operaciones de deshacer.
Todos los "estados" son inmutables por diseño (records).

Conceptualmente, las migraciones de schema SQL son Mementos:
- Cada archivo .sql es un snapshot del estado de la base de datos
- Para "deshacer" un migration, creas un nuevo migration (no modificas el existente)
- Esta es la política "append-only" del schema: Host/Services/Schema Migration/Tables/
```

---

## Cuándo usar

- Cuando quieres implementar Undo/Redo.
- Cuando el acceso directo a los campos de un objeto viola el encapsulamiento.
- Cuando guardar estado es costoso — el Memento te permite decidir cuándo hacerlo.
- Editores (texto, gráficos, CAD), juegos (guardar partida), transacciones (rollback manual).

## Cuándo NO usar

- Cuando el estado del objeto es simple y no requiere encapsulamiento estricto.
- Cuando los estados son muy pesados y guardarlos consume demasiada memoria.
- Cuando C# records con `with` resuelven el problema más simplemente.


---

## Glosario

| Término | Definición |
|---------|-----------|
| Memento | Patrón conductual que guarda y restaura el estado de un objeto sin violar el encapsulamiento |
| Originator | Objeto cuyo estado se quiere guardar; crea y restaura Mementos |
| Caretaker | Objeto que gestiona la lista de Mementos sin leer su contenido; solo sabe cuándo guardar y restaurar |
| `IMemento` | Interfaz del Memento que expone metadatos para el Caretaker (`GetName`, `GetDate`) pero no el estado interno |
| Encapsulamiento | Principio que el Memento preserva: el Caretaker no puede leer ni modificar el estado interno del Memento |
| Undo/Redo | Funcionalidad clásica implementada con Memento: pila de estados anteriores para deshacer y rehacer |
| `EditorState` | Ejemplo del Memento como record inmutable — captura el estado del `TextEditor` en un instante |
| Expresión `with` | En C# moderno, la forma idiomática de Prototype/Memento para records: genera una copia con campos modificados |
| Migración SQL | Analogía del Memento en el proyecto: cada archivo de migración es un snapshot del estado del esquema de base de datos |
| `Stack<T>` | Estructura de datos usada en el Caretaker para implementar Undo/Redo con acceso LIFO al historial |

---

*Rogelio Arriaga Gonzalez*
