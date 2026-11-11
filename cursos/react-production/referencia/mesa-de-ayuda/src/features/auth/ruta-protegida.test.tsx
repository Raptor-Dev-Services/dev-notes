import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { http, HttpResponse } from 'msw';
import { servidor } from '../../test/servidor';
import { RutaProtegida } from './ruta-protegida';

function montar(rutaInicial: string) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const router = createMemoryRouter(
    [
      { path: '/entrar', element: <p>Pantalla de entrada</p> },
      { element: <RutaProtegida />, children: [{ path: '/', element: <p>Contenido privado</p> }] },
    ],
    { initialEntries: [rutaInicial] },
  );
  return render(
    <QueryClientProvider client={cliente}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

describe('RutaProtegida', () => {
  it('manda a /entrar cuando la API contesta 401', async () => {
    servidor.use(http.get('/api/me', () => HttpResponse.json({ codigo: 'sin-sesion', mensaje: 'Inicia sesión.' }, { status: 401 })));
    montar('/');
    expect(await screen.findByText('Pantalla de entrada')).toBeInTheDocument();
    expect(screen.queryByText('Contenido privado')).not.toBeInTheDocument();
  });

  it('muestra el contenido cuando hay sesión', async () => {
    servidor.use(http.get('/api/me', () => HttpResponse.json({ id: 1, nombre: 'Ana Ejemplo', email: 'ana@example.com' })));
    montar('/');
    expect(await screen.findByText('Contenido privado')).toBeInTheDocument();
  });

  it('distingue un fallo del servidor de "no hay sesión"', async () => {
    servidor.use(http.get('/api/me', () => HttpResponse.json({ codigo: 'interno', mensaje: 'Falla' }, { status: 500 })));
    montar('/');
    expect(await screen.findByRole('alert')).toHaveTextContent('No pudimos comprobar tu sesión.');
  });
});
