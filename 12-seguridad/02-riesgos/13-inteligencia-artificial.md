# 13 - Inteligencia artificial: la misma herramienta para atacar y defender

> Materia 2, unidad 5 (Prospectivas y ataques futuros), tema 1. Nota recreada a partir del material de la materia. Es el tema que más cambió desde que se escribió el original: las actualizaciones al 2026 van marcadas.

## Por qué importa

La inteligencia artificial (IA) pasó en pocos años de ser una palabra de ciencia ficción a estar en el teclado del teléfono, en el filtro de spam del correo y en la herramienta con la que miles de personas redactan, programan o resumen documentos. Sus beneficios son reales. Pero en manos de un delincuente la IA se vuelve un complemento formidable: automatiza tareas que antes requerían tiempo y especialistas, escribe engaños sin faltas de ortografía, imita voces y busca debilidades en sistemas a una velocidad que ningún equipo humano iguala.

Al mismo tiempo, la defensa también depende cada vez más de la IA: modelar el comportamiento normal de una red, detectar lo anómalo y reaccionar en segundos ya no es posible sin ella. Esta nota explica qué es la IA, qué tecnologías la componen, cómo se usa de los dos lados y hacia dónde apuntan los ataques.

## Qué es la inteligencia artificial

La IA es la **simulación de procesos de la inteligencia humana por parte de máquinas y sistemas informáticos**. Esos procesos son tres:

| Proceso | Qué significa en una máquina |
|---|---|
| Aprendizaje | Adquirir información y las reglas para usarla. |
| Razonamiento | Aplicar esas reglas para llegar a conclusiones, aproximadas o definitivas. |
| Autocorrección | Mejorar de forma continua a partir de sus aciertos y errores. |

Entre sus aplicaciones particulares clásicas están los **sistemas expertos**, el **reconocimiento de voz** y la **visión artificial**.

Como el hardware, el software y el personal para desarrollar IA son caros, los grandes proveedores la ofrecen como servicio en la nube, para que personas y empresas experimenten antes de comprometerse. El material original mencionaba los servicios de Amazon, el asistente IBM Watson, los servicios cognitivos de Microsoft y los de Google.

> Actualización al 2026: el panorama de la IA como servicio lo dominan hoy los **modelos generativos**, sobre todo los modelos de lenguaje de gran tamaño (LLM), que escriben texto, código e imágenes a partir de instrucciones en lenguaje natural y se contratan por API a varios proveedores. Su llegada masiva, a partir de finales de 2022, cambió el tema de esta nota más que cualquier otro avance.

### La cuestión ética

Las herramientas de IA dan a las empresas funciones nuevas, pero también plantean dilemas. Los algoritmos de **aprendizaje profundo**, en los que se basan las herramientas más avanzadas, solo son tan buenos como los datos con los que se entrenan, y como esos datos los selecciona una persona, siempre existe el riesgo de que hereden **sesgos humanos** que hay que vigilar de cerca.

Algunos especialistas creen además que el término "inteligencia artificial" arrastra demasiada carga de la cultura popular y genera miedos poco realistas y expectativas exageradas. Por eso proponen hablar de **inteligencia aumentada**, una etiqueta más neutral que subraya que la IA mejora productos y servicios sin sustituir a las personas que los usan.

## Las tecnologías que componen la IA

| Tecnología | Qué es | Ejemplo |
|---|---|---|
| Automatización | Hacer que sistemas o procesos funcionen sin intervención humana. La automatización robótica de procesos (RPA) usa software para tareas repetitivas y de gran volumen; a diferencia de la automatización de TI tradicional, más rígida, se adapta a cambios en los datos. | Capturar facturas en un sistema contable. |
| Aprendizaje automático | Disciplina que construye sistemas que aprenden y mejoran con la experiencia sin programarse explícitamente para cada tarea; en esencia, automatiza el análisis predictivo a partir de datos históricos. | Predecir qué transacción es fraudulenta. |
| Visión artificial | Permite a las computadoras interpretar información visual: captura con cámaras, conversión a datos digitales y procesamiento de señales. No tiene los límites biológicos del ojo humano. | Análisis de imágenes médicas, identificación de objetos. |
| Procesamiento de lenguaje natural (PLN) | Capacidad de un programa para interpretar y manejar el lenguaje humano; hoy se basa en aprendizaje automático. | Detección de spam, traducción, análisis de sentimientos, reconocimiento de voz. |
| Robótica | Ingeniería que diseña y fabrica robots para tareas complejas o repetitivas. | Líneas de ensamble de autos; brazos que la NASA usa en el espacio; robots que interactúan socialmente. |
| Vehículos autónomos | Combinan visión artificial, reconocimiento de imágenes y aprendizaje profundo para manejar sin conductor: mantenerse en el carril y evitar obstáculos como peatones. | Robotaxis y asistentes de manejo. |

