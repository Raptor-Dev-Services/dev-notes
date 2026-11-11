import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '../services/client';
import { authApi, sesionKey } from '../services/auth-api';

/** La sesión vive en el servidor (cookie HttpOnly); aquí solo preguntamos "¿quién soy?". 401 significa "nadie", no "error". */
export function useSesion() {
  return useQuery({
    queryKey: sesionKey,
    queryFn: async ({ signal }) => {
      try {
        return await authApi.yo(signal);
      } catch (e) {
        if (e instanceof ApiError && e.estado === 401) return null;
        throw e;
      }
    },
    staleTime: 60_000,
  });
}

export function useEntrar() {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: authApi.entrar,
    onSuccess: ({ usuario }) => cliente.setQueryData(sesionKey, usuario),
  });
}

export function useSalir() {
  const cliente = useQueryClient();
  return useMutation({
    mutationFn: authApi.salir,
    onSuccess: () => {
      cliente.setQueryData(sesionKey, null);
      cliente.removeQueries({ predicate: (q) => q.queryKey[0] !== 'sesion' }); // nada de la sesión anterior queda en memoria
    },
  });
}
