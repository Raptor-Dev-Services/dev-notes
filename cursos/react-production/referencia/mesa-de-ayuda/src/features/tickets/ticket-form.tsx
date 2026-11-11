import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { ApiError } from '../../shared/api/client';
import { nuevoTicketSchema, type NuevoTicket } from '../../shared/api/contract';
import { borrador } from './borrador';
import { useCrearTicket } from './use-tickets';

export function TicketForm() {
  const { t } = useTranslation();
  const crear = useCrearTicket();
  const { register, handleSubmit, reset, setError, watch, formState: { errors } } = useForm<NuevoTicket>({
    resolver: zodResolver(nuevoTicketSchema),
    defaultValues: { titulo: borrador.leer(), prioridad: 'media' },
  });

  useEffect(() => {
    const suscripcion = watch((valores) => borrador.guardar(valores.titulo ?? ''));
    return () => suscripcion.unsubscribe();
  }, [watch]);

  const enviar = handleSubmit((valores) => {
    crear.mutate(valores, {
      onSuccess: () => {
        reset();
        borrador.guardar('');
      },
      onError: (error) => {
        // Errores del servidor (HTTP 422): se pegan al campo que corresponde. Lo escrito se queda.
        if (error instanceof ApiError && error.estado === 422) {
          for (const [campo, mensaje] of Object.entries(error.errores)) {
            if (campo === 'titulo' || campo === 'prioridad') setError(campo, { type: 'server', message: mensaje });
          }
        }
      },
    });
  });

  const falloGeneral = crear.error && !(crear.error instanceof ApiError && crear.error.estado === 422);

  return (
    <form onSubmit={enviar} noValidate aria-labelledby="titulo-form">
      <h2 id="titulo-form">{t('formulario.titulo')}</h2>
      <label htmlFor="titulo">{t('formulario.campoTitulo')}</label>
      <input id="titulo" {...register('titulo')} aria-invalid={errors.titulo ? 'true' : 'false'} aria-describedby={errors.titulo ? 'titulo-error' : undefined} />
      {errors.titulo && <p id="titulo-error" role="alert">{errors.titulo.type === 'server' ? errors.titulo.message : t(`validacion.${errors.titulo.message as 'titulo_corto'}`)}</p>}

      <label htmlFor="prioridad">{t('formulario.prioridad')}</label>
      <select id="prioridad" {...register('prioridad')}>
        <option value="baja">{t('formulario.baja')}</option>
        <option value="media">{t('formulario.media')}</option>
        <option value="alta">{t('formulario.alta')}</option>
      </select>
      {errors.prioridad && <p role="alert">{errors.prioridad.message}</p>}

      {falloGeneral && <p role="alert">{t('formulario.falloGeneral', { mensaje: crear.error.message })}</p>}
      <button type="submit" disabled={crear.isPending}>{crear.isPending ? t('formulario.guardando') : t('formulario.crear')}</button>
    </form>
  );
}
