# 15 - Caché y versionado de assets - que el usuario reciba lo que desplegaste

## Problema que resuelve

Arreglas un error, el pipeline sale en verde, despliegas... y el usuario sigue viendo el error. El arreglo existe solo en el servidor: el navegador conserva el JavaScript viejo porque una cabecera le dijo que lo guardara cuatro horas, un año o hasta nuevo aviso.

Esta nota explica qué dice cada directiva de `Cache-Control`, la receta que evita el problema (huella en el nombre del archivo, cabeceras distintas para el HTML y para los assets) y cómo **reproducir y comprobar** el fallo en un navegador real.

> Los comandos y las salidas son reales, del proyecto de ejemplo del curso "React en producción con TypeScript" (la "Mesa de ayuda", un **Ejemplo ilustrativo**): Vite 8.3.8, Playwright 1.65.0 con Chromium 158 y nginx 1.31.8, a noviembre de 2026.

---

## Lo que dice cada directiva

| Directiva | Significado |
|-----------|-------------|
| `max-age=N` | la respuesta es fresca hasta N segundos después de que el origen la generó |
| `no-cache` | se puede guardar, pero hay que **revalidar con el servidor antes de cada uso**. No significa "no guardar" |
| `no-store` | no guardar la respuesta |
| `public` | puede guardarla también una caché compartida |
| `immutable` | la respuesta no cambiará mientras sea fresca: no hace falta revalidarla ni al recargar |

`immutable` es la extensión del RFC 8246, "HTTP Immutable Responses" (IETF, septiembre de 2017): permite a los servidores identificar recursos que no se actualizarán durante su vida de frescura, de modo que los clientes no los revalidan.

Si una respuesta no lleva ninguna directiva, una caché puede decidir por su cuenta cuánto guardarla: MDN avisa que si hay un requisito de caché hay que indicarlo explícitamente en `Cache-Control`.

---

## La receta

| Archivo | `Cache-Control` | Por qué |
|---------|-----------------|---------|
| `/assets/*` (nombre con huella) | `public, max-age=31536000, immutable` | su contenido **nunca** cambia en esa URL |
| `index.html` | `no-cache` | siempre se revalida, y por eso apunta a los assets nuevos |

La guía de MDN lo dice así: con assets versionados, un `max-age` largo con `immutable` es seguro porque el contenido nunca cambiará, y el HTML con `no-cache` se revalida para que el cliente reciba la versión nueva del HTML y de los assets.

### Lo que produce Vite

```bash
npx vite build
```

```text
dist/index.html                         0.56 kB
dist/assets/jsx-runtime-Wy9s6hX0.js     8.91 kB
dist/assets/tickets-CbCN6d0C.js        41.97 kB
dist/assets/contract-BO96gj8c.js      167.23 kB
dist/assets/index-WeLmJLk9.js         233.90 kB
```

El nombre lleva una huella del contenido. Si el contenido cambia, el nombre cambia, y `index.html` (el único con nombre fijo) apunta a los nuevos:

```html
<script type="module" crossorigin src="/assets/index-WeLmJLk9.js"></script>
```

### Las cabeceras del servidor de ejemplo

```text
$ curl -sI http://127.0.0.1:3120/
cache-control: no-cache
$ curl -sI http://127.0.0.1:3120/assets/index-WeLmJLk9.js
cache-control: public, max-age=31536000, immutable
```

---

## Revalidar: ETag y 304

`no-cache` no obliga a bajar el archivo de nuevo: obliga a **preguntar**. Con nginx en un contenedor:

```text
$ curl -sI http://127.0.0.1:3125/
ETag: "6af4789d-236"
Last-Modified: Wed, 11 Nov 2026 14:15:57 GMT
Cache-Control: no-cache
$ curl -sI -H 'If-None-Match: "6af4789d-236"' http://127.0.0.1:3125/
HTTP/1.1 304 Not Modified
Cache-Control: no-cache
```

Si el HTML no cambió, el servidor contesta **304 sin cuerpo**. Si cambió, contesta 200 con el HTML nuevo, que apunta a assets con huella nueva.

