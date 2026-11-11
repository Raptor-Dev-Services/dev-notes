<!-- _class: lead -->

# Módulo 6 - Corregir, probar y reportar

### Cerrar cada hallazgo de forma que no regrese

**Rogelio Arriaga - Raptor Dev Services**

<!--
Este módulo cierra el curso. No hay técnicas nuevas de ataque: se toma cada clase de falla de los módulos 1 a 5, se corrige, se prueba y se reporta. El código de referencia (cursos/api-security/referencia) ya trae las pruebas; hoy se aprende a leerlas, a romperlas a propósito y a llevarlas a la API propia.
-->

---

## Agenda del módulo

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, alcance, qué significa "cerrado" | 10 min |
| Parte 1 | El Top 10 de APIs como lista de controles y pruebas | 20 min |
| Parte 2 | Configuración (API8) e inventario (API9) como código | 20 min |
| Parte 3 | Probar el arreglo quitándolo | 15 min |
| Parte 4 | Modelado de amenazas continuo y pruebas en CI | 20 min |
| Parte 5 | El informe y la divulgación responsable | 20 min |
| Cierre | Práctica, tarea, quiz y proyecto final | 15 min |

<!-- Descanso de 10 minutos entre la Parte 3 y la Parte 4. -->

---

## El lema del módulo

> Un hallazgo cerrado sin prueba vuelve en el siguiente merge.

- Un arreglo sin prueba depende de que nadie toque ese archivo otra vez.
- Una prueba que nunca se vio fallar no demuestra nada: puede estar pasando por una razón equivocada.
- El informe sin evidencia repetible es una opinión.

---

## Al terminar puedes

- Asignar a cada clase del OWASP API Security Top 10 un control en ASP.NET Core y la prueba que lo vigila.
- Demostrar que una prueba sirve **quitando** el control y viéndola caer.
- Configurar cabeceras, CORS, secretos y claves de firma como decisiones comprobables.
- Correr las pruebas de seguridad en integración continua de forma que un fallo bloquee el merge.
- Escribir el informe de un hallazgo con evidencia mínima, corrección y prueba.

---

## Alcance de este módulo

| Sí | No |
|---|---|
| Correcciones en C# y ASP.NET Core con su prueba | Cargas de ataque o procedimientos de explotación |
| Probar tu API propia, o el código de referencia del curso | Probar sistemas ajenos, con o sin "permiso verbal" |
| Romper un control **en el código de referencia** para ver caer su prueba | Desactivar controles en un sistema compartido |
| Informe con evidencia mínima y repetible | Evidencia que incluya datos reales de personas |

**Marco:** todo lo del módulo se hace sobre el código del curso o sobre una API que tú escribiste. Si encuentras un fallo en un servicio ajeno, la ruta es la divulgación responsable (parte 5), no probar más.

---

## Qué significa "cerrado"

Un hallazgo está cerrado cuando se cumplen **tres** cosas, no una:

1. **La causa** está corregida (no solo el caso que se vio).
2. **Existe una prueba automatizada** que habría fallado antes del arreglo.
3. **La prueba corre en cada cambio** y bloquea el merge si falla.

Si falta la 2 o la 3, el hallazgo está "arreglado", no "cerrado".

---

<!-- _class: lead -->

# Parte 1
## El Top 10 como lista de controles y pruebas

---

## OWASP API Security Top 10, edición 2023

| Id | Categoría |
|---|---|
| API1:2023 | Broken Object Level Authorization |
| API2:2023 | Broken Authentication |
| API3:2023 | Broken Object Property Level Authorization |
| API4:2023 | Unrestricted Resource Consumption |
| API5:2023 | Broken Function Level Authorization |
| API6:2023 | Unrestricted Access to Sensitive Business Flows |
| API7:2023 | Server Side Request Forgery |
| API8:2023 | Security Misconfiguration |
| API9:2023 | Improper Inventory Management |
| API10:2023 | Unsafe Consumption of APIs |

Fuente: OWASP API Security Project, edición 2023, api-security.owasp.org (consultado en noviembre de 2026).

---

## Del riesgo al control, y del control a la prueba

<!-- _pasos: filas -->

