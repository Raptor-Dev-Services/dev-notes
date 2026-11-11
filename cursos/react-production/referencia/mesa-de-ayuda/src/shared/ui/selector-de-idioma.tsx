import { useTranslation } from 'react-i18next';
import { IDIOMAS, type Idioma } from '../i18n';

export function SelectorDeIdioma() {
  const { t, i18n } = useTranslation();
  return (
    <div>
      <label htmlFor="idioma">{t('comun.idioma')}</label>
      <select id="idioma" value={i18n.language} onChange={(e) => void i18n.changeLanguage(e.target.value as Idioma)}>
        {IDIOMAS.map((i) => (
          <option key={i} value={i} lang={i}>
            {new Intl.DisplayNames([i], { type: 'language' }).of(i)}
          </option>
        ))}
      </select>
    </div>
  );
}
