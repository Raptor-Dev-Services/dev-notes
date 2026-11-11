import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { estadoTicketSchema, type EstadoTicket } from '../../shared/api/contract';
import { EstadoDeDatos } from '../../shared/ui/estado-de-datos';
import { SelectorDeIdioma } from '../../shared/ui/selector-de-idioma';
import { TicketForm } from './ticket-form';
import { useCambiarEstado } from './use-cambiar-estado';
import { useTickets } from './use-tickets';

/** El filtro vive en la URL (?estado=abierto): se puede compartir, recargar y volver atrás. Un valor inválido cuenta como "todos". */
function filtroDe(params: URLSearchParams): EstadoTicket | 'todos' {
  const r = estadoTicketSchema.safeParse(params.get('estado'));
  return r.success ? r.data : 'todos';
}

export function TicketsPage() {
  const { t, i18n } = useTranslation();
  const [params, setParams] = useSearchParams();
  const estado = filtroDe(params);
  const consulta = useTickets(estado);
  const cambiar = useCambiarEstado();
  return (
    <main>
      <h1>{t('tickets.titulo')}</h1>
      <SelectorDeIdioma />
      <label htmlFor="filtro">{t('tickets.filtro')}</label>
      <select id="filtro" value={estado} onChange={(e) => setParams(e.target.value === 'todos' ? {} : { estado: e.target.value })}>
        <option value="todos">{t('tickets.estado.todos')}</option>
        <option value="abierto">{t('tickets.estado.abierto')}</option>
        <option value="cerrado">{t('tickets.estado.cerrado')}</option>
      </select>
      <EstadoDeDatos consulta={consulta} vacio={(p) => p.datos.length === 0} mensajeVacio={t('tickets.vacio')}>
        {(pagina) => (
          <>
            <p>{t('tickets.resumen', { count: pagina.total })}</p>
            <ul aria-label={t('tickets.lista')}>
              {pagina.datos.map((ticket) => (
                <li key={ticket.id}>
                  {ticket.titulo} ({ticket.prioridad}, {ticket.estado}) - {t('tickets.creadoEl', { fecha: new Intl.DateTimeFormat(i18n.language, { dateStyle: 'medium', timeZone: 'UTC' }).format(new Date(ticket.creadoEn)) })}{' '}
                  <button type="button" onClick={() => cambiar.mutate({ id: ticket.id, estado: ticket.estado === 'abierto' ? 'cerrado' : 'abierto' })}>
                    {t(ticket.estado === 'abierto' ? 'tickets.cerrar' : 'tickets.reabrir', { id: ticket.id })}
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}
      </EstadoDeDatos>
      {cambiar.isError && <p role="alert">{t('tickets.errorEstado', { mensaje: cambiar.error.message })}</p>}
      <TicketForm />
    </main>
  );
}