| Clase | Control en ASP.NET Core | Prueba del código de referencia |
|---|---|---|
| API1 BOLA | Filtrar por dueño en cada consulta; 404 si no es suyo | `Modulo3Tests`: `Nadie_ve_el_pedido_de_otro` |
| API2 Autenticación | `JwtBearer` estricto, límite en el login | `Modulo2Tests`: tokens inválidos y `El_sexto_intento...` |
| API3 Propiedades | DTO de entrada y de salida, nunca la entidad | `Modulo3Tests`: `El_cliente_no_puede_cambiar_su_propio_rol` |
| API4 Consumo | Tope de página, tope de cuerpo, límite de tasa | `Modulo4Tests`: `El_cliente_no_decide_cuantos_registros_recibe` |
| API5 BFLA | Política `Admin` declarada en la ruta | `Modulo3Tests`: `Un_cliente_no_usa_funciones_de_administracion` |
| API6 Flujos | Operación atómica y una vez por pedido | `Modulo5Tests`: `Veinte_peticiones_simultaneas...` |
| API7 SSRF | Lista de destinos permitidos | `Modulo5Tests`: `La_lista_de_destinos_rechaza` |
| API8 Configuración | Cabeceras, CORS, clave mínima | `Modulo6Tests`, `Modulo2Tests.Una_clave_corta...` |
| API9 Inventario | Contrato de rutas comparado con las reales | `Modulo1Tests`: `La_API_expone_exactamente...` |
| API10 Consumo inseguro | Validar y limitar lo que devuelve un tercero | Fragmento de los módulos 4 y 5 |

---

## Cómo leer una prueba de seguridad

Una prueba de seguridad tiene la forma contraria a una prueba funcional:

| Prueba funcional | Prueba de seguridad |
|---|---|
| "Con datos válidos, funciona" | "Con datos que **no** debe aceptar, no funciona" |
| Un usuario | Dos usuarios, o un usuario sin permiso |
| Pasa en el camino feliz | Falla si el control desaparece |

Ejemplo del código de referencia (`Modulo3Tests`): un cliente intenta usar la función de administración y la API debe responder 403.

```csharp
using var ana = f.ComoUsuario(1);
Assert.Equal(HttpStatusCode.Forbidden, (await ana.GetAsync("/api/admin/usuarios")).StatusCode);
```

---

## Error típico: probar solo el camino feliz

> "Mi prueba de autenticación pasa: el token válido entra."

- Esa prueba también pasa en una API sin ninguna validación.
- La que importa es la contraria: el token vencido, el de otra audiencia y el de otra clave **no entran**.
- Regla: por cada control, **una prueba que lo viole** y exige el rechazo.

---

## La prueba hermana: "verlo fallar"

El código de referencia trae interruptores de laboratorio (`LabOpciones`) que reintroducen, a propósito, una falla. Cada uno tiene su prueba hermana:

| Interruptor | La prueba demuestra que... |
|---|---|
| `Bola` | el cruce entre usuarios **sí** se detecta |
| `Bfla` | el cliente entra a administración sin la política |
| `SqlConcatenado` | un apóstrofe rompe la consulta armada con texto |
| `ErroresDetallados` | la excepción llega al cliente |
| `RutaOlvidada` | la ruta sin documentar aparece en el inventario |

Estas pruebas "verlo fallar" existen solo en el laboratorio: son la evidencia de que el detector funciona.

---

<!-- _class: lead -->

# Parte 2
## Configuración (API8) e inventario (API9) como código

---

## API8: cuatro decisiones que se pueden probar

1. **Cabeceras de respuesta** de una API.
2. **CORS** con lista de orígenes.
3. **HTTPS** y transporte.
4. **Secretos y claves** fuera del código y con longitud mínima.

Cada una se escribe una vez y se vigila con una prueba, en lugar de revisarse "a ojo" en cada despliegue.

---

## Cabeceras de una respuesta de API

El código de referencia las añade en un middleware (`UseCabecerasDeApi`):

```csharp {1-2|3|4|5|*}
contexto.Response.OnStarting(() =>
{
    var h = contexto.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["Cache-Control"] = "no-store";
    h["Content-Security-Policy"] = "frame-ancestors 'none'";
    return Task.CompletedTask;
});
```

- `nosniff`: el navegador no adivina el tipo del contenido.
- `no-store`: una respuesta con datos de una persona no se guarda en cachés.
- `frame-ancestors 'none'`: la respuesta no se muestra dentro de un marco.

Referencia: OWASP, *HTTP Headers Cheat Sheet*, cheatsheetseries.owasp.org. Son decisiones de este código de referencia; tu API puede necesitar otras.

---

## La prueba de las cabeceras

