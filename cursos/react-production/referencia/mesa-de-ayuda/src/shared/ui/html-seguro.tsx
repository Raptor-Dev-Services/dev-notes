import DOMPurify from 'dompurify';

interface Props {
  /** HTML que viene de afuera (por ejemplo, la descripción con formato de un ticket). */
  html: string;
}

/**
 * El único lugar de la app que usa dangerouslySetInnerHTML, y siempre después de sanitizar.
 * Solo se permiten unas pocas etiquetas de texto: sin atributos, sin enlaces, sin imágenes.
 */
export function HtmlSeguro({ html }: Props) {
  const limpio = DOMPurify.sanitize(html, { ALLOWED_TAGS: ['b', 'i', 'em', 'strong', 'p', 'ul', 'ol', 'li', 'br'], ALLOWED_ATTR: [] });
  return <div dangerouslySetInnerHTML={{ __html: limpio }} />;
}
