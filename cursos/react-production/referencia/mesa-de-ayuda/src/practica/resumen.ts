import type { PaginaDeTickets, Prioridad, Ticket } from '../shared/api/contract';

export function resumir(tickets: PaginaDeTickets): Record<Prioridad, number> {
  const resumen: Record<Prioridad, number> = { baja: 0, media: 0, alta: 0 };
  for (const t of tickets.datos) {
    resumen[t.prioridad] += 1;
  }
  return resumen;
}

export function masUrgente(tickets: PaginaDeTickets): Ticket | undefined {
  return tickets.datos.find((t) => t.prioridad === 'alta');
}
