<!-- _class: lead -->

# Módulo 3 - Autorización rota: BOLA, BOPLA y BFLA

### Estar autenticado no significa poder ver ese registro

**Rogelio Arriaga - Raptor Dev Services**

<!--
Este módulo estudia la falla más común en APIs: la autorización a nivel de objeto, de propiedad y de función. Se enseña a detectarla, corregirla y probarla con dos usuarios. No hay procedimientos de explotación: las pruebas son contra la API de práctica propia.
-->

---

## Agenda del módulo

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, alcance, marco legal y ético | 8 min |
| Parte 1 | API1:2023 BOLA: el objeto que no es tuyo | 26 min |
| Parte 2 | API3:2023 BOPLA: propiedades que se leen o se escriben de más | 22 min |
| Parte 3 | API5:2023 BFLA: la función que no es de tu rol | 20 min |
| Parte 4 | La matriz de autorización como prueba | 26 min |
| Parte 5 | Multi-tenant, bitácoras y el 404 frente al 403 | 10 min |
| Cierre | Práctica, tarea y quiz | 8 min |

<!-- Descanso de 10 minutos entre la Parte 2 y la Parte 3. -->

---

## El lema del módulo

> Estar autenticado no significa poder ver ese registro.

- Autenticación responde **quién eres**. Autorización responde **qué puedes hacer, con qué objeto y con qué datos**.
- Un token válido solo prueba lo primero.
- Hoy el criterio es una prueba con **dos usuarios**: si Ana puede leer el pedido de Beto, el sistema tiene un hallazgo, sin importar qué tan bien esté el login.

---

## Al terminar puedes

- Distinguir BOLA (objeto), BOPLA (propiedad) y BFLA (función) en un código y en un informe.
- Corregir cada una en ASP.NET Core: comprobación de dueño, DTO de entrada y de salida, política por rol.
- Escribir la prueba con dos usuarios que detecta la falla y verla fallar al quitar la corrección.
- Armar la matriz de autorización de una API y volverla una prueba automática.
- Decidir entre responder 403 o 404 y explicar el criterio.

---

## Alcance de este módulo

| Sí | No |
|---|---|
| Probar **tu** API con dos usuarios de prueba | Recorrer identificadores de sistemas ajenos |
| Escribir la prueba que detecta la falla | Herramientas o guiones para enumerar objetos de terceros |
| Corregir con código y comprobar que la prueba pasa | Procedimientos de explotación |
| Explicar por qué ocurre y cómo se ve en la bitácora | Datos reales de personas |

**Marco legal y ético.** Los dos usuarios del módulo (Ana y Beto) y sus pedidos son datos ficticios de una API de práctica. Cambiar un identificador para leer datos de otra persona en un sistema que no es tuyo es acceso no autorizado, aunque "solo cambie un número" (Código Penal Federal, arts. 211 bis 1 a 211 bis 7). Todo se hace con la carta de autorización del módulo 1 de Ciberseguridad.

---

<!-- _class: lead -->

# Parte 1
## API1:2023 BOLA

---

## Qué es

*Broken Object Level Authorization*: la API recibe un identificador de objeto (en la ruta, en la consulta, en el cuerpo) y **no comprueba que quien llama pueda usar ese objeto**.

```http
GET /api/pedidos/100      → mi pedido     → 200
GET /api/pedidos/200      → el de otra persona → 200 (¡no debería!)
```

OWASP lo describe así: cualquier endpoint que reciba un id de objeto y actúe sobre él debe verificar que el usuario autenticado **tiene acceso a ese objeto**. Comparar el id del usuario de la sesión con el parámetro **no basta** como regla general.

Es la falla más común y más fácil de explotar de las APIs: por eso es el **primer** lugar del Top 10 de 2023.

Fuente: OWASP, API1:2023 (api-security.owasp.org/editions/2023/en/0xa1-broken-object-level-authorization).

---

## Por qué ocurre

