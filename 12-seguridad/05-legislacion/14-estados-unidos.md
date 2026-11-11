# 14 - Regulación de la ciberseguridad en Estados Unidos

> Materia 5 (Legislación nacional e internacional en ciberseguridad), unidad 5 (Regulación internacional), tema 2. Nota recreada a partir del material de la materia y actualizada al 10 de noviembre de 2026.

## Por qué importa

Estados Unidos concentra a buena parte de los proveedores de nube, redes sociales, sistemas operativos y software empresarial que usan las organizaciones mexicanas. Sus reglas de seguridad y privacidad llegan a México por tres vías: los contratos con esos proveedores, las cadenas de suministro con empresas estadounidenses y el T-MEC, cuyo capítulo de comercio digital habla de cooperación en ciberseguridad. Además, muchos marcos que se usan como buena práctica en todo el mundo (el del NIST, por ejemplo) nacieron como respuesta a leyes de ese país.

El caso estadounidense enseña algo que no se ve en Europa: se puede tener un ecosistema de ciberseguridad muy desarrollado sin una ley única que lo ordene. Entender cómo funciona ese mosaico, y cuáles son sus huecos, ayuda a valorar lo que México podría copiar y lo que le convendría evitar.

## Un mosaico, no un código

La vida diaria, la economía y la seguridad nacional estadounidenses dependen de redes estables y resilientes: con ellas se comunican las personas, se mueve el transporte, se distribuye la energía, se administra el sistema financiero y se prestan los servicios de gobierno. En la última década las intrusiones crecieron mucho, expusieron información personal y comercial, interrumpieron operaciones críticas y le costaron caro a la economía.

Aun así, no existe una ley de ciberseguridad que se aplique a todo. Lo que hay es una suma de:

- leyes federales por sector (salud, finanzas, gobierno federal);
- leyes de cada estado, sobre todo de aviso de brechas y de privacidad;
- reglamentos de agencias federales;
- criterios de los tribunales federales y estatales, que en muchos casos definen qué es seguridad "razonable".

A eso se suma que una empresa estadounidense con clientes o empleados en otros países debe cumplir también las leyes de privacidad y seguridad de esos países. El sistema se apoya más en la sanción después del hecho (acciones de las agencias y demandas privadas) que en permisos o controles previos.

### Qué entiende el gobierno por ciberseguridad

El glosario de la Iniciativa Nacional para Carreras y Estudios en Ciberseguridad (NICCS), que hoy administra la agencia CISA, la define como la actividad, el proceso, la capacidad o el estado mediante el cual los sistemas de información y comunicaciones, y la información que contienen, quedan protegidos o defendidos contra daño, uso o modificación no autorizados, o explotación. La definición se apoya en la tríada de confidencialidad, integridad y disponibilidad, pero en la práctica la ciberseguridad estadounidense ya no se limita a proteger activos de información: en ella confluyen la vigilancia, el acceso legal a datos por parte de las autoridades, los derechos humanos y la colaboración entre gobierno y empresas.

### Las brechas que cambiaron la conversación

Las violaciones de datos existían antes de la digitalización, pero las plataformas en línea multiplicaron su volumen y su costo. En 2016 Yahoo reveló dos ataques: uno de 2014 que afectó al menos 500 millones de cuentas y otro de 2013 que alcanzó más de mil millones de registros.

**Actualización al 2026:** en octubre de 2017 Yahoo corrigió la cifra del ataque de 2013: comprometió las 3,000 millones de cuentas que la empresa tenía en ese momento. Sigue siendo una de las brechas más grandes registradas.

## Los antecedentes federales

Tres leyes federales son la base histórica. Todas exigen proteger sistemas e información, pero solo a un nivel "razonable" y solo a ciertos sujetos:

| Ley | Año | A quién obliga | Qué protege |
|---|---|---|---|
| HIPAA (Ley de Portabilidad y Responsabilidad del Seguro de Salud) | 1996 | Prestadores de salud, aseguradoras y sus socios de negocio | Información de salud identificable |
| Ley Gramm-Leach-Bliley (GLBA) | 1999 | Instituciones financieras | Información financiera no pública de los clientes |
| Ley de Seguridad Nacional (Homeland Security Act) y Ley de Gobierno Electrónico, que incluyeron la FISMA | 2002 | Agencias federales | Sistemas de información del gobierno federal |

