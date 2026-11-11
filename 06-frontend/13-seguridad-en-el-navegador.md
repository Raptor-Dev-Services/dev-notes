# 13 - Seguridad en el navegador - sesión, XSS, CSRF y CSP

## Problema que resuelve

Un front de React corre en un entorno que no controlas: el navegador de otra persona, con extensiones, con dependencias de terceros y con datos que alguien más escribió. Si la sesión vive en `localStorage`, cualquier script que corra en la página (una librería comprometida, una inyección, una extensión) puede llevársela. Esta nota explica **dónde guardar la sesión**, **qué defensas se apilan** contra XSS y CSRF, y cómo **demostrar** que cada una funciona.

El ejemplo es la **Mesa de ayuda** (**Ejemplo ilustrativo**: datos, correo y contraseña inventados), un front de React con una API de laboratorio. Todo el código y las salidas de esta nota se ejecutaron el 2026-11-11 con React 19.3.1, Vite 8.3.8, DOMPurify 3.4.17 y Playwright 1.65 (Chromium 158). Lo que cambia con las versiones lleva su fecha.

> **Marco ético.** Las pruebas ofensivas de esta nota se hacen **solo** contra un laboratorio propio, en `127.0.0.1`. Nunca contra un sistema ajeno ni sin autorización escrita de su dueño.

## 1. Dónde vive la sesión

| | Cookie `HttpOnly` | Token en `localStorage` |
|---|---|---|
| Lo guarda | El navegador (`Set-Cookie`) | Tu JavaScript |
| Lo presenta | El navegador, solo | Tu JavaScript (`Authorization: Bearer`) |
| Un script de la página lo lee | **No** | **Sí** |
| Exige pensar en | CSRF | XSS, que lo roba |

OWASP lo dice sin rodeos: no se guardan identificadores de sesión en `localStorage`, porque JavaScript siempre puede leerlos, y un solo XSS basta para llevarse todo lo que haya ahí. MDN agrega que `HttpOnly` impide leer la cookie con `document.cookie` y ayuda a mitigar el XSS.

La API del ejemplo abre la sesión así:

```text
set-cookie: sid=<valor>; HttpOnly; SameSite=Lax; Path=/; Max-Age=3600
```

El cuerpo de la respuesta **no** trae el identificador. El front solo pregunta quién es:

```ts
export function useSesion() {
  return useQuery({
    queryKey: sesionKey,
    queryFn: async ({ signal }) => {
      try {
        return await authApi.yo(signal);
      } catch (e) {
        if (e instanceof ApiError && e.estado === 401) return null;
        throw e;
      }
    },
    staleTime: 60_000,
  });
}
```

Un 401 significa "nadie", no "error": el hook devuelve `null` y la ruta protegida redirige a `/entrar`.

### Los atributos de la cookie

| Atributo | Qué hace |
|---|---|
| `HttpOnly` | Los scripts no la leen |
| `Secure` | Solo viaja por HTTPS (obligatorio en producción) |
| `SameSite=Lax` | No se manda en peticiones de otros sitios, salvo al navegar hacia el sitio. Si no se declara, el navegador la trata como `Lax` |
| `SameSite=None` | Se manda siempre; **exige** `Secure` |
| Prefijo `__Host-` | Obliga `Secure`, `Path=/` y sin `Domain`: solo se manda al host que la puso |

En producción: `Set-Cookie: __Host-sid=...; Secure; HttpOnly; SameSite=Lax; Path=/`. (Fragmento: el laboratorio usa HTTP y no puede llevar `Secure`.)

### Lo que `HttpOnly` no impide

Impide **leer** la cookie, no **usarla**. Con la sesión en cookie y un XSS, el script inyectado hace `fetch('/api/tickets')` y el navegador adjunta la cookie. En el laboratorio, con la misma inyección:

```text
node lab/probar-xss.mjs token    ->  [atacante] GET /robo?token=t2V-eJl1H2b42vOPbmoIxNj7xqMunN8c
node lab/probar-xss.mjs cookie   ->  [atacante] GET /robo?cookie=
node lab/probar-xss.mjs datos    ->  [atacante] GET /robo?datos=%7B%22datos%22%3A%5B%7B%22id%22%3A1...
```