| Causa | Cómo se ve en el código |
|---|---|
| El servidor confía en el identificador del cliente | `db.Pedidos.Find(id)` sin mirar de quién es |
| El permiso se comprobó en la pantalla, no en el servidor | El botón "ver pedido" solo aparece en tus pedidos |
| Se asume que "nadie adivina el id" | Ids secuenciales o fáciles de inferir |
| Una ruta nueva copió el patrón de otra sin la comprobación | Ocurre en revisiones apuradas |
| Cada endpoint implementa su propia comprobación | Basta olvidar una vez |

La tercera causa es peligrosa: ocultar o aleatorizar los ids **no es una autorización**. El cheat sheet de autorización de OWASP lo dice así: ofuscar o aleatorizar identificadores no es suficiente; hay que verificar el acceso a cada objeto concreto.

---

## El código vulnerable (solo laboratorio)

```csharp
// Solo con Lab:Bola. VULNERABLE: busca el pedido por id y lo devuelve a cualquiera con sesión.
var pedido = datos.Pedidos.FirstOrDefault(p => p.Id == id);
if (pedido is null) return Results.NotFound();
return Results.Ok(new PedidoDto(pedido.Id, pedido.Total, pedido.Estado));
```

La autenticación **sí** ocurre: la ruta exige token válido. Lo que falta es la pregunta de autorización: **¿este pedido es de quien pregunta?**

Con la falla encendida, la prueba con dos usuarios da:

```text
usuario 1 leyo el pedido 200 (200)
usuario 2 leyo el pedido 100 (200)
usuario 2 leyo el pedido 101 (200)
```

Son los tres cruces posibles de los dos clientes. Una API correcta devuelve **cero**.

---

## La corrección

```csharp {1-2|3-4|*}
var pedido = datos.Pedidos.FirstOrDefault(p => p.Id == id);
// La pregunta correcta no es "esta autenticado" sino "este pedido es suyo". Se responde 404 en los dos casos.
if (pedido is null || (!opciones.Value.Bola && pedido.DuenoId != IdDe(yo))) return Results.NotFound();
return Results.Ok(new PedidoDto(pedido.Id, pedido.Total, pedido.Estado));
```

(El `!opciones.Value.Bola` existe solo porque la aplicación de práctica tiene el interruptor del laboratorio; sin él, es `pedido.DuenoId != IdDe(yo)`.)

Tres decisiones:

1. El **dueño** viene de la **sesión** (`sub` del token, ya validado), nunca de un campo del cuerpo ni de la ruta.
2. El pedido ajeno y el inexistente responden **igual** (404).
3. El listado (`GET /api/pedidos`) filtra por dueño en la consulta: `Where(p => p.DuenoId == IdDe(yo))`.

---

## Mejor: que la consulta ya sea del dueño

Comprobar después de leer es frágil: basta olvidar una vez. Mejor que el acceso a datos **ya** esté acotado al dueño:

```csharp
// Fragmento ilustrativo (no forma parte de la referencia): consulta acotada por dueño.
var pedido = await db.Pedidos
    .Where(p => p.Id == id && p.DuenoId == usuarioId)
    .SingleOrDefaultAsync(ct);
```

Con EF Core, un **filtro global de consulta** por dueño o por inquilino hace que todas las consultas del tipo nazcan acotadas (lee `04-backend/22-ef-core-multi-tenancy.md`). El cupón de la referencia ya lo hace así: busca `p.Id == id && p.DuenoId == IdDe(yo)`.

**Autorización por recurso** (fragmento): ASP.NET Core permite llamar a `IAuthorizationService.AuthorizeAsync(user, recurso, política)` con el objeto cargado, para que **una** clase decida quién puede qué sobre un pedido (`04-backend/27-rbac.md`).

---

## Ids aleatorios: ayudan, no autorizan

OWASP recomienda, entre las medidas de API1:2023, **preferir valores aleatorios e impredecibles como GUID** para los identificadores de registros, **y** escribir pruebas de autorización y no desplegar cambios que las hagan fallar.

