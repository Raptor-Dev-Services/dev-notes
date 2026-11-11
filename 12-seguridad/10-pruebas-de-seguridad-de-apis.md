# 10 - Pruebas de seguridad de APIs

> Lectura del curso [Seguridad de APIs: atacar y defender](../cursos/api-security/README.md), módulo 6.

## Por qué importa

Corregir una vulnerabilidad es la mitad del trabajo. La otra mitad es asegurarse de que no regrese: el siguiente refactor, la siguiente migración o la siguiente persona que toque el archivo pueden deshacer el arreglo sin que nadie lo note. Una **prueba de seguridad automatizada** convierte "lo arreglamos" en "no puede volver sin que algo se ponga en rojo".

Esta nota explica qué probar por cada clase de falla de una API, cómo escribir la prueba de modo que se pueda ver fallar, cómo correrla en integración continua y cómo reportar lo que se encuentra. Los fragmentos de C# salen del código de referencia del curso (`cursos/api-security/referencia`), que compila con .NET 10 y cuyas 97 pruebas pasaron en noviembre de 2026.

> **Alcance.** Todo es metodología y defensa: cada clase se explica, se detecta, se corrige y se verifica. No hay procedimientos de explotación ni cargas de ataque, y las pruebas se escriben contra **tu propia API**, con datos de ejemplo.

## La forma de una prueba de seguridad

Una prueba funcional comprueba que algo **funciona**. Una prueba de seguridad comprueba que algo **no** funciona cuando no debe: el segundo usuario no ve los datos del primero, el token vencido no entra, el cuerpo enorme se rechaza.

Tres reglas de estilo:

1. **Dos usuarios o un usuario sin permiso**, no uno solo con todos los permisos.
2. **Nombre que dice lo que no debe pasar:** `Nadie_ve_el_pedido_de_otro`, `Un_token_vencido_no_entra`.
3. **Una aplicación nueva por prueba**, para que el estado (contadores de límite, datos) no se arrastre de una a otra.

El código de referencia arranca la API en memoria con `WebApplicationFactory<Program>` y le inyecta usuarios y tokens de ejemplo. Más sobre pruebas de integración: `04-backend/testing/03-integration-tests.md`.

## Qué probar, clase por clase

Las clases son las del OWASP API Security Top 10, edición 2023 (OWASP, 2023).

| Clase | Qué se prueba | Cómo se ve la prueba |
|---|---|---|
| API1 Broken Object Level Authorization | Cada usuario solo alcanza sus objetos | Dos usuarios; el segundo pide un objeto del primero y debe recibir 404 |
| API2 Broken Authentication | La API rechaza tokens que no debe aceptar | Token vencido, de otra audiencia, de otro emisor, de otra clave, sin firma y sin expiración |
| API3 Broken Object Property Level Authorization | No se escriben ni se leen propiedades ajenas | El cliente envía una propiedad que no le toca y se comprueba que no cambió |
| API4 Unrestricted Resource Consumption | Topes de página, de cuerpo y de tasa | Pedir `limite=100000` devuelve como máximo el tope; un cuerpo de 70 000 caracteres da 413 |
| API5 Broken Function Level Authorization | Cada función exige su rol | Un cliente llama la función de administración y recibe 403 |
| API6 Unrestricted Access to Sensitive Business Flows | Un flujo sensible no se repite | Veinte peticiones simultáneas de un cupón: una sola tiene éxito |
| API7 Server Side Request Forgery | Solo se visitan destinos permitidos | Destinos con otro esquema, otro puerto, otro host o dirección numérica se rechazan |
| API8 Security Misconfiguration | Cabeceras, CORS, clave mínima | Se leen las cabeceras de una respuesta y se comparan con el valor esperado |
| API9 Improper Inventory Management | La API expone lo que documenta | Las rutas reales se comparan con un contrato escrito |
| API10 Unsafe Consumption of APIs | Lo que devuelve un tercero se valida | Respuesta fuera de tamaño o de formato se rechaza (prueba del lado de tu cliente HTTP) |

## Ejemplo 1: objetos de otro usuario (API1)

La idea es un **cruce** de usuarios y objetos. Se define de quién es cada pedido y se prueba cada combinación ajena:

```csharp
// Pedidos de ejemplo: 100 y 101 son de Ana (1); 200 es de Beto (2).
private static readonly (int Usuario, int Pedido)[] Ajenos = [(1, 200), (2, 100), (2, 101)];

public static async Task<List<string>> HallazgosBola(Fabrica f)
{
    var hallazgos = new List<string>();
    foreach (var (usuario, pedido) in Ajenos)
    {
        using var c = f.ComoUsuario(usuario);
        var r = await c.GetAsync($"/api/pedidos/{pedido}");
        if (r.StatusCode != HttpStatusCode.NotFound) hallazgos.Add($"usuario {usuario} leyo el pedido {pedido} ({(int)r.StatusCode})");
    }
    return hallazgos;
}
```

