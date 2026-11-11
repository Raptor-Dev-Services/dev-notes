# 13 - Introducción a la continuidad del negocio y la resiliencia

> Unidad 5, tema 1 de la materia *Seguridad de la información*. Qué es la continuidad del negocio, los tipos de planes de contingencia según el NIST, el enfoque de la agencia europea de ciberseguridad y cómo se comparan los dos procesos. Revisado a noviembre de 2026.

## Por qué importa

La continuidad del negocio es seguridad de la información en su dimensión de **disponibilidad**: que los procesos y la tecnología sigan funcionando, o vuelvan pronto, después de un evento grave. También define cómo se mantienen los demás controles cuando la empresa enfrenta un desastre, porque una recuperación apresurada en un sitio alterno mal protegido puede abrir más huecos que el propio incidente.

El estudio y la implantación de sistemas de gestión de continuidad crecieron a raíz de crisis que afectaron a miles de personas: eventos catastróficos que se consideraban "poco probables" pero de impacto altísimo, y ante los cuales casi nadie había hecho nada. Hoy varios organismos internacionales ofrecen marcos, definiciones y sistemas de gestión que permiten a quienes administran empresas y tecnología tomar decisiones que protegen los procesos de valor y mantienen la continuidad y la resiliencia.

## ¿Está tu empresa preparada para un desastre?

Toda persona que emprende quiere hacer crecer su negocio, proteger a su gente y aportar a la sociedad con sus productos y servicios. Eso exige proteger la empresa tanto en tiempos buenos como en situaciones adversas, incluso en la peor crisis. Ser competitivo en condiciones normales no basta: hay que ser **resiliente** para sobrevivir a un incendio, un sismo, un accidente, un ataque o cualquier incidente imprevisto.

La **continuidad del negocio** y la **recuperación de desastres** (BC/DR, por sus siglas en inglés) son prácticas muy relacionadas: describen cómo una organización se prepara frente a riesgos imprevistos para seguir operando. La tendencia a combinarlas en un solo concepto refleja algo que ya nadie discute: los responsables del negocio y los de tecnología deben trabajar juntos, no escribir planes por separado.

**Resiliencia** es la capacidad de recuperarse de un evento adverso y seguir con las actividades. Hoy es un factor crítico para cualquier empresa, expuesta a adversidades de todo tipo.

## Dos planes que se confunden

| | Plan de continuidad del negocio (BCP) | Plan de recuperación de desastres (DRP) |
|---|---|---|
| Qué es | Procedimientos y estrategias para reanudar a tiempo y en orden los **procesos del negocio**, con un impacto mínimo o nulo ante una contingencia | Procedimientos para reanudar a tiempo y en orden los **servicios informáticos críticos** después de una contingencia |
| Quién es responsable | Las unidades de negocio | Las áreas de tecnologías de la información |
| Pregunta que contesta | ¿Cómo seguimos atendiendo a los clientes? | ¿Cómo levantamos los sistemas que el negocio necesita? |

El DRP es una pieza del BCP: el negocio decide qué procesos son críticos y en cuánto tiempo deben volver; tecnología decide cómo lograrlo.

## Para qué sirve un plan de continuidad

Los objetivos que persigue un plan de continuidad bien hecho:

- proteger al personal y a los activos de la organización;
- asegurar la continuidad de las operaciones;
- reanudar los procesos críticos dentro de los márgenes de tiempo tolerables;
- reducir al mínimo las decisiones que hay que tomar en plena contingencia;
- reducir los efectos negativos del caos;
- mantener el servicio al cliente;
- responder a los inversionistas;
- cumplir los requisitos legales, contractuales y gubernamentales;
- depender lo menos posible de personas o grupos específicos;
- no tener que inventar procedimientos durante la contingencia;
- minimizar la posibilidad de perder información crítica;
- cumplir los requisitos de auditoría.

## La visión de Estados Unidos: NIST SP 800-34

La guía del NIST para la planeación de contingencias de sistemas federales (SP 800-34 Rev. 1, 2010) relaciona la continuidad con el resto de su marco de seguridad: con la SP 800-53, que establece los controles de seguridad (entre ellos la familia de planeación de contingencias), y con los estándares federales de categorización de la información, que clasifican cada sistema por el impacto de perder su confidencialidad, integridad o disponibilidad (bajo, moderado o alto). Esa categoría determina qué tan exigente debe ser su plan.

Para la guía, la planeación de contingencias de un sistema son las medidas provisionales para recuperar sus servicios tras una interrupción: trasladarlo a un sitio alterno, recuperarlo con equipo alterno u operar con métodos manuales mientras tanto.

### Los tipos de planes

Dentro de una organización conviven varios planes con enfoques distintos. Es importante conocer sus diferencias y cómo se relacionan:

| Plan | Propósito | Alcance |
|---|---|---|
| Continuidad del negocio (BCP) | Sostener los procesos de misión o de negocio durante y después de una interrupción | Procesos de negocio; la tecnología solo como soporte |
| Continuidad de operaciones (COOP) | Restablecer las funciones esenciales de la organización en un sitio alterno y sostenerlas hasta 30 días | Funciones esenciales en la sede o a nivel de toda la organización |
| Comunicación de crisis | Dar mensajes oportunos y coordinados al personal y al público | Comunicaciones internas y externas |
| Protección de infraestructura crítica | Proteger los componentes de la infraestructura crítica nacional | Activos y servicios críticos del sector |
| Respuesta a incidentes cibernéticos | Detectar, responder y limitar las consecuencias de un ataque | Sistemas y redes afectados |
| Recuperación de desastres (DRP) | Reubicar los sistemas en un sitio alterno después de una interrupción mayor | Sistemas de información que necesitan reubicarse |
| Contingencia del sistema de información (ISCP) | Recuperar un sistema específico, en su sitio o en uno alterno | Un solo sistema |
| Emergencia para ocupantes | Proteger la vida y la salud de las personas durante un evento físico, como un incendio o un sismo | Una instalación concreta |

Esta lectura deja clara la diferencia entre el plan de continuidad del negocio y el de recuperación de desastres, y cómo cada plan aporta valor desde su enfoque.

### El proceso de siete pasos

El NIST resume la planeación de contingencias en siete pasos fáciles de seguir, que dan cauce también al mantenimiento:

1. Escribir la **política** de planeación de contingencias.
2. Hacer el **análisis de impacto al negocio** (BIA).
3. Identificar los **controles preventivos**.
4. Crear las **estrategias de contingencia**.
5. Desarrollar el **plan de contingencia** del sistema.
6. Asegurar las **pruebas, la capacitación y los ejercicios** del plan.
7. Asegurar el **mantenimiento** del plan.

Los pasos 2 a 5 se desarrollan en la nota [14](14-plan-de-continuidad.md) y los pasos 6 y 7 en la [15](15-pruebas-y-mantenimiento-del-plan.md).

## La visión europea: ENISA

La **Agencia de la Unión Europea para la Ciberseguridad** (ENISA) busca un alto nivel común de ciberseguridad en Europa. En 2008 publicó *Business and IT Continuity: Overview and Implementation Principles*, un marco para gestionar la continuidad del negocio como un medio de mejorar la capacidad de enfrentar las amenazas a los sistemas de información y comunicaciones. Para construirlo, la agencia recopiló datos de muchas fuentes, estudió los marcos existentes y armó un modelo útil para cumplir normas internacionales.

Su enfoque está centrado en la continuidad de las **tecnologías de la información**, porque las TI están cada vez más integradas en los procesos de negocio y porque la legislación europea sobre seguridad de redes y sistemas de información lo exige. Los elementos cuya continuidad hay que gestionar son la infraestructura de TI, las aplicaciones críticas, los datos y las comunicaciones; garantizar su disponibilidad, integridad y seguridad evita interrupciones que dañen a la empresa.

### Riesgo y continuidad

ENISA distingue los mecanismos de la **gestión del riesgo** y los de la **gestión de la continuidad**, y los conecta: la continuidad puede verse como un resultado obligado de la gestión del riesgo. La gestión del riesgo identifica y reduce las amenazas probables; la continuidad se ocupa de lo que pasa cuando, a pesar de todo, ocurre un evento grave, y se concentra en los escenarios catastróficos sobre los activos y procesos críticos.

## Dos marcos, un mismo objetivo

Los dos marcos parten de modelos legislativos muy distintos. El europeo tiene un enfoque de **administración de riesgos**; el estadounidense está orientado a los **controles** de seguridad de la información. Pero ambos buscan lo mismo: mantener el control sobre los elementos críticos frente a amenazas catastróficas. Cuál se siga determina el enfoque del plan.

| NIST SP 800-34 | ENISA |
|---|---|
| Identificar estatutos y requisitos regulatorios | Iniciar el programa de gestión de la continuidad |
| Desarrollar la política de planeación de contingencias de TI | Conocer la organización |
| Publicar la política | Asignar responsabilidades de continuidad y de incidentes |
| | Definir la política de continuidad |
| Hacer el análisis de impacto al negocio | Hacer el análisis de impacto al negocio |
| Identificar los controles preventivos | Diseñar el enfoque de continuidad |
| Crear las estrategias de contingencia | |
| Desarrollar el plan de contingencia | Implantar el programa de continuidad |
| Planear pruebas, capacitación y ejercicios | Probar el programa |
| Mantener el plan | Mantener el programa |

Las etapas iniciales cambian; las intermedias se parecen mucho. Dentro de las organizaciones también hay visiones distintas de qué proteger frente a riesgos catastróficos, y por eso conocer los distintos tipos de planes es indispensable.

