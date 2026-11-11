# Tarea - U3 - La unidad en 40 palabras

**Riesgos actuales en ciberseguridad**
**Tiempo estimado:** 4 horas, individual.

> **Estudio individual.** Esta actividad se resuelve por tu cuenta: guarda tu trabajo y evalúalo tú mismo con la rúbrica o el checklist del final. No hay foro ni entrega en la plataforma. Si llevas el curso en grupo, el instructor te indica dónde se entrega.

---

## Parte 1 - Lecturas (2 horas)

### Obligatorias

| Recurso | Sección | Tiempo est. |
|---------|---------|-------------|
| [07 - Grooming](../../../12-seguridad/02-riesgos/07-grooming.md) | Todo el documento; releer "Las cuatro etapas", "Señales de alerta" y "El marco legal" | 45 min |
| [08 - Phishing](../../../12-seguridad/02-riesgos/08-phishing.md) | Todo el documento; releer "Variantes" y "La economía del phishing" | 40 min |
| [09 - Pharming](../../../12-seguridad/02-riesgos/09-pharming.md) | Todo el documento; releer "Cómo funciona la navegación" y "Las formas del ataque" | 35 min |

Mientras lees, apunta cada término que te parezca importante: es la materia prima del ejercicio.

---

## Parte 2 - Ejercicio (2 horas)

### Una nube de palabras que explique la unidad

Una nube de palabras parece un adorno, pero hacerla bien obliga a decidir qué conceptos sostienen la unidad. Aquí el tamaño de cada palabra **no** sale de cuántas veces aparece en un texto, sino de cuánto pesa en el tema, y esa decisión la defiendes tú.

#### 1. Elige 40 palabras

- Al menos **10 por tema**: grooming, phishing y pharming. Las restantes, hasta 10, pueden ser transversales (por ejemplo, ingeniería social o usurpación de identidad).
- Solo términos que aparezcan en las lecturas y signifiquen algo en la unidad. "Internet" o "computadora" no cuentan, salvo que puedas explicar qué papel juegan aquí.
- Se valen términos de varias palabras, como "envenenamiento de caché DNS" o "fase sexual". Cuida que la herramienta no los parta.

#### 2. Asígnale un peso a cada una

- Escala del 1 al 5. Un 5 es un concepto sin el cual el tema no se entiende; un 1, un detalle útil pero prescindible.
- Como máximo **cinco palabras con peso 5** en toda la nube. Si todo es central, nada lo es.

#### 3. Construye la nube

- Usa la herramienta que prefieras: un generador en línea, un programa de diseño o un script propio.
- El tamaño sigue el peso. Si tu herramienta lo calcula por frecuencia, repite cada palabra tantas veces como su peso o usa su opción de pesos.
- Un color por tema y uno neutro para las transversales, con una **leyenda** que lo explique.
- Que todo se lea: buen contraste, tipografía legible, ninguna palabra cortada, pocas en vertical y los acentos completos.

Si prefieres programarla, la biblioteca `wordcloud` de Python acepta los pesos directamente y respeta los términos compuestos:

```python
from wordcloud import WordCloud

pesos = {"grooming": 5, "phishing": 5, "pharming": 5, "DNS": 4, "smishing": 3}  # ... hasta 40
nube = WordCloud(width=1600, height=900, background_color="white").generate_from_frequencies(pesos)
nube.to_file("nube-u3.png")
```

#### 4. Escribe el anexo: 40 definiciones de una línea

Una tabla con las 40 palabras, ordenadas por tema y, dentro de cada tema, de mayor a menor peso:

```markdown
| Palabra | Tema | Peso | Definición (una línea, con tus palabras) | Fuente |
|---|---|---|---|---|
| Grooming | Grooming | 5 | ... | UNODC (2015) |
| Drive-by pharming | Pharming | 3 | ... | Stamm et al. (2007) |
```

- Cada definición cabe en una línea (25 palabras como máximo) y es tuya, no copiada.
- Cada fila dice su fuente: las notas de la unidad o las fuentes que ellas citan.