| Medida | Qué logra | Qué no logra |
|---|---|---|
| Ids aleatorios (GUID) | Hacen más difícil recorrer objetos al azar | Si el id se filtra (un correo, una bitácora, una URL compartida), el objeto queda expuesto |
| Comprobación de dueño | Impide el acceso aunque el id se conozca | No impide que existan ids adivinables en tu base |

Las dos juntas. Si solo puedes elegir una, la **comprobación**.

Fuentes: OWASP, API1:2023 (prevención); OWASP, *Authorization Cheat Sheet*.

---

## Error típico: comprobar solo en las rutas de lectura

- `GET /api/pedidos/{id}` está protegida.
- `PATCH /api/pedidos/{id}` y `DELETE /api/pedidos/{id}` se escribieron después y nadie comprobó el dueño.
- El cupón, la factura, la exportación: cada ruta que recibe el id necesita su comprobación.

**Corrección:** una sola vía de acceso a datos acotada por dueño, usada por todas las rutas; **y** una prueba con dos usuarios por **cada** ruta que reciba un identificador de objeto (la matriz de la Parte 4).

Del módulo 1: las "tres rutas candidatas a BOLA" de tu tarea eran justo estas.

---

<!-- _class: lead -->

# Parte 2
## API3:2023 BOPLA

---

## Qué es

*Broken Object Property Level Authorization* (API3:2023) reúne dos categorías de la lista de 2019:

| Antes | Qué era | Ahora, en palabras de este módulo |
|---|---|---|
| Excessive Data Exposure | La respuesta devuelve propiedades que quien llama no debería ver | Se **lee** de más |
| Mass Assignment | El cliente puede modificar propiedades que no le tocan | Se **escribe** de más |

Una API puede tener BOLA bien resuelta (el objeto es tuyo) y aun así dejarte leer `hashContrasena` o escribir `rol`: eres el dueño del **objeto**, no de **todas sus propiedades**.

Fuente: OWASP, API3:2023 (api-security.owasp.org/editions/2023/en/0xa3-broken-object-property-level-authorization).

---

## Escribir de más: la entidad enlazada

```csharp
// Solo con Lab:Bopla. VULNERABLE: se enlaza la entidad completa y se copia todo lo que llegue.
var cambios = await peticion.ReadFromJsonAsync<Usuario>();
if (!string.IsNullOrWhiteSpace(cambios.Nombre)) u.Nombre = cambios.Nombre;
if (!string.IsNullOrWhiteSpace(cambios.Rol))    u.Rol = cambios.Rol;     // el cliente no debería poder escribir esto
```

`Usuario` tiene `Nombre`, pero también `Rol`, `HashContrasena` y `NotasInternas`. Si la entrada es la **entidad**, cualquier propiedad que la clase tenga se vuelve escribible por el cliente.

Detección con una prueba (en tu propia API): mandar un cambio **con una propiedad que no debería aceptarse** y comprobar después que no cambió:

```csharp
await ana.PatchAsync("/api/perfil", """{"nombre":"Ana M.","rol":"admin"}""");
var perfil = await (await ana.GetAsync("/api/perfil")).LeerAsync<PerfilDto>();
Assert.Equal("cliente", perfil!.Rol);
```

---

## La corrección: un DTO de entrada

```csharp
public sealed record PerfilCambio(string? Nombre);   // lo único que el cliente puede cambiar
```

```csharp {1-2|3-5}
var cambios = await peticion.ReadFromJsonAsync<PerfilCambio>();
if (cambios?.Nombre is not { Length: > 0 and <= 60 } nombre)
    return Results.Problem(statusCode: 400, title: "Nombre invalido");
u.Nombre = nombre;
```

- El DTO de entrada es una **lista de permitidos** de propiedades: lo que no está en el DTO se ignora.
- Cambiar el rol es **otra función** (de administración) con otra ruta y otra política.
- La entidad **no** se enlaza nunca a la entrada.

