import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '../shared/api/client';

/** Valores por omisión de la app: un 4xx no se reintenta (repetirlo da lo mismo); la red o un 5xx, hasta 2 veces. */
export const crearQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: (intentos, error) => !(error instanceof ApiError && error.estado < 500) && intentos < 2,
      },
    },
  });
