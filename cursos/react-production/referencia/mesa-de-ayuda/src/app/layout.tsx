import { useEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router';
import { useSalir, useSesion } from '../features/auth';
import { Cabecera } from '../features/perfil';

/**
 * Composición entre features (sesión 2): la cabecera de perfil recibe lo que necesita por props, ninguna feature importa a la otra.
 * Accesibilidad (sesión 5): en una SPA el navegador no recarga la página, así que no anuncia el cambio de pantalla ni mueve el foco.
 * Al cambiar de ruta: el título del documento sigue al h1 (WCAG 2.4.2) y el foco pasa al h1 (WCAG 2.4.3).
 */
export function Layout() {
  const { pathname } = useLocation();
  const sesion = useSesion();
  const salir = useSalir();
  const primera = useRef(true);
  useEffect(() => {
    const h1 = document.querySelector('h1');
    if (!h1) return;
    document.title = `${h1.textContent} - Mesa de ayuda`;
    if (primera.current) {
      primera.current = false;
      return;
    }
    h1.tabIndex = -1;
    h1.focus();
  }, [pathname]);
  return (
    <>
      {sesion.data && <Cabecera nombre={sesion.data.nombre} alSalir={() => salir.mutate()} />}
      <Outlet />
    </>
  );
}