La prueba `Nadie_ve_el_pedido_de_otro` exige que la lista de hallazgos esté vacía. Devolver una **lista** en vez de un booleano tiene una ventaja: si falla, el mensaje dice qué cruce falló.

Se responde 404 y no 403 para que un pedido ajeno y uno inexistente sean indistinguibles; otra prueba lo comprueba (`Un_pedido_ajeno_y_uno_inexistente_responden_igual`). Es una decisión de diseño del código de referencia, no una regla universal.

## Ejemplo 2: tokens que no deben entrar (API2)

La prueba crea tokens que la API **no** debe aceptar y exige 401:

```csharp
[Fact]
public async Task Un_token_para_otra_audiencia_no_entra()
{
    using var f = new Fabrica();
    Assert.Equal(HttpStatusCode.Unauthorized, await Perfil(f, Tokens.Crear(audiencia: "otra-api")));
}
```

El ayudante `Tokens.Crear` construye un token como lo haría el emisor real, salvo el detalle que la prueba cambia. Detalles que aparecieron al escribir estas pruebas:

- El manejador de tokens añade por omisión una expiración al crear un token. Para probar "sin expiración" hubo que apagar `SetDefaultTimesOnTokenCreation`; antes de eso la prueba comprobaba otra cosa.
- Para HS512 la biblioteca exige una clave de 512 bits, así que esa prueba configura también la API con una clave de 64 bytes.
- La clave mínima para HS256 es de 256 bits (IETF, 2015, sección 3.2), y la aplicación se niega a arrancar con una más corta (`ValidateOnStart`).

## Ejemplo 3: la entrada que rompe la consulta (inyección)

No hace falta una carga de ataque para detectar una consulta armada con texto: basta un **apóstrofe ordinario**, como el de un apellido.

```csharp
var r = await c.GetAsync("/api/productos?nombre=O'Brien");
Assert.Equal(HttpStatusCode.OK, r.StatusCode);
Assert.Contains("Cuaderno O'Brien", await r.TextAsync());
```

Si la consulta concatena el texto, la sentencia queda mal formada y la API responde 500. Con parámetros, el apóstrofe es solo un carácter. La prueba hermana enciende el interruptor de laboratorio `SqlConcatenado` y exige el 500: es la evidencia de que la prueba detecta el defecto.

## Ejemplo 4: límites (API4)

```csharp
var r = await c.PostAsync("/api/auth/login", Ayuda.Json("{\"usuario\":\"" + new string('a', 70_000) + "\"}"));
Assert.Equal(HttpStatusCode.RequestEntityTooLarge, r.StatusCode);
```

Y para la tasa, el sexto intento de login del mismo cliente en un minuto debe recibir 429. Esa prueba usa el limitador de tasa de ASP.NET Core, cuya documentación recomienda someter a pruebas de carga las políticas antes de desplegarlas y aclara que los contadores se guardan en memoria de cada instancia (Microsoft, 2026).

## Ejemplo 5: el inventario como prueba (API9)

```csharp
foreach (var fuente in fabrica.Services.GetServices<EndpointDataSource>())
    foreach (var e in fuente.Endpoints.OfType<RouteEndpoint>())
    {
        var verbos = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
        foreach (var v in verbos.Where(v => v != "OPTIONS"))
        {
            reales.Add(Llave(e, v));
            if (e.Metadata.GetMetadata<IAllowAnonymous>() is not null) anonimas.Add(Llave(e, v));
        }
    }
```

La prueba exige que las rutas reales sean **exactamente** las del archivo `rutas-documentadas.txt` y que la única ruta pública sea el login. Una ruta nueva sin documentar rompe la prueba y obliga a decidir, en la revisión, si debe existir.

## Cómo escribir la prueba que falla antes del arreglo

El orden importa:

1. **Escribe la prueba** que describe el comportamiento correcto.
2. **Córrela contra el código con el defecto.** Debe fallar, y el mensaje debe ser el de la falla que esperas.
3. **Corrige.**
4. **Córrela otra vez.** Debe pasar.
5. **Quita el arreglo** un momento y confirma que la prueba vuelve a caer; restáuralo.

El paso 5 es el que casi nadie hace y el que más valor aporta. En el código de referencia se hizo con seis controles: quitar `ValidAlgorithms`, desactivar la validación de audiencia, dejar de exigir expiración, dejar de exigir firma, quitar el límite del login y quitar la política de autorización por omisión. En cada caso cayeron las pruebas de ese control y no otras (en el último, varias a la vez, porque casi todas suponen sesión obligatoria).

## Cómo se corren en integración continua

Se marcan con un rasgo y se filtran:

```csharp
[Trait("Categoria", "Seguridad")]
public class Modulo3Tests { /* ... */ }
```

```bash
dotnet test --filter "Categoria=Seguridad"
```

