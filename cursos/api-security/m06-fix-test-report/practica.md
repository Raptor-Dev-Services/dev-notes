# Práctica - M06 - Corregir, probar y reportar

**Curso:** Seguridad de APIs: atacar y defender
**Duración:** 90 minutos, individual, en tu máquina.

> **Marco legal y ético.** Esta práctica se hace sobre el **código de referencia del curso** (`cursos/api-security/referencia`) y sobre una API que tú escribiste. Romper un control para ver caer su prueba se hace **solo** en tu copia local del código de referencia. No se prueba ningún sistema ajeno, con o sin permiso verbal, y la evidencia nunca incluye datos reales de personas.

---

## Objetivo

1. Correr las pruebas de seguridad del código de referencia y entender qué vigila cada una.
2. **Verlo fallar:** quitar un control, ver caer su prueba, leer el mensaje y restaurarlo.
3. Escribir la ficha de un hallazgo con su prueba.
4. Dejar listo el flujo de CI que corre las pruebas de seguridad.

Evidencia: la salida de los pasos 1, 3 y 4, y la ficha del paso 6.

---

## Antes de empezar

Necesitas el SDK de .NET 10 (el código de referencia usa `net10.0`) y una copia del repositorio `dev-notes`.

```bash
dotnet --version                        # debe empezar con 10.
cd dev-notes/cursos/api-security/referencia
```

Las pruebas y las mutaciones de esta práctica **se ejecutaron** sobre ese código al escribirla, con .NET 10 dentro de un contenedor desechable y con los paquetes NuGet de la solución. El fragmento de CI (paso 7) es ilustrativo.

---

## Paso 1 - Correr las pruebas de seguridad

```bash
dotnet test --filter "Categoria=Seguridad"
```

Resultado obtenido al escribir la práctica (97 pruebas, todas en verde):

```text
Passed!  - Failed:     0, Passed:    97, Skipped:     0, Total:    97
```

Tu duración será distinta; lo que importa es `Failed: 0` y `Total: 97`.

Si te falla alguna antes de tocar nada, revisa que la versión del SDK sea la 10 y que el restablecimiento de paquetes (`dotnet restore`) haya terminado.

---

## Paso 2 - Leer una prueba antes de romperla

Abre `ApiSeguridad.Tests/Modulo2Tests.cs` y localiza `Un_token_para_otra_audiencia_no_entra`:

```csharp
[Fact]
public async Task Un_token_para_otra_audiencia_no_entra()
{
    using var f = new Fabrica();
    Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, Tokens.Crear(audiencia: "otra-api")));
}
```

Contesta por escrito:

1. ¿Qué cambia respecto a un token válido? (solo la audiencia)
2. ¿Qué línea de `ApiSeguridad/Autenticacion.cs` esperas que sea la responsable del rechazo?

---

## Paso 3 - Verlo fallar: quitar el control

En `ApiSeguridad/Autenticacion.cs`, dentro de `ConfigurarJwtBearer`, cambia:

```csharp
ValidateAudience = true,
```

por:

```csharp
ValidateAudience = false,
```

Corre la suite:

```bash
dotnet test --filter "Categoria=Seguridad"
```

Resultado obtenido al hacerlo: cae **una** prueba, `Un_token_para_otra_audiencia_no_entra`, con el estado `Failed`, y las otras 67 siguen en verde.

Lee el mensaje del fallo: debe decir que se esperaba `Unauthorized` y se obtuvo `OK`. Si cae por otro motivo, no vigila lo que creías (ver "La prueba que prueba otra cosa" en la clase).

---

## Paso 4 - Restaurar y confirmar

Devuelve `ValidateAudience = true` y corre la suite otra vez:

```bash
dotnet test --filter "Categoria=Seguridad"
```

Debe volver a `Failed: 0`. Anota la secuencia: **verde, rojo, verde**. Esa secuencia es la evidencia de que la prueba funciona.

---

## Paso 5 - Más mutaciones, una por una

Repite los pasos 3 y 4 con cada fila. **Una a la vez; restaura antes de la siguiente.**

| Control que quitas | Archivo | Prueba que debe caer |
|---|---|---|
| Borrar la línea `ValidAlgorithms = [SecurityAlgorithms.HmacSha256],` | `Autenticacion.cs` | `Un_token_con_algoritmo_no_permitido_no_entra` |
| `RequireExpirationTime = true` a `false` | `Autenticacion.cs` | `Un_token_sin_expiracion_no_entra` |
| `RequireSignedTokens = true` a `false` | `Autenticacion.cs` | `Un_token_sin_firma_no_entra` |
| Quitar `.RequireRateLimiting("login")` | `Program.cs` | `El_sexto_intento_de_login_en_un_minuto_se_limita` |