### Los tres tipos de aprendizaje automático

1. **Supervisado:** se entrena con datos etiquetados, que incluyen la entrada y la respuesta correcta, y el modelo aprende a predecir o clasificar casos nuevos.
2. **No supervisado:** trabaja con datos sin etiquetas y busca patrones o estructuras ocultas, por ejemplo con agrupamiento o reducción de dimensionalidad.
3. **Por refuerzo:** un agente aprende interactuando con un entorno, recibe recompensas o castigos según sus acciones y descubre la estrategia que maximiza su desempeño.

## Dónde se aplica

- **Salud:** diagnósticos más precisos y baratos; sistemas que analizan el expediente con PLN y proponen hipótesis con un nivel de confianza; asistentes virtuales que agendan citas y ayudan con la facturación.
- **Empresas:** RPA para tareas monótonas, aprendizaje automático integrado en plataformas de análisis y de relación con clientes, y chatbots que atienden en línea. La automatización de puestos de trabajo es un debate abierto.
- **Educación:** calificación automática, aprendizaje personalizado y tutores virtuales que se adaptan a cada estudiante.
- **Finanzas:** aplicaciones de finanzas personales que asesoran y ordenan los datos de los usuarios, apoyo en créditos hipotecarios y operaciones bursátiles en las que el software ejecuta buena parte de las transacciones.
- **Derecho:** automatización de la revisión de documentos y asistentes que responden preguntas a partir de bases jurídicas.
- **Manufactura:** robots industriales que pasaron de tareas aisladas a colaborar con los trabajadores.

> Actualización al 2026: algunos ejemplos del material original ya no existen como se describían. IBM vendió en 2022 sus activos de datos y analítica de salud (Watson Health), que hoy operan como la empresa Merative, y la aplicación de finanzas personales Mint cerró en marzo de 2024 y migró a Credit Karma. Las aplicaciones de la IA siguen vigentes; cambiaron los productos.

## La IA en la ciberseguridad

### Del lado defensivo

Las infraestructuras críticas son muy vulnerables a intrusiones, y los sensores y detectores físicos no bastan para vigilarlas. Hace falta tecnología capaz de **modelar el comportamiento normal y detectar el anormal**. Los sistemas de defensa deben ser flexibles, adaptables y robustos, detectar amenazas muy variadas y decidir en tiempo real. La IA aporta justo eso:

- Analiza grandes volúmenes de datos e identifica patrones sospechosos que un enfoque tradicional dejaría pasar.
- Con aprendizaje automático, mejora de forma continua y se adapta a las tácticas nuevas de los atacantes.
- Automatiza tareas de seguridad (de la detección de intrusiones a la respuesta a incidentes), lo que da más visibilidad, reduce el tiempo de respuesta y libera a los especialistas para los casos complejos.

Incorporar la IA a la defensa puede, además, emparejar el terreno: usuarios, empresas y gobiernos que no tienen grandes equipos de seguridad obtienen capacidades de detección que antes solo estaban al alcance de unos cuantos.

Para que un sistema inteligente sea eficaz en ciberseguridad, el problema se descompone en capacidades: deducción, razonamiento y resolución de problemas; representación del conocimiento; planificación; aprendizaje automático; procesamiento de lenguaje natural (recuperación de información, minería de texto, traducción); movimiento y manipulación (navegación, localización, mapeo); percepción (reconocimiento de voz, facial y de objetos); inteligencia social (simulación de empatía); creatividad; e inteligencia general.

### Del lado ofensivo

La misma potencia sirve para atacar:

- **Ataques automatizados y persistentes.** En un ataque de denegación de servicio distribuido (DDoS), una IA podría administrar el ataque sola, ajustando tiempos y flujo de datos sin supervisión, lo que lo vuelve más duradero y difícil de mitigar.
- **Imitación humana.** Un sistema de IA bien diseñado puede operar en segundo plano imitando a un atacante humano sin exponer a quien lo controla, y conversar en lenguaje natural con víctimas que no sospechan que hablan con una máquina. Una sola máquina puede atacar a miles de personas a la vez.
- **Phishing a la medida.** Analizando datos de redes sociales y otras fuentes, la IA redacta mensajes personalizados y creíbles, y un chatbot puede sostener la conversación en tiempo real.
- **Malware que cambia.** La IA facilita crear y adaptar variantes de malware y ransomware, lo que complica su detección; el ransomware y el cryptojacking crecen en parte por esto.
- **Lógica de negocio.** Los ciberdelincuentes evalúan sus técnicas como cualquier empresario: por eficacia, pero también por lo que cuesta desarrollarlas y ejecutarlas. La IA abarata el ataque.

