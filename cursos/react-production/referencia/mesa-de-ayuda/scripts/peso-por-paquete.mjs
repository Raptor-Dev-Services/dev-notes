// Peso aproximado por paquete de todo el JavaScript de dist/assets, a partir de sus sourcemaps.
//   npx vite build --sourcemap && node scripts/peso-por-paquete.mjs
// Es una estimación (reparte los bytes del archivo entre las fuentes que dicen los mapeos), útil para comparar, no para citar al byte.
import { readdirSync, readFileSync } from 'node:fs';

const B64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
function vlq(texto) {
  const salida = [];
  let desplazamiento = 0;
  let valor = 0;
  for (const c of texto) {
    const d = B64.indexOf(c);
    valor += (d & 31) << desplazamiento;
    if (d & 32) desplazamiento += 5;
    else {
      salida.push(valor & 1 ? -(valor >> 1) : valor >> 1);
      valor = desplazamiento = 0;
    }
  }
  return salida;
}

const archivos = readdirSync('dist/assets').filter((f) => f.endsWith('.js'));
if (archivos.length === 0) throw new Error('No hay dist/assets/*.js: corre npx vite build --sourcemap');
const porFuente = new Map();
for (const archivo of archivos) {
  const mapa = JSON.parse(readFileSync(`dist/assets/${archivo}.map`, 'utf8'));
  const lineas = readFileSync(`dist/assets/${archivo}`, 'utf8').split('\n');
  let fuente = 0;
  mapa.mappings.split(';').forEach((linea, n) => {
    if (!linea) return;
    let columna = 0;
    const trozos = [];
    for (const seg of linea.split(',')) {
      const v = vlq(seg);
      columna += v[0];
      if (v.length > 1) {
        fuente += v[1];
        trozos.push([columna, fuente]);
      }
    }
    const largo = lineas[n]?.length ?? 0;
    trozos.forEach(([col, f], i) => {
      const fin = trozos[i + 1]?.[0] ?? largo;
      const nombre = mapa.sources[f];
      porFuente.set(nombre, (porFuente.get(nombre) ?? 0) + fin - col);
    });
  });
}
const porPaquete = new Map();
for (const [nombre, bytes] of porFuente) {
  const m = [...nombre.matchAll(/node_modules\/((?:@[^/]+\/)?[^/]+)/g)].at(-1);
  const clave = m ? m[1] : 'src (código propio)';
  porPaquete.set(clave, (porPaquete.get(clave) ?? 0) + bytes);
}
for (const [paquete, bytes] of [...porPaquete].sort((a, b) => b[1] - a[1]).slice(0, 10)) {
  console.log(`${paquete.padEnd(32)}${(bytes / 1024).toFixed(1).padStart(8)} kB`);
}
