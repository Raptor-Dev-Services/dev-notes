# Glosario - M02 - Autenticación, JWT y OAuth

> Definiciones de los términos técnicos del módulo 2. Ordenado alfabéticamente.

---

## A

### Access token (token de acceso)
**Categoría:** OAuth 2.0
**Definición:** Credencial que representa la autorización concedida a un cliente para llamar a una API. Es para la API: el cliente no debería interpretarla.
**En el curso:** El token que valida la API en cada solicitud.

### alg (cabecera)
**Categoría:** JWT
**Definición:** Campo de la cabecera de un JWT que dice con qué algoritmo se firmó. Lo escribe quien presenta el token, por lo que el servidor no debe confiar en él para decidir cómo verificar.
**En el curso:** Se fija en el servidor con `ValidAlgorithms`.

### Algoritmo none
**Categoría:** JWT
**Definición:** Valor de `alg` que indica un JWS sin firma. RFC 7518, 3.6: las implementaciones no deben aceptar JWS sin protección por omisión.
**En el curso:** `RequireSignedTokens = true`.

### aud (audiencia)
**Categoría:** JWT
**Definición:** Claim que dice para quién es el token. RFC 7519: si quien procesa el token no se identifica en un `aud` presente, el token debe rechazarse.
**En el curso:** `ValidateAudience = true`.

### Authorization Code (código de autorización)
**Categoría:** OAuth 2.0
**Definición:** Flujo en el que el cliente recibe un código temporal y lo cambia por tokens en el endpoint de token. Es el flujo recomendado, con PKCE.
**En el curso:** Parte 4.

---

## B

### Base64url
**Categoría:** Codificación
**Definición:** Codificación de bytes a texto segura para URL, sin relleno (`=`). Un JWT usa base64url en sus tres partes. **Codificar no es cifrar.**
**En el curso:** `Base64UrlEncoder.Decode`.

### Bearer
**Categoría:** HTTP
**Definición:** Esquema de la cabecera `Authorization: Bearer <token>`: quien presenta el token queda autorizado, sin otra prueba. Por eso un token robado sirve mientras no expire.
**En el curso:** Cómo viajan los tokens en la API.

### Broken Authentication
**Categoría:** Vulnerabilidades
**Definición:** API2:2023 del OWASP API Security Top 10: mecanismos de autenticación mal implementados que permiten asumir la identidad de otra persona.
**En el curso:** Tema del módulo.

---

## C

### Claim (afirmación)
**Categoría:** JWT
**Definición:** Dato dentro del cuerpo de un JWT (`iss`, `sub`, `exp`...). Los nombres registrados los define RFC 7519.
**En el curso:** Los lee la API tras validar la firma.

### ClockSkew
**Categoría:** ASP.NET Core
**Definición:** Margen de tolerancia para diferencias de reloj al validar `exp` y `nbf`. El valor por omisión de `TokenValidationParameters` es de 5 minutos.
**En el curso:** La API lo baja a 30 segundos.

### Código de verificación (code_verifier) y desafío (code_challenge)
**Categoría:** OAuth 2.0
**Definición:** Pareja de valores de PKCE: el cliente manda el desafío al pedir el código y el verificador al cambiarlo por tokens. Si puede usar S256, debe usarlo (RFC 7636).
**En el curso:** Protege el flujo de código.

---

## E

### exp
**Categoría:** JWT
**Definición:** Momento a partir del cual el token no debe aceptarse. RFC 7519: el momento actual debe ser anterior.
**En el curso:** `RequireExpirationTime = true`.

---

## H

### HMAC
**Categoría:** Criptografía
**Definición:** Código de autenticación de mensajes con llave, construido sobre una función hash. HS256 es HMAC con SHA-256.
**En el curso:** La firma del JWT de la referencia.

### HS256
**Categoría:** JWT
**Definición:** Algoritmo de firma con HMAC y SHA-256 y llave simétrica. RFC 7518, 3.2: la llave debe medir 256 bits o más.
**En el curso:** El algoritmo permitido de la referencia.

