import { useReducer, type ComponentProps, type ReactNode } from 'react';
import type { EstadoTicket, Prioridad, Ticket } from '../shared/api/contract';

// 1. Props con interface y children opcionales
interface InsigniaProps {
  prioridad: Prioridad;
  children?: ReactNode;
}

export function Insignia({ prioridad, children }: InsigniaProps) {
  return <span data-prioridad={prioridad}>{children ?? prioridad}</span>;
}

// 2. Extender las props de un elemento nativo (en React 19 incluye `ref`)
type BotonProps = ComponentProps<'button'> & { variante?: 'primario' | 'peligro' };

export function Boton({ variante = 'primario', ...resto }: BotonProps) {
  return <button data-variante={variante} {...resto} />;
}

// 3. Unión discriminada: cada estado trae solo lo que le corresponde
export type Vista<T> =
  | { estado: 'cargando' }
  | { estado: 'error'; mensaje: string }
  | { estado: 'listo'; datos: T };

function assertNever(x: never): never {
  throw new Error(`Caso no previsto: ${JSON.stringify(x)}`);
}

export function describir(v: Vista<Ticket[]>): string {
  switch (v.estado) {
    case 'cargando':
      return 'Cargando...';
    case 'error':
      return `Error: ${v.mensaje}`;
    case 'listo':
      return `${v.datos.length} tickets`;
    default:
      return assertNever(v);
  }
}

// 4. Componente genérico: T se infiere de `elementos`
interface ListaProps<T> {
  elementos: readonly T[];
  clave: (elemento: T) => string | number;
  render: (elemento: T) => ReactNode;
}

export function Lista<T>({ elementos, clave, render }: ListaProps<T>) {
  return (
    <ul>
      {elementos.map((e) => (
        <li key={clave(e)}>{render(e)}</li>
      ))}
    </ul>
  );
}

export const ejemploLista = (tickets: Ticket[]) => <Lista elementos={tickets} clave={(t) => t.id} render={(t) => t.titulo} />;

// 5. Acciones como unión discriminada en un reducer
type Filtro = EstadoTicket | 'todos';
type Accion = { tipo: 'filtrar'; filtro: Filtro } | { tipo: 'pagina'; numero: number } | { tipo: 'reiniciar' };
interface EstadoTabla {
  filtro: Filtro;
  pagina: number;
}

export function reductor(estado: EstadoTabla, accion: Accion): EstadoTabla {
  switch (accion.tipo) {
    case 'filtrar':
      return { filtro: accion.filtro, pagina: 1 };
    case 'pagina':
      return { ...estado, pagina: accion.numero };
    case 'reiniciar':
      return { filtro: 'todos', pagina: 1 };
  }
}

export function useTabla() {
  return useReducer(reductor, { filtro: 'todos', pagina: 1 });
}

// 6. El `as` miente; una guarda de tipo comprueba
const FILTROS = ['todos', 'abierto', 'cerrado'] as const;
export const esFiltro = (valor: string): valor is Filtro => (FILTROS as readonly string[]).includes(valor);

// 7. satisfies: validar la forma sin ensanchar el tipo
export const ETIQUETAS = {
  baja: 'Baja',
  media: 'Media',
  alta: 'Alta',
} satisfies Record<Prioridad, string>;

// 8. Utilidades de tipos
export type ResumenDeTicket = Pick<Ticket, 'id' | 'titulo'>;
export type CambioDeTicket = Partial<Omit<Ticket, 'id' | 'creadoEn'>>;

// 9. Tipos opacos (branded): dos números que no se pueden confundir
type Marca<T, M extends string> = T & { readonly __marca: M };
export type TicketId = Marca<number, 'TicketId'>;
export type UsuarioId = Marca<number, 'UsuarioId'>;
export const comoTicketId = (n: number) => n as TicketId;
export const buscarTicket = (id: TicketId) => `/tickets/${id}`;