Mientras las empresas adoptan IA, aprendizaje automático y cadenas de bloques para mejorar sus operaciones, los delincuentes adoptan las mismas herramientas para superar las defensas.

## Los días cero y la IA

Una **vulnerabilidad de día cero** es una falla que todavía no ha sido detectada ni corregida por el fabricante; un exploit de día cero la aprovecha. Son muy codiciadas porque atacan vectores que nadie conoce y contra los que las herramientas tradicionales no tienen firma.

Tradicionalmente eran caras: descubrirlas y explotarlas requería mucho tiempo, esfuerzo y especialistas. Una de las técnicas más eficaces para encontrarlas es el **fuzzing**: enviar datos aleatorios o semialeatorios a un programa para provocar fallos o comportamientos inesperados que revelen una vulnerabilidad. Durante años fue una técnica sofisticada, limitada a laboratorios y a investigadores profesionales.

Las predicciones de Fortinet para 2019, publicadas en noviembre de 2018, anticipaban que los delincuentes combinarían aprendizaje automático y fuzzing para automatizar el descubrimiento de días cero. El razonamiento del material es el siguiente:

| Etapa | Qué cambia |
|---|---|
| Fuzzing con IA | Los modelos generan casos de prueba más inteligentes y analizan los resultados a gran escala; encontrar fallas se vuelve más rápido y accesible. |
| Días cero como servicio | Los exploits dejan de ser rarezas y se convierten en una **mercancía** que se vende en la web oscura, lo que amplía los vectores de ataque y presiona los costos de los servicios delictivos. |
| Peligros combinados | La IA integrada en enjambres de bots autónomos que aprenden solos podría descubrir y explotar vulnerabilidades en tiempo real. |
| Amenazas sin control | Un enjambre de fuzzing con IA desplegado sin supervisión podría causar disrupciones masivas y volver la respuesta a incidentes extremadamente compleja. |

Para las organizaciones, la consecuencia es que resulta prácticamente imposible anticipar un día cero con herramientas aisladas y tradicionales: hace falta defensa en profundidad, segmentación, monitoreo del comportamiento y capacidad de respuesta rápida.

> Actualización al 2026: la predicción se cumplió en parte, y primero del lado defensivo. En octubre de 2024, el agente Big Sleep de Google (Project Zero y DeepMind) encontró una vulnerabilidad explotable en la base de datos SQLite antes de que llegara a una versión publicada, y en agosto de 2025 la final de la competencia AI Cyber Challenge de DARPA, ganada por el equipo Team Atlanta, mostró sistemas autónomos que encuentran y corrigen fallas en software real. Los atacantes usan los mismos modelos, sobre todo para phishing, ingeniería social y desarrollo de malware. Además, los propios sistemas de IA se volvieron un blanco nuevo: la **inyección de instrucciones** (*prompt injection*) encabeza la lista de riesgos para aplicaciones con LLM que publica OWASP.

## La regulación

El material original señalaba que la regulación de la IA era limitada e indirecta. Por ejemplo, en Estados Unidos las normas de crédito obligan a los bancos a explicar por qué niegan un préstamo, lo que restringe el uso de modelos de aprendizaje profundo poco transparentes; y el Reglamento General de Protección de Datos de la Unión Europea limita el uso de datos personales para entrenar y aplicar IA de consumo. El ritmo de la tecnología suele rebasar la capacidad de los reguladores.

> Actualización al 2026: el cambio más grande es el **Reglamento (UE) 2024/1689**, la primera ley integral de IA del mundo. Entró en vigor el 1 de agosto de 2024 y se aplica por etapas: las prácticas prohibidas desde febrero de 2025, las obligaciones para modelos de uso general desde agosto de 2025 y las de transparencia (incluido el etiquetado de deepfakes) desde agosto de 2026. En julio de 2026, el llamado Ómnibus digital (Reglamento (UE) 2026/1744) movió las obligaciones de los sistemas de alto riesgo a diciembre de 2027 y agosto de 2028, y agregó desde diciembre de 2026 la prohibición de sistemas que generen imágenes íntimas no consentidas o material de abuso sexual infantil. En Estados Unidos, el Marco de Gestión de Riesgos de IA del NIST (enero de 2023, hoy en revisión) se volvió la referencia voluntaria para las organizaciones.

## Para cerrar