La FISMA obliga a cada agencia a desarrollar e implantar políticas, principios, estándares y directrices de seguridad de la información, y encarga al NIST escribir los estándares para lograrlo. De ahí salen las publicaciones de la serie SP 800 que se estudian en la nota del NIST.

Estas leyes dejaron dos huecos señalados desde entonces: no cubren a sectores enteros como los proveedores de acceso a internet o las empresas de software, y su lenguaje es tan general que deja mucho margen de interpretación sobre qué medidas bastan.

**Actualización al 2026:** la FISMA de 2002 fue sustituida por la Ley Federal de Modernización de la Seguridad de la Información de 2014 (que conserva la sigla FISMA), y en 2021 la regla de salvaguardas de la FTC derivada de la GLBA se endureció: pide un programa escrito de seguridad, un responsable designado, autenticación multifactor y cifrado; desde mayo de 2024 obliga además a avisar a la FTC brechas que afecten a 500 o más consumidores.

## Los estados: la presión de la transparencia

Ante la falta de una ley federal general, los estados encontraron un mecanismo sencillo: obligar a las empresas a decir públicamente cuando fallan. La ley de aviso de brechas de California, aprobada en 2002 y vigente desde julio de 2003, exige a cualquier organización que tenga datos personales de residentes de ese estado y sufra una violación de seguridad avisar a los afectados. No le dice a la empresa cómo protegerse; la castiga con exposición pública si no lo hace.

El efecto fue un incentivo para invertir en seguridad antes del incidente, por el costo reputacional y económico de tener que avisar. Funcionó en California y otros estados copiaron el modelo.

**Actualización al 2026:** desde 2018 los 50 estados, el Distrito de Columbia y los territorios tienen leyes de aviso de brechas. California fue más allá con la Ley de Privacidad del Consumidor (CCPA, 2018, vigente desde 2020), reforzada por la Ley de Derechos de Privacidad (CPRA, 2020), que crearon derechos de acceso, eliminación y exclusión de la venta de datos, y una agencia estatal de privacidad. Otros estados aprobaron leyes integrales parecidas, lo que agrava el mosaico.

Con todo, más leyes no han significado menos ataques. Como ha advertido el criptógrafo Bruce Schneier, los sistemas del gobierno siguen sufriendo intrusiones exitosas pese a los esfuerzos regulatorios, y lo mismo pasa en el sector privado. La conclusión práctica para cualquier organización es que el cumplimiento no sustituye a la prevención: hay que revisar los sistemas con regularidad, encontrar las vulnerabilidades y corregirlas pronto, porque los atacantes buscan oportunidades todo el tiempo y sus métodos se sofistican.

## La FTC como regulador de facto

En Estados Unidos no hay una ley general que imponga requisitos concretos de privacidad y seguridad a todas las empresas. Lo más parecido es la sección 5 de la Ley de la Comisión Federal de Comercio, que prohíbe las prácticas comerciales desleales o engañosas. Con base en ella, la FTC sanciona a empresas que prometen proteger datos y no lo hacen, o que tienen una seguridad tan deficiente que resulta desleal para sus clientes. Otras agencias regulan sectores específicos (Salud y Servicios Humanos, Educación, la Comisión Federal de Comunicaciones), pero solo la FTC alcanza a empresas de casi cualquier sector, siempre que participen en el comercio interestatal.

La FTC ha publicado guías para orientar a las empresas:

**Protecting Personal Information: A Guide for Business (noviembre de 2011)** propone cinco principios para un plan de seguridad:

| Principio | En inglés | Idea |
|---|---|---|
| Inventariar | Take stock | Saber qué datos personales se tienen, dónde están y quién accede |
| Reducir | Scale down | Conservar solo lo que el negocio necesita |
| Proteger | Lock it | Resguardar física y lógicamente lo que se conserva |
| Desechar | Pitch it | Eliminar de forma segura lo que ya no se necesita |
| Planear | Plan ahead | Tener listo un plan de respuesta a incidentes |

**Start with Security: A Guide for Business (junio de 2015)** no es vinculante, pero resume lecciones de los casos que la FTC llevó contra empresas en diez principios: empezar por la seguridad desde el diseño del negocio; controlar con criterio el acceso a los datos; exigir contraseñas seguras y autenticación; guardar la información sensible de forma segura y protegerla en tránsito; segmentar la red y vigilar quién intenta entrar o salir; asegurar el acceso remoto; aplicar prácticas seguras al desarrollar productos; verificar que los proveedores de servicios protejan lo que se les confía; mantener la seguridad al día y atender las vulnerabilidades; y proteger también el papel, los medios físicos y los dispositivos.

