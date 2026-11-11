import { z } from 'zod';

// El contrato de la API: una sola fuente. Los tipos salen de los esquemas (z.infer),
// así que lo que valida en ejecución y lo que ve el compilador no pueden divergir.
export const prioridadSchema = z.enum(['baja', 'media', 'alta']);
export const estadoTicketSchema = z.enum(['abierto', 'cerrado']);

export const ticketSchema = z.object({
  id: z.number().int(),
  titulo: z.string(),
  prioridad: prioridadSchema,
  estado: estadoTicketSchema,
  creadoEn: z.iso.datetime(),
});

export const paginaDeTicketsSchema = z.object({
  datos: z.array(ticketSchema),
  total: z.number().int(),
});

export const usuarioSchema = z.object({ id: z.number().int(), nombre: z.string(), email: z.string() });

export const nuevoTicketSchema = z.object({
  titulo: z.string().trim().min(5, 'El título necesita al menos 5 caracteres.'),
  prioridad: prioridadSchema,
});

export type Prioridad = z.infer<typeof prioridadSchema>;
export type EstadoTicket = z.infer<typeof estadoTicketSchema>;
export type Ticket = z.infer<typeof ticketSchema>;
export type PaginaDeTickets = z.infer<typeof paginaDeTicketsSchema>;
export type Usuario = z.infer<typeof usuarioSchema>;
export type NuevoTicket = z.infer<typeof nuevoTicketSchema>;

/** Forma del cuerpo de error que devuelve la API (RFC 9457 sería lo ideal; esta API usa una forma propia). */
export interface CuerpoDeError {
  codigo: string;
  mensaje: string;
  errores?: Record<string, string>;
}
