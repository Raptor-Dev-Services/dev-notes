# 06 - Marcos de referencia y arquitecturas de seguridad de la información

> Unidad 2, tema 3 de la materia *Seguridad de la información*. Procesos críticos, infraestructuras críticas, el marco de ciberseguridad del NIST y las arquitecturas empresariales SABSA y Zachman. Revisado a noviembre de 2026.

## Por qué importa

Hay tantas definiciones de seguridad de la información, y tantas maneras de organizar el trabajo, que una organización puede pasar años comparando marcos sin proteger nada. La salida no es escoger "el mejor" sino entender qué problema resuelve cada uno, a qué nivel trabaja y cómo se conecta con la estrategia del negocio. Esta nota recorre el camino completo: identificar lo crítico, conocer los marcos de referencia que se usan para protegerlo y ver cómo una arquitectura de seguridad amarra todo con alineación y trazabilidad.

## Marcos de referencia: definición y problema

Los **marcos de referencia** y las **arquitecturas de seguridad** son conjuntos estandarizados de conceptos, prácticas y criterios que guían la implantación de medidas de seguridad. Su proliferación tiene raíces culturales, técnicas y comerciales: cada comunidad, cada país y cada proveedor propone su forma de ordenar la compleja interacción entre los elementos de la seguridad.

Eso trae tres dificultades:

1. **¿Quién los establece y con qué autoridad?** Un marco nacido en un país o en una empresa refleja sus intereses.
2. **Los criterios no siempre encajan con la realidad local.** Lo que describen las normas internacionales suele estar lejos de los escenarios de una organización mexicana mediana, con presupuestos, proveedores y regulación distintos.
3. **No hay una visión unificada de lo que debe incluir un marco.** Se terminan comparando marcos de alto nivel con otros muy técnicos, algunos diseñados para respaldar la venta de un producto o servicio.

La diversidad también es una oportunidad: permite tomar lo mejor de cada enfoque y configurar una arquitectura que se ajuste a las necesidades de la organización, cumpla sus objetivos de control y considere los intereses reales de todas las partes. La eficacia de un estratega de seguridad está en manejar esa complejidad con más que tecnología: con teoría, experiencia y un equilibrio entre **tecnología, procesos y personas**.

## Lo crítico primero

### Infraestructura crítica y servicios esenciales

El Departamento de Seguridad Nacional de Estados Unidos (DHS) llama **infraestructura crítica** a los sistemas y activos, físicos o cibernéticos, tan vitales para el país que su incapacidad o destrucción debilitaría su seguridad física o económica, o la salud y la seguridad públicas. Esa infraestructura proporciona los **servicios esenciales** que sostienen a la sociedad.

Llevado a una organización: los servicios esenciales son el resultado de sus **procesos críticos**, y la infraestructura y los activos que los sostienen son su infraestructura crítica. La pregunta que orienta todo el gasto en seguridad es en qué área de la organización conviene concentrar los esfuerzos y los recursos.

Distinguir un proceso crítico de uno común no es trivial. Si se le pregunta a cualquier empleado cuáles son los procesos más importantes de su organización, lo más probable es que describa sus tareas diarias, no los procesos de los que depende la operación y la supervivencia de la empresa.

### La cadena de valor de Porter

Una herramienta útil para esa distinción, aunque no nació en el campo de la seguridad, es la **cadena de valor** de Michael Porter. Divide lo que hace una empresa en actividades que crean valor para el cliente:

| Actividades primarias (crean el producto o servicio) | Actividades de apoyo (hacen posibles a las primarias) |
|---|---|
| Logística de entrada: recibir y almacenar insumos | Infraestructura de la empresa: dirección, finanzas, planeación, legal |
| Operaciones: transformar insumos en producto | Gestión de recursos humanos |
| Logística de salida: almacenar y distribuir | Desarrollo de tecnología |
| Mercadotecnia y ventas | Abastecimiento o compras |
| Servicio posventa | |