#### 5. Defiende tus cinco pesos más altos

Un párrafo de 100 a 150 palabras: por qué esas cinco palabras son el centro de la unidad, y cuál dejaste fuera del peso 5 aunque te costó trabajo.

### Formato del documento

Un solo PDF (o un Markdown con la imagen incrustada) de **3 a 5 páginas**, con este orden:

1. Portada: título, nombre y fecha.
2. La nube a página completa, en PNG o SVG legible, con su leyenda.
3. El anexo de las 40 definiciones.
4. El párrafo del paso 5.
5. Referencias en APA 7, al menos tres fuentes confiables.

---

## Criterio de aceptación

- [ ] Son exactamente 40 palabras, con al menos 10 por tema.
- [ ] El tamaño de cada palabra corresponde al peso del anexo, y hay como máximo cinco con peso 5.
- [ ] La leyenda explica los colores y cada palabra de la nube está en el anexo, y al revés.
- [ ] Las 40 definiciones son propias, de una línea, y cada una tiene fuente.
- [ ] El párrafo de los cinco pesos más altos argumenta, no solo enumera.
- [ ] Entre 3 y 5 páginas, con referencias en APA 7.

---

## Rúbrica de autoevaluación

| Criterio | Excelente | Bueno | Regular | Insuficiente |
|---|---|---|---|---|
| **Calidad del contenido** (2 pts) | 2: las 40 palabras son conceptos de la unidad, cubren los tres temas y las definiciones son correctas, propias y con ejemplo cuando hace falta | 1.5: cubre los tres temas, pero hay hasta cuatro palabras de relleno o definiciones imprecisas | 1: un tema queda corto o hay de cinco a ocho definiciones con errores | 0: las palabras no corresponden a la unidad, o falta el anexo o la mitad de las definiciones |
| **Organización** (1 pt) | 1: el anexo sigue el orden pedido y la leyenda permite ubicar cada palabra en su tema | 0.75: el orden es claro, pero la leyenda está incompleta o hay pocos desajustes entre nube y anexo | 0.5: el orden es parcial y varias palabras de la nube no están en el anexo, o al revés | 0: no hay un orden reconocible |
| **Diseño** (2 pts) | 2: el tamaño sigue el peso, los colores distinguen los temas y todo se lee sin esfuerzo | 1.5: tamaño y color funcionan, pero el contraste, la tipografía o la orientación estorban en algunas palabras | 1: en varias palabras el tamaño no corresponde al peso, o el diseño distrae del contenido | 0: la nube no se lee, o el tamaño no guarda relación con la importancia |
| **Presentación y ortografía** (1.5 pts) | 1.5: respeta el formato y la extensión, sin faltas de ortografía ni de sintaxis, con acentos también en la nube | 1.1: formato y extensión correctos, con dos a cuatro errores | 0.75: la extensión se sale del rango o hay de cinco a siete errores | 0: no sigue el formato pedido o tiene ocho errores o más |
| **Fuentes de información** (1 pt) | 1: cada definición dice su fuente; al menos tres fuentes confiables y vigentes, bien citadas en APA 7 | 0.75: fuentes confiables y en APA 7, pero alguna desactualizada o faltan en pocas definiciones | 0.5: fuentes incompletas o con errores de formato | 0: sin fuentes, o sin formato de cita |
| **Total** | **7.5** | | | |

Suma tus puntos. Con menos de 5.25 (70 %), vuelve a leer la nota indicada y rehaz el apartado más débil.

---

## Preguntas frecuentes

**¿Puedo usar una palabra en inglés?**
Sí, si es el término que usa la unidad (*spear phishing*, *passkey*, *drive-by pharming*). Su definición va en español.

**¿"Phishing" y "spear phishing" cuentan como dos palabras?**
Sí, son conceptos distintos. Lo que no vale es inflar la lista con variaciones de lo mismo, como "phisher" y "phishers".
