// API de laboratorio de la "Mesa de ayuda" (Ejemplo ilustrativo). Sin dependencias: solo Node.
// Sirve la API en /api y, si existe, la carpeta dist/ del front.
//   node server/server.mjs            (puerto 3100, o PORT)
// Variables: PORT, DIST_DIR (carpeta a servir; por omisión dist/), CSP (cabecera Content-Security-Policy del HTML), CACHE_ASSETS (Cache-Control de lo que no es HTML).
import { createServer } from 'node:http';
import { randomBytes } from 'node:crypto';
import { readFile, stat } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = fileURLToPath(new URL('..', import.meta.url));
const DIST = process.env.DIST_DIR ?? join(RAIZ, 'dist');
const PUERTO = Number(process.env.PORT ?? 3100);
const USUARIO = { id: 1, nombre: 'Ana Ejemplo', email: 'ana@example.com' };
const CLAVE = 'clave-de-ejemplo';
const CABECERA_CSRF = 'x-requested-by';
const EXIGE_CABECERA = process.env.CSRF !== 'off'; // CSRF=off apaga la defensa: solo para ver el ataque en la práctica

const sesiones = new Map(); // sid -> usuario
const tokens = new Map(); // token -> usuario (solo para el laboratorio inseguro de la sesión 4)
let siguienteId = 4;
const tickets = [
  { id: 1, titulo: 'La impresora del piso 2 no responde', prioridad: 'media', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' },
  { id: 2, titulo: 'Solicitar acceso a la carpeta de contabilidad', prioridad: 'baja', estado: 'abierto', creadoEn: '2026-11-03T16:30:00.000Z' },
  { id: 3, titulo: 'El correo no sincroniza en el teléfono', prioridad: 'alta', estado: 'cerrado', creadoEn: '2026-11-04T09:10:00.000Z' },
];

const TIPOS = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8', '.svg': 'image/svg+xml', '.json': 'application/json; charset=utf-8', '.map': 'application/json' };

const json = (res, estado, cuerpo, extra = {}) => {
  res.writeHead(estado, { 'content-type': 'application/json; charset=utf-8', ...extra });
  res.end(JSON.stringify(cuerpo));
};
const leerJson = async (req) => {
  const partes = [];
  for await (const p of req) partes.push(p);
  try { return JSON.parse(Buffer.concat(partes).toString('utf8') || '{}'); } catch { return null; }
};
const cookies = (req) => Object.fromEntries((req.headers.cookie ?? '').split(';').map((c) => c.trim().split('=')).filter((p) => p[0]));
const usuarioDe = (req) => {
  const auth = req.headers.authorization ?? '';
  if (auth.startsWith('Bearer ')) return tokens.get(auth.slice(7)) ?? null;
  return sesiones.get(cookies(req).sid) ?? null;
};

function validarTicket(c) {
  const errores = {};
  if (typeof c.titulo !== 'string' || c.titulo.trim().length < 5) errores.titulo = 'El título necesita al menos 5 caracteres.';
  if (!['baja', 'media', 'alta'].includes(c.prioridad)) errores.prioridad = 'Elige baja, media o alta.';
  return errores;
}

async function api(req, res, url) {
  const { pathname } = url;
  const metodo = req.method;
  if (metodo === 'POST' && pathname === '/api/login') {
    const c = await leerJson(req);
    if (!c || c.email !== USUARIO.email || c.password !== CLAVE) return json(res, 401, { codigo: 'credenciales', mensaje: 'Correo o contraseña incorrectos.' });
    const id = randomBytes(24).toString('base64url');
    if (url.searchParams.get('modo') === 'token') {
      tokens.set(id, USUARIO);
      return json(res, 200, { usuario: USUARIO, token: id });
    }
    sesiones.set(id, USUARIO);
    return json(res, 200, { usuario: USUARIO }, { 'set-cookie': `sid=${id}; HttpOnly; SameSite=Lax; Path=/; Max-Age=3600` });
  }
  const usuario = usuarioDe(req);
  if (metodo === 'POST' && pathname === '/api/logout') {
    sesiones.delete(cookies(req).sid);
    return json(res, 200, { ok: true }, { 'set-cookie': 'sid=; HttpOnly; SameSite=Lax; Path=/; Max-Age=0' });
  }
  if (!usuario) return json(res, 401, { codigo: 'sin-sesion', mensaje: 'Inicia sesión.' });
  if (EXIGE_CABECERA && !['GET', 'HEAD'].includes(metodo ?? '') && !req.headers[CABECERA_CSRF]) {
    return json(res, 403, { codigo: 'csrf', mensaje: 'Falta la cabecera X-Requested-By.' });
  }
  if (metodo === 'GET' && pathname === '/api/me') return json(res, 200, usuario);
  if (metodo === 'GET' && pathname === '/api/tickets') {
    const demora = Number(url.searchParams.get('demora') ?? 0);
    if (demora > 0) await new Promise((r) => setTimeout(r, demora));
    if (url.searchParams.get('falla') === '1') return json(res, 500, { codigo: 'interno', mensaje: 'Falla provocada para la práctica.' });
    const estado = url.searchParams.get('estado');
    const lista = estado ? tickets.filter((t) => t.estado === estado) : tickets;
    if (url.searchParams.get('contrato') === 'roto') {
      // Para la práctica de la sesión 1: el servidor se sale del contrato y renombra un campo.
      return json(res, 200, { datos: lista.map(({ prioridad, ...resto }) => ({ ...resto, urgencia: prioridad })), total: lista.length });
    }
    return json(res, 200, { datos: lista, total: lista.length });
  }
  if (metodo === 'POST' && pathname === '/api/tickets') {
    const c = await leerJson(req);
    if (!c) return json(res, 400, { codigo: 'json', mensaje: 'El cuerpo no es JSON.' });
    const errores = validarTicket(c);
    if (Object.keys(errores).length) return json(res, 422, { codigo: 'validacion', mensaje: 'Revisa los campos.', errores });
    const t = { id: siguienteId++, titulo: c.titulo.trim(), prioridad: c.prioridad, estado: 'abierto', creadoEn: new Date().toISOString() };
    tickets.push(t);
    return json(res, 201, t);
  }
  const m = pathname.match(/^\/api\/tickets\/(\d+)$/);
  if (m && metodo === 'PATCH') {
    const t = tickets.find((x) => x.id === Number(m[1]));
    if (!t) return json(res, 404, { codigo: 'no-existe', mensaje: 'No existe ese ticket.' });
    const c = await leerJson(req);
    if (!c || !['abierto', 'cerrado'].includes(c.estado)) return json(res, 422, { codigo: 'validacion', mensaje: 'Revisa los campos.', errores: { estado: 'Estado inválido.' } });
    if (url.searchParams.get('falla') === '1') return json(res, 500, { codigo: 'interno', mensaje: 'Falla provocada para la práctica.' });
    t.estado = c.estado;
    return json(res, 200, t);
  }
  if (m && metodo === 'GET') {
    const t = tickets.find((x) => x.id === Number(m[1]));
    return t ? json(res, 200, t) : json(res, 404, { codigo: 'no-existe', mensaje: 'No existe ese ticket.' });
  }
  return json(res, 404, { codigo: 'no-existe', mensaje: 'Ruta desconocida.' });
}

async function laboratorio(res, url) {
  // Páginas de práctica de la sesión 4 (carpeta lab/). Solo para el laboratorio propio.
  const nombre = normalize(url.pathname.slice('/lab/'.length)).replace(/^(\.\.[/\\])+/, '');
  try {
    const datos = await readFile(join(RAIZ, 'lab', nombre));
    const cabeceras = { 'content-type': TIPOS[extname(nombre)] ?? 'text/plain', 'cache-control': 'no-store' };
    if (process.env.CSP) cabeceras['content-security-policy'] = process.env.CSP;
    res.writeHead(200, cabeceras);
    res.end(datos);
  } catch {
    res.writeHead(404);
    res.end('No existe');
  }
}

async function estatico(req, res, url) {
  if (url.pathname.startsWith('/lab/')) return laboratorio(res, url);
  let ruta = normalize(decodeURIComponent(url.pathname)).replace(/^(\.\.[/\\])+/, '');
  let archivo = join(DIST, ruta);
  let datos;
  try {
    if ((await stat(archivo)).isDirectory()) archivo = join(archivo, 'index.html');
    datos = await readFile(archivo);
  } catch {
    archivo = join(DIST, 'index.html'); // fallback de SPA
    try { datos = await readFile(archivo); } catch { res.writeHead(404); return res.end('Sin dist/: corre npm run build'); }
  }
  const tipo = TIPOS[extname(archivo)] ?? 'application/octet-stream';
  const cabeceras = { 'content-type': tipo, 'x-content-type-options': 'nosniff', 'referrer-policy': 'strict-origin-when-cross-origin' };
  if (archivo.endsWith('index.html')) {
    cabeceras['cache-control'] = 'no-cache'; // el HTML se revalida siempre; los assets llevan huella en su nombre
    if (process.env.CSP) cabeceras['content-security-policy'] = process.env.CSP;
  } else {
    cabeceras['cache-control'] = process.env.CACHE_ASSETS ?? 'public, max-age=31536000, immutable';
  }
  res.writeHead(200, cabeceras);
  res.end(datos);
}

createServer(async (req, res) => {
  const url = new URL(req.url ?? '/', 'http://localhost');
  try {
    if (url.pathname.startsWith('/api/')) return await api(req, res, url);
    return await estatico(req, res, url);
  } catch (e) {
    console.error(e);
    json(res, 500, { codigo: 'interno', mensaje: 'Error del servidor.' });
  }
}).listen(PUERTO, '127.0.0.1', () => console.log(`Mesa de ayuda (laboratorio) en http://127.0.0.1:${PUERTO}`));