---

## La trampa: un asset sin huella

```html
<script src="/app.js"></script>
```

El nombre nunca cambia. Con `Cache-Control: public, max-age=14400` (cuatro horas):

1. El navegador baja `app.js` y lo guarda.
2. Despliegas una versión nueva con el mismo nombre.
3. El navegador no pregunta: su copia sigue fresca.

El HTML nuevo llega y el JavaScript viejo se queda.

### Reproducirlo en Chromium

El "despliegue" lo simula un script que escribe una carpeta con la versión pedida:

```js
// Simula un "despliegue" del sitio de demostración de caché: escribe demo-cache-dist/ con la versión pedida.
//   node scripts/desplegar-demo.mjs 1            (app.js sin huella: <script src="/app.js">)
//   node scripts/desplegar-demo.mjs 2 --huella   (app.js?v=<hash del contenido>)
import { createHash } from 'node:crypto';
import { mkdirSync, writeFileSync } from 'node:fs';

const version = process.argv[2] ?? '1';
const conHuella = process.argv.includes('--huella');
const js = `document.getElementById('version').textContent = 'JavaScript versión ${version}';\n`;
const huella = createHash('sha256').update(js).digest('hex').slice(0, 8);
const src = conHuella ? `/app.js?v=${huella}` : '/app.js';
const html = `<!doctype html>\n<html lang="es"><head><meta charset="utf-8"><title>Demo de caché</title></head>\n<body><p id="version">sin cargar</p><script src="${src}"></script></body></html>\n`;
mkdirSync('demo-cache-dist', { recursive: true });
writeFileSync('demo-cache-dist/app.js', js);
writeFileSync('demo-cache-dist/index.html', html);
console.log(`desplegada la versión ${version}: ${src}`);
```

Y una prueba de Playwright que despliega la versión 1, visita, despliega la 2 y vuelve a visitar:

```ts
import { execFileSync } from 'node:child_process';
import { expect, test } from '@playwright/test';

// Este archivo NO corre con el resto (usa su propio servidor): npx playwright test --config=playwright.cache.config.ts
const desplegar = (...args: string[]) => execFileSync('node', ['scripts/desplegar-demo.mjs', ...args], { encoding: 'utf8' }).trim();

test('sin huella: el navegador sigue con el JavaScript viejo hasta que vence su caché', async ({ page }) => {
  console.log(desplegar('1'));
  await page.goto('/');
  await expect(page.locator('#version')).toHaveText('JavaScript versión 1');

  console.log(desplegar('2')); // el "despliegue": app.js ahora dice 2, pero la URL es la misma
  await page.goto('/');
  console.log('VISTO sin huella:', await page.locator('#version').textContent());
  await expect(page.locator('#version')).toHaveText('JavaScript versión 1'); // el navegador usó su copia
});

test('con huella en ?v=: el HTML nuevo apunta a una URL nueva y el navegador la pide', async ({ page }) => {
  console.log(desplegar('1', '--huella'));
  await page.goto('/');
  await expect(page.locator('#version')).toHaveText('JavaScript versión 1');

  console.log(desplegar('2', '--huella'));
  await page.goto('/');
  console.log('VISTO con huella:', await page.locator('#version').textContent());
  await expect(page.locator('#version')).toHaveText('JavaScript versión 2');
});
```

Salida real:

```text
desplegada la versión 1: /app.js
desplegada la versión 2: /app.js
VISTO sin huella: JavaScript versión 1
```

Y con la huella en `?v=`:

```text
desplegada la versión 1: /app.js?v=0f3a6a8b
desplegada la versión 2: /app.js?v=ed916442
VISTO con huella: JavaScript versión 2
```

El HTML (con `no-cache`) llega siempre fresco y apunta a `/app.js?v=ed916442`, una URL que ninguna caché había visto.

---

## Huella en el nombre o en `?v=`

| Opción | Ventaja | Límite |
|--------|---------|--------|
| Nombre con huella (`index-WeLmJLk9.js`) | lo hace Vite solo | requiere un build que lo genere |
| Huella en `?v=` | se agrega a un sitio que ya existe sin renombrar archivos | una caché que ignore la cadena de consulta no distinguiría versiones |

