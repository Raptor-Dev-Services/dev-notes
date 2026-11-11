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
