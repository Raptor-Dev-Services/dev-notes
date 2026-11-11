# Práctica - U4 - Seguridad en la protección de datos personales

**Seguridad de la información**
**Tiempo estimado:** 2.5 horas, individual, con contraste de posturas.
**Valor:** 7.5 puntos.

> **Estudio individual.** Esta actividad se resuelve por tu cuenta: guarda tu trabajo y evalúalo tú mismo con la rúbrica o el checklist del final. No hay foro ni entrega en la plataforma. Si llevas el curso en grupo, el instructor te indica dónde se entrega.

---

## Objetivo

Analizar si un sistema de gestión de seguridad de la información basado en ISO/IEC 27001 sirve para cumplir las obligaciones de seguridad del Reglamento General de Protección de Datos de la Unión Europea (RGPD), y en qué medida.

---

## Antes de empezar

Lean los **artículos 31 a 39** del Reglamento (UE) 2016/679 (páginas 51 a 56 de la versión en español publicada en el Diario Oficial de la Unión Europea): cooperación con la autoridad de control, seguridad del tratamiento, notificación de violaciones de seguridad a la autoridad y a los interesados, evaluación de impacto, consulta previa y delegado de protección de datos. El núcleo es el artículo 32:

> **Artículo 32. Seguridad del tratamiento.** 1. Teniendo en cuenta el estado de la técnica, los costes de aplicación, y la naturaleza, el alcance, el contexto y los fines del tratamiento, así como riesgos de probabilidad y gravedad variables para los derechos y libertades de las personas físicas, el responsable y el encargado del tratamiento aplicarán medidas técnicas y organizativas apropiadas para garantizar un nivel de seguridad adecuado al riesgo, que en su caso incluya, entre otros:
>
> a) la seudonimización y el cifrado de datos personales;
>
> b) la capacidad de garantizar la confidencialidad, integridad, disponibilidad y resiliencia permanentes de los sistemas y servicios de tratamiento;
>
> c) la capacidad de restaurar la disponibilidad y el acceso a los datos personales de forma rápida en caso de incidente físico o técnico;
>
> d) un proceso de verificación, evaluación y valoración regulares de la eficacia de las medidas técnicas y organizativas para garantizar la seguridad del tratamiento.

Repasen además la nota [`11-implementar-el-sgsi.md`](../../../12-seguridad/04-seguridad-informacion/11-implementar-el-sgsi.md) (Anexo A y declaración de aplicabilidad).

> Nota de vigencia: el material original llamaba a este instrumento "Directiva General de Protección de Datos". Es un **Reglamento** (de aplicación directa en todos los Estados miembros), vigente desde el 25 de mayo de 2018.

---

## Primer momento: tu análisis

Escribe en un documento propio:

1. **Una tabla de correspondencia** entre las obligaciones de los artículos 31 a 39 y los controles del Anexo A de ISO/IEC 27001:2022 que las atienden. Al menos ocho filas. Ejemplo de formato:

| Obligación del RGPD | Artículo | Controles del Anexo A | ¿Cubre por completo? |
|---|---|---|---|
| Cifrado y seudonimización | 32.1 a) | 8.24 Uso de criptografía; 8.11 Enmascaramiento de datos | Sí, si el alcance incluye los datos personales |
| Notificar a la autoridad en 72 horas | 33 | 5.24 a 5.28 Gestión de incidentes | Parcial: el plazo y el contenido los fija el RGPD |

2. **Lo que el SGSI no cubre.** Identifica al menos tres obligaciones del RGPD que ningún control del Anexo A resuelve por sí solo (por ejemplo, la evaluación de impacto sobre los derechos de las personas o la designación del delegado de protección de datos).
3. **Tu conclusión:** ¿es indispensable implantar un SGSI completo para cumplir estos artículos, o basta con implantar los controles pertinentes? Justifica con el principio de que la seguridad debe ser adecuada al riesgo.

> Actualización al 2026: ISO publicó normas que cierran parte de esa brecha, como ISO/IEC 27701, cuya edición 2025 especifica por sí misma un sistema de gestión de la privacidad de la información, sin depender ya de la 27001. Pueden mencionarla en su análisis.

---

## Segundo momento: contraste con otras posturas

Busca tú **dos** posturas distintas a tu conclusión (por ejemplo, un texto que sostenga que el SGSI basta y otro que sostenga que hace falta más, en el Reglamento, en ISO/IEC 27701 o en otra fuente) y resume cada una. Contrástalas con tu trabajo: ¿tu correspondencia es correcta?, ¿qué obligación olvidaste?, ¿mantienes tu conclusión y por qué? Aporta un dato nuevo en cada respuesta.

---

## Autoevaluación

Califícate con esta rúbrica:

| Criterio | Puntos |
|---|---|
| Tabla de correspondencia correcta y suficiente | 2.5 |
| Identifica lo que el SGSI no cubre | 1.5 |
| Conclusión fundada en el riesgo y en el texto del Reglamento | 1.5 |
| Contraste con dos posturas distintas, con argumentos y un dato nuevo | 1.5 |
| Redacción y referencias en APA 7 | 0.5 |

**Suma tus puntos** (máximo 7.5). Si sacas menos de 5.25 (70 %), vuelve a leer la nota [`11-implementar-el-sgsi.md`](../../../12-seguridad/04-seguridad-informacion/11-implementar-el-sgsi.md) y rehaz el apartado con menos puntos.

---

## Referencias

- International Organization for Standardization e International Electrotechnical Commission. (2022). *ISO/IEC 27001:2022 Information security, cybersecurity and privacy protection. Information security management systems. Requirements*. ISO. https://www.iso.org/standard/27001
- Parlamento Europeo y Consejo de la Unión Europea. (2016). Reglamento (UE) 2016/679 relativo a la protección de las personas físicas en lo que respecta al tratamiento de datos personales y a la libre circulación de estos datos (Reglamento general de protección de datos). *Diario Oficial de la Unión Europea*, L 119, 1-88. https://eur-lex.europa.eu/legal-content/ES/TXT/?uri=CELEX:32016R0679

---

*Rogelio Arriaga Gonzalez*
