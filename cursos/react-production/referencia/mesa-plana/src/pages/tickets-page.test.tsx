import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { renderConProveedores } from '../test/render';
import { servidor } from '../test/servidor';
import { TicketsPage } from './tickets-page';

describe('TicketsPage', () => {
  it('muestra los tickets que devuelve la API', async () => {
    renderConProveedores(<TicketsPage />);
    expect(screen.getByRole('status')).toHaveTextContent('Cargando');
    expect(await screen.findByText(/La impresora no responde/)).toBeInTheDocument();
  });

  it('muestra el estado vacío', async () => {
    servidor.use(http.get('/api/tickets', () => HttpResponse.json({ datos: [], total: 0 })));
    renderConProveedores(<TicketsPage />);
    expect(await screen.findByText('No hay tickets con ese filtro.')).toBeInTheDocument();
  });

  it('muestra el error y permite reintentar', async () => {
    servidor.use(http.get('/api/tickets', () => HttpResponse.json({ codigo: 'interno', mensaje: 'Falla provocada' }, { status: 500 }), { once: true }));
    renderConProveedores(<TicketsPage />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Falla provocada');
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(await screen.findByText(/La impresora no responde/)).toBeInTheDocument();
  });

  it('valida en el cliente antes de llamar al servidor', async () => {
    renderConProveedores(<TicketsPage />);
    await screen.findByText(/La impresora/);
    await userEvent.type(screen.getByLabelText('Título'), 'corto');
    await userEvent.clear(screen.getByLabelText('Título'));
    await userEvent.type(screen.getByLabelText('Título'), 'abc');
    await userEvent.click(screen.getByRole('button', { name: 'Crear ticket' }));
    expect(await screen.findByText('El título necesita al menos 5 caracteres.')).toBeInTheDocument();
  });

  it('pega el error 422 del servidor al campo y conserva lo escrito', async () => {
    servidor.use(
      http.post('/api/tickets', () => HttpResponse.json({ codigo: 'validacion', mensaje: 'Revisa los campos.', errores: { titulo: 'Ya existe un ticket con ese título.' } }, { status: 422 })),
    );
    renderConProveedores(<TicketsPage />);
    await screen.findByText(/La impresora/);
    await userEvent.type(screen.getByLabelText('Título'), 'Un título válido');
    await userEvent.click(screen.getByRole('button', { name: 'Crear ticket' }));
    expect(await screen.findByText('Ya existe un ticket con ese título.')).toBeInTheDocument();
    expect(screen.getByLabelText('Título')).toHaveValue('Un título válido');
  });

  it('conserva lo escrito cuando la red falla', async () => {
    servidor.use(http.post('/api/tickets', () => HttpResponse.error()));
    renderConProveedores(<TicketsPage />);
    await screen.findByText(/La impresora/);
    await userEvent.type(screen.getByLabelText('Título'), 'Sin red, pero no se pierde');
    await userEvent.click(screen.getByRole('button', { name: 'Crear ticket' }));
    expect(await screen.findByText(/Lo que escribiste sigue aquí/)).toBeInTheDocument();
    expect(screen.getByLabelText('Título')).toHaveValue('Sin red, pero no se pierde');
  });
});
