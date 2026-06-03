# 16: Namespaces y using

Los namespaces organizan el código en espacios de nombres jerárquicos. Los `using` importan esos espacios.

> Fuente: *C# 13 and .NET 9: Modern Cross-Platform Development* (Mark J. Price). Ch.2 Speaking C#: Namespaces and Assemblies

---

## ¿Qué es un namespace?

Un namespace es un contenedor lógico para tipos (clases, records, interfaces, enums). Evita colisiones de nombres entre proyectos y comunica la ubicación del código.

```csharp
namespace Application.UseCases.ExampleUsers.GetExampleUser;
// ↑ El punto (.) denota jerarquía: Application → UseCases → ExampleUsers → GetExampleUser

public sealed class GetExampleUserHandler
    : IRequestHandler<GetExampleUserRequest, GetExampleUserResponse>
{
    // ...
}
```

Sin namespace, todos los tipos del mundo estarían en el mismo espacio global. Las colisiones serían inevitables.

---

## Convención de namespaces del proyecto

La regla es: **namespace = ruta de la carpeta**.

```
Proyecto:    Application
Carpeta:     UseCases/ExampleUsers/GetExampleUser/
Namespace:   Application.UseCases.ExampleUsers.GetExampleUser

Proyecto:    Infrastructure
Carpeta:     Persistence/SQLDB/Main/ExampleUsers/
Namespace:   Infrastructure.Persistence.SQLDB.Main.ExampleUsers

Proyecto:    WebApi
Carpeta:     EndPoints/ExampleUsers/Presenters/
Namespace:   WebApi.EndPoints.ExampleUsers.Presenters
```

---

## File-scoped namespace: C# 10+

La forma moderna. Un solo `namespace` al inicio del archivo, sin llaves. Todo el archivo pertenece a ese namespace.

```csharp
// File-scoped (recomendado — menos indentación)
namespace Application.UseCases.ExampleUsers.GetExampleUser;

public sealed class GetExampleUserHandler { }
public sealed record GetExampleUserRequest(Guid PublicId) : IRequest<GetExampleUserResponse>;
```

```csharp
// Forma antigua — con llaves (más indentación)
namespace Application.UseCases.ExampleUsers.GetExampleUser
{
    public sealed class GetExampleUserHandler { }
}
```

Este proyecto usa file-scoped. Menos indentación, más legible.

---

## `using`: importar namespaces

Sin `using`, necesitas el nombre completo cada vez:

```csharp
// Sin using — verbose
Application.UseCases.ExampleUsers.GetExampleUser.GetExampleUserHandler handler =
    new Application.UseCases.ExampleUsers.GetExampleUser.GetExampleUserHandler(repo);

// Con using — conciso
using Application.UseCases.ExampleUsers.GetExampleUser;

GetExampleUserHandler handler = new GetExampleUserHandler(repo);
```

### Ubicación de los `using`

Convención: al inicio del archivo, antes del namespace:

```csharp
// ✓ Orden recomendado (no obligatorio pero convencional):
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Messaging;
using Common.Results;
using Domain.Entities.ExampleUsers;
using Domain.Repositories.ExampleUsers;

namespace Application.UseCases.ExampleUsers.GetExampleUser;

public sealed class GetExampleUserHandler { }
```

---

## `global using`: importar para todo el proyecto

C# 10+. Un `global using` en un archivo aplica a **todos** los archivos del proyecto.

Convención: crear un archivo `Usings.cs` (o `GlobalUsings.cs`) con todos los global usings:

```csharp
// Application/Usings.cs
global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Common.Messaging;
global using Common.Results;
global using Domain.Entities.ExampleUsers;
global using Domain.Repositories.ExampleUsers;
// Estos using aplican a TODOS los .cs en el proyecto Application
```

**Ventaja:** no tienes que repetir `using System.Threading.Tasks;` en cada archivo.

**En el proyecto:** los namespaces más usados en cada capa se ponen en `global using` para no repetirlos.

---

## `using static`: importar miembros estáticos

Importa los métodos estáticos de una clase sin necesidad de calificarlos:

```csharp
using static System.Console;
using static System.Math;

WriteLine("Hola");         // en vez de: Console.WriteLine("Hola")
double r = Sqrt(16);       // en vez de: Math.Sqrt(16)
double a = PI * r * r;     // en vez de: Math.PI
```

---

## `using` aliases: renombrar tipos importados

