import { describe, expect, it } from 'vitest';
import type { Ticket } from '../../shared/api/contract';
import { aTicketVista } from './domain';

const base: Ticket = { id: 7, titulo: 'Impresora', prioridad: 'alta', estado: 'abierto', creadoEn: '2026-11-02T15:00:00.000Z' };

describe('aTicketVista', () => {
  it('un ticket abierto de prioridad alta es urgente', () => {
    expect(aTicketVista(base).urgente).toBe(true);
  });
  it('un ticket cerrado ya no es urgente, aunque su prioridad sea alta', () => {
    expect(aTicketVista({ ...base, estado: 'cerrado' }).urgente).toBe(false);
  });
  it('convierte la fecha ISO en Date', () => {
    expect(aTicketVista(base).creadoEn).toEqual(new Date('2026-11-02T15:00:00.000Z'));
  });
});
