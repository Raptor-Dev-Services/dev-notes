# Código de referencia - React en producción con TypeScript

Todo en estas carpetas es un **ejemplo ilustrativo**: la *Mesa de ayuda*, sus tickets, "Ana Ejemplo" y las credenciales no existen y solo valen contra el servidor de laboratorio (`server/server.mjs`), que escucha en `127.0.0.1`.

| Carpeta | Qué es |
|---|---|
| `mesa-de-ayuda/` | El proyecto completo con lo que se construye en las seis sesiones |
| `mesa-plana/` | La misma aplicación organizada por tipo de archivo: el punto de partida de la práctica de la sesión 2 |

Requisitos: Node 22.22 o superior (a noviembre de 2026).

```bash
cd mesa-de-ayuda
npm ci
npm run typecheck && npm run lint && npm test
npm run build
npm run api          # servidor de laboratorio en http://127.0.0.1:3100 (sirve dist/)
npx playwright install chromium    # una vez, para las pruebas de punta a punta
npm run e2e
```

Notas:

- `src/features/tickets/domain.ts` (sesión 2) queda sin conectar a la pantalla: la práctica de la sesión 2 pide conectarlo con `select`, y después cada sesión cambia esa pantalla.
- `src/ejemplos/` y `src/practica/` guardan el código de las clases y prácticas de la sesión 1.
- `lab/` y `/lab/*` del servidor son **vulnerables a propósito** (sesión 4): se usan solo en `127.0.0.1`, en laboratorio propio.
- `Dockerfile`, `nginx.conf` y `.github/workflows/front.yml` son el despliegue de la sesión 6.

*Rogelio Arriaga Gonzalez*
