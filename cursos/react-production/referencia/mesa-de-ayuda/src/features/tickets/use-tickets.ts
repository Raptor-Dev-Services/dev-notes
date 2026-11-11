import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { EstadoTicket, NuevoTicket } from '../../shared/api/contract';
import { ticketsApi, ticketsKeys } from './api';

export function useTickets(estado: EstadoTicket | 'todos') {
  return useQuery({
    queryKey: ticketsKeys.lista(estado),
    queryFn: ({ signal }) => ticketsApi.listar(estado, signal),
  });
}

export function useCrearTicket() {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: (nuevo: NuevoTicket) => ticketsApi.crear(nuevo),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ticketsKeys.todos }),
  });
}
