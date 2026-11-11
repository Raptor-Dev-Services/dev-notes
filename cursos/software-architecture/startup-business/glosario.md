# Glosario Técnico  -  Startup Administrativo

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### Acuerdo de no competencia
**Categoría:** Legal / Contratos
**Definición:** Cláusula contractual que impide a un empleado o contratista trabajar para la competencia directa durante un período definido tras terminar la relación laboral. En México es aplicable únicamente si incluye límite temporal (máximo 1-2 años), ámbito geográfico acotado y compensación económica explícita; sin compensación es prácticamente inaplicable en juicio.

### ADR (Architecture Decision Record)
**Categoría:** Documentación / Arquitectura
**Definición:** Documento breve y versionado que registra una decisión de diseño o arquitectura significativa: el contexto del problema, las alternativas consideradas, la decisión tomada y sus consecuencias. Se almacena directamente en el repositorio (carpeta `docs/adr/`) y forma parte de la historia del proyecto.

### AGENTS.md
**Categoría:** AI Workflow / Documentación
**Definición:** Archivo de configuración que define el contexto, las convenciones y las restricciones del proyecto para que agentes de IA como Claude Code operen con autonomía controlada. Equivalente a un manual de onboarding para el agente: qué puede tocar, qué no, cómo se llaman las cosas en este codebase.

### Ángel (Angel Investor)
**Categoría:** Finanzas / Financiamiento
**Definición:** Persona física que invierte capital propio en startups en etapa temprana a cambio de participación accionaria. Aporta generalmente entre $50k y $500k USD, acepta mayor riesgo que un VC y suele además ofrecer mentoría y red de contactos. Un proceso de cierre con ángeles toma típicamente 3-6 meses.

### ARR (Annual Recurring Revenue)
**Categoría:** Finanzas / Métricas SaaS
**Definición:** Ingreso recurrente anualizado de una empresa SaaS. Se calcula multiplicando el MRR por 12. Es el indicador que usan los inversores para valorar empresas de software por suscripción porque proyecta los ingresos predecibles a un año. No incluye ingresos por servicios, implementación o pagos únicos.

---

## B

### Backlog
**Categoría:** Gestión de Producto / Agilidad
**Definición:** Lista priorizada de todo el trabajo pendiente de un producto o equipo: features, bugs, mejoras técnicas y deuda técnica. Un backlog saludable tiene cada ítem con título, criterio de aceptación, responsable y estimado. Se refina cada dos semanas, eliminando ítems que no se priorizaron en tres ciclos consecutivos.

### B2B (Business to Business)
**Categoría:** Modelo de Negocio
**Definición:** Modelo comercial en el que el cliente final es otra empresa, no un consumidor individual. Los ciclos de venta son más largos, los tickets más altos y el proceso de adopción involucra múltiples tomadores de decisión (compras, legal, TI). El Free Trial de 14-30 días funciona mejor que Freemium en contextos B2B.

### Blameless Postmortem
**Categoría:** Operaciones / Cultura de Equipo
**Definición:** Proceso estructurado de análisis de un incidente que busca identificar causas raíz sistémicas sin asignar culpa a personas. Su premisa es que los sistemas fallan y las personas actúan dentro de sistemas mal diseñados. Incluye: timeline objetivo, análisis con "5 Whys", impacto cuantificado, acciones correctivas con owner y fecha, y lecciones aprendidas. Debe publicarse en menos de 48 horas del incidente.

### Board (Consejo de Administración)
**Categoría:** Estructura Societaria / Gobierno Corporativo
**Definición:** Órgano de gobierno de una sociedad que supervisa la gestión ejecutiva, aprueba decisiones estratégicas y representa los intereses de los accionistas. En la SAPI de CV el board es obligatorio y tiene atribuciones más amplias que la simple asamblea de accionistas de una SA de CV. Incluye representantes de fundadores, inversores y eventualmente directores independientes.

### Bootstrapped
**Categoría:** Finanzas / Modelo de Crecimiento
**Definición:** Modelo de crecimiento en el que la empresa se financia exclusivamente con los ingresos propios y el capital inicial de los fundadores, sin inversión externa. Implica crecimiento más lento pero cero dilución accionaria. Es la fase inicial recomendada para validar el modelo de negocio antes de buscar capital externo.

