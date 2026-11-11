# Práctica - M02 - Autenticación, JWT y OAuth

**Curso:** Seguridad de APIs: atacar y defender
**Duración:** 90 minutos, individual, en tu máquina.

> **Marco legal y ético.** Esta práctica se hace con código **propio**: la API de referencia del repositorio y tu propia API. Los "tokens malos" que verás se mandan **a tu propia aplicación** para comprobar que los rechaza. No se adivinan llaves, no se fabrican tokens para sistemas de terceros y no se usan contraseñas de personas reales. Las cuentas, la clave `clave-de-ejemplo` y los datos son ficticios.

---

## Objetivo

1. Correr y leer las pruebas de autenticación de la API de referencia, una por cada control de validación.
2. **Verlo fallar** tres veces: quitar un control y leer qué prueba cae.
3. Comprobar que un token está codificado y no cifrado.
4. Comprobar el comportamiento del login: misma respuesta y límite de intentos.
5. Auditar la validación de tokens de tu propia API.

Evidencia: las salidas de cada paso y la tabla del paso 6.

---

## Antes de empezar

```bash
cd dev-notes/cursos/api-security/referencia
dotnet test --filter "FullyQualifiedName~Modulo2Tests"
```

Sin SDK local, dentro del contenedor oficial (un contenedor, se borra al terminar):

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test --nologo --filter "FullyQualifiedName~Modulo2Tests"
```

Los comandos y las salidas de este documento **se ejecutaron** al escribirlo (SDK de .NET 10, paquete `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13). Con el repositorio tal cual, las 17 pruebas del módulo 2 pasan.

---

## Paso 1 - Leer la validación

Abre `ApiSeguridad/Autenticacion.cs` y encuentra `TokenValidationParameters`. Rellena esta tabla con lo que hace cada línea (una frase):

| Línea | Qué hace |
|---|---|
| `ValidateIssuer = true` + `ValidIssuer` | |
| `ValidateAudience = true` + `ValidAudience` | |
| `ValidateLifetime = true` | |
| `RequireExpirationTime = true` | |
| `RequireSignedTokens = true` | |
| `ValidateIssuerSigningKey = true` | |
| `ValidAlgorithms = [HmacSha256]` | |
| `ClockSkew = 30 s` | |

Luego abre `Modulo2Tests.cs` y asocia cada línea con la prueba que la protege (tabla de la Parte 3 de la clase). Si encuentras una línea **sin** prueba, es una brecha.

---

## Paso 2 - Verlo fallar (tres veces)

Cada vez: **un** cambio, correr las pruebas del módulo, leer, **deshacer** (`git checkout -- ApiSeguridad`).

### 2a. Sin lista de algoritmos

Borra la línea `ValidAlgorithms = [SecurityAlgorithms.HmacSha256],`.

```bash
dotnet test --filter "FullyQualifiedName~Modulo2Tests"
```

Debe caer `Un_token_con_algoritmo_no_permitido_no_entra`. La prueba usa una llave de 64 bytes porque HS512 exige llave de 512 bits, firma con HS512 y espera 401. Sin la lista, la API acepta lo que la cabecera del token le diga.

### 2b. Sin audiencia

Cambia `ValidateAudience = true` por `false`. Cae `Un_token_para_otra_audiencia_no_entra`: un token hecho para **otra** API entraría a la tuya.

### 2c. Con una clave corta

En la prueba `Verlo_fallar_la_aplicacion_no_arranca_con_una_clave_corta` se construye la aplicación con `Jwt:Clave=secreto`. La prueba pasa **porque la aplicación falla al arrancar**. Lee su aserción:

```csharp
var error = Assert.ThrowsAny<Exception>(() => f.CreateClient());
Assert.Contains("256 bits", error.ToString());
```

El mensaje viene de `ValidateOnStart`: "La clave de firma debe medir al menos 256 bits (32 bytes)." Ahora, para ver la falla **de verdad**, quita temporalmente la línea `.Validate(o => ClavesJwt.EsValida(...))` de `Program.cs` y vuelve a correr: la prueba cae, porque la aplicación **sí arrancó** con una clave de 7 bytes.

Anota, para cada paso: la prueba que cayó, el mensaje y la clase de token falso que habría pasado.

---

## Paso 3 - Un token se lee sin la clave

```bash
dotnet test --filter "FullyQualifiedName~Un_token_se_lee_sin_la_clave"
```

La prueba hace esto:

```csharp
var partes = token.Split('.');
var cabecera = Base64UrlEncoder.Decode(partes[0]);   // {"alg":"HS256","typ":"JWT"}
var cuerpo   = Base64UrlEncoder.Decode(partes[1]);   // {"sub":"1","role":"cliente","exp":...}
```

Responde:

1. ¿Qué datos de tu aplicación irían a parar a ese cuerpo si no tuvieras cuidado? (Pista: correo, teléfono, notas.)
2. Si alguien cambia `"role":"cliente"` por `"role":"admin"` en el cuerpo, ¿qué parte del token deja de coincidir? (La firma: se calcula sobre la cabecera y el cuerpo.)
3. ¿Por qué el **servidor** no debería tomar una decisión de seguridad con un claim que no valida contra la firma?

