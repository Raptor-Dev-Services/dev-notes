import type { components } from '../shared/api/schema';
import type { Ticket, NuevoTicket } from '../shared/api/contract';

// El esquema de zod y el contrato OpenAPI deben describir lo mismo. Si uno cambia, esta línea no compila.
type Igual<A, B> = (<T>() => T extends A ? 1 : 2) extends <T>() => T extends B ? 1 : 2 ? true : false;
const mismoTicket: Igual<Ticket, components['schemas']['Ticket']> = true;
const mismoNuevo: Igual<NuevoTicket, components['schemas']['NuevoTicket']> = true;
export { mismoTicket, mismoNuevo };