### Brag Document
**Categoría:** Gestión de Personas / Performance
**Definición:** Documento personal y continuo donde cada miembro del equipo registra sus logros, decisiones tomadas y contribuciones en tiempo real a lo largo del ciclo de evaluación. Sirve para combatir el sesgo de recencia en las performance reviews, ya que el evaluador tiende a recordar solo el último mes y no los seis meses completos del ciclo.

### Burn Rate
**Categoría:** Finanzas / Métricas de Salud
**Definición:** Velocidad a la que una empresa consume su capital mensualmente. Se calcula como la diferencia entre los egresos totales del mes y los ingresos del mes. Un burn rate positivo significa que la empresa aún no es rentable y está usando su reserva de caja. Es el denominador del cálculo de runway.

---

## C

### CAC (Customer Acquisition Cost)
**Categoría:** Finanzas / Métricas SaaS
**Definición:** Costo total invertido en adquirir un nuevo cliente, que incluye gastos de marketing, ventas, demostraciones, herramientas comerciales y comisiones, dividido entre el número de nuevos clientes obtenidos en el período. Es la contraparte del LTV: si CAC > LTV, el negocio pierde dinero por cada cliente que cierra.

### Cap Table (Tabla de Capitalización)
**Categoría:** Finanzas / Estructura Societaria
**Definición:** Registro formal de la distribución de la propiedad accionaria de una empresa: quién tiene qué porcentaje, en qué clase de acciones y con qué derechos asociados. Es el documento que más cambia con cada ronda de inversión por efecto de la dilución. Se mantiene en una hoja actualizada y firmada por todos los accionistas.

### Causa Raíz (Root Cause)
**Categoría:** Operaciones / Postmortem
**Definición:** El fallo sistémico original que desencadenó un incidente, identificado mediante técnicas como el "5 Whys" (preguntar "¿por qué?" cinco veces consecutivas hasta llegar al origen real). Distinguir la causa raíz del síntoma visible es el objetivo central de un blameless postmortem.

### Churn Rate
**Categoría:** Finanzas / Métricas SaaS
**Definición:** Porcentaje de clientes que cancelan su suscripción en un período determinado, típicamente mensual. Se calcula dividiendo los clientes perdidos en el mes entre los clientes al inicio del mes. Un churn mayor al 5 % mensual indica un problema estructural en el producto o en la propuesta de valor. El benchmark saludable para SaaS es menor al 2 % mensual.

### CLAUDE.md
**Categoría:** AI Workflow / Documentación
**Definición:** Archivo de configuración en la raíz de un repositorio que provee contexto del proyecto a Claude Code: stack, convenciones de código, entidades del dominio, restricciones, rutas clave y principios de diseño. Permite que el agente de IA opere con autonomía alineada a las decisiones del equipo sin repetir el mismo contexto en cada conversación.

### Code Review
**Categoría:** Ingeniería de Software / Gestión de Equipo
**Definición:** Proceso formal en el que uno o más desarrolladores revisan el código de un compañero antes de que sea integrado a la rama principal. Detecta bugs, problemas de diseño, violaciones de convenciones y oportunidades de mejora. También es el canal principal de transferencia de conocimiento y calibración de estándares de calidad.

### Core Competency
**Categoría:** Estrategia / Escalamiento
**Definición:** Capacidad o conocimiento fundamental que diferencia a una empresa de sus competidores y que no debe externalizarse porque es fuente directa de valor para el cliente. Todo lo que no es core competency puede y debe considerarse para externalización cuando exista una solución de mercado probada.

### Criterio de Aceptación
**Categoría:** Gestión de Producto / Agilidad
**Definición:** Condición verificable y objetiva que determina cuándo un ítem del backlog está completo y cumple con lo acordado. Sin criterios de aceptación, el "terminado" queda abierto a interpretación y genera fricciones en el code review y en la entrega al cliente. Son el contrato entre quien define el ítem y quien lo implementa.

### Culture Fit
**Categoría:** Gestión de Personas / Contratación
**Definición:** Evaluación cualitativa de si un candidato comparte los valores, la forma de trabajo y la actitud de colaboración del equipo. No implica que todos sean iguales, sino que el candidato pueda trabajar bien dentro del contexto específico: cómo reacciona ante feedback, si documenta sin que se lo pidan, si pregunta antes de asumir.

---

## D