Con el DTO, la prueba `El_cliente_no_puede_cambiar_su_propio_rol` pasa: el `rol` enviado se descarta.

OWASP, API3:2023 (prevención): evitar el enlace automático de la entrada del cliente a objetos internos y permitir actualizar **solo** las propiedades que el cliente debe modificar.

---

## Leer de más: devolver la entidad

```csharp
// Solo con Lab:EntidadCompleta. VULNERABLE: la respuesta es la entidad completa.
return opciones.Value.EntidadCompleta ? Results.Ok(u) : Results.Ok(new PerfilDto(u.Id, u.Nombre, u.Rol));
```

Con la falla, la respuesta de `GET /api/perfil` incluye `hashContrasena` y `notasInternas`. Nadie lo "decidió": el serializador devuelve todas las propiedades públicas de la clase.

**Corrección:** un DTO de **salida** que enumera lo que se devuelve.

```csharp
public sealed record PerfilDto(int Id, string Nombre, string Rol);
```

Prueba (de contrato de respuesta):

```csharp
var cuerpo = await (await ana.GetAsync("/api/perfil")).TextAsync();
Assert.DoesNotContain("hashContrasena", cuerpo, StringComparison.OrdinalIgnoreCase);
Assert.DoesNotContain("notasInternas", cuerpo, StringComparison.OrdinalIgnoreCase);
```

---

## "Lo filtra el cliente" no es una defensa

Error frecuente: la API devuelve todo y la aplicación móvil o la pantalla **ocultan** lo que no se debe ver.

| Dónde se oculta | Qué pasa |
|---|---|
| En la pantalla | El dato **viajó** hasta el dispositivo: se ve en la respuesta |
| En el DTO del servidor | El dato **nunca sale** |

OWASP (API3:2023): confirmar que el usuario puede acceder a cada propiedad antes de exponerla, **seleccionar explícitamente** qué propiedades se devuelven (y no usar serialización genérica de objetos), validar respuestas contra un esquema y devolver el **mínimo** que el endpoint necesita.

---

## Dónde más se escribe de más

| Lugar | Riesgo | Corrección |
|---|---|---|
| `PATCH` de perfil | `rol`, `esAdmin`, `saldo` | DTO de entrada |
| Creación de pedido | `total`, `estado`, `duenoId` | El total lo calcula el servidor; el dueño sale de la sesión |
| Importaciones masivas | Campos internos en CSV o JSON | Lista de columnas permitidas |
| Respuestas de listados | Campos extra "porque el reporte los usaba" | DTO por endpoint |

Regla: **los campos que el servidor calcula o decide** (total, dueño, estado, rol) **no vienen del cliente**.

Libro: *Hacking APIs* (Ball, 2022), cap. 11 (*Mass Assignment*), y *Web Application Security* (Hoffman, 2.ª ed.), caps. 15 (*Attacking Data and Objects*) y 33 (*Defending Data and Objects*), tratan esta clase de falla desde ambos lados.

---

<!-- _class: lead -->

# Parte 3
## API5:2023 BFLA

---

## Qué es

*Broken Function Level Authorization*: una persona **usa una función** que no es de su rol, porque el servidor no comprueba su nivel de acceso en esa ruta. OWASP señala que las funciones de administración son el objetivo principal y que pueden estar en rutas "normales", por lo que **la URL por sí sola no dice quién debe usarlas**.

| | BOLA | BFLA |
|---|---|---|
| Qué se salta | El **objeto** (este pedido) | La **función** (administrar usuarios) |
| La pregunta | ¿Es tuyo este registro? | ¿Es tu rol para esta acción? |
| Ejemplo | `GET /pedidos/200` de otra persona | `GET /admin/usuarios` con un token de cliente |

También cuenta cambiar el **método**: una ruta que protege `GET` pero deja `DELETE` abierta.

Fuente: OWASP, API5:2023 (api-security.owasp.org/editions/2023/en/0xa5-broken-function-level-authorization).

---

