import type { Prioridad, Ticket } from '../../shared/api/contract';

/** El ticket tal como lo necesita la pantalla: no es el JSON de la API, es el modelo de la aplicación. */
export interface TicketVista {
  id: number;
  titulo: string;
  prioridad: Prioridad;
  abierto: boolean;
  creadoEn: Date;
  urgente: boolean;
}

/** Frontera entre la API y la aplicación (capa anticorrupción): el único lugar que conoce la forma del JSON. */
export function aTicketVista(dto: Ticket): TicketVista {
  const abierto = dto.estado === 'abierto';
  return {
    id: dto.id,
    titulo: dto.titulo,
    prioridad: dto.prioridad,
    abierto,
    creadoEn: new Date(dto.creadoEn),
    urgente: abierto && dto.prioridad === 'alta',
  };
}