El token sale; la cookie no; pero los **datos** de la víctima salen igual. Por eso hay tres capas: evitar la inyección, limitar lo que el script inyectado puede hacer (CSP) y acotar el daño (`HttpOnly`).

## 2. XSS: de dónde viene y cómo se evita

**Tres tipos:** almacenado (la carga está guardada en el servidor), reflejado (viaja en la petición actual) y basado en DOM (el JavaScript del cliente la toma de la URL o del almacenamiento).

### React escapa por defecto

```tsx
const ataque = '<img src=x onerror="alert(1)"><b>negritas</b><script>alert(2)</script>';

renderToStaticMarkup(<p>{ataque}</p>);
// <p>&lt;img src=x onerror=&quot;alert(1)&quot;&gt;&lt;b&gt;negritas&lt;/b&gt;&lt;script&gt;alert(2)&lt;/script&gt;</p>

renderToStaticMarkup(<p dangerouslySetInnerHTML={{ __html: ataque }} />);
// contiene onerror="alert(1)"
```

Interpolar con llaves convierte el texto en texto. `dangerouslySetInnerHTML` inserta el HTML tal cual; la documentación de React lo advierte: solo con datos confiables y sanitizados.

### Los sinks

| Sink | Alternativa |
|---|---|
| `dangerouslySetInnerHTML`, `innerHTML` | Texto con llaves o `textContent`; si hace falta HTML, sanitizar |
| `href`, `src` con datos del usuario | Validar que el esquema sea `http:` o `https:` |
| `eval`, `new Function`, `setTimeout("texto")`, `document.write` | No usarlos |
| Salida HTML de una biblioteca de Markdown | Sanitizar |

React 19 bloquea un `href` con `javascript:` (verificado con `react-dom` 19.3.1: lo sustituye por un código que lanza un error). OWASP aclara que ese bloqueo no sustituye la validación de URL de tu aplicación:

```ts
const esUrlSegura = (valor: string) => {
  try {
    return ['http:', 'https:'].includes(new URL(valor).protocol);
  } catch {
    return false;
  }
};
```

(Fragmento de ejemplo.)

### Sanitizar con DOMPurify, en un solo lugar

```tsx
import DOMPurify from 'dompurify';

/** El único lugar de la app que usa dangerouslySetInnerHTML, y siempre después de sanitizar. */
export function HtmlSeguro({ html }: Props) {
  const limpio = DOMPurify.sanitize(html, { ALLOWED_TAGS: ['b', 'i', 'em', 'strong', 'p', 'ul', 'ol', 'li', 'br'], ALLOWED_ATTR: [] });
  return <div dangerouslySetInnerHTML={{ __html: limpio }} />;
}
```

Lista de **permitidos**, no de prohibidos. Resultado real:

| Entrada | `container.innerHTML` |
|---|---|
| `<img src=x onerror="alert(1)"><b>negritas</b><script>alert(2)</script>` | `<div><b>negritas</b></div>` |
| `<a href="javascript:alert(1)">clic</a><p>texto</p>` | `<div>clic<p>texto</p></div>` |

Nunca con una expresión regular: HTML no es un lenguaje regular y los navegadores aceptan HTML malformado. Mantén DOMPurify actualizado como cualquier dependencia de seguridad.

## 3. CSRF: usar tu sesión desde otro sitio

Con la sesión en cookie, una página ajena puede mandar un formulario a tu API y el navegador adjuntará la cookie. El atacante no lee la respuesta, pero logra la acción. Las defensas se apilan:

1. **`SameSite`** en la cookie. El "sitio" es el dominio registrable y **el puerto no cuenta**: `127.0.0.1:3110` y `127.0.0.1:3119` son el mismo sitio (aunque distinto origen). OWASP lo trata como defensa en profundidad: no sustituye una defensa propia, y `Lax` solo cubre métodos no seguros.
2. **Cabecera personalizada** en cada escritura. Un formulario HTML no puede ponerla, y un `fetch` de otro origen necesita un *preflight* que el servidor no autoriza.