```csharp
var r = await c.GetAsync("/api/pedidos");
Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
Assert.Equal("no-store", r.Headers.GetValues("Cache-Control").Single());
Assert.Equal("frame-ancestors 'none'", r.Headers.GetValues("Content-Security-Policy").Single());
```

- Si alguien mueve el middleware o lo borra, la prueba cae.
- Compara contra el valor exacto: así un cambio deliberado exige editar la prueba y se ve en la revisión.

---

## CORS: una lista, no un comodín

```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("https://app.ejemplo.test")
    .WithMethods("GET", "POST", "PATCH")
    .WithHeaders("Authorization", "Content-Type")));
```

- Se permiten los orígenes, métodos y cabeceras que la aplicación usa. Nada más.
- La prueba (`CORS_solo_acepta_el_origen_de_la_aplicacion`) envía una solicitud previa desde un origen propio y otra desde uno ajeno, y exige la cabecera `Access-Control-Allow-Origin` solo en la primera.
- CORS lo aplica el **navegador**; no sustituye a la autenticación. Lectura: `04-backend/11-cors.md`.

---

## HTTPS: lo que dice la documentación de ASP.NET Core

- `UseHsts()` añade la cabecera `Strict-Transport-Security`; su vigencia por omisión es de 30 días y se cambia con `HstsOptions.MaxAge`.
- Se llama normalmente solo cuando el entorno **no** es de desarrollo.
- Para proyectos de API, la documentación indica que las plantillas no incluyen HSTS porque es una instrucción pensada para navegadores, y recomienda escuchar solo por HTTPS o rechazar HTTP con 400 en lugar de redirigir.

Fuente: Microsoft Learn, *Enforce HTTPS in ASP.NET Core* (consultado en noviembre de 2026).

```csharp
// Fragmento (no forma parte del código de referencia, que corre en memoria).
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
```

---

## Secretos y claves: dos controles al arrancar

El código de referencia no arranca si la clave de firma es corta:

```csharp {1-3|4|*}
builder.Services.AddOptions<OpcionesJwt>().BindConfiguration("Jwt")
    .Validate(o => ClavesJwt.EsValida(o.Clave),
        "La clave de firma debe medir al menos 256 bits (32 bytes).")
    .ValidateOnStart();
```

- La clave viene de la configuración (variable de entorno o gestor de secretos), **nunca** del repositorio. Lectura: `04-backend/10-secretos.md`.
- Para HS256, el RFC 7518 (sección 3.2) exige una clave del mismo tamaño que la salida del hash (256 bits) o mayor.
- `ValidateOnStart` hace que el error aparezca al desplegar, no cuando alguien adivine la clave.

---

## La prueba de la clave

```csharp
[Fact]
public void Verlo_fallar_la_aplicacion_no_arranca_con_una_clave_corta()
{
    using var f = new Fabrica("Jwt:Clave=secreto");
    var error = Assert.ThrowsAny<Exception>(() => f.CreateClient());
    Assert.Contains("256 bits", error.ToString());
}
```

- La prueba **provoca** la clave corta y exige que el arranque falle con ese mensaje.
- Es una prueba de configuración: vale tanto como una de código.

---

## API9: el inventario es una prueba

Una API que nadie documentó es la que nadie protegió. El código de referencia compara lo que **expone** con un contrato escrito (`rutas-documentadas.txt`):

```csharp {1-4|5-11|12-13|*}
foreach (var fuente in fabrica.Services.GetServices<EndpointDataSource>())
    foreach (var e in fuente.Endpoints.OfType<RouteEndpoint>())
    {
        var verbos = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
        foreach (var v in verbos.Where(v => v != "OPTIONS"))
        {
            reales.Add(Llave(e, v));
            if (e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
                anonimas.Add(Llave(e, v));
        }
    }
// ... la prueba exige: reales == documentadas
// ... y: anonimas == { "POST /api/auth/login" }
```

---

## Qué pasa cuando alguien olvida una ruta

- Una ruta de depuración que nadie documentó aparece en `SinDocumentar`.
- Una ruta pública que no es el login aparece en `AnonimasInesperadas`.
- La prueba `Verlo_fallar_una_ruta_olvidada_aparece_como_hallazgo` lo demuestra con el interruptor `RutaOlvidada`.
- Además, `FallbackPolicy` exige sesión en toda ruta que no diga lo contrario: una ruta nueva **nace protegida**.

---

## Retirar versiones viejas

El inventario no solo agrega; también **retira**:

- Una versión vieja de la API (`/v1/`) sigue siendo superficie mientras responda.
- Documenta la fecha de retiro y quita la ruta del contrato cuando se apague.
- La prueba "La API expone exactamente lo que documenta" falla en las dos direcciones: ruta sin documentar y ruta documentada que ya no existe (`Fantasma`).

---

## Error típico: configurar y no vigilar

> "Ya pusimos las cabeceras en el despliegue de enero."

- Un cambio de proxy, un middleware reordenado o una migración borran la configuración sin que nadie lo note.
- Una configuración sin prueba es un recuerdo.
- Regla: lo que se decide una vez se **prueba** una vez, y la prueba corre siempre.

---

<!-- _class: lead -->

# Parte 3
## Probar el arreglo quitándolo

---

## La técnica: quitar el control y ver caer la prueba

Para saber si una prueba protege algo:

1. Corre la prueba: pasa.
2. **Quita el control** (una línea).
3. Corre la prueba otra vez: **debe fallar**.
4. Restaura el control: vuelve a pasar.

Si el paso 3 sigue en verde, la prueba no vigila lo que crees. Esta es la regla de la skill `verify` del catálogo: un arreglo se valida retirándolo.

---

## Las mutaciones del código de referencia

Se aplicaron una por una sobre el código de referencia y se corrió la suite. La salida de cada una:

| Control que se quita | Prueba que cae |
|---|---|
| `ValidAlgorithms = [HmacSha256]` | `Un_token_con_algoritmo_no_permitido_no_entra` |
| `ValidateAudience = true` pasa a `false` | `Un_token_para_otra_audiencia_no_entra` |
| `RequireExpirationTime = true` pasa a `false` | `Un_token_sin_expiracion_no_entra` |
| `RequireSignedTokens = true` pasa a `false` | `Un_token_sin_firma_no_entra` |
| `.RequireRateLimiting("login")` | `El_sexto_intento_de_login_en_un_minuto_se_limita` |
| `FallbackPolicy` | varias: `Sin_token_no_entra`, `Un_token_vencido_no_entra`, `Una_ruta_que_nadie_documento_no_responde_sin_sesion`, `Una_ruta_sin_sesion_responde_401_con_formato_de_problema`, entre otras |

Cada mutación hace caer exactamente las pruebas de su control; la de `FallbackPolicy` cae en varias porque casi todo depende de que la sesión sea obligatoria.

---

## Dos lecciones de construir esa suite

Al escribir las pruebas aparecieron dos hallazgos del propio código, que muestran por qué se prueba:

1. **Un token "sin expiración" sí tenía expiración.** El manejador de tokens añade una por omisión (`SetDefaultTimesOnTokenCreation`); hubo que apagarla en la prueba para poder construir de verdad un token sin `exp`.
2. **Un token HS512 no se podía crear con la clave de 41 bytes**: la biblioteca exige 512 bits para ese algoritmo. La prueba usa una clave de 64 bytes configurada también en la API.

En los dos casos la prueba, al principio, probaba otra cosa de la que decía.

---

## La prueba que prueba otra cosa

| Síntoma | Causa probable |
|---|---|
| La prueba pasa siempre | No llega al control (otra ruta, otro usuario) |
| Pasa con y sin el control | El dato de entrada no es el que crees |
| Falla al quitar el control, pero por otro motivo | Mira el mensaje del fallo: debe ser el del control |

Revisa **el mensaje** del fallo, no solo el color.

---

## Error típico: arreglar y no quitar

> "Lo corregí y la prueba pasa; listo."

- Pasar después del arreglo no basta: tiene que haber **fallado antes**.
- Escribe la prueba primero, mírala fallar, corrige, mírala pasar.
- Si ya corregiste: quita el arreglo temporalmente, confirma la caída y restáuralo.

---

<!-- _class: lead -->

# Parte 4
## Modelado de amenazas continuo y pruebas en integración continua

---

## Modelar amenazas no es un documento

*Threat Modeling* (Tarandach y Coles, 2020) presenta el modelado como una práctica que se repite (caps. 1 y 2 para el planteamiento y el enfoque general; cap. 5 para el modelado continuo).

Idea práctica para una API: el modelo es una hoja corta con cuatro preguntas que se contestan **cada vez que cambia algo relevante**:

1. ¿Qué estamos construyendo? (rutas, datos, quién llama)
2. ¿Qué puede salir mal? (las diez clases del Top 10)
3. ¿Qué vamos a hacer al respecto? (control y prueba)
4. ¿Lo hicimos bien? (la prueba pasa y se vio fallar)

---