Estas cuatro y la del paso 3 se ejecutaron al escribir la práctica; en cada una cayó la prueba de la tabla.

Una extra, para ver una caída **en cadena**: borra la línea `o.FallbackPolicy = ...` de `Program.cs`. Caen varias pruebas a la vez (por ejemplo `Sin_token_no_entra` y `Una_ruta_sin_sesion_responde_401_con_formato_de_problema`), porque casi todo el código supone que la sesión es obligatoria. Restaura.

---

## Paso 6 - La ficha de un hallazgo

Elige una clase del curso y escribe su ficha en `ficha-m06.md`, con estos campos: título, clase del Top 10 de APIs (id y nombre), dónde, evidencia mínima, impacto, corrección, prueba y prioridad.

Ejemplo de cómo se ve para el interruptor de laboratorio `Bfla` (**ejemplo ilustrativo**, API ficticia):

| Campo | Contenido |
|---|---|
| Título | Un cliente puede listar a todos los usuarios |
| Clase | API5:2023 Broken Function Level Authorization |
| Dónde | `GET /api/admin/usuarios` |
| Evidencia mínima | Con la sesión de un cliente, la respuesta es 200; debería ser 403 |
| Impacto | Cualquier cliente ve nombres y roles de todas las cuentas |
| Corrección | Declarar `RequireAuthorization("Admin")` en la ruta |
| Prueba | `Un_cliente_no_usa_funciones_de_administracion` |
| Prioridad | Alta: una función de otro rol con una sesión cualquiera |

Para ver el fallo real: arranca la prueba hermana `Verlo_fallar_sin_la_politica_el_cliente_entra_a_administracion`, que enciende el interruptor `Bfla` y comprueba que el cliente entra.

---

## Paso 7 - Llevarlo a CI

Agrega a tu repositorio un flujo que corra solo las pruebas de seguridad. **Fragmento ilustrativo**: las versiones de las acciones y de .NET son ilustrativas; revisa las vigentes.

```yaml
name: seguridad
on:
  pull_request:
    branches: [main]
jobs:
  pruebas-de-seguridad:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test --filter "Categoria=Seguridad"
```

Antes de empujarlo, valida que el YAML sea correcto con un parser (por ejemplo, `python3 -c "import yaml,sys; yaml.safe_load(open(sys.argv[1]))" seguridad.yml`). Para que **bloquee** el merge, marca el trabajo `pruebas-de-seguridad` como verificación obligatoria en la regla de protección de la rama.

---

## Paso 8 - Llevarlo a tu API

Para tu API propia, prepara la lista de pruebas que necesitas:

| Clase | Prueba que escribirás | ¿Ya existe? |
|---|---|---|
| API1 | Dos usuarios, un objeto del otro: 404 | |
| API2 | Token vencido, de otra audiencia, de otra clave y sin firma | |
| API4 | Tope de página y de cuerpo | |
| API5 | Función de otro rol: 403 | |
| API8 | Cabeceras y CORS | |
| API9 | Rutas expuestas contra rutas documentadas | |

Esta lista es el punto de partida del proyecto final.

---

## Verlo fallar (resumen)

Si hiciste todo, viste fallar:

- Cinco pruebas de autenticación y límites, una por mutación.
- Varias pruebas a la vez al quitar la política por omisión.
- La prueba hermana de `Bfla`, que demuestra que el detector funciona.

---

## Errores comunes

| Síntoma | Causa | Qué hacer |
|---|---|---|
| Quitas el control y la prueba sigue verde | La prueba no vigila ese control | Lee el mensaje; revisa que la prueba use el dato que cambia |
| Caen muchas pruebas con una mutación pequeña | El control es la base de las demás (`FallbackPolicy`) | Es esperable: confirma que son las de sesión |
| `dotnet test` no encuentra pruebas con el filtro | El rasgo `Categoria` no está en la clase | Agrega `[Trait("Categoria", "Seguridad")]` |
| La prueba depende del orden | Estado compartido entre pruebas | Una `Fabrica` nueva por prueba, como en el código de referencia |

---

## Entrega

- La salida del paso 1, el "rojo" del paso 3 (con su mensaje) y el "verde" del paso 4.
- `ficha-m06.md`.
- El flujo de CI del paso 7.
- La tabla del paso 8.
