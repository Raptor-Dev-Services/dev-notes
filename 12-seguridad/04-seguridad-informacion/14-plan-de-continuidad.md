# 14 - Desarrollo e implementación del plan de continuidad

> Unidad 5, tema 2 de la materia *Seguridad de la información*. Del arranque del proyecto al plan escrito: organización y equipos, política de contingencia, análisis de impacto al negocio, métricas de recuperación, controles preventivos, estrategias y planes complementarios. Revisado a noviembre de 2026.

## Por qué importa

Un plan de continuidad existe para que, en el peor momento, nadie tenga que improvisar. Eso solo funciona si el plan se diseñó en frío: con escenarios definidos, tiempos acordados con el negocio, recursos reservados y personas que saben qué les toca. Esta nota sigue el camino de diseño, apoyándose en la guía del NIST (SP 800-34) y en la propuesta del INCIBE, con un ejemplo de lo que exige la regulación bancaria mexicana.

## Antes de empezar: alcance y escenarios

Primero se definen los departamentos, dependencias e instalaciones que cubrirá el plan. No es lo mismo preparar la continuidad de las operaciones críticas del negocio que la de los elementos de tecnologías de la información.

Después se elige, del **catálogo de amenazas**, cuáles atender: las que tienen mayor probabilidad y también las que, aunque poco probables, tendrían un impacto muy alto. En este paso pesan dos cosas: el **presupuesto** disponible y el **apetito de riesgo** de la organización. Con ellas se fijan las premisas de partida del plan.

Las organizaciones tienen que analizar su entorno todo el tiempo para captar los eventos que podrían alterar la operación de sus servicios.

## El proyecto de continuidad

El objetivo de un plan de continuidad del negocio (BCP) es establecer las estrategias y procedimientos que aplicará un equipo de personas que da dirección, apoyo, equipamiento, metodologías y estándares para garantizar que las operaciones continúen. En general, el plan tiene fases de arranque, análisis de impacto al negocio, definición y aplicación de controles, liberación del plan y pruebas.

### Arranque

La primera parte es **declarar la iniciativa** del proyecto de continuidad. Igual que el SGSI (nota [10](10-activar-el-sgsi.md)), necesita aprobación y apoyo de la alta dirección y objetivos alineados con el apetito de riesgo y la estrategia de la organización.

Con el proyecto definido, hay que **entender la organización**: sus actividades o servicios de misión crítica, su estructura, los roles y las partes interesadas, para satisfacer las necesidades clave de los clientes internos y externos y proteger la seguridad y el bienestar del personal.

Para que el proyecto tenga éxito, la alta dirección:

- designa a una persona con autoridad suficiente como responsable de la implantación y de la política de gestión de la continuidad;
- designa a una o más personas para entregar y mantener el programa;
- nombra equipos específicos para atender los incidentes.

### Quién hace qué

| Figura | Responsabilidad |
|---|---|
| Gerente de continuidad del negocio | Lidera el equipo de continuidad; entrega el plan, incorpora la continuidad a la organización y la mantiene. No necesariamente forma parte del equipo de incidentes durante un evento |
| Comité de dirección de continuidad | Formado por los gerentes más importantes, con representación de cada área clave. Asegura que los planes se consideren, revisen, prueben y actualicen cuando cambia la organización. Es indispensable para el gobierno del programa |
| Equipos de incidente | Se organizan en tres niveles, como los servicios de emergencia |

### Los tres niveles de respuesta

El modelo de **oro, plata y bronce** viene de los servicios de emergencia:

| Nivel | Equipo | Qué hace |
|---|---|---|
| Oro (estratégico) | Alta dirección | Dirige el incidente desde la estrategia. No hace tareas de recuperación: decide la planeación de largo plazo para volver a la operación normal, mantiene el enlace con las partes interesadas y atiende a los medios |
| Plata (táctico) | Gestión de incidentes | Tiene el mando y el control central del incidente. Ayuda a los procesos críticos a ejecutar sus planes de recuperación según el plan de gestión de incidentes, y conecta a los equipos de las unidades de negocio con la alta dirección |
| Bronce (operativo) | Respuesta a incidentes | Actúa si hay que llamar a los servicios de emergencia o evacuar edificios. Su papel se concentra en las primeras horas |

