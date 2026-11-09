# Homework S02 - DRY - KISS - YAGNI

## Dev Notes

**Tiempo estimado:** 3 horas
**Modalidad:** Individual

| Parte | Actividad | Tiempo |
|-------|-----------|--------|
| 1 | Lecturas | 1 hora |
| 2 | Value Objects `Email` + `Username` | 1.5 horas |
| 3 | ADR-001 individual | 30 min |

---

## Parte 1 - Lecturas (1 hora)

### Lectura obligatoria

**`dev-notes/01-fundamentos/03-refactoring.md`** - completo

Prestar atención a:
- Cuándo el refactor es una inversión vs gold-plating
- La diferencia entre duplicación accidental y duplicación esencial
- Code smells relacionados con YAGNI: dead code, speculative generality

### Lectura opcional

**The Pragmatic Programmer** - Hunt & Thomas, Cap. 7: *The Evils of Duplication*

> Si no tienes el libro, el dev-note cubre los puntos relevantes.

---

## Parte 2 - Setup

```bash
dotnet new console -n S02-Homework
cd S02-Homework
```

Estructura del proyecto:

```
S02-Homework/
├── Program.cs
├── Email.cs
├── Username.cs
└── docs/
    └── adr/
        └── ADR-001-value-objects.md
```


---

## Parte 2 - Tipo `Result<T>` (agregar al inicio de `Program.cs`)

```csharp
public sealed class Result<T>
{
    public bool   IsSuccess { get; }
    public T?     Value     { get; }
    public string Error     { get; }

    private Result(bool ok, T? value, string error)
        => (IsSuccess, Value, Error) = (ok, value, error);

    public static Result<T> OK(T value)      => new(true, value, string.Empty);
    public static Result<T> Fail(string msg) => new(false, default, msg);
}
```

> Mismo tipo que usaste en clase. En el back-template real vive en `Common/Results/Result.cs`.

---

## Parte 2 - Punto 1: `Email.cs`

Completa los `TODO` en el archivo `Email.cs`:

```csharp
public sealed record Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Result<Email> Create(string value)
    {
        // TODO: validar not null/empty
        //       → Result<Email>.Fail("Email es requerido.")
        // TODO: validar que contiene '@'
        //       → Result<Email>.Fail("Email inválido.")
        // TODO: validar longitud máx 254 chars
        //       → Result<Email>.Fail("Email demasiado largo.")
        // Si válido: normalizar a lowercase
        //       → Result<Email>.OK(new Email(value.ToLowerInvariant()))
        throw new NotImplementedException();
    }

    public override string ToString() => Value;
}
```

> El orden de validaciones importa: primero null/empty, luego formato, luego longitud.

---

## Parte 2 - Punto 2: `Username.cs`

Completa los `TODO` en el archivo `Username.cs`:

```csharp
public sealed record Username
{
    public string Value { get; }
    private Username(string value) => Value = value;

    public static Result<Username> Create(string value)
    {
        // TODO: validar not null/empty
        //       → Result<Username>.Fail("Username es requerido.")
        // TODO: validar longitud entre 3 y 20 chars (inclusive)
        //       → Result<Username>.Fail("Debe tener entre 3 y 20 caracteres.")
        // TODO: validar solo letras, dígitos y '_'
        //       Hint: value.All(c => char.IsLetterOrDigit(c) || c == '_')
        //       → Result<Username>.Fail("Solo letras, dígitos y guión bajo.")
        // Si válido: normalizar a lowercase
        //       → Result<Username>.OK(new Username(value.ToLowerInvariant()))
        throw new NotImplementedException();
    }

    public override string ToString() => Value;
}
```

---

## Parte 2 - `Program.cs` sección Email (no modificar)

```csharp
Console.WriteLine("--- Email ---");
var emails = new[]
{
    "usuario@example.com",
    "",
    "sin-arroba.com",
    new string('a', 255) + "@b.com",
};
foreach (var input in emails)
{
    var r = Email.Create(input);
    Console.WriteLine(r.IsSuccess ? $"Sí: {r.Value}" : $"No: {r.Error}");
}
```

---

## Parte 2 - `Program.cs` sección Username (no modificar)

```csharp
Console.WriteLine("--- Username ---");
var usernames = new[] { "rogelio_dev", "", "ab", "rogelio!" };
foreach (var input in usernames)
{
    var r = Username.Create(input);
    Console.WriteLine(r.IsSuccess ? $"Sí: {r.Value}" : $"No: {r.Error}");
}
```

---

## Parte 2 - Salida esperada y criterios

**Salida esperada:**

```
--- Email ---
Sí: usuario@example.com
No: Email es requerido.
No: Email inválido.
No: Email demasiado largo.
--- Username ---
Sí: rogelio_dev
No: Username es requerido.
No: Debe tener entre 3 y 20 caracteres.
No: Solo letras, dígitos y guión bajo.
```

**Criterios de aceptación:**
- [ ] `Create()` usa `Result<T>.OK()` / `Result<T>.Fail()` - ninguna excepción de negocio
- [ ] Constructores `private` - `new Email("x")` y `new Username("x")` no compilan
- [ ] Normalización a lowercase aplicada en ambos Value Objects
- [ ] La salida coincide línea por línea con la esperada
- [ ] El proyecto compila sin warnings

---

## Parte 3 - ADR-001 individual (30 min)

Crea `docs/adr/ADR-001-value-objects.md` con el formato MADR:

```markdown
# ADR-001 — [Tu decisión de diseño]

**Status:** Accepted
**Date:** [fecha]
**Deciders:** [tu rol — ej: "Desarrollador backend"]

## Contexto
[¿Qué problema o trade-off enfrentaste?
Describe la situación sin mencionar la solución.]

## Decisión
[¿Qué decidiste y por qué?
Menciona la alternativa que descartaste.]

## Consecuencias
[¿Qué ganas? ¿Qué pierdes?
Al menos 1 consecuencia positiva y 1 negativa.]
```

---

## Parte 3 - Ejemplos de decisiones válidas para el ADR

Documenta **una decisión real** que tomaste al implementar los Value Objects:

- Por qué `sealed record` y no `class` para representar un Value Object
- Por qué normalizar a lowercase en `Create()` y no en el llamador
- Por qué validar en orden: null -> formato -> longitud
- Por qué constructor `private` en lugar de `internal` o `protected`

**Criterios de aceptación - ADR:**
- [ ] Existe el archivo `docs/adr/ADR-001-value-objects.md`
- [ ] Tiene las tres secciones: Contexto, Decisión, Consecuencias
- [ ] La decisión documentada es real - no inventada ni copiada

---

## Criterios de aceptación globales

- [ ] `Email.cs` compilado y funcional
- [ ] `Username.cs` compilado y funcional
- [ ] `Program.cs` produce la salida esperada (pega el output en el PR)
- [ ] `ADR-001-value-objects.md` con las tres secciones completas

---

## Preparación para S03

S03 cubre **C# avanzado**: records, generics, async/await, pattern matching, collections.

**Prerrequisito obligatorio:** el `back-template` compilando localmente (`dotnet build` sin errores).

Si no lo tienes aún, clónalo antes de la siguiente sesión. Si encuentras algún problema al compilarlo, anótalo - lo revisamos al inicio de S03.
