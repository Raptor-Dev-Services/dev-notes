# 15 — Strings en C#

Los strings son inmutables en C#. Cada operación que "modifica" un string crea uno nuevo. Conocer esto evita bugs y problemas de rendimiento.

---

## Declaración y valores especiales

```csharp
string nombre    = "Ana García";     // string normal
string vacio     = "";               // string vacío
string vacio2    = string.Empty;     // equivalente, más explícito
string? nullable = null;             // string que puede ser null

// Verificar vacío o null:
string.IsNullOrEmpty(nombre)         // true si null o ""
string.IsNullOrWhiteSpace(nombre)    // true si null, "" o solo espacios/tabs
```

---

## Interpolación — `$""`

La forma más común de construir strings dinámicos:

```csharp
var nombre = "Ana";
var edad   = 28;

// Interpolación — las expresiones van en { }
string mensaje = $"Hola, soy {nombre} y tengo {edad} años.";
// "Hola, soy Ana y tengo 28 años."

// Expresiones dentro de {}
string resultado = $"El área es {ancho * alto:F2} cm²";
// :F2 = formato con 2 decimales

// Llamar métodos
string superior = $"Usuario: {nombre.ToUpper()}";
// "Usuario: ANA"
```

### Formatos en interpolación

```csharp
decimal precio  = 9999.5m;
DateTime fecha  = DateTime.UtcNow;
Guid    id      = Guid.NewGuid();

$"{precio:C}"       // $9,999.50 (moneda — depende del locale)
$"{precio:F2}"      // 9999.50 (2 decimales fijos)
$"{precio:N0}"      // 9,999 (número con separador de miles, sin decimales)
$"{precio:0.00}"    // 9999.50 (formato personalizado)

$"{fecha:yyyy-MM-dd}"           // "2024-01-15"
$"{fecha:dd/MM/yyyy HH:mm}"     // "15/01/2024 14:30"
$"{fecha:O}"                    // ISO 8601 completo
$"{fecha:u}"                    // "2024-01-15 14:30:00Z"

$"{id:N}"           // "d4f7a2b1abc123..." (sin guiones)
$"{id:D}"           // "d4f7a2b1-abc1-23..." (con guiones, el default)
$"{id:B}"           // "{d4f7a2b1-abc1-23...}" (con llaves)
```

---

## Raw strings — `""" """`

C# 11+. Para strings multilínea sin escapes. Esencial para SQL en este proyecto.

```csharp
// Raw string — todo literal, sin escapes
var sql = """
    SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc
    FROM dbo.ExampleUsers
    WHERE PublicId = @publicId
      AND IsActive = true
    ORDER BY CreatedAtUtc DESC;
    """;

// El nivel de indentación del """ de cierre determina cuánta indentación se elimina
// El string resultante NO tiene los espacios de la indentación de código
```

### Raw string interpolado — `$""" """`

```csharp
var tabla = "dbo.ExampleUsers";
var sql = $"""
    SELECT *
    FROM {tabla}
    WHERE Id = @id;
    """;
// Interpolación funciona normalmente dentro del raw string
```

### Con llaves literales en raw string interpolado

```csharp
// Si necesitas { } literales en el resultado (ej. JSON manual):
var json = $$"""
    {
        "nombre": "{{nombre}}",
        "edad": {{edad}}
    }
    """;
// $$ = necesitas {{ }} para interpolar, { } son literales
```

---

## Verbatim strings — `@""`

Ignora secuencias de escape (`\n`, `\t`, etc.). Útil para rutas de Windows y regex.

```csharp
// Sin verbatim — necesitas escapar backslashes
string ruta = "C:\\Users\\Juan\\Documents\\archivo.txt";

// Con verbatim — los backslashes son literales
string ruta = @"C:\Users\Juan\Documents\archivo.txt";

// Regex — mucho más legible
string patron = @"^\d{3}-\d{4}$";  // sin verbatim: "^\\d{3}-\\d{4}$"

// Multilínea con verbatim
string multilinea = @"Línea 1
Línea 2
Línea 3";
// Incluye los saltos de línea literalmente
```

---

## Concatenación y StringBuilder

### Concatenación simple — para pocos strings

```csharp
string a = "Hola";
string b = "mundo";
string c = a + " " + b;  // crea UN nuevo string — está bien para pocos
```

### `StringBuilder` — para muchos strings en loop