### Deuda Organizacional
**Categoría:** Gestión / Operaciones
**Definición:** Acumulación de procesos no documentados, decisiones no registradas, roles ambiguos y estructuras informales que frenan el crecimiento de una empresa. Es el equivalente organizacional de la deuda técnica: cada semana sin documentar un proceso, sin definir un SOP o sin estructurar un onboarding aumenta el costo futuro de hacerlo.

### Deuda Técnica (Technical Debt)
**Categoría:** Ingeniería de Software / Gestión
**Definición:** Costo adicional de trabajo futuro generado por decisiones de implementación que priorizaron la velocidad sobre la calidad: código sin tests, arquitectura improvisada, dependencias no actualizadas, falta de documentación. Como la deuda financiera, acumula interés: se vuelve más cara de pagar con el tiempo.

### Dilución (Equity Dilution)
**Categoría:** Finanzas / Estructura Societaria
**Definición:** Reducción del porcentaje de propiedad accionaria de los fundadores existentes cuando se emiten nuevas acciones para incorporar a inversores. Cada ronda de financiamiento diluye a los accionistas anteriores. Un ángel puede implicar 5-15 % de dilución; una ronda VC puede implicar 20-30 % adicional.

### Drag-Along
**Categoría:** Legal / Estructura Societaria
**Definición:** Cláusula accionaria que obliga a los socios minoritarios a unirse a la venta de la empresa si los socios mayoritarios deciden venderla, en los mismos términos. Protege al comprador de que los minoritarios bloqueen la transacción. Es un derecho negociable dentro de la SAPI de CV pero no existe en la SA de CV estándar.

---

## E

### eNPS (Employee Net Promoter Score)
**Categoría:** Gestión de Personas / Indicadores de Equipo
**Definición:** Métrica de satisfacción laboral que pregunta a los empleados: "¿Con qué probabilidad (0-10) recomendarías esta empresa como lugar de trabajo?" Los que responden 9-10 son promotores, los de 7-8 son neutrales y los de 0-6 son detractores. El score es el porcentaje de promotores menos el de detractores. Un eNPS mayor a 30 es saludable para equipos técnicos.

### Entrevista Técnica Live
**Categoría:** Gestión de Personas / Contratación
**Definición:** Sesión de evaluación técnica en tiempo real donde el candidato revisa y discute su propio código (la prueba técnica enviada previamente) con el entrevistador. El objetivo no es resolver un problema nuevo bajo presión, sino evaluar cómo el candidato razona sobre decisiones de diseño, cómo recibe feedback y qué cambiaría con más tiempo.

### Escalamiento (Scaling)
**Categoría:** Estrategia / Operaciones
**Definición:** Proceso de hacer crecer la empresa  -  en clientes, equipo, infraestructura o mercados  -  de forma sostenida y sin perder calidad ni control. Requiere que los procesos, sistemas y estructura organizacional estén en su lugar antes de acelerar. Escalar sin estructura solo amplifica el caos existente.

### Externalización (Outsourcing)
**Categoría:** Operaciones / Escalamiento
**Definición:** Decisión estratégica de contratar a un proveedor externo para ejecutar funciones que no son core competency de la empresa. Se justifica cuando existe una solución de mercado probada, la función es no-diferenciadora y el costo de hacerlo internamente supera el costo del proveedor más la coordinación requerida.

---

## F

### Feature Flag
**Categoría:** Ingeniería de Software / Gestión de Producto
**Definición:** Mecanismo que permite activar o desactivar una funcionalidad del producto en tiempo de ejecución, sin necesidad de un nuevo despliegue. Usado para lanzamientos graduales, pruebas A/B, acceso por plan de suscripción o desactivación de emergencia de una feature problemática.

### Finiquito
**Categoría:** Legal / Gestión de Personas
**Definición:** Documento legal que formaliza el cierre de la relación laboral entre empresa y empleado, detallando los pagos liquidados (partes proporcionales de aguinaldo, vacaciones, prima vacacional) y liberando a ambas partes de obligaciones futuras derivadas del contrato de trabajo. En México es obligatorio por ley y debe firmarse al terminar cualquier relación laboral.

### Free Trial
**Categoría:** Modelo de Negocio / Pricing
**Definición:** Modalidad de acceso al producto donde el usuario tiene acceso completo a todas las funcionalidades durante un período limitado (generalmente 14-30 días) sin pagar. Al finalizar el período debe convertirse a cliente de pago. Tiene mejor tasa de conversión que el Freemium en contextos B2B porque el usuario experimenta el valor completo del producto.