Las grandes filtraciones y el caso Cambridge Analytica (2018) llevaron a especialistas y legisladores a pedir un marco federal común al estilo del RGPD europeo. El debate siempre choca con el valor económico que los datos tienen en la economía digital estadounidense.

**Actualización al 2026:** los proyectos de ley federal integral de privacidad presentados en 2022 y 2024 no se aprobaron; hasta donde se pudo verificar, Estados Unidos sigue sin una ley federal general de protección de datos.

## La Ley de Ciberseguridad de 2010 (S. 773)

El proyecto conocido como Ley de Ciberseguridad de 2010, o Rockefeller-Snowe por los senadores que lo impulsaron, se presentó en el Senado por la preocupación creciente ante ataques que afectaban redes estadounidenses y de todo el mundo. Su propósito declarado era asegurar el flujo libre y continuo del comercio dentro del país y con sus socios mediante comunicaciones cibernéticas seguras, seguir desarrollando internet e intranets para esos fines, formar un cuerpo de especialistas en tecnologías de la información y mantener defensas eficaces contra interrupciones.

Sus ideas centrales siguen vigentes como diagnóstico:

- El ciberespacio es parte integral de la sociedad y una infraestructura de la que dependen todos los aspectos de la vida; la Casa Blanca lo identificó como un activo nacional estratégico.
- Internet es inseguro por diseño y las amenazas de cibercrimen y terrorismo crecen.
- Se necesitan una estrategia integral de seguridad nacional y más profesionales calificados, formados mediante políticas, guías y presupuesto para defender a los sectores público y privado de los ciberdelitos y de la guerra cibernética.

**Actualización al 2026:** el S. 773 del 111.º Congreso fue dictaminado por la Comisión de Comercio del Senado (informe S. Rept. 111-384, de 22 de diciembre de 2010), pero nunca se votó en el pleno ni llegó a ser ley. Por eso no debe citarse como norma vigente. La ley que sí se aprobó años después fue la **Ley de Ciberseguridad de 2015**, incluida como división N de la Ley de Asignaciones Consolidadas de 2016 y promulgada el 18 de diciembre de 2015.

## Leyes nuevas: el intercambio de información

La pieza central de la Ley de Ciberseguridad de 2015 es la **Ley de Intercambio de Información sobre Ciberseguridad (CISA 2015)**. Su objetivo es mejorar la ciberseguridad del país mediante el intercambio de información sobre amenazas: permite que empresas y gobierno compartan indicadores de amenaza y medidas defensivas, y da a las empresas protección de responsabilidad cuando lo hacen conforme a la ley. Su antecedente se presentó en el Senado en julio de 2014; la versión que prosperó se aprobó en el Senado el 27 de octubre de 2015.

**Actualización al 2026:** la CISA 2015 tenía fecha de vencimiento el 30 de septiembre de 2025. Expiró ese día, durante el cierre del gobierno federal, y desde entonces el Congreso la ha prorrogado varias veces de forma temporal dentro de leyes de financiamiento (a enero y luego a septiembre de 2026, con una prórroga posterior); al cierre de esta nota no tenía una reautorización de largo plazo. No hay que confundirla con la **CISA agencia** (Cybersecurity and Infrastructure Security Agency), creada en noviembre de 2018 dentro del Departamento de Seguridad Nacional.

## La estrategia internacional para el ciberespacio (2011)

En mayo de 2011 la Casa Blanca publicó *International Strategy for Cyberspace: Prosperity, Security, and Openness in a Networked World*, el primer documento que fijó la postura del país hacia el resto del mundo en esta materia. Buscaba prosperidad económica, más seguridad y un ciberespacio abierto, y lo planteaba como una agenda de colaboración internacional basada en las libertades fundamentales, la privacidad y el libre flujo de información.

Su apuesta era que normas de comportamiento responsable guiaran la conducta de los Estados, con cooperación y respeto al Estado de derecho, y sobre cuatro valores: apertura, seguridad, interoperabilidad y confianza. Las normas que proponía eran diez:

