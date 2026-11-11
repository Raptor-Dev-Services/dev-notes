import { createBrowserRouter, type RouteObject } from 'react-router';
import { RutaProtegida } from '../features/auth';
import { Layout } from './layout';

// Cada pantalla se descarga cuando se visita: el código de la pantalla de tickets (con su formulario) no viaja en la de entrada.
export const rutas: RouteObject[] = [
  {
    element: <Layout />,
    children: [
      // eslint-disable-next-line boundaries/dependencies -- excepción de la sesión 5: lazy apunta al archivo de la pantalla
      { path: '/entrar', lazy: async () => ({ Component: (await import('../features/auth/login-page')).LoginPage }) },
      {
        element: <RutaProtegida />,
        children: [
          { path: '/', lazy: async () => ({ Component: (await import('../features/tickets')).TicketsPage }) },
          // eslint-disable-next-line boundaries/dependencies -- excepción de la sesión 5: lazy apunta al archivo de la pantalla
          { path: '/volumen', lazy: async () => ({ Component: (await import('../features/volumen/volumen-page')).VolumenPage }) },
        ],
      },
    ],
  },
];

export const crearRouter = () => createBrowserRouter(rutas);