La diferencia entre el valor que se entrega y el costo de todas las actividades es el **margen**. Un proceso crítico se parece mucho a un proceso primario: si se detiene, el margen desaparece. Como ninguna organización tiene recursos para proteger todos sus procesos por igual, proteger primero los procesos, la infraestructura y los activos críticos se ha vuelto práctica estándar.

Unir la planeación estratégica con la seguridad aleja al profesional de las funciones de administrador de herramientas perimetrales y lo acerca al perfil de **consultor estratega**.

### Infraestructuras críticas a cualquier escala

Una vez distinguido lo ordinario de lo crítico, el concepto de infraestructura crítica sirve para cualquier tipo de organización:

| Organización | Su infraestructura crítica podría ser |
|---|---|
| Pyme | El acceso a internet y el servidor con los datos de operación |
| Empresa mediana | El ERP, la red entre sucursales y el centro de datos |
| Operador de servicios | Los sistemas de control de su red eléctrica o de agua |
| Estado nacional | Los sistemas SCADA que controlan los oleoductos principales, la red financiera, las telecomunicaciones |

## La visión de Estados Unidos: la Orden Ejecutiva 13636

En febrero de 2013, la Casa Blanca emitió la **Orden Ejecutiva 13636, Improving Critical Infrastructure Cybersecurity**. Leerla es obligado para un profesional del área: es documentación oficial de uno de los países que más gasta en seguridad, y anticipa el rumbo de sus programas tecnológicos. Sus puntos centrales:

- **Política:** mejorar la seguridad y la resiliencia de la infraestructura crítica con un entorno cibernético que fomente la eficiencia, la innovación y la prosperidad, respetando la privacidad y las libertades civiles.
- **Intercambio de información:** producir reportes no clasificados de amenazas dirigidos a entidades específicas y ampliar los programas que comparten información con el sector privado.
- **Privacidad y libertades civiles:** incorporar protecciones en todas las actividades de la orden.
- **Proceso de consulta:** trabajar con los propietarios y operadores de la infraestructura.
- **Marco base:** encargar al NIST un marco para reducir el riesgo cibernético, con estándares, metodologías y procedimientos voluntarios.
- **Programa voluntario** para promover la adopción del marco.
- **Identificación de la infraestructura en mayor riesgo**, sector por sector.

> Actualización al 2026: la orden se emitió junto con la Directiva Presidencial PPD-21, que definía los sectores de infraestructura crítica. En abril de 2024, la PPD-21 fue sustituida por el Memorando de Seguridad Nacional NSM-22, que conserva **16 sectores** (entre ellos químico, comunicaciones, manufactura crítica, presas, base industrial de defensa, servicios de emergencia, energía, servicios financieros, alimentos y agricultura, instalaciones de gobierno, salud, tecnologías de la información, reactores nucleares, transporte y agua) y llama a las agencias responsables *Sector Risk Management Agencies*. La lista y su estado vigente se consultan en el sitio de CISA.

## El marco de ciberseguridad del NIST

De la orden ejecutiva nació en 2014 el **Framework for Improving Critical Infrastructure Cybersecurity** del NIST (versión 1.1 en 2018), conocido como CSF. Propone una visión integral de los elementos y actores de un esfuerzo que parte de las cúpulas de gobierno. Tiene tres piezas:

1. **Núcleo** (*Core*): un conjunto de actividades y resultados organizados en **funciones**, categorías y subcategorías, con referencias a normas como ISO/IEC 27001, COBIT o NIST SP 800-53. Las funciones de la versión 1.1:

| Función | Pregunta que responde |
|---|---|
| Identificar | ¿Qué tengo, qué riesgos corre y qué es crítico? |
| Proteger | ¿Qué salvaguardas aplico para que el servicio se mantenga? |
| Detectar | ¿Cómo me entero a tiempo de que algo pasó? |
| Responder | ¿Qué hago cuando ocurre un incidente? |
| Recuperar | ¿Cómo restablezco capacidades y servicios? |

2. **Niveles de implementación** (*Tiers*): qué tan madura es la gestión del riesgo, del nivel 1 (parcial) al 2 (informado por el riesgo), 3 (repetible) y 4 (adaptativo).
3. **Perfiles:** el perfil actual describe dónde está la organización; el perfil objetivo, a dónde quiere llegar. La diferencia es el plan de trabajo.

