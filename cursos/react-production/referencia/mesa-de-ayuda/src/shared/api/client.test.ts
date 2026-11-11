import { describe, expect, it } from 'vitest';
import { http, HttpResponse } from 'msw';
import { servidor } from '../../test/servidor';
import { ApiError, pedir } from './client';
import { paginaDeTicketsSchema } from './contract';

describe('pedir', () => {
  it('falla con nombre cuando el servidor rompe el contrato', async () => {
    // El servidor renombró "prioridad" a "urgencia": el tipo ya no coincide con la realidad.
    servidor.use(http.get('/api/tickets', () => HttpResponse.json({ datos: [{ id: 1, titulo: 'x', urgencia: 'alta', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' }], total: 1 })));
    await expect(pedir('/tickets', { esquema: paginaDeTicketsSchema })).rejects.toThrow(/prioridad/);
  });

  it('convierte un 422 en ApiError con errores por campo', async () => {
    servidor.use(http.get('/api/tickets', () => HttpResponse.json({ codigo: 'validacion', mensaje: 'Revisa los campos.', errores: { titulo: 'Muy corto' } }, { status: 422 })));
    const error = await pedir('/tickets', { esquema: paginaDeTicketsSchema }).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).errores).toEqual({ titulo: 'Muy corto' });
  });

  it('manda la cabecera anti-CSRF en cada petición', async () => {
    let cabecera: string | null = null;
    servidor.use(
      http.get('/api/tickets', ({ request }) => {
        cabecera = request.headers.get('x-requested-by');
        return HttpResponse.json({ datos: [], total: 0 });
      }),
    );
    await pedir('/tickets', { esquema: paginaDeTicketsSchema });
    expect(cabecera).toBe('mesa-de-ayuda');
  });
});