1. Defensa de las libertades fundamentales.
2. Respeto a la propiedad.
3. Valor de la privacidad.
4. Protección contra el delito.
5. Derecho a la legítima defensa.
6. Interoperabilidad global.
7. Estabilidad de la red.
8. Acceso confiable.
9. Gobernanza de múltiples partes interesadas.
10. Diligencia debida en ciberseguridad.

Para lograrlo combinaba tres herramientas: diplomacia, defensa y desarrollo. Los documentos de esa etapa coinciden en que una política nacional de ciberseguridad necesita liderazgo desde el más alto nivel del gobierno y compromisos internacionales, porque las amenazas son transnacionales.

## La estrategia cibernética nacional de 2018

En septiembre de 2018 la Casa Blanca publicó la *National Cyber Strategy of the United States of America*, organizada en cuatro pilares:

| Pilar | Contenido |
|---|---|
| 1. Proteger al pueblo estadounidense, el territorio y su forma de vida | Gobierno, industria y público aseguran las redes bajo su control y se apoyan entre sí |
| 2. Promover la prosperidad estadounidense | Preservar la influencia del país en el ecosistema tecnológico y el ciberespacio como motor de crecimiento e innovación |
| 3. Preservar la paz mediante la fuerza | Tratar el ciberespacio como parte integral del poder nacional y no como un tema aparte |
| 4. Ampliar la influencia estadounidense | Integrar las opciones cibernéticas a todos los instrumentos del poder nacional para identificar, contrarrestar, perturbar, degradar y disuadir conductas desestabilizadoras, preservando la superioridad del país |

Frente a la visión colaborativa de 2011, esta estrategia buscaba la primacía en el nuevo dominio y relegaba la cooperación internacional. El contraste muestra que la política cibernética de un país cambia con cada administración.

**Actualización al 2026:** después vinieron la Orden Ejecutiva 14028 (mayo de 2021), que modernizó la ciberseguridad federal y exigió seguridad en la cadena de suministro de software; la ley CIRCIA (marzo de 2022), que obliga a operadores de infraestructura crítica a reportar incidentes graves a la CISA y pagos de rescate; y la *National Cybersecurity Strategy* de marzo de 2023, con cinco pilares (defender la infraestructura crítica, desarticular a los actores de amenaza, usar las fuerzas del mercado para impulsar la seguridad, invertir en un futuro resiliente y forjar alianzas internacionales) y la idea de trasladar la responsabilidad hacia los fabricantes de software. En 2025 la Orden Ejecutiva 14306 modificó las órdenes 14144 (enero de 2025) y 13694, y la administración anunció una nueva estrategia nacional.

## Conclusiones del tema

- El mayor reto legal de Estados Unidos es la falta de un marco único: cada sector y cada estado tienen su pieza.
- Es necesario buscar uniformidad en la protección de los datos personales, que hoy depende del estado y del sector.
- Construir con otros países un ciberespacio abierto, interoperable, próspero y confiable sigue siendo un desafío para su política exterior.
- Estados Unidos promovió el modelo de múltiples partes interesadas en la gobernanza del ciberespacio.
- Su política osciló entre la construcción colaborativa (2011) y la búsqueda de primacía cibernética (2018), y siguió cambiando en 2023 y 2025.

## Glosario

| Término | Definición |
|---------|-----------|
| FTC | Comisión Federal de Comercio, que sanciona prácticas desleales o engañosas, incluida la mala seguridad de datos |
| Sección 5 de la Ley de la FTC | Disposición que prohíbe prácticas comerciales desleales o engañosas y sirve de base a las acciones de privacidad |
| FISMA | Ley que obliga a las agencias federales a tener programas de seguridad de la información; versión de 2002 y de 2014 |
| HIPAA | Ley de 1996 que protege la información de salud identificable |
| GLBA | Ley Gramm-Leach-Bliley de 1999, que protege la información financiera de clientes |
| Ley de aviso de brechas | Ley estatal que obliga a avisar a las personas cuando sus datos se exponen en un incidente |
| CCPA | Ley de Privacidad del Consumidor de California, vigente desde 2020 y reforzada por la CPRA |
| CISA 2015 | Ley de intercambio de información sobre amenazas entre empresas y gobierno, con protección de responsabilidad |
| CISA (agencia) | Agencia de Ciberseguridad y Seguridad de la Infraestructura, creada en 2018 en el Departamento de Seguridad Nacional |
| CIRCIA | Ley de 2022 que obliga a reportar incidentes graves y pagos de rescate de la infraestructura crítica |
| Indicador de amenaza | Dato técnico que ayuda a detectar un ataque, como una dirección, un dominio o una firma de malware |
| Múltiples partes interesadas | Modelo de gobernanza donde gobiernos, empresas, comunidad técnica, academia y sociedad civil deciden juntos |
| Orden ejecutiva | Instrucción del presidente a las agencias federales, con fuerza dentro de la administración |
| Diligencia debida | Obligación de tomar medidas razonables para no permitir que el propio territorio o sistema dañe a otros |

