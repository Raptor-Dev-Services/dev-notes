import type { ReactNode } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';

interface Props<T> {
  consulta: UseQueryResult<T>;
  vacio: (datos: T) => boolean;
  mensajeVacio: string;
  children: (datos: T) => ReactNode;
}

/** Los cuatro estados de una pantalla con datos: cargando, error, vacío y con datos. */
export function EstadoDeDatos<T>({ consulta, vacio, mensajeVacio, children }: Props<T>) {
  if (consulta.isPending) return <p role="status">Cargando...</p>;
  if (consulta.isError) {
    return (
      <div role="alert">
        <p>No pudimos cargar los datos: {consulta.error.message}</p>
        <button type="button" onClick={() => void consulta.refetch()}>
          Reintentar
        </button>
      </div>
    );
  }
  if (vacio(consulta.data)) return <p>{mensajeVacio}</p>;
  return <>{children(consulta.data)}</>;
}