En el flujo de integración continua, esta orden va como un paso propio y, en la configuración del repositorio, el trabajo se marca como verificación obligatoria para integrar. Reglas de higiene:

- Solo datos y credenciales **de ejemplo**.
- Sin dependencias de internet ni de terceros reales.
- Sin escaneos activos contra entornos compartidos.

Para el ciclo de desarrollo seguro en conjunto, véanse *Building Secure and Reliable Systems* (Adkins et al., 2020), caps. 12 a 14; *Software Supply Chain Security* (Crossley, 2024), caps. 4 a 6; y *Executive's Cybersecurity Program Handbook* (Brown, 2023), caps. 11 y 12.

## Cuándo se escriben pruebas nuevas

Cuando cambia algo relevante se repite el modelado de amenazas, que es una práctica continua más que un documento (Tarandach y Coles, 2020, caps. 1, 2 y 5): un endpoint o contrato nuevo, un flujo sensible nuevo, un tercero nuevo o una forma distinta de autenticar. Cada cambio produce **al menos una prueba nueva**.

## Cómo se reporta

Cada hallazgo lleva una ficha con los mismos campos: título, clase del Top 10, dónde, evidencia mínima, impacto, corrección, prueba y prioridad.

> **Ejemplo ilustrativo (API ficticia).** Título: un cliente puede leer el pedido de otro. Clase: API1:2023. Dónde: `GET /api/pedidos/{id}`. Evidencia mínima: con la sesión del usuario 1, un pedido del usuario 2 devuelve 200 en lugar de 404. Impacto: cualquier cliente autenticado lee pedidos ajenos. Corrección: comparar el dueño con el usuario de la sesión. Prueba: `Nadie_ve_el_pedido_de_otro`. Prioridad: alta.

La prioridad se razona con una escala cualitativa (alta, media, baja) y se explica; si se usa un sistema formal de puntaje se cita su versión y se calcula con la herramienta oficial. Como lista de verificación del conjunto se puede usar el ASVS, cuya versión estable es la 5.0.0, publicada el 30 de mayo de 2025, a noviembre de 2026 (OWASP, 2025).

### Si el fallo está en un sistema ajeno

No se sigue probando. Se busca el canal de reporte del dueño, se reporta con lo mínimo para reproducir y sin datos de personas, y se da tiempo para corregir antes de hablar en público. Sobre el proceso de comunicar hallazgos y casos reales, *The Vulnerability Researcher's Handbook* (Strout, 2023), caps. 4 y 8.

## Errores frecuentes

| Error | Consecuencia |
|---|---|
| Probar solo el camino feliz | La prueba también pasa en una API sin controles |
| No ver fallar la prueba | Puede estar vigilando otra cosa |
| Reutilizar una aplicación entre pruebas | Estado compartido, pruebas que dependen del orden |
| Pruebas contra un entorno compartido o real | Riesgo para otros y pruebas no repetibles |
| Informe sin prueba | El hallazgo no se puede cerrar |

## Glosario

| Término | Definición |
|---|---|
| Camino feliz | Prueba con datos válidos y permiso; necesaria, no suficiente |
| Prueba hermana | Prueba que enciende un interruptor de laboratorio para demostrar que el detector detecta |
| Rojo, verde | Falla antes del arreglo y pasa después |
| Mutación | Quitar un control a propósito para ver caer su prueba |
| Trait | Etiqueta de xUnit para filtrar pruebas |
| Evidencia mínima | Lo estrictamente necesario para mostrar el problema, sin datos reales |
| ASVS | Estándar de verificación de seguridad de aplicaciones de OWASP |

## Referencias

- Adkins, H. et al. (2020). *Building secure and reliable systems*. Caps. 12 a 14.
- Brown, J. (2023). *Executive's cybersecurity program handbook*. Caps. 11 y 12.
- Crossley, C. (2024). *Software supply chain security*. Caps. 4 a 6.
- Harley, M. (2024). *Pentesting APIs*. Cap. 10.
- Hoffman, A. (2023). *Web application security* (2.ª ed.). Caps. 20 a 22, 24 y 25.
- Internet Engineering Task Force. (2015). *JSON Web Algorithms (JWA)* (RFC 7518). https://www.rfc-editor.org/rfc/rfc7518
- Microsoft. (2026). *Rate limiting middleware in ASP.NET Core*. https://learn.microsoft.com/aspnet/core/performance/rate-limit
- OWASP. (2023). *OWASP API Security Top 10, 2023 edition*. https://api-security.owasp.org/editions/2023/en/0x11-t10
- OWASP. (2025). *Application Security Verification Standard 5.0.0*. https://asvs.owasp.org/
- Strout, B. (2023). *The vulnerability researcher's handbook*. Caps. 4 y 8.
- Tarandach, I. y Coles, M. J. (2020). *Threat modeling*. Caps. 1, 2 y 5.

---

*Rogelio Arriaga Gonzalez*