## El código vulnerable y su corrección

```csharp {1-2|3|*}
var usuarios = api.MapGet("/admin/usuarios", (Almacen datos) =>
    datos.Usuarios.Select(u => new PerfilDto(u.Id, u.Nombre, u.Rol)));
if (!lab.Bfla) usuarios.RequireAuthorization("Admin"); // VULNERABLE si se omite: queda con la politica por omision (solo sesion)
```

```csharp
builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    o.AddPolicy("Admin", p => p.RequireRole("admin"));
});
```

Sin la política, la ruta queda con la `FallbackPolicy` (solo sesión): **cualquier cliente** puede llamarla. El módulo 1 ya mostró que sin esa política por omisión sería peor: ni siquiera pediría sesión.

La corrección es **declarativa**: `RequireAuthorization("Admin")` junto a la ruta, donde se ve en una revisión.

---

## Denegar por omisión, conceder por rol

OWASP (API5:2023, prevención): un módulo de autorización **consistente y centralizado** que **niega todo por omisión** y exige concesiones explícitas por rol; revisar cada endpoint contra la lógica de negocio y la jerarquía de grupos.

| Principio | En el código del curso |
|---|---|
| Denegar por omisión | `FallbackPolicy` exige sesión; las rutas de administración, además, política `Admin` |
| Conceder explícito | `RequireAuthorization("Admin")` en cada ruta de administración |
| Un solo lugar | Las políticas se declaran en `AddAuthorization`; las rutas solo las nombran |
| Revisable | La matriz de autorización (Parte 4) |

Para varias funciones, agrúpalas (fragmento): `api.MapGroup("/admin").RequireAuthorization("Admin")` pone la política **una vez** a todo el grupo y una ruta nueva en ese grupo nace protegida.

---

## Lo que el rol no resuelve

| Caso | Por qué el rol no basta |
|---|---|
| "Gerente de sucursal A" ve pedidos de la sucursal B | El rol dice **qué** puede hacer; falta **sobre qué objetos** (BOLA con alcance) |
| Soporte puede ver pedidos pero no cambiar el total | Permisos por **acción**, no solo por rol |
| El rol cambió hace una hora | El token viejo conserva el rol (módulo 2) |

Cheat sheet de autorización de OWASP: prefiere modelos basados en **atributos o relaciones** (ABAC/ReBAC) cuando las reglas son finas, contextuales o multi-tenant, y no te limites a RBAC. Lee `04-backend/27-rbac.md` para permisos finos.

Libros: *Pentesting APIs* (Harley, 2024), cap. 4 (*Exploring authorization mechanisms*: RBAC, ABAC, ámbitos OAuth); *Building Secure and Reliable Systems* (Adkins et al., 2020), cap. 5 (*Design for Least Privilege*); *Zero Trust Networks* (Rais et al., 2024), cap. 4 (*Making Authorization Decisions*).

---

<!-- _class: lead -->

# Parte 4
## La matriz de autorización

---

## Una tabla que se vuelve prueba

Una **matriz de autorización** cruza **quién** (sin sesión, cliente A, cliente B, administrador) con **qué llama** y escribe **lo que debe recibir**.

| Quién | Llamada | Debe recibir |
|---|---|---|
| Sin sesión | `GET /api/perfil` | 401 |
| Ana (cliente) | `GET /api/pedidos/100` (suyo) | 200 |
| Ana | `GET /api/pedidos/200` (de Beto) | 404 |
| Ana | `POST /api/pedidos/200/cupon` | 404 |
| Ana | `GET /api/admin/usuarios` | 403 |
| Beto | `GET /api/pedidos/100` (de Ana) | 404 |
| Administrador | `GET /api/admin/usuarios` | 200 |
| Administrador | `GET /api/pedidos/100` | 404 (decisión de la API de práctica) |

Son 19 filas en la referencia. Cada nueva ruta suma sus filas.

---

## La matriz en C#

