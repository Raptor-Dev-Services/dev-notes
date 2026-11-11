import type { ReactNode } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

interface Props<T> {
  consulta: UseQueryResult<T>;
  vacio: (datos: T) => boolean;
  mensajeVacio: string;
  children: (datos: T) => ReactNode;
}

/** Los cuatro estados de una pantalla con datos: cargando, error, vacío y con datos. */
export function EstadoDeDatos<T>({ consulta, vacio, mensajeVacio, children }: Props<T>) {
  const { t } = useTranslation();
  if (consulta.isPending) return <p role="status">{t('comun.cargando')}</p>;
  if (consulta.isError) {
    return (
      <div role="alert">
        <p>{t('comun.errorCarga', { mensaje: consulta.error.message })}</p>
        <button type="button" onClick={() => void consulta.refetch()}>
          {t('comun.reintentar')}
        </button>
      </div>
    );
  }
  if (vacio(consulta.data)) return <p>{mensajeVacio}</p>;
  return <>{children(consulta.data)}</>;
}