```csharp
// Cuando dos namespaces tienen un tipo con el mismo nombre:
using DomainUser    = Domain.Entities.ExampleUsers.ExampleUser;
using IdentityUser  = Microsoft.AspNetCore.Identity.IdentityUser;

// Ahora puedes distinguirlos:
DomainUser   usuario     = new DomainUser();
IdentityUser identidad   = new IdentityUser();
```

---

## `using` para disposables: liberar recursos

Diferente del `using` de namespace. Este aplica a objetos `IDisposable`:

```csharp
// Sintaxis clásica — Dispose al salir del bloque
using (var connection = factory.OpenConnection())
{
    var result = await connection.QuerySingleAsync<ExampleUser>(sql, param);
    return result;
}
// ← connection.Dispose() llamado automáticamente

// Sintaxis moderna (C# 8+) — Dispose al salir del scope del método
using var connection = factory.OpenConnection();
var result = await connection.QuerySingleAsync<ExampleUser>(sql, param);
return result;
// ← connection.Dispose() llamado al retornar
```

---

## Cómo los namespaces siguen la Clean Architecture

```
Domain/
    Entities/ExampleUsers/         → namespace Domain.Entities.ExampleUsers
    Repositories/ExampleUsers/     → namespace Domain.Repositories.ExampleUsers

Application/
    Dto/ExampleUsers/              → namespace Application.Dto.ExampleUsers
    UseCases/ExampleUsers/         → namespace Application.UseCases.ExampleUsers...

Infrastructure/
    PostgreSql/                    → namespace Infrastructure.PostgreSql
    Persistence/SQLDB/Main/        → namespace Infrastructure.Persistence.SQLDB.Main...
    Repositories/ExampleUsers/     → namespace Infrastructure.Repositories.ExampleUsers

WebApi/
    Base/                          → namespace WebApi.Base
    EndPoints/ExampleUsers/        → namespace WebApi.EndPoints.ExampleUsers

Host/
    Extensions/                    → namespace Host.Extensions
    Services/                      → namespace Host.Services
```

La jerarquía del namespace refleja exactamente la jerarquía de carpetas. Si ves `Infrastructure.Repositories.ExampleUsers`, sabes exactamente dónde buscar el archivo.

---

## Errores comunes

### Error 1: namespace que no coincide con la carpeta

```csharp
// Archivo en: Infrastructure/Repositories/ExampleUsers/ExampleUserRepository.cs
// ❌ Namespace incorrecto — confunde al lector
namespace Infrastructure.Data.Users;

// ✓ Correcto — namespace = ruta
namespace Infrastructure.Repositories.ExampleUsers;
```

### Error 2: namespace demasiado genérico

```csharp
// ❌ "Utils" es un namespace basura — no dice nada
namespace Utils;
public static class DateHelper { }

// ✓ Descriptivo
namespace Application.Helpers;
public static class DateHelper { }
```

### Error 3: `using` dentro del namespace

```csharp
// ✓ El estándar — usings fuera del namespace (antes del namespace)
using System.Threading;
namespace Application.UseCases;

// Funciona pero no convencional:
namespace Application.UseCases
{
    using System.Threading;  // dentro del namespace
    class MiClase { }
}
```


---

## Glosario

| Término | Definición |
|---------|-----------|
| Namespace | Contenedor lógico que agrupa tipos relacionados bajo un nombre jerárquico para evitar colisiones y organizar el código |
| File-scoped namespace | Sintaxis de C# 10+ que declara el namespace una vez al inicio del archivo sin llaves: `namespace Mi.Namespace;` |
| `using` | Directiva que importa un namespace para no escribir el nombre completo de cada tipo: `using System.Collections.Generic;` |
| `global using` | Directiva que importa un namespace en todos los archivos del proyecto; se declara en un archivo dedicado |
| `using static` | Directiva que importa los miembros estáticos de una clase: `using static System.Math;` para usar `Abs()` directamente |
| Alias de tipo (`using X = ...`) | Asigna un alias a un tipo para evitar colisiones de nombres o acortar nombres largos |
| Ensamblado (assembly) | Unidad de compilación en .NET (`.dll` o `.exe`); un proyecto de C# compila a un ensamblado |
| Referencia de proyecto | Dependencia entre proyectos en la solución; los namespaces del proyecto referenciado quedan disponibles |
| Convención namespace = ruta | Regla del proyecto: el namespace debe reflejar la jerarquía de carpetas del proyecto |
| Colisión de nombres | Problema que los namespaces resuelven: dos tipos con el mismo nombre en proyectos distintos conviven usando su namespace completo |

---

*Rogelio Arriaga Gonzalez*