La regla de fondo es la misma: **la URL cambia cuando el contenido cambia**.

---

## El caso real: la academia

En la propia academia, Cloudflare guardaba el JavaScript viejo hasta 4 horas. Se resolvió con una huella del contenido en `?v=`.

La documentación de Cloudflare dice que el Browser Cache TTL por omisión es de **4 horas** en todos los planes, y que con la opción "Respect Existing Headers" no sobrescribe ni inserta `Cache-Control` salvo que una regla lo haga. El caso es consistente con ese valor por omisión; no se detalla aquí la configuración exacta que había en ese momento.

---

## En nginx

```nginx
server {
  listen 80;
  root /usr/share/nginx/html;
  index index.html;

  gzip on;
  gzip_types text/css text/javascript application/javascript application/json image/svg+xml;

  # Assets con huella en el nombre (Vite): el contenido de esa URL nunca cambia.
  location /assets/ {
    add_header Cache-Control "public, max-age=31536000, immutable";
    try_files $uri =404;
  }

  # Todo lo demás (el HTML y las rutas de la SPA): revalidar siempre.
  location / {
    add_header Cache-Control "no-cache";
    try_files $uri /index.html;
  }

  # Que la API vaya al backend (ajusta el destino a tu red).
  location /api/ {
    proxy_pass http://api:3100;
  }
}
```

- `/assets/` con `immutable`; lo demás con `no-cache`, y `try_files ... /index.html` para que las rutas de la SPA devuelvan el HTML.
- Probado con `nginx:1.31.8-alpine`: `nginx -t` dice `syntax is ok`.
- **`add_header` dentro de un `location` reemplaza los heredados del nivel superior.** Si pones una cabecera de seguridad en `server`, repítela en cada `location` que defina las suyas.
- **`proxy_pass http://api:3100;` resuelve `api` al arrancar.** Sin esa red, el contenedor no arranca: `nginx: [emerg] host not found in upstream "api" in /etc/nginx/conf.d/default.conf:23`.

---

## Cómo comprobar que funciona

1. `curl -sI` del HTML y de un asset: ¿las cabeceras son las de la receta?
2. Una prueba (como la de arriba) que despliega dos versiones y mira qué corre.
3. Después de cada cambio de infraestructura (CDN, nginx, proxy), repetir 1.

Y una regla de higiene: las variables `VITE_` se incrustan en el JavaScript al compilar (nota `01-variables-entorno-vite.md`); cambiarlas en el servidor sin recompilar no cambia nada en el navegador.

---

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Nombres con huella y `immutable` para los assets del build | `immutable` en un archivo cuyo nombre no cambia |
| `no-cache` en el HTML de una SPA | `max-age` largo en el HTML |
| `?v=<huella>` para assets sueltos de un sitio que ya existe | `?v=<fecha de despliegue>` si el contenido no cambió: invalida la caché sin necesidad |
| Una prueba que despliega dos versiones y compara | "Limpia la caché" como procedimiento de despliegue |

> Fuente: MDN, "Cache-Control" (incluye "Caching static assets with cache busting"); RFC 8246, "HTTP Immutable Responses" (septiembre de 2017); documentación de Cloudflare, "Edge Cache TTL and Browser Cache TTL" (a noviembre de 2026); documentación de Vite y de nginx. Libro: *React Application Architecture for Production* (Alickovic y Alicea, 2026), cap. 12.

---

## Glosario

| Término | Significado |
|---------|-------------|
| Huella (hash de contenido) | cadena derivada del contenido de un archivo; cambia si el contenido cambia |
| Cache busting | hacer que la URL cambie cuando cambia el contenido |
| Revalidación | preguntar al servidor si la copia guardada sigue vigente; si sí, 304 |
| ETag | cabecera que identifica una versión de un recurso |
| `immutable` | directiva que dice que la respuesta no cambiará mientras sea fresca |

---

*Rogelio Arriaga Gonzalez*