```ts
headers: { accept: 'application/json', 'x-requested-by': 'mesa-de-ayuda', /* ... */ },
```

```js
if (EXIGE_CABECERA && !['GET', 'HEAD'].includes(metodo) && !req.headers['x-requested-by']) {
  return json(res, 403, { codigo: 'csrf', mensaje: 'Falta la cabecera X-Requested-By.' });
}
```

3. **Verificar el `Origin`** (o los metadatos `Sec-Fetch-Site`) en el servidor.
4. Un **token anti-CSRF** cuando se necesita más.

Tres experimentos en el laboratorio (un formulario de otro puerto manda un POST con un JSON en `text/plain`):

| | Cookie | Cabecera obligatoria | Respuesta |
|---|---|---|---|
| A. Otro sitio (`localhost` frente a `127.0.0.1`) | no se manda | apagada | `{"codigo":"sin-sesion",...}` |
| B. Mismo sitio, otro puerto | se manda | **apagada** | el ticket queda creado |
| C. Mismo sitio, otro puerto | se manda | **encendida** | `{"codigo":"csrf","mensaje":"Falta la cabecera X-Requested-By."}` |

OWASP recomienda no aceptar `text/plain` como tipo de contenido de una API. Y una advertencia que ordena el trabajo: **un XSS derrota todas las defensas contra CSRF**, porque el script corre dentro del origen. Primero no tener XSS.

**CORS en una línea:** es la política del navegador sobre quién *lee* la respuesta de otro origen; una petición simple llega al servidor aunque no la pueda leer; con cookies, el servidor no puede responder `Access-Control-Allow-Origin: *`. La opción más simple es que la SPA y la API **compartan origen**, como en este ejemplo (proxy de Vite en desarrollo, mismo servidor en producción).

## 4. Content Security Policy

Una cabecera `Content-Security-Policy` que dice desde dónde puede cargar y ejecutar cosas la página. Con `default-src` o `script-src` definidos, el JavaScript en línea queda bloqueado. MDN la presenta como defensa contra XSS **y** como complemento: no reemplaza sanitizar la entrada.

La política del ejemplo:

```text
default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self';
object-src 'none'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'
```

Un build de Vite genera un HTML con un solo `<script type="module" src="/assets/...">`: sin código en línea. Con esta política corren las pruebas de punta a punta del proyecto (Playwright 1.65), y una de ellas demuestra que un script en línea inyectado no corre.

### Lo que se ve al bloquear (Chromium 158)

```text
Executing inline event handler violates the following Content Security Policy directive 'script-src 'self''. Either the 'unsafe-inline' keyword, a hash ('sha256-...'), or a nonce ('nonce-...') is required to enable inline execution. Note that hashes do not apply to event handlers, style attributes and javascript: navigations unless the 'unsafe-hashes' keyword is present. The action has been blocked.
```

El texto exacto depende del navegador. Con una política más débil (`script-src 'self' 'unsafe-inline'; connect-src 'self'`) el script inyectado corre, pero la salida del dato la corta `connect-src`:

```text
Connecting to 'http://127.0.0.1:3119/robo?token=...' violates the following Content Security Policy directive: "connect-src 'self'". The action has been blocked.
```

### Sin `unsafe-inline`

- **Hash:** el servidor declara el SHA-256 (384 o 512) del contenido del script. Solo corre ese texto exacto. Probado: con el hash correcto el script corre (`título: ejecutado`); con un comentario agregado, no (`título: sin ejecutar`).
- **Nonce:** un valor distinto y no adivinable en cada respuesta, en la cabecera y en la etiqueta.
- MDN: los desarrolladores deben evitar `'unsafe-inline'`, porque anula gran parte del propósito de una CSP. Si una directiva trae nonce o hash, el navegador ignora `unsafe-inline`.
- Los hashes no aplican a manejadores de eventos (`onclick=`) ni a atributos `style`, salvo con `'unsafe-hashes'`: la solución es mover ese código a un archivo.

### Cómo desplegarla

1. `Content-Security-Policy-Report-Only`: no bloquea, solo avisa.
2. Recorre la aplicación completa y arregla lo que aparezca.
3. Cambia a `Content-Security-Policy`.

