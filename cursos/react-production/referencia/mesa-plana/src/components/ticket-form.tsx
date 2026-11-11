import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ApiError } from '../services/client';
import { nuevoTicketSchema, type NuevoTicket } from '../services/contract';
import { useCrearTicket } from '../hooks/use-tickets';

export function TicketForm() {
  const crear = useCrearTicket();
  const { register, handleSubmit, reset, setError, formState: { errors } } = useForm<NuevoTicket>({
    resolver: zodResolver(nuevoTicketSchema),
    defaultValues: { titulo: '', prioridad: 'media' },
  });

  const enviar = handleSubmit((valores) => {
    crear.mutate(valores, {
      onSuccess: () => reset(),
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
      <h2 id="titulo-form">Nuevo ticket</h2>
      <label htmlFor="titulo">Título</label>
      <input id="titulo" {...register('titulo')} aria-invalid={errors.titulo ? 'true' : 'false'} aria-describedby={errors.titulo ? 'titulo-error' : undefined} />
      {errors.titulo && <p id="titulo-error" role="alert">{errors.titulo.message}</p>}

      <label htmlFor="prioridad">Prioridad</label>
      <select id="prioridad" {...register('prioridad')}>
        <option value="baja">Baja</option>
        <option value="media">Media</option>
        <option value="alta">Alta</option>
      </select>
      {errors.prioridad && <p role="alert">{errors.prioridad.message}</p>}

      {falloGeneral && <p role="alert">No se pudo guardar: {crear.error.message}. Lo que escribiste sigue aquí; intenta de nuevo.</p>}
      <button type="submit" disabled={crear.isPending}>{crear.isPending ? 'Guardando...' : 'Crear ticket'}</button>
    </form>
  );
}