### Frecuencia de Deploys
**Categoría:** Operaciones / Indicadores de Equipo
**Definición:** Número de veces que el equipo despliega código a producción (o staging) en un período determinado, típicamente por semana. Es uno de los cuatro indicadores DORA de rendimiento de entrega de software. Una frecuencia alta indica un proceso de CI/CD maduro y confianza del equipo en sus tests y procedimientos de rollback.

### Freemium
**Categoría:** Modelo de Negocio / Pricing
**Definición:** Modelo de negocio donde existe un plan gratuito permanente con funcionalidades limitadas y planes de pago con acceso completo. Atrae volumen de usuarios pero tiende a diluir las ventas porque muchos se conforman con el plan gratuito. Es efectivo cuando hay un fuerte efecto de red (el producto vale más mientras más usuarios lo usan).

---

## G

### GitHub Projects
**Categoría:** Gestión de Producto / Herramientas
**Definición:** Herramienta de gestión de trabajo integrada directamente en GitHub que conecta issues y pull requests con tableros de proyecto (kanban o table view). Ideal cuando el 90 % del trabajo del equipo vive en el código y se quiere evitar duplicar el seguimiento en herramientas externas.

---

## I

### Incidente P1 / P2
**Categoría:** Operaciones / Gestión de Incidentes
**Definición:** Clasificación de severidad de incidentes en producción. P1 (Prioridad 1) es el nivel más crítico: el sistema está completamente caído o hay pérdida de datos; requiere atención inmediata sin importar el horario. P2 es crítico pero con workaround disponible: funcionalidad principal degradada. El número de incidentes P1/P2 por mes es un KPI de estabilidad del sistema.

---

## K

### Kanban
**Categoría:** Gestión de Producto / Agilidad
**Definición:** Sistema de gestión del flujo de trabajo visual donde las tareas se mueven entre columnas (To Do, In Progress, Review, Done) sin ciclos de tiempo fijos. A diferencia de Scrum, no hay sprints: el trabajo fluye de forma continua. Es recomendado para equipos pequeños (menos de 5 personas) donde los sprints formales de dos semanas agregan overhead sin valor proporcional.

### Key Result (KR)
**Categoría:** Estrategia / OKRs
**Definición:** Resultado medible y verificable que indica si se está avanzando hacia un Objective cualitativo. Debe ser un resultado de negocio o técnico concreto, no una actividad o tarea. Si siempre se logra el 100 % de los KRs al final del ciclo, están mal calibrados: el rango saludable de cumplimiento es 60-70 %.

### KPI (Key Performance Indicator)
**Categoría:** Estrategia / Métricas
**Definición:** Indicador cuantificable de desempeño operativo continuo. A diferencia de los OKRs que son temporales y orientados al cambio, los KPIs son permanentes y miden la salud del sistema: uptime, tiempo de respuesta, churn mensual, frecuencia de deploys. Se monitorean en dashboards en tiempo real y generan alertas cuando se salen del rango esperado.

---

## L

### Latencia p95 / p99
**Categoría:** Ingeniería de Software / Observabilidad
**Definición:** Percentil 95 o 99 del tiempo de respuesta de un sistema. El p95 significa que el 95 % de las peticiones se resuelven en ese tiempo o menos; el 5 % restante tarda más. Es una métrica más representativa que el promedio porque captura los casos lentos que afectan a usuarios reales. Se usa como KR en OKRs técnicos y como SLA en contratos enterprise.

### Linear
**Categoría:** Gestión de Producto / Herramientas
**Definición:** Herramienta de gestión de proyectos diseñada para equipos de ingeniería: soporte nativo para ciclos de trabajo, prioridades, integración con GitHub (vincula PRs a issues automáticamente) y una interfaz de alta velocidad pensada para uso intensivo por desarrolladores. Recomendada cuando el equipo técnico es maduro y tiene sprints regulares.

### LTV (Lifetime Value)
**Categoría:** Finanzas / Métricas SaaS
**Definición:** Valor económico total que un cliente genera durante toda su relación con la empresa. Se calcula dividiendo el MRR por cliente entre el churn rate mensual. Indica cuánto vale en promedio adquirir y retener a un cliente. El ratio LTV/CAC es el indicador más importante de viabilidad del modelo de negocio SaaS.

---

## M

