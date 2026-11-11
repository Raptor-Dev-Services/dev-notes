interface Props {
  nombre: string;
  alSalir: () => void;
}

export function Cabecera({ nombre, alSalir }: Props) {
  return (
    <header>
      <span>Hola, {nombre}</span>
      <button type="button" onClick={alSalir}>
        Salir
      </button>
    </header>
  );
}
