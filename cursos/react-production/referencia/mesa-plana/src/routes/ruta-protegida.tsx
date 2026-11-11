import { Navigate, Outlet } from 'react-router';
import { useSesion } from '../hooks/use-sesion';

export function RutaProtegida() {
  const sesion = useSesion();
  if (sesion.isPending) return <p role="status">Comprobando sesión...</p>;
  if (sesion.isError) return <p role="alert">No pudimos comprobar tu sesión.</p>;
  if (!sesion.data) return <Navigate to="/entrar" replace />;
  return <Outlet />;
}
