# Práctica - M05 - Lógica de negocio, destinos y exposición de datos; GraphQL

**Curso:** Seguridad de APIs: atacar y defender
**Duración:** 90 minutos, individual, en tu computadora.

> **Marco legal y ético.** Toda la práctica obligatoria se hace sobre la aplicación de práctica del curso (`cursos/api-security/referencia/`), en tu máquina y con datos ficticios. Las partes opcionales con OWASP crAPI y con la aplicación GraphQL vulnerable DVGA se hacen **solo** en tu máquina virtual propia, en una red aislada (por ejemplo la `192.168.56.0/24` de los cursos `/blue-team/` y `/pentesting-with-kali/`, si los hiciste), y a nivel de reconocer y de escribir el control defensivo. Nada contra sistemas de terceros, de tu escuela o de tu trabajo sin autorización escrita; el marco legal está en `/cybersecurity/` y `/cyber-law/`.

---

## Objetivo

1. **Ver fallar** tres defensas quitándolas: la regla "un cupón por pedido", el contrato de respuesta y la lista de destinos.
2. Comprobar que 20 peticiones simultáneas son una prueba distinta de dos peticiones seguidas.
3. Escribir una prueba nueva y **validarla quitando el arreglo**.
4. Leer un esquema GraphQL con la lista de los cinco frentes y decir qué falta.

Evidencia: la salida de cada paso, en `m05-evidencia.md`.

---

## Antes de empezar

```bash
cd dev-notes/cursos/api-security/referencia
dotnet --version        # debe empezar con 10.
dotnet test --filter "Categoria=Seguridad&FullyQualifiedName~Modulo5"
```

Si no tienes el SDK, el mismo comando corre en un contenedor desechable: `docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test`.

Salida esperada:

```text
Passed!  - Failed:     0, Passed:    ..., Skipped:     0
```

El código de los pasos 1 a 7 se compiló y sus pruebas pasaron al escribir el módulo (.NET 10, xUnit 2.9.3). Los fragmentos de GraphQL **no** se compilaron y van marcados.

---

## Paso 1 - Leer las pruebas

Abre `ApiSeguridad.Tests/Modulo5Tests.cs` y anota, en una línea cada una, qué regla cuida:

| Prueba | Debe cuidar |
|---|---|
| `Un_cupon_se_aplica_una_sola_vez_por_pedido` | La regla "uno por pedido", con peticiones seguidas |
| `Veinte_peticiones_simultaneas_aplican_el_cupon_una_vez` | La misma regla, con peticiones simultáneas |
| `Verlo_fallar_con_el_cupon_repetible_el_total_baja_cada_vez` | Que la prueba **detecta** la falla |
| `El_cupon_de_un_pedido_ajeno_no_existe_para_mi` | Que el flujo respeta el dueño |
| `La_lista_de_destinos_rechaza` (varios casos) | Qué destinos no se aceptan |
| `Las_direcciones_internas_se_reconocen` (varios casos) | Los límites de las direcciones internas |
| `La_API_solo_visita_destinos_de_la_lista` | El endpoint usa la lista |

---

## Paso 2 - Ver fallar la regla del cupón

En `Un_cupon_se_aplica_una_sola_vez_por_pedido`, cambia la primera línea a `using var f = new Fabrica("CuponRepetible");` y corre solo esa prueba.

Salida que se obtuvo al escribir esta práctica:

```text
Failed ApiSeguridad.Tests.Modulo5Tests.Un_cupon_se_aplica_una_sola_vez_por_pedido
  Assert.Equal() Failure: Values differ
  Expected: Conflict
  Actual:   OK
```

Responde en tu evidencia:

1. ¿Qué regla de negocio se rompió y en qué línea de `Program.cs` vive? (Busca `CuponAplicado`.)
2. El total del pedido 100 tras las dos aplicaciones es 405 en lugar de 450 (la prueba `Verlo_fallar_con_el_cupon_repetible...` lo afirma). ¿Cuánto dinero perdería el negocio si el cupón fuera del 50 % y el pedido de 10 000?
3. ¿Qué parte de esto habría visto un escáner automático? (Ninguna: la respuesta de cada petición es un 200 bien formado.)

Restaura la línea.

---

## Paso 3 - Dos peticiones no son veinte