### Matriz de Habilidades (Skills Matrix)
**Categoría:** Gestión de Personas / Planificación
**Definición:** Tabla que cruza miembros del equipo con habilidades clave y asigna a cada celda un nivel de dominio (1 = aprende con guía, 2 = ejecuta de forma independiente, 3 = puede enseñar y onboardear a otro). Permite identificar single points of failure: habilidades críticas donde solo una persona tiene nivel suficiente. Se actualiza cada tres meses.

### MRR (Monthly Recurring Revenue)
**Categoría:** Finanzas / Métricas SaaS
**Definición:** Ingreso recurrente mensual de una empresa SaaS, calculado como la suma del precio mensual multiplicado por el número de clientes activos. Es la métrica central de salud financiera de un negocio por suscripción. Debe monitorearse mensualmente y desglosarse en MRR nuevo, de expansión, de contracción y de cancelación.

---

## N

### NDA (Non-Disclosure Agreement)
**Categoría:** Legal / Contratos
**Definición:** Acuerdo de confidencialidad que obliga a las partes firmantes a no divulgar información privada o propietaria compartida durante una relación comercial o proceso de evaluación. Debe firmarse antes de cualquier entrevista técnica, demo de producto o negociación donde se comparta arquitectura, roadmap o datos de clientes.

### Notion
**Categoría:** Gestión de Producto / Herramientas
**Definición:** Herramienta de productividad que combina wikis, bases de datos, tareas y documentos en un solo espacio. Ofrece flexibilidad total pero puede volverse desordenada sin convenciones claras. Es la mejor opción para equipos en etapa inicial (menos de 5 personas) con mucha documentación y pocos procesos formalizados.

### Now / Next / Later
**Categoría:** Gestión de Producto / Roadmap
**Definición:** Framework de roadmap que organiza el trabajo en tres horizontes sin fechas fijas: Now (en lo que el equipo trabaja actualmente), Next (lo que viene después) y Later (ideas futuras sin compromiso de fecha). Evita promesas imposibles y permite ajustar prioridades sin romper la confianza con clientes o inversores.

---

## O

### Objective (OKR)
**Categoría:** Estrategia / OKRs
**Definición:** Parte cualitativa e inspiradora de un OKR. Define qué se quiere lograr en el ciclo, no cómo. Debe motivar al equipo y no ser una tarea técnica disfrazada de objetivo. Se complementa con 3 Key Results medibles que indican si el Objective se está alcanzando.

### Offboarding
**Categoría:** Gestión de Personas / Seguridad
**Definición:** Proceso formal de salida de un colaborador de la empresa que incluye la revocación inmediata de todos los accesos (código, infraestructura, herramientas), la transferencia del conocimiento que el colaborador tenía, y el cierre legal del contrato. Es el momento de mayor riesgo de seguridad para la empresa si no se ejecuta correctamente y el mismo día.

### OKR (Objectives and Key Results)
**Categoría:** Estrategia / Gestión
**Definición:** Framework de gestión de objetivos compuesto por un Objective cualitativo e inspirador y tres Key Results medibles que indican si se está avanzando. Usado por Google, Intel y miles de startups. Los ciclos son trimestrales o semestrales. Un cumplimiento de 60-70 % al final del ciclo se considera éxito; llegar al 100 % siempre indica que los OKRs estaban mal calibrados.

### Onboarding
**Categoría:** Gestión de Personas / Operaciones
**Definición:** Proceso estructurado de integración de un nuevo colaborador al equipo y a la empresa. Cubre desde el acceso a herramientas el Día 1 hasta la entrega autónoma de un feature en el Mes 1. Un onboarding efectivo se mide por si el nuevo integrante hace su primer PR en menos de 48 horas hábiles. Sin documentación, el onboarding se improvisa y tarda semanas.

---

## P

### Performance Review
**Categoría:** Gestión de Personas / Evaluación
**Definición:** Evaluación formal y periódica del desempeño de un colaborador. En startups el ciclo recomendado es semestral (no anual, que es demasiado largo para el ritmo de una startup). Debe evaluar impacto real, no actividad. El resultado debe ser una acción concreta: promoción, ajuste salarial, plan de mejora con fecha o separación conversada.

### Per-Seat (Pricing)
**Categoría:** Modelo de Negocio / Pricing
**Definición:** Modelo de cobro por usuario activo en el sistema. El cliente paga una tarifa fija por cada "asiento" (usuario con acceso). Es el modelo más intuitivo para productos de colaboración de equipo donde el valor percibido está directamente relacionado con el número de personas que lo usan.

