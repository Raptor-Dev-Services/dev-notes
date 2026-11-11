# Glosario - S04 - Autenticación y seguridad en el navegador

> Definiciones de los términos técnicos de la sesión 4.
> Ordenado alfabéticamente.

---

## A

### Autenticación
**Categoría:** Seguridad
**Definición:** Comprobar quién es una persona: correo y contraseña, un segundo factor, una llave. No es lo mismo que la autorización (qué puede hacer una vez identificada).
**En el curso:** `POST /api/login` verifica las credenciales y abre la sesión.

---

## B

### base-uri
**Categoría:** CSP
**Definición:** Directiva de la CSP que limita lo que puede ir en la etiqueta `<base>`. Con `'none'` se impide que un atacante cambie la base de las URL relativas.
**En el curso:** Va en `'none'` en la política del proyecto.

---

## C

### Clickjacking
**Categoría:** Ataques
**Definición:** Engañar a alguien para que haga clic en algo de una página que no ve porque está incrustada, invisible, en un `iframe` de otra página.
**En el curso:** `frame-ancestors 'none'` impide que otros incrusten la aplicación.

### connect-src
**Categoría:** CSP
**Definición:** Directiva de la CSP que limita a qué destinos se puede conectar el JavaScript (`fetch`, XHR, WebSocket). Impide que un script inyectado mande datos a un servidor ajeno.
**En el curso:** Con `connect-src 'self'` el token no sale hacia el puerto del atacante aunque el script corra.

### Content Security Policy (CSP)
**Categoría:** CSP
**Definición:** Cabecera de respuesta, `Content-Security-Policy`, que indica al navegador desde dónde puede cargar y ejecutar recursos la página. Es defensa en profundidad contra el XSS: no reemplaza sanitizar la entrada.
**En el curso:** Se pasa al servidor de laboratorio con la variable `CSP`.

### Cookie
**Categoría:** Sesión
**Definición:** Pequeño dato que el servidor manda con `Set-Cookie` y que el navegador devuelve en las peticiones siguientes al mismo sitio. Sus atributos (`HttpOnly`, `Secure`, `SameSite`) controlan quién la ve y cuándo viaja.
**En el curso:** La sesión de la Mesa de ayuda es la cookie `sid`.

### CORS (Cross-Origin Resource Sharing)
**Categoría:** Navegador
**Definición:** Política del navegador que decide si un sitio puede leer la respuesta de otro. Lo hace cumplir el navegador, no el servidor. Una petición simple llega al servidor aunque CORS impida leer la respuesta.
**En el curso:** El proyecto evita configurarlo: la SPA y la API comparten origen.

### CSRF (Cross-Site Request Forgery)
**Categoría:** Ataques
**Definición:** Hacer que el navegador de una persona con sesión mande a un sitio una petición que ella no quiso, aprovechando que adjunta la cookie solo. El atacante logra una acción, no lee la respuesta.
**En el curso:** Se detiene con `SameSite` y una cabecera personalizada; ver los experimentos A, B y C.

---

## D

### DOM-based XSS
**Categoría:** Ataques
**Definición:** XSS en el que el código malicioso se procesa solo en el navegador: JavaScript del cliente toma datos de la URL o del almacenamiento y los inserta como HTML, sin que el servidor intervenga.
**En el curso:** `innerHTML = 'Resultados para: ' + q` en `lab/vulnerable.js`.

### DOMPurify
**Categoría:** Seguridad
**Definición:** Biblioteca que sanitiza HTML en el navegador, quitando lo que pueda ejecutar código. OWASP la recomienda. Se configura con listas de etiquetas y atributos permitidos.
**En el curso:** `HtmlSeguro` usa DOMPurify 3.4.17 con una lista corta de etiquetas y ningún atributo.

---

## E

### Escapar (salida)
**Categoría:** Seguridad
**Definición:** Convertir los caracteres con significado especial (`<`, `>`, `&`, `"`) en entidades para que se vean como texto y no se interpreten como HTML. React lo hace al interpolar con llaves.
**En el curso:** `<p>{ataque}</p>` produce `&lt;img ...`.

---

## F

### form-action
**Categoría:** CSP
**Definición:** Directiva de la CSP que limita a dónde pueden enviar datos los formularios.
**En el curso:** Va en `'self'`.

### frame-ancestors
**Categoría:** CSP
**Definición:** Directiva de la CSP que controla qué páginas pueden incrustar la tuya en un `iframe`. Con `'none'` ninguna. Va en la cabecera: no funciona desde una etiqueta `<meta>`.
**En el curso:** Previene el clickjacking.

---

## H

### Hash (en una CSP)
**Categoría:** CSP
**Definición:** Huella SHA-256, 384 o 512 del contenido de un script en línea, escrita en `script-src` (`'sha256-...'`). Solo corre ese texto exacto: cambiar un carácter cambia el hash.
**En el curso:** `lab/hash.html`; el hash cambia al editar el script.

### HttpOnly
**Categoría:** Sesión
**Definición:** Atributo de una cookie que impide leerla desde JavaScript (`document.cookie`). Ayuda a mitigar el robo de sesión por XSS, pero no impide que un script inyectado haga peticiones que usen la cookie.
**En el curso:** `document.cookie` llega vacío en el laboratorio, y aun así los datos salen.

---

## L

### localStorage
**Categoría:** Navegador
**Definición:** Almacenamiento clave-valor persistente de un origen, accesible desde cualquier script de ese origen. Todo lo que guardes ahí lo puede leer un XSS.
**En el curso:** `lab/vulnerable.js` guarda ahí el token; el atacante lo roba.

---