Estos equipos solo se forman durante un incidente y no tienen entre sí una relación jerárquica de línea. En la operación diaria, sus integrantes hacen su trabajo normal y asisten a las sesiones informativas y a la capacitación sobre gestión de incidentes.

### La política de contingencia

Después se escriben los estatutos de la **política de planeación de contingencias**: los objetivos, el marco organizacional y las responsabilidades del plan. Sus elementos principales son:

- roles y responsabilidades;
- el alcance: tipos de plataforma y funciones de la organización sujetos a planeación;
- los recursos necesarios;
- los requisitos de capacitación;
- el calendario de pruebas y ejercicios;
- el calendario de mantenimiento del plan;
- la frecuencia mínima de los respaldos y el almacenamiento de los medios de respaldo.

## El análisis de impacto al negocio (BIA)

Establecida la política, el **análisis de impacto al negocio** (BIA, *business impact analysis*) es la base de los controles del plan. Permite al gerente o coordinador de continuidad caracterizar por completo los requisitos, procesos e interdependencias de los sistemas, y con ello fijar los requisitos y las prioridades de continuidad. Su propósito es **relacionar cada componente de TI con los procesos críticos que sostiene** y, con esa información, describir las consecuencias de que ese componente se interrumpa.

El BIA es parte del sistema de gestión de riesgos y es el punto de partida de todo plan de continuidad. Según el NIST, tiene tres pasos:

1. **Determinar los procesos de misión o negocio y su criticidad de recuperación.** Qué procesos soporta el sistema, qué impacto tendría su interrupción y cuánto tiempo puede estar detenido.
2. **Identificar los recursos necesarios** para reanudar esos procesos: equipo, software, datos, personas, instalaciones, proveedores.
3. **Fijar las prioridades de recuperación** de los recursos del sistema: qué se levanta primero.

De cada etapa sale información que servirá para implantar los controles de continuidad.

### Las métricas de recuperación

En esta parte se definen los tiempos y características del plan; de estos indicadores se desprende buena parte de la estrategia:

| Métrica | Qué mide | Quién la define |
|---|---|---|
| MTD (tiempo máximo tolerable de caída) | Cuánto puede estar detenido un proceso antes de que el daño a la organización sea irreversible | El negocio |
| RTO (objetivo de tiempo de recuperación) | Cuánto se tarda, con los recursos disponibles, en volver a poner en marcha el proceso o el servicio con las capacidades mínimas acordadas | Por lo general, el área técnica |
| RPO (objetivo de punto de recuperación) | Hasta qué momento del pasado se podrán recuperar los datos; es decir, cuánta información o cuántas operaciones previas al desastre se está dispuesto a perder | El negocio, con el área técnica |
| ROL (nivel mínimo de operación) | El nivel de servicio con el que se da por recuperada una actividad, aunque no sea el óptimo | El negocio |

Reglas que se derivan:

- El **RTO siempre debe ser menor que el MTD**. Si el MTD de facturación es de 48 horas y su RTO de 12, hay margen; si el RTO fuera de 72, el plan no sirve.
- El **RPO no depende del RTO**. Un proceso puede tolerar una recuperación lenta pero no perder un solo dato (RPO cercano a cero), o al revés. El RPO es el que manda en la política de respaldos: con un RPO de cuatro horas, un respaldo nocturno no alcanza.
- El **ROL** se puede expresar en porcentaje: un centro de atención telefónica puede considerarse recuperado al 70 % de su capacidad.
- También se registran las **dependencias** de otros procesos y de proveedores, porque un proceso no vuelve si su proveedor no vuelve.

## Controles preventivos

Las siguientes etapas implantan controles y estrategias. Los **controles preventivos** buscan que la interrupción no ocurra o que su efecto sea menor. La guía del NIST menciona, entre otros:

- sistemas de energía ininterrumpible (UPS) para cortes breves;
- plantas generadoras de gasolina o diésel para cortes largos;
- aire acondicionado con capacidad redundante;
- sistemas de detección y supresión de incendios;
- sensores de agua en el techo y el piso de las salas de equipo;
- contenedores resistentes al calor y al agua para los medios de respaldo y los registros vitales;
- almacenamiento fuera del sitio de los respaldos y de la documentación;
- controles técnicos de seguridad, como gestión de llaves criptográficas y control de acceso;
- respaldos frecuentes y programados.

