import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { en, es } from './recursos';

export const IDIOMAS = ['es', 'en'] as const;
export type Idioma = (typeof IDIOMAS)[number];

declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: { translation: typeof es };
  }
}

function idiomaInicial(): Idioma {
  try {
    const guardado = localStorage.getItem('idioma');
    if (guardado === 'es' || guardado === 'en') return guardado;
  } catch {
    // localStorage puede no estar disponible (modo privado, política del navegador): se usa el idioma del navegador
  }
  return typeof navigator !== 'undefined' && navigator.language.startsWith('en') ? 'en' : 'es';
}

void i18n.use(initReactI18next).init({
  resources: { es: { translation: es }, en: { translation: en } },
  lng: idiomaInicial(),
  fallbackLng: 'es',
  interpolation: { escapeValue: false }, // React ya escapa lo que se pinta
});

i18n.on('languageChanged', (idioma) => {
  document.documentElement.lang = idioma; // WCAG 3.1.1: el idioma de la página debe ser el real
  try {
    localStorage.setItem('idioma', idioma);
  } catch {
    // sin persistencia: el idioma se pierde al recargar, y no pasa nada más
  }
});
document.documentElement.lang = i18n.language;

export default i18n;
