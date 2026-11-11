import { paginaDeTicketsSchema, type PaginaDeTickets, type Ticket } from '../shared/api/contract';

export async function cargarTickets(): Promise<PaginaDeTickets> {
  const respuesta = await fetch('/api/tickets');
  const datos: unknown = await respuesta.json();
  return paginaDeTicketsSchema.parse(datos);
}

export function ListaDeTickets({ tickets }: { tickets: Ticket[] }) {
  return (
    <ul>
      {tickets.map((t) => (
        <li key={t.id}>
          {t.titulo} ({t.prioridad})
        </li>
      ))}
    </ul>
  );
}
