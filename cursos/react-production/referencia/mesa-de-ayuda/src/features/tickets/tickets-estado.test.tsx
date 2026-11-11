import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { renderConProveedores } from '../../test/render';
import { servidor } from '../../test/servidor';
import { TicketsPage } from './tickets-page';

describe('estado de la pantalla', () => {
  it('el filtro sale de la URL y viaja a la API', async () => {
    let recibido: string | null = null;
    servidor.use(
      http.get('/api/tickets', ({ request }) => {
        recibido = new URL(request.url).searchParams.get('estado');
        return HttpResponse.json({ datos: [], total: 0 });
      }),
    );
    renderConProveedores(<TicketsPage />, '/?estado=cerrado');
    expect(await screen.findByText('No hay tickets con ese filtro.')).toBeInTheDocument();
    expect(recibido).toBe('cerrado');
    expect(screen.getByLabelText('Estado')).toHaveValue('cerrado');
  });

  it('al cambiar el filtro se pide y se muestra la lista de ese filtro', async () => {
    servidor.use(
      http.get('/api/tickets', ({ request }) => {
        const estado = new URL(request.url).searchParams.get('estado');
        const datos = estado === 'cerrado'
          ? [{ id: 3, titulo: 'El correo no sincroniza', prioridad: 'alta', estado: 'cerrado', creadoEn: '2026-11-04T09:10:00.000Z' }]
          : [{ id: 1, titulo: 'La impresora no responde', prioridad: 'media', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' }];
        return HttpResponse.json({ datos, total: datos.length });
      }),
    );
    renderConProveedores(<TicketsPage />);
    await screen.findByText(/La impresora/);
    await userEvent.selectOptions(screen.getByLabelText('Estado'), 'cerrado');
    expect(await screen.findByText(/El correo no sincroniza/)).toBeInTheDocument();
    expect(screen.queryByText(/La impresora/)).not.toBeInTheDocument();
  });

  it('un filtro inválido en la URL cuenta como "todos"', async () => {
    renderConProveedores(<TicketsPage />, '/?estado=inventado');
    await screen.findByText(/La impresora/);
    expect(screen.getByLabelText('Estado')).toHaveValue('todos');
  });

  it('cerrar un ticket se ve al instante y vuelve atrás si el servidor falla', async () => {
    let lecturas = 0;
    servidor.use(
      http.get('/api/tickets', async () => {
        // la segunda lectura (la que sigue a la mutación) nunca contesta: así el rollback se ve solo, sin ayuda del refetch
        if (++lecturas > 1) await new Promise(() => {}); // nunca contesta
        return HttpResponse.json({ datos: [{ id: 1, titulo: 'La impresora no responde', prioridad: 'media', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' }], total: 1 });
      }),
      http.patch('/api/tickets/1', async () => {
        await new Promise((r) => setTimeout(r, 100));
        return HttpResponse.json({ codigo: 'interno', mensaje: 'Falla provocada' }, { status: 500 });
      }),
    );
    renderConProveedores(<TicketsPage />);
    await screen.findByText(/\(media, abierto\)/);
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar #1' }));
    expect(await screen.findByText(/\(media, cerrado\)/)).toBeInTheDocument(); // optimista
    // el PATCH falla y el rollback devuelve "abierto" sin esperar a la relectura (que aquí nunca llega)
    await waitFor(() => expect(screen.getByText(/\(media, abierto\)/)).toBeInTheDocument());
  });

  it('el borrador sobrevive a una recarga (mismo sessionStorage)', async () => {
    const primera = renderConProveedores(<TicketsPage />);
    await screen.findByText(/La impresora/);
    await userEvent.type(screen.getByLabelText('Título'), 'Borrador a medias');
    primera.unmount();
    renderConProveedores(<TicketsPage />);
    expect(await screen.findByLabelText('Título')).toHaveValue('Borrador a medias');
  });
});
