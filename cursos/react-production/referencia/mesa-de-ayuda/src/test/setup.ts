import '@testing-library/jest-dom/vitest';
import { afterAll, afterEach, beforeAll, beforeEach } from 'vitest';
import { cleanup } from '@testing-library/react';
import { servidor } from './servidor';
import i18n from '../shared/i18n';

beforeAll(() => servidor.listen({ onUnhandledFrame: 'error' }));
// jsdom declara navigator.language = 'en-US': sin esto la app arrancaría en inglés y las pruebas buscarían texto en español.
beforeEach(async () => {
  await i18n.changeLanguage('es');
});
afterEach(() => {
  cleanup();
  sessionStorage.clear(); // el borrador de una prueba no debe llegar a la siguiente
  servidor.resetHandlers();
});
afterAll(() => servidor.close());
