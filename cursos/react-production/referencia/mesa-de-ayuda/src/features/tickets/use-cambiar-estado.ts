import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { EstadoTicket, PaginaDeTickets } from '../../shared/api/contract';
import { ticketsApi, ticketsKeys } from './api';

/** Cambia el estado de un ticket con actualización optimista: se ve al instante y, si el servidor falla, vuelve a como estaba. */
export function useCambiarEstado() {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: ({ id, estado }: { id: number; estado: EstadoTicket }) => ticketsApi.cambiarEstado(id, estado),
    onMutate: async ({ id, estado }) => {
      await cliente.cancelQueries({ queryKey: ticketsKeys.todos });
      const antes = cliente.getQueriesData<PaginaDeTickets>({ queryKey: ticketsKeys.todos });
      cliente.setQueriesData<PaginaDeTickets>({ queryKey: ticketsKeys.todos }, (pagina) =>
        pagina && { ...pagina, datos: pagina.datos.map((t) => (t.id === id ? { ...t, estado } : t)) },
      );
      return { antes };
    },
    onError: (_error, _vars, contexto) => {
      contexto?.antes.forEach(([clave, datos]) => cliente.setQueryData(clave, datos));
    },
    onSettled: () => cliente.invalidateQueries({ queryKey: ticketsKeys.todos }),
  });
}
