import { useState } from 'react';
import type { EstadoTicket } from '../services/contract';
import { EstadoDeDatos } from '../components/estado-de-datos';
import { TicketForm } from '../components/ticket-form';
import { useTickets } from '../hooks/use-tickets';

export function TicketsPage() {
  const [estado, setEstado] = useState<EstadoTicket | 'todos'>('todos');
  const consulta = useTickets(estado);
  return (
    <main>
      <h1>Tickets</h1>
      <label htmlFor="filtro">Estado</label>
      <select id="filtro" value={estado} onChange={(e) => setEstado(e.target.value as EstadoTicket | 'todos')}>
        <option value="todos">Todos</option>
        <option value="abierto">Abiertos</option>
        <option value="cerrado">Cerrados</option>
      </select>
      <EstadoDeDatos consulta={consulta} vacio={(p) => p.datos.length === 0} mensajeVacio="No hay tickets con ese filtro.">
        {(pagina) => (
          <ul aria-label="Lista de tickets">
            {pagina.datos.map((t) => (
              <li key={t.id}>{t.titulo} ({t.prioridad}, {t.estado})</li>
            ))}
          </ul>
        )}
      </EstadoDeDatos>
      <TicketForm />
    </main>
  );
}