---

## I

### ID token
**Categoría:** OpenID Connect
**Definición:** Token que dice quién inició sesión, destinado al **cliente**. No se manda a la API como si fuera un token de acceso.
**En el curso:** Se distingue del token de acceso (Parte 4).

### iss (emisor)
**Categoría:** JWT
**Definición:** Claim con quién emitió el token. La API acepta solo su emisor.
**En el curso:** `ValidateIssuer = true`.

---

## J

### JWT
**Categoría:** JWT
**Definición:** *JSON Web Token* (RFC 7519): afirmaciones en JSON, normalmente firmadas, en tres partes separadas por puntos.
**En el curso:** El token de la API.

### JWS
**Categoría:** JWT
**Definición:** *JSON Web Signature* (RFC 7515): la estructura que firma un contenido.
**En el curso:** Es lo que hay detrás de un JWT firmado.

---

## K

### Keycloak
**Categoría:** Identidad
**Definición:** Servidor de identidad y acceso de código abierto que implementa OAuth 2.0 y OpenID Connect.
**En el curso:** Ejemplo de servidor de autorización; lectura `03-arquitectura/08-keycloak.md`.

---

## N

### nbf
**Categoría:** JWT
**Definición:** Momento antes del cual el token no debe aceptarse.
**En el curso:** Sugerido como prueba nueva en la tarea.

---

## O

### OAuth 2.0
**Categoría:** OAuth
**Definición:** Marco de **autorización delegada** (RFC 6749). RFC 9700 (enero de 2025) es la guía vigente de seguridad.
**En el curso:** Parte 4.

### OpenID Connect
**Categoría:** OAuth
**Definición:** Capa de identidad sobre OAuth 2.0 que agrega el ID token.
**En el curso:** Se contrasta con OAuth.

---

## P

### PBKDF2
**Categoría:** Criptografía
**Definición:** Función de derivación de llaves para guardar contraseñas. Con HMAC-SHA256, OWASP recomienda 600 000 iteraciones si no se puede usar Argon2id.
**En el curso:** `Contrasenas.Crear` en la referencia.

### PKCE
**Categoría:** OAuth
**Definición:** *Proof Key for Code Exchange* (RFC 7636): evita que un código de autorización interceptado se use. Los clientes públicos deben usarlo (RFC 9700).
**En el curso:** Flujo de código recomendado.

---

## R

### Rate limiting (límite de tasa)
**Categoría:** Defensa
**Definición:** Limitar cuántas solicitudes se aceptan en un periodo, por cliente o por cuenta. Responde 429.
**En el curso:** Protege el login; se profundiza en el módulo 4.

### Refresh token
**Categoría:** OAuth
**Definición:** Credencial de larga vida que sirve para pedir nuevos tokens de acceso sin volver a autenticar.
**En el curso:** Se menciona; el manejo de sesiones está en `04-backend/40-session-management.md`.

### Revocación
**Categoría:** JWT
**Definición:** Invalidar un token antes de su `exp`. Un JWT no se revoca por sí solo: el servidor necesita una lista, por ejemplo por `jti`.
**En el curso:** Parte 3.

---

## S

### Secreto / llave simétrica
**Categoría:** Criptografía
**Definición:** Valor compartido que firma y verifica. Debe ser aleatorio, de 256 bits o más y guardarse fuera del repositorio.
**En el curso:** `Jwt:Clave`.

### sub
**Categoría:** JWT
**Definición:** Claim con quién es el sujeto del token (el id del usuario).
**En el curso:** `NameClaimType = "sub"`.

---

## T

### TokenValidationParameters
**Categoría:** ASP.NET Core
**Definición:** Objeto con las reglas con las que se valida un token en `JwtBearer`.
**En el curso:** Configuración estricta de la Parte 3.

---

## V

### ValidateOnStart
**Categoría:** ASP.NET Core
**Definición:** Opción del patrón de opciones que valida la configuración al arrancar y hace que la aplicación no inicie si es inválida.
**En el curso:** Impide que una clave corta llegue a producción.