El CSF propone siete pasos para crear o mejorar un programa: priorizar y definir el alcance, orientarse (sistemas, activos, requisitos, amenazas), crear el perfil actual, evaluar el riesgo, crear el perfil objetivo, analizar y priorizar las brechas, e implantar el plan de acción.

Una lección que el marco repite: **toda iniciativa de seguridad necesita respaldo de los niveles más altos** de la jerarquía para tener éxito.

> Actualización al 2026: el 26 de febrero de 2024 el NIST publicó el **CSF 2.0**. Cambió de nombre para dirigirse a organizaciones de cualquier sector y tamaño, no solo a la infraestructura crítica, y agregó una sexta función, **Gobernar** (*Govern*), que cubre estrategia, roles, políticas, supervisión y gestión del riesgo de la cadena de suministro. Las funciones quedan como Gobernar, Identificar, Proteger, Detectar, Responder y Recuperar.

## Arquitecturas de seguridad empresarial

Además de los marcos nacionales, la bibliografía ofrece propuestas para organizaciones civiles que integran el ciclo de mejora continua. Una **arquitectura de seguridad** integra componentes de naturaleza distinta (políticas, procesos, personas, tecnología) alrededor de un objetivo común. Y, por cierto, *framework* no tiene una definición precisa: cada autor lo usa con un alcance distinto.

### Zachman

John A. Zachman propuso en 1987 un esquema para describir una empresa: una matriz que cruza seis preguntas (qué, cómo, dónde, quién, cuándo y por qué) con seis perspectivas (la de quien planea, la del dueño, la del diseñador, la del constructor, la del técnico que implementa y la de la empresa en operación). Zachman insiste en que su marco es una **estructura** (una ontología, un vocabulario ordenado para describir la empresa) y no una **metodología** (un proceso para hacer algo). Junto con TOGAF, del Open Group, y DoDAF, del Departamento de Defensa de Estados Unidos, forma el grupo de marcos de arquitectura empresarial más citados.

### SABSA

**SABSA** (*Sherwood Applied Business Security Architecture*), creada por John Sherwood, es un marco y una metodología para desarrollar arquitecturas de seguridad alineadas con los objetivos estratégicos de cualquier organización. Es una arquitectura orientada al negocio, centrada en gestionar los riesgos y en aprovechar las oportunidades que traen. Toma la idea de matriz de Zachman (algunos autores sostienen que es casi una copia) y la aplica a la seguridad:

| Capa de SABSA | Vista | Qué se define |
|---|---|---|
| Contextual | La del negocio | Qué necesita el negocio y por qué |
| Conceptual | La del arquitecto | Atributos de seguridad que el negocio requiere y objetivos de control |
| Lógica | La del diseñador | Servicios de seguridad, políticas y flujos de información |
| Física | La del constructor | Mecanismos de seguridad concretos |
| De componentes | La del especialista | Productos, herramientas y estándares |
| Operativa | La de quien gestiona el servicio | Cómo se opera, vigila y mejora la seguridad día a día |

Cada capa se analiza con las mismas seis preguntas: qué activos, por qué (motivación y riesgo), cómo (procesos), quién (personas), dónde (ubicación) y cuándo (tiempo).

No es perfecta, pero destaca por dos principios que nunca deberían faltar y que pocas veces se ven en las organizaciones:

- **Alineación:** cada control responde a un requisito del negocio.
- **Trazabilidad:** se puede seguir el hilo en ambos sentidos, desde un objetivo estratégico hasta el control que lo protege, y desde cualquier control hasta la razón de negocio que lo justifica. Es lo que permite justificar de verdad un proyecto: si no contribuye a los objetivos más altos de la organización, no debería ejecutarse.

### Propuestas comerciales

Al revisar fuentes es común encontrar arquitecturas de seguridad de fabricantes que prometen resolver todos los problemas de la organización. El estratega debe saber ubicar cada propuesta comercial dentro de su plan general: suelen cubrir bien las capas física y de componentes, y poco o nada las capas contextual y conceptual, que son responsabilidad de la propia organización.

