import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

export const ticketsDePrueba = [
  { id: 1, titulo: 'La impresora no responde', prioridad: 'media', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' },
];

export const servidor = setupServer(
  http.get('/api/tickets', () => HttpResponse.json({ datos: ticketsDePrueba, total: 1 })),
);
