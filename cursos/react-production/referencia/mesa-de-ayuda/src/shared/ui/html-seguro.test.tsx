import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { render } from '@testing-library/react';
import { HtmlSeguro } from './html-seguro';

const ataque = '<img src=x onerror="alert(1)"><b>negritas</b><script>alert(2)</script>';

describe('XSS en React', () => {
  it('React escapa el texto que se interpola', () => {
    const html = renderToStaticMarkup(<p>{ataque}</p>);
    expect(html).toBe('<p>&lt;img src=x onerror=&quot;alert(1)&quot;&gt;&lt;b&gt;negritas&lt;/b&gt;&lt;script&gt;alert(2)&lt;/script&gt;</p>');
  });

  it('dangerouslySetInnerHTML NO escapa: el atributo onerror queda en el HTML', () => {
    const html = renderToStaticMarkup(<p dangerouslySetInnerHTML={{ __html: ataque }} />);
    expect(html).toContain('onerror="alert(1)"');
  });

  it('HtmlSeguro conserva el formato permitido y quita lo peligroso', () => {
    const { container } = render(<HtmlSeguro html={ataque} />);
    expect(container.innerHTML).toBe('<div><b>negritas</b></div>');
  });

  it('un href javascript: no sobrevive a la sanitización', () => {
    const { container } = render(<HtmlSeguro html={'<a href="javascript:alert(1)">clic</a><p>texto</p>'} />);
    expect(container.innerHTML).toBe('<div>clic<p>texto</p></div>');
  });
});

describe('URL peligrosas en atributos', () => {
  it('React 19 bloquea un href javascript: al renderizar', () => {
    const html = renderToStaticMarkup(<a href="javascript:alert(1)">clic</a>);
    expect(html).toBe(`<a href="javascript:throw new Error(&#x27;React has blocked a javascript: URL as a security precaution.&#x27;)">clic</a>`);
  });
});
