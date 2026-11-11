# Práctica - M03 - Autorización rota: BOLA, BOPLA y BFLA

**Curso:** Seguridad de APIs: atacar y defender
**Duración:** 90 minutos, individual, en tu máquina.

> **Marco legal y ético.** Esta práctica se hace con código **propio**: la API de referencia del repositorio y, si quieres, tu propia API. Los usuarios Ana, Beto y Admin, sus pedidos y todo el contenido son ficticios. "Cambiar un número del identificador" en un sistema que no es tuyo es acceso no autorizado aunque no haya contraseña de por medio; aquí solo se hace dentro de las pruebas de tu código.

---

## Objetivo

1. Correr la matriz de autorización de la API de referencia y entender cada fila.
2. **Verlo fallar** cuatro veces, encendiendo los interruptores `Bola`, `Bopla`, `EntidadCompleta` y `Bfla`.
3. Agregar una ruta nueva con su comprobación y sus filas en la matriz.
4. Escribir la matriz de tu propia API.

Evidencia: las salidas de cada paso y la tabla del paso 5.

---

## Antes de empezar

```bash
cd dev-notes/cursos/api-security/referencia
dotnet test --filter "FullyQualifiedName~Modulo3Tests"
```

Sin SDK local, en el contenedor oficial (uno solo, se borra al terminar):

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test --nologo --filter "FullyQualifiedName~Modulo3Tests"
```

Todo lo de este documento **se ejecutó** al escribirlo (SDK de .NET 10). Con el repositorio tal cual, las 30 pruebas del módulo 3 pasan (11 de detección y comprobación y 19 filas de la matriz).

---

## Paso 1 - Leer la matriz

Abre `ApiSeguridad.Tests/Modulo3Tests.cs` y busca `Matriz()`. Son 19 filas con la forma `[quien, verbo, ruta, esperado]`:

| `quien` | Quién es |
|---|---|
| 0 | Sin sesión |
| 1 | Ana (cliente; dueña de los pedidos 100 y 101) |
| 2 | Beto (cliente; dueño del pedido 200) |
| 3 | Admin |

Responde por escrito:

1. ¿Cuáles filas comprueban **BOLA**? ¿Cuáles **BFLA**? ¿Hay alguna de **BOPLA** en la matriz? (No: BOPLA se prueba mirando propiedades, con las pruebas `El_cliente_no_puede_cambiar_su_propio_rol` y `El_perfil_no_devuelve_campos_internos`.)
2. ¿Por qué `Ana → POST /api/pedidos/200/cupon` espera **404** y no 403?
3. La fila `Admin → GET /api/pedidos/100` espera 404. ¿Es una decisión correcta? ¿Qué cambiarías si soporte debiera ver pedidos ajenos? (Una política de soporte **de solo lectura** y una fila nueva, no "dar admin a todos".)

---

## Paso 2 - Verlo fallar: BOLA

En `Modulo3Tests.cs`, la prueba `Verlo_fallar_sin_la_comprobacion_de_dueno_la_prueba_lo_detecta` ya construye la aplicación con el interruptor `Bola`. Léela:

```csharp
using var f = new Fabrica("Bola");
var hallazgos = await HallazgosBola(f);
Assert.Equal(3, hallazgos.Count);
```

Ahora quita el `new Fabrica("Bola")` y úsalo en la prueba **de verdad**. Cambia temporalmente `Nadie_ve_el_pedido_de_otro`:

```csharp
using var f = new Fabrica("Bola");     // antes: new Fabrica()
Assert.Empty(await HallazgosBola(f));
```

```bash
dotnet test --filter "FullyQualifiedName~Nadie_ve_el_pedido_de_otro"
```

Salida real:

```text
Assert.Empty() Failure: Collection was not empty
Collection: ["usuario 1 leyo el pedido 200 (200)", "usuario 2 leyo el pedido 100 (200)", "usuario 2 leyo el pedido 101 (200)"]
```

Responde: ¿cuántos cruces hay? ¿Por qué una API correcta devuelve una lista **vacía**? **Deshaz el cambio.**

Variante: en `Program.cs`, en la ruta `GET /pedidos/{id:int}`, cambia la comparación `pedido.DuenoId != IdDe(yo)` por `false` y corre la matriz completa: debe caer la fila `Matriz_de_autorizacion(quien: 1, verbo: "GET", ruta: "/api/pedidos/200", esperado: 404)`. Restaura.

---

## Paso 3 - Verlo fallar: BOPLA (escribir y leer de más)

### 3a. Escribir de más

La prueba:

```csharp
await ana.PatchAsync("/api/perfil", """{"nombre":"Ana M.","rol":"admin"}""");
var perfil = await (await ana.GetAsync("/api/perfil")).LeerAsync<PerfilDto>();
Assert.Equal("cliente", perfil!.Rol);
```

Cambia temporalmente `El_cliente_no_puede_cambiar_su_propio_rol` para usar `new Fabrica("Bopla")` y córrela:

```text
Assert.Equal() Failure: Strings differ
Expected: "cliente"
Actual:   "admin"
```

Esa es la falla: con la entidad enlazada, el cliente **cambió su propio rol** con un campo que la pantalla nunca le deja editar. **Restaura.**

### 3b. Leer de más

Cambia `El_perfil_no_devuelve_campos_internos` para usar `new Fabrica("EntidadCompleta")`:

```text
Assert.DoesNotContain() Failure: Sub-string found
Found:  "hashContrasena"
```

El mensaje de la prueba muestra además un trozo de la respuesta: la respuesta incluía el hash de la contraseña y las notas internas. Restaura.

Responde: ¿qué tienen en común las dos correcciones? (Un DTO: de entrada con lo que el cliente **puede** mandar, de salida con lo que el cliente **puede** ver.)

---

## Paso 4 - Verlo fallar: BFLA

Cambia temporalmente `Un_cliente_no_usa_funciones_de_administracion` para usar `new Fabrica("Bfla")`:

```text
Assert.Equal() Failure: Values differ
Expected: Forbidden
Actual:   OK
```

Con el interruptor, la ruta `/api/admin/usuarios` pierde su política `Admin`: **un cliente la usa**. Restaura.

Responde: ¿Qué línea de `Program.cs` hace la diferencia? (`usuarios.RequireAuthorization("Admin")`.) ¿Por qué es mejor declararla junto a la ruta que comprobarla dentro del manejador?

---

## Paso 5 - Una ruta nueva, con sus filas

Agrega a `Program.cs` esta ruta (borra un pedido propio). **Primero escribe las filas de la matriz** (como en TDD) y verás que fallan; después agrega la ruta.

```csharp
api.MapDelete("/pedidos/{id:int}", (int id, ClaimsPrincipal yo, Almacen datos) =>
{
    lock (datos.Candado)
    {
        var pedido = datos.Pedidos.FirstOrDefault(p => p.Id == id && p.DuenoId == IdDe(yo));
        if (pedido is null) return Results.NotFound();
        datos.Pedidos.Remove(pedido);
        return Results.NoContent();
    }
});
```

Filas nuevas en `Matriz()` (usa un pedido que no vayas a necesitar; recuerda que `Fabrica` arranca datos nuevos en cada fila):

| Quién | Llamada | Esperado |
|---|---|---|
| 0 | `DELETE /api/pedidos/100` | 401 |
| 2 | `DELETE /api/pedidos/100` | 404 |
| 1 | `DELETE /api/pedidos/100` | 204 |

Además, la prueba de inventario del módulo 1 te dirá que falta una línea en `rutas-documentadas.txt`: agrégala (`DELETE /api/pedidos/{id:int}`).

Pregunta: ¿qué pasaría si omites el `p.DuenoId == IdDe(yo)` en esa ruta? ¿Qué fila de la matriz lo detecta? (La de Beto, que debería recibir 404 y recibiría 204.) Es una ruta de **escritura**: peor que una de lectura.

---

## Paso 6 - La matriz de tu API

Para tu propia API, llena la matriz de las **cinco rutas más sensibles** (las que reciben un id de objeto o modifican datos):

| # | Ruta | Sin sesión | Dueño | Otro usuario del mismo rol | Rol superior | ¿404 o 403? |
|---|---|---|---|---|---|---|
| 1 | | 401 | 200 | | | |
| 2 | | 401 | | | | |
| 3 | | 401 | | | | |
| 4 | | 401 | | | | |
| 5 | | 401 | | | | |

Después escribe las filas como una `[Theory]` y córrelas contra **tu** API. Una fila que falle es un hallazgo: anótalo, corrígelo y vuelve a correr.

---

## Paso 7 - Limpieza

```bash
git checkout -- ApiSeguridad ApiSeguridad.Tests
docker ps -a      # no debe quedar ningún contenedor tuyo
```

---

## Entregable de la sesión

- La salida del paso 2 y tus respuestas.
- Las salidas de los pasos 3 y 4.
- La ruta nueva del paso 5 con sus filas de matriz y la prueba de inventario actualizada.
- La matriz del paso 6.

---

## Referencias

- OWASP. *API1:2023*, *API3:2023* y *API5:2023*. api-security.owasp.org
- OWASP. *Authorization Cheat Sheet*. cheatsheetseries.owasp.org
- Microsoft. *Policy-based authorization in ASP.NET Core*. learn.microsoft.com
- *Hacking APIs* (Ball, 2022), caps. 10 y 11. *Pentesting APIs* (Harley, 2024), cap. 4.
