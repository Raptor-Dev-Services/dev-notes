// Página de laboratorio. Dos defectos a propósito:
//  1) guarda un token de sesión en localStorage;
//  2) pinta el parámetro ?q= con innerHTML (un "sink" de XSS).
const estado = document.getElementById('estado');
const cabecera = { 'content-type': 'application/json', 'x-requested-by': 'laboratorio' };
const credenciales = JSON.stringify({ email: 'ana@example.com', password: 'clave-de-ejemplo' });

document.getElementById('entrar-token').addEventListener('click', async () => {
  const r = await fetch('/api/login?modo=token', { method: 'POST', headers: cabecera, body: credenciales });
  const { token } = await r.json();
  localStorage.setItem('token', token);
  estado.textContent = 'Sesión con token en localStorage';
});

document.getElementById('entrar-cookie').addEventListener('click', async () => {
  await fetch('/api/login', { method: 'POST', headers: cabecera, body: credenciales });
  localStorage.removeItem('token');
  estado.textContent = 'Sesión con cookie HttpOnly';
});

const q = new URLSearchParams(location.search).get('q');
if (q !== null) {
  // DEFECTO: el texto de la URL entra como HTML.
  document.getElementById('resultados').innerHTML = 'Resultados para: ' + q;
}