```csharp
public static IEnumerable<object[]> Matriz() =>
[
    [0, "GET", "/api/perfil", 401],
    [1, "GET", "/api/pedidos/100", 200],
    [1, "GET", "/api/pedidos/200", 404],
    [1, "POST", "/api/pedidos/200/cupon", 404],
    [1, "GET", "/api/admin/usuarios", 403],
    [3, "GET", "/api/admin/usuarios", 200],
    // ...
];

[Theory]
[MemberData(nameof(Matriz))]
public async Task Matriz_de_autorizacion(int quien, string verbo, string ruta, int esperado)
{
    using var f = new Fabrica();
    using var cliente = quien == 0 ? f.CreateClient() : f.ComoUsuario(quien);
    var contenido = verbo == "POST" ? Ayuda.Json("""{"codigo":"PROMO10"}""") : null;
    var r = await cliente.SendAsync(new HttpRequestMessage(new HttpMethod(verbo), ruta) { Content = contenido });
    Assert.Equal(esperado, (int)r.StatusCode);
}
```

Un `[Theory]` por fila: si una cambia, la prueba **nombra** la fila rota.

---

## Qué detecta cada fila

| Fila que falla | Qué clase de falla |
|---|---|
| Ana recibe 200 en el pedido de Beto | BOLA (API1) |
| Ana recibe 200 en `/api/admin/usuarios` | BFLA (API5) |
| Sin sesión recibe 200 donde esperaba 401 | Autenticación o política por omisión (módulos 1 y 2) |
| Ana recibe 200 al cupón de un pedido ajeno | BOLA en una ruta de escritura |
| El administrador recibe 200 en pedidos ajenos | Decisión de negocio: ¿debe poder? Si no, es BFLA/BOLA |

La última fila es **una decisión que se escribe**: la matriz obliga a decidir y a dejarlo registrado. Eso vale tanto como la prueba.

---

## Verlo fallar: tres interruptores

| Interruptor del laboratorio | Qué reintroduce | Prueba que lo detecta |
|---|---|---|
| `Lab:Bola` | Sin comprobación de dueño | `Verlo_fallar_sin_la_comprobacion_de_dueno_la_prueba_lo_detecta` (cuenta 3 cruces) |
| `Lab:Bopla` | Entidad enlazada en la entrada | `Verlo_fallar_con_la_entidad_enlazada_el_rol_cambia` |
| `Lab:Bfla` | Ruta de administración sin política | `Verlo_fallar_sin_la_politica_el_cliente_entra_a_administracion` |

Las pruebas "verlo fallar" **encienden** el interruptor y comprueban que **la detección funciona**: demuestran que la prueba de verdad cae cuando la falla existe. Una prueba que nunca ha fallado no demuestra que protege.

Además de las pruebas "verlo fallar", la matriz cubre todos los cruces sin interruptor. Las dos se complementan.

---

## Cómo se vería en tus bitácoras

Señales a nivel conceptual de BOLA y BFLA:

| Señal | Qué sugiere |
|---|---|
| Una misma sesión pide muchos ids distintos del mismo tipo en poco tiempo, sobre todo en secuencia | Recorrido de objetos |
| Muchos 404 (o 403) a rutas de objetos desde una sesión | Alguien prueba ids que no son suyos |
| Un cliente llama a `/admin/*` | Intento de función ajena |
| El mismo identificador de objeto consultado por muchos usuarios distintos | Un id compartido o filtrado |

Qué registrar: **usuario (id), ruta, id de objeto y resultado**. No registres cuerpos con datos personales. Detección y respuesta son del curso `/blue-team/`.

Importante: **un 404 por BOLA y un 404 por "no existe" salen igual al cliente**, pero la bitácora **sí** puede distinguirlos: registra internamente la razón.

---

<!-- _class: lead -->

# Parte 5
## 404 o 403, y varios inquilinos

---

## 404 o 403