## Estrategias de recuperación

Las estrategias se basan, ante todo, en los indicadores del BIA; pero el **costo** pesa igual, porque mantener un alto rendimiento del equipo o del sitio alterno es caro.

### Respaldos

Los datos se respaldan con una frecuencia que respete el RPO, y los respaldos se guardan fuera del sitio, en un lugar lo bastante lejano para no sufrir el mismo desastre. Se combinan respaldos completos, incrementales (solo lo que cambió desde el último respaldo de cualquier tipo) y diferenciales (lo que cambió desde el último completo).

### Sitios alternos

| Tipo de sitio | Qué tiene | Tiempo para operar | Costo |
|---|---|---|---|
| Frío | Espacio, energía y clima; sin equipo ni comunicaciones listos | Semanas | Bajo |
| Templado | Equipo y comunicaciones parcialmente instalados | Días | Medio |
| Caliente | Equipo, comunicaciones y personal listos | Horas | Alto |
| Móvil | Instalación transportable que se lleva al lugar | Días | Variable |
| Espejo | Réplica en tiempo real del sitio principal | Minutos o cero | Muy alto |

Se pueden obtener mediante sitios propios, **acuerdos recíprocos** con otra organización (cada una hospeda a la otra en caso de desastre) o contratos con proveedores comerciales. Hoy la nube agrega opciones de recuperación como servicio, con costo por uso.

### Reemplazo de equipo

Si el equipo se daña, se puede conseguir por acuerdos con proveedores, por un inventario de reserva o aprovechando equipo compatible que ya existe en la organización. Cada opción equilibra costo y tiempo de reposición.

### Planes complementarios

Con la base del plan y los controles, se suman otros planes: el de **respuesta a incidentes**, el de **comunicación y medios** y otros. El INCIBE organiza el plan de contingencia y continuidad en seis fases:

| Fase | Contenido |
|---|---|
| 0. Determinación del alcance | Qué procesos, sedes y sistemas cubre el plan |
| 1. Análisis de la organización | Reuniones con las áreas, análisis de impacto al negocio y análisis de riesgos |
| 2. Estrategia de continuidad | Qué opciones de recuperación se eligen para cada proceso |
| 3. Respuesta a la contingencia | Plan de crisis (o de incidentes), planes operativos de recuperación de entornos y procedimientos técnicos de trabajo |
| 4. Prueba, mantenimiento y revisión | Plan de mantenimiento y planes de pruebas |
| 5. Concienciación | Que el personal conozca el plan y su papel |

Documentar y separar estos subplanes facilita implantar la estrategia y probarla de forma eficaz.

### El manejo de incidentes

Para mantener la continuidad hace falta un procedimiento de manejo de incidentes que contenga la falla sin perder de vista las necesidades futuras de la organización, incluida la rendición de cuentas. La guía del NIST para el manejo de incidentes (SP 800-61 Rev. 2, 2012) describe un ciclo de cuatro fases: preparación; detección y análisis; contención, erradicación y recuperación; y actividades posteriores al incidente, con las lecciones aprendidas.

> Actualización al 2026: en abril de 2025 el NIST publicó la SP 800-61 Rev. 3, que sustituye a la Rev. 2. La respuesta a incidentes deja de verse como un ciclo aislado y se integra a la gestión del riesgo con las seis funciones del CSF 2.0 (gobernar, identificar, proteger, detectar, responder y recuperar); la preparación y las lecciones aprendidas se reparten entre las funciones de gobierno, identificación y protección.

## Un ejemplo regulatorio: los bancos en México

Las Disposiciones de carácter general aplicables a las instituciones de crédito obligan a cada banco a tener un plan de continuidad con requisitos mínimos (los del Anexo 67 de esas disposiciones). El artículo 164 Bis dice, en su parte inicial:

> **Artículo 164 Bis.-** La Dirección General deberá elaborar el Plan de Continuidad de Negocio observando al efecto lo establecido en el Anexo 67 de las presentes disposiciones; dicho plan será presentado para aprobación del Consejo de Administración a través del Comité de Auditoría.
>
> El Director General será responsable de:
>
> I. La implementación, continua actualización y difusión del plan al interior de la Institución. Al efecto, deberá establecer un programa de capacitación que responda a la participación del personal tanto en los procesos que al efecto se identifiquen como críticos, como en el desarrollo del propio plan.
>
> II. Diseñar y llevar a cabo una política de comunicación respecto de la verificación de Contingencias Operativas, la cual deberá ser parte del Plan de Continuidad de Negocio. [...]