## Referencias

- Casa Blanca. (2011). *International strategy for cyberspace: Prosperity, security, and openness in a networked world*. https://obamawhitehouse.archives.gov/sites/default/files/rss_viewer/international_strategy_for_cyberspace.pdf
- Casa Blanca. (2018). *National cyber strategy of the United States of America*. https://trumpwhitehouse.archives.gov/wp-content/uploads/2018/09/National-Cyber-Strategy.pdf
- Casa Blanca. (2023). *National cybersecurity strategy*. https://bidenwhitehouse.archives.gov/wp-content/uploads/2023/03/National-Cybersecurity-Strategy-2023.pdf
- Congreso de los Estados Unidos. (2010). *Cybersecurity Act of 2010* (S. 773, 111.º Congreso). https://www.congress.gov/bill/111th-congress/senate-bill/773
- Senado de los Estados Unidos. (2010). *Cybersecurity Act of 2010* (S. Rept. 111-384). https://www.govinfo.gov/app/details/CRPT-111srpt384
- Congreso de los Estados Unidos. (2015). *Consolidated Appropriations Act, 2016* (Public Law 114-113, división N: Cybersecurity Act of 2015). https://www.govinfo.gov/app/details/PLAW-114publ113
- Cybersecurity and Infrastructure Security Agency. (2026). *Cybersecurity Information Sharing Act of 2015 procedures and guidance*. https://www.cisa.gov/resources-tools/resources/cybersecurity-information-sharing-act-2015-procedures-and-guidance
- Cybersecurity and Infrastructure Security Agency. (2026). *Cyber Incident Reporting for Critical Infrastructure Act of 2022 (CIRCIA)*. https://www.cisa.gov/topics/cyber-threats-and-advisories/information-sharing/cyber-incident-reporting-critical-infrastructure-act-2022-circia
- Cybersecurity and Infrastructure Security Agency. (2026). *Federal Information Security Modernization Act*. https://www.cisa.gov/topics/cyber-threats-and-advisories/federal-information-security-modernization-act
- Departamento de Seguridad Nacional. (2002). *Homeland Security Act of 2002*. https://www.dhs.gov/homeland-security-act-2002
- Federal Trade Commission. (2011). *Protecting personal information: A guide for business*. https://www.ftc.gov/business-guidance/resources/protecting-personal-information-guide-business
- Federal Trade Commission. (2015). *Start with security: A guide for business. Lessons learned from FTC cases*. https://www.ftc.gov/business-guidance/resources/start-security-guide-business
- National Initiative for Cybersecurity Careers and Studies. (2026). *Cybersecurity glossary*. https://niccs.cisa.gov/resources/glossary
- Oficina del Procurador General de California. (2026). *California Consumer Privacy Act (CCPA)*. https://oag.ca.gov/privacy/ccpa
- Presidente de los Estados Unidos. (2021). *Executive Order 14028: Improving the nation's cybersecurity*. Federal Register. https://www.federalregister.gov/documents/2021/05/17/2021-10460/improving-the-nations-cybersecurity
- Presidente de los Estados Unidos. (2025). *Executive Order 14306: Sustaining select efforts to strengthen the nation's cybersecurity and amending Executive Order 13694 and Executive Order 14144*. Federal Register. https://www.federalregister.gov/documents/2025/06/11/2025-10804/sustaining-select-efforts-to-strengthen-the-nations-cybersecurity-and-amending-executive-order-13694-and-executive-order-14144
- Schneier, B. (2018). *Click here to kill everybody: Security and survival in a hyper-connected world*. W. W. Norton. https://www.schneier.com/books/click-here/

---

*Rogelio Arriaga Gonzalez*