| Respuesta | Ventaja | Costo |
|---|---|---|
| **403 Forbidden** | Es honesta: "existe pero no es tuyo" | Confirma que el objeto **existe** |
| **404 Not Found** | No revela si el objeto existe | Menos claro para quien depura; hay que registrar la razón en la bitácora |

La API de práctica elige **404** para los objetos ajenos y **403** para las **funciones** que no son del rol: una función de administración **existe** para todos (está en la documentación); un objeto concreto, no tiene por qué saberse.

La prueba `Un_pedido_ajeno_y_uno_inexistente_responden_igual` fija la decisión. Es una **decisión de diseño**, no una regla del estándar: cualquiera de las dos es correcta si es **consistente** y está probada.

---

## Varios inquilinos (multi-tenant)

En un producto con varias empresas en la misma base, BOLA se vuelve **fuga entre clientes**: la empresa A ve datos de la empresa B.

- El identificador del inquilino sale de la **sesión**, nunca de un parámetro.
- Un filtro global por inquilino hace que toda consulta nazca acotada.
- La **prueba de fuga** crea dos inquilinos con datos y comprueba que ninguna ruta cruza.

Lee `04-backend/35-tenant-leakage-tests.md` y `04-backend/22-ef-core-multi-tenancy.md`: el patrón de la matriz de este módulo es el mismo, con inquilinos en vez de usuarios.

Libro: *Web Application Security* (Hoffman, 2.ª ed.), cap. 21 (*Secure Application Architecture*).

---

## Error típico: la prueba solo con el administrador

- La prueba de la ruta de pedidos usa el token del **administrador** "porque ve todo".
- Pasa en verde. Nadie la corre con un cliente.

**Corrección:** cada ruta se prueba como **mínimo** con: sin sesión, dueño, otro usuario del mismo rol y un rol superior. Si solo pruebas con quien tiene permiso, no pruebas la autorización: pruebas que la ruta funciona.

---

## Tu práctica de hoy

Con la API de referencia:

1. Correr la matriz de autorización y leer cada fila.
2. **Verlo fallar** con `Lab:Bola`, `Lab:Bopla`, `Lab:Bfla` y `Lab:EntidadCompleta`.
3. Agregar tú una ruta nueva (`DELETE /api/pedidos/{id}`) **con** su comprobación y sus filas de matriz.
4. Escribir la matriz de **tu** API (tabla de la tarea).
5. Decidir 404 o 403 para cada caso y justificarlo.

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas

- Autenticación dice quién eres; autorización dice qué puedes hacer, con qué objeto y con qué datos.
- **BOLA** (objeto): el dueño sale de la sesión, la consulta se acota por dueño. **BOPLA** (propiedad): DTO de entrada y de salida. **BFLA** (función): denegar por omisión y política por rol.
- Los ids aleatorios ayudan; la **comprobación** protege.
- Una **matriz de autorización** se vuelve una prueba con dos usuarios: es el contrato de autorización de la API.
- Una prueba que nunca ha fallado no demuestra nada: se la ve fallar quitando la corrección.

**Siguiente módulo:** Inyección, errores y límites.

---

## Referencias

- OWASP. *API1:2023 Broken Object Level Authorization*; *API3:2023 Broken Object Property Level Authorization*; *API5:2023 Broken Function Level Authorization*. api-security.owasp.org
- OWASP. *Authorization Cheat Sheet*. cheatsheetseries.owasp.org
- Microsoft. *Policy-based authorization in ASP.NET Core*. learn.microsoft.com
- *Hacking APIs* (Ball, 2022), caps. 10 y 11. *Pentesting APIs* (Harley, 2024), cap. 4. *Web Application Security* (Hoffman, 2.ª ed.), caps. 15, 21 y 33. *Bug Bounty Bootcamp* (Li, 2021), caps. 10 y 17. *Building Secure and Reliable Systems* (Adkins et al., 2020), cap. 5. *Zero Trust Networks* (Rais et al., 2024), cap. 4. *Identity-Native Infrastructure Access Management* (Kontsevoy et al., 2023), cap. 5.