## Para cerrar el tema

- Una arquitectura de seguridad integra componentes diversos con un objetivo común.
- *Framework* no tiene una definición única entre autores.
- Los objetivos de control salen de la evaluación y el análisis de riesgos.
- La cadena de valor ayuda a identificar los procesos críticos.
- Un proceso crítico se parece a un proceso primario de la cadena de valor.

## Glosario

| Término | Definición |
|---|---|
| Marco de referencia | Conjunto estandarizado de conceptos, prácticas y criterios que guía la implantación de la seguridad |
| Arquitectura de seguridad | Integración ordenada de políticas, procesos, personas y tecnología alrededor de un objetivo común |
| Infraestructura crítica | Sistemas y activos cuya incapacidad o destrucción debilitaría la seguridad, la economía o la salud pública |
| Servicio esencial | Servicio que resulta de los procesos críticos y del que depende la sociedad o la organización |
| Proceso crítico | Proceso del que depende la operación y la supervivencia de la organización |
| Cadena de valor | Modelo de Porter que divide la empresa en actividades primarias y de apoyo que crean valor |
| SCADA | Sistemas de supervisión, control y adquisición de datos que operan procesos industriales |
| CSF | Marco de ciberseguridad del NIST, organizado en funciones, niveles de implementación y perfiles |
| Perfil | En el CSF, la descripción del estado actual o deseado de la ciberseguridad de una organización |
| SABSA | Arquitectura de seguridad empresarial orientada al negocio, basada en capas y en trazabilidad |
| Zachman | Esquema de arquitectura empresarial que cruza seis preguntas con seis perspectivas |
| Trazabilidad | Capacidad de seguir la relación entre un objetivo de negocio y los controles que lo protegen |

## Referencias

- Archistry. (2015). *SABSA overview* [Video]. YouTube. https://youtu.be/yGqC7JqDN18
- CiscoLatam. (2016). *¿Cómo actúa la arquitectura de seguridad de Cisco?* [Video]. YouTube. https://youtu.be/_EDcVT5CEho
- Cybersecurity and Infrastructure Security Agency. (2024). *National Security Memorandum on critical infrastructure security and resilience*. https://www.cisa.gov/national-security-memorandum-critical-infrastructure-security-and-resilience
- Cybersecurity and Infrastructure Security Agency. (s. f.). *Critical infrastructure sectors*. https://www.cisa.gov/topics/critical-infrastructure-security-and-resilience/critical-infrastructure-sectors
- Executive Order No. 13636. (2013). Improving critical infrastructure cybersecurity. *Federal Register, 78*(33), 11739-11744. https://www.federalregister.gov/documents/2013/02/19/2013-03915/improving-critical-infrastructure-cybersecurity
- INTED Education. (2016). *La cadena de valor de Michael Porter en 5 minutos* [Video]. YouTube. https://youtu.be/mLQcndDR-bU
- National Institute of Standards and Technology. (2018). *Framework for improving critical infrastructure cybersecurity* (versión 1.1). https://doi.org/10.6028/NIST.CSWP.04162018
- National Institute of Standards and Technology. (2024). *The NIST Cybersecurity Framework (CSF) 2.0*. https://doi.org/10.6028/NIST.CSWP.29
- Orbus Software. (2016). *What is SABSA?* [Video]. YouTube. https://youtu.be/qbFTg85I4eE
- Porter, M. E. (1985). *Competitive advantage: Creating and sustaining superior performance*. Free Press. https://www.hbs.edu/faculty/Pages/item.aspx?num=193
- Sherwood, J., Clark, A., y Lynas, D. (2005). *Enterprise security architecture: A business-driven approach*. CRC Press. https://sabsa.org/
- The Open Group. (2022). *The TOGAF standard* (10.ª ed.). https://www.opengroup.org/togaf
- Zachman, J. A. (2008). *John Zachman's concise definition of the Zachman Framework*. Business Rules Journal. https://www.brcommunity.com/articles.php?id=b696

---

*Rogelio Arriaga Gonzalez*