En `Veinte_peticiones_simultaneas_aplican_el_cupon_una_vez`, enciende también `CuponRepetible` y corre:

```text
Failed ApiSeguridad.Tests.Modulo5Tests.Veinte_peticiones_simultaneas_aplican_el_cupon_una_vez
  Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   20
```

Ahora el experimento **conceptual** sobre la atomicidad (solo leer, no ejecutar): abre la ruta del cupón en `Program.cs`.

1. Identifica el `lock (datos.Candado)`. ¿Qué dos pasos protege? (Revisar si ya se usó y marcarlo.)
2. Escribe en tu evidencia, en dos frases, qué pasaría con 20 peticiones simultáneas si el `lock` no existiera y la regla sí estuviera.
3. ¿Por qué un `lock` en memoria no basta con tres instancias de la API? ¿Qué lo reemplazaría en una base de datos? (Una restricción única o una actualización condicional con conteo de filas afectadas.)

Restaura la prueba.

---

## Paso 4 - Ver fallar el contrato de respuesta

La prueba del perfil está en `Modulo3Tests.cs`: `El_perfil_no_devuelve_campos_internos`. Cambia su primera línea a `using var f = new Fabrica("EntidadCompleta");` y corre.

```text
Failed ApiSeguridad.Tests.Modulo3Tests.El_perfil_no_devuelve_campos_internos
  Assert.DoesNotContain() Failure: Sub-string found
  String: ···"a","rol":"cliente","hashContrasena":"oM//"···
  Found:  "hashContrasena"
```

1. ¿Qué dos campos internos devuelve la entidad completa? (`hashContrasena` y `notasInternas`.)
2. El frontend nunca muestra esos campos. ¿Por qué el hallazgo existe igual? (Llegaron al cliente: están en el cuerpo que cualquiera puede leer.)
3. Agrega a `Modulo5Tests.cs` una prueba de contrato para otro endpoint, por ejemplo la lista de pedidos: `GET /api/pedidos` no debe contener `duenoId`. Escríbela, corre, y **valídala**: en `Program.cs`, quita la proyección `.Select(p => new PedidoDto(...))` de la lista (devuelve los pedidos tal cual) y corre otra vez. ¿Cae?

Restaura todo.

---

## Paso 5 - Ver fallar la lista de destinos

En `La_API_solo_visita_destinos_de_la_lista`, cambia la primera línea a `using var f = new Fabrica("SinListaDeDestinos");`:

```text
Failed ApiSeguridad.Tests.Modulo5Tests.La_API_solo_visita_destinos_de_la_lista
  Assert.Equal() Failure: Values differ
  Expected: UnprocessableEntity
  Actual:   Accepted
```

Responde: ¿qué puede alcanzar la API que el cliente no? (La red interna, el servicio de metadatos de la nube.) ¿Por qué eso convierte "acepté una URL cualquiera" en un hallazgo grave?

Restaura la línea.

---

## Paso 6 - Una prueba nueva, validada

Agrega esta prueba (se compiló y pasa):

```csharp
[Fact]
public void Un_host_que_solo_empieza_igual_no_esta_permitido()
{
    Assert.False(Destinos.EsPermitido("https://imagenes.ejemplo.test.otro.ejemplo.test/a.png", ["imagenes.ejemplo.test"]));
}
```

1. Córrela: pasa.
2. **Quita el arreglo.** En `Destinos.cs`, cambia la última línea de `EsPermitido` por:
   `return hostsPermitidos.Any(h => uri.Host.StartsWith(h, StringComparison.OrdinalIgnoreCase));`
   y córrela otra vez. Se obtuvo: `Failed ... Un_host_que_solo_empieza_igual_no_esta_permitido`.
3. Explica en dos frases por qué `StartsWith` acepta un destino que no es el permitido.
4. Restaura la igualdad exacta y vuelve a correr todo.

Esto es lo que hace que la prueba valga: **cae cuando el arreglo se quita**.

---

## Paso 7 - Un cliente que se niega a conectar

El archivo `ApiSeguridad/ClienteSeguro.cs` arma un `HttpClient` sin redirecciones, con tiempos y que verifica la dirección resuelta antes de conectar; sus pruebas están en `ApiSeguridad.Tests/ClienteSeguroTests.cs`. Se compiló y probó con .NET 10. Córrelas con:

```bash
dotnet test --filter "FullyQualifiedName~ClienteSeguro"
```

La salida esperada fue `Passed! - Failed: 0, Passed: 6`. La primera prueba abre un servidor local en `127.0.0.1` y comprueba que el cliente **no** conecta (`Assert.False(oyente.Pending())`); las otras cinco validan lo que devuelve un tercero (tipo, valor, forma). En tu evidencia:

1. Copia las tres líneas del `ConnectCallback` que deciden rechazar y explica por qué la dirección que se verifica es la misma a la que se conecta.
2. ¿Qué ataque evita esa decisión que una verificación previa de DNS no evita? (El *DNS rebinding*; lo advierte el cheat sheet de OWASP.)

---

## Paso 8 - Leer un esquema GraphQL con la lista de cinco frentes

Con el esquema de **tu** API GraphQL, o con el de DVGA si lo levantaste (paso 9), llena esta tabla sin escribir ninguna consulta de ataque; solo lee el esquema y la configuración:

| Frente | Pregunta | Tu respuesta (sí / no / no sé) | Dónde lo compruebas |
|---|---|---|---|
| Profundidad | ¿Hay relaciones circulares y un límite de profundidad? | | Esquema y `AddMaxExecutionDepthRule` o equivalente |
| Costo y tiempo | ¿Hay tiempo máximo y topes en las listas? | | `ExecutionTimeout`, paginación |
| Introspección | ¿Está desactivada en producción? | | `AllowIntrospection(false)` o equivalente |
| Autorización | ¿Cada resolución comprueba objeto y campo? | | Código de las resoluciones |
| Lotes y alias | ¿Hay tope de operaciones por petición? | | Configuración del servidor |

Los fragmentos de configuración (Hot Chocolate 15) están en las láminas y **no se compilaron**.

---

## Paso 9 (opcional) - Laboratorios de la comunidad

Solo en tu VM propia con Docker. **Estos comandos no se ejecutaron al escribir el curso**; siguen los README oficiales.

**OWASP crAPI** (github.com/OWASP/crAPI, Apache-2.0): según su README, descarga el repositorio y en `deploy/docker` corre `docker compose -f docker-compose.yml --compatibility up -d`; la web queda en `http://localhost:8888` y el correo de pruebas en `http://localhost:8025`. Los retos están descritos en `docs/challenges.md`. Tarea de reconocimiento: identifica **dos flujos de negocio** de la aplicación (por ejemplo, uno con dinero o saldo y uno con correos) y llena el mapa de la lámina "Cómo se piensa: el mapa de flujos", con la regla que **debería** estar en el servidor.

**Damn Vulnerable GraphQL Application** (github.com/dolevf/Damn-Vulnerable-GraphQL-Application, MIT): su README indica `docker build -t dvga .` y `docker run -d -t -p 5013:5013 -e WEB_HOST=0.0.0.0 --name dvga dvga`, o la imagen publicada `dolevf/dvga`; se abre en `http://localhost:5013`. Úsala para llenar la tabla del paso 8 con lo que **no** tiene. El README menciona una colección de Postman con soluciones; este curso no la usa.

Al terminar, **detén y borra** los contenedores (`docker rm -f dvga` y, para crAPI, `docker compose ... down` en la misma carpeta).

---

## Qué entregas

`m05-evidencia.md` con: las tres salidas de falla (pasos 2 a 5), tus respuestas, las dos pruebas nuevas con su resultado antes y después de quitar el arreglo (pasos 4.3 y 6), la tabla del paso 8 y, si lo hiciste, el mapa de flujos del paso 9.

---

## Si algo no sale

| Síntoma | Causa probable |
|---|---|
| La prueba de las 20 peticiones pasa con el interruptor encendido | Encendiste el interruptor en otra prueba; revisa que sea la de las 20 |
| `Un_host_que_solo_empieza_igual...` no cae con `StartsWith` | Cambiaste otra línea de `Destinos.cs`; revisa con `git diff` |
| Fallan pruebas de otros módulos | Dejaste un interruptor encendido; `git checkout -- ApiSeguridad.Tests ApiSeguridad` restaura el original |
| Docker no baja la imagen de crAPI o DVGA | Sin red en tu VM; el paso es opcional |
