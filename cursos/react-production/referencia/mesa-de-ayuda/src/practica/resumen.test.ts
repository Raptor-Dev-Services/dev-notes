import { describe, expect, it } from 'vitest';
import type { PaginaDeTickets } from '../shared/api/contract';
import { masUrgente, resumir } from './resumen';

const pagina: PaginaDeTickets = {
  total: 3,
  datos: [
    { id: 1, titulo: 'Impresora', prioridad: 'media', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' },
    { id: 2, titulo: 'Acceso', prioridad: 'baja', estado: 'abierto', creadoEn: '2026-11-03T16:30:00.000Z' },
    { id: 3, titulo: 'Correo', prioridad: 'alta', estado: 'cerrado', creadoEn: '2026-11-04T09:10:00.000Z' },
  ],
};

describe('resumen de tickets', () => {
  it('cuenta por prioridad, incluidas las que no tienen tickets', () => {
    expect(resumir({ total: 0, datos: [] })).toEqual({ baja: 0, media: 0, alta: 0 });
    expect(resumir(pagina)).toEqual({ baja: 1, media: 1, alta: 1 });
  });
  it('encuentra el más urgente', () => {
    expect(masUrgente(pagina)?.id).toBe(3);
  });
});
