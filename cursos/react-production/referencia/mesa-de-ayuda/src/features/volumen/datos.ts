import type { Ticket } from '../../shared/api/contract';

const TEMAS = ['impresora', 'correo', 'acceso', 'monitor', 'red', 'teléfono', 'licencia', 'contraseña'];
const PRIORIDADES = ['baja', 'media', 'alta'] as const;

/** Datos sintéticos y deterministas (Ejemplo ilustrativo): n tickets para probar listas grandes. */
export function ticketsSinteticos(n: number): Ticket[] {
  return Array.from({ length: n }, (_, i) => ({
    id: i + 1,
    titulo: `Incidencia ${i + 1}: ${TEMAS[i % TEMAS.length]} del piso ${(i % 7) + 1}`,
    prioridad: PRIORIDADES[i % 3] ?? 'baja',
    estado: i % 5 === 0 ? 'cerrado' : 'abierto',
    creadoEn: new Date(Date.UTC(2026, 10, 1 + (i % 28), i % 24)).toISOString(),
  }));
}