## Continuidad en las normas ISO

En ISO/IEC 27001:2013, el dominio **A.17** trataba los aspectos de seguridad de la información en la gestión de la continuidad: que la seguridad se mantenga durante una disrupción y que haya redundancia en las instalaciones de procesamiento. El sistema de gestión de continuidad completo es objeto de una norma propia, **ISO 22301**, un modelo de mejora continua que también es certificable.

> Actualización al 2026: ISO 22301 está vigente en su edición 2019 (la del material era la de 2012), con la enmienda 1:2024 sobre cambio climático; ISO ya prepara una tercera edición, que todavía es un proyecto. En ISO/IEC 27001:2022, el contenido de A.17 pasó a los controles 5.29 (seguridad de la información durante la disrupción), 5.30 (preparación de las TIC para la continuidad del negocio) y 8.14 (redundancia de las instalaciones de procesamiento). La guía del NIST SP 800-34 sigue en su revisión 1. En la Unión Europea, la Directiva NIS2 de 2022 exige a las entidades esenciales e importantes medidas de continuidad, como gestión de respaldos, recuperación de desastres y gestión de crisis.

## Para cerrar el tema

- El plan de continuidad es indispensable para organizar la recuperación de las funciones críticas ante un desastre.
- La gestión de la continuidad da un proceso integral para responder a incidentes e interrupciones.
- La resiliencia es un factor crítico ante las adversidades actuales.
- La operación depende de la tecnología, de modo que hace falta un proceso para recuperarla tras un desastre.
- El sistema de gestión de continuidad es un modelo de mejora continua certificable.

## Glosario

| Término | Definición |
|---|---|
| Continuidad del negocio | Capacidad de seguir entregando productos y servicios a un nivel aceptable durante una disrupción |
| Gestión de la continuidad | Proceso que da un marco para salvaguardar los objetivos y las obligaciones de la organización ante disrupciones |
| Resiliencia | Capacidad de recuperarse de un evento adverso y seguir operando |
| Desastre | Suceso grave que obliga a activar procedimientos de recuperación para evitar consecuencias inaceptables |
| Evento | Ocurrencia o cambio de un conjunto de circunstancias |
| BCP | Plan de continuidad del negocio: procedimientos para reanudar los procesos del negocio |
| DRP | Plan de recuperación de desastres: procedimientos para reanudar los servicios informáticos críticos |
| COOP | Plan de continuidad de operaciones: funciones esenciales en un sitio alterno hasta 30 días |
| ISCP | Plan de contingencia de un sistema de información específico |
| Plan de emergencia para ocupantes | Plan para proteger la vida de las personas en una instalación durante un evento físico |
| BC/DR | Continuidad del negocio y recuperación de desastres tratadas como un solo programa |
| ISO 22301 | Norma internacional certificable de sistemas de gestión de continuidad del negocio |

## Referencias

- Agencia de la Unión Europea para la Seguridad de las Redes y de la Información. (2008). *Business and IT continuity: Overview and implementation principles*. ENISA. https://www.enisa.europa.eu/publications/business-and-it-continuity-overview-and-implementation-principles
- INCIBE. (s. f.). *Plan de contingencia y continuidad de negocio*. Instituto Nacional de Ciberseguridad. https://www.incibe.es/sites/default/files/contenidos/dosieres/metad_plan_de_contingencia_y_continuidad_de_negocio.pdf
- International Organization for Standardization. (2019). *ISO 22301:2019 Security and resilience. Business continuity management systems. Requirements*. ISO. https://www.iso.org/standard/75106.html
- International Organization for Standardization. (2024). *ISO 22301:2019/Amd 1:2024 Security and resilience. Business continuity management systems. Requirements. Amendment 1: Climate action changes*. ISO. https://www.iso.org/standard/88412.html
- International Organization for Standardization e International Electrotechnical Commission. (2022). *ISO/IEC 27001:2022 Information security, cybersecurity and privacy protection. Information security management systems. Requirements*. ISO. https://www.iso.org/standard/27001
- ISOTools Excellence. (2014, 19 de noviembre). *Webinar ISOTools: ISO 22301:2012, gestión de continuidad de negocio* [Video]. YouTube. https://youtu.be/kD3Momd3_TA
- Parlamento Europeo y Consejo de la Unión Europea. (2022). Directiva (UE) 2022/2555 relativa a las medidas destinadas a garantizar un elevado nivel común de ciberseguridad en toda la Unión. *Diario Oficial de la Unión Europea*, L 333. https://eur-lex.europa.eu/eli/dir/2022/2555/oj
- Swanson, M., Bowen, P., Phillips, A. W., Gallup, D., y Lynes, D. (2010). *Contingency planning guide for federal information systems* (NIST Special Publication 800-34 Rev. 1). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/34/r1/upd1/final

---

*Rogelio Arriaga Gonzalez*