### Pivot
**Categoría:** Estrategia / Modelo de Negocio
**Definición:** Cambio significativo en el modelo de negocio, el segmento de mercado, el producto o el canal de distribución de una startup, motivado por evidencia de que el enfoque actual no está funcionando. Un pivot no es rendirse: es reconocer una señal de mercado y redirigir los esfuerzos hacia una oportunidad más viable con el capital restante.

### Planning (Ceremonias Ágiles)
**Categoría:** Gestión de Producto / Agilidad
**Definición:** Ceremonia ágil de planificación donde el equipo revisa los OKRs del ciclo, prioriza el backlog del período y establece compromisos realistas de entrega. En equipos pequeños se recomienda hacerla mensualmente en sesiones de 1-2 horas, enfocándose en compromisos alcanzables en lugar de deseos optimistas sin base en la velocidad histórica.

### Pool de Opciones (ESOP)
**Categoría:** Finanzas / Estructura Societaria
**Definición:** Porcentaje del cap table reservado para otorgar opciones sobre acciones a empleados clave como parte de su compensación. Alinea los intereses del equipo con el crecimiento de la empresa. Típicamente representa el 10-15 % del cap table en startups en etapa temprana y se diluye en cada ronda junto con los fundadores.

### Postmortem
**Categoría:** Operaciones / Gestión de Incidentes
**Definición:** Análisis formal de un incidente ocurrido en producción que documenta qué pasó, cuándo, con qué impacto y qué acciones correctivas se toman. Cuando es "blameless" el foco está en el sistema, no en las personas. Debe publicarse en menos de 48 horas del incidente para capturar los detalles mientras todos los involucrados los recuerdan frescos.

### Pricing Strategy
**Categoría:** Modelo de Negocio / Finanzas
**Definición:** Conjunto de decisiones sobre cómo monetizar un producto: modelo de cobro (per-seat, por uso, flat + tiers), niveles de precio y estrategia de entrada al mercado. En ausencia de datos históricos se recomienda value-based pricing: estimar cuánto ahorra o gana el cliente y cobrar entre el 10 % y el 20 % de ese valor, validado con 5 entrevistas de cliente.

### Propiedad Intelectual (PI)
**Categoría:** Legal / Contratos
**Definición:** Conjunto de derechos legales que protegen las creaciones del intelecto: código fuente, diseños, algoritmos, documentación y marcas. La cláusula de PI en el contrato de trabajo establece que todo lo producido por el empleado relacionado con el giro de la empresa es propiedad exclusiva del empleador, incluso si fue creado fuera del horario laboral pactado.

### Prueba Técnica en Casa
**Categoría:** Gestión de Personas / Contratación
**Definición:** Evaluación técnica de contratación donde el candidato resuelve un problema acotado (una API REST o un componente React) en su propio entorno, con 48 horas de plazo. Se evalúa la estructura del código, el README, las decisiones de diseño y el historial de commits, no la velocidad de resolución bajo presión artificial en una plataforma de coding challenges.

---

## R

### README.md
**Categoría:** Documentación / Ingeniería de Software
**Definición:** Archivo de documentación en la raíz de un repositorio que explica qué es el proyecto, cómo configurar el ambiente de desarrollo, cómo correr los tests y cualquier información esencial para un nuevo colaborador. Es el primer documento que lee un nuevo dev y la pieza central del checklist de onboarding del Día 1.

### Retrospectiva
**Categoría:** Gestión de Producto / Agilidad
**Definición:** Ceremonia ágil quincenal donde el equipo reflexiona sobre su proceso: qué salió bien, qué mejorar y qué experimentar en el siguiente ciclo. Debe generar máximo dos acciones concretas con dueño y fecha de cierre. Es el mecanismo principal de mejora continua del equipo más allá del progreso técnico.

### RNIE (Registro Nacional de Inversiones Extranjeras)
**Categoría:** Legal / Estructura Societaria
**Definición:** Registro obligatorio en México para empresas que reciben inversión extranjera directa operando como SA de CV. La SAPI de CV elimina esta restricción, permitiendo que inversores extranjeros participen directamente en el capital sin trámites adicionales ante esta dependencia gubernamental.

