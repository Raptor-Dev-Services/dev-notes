# Homework S03 - Módulo Contacts

**Dev Notes**

> Tiempo estimado: **3 horas**

---

## Parte 1 - Lecturas (1 hora)

### dev-notes a revisar

Abre los siguientes archivos de la base de conocimiento:

```
dev-notes/02-programacion/csharp/
├── 04-*   records — herencia de records y cuándo NO usar record
├── 08-*   async/await — errores comunes (async void, .Result, CancellationToken)
└── 19-*   configuration — IOptions<T> y ValidateOnStart
```

### Preguntas de reflexión (sin entregable - se verifican en code review)

Después de la lectura y el tour del back-template, responde mentalmente:

- ¿Por qué `GetUserProfileResponse` es `abstract record` y no `interface`?
- ¿Por qué `ISuccess<UserProfileDto>` en el Presenter usa la interfaz genérica
  y no el tipo concreto `GetUserProfileSuccess`?
- Si el Handler devolviera `null` en lugar de `GetUserProfileNotFoundFailure`,
  ¿qué fallaría en el Presenter?

> No hay entregable para las lecturas.
> La comprensión se verifica en el code review del PR.

---

## Parte 2 - Ejercicio de código: Módulo Contacts (2 horas)

### Objetivo

Implementar el caso de uso `CreateContact` siguiendo exactamente el patrón
del módulo `Users` del back-template.

El proyecto `S03-Homework-Problem/` está dado y **no compila tal como está**.
Tiene 13 TODOs intencionales que debes resolver en orden numérico.

### Setup

```bash
cd S03/S03-Homework-Problem
dotnet build   # verás errores — eso es correcto al clonar
```

Trabaja sobre los archivos existentes. No crees archivos nuevos.
El único archivo que no debes tocar es `Infrastructure/InMemoryContactRepository.cs`.

---

## Estructura del proyecto Problem

```
S03-Homework-Problem/
├── S03-Homework-Problem.csproj
├── Program.cs                              ← TODO 1-4
├── Domain/
│   ├── Contact.cs                          ← TODO 5-6
│   └── IContactRepository.cs              ← TODO 7
├── Application/
│   └── CreateContact/
│       ├── CreateContactRequest.cs         ← TODO 12
│       ├── CreateContactHandler.cs         ← TODO 13
│       └── Responses/
│           ├── CreateContactResponse.cs    ← TODO 9
│           ├── CreateContactSuccess.cs     ← TODO 10
│           └── CreateContactEmailConflictFailure.cs  ← TODO 11
├── Contracts/
│   └── ContactDto.cs                       ← TODO 8
├── Infrastructure/
│   └── InMemoryContactRepository.cs       ← DADO — no modificar
└── README.md
```

---

## El patrón que debes replicar

Compara cada archivo del homework con su equivalente en el back-template:

| Archivo del homework | Equivalente en back-template |
|---|---|
| `Domain/Contact.cs` | `Users.Domain/Entities/UserProfile.cs` |
| `Domain/IContactRepository.cs` | `Users.Domain/Repositories/IUserProfileRepository.cs` |
| `Contracts/ContactDto.cs` | `Users.Contracts/Dtos/UserProfileDto.cs` |
| `Application/.../CreateContactResponse.cs` | `GetUserProfileResponse.cs` |
| `Application/.../CreateContactSuccess.cs` | `GetUserProfileSuccess.cs` |
| `Application/.../CreateContactEmailConflictFailure.cs` | `GetUserProfileNotFoundFailure.cs` |
| `Application/.../CreateContactHandler.cs` | `GetUserProfileHandler.cs` |
| `Program.cs` | `Host.Api/Program.cs` (versión simplificada) |

---

## Interfaces simplificadas incluidas en el proyecto

El proyecto Problem incluye versiones simplificadas de las interfaces de `Common`
(sin necesidad de NuGet ni librería externa):

```csharp
// En CreateContactResponse.cs
public interface IResponse { }

// En CreateContactSuccess.cs
public interface ISuccess<T> { T Data { get; } }

// En CreateContactEmailConflictFailure.cs
public interface IFailure { string Message { get; } }
public interface IConflictFailure : IFailure { }
```

> Estas interfaces replican el contrato real de `Common/Common/Results/`.
> En el proyecto real del sistema de mensajería usarás las de la librería Common.

---

## Cómo probar cuando compile

Primero inicia el servidor:

```bash
dotnet run
```

Luego en otra terminal, prueba los dos escenarios:

**Escenario 1 - Contacto nuevo (debe devolver 201 Created):**

```bash
curl -X POST http://localhost:5000/contacts \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Ana García","email":"ana@example.com","phone":"+521234567890","tenantId":1}'
```

**Escenario 2 - Email duplicado en el mismo tenant (debe devolver 409 Conflict):**

```bash
curl -X POST http://localhost:5000/contacts \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Ana García 2","email":"ana@example.com","phone":"+521234567891","tenantId":1}'
```

---

## Criterio de aceptación

Para que el PR sea aprobado, **todos** los puntos deben estar en verde:

- [ ] `dotnet build` sin errores ni warnings
- [ ] `POST /contacts` con body válido -> 201 Created con el `ContactDto` en el body
- [ ] `POST /contacts` con email ya registrado en el mismo tenant -> 409 Conflict
- [ ] El Handler usa pattern matching sobre la respuesta del repositorio - no lanza excepciones para el flujo de negocio
- [ ] `IContactRepository` vive en `Domain/` - no en `Application/` ni `Infrastructure/`
- [ ] Ningún método usa `.Result` o `.Wait()`
- [ ] `CancellationToken` se pasa en todas las llamadas async

---

## Hint de arquitectura - el flujo completo

```
POST /contacts
  ↓
Program.cs — MapPost("/contacts")
  ↓ inyecta CreateContactHandler
  ↓ llama handler.Handle(request, ct)
  ↓
CreateContactHandler
  ↓ llama _contacts.GetByEmailAsync(email, tenantId, ct)
  ↓ si existe  → devuelve CreateContactEmailConflictFailure
  ↓ si no existe → llama _contacts.InsertAsync(...) → devuelve CreateContactSuccess
  ↓
Program.cs — pattern matching sobre la respuesta:
  ↓ CreateContactSuccess s         → Results.Created("/contacts/" + ..., s.Data)
  ↓ CreateContactEmailConflictFailure f → Results.Conflict(f.Message)
```

---

## Preparación para S04

S04 cubre los 10 patrones creacionales y estructurales (GoF).

**Prerrequisito conceptual antes de llegar:**

El `CreateContactHandler` que implementas hoy aplica implícitamente el patrón
**Strategy** cuando el repositorio decide cómo almacenar los datos.
En S04 verás por qué eso ya es un patrón de diseño.

También observa: ¿el `Program.cs` de este ejercicio actúa como una
**Factory**? ¿Quién decide qué implementación de `IContactRepository` se usa?