El mismo artículo obliga a notificar a la CNBV, dentro de los sesenta minutos siguientes, las contingencias operativas que duren al menos sesenta minutos y que afecten la infraestructura que soporta sucursales y banca electrónica, activen total o parcialmente el plan o afecten el 30 % de sucursales, cajeros, terminales punto de venta o comisionistas; a informar diariamente su estado hasta que concluyan, y a entregar en un plazo máximo de 15 días hábiles un análisis de causas, impacto cualitativo y cuantitativo y acciones para evitar que se repitan.

## Para cerrar el tema

- El BIA es parte de la gestión de riesgos y el punto de partida del plan.
- Sin un procedimiento de manejo de incidentes no hay continuidad.
- El entorno se vigila de forma permanente para detectar eventos que alteren la operación.
- El RPO dice cuánta información previa al desastre se está dispuesto a perder.
- El RTO dice en cuánto tiempo debe volver un servicio con las capacidades mínimas acordadas.

## Glosario

| Término | Definición |
|---|---|
| BIA | Análisis de impacto al negocio: estudio de las funciones de la organización y del efecto de interrumpirlas |
| Apetito de riesgo | Cantidad de riesgo que la organización está dispuesta a aceptar para lograr sus objetivos |
| Comité de dirección de continuidad | Órgano de gerentes clave que gobierna el programa de continuidad |
| Modelo oro, plata y bronce | Organización de la respuesta en niveles estratégico, táctico y operativo |
| MTD | Tiempo máximo que un proceso puede estar detenido sin causar daño irreversible |
| RTO | Tiempo objetivo para restablecer un proceso o servicio con capacidades mínimas |
| RPO | Punto en el tiempo hasta el que se pueden recuperar los datos |
| ROL | Nivel mínimo de servicio con el que una actividad se considera recuperada |
| Control preventivo | Medida que evita una interrupción o reduce su efecto, como un UPS o un sistema contra incendios |
| Sitio alterno | Instalación donde se reanuda la operación cuando el sitio principal no está disponible |
| Sitio caliente | Sitio alterno con equipo, comunicaciones y personal listos para operar en horas |
| Acuerdo recíproco | Convenio entre dos organizaciones para hospedarse mutuamente en caso de desastre |
| Respaldo | Copia de archivos y programas para recuperarlos si hace falta |
| Plan de crisis | Plan que organiza la toma de decisiones y la comunicación durante una contingencia grave |

## Referencias

- Cichonski, P., Millar, T., Grance, T., y Scarfone, K. (2012). *Computer security incident handling guide* (NIST Special Publication 800-61 Rev. 2). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/61/r2/final
- Comisión Nacional Bancaria y de Valores. (2005, con reformas posteriores). *Disposiciones de carácter general aplicables a las instituciones de crédito*. CNBV. https://www.cnbv.gob.mx/Normatividad/Disposiciones%20de%20car%C3%A1cter%20general%20aplicables%20a%20las%20instituciones%20de%20cr%C3%A9dito.pdf
- INCIBE. (s. f.). *Plan de contingencia y continuidad de negocio*. Instituto Nacional de Ciberseguridad. https://www.incibe.es/sites/default/files/contenidos/dosieres/metad_plan_de_contingencia_y_continuidad_de_negocio.pdf
- Nelson, A., Rekhi, S., Souppaya, M., y Scarfone, K. (2025). *Incident response recommendations and considerations for cybersecurity risk management: A CSF 2.0 community profile* (NIST Special Publication 800-61 Rev. 3). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/61/r3/final
- Swanson, M., Bowen, P., Phillips, A. W., Gallup, D., y Lynes, D. (2010). *Contingency planning guide for federal information systems* (NIST Special Publication 800-34 Rev. 1). National Institute of Standards and Technology. https://csrc.nist.gov/pubs/sp/800/34/r1/upd1/final

---

*Rogelio Arriaga Gonzalez*
