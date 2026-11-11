import { z } from 'zod';
import { pedir } from '../../shared/api/client';
import { usuarioSchema } from '../../shared/api/contract';

export const sesionKey = ['sesion'] as const;

export const authApi = {
  yo: (signal?: AbortSignal) => pedir('/me', { esquema: usuarioSchema, signal }),
  entrar: (credenciales: { email: string; password: string }) =>
    pedir('/login', { esquema: z.object({ usuario: usuarioSchema }), metodo: 'POST', cuerpo: credenciales }),
  salir: () => pedir('/logout', { esquema: z.object({ ok: z.boolean() }), metodo: 'POST', cuerpo: {} }),
};