Se entrega por **cabecera**: una etiqueta `<meta>` no admite todas las funciones (el modo de solo reporte no puede ir en `<meta>`).

## 5. Otras cabeceras

| Cabecera | Valor | Qué hace |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | El navegador no adivina el tipo; un script o una hoja de estilo con MIME equivocado se bloquea |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Es el valor por omisión de los navegadores modernos (a noviembre de 2026); declararlo hace explícita la intención |
| `frame-ancestors` (en la CSP) | `'none'` | Nadie incrusta tu página |

## 6. Variables y almacenamiento

- Todo lo que empieza con `VITE_` se incrusta en el JavaScript del cliente. Vite lo advierte: esas variables **no** deben contener información sensible, como claves de API. Lo secreto vive en el servidor (ver la nota `01-variables-entorno-vite.md`).
- No guardes contraseñas, tokens ni datos personales en `localStorage` ni `sessionStorage`: cualquier script de la página los lee. Un borrador de un formulario sin secretos es otra cosa.

## Qué evita

| Sin esto | Con esto |
|---|---|
| El token en `localStorage` se va con un solo XSS | La cookie `HttpOnly` no se lee desde la página |
| `<img onerror>` en un comentario ejecuta código | React escapa; `HtmlSeguro` sanitiza el HTML permitido |
| Un formulario ajeno planta un ticket | `SameSite` y la cabecera `X-Requested-By` lo detienen |
| El script inyectado manda los datos a otro servidor | `connect-src 'self'` lo bloquea |
| Un script en línea nuevo corre | La CSP sin `unsafe-inline` lo bloquea |

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Una SPA que habla con su propio backend: cookie `HttpOnly` + CSP + cabecera de CSRF | Una API consumida por una app móvil o por otro servidor: ahí un token en el encabezado tiene sentido |
| `HtmlSeguro` cuando de verdad se renderiza HTML ajeno | `dangerouslySetInnerHTML` "para dar formato" a texto plano: usa estructura de JSX |
| CSP con `Report-Only` antes de bloquear | Una CSP con `unsafe-inline` "para que funcione": protege poco |
| Las pruebas de ataque en un laboratorio propio | Probar cargas contra un sistema ajeno o sin autorización escrita |

## Fuentes

- OWASP Cheat Sheet Series: *HTML5 Security*, *Cross Site Scripting Prevention*, *Cross-Site Request Forgery Prevention* (cheatsheetseries.owasp.org), leídas el 2026-11-11.
- MDN: *Using HTTP cookies*; *Content Security Policy (CSP)*; *Cross-Origin Resource Sharing (CORS)*; glosario *Site*; `Referrer-Policy`; `X-Content-Type-Options` (developer.mozilla.org), leídos el 2026-11-11.
- React, documentación de componentes comunes (`dangerouslySetInnerHTML`) (react.dev). Vite, *Env Variables and Modes* (vite.dev/guide/env-and-mode).
- *Web Application Security*, 2.ª ed. (Andrew Hoffman, 2023): cap. 10 (Cross-Site Scripting), cap. 11 (Cross-Site Request Forgery), cap. 28 (Defending Against XSS Attacks), cap. 29 (Defending Against CSRF Attacks).
- *React Application Architecture for Production* (Alan Alickovic, Anthony Alicea, 2026): cap. 7 (Implementing Authentication and Securing the Application).
- Código y salidas: proyecto de referencia `mesa-de-ayuda` del curso (Ejemplo ilustrativo), ejecutado el 2026-11-11.

## Glosario

- **XSS**: inyección de JavaScript que corre con los permisos de la página.
- **CSRF**: hacer que el navegador de una persona con sesión mande una petición que ella no quiso.
- **CSP**: cabecera que limita desde dónde se carga y ejecuta contenido.
- **HttpOnly**: atributo de cookie que impide leerla con JavaScript.
- **SameSite**: atributo de cookie que decide si se manda en peticiones de otro sitio; el puerto no cuenta.
- **Sink**: lugar del código donde un dato se interpreta como ejecutable.
- **Preflight**: petición `OPTIONS` previa a una petición "no simple" de otro origen.

---

*Rogelio Arriaga Gonzalez*
