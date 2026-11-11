# Tarea - U3 - De la curiosidad a la vulnerabilidad

**Introducción a la ciberseguridad**
**Tiempo estimado:** 5 horas

---

## Parte 1 - Lecturas (1.5 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| [`dev-notes/12-seguridad/01-introduccion/07-ciberentorno-aproximaciones-y-tendencias.md`](../../../12-seguridad/01-introduccion/07-ciberentorno-aproximaciones-y-tendencias.md) | "Tecnologías disruptivas: innovación y preocupación" y "Tendencias del ciberentorno" | 30 min |
| [`dev-notes/12-seguridad/01-introduccion/08-riesgos-y-vulnerabilidades.md`](../../../12-seguridad/01-introduccion/08-riesgos-y-vulnerabilidades.md) | "Cuatro palabras que no son sinónimos" y "Medir el riesgo" | 30 min |
| [`dev-notes/12-seguridad/01-introduccion/09-ciberamenazas-principales-dimensiones.md`](../../../12-seguridad/01-introduccion/09-ciberamenazas-principales-dimensiones.md) | "El panorama en cifras" (malware y denegación de servicio) | 30 min |

### Recomendadas

| Recurso | Para qué |
|-------|-----------|
| Guía de análisis de riesgos en seis pasos del INCIBE | Ordenar las vulnerabilidades por riesgo, no solo enlistarlas |
| OWASP Internet of Things Project (owasp.org) | Lista de referencia de fallas típicas de dispositivos conectados |

---

## Parte 2 - El reto (3.5 horas)

**Tipo de actividad:** individual.

**Objetivo:** analizar la vulnerabilidad de las tecnologías del internet de las cosas (IoT).

Los dispositivos conectados entran a las casas, los hospitales, las fábricas y los autos porque prometen comodidad y datos. Esta tarea pide mirar el otro lado: qué puede salir mal con cada uno y por qué.

### Pasos

1. **Elige tres tecnologías con conexión IoT** de ámbitos distintos (por ejemplo: un dispositivo médico, un aparato del hogar inteligente y un vehículo o equipo industrial). Evita tres variantes del mismo producto.
2. **Investiga cómo funcionan:** qué datos recolectan, a dónde los mandan, cómo se actualizan y quién los administra.
3. **Analiza sus posibles vulnerabilidades.** Para cada tecnología, busca fallas reportadas o débiles por diseño: contraseñas de fábrica, firmware sin actualizaciones, comunicación sin cifrar, red sin segmentar, acceso remoto mal protegido, dependencia de la nube del fabricante, recolección excesiva de datos.
4. **Haz una lista** de las vulnerabilidades encontradas en cada tecnología.
5. **Explica brevemente cada vulnerabilidad:** qué es, qué amenaza la aprovecharía y qué consecuencia tendría (técnica, para la privacidad o para la integridad física de las personas).
6. **Escribe un resumen informativo** de las características de cada tecnología seleccionada.
7. **Cierra con conclusiones** individuales (qué aprendiste) y generales (qué patrón comparten las tres tecnologías).

**Opcional para subir de nivel:** ordena las vulnerabilidades con la fórmula riesgo = probabilidad × impacto (escalas de 1 a 5) y marca cuáles tratarías primero.

### Formato de la entrega

Un documento con:

- Portada con tus datos generales y el objetivo de la actividad.
- Introducción.
- Desarrollo: las tres tecnologías, cada una con su resumen, su lista de vulnerabilidades y la explicación de cada una.
- Conclusiones individuales y generales (media cuartilla).
- Fuentes de información en formato APA 7.

Extensión máxima: 5 cuartillas sin contar imágenes, interlineado 1.15, letra Arial 12.

```markdown
## Introducción
(qué es el IoT y por qué analizar su seguridad)

## Tecnología 1: ...
### Características
### Vulnerabilidades
1. ... : qué es, qué amenaza la aprovecha, qué consecuencia tendría

## Tecnología 2: ...
## Tecnología 3: ...

## Conclusiones
### Individuales
### Generales

## Referencias
```

---

## Rúbrica

Valor total: 7.5 puntos.

| Criterio | Excelente | Bueno | Regular | Deficiente |
|---|---|---|---|---|
| Calidad del contenido | 2 pts. Todo el texto se relaciona claramente con el tema y lo enriquece con ideas secundarias y ejemplos | 1 pt. Se relaciona con el tema, pero con pocas ideas secundarias y ejemplos | 0.5 pts. La relación con el tema es vaga y casi no hay ejemplos | 0 pts. No se relaciona con el tema |
| Investigación | 2 pts. Consiguió material adicional, pertinente y actual, de recursos y autores variados | 1.33 pts. Material suficiente, pertinente y actual, de recursos y autores variados | 0.67 pts. Material suficiente, pertinente y actual, pero de pocos recursos o autores | 0 pts. Material pertinente pero desactualizado, de pocos recursos o autores |
| Presentación, organización y ortografía | 1.5 pts. Respeta el formato y la extensión; sin errores de sintaxis ni de ortografía | 1 pt. Respeta formato y extensión; de dos a cuatro errores | 0.5 pts. Respeta el formato pero la extensión se aleja de lo pedido; de cinco a siete errores | 0 pts. Formato o extensión muy alejados de lo pedido; ocho errores o más |
| Fuentes de información | 2 pts. Las fuentes enriquecen el trabajo, son actuales y están citadas y referenciadas en APA 7 | 1 pt. Enriquecen y están bien citadas, pero algunas no son recientes | 0.5 pts. No todas aportan y la información no es actual, aunque el formato sea correcto | 0 pts. Fuentes poco útiles o desactualizadas, sin el formato de cita y referencia |

---

## Criterio de aceptación

- [ ] Son tres tecnologías IoT de ámbitos distintos.
- [ ] Cada una tiene su resumen de características.
- [ ] Cada vulnerabilidad está en la lista y explicada: qué es, qué amenaza la aprovecha y qué consecuencia tiene.
- [ ] Se usan con precisión los términos vulnerabilidad, amenaza y riesgo de la nota 08.
- [ ] Hay conclusiones individuales y generales.
- [ ] Las fuentes son recientes y están en APA 7.
- [ ] El documento no pasa de 5 cuartillas y respeta el formato.
- [ ] Si usaste herramientas de inteligencia artificial, lo declaras y explicas para qué; las conclusiones son tuyas.

---

## Preguntas frecuentes

**¿Puedo elegir dos dispositivos del mismo fabricante?**
Sí, si son de ámbitos distintos (por ejemplo, un reloj de salud y una cámara de seguridad). El análisis debe mostrar que sus vulnerabilidades no son idénticas.

**¿Tengo que probar las vulnerabilidades en un dispositivo real?**
No. Es una investigación documental. No intentes vulnerar dispositivos ajenos: sin autorización, eso puede ser delito.

**¿Qué hago si no encuentro fallas reportadas de un producto concreto?**
Analiza las debilidades típicas de su categoría y dilo así: "no se encontraron reportes públicos de este modelo; las vulnerabilidades siguientes son comunes en dispositivos similares", con su fuente.
