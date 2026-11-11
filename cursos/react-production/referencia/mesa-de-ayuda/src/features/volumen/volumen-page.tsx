import { memo, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useVirtualizer } from '@tanstack/react-virtual';
import type { Ticket } from '../../shared/api/contract';
import { ticketsSinteticos } from './datos';

type Modo = 'lenta' | 'memo' | 'virtual';

function FilaLenta({ ticket }: { ticket: Ticket }) {
  // Crear un Intl.DateTimeFormat en cada render es caro a propósito: es el trabajo que se repite.
  const fecha = new Intl.DateTimeFormat('es-MX', { dateStyle: 'long', timeStyle: 'short', timeZone: 'UTC' }).format(new Date(ticket.creadoEn));
  return (
    <li>
      {ticket.titulo} - {ticket.prioridad} - {fecha}
    </li>
  );
}
const FilaMemo = memo(FilaLenta);

export function VolumenPage() {
  const [params] = useSearchParams();
  const modo: Modo = params.get('modo') === 'memo' ? 'memo' : params.get('modo') === 'virtual' ? 'virtual' : 'lenta';
  const [texto, setTexto] = useState('');
  const todos = useMemo(() => ticketsSinteticos(5000), []);
  const visibles = useMemo(() => todos.filter((t) => t.titulo.toLowerCase().includes(texto.toLowerCase())), [todos, texto]);
  const contenedor = useRef<HTMLDivElement>(null);
  const virtualizador = useVirtualizer({ count: visibles.length, getScrollElement: () => contenedor.current, estimateSize: () => 24, overscan: 8 });

  return (
    <main>
      <h1>Volumen ({modo})</h1>
      <label htmlFor="buscar">Buscar</label>
      <input id="buscar" value={texto} onChange={(e) => setTexto(e.target.value)} />
      <p role="status">{visibles.length} resultados</p>
      {modo === 'virtual' ? (
        <div ref={contenedor} style={{ height: 480, overflow: 'auto' }} tabIndex={0} aria-label="Resultados">
          <ul style={{ height: virtualizador.getTotalSize(), position: 'relative', margin: 0, padding: 0, listStyle: 'none' }}>
            {virtualizador.getVirtualItems().map((v) => {
              const t = visibles[v.index];
              if (!t) return null;
              return (
                <li key={t.id} style={{ position: 'absolute', top: 0, left: 0, height: v.size, transform: `translateY(${v.start}px)` }}>
                  {t.titulo} - {t.prioridad}
                </li>
              );
            })}
          </ul>
        </div>
      ) : (
        <ul>
          {visibles.map((t) => (modo === 'memo' ? <FilaMemo key={t.id} ticket={t} /> : <FilaLenta key={t.id} ticket={t} />))}
        </ul>
      )}
    </main>
  );
}