Cuando concatenas en un loop, cada `+` crea un nuevo string. `StringBuilder` es un buffer mutable:

```csharp
// ❌ Ineficiente — crea miles de strings temporales
string resultado = "";
for (int i = 0; i < 10000; i++)
{
    resultado += $"Línea {i}\n";  // 10,000 strings temporales
}

// ✓ Eficiente — un solo buffer
var sb = new StringBuilder();
for (int i = 0; i < 10000; i++)
{
    sb.AppendLine($"Línea {i}");  // agrega al buffer
}
string resultado = sb.ToString();  // un solo string al final
```

---

## Operaciones comunes

```csharp
string s = "  Hola Mundo  ";

// Transformación
s.ToUpper()              // "  HOLA MUNDO  "
s.ToLower()              // "  hola mundo  "
s.Trim()                 // "Hola Mundo"
s.TrimStart()            // "Hola Mundo  "
s.TrimEnd()              // "  Hola Mundo"
s.Replace("Mundo", "C#") // "  Hola C#  "

// Búsqueda
s.Contains("Mundo")      // true
s.StartsWith("  Hola")   // true
s.EndsWith("Mundo  ")    // true
s.IndexOf("Mundo")       // 6 (índice donde empieza)

// Extracción
s.Substring(2, 4)        // "Hola" (empieza en índice 2, longitud 4)
s[2..6]                  // "Hola" — range syntax C# 8+
s[^5..]                  // "undo  " — desde el final

// División
"a,b,c".Split(',')       // ["a", "b", "c"]
"a, b, c".Split(", ")    // ["a", "b", "c"]

// Unión
string.Join(", ", lista) // "uno, dos, tres"
string.Join("\n", lineas)// unir con salto de línea

// Información
s.Length                 // 14
string.IsNullOrEmpty(s)  // false
```

---

## Comparación de strings

```csharp
// == compara el contenido (case-sensitive)
"Ana" == "Ana"           // true
"Ana" == "ana"           // false — diferente case

// Comparación explícita con opciones
string.Equals("Ana", "ana", StringComparison.OrdinalIgnoreCase)  // true — sin importar case
string.Equals("Ana", "ana", StringComparison.Ordinal)            // false — exacto

// Para ordenamiento (CompareTo):
"Ana".CompareTo("Luis")   // negativo — Ana va antes que Luis alfabéticamente
"Luis".CompareTo("Ana")   // positivo
"Ana".CompareTo("Ana")    // 0 — iguales

// Para diccionarios case-insensitive:
var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
config["ApiUrl"] = "https://...";
config["apiurl"]  // ← funciona aunque el case sea diferente
```

---

## Strings y encoding

```csharp
// Convertir string a bytes (para JWT, hashing, etc.)
byte[] bytes = Encoding.UTF8.GetBytes("mi clave secreta");

// Convertir bytes a string
string texto = Encoding.UTF8.GetString(bytes);

// Base64 (para tokens, archivos adjuntos)
string base64 = Convert.ToBase64String(bytes);
byte[] decoded = Convert.FromBase64String(base64);
```

---

## Range y Index — C# 8+

```csharp
string s = "Hola Mundo";

// Index desde el final
char ultimo = s[^1];      // 'o' — último carácter
char penultimo = s[^2];   // 'd'

// Range — substrings
string inicio = s[..4];   // "Hola" — desde el principio hasta índice 4
string fin    = s[5..];   // "Mundo" — desde índice 5 hasta el final
string medio  = s[2..7];  // "la Mu" — del índice 2 al 7
string últimos3 = s[^3..]; // "ndo" — últimos 3 caracteres
```

---

## Strings en el proyecto

```csharp
// Raw strings para SQL (sin escapes, legible)
private const string GetByPublicIdSql = """
    SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc
    FROM dbo.ExampleUsers
    WHERE PublicId = @publicId;
    """;

// Interpolación para logging (parámetros nombrados para Serilog)
_logger.LogInformation("Usuario {UserId} autenticado desde {Ip}", userId, clientIp);
// ❌ NO usar: _logger.LogInformation($"Usuario {userId} autenticado");
// La interpolación pierde la estructura — Serilog no puede indexar los valores

// ?? throw para configuración
var key = configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no configurado.");

// IsNullOrWhiteSpace para validar inputs
if (string.IsNullOrWhiteSpace(request.Email))
    return new RegistroValidationFailure("El email es requerido.");
```


---

*Rogelio Arriaga Gonzalez*