La IA amplía tanto la capacidad de proteger como la de atacar, y la historia muestra que cualquier tecnología, por avanzada que sea, puede explotarse con fines dañinos. La única defensa efectiva es usarla de manera consciente y orientada a la seguridad: conocer cómo se aplica a la protección de datos, entender sus límites y mantenerse al día con su evolución, porque cada avance amplifica también los riesgos.

## Glosario

| Término | Definición |
|---------|-----------|
| Inteligencia artificial (IA) | Simulación de procesos de la inteligencia humana (aprendizaje, razonamiento, autocorrección) por sistemas informáticos. |
| Aprendizaje automático | Rama de la IA en la que los sistemas aprenden de los datos sin programarse para cada tarea. |
| Aprendizaje profundo | Aprendizaje automático con redes neuronales de muchas capas; base de las herramientas más avanzadas. |
| Modelo de lenguaje (LLM) | Modelo entrenado con enormes cantidades de texto que genera y comprende lenguaje natural. |
| Sesgo algorítmico | Error sistemático de un modelo que reproduce prejuicios presentes en sus datos de entrenamiento. |
| RPA | Automatización robótica de procesos: software que ejecuta tareas repetitivas de oficina. |
| Chatbot | Programa que conversa con personas y responde de forma automática. |
| Fuzzing | Técnica de prueba que envía datos inválidos o aleatorios a un programa para encontrar fallos. |
| Día cero | Vulnerabilidad desconocida para el fabricante y sin corrección disponible. |
| Exploit | Código o técnica que aprovecha una vulnerabilidad. |
| DDoS | Ataque de denegación de servicio distribuido: saturar un servicio desde muchos equipos a la vez. |
| Infraestructura crítica | Instalaciones y servicios esenciales (energía, agua, salud, finanzas) que deben seguir operando ante desastres o ataques. |
| Inyección de instrucciones | Ataque que mete órdenes ocultas en el texto que procesa un modelo de IA para desviar su comportamiento. |
| RGPD | Reglamento General de Protección de Datos de la Unión Europea. |

## Referencias

- Big Sleep team. (2024, 1 de noviembre). *From Naptime to Big Sleep: Using large language models to catch vulnerabilities in real-world code*. Project Zero. https://projectzero.google/2024/10/from-naptime-to-big-sleep.html
- Caspian Report. (2018, 16 de diciembre). *Global race for AI superiority* [Video]. YouTube. https://youtu.be/_0gUXxZ-ExE
- Check Point Software Technologies. (s. f.). *Artificial intelligence: A silver bullet in cyber security? CPX 360 keynote* [Video]. YouTube. https://www.youtube.com/@CheckPointSW
- DARPA. (2025, 8 de agosto). *AI Cyber Challenge marks pivotal inflection point for cyber defense*. https://www.darpa.mil/news/2025/aixcc-results
- DW Documentary. (2019, 14 de agosto). *Artificial intelligence and its ethics* [Video]. YouTube. https://youtu.be/Izd2qOgOGQI
- Fortinet. (2018, 15 de noviembre). *Fortinet predicts organizations will employ more automation to combat threats* [Comunicado de prensa]. https://www.fortinet.com/corporate/about-us/newsroom/press-releases/2018/fortinet-predicts-organizations-will-employ-more-automation-to-c
- Francisco Partners. (2022, 30 de junio). *Francisco Partners completes acquisition of IBM's healthcare data and analytics assets; launches healthcare data company Merative*. https://www.franciscopartners.com/media/Merative
- National Institute of Standards and Technology. (2023). *Artificial intelligence risk management framework (AI RMF 1.0)* (NIST AI 100-1). https://doi.org/10.6028/NIST.AI.100-1
- OWASP Foundation. (2025). *OWASP top 10 for large language model applications*. https://genai.owasp.org/llm-top-10/
- Parlamento Europeo y Consejo de la Unión Europea. (2016). *Reglamento (UE) 2016/679 relativo a la protección de las personas físicas en lo que respecta al tratamiento de datos personales*. EUR-Lex. https://eur-lex.europa.eu/legal-content/ES/TXT/?uri=CELEX:32016R0679
- Parlamento Europeo y Consejo de la Unión Europea. (2024). *Reglamento (UE) 2024/1689 por el que se establecen normas armonizadas en materia de inteligencia artificial*. EUR-Lex. https://eur-lex.europa.eu/legal-content/ES/TXT/?uri=CELEX:32024R1689
- Parlamento Europeo y Consejo de la Unión Europea. (2026). *Reglamento (UE) 2026/1744 por el que se modifica el Reglamento (UE) 2024/1689 (Ómnibus digital sobre IA)*. EUR-Lex. https://eur-lex.europa.eu/eli/reg/2026/1744/oj

---

*Rogelio Arriaga Gonzalez*
