// Una sola fuente de claves: el español manda y el inglés debe tener las mismas (el compilador lo exige).
export const es = {
  comun: {
    cargando: 'Cargando...',
    reintentar: 'Reintentar',
    errorCarga: 'No pudimos cargar los datos: {{mensaje}}',
    idioma: 'Idioma',
  },
  login: {
    titulo: 'Entrar',
    correo: 'Correo',
    contrasena: 'Contraseña',
    entrar: 'Entrar',
  },
  tickets: {
    titulo: 'Tickets',
    filtro: 'Estado',
    estado: { todos: 'Todos', abierto: 'Abiertos', cerrado: 'Cerrados' },
    vacio: 'No hay tickets con ese filtro.',
    lista: 'Lista de tickets',
    resumen_one: 'Hay {{count}} ticket.',
    resumen_other: 'Hay {{count}} tickets.',
    creadoEl: 'creado el {{fecha}}',
    cerrar: 'Cerrar #{{id}}',
    reabrir: 'Reabrir #{{id}}',
    errorEstado: 'No se pudo cambiar el estado: {{mensaje}}',
  },
  formulario: {
    titulo: 'Nuevo ticket',
    campoTitulo: 'Título',
    prioridad: 'Prioridad',
    baja: 'Baja',
    media: 'Media',
    alta: 'Alta',
    crear: 'Crear ticket',
    guardando: 'Guardando...',
    falloGeneral: 'No se pudo guardar: {{mensaje}}. Lo que escribiste sigue aquí; intenta de nuevo.',
  },
  validacion: {
    titulo_corto: 'El título necesita al menos 5 caracteres.',
  },
} as const;

type Estructura<T> = { -readonly [K in keyof T]: T[K] extends string ? string : Estructura<T[K]> };

export const en: Estructura<typeof es> = {
  comun: {
    cargando: 'Loading...',
    reintentar: 'Retry',
    errorCarga: 'We could not load the data: {{mensaje}}',
    idioma: 'Language',
  },
  login: { titulo: 'Sign in', correo: 'Email', contrasena: 'Password', entrar: 'Sign in' },
  tickets: {
    titulo: 'Tickets',
    filtro: 'Status',
    estado: { todos: 'All', abierto: 'Open', cerrado: 'Closed' },
    vacio: 'No tickets match that filter.',
    lista: 'Ticket list',
    resumen_one: 'There is {{count}} ticket.',
    resumen_other: 'There are {{count}} tickets.',
    creadoEl: 'created on {{fecha}}',
    cerrar: 'Close #{{id}}',
    reabrir: 'Reopen #{{id}}',
    errorEstado: 'Could not change the status: {{mensaje}}',
  },
  formulario: {
    titulo: 'New ticket',
    campoTitulo: 'Title',
    prioridad: 'Priority',
    baja: 'Low',
    media: 'Medium',
    alta: 'High',
    crear: 'Create ticket',
    guardando: 'Saving...',
    falloGeneral: 'Could not save: {{mensaje}}. What you typed is still here; try again.',
  },
  validacion: { titulo_corto: 'The title needs at least 5 characters.' },
};