### ROFR (Right of First Refusal)
**Categoría:** Legal / Estructura Societaria
**Definición:** Derecho de preferencia que permite a un accionista existente igualar cualquier oferta de compra de acciones de otro accionista antes de que pueda venderlas a un tercero. Protege a los inversores de que los fundadores vendan su participación a competidores o a personas no alineadas con la visión del producto.

### Roadmap de Producto
**Categoría:** Gestión de Producto / Estrategia
**Definición:** Plan visual de alto nivel que comunica la dirección del producto en el tiempo: qué se está construyendo ahora, qué viene después y qué es una idea futura. Existen dos formatos: Now/Next/Later (sin fechas, para comunicación pública) y timeline con fechas (para uso interno). Una fecha pública incumplida destruye la confianza del cliente más que no haber dado una fecha.

### Runbook
**Categoría:** Operaciones / Gestión de Incidentes
**Definición:** Documento operativo que describe paso a paso qué hacer cuando algo falla en producción. Incluye: síntomas del incidente, pasos de diagnóstico, acciones de mitigación, criterios de escalación y proceso de post-incidente. A diferencia del SOP (proceso planificado), el runbook es para situaciones de emergencia activa donde cada minuto cuenta.

### Runway
**Categoría:** Finanzas / Métricas de Salud
**Definición:** Tiempo que le queda a una empresa antes de quedarse sin dinero, dado su burn rate actual. Se calcula dividiendo el saldo disponible en caja entre el burn rate mensual. Cuando el runway cae a menos de 6 meses la empresa debe actuar: reducir burn, acelerar ingresos o iniciar conversaciones con inversores. Nunca debe llegar a 3 meses sin un plan concreto ejecutándose.

---

## S

### SA de CV (Sociedad Anónima de Capital Variable)
**Categoría:** Legal / Estructura Societaria
**Definición:** La forma societaria más común en México para empresas de capital privado. Tiene estructura accionaria simple (todas las acciones son iguales, sin series diferenciadas), inversión extranjera con restricciones vía RNIE y costo de constitución relativamente bajo ($15,000-$25,000 MXN con notario). Es el punto de partida adecuado para startups bootstrapped sin planes inmediatos de levantar capital.

### SAPI de CV (Sociedad Anónima Promotora de Inversión de Capital Variable)
**Categoría:** Legal / Estructura Societaria
**Definición:** Variante societaria en México diseñada para startups que buscan capital de riesgo. Permite acciones de distintas series (A, B, preferentes), inversión extranjera directa sin permisos RNIE, y cláusulas protectoras negociables (drag-along, tag-along, ROFR, antidilución). Su constitución cuesta entre $40,000 y $80,000 MXN con notario y requiere un consejo de administración formal.

### Screening de CV
**Categoría:** Gestión de Personas / Contratación
**Definición:** Primera etapa del proceso de contratación donde se filtra el pool de candidatos revisando el currículum y el portafolio público (GitHub). Los criterios principales son: proyectos reales con código demostrable, stack compatible con el 70 % de las necesidades del equipo, y evidencia de trabajo real (no solo credenciales académicas). Duración máxima: 30 minutos por candidato.

### Sesgo de Recencia (Recency Bias)
**Categoría:** Gestión de Personas / Performance
**Definición:** Tendencia cognitiva del evaluador a recordar y dar más peso a los eventos más recientes al hacer una performance review, ignorando los logros o problemas de los primeros meses del ciclo. Se combate manteniendo un brag document continuo y revisando los seis meses completos de PRs antes de la sesión de evaluación.

### Single Point of Failure (SPOF)
**Categoría:** Ingeniería de Software / Gestión de Equipo
**Definición:** Componente único (técnico o humano) cuya falla detiene el sistema completo. En el contexto del equipo, es una habilidad crítica que solo una persona domina: si esa persona se va, el equipo queda bloqueado. Se identifica en la matriz de habilidades y se mitiga con capacitación cruzada antes de que el riesgo se materialice.

### SOP (Standard Operating Procedure)
**Categoría:** Operaciones / Documentación
**Definición:** Documento que describe cómo ejecutar un proceso repetible de forma consistente, independientemente de quién lo ejecute. El formato mínimo incluye: título, responsable, prerrequisitos, pasos numerados, verificación y links a recursos relacionados. A diferencia del runbook, el SOP es para procesos planificados y predecibles.

