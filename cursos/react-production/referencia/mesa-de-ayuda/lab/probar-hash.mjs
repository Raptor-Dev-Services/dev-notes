// Laboratorio propio: ¿corre el script en línea de lab/hash.html con la CSP del servidor?
import { chromium } from '@playwright/test';
const b = await chromium.launch();
const p = await b.newPage();
p.on('console', (m) => console.log('[consola]', m.text().slice(0, 230)));
await p.goto('http://127.0.0.1:3110/lab/hash.html');
console.log('título:', await p.title());
await b.close();