## Cuándo se vuelve a modelar

| Cambio | Pregunta nueva |
|---|---|
| Un endpoint nuevo o un cambio de contrato | ¿Quién puede llamarlo y sobre qué objetos? (API1, API3, API5) |
| Un flujo sensible nuevo (cobro, descuento, invitación) | ¿Se puede repetir o automatizar a escala? (API6, API4) |
| Un tercero nuevo (servicio al que llama tu API) | ¿Qué recibes de él y qué le mandas? (API7, API10) |
| Una nueva forma de autenticar | ¿Dónde se valida y qué claims se confían? (API2) |

Cada fila produce **una prueba nueva**, no un documento nuevo.

---

## Pruebas de seguridad en CI

Las pruebas del código de referencia llevan un rasgo (`Trait`) para poder correrlas por separado:

```csharp
[Trait("Categoria", "Seguridad")]
public class Modulo3Tests { /* ... */ }
```

```bash
dotnet test --filter "Categoria=Seguridad"
```

- El filtro corre solo las pruebas de seguridad: útil como paso rápido y visible.
- En el código de referencia son 97 pruebas y la suite completa pasó en unos 15 segundos en el servidor del curso (noviembre de 2026).

---

## Que un fallo bloquee el merge

Fragmento de un flujo de GitHub Actions (**fragmento ilustrativo**, las versiones de las acciones y de .NET son ilustrativas: revisa las vigentes y ajusta las rutas a tu repositorio):

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

Que el merge se bloquee lo decide la **regla de protección de la rama** (el trabajo como verificación obligatoria), no el YAML. Más sobre integración continua: el curso `/devops-essentials/`.

---

## Qué NO va en las pruebas de seguridad de CI

- Escaneos activos contra entornos compartidos.
- Pruebas que dependen de internet o de un tercero real.
- Credenciales reales: usa claves y usuarios de ejemplo, ficticios.

La prueba debe ser **repetible y local**: correr en un contenedor del propio trabajo, con datos de ejemplo.

---

## Seguridad dentro del ciclo de desarrollo

Cómo encaja esto en una práctica más amplia (solo capítulos, nunca texto):

- *Building Secure and Reliable Systems* (Adkins et al., 2020), caps. 12 a 14: escribir, probar y desplegar código.
- *Software Supply Chain Security* (Crossley, 2024), caps. 4 a 6: ciclo de desarrollo seguro, código fuente, construcción y despliegue, y nube y DevSecOps.
- *Executive's Cybersecurity Program Handbook* (Brown, 2023), caps. 11 y 12: DevSecOps y pruebas de seguridad con métricas.
- *Defensive Security Handbook* (Berlin et al., 2024), cap. 19: desarrollo.
- *Web Application Security*, 2.ª ed. (Hoffman, 2023), caps. 20 a 22, 24 y 25: arquitectura y configuración seguras, modelado de amenazas y revisión de código.

---

<!-- _class: lead -->

# Parte 5
## El informe y la divulgación responsable

---

## El hallazgo, redactado

Una ficha corta, siempre con los mismos campos:

| Campo | Qué lleva |
|---|---|
| Título | Una frase con el efecto, no con la técnica |
| Clase | Id del Top 10 de APIs (por ejemplo, API1:2023) |
| Dónde | Ruta, verbo y versión |
| Evidencia mínima | La petición y la respuesta que muestran el problema, **sin datos reales** |
| Impacto | Qué puede hacer alguien y a quién afecta |
| Corrección | El cambio en el código, como lo haría el equipo |
| Prueba | El nombre de la prueba que habría fallado, y que ahora pasa |
| Prioridad | Alta, media o baja, con su razón |

---

## Ejemplo ilustrativo de una ficha

> **Ejemplo ilustrativo.** API ficticia de pedidos; ningún dato es real.

| Campo | Contenido |
|---|---|
| Título | Un cliente puede leer el pedido de otro cliente |
| Clase | API1:2023 Broken Object Level Authorization |
| Dónde | `GET /api/pedidos/{id}` |
| Evidencia mínima | Con la sesión del usuario 1, la respuesta a un pedido del usuario 2 es 200 en lugar de 404 |
| Impacto | Cualquier cliente autenticado lee pedidos ajenos; afecta a todos los clientes |
| Corrección | Comparar `DuenoId` con el usuario de la sesión y responder 404 si no coincide |
| Prueba | `Nadie_ve_el_pedido_de_otro`: cae sin la comprobación y pasa con ella |
| Prioridad | Alta: expone datos de terceros con una sesión cualquiera |

