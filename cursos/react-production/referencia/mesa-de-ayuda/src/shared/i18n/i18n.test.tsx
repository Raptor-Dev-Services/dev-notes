import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import i18n from './index';
import { SelectorDeIdioma } from '../ui/selector-de-idioma';

describe('i18n', () => {
  it('usa la forma plural correcta en cada idioma', async () => {
    expect(i18n.t('tickets.resumen', { count: 1 })).toBe('Hay 1 ticket.');
    expect(i18n.t('tickets.resumen', { count: 3 })).toBe('Hay 3 tickets.');
    await i18n.changeLanguage('en');
    expect(i18n.t('tickets.resumen', { count: 1 })).toBe('There is 1 ticket.');
    expect(i18n.t('tickets.resumen', { count: 3 })).toBe('There are 3 tickets.');
  });

  it('cambiar de idioma actualiza el atributo lang del documento', async () => {
    render(<SelectorDeIdioma />);
    await userEvent.selectOptions(screen.getByLabelText('Idioma'), 'en');
    expect(document.documentElement.lang).toBe('en');
  });
});