**Nota:** no pegues tokens reales (de tus sistemas) en sitios web de terceros para decodificarlos: el token es una credencial. Decodifica en tu máquina.

---

## Paso 4 - El tiempo de vida del token

Dos pruebas muestran el comportamiento de la librería (SDK de .NET 10):

```bash
dotnet test --filter "FullyQualifiedName~margen_de_reloj|FullyQualifiedName~agrega_una_expiracion"
```

- `El_margen_de_reloj_por_omision_de_la_libreria_es_de_cinco_minutos`: sin cambiar nada, `new TokenValidationParameters().ClockSkew` vale 5 minutos.
- `El_manejador_agrega_una_expiracion_de_60_minutos_si_no_se_le_da_ninguna`: un token creado sin `Expires` trae una expiración de 60 minutos.

Preguntas:

1. Con un token de 15 minutos y el margen por omisión, ¿cuánto tiempo **real** puede seguir sirviendo un token ya vencido? ¿Y con el margen de la API de referencia (30 segundos)?
2. ¿Por qué la prueba `Un_token_sin_expiracion_no_entra` tuvo que apagar `SetDefaultTimesOnTokenCreation`? ¿Qué habría pasado si no se apaga? (El token "sin expiración" habría traído una de 60 minutos y la prueba habría pasado por la razón equivocada.)

---

## Paso 5 - El login

```bash
dotnet test --filter "FullyQualifiedName~Credenciales_correctas|FullyQualifiedName~responden_igual|FullyQualifiedName~sexto_intento"
```

Las tres pruebas:

| Prueba | Qué comprueba |
|---|---|
| `Credenciales_correctas_dan_un_token_que_funciona` | El token emitido por el login lo acepta el resto de la API |
| `Usuario_inexistente_y_clave_mala_responden_igual` | Mismo código (401) y mismo título para un usuario que no existe y una clave mala |
| `El_sexto_intento_de_login_en_un_minuto_se_limita` | Los primeros cinco intentos dan 401 y el sexto, 429 |

**Verlo fallar:** en `Program.cs`, quita `.RequireRateLimiting("login")` del `MapPost("/auth/login", ...)`. Cae `El_sexto_intento_de_login_en_un_minuto_se_limita`. Restaura.

Preguntas:

1. ¿Por qué el login verifica la clave contra un `hashFalso` cuando el usuario no existe? (Para que tarde lo mismo.)
2. El límite del login particiona por la dirección IP del cliente. ¿Qué pasa detrás de un proxy si no configuras los encabezados reenviados?
3. ¿Qué otro límite agregarías además del de IP? (Por cuenta.)

---

## Paso 6 - Audita tu propia API

Con la lista de comprobación de la clase (RFC 8725, 3.1 a 3.12), llena la tabla para **tu** API. Si no tienes una, hazla para la API de referencia y anota "sin API propia" en el alcance.

| # | Comprobación | ¿Qué hace tu API? | ¿Tiene prueba? | Brecha |
|---|---|---|---|---|
| 1 | Verifica la firma | | | |
| 2 | El algoritmo sale de una lista fija en el servidor | | | |
| 3 | Rechaza tokens sin firma | | | |
| 4 | La llave mide 256 bits o más y se valida al arrancar | | | |
| 5 | La llave sale de configuración por entorno, no del repositorio | | | |
| 6 | Valida emisor | | | |
| 7 | Valida audiencia | | | |
| 8 | Exige `exp` y valida la vigencia | | | |
| 9 | `ClockSkew` explícito y pequeño | | | |
| 10 | Los permisos finos se comprueban en el servidor, no solo con el claim | | | |
| 11 | Login con misma respuesta y límite de intentos | | | |
| 12 | Si usa OAuth: código con PKCE; sin implícito ni contraseña directa | | | |

Cada "brecha" lleva su corrección y la **prueba** que la cierra.

---

## Paso 7 (opcional) - Una llave bien hecha

Genera una llave aleatoria de 48 bytes (más de 256 bits) para un entorno tuyo:

```bash
openssl rand -base64 48
```

o en C#, `Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))`. Guárdala en una variable de entorno (por ejemplo `Jwt__Clave`) o en el gestor de secretos de tu proyecto; **no** en `appsettings.json`. Comprueba que `ClavesJwt.EsValida` la acepta.

---

## Entregable de la sesión

- La tabla del paso 1 y las notas de los tres "verlo fallar".
- Las respuestas de los pasos 3 a 5.
- La tabla del paso 6.

---

## Referencias

- IETF. *RFC 7518*, 3.2 y 3.6; *RFC 7519*; *RFC 8725*, 3.1 a 3.12; *RFC 9700*, 2.1 y 2.4. rfc-editor.org
- Microsoft. *Configure JWT bearer authentication in ASP.NET Core*; *Rate limiting middleware in ASP.NET Core*. learn.microsoft.com
- OWASP. *Password Storage Cheat Sheet*. cheatsheetseries.owasp.org
- *The JWT Handbook* (Peyrott, 2018). *Pentesting APIs* (Harley, 2024), cap. 4.
