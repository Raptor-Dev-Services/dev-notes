import type { z } from 'zod';
import type { CuerpoDeError } from './contract';

export class ApiError extends Error {
  readonly estado: number;
  readonly cuerpo: CuerpoDeError;
  constructor(estado: number, cuerpo: CuerpoDeError) {
    super(cuerpo.mensaje);
    this.name = 'ApiError';
    this.estado = estado;
    this.cuerpo = cuerpo;
  }
  /** Errores por campo (HTTP 422), o un objeto vacío. */
  get errores(): Record<string, string> {
    return this.cuerpo.errores ?? {};
  }
}

interface Opciones<S extends z.ZodType> {
  esquema: S;
  metodo?: 'GET' | 'POST';
  cuerpo?: unknown;
  signal?: AbortSignal;
}

/**
 * Una petición a la API: manda la cookie de sesión, convierte los errores HTTP en ApiError
 * y valida la respuesta contra su esquema. Si el servidor cambia el contrato, falla aquí, con nombre.
 */
export async function pedir<S extends z.ZodType>(ruta: string, { esquema, metodo = 'GET', cuerpo, signal }: Opciones<S>): Promise<z.infer<S>> {
  const respuesta = await fetch(`/api${ruta}`, {
    method: metodo,
    credentials: 'same-origin',
    signal,
    headers: {
      accept: 'application/json',
      'x-requested-by': 'mesa-de-ayuda',
      ...(cuerpo === undefined ? {} : { 'content-type': 'application/json' }),
    },
    body: cuerpo === undefined ? undefined : JSON.stringify(cuerpo),
  });
  const datos: unknown = await respuesta.json().catch(() => null);
  if (!respuesta.ok) {
    const c = datos as Partial<CuerpoDeError> | null;
    throw new ApiError(respuesta.status, { codigo: c?.codigo ?? 'desconocido', mensaje: c?.mensaje ?? `Error ${respuesta.status}`, errores: c?.errores });
  }
  return esquema.parse(datos);
}
