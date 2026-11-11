# Práctica - M01 - Superficie e inventario

**Curso:** Seguridad de APIs: atacar y defender
**Duración:** 90 minutos, individual, en tu máquina.

> **Marco legal y ético.** Esta práctica se hace con código **propio** (la API de referencia del repositorio) y, de forma opcional, con crAPI, una aplicación hecha por OWASP para practicar, instalada en una máquina virtual tuya en red aislada (en la red solo anfitrión `192.168.56.0/24` de los cursos `/blue-team/` y `/pentesting-with-kali/`, si ya la tienes). No se prueba nada contra sistemas de otras personas, con o sin "permiso verbal". Las rutas, los usuarios y los datos de la API de referencia son ficticios.

---

## Objetivo

1. Correr las pruebas de seguridad de la API de referencia y leer el inventario de rutas que imprime.
2. Ver fallar la prueba de inventario cuando aparece una ruta que nadie documentó, y leer qué nombra el error.
3. Escribir el contrato de rutas y la prueba que lo compara, para una API tuya.
4. Armar la ficha de inventario de un entorno.

Evidencia: la salida de cada paso y la ficha del paso 5.

---

## Antes de empezar

Necesitas .NET SDK 10 (o Docker para correr las pruebas dentro del contenedor oficial del SDK), `git` y el repositorio `dev-notes`.

```bash
cd dev-notes/cursos/api-security/referencia
dotnet --version        # 10.x
ls                      # ApiSeguridad  ApiSeguridad.Tests  ApiSeguridad.slnx
```

Sin SDK local, el mismo comando dentro del contenedor del SDK (un contenedor, se borra al terminar):

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test --nologo
```

Los comandos de este documento **se ejecutaron** al escribirlo, en el contenedor `mcr.microsoft.com/dotnet/sdk:10.0` con .NET 10; las salidas citadas son las reales. Lo de crAPI (paso 6) **no se ejecutó**: sigue su guía oficial.

---

## Paso 1 - Correr las pruebas de seguridad

```bash
dotnet test --filter "Categoria=Seguridad"
```

Todas las pruebas del curso llevan `[Trait("Categoria", "Seguridad")]`, así que este filtro es el que se usará en CI (módulo 6). Con el repositorio tal como está, todas pasan: 97 pruebas (todos los módulos).

Para ver solo las del módulo 1:

```bash
dotnet test --filter "FullyQualifiedName~Modulo1Tests"
```

Debes ver cinco pruebas en verde. Anota cuáles son:

| Prueba | Qué comprueba |
|---|---|
| `La_API_expone_exactamente_lo_que_documenta` | Rutas reales = rutas del contrato |
| `Solo_el_login_es_publico` | Ninguna otra ruta permite acceso anónimo |
| `Verlo_fallar_una_ruta_olvidada_aparece_como_hallazgo` | La detección funciona: con la ruta olvidada encendida, la prueba la nombra |
| `Una_ruta_que_nadie_documento_no_responde_sin_sesion` | La política por omisión protege lo que no existe aún |
| `El_inventario_se_puede_listar` | Imprime el inventario (paso 2) |

---

## Paso 2 - Leer el inventario que la aplicación calcula de sí misma

```bash
dotnet test --filter "FullyQualifiedName~El_inventario_se_puede_listar" --logger "console;verbosity=detailed"
```

Entre el ruido del registro de la aplicación aparece esto (salida real):

```text
 GET    /api/admin/usuarios              politica Admin
 GET    /api/pedidos                     sesion (politica por omision)
 GET    /api/pedidos/{id:int}            sesion (politica por omision)
 GET    /api/perfil                      sesion (politica por omision)
 GET    /api/productos                   sesion (politica por omision)
 GET    /api/reportes/resumen            politica Admin
 PATCH  /api/perfil                      sesion (politica por omision)
 POST   /api/auth/login                  publica
 POST   /api/pedidos/{id:int}/cupon      sesion (politica por omision)
 POST   /api/vistas-previas              sesion (politica por omision)
```

Lee la prueba (`Modulo1Tests.cs`) y responde por escrito:

1. ¿De qué objeto de ASP.NET Core sale la lista de rutas?
2. ¿Qué diferencia hay entre `politica Admin` y `sesion (politica por omision)`?
3. ¿Cuál es la **única** ruta pública? ¿Por qué tiene que serlo?

Respuestas esperadas: `EndpointDataSource`; la primera exige además el rol `admin`, la segunda solo sesión (es la `FallbackPolicy`); el login, porque quien llama aún no tiene token.

---

## Paso 3 - Verlo fallar: una ruta que nadie documentó

La aplicación de práctica trae un interruptor del laboratorio, `Lab:RutaOlvidada`, que registra una ruta de depuración que **no** está en `rutas-documentadas.txt`.

Edita `ApiSeguridad.Tests/Modulo1Tests.cs` y, en `La_API_expone_exactamente_lo_que_documenta`, cambia:

```csharp
using var fabrica = new Fabrica();
```

por:

```csharp
using var fabrica = new Fabrica("RutaOlvidada");
```

```bash
dotnet test --filter "FullyQualifiedName~La_API_expone"
```

Salida real:

```text
  Failed ApiSeguridad.Tests.Modulo1Tests.La_API_expone_exactamente_lo_que_documenta
  Error Message:
   Assert.Empty() Failure: Collection was not empty
