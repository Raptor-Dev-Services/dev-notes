// Compara lo que responde la API de verdad con el esquema del contrato.
//   API=http://127.0.0.1:3100 npx tsx scripts/verificar-contrato.ts [ruta]
import { paginaDeTicketsSchema } from '../src/shared/api/contract';

const base = process.env.API ?? 'http://127.0.0.1:3100';
const ruta = process.argv[2] ?? '/api/tickets';

const login = await fetch(`${base}/api/login`, {
  method: 'POST',
  headers: { 'content-type': 'application/json' },
  body: JSON.stringify({ email: 'ana@example.com', password: 'clave-de-ejemplo' }),
});
const cookie = login.headers.getSetCookie().map((c) => c.split(';')[0]).join('; ');

const respuesta = await fetch(`${base}${ruta}`, { headers: { cookie } });
const resultado = paginaDeTicketsSchema.safeParse(await respuesta.json());
if (!resultado.success) {
  for (const problema of resultado.error.issues) console.error(`${ruta} -> ${problema.path.join('.')}: ${problema.message}`);
  process.exit(1);
}
console.log(`${ruta}: ${resultado.data.total} tickets, contrato cumplido`);