---

## Prioridad: una escala cualitativa y honesta

Para un equipo pequeño basta una escala de tres niveles que se razona, no se calcula:

- **Alta:** expone datos de terceros o permite acciones de otro rol, con una sesión cualquiera.
- **Media:** requiere una condición adicional o expone información que ayuda a otro ataque.
- **Baja:** endurecimiento; no es la única defensa que falla.

No inventes puntajes: si usas un sistema formal como CVSS, cita la versión y calcula con la calculadora oficial de FIRST (first.org/cvss).

---

## Una lista de verificación: ASVS

El *Application Security Verification Standard* de OWASP es una lista de requisitos de seguridad para aplicaciones. Su última versión estable, a noviembre de 2026, es la **5.0.0, publicada el 30 de mayo de 2025** (asvs.owasp.org).

Cómo usarlo en este curso:

- Como **lista de verificación** al final de la tarea: ¿cubrí autenticación, control de acceso, validación, errores y configuración?
- Citando el requisito por su identificador en la ficha del hallazgo.
- Sin copiar su texto: se enlaza.

---

## Divulgación responsable

Si al revisar un servicio **ajeno** ves un posible fallo, la ruta correcta es:

1. **Detente.** No sigas probando ni guardes datos de otras personas.
2. Busca el canal de reporte del dueño (política de seguridad, correo de seguridad).
3. Reporta con lo mínimo para reproducir, sin datos reales.
4. Da tiempo razonable para corregir antes de hablar del tema en público.

Para el proceso completo, *The Vulnerability Researcher's Handbook* (Strout, 2023), cap. 4 (comunicar los hallazgos) y cap. 8 (casos reales, buenos y malos).

> El marco legal aplicable en México está en el curso `/cyber-law/`.

---

## Error típico: el informe de "todo lo que encontré"

- Una lista de 40 hallazgos sin orden no se corrige.
- Un informe sin corrección es una queja.
- Un informe sin prueba no se puede cerrar.

Mejor: **pocos hallazgos, priorizados, con su corrección y su prueba**.

---

## Tu práctica de hoy

Todo sobre el código de referencia, en tu máquina:

1. Correr `dotnet test --filter "Categoria=Seguridad"` y leer el resultado.
2. **Verlo fallar:** aplicar una mutación, ver qué prueba cae y leer su mensaje.
3. Restaurar el control y comprobar que todo vuelve a verde.
4. Escribir la ficha de un hallazgo con su prueba.
5. Agregar el flujo de CI a tu repositorio, como fragmento.

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas

- Cada clase del Top 10 de APIs tiene un control, una prueba y una forma de verla fallar.
- La configuración y el inventario se vigilan con pruebas, no con memoria.
- Una prueba se valida quitando el control que vigila.
- El modelado de amenazas se repite cuando cambia algo relevante y produce pruebas.
- El informe lleva evidencia mínima, corrección y la prueba que cierra el hallazgo.

**Proyecto final:** la prueba de seguridad completa de **tu** API: inventario, hallazgos priorizados, correcciones con su prueba y el informe.

---

## Referencias

- OWASP. *API Security Top 10, 2023 edition*. api-security.owasp.org
- OWASP. *Application Security Verification Standard 5.0.0*. asvs.owasp.org
- OWASP. *HTTP Headers Cheat Sheet*. cheatsheetseries.owasp.org
- IETF. *RFC 7518: JSON Web Algorithms (JWA)*, sección 3.2. rfc-editor.org/rfc/rfc7518
- Microsoft Learn. *Enforce HTTPS in ASP.NET Core*; *Rate limiting middleware in ASP.NET Core*. learn.microsoft.com
- *Threat Modeling* (Tarandach y Coles, 2020), caps. 1, 2 y 5.
- *Web Application Security*, 2.ª ed. (Hoffman, 2023), caps. 20 a 22, 24 y 25. *Pentesting APIs* (Harley, 2024), cap. 10.
- *Building Secure and Reliable Systems* (Adkins et al., 2020), caps. 12 a 14. *Software Supply Chain Security* (Crossley, 2024), caps. 4 a 6. *Executive's Cybersecurity Program Handbook* (Brown, 2023), caps. 11 y 12. *Defensive Security Handbook* (Berlin et al., 2024), cap. 19. *The Vulnerability Researcher's Handbook* (Strout, 2023), caps. 4 y 8.
- Código de referencia del curso: `cursos/api-security/referencia/`.
