const CLAVE = 'borrador-ticket';

/** Un borrador es comodidad, no dato: si el almacenamiento falla o está bloqueado, la app sigue igual. */
export const borrador = {
  leer(): string {
    try {
      return sessionStorage.getItem(CLAVE) ?? '';
    } catch {
      return '';
    }
  },
  guardar(titulo: string) {
    try {
      if (titulo) sessionStorage.setItem(CLAVE, titulo);
      else sessionStorage.removeItem(CLAVE);
    } catch {
      /* sin almacenamiento: no pasa nada */
    }
  },
};
