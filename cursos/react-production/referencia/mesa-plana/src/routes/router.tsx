import { createBrowserRouter, type RouteObject } from 'react-router';
import { LoginPage } from '../pages/login-page';
import { RutaProtegida } from './ruta-protegida';
import { TicketsPage } from '../pages/tickets-page';

export const rutas: RouteObject[] = [
  { path: '/entrar', element: <LoginPage /> },
  { element: <RutaProtegida />, children: [{ path: '/', element: <TicketsPage /> }] },
];

export const crearRouter = () => createBrowserRouter(rutas);