### Sprint
**Categoría:** Gestión de Producto / Agilidad
**Definición:** Período de tiempo fijo (típicamente 1-2 semanas) durante el cual el equipo trabaja en un conjunto comprometido de ítems del backlog. Al finalizar debe haber un incremento de producto potencialmente entregable. Para equipos muy pequeños, los sprints formales de dos semanas pueden sustituirse por kanban continuo o ciclos de una semana.

### Standup
**Categoría:** Gestión de Producto / Agilidad
**Definición:** Reunión diaria de sincronización del equipo de máximo 15 minutos. Cada persona responde tres preguntas: qué hice ayer, qué haré hoy y qué me bloquea. No es un status meeting para el líder: es sincronización entre pares para detectar bloqueos y dependencias cruzadas lo antes posible, antes de que consuman un día completo.

### Story Points
**Categoría:** Gestión de Producto / Agilidad
**Definición:** Unidad abstracta de estimación de esfuerzo relativo en metodologías ágiles como Scrum. Representan la complejidad, riesgo e incertidumbre de un ítem del backlog, no las horas de trabajo. Para equipos pequeños, las tallas de playera (S/M/L/XL) son más prácticas y rápidas de asignar que los story points con escala Fibonacci.

---

## T

### Tag-Along
**Categoría:** Legal / Estructura Societaria
**Definición:** Cláusula accionaria que protege a los socios minoritarios: si los mayoritarios venden su participación, los minoritarios tienen el derecho de unirse a la venta en los mismos términos y precio. Es el complemento del drag-along y está disponible en la SAPI de CV pero no en la SA de CV estándar.

### Tallas de Playera (T-shirt Sizing)
**Categoría:** Gestión de Producto / Estimación
**Definición:** Sistema simplificado de estimación de esfuerzo relativo usando tallas de ropa (S, M, L, XL) en lugar de story points numéricos. S es un cambio pequeño (horas), M es una tarea de un día, L toma varios días, XL debería subdividirse antes de comprometerse. Reduce el tiempo de planning sin sacrificar la señal de complejidad.

### Timeline (Roadmap con Fechas)
**Categoría:** Gestión de Producto / Comunicación
**Definición:** Formato de roadmap con fechas comprometidas públicamente para cada feature o versión del producto. Ofrece claridad comercial a clientes e inversores pero una demora destruye la confianza establecida. Se recomienda usar solo de forma interna, presentando Now/Next/Later públicamente hasta que la feature esté en staging y probada.
Públicamente, la empresa no debe comprometer fechas de lanzamiento del bot flow builder ni de las campañas masivas hasta que estén en staging y hayan pasado pruebas de carga.

---

## U

### Uptime
**Categoría:** Ingeniería de Software / Observabilidad
**Definición:** Porcentaje de tiempo en que un sistema está disponible y funcionando correctamente. El benchmark estándar de la industria para sistemas de producción es 99.9 % (equivalente a menos de 8.7 horas de caída por año). Un SLA con clientes enterprise típicamente compromete un uptime mínimo garantizado con penalizaciones por incumplimiento.

---

## V

### Value-Based Pricing
**Categoría:** Modelo de Negocio / Pricing
**Definición:** Estrategia de pricing donde el precio se basa en el valor percibido o el ahorro generado para el cliente, no en el costo de producción del producto. El proceso: estimar cuánto ahorra o gana el cliente con el producto y cobrar entre el 10 % y el 20 % de ese valor. Requiere validación con al menos 5 entrevistas a clientes potenciales antes de publicar precios.

### VC (Venture Capital)
**Categoría:** Finanzas / Financiamiento
**Definición:** Fondo de inversión que inyecta capital significativo ($500k USD o más) en startups con alto potencial de crecimiento a cambio de una participación accionaria (generalmente 20-30 % por ronda). Los procesos de cierre con VC toman de 6 a 12 meses e incluyen due diligence técnica, financiera y legal. Son apropiados solo cuando el producto está probado y el mercado es grande.

---

## W

### Wiki
**Categoría:** Operaciones / Documentación
**Definición:** Base de conocimiento colaborativa y estructurada del equipo donde se documenta: procesos operativos, SOPs, decisiones de arquitectura, guías de setup y contexto del producto. Herramientas comunes: Notion, Confluence. Debe actualizarse en el momento en que se toma una decisión o se diseña un proceso, no al final del proyecto cuando los detalles ya se perdieron.

---

*Instructor: Rogelio Arriaga González  -  Raptor Dev Services*