## N

### Nonce
**Categoría:** CSP
**Definición:** Valor aleatorio, distinto en cada respuesta, que se declara en la CSP (`'nonce-...'`) y en la etiqueta `<script nonce="...">`. Solo corren los scripts en línea que lo traen.
**En el curso:** Alternativa al hash cuando el contenido del script cambia por respuesta.

### nosniff
**Categoría:** Cabeceras
**Definición:** Valor de `X-Content-Type-Options` que prohíbe al navegador adivinar el tipo de un recurso. Un script o una hoja de estilo con un tipo MIME equivocado se bloquea.
**En el curso:** Lo manda el servidor de laboratorio en todo recurso estático.

---

## O

### Origen (origin)
**Categoría:** Navegador
**Definición:** La combinación de esquema, dominio completo y puerto. Es más estricto que el "sitio": `http://127.0.0.1:3110` y `http://127.0.0.1:3119` son orígenes distintos del mismo sitio.
**En el curso:** Explica por qué CORS bloquea una lectura y `SameSite` no basta contra un puerto vecino.

---

## P

### Preflight
**Categoría:** Navegador
**Definición:** Petición `OPTIONS` previa que el navegador manda antes de una petición "no simple" (con cabeceras propias, por ejemplo) para preguntar al servidor si la acepta.
**En el curso:** Es la razón por la que un formulario de otro sitio no puede mandar `X-Requested-By`.

---

## R

### Referrer-Policy
**Categoría:** Cabeceras
**Definición:** Cabecera que controla cuánta información del origen se manda en `Referer`. El valor por omisión de los navegadores modernos es `strict-origin-when-cross-origin`.
**En el curso:** El servidor de laboratorio lo declara explícitamente.

### Report-Only
**Categoría:** CSP
**Definición:** Modo de la CSP (`Content-Security-Policy-Report-Only`) que no bloquea nada y solo avisa de lo que habría bloqueado. Sirve para probar una política antes de aplicarla.
**En el curso:** Primer paso del camino de la tarea.

### Reflected XSS
**Categoría:** Ataques
**Definición:** XSS cuya carga viaja en la petición actual (un parámetro de la URL, un formulario) y se refleja en la respuesta. La víctima debe abrir un enlace o enviar un formulario preparado.
**En el curso:** El enlace con `?q=<img onerror=...>`.

---

## S

### Sanitizar
**Categoría:** Seguridad
**Definición:** Limpiar un HTML ajeno para quitar lo que ejecute código, dejando lo permitido. Se hace con una biblioteca mantenida, nunca con expresiones regulares.
**En el curso:** `DOMPurify.sanitize(html, { ALLOWED_TAGS, ALLOWED_ATTR })`.

### SameSite
**Categoría:** Sesión
**Definición:** Atributo de una cookie (`Strict`, `Lax` o `None`) que decide si se manda en peticiones iniciadas desde otro sitio. Sin él, los navegadores la tratan como `Lax`. El "sitio" ignora el puerto.
**En el curso:** `SameSite=Lax` detiene el experimento A de CSRF, pero no el B.

### Secure
**Categoría:** Sesión
**Definición:** Atributo de una cookie que la limita a conexiones HTTPS. `SameSite=None` lo exige.
**En el curso:** Falta en el laboratorio (HTTP local); es obligatorio en producción.

### Sesión
**Categoría:** Sesión
**Definición:** El estado de "esta persona ya se identificó", que el servidor liga a un identificador imposible de adivinar y que el navegador presenta en cada petición.
**En el curso:** La cookie `sid` y `GET /api/me`.

### Sink
**Categoría:** Seguridad
**Definición:** Lugar del código donde un dato se interpreta como algo ejecutable: `innerHTML`, `dangerouslySetInnerHTML`, `eval`, un atributo `href` con `javascript:`. Contrario: una fuente segura, como `textContent`.
**En el curso:** La línea de `innerHTML` de `vulnerable.js`.

### Stored XSS
**Categoría:** Ataques
**Definición:** XSS cuya carga queda guardada en el servidor (un comentario, un perfil) y se ejecuta en el navegador de todo el que vea ese dato.
**En el curso:** El riesgo de `dangerouslySetInnerHTML` con un texto que viene de la base de datos.

---

## U

### unsafe-inline
**Categoría:** CSP
**Definición:** Palabra clave de la CSP que permite el JavaScript en línea y los manejadores de eventos. Anula casi todo el valor de la política contra el XSS; se evita con hash, nonce o archivos externos.
**En el curso:** La CSP del proyecto no la usa; el paso 4 muestra una política débil que sí.

---

## X

### XSS (Cross-Site Scripting)
**Categoría:** Ataques
**Definición:** Inyectar código JavaScript en una página que lo ejecuta con los permisos de esa página: lee `localStorage`, hace peticiones con la sesión, cambia lo que ve la persona. Hay tres tipos: almacenado, reflejado y basado en DOM.
**En el curso:** La práctica lo siembra en el laboratorio y mide qué se lleva.

### X-Content-Type-Options
**Categoría:** Cabeceras
**Definición:** Cabecera con un único valor documentado, `nosniff`, que impide que el navegador interprete un recurso con un tipo distinto del declarado.
**En el curso:** Va en todas las respuestas del servidor de laboratorio.

### X-Requested-By
**Categoría:** CSRF
**Definición:** Cabecera propia del proyecto, que el cliente manda en cada escritura y el servidor exige. Un formulario de otro sitio no puede ponerla, y `fetch` desde otro origen la sometería a un *preflight* que el servidor no autoriza.
**En el curso:** Es la defensa que detiene el experimento C.
