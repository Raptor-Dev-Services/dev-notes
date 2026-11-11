import { pedir } from '../../shared/api/client';
import { paginaDeTicketsSchema, ticketSchema, type EstadoTicket, type NuevoTicket } from '../../shared/api/contract';

export const ticketsKeys = {
  todos: ['tickets'] as const,
  lista: (estado: EstadoTicket | 'todos') => ['tickets', 'lista', estado] as const,
};

export const ticketsApi = {
  listar: (estado: EstadoTicket | 'todos', signal?: AbortSignal) =>
    pedir(estado === 'todos' ? '/tickets' : `/tickets?estado=${estado}`, { esquema: paginaDeTicketsSchema, signal }),
  cambiarEstado: (id: number, estado: EstadoTicket) => pedir(`/tickets/${id}`, { esquema: ticketSchema, metodo: 'PATCH', cuerpo: { estado } }),
  crear: (nuevo: NuevoTicket) => pedir('/tickets', { esquema: ticketSchema, metodo: 'POST', cuerpo: nuevo }),
};