Collection: ["GET /api/v0/debug/estado"]
```

Contesta:

1. ¿Qué ruta nombra el error?
2. ¿Habrías encontrado esa ruta leyendo la documentación? ¿Y leyendo el código de los controladores a ojo?
3. ¿Qué dos cosas tiene la ruta que la hacen especialmente peligrosa? (Pista: mira `Solo_el_login_es_publico`.)

**Deshaz el cambio** (`git checkout -- ApiSeguridad.Tests/Modulo1Tests.cs`) y confirma que la prueba vuelve a pasar. Lo que acabas de hacer es el método de todo el curso: **romper a propósito, leer el error, arreglar**.

Una variante: en `Program.cs`, la línea `if (lab.RutaOlvidada) app.MapGet(...)` es la que registra la ruta. El interruptor lo lee `app.Configuration`; lo enciende la `Fabrica` de las pruebas. En una aplicación real **no existen** estos interruptores.

---

## Paso 4 - La política por omisión: verla caer

Abre `Program.cs` y comenta la línea de `FallbackPolicy`:

```csharp
builder.Services.AddAuthorization(o =>
{
    // o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    o.AddPolicy("Admin", p => p.RequireRole("admin"));
});
```

```bash
dotnet test --filter "Categoria=Seguridad"
```

Con esa línea comentada, en la medición hecha al escribir el curso fallaron, entre otras, `Sin_token_no_entra`, `Un_token_vencido_no_entra`, `Un_token_firmado_con_otra_clave_no_entra`, `Una_ruta_que_nadie_documento_no_responde_sin_sesion` y `Una_ruta_sin_sesion_responde_401_con_formato_de_problema`. Es una sola línea y cae media suite.

Responde: ¿por qué fallan pruebas de **tokens** (módulo 2) si lo que quitaste es una política de **autorización**? (Respuesta: sin la política por omisión, las rutas sin metadatos de autorización quedan abiertas: el token deja de importar porque no se exige.)

**Restaura la línea** y confirma que todo vuelve a verde.

---

## Paso 5 - El contrato y la ficha de tu API

### 5a. El contrato de rutas

En **tu** API (o, si no tienes una, en la de práctica del curso), crea un archivo `rutas-documentadas.txt` con una línea `VERBO /ruta` por cada ruta que **debe** existir. Copia de `Modulo1Tests.cs` el método `Comparar` y adáptalo (cambia la fábrica por la de tu proyecto).

Criterio: la prueba debe fallar cuando agregues una ruta nueva sin su línea.

### 5b. La ficha de inventario

Completa esta ficha para **un** entorno de tu API (producción, staging o el de tu laboratorio). Si un dato no lo sabes, escribe "no sé": es un hallazgo, no una omisión.

| Campo | Tu respuesta |
|---|---|
| Host y entorno | |
| Audiencia (quién debe llamar) | |
| Versión(es) expuestas | |
| Cómo se autentica | |
| Dónde está la documentación y quién la ve | |
| Terceros que reciben datos de esta API | |
| Dueño (persona o equipo) | |
| Fecha prevista de retiro (si es una versión vieja) | |

Regla de OWASP (API9:2023): las protecciones deben cubrir **todas** las versiones y entornos expuestos, no solo producción.

---

## Paso 6 (opcional) - crAPI: solo reconocimiento

> **No se ejecutó al escribir el curso.** Sigue la guía oficial del proyecto y registra la versión que instales.

1. En una máquina virtual tuya (red solo anfitrión), instala crAPI según el README de `github.com/OWASP/crAPI`: descargar el repositorio y, en `deploy/docker`, levantarlo con Docker Compose. La aplicación web queda en el puerto 8888 y el correo de prueba en el 8025.
2. Regístrate con un correo inventado y recorre la aplicación como un usuario normal.
3. Con las herramientas del navegador (pestaña Red), anota **las rutas que la propia aplicación web llama**: verbo, ruta y si lleva `Authorization`.
4. Compara tu lista con los retos de `docs/challenges.md` del proyecto: ¿qué clases del Top 10 aparecen? Une cada ruta con la clase que, según esa guía, se estudia ahí.
5. **No** pruebes nada más allá del reconocimiento. Los ataques a crAPI son de otros cursos y de los módulos 3 a 5 solo en su versión defensiva.

Entrega: la tabla de rutas observadas y su clase probable del Top 10, con la fuente (la guía del proyecto) de cada clase.

---

## Paso 7 - Limpieza

```bash
git checkout -- ApiSeguridad.Tests ApiSeguridad
docker ps -a          # no debe quedar ningún contenedor tuyo
```

Si levantaste crAPI, bájalo con el comando de su guía y no lo dejes encendido.

---

## Entregable de la sesión

- La salida del paso 2 y tus respuestas.
- La salida del paso 3 y tus respuestas.
- El archivo `rutas-documentadas.txt` y la prueba de tu API (paso 5a).
- La ficha de inventario (paso 5b).
- Opcional: la tabla de crAPI.

---

## Referencias

- Microsoft. *Policy-based authorization in ASP.NET Core*. learn.microsoft.com
- Microsoft. *Overview of OpenAPI support in ASP.NET Core API apps*. learn.microsoft.com
- OWASP. *API9:2023 Improper Inventory Management*. api-security.owasp.org
- OWASP crAPI. README y `docs/challenges.md`. github.com/OWASP/crAPI
- *Hacking APIs* (Ball, 2022), caps. 5 y 6. *Pentesting APIs* (Harley, 2024), caps. 2 y 3.
