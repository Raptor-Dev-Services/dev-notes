import { useState, type FormEvent } from 'react';
import { Navigate } from 'react-router';
import { useEntrar, useSesion } from '../hooks/use-sesion';

export function LoginPage() {
  const sesion = useSesion();
  const entrar = useEntrar();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  if (sesion.data) return <Navigate to="/" replace />;

  const enviar = (e: FormEvent) => {
    e.preventDefault();
    entrar.mutate({ email, password });
  };
  return (
    <main>
      <h1>Entrar</h1>
      <form onSubmit={enviar}>
        <label htmlFor="email">Correo</label>
        <input id="email" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} />
        <label htmlFor="password">Contraseña</label>
        <input id="password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        {entrar.isError && <p role="alert">{entrar.error.message}</p>}
        <button type="submit" disabled={entrar.isPending}>Entrar</button>
      </form>
    </main>
  );
}
